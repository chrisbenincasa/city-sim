using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which Lots stand near a box of Tiles. The candidates <see cref="LotSubdivider"/> tests before it
/// lays a parcel.
/// </summary>
/// <remarks>
/// <para>
/// A Lot is filed under the Cell holding the south-west corner of its reach: its parcel's bounds
/// joined with its Address Tile. A Lot that overlaps a box, or has its Address in it, has a reach that
/// meets the box, so widening a query south-west by the largest reach filed finds every such Lot.
/// </para>
/// <para>
/// Each entry carries the Lot's id. A query skips an entry whose slot is dead or now holds another
/// Lot, so freeing a Lot needs no call here. A rebuild compacts the entries once stale ones outnumber
/// the live Lots, which bounds the index at about twice the Lot count.
/// </para>
/// <para>
/// ⚠ <b>A Lot created without <see cref="Add"/> is still found.</b> Its allocation advances
/// <see cref="Rows.NextId"/> past the id this index expects next, and the next query rebuilds. The
/// generator and play register each Lot once its parcel is written, so the rebuild is a fallback for
/// fixtures and costs one pass over the Lots.
/// </para>
/// <para>
/// Derived and rebuilt (<c>05 §3</c>); it holds nothing a slot-order walk of the Lots cannot
/// recover. The order a query returns Lots in is not a contract. Callers ask whether any candidate
/// takes the ground, which no order changes. A query may rebuild, so it is not safe from parallel
/// readers.
/// </para>
/// </remarks>
public sealed class LotResidency
{
    // Entry plus one, so a cleared head reads as an empty Cell.
    private readonly int[] _head = new int[CellGrid.WorldCellCount];

    private int[] _slot = new int[64];
    private ulong[] _id = new ulong[64];
    private int[] _next = new int[64];
    private int _count;
    private int _reach;
    private ulong _expected = 1;

    /// <summary>Entries held, including stale ones for freed Lots.</summary>
    public int EntryCount => _count;

    /// <summary>Files every live Lot again, in slot order.</summary>
    public void Rebuild(LotTable lots)
    {
        ArgumentNullException.ThrowIfNull(lots);

        Array.Clear(_head);
        _count = 0;
        _reach = 0;
        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (lots.Rows.IsLive(slot)) { File(lots, slot); }
        }
        _expected = lots.Rows.NextId;
    }

    /// <summary>Files a Lot whose parcel and Address are written. Call once, after creation.</summary>
    public void Add(LotTable lots, int slot)
    {
        ArgumentNullException.ThrowIfNull(lots);

        ulong id = lots.Rows.IdAt(slot);
        if (id < _expected) { return; }
        if (id > _expected) { Rebuild(lots); return; }
        File(lots, slot);
        _expected = id + 1;
        if (_count > 2 * lots.Rows.LiveCount + 64) { Rebuild(lots); }
    }

    /// <summary>The live Lots whose reach may meet a box of Tiles. A superset; callers test each.</summary>
    public Query Near(LotTable lots, long west, long south, long east, long north)
    {
        ArgumentNullException.ThrowIfNull(lots);

        if (lots.Rows.NextId != _expected) { Rebuild(lots); }
        int lowEast = Cell(west - _reach), lowNorth = Cell(south - _reach);
        int highEast = Cell(east), highNorth = Cell(north);
        var area = new CellRect(new Cells(lowEast), new Cells(lowNorth),
            new Cells(highEast - lowEast + 1), new Cells(highNorth - lowNorth + 1));
        return new Query(_head, _slot, _id, _next, lots.Rows, area);
    }

    private void File(LotTable lots, int slot)
    {
        Reach(lots, slot, out long west, out long south, out long east, out long north);
        long extent = east - west > north - south ? east - west : north - south;
        if (extent > _reach) { _reach = (int)extent; }

        if (_count == _slot.Length) { Grow(); }
        int cell = CellGrid.Index(new Cells(Cell(west)), new Cells(Cell(south)));
        _slot[_count] = slot;
        _id[_count] = lots.Rows.IdAt(slot);
        _next[_count] = _head[cell];
        _head[cell] = ++_count;
    }

    // The parcel's bounds joined with the Address Tile, as a closed box of Tiles.
    private static void Reach(LotTable lots, int slot, out long west, out long south, out long east, out long north)
    {
        west = east = lots.East[slot].Raw;
        south = north = lots.North[slot].Raw;
        LandRectangle bounds = lots.Parcel(slot).Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) { return; }
        if (bounds.X < west) { west = bounds.X; }
        if (bounds.Y < south) { south = bounds.Y; }
        if ((long)bounds.X + bounds.Width > east) { east = (long)bounds.X + bounds.Width; }
        if ((long)bounds.Y + bounds.Height > north) { north = (long)bounds.Y + bounds.Height; }
    }

    private void Grow()
    {
        int size = checked(_slot.Length * 2);
        var slots = new int[size];
        var ids = new ulong[size];
        var next = new int[size];
        _slot.AsSpan().CopyTo(slots);
        _id.AsSpan().CopyTo(ids);
        _next.AsSpan().CopyTo(next);
        (_slot, _id, _next) = (slots, ids, next);
    }

    private static int Cell(long tile) => tile <= 0 ? 0
        : tile >= CellGrid.WorldTiles ? CellGrid.WorldCells - 1
        : (int)IntegerMath.FloorDiv(tile, CellGrid.TilesPerCell);

    public ref struct Query
    {
        private readonly ReadOnlySpan<int> _head;
        private readonly ReadOnlySpan<int> _slot;
        private readonly ReadOnlySpan<ulong> _id;
        private readonly ReadOnlySpan<int> _next;
        private readonly Rows _rows;
        private readonly CellRect _area;
        private int _east;
        private int _north;
        private int _entry;

        internal Query(ReadOnlySpan<int> head, ReadOnlySpan<int> slot, ReadOnlySpan<ulong> id,
            ReadOnlySpan<int> next, Rows rows, CellRect area)
        {
            _head = head;
            _slot = slot;
            _id = id;
            _next = next;
            _rows = rows;
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
                    int entry = _entry - 1;
                    _entry = _next[entry];
                    int slot = _slot[entry];
                    if (_rows.IsLive(slot) && _rows.IdAt(slot) == _id[entry])
                    {
                        Current = slot;
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
                _entry = _head[CellGrid.Index(new Cells(_east), new Cells(_north))];
            }
        }
    }
}
