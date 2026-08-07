using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which slot of <see cref="LayerCellTable"/> holds a given Cell, or none. The sparse index.
/// </summary>
/// <remarks>
/// <para>
/// <b>A flat array over the Cell grid, and deliberately not a hash map.</b> <c>05 §3</c> bans them
/// outright in simulation code, and at 128² Cells this is 64 KB with an <c>O(1)</c> lookup and no
/// iteration order to get wrong. It is <em>the null</em> of <c>02 §2.1</c>'s sparse storage: the array
/// is dense and cheap, the rows behind it are not.
/// </para>
/// <para>
/// <b>Derived and rebuilt, and it passes <c>05 §3</c>'s test for that classification.</b> The rule is
/// that a structure is <c>(derived AND rebuilt)</c> only if its <em>order</em> is recoverable from
/// saved state, not merely its membership. This has no order at all — it is a function from a
/// coordinate to a slot, and both sides of it are saved: the coordinate columns are state, and the
/// slot is where the row already is. A rebuild reproduces it exactly rather than plausibly.
/// </para>
/// <para>
/// <b>It lives beside the table rather than in it, and <c>BOR0901</c> is why that is correct rather
/// than a dodge.</b> The lint rejects storage in a <c>[Table]</c> type that is not a declared column,
/// and this is not per-row storage — it is indexed by Cell coordinate, of which there are 16,384
/// whether or not any of them has a row. A column would have to invent 16,384 rows to avoid one array.
/// </para>
/// </remarks>
public sealed class CellResidency
{
    /// <summary>What <see cref="Slot"/> answers for a Cell with no row.</summary>
    public const int NotResident = Rows.NoSlot;

    /// <summary>
    /// Slot plus one, so that a zeroed entry reads as <em>absent</em> rather than as slot 0.
    /// </summary>
    /// <remarks>
    /// The same encoding <see cref="IndexList"/> uses, for the same reason: the empty state has to be
    /// the default value or the structure needs an initialisation pass that somebody will forget.
    /// </remarks>
    private readonly int[] _slots = new int[CellGrid.WorldCellCount];

    private int _count;

    /// <summary>How many Cells have a row.</summary>
    public int Count => _count;

    /// <summary>The slot holding a Cell, or <see cref="NotResident"/> — including for an off-map Cell.</summary>
    /// <remarks>
    /// <b>Off-map answers <em>absent</em> rather than throwing, and that is the boundary policy of the
    /// whole convolution.</b> A kernel centred near the edge reaches past it, and a source outside the
    /// map does not exist, so it contributes zero. Zero-extension is what keeps the operator linear at
    /// the edge — clamping or wrapping would make a Cell's value depend on where the map ends, and a
    /// plume that piles up against the world edge is a rendering artefact of the boundary rule rather
    /// than anything the city did.
    /// </remarks>
    public int Slot(Cells east, Cells north) =>
        CellGrid.Contains(east, north)
            ? _slots[CellGrid.Index(east, north)] - 1
            : NotResident;

    /// <summary>The slot holding a Cell, allocating a row if there is none.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Cell is off the map.</exception>
    public int Ensure(LayerCellTable cells, Cells east, Cells north)
    {
        ArgumentNullException.ThrowIfNull(cells);

        if (!CellGrid.Contains(east, north))
        {
            throw new ArgumentOutOfRangeException(
                nameof(east),
                $"Cell ({east.Raw}, {north.Raw}) is off a {CellGrid.WorldCells}-Cell map.");
        }

        int index = CellGrid.Index(east, north);

        if (_slots[index] != 0)
        {
            return _slots[index] - 1;
        }

        int slot = cells.Rows.Resolve(cells.Create(east, north));
        _slots[index] = slot + 1;
        _count++;

        return slot;
    }

    /// <summary>Makes every Cell in a rectangle resident. What an emission's halo needs.</summary>
    public void Ensure(LayerCellTable cells, CellRect rect)
    {
        CellRect clamped = rect.Clamp();

        for (int north = clamped.North.Raw; north < clamped.NorthEnd.Raw; north++)
        {
            for (int east = clamped.East.Raw; east < clamped.EastEnd.Raw; east++)
            {
                Ensure(cells, new Cells(east), new Cells(north));
            }
        }
    }

    /// <summary>
    /// Rebuilds the index from the table's saved coordinates. What a load calls.
    /// </summary>
    /// <remarks>
    /// <b>In index order, walking the table</b> — the one order a rebuild has available (<c>05 §3</c>),
    /// and sufficient here because the result is a function rather than a sequence.
    /// </remarks>
    public void Rebuild(LayerCellTable cells)
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
