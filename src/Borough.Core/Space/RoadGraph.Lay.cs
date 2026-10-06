using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>Why a freeform Street lay was refused. <see cref="None"/> means it was laid.</summary>
public enum StreetLayRefusal : byte
{
    None,

    /// <summary>The world has no roads, or an end lies off the map.</summary>
    OffMap,

    /// <summary>The ends coincide, or the sagitta bends the Street past a quarter turn.</summary>
    NotAnArc,

    /// <summary>The curve is tighter than <c>[roads] min_curve_radius_tiles</c>.</summary>
    TooTight,

    /// <summary>A Segment the lay or its splits would leave is shorter than <c>[roads] min_segment_length_tiles</c>.</summary>
    TooShort,

    /// <summary>The Street meets a Segment more shallowly than <c>[roads] min_crossing_angle_degrees</c>.</summary>
    TooShallow,

    /// <summary>A split or a piece of the new Street would move the road past the split bound.</summary>
    MovesRoad,
}

/// <summary>A Segment split by a lay. Lots past <paramref name="Retained"/> along the original move to the created Segment.</summary>
/// <param name="Original">The slot that keeps the part before the cut.</param>
/// <param name="Created">The slot holding the part after the cut.</param>
/// <param name="Retained">The cut's offset along <paramref name="Original"/>, in whole Tiles.</param>
public readonly record struct SegmentSplit(int Original, int Created, Tiles Retained);

public sealed partial class RoadGraph
{
    private readonly record struct Join(int East, int North, int Offset, int Segment, int OtherOffset);

    private readonly record struct Piece(int FromEast, int FromNorth, int ToEast, int ToNorth, int Sagitta, int Length);

    /// <summary>
    /// Lays a Street between two Tiles with a signed Q16.16 sagitta, joining and splitting the
    /// Segments it meets.
    /// </summary>
    /// <remarks>
    /// An end joins an existing Node only on an exact Tile match. A crossing, or an end within half
    /// a Tile of a Segment, adds a Node at the nearest Tile and splits that Segment there. Every
    /// check runs before the graph changes, so a refused lay changes nothing. Splits are reported
    /// in order, so applying them in turn moves frontage correctly when one Segment is cut twice.
    /// </remarks>
    public StreetLayRefusal LayStreet(int aEast, int aNorth, int bEast, int bNorth, int sagitta,
        List<SegmentSplit> splits, List<int> laid)
    {
        ArgumentNullException.ThrowIfNull(splits);
        ArgumentNullException.ThrowIfNull(laid);
        if (!_ruleset.Runs || !OnMap(aEast, aNorth) || !OnMap(bEast, bNorth)) { return StreetLayRefusal.OffMap; }
        if (!StreetArc.TryCreate(aEast, aNorth, bEast, bNorth, sagitta, out StreetArc line)) { return StreetLayRefusal.NotAnArc; }
        if (!line.IsStraight && line.Radius < (long)_ruleset.MinCurveRadiusTiles * Fixed.One) { return StreetLayRefusal.TooTight; }

        List<int> near = Candidates(line, sagitta);
        var joins = new List<Join>();
        Span<int> offsets = stackalloc int[4];
        foreach (int segment in near)
        {
            StreetArc other = _segments.Centerline[segment];
            int found = line.Crossings(other, offsets);
            for (int i = 0; i < found; i++)
            {
                var point = line.PointAt(offsets[i]);
                if (other.DistanceTo(point.East, point.North) > Fixed.One >> 6) { continue; }
                AddJoin(joins, RoundTile(point.East), RoundTile(point.North), offsets[i], segment,
                    other.OffsetAlong(point.East, point.North));
            }

            foreach ((int offset, int east, int north) in stackalloc[] { (0, aEast, aNorth), (line.Length, bEast, bNorth) })
            {
                long e = (long)east * Fixed.One, n = (long)north * Fixed.One;
                if (other.DistanceTo(e, n) <= Fixed.One >> 1) { AddJoin(joins, east, north, offset, segment, other.OffsetAlong(e, n)); }
            }
        }

        int shortest = _ruleset.MinSegmentLengthTiles * Fixed.One;
        var stops = new List<Join> { new(aEast, aNorth, 0, Rows.NoSlot, 0) };
        foreach (Join join in joins) { if (!StopAt(stops, join.East, join.North)) { stops.Add(join with { Segment = Rows.NoSlot }); } }
        if (!StopAt(stops, bEast, bNorth)) { stops.Add(new(bEast, bNorth, line.Length, Rows.NoSlot, 0)); }
        stops.Sort(static (x, y) => x.Offset.CompareTo(y.Offset));
        if (stops[0].East != aEast || stops[0].North != aNorth || stops[^1].East != bEast || stops[^1].North != bNorth)
        {
            return StreetLayRefusal.MovesRoad;
        }

        var pieces = new List<Piece>();
        StreetLayRefusal refusal = Cut(line, stops, shortest, pieces);
        if (refusal != StreetLayRefusal.None) { return refusal; }

        var cuts = new List<(int Segment, List<Join> Stops, List<Piece> Pieces)>();
        foreach (int segment in near)
        {
            StreetArc other = _segments.Centerline[segment];
            int oaE = RoundTile(other.A.East), oaN = RoundTile(other.A.North);
            int obE = RoundTile(other.B.East), obN = RoundTile(other.B.North);
            var on = new List<Join> { new(oaE, oaN, 0, segment, 0) };
            foreach (Join join in joins)
            {
                if (join.Segment != segment || (join.East == oaE && join.North == oaN) || (join.East == obE && join.North == obN)
                    || StopAt(on, join.East, join.North)) { continue; }
                on.Add(new(join.East, join.North, join.OtherOffset, segment, join.Offset));
            }

            if (on.Count == 1) { continue; }
            on.Add(new(obE, obN, other.Length, segment, 0));
            on.Sort(static (x, y) => x.Offset.CompareTo(y.Offset));
            var split = new List<Piece>();
            refusal = Cut(other, on, shortest, split);
            if (refusal != StreetLayRefusal.None) { return refusal; }
            cuts.Add((segment, on, split));
        }

        int cosine = Transcendental.Cos((int)IntegerMath.FloorDiv((long)_ruleset.MinCrossingAngleDegrees * Fixed.One, 360));
        foreach (Join stop in stops)
        {
            if (TooShallow(line, stop, near, cuts, cosine)) { return StreetLayRefusal.TooShallow; }
        }

        foreach (var (segment, on, split) in cuts)
        {
            Handle<RoadNode> end = _segments.NodeB[segment];
            int previous = segment, retained = 0;
            for (int i = 0; i < split.Count; i++)
            {
                Piece piece = split[i];
                Handle<RoadNode> to = i == split.Count - 1 ? end : NodeAtTile(piece.ToEast, piece.ToNorth);
                if (i == 0)
                {
                    _segments.NodeB[segment] = to;
                    _segments.Sagitta[segment] = new SubTiles(piece.Sagitta);
                    _segments.LengthTiles[segment] = WholeTiles(piece.Length);
                    _segments.Edited(segment);
                    continue;
                }

                int created = _segments.Rows.Resolve(_segments.Create(NodeAtTile(piece.FromEast, piece.FromNorth), to,
                    WholeTiles(piece.Length), (RoadKind)_segments.Kind[segment],
                    (TravelMode)_segments.ModesForward[segment], (TravelMode)_segments.ModesBackward[segment]));
                _segments.Sagitta[created] = new SubTiles(piece.Sagitta);
                int cut = (int)IntegerMath.RoundDiv(on[i].Offset, Fixed.One);
                splits.Add(new SegmentSplit(previous, created, new Tiles(cut - retained)));
                previous = created;
                retained = cut;
            }
        }

        foreach (Piece piece in pieces)
        {
            int created = _segments.Rows.Resolve(_segments.Create(NodeAtTile(piece.FromEast, piece.FromNorth),
                NodeAtTile(piece.ToEast, piece.ToNorth), WholeTiles(piece.Length), RoadKind.Street, TravelMode.Any, TravelMode.Any));
            _segments.Sagitta[created] = new SubTiles(piece.Sagitta);
            laid.Add(created);
        }

        RebuildDerived();
        return StreetLayRefusal.None;
    }

    private static bool OnMap(int east, int north) =>
        east >= 0 && north >= 0 && east <= CellGrid.WorldTiles && north <= CellGrid.WorldTiles;

    private static int RoundTile(long value) => (int)IntegerMath.RoundDiv(value, Fixed.One);

    private static Tiles WholeTiles(int length)
    {
        int tiles = (int)IntegerMath.RoundDiv((long)length, Fixed.One);
        return new Tiles(tiles < 1 ? 1 : tiles);
    }

    // Live Segments whose residency overlaps the arc's box, padded by a Tile, in slot order.
    private List<int> Candidates(StreetArc line, int sagitta)
    {
        var middle = line.PointAt(line.Length >> 1);
        long west = Min(Min(line.A.East, line.B.East), middle.East), east = Max(Max(line.A.East, line.B.East), middle.East);
        long south = Min(Min(line.A.North, line.B.North), middle.North), north = Max(Max(line.A.North, line.B.North), middle.North);
        int pad = (int)IntegerMath.FloorDiv(sagitta < 0 ? -(long)sagitta : sagitta, 2 * Fixed.One) + 1;
        int w = (int)IntegerMath.ShiftRight(west, Fixed.FractionalBits) - pad;
        int s = (int)IntegerMath.ShiftRight(south, Fixed.FractionalBits) - pad;
        int e = (int)IntegerMath.ShiftRight(east, Fixed.FractionalBits) + pad;
        int n = (int)IntegerMath.ShiftRight(north, Fixed.FractionalBits) + pad;
        var found = new List<int>();
        foreach (int segment in _residency.In(new Tiles(w), new Tiles(s), new Tiles(e - w + 1), new Tiles(n - s + 1)))
        {
            if (_segments.Rows.IsLive(segment)) { found.Add(segment); }
        }

        found.Sort();
        return found;
    }

    private static long Min(long a, long b) => a < b ? a : b;

    private static long Max(long a, long b) => a > b ? a : b;

    private static void AddJoin(List<Join> joins, int east, int north, int offset, int segment, int otherOffset)
    {
        foreach (Join join in joins) { if (join.Segment == segment && join.East == east && join.North == north) { return; } }
        joins.Add(new(east, north, offset, segment, otherOffset));
    }

    private static bool StopAt(List<Join> stops, int east, int north)
    {
        foreach (Join stop in stops) { if (stop.East == east && stop.North == north) { return true; } }
        return false;
    }

    private static StreetLayRefusal Cut(StreetArc line, List<Join> stops, int shortest, List<Piece> into)
    {
        for (int i = 0; i + 1 < stops.Count; i++)
        {
            Join from = stops[i], to = stops[i + 1];
            if (!line.TryPiece(from.Offset, from.East, from.North, to.Offset, to.East, to.North, out StreetArc piece, out int sagitta))
            {
                return StreetLayRefusal.MovesRoad;
            }

            if (piece.Length < shortest) { return StreetLayRefusal.TooShort; }
            into.Add(new(from.East, from.North, to.East, to.North, sagitta, piece.Length));
        }

        return StreetLayRefusal.None;
    }

    // Every direction the new Street leaves a stop in must differ by the minimum angle from every
    // direction an existing Segment leaves the same Tile in. Opposite directions are a continuation.
    private bool TooShallow(StreetArc line, Join stop, List<int> near, List<(int Segment, List<Join> Stops, List<Piece> Pieces)> cuts, int cosine)
    {
        Span<(int East, int North)> mine = stackalloc (int, int)[2];
        int count = 0;
        var tangent = line.TangentAt(stop.Offset);
        if (stop.Offset < line.Length) { mine[count++] = tangent; }
        if (stop.Offset > 0) { mine[count++] = (-tangent.East, -tangent.North); }

        foreach (int segment in near)
        {
            StreetArc other = _segments.Centerline[segment];
            if (RoundTile(other.A.East) == stop.East && RoundTile(other.A.North) == stop.North
                && Meets(mine[..count], other.TangentAt(0), cosine)) { return true; }
            var end = other.TangentAt(other.Length);
            if (RoundTile(other.B.East) == stop.East && RoundTile(other.B.North) == stop.North
                && Meets(mine[..count], (-end.East, -end.North), cosine)) { return true; }
        }

        foreach (var (segment, on, _) in cuts)
        {
            for (int i = 1; i + 1 < on.Count; i++)
            {
                if (on[i].East != stop.East || on[i].North != stop.North) { continue; }
                var through = _segments.Centerline[segment].TangentAt(on[i].Offset);
                if (Meets(mine[..count], through, cosine) || Meets(mine[..count], (-through.East, -through.North), cosine)) { return true; }
            }
        }

        return false;
    }

    private static bool Meets(ReadOnlySpan<(int East, int North)> mine, (int East, int North) theirs, int cosine)
    {
        foreach (var direction in mine)
        {
            long dot = ((long)direction.East * theirs.East) + ((long)direction.North * theirs.North);
            if (dot > (long)cosine * Fixed.One) { return true; }
        }

        return false;
    }

    // ponytail: a scan of every Node per lookup. Index Nodes by Tile if lays show up in a profile.
    private Handle<RoadNode> NodeAtTile(int east, int north)
    {
        for (int slot = 0; slot < _nodes.Rows.SlotCount; slot++)
        {
            if (_nodes.Rows.IsLive(slot) && _nodes.East[slot].Raw == east && _nodes.North[slot].Raw == north) { return _nodes.Rows.At(slot); }
        }

        return _nodes.Create(new Tiles(east), new Tiles(north));
    }
}
