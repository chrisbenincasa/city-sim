using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Rules;
using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Persistence;

public sealed class SaveCapacityTests
{
    [Fact]
    public void An_active_city_restores_movement_headroom_without_changing_save_bytes_or_future_state()
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(1);
        var world = new World(4000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        var continuous = new Simulation(world, key);
        for (int i = 0; i < 600; i++) { continuous.Step(default); }
        var save = new MemorySave();
        SaveFile.Write(world, 0, save);
        var restored = SaveFile.Read(save, loaded.Ruleset!, out _);
        var rewritten = new MemorySave();
        SaveFile.Write(restored, 0, rewritten);
        Assert.Equal(save.Bytes, rewritten.Bytes);
        Assert.Equal(world.Trips.Rows.Capacity, restored.Trips.Rows.Capacity);
        Assert.Equal(world.Travellers.Rows.Capacity, restored.Travellers.Rows.Capacity);
        Assert.Equal(world.Legs.Rows.Capacity, restored.Legs.Rows.Capacity);
        Assert.Equal(world.RouteHops.Rows.Capacity, restored.RouteHops.Rows.Capacity);
        Assert.True(world.RouteHops.Rows.Capacity > world.RouteHops.Rows.SlotCount);
        Assert.Equal(restored.Citizens.Rows.SlotCount, restored.Citizens.Rows.Capacity);
        var resumed = new Simulation(restored, key);
        for (int i = 0; i < 1024; i++)
        {
            continuous.Step(default); resumed.Step(default);
            Assert.Equal(world.HashState(), restored.HashState());
        }
        continuous.CheckEndOfRun(); resumed.CheckEndOfRun();
    }

    [Fact]
    public void Reload_keeps_the_growth_headroom_of_transient_route_storage()
    {
        var world = new World(0, Ruleset.Empty, WorldKey.FromSeed(0));
        for (int i = 0; i < 65; i++) { world.RouteHops.Rows.Allocate(); }
        var save = new MemorySave();
        SaveFile.Write(world, 0, save);
        var restored = SaveFile.Read(save, Ruleset.Empty, out _);
        Assert.Equal(world.HashState(), restored.HashState());
        int originalCapacity = world.RouteHops.Rows.Capacity;
        int loadedCapacity = restored.RouteHops.Rows.Capacity;
        for (int i = 65; i < 96; i++)
        {
            Assert.Equal(world.RouteHops.Rows.Allocate(), restored.RouteHops.Rows.Allocate());
            Assert.Equal(world.HashState(), restored.HashState());
        }
        Assert.Equal(originalCapacity, world.RouteHops.Rows.Capacity);
        Assert.True(restored.RouteHops.Rows.Capacity == loadedCapacity,
            $"Reload grew from {loadedCapacity} to {restored.RouteHops.Rows.Capacity}; uninterrupted capacity {originalCapacity} already held the same rows.");
    }
}
