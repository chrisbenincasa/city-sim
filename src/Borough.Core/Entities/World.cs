namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Invariants;
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
    private const ulong HashSeed = 0x426F_726F_7567_6801UL;

    private readonly Rows[] _tables;

    /// <param name="citizens">Initial Citizen capacity. Every other table is sized from it.</param>
    /// <remarks>
    /// <b>Sizing is a derivation, not a constant.</b> Every row count S4 task 2 derived is linear in
    /// population, so the ratios are stated per 1,000 Citizens and stay correct at 250k or at 2M —
    /// 360 Households, ~150 Buildings, ~225 Lots. 1M is a floor rather than a cap.
    /// </remarks>
    public World(int citizens)
        : this(citizens, LayerRuleset.Default)
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
        : this(citizens, layers, Ruleset.Empty)
    {
    }

    /// <inheritdoc cref="World(int, LayerRuleset)"/>
    /// <param name="citizens">Initial Citizen capacity. Every other entity table is sized from it.</param>
    /// <param name="layers">The Map Layer cadence and rates.</param>
    /// <param name="rules">
    /// The Bin Rules, already validated (<c>adr/0048</c>). <b>Not simulation state and not folded into
    /// the State Hash</b> — what names a Ruleset in the hash is its content hash, carried in the Input
    /// Log, because two runs against different Rules are two different simulations.
    /// </param>
    public World(int citizens, LayerRuleset layers, Ruleset rules)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(citizens);
        ArgumentNullException.ThrowIfNull(rules);

        Rules = rules;

        Lots = new LotTable(PerThousand(citizens, 225));
        Buildings = new BuildingTable(PerThousand(citizens, 150), Lots);
        Households = new HouseholdTable(PerThousand(citizens, 360), Buildings);
        Citizens = new CitizenTable(citizens, Households, Buildings);
        Layers = new MapLayers(layers);

        // Sized for a city in trouble rather than a healthy one: the Pool is empty when everybody is
        // housed, and the table's job is to absorb a District emptying without reallocating mid-Tick.
        UnplacedPool = new UnplacedTable(PerThousand(citizens, 36), Households);

        // ~3 Bins and ~3 Rules on each of ~150 Buildings per 1,000 Citizens. Both multipliers are
        // capacity hints rather than bounds — the tables grow — but they are numbers nobody has
        // justified, so plans/0002 carries them as unratified until a real Ruleset supplies the shape.
        Bins = new BinTable(PerThousand(citizens, 450), Buildings);
        RuleInstances = new RuleInstanceTable(PerThousand(citizens, 450), Buildings, Bins);
        Wheel = new EventWheel(RuleInstances);

        // Declaration order, which is hash composition order. The Rule engine's three tables go last
        // because they arrived last; the order is arbitrary but it is not free to change, so it is
        // stated here and moving it is a re-baseline rather than a tidy-up.
        _tables = [
            Lots.Rows, Buildings.Rows, Households.Rows, Citizens.Rows, Layers.Cells.Rows,
            Bins.Rows, RuleInstances.Rows, Wheel.Buckets.Rows, UnplacedPool.Rows,
        ];

        Invariants = new InvariantRegistry();
        WorldInvariants.RegisterAll(Invariants);
    }

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

    /// <summary>The Bins, and their wait lists.</summary>
    public BinTable Bins { get; }

    /// <summary>One row per (Building, Bin Rule) — armed, or asleep on a Bin.</summary>
    public RuleInstanceTable RuleInstances { get; }

    /// <summary>Slice 7's minimal Event Wheel: a bucket per Tick, an arming, and a drain.</summary>
    public EventWheel Wheel { get; }

    /// <summary>The Households seeking housing, and the only demand signal this design has.</summary>
    /// <remarks>
    /// <b>Named <c>UnplacedPool</c> in full because <c>CONTEXT</c> holds two Pools and they are
    /// unrelated.</b> A District Pool is where Goods sit; the Unplaced Pool is where people wait.
    /// Shortening either to <c>Pool</c> in code is how the two would eventually be confused by
    /// somebody reading one call site.
    /// </remarks>
    public UnplacedTable UnplacedPool { get; }

    /// <summary>The Bin Rules this world runs. Ids and integers; no string reaches here.</summary>
    /// <remarks>
    /// <b>A constructor argument rather than a load, for <see cref="MapLayers.Ruleset"/>'s reason.</b>
    /// The core cannot read a file (<c>02 §1</c>) and cannot name the parser (<c>adr/0048</c>), so a
    /// Ruleset arrives already validated or not at all. Slice 8 makes it swappable at a phase boundary;
    /// this slice loads one at world creation and leaves it.
    /// </remarks>
    public Ruleset Rules { get; }

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

        if (Households.Rows.TryResolve(Citizens.HouseholdOf[slot], out int householdSlot))
        {
            Members.Remove(householdSlot, slot);
        }

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

        Buildings.OccupantHead.Span.Clear();
        Buildings.OccupantTail.Span.Clear();
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

        IndexList buildingRules = BuildingRules;
        for (int slot = 0; slot < RuleInstances.Rows.SlotCount; slot++)
        {
            if (RuleInstances.Rows.IsLive(slot)
                && Buildings.Rows.TryResolve(RuleInstances.Building[slot], out int buildingSlot))
            {
                buildingRules.InsertOrdered(buildingSlot, slot);
            }
        }
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

    /// <summary>The Bins on each Building.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList BuildingBins => new(Buildings.BinHead, Buildings.BinTail, Bins.BinNext);

    /// <summary>The Rule Instances each Building runs.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList BuildingRules =>
        new(Buildings.RuleHead, Buildings.RuleTail, RuleInstances.RuleNext);

    /// <summary>The Rule Instances asleep on each Bin because it was <em>short</em>.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList LevelWaiters => new(Bins.LevelHead, Bins.LevelTail, RuleInstances.QueueNext);

    /// <summary>The Rule Instances asleep on each Bin because it was <em>full</em>.</summary>
    /// <inheritdoc cref="Occupants"/>
    public IndexList HeadroomWaiters => new(Bins.HeadroomHead, Bins.HeadroomTail, RuleInstances.QueueNext);

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
    public Handle<Building> CreateBuilding(Handle<Lot> lot, byte kind, Ticks now, WorldKey key)
    {
        int lotSlot = Lots.Rows.Resolve(lot);

        // 02 §2.2's "a Lot is either vacant or holds exactly one Building", at the write site, which
        // is where 02 §10 puts the checks that are O(1). The whole-world half is
        // Invariant.LotHoldsExactlyOneBuilding.
        Invariants.Require(Lots.IsVacant(lotSlot), Invariant.LotIsNotAlreadyBuiltOn, lotSlot);

        Handle<Building> building = Buildings.Create(Lots, lot, kind);

        // A kind the Ruleset does not declare gets no Bins and no Rules, and that is a real state
        // rather than a swallowed error: 02 §4.3 already names it, saying a reload marks Buildings
        // whose kind no longer exists **derelict rather than deleted**. There is no derelict flag yet
        // — it arrives with hot reload in slice 8 — so today the situation is unnamed rather than
        // wrong, and the commonest instance of it is a world running on Ruleset.Empty, which is every
        // figure this project has recorded so far.
        if (!Rules.Declares(kind))
        {
            return building;
        }

        foreach (BinDeclaration bin in Rules.BinsOf(kind))
        {
            CreateBin(building, bin.Resource, bin.Capacity);
        }

        foreach (RuleId rule in Rules.RulesOf(kind))
        {
            CreateRuleInstance(building, rule, now, ArmingStagger(building, rule, now, key));
        }

        return building;
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
    /// second — <see cref="EventWheel.Arm"/>'s refusal names slice 9's overflow list, which is the
    /// correct diagnosis and the one worth surfacing at world creation rather than a rate later.
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

    public Handle<Bin> CreateBin(Handle<Building> owner, ResourceId resource, BinCapacity capacity)
    {
        int buildingSlot = Buildings.Rows.Resolve(owner);

        Invariants.Require(
            FindBin(buildingSlot, resource) == Rows.NoSlot,
            Invariant.BuildingHasOneBinPerResource,
            buildingSlot,
            resource.Raw);

        Handle<Bin> handle = Bins.Create(owner, resource, capacity);

        BuildingBins.InsertOrdered(buildingSlot, Bins.Rows.Resolve(handle));

        return handle;
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
    public void Deposit(Handle<Bin> bin, int amount, Ticks tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        int slot = Bins.Rows.Resolve(bin);

        Invariants.Require(
            amount <= Bins.HeadroomAt(slot), Invariant.BinLevelIsWithinCapacity, slot, amount);

        Bins.Move(slot, amount);
        Drain(slot, Blocking.Level, amount, tick);
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
    public void Withdraw(Handle<Bin> bin, int amount, Ticks tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        int slot = Bins.Rows.Resolve(bin);

        Invariants.Require(
            amount <= Bins.LevelAt(slot), Invariant.BinLevelIsWithinCapacity, slot, amount);

        Bins.Move(slot, -amount);
        Drain(slot, Blocking.Headroom, amount, tick);
    }

    /// <summary>
    /// Puts a Rule Instance to sleep on the Bin that blocked it, carrying what it was short of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It does not re-arm, and that is the whole mechanism</b> (<c>02 §4.1</c>): a Rule that fails
    /// subscribes <em>instead of</em> retrying on a timer, so a starved District costs nothing at all
    /// until supply arrives. What wakes it is the mutator that writes the Bin.
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
    public void Subscribe(
        Handle<RuleInstance> instance, Handle<Bin> bin, Blocking blocking, int shortfall)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(shortfall);

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
        RuleInstances.Shortfall[slot] = shortfall;

        Waiters(blocking).Append(binSlot, slot);
    }

    /// <summary>
    /// Demolishes a Building, with its Bins, its Rules, and everybody asleep on its Bins.
    /// </summary>
    /// <remarks>
    /// <b>The waiters are woken rather than dropped.</b> A Rule Instance asleep on a Bin that no
    /// longer exists is <c>05 §9</c>'s asleep-for-ever reached through demolition instead of through a
    /// missed drain — nothing will ever write that Bin again. Waking them puts each back on the Wheel
    /// to re-evaluate, fail against a Bin that is gone, and take whatever its <c>on_fail</c> chain
    /// offers, which is the reportable outcome rather than the silent one.
    /// </remarks>
    public void DestroyBuilding(Handle<Building> building, Ticks tick)
    {
        int slot = Buildings.Rows.Resolve(building);

        // The Rules first, so that any of them asleep on this Building's own Bins are off those wait
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

        // Before the row is freed, because the Lot handle is read off it.
        if (Lots.Rows.TryResolve(Buildings.Lot[slot], out int lotSlot))
        {
            Lots.Vacate(lotSlot);
        }

        Buildings.Rows.Free(building);
    }

    /// <summary>Which of a Bin's two wait lists a given blocking reason queues on.</summary>
    private IndexList Waiters(Blocking blocking) =>
        blocking == Blocking.Level ? LevelWaiters : HeadroomWaiters;

    /// <summary>
    /// Wakes waiters from the head while the arriving quantity still covers their shortfalls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>From the head, and it stops rather than skips.</b> <c>02 §4.1</c>: <em>six flour arriving
    /// wakes exactly the one bakery that needs six.</em> Skipping an uncovered waiter to reach a
    /// smaller one behind it would starve every large waiter permanently; waking everybody instead
    /// would push the whole list into Phase 2 and let the sorted settle order pick a winner that never
    /// changes, which is the wait list being decorative.
    /// </para>
    /// <para>
    /// <b>The budget is what arrived, not what the Bin now holds.</b> A shortfall was recorded against
    /// the level at the moment of failure, so what decides whether it is covered is the change since —
    /// and spending the budget down as waiters are woken is what lets six flour wake two bakeries
    /// needing three each without also waking a third.
    /// </para>
    /// </remarks>
    private void Drain(int binSlot, Blocking blocking, int arriving, Ticks tick)
    {
        IndexList waiters = Waiters(blocking);
        int remaining = arriving;

        while (true)
        {
            int head = waiters.PeekFront(binSlot);

            if (head == Rows.NoSlot || RuleInstances.Shortfall[head] > remaining)
            {
                return;
            }

            remaining -= RuleInstances.Shortfall[head];

            waiters.PopFront(binSlot);
            Wake(head, tick);
        }
    }

    /// <summary>Empties both of a Bin's wait lists, for a Bin that is about to stop existing.</summary>
    private void WakeAll(int binSlot, Ticks tick)
    {
        WakeAll(LevelWaiters, binSlot, tick);
        WakeAll(HeadroomWaiters, binSlot, tick);
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
        RuleInstances.Shortfall[instanceSlot] = 0;

        Wheel.Arm(instanceSlot, tick, 1);
    }

    /// <summary>Takes a Rule Instance off whichever one list it is on, leaving it on neither.</summary>
    private void Unlink(int instanceSlot)
    {
        Blocking blocking = RuleInstances.Blocked[instanceSlot];

        if (blocking == Blocking.Nothing)
        {
            Wheel.Armed.Remove(
                EventWheel.BucketOf(RuleInstances.NextTick[instanceSlot]), instanceSlot);
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
