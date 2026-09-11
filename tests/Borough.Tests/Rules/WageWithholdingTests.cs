using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Borough.Tests.Persistence;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// <b>Citizen income tax, withheld where the wage is paid.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0072</c>, amnesty row 33. <see cref="IncomeTaxTests"/> owns the schedule arithmetic on
/// its own; these run the same claims through <c>WageEngine.Pay</c>, where the money actually
/// moves — <b>one withdrawal of the gross against two deposits that sum to it exactly</b>.
/// </para>
/// <para>
/// ⚠ <b>Most of them run the payday themselves rather than letting
/// <see cref="Simulation.Step"/> reach it, and that is what makes an exact figure assertable at
/// all.</b> A stepped Tick moves money for a dozen other reasons — a market clears, a Household
/// shops, a Business is founded — so a treasury that rose by the tax could not be told from a
/// treasury that rose by the tax and a levy. The fixture still <em>steps</em> to build the city
/// (see <see cref="WarmDays"/>); what it does not do is step across the payday it is measuring. The
/// two tests that do step whole runs assert bounds rather than amounts.
/// </para>
/// <para>
/// 🔴 <b>The fixture leaves exactly ONE PAID job in the city</b>, so every figure a payday reports
/// belongs to a worker whose rate, employer and earning Days the test chose. Every other Business
/// still has its payday swept; none of them has anybody it must pay.
/// </para>
/// </remarks>
public sealed class WageWithholdingTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const int Population = 2_000;

    /// <summary>
    /// How long the fixture runs before it hires anybody.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A populated world posts no wage at all, and this is why the fixture steps.</b>
    /// <c>SyntheticCity</c> raises <c>waged.toml</c>'s dwellings, but the trade that pays a wage is
    /// <em>founded</em> — a Citizen spends part of their Household's balance to capitalise it — and
    /// founding happens on a Tick. So a world at Tick 0 holds plenty of Businesses and not one of
    /// them posts a rate; a couple of Days in, a handful do, each with a till. ***A fixture that
    /// asserted against Tick 0 would be asserting against a city with no employer in it.***
    /// </remarks>
    private const int WarmDays = 2;

    /// <summary>
    /// The schedule these read against, appended to <c>rulesets/waged.toml</c>.
    /// </summary>
    /// <remarks>
    /// <b>The allowance is deliberately about HALF a Day's wage rather than a rounding error against
    /// it</b>, which is what makes granting it twice visible. <c>waged.toml</c> posts
    /// <c>wage_per_day = 4096</c> and this file's <c>[jobs]</c> grades nothing, so a Day's gross is
    /// 4,096 against an allowance of 2,000 — and a schedule handing that allowance out a second time
    /// changes the figure by hundreds rather than by units. ⚠ <b>The four numbers are a FIXTURE and
    /// not content</b>: <c>rulesets/taxed.toml</c> owns the authored schedule and its argument.
    /// </remarks>
    private const string Banded = """


        [income_tax]
        allowance_per_day       = 2000
        upper_threshold_per_day = 3000
        middle_rate_percent     = 10
        upper_rate_percent      = 50
        """;

    /// <summary>A second schedule, harsher in both bands, for the Days a rate change reaches.</summary>
    private static readonly IncomeTaxSchedule Harsher = new(
        AllowancePerDay: 500,
        UpperThresholdPerDay: 1_000,
        MiddleRatePercent: 40,
        UpperRatePercent: 80);

    // ---- the split -------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>One debit against two credits that sum to it exactly.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0072</c> D5: withholding MOVES Money and never destroys it.</b> The alternative
    /// the split exists to refuse is the treasury collecting separately from a Household that has
    /// already been paid in full — which collects nothing from a Household that has already spent
    /// it, and makes the tax a function of shopping habits.
    /// </remarks>
    [Fact]
    public void A_wage_payment_splits_into_two_credits_that_sum_to_the_debit()
    {
        Payroll payroll = Start(period: 1, Banded);
        World world = payroll.World;

        SetTill(payroll, 4 * payroll.Rate);

        long tillBefore = world.Bins.LevelAt(payroll.Till);
        long purseBefore = world.Bins.LevelAt(payroll.Purse);
        long treasuryBefore = world.Bins.LevelAt(payroll.Treasury);
        Money issuedBefore = world.MoneySupply.Issued[MoneySupplyTable.Slot];

        PayrollReading reading = Sweep(payroll, WarmDays + 1);

        long gross = payroll.Rate;
        long tax = IncomeTax.DueOn(gross, world.Rules.IncomeTax);

        _output.WriteLine($"gross {gross}, withheld {tax}, take-home {gross - tax}.");

        Assert.True(tax > 0, "the schedule took nothing, so this test asserts nothing.");
        Assert.Equal(gross, reading.Paid);
        Assert.Equal(1, reading.Workers);
        Assert.Equal(tax, reading.Withheld);

        long tillAfter = world.Bins.LevelAt(payroll.Till);
        long purseAfter = world.Bins.LevelAt(payroll.Purse);
        long treasuryAfter = world.Bins.LevelAt(payroll.Treasury);

        // The till gave up the GROSS -- not the take-home, which is the whole of the decision.
        Assert.Equal(gross, tillBefore - tillAfter);
        Assert.Equal(gross - tax, purseAfter - purseBefore);
        Assert.Equal(tax, treasuryAfter - treasuryBefore);

        // And the two credits sum to the one debit, stated as its own line rather than left to be
        // inferred from the three above.
        Assert.Equal(
            tillBefore - tillAfter,
            (purseAfter - purseBefore) + (treasuryAfter - treasuryBefore));

        // A transfer, so nothing was issued and nothing was written down.
        Assert.Equal(issuedBefore, world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b><c>Invariant.MoneyIsConserved</c> stays green across a run with withholding live.</b>
    /// </summary>
    /// <remarks>
    /// <b>The stepped counterpart to the test above, and it asserts a bound rather than an amount.</b>
    /// A whole city is paying, shopping and trading, so no figure here is attributable to the tax;
    /// what is asserted is that the supply of record did not move at all, which is the one thing a
    /// withholding that minted or burnt Money could not leave true.
    /// </remarks>
    [Fact]
    public void Withholding_conserves_money_across_a_run()
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, Rules(period: 7, Banded), key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        Money issued = world.MoneySupply.Issued[MoneySupplyTable.Slot];
        long withheld = 0;
        long paid = 0;

        for (int tick = 0; tick < 30 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            withheld += simulation.LastPayroll.Withheld;
            paid += simulation.LastPayroll.Paid;
        }

        _output.WriteLine($"{paid} paid over 30 Days, {withheld} of it withheld.");

        Assert.True(paid > 0, "no wage was ever paid, so this test asserts nothing.");
        Assert.True(withheld > 0, "nothing was ever withheld, so this test asserts nothing.");
        Assert.True(withheld < paid, "the treasury took at least the whole payroll.");

        // Withholding is a transfer. adr/0142's supply is what would catch it minting or burning.
        Assert.Equal(issued, world.MoneySupply.Issued[MoneySupplyTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>A world with no <c>[income_tax]</c> is the city every Ruleset shipped before the table
    /// existed, and it must be that city byte for byte.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The accumulator assertion is the load-bearing one and it is about the STATE HASH.</b>
    /// <c>WageEngine.Pay</c> asks <c>IncomeTaxRates.Levies</c> once per employer and the answer
    /// guards the <em>writes</em> rather than only the arithmetic — because
    /// <see cref="CitizenTable.TaxedDay"/> and <see cref="CitizenTable.TaxedGross"/> are saved and
    /// hashed, so a Day and a zero written into every worker on every payday would move the hash of
    /// every untaxed world in the corpus for a tax it does not have.
    /// </remarks>
    [Fact]
    public void A_world_with_no_income_tax_withholds_nothing()
    {
        Payroll payroll = Start(period: 1, incomeTax: "");
        World world = payroll.World;

        Assert.False(world.Rules.IncomeTax.Levies);

        SetTill(payroll, 4 * payroll.Rate);

        long purseBefore = world.Bins.LevelAt(payroll.Purse);
        long treasuryBefore = world.Bins.LevelAt(payroll.Treasury);

        PayrollReading reading = Sweep(payroll, WarmDays + 1);

        Assert.Equal(payroll.Rate, reading.Paid);
        Assert.Equal(0, reading.Withheld);

        // The whole gross, and nothing at all for the treasury.
        Assert.Equal(payroll.Rate, world.Bins.LevelAt(payroll.Purse) - purseBefore);
        Assert.Equal(treasuryBefore, world.Bins.LevelAt(payroll.Treasury));

        // And the accumulator was not touched, on any Citizen in the city.
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.Equal(0, world.Citizens.TaxedDay[slot]);
            Assert.Equal(0, world.Citizens.TaxedGross[slot]);
        }

        world.Invariants.RunEndOfRun(world);
    }

    // ---- the Day, and the allowance that belongs to it once --------------------------------------

    /// <summary>
    /// 🔴 <b>One earning Day paid in two instalments is granted the allowance ONCE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>plans/0072</c> D5, through the shipped mechanism rather than through
    /// <c>IncomeTax.WithholdingOn</c> alone.</b> The first payday finds a till too poor to cover the
    /// Day, so it hands over part of it and — because <c>WageEngine.Pay</c> advances
    /// <see cref="CitizenTable.LastPaidDay"/> only over whole Days covered — leaves that Day
    /// claimable. The second payment therefore closes a Day that has already been taxed, and
    /// <see cref="CitizenTable.TaxedGross"/> is what stops the allowance being handed out again.
    /// </para>
    /// <para>
    /// ⚠ <b>THE PAY PERIOD IS LENGTHENED BETWEEN THE TWO PAYDAYS, AND WITHOUT THAT THE CASE IS
    /// UNREACHABLE.</b> <c>Pay</c>'s arrears cap forfeits anything older than one period and moves
    /// <c>LastPaidDay</c> up to <c>today - period</c> when it fires — and after a part payment the
    /// gap to the <em>next</em> payday is always exactly one Day more than the period, so the cap
    /// always fires and the part-paid Day is always forfeited before a second instalment can reach
    /// it. A hot reload under <c>adr/0015</c> is the one shipped thing that puts two paydays inside
    /// one period. ***See the report on this row: on a fixed Ruleset the accumulator is dead.***
    /// </para>
    /// </remarks>
    [Fact]
    public void One_earning_day_paid_in_two_instalments_is_granted_the_allowance_once()
    {
        // The employer has to be one whose payday under the LATER period falls soon enough after
        // Day 1 that the arrears cap does not fire -- see the remarks.
        Payroll payroll = Start(
            period: 1,
            Banded,
            employer: (world, key, slot) =>
                NextPayday(world, key, slot, period: 4, after: WarmDays + 1) <= WarmDays + 4);

        World world = payroll.World;
        IncomeTaxSchedule schedule = world.Rules.IncomeTax;

        // Poor enough to cover a bit over half a Day and no more.
        long part = (payroll.Rate * 3) / 5;
        SetTill(payroll, part);

        PayrollReading first = Sweep(payroll, WarmDays + 1);

        Assert.Equal(part, first.Paid);
        Assert.Equal(IncomeTax.DueOn(part, schedule), first.Withheld);

        // The Day is recorded as part-paid, and the clock did NOT move past it.
        Assert.Equal(WarmDays + 1, world.Citizens.TaxedDay[payroll.Worker]);
        Assert.Equal(part, world.Citizens.TaxedGross[payroll.Worker]);
        Assert.Equal(WarmDays, world.Citizens.LastPaidDay[payroll.Worker]);

        // The reload: a longer period, so the next payday lands inside the arrears window.
        world.Adopt(Rules(period: 4, Banded), 2, world.Tick, world.Key);

        long second = NextPayday(
            world, world.Key, payroll.Employer, period: 4, after: WarmDays + 1);

        Assert.InRange(second, WarmDays + 2, WarmDays + 4);

        long days = second - WarmDays;

        SetTill(payroll, days * payroll.Rate);

        PayrollReading paid = Sweep(payroll, second);

        // Every Day but the first is a fresh full Day at the posted rate.
        long fresh = (days - 1) * IncomeTax.DueOn(payroll.Rate, schedule);
        long onDayOne = paid.Withheld - fresh;

        _output.WriteLine(
            $"Day 1 took {first.Withheld} on {part}, then {onDayOne} on a further {payroll.Rate}; "
            + $"the other {days - 1} Days took {fresh}.");

        Assert.Equal(days * payroll.Rate, paid.Paid);

        // 🔴 THE CLAIM. What the Day paid in two instalments cost, in total, is what one assessment
        // of the whole Day's gross costs -- and the allowance appears in it once.
        Assert.Equal(
            IncomeTax.DueOn(part + payroll.Rate, schedule),
            first.Withheld + onDayOne);

        // And the difference is the decision: assessing each instalment fresh would grant the
        // allowance twice and cost the treasury this much.
        Assert.True(
            IncomeTax.DueOn(part, schedule) + IncomeTax.DueOn(payroll.Rate, schedule)
                < IncomeTax.DueOn(part + payroll.Rate, schedule),
            "the schedule cannot tell one assessment of the total from two of the parts, so this "
            + "test would pass on a mechanism that granted the allowance twice.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// 🔴 <b>On a Ruleset nobody retunes, the arrears cap reaches a part-paid Day before a second
    /// instalment can — so the accumulator above is never read back.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the finding the test above works around, pinned rather than described.</b> After a
    /// part payment <c>LastPaidDay</c> sits one Day behind the payday, so the gap to the next payday
    /// is <em>period + 1</em> — one more than the cap allows, every time and whatever the period.
    /// The cap therefore fires on every payday that follows a short one, moves the clock past the
    /// part-paid Day, and the walk starts on the Day after it. ***The Day is assessed on what was
    /// actually handed over and never again.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is correct and it is not obviously intended.</b> The forfeiture is <c>adr/0006</c>'s
    /// sink working as written — unpaid work must not accrue for ever — and the consequence for the
    /// tax is that <see cref="CitizenTable.TaxedGross"/> is written on every payday and read back on
    /// none of them. <b>Filed in the report for this row rather than repaired here.</b>
    /// </para>
    /// </remarks>
    [Fact]
    public void The_arrears_cap_reaches_a_part_paid_day_before_a_second_instalment_can()
    {
        Payroll payroll = Start(period: 1, Banded);
        World world = payroll.World;
        IncomeTaxSchedule schedule = world.Rules.IncomeTax;

        long part = (payroll.Rate * 3) / 5;
        SetTill(payroll, part);

        PayrollReading first = Sweep(payroll, WarmDays + 1);

        Assert.Equal(part, first.Paid);
        Assert.Equal(WarmDays + 1, world.Citizens.TaxedDay[payroll.Worker]);
        Assert.Equal(WarmDays, world.Citizens.LastPaidDay[payroll.Worker]);

        // Rich enough to settle everything it is asked for, so nothing here is about the money.
        SetTill(payroll, 8 * payroll.Rate);

        PayrollReading second = Sweep(payroll, WarmDays + 2);

        // One Day's wage and not two: the cap forfeited what the short payday left owing.
        Assert.Equal(payroll.Rate, second.Paid);
        Assert.Equal(WarmDays + 1, world.Citizens.LastPaidDay[payroll.Worker] - 1);

        // And it is assessed as a whole fresh Day, with the allowance granted to it in full --
        // which is only right because the Day it left behind will never be paid again.
        Assert.Equal(IncomeTax.DueOn(payroll.Rate, schedule), second.Withheld);
        Assert.Equal(WarmDays + 2, world.Citizens.TaxedDay[payroll.Worker]);
        Assert.Equal(payroll.Rate, world.Citizens.TaxedGross[payroll.Worker]);

        _output.WriteLine(
            $"Day {WarmDays + 1} was paid {part} of {payroll.Rate} and taxed {first.Withheld}; "
            + $"the rest of it was forfeited and Day {WarmDays + 2} took {second.Withheld}.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>A payment covering several Days is taxed a Day at a time, and never as one lump.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0072</c> D4: the thresholds are earnings per Day.</b> A week paid together at a
    /// weekly period would otherwise read as one enormous Day — one allowance for the week, and the
    /// upper band reached on a wage that never approaches it. ⚠ <b>The inequality is the assertion
    /// that matters</b>; the equality above it would hold on a mechanism that spread the payment
    /// over the wrong number of Days.
    /// </remarks>
    [Fact]
    public void A_multi_day_payment_is_taxed_a_day_at_a_time()
    {
        Payroll payroll = Start(period: 7, Banded);
        World world = payroll.World;
        IncomeTaxSchedule schedule = world.Rules.IncomeTax;

        long payday = NextPayday(
            world, world.Key, payroll.Employer, period: 7, after: WarmDays + 7);

        // A full period owed, and a till that can cover all of it.
        world.Citizens.LastPaidDay[payroll.Worker] = (ushort)(payday - 7);
        SetTill(payroll, 7 * payroll.Rate);

        PayrollReading reading = Sweep(payroll, payday);

        long perDay = 7 * IncomeTax.DueOn(payroll.Rate, schedule);
        long asOneDay = IncomeTax.DueOn(7 * payroll.Rate, schedule);

        _output.WriteLine(
            $"seven Days at {payroll.Rate} paid together on Day {payday}: {reading.Withheld} "
            + $"withheld a Day at a time against {asOneDay} as one lump.");

        Assert.Equal(7 * payroll.Rate, reading.Paid);
        Assert.Equal(perDay, reading.Withheld);

        // 🔴 The decision. A lump reaches the upper band a Day's earnings never would.
        Assert.True(
            perDay < asOneDay,
            $"a Day at a time took {perDay} and one lump would take {asOneDay}, so this schedule "
            + "cannot tell the two apart and the test asserts nothing.");

        world.Invariants.RunEndOfRun(world);
    }

    // ---- the rate change -------------------------------------------------------------------------

    /// <summary>
    /// <b>A wage paid late is taxed at the schedule in force on the Day it was EARNED.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0072</c> D6: a rate change is prospective.</b> The window is split on purpose
    /// rather than governed after every Day it covers — a schedule that reached the whole payment
    /// and one that reached none of it are both wrong, and only a split window tells them apart.
    /// </remarks>
    [Fact]
    public void A_late_payment_is_taxed_at_the_schedule_of_the_day_it_was_earned()
    {
        Payroll payroll = Start(period: 7, Banded);
        World world = payroll.World;
        IncomeTaxSchedule authored = world.Rules.IncomeTax;

        long payday = NextPayday(
            world, world.Key, payroll.Employer, period: 7, after: WarmDays + 7);

        world.Citizens.LastPaidDay[payroll.Worker] = (ushort)(payday - 7);
        SetTill(payroll, 7 * payroll.Rate);

        // The payment covers the Days payday-6 .. payday. The change reaches the last three of them.
        world.IncomeTaxRates.Govern(payday - 2, Harsher);

        PayrollReading reading = Sweep(payroll, payday);

        long old = IncomeTax.DueOn(payroll.Rate, authored);
        long @new = IncomeTax.DueOn(payroll.Rate, Harsher);
        long expected = (4 * old) + (3 * @new);

        _output.WriteLine(
            $"Days {payday - 6}..{payday - 3} at {old} each and {payday - 2}..{payday} at {@new}: "
            + $"{reading.Withheld} withheld.");

        Assert.Equal(expected, reading.Withheld);

        // Neither schedule reached the whole payment, which is what makes the figure above a split.
        Assert.NotEqual(7 * old, reading.Withheld);
        Assert.NotEqual(7 * @new, reading.Withheld);

        world.Invariants.RunEndOfRun(world);
    }

    // ---- the treasury ----------------------------------------------------------------------------

    /// <summary>
    /// <b>A Business with a till always has somewhere to withhold into</b>, which is what makes
    /// <c>Pay</c>'s <c>NoSlot</c> branch unreachable rather than untested.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The brief for this row asked for a Business whose money Resource has no treasury Bin,
    /// and no world can be built in that state.</b> <c>World.FitTreasury</c> opens a treasury Bin
    /// for every <em>conserved</em> Resource, <c>Ruleset.IsConserved</c> is exactly
    /// <c>ResourceFamily.Money</c>, and it runs both in the constructor and on every
    /// <c>World.Adopt</c> — so a till, which is a money Bin by construction, always finds one.
    /// </para>
    /// <para>
    /// ⚠ <b>Asserted as a positive claim rather than pinned as a dead branch.</b> The day something
    /// makes the branch reachable — a Resource family that is money and not conserved, a treasury
    /// Bin that can be retired — this fails and names the assumption that changed, which is the
    /// whole of what a test over an unreachable state can be worth.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_business_till_always_has_a_treasury_bin_to_withhold_into()
    {
        Payroll payroll = Start(period: 1, Banded);
        World world = payroll.World;

        int checked_ = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || !world.Bins.Rows.TryResolve(world.Businesses.Balance[slot], out int till))
            {
                continue;
            }

            Assert.True(world.Rules.IsConserved(world.Bins.Resource[till]));
            Assert.NotEqual(Rows.NoSlot, world.FindTreasuryBin(world.Bins.Resource[till]));

            checked_++;
        }

        _output.WriteLine($"{checked_} tills, every one of them with a treasury Bin to pay into.");

        Assert.True(checked_ > 0, "no Business held a till, so this test asserts nothing.");
    }

    // ---- the save --------------------------------------------------------------------------------

    /// <summary>
    /// <b>The Factorio test over the accumulator and the schedule ring.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>05 §4</c> invariant 6, pointed at this row's new saved columns.</b>
    /// <see cref="CitizenTable.TaxedDay"/>, <see cref="CitizenTable.TaxedGross"/> and every column of
    /// <see cref="IncomeTaxTable"/> are <c>(saved AND hashed)</c>, so a column the writer misses is a
    /// world that comes back owing a different tax — invisible at the instant of the load, because
    /// the hash agrees on what was written, and visible only once the reloaded world has run a
    /// payday. ⚠ <b>The run is stepped in lockstep and compared every Tick</b>, on
    /// <c>FactorioTests</c>' reason: a divergence compared only at the end names the wrong Tick.
    /// </remarks>
    [Fact]
    public void A_saved_and_reloaded_world_keeps_the_withholding_accumulator()
    {
        const int N = 12 * Ticks.PerDay;
        const int M = 2 * Ticks.PerDay;

        Ruleset rules = Rules(period: 7, Banded);

        (World control, Simulation controlRun) = Stepped(rules, N);
        (World subject, Simulation subjectRun) = Stepped(rules, N);

        // Not vacuous: somebody has to have been taxed by the time the save is taken.
        Assert.True(
            Taxed(control) > 0,
            $"no Citizen carried a withholding accumulator after {N} Ticks, so the save covers "
            + "nothing this row added.");

        var file = new MemorySave();
        subjectRun.SaveAtEndOfTick(file);
        subjectRun.Step(default);
        controlRun.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        Assert.Equal(control.HashState(), reloaded.HashState());

        // The ring came back, and it came back saying what the schedule change said.
        Assert.Equal(Harsher, reloaded.IncomeTaxRates.ScheduleFor(5, rules.IncomeTax));

        for (int tick = 0; tick < M; tick++)
        {
            controlRun.Step(default);
            resumed.Step(default);

            Assert.Equal(
                (object)$"tick {N + 1 + tick}: {control.HashState():X16}",
                $"tick {N + 1 + tick}: {reloaded.HashState():X16}");
        }

        _output.WriteLine(
            $"saved at {N}, ran on {M}, {file.Bytes.Length:N0} B, "
            + $"{Taxed(control)} Citizens carrying an accumulator.");
    }

    /// <summary>How many live Citizens are carrying a part-taxed earning Day.</summary>
    private static int Taxed(World world)
    {
        int carrying = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot) && world.Citizens.TaxedGross[slot] != 0)
            {
                carrying++;
            }
        }

        return carrying;
    }

    /// <summary>
    /// A populated world with a schedule change already governed, stepped for
    /// <paramref name="ticks"/>.
    /// </summary>
    /// <remarks>
    /// <b>The change is governed before the first Tick so that both worlds carry it identically.</b>
    /// A ring stamped on one side only would be a divergence the test manufactured.
    /// </remarks>
    private static (World World, Simulation Simulation) Stepped(Ruleset rules, int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.IncomeTaxRates.Govern(5, Harsher);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }

    // ---- the fixture -----------------------------------------------------------------------------

    /// <summary>
    /// One employer, one worker, and the three Bins a payday moves money between.
    /// </summary>
    private sealed record Payroll(
        World World,
        WageEngine Wages,
        int Employer,
        int Worker,
        int Till,
        int Purse,
        int Treasury,
        long Rate);

    /// <summary>Runs one payday, at the Day boundary of <paramref name="day"/>.</summary>
    private static PayrollReading Sweep(Payroll payroll, long day) =>
        payroll.Wages.Sweep(new Ticks((ulong)(day * Ticks.PerDay)));

    /// <summary>
    /// A populated city in which exactly one Citizen has a job.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The city hires for itself while it warms up, so the fixture DISMISSES before it hires.</b>
    /// <c>EmploymentEngine</c> has fifteen hundred Citizens in work two Days in — but all but a
    /// handful of them work for the kind that posts no wage, and <c>WageEngine.Sweep</c> skips a
    /// trade with no rate entirely. Only the workers of a trade that <em>pays</em> would show up in
    /// a payday's figures, and those are the ones this lets go.
    /// </para>
    /// <para>
    /// ⚠ <b><c>World.Employ</c> starts the pay clock on the Day of hire</b>, so the worker is owed
    /// from <see cref="WarmDays"/> and every Day a test names is counted from there.
    /// </para>
    /// <para>
    /// <b><paramref name="employer"/> narrows the search rather than the assertion.</b> A payday is
    /// staggered per Business, so a test that needs one to fall on a particular Day picks the
    /// Business rather than moving the Day.
    /// </para>
    /// </remarks>
    private static Payroll Start(
        int period, string incomeTax, Func<World, WorldKey, int, bool>? employer = null)
    {
        Ruleset rules = Rules(period, incomeTax);
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < WarmDays * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        int trader = Rows.NoSlot;
        int till = Rows.NoSlot;
        BusinessKindDefinition trade = default;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!Pays(world, rules, slot, out int bin)
                || (employer is not null && !employer(world, key, slot)))
            {
                continue;
            }

            trader = slot;
            till = bin;
            trade = rules.BusinessKind(world.Businesses.Kind[slot]);
            break;
        }

        Assert.True(trader != Rows.NoSlot, "no Business in this city posts a wage against a till.");

        Empty(world, rules);

        int worker = Rows.NoSlot;
        int purse = Rows.NoSlot;
        long rate = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot)
                || !world.Citizens.Workplace[slot].IsNone
                || world.Citizens.TaxedGross[slot] != 0
                || !world.Households.Rows.TryResolve(
                    world.Citizens.HouseholdOf[slot], out int household)
                || !world.Bins.Rows.TryResolve(world.Households.Balance[household], out int bin))
            {
                continue;
            }

            long graded = WorkSchedule.Graded(world, slot, trade.WagePerDay);

            if (graded <= 0)
            {
                continue;
            }

            worker = slot;
            purse = bin;
            rate = graded;
            break;
        }

        Assert.True(worker != Rows.NoSlot, "no Citizen in this city could take a job.");

        world.Employ(
            world.Citizens.Rows.At(worker), world.Businesses.Rows.At(trader), Ticks.Zero);

        // Exactly one paid job in the city, which is what makes a payday's figures readable.
        Assert.Equal(1, Waged(world, rules));
        Assert.Equal(WarmDays, world.Citizens.LastPaidDay[worker]);

        int treasury = world.FindTreasuryBin(world.Bins.Resource[till]);

        Assert.NotEqual(Rows.NoSlot, treasury);

        return new Payroll(
            world, new WageEngine(world, key), trader, worker, till, purse, treasury, rate);
    }

    /// <summary>Whether this Business posts a wage and holds a till to pay it out of.</summary>
    private static bool Pays(World world, Ruleset rules, int slot, out int till)
    {
        till = Rows.NoSlot;

        if (!world.Businesses.Rows.IsLive(slot))
        {
            return false;
        }

        byte kind = world.Businesses.Kind[slot];

        if (kind == 0 || kind > rules.BusinessKindCount)
        {
            return false;
        }

        BusinessKindDefinition trade = rules.BusinessKind(kind);

        return trade.WagePerDay > 0 && trade.PayPeriodDays > 0
            && world.Bins.Rows.TryResolve(world.Businesses.Balance[slot], out till);
    }

    /// <summary>Dismisses every Citizen working for a trade that posts a wage.</summary>
    /// <remarks>
    /// <b>The list is collected before anybody is let go.</b> <c>World.Dismiss</c> takes the Citizen
    /// off the very list being walked, and an intrusive list rewritten under its own walk is the
    /// shape of a bug that reads as a flaky fixture.
    /// </remarks>
    private static void Empty(World world, Ruleset rules)
    {
        List<int> letting = [];

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!Pays(world, rules, slot, out _))
            {
                continue;
            }

            foreach (int worker in world.Workers.Walk(slot))
            {
                letting.Add(worker);
            }
        }

        foreach (int worker in letting)
        {
            world.Dismiss(world.Citizens.Rows.At(worker));
        }

        Assert.Equal(0, Waged(world, rules));
    }

    /// <summary>How many Citizens hold a job that pays a wage.</summary>
    private static int Waged(World world, Ruleset rules)
    {
        int held = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!Pays(world, rules, slot, out _))
            {
                continue;
            }

            foreach (int worker in world.Workers.Walk(slot))
            {
                _ = worker;
                held++;
            }
        }

        return held;
    }

    /// <summary>
    /// Moves the employer's till to exactly <paramref name="target"/>, conserving Money.
    /// </summary>
    /// <remarks>
    /// <b>Through <c>World.Endow</c> and a transfer rather than a write into the column</b>, so
    /// <c>Invariant.MoneyIsConserved</c> stays checkable across the setup as well as across the
    /// payday — <c>MoneyConservationTests</c>' <c>Poke</c> is the shape this is not.
    /// </remarks>
    private static void SetTill(Payroll payroll, long target)
    {
        World world = payroll.World;
        long level = world.Bins.LevelAt(payroll.Till);

        if (level > target)
        {
            world.Withdraw(world.Bins.Rows.At(payroll.Till), level - target, world.Tick);
            world.Deposit(world.Bins.Rows.At(payroll.Treasury), level - target, world.Tick);

            return;
        }

        if (level == target)
        {
            return;
        }

        long missing = target - level;
        Handle<Household> household = world.Citizens.HouseholdOf[payroll.Worker];

        world.Endow(household, new Money(missing));
        world.Withdraw(
            world.Households.Balance[world.Households.Rows.Resolve(household)],
            missing,
            world.Tick);
        world.Deposit(world.Bins.Rows.At(payroll.Till), missing, world.Tick);
    }

    /// <summary>
    /// The first Day after <paramref name="after"/> on which this Business's payday comes round.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It mirrors <c>WageEngine.IsPayday</c> and it is a fixture rather than an assertion</b> —
    /// nothing here checks that the payday is on the right Day; what it does is let a test that
    /// needs a payday say <em>which</em> one it is talking about. The offset is derived from the
    /// Business's monotonic id and never stored, so there is nothing on the row to read instead.
    /// </remarks>
    private static long NextPayday(World world, WorldKey key, int business, int period, long after)
    {
        ulong offset = Randomness.Draw(
            key, world.Businesses.Rows.IdAt(business), new Ticks(0), PurposeTag.WagePayday)
            % (ulong)period;

        for (long day = after + 1; day <= after + period; day++)
        {
            if ((day + (long)offset) % period == 0)
            {
                return day;
            }
        }

        Assert.Fail($"business {business} has no payday in the {period} Days after {after}.");

        return 0;
    }

    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "waged.toml"));

    /// <summary><c>rulesets/waged.toml</c>, retimed, with a schedule appended.</summary>
    private static Ruleset Rules(int period, string incomeTax)
    {
        RulesetLoadResult parsed = RulesetLoader.Parse(
            Text().Replace(
                "pay_period_days = 7", $"pay_period_days = {period}", StringComparison.Ordinal)
            + incomeTax,
            "waged-taxed.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        return parsed.Ruleset!;
    }
}
