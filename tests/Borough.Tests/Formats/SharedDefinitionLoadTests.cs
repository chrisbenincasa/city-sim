using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The runtime factoring plan's Formats half: <c>[[basket]]</c> and <c>[[reserve]]</c> resolved into
/// today's numeric Ruleset.
/// </summary>
/// <remarks>
/// <b>Lowering and nothing else.</b> A basket becomes input terms carrying <c>Term.PerDay</c>, which
/// Core already spends; a reserve becomes a Bin's integer ceiling, which is derived at load and at
/// every swap. Nothing here reaches saved state, so the whole surface is a property of the file.
/// </remarks>
public sealed class SharedDefinitionLoadTests
{
    private const string Larder = "reserve = { profile = \"standard\", basket = \"basic\" }";
    private const string Roof = "{ resource = \"repairs\", capacity = 4 }";

    private const string Goods = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "good"

        [[resource]]
        name = "money"
        family = "money"
        """;

    /// <summary>A basket of one Good and three Days of cover, as <c>rulesets/stocked.toml</c>.</summary>
    private static string Shared(
        string owner = "owner = \"occupant\"", string use = "sundries = 250", int days = 3) =>
        $$"""
        [[basket]]
        name = "basic"
        {{owner}}
        use_per_day = { {{use}} }

        [[reserve]]
        name = "standard"
        days = {{days}}
        """;

    /// <summary>
    /// A dwelling whose larder is sized by a reserve and drawn by a basket Rule.
    /// </summary>
    /// <param name="larder">What the sundries Bin states about its ceiling.</param>
    /// <param name="roof">The second Bin, literal by default so both kinds appear together.</param>
    /// <param name="terms">What the Rule states about the Goods it moves.</param>
    /// <param name="apply">The Rule's apply count.</param>
    /// <param name="extra">A second Rule, for the shared-progress refusal.</param>
    private static string Dwelling(
        string larder = Larder,
        string roof = Roof,
        string terms = "basket = \"basic\"",
        string apply = "{ min = 1, max = 1 }",
        string extra = "") =>
        $$"""
        [[building]]
        name = "dwelling"
        houses = true
        premises = true
        bins = [
          { resource = "sundries", {{larder}}, owner = "occupant" },
          {{roof}},
        ]

        [[rule]]
        name   = "consume"
        kind   = "dwelling"
        rate   = 32
        apply  = {{apply}}
        {{terms}}
        {{extra}}
        """;

    private static string File(string? dwelling = null, string? shared = null) =>
        $"{Goods}\n\n{shared ?? Shared()}\n\n{dwelling ?? Dwelling()}\n";

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static string Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0].Reason;
    }

    [Fact]
    public void A_basket_supplies_the_rules_inputs_as_a_daily_quantity()
    {
        Term term = Assert.Single(Accepted(File()).Inputs(new RuleId(1)).ToArray());

        Assert.True(term.PerDay);
        Assert.Equal(250, term.Amount);
        Assert.Equal(Scope.Local, term.Bin.Scope);
        Assert.Equal(1, term.Bin.Resource.Raw);
    }

    /// <summary>
    /// The Rule keeps the tenancy its terms derive, which is the occupant's because the basket's
    /// Goods live in the occupant's Bin.
    /// </summary>
    [Fact]
    public void A_basket_rule_is_the_tenants_rule() =>
        Assert.Equal(BinTenancy.Occupant, Accepted(File()).Rule(new RuleId(1)).Tenancy);

    [Fact]
    public void A_reserve_derives_the_bins_ceiling_from_the_basket_and_the_days()
    {
        ReadOnlySpan<BinDeclaration> bins = Accepted(File()).BinsOf(1);

        Assert.Equal(750, bins[0].Capacity.Units);

        // A literal capacity beside a derived one, which stays supported.
        Assert.Equal(4, bins[1].Capacity.Units);
    }

    /// <summary>
    /// Changing the shared basket moves every ceiling sized from it, which is the whole reason the
    /// ceiling is not a number in the file.
    /// </summary>
    [Fact]
    public void Changing_the_basket_moves_the_derived_ceiling()
    {
        Ruleset ruleset = Accepted(File(shared: Shared(use: "sundries = 300")));

        Assert.Equal(900, ruleset.BinsOf(1)[0].Capacity.Units);
        Assert.Equal(300, ruleset.Inputs(new RuleId(1))[0].Amount);
    }

    /// <summary>
    /// A local override states Days and not units, so the shared basket still moves the Bin it
    /// excepts.
    /// </summary>
    [Fact]
    public void A_local_days_override_preserves_days_rather_than_a_frozen_capacity()
    {
        string excepted = Dwelling(
            larder: "reserve = { profile = \"standard\", basket = \"basic\", days = 5 }");

        Assert.Equal(1250, Accepted(File(excepted)).BinsOf(1)[0].Capacity.Units);
        Assert.Equal(1500,
            Accepted(File(excepted, Shared(use: "sundries = 300"))).BinsOf(1)[0].Capacity.Units);
    }

    /// <summary>
    /// The Goods are sorted by Resource id, so two files naming the same Goods in a different order
    /// lower to one term array and hash together.
    /// </summary>
    [Fact]
    public void A_baskets_goods_are_ordered_by_resource_and_not_by_how_they_were_typed()
    {
        string dwelling = Dwelling(
            roof: "{ resource = \"repairs\", capacity = 4, owner = \"occupant\" }");

        Term[] inputs =
            Accepted(File(dwelling, Shared(use: "repairs = 8, sundries = 250")))
                .Inputs(new RuleId(1)).ToArray();

        Assert.Equal([1, 2], inputs.Select(t => (int)t.Bin.Resource.Raw));
        Assert.Equal([250, 8], inputs.Select(t => t.Amount));
    }

    [Theory]
    [InlineData("basket = \"pantry\"", "not a declared [[basket]]")]
    [InlineData("basket = \"basic\"\ninputs  = []", "both a basket and inputs")]
    [InlineData("basket = \"basic\"\noutputs = []", "both a basket and outputs")]
    public void A_rules_basket_is_resolved_and_stands_alone(string terms, string reason) =>
        Assert.Contains(reason, Refused(File(Dwelling(terms: terms))), StringComparison.Ordinal);

    /// <summary>
    /// A daily quantity is spent once per firing, and <c>RuleEngine.PerFiring</c> throws on any other
    /// apply count saying the refusal belongs here.
    /// </summary>
    [Fact]
    public void A_basket_rule_applies_exactly_once() =>
        Assert.Contains("apply count that is not fixed at one",
            Refused(File(Dwelling(apply: "{ min = 1, max = 4 }"))), StringComparison.Ordinal);

    /// <summary>
    /// One Bin carries one consumption progress, so two daily draws on it would continue from each
    /// other's remainder and split the Day by firing order.
    /// </summary>
    [Fact]
    public void Two_daily_rules_cannot_draw_one_actors_bin()
    {
        const string Snack = """


            [[rule]]
            name   = "snack"
            kind   = "dwelling"
            rate   = 64
            apply  = { min = 1, max = 1 }
            basket = "basic"
            """;

        Assert.Contains("one consumption progress", Refused(File(Dwelling(extra: Snack))),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("reserve = { profile = \"deep\", basket = \"basic\" }",
        "'deep' is not a declared [[reserve]]")]
    [InlineData("reserve = { profile = \"standard\", basket = \"pantry\" }",
        "'pantry' is not a declared [[basket]]")]
    [InlineData("capacity = 48, reserve = { profile = \"standard\", basket = \"basic\" }",
        "both a capacity and a reserve")]
    public void A_bins_reserve_selection_is_resolved(string larder, string reason) =>
        Assert.Contains(reason, Refused(File(Dwelling(larder))), StringComparison.Ordinal);

    [Fact]
    public void A_bin_and_the_basket_sizing_it_belong_to_one_actor() =>
        Assert.Contains("basket 'basic' states what the business uses",
            Refused(File(shared: Shared(owner: "owner = \"business\""))),
            StringComparison.Ordinal);

    [Fact]
    public void A_basket_that_does_not_name_the_bins_resource_cannot_size_it()
    {
        string dwelling = Dwelling(
            roof: $"{{ resource = \"repairs\", {Larder}, owner = \"occupant\" }}");

        Assert.Contains("does not name this Bin's Resource", Refused(File(dwelling)),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Money has no ceiling for Days of cover to size, and no rate of use for a basket to state.
    /// </summary>
    [Fact]
    public void Money_is_refused_on_both_sides_of_the_indirection()
    {
        string dwelling = Dwelling(roof: $"{{ resource = \"money\", {Larder} }}");

        Assert.Contains("a money Bin declares no reserve", Refused(File(dwelling)),
            StringComparison.Ordinal);

        Assert.Contains("'money' is money",
            Refused(File(shared: Shared(use: "money = 250"))), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("flour = 4", "'flour' is not a declared [[resource]]")]
    [InlineData("sundries = 0", "is 0 a Day")]
    public void A_basket_states_goods_an_actor_really_uses(string use, string reason) =>
        Assert.Contains(reason, Refused(File(shared: Shared(use: use))),
            StringComparison.Ordinal);

    [Fact]
    public void A_basket_names_the_actor_whose_day_it_describes()
    {
        Assert.Contains("not a Bin owner",
            Refused(File(shared: Shared(owner: "owner = \"landlord\""))), StringComparison.Ordinal);

        Assert.Contains("declares no owner",
            Refused(File(shared: Shared(owner: string.Empty))), StringComparison.Ordinal);
    }

    [Fact]
    public void A_reserve_holds_at_least_one_day() =>
        Assert.Contains("days is 0", Refused(File(shared: Shared(days: 0))),
            StringComparison.Ordinal);

    /// <summary>
    /// A recipe shares inputs and outputs together and is not resolved by this build, so it is
    /// refused by name rather than read as an unknown key.
    /// </summary>
    [Fact]
    public void A_recipe_is_refused_by_name() =>
        Assert.Contains("states a recipe",
            Refused(File(Dwelling(terms: "recipe = \"bake\""))), StringComparison.Ordinal);

    /// <summary>
    /// A basket's Goods are named by the author, so they are neither refused as unknown keys nor
    /// published as keys of the format.
    /// </summary>
    [Fact]
    public void A_baskets_goods_are_not_keys_of_the_format()
    {
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, RulesetKeyKind>> surface =
            RulesetLoader.KeySurface(File(), "test.toml");

        Assert.False(surface.ContainsKey("[[basket]] use_per_day"));
        Assert.Contains("use_per_day", surface["[[basket]]"].Keys);
    }
}
