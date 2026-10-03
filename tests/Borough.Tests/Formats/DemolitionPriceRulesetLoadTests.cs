using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary><c>[lots] demolition_price_per_tile</c> and its refusals.</summary>
public sealed class DemolitionPriceRulesetLoadTests
{
    private const string Currency = """
        [[resource]]
        name = "money"
        family = "money"

        """;

    private const string Lots = """
        [roads]
        block_tiles = 32
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000

        [lots]
        lots_per_segment = 5
        setback_tiles = 2
        """;

    [Fact]
    public void A_stated_price_reaches_the_lot_ruleset()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Currency}{Lots}\ndemolition_price_per_tile = 40\n", "test.toml");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(new Money(40), result.Ruleset!.Lots.DemolitionPricePerTile);
    }

    [Fact]
    public void Absent_price_is_free()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Currency}{Lots}", "test.toml");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(Money.Zero, result.Ruleset!.Lots.DemolitionPricePerTile);
    }

    [Fact]
    public void A_negative_price_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Currency}{Lots}\ndemolition_price_per_tile = -1\n", "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("demolition_price_per_tile", result.Refusals[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_price_in_a_file_with_no_money_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Lots}\ndemolition_price_per_tile = 40\n", "test.toml");

        Assert.False(result.Ok);
        Assert.Contains(result.Refusals, r => r.Reason.Contains("names no money", StringComparison.Ordinal));
    }
}
