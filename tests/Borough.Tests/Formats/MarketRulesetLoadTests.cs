using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 12 task 6: <c>[[hinterland]] prices</c> and the <c>[market]</c> table.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="HinterlandRulesetLoadTests"/>' discipline — which is the same rule
/// <c>adr/0048</c> states and <c>RefusalCountTests</c> counts.
/// </para>
/// <para>
/// <b>The one refusal here that is not a range check is the one worth reading.</b> A file that states
/// <c>[districts]</c> and leaves a <c>good</c> unpriced at every Hinterland is refused outright: a
/// District opens a Pool per Good and the Hinterland's price is the <em>only</em> ceiling on it
/// (<c>adr/0050</c>, <c>adr/0135</c>), so an unpriced Good is not unanchored — it is
/// ***free everywhere, for ever***, which reads as a balance problem rather than as a missing key.
/// </para>
/// <para>
/// ⚠ <b>It is gated on <c>[districts]</c> and NOT on a gate kind, and the asymmetry is the point.</b>
/// Whether a city has Districts is stated in the file; which edge a gate stands on is a property of
/// where it was <em>placed</em>, which the loader cannot see. The first is reachable at parse time and
/// the second never will be.
/// </para>
/// </remarks>
public sealed class MarketRulesetLoadTests
{
    /// <summary>Two Goods and a money Resource, which is what a priced file needs.</summary>
    private const string Goods = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "good"

        [[resource]]
        name = "pound"
        family = "money"

        [[building]]
        name = "dwelling"
        """;

    /// <summary>A <c>[districts]</c> table the loader accepts, so the Pool refusal can be reached.</summary>
    private const string Districts = """

        [districts]
        prominence_percent = 50
        revisit_ticks = 2048
        hysteresis_percent = 50
        migrate_cells = 16
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

    // ---- the prices ------------------------------------------------------------------------------

    /// <summary>A price per Good per Hinterland survives the load, keyed by both.</summary>
    [Fact]
    public void A_price_per_good_per_hinterland_survives_the_load()
    {
        Ruleset ruleset = Accepted(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 100 }, { resource = "repairs", price = 250 } ]

            [[hinterland]]
            edge = "east"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 120 }, { resource = "repairs", price = 200 } ]
            """);

        Assert.Equal(new Money(100), ruleset.ImportPrice(0, new ResourceId(1)));
        Assert.Equal(new Money(250), ruleset.ImportPrice(0, new ResourceId(2)));
        Assert.Equal(new Money(120), ruleset.ImportPrice(1, new ResourceId(1)));
        Assert.Equal(new Money(200), ruleset.ImportPrice(1, new ResourceId(2)));
    }

    /// <summary>
    /// The ceiling is the lowest price across the Hinterlands, and the two Goods take theirs from
    /// different edges.
    /// </summary>
    /// <remarks>
    /// <b>The second half is what makes this a test rather than a tautology.</b> A <c>min</c> that
    /// silently returned the first table would be right about <c>sundries</c> and wrong about
    /// <c>repairs</c>, so the fixture prices them in opposite orders across the two edges.
    /// ***A minimum over a list is only demonstrated by a list whose minimum is not its head.***
    /// </remarks>
    [Fact]
    public void The_ceiling_is_the_lowest_price_and_the_two_goods_take_it_from_different_edges()
    {
        Ruleset ruleset = Accepted(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 100 }, { resource = "repairs", price = 250 } ]

            [[hinterland]]
            edge = "east"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 120 }, { resource = "repairs", price = 200 } ]
            """);

        Assert.Equal(new Money(100), ruleset.ImportCeiling(new ResourceId(1)));
        Assert.Equal(new Money(200), ruleset.ImportCeiling(new ResourceId(2)));
    }

    /// <summary>A Hinterland with no <c>prices</c> at all loads, in a file with no Districts.</summary>
    /// <remarks>
    /// <b><c>rulesets/bordered.toml</c> and <c>crowded.toml</c> are exactly this</b>, and neither has
    /// a Pool to price. The key is optional wherever nothing reads it, which is <c>adr/0131</c>'s rule
    /// arriving on the reading side.
    /// </remarks>
    [Fact]
    public void A_hinterland_with_no_prices_loads_where_nothing_pools()
    {
        Ruleset ruleset = Accepted(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            """);

        Assert.Equal(Money.Zero, ruleset.ImportCeiling(new ResourceId(1)));
    }

    /// <summary>A price that is not a positive amount is refused.</summary>
    [Fact]
    public void A_price_of_zero_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 0 } ]
            """);

        Assert.Contains("sundries", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>Only a Good has an import price.</summary>
    [Fact]
    public void Pricing_a_resource_that_is_not_a_good_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "pound", price = 1 } ]
            """);

        Assert.Contains("pound", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>One market, one price per Good.</summary>
    [Fact]
    public void Pricing_one_good_twice_at_one_hinterland_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 100 }, { resource = "sundries", price = 90 } ]
            """);

        Assert.Contains("twice", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary><c>prices</c> must be an array of inline tables.</summary>
    [Fact]
    public void Prices_that_are_not_an_array_are_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = 100
            """);

        Assert.Contains("prices", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>An entry of <c>prices</c> must be an inline table.</summary>
    [Fact]
    public void A_prices_entry_that_is_not_an_inline_table_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ 100 ]
            """);

        Assert.Contains("inline table", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the anchor a Pool needs -----------------------------------------------------------------

    /// <summary>A file with Districts in it and a Good nothing prices is refused.</summary>
    [Fact]
    public void A_good_no_hinterland_prices_is_refused_where_the_file_states_districts()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [[hinterland]]
            edge = "north"
            emigrant_balance_min = 0
            emigrant_balance_max = 0
            prices = [ { resource = "sundries", price = 100 } ]
            """ + Districts);

        Assert.Contains("repairs", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A file with Districts and no Hinterland at all is refused, on the same ground.</summary>
    /// <remarks>
    /// <b>This is the case <c>rulesets/twinned.toml</c> was in when task 6 started</b> — the only
    /// world with Districts in it, and no ceiling under any of its Pools.
    /// </remarks>
    [Fact]
    public void A_file_with_districts_and_no_hinterland_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + Districts);

        Assert.Contains("sundries", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The same file without <c>[districts]</c> loads, because it pools nothing.</summary>
    /// <remarks>
    /// ⚠ <b>The pair with the test above is the whole shape of the refusal.</b> An unpriced Good is
    /// not a defect on its own — it is a defect in a city that opens a Pool for it, and eight of the
    /// eleven shipped Rulesets are the accepted half.
    /// </remarks>
    [Fact]
    public void The_same_file_without_districts_loads()
    {
        Ruleset ruleset = Accepted(Goods);

        Assert.Empty(ruleset.Hinterlands);
        Assert.Equal(Money.Zero, ruleset.ImportCeiling(new ResourceId(1)));
    }

    // ---- the damping -----------------------------------------------------------------------------

    /// <summary>Both <c>[market]</c> keys survive the load.</summary>
    [Fact]
    public void The_market_tables_two_keys_survive_the_load()
    {
        Ruleset ruleset = Accepted(Goods + """

            [market]
            decay_percent = 50
            move_cap_percent = 10
            """);

        Assert.True(ruleset.Market.Runs);
        Assert.Equal(50, ruleset.Market.DecayPercent);
        Assert.Equal(10, ruleset.Market.MoveCapPercent);
    }

    /// <summary>Omitting <c>[market]</c> is a city whose prices never move.</summary>
    [Fact]
    public void No_market_table_is_a_price_that_never_moves()
    {
        Ruleset ruleset = Accepted(Goods);

        Assert.False(ruleset.Market.Runs);
    }

    /// <summary>Zero damping is accepted, because it means no smoothing.</summary>
    /// <remarks>
    /// <b>The pair to <see cref="A_decay_of_a_hundred_is_refused"/>, and the asymmetry is the
    /// decision.</b> A rate equal to the Day's own draw is a twitchy market and a real one; a rate
    /// that never takes on a new Day is a price frozen at its seed while the file appears to be
    /// damping it. ***Only the value that duplicates the omitted table may spell it.***
    /// </remarks>
    [Fact]
    public void A_decay_of_zero_is_accepted()
    {
        Ruleset ruleset = Accepted(Goods + """

            [market]
            decay_percent = 0
            move_cap_percent = 10
            """);

        Assert.True(ruleset.Market.Runs);
        Assert.Equal(0, ruleset.Market.DecayPercent);
    }

    /// <summary>A decay of 100 is a rate that never moves, and is refused.</summary>
    [Fact]
    public void A_decay_of_a_hundred_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [market]
            decay_percent = 100
            move_cap_percent = 10
            """);

        Assert.Contains("decay_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A cap of zero is the omitted table, and is refused rather than accepted as one.</summary>
    [Fact]
    public void A_move_cap_of_zero_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [market]
            decay_percent = 50
            move_cap_percent = 0
            """);

        Assert.Contains("move_cap_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A cap above the whole range can never bind, and is refused.</summary>
    [Fact]
    public void A_move_cap_above_a_hundred_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [market]
            decay_percent = 50
            move_cap_percent = 101
            """);

        Assert.Contains("move_cap_percent", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A stated <c>[market]</c> states both its keys.</summary>
    [Theory]
    [InlineData("decay_percent = 50", "move_cap_percent")]
    [InlineData("move_cap_percent = 10", "decay_percent")]
    public void A_stated_market_table_states_both_its_keys(string only, string missing)
    {
        RulesetRefusal refusal = Refused($"{Goods}\n\n[market]\n{only}\n");

        Assert.Contains(missing, refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A second <c>[market]</c> is refused rather than letting the later one win.</summary>
    [Fact]
    public void A_second_market_table_is_refused()
    {
        RulesetRefusal refusal = Refused(Goods + """

            [market]
            decay_percent = 50
            move_cap_percent = 10

            [market]
            decay_percent = 60
            move_cap_percent = 20
            """);

        Assert.Contains("[market]", refusal.Reason, StringComparison.Ordinal);
    }
}
