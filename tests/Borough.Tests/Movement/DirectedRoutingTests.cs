using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Space;

namespace Borough.Tests.Movement;

public sealed class DirectedRoutingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Costs_endpoints_and_exact_arcs_match_dijkstra_on_ties_one_way_and_diagonal_roads(int variant)
    {
        var graph = Grid(12, variant);
        var directed = new WalkScratch();
        var control = new WalkScratch { UseHeuristic = false };
        var random = new Random(741 + variant);
        for (int version = 0; version < 2; version++)
        {
            for (int i = 0; i < 2000; i++)
            {
                int a = random.Next(graph.Segments.Rows.SlotCount), b = random.Next(graph.Segments.Rows.SlotCount);
                var from = Address.On(a, new Tiles(random.Next(graph.Segments.LengthTiles[a].Raw + 1)), (StreetSide)(i % 2));
                var to = Address.On(b, new Tiles(random.Next(graph.Segments.LengthTiles[b].Raw + 1)), (StreetSide)((i / 2) % 2));
                var mode = i % 3 == 0 ? TravelMode.Foot : TravelMode.Car;
                var expected = WalkRouting.Cost(graph, mode, from, to, TravelTime.FromSeconds(30), control, true);
                Assert.Equal(expected, WalkRouting.Cost(graph, mode, from, to, TravelTime.FromSeconds(30), directed, true));
                Assert.Equal(control.Arrived, directed.Arrived);
                if (control.Arrived == WalkScratch.NoNode) { continue; }
                int count = control.ArcsTo(control.Arrived, []);
                var arcs = new int[count]; control.ArcsTo(control.Arrived, arcs);
                var actual = new int[directed.ArcsTo(directed.Arrived, [])]; directed.ArcsTo(directed.Arrived, actual);
                Assert.Equal(arcs, actual);
            }
            graph.Adopt(RoadFixtures.Roads(footPaths: 0) with { WalkSpeed = Speed.FromKilometresPerHour(3) });
        }
    }

    [Fact]
    public void Missing_targets_retain_the_exhaustive_unreachable_answer()
    {
        var graph = RoadFixtures.Chain(4);
        var scratch = new WalkScratch();
        scratch.Begin(graph.Nodes.Rows.SlotCount); scratch.Seed(0, TravelTime.Zero);
        Assert.Equal(TravelTime.Impassable, scratch.Search(graph, TravelMode.Foot,
            -1, int.MaxValue, TravelTime.Zero, TravelTime.Zero));
    }

    [Fact]
    public void Destination_direction_reduces_expansion_and_settle_all_still_clears_it()
    {
        var graph = Grid(32, 0);
        var fast = new WalkScratch(); var baseline = new WalkScratch { UseHeuristic = false };
        int target = 31 * 32 + 20;
        fast.Begin(graph.Nodes.Rows.SlotCount); fast.Seed(20, TravelTime.Zero);
        baseline.Begin(graph.Nodes.Rows.SlotCount); baseline.Seed(20, TravelTime.Zero);
        Assert.Equal(baseline.Search(graph, TravelMode.Car, target, target, TravelTime.Zero, TravelTime.Zero),
            fast.Search(graph, TravelMode.Car, target, target, TravelTime.Zero, TravelTime.Zero));
        Assert.True(fast.Relaxed * 4 < baseline.Relaxed, $"{fast.Relaxed} vs {baseline.Relaxed}");
        fast.Begin(graph.Nodes.Rows.SlotCount); fast.Seed(0, TravelTime.Zero); fast.SettleAll(graph, TravelMode.Car);
        baseline.Begin(graph.Nodes.Rows.SlotCount); baseline.Seed(0, TravelTime.Zero); baseline.SettleAll(graph, TravelMode.Car);
        for (int i = 0; i < graph.Nodes.Rows.SlotCount; i++) { Assert.Equal(baseline.CostTo(i), fast.CostTo(i)); }
    }

    private static RoadGraph Grid(int side, int variant)
    {
        var graph = new RoadGraph(RoadFixtures.Roads(footPaths: 0));
        var nodes = new Handle<RoadNode>[side * side];
        for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++) { nodes[y * side + x] = graph.Nodes.Create(new Tiles(x * 32), new Tiles(y * 32)); }
        for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                int n = y * side + x;
                var back = variant == 1 && n % 4 == 0 ? TravelMode.Foot : TravelMode.Any;
                int length = variant == 2 && n % 9 == 0 ? 0 : 32;
                if (x + 1 < side) { graph.Segments.Create(nodes[n], nodes[n + 1], new Tiles(length), RoadKind.Street, TravelMode.Any, back); }
                if (y + 1 < side) { graph.Segments.Create(nodes[n], nodes[n + side], new Tiles(32), RoadKind.Street, TravelMode.Any, back); }
                if (variant != 0 && x + 1 < side && y + 1 < side && n % 7 == 0)
                { graph.Segments.Create(nodes[n], nodes[n + side + 1], new Tiles(40), RoadKind.Street, TravelMode.Car, TravelMode.Car); }
            }
        graph.RebuildDerived(); return graph;
    }
}
