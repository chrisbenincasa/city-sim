namespace Borough.Core.Invariants;

/// <summary>
/// How often an invariant is checked, which is the only axis <c>02 §10</c> sorts them on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Invariants sort by frequency, never by build configuration.</b> <c>02 §10</c> used to gate
/// these on debug builds and says plainly that it was backwards: the runs that surface this class of
/// bug are the headless balance runs, millions of Ticks long, and they are <b>release</b> builds. A
/// gate on <c>#if DEBUG</c> closes exactly where the exposure is.
/// </para>
/// <para>
/// <b>The tiers are also a cost model, and that is why there are three.</b> The old wording —
/// <em>Goods conserved, no Citizen in two places</em>, per Tick — is <c>O(n)</c> per Tick, defensible
/// at 10,000 Citizens and not at 1,000,000. <c>adr/0033</c> had already found the shape and it had
/// not been applied here: <em>unaffordable per Tick and trivial at the end of a headless run.</em>
/// </para>
/// </remarks>
public enum InvariantTier
{
    /// <summary>
    /// Every Tick, in every build. Only <c>O(1)</c> and <c>O(changed)</c>, checked at the write site.
    /// </summary>
    /// <remarks>
    /// <b>At the write site, not as a sweep.</b> This tier is not a list of things walked once a Tick
    /// — it is the checks a mutation makes about itself as it happens, which is what keeps it
    /// <c>O(changed)</c> and what makes the resulting failure point at the code that caused it rather
    /// than at whatever ran next.
    /// </remarks>
    PerTick,

    /// <summary>
    /// Every Tick, in every build, one slice of the world per Tick.
    /// </summary>
    /// <remarks>
    /// The <c>O(n)</c> sweeps, amortised across the population the same way Sweep Rules are
    /// (<c>adr/0033</c>). The whole world is covered every <see cref="Invariants.Slices"/> Ticks, so a
    /// violation is found within that many Ticks of appearing rather than immediately — which is the
    /// price of the tier existing at all at a million Citizens.
    /// </remarks>
    Staggered,

    /// <summary>
    /// Once, after the last Tick of a headless run.
    /// </summary>
    /// <remarks>
    /// The expensive whole-world walks, which are affordable precisely because there is one of them
    /// per run however long the run was.
    /// </remarks>
    EndOfRun,
}

/// <summary>
/// The invariants this build knows how to violate.
/// </summary>
/// <remarks>
/// <para>
/// <b>An id rather than a message, because the core does not own the words</b> (<c>adr/0002</c>).
/// A violation carries this, the rows involved and the Tick; what a human reads is assembled by
/// whoever is showing it. The id is also what a test asserts on, which keeps the tests from
/// depending on prose.
/// </para>
/// <para>
/// <b>The list is short because most of what <c>02 §10</c> names does not exist yet.</b> There are no
/// Bins, no Trips and no parking before slice 7, so the per-Tick tier the corpus describes is almost
/// entirely unbuilt. That is the expected state after this slice: what matters is that the next
/// mechanism has a tier to register into and cannot default into a debug-only check.
/// </para>
/// </remarks>
public enum Invariant
{
    /// <summary>Reserved, and never a real violation.</summary>
    None = 0,

    /// <summary>A Citizen was about to be added to a Household that already lists them.</summary>
    /// <remarks>
    /// The per-Tick, <c>O(changed)</c> half of <c>02 §10</c>'s <em>no Citizen in two places</em>. It
    /// is complete for one Household and blind across two, which is why
    /// <see cref="CitizenIsInExactlyOneHousehold"/> exists at the end of the run. Between them the
    /// corpus's invariant is covered; neither alone covers it, and saying so is the point of having
    /// tiers rather than a list.
    /// </remarks>
    CitizenIsNotAlreadyInThisHousehold = 1,

    /// <summary>A Household was about to be added to a Building that already lists them.</summary>
    HouseholdIsNotAlreadyInThisBuilding = 2,

    /// <summary>
    /// <b>Superseded by <see cref="HouseholdIsHousedOrInThePool"/> (<c>adr/0054</c>). Nothing reports
    /// it, and the id is retired rather than reused.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// It claimed <em>a Household's home is a Building that is still there</em>, which stopped being
    /// true the day a demolition could evict. The successor carries the qualified claim — <em>housed
    /// <b>or</b> in the Pool</em> — and slice 10 task 8 expected to amend this member in place;
    /// task 6 had already added a new one, which is the better outcome and left this behind.
    /// </para>
    /// <para>
    /// <b>Kept because the id travels.</b> A violation reaches a human through a crash artifact
    /// carrying the number, so deleting 3 would let a later invariant inherit it and make every
    /// artifact written before that day say something false. A banner costs nothing; a reused id
    /// cannot be un-reused.
    /// </para>
    /// </remarks>
    [Obsolete("Superseded by HouseholdIsHousedOrInThePool (adr/0054). The id is retired, not reused.")]
    HouseholdHomeExists = 3,

    /// <summary>A Household's home lists them as an occupant.</summary>
    HouseholdIsAnOccupantOfItsHome = 4,

    /// <summary>A Citizen's Household is still there.</summary>
    CitizenHouseholdExists = 5,

    /// <summary>A Citizen's Household lists them as a member.</summary>
    CitizenIsAMemberOfItsHousehold = 6,

    /// <summary>Every handle in every column addresses a row that is still there.</summary>
    /// <remarks>
    /// Referential integrity is ours to maintain (<c>adr/0004</c>): nothing in the table layer stops
    /// a column holding a handle to a freed row. The State Hash folds a dangling handle as a sentinel
    /// rather than throwing, precisely so that this walk rather than the hash is what reports it.
    /// </remarks>
    CrossTableHandleResolves = 7,

    /// <summary>A live Citizen appears in exactly one Household's member list.</summary>
    CitizenIsInExactlyOneHousehold = 8,

    /// <summary>A live Household appears in exactly one Building's occupant list.</summary>
    HouseholdIsInExactlyOneBuilding = 9,

    /// <summary>A freed row is not still linked into some list.</summary>
    /// <remarks>
    /// The failure <c>adr/0006</c> describes arriving by the back door — a row nothing can reach and
    /// nothing will free is a collection that only grows.
    /// </remarks>
    NoFreedRowIsStillLinked = 10,

    /// <summary>The city's money is still representable.</summary>
    /// <remarks>
    /// <b><c>adr/0003</c>'s overflow detector.</b> Conservation proper needs a treasury and
    /// transactions, neither of which exists; what is checkable today is that the sum has not run
    /// away, which is the half of the check that catches an accumulator with no sink.
    /// </remarks>
    MoneyIsRepresentable = 11,

    /// <summary>A Map Layer's value at some Cell has run away.</summary>
    /// <remarks>
    /// <b><c>adr/0003</c> extended <c>adr/0006</c> from collections to quantities, and a diffusing
    /// Layer with a source and no decay is exactly the shape that violates it.</b> Pollution was that
    /// shape until <c>adr/0051</c> made the source a stock the environment absorbs; the level is now
    /// bounded by an equilibrium rather than by this check, and what remains here is the kernel's
    /// representation ceiling. <c>plans/0009</c> task 10 registers it in the end-of-run tier because
    /// that is where the long runs are, not because it is expensive.
    /// </remarks>
    LayerMagnitudeIsBounded = 12,

    /// <summary>A Cell has more Tiles built on it than it has Tiles.</summary>
    /// <remarks>
    /// Sealing is bounded by <c>CellGrid.TilesInCell</c> by construction, clamped at its one write
    /// site. This is that bound checked rather than trusted: <b>a bound maintained at one write site
    /// is a bound that stops holding on the day somebody adds a second</b>, and the failure is silent
    /// because an over-sealed Cell still reads as a plausible number.
    /// </remarks>
    SealingIsWithinTheCell = 13,

    /// <summary>A Bin's level left <c>[0, capacity]</c>.</summary>
    /// <remarks>
    /// <b><c>CONTEXT.md</c> → Bin's constraint, checked at the one write site that can break it.</b>
    /// A Rule applies in its entirety or not at all precisely so that this holds, so a violation here
    /// says the atomicity check was skipped rather than that the arithmetic overflowed — which is why
    /// it is worth reporting at the deposit rather than discovering in a sweep.
    /// </remarks>
    BinLevelIsWithinCapacity = 14,

    /// <summary>A Building was about to be given a second Bin for one Resource.</summary>
    /// <remarks>
    /// One Bin, one Resource. Two would make <c>local</c> scope ambiguous — a Rule naming a Resource
    /// would draw from whichever the list happened to reach first, which is a balance outcome decided
    /// by allocation order.
    /// </remarks>
    BuildingHasOneBinPerResource = 15,

    /// <summary>A Rule Instance was about to be armed and waiting at once.</summary>
    /// <remarks>
    /// The two states share one link, so being on both lists is not representable; what this catches
    /// is the step before, a subscribe on a row that was never taken off the Wheel. That row would
    /// evaluate on its armed Tick <em>and</em> on the write that satisfies it — a Rule that polls and
    /// subscribes, which is the defect <c>02 §4.1</c>'s subscription model exists to remove.
    /// </remarks>
    RuleInstanceIsArmedOrWaiting = 16,

    /// <summary>A waiting Rule Instance is on the wait list of the Bin it names.</summary>
    /// <remarks>
    /// The whole-world half of the claim <see cref="RuleInstanceIsArmedOrWaiting"/> makes locally. A
    /// row naming a Bin it is not queued on is asleep with nothing that will ever wake it, which is
    /// <c>05 §9</c>'s failure reached from the other side — and unlike a missed drain it leaves no
    /// trace at the write site at all.
    /// </remarks>
    WaiterIsQueuedOnTheBinItNames = 17,

    /// <summary>
    /// <c>02 §2.2</c>: a Lot is either vacant or holds exactly one Building — checked where the
    /// second one would be built.
    /// </summary>
    /// <remarks>
    /// The <c>O(1)</c> write-site half, and the cheap one: <see cref="World.CreateBuilding"/> already
    /// has the Lot resolved, so the check is one comparison against the reverse index. Without it a
    /// second Building on an occupied Lot would overwrite the index and orphan the first — a live row
    /// nothing points at, which is <c>adr/0006</c>'s unreachable row arriving through a door nobody
    /// watches.
    /// </remarks>
    LotIsNotAlreadyBuiltOn = 18,

    /// <summary>
    /// The whole-world half of the same claim: the Lot and Building relation is a bijection.
    /// </summary>
    /// <remarks>
    /// Both directions, because each catches what the other cannot. Forward — every live Building is
    /// claimed by the Lot it names — catches a stale index after a demolition that did not vacate.
    /// Reverse — every occupied Lot names a live Building that names it back — catches an index
    /// pointing at a freed or recycled row, which is exactly what a missed <see cref="Invariant"/>
    /// at the write site would leave behind.
    /// </remarks>
    LotHoldsExactlyOneBuilding = 19,

    /// <summary>
    /// A Household entering the Unplaced Pool was living somewhere.
    /// </summary>
    /// <remarks>
    /// The <c>O(1)</c> write-site guard on the Pool's one corruption: unplacing a Household already
    /// in the Pool gives it a second membership row, and a draw over the Pool would then favour it in
    /// proportion to how often that happened. Nothing downstream could tell that apart from luck.
    /// </remarks>
    OnlyAHousedHouseholdIsUnplaced = 20,

    /// <summary>
    /// <c>adr/0054</c>: a Household is housed <b>or</b> is in the Unplaced Pool.
    /// </summary>
    /// <remarks>
    /// <b>The qualified form of <see cref="HouseholdHomeExists"/>, and the qualification is the
    /// point.</b> A Household with no dwelling used to be a violation outright; now it is legal
    /// precisely when the Pool holds it. Deleting the check instead would have removed the only thing
    /// that catches a genuinely orphaned Household — one the city has neither housed nor is looking
    /// for a home for, which is a row nothing will ever touch again.
    /// </remarks>
    HouseholdIsHousedOrInThePool = 21,

    /// <summary>
    /// Every member of the Unplaced Pool is a live Household with no dwelling, named once.
    /// </summary>
    /// <remarks>
    /// The Pool's half of the bijection above. It catches a membership left behind after a Household
    /// was housed by some path that did not go through <see cref="World.Place"/>, and a Household
    /// listed twice — both of which bias the draw silently rather than failing.
    /// </remarks>
    ThePoolNamesOnlyUnhousedHouseholds = 22,

    /// <summary>
    /// The Unplaced Pool's live rows are dense: every slot below its count is live.
    /// </summary>
    /// <remarks>
    /// <b>The property that makes an unbiased draw <c>O(1)</c>, asserted rather than argued.</b>
    /// <see cref="UnplacedTable.Leave"/> keeps it by moving the last member into the vacated position;
    /// if it ever stopped, a draw over the count would name a dead slot and a Lot that qualified
    /// would silently not be built on — which reads as a city that grows slowly and not as a defect.
    /// </remarks>
    ThePoolIsDense = 23,

    /// <summary>
    /// A Household joining the Unplaced Pool landed at the end of it.
    /// </summary>
    /// <remarks>
    /// <b>The <c>O(1)</c> write-site half of <see cref="ThePoolIsDense"/>, and it guards a borrowed
    /// assumption rather than a local one.</b> Density holds because <c>Rows</c>'s free list is LIFO
    /// and <see cref="UnplacedTable.Leave"/> frees only the last slot — but nothing in <c>Rows</c>
    /// promises LIFO, so this table depends on another type's implementation detail. Checked here, a
    /// change to the allocator fails on the first eviction; left to the whole-world walk, the city
    /// builds less than its Ruleset says for the length of a run and then reports it once.
    /// </remarks>
    ThePoolAppendsInOrder = 24,

    /// <summary>
    /// A Household being housed out of the Unplaced Pool was in it.
    /// </summary>
    /// <remarks>
    /// The mirror of <see cref="OnlyAHousedHouseholdIsUnplaced"/>, closing the other direction of the
    /// same boundary. Housing somebody who was never in the Pool would move them out of a dwelling
    /// they still occupy and take an unrelated membership row with them.
    /// </remarks>
    OnlyAPooledHouseholdIsPlaced = 25,

    /// <summary>
    /// A Building the Ruleset in force cannot describe runs nothing.
    /// </summary>
    /// <remarks>
    /// <b>The claim slice 8's migration is most likely to break, because nothing in the shape of a
    /// re-arm loop makes the exclusion obvious.</b> Dereliction is <c>Kind == 0</c> and has no flag
    /// (<c>adr/0057</c>),
    /// so a refit that armed by walking the Rule table rather than the kind's declarations would leave
    /// a derelict Building firing the Rules of the kind it used to be — a bakery baking under a
    /// Ruleset that has never heard of bakeries, and with no name for what it is doing.
    /// <para>
    /// It is also the invariant that keeps <c>adr/0055</c>'s consequence bullet honest rather than
    /// repairing it. A Building with no Rules has no failures, so <c>ZoneRuleEngine.Condemn</c>'s
    /// threshold walk finds nothing and the Building stands until the player clears it — which is
    /// <c>PLAYER GOVERNS</c>, and the alternative is silent deletion arriving through a Zone Rule
    /// instead of through the reload.
    /// </para>
    /// </remarks>
    DerelictBuildingRunsNoRules = 26,

    /// <summary>
    /// An armed row is due strictly after now and strictly within one period.
    /// </summary>
    /// <remarks>
    /// <b>The half of <see cref="RuleInstanceIsArmedOrWaiting"/> that could not be written modulo the
    /// period.</b> The whole-world walk checks that an armed row sits in the bucket its
    /// <see cref="Rules.RuleInstanceTable.NextTick"/> names, and <c>BucketOf</c> is
    /// <c>NextTick % WHEEL_SIZE</c> — so that test is invariant under adding a whole period, and a row
    /// due 8,192 Ticks ago passes it exactly as a row due next period does. Membership in the right
    /// bucket is not the same claim as being reachable by the drain.
    /// <para>
    /// <b>It is unreachable while the drain visits every bucket in order, which is why it is stated
    /// rather than assumed.</b> The two mechanisms that can produce a stale <c>NextTick</c> are a
    /// Ruleset reload's refit and a load from a save — and slice 8's refit re-arms on slice 7's
    /// <c>[1, rate]</c> stagger, so today only the second can, and the second does not exist yet
    /// (architecture invariant 6, the Factorio test). This is a check written in front of a mechanism
    /// rather than behind a caller. See <c>plans/0016</c>.
    /// </para>
    /// <para>
    /// <b>It is exactly as good as the Tick its caller passes</b>, because the World does not hold one —
    /// the same reason <c>World.Adopt</c> has to take it as a parameter. <c>Simulation.CheckEndOfRun</c>
    /// passes the real Tick, which is the path the long runs and the runner take.
    /// </para>
    /// </remarks>
    AnArmedRowIsDueWithinOnePeriod = 27,

    /// <summary>
    /// A waiter is asleep on a Bin that has stopped blocking it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0033</c> names this as one of two mitigations it calls <em>both required</em>, and it
    /// has been specified in three documents and built in none.</b> The wording there is <em>no Rule is
    /// asleep with all inputs satisfiable</em>; <c>02 §10</c> lists it in the end-of-run tier and
    /// <c>plans/0008</c> repeats it. Subscription's failure mode is silent where polling's was merely
    /// slow — a Bin written without draining its wait list leaves every waiter asleep for ever with no
    /// error and no timer to rescue it — so this is the only mechanism in the design that can notice it.
    /// </para>
    /// <para>
    /// <b>It is deliberately narrower than that sentence in what it inspects, and stronger in what it
    /// catches</b> (<c>adr/0063</c>). <em>Would this Rule fire</em> is blind to a waiter asleep on a Bin
    /// that has stopped blocking it while a different Bin blocks it now: the drain should have woken it,
    /// it would have re-checked, failed elsewhere, and <em>resubscribed to the Bin that actually blocks
    /// it</em>. Left where it is, a deposit to that other Bin never reaches it — a livelock, not a slow
    /// city — and the strict reading reports nothing. Checking the named Bin alone catches both that and
    /// the plain missed wake, and costs one term walk instead of a whole evaluation.
    /// </para>
    /// <para>
    /// <b>It was expected to be unfirable on today's content, and it fired on the committed golden
    /// session within minutes of being registered.</b> The reasoning for the expectation was that a
    /// violation needs a Bin written in instalments smaller than a waiter's requirement, which is what
    /// the <c>pool</c> scope will be and what <c>local</c> cannot be — and that
    /// <c>rulesets/minimal.toml</c> is safe because <c>restock</c>'s deficit is <b>1</b>, the smallest
    /// quantity expressible, so any withdrawal covers it. Both halves are true and the conclusion was
    /// still wrong: <b>the golden session reloads into <c>rulesets/minimal-tuned.toml</c> at Tick 128,
    /// and the one number that file changes is <c>restock</c>'s output amount, 1 → 2.</b> A producer
    /// whose deficit is 2, drawn down by the occupancy-1 Buildings a Zone Rule creates, is withdrawn
    /// from one unit at a time and never woken. At Tick 256 the trace holds a <c>restock</c> asleep on
    /// headroom <b>3</b> against a recorded shortfall of <b>2</b>.
    /// </para>
    /// <para>
    /// <b>So the defect is live in the shipped baseline rather than waiting for <c>pool</c></b>, and
    /// <c>minimal.toml</c>'s header states the condition that keeps it honest — <em>producing in a
    /// quantum at least as large as any consumer's deficit</em> — without stating the mirror, which is
    /// the one the tuned file breaks. The trickle fixtures in <c>BinWaitListTests</c> assert both
    /// directions on purpose-built content; this member is what found the real instance.
    /// </para>
    /// </remarks>
    WaiterIsBlockedByTheBinItNames = 28,

    /// <summary>
    /// A live Bin's capacity is what the Ruleset in force declares for its Building's kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The honest price of <c>adr/0064</c> rather than an extra.</b> Capacity became <em>derived
    /// and rebuilt</em>, and a derived column nobody rebuilt is <b>stale</b> — which is silent, because
    /// every row stays self-consistent and the only wrong thing is a number that agrees with a Ruleset
    /// nobody is running any more. That is the same class of failure <c>adr/0063</c> had to build
    /// <see cref="WaiterIsBlockedByTheBinItNames"/> to notice, arriving one column over.
    /// </para>
    /// <para>
    /// <b>It should be unfirable, and that is what makes it worth registering.</b> Two sites write the
    /// ceiling — construction and the rebuild — and both read the same declaration. If this fires, the
    /// fault is not the arithmetic but the rebuild's <em>placement</em>, and the thing to look for is a
    /// path on which a Ruleset reaches a <see cref="Borough.Core.Entities.World"/> without passing
    /// through the swap.
    /// </para>
    /// <para>
    /// <b>A derelict Building's Bins are in scope, at a declared ceiling of zero.</b> A kind the
    /// Ruleset dropped declares no store of anything, so zero is the derivation's answer rather than an
    /// exemption — and exempting them is how a whole class of rows would stop being checked at exactly
    /// the moment a reload had touched them.
    /// </para>
    /// <para>
    /// <b>End of run, per <c>02 §10</c>'s frequency tiering.</b> It is a whole-world walk of every Bin
    /// with a lookup per row, and there is one of them per run however long the run was.
    /// </para>
    /// </remarks>
    BinCapacityMatchesItsDeclaration = 29,

    /// <summary>A Building holds no more Occupants than its kind declares room for.</summary>
    /// <remarks>
    /// <para>
    /// <b>A write-site guard and not a standing check</b>, which is <c>adr/0064</c>'s id-14 finding
    /// applied to the second capacity a Building has. <see cref="Entities.World.HasRoom"/> is the
    /// *predicate* a caller asks before placing; reaching this member means somebody placed without
    /// asking, which is a caller bug rather than a world in a bad state.
    /// </para>
    /// <para>
    /// <b>An over-capacity Building is legal and does not report here</b>, because it is reachable
    /// without anybody having written anything wrong: a Ruleset lowering a ceiling leaves standing
    /// Buildings above it for exactly as long as it takes <c>Adopt</c> to run its eviction. What is
    /// illegal is a *placement* into one, which is the direction this guard faces.
    /// </para>
    /// <para>
    /// <b>Dereliction is not a violation either</b> (<c>adr/0068</c>). A Building whose kind the
    /// incoming Ruleset dropped has no declared ceiling at all, keeps its Occupants — <c>CONTEXT</c>
    /// → Derelict Building says so in its own words — and admits nobody new. Evicting on a kind
    /// disappearing would empty a District because a designer deleted a paragraph.
    /// </para>
    /// </remarks>
    BuildingHasRoomForTheHousehold = 30,

    /// <summary>A Segment's two endpoint handles both resolve to live nodes.</summary>
    /// <remarks>
    /// <b>The Road Graph's referential integrity, and the one failure that is silent rather than
    /// loud.</b> <c>RoadGraph.RebuildDerived</c> skips a Segment whose endpoint is dangling rather
    /// than throwing, because a rebuild runs on the load path and one that threw would take down a
    /// load instead of a write. The skip is correct and the state that provoked it is not: the
    /// Segment stays in the table, folds into the State Hash, and is absent from every adjacency —
    /// a road that exists and that nothing can reach. This is the check that says so.
    /// </remarks>
    RoadSegmentEndpointsExist = 31,

    /// <summary>A Segment has a positive length and a positive free-flow speed.</summary>
    /// <remarks>
    /// <b>Both are divisors.</b> A traversal cost is length ÷ speed, so a zero speed raises and a
    /// zero length makes every route through the Segment free — which A* will then prefer to every
    /// alternative, for ever. The loader refuses a speed below 1 km/h, so reaching this means a
    /// Ruleset that did not come through it.
    /// </remarks>
    RoadSegmentIsTraversable = 32,

    /// <summary>A Segment's derived mask is exactly the union of its two saved direction masks.</summary>
    /// <remarks>
    /// <b><c>adr/0072</c>'s consequence, checked rather than assumed.</b> The Arcs own the truth and
    /// the Segment's mask is a derived <c>OR</c>; a derived column that has silently stopped agreeing
    /// with its source is the defect the <c>(derived AND rebuilt)</c> declaration exists to make
    /// impossible to hide, and the only way to see it is to recompute and compare.
    /// </remarks>
    SegmentModesAreTheUnionOfItsArcs = 33,

    /// <summary>
    /// The Arcs partition into the nodes' CSR slices, and each is a direction of the Segment it names.
    /// </summary>
    /// <remarks>
    /// <b>The spike's <c>AssertWellFormed</c>, promoted from a sanity walk to an invariant.</b> A
    /// generator that quietly emitted a self-looping or mis-grouped adjacency would produce routing
    /// figures that look entirely fine and mean nothing — which is exactly what happened to S2's first
    /// capture, where every Arterial left the map within a step and the footprint table stayed
    /// healthy. Whole-world tier, because it is a walk over every Arc.
    /// </remarks>
    ArcsAreDirectionsOfTheirSegments = 34,

    /// <summary>
    /// A vacant Lot has frontage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every <em>vacant</em> Lot, and the qualifier is the invariant.</b> <c>plans/0022</c> task 7
    /// asked for <i>"every Lot has frontage"</i>, which is false under that document's own decision 4:
    /// a Building whose last Street is bulldozed keeps standing on a Lot that now has none
    /// (<c>adr/0079</c>). What re-subdivision guarantees is the other half — land nobody has built on
    /// re-parcels, so a vacant Lot with no Street is a Lot the freeing pass missed.
    /// </para>
    /// <para>
    /// <b>It is also the only check that can notice frontage having silently stopped being derived.</b>
    /// <c>World.RebuildDerived</c> recomputes every Lot's Segment from its saved position, and a
    /// rebuild that quietly produced nothing would leave a coherent world, a moving State Hash and
    /// every Lot unreachable — the failure has no other symptom until 5b tries to route to one.
    /// Whole-world tier: it is a walk over every Lot, and a Lot loses frontage only when a road is
    /// edited.
    /// </para>
    /// </remarks>
    VacantLotHasFrontage = 35,

    /// <summary>
    /// No Trip is released without a Fate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §10</c> named this and could not build it: <i>"no Trip without a Fate needs Trips"</i>.
    /// Trips exist as of milestone 5b, so it does now.
    /// </para>
    /// <para>
    /// <b>A write-site check on a condition that currently holds by construction, which is exactly
    /// when it is worth writing.</b> <see cref="Movement.TripEngine"/> is the only code that frees a
    /// Trip row and it releases nothing still <see cref="Movement.TripFate.InFlight"/>, so this cannot
    /// fire today. What it guards is the <em>second</em> release site — a generator cancelling a Trip,
    /// a Household evicted mid-journey, a bulldozed Segment taking its Travellers with it — each of
    /// which is a plausible future edit that frees a row without going through the sweep.
    /// </para>
    /// <para>
    /// <b>A Trip freed without a Fate is an <c>adr/0006</c>-class defect that presents as good
    /// news</b>: the Census counts Fates, not rows, so the lost Trip does not appear as a failure. It
    /// appears as a city whose Trips all succeed.
    /// </para>
    /// </remarks>
    TripHasAFate = 36,

    /// <summary>
    /// Summed Segment volume equals the number of in-flight vehicular Travellers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0041</c> asks for it by name — <i>"a new invariant belongs with the definition of done:
    /// summed Segment volume equals the number of in-flight vehicular Travellers, every Tick"</i> —
    /// on the ground that <i>"a Traveller that vanishes without decrementing destroys the reading
    /// permanently, which is an <c>adr/0006</c>-class defect that presents as a road that looks busy
    /// forever."</i>
    /// </para>
    /// <para>
    /// <b>Whole-world tier rather than per-Tick, which is a narrower claim than the ADR's sentence.</b>
    /// The sum is a walk over every Segment — ~33,024 at the shipped <c>[roads]</c> — and <c>02 §10</c>
    /// sorts by frequency rather than by importance, with S0a's <c>O(world)</c> Decide guard as the
    /// standing evidence: on by default, it was 95% of a run. What holds every Tick is conservation
    /// <em>structurally</em>, from increment and decrement being paired; this is the check that the
    /// pairing was not broken.
    /// </para>
    /// <para>
    /// <b>It is vacuously satisfied through the whole of milestone 5b, and it is written now on
    /// purpose.</b> Both sides are zero: 5b resolves walk Legs only and <c>adr/0041</c> increments on
    /// <b>vehicular</b> Legs alone. The alternative is a check written by whoever adds the first
    /// vehicular Leg, at the moment they are least able to notice they have got the pairing wrong.
    /// </para>
    /// </remarks>
    SegmentVolumeIsConserved = 37,
}
