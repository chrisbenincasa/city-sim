using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which <see cref="FloodCellTable"/> row a Cell has, or that it is above the floodplain.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="WaterResidency"/> exactly, over the Hazard Region instead of the water.</b>
/// <see cref="FloodCellTable"/>'s own remarks predicted this class and deferred it — <em>"there is
/// no residency index beside it, and that is a deliberate absence: the only caller that would ask
/// is the overlay, which is unbuilt. The task that builds the overlay adds the index."</em> ⚠ <b>The
/// caller that arrived first was not the overlay.</b> A flood spreads through the Hazard Region by
/// asking each neighbour <em>are you floodplain, and how deep</em>, which is this question at every
/// step of the walk; the overlay still does not exist.
/// </para>
/// <para>
/// <b><c>(derived AND rebuilt)</c></b> in <see cref="CellResidency"/>'s sense: a function from a
/// coordinate to a slot, holding nothing the rows do not. A load rebuilds it from the saved
/// coordinates.
/// </para>
/// </remarks>
public sealed class FloodResidency
{
    /// <summary>What <see cref="Slot"/> answers for ground above the floodplain.</summary>
    public const int NotResident = Rows.NoSlot;

    /// <summary>Slot plus one, so a zeroed entry reads as <em>dry</em> rather than as slot 0.</summary>
    private readonly int[] _slots = new int[CellGrid.WorldCellCount];

    private int _count;

    /// <summary>How many Cells are in the Hazard Region.</summary>
    public int Count => _count;

    /// <summary>The slot holding a Cell, or <see cref="NotResident"/> — including off-map.</summary>
    /// <remarks>
    /// <b>Off-map answers <em>not floodplain</em> rather than throwing</b>, for
    /// <see cref="WaterResidency.Slot"/>'s reason: it is what lets a spread walk step off the edge
    /// of the world without a bounds test at every neighbour. A flood that reaches the map's
    /// boundary simply stops there.
    /// </remarks>
    public int Slot(Cells east, Cells north) =>
        CellGrid.Contains(east, north)
            ? _slots[CellGrid.Index(east, north)] - 1
            : NotResident;

    /// <summary>
    /// How deep a flood would stand on a Cell, or zero where it would not reach at all.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Zero is <em>not floodplain</em> and never a shallow one.</b>
    /// <see cref="FloodCellTable.Create"/> refuses a non-positive depth precisely so that this
    /// conflation cannot arise — every row is strictly positive, so the absent answer and the
    /// shallowest real one are distinguishable without a second return value.
    /// </remarks>
    public int DepthAt(FloodCellTable cells, Cells east, Cells north)
    {
        ArgumentNullException.ThrowIfNull(cells);

        int slot = Slot(east, north);

        return slot == NotResident ? 0 : cells.Depth[slot];
    }

    /// <summary>Records that a Cell's row sits at a slot.</summary>
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

    /// <summary>Rebuilds the whole index from the rows. What a load calls.</summary>
    /// <remarks>
    /// <b>In index order, walking the table</b> — the one order a rebuild has available
    /// (<c>05 §3</c>), and sufficient because the result is a function rather than a sequence.
    /// </remarks>
    public void Rebuild(FloodCellTable cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        Array.Clear(_slots);
        _count = 0;

        for (int slot = 0; slot < cells.Rows.SlotCount; slot++)
        {
            if (cells.Rows.IsLive(slot))
            {
                Add(cells.East[slot], cells.North[slot], slot);
            }
        }
    }
}
