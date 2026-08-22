using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// The Districts: one row per centre of Building density that the watershed found.
/// </summary>
/// <remarks>
/// <para>
/// <b>A row is a centre, and the extent is somewhere else</b> — <c>adr/0134</c>, whose whole claim is
/// that a District <em>is</em> a centre and its basin, so the count follows centres rather than a
/// ceiling on area. What that means structurally is that this table is small and boring: the
/// interesting state is the membership in <see cref="DistrictCellTable"/>, and a District with no
/// Cells is not a thing this table is allowed to hold.
/// </para>
/// <para>
/// <b>There is no <c>DistrictId</c> type and that is on purpose.</b> The plan named one; the project
/// already has exactly one identity for a table row, which is <see cref="Handle{T}"/> over a monotonic
/// never-reused id, and it is the identity the State Hash folds through
/// <see cref="Rows{T}.SavedHandle"/>. A second spelling of <em>which District</em> would be a second
/// thing to keep in step across a save, a reload and a re-evaluation, for no capability.
/// </para>
/// <para>
/// <b>The centre is stored rather than recomputed</b>, and it is not redundant with the membership.
/// The watershed picks the centre <em>while</em> flooding — it is the Cell the basin drained to — and
/// after the flood there may be several Cells in the District sharing that peak density. Recovering
/// which one was the seed from the finished membership would need a tie-break rule, which is a second
/// place for the answer to live.
/// </para>
/// <para>
/// ⚠ <b>No <c>Peak</c> column, deliberately.</b> The density at the centre is
/// <see cref="BuildingResidency.Density"/> at the centre Cell, which is one lookup away, and a copy of
/// it here would be a saved number that goes stale the moment a Building is raised.
/// </para>
/// </remarks>
[Table]
public sealed class DistrictTable
{
    private readonly Rows<District> _rows;

    /// <param name="capacity">Initial row count. A city has few Districts and they arrive slowly.</param>
    public DistrictTable(int capacity)
    {
        _rows = new Rows<District>("district", capacity, Buffering.OneCopy);

        CentreEast = _rows.Saved<Cells>("centre_east");
        CentreNorth = _rows.Saved<Cells>("centre_north");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<District> Rows => _rows;

    /// <summary>The east coordinate of the Cell the basin drains to.</summary>
    public Column<Cells> CentreEast { get; }

    /// <summary>The north coordinate of the Cell the basin drains to.</summary>
    public Column<Cells> CentreNorth { get; }

    /// <summary>Opens a District at a centre Cell.</summary>
    public Handle<District> Create(Cells east, Cells north)
    {
        Handle<District> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        CentreEast[slot] = east;
        CentreNorth[slot] = north;

        return handle;
    }
}
