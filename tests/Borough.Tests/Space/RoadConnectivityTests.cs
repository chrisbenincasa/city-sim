using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Space;

/// <summary>
/// The component labelling, against graphs whose answer is known before the code runs.
/// </summary>
/// <remarks>
/// <para>
/// <b>The definition of done names this fixture directly</b> — <i>"connectivity components are
/// correct against a hand-built disconnected fixture"</i> — and the emphasis is on <em>hand-built</em>.
/// <see cref="RoadSeveranceTests"/> asks whether the labelling moves in the right direction when the
/// Ruleset changes, which is a comparison of the mechanism against itself; these ask whether it
/// produces the number a person counted on a diagram.
/// </para>
/// <para>
/// <b>What this deliverable is for is the <c>pool</c> scope, which does not exist yet.</b>
/// <c>04 §6</c> requires that a District whose internal Road Graph is broken must still fail to
/// distribute, and that is prose until something can answer <em>are these two places on the same
/// network</em>. Nothing consumes <see cref="RoadNodeTable.CarComponent"/> today, so these tests are
/// the only thing standing between a wrong label and a bug that surfaces two milestones later in a
/// mechanism that had nothing to do with it.
/// </para>
/// </remarks>
public sealed class RoadConnectivityTests
{
    /// <summary>Two chains with nothing between them are two components, of known size each.</summary>
    /// <remarks>
    /// <b>The size assertion is the load-bearing half.</b> A count of two is also what a labelling
    /// that lost half the graph would report, and what one that merged the islands and then split a
    /// chain in the middle would report. Asserting how many nodes each component holds pins it.
    /// </remarks>
    [Fact]
    public void Two_islands_are_two_components_of_equal_size()
    {
        RoadGraph graph = RoadFixtures.TwoIslands(each: 6);

        Assert.Equal(2, graph.Connectivity.CarComponents);
        Assert.Equal(2, graph.Connectivity.FootComponents);
        Assert.Equal(6, graph.Connectivity.LargestCar);
        Assert.Equal(6, graph.Connectivity.LargestFoot);

        Assert.Equal([6, 6], Sizes(graph, graph.Nodes.CarComponent));
    }

    /// <summary>Every node in one island shares a label, and no node shares one across the gap.</summary>
    /// <remarks>
    /// The property a consumer will actually use: not <em>how many components</em> but <em>is this
    /// label the same as that one</em>. Stated over the nodes rather than over the counts, so a
    /// labelling that got the arithmetic right and the assignment wrong is caught.
    /// </remarks>
    [Fact]
    public void A_label_is_shared_within_an_island_and_never_across_the_gap()
    {
        RoadGraph graph = RoadFixtures.TwoIslands(each: 6);
        Column<int> label = graph.Nodes.CarComponent;

        for (int slot = 1; slot < 6; slot++)
        {
            Assert.Equal(label[0], label[slot]);
        }

        for (int slot = 7; slot < 12; slot++)
        {
            Assert.Equal(label[6], label[slot]);
        }

        Assert.NotEqual(label[0], label[6]);
    }

    /// <summary>A chain is one component however long it is.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(64)]
    public void A_chain_is_one_component(int nodes)
    {
        RoadGraph graph = RoadFixtures.Chain(nodes);

        Assert.Equal(1, graph.Connectivity.CarComponents);
        Assert.Equal(nodes, graph.Connectivity.LargestCar);
    }

    /// <summary>
    /// A node with no Segment on it is its own component.
    /// </summary>
    /// <remarks>
    /// <b>Deliberate, and it is the reason <see cref="RoadConnectivity.LargestCar"/> is reported
    /// beside the count.</b> An isolated node is genuinely unreachable and calling it a component is
    /// the honest answer; the cost is that the count rises with the number of stranded corners and
    /// stops distinguishing <i>a city in eight pieces</i> from <i>a city in one piece with seven
    /// stranded corners</i>. The pair of numbers distinguishes them and the count alone cannot.
    /// </remarks>
    [Fact]
    public void An_isolated_node_is_its_own_component()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        graph.Nodes.Create(new Tiles(2_048), new Tiles(2_048));
        graph.RebuildDerived();

        Assert.Equal(2, graph.Connectivity.CarComponents);
        Assert.Equal(4, graph.Connectivity.LargestCar);
    }

    /// <summary>
    /// A Segment only cars may use joins the car network and leaves the foot network in two.
    /// </summary>
    /// <remarks>
    /// <b>The minimal Severance, built by hand, and the smallest graph that can show it.</b> Two
    /// chains and one car-only Segment between them: one car component and two foot components, which
    /// is <c>CONTEXT.md</c> → Severance in six nodes. Everything
    /// <see cref="RoadSeveranceTests"/> asserts over a generated city reduces to this.
    /// </remarks>
    [Fact]
    public void A_car_only_segment_joins_one_network_and_not_the_other()
    {
        RoadGraph graph = RoadFixtures.TwoIslands(each: 3);

        graph.Segments.Create(
            graph.Nodes.Rows.At(2),
            graph.Nodes.Rows.At(3),
            new Tiles(64),
            RoadKind.Arterial,
            TravelMode.Car,
            TravelMode.Car);

        graph.RebuildDerived();

        Assert.Equal(1, graph.Connectivity.CarComponents);
        Assert.Equal(2, graph.Connectivity.FootComponents);
    }

    /// <summary>
    /// Removing the Segment that held two halves together splits the component.
    /// </summary>
    /// <remarks>
    /// <b>The direction that matters, and the one <c>adr/0012</c> is about.</b> Removal is the
    /// asymmetric case: adding a road can only improve a route and can be noticed lazily, while
    /// deleting one can invalidate anything and must be noticed at once. Connectivity is the coarsest
    /// consumer of that asymmetry, so it is the first place a rebuild that quietly stopped running
    /// would show.
    /// </remarks>
    [Fact]
    public void Cutting_a_chain_in_the_middle_makes_two_components()
    {
        RoadGraph graph = RoadFixtures.Chain(8);

        Assert.Equal(1, graph.Connectivity.CarComponents);

        graph.Segments.Rows.Free(graph.Segments.Rows.At(3));
        graph.RebuildDerived();

        Assert.Equal(2, graph.Connectivity.CarComponents);
        Assert.Equal([4, 4], Sizes(graph, graph.Nodes.CarComponent));
    }

    /// <summary>A freed node carries no label at all, rather than a stale one.</summary>
    /// <remarks>
    /// <see cref="RoadConnectivity.Unlabelled"/> rather than whatever the slot held before it was
    /// recycled. A stale label on a dead slot is the exact shape of bug that survives every count
    /// assertion in this file and then answers <em>yes, same network</em> about a node that no longer
    /// exists.
    /// </remarks>
    [Fact]
    public void A_freed_node_is_unlabelled()
    {
        RoadGraph graph = RoadFixtures.Chain(4);

        graph.Segments.Rows.Free(graph.Segments.Rows.At(2));
        graph.Nodes.Rows.Free(graph.Nodes.Rows.At(3));
        graph.RebuildDerived();

        Assert.Equal(RoadConnectivity.Unlabelled, graph.Nodes.CarComponent[3]);
        Assert.NotEqual(RoadConnectivity.Unlabelled, graph.Nodes.CarComponent[0]);
    }

    /// <summary>
    /// Labels are numbered by ascending slot, so the same graph labels the same way twice.
    /// </summary>
    /// <remarks>
    /// <b>Union-find's roots depend on the order the unions happened; a renumbering by first
    /// appearance does not.</b> The labels are <c>(derived AND rebuilt)</c> today and so fold into
    /// nothing, but a derived value that is not a function of the saved state is a trap for whoever
    /// promotes it to saved — and the promotion is exactly what a consumer wanting to serialise a
    /// Settlement id would reach for.
    /// </remarks>
    [Fact]
    public void The_first_island_gets_the_first_label()
    {
        RoadGraph graph = RoadFixtures.TwoIslands(each: 5);

        Assert.Equal(0, graph.Nodes.CarComponent[0]);
        Assert.Equal(1, graph.Nodes.CarComponent[5]);

        graph.RebuildDerived();

        Assert.Equal(0, graph.Nodes.CarComponent[0]);
        Assert.Equal(1, graph.Nodes.CarComponent[5]);
    }

    /// <summary>How many nodes each component holds, in label order.</summary>
    private static int[] Sizes(RoadGraph graph, Column<int> label)
    {
        var sizes = new int[graph.Connectivity.CarComponents];

        for (int slot = 0; slot < graph.Nodes.Rows.SlotCount; slot++)
        {
            if (graph.Nodes.Rows.IsLive(slot))
            {
                sizes[label[slot]]++;
            }
        }

        return sizes;
    }
}
