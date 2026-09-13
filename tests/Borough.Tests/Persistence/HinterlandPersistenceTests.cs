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
/// <para>
/// <b>The generic machinery already carries these tables and that is exactly why this file exists.</b>
/// <c>SaveFile</c> and <c>SaveHash</c> walk <c>World.Tables</c> and each table's saved columns, so the
/// four tables row 31 added went into the file the moment they were appended to the world. What no
/// generic walk can say is whether the world they come back as behaves like the one that was saved —
/// <c>FactorioTests</c> runs that comparison on <c>minimal</c> and <c>congested</c>, and neither states
/// <c>[immigration]</c>, so no world with anybody standing behind its edges had ever been resumed.
/// </para>
/// <para>
/// ⚠ <b>The state under test is deliberately awkward, and the fixture asserts that it is.</b> A save
/// taken over an idle Outside would round-trip whatever it was given: empty queues, whole fractions and
/// unspent quotas all restore correctly by holding still. Every element below is checked to be present
/// before the file is written, so a fixture that stops reaching one fails here rather than passing on
/// nothing.
/// </para>
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
    /// <b>A returned group that drains is what frees a row</b>, and a freed row is half of what this
    /// file is for — the shipped thirty-two Days would leave every slot this world ever allocated still
    /// live, and a reload cannot disagree about an allocator nothing has used.
    /// </remarks>
    private static Ruleset Quick() =>
        Parsed(
            Text("attracted.toml")
                .Replace("recovery_days         = 32", "recovery_days         = 1", StringComparison.Ordinal),
            "quick.toml");

    /// <summary>
    /// A world holding every piece of Outside state a save has to carry.
    /// </summary>
    /// <remarks>
    /// <b>Nothing here stages the awkwardness by writing a meter.</b> The doors are left alone and the
    /// authored quota does the work: four gates take ninety-six families a Day between them against an
    /// Outside that reconsiders every second Day, so the quota runs out, the queue fills, the stock
    /// falls below its resting count and recovery starts accruing against it.
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
    /// <remarks>
    /// ⚠ <b>The drained group is identified by its composition and not by its slot.</b> Its row is
    /// freed and handed straight back out to the next return, so the slot is live again and holding
    /// somebody else — which is the reuse this file wants in the save, and would read as a group that
    /// never drained.
    /// </remarks>
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
    /// <para>
    /// <b>The Factorio property, on the one kind of world it had never been asked of</b>
    /// (<c>plans/0073</c> D11). The hash is compared on every Tick of the resumed run rather than at the
    /// end, because a derived structure rebuilt wrongly can be overwritten by the next pass before any
    /// closing comparison sees it.
    /// </para>
    /// <para>
    /// <b>The second save sits six Ticks before a Day rolls</b>, so the resumed world is the one that
    /// performs the rollover — the flow counters move on in a world that was loaded rather than run
    /// into that Day.
    /// </para>
    /// <para>
    /// ⚠ <b>Both save points are inside the window where the doors are the constraint, and that window
    /// closes.</b> While the quota binds, willing families queue behind it; once the city is full
    /// enough that housing is the limit instead, prospects stay outside and the queue stands empty. A
    /// save taken later would be a save over an idle Outside, which is what <c>AssertAwkward</c> is
    /// there to refuse.
    /// </para>
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
    /// 🔴 <b>The one piece of Outside state a rebuild could not have recovered</b> (D11). Join order
    /// lives in the admission list and nowhere else, so a load that reconstructed the queue from live
    /// rows would come back in slot order and admit a different family — a defect a State Hash
    /// comparison would notice only once the two worlds had served somebody different.
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
    /// <remarks>
    /// <b>Column by column rather than by hash alone.</b> A hash says two worlds differ and never which
    /// figure moved, and these are the columns whose loss would be invisible for many Ticks — a cleared
    /// fraction only delays an occasion, and a reset sequence only repeats an identity.
    /// </remarks>
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
    /// <remarks>
    /// <b>Admitted families are what makes this worth asking</b> (D11). An arrival joins the Unplaced
    /// Pool at its gate and is housed by Placement, which is what puts move-in Trips through the router
    /// — so a stock world exercises the parallel path with journeys no other fixture here generates.
    /// </remarks>
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
    /// <b>A hand-built log cannot name a gate.</b> Gate Tiles exist only once the subdivider has run,
    /// so a log of <c>Arrive</c> commands would have to guess coordinates the generator decides —
    /// which is why <c>Populate</c> is the verb every replayed city in this project is built by.
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
    /// <para>
    /// <b>Replay equivalence had never been asked of a world anybody emigrates to</b>
    /// (<c>plans/0073</c> D11). <c>ReplayTests</c> runs under <c>Ruleset.Empty</c>, so every prior
    /// demonstration that a log reproduces its session was taken over a city with no Outside behind
    /// its edges.
    /// </para>
    /// <para>
    /// ⚠ <b>The two runs share nothing, which is what separates this from a reload.</b> Each builds
    /// its own World from the log's seed and capacity, so an admission decided from anything outside
    /// the counter hash — a slot number, an allocation order, a stale index — sends a different family
    /// through a door and the two traces part.
    /// </para>
    /// <para>
    /// The window crosses a Day boundary, so the doors refill and the Outside takes a reconsider
    /// occasion inside the Ticks being compared. <see cref="Admitted"/> is asserted nonzero because a
    /// trace over a stock engine that never ran would agree with itself perfectly.
    /// </para>
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
