namespace Borough.Core.Arithmetic;

/// <summary>
/// Integer arithmetic with stated semantics.
/// </summary>
/// <remarks>
/// <para>
/// Division and shifts are <em>specified rather than banned</em>, because banning them would ban
/// arithmetic. Raw <c>/</c> and non-constant <c>&lt;&lt;</c> are lints as of slice 3
/// (<c>BOR0203</c>, <c>BOR0204</c>), and this namespace is the one place exempt from them, because
/// it is where their replacements are implemented.
/// </para>
/// <para>
/// <b>Why division needs a helper.</b> C# truncates toward zero, so <c>-7 / 2 == -3</c> while
/// <c>7 / 2 == 3</c>. That is deterministic — both runs agree, so the State Hash cannot see it — but
/// it is a <em>directional bias whose sign flips at every zero crossing</em>, which is a slow leak
/// in anything that accumulates around zero. Flooring biases uniformly instead, and a uniform bias
/// is one a designer can reason about.
/// </para>
/// <para>
/// <b>Why shifts need a helper.</b> Shift counts are silently masked by the CPU: <c>x &lt;&lt; 32</c>
/// on an <see cref="int"/> is <c>x &lt;&lt; 0</c>, not zero. That is a bug which produces plausible
/// output forever and never throws. Constant shifts are safe because they are visible; a shift by a
/// computed value is not.
/// </para>
/// </remarks>
public static class IntegerMath
{
    /// <summary>
    /// Magnitude, rejecting the one input that has no magnitude in two's complement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because <c>BOR0202</c> bans <c>Math.Abs</c> and finding the replacement found
    /// a defect.</b> <c>05 §4</c> bans every <c>Math.*</c> member and the ban reads over-broad here —
    /// <c>Math.Abs(int)</c> is exact integer arithmetic with no intrinsic to vary. But it also
    /// throws <see cref="OverflowException"/> on <see cref="int.MinValue"/>, which
    /// <c>Tiles.Magnitude</c> was propagating without saying so. The absolute rule was cheaper to
    /// obey than to argue with, and obeying it surfaced the edge case.
    /// </para>
    /// <para>
    /// Throwing rather than saturating follows <see cref="ShiftLeft(int,int)"/>: there is no correct
    /// answer, so the loud wrong answer beats the quiet one. No quantity in the core can reach
    /// <see cref="int.MinValue"/> — the map is 4096² Tiles — so this is a guard against a bug
    /// upstream rather than a case to handle.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> is <see cref="int.MinValue"/>, whose magnitude is not representable.
    /// </exception>
    public static int Abs(int value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, int.MinValue);
        return value < 0 ? -value : value;
    }

    /// <summary>Divides, rounding toward negative infinity. The default rounding in the core.</summary>
    public static int FloorDiv(int numerator, int denominator)
    {
        int quotient = numerator / denominator;
        // Truncation and flooring differ only when the signs disagree and the division was inexact.
        if ((numerator % denominator != 0) && ((numerator < 0) != (denominator < 0)))
        {
            quotient--;
        }

        return quotient;
    }

    /// <inheritdoc cref="FloorDiv(int,int)"/>
    public static long FloorDiv(long numerator, long denominator)
    {
        long quotient = numerator / denominator;
        if ((numerator % denominator != 0) && ((numerator < 0) != (denominator < 0)))
        {
            quotient--;
        }

        return quotient;
    }

    /// <summary>
    /// Divides, rounding toward positive infinity. Stated for the sizing cases — how many Chunks
    /// cover a span, how many Ticks a job needs — where flooring would under-provision.
    /// </summary>
    public static int CeilDiv(int numerator, int denominator) =>
        -FloorDiv(-numerator, denominator);

    /// <summary>
    /// Divides, rounding half away from zero. Use only where a designer would expect
    /// "round the number"; it is not the default because it is not the cheapest.
    /// </summary>
    public static int RoundDiv(int numerator, int denominator)
    {
        int doubled = numerator * 2;
        int rounded = (numerator < 0) != (denominator < 0)
            ? doubled - denominator
            : doubled + denominator;

        return rounded / (denominator * 2);
    }

    /// <summary>Left-shifts, rejecting a count the hardware would silently mask.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The count is outside <c>[0, 31]</c>, which would be masked rather than saturated.
    /// </exception>
    public static int ShiftLeft(int value, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 31);
        return value << count;
    }

    /// <summary>
    /// Arithmetic right-shift, rejecting a masked count. Note this floors for negative values,
    /// which is deliberately the same rounding <see cref="FloorDiv(int,int)"/> gives.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside <c>[0, 31]</c>.</exception>
    public static int ShiftRight(int value, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 31);
        return value >> count;
    }

    /// <inheritdoc cref="ShiftLeft(int,int)"/>
    public static long ShiftLeft(long value, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 63);
        return value << count;
    }

    /// <inheritdoc cref="ShiftRight(int,int)"/>
    public static long ShiftRight(long value, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 63);
        return value >> count;
    }
}
