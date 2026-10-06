using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>The three <c>[roads]</c> minimums a Street lay is refused against, and their defaults.</summary>
public sealed class StreetMinimumsRulesetLoadTests
{
    private static RulesetLoadResult Load(string roads) => RulesetLoader.Parse($"""
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
        {roads}

        [lots]
        lots_per_segment = 5
        setback_tiles = 2
        """, "test.toml");

    [Fact]
    public void Absent_minimums_take_one_plot_width_thirty_degrees_and_half_a_block()
    {
        RulesetLoadResult result = Load("");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(12, result.Ruleset!.Roads.MinSegmentLengthTiles);
        Assert.Equal(30, result.Ruleset.Roads.MinCrossingAngleDegrees);
        Assert.Equal(16, result.Ruleset.Roads.MinCurveRadiusTiles);
    }

    [Fact]
    public void Stated_minimums_reach_the_road_ruleset()
    {
        RulesetLoadResult result = Load("""
            min_segment_length_tiles = 3
            min_crossing_angle_degrees = 45
            min_curve_radius_tiles = 40
            """);

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(3, result.Ruleset!.Roads.MinSegmentLengthTiles);
        Assert.Equal(45, result.Ruleset.Roads.MinCrossingAngleDegrees);
        Assert.Equal(40, result.Ruleset.Roads.MinCurveRadiusTiles);
    }

    [Theory]
    [InlineData("min_segment_length_tiles = 0")]
    [InlineData("min_crossing_angle_degrees = 0")]
    [InlineData("min_crossing_angle_degrees = 91")]
    [InlineData("min_curve_radius_tiles = 0")]
    public void A_minimum_out_of_range_is_refused(string line)
    {
        RulesetLoadResult result = Load(line);

        Assert.False(result.Ok);
        Assert.Contains(line[..line.IndexOf(' ', StringComparison.Ordinal)], result.Refusals[0].Reason,
            StringComparison.Ordinal);
    }
}
