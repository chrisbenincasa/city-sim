using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The geometry of a <see cref="BlockPattern.MarketHall"/>: a hall on the south half of the block,
/// its stalls back to back in pairs of columns between aisles, and a market square on the north half.
/// </summary>
/// <remarks>The square is drawn only, and every stall is its own Unit.</remarks>
public static class MarketHall
{
    public const int AisleTiles = 2;

    /// <summary>The width one pair of stall columns and the aisle west of it take.</summary>
    public const int ModuleTiles = AisleTiles + 2;

    /// <summary>How many pairs of stall columns fit, with an aisle west of each and one at the east wall.</summary>
    public static int Pairs(int wide) => wide < ModuleTiles + AisleTiles ? 0 : IntegerMath.FloorDiv(wide - AisleTiles, ModuleTiles);

    /// <summary>Where the west column of a pair stands. The pairs sit centred between aisles.</summary>
    public static int PairEast(int wide, int pair) =>
        AisleTiles + IntegerMath.FloorDiv(wide - AisleTiles - (Pairs(wide) * ModuleTiles), 2) + (pair * ModuleTiles);

    /// <summary>How many stalls one column holds, leaving an aisle across each end of the hall.</summary>
    public static int StallsPerColumn(int deep) => deep < 3 ? 0 : deep - 2;

    public static int Stalls(int wide, int deep) => 2 * Pairs(wide) * StallsPerColumn(deep);

    /// <summary>The hall: the south half of the ground inside the street edges.</summary>
    public static (Tiles East, Tiles North, Tiles Wide, Tiles Deep) Footprint(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles)
    {
        int west = Max(parcel.East.Raw, ground.East + streetHalfWidthTiles);
        int east = Min(parcel.East.Raw + parcel.Wide.Raw, ground.East + ground.Wide - streetHalfWidthTiles);
        int south = Max(parcel.North.Raw, ground.North + streetHalfWidthTiles);
        int top = Min(parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - streetHalfWidthTiles);
        int deep = IntegerMath.FloorDiv(top - south, 2);

        if (east <= west || deep < 1)
        {
            return (Tiles.Zero, Tiles.Zero, Tiles.Zero, Tiles.Zero);
        }

        return (new Tiles(west), new Tiles(south), new Tiles(east - west), new Tiles(deep));
    }

    private static int Max(int a, int b) => a > b ? a : b;

    private static int Min(int a, int b) => a < b ? a : b;
}
