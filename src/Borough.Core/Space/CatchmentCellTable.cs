using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>One Cell's worth of catchment. Empty for the reason <see cref="WoodlandCell"/> is.</summary>
public readonly struct CatchmentCell;

/// <summary>
/// Which Water Body each Cell drains into — <b>dense, one row per Cell, and it is what lets anything
/// on dry land name a river at all.</b>
/// </summary>
/// <remarks>
/// <para>
/// Milestone 24 task 6b. <c>02 §2.4</c> gives water pollution the sources <em>"dumping, runoff"</em>,
/// and both of those happen on <b>dry</b> ground. 🔴 <b>Task 6a built the wet half of the water graph
/// and nothing else</b> — <see cref="WaterResidency.Of"/> answers <em>which body is this Cell part
/// of</em> and answers it only for a Cell that is under water. ***A Building stands on dry land, so
/// before this table there was no expression in the whole build that named the river a Building
/// fouls.*** Without it a Water Body's Bin would be storage nothing could reach, permanently zero,
/// which is exactly the <em>present and permanently zero</em> failure <c>adr/0123</c> exists to
/// prevent.
/// </para>
/// <para>
/// <b>Dense, because the question is asked about ground rather than about water.</b> Wet Cells are
/// sparse and get a residency index; every Cell has a catchment, including the wet ones, whose
/// catchment is the body they are part of. It is <see cref="WoodlandCellTable"/>'s shape for
/// <see cref="TerrainCellTable"/>'s reason, and the slot <b>is</b> <see cref="CellGrid.Index"/>.
/// </para>
/// <para>
/// <b>Saved rather than derived, on the same forced grounds as <see cref="TerrainCellTable"/> and
/// <see cref="WoodlandCellTable.Potential"/>.</b> It is a function of the <c>WorldKey</c>'s height
/// field, so it looks derivable — and <c>World</c>'s own note on its table list says a save does not
/// carry the <c>WorldKey</c> back into the generator. ***A column nothing can rebuild is not derived
/// state, however cheap its formula looks.***
/// </para>
/// <para>
/// ⚠ <b><c>default</c> means <em>drains nowhere</em> and it is a real answer.</b> A Cell in a local
/// minimum with no wet Cell downhill of it sheds its water into ground that goes nowhere — the dry
/// counterpart of the endorheic basin <see cref="WaterBodyTable.Downstream"/> already spells the same
/// way. ⚠ <b>It also covers every Cell on a world with no <c>[water]</c> at all</b>, which is an
/// inland city and a legitimate world (<c>adr/0160</c>).
/// </para>
/// </remarks>
[Table]
public sealed class CatchmentCellTable
{
    private readonly Rows<CatchmentCell> _rows;

    /// <param name="bodies">The table <see cref="Body"/> handles are resolved against.</param>
    public CatchmentCellTable(WaterBodyTable bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);

        _rows = new Rows<CatchmentCell>("catchment_cell", CellGrid.WorldCellCount);

        Body = _rows.SavedHandle("body", bodies.Rows);

        _rows.Seal();

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            _rows.Allocate();
        }
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<CatchmentCell> Rows => _rows;

    /// <summary>
    /// The Water Body this Cell's runoff reaches, or <c>default</c> for <b>drains nowhere</b>.
    /// </summary>
    public HandleColumn<WaterBody> Body { get; }

    /// <summary>Which Water Body this Cell drains into. <c>default</c> is <b>nowhere</b>.</summary>
    public Handle<WaterBody> At(Cells east, Cells north) => Body[Slot(east, north)];

    /// <summary>Records where a Cell's runoff goes. <b>World creation only.</b></summary>
    public void DrainsTo(Cells east, Cells north, Handle<WaterBody> body) =>
        Body[Slot(east, north)] = body;

    /// <summary>The fold of every column, for the generator-output guard.</summary>
    public ulong Fingerprint()
    {
        ulong hash = 0;
        _rows.FoldAll(ref hash);

        return hash;
    }

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
