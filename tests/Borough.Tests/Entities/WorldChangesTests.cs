using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

public sealed class WorldChangesTests
{
    [Fact]
    public void Replacing_a_building_in_the_same_slot_is_reported_without_moving_the_hash()
    {
        var observed = new World(32) { Changes = new WorldChanges() };
        var plain = new World(32);
        Assert.True(observed.Changes.Full);
        observed.Changes.Clear();
        Handle<Building> first = Build(observed);
        Handle<Building> other = Build(plain);
        int slot = observed.Buildings.Rows.Resolve(first);
        Assert.Equal(new[] { slot }, observed.Changes.Buildings.ToArray());
        Assert.Equal(plain.HashState(), observed.HashState());
        observed.Changes.Clear();
        observed.DestroyBuilding(first, new Ticks(1));
        plain.DestroyBuilding(other, new Ticks(1));
        Handle<Building> replacement = Build(observed);
        Build(plain);
        Assert.Equal(slot, observed.Buildings.Rows.Resolve(replacement));
        Assert.NotEqual(first, replacement);
        Assert.Equal(new[] { slot }, observed.Changes.Buildings.ToArray());
        Assert.Equal(plain.HashState(), observed.HashState());
        observed.Changes.Clear();
        Assert.Empty(observed.Changes.Buildings.ToArray());
    }

    [Fact]
    public void Occupancy_and_abandonment_report_the_building_with_no_population_scan()
    {
        var world = new World(32) { Changes = new WorldChanges() };
        Handle<Building> building = Build(world);
        int slot = world.Buildings.Rows.Resolve(building);
        world.Changes.Clear();
        Handle<Household> household = world.CreateHousehold(building, 0);
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
        world.Changes.Clear();
        world.DestroyHousehold(household);
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
        world.Changes.Clear();
        world.AbandonBuilding(building, new Ticks(1));
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
    }

    [Fact]
    public void Letting_and_vacating_a_Unit_report_its_building()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "pictured.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(2_000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.Changes = new WorldChanges();
        world.Changes.Clear();

        (int centre, int tenant) = FirstLetCentreUnit(world);
        world.DestroyBusiness(world.Businesses.Rows.At(tenant));
        Assert.Contains(centre, world.Changes.Buildings.ToArray());

        world.Changes.Clear();
        Assert.True(world.OpenInVacantUnit(centre));
        Assert.Contains(centre, world.Changes.Buildings.ToArray());
    }

    private static (int Centre, int Tenant) FirstLetCentreUnit(World world)
    {
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.IsTradeCentre(building)) continue;
            foreach (int unit in world.BuildingUnits.Walk(building))
            {
                int tenant = world.Units.TenantSlot(unit);
                if (tenant >= 0 && world.Units.Anchor[unit] == 0) return (building, tenant);
            }
        }

        throw new InvalidOperationException("pictured.toml raised no centre with a let Unit.");
    }

    private static Handle<Building> Build(World world)
    {
        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(1), 1);
        return world.CreateBuilding(lot, 1, Ticks.Zero, WorldKey.FromSeed(1));
    }
}
