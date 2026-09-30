using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

public sealed class CarParkCentreRulesetLoadTests
{
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    private const string Lots = """
        [lots]
        lots_per_segment = 5
        setback_tiles = 2
        """;

    private const string Stalls = """
        stall_width_centimetres = 250
        stall_length_centimetres = 500
        aisle_width_centimetres = 600
        """;

    private static string Parking(string body) =>
        $"[parking]\nradius_metres = 400\nshed_keeps = 24\n{body}";

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

    [Fact]
    public void Stall_sizes_load_in_centimetres()
    {
        Ruleset rules = Accepted($"{Nothing}\n\n{Parking(Stalls)}");

        Assert.Equal(new StallSizes(250, 500, 600), rules.Parking.Stalls);
    }

    [Fact]
    public void Parking_without_stall_sizes_lays_out_no_stalls()
    {
        Ruleset rules = Accepted($"{Nothing}\n\n{Parking(string.Empty)}");

        Assert.False(rules.Parking.Stalls.Runs);
    }

    [Fact]
    public void A_stall_size_without_the_width_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n{Parking("stall_length_centimetres = 500")}");

        Assert.Contains("come as a set", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_width_without_the_other_sizes_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n{Parking("stall_width_centimetres = 250")}");

        Assert.Contains("stall_length_centimetres", refusal.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public void A_stall_size_out_of_range_is_refused(int width)
    {
        string body = Stalls.Replace("= 250", $"= {width}", StringComparison.Ordinal);

        RulesetRefusal refusal = Refused($"{Nothing}\n\n{Parking(body)}");

        Assert.Contains("between 1 and 10000", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Trade_form_switches_centres_on()
    {
        Ruleset rules = Accepted(
            $"{Nothing}\n\n{Lots}\ntrade_form = \"car_park_centre\"\n\n{Parking(Stalls)}");

        Assert.True(rules.Lots.CarParkCentres);
    }

    [Fact]
    public void Centres_are_off_without_trade_form()
    {
        Ruleset rules = Accepted($"{Nothing}\n\n{Lots}\n\n{Parking(Stalls)}");

        Assert.False(rules.Lots.CarParkCentres);
    }

    [Fact]
    public void An_unknown_trade_form_is_refused()
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n{Lots}\ntrade_form = \"strip_mall\"\n\n{Parking(Stalls)}");

        Assert.Contains("is not a trade form", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Centres_without_stall_sizes_are_refused()
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n{Lots}\ntrade_form = \"car_park_centre\"\n\n{Parking(string.Empty)}");

        Assert.Contains("needs [parking] stall_width_centimetres", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Trade_form_weights_load_and_an_omitted_form_weighs_one()
    {
        Ruleset rules = Accepted(
            $"{Nothing}\n\n{Lots}\ntrade_form = \"by_band\"\n"
            + "trade_form_weights = { car_park_centre = 3, sales_yard = 0 }\n\n"
            + Parking(Stalls));

        Assert.Equal(new TradeFormWeights(3, 0, 1, 1, 1, 1, 1), rules.Lots.TradeFormWeights);
    }

    [Theory]
    [InlineData("car_park_centre = 0, sales_yard = 0", "\"by_band\"")]
    [InlineData("supermarket = -1", "\"by_band\"")]
    [InlineData("sales_yard = 2", "\"car_park_centre\"")]
    public void Trade_form_weights_that_leave_a_tier_empty_or_lack_bands_are_refused(string weights, string form)
    {
        RulesetRefusal refusal = Refused(
            $"{Nothing}\n\n{Lots}\ntrade_form = {form}\ntrade_form_weights = {{ {weights} }}\n\n{Parking(Stalls)}");

        Assert.Contains("trade_form_weights", refusal.Reason, StringComparison.Ordinal);
    }
}
