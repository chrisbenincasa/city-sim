using System.Text.RegularExpressions;
using Borough.Core.Arithmetic;
using Borough.Formats;

namespace Borough.Tests.Review;

// Review probe A1: IntegerMath.RoundDiv(int, int) doubles its numerator unchecked, so a Q16.16
// value at or above 2^30 (16384 in whole units) wraps. The [layers] percent keys reach it through
// RulesetLoader with a minimum and no maximum.
public sealed class RoundDivOverflowProbe
{
    [Theory]
    [InlineData(400)]
    [InlineData(16383)]
    [InlineData(16384)]
    [InlineData(32767)]
    public void RoundDiv_matches_the_long_overload(int percent)
    {
        int q = Fixed.FromInt(percent);
        Assert.Equal(IntegerMath.RoundDiv((long)q, 100L), (long)IntegerMath.RoundDiv(q, 100));
    }

    [Theory]
    [InlineData(400, 262144)]
    [InlineData(20000, 13107200)]
    public void Noise_intensity_loads_with_the_stated_sign(int percent, int expected)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "care.toml");
        string text = Regex.Replace(
            File.ReadAllText(path), @"(?m)^noise_intensity_percent\s*=\s*\d+", $"noise_intensity_percent = {percent}");
        RulesetLoadResult loaded = RulesetLoader.Parse(text, path);
        Assert.NotNull(loaded.Ruleset);
        Assert.Equal(expected, loaded.Ruleset!.Layers.Desirability.NoiseSource.IntensityPerFlow);
    }

    [Fact]
    public void An_out_of_range_percent_is_refused_rather_than_thrown()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Rulesets", "care.toml");
        string text = Regex.Replace(
            File.ReadAllText(path), @"(?m)^noise_intensity_percent\s*=\s*\d+", "noise_intensity_percent = 40000");
        RulesetLoadResult loaded = RulesetLoader.Parse(text, path);
        Assert.Null(loaded.Ruleset);
    }
}
