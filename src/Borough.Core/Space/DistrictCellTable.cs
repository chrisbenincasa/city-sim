using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell's membership of a District. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct DistrictCell;

/// <summary>
/// Which District each built Cell belongs to, stored sparsely — the District's extent, as rows.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sparse and built-Cells-only</b>, on <see cref="LayerCellTable"/>'s reasoning and one of its own.
/// The Layer tables are sparse because an undeveloped region is a null; this is sparse because an
/// undeveloped region is not in any District <em>by definition</em>. <c>adr/0134</c> makes a District
/// a centre and the basin that drains to it, and empty ground drains nowhere: it holds no Building, so
/// it has no Bin, no Provider and nothing to pool. A dense partition of the map would have to invent
/// an answer for all 262,144 Cells of the world grid, nearly every one of which holds nothing.
/// </para>
/// <para>
/// <b>The membership hangs off the Cell rather than off the Building</b>, and that is the difference
/// between one row per Cell and one column on every Building. A Building's District is its Cell's
/// District — <see cref="DistrictResidency.Of"/> — so there is one place to write when a boundary
/// moves at task 4, and a Building that is raised into an existing District needs no write at all.
/// </para>
/// <para>
/// ⚠ <b><c>(saved AND hashed)</c>, matching <see cref="DistrictTable"/> and for its reason.</b> The
/// dense index beside it (<see cref="DistrictResidency"/>) is the derived half, and it is derived in
/// <see cref="CellResidency"/>'s exact sense: a function from a coordinate to a slot, with both sides
/// of it saved.
/// </para>
/// </remarks>
[Table]
public sealed class DistrictCellTable
{
    private readonly Rows<DistrictCell> _rows;

    /// <param name="capacity">Initial row count. One row per built Cell.</param>
    /// <param name="districts">The table <see cref="District"/> handles are resolved against.</param>
    public DistrictCellTable(int capacity, DistrictTable districts)
    {
        ArgumentNullException.ThrowIfNull(districts);

        _rows = new Rows<DistrictCell>("district_cell", capacity, Buffering.OneCopy);

        East = _rows.Saved<Cells>("east");
        North = _rows.Saved<Cells>("north");
        District = _rows.SavedHandle("district", districts.Rows);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<DistrictCell> Rows => _rows;

    /// <summary>The Cell's east coordinate. Saved, because residency is rebuilt from it.</summary>
    public Column<Cells> East { get; }

    /// <summary>The Cell's north coordinate.</summary>
    public Column<Cells> North { get; }

    /// <summary>The District this Cell drains to.</summary>
    public HandleColumn<District> District { get; }

    /// <summary>Records that a Cell belongs to a District.</summary>
    public Handle<DistrictCell> Create(Cells east, Cells north, Handle<District> district)
    {
        Handle<DistrictCell> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        District[slot] = district;

        return handle;
    }
}
