using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Formats;

public sealed class UtilityRulesetLoadTests
{
    private const string DeliveredUtility = """
        [[resource]]
        name = "power"
        family = "utility"
        storage = 0
        link_per_crossing = 120
        """;

    private const string Districts = """

        [districts]
        prominence_percent = 50
        revisit_ticks = 2048
        hysteresis_percent = 50
        migrate_cells = 16
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml, string key)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return Assert.Single(result.Refusals, refusal =>
            refusal.Reason.Contains(key, StringComparison.Ordinal));
    }

    private static string RuleWithTerm(bool input, string scope)
    {
        string term = $$"""{ scope = "{{scope}}", resource = "power", amount = 1 }""";

        return $$"""

            [[building]]
            name = "plant"

            [[rule]]
            name = "use_power"
            kind = "plant"
            rate = 10
            apply = { min = 1, max = 1 }
            inputs = [ {{(input ? term : "")}} ]
            outputs = [ {{(input ? "" : term)}} ]
            """;
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("loss_per_road_tile = 3", 3)]
    public void Delivered_utility_keys_reach_the_ruleset(string loss, int expectedLoss)
    {
        Ruleset ruleset = Accepted(DeliveredUtility + "\n" + loss);
        var power = new ResourceId(1);

        Assert.True(ruleset.IsDeliveredUtility(power));
        Assert.Equal(0, ruleset.StorageOf(power));
        Assert.Equal(120, ruleset.LinkPerCrossing(power));
        Assert.Equal(expectedLoss, ruleset.LossPerRoadTile(power));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Nonzero_storage_names_the_key_and_the_water_slice(int storage)
    {
        string toml = DeliveredUtility.Replace("storage = 0", $"storage = {storage}",
            StringComparison.Ordinal);

        RulesetRefusal refusal = Refused(toml, "storage");

        Assert.Contains("stored Utilities arrive with Water", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("storage", 0)]
    [InlineData("link_per_crossing", 120)]
    [InlineData("loss_per_road_tile", 3)]
    [InlineData("import_per_connection", 2560)]
    public void Delivery_keys_on_a_good_name_the_key_and_family(string key, int value)
    {
        RulesetRefusal refusal = Refused($$"""
            [[resource]]
            name = "flour"
            family = "good"
            {{key}} = {{value}}
            """, key);

        Assert.Contains("good", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Loss_without_a_link_names_both_keys()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "power"
            family = "utility"
            storage = 0
            loss_per_road_tile = 3
            """, "loss_per_road_tile");

        Assert.Contains("link_per_crossing", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_link_without_storage_names_both_keys()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "power"
            family = "utility"
            link_per_crossing = 120
            """, "storage");

        Assert.Contains("link_per_crossing", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the import price and its capacity ---------------------------------------------------

    /// <summary>One <c>[[hinterland]]</c> on the south edge pricing whatever it is handed.</summary>
    private static string Priced(string resource, int price) => $$"""

        [[hinterland]]
        edge = "south"
        emigrant_balance_min = 0
        emigrant_balance_max = 0
        prices = [ { resource = "{{resource}}", price = {{price}} } ]
        """;

    /// <summary>
    /// A delivered Utility carries an import price and a per-gate capacity, and both reach the
    /// Ruleset.
    /// </summary>
    [Fact]
    public void A_priced_delivered_utility_reaches_the_ruleset_with_its_capacity()
    {
        Ruleset ruleset = Accepted(
            DeliveredUtility + "\nimport_per_connection = 2560" + Priced("power", 7));
        var power = new ResourceId(1);

        Assert.Equal(2560, ruleset.ImportPerConnection(power));
        Assert.Equal(new Money(7), ruleset.ImportPrice(0, power));
        Assert.Equal(new Money(7), ruleset.ImportCeiling(power));
    }

    /// <summary>
    /// An undelivered Utility has no network to import onto, so a <c>prices</c> entry for one is
    /// refused.
    /// </summary>
    [Fact]
    public void A_price_for_an_undelivered_utility_names_the_key_and_the_resource()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "power"
            family = "utility"
            """ + Priced("power", 7), "prices");

        Assert.Contains("'power'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("link_per_crossing", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The same for the capacity half, which names the key and the Resource too.</summary>
    [Fact]
    public void A_capacity_on_an_undelivered_utility_names_the_key_and_the_resource()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "power"
            family = "utility"
            import_per_connection = 2560
            """ + Priced("power", 7), "import_per_connection");

        Assert.Contains("'power'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("link_per_crossing", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A price with no capacity is refused: no Outside Connection could bring any of it in, so the
    /// price would be read by nobody.
    /// </summary>
    [Fact]
    public void A_priced_utility_with_no_capacity_names_both_halves()
    {
        RulesetRefusal refusal =
            Refused(DeliveredUtility + Priced("power", 7), "import_per_connection");

        Assert.Contains("'power'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("[[hinterland]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A capacity no Hinterland prices is refused: the treasury pays per imported unit and there is
    /// no price for it to pay.
    /// </summary>
    [Fact]
    public void A_capacity_no_hinterland_prices_names_both_halves()
    {
        RulesetRefusal refusal = Refused(
            DeliveredUtility + "\nimport_per_connection = 2560", "import_per_connection");

        Assert.Contains("'power'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("[[hinterland]]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A delivered Utility that neither half names stays unimportable and loads.</summary>
    [Fact]
    public void A_delivered_utility_with_neither_half_loads_unimportable()
    {
        Ruleset ruleset = Accepted(DeliveredUtility);
        var power = new ResourceId(1);

        Assert.True(ruleset.IsDeliveredUtility(power));
        Assert.Equal(0, ruleset.ImportPerConnection(power));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_delivered_local_term_is_refused_without_any_pool_term(bool input)
    {
        RulesetRefusal refusal = Refused(DeliveredUtility + RuleWithTerm(input, "local"), "scope");

        Assert.Contains("delivered Utility 'power'", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("pool", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void An_undelivered_utility_pool_term_still_has_no_delivery(bool input)
    {
        const string UndeliveredUtility = """
            [[resource]]
            name = "power"
            family = "utility"

            [[resource]]
            name = "pound"
            family = "money"
            """;

        RulesetRefusal refusal = Refused(
            UndeliveredUtility + Districts + RuleWithTerm(input, "pool"), "pool");

        Assert.Contains("power", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("utility", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("no market", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_delivered_pool_term_loads_without_a_money_resource(bool input)
    {
        Ruleset ruleset = Accepted(DeliveredUtility + Districts + RuleWithTerm(input, "pool"));
        var rule = new RuleId(1);
        Term term = Assert.Single((input ? ruleset.Inputs(rule) : ruleset.Outputs(rule)).ToArray());

        Assert.Equal(1, ruleset.ResourceCount);
        Assert.Equal(Scope.Pool, term.Bin.Scope);
        Assert.Equal(new ResourceId(1), term.Bin.Resource);
        Assert.Equal(1, term.Amount);
    }

    [Fact]
    public void A_mixed_utility_and_service_pool_rule_still_needs_money()
    {
        const string Service = """

            [[resource]]
            name = "accounting"
            family = "service"
            """;
        string rule = RuleWithTerm(input: false, "pool").Replace("inputs = [  ]",
            """inputs = [ { scope = "pool", resource = "accounting", amount = 1 } ]""",
            StringComparison.Ordinal);

        Refused(DeliveredUtility + Service + Districts + rule, "money");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_delivered_pool_term_without_districts_names_the_table(bool input)
    {
        Refused(DeliveredUtility + RuleWithTerm(input, "pool"), "[districts]");
    }
}
