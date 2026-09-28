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

    /// <summary>
    /// Writes every stall of <see cref="Of"/>'s layout into <paramref name="stalls"/>, and returns
    /// how many it wrote.
    /// </summary>
    /// <remarks>
    /// Rectangles are in centimetres from the car park's south-west corner, where south is the
    /// street and north is the Unit row. The cross aisle lies against the row, and the stalls run
    /// south from it. The modules are centred across the car park, so any width left over is split
    /// between its two sides. Leftover depth lies along the street.
    /// </remarks>
    public static int Place(int alongTiles, int towardTiles, StallSizes sizes, Span<Stall> stalls)
    {
        StallLayout layout = Of(alongTiles, towardTiles, sizes);

        if (stalls.Length < layout.Stalls)
        {
            throw new ArgumentException(
                $"{layout.Stalls} stalls need placing and the buffer holds {stalls.Length}.", nameof(stalls));
        }

        int length = sizes.LengthCentimetres;
        int width = sizes.WidthCentimetres;
        int module = (2 * length) + sizes.AisleCentimetres;
        int slack = (alongTiles * CentimetresPerTile) - (layout.Modules * module);
        int west = IntegerMath.FloorDiv(slack, 2);
        int top = (towardTiles * CentimetresPerTile) - sizes.AisleCentimetres;
        int written = 0;

        for (int m = 0; m < layout.Modules; m++)
        {
            int moduleWest = west + (m * module);

            for (int side = 0; side < 2; side++)
            {
                int east = side == 0 ? moduleWest : moduleWest + length + sizes.AisleCentimetres;

                for (int s = 0; s < layout.StallsPerColumn; s++)
                {
                    stalls[written++] = new Stall(east, top - ((s + 1) * width), length, width);
                }
            }
        }

        return written;
    }
}

/// <summary>
/// One parking stall's rectangle, in centimetres.
/// </summary>
public readonly record struct Stall(
    int EastCentimetres, int NorthCentimetres, int WideCentimetres, int DeepCentimetres);
