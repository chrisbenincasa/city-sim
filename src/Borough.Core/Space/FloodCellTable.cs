using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell of floodplain. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct FloodCell;

/// <summary>
/// How deep a flood stands on each Cell it reaches — <b>the Hazard Region, as rows.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Hazard Region, <c>01 §5.2</c>, milestone 24 task 9. <b>Ground where a Disaster
/// can occur, derived from terrain at world generation and never WRITTEN during a Tick</b>, so
/// <c>adr/0021</c> holds — ✅ <b>and read during one since <c>plans/0045</c> row 12</b>, by
/// <see cref="DisasterEngine"/>. Its purpose is to make risky land <em>a decision with a posted price rather
/// than an ambush</em>, which needs it visible from Tick zero and needs it to owe nothing to what is
/// standing there.
/// </para>
/// <para>
/// <b>Sparse, and <see cref="Space.WaterCellTable"/>'s reasoning exactly</b> — high ground is in no
/// floodplain by definition, so a dense column would invent an answer for 262,144 Cells to store one
/// for a few thousand. ⚠ <b><see cref="Depth"/> is the whole reason this is a table and not a flag on
/// terrain</b>: <c>01 §5.2</c> spreads Flood *by depth*, and <c>adr/0157</c> stores that depth here
/// precisely so that no height column has to ship.
/// </para>
/// <para>
/// ✅ <b>There is a residency index beside it now — <see cref="FloodResidency"/>, added by
/// <c>plans/0045</c> row 12.</b> This paragraph used to say the absence was deliberate and that
/// ***the task that builds the overlay adds the index***. 🔴 <b>The overlay is STILL unbuilt and the
/// index shipped anyway</b>, because the caller that arrived first was not the one the sentence
/// predicted: a flood spreads by asking each neighbouring Cell <em>are you floodplain, and how
/// deep</em>, which is this question at every step of the walk. ⚠ <b>The prediction named the right
/// mechanism for the wrong reason</b>, which is <c>adr/0093</c>'s failure mode exactly — a sentence
/// about the build was wrong about the <em>trigger</em> and right about everything else.
/// </para>
/// <para>
/// <b>Saved rather than derived, on <see cref="TerrainCellTable"/>'s forced grounds</b>: it is a
/// function of the <c>WorldKey</c>, and a save does not carry the <c>WorldKey</c> back into the
/// generator. ⚠ <b>A world whose Ruleset omits <c>[water] flood_level_percent</c> has NO ROWS here
/// rather than rows of depth zero</b> — <c>adr/0123</c>, and the same distinction
/// <c>WaterRuleset.Stated</c> makes one level up.
/// </para>
/// </remarks>
[Table]
public sealed class FloodCellTable
{
    private readonly Rows<FloodCell> _rows;

    /// <param name="capacity">Initial row count. One row per Cell a flood reaches.</param>
    public FloodCellTable(int capacity)
    {
        _rows = new Rows<FloodCell>("flood_cell", capacity, Buffering.OneCopy);

        East = _rows.Saved<Cells>("east");
        North = _rows.Saved<Cells>("north");
        Depth = _rows.Saved<int>("depth");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<FloodCell> Rows => _rows;

    /// <summary>The Cell's east coordinate.</summary>
    public Column<Cells> East { get; }

    /// <summary>The Cell's north coordinate.</summary>
    public Column<Cells> North { get; }

    /// <summary>
    /// How far the flood level stands above this Cell's ground, in height-field units.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A DEPTH and not an elevation, and the difference is what makes it storable.</b> An
    /// elevation would be a height column wearing another name, which <c>adr/0157</c> refuses; a depth
    /// is the flood level minus the ground and it is meaningless off the floodplain, which is why the
    /// rows stop where they do. <b>Always positive</b> — a Cell at or above the flood level has no
    /// row.
    /// </remarks>
    public Column<int> Depth { get; }

    /// <summary>Records that a flood reaches a Cell, and how deep it stands there.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="depth"/> is not positive.</exception>
    public Handle<FloodCell> Create(Cells east, Cells north, int depth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);

        Handle<FloodCell> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Depth[slot] = depth;

        return handle;
    }

    /// <summary>This table's contribution to a State Hash, folded alone.</summary>
    public ulong Fingerprint()
    {
        ulong hash = 0;
        _rows.FoldAll(ref hash);

        return hash;
    }
}
