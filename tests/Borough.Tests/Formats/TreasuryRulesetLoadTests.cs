using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// <c>plans/0070</c> row 32: the <c>[treasury]</c> table and every refusal it states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>HouseholdRulesetLoadTests</c>' discipline.
/// </para>
/// <para>
/// ⚠ <b>The absent table is the case worth its class remark.</b> <c>adr/0116</c> chose an empty
/// opening treasury so a dry sweep is reachable on the first sweep, and <c>rulesets/levied.toml</c>
/// demonstrates it. A defaulted balance would delete that demonstration to enable this table's, so
/// omission is the behaviour every Ruleset already had and is reached through the absence of the
/// table rather than through a defaulted key.
/// </para>
/// </remarks>
public sealed class TreasuryRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset that names money.</summary>
    private const string Named = """
        [[resource]]
        name = "money"
        family = "money"
        """;

    /// <summary>The smallest complete Ruleset that names none.</summary>
    private const string Moneyless = """
        [[resource]]
        name = "flour"
        family = "good"
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

    /// <summary>
    /// <b>The absence is <c>adr/0116</c>'s empty treasury, and the file has said so by omitting the
    /// table.</b>
    /// </summary>
    [Fact]
    public void A_ruleset_with_no_treasury_table_opens_the_city_with_nothing()
    {
        Ruleset rules = Accepted(Named);

        Assert.Equal(TreasuryRuleset.None, rules.Treasury);
        Assert.Equal(Money.Zero, rules.Treasury.OpeningBalance);
    }

    /// <summary>A stated table must state its key.</summary>
    [Fact]
    public void A_treasury_table_with_no_opening_balance_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Named}\n\n[treasury]\n");

        Assert.Contains("opening_balance", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Two tables of numbers for one treasury is ambiguous rather than additive.</summary>
    /// <remarks>
    /// <b>Written as an array of tables</b>, on <c>ParkingRulesetLoadTests</c>' shape: a repeated
    /// <c>[treasury]</c> is caught by the TOML parser itself and never reaches this guard, so a test
    /// written the obvious way would assert the parser's message and leave the guard untested.
    /// </remarks>
    [Fact]
    public void A_second_treasury_table_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Named}

            [[treasury]]
            opening_balance = 10

            [[treasury]]
            opening_balance = 20
            """);

        Assert.Contains("second [treasury]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the value ------------------------------------------------------------------------------

    /// <summary>The key loads, and it is the figure the file wrote.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4_194_304)]
    [InlineData(int.MaxValue)]
    public void An_opening_balance_loads_as_written(long balance)
    {
        Ruleset rules = Accepted($"{Named}\n\n[treasury]\nopening_balance = {balance}\n");

        Assert.Equal(new Money(balance), rules.Treasury.OpeningBalance);
    }

    /// <summary>
    /// <b>A stock is never negative</b>, so founding a city in arrears is refused rather than read as
    /// a debt.
    /// </summary>
    [Fact]
    public void A_negative_opening_balance_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Named}\n\n[treasury]\nopening_balance = -1\n");

        Assert.Contains("opening_balance is -1", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("never negative", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Above <see cref="int.MaxValue"/> the fiscal decision the balance exists for can never
    /// bind</b>, so the figure is refused rather than clamped.
    /// </summary>
    [Fact]
    public void An_opening_balance_above_int_max_is_refused()
    {
        RulesetRefusal refusal = Refused(
            $"{Named}\n\n[treasury]\nopening_balance = {(long)int.MaxValue + 1}\n");

        Assert.Contains("can never run out", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the pair -------------------------------------------------------------------------------

    /// <summary>
    /// <b>A balance in a file that names no money is refused at load</b>, which is
    /// <c>[households]</c>' opening-band refusal one table along.
    /// </summary>
    /// <remarks>
    /// The treasury holds one Bin per conserved Resource (<c>adr/0114</c>, <c>adr/0116</c>), so
    /// <c>World.EndowTreasury</c> would throw at world creation. The loader can see both halves — the
    /// families are read in the first pass — so this is a file and a line rather than a crash.
    /// </remarks>
    [Fact]
    public void A_treasury_in_a_ruleset_that_names_no_money_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Moneyless}\n\n[treasury]\nopening_balance = 100\n");

        Assert.Contains("names no money", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>A balance of zero in a moneyless file loads</b>, and the asymmetry is deliberate.
    /// </summary>
    /// <remarks>
    /// Zero founds nothing, so there is nothing to put anywhere and no world-creation call to
    /// throw — <c>[households]</c>' band takes the same shape. The refusal is about money with
    /// nowhere to sit rather than about the table being stated.
    /// </remarks>
    [Fact]
    public void A_zero_opening_balance_in_a_ruleset_that_names_no_money_loads()
    {
        Ruleset rules = Accepted($"{Moneyless}\n\n[treasury]\nopening_balance = 0\n");

        Assert.Equal(Money.Zero, rules.Treasury.OpeningBalance);
    }
}
