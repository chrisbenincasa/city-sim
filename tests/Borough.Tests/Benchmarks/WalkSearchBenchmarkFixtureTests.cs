using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Benchmarks;

/// <summary>
/// That <see cref="WalkSearchBenchmarks"/> is timing a walk search, and which of its rows is timing no
/// search at all.
/// </summary>
/// <remarks>
/// <para>
/// <b>The failure this guards against has happened twice in this corpus and once in this namespace.</b>
/// A benchmark returns a number whatever it runs, so a fixture that quietly stopped reaching the
/// expensive path would publish a fast, precise, meaningless figure — and a walk search has two ways
/// to do that, both of which return in constant time: a same-Segment pair never enters the Dijkstra,
/// and an unreachable pair is answered by a union-find component comparison. Either would look like a
/// very fast search rather than like no search.
/// </para>
/// <para>
/// <b>So the assertions are on <see cref="WalkScratch.Relaxed"/> — settled nodes — and not on the
/// answer.</b> Cost rising with distance is consistent with a fixture that walks a straight line and
/// never searches; <em>work</em> rising with distance is not. That distinction is slice 5 task 11's
/// standing warning in its measuring form: a run records what it did, so assert what it reached.
/// </para>
/// <para>
/// <b>These assert the fixture, not the algorithm.</b> If a change to the generator moves them, the
/// number to correct is the one published in <c>plans/0013</c>, and this failing is the notice.
/// </para>
/// </remarks>
public sealed class WalkSearchBenchmarkFixtureTests
{
    /// <summary>
    /// The benchmark's graph is the shipped city's, not a scaled-down stand-in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two spans, and they stopped being the same number on 2026-08-13.</b>
    /// <see cref="StreetGrid.Span"/> is the <em>index's</em> reach and is sized to
    /// <see cref="CellGrid.WorldTiles"/>, because a player may lay a Street anywhere on the map
    /// (<c>CommandKind.Connect</c>) and the index has to have a slot for it. The
    /// <em>generator's</em> lattice is sized to <see cref="WalkSearchFixture.ExtentTiles"/>, which is
    /// what the node count below is about. This test asserted <c>129</c>, which was both at once while
    /// the map was 4,096 Tiles; at 512 Cells the index span is <b>513</b> and the lattice is unchanged.
    /// </para>
    /// <para>
    /// <b>Both are derived here rather than spelled.</b> A literal in this position is a premise about
    /// map size, and it goes stale silently — <c>plans/0003</c> queue item 6's own finding, arriving in
    /// a third file.
    /// </para>
    /// <para>
    /// Every other Road Graph fixture in the suite widens the block to 512 for the sake of the suite's
    /// wall clock, which is right for a structural question and wrong for a cost: <c>plans/0013</c>'s
    /// standing lesson is that a unit cost is a hypothesis until a real world has produced one.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_benchmark_graph_is_the_shipped_lattice_at_full_size()
    {
        RoadGraph graph = WalkSearchFixture.Shipped();

        Assert.Equal((CellGrid.WorldTiles / graph.Streets.BlockTiles) + 1, graph.Streets.Span);
        int side = (WalkSearchFixture.ExtentTiles / graph.Streets.BlockTiles) + 1;

        // At least the lattice, and not exactly it: Arterial junctions and cut-through ends are nodes
        // too, and they are drawn from the world key rather than laid on the grid.
        Assert.True(
            graph.Nodes.Rows.LiveCount >= side * side,
            $"the lattice laid only {graph.Nodes.Rows.LiveCount} nodes against a {side}x{side} grid; "
            + "the cost of a search over it would not be the cost of a search over the shipped city");
    }

    /// <summary>
    /// <b>The search does more work the further it goes</b> — which is what makes
    /// <see cref="WalkSearchBenchmarks.Blocks"/> an axis rather than a label.
    /// </summary>
    /// <remarks>
    /// Asserted on settled nodes rather than on the answer, and strictly rather than loosely: a walk
    /// twice as far that settles the same nodes is a fixture reporting a straight line, and its timing
    /// column would be flat for a reason that has nothing to do with the algorithm.
    /// </remarks>
    [Fact]
    public void Work_grows_with_distance()
    {
        RoadGraph graph = WalkSearchFixture.Shipped();
        var scratch = new WalkScratch();

        int previous = 0;
        TravelTime previousCost = TravelTime.Zero;

        foreach (int blocks in (int[])[1, 2, 4, 8, 16, 32])
        {
            (Address from, Address to) = WalkSearchFixture.Apart(graph, blocks);
            TravelTime cost = WalkRouting.Cost(graph, from, to, TravelTime.Zero, scratch);

            Assert.False(cost.IsImpassable, $"the {blocks}-block pair is not reachable");
            Assert.True(
                scratch.Relaxed > previous,
                $"{blocks} blocks settled {scratch.Relaxed} nodes against {previous} at half the "
                + "distance — the benchmark's axis is not moving the search");
            Assert.True(cost > previousCost, $"{blocks} blocks did not cost more than half of it");

            previous = scratch.Relaxed;
            previousCost = cost;
        }
    }

    /// <summary>
    /// <b>The across-the-street row times no search</b>, and is in the harness for exactly that reason.
    /// </summary>
    /// <remarks>
    /// A same-Segment walk is two offsets subtracted plus the crossing cost (<c>adr/0074</c>), so it
    /// is the floor the search's cost is read against. If this ever settles a node the closed-form
    /// branch has been lost and the published floor is a search.
    /// </remarks>
    [Fact]
    public void The_across_the_street_case_never_enters_the_search()
    {
        RoadGraph graph = WalkSearchFixture.Shipped();
        var scratch = new WalkScratch();

        (Address kerb, Address opposite) = WalkSearchFixture.AcrossTheStreet(graph);
        TravelTime cost = WalkRouting.Cost(graph, kerb, opposite, TravelTime.Zero, scratch);

        Assert.False(cost.IsImpassable);
        Assert.Equal(0, scratch.Relaxed);
    }

    /// <summary>
    /// <b>The severed case is answered without settling a node</b> — the finding, not the control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <i>No route exists</i> reads like the worst case: an exhausted search over a whole component.
    /// It is instead the cheapest of the three, because union-find components over the foot subgraph
    /// are rebuilt with the adjacency and the question is a comparison. <b>That matters to task 4's
    /// design</b>: the severed Trip is the one 5b exists to report, and a generator built to avoid
    /// producing it would be avoiding a cost that is not there.
    /// </para>
    /// <para>
    /// <b>Taken on the severing Ruleset rather than on the shipped one, and the reason is
    /// robustness rather than availability.</b> The shipped lattice does strand a pocket at this
    /// suite's seed — seven nodes, see below — but it strands nothing at the other seeds tried, so a
    /// severed pair drawn from it exists by luck. <c>rulesets/severance.toml</c>'s values have a
    /// characterised floor of 32.3% across seeds, so this row survives a re-seed and the one below
    /// records the draw.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_severed_case_is_answered_in_constant_time()
    {
        RoadGraph graph = WalkSearchFixture.Severing();
        var scratch = new WalkScratch();

        (Address islandA, Address islandB) = WalkSearchFixture.Severed(graph);
        TravelTime cost = WalkRouting.Cost(graph, islandA, islandB, TravelTime.Zero, scratch);

        Assert.True(cost.IsImpassable);
        Assert.Equal(0, scratch.Relaxed);
    }

    /// <summary>
    /// <b>The shipped lattice strands a pocket at the seed the whole suite runs on, and at no other
    /// seed tried.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test was written to assert the opposite and was wrong, which is why it is here.</b>
    /// <c>plans/0020</c>'s amendment records that the shipped 32-Tile lattice strands <i>zero</i>
    /// walkable nodes <b>on seven of eight seeds</b> — and the eighth turns out to be
    /// <see cref="WalkSearchFixture.Seed"/>, the constant every Road Graph fixture in this suite lays
    /// from. Seven of 16,641 walkable nodes sit in a pocket with no pedestrian route to the rest of
    /// the city: <b>0.04%</b>, which is why <c>--roads</c> reports it as 0.0% and why nobody had met
    /// it.
    /// </para>
    /// <para>
    /// <b>The distinction it enforces is S2 R0.5's, arriving in a different subsystem.</b> <i>The
    /// shipped Ruleset severs nothing</i> is a claim about a <b>table</b>; what is true of any
    /// particular world is a claim about a <b>draw</b> of the Arterial polyline, which is hashed off
    /// the world key. The two are not the same sentence, and the corpus has now been caught reading
    /// one as the other twice.
    /// </para>
    /// <para>
    /// <b>Consequence for the benchmark, and it is the reason this is asserted rather than noted.</b>
    /// <see cref="WalkSearchFixture.Apart"/> checks reachability by running the search precisely
    /// because of this: a pair drawn blind from the shipped graph is <em>not</em> guaranteed
    /// connected, and one drawn from the pocket would have timed a constant-time component comparison
    /// and published it as the cost of a walk search.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shipped_lattice_strands_a_pocket_at_the_suites_own_seed()
    {
        RoadConnectivity atTheSuitesSeed = WalkSearchFixture.Shipped().Connectivity;

        Assert.Equal(16_641, atTheSuitesSeed.WalkableNodes);
        Assert.Equal(7, atTheSuitesSeed.StrandedOnFoot);

        // Three other draws of the same [roads] table, which strand nothing at all.
        foreach (ulong seed in (ulong[])[2, 3, 4])
        {
            Assert.Equal(0, WalkSearchFixture.LaidAt(seed).Connectivity.StrandedOnFoot);
        }
    }
}
