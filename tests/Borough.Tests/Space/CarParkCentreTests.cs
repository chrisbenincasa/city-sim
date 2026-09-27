using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

public sealed class CarParkCentreTests
{
    private const int BlockTiles = 32;
    private const int StreetHalfWidth = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x0C3A_7C3A_0000_0001UL);
    private static readonly StallSizes Standard = new(250, 500, 600);

    private static Parcel WholeBlock()
    {
        Span<Parcel> parcels = stackalloc Parcel[BlockPatterns.Ceiling(5)];
        int count = BlockPatterns.Carve(Key, BlockPattern.CarParkCentre, BlockTiles, 5, parcels);

        Assert.Equal(1, count);
        return parcels[0];
    }

    [Fact]
    public void A_centre_carves_its_block_as_one_parcel_facing_south()
    {
        Parcel parcel = WholeBlock();

        Assert.Equal(BlockFace.South, parcel.Face);
        Assert.Equal(new Tiles(0), parcel.East);
        Assert.Equal(new Tiles(0), parcel.North);
        Assert.Equal(new Tiles(BlockTiles), parcel.Wide);
        Assert.Equal(new Tiles(BlockTiles), parcel.Deep);
        Assert.True(BlockPatterns.Exhaustive(BlockPattern.CarParkCentre));
    }

    [Fact]
    public void The_unit_row_stands_at_the_rear_across_the_whole_frontage()
    {
        var foot = CarParkCentre.Footprint(WholeBlock(), BlockGround.Square(BlockTiles), StreetHalfWidth);

        Assert.Equal(new Tiles(1), foot.East);
        Assert.Equal(new Tiles(BlockTiles - 1 - CarParkCentre.RowDepthTiles), foot.North);
        Assert.Equal(new Tiles(BlockTiles - 2), foot.Wide);
        Assert.Equal(new Tiles(CarParkCentre.RowDepthTiles), foot.Deep);
    }

    [Fact]
    public void The_car_park_fills_the_ground_between_the_row_and_the_street()
    {
        var foot = CarParkCentre.Footprint(WholeBlock(), BlockGround.Square(BlockTiles), StreetHalfWidth);

        StallLayout layout = CarParkCentre.Stalls(
            parcelNorth: 0, foot.North.Raw, foot.Wide.Raw, StreetHalfWidth, Standard);

        Assert.Equal(new StallLayout(7, 36), layout);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(0xDEAD_BEEFUL)]
    public void The_row_has_a_half_width_anchor_at_one_end_and_mixed_small_units(ulong draw)
    {
        Span<int> widths = stackalloc int[CarParkCentre.UnitCount(30)];
        int count = CarParkCentre.UnitWidths(30, draw, widths);
        int anchor = CarParkCentre.AnchorIndex(count, draw);

        Assert.Equal(CarParkCentre.UnitCount(30), count);
        Assert.True(anchor == 0 || anchor == count - 1);
        Assert.Equal(14, widths[anchor]);

        int total = 0;
        var small = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            total += widths[i];
            Assert.Equal(0, widths[i] % CarParkCentre.BayTiles);

            if (i != anchor)
            {
                small.Add(widths[i]);
            }
        }

        Assert.Equal(30, total);
        Assert.True(small.Count > 1, "the small Units should not all be one width");
    }

    [Fact]
    public void Which_end_the_anchor_takes_follows_the_draw()
    {
        Assert.Equal(0, CarParkCentre.AnchorIndex(6, 0UL));
        Assert.Equal(5, CarParkCentre.AnchorIndex(6, 1UL));
    }

    [Fact]
    public void A_row_too_narrow_to_split_is_one_unit()
    {
        Span<int> widths = stackalloc int[CarParkCentre.UnitCount(3)];

        Assert.Equal(1, CarParkCentre.UnitWidths(3, 0UL, widths));
        Assert.Equal(3, widths[0]);
    }

    [Fact]
    public void A_centre_is_one_storey_and_never_hollowed()
    {
        Assert.Equal(30 * 6, BuildingPlan.FloorTiles(BlockPattern.CarParkCentre, 30, 6, 1));
        Assert.Equal(30 * 30, BuildingPlan.FloorTiles(BlockPattern.CarParkCentre, 30, 30, 1));
    }
}
