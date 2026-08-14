using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// The path source (5c task 3): <b>the search hands back the Segments it crossed, and the proof is
/// that re-walking them costs exactly what the search said</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Reconstruction rather than comparison, because there is nothing to compare against.</b> This is
/// the first route this project has ever produced, so no second implementation exists and no
/// baseline holds one. What can be checked without one is stronger than a spot check: take the
/// Segment list back to the Road Graph, follow it hop by hop from the origin, and confirm it forms a
/// connected chain, ends at the destination, and sums to the cost. A route that is plausible but
/// wrong fails all three.
/// </para>
/// <para>
/// <b>No vehicle, no storage and no baseline movement.</b> Nothing in the simulation reads a route
/// yet — the route cache is task 4 and the drive Leg is task 5 — so this is the route-finder alone,
/// validated against arithmetic it cannot fake.
/// </para>
/// </remarks>
public sealed class RoutePathTests(ITestOutputHelper output)
{
    /// <summary>How many destinations to reconstruct. Every node would be a slow test for no more proof.</summary>
    private const int Samples = 64;

    /// <summary>
    /// A recorded search returns a connected chain of Segments whose times sum to the cost it
    /// reported.
    /// </summary>
    /// <remarks>
    /// <b>Three independent claims in one walk, and the middle one is the load-bearing one.</b> The
    /// chain has to <em>connect</em> — each Segment must leave the node the previous one arrived at —
    /// which is what distinguishes a real route from a set of Segments that happen to lie near it. The
    /// sum then has to match to the last <see cref="Fixed"/> unit, not approximately: the predecessor
    /// records the Arc that produced the winning cost, so any drift between the recorded route and the
    /// settled cost is a defect rather than a rounding difference.
    /// </remarks>
    [Fact]
    public void A_recorded_route_reconstructs_the_cost_the_search_reported()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;

        int origin = FirstWalkable(graph);
        WalkScratch scratch = new();

        scratch.Begin(graph.Nodes.Rows.SlotCount, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.SettleAll(graph, TravelMode.Foot);

        int[] route = new int[graph.Segments.Rows.SlotCount];
        int checked_ = 0;
        int longest = 0;

        foreach (int target in Sample(graph, origin))
        {
            TravelTime cost = scratch.CostTo(target);

            if (cost.IsImpassable)
            {
                continue;
            }

            int length = scratch.PathTo(graph.Arcs, target, route);

            Assert.NotEqual(WalkScratch.NoPath, length);

            TravelTime walked = ReWalk(graph, origin, route.AsSpan(0, length), out int arrived);

            Assert.Equal(target, arrived);
            Assert.Equal(cost, walked);

            checked_++;
            longest = Math.Max(longest, length);
        }

        Assert.True(checked_ > 8, $"only {checked_} destinations were reachable to reconstruct");

        output.WriteLine($"reconstructed {checked_} routes, longest {longest} Segments");
    }

    /// <summary>
    /// The origin has an empty route, and a node the search never reached has none at all.
    /// </summary>
    /// <remarks>
    /// <b>Zero and <see cref="WalkScratch.NoPath"/> are different answers and a caller must be able to
    /// tell them apart.</b> A journey that begins where it ends crosses no Segments, which is a
    /// correct route of length zero; a journey to an unreachable node has no route. Collapsing the two
    /// would make a severed destination read as *you are already there* — the failure mode with no
    /// symptom, since both produce a traveller that does not move.
    /// </remarks>
    [Fact]
    public void An_origin_has_an_empty_route_and_an_unreached_node_has_none()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;

        int origin = FirstWalkable(graph);
        WalkScratch scratch = new();
        int[] route = new int[16];

        scratch.Begin(graph.Nodes.Rows.SlotCount, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);

        // Before the search runs, the origin is relaxed but not settled — so it has no route yet.
        Assert.Equal(WalkScratch.NoPath, scratch.PathTo(graph.Arcs, origin, route));

        scratch.SettleAll(graph, TravelMode.Foot);

        Assert.Equal(0, scratch.PathTo(graph.Arcs, origin, route));
        Assert.Equal(WalkScratch.NoPath, scratch.PathTo(graph.Arcs, int.MaxValue - 1, route));
    }

    /// <summary>
    /// A search that was not asked to record has no route to give, and costs the same either way.
    /// </summary>
    /// <remarks>
    /// <b>The opt-in has to be free for a walk and honest when forgotten.</b> A walk Leg discards its
    /// route by decision (<c>adr/0075</c>), so recording is off by default and every existing caller
    /// pays nothing. The second half is the one that could rot: without the flag the predecessor
    /// arrays still hold whatever the last recording search left, and the stamp cannot notice because
    /// the stamp is written by the cost side. Returning <see cref="WalkScratch.NoPath"/> is what stops
    /// a forgotten flag from returning somebody else's journey.
    /// </remarks>
    [Fact]
    public void A_search_that_was_not_recording_reports_no_route_and_costs_the_same()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;

        int origin = FirstWalkable(graph);
        int nodes = graph.Nodes.Rows.SlotCount;
        WalkScratch scratch = new();
        int[] route = new int[64];

        scratch.Begin(nodes, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.SettleAll(graph, TravelMode.Foot);

        int[] targets = [.. Sample(graph, origin)];
        TravelTime[] recorded = [.. targets.Select(scratch.CostTo)];

        scratch.Begin(nodes);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.SettleAll(graph, TravelMode.Foot);

        for (int i = 0; i < targets.Length; i++)
        {
            Assert.Equal(recorded[i], scratch.CostTo(targets[i]));
            Assert.Equal(WalkScratch.NoPath, scratch.PathTo(graph.Arcs, targets[i], route));
        }
    }

    /// <summary>
    /// A route too long for the buffer is measured and not written.
    /// </summary>
    /// <remarks>
    /// <b>A truncated route is a different route, not a partial one.</b> It ends at a node the
    /// traveller was passing through, so a caller that ignored the length would get a complete,
    /// connected, plausible journey to the wrong place. The buffer is therefore left untouched and the
    /// required length returned, which is the only contract under which ignoring the answer fails
    /// loudly.
    /// </remarks>
    [Fact]
    public void A_route_that_does_not_fit_is_measured_and_not_written()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;

        int origin = FirstWalkable(graph);
        WalkScratch scratch = new();

        scratch.Begin(graph.Nodes.Rows.SlotCount, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.SettleAll(graph, TravelMode.Foot);

        int target = Sample(graph, origin).First(t => scratch.PathTo(graph.Arcs, t, []) > 2);
        int length = scratch.PathTo(graph.Arcs, target, []);

        int[] tooSmall = [.. Enumerable.Repeat(-7, length - 1)];

        Assert.Equal(length, scratch.PathTo(graph.Arcs, target, tooSmall));
        Assert.All(tooSmall, slot => Assert.Equal(-7, slot));

        int[] exact = new int[length];

        Assert.Equal(length, scratch.PathTo(graph.Arcs, target, exact));
        Assert.Equal(length, ReWalkLength(graph, origin, exact));
    }

    /// <summary>
    /// A point-to-point search says which of its two targets it arrived at, and the route reaches it.
    /// </summary>
    /// <remarks>
    /// <b>The cost does not say where the journey ended, and for a vehicle that is half the answer.</b>
    /// A walk leaves its Segment by either endpoint and enters the destination's by either, so
    /// <see cref="WalkScratch.Search"/> returns the cheapest of four combinations. Which one won
    /// decides the last Segment of the route — and therefore, for a drive Leg, which end of the street
    /// the vehicle arrives at. A walk never asked, which is why nothing noticed it was missing.
    /// </remarks>
    [Fact]
    public void A_point_to_point_search_names_the_endpoint_its_route_reaches()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;

        int origin = FirstWalkable(graph);
        WalkScratch scratch = new();

        scratch.Begin(graph.Nodes.Rows.SlotCount, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.SettleAll(graph, TravelMode.Foot);

        int targetA = Sample(graph, origin).First(t => scratch.PathTo(graph.Arcs, t, []) > 2);
        int targetB = Sample(graph, origin).Last(t => scratch.PathTo(graph.Arcs, t, []) > 2);

        scratch.Begin(graph.Nodes.Rows.SlotCount, recordPath: true);
        scratch.Seed(origin, TravelTime.Zero);

        TravelTime best = scratch.Search(
            graph, TravelMode.Foot, targetA, targetB, TravelTime.Zero, TravelTime.Zero);

        Assert.False(best.IsImpassable);
        Assert.True(scratch.Arrived == targetA || scratch.Arrived == targetB);

        int[] route = new int[graph.Segments.Rows.SlotCount];
        int length = scratch.PathTo(graph.Arcs, scratch.Arrived, route);

        Assert.NotEqual(WalkScratch.NoPath, length);

        TravelTime walked = ReWalk(graph, origin, route.AsSpan(0, length), out int arrived);

        Assert.Equal(scratch.Arrived, arrived);
        Assert.Equal(best, walked);
    }

    /// <summary>
    /// The mode parameter reaches the arc costs: the same route is quicker by car than on foot.
    /// </summary>
    /// <remarks>
    /// <b>The cheapest check that <see cref="WalkScratch.Search"/> stopped being foot-only.</b> It read
    /// <c>FootTime</c> and tested the foot bit directly until 5c task 3; a mode parameter that was
    /// threaded through the signature and not through the loop would compile, pass every existing
    /// test, and quietly price every car journey at walking pace. Both modes traverse the same Street
    /// lattice in the shipped Ruleset, so the topology is held constant and only the speed differs.
    /// </remarks>
    [Fact]
    public void A_car_search_is_quicker_than_a_walk_over_the_same_streets()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;

        int origin = FirstWalkable(graph);
        WalkScratch scratch = new();

        scratch.Begin(graph.Nodes.Rows.SlotCount);
        scratch.Seed(origin, TravelTime.Zero);
        scratch.SettleAll(graph, TravelMode.Foot);

        int target = Sample(graph, origin).First(t => !scratch.CostTo(t).IsImpassable);
        TravelTime onFoot = scratch.CostTo(target);

        scratch.Begin(graph.Nodes.Rows.SlotCount);
        scratch.Seed(origin, TravelTime.Zero);

        TravelTime byCar = scratch.Search(
            graph, TravelMode.Car, target, target, TravelTime.Zero, TravelTime.Zero);

        Assert.False(byCar.IsImpassable);
        Assert.True(byCar < onFoot, $"car {byCar.Raw} was not quicker than foot {onFoot.Raw}");

        output.WriteLine($"foot {onFoot.Raw} Ticks, car {byCar.Raw} Ticks");
    }

    /// <summary>
    /// Follows a Segment list from the origin, summing the arc times it crosses.
    /// </summary>
    /// <remarks>
    /// <b>The graph is consulted from scratch, which is what makes this a check rather than a
    /// restatement.</b> Nothing here reads the predecessor arrays: at every hop it searches the
    /// current node's own Arcs for one carrying the next Segment, so a Segment that does not leave the
    /// node the route claims to be at throws rather than being skipped.
    /// </remarks>
    private static TravelTime ReWalk(
        RoadGraph graph, int origin, ReadOnlySpan<int> route, out int arrived)
    {
        RoadNodeTable nodes = graph.Nodes;
        RoadArcs arcs = graph.Arcs;

        TravelTime total = TravelTime.Zero;
        int cursor = origin;

        foreach (int segment in route)
        {
            int start = nodes.ArcStart[cursor];
            int count = nodes.ArcCount[cursor];
            int taken = -1;

            for (int i = start; i < start + count; i++)
            {
                if (arcs.Segment[i] != segment || !arcs.Admits(i, TravelMode.Foot))
                {
                    continue;
                }

                if (taken < 0 || arcs.FootTime[i] < arcs.FootTime[taken])
                {
                    taken = i;
                }
            }

            Assert.True(taken >= 0, $"Segment {segment} does not leave node {cursor}");

            total += arcs.FootTime[taken];
            cursor = arcs.Target[taken];
        }

        arrived = cursor;

        return total;
    }

    /// <summary>How many hops a Segment list turns out to be. A connectivity check with no cost in it.</summary>
    private static int ReWalkLength(RoadGraph graph, int origin, ReadOnlySpan<int> route)
    {
        ReWalk(graph, origin, route, out _);

        return route.Length;
    }

    /// <summary>The lowest node slot with a walkable Arc — a deterministic origin with no draw in it.</summary>
    private static int FirstWalkable(RoadGraph graph)
    {
        for (int node = 0; node < graph.Nodes.Rows.SlotCount; node++)
        {
            if (!graph.Nodes.Rows.IsLive(node))
            {
                continue;
            }

            int start = graph.Nodes.ArcStart[node];

            for (int i = start; i < start + graph.Nodes.ArcCount[node]; i++)
            {
                if (graph.Arcs.Admits(i, TravelMode.Foot))
                {
                    return node;
                }
            }
        }

        throw new InvalidOperationException("the graph has no walkable node");
    }

    /// <summary>
    /// A spread of destination nodes, taken by a coprime stride so the sample is deterministic and
    /// does not cluster near the origin.
    /// </summary>
    private static IEnumerable<int> Sample(RoadGraph graph, int origin)
    {
        int slots = graph.Nodes.Rows.SlotCount;

        for (int i = 1; i <= Samples; i++)
        {
            int node = ((i * 37) + 11) % slots;

            if (node != origin && graph.Nodes.Rows.IsLive(node))
            {
                yield return node;
            }
        }
    }

    private static Simulation Populated(int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, GoldenFixtures.Rules());

        Replay.Trace(simulation, log, new Ticks(1), 1, []);

        return simulation;
    }
}
