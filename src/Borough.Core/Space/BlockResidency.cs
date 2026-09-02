using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which slot of <c>BlockTable</c> holds a given lattice square, or none. The sparse index.
/// </summary>
/// <remarks>
/// <para>
/// <b>A flat array over the Street lattice, and deliberately not a hash map</b>, which is
/// <see cref="CellResidency"/>'s argument unchanged: <c>05 §3</c> bans them in simulation code, and a
/// flat array has an <c>O(1)</c> lookup and no iteration order to get wrong. The array is dense and
/// cheap; the rows behind it are not.
/// </para>
/// <para>
/// 🔴 <b>IT IS SIZED BY A TUNING KEY AND NOT BY A DESIGN CONSTANT, AND IT IS THE ONLY RESIDENCY THAT
/// IS.</b> <see cref="CellResidency"/>, <c>BuildingResidency</c>, <c>DistrictResidency</c>,
/// <c>FloodResidency</c> and <c>WaterResidency</c> every one index <see cref="CellGrid"/>, whose extent
/// is a design constant that never moves — so every one of them can allocate a fixed array in a field
/// initialiser. This one indexes the lattice, whose extent is
/// <c>WorldTiles / block_tiles + 1</c>, and <c>[roads] block_tiles</c> is hot-reloadable tuning data
/// whose loader floor is <b>1</b>. ***So the house pattern does not transfer, and copying it would
/// have allocated a fixed array of the wrong length.***
/// </para>
/// <para>
/// <b>What that costs, which is why the sizing is lazy:</b>
/// </para>
/// <code>
///   block_tiles     span   lattice   dense int[]
///        32 —— shipped  513     263 k       1.05 MB
///         8           2,049     4.2 M       16.8 MB
///         1 —— the floor 16,385   268 M       1.07 GB
/// </code>
/// <para>
/// ⚠ <b>A 1-Tile block is a world where every Tile is a Street</b>, which is not a city — but the
/// loader permits it today, so this class sizes itself from what it is told rather than from what it
/// hopes. The floor being 1 is filed against <c>plans/0012</c> and is not this type's to fix.
/// </para>
/// <para>
/// <b>Derived and rebuilt, and it passes <c>05 §3</c>'s test for that classification</b>, for
/// <see cref="CellResidency"/>'s reason: it has no order at all. It is a function from a lattice
/// position to a slot, and both sides of it are saved — the position columns are state, and the slot is
/// where the row already is. A rebuild reproduces it exactly rather than plausibly.
/// </para>
/// <para>
/// <b>It lives beside the table rather than in it, and <c>BOR0901</c> is why that is correct rather
/// than a dodge.</b> The lint rejects storage in a <c>[Table]</c> type that is not a declared column,
/// and this is not per-row storage — it is indexed by lattice position, of which there are
/// <c>Span²</c> whether or not any of them has a row.
/// </para>
/// </remarks>
public sealed class BlockResidency
{
    /// <summary>What <see cref="Slot"/> answers for a lattice square with no row.</summary>
    public const int NotResident = Rows.NoSlot;

    /// <summary>
    /// Slot plus one, so that a zeroed entry reads as <em>absent</em> rather than as slot 0.
    /// </summary>
    /// <remarks>
    /// The same encoding <see cref="CellResidency"/> and <c>IndexList</c> use, for the same reason: the
    /// empty state has to be the default value or the structure needs an initialisation pass that
    /// somebody will forget.
    /// </remarks>
    private int[] _slots = [];

    private int _span;

    private int _count;

    /// <summary>Intersections along one edge, as the array is currently sized for. Zero before any.</summary>
    public int Span => _span;

    /// <summary>How many lattice squares have a row.</summary>
    public int Count => _count;

    /// <summary>Blocks along one edge — one fewer than the intersections, matching <c>StreetGrid</c>.</summary>
    public int Blocks => _span > 0 ? _span - 1 : 0;

    /// <summary>Whether a lattice position is on the lattice this index is sized for.</summary>
    public bool Contains(int column, int row) =>
        column >= 0 && row >= 0 && column < Blocks && row < Blocks;

    /// <summary>
    /// Sizes the index for a lattice of <paramref name="span"/> intersections, clearing it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Called from the road graph's rebuild, because <c>StreetGrid.Span</c> is where the lattice's
    /// extent becomes known</b> — it is zero in a world with no roads and becomes real when the first
    /// Street is laid, so this cannot be sized in a constructor.
    /// </para>
    /// <para>
    /// ⚠ <b>It CLEARS, so every caller owes a repopulation.</b> That is deliberate and it is the same
    /// contract the frontage claim mask has: the index is derived from the rows, so rebuilding it from
    /// a stale array would preserve exactly the entries a rebuild exists to discard.
    /// </para>
    /// </remarks>
    public void Resize(int span)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(span);

        int wanted = span > 1 ? (span - 1) * (span - 1) : 0;

        // Grown rather than reallocated on every rebuild, matching every other index in this
        // namespace: block_tiles is world-creation in practice, so the array is sized once and the
        // comparison costs nothing on the path that does not need it.
        if (_slots.Length < wanted)
        {
            _slots = new int[wanted];
        }
        else
        {
            Array.Clear(_slots, 0, _slots.Length);
        }

        _span = span;
        _count = 0;
    }

    /// <summary>The slot holding a lattice square, or <see cref="NotResident"/> — including off it.</summary>
    /// <remarks>
    /// <b>Off the lattice answers <em>absent</em> rather than throwing</b>, on
    /// <see cref="CellResidency.Slot"/>'s policy: a caller sweeping a neighbourhood reaches past the
    /// edge, and a square that is not there has no row, which is the true answer rather than an error.
    /// </remarks>
    public int Slot(int column, int row) =>
        Contains(column, row) ? _slots[(row * Blocks) + column] - 1 : NotResident;

    /// <summary>Records that <paramref name="slot"/> holds this lattice square.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The square is off the lattice.</exception>
    /// <exception cref="InvalidOperationException">The square already has a row.</exception>
    public void Occupy(int column, int row, int slot)
    {
        if (!Contains(column, row))
        {
            throw new ArgumentOutOfRangeException(
                nameof(column),
                $"Block ({column}, {row}) is off a {Blocks}-block lattice.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        int index = (row * Blocks) + column;

        // Refused rather than overwritten. A second row for one square is the defect this index
        // exists to make impossible, and silently repointing would leave the first row live and
        // unreachable -- which is a leak that nothing would ever report.
        if (_slots[index] != 0)
        {
            throw new InvalidOperationException(
                $"Block ({column}, {row}) already holds slot {_slots[index] - 1}, so slot {slot} "
                + "cannot claim it. A lattice square has at most one row.");
        }

        _slots[index] = slot + 1;
        _count++;
    }

    /// <summary>Forgets the row at a lattice square, if it has one.</summary>
    public void Release(int column, int row)
    {
        if (!Contains(column, row))
        {
            return;
        }

        int index = (row * Blocks) + column;

        if (_slots[index] != 0)
        {
            _slots[index] = 0;
            _count--;
        }
    }
}
