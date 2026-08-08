using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Formats;

/// <summary>
/// The one route from an authored decimal to a <see cref="Ratio"/>. <b>Hand-rolled, digit by digit,
/// with no library parse anywhere on the path.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0048</c> makes a tuning number either a bare integer or a <em>quoted</em> decimal, and
/// <see cref="RulesetLoader"/> refuses the unquoted form by name. That refusal is only half a
/// policy: it tells an author to write <c>decline_rate = "0.15"</c>, and something has to be able to
/// read what they wrote. This is that something.
/// </para>
/// <para>
/// <b>Why the string is taken apart by hand rather than handed to <c>decimal.Parse</c> with
/// <c>CultureInfo.InvariantCulture</c>.</b> The invariant-culture spelling is correct and is one
/// forgotten argument away from being wrong — the defect it guards against is silent, arrives by
/// autocomplete, and reproduces only on a machine configured differently from the author's. Reading
/// the digits directly removes the argument, and with it the class of defect: there is no format
/// provider to pass, no separator to look up, and nothing in the routine that a locale could move.
/// <c>InvariantGlobalization</c> in <c>Directory.Build.props</c> is a second line and not this one;
/// the property is a build setting a future project file could drop, and this routine would still be
/// right the morning after that happened.
/// </para>
/// <para>
/// <b>It also means no <c>decimal</c> and no <c>double</c> ever exists on the path into the
/// simulation</b>, which is the sentence <c>adr/0003</c> is actually asking for. A routine that went
/// via <c>decimal</c> and narrowed at the end would satisfy the boundary test in
/// <c>BoundaryTests</c> — the value never reaches the core — while still deciding the last bit of a
/// tuning number by a rounding rule nobody in this repository wrote.
/// </para>
/// <para>
/// <b>Rounding is floor, matching <see cref="Fixed"/> throughout.</b> Most authored decimals have no
/// exact Q16.16 representation — <c>"0.15"</c> is 9830.4 sixty-fourths of a thousand and lands
/// between two representable values — so a rounding rule is owed whether or not it is stated. Floor
/// is chosen because it is the rule the rest of the arithmetic already uses, which makes
/// <c>"0.15"</c> and <see cref="Ratio.FromFraction"/><c>(15, 100)</c> the same number rather than two
/// numbers that agree until they do not.
/// </para>
/// </remarks>
public static class QuotedDecimal
{
    /// <summary>
    /// The most fractional digits accepted. Nine is far past Q16.16's ≈ 1.5e-5 resolution, so a
    /// tenth digit could never change the result — it can only mean the author believes they are
    /// specifying something this representation cannot hold.
    /// </summary>
    public const int MaxFractionalDigits = 9;

    /// <summary>The most whole digits accepted. Q16.16 tops out just short of 32,768.</summary>
    private const int MaxWholeDigits = 5;

    private static readonly long[] PowersOfTen =
        [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000];

    /// <summary>
    /// Reads a decimal written as a string into a <see cref="Ratio"/>, or explains why it will not.
    /// </summary>
    /// <param name="text">The authored text, without its quotes — <c>0.15</c>, <c>-2</c>, <c>7.5</c>.</param>
    /// <param name="value">The value, exact to the digits given and then floored to Q16.16.</param>
    /// <param name="reason">
    /// Why the text was refused, phrased for the person who wrote it. Null on success.
    /// </param>
    /// <returns>True if the text was read.</returns>
    public static bool TryParse(string? text, out Ratio value, out string? reason)
    {
        value = Ratio.Zero;
        reason = null;

        if (string.IsNullOrEmpty(text))
        {
            reason = "an empty string is not a number.";
            return false;
        }

        int at = 0;
        bool negative = text[0] == '-';

        if (negative || text[0] == '+')
        {
            at = 1;
        }

        long whole = 0;
        int wholeDigits = 0;

        while (at < text.Length && IsDigit(text[at]))
        {
            if (wholeDigits == MaxWholeDigits)
            {
                reason = $"'{text}' is outside the ±32,768 a Q16.16 ratio can hold.";
                return false;
            }

            whole = (whole * 10) + (text[at] - '0');
            wholeDigits++;
            at++;
        }

        long fraction = 0;
        int fractionDigits = 0;

        if (at < text.Length && text[at] == '.')
        {
            at++;

            while (at < text.Length && IsDigit(text[at]))
            {
                if (fractionDigits == MaxFractionalDigits)
                {
                    reason = $"'{text}' has more than {MaxFractionalDigits} fractional digits. "
                        + "A Q16.16 ratio resolves to about 0.000015, so the digits past that one "
                        + "cannot be represented and would be silently discarded.";
                    return false;
                }

                fraction = (fraction * 10) + (text[at] - '0');
                fractionDigits++;
                at++;
            }

            if (fractionDigits == 0)
            {
                reason = $"'{text}' ends in a decimal point with no digits after it.";
                return false;
            }
        }

        if (at != text.Length)
        {
            reason = $"'{text}' is not a decimal number -- '{text[at]}' is not a digit. "
                + "Write it plainly, as \"0.15\": no grouping separators, no exponent, and no "
                + "unit suffix.";
            return false;
        }

        if (wholeDigits == 0 && fractionDigits == 0)
        {
            reason = $"'{text}' has no digits in it.";
            return false;
        }

        // Exact to here. One division, one rounding, and the rounding is the one Fixed uses.
        long scale = PowersOfTen[fractionDigits];
        long numerator = IntegerMath.ShiftLeft((whole * scale) + fraction, Fixed.FractionalBits);
        long raw = IntegerMath.FloorDiv(negative ? -numerator : numerator, scale);

        if (raw is < Fixed.MinValue or > Fixed.MaxValue)
        {
            reason = $"'{text}' is outside the ±32,768 a Q16.16 ratio can hold.";
            return false;
        }

        value = new Ratio((int)raw);
        return true;
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';
}
