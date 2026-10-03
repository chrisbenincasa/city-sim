using Borough.Core.Arithmetic;

namespace Borough.Tests.Arithmetic;

public class TranscendentalTrigTests
{
    [Fact]
    public void Sin_and_cos_match_the_reference_over_every_representable_turn_fraction()
    {
        double worstSin = 0;
        double worstCos = 0;
        for (int angle = 0; angle < Fixed.One; angle++)
        {
            double radians = angle * Math.Tau / Fixed.One;
            int sin = Transcendental.Sin(angle);
            int cos = Transcendental.Cos(angle);
            Assert.InRange(sin, -Fixed.One, Fixed.One);
            Assert.InRange(cos, -Fixed.One, Fixed.One);
            worstSin = Math.Max(worstSin, Math.Abs(sin - (Math.Sin(radians) * Fixed.One)));
            worstCos = Math.Max(worstCos, Math.Abs(cos - (Math.Cos(radians) * Fixed.One)));
        }

        Assert.InRange(worstSin, 0, 1);
        Assert.InRange(worstCos, 0, 1);
    }

    [Theory]
    [InlineData(0, 0, 65536)]
    [InlineData(16384, 65536, 0)]
    [InlineData(32768, 0, -65536)]
    [InlineData(49152, -65536, 0)]
    [InlineData(-16384, -65536, 0)]
    [InlineData(int.MinValue, 0, 65536)]
    public void Quarter_turns_are_exact(int angle, int sin, int cos)
    {
        Assert.Equal(sin, Transcendental.Sin(angle));
        Assert.Equal(cos, Transcendental.Cos(angle));
    }

    [Fact]
    public void Trigonometry_wraps_at_both_int_extremes()
    {
        int[] angles = [int.MinValue, int.MinValue + 1, -65537, -1, 65536, int.MaxValue - 1, int.MaxValue];
        foreach (int angle in angles)
        {
            Assert.Equal(Transcendental.Sin(angle & 65535), Transcendental.Sin(angle));
            Assert.Equal(Transcendental.Cos(angle & 65535), Transcendental.Cos(angle));
        }
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 16384)]
    [InlineData(0, -1, 32768)]
    [InlineData(-1, 0, -16384)]
    [InlineData(1, 1, 8192)]
    [InlineData(1, -1, 24576)]
    [InlineData(-1, -1, -24576)]
    [InlineData(-1, 1, -8192)]
    [InlineData(long.MinValue, long.MinValue, -24576)]
    [InlineData(long.MaxValue, long.MaxValue, 8192)]
    public void Atan2_axes_and_diagonals_are_exact(long y, long x, int angle) =>
        Assert.Equal(angle, Transcendental.Atan2(y, x));

    [Fact]
    public void Atan2_matches_the_reference_in_all_octants_and_at_common_scales()
    {
        double worst = 0;
        long[] scales = [1, 1L << 40, long.MaxValue / Fixed.One];
        for (int fraction = 0; fraction <= Fixed.One; fraction += 31)
        {
            foreach (long scale in scales)
            {
                long minor = fraction * scale;
                long major = Fixed.One * scale;
                for (int quadrant = 0; quadrant < 8; quadrant++)
                {
                    long x = (quadrant & 1) == 0 ? major : minor;
                    long y = (quadrant & 1) == 0 ? minor : major;
                    x = (quadrant & 2) == 0 ? x : -x;
                    y = (quadrant & 4) == 0 ? y : -y;
                    int angle = Transcendental.Atan2(y, x);
                    Assert.InRange(angle, -32767, 32768);
                    Assert.Equal(Transcendental.Atan2(y / scale, x / scale), angle);
                    double error = Math.Abs(angle - (Math.Atan2(y, x) * Fixed.One / Math.Tau));
                    worst = Math.Max(worst, Math.Min(error, Fixed.One - error));
                }
            }
        }

        Assert.InRange(worst, 0, 1);
    }

    [Fact]
    public void Atan2_handles_extreme_ratios_and_the_negative_axis_wrap()
    {
        long[] components = [long.MinValue, long.MinValue + 1, -1, 0, 1, long.MaxValue];
        foreach (long x in components)
        {
            foreach (long y in components)
            {
                int angle = Transcendental.Atan2(y, x);
                Assert.InRange(angle, -32767, 32768);
                double error = Math.Abs(angle - (Math.Atan2(y, x) * Fixed.One / Math.Tau));
                Assert.InRange(Math.Min(error, Fixed.One - error), 0, 1);
            }
        }
    }
}
