using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell currently under water. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct Inundation;

/// <summary>
/// Where the water is standing right now — <b>a Disaster's footprint, as rows.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Disaster: <em>"a sudden perturbation with a bounded footprint of Tiles"</em>.
/// <b>This is that footprint, and it is the one thing here that changes during a Tick</b> —
/// <see cref="FloodCellTable"/> is where a flood <em>could</em> reach and is written once by the
/// generator; these rows are where one <em>has</em> reached and they come and go.
/// </para>
/// <para>
/// <b>The footprint is stored rather than derived, and the reason is that it is a walk and not a
/// predicate.</b> A Cell is under water when it is in the Hazard Region, below the current surge,
/// <em>and connected to the seed through Cells that are both</em>. The first two are a test; the
/// third is a flood fill, and re-running it every Tick to answer a question the last Tick already
/// answered is the shape <c>02 §10</c> calls a whole-world pass at write-site frequency.
/// </para>
/// <para>
/// ⚠ <b>Rows are freed as the water leaves, so this is a collection <em>with</em> a sink</b> —
/// <c>adr/0006</c>, and a floodplain is exactly the kind of place where a missing one would not
/// show up for a hundred thousand Ticks. The sink is <c>[disasters] flood_recedes_over_days</c>
/// and it is a duration, so it cannot be outrun by the interval without an author saying so.
/// </para>
/// </remarks>
[Table]
public sealed class InundationTable
{
    private readonly Rows<Inundation> _rows;

    /// <param name="capacity">Initial row count. One row per Cell under water.</param>
    /// <param name="disasters">The Disasters a row can belong to.</param>
    public InundationTable(int capacity, Rows<Disaster> disasters)
    {
        _rows = new Rows<Inundation>("inundation", capacity, Buffering.OneCopy);

        East = _rows.Saved<Cells>("east");
        North = _rows.Saved<Cells>("north");
        Depth = _rows.Saved<int>("depth");
        Since = _rows.Saved<Ticks>("since");
        Cause = _rows.SavedHandle("cause", disasters);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Inundation> Rows => _rows;

    /// <summary>The Cell's east coordinate.</summary>
    public Column<Cells> East { get; }

    /// <summary>The Cell's north coordinate.</summary>
    public Column<Cells> North { get; }

    /// <summary>
    /// The Hazard Region depth of this Cell, copied so the drain does not have to ask twice.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A copy of <see cref="FloodCellTable.Depth"/> and not a second fact</b>, which is a
    /// denormalisation and is worth naming as one. The recession walks these rows deciding which
    /// have come back above the surge, and the alternative is a residency lookup per row per drain
    /// step for a number that cannot change: the Hazard Region is generator output and is never
    /// written in a Tick.
    /// </remarks>
    public Column<int> Depth { get; }

    /// <summary>The Tick the water reached this Cell.</summary>
    public Column<Ticks> Since { get; }

    /// <summary>The Disaster that put it here.</summary>
    public HandleColumn<Disaster> Cause { get; }

    /// <summary>Records that water has reached a Cell.</summary>
    public Handle<Inundation> Create(
        Cells east, Cells north, int depth, Ticks since, Handle<Disaster> cause)
    {
        Handle<Inundation> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;
        Depth[slot] = depth;
        Since[slot] = since;
        Cause[slot] = cause;

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
