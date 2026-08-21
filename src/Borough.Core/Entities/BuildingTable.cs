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

        // A Building has at most one Car Park, so this is a slot rather than a list head -- the Lot's
        // reverse index shape and not the Bins'. Derived for BinHead's reason: it is reproducible
        // from CarParkTable.Owner, which is saved, so storing it twice would let the two disagree.
        CarPark = _rows.Derived<int>("car_park");

        CellNext = _rows.Derived<int>("cell_next");

        // The gate's daily throughput meter. Saved rather than derived, because how many crossed
        // today is not reproducible from anything else -- a reload that reset it would let a gate
        // admit its whole quota twice in one Day, and the Factorio test is where that would surface.
        ArrivalsToday = _rows.Saved<int>("arrivals_today");
        ArrivalDay = _rows.Saved<int>("arrival_day");

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
    /// This Building's Car Park slot, <b>plus one</b>, or zero for a Building that has none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One rather than a list</b>, because <c>[[building]] parking</c> is one number per kind. A
    /// Segment-held Car Park (<c>adr/0120</c>, not built) has no Building at all and would be found
    /// spatially rather than through here, so this column does not have to grow for it.
    /// </para>
    /// <para>
    /// <b>Plus one, for <c>LotTable.BuildingSlot</c>'s reason exactly.</b> A freshly allocated or
    /// freed row is zero-filled, so a <see cref="Rows.NoSlot"/> sentinel would make every Building
    /// with no parking read as owning <em>Car Park slot 0</em> — one real Car Park claimed by the
    /// whole city, with every hash moving and every test passing. Read it through
    /// <see cref="HasCarPark"/> and <see cref="CarParkOf"/>; the encoding is not meant to travel.
    /// </para>
    /// </remarks>
    public Column<int> CarPark { get; }

    /// <summary>Whether this Building has a Car Park at all.</summary>
    public bool HasCarPark(int slot) => CarPark[slot] != 0;

    /// <summary>
    /// This Building's Car Park slot, or <see cref="Rows.NoSlot"/> if it has none.
    /// </summary>
    public int CarParkOf(int slot) => CarPark[slot] - 1;

    /// <summary>Records that this Building's parking lives in <paramref name="carParkSlot"/>.</summary>
    internal void AttachCarPark(int slot, int carParkSlot) => CarPark[slot] = carParkSlot + 1;

    /// <summary>Records that this Building has no Car Park.</summary>
    internal void DetachCarPark(int slot) => CarPark[slot] = 0;

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

    /// <summary>
    /// How many Households have crossed this gate on the Day <see cref="ArrivalDay"/> names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what makes <c>[[building]] arrivals_per_day</c> bind rather than merely be
    /// declared</b> (<c>adr/0088</c>, <c>plans/0035</c> decision 9). The ceiling is a <em>rate</em>,
    /// so meeting it needs a count and the period the count belongs to; a bound applied per call
    /// would let two arrival events in one Tick each take the whole quota, which is a mechanism that
    /// looks like a daily ceiling and is not one.
    /// </para>
    /// <para>
    /// <b>Zero on every Building that is not a gate, and on every gate in every Ruleset that declares
    /// none.</b> A kind is an Outside Connection precisely when it states the key
    /// (<c>World.IsOutsideConnection</c>), so nine of the ten shipped Rulesets never advance either
    /// column.
    /// </para>
    /// </remarks>
    public Column<int> ArrivalsToday { get; }

    /// <summary>
    /// Which Day <see cref="ArrivalsToday"/> counts, so the meter resets without a sweep.
    /// </summary>
    /// <remarks>
    /// <b>A stored period rather than a scheduled reset, and the choice is about what it costs to be
    /// wrong.</b> A per-Day pass clearing every gate is <c>O(Buildings)</c> for a column almost every
    /// row leaves at zero, and it puts the meter's correctness in a phase that has to run — so a gate
    /// created between the reset and the arrival, or a load that lands mid-Day, reads a count from a
    /// Day that has passed. Comparing the stored Day at the read site cannot be skipped, costs
    /// nothing on a Building nobody arrives at, and is right across a save.
    /// </remarks>
    public Column<int> ArrivalDay { get; }

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
