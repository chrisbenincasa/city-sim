using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// A freeform Street lay: exact joins, crossing splits, the <c>[roads]</c> minimums, sealing and the
/// Addresses a split carries along. <c>minimal.toml</c> gives a 12-Tile shortest Segment, a 30°
/// crossing and a 16-Tile radius.
/// </summary>
public sealed class FreeformLayTests
{
    private static World Fresh()
    {
        RulesetLoadResult result = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        return new World(1_000, result.Ruleset ?? throw new InvalidOperationException(result.Describe()));
    }

    private static List<int> Live(RoadGraph roads)
    {
        var live = new List<int>();
        for (int slot = 0; slot < roads.Segments.Rows.SlotCount; slot++)
        {
            if (roads.Segments.Rows.IsLive(slot)) { live.Add(slot); }
        }

        return live;
    }

    private static (int East, int North) End(RoadGraph roads, Handle<RoadNode> node)
    {
        int slot = roads.Nodes.Rows.Resolve(node);
        return (roads.Nodes.East[slot].Raw, roads.Nodes.North[slot].Raw);
    }

    private static int Degree(RoadGraph roads, int east, int north) =>
        Live(roads).Count(s => End(roads, roads.Segments.NodeA[s]) == (east, north) || End(roads, roads.Segments.NodeB[s]) == (east, north));

    [Fact]
    public void A_lone_street_lays_one_segment_and_seals_its_ground()
    {
        World world = Fresh();
        Cells east = CellGrid.ToCellsClamped(new Tiles(1_050)), north = CellGrid.ToCellsClamped(new Tiles(1_000));
        int before = world.Layers.Sealing(east, north);

        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_000, 1_000, 1_100, 1_000, 0));

        int segment = Assert.Single(Live(world.Roads));
        Assert.Equal(100, world.Roads.Segments.LengthTiles[segment].Raw);
        Assert.True(world.Layers.Sealing(east, north) > before);
    }

    [Fact]
    public void A_crossing_splits_both_streets_at_one_shared_node()
    {
        World world = Fresh();
        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_000, 1_000, 1_100, 1_000, 0));

        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_050, 950, 1_050, 1_050, 0));

        Assert.Equal(4, Live(world.Roads).Count);
        Assert.Equal(4, Degree(world.Roads, 1_050, 1_000));
    }

    [Fact]
    public void An_end_on_a_street_splits_it_and_an_end_on_a_node_reuses_it()
    {
        World world = Fresh();
        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_000, 1_000, 1_100, 1_000, 0));

        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_040, 1_000, 1_040, 1_060, 0));
        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_100, 1_000, 1_150, 1_050, 0));

        Assert.Equal(4, Live(world.Roads).Count);
        Assert.Equal(3, Degree(world.Roads, 1_040, 1_000));
        Assert.Equal(2, Degree(world.Roads, 1_100, 1_000));
    }

    [Fact]
    public void A_curve_crossing_a_street_twice_cuts_it_in_three_and_moves_frontage_past_each_cut()
    {
        World world = Fresh();
        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_000, 1_000, 1_100, 1_000, 0));
        int street = Assert.Single(Live(world.Roads));
        int lot = world.Lots.Rows.Resolve(world.Lots.Create(new Tiles(1_090), new Tiles(1_000), LotTable.Housing, StreetSide.Left));
        world.Lots.Front(lot, world.Roads.Segments.Rows.At(street), new Tiles(90));

        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_000, 990, 1_100, 990, 20 * Fixed.One));

        int fronting = world.Lots.FrontageOn(lot);
        Assert.NotEqual(street, fronting);
        StreetArc piece = world.Roads.Segments.Centerline[fronting];
        var at = piece.PointAt(world.Lots.FrontageOffset[lot].Raw * Fixed.One);
        Assert.InRange(at.East - (1_090L * Fixed.One), -2L * Fixed.One, 2L * Fixed.One);
        Assert.Equal(3, Live(world.Roads).Count(s => world.Roads.Segments.Centerline[s].IsStraight));
    }

    [Theory]
    [InlineData(1_000, 1_000, 1_000, 1_000, 0, StreetLayRefusal.NotAnArc)]
    [InlineData(-5, 1_000, 1_000, 1_000, 0, StreetLayRefusal.OffMap)]
    [InlineData(1_000, 1_100, 1_020, 1_100, 4, StreetLayRefusal.TooTight)]
    [InlineData(1_005, 950, 1_005, 1_050, 0, StreetLayRefusal.TooShort)]
    [InlineData(1_000, 990, 1_100, 1_008, 0, StreetLayRefusal.TooShallow)]
    [InlineData(1_000, 1_000, 1_100, 1_000, 0, StreetLayRefusal.TooShallow)]
    public void A_refused_lay_changes_nothing(int aE, int aN, int bE, int bN, int sagittaTiles, StreetLayRefusal expected)
    {
        World world = Fresh();
        Assert.Equal(StreetLayRefusal.None, world.LayStreet(1_000, 1_000, 1_100, 1_000, 0));
        int nodes = world.Roads.Nodes.Rows.SlotCount;

        Assert.Equal(expected, world.LayStreet(aE, aN, bE, bN, sagittaTiles * Fixed.One));

        Assert.Single(Live(world.Roads));
        Assert.Equal(nodes, world.Roads.Nodes.Rows.SlotCount);
    }
}
