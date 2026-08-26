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
/// <param name="Resource">Which Resource the Bin holds.</param>
/// <param name="Level">What is in it.</param>
/// <param name="Capacity">Its ceiling — <b>always the premises' kind's</b>, whoever holds the level.</param>
/// <param name="Tenant">
/// <b>Whose Bin it is</b> — the Occupant holding the level, or the unset handle when the premises
/// hold it (<c>adr/0141</c>). ⚠ <b>A tenant's Bin appears in this panel at all only because of this
/// field</b>: the list behind it is the Building's own, and milestone 25 task 2 moved `sundries` off
/// it — so the panel printed three Rules drawing from a Bin it did not show. ***An instrument that
/// hides half a transaction is worse than one that shows nothing***, and the capacity column is the
/// reason the two kinds sit in one table rather than two: it comes from the same place for both.
/// </param>
public readonly record struct BinEvidence(
    ResourceId Resource, long Level, long Capacity, Handle<Household> Tenant);

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
/// <param name="Tenant">
/// <b>Whose Rule it is</b> — the Occupant running it, or the unset handle when the premises run it
/// themselves (<c>adr/0141</c>). ⚠ <b>This type named no subject at all until milestone 25 task 4</b>,
/// and it did not need to: a Rule Instance belonged to its Building, and the subject was the enclosing
/// <see cref="BuildingEvidence"/>. ***Task 2 made that false and visible in one step*** — a dwelling
/// holding three Households prints three identical <c>restock</c> rows — which is why <c>adr/0141</c>
/// called this <em>a field, not a redesign</em>.
/// </param>
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
/// <param name="WaitingFor">
/// <b>Which Resource the Bin it is asleep on holds</b>, or <c>default</c> when it is not asleep —
/// <c>adr/0137</c>, and the field that record exists for.
/// <para>
/// ⚠ <b>This is what makes bankruptcy and starvation different sentences.</b>
/// <c>RuleInstanceTable.WaitingOn</c> has always known which Bin stopped a Rule and this type did
/// not, so a Business short of flour and a Business short of money both surfaced as
/// <see cref="Blocked"/> = <c>Supply</c> and nothing downstream could tell them apart.
/// <c>adr/0050</c> called the distinction one that *"falls out of the wait list rather than needing a
/// mechanism"*, ***which was true of the wait list and false of every reader***, because the wait
/// list is not one.
/// </para>
/// <para>
/// <b>Classification is the shell's and stays there.</b> Money is a <c>ResourceFamily</c>, declared
/// by the Ruleset, so <c>Ruleset.IsConserved</c> answers *is this bankruptcy* from this id alone and
/// <c>Core</c> keeps returning ids rather than words (<c>CLAUDE.md</c>, and <c>adr/0137</c>'s
/// *Rejected* refusing a third <see cref="Blocking"/> value for the same reason: that enum
/// distinguishes wait lists by <em>what wakes them</em>, and a money shortfall is woken by a deposit,
/// so it <em>is</em> <c>Supply</c>).
/// </para>
/// </param>
/// <param name="WaitingOn">
/// <b>Whose Bin that is</b>, or <see cref="BinOwnerKind.None"/> when it is not asleep.
/// <para>
/// 🔴 ⚠ <b><c>adr/0137</c> says <em>gains one field</em> and this is the second, because milestone 26
/// task 4 created a wait target that record could not have seen.</b> A buyer blocked on a purchase
/// sleeps on the <b>District market row</b> — <c>adr/0139</c>, and <c>adr/0167</c> for why it is the
/// market and never the shop it drew — so ***the Bin that stopped a Rule need no longer belong to the
/// Building the Rule runs in.*** <see cref="WaitingFor"/> alone would then report <c>sundries</c> for
/// two different cities: a tenant whose own larder is empty, and a District in which nobody is
/// selling. **One is a household's problem and one is the market's**, and the enclosing
/// <see cref="BuildingEvidence"/> no longer disambiguates them the way it did for every Bin before
/// the purchase existed.
/// </para>
/// <para>
/// ⚠ <b>It is an owner <em>kind</em> and not an owner.</b> Which District is a question this panel
/// does not ask and could not answer cheaply — <c>BinTable.Owner</c> is a <c>Handle&lt;Building&gt;</c>
/// and a District names its Bins from the other side (<see cref="BinOwnerKind.District"/>). ***What a
/// reader needs is which sentence to write, and the kind decides that.***
/// </para>
/// </param>
public readonly record struct RuleEvidence(
    RuleId Rule,
    Handle<Household> Tenant,
    Ticks LastRan,
    bool Succeeded,
    Blocking Blocked,
    ConditionId Reported,
    Ticks StarvedSince,
    uint Rate,
    long MissedFirings,
    ResourceId WaitingFor,
    BinOwnerKind WaitingOn);

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
        long pressure,
        long tenantPressure)
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
        TenantPressure = tenantPressure;
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
    /// The <b>premises'</b> accumulated failure pressure, in missed firings — what condemns this
    /// Building.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The largest of its Rules', not the sum</b> — <c>adr/0053</c> as amended, and the sentence
    /// milestone 6 task 2 found had never had a consumer: <em>the Building's pressure is the longest
    /// of its Rules', measured in missed firings</em>, followed by <em>the maximum is never stored
    /// anywhere</em>. It is stored nowhere still; this recomputes it, which is what an assembler is
    /// for.
    /// </para>
    /// <para>
    /// ⚠ <b>AMENDED at milestone 25 task 4: <em>its Rules'</em> now means the ones the premises run
    /// themselves</b> (<c>adr/0141</c>). A tenant's pressure ends the <b>tenancy</b> and leaves the
    /// premises standing, so folding it in here would print a number against a <c>condemn_after</c>
    /// that no longer governs it — ***a panel reporting the Building as about to fall down because
    /// somebody living in it went hungry***, which is the defect this milestone exists to remove,
    /// arriving in the instrument instead of in the engine. See <see cref="TenantPressure"/>.
    /// </para>
    /// </remarks>
    public long Pressure { get; }

    /// <summary>
    /// The worst <b>tenant's</b> failure pressure, in missed firings — what ends a tenancy.
    /// </summary>
    /// <remarks>
    /// <b>The largest across every tenant and every Rule of theirs, which is the same shape one level
    /// out.</b> ⚠ <b>It is a maximum over the whole Building and NOT per tenant</b>, and that is a
    /// limitation rather than a design: the verdict is made per tenant in
    /// <c>ZoneRuleEngine.Condemn</c>, so a Building whose worst tenant is past the threshold prints
    /// one number that does not say which family. <see cref="RuleEvidence.Tenant"/> is where a reader
    /// gets that, per row.
    /// </remarks>
    public long TenantPressure { get; }
}
