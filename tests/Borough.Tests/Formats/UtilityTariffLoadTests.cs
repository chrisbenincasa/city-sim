using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Utilities;

namespace Borough.Tests.Formats;

public sealed class UtilityTariffLoadTests
{
    private static string Billed => UtilityBillingTests.Source + UtilityBillingTests.Tariff;

    private static void Refused(string text, string reason)
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(text, "tariff.toml");
        Assert.False(loaded.Ok, loaded.Describe());
        Assert.Contains(loaded.Refusals, refusal => refusal.Reason.Contains(reason, StringComparison.Ordinal));
    }

    [Fact]
    public void A_tariff_is_governable_and_names_its_delivered_utility()
    {
        Ruleset rules = UtilityBillingTests.Load(Billed);
        PolicyDefinition policy = Assert.Single(rules.Policies);
        Assert.Equal(PolicyTool.Tariff, policy.Tool);
        Assert.Equal(3, policy.Amount);
        Assert.Equal(0, rules.TariffOf(policy.Utility));
        Assert.Equal(-1, rules.TariffOf(policy.Resource));
    }

    [Fact]
    public void A_zero_tariff_loads()
    {
        Ruleset rules = UtilityBillingTests.Load(Billed.Replace("amount = 3", "amount = 0", StringComparison.Ordinal));
        Assert.Equal(0, Assert.Single(rules.Policies).Amount);
    }

    [Fact]
    public void A_tariff_requires_a_utility() =>
        Refused(Billed.Replace("utility = \"power\"", "", StringComparison.Ordinal), "must name its delivered Utility");

    [Theory]
    [InlineData("token")]
    [InlineData("missing")]
    public void A_tariff_requires_a_delivered_utility_resource(string resource) =>
        Refused(Billed.Replace("utility = \"power\"", $"utility = \"{resource}\"", StringComparison.Ordinal), "must name a delivered Utility");

    [Fact]
    public void An_undelivered_utility_cannot_have_a_tariff() =>
        Refused(Billed.Replace("link_per_crossing = 120", "", StringComparison.Ordinal), "must name a delivered Utility");

    [Fact]
    public void Two_tariffs_cannot_name_the_same_utility() =>
        Refused(Billed + UtilityBillingTests.Tariff.Replace("power_tariff", "duplicate", StringComparison.Ordinal), "two tariffs");

    [Theory]
    [InlineData("from = \"global\", to = \"local\", resource = \"money\", amount = 3")]
    [InlineData("from = \"local\", to = \"global\", resource = \"token\", amount = 3")]
    public void A_tariff_requires_local_to_global_money(string transfer) =>
        Refused(Billed.Replace("from = \"local\", to = \"global\", resource = \"money\", amount = 3", transfer, StringComparison.Ordinal), "tariff transfer must move money from local to global");

    [Fact]
    public void A_negative_tariff_is_refused() =>
        Refused(Billed.Replace("amount = 3", "amount = -1", StringComparison.Ordinal), "A tariff allows 0");

    [Fact]
    public void A_non_tariff_policy_cannot_name_a_utility() =>
        Refused(Billed.Replace("tool = \"tariff\"", "tool = \"transfer\"\nsweeps = \"household\"\ninterval = 1\napply = { min = 1, max = 1 }", StringComparison.Ordinal), "utility is allowed only on a tariff");

    [Theory]
    [InlineData("sweeps = \"household\"", "sweeps")]
    [InlineData("interval = 1", "interval")]
    [InlineData("apply = { min = 1, max = 1 }", "apply")]
    [InlineData("trade = \"shop\"", "trade")]
    [InlineData("ceiling = 1", "ceiling")]
    [InlineData("relief_percent = 1", "relief_percent")]
    public void A_tariff_refuses_sweep_and_other_tool_keys(string key, string reason) =>
        Refused(Billed + "\n" + key, reason);

    [Fact]
    public void A_premises_rule_drawing_a_tariffed_utility_is_refused() =>
        Refused(Billed.Replace("owner = \"occupant\"", "owner = \"premises\"", StringComparison.Ordinal), "premises Rule draws a tariffed Utility");
}
