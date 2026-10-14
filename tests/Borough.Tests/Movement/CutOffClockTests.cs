using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Formats;
using Borough.Tests.Persistence;

using static Borough.Tests.Movement.BridgeCity;

namespace Borough.Tests.Movement;

/// <summary>
/// A Building no road joins to an Outside Connection is abandoned at its kind's
/// <c>abandoned_after_cut_off_days</c> deadline (<c>adr/0177</c>).
/// </summary>
public sealed class CutOffClockTests
{
    private const int DeadlineDays = 2;

    [Fact]
    public void Removing_the_bridge_starts_every_far_dwelling_clock_at_the_edit()
    {
        (Simulation simulation, long removedAt) = Severed(DeadlineDays);
        World world = simulation.World;

        int far = 0;

        foreach (int building in Dwellings(world))
        {
            Ticks since = world.Buildings.CutOffSince[building];

            if (IsFar(world, building))
            {
                Assert.Equal((ulong)removedAt + 1, since.Raw);
                Assert.True(world.IsCutOff(building), $"Building {building} reads as reachable.");
                far++;
            }
            else
            {
                Assert.Equal(Ticks.Zero, since);
            }
        }

        Assert.True(far > 0, "the fixture has no far-side dwelling to cut off.");
        Assert.Equal(Ticks.Zero, world.Buildings.CutOffSince[Gate(world)]);
    }

    [Fact]
    public void Rebuilding_the_bridge_clears_every_clock_and_abandons_nothing()
    {
        (Simulation simulation, _) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, LateEvening(2));
        RebuildBridge(simulation);
        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int building in Dwellings(world))
        {
            Assert.Equal(Ticks.Zero, world.Buildings.CutOffSince[building]);
            Assert.False(world.Buildings.IsAbandoned(building), $"Building {building} was abandoned after the repair.");
        }
    }

    [Fact]
    public void The_deadline_abandons_every_far_dwelling_for_being_cut_off()
    {
        (Simulation simulation, _) = Severed(DeadlineDays);
        World world = simulation.World;

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        int far = 0;

        foreach (int building in Dwellings(world))
        {
            if (IsFar(world, building))
            {
                Assert.True(world.Buildings.IsAbandoned(building), $"Building {building} still stands in use.");
                Assert.Equal((byte)AbandonmentCause.CutOff, world.Buildings.AbandonedCause[building]);
                Assert.Equal(Ticks.Zero, world.Buildings.CutOffSince[building]);
                far++;
            }
            else
            {
                Assert.NotEqual((byte)AbandonmentCause.CutOff, world.Buildings.AbandonedCause[building]);
            }
        }

        Assert.True(far > 0, "the fixture has no far-side dwelling to abandon.");
    }

    [Fact]
    public void Without_the_key_the_clock_never_runs()
    {
        (Simulation simulation, _) = Severed(cutOffDays: null);
        World world = simulation.World;

        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        foreach (int building in Dwellings(world))
        {
            Assert.Equal(Ticks.Zero, world.Buildings.CutOffSince[building]);
            Assert.NotEqual((byte)AbandonmentCause.CutOff, world.Buildings.AbandonedCause[building]);
        }
    }

    [Fact]
    public void A_world_with_no_Outside_Connection_runs_no_clock()
    {
        Simulation simulation = Start(BridgeRules(cutOffDays: DeadlineDays));
        World world = simulation.World;

        StepTo(simulation, LateEvening(1));
        while (world.HasOutsideConnection())
        {
            world.DestroyBuilding(world.Buildings.Rows.At(Gate(world)), world.Tick);
        }

        RemoveBridge(simulation);
        StepTo(simulation, (2 + DeadlineDays + 2) * Ticks.PerDay);

        Assert.False(world.HasOutsideConnection());

        foreach (int building in Dwellings(world))
        {
            Assert.Equal(Ticks.Zero, world.Buildings.CutOffSince[building]);
            Assert.NotEqual((byte)AbandonmentCause.CutOff, world.Buildings.AbandonedCause[building]);
        }
    }

    [Fact]
    public void A_running_clock_survives_save_and_reload()
    {
        var rules = BridgeRules(cutOffDays: DeadlineDays);
        (Simulation simulation, _) = Severed(DeadlineDays, rules);
        World world = simulation.World;

        StepTo(simulation, (2 * Ticks.PerDay) + 1);

        var file = new MemorySave();
        simulation.SaveAtEndOfTick(file);
        simulation.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        Assert.Equal(world.HashState(), reloaded.HashState());

        long end = (2 + DeadlineDays + 2) * Ticks.PerDay;

        StepTo(simulation, end);
        StepTo(resumed, end);

        Assert.Equal(world.HashState(), reloaded.HashState());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65_536)]
    public void The_loader_refuses_a_deadline_outside_one_to_65535_days(int days)
    {
        RulesetLoadResult result = RulesetLoader.Parse(BridgeToml(cutOffDays: days), "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("abandoned_after_cut_off_days", result.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void The_loader_requires_a_collapse_beside_the_deadline()
    {
        string toml = BridgeToml(cutOffDays: DeadlineDays)
            .Replace("collapses_after_days = 30\n", "", StringComparison.Ordinal);

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("collapses_after_days", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>The bridge removed at 23:00 on Day 1.</summary>
    private static (Simulation Simulation, long RemovedAt) Severed(
        int? cutOffDays, Core.Rules.Ruleset? rules = null)
    {
        Simulation simulation = Start(rules ?? BridgeRules(cutOffDays: cutOffDays));

        StepTo(simulation, LateEvening(1));

        long removedAt = (long)simulation.World.Tick.Raw;

        RemoveBridge(simulation);

        return (simulation, removedAt);
    }

    private static List<int> Dwellings(World world)
    {
        List<int> dwellings = [];

        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (world.Buildings.Rows.IsLive(building) && !world.IsOutsideConnection(world.Buildings.Kind[building]))
            {
                dwellings.Add(building);
            }
        }

        return dwellings;
    }
}
