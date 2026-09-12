using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// <c>plans/0070</c> row 32: <c>[[building]] placement_cost</c> and every refusal it states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>TreasuryRulesetLoadTests</c>' discipline one key along.
/// </para>
/// <para>
/// 🔴 <b>The case worth its class remark is
/// <see cref="A_kind_that_states_no_placement_cost_is_placed_free"/>.</b> Every shipped world was
/// written before this key existed, and two headless instruments place schools through the same
/// verb against an empty treasury — so an absence that meant anything but <em>free</em> would
/// silently stop them. ***A key whose absence changes a shipped world is not an optional key.***
/// </para>
/// </remarks>
public sealed class PlacementCostRulesetLoadTests
{
    /// <summary>A file that names money and declares one service kind.</summary>
    private const string Named = """
        [[resource]]
        name = "money"
        family = "money"

        [[building]]
        name = "school"
        serves = "education"
        """;

    /// <summary>The same file with no money in it.</summary>
    private const string Moneyless = """
        [[resource]]
        name = "flour"
        family = "good"

        [[building]]
        name = "school"
        serves = "education"
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

    /// <summary>The key reaches the kind as the number the file states.</summary>
    [Fact]
    public void A_stated_placement_cost_reaches_the_kind()
    {
        Ruleset rules = Accepted($"{Named}\nplacement_cost = 262144\n");

        Assert.Equal(new Money(262_144), rules.Kind(1).PlacementCost);
    }

    /// <summary>
    /// 🔴 <b>Absent means free, which is what every kind meant before the key existed.</b>
    /// </summary>
    [Fact]
    public void A_kind_that_states_no_placement_cost_is_placed_free()
    {
        Ruleset rules = Accepted(Named);

        Assert.Equal(Money.Zero, rules.Kind(1).PlacementCost);
    }

    /// <summary>
    /// A stated zero is accepted and means what absence means.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The contrast with <c>arrivals_per_day</c> is the point.</b> A gate admitting nobody is a
    /// door that never opens, so a stated zero there is refused; free is a real price a designer may
    /// mean, so zero here is <c>rent</c>'s polarity and not the gate's.
    /// </remarks>
    [Fact]
    public void A_placement_cost_of_zero_is_accepted()
    {
        Ruleset rules = Accepted($"{Named}\nplacement_cost = 0\n");

        Assert.Equal(Money.Zero, rules.Kind(1).PlacementCost);
    }

    /// <summary>A placement the city is paid for is a construction subsidy nobody has designed.</summary>
    [Fact]
    public void A_negative_placement_cost_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Named}\nplacement_cost = -1\n");

        Assert.Contains("placement_cost is -1", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("construction subsidy", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A price above <see cref="int.MaxValue"/> is a kind no city can ever afford to place.
    /// </summary>
    /// <remarks>
    /// <b>The bound is <c>[treasury] opening_balance</c>'s own</b>, and the two are read against
    /// each other — a price past the largest balance a file may found loads clean and refuses every
    /// click.
    /// </remarks>
    [Fact]
    public void A_placement_cost_above_the_balance_ceiling_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Named}\nplacement_cost = 2147483648\n");

        Assert.Contains("2147483648", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("no city can ever afford to place", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A priced placement in a file naming no money has nothing to pay it out of.
    /// </summary>
    /// <remarks>
    /// <c>[treasury] opening_balance</c>'s money-family refusal at a fourth door, after
    /// <c>[households]</c>, <c>[[hinterland]]</c> and the treasury itself. The treasury holds one
    /// Bin per conserved Resource, so a file with no money Resource has no treasury to charge.
    /// </remarks>
    [Fact]
    public void A_placement_cost_in_a_file_that_names_no_money_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Moneyless}\nplacement_cost = 100\n");

        Assert.Contains("names no money", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>And a free placement in such a file is not, because it charges nothing.</summary>
    [Fact]
    public void A_placement_cost_of_zero_in_a_moneyless_file_is_accepted()
    {
        Ruleset rules = Accepted($"{Moneyless}\nplacement_cost = 0\n");

        Assert.Equal(Money.Zero, rules.Kind(1).PlacementCost);
    }
}
