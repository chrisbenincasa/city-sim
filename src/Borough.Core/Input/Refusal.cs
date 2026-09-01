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
}
