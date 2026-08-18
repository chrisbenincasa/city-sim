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
    private const ulong HashSeed = 0x426F_726F_7567_6802UL;

    private readonly Rows[] _tables;

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
        Households = new HouseholdTable(PerThousand(citizens, 360), Buildings);
        Citizens = new CitizenTable(citizens, Households, Buildings);
        Layers = new MapLayers(rules.Layers);
        Roads = new RoadGraph(rules.Roads);

        // Sized for a city in trouble rather than a healthy one: the Pool is empty when everybody is
        // housed, and the table's job is to absorb a District emptying without reallocating mid-Tick.
        UnplacedPool = new UnplacedTable(PerThousand(citizens, 36), Households);

        // ~3 Bins and ~3 Rules on each of ~150 Buildings per 1,000 Citizens. Both multipliers are
        // capacity hints rather than bounds — the tables grow — but they are numbers nobody has
        // justified, so plans/0002 carries them as unratified until a real Ruleset supplies the shape.
        Bins = new BinTable(PerThousand(citizens, 450), Buildings);
        RuleInstances = new RuleInstanceTable(PerThousand(citizens, 450), Buildings, Bins);
        Clock = new ClockTable();
        RulesetTrail = new RulesetTrailTable();
        CondemnationTrail = new CondemnationTrailTable(Lots.Rows);

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
        ];

        WorldInvariants.RegisterAll(Invariants);
    }

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
        Commutes.Rebuild(Citizens, Buildings, rules, Key);

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
    /// <summary>Adds a Household to a Building, linking it into the Building's occupant list.</summary>
    public Handle<Household> CreateHousehold(Handle<Building> dwelling, byte lifeStage)
    {
        int buildingSlot = Buildings.Rows.Resolve(dwelling);

        Handle<Household> handle = Households.Rows.Allocate();
        int slot = Households.Rows.Resolve(handle);

        Households.Dwelling[slot] = dwelling;
        Households.LifeStage[slot] = lifeStage;

        Invariants.Require(
            !Lists(Occupants, buildingSlot, slot),
            Invariant.HouseholdIsNotAlreadyInThisBuilding,
            slot,
            buildingSlot);

        Occupants.InsertOrdered(buildingSlot, slot);

        return handle;
    }

    /// <summary>
    /// Moves a housed Household into the Unplaced Pool, keeping its Money and Savings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eviction is free, and that is <c>adr/0054</c>'s finding rather than a convenience.</b> This
    /// touches the dwelling handle and the occupant list. It does not touch <c>Money</c> or
    /// <c>Savings</c>, so <em>"a Household keeps what it owns when the city stops housing it"</em>
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

        Occupants.Remove(buildingSlot, slot);
        Households.Dwelling[slot] = default;

        // The write-site half of the Pool's density claim. The table needs the allocator to hand back
        // exactly the next position, which is true because the free list is LIFO and Leave only ever
        // frees the last slot — an implementation detail of Rows that nothing in Rows promises. Left
        // to the end-of-run walk, a change there would surface as a city that had been quietly
        // building less than its Ruleset said for the length of a run.
        Invariants.Require(
            UnplacedPool.Join(Households, household) == UnplacedPool.Count - 1,
            Invariant.ThePoolAppendsInOrder,
            slot);
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
    }

    /// <summary>Adds a Citizen to a Household, linking it into the Household's member list.</summary>
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
        Commutes.Add(Citizens, Buildings, Rules, Key, slot);

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

    /// <summary>Retires a Citizen, unlinking it from its Household first.</summary>
    public void DestroyCitizen(Handle<Citizen> citizen)
    {
        int slot = Citizens.Rows.Resolve(citizen);

        // Before the row is freed, because both phases are derived from state the row carries.
        Commutes.Remove(Citizens, slot);

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

        int member = Members.PopFront(slot);
        while (member != Rows.NoSlot)
        {
            Unlist(member);
            Citizens.Rows.Free(Citizens.Rows.At(member));
            member = Members.PopFront(slot);
        }

        if (Buildings.Rows.TryResolve(Households.Dwelling[slot], out int buildingSlot))
        {
            Occupants.Remove(buildingSlot, slot);
        }

        Households.Rows.Free(household);
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
        Buildings.WorkerHead.Span.Clear();
        Buildings.WorkerTail.Span.Clear();
        Citizens.WorkerNext.Span.Clear();
        Citizens.CommuteNext.Span.Clear();
        Buildings.BinHead.Span.Clear();
        Buildings.BinTail.Span.Clear();
        Buildings.RuleHead.Span.Clear();
        Buildings.RuleTail.Span.Clear();
        Households.DwellingNext.Span.Clear();
        Households.PoolSlot.Span.Clear();
        Households.MemberHead.Span.Clear();
        Households.MemberTail.Span.Clear();
        Citizens.MemberNext.Span.Clear();
        Bins.BinNext.Span.Clear();
        RuleInstances.RuleNext.Span.Clear();
        Lots.BuildingSlot.Span.Clear();
        Lots.FrontageSlot.Span.Clear();
        Lots.FrontageOffset.Span.Clear();
        Bins.Capacity.Span.Clear();

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
                && Buildings.Rows.TryResolve(Citizens.Workplace[slot], out int workplaceSlot))
            {
                workers.InsertOrdered(workplaceSlot, slot);
            }
        }

        // The Bin and Rule Instance lists are derived; the wait lists threaded through the same rows
        // are not, and are untouched here. That asymmetry is the point of the two declarations: which
        // Bins a Building has follows from the Bins' own owner column, and the order a queue was
        // joined in follows from nothing.
        IndexList buildingBins = BuildingBins;
        for (int slot = 0; slot < Bins.Rows.SlotCount; slot++)
        {
            if (Bins.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(Bins.Owner[slot], out int buildingSlot))
            {
                buildingBins.InsertOrdered(buildingSlot, slot);
            }
        }

        RebuildCapacities();

        // After the Lot reverse index above, because it reads a Building's Lot handle -- and it
        // clears its own arrays and CellNext rather than being cleared with the block at the top,
        // because its head and tail are not columns and the block above is a list of columns.
        BuildingsInCells.Rebuild(Buildings, Lots);

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
        Commutes.Rebuild(Citizens, Buildings, Rules, Key);
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
        new(Buildings.WorkerHead, Buildings.WorkerTail, Citizens.WorkerNext);

    /// <summary>The Bins on each Building.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList BuildingBins => new(Buildings.BinHead, Buildings.BinTail, Bins.BinNext);

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
    /// <c>adr/0070</c> forbids. The two diverge when something makes them: milestone 8's parking, and
    /// <c>03 §6.6</c>'s freight.
    /// </para>
    /// <para>
    /// <b>This is never a fallback from an exhausted Parking Shed.</b> Milestone 8's rule, written
    /// now because the shape is easier to forbid than to remove: a full car park must not cost less
    /// than an empty one, so a Shed with no room <em>widens</em> its search rather than resolving to
    /// the Building's own kerb at zero cost.
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
    /// <see cref="VehicleAccessPoint"/> explains why they are equal and what would separate them —
    /// milestone 8's parking, and <c>03 §6.6</c>'s freight. A caller that reached for
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

    /// <summary>Gives a Building a Bin, per its kind's declaration in the Ruleset.</summary>
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

        Handle<Building> building = Buildings.Create(Lots, lot, kind);

        BuildingsInCells.Add(Buildings, Lots, Buildings.Rows.Resolve(building));

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
            if (FindBin(buildingSlot, bin.Resource) == Rows.NoSlot)
            {
                CreateBin(building, bin.Resource);
            }
        }

        int armed = 0;

        foreach (RuleId rule in Rules.RulesOf(kind))
        {
            CreateRuleInstance(building, rule, now, ArmingStagger(building, rule, now, key));
            armed++;
        }

        return armed;
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
    private uint ArmingStagger(Handle<Building> building, RuleId rule, Ticks now, WorldKey key)
    {
        uint rate = Rules.Rule(rule).Rate;

        // The Rule as well as the Building, so that two Rules on one Building do not share an offset
        // and arrive together every time — which is the same bucket spike, one Building wide. The
        // Building's contribution is its monotonic never-reused id and not its slot, for the reason
        // the State Hash folds the same thing: a recycled slot would make a demolished Building's
        // replacement inherit its schedule.
        ulong id = Buildings.Rows.IdAt(Buildings.Rows.Resolve(building));
        ulong entity = Randomness.Mix(id ^ ((ulong)rule.Raw << 32));
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

            byte kind = Buildings.Rows.TryResolve(Bins.Owner[slot], out int buildingSlot)
                ? Buildings.Kind[buildingSlot]
                : (byte)0;

            Bins.SetCapacity(slot, DeclaredCapacity(kind, Bins.Resource[slot]));
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
    /// Whether <paramref name="buildingSlot"/> could take one more Household.
    /// </summary>
    /// <remarks>
    /// <b>The predicate, where <see cref="Invariant.BuildingHasRoomForTheHousehold"/> is the guard.</b>
    /// Placement asks this of every candidate it samples and moves on when the answer is no, which is
    /// an ordinary outcome and not a fault; the guard exists for a caller that placed without asking.
    /// Keeping the two apart is what stops a full city filling the invariant log.
    /// </remarks>
    public bool HasRoom(int buildingSlot) =>
        TryDeclaredOccupancy(Buildings.Kind[buildingSlot], out int occupants)
        && Occupants.Length(buildingSlot) < occupants;

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
                while (Occupants.Length(slot) > allowed)
                {
                    Unplace(Households.Rows.At(Loser(slot, now, key)));
                }
            }

            // Asked separately rather than inside the branch above, because the two ceilings have
            // independent derelict cases: a kind is either declared or it is not, and both readings
            // of that come from the same two-compare predicate, but a Building that houses nobody
            // and employs a hundred is an ordinary kind rather than a corner.
            if (TryDeclaredJobs(kind, out int posts))
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
        if (!Rules.Declares(kind))
        {
            jobs = 0;
            return false;
        }

        jobs = Rules.Kind(kind).Jobs;
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
    public bool HasJob(int buildingSlot) =>
        TryDeclaredJobs(Buildings.Kind[buildingSlot], out int jobs)
        && Workers.Length(buildingSlot) < jobs;

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
    public void Employ(Handle<Citizen> citizen, Handle<Building> workplace, Ticks plannedCommute)
    {
        int slot = Citizens.Rows.Resolve(citizen);
        int buildingSlot = Buildings.Rows.Resolve(workplace);

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
        Commutes.Add(Citizens, Buildings, Rules, Key, slot);
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
        if (Buildings.Rows.TryResolve(Citizens.Workplace[citizenSlot], out int workplaceSlot))
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
    public Handle<RuleInstance> CreateRuleInstance(
        Handle<Building> building, RuleId rule, Ticks now, uint delay)
    {
        int buildingSlot = Buildings.Rows.Resolve(building);

        Handle<RuleInstance> handle = RuleInstances.Create(building, rule);
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
    /// <b>The Occupants are evicted into the Unplaced Pool, with their Money and Savings intact</b>
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
        IndexList workers = Workers;
        int worker = workers.PopFront(slot);
        while (worker != Rows.NoSlot)
        {
            Commutes.Remove(Citizens, worker);
            worker = workers.PopFront(slot);
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
            }
        }

        Buildings.Rows.Free(building);
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
    /// </remarks>
    private void Drain(int binSlot, Blocking blocking, Ticks tick)
    {
        IndexList waiters = Waiters(blocking);

        long remaining = blocking == Blocking.Supply
            ? Bins.LevelAt(binSlot)
            : Bins.SpaceAt(binSlot);

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

    /// <summary>Takes a Rule Instance off whichever one list it is on, leaving it on neither.</summary>
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
