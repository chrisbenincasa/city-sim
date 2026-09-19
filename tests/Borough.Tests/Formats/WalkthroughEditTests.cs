using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The authoring guide's content edits to `rulesets/split/`, and the numbers it quotes for them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The guide walks a designer through adding a Good and a recipe, sharing a basket, sizing a Bin
/// from a reserve and making an exception, and quotes what the preview prints for each.</b> Those
/// numbers are derived — a capacity is <c>use_per_day × days</c> and never appears in a member — so
/// a loader change moves them without touching the guide, and the guide goes quietly wrong.
/// </para>
/// <para>
/// ⚠ <b>The edits are applied here rather than copied from the guide, so this holds the arithmetic
/// and not the prose.</b> A step renamed or reworded stays green; a step whose result changed does
/// not. The commands the guide prints are still unheld, as <c>ExamplePackageTests</c> says.
/// </para>
/// </remarks>
public sealed class WalkthroughEditTests
{
    private const string Manifest = "ruleset.toml";

    private const string SharedBasket = """
        [[basket]]
        id          = "basic"
        owner       = "occupant"
        use_per_day = { sundries = 128 }

        [[reserve]]
        id   = "standard"
        days = 3
        """;

    private const string SharedRecipe = """
        [[recipe]]
        id      = "mend"
        inputs  = [ { scope = "local", resource = "timber",  amount = 1 } ]
        outputs = [ { scope = "local", resource = "repairs", amount = 4 } ]
        """;

    private static Dictionary<string, string> Package()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Rulesets", "split");

        return Directory.GetFiles(folder, "*.toml")
            .ToDictionary(path => Path.GetFileName(path), File.ReadAllText, StringComparer.Ordinal);
    }

    private static RulesetSourceResult Resolve(Dictionary<string, string> members)
    {
        string manifest = members[Manifest];

        return RulesetSource.Resolve(RulesetCapture.FromEntries(
            Manifest,
            SourcePackage.Utf8(manifest),
            members.Where(m => m.Key != Manifest).Select(
                m => new KeyValuePair<string, byte[]>(m.Key, SourcePackage.Utf8(m.Value)))));
    }

    private static Dictionary<string, string> WithMember(
        Dictionary<string, string> members, string path, string text)
    {
        members[Manifest] = members[Manifest].Replace(
            "\"rules.toml\"]", $"\"rules.toml\", \"{path}\"]", StringComparison.Ordinal);

        members[path] = text;

        return members;
    }

    private static Dictionary<string, string> Basketed()
    {
        Dictionary<string, string> members = WithMember(Package(), "shared.toml", SharedBasket);

        members["dwelling.toml"] = members["dwelling.toml"].Replace(
            "{ resource = \"sundries\", capacity = 48, owner = \"occupant\" },",
            "{ resource = \"sundries\", reserve = { profile = \"standard\", basket = \"basic\" }, "
            + "owner = \"occupant\" },",
            StringComparison.Ordinal);

        members["rules.toml"] = members["rules.toml"].Replace(
            """
            rate    = 32
            apply   = { min = 1, max = 1 }
            inputs  = [ { scope = "local", resource = "sundries", amount = 4 } ]
            outputs = []
            """,
            """
            rate    = 32
            apply   = { min = 1, max = 1 }
            basket  = "basic"
            """,
            StringComparison.Ordinal);

        return members;
    }

    private static RulesetImpact Between(
        Dictionary<string, string> before, Dictionary<string, string> after)
    {
        RulesetSourceResult was = Resolve(before);
        RulesetSourceResult now = Resolve(after);

        Assert.True(was.Ok, was.Describe());
        Assert.True(now.Ok, now.Describe());

        return RulesetImpact.Between(was, now);
    }

    private static string Value(RulesetImpact impact, string section, string id, string path) =>
        Assert.Single(
            Assert.Single(impact.Declarations, d => d.Section == section && d.Id == id).Values,
            v => v.Path == path).After!;

    [Fact]
    public void Adding_a_good_and_a_recipe_gives_the_rule_the_recipes_terms()
    {
        Dictionary<string, string> after = WithMember(Package(), "shared.toml", SharedRecipe);

        after["goods.toml"] += "\n[[resource]]\nid = \"timber\"\nfamily = \"good\"\n";
        after["rules.toml"] += """

            [[rule]]
            id     = "mend"
            order  = 4
            kind   = "dwelling"
            rate   = 64
            apply  = { min = 1, max = 2 }
            recipe = "mend"
            """;

        RulesetImpact impact = Between(Package(), after);

        Assert.Equal("timber", Value(impact, "rule", "mend", "inputs[0].Bin.Resource.Raw"));
        Assert.Equal("1", Value(impact, "rule", "mend", "inputs[0].Amount"));
        Assert.Equal("repairs", Value(impact, "rule", "mend", "outputs[0].Bin.Resource.Raw"));
        Assert.Equal("4", Value(impact, "rule", "mend", "outputs[0].Amount"));

        // A recipe resolves into the Rules that run it and holds no runtime value of its own.
        Assert.Empty(
            Assert.Single(impact.Declarations, d => d.Section == "recipe" && d.Id == "mend").Values);
    }

    [Fact]
    public void A_basket_and_a_reserve_derive_the_capacity_the_guide_quotes()
    {
        RulesetImpact impact = Between(Package(), Basketed());

        // use_per_day 128 over a 3 Day reserve.
        Assert.Equal("384", Value(impact, "building", "dwelling", "bins[0].Capacity.Units"));

        Assert.Equal(
            ["128", "True"],
            Assert.Single(impact.Declarations, d => d.Section == "rule" && d.Id == "consume")
                .Values.Select(v => v.After));
    }

    [Fact]
    public void A_local_days_exception_leaves_the_rule_alone()
    {
        Dictionary<string, string> after = Basketed();

        after["dwelling.toml"] = after["dwelling.toml"].Replace(
            "basket = \"basic\" }", "basket = \"basic\", days = 5 }", StringComparison.Ordinal);

        RulesetImpact impact = Between(Basketed(), after);

        Assert.Equal("640", Value(impact, "building", "dwelling", "bins[0].Capacity.Units"));

        Assert.Equal(
            RulesetImpactVerdict.Unchanged,
            Assert.Single(impact.Declarations, d => d.Section == "rule" && d.Id == "consume").Verdict);
    }

    /// <summary>
    /// An exception preserves Days rather than freezing a capacity.
    /// </summary>
    /// <remarks>
    /// The guide's warning turns on this: an excepted Bin still follows the shared basket, so
    /// 128 x 5 becoming 160 x 5 is the behaviour and 640 staying put would be the defect.
    /// </remarks>
    [Fact]
    public void Moving_the_shared_basket_moves_an_excepted_bin_too()
    {
        Dictionary<string, string> excepted = Basketed();

        excepted["dwelling.toml"] = excepted["dwelling.toml"].Replace(
            "basket = \"basic\" }", "basket = \"basic\", days = 5 }", StringComparison.Ordinal);

        Dictionary<string, string> after = new(excepted, StringComparer.Ordinal)
        {
            ["shared.toml"] = SharedBasket.Replace(
                "sundries = 128", "sundries = 160", StringComparison.Ordinal),
        };

        RulesetImpact impact = Between(excepted, after);

        Assert.Equal("800", Value(impact, "building", "dwelling", "bins[0].Capacity.Units"));
        Assert.Equal("160", Value(impact, "rule", "consume", "inputs[0].Amount"));
    }

    [Fact]
    public void A_basket_rule_with_an_apply_band_is_refused()
    {
        Dictionary<string, string> members = Basketed();

        members["rules.toml"] = members["rules.toml"].Replace(
            "apply   = { min = 1, max = 1 }\nbasket  = \"basic\"",
            "apply   = { min = 1, max = 4 }\nbasket  = \"basic\"",
            StringComparison.Ordinal);

        RulesetSourceResult result = Resolve(members);

        Assert.False(result.Ok);
        Assert.Contains(
            "apply count that is not fixed at one", result.Describe(), StringComparison.Ordinal);
    }
}
