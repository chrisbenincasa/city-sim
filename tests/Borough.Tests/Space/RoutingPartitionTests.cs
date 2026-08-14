using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// The routing partition — <b>the tiling the travel-time matrix keys on</b> (<c>adr/0040</c>,
/// <c>adr/0047</c>), and the cross-check on the size 5c task 1 chose provisionally.
/// </summary>
/// <remarks>
/// <para>
/// <b>Half of this file is a structural suite and half is a standing measurement, and the second
/// half exists because of what 5b-bis's job-search box turned out to be.</b> That box was derived
/// correctly, tested correctly, and had never filtered anything in any world the project builds —
/// because nobody asserted what it did to a real city, only that its arithmetic was right. The
/// readings below print and <b>assert both ends of the ladder</b>, so a future change that quietly
/// makes the partition degenerate on the fixtures anybody runs fails here rather than being
/// discovered a milestone later.
/// </para>
/// <para>
/// <b>Both ends, on <c>JobSearchBoxTests</c>' precedent and for its reason.</b> A single-sided
/// assertion — <i>the partition is smaller than the map</i> — passes on a partition of one, which is
/// the failure mode that matters here: a matrix with four rows cannot be wrong in any way a test can
/// see. So each reading asserts a floor <em>and</em> a ceiling.
/// </para>
/// <para>
/// <b>The structural half reads the implementation rather than restating it.</b>
/// <see cref="RoutingPartition.DesignEdge"/>, <see cref="RoutingPartition.Side"/> and
/// <see cref="ChunkGrid.CellsPerChunk"/> come off the types under test; a test that recomputed them
/// from its own literals would be <c>plans/0012</c> <i>Cause 1</i> with extra steps — one fact,
/// stored twice, and the copy in the test is the one that drifts silently green.
/// </para>
/// </remarks>
public sealed class RoutingPartitionTests(ITestOutputHelper output)
{
    /// <summary>
    /// The edge is a whole number of Cells and the Chunk divides it — <c>adr/0040</c>'s owed
    /// correction, as an assertion rather than as a comment.
    /// </summary>
    /// <remarks>
    /// <b>This is the test that ADR was missing.</b> It sized the cluster in <em>Chunks</em> and
    /// never ran <c>05 §4</c>'s hash test on the dependency that creates: the Chunk is declared
    /// <em>tuning, hash-preserving</em>, so a routing structure whose size is <c>k</c> Chunks lets a
    /// profiler change the city by turning a knob documented as unable to. The repair reverses the
    /// arrow — the partition is a multiple of the frozen Cell, and the Chunk must divide it — and the
    /// ADR's own consequence says the constraint <i>"is load-bearing and must be enforced ... it
    /// would do so silently"</i>. An unenforced alignment rule is exactly the thing that fails
    /// quietly.
    /// </remarks>
    [Fact]
    public void The_edge_is_a_whole_number_of_Cells_and_the_Chunk_divides_it()
    {
        int edge = RoutingPartition.DesignEdge.Raw;

        Assert.True(edge >= ChunkGrid.CellsPerChunk, "A partition is at least one Chunk.");
        Assert.Equal(0, edge % ChunkGrid.CellsPerChunk);
        Assert.Equal(0, CellGrid.WorldCells % edge);
        Assert.Equal(0, edge & (edge - 1));

        // The map divides into a whole number of partitions on each axis, so no partition hangs off
        // the north or east edge holding a fraction of the ground every other one holds.
        RoutingPartition partition = new(RoutingPartition.DesignEdge);

        Assert.Equal(CellGrid.WorldCells / edge, partition.Side);
        Assert.Equal(RoutingPartition.DesignEdge, partition.Edge);
    }

    /// <summary>An edge that is not a power of two, is smaller than a Chunk, or larger than the map, is refused.</summary>
    /// <remarks>
    /// <b>Refused at construction rather than clamped</b>, because every one of these produces a
    /// tiling that works — it simply disagrees with the Cell grid about which side of a line a node
    /// is on, which is the class of defect <c>05 §5</c> unified the two grids to make
    /// unrepresentable. A clamp would silently build the neighbouring size.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(CellGrid.WorldCells * 2)]
    public void An_edge_that_would_not_tile_the_map_is_refused(int edge) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingPartition(new Cells(edge)));

    /// <summary>
    /// A chain of nodes partitions by geometry, and a tie for the access node goes to the lower slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A hand-built fixture, so the answer is known in advance rather than compared against
    /// itself</b> — <c>RoadFixtures</c>' stated reason for having hand-built graphs at all. Nine
    /// nodes 32 Tiles apart span 0–256 Tiles; a partition is 128 Tiles at
    /// <see cref="RoutingPartition.DesignEdge"/>, so the chain falls in three partitions holding four,
    /// four and one.
    /// </para>
    /// <para>
    /// <b>The tie-break is the interesting half.</b> Every node sits at north 0 and every partition's
    /// centre is 64 Tiles north of it, so the Chebyshev distance is 64 for all four nodes in the first
    /// partition and the comparison never separates them. The access node is therefore decided
    /// entirely by <see cref="RoutingPartition.Rebuild"/>'s strict <c>&lt;</c> walking in slot order —
    /// which is the property that makes a rebuilt world agree with a continuously-run one, and the
    /// only way to see it is a fixture where the geometry declines to choose.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_chain_partitions_by_geometry_and_a_tie_goes_to_the_lower_slot()
    {
        RoadGraph graph = RoadFixtures.Chain(9);
        RoutingPartition partition = graph.Partition;

        Assert.Equal(3, partition.Count);

        Assert.Equal(0, partition.At(Tiles.Zero, Tiles.Zero));
        Assert.Equal(0, partition.At(new Tiles(96), Tiles.Zero));
        Assert.Equal(1, partition.At(new Tiles(128), Tiles.Zero));
        Assert.Equal(2, partition.At(new Tiles(256), Tiles.Zero));

        Assert.Equal(0, partition.EastOf(0));
        Assert.Equal(1, partition.EastOf(1));
        Assert.Equal(0, partition.NorthOf(2));

        // Slots 0..3 all sit 64 Tiles from the centre; slot 0 wins because it is first.
        Assert.Equal(0, partition.AccessNode(0, TravelMode.Foot));
        Assert.Equal(4, partition.AccessNode(1, TravelMode.Foot));
        Assert.Equal(8, partition.AccessNode(2, TravelMode.Foot));
    }

    /// <summary>Ground no node stands on is in no partition, and the map's outside is not a partition either.</summary>
    [Fact]
    public void Empty_ground_and_the_outside_are_both_absent()
    {
        RoutingPartition partition = RoadFixtures.Chain(9).Partition;

        Assert.Equal(RoutingPartition.None, partition.At(new Tiles(4_096), Tiles.Zero));
        Assert.Equal(RoutingPartition.None, partition.At(new Tiles(-32), Tiles.Zero));
        Assert.Equal(
            RoutingPartition.None,
            partition.At(new Tiles(CellGrid.WorldTiles), Tiles.Zero));
    }

    /// <summary>
    /// Partitions are numbered in grid order, so the numbering cannot depend on the node free list.
    /// </summary>
    /// <remarks>
    /// <b><c>05 §3</c>'s bar cleared with room to spare, and the assertion is what proves the margin
    /// is real.</b> A derived structure earns <c>(derived AND rebuilt)</c> only if its <em>order</em>
    /// is recoverable from saved state. A first-touch numbering in slot order would clear that bar
    /// too — and would tie the matrix's row order to the order road was laid in, so a bulldoze that
    /// recycled a slot would renumber the matrix without changing the city. Row-major numbering is a
    /// function of occupancy alone, and this is the shape of that claim: strictly ascending in
    /// <c>(north, east)</c>.
    /// </remarks>
    [Fact]
    public void Partitions_are_numbered_in_grid_order()
    {
        RoutingPartition partition = Populated(GoldenFixtures.Population).World.Roads.Partition;

        Assert.True(partition.Count > 1, "The fixture must occupy more than one partition.");

        int previous = int.MinValue;

        for (int id = 0; id < partition.Count; id++)
        {
            int key = (partition.NorthOf(id) * partition.Side) + partition.EastOf(id);

            Assert.True(key > previous, $"Partition {id} is out of grid order.");
            previous = key;
        }
    }

    /// <summary>A graph with no nodes has no partitions, rather than one empty one.</summary>
    /// <remarks>
    /// <b>The state a world is in before <c>CommandKind.Populate</c> runs, and a player's world is in
    /// permanently until they lay a Street</b> (<c>adr/0090</c>). A tiling that reported one partition
    /// over an empty map would give task 2 a matrix of one row measured from a node that does not
    /// exist, and every guard downstream is written against <em>absent</em> rather than against
    /// <em>empty</em>.
    /// </remarks>
    [Fact]
    public void A_graph_with_no_nodes_has_no_partitions() =>
        Assert.Equal(0, new RoadGraph(RoadFixtures.Roads()).Partition.Count);

    /// <summary>
    /// Every live node has a partition, and its partition's access node is in that same partition.
    /// </summary>
    /// <remarks>
    /// <b>The second half is the one that would fail silently.</b> An access node drawn from the wrong
    /// partition gives a matrix whose rows are labelled correctly and measured from somewhere else —
    /// an error with no upper bound and no symptom except a travel time that is wrong by a constant
    /// nobody can attribute. Membership is cheap to check and the check is the only thing standing
    /// between task 2 and that.
    /// </remarks>
    [Fact]
    public void Every_node_has_a_partition_and_every_access_node_is_in_its_own()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoadGraph graph = simulation.World.Roads;
        RoutingPartition partition = graph.Partition;

        Assert.True(partition.Count > 0);

        for (int slot = 0; slot < graph.Nodes.Rows.SlotCount; slot++)
        {
            if (graph.Nodes.Rows.IsLive(slot))
            {
                Assert.NotEqual(RoutingPartition.None, partition.Of(graph.Nodes, slot));
            }
        }

        for (int id = 0; id < partition.Count; id++)
        {
            int access = partition.AccessNode(id, TravelMode.Foot);

            Assert.True(graph.Nodes.Rows.IsLive(access), $"Partition {id} has no live access node.");
            Assert.Equal(id, partition.Of(graph.Nodes, access));
        }
    }

    /// <summary>
    /// A rebuild reproduces the partition exactly — the same count, the same coordinates, the same
    /// access nodes.
    /// </summary>
    /// <remarks>
    /// <b>Exactly rather than plausibly, which is the phrase <c>05 §3</c> turns on.</b> This is the
    /// reload path: <c>World.RebuildDerived</c> is what runs after a save is read back, and a derived
    /// structure that reproduces a <em>different</em> valid answer is the failure nothing reports,
    /// because it never reaches the State Hash. <c>BuildingResidency</c> and <c>CommuteRoster</c> each
    /// carry the same test for the same reason.
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_the_partition_exactly()
    {
        Simulation simulation = Populated(GoldenFixtures.Population);
        RoutingPartition partition = simulation.World.Roads.Partition;

        int count = partition.Count;
        int[] east = new int[count];
        int[] north = new int[count];
        int[] access = new int[count];

        for (int id = 0; id < count; id++)
        {
            east[id] = partition.EastOf(id);
            north[id] = partition.NorthOf(id);
            access[id] = partition.AccessNode(id, TravelMode.Foot);
        }

        simulation.World.RebuildDerived();

        Assert.Equal(count, partition.Count);

        for (int id = 0; id < count; id++)
        {
            Assert.Equal(east[id], partition.EastOf(id));
            Assert.Equal(north[id], partition.NorthOf(id));
            Assert.Equal(access[id], partition.AccessNode(id, TravelMode.Foot));
        }
    }

    /// <summary>
    /// The partition covers the city rather than the map, and the city is never one partition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reading 5c task 2 has to refute, taken now so that it is a before rather than a
    /// memory.</b> Two numbers, and they pull opposite ways. The floor: the golden fixture must
    /// occupy enough partitions for a matrix over it to be capable of being wrong — at 8 Cells it
    /// occupies 2×2 and no test could see an error. The ceiling: the occupied count must stay far
    /// below the map's <see cref="RoutingPartition.Side"/>², because the matrix is quadratic in it and
    /// a matrix over a 512-Cell map is 1.07 GB where a matrix over a 1M city is 8.3 MB. That is
    /// <c>adr/0021</c>'s <em>scale with developed area, not map area</em> arriving one structure on
    /// from <c>RoadGenerator</c>.
    /// </para>
    /// <para>
    /// <b>⚠ What this cannot tell you is whether the size is right</b>, only that it is not
    /// degenerate. Entry error is the ratifier and it needs a matrix, which is task 2. The figures
    /// print so that the comparison is available when it runs.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_partition_covers_the_city_and_never_collapses_to_one()
    {
        Ruleset rules = GoldenFixtures.Rules();
        int atFixture = 0;

        output.WriteLine(" population  nodes    partitions  of map      spread");

        foreach (int population in (int[])[4_000, 10_000, 40_000, 160_000])
        {
            RoadGraph graph = Populated(population, rules).World.Roads;
            RoutingPartition partition = graph.Partition;

            int east = 0;
            int north = 0;

            for (int id = 0; id < partition.Count; id++)
            {
                east = Math.Max(east, partition.EastOf(id) + 1);
                north = Math.Max(north, partition.NorthOf(id) + 1);
            }

            output.WriteLine(
                $" {population,-11} {graph.Nodes.Rows.LiveCount,-8} {partition.Count,-11}"
                + $" {100.0 * partition.Count / (partition.Side * partition.Side),-11:0.00}%"
                + $" {east}x{north}");

            if (population == GoldenFixtures.Population)
            {
                atFixture = partition.Count;
            }

            Assert.True(
                partition.Count < partition.Side * partition.Side / 4,
                "The partition has started covering map rather than city.");
        }

        Assert.True(
            atFixture >= 9,
            $"The golden fixture occupies {atFixture} partitions; a matrix that small cannot be "
            + "measurably wrong, which is what the ratifier in task 2 needs it to be able to be.");
    }

    private static Simulation Populated(int population, Ruleset? rules = null)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(population),
            GoldenFixtures.RulesetHash);

        builder.Append(new Ticks(0), new Command(CommandKind.Populate, default, default));

        InputLog log = builder.Build();
        Simulation simulation = Replay.Start(log, rules ?? GoldenFixtures.Rules());

        // One Tick, because Populate arrives through Phase 0 like every other command (adr/0080) and
        // Replay.Start only opens the log. The road lattice does not exist until the verb has run.
        Replay.Trace(simulation, log, new Ticks(1), 1, []);

        return simulation;
    }
}
