using System.Numerics;

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
        // The sign test is first because it is a comparison and the other is a division: RyuJIT does
        // not fuse `%` with the `/` above it, so evaluating the modulo unconditionally makes every
        // call two divisions. Measured at 1.50× on S5's Lane kernel, where two of three sites can
        // never have disagreeing signs. `&&` over two side-effect-free conditions is commutative, so
        // this is bit-identical to the other order and moves no State Hash.
        if (((numerator < 0) != (denominator < 0)) && (numerator % denominator != 0))
        {
            quotient--;
        }

        return quotient;
    }

    /// <inheritdoc cref="FloorDiv(int,int)"/>
    public static long FloorDiv(long numerator, long denominator)
    {
        long quotient = numerator / denominator;
        if (((numerator < 0) != (denominator < 0)) && (numerator % denominator != 0))
        {
            quotient--;
        }

        return quotient;
    }

    /// <summary>
    /// Divides, rounding toward positive infinity. Stated for the sizing cases — how many Chunks
    /// cover a span, how many Ticks a job needs — where flooring would under-provision.
    /// </summary>
    /// <remarks>
    /// <b>This pays two hardware divisions in its common case, and negating the numerator is why.</b>
    /// <see cref="FloorDiv(int,int)"/> evaluates its modulo only when the signs disagree — but
    /// <c>CeilDiv</c> hands it <c>-numerator</c>, so two <em>positive</em> arguments, which is every
    /// call site today, take that branch by construction. For non-negative operands
    /// <c>(numerator + denominator - 1) / denominator</c> is one division and no modulo.
    /// <b>Deliberately not done</b> (2026-08-11): all four call sites are cold — a Zone Rule's derived
    /// sample at Ruleset load, a Cell-grid span at world creation, a Layer dump — so the faster form
    /// would buy nothing measurable and would cost the negative range this one handles for free.
    /// Recorded here rather than filed, per <c>adr/0073</c>: the finding belongs where the next reader
    /// of this method is, and the trigger to act on it is <b>a hot caller appearing</b>.
    /// </remarks>
    public static int CeilDiv(int numerator, int denominator) =>
        -FloorDiv(-numerator, denominator);

    /// <inheritdoc cref="CeilDiv(int,int)"/>
    /// <remarks>
    /// <b>Widened for a sizing case whose product does not fit in 32 bits.</b> A Zone Rule's sample is
    /// <c>ceil(Lots × interval ÷ revisit_ticks)</c> (<c>adr/0059</c>), and the numerator alone reaches
    /// 40 bits at a large map and a long interval — so the multiply has to happen in a
    /// <see cref="long"/> and the division has to meet it there.
    /// </remarks>
    public static long CeilDiv(long numerator, long denominator) =>
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

    /// <inheritdoc cref="RoundDiv(int,int)"/>
    /// <remarks>
    /// <b>Widened for the one call site that needs it: normalising a two-pass convolution.</b> A
    /// separable kernel of gain <em>g</em> accumulates at <em>g²</em>, which overflows an <c>int</c>
    /// long before the field values do, so the accumulator is a <c>long</c> and the division has to
    /// meet it there. Doubling the numerator is safe at that magnitude — a Layer scaled by 6,561 needs
    /// 44 bits, not 63.
    /// </remarks>
    public static long RoundDiv(long numerator, long denominator)
    {
        long doubled = numerator * 2;
        long rounded = (numerator < 0) != (denominator < 0)
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

    /// <summary>
    /// The exact floor of the square root of a non-negative value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It enters the substrate with the Road Graph and not before.</b>
    /// <c>spikes/S2.Routing/Graph/IntegerGeometry.cs</c> held this and said in its own header that
    /// <i>"nothing here enters the substrate… it dies with the spike"</i> — correctly, because A*'s
    /// admissibility needs only a <em>lower bound</em> on a distance and a spike needs no more. What
    /// needs the true value is the generator: a freeform Arterial's Segment length is the <em>arc
    /// length</em> of its polyline, that length divides into a traversal cost, and a cost is a
    /// hashed consequence rather than a heuristic.
    /// </para>
    /// <para>
    /// <b>No division, no <c>Math</c>, and exact.</b> Newton's method needs a division per iteration
    /// and a seed; the restoring bit-by-bit algorithm below needs neither, uses only constant shifts,
    /// comparisons and subtraction, and returns the true floor rather than an approximation somebody
    /// would have to argue about. Exactness is what makes it hashable: an approximation would be a
    /// second implementation's chance to disagree.
    /// </para>
    /// <para>
    /// <b>The first line is a fix, not a flourish.</b> It was
    /// <c>long bit = 1L &lt;&lt; 62; while (bit &gt; remainder) bit &gt;&gt;= 2;</c> and that cost S2's
    /// first denominator capture most of its time — the search called it twice per pushed node and
    /// span ~30 iterations of warm-up on the small distances that dominate. <see cref="BitOperations.Log2"/>
    /// gives the exponent outright, and masking the low bit makes it even, which is the invariant the
    /// restoring loop needs.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public static int SqrtFloor(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        if (value == 0)
        {
            return 0;
        }

        long remainder = value;
        long result = 0;
        long bit = ShiftLeft(1L, BitOperations.Log2((ulong)value) & ~1);

        while (bit != 0)
        {
            if (remainder >= result + bit)
            {
                remainder -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return (int)result;
    }
}
