namespace Borough.Core.Entities;

using Borough.Core.Tables;

/// <summary>
/// Structures on Lots. Owns the intrusive list of Households living in each.
/// </summary>
/// <remarks>
/// <b>The occupant list is <see cref="Disposition.Derived"/>, and that is a claim worth reading as
/// one.</b> It says the list is a pure function of saved state — every Household's <c>dwelling</c>
/// handle — so it is neither written to the save nor folded into the State Hash, and
/// <see cref="World.RebuildDerived"/> reconstructs it. The claim is checkable and is checked: rebuild
/// it and the hash must not move. A derived field that is <em>not</em> a pure function of saved state
/// is a divergence the hash has been told to ignore, which is the one way back into the defect the
/// single declaration exists to close.
/// </remarks>
[Table]
public sealed class BuildingTable
{
    private readonly Rows<Building> _rows;

    /// <param name="capacity">Initial slot count. ~150 Buildings per 1,000 Citizens, per S4 task 2.</param>
    /// <param name="lots">The table this one's <see cref="Lot"/> handles address.</param>
    public BuildingTable(int capacity, LotTable lots)
    {
        ArgumentNullException.ThrowIfNull(lots);

        _rows = new Rows<Building>("building", capacity, Buffering.OneCopy);

        Lot = _rows.SavedHandle("lot", lots.Rows);
        Kind = _rows.Saved<byte>("kind");
        OccupantHead = _rows.Derived<int>("occupant_head");
        OccupantTail = _rows.Derived<int>("occupant_tail");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Building> Rows => _rows;

    /// <summary>The Lot this Building stands on.</summary>
    public HandleColumn<Lot> Lot { get; }

    /// <summary>Which Building kind. Resolved through the Ruleset.</summary>
    public Column<byte> Kind { get; }

    /// <summary>Head of the occupant list — see <see cref="HouseholdTable.DwellingNext"/>.</summary>
    public Column<int> OccupantHead { get; }

    /// <summary>Tail of the occupant list, so a Household appends rather than push-fronts.</summary>
    public Column<int> OccupantTail { get; }

    /// <summary>Allocates a Building on a Lot.</summary>
    public Handle<Building> Create(Handle<Lot> lot, byte kind)
    {
        Handle<Building> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Lot[slot] = lot;
        Kind[slot] = kind;

        return handle;
    }
}
