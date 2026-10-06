using Borough.Core.Arithmetic;

namespace Borough.Core.Space;

/// <summary>
/// The faces of the planar Road Graph. Each Arc is a half-edge with its face on its left, so the
/// Arc leaving a Segment's <c>NodeA</c> borders side 0 and the Arc leaving <c>NodeB</c> borders side 1.
/// </summary>
/// <remarks>
/// <para>
/// Derived arrays beside the graph, rebuilt wholesale with the Arcs, for the reason
/// <see cref="RoadArcs"/> gives. Face indices are valid only until the next rebuild; a face's stable
/// identity is its <see cref="Anchor"/>.
/// </para>
/// <para>
/// Only Streets bound faces, because a block is a face of the Street graph. A closed face has
/// positive area and is walked counterclockwise. Each connected component also yields one face of
/// nonpositive area, its outer boundary. Other road kinds and zero-length Segments border no face.
/// </para>
/// </remarks>
public sealed class RoadFaces
{
    private int[] _source = [];
    private int[] _twin = [];
    private int[] _rank = [];
    private int[] _ccw = [];
    private int[] _ccwCount = [];
    private int[] _faceOf = [];
    private int[] _boundary = [];
    private int[] _faceStart = [];
    private Int128[] _area = [];
    private long[] _minEast = [];
    private long[] _minNorth = [];
    private long[] _maxEast = [];
    private long[] _maxNorth = [];
    private ulong[] _anchorId = [];
    private byte[] _anchorSide = [];
    private int[] _firstArc = [];
    private int _arcCount;
    private int _count;
    private RoadSegmentTable? _segments;
    private RoadArcs? _arcs;
    private RoadNodeTable? _nodes;

    /// <summary>How many faces the graph has, closed and outer.</summary>
    public int Count => _count;

    /// <summary>The face on an Arc's left, or -1 for an Arc that borders no face.</summary>
    public int FaceOf(int arc) => _faceOf[arc];

    /// <summary>The Segment side an Arc borders: 0 when it leaves the Segment's <c>NodeA</c>, else 1.</summary>
    public byte SideOf(int arc) => Side(arc);

    /// <summary>The other direction of the same Segment.</summary>
    public int Twin(int arc) => _twin[arc];

    /// <summary>The Arc that follows <paramref name="arc"/> around its face.</summary>
    public int Next(int arc)
    {
        int twin = _twin[arc];
        int node = _source[twin];
        int position = _rank[twin];
        int count = _ccwCount[node];
        int start = _nodes!.ArcStart[node];
        return _ccw[start + (position == 0 ? count - 1 : position - 1)];
    }

    /// <summary>The face's Arcs in walk order.</summary>
    public ReadOnlySpan<int> Boundary(int face) =>
        _boundary.AsSpan(_faceStart[face], _faceStart[face + 1] - _faceStart[face]);

    /// <summary>Whether the face encloses ground: positive area, walked counterclockwise.</summary>
    public bool IsClosed(int face) => _area[face] > 0;

    /// <summary>Six times the face's signed area, in raw Q16.16 units squared. Arcs count with a parabolic segment.</summary>
    public Int128 ScaledArea(int face) => _area[face];

    /// <summary>The face's stable identity: the lowest Segment id on its boundary and that Arc's side.</summary>
    public (ulong SegmentId, byte Side) Anchor(int face) => (_anchorId[face], _anchorSide[face]);

    /// <summary>Whether a Q16.16 point lies inside the face's boundary.</summary>
    /// <remarks>Boundary points follow the half-open crossing rule, so each lies in exactly one of two adjacent faces.</remarks>
    public bool Contains(int face, long east, long north)
    {
        if (east < _minEast[face] || east > _maxEast[face] || north < _minNorth[face] || north > _maxNorth[face])
        {
            return false;
        }

        RoadSegmentTable segments = _segments!;
        bool inside = false;

        foreach (int arc in Boundary(face))
        {
            StreetArc line = segments.Centerline[ArcSegment(arc)];
            var (from, to) = Side(arc) == 0 ? (line.A, line.B) : (line.B, line.A);

            if ((from.North > north) != (to.North > north))
            {
                Int128 cross = ((Int128)(to.East - from.East) * (north - from.North))
                    - ((Int128)(to.North - from.North) * (east - from.East));
                if ((cross > 0) == (to.North > from.North))
                {
                    inside = !inside;
                }
            }

            if (!line.IsStraight && InBulge(line, segments.Sagitta[ArcSegment(arc)].Raw, east, north))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>The smallest closed face containing a Q16.16 point, or -1 when it lies in no closed face.</summary>
    // ponytail: linear scan over faces; add a Cell index when a per-Tick caller appears.
    public int Find(long east, long north)
    {
        int best = -1;

        for (int face = 0; face < _count; face++)
        {
            if (IsClosed(face) && (best < 0 || _area[face] < _area[best]) && Contains(face, east, north))
            {
                best = face;
            }
        }

        return best;
    }

    internal void Rebuild(RoadNodeTable nodes, RoadSegmentTable segments, RoadArcs arcs)
    {
        _segments = segments;
        _arcs = arcs;
        _nodes = nodes;
        int count = arcs.Count;
        int nodeSlots = nodes.Rows.SlotCount;
        Grow(ref _source, count);
        Grow(ref _twin, count);
        Grow(ref _rank, count);
        Grow(ref _ccw, count);
        Grow(ref _faceOf, count);
        Grow(ref _boundary, count);
        Grow(ref _ccwCount, nodeSlots);
        Grow(ref _firstArc, segments.Rows.SlotCount);
        _arcCount = count;

        Array.Fill(_firstArc, -1, 0, segments.Rows.SlotCount);
        for (int node = 0; node < nodeSlots; node++)
        {
            int start = nodes.ArcStart[node];
            int end = start + nodes.ArcCount[node];
            for (int arc = start; arc < end; arc++)
            {
                _source[arc] = node;
            }
        }

        for (int arc = 0; arc < count; arc++)
        {
            int segment = arcs.Segment[arc];
            if (_firstArc[segment] < 0)
            {
                _firstArc[segment] = arc;
            }
            else
            {
                _twin[arc] = _firstArc[segment];
                _twin[_firstArc[segment]] = arc;
            }
        }

        for (int node = 0; node < nodeSlots; node++)
        {
            Order(node);
        }

        WalkFaces();
    }

    private int ArcSegment(int arc) => _arcs!.Segment[arc];

    private byte Side(int arc)
    {
        int segment = ArcSegment(arc);
        return _nodes!.Rows.Resolve(_segments!.NodeA[segment]) == _source[arc] ? (byte)0 : (byte)1;
    }

    private bool Excluded(int arc)
    {
        int segment = ArcSegment(arc);
        return (RoadKind)_segments!.Kind[segment] != RoadKind.Street || _segments.Centerline[segment].Length == 0;
    }

    private void Order(int node)
    {
        int start = _nodes!.ArcStart[node];
        int end = start + _nodes.ArcCount[node];
        int length = 0;

        for (int arc = start; arc < end; arc++)
        {
            if (Excluded(arc))
            {
                _rank[arc] = -1;
                continue;
            }

            int position = start + length;
            while (position > start && Compare(arc, _ccw[position - 1]) < 0)
            {
                _ccw[position] = _ccw[position - 1];
                position--;
            }

            _ccw[position] = arc;
            length++;
        }

        _ccwCount[node] = length;
        for (int position = 0; position < length; position++)
        {
            _rank[_ccw[start + position]] = position;
        }
    }

    // Counterclockwise from due east. Equal tangents order by signed curvature, then Segment id.
    private int Compare(int a, int b)
    {
        var (aEast, aNorth, aTurn, aRadius) = Departure(a);
        var (bEast, bNorth, bTurn, bRadius) = Departure(b);
        int aHalf = aNorth < 0 || (aNorth == 0 && aEast < 0) ? 1 : 0;
        int bHalf = bNorth < 0 || (bNorth == 0 && bEast < 0) ? 1 : 0;
        if (aHalf != bHalf)
        {
            return aHalf - bHalf;
        }

        long cross = ((long)aEast * bNorth) - ((long)aNorth * bEast);
        if (cross != 0)
        {
            return cross > 0 ? -1 : 1;
        }

        if (aTurn != bTurn)
        {
            return aTurn - bTurn;
        }

        if (aTurn != 0 && aRadius != bRadius)
        {
            return (aRadius < bRadius) == (aTurn > 0) ? 1 : -1;
        }

        return _segments!.Rows.IdAt(ArcSegment(a)).CompareTo(_segments.Rows.IdAt(ArcSegment(b)));
    }

    // The unit tangent leaving the Arc's source, and which way the road then turns: +1 counterclockwise.
    private (int East, int North, int Turn, long Radius) Departure(int arc)
    {
        int segment = ArcSegment(arc);
        StreetArc line = _segments!.Centerline[segment];
        int sagitta = line.IsStraight ? 0 : _segments.Sagitta[segment].Raw;
        if (Side(arc) == 0)
        {
            var (east, north) = line.TangentAt(0);
            return (east, north, -Sign(sagitta), line.Radius);
        }

        var (backEast, backNorth) = line.TangentAt(line.Length);
        return (-backEast, -backNorth, Sign(sagitta), line.Radius);
    }

    private void WalkFaces()
    {
        Array.Fill(_faceOf, -1, 0, _arcCount);
        _count = 0;
        int written = 0;
        Grow(ref _faceStart, 1);
        _faceStart[0] = 0;

        for (int first = 0; first < _arcCount; first++)
        {
            if (_faceOf[first] >= 0 || _rank[first] < 0)
            {
                continue;
            }

            int face = _count++;
            GrowFaces(_count);
            Int128 area = 0;
            long minEast = long.MaxValue, minNorth = long.MaxValue, maxEast = long.MinValue, maxNorth = long.MinValue;
            ulong anchorId = ulong.MaxValue;
            byte anchorSide = 0;
            int arc = first;

            do
            {
                _faceOf[arc] = face;
                _boundary[written++] = arc;
                int segment = ArcSegment(arc);
                StreetArc line = _segments!.Centerline[segment];
                byte side = Side(arc);
                var (from, to) = side == 0 ? (line.A, line.B) : (line.B, line.A);
                area += 3 * (((Int128)from.East * to.North) - ((Int128)to.East * from.North));

                long bulge = 0;
                if (!line.IsStraight)
                {
                    long sagitta = _segments.Sagitta[segment].Raw;
                    long dx = line.B.East - line.A.East;
                    long dy = line.B.North - line.A.North;
                    long chord = IntegerMath.SqrtFloor((dx * dx) + (dy * dy));
                    area -= 4 * (Int128)chord * (side == 0 ? sagitta : -sagitta);
                    bulge = sagitta < 0 ? -sagitta : sagitta;
                }

                minEast = Min(minEast, Min(from.East, to.East) - bulge);
                minNorth = Min(minNorth, Min(from.North, to.North) - bulge);
                maxEast = Max(maxEast, Max(from.East, to.East) + bulge);
                maxNorth = Max(maxNorth, Max(from.North, to.North) + bulge);

                ulong id = _segments.Rows.IdAt(segment);
                if (id < anchorId || (id == anchorId && side < anchorSide))
                {
                    anchorId = id;
                    anchorSide = side;
                }

                arc = Next(arc);
            }
            while (arc != first);

            _area[face] = area;
            _minEast[face] = minEast;
            _minNorth[face] = minNorth;
            _maxEast[face] = maxEast;
            _maxNorth[face] = maxNorth;
            _anchorId[face] = anchorId;
            _anchorSide[face] = anchorSide;
            _faceStart[face + 1] = written;
        }
    }

    // A point strictly inside the circular segment between the chord and the arc. A quarter-turn arc is
    // a minor arc, so the disc on the bulge side of the chord is exactly that segment.
    private static bool InBulge(StreetArc line, int sagitta, long east, long north)
    {
        Int128 cross = ((Int128)(line.B.East - line.A.East) * (north - line.A.North))
            - ((Int128)(line.B.North - line.A.North) * (east - line.A.East));
        if (sagitta > 0 ? cross <= 0 : cross >= 0)
        {
            return false;
        }

        Int128 dx = east - line.Center.East;
        Int128 dy = north - line.Center.North;
        return (dx * dx) + (dy * dy) < (Int128)line.Radius * line.Radius;
    }

    private void GrowFaces(int faces)
    {
        if (_area.Length >= faces)
        {
            Grow(ref _faceStart, faces + 1);
            return;
        }

        int size = Max(faces, _area.Length * 2);
        Array.Resize(ref _area, size);
        Array.Resize(ref _minEast, size);
        Array.Resize(ref _minNorth, size);
        Array.Resize(ref _maxEast, size);
        Array.Resize(ref _maxNorth, size);
        Array.Resize(ref _anchorId, size);
        Array.Resize(ref _anchorSide, size);
        Array.Resize(ref _faceStart, size + 1);
    }

    private static void Grow<T>(ref T[] array, int size)
    {
        if (array.Length < size)
        {
            Array.Resize(ref array, size);
        }
    }

    private static int Sign(int value) => value > 0 ? 1 : value < 0 ? -1 : 0;

    private static long Min(long a, long b) => a < b ? a : b;

    private static long Max(long a, long b) => a > b ? a : b;

    private static int Max(int a, int b) => a > b ? a : b;
}
