using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>Road Segments indexed by the Cells touched by their centerline bounds.</summary>
/// <remarks>
/// Flat derived storage owned and rebuilt by RoadGraph, like RoadArcs. Queries return conservative
/// candidates in Cell order, then descending slot order. They allocate nothing and share no scratch.
/// </remarks>
public sealed class SegmentResidency
{
    private readonly int[] _head = new int[CellGrid.WorldCellCount];
    private CellRect[] _bounds = [];
    private Entry[] _entries = [];

    internal readonly record struct Entry(int Segment, int Next);

    /// <summary>Replaces every Cell list from the live Segment centerlines.</summary>
    public void Rebuild(RoadSegmentTable segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        Array.Clear(_head);
        int slots = segments.Rows.SlotCount;
        if (_bounds.Length < slots) { _bounds = new CellRect[slots]; }
        int count = 0;
        for (int slot = 0; slot < slots; slot++)
        {
            CellRect box = CellRect.Empty;
            if (segments.Rows.IsLive(slot))
            {
                StreetArc arc = segments.Centerline[slot];
                long sagitta = segments.Sagitta[slot].Raw;
                if (sagitta < 0) { sagitta = -sagitta; }
                long margin = IntegerMath.CeilDiv(sagitta, Fixed.One);
                long aEast = IntegerMath.FloorDiv(arc.A.East, Fixed.One);
                long aNorth = IntegerMath.FloorDiv(arc.A.North, Fixed.One);
                long bEast = IntegerMath.FloorDiv(arc.B.East, Fixed.One);
                long bNorth = IntegerMath.FloorDiv(arc.B.North, Fixed.One);
                box = Bounds(
                    (aEast < bEast ? aEast : bEast) - margin,
                    (aNorth < bNorth ? aNorth : bNorth) - margin,
                    (aEast > bEast ? aEast : bEast) + margin,
                    (aNorth > bNorth ? aNorth : bNorth) + margin);
            }
            _bounds[slot] = box;
            count = checked(count + box.Count);
        }
        if (_entries.Length < count) { _entries = new Entry[count]; }
        int entry = 0;
        for (int slot = 0; slot < slots; slot++)
        {
            CellRect box = _bounds[slot];
            for (int north = box.North.Raw; north < box.NorthEnd.Raw; north++)
            {
                for (int east = box.East.Raw; east < box.EastEnd.Raw; east++)
                {
                    int cell = (north * CellGrid.WorldCells) + east;
                    _entries[entry] = new Entry(slot, _head[cell]);
                    _head[cell] = ++entry;
                }
            }
        }
    }

    /// <summary>Candidates whose Cell sets meet a half-open rectangle of Cells.</summary>
    public Query In(CellRect area) => new(_head, _bounds, _entries, area.Clamp());

    /// <summary>Candidates whose Cell sets meet a half-open rectangle of Tiles.</summary>
    public Query In(Tiles east, Tiles north, Tiles width, Tiles height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width.Raw, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegative(height.Raw, nameof(height));
        return In(width.Raw == 0 || height.Raw == 0 ? CellRect.Empty : Bounds(
            east.Raw, north.Raw, (long)east.Raw + width.Raw - 1, (long)north.Raw + height.Raw - 1));
    }

    /// <summary>Candidates for the inclusive square around a point. Callers test exact distance.</summary>
    public Query Near(Tiles east, Tiles north, Tiles range)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(range.Raw, nameof(range));
        return In(Bounds(
            (long)east.Raw - range.Raw, (long)north.Raw - range.Raw,
            (long)east.Raw + range.Raw, (long)north.Raw + range.Raw));
    }

    private static CellRect Bounds(long east, long north, long eastEnd, long northEnd)
    {
        if (eastEnd < 0 || northEnd < 0 || east > CellGrid.WorldTiles || north > CellGrid.WorldTiles)
        {
            return CellRect.Empty;
        }
        int lowEast = Cell(east), lowNorth = Cell(north);
        int highEast = Cell(eastEnd), highNorth = Cell(northEnd);
        return new CellRect(new Cells(lowEast), new Cells(lowNorth),
            new Cells(highEast - lowEast + 1), new Cells(highNorth - lowNorth + 1));
    }

    private static int Cell(long tile) => tile <= 0 ? 0
        : tile >= CellGrid.WorldTiles ? CellGrid.WorldCells - 1
        : (int)IntegerMath.FloorDiv(tile, CellGrid.TilesPerCell);

    /// <summary>A read-only, allocation-free walk that returns each Segment once.</summary>
    public ref struct Query
    {
        private readonly ReadOnlySpan<int> _head;
        private readonly ReadOnlySpan<CellRect> _bounds;
        private readonly ReadOnlySpan<Entry> _entries;
        private readonly CellRect _area;
        private int _east;
        private int _north;
        private int _entry;

        internal Query(ReadOnlySpan<int> head, ReadOnlySpan<CellRect> bounds,
            ReadOnlySpan<Entry> entries, CellRect area)
        {
            _head = head;
            _bounds = bounds;
            _entries = entries;
            _area = area;
            _east = area.East.Raw - 1;
            _north = area.North.Raw;
            _entry = 0;
            Current = Rows.NoSlot;
        }

        public readonly Query GetEnumerator() => this;
        public int Current { get; private set; }

        public bool MoveNext()
        {
            if (_area.IsEmpty) { return false; }
            while (true)
            {
                while (_entry != 0)
                {
                    Entry entry = _entries[_entry - 1];
                    _entry = entry.Next;
                    CellRect box = _bounds[entry.Segment];

                    // Only the first Cell shared by the two rectangles can return this Segment.
                    // This removes duplicates without stamps, so simultaneous queries stay read-only.
                    int firstEast = box.East.Raw > _area.East.Raw ? box.East.Raw : _area.East.Raw;
                    int firstNorth = box.North.Raw > _area.North.Raw ? box.North.Raw : _area.North.Raw;
                    if (_east == firstEast && _north == firstNorth)
                    {
                        Current = entry.Segment;
                        return true;
                    }
                }
                _east++;
                if (_east >= _area.EastEnd.Raw)
                {
                    _east = _area.East.Raw;
                    _north++;
                }
                if (_north >= _area.NorthEnd.Raw) { return false; }
                _entry = _head[(_north * CellGrid.WorldCells) + _east];
            }
        }
    }
}
