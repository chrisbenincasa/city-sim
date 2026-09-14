namespace Borough.Core.Entities;

using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// One family standing at the edge of the map, deciding.
/// </summary>
/// <remarks>
/// The purse and choice identity survive queueing and admission. Composition and edge must
/// match the live stock row; admission creates the individual Citizens.
/// </remarks>
/// <param name="Group">The stock row this family stands in. Admission debits exactly this row.</param>
/// <param name="Edge">Which edge it stands behind.</param>
/// <param name="Composition">Who it is made of, exactly as the Outside stores it.</param>
/// <param name="Purse">What it carries, drawn once inside its band and endowed unchanged.</param>
/// <param name="Identity">
/// The coordinate its preferences are drawn on, and the one the admitted Household keeps.
/// </param>
public readonly record struct ArrivalProspect(
    Handle<HinterlandPopulation> Group,
    MapEdge Edge,
    HinterlandComposition Composition,
    Money Purse,
    ulong Identity)
{
    /// <summary>The Life Stage this family is in.</summary>
    public byte Stage => Composition.Stage;

    /// <summary>How many people would cross with it.</summary>
    public int Members => Composition.Members;

    /// <summary>
    /// The family a group of <paramref name="composition"/> presents on this occasion.
    /// </summary>
    /// <remarks>
    /// Draw the purse once. Reviews reuse it and the identity with the current Ruleset preferences.
    /// </remarks>
    /// <param name="key">The world seed.</param>
    /// <param name="source">The economy behind the edge, which owns the purse bands.</param>
    /// <param name="group">The stock row presenting it.</param>
    /// <param name="edge">The edge that row stands behind.</param>
    /// <param name="composition">Who the row's Households are made of.</param>
    /// <param name="identity">This occasion's identity, unique across the world's whole run.</param>
    public static ArrivalProspect Of(
        WorldKey key,
        in HinterlandDefinition source,
        Handle<HinterlandPopulation> group,
        MapEdge edge,
        in HinterlandComposition composition,
        ulong identity) =>
        new(
            group,
            edge,
            composition,
            source.BandBalance(key, identity, composition.MoneyBand),
            identity);
}

/// <summary>
/// What a gate did with a prospect. <b><see cref="Admitted"/> is the only outcome that wrote
/// anything.</b>
/// </summary>
/// <remarks>
/// Ordinary refusals leave stock, quota, Money and population unchanged. Allocation failure is fatal.
/// </remarks>
public enum Admission : byte
{
    /// <summary>They came in. Stock, quota, population and the Pool all moved.</summary>
    Admitted = 0,

    /// <summary>The stock row is gone — retired, or a handle held across a save.</summary>
    GroupIsGone,

    /// <summary>The prospect does not describe the group it names.</summary>
    ProspectIsNotOfThatGroup,

    /// <summary>Nobody of this composition is left unpromised behind the edge.</summary>
    StockIsSpent,

    /// <summary>The Building named is not a live Outside Connection.</summary>
    GateAdmitsNobody,

    /// <summary>The gate stands on a different edge from the one this family came from.</summary>
    GateIsOnAnotherEdge,

    /// <summary>The gate has taken its <c>arrivals_per_day</c> already.</summary>
    GateIsFullToday,
}

/// <summary>What one family made of the city on one occasion.</summary>
/// <remarks>
/// NoSample means there was no feasible city alternative; StayedOutside means the family
/// compared feasible alternatives and preferred the Outside.
/// </remarks>
public enum ProspectOutcome : byte
{
    /// <summary>Nothing here it could afford or fit in, so there was nothing to weigh.</summary>
    NoSample = 0,

    /// <summary>It weighed the city against home and stayed at home.</summary>
    StayedOutside,

    /// <summary>It chose the city.</summary>
    Willing,
}
