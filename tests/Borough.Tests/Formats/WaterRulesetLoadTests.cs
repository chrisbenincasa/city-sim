using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 24 tasks 6a and 9: <c>[water]</c>, its two keys, and what each absence means.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0159</c> and <c>adr/0156</c>. <b>Two nested optionals, and the whole file is about what an
/// absence says.</b> No <c>[water]</c> is an inland world; <c>[water]</c> without
/// <c>flood_level_percent</c> is a steep coast. ⚠ <b>Neither is a defaulted number</b> — the absence
/// is the spelling, which is <c>adr/0123</c>'s rule about a mechanism that is present and
/// permanently zero, applied one level up at the file.
/// </para>
/// <para>
/// 🔴 <b><c>[water]</c> shipped at task 6a with no loader test at all, and this file is that gap
/// closed rather than task 9's own work.</b> The sea level's two refusals were written, reasoned about
/// in <c>adr/0048</c>'s enumeration, and never watched to fire. <c>CLAUDE.md</c>: *every diagnostic
/// ships with a test that writes the violation and watches it fire.*
/// </para>
/// </remarks>
public sealed class WaterRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    /// <summary>A Ruleset with no <c>[water]</c> is an inland world and a complete Ruleset.</summary>
    [Fact]
    public void A_Ruleset_with_no_water_table_is_a_complete_Ruleset()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.False(ruleset.Water.Stated);
        Assert.False(ruleset.Water.HasFloodplain);
    }

    /// <summary>
    /// ⚠ <b><c>[water]</c> without a flood level is a steep coast</b> — water, and no Hazard Region.
    /// </summary>
    [Fact]
    public void Water_without_a_flood_level_is_a_coast_with_no_floodplain()
    {
        Ruleset ruleset = Accepted($"{Nothing}\n\n[water]\nsea_level_percent = 25\n");

        Assert.True(ruleset.Water.Stated);
        Assert.Equal(25, ruleset.Water.SeaLevelPercent);
        Assert.False(ruleset.Water.HasFloodplain);
    }

    /// <summary>Both keys stated, which is what <c>rulesets/coastal.toml</c> does.</summary>
    [Fact]
    public void Water_with_a_flood_level_carries_both()
    {
        Ruleset ruleset =
            Accepted($"{Nothing}\n\n[water]\nsea_level_percent = 25\nflood_level_percent = 30\n");

        Assert.True(ruleset.Water.HasFloodplain);
        Assert.Equal(30, ruleset.Water.FloodLevelPercent);
    }

    /// <summary>
    /// A sea level outside 1–99 is refused, and <b>both ends are refused for different reasons.</b>
    /// </summary>
    /// <remarks>
    /// 0 is a second spelling of omitting the table — a designer who wrote it would mean something the
    /// generator cannot hear; 100 puts every Cell under water, which is not a city. <c>adr/0159</c>.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public void A_sea_level_outside_its_range_is_refused_by_name(int percent)
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[water]\nsea_level_percent = {percent}\n");

        Assert.Contains("sea_level_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// 🔴 <b>A flood level AT OR BELOW the sea is refused, and this is the interesting one.</b> It
    /// describes ground that is already under water, so the Hazard Region it defines is empty — a key
    /// somebody wrote deliberately, that loads clean, and that derives nothing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>That is <c>adr/0123</c>'s present-and-permanently-zero arriving in a loader rather than in
    /// a mechanism</b>, which is why it is a refusal and not a clamp: clamping would produce a world
    /// the file did not describe, and silence would produce an overlay that is always empty.
    /// </remarks>
    [Theory]
    [InlineData(25)]
    [InlineData(24)]
    [InlineData(1)]
    [InlineData(100)]
    public void A_flood_level_at_or_below_the_sea_is_refused_by_name(int flood)
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n[water]\nsea_level_percent = 25\nflood_level_percent = {flood}\n");

        Assert.Contains("flood_level_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⚠ <b>The refusal names the sea level it was measured against</b>, because *below the sea* is
    /// not a fact a reader can check without the other number in front of them.
    /// </summary>
    [Fact]
    public void The_flood_refusal_names_the_sea_level_it_was_compared_with()
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n[water]\nsea_level_percent = 40\nflood_level_percent = 30\n");

        Assert.Contains("40", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("30", refusal.Reason, StringComparison.Ordinal);
    }
}
