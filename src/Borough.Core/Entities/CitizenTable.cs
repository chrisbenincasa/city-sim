namespace Borough.Core.Entities;

using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// People. The largest table by row count, and the one whose per-Tick width the Event Wheel's
/// argument rests on.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three columns are <see cref="Touch.PerTick"/> and the rest are not, which is the whole
/// point.</b> S4 task 2 found the corpus's <em>"on the order of 40 bytes hot"</em> was never one
/// number: the row is 13 B under the per-Tick reading — <c>next_event_tick</c>, the Wheel link and
/// the current activity — and 51 B under the working-set one. The two size different things: the
/// per-Tick figure sizes the Wheel drain and the wake gather, the working-set figure sizes the world
/// and the save copy. K0 then measured that only 13% of the world is addressable on an ordinary Tick,
/// which is the Event Wheel's premise made arithmetic.
/// </para>
/// <para>
/// <b><c>wheel_next</c> is declared here and used in slice 9, and its disposition is provisional.</b>
/// It is the intrusive link into an Event Wheel bucket, and it is <see cref="Disposition.Derived"/>
/// because the bucket a Citizen sits in is a pure function of <c>next_event_tick</c> — the Wheel is
/// an index over saved state, not state. <b>That is true of membership and is unproven of order.</b>
/// Under the rule <c>05 §3</c> now states — a list may be derived only if its <em>order</em> is
/// recoverable, not merely its membership — this holds only if nothing observable depends on the
/// order Citizens are woken in within one Tick. Phase 2 is read-only so Decide cannot be
/// order-dependent, and <c>02 §8</c> rule 5 settles contested outcomes by shuffle rather than by
/// arrival; slice 9 has to make that claim explicitly rather than inherit it, and if it fails this
/// column becomes <see cref="Disposition.Saved"/> exactly as the Bin wait list did.
/// </para>
/// </remarks>
[Table]
public sealed class CitizenTable
{
    private readonly Rows<Citizen> _rows;

    /// <param name="capacity">Initial slot count. The population; 1M is a floor rather than a cap.</param>
    /// <param name="households">The table this one's household handles address.</param>
    /// <param name="buildings">The table this one's workplace handles address.</param>
    public CitizenTable(int capacity, HouseholdTable households, BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(households);
        ArgumentNullException.ThrowIfNull(buildings);

        _rows = new Rows<Citizen>("citizen", capacity, Buffering.OneCopy);

        NextEventTick = _rows.Saved<Ticks>("next_event_tick", Touch.PerTick);
        CommuteNext = _rows.Derived<int>("commute_next", Touch.PerTick);
        CommuteReturnNext = _rows.Derived<int>("commute_return_next", Touch.PerTick);
        CommuteBucket = _rows.Derived<int>("commute_bucket", Touch.PerTick);
        PlannedCommute = _rows.Saved<Ticks>("planned_commute");
        Activity = _rows.Saved<byte>("activity", Touch.PerTick);

        HouseholdOf = _rows.SavedHandle("household", households.Rows);
        // Severable, and slice 10 is what forced the declaration. A Citizen's Workplace is somebody
        // else's Building, so demolition can free it out from under this handle — and a workplace
        // that no longer resolves is the job no longer existing, which is the fact and not a break
        // in it. The reverse index this note once said did not exist is `WorkerNext` below, built by
        // milestone 5b-bis task 2; it does not make the handle unseverable, because it is derived
        // *from* the handle and a demolition still leaves the Citizen pointing at a freed row.
        Workplace = _rows.SavedHandle(
            "workplace", buildings.Rows, reference: Reference.Severable);
        Experience = _rows.Saved<long>("experience");
        SkillTier = _rows.Saved<byte>("skill_tier");
        Employment = _rows.Saved<byte>("employment");
        ReachFailures = _rows.Saved<ushort>("reach_failures");
        MemberNext = _rows.Derived<int>("member_next");
        WorkerNext = _rows.Derived<int>("worker_next");

        Age = _rows.Saved<ushort>("age", Touch.Cold);
        Health = _rows.Saved<byte>("health", Touch.Cold);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Citizen> Rows => _rows;

    /// <summary>The Tick this Citizen next wakes on. The Event Wheel's bucket key.</summary>
    public Column<Ticks> NextEventTick { get; }

    /// <summary>
    /// Link in this Citizen's departure bucket — see <c>Movement.CommuteRoster</c>.
    /// </summary>
    /// <remarks>
    /// <b>Declared in slice 4 as <c>wheel_next</c>, for an Event Wheel that never carried a Citizen,
    /// and read by nothing for five slices.</b> Renamed rather than twinned when 5b-bis task 5 needed
    /// exactly this shape: a second column beside a dead one of the same type is 4 MB at 1M Citizens
    /// and a name that means two things. The Rule engine's Wheel threads
    /// <c>RuleInstanceTable.QueueNext</c> and never wanted this.
    /// <para>
    /// It is <b>not</b> a Wheel link now, and the distinction is the whole of task 5's design: a
    /// commute recurs every Day, the Wheel is a Day long, so an armed Citizen would never change
    /// bucket — which makes the bucket a partition on a constant rather than a schedule.
    /// </para>
    /// </remarks>
    public Column<int> CommuteNext { get; }

    /// <summary>
    /// Link in this Citizen's <em>return</em> bucket — the second half of <c>Movement.CommuteRoster</c>.
    /// </summary>
    /// <remarks>
    /// <b>A second link column rather than a second roster keyed on the same one</b>, because a
    /// Citizen is in both partitions at once: they leave home at one Tick of the Day and leave work at
    /// another, and one <c>next</c> cannot thread a row into two lists. 4 MB at 1M Citizens, derived,
    /// and rebuilt with its sibling. <c>adr/0101</c>.
    /// </remarks>
    public Column<int> CommuteReturnNext { get; }

    /// <summary>
    /// Which two buckets of <c>Movement.CommuteRoster</c> this Citizen was actually put in, packed,
    /// or zero for a Citizen in neither.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This exists because of a defect the first cut of <c>adr/0101</c> shipped, and the general
    /// form is worth more than the fix.</b> That cut removed a Citizen from the roster by
    /// <em>recomputing</em> its buckets from the Workplace — which is exactly how it had inserted
    /// them, and looks symmetrical. It is not, because <see cref="Workplace"/> is
    /// <c>Reference.Severable</c>: demolishing a workplace invalidates the handle <b>with no hook and
    /// no notification</b>, so the recomputation silently returned <em>not rostered</em> and the
    /// removal became a no-op. Every subsequent re-employment then inserted the Citizen a second time.
    /// </para>
    /// <para>
    /// <b>The result was an <c>adr/0006</c> violation with a quadratic tail</b>: buckets grew with
    /// elapsed time on a Ruleset that demolishes, every insert walked a longer list, and every Day
    /// generated more duplicate Trips than the last. 256 Ticks ran in 4.9 s and 512 did not finish in
    /// two minutes, which is how it was found.
    /// </para>
    /// <para>
    /// ***An intrusive index that unlinks by recomputing its key cannot outlive a change to that
    /// key's inputs.*** The old roster was safe from this by luck rather than by design — its key was
    /// the Citizen's own monotonic id, which nothing can invalidate — so the property that protected
    /// it was never stated and did not survive the key changing.
    /// </para>
    /// <para>
    /// <b>Derived rather than saved, and it rebuilds to the same answer.</b> A Citizen whose Workplace
    /// no longer resolves is not in a rebuilt roster at all, which is the correct membership; the
    /// stored pair simply says where a maintained roster put them, so that unlinking never has to
    /// consult anything that can vanish. Packed as <c>(outbound + 1) | ((homeward + 1) &lt;&lt; 16)</c>
    /// so that a zeroed array reads as <em>in neither</em> rather than as <em>bucket 0</em>.
    /// </para>
    /// </remarks>
    public Column<int> CommuteBucket { get; }

    /// <summary>
    /// What this Citizen's journey to work cost <b>when they took the job</b>. Not what it costs now.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one saved column <c>adr/0101</c> adds, and the argument for it being saved is that it is
    /// a measurement rather than a draw.</b> A departure is the Workplace's Shift start less this, so
    /// somebody who lives further out leaves earlier — permanently, and without asking the road what
    /// it looks like this morning. Everything else that decision needed turned out derivable: the
    /// Shift start is a draw on the Building's id and the Shift length a draw on the Citizen's, both
    /// against the Ruleset in force. This is not, because <b>no function of an id and a Ruleset
    /// recovers a fact about a past world</b>.
    /// </para>
    /// <para>
    /// <b>Zero is the honest value for somebody with no job</b>, and it is never read for one: the
    /// roster only holds Citizens whose <see cref="Workplace"/> resolves. Written by
    /// <c>EmploymentEngine</c> at assignment, from the candidate walk it had already paid for — so
    /// this costs no search of its own.
    /// </para>
    /// <para>
    /// ⚠ <b>It is deliberately <em>not</em> refreshed as the commute changes.</b> A Citizen still
    /// leaving at the old hour for a journey that has since got worse is late for work, which is a
    /// diagnosis the city can show and a reason to reconsider the job — <c>CONTEXT.md</c> → Provider
    /// List's <i>how I get to work is decided when the job is taken, not every morning</i>, and
    /// <c>adr/0046</c>'s Habit on the daily axis. Refreshing it would also make the roster a partition
    /// keyed on a value that moves with congestion, and therefore a rebuild every Tick.
    /// </para>
    /// </remarks>
    public Column<Ticks> PlannedCommute { get; }

    /// <summary>What the Citizen is doing. What a wake mutates.</summary>
    public Column<byte> Activity { get; }

    /// <summary>The Household this Citizen belongs to.</summary>
    /// <remarks>
    /// Named for the relationship rather than for the type, because <c>Household</c> alone would
    /// shadow the entity tag inside this file and read as though the column held one.
    /// </remarks>
    public HandleColumn<Household> HouseholdOf { get; }

    /// <summary>Where this Citizen works, or the unset handle, <c>default</c>.</summary>
    public HandleColumn<Building> Workplace { get; }

    /// <summary>Accumulated on-the-job experience. An i64 accumulator, per <c>05 §3</c>'s widths.</summary>
    public Column<long> Experience { get; }

    /// <summary>Which Skill Tier. Resolved through the Ruleset.</summary>
    public Column<byte> SkillTier { get; }

    /// <summary>Employment state.</summary>
    public Column<byte> Employment { get; }

    /// <summary>
    /// How many job-search occasions have ended with the Road Graph unable to deliver anything
    /// inside the Commute Budget, since this Citizen last took a job.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0097</c>: a memory buys a skipped detection.</b> A candidate refused because its
    /// posts are full costs one array read to re-detect, so it is forgotten; a candidate refused
    /// because it is beyond the Budget costs a full Dijkstra, so it is remembered. This is the
    /// remembering, and it is the first honest constituent of <c>02 §9</c>'s Citizen row — the
    /// <c>jobs beyond budget</c> aggregate could report <i>distance rather than supply separates
    /// them</i> and could not name one person it was true of.
    /// </para>
    /// <para>
    /// <b>The unit is the <em>occasion</em>, not the candidate, and <c>adr/0097</c>'s title says
    /// candidate.</b> Settled the other way when the column was built (milestone 6 task 3), because
    /// <see cref="Borough.Core.Rules.EmploymentEngine"/> looks at <c>[jobs] candidates</c> candidates
    /// per occasion: a per-candidate count is that tuning number times the quantity anybody wants,
    /// so a Ruleset moving <c>candidates</c> from 3 to 5 would inflate every Citizen's history by
    /// 5/3 with nothing saying so, and milestone 19's threshold would mean different things in
    /// different Rulesets. ***A derivation that reuses a constant inherits every decision that
    /// constant is already carrying*** — <c>adr/0079</c>'s refusal, which this milestone applied to
    /// the Evidence window one task ago. What the count measures is <b>persistence</b>, and
    /// persistence is denominated in occasions: <c>adr/0067</c>'s consecutive-failed-occasions is
    /// the shape, and this ADR's own argument (<i>structurally excluded</i> against <i>unlucky this
    /// occasion</i>) is denominated the same way. The per-candidate quantity still exists and is
    /// still reported — it is the <c>beyond</c> Census flow, which is an instrument rather than
    /// state.
    /// </para>
    /// <para>
    /// <b>An occasion with no reach refusal in it does not increment</b>, even when it ends in no
    /// job. Every candidate being full is a Space refusal and is deliberately not remembered, so a
    /// count that moved on it would be a count of joblessness wearing a reachability name — which is
    /// exactly the conflation <c>adr/0097</c> exists to undo.
    /// </para>
    /// <para>
    /// <b>It saturates rather than wrapping</b> (<c>adr/0003</c>'s no-unbounded-magnitude rule
    /// applies to a counter as much as to a quantity), and the saturation point is <b>a wrap guard
    /// rather than a chosen bound</b>. A Citizen is looked at roughly twice a Day at the shipped
    /// <c>[jobs] revisit_ticks</c>, so <see cref="ushort.MaxValue"/> is on the order of 32,000 Days
    /// against a campaign of 562 — no world this project can build reaches it. That is the point:
    /// <c>adr/0097</c> says the width follows from milestone 19's threshold, which does not exist,
    /// and a <em>reachable</em> cap would be choosing when attribution stops being exact on behalf of
    /// a consumer nobody has designed (<c>adr/0070</c>). Narrowing the column the day 19 sets a
    /// threshold is one edit and one re-record (<c>adr/0100</c>).
    /// </para>
    /// <para>
    /// <b>It resets on employment and on nothing else</b>, in <see cref="World.Employ"/> — the one
    /// door onto <see cref="Workplace"/>, so every path that gives somebody a job clears it and none
    /// has to remember to. In particular it does <em>not</em> reset on a Ruleset reload: this is the
    /// Citizen's history, not a cache of a Ruleset value, which is the distinction <c>adr/0064</c>
    /// and <c>adr/0065</c> were both written around. A Citizen whose workplace is demolished keeps
    /// the zero their employment bought and starts again from it.
    /// </para>
    /// <para>
    /// <b>Read by nothing yet, and saying so is part of the decision.</b> The consumer is milestone
    /// 19's Departure, and <c>adr/0097</c>'s 2026-08-15 amendment names <em>which</em> of its three
    /// channels: the <b>Destitute</b> one, because <c>CONTEXT.md</c> → Unemployment already routes
    /// reachability there — <i>destitution is a reachability failure wearing a money costume</i>.
    /// The count does not repair the search bill it records; <c>plans/0013</c> owns that, and
    /// <i>a byte on a row does not repair a bill</i>.
    /// </para>
    /// </remarks>
    public Column<ushort> ReachFailures { get; }

    /// <summary>Link in the Household's member list.</summary>
    public Column<int> MemberNext { get; }

    /// <summary>
    /// Link in the Workplace's worker list — see <see cref="BuildingTable.WorkerHead"/>.
    /// </summary>
    /// <remarks>
    /// <b><see cref="Disposition.Derived"/>, and the order is recoverable rather than merely the
    /// membership</b>, which is the test <c>05 §3</c> states and the one a list has to pass to be
    /// declared this way. Membership follows from <see cref="Workplace"/>; the order follows because
    /// the rebuild inserts by monotonic id rather than appending, so a Building that lost a worker
    /// and gained another into the recycled slot lists them in the same order either way.
    /// </remarks>
    public Column<int> WorkerNext { get; }

    /// <summary>Age, in Days.</summary>
    public Column<ushort> Age { get; }

    /// <summary>Health.</summary>
    public Column<byte> Health { get; }
}
