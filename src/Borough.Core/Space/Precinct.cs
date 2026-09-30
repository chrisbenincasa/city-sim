using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The geometry of a <see cref="BlockPattern.Precinct"/>: rows of shop Units either side of walkways
/// that run back from the south street, and a deck behind them.
/// </summary>
/// <remarks>
/// Every storey repeats the ground's layout. The walkways are drawn only; Citizens reach every Unit
/// through the Lot's street address. The deck reuses <see cref="TownSupermarket.CarPark"/>.
/// </remarks>
public static class Precinct
{
    public const int RowTiles = 6;

    public const int WalkwayTiles = 2;

    /// <summary>How far each Unit runs along its row, in Tiles (8 m).</summary>
    public const int UnitTiles = 2;

    public const int DeckTiles = 8;

    /// <summary>The width one walkway and its two rows take.</summary>
    public const int ModuleTiles = (2 * RowTiles) + WalkwayTiles;

    /// <summary>One row of Units, in Tiles east of the footprint's west wall, and the side it faces.</summary>
    public readonly record struct Row(int East, int Wide, BlockFace Face);

    public static int Storeys(BlockPattern pattern) => pattern == BlockPattern.GalleryPrecinct ? 2 : 1;

    public static int Walkways(int wide) => wide < ModuleTiles ? 1 : IntegerMath.FloorDiv(wide, ModuleTiles);

    public static int RowCount(int wide) => 2 * Walkways(wide);

    /// <summary>
    /// Writes the rows west to east and returns how many. The outer rows take the width the modules
    /// leave over, and a footprint too narrow for two rows and a walkway has none.
    /// </summary>
    public static int Rows(int wide, Span<Row> into)
    {
        int walkways = Walkways(wide);
        int spare = wide - (walkways * ModuleTiles);
        int first = RowTiles + IntegerMath.FloorDiv(spare, 2);
        int last = RowTiles + spare - IntegerMath.FloorDiv(spare, 2);

        if (first < 1 || last < 1)
        {
            return 0;
        }

        int east = 0;

        for (int walkway = 0; walkway < walkways; walkway++)
        {
            int west = walkway == 0 ? first : RowTiles;
            int beyond = walkway == walkways - 1 ? last : RowTiles;

            into[2 * walkway] = new Row(east, west, BlockFace.East);
            east += west + WalkwayTiles;
            into[(2 * walkway) + 1] = new Row(east, beyond, BlockFace.West);
            east += beyond;
        }

        return 2 * walkways;
    }

    /// <summary>How many Units one row holds. The northmost takes any odd Tile.</summary>
    public static int UnitsPerRow(int deep) => deep < UnitTiles ? 1 : IntegerMath.FloorDiv(deep, UnitTiles);

    /// <summary>How many Units the precinct holds over every storey.</summary>
    public static int Units(int wide, int deep, int storeys)
    {
        Span<Row> rows = stackalloc Row[RowCount(wide)];

        return Rows(wide, rows) * UnitsPerRow(deep) * storeys;
    }

    /// <summary>The shop rows and walkways: the ground in front of the deck, inside the street edges.</summary>
    public static (Tiles East, Tiles North, Tiles Wide, Tiles Deep) Footprint(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles)
    {
        int west = Max(parcel.East.Raw, ground.East + streetHalfWidthTiles);
        int east = Min(parcel.East.Raw + parcel.Wide.Raw, ground.East + ground.Wide - streetHalfWidthTiles);
        int south = Max(parcel.North.Raw, ground.North + streetHalfWidthTiles);
        int top = Min(parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - streetHalfWidthTiles);
        int deep = top - south - DeckTiles;

        if (east <= west || deep < 1)
        {
            return (Tiles.Zero, Tiles.Zero, Tiles.Zero, Tiles.Zero);
        }

        return (new Tiles(west), new Tiles(south), new Tiles(east - west), new Tiles(deep));
    }

    private static int Max(int a, int b) => a > b ? a : b;

    private static int Min(int a, int b) => a < b ? a : b;
}
