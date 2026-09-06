using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

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

    private static Handle<Building> Build(World world)
    {
        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(1), 1);
        return world.CreateBuilding(lot, 1, Ticks.Zero, WorldKey.FromSeed(1));
    }
}
