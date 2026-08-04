namespace Borough.Core.Arithmetic;

/// <summary>
/// Q16.16 fixed-point arithmetic. <b>This is the one place in <c>Borough.Core</c> where
/// <c>checked</c> appears.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The overflow posture, stated where it is enforced rather than only in prose.</b> adr/0003's
/// policy is that width is chosen at the type so that the common case cannot overflow, and
/// <c>checked</c> is scoped to this library alone. Ambient arithmetic in the core stays
/// <c>unchecked</c>: a check is worthless where the width already closes the question, and it is not
/// free on the hottest code in the project.
/// </para>
/// <para>
/// <b>It is not free here either, and S4 measured the price.</b> K1 put <c>checked</c> at
/// <b>27% on x86-64 and 29% on arm64</b>, on a scan that does nothing but the multiply — the worst
/// case, since a real Rule does other work against which the check amortises. Whether that counts as
/// "cheap" is a judgement adr/0003 still owes; see docs/spike-results.md. It is paid here because
/// this is the narrowing step, and a narrowing cast that wraps is the one defect class the State Hash
/// cannot find: both runs wrap identically, the hashes agree, replay succeeds, and the city is wrong.
/// </para>
/// <para>
/// <b>The footgun, also from K1, and the reason every <c>checked</c> below wraps an expression rather
/// than a block.</b> <c>checked</c> is a <em>block</em> in C#, so scoping it to a region that also
/// indexes a pointer silently prices the address arithmetic — worth a further 40 points on x86-64 and
/// <b>95 on arm64</b>. The fix is to scope it to the value expression, which is what these methods do.
/// </para>
/// <para>
/// <b>Rounding is floor throughout</b>, matching <see cref="IntegerMath.FloorDiv(int,int)"/>. An
/// arithmetic right-shift already floors, so the multiply gets this for free and the divide is made
/// to agree. Floor is chosen over truncation because truncation's bias reverses sign at zero;
/// see <see cref="IntegerMath"/>.
/// </para>
/// </remarks>
public static class Fixed
{
    /// <summary>Fractional bits. A format constant, not a tuning value — changing it changes the State Hash.</summary>
    public const int FractionalBits = 16;

    /// <summary>The representation of 1.0.</summary>
    public const int One = 1 << FractionalBits;

    /// <summary>The largest representable value, ≈ 32,767.99998.</summary>
    public const int MaxValue = int.MaxValue;

    /// <summary>The smallest representable value, ≈ -32,768.</summary>
    public const int MinValue = int.MinValue;

    /// <summary>Converts a whole number to Q16.16.</summary>
    /// <exception cref="OverflowException">
    /// The value is outside ±32,768 and has no Q16.16 representation.
    /// </exception>
    public static int FromInt(int value) => checked(value * One);

    /// <summary>Truncates toward negative infinity to a whole number.</summary>
    public static int ToIntFloor(int fixedValue) => fixedValue >> FractionalBits;

    /// <summary>
    /// Multiplies two Q16.16 values. The intermediate is 64-bit, so only the narrowing can overflow.
    /// </summary>
    /// <exception cref="OverflowException">
    /// The product is outside ±32,768. The caller is multiplying two quantities whose magnitudes the
    /// design did not anticipate — the fix is a range assertion at the call site, not a wider type.
    /// </exception>
    public static int Mul(int a, int b) => checked((int)(((long)a * b) >> FractionalBits));

    /// <summary>Divides two Q16.16 values, rounding toward negative infinity.</summary>
    /// <exception cref="DivideByZeroException">The divisor is zero.</exception>
    /// <exception cref="OverflowException">The quotient is outside ±32,768.</exception>
    public static int Div(int a, int b) =>
        checked((int)IntegerMath.FloorDiv((long)a << FractionalBits, b));

    /// <summary>
    /// Linear interpolation. <paramref name="t"/> is a Q16.16 ratio; values outside <c>[0, One]</c>
    /// extrapolate rather than clamp, which is deliberate — a clamp here would hide a caller whose
    /// ratio is wrong.
    /// </summary>
    public static int Lerp(int a, int b, int t) => a + Mul(b - a, t);

    /// <summary>
    /// Asserts that a Q16.16 value lies within the magnitude a call site claims for it.
    /// </summary>
    /// <remarks>
    /// This exists so that a range guard states <em>what it is guarding</em> rather than asserting a
    /// generic bound. "This VDF ratio is expected in [0, 3]" is a claim a later reader can check
    /// against the design; "value fits in an int" is not.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the claimed range.</exception>
    public static int AssertMagnitude(int fixedValue, int minWhole, int maxWhole)
    {
        int min = FromInt(minWhole);
        int max = FromInt(maxWhole);

        if (fixedValue < min || fixedValue > max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fixedValue),
                fixedValue,
                $"Expected a Q16.16 value within [{minWhole}, {maxWhole}].");
        }

        return fixedValue;
    }
}
