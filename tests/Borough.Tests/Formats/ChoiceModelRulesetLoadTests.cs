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

    /// <summary>A money Resource, which an emigrant band needs a Bin for (adr/0114).</summary>
    private const string Coin = """
        [[resource]]
        name = "money"
        family = "money"
        """;

    private const string Whole = """
        interval      = 32
        revisit_ticks = 1024
        candidates    = 3
        """;

    private static string With(string extra) => $"{Nothing}\n\n[placement]\n{Whole}\n{extra}";

    /// <summary>Every choice-model key, well-formed.</summary>
    private const string Model = """
        mu_percent                = 100
        centrality_tiles_per_unit = 2048
        rent_per_unit             = 120
        moving_costs_rent         = 240
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
            moving_costs_rent         = 240
            """)).Placement;

        Assert.True(placement.Chooses);
        Assert.Equal(Fixed.One * 5 / 2, placement.Mu);
        Assert.Equal(2048, placement.CentralityTilesPerUnit);
        Assert.Equal(120, placement.RentPerUnit);
        Assert.Equal(240, placement.MovingCostsRent);
        Assert.Equal(Fixed.One * 2, placement.StayingPut);
    }

    [Fact]
    public void A_mu_with_no_centrality_scale_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            mu_percent        = 100
            rent_per_unit     = 120
            moving_costs_rent = 240
            """));

        Assert.Contains("centrality_tiles_per_unit", refusal.Reason);
    }

    [Fact]
    public void A_mu_with_no_rent_scale_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            mu_percent                = 100
            centrality_tiles_per_unit = 2048
            moving_costs_rent         = 240
            """));

        Assert.Contains("rent_per_unit", refusal.Reason);
    }

    [Fact]
    public void A_scale_with_no_mu_is_refused()
    {
        Assert.Contains("mu_percent", Refused(With("centrality_tiles_per_unit = 2048")).Reason);
        Assert.Contains("mu_percent", Refused(With("rent_per_unit = 120")).Reason);
        Assert.Contains("mu_percent", Refused(With("moving_costs_rent = 240")).Reason);
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
            moving_costs_rent         = 240
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
            moving_costs_rent         = 240
            """)).Reason);

        Assert.Contains("rent_per_unit", Refused(With("""
            mu_percent                = 100
            centrality_tiles_per_unit = 2048
            rent_per_unit             = 0
            moving_costs_rent         = 240
            """)).Reason);
    }

    [Fact]
    public void A_mu_with_no_moving_cost_is_refused()
    {
        RulesetRefusal refusal = Refused(With("""
            mu_percent                = 100
            centrality_tiles_per_unit = 2048
            rent_per_unit             = 120
            """));

        Assert.Contains("moving_costs_rent", refusal.Reason);
    }

    /// <summary>
    /// Zero is a city where moving costs nothing, which is why it is accepted and still required.
    /// </summary>
    /// <remarks>
    /// <b>The contrast with the two scales is the point.</b> A scale is a divisor and its zero
    /// divides by nothing; this is an amount, and its zero is a world somebody could mean. What the
    /// file may not do is state a choice model and leave a reader guessing which it meant.
    /// </remarks>
    [Fact]
    public void A_moving_cost_of_zero_is_a_world_and_not_an_omission()
    {
        PlacementRuleset placement = Accepted(With(Model.Replace(
            "moving_costs_rent         = 240",
            "moving_costs_rent         = 0",
            StringComparison.Ordinal))).Placement;

        Assert.True(placement.Chooses);
        Assert.Equal(0, placement.StayingPut);
    }

    /// <summary>A Hinterland in a file with a choice model owes the fields it will be scored on.</summary>
    /// <remarks>
    /// <b>The first refusal in this loader that reads <c>[[hinterland]]</c> against
    /// <c>[placement]</c>.</b> adr/0023 makes the Outside an ordinary row in the same comparison, and
    /// a row missing the columns everything else is scored on is not a row.
    /// </remarks>
    [Fact]
    public void A_hinterland_in_a_choosing_world_owes_a_rent_and_a_centrality()
    {
        Assert.Contains("rent", Refused(
            $"{Nothing}\n\n{Coin}\n\n[[hinterland]]\nedge = \"north\"\nemigrant_balance_min = 0\n"
            + $"emigrant_balance_max = 100\n\n[placement]\n{Whole}\n{Model}").Reason);

        Assert.Contains("centrality_tiles", Refused(
            $"{Nothing}\n\n{Coin}\n\n[[hinterland]]\nedge = \"north\"\nrent = 620\n"
            + "emigrant_balance_min = 0\nemigrant_balance_max = 100\n\n"
            + $"[placement]\n{Whole}\n{Model}").Reason);
    }

    /// <summary>And a Hinterland in a file with no choice model is refused for stating them.</summary>
    [Fact]
    public void A_hinterland_outside_a_choosing_world_is_refused_those_fields()
    {
        Assert.Contains("mu_percent", Refused(
            $"{Nothing}\n\n{Coin}\n\n[[hinterland]]\nedge = \"north\"\nrent = 620\n"
            + "emigrant_balance_min = 0\nemigrant_balance_max = 100").Reason);
    }

    [Fact]
    public void A_hinterland_states_both_fields_and_loads()
    {
        Ruleset rules = Accepted(
            $"{Nothing}\n\n{Coin}\n\n[[hinterland]]\nedge = \"north\"\nrent = 620\n"
            + "centrality_tiles = 9000\nemigrant_balance_min = 0\nemigrant_balance_max = 100\n\n"
            + $"[placement]\n{Whole}\n{Model}");

        Assert.True(rules.TryHinterland(Core.Space.MapEdge.North, out HinterlandDefinition north));
        Assert.Equal(620, north.Rent.Raw);
        Assert.Equal(9_000, north.CentralityTiles);
    }

    [Fact]
    public void A_negative_moving_cost_is_refused()
    {
        Assert.Contains("moving_costs_rent", Refused(With(Model.Replace(
            "moving_costs_rent         = 240",
            "moving_costs_rent         = -1",
            StringComparison.Ordinal))).Reason);
    }
}
