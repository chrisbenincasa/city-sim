using System.Globalization;

using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Slice 7 task 4: a quoted decimal becomes a <see cref="Ratio"/>, exactly, and no locale can move it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The culture tests here are a measurement, not a reassurance.</b> <c>adr/0043</c> types the
/// claim <em>"this parse is culture-insensitive"</em> as measurable — the refuting number is a value
/// that differs under a different locale, and the machine that produces it is a unit test — so it is
/// written rather than asserted in a comment.
/// </para>
/// <para>
/// <b>The hostile culture is built by hand, and that is the interesting part of this file.</b>
/// <c>Directory.Build.props</c> sets <c>InvariantGlobalization</c>, which applies to this test
/// project as much as to the core, and under it <c>new CultureInfo("de-DE")</c> — the obvious
/// spelling — <b>throws <c>CultureNotFoundException</c></b>; relaxing
/// <c>PredefinedCulturesOnly</c> would only downgrade that to a culture carrying invariant data, so
/// the obvious spelling either fails or measures nothing. Setting the separators directly on a cloned
/// <see cref="NumberFormatInfo"/> needs no ICU and cannot be neutralised by a build property.
/// </para>
/// <para>
/// <b>That the rig is live was itself measured, and it bites harder than <c>de-DE</c> would have.</b>
/// Under this culture, and with <c>InvariantGlobalization</c> on: <c>double.Parse("1,5")</c> returns
/// <b>1.5</b>, <c>double.Parse("1.5")</c> returns <b>15</b> — the point read as a grouping separator,
/// so a designer's number is silently multiplied by ten — and <c>double.TryParse("-1")</c> returns
/// <b>false</b>, because the negative sign has moved. A naive routine fails all three, which is what
/// makes the assertions below evidence rather than decoration.
/// </para>
/// </remarks>
public sealed class QuotedDecimalTests
{
    /// <summary>
    /// A locale in which every character this routine reads means something else: the decimal point
    /// groups, the comma separates the fraction, and the minus sign is not a minus sign.
    /// </summary>
    private static CultureInfo Hostile()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        culture.NumberFormat.NegativeSign = "!";
        return culture;
    }

    private static T UnderHostileCulture<T>(Func<T> work)
    {
        CultureInfo restore = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = Hostile();

        try
        {
            return work();
        }
        finally
        {
            CultureInfo.CurrentCulture = restore;
        }
    }

    private static Ratio Parse(string text)
    {
        Assert.True(QuotedDecimal.TryParse(text, out Ratio value, out string? reason),
            $"'{text}' was refused: {reason}");
        return value;
    }

    private static string Refuse(string text)
    {
        Assert.False(QuotedDecimal.TryParse(text, out Ratio value, out string? reason),
            $"'{text}' was accepted as {value.Raw}.");
        Assert.NotNull(reason);
        return reason!;
    }

    // ---- what the number is ---------------------------------------------------------------------

    [Fact]
    public void A_whole_number_is_the_fixed_point_representation_of_itself()
    {
        Assert.Equal(Ratio.Zero, Parse("0"));
        Assert.Equal(Ratio.One, Parse("1"));
        Assert.Equal(new Ratio(Fixed.FromInt(32767)), Parse("32767"));
        Assert.Equal(new Ratio(-Fixed.One), Parse("-1"));
    }

    /// <summary>
    /// The rounding rule, stated as an equality rather than as prose: a quoted decimal and the
    /// fraction it spells are the same number.
    /// </summary>
    /// <remarks>
    /// <c>0.15</c> has no exact Q16.16 representation, so something must decide the last bit. Both
    /// sides of this assertion floor, because <see cref="Fixed"/> floors throughout — and the value
    /// of writing it down is that a later change to either routine's rounding fails here rather than
    /// showing up as a balance drift nobody can attribute.
    /// </remarks>
    [Fact]
    public void A_quoted_decimal_is_the_fraction_it_spells()
    {
        Assert.Equal(Ratio.FromFraction(15, 100), Parse("0.15"));
        Assert.Equal(Ratio.FromFraction(1, 2), Parse("0.5"));
        Assert.Equal(Ratio.FromFraction(3, 4), Parse("0.75"));
        Assert.Equal(Ratio.FromFraction(-15, 100), Parse("-0.15"));
    }

    /// <summary>
    /// Floor, not truncation — which is the whole difference between the two, and it only shows on
    /// the negative side.
    /// </summary>
    [Fact]
    public void Rounding_is_floor_on_both_sides_of_zero()
    {
        // 0.15 * 65536 = 9830.4
        Assert.Equal(9830, Parse("0.15").Raw);
        Assert.Equal(-9831, Parse("-0.15").Raw);

        // An exactly representable value is not moved by the rounding rule at all.
        Assert.Equal(Fixed.One / 2, Parse("0.5").Raw);
        Assert.Equal(-Fixed.One / 2, Parse("-0.5").Raw);
    }

    /// <summary>
    /// A sub-resolution value floors to zero rather than to something. Silent, but correct, and worth
    /// pinning: it is the behaviour a designer would be surprised by.
    /// </summary>
    [Fact]
    public void A_value_below_the_resolution_floors_rather_than_rounding_up()
    {
        Assert.Equal(Ratio.Zero, Parse("0.000001"));
        Assert.Equal(new Ratio(-1), Parse("-0.000001"));
    }

    [Fact]
    public void A_leading_plus_and_a_bare_fraction_are_both_read()
    {
        Assert.Equal(Ratio.One, Parse("+1"));
        Assert.Equal(Ratio.FromFraction(15, 100), Parse(".15"));
    }

    /// <summary>Trailing zeroes change the digit count and must not change the number.</summary>
    [Fact]
    public void Trailing_zeroes_do_not_change_the_value()
    {
        Assert.Equal(Parse("0.5"), Parse("0.500000000"));
        Assert.Equal(Parse("2"), Parse("2.0"));
    }

    // ---- what it refuses -------------------------------------------------------------------------

    [Fact]
    public void Text_that_is_not_a_decimal_is_refused_and_never_coerced()
    {
        Assert.Contains("empty", Refuse(""), StringComparison.Ordinal);
        Assert.Contains("no digits", Refuse("-"), StringComparison.Ordinal);
        Assert.Contains("decimal point", Refuse("1."), StringComparison.Ordinal);
        Assert.Contains("not a digit", Refuse("1.5x"), StringComparison.Ordinal);
        Assert.Contains("not a digit", Refuse("1 000"), StringComparison.Ordinal);
        Assert.Contains("not a digit", Refuse("1e-3"), StringComparison.Ordinal);
        Assert.Contains("not a digit", Refuse("1.2.3"), StringComparison.Ordinal);
    }

    /// <summary>
    /// A grouped or comma-spelled number is refused rather than read as something else. This is the
    /// one that would bite: <c>"1,5"</c> is a perfectly ordinary way to write one and a half in much
    /// of Europe, and reading it as fifteen would be a balance change delivered by a keyboard layout.
    /// </summary>
    [Fact]
    public void A_number_spelled_for_another_locale_is_refused_rather_than_misread()
    {
        Assert.Contains("not a digit", Refuse("1,5"), StringComparison.Ordinal);
        Assert.Contains("not a digit", Refuse("1.000,5"), StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_Q16_16_cannot_hold_is_refused_rather_than_wrapped()
    {
        Assert.Contains("32,768", Refuse("40000"), StringComparison.Ordinal);
        Assert.Contains("32,768", Refuse("-40000"), StringComparison.Ordinal);
        Assert.Contains("32,768", Refuse("32768"), StringComparison.Ordinal);
    }

    /// <summary>
    /// Ten fractional digits is refused, because the tenth cannot be represented and accepting it
    /// would tell a designer their number was read when it was rounded away.
    /// </summary>
    [Fact]
    public void More_precision_than_the_representation_holds_is_refused()
    {
        Assert.Contains("fractional digits", Refuse("0.0000000001"), StringComparison.Ordinal);
        Assert.Equal(Ratio.Zero, Parse("0.000000001"));
    }

    // ---- the measurement -------------------------------------------------------------------------

    /// <summary>
    /// <b>The claim, measured.</b> Every value above is re-read under a culture whose decimal
    /// separator, group separator and negative sign have all been moved, and none of them moves.
    /// </summary>
    [Fact]
    public void The_current_culture_cannot_move_a_single_parsed_value()
    {
        string[] cases =
            ["0", "1", "-1", "0.15", "-0.15", "0.5", "2.0", "32767", "0.000001", ".15"];

        int[] invariant = cases.Select(c => Parse(c).Raw).ToArray();
        int[] hostile = UnderHostileCulture(() => cases.Select(c => Parse(c).Raw).ToArray());

        Assert.Equal(invariant, hostile);
    }

    /// <summary>
    /// And the refusals do not move either — a locale cannot turn a refused spelling into an accepted
    /// one, which is the direction that would actually change a city.
    /// </summary>
    [Fact]
    public void The_current_culture_cannot_make_a_refused_spelling_acceptable()
    {
        UnderHostileCulture<object?>(() =>
        {
            Assert.False(QuotedDecimal.TryParse("1,5", out _, out _));
            Assert.False(QuotedDecimal.TryParse("1.000,5", out _, out _));
            return null;
        });
    }

    /// <summary>
    /// <b>The other half of the claim, which belongs to the library rather than to us.</b>
    /// <c>adr/0048</c> lets Tomlyn read the integers, so <em>"Tomlyn's integer parsing is
    /// culture-insensitive"</em> is a load-bearing assumption of the whole loader. It is measurable,
    /// so it is measured here rather than believed.
    /// </summary>
    [Fact]
    public void The_current_culture_cannot_move_an_integer_the_toml_parser_read()
    {
        const string Ruleset = """
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 1000000 } ]

            [[rule]]
            name = "bake"
            kind = "bakery"
            rate = 1024
            apply = { min = 1, max = 12 }
            inputs = [ { scope = "local", resource = "flour", amount = 6 } ]
            """;

        static (int Capacity, uint Rate, int Max, int Amount) Read()
        {
            RulesetLoadResult result = RulesetLoader.Parse(Ruleset, "culture.toml");
            Assert.True(result.Ok, result.Describe());

            Ruleset ruleset = result.Ruleset!;
            RuleDefinition rule = ruleset.Rule(new RuleId(1));
            return (ruleset.BinsOf(1)[0].Capacity.Units, rule.Rate, rule.Apply.Max,
                ruleset.Inputs(new RuleId(1))[0].Amount);
        }

        Assert.Equal(Read(), UnderHostileCulture(Read));
    }

    /// <summary>
    /// The loader's advice is followed to its end: an unquoted decimal is refused, and the quoted
    /// form it points at is one this project can actually read.
    /// </summary>
    /// <remarks>
    /// Refusal 3 and this routine are two halves of one policy, and until task 4 only the refusing
    /// half existed — the loader was telling designers to write a spelling nothing could consume.
    /// </remarks>
    [Fact]
    public void The_spelling_the_loader_demands_is_the_spelling_this_routine_reads()
    {
        RulesetLoadResult refused = RulesetLoader.Parse("""
            [[resource]]
            name = "flour"
            family = "good"
            decline_rate = 0.15
            """, "decimal.toml");

        Assert.False(refused.Ok);
        Assert.Contains("unquoted decimal", refused.Describe(), StringComparison.Ordinal);
        Assert.Contains("\"0.15\"", refused.Describe(), StringComparison.Ordinal);

        Assert.Equal(Ratio.FromFraction(15, 100), Parse("0.15"));
    }
}
