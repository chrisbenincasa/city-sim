namespace Borough.Core.Entities;

using Borough.Core.Quantities;
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

        // When the city abandoned this Building, or default if it did not. SAVED AND NOT DERIVED, and
        // the reason is the whole of what separates this state from dereliction.
        //
        // CONTEXT.md -> Derelict is derived on purpose: `Kind == 0` is read off the Ruleset in force,
        // so a reload that describes the kind again RECOVERS the Building, which is what a designer
        // balancing needs because their commonest move is undo (adr/0057).
        //
        // Abandonment has the opposite requirement. It is what the CITY did, over a duration, and a
        // reload must not undo it -- but a reload re-runs Fit, so anything derived from `has a kind
        // and holds no Rules` would resurrect every abandoned shell in the world the first time a
        // designer touched the Ruleset. So it is recorded, and the two states cannot share a
        // representation for the same reason CONTEXT.md:313 says they share no machinery.
        //
        // A Ticks rather than a flag, because 02 5.9 wants the condition retained ON the Building and
        // `how long has this stood empty` is the question the contagion term and the clearance Policy
        // both ask. Zero reads as standing, which is what a zero-filled row should mean.
        AbandonedSince = _rows.Saved<Ticks>("abandoned_since", Touch.Cold);

        // When this Building last held nobody, or default while somebody lives here. SAVED for
        // AbandonedSince's reason exactly: it is a duration the city is part-way through, and a
        // reload that reset it would restart every empty Building's clock at zero.
        //
        // ⚠ IT COUNTS HOUSEHOLDS AND NOT TENANTS, which is the one thing about this column that will
        // be misread. `adr/0147` made `occupants` count tenants of any kind, so a dwelling that comes
        // with a trade (`adr/0148`) holds a Business from the moment it is raised and NEVER has zero
        // tenants -- a tenant-counting clock would be permanently unarmed in every shipped world that
        // declares `business`, which is most of them. The question this column asks is *does anybody
        // LIVE here*, and a shop is not a resident.
        //
        // 🔴 PLUS ONE, WHICH IS `CarPark`'S ENCODING AND NOT `AbandonedSince`'S -- AND THE FIRST
        // SPELLING TOOK THE WRONG NEIGHBOUR'S. A zero-filled row has to read as OCCUPIED, and a
        // Building is empty from the Tick it is raised (adr/0069: construction houses nobody), so
        // zero-as-sentinel loses every Building raised on TICK 0. That is not a corner: it is every
        // fixture in the suite and every Building SyntheticCity lays. It was invisible on the shipped
        // world only because the populator fills what it raises in the same call.
        //
        // The encoding does not travel. MarkEmpty, MarkOccupied and HasStoodEmptyFor below are the
        // whole interface, and the comparison is made in ENCODED space -- `now + 1 >= stored + d` is
        // `now >= since + d` -- because Ticks refuses a subtraction operator on purpose.
        EmptySince = _rows.Saved<Ticks>("empty_since", Touch.Cold);

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

    /// <summary>Whether the city has abandoned this Building — see <see cref="AbandonedSince"/>.</summary>
    /// <remarks>
    /// An abandoned Building still stands and still holds its Lot. What it no longer holds is
    /// Occupants, Rules or Bins, so it has nothing left to fail at and accumulates no further
    /// pressure — the shell outlives what killed it, which is what <c>02 §5.9</c> needs in order to
    /// retain the condition on the Building and what <c>adr/0091</c>'s clearance acts on.
    /// </remarks>
    public bool IsAbandoned(int slot) => AbandonedSince[slot] != default;

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

    /// <summary>
    /// The Tick the city abandoned this Building on, or <c>default</c> if it still stands in use.
    /// </summary>
    /// <remarks>
    /// <b>Abandonment is what the city does to a Building; dereliction is what a Ruleset edit does to
    /// one, and they share no machinery</b> (<c>CONTEXT.md</c>:313). Do not read this column to answer
    /// <i>is this Building derelict</i> — that is <see cref="Kind"/> being undeclared, it is derived,
    /// and a reload recovers it. This one is recorded and a reload must not.
    /// </remarks>
    public Column<Ticks> AbandonedSince { get; }

    /// <summary>
    /// The Tick this Building last stopped housing anybody, or <c>default</c> while somebody lives
    /// here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The clock under <c>[[building]] abandoned_when_empty_after_days</c></b> — a dwelling nobody
    /// moves into is the sink <c>02 §5.5</c> names as redevelopment's floor, <i>the case where nobody
    /// wants the land</i>. It is the mirror of <c>adr/0069</c>'s build predicate: a developer builds
    /// while the Unplaced Pool is non-empty and gives up on a Building the Pool never came for.
    /// </para>
    /// <para>
    /// ⚠ <b>Households, not tenants.</b> See the constructor: a dwelling declaring
    /// <c>business</c> holds a trade from the Tick it is raised, so a clock keyed on
    /// <see cref="World.Tenants"/> would never start in the worlds that need it.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero reads as occupied</b>, which is <see cref="AbandonedSince"/>'s convention and carries
    /// its one cost: a Building emptied on Tick 0 reads as occupied until the next Household leaves
    /// it. Nothing in the build can empty a Building on Tick 0 — the populator fills what it raises in
    /// the same call — so the case is unreachable rather than tolerated, and the alternative was a
    /// plus-one encoding on a quantity that is compared rather than indexed.
    /// </para>
    /// </remarks>
    public Column<Ticks> EmptySince { get; }

    /// <summary>Starts this Building's empty clock at <paramref name="now"/>.</summary>
    internal void MarkEmpty(int slot, Ticks now) => EmptySince[slot] = now + new Ticks(1);

    /// <summary>Stops this Building's empty clock, because somebody lives here.</summary>
    internal void MarkOccupied(int slot) => EmptySince[slot] = default;

    /// <summary>
    /// Whether this Building has housed nobody for at least <paramref name="ticks"/>, as of
    /// <paramref name="now"/>.
    /// </summary>
    /// <remarks>
    /// <b>A predicate rather than an accessor, so the plus-one encoding stays in this file.</b> The
    /// constructor says why the encoding is needed; a caller that read the column and compared it
    /// itself would be off by one Tick and right about everything else, which is the class of defect
    /// that survives every test.
    /// </remarks>
    public bool HasStoodEmptyFor(int slot, Ticks now, ulong ticks) =>
        EmptySince[slot] != default && now + new Ticks(1) >= EmptySince[slot] + new Ticks(ticks);

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
