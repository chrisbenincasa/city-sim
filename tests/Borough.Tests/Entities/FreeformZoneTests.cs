using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

public sealed class FreeformZoneTests
{
    private static Ruleset Rules() =>
        RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml")).Ruleset!;

    private static (World World, Simulation Simulation) Empty(params Command[] streets)
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(0, Rules(), key);
        var simulation = new Simulation(world, key);
        simulation.Step(new TickInput([new Command(CommandKind.Ground, default, default)], 0));
        foreach (Command street in streets) { simulation.Step(new TickInput([street], 0)); }
        return (world, simulation);
    }

    private static Command Street(int aEast, int aNorth, int bEast, int bNorth, int sagittaTiles = 0) =>
        Command.Street(new Tiles(aEast), new Tiles(aNorth), new Tiles(bEast), new Tiles(bNorth),
            new SubTiles(sagittaTiles * Fixed.One));

    private static Command Zone(int east, int north) => new(CommandKind.Zone, new Tiles(east), new Tiles(north), LotTable.Housing);

    private static readonly Command[] Triangle = [Street(100, 100, 164, 100), Street(164, 100, 100, 164), Street(100, 164, 100, 100)];

    [Fact]
    public void Zone_paints_the_triangle_holding_the_tile_and_its_turned_lots_read_that_paint()
    {
        (World world, Simulation simulation) = Empty(Triangle);
        Assert.True(world.TryZoneGround(new Tiles(110), new Tiles(110), out ZoneGround ground));
        Assert.True(ground.Face >= 0 && world.Roads.Faces.IsClosed(ground.Face));

        simulation.Step(new TickInput([Zone(110, 110)], 0));

        Assert.Equal(LotTable.Housing, world.LandPermissions.At(110, 110).Uses);
        Assert.Equal(LotTable.Housing, world.LandPermissions.At(130, 132).Uses);
        Assert.Equal(0, world.LandPermissions.At(140, 140).Uses);
        Assert.Equal(0, world.LandPermissions.At(90, 110).Uses);

        int[] inside = Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .Where(lot => world.Lots.Rows.IsLive(lot) && Within(ground, world.Lots.Parcel(lot))).ToArray();
        Assert.Contains(inside, lot => world.Lots.AxisNorthQ16[lot] != 0);
        Assert.All(inside, lot => Assert.Equal(LotTable.Housing, world.LotPermissions(lot).CommonUses));
        Assert.All(inside, lot => Assert.Equal(LotTable.Housing, world.Lots.Zone[lot]));
    }

    [Fact]
    public void Corner_plots_stop_at_the_adjacent_street_and_every_lot_is_wholly_painted()
    {
        (World world, Simulation simulation) = Empty(Triangle);
        simulation.Step(new TickInput([Zone(110, 110)], 0));

        int half = world.Rules.Lots.StreetHalfWidthTiles;
        int[] lots = Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive).ToArray();
        Assert.Equal(10, lots.Length);
        foreach (int lot in lots)
        {
            Assert.Equal(LotTable.Housing, world.LotPermissions(lot).CommonUses);
            OrientedRectangle parcel = world.Lots.Parcel(lot);
            int own = world.Lots.FrontageOn(lot);
            for (int segment = 0; segment < world.Roads.Segments.Rows.SlotCount; segment++)
            {
                if (segment == own || !world.Roads.Segments.Rows.IsLive(segment)) { continue; }
                StreetArc line = world.Roads.Segments.Centerline[segment];
                for (int column = 0; column < parcel.Wide; column++)
                    for (int row = 0; row < parcel.Deep; row++)
                    {
                        long east = parcel.EastQ16 + IntegerMath.FloorDiv((2L * column + 1) * parcel.AxisEastQ16 - (2L * row + 1) * parcel.AxisNorthQ16, 2);
                        long north = parcel.NorthQ16 + IntegerMath.FloorDiv((2L * column + 1) * parcel.AxisNorthQ16 + (2L * row + 1) * parcel.AxisEastQ16, 2);
                        Assert.True(line.DistanceTo(east, north) > half * Fixed.One, $"Lot {lot} reaches Segment {segment}.");
                    }
            }
        }
    }

    private static bool Within(ZoneGround ground, OrientedRectangle parcel)
    {
        var tiles = new OrientedTiles(parcel);
        LandRectangle box = tiles.Bounds;
        for (int y = box.Y; y < box.Y + box.Height; y++)
            for (int x = box.X; x < box.X + box.Width; x++)
                if (tiles.Contains(x, y) && !ground.Contains(x, y)) { return false; }
        return true;
    }

    [Fact]
    public void Zone_beside_an_open_curve_paints_only_that_side()
    {
        (World world, Simulation simulation) = Empty(Street(300, 100, 364, 100, 6));
        Assert.True(world.TryZoneGround(new Tiles(332), new Tiles(112), out ZoneGround ground));
        Assert.Equal(-1, ground.Face);
        Assert.Equal(StreetSide.Left, ground.Side);

        simulation.Step(new TickInput([Zone(332, 112)], 0));

        Assert.Equal(LotTable.Housing, world.LandPermissions.At(332, 112).Uses);
        Assert.Equal(0, world.LandPermissions.At(332, 96).Uses);
        int[] lots = Enumerable.Range(0, world.Lots.Rows.SlotCount).Where(world.Lots.Rows.IsLive).ToArray();
        Assert.NotEmpty(lots);
        Assert.All(lots, lot => Assert.Equal((byte)StreetSide.Left, world.Lots.Side[lot]));
        Assert.All(lots, lot => Assert.True(Within(ground, world.Lots.Parcel(lot)), $"Lot {lot} reaches past the painted side."));
        Assert.All(lots, lot => Assert.Equal(LotTable.Housing, world.LotPermissions(lot).CommonUses));
    }

    [Fact]
    public void Ground_beyond_every_streets_reach_is_refused_by_name()
    {
        (World world, Simulation simulation) = Empty(Street(300, 100, 364, 100));

        // minimal.toml: a one-Tile half width plus a back-to-back plot half of the 32-Tile block.
        const int reach = 1 + 16;
        Assert.Equal(reach, LotSubdivider.SideReachTiles(world));

        Assert.False(world.TryZoneGround(new Tiles(332), new Tiles(100 + reach + 1), out _));
        Assert.Equal(Refusal.ZoneNoStreet, simulation.Refuses(Zone(332, 100 + reach + 1)));
        Assert.Equal(Refusal.None, simulation.Refuses(Zone(332, 100 + reach - 2)));
    }

    [Fact]
    public void Asking_for_zone_ground_writes_nothing()
    {
        (World world, Simulation simulation) = Empty([.. Triangle, Street(300, 100, 364, 100, 6)]);
        ulong before = world.HashState();

        world.TryZoneGround(new Tiles(110), new Tiles(110), out _);
        world.TryZoneGround(new Tiles(332), new Tiles(112), out _);
        world.TryZoneGround(new Tiles(2_000), new Tiles(2_000), out _);
        simulation.Refuses(Zone(110, 110));
        simulation.Refuses(Zone(332, 112));

        Assert.Equal(before, world.HashState());
    }

    [Fact]
    public void A_turned_parcel_reads_only_its_own_tiles()
    {
        var permissions = new LandPermissions(new LandPermissionTable());
        int axis = Fixed.FromInt(3) / 5, across = Fixed.FromInt(4) / 5;
        var parcel = new OrientedRectangle(Fixed.FromInt(20), Fixed.FromInt(20), axis, across, 10, 6);
        Assert.Equal(PermissionRefusal.None, permissions.PaintUses(new OrientedTiles(parcel), LotTable.Housing, 1_024));

        Assert.Equal(PermissionRefusal.None, permissions.CheckParcel(parcel, LotTable.Housing, 1, out _));
        Assert.Equal(PermissionRefusal.Unzoned, permissions.Check(parcel.Bounds, LotTable.Housing, 1, out _));
        Assert.Equal(LotTable.Housing, permissions.ParcelSummary(parcel).CommonUses);
        Assert.Equal(0, permissions.Summary(parcel.Bounds).CommonUses);
    }
}
