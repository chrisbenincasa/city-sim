using Borough.Formats;

namespace Borough.Tests.Formats;

public sealed class ShoppingRulesetTests
{
    private static string Text => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "shopping.toml"));

    [Theory]
    [InlineData("target_days = 3", "target_days = 1")]
    [InlineData("known_shops = 3", "known_shops = 0")]
    [InlineData("severe_need = -16", "severe_need = 0")]
    [InlineData("work_days = 31", "work_days = 128")]
    [InlineData("closes_hour = 21", "closes_hour = 7")]
    [InlineData("open_days = 127", "")]
    [InlineData("retry_ticks = 128", "retry_ticks = 0")]
    [InlineData("interval = 64", "interval = 0")]
    public void Invalid_shopping_content_is_refused(string before, string after)
    {
        var loaded = RulesetLoader.Parse(Text.Replace(before, after, StringComparison.Ordinal), "test.toml");
        Assert.False(loaded.Ok);
    }

    [Fact]
    public void A_shop_can_close_at_midnight()
    {
        var loaded = RulesetLoader.Parse(Text.Replace("closes_hour = 21", "closes_hour = 24", StringComparison.Ordinal), "test.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        Assert.True(loaded.Ruleset!.BusinessKind(2).ShopHours.IsOpen(new Borough.Core.Quantities.Ticks(
            (ulong)Borough.Core.Quantities.Ticks.AtClock(23))));
    }
}
