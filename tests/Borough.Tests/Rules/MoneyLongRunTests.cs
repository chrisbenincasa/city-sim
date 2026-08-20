using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0033</c> task 9's acceptance run: 100,000 Ticks of a city with a money circuit in force,
/// and the money sum asserted as an <b>exact equality</b> rather than as a band.
/// </summary>
/// <remarks>
/// <para>
/// <b>The exactness is a property of the schedule and it expires</b> (<c>plans/0033</c> <b>F5</b>).
/// Money's only source and sink is the Outside Connection, which is milestone <b>11</b>, so for the
/// whole of milestone 10 the supply is a constant and conservation is an equality rather than a sum
/// with a flow term. ⚠ <b>This is the only exact conservation assertion this project will ever
/// have</b>, and the reason to write it now is that the file it lives in becomes a band the day the
/// gate opens. ***Take the reading while it is exact.***
/// </para>
/// <para>
/// <b>The equality is checked through the production invariant, not restated here.</b> Every reading
/// calls <see cref="Simulation.CheckEndOfRun"/>, which runs
/// <c>WorldInvariants.MoneyIsConserved</c> — the same walk, the same anchor. A test that wrote
/// <c>total == issued</c> in its own words would agree with the code by construction and would pass
/// over a defect in the expression it was copying.
/// </para>
/// <para>
/// <b>Checking it 48 times rather than once is the point of the length.</b> An end-of-run check
/// passes over a run in which money leaked and came back — and a levy-and-rebate circuit is exactly
/// the shape that could do that, because the two halves are equal and opposite by construction. What
/// the interval readings add is <em>when</em>.
/// </para>
/// <para>
/// ⚠ <b>The circuit reaches half the city each Day, and it is a different half each time.</b> The
/// rebate pays a flat 100 out of a treasury holding one Day's levy, so it funds 172 of 360
/// Households and then stops — <c>adr/0035</c>'s refusal of an automatic overdraft arriving as
/// behaviour rather than as a refusal message. <c>PurposeTag.PolicyScanStart</c> is what makes the
/// half a fresh draw, and
/// <see cref="No_household_is_driven_destitute_and_the_spread_does_not_diverge"/> is the only thing
/// in the suite that would notice if that stagger were removed.
/// </para>
/// <para>
/// ⚠⚠ <b>And that is the finding, because the sum cannot see it.</b> Pinning the sweep start to a
/// constant leaves 172 Households at the ceiling and the rest at <b>zero</b> for the whole run — and
/// every conservation reading is byte-identical to the healthy run's. ***A conserved economy tells
/// you nothing about who holds it***, which is why the milestone's headline assertion is one of four
/// here rather than the whole file.
/// </para>
/// </remarks>
public sealed class MoneyLongRunTests(MoneyLongRun run) : IClassFixture<MoneyLongRun>
{
    /// <summary>
    /// The flat amount <c>rulesets/taxed.toml</c>'s rebate pays each Household, restated here because
    /// <see cref="The_treasury_is_a_conduit_rather_than_an_accumulator"/> is a claim about it.
    /// </summary>
    private const long RebateGrain = 100;

    /// <summary>
    /// How many readings the opening endowment is allowed to still be visible in.
    /// </summary>
    /// <remarks>
    /// <b>Two Days, and it is not a settling period — it is the endowment's own floor.</b>
    /// <c>rulesets/taxed.toml</c> states <c>opening_balance_min = 0</c>, so a Household can be
    /// <em>founded</em> holding nothing, and until the rebate has reached it the run is reporting the
    /// populator rather than the circuit. Everything after this is the circuit's own doing. ⚠ The sum
    /// needs no settling at all: it is exact from Tick 0, and only the <em>distribution</em> has a
    /// transient.
    /// </remarks>
    private const int Endowed = 2;

    /// <summary>
    /// The whole of task 9, and every assertion in it is about a different failure.
    /// </summary>
    /// <remarks>
    /// <b>The first two are the milestone's claim; the rest are what stop it being vacuous.</b> A run
    /// in which no Policy ever fired conserves money perfectly, and so does a run in which the
    /// treasury swallowed the city — so conservation on its own is the weakest true thing this test
    /// could say.
    /// </remarks>
    [Fact]
    public void The_hundred_thousand_Tick_acceptance_run()
    {
        MoneyLongRun.Reading[] readings = run.Readings;

        // 1. THE SUPPLY NEVER MOVED. adr/0031's equality is against MoneySupplyTable.Issued, and an
        //    equality against a moving anchor is two errors agreeing. Only World.Endow writes it and
        //    only the populator calls Endow, so a second distinct value here is a second issuer --
        //    which is the hole adr/0024 exists to close and milestone 11 is what may legally open.
        Assert.Single(Distinct(readings, r => r.Issued));

        // 2. AND SO DID THE CITY'S HOLDINGS. The invariant already ran at every reading, inside
        //    CheckEndOfRun, so this is not the equality again -- it is the stronger claim that the
        //    sum did not move AT ALL. A leak and a matching double-issue would satisfy the invariant
        //    at every reading and fail here.
        Assert.Single(Distinct(readings, r => r.Total));

        // 3. THE RUN DID SOMETHING, in both directions, on every Day of it. Reported unnetted for
        //    PolicyActivity's reason: a net cannot tell a city that taxed nothing and paid nothing
        //    from one that taxed heavily and paid it all back.
        foreach (MoneyLongRun.Reading reading in readings)
        {
            Assert.True(reading.ToTreasury > 0, $"the levy collected nothing at Tick {reading.Tick}.");
            Assert.True(reading.FromTreasury > 0, $"the rebate paid nothing at Tick {reading.Tick}.");
        }
    }

    /// <summary>
    /// The treasury carries less than one payment between Days, and it is derived rather than banded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0006</c>'s magnitude half, on the one balance in the city that could accumulate
    /// without anybody noticing.</b> The levy is a percentage and the rebate is flat, so nothing in
    /// the Ruleset makes the two equal — the treasury is emptied each Day only because the sweep pays
    /// until it cannot pay again, and what is left over is therefore strictly less than one payment.
    /// The bound is the rebate's own grain and not a number read off a run.
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="The_treasury_genuinely_empties_and_the_sweep_waits"/> is what stops this
    /// passing over a dead circuit.</b> A treasury that never received anything also holds less than
    /// 100.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_treasury_is_a_conduit_rather_than_an_accumulator()
    {
        foreach (MoneyLongRun.Reading reading in run.Readings)
        {
            Assert.True(
                reading.Treasury < RebateGrain,
                $"the treasury held {reading.Treasury} at Tick {reading.Tick}, which is a whole "
                + $"rebate payment or more. The sweep pays out until it cannot pay {RebateGrain} "
                + "again, so a carry that large is money the circuit stopped moving.");
        }
    }

    /// <summary>
    /// The treasury runs dry on every sweep, the sweep stops, and nothing overdraws.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0035</c> corrects <c>adr/0024</c> on exactly this</b> — <em>"borrowing is a player
    /// action"</em>, and an automatic damper <em>"delete[s] a decision the player should be
    /// making."</em> This is that refusal observed over 48 Days rather than argued: the payer is short
    /// on every one of them, the sweep abandons the rest of the population, and the money supply is
    /// unchanged by it. ⚠ <b>The counter is <c>Exhausted</c> and not <c>Unaffordable</c></b>, which
    /// is <c>PolicyActivity</c>'s distinction: a member owing nothing is not a treasury that cannot
    /// pay, and the two failures are counted apart because only one of them ends the sweep.
    /// </remarks>
    [Fact]
    public void The_treasury_genuinely_empties_and_the_sweep_waits()
    {
        foreach (MoneyLongRun.Reading reading in run.Readings)
        {
            Assert.True(
                reading.Exhausted > 0,
                $"the rebate funded every Household at Tick {reading.Tick}, so this Day never "
                + "reached the no-overdraft path and adr/0035's refusal went unobserved.");

            Assert.Equal(0, reading.Unaffordable);
        }
    }

    /// <summary>
    /// Nobody is driven destitute, and the spread of balances contracts rather than diverging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0006</c>'s magnitude half applied per entity rather than to a total</b>, and the
    /// total cannot see this: the city holds the same money whether every Household has an equal
    /// share of it or one Household has all of it. That is the failure a conserved economy makes easy
    /// and a conservation check cannot report.
    /// </para>
    /// <para>
    /// <b>The circuit is contractive and nothing in the Ruleset says so.</b> A levy of 10% and a
    /// rebate of a flat 100 have a fixed point at a balance of 1,000, but the rebate reaches only
    /// half the city each Day, so whether a Household converges depends entirely on the sweep start
    /// being a fresh draw — <c>PolicyEngine</c>'s <c>PurposeTag.PolicyScanStart</c>. Over 48 Days the
    /// opening 0–1,000 endowment pulls in to a band wandering between roughly 340 and 620 wide.
    /// </para>
    /// <para>
    /// ⚠ <b>The destitution assertion is earned by a counterfactual rather than by a band.</b>
    /// Pinning that draw to a constant and running the same 100,000 Ticks holds <c>Lowest</c> at
    /// <b>0</b> and <c>Highest</c> at <b>1,000</b> at every one of the 48 readings: the unreached
    /// half is levied to nothing and stays there for ever. So the floor is not a threshold anybody
    /// chose — it separates the two runs absolutely.
    /// </para>
    /// <para>
    /// ⚠ <b>There is deliberately no assertion that the spread narrows, because it does not.</b> It
    /// goes 996 → 820 → <b>888</b> → 341 and then wanders between roughly 340 and 700 for the rest of
    /// the run, with no trend under it: the poorest Household climbing and the richest climbing
    /// faster widens the band before the rotation pulls it in. Forty-eight Days is not long enough to
    /// converge and a bound tight enough to mean anything would have been read off this run rather
    /// than derived from the Ruleset. What <em>is</em> asserted is the weaker claim <c>adr/0006</c>
    /// actually makes — <b>it does not trend upward</b>.
    /// </para>
    /// <para>
    /// ⚠⚠ <b>And the sum is identical in both runs.</b> A city with half its Households destitute
    /// conserves money exactly as well as a healthy one, to the unit, at every reading. ***That is
    /// what a conservation invariant cannot see, and it is the reason this file holds four assertions
    /// rather than the one the task named.***
    /// </para>
    /// </remarks>
    [Fact]
    public void No_household_is_driven_destitute_and_the_spread_does_not_diverge()
    {
        MoneyLongRun.Reading[] readings = run.Readings;
        MoneyLongRun.Reading[] tail = readings[Endowed..];

        foreach (MoneyLongRun.Reading reading in tail)
        {
            Assert.True(
                reading.Lowest > 0,
                $"a Household held nothing at Tick {reading.Tick}. The rebate reaches half the city "
                + "a Day, so a Household never in that half is levied to zero and stays there -- "
                + "which is exactly what a sweep start that did not rotate produces.");
        }

        // adr/0006's magnitude half, per entity. One direction only, for RuleLongRunTests' reason: a
        // distribution that pulled together is not this test's business, and demanding symmetry
        // would fail a run whose last reading caught the band mid-wander.
        long early = MeanSpread(tail[..(tail.Length / 2)]);
        long late = MeanSpread(tail[(tail.Length / 2)..]);

        Assert.True(
            late <= early + (early / 8),
            $"the spread of Household balances averaged {early} over the first half of the run and "
            + $"{late} over the second. A conserved economy cannot get richer, so a widening band is "
            + "the city concentrating its money -- which the sum is blind to and this is not.");
    }

    /// <summary>The mean gap between the richest and poorest Household across some readings.</summary>
    private static long MeanSpread(MoneyLongRun.Reading[] readings)
    {
        long total = 0;

        foreach (MoneyLongRun.Reading reading in readings)
        {
            total += reading.Highest - reading.Lowest;
        }

        return total / readings.Length;
    }

    private static long[] Distinct(MoneyLongRun.Reading[] readings, Func<MoneyLongRun.Reading, long> of)
    {
        var seen = new SortedSet<long>();

        foreach (MoneyLongRun.Reading reading in readings)
        {
            seen.Add(of(reading));
        }

        return [.. seen];
    }
}

/// <summary>
/// The run, done once and read by every assertion in <see cref="MoneyLongRunTests"/>.
/// </summary>
/// <remarks>
/// <b>An <c>IClassFixture</c> because the run is the expensive part and the assertions are not.</b>
/// Four assertions over four separate 100,000-Tick runs would cost four times as much for the same
/// readings, and <c>TierBudgetTests</c> would be right to fail it.
/// </remarks>
public sealed class MoneyLongRun
{
    private const int Ticks = 100_000;
    private const int Population = 1_000;

    /// <summary>
    /// The interval a reading covers. <b>Exactly <c>rulesets/taxed.toml</c>'s Policy interval</b>, so
    /// each reading holds one trigger of the levy and one of the rebate, and the flow figures compare
    /// across readings without a phase term.
    /// </summary>
    private const int ReadEvery = 2_048;

    /// <summary>Every reading of the run, in Tick order.</summary>
    public Reading[] Readings { get; } = Run();

    /// <summary>What one reading holds. One walk of the Bins, plus one drain of the Policies.</summary>
    public readonly record struct Reading(
        int Tick,
        long Total,
        long Issued,
        long Treasury,
        long ToTreasury,
        long FromTreasury,
        long Exhausted,
        long Unaffordable,
        long Lowest,
        long Highest);

    /// <summary>
    /// The run itself, on <c>rulesets/taxed.toml</c> as the runner loads it.
    /// </summary>
    /// <remarks>
    /// <b>The shipped file rather than a fixture, which is the opposite of <c>PolicyTests</c>'
    /// choice and for the opposite reason.</b> A unit test authors its Ruleset inline so the
    /// assertion states its own premises; an acceptance run has to be able to fail when the shipped
    /// content changes, because that is what it is accepting.
    /// </remarks>
    private static Reading[] Run()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "taxed.toml"));

        Assert.True(result.Ok, $"rulesets/taxed.toml was refused:\n  {result.Describe()}");

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, result.Ruleset!, key);

        var simulation = new Simulation(world, key)
        {
            // O(world) twice per Tick against a phase meant to be O(woken). RuleLongRunTests says
            // why the long runs turn it off; the guard's own correctness is covered where it lives.
            VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, new Core.Quantities.Ticks(0));

        Assert.True(
            world.Households.Rows.LiveCount > 0,
            "the populator built no Households, so every reading below is over an empty city.");

        List<Reading> readings = [];

        for (int tick = 0; tick < Ticks; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % ReadEvery != 0)
            {
                continue;
            }

            // The production tier, on the Simulation that ran rather than a fresh one over the same
            // world -- RuleLongRunTests found the tick stamp. This is what makes the conservation
            // claim 48 checks instead of one, and it throws, so reaching the next line passes.
            simulation.CheckEndOfRun();

            MoneyLedger ledger = MoneyLedger.Of(world);
            PolicyActivity policy = simulation.Policies.Drain();
            (long lowest, long highest) = Balances(world);

            readings.Add(new Reading(
                tick + 1,
                ledger.Total,
                world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw,
                ledger.Treasury,
                policy.ToTreasury.Sum,
                policy.FromTreasury.Sum,
                policy.Exhausted.Sum,
                policy.Unaffordable.Sum,
                lowest,
                highest));
        }

        return [.. readings];
    }

    /// <summary>The poorest and richest Household in the city, by the Bin each one names.</summary>
    private static (long Lowest, long Highest) Balances(World world)
    {
        long lowest = long.MaxValue;
        long highest = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot)
                || !world.Bins.Rows.TryResolve(world.Households.Balance[slot], out int balance))
            {
                continue;
            }

            long level = world.Bins.LevelAt(balance);

            lowest = Math.Min(lowest, level);
            highest = Math.Max(highest, level);
        }

        return (lowest, highest);
    }
}
