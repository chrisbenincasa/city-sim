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

    /// <summary>A Household's home is a Building that is still there.</summary>
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
    /// Layer with a source and no decay is exactly the shape that violates it.</b> Pollution
    /// accumulates from every emission and nothing removes it, so the long-run test is where this is
    /// found — <c>plans/0009</c> task 10 registers it in the end-of-run tier for that reason rather
    /// than because it is expensive.
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
}
