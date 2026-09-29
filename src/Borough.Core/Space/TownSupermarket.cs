using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The geometry of a <see cref="BlockPattern.Supermarket"/>: a single-storey store along the front of
/// one Lot, and a car park between the store and the rear of the block.
/// </summary>
/// <remarks>
/// <para>
/// The store runs east–west along the Lot's south edge and its door faces south, onto the Street.
/// Its car park fills the ground behind it, as wide as the store. A
/// <see cref="BlockPattern.DeckedSupermarket"/> raises that car park into a deck, and every deck level
/// repeats the surface layout.
/// </para>
/// <para>
/// The store depth and deck levels are shape constants of the form. The stall sizes are Ruleset
/// data, in <c>[parking]</c>.
/// </para>
/// </remarks>
public static class TownSupermarket
{
    /// <summary>How deep the store is, in Tiles (40 m), the anchor row depth.</summary>
    public const int StoreDepthTiles = 10;

    /// <summary>How many levels a deck has, counting the ground.</summary>
    public const int DeckLevels = 3;

    /// <summary>How many levels of parking a supermarket form has.</summary>
    public static int Levels(BlockPattern pattern) =>
        pattern == BlockPattern.DeckedSupermarket ? DeckLevels : 1;

    /// <summary>
    /// The store's rectangle: the parcel's front strip, inside the block's street edges.
    /// </summary>
    public static (Tiles East, Tiles North, Tiles Wide, Tiles Deep) Footprint(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles)
    {
        int west = Max(parcel.East.Raw, ground.East + streetHalfWidthTiles);
        int east = Min(parcel.East.Raw + parcel.Wide.Raw, ground.East + ground.Wide - streetHalfWidthTiles);
        int south = Max(parcel.North.Raw, ground.North + streetHalfWidthTiles);
        int top = Min(parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - streetHalfWidthTiles);
        int deep = Min(StoreDepthTiles, top - south);

        if (east <= west || deep < 1)
        {
            return (Tiles.Zero, Tiles.Zero, Tiles.Zero, Tiles.Zero);
        }

        return (new Tiles(west), new Tiles(south), new Tiles(east - west), new Tiles(deep));
    }

    /// <summary>
    /// The car park's rectangle in Tiles: as wide as the store, from the store's back up to the rear
    /// street edge of the parcel.
    /// </summary>
    public static (int East, int North, int Along, int Toward) CarPark(
        int parcelNorth, int parcelDeep, int footprintEast, int footprintNorth, int footprintWide,
        int footprintDeep, int streetHalfWidthTiles)
    {
        int south = footprintNorth + footprintDeep;
        int top = parcelNorth + parcelDeep - streetHalfWidthTiles;

        return (footprintEast, south, footprintWide, top - south);
    }

    /// <summary>The stalls on one level of the car park.</summary>
    public static StallLayout Stalls(
        int parcelNorth, int parcelDeep, int footprintNorth, int footprintWide, int footprintDeep,
        int streetHalfWidthTiles, StallSizes sizes)
    {
        (_, _, int along, int toward) = CarPark(
            parcelNorth, parcelDeep, 0, footprintNorth, footprintWide, footprintDeep, streetHalfWidthTiles);

        return StallLayout.Of(along, toward, sizes);
    }

    private static int Max(int a, int b) => a > b ? a : b;

    private static int Min(int a, int b) => a < b ? a : b;
}
