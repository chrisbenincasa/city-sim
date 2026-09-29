using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

public sealed class TownSupermarketTests
{
    private const int BlockTiles = 32;
    private const int StreetHalfWidth = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x5A9E_7A2C_0000_0001UL);
    private static readonly StallSizes Standard = new(250, 500, 600);

    private static Parcel WholeBlock(BlockPattern form)
    {
        Span<Parcel> parcels = stackalloc Parcel[BlockPatterns.Ceiling(5)];
        int count = BlockPatterns.Carve(Key, form, BlockTiles, 5, parcels);

        Assert.Equal(1, count);
        Assert.Equal(BlockFace.South, parcels[0].Face);
        return parcels[0];
    }

    [Theory]
    [InlineData(BlockPattern.Supermarket)]
    [InlineData(BlockPattern.DeckedSupermarket)]
    public void The_store_stands_at_the_front_across_the_whole_frontage(BlockPattern form)
    {
        var foot = TownSupermarket.Footprint(WholeBlock(form), BlockGround.Square(BlockTiles), StreetHalfWidth);

        Assert.Equal(new Tiles(1), foot.East);
        Assert.Equal(new Tiles(1), foot.North);
        Assert.Equal(new Tiles(BlockTiles - 2), foot.Wide);
        Assert.Equal(new Tiles(TownSupermarket.StoreDepthTiles), foot.Deep);
    }

    [Fact]
    public void The_car_park_fills_the_ground_between_the_store_and_the_rear_street()
    {
        Parcel parcel = WholeBlock(BlockPattern.Supermarket);
        var foot = TownSupermarket.Footprint(parcel, BlockGround.Square(BlockTiles), StreetHalfWidth);

        (int east, int north, int along, int toward) = TownSupermarket.CarPark(
            parcel.North.Raw, parcel.Deep.Raw, foot.East.Raw, foot.North.Raw, foot.Wide.Raw, foot.Deep.Raw,
            StreetHalfWidth);

        Assert.Equal((1, 11, 30, 20), (east, north, along, toward));

        StallLayout level = TownSupermarket.Stalls(
            parcel.North.Raw, parcel.Deep.Raw, foot.North.Raw, foot.Wide.Raw, foot.Deep.Raw, StreetHalfWidth,
            Standard);

        Assert.Equal(new StallLayout(7, 29), level);
    }

    [Fact]
    public void Only_a_decked_supermarket_parks_on_more_than_one_level()
    {
        Assert.Equal(1, TownSupermarket.Levels(BlockPattern.Supermarket));
        Assert.Equal(TownSupermarket.DeckLevels, TownSupermarket.Levels(BlockPattern.DeckedSupermarket));
    }

    [Theory]
    [InlineData(BlockPattern.Supermarket)]
    [InlineData(BlockPattern.DeckedSupermarket)]
    public void A_store_is_one_solid_storey(BlockPattern form)
    {
        Assert.False(BuildingPlan.Hollow(form, 30, 10, out _, out _));
        Assert.Equal(30 * 10, BuildingPlan.FloorTiles(form, 30, 10, 1));
    }
}
