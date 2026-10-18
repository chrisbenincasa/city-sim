using Borough.Core.Tables;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

public sealed class ServiceRulesetLoadTests
{
    private const string Accounting = """
        name = "accounting"
        family = "service"
        reach_minutes = 30
        """;

    private const string ServiceInput = """{ scope = "pool", resource = "accounting", amount = 1 },""";

    private const string Restock = """inputs  = [ { scope = "pool", resource = "sundries", amount = 4 } ]""";

    // Both output lists, up to but not including the closing bracket, so a case appends a term.
    private const string ShopOutputs = """outputs = [ { scope = "local", resource = "sundries", amount = 8 }""";

    private const string HouseOutputs = """outputs = [ { scope = "local", resource = "sundries", amount = 4 }""";

    private const string RatesOutputs = """outputs = [ { scope = "global", resource = "money", amount = 16384 }""";

    private const string MakesAccounting = """, { scope = "local", resource = "accounting", amount = 1 }""";

    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "serviced.toml"));

    private static RulesetLoadResult Parse(string text) => RulesetLoader.Parse(text, "serviced.toml");

    [Fact]
    public void A_service_loads_with_its_reach_and_its_hinterland_price()
    {
        RulesetLoadResult loaded = Parse(Text());

        Assert.True(loaded.Ok, loaded.Describe());
        Ruleset rules = loaded.Ruleset!;
        var accounting = new ResourceId(3);
        Assert.Equal(ResourceFamily.Service, rules.Family(accounting));
        Assert.Equal(new ServiceReach(30), rules.ReachOf(accounting));
        Assert.True(rules.IsTraded(accounting));
        Assert.Equal(90, rules.ImportPrice(0, accounting).Raw);
        Assert.True(rules.ReachOf(new ResourceId(4)).IsUnlimited);
    }

    [Fact]
    public void A_remote_service_reaches_any_distance()
    {
        RulesetLoadResult loaded = Parse(Text().Replace("reach_minutes = 30", "remote = true"));

        Assert.True(loaded.Ok, loaded.Describe());
        Assert.True(loaded.Ruleset!.ReachOf(new ResourceId(3)).IsUnlimited);
    }

    [Theory]
    [InlineData("reach_minutes = 30\n", "", "reach")]
    [InlineData("reach_minutes = 30\n", "reach_minutes = 30\nremote = true\n", "reach")]
    [InlineData("reach_minutes = 30\n", "remote = false\n", "reach")]
    [InlineData("reach_minutes = 30\n", "reach_minutes = 0\n", "reach_minutes")]
    [InlineData("name = \"repairs\"\nfamily = \"good\"\n", "name = \"repairs\"\nfamily = \"good\"\nreach_minutes = 30\n", "Professional Service")]
    [InlineData(ServiceInput, "{ scope = \"local\", resource = \"accounting\", amount = 1 },", "pool input")]
    [InlineData(ServiceInput, ServiceInput + "\n    " + ServiceInput, "twice")]
    [InlineData(ShopOutputs, ShopOutputs + ", { scope = \"pool\", resource = \"accounting\", amount = 1 }", "cannot hold one")]
    [InlineData("{ resource = \"repairs\",  capacity = 4 },\n    { resource = \"money\",    owner = \"business\" },", "{ resource = \"repairs\",  capacity = 4 },\n    { resource = \"accounting\", capacity = 4 },\n    { resource = \"money\",    owner = \"business\" },", "never stocked")]
    public void A_service_declaration_that_cannot_work_is_refused(string from, string to, string named)
    {
        string text = Text();
        Assert.Contains(from, text);

        RulesetLoadResult loaded = Parse(text.Replace(from, to));

        Assert.False(loaded.Ok);
        Assert.Contains(named, loaded.Describe());
    }

    /// <summary>
    /// A provider is a firm with staff, and never its own client. Each refusal here is a Ruleset that
    /// would otherwise load an office nobody could run, or one that could contract with itself.
    /// </summary>
    [Theory]
    [InlineData(HouseOutputs, "makes the service")]
    [InlineData(HouseOutputs, "no Business runs")]
    [InlineData(RatesOutputs, "spends no labour")]
    [InlineData(ShopOutputs, "both buys and makes")]
    public void A_provider_that_could_not_run_is_refused(string outputs, string named)
    {
        string text = Text();
        Assert.Contains(outputs, text);

        RulesetLoadResult loaded = Parse(text.Replace(outputs, outputs + MakesAccounting));

        Assert.False(loaded.Ok);
        Assert.Contains(named, loaded.Describe());
    }

    [Fact]
    public void An_office_loads_as_a_provider_of_both_its_services()
    {
        RulesetLoadResult loaded = RulesetLoader.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "offices.toml")),
            "offices.toml");

        Assert.True(loaded.Ok, loaded.Describe());
        Ruleset rules = loaded.Ruleset!;
        var accounting = new ResourceId(3);
        var insurance = new ResourceId(4);
        Assert.Equal(ResourceFamily.Service, rules.Family(accounting));
        Assert.Equal(new ServiceReach(30), rules.ReachOf(accounting));
        Assert.True(rules.ReachOf(insurance).IsUnlimited);

        // The Pool price an office charges is the import ceiling, which is the lowest price any
        // declared Hinterland names. Every edge prices both services, or that floor would be zero.
        Assert.Equal(90, rules.ImportCeiling(accounting).Raw);
        Assert.Equal(30, rules.ImportCeiling(insurance).Raw);
    }

    [Fact]
    public void A_household_buying_a_service_is_refused()
    {
        RulesetLoadResult loaded = Parse(Text().Replace(
            Restock,
            Restock.Replace(" ]", ", { scope = \"pool\", resource = \"accounting\", amount = 1 } ]")));

        Assert.False(loaded.Ok);
        Assert.Contains("no Business runs it", loaded.Describe());
    }

    [Theory]
    [InlineData("money", "is a money")]
    [InlineData("labour", "spends labour from outside its own premises")]
    public void A_pool_term_on_money_or_labour_is_refused(string resource, string named)
    {
        RulesetLoadResult loaded = Parse(Text().Replace(
            Restock, Restock.Replace("\"sundries\"", $"\"{resource}\"")));

        Assert.False(loaded.Ok);
        Assert.Contains(named, loaded.Describe());
    }

    [Fact]
    public void A_hinterland_buys_nothing_without_an_export_budget()
    {
        RulesetLoadResult loaded = Parse(Text());

        Assert.True(loaded.Ok, loaded.Describe());
        Assert.All(loaded.Ruleset!.Hinterlands, h => Assert.Equal(0, h.ExportBudget.Raw));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("2147483648")]
    public void An_export_budget_out_of_range_is_refused(string budget)
    {
        RulesetLoadResult loaded = Parse(Text().Replace(
            "edge = \"north\"\n", $"edge = \"north\"\nexport_budget = {budget}\n"));

        Assert.False(loaded.Ok);
        Assert.Contains("export_budget", loaded.Describe());
    }

    [Fact]
    public void A_pool_term_on_a_utility_is_refused()
    {
        string text = Text().Replace(Accounting, "name = \"accounting\"\nfamily = \"utility\"")
            .Replace("  { resource = \"accounting\", price = 90 },\n", "")
            .Replace("  { resource = \"accounting\", price = 110 },\n", "");

        RulesetLoadResult loaded = Parse(text);

        Assert.False(loaded.Ok);
        Assert.Contains("is a utility", loaded.Describe());
    }
}
