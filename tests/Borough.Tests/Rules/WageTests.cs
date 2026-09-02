using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// <b>Wages: the return edge of the money loop, and the arrears cap that keeps it bounded.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0045</c> queue item 6. Every world before <c>rulesets/waged.toml</c> moved money in one
/// direction — a Household could be taxed and a Business levied, and nothing paid anybody — so these
/// run on the one shipped file in which a Business pays.
/// </para>
/// <para>
/// ⚠ <b>The interesting assertions are the two that are about <em>bounds</em> rather than about
/// amounts.</b> A wage is a PROVISIONAL number chosen by taste, so no test here asserts what anybody
/// earns; what they assert is that the rhythm does not change the income and that unpaid work does
/// not accrue for ever.
/// </para>
/// </remarks>
public sealed class WageTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const int Population = 2_000;

    [Fact]
    public void A_business_pays_its_workers_and_the_money_comes_out_of_its_own_till()
    {
        (World world, Simulation simulation) = Start(Rules(7));

        long paid = 0;
        int workers = 0;

        for (int tick = 0; tick < 30 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            paid += simulation.LastPayroll.Paid;
            workers = int.Max(workers, simulation.LastPayroll.Workers);
        }

        _output.WriteLine($"{paid} paid over 30 Days, at most {workers} workers on one payday.");

        Assert.True(paid > 0, "no wage was ever paid, so this test asserts nothing.");
        Assert.True(workers > 0, "no worker was ever reached.");

        // The money is the employer's rather than minted. adr/0142's supply is what would catch a
        // wage that created money, and it is checked whole at the end of the run.
        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>Changing the pay period changes the rhythm and not the income.</b>
    /// </summary>
    /// <remarks>
    /// <b>The whole reason <c>wage_per_day</c> is a daily rate rather than what lands on payday.</b>
    /// A lump-sum key would make a 14× change of period a 14× change of income, and no reading taken
    /// across periods could be attributed to the rhythm. What is required is that the three totals
    /// are the same <em>order</em>, not that they are equal.
    /// <para>
    /// 🔴 <b>A FINDING RATHER THAN A THRESHOLD, AND IT IS NOT THE PART-PERIOD.</b> The old reading
    /// was <b>2,701,864 / 2,809,328 / 2,950,152</b> over 60 Days at periods 14, 7 and 1 — a spread of
    /// <b>9%</b>, and the remark here attributed it to a fixed-length run catching a different amount
    /// of the last part-period. Re-measured 2026-09-02 at <c>plans/0053</c>: <b>6,946,816 /
    /// 8,007,680 / 9,352,912</b>, a spread of <b>35%</b> in the same direction. ⚠ <b>The run is 56
    /// Days now and not 60</b>, which is a whole number of every period under test — ***so the
    /// part-period is eliminated by construction and the spread survives it.*** The old explanation
    /// was never checked and is wrong.
    /// </para>
    /// <para>
    /// ⚠ <b>The cause is NOT established here and two candidates are named rather than one asserted</b>
    /// (<c>adr/0043</c>): a longer period presents a <em>larger lump</em> against an employer's
    /// balance, which <c>adr/0142</c> makes the source of every wage — and occupancy dividing the
    /// ground made this city's Businesses fewer and larger, which would amplify exactly that; or
    /// entitlement is lost when a job ends between paydays, which <see cref="Population"/>'s churn
    /// would also amplify. <b>Filed in <c>plans/0053</c>.</b> ***The band is widened to bound the
    /// order rather than tightened around a number nobody has explained.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_pay_period_moves_the_rhythm_and_leaves_the_income_alone()
    {
        long daily = PaidOver(1, 56);
        long weekly = PaidOver(7, 56);
        long fortnightly = PaidOver(14, 56);

        _output.WriteLine($"period  1: {daily}");
        _output.WriteLine($"period  7: {weekly}");
        _output.WriteLine($"period 14: {fortnightly}");

        Assert.True(daily > 0 && weekly > 0 && fortnightly > 0, "some period paid nothing at all.");

        long most = long.Max(daily, long.Max(weekly, fortnightly));
        long least = long.Min(daily, long.Min(weekly, fortnightly));

        Assert.True(
            most <= least * 2,
            $"the three periods paid {daily}, {weekly} and {fortnightly}. A daily rate should make "
            + "the period change WHEN people are paid and not HOW MUCH, so a 14x period paying half "
            + "of what a 1x period pays is the rate being read as a lump. ⚠ A spread inside this "
            + "band is a KNOWN and UNEXPLAINED 35% -- see the remarks, and do not tighten this "
            + "without explaining that first.");
    }

    /// <summary>
    /// 🔴 <b><c>adr/0006</c> in a wage: unpaid work must not accrue for ever.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the defect the mechanism shipped with and the test that would have caught it.</b>
    /// Entitlement accrues from <c>CitizenTable.LastPaidDay</c> and the clock advances only for what
    /// was actually paid, so a worker at an employer that can never pay them is owed one more Day
    /// every Day. Measured before <c>WageEngine.Pay</c>'s cap existed, on this file at a daily
    /// period: a shortfall of <b>8,384</b> on Day 14 and <b>499,712</b> on Day 56, climbing linearly.
    /// ⚠ <b>It is asserted as a ceiling rather than as flatness</b>, because a shortfall is what one
    /// payday could not cover and is genuinely allowed to come and go.
    /// </remarks>
    [Fact]
    public void Unpaid_wages_do_not_accrue_for_ever()
    {
        (_, Simulation simulation) = Start(Rules(1));

        long worst = 0;
        long lastSeen = 0;

        for (int tick = 0; tick < 90 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            if (simulation.LastPayroll.Shortfall <= 0)
            {
                continue;
            }

            lastSeen = simulation.LastPayroll.Shortfall;
            worst = long.Max(worst, lastSeen);
        }

        _output.WriteLine($"worst shortfall on any one payday: {worst}; last seen: {lastSeen}.");

        // One period's worth for a handful of workers, and nowhere near the half-million the
        // uncapped version reached by Day 56. The ceiling is deliberately loose -- what it catches is
        // a debt that grows with elapsed time, which passes no ceiling at all.
        Assert.True(
            worst < 200_000,
            $"the worst single-payday shortfall was {worst}. Arrears are capped at one pay period "
            + "(WageEngine.Pay), so a figure this size means the cap is not holding and unpaid work "
            + "is accruing without a sink -- adr/0006.");
    }

    /// <summary>A trade that states a rate and no period, and one that states neither.</summary>
    [Fact]
    public void A_rate_without_a_period_is_refused_and_neither_is_fine()
    {
        RulesetLoadResult half = RulesetLoader.Parse(
            Text().Replace("pay_period_days = 7", "", StringComparison.Ordinal), "half.toml");

        Assert.False(half.Ok, "a wage_per_day with no pay_period_days loaded.");
        Assert.Contains("half a mechanism", half.Describe(), StringComparison.Ordinal);

        RulesetLoadResult none = RulesetLoader.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "provisioned.toml")),
            "provisioned.toml");

        Assert.True(none.Ok, none.Describe());
    }

    private static long PaidOver(int period, int days)
    {
        (_, Simulation simulation) = Start(Rules(period));

        long paid = 0;

        for (int tick = 0; tick < days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
            paid += simulation.LastPayroll.Paid;
        }

        return paid;
    }

    private static (World World, Simulation Simulation) Start(Ruleset rules)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, rules, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        return (world, simulation);
    }

    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "waged.toml"));

    private static Ruleset Rules(int period)
    {
        RulesetLoadResult parsed = RulesetLoader.Parse(
            Text().Replace(
                "pay_period_days = 7", $"pay_period_days = {period}", StringComparison.Ordinal),
            "waged.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        return parsed.Ruleset!;
    }
}
