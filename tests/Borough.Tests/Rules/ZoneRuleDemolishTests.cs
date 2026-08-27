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
/// out of space does not, because a full Bin is what a well-supplied Building with nowhere to sell
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

    /// <summary>
    /// The condemnation threshold in <b>Ticks</b> — four missed firings of a rate-<see cref="Rate"/>
    /// Rule, which is what this constant meant when the key was a firing count.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Milestone 17 moved the threshold from missed firings to a duration</b>, so the engine no
    /// longer multiplies by each Rule's own rate and every Rule on a Building is now judged against
    /// one wall clock. This fixture runs a single rate, so the two readings coincide here and the
    /// old assertions still mean what they meant.
    /// </remarks>
    private const int Condemn = 4 * (int)Rate;

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
            kinds:
            [
                new KindDefinition(0, 1, 0, 1)
                {
                    CondemnAfterTicks = condemnAfter, CollapsesAfterDays = 1, Occupants = 1,
                },
            ],
            inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],
            zoneRules: zones);

    /// <summary>
    /// A kind whose one Rule <em>produces</em> into a Bin that starts full, so it fails on space
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
            kinds:
            [
                new KindDefinition(0, 1, 0, 1)
                {
                    CondemnAfterTicks = Condemn, CollapsesAfterDays = 1, Occupants = 1,
                },
            ],
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

    /// <summary>
    /// Buildings that are live <em>and still in use</em> — the count that meant
    /// <c>Rows.LiveCount</c> before abandonment left the shell standing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Every <c>Assert.Equal(0, Buildings.Rows.LiveCount)</c> in this class meant <i>they all
    /// fell</i></b>, and while condemnation freed the row those were the same sentence. Since
    /// milestone 17 task 1 they are not: an abandoned Building keeps its row and its Lot
    /// (<c>adr/0091</c>), so the row count stays at the house count for ever and only this one moves.
    /// </remarks>
    private static int Standing(World world)
    {
        int standing = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && !world.Buildings.IsAbandoned(slot))
            {
                standing++;
            }
        }

        return standing;
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
    public void Running_out_of_space_does_not()
    {
        (World world, Simulation simulation) = Built(Overstocked([]));

        // Filled to the ceiling before the Rule ever runs, so its first evaluation fails on space.
        foreach (int bin in world.BuildingBins.Walk(0))
        {
            world.Deposit(world.Bins.Rows.At(bin), 4, Ticks.Zero);
        }

        Run(simulation, (int)Rate * (Condemn + 2));

        int instance = InstanceOf(world, 0);

        Assert.Equal(Blocking.Space, world.RuleInstances.Blocked[instance]);
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

    /// <summary>Past it, the sample that finds it empties it and leaves the shell standing.</summary>
    [Fact]
    public void A_building_past_the_threshold_is_abandoned()
    {
        (World world, Simulation simulation) = Built(Declining(Condemn, [Watching()]));

        Run(simulation, (int)Rate * (Condemn + 2));

        Assert.Equal(0, Standing(world));
        Assert.Equal(0, world.Bins.Rows.LiveCount);
        Assert.Equal(0, world.RuleInstances.Rows.LiveCount);

        // The shells outlive what emptied them, which is the whole of what task 1 changed: the Bins
        // and the Rules are gone and the premises are not.
        Assert.Equal(4, world.Buildings.Rows.LiveCount);
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
    /// 🔴 <b>THIS TEST ASSERTED THE OPPOSITE UNTIL MILESTONE 17, and it was right to.</b> It was
    /// called <c>The_threshold_is_in_firings_and_not_in_ticks</c> and it guarded
    /// <c>adr/0053</c>'s choice: two Rulesets identical but for their rate condemned after the same
    /// number of <em>missed firings</em>, so retuning a cadence could not silently retune a
    /// Building's lifespan.
    /// <para>
    /// <b>What that protected, and what it cost.</b> The property is real and the price was that no
    /// designer could see what the number meant — <c>condemn_after = 4</c> against a rate-16
    /// <c>upkeep</c> is 45 in-world minutes, and it stood in all eighteen shipped files without one
    /// author ever changing it. Milestone 17 authored the felt quantity instead
    /// (<c>adr/0059</c>, <c>adr/0130</c>), which inverts this test.
    /// </para>
    /// <para>
    /// ⚠ <b>The cadence sensitivity did not vanish; it shrank to one firing.</b> A Rule starts
    /// starving at its first failed firing, so a patient Rule begins its clock later — by one
    /// <c>rate</c> and not by a multiple of the threshold. That residue is asserted here, because a
    /// reader who expects the two cities to fall down on the identical Tick would otherwise file a
    /// defect against arithmetic that is working.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_threshold_is_a_duration_and_not_a_count_of_firings()
    {
        (World fast, Simulation fastRun) = Built(Declining(Condemn, [Watching()]));
        (World slow, Simulation slowRun) = Built(Patient([Watching()]));

        // The patient Rule's first firing, then the whole threshold starved, then a sweep to notice.
        // ⚠ Under the firing count this was nowhere near enough — that Rule needed FOUR TIMES the
        // threshold to reach the same missed-firing count, which is 128 Ticks of starvation against
        // the 32 it gets here. The two cities being flat at the same moment IS the unit change.
        int enough = ((int)Rate * 4) + Condemn + ((int)Rate * 4);

        Run(fastRun, enough);
        Run(slowRun, enough);

        Assert.Equal(0, Standing(fast));
        Assert.Equal(0, Standing(slow));
    }

    /// <summary>
    /// The patient city is still standing at the Tick the fast one went flat, and the gap is one
    /// <c>rate</c> rather than a multiple of the threshold.
    /// </summary>
    /// <remarks>
    /// <b>The residue the test above describes, asserted rather than described.</b> Without this, a
    /// cadence that stopped mattering at all would pass the test above and nobody would notice that
    /// <c>StarvedSince</c> had started being written at Tick 0 instead of at the first failed firing.
    /// </remarks>
    [Fact]
    public void A_patient_rule_starts_its_clock_one_firing_later()
    {
        // Stepped rather than compared at one chosen Tick, because the exact Tick each city goes
        // flat is a function of the arming stagger and the sweep cadence as well as the threshold.
        // ***Pinning it would make this test fail whenever either was retuned, which is the failure
        // mode adr/0053 chose its unit to avoid and this file is not going to reintroduce.***
        int fastFlat = WhenFlat(Built(Declining(Condemn, [Watching()])));
        int slowFlat = WhenFlat(Built(Patient([Watching()])));

        Assert.True(
            slowFlat > fastFlat,
            $"the patient city went flat at {slowFlat} and the fast one at {fastFlat}. A Rule starts "
            + "its pressure clock at its first FAILED firing, so a slower cadence must start later.");

        // ⚠ THE BOUND IS THE WHOLE POINT. Under the old firing count the patient Rule needed FOUR
        // TIMES the threshold to reach the same missed-firing count, so this gap was ~3 x Condemn.
        // It is now one cadence -- bounded by a single threshold and nowhere near a multiple of it.
        Assert.True(
            slowFlat - fastFlat < Condemn,
            $"the gap is {slowFlat - fastFlat} Ticks against a threshold of {Condemn}. A gap that "
            + "scales with the threshold means the comparison is back in missed firings.");
    }

    /// <summary>The first Tick at which nothing is left standing, stepping one sweep at a time.</summary>
    private static int WhenFlat((World World, Simulation Simulation) city)
    {
        const int Step = 4;
        const int GiveUp = 4_096;

        for (int tick = Step; tick <= GiveUp; tick += Step)
        {
            Run(city.Simulation, Step);

            if (Standing(city.World) == 0)
            {
                return tick;
            }
        }

        Assert.Fail($"nothing was condemned in {GiveUp} Ticks, so the fixture never declines.");

        return GiveUp;
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
            kinds: [new KindDefinition(0, 1, 0, 1) { CondemnAfterTicks = Condemn, Occupants = 1 }],
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

        Assert.Equal(0, Standing(world));
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

        // ⚠ THREE DAYS RATHER THAN A HANDFUL OF FIRINGS, and the change is milestone 17's whole
        // shape. This ran for `Rate * (Condemn + 4)` -- 64 Ticks -- because condemnation used to
        // free the Lot on the sweep that found it, so the cycle closed within a few firings. It does
        // not any more: abandonment leaves a shell, and the shell's collapse is a DURATION IN DAYS
        // (adr/0091, and adr/0059's rule that a Ruleset states a duration). One Day is 2,048 Ticks,
        // so a 64-Tick run cannot contain a single collapse and the cycle it asserts cannot happen.
        // ***The test was calibrated against the old sink's units, not against the city.***
        Run(simulation, Ticks.PerDay * 3);

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
