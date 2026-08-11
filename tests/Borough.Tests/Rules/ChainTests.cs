using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 7 task 8: <c>on_fail</c> chains.
/// </summary>
/// <remarks>
/// <b>A link does not do the head's work by a more expensive route — it refills the Bin the head
/// failed on</b> (<c>adr/0045</c>). So a rescued bakery produces no bread on the Tick it was
/// rescued: the link deposits flour, and the deposit is what wakes the head, through the Bin's wait
/// list rather than through a retry.
/// <para>
/// The corpus's own chain steps through <c>pool</c> on its first link, and the District Pool is a
/// named hole that throws. The shape is reproduced here entirely in <c>local</c> scope — milling
/// grain into flour is a second <em>source</em> for the head's Bin, which is what the ladder is.
/// </para>
/// </remarks>
public sealed class ChainTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);
    private static readonly ResourceId Grain = new(3);

    private static readonly ConditionId InputStarved = new(1);

    private const byte Bakery = 1;
    private const uint BakeRate = 8;
    private const uint MillRate = 4;

    /// <summary>Bake, falling back to milling, falling back to a report.</summary>
    private static Ruleset Chained() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(Bakery, BakeRate, ApplyCount.Band(1, 1), new RuleId(2),
                false, default, ConditionId.None, 0, 1, 0, 1, 0, 0),
            new RuleDefinition(Bakery, MillRate, ApplyCount.Band(1, 1), new RuleId(3),
                false, default, ConditionId.None, 1, 1, 1, 1, 0, 0),
            new RuleDefinition(Bakery, MillRate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, InputStarved, 0, 0, 0, 0, 0, 0),
        ],
        kinds: [new KindDefinition(0, 3, 0, 1)],
        inputs:
        [
            new Term(new BinRef(Scope.Local, Flour), 6),
            new Term(new BinRef(Scope.Local, Grain), 6),
        ],
        outputs:
        [
            new Term(new BinRef(Scope.Local, Bread), 4),
            new Term(new BinRef(Scope.Local, Flour), 6),
        ],
        emissions: [],
        bins:
        [
            new BinDeclaration(Flour, BinCapacity.Of(60)),
            new BinDeclaration(Bread, BinCapacity.Of(20)),
            new BinDeclaration(Grain, BinCapacity.Of(60)),
        ],
        // The head only. The other two are links, reached by walking a chain that failed.
        kindRules: [new RuleId(1)],
        zoneRules: []);

    private static (World World, Simulation Simulation, Handle<Building> Building) Built(
        Ruleset ruleset, ulong seed = 1)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(seed));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Bakery);

        foreach (BinDeclaration bin in ruleset.BinsOf(Bakery))
        {
            world.CreateBin(building, bin.Resource);
        }

        foreach (RuleId rule in ruleset.RulesOf(Bakery))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay: 1);
        }

        return (world, simulation, building);
    }

    private static int BinOf(World world, Handle<Building> building, ResourceId resource) =>
        world.FindBin(world.Buildings.Rows.Resolve(building), resource);

    private static long Level(World world, Handle<Building> building, ResourceId resource) =>
        world.Bins.LevelAt(BinOf(world, building, resource));

    /// <remarks>
    /// The Tick matters: waking a subscriber arms it for <c>tick + 1</c>, so a deposit stamped with
    /// a Tick that has already passed arms it into a Wheel bucket nothing will collect until the
    /// Wheel wraps. Callers depositing mid-run pass the Tick they are on.
    /// </remarks>
    private static void Fill(
        World world, Handle<Building> building, ResourceId resource, int amount, Ticks? at = null) =>
        world.Deposit(
            world.Bins.Rows.At(BinOf(world, building, resource)), amount, at ?? Ticks.Zero);

    private static void StepToTheFiring(Simulation simulation)
    {
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);
    }

    // ---- a link rescues -----------------------------------------------------------------------

    /// <summary>No flour and plenty of grain: the mill runs, and the oven does not.</summary>
    [Fact]
    public void A_head_that_fails_is_rescued_by_its_link()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        Fill(world, building, Grain, 60);

        StepToTheFiring(simulation);

        Assert.Equal(6, Level(world, building, Flour));
        Assert.Equal(54, Level(world, building, Grain));

        // The head did not fire. A link refills the Bin; it does not bake.
        Assert.Equal(0, Level(world, building, Bread));
    }

    /// <summary>A rescued chain records nothing: nothing failed that a player needs told about.</summary>
    [Fact]
    public void A_rescued_chain_reports_no_condition()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        Fill(world, building, Grain, 60);

        StepToTheFiring(simulation);

        Assert.Equal(ConditionId.None, world.RuleInstances.Reported[0]);
        Assert.False(world.RuleInstances.IsWaiting(0));
    }

    /// <summary>
    /// The head is not skipped for ever: once its Bin is refilled it bakes on a later Tick.
    /// </summary>
    /// <remarks>
    /// This is the whole point of the ladder. The first firing mills, the flour lands in the head's
    /// Bin, and the head then has its input — reached without any retry logic, because the link's
    /// deposit is a mutator and <c>02 §7</c>'s mutators wake observers.
    /// </remarks>
    [Fact]
    public void A_rescued_head_bakes_once_its_bin_has_been_refilled()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        Fill(world, building, Grain, 60);

        for (int tick = 0; tick < 40; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.True(Level(world, building, Bread) > 0, "the rescued head never baked.");
    }

    // ---- a chain that fails -------------------------------------------------------------------

    /// <summary>
    /// <c>adr/0045</c>: a failed chain subscribes <b>once, at its head</b> — on the Bin the head was
    /// short of, never on the one the last link was short of.
    /// </summary>
    /// <remarks>
    /// Every link relieves the head's Bin, so that one subscription wakes on every rescue path and
    /// chain depth costs no subscriptions at all. Subscribing on grain would be the plausible bug:
    /// grain is what the walk actually stopped on, and it is the wrong answer.
    /// </remarks>
    [Fact]
    public void A_failed_chain_subscribes_once_at_its_head()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        StepToTheFiring(simulation);

        Assert.True(world.RuleInstances.IsWaiting(0));
        Assert.Equal(Blocking.Level, world.RuleInstances.Blocked[0]);

        Assert.Equal(
            world.Bins.Rows.At(BinOf(world, building, Flour)),
            world.RuleInstances.WaitingOn[0]);
    }

    /// <summary>The terminal records its condition, and the chain stays failed.</summary>
    [Fact]
    public void A_terminal_records_its_condition()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        StepToTheFiring(simulation);

        Assert.Equal(InputStarved, world.RuleInstances.Reported[0]);
        Assert.Equal(0, Level(world, building, Bread));
    }

    /// <summary>
    /// <b>The defect the corpus's own worked example contained.</b> A terminal has no term that
    /// could be short, so under ordinary Rule semantics it succeeds — and a Rule that succeeds
    /// re-arms on the Wheel at <c>+rate</c>. A chronically starved bakery would then walk its whole
    /// chain every <c>rate</c> Ticks for as long as the shortage lasted, which is verbatim the cost
    /// <c>adr/0033</c> names as the reason subscription exists. The terminal is never evaluated.
    /// </summary>
    [Fact]
    public void A_terminal_does_not_re_arm_the_head_and_the_building_sleeps()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        for (int tick = 0; tick < 1 + (8 * (int)BakeRate); tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        // Asleep on its subscription, not armed on the Wheel, however long the shortage lasts.
        Assert.True(world.RuleInstances.IsWaiting(0));
        Assert.Equal(0, Level(world, building, Bread));
    }

    /// <summary>
    /// A shortage that ends wakes the head through the Bin, which is what a subscription is for.
    /// </summary>
    [Fact]
    public void A_slept_head_wakes_when_its_bin_is_written()
    {
        (World world, Simulation simulation, Handle<Building> building) = Built(Chained());

        StepToTheFiring(simulation);

        Assert.True(world.RuleInstances.IsWaiting(0));

        Fill(world, building, Flour, 60, at: simulation.Tick);

        Assert.False(world.RuleInstances.IsWaiting(0));

        for (int tick = 0; tick < 1 + (int)BakeRate; tick++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(4, Level(world, building, Bread));
    }
}
