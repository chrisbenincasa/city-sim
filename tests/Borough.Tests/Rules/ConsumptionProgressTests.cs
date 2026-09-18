using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Tests.Persistence;

namespace Borough.Tests.Rules;

/// <summary>
/// A daily consumption quantity spread across firings that do not divide it evenly.
/// </summary>
/// <remarks>
/// <b>The quantity a designer states is per Day and the Rule fires eight times a Day, so the only
/// two answers available without a remainder are both wrong.</b> Rounding each firing down takes
/// nothing at all for any quantity under eight; rounding up takes eight times the Day's amount. The
/// remainder on the Bin is what makes three a Day mean three.
/// </remarks>
public sealed class ConsumptionProgressTests
{
    private static readonly ResourceId Grain = new(1);

    private const byte Larder = 1;

    /// <summary>Eight firings a Day, which is the count that makes three a Day awkward.</summary>
    private const uint Rate = Ticks.PerDay / 8;

    private static Ruleset Eating(int perDay) => new(
        resources: [ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(
                Larder, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default, ConditionId.None,
                0, 1, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Grain), perDay) { PerDay = true }],
        outputs: [],
        emissions: [],
        bins: [new BinDeclaration(Grain, BinCapacity.Of(1_000))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    private static (World World, Simulation Simulation, Handle<Building> Building) Built(
        Ruleset ruleset, long stock)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(1))
        {
            VerifyDecideWritesNothing = true,
        };

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Larder);

        foreach (BinDeclaration bin in ruleset.BinsOf(Larder))
        {
            world.CreateBin(building, bin.Resource);
        }

        foreach (RuleId rule in ruleset.RulesOf(Larder))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay: 1);
        }

        if (stock > 0)
        {
            world.Deposit(world.Bins.Rows.At(BinOf(world, building)), stock, Ticks.Zero);
        }

        return (world, simulation, building);
    }

    private static int BinOf(World world, Handle<Building> building) =>
        world.FindBin(world.Buildings.Rows.Resolve(building), Grain);

    private static long Level(World world, Handle<Building> building) =>
        world.Bins.LevelAt(BinOf(world, building));

    private static int Progress(World world, Handle<Building> building) =>
        world.Bins.Progress[BinOf(world, building)];

    private static void Step(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>To the end of the Tick the helper arms for, and then through <paramref name="more"/>.</summary>
    private static void StepThroughFirings(Simulation simulation, int more) =>
        Step(simulation, 2 + ((int)Rate * (more - 1)));

    [Fact]
    public void Three_a_day_over_a_day_of_firings_takes_three()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(3), 100);

        StepThroughFirings(simulation, 8);

        Assert.Equal(97, Level(world, building));
    }

    /// <summary>Eight Days of it, so a drift of one unit a Day would be eight units out.</summary>
    [Fact]
    public void A_daily_quantity_does_not_drift_over_many_days()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(3), 100);

        StepThroughFirings(simulation, 64);

        Assert.Equal(76, Level(world, building));
    }

    /// <summary>
    /// A quantity that divides evenly leaves no remainder at all, so the literal form and the daily
    /// form are the same Rule.
    /// </summary>
    [Fact]
    public void A_quantity_that_divides_evenly_leaves_nothing_behind()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(8), 100);

        StepThroughFirings(simulation, 8);

        Assert.Equal(92, Level(world, building));
        Assert.Equal(0, Progress(world, building));
    }

    /// <summary>
    /// <c>3 × 256 = 768</c> of the <c>2048</c> a unit costs, so the first firing owes nothing yet.
    /// </summary>
    [Fact]
    public void A_firing_below_a_whole_unit_succeeds_and_moves_nothing()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(3), 100);

        StepThroughFirings(simulation, 1);

        Assert.Equal(100, Level(world, building));
        Assert.Equal(768, Progress(world, building));
        Assert.False(world.RuleInstances.IsWaiting(0));
    }

    /// <summary>
    /// <b>A starved actor accrues nothing while it waits.</b> Firings one and two take no whole unit
    /// and so cannot be short; the third owes one, finds an empty Bin and blocks, and its remainder
    /// stays where the second firing left it for as long as the shortage lasts.
    /// </summary>
    [Fact]
    public void A_blocked_firing_accrues_nothing_while_it_waits()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(3), 0);

        StepThroughFirings(simulation, 3);

        Assert.True(world.RuleInstances.IsWaiting(0));
        Assert.Equal(1_536, Progress(world, building));

        Step(simulation, (int)Rate * 4);

        Assert.Equal(1_536, Progress(world, building));
    }

    /// <summary>
    /// <b>And does not catch up on recovery.</b> A restocked Bin gives up the one unit the blocked
    /// firing owed, not the four Days of quantity that went by while it waited.
    /// </summary>
    [Fact]
    public void A_starved_actor_does_not_catch_up_when_supply_returns()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(3), 0);

        StepThroughFirings(simulation, 3);
        Step(simulation, (int)Rate * 4);

        world.Deposit(world.Bins.Rows.At(BinOf(world, building)), 100, simulation.Tick);

        Assert.False(world.RuleInstances.IsWaiting(0));

        Step(simulation, 2);

        Assert.Equal(99, Level(world, building));
        Assert.Equal(256, Progress(world, building));
    }

    /// <summary>
    /// A world saved part way through a unit resumes on the same schedule, rather than losing the
    /// fraction or starting the unit again.
    /// </summary>
    [Fact]
    public void A_part_accrued_unit_survives_save_and_reload()
    {
        Ruleset rules = Eating(3);

        (World world, Simulation simulation, Handle<Building> building) = Built(rules, 100);

        StepThroughFirings(simulation, 2);

        Assert.Equal(1_536, Progress(world, building));

        var file = new MemorySave();
        SaveFile.Write(world, 0, file);

        World reloaded = SaveFile.Read(file, rules, out _);

        Assert.Equal(1_536, Progress(reloaded, building));
        Assert.Equal(world.HashState(), reloaded.HashState());
    }

    /// <summary>A recycled Bin opens empty of the fraction as well as of the Goods.</summary>
    [Fact]
    public void A_recycled_bin_does_not_inherit_a_part_accrued_unit()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Eating(3), 100);

        StepThroughFirings(simulation, 2);

        Assert.Equal(1_536, Progress(world, building));

        int bin = BinOf(world, building);

        world.DestroyBuilding(building, simulation.Tick);

        Handle<Lot> lot = world.Lots.Create(new Tiles(4), new Tiles(5), zone: 1);
        Handle<Building> raised = world.Buildings.Create(world.Lots, lot, Larder);

        world.CreateBin(raised, Grain);

        Assert.Equal(bin, BinOf(world, raised));
        Assert.Equal(0, Progress(world, raised));
    }

    /// <summary>
    /// A daily quantity is spent once per firing, so a Rule free to apply twice would take twice the
    /// Day's amount while the remainder advanced once.
    /// </summary>
    [Fact]
    public void A_per_day_term_under_a_band_is_refused()
    {
        var greedy = new Ruleset(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    Larder, Rate, ApplyCount.Band(1, 4), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 0, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1)],
            inputs: [new Term(new BinRef(Scope.Local, Grain), 3) { PerDay = true }],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Grain, BinCapacity.Of(1_000))],
            kindRules: [new RuleId(1)],
            zoneRules: []);

        (World _, Simulation simulation, Handle<Building> _) = Built(greedy, 100);

        Assert.Throws<InvalidOperationException>(() => StepThroughFirings(simulation, 1));
    }
}
