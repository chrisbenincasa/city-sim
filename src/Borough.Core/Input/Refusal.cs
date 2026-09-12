namespace Borough.Core.Input;

/// <summary>
/// Why a <see cref="Command"/> would not apply — <b>the refusal as a number, so that somebody else
/// can own the sentence.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every member here is a refusal <see cref="Simulation"/> already made, and none is a new
/// rule.</b> The rules were reachable in one way only: as the message of an
/// <c>InvalidOperationException</c> thrown out of Phase 0. That is the right artefact for a log — a
/// replay that diverges from the session it describes must say so and stop — and it is the wrong one
/// for a person, because an exception out of <c>Apply</c> aborts <c>Step</c> half way and leaves a
/// world no invariant covers. ***A crash is not the worst outcome of an unguarded click; a
/// half-stepped world is.***
/// </para>
/// <para>
/// 🔴 <b>So the shell guarded three of them by restating the rule in its own words</b>, which is
/// <c>plans/0012</c> <b>Cause 1</b> by construction — two places storing one rule, and the copy is
/// the one that drifts. ⚠ <b>Three of the ten that belong to the five verbs it issues</b>, of seventeen in all.
/// <see cref="Simulation.Refuses"/> answers the same question the applier answers, off the same
/// predicate, so a shell that declines to send is declining on the core's own finding rather than on
/// a paraphrase of it.
/// </para>
/// <para>
/// ⚠ <b>A number and not a string, and that is <c>CLAUDE.md</c>'s leak vector rather than a
/// preference.</b> <em>"<c>Core</c> returns ids and numbers, never human-readable strings … the real
/// leak vector is not <c>using Godot;</c> — it is a method that returns a formatted string because a
/// panel wanted one."</em> The words a player reads are the shell's, resolved through the Ruleset,
/// and a second front end may word them differently or in another language.
/// </para>
/// <para>
/// ⚠ <b>The exception messages stay where they were</b> and are composed from these codes. They are
/// diagnostics in a crash artefact rather than a panel's copy, which is a different reader with a
/// different need: one wants the ADR number and the successor mechanism, the other wants to know why
/// the click did nothing.
/// </para>
/// </remarks>
public enum Refusal : ushort
{
    /// <summary>The command applies. <b>Zero, so a defaulted answer is not a refusal by accident.</b></summary>
    None = 0,

    /// <summary>The verb is declared in <see cref="CommandKind"/> and not applied in this build.</summary>
    VerbNotApplied = 1,

    /// <summary>
    /// <c>Connect</c> names a road kind that is not a Street. <c>adr/0077</c> defers Arterials and
    /// Junction pieces by name.
    /// </summary>
    ConnectRoadKindIsNotStreet = 2,

    /// <summary>
    /// <c>Connect</c> names a lattice this world has not got — the Ruleset states no
    /// <c>[roads] block_tiles</c>.
    /// </summary>
    ConnectWorldHasNoLattice = 3,

    /// <summary>
    /// <c>Trip</c> is commanded against a Ruleset declaring no <c>[trips]</c>, so a crossing has no
    /// cost and the Commute Budget has no place to fall.
    /// </summary>
    TripRulesetStatesNoTrips = 4,

    /// <inheritdoc cref="ConnectWorldHasNoLattice"/>
    TripWorldHasNoLattice = 5,

    /// <summary><c>Trip</c> names a block holding no occupied Building, at one end or both.</summary>
    TripBlockHoldsNobody = 6,

    /// <summary><c>Trip</c> resolves both endpoints to one Building.</summary>
    TripEndpointsAreOneBuilding = 7,

    /// <summary><c>Trip</c> names an origin with no Citizen in it to be a Traveller.</summary>
    TripOriginHoldsNoCitizen = 8,

    /// <summary><c>Arrive</c> names a Tile where no Outside Connection stands.</summary>
    ArriveNoGateOnThatTile = 9,

    /// <summary><c>Govern</c> names a position past the Ruleset's declared <c>[[policy]]</c> set.</summary>
    GovernNoSuchPolicy = 10,

    /// <summary>
    /// <c>Govern</c> names a Policy this world holds no governable row for, because the table is
    /// sized at world creation and a reload that grew the set does not resize it.
    /// </summary>
    GovernPolicyNotInThisWorld = 11,

    /// <summary>
    /// <c>Govern</c> names a <c>[[policy]]</c> stating no <c>name</c>. A governed amount is saved
    /// state, and a name is the only thing that survives a renumbering.
    /// </summary>
    GovernPolicyHasNoName = 12,

    /// <summary><c>Demolish</c> names a Tile where no Building stands.</summary>
    DemolishNoBuildingOnThatTile = 13,

    /// <summary>
    /// <c>Demolish</c> names a Building somebody is still in. Clearing occupied ground is
    /// <c>adr/0091</c>'s compulsory purchase, whose price that ADR refuses to compose.
    /// </summary>
    DemolishBuildingIsOccupied = 14,

    /// <summary><c>Service</c> names a Building kind this Ruleset does not declare.</summary>
    ServiceKindNotDeclared = 15,

    /// <summary>
    /// <c>Service</c> names a kind declaring no <c>serves</c> key. <c>01 §5</c> makes this verb the
    /// design's one placement exception, and an ordinary kind is not in it.
    /// </summary>
    ServiceKindServesNothing = 16,

    /// <summary><c>Service</c> names a Tile holding no vacant Lot — a shell is not vacant.</summary>
    ServiceNoVacantLotOnThatTile = 17,

    /// <summary>
    /// <c>People</c> is commanded on a world that already holds a population. World creation belongs
    /// at Tick 0 and once — a second application would build a city of twice the configured size
    /// into tables sized for one.
    /// </summary>
    PeopleWorldAlreadyHasAPopulation = 18,

    /// <summary>
    /// <c>People</c> is commanded on a world with no Lots, so there is nowhere to put anybody. On a
    /// world laid by <c>Connect</c> the Lots come from <c>Zone</c>, which carves against the Street
    /// faces that are standing.
    /// </summary>
    PeopleWorldHasNoLots = 19,

    /// <summary>
    /// <c>Tax</c> names a control that is not one of <see cref="TaxControl"/>'s seven. The selector is
    /// the verb's whole payload beside the value, so a number nothing declares is a command with no
    /// subject rather than a setting to fall back from.
    /// </summary>
    TaxControlNotDeclared = 20,

    /// <summary><c>Tax</c> sets a marginal rate outside 0..100. A percentage is a percentage.</summary>
    TaxRateOutOfRange = 21,

    /// <summary>
    /// <c>Tax</c> sets a negative tax-free allowance. An allowance is the earnings below which
    /// nothing is due, and a negative one is not a heavier tax — it is a threshold no Day can be on
    /// the wrong side of.
    /// </summary>
    TaxAllowanceIsNegative = 22,

    /// <summary>
    /// <c>Tax</c> would leave the upper marginal rate below the middle one — <c>plans/0072</c> D7.
    /// Both rates are marginal, so take-home income would step <em>downward</em> at the threshold: a
    /// Citizen keeps less for having earned more.
    /// </summary>
    TaxUpperRateBelowMiddleRate = 23,

    /// <summary>
    /// <c>Tax</c> would leave the upper band starting below the allowance. A band that opens before
    /// taxation does is not a band.
    /// </summary>
    TaxUpperThresholdBelowAllowance = 24,

    /// <summary>
    /// <c>Tax</c> sets a negative Business profit threshold. Profit itself ranges below zero, so
    /// the obvious reading of a negative threshold is that it means something — it does not.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A quantity's range is not its boundary's range.</b> The threshold is where the upper
    /// band opens, and opening it below zero puts the whole lower band where no profit can reach
    /// it, leaving <c>ProfitLowerRate</c> reading as a setting while applying to nothing. A loss is
    /// already untaxed — <c>plans/0072</c> D26 — and needs no negative threshold to say so.
    /// </remarks>
    TaxProfitThresholdIsNegative = 25,

    /// <summary>
    /// <c>Tax</c> would leave the upper marginal profit rate below the lower one —
    /// <c>plans/0072</c> D8, and <see cref="TaxUpperRateBelowMiddleRate"/>'s argument applied to
    /// profit instead of earnings.
    /// </summary>
    /// <remarks>
    /// Both are marginal, so a rate that fell as profit rose would make post-tax profit step
    /// <em>downward</em> at the threshold: a Business that made one unit more would keep less than
    /// one that made one unit less. That is not relief on large profits, it is a schedule that has
    /// stopped being monotone.
    /// </remarks>
    TaxProfitUpperRateBelowLowerRate = 26,

    /// <summary>
    /// <c>Fund</c> names a Policy that pays nobody. A funding ceiling belongs to a subsidy, and
    /// every other tool has nothing to ration.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It reads as a setting on any Policy in the list</b>, because the panel shows one row per
    /// Policy and a ceiling is just another number to type. A charge collects whatever is owed and a
    /// relief moves no Money at all, so a ceiling against either would be saved, hashed, carried
    /// across a reload and consulted by nothing.
    /// </remarks>
    FundPolicyPaysNobody = 27,

    /// <summary>
    /// <c>Fund</c> sets a negative funding ceiling. A ceiling of zero is a subsidy switched off and
    /// is the way to spell that; below zero it has no reading.
    /// </summary>
    FundCeilingIsNegative = 28,

    /// <summary>
    /// <c>Service</c> names a kind whose <c>placement_cost</c> is more than the treasury holds. The
    /// city pays for its own placements in full or not at all.
    /// </summary>
    /// <remarks>
    /// <b>It is the first refusal that turns on a LEVEL rather than on a shape</b>, so unlike every
    /// other one here it can answer differently for the same command two Ticks apart — a levy pays
    /// in, and the click the shell greyed out is live again. A front end asking this every frame
    /// gets the right answer every frame, which is why it is asked rather than cached.
    /// </remarks>
    ServiceTreasuryCannotPay = 29,
}
