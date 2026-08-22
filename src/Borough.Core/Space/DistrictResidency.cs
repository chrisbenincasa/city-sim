using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which slot of <see cref="DistrictCellTable"/> holds a given Cell, or none. The sparse index.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="CellResidency"/>'s shape exactly, and for its three reasons</b> — a flat array over
/// the Cell grid rather than a hash map (<c>05 §3</c> bans those in simulation code); slot-plus-one
/// encoding so a zeroed entry reads as <em>absent</em> rather than as slot 0; and storage beside the
/// table rather than in it, because this is indexed by coordinate and not by row, so <c>BOR0901</c> is
/// satisfied rather than dodged.
/// </para>
/// <para>
/// <b>It is <c>(derived AND rebuilt)</c> in the strict sense</b>: it has no order at all, being a
/// function from a coordinate to a slot, and both sides of it are saved — the coordinates are columns
/// and the slot is where the row already is. A rebuild reproduces it exactly rather than plausibly.
/// </para>
/// <para>
/// ⚠ <b>Off-map answers <em>absent</em>, and an unbuilt Cell answers <em>absent</em> too.</b> They are
/// the same answer because they mean the same thing here: no District contains that ground. A caller
/// that needs to tell the two apart is asking a question about the map, and
/// <see cref="CellGrid.Contains"/> is where that question is answered.
/// </para>
/// </remarks>
public sealed class DistrictResidency
{
    /// <summary>What <see cref="Slot"/> answers for a Cell in no District.</summary>
    public const int NotResident = Rows.NoSlot;

    private readonly int[] _slots = new int[CellGrid.WorldCellCount];

    private int _count;

    /// <summary>How many Cells belong to a District.</summary>
    public int Count => _count;

    /// <summary>The slot holding a Cell's membership, or <see cref="NotResident"/>.</summary>
    public int Slot(Cells east, Cells north) =>
        CellGrid.Contains(east, north)
            ? _slots[CellGrid.Index(east, north)] - 1
            : NotResident;

    /// <summary>
    /// The District a Cell belongs to, or a handle that resolves to nothing.
    /// </summary>
    /// <remarks>
    /// <b>This is the lookup a Building's District goes through</b> — a Building's Cell, then this.
    /// There is no District column on <see cref="BuildingTable"/> and there must not be one: it would
    /// be a copy that goes stale the first time task 4 moves a boundary.
    /// </remarks>
    public Handle<District> Of(DistrictCellTable cells, Cells east, Cells north)
    {
        ArgumentNullException.ThrowIfNull(cells);

        int slot = Slot(east, north);

        return slot == NotResident ? default : cells.District[slot];
    }

    /// <summary>Records a Cell's membership row in the index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Cell is off the map.</exception>
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

    /// <summary>Rebuilds the index from the table's saved coordinates. What a load calls.</summary>
    /// <remarks>
    /// <b>In index order, walking the table</b> — the one order a rebuild has available
    /// (<c>05 §3</c>), and sufficient because the result is a function rather than a sequence.
    /// </remarks>
    public void Rebuild(DistrictCellTable cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        Array.Clear(_slots);
        _count = 0;

        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (!cells.Rows.IsLive(slot))
            {
                continue;
            }

            _slots[CellGrid.Index(cells.East[slot], cells.North[slot])] = slot + 1;
            _count++;
        }
    }
}
