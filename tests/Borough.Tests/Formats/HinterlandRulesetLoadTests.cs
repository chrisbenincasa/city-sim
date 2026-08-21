using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 11 task 2: <c>[[hinterland]]</c>, the economy behind one map edge.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="OutsideConnectionRulesetLoadTests"/>' discipline.
/// </para>
/// <para>
/// <b>Two fields is the whole object at this milestone, and that is the thing under test.</b>
/// <c>adr/0131</c>: <i>a Hinterland field is authored in the milestone that reads it</i> — so a
/// price per Good, a wage, a rent and a depth are all absent, and a table stating only its edge is
/// refused rather than accepted as a partial one. ***An object whose only content-bearing field is
/// optional can be declared and say nothing.***
/// </para>
/// <para>
/// ⚠ <b><see cref="A_hinterland_whose_emigrants_carry_nothing_is_accepted"/> is the pair to
/// <c>OutsideConnectionRulesetLoadTests.A_gate_that_admits_nobody_is_refused</c></b>, and the two
/// zeroes are deliberately different. A gate admitting nobody disables itself; a Hinterland sending
/// paupers is an economy.
/// </para>
/// </remarks>
public sealed class HinterlandRulesetLoadTests
{
    /// <summary>The smallest Ruleset that can hold a balance: it names a money Resource.</summary>
    private const string WithMoney = """
        [[resource]]
        name = "pound"
        family = "money"

        [[building]]
        name = "dwelling"
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

    /// <summary>A Hinterland is an edge and a band, and both survive the load.</summary>
    [Fact]
    public void An_edge_and_a_band_survive_the_load()
    {
        Ruleset ruleset = Accepted(WithMoney + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 200
            emigrant_balance_max = 900
            """);

        Assert.True(ruleset.TryHinterland(MapEdge.North, out HinterlandDefinition north));
        Assert.Equal(new Money(200), north.EmigrantBalanceMin);
        Assert.Equal(new Money(900), north.EmigrantBalanceMax);
        Assert.True(north.Endows);
    }

    /// <summary>An edge with no <c>[[hinterland]]</c> behind it has no market, and says so.</summary>
    /// <remarks>
    /// <b>Four edges are four independent markets</b> (<c>CONTEXT.md</c> → Hinterland), so authoring
    /// one is not authoring the Outside. What a gate on an unauthored edge costs is paid at arrival,
    /// which is task 4's.
    /// </remarks>
    [Fact]
    public void An_edge_with_no_table_behind_it_has_no_market()
    {
        Ruleset ruleset = Accepted(WithMoney + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            """);

        Assert.False(ruleset.TryHinterland(MapEdge.South, out _));
        Assert.False(ruleset.TryHinterland(MapEdge.East, out _));
        Assert.False(ruleset.TryHinterland(MapEdge.West, out _));
    }

    /// <summary><see cref="MapEdge.None"/> is never a market, whatever is declared.</summary>
    /// <remarks>
    /// <b>It is the answer <see cref="MapEdges.Touching"/> gives for a corner as well as for the
    /// middle of the city</b>, so a lookup on it must not find anything — a corner gate draws from
    /// two Hinterlands and the design has no rule saying which.
    /// </remarks>
    [Fact]
    public void Nowhere_is_never_a_market()
    {
        Ruleset ruleset = Accepted(WithMoney + """

            [[hinterland]]
            edge = "west"
            emigrant_balance_min = 5
            emigrant_balance_max = 5
            """);

        Assert.False(ruleset.TryHinterland(MapEdge.None, out _));
    }

    /// <summary>All four edges may be authored, and they are separate economies.</summary>
    [Fact]
    public void Four_edges_are_four_markets()
    {
        Ruleset ruleset = Accepted(WithMoney + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 100
            emigrant_balance_max = 100

            [[hinterland]]
            edge = "south"
            emigrant_balance_min = 200
            emigrant_balance_max = 200

            [[hinterland]]
            edge = "east"
            emigrant_balance_min = 300
            emigrant_balance_max = 300

            [[hinterland]]
            edge = "west"
            emigrant_balance_min = 400
            emigrant_balance_max = 400
            """);

        Assert.Equal(4, ruleset.Hinterlands.Length);

        Assert.True(ruleset.TryHinterland(MapEdge.North, out HinterlandDefinition north));
        Assert.True(ruleset.TryHinterland(MapEdge.West, out HinterlandDefinition west));
        Assert.Equal(new Money(100), north.EmigrantBalanceMin);
        Assert.Equal(new Money(400), west.EmigrantBalanceMin);
    }

    /// <summary>A Hinterland sending paupers is an economy, not an unset field.</summary>
    [Fact]
    public void A_hinterland_whose_emigrants_carry_nothing_is_accepted()
    {
        Ruleset ruleset = Accepted("""
            [[building]]
            name = "dwelling"

            [[hinterland]]
            edge = "east"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            """);

        Assert.True(ruleset.TryHinterland(MapEdge.East, out HinterlandDefinition east));
        Assert.False(east.Endows);
    }

    /// <summary>A Ruleset with no <c>[[hinterland]]</c> has no Outside authored.</summary>
    [Fact]
    public void A_file_that_declares_none_has_no_Outside()
    {
        Ruleset ruleset = Accepted(WithMoney);

        Assert.Empty(ruleset.Hinterlands);
        Assert.False(ruleset.TryHinterland(MapEdge.North, out _));
    }

    [Fact]
    public void A_hinterland_behind_no_edge_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            edge = "northwest"
            emigrant_balance_min = 1
            emigrant_balance_max = 2
            """);

        Assert.Contains("is not a map edge", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_hinterland_with_no_edge_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            emigrant_balance_min = 1
            emigrant_balance_max = 2
            """);

        Assert.Contains("no edge", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Two tables for one edge is ambiguous rather than additive.</summary>
    [Fact]
    public void A_second_hinterland_on_one_edge_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            edge = "south"
            emigrant_balance_min = 1
            emigrant_balance_max = 2

            [[hinterland]]
            edge = "south"
            emigrant_balance_min = 3
            emigrant_balance_max = 4
            """);

        Assert.Contains("a second hinterland", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("south", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>An edge alone declares an economy and says nothing about it.</summary>
    [Fact]
    public void A_hinterland_that_states_only_its_edge_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            edge = "north"
            """);

        Assert.Contains("emigrant_balance_min", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Half a band has two readings and they are different economies.</summary>
    [Fact]
    public void Half_a_band_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 200
            """);

        Assert.Contains("emigrant_balance_max", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_floor_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = -1
            emigrant_balance_max = 10
            """);

        Assert.Contains("a stock is never negative", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void An_inverted_band_is_refused()
    {
        RulesetRefusal refusal = Refused(WithMoney + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 900
            emigrant_balance_max = 200
            """);

        Assert.Contains("inverted one is empty", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Emigrants carrying money into a city that names none would have nowhere to put it.
    /// </summary>
    /// <remarks>
    /// <b><c>[households]</c>'s balance refusal, at the other door.</b> A balance is a Bin and a Bin
    /// exists only for a declared Resource (<c>adr/0114</c>), so the loader can see both halves and
    /// refuses with a file and a line rather than throwing at the first arrival.
    /// </remarks>
    [Fact]
    public void Emigrants_carrying_money_into_a_moneyless_city_are_refused()
    {
        RulesetRefusal refusal = Refused("""
            [[building]]
            name = "dwelling"

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 1
            emigrant_balance_max = 2
            """);

        Assert.Contains("names none", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The section is spelled out where an unknown one is refused.</summary>
    [Fact]
    public void The_section_list_names_it()
    {
        RulesetRefusal refusal = Refused("""
            [hinterlands]
            edge = "north"
            """);

        Assert.Contains("[[hinterland]]", refusal.Reason, StringComparison.Ordinal);
    }
}
