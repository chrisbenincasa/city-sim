namespace Borough.Core.Arithmetic;

/// <summary>
/// Division and shifts with stated semantics.
/// </summary>
/// <remarks>
/// <para>
/// These two constructs are <em>specified rather than banned</em>, because banning them would ban
/// arithmetic. Raw <c>/</c> and non-constant <c>&lt;&lt;</c> become a lint once slice 3's analyser
/// exists (plans/0006); until then this type is the convention and the lint is owed.
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
