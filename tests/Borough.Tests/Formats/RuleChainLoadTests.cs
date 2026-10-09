using Borough.Formats;
using Xunit;

namespace Borough.Tests.Formats;

public sealed class RuleChainLoadTests
{
    private const string Works = """
        [[resource]]
        name = "a"
        family = "good"

        [[resource]]
        name = "b"
        family = "good"

        [[resource]]
        name = "c"
        family = "good"

        [[resource]]
        name = "d"
        family = "good"

        [[resource]]
        name = "money"
        family = "money"

        [[building]]
        name = "works"
        bins = [
          { resource = "a", capacity = 60 },
          { resource = "b", capacity = 60 },
          { resource = "c", capacity = 60 },
          { resource = "d", capacity = 60 },
          { resource = "money" },
        ]

        """;

    private static string Rule(string name, string from, string to) => $$"""

        [[rule]]
        name    = "{{name}}"
        kind    = "works"
        rate    = 10
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "{{from}}", amount = 1 } ]
        outputs = [ { scope = "local", resource = "{{to}}", amount = 1 } ]

        """;

    private static string PaidRule(string name, string from, string to) => $$"""

        [[rule]]
        name    = "{{name}}"
        kind    = "works"
        rate    = 10
        apply   = { min = 1, max = 1 }
        inputs  = [
            { scope = "local", resource = "{{from}}", amount = 1 },
            { scope = "local", resource = "money", amount = 5 },
        ]
        outputs = [
            { scope = "local",  resource = "{{to}}", amount = 1 },
            { scope = "global", resource = "money", amount = 5 },
        ]

        """;

    private static string Replaced(string text, string old, string replacement)
    {
        Assert.Contains(old, text, StringComparison.Ordinal);
        return text.Replace(old, replacement, StringComparison.Ordinal);
    }

    private static RulesetLoadResult Load(string toml) => RulesetLoader.Parse(toml, "test.toml");

    [Fact]
    public void A_chain_three_Resources_deep_is_accepted()
    {
        RulesetLoadResult result = Load(Works + Rule("make_b", "a", "b") + Rule("make_c", "b", "c"));

        Assert.True(result.Ok, result.Describe());
    }

    [Fact]
    public void A_chain_four_Resources_deep_is_refused_at_its_head()
    {
        RulesetLoadResult result = Load(
            Works + Rule("make_c", "b", "c") + Rule("make_d", "c", "d") + Rule("make_b", "a", "b"));

        RulesetRefusal refusal = Assert.Single(result.Refusals);
        Assert.Equal("make_b", refusal.Rule);
        Assert.Contains("a -> b -> c -> d, which is 4 Resources deep", refusal.Reason);
    }

    [Fact]
    public void A_cycle_through_Bins_is_refused()
    {
        RulesetLoadResult result = Load(Works + Rule("make_b", "a", "b") + Rule("make_a", "b", "a"));

        RulesetRefusal refusal = Assert.Single(result.Refusals);
        Assert.Equal("make_a", refusal.Rule);
        Assert.Contains("a -> b -> a", refusal.Reason);
    }

    [Fact]
    public void A_Rule_that_moves_one_Resource_adds_no_link()
    {
        RulesetLoadResult result = Load(
            Works + Rule("move_a", "a", "a") + Rule("make_b", "a", "b") + Rule("make_c", "b", "c"));

        Assert.True(result.Ok, result.Describe());
    }

    [Fact]
    public void Money_does_not_link_a_paid_chain()
    {
        RulesetLoadResult result = Load(Works + PaidRule("make_b", "a", "b") + PaidRule("make_c", "b", "c"));

        Assert.True(result.Ok, result.Describe());
    }

    [Fact]
    public void Labour_does_not_count_toward_depth()
    {
        string milled = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "milled.toml"));
        string packed = Replaced(
                Replaced(
                    Replaced(
                        milled,
                        "    { resource = \"sundries\", capacity = 1024, owner = \"business\" },\n",
                        "    { resource = \"sundries\", capacity = 1024, owner = \"business\" },\n"
                        + "    { resource = \"hampers\",  capacity = 64, owner = \"business\" },\n"),
                    "[[resource]]\nname = \"labour\"\n",
                    "[[resource]]\nname = \"hampers\"\nfamily = \"good\"\n\n[[resource]]\nname = \"labour\"\n"),
                "  { resource = \"flour\",    price = 1400 },\n",
                "  { resource = \"flour\",    price = 1400 },\n  { resource = \"hampers\",  price = 500 },\n")
            + """

            [[rule]]
            name    = "pack"
            kind    = "shopfront"
            rate    = 64
            apply   = { min = 1, max = 1 }
            inputs  = [
                { scope = "local", resource = "labour",   amount = 1 },
                { scope = "local", resource = "sundries", amount = 4 },
            ]
            outputs = [ { scope = "local", resource = "hampers", amount = 1 } ]

            """;

        RulesetLoadResult result = Load(packed);

        Assert.True(result.Ok, result.Describe());
    }
}
