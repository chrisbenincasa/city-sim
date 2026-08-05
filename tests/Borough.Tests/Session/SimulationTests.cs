using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Tests.Session;

/// <summary>
/// <c>step(inputs)</c>: the Tick counter, Phase 0's command application, and the guard that keeps
/// Phase 2 honest.
/// </summary>
public sealed class SimulationTests
{
    private const int Population = 64;

    [Fact]
    public void A_new_simulation_starts_at_tick_zero() =>
        Assert.Equal(0UL, Build().Tick.Raw);

    [Fact]
    public void Each_step_advances_the_tick_by_exactly_one()
    {
        Simulation simulation = Build();

        for (ulong expected = 1; expected <= 16; expected++)
        {
            simulation.Step(TickInput.Empty);
            Assert.Equal(expected, simulation.Tick.Raw);
        }
    }

    [Fact]
    public void A_tick_with_no_commands_changes_nothing()
    {
        Simulation simulation = Build();
        ulong before = simulation.World.HashState();

        simulation.Step(TickInput.Empty);

        Assert.Equal(before, simulation.World.HashState());
    }

    [Fact]
    public void A_zone_command_paints_a_lot()
    {
        Simulation simulation = Build();
        Command zone = Paint(east: 12, north: 8, permissions: 1);

        simulation.Step(new TickInput([zone], rulesetHash: 0));

        Assert.Equal(1, simulation.World.Lots.Rows.LiveCount);
        Assert.Equal(new Tiles(12), simulation.World.Lots.East[0]);
        Assert.Equal(new Tiles(8), simulation.World.Lots.North[0]);
    }

    [Fact]
    public void Commands_in_one_tick_are_applied_in_issue_order()
    {
        Simulation simulation = Build();

        simulation.Step(new TickInput(
            [Paint(east: 1, north: 0, permissions: 1), Paint(east: 2, north: 0, permissions: 1)],
            rulesetHash: 0));

        Assert.Equal(new Tiles(1), simulation.World.Lots.East[0]);
        Assert.Equal(new Tiles(2), simulation.World.Lots.East[1]);
    }

    /// <summary>
    /// A verb the slice has not built throws rather than being skipped: a run that silently drops a
    /// command diverges from the log describing it while reporting success.
    /// </summary>
    [Theory]
    [InlineData(CommandKind.None)]
    [InlineData(CommandKind.Connect)]
    [InlineData(CommandKind.Service)]
    [InlineData(CommandKind.Govern)]
    public void An_unapplied_verb_throws_rather_than_being_skipped(CommandKind kind)
    {
        Simulation simulation = Build();
        var command = new Command(kind, new Tiles(0), new Tiles(0));

        Assert.Throws<InvalidOperationException>(() =>
            simulation.Step(new TickInput([command], rulesetHash: 0)));
    }

    /// <summary>
    /// <b>Why the Decide guard folds storage rather than the State Hash.</b> A write to a derived
    /// column is invisible to the State Hash <em>by declaration</em> — that is what
    /// <c>(derived AND rebuilt)</c> means — so a read-only check built on the State Hash would wave
    /// through exactly the write a Rule evaluating in Decide is most likely to make: caching something.
    /// This is slice 4's blind spot read from the other side.
    /// </summary>
    [Fact]
    public void The_decide_guard_sees_a_write_the_state_hash_cannot()
    {
        World world = Populate();

        ulong hashBefore = world.HashState();
        ulong storageBefore = FoldEverything(world);

        // A derived column: the intrusive link from a Citizen to the next member of its Household.
        world.Citizens.MemberNext[0] += 1;

        Assert.Equal(hashBefore, world.HashState());
        Assert.NotEqual(storageBefore, FoldEverything(world));
    }

    [Fact]
    public void The_decide_guard_is_on_by_default() =>
        Assert.True(Build().VerifyDecideWritesNothing);

    /// <summary>
    /// The guard is a runtime switch rather than a build configuration, so that a release long-run can
    /// turn it off for speed and a release correctness run can leave it on. <c>02 §10</c>'s rule is
    /// that invariants sort by frequency, never by build.
    /// </summary>
    [Fact]
    public void The_decide_guard_can_be_turned_off_without_changing_the_result()
    {
        Simulation guarded = Build();
        Simulation unguarded = Build();
        unguarded.VerifyDecideWritesNothing = false;

        for (int i = 0; i < 8; i++)
        {
            guarded.Step(TickInput.Empty);
            unguarded.Step(TickInput.Empty);
        }

        Assert.Equal(guarded.World.HashState(), unguarded.World.HashState());
    }

    [Fact]
    public void The_phase_after_a_step_is_the_last_one()
    {
        Simulation simulation = Build();
        simulation.Step(TickInput.Empty);

        Assert.Equal(TickPhase.Commit, simulation.Phase);
    }

    private static Command Paint(int east, int north, ushort permissions) =>
        new(CommandKind.Zone, new Tiles(east), new Tiles(north), permissions);

    private static Simulation Build() =>
        new(new World(Population), WorldKey.FromSeed(0xD0D0_CACA_0000_0001UL));

    private static ulong FoldEverything(World world)
    {
        ulong hash = 0;

        foreach (Rows table in world.Tables)
        {
            table.FoldAll(ref hash);
        }

        return hash;
    }

    /// <summary>A world with one of everything, so that every table has a row to disturb.</summary>
    private static World Populate()
    {
        var world = new World(Population);

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> building = world.Buildings.Create(lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);

        world.CreateCitizen(household, new Ticks(4));
        world.CreateCitizen(household, new Ticks(9));

        return world;
    }
}
