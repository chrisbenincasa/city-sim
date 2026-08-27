namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Invariants;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// The tables, in declaration order, and the cross-table operations that keep them consistent.
/// </summary>
/// <remarks>
/// <para>
/// <b>Declaration order is a hash input.</b> The composition rule is <em>tables in declaration order,
/// arrays in index order</em>, and the order is the one below — Lots, Buildings, Households, Citizens
/// — which is also the dependency order, since every handle runs from a later table to an earlier
/// one. Reordering this list re-baselines the State Hash and is a deliberate act.
/// </para>
/// <para>
/// <b>Referential integrity lives here rather than on the tables.</b> No framework enforces that a
/// Household's dwelling lists it as an occupant (adr/0004), so the owning code does — and the owning
/// code for a relationship spanning two tables is neither of them. It also keeps
/// <c>BOR0901</c> honest: a table holding a reference to another table would be undeclared state
/// sitting beside the columns.
/// </para>
/// <para>
/// <b>There is no <c>step()</c> here, and no Tick.</b> Slice 5 builds those. This is the storage the
/// Tick will operate on, plus the hash it will be checked with — the hash is generated from the field
/// declaration, so it is a property of the table layer and building it after the Tick would mean
/// building it twice.
/// </para>
/// </remarks>
public sealed class World
{
    /// <summary>
    /// The State Hash's starting value: <c>"Borough"</c> in ASCII, with a version byte.
    /// </summary>
    /// <remarks>
    /// <b>The version byte is where a deliberate re-baseline is recorded.</b> <c>05 §4</c>'s test is
    /// that the hash never moves without somebody saying so; a change to the fold, to the composition
    /// order, or to <c>Randomness.Mix</c> moves every hash in the project at once, and the only way to
    /// tell that from a regression is for the change to be signed. Bump this when re-baselining, never
    /// otherwise.
    /// </remarks>
    // 02: the clock joined the composition. The Tick moved out of Simulation and into ClockTable, so
    // every hash in the project moved at once on account of the composition rather than the city —
    // which is exactly the case this byte exists to distinguish from a regression.
    //
    // A NEW table joining _tables does not bump this, and the line is worth stating because it has
    // now been drawn twice in silence: 5a appended the two Road Graph tables and 5b the three
    // Movement ones, neither with a bump. What this byte signs is *the same city hashing
    // differently* -- the clock was state that already existed, re-composed. New state is a design
    // change under 05 §4: the city genuinely has more in it, the baselines move because the world
    // moved, and signing that would file a real change as a bookkeeping one.
    internal const ulong HashSeed = 0x426F_726F_7567_6802UL;

    private readonly Rows[] _tables;
    private readonly Rows[] _writableTables;

    // The Day's water outflow, one entry per Water Body slot. A scratch buffer rather than a column:
    // it holds a single pass's intermediate and is meaningless between Ticks, so a column would put
    // it in the State Hash and in every save. Grown on demand and never shrunk, which is bounded by
    // the body count -- 14 to 64 on a measured world -- rather than by anything that accumulates.
    // milestone 24 task 6b.
    private long[] _waterOutflow = [];

    /// <param name="citizens">Initial Citizen capacity. Every other table is sized from it.</param>
    /// <remarks>
    /// <b>Sizing is a derivation, not a constant.</b> Every row count S4 task 2 derived is linear in
    /// population, so the ratios are stated per 1,000 Citizens and stay correct at 250k or at 2M —
    /// 360 Households, ~150 Buildings, ~225 Lots. 1M is a floor rather than a cap.
    /// </remarks>
    public World(int citizens)
        : this(citizens, Ruleset.Empty)
    {
    }

    /// <inheritdoc cref="World(int, LayerRuleset)"/>
    /// <summary>A world whose Layer cadence is stated and whose Layer rates are the defaults.</summary>
    public World(int citizens, LayerSchedule layers)
        : this(citizens, new LayerRuleset(layers, LayerRates.Default))
    {
    }

    /// <inheritdoc cref="World(int)"/>
    /// <param name="citizens">Initial Citizen capacity. Every other entity table is sized from it.</param>
    /// <param name="layers">
    /// The Map Layer cadence and rates. Ruleset data (<c>adr/0044</c>); see <see cref="LayerRuleset"/>.
    /// </param>
    /// <remarks>
    /// <b>The Layer ruleset is a constructor argument rather than a constant so that it could be
    /// measured, and it stays one because slice 8 will read it from a file.</b> <c>02 §1.2</c> filed
    /// the diffusion cadence as tuning and <c>plans/0009</c> doubted it; under <c>adr/0043</c> that
    /// claim is <em>measurable</em> rather than arguable, because the number that would settle it is a
    /// State Hash and the machine that produces it is this one. Two worlds differing only in this
    /// argument, run over one Input Log, either hash the same or do not. They do not — so the cadence
    /// is the designer's number and not the profiler's (<c>adr/0044</c>).
    /// </remarks>
    public World(int citizens, LayerRuleset layers)
        : this(citizens, Ruleset.Empty.WithLayers(layers))
    {
    }

    /// <inheritdoc cref="World(int, LayerRuleset)"/>
    /// <param name="citizens">Initial Citizen capacity. Every other entity table is sized from it.</param>
    /// <param name="rules">
    /// The Rules, already validated (<c>adr/0048</c>). <b>Not simulation state and not folded into
    /// the State Hash</b> — what names a Ruleset in the hash is its content hash, carried in the Input
    /// Log, because two runs against different Rules are two different simulations.
    /// </param>
    /// <remarks>
    /// <b>The Layer data comes from the Ruleset and from nowhere else</b> (slice 8 task 3). It used to
    /// arrive as its own argument beside the Rules, which admitted a world whose cadence disagreed with
    /// the Ruleset in force — and the first reload would then silently revert it to whatever the file
    /// said. One source is what closes that; <see cref="Ruleset.WithLayers"/> is how a caller holding
    /// only a cadence still gets a world.
    /// </remarks>
    public World(int citizens, Ruleset rules, WorldKey key = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(citizens);
        ArgumentNullException.ThrowIfNull(rules);

        Rules = rules;
        Key = key;

        Lots = new LotTable(PerThousand(citizens, 225));
        Buildings = new BuildingTable(PerThousand(citizens, 150), Lots);

        // Ahead of the actors rather than beside the Rule engine, because since adr/0114 a Household
        // and a Business each hold a saved handle INTO this table -- their balance -- so this one has
        // to exist before either constructor can name its Rows. It sizes off ~3 Bins on each of ~150
        // Buildings per 1,000 Citizens, plus one apiece for the 360 Households and the Businesses.
        // Construction order is not composition order: _tables below is what the State Hash walks.
        Bins = new BinTable(PerThousand(citizens, 450), Buildings);

        Households = new HouseholdTable(PerThousand(citizens, 360), Buildings, Bins);
        Layers = new MapLayers(rules.Layers);

        // Roads and the Car Parks moved ahead of the Citizens on 2026-08-18, milestone 7 task 1, and
        // the reorder is free: CONSTRUCTION order is not composition order. What the State Hash folds
        // is the order of `_tables` below, which is unchanged and says so at its own site. A
        // CitizenTable now takes a CarParkTable because `ParkedIn` is a handle rather than a slot --
        // Address.cs's rule, one table over: a saved slot index folds the city's whole demolition
        // history into the hash, so two runs building the same city would disagree.
        Roads = new RoadGraph(rules.Roads);

        // One Car Park per Building, so this is the Building ratio and not a fourth guessed one --
        // which 0002 §D1 asks in as many words that nobody add. A kind declaring `parking = 0` still
        // gets no row, so this is a ceiling on what a fully-provisioned city allocates.
        CarParks = new Parking.CarParkTable(PerThousand(citizens, 150), Buildings, Roads.Segments);

        // Sized off the Building count rather than off the population: a Business occupies premises,
        // so what bounds it is how many premises there are. How many may share one is undesigned
        // (adr/0070), so one apiece is the capacity hint that assumes least -- and it is a hint, since
        // the table grows.
        //
        // ⚠ CONSTRUCTED BEFORE Citizens as of milestone 27 task 7, and the order is forced rather
        // than tidied: CitizenTable.Workplace is a handle into THIS table now (adr/0141), so the
        // rows it addresses have to exist first. The reverse dependency does not exist and must not
        // be created -- a Business points at its premises and at no Citizen, and the worker list is
        // an intrusive index list whose `next` column lives on the Citizen.
        Businesses = new BusinessTable(PerThousand(citizens, 150), Buildings, Bins);

        Citizens = new CitizenTable(citizens, Households, Buildings, Businesses, CarParks);

        // Sized for a city in trouble rather than a healthy one: the Pool is empty when everybody is
        // housed, and the table's job is to absorb a District emptying without reallocating mid-Tick.
        UnplacedPool = new UnplacedTable(PerThousand(citizens, 36), Households, Buildings);

        // ~3 Rules on each of ~150 Buildings per 1,000 Citizens. A capacity hint rather than a bound —
        // the table grows — but it is a number nobody has justified, so plans/0002 carries it as
        // unratified until a real Ruleset supplies the shape. Bins is constructed above, with the
        // actors that hold handles into it.
        RuleInstances =
            new RuleInstanceTable(
                PerThousand(citizens, 450), Buildings, Bins, Households, Businesses);
        Clock = new ClockTable();
        Treasury = new TreasuryTable();
        MoneySupply = new MoneySupplyTable();

        // adr/0142's collection, and its capacity hint assumes even less than the Business table's:
        // milestone 27 task 8 gave this pool its inflow (adr/0145), so it is no longer empty in every
        // world -- a Ruleset stating [business] founds into it. A hint, since it grows.
        UnpremisedPool = new UnpremisedTable(8, Businesses, Buildings);
        RulesetTrail = new RulesetTrailTable();
        CondemnationTrail = new CondemnationTrailTable(Lots.Rows);

        // Districts are few and their Cells are many, and neither is a function of population in the
        // way a Building is: what bounds the Cell count is how much GROUND is built on, and a
        // Building's Cell already holds several of its neighbours. Both are capacity hints on a
        // growing table, so the number that matters is that neither is zero -- a District table sized
        // from the population would be a number that looks derived and is not.
        Districts = new Space.DistrictTable(8);
        DistrictCells = new Space.DistrictCellTable(PerThousand(citizens, 150), Districts);

        // One row per Good per District, and both are few -- so this is the one table in the
        // constructor whose hint is a literal because the product of two small numbers is small,
        // rather than because nobody worked it out.
        DistrictPools = new Space.DistrictPoolTable(64, Districts, Bins);

        // Water is sized from the MAP and never from the population, which is the one capacity hint
        // in this constructor that has nothing to do with how many Citizens there are: a coastline is
        // a property of the ground, and an empty world on a wet key has every wet Cell that a
        // crowded one does. The body count starts at 8 for DistrictTable's reason -- a small table
        // that grows -- and the Cell hint is a sixteenth of the map, which is a hint and not a claim
        // about how much of a world is water. adr/0160 says why no key states that share.
        Water = new Space.WaterBodyTable(8, Bins);
        WaterCells = new Space.WaterCellTable(
            IntegerMath.FloorDiv(Space.CellGrid.WorldCellCount, 16), Water);

        // Which Water Body each Cell's runoff reaches, and it takes no hint at all because it is
        // DENSE -- one row per Cell of the map, allocated in its own constructor. The question is
        // asked about dry ground, so sparsity would be storing a residency index to say "no" about
        // the Cells that are the whole point. milestone 24 task 6b, adr/0160.
        Catchment = new Space.CatchmentCellTable(Water);

        // The Hazard Region, sized from the MAP for the water tables' own reason. A floodplain is a
        // band above the waterline, so a sixty-fourth of the map is a hint and not a claim about how
        // much of a world floods -- adr/0157's own revisit trigger is that this turns out not to be
        // sparse. milestone 24 task 9.
        Flood = new Space.FloodCellTable(
            IntegerMath.FloorDiv(Space.CellGrid.WorldCellCount, 64));

        // The three Movement tables, and their capacity is deliberately NOT a function of population.
        //
        // plans/0021 -> "Decisions this slice must close" 3 is explicit about why: adr/0008 says the
        // Trip table "must be sized for this rather than for a Leg-per-Trip assumption", the ratio it
        // means (0002 §B-17, mean Legs per Trip) has never been measured, and 0002 §D1 already carries
        // table sizing ratios as a LIVE INCONSISTENCY -- World allocates 225 Lots and 150 Buildings per
        // 1,000 while the populator builds 120 of each. The instruction is "do not add a fourth guessed
        // ratio to that row".
        //
        // So this is a capacity hint that says only "a handful", and Rows grows. The number that
        // replaces it comes from milestone 5b-bis measuring what a real generator produces, at a
        // recorded rung -- not from arithmetic done here.
        Trips = new Movement.TripTable(64, Roads.Segments);
        Legs = new Movement.LegTable(64, Roads.Segments);
        RouteHops = new Movement.RouteHopTable(64, Roads.Segments);
        Travellers = new Movement.TravellerTable(64, Citizens, Trips);

        // The registry is built before the Wheel rather than after the tables, because EventWheel.Arm
        // reports a double arming through it, and after the Clock, because it reads the Tick from this
        // world rather than being told one. Ordering only — the registry folds nothing.
        Invariants = new InvariantRegistry(this);

        Wheel = new EventWheel(RuleInstances, Invariants);

        // Declaration order, which is hash composition order. The Rule engine's three tables go last
        // because they arrived last; the order is arbitrary but it is not free to change, so it is
        // stated here and moving it is a re-baseline rather than a tidy-up. The trail is appended for
        // the same reason — it arrived last, in slice 8 task 7 — and appending is the only edit to
        // this list that does not move rows relative to one another.
        _tables = [
            Lots.Rows, Buildings.Rows, Households.Rows, Citizens.Rows, Layers.Cells.Rows,
            Bins.Rows, RuleInstances.Rows, Wheel.Buckets.Rows, UnplacedPool.Rows, Clock.Rows,
            RulesetTrail.Rows, Roads.Nodes.Rows, Roads.Segments.Rows,

            // Appended, which this comment's own rule says is the one edit that moves no row relative
            // to another. They were built by 5b tasks 1-3 and left out of this list, so until now they
            // were outside the State Hash entirely -- declared columns with a saved/derived disposition
            // that nothing folded. adr/0080 wires them because Phase 4 is what constructs a row in them.
            Trips.Rows, Legs.Rows, Travellers.Rows,

            // Appended for the same reason, 5c task 6. A drive Leg's route is saved state -- a route
            // is a function of its endpoints AND the graph at the moment it was planned, so a reload
            // that recomputed it would silently re-plan every journey in flight against a graph the
            // player may have edited since.
            RouteHops.Rows,

            // Appended for the same reason, milestone 6 task 1. A condemnation's cause is available for
            // exactly one line -- DestroyBuilding frees the Rule Instances that hold it -- so this is
            // history rather than present state, and 02 §9's "why is this Lot vacant" cannot be
            // answered from a world that did not keep it.
            CondemnationTrail.Rows,

            // Appended for the same reason, milestone 10 task 4. The money supply of record is saved
            // state a snapshot cannot re-derive -- summing the balances to recover it is what would
            // make Invariant.MoneyIsConserved recompute the producer's own expression -- so it is in
            // the composition, unlike the treasury row below it.
            MoneySupply.Rows,

            // Appended for the same reason, milestone 10 task 4b. adr/0113: a Business is the second
            // Occupant kind, its balance is a saved column on the actor, and conserved money sitting
            // outside the composition would be money the State Hash cannot see.
            Businesses.Rows,

            // Appended for the same reason, milestone 12 task 3. adr/0134: a District is derived from
            // the city and then SAVED, because task 4's hysteresis, damping and persistence all read
            // the previous extent -- so it is state a snapshot cannot re-derive, and a Pool Bin
            // hanging off a District that a reload numbered differently would be a Pool that moved.
            Districts.Rows, DistrictCells.Rows,

            // Appended for the same reason, milestone 12 task 5. The Pool's Bins are in Bins.Rows
            // already; what is here is which District each belongs to, and BinTable.Owner cannot say
            // -- so this join is the only saved statement of the relation and a hash without it would
            // agree about two worlds whose Pools belonged to different Districts.
            DistrictPools.Rows,

            // Appended, milestone 25 task 5. 🔴 IT WAS MISSING FOR THE LENGTH OF ONE BUILD AND EVERY
            // TEST PASSED -- 2,074 of them -- because a table absent from this list is not hashed, and
            // a fact nothing folds cannot disagree with anything. ⚠ The allocation-by-declaration
            // rule closes the COLUMN-level coverage hole and leaves this one wide open: declaring
            // `Since` as Rows.Saved guarantees it is folded IF this table is walked, and guarantees
            // nothing at all about whether it is. ***A saved table outside this array is state the
            // State Hash has agreed not to look at.***
            //
            // It carries the membership and the give-up clock, and both are load-bearing: a member is
            // drawn by POSITION, so a reload that produced a different order would retire a different
            // Business from the same save, and a Since that did not travel would restart every clock
            // at the load.
            UnpremisedPool.Rows,

            // TreasuryTable is deliberately NOT here, milestone 10 task 1. Both its columns are
            // Derived, and 5a's finding is that a wholly-derived table cannot join this list: Rows.Fold
            // folds the allocator's four scalars BEFORE consulting any column's disposition, so such a
            // table hashes its own allocation history rather than any state. It is safe here only
            // because the one row is allocated in the constructor and never freed, which makes the
            // contribution a constant -- and a constant is not a reason to add a row to a list whose
            // order is a re-baseline to change. The treasury's *state* is in Bins.Rows, which is here.

            // Appended for the same reason, milestone 7 task 1. A Car Park's occupancy is saved state
            // and nothing recomputes it: adr/0084 calls a leak here an adr/0006-class *permanent*
            // capacity loss, so a reload that re-derived occupancy would launder exactly the defect
            // the two invariants exist to catch. Appending is still the one edit to this list that
            // moves no row relative to another, and the version byte above is deliberately NOT
            // bumped -- this is new state, so the baselines move because the world moved.
            CarParks.Rows,

            // Appended for the same reason, milestone 24 task 2. adr/0158: the terrain type column is
            // (saved AND hashed), and it is the ONE table here whose rows are all allocated in its
            // constructor and never freed -- so its contribution to Rows.Fold's allocator scalars is
            // a constant, and what moves the hash is the column. The TreasuryTable note above says a
            // constant is not a reason to ADD a table; this one is here because its column is state
            // no snapshot can re-derive without the WorldKey, which a save does not carry back into
            // the generator. Appending stays the one edit to this list that moves no row relative to
            // another.
            Layers.Terrain.Rows,

            // Appended for the same reason, milestone 24 task 8a. adr/0159: the Woodland Tile count is
            // (saved AND hashed), dense like terrain, and state no snapshot can re-derive -- the
            // generator gives the world its forest and the running city spends it, so neither the
            // WorldKey alone nor the Tick history alone reproduces the column.
            //
            // ⚠ UNLIKE TERRAIN IT STAYS IN _writableTables, and the difference is the whole test
            // TablesAPhaseCanWrite states: not *is it expensive* but *can any phase write it*.
            // MapLayers.Seal writes this one every time a Building is created, so excluding it would
            // be the silent hole that document warns about rather than the narrowing it describes.
            Layers.Woodland.Rows,

            // Appended for the same reason, milestone 24 task 6a. adr/0034 and adr/0160: the water
            // graph is (saved AND hashed) because a save does not carry the WorldKey back into the
            // generator, and adr/0021 makes water immutable -- so these two tables are written once,
            // at world creation, and never again. Appending stays the one edit to this list that
            // moves no row relative to another.
            Water.Rows, WaterCells.Rows,

            // And the catchment with them, milestone 24 task 6b. Saved on the water tables' own
            // grounds -- it is a function of the WorldKey and a save does not carry the WorldKey
            // back into the generator -- so a load restores it rather than recomputing it.
            Catchment.Rows,

            // And the Hazard Region, milestone 24 task 9. Generated once, never written in a Tick
            // (CONTEXT.md -> Hazard Region), and saved because a load does not re-run the generator.
            Flood.Rows,
        ];

        // The same list minus the tables no Tick phase can write, for the Decide guard alone. See
        // TablesAPhaseCanWrite -- it is a subset of the COMPOSITION and never a second composition.
        // The water tables join terrain here, and on terrain's test rather than on a cost: adr/0021
        // makes water generated once and immutable, so NO Tick phase can write either of them. If a
        // phase ever does -- a Bin on a Water Body fills at task 6b, and a Bin's level is a write --
        // it is that task's job to take the table back out of this exclusion, not to work around it.
        _writableTables =
        [
            .. _tables.Where(table =>
                !ReferenceEquals(table, Layers.Terrain.Rows)
                && !ReferenceEquals(table, Water.Rows)
                && !ReferenceEquals(table, WaterCells.Rows)
                && !ReferenceEquals(table, Catchment.Rows)
                && !ReferenceEquals(table, Flood.Rows)),
        ];

        WorldInvariants.RegisterAll(Invariants);

        // adr/0114 and adr/0116, and it is here rather than only in Adopt because a world constructed
        // with a Ruleset never adopts one -- Adopt is the SWAP path. A treasury fitted only on swap
        // would leave every world that loaded its Ruleset at construction with a global scope that
        // resolves to nothing, which is the same hole this task exists to close, differently spelt.
        FitTreasury();
    }

    /// <summary>
    /// Every Car Park in the city — the supply side of <c>adr/0009</c>'s parking model.
    /// </summary>
    /// <remarks>
    /// <b>Buildings only in this milestone, and Road Segments are omitted rather than foreclosed</b>
    /// (<c>adr/0120</c>). A Car Park is located by an <c>Address</c>, which is already
    /// <c>(Segment, offset, side)</c>, so a Segment-held one needs no new column — only rows and a
    /// balance pass. The omission is filed in <c>06</c>'s <em>Mechanisms with no milestone</em>.
    /// </remarks>
    public Parking.CarParkTable CarParks { get; }

    /// <summary>
    /// Trips in flight and Trips resolved but not yet read out. <c>adr/0075</c>'s <em>what</em>.
    /// </summary>
    public Movement.TripTable Trips { get; }

    /// <summary>
    /// Every Leg of every live Trip. <c>adr/0075</c>'s <em>plan</em> — a cost, never a path.
    /// </summary>
    public Movement.LegTable Legs { get; }

    /// <summary>
    /// The Segments each drive Leg crosses — <b>what a moving Traveller reads to know where it is</b>.
    /// </summary>
    public Movement.RouteHopTable RouteHops { get; }

    /// <summary>
    /// Citizens currently on the road. <c>adr/0075</c>'s <em>cursor</em>, and a view rather than an
    /// owner: no conserved quantity lives here.
    /// </summary>
    public Movement.TravellerTable Travellers { get; }

    /// <summary>The world's position in time, as one saved row.</summary>
    public ClockTable Clock { get; }

    /// <summary>
    /// The city's balance sheet: one row, holding the head of the treasury's Bins (<c>adr/0114</c>).
    /// </summary>
    public TreasuryTable Treasury { get; }

    /// <summary>
    /// The Businesses, each occupying a Building and holding its own balance (<c>adr/0113</c>).
    /// </summary>
    public BusinessTable Businesses { get; }

    /// <summary>
    /// How much money has been issued into this world, as one saved row. The anchor
    /// <see cref="Invariant.MoneyIsConserved"/> is an equality against, and <b>not</b> the treasury's
    /// balance — see <see cref="MoneySupplyTable"/>.
    /// </summary>
    public MoneySupplyTable MoneySupply { get; }

    /// <summary>
    /// The Tick this world is about to run.
    /// </summary>
    /// <remarks>
    /// <b>Read freely; advanced only by <see cref="Simulation.Step"/>.</b> It is the <em>next</em> Tick
    /// rather than the last one run — <see cref="ClockTable"/> says why that convention is kept — so a
    /// row armed for exactly it is due next rather than overdue, which is what makes the Event Wheel's
    /// period bound half-open at the bottom.
    /// </remarks>
    public Ticks Tick => Clock.Tick[0];

    /// <summary>Moves the world on one Tick. <b>The Tick loop's, and nothing else's.</b></summary>
    internal void Advance() => Clock.Tick[0] += new Ticks(1);

    /// <summary>Parcels of land.</summary>
    public LotTable Lots { get; }

    /// <summary>Structures.</summary>
    public BuildingTable Buildings { get; }

    /// <summary>People sharing a dwelling and finances.</summary>
    public HouseholdTable Households { get; }

    /// <summary>People.</summary>
    public CitizenTable Citizens { get; }

    /// <summary>The coarse environment: one integer per Cell per Map Layer.</summary>
    public MapLayers Layers { get; }

    /// <summary>
    /// The routing abstraction: nodes and Segments, uniform regardless of how a road was drawn.
    /// </summary>
    /// <remarks>
    /// <b>Two of its three structures are registered above and the third is deliberately not.</b> The
    /// nodes and the Segments are saved entities and fold; the Arcs are wholly derived, and a table
    /// with no saved column still folds four allocator scalars — including <c>next_id</c>, which
    /// counts every row ever allocated. Registering them would make the State Hash depend on how many
    /// times the adjacency had been rebuilt. See <see cref="RoadArcs"/>.
    /// </remarks>
    public RoadGraph Roads { get; }

    /// <summary>
    /// Every Lot's contact with the Street it takes access from — <b>derived, rebuilt on the Epoch,
    /// and outside the State Hash</b> (<c>adr/0078</c>).
    /// </summary>
    /// <remarks>
    /// <b>Not a registered table, for <see cref="RoadArcs"/>'s reason.</b> It writes into
    /// <see cref="LotTable"/>'s two derived columns and owns one array of its own — the per-Segment
    /// claim mask — none of which is saved, so a table of its own would fold four allocator scalars
    /// and make the hash depend on how many times frontage had been recomputed.
    /// </remarks>
    public Frontage Frontage { get; } = new();

    /// <summary>
    /// Which Buildings stand in a given Cell. The query from a place to the things on it.
    /// </summary>
    /// <remarks>
    /// <b>Not a registered table, for <see cref="Frontage"/>'s reason</b>, and it is the same
    /// argument once more: it owns two arrays over the Cell grid and writes one derived column on
    /// <see cref="BuildingTable"/>, none of it saved, so a table of its own would fold the allocator's
    /// four scalars and make the State Hash depend on how many times the index had been rebuilt —
    /// which is `plans/0020`'s finding that a wholly-derived table cannot join
    /// <see cref="Tables"/>.
    /// </remarks>
    public BuildingResidency BuildingsInCells { get; } = new();

    /// <summary>
    /// Which live Lots admit a given use — the draw space a seeker's candidates come from.
    /// </summary>
    /// <remarks>
    /// <b>Rebuilt whole rather than maintained, and every writer of the Lot set invalidates it</b>
    /// (<see cref="ZonedLots"/>, which carries the argument). Nothing here calls it per Lot: a
    /// subdivision creates them in bulk and an eager rebuild per <c>Create</c> would be quadratic
    /// over one road edit.
    /// </remarks>
    public ZonedLots LotsAdmitting { get; } = new();

    /// <summary>
    /// The city's Districts — a centre and the basin that drains to it, <c>adr/0134</c>.
    /// </summary>
    /// <remarks>
    /// <b><c>(saved AND hashed)</c>, and the reason lands at milestone 12 task 4</b> rather than here:
    /// hysteresis, damping and persistence all consult the previous extent, so a District recomputed
    /// from scratch on load would be a different District from the one that was saved. Today the
    /// derivation runs once, at world creation, and reproduces itself — which is exactly the state a
    /// task-4 re-evaluation will stop being in.
    /// </remarks>
    public Space.DistrictTable Districts { get; }

    /// <summary>Which District each built Cell drains to. The District's extent, as rows.</summary>
    public Space.DistrictCellTable DistrictCells { get; }

    /// <summary>The Cell-to-District index. Derived, and rebuilt from the saved coordinates.</summary>
    public Space.DistrictResidency DistrictsInCells { get; } = new();

    /// <summary>Which Bins are in which District's Pool. Saved, and the only thing that knows.</summary>
    public Space.DistrictPoolTable DistrictPools { get; }

    /// <summary>
    /// Where a purchase looks — a District's market row for a Good, and the sellers standing in it.
    /// </summary>
    /// <remarks>
    /// <b>Rebuilt whole rather than maintained</b> (<see cref="Space.DistrictMarkets"/>, which carries
    /// the argument), and the writers that invalidate it are the two ends of a Business's tenancy,
    /// the two ends of a Pool row, and the watershed. ⚠ <b>It is not <see cref="FindDistrictPoolBin"/>
    /// with an index in front of it</b>: that walk stays, because it is what the rebuild reads and a
    /// lookup that consulted the thing it builds could not build it.
    /// </remarks>
    public Space.DistrictMarkets Markets { get; } = new();

    /// <summary>
    /// The Water Bodies and which one each drains into. <b>Generated once and never written again.</b>
    /// </summary>
    /// <remarks>
    /// Here rather than on <c>MapLayers</c> — unlike terrain and Woodland, which are quantities of the
    /// ground — because a Water Body is a thing the city <em>contains</em> and will own a Bin at task
    /// 6b, which is <see cref="Districts"/>'s shape and not a Layer's. <c>adr/0034</c>,
    /// <c>adr/0160</c>.
    /// </remarks>
    public Space.WaterBodyTable Water { get; }

    /// <summary>Which Water Body covers each wet Cell. A body's extent, as rows.</summary>
    public Space.WaterCellTable WaterCells { get; }

    /// <summary>The Cell-to-water index. Derived, and rebuilt from the saved coordinates.</summary>
    public Space.WaterResidency WaterInCells { get; } = new();

    /// <summary>
    /// Desirability's <c>w₅</c> source, or <b>null on a world whose water has no Bin.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Built per read rather than held</b>, because <c>[water] capacity_per_cell</c> is the
    /// denominator under every fill fraction and it is <em>hot-reloadable</em> (<c>adr/0015</c>). A
    /// cached instance would keep answering against the capacity that was in force when the world was
    /// made, which is the quietest kind of reload bug. The object holds four references and no state,
    /// so making one costs nothing worth caching.
    /// </para>
    /// <para>
    /// ⚠ <b>Null is the world with no water and it is not the same as a clean coastline.</b> Null drops
    /// <c>w₅</c> out of the composition; a Ruleset with water and an empty Bin keeps the term and gets
    /// zero from it. <c>adr/0123</c> turns on exactly that distinction.
    /// </para>
    /// </remarks>
    public Space.Shoreline? Shore =>
        Rules.Water.HasBin
            ? new Space.Shoreline(
                WaterCells, WaterInCells, Water, Bins, Rules.Water.CapacityPerCell)
            : null;

    /// <summary>
    /// Which Water Body each Cell drains into — <b>every Cell, wet and dry.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b><see cref="WaterInCells"/> answers a different question and cannot answer this one.</b> It
    /// says which body a Cell <em>is part of</em>, which is a fact about wet Cells; Buildings stand on
    /// dry ground, so a Bin addressed through it would be a Bin nothing could ever reach. This table
    /// is what gives a dry Cell a body to name. <c>milestone 24 task 6b</c>.
    /// </remarks>
    public Space.CatchmentCellTable Catchment { get; }

    /// <summary>
    /// The Hazard Region — <b>how deep a flood stands on each Cell it reaches.</b>
    /// </summary>
    /// <remarks>
    /// <c>CONTEXT.md</c> → Hazard Region: ground where a Disaster can occur, derived from terrain at
    /// world generation and <b>never read during a Tick</b>, so <c>adr/0021</c> holds. ⚠ <b>Nothing
    /// fires on it and that is by design, not by omission</b> — <c>plans/0042</c> puts Disasters
    /// behind milestone 15's fire-service reachability, and *deriving where something could happen is
    /// the terrain milestone's; scheduling it is the milestone that has something to schedule.*
    /// </remarks>
    public Space.FloodCellTable Flood { get; }

    /// <summary>Which Car Parks sit on which Segment — the Parking Shed query's supply index.</summary>
    /// <remarks>
    /// <b>Owned here rather than by the caller, and that is what makes it survive a load.</b> Its
    /// element column is <c>car_park.segment_next</c>, declared <see cref="Disposition.Derived"/>,
    /// and a derived column's whole claim is that a load can reproduce it — which is only true if
    /// something inside the world rebuilds it. ⚠ <b>It was a caller-owned scratch structure when the
    /// shed query was written, and every shed in a loaded world would have come back empty</b>:
    /// nothing read it yet, so nothing was wrong, and the defect was invisible until milestone 8's
    /// <c>DerivedRebuildAuditTests</c> asked which derived columns no world populates. ***A structure
    /// that lives outside the world is not derived state, however it is declared.***
    /// </remarks>
    public Parking.CarParkResidency CarParksOnSegments { get; } = new();

    /// <summary>
    /// Whether this world has a Street lattice for a Lot to front.
    /// </summary>
    /// <remarks>
    /// <b>A Ruleset with no <c>[roads]</c> is a world with no geography, and frontage is not a thing
    /// it can be missing.</b> <c>SyntheticCity</c> lays rows with no frontage on purpose in that case
    /// and says so at length — S0a's footprint capture is <c>--citizens</c> with no <c>--ruleset</c>,
    /// and a populator that made no rows would answer the sizing question with an empty world. Every
    /// rule about frontage is therefore conditioned on this rather than stated absolutely, which is
    /// <c>adr/0070</c> at the site: the absence is <em>unbuilt geography</em>, not a defect.
    /// </remarks>
    public bool HasStreets => Roads.Streets.BlockTiles > 0;

    /// <summary>The Bins, and their wait lists.</summary>
    public BinTable Bins { get; }

    /// <summary>One row per (Building, Bin Rule) — armed, or asleep on a Bin.</summary>
    public RuleInstanceTable RuleInstances { get; }

    /// <summary>Slice 7's minimal Event Wheel: a bucket per Tick, an arming, and a drain.</summary>
    public EventWheel Wheel { get; }

    /// <summary>
    /// Which Citizens leave for work on which Tick of the Day (<c>adr/0081</c>).
    /// </summary>
    /// <remarks>
    /// <b>Not on the <see cref="Wheel"/>, and the reason is that it would never move.</b> A commute
    /// recurs every Day and the Wheel is exactly a Day long, so an armed Citizen sits in one bucket
    /// for life — which makes the bucket a partition on a constant, and therefore derivable. See
    /// <see cref="CommuteRoster"/>.
    /// </remarks>
    public CommuteRoster Commutes { get; } = new();

    /// <summary>
    /// The world seed, as <c>Randomness.Draw</c>'s first coordinate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Held here because a derived structure needs it and nothing was passing it in.</b>
    /// <see cref="Commutes"/> is a pure function of the Ruleset in force and of each Citizen's
    /// <em>id</em> hashed against this key, so <see cref="RebuildDerived"/> — which takes no
    /// arguments and must not start taking them — cannot reproduce the maintained list without it.
    /// </para>
    /// <para>
    /// ⚠ <b>Every other mutator on this class takes a <c>WorldKey</c> as a parameter, and that is now
    /// redundant rather than wrong.</b> A world has one seed; passing a second one to
    /// <see cref="CreateBuilding"/> would make the arming stagger disagree with the commute roster
    /// about which world this is. The threading predates this property and removing it is a sweep
    /// rather than a task-5 edit — filed to <c>plans/0012</c> rather than done here, because a
    /// signature change across nine call sites in the middle of a milestone is how an unrelated
    /// defect gets committed under a feature's name. <b>The entry exists; the first version of this
    /// sentence claimed it did two commits before it was written, which is its own entry in that
    /// ledger — a citation is not a filing.</b>
    /// </para>
    /// <para>
    /// <b>Zero is the default and it is a legitimate seed rather than a placeholder.</b> That is safe
    /// here in the way session F's rule requires — not because zero is outside the range, but because
    /// there is exactly <em>one</em> of these per world, so a wrong value gives a different city and
    /// never an inconsistent one. The failure a placeholder causes is disagreement between two
    /// readers, and there is only one reader.
    /// </para>
    /// </remarks>
    public WorldKey Key { get; }

    /// <summary>The Households seeking housing, and the only demand signal this design has.</summary>
    /// <remarks>
    /// <b>Named <c>UnplacedPool</c> in full because <c>CONTEXT</c> holds two Pools and they are
    /// unrelated.</b> A District Pool is where Goods sit; the Unplaced Pool is where people wait.
    /// Shortening either to <c>Pool</c> in code is how the two would eventually be confused by
    /// somebody reading one call site.
    /// </remarks>
    public UnplacedTable UnplacedPool { get; }

    /// <summary>
    /// The unpremised pool: the Businesses seeking premises.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It ships with one exit and that exit is the SINK</b> — see <see cref="UnpremisedTable"/>.
    /// Nothing tenants a Business because nothing creates one, so a member leaves only by giving up
    /// and emigrating. ***That is <c>adr/0006</c>'s bound arriving with the collection***, which is
    /// <c>adr/0142</c>'s own rule applied to itself.
    /// </remarks>
    public UnpremisedTable UnpremisedPool { get; }

    /// <summary>
    /// What every Ruleset transition this world survived destroyed, capped and aggregated.
    /// </summary>
    /// <remarks>
    /// <b>The one piece of this world that is about its past rather than its present</b>, and it is
    /// here rather than on the <see cref="Simulation"/> because <c>05 §7</c> puts it in the
    /// <em>save</em>: the bug it exists for is a defect caused by a degradation upstream of every
    /// snapshot anybody holds, and a trail that died with the process could never reach one.
    /// </remarks>
    public RulesetTrailTable RulesetTrail { get; }

    /// <summary>
    /// Every Building this world condemned and why, capped and aggregated past the cap.
    /// </summary>
    /// <remarks>
    /// <b>Here rather than on the <see cref="Simulation"/> for <see cref="RulesetTrail"/>'s reason, and
    /// the reason is stronger.</b> That trail is in the save because a degradation upstream of every
    /// snapshot could not otherwise be reached; this one is in the save because <c>02 §9</c>'s question
    /// — <em>why is this Lot vacant</em> — is asked about a world the player loaded, and an answer that
    /// died with the process would leave the emptiest part of a city the least explicable.
    /// </remarks>
    public CondemnationTrailTable CondemnationTrail { get; }

    /// <summary>The Bin Rules this world runs. Ids and integers; no string reaches here.</summary>
    /// <remarks>
    /// <b>A constructor argument rather than a load, for <see cref="MapLayers.Ruleset"/>'s reason.</b>
    /// The core cannot read a file (<c>02 §1</c>) and cannot name the parser (<c>adr/0048</c>), so a
    /// Ruleset arrives already validated or not at all. Slice 8 makes it swappable at a phase boundary;
    /// this slice loads one at world creation and leaves it.
    /// </remarks>
    public Ruleset Rules { get; private set; }

    /// <summary>
    /// Puts a different Ruleset in force, degrading whatever the new one cannot describe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Internal, so the door stays one door.</b> A reload reaches the world through Phase 0 like
    /// everything else (<c>Simulation.Step</c>), which is what keeps the Input Log a complete
    /// description of a session. A public setter would let a shell swap Rules between Ticks, and a
    /// replay would reproduce the commands and not the city.
    /// </para>
    /// <para>
    /// <b>A reload that moves only numbers still changes no row</b>, which is slice 8 task 1's
    /// behaviour and is kept rather than subsumed. <see cref="RulesetShape.Compare"/> is what tells
    /// the two apart, and the reason a tuning swap may skip the pass is the corpus's own reason for
    /// the pass existing: <c>02 §4.3</c> drops the wait lists because <em>a subscription taken under
    /// the old Ruleset may name a Bin the new one does not have</em>, and under
    /// <see cref="RulesetChange.None"/> it demonstrably cannot.
    /// </para>
    /// <para>
    /// <b>Every refusal runs before anything moves, and that ordering is the requirement rather than
    /// a tidiness.</b> Two things can still refuse a reload outright — a world-creation Layer constant
    /// (<see cref="MapLayers.Adopt"/>) and a Resource that changes family
    /// (<see cref="RulesetMigration.FamilyChanged"/>) — and <c>adr/0015</c>'s whole polarity is that a
    /// swap this build cannot perform correctly must leave the previous Ruleset live rather than
    /// half-happen.
    /// </para>
    /// </remarks>
    /// <param name="rules">The Ruleset to put in force.</param>
    /// <param name="contentHash">
    /// That Ruleset's content hash, which is how the trail names it. <c>Core</c> cannot compute one —
    /// it never sees the file (<c>adr/0048</c>) — so it arrives with the Ruleset or the provenance
    /// trail records a transition it cannot identify. It is a parameter rather than a second call for
    /// <see cref="UnplacedTable.Join"/>'s reason: one call writes both halves, so there is no door
    /// through which a migration and its record can disagree.
    /// </param>
    /// <param name="now">The current Tick, which the refit's arming is relative to.</param>
    /// <param name="key">The world key, for the stagger draw.</param>
    /// <returns>What the reload cost the city, for the shell to turn into a warning.</returns>
    internal RulesetDegradation Adopt(Ruleset rules, ulong contentHash, Ticks now, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(rules);

        RulesetChange change = RulesetShape.Compare(Rules, rules);
        RulesetMigration? migration = null;

        if (change != RulesetChange.None)
        {
            migration = RulesetMigration.Between(Rules, rules);

            if (migration.FamilyChanged.Raw != 0)
            {
                throw new NotSupportedException(
                    $"Resource {migration.FamilyChanged.Raw} changes family, and live Bins hold its "
                    + "stock. adr/0024 makes conservation a property the whole Rule engine enforces, "
                    + "so a Good becoming Money would make every unit already banked either created "
                    + "or destroyed by an edit. There is no honest degradation for that.");
            }
        }

        // Every refusal has run by here, so what follows cannot leave the world half-migrated.
        Layers.Adopt(rules.Layers);
        Roads.Adopt(rules.Roads);
        Rules = rules;

        RulesetDegradation cost = migration is null ? default : Migrate(migration, now, key);

        // adr/0064, and it runs for every swap rather than only a structural one. A capacity edit is
        // a RulesetChange.None change, so it skips Migrate entirely -- which is exactly how the edit
        // used to reach no Building already standing. After Migrate, because that pass frees Bins and
        // creates others, and a rebuild before it would derive ceilings for rows about to move.
        RebuildCapacities();

        // adr/0114, and it runs for every swap for RebuildCapacities' reason: a Ruleset that adds a
        // conserved Resource is a RulesetChange.None edit if it declares no new kind, so a treasury
        // that was fitted only at world creation would never acquire the Bin. It adds and never
        // removes -- see FitTreasury.
        FitTreasury();

        // The Pools, for the same reason one rung down: a Ruleset that adds a Good must reach the
        // Districts that already exist, and the watershed is the only other thing that fits them --
        // which runs on a cadence measured in Days, so waiting for it would leave a hot reload's new
        // Good unpoolable for the rest of the Day. ⚠ NOT in the constructor beside FitTreasury: a
        // world under construction has no Districts, so a call there would be one that cannot do
        // anything, and NOT in RebuildDerived either -- fitting CREATES saved rows, and a rebuild that
        // creates saved state is a load that moves the State Hash it just checked.
        FitDistrictPools();

        // And the Water Bodies, for the same reason again: a file that starts stating a water Bin has
        // to reach the bodies the generator already laid, and nothing else fits them -- the generator
        // runs once, at world creation, and never again (adr/0021). milestone 24 task 6b.
        FitWaterBins();

        // And the actors, for the same reason on the same trigger: a file that adds money has to reach
        // every Household standing, not only the ones built after the swap. O(actors) rather than
        // O(1), which is the class RebuildCapacities and EvictOverflow already put this pass in.
        FitBalances();

        // adr/0068's other half, and it runs after the rebuild for the same reason the rebuild runs
        // after Migrate: this reads the kinds the incoming Ruleset declares, and a Building whose kind
        // the migration is about to change would be measured against the wrong ceiling. A world that
        // has no Buildings yet -- every world at construction -- walks a table of nothing.
        EvictOverflow(now, key);

        // Recorded after the pass rather than before it, because a degradation this build refuses
        // half-way through would otherwise leave a trail entry claiming a transition that did not
        // happen -- and a provenance trail nobody can trust is worse than none, since the whole of its
        // value is being believed about a Tick nobody can replay to.
        // 5a-bis's trap, and the reason this line is here rather than left to the next write: a
        // derived structure that caches a Ruleset value reads as *absent* rather than as *stale*
        // before its first rebuild, and absent is the state every guard is written against. The
        // Shift start band is per kind and the Shift length band is [jobs]', so retuning either
        // moves the standing city's departures -- adr/0064's disposition, on a third axis.
        Commutes.Rebuild(Citizens, Buildings, Businesses, rules, Key);

        RulesetTrail.Record(now, contentHash, cost);

        return cost;
    }

    /// <summary>
    /// The one pass over the world a structural reload needs: drop, remap, derelict, refit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The order is <see cref="DestroyBuilding"/>'s order, for the same reason.</b> The Rule
    /// Instances go first so that nothing is asleep on a Bin by the time Bins start disappearing —
    /// <c>02 §4.3</c>: <em>all wait lists are dropped and every Rule is woken with a stagger</em>,
    /// because a subscription taken under the old Ruleset may name a Bin the new one does not have,
    /// <em>which also means a wait list is never cross-version state</em>. Then the Bins are remapped
    /// and the casualties freed; then the kinds, whose casualties are derelict rather than freed; then
    /// the refit, which needs the kind already remapped to know what to fit.
    /// </para>
    /// <para>
    /// <b>Four passes rather than one loop doing four things.</b> The steps are not independent —
    /// step 4 allocates rows step 2 has just freed — so interleaving them is a different assignment of
    /// slots and therefore a different State Hash. Both orders are deterministic; this one is the one
    /// the corpus describes, and it is written as four walks so that it stays that one.
    /// </para>
    /// <para>
    /// <b>There is no reload-specific stagger, and that is a decision rather than an omission.</b>
    /// Slice 7 derived the arming offset as uniform over <c>[1, rate]</c> because a Rule re-arms at
    /// <c>+rate</c> for ever and no other window stays spread. A reload re-arms every Rule Instance in
    /// the world at once, which is the largest instance of exactly that problem, so the answer is
    /// already the right one — see <see cref="ArmingStagger"/>.
    /// </para>
    /// </remarks>
    private RulesetDegradation Migrate(RulesetMigration migration, Ticks now, WorldKey key)
    {
        int derelicted = 0;
        int dropped = 0;
        int rearmed = 0;

        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (!Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            int instance = BuildingRules.PopFront(slot);

            while (instance != Rows.NoSlot)
            {
                Unlink(instance);
                RuleInstances.Rows.Free(RuleInstances.Rows.At(instance));
                instance = BuildingRules.PopFront(slot);
            }
        }

        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (Buildings.Rows.IsLive(slot))
            {
                dropped += RemapBins(slot, migration);
            }
        }

        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (!Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            byte was = Buildings.Kind[slot];
            byte becomes = migration.Kind(was);

            Buildings.Kind[slot] = becomes;

            // Only a Building that had a kind can lose one. A world running on Ruleset.Empty is
            // already full of Buildings at kind 0, and counting those would report a city's whole
            // stock as casualties of a reload that touched nothing.
            if (was != 0 && becomes == 0)
            {
                derelicted++;
            }
        }

        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (Buildings.Rows.IsLive(slot))
            {
                rearmed += Fit(Buildings.Rows.At(slot), Buildings.Kind[slot], now, key);
            }
        }

        // The tenants, after every Building has its own (adr/0141). It is a second loop rather than a
        // call inside the first because Fit is keyed on a Building and a tenancy is keyed on a
        // Household -- an unhoused one has no premises to refit through and must be skipped, which
        // FitOccupant answers by returning zero rather than by being called conditionally here.
        //
        // ⚠ IT HAS TO RUN AT ALL, and that is the half a Building-only refit would have missed: the
        // migration frees every Rule Instance in the world, tenants' included, so a reload without
        // this leaves every Household holding Bins and running nothing.
        for (int slot = 0; slot < Households.Rows.SlotCount; slot++)
        {
            if (Households.Rows.IsLive(slot))
            {
                rearmed += FitOccupant(Households.Rows.At(slot));
            }
        }

        // The trade, on the second kind namespace (adr/0141). A Business whose [[business]] the new
        // file does not name keeps its row, its premises and its balance and loses only the word --
        // so this is NOT counted in `derelicted`, which is a Building's word for a Building's loss.
        //
        // ⚠ AND THEN IT REFITS, which the Household loop above has always done and this one did not.
        // The comment here read "a Business kind declares nothing yet, so there is nothing else to
        // lose" -- true until adr/0166, and false the moment a Business runs Rules: the migration
        // frees every Rule Instance in the world, a trader's included, so a reload without the
        // second half leaves every shop holding stock and running nothing. It is FitOccupant's own
        // "it has to run at all" arriving at the other Occupant.
        //
        // The kind first and the refit second, in one loop rather than two, because unlike the
        // Household case the two are keyed on the same row -- and the refit reads the kind through
        // the PREMISES rather than through Businesses.Kind, so the order is a tidiness rather than a
        // dependency.
        for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
        {
            if (Businesses.Rows.IsLive(slot))
            {
                Businesses.Kind[slot] = migration.BusinessKind(Businesses.Kind[slot]);
                rearmed += FitBusiness(Businesses.Rows.At(slot));
            }
        }

        return new RulesetDegradation(derelicted, dropped, rearmed);
    }

    /// <summary>
    /// Rewrites one Building's Bins to the incoming Ruleset's ids, freeing those whose Resource went.
    /// </summary>
    /// <remarks>
    /// <b>A rotation rather than a removal from the middle.</b> The list is popped from the front and
    /// survivors are appended to the back exactly as many times as it is long, which restores the
    /// original order — and the original order is ascending by slot, which
    /// <see cref="RebuildDerived"/> depends on because a derived list has to be reproducible from the
    /// owner columns alone. Removing in place would be <c>O(k²)</c> over a list of three and would
    /// need a re-scan after every removal to stay safe against the walk it was mutating.
    /// <para>
    /// <b>The wait lists are not woken here, unlike in <see cref="DestroyBuilding"/>.</b> They are
    /// already empty: every Rule Instance in the world was unlinked and freed before this ran, which
    /// is the whole reason that step comes first.
    /// </para>
    /// </remarks>
    private int RemapBins(int buildingSlot, RulesetMigration migration)
    {
        int length = 0;

        foreach (int _ in BuildingBins.Walk(buildingSlot))
        {
            length++;
        }

        int dropped = 0;

        for (int i = 0; i < length; i++)
        {
            int bin = BuildingBins.PopFront(buildingSlot);
            ResourceId becomes = migration.Resource(Bins.Resource[bin]);

            if (becomes.Raw == 0)
            {
                Bins.Rows.Free(Bins.Rows.At(bin));
                dropped++;
                continue;
            }

            Bins.Resource[bin] = becomes;
            BuildingBins.Append(buildingSlot, bin);
        }

        return dropped;
    }

    /// <summary>Every table, in the declaration order the hash folds them in.</summary>
    public ReadOnlySpan<Rows> Tables => _tables;

    /// <summary>
    /// <see cref="Tables"/> minus the tables <b>no Tick phase can write</b>. For the Decide guard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A subset of the composition and never a second composition.</b> The State Hash folds
    /// <see cref="Tables"/> and must keep folding all of it — that is <c>05 §4</c>'s coverage
    /// guarantee, and a column outside it is a column the hash cannot see. What this narrows is
    /// <see cref="Simulation.VerifyDecideWritesNothing"/>, which asks a different question:
    /// ***did Phase 2 write anything?*** — and a table nothing writes <em>at all</em> is not evidence
    /// either way.
    /// </para>
    /// <para>
    /// 🔴 <b>It exists because the guard's cost is <c>O(world)</c> and terrain made the world big.</b>
    /// Milestone 24 task 2's <see cref="Space.TerrainCellTable"/> is dense — one row per Cell, 262,144
    /// of them — and the guard folds everything <b>twice a Tick</b>, so terrain became about
    /// <b>ninety per cent</b> of what a fold walks. MEASURED on a quiet machine: a Tick went
    /// <b>0.03 ms → 4.14 ms</b> with the guard on, and the assertion tier <b>3m11s → 4m19s</b>
    /// (`plans/0042` F8, `plans/0013`).
    /// </para>
    /// <para>
    /// ⚠ <b>The guard is narrowed and NOT weakened, and the second half is what makes that true.</b>
    /// Skipping a table on trust would be exactly the silent hole this project keeps finding — so
    /// terrain is checked instead by <c>WorldInvariants.TerrainIsWhatItsWorldKeyGenerates</c> at the
    /// <see cref="Invariants.InvariantTier.EndOfRun"/> tier, which re-runs the generator and compares
    /// every Cell. ***That is a stronger statement than the guard was making***: the guard could only
    /// say Decide did not move it, and this says nothing anywhere did. It is `02 §10`'s own rule that
    /// invariants are sorted by frequency — this one moved from twice a Tick to once a run, because
    /// once a run is where a check on a thing that never changes belongs.
    /// </para>
    /// <para>
    /// ⚠ <b>Adding a table here is a decision and not a tidy-up.</b> The test is not *is it expensive*
    /// but *can any phase write it* — and the day terraforming ships, terrain stops qualifying and
    /// comes out.
    /// </para>
    /// </remarks>
    public ReadOnlySpan<Rows> TablesAPhaseCanWrite => _writableTables;

    /// <summary>
    /// The three tiers of <c>02 §10</c>, and the channel the write sites below report through.
    /// </summary>
    /// <remarks>
    /// <b>Owned here rather than by the Simulation, because the claims are about this world.</b> The
    /// Simulation drives the tiers — when a slice runs is a Tick concern — but a world built by hand
    /// for a test has the same invariants as one a session played, and it would be able to violate
    /// them silently if the registry arrived with the Tick loop.
    /// </remarks>
    public InvariantRegistry Invariants { get; }

    /// <summary>
    /// The State Hash: every saved column of every table, folded through slice 2's <c>mix</c>.
    /// </summary>
    /// <remarks>
    /// <b>Values, never identity.</b> A handle's index is identity — a slot address assigned by a
    /// free list — so a handle column folds the target row's monotonic never-reused id instead. See
    /// <see cref="HandleColumn{TTarget}"/> for what that buys and what it costs.
    /// </remarks>
    public ulong HashState()
    {
        ulong hash = HashSeed;

        foreach (Rows table in _tables)
        {
            table.Fold(ref hash);
        }

        return hash;
    }

    /// <summary>Adds a Household to a Building, linking it into the Building's occupant list.</summary>
    public Handle<Household> CreateHousehold(Handle<Building> dwelling, byte lifeStage)
    {
        int buildingSlot = Buildings.Rows.Resolve(dwelling);

        Handle<Household> handle = Households.Rows.Allocate();
        int slot = Households.Rows.Resolve(handle);

        Households.Dwelling[slot] = dwelling;
        Households.LifeStage[slot] = lifeStage;

        // adr/0114: a balance is a Bin, opened here so that a Household never exists without one in a
        // world whose Ruleset names money. Empty -- World.Endow is the only door money enters by.
        if (TryMoneyResource(out ResourceId money))
        {
            // adr/0143: the LIST is the saved truth and Balance is derived from it, so the append is
            // the write that matters and the assignment is a derived column maintained at its write
            // site -- the same shape as BuildingBins.InsertOrdered in CreateBin.
            Handle<Bin> balance = OpenBalance(BinOwnerKind.Household, money);

            AppendOwnerBin(Households.BinHead, Households.BinTail, slot, balance);
            Households.Balance[slot] = balance;
        }

        Invariants.Require(
            !Lists(Occupants, buildingSlot, slot),
            Invariant.HouseholdIsNotAlreadyInThisBuilding,
            slot,
            buildingSlot);

        Occupants.InsertOrdered(buildingSlot, slot);

        // The tenancy starts here, so the tenant's Bins and Rules do (adr/0141). It runs after the
        // occupant list is joined because FitOccupant reads the dwelling handle to find the kind
        // whose ceilings it opens Bins at.
        FitOccupant(handle);

        return handle;
    }

    /// <summary>
    /// Puts a Business into a Building, linking it into the premises' Business list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It opens with a zero balance and there is no door that funds one</b> (<c>adr/0113</c>).
    /// Milestone 10 needs exactly one property of a Business — somewhere for conserved money to sit
    /// that is not a Building — and the counterparty that would pay it is milestone <b>13</b>'s price
    /// surface. <see cref="Endow"/> is deliberately not widened to reach here: money entering the
    /// world is a founding act about Households, and a Business funded from nowhere is the failure
    /// <see cref="Invariant.MoneyIsConserved"/> exists to report.
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing in the simulation calls this.</b> A Business is placed by no pass, because what
    /// would place one is commercial and industrial placement, which is milestone <b>13</b>. The door
    /// exists so the table is reachable and its balance is testable, which is <c>adr/0070</c>'s
    /// <em>unbuilt</em> rather than an omission.
    /// </para>
    /// </remarks>
    /// <param name="premises">The Building it occupies.</param>
    /// <param name="kind">
    /// Which trade, indexed into <c>[[business]]</c> — <b>not</b> the premises' kind namespace
    /// (<c>adr/0141</c>). <b>Zero, the default, means this Business names no trade</b>, which is a
    /// legal state and the one every fixture predating milestone 27 is in.
    /// <para>
    /// ⚠ <b>It is optional rather than required, and that is an argument about consequence rather
    /// than tidiness.</b> <see cref="CreateBuilding"/>'s kind is required because a Building cannot be
    /// fitted without one — its Bins and Rules are read off it. A Business kind declares
    /// <em>nothing</em> until milestone 27's task 7 gives it <c>jobs</c>, so requiring it here would
    /// force seventeen call sites to name a value nothing reads. ***When the kind acquires a
    /// consequence, making it required is a change with a reason behind it.***
    /// </para>
    /// </param>
    public Handle<Business> CreateBusiness(Handle<Building> premises, byte kind = 0)
    {
        // A DEFAULT handle is accepted and means unpremised, which milestone 27 task 8 made
        // reachable (adr/0145): a founded Business is created with no premises and looks for them
        // from the pool. It is not a new state -- BusinessTable.Building is Reference.Severable and
        // adr/0142 makes unpremised a legitimate steady state -- it is the state finally being
        // expressible at CREATION rather than only arrived at by demolition.
        bool premised = Buildings.Rows.TryResolve(premises, out int buildingSlot);

        Handle<Business> handle = Businesses.Rows.Allocate();
        int slot = Businesses.Rows.Resolve(handle);

        Businesses.Building[slot] = premises;
        Businesses.Kind[slot] = kind;

        // CreateHousehold's line, for its reason (adr/0114).
        if (TryMoneyResource(out ResourceId money))
        {
            Handle<Bin> balance = OpenBalance(BinOwnerKind.Business, money);

            AppendOwnerBin(Businesses.BinHead, Businesses.BinTail, slot, balance);
            Businesses.Balance[slot] = balance;
        }

        // Only when there are premises to list it against. An unpremised Business is in the pool's
        // list and in no Building's.
        if (premised)
        {
            BuildingBusinesses.InsertOrdered(buildingSlot, slot);
        }

        return handle;
    }

    /// <summary>
    /// Gives a Household money that did not exist before. <b>The only way money enters this world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One door, because conservation is a claim about doors.</b> <c>adr/0031</c> asks that nothing
    /// be created or destroyed except at the gate, and a claim of that shape is only checkable if the
    /// gates can be counted. Writing <c>Households.Money</c> directly is therefore a defect rather than
    /// a shortcut — <see cref="Invariant.MoneyIsConserved"/> reports it, because the balance moved and
    /// <see cref="MoneySupplyTable.Issued"/> did not. This is <see cref="Deposit"/>'s argument on the
    /// money axis: there is no second spelling in which the other half can be forgotten.
    /// </para>
    /// <para>
    /// <b>It is the founding door and it is not the gate.</b> Money's only runtime source and sink is
    /// the Outside Connection (<c>CONTEXT.md</c> → Money), which is milestone <b>11</b>; until that
    /// exists a world's supply is fixed at whatever it was founded with, which is what makes the
    /// invariant an exact equality rather than a sum with a flow term. Nothing in the build calls this
    /// yet — no production writer sets a Household's money at all — so every world the simulation can
    /// make on its own is founded on nothing, and the check is correct and temporarily trivial.
    /// </para>
    /// <para>
    /// ⚠ <b>It refuses a Household with no balance rather than founding one silently.</b> A balance is
    /// a Bin since <c>adr/0114</c> and a Bin exists only for a Resource the Ruleset declares, so an
    /// unset handle means <em>this world's Ruleset names no money</em> — and endowing there would be
    /// asked to put a quantity somewhere the file says does not exist. ***A door that creates the
    /// thing it was asked to fill cannot report that the request was wrong.*** The failure is loud
    /// because the caller is a fixture author, and the fix is the Ruleset rather than the call.
    /// </para>
    /// <para>
    /// ⚠ <b>It took an <c>onHand</c> and a <c>savings</c> until task 4c, and <c>savings</c> is gone
    /// rather than folded in.</b> Two amounts existed because two columns did; every design sentence
    /// about savings describes a <em>threshold</em> rather than a second pile, so what a Household has
    /// set aside is a reserve computed from its Life Stage when something spends money — milestone
    /// <b>14</b> — and not a quantity a founder can hand it.
    /// </para>
    /// </remarks>
    /// <param name="household">Who is being endowed.</param>
    /// <param name="amount">Money to add to its balance.</param>
    /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
    /// <exception cref="InvalidOperationException">The Ruleset in force names no money.</exception>
    public void Endow(Handle<Household> household, Money amount)
    {
        // Refused rather than treated as a withdrawal. A negative endowment would be money leaving the
        // world through the door money comes in by, which is the gate's job and the gate does not
        // exist -- and adr/0003's signed Money makes it representable, so nothing else would notice.
        ArgumentOutOfRangeException.ThrowIfNegative(amount.Raw, nameof(amount));

        int slot = Households.Rows.Resolve(household);
        Handle<Bin> balance = Households.Balance[slot];

        if (balance.IsNone)
        {
            throw new InvalidOperationException(
                $"household {slot} has no balance, so the Ruleset in force declares no money Resource "
                + "(adr/0114: a balance is a Bin, and a Bin exists only for a Resource a Ruleset "
                + "names). Endowing would put money where the file says money does not exist. Load a "
                + "Ruleset with a `family = \"money\"` [[resource]] block.");
        }

        // Through Deposit rather than into the level, for Deposit's own reason: a Bin written without
        // draining its wait list leaves whoever was short of money asleep for ever. Founding a balance
        // is exactly the arrival a waiter is waiting for.
        Deposit(balance, amount.Raw, Tick);

        MoneySupply.Issued[MoneySupplyTable.Slot] += amount;
    }

    /// <summary>
    /// What a Household holds.
    /// </summary>
    /// <remarks>
    /// <b>Zero when the Ruleset in force names no money</b>, which is a different fact from a
    /// Household that has spent everything and is deliberately not distinguished here. The two are
    /// distinguishable — <c>Households.Balance[slot].IsNone</c> — and nothing needs to be: a world with
    /// no currency and a Household with none behave identically at every call site money has.
    /// </remarks>
    public Money BalanceOf(Handle<Household> household) =>
        Bins.Rows.TryResolve(Households.Balance[Households.Rows.Resolve(household)], out int bin)
            ? new Money(Bins.LevelAt(bin))
            : Money.Zero;

    /// <inheritdoc cref="BalanceOf(Handle{Household})"/>
    public Money BalanceOf(Handle<Business> business) =>
        Bins.Rows.TryResolve(Businesses.Balance[Businesses.Rows.Resolve(business)], out int bin)
            ? new Money(Bins.LevelAt(bin))
            : Money.Zero;

    /// <summary>
    /// A Citizen founds a Business, spending part of their Household's balance to capitalise it, and
    /// becomes its first worker.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0145</c>'s founding channel, and it is a TRANSFER rather than an issuance.</b> The
    /// band moves from the founder's balance Bin into the new Business's, so
    /// <c>MoneySupply.Issued</c> is untouched and <c>Invariant.MoneyIsConserved</c> needs no new case.
    /// ⚠ <b>That is the half of <c>adr/0145</c> that decided the shape</b>: had founding issued, this
    /// would be the third production door into a money supply whose map already miscounts at two.
    /// </para>
    /// <para>
    /// <b>The Business is created UNPREMISED and joins the pool</b>, with no gate — a founder is
    /// inside the city and came through no door. Nothing is tenanted here, which is
    /// <c>adr/0069</c>'s <em>construction houses nobody</em> holding on the commercial side:
    /// placement is what puts an occupant into standing stock.
    /// </para>
    /// <para>
    /// 🔴 <b>A Business founded here and never tenanted EXPORTS this money.</b>
    /// <see cref="Depart(Handle{Business})"/> subtracts the balance from the supply when the give-up
    /// bound expires (<c>adr/0142</c>), so found-then-fail is a one-way leak of household wealth out
    /// of the city. ***It is emergent from two ADRs and neither shows it alone***, which is why it is
    /// written at the site as well as in the record.
    /// </para>
    /// </remarks>
    /// <param name="founder">
    /// The Citizen founding it. Their Household puts up the capital and must be able to afford it,
    /// and they must be unemployed — <see cref="Employ"/> would otherwise move them off the list they
    /// are on, which is a resignation nobody asked for.
    /// </param>
    /// <param name="kind">Which trade, indexed into <c>[[business]]</c>.</param>
    /// <param name="band">What the founder's Household spends. Never negative.</param>
    /// <param name="now">The Tick the spell in the pool begins.</param>
    /// <returns>The Business, already in the unpremised pool, with its founder on its worker list.</returns>
    public Handle<Business> Found(
        Handle<Citizen> founder, byte kind, Money band, Ticks now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(band.Raw, nameof(band));

        int citizenSlot = Citizens.Rows.Resolve(founder);
        int founderSlot = Households.Rows.Resolve(Citizens.HouseholdOf[citizenSlot]);
        Handle<Bin> from = Households.Balance[founderSlot];

        if (from.IsNone)
        {
            throw new InvalidOperationException(
                "a Household with no balance Bin cannot found a Business. The caller tests "
                + "affordability against BalanceOf, which reads zero for such a Household, so "
                + "reaching here means the band was zero -- which the loader refuses.");
        }

        // Affordability is NOT re-asserted here, and deliberately: Withdraw already requires the
        // amount to be within the Bin's level, under Invariant.BinLevelIsWithinCapacity, which is
        // the invariant that actually names this condition. A second Require would either duplicate
        // it or -- worse -- label an affordability failure as a conservation failure, which is a
        // wrong diagnosis written into the one channel that exists to give a right one.

        // No premises: the Business is created unpremised and looks for them from the pool. The
        // default handle is what BusinessTable.Building's severable declaration already expects.
        Handle<Business> business = CreateBusiness(default, kind);

        // Out before in, so no instant exists in which the band is in two Bins at once. A reader
        // folding the State Hash between the two would see money that is not there.
        Withdraw(from, band.Raw, now);

        int slot = Businesses.Rows.Resolve(business);
        Handle<Bin> into = Businesses.Balance[slot];

        if (into.IsNone)
        {
            throw new InvalidOperationException(
                "a Business was created with no balance Bin while a Ruleset declaring [founding] was "
                + "in force. CreateBusiness opens one whenever the Ruleset declares a money Resource "
                + "and the loader refuses [founding] without one, so the two have drifted.");
        }

        Deposit(into, band.Raw, now);

        // No gate: see UnpremisedTable.Gate. A founder came from inside the city.
        UnpremisedPool.Join(Businesses, business, default, now);

        // ⚠ THE LABOUR COST, and it is the whole of what adr/0146 ships. The founder becomes the
        // Business's first worker, so the employment pass will not hire them and the city is one
        // worker down -- a cost with no wage attached is still a cost, because a Citizen is a scarce
        // thing. THE INCOME HALF IS adr/0026 AT MILESTONE 15 and must not be proxied here: "the
        // founder's job pays nothing until the Business earns" is that ADR running on a Business with
        // an empty Bin, and a 27-shaped stand-in would be a second, worse answer somebody has to find
        // and delete on the day 15 lands.
        //
        // ⚠ AND IT IS WHY NO `founder` COLUMN EXISTS. The link is the job, which is a column that
        // already had to be there -- declaring a severable handle from BusinessTable to CitizenTable
        // would make the two tables mutually dependent at construction, and they are built in one
        // ordered pass.
        //
        // Ticks.Zero for the planned commute, and it is a fact rather than a placeholder: the
        // employer is unpremised, so there is no journey to plan. CommuteRoster.Add reads the
        // premises through the Business and declines to bucket a worker whose employer has none --
        // which is Unpremise's "the jobs SURVIVE, the journey does not" reached from the other end.
        Employ(founder, business, Ticks.Zero);

        return business;
    }

    /// <summary>
    /// Moves a housed Household into the Unplaced Pool, keeping its balance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eviction is free, and that is <c>adr/0054</c>'s finding rather than a convenience.</b> This
    /// touches the dwelling handle and the occupant list. It does not touch <see cref="Household"/>'s
    /// balance handle, so <em>"a Household keeps what it owns when the city stops housing it"</em>
    /// needed no code — it is what not writing to those columns already means, and it is what keeps
    /// demolition from becoming a hole in <c>adr/0024</c>'s conserved Money.
    /// </para>
    /// <para>
    /// <b>Only a housed Household can be unplaced, and the check is why it is <c>O(1)</c>.</b>
    /// Unplacing one already in the Pool would give it a second membership row, and the draw would
    /// then favour it in proportion to how many times it had been unplaced. Detecting that afterwards
    /// costs a walk of the Pool; refusing it here costs the handle resolve already being done.
    /// </para>
    /// </remarks>
    /// <param name="household">The Household to evict.</param>
    public void Unplace(Handle<Household> household)
    {
        int slot = Households.Rows.Resolve(household);

        if (!Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
        {
            Invariants.Report(Invariant.OnlyAHousedHouseholdIsUnplaced, slot);
            return;
        }

        // Before the dwelling handle is cleared, because the tenant's Rule Instances are found
        // through the premises' Rule list and its Bins were opened at that kind's ceilings.
        UnfitOccupant(household);

        Occupants.Remove(buildingSlot, slot);
        Households.Dwelling[slot] = default;

        // The write-site half of the Pool's density claim. The table needs the allocator to hand back
        // exactly the next position, which is true because the free list is LIFO and Leave only ever
        // frees the last slot — an implementation detail of Rows that nothing in Rows promises. Left
        // to the end-of-run walk, a change there would surface as a city that had been quietly
        // building less than its Ruleset said for the length of a run.
        // No gate: an eviction is one of the three entry routes that came from nowhere outside
        // (adr/0129), so this Household's move-in has no origin to start from and the column says so
        // rather than borrowing one.
        Invariants.Require(
            UnplacedPool.Join(Households, household, default, Tick) == UnplacedPool.Count - 1,
            Invariant.ThePoolAppendsInOrder,
            slot);
    }

    /// <summary>
    /// Creates a Household that has never lived here, straight into the Unplaced Pool at
    /// <paramref name="gate"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The second door into the Pool, and it exists because the first one refuses this case by
    /// invariant</b> (<c>adr/0129</c>, milestone 11 task 4). <see cref="CreateHousehold"/> demands a
    /// dwelling and <see cref="Unplace"/> reports
    /// <see cref="Invariant.OnlyAHousedHouseholdIsUnplaced"/>, so between them the build could not
    /// hold a Household that had never been housed — while <c>CONTEXT.md</c> → Unplaced Pool says the
    /// Pool's four entry routes <em>"all enter on equal terms"</em>. ***A door the design describes
    /// and an invariant refuses is a disagreement, not a defect***, and this is which one moved.
    /// </para>
    /// <para>
    /// <b>The Household joins the Pool and makes no Trip.</b> <c>adr/0023</c> reads *arrive as Trips,
    /// enter the Pool, house themselves*, and that order cannot be built as written:
    /// <c>TripTable.Start</c> takes an origin <em>and a destination</em> Address, and a Household the
    /// Pool has not placed yet has no destination. ***A journey described in prose can name an
    /// endpoint the mechanism has to compute.*** The Trip is the move-in, gate → dwelling, and it
    /// belongs to task 6.
    /// </para>
    /// <para>
    /// <b>The daily ceiling binds here, which is the only place it can</b> (<c>adr/0088</c>,
    /// <c>plans/0035</c> decision 9). <c>[[building]] arrivals_per_day</c> is a <em>rate</em>, so
    /// meeting it takes a count and the Day the count belongs to —
    /// <see cref="BuildingTable.ArrivalsToday"/> and <see cref="BuildingTable.ArrivalDay"/>. A bound
    /// applied per call would let two arrival events in one Tick each take the whole quota, and would
    /// read as a daily ceiling while being nothing of the kind.
    /// </para>
    /// <para>
    /// ⚠ <b>A refusal on the ceiling is <c>false</c> with no invariant reported, and a refusal on the
    /// gate is both.</b> The first is the mechanism working; the second is a caller naming a Building
    /// that admits nobody. Collapsing them would put the ceiling's ordinary operation into the crash
    /// artifact, and would leave the real fault indistinguishable from a busy Day.
    /// </para>
    /// <para>
    /// <b>Money crosses with them, and this is
    /// <see cref="MoneySupplyTable.Issued"/>'s second writer</b> (<c>adr/0131</c>, milestone 11 task
    /// 5) — the first thing in the project that moves the supply after the founding, so a world's
    /// money is no longer a constant. The amount is drawn from the Hinterland behind the gate's
    /// edge, uniformly over its band, on the Household's own id. <b><see cref="Endow"/> is still the
    /// only door money enters by</b>: it deposits through the Bin's wait list and writes the anchor
    /// in one call, so <see cref="Invariant.MoneyIsConserved"/> needs no flow term and is unchanged.
    /// </para>
    /// <para>
    /// ⚠ <b>How many people arrive is <em>stated</em> by the caller and is not modelled here.</b>
    /// <c>CONTEXT.md</c> → Life Stage makes composition — *how many adults, how many children* — a
    /// property of the stage, and that table is <c>adr/0011</c>'s and Phase 2's; nothing in the build
    /// maps one to the other. The three alternatives were a chosen constant (a hash-bearing number
    /// with no ratifier under <c>adr/0052</c>), the city's own population-to-Household ratio (a
    /// stand-in returning plausible results, which is milestone 9's F13), and authoring the stage
    /// table early (<c>adr/0131</c>'s rule inverted — a field authored in a milestone that does not
    /// read it). ***An instrument states what it is standing in for, and does not model it***, which
    /// is <see cref="Input.CommandKind.Arrive"/>'s posture for the Life Stage already.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>A gate with no <c>[[hinterland]]</c> behind its edge admits nobody, and the refusal is
    /// F13 rather than strictness.</b> A Household admitted through such a gate would carry zero —
    /// and zero is a *legitimate* answer, because <see cref="HinterlandDefinition.Endows"/> says a
    /// Hinterland whose emigrants arrive penniless is a real economy. Admitting would make *nowhere*
    /// and *somewhere poor* the same observation. ***A zero that is a real answer cannot double as
    /// the absence of an answer.*** See <see cref="Invariant.AGateOpensOntoAHinterland"/>, which
    /// also records where the two checks that are not built would go.
    /// </para>
    /// </remarks>
    /// <param name="gate">The Outside Connection they entered by.</param>
    /// <param name="lifeStage">Which Life Stage arrives. The mix is milestone 16's; here it is stated.</param>
    /// <param name="now">The Tick the arrival happens on, which is what names the Day.</param>
    /// <param name="citizens">How many people are in the Household. See the remarks.</param>
    /// <param name="household">The Household created, or a default handle when the gate refused.</param>
    /// <returns><c>true</c> when the gate admitted them.</returns>
    public bool TryArrive(
        Handle<Building> gate,
        byte lifeStage,
        byte citizens,
        Ticks now,
        out Handle<Household> household)
    {
        household = default;

        if (!Buildings.Rows.TryResolve(gate, out int gateSlot)
            || !TryArrivalsPerDay(Buildings.Kind[gateSlot], out int ceiling))
        {
            Invariants.Report(Invariant.AnArrivalCrossesAnOutsideConnection, gateSlot);
            return false;
        }

        // Before the meter, so a door onto nowhere does not burn a Day's quota being refused. The
        // edge is a property of where the gate was placed and not of the Ruleset, which is why the
        // loader cannot make this pairing and why it happens here.
        if (!Lots.Rows.TryResolve(Buildings.Lot[gateSlot], out int gateLot)
            || !Rules.TryHinterland(EdgeOf(gateLot), out HinterlandDefinition hinterland))
        {
            Invariants.Report(Invariant.AGateOpensOntoAHinterland, gateSlot);
            return false;
        }

        // FloorDiv rather than raw '/', which BOR0203 is an error for: a Tick count is unsigned and
        // never crosses zero, so the two agree here -- but stating the rounding is the rule and the
        // exception is not this method's to grant.
        int day = (int)IntegerMath.FloorDiv((long)now.Raw, Ticks.PerDay);

        // Lazily, rather than in a per-Day sweep: the reset is O(1) at the read site, costs nothing
        // on the Buildings nobody arrives at -- which is all of them in nine of the ten shipped
        // Rulesets -- and is right for a world loaded mid-Day, where a sweep that has already run
        // would leave the meter counting a Day that has passed.
        if (Buildings.ArrivalDay[gateSlot] != day)
        {
            Buildings.ArrivalDay[gateSlot] = day;
            Buildings.ArrivalsToday[gateSlot] = 0;
        }

        if (Buildings.ArrivalsToday[gateSlot] >= ceiling)
        {
            return false;
        }

        Buildings.ArrivalsToday[gateSlot]++;

        Handle<Household> handle = Households.Rows.Allocate();
        int slot = Households.Rows.Resolve(handle);

        Households.LifeStage[slot] = lifeStage;

        // CreateHousehold's line, for its reason (adr/0114). Empty: what an emigrant carries is drawn
        // from the Hinterland at task 5, and World.Endow is still the only door money enters by.
        if (TryMoneyResource(out ResourceId money))
        {
            // adr/0143: the LIST is the saved truth and Balance is derived from it, so the append is
            // the write that matters and the assignment is a derived column maintained at its write
            // site -- the same shape as BuildingBins.InsertOrdered in CreateBin.
            Handle<Bin> balance = OpenBalance(BinOwnerKind.Household, money);

            AppendOwnerBin(Households.BinHead, Households.BinTail, slot, balance);
            Households.Balance[slot] = balance;
        }

        // Money crosses here, which is MoneySupplyTable.Issued's second writer and the first thing in
        // this project that moves the supply after the founding. Endow is still the only door: it
        // deposits through the Bin's wait list and writes the anchor in one call, so there is no
        // spelling in which the second half can be forgotten (adr/0031).
        //
        // Drawn on the Household's monotonic id rather than its slot, because a slot is recycled and
        // two Households sharing one would draw the same balance -- 02 §8 rule 5, on the coordinate
        // rather than on the stream.
        Money carried = hinterland.EmigrantBalance(Key, Households.Rows.IdAt(slot));

        if (carried.Raw > 0 && !Households.Balance[slot].IsNone)
        {
            Endow(handle, carried);
        }

        // The people. A Household with no members is a Household nobody lives in, and it is also a
        // Household nobody can make the move-in Trip for -- adr/0075 makes a Traveller a cursor over
        // a CITIZEN's journey, so an empty Household would arrive and then never travel.
        //
        // They wake on the Tick they arrive, which is the honest reading of the Event Wheel's bucket
        // key for somebody who has just got here: their next event is this one.
        for (int i = 0; i < citizens; i++)
        {
            CreateCitizen(handle, now);
        }

        // The dwelling handle is left default by the allocator -- FreeSlot zeroes every column, so a
        // recycled slot arrives unhoused rather than carrying its predecessor's address.
        Invariants.Require(
            UnplacedPool.Join(Households, handle, gate, now) == UnplacedPool.Count - 1,
            Invariant.ThePoolAppendsInOrder,
            slot);

        household = handle;

        return true;
    }

    /// <summary>
    /// Takes a Household out of the Unplaced Pool and houses it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Keyed on the Household and not on its position, even though the caller drew a position.</b>
    /// A Zone Rule picks a member with a counter-based draw over <see cref="UnplacedTable.Count"/> —
    /// never the front of the queue and never the lowest slot, since <c>02 §8</c> rule 5's argument
    /// applies directly: a Pool that does not fully drain would otherwise leave the same Households
    /// unhoused for the life of the city, and no player could see why. But the draw's *result* is
    /// converted to a handle immediately, because a position is only valid until the next call.
    /// </para>
    /// <para>
    /// <b>That is the difference between a mistake that throws and one that houses the wrong
    /// family.</b> Leaving swaps the last member into the vacated position, so a caller holding two
    /// positions and using both would silently move somebody who was never drawn — and since the whole
    /// mechanism is a draw, the result looks exactly like a legitimate one. The reverse index makes
    /// the lookup free, so nothing is paid for the safer shape.
    /// </para>
    /// </remarks>
    /// <param name="household">The Household to house. Must currently be in the Pool.</param>
    /// <param name="dwelling">The Building they move into.</param>
    public void Place(Handle<Household> household, Handle<Building> dwelling)
    {
        int buildingSlot = Buildings.Rows.Resolve(dwelling);
        int slot = Households.Rows.Resolve(household);

        if (!Households.IsUnplaced(slot))
        {
            Invariants.Report(Invariant.OnlyAPooledHouseholdIsPlaced, slot);
            return;
        }

        // Resolved from the reverse index rather than taken from the caller. Leave moves the last
        // member into the vacated position, so a caller holding two positions and using both would
        // house the wrong Household with the second — and the swap makes that failure look like an
        // ordinary draw. Keyed on identity there is no stale value to hold.
        // adr/0068. The predicate is HasRoom and every caller is expected to have asked it -- a full
        // Building is an ordinary answer to placement and not a fault -- so reaching this is a caller
        // that placed without asking. Report-and-return rather than Require, on the same reasoning as
        // the check above: under Collect a Require records and falls through, which would house the
        // family anyway and leave the violation describing a world the run then kept running in.
        if (!HasRoom(buildingSlot))
        {
            Invariants.Report(Invariant.BuildingHasRoomForTheHousehold, slot, buildingSlot);
            return;
        }

        Handle<Household> left = UnplacedPool.Leave(Households, Households.PoolPosition(slot));

        Invariants.Require(left == household, Invariant.ThePoolNamesOnlyUnhousedHouseholds, slot);

        Households.Dwelling[slot] = dwelling;

        Invariants.Require(
            !Lists(Occupants, buildingSlot, slot),
            Invariant.HouseholdIsNotAlreadyInThisBuilding,
            slot,
            buildingSlot);

        Occupants.InsertOrdered(buildingSlot, slot);

        // adr/0141: housing a Household is the start of a tenancy, and a tenancy is what its own Bins
        // and Rules hang off.
        FitOccupant(household);
    }

    /// <summary>Adds a Citizen to a Household, linking it into the Household's member list.</summary>
    public Handle<Citizen> CreateCitizen(Handle<Household> household, Ticks nextEventTick)
    {
        int householdSlot = Households.Rows.Resolve(household);

        Handle<Citizen> handle = Citizens.Rows.Allocate();
        int slot = Citizens.Rows.Resolve(handle);

        Citizens.HouseholdOf[slot] = household;
        Citizens.NextEventTick[slot] = nextEventTick;

        // 02 §10's per-Tick tier: O(changed), at the write site. A member list is small by
        // construction, so this is the cheap half of *no Citizen in two places* — complete within
        // one Household and blind across two, which is what the end-of-run walk is for.
        Invariants.Require(
            !Lists(Members, householdSlot, slot),
            Invariant.CitizenIsNotAlreadyInThisHousehold,
            slot,
            householdSlot);

        Members.InsertOrdered(householdSlot, slot);

        // The departure bucket, and it is joined here for the reason the member list is: a derived
        // index maintained at the door is the only kind RebuildDerived can be checked against.
        Commutes.Add(Citizens, Buildings, Businesses, Rules, Key, slot);

        return handle;
    }

    /// <summary>Whether <paramref name="node"/> is already in <paramref name="owner"/>'s list.</summary>
    private static bool Lists(IndexList list, int owner, int node)
    {
        foreach (int element in list.Walk(owner))
        {
            if (element == node)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// An unhoused Household gives up looking and leaves the city, taking its money with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Unplaced Pool's sink, and the first one it has ever had</b>
    /// (<see href="../../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md">adr/0130</see>,
    /// milestone 11 task 7). Until the gate opened, the Pool was a subset of a population fixed at
    /// world creation and could not grow with elapsed time whatever it did — so <c>adr/0006</c> was
    /// satisfied for a reason that had nothing to do with Departure. The gate removed that reason,
    /// and this is what replaces it: ***<c>adr/0006</c> discharged for the Pool by a mechanism rather
    /// than by an absence.***
    /// </para>
    /// <para>
    /// 🔴 <b>The money leaves with them, and that answers the question
    /// <see cref="DestroyHousehold"/> filed against its first production caller.</b> That method's own
    /// remark says the balance is destroyed, that the omission is deliberate, and that *the first
    /// production caller of this method is what has to answer it*. This is that caller, and the
    /// answer is the only one with a recipient: a Household that walks out of the city carries what
    /// it holds, so <see cref="MoneySupplyTable.Issued"/> goes down by exactly what left. That is the
    /// mirror of <c>adr/0131</c>'s arriving balance — <see cref="Endow"/>'s counterpart — and it is
    /// why <see cref="Invariant.MoneyIsConserved"/> still holds as an exact equality rather than
    /// needing a flow term: <c>Issued</c> is declared *net of anything that has left it*, and both
    /// sides move in the same call.
    /// </para>
    /// <para>
    /// ⚠ <b>The alternative was leaving the money in the city, and it has no recipient.</b> There is
    /// no escheat, no estate and no treasury claim on a departing Household — every one of those is a
    /// mechanism nobody has designed (<c>adr/0070</c>), and inventing one here to keep a total tidy
    /// would be a Policy decided by an invariant. ***A guard at the site where a quantity disappears
    /// can only ever check that it disappeared tidily.***
    /// </para>
    /// <para>
    /// ⚠ <b>No Trip is made, and that is not an omission.</b> The move-in at task 6 is a Trip because
    /// both its endpoints exist — a gate to leave from and a dwelling to arrive at. An <em>unhoused</em>
    /// Household has no dwelling by definition, so there is no origin Address to travel from, and a
    /// Trip from the gate to the gate is not a journey. ***The housed Departure is the one with a
    /// journey in it***, and that channel is milestone 16's with the comparison (<c>adr/0128</c>).
    /// </para>
    /// </remarks>
    /// <param name="household">The Household leaving. It must be in the Pool.</param>
    public void Depart(Handle<Household> household)
    {
        int slot = Households.Rows.Resolve(household);

        if (Buildings.Rows.TryResolve(Households.Dwelling[slot], out _))
        {
            // A housed Household leaving is the other channel, it is a comparison rather than a
            // threshold (adr/0102), and it ships at 16. Reaching here with a dwelling means somebody
            // wired the wrong channel to this door.
            Invariants.Report(Invariant.OnlyAnUnhousedHouseholdGivesUp, slot);
            return;
        }

        int position = Households.PoolPosition(slot);

        if (position < 0 || position >= UnplacedPool.Count)
        {
            Invariants.Report(Invariant.OnlyAnUnhousedHouseholdGivesUp, slot);
            return;
        }

        // Before DestroyHousehold, because that frees the Bin -- and reading the level afterwards
        // would read a freed row. The supply is written here rather than inside DestroyHousehold on
        // purpose: destroying a Household is a table operation with several callers, and only THIS
        // one means "somebody left the city with their savings". A Household bulldozed by a fixture
        // has not emigrated.
        if (Bins.Rows.TryResolve(Households.Balance[slot], out int balance))
        {
            MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(Bins.LevelAt(balance));
        }

        // The Pool membership goes first, because DestroyHousehold frees the Household row and the
        // membership holds a handle to it -- a Pool left holding a freed row is what
        // Invariant.ThePoolIsDenseAndAgreesWithTheHouseholds exists to catch, and it would catch it
        // at the end of the run rather than here.
        UnplacedPool.Leave(Households, position);

        DestroyHousehold(household);
    }

    /// <summary>
    /// Puts a Business that has lost its premises into the unpremised pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Unplace(Handle{Household})"/>'s opposite number</b>, and deliberately the same
    /// shape: it severs the premises handle, puts the actor in a pool with a clock on it, and touches
    /// <b>nothing it owns</b>. ***A pooled tenant keeps what it owns and that needs no code***
    /// (<c>adr/0142</c>) — the balance handle, the Bin list and the row itself are all untouched here.
    /// </para>
    /// <para>
    /// ⚠ <b>The caller takes the Business off <c>BuildingBusinesses</c>, not this method.</b>
    /// <see cref="DestroyBuilding"/> is draining that list as it goes and re-entering it here would
    /// be a mutation of the list being walked — <c>plans/0040</c> <b>F32</b>'s shape, avoided by not
    /// creating it.
    /// </para>
    /// <para>
    /// ⚠ <b>Its stock is NOT carried into the pool</b>
    /// (<see href="../../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md">adr/0144</see>).
    /// A Business owns exactly one Bin today — its balance — so there is nothing here to free and the
    /// loop that would do it is not written: a Business's stock Bins are <b>unbuilt</b>, and writing
    /// the sweep for them now would be a mechanism with no rows to walk. ***The rule is decided and
    /// the code for it is milestone 27's***, which is the honest split rather than an omission.
    /// </para>
    /// </remarks>
    public void Unpremise(Handle<Business> business, Ticks now)
    {
        int slot = Businesses.Rows.Resolve(business);

        if (Businesses.IsUnpremised(slot))
        {
            // Already pooled. Reaching here twice would put one Business in the pool at two
            // positions, and the second Leave would swap a live member into a freed slot.
            Invariants.Report(Invariant.ABusinessIsPremisedOrItIsInThePool, slot);
            return;
        }

        // ⚠ The Building's list FIRST, and this line was missing until milestone 27's placement pass
        // placement pass (adr/0147). Without it an unpremised Business stays threaded into the
        // premises it just left -- a ghost tenant. It was invisible while nothing called this from
        // src/ and nothing read the list for a decision, and it is a REAL defect either way:
        // BuildingBusinesses is derived, so a REBUILT world walks Businesses.Building and omits the
        // ghost while a MAINTAINED one keeps it. That is a save/reload divergence in a derived
        // structure, which is exactly the failure Employ's own remark describes from the other end --
        // "the disagreement is invisible, because the list is derived and therefore folds into no
        // hash." Found by EvictOverflow, which reads the list's LENGTH and would have looped for ever
        // draining a count that never fell.
        //
        // Remove rather than an assert, because ONE caller legitimately arrives with the row already
        // gone: Destroy drains BuildingBusinesses with PopFront and then calls this per tenant, so by
        // the time control reaches here the row is off the list. IndexList.Remove walks, fails to
        // find it and returns false, which is a no-op -- and the list is at most `occupants` long, so
        // the wasted walk is bounded by a Ruleset constant.
        if (Buildings.Rows.TryResolve(Businesses.Building[slot], out int buildingSlot))
        {
            BuildingBusinesses.Remove(buildingSlot, slot);
        }

        // Its Rules and its Bins go with the premises, and its BALANCE does not (adr/0166,
        // adr/0144). ⚠ BEFORE the handle is severed, because UnfitBusiness reads it to find the
        // Building whose Rule list holds this trader's Instances -- and a Rule Instance left behind
        // is the StaleHandleException milestone 27 task 9 died of, arriving from the other side.
        UnfitBusiness(business);

        Businesses.Building[slot] = default;

        // ⚠ AND OFF THE COMMUTE ROSTER, every worker of this employer. Both departure buckets are
        // computed from the Workplace's premises (adr/0101), so a Business that loses its premises
        // strands its staff in buckets nothing will ever empty: CommuteRoster's phase lookup returns
        // false for an unpremised employer, so a REBUILT world drops them and a maintained one would
        // not. That is (derived AND rebuilt) broken, and it is the same argument DestroyBuilding
        // carried when the fact lived on the Building.
        //
        // ⚠ The jobs SURVIVE. This is not a dismissal -- the staff keep their employer and lose only
        // the journey, which is what an employer between premises means.
        foreach (int worker in Workers.Walk(slot))
        {
            Commutes.Remove(Citizens, worker);
        }

        // No gate: a Business that LOST its premises is inside the city however it got here, and the
        // gate column records how it ARRIVED rather than where it is. adr/0145 makes the column
        // meaningful for the arrival channel; an orphan is neither channel and reads default.
        UnpremisedPool.Join(Businesses, business, default, now);
    }

    /// <summary>
    /// Gives an unpremised Business premises, linking it into that Building's business list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Unpremise"/>'s inverse, and <see cref="Place(Handle{Household}, Handle{Building})"/>'s
    /// mirror</b> (<c>adr/0147</c>). Milestone 25 shipped the exit before anything could take the
    /// entrance: <c>Unpremise</c> has existed since task 5 and ***this is the method it was the
    /// inverse of before that method was written.***
    /// </para>
    /// <para>
    /// <b>It does not ask <see cref="HasRoom"/>, and <see cref="Employ"/> is the precedent.</b> A
    /// ceiling guard belongs at the door a <em>sampling</em> caller comes through, where refusing is
    /// an ordinary outcome it already handles; a bare mutator that refused would leave its caller
    /// believing a write happened. ⚠ <b>The pooled check is different and IS here</b> — premising
    /// something already premised would leave it in the pool at a position a later <c>Leave</c> would
    /// swap a live member into, which is <c>Unpremise</c>'s own stated reason for its guard.
    /// </para>
    /// <para>
    /// <b>The Building's list is <see cref="BuildingBusinesses"/> and it is ordered</b>, which is what
    /// makes a rebuilt world byte-identical to a maintained one — the property every intrusive list
    /// in this build earns the same way.
    /// </para>
    /// </remarks>
    public void Premise(Handle<Business> business, Handle<Building> premises)
    {
        int slot = Businesses.Rows.Resolve(business);
        int buildingSlot = Buildings.Rows.Resolve(premises);

        if (!Businesses.IsUnpremised(slot))
        {
            Invariants.Report(Invariant.ABusinessIsPremisedOrItIsInThePool, slot);
            return;
        }

        UnpremisedPool.Leave(Businesses, Businesses.PoolPosition(slot));

        Businesses.Building[slot] = premises;
        BuildingBusinesses.InsertOrdered(buildingSlot, slot);

        // Its Bins and its Rules, which live exactly as long as this tenancy (adr/0166). After the
        // handle is written, because FitBusiness reads it to find the kind that declares the
        // ceilings -- the same ordering Place makes for a Household and for the same reason.
        FitBusiness(business);

        // ⚠ AND BACK ONTO THE ROSTER, which is Unpremise's mirror and is the only place in the build
        // where a Workplace GAINS a location. Every prior transition ran one way -- a demolition took
        // a workplace away and nothing ever gave one back -- so this direction has no precedent to
        // copy and would have failed silently: an unrostered worker makes no Trip, and no invariant
        // counts Trips that should have happened.
        //
        // After the handle is written, because the phase lookup reads it.
        foreach (int worker in Workers.Walk(slot))
        {
            Commutes.Add(Citizens, Buildings, Businesses, Rules, Key, worker);
        }
    }

    /// <summary>
    /// Sends an unpremised Business out of the city, taking its money with it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Depart(Handle{Household})"/>'s mirror, and the mechanism is that method's
    /// unchanged</b> (<c>adr/0142</c>): the balance is subtracted from <c>MoneySupply.Issued</c>
    /// before the row is freed, so <c>Invariant.MoneyIsConserved</c> holds across the departure.
    /// ***The money is neither destroyed nor confiscated; it is exported*** — through the same door
    /// <see cref="Endow"/> brings an arriving Household's balance in by.
    /// </para>
    /// <para>
    /// ⚠ <b>The supply write is here and not in <see cref="DestroyBusiness"/></b>, which is the split
    /// <see cref="Depart(Handle{Household})"/> makes and states its reason for: destroying a row is a
    /// table operation with several callers and ***only THIS one means somebody left the city.*** A
    /// fixture bulldozing a Business has not moved money out of any economy.
    /// </para>
    /// <para>
    /// ⚠ <b>It refuses a PREMISED Business</b>, which is
    /// <c>Invariant.OnlyAnUnhousedHouseholdGivesUp</c>'s rule with a different subject. A premised
    /// Business choosing to leave is the housed-departure channel — <b>unbuilt</b>, a comparison
    /// rather than a threshold (<c>adr/0102</c>) — so reaching here with premises means somebody
    /// wired the wrong channel to this door.
    /// </para>
    /// </remarks>
    public void Depart(Handle<Business> business)
    {
        int slot = Businesses.Rows.Resolve(business);

        if (Buildings.Rows.TryResolve(Businesses.Building[slot], out _))
        {
            Invariants.Report(Invariant.ABusinessIsPremisedOrItIsInThePool, slot);
            return;
        }

        int position = Businesses.PoolPosition(slot);

        if (position < 0 || position >= UnpremisedPool.Count)
        {
            Invariants.Report(Invariant.ABusinessIsPremisedOrItIsInThePool, slot);
            return;
        }

        // Before DestroyBusiness, because that frees the Bin -- reading the level afterwards would
        // read a freed row. Depart(Household)'s line and its reason.
        if (Bins.Rows.TryResolve(Businesses.Balance[slot], out int balance))
        {
            MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(Bins.LevelAt(balance));
        }

        // The membership goes first, because DestroyBusiness frees the row the membership holds a
        // handle to.
        UnpremisedPool.Leave(Businesses, position);

        DestroyBusiness(business);
    }

    /// <summary>
    /// Frees a Business and every Bin it owned.
    /// </summary>
    /// <remarks>
    /// <b>The table operation, with no economics in it</b> — see <see cref="Depart(Handle{Business})"/>
    /// for why the money supply is written by the caller rather than here.
    /// ⚠ <b>Every Bin it owned and not only its balance</b>, which is <c>adr/0143</c>'s saved owner
    /// list being the only way to reach a tenant's Bins at all, and <c>DestroyHousehold</c>'s own
    /// walk one actor across. The link is taken <b>before</b> the row is freed, because reading
    /// <c>OwnerNext</c> out of a freed row reads a zeroed slot and truncates the walk at the first
    /// entry.
    /// </remarks>
    public void DestroyBusiness(Handle<Business> business)
    {
        int slot = Businesses.Rows.Resolve(business);

        if (Businesses.IsUnpremised(slot))
        {
            UnpremisedPool.Leave(Businesses, Businesses.PoolPosition(slot));
        }

        if (Buildings.Rows.TryResolve(Businesses.Building[slot], out int buildingSlot))
        {
            BuildingBusinesses.Remove(buildingSlot, slot);
        }

        // ⚠ THE STAFF COME OFF THE LIST AND THEIR HANDLES ARE LEFT TO GO STALE, which is the split
        // that matters. The list is (derived AND rebuilt) and threads Citizens.WorkerNext through the
        // row about to be freed, so a freed row recycled with a live WorkerHead would hand its
        // successor somebody else's staff -- that has to be drained, from the head rather than by a
        // walk, since a walk would read its next link out of state it had already cleared.
        //
        // 🔴 The Workplace handle is NOT cleared, and clearing it would be the mistake. It is
        // declared Reference.Severable precisely so that it may point at a freed row and answer `my
        // employer is gone` when asked -- writing `default` instead answers `I never had one`, which
        // is a different sentence, costs a write per worker, and leaves the severable mechanism with
        // no producer in the whole build. EmploymentEngine already reads it the right way round.
        //
        // The commute DOES come off the roster: both departure buckets are computed from the
        // employer's premises (adr/0101), so a stranded worker sits in a bucket nothing will empty.
        int worker = Workers.PopFront(slot);

        while (worker != Rows.NoSlot)
        {
            Commutes.Remove(Citizens, worker);
            worker = Workers.PopFront(slot);
        }

        Handle<Bin> owned = Businesses.BinHead[slot];

        while (!owned.IsNone)
        {
            int binSlot = Bins.Rows.Resolve(owned);
            Handle<Bin> next = Bins.OwnerNext[binSlot];

            WakeAll(binSlot, Tick);
            Bins.Rows.Free(owned);

            owned = next;
        }

        Businesses.Rows.Free(business);
    }

    /// <summary>
    /// Ends a Business with the premises it came with, when those premises come down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0148</c>'s SINK, and it is the inverse of its source rather than a timeout.</b>
    /// Construction instantiates the trade a kind declares; demolition destroys it. ***Without this
    /// pairing the shop count is unbounded*** — measured on <c>rulesets/minimal.toml</c>, which
    /// condemns every dwelling it raises: 121 Businesses became <b>1,095</b> over 32,768 Ticks, with
    /// the unpremised pool carrying 907 of them. That is <c>adr/0006</c> exactly, and
    /// <c>gives_up_after_days</c> does not close it — a bound drains a stock at a rate, and this is a
    /// source with no matching sink.
    /// </para>
    /// <para>
    /// ⚠ <b>Only ONE Business dies with a Building, and it is chosen by trade rather than by a
    /// column.</b> <see cref="Fit"/> creates at most one of the kind's declared trade, so demolition
    /// destroys at most one of it and every other tenant is unpremised into the pool as before
    /// (<c>adr/0144</c>). The flag <c>adr/0148</c> refused stays refused: a stored *"came with the
    /// premises"* bit would distinguish two Businesses that are identical in every column, and the
    /// only case it decides differently is one where both outcomes are the same.
    /// </para>
    /// <para>
    /// <b>The money leaves the world with it</b>, exactly as <see cref="Depart(Handle{Business})"/>
    /// makes it leave with an emigrant. A kind-declared shop holds nothing today, so the write is
    /// zero in every shipped world — it is here because <c>Invariant.MoneyIsConserved</c> would
    /// report the day one of them earned something, and it would report it far from here.
    /// </para>
    /// </remarks>
    private void Raze(Handle<Business> business)
    {
        int slot = Businesses.Rows.Resolve(business);

        // Before DestroyBusiness, which frees the Bin. Depart's line and its reason.
        if (Bins.Rows.TryResolve(Businesses.Balance[slot], out int balance))
        {
            MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(Bins.LevelAt(balance));
        }

        DestroyBusiness(business);
    }

    /// <summary>Retires a Citizen, unlinking it from its Household first.</summary>
    public void DestroyCitizen(Handle<Citizen> citizen)
    {
        int slot = Citizens.Rows.Resolve(citizen);

        // Before the row is freed, because both phases are derived from state the row carries.
        Commutes.Remove(Citizens, slot);

        // And the parking space, for the reason the roster goes first and not for a reason of its
        // own: CarParkTable.Occupied and CitizenTable.ParkedIn are adr/0084's two halves of one sum,
        // and a freed row takes its half of the sum with it. Until milestone 11 task 9 this method
        // had no production caller at all, so a space held by a retiring Citizen was occupied for
        // the rest of the run and nothing could take it -- plans/0035 F30, found by the acceptance
        // run reading 234 occupied spaces against 233 holders at Tick 65,664.
        //
        // ⚠ A demolished Car Park is handled INSIDE ReleaseParking and is not a leak: both sides
        // lose the row together, so the holding is dropped without a decrement.
        ReleaseParking(slot);

        if (Households.Rows.TryResolve(Citizens.HouseholdOf[slot], out int householdSlot))
        {
            Members.Remove(householdSlot, slot);
        }

        // And off the employer's list, for the reason the member list is unlinked: a freed row still
        // threaded into a live list is what Invariant.NoFreedRowIsStillLinked exists to catch, and
        // the next allocation of this slot would be inserted into a list it is already in.
        Unlist(slot);

        Citizens.Rows.Free(citizen);
    }

    /// <summary>
    /// Retires a Household and every Citizen in it, unlinking it from its dwelling.
    /// </summary>
    /// <remarks>
    /// The members go with it rather than being left behind, because a Citizen whose Household handle
    /// is stale is a row nothing can reach and nothing will free — which is adr/0006's rule about
    /// collections that only grow, arriving through the back door.
    /// </remarks>
    public void DestroyHousehold(Handle<Household> household)
    {
        int slot = Households.Rows.Resolve(household);

        // ⚠ THROUGH DestroyCitizen RATHER THAN BY HAND, and this loop retired its members by hand
        // until milestone 11 task 9 -- plans/0035 F29. It unlinked each member from its employer and
        // freed the row, and never took it off the COMMUTE ROSTER, which DestroyCitizen has always
        // done one line earlier than it does the other two. So every Household destroyed with an
        // employed member left two dangling bucket entries, and the next allocation of that Citizen
        // slot was inserted into a list it was already in.
        //
        // PeekFront rather than PopFront because DestroyCitizen unlinks the member itself: the head
        // is re-read each turn, and the list shortens from the front either way.
        //
        // ***Retiring a Citizen now has ONE implementation***, which is the repair rather than the
        // missing call being it. The two paths agreed on the day they were written and the roster
        // arrived at only one of them -- adr/0069's own finding, quoted in World.Employ five hundred
        // lines below: a mechanism living inside another mechanism's caller is a mechanism nobody
        // built.
        int member = Members.PeekFront(slot);
        while (member != Rows.NoSlot)
        {
            DestroyCitizen(Citizens.Rows.At(member));
            member = Members.PeekFront(slot);
        }

        // adr/0141: the tenancy ends before the Household does, through the one implementation that
        // ends a tenancy. It frees this Household's own Rule Instances -- which are in the PREMISES'
        // Rule list and would otherwise be left holding a handle to a freed row -- and its stock
        // Bins, leaving the balance for the walk below to destroy along with the row.
        UnfitOccupant(household);

        if (Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
        {
            Occupants.Remove(buildingSlot, slot);
        }

        // The balance Bin goes with the Household, because a Bin nothing points at is a row nothing
        // will ever free -- adr/0006 through the same back door the members came through four lines
        // up. It is NOT a Severable handle left dangling on purpose: a Business keeps its row through
        // a demolition because the Business outlives its premises, and a Household being destroyed
        // outlives nothing.
        //
        // ⚠ THE MONEY IN IT IS DESTROYED, and that is STILL deliberately not repaired here -- but
        // the question it was waiting on has been answered. This method's remark used to say that
        // what becomes of a departing Household's balance was undesigned and that the first
        // production caller would have to answer it. World.Depart is that caller (milestone 11 task
        // 7) and its answer is that the money LEAVES WITH THEM: it decrements
        // MoneySupplyTable.Issued before calling this, so conservation holds across a Departure.
        //
        // What has not changed is that the write does not belong HERE. Destroying a Household is a
        // table operation with several callers and only one of them means "somebody emigrated" -- a
        // fixture bulldozing a row has not moved any money out of any economy. Folding the supply
        // write into this method would make every future caller silently claim an emigration.
        // Invariant.MoneyIsConserved still reports a caller that forgets, which is what it is for.
        // adr/0143: EVERY Bin the Household owned, and not only its balance. As of adr/0141 a Bin
        // belongs to the Occupant whose leaving would empty it, so a tenant's stock leaves with it for
        // the same reason its money does -- and walking the saved list is the only way to reach those
        // Bins, because a tenant-owned Bin names no owner. ⚠ The list is read forward and each link is
        // taken BEFORE the row is freed; reading OwnerNext out of a freed row would be reading a
        // zeroed slot and would silently truncate the walk at the first entry.
        //
        // The heads are not cleared here because Rows.Free zeroes the row, which is the same property
        // IndexList's slot-plus-one encoding exists to rely on.
        Handle<Bin> owned = Households.BinHead[slot];

        while (!owned.IsNone)
        {
            int binSlot = Bins.Rows.Resolve(owned);
            Handle<Bin> next = Bins.OwnerNext[binSlot];

            WakeAll(binSlot, Tick);
            Bins.Rows.Free(owned);

            owned = next;
        }

        Households.Rows.Free(household);
    }

    /// <summary>
    /// Finds the Districts the Building-density field currently supports and replaces the old ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Called once, at world creation, and by nothing on a Tick.</b> <c>adr/0134</c> settles what a
    /// District is; milestone 12 task 4 settles when it is re-asked, and until it does there is no
    /// cadence, no hysteresis and no per-evaluation Cell bound — which is why this is a method
    /// somebody calls rather than a phase that runs.
    /// </para>
    /// <para>
    /// ⚠ <b>It is NOT part of <see cref="RebuildDerived"/> and must never be folded into it.</b> A
    /// load restores the Districts the world <em>had</em>; this computes the ones its field would
    /// support <em>now</em>. Task 4 makes those two answers differ on purpose, and a rebuild that
    /// quietly re-evaluated would be a save/reload that moved every boundary.
    /// </para>
    /// </remarks>
    public void EvaluateDistricts()
    {
        // A boundary that moves changes which market a shop sells in, and nothing about the shop
        // changed -- which is the one invalidation that is not a lifecycle event.
        Markets.Invalidate();

        Space.DistrictWatershed.Evaluate(this);

        // After, and never before. A District opened by the evaluation has no Pool until this runs,
        // and a District the evaluation destroyed has already handed its Pool to its heir -- so
        // fitting first would open Bins for rows that are about to die and then leave the heir short.
        // FitTreasury's own rule, meeting a table that changes shape underneath it.
        FitDistrictPools();
    }

    /// <summary>
    /// Rebuilds every <see cref="Disposition.Derived"/> structure from saved state.
    /// </summary>
    /// <remarks>
    /// <b>This is what a load will call, and what proves the declaration was honest.</b> Every list
    /// rebuilt here is one the save does not write and the hash does not fold; the claim that made
    /// that legal is that each is a pure function of saved state. Calling this on a running world and
    /// finding the lists unchanged is the test of that claim — and it is a test the hash cannot
    /// perform for us, precisely because these fields are outside it.
    /// </remarks>
    public void RebuildDerived()
    {
        Layers.RebuildDerived();
        Roads.RebuildDerived();

        Buildings.OccupantHead.Span.Clear();
        Buildings.OccupantTail.Span.Clear();
        Businesses.WorkerHead.Span.Clear();
        Businesses.WorkerTail.Span.Clear();
        Buildings.BusinessHead.Span.Clear();
        Buildings.BusinessTail.Span.Clear();
        Businesses.BuildingNext.Span.Clear();
        Citizens.WorkerNext.Span.Clear();
        Citizens.CommuteNext.Span.Clear();
        Buildings.BinHead.Span.Clear();
        Buildings.BinTail.Span.Clear();
        Treasury.BinHead.Span.Clear();
        Treasury.BinTail.Span.Clear();
        Buildings.CarPark.Span.Clear();
        CarParks.Capacity.Span.Clear();
        Buildings.RuleHead.Span.Clear();
        Buildings.RuleTail.Span.Clear();
        Households.DwellingNext.Span.Clear();
        Households.PoolSlot.Span.Clear();
        Businesses.PoolSlot.Span.Clear();
        Households.MemberHead.Span.Clear();
        Households.MemberTail.Span.Clear();
        Citizens.MemberNext.Span.Clear();
        Bins.BinNext.Span.Clear();
        RuleInstances.RuleNext.Span.Clear();
        Lots.BuildingSlot.Span.Clear();
        Lots.FrontageSlot.Span.Clear();
        Lots.FrontageOffset.Span.Clear();
        Bins.Capacity.Span.Clear();

        // The Parking Shed's supply index, rebuilt wholesale from the Car Parks' saved Addresses.
        // After Roads.RebuildDerived because it resolves a Segment handle against the rebuilt graph,
        // and it clears car_park.segment_next itself rather than being cleared above -- the head and
        // tail arrays live outside any table, so the two halves have to be cleared together or a
        // stale head would point into an emptied column.
        CarParksOnSegments.Rebuild(CarParks, Roads.Segments);

        // Frontage before the reverse indices below, because it reads only the Lot's saved position
        // and the Street lattice — which Roads.RebuildDerived has just rebuilt — and nothing else
        // here depends on it. A Lot whose Street is gone comes out of this with no Address and keeps
        // its position, which is adr/0079.
        Frontage.Rebuild(Lots, Roads.Streets);

        // The Lot's reverse index. Not ordered like the four lists below it — a Lot holds at most one
        // Building, so there is nothing to insert in order, and a second Building naming the same Lot
        // is a violation the whole-world tier reports rather than a list this would silently lengthen.
        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (Buildings.Rows.IsLive(slot)
                && Lots.Rows.TryResolve(Buildings.Lot[slot], out int lotSlot))
            {
                Lots.Occupy(lotSlot, slot);
            }
        }

        // The Car Park's reverse index. The Lot's shape rather than the Bins' -- a Building has at
        // most one, so there is nothing to insert in order, and a second Car Park naming the same
        // Building is a violation the whole-world tier reports rather than a list this would silently
        // lengthen. The TryResolve does real work: CarParkTable.Owner outlives nothing, but a Car
        // Park whose Building has gone is a row DestroyBuilding should already have freed, so one
        // surviving here is a leak this walk declines to re-attach.
        for (int slot = 0; slot < CarParks.Rows.SlotCount; slot++)
        {
            if (CarParks.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(CarParks.Owner[slot], out int ownerSlot))
            {
                Buildings.AttachCarPark(ownerSlot, slot);
            }
        }

        // The Pool's reverse index, and the one place where a derived structure is rebuilt from a
        // saved *table* rather than from a saved column. The Pool is the saved side because a member
        // is drawn by position, and a position that changed across a reload would rehouse a different
        // Household from the same save.
        for (int slot = 0; slot < UnplacedPool.Rows.SlotCount; slot++)
        {
            if (UnplacedPool.Rows.IsLive(slot)
                && Households.Rows.TryResolve(UnplacedPool.Household[slot], out int householdSlot))
            {
                Households.EnterPool(householdSlot, slot);
            }
        }

        // The unpremised pool's reverse index, on the Unplaced Pool's own reasoning eight lines up.
        for (int slot = 0; slot < UnpremisedPool.Rows.SlotCount; slot++)
        {
            if (UnpremisedPool.Rows.IsLive(slot)
                && Businesses.Rows.TryResolve(UnpremisedPool.Business[slot], out int businessSlot))
            {
                Businesses.EnterPool(businessSlot, slot);
            }
        }

        IndexList occupants = Occupants;
        for (int slot = 0; slot < Households.Rows.SlotCount; slot++)
        {
            if (Households.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
            {
                occupants.InsertOrdered(buildingSlot, slot);
            }
        }

        IndexList members = Members;
        for (int slot = 0; slot < Citizens.Rows.SlotCount; slot++)
        {
            if (Citizens.Rows.IsLive(slot)
                && Households.Rows.TryResolve(Citizens.HouseholdOf[slot], out int householdSlot))
            {
                members.InsertOrdered(householdSlot, slot);
            }
        }

        // The worker list, and the TryResolve is doing more work here than it does above. A
        // Workplace handle is Reference.Severable, so a demolished Building leaves live Citizens
        // pointing at a freed row -- and this walk drops them, which is the same answer the write
        // path gives at DestroyBuilding. A Citizen whose workplace no longer resolves is in no
        // Building's worker list under either route, which is what makes the two agree.
        IndexList workers = Workers;
        for (int slot = 0; slot < Citizens.Rows.SlotCount; slot++)
        {
            if (Citizens.Rows.IsLive(slot)
                && Businesses.Rows.TryResolve(Citizens.Workplace[slot], out int workplaceSlot))
            {
                workers.InsertOrdered(workplaceSlot, slot);
            }
        }

        // The Business list, and the TryResolve carries the same weight it does above for the same
        // reason: BusinessTable.Building is Reference.Severable, so a demolished Building leaves a
        // live Business pointing at a freed row and this walk drops it -- the answer DestroyBuilding
        // gives on the write path. A Business whose premises no longer resolve is in no Building's
        // list under either route.
        IndexList businesses = BuildingBusinesses;
        for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
        {
            if (Businesses.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(Businesses.Building[slot], out int premisesSlot))
            {
                businesses.InsertOrdered(premisesSlot, slot);
            }
        }

        // The Bin and Rule Instance lists are derived; the wait lists threaded through the same rows
        // are not, and are untouched here. That asymmetry is the point of the two declarations: which
        // Bins a Building has follows from the Bins' own owner column, and the order a queue was
        // joined in follows from nothing.
        //
        // The owner KIND is what the walk branches on rather than whether the owner handle resolves,
        // and the difference is the whole of adr/0114's discriminator. A treasury Bin's handle is unset
        // by design, so a TryResolve alone would drop it exactly as it drops a Bin whose Building is
        // gone -- one is the state being modelled and the other is a broken row, and a walk that could
        // not tell them apart would silently unlink the treasury on every load.
        IndexList buildingBins = BuildingBins;
        IndexList treasuryBins = TreasuryBins;
        for (int slot = 0; slot < Bins.Rows.SlotCount; slot++)
        {
            if (!Bins.Rows.IsLive(slot))
            {
                continue;
            }

            switch (Bins.OwnerKind[slot])
            {
                case BinOwnerKind.Building:
                    if (Buildings.Rows.TryResolve(Bins.Owner[slot], out int buildingSlot))
                    {
                        buildingBins.InsertOrdered(buildingSlot, slot);
                    }

                    break;

                case BinOwnerKind.Treasury:
                    treasuryBins.InsertOrdered(TreasuryTable.Slot, slot);
                    break;

                case BinOwnerKind.District:
                    // Nothing to rebuild HERE, and for the actors' reason arriving at a different
                    // cardinality: a District's Pool Bins are named by DistrictPoolTable, which is
                    // saved, so the link came out of the file already made. ⚠ What separates it from
                    // the Building case is not care but RECOVERABILITY -- a derived list can only be
                    // derived when the element names its owner, and a Pool Bin's row does not.
                    break;

                case BinOwnerKind.WaterBody:
                    // Nothing to rebuild HERE, and it is the District case exactly: a Water Body's
                    // Bin is named by WaterBodyTable.Bin, which is saved, so the link came out of the
                    // file already made. milestone 24 task 6b.
                    break;

                case BinOwnerKind.Household:
                case BinOwnerKind.Business:
                    // Nothing to rebuild HERE, and the reason is the District case's, not the one
                    // this comment used to give. An Occupant's Bins hang off a SAVED list --
                    // HouseholdTable.BinHead / BusinessTable.BinHead, threaded through
                    // BinTable.OwnerNext -- so the link came out of the file already made. A
                    // Building's Bins hang off a DERIVED list and this walk is the only thing that
                    // can put them back. ⚠ What separates them is RECOVERABILITY and not care: a
                    // derived list can only be derived when the element names its owner, and a
                    // tenant-owned Bin names nobody (adr/0143).
                    //
                    // ⚠ THIS COMMENT SAID `an actor's balance is a single SAVED handle ... an actor
                    // holds one because money is one Resource` UNTIL adr/0141. An Occupant now holds
                    // a list, because a Bin belongs to the Occupant whose leaving would empty it and
                    // flour leaves with the baker. The balance is one entry in that list and is
                    // re-derived below rather than saved.
                    //
                    // The case is spelled out rather than folded into the default, because falling
                    // through to a throw that says `names no owner kind` would be a true statement
                    // about the wrong Bin.
                    break;

                case BinOwnerKind.None:
                default:
                    throw new InvalidOperationException(
                        $"bin {slot} is live and names no owner kind. Every Bin is created through "
                        + "BinTable.Create, which writes one; a zero here is a row that was allocated "
                        + "and never initialised, or a save whose owner_kind column did not load.");
            }
        }

        // The actors' balances, which are DERIVED as of adr/0143 and were saved before it. An Occupant
        // owns a list of Bins now, and a second saved handle to one of its entries would be two saved
        // facts that can disagree -- so the handle is re-found here, exactly as a Building's Bin list
        // is re-threaded above. ⚠ It is a walk of each actor's OWN list rather than a scan of the Bin
        // table, and that list is short by construction: one Bin per Resource the owner keeps.
        if (TryMoneyResource(out ResourceId balanceResource))
        {
            for (int slot = 0; slot < Households.Rows.SlotCount; slot++)
            {
                if (Households.Rows.IsLive(slot))
                {
                    Households.Balance[slot] = FindOwnerBin(Households.BinHead, slot, balanceResource);
                }
            }

            for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
            {
                if (Businesses.Rows.IsLive(slot))
                {
                    Businesses.Balance[slot] = FindOwnerBin(Businesses.BinHead, slot, balanceResource);
                }
            }
        }

        RebuildCapacities();

        // After the Lot reverse index above, because it reads a Building's Lot handle -- and it
        // clears its own arrays and CellNext rather than being cleared with the block at the top,
        // because its head and tail are not columns and the block above is a list of columns.
        BuildingsInCells.Rebuild(Buildings, Lots);

        // The zoned draw space, from the Lots' own saved Zones. A counting sort in slot order, so a
        // load reproduces the runs exactly rather than plausibly -- which is what makes a candidate
        // draw over it stable across save and reload.
        LotsAdmitting.Rebuild(Lots);

        // The Cell-to-District index, from the membership rows' own saved coordinates. NOT a
        // re-evaluation: the watershed does not run here and must not, because a load restores the
        // Districts the world had rather than the ones its field would support today. That
        // distinction is the whole of why DistrictCellTable is Saved and this is not.
        DistrictsInCells.Rebuild(DistrictCells);

        // Not rebuilt here, invalidated. It reads Businesses, Buildings, Lots and the Cell index --
        // the last of which is the line above -- and the Rule Instance threading below has not run
        // yet. A flag costs one O(n) pass at the first query and cannot be got wrong by ordering.
        Markets.Invalidate();

        IndexList buildingRules = BuildingRules;
        for (int slot = 0; slot < RuleInstances.Rows.SlotCount; slot++)
        {
            if (RuleInstances.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(RuleInstances.Building[slot], out int buildingSlot))
            {
                buildingRules.InsertOrdered(buildingSlot, slot);
            }
        }

        // The commute roster, which clears its own head and tail for BuildingsInCells' reason: they
        // are arrays over the Day rather than columns over a table, so the block at the top of this
        // method -- which is a list of columns -- cannot reach them.
        Commutes.Rebuild(Citizens, Buildings, Businesses, Rules, Key);
    }

    /// <summary>The Households living in each Building.</summary>
    /// <remarks>
    /// Bound freshly on each use rather than cached: the spans are over the live prefix of their
    /// columns, and that prefix moves when a row is allocated. A cached <see cref="IndexList"/> is a
    /// list that cannot see the row you just created.
    /// </remarks>
    public IndexList Occupants =>
        new(Buildings.OccupantHead, Buildings.OccupantTail, Households.DwellingNext);

    /// <summary>The Citizens in each Household.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList Members =>
        new(Households.MemberHead, Households.MemberTail, Citizens.MemberNext);

    /// <summary>The Citizens who work in each Building.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList Workers =>
        new(Businesses.WorkerHead, Businesses.WorkerTail, Citizens.WorkerNext);

    /// <summary>
    /// The Businesses occupying each Building — the second Occupant list (<c>adr/0113</c>).
    /// </summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList BuildingBusinesses =>
        new(Buildings.BusinessHead, Buildings.BusinessTail, Businesses.BuildingNext);

    /// <summary>The Bins on each Building.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList BuildingBins => new(Buildings.BinHead, Buildings.BinTail, Bins.BinNext);

    /// <summary>
    /// The treasury's Bins — the city's own balance sheet (<c>adr/0114</c>).
    /// </summary>
    /// <remarks>
    /// <b>It threads the same <see cref="BinTable.BinNext"/> as <see cref="BuildingBins"/>, and that
    /// is safe rather than a shortcut</b>: a Bin has exactly one owner, so it is in exactly one of
    /// these lists, and one link column is the representation of that fact rather than a saving.
    /// The head and tail live on <see cref="TreasuryTable"/>'s one row for the reason every other
    /// intrusive list's do — a list needs an owner row, and the treasury had none until it had a
    /// table.
    /// </remarks>
    /// <inheritdoc cref="Occupants"/>
    public IndexList TreasuryBins => new(Treasury.BinHead, Treasury.BinTail, Bins.BinNext);

    /// <summary>The Rule Instances each Building runs.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList BuildingRules =>
        new(Buildings.RuleHead, Buildings.RuleTail, RuleInstances.RuleNext);

    /// <summary>The Rule Instances asleep on each Bin because it was <em>short</em>.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList SupplyWaiters => new(Bins.SupplyHead, Bins.SupplyTail, RuleInstances.QueueNext);

    /// <summary>The Rule Instances asleep on each Bin because it was <em>full</em>.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList SpaceWaiters => new(Bins.SpaceHead, Bins.SpaceTail, RuleInstances.QueueNext);

    /// <summary>Where a pedestrian arrives at this Building — its front door on the Road Graph.</summary>
    /// <remarks>
    /// <para>
    /// <b>Resolved through the Lot, and stored nowhere.</b> <c>adr/0078</c> makes frontage
    /// <c>(derived AND rebuilt)</c> on the Segment's Epoch, so the Lot re-derives its
    /// <see cref="Address"/> whenever the roads change. A column on the Building would be a second
    /// copy of that fact, and the failure is the quiet one: laying a new Street can give a Lot a
    /// better front door without touching the old one, so the copy is simply wrong, with nothing
    /// invalidated and nothing to notice. That is <c>adr/0064</c>'s argument for a Bin's capacity and
    /// <c>adr/0068</c>'s for a Building's occupancy, reaching a third row.
    /// </para>
    /// <para>
    /// <b><see cref="Address.None"/> is a named absence, not a failure.</b> A Building whose Lot has
    /// no frontage has no front door — <c>adr/0079</c> — and a Trip to it ends <em>no route found</em>
    /// rather than taking a long walk. A Building outlives its frontage, so this is a state the city
    /// reaches in normal running and not only through a bug.
    /// </para>
    /// </remarks>
    public Address PedestrianAccessPoint(int buildingSlot) => AccessPointOf(buildingSlot);

    /// <summary>Where a Vehicle arrives at this Building.</summary>
    /// <remarks>
    /// <para>
    /// <b>Equal to <see cref="PedestrianAccessPoint"/> by construction, and that is built behaviour
    /// rather than an interim simplification.</b> <c>adr/0074</c> gives a Building two Access Points
    /// because side of street is a property of the Address; <see cref="LotSubdivider"/> derives
    /// <em>one</em> Address per Lot, from the Lot's own position and side. A second would need a
    /// second saved fact, and inventing one for a consumer that does not exist is the position
    /// <c>adr/0070</c> forbids. ⚠ <b>Parking was the milestone expected to separate them and it did
    /// not.</b> Milestone <b>7</b> — renumbered from 8, which is what this sentence used to say — gave
    /// a car somewhere else to be, but it did so by having a Trip's flanking Legs name a <b>Car Park's
    /// Address</b> directly rather than by giving a Building a second door. So the two are still equal,
    /// and the remaining candidate is <c>03 §6.6</c>'s freight. ***A mechanism can arrive and leave the
    /// distinction it was expected to force still unforced***, which is why this remark names what
    /// would separate them rather than when.
    /// </para>
    /// <para>
    /// <b>This is never a fallback from an exhausted Parking Shed</b>, and as of milestone 7 task 5
    /// that is enforced rather than merely written down: a full car park must not cost less than an
    /// empty one, so a Shed with no room widens its search — <see cref="TryChooseParking"/> — and a
    /// destination with no room anywhere refuses the Trip on the Commute Budget rather than resolving
    /// to the Building's own kerb at zero cost.
    /// </para>
    /// </remarks>
    public Address VehicleAccessPoint(int buildingSlot) => AccessPointOf(buildingSlot);

    /// <summary>How a Citizen travels: by car if their Household keeps one, else on foot.</summary>
    /// <remarks>
    /// <para>
    /// <b>One place decides a Citizen's mode, and that is the whole reason this is on
    /// <see cref="World"/> rather than in each caller.</b> <see cref="Movement.CommuteEngine"/> uses
    /// it to make the journey and <see cref="Rules.EmploymentEngine"/> uses it to judge whether a job
    /// is reachable. Two copies of the rule would let a Citizen take a job because they could walk to
    /// it and then drive there, or the reverse — one fact stored twice with one copy drifting, which
    /// is <c>plans/0012</c> <b>Cause 1</b> written in code instead of prose.
    /// </para>
    /// <para>
    /// <b>Judging and travelling in the same mode is <c>adr/0008</c>'s requirement, not a
    /// convenience.</b> Session F refused a per-mode weight on the Commute Budget precisely so that a
    /// walk and a drive are compared on one clock; a driver judging a job by walking time is applying
    /// that Budget in the wrong currency, and the resulting shortfall would read as a labour-market
    /// finding rather than as a unit error.
    /// </para>
    /// <para>
    /// <b>A Citizen with no resolvable Household walks.</b> That is a referential break rather than a
    /// state the city reaches, and the caller's next act is a route search — which is the operation
    /// that already reports an impossible journey honestly, so there is nothing better to do here
    /// than answer with the mode that owns no assumptions.
    /// </para>
    /// </remarks>
    public TravelMode ModeOf(int citizenSlot)
    {
        if (!Rules.Households.Runs
            || !Households.Rows.TryResolve(Citizens.HouseholdOf[citizenSlot], out int household))
        {
            return TravelMode.Foot;
        }

        return Rules.Households.OwnsCar(Key, Households.Rows.IdAt(household))
            ? TravelMode.Car
            : TravelMode.Foot;
    }

    /// <summary>Where a traveller in this mode arrives at a Building.</summary>
    /// <remarks>
    /// <b>The two are the same Address today and the caller must not know that.</b>
    /// <see cref="VehicleAccessPoint"/> explains why they are equal and what would separate them,
    /// which after milestone 7 is <c>03 §6.6</c>'s freight alone. A caller that reached for
    /// <see cref="PedestrianAccessPoint"/> for both would be correct now and silently wrong on the day
    /// they diverge, with no compile error and no failing test to say so.
    /// </remarks>
    public Address AccessPoint(int buildingSlot, TravelMode mode) =>
        mode == TravelMode.Car
            ? VehicleAccessPoint(buildingSlot)
            : PedestrianAccessPoint(buildingSlot);

    /// <summary>The Address of the Lot a Building stands on, or <see cref="Address.None"/>.</summary>
    /// <remarks>
    /// The handle resolve is not defensive: <see cref="LotTable"/> is earlier in declaration order
    /// than <see cref="BuildingTable"/> and a Lot outlives the Building on it, so a failure here is a
    /// referential-integrity break rather than a demolition. It still answers with the absence,
    /// because the caller's next act is a route search and <c>adr/0079</c> has already said what a
    /// Building with no front door does to one.
    /// </remarks>
    private Address AccessPointOf(int buildingSlot) =>
        Lots.Rows.TryResolve(Buildings.Lot[buildingSlot], out int lotSlot)
            ? Lots.AddressOf(lotSlot)
            : Address.None;

    /// <summary>
    /// Builds a Building and fits it out with its kind's Bins and Rule Instances.
    /// </summary>
    /// <param name="lot">The Lot it occupies.</param>
    /// <param name="kind">The Building kind, which is what the Ruleset declares Bins and Rules against.</param>
    /// <param name="now">The current Tick, which the arming is relative to.</param>
    /// <param name="key">The world key, for the stagger draw.</param>
    /// <remarks>
    /// <para>
    /// <b>This is the door, and it is the symmetric partner of <see cref="DestroyBuilding"/>.</b>
    /// <c>CONTEXT.md</c> → Bin: <em>"A Building is given exactly its kind's Bins when it is built."</em>
    /// That sentence had no implementation — every Bin in the project was created by a test writing
    /// the loop by hand, so the Ruleset declared a shape nothing built. Putting it here rather than in
    /// the populator is what makes it true of a Building grown in Phase 2 on the same terms as one the
    /// populator makes, which is the case that would otherwise have been discovered late.
    /// </para>
    /// <para>
    /// <b>What it fits out with is <see cref="Fit"/>, which a reload calls again.</b> A Building is
    /// constructed once and refitted every time the Ruleset moves under it, so the two had better be
    /// one piece of code.
    /// </para>
    /// </remarks>
    public Handle<Building> CreateBuilding(Handle<Lot> lot, byte kind, Ticks now, WorldKey key)
    {
        int lotSlot = Lots.Rows.Resolve(lot);

        // 02 §2.2's "a Lot is either vacant or holds exactly one Building", at the write site, which
        // is where 02 §10 puts the checks that are O(1). The whole-world half is
        // Invariant.LotHoldsExactlyOneBuilding.
        Invariants.Require(Lots.IsVacant(lotSlot), Invariant.LotIsNotAlreadyBuiltOn, lotSlot);

        // adr/0088's edge constraint, and it costs one comparison on kinds that are not gates.
        Invariants.Require(
            !IsOutsideConnection(kind) || EdgeOf(lotSlot) != MapEdge.None,
            Invariant.OutsideConnectionStandsOnOneEdge,
            lotSlot);

        Handle<Building> building = Buildings.Create(Lots, lot, kind);

        BuildingsInCells.Add(Buildings, Lots, Buildings.Rows.Resolve(building));

        // CONTEXT.md -> Building: "a Building has a footprint (the set of Tiles it covers)" and
        // "interacts with Map Layers through that footprint". Sealing is such a Layer, and this is
        // the single door every Building comes through -- the populator's and the Zone Rule's alike
        // -- so it is where the footprint meets the ground rather than at either caller.
        //
        // The clamp is a backstop and not the refusal. RulesetLoader refuses footprint_tiles below
        // one at the parse site (adr/0048); reaching here with zero means a KindDefinition was built
        // in a test rather than loaded, and a Building covering no ground is not a thing this method
        // should invent a second meaning for.
        // Declares() rather than Kind(), because a fixture may raise a Building of a kind its Ruleset
        // never declared -- BuildingResidencyTests builds a world with no [[building]] at all and
        // asks the Cell index to hold one. That is a legal thing for a test to do and it is not this
        // method's business to start refusing it, so an undeclared kind takes CONTEXT.md's one Tile.
        int footprintTiles = Rules.Declares(kind) ? Rules.Kind(kind).FootprintTiles : 1;

        // Clamped, because a gate Lot stands ON a map edge (adr/0088) and the edge is a
        // fencepost: Tile WorldTiles is one past the last Tile and has no Cell of its own.
        Layers.Seal(
            CellGrid.ToCellsClamped(Lots.East[lotSlot]),
            CellGrid.ToCellsClamped(Lots.North[lotSlot]),
            footprintTiles < 1 ? 1 : footprintTiles);

        Fit(building, kind, now, key);

        return building;
    }

    /// <summary>
    /// Gives a Building the Bins and Rule Instances its kind declares and does not already have.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Factored out of <see cref="CreateBuilding"/> because a reload calls it again</b>, and the
    /// point of factoring rather than copying is that a refit which drifted from a construction would
    /// make a Building raised before a reload a different Building from one raised after it — with
    /// nothing to compare, since both are legal.
    /// </para>
    /// <para>
    /// <b>A kind the Ruleset does not declare gets no Bins and no Rules, and that is a real state
    /// rather than a swallowed error.</b> <c>02 §4.3</c> names it: a reload marks Buildings whose kind
    /// no longer exists <b>derelict rather than deleted</b>. There is no derelict column and there
    /// deliberately never will be (<c>adr/0057</c>): dereliction is <c>Kind == 0</c>, a Building the
    /// Ruleset in force cannot describe, and a saved mark would be a cache of that two-compare
    /// predicate — a second spelling of one fact, hashed beside it, that nothing clears. It is
    /// <b>design-time state</b>: only a Ruleset edit under a running city produces it, so a played
    /// game never reaches it, and it must never be given abandonment's mechanisms. The commonest
    /// instance today is a world running on <see cref="Ruleset.Empty"/>.
    /// </para>
    /// <para>
    /// <b>Only the chain heads are armed.</b> <see cref="Ruleset.RulesOf"/> returns them, and a link
    /// is reached by walking a chain that failed rather than by coming due — arming a link would run
    /// it independently of the head it exists to rescue, and the reporting terminal would fire on its
    /// own rate for ever. That is <c>adr/0045</c>'s polling defect arriving through the Rule Instance
    /// table instead of through the walk.
    /// </para>
    /// <para>
    /// <b>The Bins come first.</b> A Rule armed before its Bins exist could come due against a
    /// Building that cannot hold what it transforms — not reachable today, since arming is at least
    /// one Tick out, but the ordering costs nothing and the alternative relies on that staying true.
    /// </para>
    /// </remarks>
    /// <returns>How many Rule Instances were armed.</returns>
    private int Fit(Handle<Building> building, byte kind, Ticks now, WorldKey key)
    {
        if (!Rules.Declares(kind))
        {
            return 0;
        }

        int buildingSlot = Buildings.Rows.Resolve(building);

        // Asked rather than assumed, because a refit meets a Building that already holds the Bins
        // that survived the migration; on the construction path the answer is always NoSlot and the
        // walk is over an empty list.
        foreach (BinDeclaration bin in Rules.BinsOf(kind))
        {
            // The premises' Bins only. A tenant's are opened by FitOccupant when the tenancy starts,
            // because they hold what leaves with the tenant (adr/0141) -- and a Building with room
            // for three families would otherwise open one larder for all of them, which is the shared
            // kitchen minimal.toml's own header disclaims.
            if (bin.Tenancy != BinTenancy.Premises)
            {
                continue;
            }

            if (FindBin(buildingSlot, bin.Resource) == Rows.NoSlot)
            {
                CreateBin(building, bin.Resource);
            }
        }

        // The Car Park, asked rather than assumed for the Bins' reason: a refit meets a Building that
        // already has one. A kind declaring `parking = 0` gets no row at all, which is not the same
        // as a row of capacity zero -- the shed walks rows, so an empty row would be a Car Park that
        // is permanently full rather than a Building that has none, and the two read differently in
        // every diagnosis adr/0009 exists to give.
        if (!Buildings.HasCarPark(buildingSlot)
            && TryDeclaredParking(kind, out int spaces)
            && spaces > 0)
        {
            CreateCarPark(building, spaces);
        }

        // adr/0148: the trade this kind comes with, instantiated already premised. It is DRAWN FROM
        // NO POOL -- neither the Unplaced Pool nor the unpremised one is touched -- which is why this
        // does not reach adr/0069's "construction houses nobody": that rule protects a demand signal
        // from being drained by construction, and nothing here drains one. What it makes is an
        // ORDINARY Business carrying no flag and no founder, and it takes one of the kind's occupant
        // slots exactly as a Household does (adr/0147).
        //
        // Asked rather than assumed, for the Bins' reason and the Car Park's: a refit meets a
        // Building that already holds the trade it came with, and a second one would double the
        // city's employment on every reload.
        int armed = 0;
        byte trade = Rules.Kind(kind).Business;

        if (trade != 0 && !HoldsOwnTrade(buildingSlot))
        {
            // The origin is written here rather than inside CreateBusiness, because this is the one
            // caller that has premises to claim. A founded Business is created unpremised and a
            // fixture's is created with no origin at all, and both of those are ordinary Businesses
            // that no demolition may raze.
            Handle<Business> came = CreateBusiness(building, trade);

            Businesses.Origin[Businesses.Rows.Resolve(came)] = building;

            // And it takes up its tenancy in the same breath, because it was created already
            // premised (adr/0166). Every OTHER Business reaches FitBusiness through Premise; this
            // one never goes through that door, so a shop instantiated with its Building would
            // otherwise stand there holding nothing and running nothing.
            armed += FitBusiness(came);
        }

        foreach (RuleId rule in Rules.RulesOf(kind))
        {
            if (Rules.Rule(rule).Tenancy != BinTenancy.Premises)
            {
                continue;
            }

            CreateRuleInstance(
                building, rule, now, ArmingStagger(Buildings.Rows.IdAt(buildingSlot), rule, now, key));
            armed++;
        }

        return armed;
    }

    /// <summary>Whether a Building already holds the Business it instantiated itself.</summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Fit"/>'s idempotence for <c>adr/0148</c>'s declared trade</b>, and it asks the
    /// same question the Bin walk and the Car Park check ask. It is <see cref="DestroyBuilding"/>'s
    /// predicate exactly, run in the other direction: ***the pairing that keeps the shop count bounded
    /// has to identify the same row at both ends, or it is not a pairing.***
    /// </para>
    /// <para>
    /// 🔴 <b>It asked about the TRADE until milestone 27 task 10 and that was the defect.</b> A
    /// Building may hold a second Business of the same trade — one a Household founded, since
    /// <c>[founding]</c> draws uniformly over every declared trade — and matching on kind made the two
    /// interchangeable at both ends: a refit declined to instantiate because a *founded* shop was
    /// standing there, and a demolition razed the founded one in the instantiated one's place. **The
    /// old comment here named the alternative and called it a flag <c>adr/0148</c> refuses.** It is
    /// not a flag; it is <see cref="BusinessTable.Origin"/>, a handle naming one Building, and a
    /// handle that stops meaning anything the moment the Business leaves is the opposite of a flag.
    /// </para>
    /// </remarks>
    private bool HoldsOwnTrade(int buildingSlot)
    {
        foreach (int business in BuildingBusinesses.Walk(buildingSlot))
        {
            if (Buildings.Rows.TryResolve(Businesses.Origin[business], out int origin)
                && origin == buildingSlot)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Opens a Household's own Bins and arms its own Rules, at the start of a tenancy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Fit"/>'s other half</b> (<c>adr/0141</c>). The premises declare every Bin on the
    /// Lot and the tenant holds the level in the ones that would empty if it left, so the *set* comes
    /// from the Building's kind here exactly as it does there — what differs is the owner the Bin
    /// hangs off and the subject the Rule Instance names.
    /// </para>
    /// <para>
    /// ⚠ <b>A tenant's Bins and Rules live exactly as long as the tenancy, and that is forced rather
    /// than chosen.</b> A ceiling is a function of <c>(building kind, Resource)</c> and an unhoused
    /// Household has no kind to read one from, so a Bin that outlived its tenancy would be a Bin
    /// <see cref="RebuildCapacities"/> cannot give a ceiling to. ⚠ <b>What that costs is the tenant's
    /// stock, destroyed on eviction</b> — filed as a finding, and it is <em>not</em> the money, which
    /// is unbounded, has no premises in its ceiling and stays with the Household under
    /// <c>adr/0054</c>. ⚠ <b>It is also NOT the answer for a Business</b>, which under
    /// <c>adr/0142</c> goes on existing unpremised holding what it had; that is open decision 1 of
    /// <c>plans/0040</c> and this method does not settle it.
    /// </para>
    /// <para>
    /// <b>Asked rather than assumed, exactly as <see cref="Fit"/> asks</b>, so that a Ruleset swap
    /// meets a Household that already holds the Bins that survived the migration.
    /// </para>
    /// </remarks>
    /// <returns>How many Rule Instances were armed.</returns>
    private int FitOccupant(Handle<Household> household)
    {
        int slot = Households.Rows.Resolve(household);
        Handle<Building> dwelling = Households.Dwelling[slot];

        if (!Buildings.Rows.TryResolve(dwelling, out int buildingSlot))
        {
            return 0;
        }

        byte kind = Buildings.Kind[buildingSlot];

        if (!Rules.Declares(kind))
        {
            return 0;
        }

        foreach (BinDeclaration bin in Rules.BinsOf(kind))
        {
            if (bin.Tenancy != BinTenancy.Occupant)
            {
                continue;
            }

            if (FindOwnerBin(Households.BinHead, slot, bin.Resource).IsNone)
            {
                CreateOccupantBin(household, buildingSlot, bin.Resource);
            }
        }

        int armed = 0;

        foreach (RuleId rule in Rules.RulesOf(kind))
        {
            if (Rules.Rule(rule).Tenancy != BinTenancy.Occupant)
            {
                continue;
            }

            // The TENANT's monotonic id into the stagger and not the Building's, which is the half of
            // adr/0141 that is a correctness bug rather than a tidy-up: three Households in one
            // dwelling all running `consume` would mix the same Building id with the same RuleId and
            // land on one Wheel bucket, together, for ever.
            CreateRuleInstance(
                dwelling,
                rule,
                Tick,
                ArmingStagger(Households.Rows.IdAt(slot), rule, Tick, Key),
                household);

            armed++;
        }

        return armed;
    }

    /// <summary>
    /// Closes a Household's own Bins and frees its own Rule Instances, at the end of a tenancy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The balance survives and everything else does not</b> (<c>adr/0054</c>, <c>adr/0024</c>):
    /// money is unbounded, so its ceiling names no premises and there is nothing about a tenancy for
    /// it to depend on. ⚠ <b>Destroying it would be money leaving the world through an eviction</b>,
    /// which is the hole <c>Invariant.MoneyIsConserved</c> exists to report and which it would report
    /// a very long way from here.
    /// </para>
    /// <para>
    /// <b>The Rules go before the Bins</b>, for the reason <see cref="DestroyBuilding"/> states in the
    /// same order: a Rule Instance asleep on a Bin has to come off that wait list before the Bin is
    /// freed, or the wake walks a freed row.
    /// </para>
    /// </remarks>
    private void UnfitOccupant(Handle<Household> household)
    {
        int slot = Households.Rows.Resolve(household);

        if (Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
        {
            // Rescanned from the front after every removal rather than walked once with the link
            // held. ⚠ RuleInstance.RuleNext IS ENCODED -- IndexList stores slot-plus-one so that a
            // zeroed column reads as `no list` rather than as slot 0 -- so reading it raw to hold a
            // successor across a Free would walk one row past every element. The list is a Building's
            // Rules, which is single digits, so the rescan is cheaper than a second decode API.
            IndexList rules = BuildingRules;
            bool removed = true;

            while (removed)
            {
                removed = false;

                foreach (int instance in rules.Walk(buildingSlot))
                {
                    if (RuleInstances.Household[instance] != household)
                    {
                        continue;
                    }

                    rules.Remove(buildingSlot, instance);
                    Unlink(instance);
                    RuleInstances.Rows.Free(RuleInstances.Rows.At(instance));
                    removed = true;
                    break;
                }
            }
        }

        Handle<Bin> at = Households.BinHead[slot];
        Handle<Bin> keptHead = default;
        Handle<Bin> keptTail = default;

        while (!at.IsNone)
        {
            int binSlot = Bins.Rows.Resolve(at);
            Handle<Bin> next = Bins.OwnerNext[binSlot];

            if (Rules.IsConserved(Bins.Resource[binSlot]))
            {
                Bins.OwnerNext[binSlot] = default;

                if (keptTail.IsNone)
                {
                    keptHead = at;
                }
                else
                {
                    Bins.OwnerNext[Bins.Rows.Resolve(keptTail)] = at;
                }

                keptTail = at;
            }
            else
            {
                WakeAll(binSlot, Tick);
                Bins.Rows.Free(at);
            }

            at = next;
        }

        Households.BinHead[slot] = keptHead;
        Households.BinTail[slot] = keptTail;
    }

    /// <summary>
    /// Opens a Business's own Bins and arms its own Rules, at the start of its tenancy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="FitOccupant"/>'s twin for the other Occupant</b> (<c>adr/0166</c>), and the twin
    /// is close enough to be suspicious, so here is what differs and why. The *set* of Bins comes from
    /// the premises' kind in both, because a ceiling is a function of
    /// <c>(building kind, Resource)</c> and neither tenant has a kind of its own. What differs is the
    /// owner the Bin hangs off, the subject the Rule Instance names, and ***where the premises handle
    /// is read from*** — a Household's is <c>Households.Dwelling</c> and a Business's is
    /// <c>Businesses.Building</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>A money declaration opens nothing here, and that is the one asymmetry with
    /// <see cref="Fit"/>.</b> A balance is <see cref="OpenBalance"/>'s, unbounded and with no premises
    /// in its ceiling, and it is opened when the Business is *created* rather than when it takes
    /// premises — because <c>adr/0144</c> keeps it through unpremising. So a
    /// <see cref="Rules.BinTenancy.Business"/> declaration naming a conserved Resource is a **tenancy
    /// claim and not an allocation**: it exists so <c>RulesetLoader.ApplyTenancies</c> can derive the
    /// owner of a Rule addressing money, which is <c>adr/0166</c>'s decline half and the case a
    /// two-valued tenancy silently gave to the landlord (<c>plans/0044</c> open decision 4).
    /// </para>
    /// <para>
    /// <b>Asked rather than assumed, exactly as <see cref="Fit"/> and <see cref="FitOccupant"/> ask</b>,
    /// so a Ruleset swap meets a Business that already holds the Bins that survived the migration.
    /// </para>
    /// </remarks>
    /// <returns>How many Rule Instances were armed.</returns>
    private int FitBusiness(Handle<Business> business)
    {
        int slot = Businesses.Rows.Resolve(business);
        Handle<Building> premises = Businesses.Building[slot];

        if (!Buildings.Rows.TryResolve(premises, out int buildingSlot))
        {
            return 0;
        }

        byte kind = Buildings.Kind[buildingSlot];

        if (!Rules.Declares(kind))
        {
            return 0;
        }

        foreach (BinDeclaration bin in Rules.BinsOf(kind))
        {
            if (bin.Tenancy != BinTenancy.Business || Rules.IsConserved(bin.Resource))
            {
                continue;
            }

            if (FindOwnerBin(Businesses.BinHead, slot, bin.Resource).IsNone)
            {
                CreateTraderBin(business, buildingSlot, bin.Resource);
            }
        }

        int armed = 0;

        foreach (RuleId rule in Rules.RulesOf(kind))
        {
            if (Rules.Rule(rule).Tenancy != BinTenancy.Business)
            {
                continue;
            }

            // The TRADER's monotonic id into the stagger and not the Building's, which is
            // FitOccupant's correctness bug with a different subject: two Businesses in one
            // Building -- the kind's own trade under adr/0148 and one a Household founded -- would
            // otherwise mix the same Building id with the same RuleId and land on one Wheel bucket,
            // together, for ever.
            CreateRuleInstance(
                premises,
                rule,
                Tick,
                ArmingStagger(Businesses.Rows.IdAt(slot), rule, Tick, Key),
                default,
                business);

            armed++;
        }

        return armed;
    }

    /// <summary>
    /// Closes a Business's own Bins and frees its own Rule Instances, at the end of its tenancy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="UnfitOccupant"/>'s twin, and the balance survives here for the same reason it
    /// survives there</b> (<c>adr/0144</c>, <c>adr/0024</c>): money is unbounded, its ceiling names no
    /// premises, and destroying it would be money leaving the world through a demolition — the hole
    /// <c>Invariant.MoneyIsConserved</c> exists to report and would report a very long way from here.
    /// ***An unpremised Business goes on existing holding only its money***, which is <c>adr/0142</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>What it costs is the shop's STOCK, destroyed when it loses its premises</b> — the same
    /// finding <see cref="FitOccupant"/> files for a tenant's larder, arriving at a seller whose
    /// inventory is the thing <c>adr/0139</c> put there. It is filed rather than fixed: a Bin that
    /// outlived its tenancy would be a Bin <see cref="RebuildCapacities"/> cannot give a ceiling to.
    /// </para>
    /// <para>
    /// <b>The Rules go before the Bins</b>, for <see cref="UnfitOccupant"/>'s reason: a Rule Instance
    /// asleep on a Bin has to come off that wait list before the Bin is freed, or the wake walks a
    /// freed row.
    /// </para>
    /// </remarks>
    private void UnfitBusiness(Handle<Business> business)
    {
        int slot = Businesses.Rows.Resolve(business);

        if (Buildings.Rows.TryResolve(Businesses.Building[slot], out int buildingSlot))
        {
            // Rescanned from the front after every removal, which is UnfitOccupant's note and its
            // reason: RuleInstance.RuleNext is slot-plus-one encoded, so holding a successor across
            // a Free would walk one row past every element.
            IndexList rules = BuildingRules;
            bool removed = true;

            while (removed)
            {
                removed = false;

                foreach (int instance in rules.Walk(buildingSlot))
                {
                    if (RuleInstances.Business[instance] != business)
                    {
                        continue;
                    }

                    rules.Remove(buildingSlot, instance);
                    Unlink(instance);
                    RuleInstances.Rows.Free(RuleInstances.Rows.At(instance));
                    removed = true;
                    break;
                }
            }
        }

        Handle<Bin> at = Businesses.BinHead[slot];
        Handle<Bin> keptHead = default;
        Handle<Bin> keptTail = default;

        while (!at.IsNone)
        {
            int binSlot = Bins.Rows.Resolve(at);
            Handle<Bin> next = Bins.OwnerNext[binSlot];

            if (!Rules.IsConserved(Bins.Resource[binSlot]))
            {
                // A seller left the market with its stock. Narrowed to this branch for
                // CreateTraderBin's reason: the balance below survives the tenancy and was never
                // merchandise, and most Businesses in this build hold nothing else.
                Markets.Invalidate();
            }

            if (Rules.IsConserved(Bins.Resource[binSlot]))
            {
                Bins.OwnerNext[binSlot] = default;

                if (keptTail.IsNone)
                {
                    keptHead = at;
                }
                else
                {
                    Bins.OwnerNext[Bins.Rows.Resolve(keptTail)] = at;
                }

                keptTail = at;
            }
            else
            {
                WakeAll(binSlot, Tick);
                Bins.Rows.Free(at);
            }

            at = next;
        }

        Businesses.BinHead[slot] = keptHead;
        Businesses.BinTail[slot] = keptTail;
    }

    /// <summary>
    /// Where in its own rate a new Rule Instance first comes due — uniform over <c>[1, rate]</c>.
    /// </summary>
    /// <remarks>
    /// <b>Hash-bearing, and derived rather than chosen</b>, which is what
    /// <c>adr/0052</c> asks of a number like this. The window is the Rule's own <c>rate</c> because a
    /// Rule re-arms at <c>+rate</c> for ever after: any other window would spread the first firing and
    /// then let the population re-converge. See <see cref="PurposeTag.RuleArmingStagger"/>.
    /// <para>
    /// <b>A rate at or beyond the wheel's period is left to throw.</b> Such a Rule could not re-arm
    /// after its first success either, so clamping here would buy one working firing and fail on the
    /// second — <see cref="EventWheel.Arm"/>'s refusal names the coarse wheel, which is the correct
    /// diagnosis and the one worth surfacing at world creation rather than a rate later.
    /// </para>
    /// </remarks>
    private uint ArmingStagger(ulong subject, RuleId rule, Ticks now, WorldKey key)
    {
        uint rate = Rules.Rule(rule).Rate;

        // The Rule as well as the subject, so that two Rules on one subject do not share an offset
        // and arrive together every time — which is the same bucket spike, one Building wide. The
        // subject's contribution is its monotonic never-reused id and not its slot, for the reason
        // the State Hash folds the same thing: a recycled slot would make a demolished Building's
        // replacement inherit its schedule.
        //
        // ⚠ THE SUBJECT IS THE TENANT FOR A TENANT'S RULE AND THE BUILDING FOR THE PREMISES', which
        // is adr/0141's stagger clause and a correctness bug rather than a quality one: three
        // Households in one dwelling all running `consume` would otherwise mix the same Building id
        // with the same RuleId, and land in one Wheel bucket together for the life of the world. The
        // two id spaces are different tables' and may collide across them -- which is harmless,
        // because a collision only matters between two rows that share a RuleId AND a bucket, and a
        // Rule belongs to one tenancy side by construction.
        ulong entity = Randomness.Mix(subject ^ ((ulong)rule.Raw << 32));
        ulong draw = Randomness.Draw(key, entity, now, PurposeTag.RuleArmingStagger);

        return 1u + (uint)(draw % rate);
    }

    /// <summary>
    /// Puts a Bin for <paramref name="resource"/> on <paramref name="owner"/>, empty, at the ceiling
    /// its kind declares.
    /// </summary>
    /// <remarks>
    /// <b>It takes no capacity, and that is <c>adr/0064</c> rather than a convenience.</b> A ceiling
    /// is a function of <c>(kind, Resource)</c> against the Ruleset in force, so a caller passing one
    /// would be writing a number the next rebuild overwrites — which is the *stale is silent* failure
    /// the decision exists to make unrepresentable. This applies the same derivation
    /// <see cref="RebuildCapacities"/> does, from the same place, so a Bin is never briefly wrong.
    /// </remarks>
    public Handle<Bin> CreateBin(Handle<Building> owner, ResourceId resource)
    {
        int buildingSlot = Buildings.Rows.Resolve(owner);

        Invariants.Require(
            FindBin(buildingSlot, resource) == Rows.NoSlot,
            Invariant.BuildingHasOneBinPerResource,
            buildingSlot,
            resource.Raw);

        Handle<Bin> handle = Bins.Create(
            owner, resource, DeclaredCapacity(Buildings.Kind[buildingSlot], resource));

        BuildingBins.InsertOrdered(buildingSlot, Bins.Rows.Resolve(handle));

        return handle;
    }

    /// <summary>
    /// The treasury's Bin for <paramref name="resource"/>, or <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <b>It takes no owner, because the treasury is a singleton.</b> <see cref="FindBin"/>'s walk is
    /// short because a Building's Bin set is its kind's; this one is short because the treasury holds
    /// one Bin per conserved Resource and <c>04 §2</c> declares exactly one money Resource.
    /// </remarks>
    public int FindTreasuryBin(ResourceId resource)
    {
        foreach (int bin in TreasuryBins.Walk(TreasuryTable.Slot))
        {
            if (Bins.Resource[bin] == resource)
            {
                return bin;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>
    /// Gives the treasury a Bin for <paramref name="resource"/>, empty and unbounded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Unbounded, and it is derived rather than declared.</b> <c>04 §2</c>: <em>"Money is a Resource
    /// too, and its Bin is unbounded."</em> There is no <c>[[building]]</c> kind to read a ceiling off
    /// — the treasury has no kind — so <see cref="DeclaredCapacity"/> has nothing to say here and
    /// <see cref="long.MaxValue"/> is what <see cref="BinTable.SpaceAt"/>'s own remark calls the
    /// unbounded spelling. ⚠ <b><see cref="RebuildCapacities"/> derives the same number rather than
    /// skipping this Bin</b>, and it must: the ceiling is a <see cref="Disposition.Derived"/> column,
    /// so it is not in the save and a load — which creates a Bin through <c>Rows.Restore</c> and never
    /// through this method — would otherwise restore a treasury that can hold nothing.
    /// </para>
    /// <para>
    /// <b>It opens empty and nothing authors a value</b>
    /// (<c>adr/0116</c>). The tax flows in before the transfer pays out, so the circuit needs no
    /// opening stock — and an empty treasury is what makes <c>02 §4.2</c>'s <em>pays whom it reaches
    /// and reports where it stopped</em> branch reachable on the first sweep. ⚠ This is <b>not</b> the
    /// founding balance, which is a different quantity with a different owner and happens to share
    /// this one's range.
    /// </para>
    /// </remarks>
    public Handle<Bin> CreateTreasuryBin(ResourceId resource)
    {
        Invariants.Require(
            FindTreasuryBin(resource) == Rows.NoSlot,
            Invariant.BuildingHasOneBinPerResource,
            TreasuryTable.Slot,
            resource.Raw);

        Handle<Bin> handle = Bins.Create(BinOwnerKind.Treasury, resource, long.MaxValue);

        TreasuryBins.InsertOrdered(TreasuryTable.Slot, Bins.Rows.Resolve(handle));

        return handle;
    }

    /// <summary>
    /// Gives the treasury one Bin per conserved Resource the Ruleset in force declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Conserved rather than every Resource</b>, which is <see cref="Ruleset.IsConserved"/>'s own
    /// test — the Money family. <c>02 §4.3</c> is narrow about what <c>global</c> is for: it
    /// <em>"names the treasury, and it appears only as the far end of an explicit transfer — local
    /// money out, global money in"</em>. A treasury Bin for Food would be a city-wide larder nothing
    /// in the design describes.
    /// </para>
    /// <para>
    /// <b>Asked rather than assumed, exactly as <see cref="Fit"/> asks.</b> This runs at world load and
    /// at every Ruleset swap, and a swap meets a treasury that already holds the Bins that survived the
    /// migration. ⚠ <b>It adds and never removes</b>: a Resource that leaves the Ruleset leaves its
    /// treasury Bin standing with its stock, which is the same answer the migration gives a Building —
    /// dropping it would destroy conserved money to satisfy a file edit, and <c>adr/0024</c> puts the
    /// Outside Connection between money and non-existence.
    /// </para>
    /// </remarks>
    internal void FitTreasury()
    {
        for (int raw = 1; raw <= Rules.ResourceCount; raw++)
        {
            var resource = new ResourceId((ushort)raw);

            if (Rules.IsConserved(resource) && FindTreasuryBin(resource) == Rows.NoSlot)
            {
                CreateTreasuryBin(resource);
            }
        }
    }

    /// <summary>
    /// A District's Pool Bin for <paramref name="resource"/>, or <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <b>A walk, and it is a walk over the whole join rather than over one District's list.</b>
    /// <see cref="FindTreasuryBin"/> and <see cref="FindBin"/> each walk an intrusive list because
    /// their owner rows carry a head; a Pool's owner row does not, because the relation is saved in
    /// <see cref="Space.DistrictPoolTable"/> rather than threaded. ⚠ <b>Every caller here is cold</b>
    /// — opening a Pool, and retiring one — and the table is one row per Good per District, so the
    /// product is small twice over. ✅ <b>The hot path arrived with the purchase and does not come
    /// through here</b>: <see cref="Space.DistrictMarkets"/> is the <c>(District, Resource) → row</c>
    /// index milestone 26 task 4 owed, and this walk is what its rebuild reads. ***A lookup that
    /// consulted the thing it builds could not build it***, which is why the walk stays rather than
    /// being replaced.
    /// </remarks>
    public int FindDistrictPoolBin(int districtSlot, ResourceId resource)
    {
        Handle<District> district = Districts.Rows.At(districtSlot);

        if (district.IsNone)
        {
            return Rows.NoSlot;
        }

        for (int row = 0; row < DistrictPools.Rows.SlotCount; row++)
        {
            if (!DistrictPools.Rows.IsLive(row) || DistrictPools.District[row] != district)
            {
                continue;
            }

            if (Bins.Rows.TryResolve(DistrictPools.Bin[row], out int bin)
                && Bins.Resource[bin] == resource)
            {
                return bin;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>
    /// Gives a District a Pool Bin for <paramref name="resource"/>, empty and unbounded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Unbounded, and <see cref="RebuildCapacities"/> derives the same number rather than skipping
    /// the row</b> — <see cref="CreateTreasuryBin"/>'s reason exactly, because a ceiling is
    /// <see cref="Disposition.Derived"/> and a load never comes through here. The argument for the
    /// value is at the rebuild site and is not money's.
    /// </para>
    /// <para>
    /// <b>It opens AT THE CEILING</b> — <c>Rules.ImportCeiling</c>, which is the lowest price any
    /// declared Hinterland charges for the Good (<c>adr/0135</c>, milestone 12 task 6). ⚠ <b>That is
    /// why the tâtonnement needed no seed number and <c>plans/0002</c> §D carries two rows here rather
    /// than three</b>: a Pool with no local supply in it should cost what importing costs, and a Pool
    /// nobody has traded in yet has no local supply by construction. ***The seed is not a choice, it
    /// is the answer the mechanism gives when asked before anything has happened.***
    /// </para>
    /// <para>
    /// ⚠ <b>Zero when no <c>[[hinterland]]</c> prices the Good</b>, and that is a Ruleset the loader
    /// refuses whenever the file states <c>[districts]</c> — see <c>RulesetLoader</c>. A world reaching
    /// here with a zero ceiling has a Pool that is free for ever, which is <c>adr/0050</c>'s runaway
    /// arriving from below.
    /// </para>
    /// </remarks>
    public Handle<Bin> CreateDistrictPoolBin(Handle<District> district, ResourceId resource)
    {
        Invariants.Require(
            FindDistrictPoolBin(Districts.Rows.Resolve(district), resource) == Rows.NoSlot,
            Invariant.BuildingHasOneBinPerResource,
            Districts.Rows.Resolve(district),
            resource.Raw);

        Handle<Bin> handle = Bins.Create(BinOwnerKind.District, resource, long.MaxValue);

        DistrictPools.Create(district, handle, Rules.ImportCeiling(resource));

        Markets.Invalidate();

        return handle;
    }

    /// <summary>
    /// Gives every live Water Body the one Bin it holds, if the Ruleset states one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0161</c>, <c>CONTEXT.md</c> → Water Body, milestone 24 task 6b. <b>Exactly one Bin and
    /// exactly one Resource</b>, whose family is <c>Utility</c>: a Water Body moves its contents along
    /// an edge of the water graph, and a Good doing that would move with no Vehicle.
    /// </para>
    /// <para>
    /// <b>Capacity is the body's size times <c>[water] capacity_per_cell</c></b>, which is what makes
    /// <c>CONTEXT.md</c>'s debt-versus-rent a <em>gradient</em> rather than two categories — a small
    /// body accumulates permanently, a large one tracks throughput. ⚠ <b>It is set here AND rederived
    /// in <see cref="RebuildCapacities"/></b>, because <c>bin.capacity</c> is a derived column and a
    /// load must reproduce it.
    /// </para>
    /// <para>
    /// <b>It adds and never removes</b>, on <see cref="FitDistrictPools"/>'s rule: a Ruleset that
    /// stops stating a water Bin must not destroy the level standing in one.
    /// </para>
    /// </remarks>

    /// <summary>
    /// Sheds what the paved city drains into the water below it. <b>Once a Day, before the outflow.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CONTEXT.md</c> → Water Body gives a Bin two inflows, <em>"dumping, runoff"</em>. <b>This is
    /// runoff, and it is the half whose input already existed</b>: the catchment (<c>plans/0042</c>
    /// <b>F14</b>) says which body a dry Cell drains to, and Sealing says how much of that Cell is
    /// paved. ⚠ <b>Dumping is NOT built</b> — it needs a <see cref="Rules.Scope"/> that reaches a Water
    /// Body, and a Bin can <em>fail</em> where a Map Layer cell cannot, which is a design question and
    /// not a copy of <see cref="Rules.Scope.Map"/>.
    /// </para>
    /// <para>
    /// <b>Sealing is the driver, and that is the hydrology as well as the game.</b> Impervious surface
    /// is what makes runoff run rather than soak, so ***the more of a catchment a player paves, the
    /// more it fouls the body it drains into*** — and because the catchment crosses no ownership
    /// boundary and the water graph runs downhill, the consequence lands **downstream of whoever
    /// caused it**. That is the asymmetry <c>CONTEXT.md</c> calls the only one in the design, reached
    /// without a single new verb.
    /// </para>
    /// <para>
    /// ⚠ <b>Shedding does not deplete anything, and Sealing is not consumed by it.</b> Runoff is not a
    /// transfer of a stock — pavement does not run out — so nothing here decrements a Cell. ***The
    /// quantity that IS bounded is the receiving Bin***, which is why capacity exists and why the
    /// deposit is capped rather than asserted.
    /// </para>
    /// <para>
    /// <b>It walks the SEALED Cells and not the map.</b> <see cref="Space.LayerCellTable"/> is sparse
    /// and holds a row only where something happened, so this is proportional to the built city rather
    /// than to <see cref="Space.CellGrid.WorldCellCount"/> — the one whole-map sweep this milestone
    /// did not have to take.
    /// </para>
    /// <para>
    /// <b>Index order is hash-bearing</b>, because two Cells shedding into a body that is nearly full
    /// are separated by which one is met first.
    /// </para>
    /// </remarks>
    internal void RunoffIntoWater(Ticks now)
    {
        if (!Rules.Water.HasBin || Rules.Water.RunoffPerSealedCellPerDay <= 0)
        {
            return;
        }

        Rows<Space.LayerCell> cells = Layers.Cells.Rows;

        for (int slot = 0; slot < cells.SlotCount; slot++)
        {
            if (!cells.IsLive(slot))
            {
                continue;
            }

            int sealed_ = Layers.Cells.Sealing[slot];

            if (sealed_ <= 0)
            {
                continue;
            }

            Handle<Space.WaterBody> body =
                Catchment.At(Layers.Cells.East[slot], Layers.Cells.North[slot]);

            if (!Water.Rows.TryResolve(body, out int into)
                || !Bins.Rows.TryResolve(Water.Bin[into], out int bin))
            {
                continue;
            }

            // Scaled by how much of the Cell is paved, so the authored number is a FULLY sealed Cell's
            // shedding and a Cell half built on sheds half of it. FloorDiv rather than a raw divide --
            // BOR0203 -- and it floors to zero below a thirty-second of a Cell, which is a Cell with
            // almost nothing on it and is the right answer.
            long amount = IntegerMath.FloorDiv(
                (long)sealed_ * Rules.Water.RunoffPerSealedCellPerDay, Space.CellGrid.TilesInCell);

            long space = Bins.SpaceAt(bin);
            amount = amount < space ? amount : space;

            if (amount > 0)
            {
                Deposit(Bins.Rows.At(bin), amount, now);
            }
        }
    }

    /// <summary>
    /// Moves every Water Body's contents one step down the water graph. <b>Once a Day.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CONTEXT.md</c> → Water Body, <c>adr/0161</c>, milestone 24 task 6b. <b>A body sheds
    /// <c>exits × [water] outflow_per_exit_per_day</c></b>, and the three behaviours that entry
    /// describes fall out with no taxonomy: <em>a pond has no outflow and fills</em> (zero exits), a
    /// body touching the map's edge sheds along its whole boundary, and a landlocked lake spills
    /// through the single rim Cell it overtops.
    /// </para>
    /// <para>
    /// ⚠ <b>TWO PHASES, and the second one is why.</b> Withdrawing and depositing in one walk lets
    /// water cross two graph edges in a single Day whenever the chain happens to run in ascending slot
    /// order — ***a body's drainage speed would depend on the order the generator happened to find
    /// it***, which is a determinism-preserving bug rather than a determinism bug and therefore the
    /// worse kind. Every body's outflow is measured against the level it started the Day with.
    /// </para>
    /// <para>
    /// ⚠ <b>A full downstream backs the water up, and that is the model rather than a guard.</b> The
    /// amount is capped by the receiving Bin's headroom, so what cannot leave stays where it is —
    /// which is what makes <c>CONTEXT.md</c>'s <em>"nothing is an infinite sink"</em> true of a
    /// <em>chain</em> and not only of one body.
    /// </para>
    /// <para>
    /// 🔴 <b>Nothing puts anything in, so every level is zero and this pass moves nothing on every
    /// shipped world.</b> No <c>Scope</c> reaches a Water Body — <c>adr/0161</c> names that as
    /// <c>adr/0070</c>'s *unbuilt* — so the mechanism is exercised by tests and by no Ruleset.
    /// </para>
    /// </remarks>
    internal void DrainWaterBodies(Ticks now)
    {
        if (!Rules.Water.HasBin)
        {
            return;
        }

        int slots = Water.Rows.SlotCount;

        if (_waterOutflow.Length < slots)
        {
            _waterOutflow = new long[slots];
        }

        for (int body = 0; body < slots; body++)
        {
            _waterOutflow[body] = 0;

            if (!Water.Rows.IsLive(body)
                || Water.Exits[body] == 0
                || !Bins.Rows.TryResolve(Water.Bin[body], out int bin))
            {
                continue;
            }

            long rate = (long)Water.Exits[body] * Rules.Water.OutflowPerExitPerDay;
            long level = Bins.LevelAt(bin);
            long leaving = level < rate ? level : rate;

            // Capped by where it is going, so a full body downstream backs the water up rather than
            // destroying it. A body draining off the map has no receiver and no cap.
            if (Water.Rows.TryResolve(Water.Downstream[body], out int into)
                && Bins.Rows.TryResolve(Water.Bin[into], out int receiving))
            {
                long space = Bins.SpaceAt(receiving);
                leaving = leaving < space ? leaving : space;
            }

            _waterOutflow[body] = leaving < 0 ? 0 : leaving;
        }

        for (int body = 0; body < slots; body++)
        {
            long leaving = _waterOutflow[body];

            if (leaving == 0 || !Bins.Rows.TryResolve(Water.Bin[body], out int bin))
            {
                continue;
            }

            Withdraw(Bins.Rows.At(bin), leaving, now);

            if (Water.Rows.TryResolve(Water.Downstream[body], out int into)
                && Bins.Rows.TryResolve(Water.Bin[into], out int receiving))
            {
                Deposit(Bins.Rows.At(receiving), leaving, now);
            }
        }
    }

    internal void FitWaterBins()
    {
        if (!Rules.Water.HasBin)
        {
            return;
        }

        for (int slot = 0; slot < Water.Rows.SlotCount; slot++)
        {
            if (!Water.Rows.IsLive(slot) || !Water.Bin[slot].IsNone)
            {
                continue;
            }

            Water.Bin[slot] = Bins.Create(
                BinOwnerKind.WaterBody,
                Rules.Water.Carries,
                WaterCapacity(Water.CellCount[slot]));
        }
    }

    /// <summary>
    /// What a Water Body of this many Cells holds. <b>Saturating rather than wrapping.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The product is taken in <c>long</c> because it overflows <c>int</c> on a real world.</b>
    /// The largest body measured on <c>coastal.toml</c> is <b>33,435</b> Cells, so any
    /// <c>capacity_per_cell</c> above about 64,000 would wrap — and the loader's ceiling on that key is
    /// well above it, deliberately, because a capacity is a quantity of a Resource and the Resource's
    /// units are the Ruleset's own.
    /// </remarks>
    private long WaterCapacity(int cells) => (long)cells * Rules.Water.CapacityPerCell;

    /// <summary>The ceiling on a Bin known to be a Water Body's, found from the body that owns it.</summary>
    /// <remarks>
    /// ⚠ <b>It searches the bodies rather than reading the Bin, because a Bin cannot name a Water
    /// Body</b> — <see cref="BinTable.Owner"/> is bound to the Building table, which is
    /// <see cref="BinOwnerKind.District"/>'s constraint arriving at a seventh owner. The table is 14
    /// to 64 rows on a measured world, so the walk is not worth an index.
    /// </remarks>
    private long WaterCapacityOf(int binSlot)
    {
        for (int body = 0; body < Water.Rows.SlotCount; body++)
        {
            if (Water.Rows.IsLive(body)
                && Bins.Rows.TryResolve(Water.Bin[body], out int owned)
                && owned == binSlot)
            {
                return WaterCapacity(Water.CellCount[body]);
            }
        }

        return 0;
    }

    /// <summary>
    /// Gives every live District one Pool Bin per <see cref="ResourceFamily.Good"/> the Ruleset in
    /// force declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Goods and not every Resource</b>, which is the narrowest thing <c>04 §2</c> supports. Money
    /// is refused because <c>plans/0037</c> decision 10 is open — <em>who holds the Pool's money
    /// between a Provider's deposit and a consumer's draw</em> — and a Bin opened before that is
    /// answered would be the answer, written by whoever needed a column. A Utility is refused because
    /// <c>ResourceFamily.Utility</c> <em>"flows along the District adjacency graph"</em>, which is a
    /// different mechanism with no milestone.
    /// </para>
    /// <para>
    /// <b>It adds and never removes</b>, which is <see cref="FitTreasury"/>'s rule and here it is
    /// load-bearing rather than tidy: a Good leaving the Ruleset must not destroy the stock standing
    /// in its Pool, and <c>04 §2</c>'s audit — <em>"if a hundred units of Food entered the District, a
    /// hundred units must be accounted for"</em> — is what a removal would break. A District's Pool
    /// closes when the <em>District</em> does, in <see cref="RetirePool"/>, and never because a file
    /// was edited.
    /// </para>
    /// <para>
    /// <b>Asked rather than assumed, on <see cref="FitTreasury"/>'s reasoning</b>: it runs at world
    /// load, at every Ruleset swap, and after every evaluation of the watershed, and each of those
    /// meets Districts that already hold some of what it would open.
    /// </para>
    /// </remarks>
    internal void FitDistrictPools()
    {
        for (int slot = 0; slot < Districts.Rows.SlotCount; slot++)
        {
            if (!Districts.Rows.IsLive(slot))
            {
                continue;
            }

            Handle<District> district = Districts.Rows.At(slot);

            for (int raw = 1; raw <= Rules.ResourceCount; raw++)
            {
                var resource = new ResourceId((ushort)raw);

                if (Rules.Family(resource) == ResourceFamily.Good
                    && FindDistrictPoolBin(slot, resource) == Rows.NoSlot)
                {
                    CreateDistrictPoolBin(district, resource);
                }
            }
        }
    }

    /// <summary>
    /// Closes a dying District's Pool, moving what it holds into <paramref name="heir"/>'s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The heir is whoever now owns the dying District's CENTRE Cell</b>, decided by the caller —
    /// the same one Cell that decides identity in <c>DistrictWatershed</c>'s first pass, used for
    /// succession. ⚠ <b>That symmetry is the argument</b>: a District <em>is</em> its centre
    /// (<c>adr/0134</c>), so the row that inherits the centre is the row that inherited the District,
    /// and any other rule would make identity and succession disagree about the same Cell.
    /// </para>
    /// <para>
    /// 🔴 <b>A District can die with NO heir, and this would destroy Goods if a Pool ever held any.
    /// It cannot, and the reason changed at milestone 26 without this transfer being reached once.</b>
    /// ⚠ <b>The paragraph below predicted the wrong trigger and is kept so the correction is legible:
    /// it said the check <em>"will fail on the day task 7 opens the scope"</em>, and task 4 opened
    /// the scope and it did not.</b> <c>adr/0139</c> — written the day after this comment — makes the
    /// Pool a <b>market and not a store</b>: the stock stays in the selling Business's own Bin and the
    /// market row is a price, a wake target and a list of reachable sellers. ***So nothing deposits
    /// into a Pool Bin in this build either, and <c>held</c> is zero for ever by a different
    /// argument.*** <see cref="Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool"/> still makes that a
    /// check instead of a hope, and <c>ProvisionedRulesetTests</c> asserts the same thing every Tick
    /// of a run on the one world that trades. <b>The transfer below is therefore unreachable</b>, and
    /// <c>adr/0139</c> says it and this invariant both go — struck deliberately rather than deleted
    /// because a method disappeared. *What it said:* <em>"It is not a live defect today and the reason
    /// is exact rather than lucky: <c>Scope.Pool</c> throws, so nothing in the build can put a unit
    /// into a Pool, and every Pool is empty at every moment."</em> ***The conclusion held and its
    /// stated cause was retired without anybody noticing***, which is <c>adr/0093</c> exactly.
    /// </para>
    /// <para>
    /// <b>It opens the heir's Bin on demand rather than trusting <see cref="FitDistrictPools"/> to
    /// have run.</b> The heir may have been created by the very evaluation that is retiring this Pool,
    /// and fitting runs after — so the alternative is an ordering that has to be got right rather than
    /// a call that cannot be got wrong.
    /// </para>
    /// </remarks>
    internal void RetirePool(int districtSlot, Handle<District> heir)
    {
        Markets.Invalidate();

        Handle<District> dying = Districts.Rows.At(districtSlot);

        if (dying.IsNone)
        {
            return;
        }

        bool inherited = Districts.Rows.TryResolve(heir, out int heirSlot) && heirSlot != districtSlot;

        for (int row = DistrictPools.Rows.SlotCount - 1; row >= 0; row--)
        {
            if (!DistrictPools.Rows.IsLive(row) || DistrictPools.District[row] != dying)
            {
                continue;
            }

            if (Bins.Rows.TryResolve(DistrictPools.Bin[row], out int bin))
            {
                long held = Bins.LevelAt(bin);

                Invariants.Require(
                    inherited || held == 0,
                    Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool,
                    districtSlot,
                    bin);

                if (inherited && held != 0)
                {
                    ResourceId resource = Bins.Resource[bin];
                    int into = FindDistrictPoolBin(heirSlot, resource);

                    if (into == Rows.NoSlot)
                    {
                        into = Bins.Rows.Resolve(CreateDistrictPoolBin(heir, resource));
                    }

                    Bins.Move(bin, -held);
                    Bins.Move(into, held);
                }

                // Before the Free, and it is DestroyBuilding's order rather than a precaution: a
                // waiter left on a freed Bin is a Rule Instance pointing at a recycled row, and the
                // only thing that would notice is a whole-world walk long afterwards.
                WakeAll(bin, Tick);

                Bins.Rows.Free(Bins.Rows.At(bin));
            }

            DistrictPools.Rows.Free(DistrictPools.Rows.At(row));
        }
    }

    /// <summary>
    /// <b>Moves every Pool price one Day's step</b> — <c>adr/0135</c>'s damped tâtonnement, milestone
    /// 12 task 6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One pass over <c>DistrictPools</c>, and the whole market is one column read and one written.</b>
    /// For each row: fold the Day's <see cref="Space.DistrictPoolTable.Consumed"/> into the standing
    /// <see cref="Space.DistrictPoolTable.Rate"/>, zero the bucket, and reprice from the Bin's level
    /// against that rate — <see cref="MarketRuleset.Reprice"/> holds the arithmetic and the argument
    /// for it.
    /// </para>
    /// <para>
    /// ⚠ <b>The bucket is zeroed even when <c>[market]</c> is absent.</b> A file with no damping still
    /// accumulates draws, and carrying a Day's worth into the next Day would make the rate depend on
    /// how long the Ruleset had gone unstated — so the zeroing is unconditional and only the price is
    /// gated. ***A cadence that skips its own reset is a cadence whose absence is not the same city
    /// twice.***
    /// </para>
    /// <para>
    /// <b>Whole-table, and there is no index.</b> Once in 2048 Ticks over a table with one row per Good
    /// per District — two Districts and a handful of Goods on the only world that has any — so the walk
    /// is cheaper than what indexing it would cost to maintain. <c>DistrictPoolTable</c> records what
    /// changes that, and it is task 7's purchase rather than this.
    /// </para>
    /// <para>
    /// 🔴 <b>It is correct and it is inert, on every Ruleset that exists.</b> Nothing writes
    /// <see cref="Space.DistrictPoolTable.Consumed"/> while <c>Scope.Pool</c> throws, so every rate is
    /// zero, <see cref="MarketRuleset.Reprice"/> reads that as *no trades*, and every price stays at
    /// the ceiling it opened at. <b>Task 7 is the writer</b>, and the price has to exist before a
    /// purchase can settle at one.
    /// </para>
    /// </remarks>
    internal void RepriceDistrictPools()
    {
        MarketRuleset market = Rules.Market;

        for (int row = 0; row < DistrictPools.Rows.SlotCount; row++)
        {
            if (!DistrictPools.Rows.IsLive(row)
                || !Bins.Rows.TryResolve(DistrictPools.Bin[row], out int bin))
            {
                continue;
            }

            long rate = market.Smooth(DistrictPools.Rate[row], DistrictPools.Consumed[row]);

            DistrictPools.Rate[row] = rate;
            DistrictPools.Consumed[row] = 0;

            DistrictPools.Price[row] = market.Reprice(
                DistrictPools.Price[row],
                Rules.ImportCeiling(Bins.Resource[bin]),
                Bins.LevelAt(bin),
                rate);
        }
    }

    /// <summary>
    /// The one conserved Resource the Ruleset in force declares, if it declares one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An actor's balance is a single Bin, so there is exactly one money Resource or none</b>
    /// (<c>adr/0114</c>, and see <see cref="HouseholdTable.Balance"/> for why the link sits on the
    /// actor). The treasury is the asymmetric one and it is right to be: it holds one Bin per
    /// conserved Resource because <see cref="TreasuryBins"/> is a list, and a list is what a singleton
    /// can afford.
    /// </para>
    /// <para>
    /// ⚠ <b>A second conserved Resource throws here rather than being quietly ignored.</b> Silently
    /// balancing on the first would give every actor a balance in one currency and none in the other,
    /// and every sum in <see cref="Invariant.MoneyIsConserved"/> would still add up — money in a
    /// Resource nobody can hold is money nothing can lose. <c>adr/0114</c>'s revisit trigger already
    /// calls a second money Resource <em>"a decision rather than a detail"</em>; this is that decision
    /// being demanded rather than defaulted.
    /// </para>
    /// </remarks>
    internal bool TryMoneyResource(out ResourceId money)
    {
        money = default;

        bool found = false;

        for (int raw = 1; raw <= Rules.ResourceCount; raw++)
        {
            var resource = new ResourceId((ushort)raw);

            if (!Rules.IsConserved(resource))
            {
                continue;
            }

            if (found)
            {
                throw new NotSupportedException(
                    $"the Ruleset in force declares conserved Resources {money.Raw} and {resource.Raw}. "
                    + "An actor's balance is one saved Bin handle (adr/0114), so a second money "
                    + "Resource is a decision about what a Household holds two of, and that ADR's "
                    + "revisit trigger names it as one. It is not defaulted here.");
            }

            money = resource;
            found = true;
        }

        return found;
    }

    /// <summary>
    /// Gives every actor that has no balance yet a money Bin, if the Ruleset in force names money.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="FitTreasury"/>'s argument, on the actors.</b> It runs at world load and at every
    /// Ruleset swap, and for the swap's reason: a Ruleset that adds a conserved Resource is a
    /// <see cref="RulesetChange.None"/> edit if it declares no new kind, so a world balanced only at
    /// creation would never acquire the Bins. ⚠ <b>It adds and never removes</b> — a Resource that
    /// leaves the Ruleset leaves every balance standing with its stock, because dropping them would
    /// destroy conserved money to satisfy a file edit and <c>adr/0024</c> puts the Outside Connection
    /// between money and non-existence.
    /// </para>
    /// <para>
    /// ⚠ <b>It is <c>O(actors)</c> at a Ruleset swap and that is a real cost at a million Citizens</b>
    /// — 360,000 Households — where <see cref="FitTreasury"/> is <c>O(1)</c>. It is the same order as
    /// <see cref="RebuildCapacities"/> and <see cref="EvictOverflow"/>, which run beside it for the
    /// same reason, so a swap was already a whole-world pass and this does not change its class.
    /// </para>
    /// </remarks>
    internal void FitBalances()
    {
        if (!TryMoneyResource(out ResourceId money))
        {
            return;
        }

        // adr/0143: the test is the LIST rather than the derived Balance handle. A reload rebuilds
        // Balance from the list, so an actor that already holds a money Bin is one whose list contains
        // one -- and asking the derived column instead would open a second Bin for an actor that has
        // one whenever this ran before RebuildDerived did.
        for (int slot = 0; slot < Households.Rows.SlotCount; slot++)
        {
            if (Households.Rows.IsLive(slot)
                && FindOwnerBin(Households.BinHead, slot, money).IsNone)
            {
                Handle<Bin> balance = OpenBalance(BinOwnerKind.Household, money);

                AppendOwnerBin(Households.BinHead, Households.BinTail, slot, balance);
                Households.Balance[slot] = balance;
            }
        }

        for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
        {
            if (Businesses.Rows.IsLive(slot)
                && FindOwnerBin(Businesses.BinHead, slot, money).IsNone)
            {
                Handle<Bin> balance = OpenBalance(BinOwnerKind.Business, money);

                AppendOwnerBin(Businesses.BinHead, Businesses.BinTail, slot, balance);
                Businesses.Balance[slot] = balance;
            }
        }
    }

    /// <summary>
    /// Opens one actor's money Bin — empty, unbounded, and owned by nothing the Bin can point at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Unbounded for <see cref="CreateTreasuryBin"/>'s reason and not by analogy with it</b>:
    /// <c>04 §2</c> says money's Bin is unbounded, and there is no <c>[[building]]</c> kind to read a
    /// ceiling off, because the owner is not a Building. <see cref="RebuildCapacities"/> derives the
    /// same number from the same sentence, so the write here is the one that makes a Bin usable before
    /// the next rebuild rather than the one that decides what its ceiling is.
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="BinTable.Owner"/> is left unset and <see cref="BinTable.OwnerKind"/> is the
    /// whole of what says who owns this.</b> That column is a <c>HandleColumn&lt;Building&gt;</c> and
    /// an actor is not a Building, so there is nothing to write; the link that can be walked runs the
    /// other way, from the actor's own saved handle. The kind is still folded, so two Bins with the
    /// same Resource and different owner kinds stay distinct in the State Hash.
    /// </para>
    /// </remarks>
    private Handle<Bin> OpenBalance(BinOwnerKind kind, ResourceId money) =>
        Bins.Create(kind, money, long.MaxValue);

    /// <summary>
    /// Opens a Bin a Household holds, at the ceiling <b>its premises' kind</b> declares.
    /// </summary>
    /// <remarks>
    /// <b>Two owners in one call, and that is <c>adr/0141</c> rather than an awkward signature.</b>
    /// The Household holds the level and the Building declares the ceiling, so the creation site
    /// needs both — <em>a shop holds what fits in the shop, and what is in it is the shopkeeper's.</em>
    /// ⚠ <see cref="CreateBin"/> reads its kind from the owner because there the two are the same
    /// thing; here they are not, which is the whole of what this method adds.
    /// </remarks>
    private Handle<Bin> CreateOccupantBin(
        Handle<Household> owner, int premisesSlot, ResourceId resource)
    {
        int slot = Households.Rows.Resolve(owner);

        Handle<Bin> handle = Bins.Create(
            BinOwnerKind.Household,
            resource,
            DeclaredCapacity(Buildings.Kind[premisesSlot], resource));

        AppendOwnerBin(Households.BinHead, Households.BinTail, slot, handle);

        return handle;
    }

    /// <summary>
    /// Opens a Bin a Business holds, at the ceiling <b>its premises' kind</b> declares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="CreateOccupantBin(Handle{Household}, int, ResourceId)"/>'s twin, and the ceiling
    /// comes from the same place for the same reason.</b> <c>adr/0141</c> splits <em>who declares the
    /// ceiling</em> from <em>who holds the level</em>, and a shop's stock is the ADR's own worked
    /// example — ***a shop holds what fits in the shop, and what is in it is the shopkeeper's.***
    /// </para>
    /// <para>
    /// ⚠ <b>This is <c>adr/0139</c>'s seller acquiring somewhere to keep stock</b>, which is what
    /// <c>World.Unpremise</c>'s remark called <em>unbuilt</em>: *"a Business's stock Bins are unbuilt,
    /// and writing the sweep for them now would be a mechanism with no rows to walk."* ***There are
    /// rows to walk now.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is never called for money.</b> A balance is <c>World.OpenBalance</c>'s, at
    /// <c>long.MaxValue</c> and with no premises in its ceiling — so the fit walks skip a conserved
    /// Resource, and a money declaration in <c>[[building]] bins</c> states a tenancy and allocates
    /// nothing (<see cref="Rules.BinTenancy.Business"/>).
    /// </para>
    /// </remarks>
    private Handle<Bin> CreateTraderBin(
        Handle<Business> owner, int premisesSlot, ResourceId resource)
    {
        int slot = Businesses.Rows.Resolve(owner);

        Handle<Bin> handle = Bins.Create(
            BinOwnerKind.Business,
            resource,
            DeclaredCapacity(Buildings.Kind[premisesSlot], resource));

        AppendOwnerBin(Businesses.BinHead, Businesses.BinTail, slot, handle);

        // A seller appeared, so the market index is out of date. ⚠ Here and not in FitBusiness,
        // which is where it was first written: that runs whenever ANY Business takes premises, and
        // adr/0148 gives every dwelling one -- so invalidating there would have been a rebuild per
        // Building raised, on every world, including the eight that have no Districts at all.
        Markets.Invalidate();

        return handle;
    }

    /// <summary>
    /// The Bin a Rule Instance's <c>local</c> term addresses: the Occupant's if it has one, the
    /// premises' otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A scope answers <em>whose is it</em> and not <em>where do I look</em></b> (<c>adr/0050</c>),
    /// and this is that sentence acquiring a third possible answer. Before <c>adr/0141</c> the
    /// subject of every Rule was its Building, so <see cref="FindBin"/> was the whole of
    /// <c>local</c>; then the subject became whatever <c>RuleInstanceTable.Household</c> named; and
    /// <c>adr/0166</c> adds <c>RuleInstanceTable.Business</c> beside it.
    /// </para>
    /// <para>
    /// ⚠ <b>The two Occupant handles are mutually exclusive and the order below is therefore not a
    /// precedence</b>, which matters because it reads like one. The loader refuses a Rule whose
    /// <c>local</c> terms address two owners (<c>adr/0141</c>), so at most one of them is ever set;
    /// ***if both were set this would silently prefer the Household***, and the thing that stops that
    /// is upstream rather than here. <c>plans/0041</c> **G10** called this the binary becoming a
    /// ternary, and this remark is the reason it is safe as one.
    /// </para>
    /// </remarks>
    public int FindLocalBin(int instance, ResourceId resource)
    {
        Handle<Household> tenant = RuleInstances.Household[instance];

        if (!tenant.IsNone)
        {
            return Held(FindOwnerBin(
                Households.BinHead, Households.Rows.Resolve(tenant), resource));
        }

        Handle<Business> trader = RuleInstances.Business[instance];

        if (!trader.IsNone)
        {
            return Held(FindOwnerBin(
                Businesses.BinHead, Businesses.Rows.Resolve(trader), resource));
        }

        return FindBin(Buildings.Rows.Resolve(RuleInstances.Building[instance]), resource);

        int Held(Handle<Bin> bin) => bin.IsNone ? Rows.NoSlot : Bins.Rows.Resolve(bin);
    }

    /// <summary>
    /// Appends a Bin to an Occupant's list of its own Bins (<c>adr/0143</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>At the tail, for <c>adr/0033</c>'s reason</b>: the order Bins were opened in is the order a
    /// walk hands them back, and a push-front list would hand back the reverse — a different city
    /// rather than a different implementation.
    /// </para>
    /// <para>
    /// ⚠ <b>The links are handles, so the empty list is the unset handle rather than a sentinel.</b>
    /// <see cref="Tables.IndexList"/> encodes slot-plus-one precisely because a zeroed <c>int</c> would
    /// read as slot 0; a zeroed <c>Handle</c> already reads as <em>nothing</em>, which is the property
    /// that makes the saved form safe to fold.
    /// </para>
    /// </remarks>
    private void AppendOwnerBin(
        HandleColumn<Bin> head, HandleColumn<Bin> tail, int ownerSlot, Handle<Bin> bin)
    {
        Handle<Bin> last = tail[ownerSlot];

        if (last.IsNone)
        {
            head[ownerSlot] = bin;
        }
        else
        {
            Bins.OwnerNext[Bins.Rows.Resolve(last)] = bin;
        }

        tail[ownerSlot] = bin;
    }

    /// <summary>
    /// The Bin an Occupant holds for one Resource, or the unset handle if it holds none.
    /// </summary>
    /// <remarks>
    /// <b>A walk of one owner's list and not a scan of the Bin table.</b> The list is short by
    /// construction — one Bin per Resource that Occupant keeps — which is the property that let
    /// <c>adr/0143</c> refuse a join table: a join would have needed the dense index
    /// <c>DistrictPoolTable</c> was allowed to defer, because <em>that</em> path is cold and this one
    /// is a Rule term resolved per evaluation.
    /// </remarks>
    private Handle<Bin> FindOwnerBin(HandleColumn<Bin> head, int ownerSlot, ResourceId resource)
    {
        Handle<Bin> at = head[ownerSlot];

        while (!at.IsNone)
        {
            int slot = Bins.Rows.Resolve(at);

            if (Bins.Resource[slot].Raw == resource.Raw)
            {
                return at;
            }

            at = Bins.OwnerNext[slot];
        }

        return default;
    }

    /// <summary>
    /// Writes every live Bin's ceiling from the Ruleset in force (<c>adr/0064</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Run at world load and at every Ruleset swap, including a swap that moved no structure.</b>
    /// The swap case is the one that matters: a capacity edit is a <see cref="RulesetChange.None"/>
    /// change, so it skips the migration entirely — which is precisely how the edit used to reach
    /// nothing standing.
    /// </para>
    /// <para>
    /// <b>Off the Tick, and <c>O(bins)</c>.</b> It is the same walk the rejected sweep would have
    /// made, at the same moments, writing the same numbers; what the derivation buys over the sweep is
    /// that the column stops being state a reload periodically corrects.
    /// </para>
    /// </remarks>
    internal void RebuildCapacities()
    {
        for (int slot = 0; slot < Bins.Rows.SlotCount; slot++)
        {
            if (!Bins.Rows.IsLive(slot))
            {
                continue;
            }

            // ⚠ A MONEY BIN'S CEILING IS DERIVED HERE AND WAS `SET ONCE AT CREATION AND LEFT` UNTIL
            // TASK 4C, WHICH WAS A LIVE SAVE/LOAD DEFECT. Capacity is a DERIVED column (adr/0064), so
            // it is not in the file and a load rebuilds it from nothing -- and a load creates a Bin
            // through Rows.Restore rather than through CreateTreasuryBin, so `at creation` never
            // happens on that path. A reloaded treasury would have come back with a ceiling of ZERO:
            // a treasury every transfer into it fails against, which is exactly the outcome the old
            // comment here named as the reason for skipping.
            //
            // It was invisible because no fixture in the audit had a treasury Bin -- Ruleset.Empty
            // names no money, so FitTreasury made none -- and it surfaced the moment task 4c gave the
            // golden fixture a currency. ***A derived column with one write site outside the rebuild
            // is a column that does not survive a load, and a fixture that never creates one cannot
            // report it.*** adr/0093 on the lifecycle axis: the comment was right about where the
            // number comes from and wrong about when it is written.
            //
            // 04 §2 is the source -- "Money is a Resource too, and its Bin is unbounded" -- so the
            // ceiling is derived from the RESOURCE rather than from a kind, which is why no owner
            // needs to be resolved for it.
            ResourceId resource = Bins.Resource[slot];

            // ⚠ A Bin naming a Resource the INCOMING Ruleset does not declare, which a swap reaches
            // and a fresh world cannot: ids run 1..ResourceCount, so asking this file about a
            // Resource it never heard of indexes past the end. It keeps the ceiling it has, which is
            // FitTreasury and FitBalances' answer to the same situation -- they add and never remove,
            // because a Resource leaving a file must not destroy the stock held in it (adr/0024 puts
            // the Outside Connection between money and non-existence). A migration that means to drop
            // the Bin says so through RulesetMigration, and that runs before this.
            if (resource.Raw > Rules.ResourceCount)
            {
                continue;
            }

            if (Rules.IsConserved(resource))
            {
                Bins.SetCapacity(slot, long.MaxValue);
                continue;
            }

            // A District's Pool is unbounded, and the ARGUMENT is not money's even though the number
            // is. Money's ceiling is refused because a ceiling on a balance models nothing; a Pool's
            // is absent because there is no shed -- CONTEXT.md -> District Pool has Goods passing
            // through it INSTANTLY, so the Pool is a clearing point rather than a store, and the
            // thing that discourages selling into a full one is adr/0135's falling price rather than
            // a wall. ⚠ It is also the only ceiling available: a capacity is a function of (Building
            // kind, Resource) and a District has no kind, so an authored ceiling here would be the
            // const-where-a-Ruleset-value-belongs that adr/0015 calls a defect.
            if (Bins.OwnerKind[slot] == BinOwnerKind.District)
            {
                Bins.SetCapacity(slot, long.MaxValue);
                continue;
            }

            // A Water Body's Bin, whose ceiling IS derivable without a Building kind -- the one
            // owner other than a Building for which that is true. It is the body's own size times
            // [water] capacity_per_cell, and CONTEXT.md -> Water Body is explicit that size is what
            // makes pollution behave as a debt in a small body and a rent in a large one. ⚠ Rederived
            // here rather than only at FitWaterBins because bin.capacity is a DERIVED column and a
            // load must reproduce it; both sides read the same two saved numbers. milestone 24 task
            // 6b.
            if (Bins.OwnerKind[slot] == BinOwnerKind.WaterBody)
            {
                Bins.SetCapacity(slot, WaterCapacityOf(slot));
                continue;
            }

            // A tenant's Bin, left to the owner walk below. ⚠ THIS IS THE HALF adr/0143 PREDICTED:
            // its ceiling comes from the PREMISES (adr/0141), and a Bin cannot name its owner -- which
            // is exactly what that record gave up -- so the only way to reach a Household from a Bin
            // is not to start from the Bin. `RebuildCapacities walks owners rather than Bins`, in its
            // own words, and here it does both: the cases a Bin can answer alone stay in this loop.
            if (Bins.OwnerKind[slot] == BinOwnerKind.Household)
            {
                continue;
            }

            // ✅ AND A BUSINESS'S, for the identical reason and by the identical route. This case
            // THREW until milestone 26 task 1, with a comment naming it open decision 1 of
            // plans/0040 -- "nothing creates a Business's stock Bin yet, so it throws rather than
            // defaulting". adr/0139 put the seller's inventory on the Business and adr/0166 gave it
            // a tenancy to hang off, so the ceiling now comes from the premises' kind exactly as a
            // Household's does, and the owner walk below is where both are in scope at once.
            if (Bins.OwnerKind[slot] == BinOwnerKind.Business)
            {
                continue;
            }

            // Anything else owned by something that is not a Building has no ceiling to derive: a
            // ceiling is a function of (Building kind, Resource), and there is no second source.
            if (Bins.OwnerKind[slot] != BinOwnerKind.Building)
            {
                throw new NotSupportedException(
                    $"bin {slot} holds a non-conserved Resource and is owned by "
                    + $"{Bins.OwnerKind[slot]} rather than a Building, so there is no declaration to "
                    + "derive its ceiling from. A ceiling is a function of (Building kind, Resource) "
                    + "and 04 §2's unbounded spelling is money's alone (adr/0064, adr/0114).");
            }

            byte kind = Buildings.Rows.TryResolve(Bins.Owner[slot], out int buildingSlot)
                ? Buildings.Kind[buildingSlot]
                : (byte)0;

            Bins.SetCapacity(slot, DeclaredCapacity(kind, resource));
        }

        // The tenants' Bins, from the owner rather than from the Bin (adr/0143). A Household's Bins
        // hang off the Household and its ceiling hangs off the Building it lives in, so this walk is
        // the only place both are in scope at once.
        //
        // ⚠ AN UNHOUSED HOUSEHOLD HOLDS NO BIN BUT ITS BALANCE, by construction rather than by luck:
        // UnfitOccupant closes the rest when the tenancy ends, precisely because there would be no
        // kind here to read a ceiling from. A conserved Bin skips because its ceiling is money's and
        // the loop above has already written it.
        for (int slot = 0; slot < Households.Rows.SlotCount; slot++)
        {
            if (!Households.Rows.IsLive(slot))
            {
                continue;
            }

            // A derelict dwelling keeps the ceilings it had, which is the same answer this method
            // gives a derelict Building's own Bins two loops up: DeclaredCapacity of an undeclared
            // kind is a statement about a file rather than about the city.
            byte kind = Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot)
                ? Buildings.Kind[buildingSlot]
                : (byte)0;

            Handle<Bin> at = Households.BinHead[slot];

            while (!at.IsNone)
            {
                int binSlot = Bins.Rows.Resolve(at);
                ResourceId held = Bins.Resource[binSlot];

                if (held.Raw <= Rules.ResourceCount && !Rules.IsConserved(held))
                {
                    Bins.SetCapacity(binSlot, DeclaredCapacity(kind, held));
                }

                at = Bins.OwnerNext[binSlot];
            }
        }

        // The traders' Bins, on the Households' rule and by the same walk (adr/0141, adr/0166). A
        // Business's Bins hang off the Business and their ceilings hang off the Building it tenants,
        // so this is the only place both are in scope at once.
        //
        // ⚠ AN UNPREMISED BUSINESS HOLDS NO BIN BUT ITS BALANCE, by construction rather than by
        // luck: UnfitBusiness closes the rest when the tenancy ends, precisely because there would be
        // no kind here to read a ceiling from. ***That is what makes adr/0142's unpremised steady
        // state safe*** -- a Business between premises is a row, a balance and nothing that needs a
        // ceiling.
        for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
        {
            if (!Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            byte kind = Buildings.Rows.TryResolve(Businesses.Building[slot], out int buildingSlot)
                ? Buildings.Kind[buildingSlot]
                : (byte)0;

            Handle<Bin> at = Businesses.BinHead[slot];

            while (!at.IsNone)
            {
                int binSlot = Bins.Rows.Resolve(at);
                ResourceId held = Bins.Resource[binSlot];

                if (held.Raw <= Rules.ResourceCount && !Rules.IsConserved(held))
                {
                    Bins.SetCapacity(binSlot, DeclaredCapacity(kind, held));
                }

                at = Bins.OwnerNext[binSlot];
            }
        }

        // The Car Parks, on the Bins' rule and with the difference stated at CarParkTable.SpaceAt: a
        // lowered ceiling leaves a Bin to drain and leaves a Car Park over-full, because a Bin has a
        // consumer and a parked car has a holder. The dismissal that resolves it is a write to the
        // *holders* as well as to this column, so it does not belong in a capacity rebuild -- it is
        // task 4's, beside the acquire and the release it has to stay paired with.
        for (int slot = 0; slot < CarParks.Rows.SlotCount; slot++)
        {
            if (!CarParks.Rows.IsLive(slot))
            {
                continue;
            }

            // A derelict Building keeps the parking it had, exactly as it keeps its jobs: TryDeclared
            // says *the Ruleset no longer describes this*, which is a statement about a file rather
            // than about the city (CONTEXT.md -> Dereliction).
            if (Buildings.Rows.TryResolve(CarParks.Owner[slot], out int buildingSlot)
                && TryDeclaredParking(Buildings.Kind[buildingSlot], out int spaces))
            {
                CarParks.SetCapacity(slot, spaces);
            }
        }
    }

    /// <summary>
    /// How many Occupants the Ruleset in force allows a Building of <paramref name="kind"/>, or
    /// <c>false</c> where it declares no such kind at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two negative cases are different and must stay so</b> (<c>adr/0068</c>). A declared
    /// kind holding <c>occupants = 0</c> houses nobody, which is what a factory means. A kind the
    /// Ruleset does not mention is <b>derelict</b> (<c>02 §4.3</c>), and it has no ceiling: it keeps
    /// the Occupants it has and admits nobody new. Collapsing them would make a designer deleting a
    /// paragraph evict a District — the loudest possible consequence for the quietest possible edit.
    /// </para>
    /// <para>
    /// <b>There is no derived column behind this and that is deliberate.</b> <c>adr/0068</c> was
    /// drafted saying <c>Rows.Derived</c>, by analogy with a Bin's ceiling, and the analogy does not
    /// survive contact: a Bin needed the column because <see cref="Rules.BinTable.SpaceAt"/> is on
    /// the hot path and would otherwise resolve an owner and walk a declaration list on every check,
    /// where this is read at a guard that runs once per placement and the Building already carries
    /// its <see cref="BuildingTable.Kind"/>. A column here would be a second copy of a fact one field
    /// away — which is the thing <c>adr/0064</c> was about.
    /// </para>
    /// </remarks>
    internal bool TryDeclaredOccupancy(byte kind, out int occupants)
    {
        if (!Rules.Declares(kind))
        {
            occupants = 0;
            return false;
        }

        occupants = Rules.Kind(kind).Occupants;
        return true;
    }

    /// <summary>
    /// How many <em>Households</em> a Building of <paramref name="kind"/> can hold, or <c>false</c>
    /// where the Ruleset declares no such kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="TryDeclaredOccupancy"/> minus the trade the kind comes with</b>
    /// (<c>adr/0148</c>). One ceiling counts both kinds of tenant (<c>adr/0147</c>), so a kind
    /// declaring <c>occupants = 4</c> and a trade holds <b>three</b> families — and ***the two
    /// questions stopped having the same answer on the day a premises could come with a shop.***
    /// </para>
    /// <para>
    /// 🔴 <b>Anything sizing a city must ask THIS one</b>, and a caller that asks the other builds
    /// too few homes and queues the difference for ever. That is not hypothetical: it is what
    /// <see cref="SyntheticCity"/> did for the length of one test run, and the symptom was an
    /// Unplaced Pool growing 6.5% a reading — <c>adr/0006</c>'s shape, produced by arithmetic rather
    /// than by a missing sink.
    /// </para>
    /// <para>
    /// ⚠ <b>It is a property of the KIND and not of a standing Building.</b> A Building may hold a
    /// second shop that walked in through placement, and then houses fewer families than this says;
    /// the live question is <see cref="HasRoom"/>, which counts what is actually there.
    /// </para>
    /// </remarks>
    internal bool TryDeclaredHousing(byte kind, out int households)
    {
        if (!TryDeclaredOccupancy(kind, out int occupants))
        {
            households = 0;
            return false;
        }

        households = Rules.Kind(kind).Business != 0 && occupants > 0
            ? occupants - 1
            : occupants;

        return true;
    }

    /// <summary>
    /// Whether <paramref name="kind"/> is an Outside Connection — a gate the city can be entered
    /// through (<c>adr/0088</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Declaring a throughput is what makes a kind a gate</b>, so this is the whole test. See
    /// <see cref="Rules.KindDefinition.ArrivalsPerDay"/> for why there is no second key beside it and
    /// why a stated zero is refused at the door.
    /// </para>
    /// <para>
    /// <b>A kind the Ruleset does not declare is not a gate</b>, which follows
    /// <see cref="TryDeclaredOccupancy"/>'s rule and matters for the same reason: dereliction is
    /// <c>Kind == 0</c> (<c>adr/0057</c>), and a derelict Building must not start reading as a door
    /// into the city because its kind vanished from the Ruleset in force.
    /// </para>
    /// </remarks>
    public bool IsOutsideConnection(byte kind) =>
        Rules.Declares(kind) && Rules.Kind(kind).ArrivalsPerDay > 0;

    /// <summary>
    /// How many Households a gate of <paramref name="kind"/> admits per Day, when it is one.
    /// </summary>
    /// <remarks>
    /// <b><see cref="TryDeclaredOccupancy"/>'s shape</b>, and it separates the two cases that matter
    /// for the same reason that one does: a kind the Ruleset no longer declares is a different thing
    /// from a kind that is not a gate, and a caller metering arrivals must not read a derelict gate
    /// as a gate with a ceiling of zero.
    /// </remarks>
    internal bool TryArrivalsPerDay(byte kind, out int arrivals)
    {
        if (!IsOutsideConnection(kind))
        {
            arrivals = 0;
            return false;
        }

        arrivals = Rules.Kind(kind).ArrivalsPerDay;
        return true;
    }

    /// <summary>
    /// Which map edge the Lot at <paramref name="lotSlot"/> stands on, or
    /// <see cref="MapEdge.None"/>.
    /// </summary>
    /// <remarks>
    /// <b>A corner reads as <see cref="MapEdge.None"/> and that is deliberate</b> — see
    /// <see cref="Invariant.OutsideConnectionStandsOnOneEdge"/>. Under <c>adr/0088</c> the edge
    /// selects a market, so a position touching two of them names no market rather than either, and
    /// the caller that cares refuses it. <see cref="MapEdges.Touching"/> is where the two cases are
    /// still distinguishable, for a caller that wants to say which failure it hit.
    /// </remarks>
    public MapEdge EdgeOf(int lotSlot)
    {
        MapEdges.Touching(Lots.East[lotSlot], Lots.North[lotSlot], out MapEdge edge);
        return edge;
    }

    /// <summary>
    /// Whether <paramref name="buildingSlot"/> could take one more Household.
    /// </summary>
    /// <remarks>
    /// <b>The predicate, where <see cref="Invariant.BuildingHasRoomForTheHousehold"/> is the guard.</b>
    /// Placement asks this of every candidate it samples and moves on when the answer is no, which is
    /// an ordinary outcome and not a fault; the guard exists for a caller that placed without asking.
    /// Keeping the two apart is what stops a full city filling the invariant log.
    /// <para>
    /// 🔴 <b>AN ABANDONED SHELL IS REFUSED FIRST, and leaving it out was milestone 17 task 1's own
    /// defect.</b> The occupancy test is <i>declared ceiling against current tenants</i>, and an
    /// abandoned Building has a declared kind and <b>zero</b> tenants — so it does not merely pass,
    /// it looks like the emptiest and most attractive dwelling in the city. Placement rehoused
    /// Households into premises the city had just condemned, and re-premised pooled Businesses into
    /// them, and the collapse then evicted the lot a second time.
    /// </para>
    /// <para>
    /// ⚠ <b>It surfaced as an <c>adr/0006</c> failure on the BUSINESS table</b> — the churn founded
    /// and pooled trades faster than anything retired them — which is a long way from the Building
    /// the defect is about. ***The symptom named the collection that grew, not the predicate that
    /// was wrong.***
    /// </para>
    /// </remarks>
    public bool HasRoom(int buildingSlot) =>
        !Buildings.IsAbandoned(buildingSlot)
        && TryDeclaredOccupancy(Buildings.Kind[buildingSlot], out int occupants)
        && Tenants(buildingSlot) < occupants;

    /// <summary>
    /// How many tenants of <em>any</em> kind <paramref name="buildingSlot"/> holds — Households and
    /// Businesses together.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>occupants</c> counts tenants of any kind</b> (<c>adr/0141</c>, built by <c>adr/0147</c>),
    /// so ***a dwelling declaring three holds three families, or two families and a shop.*** The
    /// consequence is the point: **a city that fills with shops houses fewer people, from one number,
    /// with no rule expressing it.**
    /// </para>
    /// <para>
    /// <b>Two lists summed rather than one list holding both.</b> Each intrusive list threads a
    /// <c>next</c> column on its own owner's table — <c>Households.DwellingNext</c> and
    /// <c>BusinessTable.BuildingNext</c> — so a single mixed list would need a discriminated element
    /// to know which table a slot indexes, which is the polymorphic column <c>adr/0143</c>
    /// deliberately left unbuilt. ***An add costs less than a column.***
    /// </para>
    /// </remarks>
    public int Tenants(int buildingSlot) =>
        Occupants.Length(buildingSlot) + BuildingBusinesses.Length(buildingSlot);

    /// <summary>
    /// Evicts into the Unplaced Pool every Occupant a lowered ceiling has left over
    /// (<c>adr/0068</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eviction rather than <c>adr/0064</c>'s <em>leave it to drain</em>, and the transplant stops
    /// here for one nameable reason: a Bin has a consumer and occupancy has none.</b> Nothing removes
    /// a single Household from a standing Building — no housed departure, no moving — so a Building
    /// left over its ceiling would sit there for the life of the city, consuming at the occupancy it
    /// has while the Ruleset says another number. That is exactly the split city <c>adr/0064</c>
    /// called its own deciding argument, and it is worse here, because a Bin's over-fullness spends
    /// itself and this one never would.
    /// </para>
    /// <para>
    /// <b>Which Occupants leave is a lottery, never list order.</b> Each holds a draw keyed on its own
    /// monotonic id, and the highest lose. Evicting from the tail would be free and would remove the
    /// same families from every Building on every patch, which is <c>02 §8</c> rule 5's argument — the
    /// same one that made the Pool drain a draw rather than a queue. The tag is its own, because
    /// sharing <see cref="PurposeTag.PoolDraw"/> would correlate *who is evicted* with *who is
    /// rehoused* and quietly send the same families straight back.
    /// </para>
    /// <para>
    /// <b>Off the Tick, <c>O(occupants)</c> per over-capacity Building, and usually zero of them.</b>
    /// It runs where <see cref="RebuildCapacities"/> runs and for the same reason: a ceiling edit is a
    /// <see cref="RulesetChange.None"/> change, so it skips the migration entirely.
    /// </para>
    /// </remarks>
    internal void EvictOverflow(Ticks now, WorldKey key)
    {
        for (int slot = 0; slot < Buildings.Rows.SlotCount; slot++)
        {
            if (!Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            byte kind = Buildings.Kind[slot];

            if (TryDeclaredOccupancy(kind, out int allowed))
            {
                // adr/0147: the ceiling counts tenants of any kind, so the loop drains until the SUM
                // fits and the draw ranges over both lists. adr/0141's own words -- "an over-capacity
                // Building evicts, and it never asked what the overflow was" -- so there is no kind
                // preference here and adding one would be a policy claim (PLAYER GOVERNS).
                while (Tenants(slot) > allowed)
                {
                    if (LosingTenant(slot, now, key, out int household, out int business))
                    {
                        Unplace(Households.Rows.At(household));
                    }
                    else
                    {
                        Unpremise(Businesses.Rows.At(business), now);
                    }
                }
            }

        }

        // 🔴 THE JOBS CEILING MOVED OUT OF THE BUILDING LOOP, and it had to. It used to sit inside it,
        // passing Buildings.Kind[slot] to TryDeclaredJobs and indexing Workers by a Building slot --
        // and since milestone 27 task 7 both of those read the BUSINESS namespace (adr/0141). A
        // Building kind byte is not a trade byte and a Building slot is not a Business slot; the old
        // shape agreed with the new one only where both coincided numerically, which is a fixture
        // property rather than a fact about the city.
        //
        // ⚠ The paragraph this replaces argued the two ceilings are asked SEPARATELY because their
        // derelict cases are independent. That argument survives and gets stronger: they are now
        // asked over DIFFERENT TABLES, so they could not share a loop even if somebody wanted them to.
        for (int slot = 0; slot < Businesses.Rows.SlotCount; slot++)
        {
            if (!Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            if (TryDeclaredJobs(Businesses.Kind[slot], out int posts))
            {
                while (Workers.Length(slot) > posts)
                {
                    Dismiss(Citizens.Rows.At(LosingWorker(slot, now, key)));
                }
            }
        }
    }

    /// <summary>
    /// The Occupant of <paramref name="buildingSlot"/> holding the highest draw, which is the one a
    /// lowered ceiling evicts next.
    /// </summary>
    /// <remarks>
    /// Keyed on the Household's <b>monotonic id</b> rather than its slot, for the reason a Zone Rule
    /// keys its Pool draw the same way (<c>02 §8</c> rule 5's footnote): a slot is recycled when a row
    /// is freed, so drawing against one would make an unrelated demolition change who keeps their
    /// home. Re-drawn per eviction rather than sorted once — the list is a handful, and a sort here
    /// would want an allocation on a path that must not have one.
    /// </remarks>
    private int Loser(int buildingSlot, Ticks now, WorldKey key)
    {
        int worst = Rows.NoSlot;
        ulong highest = 0;

        foreach (int household in Occupants.Walk(buildingSlot))
        {
            ulong draw = Randomness.Draw(
                key, Households.Rows.IdAt(household), now, PurposeTag.OverflowEviction);

            if (worst == Rows.NoSlot || draw > highest)
            {
                worst = household;
                highest = draw;
            }
        }

        return worst;
    }

    /// <summary>
    /// The tenant of <paramref name="buildingSlot"/> holding the highest draw across <em>both</em>
    /// kinds. Returns <c>true</c> when that tenant is a Household.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Loser"/> widened to the mixed population <c>adr/0141</c> created</b>, and built
    /// by <c>adr/0147</c>. The two walks are separate because the two lists are separate; the
    /// comparison is one, because ***the ceiling is one.***
    /// </para>
    /// <para>
    /// 🔴 <b>The two draws use DIFFERENT purpose tags and that is load-bearing.</b> A draw is keyed on
    /// an entity's monotonic id, and Household ids and Business ids are <b>independent sequences from
    /// different tables</b> — so Household 5 and Business 5 both exist and under one tag would draw
    /// the <em>identical value</em>. ⚠ ***Two tenants of one Building would be perfectly correlated in
    /// a decision about which of them loses their place.*** This is the build's first draw over a
    /// mixed population, which is why the distinct-tag rule has never had anything to bite on before.
    /// </para>
    /// <para>
    /// <b>Ties go to the Household, and a tie is not reachable.</b> The two tags make a collision a
    /// hash coincidence rather than a structural certainty; the <c>&gt;</c> comparison resolves one
    /// deterministically if it ever happens, which is what a determinism guarantee needs — an answer,
    /// not an absence of the question.
    /// </para>
    /// </remarks>
    private bool LosingTenant(
        int buildingSlot, Ticks now, WorldKey key, out int household, out int business)
    {
        household = Rows.NoSlot;
        business = Rows.NoSlot;

        ulong highest = 0;
        bool any = false;

        foreach (int occupant in Occupants.Walk(buildingSlot))
        {
            ulong draw = Randomness.Draw(
                key, Households.Rows.IdAt(occupant), now, PurposeTag.OverflowEviction);

            if (!any || draw > highest)
            {
                household = occupant;
                business = Rows.NoSlot;
                highest = draw;
                any = true;
            }
        }

        foreach (int tenant in BuildingBusinesses.Walk(buildingSlot))
        {
            ulong draw = Randomness.Draw(
                key, Businesses.Rows.IdAt(tenant), now, PurposeTag.BusinessOverflowEviction);

            if (!any || draw > highest)
            {
                business = tenant;
                household = Rows.NoSlot;
                highest = draw;
                any = true;
            }
        }

        // The winner clears the loser, so exactly one of the two is set on return and the caller
        // cannot evict the wrong one by reading a stale value from the walk that did not win.
        return business == Rows.NoSlot;
    }

    /// <summary>
    /// How many Citizens the Ruleset in force lets a Building of <paramref name="kind"/> employ, or
    /// <c>false</c> where it declares no such kind at all.
    /// </summary>
    /// <remarks>
    /// <b><see cref="TryDeclaredOccupancy"/>'s two negative cases, on the employment axis</b>
    /// (<c>adr/0068</c>'s rule, milestone 5b-bis task 2). A declared kind at <c>jobs = 0</c> employs
    /// nobody, which is what a dwelling means and is every kind in every shipped Ruleset today. A
    /// kind the Ruleset does not mention is <b>derelict</b> and has no ceiling at all: it keeps the
    /// workers it has and takes nobody new, because a designer deleting a paragraph must not sack a
    /// District. There is no derived column behind this for the reason there is none behind
    /// occupancy — it is read at a guard, and the Building already carries its
    /// <see cref="BuildingTable.Kind"/>.
    /// </remarks>
    internal bool TryDeclaredJobs(byte kind, out int jobs)
    {
        // ⚠ The BUSINESS kind as of milestone 27 task 7 (adr/0141): a Citizen is employed by a trade
        // and not by premises, so the ceiling is declared by the trade. The derelict rule below is
        // unchanged and transplants for its own reason rather than by analogy -- a TRADE the Ruleset
        // no longer declares keeps the workers it has and takes nobody new, because a designer
        // deleting a paragraph must not sack a District.
        if (!Rules.DeclaresBusiness(kind))
        {
            jobs = 0;
            return false;
        }

        jobs = Rules.BusinessKind(kind).Jobs;
        return true;
    }

    /// <summary>
    /// How many Vehicles a kind can park, and whether the Ruleset in force declares the kind at all.
    /// </summary>
    /// <remarks>
    /// <b><see cref="TryDeclaredJobs"/>'s shape and its reason</b> (<c>adr/0120</c>): the two answers
    /// have to stay apart, because a kind the Ruleset does not declare is <em>derelict</em> and must
    /// not be treated as a kind that declares no parking. Dereliction must not evict a city's cars any
    /// more than it may sack a District.
    /// </remarks>
    internal bool TryDeclaredParking(byte kind, out int spaces)
    {
        if (!Rules.Declares(kind))
        {
            spaces = 0;
            return false;
        }

        spaces = Rules.Kind(kind).Parking;
        return true;
    }

    /// <summary>
    /// Gives a Building a Car Park at its vehicle Access Point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Address is taken once, here, and is saved from then on</b> (<c>adr/0120</c>, corrected
    /// by this task). It is not derived from the owner, and the reason is the Segment case: a
    /// Building-held Car Park's Address is recoverable from its Building, a Segment-held one's is
    /// where the player put it, and a column is declared once — so deriving would have forced street
    /// parking to bring a second column and made <c>adr/0120</c>'s <em>needs no new column</em> false.
    /// </para>
    /// <para>
    /// ⚠ <b>A Building with no frontage yet gets a Car Park with no Address, and nothing re-points it
    /// today.</b> That is the cost the saved disposition buys, it is bounded to Buildings raised
    /// before their Street exists, and the repair belongs to task 3: the Parking Shed rebuilds on the
    /// per-Segment Epoch, which is the one pass that already runs when frontage changes. Recorded
    /// here rather than worked around, because a silent no-Address Car Park is invisible supply.
    /// </para>
    /// </remarks>
    internal Handle<Parking.CarPark> CreateCarPark(Handle<Building> building, int spaces)
    {
        int buildingSlot = Buildings.Rows.Resolve(building);
        Address door = VehicleAccessPoint(buildingSlot);

        Handle<RoadSegment> segment = door.Exists
            ? Roads.Segments.Rows.At(door.Segment)
            : default;

        Handle<Parking.CarPark> carPark =
            CarParks.Create(building, segment, door.Offset, door.Side, spaces);

        int carParkSlot = CarParks.Rows.Resolve(carPark);

        Buildings.AttachCarPark(buildingSlot, carParkSlot);
        CarParksOnSegments.Add(CarParks, Roads.Segments, carParkSlot);

        return carPark;
    }

    /// <summary>
    /// Takes the nearest parking space with room to <paramref name="door"/>, and records that
    /// <paramref name="citizenSlot"/> holds it.
    /// </summary>
    /// <param name="citizenSlot">The driver. <c>adr/0119</c> puts the space on the Citizen.</param>
    /// <param name="door">The Access Point being arrived at.</param>
    /// <param name="scratch">The caller's own scratch. Never shared across threads.</param>
    /// <param name="carParkSlot">The Car Park taken, or <see cref="Rows.NoSlot"/>.</param>
    /// <returns>Whether a space was found.</returns>
    /// <remarks>
    /// <para>
    /// <b>The two writes are here together because neither is correct alone</b>, which is why
    /// <see cref="Parking.CarParkTable.Move"/> is <see langword="internal"/>. An occupancy bumped
    /// without a holder is capacity conjured from nothing and a holder written without an occupancy is
    /// a space two Citizens can take — the <c>adr/0006</c>-class pair <c>adr/0084</c>'s two invariants
    /// exist to catch, and the only defence that does not rely on remembering is that one method does
    /// both.
    /// </para>
    /// <para>
    /// <b>Nearest-first, first with room, and <em>not</em> nearest-with-room</b>, which is
    /// <c>adr/0009</c>'s sentence taken literally: <i>"nearest-first, and takes the first with
    /// capacity"</i>. The distinction is invisible while the shed is ordered — the first with room
    /// <em>is</em> the nearest with room — and it stops being invisible if anything ever reorders the
    /// answer, so the walk is written to depend on the order rather than to re-derive it.
    /// </para>
    /// <para>
    /// ⚠ <b>An exhausted shed answers <see langword="false"/> and must never answer
    /// <see cref="VehicleAccessPoint"/>.</b> <c>adr/0008</c> forbids the fallback by name, because a
    /// kerbside space at zero cost makes a full car park cheaper than an empty one — the failure that
    /// reads as generosity rather than as a bug. The caller's honest answers are to widen or to fail;
    /// what it may not do is arrive for free.
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="Parking.CarParkTable.SpaceAt"/> may read negative and the test is therefore
    /// <c>&gt; 0</c> rather than <c>!= 0</c>.</b> A lowered <c>[[building]] parking</c> lands the
    /// derived ceiling under the standing occupancy until the overflow is dismissed, and a
    /// <c>!= 0</c> test would read that as room.
    /// </para>
    /// </remarks>
    internal bool TryTakeParking(
        int citizenSlot, Address door, Parking.ShedScratch scratch, out int carParkSlot)
    {
        ArgumentNullException.ThrowIfNull(scratch);

        if (!TryChooseParking(door, scratch, out carParkSlot))
        {
            return false;
        }

        CarParks.Move(carParkSlot, 1);
        Citizens.ParkedIn[citizenSlot] = CarParks.Rows.At(carParkSlot);

        return true;
    }

    /// <summary>
    /// Which space a driver arriving at <paramref name="door"/> would take, <b>without taking it</b>.
    /// </summary>
    /// <returns>Whether any Car Park in the shed has room.</returns>
    /// <remarks>
    /// <para>
    /// <b>The same walk and the same predicate as <see cref="TryTakeParking"/>, which is what lets the
    /// two run at different moments and still agree.</b> A car Trip chooses its space at Trip creation,
    /// because <c>adr/0075</c> creates every Leg then and the drive Leg's second Address is the Car
    /// Park it is driving to; it <em>takes</em> the space on arrival, where the occupancy belongs. Both
    /// go through this method, so the space chosen at the kerb and the space taken at the far end are
    /// the same one unless the occupancy moved in between — and when it did, the take walks on to the
    /// next with room, which is <c>adr/0009</c>'s <i>the shed widens</i> rather than a special case.
    /// </para>
    /// <para>
    /// <b>It writes nothing, and that is the whole reason it exists separately.</b> A Trip refused for
    /// its Budget, or for having no route, must not have to give a space back — and the way to
    /// guarantee that is for the choosing not to have taken one. <c>adr/0009</c> puts the refusal on
    /// the Budget rather than on a Fate of its own: <i>"if the whole shed is full the Trip fails
    /// immediately with Fate exceeded commute budget, which is exactly why this ADR refused a no
    /// parking Fate."</i>
    /// </para>
    /// </remarks>
    internal bool TryChooseParking(Address door, Parking.ShedScratch scratch, out int carParkSlot)
    {
        ArgumentNullException.ThrowIfNull(scratch);

        carParkSlot = Rows.NoSlot;

        if (!Rules.Parking.Runs || !door.Exists)
        {
            return false;
        }

        Span<int> shed = stackalloc int[Rules.Parking.Keeps];

        int kept = Parking.ParkingShed.Nearest(
            Roads, CarParks, CarParksOnSegments, door, Rules.Parking.Radius, scratch, shed, out _);

        for (int i = 0; i < kept; i++)
        {
            if (CarParks.SpaceAt(shed[i]) > 0)
            {
                carParkSlot = shed[i];

                return true;
            }
        }

        return false;
    }

    /// <summary>The Address of the space <paramref name="citizenSlot"/> holds, or none.</summary>
    /// <remarks>
    /// <b>Where a driver walks to to reach their car.</b> A Citizen who holds nothing is the ordinary
    /// answer — they have never parked anywhere — and the caller's fallback is the Building's own
    /// vehicle Access Point. ⚠ <b>That fallback is not <c>adr/0008</c>'s forbidden one</b>: the
    /// prohibition is against an <em>exhausted shed</em> resolving to the kerb at zero cost, which
    /// would make a full car park cheaper than an empty one. This is a car that has never been parked,
    /// it is one journey per Citizen ever, and the next arrival gives them a real space.
    /// </remarks>
    internal Address HeldParkingAddress(int citizenSlot) =>
        CarParks.Rows.TryResolve(Citizens.ParkedIn[citizenSlot], out int carParkSlot)
            ? CarParks.AddressAt(Roads.Segments, carParkSlot)
            : Address.None;

    /// <summary>
    /// Gives up the parking space <paramref name="citizenSlot"/> holds, if it holds one.
    /// </summary>
    /// <returns>Whether a space was given up.</returns>
    /// <remarks>
    /// <para>
    /// <b>It consults no shed, which is <c>adr/0083</c>'s own sentence</b> — <i>"a departing car knows
    /// which Bin it holds, so it decrements that Bin directly"</i>. That is what makes the release
    /// <c>O(1)</c> against the acquire's ball, and it is why the holder is a column rather than a
    /// thing to be searched for.
    /// </para>
    /// <para>
    /// <b>A Citizen holding nothing is the ordinary answer and not a violation.</b> A walker, a
    /// Citizen who has never driven, and a driver already in motion are all <i>parked nowhere</i>, and
    /// <c>CitizenTable.ParkedIn</c>'s zero handle says so without a sentinel. The invariant fires on
    /// the different case below.
    /// </para>
    /// <para>
    /// ⚠ <b>A handle that does not resolve is a demolished Car Park, and it is <em>not</em> a
    /// violation either</b> — <c>ParkedIn</c> is <c>Reference.Severable</c> exactly so this is
    /// representable. Both sides of <c>adr/0084</c>'s conservation sum lose the row together, so a
    /// garage torn down under a parked car cannot read as a leak. What the column is cleared to is the
    /// same zero, and the occupancy is not decremented because the row it would be decremented on is
    /// gone.
    /// </para>
    /// </remarks>
    internal bool ReleaseParking(int citizenSlot)
    {
        Handle<Parking.CarPark> held = Citizens.ParkedIn[citizenSlot];

        if (held.Equals(default))
        {
            return false;
        }

        if (!CarParks.Rows.TryResolve(held, out int carParkSlot))
        {
            // The garage was demolished under a parked car. adr/0079's named absence: the holding is
            // dropped without a decrement, because DestroyBuilding already freed the row that carried
            // the occupancy. Decrementing here would be adr/0084's sum losing one side and not the
            // other, which is the leak the invariant is for rather than a repair of it.
            Citizens.ParkedIn[citizenSlot] = default;

            return false;
        }

        Invariants.Require(
            CarParks.Occupied[carParkSlot] > 0,
            Invariant.ParkingSpaceIsReleasedOnce,
            citizenSlot,
            carParkSlot);

        CarParks.Move(carParkSlot, -1);
        Citizens.ParkedIn[citizenSlot] = default;

        return true;
    }

    /// <summary>
    /// Whether <paramref name="buildingSlot"/> has a job nobody holds.
    /// </summary>
    /// <remarks>
    /// <b>The predicate the assignment pass asks of every candidate it samples</b>, and
    /// <see cref="HasRoom"/>'s exact shape: a full employer is an ordinary answer rather than a
    /// fault, so this is where the cost of asking is paid and there is no invariant behind it.
    /// </remarks>
    public bool HasJob(int businessSlot) =>
        TryDeclaredJobs(Businesses.Kind[businessSlot], out int jobs)
        && Workers.Length(businessSlot) < jobs;

    /// <summary>
    /// Gives a Citizen a Workplace, linking them into that Building's worker list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one door onto <see cref="CitizenTable.Workplace"/>, and that is what the reverse index
    /// costs.</b> Writing the handle without linking leaves a Building whose worker list disagrees
    /// with the Citizens pointing at it — and the disagreement is invisible, because the list is
    /// derived and therefore folds into no hash. It would surface as a reloaded city employing
    /// people a continuously-run one did not.
    /// </para>
    /// <para>
    /// <b>It does not ask <see cref="HasJob"/>, and <see cref="CreateHousehold"/> is the precedent
    /// rather than <see cref="Place"/>.</b> A ceiling guard belongs at the door a *sampling* caller
    /// comes through, where refusing is an ordinary outcome it already handles; a bare mutator that
    /// refused would leave its caller believing a write happened. The assignment pass asks the
    /// predicate; this maintains the list.
    /// </para>
    /// <para>
    /// <b>A Citizen who already works somewhere leaves that list first.</b> Taking a second job
    /// silently is the failure a one-directional write would produce, and it is the same shape as a
    /// Household housed twice — the difference being that a Household has an invariant counting its
    /// appearances and this now has one too.
    /// </para>
    /// </remarks>
    public void Employ(Handle<Citizen> citizen, Handle<Business> workplace, Ticks plannedCommute)
    {
        int slot = Citizens.Rows.Resolve(citizen);
        int buildingSlot = Businesses.Rows.Resolve(workplace);

        Unlist(slot);

        // ⚠ Order: unroster, then rewrite, then roster. Both commute phases are computed from the
        // Workplace handle, so a Citizen re-rostered around a rewritten handle is removed from the
        // buckets its *new* job names and left for ever in the ones its old job put it in --
        // adr/0101, and CommuteRoster.Remove says the same thing from the other end.
        Commutes.Remove(Citizens, slot);

        Citizens.Workplace[slot] = workplace;
        Citizens.PlannedCommute[slot] = plannedCommute;

        // adr/0097: the reach-failure count resets on employment and on nothing else. It is here
        // rather than in the assignment pass for the reason the paragraph above gives for the worker
        // list -- this is the one door onto the handle, so a second path into employment cannot
        // forget to clear it. adr/0069's finding, that a mechanism living inside another mechanism's
        // caller is a mechanism nobody built.
        Citizens.ReachFailures[slot] = 0;

        Invariants.Require(
            !Lists(Workers, buildingSlot, slot),
            Invariant.CitizenIsNotAlreadyEmployedHere,
            slot,
            buildingSlot);

        Workers.InsertOrdered(buildingSlot, slot);
        Commutes.Add(Citizens, Buildings, Businesses, Rules, Key, slot);
    }

    /// <summary>
    /// Records that a job search ended with nothing the Road Graph could deliver inside the Commute
    /// Budget — one occasion, however many candidates it refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The write half of <see cref="CitizenTable.ReachFailures"/>, and it is a method rather than
    /// an indexer assignment at the call site so that the saturation is stated once.</b> A counter
    /// that stops at its width is <c>adr/0003</c>'s no-unbounded-magnitude rule applied to a tally;
    /// a counter that wraps is a Citizen with a long history reading as a Citizen with none, which
    /// is the one failure that would look like the mechanism working.
    /// </para>
    /// <para>
    /// <b>It is not the aggregate.</b> <c>EmploymentEngine</c> still counts <em>candidates</em>
    /// refused into the <c>beyond</c> Census flow, and that flow is an instrument. This is state,
    /// and its unit is the occasion — see the column for why the two differ.
    /// </para>
    /// </remarks>
    public void RecordReachFailure(int citizenSlot)
    {
        ushort failures = Citizens.ReachFailures[citizenSlot];

        if (failures < ushort.MaxValue)
        {
            Citizens.ReachFailures[citizenSlot] = (ushort)(failures + 1);
        }
    }

    /// <summary>
    /// Records how a Citizen's journey ended, and resolves the Trip. <b>The one door onto
    /// <see cref="CitizenTable.LastTripFate"/>.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Milestone 6 task 7, and it exists because the answer is unrecoverable a line later.</b>
    /// <c>Movement.TripEngine.Release</c> frees the Trip row immediately after asserting it carries a
    /// Fate, and <c>AdvanceTravellers</c> frees the <b>Traveller</b> — which holds the only
    /// Citizen-to-Trip link there is — earlier in the same pass. So the Fate and the association with
    /// the person who made the journey ceased to exist together, which is <c>02 §9</c>'s
    /// <i>"current or last Trip with its Fate"</i> failing on its second half.
    /// </para>
    /// <para>
    /// <b>The two writes are one method rather than two calls, on <see cref="Employ"/>'s argument.</b>
    /// Resolving a Trip and recording whose journey it was are the same event, and a caller that can
    /// do one without the other will eventually do one without the other. Requiring the Citizen is
    /// what makes that structural: all four of <c>TripEngine</c>'s Fate sites already have one —
    /// <c>Start</c> takes it as its first parameter and <c>AdvanceTravellers</c> reads
    /// <c>TravellerTable.Citizen</c> — so nothing had to be threaded anywhere to satisfy it, and a
    /// fifth site cannot be written without deciding whose journey it was.
    /// </para>
    /// <para>
    /// <b>The Tick is read here rather than passed in</b>, so a caller cannot date a journey wrongly
    /// and there is no argument that could disagree with the clock.
    /// </para>
    /// </remarks>
    /// <param name="citizenSlot">Whose journey it was.</param>
    /// <param name="tripSlot">The Trip that ended.</param>
    /// <param name="fate">How it ended. Never <c>TripFate.InFlight</c> — the table refuses that.</param>
    /// <param name="failingLeg">The Leg that produced the Fate, where there is one.</param>
    internal void ResolveTrip(
        int citizenSlot, int tripSlot, Movement.TripFate fate, int failingLeg = Rows.NoSlot)
    {
        Trips.Resolve(tripSlot, fate, failingLeg);
        RecordTripFate(citizenSlot, fate);
    }

    /// <summary>
    /// Records how a Citizen's journey ended, without touching the Trip.
    /// </summary>
    /// <remarks>
    /// <b>Separate from <see cref="ResolveTrip"/> only because one Fate is recorded where the Trip row
    /// is reached by handle rather than by slot</b> — see its caller in <c>AdvanceTravellers</c>.
    /// </remarks>
    /// <param name="citizenSlot">Whose journey it was.</param>
    /// <param name="fate">How it ended.</param>
    internal void RecordTripFate(int citizenSlot, Movement.TripFate fate)
    {
        Citizens.LastTripFate[citizenSlot] = (byte)fate;

        // Days, not Ticks: CitizenTable.LastTripEndedDay carries why, and it is a memory argument
        // rather than a precision one. FloorDiv because 05 §4's lint 3 bans the raw operator, and the
        // quotient IS the answer here rather than something thrown away.
        long day = IntegerMath.FloorDiv((long)Tick.Raw, Quantities.Ticks.PerDay);

        Citizens.LastTripEndedDay[citizenSlot] =
            day >= ushort.MaxValue ? ushort.MaxValue : (ushort)day;
    }

    /// <summary>
    /// Takes a Citizen's Workplace away, leaving everything else about them alone.
    /// </summary>
    /// <remarks>
    /// <b>The handle is cleared rather than left severed, and that is the difference between this and
    /// demolition.</b> A Building demolished out from under a worker leaves the handle pointing at a
    /// freed row, which reads as <em>the job stopped existing</em> and is the fact. A ceiling lowered
    /// under one leaves the Building standing, so a severed handle would be a lie — the employer is
    /// right there. Clearing it is also what puts the Citizen back in front of the assignment pass,
    /// which is <c>adr/0054</c>'s answer for a demolished dwelling read across.
    /// </remarks>
    public void Dismiss(Handle<Citizen> citizen)
    {
        int slot = Citizens.Rows.Resolve(citizen);

        Unlist(slot);

        // Off the commute roster too, and before the handle is cleared, for DestroyBuilding's reason:
        // the departure buckets are a function of the Workplace, so a Citizen who has stopped having
        // one has stopped having them. Employ's ordering comment applies here as the degenerate case
        // -- unroster, then rewrite -- and it is written the same way round so the two read alike.
        Commutes.Remove(Citizens, slot);
        Citizens.Workplace[slot] = default;
    }

    /// <summary>
    /// Takes <paramref name="citizenSlot"/> off its Workplace's worker list, if it is on one.
    /// </summary>
    /// <remarks>
    /// <b>Silent where the handle does not resolve, and that is the severable case rather than an
    /// error.</b> <see cref="DestroyBuilding"/> unlists its workers before freeing the row, so a
    /// Citizen whose <see cref="CitizenTable.Workplace"/> is dangling is already off every list —
    /// which is the same state <see cref="RebuildDerived"/> produces for it, and the reason the two
    /// agree.
    /// </remarks>
    private void Unlist(int citizenSlot)
    {
        if (Businesses.Rows.TryResolve(Citizens.Workplace[citizenSlot], out int workplaceSlot))
        {
            Workers.Remove(workplaceSlot, citizenSlot);
        }
    }

    /// <summary>
    /// The worker of <paramref name="buildingSlot"/> holding the highest draw, which is the one a
    /// lowered <c>jobs</c> ceiling dismisses next.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Loser"/> on the employment axis, keyed on the Citizen's monotonic id and drawn
    /// against its own tag.</b> Sharing <see cref="PurposeTag.OverflowEviction"/> would tie *who is
    /// sacked* to *who is evicted from their home*, so a patch that lowered both ceilings would turn
    /// the same families out of both — a correlation with no cause in the city and no readout that
    /// could say where it came from.
    /// </remarks>
    private int LosingWorker(int buildingSlot, Ticks now, WorldKey key)
    {
        int worst = Rows.NoSlot;
        ulong highest = 0;

        foreach (int citizen in Workers.Walk(buildingSlot))
        {
            ulong draw = Randomness.Draw(
                key, Citizens.Rows.IdAt(citizen), now, PurposeTag.JobEviction);

            if (worst == Rows.NoSlot || draw > highest)
            {
                worst = citizen;
                highest = draw;
            }
        }

        return worst;
    }

    /// <summary>
    /// The ceiling the Ruleset declares for <paramref name="resource"/> on <paramref name="kind"/>,
    /// or zero where it declares none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Zero rather than <em>leave it alone</em>, because the derivation has to be total.</b> A
    /// derived column is legal only while it is a pure function of saved state; one that skipped rows
    /// it had no declaration for would not survive a save and a load, which is the one thing
    /// <see cref="RebuildDerived"/> exists to prove.
    /// </para>
    /// <para>
    /// <b>The rows it reaches are a derelict Building's</b> — a kind the incoming Ruleset dropped
    /// (<c>02 §4.3</c>). Such a Building runs no Rules, so nothing reads or writes these Bins; zero
    /// says *the Ruleset declares no store of this here*, leaves the stock in place rather than
    /// destroying it (<c>04 §2</c>), and gives the Bin negative space, which refuses a deposit that
    /// cannot arrive anyway. **It does not drain**, unlike <c>adr/0064</c>'s over-full case, because
    /// nothing is left to withdraw from it — dereliction is where the self-healing argument stops, and
    /// it stops harmlessly because the Building is inert by then.
    /// </para>
    /// <para>
    /// <b>A linear walk of a handful.</b> A kind's declared Bins are its whole set and
    /// <c>(kind, Resource)</c> is a key the loader enforces, so the first match is the only match.
    /// </para>
    /// </remarks>
    internal long DeclaredCapacity(byte kind, ResourceId resource)
    {
        if (!Rules.Declares(kind))
        {
            return 0;
        }

        foreach (BinDeclaration declared in Rules.BinsOf(kind))
        {
            if (declared.Resource.Raw == resource.Raw)
            {
                return declared.Capacity.Units;
            }
        }

        return 0;
    }

    /// <summary>
    /// The Bin on <paramref name="buildingSlot"/> storing <paramref name="resource"/>, or
    /// <see cref="Rows.NoSlot"/>.
    /// </summary>
    /// <remarks>
    /// <b>A walk rather than a binary search, and the list is short by construction.</b> A Building's
    /// Bins are its kind's declared set — a handful — so the search is a few sequential comparisons
    /// against a sorted-array lookup's setup cost. The alternative that would earn
    /// <see cref="ResourceMap"/> here is a contiguous block per Building, which buys the search back
    /// at the price of a second allocator and a fragmentation sink under <c>adr/0006</c>.
    /// </remarks>
    public int FindBin(int buildingSlot, ResourceId resource)
    {
        foreach (int bin in BuildingBins.Walk(buildingSlot))
        {
            if (Bins.Resource[bin] == resource)
            {
                return bin;
            }
        }

        return Rows.NoSlot;
    }

    /// <summary>Gives a Building one of the Rules its kind runs, armed to fire in <paramref name="delay"/>.</summary>
    /// <remarks>
    /// ⚠ <b><paramref name="tenant"/> and <paramref name="trader"/> are mutually exclusive</b>, and
    /// nothing here enforces it because the loader already has: a Rule whose <c>local</c> terms
    /// address two owners is refused at load (<c>adr/0141</c>), so no <c>RuleDefinition.Tenancy</c>
    /// can ask for both. ***The discriminant is sound upstream, which is why there is no tag column.***
    /// </remarks>
    public Handle<RuleInstance> CreateRuleInstance(
        Handle<Building> building,
        RuleId rule,
        Ticks now,
        uint delay,
        Handle<Household> tenant = default,
        Handle<Business> trader = default)
    {
        int buildingSlot = Buildings.Rows.Resolve(building);

        Handle<RuleInstance> handle = RuleInstances.Create(building, rule, tenant, trader);
        int slot = RuleInstances.Rows.Resolve(handle);

        BuildingRules.InsertOrdered(buildingSlot, slot);
        Wheel.Arm(slot, now, delay);

        return handle;
    }

    /// <summary>
    /// Adds to a Bin and drains its wait list. <b>The only way a Bin's level ever rises.</b>
    /// </summary>
    /// <remarks>
    /// <b>The write and the drain are one call because <c>05 §9</c> says what happens when they are
    /// two</b>: <em>a Bin written without draining its wait list leaves that Building asleep for ever,
    /// with no error and no timer to rescue it.</em> There is no level column to assign, so there is
    /// no second spelling in which the drain can be forgotten.
    /// </remarks>
    public void Deposit(Handle<Bin> bin, long amount, Ticks tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        int slot = Bins.Rows.Resolve(bin);

        Invariants.Require(
            amount <= Bins.SpaceAt(slot), Invariant.BinLevelIsWithinCapacity, slot, amount);

        Bins.Move(slot, amount);
        Drain(slot, Blocking.Supply, tick);
        RingMarket(slot, tick);
    }

    /// <summary>
    /// Wakes the buyers waiting on a market row, when the Bin just filled is a seller's stock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0139</c>'s one mechanism that could not be read off the build, because it did not
    /// exist</b>: *"a blocked buyer waits on the market row, and a seller's deposit rings it."* A
    /// waiter names <b>one</b> Bin — <c>RuleInstanceTable.WaitingOn</c> is a single handle and
    /// <c>QueueNext</c> a single link — so ***subscribing to N sellers is not expressible and never
    /// will be***, and the market row is therefore the subscription target. Nothing deposits into that
    /// row, so without this the queue on it would never drain.
    /// </para>
    /// <para>
    /// <b>⚠ The budget is THIS seller's level, not the market Bin's and not the arriving delta.</b>
    /// The market Bin holds nothing by construction, so <see cref="Drain(int, Blocking, Ticks)"/>'s
    /// own budget would be zero and wake nobody. The arriving delta is what <c>adr/0063</c> refused —
    /// a consumer short of three, fed by arrivals of one, sleeping for ever behind a filling Bin. What
    /// is left is the level of the seller that just restocked, which is exactly what a buyer woken by
    /// this ring could draw on.
    /// </para>
    /// <para>
    /// <b>⚠ A woken waiter reserves nothing, so several may wake against one seller's stock and
    /// re-fail.</b> <c>adr/0139</c> says so in terms and files it as inherited from <c>adr/0063</c>
    /// rather than created here: the drain's guarantee was always about an instant. It is also why
    /// this rings only the row the depositing seller stands in — another District's buyers cannot
    /// reach this stock at all.
    /// </para>
    /// </remarks>
    private void RingMarket(int binSlot, Ticks tick)
    {
        // ⚠ TWO ARRAY READS BEFORE THE INDEX IS CONSULTED, and they are not a micro-optimisation.
        // Every Deposit in the city arrives here -- a larder filling, a treasury collecting, runoff
        // reaching a Water Body -- and Markets.MarketOf may REBUILD, which walks every Business and
        // every Bin. Only a Business-owned Good Bin can be stock, so asking the Bin its own owner
        // first turns the common case into two loads and keeps the rebuild on the seller's path.
        if (Bins.OwnerKind[binSlot] != BinOwnerKind.Business
            || Rules.IsConserved(Bins.Resource[binSlot]))
        {
            return;
        }

        int row = Markets.MarketOf(this, binSlot);

        if (row == Space.DistrictMarkets.NoRow
            || !Bins.Rows.TryResolve(DistrictPools.Bin[row], out int market))
        {
            return;
        }

        Drain(market, Blocking.Supply, tick, Bins.LevelAt(binSlot));
    }

    /// <summary>
    /// Takes from a Bin and drains the waiters that wanted it emptier.
    /// </summary>
    /// <remarks>
    /// <b>Symmetric with <see cref="Deposit"/>, because <c>adr/0045</c>'s <em>blocking</em> is</b> —
    /// <em>refill if the Bin was short, drain if it was a full output.</em> A withdrawal is what
    /// rescues a Rule whose output Bin was full, and a withdrawal that did not drain would strand
    /// those waiters exactly as a deposit that did not drain strands the others.
    /// </remarks>
    public void Withdraw(Handle<Bin> bin, long amount, Ticks tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        int slot = Bins.Rows.Resolve(bin);

        Invariants.Require(
            amount <= Bins.LevelAt(slot), Invariant.BinLevelIsWithinCapacity, slot, amount);

        Bins.Move(slot, -amount);
        Drain(slot, Blocking.Space, tick);
    }

    /// <summary>
    /// Puts a Rule Instance to sleep on the Bin that blocked it, and in which direction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It does not re-arm, and that is the whole mechanism</b> (<c>02 §4.1</c>): a Rule that fails
    /// subscribes <em>instead of</em> retrying on a timer, so a starved District costs nothing at all
    /// until supply arrives. What wakes it is the mutator that writes the Bin.
    /// </para>
    /// <para>
    /// <b>It records no quantity</b> (<c>adr/0063</c>). <em>Which</em> Bin and <em>why</em> are facts
    /// about this row; <em>how much</em> is a question <see cref="Rules.RuleEngine.Requirement"/>
    /// answers from the Bin and the Ruleset in force at the moment the drain asks it, which is what
    /// keeps a waiter's requirement from being a fact about the Ruleset that has since been reloaded.
    /// </para>
    /// <para>
    /// <b>The caller must already have taken the row off the Wheel, and this cannot check it.</b>
    /// <see cref="Invariant.RuleInstanceIsArmedOrWaiting"/> here catches a double subscribe, which is
    /// <c>O(1)</c>; the row that is still in a Wheel bucket looks identical from the write site,
    /// because the only evidence is a link in a bucket nobody has walked. That half is
    /// <see cref="Invariants.WorldInvariants.RuleInstancesAreQueuedExactlyOnce"/>, at the end of the
    /// run, and the split is <c>02 §10</c>'s tiering rather than a gap — neither half covers the
    /// invariant alone.
    /// </para>
    /// </remarks>
    public void Subscribe(Handle<RuleInstance> instance, Handle<Bin> bin, Blocking blocking)
    {
        if (blocking == Blocking.Nothing)
        {
            throw new ArgumentOutOfRangeException(
                nameof(blocking), blocking, "a subscription must name why it is blocked.");
        }

        int slot = RuleInstances.Rows.Resolve(instance);
        int binSlot = Bins.Rows.Resolve(bin);

        Invariants.Require(
            !RuleInstances.IsWaiting(slot), Invariant.RuleInstanceIsArmedOrWaiting, slot, binSlot);

        RuleInstances.WaitingOn[slot] = bin;
        RuleInstances.Blocked[slot] = blocking;

        Waiters(blocking).Append(binSlot, slot);
    }

    /// <summary>
    /// Demolishes a Building, with its Bins, its Rules, and everybody asleep on its Bins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Occupants are evicted into the Unplaced Pool, with their balances intact</b>
    /// (<c>adr/0054</c>). Destroying them instead would delete their Money, which <c>adr/0024</c>
    /// forbids — the Outside Connection is money's only sink — and would be an unbounded population
    /// sink with no Departure record. It happens first, while the Building is still whole, because
    /// <see cref="Unplace"/> reads each Household's dwelling handle to find the occupant list it is
    /// leaving.
    /// </para>
    /// <para>
    /// <b>The waiters are woken rather than dropped.</b> A Rule Instance asleep on a Bin that no
    /// longer exists is <c>05 §9</c>'s asleep-for-ever reached through demolition instead of through a
    /// missed drain — nothing will ever write that Bin again. Waking them puts each back on the Wheel
    /// to re-evaluate, fail against a Bin that is gone, and take whatever its <c>on_fail</c> chain
    /// offers, which is the reportable outcome rather than the silent one.
    /// </para>
    /// </remarks>
    public void DestroyBuilding(Handle<Building> building, Ticks tick)
    {
        int slot = Buildings.Rows.Resolve(building);

        EmptyPremises(slot, tick);

        // The Car Park, and it goes with the Building because the parking a garage provides stops
        // existing when the garage does. The cars in it are NOT unparked here, and that is deliberate
        // rather than an oversight: CitizenTable.ParkedIn is Reference.Severable, so every holder's
        // handle stops resolving in the same act -- which is what keeps adr/0084's conservation sum
        // balanced across a demolition instead of reporting the leak it exists to catch.
        //
        // ⚠ It is also adr/0084's named second mutation site -- *a car displaced by a bulldozed
        // garage* -- and that ADR says the write-site predicate needs restating before it ships. What
        // is restated here is only the conservation half. Whether a displaced car should re-query a
        // shed, and where, is task 4's, and it is the acquire/release pairing that decides it.
        if (Buildings.HasCarPark(slot))
        {
            int carPark = Buildings.CarParkOf(slot);
            Buildings.DetachCarPark(slot);

            // Before the Free, because unlisting reads the row's own Address to find which Segment's
            // list to walk -- see CarParkResidency.Remove.
            CarParksOnSegments.Remove(CarParks, Roads.Segments, carPark);

            CarParks.Rows.Free(CarParks.Rows.At(carPark));
        }

        // Before the Lot is freed below, because BuildingResidency reads the Lot's position through
        // the Building's Lot handle -- so a Building removed after its Lot has gone would not find
        // the Cell it is listed in and would leave a dangling entry for the next allocation of this
        // slot to be inserted into twice.
        BuildingsInCells.Remove(Buildings, Lots, slot);

        // Before the row is freed, because the Lot handle is read off it.
        if (Lots.Rows.TryResolve(Buildings.Lot[slot], out int lotSlot))
        {
            Lots.Vacate(lotSlot);

            // A Lot with no frontage outlived its Street only because something stood on it
            // (adr/0079). With the Building gone there is nothing to keep it a parcel, so it goes
            // back to being land -- and land with no Street re-parcels if one ever returns.
            //
            // Found by LotLongRunTests rather than reasoned out: re-subdivision runs on a road edit,
            // and a Lot can be vacated at any Tick, so between a demolition and the next edit the
            // world held a vacant Lot with no Street. That is exactly what
            // Invariant.VacantLotHasFrontage forbids, and freeing it here is what makes the
            // invariant true continuously rather than only immediately after an edit.
            if (HasStreets && !Lots.HasFrontage(lotSlot))
            {
                Lots.Rows.Free(Lots.Rows.At(lotSlot));

                // The Lot set shrank, so the zoned draw space is out of date.
                LotsAdmitting.Invalidate();
            }
        }

        Buildings.Rows.Free(building);
    }

    /// <summary>
    /// Empties a Building of everything that lives in it, leaving the structure itself untouched.
    /// </summary>
    /// <remarks>
    /// <b>Shared by <see cref="DestroyBuilding"/> and <see cref="AbandonBuilding"/>, and that sharing
    /// is the point.</b> The two differ only in what happens to the shell afterwards — demolition
    /// frees the row and returns the Lot to vacant, abandonment leaves both standing. Everything
    /// before that point is identical, and stating it twice would be two rules that drift
    /// (<c>05 §4</c>'s reason for one home per predicate).
    /// </remarks>
    private void EmptyPremises(int slot, Ticks tick)
    {
        // Peeked rather than popped, because Unplace removes the Household from this list itself —
        // popping first would leave it unlinking a node that is already off, and the two spellings of
        // "leave the occupant list" would have to agree for ever.
        int occupant = Occupants.PeekFront(slot);
        while (occupant != Rows.NoSlot)
        {
            Unplace(Households.Rows.At(occupant));
            occupant = Occupants.PeekFront(slot);
        }

        // The workers next, and they are unlisted rather than dismissed. Clearing the Workplace
        // handle would be a write to a saved column, so demolition would move the State Hash for a
        // reason that has nothing to do with the demolition -- and the handle is Severable precisely
        // so that a dangling one can say `the job stopped existing`, which is the fact here.
        // Unlisting alone is what makes the write path agree with RebuildDerived, whose TryResolve
        // drops exactly these Citizens.
        //
        // Drained rather than Clear()ed, because the Citizens stay live: IndexList.Clear drops the
        // heads without touching the elements' next links, which is correct only when they are about
        // to be freed or re-linked and neither is true here.
        //
        // ⚠ AND OFF THE COMMUTE ROSTER, which is a second list keyed on the same fact and was missed
        // when the first was written. A Citizen's two departure buckets are computed from their
        // Workplace's Shift band (adr/0101), so a demolished employer strands them in buckets nothing
        // will ever empty: CommuteRoster.Rebuild drops them and the maintained roster does not, which
        // is (derived AND rebuilt) broken. Unlisting from Workers is NOT enough and the reason is
        // exactly what makes the handle Severable -- the write path has to say what the rebuild path
        // would say, at every list keyed on the severed thing rather than at the first one.
        // 🔴 THE WORKER LIST IS NOT DRAINED HERE, and this block used to do exactly that. Since
        // milestone 27 task 7 `Workers` is indexed by BUSINESS slot (adr/0141), so popping it at a
        // BUILDING slot read out of bounds the moment the two tables differed in width -- and every
        // world that demolishes anything threw. The paragraph above is kept because its ARGUMENT is
        // untouched and now belongs one level down: the write path still has to say what the rebuild
        // path would say, at every list keyed on the severed thing.
        //
        // What changed is WHICH fact is severed. Demolishing premises no longer ends a job -- the
        // employer survives the demolition, unpremised, and keeps its workers (adr/0142, adr/0144).
        // What it ends is the JOURNEY, because a Business with no premises has nowhere to be
        // travelled to. So the un-rostering moved into Unpremise, which is where the fact now lives
        // and which the BuildingBusinesses drain below calls once per tenant.

        // The Businesses next, unlisted and NOT destroyed, which is the worker branch's answer on the
        // Occupant axis rather than the Household branch's. A Household is evicted to the Unplaced
        // Pool because there is somewhere for it to go; there is no pool for a Business, and freeing
        // the row would destroy its balance -- money out of the world through a demolition, which is
        // the hole adr/0024 exists to close and which Invariant.MoneyIsConserved would report far from
        // here. So the row survives with its money, holding a severed premises handle that says `the
        // premises stopped existing`.
        //
        // adr/0142, milestone 25 task 5: the orphaned Businesses go into the unpremised pool, where a
        // give-up bound sends them out of the city with their money. ⚠ THIS BLOCK SAID THE
        // DESTINATION WAS UNDESIGNED until 2026-08-23, and it was right on its own terms -- it
        // declined to invent one rather than leaking money to close the loop, and named the leak it
        // was leaving. What changed is that adr/0142 supplied a destination that already existed.
        //
        // Still drained rather than Clear()ed, for the reason the worker branch above states, and now
        // for a second: Unpremise reads Businesses.Building before severing it, so the rows must come
        // off this list one at a time rather than the two heads being dropped underneath them.
        // IndexList.Clear leaves every element's next link intact, and a Business in no list pointing
        // at its old sibling is a (derived AND rebuilt) disagreement no hash can see.
        // ⚠ adr/0148: the trade this KIND came with dies with the premises, and every other tenant
        // is pooled. Fit creates one of the declared trade at construction, so this destroys one --
        // the pairing that keeps the shop count bounded, and the reason Raze exists. See Raze.
        //
        // 🔴 ON THE ORIGIN AND NOT ON THE KIND, since milestone 27 task 10. A kind is not an identity:
        // [founding] draws uniformly over every declared trade, so a Household may found a shop of
        // the very trade a dwelling declares, and matching on kind razed whichever came first in the
        // list. Two defects from that one line -- the founded shop's capital left the city through
        // Raze's money-supply write, and the instantiated shop outlived its premises into the
        // unpremised pool, where nothing ever collected it. Measured on rulesets/levied.toml at
        // 24,576 Ticks: 52 stranded; on minimal.toml and taxed.toml, which found nothing, zero.
        foreach (int came in BuildingBusinesses.Walk(slot))
        {
            if (Buildings.Rows.TryResolve(Businesses.Origin[came], out int origin) && origin == slot)
            {
                Raze(Businesses.Rows.At(came));
                break;
            }
        }

        IndexList premises = BuildingBusinesses;
        int tenant = premises.PopFront(slot);
        while (tenant != Rows.NoSlot)
        {
            Unpremise(Businesses.Rows.At(tenant), tick);
            tenant = premises.PopFront(slot);
        }

        // The Rules next, so that any of them asleep on this Building's own Bins are off those wait
        // lists before the Bins below start waking whoever is left on them.
        int instance = BuildingRules.PopFront(slot);
        while (instance != Rows.NoSlot)
        {
            Unlink(instance);
            RuleInstances.Rows.Free(RuleInstances.Rows.At(instance));
            instance = BuildingRules.PopFront(slot);
        }

        int bin = BuildingBins.PopFront(slot);
        while (bin != Rows.NoSlot)
        {
            WakeAll(bin, tick);
            Bins.Rows.Free(Bins.Rows.At(bin));
            bin = BuildingBins.PopFront(slot);
        }
    }

    /// <summary>
    /// The city abandons a Building: its Occupants leave, its tenants leave, and <b>the shell stays
    /// standing on its Lot</b>.
    /// </summary>
    /// <remarks>
    /// <b><c>02 §5.9</c> contradicts itself about this twelve lines apart and
    /// <c>adr/0091</c> settled which reading stands</b> — <i>"abandonment empties a Building and
    /// leaves it standing on its Lot"</i> — because three other mechanisms need the shell to exist:
    /// the abandonment contagion that raises a neighbour's pressure has no carrier without it,
    /// <c>01 §6</c>'s sustained-detection duration is derived from that contagion, and
    /// <c>adr/0091</c>'s own clearance verb has nothing to act on. <b>The build implemented the other
    /// reading</b> and did so from before that ADR was written.
    /// <para>
    /// ⚠ <b>This is not dereliction and must never be called that</b> (<c>CONTEXT.md</c>:313).
    /// Dereliction is what a Ruleset edit does to a Building and it is recovered by a reload;
    /// abandonment is what the city did to one and a reload must not undo it.
    /// </para>
    /// <para>
    /// The Lot is <b>not</b> vacated and the row is <b>not</b> freed, so nothing here re-parcels the
    /// Lot or disturbs <c>BuildingsInCells</c> — the Building is still standing ground as far as
    /// every reader of those structures is concerned, which is exactly what the contagion term will
    /// need. The Car Park is likewise left attached: the garage still physically stands.
    /// </para>
    /// </remarks>
    public void AbandonBuilding(Handle<Building> building, Ticks tick)
    {
        int slot = Buildings.Rows.Resolve(building);

        EmptyPremises(slot, tick);

        // After the emptying rather than before it, so that a reader of this column during the walk
        // above cannot see a half-emptied Building calling itself abandoned.
        Buildings.AbandonedSince[slot] = tick;
    }

    /// <summary>Which of a Bin's two wait lists a given blocking reason queues on.</summary>
    private IndexList Waiters(Blocking blocking) =>
        blocking == Blocking.Supply ? SupplyWaiters : SpaceWaiters;

    /// <summary>
    /// Wakes waiters from the head while the Bin's own state can still complete them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The budget is what the Bin holds, not what just arrived</b> (<c>adr/0063</c>). It read the
    /// write's delta until then, which meant a requirement coarser than the granularity of supply was
    /// never reached however much accumulated: a consumer short of three, fed by arrivals of one, slept
    /// for ever against a Bin filling to its ceiling behind it. That is
    /// <see cref="Invariant.WaiterIsBlockedByTheBinItNames"/>'s violation — <c>adr/0033</c>'s <em>no
    /// Rule is asleep with all inputs satisfiable</em> — so the old predicate was not a policy this one
    /// replaces but a state the corpus had already declared inadmissible.
    /// </para>
    /// <para>
    /// <b>The requirement is derived per waiter, and that is where this costs something.</b>
    /// <see cref="Rules.RuleEngine.Requirement"/> is a partial <c>Check</c> — the band plus the term
    /// walk for this one Bin — against a stored column that was one field read. The drain stops at the
    /// first uncovered waiter, so the common case is <em>one</em> derivation per Bin write, and
    /// <c>adr/0063</c> does not declare that affordable: the claim <em>it costs less than the Bin write
    /// it accompanies</em> is measurable, unmeasured, and routed to <c>0002</c> §B.
    /// </para>
    /// <para>
    /// <b>The spend-down is retained, and it is what bounds size bias.</b> Waking every subscriber
    /// would let small waiters beat large ones on quantity — not on identity, since
    /// <see cref="Rules.RuleEngine.Evaluate"/> keys Phase 3's order on a draw over
    /// <c>(seed, instance, tick, purpose)</c> and no Building can hold a standing advantage. Deducting
    /// each woken waiter's requirement means a budget of six against waiters needing six, four and two
    /// wakes only the first. Stopping rather than skipping is the other half: skipping an uncovered
    /// waiter to reach a smaller one behind it would starve every large waiter permanently.
    /// </para>
    /// <para>
    /// <b>Servings stay atomic, and that is what preserves throughput rather than a conservatism.</b>
    /// Three consumers each needing six against a supply of twelve: dividing each arrival gives four,
    /// four and four — no firings and twelve units immobilised — where serving the head completely and
    /// rotating gives two firings and immobilises nothing. <c>02 §4.1</c>'s gradient is evenness
    /// <em>over time</em>, never within an arrival. Accumulating toward a threshold is a thing a
    /// Ruleset may author, as an acquisition Rule feeding the consumer's own Bin.
    /// </para>
    /// <para>
    /// <b>It is <c>internal</c> so that a waiter joining a queue can re-run it</b>, which is the one
    /// caller that is not a Bin write. See <see cref="Rules.RuleEngine.Stop"/>: Phase 3 applies intents
    /// in shuffle order, so the deposit that covers a waiter may already have run this Tick, against a
    /// queue the waiter had not yet joined.
    /// </para>
    /// </remarks>
    internal void Drain(int binSlot, Blocking blocking, Ticks tick) =>
        Drain(
            binSlot,
            blocking,
            tick,
            blocking == Blocking.Supply ? Bins.LevelAt(binSlot) : Bins.SpaceAt(binSlot));

    /// <summary>
    /// <see cref="Drain(int, Blocking, Ticks)"/> against a budget the Bin itself does not hold.
    /// </summary>
    /// <remarks>
    /// <b>One caller, and it is the only Bin in the design whose own state does not answer the
    /// question</b> — a District's market row, which is a price and a wake target rather than a store
    /// (<c>adr/0139</c>). See <see cref="RingMarket"/>. Everything else about the walk is unchanged,
    /// including the spend-down, which is what bounds size bias.
    /// </remarks>
    private void Drain(int binSlot, Blocking blocking, Ticks tick, long budget)
    {
        IndexList waiters = Waiters(blocking);

        long remaining = budget;

        while (true)
        {
            int head = waiters.PeekFront(binSlot);

            if (head == Rows.NoSlot)
            {
                return;
            }

            long requirement = RuleEngine.Requirement(this, head, binSlot, blocking);

            if (requirement > remaining)
            {
                return;
            }

            remaining -= requirement;

            waiters.PopFront(binSlot);
            Wake(head, tick);
        }
    }

    /// <summary>
    /// Wakes the premises' sleeping Rules whose <c>apply</c> count is derived, because the Readout
    /// they derive it from has just changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>adr/0063</c>'s principle reaching the one input that is not a Bin.</b> A wait list
    /// wakes on the <em>Bin's</em> state, and that is every reason a Rule's verdict can change —
    /// except one. A Rule whose <c>apply</c> is <c>{ derived = ... }</c> also depends on a Readout,
    /// and ***nothing anywhere was watching it***: the count is recomputed only when the Rule is
    /// evaluated, and a starving Rule subscribes and sleeps rather than re-arming on its rate
    /// (<c>adr/0045</c>). So a Rule could be asleep waiting for supply it no longer needed.
    /// </para>
    /// <para>
    /// 🔴 <b>Found by a test rather than by reading, and it had made milestone 17 task 3 silently
    /// inert.</b> Shedding an Occupant lowers a derived Rule's demand to nothing at zero occupancy —
    /// the whole point of the first threshold — and the Building was condemned anyway, because
    /// <c>upkeep</c> never woke to notice. ***The mechanism was correct and unobservable***, which is
    /// the shape <c>adr/0093</c> warns about.
    /// </para>
    /// <para>
    /// ⚠ <b>Only the SLEEPING ones need this, and that is what makes the fix complete rather than
    /// partial.</b> A Rule that is not starving is armed on the Wheel and re-evaluates on its rate,
    /// picking up the new Readout by itself — so a rise in occupancy needs no wake at all. Only a
    /// fall, on a Rule already asleep, is unreachable by any other path.
    /// </para>
    /// <para>
    /// ⚠ <b>The PREMISES only.</b> A tenant's Rule Instances carry their Household handle and derive
    /// nothing from the Building's occupancy; waking them would re-arm Rules whose inputs did not
    /// move.
    /// </para>
    /// <para>
    /// <b>Safe here by the phase order and not by anything this does</b> — see <see cref="Unlink"/>.
    /// Its caller is <c>ZoneRuleEngine.Shed</c>, which runs in phase 6, after Phase 3 has put every
    /// due row back.
    /// </para>
    /// </remarks>
    internal void WakeDerivedApply(int buildingSlot, Ticks tick)
    {
        foreach (int instance in BuildingRules.Walk(buildingSlot))
        {
            if (RuleInstances.Household[instance] != default)
            {
                continue;
            }

            // ⚠ ASLEEP ON A WAIT LIST, which is NOT the same as starving and the difference threw.
            // StarvedSince stays stamped until the Rule actually fires again, so an instance this
            // method has already woken still reads as starving while being armed on the Wheel --
            // and EventWheel.Arm refuses an instance that is already armed
            // (Invariant.RuleInstanceIsArmedOrWaiting). `Blocked` is the state that answers the
            // question this method is asking: is anything other than a Bin write going to reach it.
            if (RuleInstances.Blocked[instance] == Blocking.Nothing)
            {
                continue;
            }

            if (!Rules.Rule(RuleInstances.Rule[instance]).Apply.IsDerived)
            {
                continue;
            }

            Unlink(instance);
            Wake(instance, tick);
        }
    }

    /// <summary>Empties both of a Bin's wait lists, for a Bin that is about to stop existing.</summary>
    private void WakeAll(int binSlot, Ticks tick)
    {
        WakeAll(SupplyWaiters, binSlot, tick);
        WakeAll(SpaceWaiters, binSlot, tick);
    }

    private void WakeAll(IndexList waiters, int binSlot, Ticks tick)
    {
        int waiter = waiters.PopFront(binSlot);

        while (waiter != Rows.NoSlot)
        {
            Wake(waiter, tick);
            waiter = waiters.PopFront(binSlot);
        }
    }

    /// <summary>
    /// Moves a Rule Instance off a wait list and back onto the Wheel, for the next Tick.
    /// </summary>
    /// <remarks>
    /// <b>The next Tick, not this one.</b> A Bin is written in Phase 3 and a Rule evaluates in Phase 2,
    /// which has already run — so arming for <c>tick + 1</c> is the earliest honest answer. Arming for
    /// this Tick would be a write in Phase 3 that a Phase 2 already past was supposed to have seen.
    /// </remarks>
    private void Wake(int instanceSlot, Ticks tick)
    {
        RuleInstances.Blocked[instanceSlot] = Blocking.Nothing;
        RuleInstances.WaitingOn[instanceSlot] = default;

        Wheel.Arm(instanceSlot, tick, 1);
    }

    /// <summary>
    /// Takes a Rule Instance off whichever of the two structures holds it.
    /// </summary>
    /// <remarks>
    /// <b>Every live scheduled row is in exactly one of {armed, waiting} — at a phase boundary, which
    /// is the domain and not a caveat</b> (<c>adr/0056</c>, as amended). Between Phase 1's
    /// <c>CollectDue</c> and the end of Phase 3 a due row is on <em>neither</em>: it is held in the
    /// Rule engine's own array, with <see cref="Rules.RuleInstanceTable.Blocked"/> still reading
    /// <see cref="Blocking.Nothing"/>. So this method's branch would take the armed path and remove
    /// nothing.
    /// <para>
    /// <b>Both callers are safe, and both are safe by the phase order rather than by anything either
    /// of them does.</b> A Zone Rule demolishes in phase 6, after Phase 3 has put every due row back;
    /// a Ruleset reload refits in phase 0, before Phase 1 has taken any out. Nothing anywhere stated
    /// that until now, and two callers resting on an unstated ordering property is what makes it worth
    /// stating: a third caller between Phases 1 and 3 would free rows the engine is still holding, and
    /// the only thing that would notice is <see cref="Invariant.NoFreedRowIsStillLinked"/>, a whole run
    /// later, about a slot that has since been recycled.
    /// </para>
    /// <para>
    /// <b>The removal's answer is therefore checked rather than discarded.</b>
    /// <see cref="IndexList.Remove"/> returns whether the node was in the list at all, which is the one
    /// signal separating <em>unlinked</em> from <em>was not there</em> — and while the callers hold to
    /// the phase order the two are never the same thing.
    /// </para>
    /// </remarks>
    private void Unlink(int instanceSlot)
    {
        Blocking blocking = RuleInstances.Blocked[instanceSlot];

        if (blocking == Blocking.Nothing)
        {
            bool wasArmed = Wheel.Armed.Remove(
                EventWheel.BucketOf(RuleInstances.NextTick[instanceSlot]), instanceSlot);

            Invariants.Require(
                wasArmed, Invariant.RuleInstanceIsArmedOrWaiting, instanceSlot, -1);

            return;
        }

        if (Bins.Rows.TryResolve(RuleInstances.WaitingOn[instanceSlot], out int binSlot))
        {
            Waiters(blocking).Remove(binSlot, instanceSlot);
        }

        RuleInstances.Blocked[instanceSlot] = Blocking.Nothing;
        RuleInstances.WaitingOn[instanceSlot] = default;
    }

    private static int PerThousand(int citizens, int rows)
    {
        long scaled = IntegerMath.FloorDiv(((long)citizens * rows) + 999, 1000);
        return scaled > int.MaxValue ? int.MaxValue : (int)scaled;
    }
}
