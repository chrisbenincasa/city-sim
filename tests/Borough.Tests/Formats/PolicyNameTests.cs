using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// <c>plans/0045</c> queue item 15c: a <c>[[policy]]</c>'s readable name, for a panel to put beside
/// a dial.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>Ruleset.PolicyKeys</c> holds a HASH and a person cannot read one.</b> <c>05 §1</c> has the
/// shell resolving every human-readable string through the Ruleset, and until now
/// <c>RulesetNames</c> had no Policy accessor — so a governing panel could only have offered
/// <em>policy 0, policy 1, policy 2</em>, which is a list of positions rather than of decisions.
/// </para>
/// <para>
/// ⚠ <b>A list and not a map, which is the one name here that is not inverted from an id table.</b>
/// A Policy has no id: <c>Govern</c> addresses it by <b>declaration position</b>, so the position is
/// the index and inverting anything would lose it.
/// </para>
/// </remarks>
public sealed class PolicyNameTests
{
    private const string Governed = """
        [[resource]]
        name = "money"
        family = "money"

        [[policy]]
        name = "household_levy"
        sweeps = "household"
        interval = 2048
        apply = { min = 1, max = 1 }
        transfer = { from = "local", to = "global", resource = "money", amount = 3 }

        [[policy]]
        sweeps = "household"
        interval = 2048
        apply = { min = 1, max = 1 }
        transfer = { from = "local", to = "global", resource = "money", amount = 5 }

        [[policy]]
        name = "trade_levy"
        sweeps = "household"
        interval = 2048
        apply = { min = 1, max = 1 }
        transfer = { from = "local", to = "global", resource = "money", amount = 7 }
        """;

    private static RulesetLoadResult Loaded()
    {
        RulesetLoadResult result = RulesetLoader.Parse(Governed, "governed.toml");

        Assert.True(result.Ok, result.Describe());

        return result;
    }

    /// <summary>The name reaches the panel, at the position <c>Govern</c> addresses.</summary>
    [Fact]
    public void A_policy_is_named_by_its_declaration_position()
    {
        RulesetLoadResult loaded = Loaded();

        Assert.Equal("household_levy", loaded.Names.Policy(0));
        Assert.Equal("trade_levy", loaded.Names.Policy(2));
    }

    /// <summary>
    /// 🔴 <b>An unnamed table still occupies its position, and this is the assertion that says so.</b>
    /// </summary>
    /// <remarks>
    /// <b>The failure this refuses is a panel that omits the row.</b> An unnamed <c>[[policy]]</c>
    /// keys to zero and <c>Simulation.ApplyGovern</c> refuses it — but it is still declaration
    /// position 1, so a list that skipped it would put <c>trade_levy</c> at index 1 and every
    /// <c>Govern</c> issued from that panel would name the wrong Policy. ***A gap in the middle has
    /// to be a gap and not a shortening.***
    /// </remarks>
    [Fact]
    public void An_unnamed_policy_holds_its_position_and_reads_as_null()
    {
        RulesetLoadResult loaded = Loaded();

        Assert.Null(loaded.Names.Policy(1));
        Assert.Equal(0UL, loaded.Ruleset!.PolicyKey(1));

        // And the position is genuinely held: the third table is still third on both sides.
        Assert.Equal("trade_levy", loaded.Names.Policy(2));
        Assert.NotEqual(0UL, loaded.Ruleset.PolicyKey(2));
    }

    /// <summary>Off the end is null rather than a throw, which is what a panel wants.</summary>
    [Fact]
    public void A_position_no_table_occupies_reads_as_null()
    {
        Assert.Null(Loaded().Names.Policy(9));
        Assert.Null(RulesetNames.None.Policy(0));
    }
}
