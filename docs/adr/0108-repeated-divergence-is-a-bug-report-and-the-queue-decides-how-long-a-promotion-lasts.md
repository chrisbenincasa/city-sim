# Repeated divergence is a bug report, and the queue decides how long a promotion lasts

**The rotating audit gets no escalation mechanism. A divergent Segment is promoted, and how long it
stays Microscopic is decided by `03 §4` invariant 3's existing queue guard and by nothing else — so a
Segment that is genuinely failing holds itself until it stops, and one that diverged for any other
reason demotes on the next cycle. Permanent promotion is refused. The divergence *record* has a
consumer already named in this corpus — [`03 §5.1`](../03-agent-architecture.md)'s acceptance suite —
and repeated divergence at one Segment is evidence about `complexity_factor`, which is a measurement
and not a mechanism.**

**The audit is a third claim on the Microscopic Cap and it ranks last**, behind force-promotion and
stress-promotion, because it is the only one of the three whose refusal is a *deferral* rather than a
loss.

**The audit *rate* is not settled here and its unit is.** A rate stated as a sample size is the defect
[`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) fixed for
the Zone Rule; what a Ruleset states is a **coverage period** and the engine derives the count. The
period itself stays **unset**, with a named ratifier.

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`, `LEGIBLE CAUSE`.

## Why

`03 §3.5` closes the audit's divergence metric and leaves one sentence open: *"the audit rate, and what
happens on repeated divergence (permanent promotion? a flag for us?). The metric is settled."* `03 §6`
question 1 repeats it. This is milestone **23**'s deliverable, and it is the half that decides whether
the audit is an instrument a developer reads or a mechanism the city runs.

### Half of it was answered in the same document, on the same day, four sections later

`03 §5.1` says, in its own words:

> **The audit is the discovery route.** §3.5 already describes it as continuous running validation
> whose divergences are an always-on quality signal. A divergence found in a live city, reduced to a
> minimal deterministic scenario, *is* a new suite entry. **That closes a loop previously open at both
> ends: the audit had no consumer for what it found, and the suite had no source of new cases.**

Both sentences date from the repository's first commit. So *a flag for us* is not merely one of two
candidates — it is the answer, it has a **named consumer**, and the paragraph that supplies it says
outright that it is closing this loop. `§3.5`'s open question was never struck and `§6` copied it
forward. That is [`plans/0012`](../../plans/0012-corpus-audit.md) **Cause 1** inside one file, which
`05 §3` did to session J four sections apart and this does five.

⚠ **The parenthesis is why nobody noticed.** ***A question that offers its own candidate answers stops
being read as open in the ordinary way*** — a reader arriving at `§3.5` is handed a menu and does not
go looking outside it, and the menu's two items are *permanent promotion* and *a flag for us*, of
which the second is under-described rather than wrong. **A flag with no named consumer would have been
the defect 5b-bis task 6 named** — a Census family with no reader, built, wired, tested and printed
nowhere. This one has had a consumer since day one.

What `§5.1` does **not** answer is the other branch: whether the Segment stays promoted. That is the
part this decision has to take.

### Permanent promotion is refused, and each ground is a decision already in force

**It is a collection that grows with elapsed time.** Every audit cycle can add a Segment to the
permanently-promoted set and nothing ever removes one, so the set is monotone in Ticks. `03 §4`
invariant 5 and [`0006`](0006-no-collection-grows-with-elapsed-time.md) prohibit exactly that. It is
worse than an ordinary unbounded collection, because this one is a standing claim on the **Microscopic
Cap**: a city played long enough would find its whole Cap held by things the audit once found, and the
Cap is the scarcest resource in the tier.

**It makes fidelity a function of history.** `03 §4` invariant 4 states the form in one sentence — *"The
simulated set is a function of stress and tick. **Nothing else.**"* — and
[`0007`](0007-stress-driven-simulation-detail.md) is the decision that put fidelity on **place driven
by stress** in the first place. A Segment held Microscopic because of what an audit found forty
thousand Ticks ago is fidelity following **audit history**, which is a third term. *(It would remain
deterministic and replayable, so nothing mechanical would catch it — which is the reason to refuse it
in a document rather than trust a lint.)*

**And it is a remembered tally where this corpus has four times chosen a live read.**
[`0053`](0053-failure-pressure-is-a-duration-not-a-tally.md) made Failure Pressure a duration rather
than a tally; `adr/0063` made the wait-list wake predicate read live state;
[`0102`](0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)
made a Departure a comparison the Household re-runs rather than a threshold it crosses. A divergence
counter that eventually pins a Segment Microscopic is the shape all three refused.

### It is also unnecessary, because the persistence it reaches for already exists

`03 §4` invariant 3 carries a guard that has been in this document since its first commit:

> **A segment with a non-empty queue does not demote, regardless of stress.**

And `§3.5` describes its own detector in terms that make the guard bite:

> A junction failing at 40% of capacity is invisible to `v/c` by construction, but it is loud on this
> metric: **the statistical model predicts free-flow and the simulation finds a queue.**

**The divergence that justifies promoting a Segment *is* a non-empty queue.** So a promoted Segment
that is genuinely failing cannot demote while it fails, and demotes when it stops — which is the
behaviour permanent promotion was reaching for, keyed on **the failure** rather than on a memory of it,
with no new state, no counter and no threshold. The promotion lasts exactly as long as the thing it
was for.

⚠ **The guard sorts the two directions of divergence without being told they exist.** Travel time can
diverge upward or downward. Upward is a queue, and the guard holds the Segment. Downward — the
simulation traverses the Segment *faster* than the statistical model predicted — leaves no queue, so
the Segment demotes on the next cycle, which is correct: a road nothing is waiting on does not need
Microscopic simulation. Invariant 6 already types that second case and gives it the same destination
as the first: *"Exceeding it is a bug report about the statistical model, not grounds for widening the
tolerance."* **One guard written for hysteresis turns out to discriminate the two divergence signs**,
and nobody arranged that.

### The steelman, and why its cost belongs to the complexity factor

The case for permanent promotion is real and has to be answered rather than dodged. A junction that
fails through turning conflicts fails **every peak, every Day, until the player changes something**.
Under the rule above, the audit catches it once, the queue holds it through that peak, the queue clears
and it demotes — and the next Day's failure is invisible again until rotation happens to return. The
city therefore spends most of its life mis-simulating a junction it has **already diagnosed**, which
looks like knowledge being thrown away.

**What that argument actually wants is not a permanent promotion but a lower threshold, and `03 §3.3`
already has the term.** The complexity factor *"lowers the effective threshold for junctions with many
conflicting movements, so a complex junction enters microscopic simulation at lower volume than a
simple through-road"*, and `§3.6` names it as the standing mitigation for precisely this blind spot. A
junction the audit **repeatedly** finds diverging is, by construction, a junction whose complexity
factor is **too low** — it should have been promoting itself at that volume and was not.

So repeated divergence at one Segment is a **measurement of `complexity_factor`**, and under
[`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) it may not be
settled by machinery here. It is routed below. ***Holding the Segment promoted would be paying for ever
for a parameter that is wrong once.***

⚠ **A learned complexity factor was drafted in this sitting and is refused.** Letting the audit correct
the factor at runtime is attractive — it is bounded, per-Segment, self-limiting, and it keeps fidelity
a function of stress because the factor is a term *inside* stress rather than beside it. It is refused
on three counts. It introduces a **feedback term into the traffic model**, which milestone 5c task 8
established this corpus does not have and has not argued for — the priced and free-flow runs agree *to
the Citizen* on employment while their occupancies differ by 51.6%, which is
[`0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md) working as
decided and ***congestion is a cost paid and never a cost avoided***. It would open a **learning rate**,
a hash-bearing number with no ratifier, to avoid opening a measurement that has one. And it lands on
the exact term session E's **Q3** is about — whether Stress and Sight read the same quantity — so it
would deepen a divergence while that question is open. **Named here so that reaching for it later is a
decision rather than a drift.**

### The audit is a third claim on the Cap, and `adr/0062` enumerated two

[`0062`](0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md) sets the admission
order: *"force-promotion outranks stress-promotion, ties break on Stress and then on Segment id."* An
audit promotion is neither. It is a **third kind of claim** and that ADR does not have a rank for it —
an enumeration written before the third member existed, which is the same failure Q1 found in the
demotion list one question earlier.

**It ranks last, on `adr/0062`'s own principle.** That ADR orders by what a claim *buys*:
force-promotion buys **correctness** (spillback is one of `§5.1`'s three acceptance criteria),
stress-promotion buys **accuracy** (travel times from simulation rather than from a curve). An audit
promotion buys neither for the running city; it buys **knowledge about the model**. A resource limit
may cost the simulation accuracy and must not cost it a phenomenon — and it may certainly cost it a
measurement.

⚠ **The asymmetry that makes this cheap: an audit refused is deferred, not lost.** Rotation is a
function of Tick, so a Segment refused admission comes round again, and `§3.5` already accepts that
latency in its own words — *"its coverage rate is low by design, so a problem may persist for some time
before its segment comes up. That is acceptable."* A refused **stress**-promotion is an inaccuracy that
happened and cannot be re-run; a refused **force**-promotion is a phenomenon that did not occur. Only
the audit's loss is recoverable by waiting, and it is the one already budgeted for.

⚠ **And it almost never has to be refused, because `adr/0062` counts Vehicles.** An audited Segment is
**unstressed by construction** and therefore nearly empty, so it costs almost nothing in the unit the
Cap is denominated in. Under the Segment denomination `adr/0062` retired, a sample of *N* Segments
would have consumed *N* slots and competed head-on with *N* jammed arterials. ***`adr/0062` made the
audit affordable in a section it never mentions*** — which is what choosing the right unit buys, and it
is worth recording because that ADR's own consequence list could not have predicted it.

### The rate is a duration, and `03 §3.5` states it in the unit `adr/0059` retired

`§3.5`'s third bullet reads *"It is deterministic and fixed-cost. Rotation is a function of tick; **the
sample size is a constant**."*

**That is `02 §5.7`'s defect verbatim.** `adr/0059` found the Zone Rule's `sample` stated as an
absolute count and showed the consequence is arithmetic rather than a badly chosen number: *"Any
constant makes the revisit period proportional to the Lot count."* Here the population is Segments —
33,024 on the shipped lattice, **525,312** if a player pays for a 512-Cell map's worth of road
([`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)) — so a constant sample makes
the time to look at every Segment once proportional to how much road the city has.

⚠ **And it is the same *bundling* error, in the same shape.** `adr/0059` found that *"the cost claim and
the pacing claim were doing different jobs under one bullet"*: the cost claim survived and the pacing
claim did not. `§3.5`'s bullet 3 bundles **deterministic** and **fixed-cost**, and the constant sample
delivers the second while silently setting the coverage period — which **bullet 1** depends on, because
*"it catches failures the trigger structurally cannot"* is true only if the audit **reaches** them. ***A
constant sample makes the audit's only stated benefit inversely proportional to the size of the city's
road network.***

So a Ruleset states a **coverage period** — how long before every Segment has been looked at once — and
the engine derives `sample = ceil(Segments × interval ÷ coverage_period)`. This is `adr/0059`'s shape
for the **fifth** time, after `pollution_decay_ticks`, the Zone Rule's `revisit_ticks`, `[placement]`
and `[jobs]`.

⚠ **Unlike `adr/0059` there is no forced default, and the reason is new as of last Day.** `adr/0059`
could derive one Day from `TICKS_PER_DAY` and open no free parameter. That move is **not available
here**, because
[`0101`](0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md) gave the Day a
**shape** on 2026-08-15 — a morning peak, a broad one, a flatter evening, a quiet night. A rotation
*"selected deterministically by tick"* whose coverage period is commensurate with `TICKS_PER_DAY` visits
every Segment at **the same phase of the Day for ever**, so the audit would be structurally blind to any
junction that fails outside its own sampling phase — and a junction that fails at peak and recovers is
the entire phenomenon `§3.6` describes. **The period must precess against the Day.** That is a property
of the pair rather than of either, so it is a constraint milestone 23's rotation function and this
number have to satisfy **together**, and the loader is where it is refused — this corpus already refuses
`[jobs]` without a Commute Budget and `[traffic]` without car ownership.

***Before `adr/0101` the Day had no shape and this hazard could not have been stated***, which is why
`§3.5` does not mention it and is not wrong to omit it.

## What is argued here and what is routed

| Settled here | Type |
|---|---|
| No escalation mechanism; the queue guard decides a promotion's length | **arguable** |
| Permanent promotion is refused | **arguable** — `adr/0006`, invariant 4, and the live-read family |
| The divergence record's consumer is `§5.1`'s suite | **arguable**, and largely already written there |
| The audit ranks third in `adr/0062`'s admission order | **arguable** — on that ADR's own ordering principle |
| The rate is a **coverage period**, not a sample size | **arguable** — `adr/0059`'s unit argument, which is arithmetic |
| The period must precess against `TICKS_PER_DAY` | **arguable** — `adr/0101` gave the Day a shape |

| Routed, and no document may cite it as decided | The refuting number | The machine, the world and the quantity |
|---|---|---|
| **Does the static complexity factor predict which junctions the audit finds diverging?** | divergence rate per junction against its geometric complexity factor | milestone **23**'s audit, on a world containing a low-volume turning failure |
| **The coverage period's value** | detection latency, two-sided — see below | named in full below |

**The coverage period stays unset, and that is a gap rather than a debt** — nothing accretes on a value
that does not exist, which is `adr/0052`'s triage. Its ratifier is nameable in all three parts, which is
what that ADR's 2026-08-15 amendment and `adr/0098`'s 2026-08-15 finding together require:

- **The machine** — [`0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)'s
  reference class, one core.
- **The world** — a city whose Streets were laid by `CommandKind.Connect` and which contains a junction
  that fails at low volume through turning conflicts. **A generated city cannot supply it**: 5c task 8
  measured `v/c` peaking at 0.44 at 4,000, 16,000 and 64,000 Citizens alike, because the paved extent
  scales with √population and ***the same number sizes both the demand and the supply***.
  `ConnectedCityCongestionTests` is the shape such a world takes.
- **The quantity** — **detection latency**, in Ticks, from a junction beginning to diverge to the audit
  reaching it. **Too long** is refuted against `§3.5`'s own stated standard, that a delay is acceptable
  *"where the symptom is visible — a player who notices something wrong will look"*: a latency longer
  than a play session means the player is the detector. **Too short** is refuted on the share of the Cap
  the audit takes from stress-promotion.

⚠ **None of the three exists yet.** The Microscopic tier is milestone **21**, the audit is **23**, and a
world with a turning failure needs 21's Overlaps. So the number is unsettleable today for a reason that
is written down rather than for want of attention.

## Rejected

**Permanent promotion.** Above. It is named rather than merely unchosen, because `§3.5`'s parenthesis
offers it and a reader who takes the menu at face value would read it as the leading candidate.

**A divergence counter with a threshold.** The intermediate form — *promote permanently after N
divergences* — inherits every objection to permanent promotion and adds a hash-bearing `N` with no
ratifier. It is the tally shape `adr/0053` refused.

**A learned complexity factor.** Above, and the most attractive of the three.

**Widening the tolerance when divergence is frequent.** `03 §4` invariant 6 forbids it in terms —
*"incremental widening is exactly how this design would slide into the failure mode it exists to
prevent"*. Recorded because a frequent-divergence rule is exactly the circumstance under which somebody
proposes it.

## Consequences

- **`03 §3.5`'s open question closes on its escalation half and narrows on its rate half**, and the
  parenthesis goes with it. `03 §6` question 1 follows.
- **`03 §4` invariant 3's queue guard acquires a second job.** It was written to protect hysteresis; it
  is now also what bounds an audit promotion, and it discriminates the two signs of divergence. **A
  future proposal to weaken it is therefore larger than it looks**, which is the note to carry forward —
  `adr/0062`'s refused **virtual queue** is such a proposal.
- **`adr/0062`'s admission order gains a third rank** and is amended there. Its two-member enumeration
  was written before the audit was read as a promoter, which is the same class of defect
  [`0107`](0107-a-demotion-discards-the-cursor-and-nothing-it-discards-has-to-be-invented.md) found in
  the demotion list one question earlier: ***an enumeration is written against the members that exist
  when it is written, and nothing re-counts it.***
- **`03 §5.1`'s suite gains its first named producer for a *live* case.** Everything else on that suite
  is a scenario somebody wrote; this is the route by which the running city adds one.
- **Milestone 23's deliverable is now scopeable**, which it was not: an audit whose response to its own
  findings is undecided has no acceptance criterion. What 23 builds is the rotation, the comparison, the
  record and the Cap-ordered admission — and **no escalation machinery at all**, which is a smaller
  milestone than the roadmap row implies.
- **A `[audit]` Ruleset table is foreseen and not written.** It would carry the coverage period and the
  interval. Nothing may be authored before the period has a ratifier, and the loader will refuse a
  period commensurate with `TICKS_PER_DAY`.
- **This opens one `adr/0052` number and closes none**, and the number is a **duration** rather than the
  count `§3.5` implies — so §D gains a row whose unit is already correct, rather than gaining one that
  would later have to be retyped.

## What would trigger revisiting

- **A junction failing so persistently that the queue never empties.** The guard then holds the Segment
  Microscopic indefinitely, which is permanent promotion arrived at honestly — by the failure rather
  than by a counter. That is the correct outcome and it is worth noticing when it happens, because it
  means the complexity factor is badly wrong rather than slightly.
- **The complexity factor turning out to predict divergence well.** The routed measurement above. If it
  does, `§3.6`'s blind spot is smaller than stated and the audit's rate can be coarser; if it does not,
  the factor's static derivation is what needs work and this decision is unaffected either way.
- **Turning movements coming off [`deferred.md`](../deferred.md).** That document's own third revisit
  trigger already contemplates the Microscopic half arriving alone — *"a fidelity decision inside the
  traffic model"* — but if the **Statistical** side ever accumulates per-movement volume, `v/c` stops
  being blind to the blind spot and the audit loses its principal job.
- **The audit being made to consume the Cap non-trivially.** The last-rank ordering is cheap because an
  audited Segment is nearly empty. A variant that audited *stressed* Segments, or that held a sample
  Microscopic across many Ticks to watch it, would change the arithmetic and the ordering with it.
- **A second lossy path** ([`0062`](0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md)'s
  virtual queue, or any Cap-pressure demotion). It would let a queued Segment demote, which is the guard
  this decision leans on, so an audit promotion would need a bound of its own.
