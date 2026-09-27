using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Entities;

public sealed class UnitTests
{
    private const byte Shopfront = 1;
    private const byte Trade = 1;
    private const ushort AnyZone = 1;

    private const int FloorPerOccupant = 4;
    private const int FloorTiles = 12;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x0417_5EED_0000_0001UL);

    private static Ruleset Trading(int floorPerOccupant = FloorPerOccupant, int floorPerJob = 1) =>
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
            Capacity = new CapacityRuleset(floorPerOccupant, floorPerJob, 0),
            BusinessKindCount = 1,
        };

    private static (World World, int Building) Raised(Ruleset? rules = null)
    {
        var world = new World(1_000, rules ?? Trading());

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), AnyZone, wide: new Tiles(FloorTiles), deep: new Tiles(1));
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

    private static int UnitSlotOf(World world, int business) =>
        world.Units.Rows.TryResolve(world.Businesses.Unit[business], out int unit) ? unit : Rows.NoSlot;

    [Fact]
    public void An_existing_kind_holds_one_equal_unit_per_tenancy()
    {
        (World world, int building) = Raised();

        List<int> units = UnitsOf(world, building);

        Assert.Equal(FloorTiles / FloorPerOccupant, units.Count);
        Assert.All(units, unit => Assert.Equal(FloorPerOccupant, world.Units.Floor[unit]));
        Assert.All(
            units,
            unit => Assert.Equal(world.Buildings.Rows.At(building), world.Units.Building[unit]));
    }

    [Fact]
    public void The_declared_trade_takes_a_unit_when_the_building_is_raised()
    {
        (World world, int building) = Raised();

        int business = Assert.Single(Walk(world, building));
        int unit = UnitSlotOf(world, business);

        Assert.Contains(unit, UnitsOf(world, building));
        Assert.Equal(business, world.Units.TenantSlot(unit));
    }

    [Fact]
    public void Posts_are_the_units_floor_over_the_job_rate()
    {
        (World world, int building) = Raised(Trading(floorPerJob: 2));

        int business = Assert.Single(Walk(world, building));

        Assert.True(world.TryDeclaredJobs(Trade, business, out int posts));
        Assert.Equal(FloorPerOccupant / 2, posts);
    }

    [Fact]
    public void Losing_premises_vacates_the_unit_and_taking_them_lets_a_free_one()
    {
        (World world, int building) = Raised();

        int business = Assert.Single(Walk(world, building));
        int unit = UnitSlotOf(world, business);
        Handle<Business> trader = world.Businesses.Rows.At(business);

        world.Unpremise(trader, Ticks.Zero);

        Assert.Equal(Rows.NoSlot, world.Units.TenantSlot(unit));
        Assert.True(world.Businesses.Unit[business].IsNone);

        world.Premise(trader, world.Buildings.Rows.At(building));

        int taken = UnitSlotOf(world, business);

        Assert.Contains(taken, UnitsOf(world, building));
        Assert.Equal(business, world.Units.TenantSlot(taken));
    }

    [Fact]
    public void Two_businesses_never_share_a_unit()
    {
        (World world, int building) = Raised();

        world.CreateBusiness(world.Buildings.Rows.At(building), Trade);

        var held = new HashSet<int>();

        foreach (int business in Walk(world, building))
        {
            Assert.True(held.Add(UnitSlotOf(world, business)));
        }

        Assert.Equal(2, held.Count);
    }

    [Fact]
    public void A_premised_business_needs_a_vacant_unit()
    {
        (World world, int building) = Raised();

        for (int i = 1; i < FloorTiles / FloorPerOccupant; i++)
        {
            Assert.True(world.HasRoomForPremises(building));
            world.CreateBusiness(world.Buildings.Rows.At(building), Trade);
        }

        Assert.False(world.HasRoomForPremises(building));
    }

    [Fact]
    public void Demolition_frees_every_unit()
    {
        (World world, int building) = Raised();

        List<int> units = UnitsOf(world, building);

        world.DestroyBuilding(world.Buildings.Rows.At(building), Ticks.Zero);

        Assert.All(units, unit => Assert.False(world.Units.Rows.IsLive(unit)));
    }

    [Fact]
    public void A_lowered_ceiling_shrinks_the_units_and_keeps_every_tenant_on_one()
    {
        (World world, int building) = Raised();

        world.CreateBusiness(world.Buildings.Rows.At(building), Trade);
        world.CreateBusiness(world.Buildings.Rows.At(building), Trade);

        world.Adopt(Trading(floorPerOccupant: 6), 2, new Ticks(64), Key);

        List<int> units = UnitsOf(world, building);

        Assert.Equal(FloorTiles / 6, units.Count);
        Assert.All(units, unit => Assert.Equal(6, world.Units.Floor[unit]));

        foreach (int business in Walk(world, building))
        {
            Assert.Contains(UnitSlotOf(world, business), units);
        }
    }

    [Fact]
    public void A_rebuild_restores_which_business_holds_which_unit()
    {
        (World world, int building) = Raised();

        world.CreateBusiness(world.Buildings.Rows.At(building), Trade);

        List<int> units = UnitsOf(world, building);
        int[] tenants = [.. units.Select(world.Units.TenantSlot)];

        world.RebuildDerived();

        Assert.Equal(units, UnitsOf(world, building));
        Assert.Equal(tenants, units.Select(world.Units.TenantSlot));
    }

    private static List<int> Walk(World world, int building)
    {
        var businesses = new List<int>();

        foreach (int business in world.BuildingBusinesses.Walk(building))
        {
            businesses.Add(business);
        }

        return businesses;
    }
}
