using Borough.Core.Arithmetic;

namespace S5.Lanes.Lanes;

/// <summary>
/// The two <em>exact</em> alternatives to <c>Fixed.Div</c>, measured against it and against the
/// approximate reciprocal form L1 already carries.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because S5's own attribution stopped one question short.</b> L1 measured the IDM
/// as written against a precomputed <em>approximate</em> reciprocal and reported 1.63–1.75×, with
/// the standing caveat that the reciprocal changes the arithmetic, therefore the State Hash,
/// therefore is a design change under <c>CLAUDE.md</c>'s own test however it was motivated. What it
/// did not ask is whether the speed is available <em>without</em> changing the arithmetic. It is,
/// twice over, and neither form moves a single bit.
/// </para>
/// <para>
/// <b>The reordering is free and is not about the IDM at all.</b>
/// <c>IntegerMath.FloorDiv</c> spells its correction as
/// <c>(n % d != 0) &amp;&amp; ((n &lt; 0) != (d &lt; 0))</c>, and the modulo is the <em>first</em>
/// operand, so it is always evaluated. RyuJIT does not fuse it with the division above it, so every
/// <c>FloorDiv</c> is <b>two</b> 64-bit divisions. Swapping the operands short-circuits the modulo
/// away whenever the signs agree. <c>&amp;&amp;</c> over two side-effect-free conditions is
/// commutative, so the result is bit-identical by construction — and <c>Fixed.Div</c> is the
/// substrate's divide, so this reaches every division site in the simulation rather than these
/// three.
/// </para>
/// <para>
/// <b>The magic form is exact, not approximate, and that is the whole point.</b> For a divisor
/// fixed at Ruleset load there is a multiplier and a shift reproducing <c>floor(n/d)</c>
/// bit-for-bit over a bounded dividend (Granlund &amp; Montgomery 1994). It is not a reciprocal and
/// it does not round: <see cref="MagicDivisor.For"/> searches for the smallest shift that is exact
/// at <em>every</em> quotient boundary in range and refuses to return one that is not.
/// </para>
/// <para>
/// The 128-bit intermediate is required rather than chosen. The product <c>n × M</c> runs to 65–70
/// bits across a realistic spread of driver speeds, so a 64-bit form would be correct only below a
/// speed cap — and a correctness property conditional on a tuning number is a worse foundation than
/// the division it replaces. <c>UInt128</c> is one <c>mulx</c> and needs no <c>Math.*</c>, so it
/// trips no lint; <c>Harness/Timing.cs</c> already uses <c>Int128</c> for the same kind of reason.
/// </para>
/// </remarks>
internal static class ExactDivision
{
    /// <summary>
    /// <c>IntegerMath.FloorDiv</c> with the two conditions of its correction swapped. Bit-identical
    /// to the shipped form; the modulo is skipped whenever the signs agree.
    /// </summary>
    public static long FloorDivReordered(long numerator, long denominator)
    {
        // BOR0203 is suppressed for exactly one statement, and for the same reason
        // `Borough.Core.Arithmetic` is exempt from it wholesale: this *is* a rounding
        // implementation, so it is where the raw operator has to appear. The lint's demand — state
        // the rounding — is satisfied by the correction below, which is the shipped one with its
        // two conditions swapped and nothing else.
#pragma warning disable BOR0203
        long quotient = numerator / denominator;
#pragma warning restore BOR0203
        if (((numerator < 0) != (denominator < 0)) && (numerator % denominator != 0))
        {
            quotient--;
        }

        return quotient;
    }

    /// <summary><c>Fixed.Div</c> over the reordered correction.</summary>
    public static int DivReordered(int a, int b) =>
        checked((int)FloorDivReordered((long)a << 16, b));
}

/// <summary>
/// An exact floor-division by a divisor known at Ruleset load, as a multiplier and a shift.
/// </summary>
/// <remarks>
/// Constructed once and verified at construction. <see cref="Divide"/> is exact for every dividend
/// in <c>[-maxAbsDividend, +maxAbsDividend]</c> and agrees with <c>IntegerMath.FloorDiv</c> on sign,
/// which matters because the interaction term's dividend straddles zero.
/// </remarks>
internal readonly struct MagicDivisor
{
    public readonly ulong Multiplier;
    public readonly int Shift;
    public readonly int Divisor;

    private MagicDivisor(ulong multiplier, int shift, int divisor)
    {
        Multiplier = multiplier;
        Shift = shift;
        Divisor = divisor;
    }

    /// <summary>
    /// The smallest shift whose round-up multiplier is exact at every quotient boundary in range.
    /// </summary>
    /// <remarks>
    /// <b>Boundaries are sufficient and the whole range is not needed.</b> <c>floor(n/d)</c> is a
    /// step function changing only at multiples of <c>d</c>, and the round-up magic errs only by
    /// overshooting, so if it agrees at <c>kd − 1</c> and <c>kd</c> for every <c>k</c> it agrees
    /// everywhere between. That turns a 2^33-point check into a 2^17-point one.
    /// </remarks>
    public static MagicDivisor For(int divisor, long maxAbsDividend)
    {
        if (divisor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), "The magic form assumes d > 0.");
        }

        for (int shift = 1; shift < 96; shift++)
        {
            UInt128 multiplier =
                ((UInt128.One << shift) + (UInt128)(uint)divisor - 1) / (UInt128)(uint)divisor;

            if (multiplier > ulong.MaxValue)
            {
                continue;
            }

            if (IsExact((ulong)multiplier, shift, divisor, maxAbsDividend))
            {
                return new MagicDivisor((ulong)multiplier, shift, divisor);
            }
        }

        throw new InvalidOperationException(
            $"No exact multiplier for divisor {divisor} over ±{maxAbsDividend}.");
    }

    private static bool IsExact(ulong multiplier, int shift, int divisor, long maxAbsDividend)
    {
        for (long k = 0; k <= IntegerMath.FloorDiv(maxAbsDividend, divisor) + 1; k++)
        {
            long boundary = k * divisor;

            if (boundary - 1 >= 0 && boundary - 1 <= maxAbsDividend
                && (long)(((UInt128)multiplier * (UInt128)(boundary - 1)) >> shift)
                   != IntegerMath.FloorDiv(boundary - 1, divisor))
            {
                return false;
            }

            if (boundary <= maxAbsDividend
                && (long)(((UInt128)multiplier * (UInt128)boundary) >> shift) != IntegerMath.FloorDiv(boundary, divisor))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Exact <c>floor(numerator / Divisor)</c>, for either sign of the numerator.</summary>
    public long Divide(long numerator)
    {
        long magnitude = numerator < 0 ? -numerator : numerator;
        long quotient = (long)(((UInt128)Multiplier * (UInt128)magnitude) >> Shift);

        if (numerator >= 0)
        {
            return quotient;
        }

        // Floor, not truncation: a negative dividend rounds away from zero unless d divides it
        // exactly. The remainder is recovered by a multiply, never by a second division.
        return magnitude - (quotient * Divisor) == 0 ? -quotient : -quotient - 1;
    }

    /// <summary>The Q16.16 <c>Fixed.Div</c> shape: <c>floor((a &lt;&lt; 16) / Divisor)</c>.</summary>
    public int DivideFixed(int a) => checked((int)Divide((long)a << 16));
}

/// <summary>
/// The load-time tables the exact kernel needs: one multiplier per driver for <c>v/v0</c>, and one
/// for the Ruleset's <c>2√(ab)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shift is shared across every driver so that the per-Vehicle column is a multiplier and
/// nothing else.</b> A per-driver shift would make the column 12 bytes and would be measuring a
/// different question. The shared shift is the smallest one exact for every <em>distinct</em>
/// divisor in the population, which is what makes construction affordable: the fixture draws
/// <c>v0</c> from a spread of twenty values over millions of Vehicles, so twenty divisors are
/// verified and the rest is arithmetic.
/// </para>
/// <para>
/// <b>The width the multiplier needs is reported rather than assumed</b>, because it is the row
/// cost and therefore the honest comparison against L1's approximate reciprocal, which is 4 bytes.
/// </para>
/// </remarks>
internal sealed class MagicTables
{
    public required ulong[] DesiredSpeedMultiplier { get; init; }
    public required int DesiredSpeedShift { get; init; }
    public required MagicDivisor Interaction { get; init; }

    /// <summary>The widest multiplier in the driver table, in bits. 32 or fewer means a `uint` column.</summary>
    public required int MultiplierBits { get; init; }

    /// <summary>The dividend bound the driver table was verified over, in bits.</summary>
    public required int DividendBits { get; init; }

    public static MagicTables For(LaneNetwork n, int maxSpeed)
    {
        // Site A's dividend is v << 16, and v is bounded by the fastest driver in the world rather
        // than by this Vehicle's own v0: a Vehicle promoted at free flow into a slow driver's Lane
        // decelerates from above. Doubled for headroom, because a bound that is merely usually true
        // is not a bound.
        long dividendBoundA = ((long)maxSpeed << 16) * 2;

        // Site B's is Mul(v, closing) << 16, and closing spans ±v_max. Both signs occur.
        long interactionBound = ((((long)maxSpeed * maxSpeed * 2) >> 16) << 16) * 2;

        int[] distinct = Distinct(n.DesiredSpeed, n.Vehicles);
        int shift = SharedShift(distinct, dividendBoundA);

        var multiplier = new ulong[n.Vehicles];
        ulong widest = 0;
        for (int i = 0; i < n.Vehicles; i++)
        {
            int divisor = n.DesiredSpeed[i];
            if (divisor <= 0)
            {
                continue;
            }

            ulong m = (ulong)(((UInt128.One << shift) + (UInt128)(uint)divisor - 1)
                              / (UInt128)(uint)divisor);
            multiplier[i] = m;
            if (m > widest)
            {
                widest = m;
            }
        }

        return new MagicTables
        {
            DesiredSpeedMultiplier = multiplier,
            DesiredSpeedShift = shift,
            Interaction = MagicDivisor.For(Units.TwoRootAb, interactionBound),
            MultiplierBits = 64 - System.Numerics.BitOperations.LeadingZeroCount(widest),
            DividendBits = 64 - System.Numerics.BitOperations.LeadingZeroCount((ulong)dividendBoundA),
        };
    }

    private static int[] Distinct(int[] values, int count)
    {
        var copy = new int[count];
        Array.Copy(values, copy, count);
        Array.Sort(copy);

        var unique = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (copy[i] > 0 && (unique.Count == 0 || unique[^1] != copy[i]))
            {
                unique.Add(copy[i]);
            }
        }

        return unique.ToArray();
    }

    private static int SharedShift(int[] divisors, long maxAbsDividend)
    {
        int shift = 1;
        foreach (int divisor in divisors)
        {
            MagicDivisor one = MagicDivisor.For(divisor, maxAbsDividend);
            if (one.Shift > shift)
            {
                shift = one.Shift;
            }
        }

        // A shift large enough for the worst divisor is not automatically exact for the others, so
        // every divisor is re-verified at the shared shift rather than assumed to survive it.
        foreach (int divisor in divisors)
        {
            ulong m = (ulong)(((UInt128.One << shift) + (UInt128)(uint)divisor - 1)
                              / (UInt128)(uint)divisor);

            for (long k = 0; k <= IntegerMath.FloorDiv(maxAbsDividend, divisor) + 1; k++)
            {
                long boundary = k * divisor;

                if (boundary - 1 >= 0 && boundary - 1 <= maxAbsDividend
                    && (long)(((UInt128)m * (UInt128)(boundary - 1)) >> shift)
                       != IntegerMath.FloorDiv(boundary - 1, divisor))
                {
                    throw new InvalidOperationException(
                        $"Shared shift {shift} is not exact for divisor {divisor}.");
                }

                if (boundary <= maxAbsDividend
                    && (long)(((UInt128)m * (UInt128)boundary) >> shift) != IntegerMath.FloorDiv(boundary, divisor))
                {
                    throw new InvalidOperationException(
                        $"Shared shift {shift} is not exact for divisor {divisor}.");
                }
            }
        }

        return shift;
    }
}
