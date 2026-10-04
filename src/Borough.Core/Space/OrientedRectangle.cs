using Borough.Core.Arithmetic;

namespace Borough.Core.Space;

/// <summary>A saved Q16.16 corner and unit axis, with whole-Tile extents. The second axis turns left 90 degrees.</summary>
public readonly record struct OrientedRectangle(
    int EastQ16, int NorthQ16, int AxisEastQ16, int AxisNorthQ16, int Wide, int Deep)
{
    public static OrientedRectangle FromBounds(LandRectangle bounds) => new(
        Fixed.FromInt(bounds.X), Fixed.FromInt(bounds.Y), Fixed.One, 0, bounds.Width, bounds.Height);

    public bool IsValid => Wide > 0 && Deep > 0 && Wide <= CellGrid.WorldTiles && Deep <= CellGrid.WorldTiles
        && AxisEastQ16 >= -Fixed.One && AxisEastQ16 <= Fixed.One
        && AxisNorthQ16 >= -Fixed.One && AxisNorthQ16 <= Fixed.One
        && (AxisEastQ16 != 0 || AxisNorthQ16 != 0) && Bounds.IsValid;

    /// <summary>The integer box enclosing all four corners, rounded outward.</summary>
    public LandRectangle Bounds
    {
        get
        {
            Extents(AxisEastQ16, -AxisNorthQ16, out long x0, out long x1);
            Extents(AxisNorthQ16, AxisEastQ16, out long y0, out long y1);
            int x = (int)IntegerMath.FloorDiv(EastQ16 + x0, Fixed.One);
            int y = (int)IntegerMath.FloorDiv(NorthQ16 + y0, Fixed.One);
            return new(x, y, (int)IntegerMath.CeilDiv(EastQ16 + x1, Fixed.One) - x,
                (int)IntegerMath.CeilDiv(NorthQ16 + y1, Fixed.One) - y);
        }
    }

    /// <summary>The center in Q16.16 Tiles, rounded toward negative infinity.</summary>
    public (int East, int North) Center => (
        (int)(EastQ16 + IntegerMath.FloorDiv((long)Wide * AxisEastQ16 - (long)Deep * AxisNorthQ16, 2)),
        (int)(NorthQ16 + IntegerMath.FloorDiv((long)Wide * AxisNorthQ16 + (long)Deep * AxisEastQ16, 2)));

    /// <summary>Includes the near edges and excludes the far edges on both saved axes.</summary>
    public bool Contains(int eastQ16, int northQ16)
    {
        if (!IsValid) { return false; }
        long x = (long)eastQ16 - EastQ16, y = (long)northQ16 - NorthQ16;
        long s = x * AxisEastQ16 + y * AxisNorthQ16;
        long t = -x * AxisNorthQ16 + y * AxisEastQ16;
        long lengthSquared = (long)AxisEastQ16 * AxisEastQ16 + (long)AxisNorthQ16 * AxisNorthQ16;
        return s >= 0 && s < Wide * lengthSquared && t >= 0 && t < Deep * lengthSquared;
    }

    /// <summary>Contains the entire other rectangle, including coincident boundaries.</summary>
    public bool Contains(OrientedRectangle other) => IsValid && other.IsValid
        && EnclosesProjection(other, AxisEastQ16, AxisNorthQ16)
        && EnclosesProjection(other, -AxisNorthQ16, AxisEastQ16);

    /// <summary>Tests interiors with the separating axes of both rectangles. Shared edges do not overlap.</summary>
    public bool Overlaps(OrientedRectangle other) => IsValid && other.IsValid
        && IntersectsProjection(other, AxisEastQ16, AxisNorthQ16)
        && IntersectsProjection(other, -AxisNorthQ16, AxisEastQ16)
        && IntersectsProjection(other, other.AxisEastQ16, other.AxisNorthQ16)
        && IntersectsProjection(other, -other.AxisNorthQ16, other.AxisEastQ16);

    private bool IntersectsProjection(OrientedRectangle other, int east, int north)
    {
        Project(east, north, out long low, out long high);
        other.Project(east, north, out long otherLow, out long otherHigh);
        return low < otherHigh && otherLow < high;
    }

    private bool EnclosesProjection(OrientedRectangle other, int east, int north)
    {
        Project(east, north, out long low, out long high);
        other.Project(east, north, out long otherLow, out long otherHigh);
        return low <= otherLow && otherHigh <= high;
    }

    private void Project(int east, int north, out long low, out long high)
    {
        // WorldCells * TilesPerCell = 16384 = 2^14. Q16 corners are at most 2^30;
        // axis components are at most 2^16. Each dot is at most 2^47 and each
        // extent contribution at most 2^47, so the unrounded projection sum fits below 2^49.
        long corner = (long)EastQ16 * east + (long)NorthQ16 * north;
        Extents((long)AxisEastQ16 * east + (long)AxisNorthQ16 * north,
            -(long)AxisNorthQ16 * east + (long)AxisEastQ16 * north, out low, out high);
        low += corner;
        high += corner;
    }

    private void Extents(long a, long b, out long low, out long high)
    {
        long s = Wide * a, t = Deep * b;
        low = (s < 0 ? s : 0) + (t < 0 ? t : 0);
        high = (s > 0 ? s : 0) + (t > 0 ? t : 0);
    }
}
