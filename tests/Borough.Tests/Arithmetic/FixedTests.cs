using Borough.Core.Arithmetic;

namespace Borough.Tests.Arithmetic;

/// <summary>plans/0005 task 2, and the overflow half of task 7.</summary>
public class FixedTests
{
    [Fact]
    public void One_is_the_multiplicative_identity()
    {
        Assert.Equal(Fixed.One, Fixed.Mul(Fixed.One, Fixed.One));
        Assert.Equal(Fixed.FromInt(7), Fixed.Mul(Fixed.FromInt(7), Fixed.One));
    }

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(-2, 3, -6)]
    [InlineData(-2, -3, 6)]
    [InlineData(0, 5, 0)]
    public void Mul_agrees_with_whole_number_multiplication(int a, int b, int expected) =>
        Assert.Equal(Fixed.FromInt(expected), Fixed.Mul(Fixed.FromInt(a), Fixed.FromInt(b)));

    [Theory]
    [InlineData(6, 3, 2)]
    [InlineData(-6, 3, -2)]
    [InlineData(7, 1, 7)]
    public void Div_agrees_with_whole_number_division(int a, int b, int expected) =>
        Assert.Equal(Fixed.FromInt(expected), Fixed.Div(Fixed.FromInt(a), Fixed.FromInt(b)));

    [Fact]
    public void Half_round_trips_through_Mul_and_Div()
    {
        int half = Fixed.Div(Fixed.One, Fixed.FromInt(2));
        Assert.Equal(Fixed.One / 2, half);
        Assert.Equal(Fixed.FromInt(5), Fixed.Mul(Fixed.FromInt(10), half));
    }

    /// <summary>
    /// Rounding is floor, and the interesting case is a negative inexact result — where truncation
    /// would give a different answer and the difference would be invisible to the State Hash.
    /// </summary>
    [Fact]
    public void Rounding_is_floor_and_not_truncation()
    {
        int third = Fixed.Div(Fixed.One, Fixed.FromInt(3));
        int negativeThird = Fixed.Div(Fixed.FromInt(-1), Fixed.FromInt(3));

        // Floor: -1/3 lands one representable step below the negation of 1/3.
        Assert.Equal(-third - 1, negativeThird);
        Assert.Equal(-1, Fixed.ToIntFloor(negativeThird));
        Assert.Equal(0, Fixed.ToIntFloor(third));
    }

    [Fact]
    public void ToIntFloor_rounds_toward_negative_infinity()
    {
        Assert.Equal(1, Fixed.ToIntFloor(Fixed.FromInt(1)));
        Assert.Equal(1, Fixed.ToIntFloor(Fixed.FromInt(1) + 1));
        Assert.Equal(-2, Fixed.ToIntFloor(Fixed.FromInt(-1) - 1));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 10)]
    public void Lerp_hits_both_endpoints(int t, int expected) =>
        Assert.Equal(
            Fixed.FromInt(expected),
            Fixed.Lerp(0, Fixed.FromInt(10), Fixed.FromInt(t)));

    [Fact]
    public void Lerp_finds_the_midpoint()
    {
        int half = Fixed.Div(Fixed.One, Fixed.FromInt(2));
        Assert.Equal(Fixed.FromInt(5), Fixed.Lerp(0, Fixed.FromInt(10), half));
        Assert.Equal(Fixed.FromInt(15), Fixed.Lerp(Fixed.FromInt(10), Fixed.FromInt(20), half));
    }

    /// <summary>
    /// The reason `checked` is paid here. Without it these wrap silently, both runs wrap
    /// identically, the State Hash agrees, and the city is wrong.
    /// </summary>
    [Fact]
    public void The_narrowing_step_throws_rather_than_wrapping()
    {
        Assert.Throws<OverflowException>(() => Fixed.FromInt(40_000));
        Assert.Throws<OverflowException>(() => Fixed.FromInt(-40_000));
        Assert.Throws<OverflowException>(() => Fixed.Mul(Fixed.FromInt(1_000), Fixed.FromInt(1_000)));
    }

    [Fact]
    public void Div_by_zero_throws()
        => Assert.Throws<DivideByZeroException>(() => Fixed.Div(Fixed.One, 0));

    [Fact]
    public void AssertMagnitude_states_what_it_guards()
    {
        int inRange = Fixed.FromInt(2);
        Assert.Equal(inRange, Fixed.AssertMagnitude(inRange, 0, 3));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed.AssertMagnitude(Fixed.FromInt(4), 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Fixed.AssertMagnitude(Fixed.FromInt(-1), 0, 3));
    }

    /// <summary>The headroom claim in adr/0003, asserted rather than trusted.</summary>
    [Fact]
    public void The_map_fits_inside_Q16_16_with_the_claimed_margin()
    {
        const int mapTiles = 4096;

        Assert.Equal(mapTiles * Fixed.One, Fixed.FromInt(mapTiles));
        Assert.Throws<OverflowException>(() => Fixed.FromInt(mapTiles * 8));
        Assert.Equal(32_768, (Fixed.MaxValue >> Fixed.FractionalBits) + 1);
    }
}
