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

        // The service Building's daily attendance meter -- ArrivalsToday's shape exactly, and saved
        // for its reason: how many attended today is reproducible from nothing else, and a reload
        // that reset it would let one school take a whole Day's places twice.
        //
        // ⚠ IT COUNTS IN EVERY WORLD AND BINDS ONLY WHERE A RATE IS STATED. A city with no
        // [capacity] floor_tiles_per_place has no ceiling, but it still has an answer to `how many
        // children came here today` -- and that answer is what a designer needs in order to choose
        // the rate at all. A meter nobody can read is not evidence for the number that would bound
        // it.
        AttendedToday = _rows.Saved<int>("attended_today");
        AttendedDay = _rows.Saved<int>("attended_day");

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

        // The Tick this Building was raised on. SAVED because it is the only record the city keeps
        // of WHEN anything happened to it, and a reload that reset it would make every Building in a
        // hundred-year-old town the same age as the one raised this morning.
        //
        // ⚠ PLUS ONE, WHICH IS EmptySince's ENCODING AND FOR ITS REASON. A zero-filled row has to
        // read as "no Building here", and Tick 0 is when the populator raises every Building in
        // every shipped world -- so zero-as-a-Tick would make the whole generated city read as
        // unraised. RaisedOn and StandingFor below are the whole interface.
        //
        // 🔴 IT VARIES IN NO GENERATED CITY AND THAT IS NOT A DEFECT IN THIS COLUMN. SyntheticCity
        // raises everything it lays in one call at one Tick, so a fresh world is uniformly aged and
        // only a RUN produces a spread -- the Zone Rule building on cleared land is the one thing in
        // the build that raises a Building at a Tick somebody would notice. ***A city with no
        // history has no ages***, which is a statement about the generator and not about age.
        RaisedAt = _rows.Saved<Ticks>("raised_at", Touch.Cold);

        // 🔴 WHAT THIS BUILDING PUT OUT, ATTRIBUTABLE TO IT. `MapLayers.EmitPollution` adds into a
        // Cell that many Lots share and that decays every cadence, so once a Rule has emitted there
        // is nothing left in the world that says which Building did it. A charge levied per unit
        // emitted needs that number, and no query over the Layer can reconstruct it.
        //
        // THREE COLUMNS AND NOT TWO, which is where this parts company with
        // BusinessTable.TradingDay/DayRevenue/DayExpense. A Business's pair is read by a sweep
        // pinned to the very head of the Tick, so "yesterday" there is whatever the accumulators are
        // still carrying -- an arrangement that needs two tests to hold its phase position and that
        // fails silently if anything moves. Emission is written at phase 3 and charged at phase 6,
        // within one Tick, so a two-column form would hand a charge a HALF-FINISHED Day every time
        // the Day boundary fell inside the Tick it read on. PriorEmitted is a COMPLETE Day whenever
        // it is read, from any phase, and that is a property of the column rather than a note about
        // the current arrangement of sweeps.
        //
        // ⚠ THE ROLL IS THE TABLE'S. <see cref="Emit"/> is the only write door and it rolls first;
        // <see cref="PriorEmissionOn"/> is the only read door and it answers for the Day asked about
        // rather than for the Day the columns happen to hold. Neither needs a midnight sweep and
        // neither has a special case for a Building that has never emitted -- see PriorEmissionOn.
        //
        // A zero fill is honest, on TradingDay's precedent: Day 0 with nothing on either side reads
        // as "this Building has emitted nothing today", which is true of one that has never emitted
        // at all.
        //
        // Cold because emission is a Rule firing on its rate, not a per-Tick walk.
        EmittingDay = _rows.Saved<ushort>("emitting_day", Touch.Cold);
        DayEmitted = _rows.Saved<long>("day_emitted", Touch.Cold);
        PriorEmitted = _rows.Saved<long>("prior_emitted", Touch.Cold);

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
    /// <b>One rather than a list</b>, because a Building has one derived parking ceiling, however it
    /// is arrived at — <c>[capacity] floor_tiles_per_parking_space</c> now, a per-kind count before
    /// <c>plans/0053</c> step 3, and one Car Park either way. A
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

    /// <summary>
    /// The Tick this Building was raised on, <b>encoded plus one</b> so that a zero-filled row reads
    /// as no Building rather than as one raised at midnight on the first Day.
    /// </summary>
    /// <remarks>
    /// <b>Read it through <see cref="RaisedOn"/> and <see cref="StandingFor"/> and never directly</b>,
    /// which is <see cref="EmptySince"/>'s rule and for its reason: the encoding does not travel, and
    /// a comparison made in decoded space is one subtraction away from a <c>Ticks</c> that refuses to
    /// be subtracted.
    /// </remarks>
    public Column<Ticks> RaisedAt { get; }

    /// <summary>Records the Tick this Building was raised on.</summary>
    public void MarkRaised(int slot, Ticks now) => RaisedAt[slot] = new Ticks(now.Raw + 1);

    /// <summary>The Tick this Building was raised on, or <c>default</c> if nothing recorded one.</summary>
    /// <remarks>
    /// ⚠ <b><c>default</c> is a real answer and means <em>unrecorded</em></b>, not <em>Tick 0</em> —
    /// a save written before this column existed loads every Building that way, and so does any
    /// fixture that builds a row by hand rather than through <c>World.CreateBuilding</c>.
    /// </remarks>
    public Ticks RaisedOn(int slot) =>
        RaisedAt[slot] == default ? default : new Ticks(RaisedAt[slot].Raw - 1);

    /// <summary>How many Ticks this Building has stood, or zero if nothing recorded when it went up.</summary>
    /// <remarks>
    /// <b>Subtracted in ENCODED space</b>, which is <see cref="HasStoodEmptyFor"/>'s rule: the stored
    /// value is the Tick plus one, so <c>now + 1 - stored</c> is <c>now - raised</c> without ever
    /// asking <c>Ticks</c> for an operator it refuses. ⚠ <b>The comparison guards the subtraction
    /// rather than clamping after it</b> — these are unsigned, so a Building recorded in the future
    /// would wrap to an enormous age instead of to a negative one.
    /// </remarks>
    public ulong StandingFor(int slot, Ticks now)
    {
        ulong stored = RaisedAt[slot].Raw;

        return stored == 0 || now.Raw + 1 < stored ? 0 : now.Raw + 1 - stored;
    }

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
    /// How many Households have attended this service Building on the Day
    /// <see cref="AttendedDay"/> names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what makes <c>[capacity] floor_tiles_per_place</c> bind</b>, and it is
    /// <see cref="ArrivalsToday"/>'s mechanism rather than its analogy: a ceiling on attendance is a
    /// <em>rate</em>, so meeting it takes a count and the period the count belongs to. The whole
    /// population attends on one Tick (<c>ServiceEngine.Attend</c>), so a per-call bound would let
    /// every family in the city take the same last place.
    /// </para>
    /// <para>
    /// ⚠ <b>Advanced in every world, bounded only where a rate is stated.</b> A Ruleset with no
    /// <c>floor_tiles_per_place</c> has no ceiling and still keeps the tally, because
    /// ***the number a designer would need in order to choose the ceiling is the one this column
    /// holds.*** Zero on every Building that is not a service, and on every service in a world with
    /// no attended Need.
    /// </para>
    /// </remarks>
    public Column<int> AttendedToday { get; }

    /// <summary>
    /// Which Day <see cref="AttendedToday"/> counts, so the meter resets without a sweep.
    /// </summary>
    /// <remarks>
    /// <see cref="ArrivalDay"/>'s argument, unchanged: a stored period is <c>O(1)</c> at the read
    /// site, costs nothing on the Buildings nobody attends, and is right across a load that lands
    /// mid-Day where a scheduled reset that has already run would not be.
    /// </remarks>
    public Column<int> AttendedDay { get; }

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

    /// <summary>
    /// The Day <see cref="DayEmitted"/> is accumulating for.
    /// </summary>
    /// <remarks>
    /// <b>A zero fill is honest</b>, on <see cref="BusinessTable.TradingDay"/>'s precedent: it reads
    /// as <em>nothing has been emitted against Day 0</em>, which is true of a Building that has never
    /// emitted, and every later Day fails the match.
    /// </remarks>
    public Column<ushort> EmittingDay { get; }

    /// <summary>
    /// What this Building has emitted into a Map Layer on <see cref="EmittingDay"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It is HALF-FINISHED for as long as the Day is</b>, which is why nothing outside this
    /// table reads it. A charge that read it would be charging for however much of the Day had
    /// happened by the phase it ran in, and would be right on every Tick but the ones that matter.
    /// <see cref="PriorEmissionOn"/> is the door.
    /// </para>
    /// <para>
    /// <b>It counts what the Rule engine handed the Layer</b> — <c>emission.Amount ×
    /// applications</c>, summed over every emission of every Rule this Building fired. It is not a
    /// reading of the Layer, which has diffused and decayed by the time anybody looks at it.
    /// </para>
    /// </remarks>
    public Column<long> DayEmitted { get; }

    /// <summary>
    /// What this Building emitted over the whole of the Day before <see cref="EmittingDay"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A COMPLETE Day whenever it is read, from any phase.</b> That is the property the third
    /// column buys and the reason it is not redundant with <see cref="DayEmitted"/>: emission is
    /// written at phase 3 and charged later in the same Tick, so a reader of a two-column form would
    /// see a Day still being written to on exactly the Ticks a Day boundary falls in — a wrong
    /// number with no symptom. Nothing here depends on where in the Tick the reader runs.
    /// </para>
    /// <para>
    /// ⚠ <b>Read it through <see cref="PriorEmissionOn"/> and not directly.</b> The column holds the
    /// Day before <see cref="EmittingDay"/>, which is the Day before <em>today</em> only while the
    /// Building is still emitting daily. A Building that last emitted a week ago has a stale pair,
    /// and the door is what turns that into the zero it means.
    /// </para>
    /// </remarks>
    public Column<long> PriorEmitted { get; }

    /// <summary>
    /// Records <paramref name="amount"/> emitted by this Building on <paramref name="today"/>.
    /// </summary>
    /// <remarks>
    /// <b>The Day is rolled first, here and nowhere else</b> — see <see cref="Roll"/>. A caller hands
    /// over the Day it is acting on and never has to know which Day the accumulators are carrying,
    /// which is <see cref="BusinessTable.Earn"/>'s arrangement and it is here for that one's reason:
    /// an accumulator whose reset is the caller's responsibility is reset on every path but the one
    /// nobody thought about.
    /// </remarks>
    public void Emit(int slot, ushort today, long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Roll(slot, today);
        DayEmitted[slot] += amount;
    }

    /// <summary>
    /// What this Building emitted over the whole of the Day before <paramref name="today"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three answers and no special case, which is the whole design of the pair.</b> The
    /// accumulators are rolled lazily by <see cref="Emit"/>, so which column holds yesterday depends
    /// on whether this Building has emitted yet today:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="EmittingDay"/> <em>is</em> <paramref name="today"/> — it has
    /// emitted today, the roll has already happened, and yesterday is
    /// <see cref="PriorEmitted"/>.</description></item>
    /// <item><description><see cref="EmittingDay"/> is <paramref name="today"/> less one — it has not
    /// emitted yet today, nothing has rolled, and yesterday is <see cref="DayEmitted"/>, which is
    /// complete because no later write can ever land on a Day that has ended.</description></item>
    /// <item><description>Anything else — it emitted nothing at all on the Day asked about, so the
    /// answer is <c>0</c>.</description></item>
    /// </list>
    /// <para>
    /// 🔴 <b>The third branch is what a lazy roll costs and what it must pay.</b> A Building that
    /// emitted on Day 3 and next on Day 9 reads <c>0</c> on Day 9 rather than Day 3's figure — the
    /// columns still hold Day 3 at that moment, and a reader that trusted them would charge a
    /// Building for six Days it emitted nothing on. ***A stale accumulator is not a small
    /// number; it is last week's number wearing today's label.***
    /// </para>
    /// <para>
    /// <b>A Building that has never emitted reads <c>0</c> without being asked about separately.</b>
    /// A zero-filled row is <c>EmittingDay = 0</c> with both accumulators at zero, and every branch
    /// above returns zero from it on every Day.
    /// </para>
    /// <para>
    /// ⚠ <b><paramref name="today"/> is compared as an <see cref="int"/></b> so that Day 0 asks about
    /// Day −1 and matches nothing, rather than wrapping to 65,535 and matching a row that has never
    /// been written.
    /// </para>
    /// </remarks>
    public long PriorEmissionOn(int slot, ushort today)
    {
        int emitting = EmittingDay[slot];

        if (emitting == today)
        {
            return PriorEmitted[slot];
        }

        return emitting == today - 1 ? DayEmitted[slot] : 0;
    }

    /// <summary>
    /// Closes the Day the accumulators are carrying when it is not <paramref name="today"/>.
    /// </summary>
    /// <remarks>
    /// <b>The gap case is the one to get right.</b> The Day being closed becomes
    /// <see cref="PriorEmitted"/> only when it really is the Day before <paramref name="today"/>;
    /// a larger gap means this Building emitted nothing across the whole of yesterday, and carrying
    /// an older Day's total forward would charge it for a Day it was idle. ⚠ <b>Private, on
    /// <see cref="BusinessTable"/>'s reason</b> — the reset belongs to the table because a call site
    /// that could add without rolling would fold one Day's emission into another Day's total, and
    /// nothing downstream could tell.
    /// </remarks>
    private void Roll(int slot, ushort today)
    {
        if (EmittingDay[slot] == today)
        {
            return;
        }

        PriorEmitted[slot] = EmittingDay[slot] == today - 1 ? DayEmitted[slot] : 0;
        DayEmitted[slot] = 0;
        EmittingDay[slot] = today;
    }

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
