using Borough.Core.Arithmetic;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The three <c>[placement]</c> keys 02 section 5.4 needs, and every refusal they state.
/// </summary>
/// <remarks>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <c>JobRulesetLoadTests</c>' discipline. The pair worth reading is the two directions of the same
/// rule: a scale without a μ weighs a term in a model the file does not ask for, and a μ without a
/// scale scores in units nobody authored.
/// </remarks>
public sealed class ChoiceModelRulesetLoadTests
{
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    private const string Whole = """
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3
        """;

    private static string With(string extra) => $"{Nothing}\n\n[placement]\n{Whole}\n{extra}";

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

    /// <summary>
    /// A file stating no μ gets the city it already had, and that is the model rather than a default.
    /// </summary>
    [Fact]
    public void A_ruleset_that_states_no_mu_states_no_choice_model()
    {
        PlacementRuleset placement = Accepted(With(string.Empty)).Placement;

        Assert.False(placement.Chooses);
        Assert.Equal(0, placement.MuPercent);
    }

    [Fact]
    public void A_stated_mu_arrives_in_q16_16()
    {
        PlacementRuleset placement = Accepted(With("""
            mu_percent                = 250
            centrality_tiles_per_unit = 2048
            rent_per_unit             = 120
            """)).Placement;

        Assert.True(placement.Chooses);
        Assert.Equal(Fixed.One * 5 / 2, placement.Mu);
        Assert.Equal(2048, placement.CentralityTilesPerUnit);
        Assert.Equal(120, placement.RentPerUnit);
    }

    [Fact]
    public void A_mu_with_no_centrality_scale_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            mu_percent    = 100
            rent_per_unit = 120
            """));

        Assert.Contains("centrality_tiles_per_unit", refusal.Reason);
    }

    [Fact]
    public void A_mu_with_no_rent_scale_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            mu_percent                = 100
            centrality_tiles_per_unit = 2048
            """));

        Assert.Contains("rent_per_unit", refusal.Reason);
    }

    [Fact]
    public void A_scale_with_no_mu_is_refused()
    {
        Assert.Contains("mu_percent", Refused(With("centrality_tiles_per_unit = 2048")).Reason);
        Assert.Contains("mu_percent", Refused(With("rent_per_unit = 120")).Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200_000)]
    public void A_mu_outside_the_band_is_refused(int mu)
    {
        RulesetRefusal refusal = Refused(With($"""
            mu_percent                = {mu}
            centrality_tiles_per_unit = 2048
            rent_per_unit             = 120
            """));

        Assert.Contains("mu_percent", refusal.Reason);
    }

    /// <summary>
    /// A scale of zero divides by nothing, so it is refused rather than read as an absent term.
    /// </summary>
    [Fact]
    public void A_scale_of_zero_is_refused()
    {
        Assert.Contains("centrality_tiles_per_unit", Refused(With("""
            mu_percent                = 100
            centrality_tiles_per_unit = 0
            rent_per_unit             = 120
            """)).Reason);

        Assert.Contains("rent_per_unit", Refused(With("""
            mu_percent                = 100
            centrality_tiles_per_unit = 2048
            rent_per_unit             = 0
            """)).Reason);
    }
}
