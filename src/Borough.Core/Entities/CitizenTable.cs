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
    public CitizenTable(
        int capacity, HouseholdTable households, BuildingTable buildings, Parking.CarParkTable carParks)
    {
        ArgumentNullException.ThrowIfNull(households);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(carParks);

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

        // Cold: a Fate is written once per journey and read only by a panel. Appended after the
        // per-Tick columns for that reason and never interleaved among them.
        LastTripFate = _rows.Saved<byte>("last_trip_fate", Touch.Cold);
        LastTripEndedDay = _rows.Saved<ushort>("last_trip_ended_day", Touch.Cold);

        // Severable, for Workplace's reason and with one difference worth stating. A Car Park is on
        // somebody else's Building, so demolition frees it out from under this handle -- and a car
        // park that no longer resolves is the garage no longer existing, which is the fact rather
        // than a break in it. The difference: adr/0084's conservation sum reads *resolving* holdings,
        // so a demolished Car Park removes both sides of the equation together and cannot read as a
        // leak. adr/0084 names the displaced car as a second mutation site whose write-site predicate
        // needs restating; that restatement is task 4's, and this declaration is what makes the case
        // representable rather than silent.
        ParkedIn = _rows.SavedHandle(
            "parked_in", carParks.Rows, reference: Reference.Severable);

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
    /// ⚠ <b>That reset is also this column's <c>adr/0006</c> bound, and until milestone 6 task 6
    /// nothing said so.</b> The paragraph above states the saturation is a wrap guard <em>rather
    /// than</em> a chosen bound — so the declared position is that this magnitude has no bound — and
    /// the paragraph below it describes the reset as an <b>attribution</b> rule, which is what it was
    /// designed as. Both are accurate and neither says that the reset is the only thing standing
    /// between this column and a quantity that grows with elapsed time. ***A sentence can name a
    /// mechanism exactly and still not state the property that mechanism is holding up***, which is
    /// <c>adr/0093</c> on a new axis: that decision governs a description being wrong about a
    /// trigger, and this is a description being silent about a <em>consequence</em>. The cost of the
    /// silence is that anybody reopening <i>should employment really wipe the history?</i> would
    /// weigh an attribution question and never see the unbounded-magnitude one. <b>Measured</b>:
    /// with the reset removed, the longest history in the city climbs 14.8 → 24.8 across a 41-Day
    /// tail and <b>3,868 of 4,000</b> Citizens carry a history that can never be cleared — see
    /// <c>Evidence.EvidenceLongRunTests</c>, which is where an edit to that line now fails.
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

    /// <summary>
    /// <b>How this Citizen's last journey ended</b>, as a <c>Movement.TripFate</c> — or
    /// <c>InFlight</c>, which here means <em>no journey of theirs has ever ended</em>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>02 §9</c>'s <i>"current or last Trip with its Fate"</i>, second half.</b> The first half
    /// is a scan of <c>Movement.TravellerTable</c> and needs no state; this half needed some, because
    /// <c>TripEngine.Release</c> frees the Trip row on the line after asserting it carries a Fate and
    /// <c>AdvanceTravellers</c> frees the <b>Traveller</b> — the only Citizen-to-Trip link there is —
    /// earlier in the same pass. The Fate and the association with the person who made the journey
    /// ceased to exist together, so the answer was unrecoverable rather than merely unassembled.
    /// </para>
    /// <para>
    /// ⚠ <b>A column and not a trail, and the brief said trail.</b> Milestone 6 scoped this as
    /// <i>"task 2's situation verbatim"</i> — the abandonment reason, where the fact is copied into
    /// <c>Rules.CondemnationTrailTable</c> before the row is freed. The two look identical from the
    /// freeing site and differ at the <b>subject</b>. A condemnation's subject is the Building, which
    /// is destroyed, so there is no entity left to hang the fact on and a trail is the only shape —
    /// that is the milestone's D3 argument in its own words. A Trip's subject is the <b>Citizen</b>,
    /// who outlives the journey by design, and <c>02 §9</c> asks this question <em>of a Citizen</em>.
    /// ***What is freed is not always the subject, and it is the subject that decides the shape.***
    /// </para>
    /// <para>
    /// <b>The scale argument agrees and is the reason a trail would have been wrong rather than
    /// merely indirect.</b> A commute is two journeys a Day, so a million Citizens end roughly two
    /// million Trips a Day — about <b>a thousand per Tick</b>. A 256-entry window would cover a
    /// quarter of a Tick and would answer <i>what happened in the city lately</i> rather than
    /// <i>what happened to this person</i>. Milestone 6 task 6 found the same shape from the other
    /// end: ***the unit a bound is written in is not the unit its argument is about***. Here there is
    /// no bound to size and no <c>adr/0052</c> number: every Citizen's answer is exact, for ever.
    /// </para>
    /// <para>
    /// <b><c>TripFate.InFlight</c> is the never-travelled sentinel, and it is free rather than
    /// chosen.</b> That enum reserves zero for <em>the Trip has not ended</em> precisely so a row
    /// nothing has written cannot read back as an outcome, and a freshly allocated Citizen is
    /// zero-filled — so the value that means <i>unset</i> here is the one the Fate set already
    /// reserved for it. A stored <c>InFlight</c> is impossible as a real reading, because a Fate is
    /// recorded only when a journey ends. ***A sentinel outside the range of legitimate answers is
    /// the one kind that can announce itself*** — the rule <c>adr/0074</c>'s crossing cost and
    /// <c>adr/0098</c>'s ownership rate both had to reach for, available here for nothing.
    /// </para>
    /// <para>
    /// <b>Written in <see cref="World.RecordTripFate"/> and nowhere else.</b> That is
    /// <see cref="World.Employ"/>'s door discipline on a third axis: all four of
    /// <c>TripEngine</c>'s Fate sites have the Citizen in hand — <c>TripEngine.Start</c> takes one as
    /// its first parameter and <c>AdvanceTravellers</c> reads <c>TravellerTable.Citizen</c> — so the
    /// door can require it and a fifth site cannot be added without deciding whose journey it was.
    /// </para>
    /// </remarks>
    public Column<byte> LastTripFate { get; }

    /// <summary>
    /// Which Day <see cref="LastTripFate"/> was recorded on. Meaningless while that column reads
    /// <c>TripFate.InFlight</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Without it the Fate is undated, and an undated Fate is the weakest exactly where it is
    /// most needed.</b> Anybody who still commutes overwrites this daily, so staleness can only
    /// afflict somebody who makes no journeys at all — which is precisely the person a decline
    /// diagnosis is about. <c>NoRouteFound</c> from four hundred Days ago and <c>NoRouteFound</c>
    /// this morning are different evidence and would otherwise read identically.
    /// </para>
    /// <para>
    /// <b>Days rather than Ticks, and the precedent is four lines below.</b> <see cref="Age"/> is a
    /// <c>ushort</c> denominated in Days for the same reason: <c>Ticks</c> is a <c>ulong</c>, so a
    /// Tick-denominated column is <b>8 MB at a million Citizens</b> — around a tenth of the 85.98 MiB
    /// S0a measured for the whole table set — against <b>2 MB</b> here, for a field no code path from
    /// <c>step()</c> reads. ⚠ <b>What it costs is within-Day resolution, and a commute is a
    /// within-Day phenomenon</b>: <i>this morning</i> against <i>this evening</i> is not recoverable
    /// from this column, and a panel that wants it wants the <em>current</em> Trip, which carries a
    /// real Tick. ***A column recording that something happened is denominated differently from one
    /// recording when to do something next.***
    /// </para>
    /// <para>
    /// <b>It saturates rather than wrapping</b> (<c>adr/0003</c>), on <see cref="ReachFailures"/>'
    /// argument and with the same slack: 65,535 Days against <c>01 §4</c>'s twenty-hour campaign of
    /// <b>562</b>, so no world this project can build reaches it and the saturation is a wrap guard
    /// rather than a bound anybody chose.
    /// </para>
    /// </remarks>
    public Column<ushort> LastTripEndedDay { get; }

    /// <summary>
    /// The Car Park this Citizen's car is parked in, or an unresolving handle if it is parked in none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The holder of a parking space is the Citizen</b> (<c>adr/0119</c>), and the two objects the
    /// corpus named before it are both wrong for one reason: <c>adr/0009</c> put it on the
    /// <b>Trip</b> and <c>adr/0084</c> put it on the <b>Traveller</b>, and <em>both are freed when
    /// the journey ends</em>. Since <c>adr/0101</c> made a commute two journeys, the space is held
    /// across a gap in which neither exists — which is that ADR's own canonical case, <i>a
    /// household's car sits at home overnight</i>. ***What is freed is not always the subject, and it
    /// is the subject that decides the shape*** — the rule <see cref="LastTripFate"/> above was
    /// placed by, one milestone earlier and on the same table.
    /// </para>
    /// <para>
    /// <b>Not the Household, which is the obvious repair and the one the brief recommended.</b>
    /// <see cref="World.ModeOf"/> returns <c>TravelMode.Car</c> for <em>every member</em> of a
    /// car-owning Household, so a Household of three workers parks three cars at three destinations
    /// and one column would overwrite two of them — an acquire with no matching release, which is
    /// exactly the <c>adr/0006</c>-class leak <c>adr/0084</c>'s invariants exist to catch. The
    /// argument is <see cref="Workplace"/>'s, about the same two people: <c>CONTEXT.md</c> → Building
    /// says employment <i>counts Citizens and never Households</i> because <i>two adults in one
    /// Household working opposite sides of the city is the case a per-Household count could not
    /// express</i>. Driving to opposite sides is the same case.
    /// </para>
    /// <para>
    /// <b>One column is what makes <c>ParkingSpaceIsReleasedOnce</c> nearly free.</b> That invariant
    /// asserts a release names a Car Park this Citizen holds <em>exactly once</em>, and holding two
    /// is <b>unrepresentable</b> here rather than checked — the <c>Rule Instance</c> armed/waiting
    /// precedent, where the corpus prefers a state it cannot express to a state it verifies.
    /// </para>
    /// <para>
    /// <b>An unresolving handle is the sentinel and it is free rather than chosen</b>, on
    /// <see cref="LastTripFate"/>'s argument: a freshly allocated Citizen is zero-filled, a
    /// zero handle resolves to nothing, and <i>parked nowhere</i> is what a walker, a Citizen who has
    /// never driven, and a driver in motion all are. There is no value inside the range of legitimate
    /// answers doing duty as <i>unset</i>.
    /// </para>
    /// </remarks>
    public HandleColumn<Parking.CarPark> ParkedIn { get; }

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
