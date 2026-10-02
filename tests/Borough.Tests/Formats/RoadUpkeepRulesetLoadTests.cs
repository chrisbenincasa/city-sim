using Borough.Core.Quantities;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary><c>[roads] upkeep_per_segment_per_day</c> and its refusals.</summary>
public sealed class RoadUpkeepRulesetLoadTests
{
    private const string Currency = """
        [[resource]]
        name = "money"
        family = "money"

        """;

    private const string Roads = """
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
        """;

    [Fact]
    public void A_stated_upkeep_reaches_the_road_ruleset()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Currency}{Roads}\nupkeep_per_segment_per_day = 512\n", "test.toml");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(new Money(512), result.Ruleset!.Roads.UpkeepPerSegmentPerDay);
    }

    [Fact]
    public void Absent_upkeep_is_free()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Currency}{Roads}", "test.toml");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(Money.Zero, result.Ruleset!.Roads.UpkeepPerSegmentPerDay);
    }

    [Fact]
    public void A_negative_upkeep_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Currency}{Roads}\nupkeep_per_segment_per_day = -1\n", "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("upkeep_per_segment_per_day", result.Refusals[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Upkeep_in_a_file_with_no_money_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse($"{Roads}\nupkeep_per_segment_per_day = 512\n", "test.toml");

        Assert.False(result.Ok);
        Assert.Contains("names no money", result.Refusals[0].Reason, StringComparison.Ordinal);
    }
}
