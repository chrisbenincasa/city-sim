using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The four keys the targeted-policy catalogue adds to <c>[[policy]]</c> — <c>tool</c>,
/// <c>trade</c>, <c>ceiling</c> and <c>relief_percent</c> (<c>plans/0072</c> D10 to D13 and D28).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="BusinessTaxRulesetLoadTests"/>' discipline — the rule <c>adr/0048</c> states and
/// <c>RefusalCountTests</c> counts.
/// </para>
/// <para>
/// 🔴 <b>The matrix is the subject of this file and not the individual keys.</b> Each of
/// <c>transfer</c>, <c>ceiling</c> and <c>relief_percent</c> is well-formed on its own and legal on
/// some Policy, so the only thing that can be wrong is the company it keeps. That is why the tests
/// below run in <b>both</b> directions on every cell: a key required and absent, and the same key
/// present where the tool does not read it. ***A test that only checked the required half would pass
/// against a loader that accepted every key everywhere*** — which is precisely the <em>loads clean
/// and misbehaves in silence</em> city the refusals exist to prevent.
/// </para>
/// </remarks>
public sealed class PolicyToolRulesetLoadTests
{
    /// <summary>The smallest file the loader accepts, with two trades to aim a Policy at.</summary>
    /// <remarks>
    /// <b>Two <c>[[business]]</c> tables and not one.</b> A trade resolves to a kind id, and a single
    /// declared trade would pass a reader that returned any non-sentinel byte at all.
    /// </remarks>
    private const string City = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "pound"
        family = "money"

        [[building]]
        name = "dwelling"

        [[business]]
        name = "grocer"

        [[business]]
        name = "foundry"
        """;

    /// <summary>The transfer every tool but a relief states.</summary>
    private const string Transfer =
        "transfer = { from = \"local\", to = \"global\", resource = \"pound\", amount = 7 }";

    /// <summary>A well-formed <c>[[policy]]</c>, for the tests that vary one key of it.</summary>
    private static string Policy(
        string sweeps = "business",
        string? tool = null,
        string? trade = null,
        string? transfer = Transfer,
        string? ceiling = null,
        string? relief = null)
    {
        List<string> lines =
        [
            "[[policy]]",
            "name = \"levy\"",
            $"sweeps = \"{sweeps}\"",
            "interval = 2048",
            "apply = { min = 1, max = 1 }",
        ];

        if (tool is not null) { lines.Add($"tool = \"{tool}\""); }

        if (trade is not null) { lines.Add($"trade = \"{trade}\""); }

        if (transfer is not null) { lines.Add(transfer); }

        if (ceiling is not null) { lines.Add($"ceiling = {ceiling}"); }

        if (relief is not null) { lines.Add($"relief_percent = {relief}"); }

        return $"{City}\n\n{string.Join('\n', lines)}\n";
    }

    private static PolicyDefinition Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return Assert.Single(result.Ruleset!.Policies);
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    // ---- the default ------------------------------------------------------------------------------

    /// <summary>
    /// A <c>[[policy]]</c> stating neither key is the transfer it has always been.
    /// </summary>
    /// <remarks>
    /// <b>Every Policy shipped before the catalogue existed is this one</b>, so the absence of both
    /// keys had to be behaviour-preserving down to the byte — the State Hash of a standing world
    /// folds these fields.
    /// </remarks>
    [Fact]
    public void A_policy_naming_no_tool_and_no_trade_is_the_transfer_it_always_was()
    {
        PolicyDefinition policy = Accepted(Policy());

        Assert.Equal(PolicyTool.Transfer, policy.Tool);
        Assert.Equal(TradeKind.Any, policy.Trade);
        Assert.Equal(0, policy.Ceiling);
        Assert.Equal(7, policy.Amount);
    }

    // ---- tool -------------------------------------------------------------------------------------

    /// <summary>Each of the four spellings survives the load as its own tool.</summary>
    [Theory]
    [InlineData("transfer", PolicyTool.Transfer)]
    [InlineData("charge", PolicyTool.Charge)]
    public void A_moving_tool_survives_the_load(string spelling, PolicyTool tool) =>
        Assert.Equal(tool, Accepted(Policy(tool: spelling)).Tool);

    /// <summary>A relief states its percentage and no transfer, and loads.</summary>
    [Fact]
    public void A_relief_survives_the_load()
    {
        PolicyDefinition policy = Accepted(Policy(tool: "relief", transfer: null, relief: "25"));

        Assert.Equal(PolicyTool.Relief, policy.Tool);
        Assert.Equal(25, policy.Amount);
    }

    /// <summary>A subsidy states a transfer out of the treasury and a ceiling, and loads.</summary>
    [Fact]
    public void A_subsidy_survives_the_load()
    {
        PolicyDefinition policy = Accepted(Policy(
            tool: "subsidy",
            transfer: "transfer = { from = \"global\", to = \"local\", resource = \"pound\", "
                + "amount = 7 }",
            ceiling: "500"));

        Assert.Equal(PolicyTool.Subsidy, policy.Tool);
        Assert.Equal(500, policy.Ceiling);
        Assert.Equal(Scope.Global, policy.From);
    }

    /// <summary>
    /// A <c>tool</c> naming none of the four is refused rather than falling back to a transfer.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The refusal that matters most in this file.</b> A misspelling that defaulted would be a
    /// Policy paying out of the swept Business's own till for ever, reading on the page as a grant.
    /// </remarks>
    [Fact]
    public void A_tool_naming_none_of_the_four_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(tool: "rebate"));

        Assert.Contains("rebate", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("subsidy", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- trade ------------------------------------------------------------------------------------

    /// <summary>A trade resolves to the kind id the second <c>[[business]]</c> was registered under.</summary>
    [Fact]
    public void A_trade_resolves_to_the_business_kind_it_names()
    {
        Assert.Equal(1, Accepted(Policy(trade: "grocer")).Trade);
        Assert.Equal(2, Accepted(Policy(trade: "foundry")).Trade);
    }

    /// <summary>A trade nothing declares is refused, because the Policy would reach nobody.</summary>
    [Fact]
    public void A_trade_no_business_declares_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(trade: "bakery"));

        Assert.Contains("bakery", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("[[business]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A trade on a Policy sweeping households is refused.</summary>
    /// <remarks>
    /// <b>The key is individually legal and the sweep is individually legal.</b> A Household has no
    /// trade, so the pair is what is wrong: the key would be saved, hashed, carried across a reload
    /// and narrow nothing, while reading as a targeting decision somebody made.
    /// </remarks>
    [Fact]
    public void A_trade_on_a_household_sweep_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(sweeps: "household", trade: "grocer"));

        Assert.Contains("grocer", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("Household has no trade", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the matrix: transfer ---------------------------------------------------------------------

    /// <summary>A transfer and a charge owe a <c>transfer</c>.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("transfer")]
    [InlineData("charge")]
    public void A_moving_tool_states_a_transfer(string? tool)
    {
        RulesetRefusal refusal = Refused(Policy(tool: tool, transfer: null));

        Assert.Contains("no transfer", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>And so does a subsidy, which is the tool that draws on the treasury.</summary>
    [Fact]
    public void A_subsidy_states_a_transfer()
    {
        RulesetRefusal refusal = Refused(Policy(tool: "subsidy", transfer: null, ceiling: "500"));

        Assert.Contains("no transfer", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A relief states no transfer, and one is refused rather than ignored.</summary>
    /// <remarks>
    /// <b>A relief moves no money anywhere</b> (D10). A Policy that both forgoes revenue and moves
    /// money is two Policies, and the one that moves money has to be funded as a subsidy.
    /// </remarks>
    [Fact]
    public void A_relief_stating_a_transfer_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(tool: "relief", relief: "25"));

        Assert.Contains("relief states a transfer", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the matrix: ceiling ----------------------------------------------------------------------

    /// <summary>A subsidy states its funding bound rather than defaulting one.</summary>
    [Fact]
    public void A_subsidy_states_a_ceiling()
    {
        RulesetRefusal refusal = Refused(Policy(
            tool: "subsidy",
            transfer: "transfer = { from = \"global\", to = \"local\", resource = \"pound\", "
                + "amount = 7 }"));

        Assert.Contains("no ceiling", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A ceiling on anything but a subsidy is refused.</summary>
    /// <remarks>
    /// ⚠ <b>This is the cell the whole matrix exists for.</b> A <c>ceiling</c> on a charge parses,
    /// is saved, is hashed, survives a reload and is consulted by nothing — while reading on the page
    /// as a cap on what the city collects.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("transfer")]
    [InlineData("charge")]
    public void A_ceiling_on_a_moving_tool_is_refused(string? tool)
    {
        RulesetRefusal refusal = Refused(Policy(tool: tool, ceiling: "500"));

        Assert.Contains("ceiling is stated", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>And a ceiling on a relief, which pays nobody at all.</summary>
    [Fact]
    public void A_ceiling_on_a_relief_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(
            tool: "relief", transfer: null, relief: "25", ceiling: "500"));

        Assert.Contains("ceiling is stated", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A ceiling of zero is a subsidy shipped switched off, and is accepted.</summary>
    /// <remarks>
    /// <b>A real answer rather than the mechanism disabling itself.</b> The catalogue entry exists
    /// and the player raises it through <c>Govern</c>; nothing is paid until they do.
    /// </remarks>
    [Fact]
    public void A_ceiling_of_zero_is_accepted()
    {
        PolicyDefinition policy = Accepted(Policy(
            tool: "subsidy",
            transfer: "transfer = { from = \"global\", to = \"local\", resource = \"pound\", "
                + "amount = 7 }",
            ceiling: "0"));

        Assert.Equal(0, policy.Ceiling);
        Assert.Equal(PolicyTool.Subsidy, policy.Tool);
    }

    /// <summary>A negative ceiling is refused: the ration would be gone before the first claimant.</summary>
    [Fact]
    public void A_negative_ceiling_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(
            tool: "subsidy",
            transfer: "transfer = { from = \"global\", to = \"local\", resource = \"pound\", "
                + "amount = 7 }",
            ceiling: "-1"));

        Assert.Contains("ceiling = -1", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the matrix: relief_percent ---------------------------------------------------------------

    /// <summary>A relief states its percentage, because nothing else on it carries one.</summary>
    [Fact]
    public void A_relief_states_a_percentage()
    {
        RulesetRefusal refusal = Refused(Policy(tool: "relief", transfer: null));

        Assert.Contains("no relief_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A relief percentage on anything that moves money is refused.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("transfer")]
    [InlineData("charge")]
    public void A_relief_percent_on_a_moving_tool_is_refused(string? tool)
    {
        RulesetRefusal refusal = Refused(Policy(tool: tool, relief: "25"));

        Assert.Contains("relief_percent is stated", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>And on a subsidy, which spends rather than forgoes.</summary>
    [Fact]
    public void A_relief_percent_on_a_subsidy_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(
            tool: "subsidy",
            transfer: "transfer = { from = \"global\", to = \"local\", resource = \"pound\", "
                + "amount = 7 }",
            ceiling: "500",
            relief: "25"));

        Assert.Contains("relief_percent is stated", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A percentage outside 0..100 is not a share of a bill.</summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("101")]
    public void A_relief_percent_outside_a_hundred_percent_is_refused(string percent)
    {
        RulesetRefusal refusal = Refused(Policy(tool: "relief", transfer: null, relief: percent));

        Assert.Contains($"relief_percent = {percent}", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Both ends of the range are accepted, and carried in the amount.</summary>
    /// <remarks>
    /// <b>Zero is a relief switched off and 100 is a bill forgiven whole</b>, and both are cities
    /// somebody could mean. The percentage rides in <c>Amount</c> because a relief has no transfer
    /// to occupy it, which is what puts it under the existing <c>Govern</c> verb.
    /// </remarks>
    [Theory]
    [InlineData("0", 0)]
    [InlineData("100", 100)]
    public void The_ends_of_the_relief_range_are_accepted(string percent, int expected) =>
        Assert.Equal(
            expected,
            Accepted(Policy(tool: "relief", transfer: null, relief: percent)).Amount);

    // ---- the direction ----------------------------------------------------------------------------

    /// <summary>A subsidy paying into the treasury is refused.</summary>
    /// <remarks>
    /// 🔴 <b>The contradiction is refused rather than left to the author.</b> A subsidy declared to
    /// pay INTO the treasury is a charge wearing the wrong name, and nothing downstream would notice:
    /// it would be rationed by its ceiling exactly as written, so the bound would cap what the city
    /// COLLECTS while every reader of the file took it for a cap on what the city spends.
    /// </remarks>
    [Fact]
    public void A_subsidy_paying_into_the_treasury_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(tool: "subsidy", ceiling: "500"));

        Assert.Contains("subsidy draws from", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("OUT OF THE TREASURY", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>And a charge paying out of it, which is a subsidy with no funding.</summary>
    [Fact]
    public void A_charge_paying_out_of_the_treasury_is_refused()
    {
        RulesetRefusal refusal = Refused(Policy(
            tool: "charge",
            transfer: "transfer = { from = \"global\", to = \"local\", resource = \"pound\", "
                + "amount = 7 }"));

        Assert.Contains("charge pays to", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("TO THE TREASURY", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the reload -------------------------------------------------------------------------------

    /// <summary>Retuning a relief's percentage needs no migration.</summary>
    /// <remarks>
    /// <b><c>adr/0015</c>'s acceptance test on the new keys.</b> A relief has no transfer, so
    /// <see cref="RulesetShape"/> reads ends and a Resource that nothing authored — which is why the
    /// loader gives it a real Resource id rather than a zero one. This test is what would fail if it
    /// stopped doing so.
    /// </remarks>
    [Fact]
    public void Retuning_a_relief_is_a_reload_that_needs_no_migration()
    {
        RulesetLoadResult before = RulesetLoader.Parse(
            Policy(tool: "relief", transfer: null, relief: "25"), "before.toml");
        RulesetLoadResult after = RulesetLoader.Parse(
            Policy(tool: "relief", transfer: null, relief: "40"), "after.toml");

        Assert.True(before.Ok, before.Describe());
        Assert.True(after.Ok, after.Describe());
        Assert.Equal(RulesetChange.None, RulesetShape.Compare(before.Ruleset!, after.Ruleset!));
    }
}
