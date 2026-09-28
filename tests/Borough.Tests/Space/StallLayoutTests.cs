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

    [Theory]
    [InlineData(30, 24)]
    [InlineData(17, 9)]
    [InlineData(4, 3)]
    [InlineData(3, 24)]
    public void Placing_writes_exactly_the_stalls_the_layout_counts(int along, int toward)
    {
        var stalls = new Stall[1_000];

        Assert.Equal(StallLayout.Of(along, toward, Standard).Stalls, StallLayout.Place(along, toward, Standard, stalls));
    }

    [Fact]
    public void Placed_stalls_lie_inside_the_car_park_and_never_overlap()
    {
        var stalls = new Stall[504];
        int placed = StallLayout.Place(30, 24, Standard, stalls);

        int wide = 30 * 400;
        int deep = 24 * 400;

        for (int i = 0; i < placed; i++)
        {
            Stall a = stalls[i];

            Assert.InRange(a.EastCentimetres, 0, wide - a.WideCentimetres);
            Assert.InRange(a.NorthCentimetres, 0, deep - 600 - a.DeepCentimetres);

            for (int j = i + 1; j < placed; j++)
            {
                Stall b = stalls[j];

                bool apart = a.EastCentimetres + a.WideCentimetres <= b.EastCentimetres
                    || b.EastCentimetres + b.WideCentimetres <= a.EastCentimetres
                    || a.NorthCentimetres + a.DeepCentimetres <= b.NorthCentimetres
                    || b.NorthCentimetres + b.DeepCentimetres <= a.NorthCentimetres;

                Assert.True(apart, $"stalls {i} and {j} overlap.");
            }
        }
    }

    [Fact]
    public void Placed_modules_are_centred_across_the_car_park()
    {
        // 120 m holds seven 16 m modules, which leaves 8 m: 4 m on each side.
        var stalls = new Stall[504];
        Assert.Equal(504, StallLayout.Place(30, 24, Standard, stalls));

        int west = stalls.Min(s => s.EastCentimetres);
        int east = stalls.Max(s => s.EastCentimetres + s.WideCentimetres);

        Assert.Equal(400, west);
        Assert.Equal((30 * 400) - 400, east);
    }

    [Fact]
    public void Placing_into_a_short_buffer_is_refused()
    {
        Assert.Throws<ArgumentException>(() => StallLayout.Place(30, 24, Standard, new Stall[10]));
    }
}
