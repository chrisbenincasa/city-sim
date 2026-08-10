namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// The world's clock: one row, holding the Tick the world is at.
/// </summary>
/// <remarks>
/// <para>
/// <b>A table with one row, because the Tick is state and state is declared.</b> It lived as a private
/// <c>ulong</c> on the Simulation until three mechanisms had paid for that: slice 8's
/// <see cref="World.Adopt"/> took the Tick as a parameter to compute an arming stagger, slice 9's three
/// Event Wheel checks are each relative to a <em>now</em> the wheel could only reach through its
/// caller, and the end-of-run invariant tier spent both 100,000-Tick acceptance runs stamping every
/// violation <c>Tick 0</c> — because a freshly-built Simulation over an already-run world honestly
/// reported a Tick of zero, and nothing read the stamp closely enough to notice for as long as no
/// invariant was relative to it.
/// </para>
/// <para>
/// <b>The one-row shape is <c>WheelBucketTable</c>'s, and it is a table rather than a field for the same
/// reason:</b> storage beside the columns is what <c>BOR0901</c> is an error for, and a value that must
/// survive a save has to be in the field declaration or the State Hash has a coverage hole. Declaring it
/// <see cref="Rows.Saved{T}"/> is what puts it in both at once.
/// </para>
/// <para>
/// <b>The stored Tick is the <em>next</em> Tick to run, not the last one run.</b> That was
/// <c>Simulation._tick</c>'s convention and it is kept deliberately, so this is a relocation and not a
/// redefinition: <see cref="Simulation.Step"/> reads the Tick, runs it, and advances afterwards. The
/// convention now reaches the hash and the save, which is the one thing moving it costs — and it is
/// stated here rather than left to be re-derived from an increment's position.
/// </para>
/// </remarks>
[Table]
public sealed class ClockTable
{
    private readonly Rows<Clock> _rows;

    /// <summary>Builds the clock at Tick 0, which is the Tick a new world is about to run.</summary>
    public ClockTable()
    {
        _rows = new Rows<Clock>("clock", 1, Buffering.OneCopy);

        Tick = _rows.Saved<Ticks>("tick", Touch.PerTick);

        _rows.Seal();

        // One row for the life of the world, never freed. The allocator is used rather than bypassed so
        // that the row count and the columns agree — WheelBucketTable's reason.
        _rows.Allocate();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Clock> Rows => _rows;

    /// <summary>The Tick the world is about to run.</summary>
    public Column<Ticks> Tick { get; }
}
