using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Persistence;

/// <summary>
/// <c>plans/0045</c> row 31 task 6: a counted Outside saved in the middle of everything it does, and
/// the same city on the far side of the load.
/// </summary>
/// <remarks>
/// Assert nonempty queues, partial fractions, spent quota and reusable slots before saving.
/// Compare continued runs, not just serialized values.
/// </remarks>
public sealed class HinterlandPersistenceTests(ITestOutputHelper output)
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    private readonly ITestOutputHelper _output = output;

    private static string Text(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    private static Ruleset Parsed(string text, string name)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, name);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>The shipped stock world, recovering fast enough to empty a group inside one run.</summary>
    /// <remarks>
    /// Short recovery frees return-only slots; narrow gates retain a queue at the save checkpoints.
    /// </remarks>
    private static Ruleset Quick() =>
        Parsed(
            Text("attracted.toml")
                .Replace("recovery_days         = 32", "recovery_days         = 1", StringComparison.Ordinal)
                .Replace("arrivals_per_day = 96", "arrivals_per_day = 24", StringComparison.Ordinal),
            "quick.toml");

    /// <summary>
    /// A world holding every piece of Outside state a save has to carry.
    /// </summary>
    /// <remarks>
    /// Reach queues and spent quota through ordinary simulation flow rather than staging their
    /// counters.
    /// </remarks>
    private static (World World, Simulation Simulation, HinterlandComposition Drained) Awkward(
        Ruleset rules, int ticks)
    {
        var world = new World(1_000, rules, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key);

        HinterlandComposition young = HinterlandComposition.Of(rules.HinterlandPopulations[0]);
        HinterlandComposition drained = young with { Children = 5 };

        world.ReturnToHinterland(MapEdge.West, drained);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        world.ReturnToHinterland(MapEdge.North, young with { Children = 3 });

        return (world, simulation, drained);
    }

    /// <summary>What the fixture promises the file is being written over.</summary>
    private void AssertAwkward(World world, in HinterlandComposition drained)
    {
        long sequence = 0;
        long admitted = 0;
        long unauthored = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            sequence += (long)world.Hinterlands.Sequence[edge];
            admitted += world.Hinterlands.AdmittedHouseholds[edge];
        }

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot)
                && world.HinterlandPopulation.Authored[slot] == 0)
            {
                unauthored++;
            }
        }

        _output.WriteLine(
            $"tick {world.Tick.Raw}: {world.HinterlandQueue.Rows.LiveCount} waiting, {unauthored} "
            + $"returned group(s), {admitted} admitted, sequence {sequence}, {SpentGates(world)} "
            + $"spent door(s), reconsider {Any(world, world.HinterlandPopulation.ReconsiderNumerator)}, "
            + $"recovery {Any(world, world.HinterlandPopulation.RecoveryNumerator)}, "
            + $"reserved {Any(world, world.HinterlandPopulation.Reserved)}.");

        Assert.False(
            world.HinterlandCompositions.TryFind(
                world.HinterlandPopulation, MapEdge.West, drained, out _),
            "the group that was supposed to drain is still standing, so no row was ever freed.");

        Assert.True(Any(world, world.HinterlandPopulation.ReconsiderNumerator));
        Assert.True(Any(world, world.HinterlandPopulation.RecoveryNumerator));
        Assert.True(world.HinterlandQueue.Rows.LiveCount > 0, "nobody is waiting.");
        Assert.True(Any(world, world.HinterlandPopulation.Reserved), "nothing is reserved.");

        Assert.True(sequence > 0, "no prospect was ever sequenced.");
        Assert.True(admitted > 0, "nobody crossed, so no quota was spent.");
        Assert.True(unauthored > 0, "no group the Ruleset does not declare is standing.");
        Assert.True(SpentGates(world) > 0, "no door has taken anybody today.");
    }

    private static bool Any(World world, Column<long> column)
    {
        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot) && column[slot] != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Any(World world, Column<int> column)
    {
        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot) && column[slot] != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How many doors have taken somebody on the Day the world is standing in.</summary>
    private static int SpentGates(World world)
    {
        int spent = 0;
        int today = (int)(world.Tick.Raw / Ticks.PerDay);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot])
                && world.Buildings.ArrivalDay[slot] == today
                && world.Buildings.ArrivalsToday[slot] > 0)
            {
                spent++;
            }
        }

        return spent;
    }

    /// <summary>Every waiting family, edge by edge, in the order the queue would serve them.</summary>
    private static List<ulong> Waiting(World world)
    {
        var order = new List<ulong>();
        LinkedIndexList admissions = world.HinterlandQueue.Admissions(world.Hinterlands);

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            foreach (int slot in admissions.Walk(edge))
            {
                order.Add(world.HinterlandQueue.Identity[slot]);
            }
        }

        return order;
    }

    /// <summary>A stock world saved mid-Day runs on exactly as the world that never stopped.</summary>
    /// <remarks>
    /// Keep the built-in MixedPattern ranking: an artificial pattern can change when floor units
    /// are derived after construction and confound the save/reload comparison.
    /// </remarks>
    [Theory]
    [InlineData(4_000, 96)]
    [InlineData(4_090, 64)]
    public void A_counted_Outside_saved_mid_Day_runs_on_identically(int n, int m)
    {
        Ruleset rules = Quick();

        (World control, Simulation controlSimulation, _) = Awkward(rules, n);
        (World subject, Simulation subjectSimulation, HinterlandComposition drained) =
            Awkward(rules, n);

        AssertAwkward(subject, drained);

        Assert.NotEqual(0UL, subject.Tick.Raw % Ticks.PerDay);

        List<ulong> queue = Waiting(subject);

        var file = new MemorySave();
        subjectSimulation.SaveAtEndOfTick(file);
        subjectSimulation.Step(default);
        controlSimulation.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key);

        Assert.Equal(control.HashState(), reloaded.HashState());

        for (int tick = 0; tick < m; tick++)
        {
            controlSimulation.Step(default);
            resumed.Step(default);

            Assert.Equal(
                (object)$"tick {n + 1 + tick}: {control.HashState():X16}",
                $"tick {n + 1 + tick}: {reloaded.HashState():X16}");
        }

        reloaded.Invariants.RunEndOfRun(reloaded);

        _output.WriteLine($"saved at {n}, ran on {m}, {file.Bytes.Length:N0} B, queue of {queue.Count}.");
    }

    /// <summary>A reloaded queue serves the families in the order it joined them.</summary>
    /// <remarks>
    /// Join order must survive slot reuse; rebuilding the queue in slot order changes admission order.
    /// </remarks>
    [Fact]
    public void A_reloaded_queue_holds_the_order_it_was_saved_in()
    {
        Ruleset rules = Quick();

        (World world, Simulation simulation, HinterlandComposition drained) = Awkward(rules, 4_000);

        AssertAwkward(world, drained);

        List<ulong> before = Waiting(world);

        Assert.True(before.Count > 1, "one waiting family cannot demonstrate an order.");

        var file = new MemorySave();
        SaveFile.Write(world, InForce, file);

        World reloaded = SaveFile.Read(file, rules, out _);

        Assert.Equal(before, Waiting(reloaded));
        Assert.Equal(world.HashState(), reloaded.HashState());

        reloaded.Invariants.RunEndOfRun(reloaded);
    }

    /// <summary>The Outside's fractions, sequences and returned groups come back as they went in.</summary>
    [Fact]
    public void The_Outside_comes_back_at_the_figures_it_was_saved_at()
    {
        Ruleset rules = Quick();

        (World world, Simulation simulation, HinterlandComposition drained) = Awkward(rules, 4_000);

        AssertAwkward(world, drained);

        var file = new MemorySave();
        SaveFile.Write(world, InForce, file);

        World reloaded = SaveFile.Read(file, rules, out _);

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            Assert.Equal(world.Hinterlands.Sequence[edge], reloaded.Hinterlands.Sequence[edge]);
            Assert.Equal(
                world.Hinterlands.AdmittedHouseholds[edge], reloaded.Hinterlands.AdmittedHouseholds[edge]);
            Assert.Equal(
                world.Hinterlands.RequestedToday[edge], reloaded.Hinterlands.RequestedToday[edge]);
        }

        Assert.Equal(
            world.HinterlandPopulation.Rows.LiveCount, reloaded.HinterlandPopulation.Rows.LiveCount);

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            Assert.Equal(
                world.HinterlandPopulation.Rows.IsLive(slot),
                reloaded.HinterlandPopulation.Rows.IsLive(slot));

            if (!world.HinterlandPopulation.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.Equal(world.HinterlandPopulation.Stock[slot], reloaded.HinterlandPopulation.Stock[slot]);
            Assert.Equal(
                world.HinterlandPopulation.Reserved[slot], reloaded.HinterlandPopulation.Reserved[slot]);
            Assert.Equal(
                world.HinterlandPopulation.Authored[slot], reloaded.HinterlandPopulation.Authored[slot]);
            Assert.Equal(
                world.HinterlandPopulation.ReconsiderNumerator[slot],
                reloaded.HinterlandPopulation.ReconsiderNumerator[slot]);
            Assert.Equal(
                world.HinterlandPopulation.RecoveryNumerator[slot],
                reloaded.HinterlandPopulation.RecoveryNumerator[slot]);
            Assert.Equal(
                world.HinterlandPopulation.RecoveryDirection[slot],
                reloaded.HinterlandPopulation.RecoveryDirection[slot]);

            Assert.True(
                reloaded.HinterlandCompositions.TryFind(
                    reloaded.HinterlandPopulation,
                    (MapEdge)world.HinterlandPopulation.Edge[slot],
                    world.HinterlandPopulation.CompositionAt(slot),
                    out int found),
                "a group came back that its own rebuilt index cannot find.");

            Assert.Equal(slot, found);
        }
    }

    /// <summary>A resumed stock world is the same city at one routing worker and at eight.</summary>
    [Fact]
    public void A_resumed_stock_world_agrees_across_worker_counts()
    {
        Ruleset rules = Quick();

        (World control, Simulation controlSimulation, HinterlandComposition drained) =
            Awkward(rules, 4_000);

        AssertAwkward(control, drained);

        var file = new MemorySave();
        SaveFile.Write(control, InForce, file);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { RouteWorkerCount = 8 };

        controlSimulation.RouteWorkerCount = 1;

        Assert.Equal(control.HashState(), reloaded.HashState());

        long admitted = control.PopulationLedger.Admissions[PopulationLedgerTable.Slot];

        for (int tick = 0; tick < 256; tick++)
        {
            if (tick == 128)
            {
                controlSimulation.RouteWorkerCount = 8;
                resumed.RouteWorkerCount = 1;
            }

            controlSimulation.Step(default);
            resumed.Step(default);

            Assert.Equal(control.HashState(), reloaded.HashState());
        }

        Assert.True(
            control.PopulationLedger.Admissions[PopulationLedgerTable.Slot] > admitted,
            "nobody was admitted while the worker counts differed, so no move-in Trip was routed.");

        controlSimulation.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    /// <summary>A session whose whole content is the verb that lays a city down.</summary>
    /// <remarks>
    /// Use normal Populate input so replay owns the founding state.
    /// </remarks>
    private static InputLog Populated()
    {
        InputLogBuilder builder = new(seed: 7, new WorldConfiguration(1_000), rulesetHash: 0);

        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));

        return builder.Build();
    }

    private static (ulong[] Trace, World World) Replayed(InputLog log, Ruleset rules, Ticks ticks)
    {
        Simulation simulation = Replay.Start(log, rules);
        var trace = new List<ulong>();

        Replay.Trace(simulation, log, ticks, hashEvery: 1, trace);

        return ([.. trace], simulation.World);
    }

    private static long Admitted(World world)
    {
        long admitted = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            admitted += world.Hinterlands.AdmittedHouseholds[edge];
        }

        return admitted;
    }

    /// <summary>A session replayed into a counted Outside reproduces itself Tick for Tick.</summary>
    /// <remarks>
    /// Use the same synthetic/world seed for replay. Mismatched keys compare different worlds.
    /// </remarks>
    [Fact]
    public void A_replayed_session_builds_the_same_stock_world_twice()
    {
        Ruleset rules = Quick();
        InputLog log = Populated();

        (ulong[] first, World left) = Replayed(log, rules, new Ticks(2_100));
        (ulong[] second, World right) = Replayed(log, rules, new Ticks(2_100));

        Assert.Equal(first, second);
        Assert.NotEqual(first[0], first[^1]);

        long admitted = Admitted(left);

        Assert.True(admitted > 0, "nobody crossed, so the replay never reached the stock engine.");
        Assert.Equal(admitted, Admitted(right));

        left.Invariants.RunEndOfRun(left);
        right.Invariants.RunEndOfRun(right);

        _output.WriteLine($"{first.Length} hashes, {admitted} admitted, ending 0x{first[^1]:X16}.");
    }
}
