using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary><c>[lots] min_plot_depth_tiles</c> and its refusals.</summary>
public sealed class MinPlotDepthRulesetLoadTests
{
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

    private static RulesetLoadResult Load(string extra) => RulesetLoader.Parse($"{Lots}\n{extra}", "test.toml");

    [Fact]
    public void A_stated_depth_reaches_the_lot_ruleset()
    {
        RulesetLoadResult result = Load("min_plot_depth_tiles = 8\n");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(8, result.Ruleset!.Lots.MinPlotDepthTiles);
    }

    [Fact]
    public void Absent_depth_drops_overlapping_plots()
    {
        RulesetLoadResult result = Load("");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(0, result.Ruleset!.Lots.MinPlotDepthTiles);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(17)]
    public void A_depth_with_no_room_for_a_footprint_or_past_half_the_block_is_refused(int depth)
    {
        RulesetLoadResult result = Load($"min_plot_depth_tiles = {depth}\n");

        Assert.False(result.Ok);
        Assert.Contains("min_plot_depth_tiles", result.Refusals[0].Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(16)]
    public void The_range_ends_load(int depth)
    {
        RulesetLoadResult result = Load($"min_plot_depth_tiles = {depth}\n");

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(depth, result.Ruleset!.Lots.MinPlotDepthTiles);
    }
}
