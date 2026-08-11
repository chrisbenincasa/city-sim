using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 10 task 7: failure pressure is a duration, and a Building past its kind's threshold is
/// demolished with its Households evicted into the Unplaced Pool.
/// </summary>
/// <remarks>
/// <para>
/// <b>The fixture starves a Rule the only way a Rule can be starved for ever</b>: its input Bin is
/// filled by nothing. Under <c>adr/0045</c> a failed Rule does not retry — it subscribes to the Bin
/// that stopped it and sleeps — so a Bin no Rule ever writes is a Rule that never wakes, which is the
/// continuous failure a duration exists to measure. Anything intermittent produces a duration that
/// resets, which is the point of <c>adr/0053</c> and is tested separately below.
/// </para>
/// <para>
/// <b>Every test here separates the two failures by name.</b> Short of an input starts the clock;
/// out of headroom does not, because a full Bin is what a well-supplied Building with nowhere to sell
/// looks like. That distinction is the amendment to <c>adr/0053</c> this task made, and it is the one
/// thing in the mechanism that would fail silently: reading both would condemn a healthy city, and
/// the symptom would be a city that declined everywhere at once for no reason a player could see.
/// </para>
/// </remarks>
public sealed class ZoneRuleDemolishTests
{
    private static readonly ResourceId Repairs = new(1);

    private const byte House = 1;
    private const byte HousingBit = 0;
    private const ushort Housing = 1 << HousingBit;

    /// <summary>How long <c>upkeep</c> waits between firings, when it can fire at all.</summary>
    private const uint Rate = 8;

    /// <summary>Missed firings before condemnation, so the threshold is <c>Condemn × Rate</c> Ticks.</summary>
    private const int Condemn = 4;

    /// <summary>
    /// A kind whose one Rule draws on a Bin nothing fills, condemned after
    /// <see cref="Condemn"/> missed firings.
    /// </summary>
    private static Ruleset Declining(int condemnAfter, ZoneRuleDefinition[] zones) =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 0, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfter = condemnAfter, Occupants = 1 }],
            inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],
            zoneRules: zones);

    /// <summary>
    /// A kind whose one Rule <em>produces</em> into a Bin that starts full, so it fails on headroom
    /// and never on level.
    /// </summary>
    private static Ruleset Overstocked(ZoneRuleDefinition[] zones) =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, Rate, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 0, 0, 1, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfter = Condemn, Occupants = 1 }],
            inputs: [],
            outputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],
            zoneRules: zones);

    /// <summary>A Zone Rule that may build on these Lots, so it both raises and condemns.</summary>
    /// <remarks>
    /// <b>The revisit period equals the interval, which is the fastest survey a Ruleset can legally
    /// author</b> (<c>adr/0059</c>: the loader refuses a shorter one). It derives a sample of one draw
    /// per Lot per trigger — the whole city looked at every cycle, which is what these fixtures want,
    /// since what is under test is the condemn predicate rather than the sampler's pacing. Drawing is
    /// with replacement, so one trigger still misses about a third of the Lots and the runs below are
    /// long enough for that not to matter.
    /// </remarks>
    private static ZoneRuleDefinition Sweeping(int revisit = 4, uint interval = 4) =>
        new(House, HousingBit, interval, revisit);

    /// <summary>
    /// A Zone Rule whose permission bit no Lot in the fixture carries, so it condemns and never
    /// builds.
    /// </summary>
    /// <remarks>
    /// <b>This is <c>adr/0055</c> used as a fixture, and it is the ADR's own claim.</b> A permission
    /// set scopes what a Zone Rule <em>builds</em> and never which Lots it looks at — so a Rule that
    /// could not have raised this Building may still notice that it has fallen down. It also makes
    /// the demolition tests readable: with <see cref="Sweeping"/> the same Rule rebuilds on the Lot it
    /// just cleared, within the same run, and a live-Building count says nothing about whether
    /// anything was demolished at all.
    /// </remarks>
    private static ZoneRuleDefinition Watching(int revisit = 4, uint interval = 4) =>
        new(House, HousingBit + 1, interval, revisit);

    /// <summary>A world of <paramref name="houses"/> Buildings, each with one Household in it.</summary>
    private static (World World, Simulation Simulation) Built(Ruleset ruleset, int houses = 4)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0007UL));

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), Housing);
            Handle<Building> building = world.CreateBuilding(
                lot, House, Ticks.Zero, simulation.Key);

            world.CreateHousehold(building, lifeStage: 0);
        }

        return (world, simulation);
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>The one Rule Instance of the <paramref name="index"/>th Building.</summary>
    private static int InstanceOf(World world, int index)
    {
        foreach (int instance in world.BuildingRules.Walk(index))
        {
            return instance;
        }

        return Rows.NoSlot;
    }

    // ---- the clock ------------------------------------------------------------------------------

    /// <summary>A Rule short of an input records the Tick it went short.</summary>
    [Fact]
    public void Going_short_of_an_input_starts_the_clock()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, []));

        Run(simulation, (int)Rate + 2);

        int instance = InstanceOf(world, 0);

        Assert.True(world.RuleInstances.IsStarving(instance));
        Assert.True(world.RuleInstances.StarvedSince[instance].Raw > 0);
    }

    /// <summary>
    /// A Rule that cannot place its output is not starving, and this is the assertion the whole
    /// mechanism turns on.
    /// </summary>
    /// <remarks>
    /// <b>Written as its own test because the failure is invisible in every other one.</b> Reading
    /// both blocking reasons produces a mechanism that works — Buildings decline, demolitions happen,
    /// the cycle runs — and condemns a city for being well supplied. The symptom is a city that falls
    /// down everywhere at once, which reads as a balance problem rather than as a defect.
    /// </remarks>
    [Fact]
    public void Running_out_of_headroom_does_not()
    {
        (World world, Simulation simulation) = Built(Overstocked([]));

        // Filled to the ceiling before the Rule ever runs, so its first evaluation fails on headroom.
        foreach (int bin in world.BuildingBins.Walk(0))
        {
            world.Deposit(world.Bins.Rows.At(bin), 4, Ticks.Zero);
        }

        Run(simulation, (int)Rate * (Condemn + 2));

        int instance = InstanceOf(world, 0);

        Assert.Equal(Blocking.Headroom, world.RuleInstances.Blocked[instance]);
        Assert.False(world.RuleInstances.IsStarving(instance));
        Assert.Equal(4, world.Buildings.Rows.LiveCount);
    }

    /// <summary>Firing clears the clock: recovery is total, with no debt worked off.</summary>
    [Fact]
    public void Firing_clears_the_clock()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, []));

        Run(simulation, (int)Rate + 2);

        int instance = InstanceOf(world, 0);
        Assert.True(world.RuleInstances.IsStarving(instance));

        // Supply arrives, which drains the wait list and re-arms the Rule.
        foreach (int bin in world.BuildingBins.Walk(0))
        {
            world.Deposit(world.Bins.Rows.At(bin), 4, new Ticks((ulong)Rate + 2));
        }

        Run(simulation, 2);

        Assert.False(world.RuleInstances.IsStarving(instance));
    }

    /// <summary>
    /// Waking and going short again does not restart the clock, because the Rule never fired.
    /// </summary>
    /// <remarks>
    /// <b>The difference between a duration of continuous starvation and a time since the last
    /// complaint.</b> A Rule woken by an arrival too small to cover its shortfall comes back through
    /// the same code path having achieved nothing; restarting the clock there would make a Building
    /// that is fed just enough to keep being disappointed immortal, which is the severity inversion
    /// <c>adr/0053</c> exists to refuse, reappearing one level down.
    /// </remarks>
    [Fact]
    public void Waking_and_failing_again_does_not_restart_it()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, []));

        Run(simulation, (int)Rate + 2);

        int instance = InstanceOf(world, 0);
        Ticks since = world.RuleInstances.StarvedSince[instance];

        // One unit arrives and is taken by nobody's satisfaction: the Rule needs one, so it wakes,
        // fires, and would clear the clock — so instead the deposit is withdrawn in the same Tick,
        // leaving the wake without the supply that justified it.
        foreach (int bin in world.BuildingBins.Walk(0))
        {
            Handle<Bin> handle = world.Bins.Rows.At(bin);
            world.Deposit(handle, 1, new Ticks((ulong)Rate + 2));
            world.Withdraw(handle, 1, new Ticks((ulong)Rate + 2));
        }

        Run(simulation, 4);

        Assert.True(world.RuleInstances.IsStarving(instance));
        Assert.Equal(since, world.RuleInstances.StarvedSince[instance]);
    }

    // ---- condemnation ---------------------------------------------------------------------------

    /// <summary>Below the threshold a starving Building is left standing.</summary>
    [Fact]
    public void A_building_short_of_the_threshold_is_not_condemned()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, [Watching()]));

        Run(simulation, (int)Rate * Condemn / 2);

        Assert.Equal(4, world.Buildings.Rows.LiveCount);
    }

    /// <summary>Past it, the sample that finds it demolishes it.</summary>
    [Fact]
    public void A_building_past_the_threshold_is_demolished()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, [Watching()]));

        Run(simulation, (int)Rate * (Condemn + 2));

        Assert.Equal(0, world.Buildings.Rows.LiveCount);
        Assert.Equal(0, world.Bins.Rows.LiveCount);
        Assert.Equal(0, world.RuleInstances.Rows.LiveCount);
    }

    /// <summary>A kind with no threshold never declines, whatever its Rules do.</summary>
    /// <remarks>
    /// Zero is the default, so this is also the assertion that every Ruleset written before decline
    /// existed still means what it meant.
    /// </remarks>
    [Fact]
    public void A_kind_with_no_threshold_is_never_condemned()
    {
        (World world, Simulation simulation) = Built(Declining(0, [Watching()]));

        Run(simulation, (int)Rate * (Condemn + 8));

        Assert.Equal(4, world.Buildings.Rows.LiveCount);
    }

    /// <summary>The threshold is in missed firings, so halving every rate does not halve a lifespan.</summary>
    /// <remarks>
    /// <b>The property <c>adr/0053</c> chose the unit for.</b> Two Rulesets identical but for their
    /// rate condemn at different Ticks and after the same number of missed firings — which is what a
    /// threshold in Ticks would have got wrong, silently, on the day somebody retuned the file.
    /// </remarks>
    [Fact]
    public void The_threshold_is_in_firings_and_not_in_ticks()
    {
        (World fast, Simulation fastRun) = Built(Declining(Condemn, [Watching()]));

        Run(fastRun, (int)Rate * (Condemn + 2));
        Assert.Equal(0, fast.Buildings.Rows.LiveCount);

        // The same city, whose Rule is four times as patient. At the Tick the first was flat, this
        // one is untouched; it needs four times as long to reach the same number of missed firings.
        (World slow, Simulation slowRun) = Built(Patient([Watching()]));

        Run(slowRun, (int)Rate * (Condemn + 2));
        Assert.Equal(4, slow.Buildings.Rows.LiveCount);

        Run(slowRun, (int)Rate * 4 * (Condemn + 2));
        Assert.Equal(0, slow.Buildings.Rows.LiveCount);
    }

    /// <summary><see cref="Declining"/> with a rate four times as long.</summary>
    private static Ruleset Patient(ZoneRuleDefinition[] zones) =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, Rate * 4, ApplyCount.Band(1, 1), RuleId.None, false, default,
                    ConditionId.None, 0, 1, 0, 0, 0, 0),
            ],
            kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfter = Condemn, Occupants = 1 }],
            inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],
            zoneRules: zones);

    // ---- eviction -------------------------------------------------------------------------------

    /// <summary>
    /// The Households of a demolished Building are in the Pool afterwards, not destroyed.
    /// </summary>
    /// <remarks>
    /// <c>adr/0054</c>: destroying them would delete their Money, which <c>adr/0024</c> forbids since
    /// the Outside Connection is money's only sink, and would be an unbounded population sink with no
    /// Departure record.
    /// </remarks>
    [Fact]
    public void The_occupants_are_evicted_into_the_pool()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, [Watching()]));

        Assert.Equal(0, world.UnplacedPool.Count);

        Run(simulation, (int)Rate * (Condemn + 2));

        Assert.Equal(0, world.Buildings.Rows.LiveCount);
        Assert.Equal(4, world.Households.Rows.LiveCount);
        Assert.Equal(4, world.UnplacedPool.Count);

        for (int i = 0; i < world.UnplacedPool.Count; i++)
        {
            Assert.True(
                world.Households.IsUnplaced(
                    world.Households.Rows.Resolve(world.UnplacedPool.At(i))));
        }
    }

    /// <summary>
    /// One Zone Rule both demolishes and rebuilds, which is the growth cycle closing for the first
    /// time.
    /// </summary>
    /// <remarks>
    /// <b>Task 6's finding, discharged.</b> It found that the cycle cannot be entered from a standing
    /// start — a populated city has no vacant Lot and an empty Pool, so creation had nothing to act on
    /// and the only way to test it was to unplace a Household by hand. Demolition is what supplies
    /// both, and this is the assertion that says so: nothing in this fixture ever calls
    /// <c>Unplace</c>, and creation still happens.
    /// </remarks>
    [Fact]
    public void One_rule_demolishes_and_rebuilds_and_the_cycle_closes()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, [Sweeping()]));

        Run(simulation, (int)Rate * (Condemn + 4));

        ZoneActivity activity = simulation.Zoning.Drain();

        Assert.True(activity.Demolished.Sum > 0, "nothing was ever condemned.");
        Assert.True(activity.Created.Sum > 0, "nothing was ever rebuilt on a cleared Lot.");

        // The Lots outlive the Buildings on them, which is what makes this a cycle rather than a
        // city consuming itself.
        Assert.Equal(4, world.Lots.Rows.LiveCount);
    }

    /// <summary>
    /// A rebuilt Building starts healthy, on rows the demolished one was using.
    /// </summary>
    /// <remarks>
    /// <b>The recycled-row test, and it is why <c>Create</c> writes the columns it does not read.</b>
    /// <c>Rows.Allocate</c> hands back a free slot without clearing anything, so a Bin would open with
    /// its predecessor's contents and a Rule Instance would inherit its predecessor's starvation —
    /// which would condemn the new Building on the Tick it was raised, at an age it had not lived.
    /// </remarks>
    [Fact]
    public void A_rebuilt_building_inherits_nothing_from_the_one_it_replaced()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, [Sweeping()]));

        foreach (int bin in world.BuildingBins.Walk(0))
        {
            world.Deposit(world.Bins.Rows.At(bin), 4, Ticks.Zero);
        }

        Run(simulation, (int)Rate * (Condemn + 4));

        int slots = world.Buildings.Rows.SlotCount;

        Run(simulation, (int)Rate);

        Assert.True(world.Buildings.Rows.LiveCount > 0);
        Assert.Equal(slots, world.Buildings.Rows.SlotCount);

        foreach (int bin in world.BuildingBins.Walk(0))
        {
            Assert.Equal(0, world.Bins.LevelAt(bin));
        }
    }
}
