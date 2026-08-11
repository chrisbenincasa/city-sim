using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// Severance: <b>a city can be perfectly well connected for cars and broken for people, and the game
/// can say so</b> (<c>CONTEXT.md</c> → Severance).
/// </summary>
/// <remarks>
/// <para>
/// <b>The whole point of <c>plans/0020</c> task 2, and the design's flagship emergent behaviour.</b>
/// It is emergent rather than scripted: nothing deletes a pedestrian route. An Arterial's Arcs carry
/// <see cref="TravelMode.Car"/> and not <see cref="TravelMode.Foot"/>, an Arterial physically occupies
/// the ground its polyline crosses so the Streets it runs over stop existing, and what is left is a
/// pedestrian network in pieces. The mask simply never granted a route.
/// </para>
/// <para>
/// <b>Every test here carries its control, because the interesting assertion is a negative.</b>
/// <i>The foot network is severed</i> is worth nothing without <i>and the car network, over the same
/// Segments, is not</i> — otherwise a graph that failed to generate at all would pass. This is
/// <c>ReplayTests</c>' habit of asserting <c>NotEqual</c> against a control, applied to a mechanism
/// rather than to a hash.
/// </para>
/// <para>
/// <b>The walk is a real traversal of the CSR rather than a comparison of component labels.</b> The
/// labels come from union-find over the Segments; a walk goes node to node along the Arcs, through
/// the mask, exactly as a router would. Two mechanisms agreeing is evidence, and one mechanism
/// agreeing with itself is not — which matters here because it is <see cref="RoadConnectivity"/>
/// whose answer the <c>pool</c> scope is going to consume.
/// </para>
/// </remarks>
public sealed class RoadSeveranceTests
{
    /// <summary>
    /// With no crossings, some pair of nodes is reachable by car and not on foot.
    /// </summary>
    /// <remarks>
    /// <b>This is the sentence <c>CONTEXT.md</c> → Severance states, as an assertion.</b> The pair is
    /// found by component label and then <em>confirmed by walking</em>, so the claim rests on the
    /// adjacency and not only on the labelling that produced the candidate.
    /// </remarks>
    [Fact]
    public void Without_crossings_a_pair_is_connected_for_cars_and_broken_for_people()
    {
        RoadGraph graph = Laid(RoadFixtures.Severing(crossingEvery: 0));

        Assert.True(
            TryFindSeveredPair(graph, out int from, out int to),
            "no node pair was reachable by car and unreachable on foot, so this fixture severs "
            + "nothing and every assertion about Severance below it would be vacuous.");

        Assert.True(Reaches(graph, from, to, TravelMode.Car));
        Assert.False(Reaches(graph, from, to, TravelMode.Foot));
    }

    /// <summary>
    /// Keeping every severed Street as a crossing puts the pedestrian network back.
    /// </summary>
    /// <remarks>
    /// <b>The control for the test above, and it is the one that could catch a generator that had
    /// stopped severing at all.</b> At <c>foot_crossing_every = 1</c> every Street an Arterial runs
    /// over survives as a footbridge — the road is gone, the footway is not — so the foot network
    /// spans exactly what it spanned before any Arterial was laid.
    /// </remarks>
    [Fact]
    public void Keeping_every_severed_street_as_a_crossing_reconnects_the_pedestrians()
    {
        RoadGraph graph = Laid(RoadFixtures.Severing(crossingEvery: 1));

        Assert.False(
            TryFindSeveredPair(graph, out _, out _),
            "a pair was still cut off on foot although every severed Street was kept as a crossing.");
    }

    /// <summary>
    /// Crossings are a dial, and the pedestrian network gets worse as they get rarer.
    /// </summary>
    /// <remarks>
    /// <b>Monotone rather than a threshold, which is what makes it a design knob.</b> A city does not
    /// become severed at a magic value; it becomes steadily more severed as the crossings thin out,
    /// which is <c>HONEST DEGRADATION</c> — pressure arrives as a gradient before it arrives as a
    /// failure.
    /// </remarks>
    [Fact]
    public void Rarer_crossings_leave_the_pedestrian_network_in_more_pieces()
    {
        int dense = PedestrianPieces(Laid(RoadFixtures.Severing(crossingEvery: 1)));
        int sparse = PedestrianPieces(Laid(RoadFixtures.Severing(crossingEvery: 8)));
        int none = PedestrianPieces(Laid(RoadFixtures.Severing(crossingEvery: 0)));

        Assert.True(
            dense <= sparse && sparse <= none,
            $"the pedestrian network was in {dense} pieces with every crossing kept, {sparse} with "
            + $"every eighth, and {none} with none. Thinning the crossings did not make it worse, so "
            + "the dial does not do what the Ruleset says it does.");

        Assert.True(
            none > dense,
            $"removing every crossing left the pedestrian network in {none} pieces against {dense} "
            + "with all of them, so no crossing was load-bearing and this fixture cannot show "
            + "Severance.");
    }

    /// <summary>
    /// An Arterial admits cars and refuses pedestrians, in both directions.
    /// </summary>
    /// <remarks>
    /// The mechanism under everything above. If an Arterial ever carried a foot Arc, Severance would
    /// quietly stop existing and every count in this file would still look plausible.
    /// </remarks>
    [Fact]
    public void No_arterial_carries_a_pedestrian_arc()
    {
        RoadGraph graph = Laid(RoadFixtures.Severing(crossingEvery: 0));

        int arterials = 0;

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot)
                || (RoadKind)graph.Segments.Kind[slot] != RoadKind.Arterial)
            {
                continue;
            }

            arterials++;

            Assert.Equal(0, graph.Segments.Modes[slot] & (byte)TravelMode.Foot);
            Assert.NotEqual(0, graph.Segments.Modes[slot] & (byte)TravelMode.Car);
        }

        Assert.True(arterials > 0, "no Arterial was laid, so this test asserted nothing.");
    }

    /// <summary>
    /// A crossing kept from a severed Street carries feet and not cars.
    /// </summary>
    /// <remarks>
    /// <b>The asymmetry that is the whole of Severance</b>: the road is gone, the footbridge is not.
    /// It is also why a crossing changes <see cref="RoadKind"/> rather than only its mask — free-flow
    /// and capacity follow the kind, so a crossing left as a <see cref="RoadKind.Street"/> would be a
    /// footbridge with a 50 km/h speed limit on it.
    /// </remarks>
    [Fact]
    public void A_crossing_carries_feet_and_not_cars()
    {
        RoadGraph graph = Laid(RoadFixtures.Severing(crossingEvery: 1));

        int crossings = 0;

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot)
                || (RoadKind)graph.Segments.Kind[slot] != RoadKind.FootPath)
            {
                continue;
            }

            crossings++;

            Assert.Equal((byte)TravelMode.Foot, graph.Segments.Modes[slot]);
            Assert.Equal(graph.Ruleset.WalkSpeed, graph.Segments.FreeFlow[slot]);
        }

        Assert.True(crossings > 0, "no Street was kept as a crossing, so this test asserted nothing.");
    }

    /// <summary>
    /// Both branches the generator can take were reached — a Street severed, and one kept.
    /// </summary>
    /// <remarks>
    /// <b>Slice 10 task 11's standing lesson, applied before it can bite.</b> A baseline records what
    /// a run <em>did</em>, so a change that narrows what a run <em>reaches</em> is invisible in it by
    /// construction: the Zone Rule's create branch stopped running entirely, every hash still moved,
    /// and every test still passed. Here the equivalent silent narrowing is an Arterial that stops
    /// crossing any Street — the Segment count stays healthy and the severance mechanism simply never
    /// runs.
    /// </remarks>
    [Fact]
    public void Both_the_severed_and_the_kept_branches_are_reached()
    {
        RoadGraph graph = Laid(RoadFixtures.Severing(crossingEvery: 4));

        int streets = 0;
        int crossings = 0;

        for (int slot = 0; slot < graph.Segments.Rows.SlotCount; slot++)
        {
            if (!graph.Segments.Rows.IsLive(slot))
            {
                continue;
            }

            switch ((RoadKind)graph.Segments.Kind[slot])
            {
                case RoadKind.Street:
                    streets++;
                    break;

                case RoadKind.FootPath:
                    crossings++;
                    break;

                default:
                    break;
            }
        }

        Assert.True(crossings > 0, "the generator kept no crossing; the kept branch is uncovered.");

        int intact = Laid(RoadFixtures.Roads(blockTiles: 256, arterials: 0, crossingEvery: 4, footPaths: 0))
            .Segments.Rows.LiveCount;

        Assert.True(
            streets < intact,
            $"{streets} Streets survived against {intact} with no Arterial on the map, so nothing "
            + "was severed and the delete branch is uncovered.");
    }

    // --- The walk -----------------------------------------------------------------------------

    /// <summary>
    /// Finds two <em>pedestrian places</em> in one car component and two foot components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both ends must be somewhere a pedestrian can stand, and getting that wrong was this file's
    /// first result.</b> Written without the <see cref="Walkable"/> filter, this found a severed pair
    /// in every graph including the one with a crossing on every severed Street — because an Arterial
    /// lays its own nodes, those nodes carry no foot Arc, and a node with no foot Arc is trivially its
    /// own foot component. The pair it kept returning was <i>an intersection, and a point in the
    /// middle of a dual carriageway</i>, which is a true statement about the labels and says nothing
    /// whatever about Severance.
    /// </para>
    /// <para>
    /// <b>The mistake generalises, and it is why <see cref="RoadConnectivity.LargestFoot"/> exists.</b>
    /// A component count over a subgraph counts the nodes the mode cannot use as isolated singletons,
    /// so it rises with the size of the <em>other</em> mode's network. Severance is a claim about two
    /// places pedestrians <em>use</em> being cut apart, and that predicate has to be stated rather than
    /// inferred from a count.
    /// </para>
    /// <para>
    /// Walked in slot order and returning the first pair, so the answer is a function of the graph
    /// rather than of enumeration order — the same discipline a derived structure is rebuilt under.
    /// </para>
    /// </remarks>
    private static bool TryFindSeveredPair(RoadGraph graph, out int from, out int to)
    {
        from = -1;
        to = -1;

        RoadNodeTable nodes = graph.Nodes;

        for (int a = 0; a < nodes.Rows.SlotCount; a++)
        {
            if (!Walkable(graph, a))
            {
                continue;
            }

            for (int b = a + 1; b < nodes.Rows.SlotCount; b++)
            {
                if (!Walkable(graph, b))
                {
                    continue;
                }

                if (nodes.CarComponent[a] == nodes.CarComponent[b]
                    && nodes.FootComponent[a] != nodes.FootComponent[b])
                {
                    from = a;
                    to = b;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// The pieces the pedestrian network is in, counting only the nodes pedestrians can use.
    /// </summary>
    /// <remarks>
    /// <see cref="RoadConnectivity.FootComponents"/> with the car-only nodes taken out. The Core
    /// number is the honest one and the runner prints it beside its largest component for exactly this
    /// reason; this is the one a test can compare across Rulesets, because it does not move when the
    /// Arterial count does.
    /// </remarks>
    private static int PedestrianPieces(RoadGraph graph)
    {
        var seen = new HashSet<int>();

        for (int slot = 0; slot < graph.Nodes.Rows.SlotCount; slot++)
        {
            if (Walkable(graph, slot))
            {
                seen.Add(graph.Nodes.FootComponent[slot]);
            }
        }

        return seen.Count;
    }

    /// <summary>Whether any Arc out of this node admits a pedestrian.</summary>
    private static bool Walkable(RoadGraph graph, int slot)
    {
        if (!graph.Nodes.Rows.IsLive(slot))
        {
            return false;
        }

        int end = graph.Nodes.ArcStart[slot] + graph.Nodes.ArcCount[slot];

        for (int arc = graph.Nodes.ArcStart[slot]; arc < end; arc++)
        {
            if (graph.Arcs.Admits(arc, TravelMode.Foot))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks the mode's subgraph from one node, through the Arcs, exactly as a router would.
    /// </summary>
    /// <remarks>
    /// <b>Breadth-first over the CSR, and it is the only thing in the suite that uses the adjacency
    /// as an adjacency.</b> Everything else asserts about the shape of the Arc array; this asks the
    /// question the array exists to answer.
    /// </remarks>
    private static bool Reaches(RoadGraph graph, int from, int to, TravelMode mode)
    {
        var seen = new bool[graph.Nodes.Rows.SlotCount];
        var queue = new Queue<int>();

        seen[from] = true;
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            int node = queue.Dequeue();

            if (node == to)
            {
                return true;
            }

            int end = graph.Nodes.ArcStart[node] + graph.Nodes.ArcCount[node];

            for (int arc = graph.Nodes.ArcStart[node]; arc < end; arc++)
            {
                int target = graph.Arcs.Target[arc];

                if (graph.Arcs.Admits(arc, mode) && !seen[target])
                {
                    seen[target] = true;
                    queue.Enqueue(target);
                }
            }
        }

        return false;
    }

    private static RoadGraph Laid(RoadRuleset roads)
    {
        World world = new(citizens: 100, RoadFixtures.With(roads));

        SyntheticCity.PopulateInto(world, WorldKey.FromSeed(0x5E_5E_5E), Ticks.Zero);

        return world.Roads;
    }
}
