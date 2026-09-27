using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The stall sizes a surface car park is laid out with, in centimetres.
/// </summary>
/// <remarks>
/// Real-world dimensions rather than Tiles, because a stall is 2.5 m wide and a Tile is 4 m.
/// A Ruleset without them lays out no stalls.
/// </remarks>
public readonly record struct StallSizes(
    int WidthCentimetres, int LengthCentimetres, int AisleCentimetres)
{
    /// <summary>Sizes that lay out nothing.</summary>
    public static StallSizes None => default;

    /// <summary>Whether every size is stated.</summary>
    public bool Runs => WidthCentimetres > 0 && LengthCentimetres > 0 && AisleCentimetres > 0;
}

/// <summary>
/// The stalls of a surface car park laid between a Unit row and its street.
/// </summary>
/// <remarks>
/// <para>
/// Aisles run perpendicular to the Unit row, each with a column of stalls on either side, and one
/// cross aisle runs along the row's front. A module is one aisle and its two stall columns. Across
/// the row there are as many whole modules as fit; along each aisle there are as many stalls as fit
/// after the cross aisle.
/// </para>
/// <para>
/// The capacity of the Building's Car Park is <see cref="Stalls"/>, and the drawing paints the same
/// layout.
/// </para>
/// </remarks>
public readonly record struct StallLayout(int Modules, int StallsPerColumn)
{
    private const int CentimetresPerTile = Tiles.Metres * 100;

    /// <summary>How many stalls the layout holds.</summary>
    public int Stalls => Modules * 2 * StallsPerColumn;

    /// <summary>
    /// Lays out a car park <paramref name="alongTiles"/> wide parallel to the Unit row and
    /// <paramref name="towardTiles"/> deep from the row to the street.
    /// </summary>
    public static StallLayout Of(int alongTiles, int towardTiles, StallSizes sizes)
    {
        if (!sizes.Runs || alongTiles < 1 || towardTiles < 1)
        {
            return default;
        }

        int module = (2 * sizes.LengthCentimetres) + sizes.AisleCentimetres;
        int modules = IntegerMath.FloorDiv(alongTiles * CentimetresPerTile, module);

        int aisleLength = (towardTiles * CentimetresPerTile) - sizes.AisleCentimetres;
        int perColumn = aisleLength > 0
            ? IntegerMath.FloorDiv(aisleLength, sizes.WidthCentimetres)
            : 0;

        return modules < 1 || perColumn < 1 ? default : new StallLayout(modules, perColumn);
    }
}
