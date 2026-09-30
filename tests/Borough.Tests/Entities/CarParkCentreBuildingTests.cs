using Borough.Appearance;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

public sealed class CarParkCentreBuildingTests
{
    private const byte Shopfront = 1;
    private const byte Trade = 1;
    private const ushort AnyZone = 1;
    private const int BlockTiles = 32;
    private const int RowWide = BlockTiles - 2;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x0C3A_7C3A_0000_0002UL);

    private static Ruleset Centres() =>
        new(
            resources: [],
            rules: [],
            kinds:
            [
                new KindDefinition(0, 0, 0, 0) { Houses = true, Premises = true, Business = Trade },
            ],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: [])
        {
            Capacity = new CapacityRuleset(25, 1, 12),
            BusinessKindCount = 1,
            Lots = new LotRuleset(5, 2, CarParkCentres: true),
            Parking = new ParkingRuleset(400, 24, new StallSizes(250, 500, 600)),
        };

    private static (World World, int Building) Raised()
    {
        var world = new World(1_000, Centres());

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), AnyZone, wide: new Tiles(BlockTiles), deep: new Tiles(BlockTiles));
        int slot = world.Lots.Rows.Resolve(lot);
        var parcel = new Parcel(
            BlockFace.South, StreetSide.Left, Tiles.Zero, new Tiles(0), new Tiles(0),
            new Tiles(BlockTiles), new Tiles(BlockTiles));
        var foot = CarParkCentre.Footprint(parcel, BlockGround.Square(BlockTiles), 1);

        world.Lots.FootprintEast[slot] = foot.East;
        world.Lots.FootprintNorth[slot] = foot.North;
        world.Lots.FootprintWide[slot] = foot.Wide;
        world.Lots.FootprintDeep[slot] = foot.Deep;
        world.Lots.Storeys[slot] = 1;
        world.Lots.Pattern[slot] = (byte)((byte)BlockPattern.CarParkCentre + 1);

        Handle<Building> building = world.CreateBuilding(lot, Shopfront, Ticks.Zero, Key);

        return (world, world.Buildings.Rows.Resolve(building));
    }

    private static List<int> UnitsOf(World world, int building)
    {
        var units = new List<int>();

        foreach (int unit in world.BuildingUnits.Walk(building))
        {
            units.Add(unit);
        }

        return units;
    }

    [Fact]
    public void A_centre_raises_a_row_of_units_that_tiles_its_footprint()
    {
        (World world, int building) = Raised();

        List<int> units = UnitsOf(world, building);

        Assert.Equal(CarParkCentre.UnitCount(RowWide), units.Count);
        Assert.Single(units, unit => world.Units.Anchor[unit] == 1);

        int east = 0;
        int floor = 0;

        foreach (int unit in units.OrderBy(unit => world.Units.East[unit].Raw))
        {
            Assert.Equal(east, world.Units.East[unit].Raw);
            Assert.Equal(CarParkCentre.RowDepthTiles, world.Units.Deep[unit].Raw);
            Assert.Equal((byte)BlockFace.South, world.Units.Side[unit]);
            Assert.Equal(world.Units.Wide[unit].Raw * CarParkCentre.RowDepthTiles, world.Units.Floor[unit]);
            east += world.Units.Wide[unit].Raw;
            floor += world.Units.Floor[unit];
        }

        Assert.Equal(RowWide, east);
        Assert.Equal(world.FloorTilesOf(building), floor);
    }

    [Fact]
    public void A_centre_houses_nobody()
    {
        (World world, int building) = Raised();

        Assert.False(world.HasRoomForHousehold(building));
    }

    [Fact]
    public void A_centre_lets_every_unit_and_no_more()
    {
        (World world, int building) = Raised();

        int units = UnitsOf(world, building).Count;

        for (int let = 1; let < units; let++)
        {
            Assert.True(world.HasRoomForPremises(building));
            world.CreateBusiness(world.Buildings.Rows.At(building), Trade);
        }

        Assert.False(world.HasRoomForPremises(building));
        Assert.All(UnitsOf(world, building), unit => Assert.False(world.Units.IsVacant(unit)));
    }

    [Fact]
    public void A_centre_business_has_the_posts_of_its_unit()
    {
        (World world, int building) = Raised();

        var businesses = new List<int>();

        foreach (int held in world.BuildingBusinesses.Walk(building))
        {
            businesses.Add(held);
        }

        int business = Assert.Single(businesses);

        Assert.True(world.TryDeclaredJobs(Trade, business, out int posts));
        Assert.Equal(world.UnitFloorOf(business), posts);
        Assert.True(posts > 0);
    }

    [Fact]
    public void A_centre_has_one_car_park_of_its_stall_count_whatever_its_kind_says()
    {
        (World world, int building) = Raised();

        Assert.True(world.Buildings.HasCarPark(building));
        Assert.Equal(new StallLayout(7, 36).Stalls, world.CarParks.Capacity[world.Buildings.CarParkOf(building)]);
    }

    [Fact]
    public void Filling_a_centre_lets_every_vacant_unit_to_a_business_it_originates()
    {
        (World world, int building) = Raised();

        int units = UnitsOf(world, building).Count;

        Assert.Equal(units - 1, world.FillUnits(building));
        Assert.All(UnitsOf(world, building), unit => Assert.False(world.Units.IsVacant(unit)));

        foreach (int business in world.BuildingBusinesses.Walk(building))
        {
            Assert.Equal(world.Buildings.Rows.At(building), world.Businesses.Origin[business]);
        }

        Assert.Equal(0, world.FillUnits(building));
    }

    [Fact]
    public void Pictured_raises_every_centre_full_of_businesses_at_world_creation()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "pictured.toml"));
        Assert.True(loaded.Ok, loaded.Describe());

        var world = new World(1_000, loaded.Ruleset!, Key);
        SyntheticCity.PopulateInto(world, Key, Ticks.Zero, 200);

        int centres = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || world.Lots.PatternOf(slot) != BlockPattern.CarParkCentre)
            {
                continue;
            }

            Assert.False(world.Lots.IsVacant(slot), $"centre Lot {slot} stands unbuilt.");

            int building = world.Lots.BuildingOn(slot);

            Assert.All(UnitsOf(world, building), unit => Assert.False(world.Units.IsVacant(unit)));
            Assert.All(UnitsOf(world, building), unit =>
            {
                Assert.True(UnitLiveFacts.TryOf(world, unit, out UnitLiveFacts live));
                Assert.True(live.Let);
            });
            Assert.True(BuildingFacts.IsCorner(world.Roads.Streets.Lattice, world.Lots, slot));
            centres++;
        }

        Assert.True(centres > 0, "pictured.toml laid no car-park centre.");
    }

    [Fact]
    public void A_centre_reports_its_units_anchor_and_surface_car_park_to_the_drawing()
    {
        (World world, int building) = Raised();

        Assert.True(BuildingFacts.TryOf(world, RulesetNames.None, building, out BuildingFacts facts));
        Assert.Equal(BlockPattern.CarParkCentre, facts.Pattern);
        Assert.Equal(CarParkCentre.UnitCount(RowWide), facts.Units);
        Assert.True(facts.Anchored);
        Assert.Equal(ParkingForm.Surface, facts.Parking);
    }

    [Fact]
    public void A_vacant_unit_is_neither_let_nor_open()
    {
        (World world, int building) = Raised();

        List<int> vacant = UnitsOf(world, building).FindAll(world.Units.IsVacant);
        Assert.NotEmpty(vacant);

        Assert.All(vacant, unit =>
        {
            Assert.True(UnitLiveFacts.TryOf(world, unit, out UnitLiveFacts live));
            Assert.Equal(world.Units.Rows.IdAt(unit), live.Id);
            Assert.False(live.Let);
            Assert.False(live.Open);
        });
    }

    [Theory]
    [InlineData(0, 0, BlockTiles, BlockTiles, true)]
    [InlineData(0, 0, 8, 10, true)]
    [InlineData(24, 22, 8, 10, true)]
    [InlineData(8, 0, 8, 10, false)]
    [InlineData(0, 10, 8, 10, false)]
    [InlineData(BlockTiles, 0, 8, 10, true)]
    [InlineData(BlockTiles + 8, BlockTiles, 8, 10, false)]
    public void A_parcel_is_a_corner_when_it_reaches_two_perpendicular_block_edges(
        int east, int north, int wide, int deep, bool corner)
    {
        var world = new World(1_000, Centres());
        int slot = world.Lots.Rows.Resolve(world.Lots.Create(new Tiles(east), new Tiles(north), AnyZone));

        world.Lots.ParcelEast[slot] = new Tiles(east);
        world.Lots.ParcelNorth[slot] = new Tiles(north);
        world.Lots.ParcelWide[slot] = new Tiles(wide);
        world.Lots.ParcelDeep[slot] = new Tiles(deep);

        Assert.Equal(corner, BuildingFacts.IsCorner(BlockLattice.Even(BlockTiles), world.Lots, slot));
    }

    [Fact]
    public void The_stalls_placed_on_the_centre_car_park_are_its_capacity()
    {
        (World world, int building) = Raised();

        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
        (_, _, int along, int toward) = CarParkCentre.CarPark(
            world.Lots.ParcelNorth[lot].Raw, world.Lots.FootprintEast[lot].Raw, world.Lots.FootprintNorth[lot].Raw,
            world.Lots.FootprintWide[lot].Raw, world.Rules.Lots.StreetHalfWidthTiles);

        int placed = StallLayout.Place(along, toward, world.Rules.Parking.Stalls, new Stall[1_000]);

        Assert.Equal(world.CarParks.Capacity[world.Buildings.CarParkOf(building)], placed);
    }

    [Fact]
    public void A_rebuild_keeps_the_centre_row_and_its_tenants()
    {
        (World world, int building) = Raised();

        List<int> before = UnitsOf(world, building);
        int tenant = world.Units.TenantSlot(before[0]);

        world.RebuildDerived();

        Assert.Equal(before, UnitsOf(world, building));
        Assert.Equal(tenant, world.Units.TenantSlot(before[0]));
    }
}
