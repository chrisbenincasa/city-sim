using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/exported.toml</c>: mills and grocers sell what they cannot store to the Hinterlands.
/// East pays 1600 for flour with a budget of 16000 a Day; north pays 1400 with 64000.
/// </summary>
public sealed class ExportTests
{
    private const long EastBudget = 16000;
    private const long NorthBudget = 64000;
    private const long LargestBatch = 4 * 4 * 1600;

    private static Ruleset Rules()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "exported.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        return loaded.Ruleset!;
    }

    private static (World World, Simulation Sim) Start(Ruleset? rules = null)
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(400, rules ?? Rules(), key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        return (world, sim);
    }

    private static long Today(World world, MapEdge edge) =>
        world.Hinterlands.ExportedToday[HinterlandTable.SlotOf(edge)].Raw;

    private static long Yesterday(World world, MapEdge edge) =>
        world.Hinterlands.ExportedYesterday[HinterlandTable.SlotOf(edge)].Raw;

    private static long Exported(World world) => Today(world, MapEdge.North) + Today(world, MapEdge.East);

    /// <summary>Steps until a Hinterland has bought something, holding both budgets on every Tick.</summary>
    private static bool StepUntilExport(World world, Simulation sim, int days)
    {
        for (int t = 0; t < days * Ticks.PerDay; t++)
        {
            sim.Step(default);
            AssertWithinBudget(world);

            if (Exported(world) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertWithinBudget(World world)
    {
        Assert.InRange(Today(world, MapEdge.East), 0, EastBudget);
        Assert.InRange(Today(world, MapEdge.North), 0, NorthBudget);
    }

    private static List<(Handle<Lot> Lot, byte Kind)> RazeGates(World world, params MapEdge[] edges)
    {
        var razed = new List<(Handle<Lot>, byte)>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot])
                && edges.Contains(world.EdgeOf(world.Lots.Rows.Resolve(world.Buildings.Lot[slot]))))
            {
                razed.Add((world.Buildings.Lot[slot], world.Buildings.Kind[slot]));
                world.DestroyBuilding(world.Buildings.Rows.At(slot), world.Tick);
            }
        }

        return razed;
    }

    [Fact]
    public void Firms_export_what_they_cannot_store_to_the_highest_payer_first()
    {
        var (world, sim) = Start();
        sim.VerifyDecideWritesNothing = true;

        Assert.True(StepUntilExport(world, sim, 28), "no firm ever exported");
        Assert.True(Today(world, MapEdge.East) > 0, "the first export did not go to the higher payer");
        Assert.Equal(0, Today(world, MapEdge.North));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_spent_budget_passes_exports_to_the_next_payer_and_recovers_the_next_day()
    {
        var (world, sim) = Start();
        Assert.True(StepUntilExport(world, sim, 28), "no firm ever exported");

        bool eastSpent = false;
        bool northBoughtAfterEast = false;
        bool eastRecovered = false;

        for (int t = 0; t < 7 * Ticks.PerDay; t++)
        {
            long north = Today(world, MapEdge.North);
            int day = (int)(world.Tick.Raw / (ulong)Ticks.PerDay);

            sim.Step(default);
            AssertWithinBudget(world);

            bool newDay = (int)(world.Tick.Raw / (ulong)Ticks.PerDay) != day;

            if (newDay && eastSpent && Yesterday(world, MapEdge.East) > EastBudget - LargestBatch)
            {
                eastRecovered |= StepUntilEast(world, sim);
            }

            if (!newDay && Today(world, MapEdge.North) > north)
            {
                Assert.True(
                    EastBudget - Today(world, MapEdge.East) < LargestBatch,
                    "north bought while east could still pay for a batch");
                northBoughtAfterEast = true;
            }

            eastSpent |= EastBudget - Today(world, MapEdge.East) < LargestBatch;
        }

        Assert.True(eastSpent, "east's budget never ran low");
        Assert.True(northBoughtAfterEast, "north never bought once east was spent");
        Assert.True(eastRecovered, "east bought nothing on the Day after its budget ran out");
        sim.CheckEndOfRun();
    }

    private static bool StepUntilEast(World world, Simulation sim)
    {
        for (int t = 0; t < Ticks.PerDay - 1; t++)
        {
            if (Today(world, MapEdge.East) > 0)
            {
                return true;
            }

            sim.Step(default);
            AssertWithinBudget(world);
        }

        return Today(world, MapEdge.East) > 0;
    }

    [Fact]
    public void Nothing_is_exported_without_a_gate_and_a_new_gate_wakes_the_exporters()
    {
        var (world, sim) = Start();
        var gates = RazeGates(world, MapEdge.North, MapEdge.East);

        Assert.False(StepUntilExport(world, sim, 21), "a Hinterland bought with no gate standing");

        (Handle<Lot> lot, byte kind) = gates.First(g =>
            world.EdgeOf(world.Lots.Rows.Resolve(g.Lot)) == MapEdge.East);
        world.CreateBuilding(lot, kind, world.Tick, WorldKey.FromSeed(0));

        Assert.True(StepUntilExport(world, sim, 1), "a new gate did not wake a full firm");
        Assert.True(Today(world, MapEdge.East) > 0);
        Assert.Equal(0, Today(world, MapEdge.North));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void Exports_survive_save_and_reload()
    {
        Ruleset rules = Rules();
        var (world, sim) = Start(rules);
        Assert.True(StepUntilExport(world, sim, 28), "no firm ever exported");

        var file = new MemorySave();
        sim.SaveAtEndOfTick(file);
        sim.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key);

        Assert.Equal(world.HashState(), reloaded.HashState());

        for (int t = 0; t < 2 * Ticks.PerDay; t++)
        {
            sim.Step(default);
            resumed.Step(default);
        }

        Assert.Equal(world.HashState(), reloaded.HashState());
        sim.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    [Fact]
    public void A_replayed_run_reaches_the_same_state()
    {
        Ruleset rules = Rules();

        ulong Run()
        {
            var (world, sim) = Start(rules);
            Assert.True(StepUntilExport(world, sim, 28), "no firm ever exported");

            for (int t = 0; t < Ticks.PerDay; t++)
            {
                sim.Step(default);
            }

            sim.CheckEndOfRun();
            return world.HashState();
        }

        Assert.Equal(Run(), Run());
    }
}
