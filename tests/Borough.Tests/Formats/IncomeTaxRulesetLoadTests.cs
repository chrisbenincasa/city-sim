using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The <c>[income_tax]</c> table: the three marginal bands a Day's earnings are read against.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="MarketRulesetLoadTests"/>' discipline — which is the rule <c>adr/0048</c> states and
/// <c>RefusalCountTests</c> counts.
/// </para>
/// <para>
/// <b>The table's polarity is the first thing tested and it is the thing most easily lost.</b>
/// Omitting it is a city that levies no income tax at all, reached by deleting the table rather than
/// by writing zeroes into it — <c>[traffic]</c>'s shape and <c>[households]
/// car_ownership_percent</c>'s. A defaulted allowance or rate would be a number arriving at a
/// setting no designer picked, sitting inside the range of answers somebody might have meant.
/// </para>
/// <para>
/// 🔴 <b>The one refusal here that is not a range check is the falling upper rate.</b> Both rates
/// are marginal, so an upper rate below the middle one makes take-home income step
/// <em>downward</em> at the threshold: a Citizen keeps less for having earned more. That is not a
/// lighter tax on high earners somebody could be tuning toward — it is the schedule ceasing to be
/// monotone, which <c>IncomeTax.WithholdingOn</c> leans on to keep a withholding non-negative.
/// </para>
/// </remarks>
public sealed class IncomeTaxRulesetLoadTests
{
    /// <summary>The smallest file the loader accepts, which this table hangs off.</summary>
    private const string City = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "pound"
        family = "money"

        [[building]]
        name = "dwelling"
        """;

    /// <summary>A well-formed schedule, for the tests that vary one key of it.</summary>
    private static string Schedule(
        string allowance = "allowance_per_day = 50",
        string threshold = "upper_threshold_per_day = 200",
        string middle = "middle_rate_percent = 10",
        string upper = "upper_rate_percent = 20") =>
        $"{City}\n\n[income_tax]\n{allowance}\n{threshold}\n{middle}\n{upper}\n";

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    // ---- the absence -----------------------------------------------------------------------------

    /// <summary>A file with no <c>[income_tax]</c> levies nothing.</summary>
    /// <remarks>
    /// <b>Every Ruleset shipped before this table existed is this city</b>, so the absence had to be
    /// the behaviour-preserving answer rather than a schedule of zeroes.
    /// </remarks>
    [Fact]
    public void No_income_tax_table_is_a_city_that_levies_nothing()
    {
        Ruleset ruleset = Accepted(City);

        Assert.Equal(IncomeTaxSchedule.None, ruleset.IncomeTax);
        Assert.False(ruleset.IncomeTax.Levies);
    }

    // ---- the schedule ----------------------------------------------------------------------------

    /// <summary>All four keys survive the load, in the right fields.</summary>
    /// <remarks>
    /// <b>Four distinct values on purpose.</b> A reader that transposed the allowance and the
    /// threshold, or the two rates, would pass any fixture whose numbers repeat.
    /// </remarks>
    [Fact]
    public void The_four_keys_survive_the_load()
    {
        Ruleset ruleset = Accepted(Schedule());

        Assert.Equal(50, ruleset.IncomeTax.AllowancePerDay);
        Assert.Equal(200, ruleset.IncomeTax.UpperThresholdPerDay);
        Assert.Equal(10, ruleset.IncomeTax.MiddleRatePercent);
        Assert.Equal(20, ruleset.IncomeTax.UpperRatePercent);
        Assert.True(ruleset.IncomeTax.Levies);
    }

    /// <summary>An allowance of zero is a schedule that taxes the first unit earned.</summary>
    [Fact]
    public void An_allowance_of_zero_is_accepted()
    {
        Ruleset ruleset = Accepted(Schedule(allowance: "allowance_per_day = 0"));

        Assert.Equal(0, ruleset.IncomeTax.AllowancePerDay);
    }

    /// <summary>A threshold equal to the allowance is a two-band schedule, and is accepted.</summary>
    /// <remarks>
    /// <b>The pair to <see cref="A_threshold_below_the_allowance_is_refused"/>.</b> Equal collapses
    /// the middle band to nothing, which is a schedule somebody could mean — a flat rate above an
    /// allowance. Below is the same band inverted, which is nothing anybody means.
    /// </remarks>
    [Fact]
    public void A_threshold_equal_to_the_allowance_is_accepted()
    {
        Ruleset ruleset = Accepted(Schedule(
            allowance: "allowance_per_day = 50",
            threshold: "upper_threshold_per_day = 50"));

        Assert.Equal(50, ruleset.IncomeTax.UpperThresholdPerDay);
    }

    /// <summary>Two equal rates are a flat tax above the allowance, and are accepted.</summary>
    [Fact]
    public void Two_equal_rates_are_accepted()
    {
        Ruleset ruleset = Accepted(Schedule(
            middle: "middle_rate_percent = 25", upper: "upper_rate_percent = 25"));

        Assert.Equal(25, ruleset.IncomeTax.MiddleRatePercent);
        Assert.Equal(25, ruleset.IncomeTax.UpperRatePercent);
    }

    /// <summary>Rates of zero load, and the schedule says it takes nothing.</summary>
    [Fact]
    public void Rates_of_zero_are_a_schedule_that_levies_nothing()
    {
        Ruleset ruleset = Accepted(Schedule(
            middle: "middle_rate_percent = 0", upper: "upper_rate_percent = 0"));

        Assert.False(ruleset.IncomeTax.Levies);
    }

    // ---- the group -------------------------------------------------------------------------------

    /// <summary>A stated <c>[income_tax]</c> states all four of its keys.</summary>
    /// <remarks>
    /// <b>The optionality belongs to the group rather than to any key</b>, which is
    /// <c>opening_balance_min</c>/<c>opening_balance_max</c>'s rule with four ends instead of two.
    /// </remarks>
    [Theory]
    [InlineData("allowance_per_day")]
    [InlineData("upper_threshold_per_day")]
    [InlineData("middle_rate_percent")]
    [InlineData("upper_rate_percent")]
    public void A_stated_income_tax_table_states_all_four_of_its_keys(string missing)
    {
        string[] lines =
        [
            "allowance_per_day = 50",
            "upper_threshold_per_day = 200",
            "middle_rate_percent = 10",
            "upper_rate_percent = 20",
        ];

        string kept = string.Join(
            '\n',
            lines.Where(line => !line.StartsWith(missing, StringComparison.Ordinal)));

        RulesetRefusal refusal = Refused($"{City}\n\n[income_tax]\n{kept}\n");

        Assert.Contains(missing, refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("one decision in four keys", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the ranges ------------------------------------------------------------------------------

    /// <summary>A negative allowance is a Day taxed before anything was earned.</summary>
    [Fact]
    public void A_negative_allowance_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule(allowance: "allowance_per_day = -1"));

        Assert.Contains("allowance_per_day", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A band that starts below where taxation starts is not a band.</summary>
    [Fact]
    public void A_threshold_below_the_allowance_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule(
            allowance: "allowance_per_day = 50",
            threshold: "upper_threshold_per_day = 49"));

        Assert.Contains("upper_threshold_per_day", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("empty band", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A middle rate outside 0..100 is a quantity that is not a share.</summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    public void A_middle_rate_outside_a_hundred_percent_is_refused(string rate)
    {
        RulesetRefusal refusal = Refused(Schedule(middle: $"middle_rate_percent = {rate}"));

        Assert.Contains("middle_rate_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>And the upper rate, through the same guard.</summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    public void An_upper_rate_outside_a_hundred_percent_is_refused(string rate)
    {
        RulesetRefusal refusal = Refused(Schedule(upper: $"upper_rate_percent = {rate}"));

        Assert.Contains("upper_rate_percent", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the design refusal ----------------------------------------------------------------------

    /// <summary>An upper rate below the middle one is refused, and the sentence says why.</summary>
    /// <remarks>
    /// <b>Not a typo guard.</b> The refusal has to name the consequence — take-home income stepping
    /// downward at the threshold — because the mistake reads as a deliberate setting: a designer who
    /// wrote it was picking a lighter tax on high earners, and the file gives no other sign that the
    /// schedule has stopped being monotone.
    /// </remarks>
    [Fact]
    public void An_upper_rate_below_the_middle_rate_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule(
            middle: "middle_rate_percent = 40", upper: "upper_rate_percent = 20"));

        Assert.Contains("upper_rate_percent", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("DOWNWARD", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the table -------------------------------------------------------------------------------

    /// <summary>A second <c>[income_tax]</c> is refused rather than letting the later one win.</summary>
    [Fact]
    public void A_second_income_tax_table_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule() + """

            [income_tax]
            allowance_per_day = 10
            upper_threshold_per_day = 20
            middle_rate_percent = 5
            upper_rate_percent = 6
            """);

        Assert.Contains("[income_tax]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the depth of the schedule history -------------------------------------------------------

    /// <summary>A trade and the floor space to employ through it, so a wage means something.</summary>
    private static string Trade(int payPeriodDays) => $"""

        [capacity]
        floor_tiles_per_occupant      = 25
        floor_tiles_per_job           = 3
        floor_tiles_per_parking_space = 12

        [[business]]
        name = "shop"
        shift_start_earliest_hour = 6
        shift_start_latest_hour   = 10
        wage_per_day = 10
        pay_period_days = {payPeriodDays}
        """;

    /// <summary>
    /// A pay period deeper than the schedule history is refused beside <c>[income_tax]</c>.
    /// </summary>
    /// <remarks>
    /// <b>A defect that loads clean and misbehaves in silence.</b> <c>WageEngine.Withhold</c> walks
    /// the earning Days a payment closes, bounded at <c>IncomeTaxTable.Retained</c>, and assesses
    /// whatever is still owed past that depth as one Day's earnings — so the surplus lands at the
    /// top band and nothing reports it. ⚠ <b>The class remark on <c>IncomeTaxTable</c> claimed this
    /// refusal existed before it did</b>, which is why this test names both halves it must carry.
    /// </remarks>
    [Fact]
    public void A_pay_period_deeper_than_the_schedule_history_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule() + Trade(IncomeTaxTable.Retained + 8));

        Assert.Contains("shop", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("pay_period_days", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains(
            IncomeTaxTable.Retained.ToString(CultureInfo.InvariantCulture),
            refusal.Reason,
            StringComparison.Ordinal);
        Assert.Contains("ONE Day's earnings", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The same trade with no <c>[income_tax]</c> in the file still loads.</summary>
    /// <remarks>
    /// <b>The half that makes this a refusal of a PAIR rather than a bound on pay periods.</b> A city
    /// that pays quarterly and taxes no income is coherent, and nothing in it consults a schedule at
    /// all.
    /// </remarks>
    [Fact]
    public void A_long_pay_period_without_an_income_tax_table_loads()
    {
        Ruleset ruleset = Accepted(City + Trade(IncomeTaxTable.Retained + 8));

        Assert.Equal(IncomeTaxSchedule.None, ruleset.IncomeTax);
        Assert.Equal(IncomeTaxTable.Retained + 8, ruleset.BusinessKinds[0].PayPeriodDays);
    }

    /// <summary>A period exactly as deep as the history loads; one Day more does not.</summary>
    /// <remarks>
    /// <b>The boundary is the whole content of the bound</b>, and it is inclusive: a payment closing
    /// exactly <c>Retained</c> Days is walked Day by Day to the end with nothing left over, so it is
    /// assessed exactly. One more Day is the first payment carrying a remainder the walk cannot
    /// place.
    /// </remarks>
    [Fact]
    public void Exactly_the_retained_depth_loads_and_one_day_more_does_not()
    {
        Ruleset ruleset = Accepted(Schedule() + Trade(IncomeTaxTable.Retained));

        Assert.Equal(IncomeTaxTable.Retained, ruleset.BusinessKinds[0].PayPeriodDays);

        RulesetRefusal refusal = Refused(Schedule() + Trade(IncomeTaxTable.Retained + 1));

        Assert.Contains("pay_period_days", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the reload ------------------------------------------------------------------------------

    /// <summary>Retuning the schedule is a tuning change, so a reload does not need a migration.</summary>
    /// <remarks>
    /// <b><c>adr/0015</c>'s acceptance test on a fourth surface.</b> Nothing in the world points at a
    /// band — a schedule is read at the moment earnings are attributed and never stored — so
    /// <see cref="RulesetShape"/> compares none of it and the standing city takes the new numbers.
    /// </remarks>
    [Fact]
    public void Retuning_the_schedule_is_a_reload_that_needs_no_migration()
    {
        Ruleset before = Accepted(Schedule());
        Ruleset after = Accepted(Schedule(
            allowance: "allowance_per_day = 80",
            threshold: "upper_threshold_per_day = 400",
            middle: "middle_rate_percent = 15",
            upper: "upper_rate_percent = 45"));

        Assert.Equal(RulesetChange.None, RulesetShape.Compare(before, after));
    }

    /// <summary>Adding the table to a file that had none is a tuning change too.</summary>
    [Fact]
    public void Adding_the_table_on_a_reload_needs_no_migration()
    {
        Assert.Equal(
            RulesetChange.None,
            RulesetShape.Compare(Accepted(City), Accepted(Schedule())));
    }
}
