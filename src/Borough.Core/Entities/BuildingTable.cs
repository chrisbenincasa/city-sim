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
        WorkerHead = _rows.Derived<int>("worker_head");
        WorkerTail = _rows.Derived<int>("worker_tail");
        BusinessHead = _rows.Derived<int>("business_head");
        BusinessTail = _rows.Derived<int>("business_tail");
        BinHead = _rows.Derived<int>("bin_head", Touch.PerTick);
        BinTail = _rows.Derived<int>("bin_tail");
        RuleHead = _rows.Derived<int>("rule_head");
        RuleTail = _rows.Derived<int>("rule_tail");

        CellNext = _rows.Derived<int>("cell_next");

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

    /// <summary>
    /// Head of the worker list — see <see cref="CitizenTable.WorkerNext"/>.
    /// </summary>
    /// <remarks>
    /// <b>The Building-to-workers reverse index <see cref="CitizenTable.Workplace"/> was declared
    /// without</b>, and its absence is why that handle is <see cref="Tables.Reference.Severable"/>.
    /// It is the occupant list on a second axis and it is derived for the same reason: which
    /// Citizens work here follows from their own saved <c>workplace</c> handles, so nothing here is
    /// state.
    /// </remarks>
    public Column<int> WorkerHead { get; }

    /// <summary>Tail of the worker list, so a Citizen appends rather than push-fronts.</summary>
    public Column<int> WorkerTail { get; }

    /// <summary>
    /// Head of the Business list — see <see cref="BusinessTable.BuildingNext"/>.
    /// </summary>
    /// <remarks>
    /// <b>A second Occupant list rather than a polymorphic one</b> (<c>adr/0113</c>). A Household and
    /// a Business are both Occupants and are different row types, so <c>CONTEXT.md</c> → Occupant is a
    /// concept spanning two lists and every handle here stays typed — which is what lint 7 wants and
    /// what <see cref="WorkerHead"/> already established as the shape.
    /// </remarks>
    public Column<int> BusinessHead { get; }

    /// <summary>Tail of the Business list, so a Business appends rather than push-fronts.</summary>
    public Column<int> BusinessTail { get; }

    /// <summary>
    /// Head of this Building's Bins — see <see cref="Rules.BinTable.BinNext"/>.
    /// </summary>
    /// <remarks>
    /// <b>Derived, where the wait list hanging off each of those Bins is not.</b> Which Bins a
    /// Building has is a pure function of the Bins' own <c>owner</c> column and the order carries no
    /// meaning, a lookup by Resource being a search either way. A wait list's order is arrival order,
    /// which is recoverable from nothing, so it is state. The two calls sit one indirection apart and
    /// go opposite ways, which is worth noticing rather than assuming.
    /// </remarks>
    public Column<int> BinHead { get; }

    /// <summary>Tail of the Bin list.</summary>
    public Column<int> BinTail { get; }

    /// <summary>Head of the Rule Instances this Building runs.</summary>
    public Column<int> RuleHead { get; }

    /// <summary>Tail of the Rule Instance list.</summary>
    public Column<int> RuleTail { get; }

    /// <summary>
    /// The next Building in the same Cell. The element side of
    /// <see cref="Space.BuildingResidency"/>.
    /// </summary>
    /// <remarks>
    /// <b>The head is not here and could not be.</b> This list's owner is a <em>Cell</em>, not a row,
    /// so its head and tail live in flat arrays beside the table — see
    /// <see cref="Space.BuildingResidency"/> for why that is the correct shape rather than a dodge.
    /// What is per-row is the threading, and that is this column.
    /// </remarks>
    public Column<int> CellNext { get; }

    /// <summary>Allocates a Building on a Lot.</summary>
    /// <summary>Allocates a Building on a Lot, and records it on the Lot.</summary>
    /// <param name="lots">
    /// The Lot table, so that the reverse index is written in the same call as the forward handle.
    /// </param>
    /// <param name="lot">The Lot to stand on. A default handle makes a Building on no Lot.</param>
    /// <param name="kind">Which Building kind.</param>
    /// <remarks>
    /// <para>
    /// <b>The Lot table is a parameter because the relation has two ends and this writes both.</b>
    /// Slice 10 gave <see cref="LotTable.BuildingSlot"/> the reverse of <see cref="Lot"/>, and a
    /// <c>Create</c> that wrote only the forward handle would leave every caller responsible for the
    /// other end — which is the arrangement that produced <c>02 §2.2</c>'s invariant being
    /// unenforceable for four slices.
    /// </para>
    /// <para>
    /// <b>It is not held as a field, and that is <c>BOR0901</c> rather than taste.</b> A <c>[Table]</c>
    /// type may hold declared columns and its own <c>Rows</c> and nothing else, so the reference
    /// arrives per call. The constructor already takes the same table to declare
    /// <see cref="Lot"/> against it, so the coupling is not new — only the enforcement is.
    /// </para>
    /// </remarks>
    public Handle<Building> Create(LotTable lots, Handle<Lot> lot, byte kind)
    {
        ArgumentNullException.ThrowIfNull(lots);

        Handle<Building> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Lot[slot] = lot;
        Kind[slot] = kind;

        // A default or stale handle leaves the Building on no Lot, which the fixtures use
        // deliberately and the whole-world tier reports rather than this silently inventing a Lot.
        if (lots.Rows.TryResolve(lot, out int lotSlot))
        {
            lots.Occupy(lotSlot, slot);
        }

        return handle;
    }
}
