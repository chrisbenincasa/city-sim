using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>Seals the ground under a laid Street, one Tile per Tile of its length (<c>adr/0151</c>).</summary>
public static class StreetSealing
{
    /// <summary>Walks the centerline a Tile at a time and seals each Tile into the Cell it stands in.</summary>
    public static void Seal(MapLayers layers, StreetArc line)
    {
        ArgumentNullException.ThrowIfNull(layers);
        int steps = (int)IntegerMath.FloorDiv((long)line.Length + Fixed.One - 1, Fixed.One);
        if (steps <= 0) { return; }

        (Cells East, Cells North) run = CellAt(line, 0);
        int count = 0;
        for (int step = 0; step < steps; step++)
        {
            var cell = CellAt(line, step);
            if (cell != run)
            {
                layers.Seal(run.East, run.North, count);
                run = cell;
                count = 0;
            }

            count++;
        }

        layers.Seal(run.East, run.North, count);
    }

    private static (Cells East, Cells North) CellAt(StreetArc line, int step)
    {
        var point = line.PointAt(step * Fixed.One);
        return (CellGrid.ToCellsClamped(new Tiles((int)IntegerMath.ShiftRight(point.East, Fixed.FractionalBits))),
            CellGrid.ToCellsClamped(new Tiles((int)IntegerMath.ShiftRight(point.North, Fixed.FractionalBits))));
    }
}
