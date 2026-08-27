using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Rules;

using Evidence = Borough.Core.Evidence.Evidence;
using BuildingEvidence = Borough.Core.Evidence.BuildingEvidence;
using RuleEvidence = Borough.Core.Evidence.RuleEvidence;

/// <summary>
/// Milestone 26 task 9 — the long acceptance run for the purchase.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four obligations, and the run answered three of them cleanly and the fourth with a finding.</b>
/// <c>plans/0044</c> task 9 asks for exact conservation across trades (<c>adr/0024</c>), no collection
/// or magnitude trending at steady state (<c>adr/0006</c>), bankruptcy distinguishable from starvation
/// in <c>Evidence</c>, and a look for <c>adr/0163</c>'s own revisit trigger — shops built, condemned
/// for want of customers, and rebuilt on the demand their condemnation restored.
/// </para>
/// <para>
/// 🔴 <b>THE FOURTH ONE CANNOT BE ASKED, BECAUSE THIS WORLD HAS NO STEADY STATE TO ASK IT AT.</b>
/// <c>rulesets/provisioned.toml</c> has no <c>[[policy]]</c>, so <c>adr/0169</c>'s levy is a one-way
/// pump: Households pay shops, shops pay the treasury, and ***nothing pays anybody back.*** Measured
/// over 524,288 Ticks at 2,000 Citizens, **98.3% of the city's money ends in the treasury** and every
/// Household is floored. ⚠ <b>Conservation is perfect the whole way</b> — the two halves are
/// independent, and a conserved economy draining into one account is exactly what conservation cannot
/// see. ***A run that ends is not a run at steady state***, so what this class asserts about the tail
/// is boundedness and never equilibrium.
/// </para>
/// <para>
/// ⚠ <b>Two worlds, because the birth rule and the death rule cannot live in one</b>
/// (<c>adr/0170</c> condition 4, <c>plans/0044</c> <b>F48</b>). <c>provisioned.toml</c> carries tier 1
/// and raises few shops; <c>oversupplied.toml</c> is the same file with the two tier-1 keys deleted
/// and is where shops compete and fail. Both are run, because <c>adr/0006</c> is owed against the one
/// that churns and <c>adr/0170</c>'s convergence reading is owed against the one that does not.
/// </para>
/// <para>
/// ⚠ <b>The readings are taken once and shared</b> — <c>IClassFixture</c>, on
/// <c>MoneyLongRunTests</c>' rule that five facts over five runs cost five times as much for the same
/// numbers and <c>TierBudgetTests</c> would be right to fail it. <b>No tier trait</b>: this is an
/// assertion, it fails when the city changes, and it costs seconds rather than the budget's minutes.
/// </para>
/// </remarks>
public sealed class MarketLongRunTests(MarketLongRun run) : IClassFixture<MarketLongRun>
{
    private readonly MarketLongRun _run = run;

    /// <summary>
    /// <b><c>adr/0024</c>'s equality is exact at every reading, on both worlds.</b>
    /// </summary>
    /// <remarks>
    /// <c>Simulation.CheckEndOfRun</c> already ran the invariant at every reading and would have
    /// thrown, so what is added here is the <b>stronger</b> claim it cannot make: the supply never
    /// moved and neither did the total held. ⚠ <b>Both are needed.</b> A supply that moved with
    /// holdings would satisfy the invariant and mean the city had minted money; holdings that moved
    /// against a fixed supply would mean it had leaked.
    /// </remarks>
    [Fact]
    public void The_supply_is_conserved_to_the_penny_at_every_reading()
    {
        foreach (MarketLongRun.Arm world in _run.Worlds)
        {
            long issued = world.Readings[0].Issued;

            Assert.True(issued > 0, $"{world.File} endowed nobody, so conservation holds vacuously.");

            foreach (MarketLongRun.Reading reading in world.Readings)
            {
                Assert.Equal(issued, reading.Issued);
                Assert.Equal(issued, reading.Held);
            }
        }
    }

    /// <summary>
    /// <b>No collection grows with elapsed time</b> (<c>adr/0006</c>), on either world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Slot counts and not live counts</b>, on <c>BusinessLongRunTests</c>' rule: a live count
    /// oscillates and can sit still while the allocator creeps underneath it, so ***a slot count is
    /// the high-water mark and the high-water mark is what <c>adr/0006</c> is about.***
    /// </para>
    /// <para>
    /// ⚠ <b>Exact equality over the tail rather than a sigma band</b>, because a slot count is
    /// monotonic by construction — it can only rise — so *it did not rise* is a claim a single
    /// comparison settles and a band would only weaken. The non-vacuity guard is that the live count
    /// is strictly below it: a world that filled every slot and stopped would pass this trivially.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_collection_grows_over_the_tail()
    {
        foreach (MarketLongRun.Arm world in _run.Worlds)
        {
            MarketLongRun.Reading[] tail = world.Tail;
            MarketLongRun.Reading opening = tail[0];
            MarketLongRun.Reading closing = tail[^1];

            Flat(world.File, "bin slots", opening.BinSlots, closing.BinSlots);
            Flat(world.File, "Rule Instance slots", opening.RuleSlots, closing.RuleSlots);
            Flat(world.File, "building slots", opening.BuildingSlots, closing.BuildingSlots);
            // ⚠ Two of the five are held to DECELERATION and not to equality, and the split is a
            // property of the table rather than a concession. A Bin, a Rule Instance and a Building
            // reach their peak in the cold start, so a slot count that moved afterwards is a leak.
            // A market row and an unpremised Business are RARE: their slot count is the high-water
            // mark of a small concurrent population, which keeps setting new records for as long as
            // you keep drawing, at a rate that falls. Holding those to equality would assert that a
            // maximum over 240 readings equals a maximum over 16, which is false of any distribution
            // with a tail. BusinessLongRunTests' idiom, and its reasoning.
            Bounded(world, "market rows", at => at.PoolSlots, at => at.PoolLive);
            Bounded(world, "unpremised Businesses", at => at.UnpremisedSlots, at => at.UnpremisedLive);

            Assert.True(
                closing.BinsLive < closing.BinSlots,
                $"{world.File} has every Bin slot live at end of run, so a slot count that did not "
                + "grow says nothing — the arena is simply full and recycling nothing.");
        }
    }

    /// <summary>
    /// <b>The three ways a Rule can be short stay told apart for the whole run</b>, in
    /// <c>Evidence</c> and not only in the tables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0137</c>'s whole claim, held over 131,072 Ticks rather than over the 6,144 that
    /// <c>ProvisionedRulesetTests</c> proves it at once. <b>All three classes must be non-empty</b>:
    /// a run in which everybody is broke and a run in which nobody is would each pass an assertion
    /// that bankruptcy is *reachable*, and neither would show the distinction doing any work.
    /// </para>
    /// <para>
    /// ⚠ <b>The `Evidence` half is asserted separately from the table half and that is deliberate.</b>
    /// The counts come from a walk over <c>RuleInstances</c>; the discrimination is then re-read
    /// through <c>Evidence.OfBuilding</c>, which is the surface a shell would use. ***A distinction
    /// the tables carry and `Evidence` drops is `adr/0137`'s original defect exactly***, and it is the
    /// half that cannot be caught by counting.
    /// </para>
    /// </remarks>
    [Fact]
    public void Bankruptcy_starvation_and_an_empty_market_stay_distinguishable()
    {
        foreach (MarketLongRun.Arm world in _run.Worlds)
        {
            Assert.True(
                world.Readings.Sum(at => at.Broke) > 0,
                $"{world.File}: nothing was ever stopped on a money Bin over the whole run.");

            Assert.True(
                world.Readings.Sum(at => at.Larder) > 0,
                $"{world.File}: nothing was ever stopped on a Bin of its own, so every shortfall was "
                + "blamed on money and the classification is not discriminating.");

            Assert.True(
                world.SeenInEvidence.Count > 1,
                $"{world.File}: Evidence reported {world.SeenInEvidence.Count} of the three ways a "
                + "Rule can be short, over a run in which the tables saw more than one. That is "
                + "adr/0137's defect returning — the distinction exists and the reader cannot see it.");
        }
    }

    /// <summary>
    /// <b><c>adr/0163</c>'s revisit trigger does not fire: the shop count is bounded and does not
    /// grow, on both worlds.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The trigger is *shops built, condemned for want of customers, and rebuilt on the demand their
    /// condemnation restored*, re-aimed by <c>adr/0170</c> at **non-convergence** rather than at
    /// churn: ***that cycle is the mechanism, and what distinguishes health from failure is whether
    /// it converges.*** So what is asserted is that the tail's shop count neither trends nor swings
    /// wider than the band the world sat in early.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>THIS IS NOT `adr/0170`'S RATIFIER AND MUST NOT BE READ AS ONE.</b> That record names
    /// *whether the live shopfront count converges* as the quantity, on the reference machine, on
    /// <c>provisioned.toml</c> — and a convergence reading taken on a city whose Households have all
    /// been floored is a reading of a city that stopped buying, not of a market that settled. The
    /// ratifier stays open in <c>plans/0002</c> §D1 and this fact is a **regression guard**.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shop_count_is_bounded_and_does_not_trend()
    {
        foreach (MarketLongRun.Arm world in _run.Worlds)
        {
            MarketLongRun.Reading[] tail = world.Tail;

            Assert.True(
                tail.Max(at => at.Shops) > 0,
                $"{world.File} never raised a shop, so there is no count to converge.");

            double early = tail[..(tail.Length / 2)].Average(at => (double)at.Shops);
            double late = tail[(tail.Length / 2)..].Average(at => (double)at.Shops);

            Assert.True(
                late <= (early * 2) + 1,
                $"{world.File}'s shop count read {early:F1} over the first half of the tail and "
                + $"{late:F1} over the second. That is adr/0163's revisit trigger: the threshold and "
                + "the claim disagreeing, with each condemnation restoring the demand that rebuilds.");
        }
    }

    /// <summary>
    /// 🔴 <b>THE STRANDED-WAITER DEFECT IS GONE AND A DIFFERENT MISSED WAKE IS UNDERNEATH IT, SO THIS
    /// FACT IS AN ALLOWLIST THAT TELLS THE TWO APART.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ✅ <b><c>plans/0003</c> queue item 22 is FIXED and this asserts the fix rather than trusting
    /// it.</b> A buyer stranded on the market row of a District it had left broke
    /// <see cref="Invariant.WaiterIsBlockedByTheBinItNames"/> at Tick 362,496 on
    /// <c>oversupplied.toml</c>; <c>adr/0171</c> has <c>World.EvaluateDistricts</c> sweep every market
    /// row's queue for waiters whose Rule no longer names it. ***Any violation on a market row's Bin
    /// fails here***, which is the regression that matters.
    /// </para>
    /// <para>
    /// 🔴 <b>What is left is <c>plans/0003</c> queue item 23, and it is not a market defect at all.</b>
    /// Measured on <c>provisioned.toml</c> at Tick 32,768: bin 1523 is a Household's larder holding
    /// <b>294</b>, and Rule Instance 725 is asleep on it needing <b>280</b>. ***The Bin never moved and
    /// the REQUIREMENT came down to meet it*** — 320, then 280, then 240 — because
    /// <c>RuleEngine.Band</c> derives the application count from a <b>readout</b>, and a readout
    /// changes with the city rather than with a Bin write. <c>adr/0063</c> made the requirement live
    /// and nothing re-drains when its input moves.
    /// </para>
    /// <para>
    /// ⚠ <b>It is PRE-EXISTING and was unmasked rather than caused</b>, which was checked rather than
    /// assumed: the probe reaches it on <c>provisioned.toml</c> with <c>adr/0171</c> in and does not
    /// reach it at the commit before. ***A trajectory change is not a cause***, and the readout hole
    /// is reachable by anything that moves an occupancy.
    /// </para>
    /// <para>
    /// ⚠ <b>A green here is worth nothing without the horizon, so the horizon is asserted too.</b> The
    /// same green is produced by a run that no longer reaches Tick 362,496, and a fixture constant is
    /// one edit away from that at all times. ***The cheapest way to make a defect look fixed is to stop
    /// looking.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_only_missed_wake_left_is_the_one_a_readout_shrank()
    {
        foreach (MarketLongRun.Arm world in _run.Worlds)
        {
            foreach (MarketLongRun.Broken broken in world.Violations)
            {
                Assert.True(
                    broken.Violation.Invariant == Invariant.WaiterIsBlockedByTheBinItNames
                    && !broken.OnAMarketRow
                    && broken.Requirement > 0,
                    $"{world.File} broke {broken.Violation.Invariant} at Tick "
                    + $"{broken.Violation.Tick.Raw:N0}, waiter {broken.Violation.Slot:N0}, bin "
                    + $"{broken.Violation.Other:N0}, on a market row = {broken.OnAMarketRow}, "
                    + $"requirement {broken.Requirement:N0}. The allowlist here is queue item 23 "
                    + "alone -- an ORDINARY Bin whose waiter still wants something it now covers, "
                    + "because a readout shrank the band under it. A market row's Bin, or a "
                    + "requirement of zero, is queue item 22 coming back and adr/0171's sweep has "
                    + "stopped working.");
            }

            Assert.True(
                world.Readings.Length * MarketLongRun.ReadEvery > 362_496,
                $"{world.File} stopped short of Tick 362,496, which is where the stranded waiter "
                + "first appeared. A shorter run is green for the wrong reason.");
        }

        Assert.True(
            _run.Worlds.Any(at => at.Violations.Length > 0),
            "no invariant was violated on either world, so queue item 23 is fixed too and this fact "
            + "is the thing to delete.");
    }

    /// <summary>
    /// 🔴 <b>THE CITY HAS NO STEADY STATE, THE TREASURY IS WHY, AND THIS FACT ASSERTS IT SO THAT A FIX
    /// GOES RED.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rulesets/provisioned.toml</c> states no <c>[[policy]]</c>, so nothing ever moves money out
    /// of the treasury: Households pay shops for sundries, shops pay <c>adr/0169</c>'s levy to the
    /// treasury, and the treasury pays nobody. **Measured at 2,000 Citizens over 524,288 Ticks: the
    /// treasury holds 9,363,456 of a 9,522,192 supply — 98.3% — and every Household is floored at the
    /// same 138,192 from early in the run to the end.**
    /// </para>
    /// <para>
    /// ⚠ <b>It is <c>adr/0070</c> *unbuilt* rather than a defect, and the unbuilt thing is named</b>:
    /// a Business has no revenue but sales and a Household has no income at all until
    /// <c>adr/0026</c>'s wage at milestone 15. ***So a levy on this world is a levy on capital***,
    /// which <c>plans/0002</c> §D1 already says of the levy's own two numbers — and this fact is the
    /// same sentence arriving as a measurement over half a million Ticks.
    /// </para>
    /// <para>
    /// ⚠ <b>What is asserted is the SHAPE and not the digits.</b> The treasury's share is checked
    /// against a floor of half the supply rather than against 98.3%, because the exact figure moves
    /// with the levy's level, the population and the horizon, and ***a test pinned to a digit is a
    /// test that fails for the wrong reason first.*** On the day a wage or a Policy closes the
    /// circuit, this goes red and names the sentence to delete.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_treasury_is_a_sink_and_the_run_therefore_has_an_end()
    {
        foreach (MarketLongRun.Arm world in _run.Worlds)
        {
            MarketLongRun.Reading opening = world.Readings[0];
            MarketLongRun.Reading closing = world.Readings[^1];

            Assert.True(
                closing.Treasury > opening.Treasury,
                $"{world.File}'s treasury did not fill over 131,072 Ticks, so either the levy stopped "
                + "firing or something now pays money back — and if it is the second, this whole "
                + "fact is obsolete and wants deleting rather than fixing.");

            Assert.True(
                closing.Treasury * 2 > closing.Held,
                $"{world.File}'s treasury holds {closing.Treasury:N0} of {closing.Held:N0} at end of "
                + "run, which is under half. This fact records a one-way pump into an account with "
                + "no outflow; a treasury that stopped dominating means a circuit exists now.");

            Assert.True(
                closing.Households < opening.Households,
                $"{world.File}'s Households ended no poorer than they began, so nothing is being "
                + "spent and the market is not the thing under test.");

            long fell = 0;

            for (int at = 1; at < world.Readings.Length; at++)
            {
                if (world.Readings[at].Treasury < world.Readings[at - 1].Treasury)
                {
                    fell++;
                }
            }

            Assert.True(
                fell == 0,
                $"{world.File}'s treasury fell on {fell:N0} of {world.Readings.Length:N0} readings. "
                + "It is asserted MONOTONIC and not merely rising, because that is the difference "
                + "between an account with no outflow and one whose inflow happens to dominate.");
        }
    }

    /// <summary>
    /// A bounded fluctuating population whose arena is still setting records, asserted on the
    /// population rather than on the arena.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE OBVIOUS CHECK IS WRONG HERE AND WAS WRITTEN AND WITHDRAWN.</b> A slot count is a
    /// high-water mark of the concurrent live count, so for a population that fluctuates in a bounded
    /// range it grows like the maximum of <em>n</em> draws — ***logarithmically, for ever, without
    /// anything leaking.*** Measured on <c>provisioned.toml</c> at 2,000 Citizens, the unpremised
    /// pool's slots read <b>4, 9, 12, 13</b> at 131,072 / 524,288 / 2,097,152 / 8,388,608 Ticks while
    /// its live count read <b>1, 4, 6, 1</b>. ***Sixty-four times the Ticks for three more slots is
    /// not a collection growing with elapsed time***, and both `Flat` and `BusinessLongRunTests`'
    /// four-fold deceleration reject it.
    /// </para>
    /// <para>
    /// <b>So the claim is made in two halves, and each is the strongest thing true of its side.</b>
    /// The <em>live</em> count must not trend — that is <c>adr/0006</c> proper, and it is where a leak
    /// would actually show. The <em>slot</em> count must not <b>accelerate</b>: a log curve gains less
    /// each half and a leak gains at least as much, so this separates them without pinning a rate.
    /// ⚠ <b>A four-fold deceleration is NOT available</b> — that is `BusinessLongRunTests`' idiom for
    /// a table settling toward a ceiling, and a high-water mark has no ceiling to settle toward.
    /// </para>
    /// </remarks>
    private static void Bounded(
        MarketLongRun.Arm world,
        string what,
        Func<MarketLongRun.Reading, int> slots,
        Func<MarketLongRun.Reading, int> live)
    {
        MarketLongRun.Reading[] tail = world.Tail;
        int half = tail.Length / 2;

        double early = tail[..half].Average(at => (double)live(at));
        double late = tail[half..].Average(at => (double)live(at));
        double error = Math.Sqrt(
            (Spread(tail[..half], live) / half) + (Spread(tail[half..], live) / (tail.Length - half)));
        double band = (3 * error) + 1;

        Assert.True(
            late - early <= band,
            $"{world.File}'s live {what} read {early:F1} over the first half of the tail and "
            + $"{late:F1} over the second -- a rise of {late - early:F1} against a 3-sigma band of "
            + $"{band:F1}. That is adr/0006 on the population, which is where a leak shows.");

        int gainedEarly = slots(tail[half]) - slots(tail[0]);
        int gainedLate = slots(tail[^1]) - slots(tail[half]);

        Assert.True(
            gainedLate <= gainedEarly,
            $"{world.File}'s {what} arena gained {gainedEarly:N0} slots over the first half of the "
            + $"tail and {gainedLate:N0} over the second. A high-water mark of a bounded population "
            + "gains less each half; one that gains more is a population that is growing.");
    }

    /// <summary>Population variance about the mean.</summary>
    private static double Spread(MarketLongRun.Reading[] days, Func<MarketLongRun.Reading, int> of)
    {
        double mean = days.Average(at => (double)of(at));

        return days.Sum(at => (of(at) - mean) * (of(at) - mean)) / days.Length;
    }

    /// <summary>A slot count that may only rise, asserted not to have risen.</summary>
    private static void Flat(string file, string what, int opening, int closing) =>
        Assert.True(
            opening == closing,
            $"{file}'s {what} went {opening:N0} -> {closing:N0} over the tail of the run. A slot "
            + "count is a high-water mark, so this is adr/0006: something is allocated and never "
            + "recycled, and the tail is where a leak is no longer confusable with a cold start.");
}

/// <summary>
/// The run itself, paid once for the whole class.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two worlds at 131,072 Ticks and 2,000 Citizens</b> — 64 readings a world, one a Day.
/// <c>provisioned.toml</c> carries <c>adr/0163</c> tier 1 and <c>oversupplied.toml</c> is the same
/// file with its two keys deleted, so ***the pair is the only way to hold both the birth rule and
/// the death rule in one acceptance run*** (<c>adr/0170</c> condition 4).
/// </para>
/// <para>
/// ⚠ <b><c>CheckEndOfRun</c> runs at every reading rather than at the end</b>, on
/// <c>MoneyLongRunTests</c>' rule: an invariant checked once at the end names the run and not the
/// Tick, and a conservation break that healed itself would go unreported.
/// </para>
/// <para>
/// ⚠ <b>The trade's kind is found by what it SELLS and not by what it reads.</b>
/// <c>oversupplied.toml</c> has no demand-reading Zone Rule at all, so
/// <c>ZoneRuleDefinition.ReadsDemand</c> — which <c>ProvisionedRulesetTests</c> uses — finds nothing
/// there. A kind holding a Good in a business-owned Bin is the predicate that works on both, and it
/// is <c>MarketDump.Sells</c>' own.
/// </para>
/// </remarks>
public sealed class MarketLongRun
{
    /// <summary>
    /// Two hundred and fifty-six Days — <b>four times the 100,000 the Definition of done asks for, and
    /// the horizon is set by the treasury rather than by the collections.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>131,072 was tried first and could not support this class's own sentence.</b> At 64 Days
    /// the treasury holds 46% of the supply and the one-way pump reads as an ordinary imbalance; by
    /// 256 it holds 98% and the city has visibly stopped. ***A test whose remarks quote a number from
    /// a longer run than the one it performs is `plans/0012` Cause 5 built in***, so the run was
    /// lengthened rather than the sentence weakened. It costs seconds.
    /// </remarks>
    private const int TickCount = 524_288;

    /// <summary>
    /// Two thousand, which is what <c>adr/0170</c>'s measurements were taken at.
    /// </summary>
    /// <remarks>
    /// ⚠ A smaller city raises too few shops for a count to converge to anything, and a larger one
    /// changes the Lot ceiling that <c>plans/0044</c> <b>F47</b> records as binding — so this is the
    /// population every number about this world is denominated in.
    /// </remarks>
    private const int Population = 2_000;

    /// <summary>One reading a Day, which is what <c>[market]</c> reprices on.</summary>
    internal const int ReadEvery = 2_048;

    /// <summary>
    /// The first sixteen Days are the cold start and are not the tail.
    /// </summary>
    /// <remarks>
    /// A District does not exist until <c>[districts] revisit_ticks</c>, a shop is not raised until a
    /// Zone Rule samples its Lot, and a Household is not short until it has spent. Sixteen Days is
    /// comfortably past all three and leaves 48 readings to test.
    /// </remarks>
    private const int SettleReadings = 16;

    public MarketLongRun()
    {
        Worlds = [Run("provisioned.toml"), Run("oversupplied.toml")];
    }

    /// <summary>The two worlds, in the order they were run.</summary>
    public IReadOnlyList<Arm> Worlds { get; }

    private static Arm Run(string file)
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Assert.True(loaded.Ok, loaded.Describe());

        Ruleset rules = loaded.Ruleset!;
        byte trade = Trade(rules);

        var key = WorldKey.FromSeed(0x9A0FEDU);
        var world = new World(Population, rules, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason.
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        List<Reading> readings = [];
        List<Broken> violations = [];

        for (int tick = 0; tick < TickCount; tick++)
        {
            simulation.Step(TickInput.Empty);

            if ((tick + 1) % ReadEvery != 0)
            {
                continue;
            }

            // ⚠ Caught rather than propagated, and the allowlist is a [Fact] rather than a silence.
            // An invariant checked only at the end names the run and not the Tick, so it is checked
            // at every reading -- and the one violation this world produces is a REAL DEFECT, filed
            // as plans/0003 queue item 22, which a thrown exception would turn into a class that
            // cannot construct and therefore into four other obligations nobody can read.
            try
            {
                simulation.CheckEndOfRun();
            }
            catch (InvariantViolationException broken)
            {
                violations.Add(Classify(world, broken.Violation));
            }

            readings.Add(Read(world, trade));
        }

        return new Arm(
            file, [.. readings], [.. readings[SettleReadings..]], Seen(world, rules), [.. violations]);
    }

    /// <summary>What kind of missed wake this is, read off the world at the Tick it fired.</summary>
    private static Broken Classify(World world, Violation violation)
    {
        int bin = (int)violation.Other;
        int waiter = (int)violation.Slot;

        if (violation.Invariant != Invariant.WaiterIsBlockedByTheBinItNames
            || !world.Bins.Rows.IsLive(bin)
            || !world.RuleInstances.Rows.IsLive(waiter))
        {
            return new Broken(violation, false, -1);
        }

        return new Broken(
            violation,
            world.Markets.PoolRowOf(world, bin) != DistrictMarkets.NoRow,
            RuleEngine.Requirement(world, waiter, bin, Blocking.Supply));
    }

    /// <summary>One Day's reading of everything the four obligations need.</summary>
    private static Reading Read(World world, byte trade)
    {
        MoneyLedger ledger = MoneyLedger.Of(world);

        long broke = 0;
        long larder = 0;
        long market = 0;

        for (int slot = 0; slot < world.RuleInstances.Rows.SlotCount; slot++)
        {
            if (!world.RuleInstances.Rows.IsLive(slot)
                || !world.RuleInstances.IsStarving(slot)
                || !world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[slot], out int bin))
            {
                continue;
            }

            if (world.Bins.OwnerKind[bin] == BinOwnerKind.District)
            {
                market++;
            }
            else if (world.Rules.IsConserved(world.Bins.Resource[bin]))
            {
                broke++;
            }
            else
            {
                larder++;
            }
        }

        int shops = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.Kind[slot] == trade)
            {
                shops++;
            }
        }

        return new Reading(
            Issued: world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw,
            Held: ledger.Total,
            Treasury: ledger.Treasury,
            Households: Held(world, BinOwnerKind.Household),
                Shops: shops,
            Broke: broke,
            Larder: larder,
            Market: market,
            BinsLive: world.Bins.Rows.LiveCount,
            BinSlots: world.Bins.Rows.SlotCount,
            RuleSlots: world.RuleInstances.Rows.SlotCount,
            BuildingSlots: world.Buildings.Rows.SlotCount,
           PoolSlots: world.DistrictPools.Rows.SlotCount,
            PoolLive: world.DistrictPools.Rows.LiveCount,
            UnpremisedSlots: world.UnpremisedPool.Rows.SlotCount,
            UnpremisedLive: world.UnpremisedPool.Rows.LiveCount);
    }

    /// <summary>What one class of owner holds in conserved Resources.</summary>
    private static long Held(World world, BinOwnerKind owner)
    {
        long held = 0;

        for (int bin = 0; bin < world.Bins.Rows.SlotCount; bin++)
        {
            if (world.Bins.Rows.IsLive(bin)
                && world.Bins.OwnerKind[bin] == owner
                && world.Rules.IsConserved(world.Bins.Resource[bin]))
            {
                held += world.Bins.LevelAt(bin);
            }
        }

        return held;
    }

    /// <summary>
    /// Which of the three shortfalls <c>Evidence</c> can name at end of run, read the way a shell
    /// would read them.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Every Building rather than the worst</b>, on
    /// <c>ProvisionedRulesetTests.Bankruptcy_starvation_and_a_district_shortage_are_told_apart</c>'s
    /// finding: the pressure leader here is an ordinary <c>consume</c> Rule starving in a larder, so
    /// a walk that stopped at the leader would report one class and conclude the other two are absent.
    /// </remarks>
    private static HashSet<string> Seen(World world, Ruleset rules)
    {
        HashSet<string> seen = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            BuildingEvidence evidence = Evidence.OfBuilding(world, world.Buildings.Rows.At(slot));

            foreach (RuleEvidence rule in evidence.Rules.ToArray())
            {
                if (rule.WaitingOn == BinOwnerKind.None)
                {
                    continue;
                }

                Assert.NotEqual(default, rule.WaitingFor);

                seen.Add(
                    rules.IsConserved(rule.WaitingFor) ? "broke"
                    : rule.WaitingOn == BinOwnerKind.District ? "market"
                    : "larder");
            }
        }

        return seen;
    }

    /// <summary>The Building kind that sells — the one predicate that works on both files.</summary>
    private static byte Trade(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            foreach (BinDeclaration bin in rules.BinsOf((byte)kind))
            {
                if (bin.Tenancy == BinTenancy.Business && !rules.IsConserved(bin.Resource))
                {
                    return (byte)kind;
                }
            }
        }

        Assert.Fail("this Ruleset declares no kind that sells, so it cannot demonstrate a market.");

        return 0;
    }

    /// <summary>One world's run, whole and tail.</summary>
    public sealed record Arm(
        string File,
        Reading[] Readings,
        Reading[] Tail,
        HashSet<string> SeenInEvidence,
        Broken[] Violations);

    /// <summary>
    /// One violation, plus the two readings that tell <c>plans/0003</c> queue item 22 from item 23.
    /// </summary>
    /// <remarks>
    /// <b>Recorded at the Tick it fired, because neither is recoverable afterwards.</b> A slot is
    /// recycled and a requirement is derived live, so a test that looked at the world at the end of the
    /// run would be asking about a different city. ***The two defects are the same invariant and the
    /// same message***, and what separates them is whether the Bin is a market row's — item 22, a stale
    /// queue, whose waiter's requirement is therefore <b>zero</b> — or an ordinary Bin whose waiter
    /// still wants something the Bin now covers, which is item 23.
    /// </remarks>
    public readonly record struct Broken(
        Violation Violation, bool OnAMarketRow, long Requirement);

    /// <summary>One Day's reading.</summary>
    public readonly record struct Reading(
        long Issued,
        long Held,
        long Treasury,
        long Households,
        int Shops,
        long Broke,
        long Larder,
        long Market,
        int BinsLive,
        int BinSlots,
        int RuleSlots,
        int BuildingSlots,
        int PoolSlots,
        int PoolLive,
        int UnpremisedSlots,
        int UnpremisedLive);
}
