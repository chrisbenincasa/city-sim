using Borough.Core.Arithmetic;

namespace Borough.Tests.Arithmetic;

/// <summary>
/// plans/0005 task 3. The point of these is not that division works — it is that division rounds
/// the way the core says it does, at the sign boundary where C# and the core disagree.
/// </summary>
public class IntegerMathTests
{
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
