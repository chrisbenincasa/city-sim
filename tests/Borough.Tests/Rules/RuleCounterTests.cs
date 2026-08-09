using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 7 task 9: <c>02 §4</c>'s two counters, and the scheduled load they are read against.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim under test is one sentence of <c>02 §4</c>, and it was a defect rather than a
/// wording.</b> The section says the evaluation counter <em>"currently counts due Rule Instances,
/// which does not see a chain link at all"</em> — so the quantity the slice's tripwire is stated over
/// was the one quantity chain walking could not move. Every test below that compares two worlds is
/// comparing worlds with the <em>same</em> due count, because that is the comparison the old counter
/// could not make.
/// </para>
/// <para>
/// <b>The exact evaluation counts are asserted rather than bounded</b>, which looks brittle and is
/// the point: an evaluation is a unit of cost, the tripwire is a number of them per Tick, and a test
/// asserting <em>at least one</em> would leave the engine free to double the bill without failing.
/// </para>
/// </remarks>
public sealed class RuleCounterTests
{
    private static readonly ResourceId Flour = new(1);
    private static readonly ResourceId Bread = new(2);
    private static readonly ResourceId Grain = new(3);

    private static readonly ConditionId InputStarved = new(1);

    private const byte Bakery = 1;
    private const uint BakeRate = 8;
    private const uint MillRate = 4;

    /// <summary><c>02 §4.3</c>'s bakery, with no <c>on_fail</c> at all: six flour in, four bread out.</summary>
    private static Ruleset Unchained() => new(
        resources: [ResourceFamily.Good, ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(Bakery, BakeRate, ApplyCount.Band(1, 1), RuleId.None,
                false, default, ConditionId.None, 0, 1, 0, 1, 0, 0),
        ],
        kinds: [new KindDefinition(0, 2, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [new Term(new BinRef(Scope.Local, Bread), 4)],
        emissions: [],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60)), new BinDeclaration(Bread, BinCapacity.Of(20))],
        kindRules: [new RuleId(1)]);

    /// <summary>
    /// The same bakery, falling back to milling, falling back to a report.
    /// </summary>
    /// <remarks>
    /// <b>Identical to <see cref="Unchained"/> in everything the Wheel can see</b> — one Rule armed on
    /// one Building at one rate — and different only below the head. That is what makes the pair a
    /// measurement of chain walking rather than of two different cities.
    /// </remarks>
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
        kindRules: [new RuleId(1)]);

    private static (World World, Simulation Simulation) Built(Ruleset ruleset)
    {
        var world = new World(1_000, LayerRuleset.Default, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(1));

        return (world, simulation);
    }

    /// <summary>Adds one bakery with its declared Bins and its head Rule armed <paramref name="delay"/> Ticks out.</summary>
    private static Handle<Building> Bake(World world, Simulation simulation, uint delay = 1)
    {
        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Bakery);

        foreach (BinDeclaration bin in world.Rules.BinsOf(Bakery))
        {
            world.CreateBin(building, bin.Resource, bin.Capacity);
        }

        foreach (RuleId rule in world.Rules.RulesOf(Bakery))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay);
        }

        return building;
    }

    private static void Fill(World world, Handle<Building> building, ResourceId resource, int amount)
    {
        int slot = world.FindBin(world.Buildings.Rows.Resolve(building), resource);

        world.Deposit(world.Bins.Rows.At(slot), amount, Ticks.Zero);
    }

    /// <summary>Steps until the Rule armed at <c>delay = 1</c> has been through all three phases.</summary>
    private static void StepToTheFiring(Simulation simulation)
    {
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);
    }

    // ---- what one evaluation is ---------------------------------------------------------------

    /// <summary>
    /// A Rule that fires is evaluated twice: once against the Past and once against the Future.
    /// </summary>
    /// <remarks>
    /// Phase 3's re-check is a full evaluation and costs one, so it is counted as one. It is not
    /// belt and braces — <c>adr/0049</c> makes it the mechanism by which a greedy Rule is served
    /// short — and a counter that skipped it would under-report the engine's bill by the number of
    /// Rules that succeed, which is most of them in a healthy city.
    /// </remarks>
    [Fact]
    public void A_rule_that_fires_costs_two_evaluations_and_walks_no_chain()
    {
        (World world, Simulation simulation) = Built(Unchained());
        Handle<Building> bakery = Bake(world, simulation);

        Fill(world, bakery, Flour, 6);
        StepToTheFiring(simulation);

        RuleActivity activity = simulation.Rules.Drain();

        Assert.Equal(1, activity.Due.Sum);
        Assert.Equal(2, activity.Evaluations.Sum);
        Assert.Equal(0, activity.ChainRungs.Sum);
    }

    /// <summary>A Rule that fails against the Past is never re-checked, so it costs one.</summary>
    [Fact]
    public void A_failing_head_with_no_chain_costs_one_evaluation()
    {
        (World world, Simulation simulation) = Built(Unchained());

        Bake(world, simulation);
        StepToTheFiring(simulation);

        RuleActivity activity = simulation.Rules.Drain();

        Assert.Equal(1, activity.Due.Sum);
        Assert.Equal(1, activity.Evaluations.Sum);
        Assert.Equal(0, activity.ChainRungs.Sum);
    }

    // ---- the sentence 02 §4 wrote against the old counter --------------------------------------

    /// <summary>
    /// Two starved bakeries, one with a chain and one without: the same due count, different bills.
    /// </summary>
    /// <remarks>
    /// <b>This is the assertion the counter existed to fail.</b> Counting due Rule Instances, both
    /// worlds read <c>1</c> and the chain is free; counting evaluations, the chained world costs twice
    /// as much and the difference is exactly the link that was walked. <c>02 §4</c> states the
    /// tripwire over evaluations for this reason, and the tripwire would have been unfalsifiable
    /// against the old quantity.
    /// </remarks>
    [Fact]
    public void A_walked_chain_moves_evaluations_and_leaves_the_due_count_alone()
    {
        (World flat, Simulation flatRun) = Built(Unchained());
        Bake(flat, flatRun);
        StepToTheFiring(flatRun);

        (World laddered, Simulation ladderedRun) = Built(Chained());
        Bake(laddered, ladderedRun);
        StepToTheFiring(ladderedRun);

        RuleActivity without = flatRun.Rules.Drain();
        RuleActivity with = ladderedRun.Rules.Drain();

        Assert.Equal(without.Due.Sum, with.Due.Sum);
        Assert.Equal(1, without.Evaluations.Sum);
        Assert.Equal(2, with.Evaluations.Sum);
    }

    /// <summary>
    /// The reporting terminal is a rung and not an evaluation, which is why the two counters differ.
    /// </summary>
    /// <remarks>
    /// <b>Task 8's semantic, read off the instruments.</b> A terminal is never evaluated — it has no
    /// term that could be short, so ordinary Rule semantics would fire it, re-arm the head on
    /// <c>rate</c>, and walk the chain again for as long as the shortage lasted. So the walk descends
    /// two rungs (the mill and the report) and spends two evaluations (the head and the mill), and
    /// the two numbers being equal here is a coincidence of depth rather than a rule: the head is an
    /// evaluation and not a rung, the terminal a rung and not an evaluation.
    /// </remarks>
    [Fact]
    public void A_chain_ending_in_a_report_costs_one_fewer_evaluation_than_its_depth()
    {
        (World world, Simulation simulation) = Built(Chained());

        Bake(world, simulation);
        StepToTheFiring(simulation);

        RuleActivity activity = simulation.Rules.Drain();

        // Head, then mill. The terminal is reached and not evaluated.
        Assert.Equal(2, activity.Evaluations.Sum);

        // Mill, then terminal. The head is not a rung of its own chain.
        Assert.Equal(2, activity.ChainRungs.Sum);
    }

    /// <summary>A link that rescues stops the descent, so the terminal is never reached.</summary>
    [Fact]
    public void A_rescued_head_descends_one_rung_and_not_the_whole_ladder()
    {
        (World world, Simulation simulation) = Built(Chained());
        Handle<Building> bakery = Bake(world, simulation);

        Fill(world, bakery, Grain, 6);
        StepToTheFiring(simulation);

        RuleActivity activity = simulation.Rules.Drain();

        Assert.Equal(1, activity.ChainRungs.Sum);

        // Head, mill, and Phase 3's re-check of the mill that will actually run.
        Assert.Equal(3, activity.Evaluations.Sum);
    }

    // ---- a flow is not a level ------------------------------------------------------------------

    /// <summary>
    /// The peak is the busiest single Tick of an interval, which is neither its mean nor its last.
    /// </summary>
    /// <remarks>
    /// <b>The number the tripwire is read against.</b> Two bakeries due on one Tick and a third due on
    /// the next is a three-evaluation interval whose worst Tick is two — and a budget is spent per
    /// Tick, so an interval reporting only its total would let a spike hide inside a mean that fits.
    /// </remarks>
    [Fact]
    public void The_peak_is_the_busiest_tick_and_not_the_mean()
    {
        (World world, Simulation simulation) = Built(Unchained());

        Bake(world, simulation, delay: 1);
        Bake(world, simulation, delay: 1);
        Bake(world, simulation, delay: 2);

        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);

        RuleActivity activity = simulation.Rules.Drain();

        Assert.Equal(3, activity.Due.Sum);
        Assert.Equal(2, activity.Due.Peak);
    }

    /// <summary>Reading a flow drains it, because the reading is the interval's end.</summary>
    /// <remarks>
    /// <b>A cumulative counter would trend upward for the life of every run</b>, and a census exists
    /// to answer <em>is this trending upward</em>. An instrument that always says yes says nothing —
    /// and the draining is also where <c>adr/0006</c>'s question, <em>what is the sink</em>, is
    /// answered for these three.
    /// </remarks>
    [Fact]
    public void A_reading_drains_the_interval()
    {
        (World world, Simulation simulation) = Built(Unchained());
        Handle<Building> bakery = Bake(world, simulation);

        Fill(world, bakery, Flour, 6);
        StepToTheFiring(simulation);

        Assert.NotEqual(0, simulation.Rules.Drain().Evaluations.Sum);
        Assert.Equal(default, simulation.Rules.Drain());
    }

    // ---- and into the Census --------------------------------------------------------------------

    /// <summary>The counters reach <c>series(metric, window)</c>, which is what task 9 owes.</summary>
    [Fact]
    public void The_census_carries_the_rule_counters()
    {
        (World world, Simulation simulation) = Built(Chained());

        Bake(world, simulation);

        var census = new Census(world);

        StepToTheFiring(simulation);
        census.Observe(simulation);

        Series evaluations = census.Series(
            Metric.Of(RuleCounter.Evaluations, Aggregate.Sum), new Ticks(1_000));
        Series rungs = census.Series(
            Metric.Of(RuleCounter.ChainRungs, Aggregate.Peak), new Ticks(1_000));

        Assert.Equal(2, evaluations.Samples.Span[^1].Value);
        Assert.Equal(2, rungs.Samples.Span[^1].Value);
    }

    /// <summary>
    /// A census reading drains the engine, so a second reading of one Tick is an empty interval.
    /// </summary>
    /// <remarks>
    /// Worth asserting rather than inferring: it is the one way the two families visibly disagree,
    /// and a caller sampling twice would otherwise read the second zero as a quiet Tick.
    /// </remarks>
    [Fact]
    public void A_second_census_reading_of_one_tick_sees_an_empty_interval()
    {
        (World world, Simulation simulation) = Built(Unchained());
        Handle<Building> bakery = Bake(world, simulation);

        Fill(world, bakery, Flour, 6);
        StepToTheFiring(simulation);

        var census = new Census(world);

        census.Observe(simulation);
        census.Observe(simulation);

        Series series = census.Series(
            Metric.Of(RuleCounter.Evaluations, Aggregate.Sum), new Ticks(1_000));

        Assert.Equal(2, series.Samples.Span[0].Value);
        Assert.Equal(0, series.Samples.Span[1].Value);
    }

    /// <summary>
    /// A world nobody stepped reads zero rather than refusing, and that is a true reading.
    /// </summary>
    [Fact]
    public void A_world_with_no_run_behind_it_reads_an_empty_interval()
    {
        (World world, Simulation _) = Built(Unchained());
        var census = new Census(world);

        census.Observe(world, Ticks.Zero, default);

        Series series = census.Series(
            Metric.Of(RuleCounter.Due, Aggregate.Sum), new Ticks(1_000));

        Assert.Equal(0, series.Samples.Span[0].Value);
    }

    // ---- the two families do not answer each other's questions ----------------------------------

    /// <summary>A Rule metric names no table, and asking is an error rather than table zero.</summary>
    /// <remarks>
    /// <b>Zero is a valid table index</b>, so a default would answer the wrong question with the
    /// first table's name. This is the same reasoning that keeps <c>pool</c> and <c>global</c> named
    /// holes that throw rather than scopes that return nothing.
    /// </remarks>
    [Fact]
    public void A_rule_metric_refuses_to_name_a_table()
    {
        Metric metric = Metric.Of(RuleCounter.Due, Aggregate.Sum);

        Assert.Throws<InvalidOperationException>(() => metric.Table);
        Assert.Throws<InvalidOperationException>(() => metric.Counter);
    }

    /// <summary>A table metric is a level, so it has no reduction to ask for.</summary>
    [Fact]
    public void A_table_metric_refuses_to_be_aggregated()
    {
        Metric metric = Metric.Of(0, CensusCounter.Live);

        Assert.Throws<InvalidOperationException>(() => metric.Aggregate);
        Assert.Throws<InvalidOperationException>(() => metric.RuleCounter);
    }

    /// <summary>An undeclared Rule counter is refused, as an undeclared table counter already was.</summary>
    [Fact]
    public void An_undeclared_rule_counter_is_refused()
    {
        (World world, Simulation _) = Built(Unchained());
        var census = new Census(world);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => census.Series(Metric.Of((RuleCounter)9, Aggregate.Sum), new Ticks(1)));
    }
}
