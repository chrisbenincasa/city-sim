using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 10 task 5: the trigger in Tick phase 6, and what it does not share with a Bin Rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>The counts are asserted exactly rather than bounded.</b> A trigger is the unit of cost this
/// slice's tripwire is stated over, so a test asserting <em>at least one</em> would leave the engine
/// free to fire twice a Tick without failing — and firing twice on an interval is precisely the
/// defect an off-by-one in the modulus produces.
/// </para>
/// <para>
/// <b>Nothing here builds or demolishes anything</b>, because neither predicate exists yet. What is
/// under test is the schedule and the fork: how often a Zone Rule runs, how many Lots it looks at,
/// and which of the two mechanisms each of those Lots is a candidate for.
/// </para>
/// </remarks>
public sealed class ZoneRuleTriggerTests
{
    private const byte House = 1;

    /// <summary>
    /// The shipped revisit period, and the default a Ruleset that states none is given.
    /// </summary>
    /// <remarks>
    /// <b>Most of these fixtures do not care what it is, which is the point of naming it once.</b>
    /// A Zone Rule authors a duration and the engine derives the sample from the Lot count
    /// (<c>adr/0059</c>), so the tests about the <em>schedule</em> want a period that is simply not
    /// interesting; the two that are about the sample state their own.
    /// </remarks>
    private const int Day = Ticks.PerDay;

    private static Ruleset Zoned(params ZoneRuleDefinition[] zoneRules) => new(
        resources: [],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: zoneRules);

    /// <summary>A world with <paramref name="lots"/> vacant Lots and no Buildings at all.</summary>
    private static (World World, Simulation Simulation) Built(Ruleset ruleset, int lots = 200)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0F05_0F05_0F05UL));

        for (int i = 0; i < lots; i++)
        {
            world.Lots.Create(new Tiles(i), new Tiles(0), zone: 1);
        }

        return (world, simulation);
    }

    private static ZoneActivity Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        return simulation.Zoning.Drain();
    }

    // ---- the schedule ---------------------------------------------------------------------------

    /// <summary>
    /// A Zone Rule fires on the Ticks its interval divides, and on no others.
    /// </summary>
    /// <remarks>
    /// Tick 0 counts, which matches the Map Layer schedule's <c>tick % 64 == 0</c> and is the reason
    /// 256 Ticks at an interval of 64 is four firings rather than three or five.
    /// </remarks>
    [Fact]
    public void A_zone_rule_fires_on_its_interval_and_not_between()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 64, Day)));

        ZoneActivity activity = Run(simulation, 256);

        Assert.Equal(4, activity.Triggers.Sum);
        Assert.Equal(1, activity.Triggers.Peak);
    }

    /// <summary>
    /// An interval of 1 fires every Tick, which is the bound the loader admits and nothing rounds up.
    /// </summary>
    [Fact]
    public void An_interval_of_one_fires_every_tick()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 1, Day)));

        Assert.Equal(32, Run(simulation, 32).Triggers.Sum);
    }

    /// <summary>
    /// Two Zone Rules keep their own schedules, and the Tick they share reads as a peak of two.
    /// </summary>
    /// <remarks>
    /// <b>The peak is the assertion that matters.</b> Sums alone cannot distinguish two Rules that
    /// never coincide from two that always do, and it is the coincident Tick that a budget is held
    /// against — which is why <c>02 §4</c> makes a flow two numbers rather than one.
    /// </remarks>
    [Fact]
    public void Two_zone_rules_keep_their_own_intervals()
    {
        (_, Simulation simulation) = Built(Zoned(
            new ZoneRuleDefinition(House, 0, 8, Day),
            new ZoneRuleDefinition(House, 0, 32, Day)));

        ZoneActivity activity = Run(simulation, 64);

        // 8 firings of the first (0, 8, ... 56) and 2 of the second (0, 32).
        Assert.Equal(10, activity.Triggers.Sum);
        Assert.Equal(2, activity.Triggers.Peak);
    }

    /// <summary>A Ruleset with no Zone Rule sweeps nothing, which is every Ruleset shipped so far.</summary>
    [Fact]
    public void A_ruleset_with_no_zone_rules_does_nothing_in_phase_six()
    {
        (_, Simulation simulation) = Built(Zoned());

        ZoneActivity activity = Run(simulation, 128);

        Assert.Equal(0, activity.Triggers.Sum);
        Assert.Equal(0, activity.Evaluated);
    }

    /// <summary>
    /// A Ruleset built in code rather than loaded is refused rather than dividing by its interval.
    /// </summary>
    /// <remarks>
    /// <c>adr/0048</c>'s two-sided check, in the direction only the core can cover: the loader refuses
    /// an interval below 1, and this refuses one that never went through the loader.
    /// </remarks>
    [Fact]
    public void An_interval_of_zero_is_refused_rather_than_divided_by()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 0, Day)));

        InvalidOperationException thrown =
            Assert.Throws<InvalidOperationException>(() => simulation.Step(TickInput.Empty));

        Assert.Contains("interval of 0", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same two-sided check for the revisit period, which is the other thing that divides.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0059</c> gave this family a second denominator and it needs the same pair of
    /// refusals.</b> The loader refuses a revisit period below the interval, so it can never reach
    /// zero through a file; a Ruleset built in code has been through none of that, and dividing by it
    /// would be a <see cref="DivideByZeroException"/> from inside <c>IntegerMath</c> with nothing
    /// naming the Rule.
    /// </remarks>
    [Fact]
    public void A_revisit_period_of_zero_is_refused_rather_than_divided_by()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 0)));

        InvalidOperationException thrown =
            Assert.Throws<InvalidOperationException>(() => simulation.Step(TickInput.Empty));

        Assert.Contains("revisit period of 0", thrown.Message, StringComparison.Ordinal);
    }

    // ---- adr/0033's observable difference --------------------------------------------------------

    /// <summary>
    /// A Zone Rule triggers without ever reaching the Event Wheel.
    /// </summary>
    /// <remarks>
    /// <b>This is <c>adr/0033</c> made checkable, and it is the first test in the project that can
    /// tell the two Rule families apart.</b> A Bin Rule exists in the world as a Rule Instance row
    /// armed into a wheel bucket, and its cost shows up as a due count. A Zone Rule has no row, no
    /// bucket and no subscription — so a world running one and nothing else does real work in phase 6
    /// while the Bin Rule engine's counters stay at zero for the whole run. Moving a mechanism between
    /// the families would move it across this boundary, which is why the ADR calls it a change to the
    /// city rather than an optimisation.
    /// </remarks>
    [Fact]
    public void A_zone_rule_has_no_wheel_entry_and_no_due_count()
    {
        (World world, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, Day)));

        ZoneActivity zoning = Run(simulation, 64);
        RuleActivity bins = simulation.Rules.Drain();

        Assert.Equal(16, zoning.Triggers.Sum);
        Assert.True(zoning.Evaluated > 0);

        Assert.Equal(0, bins.Due.Sum);
        Assert.Equal(0, bins.Evaluations.Sum);
        Assert.Equal(0, world.RuleInstances.Rows.LiveCount);
    }

    // ---- the fork ------------------------------------------------------------------------------

    /// <summary>
    /// Every sampled Lot is a candidate for exactly one of the two mechanisms.
    /// </summary>
    /// <remarks>
    /// The sample is drawn from every Lot (<c>adr/0055</c>), so what separates the create path from
    /// the decline path is occupancy and nothing else — not the permission bit, which is a term in the
    /// create predicate alone.
    /// </remarks>
    [Fact]
    public void An_empty_city_offers_only_vacant_lots()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, Day)));

        ZoneActivity activity = Run(simulation, 64);

        Assert.Equal(0, activity.Occupied.Sum);
        Assert.Equal(activity.Vacant.Sum, activity.Evaluated);
        Assert.True(activity.Evaluated > 0);
    }

    /// <summary>The mirror: a fully built city offers only occupied ones.</summary>
    [Fact]
    public void A_fully_built_city_offers_only_occupied_lots()
    {
        (World world, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, Day)));

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            world.CreateBuilding(world.Lots.Rows.At(slot), House, simulation.Tick, simulation.Key);
        }

        ZoneActivity activity = Run(simulation, 64);

        Assert.Equal(0, activity.Vacant.Sum);
        Assert.Equal(activity.Occupied.Sum, activity.Evaluated);
        Assert.True(activity.Evaluated > 0);
    }

    /// <summary>
    /// A trigger evaluates exactly its derived sample, and the derivation is the Ruleset's arithmetic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This used to say <em>never more Lots than it declares</em>, and it was the wrong claim to
    /// hold.</b> A Ruleset declared an absolute count, so a bound stated against it was trivially true
    /// at any city size and said nothing at all — which is the shape of the defect <c>adr/0059</c>
    /// fixes. What is asserted now is that the engine evaluates what <c>SampleFor</c> says, so the
    /// scale property in the test below is a property of the running engine rather than of a formula
    /// nothing calls.
    /// </para>
    /// <para>
    /// <b>Exactly, rather than at most.</b> Every Lot here is live, so no draw is discarded, and
    /// sampling is with replacement (task 11c) so no draw is dropped as a repeat either.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_trigger_evaluates_exactly_its_derived_sample()
    {
        var rule = new ZoneRuleDefinition(House, 0, 8, Day);

        (_, Simulation small) = Built(Zoned(rule), lots: 2_048);
        (_, Simulation large) = Built(Zoned(rule), lots: 204_800);

        int smallSample = rule.SampleFor(2_048);
        int largeSample = rule.SampleFor(204_800);

        // The engine evaluates what SampleFor says, which is the claim. The sample's own value is a
        // function of Ticks.PerDay and was the literals 2 and 200 until the clock moved on 2026-08-13
        // (adr/0094) -- so it is asserted as the derivation's shape rather than as two numbers.
        Assert.Equal(10 * smallSample, Run(small, 80).Evaluated);
        Assert.Equal(10 * largeSample, Run(large, 80).Evaluated);

        // A hundred times the Lots is a hundred times the sample, exactly. This is the property
        // adr/0059 exists for and the one no clock can move.
        Assert.Equal(100 * smallSample, largeSample);
        Assert.True(smallSample > 0);
    }

    /// <summary>
    /// <b>The scale test, and the reason task 11 exists.</b> Two cities three orders of magnitude
    /// apart revisit a Lot on the same period.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What the shipped Ruleset used to do instead.</b> <c>sample</c> was an absolute count of Lots
    /// per trigger, so the quantity a city actually feels — the fraction of itself looked at per cycle
    /// — was inversely proportional to its size: S0b measured one visit per Lot every 968 Ticks at
    /// 1,000 Citizens and every 960,008 at 1,000,000, and at target scale the Ruleset built nothing at
    /// all in 2,000 Ticks. The failure was invisible to every test in this file, because every test in
    /// this file ran at one city size.
    /// </para>
    /// <para>
    /// <b>The lot counts divide exactly, and that is a fixture decision worth stating.</b> The
    /// derivation takes a ceiling (<c>ZoneRuleDefinition.SampleFor</c> says why), so a city whose exact
    /// sample is fractional is surveyed slightly <em>faster</em> than its file asks for — bounded by
    /// one Lot a trigger, and unbounded as a ratio on a city small enough. Choosing counts that divide
    /// is what lets this assert an equality rather than a tolerance nobody could read.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(2_048)]
    [InlineData(204_800)]
    public void The_revisit_period_is_the_same_at_every_city_size(int lots)
    {
        const uint Interval = 8;
        const int Ticks = 800;

        (World world, Simulation simulation) = Built(
            Zoned(new ZoneRuleDefinition(House, 0, Interval, Day)), lots);

        long evaluated = Run(simulation, Ticks).Evaluated;

        // Ticks × Lots ÷ evaluations: how long a city of this size takes to look at itself once.
        Assert.Equal(Day, (int)(Ticks * (long)world.Lots.Rows.SlotCount / evaluated));
    }

    // ---- the instrument ------------------------------------------------------------------------

    /// <summary>Draining twice is not reading twice: the second sees an empty interval.</summary>
    [Fact]
    public void A_reading_drains_the_interval()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, Day)));

        Assert.True(Run(simulation, 32).Triggers.Sum > 0);
        Assert.Equal(0, simulation.Zoning.Drain().Triggers.Sum);
    }

    /// <summary>
    /// Two runs of one world and one key sweep identically.
    /// </summary>
    [Fact]
    public void The_sweep_is_reproducible()
    {
        (_, Simulation first) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, Day)));
        (_, Simulation second) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, Day)));

        Assert.Equal(Run(first, 64), Run(second, 64));
    }

    /// <summary>
    /// The sweep runs every Tick for the life of the city, so it may allocate on the first and never
    /// again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one allocation this engine can make is its scratch buffer, grown to the widest sample any
    /// Zone Rule reaches. Measuring from the second trigger is what separates <em>grown once</em>
    /// from <em>grown every time</em>, which are indistinguishable in a total.
    /// </para>
    /// <para>
    /// <b>What bounds it changed under <c>adr/0059</c> and the test did not have to.</b> The widest
    /// sample used to be the largest number in the Ruleset; it is now derived from the Lot count, so
    /// a city that is still being painted grows the buffer as it grows. The Lot table here is fixed
    /// after the arrange, which is the condition this asserts under — and the honest statement of the
    /// property is <em>allocates nothing while the city is not gaining Lots</em>.
    /// </para>
    /// </remarks>
    [Fact]
    public void Sweeping_allocates_nothing_after_the_first_trigger()
    {
        // Two Rules whose derived samples differ, so that "the widest" is a real choice rather than
        // a tie: at 200 Lots these are 2 and 20.
        (_, Simulation simulation) = Built(Zoned(
            new ZoneRuleDefinition(House, 0, 1, 100),
            new ZoneRuleDefinition(House, 0, 2, 20)));

        // Two Ticks: both Rules fire on Tick 0 and the buffer reaches the wider of the two; Tick 1
        // fires only the first, which must not shrink or regrow it.
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 500; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        AllocationProbe.Record(
            "ZoneRuleTriggerTests.Sweeping_allocates_nothing_after_the_first_trigger",
            after - before,
            GC.CollectionCount(0) - gen0,
            GC.CollectionCount(1) - gen1,
            GC.CollectionCount(2) - gen2);

        Assert.Equal(before, after);
    }
}
