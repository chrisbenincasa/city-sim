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
    /// ⚠ <b>And narrower again in <em>which waiter</em> since <c>plans/0003</c> hash-moving queue item
    /// 14: the head of the list, and only the head.</b> The drain stops at the first waiter it cannot
    /// cover rather than skipping to a smaller one behind it — <c>adr/0063</c>'s queue order, and the
    /// alternative starves every large waiter for the life of the city — so a covered waiter queued
    /// <em>behind</em> an uncovered one is parked correctly. This member was stated more strongly than
    /// the drain can deliver, and <see cref="Rules.RuleEngine"/>'s own starvation test asserted the
    /// state it called a violation. ***The drain was right and the sentence describing it was too
    /// strong.***
    /// </para>
    /// <para>
    /// <b>And it is asked against the level less what is already spoken for.</b> A woken waiter records
    /// no claim anywhere: <c>World.Wake</c> clears <c>Blocked</c> and arms for <c>tick + 1</c>, and
    /// nothing is drawn until that row runs — so between the drain and the end of the Tick the level
    /// reads as though none of it were owed. <b>The drain's guarantee is true of an instant</b>, and
    /// three waiters needing three each against a deposit of six leave the third one as the head with
    /// the whole level still covering it. <see cref="Rules.RuleEngine.AccumulateClaims"/> derives the
    /// difference. ⚠ <b>Neither half is reached by the other's repair.</b>
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
    /// space <b>3</b> against a recorded shortfall of <b>2</b>.
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

    /// <summary>A Citizen is not already on the worker list of the Building employing them.</summary>
    /// <remarks>
    /// <b><see cref="HouseholdIsNotAlreadyInThisBuilding"/> on the employment axis, and a write-site
    /// guard for the same reason.</b> <see cref="Entities.World.Employ"/> unlists the Citizen's
    /// previous Workplace before it links the new one, so reaching this means the two ends disagreed
    /// — a Workplace handle that did not resolve to the list the Citizen was actually on. The
    /// consequence is a doubly-linked Citizen, which walks as two workers and fills one job twice.
    /// </remarks>
    CitizenIsNotAlreadyEmployedHere = 38,

    /// <summary>
    /// A Citizen appears on exactly one Building's worker list, or on none if they hold no job.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The whole-world half of <see cref="CitizenIsNotAlreadyEmployedHere"/></b>, and the check
    /// that licenses <see cref="Entities.CitizenTable.WorkerNext"/>'s
    /// <see cref="Tables.Disposition.Derived"/> declaration in production rather than only in a test.
    /// A derived list folds into no hash, so a maintained list that has drifted from the saved
    /// handles it is derived from is invisible to replay, to the golden baseline and to save/reload
    /// alike — every one of those compares hashes, and this column is in none of them.
    /// </para>
    /// <para>
    /// <b>Absent is legal for an unemployed Citizen and for a severed one alike, and the exemption is
    /// two-sided.</b> A Citizen with no Workplace holds no job; a Citizen whose Workplace was
    /// demolished holds a handle that no longer resolves, which
    /// <c>CONTEXT.md</c> → Trip Fate's own reasoning makes <em>the job stopped existing</em> rather
    /// than a break. Either one appearing on a list anyway is the corruption that would let a
    /// demolished employer keep staff.
    /// </para>
    /// </remarks>
    CitizenIsInExactlyOneWorkplace = 39,

    /// <summary>The city holds exactly the money that was issued into it.</summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0031</c>'s promise, and the other half of
    /// <see cref="MoneyIsRepresentable"/>.</b> That one catches an accumulator with no sink — a sum
    /// that has run away — and this one catches the failures a well-behaved magnitude hides: money
    /// created by a write that had no counterparty, and money destroyed with the row it was sitting
    /// on. Both are end-of-run walks, and they are two checks rather than one because the second is
    /// only possible once there is an anchor to compare against.
    /// </para>
    /// <para>
    /// <b>Two sums arrived at differently.</b> Every live Household's <c>Money</c> and <c>Savings</c>,
    /// plus every live Bin holding a conserved Resource, against
    /// <see cref="Entities.MoneySupplyTable.Issued"/> — which moves only at <c>World.Endow</c>. An
    /// anchor recovered by summing the balances would be the failure milestone 10 task 1 found in a
    /// different invariant: recomputing the producer's own expression checks that the write happened
    /// and never what was written.
    /// </para>
    /// <para>
    /// <b>The Bin side walks every owner rather than the treasury's list</b>, because conservation is a
    /// claim about totals and not about placement. A Building holding money is refused by
    /// <c>adr/0113</c> and would be a violation of <em>that</em>; counting it here would report it as
    /// money destroyed, which is the wrong diagnosis and would go on being wrong after the placement
    /// rule was enforced.
    /// </para>
    /// <para>
    /// <b>The reported <c>other</c> is the discrepancy</b> — what the city holds minus what was issued
    /// — because that number says which way the leak runs, and a slot cannot: the failure is a
    /// property of the whole world and no single row is to blame for it.
    /// </para>
    /// <para>
    /// ⚠ <b>An exact equality was expected to be a property of the schedule, and it turned out to be
    /// a property of the anchor.</b> The reasoning here was that the supply is constant for the whole
    /// of milestone 10, so the reading could be taken while it was. ✅ <b>The gate landed at milestone
    /// 11 task 5 and the check did not move</b> — <see cref="Entities.MoneySupplyTable.Issued"/> is
    /// declared as money that has entered <em>net of anything that has left it</em>, and
    /// <c>World.Endow</c> writes it in the same call that deposits, so an arrival moves both sides
    /// together. ***A flow term is only owed where the two sides are arrived at on different
    /// schedules***, and one door that writes both is what makes sure they are not.
    /// </para>
    /// </remarks>
    MoneyIsConserved = 40,

    /// <summary>
    /// A Citizen giving up a parking space holds one that resolves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A write-site check on a condition that holds by construction, which is exactly when it is
    /// worth writing</b> — <see cref="TripHasAFate"/>'s shape, and <c>adr/0084</c> asks for this half
    /// at the write site rather than per Tick for that ADR's own reason. <c>World.ReleaseParking</c>
    /// is the only code that decrements <c>CarParkTable.Occupied</c>, and nothing today reaches it
    /// without a resolving <c>CitizenTable.ParkedIn</c>, so it cannot fire. What it guards is the
    /// <em>second</em> release site.
    /// </para>
    /// <para>
    /// <b>An unpaired release is an <c>adr/0006</c>-class defect that presents as good news.</b>
    /// Occupancy decremented without a holder is capacity conjured from nothing: the city reports
    /// more parking than it built, every shed query succeeds, and the shortage the player is supposed
    /// to feel simply never arrives. It reads as a well-provisioned city rather than as a leak, which
    /// is why <c>CarParkTable.Move</c> is <see langword="internal"/> and pairing it with the holder's
    /// column is <see cref="Entities.World"/>'s job alone.
    /// </para>
    /// <para>
    /// ⚠ <b>Holding <em>two</em> spaces is unrepresentable rather than checked</b>, because
    /// <c>adr/0119</c> puts the space on the Citizen in a single column. That is the Rule Instance
    /// armed/waiting precedent — the corpus prefers a state it cannot express to a state it verifies —
    /// so this invariant is about the release naming a live Car Park and never about the count.
    /// </para>
    /// </remarks>
    ParkingSpaceIsReleasedOnce = 41,

    /// <summary>
    /// Summed Car Park occupancy equals the number of Citizens holding a space that resolves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The invariant <c>plans/0012</c> check 7 was filed over</b>, and the reason that check exists:
    /// it was specified in <b>four</b> documents — <c>adr/0009</c>, <c>02 §10</c>, <c>05 §60</c> and
    /// <c>06</c>'s milestone 7 risk — and built in <b>none</b>, which is invisible because an
    /// obligation with no member reads as absent rather than as owed. It is declared here and owed by
    /// milestone 7 task 6.
    /// </para>
    /// <para>
    /// ⚠ <b><c>adr/0084</c> states its right-hand side wrongly and this milestone corrected it.</b>
    /// That ADR sums against <i>"Travellers currently parked"</i>, and a car parked overnight has no
    /// Traveller — so the sum as specified reads <b>0 against a full car park</b> every night, on the
    /// design's own canonical case. <c>adr/0119</c> puts the space on the <b>Citizen</b>, so the
    /// operand is Citizens whose holding resolves. The ADR is not wrong about the tier or the split.
    /// </para>
    /// <para>
    /// <b>It is an end-of-run walk and not the per-Tick check <c>02 §10</c>'s table said it was</b>,
    /// which is <c>adr/0084</c>'s ruling and the reason it gives: occupancy is conserved every Tick
    /// <em>structurally</em>, because <see cref="Entities.World.TryTakeParking"/> and
    /// <see cref="Entities.World.ReleaseParking"/> write the occupancy and the holder's column
    /// together. What can break is the <b>pairing</b>, and a pairing defect is a property of a run
    /// rather than of a moment — so a whole-world sum every Tick spends <c>O(world)</c> to find what
    /// one sum at the end finds just as certainly. <see cref="SegmentVolumeIsConserved"/> and
    /// <see cref="BinCapacityMatchesItsDeclaration"/> are the two precedents that were demoted for
    /// this reason before it.
    /// </para>
    /// <para>
    /// ⚠ <b>Both sides skip a Car Park that has been demolished, and that is the check having
    /// content rather than being weakened.</b> <c>CitizenTable.ParkedIn</c> is
    /// <c>Reference.Severable</c>, so a bulldozed garage takes its occupancy column and every
    /// holder's resolution in one act — counting an unresolvable holding would report a demolition
    /// as a leak, which is the wrong diagnosis for the only mutation site that is allowed to drop a
    /// holding without a decrement.
    /// </para>
    /// <para>
    /// <b>The leak it is actually for is a Citizen freed while holding a space.</b>
    /// <see cref="Entities.World.DestroyCitizen"/> and <see cref="Entities.World.DestroyHousehold"/>
    /// unlink a Citizen from everything except a Car Park, so the occupancy would stay up with
    /// nobody standing in it: the city would report more parking taken than it has drivers, and
    /// every subsequent shed query would find less room than exists. That is the <c>adr/0006</c>
    /// class — a quantity with a source and no sink — and it presents as a <em>shortage</em> rather
    /// than as a crash, which is why nothing else would report it. ⚠ Neither method has a caller
    /// outside a test, so this is a check on a pairing that has not been made yet rather than on one
    /// that is broken — <see cref="TripHasAFate"/>'s shape, and the same argument
    /// <see cref="ParkingSpaceIsReleasedOnce"/> makes about the second release site.
    /// </para>
    /// </remarks>
    ParkingOccupancyIsConserved = 42,

    /// <summary>Goods are conserved across every Bin and every movement.</summary>
    /// <remarks>
    /// ⚠ <b>Named by <c>02 §10</c>'s staggered tier since before the ADRs and owned by no milestone
    /// anywhere.</b> It is not deferred, not gated and not refused — nothing in <c>06</c>, <c>0003</c>
    /// or <c>0002</c> claims it, which is why it has sat unbuilt without ever appearing as owed. Found
    /// by building check 7 rather than by anybody reading the tier table, which is precisely that
    /// check's argument for existing.
    /// </remarks>
    [Unbuilt("nothing — 02 §10 names it and no milestone claims it. plans/0012 check 7's own finding")]
    GoodsAreConserved = 43,

    /// <summary>No Citizen is in two places at once.</summary>
    /// <remarks>
    /// ⚠ <b><c>02 §10</c>'s staggered tier again, and owned by no milestone either.</b> Its sibling
    /// <see cref="CitizenIsInExactlyOneHousehold"/> is live and is a different claim — that one is about
    /// <em>membership</em>, this one about <em>location</em>, and a Citizen can be in exactly one
    /// Household while a Traveller and a Workplace disagree about where they are standing. ***A live
    /// invariant with a similar name is how an unbuilt one stays invisible***, and it is why this is
    /// declared separately rather than read as already covered.
    /// </remarks>
    [Unbuilt("nothing — 02 §10 names it and no milestone claims it. plans/0012 check 7's own finding")]
    CitizenIsInExactlyOnePlace = 44,

    /// <summary>
    /// An Outside Connection stands on exactly one map edge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0088</c>'s *"a position constrained to an edge"*, at the write site</b> (milestone 11
    /// task 1). It is <c>O(1)</c> in <see cref="Entities.World.CreateBuilding"/>, which is where
    /// <c>02 §10</c> puts a check that costs one comparison per placement, and it sits beside
    /// <see cref="LotIsNotAlreadyBuiltOn"/> in the same method for the same reason.
    /// </para>
    /// <para>
    /// <b>*Exactly one* rather than *at least one*, and the corner is why.</b> Under <c>adr/0088</c>
    /// the edge <em>selects a market</em> — <c>CONTEXT.md</c> → Hinterland is per edge — so a gate on
    /// a corner Lot would sit in two Hinterlands with nothing in the world to say which one its
    /// emigrants came from. That is not a tie to break; it is a question the design has no answer to,
    /// and the milestone that reads the answer is this one. ***A position touching two markets is
    /// refused rather than resolved, because resolving it would invent the rule.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is a guard and not a predicate, so nothing samples against it.</b> A gate is placed
    /// deliberately — by the generator at task 3, by the player later — and never by a Zone Rule
    /// walking candidate Lots, so there is no *ordinary outcome* here for the guard to spam on. That
    /// is the opposite of <see cref="BuildingHasRoomForTheHousehold"/>, which needed a predicate
    /// beside it precisely because placement asks it of every candidate it samples.
    /// </para>
    /// </remarks>
    OutsideConnectionStandsOnOneEdge = 45,

    /// <summary>
    /// A Household arriving from outside crosses a live Outside Connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The <c>O(1)</c> write-site guard on <see cref="Entities.World.TryArrive"/></b>, milestone
    /// 11 task 4. Arrival is the one door that creates a Household which has never lived here
    /// (<c>adr/0129</c>), and the gate it names becomes the origin of that Household's move-in Trip —
    /// so a gate that is not a gate produces a Trip starting nowhere in particular, at placement,
    /// long after the call that was wrong.
    /// </para>
    /// <para>
    /// ⚠ <b>It does not fire when the gate's daily ceiling is met, and that separation is the
    /// point.</b> A full gate refusing an arrival is <c>[[building]] arrivals_per_day</c> doing its
    /// job — an ordinary outcome with a diagnosis of its own — and reporting it here would put the
    /// mechanism's normal operation in the crash artifact. ***A bound that binds is not a violated
    /// invariant.*** What this reports is a caller naming a Building that cannot admit anybody at
    /// all, which is <see cref="BuildingHasRoomForTheHousehold"/>'s distinction one door over.
    /// </para>
    /// </remarks>
    AnArrivalCrossesAnOutsideConnection = 46,

    /// <summary>
    /// Every Unplaced Pool member either names a live Outside Connection or names no gate at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The whole-world half of <see cref="AnArrivalCrossesAnOutsideConnection"/></b>, and it exists
    /// because the write-site guard cannot see what a <em>Ruleset reload</em> does. A kind is a gate
    /// precisely when it declares <c>arrivals_per_day</c> (<c>World.IsOutsideConnection</c>) and a
    /// Ruleset is hot-reloadable (<c>adr/0015</c>), so removing the key converts every standing gate
    /// back into an ordinary Building **with no call made** — leaving members of the Pool waiting at
    /// a door that is no longer one. That is <c>plans/0035</c> <b>F14</b> exactly, one milestone's
    /// task later and on a different column.
    /// </para>
    /// <para>
    /// <b>A default handle passes, and is the ordinary reading.</b> Three of the Pool's four entry
    /// routes have no gate — see <see cref="Entities.UnplacedTable.Gate"/> — so *no gate* and *a gate
    /// that is not one* have to be distinguishable, and they are: <c>default</c> against a handle
    /// that resolves to a Building whose kind is not an Outside Connection.
    /// </para>
    /// </remarks>
    ThePoolsGateIsAnOutsideConnection = 47,

    /// <summary>
    /// The gate a Household arrives through opens onto a declared Hinterland.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The pairing the loader cannot make</b> (<c>plans/0035</c> task 2). Which edge an Outside
    /// Connection stands on is a property of where it was <em>placed</em>
    /// (<c>World.EdgeOf</c>), not of the Ruleset — so a file declaring a gate kind and no
    /// <c>[[hinterland]]</c> is not refusable at load, because the loader cannot see a world. The
    /// pairing happens at arrival, and this is it.
    /// </para>
    /// <para>
    /// 🔴 <b>The arrival is refused rather than admitted with nothing, and that is milestone 9's
    /// F13.</b> A Household admitted through a gate with no economy behind it would carry zero — and
    /// zero is a <em>legitimate</em> answer, because <see cref="Rules.HinterlandDefinition.Endows"/>
    /// says a Hinterland whose emigrants arrive penniless is a real economy. So admitting would make
    /// *nowhere* and *somewhere poor* the same observation. ***A mechanism returning plausible
    /// results while saying something false is worse than one that refuses***, and a zero that is a
    /// real answer cannot double as the absence of an answer.
    /// </para>
    /// <para>
    /// ⚠ <b>It fires at the arrival and not at the placement, and the absence of the second half is
    /// <c>unbuilt</c> rather than refused</b> (<c>adr/0070</c>). A check in
    /// <c>World.CreateBuilding</c> would say so at once, and a whole-world walk would catch a reload
    /// removing a <c>[[hinterland]]</c> from under a standing gate — <c>plans/0035</c> <b>F14</b>'s
    /// shape a third time. Neither is built because no shipped Ruleset can produce either: the one
    /// file with a gate declares all four edges. ***Naming where the other halves would go is what
    /// keeps their absence from reading as a decision.***
    /// </para>
    /// </remarks>
    AGateOpensOntoAHinterland = 48,

    /// <summary>
    /// <see cref="Entities.World.Depart"/> was handed a Household that is not unhoused in the Pool.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A door check on the only Departure channel that exists</b>
    /// (<c>adr/0130</c>, milestone 11 task 7). <c>CONTEXT</c> gives Departure three channels and they
    /// are not three sizes of the same thing: the <b>unhoused</b> one is a bound and a threshold and
    /// ships here; the <b>housed</b> one is a *comparison* the Household re-runs (<c>adr/0102</c>) and
    /// ships at 16 with the choice model; the <b>destitute</b> one needs Unemployment and a floor and
    /// is later still.
    /// </para>
    /// <para>
    /// <b>So this fires on a wiring mistake rather than on a city state.</b> A housed Household
    /// arriving at this door would leave the city through the unhoused channel's accounting — and the
    /// readouts that report Departures *by channel* would then attribute a family that chose to move
    /// to a family that could not find a home, which is two opposite diagnoses sharing a number.
    /// ⚠ <b>It reports rather than throwing</b>, on <c>02 §10</c>'s rule: the Household stays where it
    /// is, which is the conservative outcome, and the run continues so the whole-world checks can say
    /// what else is wrong.
    /// </para>
    /// </remarks>
    OnlyAnUnhousedHouseholdGivesUp = 49,

    /// <summary>
    /// A <c>DistrictCell</c> names a District that is not live.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The State Hash cannot report either half, which is the whole argument for the check</b>
    /// (<c>adr/0134</c>, milestone 12 task 4). A handle column folds the target row's monotonic id
    /// through <c>Rows.TryIdAt</c>, and a handle whose target has been freed folds as <b>zero</b> — so
    /// a membership row left pointing at a destroyed District is a dangling reference that every
    /// determinism test in the project agrees about. ***Two runs reproduce the same wrong answer.***
    /// </para>
    /// <para>
    /// <b>It is reachable because re-evaluation destroys Districts</b>, which is what makes
    /// <c>adr/0134</c>'s <em>the count is physics</em> true of a running city and not only of a new
    /// one. The order the reconciliation frees things in — Cells released, then Districts — is the
    /// thing this asserts, and it is the kind of ordering that is right when written and wrong three
    /// mechanisms later.
    /// </para>
    /// <para>
    /// 🔴 <b>IT HAD A SECOND HALF — <em>and a Cell that holds a Building</em> — AND THAT HALF WAS
    /// WRONG HERE.</b> Narrowed 2026-08-22 by <c>plans/0003</c> queue item 16, which was filed when a
    /// three-Day headless run of <c>rulesets/twinned.toml</c> panicked on it. The extent is derived on
    /// <c>[districts] revisit_ticks</c>, so ***between two evaluations it describes the city as of the
    /// last one*** — a Cell demolished at Tick 1,152 keeps its membership until Tick 2,048, measured,
    /// and the eviction then clears it. **The mechanism was right and the sentence describing it was
    /// too strong**, which is <see cref="WaiterIsBlockedByTheBinItNames"/>'s finding arriving on a
    /// second mechanism: a check is a description of the build, and a description can overstate.
    /// </para>
    /// <para>
    /// ⚠ <b>The tempting repair — evict at the demolition site — was refused, and the reason is
    /// symmetry.</b> A Cell that <em>gains</em> its first Building also waits for the cadence to join a
    /// District. Making removal instant while addition stays cadenced is an asymmetry with no argument
    /// behind it, and it would leave <c>[districts] revisit_ticks</c> doing something other than what
    /// <c>adr/0134</c> says it does. ***A structure derived on a cadence is stale between evaluations,
    /// and that is what a cadence IS*** — a Map Layer is stale between diffusions and nobody calls that
    /// a defect.
    /// </para>
    /// <para>
    /// <b>The half that was removed still holds where it is true</b>, and is asserted there instead:
    /// <see cref="ADistrictCellNamesBuiltGroundWhenEvaluated"/>, a post-condition of the evaluation
    /// rather than a property of the world.
    /// </para>
    /// <para>
    /// ⚠ <b>The <em>dangling</em> half overlaps <see cref="CrossTableHandleResolves"/> and is kept
    /// anyway.</b> That walk is column-driven — it asks every saved handle column in the world whether
    /// it dangles, so it covered this one the day it was declared and it is registered first. What is
    /// left here is the <b>unset</b> handle, which is not dangling and which the generic walk is right
    /// not to report. ***Stating the whole sentence and noting the overlap beats writing down the half
    /// nobody else covers***, which would read as a check with an arbitrary gap in it. A test that
    /// wants to see this member fire calls it directly rather than through the registry.
    /// </para>
    /// </remarks>
    ADistrictCellNamesALiveDistrict = 50,

    /// <summary>
    /// The evaluation left a <c>DistrictCell</c> naming ground that holds no Building.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A post-condition of <c>DistrictWatershed.Evaluate</c>, and it is deliberately not a property
    /// of the world</b> (<c>adr/0134</c>, <c>plans/0003</c> queue item 16). <c>adr/0134</c> makes a
    /// District's extent **built Cells only**, and that is true of what an evaluation produces — it is
    /// not true a Tick later, because Buildings come down between evaluations and the extent is derived
    /// on a cadence. ***So the honest place to ask is immediately after the answer is computed***, which
    /// is where this is asked.
    /// </para>
    /// <para>
    /// ⚠ <b>What it guards is the eviction pass, and that pass is easy to lose.</b>
    /// <c>DistrictWatershed.Evict</c> frees every Cell row the flood no longer covers, and it is the
    /// only thing standing between a demolished Cell and a membership row that outlives every Building
    /// on it for ever. A reconciliation reordered three mechanisms later would drop it silently —
    /// <see cref="ADistrictCellNamesALiveDistrict"/>'s own *right when written and wrong three
    /// mechanisms later*, which is why that member exists and why this one does.
    /// </para>
    /// <para>
    /// ⚠ <b>It is <c>O(extent)</c> on a path that is already <c>O(extent)</c></b>, so it is free in the
    /// sense <c>02 §10</c> cares about — it does not add an order to anything. It is not an end-of-run
    /// check and must not be moved to one, because ***the thing that would make it fail there is the
    /// cadence working.***
    /// </para>
    /// </remarks>
    ADistrictCellNamesBuiltGroundWhenEvaluated = 53,

    /// <summary>
    /// A Business is neither premised nor in the unpremised pool, or is somehow both.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The collection's own bound, checked rather than asserted in prose</b>
    /// (<see href="../../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md">adr/0142</see>,
    /// milestone 25 task 5). Every live Business is in exactly one of two states, and the pool is the
    /// <em>only</em> place an unpremised one can wait — so a Business in neither is a row nothing will
    /// ever reach, which is <c>adr/0006</c> through the back door: it cannot be tenanted, it cannot
    /// give up, and it holds money the supply still counts as issued.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>The State Hash cannot report it and that is the whole argument for the check.</b> Both
    /// halves fold — the severable premises handle and the pool table — and they fold to a perfectly
    /// stable pair of values whichever way they disagree. ***A leak is a row that is CONSISTENTLY
    /// wrong, and consistency is the one thing a hash cannot object to.***
    /// </para>
    /// <para>
    /// ⚠ <b>It reports rather than throwing</b>, on <c>02 §10</c>'s rule and
    /// <see cref="OnlyAnUnhousedHouseholdGivesUp"/>'s precedent: at the write sites the Business
    /// stays where it is, which is the conservative outcome, and the run continues so the whole-world
    /// checks can say what else is wrong.
    /// </para>
    /// </remarks>
    ABusinessIsPremisedOrItIsInThePool = 54,

    /// <summary>
    /// A District that is destroyed either hands its Pool to an heir or hands over nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A write-site check, because the state it forbids does not survive the write that creates
    /// it</b> (<c>02 §10</c>). The moment <c>World.RetirePool</c> frees a Bin holding stock with
    /// nowhere to send it, the units are gone and every later walk finds a world that adds up — the
    /// missing quantity is missing from the only place that could have reported it.
    /// </para>
    /// <para>
    /// 🔴 <b>It is <c>04 §2</c>'s audit standing where a District dies</b> — <em>"if a hundred units
    /// of Food entered the District, a hundred units must be accounted for."</em> A District dies when
    /// no basin claims it, and its heir is whoever now owns its centre Cell; demolish everything in it
    /// and its centre stops being built, so there is no heir and the stock has nowhere to go.
    /// </para>
    /// <para>
    /// ⚠ <b>It cannot fire today, and the reason is exact rather than lucky.</b> <c>Scope.Pool</c>
    /// throws, so nothing in the build can put a unit into a Pool: every Pool is empty at every
    /// moment, so <em>no heir</em> and <em>nothing to hand over</em> coincide for now. ***That is a
    /// property of the build and not of the design***, and this member is what turns the day it stops
    /// being true into a failure rather than a leak. <c>plans/0037</c> task 7 is the day.
    /// </para>
    /// </remarks>
    ADistrictDiesWithAnHeirOrAnEmptyPool = 51,

    /// <summary>
    /// Every District's Pool is one live Bin per Good, and every Pool row names rows that exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two halves and one id, on <see cref="ADistrictCellNamesALiveDistrict"/>'s
    /// reasoning</b>: they are the same sentence read from either end, and a violation of one is
    /// diagnosed by looking at the other.
    /// </para>
    /// <para>
    /// 🔴 <b>The State Hash cannot report either half.</b> <c>Space.DistrictPoolTable</c> is the only
    /// saved statement of which District owns a Pool Bin, and a handle column folds the target row's
    /// monotonic id — which is <b>zero</b> for a freed target. So a Pool row naming a destroyed
    /// District, or a destroyed Bin, is a dangling reference that replay, thread-count and save/reload
    /// equivalence all agree about. ***Two runs reproduce the same wrong answer.***
    /// </para>
    /// <para>
    /// <b>The completeness half is the one that catches a missed <c>World.FitDistrictPools</c>.</b> A
    /// District opened by the watershed and never fitted has no Pool at all, which reads as a District
    /// whose every Good is permanently out of stock — a city that looks starved rather than broken,
    /// and the failure mode <c>02 §5.9</c> is least able to tell from the real thing.
    /// </para>
    /// <para>
    /// ⚠ <b>The <em>dangling</em> half overlaps <see cref="CrossTableHandleResolves"/> and is kept
    /// anyway.</b> That walk is column-driven — it asks every saved handle column in the world whether
    /// it dangles, so it covered this one the day it was declared and it is registered first. What is
    /// left here is the <b>unset</b> handle, which is not dangling and which the generic walk is right
    /// not to report. ***Stating the whole sentence and noting the overlap beats writing down the half
    /// nobody else covers***, which would read as a check with an arbitrary gap in it. A test that
    /// wants to see this member fire calls it directly rather than through the registry.
    /// </para>
    /// </remarks>
    ADistrictPoolIsOneLiveBinPerGood = 52,

    /// <summary>
    /// Nothing has written the terrain table since the ground was laid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0157</c>, milestone 24 task 2. <b>Terrain is written once, at world creation, and never
    /// again</b> — there is no terraforming, so no Tick phase writes
    /// <c>Space.TerrainCellTable</c> at all.
    /// </para>
    /// <para>
    /// 🔴 <b>This is what pays for the Decide guard skipping that table.</b>
    /// <c>Simulation.VerifyDecideWritesNothing</c> folds
    /// <c>Entities.World.TablesAPhaseCanWrite</c> rather than the whole composition, because folding
    /// 262,144 terrain rows twice a Tick made a Tick 138× itself. ⚠ <b>Skipping a table on trust is
    /// the silent hole this project keeps finding</b>, so the trust is replaced by a check rather
    /// than assumed.
    /// </para>
    /// <para>
    /// <b>It is BROADER than what the guard was saying.</b> The guard could only report that
    /// <em>Decide</em> did not move terrain between two folds; this compares the table against its
    /// fingerprint at the moment it was laid, so it reports that <b>no phase at all</b> moved it.
    /// ⚠ <b>It deliberately does NOT ask whether the terrain matches the world key.</b> The first
    /// version did, and reported sixty-three healthy worlds as corrupt: a world built through the
    /// cold API is never populated and a loaded world is restored rather than generated, so neither
    /// has terrain the generator just laid. ***A check derived from the seed cannot be run against
    /// worlds that never met the seed.*** A load restoring the wrong terrain is <c>adr/0112</c>'s
    /// job, and it is done.
    /// </para>
    /// <para>
    /// <b>End-of-run and not per-Tick, which is <c>02 §10</c>'s own sorting.</b> A check on a thing
    /// that cannot change belongs at the lowest frequency there is; running it twice a Tick was the
    /// defect, not the diligence.
    /// </para>
    /// <para>
    /// ⚠ <b>It stops being true the day terraforming ships</b>, at which point terrain leaves
    /// <c>TablesAPhaseCanWrite</c>'s exclusion and this check is replaced rather than relaxed.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>This ordinal was 54 until the merge with <c>main</c> on 2026-08-24</b>, where milestone 25
    /// had independently taken 54 for <see cref="ABusinessIsPremisedOrItIsInThePool"/>. The two enum
    /// members sat on different lines, so <b>git merged them without a conflict and <c>CA1069</c> is
    /// what caught it</b> — the analyser doing the job the merge could not. ***The branch yielded
    /// rather than the trunk***, as it did for the ADR numbers and for <c>PurposeTag</c>.
    /// </remarks>
    TerrainIsUnchangedSinceItWasLaid = 55,
}
