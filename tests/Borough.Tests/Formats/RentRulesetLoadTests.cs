using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The <c>[rent]</c> table and every refusal it states —
/// <c>plans/money-circulation.md</c> L8 and L16.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="TreasuryRulesetLoadTests"/>' discipline.
/// </para>
/// <para>
/// <b>One key, and the table's presence is the switch.</b> <c>evicts_after_days</c> both turns the
/// daily charge on and bounds the debt a tenant can run up, so a city that charges no rent omits the
/// table rather than writing a number meaning never. That is <c>[households]</c>' shape — an
/// optional table whose one key is required — and it keeps absence behaviour-preserving for the
/// eighteen Rulesets written before this slice.
/// </para>
/// </remarks>
public sealed class RentRulesetLoadTests
{
    /// <summary>The smallest file that can collect rent: money, and a Pool that empties.</summary>
    private const string Rentable = """
        [[resource]]
        name = "money"
        family = "money"

        [placement]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        gives_up_after_days = 60
        """;

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

    // ---- the absent table -----------------------------------------------------------------------

    /// <summary>A file that omits the table collects nothing, which is what every file did before.</summary>
    [Fact]
    public void A_ruleset_with_no_rent_table_charges_nothing()
    {
        Ruleset rules = Accepted(Rentable);

        Assert.Equal(RentRuleset.None, rules.Rent);
        Assert.False(rules.Rent.Collects);
        Assert.Equal(0, rules.Rent.EvictsAfterDays);
    }

    /// <summary>A stated table must state its key.</summary>
    [Fact]
    public void A_rent_table_with_no_deadline_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Rentable}\n\n[rent]\n");

        Assert.Contains("evicts_after_days", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Two tables of numbers for one charge is ambiguous rather than additive.</summary>
    /// <remarks>
    /// <b>Written as an array of tables</b>, on <see cref="TreasuryRulesetLoadTests"/>' shape: the
    /// TOML parser catches a repeated <c>[rent]</c> itself and never reaches this guard.
    /// </remarks>
    [Fact]
    public void A_second_rent_table_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Rentable}

            [[rent]]
            evicts_after_days = 7

            [[rent]]
            evicts_after_days = 14
            """);

        Assert.Contains("second [rent]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the value ------------------------------------------------------------------------------

    /// <summary>The key loads, and it is the figure the file wrote.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(ushort.MaxValue)]
    public void A_stated_deadline_is_read(int days)
    {
        Ruleset rules = Accepted($"{Rentable}\n\n[rent]\nevicts_after_days = {days}\n");

        Assert.True(rules.Rent.Collects);
        Assert.Equal(days, rules.Rent.EvictsAfterDays);
    }

    /// <summary>
    /// Zero is refused rather than read as never, because the deadline is what bounds the debt.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ushort.MaxValue + 1)]
    public void A_deadline_outside_the_range_is_refused(int days)
    {
        RulesetRefusal refusal = Refused($"{Rentable}\n\n[rent]\nevicts_after_days = {days}\n");

        Assert.Contains($"evicts_after_days is {days}", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("omit the whole [rent] table", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- what the table is owed by the rest of the file -----------------------------------------

    /// <summary>
    /// A charge in a file that names no money has nothing to pay with and nowhere for the rent to
    /// land, which is <c>adr/0114</c>'s "a balance is a Bin" read as a load-time check.
    /// </summary>
    [Fact]
    public void Rent_in_a_file_that_names_no_money_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "flour"
            family = "good"

            [placement]
            interval = 32
            revisit_ticks = 1024
            candidates = 3
            gives_up_after_days = 60

            [rent]
            evicts_after_days = 7
            """);

        Assert.Contains("names no money", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Eviction is a door into the Unplaced Pool, so a file that opens it owes the Pool a sink —
    /// <c>adr/0130</c> and <c>adr/0006</c>.
    /// </summary>
    [Fact]
    public void Rent_without_a_give_up_rule_is_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "money"
            family = "money"

            [rent]
            evicts_after_days = 7
            """);

        Assert.Contains("gives_up_after_days", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A deadline shorter than the longest pay period evicts a tenant for the Days between its
    /// paydays rather than for being unable to pay — <c>L16</c>.
    /// </summary>
    /// <remarks>
    /// <b>The pair the loader can see and neither reader can.</b> The deadline knows nothing about
    /// who pays whom and a trade knows nothing about whether the city charges rent, so the check
    /// runs after <c>ReadBusinessKinds</c> with both in hand.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void A_deadline_below_the_longest_pay_period_is_refused(int days)
    {
        RulesetRefusal refusal = Refused(Waged(days));

        Assert.Contains($"evicts_after_days is {days}", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("pays every 7 Days", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("State 7 or more", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A deadline at or above the longest pay period is accepted.</summary>
    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    public void A_deadline_at_or_above_the_longest_pay_period_is_accepted(int days)
    {
        Assert.Equal(days, Accepted(Waged(days)).Rent.EvictsAfterDays);
    }

    /// <summary>A file whose trades pay no wage has no pay period to clear.</summary>
    [Fact]
    public void A_short_deadline_is_accepted_where_no_trade_pays_a_wage()
    {
        Ruleset rules = Accepted($"""
            {Rentable}

            [capacity]
            floor_tiles_per_job = 1

            [[business]]
            name = "shop"
            shift_start_earliest_hour = 6
            shift_start_latest_hour   = 10

            [rent]
            evicts_after_days = 1
            """);

        Assert.Equal(1, rules.Rent.EvictsAfterDays);
    }

    /// <summary>A file with one weekly-paid trade and the stated deadline.</summary>
    private static string Waged(int days) => $"""
        {Rentable}

        [capacity]
        floor_tiles_per_job = 1

        [[business]]
        name = "shop"
        shift_start_earliest_hour = 6
        shift_start_latest_hour   = 10
        wage_per_day = 10
        pay_period_days = 7
        goes_bankrupt_after_days_in_arrears = 9

        [rent]
        evicts_after_days = {days}
        """;
}
