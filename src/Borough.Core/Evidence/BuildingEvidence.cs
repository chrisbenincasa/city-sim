namespace Borough.Core.Evidence;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>One of a Building's Bins, with what is in it and what it holds.</summary>
/// <param name="Resource">Which Resource.</param>
/// <param name="Level">How much is in it.</param>
/// <param name="Capacity">
/// What it holds under the Ruleset in force. <b>Derived rather than frozen at construction</b>
/// (<c>adr/0064</c>), so a reload can move it under a standing Building.
/// </param>
public readonly record struct BinEvidence(ResourceId Resource, long Level, long Capacity);

/// <summary>
/// One of a Building's Rule Instances: when it last ran, whether that worked, and what it is waiting
/// on if it did not.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="LastRan"/> is derived and the derivation is worth stating, because it is the whole
/// of <c>02 §9</c>'s <em>which Rule it last ran and whether it succeeded</em>.</b> A Rule Instance is
/// either armed on the Event Wheel or asleep on a Bin's wait list, never both and never neither
/// (<c>RuleInstanceTable</c>'s own opening line). <c>EventWheel.Arm</c> is the only writer of
/// <c>NextTick</c>. So a rule that <b>succeeded</b> re-armed at <c>+rate</c> and last ran at
/// <c>NextTick − rate</c>; a rule that <b>failed</b> is on a wait list with <c>NextTick</c> left at
/// the Tick it fired on. Two states, one column, and both recoverable.
/// </para>
/// <para>
/// ⚠ <b>It cannot distinguish <em>last ran at T</em> from <em>has never run</em>, and nothing in the
/// build can.</b> A Building's Rules are armed at construction with a stagger uniform in
/// <c>[1, rate]</c>, so a Rule that has never fired reports a <see cref="LastRan"/> at or just before
/// the Building was raised. Separating them needs a <b>Building creation Tick</b>, and
/// <c>BuildingTable</c> has no such column. It is harmless for the aggregate question — a
/// never-fired Rule's phantom is earlier than any real firing, so it loses the <em>which ran last</em>
/// comparison to any Rule that has actually run — and it is wrong in exactly one case, a Building
/// none of whose Rules has ever fired. **Recorded rather than papered over**: adding the column is a
/// hash move somebody should make for a reason, not as a side effect of an inspector.
/// </para>
/// </remarks>
/// <param name="Rule">Which Rule.</param>
/// <param name="LastRan">When it last fired, subject to the caveat above.</param>
/// <param name="Succeeded">Whether that firing worked. Equivalently, whether it is armed rather than asleep.</param>
/// <param name="Blocked">What it is asleep on — <c>Supply</c> is an empty input, <c>Space</c> a full output.</param>
/// <param name="Reported">
/// The condition it reports, which is where its fallback chain terminated. <c>ConditionId.None</c>
/// when it is not blocked.
/// </param>
/// <param name="StarvedSince">
/// When the current run of failures began, or <c>default</c> when it is not starved. <b>The pressure
/// clock</b> — <c>adr/0053</c> measures condemnation in missed firings, so this and
/// <see cref="RuleEvidence.Rate"/> are the two terms behind it.
/// </param>
/// <param name="Rate">How often the Rule fires when healthy, in Ticks.</param>
/// <param name="MissedFirings">
/// How many firings this Rule has missed, <c>(now − StarvedSince) ÷ Rate</c>, or zero when it is
/// healthy. <b>This is the Building's failure pressure, per Rule</b>, and the Building's own is the
/// largest of them (<c>adr/0053</c> as amended, and milestone 6 task 2's finding).
/// </param>
public readonly record struct RuleEvidence(
    RuleId Rule,
    Ticks LastRan,
    bool Succeeded,
    Blocking Blocked,
    ConditionId Reported,
    Ticks StarvedSince,
    uint Rate,
    long MissedFirings);

/// <summary>
/// <c>02 §9</c>'s Building answer: who is in it, what is in its Bins, what its Rules are doing, and
/// what it is under pressure from.
/// </summary>
/// <remarks>
/// <para>
/// <b>Entirely assembled from live state — this type stores nothing and no accumulator backs it.</b>
/// That is milestone 6's D2 working as intended: a Building is standing while the question is asked,
/// so every part of the answer is still in the world and re-reading it on a cold path is cheaper and
/// more honest than shadowing it.
/// </para>
/// <para>
/// <b>Cold and allocating</b> — see <see cref="ColdPathAttribute"/>. The spans are copies, because
/// the lists behind them are intrusive and a caller holding one across a Tick would be holding slots
/// that recycle.
/// </para>
/// </remarks>
[ColdPath("02 §9's Building answer, assembled when a panel asks. No path from step() reaches it.")]
public readonly struct BuildingEvidence
{
    internal BuildingEvidence(
        Handle<Building> building,
        byte kind,
        Handle<Lot> lot,
        bool declared,
        int declaredOccupancy,
        int declaredJobs,
        Handle<Household>[] occupants,
        Handle<Citizen>[] workers,
        BinEvidence[] bins,
        RuleEvidence[] rules,
        long pressure)
    {
        Building = building;
        Kind = kind;
        Lot = lot;
        IsDeclared = declared;
        DeclaredOccupancy = declaredOccupancy;
        DeclaredJobs = declaredJobs;
        Occupants = occupants;
        Workers = workers;
        Bins = bins;
        Rules = rules;
        Pressure = pressure;
    }

    /// <summary>Which Building this is about.</summary>
    public Handle<Building> Building { get; }

    /// <summary>Its kind, as an id the host resolves through the Ruleset.</summary>
    public byte Kind { get; }

    /// <summary>The Lot it stands on.</summary>
    public Handle<Lot> Lot { get; }

    /// <summary>
    /// Whether the Ruleset in force declares this kind at all.
    /// </summary>
    /// <remarks>
    /// False means <b>derelict</b> (<c>adr/0068</c>): it keeps the occupants and workers it has and
    /// takes no new ones, because a designer deleting a paragraph must not sack a District. The two
    /// declared counts below are meaningless when this is false.
    /// </remarks>
    public bool IsDeclared { get; }

    /// <summary>How many Households its kind houses.</summary>
    public int DeclaredOccupancy { get; }

    /// <summary>How many Citizens its kind employs.</summary>
    public int DeclaredJobs { get; }

    /// <summary>The Households living here, in list order.</summary>
    public ReadOnlyMemory<Handle<Household>> Occupants { get; }

    /// <summary>The Citizens working here, in list order.</summary>
    public ReadOnlyMemory<Handle<Citizen>> Workers { get; }

    /// <summary>Its Bins.</summary>
    public ReadOnlyMemory<BinEvidence> Bins { get; }

    /// <summary>Its Rule Instances.</summary>
    public ReadOnlyMemory<RuleEvidence> Rules { get; }

    /// <summary>
    /// The Building's accumulated failure pressure, in missed firings.
    /// </summary>
    /// <remarks>
    /// <b>The largest of its Rules', not the sum</b> — <c>adr/0053</c> as amended, and the sentence
    /// milestone 6 task 2 found had never had a consumer: <em>the Building's pressure is the longest
    /// of its Rules', measured in missed firings</em>, followed by <em>the maximum is never stored
    /// anywhere</em>. It is stored nowhere still; this recomputes it, which is what an assembler is
    /// for.
    /// </remarks>
    public long Pressure { get; }
}
