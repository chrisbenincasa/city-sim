using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core;

/// <summary>
/// <c>step(inputs)</c>: one world, one Tick counter, and the eight phases in order.
/// </summary>
/// <remarks>
/// <para>
/// <b>The simulation is a pure function of its inputs</b> (<c>02 §1</c>). It has no concept of
/// wall-clock time, no camera, no renderer and no filesystem, and the host decides when to advance
/// the Tick. That is what makes fast-forward, headless testing and replay free rather than features
/// somebody has to build.
/// </para>
/// <para>
/// <b>This is separate from <see cref="Entities.World"/> because storage and time are separate
/// things.</b> The World is the tables and the cross-table operations that keep them consistent; the
/// Simulation is what advances them. Keeping the Tick counter out of the World also keeps it out of
/// the State Hash's composition, where it does not belong: two worlds identical in every row are
/// identical worlds, and the Tick they were reached on is a property of the run.
/// </para>
/// <para>
/// <b>Which thread runs this is deliberately not decided here.</b> <c>05 §6</c> never says, and
/// <c>adr/0037</c> made it consequential by leaving one live state. Phase 1 does not need the answer —
/// it is single-threaded and headless — and the shape of this class does not foreclose it: nothing
/// here owns a thread, so the recommendation on the table (the simulation owns a thread, and
/// <c>threads=1</c> means it runs on the caller's) remains available.
/// </para>
/// </remarks>
public sealed class Simulation
{
    private readonly World _world;
    private readonly WorldKey _key;
    private readonly RuleEngine _rules;
    private readonly ZoneRuleEngine _zoning;
    private readonly PolicyEngine _policies;
    private readonly PlacementEngine _placement;
    private readonly EmploymentEngine _employment;
    private readonly TripEngine _trips;
    private readonly CommuteEngine _commutes;
    private readonly RulesetCatalogue _rulesets;

    // Reusable search state for every walk this simulation resolves. It is not simulation state and
    // folds nothing: WalkScratch.Begin grows its arrays and bumps a generation stamp rather than
    // clearing, so holding one costs a high-water mark of nodes and saves a whole allocation per Leg.

    private ISaveSink? _saveDue;
    private WorldSnapshot? _snapshot;

    private TickPhase _phase = TickPhase.Commit;
    private ulong _inForce;
    private bool _opened;
    private int _reloads;
    private RulesetDegradation _degradation;

    /// <param name="world">The tables this advances. Not copied; the Simulation does not own it.</param>
    /// <param name="key">The world seed, already folded. The first coordinate of every draw.</param>
    public Simulation(World world, WorldKey key)
        : this(world, key, RulesetCatalogue.None)
    {
    }

    /// <inheritdoc cref="Simulation(World, WorldKey)"/>
    /// <param name="world">The tables this advances. Not copied; the Simulation does not own it.</param>
    /// <param name="key">The world seed, already folded. The first coordinate of every draw.</param>
    /// <param name="rulesets">
    /// Every Ruleset this session references, by content hash, opening one first. Slice 8's reload
    /// resolves a transition out of this and never out of a file.
    /// </param>
    /// <remarks>
    /// <b>Supplied up front because <c>Core</c> cannot turn a hash into Rules</b> — see
    /// <see cref="RulesetCatalogue"/>. A session that never reloads passes
    /// <see cref="RulesetCatalogue.None"/> and behaves exactly as it did before slice 8, which is what
    /// keeps every existing golden baseline meaningful.
    /// </remarks>
    public Simulation(World world, WorldKey key, RulesetCatalogue rulesets)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(rulesets);

        if (rulesets.Count > 0 && !ReferenceEquals(rulesets.Opening, world.Rules))
        {
            // The catalogue's first entry is what the world opened with, and a catalogue that
            // disagreed would make the first reload swap *away* from Rules the city had never been
            // running — a divergence with no symptom, because both Rulesets load and both run.
            throw new ArgumentException(
                "the catalogue's opening Ruleset is not the one this World holds. The first entry "
                + "names the Rules the world was created with; a reload is a transition away from "
                + "them.",
                nameof(rulesets));
        }

        _world = world;
        _key = key;
        _rules = new RuleEngine(world, key);
        _zoning = new ZoneRuleEngine(world, key);
        _policies = new PolicyEngine(world, key);
        _employment = new EmploymentEngine(world, key);

        // No WorldKey: nothing in Phase 4 draws. A Traveller advances when its Leg's arrival Tick has
        // come, which is arithmetic over state the log already determines, so there is no decision here
        // for a purpose_tag to separate. If one ever appears, it needs its own tag (BOR0801-BOR0803).
        _trips = new TripEngine(world);

        // Ahead of the Trip engine until milestone 11 task 6, and moved here because placement now
        // starts the move-in Trip. Construction order is not composition order -- the State Hash
        // folds World._tables, which nothing here touches.
        _placement = new PlacementEngine(world, key, _trips);
        _commutes = new CommuteEngine(world, _trips);
        _rulesets = rulesets;
    }

    /// <summary>The tables this Simulation advances.</summary>
    public World World => _world;

    /// <summary>The Bin Rule interpreter: Phase 1 collects, Phase 2 evaluates, Phase 3 applies.</summary>
    /// <remarks>
    /// <b>Owned here rather than by the <see cref="Entities.World"/>, unlike the invariant registry.</b>
    /// The registry holds claims about a world and a hand-built test world has the same ones; the
    /// engine holds a Tick's worth of scratch, which only means anything inside a Tick. Putting it on
    /// the World would have made the buffers look like state, which is precisely what they must not be.
    /// </remarks>
    public RuleEngine Rules => _rules;

    /// <summary>The Sweep Rule interpreter: Zone Rules trigger, sample and act, all in phase 6.</summary>
    /// <remarks>
    /// <b>A second engine rather than a mode of the first, which is <c>adr/0033</c> honoured in the
    /// code layout.</b> The two families share the word <em>Rule</em> and nothing else structural: one
    /// is woken by the Event Wheel and settled against contention, the other is polled on an interval
    /// and acts where it runs. A single class that branched on family would make moving a mechanism
    /// between them look like a flag, which is exactly what the ADR says it is not.
    /// </remarks>
    public ZoneRuleEngine Zoning => _zoning;

    /// <summary>
    /// <c>02 §5.2</c> step 2: the Unplaced Pool draining into standing vacancy, in phase 6 ahead of
    /// <see cref="Zoning"/>.
    /// </summary>
    /// <remarks>
    /// <b>A third engine rather than a step inside the second, and the reason is not symmetry.</b>
    /// Placement is not a Rule of either family — nothing declares it, no Ruleset chooses whether a
    /// city houses people, and it acts on Households where a Zone Rule acts on Lots. Folding it into
    /// <see cref="ZoneRuleEngine"/> would put a mechanism the Ruleset does not author inside the class
    /// that exists to interpret what the Ruleset does author, and would re-create the coupling
    /// <c>adr/0069</c> exists to break: construction and placement doing each other's jobs.
    /// </remarks>
    public PlacementEngine Placement => _placement;

    /// <summary>
    /// <c>adr/0081</c>: a Citizen with no Workplace taking one near home, in phase 6 behind
    /// <see cref="Placement"/>.
    /// </summary>
    /// <remarks>
    /// <b>A fourth engine on <see cref="Placement"/>'s reasoning exactly</b>, and behind it on
    /// <see cref="Placement"/>'s ordering argument read one step further: a commute is anchored at a
    /// dwelling, so somebody housed this Tick can look for work on the same Tick rather than waiting a
    /// whole interval for an address they already have. The reverse order would make the delay between
    /// being housed and being employed an artefact of phase ordering, which is the class of coupling
    /// <c>02 §1.1</c> calls the determinism contract and asks to be stated rather than inherited.
    /// </remarks>
    public EmploymentEngine Employment => _employment;

    /// <summary>
    /// The Sweep family's second member: Policies sweep a population and move money, in phase 6
    /// <em>ahead of</em> <see cref="Placement"/>.
    /// </summary>
    /// <remarks>
    /// <b>A fifth engine, and the first that runs in front of the other three rather than behind
    /// them.</b> Placement, employment and the Zone Rules each read the world the pass before it
    /// left; a Policy <em>changes what an actor holds</em>, so it belongs at the head of the phase
    /// for the reason phase 5 precedes phase 6 — growth this Tick sees the diffusion this Tick, and a
    /// Household paid this Tick is judged on what it holds now. Nothing downstream reads a balance
    /// yet, which is why the ordering is stated here rather than discovered later.
    /// </remarks>
    public PolicyEngine Policies => _policies;

    /// <summary>Tick phase 4, and the Trip Fate counters the Census drains.</summary>
    public TripEngine Trips => _trips;

    /// <summary>The world seed, as <see cref="Randomness.Draw"/>'s first coordinate.</summary>
    public WorldKey Key => _key;

    /// <summary>
    /// The Tick about to run. Starts at zero and advances once per <see cref="Step"/>.
    /// </summary>
    /// <remarks>
    /// An unsigned counter, per <c>CONTEXT.md</c>. At the reference rate of 16 Ticks/s a
    /// <see cref="ulong"/> outlasts any session by a margin with no useful name, so nothing in the
    /// core checks it for overflow.
    /// <para>
    /// <b>The world holds it, and this is a view of it.</b> It was a private field here, which meant a
    /// second Simulation over the same world reported a Tick of zero and every invariant that took a
    /// Tick from its caller could be told the wrong one — see
    /// <see cref="Entities.ClockTable"/> for the three mechanisms that paid for that before it moved.
    /// A Simulation is the loop; the Tick is state.
    /// </para>
    /// </remarks>
    public Ticks Tick => _world.Tick;

    /// <summary>The phase last entered. For the crash artifact, which reports where a panic landed.</summary>
    public TickPhase Phase => _phase;

    /// <summary>
    /// Whether to prove, every Tick, that <see cref="TickPhase.Decide"/> wrote nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>On by default, and a runtime switch rather than a build configuration.</b> The obvious
    /// implementation is <c>#if DEBUG</c>, and it is backwards for the same reason <c>02 §10</c> gives
    /// about the invariant tiers: the runs that surface this class of bug are headless balance runs,
    /// millions of Ticks long, in <b>release</b>. A guard that compiles out of the build where the bug
    /// appears is a guard aimed at the wrong build.
    /// </para>
    /// <para>
    /// <b>What it costs is why it is a switch at all.</b> The check folds every column of every table
    /// twice per Tick, so it is <c>O(world)</c> against a phase that is meant to be <c>O(woken)</c> —
    /// affordable for a correctness run and not for a long one. Turn it off for the 100,000-Tick test
    /// and leave it on everywhere else.
    /// </para>
    /// </remarks>
    public bool VerifyDecideWritesNothing { get; set; } = true;

    /// <summary>
    /// Advances the world by one Tick, running all eight phases in order.
    /// </summary>
    /// <remarks>
    /// <b>The ordering is the determinism contract</b> (<c>02 §1.1</c>), which is why the calls are
    /// written out straight rather than driven from a table that something could reorder at runtime.
    /// </remarks>
    public void Step(in TickInput input)
    {
        Ticks tick = _world.Tick;

        Reload(input, tick);
        ApplyInput(input, tick);
        Wake(tick);
        Decide(tick);
        Settle(tick);
        Move(tick);
        Layers(tick);
        Growth(tick);
        Commit(tick);

        _world.Advance();

        // The save, and it is after Advance rather than at the end of Commit for a reason measured
        // rather than argued -- see TakeSaveIfDue. Advance is the clock increment, and the clock has
        // been saved state since adr/0058.
        TakeSaveIfDue();
    }

    /// <summary>How many times the Ruleset in force has changed since this Simulation started.</summary>
    /// <remarks>
    /// <b>A count, for <c>RulesetInForce.Refusals</c>'s reason turned the other way up.</b> That one
    /// climbs when a designer is fighting the file format; this one climbs when a designer is
    /// *tuning*, which is <c>adr/0015</c>'s success condition rather than its failure one. It is also
    /// what a crash artifact needs to say how many Rulesets a session was played against.
    /// </remarks>
    public int Reloads => _reloads;

    /// <summary>The content hash of the Ruleset in force, or 0 before the first Tick.</summary>
    public ulong RulesetInForce => _inForce;

    /// <summary>
    /// What the most recent reload cost the city: Buildings derelicted, Bins dropped, Rules re-armed.
    /// </summary>
    /// <remarks>
    /// <b>The logged warning <c>02 §4.3</c> and <c>adr/0015</c> both ask for, in the only currency
    /// <c>Core</c> may use</b> (<c>adr/0002</c>). It is the <em>latest</em> transition, and it is a
    /// different question from the one <see cref="Entities.World.RulesetTrail"/> answers rather than a
    /// weaker version of it: this is what the shell warns about now, that is what a diagnosis three
    /// patches later reads. The trail is world state because <c>05 §7</c> puts it in the save; this is
    /// a Simulation's, because a warning is about the run.
    /// </remarks>
    public RulesetDegradation LastReload => _degradation;

    /// <summary>
    /// The top of Phase 0: put a different Ruleset in force, if this Tick names one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Before the commands, not after, and a Tick has exactly one Ruleset.</b> That is what
    /// <c>InputLog.RulesetHashAt(tick)</c>'s signature already promises — it answers per Tick, not per
    /// command — so the commands in the Tick that reloads are applied under the **new** Rules. The
    /// alternative reading, swap after, would make a Tick's meaning depend on where in the Tick you
    /// asked.
    /// </para>
    /// <para>
    /// <b>There is no new verb, and <c>Command</c> was not widened.</b> A reload is a *transition*
    /// rather than an event: <c>TickInput.RulesetHash</c> is populated every Tick and was read by
    /// nothing until now, and <c>Command</c> is 12 bytes and could not have carried a hash anyway. The
    /// door argument is satisfied rather than weakened — <c>TickInput</c> **is** Phase 0's input, so
    /// nothing reaches in from outside a Tick and a replay reproduces the transition by construction.
    /// </para>
    /// <para>
    /// <b>The first Tick establishes the hash in force rather than swapping.</b> A World is
    /// constructed with its opening Ruleset and the log's first Tick names it; treating that as a
    /// transition would make every session that ever ran reload on Tick 0.
    /// </para>
    /// </remarks>
    private void Reload(in TickInput input, Ticks tick)
    {
        _phase = TickPhase.Input;

        if (!_opened)
        {
            _inForce = input.RulesetHash;
            _opened = true;
            return;
        }

        if (input.RulesetHash == _inForce)
        {
            return;
        }

        if (!_rulesets.TryResolve(input.RulesetHash, out Ruleset replacement))
        {
            // adr/0015's polarity: its revisit trigger names *silently ignoring* as the failure mode,
            // so a transition this session cannot resolve says so rather than running on regardless.
            throw new InvalidOperationException(
                $"Tick {tick.Raw} names Ruleset 0x{input.RulesetHash:X16} and this session was given "
                + $"{_rulesets.Count} Ruleset(s), none of which is it. A reload is resolved out of "
                + "the catalogue supplied to the Simulation, because Core cannot read a file.");
        }

        // The degradations live in the World because they are a pass over its rows, and the residual
        // refusal lives with them for the same reason -- what a family change breaks is the stock in
        // the Bins, which is state rather than shape. Nothing is caught here: a refused reload leaves
        // the previous Ruleset live by having changed nothing, and the throw is the diagnosis.
        _degradation = _world.Adopt(replacement, input.RulesetHash, tick, _key);
        _inForce = input.RulesetHash;
        _reloads++;
    }

    /// <summary>Phase 0 — apply the player's commands.</summary>
    /// <remarks>
    /// <b>Serial, and the only door into the simulation.</b> Everything that affects simulation state
    /// enters here, which is what makes the Input Log a complete description of a session. A mechanism
    /// that reaches into the world from outside a Tick is not merely untidy — it is a state change no
    /// replay can reproduce and no State Hash divergence can explain.
    /// </remarks>
    private void ApplyInput(in TickInput input, Ticks tick)
    {
        _phase = TickPhase.Input;

        foreach (Command command in input.Commands)
        {
            Apply(command, tick);
        }
    }

    /// <summary>Applies one command. The verb table of <c>01 §2</c>, as far as it is built.</summary>
    /// <remarks>
    /// <b>An unknown verb throws rather than being skipped.</b> Skipping it would produce a run that
    /// diverges from the log that describes it while reporting success, which is the one outcome this
    /// slice exists to make impossible.
    /// </remarks>
    private void Apply(Command command, Ticks tick)
    {
        switch (command.Kind)
        {
            case CommandKind.Zone:
                // 02 §2.2: Lots are **generated, not painted**. Until 5a-bis this line created exactly
                // one Lot at the command's coordinates, and said so -- there was no Street network to
                // carve against, so painting a *region* would have stood in for more of 5a than
                // painting one did, and every Lot it invented would have been one the real subdivider
                // would have refused.
                //
                // Now the verb zones the **block** the named Tile falls in, and the subdivider carves
                // it against its own four faces. A block with no Street on any face yields **no Lots
                // at all**, which is that section's third rule and the whole of what makes a bad
                // street layout punish the player mechanically rather than through a penalty number.
                LotSubdivider.SubdivideAt(_world, command.East, command.North, command.Zone);
                break;

            case CommandKind.Connect:
                // 01 §2's fifth verb, declared since slice 5 and thrown until now. adr/0077 settles
                // what it is: one Street Segment on the lattice edge leaving the named intersection.
                ApplyConnect(command);
                break;

            case CommandKind.Populate:
                // Spike S0's verb, and the only one here that is an instrument rather than a player's.
                // It is applied like any other because that is the point of it: a population that
                // arrived any other way would be outside the log that claims to describe the session.
                SyntheticCity.PopulateInto(_world, _key, tick);
                break;

            case CommandKind.Trip:
                // adr/0080's verb. Phase 4 is built ahead of anything that generates a Trip, because
                // every generator the corpus names is unmilestoned -- so a Trip enters through the log
                // exactly as a population does, and for the same reason: what arrives any other way is
                // a state change no replay reproduces.
                ApplyTrip(command, tick);
                break;

            case CommandKind.Arrive:
                // adr/0128's door. The gate ships at 11 and what decides to arrive ships at 16, so
                // the only thing that admits anybody is a command -- Populate's position, taken for
                // Populate's reason.
                ApplyArrive(command, tick);
                break;

            case CommandKind.None:
            case CommandKind.Service:
            case CommandKind.Govern:
            default:
                throw new InvalidOperationException(
                    $"command kind {(ushort)command.Kind} is declared but not applied in this slice.");
        }
    }

    /// <summary>
    /// Lays or bulldozes one Street, then re-parcels what the edit changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the first thing in the project that edits the Road Graph in a running world</b>, and
    /// therefore the first thing that has ever driven <c>adr/0012</c>'s per-Segment Epoch by anything
    /// a player did. Until now the Epoch was exercised by unit tests and by nothing else, because the
    /// generator runs at world creation.
    /// </para>
    /// <para>
    /// <b>Re-subdivision runs only when the graph actually changed</b>, which is what makes a repeated
    /// <c>connect</c> free. A log replays a player's clicks, and a player clicking a Tile that already
    /// has a road on it is an ordinary event rather than an error to die on.
    /// </para>
    /// <para>
    /// <b>Anything that is not a Street is refused by name.</b> An Arterial is a spline with many
    /// control points and is not one command at all; a Junction piece needs a piece library
    /// (<c>adr/0014</c>: <em>"content with three faces each"</em>). Both are deferred with a named
    /// successor rather than silently missing, which is the distinction <c>adr/0070</c> exists to
    /// keep — only a <em>refused</em> absence is evidence a later sitting may reason from.
    /// </para>
    /// </remarks>
    private void ApplyConnect(Command command)
    {
        ConnectPayload payload = ConnectPayload.Decode(command.Zone);

        if (payload.Kind != RoadKind.Street)
        {
            throw new InvalidOperationException(
                $"connect names road kind {(int)payload.Kind}, and only Street is applied. "
                + "adr/0077 defers Arterials and Junction pieces by name: an Arterial is a spline "
                + "with many control points and is not one command, and a Junction piece needs the "
                + "authored library adr/0014 calls content. Neither is missing by oversight.");
        }

        int block = _world.Roads.Streets.BlockTiles;

        if (block <= 0)
        {
            throw new InvalidOperationException(
                "connect names a Street lattice this world does not have. The spacing comes from "
                + "the Ruleset's [roads] block_tiles, and a Ruleset that declares no [roads] is a "
                + "world with no road network to edit.");
        }

        // Snapped down to the lattice, which is what adr/0014's "Streets snap to the grid" means once
        // the graph is nodes at intersections rather than Tiles: the player names a place and the
        // edit lands on the edge leaving the intersection at or below it.
        int column = IntegerMath.FloorDiv(command.East.Raw, block);
        int row = IntegerMath.FloorDiv(command.North.Raw, block);

        bool changed = payload.Action == ConnectAction.Lay
            ? _world.Roads.LayStreet(column, row, payload.Axis)
            : _world.Roads.BulldozeStreet(column, row, payload.Axis);

        if (!changed)
        {
            return;
        }

        // The graph edit has already rebuilt the Street lattice; frontage follows it, and only then
        // can the subdivider tell which Lots have lost their Street and which faces have gained one.
        _world.Frontage.Rebuild(_world.Lots, _world.Roads.Streets);

        // The Parking Shed's supply index is keyed on Segment slots, so a graph edit invalidates it
        // for the same reason it invalidates frontage -- and it must be rebuilt BEFORE the subdivider
        // runs, because resubdividing condemns Buildings and a condemnation unlists their Car Parks.
        // ⚠ Bulldozing frees the Segment row, which severs every Car Park Address naming it, and
        // CarParkResidency.Remove finds a Car Park's list *through* that Address -- so an unlist
        // attempted after the sever silently does nothing and leaves the row listed as it is freed.
        // The recycled slot is then inserted into the same list twice, which self-links it. This is
        // where BuildingsInCells' shape stops being a guide: a Cell never goes away, and a Segment
        // does.
        _world.CarParksOnSegments.Rebuild(_world.CarParks, _world.Roads.Segments);

        LotSubdivider.Resubdivide(_world);
    }

    /// <summary>
    /// <c>adr/0080</c>'s verb: put one Citizen on the road between two Buildings the operator names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every refusal here is a refusal rather than a degradation</b>, on the <c>Scope.Pool</c> and
    /// <c>--zones</c> precedent. A command naming an empty block could plausibly be answered with
    /// <em>the nearest Building instead</em>, and that answer would make an operator's typo indis­tin­guish­able
    /// from a Trip they meant — which is the whole class of defect a commanded Trip exists to avoid.
    /// </para>
    /// <para>
    /// <b>What is <em>not</em> a refusal is a Building with no front door.</b> <c>adr/0079</c> settles
    /// that a Building outlives its frontage and an Address that has none is <em>a hole the Trip model
    /// reports</em> — so that is a Trip whose Fate is <see cref="TripFate.NoRouteFound"/>, and it is the
    /// case this verb exists to be able to produce on demand.
    /// </para>
    /// <para>
    /// <b>The crossing cost and the Commute Budget are read from the <c>[trips]</c> table</b>
    /// (5b-bis task 3), which is what turns this from a Trip that is always made into a Trip that can
    /// be refused. A Ruleset declaring no <c>[trips]</c> is refused outright rather than costed at
    /// zero: zero is a legitimate crossing cost — <c>adr/0074</c>'s rung 1, what the corpus had by
    /// omission — so a zero standing in for <em>nobody has authored one</em> is the placeholder
    /// session F named, indistinguishable from a decision.
    /// </para>
    /// <para>
    /// <b>A city with no Commute Budget refuses nothing for its length</b>, which is what an omitted
    /// <c>commute_budget_minutes</c> states. That is the city whose cost distribution this milestone
    /// has to measure before the number can be chosen, so
    /// <see cref="TripFate.ExceededCommuteBudget"/> is reachable here and unreached until a Ruleset
    /// states one.
    /// </para>
    /// </remarks>
    private void ApplyTrip(Command command, Ticks tick)
    {
        TripPayload payload = TripPayload.Decode(command.Zone);

        TripRuleset trips = _world.Rules.Trips;

        if (!trips.Runs)
        {
            throw new InvalidOperationException(
                "trip is commanded against a Ruleset that declares no [trips] table, so what a "
                + "crossing costs and where the Commute Budget falls are both unauthored. The verb "
                + "refuses rather than costing the journey at zero, because zero is a legitimate "
                + "crossing cost -- a city where the shop opposite is the shop next door -- and a "
                + "placeholder inside the range of legitimate answers cannot announce itself.");
        }

        int block = _world.Roads.Streets.BlockTiles;

        if (block <= 0)
        {
            throw new InvalidOperationException(
                "trip names a Street lattice this world does not have. A Building's Access Point is "
                + "derived through its Lot, a Lot is carved out of a block, and the spacing comes from "
                + "the Ruleset's [roads] block_tiles -- so a Ruleset declaring no [roads] is a world "
                + "in which nobody has an address to travel between.");
        }

        int column = IntegerMath.FloorDiv(command.East.Raw, block);
        int row = IntegerMath.FloorDiv(command.North.Raw, block);

        int origin = OccupiedBuildingIn(column, row);
        int destination = OccupiedBuildingIn(column + payload.BlocksEast, row + payload.BlocksNorth);

        if (origin < 0 || destination < 0)
        {
            throw new InvalidOperationException(
                $"trip from block ({column}, {row}) to block ({column + payload.BlocksEast}, "
                + $"{row + payload.BlocksNorth}) names a block with no occupied Building in it. The "
                + "verb refuses rather than substituting a nearby one, because a substituted endpoint "
                + "makes a mistyped command indistinguishable from the Trip somebody meant.");
        }

        if (origin == destination)
        {
            throw new InvalidOperationException(
                "trip names one Building as both endpoints. A Trip to where you already are is not a "
                + "degenerate Trip, it is a command with a wrong payload -- the block delta is zero, or "
                + "both blocks resolved to the same Building because only one is occupied.");
        }

        int citizen = FirstResidentOf(origin);

        if (citizen < 0)
        {
            throw new InvalidOperationException(
                "trip names an origin Building with no Citizen in it. A Traveller is a cursor over a "
                + "Citizen's journey (adr/0075) and there is nobody here to be one.");
        }

        // The Citizen's own mode, not a walk. adr/0080 demotes this command to a test affordance
        // rather than the only door, and an affordance that travels differently from the city it is
        // probing is an instrument measuring itself.
        TravelMode mode = _world.ModeOf(citizen);

        _trips.Start(citizen, origin, destination, mode, TripPurpose.Commanded, tick);
    }

    /// <summary>
    /// The first occupied Building in a block of the Street lattice, or <c>-1</c> if it holds none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This is an <c>O(Lots)</c> scan, and it is the call site of the query milestone 5b-bis has
    /// to build.</b> There is no index from a place to the Buildings near it anywhere in
    /// <c>Borough.Core</c> — <see cref="Space.CellResidency"/> indexes Map Layer cells rather than
    /// entities, and <see cref="Space.Frontage"/> maps a Lot to its Segment in one direction only. The
    /// §A sitting found that all three candidate Trip generators need one and no document had named it
    /// (<c>adr/0081</c>).
    /// </para>
    /// <para>
    /// <b>It is affordable here for a reason that will not survive a generator.</b> This runs in Phase 0,
    /// once per command a human wrote, so the scan is paid per keystroke rather than per Tick. A
    /// generator runs per Household per occasion, and the same scan there is <c>O(Lots x Households)</c>
    /// — which is why the index is 5b-bis's <em>first</em> task rather than a detail inside its
    /// generator. <b>Left deliberately un-optimised</b>: a local index built here would remove the
    /// pressure that gets the shared one built, which is <c>adr/0073</c>'s finding exactly.
    /// </para>
    /// <para>
    /// <b>Slot order is a deterministic total order and the scan takes the first match</b>, which is the
    /// discipline <see cref="World"/>'s own rebuild loops use. It is not an identity — nothing stores
    /// the slot — so <c>BOR0206</c> is not in play.
    /// </para>
    /// </remarks>
    private int OccupiedBuildingIn(int column, int row)
    {
        int block = _world.Roads.Streets.BlockTiles;
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || lots.IsVacant(slot))
            {
                continue;
            }

            if (IntegerMath.FloorDiv(lots.East[slot].Raw, block) == column
                && IntegerMath.FloorDiv(lots.North[slot].Raw, block) == row)
            {
                return lots.BuildingOn(slot);
            }
        }

        return -1;
    }

    /// <summary>
    /// Admits up to the requested number of Households through the gate at the named Tile.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A Tile with no gate on it throws, and a gate that is full does not.</b> The two failures
    /// are different in kind: naming the wrong place is a command with a wrong payload, and
    /// <c>ApplyTrip</c>'s refusals are written against exactly that — ***a substituted endpoint makes
    /// a mistyped command indistinguishable from the one somebody meant***. A gate admitting fewer
    /// than were asked for is <c>[[building]] arrivals_per_day</c> binding, which is the mechanism
    /// working and the thing <c>plans/0002</c> §D1 wants to be able to read.
    /// </para>
    /// <para>
    /// <b>The Tile is matched against the Lot exactly rather than against the block it falls in</b>,
    /// which is where this parts company from <see cref="OccupiedBuildingIn"/>. On the shipped
    /// lattice <b>one block carries two edges</b> — <c>SyntheticCity.CarveEdgeBlock</c> puts the west
    /// and south gates in the block at the origin — so *the gate in this block* names two Buildings
    /// standing in two different Hinterlands, and picking either would be inventing the answer to
    /// which market the arrivals came from.
    /// </para>
    /// <para>
    /// <b>A zero count is a command that does nothing, and it is not refused.</b> The loop runs no
    /// iterations. That is deliberate on <c>ApplyConnect</c>'s terms — a log replays what happened,
    /// and a verb that dies on a degenerate payload makes a replay less faithful than the session it
    /// describes.
    /// </para>
    /// </remarks>
    private void ApplyArrive(Command command, Ticks tick)
    {
        ArrivePayload payload = ArrivePayload.Decode(command.Zone);

        int gate = GateOn(command.East, command.North);

        if (gate < 0)
        {
            throw new InvalidOperationException(
                $"arrive names Tile ({command.East.Raw}, {command.North.Raw}), where no Outside "
                + "Connection stands. The verb refuses rather than admitting anybody through the "
                + "nearest gate, because the edge a Household entered by selects its Hinterland "
                + "(adr/0088) -- so a substituted gate does not misplace an arrival, it changes "
                + "which market it came from.");
        }

        Handle<Building> handle = _world.Buildings.Rows.At(gate);

        for (int i = 0; i < payload.Households; i++)
        {
            // Stops on the first refusal rather than trying the rest. The only refusal reachable
            // here is the daily ceiling -- the gate resolved a line above -- and a ceiling that has
            // bound once binds for the remainder of the Day.
            if (!_world.TryArrive(handle, payload.LifeStage, payload.Citizens, tick, out _))
            {
                break;
            }
        }
    }

    /// <summary>
    /// The Outside Connection standing on the Lot at exactly this Tile, or <c>-1</c>.
    /// </summary>
    /// <remarks>
    /// <b>Exact rather than nearest, and gate-kinded rather than merely occupied.</b> Both narrowings
    /// are load-bearing: the origin block carries two gates on the shipped lattice, and an ordinary
    /// dwelling standing at the named Tile is a different Building from the one the command meant.
    /// </remarks>
    private int GateOn(Tiles east, Tiles north)
    {
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot)
                || lots.IsVacant(slot)
                || lots.East[slot].Raw != east.Raw
                || lots.North[slot].Raw != north.Raw)
            {
                continue;
            }

            int building = lots.BuildingOn(slot);

            if (building >= 0 && _world.IsOutsideConnection(_world.Buildings.Kind[building]))
            {
                return building;
            }
        }

        return -1;
    }

    /// <summary>
    /// The first Citizen living in a Building, or <c>-1</c> if nobody does.
    /// </summary>
    /// <remarks>
    /// Two intrusive lists deep — a Building's Households, then a Household's members — and taking the
    /// head of each, because both are kept in slot order by <c>InsertOrdered</c> and the head is
    /// therefore a stable choice rather than an arbitrary one.
    /// </remarks>
    private int FirstResidentOf(int building)
    {
        int household = _world.Occupants.PeekFront(building);

        return household < 0 ? -1 : _world.Members.PeekFront(household);
    }

    /// <summary>Phase 1 — drain the Event Wheel bucket for this Tick.</summary>
    /// <remarks>
    /// <b>Serial, and the only phase that takes rows off the Wheel.</b> Slice 7's wheel carries Rule
    /// Instances; slice 9 generalises it to everything else that sleeps, and every Citizen already
    /// carries the <c>next_event_tick</c> it will be bucketed by, declared in slice 4.
    /// </remarks>
    private void Wake(Ticks tick)
    {
        _phase = TickPhase.Wake;

        _rules.CollectDue(tick);
    }

    /// <summary>
    /// Phase 2 — evaluate Rules and Needs against the Past, emitting intents and writing nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Writing nothing is the load-bearing property</b> (<c>adr/0037</c>): it is what permits every
    /// entity table to be single-buffered, so the Past is <em>the state as of the start of this
    /// Tick</em> rather than a second copy of the world. The guard below is that claim checked instead
    /// of trusted — see <see cref="VerifyDecideWritesNothing"/>.
    /// </para>
    /// <para>
    /// <b>The intents it will emit are not table state</b>, so they are outside what the guard folds.
    /// That is the correct boundary: an intent is a proposal that Phase 3 may reject, and a phase that
    /// could not write its own proposals could not propose anything.
    /// </para>
    /// </remarks>
    private void Decide(Ticks tick)
    {
        _phase = TickPhase.Decide;

        if (!VerifyDecideWritesNothing)
        {
            DecideCore(tick);
            return;
        }

        ulong before = FoldEverything();
        DecideCore(tick);
        ulong after = FoldEverything();

        if (before != after)
        {
            throw new InvalidOperationException(
                $"Decide wrote to the world on Tick {tick.Raw}. Phase 2 is read-only (adr/0037): "
                + "every entity table is single-buffered because of it.");
        }
    }

    /// <summary>The read-only half of Phase 2: evaluate every Rule the Wheel handed us.</summary>
    private void DecideCore(Ticks tick) => _rules.Evaluate(tick);

    /// <summary>Phase 3 — apply intents in shuffle order, re-checking atomicity.</summary>
    /// <remarks>
    /// Serial and sorted. Contested outcomes are settled by a counter-based shuffle rather than by
    /// arrival or by entity id (<c>02 §8</c> rule 5), because ordering by id is <em>biased</em> — the
    /// same Building would win every contested draw for the life of the city.
    /// </remarks>
    private void Settle(Ticks tick)
    {
        _phase = TickPhase.Settle;

        _rules.Apply(tick);
    }

    /// <summary>Phase 4 — Lanes advance Vehicles; Statistical trips check arrival.</summary>
    /// <remarks>
    /// Permitted parallel, and one of <c>adr/0037</c>'s two double-buffered tables when it exists,
    /// because it is a parallel phase that both reads and writes. Empty until Phase 2 of the roadmap.
    /// </remarks>
    private void Move(Ticks tick)
    {
        _phase = TickPhase.Move;

        // Generation before advance, and the ordering is what makes a sub-Tick walk arrive on the Tick
        // it departed rather than a Tick later. adr/0071: travel time is sub-Tick, so a walk across the
        // street genuinely costs less than one integration step, and a phase that advanced first would
        // hold such a Trip in flight for a whole Tick for no reason in the city.
        _commutes.Generate(tick);

        // Serially, though the phase permits parallel. Phases.Runs states that permission as an upper
        // bound rather than an instruction, and plans/0021 task 5 says to take it as one: a parallel
        // Phase 4 is one of adr/0037's two double-buffered tables, and neither the buffering nor the
        // measurement that would justify it exists.
        _trips.Advance(tick);
    }

    /// <summary>Phase 5 — Map Layer diffusion for whatever is scheduled this Tick.</summary>
    /// <remarks>
    /// <para>
    /// Permitted parallel, and <c>adr/0037</c>'s other double-buffered table — the first one in the
    /// project to declare <c>Buffering.TwoCopies</c>. This build runs it serially, which
    /// <c>Phases.Runs</c> states: permission is an upper bound.
    /// </para>
    /// <para>
    /// <b>It costs nothing on the overwhelming majority of Ticks, by construction.</b> The schedule is
    /// staggered — pollution on <c>tick % 64 == 0</c>, land value every 256 offset by 16 — so 63 Ticks
    /// in 64 do no convolution at all, and the two Layers never land on the same Tick. That is
    /// <c>05 §9</c>'s third multiplier: a spike every 64 Ticks is a visible stutter, and the same work
    /// spread over those 64 Ticks is not.
    /// </para>
    /// <para>
    /// <b>The cadence is hash-bearing, which makes it the designer's number and not the profiler's</b>
    /// (<c>adr/0044</c>). It decides when a source becomes visible to a Rule that reads the Cell, so
    /// two worlds differing only in the period produce two hash traces — <c>05 §4</c>'s test, run
    /// rather than argued, because under <c>adr/0043</c> the claim was <em>measurable</em> and the
    /// machine that settles it is this one. It stays ordinary hot-reloadable Ruleset data, which is
    /// why the schedule arrives as a constructor argument of the <c>World</c> rather than a constant
    /// here: a number embedded in an <c>if</c> has no name, and a number with no name is one nobody
    /// runs the hash rule against.
    /// </para>
    /// </remarks>
    private void Layers(Ticks tick)
    {
        _phase = TickPhase.Layers;

        _world.Layers.Step(tick, _world.Roads, _world.Rules.Terrain);
    }

    /// <summary>Phase 6 — Zone Rules sample Lots; Buildings with accumulated failure decline.</summary>
    /// <remarks>
    /// <para>
    /// Serial, and the whole of a Sweep Rule's Tick. <b>That it is one phase rather than three is the
    /// difference <c>adr/0033</c> is about</b>: a Bin Rule's proposal survives phase 2 only to be
    /// re-checked and possibly refused in phase 3, whereas a Zone Rule's effect exists the instant it
    /// acts. Nothing here proposes, so nothing here can be outbid.
    /// </para>
    /// <para>
    /// <b>It runs after phase 5 and that ordering is a decision.</b> A Zone Rule's predicates read
    /// Map Layer values, so growth this Tick sees the diffusion this Tick — a Building placed on a
    /// Cell whose pollution rose in phase 5 is judged against the risen figure and not the stale one.
    /// Reversing the two would make growth lag the environment by a whole Tick on the 1-in-64 Ticks a
    /// Layer moves, which is a difference no readout could explain.
    /// </para>
    /// </remarks>
    private void Growth(Ticks tick)
    {
        _phase = TickPhase.Growth;

        // adr/0069, and the ORDER is the decision rather than the presence. 02 §1.1 calls the phase
        // ordering the determinism contract, and this line is inside a phase rather than beside it:
        // placement drains the Pool into standing vacancy first, so the Pool a Zone Rule then reads is
        // the RESIDUAL -- the Households the existing stock could not house. That is what makes the
        // create predicate a statement about vacancy rather than about population, and it is what
        // replaces the self-limiting property construction used to supply by housing one itself.
        // Ahead of everything else in the phase, and the ORDER is the decision. A Policy moves what
        // an actor holds, so the three passes behind it respond to the city it just changed rather
        // than to last Tick's -- phase 5 -> phase 6's own argument, one level in. Nothing downstream
        // reads a balance today, which is exactly why this is the cheap moment to fix it.
        _policies.Sweep(tick);

        _placement.Place(tick);

        // adr/0081, and behind placement rather than in front of it: a commute is anchored at a
        // dwelling, so a Household housed a line above can look for work on the same Tick. Ahead of
        // the Zone Rules for a second reason of its own -- a Building condemned this Tick should not
        // acquire a worker first, and taking a job in a doomed Building is a churn nothing reports.
        _employment.Assign(tick);

        _zoning.Sweep(tick);

        // adr/0134's re-evaluation, LAST in the phase and after the Zone Rules, because the field it
        // reads is a count of Buildings and this is the line after which that count is settled for the
        // Tick. Placing it earlier would derive the Districts of the city as it was before this Tick's
        // growth, which is phase 5 -> phase 6's own argument arriving one phase further on.
        //
        // IN PHASE 6 AND NOT PHASE 5, though phase 5 is where the other cadenced field update lives.
        // A Map Layer diffuses a field that phase 6 then READS; this derives a structure FROM what
        // phase 6 writes. Same shape, opposite direction, and putting it beside the Layers would make
        // every District lag its own city by a Tick on the Ticks that matter. adr/0134 is explicit that
        // this cadence is not [layers]'.
        if (_world.Rules.Districts.RevisitsOn(tick))
        {
            _world.EvaluateDistricts();
        }

        // adr/0135's Day-boundary reprice, and LAST because it must price the Pools the Tick actually
        // ends with. The line above can open a Pool, destroy one, or hand a dying District's stock to
        // its heir -- and a price computed before that would be a price for a level that changed
        // afterwards. FitDistrictPools' own argument, one line further on.
        //
        // NO WHEEL, and no dependency on milestone 18. "Recomputed each Day" is a modulo over
        // Ticks.PerDay, which this build already takes in two other places; 18's wheel exists so that
        // MANY Day countdowns can share a structure, and one recompute per Good needs none of it.
        // adr/0135 records that the dependency was inferred from a cadence's units rather than read
        // off a symbol.
        if (_world.Rules.Market.RepricesOn(tick))
        {
            _world.RepriceDistrictPools();
        }
    }

    /// <summary>Phase 7 — schedule next events, re-evaluate Stress, emit the State Hash if due.</summary>
    /// <remarks>
    /// <b>This phase no longer swaps Past and Future</b>, which <c>02 §1.1</c>'s table still says it
    /// does. <c>adr/0037</c> deleted the full-world double buffer in favour of one live state, so
    /// there is nothing to swap; the Past is a property of the phase ordering rather than a copy.
    /// Emitting the hash is the caller's business in slice 5 — see <c>Replay</c> — because <em>if
    /// due</em> is a cadence the host chooses.
    /// </remarks>
    private void Commit(Ticks tick)
    {
        _phase = TickPhase.Commit;

        // 02 §10's staggered tier. Last in the Tick because it asserts about a settled world: run
        // before Growth and it would report a city mid-edit, which is a check that fires on correct
        // code and is therefore a check somebody eventually deletes.
        _world.Invariants.RunStaggered(_world);
    }

    /// <summary>
    /// Asks for a save at the end of this Tick. The one door a host has to a save.
    /// </summary>
    /// <param name="destination">
    /// Where the finished bytes go. Written once, synchronously, before <see cref="Step"/> returns.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>The cadence is the host's and the <em>instant</em> is not</b> (<c>adr/0087</c>). An autosave
    /// interval changes no state and enters no hash, so it is a setting rather than a world constant;
    /// where in the Tick the copy is taken is neither. A copy taken mid-Tick captures some tables
    /// before Settle and some after, which is <b>a world that never existed</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>The copy is taken after <see cref="Entities.World.Advance"/> and not at the end of phase 7,
    /// against <c>adr/0087</c>'s wording, and the difference is the clock.</b> That ADR's reasoning
    /// about phase 7 is sound and untouched — it is serial, and both double-buffered tables have
    /// settled, because <c>MapLayers</c> swaps inside the Layers phase. What it does not account for is
    /// that <c>Advance</c> runs <em>after</em> Commit and increments the Tick, which has been <b>saved
    /// state</b> since <c>adr/0058</c>. A copy taken before it records the Tick just finished, so
    /// reloading it re-runs that Tick: one duplicated Tick, no error, and a hash that diverges from the
    /// run it was meant to continue. ***"The end of phase 7" and "the end of the Tick" are different
    /// instants, and the difference is one increment of saved state.*** Found by the hash disagreeing;
    /// <c>adr/0058</c> shipped before <c>adr/0087</c> and the phrase reads as though they are the same
    /// moment.
    /// </para>
    /// <para>
    /// <b>⚠ It is called from inside <see cref="Step"/> rather than after it, and that is the seam.</b>
    /// <c>05 §6</c>'s threading policy is session R's and decides nothing a save needs, so this
    /// milestone writes synchronously (<c>plans/0030</c> D4) — but the boundary a thread would wrap is
    /// drawn now: <see cref="WorldSnapshot"/> is filled on the simulation thread, at ~10 ms once per
    /// autosave, and everything after that moves. Calling this from outside a Tick would put the copy at
    /// a moment nothing guarantees is a boundary.
    /// </para>
    /// <para>
    /// ⚠ <b>The hash is on the movable side, which is task 10's whole result</b> (<c>adr/0112</c>).
    /// <c>SaveHash</c> folds the copy rather than the world, so the ninth header field costs this thread
    /// nothing — where folding the live world would have cost <b>32.47 ms</b> at 1M, on top of the copy.
    /// </para>
    /// <para>
    /// <b>One save may be outstanding.</b> Asking twice before a Tick runs replaces the destination
    /// rather than queueing, because two saves of the same instant are one save written twice.
    /// </para>
    /// </remarks>
    public void SaveAtEndOfTick(ISaveSink destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        _saveDue = destination;
    }

    /// <summary>Whether a save has been asked for and not yet taken.</summary>
    public bool SaveIsDue => _saveDue is not null;

    private void TakeSaveIfDue()
    {
        if (_saveDue is null)
        {
            return;
        }

        ISaveSink destination = _saveDue;
        _saveDue = null;

        // Allocated on the first save rather than with the Simulation: a session that never saves must
        // not carry 131.33 MiB of buffer for the option.
        _snapshot ??= new WorldSnapshot();

        // The copy, the hash and the write. Only the copy has to happen on this thread -- ~10 ms at 1M,
        // once per autosave, serial and after every phase; what moved against adr/0087's wording is that
        // it is after the clock increment rather than before it. The hash is folded from the copy rather
        // than from the world (adr/0112), so it is on the far side of the seam with the write and the
        // simulation thread pays nothing for a save that verifies itself on load.
        SaveFile.Write(_world, _inForce, _snapshot, destination);
    }

    /// <summary>
    /// Runs <c>02 §10</c>'s end-of-run tier: the expensive whole-world walks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Called once, after the last Tick, by whoever owns the run.</b> It is <c>O(world)</c> and
    /// affordable precisely because there is one of it per run however long the run was — which is
    /// <c>adr/0033</c>'s shape, quoted by <c>02 §10</c>: <em>unaffordable per Tick and trivial at the
    /// end of a headless run.</em>
    /// </para>
    /// <para>
    /// <b>It is not called from <see cref="Step"/>, and that is the whole point of the tier.</b> A
    /// check that ran every Tick would have to be cheap, and these are the ones that are not.
    /// </para>
    /// </remarks>
    public void CheckEndOfRun() => _world.Invariants.RunEndOfRun(_world);

    /// <summary>
    /// Folds every column of every table — derived ones included — for the Decide guard.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately not <see cref="Entities.World.HashState"/>.</b> The State Hash folds saved
    /// state only, by declaration, so a Decide that wrote to a derived column would pass a check built
    /// on it. That is exactly the hole slice 4 found in the other direction, where a derived list's
    /// ordering escaped the hash; the same blind spot, read from the other side.
    /// </remarks>
    private ulong FoldEverything()
    {
        ulong hash = 0;

        // TablesAPhaseCanWrite and not Tables, and the difference is the guard's whole cost. A table
        // nothing writes at all cannot be evidence about what Decide wrote, and folding terrain's
        // 262,144 rows twice a Tick made a Tick 138x itself. What stops that being a silent hole is
        // WorldInvariants.TerrainIsWhatItsWorldKeyGenerates, at the end-of-run tier. See World.
        foreach (Rows table in _world.TablesAPhaseCanWrite)
        {
            table.FoldAll(ref hash);
        }

        return hash;
    }
}
