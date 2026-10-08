using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>One plot cut along a Segment side.</summary>
/// <param name="Offset">Where the plot starts along the Segment, from its A end, in whole Tiles.</param>
/// <param name="Geometry">The plot's ground. Its axis runs along the front edge.</param>
public readonly record struct SidePlot(Tiles Offset, OrientedRectangle Geometry);

/// <summary>Cuts a row of rectangular plots along one side of a straight or curved Segment.</summary>
/// <remarks>
/// The Segment's length divides into equal spans no shorter than the plot width. Each plot's front
/// edge is the chord across its span, set back from the centerline by the street half-width, and its
/// depth runs away from the Street. On the outside of a curve the plots fan apart and leave wedges.
/// On the inside they converge, and the claim rule decides the overlaps.
/// </remarks>
public static class SegmentSide
{
    /// <summary>How many plots <see cref="Cut"/> yields for this Segment and width.</summary>
    public static int Count(StreetArc line, int widthTiles) =>
        widthTiles <= 0 ? 0 : (int)IntegerMath.FloorDiv(line.Length, (long)widthTiles * Fixed.One);

    /// <summary>Cuts plots along one side, in order of increasing offset.</summary>
    /// <param name="from">Where the cut starts along the Segment, in Q16.16 Tiles.</param>
    /// <param name="to">Where the cut ends, or a negative value for the Segment's B end.</param>
    /// <returns>How many plots were written.</returns>
    public static int Cut(StreetArc line, StreetSide side, int widthTiles, int depthTiles, int halfWidthTiles,
        Span<SidePlot> into, int from = 0, int to = -1)
    {
        if (to < 0) { to = line.Length; }
        int span = to - from;
        int count = widthTiles <= 0 || span <= 0 ? 0 : (int)IntegerMath.FloorDiv(span, (long)widthTiles * Fixed.One);
        if (count == 0 || depthTiles <= 0) { return 0; }
        if (into.Length < count) { throw new ArgumentException("The plot buffer is smaller than Count.", nameof(into)); }

        int written = 0;
        for (int k = 0; k < count; k++)
        {
            int start = from + (int)IntegerMath.FloorDiv((long)span * k, count);
            int end = from + (int)IntegerMath.FloorDiv((long)span * (k + 1), count);
            if (Plot(line, side, start, end, depthTiles, halfWidthTiles, out OrientedRectangle ground))
            {
                into[written++] = new SidePlot(new Tiles((int)IntegerMath.FloorDiv(start, Fixed.One)), ground);
            }
        }

        return written;
    }

    /// <summary>The plot whose front edge is the chord between two offsets, set back by the half-width.</summary>
    public static bool Plot(StreetArc line, StreetSide side, int start, int end, int depthTiles, int halfWidthTiles,
        out OrientedRectangle ground)
    {
        ground = default;
        var (from, to) = side == StreetSide.Left
            ? (line.PointAt(start), line.PointAt(end))
            : (line.PointAt(end), line.PointAt(start));
        long east = to.East - from.East, north = to.North - from.North;
        long chord = StreetArc.Hypot(east, north);
        int wide = (int)IntegerMath.FloorDiv(chord, Fixed.One);
        if (wide <= 0) { return false; }

        int axisEast = (int)IntegerMath.FloorDiv(east * Fixed.One, chord);
        int axisNorth = (int)IntegerMath.FloorDiv(north * Fixed.One, chord);
        int cornerEast = (int)(from.East - (long)axisNorth * halfWidthTiles);
        int cornerNorth = (int)(from.North + (long)axisEast * halfWidthTiles);
        ground = new OrientedRectangle(cornerEast, cornerNorth, axisEast, axisNorth, wide, depthTiles);
        return true;
    }
}
