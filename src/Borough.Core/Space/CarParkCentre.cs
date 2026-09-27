using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The geometry of a <see cref="BlockPattern.CarParkCentre"/>: a single-storey row of Units along the
/// rear of one Lot, and a surface car park between the row and the street.
/// </summary>
/// <remarks>
/// <para>
/// The row runs east–west along the Lot's north edge and its doors face south, toward the car park
/// and the street. One Unit is the anchor and takes about half the row at one end; the rest are small
/// Units of mixed widths in whole bays.
/// </para>
/// <para>
/// The row depth and bay are shape constants of the form, like a courtyard's third. The stall sizes
/// that fill the car park are Ruleset data, in <c>[parking]</c>.
/// </para>
/// </remarks>
public static class CarParkCentre
{
    /// <summary>How deep the Unit row is, in Tiles (24 m).</summary>
    public const int RowDepthTiles = 6;

    /// <summary>The structural bay every Unit width is a whole number of, in Tiles (8 m).</summary>
    public const int BayTiles = 2;

    /// <summary>The mean width of a small Unit, in Tiles.</summary>
    private const int SmallUnitTiles = 3;

    /// <summary>
    /// The Unit row's rectangle: the parcel's rear strip, inside the block's street edges.
    /// </summary>
    public static (Tiles East, Tiles North, Tiles Wide, Tiles Deep) Footprint(
        Parcel parcel, BlockGround ground, int streetHalfWidthTiles)
    {
        int west = Max(parcel.East.Raw, ground.East + streetHalfWidthTiles);
        int east = Min(parcel.East.Raw + parcel.Wide.Raw, ground.East + ground.Wide - streetHalfWidthTiles);
        int south = Max(parcel.North.Raw, ground.North + streetHalfWidthTiles);
        int top = Min(parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - streetHalfWidthTiles);
        int deep = Min(RowDepthTiles, top - south);

        if (east <= west || deep < 1)
        {
            return (Tiles.Zero, Tiles.Zero, Tiles.Zero, Tiles.Zero);
        }

        return (new Tiles(west), new Tiles(top - deep), new Tiles(east - west), new Tiles(deep));
    }

    /// <summary>
    /// The stalls between the Unit row's front and the street edge of the parcel.
    /// </summary>
    public static StallLayout Stalls(
        int parcelNorth, int footprintNorth, int footprintWide, int streetHalfWidthTiles, StallSizes sizes) =>
        StallLayout.Of(footprintWide, footprintNorth - (parcelNorth + streetHalfWidthTiles), sizes);

    /// <summary>How many Units a row <paramref name="wide"/> Tiles long holds.</summary>
    public static int UnitCount(int wide)
    {
        if (wide < 1)
        {
            return 0;
        }

        int rest = wide - Anchor(wide);

        return Anchor(wide) < BayTiles || rest < BayTiles ? 1 : SmallGroups(rest) + 1;
    }

    /// <summary>
    /// Divides a row <paramref name="wide"/> Tiles long into Unit widths, west to east.
    /// </summary>
    /// <returns>How many widths were written, which is <see cref="UnitCount"/>.</returns>
    public static int UnitWidths(int wide, ulong draw, Span<int> into)
    {
        int count = UnitCount(wide);

        if (count <= 1)
        {
            if (count == 1)
            {
                into[0] = wide;
            }

            return count;
        }

        int anchor = AnchorIndex(count, draw);
        int small = count - 1;

        BlockPatterns.Widths(draw >> 1, BayTiles, wide - Anchor(wide), small, into.Slice(anchor == 0 ? 1 : 0, small));
        into[anchor] = Anchor(wide);

        return count;
    }

    /// <summary>Which of <paramref name="count"/> Units is the anchor.</summary>
    public static int AnchorIndex(int count, ulong draw) =>
        count < 2 || (draw & 1) == 0 ? 0 : count - 1;

    private static int Anchor(int wide) => BayTiles * IntegerMath.FloorDiv(wide, 2 * BayTiles);

    private static int SmallGroups(int rest)
    {
        int groups = IntegerMath.FloorDiv(rest, SmallUnitTiles);

        return groups < 1 ? 1 : groups;
    }

    private static int Max(int a, int b) => a > b ? a : b;

    private static int Min(int a, int b) => a < b ? a : b;
}
