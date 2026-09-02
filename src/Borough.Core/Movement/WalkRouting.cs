using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

/// <summary>
/// Resolves a Leg: <b>the cost of getting from one <see cref="Address"/> to another in a given
/// <see cref="TravelMode"/></b>, over that mode's subgraph.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>The name is now wrong and the rename is deferred rather than forgotten.</b> This served walks
/// only until 5c task 5 gave the commute a car; <see cref="WalkScratch"/> is misnamed for the same
/// reason and by a larger margin. Both are one mechanical rename across ~70 call sites, and doing it
/// inside a task that changes behaviour would bury the behaviour change in the diff.
/// </para>
/// <para>
/// <b>For a walk Leg <c>distance / speed</c> is not an approximation, it is the exact answer</b>
/// (<c>03 §3.7</c>), and the reason is a property of pedestrians rather than a simplification:
/// pedestrian networks do not saturate, so there is no congestion term to be wrong about. <b>This is
/// what made milestone 5b buildable before 5c</b> — a walk Leg needs no travel-time matrix, no route
/// cache and no volume-delay function.
/// </para>
/// <para>
/// ⚠ <b>For a drive Leg it is <em>not</em> the exact answer, and nothing here says so.</b> A car's
/// free-flow time is what a Segment costs with nobody else on it, and 5c task 6's volume-delay
/// function is the term that makes it a real one. Until that lands, a drive is priced at free-flow —
/// which is an <em>underestimate</em>, in the one mode where the error grows with the city.
/// </para>
/// <para>
/// <b>The search is over a subgraph, not a second network.</b> The mode bit lives on the
/// <see cref="RoadArcs">Arc</see> (<c>adr/0072</c>), so a Street's footway is the same Segment with
/// the bit set and walking adds no Segments at all. <b>This is also where Severance is</b>: an
/// Arterial's Arcs carry <see cref="TravelMode.Car"/> and not <see cref="TravelMode.Foot"/>, so
/// nobody deletes a pedestrian route — the mask simply never granted one, and the search genuinely
/// fails.
/// </para>
/// <para>
/// <b>The result is a cost and the path is discarded</b> (<c>adr/0075</c>). Nothing downstream reads
/// a walk Leg's Segments: <c>CONTEXT.md</c> → Fidelity keeps pedestrians out of Stress entirely, so a
/// walk Leg increments no Segment volume. <b>A drive Leg's Segments are a different matter</b> —
/// <c>adr/0041</c> attributes volume by the traveller, so task 6 needs them; that is what
/// <see cref="WalkScratch.PathTo"/> exists for, and it is opt-in so that no walk pays for it.
/// </para>
/// </remarks>
public static class WalkRouting
{
    /// <summary>
    /// The cost of travelling between two Addresses in one mode, or
    /// <see cref="TravelTime.Impassable"/> when no route exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><paramref name="mode"/> has no default, and 5c task 3 is why.</b> That task threaded a
    /// mode through <see cref="WalkScratch.Search"/>'s signature and not through its loop, and the
    /// result compiled, passed every test, and priced every car journey at walking pace. A
    /// <see cref="TravelMode.Foot"/> default here is the same hazard pointing the other way: a new
    /// caller writes the call it is used to and silently gets a walk. <b>Foot is a legitimate answer,
    /// so a foot default cannot announce itself</b> — session F's rule about placeholders, and the
    /// same reason <paramref name="crossingCost"/> has no default either.
    /// </para>
    /// <para>
    /// ⚠ <b><paramref name="crossingCost"/> is a pedestrian term and is charged to pedestrians only</b>
    /// (<c>adr/0074</c>). A driver's equivalent — the U-turn or the trip round the block that reaching
    /// the far kerb actually costs — is a property of a <em>junction</em>, and this simulation models
    /// no turns at all. Charging the pedestrian figure to a car would be inventing that mechanism at a
    /// number chosen for something else; <c>adr/0070</c> says an undesigned absence is evidence of
    /// nothing, so the honest answer is zero and a note. <b>The visible artefact is that driving across
    /// the street is cheaper than walking across it</b>, which is true of this model and false of a
    /// city, and it is bounded by one Segment.
    /// </para>
    /// <para>
    /// <b>The crossing applies only when the two Addresses share a Segment and differ in side.</b>
    /// That is exact for the across-the-street case walkability turns on, and silent elsewhere —
    /// because <i>the same side</i> stops meaning anything once a route turns a corner, so charging it
    /// on a multi-Segment walk would be inventing precision the model does not have.
    /// </para>
    /// </remarks>
    /// <param name="graph">The Road Graph to search.</param>
    /// <param name="mode">How the journey is made. Exactly one of <see cref="TravelMode"/>'s modes.</param>
    /// <param name="from">Where the journey starts.</param>
    /// <param name="to">Where it ends.</param>
    /// <param name="crossingCost">What it costs a pedestrian to reach the other side of a Segment.</param>
    /// <param name="scratch">Reusable search state; one per caller, never shared across threads.</param>
    /// <param name="recordPath">
    /// Whether to retain the Segments crossed, readable afterwards through
    /// <see cref="WalkScratch.PathTo"/> at <see cref="WalkScratch.Arrived"/>. <b>Off by default and
    /// that default is safe</b>, unlike <paramref name="mode"/>'s would be: not recording is the
    /// absence of a second output rather than a wrong answer to the question asked, so a caller that
    /// forgets it gets nothing instead of getting something plausible.
    /// </param>
    public static TravelTime Cost(
        RoadGraph graph, TravelMode mode, Address from, Address to, TravelTime crossingCost,
        WalkScratch scratch, bool recordPath = false)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(scratch);

        // 🔴 Before the rejects and not after them. Five of the returns below answer without searching,
        // and a caller reading WalkScratch.Arrived on one of those paths was reading the PREVIOUS
        // journey's destination -- so a drive Leg between two Addresses on one Segment recorded another
        // Traveller's route and attributed adr/0041's volume along it. See WalkScratch.Forget.
        scratch.Forget();

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

        if (!Admits(segments, fromSegment, mode) || !Admits(segments, toSegment, mode))
        {
            return TravelTime.Impassable;
        }

        // The same-Segment case is closed form and never reaches the search. It is also the only
        // case a crossing cost applies to, which is what keeps that term's blast radius at exactly
        // the across-the-street question it was argued for.
        if (fromSegment == toSegment)
        {
            Tiles along = (from.Offset - to.Offset).Magnitude;
            TravelTime direct = TravelTime.Over(along, SpeedOn(graph, fromSegment, mode));

            return from.Side == to.Side || mode != TravelMode.Foot ? direct : direct + crossingCost;
        }

        // The reachability reject. Two Addresses in different components have no route, so the
        // search below would settle the whole of the origin's component to prove it — the most
        // expensive answer this method can give, for the one question it can be certain about
        // without searching at all.
        if (!Connected(graph, mode, fromSegment, toSegment))
        {
            return TravelTime.Impassable;
        }

        return Across(graph, mode, from, to, fromSegment, toSegment, scratch, recordPath);
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
    private static bool Connected(RoadGraph graph, TravelMode mode, int fromSegment, int toSegment)
    {
        if (!graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[fromSegment], out int from)
            || !graph.Nodes.Rows.TryResolve(graph.Segments.NodeA[toSegment], out int to))
        {
            return true;
        }

        Column<int> component = Component(graph.Nodes, mode);
        int origin = component[from];
        int destination = component[to];

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
        RoadGraph graph, TravelMode mode, Address from, Address to, int fromSegment, int toSegment,
        WalkScratch scratch, bool recordPath)
    {
        RoadSegmentTable segments = graph.Segments;
        RoadNodeTable nodes = graph.Nodes;

        if (!Endpoints(graph, fromSegment, out int fromA, out int fromB)
            || !Endpoints(graph, toSegment, out int toA, out int toB))
        {
            return TravelTime.Impassable;
        }

        // Severance, answered in constant time. Union-find components over the mode's subgraph are
        // rebuilt with the adjacency, so *is this reachable at all* is a comparison rather than an
        // exhausted search — which matters because the severed case is the one 5b exists to report
        // and would otherwise be the most expensive query in the city.
        Column<int> component = Component(nodes, mode);
        int destination = component[toA];

        if (component[fromA] != destination && component[fromB] != destination)
        {
            return TravelTime.Impassable;
        }

        Speed fromSpeed = SpeedOn(graph, fromSegment, mode);
        Speed toSpeed = SpeedOn(graph, toSegment, mode);
        Tiles fromLength = segments.LengthTiles[fromSegment];
        Tiles toLength = segments.LengthTiles[toSegment];

        scratch.Begin(nodes.Rows.SlotCount, recordPath);
        scratch.Seed(fromA, TravelTime.Over(from.Offset, fromSpeed));
        scratch.Seed(fromB, TravelTime.Over(fromLength - from.Offset, fromSpeed));

        TravelTime inA = TravelTime.Over(to.Offset, toSpeed);
        TravelTime inB = TravelTime.Over(toLength - to.Offset, toSpeed);

        return scratch.Search(graph, mode, toA, toB, inA, inB);
    }

    /// <summary>Whether a Segment admits this mode in either direction.</summary>
    private static bool Admits(RoadSegmentTable segments, int slot, TravelMode mode) =>
        (segments.Modes[slot] & (byte)mode) != 0;

    /// <summary>
    /// The connectivity labels for a mode — <b>the one routing question that is certain without a
    /// search, per mode</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Two columns and they genuinely disagree.</b> An Arterial's Arcs carry
    /// <see cref="TravelMode.Car"/> and not <see cref="TravelMode.Foot"/>, so a city can be one piece
    /// to a driver and several to a pedestrian — which is what Severance <em>is</em>. Reading the foot
    /// labels for a car would refuse drives across a severing Arterial, and reading the car labels for
    /// a walk would send a pedestrian down a motorway; both compile and neither has a symptom short of
    /// a distribution nobody is looking at.
    /// </remarks>
    private static Column<int> Component(RoadNodeTable nodes, TravelMode mode) =>
        mode == TravelMode.Foot ? nodes.FootComponent : nodes.CarComponent;

    /// <summary>
    /// Pace on a Segment: the road's free-flow, capped by the traveller's own ceiling if it has one.
    /// </summary>
    /// <remarks>
    /// <b>A pedestrian has a ceiling and a driver does not, and that asymmetry is the model rather
    /// than an omission.</b> A pedestrian walks at walking pace on a boulevard and in a lane alike, so
    /// the road's speed binds only where the road is somehow slower than a person. A car's ceiling
    /// <em>is</em> the road's free-flow — <c>03 §3.7</c>'s congestion term is what will lower it, and
    /// that is 5c task 6's volume-delay function rather than a second constant here.
    /// </remarks>
    private static Speed SpeedOn(RoadGraph graph, int segment, TravelMode mode)
    {
        Speed freeFlow = graph.Segments.FreeFlow[segment];

        return mode == TravelMode.Foot ? Speed.SlowerOf(freeFlow, graph.Ruleset.WalkSpeed) : freeFlow;
    }

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
