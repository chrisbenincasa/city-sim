using Borough.Core.Arithmetic;

namespace Borough.Tests.Arithmetic;

/// <summary>
/// plans/0005 task 3. The point of these is not that division works — it is that division rounds
/// the way the core says it does, at the sign boundary where C# and the core disagree.
/// </summary>
public class IntegerMathTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 7)]
    [InlineData(-7, 7)]
    [InlineData(int.MaxValue, int.MaxValue)]
    [InlineData(-int.MaxValue, int.MaxValue)]
    public void Abs_returns_magnitude(int value, int expected) =>
        Assert.Equal(expected, IntegerMath.Abs(value));

    /// <summary>
    /// The defect BOR0202 surfaced. <c>Tiles.Magnitude</c> called <c>Math.Abs</c>, which throws here,
    /// and nothing said so. There is no correct answer — two's complement has no positive
    /// <see cref="int.MinValue"/> — so the rule is the same as <c>ShiftLeft</c>'s: the loud wrong
    /// answer beats the quiet one, which would be to return <see cref="int.MinValue"/> unchanged and
    /// let a negative magnitude propagate.
    /// </summary>
    [Fact]
    public void Abs_rejects_the_one_value_with_no_magnitude() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.Abs(int.MinValue));

    [Theory]
    // The whole reason the helper exists: C# gives -3 here, and the bias reverses at zero.
    [InlineData(-7, 2, -4)]
    [InlineData(7, 2, 3)]
    [InlineData(-7, -2, 3)]
    [InlineData(7, -2, -4)]
    // Exact division must not be perturbed by the correction.
    [InlineData(-8, 2, -4)]
    [InlineData(8, 2, 4)]
    [InlineData(0, 5, 0)]
    public void FloorDiv_rounds_toward_negative_infinity(int numerator, int denominator, int expected) =>
        Assert.Equal(expected, IntegerMath.FloorDiv(numerator, denominator));

    [Fact]
    public void FloorDiv_and_csharp_division_agree_only_when_signs_agree()
    {
        for (int n = -50; n <= 50; n++)
        {
            for (int d = -7; d <= 7; d++)
            {
                if (d == 0)
                {
                    continue;
                }

                bool signsAgree = (n < 0) == (d < 0);
                bool exact = n % d == 0;
                bool shouldMatch = signsAgree || exact;

                Assert.Equal(shouldMatch, IntegerMath.FloorDiv(n, d) == n / d);
            }
        }
    }

    [Theory]
    [InlineData(7, 2, 4)]
    [InlineData(-7, 2, -3)]
    [InlineData(8, 2, 4)]
    public void CeilDiv_rounds_toward_positive_infinity(int numerator, int denominator, int expected) =>
        Assert.Equal(expected, IntegerMath.CeilDiv(numerator, denominator));

    [Theory]
    [InlineData(7, 2, 4)]
    [InlineData(-7, 2, -4)]
    [InlineData(5, 2, 3)]
    [InlineData(1, 2, 1)]
    [InlineData(-1, 2, -1)]
    [InlineData(0, 3, 0)]
    public void RoundDiv_rounds_half_away_from_zero(int numerator, int denominator, int expected) =>
        Assert.Equal(expected, IntegerMath.RoundDiv(numerator, denominator));

    [Fact]
    public void FloorDiv_on_long_matches_the_int_overload()
    {
        for (int n = -20; n <= 20; n++)
        {
            for (int d = -5; d <= 5; d++)
            {
                if (d != 0)
                {
                    Assert.Equal(IntegerMath.FloorDiv(n, d), (int)IntegerMath.FloorDiv((long)n, d));
                }
            }
        }
    }

    [Fact]
    public void CeilDiv_on_long_matches_the_int_overload()
    {
        for (int n = -20; n <= 20; n++)
        {
            for (int d = -5; d <= 5; d++)
            {
                if (d != 0)
                {
                    Assert.Equal(IntegerMath.CeilDiv(n, d), (int)IntegerMath.CeilDiv((long)n, d));
                }
            }
        }
    }

    /// <summary>
    /// The reason the overload exists: the product overflows an <c>int</c> and the answer does not.
    /// </summary>
    /// <remarks>
    /// A Zone Rule's sample is <c>ceil(Lots × interval ÷ revisit_ticks)</c>, and 900,000 Lots at an
    /// interval of 8,191 is 7.4e9 — three and a half times what an <c>int</c> holds, for a sample that
    /// fits in one comfortably.
    /// </remarks>
    [Fact]
    public void CeilDiv_on_long_survives_a_numerator_no_int_could_hold()
    {
        const long Numerator = 900_000L * 8_191L;

        Assert.True(Numerator > int.MaxValue);
        Assert.Equal(899_891L, IntegerMath.CeilDiv(Numerator, 8_192L));
    }

    /// <summary>
    /// The bug this helper exists to make impossible: the hardware masks the count, so a raw
    /// `1 &lt;&lt; 32` is `1`, not `0`, and nothing ever throws.
    /// </summary>
    [Fact]
    public void ShiftLeft_rejects_the_count_the_hardware_would_mask()
    {
        Assert.Equal(1, 1 << 32);   // documents the footgun rather than endorsing it

        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ShiftLeft(1, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ShiftLeft(1, -1));
        Assert.Equal(1 << 31, IntegerMath.ShiftLeft(1, 31));
    }

    [Fact]
    public void ShiftRight_rejects_the_count_the_hardware_would_mask()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ShiftRight(1, 32));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ShiftRight(1, -1));
        Assert.Equal(0, IntegerMath.ShiftRight(1, 31));
    }

    [Fact]
    public void Long_shifts_reject_counts_past_63()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ShiftLeft(1L, 64));
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.ShiftRight(1L, 64));
        Assert.Equal(1L << 63, IntegerMath.ShiftLeft(1L, 63));
    }

    /// <summary>Right-shift floors for negatives, which must agree with FloorDiv by a power of two.</summary>
    [Fact]
    public void ShiftRight_agrees_with_FloorDiv_on_powers_of_two()
    {
        for (int value = -1000; value <= 1000; value++)
        {
            Assert.Equal(IntegerMath.FloorDiv(value, 8), IntegerMath.ShiftRight(value, 3));
        }
    }
}
