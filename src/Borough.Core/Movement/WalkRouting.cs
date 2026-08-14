using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Resolves a walk Leg: <b>the cost of getting from one <see cref="Address"/> to another on foot</b>,
/// over the <see cref="TravelMode.Foot"/> subgraph.
/// </summary>
/// <remarks>
/// <para>
/// <b>For a walk Leg <c>distance / speed</c> is not an approximation, it is the exact answer</b>
/// (<c>03 §3.7</c>), and the reason is a property of pedestrians rather than a simplification:
/// pedestrian networks do not saturate, so there is no congestion term to be wrong about. <b>This is
/// what makes milestone 5b buildable before 5c</b> — a walk Leg needs no travel-time matrix, no route
/// cache and no volume-delay function, and it is half the Legs in the city.
/// </para>
/// <para>
/// <b>The search is over a subgraph, not a second network.</b> The foot bit lives on the
/// <see cref="RoadArcs">Arc</see> (<c>adr/0072</c>), so a Street's footway is the same Segment with
/// the bit set and walking adds no Segments at all. <b>This is also where Severance is</b>: an
/// Arterial's Arcs carry <see cref="TravelMode.Car"/> and not <see cref="TravelMode.Foot"/>, so
/// nobody deletes a pedestrian route — the mask simply never granted one, and the search genuinely
/// fails.
/// </para>
/// <para>
/// <b>The result is a cost and the path is discarded</b> (<c>adr/0075</c>). Nothing downstream reads
/// a walk Leg's Segments: <c>CONTEXT.md</c> → Fidelity keeps pedestrians out of Stress entirely, so a
/// walk Leg increments no Segment volume. A pedestrian Map Layer — footfall on a high street, which
/// the corpus has not designed — is the one consumer that would need them, and the answer then is to
/// accumulate into the Layer <em>during</em> the search rather than to retain the path.
/// </para>
/// </remarks>
public static class WalkRouting
{
    /// <summary>
    /// The cost of walking between two Addresses, or <see cref="TravelTime.Impassable"/> when no
    /// route exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><paramref name="crossingCost"/> has no default, and that is deliberate.</b> It is
    /// <c>[trips]</c> Ruleset data, hash-bearing, and <c>plans/0002</c> §D2 carries it as unset with
    /// a named ratifier — <b>5b must report a walk-Leg cost distribution and must not choose the
    /// value</b>. Session F's own finding is the reason it is a required parameter rather than a
    /// defaulted one: <i>a placeholder whose value sits inside the range of legitimate answers cannot
    /// announce itself</i>. Zero is a legitimate crossing cost, so a zero default would be
    /// indistinguishable from a decision, and nothing later would know a number had been skipped.
    /// </para>
    /// <para>
    /// <b>The crossing applies only when the two Addresses share a Segment and differ in side</b>
    /// (<c>adr/0074</c>). That is exact for the across-the-street case walkability turns on, and
    /// silent elsewhere — because <i>the same side</i> stops meaning anything once a route turns a
    /// corner, so charging it on a multi-Segment walk would be inventing precision the model does not
    /// have.
    /// </para>
    /// </remarks>
    /// <param name="graph">The Road Graph to search.</param>
    /// <param name="from">Where the walk starts.</param>
    /// <param name="to">Where it ends.</param>
    /// <param name="crossingCost">What it costs to reach the other side of a Segment.</param>
    /// <param name="scratch">Reusable search state; one per caller, never shared across threads.</param>
    public static TravelTime Cost(
        RoadGraph graph, Address from, Address to, TravelTime crossingCost, WalkScratch scratch)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(scratch);

        // A Building with no frontage, or an endpoint whose Segment was bulldozed and which the read
        // boundary has already turned into a named absence (adr/0079). Not a long walk — no walk,
        // and the caller records *no route found* or *stranded* accordingly.
        if (!from.Exists || !to.Exists)
        {
            return TravelTime.Impassable;
        }

        RoadSegmentTable segments = graph.Segments;
        int fromSegment = from.Segment;
        int toSegment = to.Segment;

        if (!segments.Rows.IsLive(fromSegment) || !segments.Rows.IsLive(toSegment))
        {
            return TravelTime.Impassable;
        }

        if (!Walkable(segments, fromSegment) || !Walkable(segments, toSegment))
        {
            return TravelTime.Impassable;
        }

        // The same-Segment case is closed form and never reaches the search. It is also the only
        // case a crossing cost applies to, which is what keeps that term's blast radius at exactly
        // the across-the-street question it was argued for.
        if (fromSegment == toSegment)
        {
            Tiles along = (from.Offset - to.Offset).Magnitude;
            TravelTime direct = TravelTime.Over(along, FootSpeed(graph, fromSegment));

            return from.Side == to.Side ? direct : direct + crossingCost;
        }

        // The reachability reject. Two Addresses in different foot components have no route, so the
        // search below would settle the whole of the origin's component to prove it — the most
        // expensive answer this method can give, for the one question it can be certain about
        // without searching at all.
        if (!Connected(graph, fromSegment, toSegment))
        {
            return TravelTime.Impassable;
        }

        return Across(graph, from, to, fromSegment, toSegment, scratch);
    }

    /// <summary>
    /// Whether two Segments are in the same <see cref="TravelMode.Foot"/> component — <b>the one
    /// routing question that is certain without a search</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An integer comparison against labels milestone 5a has computed since it shipped, and which
    /// nothing has ever read.</b> <see cref="RoadConnectivity"/> unions both endpoints of every
    /// Segment admitting the mode, so a component label is a property of the graph rather than of any
    /// structure laid over it: two Addresses in different components genuinely have no route, and two
    /// in the same one genuinely have a route to find. That is what makes this a <em>reject</em> and
    /// not an estimate — there is no margin, no tuning number and nothing to ratify.
    /// </para>
    /// <para>
    /// <b>⚠ Not the travel-time matrix's Impassable, and 5c task 2 nearly used that instead.</b> A
    /// matrix entry runs access node to access node, so a routing partition holding two pieces that
    /// do not connect to each other can report *severed* for a journey that succeeds — an unsound
    /// reject that discards reachable work with no symptom. Measured on <c>rulesets/severance.toml</c>
    /// the matrix happened not to misfire, which makes that an untriggered hazard rather than a
    /// refuted one. ***A structure laid over a graph cannot answer a question about the graph.***
    /// </para>
    /// <para>
    /// <b>Behaviour-identical, and that is the test that says so.</b> Under <c>05 §4</c> a change is
    /// an optimisation if the State Hash is unchanged: this returns <see cref="TravelTime.Impassable"/>
    /// in exactly the cases the search would have, so every counter, Fate and rung downstream reads
    /// the same. It is guarded to fire only where both labels resolve, so a Segment the labelling
    /// never reached falls through to the search rather than being refused on a default.
    /// </para>
    /// </remarks>
    private static bool Connected(RoadGraph graph, int fromSegment, int toSegment)
    {
        if (!graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[fromSegment], out int from)
            || !graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[toSegment], out int to))
        {
            return true;
        }

        int origin = graph.Nodes.FootComponent[from];
        int destination = graph.Nodes.FootComponent[to];

        return origin == RoadConnectivity.Unlabelled
            || destination == RoadConnectivity.Unlabelled
            || origin == destination;
    }

    /// <summary>
    /// The between-Segments case: walk to an endpoint, search node to node, walk in from the other
    /// end.
    /// </summary>
    /// <remarks>
    /// <b>Four combinations rather than one, because an Address is not a Node.</b> A walk may leave
    /// its Segment by either endpoint and enter the destination's by either, so the answer is the
    /// cheapest of the four — which the search gets for free by seeding both origin endpoints with
    /// their partial costs and reading both destination endpoints at the end.
    /// </remarks>
    private static TravelTime Across(
        RoadGraph graph, Address from, Address to, int fromSegment, int toSegment,
        WalkScratch scratch)
    {
        RoadSegmentTable segments = graph.Segments;
        RoadNodeTable nodes = graph.Nodes;

        if (!Endpoints(graph, fromSegment, out int fromA, out int fromB)
            || !Endpoints(graph, toSegment, out int toA, out int toB))
        {
            return TravelTime.Impassable;
        }

        // Severance, answered in constant time. Union-find components over the foot subgraph are
        // rebuilt with the adjacency, so *is this reachable at all* is a comparison rather than an
        // exhausted search — which matters because the severed case is the one 5b exists to report
        // and would otherwise be the most expensive query in the city.
        int destination = nodes.FootComponent[toA];

        if (nodes.FootComponent[fromA] != destination && nodes.FootComponent[fromB] != destination)
        {
            return TravelTime.Impassable;
        }

        Speed fromSpeed = FootSpeed(graph, fromSegment);
        Speed toSpeed = FootSpeed(graph, toSegment);
        Tiles fromLength = segments.LengthTiles[fromSegment];
        Tiles toLength = segments.LengthTiles[toSegment];

        scratch.Begin(nodes.Rows.SlotCount);
        scratch.Seed(fromA, TravelTime.Over(from.Offset, fromSpeed));
        scratch.Seed(fromB, TravelTime.Over(fromLength - from.Offset, fromSpeed));

        TravelTime inA = TravelTime.Over(to.Offset, toSpeed);
        TravelTime inB = TravelTime.Over(toLength - to.Offset, toSpeed);

        return scratch.Search(graph, TravelMode.Foot, toA, toB, inA, inB);
    }

    /// <summary>Whether a Segment admits pedestrians in either direction.</summary>
    private static bool Walkable(RoadSegmentTable segments, int slot) =>
        (segments.Modes[slot] & (byte)TravelMode.Foot) != 0;

    /// <summary>
    /// Walking pace on a Segment: <c>min(the walker's ceiling, the road's free-flow)</c>.
    /// </summary>
    /// <remarks>
    /// The same rule <see cref="RoadGraph"/> applies to a whole Arc, applied to part of one. A
    /// pedestrian walks at walking pace on a boulevard and in a lane alike, so the road's speed binds
    /// only where the road is somehow slower than a person.
    /// </remarks>
    private static Speed FootSpeed(RoadGraph graph, int segment) =>
        Speed.SlowerOf(graph.Segments.FreeFlow[segment], graph.Ruleset.WalkSpeed);

    /// <summary>A Segment's two endpoint node slots, or false if either is dangling.</summary>
    private static bool Endpoints(RoadGraph graph, int segment, out int a, out int b)
    {
        a = 0;
        b = 0;

        RoadSegmentTable segments = graph.Segments;

        return graph.Nodes.Rows.TryResolve(segments.NodeA[segment], out a)
            && graph.Nodes.Rows.TryResolve(segments.NodeB[segment], out b);
    }
}
