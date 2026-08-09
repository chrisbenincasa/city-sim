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
        var world = new World(1_000, LayerRuleset.Default, ruleset);
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
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 64, 4)));

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
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 1, 2)));

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
            new ZoneRuleDefinition(House, 0, 8, 2),
            new ZoneRuleDefinition(House, 0, 32, 2)));

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
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 0, 4)));

        InvalidOperationException thrown =
            Assert.Throws<InvalidOperationException>(() => simulation.Step(TickInput.Empty));

        Assert.Contains("interval of 0", thrown.Message, StringComparison.Ordinal);
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
        (World world, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 8)));

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
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 8)));

        ZoneActivity activity = Run(simulation, 64);

        Assert.Equal(0, activity.Occupied.Sum);
        Assert.Equal(activity.Vacant.Sum, activity.Evaluated);
        Assert.True(activity.Evaluated > 0);
    }

    /// <summary>The mirror: a fully built city offers only occupied ones.</summary>
    [Fact]
    public void A_fully_built_city_offers_only_occupied_lots()
    {
        (World world, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 8)));

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
    /// A sample never evaluates more Lots than it declares, however many Rules are running.
    /// </summary>
    /// <remarks>
    /// <b>The tripwire's claim in its weakest testable form.</b> <c>02 §5.7</c> says a Zone Rule's
    /// cost is independent of Zone size; task 9 measures that, and this asserts the structural half —
    /// the per-trigger evaluation count is bounded by the Ruleset and by nothing about the city.
    /// </remarks>
    [Fact]
    public void A_trigger_never_evaluates_more_lots_than_its_sample()
    {
        (_, Simulation small) = Built(Zoned(new ZoneRuleDefinition(House, 0, 8, 6)), lots: 20);
        (_, Simulation large) = Built(Zoned(new ZoneRuleDefinition(House, 0, 8, 6)), lots: 5_000);

        ZoneActivity thin = Run(small, 80);
        ZoneActivity wide = Run(large, 80);

        Assert.Equal(10, thin.Triggers.Sum);
        Assert.Equal(10, wide.Triggers.Sum);
        Assert.True(thin.Evaluated <= 10 * 6);
        Assert.True(wide.Evaluated <= 10 * 6);
    }

    // ---- the instrument ------------------------------------------------------------------------

    /// <summary>Draining twice is not reading twice: the second sees an empty interval.</summary>
    [Fact]
    public void A_reading_drains_the_interval()
    {
        (_, Simulation simulation) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 8)));

        Assert.True(Run(simulation, 32).Triggers.Sum > 0);
        Assert.Equal(0, simulation.Zoning.Drain().Triggers.Sum);
    }

    /// <summary>
    /// Two runs of one world and one key sweep identically.
    /// </summary>
    [Fact]
    public void The_sweep_is_reproducible()
    {
        (_, Simulation first) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 8)));
        (_, Simulation second) = Built(Zoned(new ZoneRuleDefinition(House, 0, 4, 8)));

        Assert.Equal(Run(first, 64), Run(second, 64));
    }

    /// <summary>
    /// The sweep runs every Tick for the life of the city, so it may allocate on the first and never
    /// again.
    /// </summary>
    /// <remarks>
    /// The one allocation this engine can make is its scratch buffer, grown to the widest sample any
    /// Zone Rule declares. Measuring from the second trigger is what separates <em>grown once</em>
    /// from <em>grown every time</em>, which are indistinguishable in a total.
    /// </remarks>
    [Fact]
    public void Sweeping_allocates_nothing_after_the_first_trigger()
    {
        (_, Simulation simulation) = Built(Zoned(
            new ZoneRuleDefinition(House, 0, 1, 4),
            new ZoneRuleDefinition(House, 0, 2, 16)));

        // Two Ticks: the first grows the buffer to 4, the second to 16, which is the widest.
        simulation.Step(TickInput.Empty);
        simulation.Step(TickInput.Empty);

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 500; i++)
        {
            simulation.Step(TickInput.Empty);
        }

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }
}
