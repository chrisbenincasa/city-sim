namespace Borough.Core.Evidence;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Why a Lot is vacant, as far as the build can say.
/// </summary>
/// <remarks>
/// <para>
/// <b>A flag set rather than a single reason</b>, because more than one can hold at once and picking
/// between them would be the assembler deciding which the player cares about. That is the host's
/// judgement and <c>adr/0002</c> gives it the host.
/// </para>
/// <para>
/// ⚠ <b><c>02 §9</c> names four reasons and this enum declares two, deliberately.</b> That section
/// asks for <em>no frontage, no household in the queue that would accept it, conditions below
/// tolerance, or no capital</em> — and the last two have <b>no mechanism anywhere in the build</b>:
/// <c>MapLayers.Desirability</c> throws by design (<c>02 §2.4</c>, <c>adr/0034</c>) and there is no
/// capital, price or bid concept at all. <b>Declaring flags for them would be worse than omitting
/// them</b>, because a flag that can never be set reads as <em>checked and not the reason</em> rather
/// than as <em>not measured</em> — session F's ***a placeholder whose value sits inside the range of
/// legitimate answers cannot announce itself***, on a bit instead of a number. They arrive with
/// milestone <b>17</b>, which owns decline and desirability alike, and adding a flag then is additive.
/// </para>
/// </remarks>
[System.Flags]
public enum VacancyReason
{
    /// <summary>No reason applies — either the Lot is built on, or nothing here explains it.</summary>
    /// <remarks>
    /// ⚠ <b>Zero on a vacant Lot is a real answer and it means <em>the build cannot say</em>.</b> It
    /// is reachable today: a Lot with frontage, admitted by a Zone Rule, with somebody in the Pool,
    /// is one the sampler simply has not reached yet — the Zone Rule samples rather than sweeps
    /// (<c>adr/0059</c>), so <em>not yet looked at</em> is the ordinary state of most vacant Lots and
    /// is not a defect in anything.
    /// </remarks>
    None = 0,

    /// <summary>
    /// The Lot touches no Street, so nothing can be addressed here.
    /// </summary>
    /// <remarks>
    /// <c>LotTable.HasFrontage</c>. Derived on the Segment Epoch, so this answer is as current as the
    /// last road edit — see <c>adr/0078</c>.
    /// </remarks>
    NoFrontage = 1,

    /// <summary>
    /// The Unplaced Pool is empty, so no Zone Rule would build here whatever else were true.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Named for what it measures rather than for <c>02 §9</c>'s wording.</b> That section says
    /// <em>no household in the queue <b>that would accept it</b></em>, and the acceptance half does
    /// not exist: <c>02 §5.4</c>'s residential choice model is unbuilt, and the 2026-08-15 corpus
    /// sweep found it had been invisible to <c>06</c>'s inventory for its whole life. What
    /// <c>ZoneRuleEngine.Create</c> actually tests is <c>UnplacedPool.Count == 0</c>, so this flag
    /// says <em>the queue is empty</em> and claims nothing about acceptability.
    /// </remarks>
    NobodySeeking = 2,

    /// <summary>
    /// No <c>[[zone_rule]]</c> in the Ruleset in force admits this Lot's zone bits.
    /// </summary>
    /// <remarks>
    /// The other clause of <c>ZoneRuleEngine.Create</c>'s predicate,
    /// <c>(Lots.Zone[lot] &amp; definition.Admits) == 0</c>, quantified over every declared Zone Rule.
    /// <b>Not one of <c>02 §9</c>'s four</b>, and included because it is the commonest honest answer
    /// on a map the player has not zoned — the question is <em>why is nothing building here</em>, and
    /// <em>you have not asked for anything to</em> is a complete answer to it.
    /// </remarks>
    NotZoned = 4,
}

/// <summary>
/// What was demolished on a Lot, and the Rule condition that condemned it.
/// </summary>
/// <remarks>
/// Read out of <see cref="CondemnationTrailTable"/>, which milestone 6 task 2 built for exactly this:
/// the condemning condition is freed with the Building's Rule Instances one line later, so it is
/// recorded at the moment of the decision or not at all.
/// </remarks>
/// <param name="Tick">When the Building was condemned.</param>
/// <param name="Kind">The Building kind that stood here.</param>
/// <param name="Condition">
/// The condition the condemning Rule reports. <b>The Rule with the most missed firings</b>, not the
/// first past its threshold — a Building's pressure is the longest of its Rules' (<c>adr/0053</c>).
/// </param>
public readonly record struct CondemnationEvidence(Ticks Tick, byte Kind, ConditionId Condition);

/// <summary>
/// <c>02 §9</c>'s Lot answer: <em>why it is vacant. Not "vacant" — why.</em>
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §9</c> calls this "the hardest and the most valuable"</b> — <em>"why is nothing building
/// here?" is the question every city-builder player asks and no city builder answers</em> — and it is
/// worth being exact about how much of it this answers. Of the four reasons that section names,
/// <b>two are computable</b> (frontage, an empty queue), <b>one is recorded</b> where the Lot was
/// vacated by demolition (<see cref="Condemnation"/>, task 2), and <b>two are named holes</b> with no
/// mechanism behind them. This reports what the build has and manufactures none of the rest.
/// </para>
/// <para>
/// <b>Cold and allocating</b>, on <c>Instruments.Series</c>'s precedent — see
/// <see cref="ColdPathAttribute"/>. Nothing on the hot path reaches it.
/// </para>
/// </remarks>
[ColdPath("02 §9's Lot answer, assembled when a panel asks. No path from step() reaches it.")]
public readonly struct LotEvidence
{
    internal LotEvidence(
        Handle<Lot> lot,
        ushort zone,
        bool vacant,
        Address address,
        VacancyReason reason,
        CondemnationEvidence? condemnation,
        Handle<Building> building)
    {
        Lot = lot;
        Zone = zone;
        IsVacant = vacant;
        Address = address;
        Reason = reason;
        Condemnation = condemnation;
        Building = building;
    }

    /// <summary>Which Lot this is about.</summary>
    public Handle<Lot> Lot { get; }

    /// <summary>The Lot's permission set — one bit per admitted Building kind.</summary>
    public ushort Zone { get; }

    /// <summary>Whether anything stands here.</summary>
    public bool IsVacant { get; }

    /// <summary>
    /// Where the Lot is on the Road Graph, or <c>Address.None</c> when it has no frontage.
    /// </summary>
    public Address Address { get; }

    /// <summary>
    /// Why it is vacant. <see cref="VacancyReason.None"/> on an occupied Lot, and also on a vacant one
    /// the build cannot explain — see that member.
    /// </summary>
    public VacancyReason Reason { get; }

    /// <summary>
    /// The last demolition recorded on this Lot, if the trail still holds one.
    /// </summary>
    /// <remarks>
    /// <b>Absent does not mean <em>never demolished</em>.</b> The trail retains
    /// <c>CondemnationTrailTable.Retained</c> entries and folds older ones into an aggregate that
    /// keeps the count and drops the identity — ***attribution decays to magnitude***. So this is
    /// <em>no entry survives for this Lot</em>, which on a busy city is the ordinary state of an old
    /// demolition.
    /// </remarks>
    public CondemnationEvidence? Condemnation { get; }

    /// <summary>What stands here, or the unset handle when <see cref="IsVacant"/>.</summary>
    public Handle<Building> Building { get; }
}
