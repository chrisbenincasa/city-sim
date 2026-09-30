using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The four Lots a <see cref="BlockPattern.SalesYard"/> block carves, and the ground each lays out.
/// </summary>
/// <remarks>
/// A Lot runs back from its street: a stall band across its whole width, then the shed at one side
/// with the yard beside it. The drawing fills the yard by family, so the simulation never says
/// whether it holds cars, plants or timber. The yard is derived and never saved.
/// </remarks>
public static class SalesYard
{
    public const int Lots = 4;

    public const int StallBandTiles = 4;

    public const int ShedWideTiles = 6;

    public const int ShedDeepTiles = 10;

    /// <summary>Quarters the block: two Lots on the south face, then two on the north.</summary>
    public static int Carve(BlockGround ground, Span<Parcel> into)
    {
        int west = IntegerMath.FloorDiv(ground.Wide, 2);
        int south = IntegerMath.FloorDiv(ground.Deep, 2);
        int east = ground.Wide - west;
        int north = ground.Deep - south;

        into[0] = Quarter(BlockFace.South, ground, 0, 0, west, south);
        into[1] = Quarter(BlockFace.South, ground, west, 0, east, south);
        into[2] = Quarter(BlockFace.North, ground, 0, south, west, north);
        into[3] = Quarter(BlockFace.North, ground, west, south, east, north);

        return Lots;
    }

    private static Parcel Quarter(BlockFace face, BlockGround ground, int east, int north, int wide, int deep) =>
        new(face, BlockPatterns.SideOf(face), new Tiles(east + IntegerMath.FloorDiv(wide, 2)),
            new Tiles(ground.East + east), new Tiles(ground.North + north), new Tiles(wide), new Tiles(deep));

    /// <summary>The shed: behind the stall band, against the west or east edge of the Lot.</summary>
    public static (Tiles East, Tiles North, Tiles Wide, Tiles Deep) Footprint(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles, bool eastSide)
    {
        (int west, int south, int eastEdge, int top) = Net(parcel, ground, streetHalfWidthTiles);
        int wide = Min(ShedWideTiles, eastEdge - west);
        int behind = top - south - StallBandTiles;
        int deep = Min(ShedDeepTiles, behind);

        if (wide < 1 || deep < 1)
        {
            return (Tiles.Zero, Tiles.Zero, Tiles.Zero, Tiles.Zero);
        }

        int north = parcel.Face == BlockFace.North ? top - StallBandTiles - deep : south + StallBandTiles;

        return (new Tiles(eastSide ? eastEdge - wide : west), new Tiles(north), new Tiles(wide), new Tiles(deep));
    }

    /// <summary>
    /// The stall band along the street, in the frame <see cref="StallLayout"/> places stalls in.
    /// </summary>
    public static (int East, int North, int Along, int Toward) CarPark(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles)
    {
        (int west, int south, int eastEdge, int top) = Net(parcel, ground, streetHalfWidthTiles);
        int north = parcel.Face == BlockFace.North ? top - StallBandTiles : south;

        return (west, north, eastEdge - west, StallBandTiles);
    }

    /// <summary>The yard: behind the stall band, beside the shed.</summary>
    public static (int East, int North, int Wide, int Deep) Yard(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles, int shedEast, int shedWide)
    {
        (int west, int south, int eastEdge, int top) = Net(parcel, ground, streetHalfWidthTiles);
        int north = parcel.Face == BlockFace.North ? south : south + StallBandTiles;
        int east = shedEast > west ? west : shedEast + shedWide;
        int end = shedEast > west ? shedEast : eastEdge;

        return (east, north, end - east, top - south - StallBandTiles);
    }

    public static StallLayout Stalls(Parcel parcel, BlockGround ground, int streetHalfWidthTiles, StallSizes sizes)
    {
        (_, _, int along, int toward) = CarPark(parcel, ground, streetHalfWidthTiles);

        return StallLayout.Of(along, toward, sizes);
    }

    private static (int West, int South, int East, int Top) Net(Parcel parcel, BlockGround ground, int half) =>
        (Max(parcel.East.Raw, ground.East + half),
            Max(parcel.North.Raw, ground.North + half),
            Min(parcel.East.Raw + parcel.Wide.Raw, ground.East + ground.Wide - half),
            Min(parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - half));

    private static int Max(int a, int b) => a > b ? a : b;

    private static int Min(int a, int b) => a < b ? a : b;
}
