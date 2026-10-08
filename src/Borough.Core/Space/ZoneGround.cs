using Borough.Core.Arithmetic;

namespace Borough.Core.Space;

/// <summary>The ground a <c>Zone</c> command paints: one closed face, or one side of one Street.</summary>
/// <remarks>
/// A face holds the Tiles whose centers lie inside it. A side holds the Tiles whose centers lie
/// within <see cref="ReachQ16"/> of the centerline on that side, between the Segment's ends.
/// Valid only until the next Street edit.
/// </remarks>
[ColdPath("built once per Zone command or shell preview; never held across a Tick.")]
public readonly struct ZoneGround : ITileSet
{
    private readonly RoadFaces? _faces;
    private readonly StreetArc _line;

    private ZoneGround(RoadFaces? faces, int face, int segment, StreetArc line, StreetSide side, int reachQ16, LandRectangle bounds)
    {
        _faces = faces;
        Face = face;
        Segment = segment;
        _line = line;
        Side = side;
        ReachQ16 = reachQ16;
        Bounds = bounds;
    }

    /// <summary>The closed face, or -1 for a Street side.</summary>
    public int Face { get; }

    /// <summary>The Segment slot whose side this is, or <see cref="Tables.Rows.NoSlot"/> for a face.</summary>
    public int Segment { get; }

    public StreetSide Side { get; }

    /// <summary>How far a side reaches from its centerline, in Q16.16 Tiles.</summary>
    public int ReachQ16 { get; }

    public LandRectangle Bounds { get; }

    public static ZoneGround OfFace(RoadFaces faces, int face) =>
        new(faces, face, Tables.Rows.NoSlot, default, default, 0, faces.TileBounds(face));

    public static ZoneGround OfSide(int segment, StreetArc line, StreetSide side, int reachQ16, LandRectangle bounds) =>
        new(null, -1, segment, line, side, reachQ16, bounds);

    public bool Contains(int east, int north)
    {
        long x = Fixed.FromInt(east) + OrientedTiles.HalfTile, y = Fixed.FromInt(north) + OrientedTiles.HalfTile;
        if (_faces is not null) { return _faces.Contains(Face, x, y); }
        return SideOf(_line, x, y, ReachQ16, out StreetSide side, out _) && side == Side;
    }

    /// <summary>Whether a Q16.16 point lies within reach of the centerline, between its ends.</summary>
    /// <param name="side">Left when the point lies left of the A-to-B direction or on the centerline.</param>
    internal static bool SideOf(StreetArc line, long east, long north, int reachQ16, out StreetSide side, out int distance)
    {
        side = StreetSide.Left;
        distance = int.MaxValue;
        int offset = line.OffsetAlong(east, north);
        if (offset <= 0 || offset >= line.Length) { return false; }
        var at = line.PointAt(offset);
        long dx = east - at.East, dy = north - at.North;
        long length = StreetArc.Hypot(dx, dy);
        if (length > reachQ16) { return false; }
        distance = (int)length;
        var (tangentEast, tangentNorth) = line.TangentAt(offset);
        side = (long)tangentEast * dy - (long)tangentNorth * dx < 0 ? StreetSide.Right : StreetSide.Left;
        return true;
    }
}
