using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The <c>[business_tax]</c> table: the two marginal bands a Business's profit for one Day is read
/// against (<c>plans/0072</c> D8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="IncomeTaxRulesetLoadTests"/>' discipline — the rule <c>adr/0048</c> states and
/// <c>RefusalCountTests</c> counts.
/// </para>
/// <para>
/// 🔴 <b>TWO bands where the Citizen schedule has three, and the missing one is the allowance.</b>
/// D8: *"There is no separate tax-free band, although setting the lower rate to zero can provide
/// one."* So the test that matters most here is the one asserting there is no allowance key — a
/// reader adding one later would be adding a second spelling of a city this file already writes.
/// </para>
/// <para>
/// ⚠ <b>And there is deliberately no pay-period test, which is where this file stops mirroring its
/// sibling.</b> Profit is assessed once per Day against a figure the Business accumulates (D23) and
/// no schedule history stands behind it, so no fixed ring exists for a long interval to outrun.
/// ***The Citizen-side refusal was a property of a table depth and not of taxation.***
/// </para>
/// </remarks>
public sealed class BusinessTaxRulesetLoadTests
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
        string threshold = "threshold_per_day = 200",
        string lower = "lower_rate_percent = 10",
        string upper = "upper_rate_percent = 30") =>
        $"{City}\n\n[business_tax]\n{threshold}\n{lower}\n{upper}\n";

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

    /// <summary>A file with no <c>[business_tax]</c> taxes no profit.</summary>
    /// <remarks>
    /// <b>Every Ruleset shipped before this table existed is this city</b>, so the absence had to be
    /// the behaviour-preserving answer rather than a schedule of zeroes.
    /// </remarks>
    [Fact]
    public void No_business_tax_table_is_a_city_that_taxes_no_profit()
    {
        Ruleset ruleset = Accepted(City);

        Assert.Equal(BusinessTaxSchedule.None, ruleset.BusinessTax);
        Assert.False(ruleset.BusinessTax.Levies);
    }

    // ---- the schedule ----------------------------------------------------------------------------

    /// <summary>All three keys survive the load, in the right fields.</summary>
    /// <remarks>
    /// <b>Three distinct values on purpose.</b> A reader that transposed the two rates would pass
    /// any fixture whose numbers repeat.
    /// </remarks>
    [Fact]
    public void The_three_keys_survive_the_load()
    {
        Ruleset ruleset = Accepted(Schedule());

        Assert.Equal(200, ruleset.BusinessTax.ThresholdPerDay);
        Assert.Equal(10, ruleset.BusinessTax.LowerRatePercent);
        Assert.Equal(30, ruleset.BusinessTax.UpperRatePercent);
        Assert.True(ruleset.BusinessTax.Levies);
    }

    /// <summary>
    /// There is no allowance key, and stating one is refused rather than ignored.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The absence D8 decided, held by a test.</b> A designer who has read the Citizen schedule
    /// has positive reason to reach for an allowance here, so the failure mode is an author writing
    /// one and getting a city that quietly taxes from the first unit. The unknown-key check catches
    /// it; this test is what stops a later reader from "fixing" that by adding the key.
    /// </remarks>
    [Fact]
    public void There_is_no_allowance_key()
    {
        RulesetRefusal refusal = Refused(
            $"{City}\n\n[business_tax]\nallowance_per_day = 50\nthreshold_per_day = 200\n"
            + "lower_rate_percent = 10\nupper_rate_percent = 30\n");

        Assert.Contains("allowance_per_day", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A lower rate of zero is how an author writes a tax-free band.</summary>
    /// <remarks>
    /// <b>The pair to <see cref="There_is_no_allowance_key"/>, and together they are D8.</b> The
    /// city is writable; it is writable one way.
    /// </remarks>
    [Fact]
    public void A_lower_rate_of_zero_is_accepted_and_is_the_tax_free_band()
    {
        Ruleset ruleset = Accepted(Schedule(lower: "lower_rate_percent = 0"));

        Assert.Equal(0, ruleset.BusinessTax.LowerRatePercent);
        Assert.True(ruleset.BusinessTax.Levies);
        Assert.Equal(0, BusinessTax.DueOn(200, ruleset.BusinessTax));
    }

    /// <summary>A threshold of zero puts every unit of profit in the upper band.</summary>
    [Fact]
    public void A_threshold_of_zero_is_accepted()
    {
        Ruleset ruleset = Accepted(Schedule(threshold: "threshold_per_day = 0"));

        Assert.Equal(0, ruleset.BusinessTax.ThresholdPerDay);
        Assert.Equal(30, BusinessTax.DueOn(100, ruleset.BusinessTax));
    }

    /// <summary>Two equal rates are a flat tax on profit, and are accepted.</summary>
    [Fact]
    public void Two_equal_rates_are_accepted()
    {
        Ruleset ruleset = Accepted(Schedule(
            lower: "lower_rate_percent = 25", upper: "upper_rate_percent = 25"));

        Assert.Equal(25, ruleset.BusinessTax.LowerRatePercent);
        Assert.Equal(25, ruleset.BusinessTax.UpperRatePercent);
    }

    /// <summary>Rates of zero load, and the schedule says it takes nothing.</summary>
    [Fact]
    public void Rates_of_zero_are_a_schedule_that_takes_nothing()
    {
        Ruleset ruleset = Accepted(Schedule(
            lower: "lower_rate_percent = 0", upper: "upper_rate_percent = 0"));

        Assert.False(ruleset.BusinessTax.Levies);
    }

    // ---- the group -------------------------------------------------------------------------------

    /// <summary>A stated <c>[business_tax]</c> states all three of its keys.</summary>
    /// <remarks>
    /// <b>The optionality belongs to the group rather than to any key</b>, which is
    /// <c>[income_tax]</c>'s rule with three ends instead of four.
    /// </remarks>
    [Theory]
    [InlineData("threshold_per_day")]
    [InlineData("lower_rate_percent")]
    [InlineData("upper_rate_percent")]
    public void A_stated_business_tax_table_states_all_three_of_its_keys(string missing)
    {
        string[] lines =
        [
            "threshold_per_day = 200",
            "lower_rate_percent = 10",
            "upper_rate_percent = 30",
        ];

        string kept = string.Join(
            '\n',
            lines.Where(line => !line.StartsWith(missing, StringComparison.Ordinal)));

        RulesetRefusal refusal = Refused($"{City}\n\n[business_tax]\n{kept}\n");

        Assert.Contains(missing, refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("one decision in three keys", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the ranges ------------------------------------------------------------------------------

    /// <summary>
    /// A negative threshold is refused, even though profit itself may be negative.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The refusal whose obvious reading is wrong.</b> A loss is an untaxed Day (D26), so
    /// profit ranges below zero and a reader reaches for *a negative threshold is therefore
    /// meaningful*. It is not: a band <em>boundary</em> below zero puts the whole lower band where
    /// no profit can ever fall, so the lower rate becomes unreachable while still reading as a
    /// setting. ***A quantity's range is not its boundary's range.***
    /// </remarks>
    [Fact]
    public void A_negative_threshold_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule(threshold: "threshold_per_day = -1"));

        Assert.Contains("threshold_per_day", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("unreachable", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A lower rate outside 0..100 is a quantity that is not a share.</summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    public void A_lower_rate_outside_a_hundred_percent_is_refused(string rate)
    {
        RulesetRefusal refusal = Refused(Schedule(lower: $"lower_rate_percent = {rate}"));

        Assert.Contains("lower_rate_percent", refusal.Reason, StringComparison.Ordinal);
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

    /// <summary>An upper rate below the lower one is refused, and the sentence says why.</summary>
    /// <remarks>
    /// <b>Not a typo guard.</b> D8 requires the upper at or above the lower, and the refusal has to
    /// name the consequence — post-tax profit stepping downward at the threshold — because the
    /// mistake reads as a deliberate setting: a designer who wrote it was granting relief to large
    /// profits, and the file gives no other sign that the schedule has stopped being monotone.
    /// </remarks>
    [Fact]
    public void An_upper_rate_below_the_lower_rate_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule(
            lower: "lower_rate_percent = 40", upper: "upper_rate_percent = 20"));

        Assert.Contains("upper_rate_percent", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("DOWNWARD", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the table -------------------------------------------------------------------------------

    /// <summary>A second <c>[business_tax]</c> is refused rather than letting the later one win.</summary>
    [Fact]
    public void A_second_business_tax_table_is_refused()
    {
        RulesetRefusal refusal = Refused(Schedule() + """

            [business_tax]
            threshold_per_day = 10
            lower_rate_percent = 5
            upper_rate_percent = 6
            """);

        Assert.Contains("[business_tax]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the two schedules are independent -------------------------------------------------------

    /// <summary>
    /// A file may state both tax tables, or either alone, and they do not read each other.
    /// </summary>
    /// <remarks>
    /// <b>Worth a test because they were built one after the other from the same shape.</b> A reader
    /// that reached for the wrong table field would still pass every test above, which asserts one
    /// schedule at a time against a file that states only that one.
    /// </remarks>
    [Fact]
    public void The_citizen_and_business_schedules_are_independent()
    {
        Ruleset both = Accepted(
            Schedule()
            + "\n[income_tax]\nallowance_per_day = 50\nupper_threshold_per_day = 200\n"
            + "middle_rate_percent = 10\nupper_rate_percent = 20\n");

        Assert.Equal(50, both.IncomeTax.AllowancePerDay);
        Assert.Equal(20, both.IncomeTax.UpperRatePercent);
        Assert.Equal(200, both.BusinessTax.ThresholdPerDay);
        Assert.Equal(30, both.BusinessTax.UpperRatePercent);

        Assert.Equal(BusinessTaxSchedule.None, Accepted(City).BusinessTax);
    }

    // ---- the reload ------------------------------------------------------------------------------

    /// <summary>Retuning the schedule is a tuning change, so a reload needs no migration.</summary>
    /// <remarks>
    /// <b><c>adr/0015</c>'s acceptance test on a fifth surface.</b> Nothing in the world points at a
    /// band, so <see cref="RulesetShape"/> compares none of it and the standing city takes the new
    /// numbers.
    /// </remarks>
    [Fact]
    public void Retuning_the_schedule_is_a_reload_that_needs_no_migration()
    {
        Ruleset before = Accepted(Schedule());
        Ruleset after = Accepted(Schedule(
            threshold: "threshold_per_day = 900",
            lower: "lower_rate_percent = 15",
            upper: "upper_rate_percent = 45"));

        Assert.Equal(RulesetChange.None, RulesetShape.Compare(before, after));
    }

    /// <summary>Adding the table on a reload is a tuning change too.</summary>
    [Fact]
    public void Adding_the_table_on_a_reload_needs_no_migration() =>
        Assert.Equal(
            RulesetChange.None,
            RulesetShape.Compare(Accepted(City), Accepted(Schedule())));
}
