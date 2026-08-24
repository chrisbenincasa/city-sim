using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which slot of <see cref="WaterCellTable"/> holds a given Cell, or none. The sparse index.
/// </summary>
/// <remarks>
/// <para>
/// <b>A flat array over the Cell grid, and deliberately not a hash map</b> — <c>05 §3</c> bans them in
/// simulation code, and <see cref="CellResidency"/> carries the full argument. It is
/// <c>(derived AND rebuilt)</c> for that class's stated reason: a function from a coordinate to a
/// slot, with both sides of it saved.
/// </para>
/// <para>
/// ⚠ <b>A dense index beside a sparse table is not a contradiction of the sparse choice.</b> The index
/// costs one <c>int</c> per Cell whether or not the Cell is wet; the <em>rows</em> are what sparsity
/// saves, and they carry three columns each. The same trade is already made twice, by
/// <see cref="CellResidency"/> and <see cref="DistrictResidency"/>.
/// </para>
/// </remarks>
public sealed class WaterResidency
{
    /// <summary>What <see cref="Slot"/> answers for a dry Cell.</summary>
    public const int NotResident = Rows.NoSlot;

    /// <summary>Slot plus one, so a zeroed entry reads as <em>dry</em> rather than as slot 0.</summary>
    private readonly int[] _slots = new int[CellGrid.WorldCellCount];

    private int _count;

    /// <summary>How many Cells are wet.</summary>
    public int Count => _count;

    /// <summary>The slot holding a Cell, or <see cref="NotResident"/> — including for an off-map Cell.</summary>
    /// <remarks>
    /// <b>Off-map answers <em>dry</em> rather than throwing</b>, which is what lets a shoreline scan
    /// walk off the edge of the world without a bounds test at every neighbour. ⚠ It also means
    /// <em>the world's edge reads as land</em>, so a body reaching the boundary is found by asking
    /// <see cref="CellGrid.Contains"/> and never by asking this.
    /// </remarks>
    public int Slot(Cells east, Cells north) =>
        CellGrid.Contains(east, north)
            ? _slots[CellGrid.Index(east, north)] - 1
            : NotResident;

    /// <summary>Whether a Cell is covered by water.</summary>
    public bool IsWet(Cells east, Cells north) => Slot(east, north) != NotResident;

    /// <summary>The Water Body covering a Cell, or <c>default</c> if it is dry.</summary>
    public Handle<WaterBody> Of(WaterCellTable cells, Cells east, Cells north)
    {
        ArgumentNullException.ThrowIfNull(cells);

        int slot = Slot(east, north);

        return slot == NotResident ? default : cells.Body[slot];
    }

    /// <summary>Records that a Cell's row sits at a slot.</summary>
    public void Add(Cells east, Cells north, int slot)
    {
        if (!CellGrid.Contains(east, north))
        {
            throw new ArgumentOutOfRangeException(
                nameof(east),
                $"Cell ({east.Raw}, {north.Raw}) is off a {CellGrid.WorldCells}-Cell map.");
        }

        int index = CellGrid.Index(east, north);

        if (_slots[index] == 0)
        {
            _count++;
        }

        _slots[index] = slot + 1;
    }

    /// <summary>Rebuilds the whole index from the rows. What a load calls.</summary>
    public void Rebuild(WaterCellTable cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        Array.Clear(_slots);
        _count = 0;

        // In index order, walking the table -- the one order a rebuild has available (05 section 3),
        // and sufficient because the result is a function rather than a sequence.
        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot))
            {
                continue;
            }

            Add(cells.East[slot], cells.North[slot], slot);
        }
    }
}
