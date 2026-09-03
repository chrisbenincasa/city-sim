using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// What a <c>[[band]]</c> table means, and what the loader refuses.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 2</b> — <c>adr/0025</c>'s density band arriving as a real value. That ADR
/// decided bands exist and that <em>"Lot subdivision must vary by band"</em>, and nothing in
/// <c>Borough.Core</c> had carried one.
/// </para>
/// <para>
/// <b>Every refusal here is one of <c>adr/0048</c>'s shape</b>: a key that would load clean and do
/// nothing gets a line number instead. A band admitting no zone bit, a bit outside the permission
/// set's width, and a bit admitted twice are each cases where the file says something and the world
/// would show nothing.
/// </para>
/// </remarks>
public sealed class BandRulesetLoadTests
{
    /// <summary>The smallest file that parses, with a hole where the bands go.</summary>
    private const string Skeleton = """
        [[resource]]
        name   = "sundries"
        family = "good"

        [[building]]
        name      = "dwelling"
        bins      = [ { resource = "sundries", capacity = 8 } ]
        houses = true
        premises = true
        [[zone_rule]]
        name          = "housing"
        kind          = "dwelling"
        zone          = 0
        interval      = 32
        revisit_ticks = 2048
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

    /// <summary><b>A file with no <c>[[band]]</c> is a complete Ruleset</b>, which is what keeps this
    /// step from being an edit to every shipped file.</summary>
    [Fact]
    public void A_ruleset_with_no_band_declares_none_and_still_loads()
    {
        Ruleset ruleset = Accepted(Skeleton);

        Assert.False(ruleset.HasBands);
        Assert.Empty(ruleset.Bands);
    }

    /// <summary>
    /// 🔴 <b>An absent band admits EVERYTHING</b>, because the mechanism is subtractive.
    /// </summary>
    /// <remarks>
    /// <b>The permissive answer is the correct one here and it is the opposite of how most absences in
    /// this Ruleset read.</b> A band is a <em>cap</em>, applied by intersection — so the identity for a
    /// world nobody banded has to be all-bits-set. The restrictive reading would give every Lot in
    /// every bandless world a permission set of zero and build no city at all.
    /// </remarks>
    [Fact]
    public void An_absent_band_admits_everything()
    {
        Ruleset ruleset = Accepted(Skeleton);

        Assert.Equal(ushort.MaxValue, ruleset.Band(0).Admits);
        Assert.Equal(ushort.MaxValue, ruleset.Band(7).Admits);
    }

    /// <summary><b>Bands are one-based and in declaration order</b>, lowest intensity first.</summary>
    [Fact]
    public void Bands_are_indexed_from_one_in_declaration_order()
    {
        Ruleset ruleset = Accepted(Skeleton + """

            [[band]]
            name   = "suburban"
            admits = [0]

            [[band]]
            name   = "central"
            admits = [0, 1]
            """);

        Assert.True(ruleset.HasBands);
        Assert.Equal(2, ruleset.Bands.Length);

        Assert.Equal(0b01, ruleset.Band(1).Admits);
        Assert.Equal(0b11, ruleset.Band(2).Admits);

        // Past the end is the same permissive answer as zero, and for the same reason: a cap nobody
        // stated cannot be allowed to subtract.
        Assert.Equal(ushort.MaxValue, ruleset.Band(3).Admits);
    }

    /// <summary><b>A band admitting nothing is refused</b>, not read as <em>nothing may build</em>.</summary>
    [Fact]
    public void A_band_admitting_nothing_is_refused()
    {
        RulesetRefusal refusal = Refused(Skeleton + """

            [[band]]
            name   = "void"
            admits = []
            """);

        Assert.Contains("admits no zone bit", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary><b>A band with no <c>admits</c> key at all is refused</b>, for the same reason.</summary>
    [Fact]
    public void A_band_with_no_admits_key_is_refused()
    {
        RulesetRefusal refusal = Refused(Skeleton + """

            [[band]]
            name = "nameless"
            """);

        Assert.Contains("states no admits", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A bit outside the permission set's width is refused</b>, matching <c>[[zone_rule]]</c>'s
    /// own refusal on the same integer.
    /// </summary>
    [Fact]
    public void A_zone_bit_outside_the_permission_set_is_refused()
    {
        RulesetRefusal refusal = Refused(Skeleton + $"""

            [[band]]
            name   = "wide"
            admits = [{LotTable.ZoneBits}]
            """);

        Assert.Contains($"outside 0..{LotTable.ZoneBits - 1}", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary><b>A bit admitted twice by one band is refused</b> — the second one changes nothing.</summary>
    [Fact]
    public void A_zone_bit_admitted_twice_is_refused()
    {
        RulesetRefusal refusal = Refused(Skeleton + """

            [[band]]
            name   = "doubled"
            admits = [1, 1]
            """);

        Assert.Contains("admitted twice", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary><b>The shipped demonstration file declares two bands</b>, and it is the only one that does.</summary>
    [Fact]
    public void The_shipped_banded_ruleset_declares_two_bands()
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "banded.toml"));

        Assert.True(result.Ok, result.Describe());

        Ruleset ruleset = result.Ruleset!;

        Assert.Equal(2, ruleset.Bands.Length);
        Assert.Equal(LotTable.Housing, ruleset.Band(1).Admits);
        Assert.Equal(LotTable.Housing | LotTable.Trade, ruleset.Band(2).Admits);
    }
}
