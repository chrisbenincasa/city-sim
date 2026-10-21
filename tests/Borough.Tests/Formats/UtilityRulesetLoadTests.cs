using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Formats;

public sealed class UtilityRulesetLoadTests
{
    private const string DeliveredUtility = """
        [[resource]]
        name = "power"
        family = "utility"
        storage = "none"
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
        Assert.Equal(UtilityStorage.None, ruleset.StorageOf(power));
        Assert.False(ruleset.HasReserve(power));
        Assert.Equal(120, ruleset.LinkPerCrossing(power));
        Assert.Equal(expectedLoss, ruleset.LossPerRoadTile(power));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("\"unknown\"")]
    public void Invalid_storage_names_both_accepted_strings(string storage)
    {
        string toml = DeliveredUtility.Replace("storage = \"none\"", $"storage = {storage}",
            StringComparison.Ordinal);

        RulesetRefusal refusal = Refused(toml, "storage");

        Assert.Contains("\"none\" or \"reserve\"", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Fills_storage_names_sewage()
    {
        string toml = DeliveredUtility.Replace("\"none\"", "\"fills\"", StringComparison.Ordinal);

        RulesetRefusal refusal = Refused(toml, "storage");

        Assert.Contains("arrives with Sewage", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("storage", "\"reserve\"")]
    [InlineData("link_per_crossing", "120")]
    [InlineData("loss_per_road_tile", "3")]
    public void Delivery_keys_on_a_good_name_the_key_and_family(string key, string value)
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
            storage = "none"
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

    /// <summary>
    /// A reserve with no link is refused rather than loading as a Bin nothing ever fills.
    /// </summary>
    /// <remarks>
    /// <b>The mode only means anything on a delivered Utility.</b> A tower Bin is filled by the
    /// settle and the settle runs on the networks a link opens, so <c>"reserve"</c> stated alone is
    /// <c>adr/0048</c>'s <em>loads clean and misbehaves in silence</em> — the Ruleset declares storage
    /// the city can neither fill nor draw.
    /// </remarks>
    [Fact]
    public void A_reserve_without_a_link_names_both_keys()
    {
        RulesetRefusal refusal = Refused("""
            [[resource]]
            name = "water"
            family = "utility"
            storage = "reserve"
            """, "link_per_crossing");

        Assert.Contains("storage = \"reserve\"", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("utility", "none", true, false)]
    [InlineData("utility", "reserve", true, true)]
    [InlineData("utility", "none", false, false)]
    [InlineData("utility", null, false, false)]
    [InlineData("good", null, false, false)]
    [InlineData("money", null, false, false)]
    public void HasReserve_requires_delivery_and_reserve_storage(
        string family, string? storage, bool delivered, bool expected)
    {
        Ruleset ruleset = Accepted($$"""
            [[resource]]
            name = "resource"
            family = "{{family}}"
            {{(storage is null ? "" : $"storage = \"{storage}\"")}}
            {{(delivered ? "link_per_crossing = 120" : "")}}
            """);

        Assert.Equal(expected, ruleset.HasReserve(new ResourceId(1)));
        Assert.False(ruleset.HasReserve(default));
        Assert.False(ruleset.HasReserve(new ResourceId(2)));
        Assert.False(Ruleset.Empty.HasReserve(new ResourceId(1)));
    }

    [Theory]
    [InlineData("reserve", true)]
    [InlineData("none", false)]
    public void Building_bins_accept_reserve_or_undelivered_utilities(string storage, bool delivered)
    {
        Ruleset ruleset = Accepted($$"""
            [[resource]]
            name = "water"
            family = "utility"
            storage = "{{storage}}"
            {{(delivered ? "link_per_crossing = 120" : "")}}

            [[building]]
            name = "tower"
            bins = [ { resource = "water", capacity = 65536 } ]
            """);

        Assert.Equal(new ResourceId(1), Assert.Single(ruleset.BinsOf(1).ToArray()).Resource);
    }

    [Fact]
    public void A_delivered_utility_bin_requires_reserve_storage()
    {
        RulesetRefusal refusal = Refused(DeliveredUtility + """

            [[building]]
            name = "tower"
            bins = [ { resource = "power", capacity = 65536 } ]
            """, "Building can hold a delivered Utility");

        Assert.Contains("only when it keeps a reserve", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal("tower", refusal.Rule);
    }

    [Theory]
    [InlineData("none", "reserve")]
    [InlineData("reserve", "none")]
    public void Storage_mode_reload_is_structural_and_adopts_without_retiring_delivery(
        string before, string after)
    {
        Ruleset current = Accepted(DeliveredUtility.Replace("\"none\"", $"\"{before}\"",
            StringComparison.Ordinal) + Districts);
        Ruleset replacement = Accepted(DeliveredUtility.Replace("\"none\"", $"\"{after}\"",
            StringComparison.Ordinal) + Districts);
        var power = new ResourceId(1);
        var world = new World(100, current);
        world.Districts.Create(Cells.Zero, Cells.Zero);
        world.FitDistrictPools();
        Assert.Equal(1, world.UtilityFlows.Rows.LiveCount);
        var network = world.UtilityFlows.Rows.At(0);
        var pool = world.UtilityFlows.Pool[0];
        world.UtilityFlows.Produced[0] = 23;

        Assert.Equal(RulesetChange.ResourceDelivery, RulesetShape.Compare(current, replacement));

        world.Adopt(replacement, contentHash: 0, Ticks.Zero, WorldKey.FromSeed(7));

        Assert.Same(replacement, world.Rules);
        Assert.Equal(after == "reserve", world.Rules.HasReserve(power));
        Assert.Equal(1, world.UtilityFlows.Rows.LiveCount);
        int flow = world.UtilityFlows.Rows.Resolve(network);
        Assert.Equal(pool, world.UtilityFlows.Pool[flow]);
        Assert.Equal(23, world.UtilityFlows.Produced[flow]);
        Assert.True(world.DistrictPools.Rows.TryResolve(pool, out _));
    }

    [Fact]
    public void Watered_fixture_declares_a_reserve_tower_and_pool_rules()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "watered.toml"));
        Assert.True(result.Ok, result.Describe());
        Ruleset ruleset = result.Ruleset!;
        var water = new ResourceId(1);

        Assert.Equal(2, ruleset.ResourceCount);
        Assert.True(ruleset.HasReserve(water));
        Assert.Equal(UtilityStorage.Reserve, ruleset.StorageOf(water));
        Assert.True(ruleset.LinkPerCrossing(water) > 0);
        Assert.True(ruleset.LossPerRoadTile(water) > 0);
        Assert.Equal(water, Assert.Single(ruleset.BinsOf(3).ToArray()).Resource);
        Assert.Equal(Scope.Pool, Assert.Single(ruleset.Outputs(new RuleId(1)).ToArray()).Bin.Scope);
        Assert.Equal(Scope.Pool, Assert.Single(ruleset.Inputs(new RuleId(2)).ToArray()).Bin.Scope);
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
