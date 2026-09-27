using Borough.Core.Space;

namespace Borough.Tests.Space;

public sealed class StallLayoutTests
{
    private static readonly StallSizes Standard = new(250, 500, 600);

    [Fact]
    public void A_centre_on_a_pictured_block_holds_seven_double_rows()
    {
        // 30 Tiles is 120 m: seven 16 m modules. 24 Tiles is 96 m: a 6 m cross aisle and 36 stalls
        // of 2.5 m along each aisle.
        StallLayout layout = StallLayout.Of(30, 24, Standard);

        Assert.Equal(new StallLayout(7, 36), layout);
        Assert.Equal(504, layout.Stalls);
    }

    [Fact]
    public void Ground_narrower_than_one_module_holds_nothing()
    {
        Assert.Equal(0, StallLayout.Of(3, 24, Standard).Stalls);
    }

    [Fact]
    public void Ground_shallower_than_the_cross_aisle_holds_nothing()
    {
        Assert.Equal(0, StallLayout.Of(30, 1, Standard).Stalls);
    }

    [Fact]
    public void Unstated_sizes_lay_out_nothing()
    {
        Assert.Equal(0, StallLayout.Of(30, 24, StallSizes.None).Stalls);
    }
}
