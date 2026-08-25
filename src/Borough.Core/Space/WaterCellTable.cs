using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell's membership of a Water Body. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct WaterCell;

/// <summary>
/// Which Water Body each wet Cell belongs to, stored sparsely — a body's extent, as rows.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0034</c>, milestone 24 task 6a. <b>Sparse, on <see cref="DistrictCellTable"/>'s reasoning
/// exactly</b>: dry ground is in no Water Body <em>by definition</em>, and a dense column would have
/// to invent an answer for all 262,144 Cells of the world grid, most of which are not wet.
/// </para>
/// <para>
/// ⚠ <b>It is sparse where <see cref="WoodlandCellTable"/> and <see cref="TerrainCellTable"/> are
/// dense, and the difference is not a preference.</b> Every Cell has a terrain type and nearly every
/// Cell has some Woodland, so a dense row is what those quantities <em>are</em>. Water is a minority
/// of the map by construction. ***The question is never how much a table costs; it is whether the
/// absent case is a value or an absence.***
/// </para>
/// <para>
/// <b>The dense half is <see cref="WaterResidency"/> beside it</b>, which is <c>(derived AND
/// rebuilt)</c> in <see cref="CellResidency"/>'s exact sense: a function from a coordinate to a slot,
/// with both sides of it saved.
/// </para>
/// </remarks>
[Table]
public sealed class WaterCellTable
{
    private readonly Rows<WaterCell> _rows;

    /// <param name="capacity">Initial row count. One row per wet Cell.</param>
    /// <param name="bodies">The table <see cref="Body"/> handles are resolved against.</param>
    public WaterCellTable(int capacity, WaterBodyTable bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);

        _rows = new Rows<WaterCell>("water_cell", capacity, Buffering.OneCopy);

        East = _rows.Saved<Cells>("east");
        North = _rows.Saved<Cells>("north");
        Body = _rows.SavedHandle("body", bodies.Rows);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<WaterCell> Rows => _rows;

    /// <summary>The Cell's east coordinate. Saved, because residency is rebuilt from it.</summary>
    public Column<Cells> East { get; }

    /// <summary>The Cell's north coordinate.</summary>
    public Column<Cells> North { get; }

    /// <summary>The Water Body covering this Cell.</summary>
    public HandleColumn<WaterBody> Body { get; }

    /// <summary>Records that a Cell is covered by a Water Body.</summary>
    public Handle<WaterCell> Create(Cells east, Cells north, Handle<WaterBody> body)
    {
        Handle<WaterCell> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Body[slot] = body;

        return handle;
    }
}
