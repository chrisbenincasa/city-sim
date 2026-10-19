using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Tests.Space;

namespace Borough.Tests.Movement;

public sealed class JobFloodTests
{
    [Fact]
    public void Flood_matches_point_search_at_every_door_and_crossing()
    {
        RoadGraph graph = RoadFixtures.Chain(12);
        Address from = Address.On(1, new Tiles(11), StreetSide.Left);
        var flood = new WalkScratch();
        TravelTime budget = TravelTime.FromTicks(100);
        TravelTime crossing = TravelTime.FromTicks(1);
        Assert.True(WalkRouting.Flood(graph, TravelMode.Foot, from, budget, flood));
        for (int segment = 0; segment < 11; segment++)
            for (int offset = 0; offset <= 32; offset += 4)
                foreach (StreetSide side in new[] { StreetSide.Left, StreetSide.Right })
                {
                    Address to = Address.On(segment, new Tiles(offset), side);
                    Assert.Equal(
                        WalkRouting.Cost(graph, TravelMode.Foot, from, to, crossing, new WalkScratch()),
                        WalkRouting.CostFromFlood(graph, TravelMode.Foot, from, to, crossing, flood));
                }
    }

    [Fact]
    public void Flood_does_not_reach_a_disconnected_job()
    {
        RoadGraph graph = RoadFixtures.TwoIslands(4);
        var flood = new WalkScratch();
        Address from = Address.On(0, new Tiles(4), StreetSide.Left);
        Address to = Address.On(4, new Tiles(4), StreetSide.Left);
        Assert.True(WalkRouting.Flood(graph, TravelMode.Foot, from, TravelTime.FromTicks(100), flood));
        Assert.Equal(TravelTime.Impassable, WalkRouting.CostFromFlood(graph, TravelMode.Foot,
            from, to, TravelTime.Zero, flood));
    }

    [Fact]
    public void Bounded_flood_keeps_exact_boundary_and_does_not_settle_distant_nodes()
    {
        RoadGraph graph = RoadFixtures.Chain(100);
        Address from = Address.On(0, new Tiles(4), StreetSide.Left);
        Address to = Address.On(2, new Tiles(20), StreetSide.Right);
        TravelTime exact = WalkRouting.Cost(graph, TravelMode.Foot, from, to,
            TravelTime.Zero, new WalkScratch());
        var flood = new WalkScratch();
        Assert.True(WalkRouting.Flood(graph, TravelMode.Foot, from, exact, flood));
        Assert.Equal(exact, WalkRouting.CostFromFlood(graph, TravelMode.Foot, from, to,
            TravelTime.Zero, flood));
        Assert.InRange(flood.Relaxed, 1, 5);
        Address far = Address.On(98, new Tiles(20), StreetSide.Left);
        Assert.True(WalkRouting.CostFromFlood(graph, TravelMode.Foot, from, far,
            TravelTime.Zero, flood) > exact);
    }
}
