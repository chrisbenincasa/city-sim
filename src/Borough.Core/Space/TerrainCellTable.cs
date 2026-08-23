using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell's worth of terrain. Empty for the reason <see cref="LayerCell"/> is empty.
/// </summary>
public readonly struct TerrainCell;

/// <summary>
/// What sort of ground every Cell is — <b>dense, one row per Cell, written once at world creation.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0157</c>, milestone 24 task 2. <see cref="TerrainGenerator.LayInto"/> fills it; nothing in a
/// Tick writes it, because there is no terraforming.
/// </para>
/// <para>
/// <b>A table of its own rather than a column on <see cref="LayerCellTable"/>, and the split is the
/// decision.</b> Those two tables are per-Cell alike and their <em>lifetimes</em> are opposite. A
/// Layer row means <em>something happened here</em> — an emission, a plume, a Seal — so the table is
/// sparse by design and every Layer pass is <c>O(live rows)</c>. <b>Terrain is dense by nature</b>:
/// every Cell is made of something on the Tick the world is made. Putting a dense fact in the sparse
/// table would have made the sparse table dense, which is not a storage change but a <em>cost</em>
/// change to four passes that never asked about terrain.
/// </para>
/// <para>
/// <b>It was measured rather than assumed</b> (<c>adr/0043</c>). Ensuring whole-map Cell residency on
/// <c>minimal.toml</c> took the land value target pass from about <b>2.5 ms</b> to about <b>114 ms</b>
/// on the one Tick in 256 it fires — ⚠ <b>a figure taken on a machine that was not quiet and which no
/// document may quote</b> (<c>adr/0106</c>); what it is good for is the ratio and the sign, which is
/// the one thing a spoiled reading still settles. <c>plans/0041</c> <b>F7</b> found the same wire from
/// the other side, and <b>F7's own 88 seconds is not this cost and must not be quoted as one.</b>
/// </para>
/// <para>
/// <b>The slot IS <see cref="CellGrid.Index"/>, which is what makes this dense rather than merely
/// large.</b> Every row is allocated in the constructor, in index order, and none is ever freed — so
/// there is no residency index, no <c>Ensure</c>, and no east/north columns to store a coordinate the
/// position already carries. <see cref="Borough.Core.Entities.TreasuryTable"/>'s single constructor-allocated row
/// is the precedent for the allocation shape; this is that shape at map scale.
/// </para>
/// <para>
/// <b><c>(saved AND hashed)</c>, and the ordinals move the State Hash</b> — appending a sixth
/// <see cref="TerrainKind"/> is free, renumbering the five is a re-baseline. <b>One byte a Cell</b>,
/// so the whole map is <see cref="CellGrid.WorldCellCount"/> bytes of terrain and the memory argument
/// nobody will find here is the same one <see cref="LayerCellTable"/> declines to make.
/// </para>
/// <para>
/// ⚠ <b>Terrain height is not here and is not anywhere</b> (<c>adr/0156</c>). The generator computes a
/// height, reads it while choosing these, and keeps only the choice.
/// </para>
/// </remarks>
[Table]
public sealed class TerrainCellTable
{
    private readonly Rows<TerrainCell> _rows;

    /// <summary>Allocates one row per Cell, in <see cref="CellGrid.Index"/> order.</summary>
    /// <remarks>
    /// <b>Every row, here, once.</b> The table has no <c>Create</c> and no growth path on purpose: a
    /// Cell that could be missing would need a residency index, and a residency index is the thing
    /// this table exists to not have.
    /// </remarks>
    public TerrainCellTable()
    {
        _rows = new Rows<TerrainCell>("terrain_cell", CellGrid.WorldCellCount);

        Kind = _rows.Saved<TerrainKind>("kind");

        _rows.Seal();

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            _rows.Allocate();
        }
    }

    /// <summary>The allocator, for the State Hash and the save.</summary>
    public Rows<TerrainCell> Rows => _rows;

    /// <summary>
    /// What sort of ground this Cell is. <b>The only terrain the world stores.</b>
    /// </summary>
    /// <remarks>
    /// <b>The type and not a value.</b> Base Fertility is Ruleset data keyed by this
    /// (<c>adr/0154</c>, <see cref="Rules.TerrainRuleset.BaseFertility"/>), so a designer retunes what
    /// ground is worth without touching a save. Storing the number instead would freeze the Ruleset in
    /// force at world creation into every world ever made from it, which is <c>adr/0015</c>'s whole
    /// complaint about a <c>const</c>. Task 4's Sealing decay rate keys off this column for the same
    /// reason.
    /// </remarks>
    public Column<TerrainKind> Kind { get; }

    /// <summary>What sort of ground one Cell is.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The Cell is off the map.</exception>
    public TerrainKind At(Cells east, Cells north) => Kind[Slot(east, north)];

    /// <summary>Sets what sort of ground one Cell is. <b>World creation only.</b></summary>
    /// <exception cref="ArgumentOutOfRangeException">The Cell is off the map.</exception>
    public void Set(Cells east, Cells north, TerrainKind kind) => Kind[Slot(east, north)] = kind;

    /// <summary>The row for a Cell, which is its index and never a lookup.</summary>
    private static int Slot(Cells east, Cells north)
    {
        if (!CellGrid.Contains(east, north))
        {
            throw new ArgumentOutOfRangeException(
                nameof(east),
                $"Cell ({east.Raw}, {north.Raw}) is off a {CellGrid.WorldCells}-Cell map.");
        }

        return CellGrid.Index(east, north);
    }
}
