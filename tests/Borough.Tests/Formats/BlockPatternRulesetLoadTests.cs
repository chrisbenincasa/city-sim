using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary><c>[lots] block_pattern</c> and its refusal.</summary>
public sealed class BlockPatternRulesetLoadTests
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

    [Theory]
    [InlineData("", BlockPattern.Detached)]
    [InlineData("block_pattern = \"detached\"\n", BlockPattern.Detached)]
    [InlineData("block_pattern = \"perimeter\"\n", BlockPattern.Perimeter)]
    [InlineData("block_pattern = \"back_to_back\"\n", BlockPattern.BackToBack)]
    public void A_stated_pattern_reaches_the_lot_ruleset(string line, BlockPattern expected)
    {
        RulesetLoadResult result = Load(line);

        Assert.True(result.Ok, result.Describe());
        Assert.Equal(expected, result.Ruleset!.Lots.UnbandedPattern);
    }

    [Fact]
    public void An_unknown_pattern_is_refused()
    {
        RulesetLoadResult result = Load("block_pattern = \"tower\"\n");

        Assert.False(result.Ok);
        Assert.Contains("block_pattern", result.Refusals[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_band_the_stated_pattern_is_drawn()
    {
        Assert.Equal(BlockPattern.Perimeter,
            BlockPatterns.ForBand(0, 0, 32, 5, default, 0, 0, BlockPattern.Perimeter));
    }
}
