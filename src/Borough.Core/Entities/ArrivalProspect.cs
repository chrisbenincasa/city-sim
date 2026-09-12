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
/// <para>
/// 🔴 <b>The first individual in the Outside's story.</b> A <see cref="HinterlandPopulationTable"/>
/// row is a count of Households that are alike; nobody in it has a purse, a taste or a name. This is
/// what one of them becomes on the occasion it considers the city — and it is the same person the
/// gate admits, which is the whole of <c>plans/0073</c> D4. The old prospect path drew a purse to
/// test affordability and the admitted Household drew another on its own id, so ***the family that
/// could afford to come was not the family that came***.
/// </para>
/// <para>
/// <b><see cref="Identity"/> is what carries across that seam.</b> It is the coordinate every
/// preference is drawn on — centrality taste, purse — before the Household exists, and
/// <see cref="HouseholdTable.ChoiceIdentity"/> is where it lands afterwards, so a Life Stage
/// transition recomputes the same family's preferences rather than a new stranger's.
/// </para>
/// <para>
/// ⚠ <b>It reserves nothing and promises nothing.</b> A prospect holds a group handle so admission
/// can debit the row it came from; it does not hold a dwelling, a gate or a place in a queue. The
/// home it compared the Outside against is evidence that it wanted to come, and the city may house
/// it somewhere else entirely.
/// </para>
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
    /// <b>The purse is drawn here and nowhere else afterwards.</b> Everything particular about this
    /// family is a function of <paramref name="identity"/> and the Rules in force, so a prospect that
    /// waits outside and is reconsidered later is the same family facing a changed city rather than a
    /// fresh draw that happens to stand in the same place.
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
/// <para>
/// <b>An enum rather than a <c>bool</c>, because five of these are ordinary and one is a defect.</b>
/// A full gate and a spent stock are the mechanism working — <c>plans/0073</c> D7 — and a caller that
/// names a Building admitting nobody has made a mistake. Collapsing them would put the ordinary
/// operation of a daily ceiling into the crash artifact and leave the real fault indistinguishable
/// from a busy Day, which is <c>World.TryArrive</c>'s own distinction made readable.
/// </para>
/// <para>
/// ⚠ <b>Every refusal is checked before the first write.</b> There is no outcome here that means
/// <em>half a family arrived</em>: allocation failure is not an ordinary rejection and does not
/// appear in this enum.
/// </para>
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
