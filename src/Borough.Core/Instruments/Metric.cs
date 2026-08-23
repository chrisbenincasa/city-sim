using Borough.Core.Movement;

namespace Borough.Core.Instruments;

/// <summary>
/// The counters a table exposes to the <see cref="Census"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three counters rather than one, because they fail differently.</b> A table that is leaking and
/// a table that is merely busy both show a rising <see cref="Live"/>, and nothing about that number
/// alone separates them. The pair that separates them is <see cref="Live"/> against
/// <see cref="Slots"/>: slots are only ever allocated when the free list is empty, so
/// <em>Slots rising while Live is flat</em> is a free list that is not being returned to — which is
/// <c>adr/0006</c>'s failure with the population held constant, and is invisible in a row count.
/// </para>
/// <para>
/// <b><see cref="Capacity"/> is the third because it is the one that costs memory.</b> Slots are a
/// high-water mark and capacity is what was actually allocated to hold them; they diverge because
/// growth is geometric. A run whose capacity climbs while its slots do not has an array being grown
/// by something other than demand.
/// </para>
/// </remarks>
public enum CensusCounter : byte
{
    /// <summary>Rows in use. The city's size, and on its own not evidence of anything.</summary>
    Live,

    /// <summary>
    /// Slots ever allocated — the high-water mark of simultaneous demand.
    /// </summary>
    /// <remarks>
    /// Rises only when a row is created and the free list is empty, so it is flat across any
    /// create-and-destroy cycle that recycles. This is the counter <c>adr/0006</c> is about.
    /// </remarks>
    Slots,

    /// <summary>The length of the backing arrays. What the table costs, as opposed to what it holds.</summary>
    Capacity,
}

/// <summary>
/// The counters the Rule engine exposes to the <see cref="Census"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §4</c> names two of these and then argues that the interesting quantity is a third.</b>
/// The section calls for <em>Rule evaluations per Tick</em> and <em>walked chain depth</em>, and in
/// the same paragraph says the cost driver is <em>how often a Bin crosses the supplied/short
/// boundary</em> — which neither of them measures. <see cref="Due"/> is not that third quantity, but
/// it is what makes the first two readable at all: without it a rising evaluation count could equally
/// be a bigger city or a more unstable one, and those mean opposite things.
/// </para>
/// <para>
/// <b>The pair that separates them is <see cref="Due"/> against <see cref="Evaluations"/>.</b> Due
/// rows are the scheduled load — what the Event Wheel handed over — and evaluations are what was
/// actually spent, so <em>evaluations − due</em> is the whole cost of chain walking and Phase 3's
/// re-check. This is the same discrimination <see cref="CensusCounter"/> makes with
/// <see cref="CensusCounter.Live"/> against <see cref="CensusCounter.Slots"/>, and it is here for the
/// same reason: one number rising is not evidence of which thing rose.
/// </para>
/// <para>
/// <b>All three are flows, which is what separates them from every table counter.</b> A table counter
/// is a level read at an instant and is the same number whether it is read every Tick or every
/// thousand. These accumulate, so a reading is <em>what happened since the last one</em> and the
/// reading drains it — see <see cref="Aggregate"/>.
/// </para>
/// </remarks>
public enum RuleCounter : byte
{
    /// <summary>
    /// Rule Instances taken off the Event Wheel — the scheduled load, before anything is spent on it.
    /// </summary>
    /// <remarks>
    /// A failed Rule does not re-arm (<c>02 §4.1</c>), so this falls as a District starves and rises
    /// as it recovers. It is the denominator the other two are read against.
    /// </remarks>
    Due,

    /// <summary>
    /// Rules evaluated — <c>02 §4</c>'s first counter, counting every evaluation rather than every
    /// due row.
    /// </summary>
    /// <remarks>
    /// <b>One per <c>Check</c>, which is what the section's own note asked for</b>: a head, every
    /// non-terminal link of a chain walked below it, and Phase 3's re-check are each an evaluation,
    /// because each costs one. Counting due rows instead — which is what this counter did before task
    /// 9 — <em>"does not see a chain link at all"</em>, so the quantity the tripwire is stated over
    /// was the one quantity chain walking could not move.
    /// </remarks>
    Evaluations,

    /// <summary>
    /// Chain rungs descended — <c>02 §4</c>'s second counter, and a depth rather than a cost.
    /// </summary>
    /// <remarks>
    /// <b>Every rung reached, the terminal included.</b> A terminal is never evaluated (it has no term
    /// that could be short, so ordinary Rule semantics would fire it and re-arm the head on
    /// <c>rate</c>), so it adds a rung here and no evaluation there. That divergence is the point:
    /// this counter is how deep the ladder went and <see cref="Evaluations"/> is what the descent
    /// cost, and a chain that ends in a report costs less than its depth suggests.
    /// </remarks>
    ChainRungs,
}

/// <summary>
/// The counters the Zone Rules expose to the <see cref="Census"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The pair the slice's tripwire is stated over is <see cref="Triggers"/> against the sum of
/// <see cref="Vacant"/> and <see cref="Occupied"/>.</b> <c>02 §5.7</c> claims a Zone Rule's
/// per-trigger cost is independent of the size of the Zone it sweeps, and the quantity that claim is
/// really about is <em>Lots evaluated per trigger</em> — which is the sample size in the Ruleset and
/// nothing else. Neither number can state it alone: triggers rise with elapsed time and evaluations
/// rise with both.
/// </para>
/// <para>
/// <b><see cref="Vacant"/> and <see cref="Occupied"/> are separate because they are two mechanisms.</b>
/// A vacant Lot is a candidate for creation and an occupied one is a Building whose failure pressure
/// is read; their sum is what the tripwire holds fixed, and their <em>ratio</em> is how full the city
/// is. Summing them in the census and recovering the split later is not possible, so the split is what
/// is stored.
/// </para>
/// <para>
/// <b><see cref="Created"/> and <see cref="Demolished"/> are the outcomes, and they are what make a
/// reading legible.</b> Evaluations without outcomes is a Zone Rule sweeping for ever and doing
/// nothing, which is the whole class the loader's three refusals exist to catch at load time and which
/// this pair catches at run time.
/// </para>
/// <para>
/// <b>All five are flows</b>, for <see cref="RuleCounter"/>'s reason: a reading is what happened since
/// the last one, and the reading drains it.
/// </para>
/// </remarks>
public enum ZoneCounter : byte
{
    /// <summary>Zone Rule firings — one per Rule per Tick its interval divides.</summary>
    Triggers,

    /// <summary>Sampled Lots with no Building on them.</summary>
    Vacant,

    /// <summary>Sampled Lots with one.</summary>
    Occupied,

    /// <summary>Buildings built — the subset of <see cref="Vacant"/> that qualified.</summary>
    Created,

    /// <summary>
    /// Buildings condemned — the subset of <see cref="Occupied"/> whose <b>premises'</b> pressure is
    /// past its kind's threshold.
    /// </summary>
    Demolished,

    /// <summary>
    /// <b>Tenancies ended</b> — Households evicted because <em>their own</em> Rules crossed the same
    /// threshold, leaving the premises standing (<c>adr/0141</c>).
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It never overlaps <see cref="Demolished"/>.</b> A demolition ends every tenancy in the
    /// Building and is counted once, as one Building. ***The gap between the two is the split made
    /// visible***: before <c>adr/0141</c> the second outcome did not exist, and one starving tenant
    /// was reported here as a demolished Building.
    /// </remarks>
    Ended,
}

/// <summary>
/// The counters the placement pass exposes to the <see cref="Census"/> (<c>adr/0069</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Two counters rather than one, because the interesting quantity is the gap between them.</b>
/// <see cref="Considered"/> against <see cref="Placed"/> is the housing shortage stated as a rate: a
/// Pool that is being looked at and not housed is a city out of dwellings, where a Pool that is not
/// being looked at is a mechanism that has stopped. A single <em>placed</em> counter reads identically
/// in both cases — which is the shape slice 7 task 9 already found once in
/// <c>evaluations − due</c>, and it is worth not finding a third time.
/// </para>
/// <para>
/// <b>A fourth family rather than more <see cref="ZoneCounter"/>s.</b> Placement and the Zone Rules
/// share a Tick phase and nothing else: one drains the Unplaced Pool into what stands, the other
/// builds and condemns. Counting a seeker's occasion alongside a developer's trigger would be the
/// arithmetic saying they are the same kind of event, which is <see cref="MetricSource.Zones"/>'
/// reasoning applied a second time.
/// </para>
/// </remarks>
public enum PlacementCounter : byte
{
    /// <summary>Pool members given an occasion to look — the sample, summed over the interval.</summary>
    Considered,

    /// <summary>Of those, the ones that found a dwelling with room.</summary>
    Placed,

    /// <summary>
    /// Of those, the ones that gave up looking and left the city (<c>adr/0130</c>).
    /// </summary>
    /// <remarks>
    /// <b>A different <em>kind</em> of quantity from the two above, and `CONTEXT` → Departure says
    /// why it cannot be inferred from them.</b> *"Departure rate is a distinct demand signal from
    /// Pool size: Pool size is a stock of latent demand, departure rate is a flow measuring how badly
    /// the city is failing to convert its own attractiveness into capacity. A city can have a large
    /// Pool and be healthy, or a small Pool and be in crisis; only the flow distinguishes them."*
    /// ***Reporting the Pool without this reports the stock and calls it the diagnosis.***
    /// </remarks>
    Departed,
}

/// <summary>
/// One counter per <see cref="Movement.TripFate"/>: how journeys ended over the interval.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Fate rather than a Trip count, because <c>02</c>'s rule is <i>no Trip without a Fate</i> and
/// the Fate is the thing worth keeping.</b> A Trip row lives for as long as the journey and then goes
/// back to the allocator (<c>adr/0006</c>), so the count of Trips is a level that is always about to
/// be wrong; how they <em>ended</em> is an event, and an event survives its row only if something
/// counts it on the way out.
/// </para>
/// <para>
/// <b>All four, including the two nothing can currently produce.</b> <c>adr/0076</c> closes the set at
/// four, so this enumeration is closed with it and needs no edit when the missing conditions arrive.
/// <see cref="ExceededCommuteBudget"/> waits on the Commute Budget existing as <c>[trips]</c> Ruleset
/// data — milestone 5b-bis, <c>adr/0081</c>.
/// </para>
/// </remarks>
public enum TripCounter : byte
{
    /// <summary>Reached the destination. <b>Running out of Legs is what arriving means.</b></summary>
    Completed,

    /// <summary>
    /// No walkable route existed — including the case where an endpoint had no front door at all,
    /// which <c>adr/0079</c> makes a reported hole rather than an error.
    /// </summary>
    NoRouteFound,

    /// <summary>The journey was possible and cost more than the Commute Budget allows.</summary>
    ExceededCommuteBudget,

    /// <summary>The network changed underneath the Trip and left it with nowhere to go.</summary>
    Stranded,

    /// <summary>Legs created on foot (5c task 5).</summary>
    /// <remarks>
    /// ⚠ <b>A different denominator from the four above it, and the family now holds three.</b> The
    /// Fates count Trips that <em>ended</em>; <see cref="TripCostBucket"/> counts Trips that were
    /// <em>created</em>; these two count <b>Legs</b> that were created. They are in this family rather
    /// than a new one because a Leg's mode is decided at the same instant and by the same pass, and a
    /// family whose members share an owner is what every other family here already is — but the sums
    /// do not cross, and a reading that compares a Leg count to a Fate count is comparing two things.
    /// </remarks>
    WalkLegs,

    /// <summary>
    /// Legs created by car. <b>The supply side of <c>adr/0041</c>'s volume attribution</b> — only a
    /// vehicular Leg increments a Segment's count, so this is the population task 6 draws from.
    /// </summary>
    DriveLegs,
}

/// <summary>
/// A Trip's cost, bucketed. The shape of what the city actually walks.
/// </summary>
/// <remarks>
/// <para>
/// <b>The corpus's first histogram, and the first Census family that is one metric rather than
/// several.</b> Every other family here names distinct quantities that happen to share an owner —
/// <c>due</c> and <c>evaluations</c> mean different things. These seven are one quantity at seven
/// resolutions, and the reason it needs seven is that <b>the Commute Budget is a <em>percentile</em></b>
/// (`0002` §D), and a mean cannot locate a percentile: a city of short walks with a long tail and a
/// city of uniform medium walks have the same mean and want different Budgets.
/// </para>
/// <para>
/// <b>The ladder is geometric in clock minutes and is not derived from the Commute Budget, which was
/// the tempting choice.</b> Buckets stated as fractions of the Budget would need no free numbers at
/// all — <c>adr/0059</c>'s shape — and they would be <b>useless for the one job this family has</b>:
/// the ratifier compares runs at <em>different</em> Budgets, and a distribution measured in units of
/// the number under test has the same shape at every value of it. <b>A ruler must not move with the
/// thing it measures.</b>
/// </para>
/// <para>
/// <b>The edges are free numbers and need no ratifier, which is worth stating because every other
/// number in this milestone needed one.</b> <c>adr/0052</c> governs <em>hash-bearing and
/// world-creation</em> numbers; a Census bucket edge is neither. The Census is read-only, folds into
/// nothing, and changing an edge changes what a report looks like and no city anywhere. It is an
/// instrument's resolution.
/// </para>
/// <para>
/// ⚠ <b>This is not the distribution the Commute Budget is a percentile of, and the difference is the
/// whole of 5b-bis task 6's finding.</b> These are Trips the city <em>made</em>, and a commute exists
/// only because the assignment pass already accepted the job at the other end of it — inside the
/// Budget. So the ceiling is upstream: this distribution is <b>censored by the number it would be
/// used to ratify</b>. The uncensored one is <c>--trips</c>' geometric census over every Building
/// pair, which had to be taken <em>before</em> a Budget existed and was — task 3 refused to set one
/// for exactly this reason, and this family is the evidence that the refusal was right, because there
/// is now no way to take that reading again.
/// </para>
/// <para>
/// <b>Counted at creation rather than at completion</b>, so a Trip refused for its length is in here
/// with its cost. A histogram of completed Trips only would be censored a second time, by the Fate.
/// </para>
/// </remarks>
public enum TripCostBucket : byte
{
    /// <summary>Under one minute. On foot at 5 km/h that is under 83 m — the other side of a Lot.</summary>
    UnderOneMinute,

    /// <summary>One to two minutes. About one 128 m block, which is 92 s at walking pace.</summary>
    UnderTwoMinutes,

    /// <summary>Two to four minutes.</summary>
    UnderFourMinutes,

    /// <summary>Four to eight minutes.</summary>
    UnderEightMinutes,

    /// <summary>Eight to sixteen minutes.</summary>
    UnderSixteenMinutes,

    /// <summary>Sixteen to thirty-two minutes. <b>The shipped 20-minute Budget falls inside this one.</b></summary>
    UnderThirtyTwoMinutes,

    /// <summary>
    /// Thirty-two minutes or more, <b>and every Trip with no route at all</b>.
    /// </summary>
    /// <remarks>
    /// <b>An impassable cost is <c>Fixed.MaxValue</c> and lands here rather than in a bucket of its
    /// own</b>, because <see cref="TripCounter.NoRouteFound"/> already counts it exactly and a second
    /// counter of the same event is <c>plans/0012</c> <i>Cause 1</i>. What this bucket claims is
    /// <em>a journey nobody would make</em>, and an impossible one qualifies.
    /// </remarks>
    ThirtyTwoMinutesOrMore,
}

/// <summary>
/// One counter per outcome of the job assignment pass: how looking for work went over the interval.
/// </summary>
/// <remarks>
/// <para>
/// <b>Four rather than two, and the third and fourth are the ones worth having.</b>
/// <see cref="PlacementCounter"/>'s remark records why a single <em>placed</em> counter is not enough:
/// a queue that is not being looked at and a queue with nowhere to go read identically. This pass has
/// a <em>third</em> way to do nothing — the candidate existed, had a vacancy, and could not be reached
/// inside the Commute Budget — and collapsing that into the shortage would make a severed city
/// indistinguishable from an unemployed one.
/// </para>
/// <para>
/// <b><see cref="Beyond"/> is the only counter in the Census that reports the shape of the Road
/// Graph.</b> <c>03 §3.7</c>'s Severance is a mechanism rather than a paragraph because the mode mask
/// exists; it becomes a mechanism anybody can <em>observe</em> because this counts what it costs.
/// </para>
/// <para>
/// <b>It is counted per <em>look</em> where the other three are per Citizen</b>, which is stated here
/// because the units differ and nothing else says so: one seeker rejecting three unreachable
/// employers moves <see cref="Beyond"/> by three and <see cref="Seeking"/> by one.
/// </para>
/// </remarks>
public enum JobCounter : byte
{
    /// <summary>Live Citizens the pass looked at — the sample, summed over the interval.</summary>
    Considered,

    /// <summary>Of those, the ones with no Workplace and a home to search from.</summary>
    Seeking,

    /// <summary>Of those, the ones who took a job.</summary>
    Employed,

    /// <summary>Candidate vacancies rejected because the walk exceeded the ceiling.</summary>
    Beyond,

    /// <summary>Of those employed, the ones whose commute is <see cref="CommuteRung.Fast"/>.</summary>
    Fast,

    /// <summary>Of those employed, <see cref="CommuteRung.Moderate"/>.</summary>
    Moderate,

    /// <summary>
    /// Of those employed, <see cref="CommuteRung.Unsavoury"/> — <c>01 §4</c>'s <em>housed</em>
    /// Departure candidates.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Reads zero in every world the shipped Rulesets produce below about 40,000 Citizens</b>,
    /// and that is a property of the cities rather than of the counter. The paved extent is derived
    /// from population, so a 10,000-Citizen city is 1.9 km across against a 50-minute walking ceiling
    /// that reaches 4.2 km — nobody can live far enough from work to be graded badly. It first
    /// occupies at 40,000 (10 of 2,424) and reaches 738 of 9,473 at 160,000; the ladder is in
    /// <c>adr/0095</c> and in <c>rulesets/minimal.toml</c>'s own header.
    /// </remarks>
    Unsavoury,
}

/// <summary>
/// How a flow counter's Ticks are reduced into one reading.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two readings rather than one, because the tripwire and the bill are different questions.</b>
/// <c>02 §4</c>'s tripwire is stated per Tick — <em>chain walking fits while fewer than N evaluations
/// occur per Tick</em> — and a mean over a census interval cannot state it: burstiness is
/// <em>authored</em> under this design, so the interval containing the burst and the interval either
/// side of it are the same mean. <see cref="Sum"/> is what a run cost; <see cref="Peak"/> is what the
/// worst Tick in it cost, and only the second can be held against a budget.
/// </para>
/// <para>
/// <b>Neither loses the Ticks between readings, which is why there is one census cadence and not
/// two.</b> The accumulator behind these sees every Tick even though it is read every sixty-fourth,
/// so a slow reading discards only the <em>shape</em> of the interval — and <c>sum ÷ cadence</c>
/// against <see cref="Peak"/> recovers the part of that shape anybody reads a census for, which is
/// whether the load is a plateau or a spike. A second cadence would buy a finer view of something
/// already fully observed, at the cost of the property that a reading and a State Hash from the same
/// Tick are two views of one moment.
/// </para>
/// </remarks>
public enum Aggregate : byte
{
    /// <summary>The total over the interval since the previous reading. Divided by the cadence, a mean rate.</summary>
    Sum,

    /// <summary>The largest single Tick in that interval. The figure a per-Tick tripwire is read against.</summary>
    Peak,
}

/// <summary>
/// One counter of the Policy sweeps — <c>02 §4.2</c>'s <em>Flow</em> Policies.
/// </summary>
/// <remarks>
/// <para>
/// <b>Counts, and no money magnitude.</b> A magnitude is a <c>Money</c> rather than a count and
/// belongs to <c>plans/0033</c> task 7, which reports the money supply and the treasury separately
/// (<c>01 §5.1</c>). This family answers <em>did it run and whom did it reach</em>.
/// </para>
/// <para>
/// <b>The four outcomes are exclusive and exhaustive per member swept</b>, which is the property a
/// reader relies on without being told: every one of <see cref="Considered"/> either had money moved
/// (<see cref="Applied"/>), owed nothing, could not cover it (<see cref="Unaffordable"/>), or was
/// never reached because the sweep had already stopped. <see cref="Floored"/> is a <em>reason</em>
/// for owing nothing rather than a fifth outcome, and <see cref="Exhausted"/> counts sweeps rather
/// than members — both are deliberately not members of the partition and both say so here, because
/// a counter in a family whose members look like a partition will be summed by somebody.
/// </para>
/// </remarks>
public enum PolicyCounter : byte
{
    /// <summary>Policy firings — one per Policy per Tick its interval divides.</summary>
    Triggers,

    /// <summary>Live members swept. The denominator.</summary>
    Considered,

    /// <summary>Members the transfer moved money for.</summary>
    Applied,

    /// <summary>
    /// ⚠ <b><c>adr/0115</c>'s instrument.</b> Percentage applications that floored to zero from a
    /// non-zero Readout — the poorest paying nothing and everybody else the stated rate, which is a
    /// regressive outcome produced by rounding and chosen by nobody.
    /// </summary>
    Floored,

    /// <summary>Sweeps that stopped early because the payer ran dry. <b>Counts sweeps, not members.</b></summary>
    Exhausted,

    /// <summary>Members skipped because they could not cover the transfer. Does not stop the sweep.</summary>
    Unaffordable,
}

/// <summary>
/// The money aggregates: where the city's money is, read at an instant.
/// </summary>
/// <remarks>
/// <para>
/// <b>Levels rather than flows, and the first magnitudes the Census has ever carried.</b> Every
/// other family here counts events between two readings; these are stocks, and a stock read at an
/// instant means the same thing at any cadence — which is <see cref="MetricSource.Table"/>'s
/// property, not a flow's. So a money metric takes no <see cref="Aggregate"/>, for the same reason a
/// table counter does not.
/// </para>
/// <para>
/// ⚠ <b><see cref="Supply"/> and <see cref="Treasury"/> are reported separately because
/// <c>01 §5.1</c> requires it</b>, and it is a separation between two <em>trajectories</em> rather
/// than an editorial preference: insolvency is the treasury emptying, a trade deficit is the money
/// supply contracting, and the table that names them says of the second that it is <em>"a different
/// bill — the money supply, not the treasury"</em>. A picture showing one is a picture hiding the
/// one the endgame turns on.
/// </para>
/// <para>
/// <b><see cref="Held"/> against <see cref="Supply"/> is <c>Invariant.MoneyIsConserved</c> made
/// visible</b>, and it is deliberately redundant: <see cref="Held"/> is what the walk over the Bins
/// found and <see cref="Supply"/> is what <c>World.Endow</c> issued, arrived at differently, and the
/// milestone exists to make them equal. Reading them off the same series is how a reader sees that
/// without running the invariant.
/// </para>
/// </remarks>
public enum MoneyCounter : byte
{
    /// <summary>
    /// Money issued into this world, net of anything that has left it: <c>MoneySupplyTable.Issued</c>.
    /// </summary>
    /// <remarks>
    /// Flat for the whole of milestone 10, because <c>adr/0024</c> makes the Outside Connection
    /// money's only source and sink and that is milestone 11. A reading that moves before then is a
    /// leak, not a trade balance.
    /// </remarks>
    Supply,

    /// <summary>
    /// Every conserved Bin in the city, summed — <c>MoneyLedger.Total</c>. The four below decompose it.
    /// </summary>
    Held,

    /// <summary>What the treasury holds. <c>01 §5.1</c>'s insolvency reading.</summary>
    Treasury,

    /// <summary>What the Households hold, summed.</summary>
    Households,

    /// <summary>What the Businesses hold, summed.</summary>
    Businesses,

    /// <summary>
    /// <see cref="Held"/> less the three named holders: money in a place this family does not name.
    /// </summary>
    /// <remarks>
    /// Zero because <c>adr/0113</c> says a Building never holds money and there is no fifth owner
    /// kind — and it is carried precisely so that ceasing to be true is visible. Without it the
    /// unaccounted money would simply not appear in any row, and a decomposition that silently stops
    /// decomposing reads exactly like one that still does.
    /// </remarks>
    Elsewhere,
}

/// <summary>
/// The money magnitudes the Policy sweeps moved: the flow half of the balance sheet.
/// </summary>
/// <remarks>
/// <b>Its own family rather than two more <see cref="PolicyCounter"/>s, because a count and an amount
/// are not commensurable.</b> <c>PolicyCounter.Applied</c> is how many members a transfer moved money
/// for; these are how much money. Printed in one block they would sit under one heading and share a
/// column width, which is the report telling a reader they are the same kind of number. They are also
/// the only members of the Census folded through <c>MoneyFlow</c> rather than <c>RuleFlow</c>, and
/// that is a width difference an amount needs and a count does not.
/// </remarks>
public enum MoneyFlowCounter : byte
{
    /// <summary>Money moved into the treasury over the interval.</summary>
    ToTreasury,

    /// <summary>
    /// Money moved out of the treasury over the interval, reported apart from
    /// <see cref="ToTreasury"/> rather than netted.
    /// </summary>
    /// <remarks>
    /// A net is the one figure that cannot say whether a city taxed nothing and paid nothing or
    /// taxed heavily and paid it all back, and those are different cities.
    /// </remarks>
    FromTreasury,
}

/// <summary>Which family of thing a <see cref="Metric"/> names.</summary>
/// <remarks>
/// <b>Two families rather than one, because a level and a flow are not the same kind of number.</b>
/// A table counter is read at an instant and means the same thing at any cadence; a Rule counter is
/// accumulated between readings and drained by one. Filing the second as a synthetic table would have
/// let <c>live</c>, <c>slots</c> and <c>capacity</c> name three things that are none of those — the
/// drift <see cref="Metric"/> exists to prevent.
/// </remarks>
public enum MetricSource : byte
{
    /// <summary>One counter of one table: a collection's size, sampled.</summary>
    Table,

    /// <summary>One counter of the Rule engine: a flow, accumulated and drained.</summary>
    Rules,

    /// <summary>One counter of the Zone Rules: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// A third family rather than more <see cref="RuleCounter"/>s, because <c>adr/0033</c> makes the
    /// two Rule families differ in observable behaviour. Counting a Zone Rule's triggers alongside a
    /// Bin Rule's due rows would be the arithmetic saying they are the same kind of event, which is
    /// the reading the ADR exists to refuse.
    /// </remarks>
    Zones,

    /// <summary>One counter of the placement pass: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// A fourth family on <see cref="Zones"/>' reasoning. Placement shares Tick phase 6 with the Zone
    /// Rules and shares no event with them: <c>adr/0069</c> separates the mechanism that houses people
    /// from the mechanism that builds, and a shared counter would put them back together.
    /// </remarks>
    Placement,

    /// <summary>One counter of Tick phase 4: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// A fifth family on <see cref="Zones"/>' reasoning, and the first that is not a Rule family at
    /// all. A Trip Fate is not something a Rule did — it is how a journey ended — so folding it in
    /// beside <see cref="RuleCounter"/> would put the two mechanisms <c>adr/0033</c> and
    /// <c>adr/0075</c> keep apart into one arithmetic.
    /// </remarks>
    Trips,

    /// <summary>One counter of the job assignment pass: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// A sixth family on <see cref="Placement"/>'s reasoning exactly. The two passes share Tick phase
    /// 6 and share no event: one puts a family in a dwelling, the other puts a person in a job, and
    /// <c>adr/0081</c> keeps them apart for the same reason <c>adr/0069</c> kept placement out of the
    /// Zone Rules.
    /// </remarks>
    Jobs,

    /// <summary>One bucket of the Trip cost histogram: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// <b>A seventh family rather than seven more <see cref="TripCounter"/>s, because a Fate and a
    /// cost are different questions about the same journey.</b> Every Trip contributes to exactly one
    /// counter in each family, so folding them together would produce a family whose counters do not
    /// sum to the Trips that happened — which is the one property a reader of a Census family relies
    /// on without being told.
    /// </remarks>
    TripCosts,

    /// <summary>One counter of the Policy sweeps: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// <b>An eighth family, and the second the Sweep family owns.</b> It is not more
    /// <see cref="Zones"/>: a Zone Rule samples Lots and builds, a Policy sweeps a population and
    /// moves money, and <c>02 §4.2</c>'s whole table is the argument that those differ on coverage
    /// and on what they act by. Sharing an arithmetic would make <em>triggered</em> mean two things
    /// and would put a Zone Rule's sample beside a Policy's sweep as though the two counted the same
    /// event.
    /// </remarks>
    Policies,

    /// <summary>One money aggregate: a level, read at an instant and not aggregated.</summary>
    /// <remarks>
    /// <b>A ninth family, and the first since <see cref="Table"/> that is a level rather than a
    /// flow.</b> It is not a table counter, though it is read the same way: a table counter is a row
    /// count and this is a magnitude, and <c>Series.cs</c> widened a sample to 64 bits for exactly
    /// this member — <em>"a magnitude is a <c>Money</c> or a <c>Fixed</c> rather than a count"</em> —
    /// against a table counter that has never needed the width. Filing money as a synthetic table
    /// would make <c>live</c>, <c>slots</c> and <c>capacity</c> name three things money has none of.
    /// </remarks>
    Money,

    /// <summary>One money magnitude the Policy sweeps moved: a flow, accumulated and drained.</summary>
    /// <remarks>
    /// <b>A tenth family, and the pair to <see cref="Money"/> rather than to
    /// <see cref="Policies"/>.</b> <c>plans/0033</c> task 7's sentence is that a balance sheet is a
    /// level and a flow at once, and these two families are that sentence: <see cref="Money"/> is
    /// where the money is and this is what moved. Kept out of <see cref="Policies"/> because that
    /// family counts events and this one measures amounts.
    /// </remarks>
    MoneyFlow,
}

/// <summary>
/// What a <see cref="Census"/> series is a series <em>of</em>: one counter of one table, or one
/// counter of the Rule engine.
/// </summary>
/// <remarks>
/// <para>
/// <b>An id and a number, never a name.</b> <c>adr/0002</c> gives the core ids and gives the shell
/// every string a human reads, and a metric identified by a string would have put the vocabulary of
/// the panel inside the simulation. <see cref="Table"/> is the index into <c>World.Tables</c>, which
/// is declaration order — the same order the State Hash folds tables in, so a metric means the same
/// thing to a trace as it does to a hash.
/// </para>
/// <para>
/// <b>A table metric names a table rather than a collection because the intrusive-list pattern makes
/// those the same thing.</b> Every variable-length structure in the core is an intrusive index list
/// whose nodes are rows, so the total length of every list over a table is that table's live row
/// count and there is no separate collection to sample. A list whose nodes are live rows missing from
/// it is not a magnitude problem but a correctness one, and belongs to the invariant tiers rather
/// than here.
/// </para>
/// <para>
/// <b>The accessors for the family a metric is not are errors rather than defaults.</b> A
/// <see cref="Table"/> of zero on a Rule metric would be a valid table index and would read as the
/// first table, so the wrong question gets an answer that looks right. Constructed only through the
/// two factories, for the same reason.
/// </para>
/// </remarks>
public readonly record struct Metric
{
    private readonly int _table;
    private readonly byte _counter;
    private readonly byte _aggregate;

    private Metric(MetricSource source, int table, byte counter, byte aggregate)
    {
        Source = source;
        _table = table;
        _counter = counter;
        _aggregate = aggregate;
    }

    /// <summary>Which family this names.</summary>
    public MetricSource Source { get; }

    /// <summary>Index into <c>World.Tables</c>, in declaration order.</summary>
    public int Table => Source is MetricSource.Table
        ? _table
        : throw new InvalidOperationException($"a {Source} metric does not name a table.");

    /// <summary>Which of the table's counters.</summary>
    public CensusCounter Counter => Source is MetricSource.Table
        ? (CensusCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a table counter.");

    /// <summary>Which of the Rule engine's counters.</summary>
    public RuleCounter RuleCounter => Source is MetricSource.Rules
        ? (RuleCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a Rule counter.");

    /// <summary>Which of the Zone Rules' counters.</summary>
    public ZoneCounter ZoneCounter => Source is MetricSource.Zones
        ? (ZoneCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a Zone counter.");

    /// <summary>Which of the placement pass's counters.</summary>
    public PlacementCounter PlacementCounter => Source is MetricSource.Placement
        ? (PlacementCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a placement counter.");

    /// <summary>Which of Tick phase 4's Fate counters.</summary>
    public TripCounter TripCounter => Source is MetricSource.Trips
        ? (TripCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a Trip counter.");

    /// <summary>Which of the job assignment pass's counters.</summary>
    public JobCounter JobCounter => Source is MetricSource.Jobs
        ? (JobCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a job counter.");

    /// <summary>The money aggregate this names.</summary>
    /// <exception cref="InvalidOperationException">This metric is not a money level.</exception>
    public MoneyCounter MoneyCounter => Source is MetricSource.Money
        ? (MoneyCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not name a money aggregate.");

    /// <summary>The money movement this names.</summary>
    /// <exception cref="InvalidOperationException">This metric is not a money flow.</exception>
    public MoneyFlowCounter MoneyFlowCounter => Source is MetricSource.MoneyFlow
        ? (MoneyFlowCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not name a money movement.");

    /// <summary>Which of the Policy sweeps' counters.</summary>
    public PolicyCounter PolicyCounter => Source is MetricSource.Policies
        ? (PolicyCounter)_counter
        : throw new InvalidOperationException($"a {Source} metric does not carry a policy counter.");

    /// <summary>Which bucket of the Trip cost histogram.</summary>
    public TripCostBucket TripCostBucket => Source is MetricSource.TripCosts
        ? (TripCostBucket)_counter
        : throw new InvalidOperationException($"a {Source} metric does not name a cost bucket.");

    /// <summary>How the counter's Ticks are reduced into one reading.</summary>
    /// <remarks>
    /// Meaningful only for a flow. A table counter is read at an instant, so there is nothing over
    /// which to take a sum or a peak and asking is a mistake rather than a default.
    /// </remarks>
    public Aggregate Aggregate =>
        Source is MetricSource.Rules or MetricSource.Zones or MetricSource.Placement
            or MetricSource.Trips or MetricSource.Jobs or MetricSource.TripCosts
            or MetricSource.Policies or MetricSource.MoneyFlow
        ? (Aggregate)_aggregate
        : throw new InvalidOperationException($"a {Source} metric is a level and is not aggregated.");

    /// <summary>One counter of one table.</summary>
    /// <param name="table">Index into <c>World.Tables</c>, in declaration order.</param>
    /// <param name="counter">Which of the table's counters.</param>
    public static Metric Of(int table, CensusCounter counter) =>
        new(MetricSource.Table, table, (byte)counter, 0);

    /// <summary>One counter of the Rule engine, under one reduction.</summary>
    /// <param name="counter">Which of the engine's counters.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(RuleCounter counter, Aggregate aggregate) =>
        new(MetricSource.Rules, 0, (byte)counter, (byte)aggregate);

    /// <summary>One counter of the Zone Rules, under one reduction.</summary>
    /// <param name="counter">Which of the Sweep family's counters.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(ZoneCounter counter, Aggregate aggregate) =>
        new(MetricSource.Zones, 0, (byte)counter, (byte)aggregate);

    /// <summary>One counter of the placement pass, under one reduction.</summary>
    /// <param name="counter">Which of the pass's counters.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(PlacementCounter counter, Aggregate aggregate) =>
        new(MetricSource.Placement, 0, (byte)counter, (byte)aggregate);

    /// <summary>One Trip Fate counter, under one reduction.</summary>
    /// <param name="counter">Which Fate.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(TripCounter counter, Aggregate aggregate) =>
        new(MetricSource.Trips, 0, (byte)counter, (byte)aggregate);

    /// <summary>One counter of the job assignment pass, under one reduction.</summary>
    /// <param name="counter">Which of the pass's counters.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(JobCounter counter, Aggregate aggregate) =>
        new(MetricSource.Jobs, 0, (byte)counter, (byte)aggregate);

    /// <summary>One bucket of the Trip cost histogram, under one reduction.</summary>
    /// <param name="bucket">Which band of clock minutes.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(TripCostBucket bucket, Aggregate aggregate) =>
        new(MetricSource.TripCosts, 0, (byte)bucket, (byte)aggregate);

    /// <summary>One counter of the Policy sweeps, under one reduction.</summary>
    /// <param name="counter">Which of the sweeps' counters.</param>
    /// <param name="aggregate">How its Ticks are reduced into one reading.</param>
    public static Metric Of(PolicyCounter counter, Aggregate aggregate) =>
        new(MetricSource.Policies, 0, (byte)counter, (byte)aggregate);

    /// <summary>Names one money aggregate. A level, so it takes no <see cref="Aggregate"/>.</summary>
    /// <param name="counter">Which aggregate.</param>
    /// <returns>The metric.</returns>
    public static Metric Of(MoneyCounter counter) =>
        new(MetricSource.Money, 0, (byte)counter, 0);

    /// <summary>Names one money movement, reduced one way.</summary>
    /// <param name="counter">Which movement.</param>
    /// <param name="aggregate">How to reduce it over the interval.</param>
    /// <returns>The metric.</returns>
    public static Metric Of(MoneyFlowCounter counter, Aggregate aggregate) =>
        new(MetricSource.MoneyFlow, 0, (byte)counter, (byte)aggregate);
}
