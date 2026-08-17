# 0029 — Session E: fidelity, and what a demotion is allowed to lose

**The brief and the record.** Session E grills [`adr/0005`](../docs/adr/0005-two-fidelity-tiers.md)
*two fidelity tiers, and decisions are never shared* and
[`adr/0007`](../docs/adr/0007-stress-driven-simulation-detail.md) *simulation detail follows network
stress, not the camera* — two of the six decisions [`0002`](0002-open-questions.md) records as
**written from research and never argued**. It is **one session and not two**, because `0007` moved
Fidelity from person to **place** and `0005`'s tiers are what it moved.

**It unblocks milestones 22** (Stress-driven Fidelity with hysteresis) **and 23** (the rotating
Audit), and it is what decides their **positions**, which [`06`](../docs/06-roadmap.md) marks
*provisional — session E moves it* on both rows.

**Gate: none.** 22's gate was stated wider than the milestone — `adr/0005`'s own last line says its
fidelity half was superseded by `0007`, so 22 needs the **`0007` half alone** — and its second gate,
5c for Segment volume, closed 2026-08-16.

---

## Task 0 — the typing pass, run before anything was grilled

[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
requires every claim to be typed before it is settled: *name the number that would refute this, and
the machine that would produce it.* Session D established the pass as a task rather than a habit, and
it is run here first because **this pair of ADRs is unusually rich in claims a session must not
touch.**

**The result is that session E is much smaller than *grill two ADRs*, and its real content is two
enumerations nobody has ever written.**

| Claim | Type | Owner | May E close it? |
|---|---|---|---|
| `T_high` / `T_low`, the hysteresis thresholds | **measurable** | a Phase 2 sweep, [`0002`](0002-open-questions.md) §B | **No.** [`03 §3.3`](../docs/03-agent-architecture.md) says in its own words *"the two thresholds are measured, not chosen"* |
| Is force-promotion needed at all? | **measurable** | `§5.1`'s spillback scenario with it disabled, §B | **No** — session D retyped it already |
| Does `03 §3.4`'s self-correction loop still close? | **measurable** | R8.5's instrument on a variant-supplied route set, §B | **No** |
| Lossy demotion under Cap pressure; the virtual queue; VDF error against queue age | **measurable** | `§5.1`'s acceptance suite, milestone 21 | **No** |
| The Microscopic Cap's value | **measurable**, and half-unmeasured | S5 supply / an unbuilt demand side | **No** |
| A fallback tier below Microscopic | **undesigned, deliberately** | [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) | **No** — *given the Cap is too small, should something compensate?* is the void form verbatim |
| **What a demotion is allowed to discard** (`03 §4` invariant 3) | **arguable** | nobody | **YES — Q1** |
| **What happens on repeated audit divergence** (`03 §3.5`, `§6.1`) | **arguable** | nobody | **YES — Q2** |
| **Whether Stress and Sight read the same quantity** | **arguable** | nobody | **YES — Q3** |
| **22 and 23's positions in Phase 2** | **arguable** | this session, by `06`'s own marking | **YES — Q4** |
| `adr/0005`'s *decisions are never shared* | arguable | untouched | **Out of scope by choice** — see *What this session does not do* |

⚠ **The audit *rate* is a number and not a claim**, so it is
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)'s
and not `adr/0043`'s. It may be chosen here only with a named ratifier — **a machine, a world and a
quantity** — and `§3.5` already fixes what it would be read against by settling the metric. If no
ratifier can be named, it stays unset and that is a **gap rather than a debt**.

---

## Q1 — What is a demotion allowed to lose?

`03 §4` invariant 3 reads *"Demotion is lossy only in enumerated ways. Write down what is discarded
when a Traveller leaves a Microscopic segment. Anything not enumerated is a bug."* **The enumeration
has never been written.** It exists in no ADR, no design section and no plan; the invariant is a
promise to enumerate, standing in for the enumeration.

**It was blocked and it is not blocked any more, and that is why this is the session's first
question.** [`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) says so
outright — *"an enumeration cannot be written against a structure nobody has specified"* — and then
specifies the structure: a Leg is a plan, a Traveller is a cursor. So the precondition this invariant
was waiting on has been discharged since 2026-08-11 and nothing went back for it.

**This is milestone 22's deliverable.** A promotion/demotion mechanism whose lossy direction is
unenumerated has no acceptance criterion, which is the concrete reason 22 cannot be scoped today.

⚠ **One guard already exists and must not be mistaken for the enumeration**: `§4` invariant 3's own
note makes a **non-empty queue** block demotion regardless of stress. That is a rule about *when* a
demotion may happen. Q1 asks what is lost *when one does*.

### ✅ CLOSED — [`adr/0107`](../docs/adr/0107-a-demotion-discards-the-cursor-and-nothing-it-discards-has-to-be-invented.md), 2026-08-16

**Four fields: position along the Lane, velocity, Lane assignment, a Switch Lane traversal in
progress** — and each rebuilds at free flow, so `03 §4` invariant 2 holds across a whole
demote/promote cycle rather than only across a promotion. `03 §4` invariant 3 carries the list;
`adr/0075`'s consequence bullet is amended in place.

**The *"what would make this session wrong"* case below half-landed, and the half that did not is the
finding.** The enumeration did exist — twice, inside `adr/0075` — so Q1 was not the blank sheet the
brief expected. **It was not void as posed either, because the list was wrong**, and that is a better
reason for the question than the one it was asked with.

⚠ ***A list that names a derived quantity as state cannot be checked against a structure.*** Of
`adr/0075`'s three fields, **headway is not a field at all** — `03 §5` has vehicles hold no
references to each other, so it is computed inside the Lane's single pass — and **queue position**
resolves into the metric offset car-following integrates. **Velocity and Lane assignment were
missing.** So the list was **too long and too short at once**, and it failed in the direction nobody
guards against: an over-long enumeration reads as *more* careful, and nothing in the corpus can catch
it except holding it against `§5`, which no mechanical check does.

⚠ **Three writes were owed and none had landed.** `adr/0075` declared the enumeration written; `03 §4`
went on reading as owing one; [`adr/0062`](../docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md)
counted *"one lossy path and one enumeration today"* against a list that was never checked. **The
routing was wrong too** — `adr/0075` sends the artefact to milestone **21**, which is gated on session
**G**, when invariant 3 is milestone **22**'s and 22 is gated on nothing. ***Sending an artefact behind
a gate it does not need is how it stays unwritten***, and it stayed unwritten for five days after the
ADR that owed it declared it writable.

⚠ **A fourth finding, checked against the build rather than argued: `adr/0075` carries the error and
the warning against it in two amendments dated the same day.** Its **task 3** amendment says *"position
along the route is precisely what demotion discards — a Statistical Traveller resumes from its arrival
Tick"*. Its **task 6** amendment, sixteen lines below, says a vehicular Leg must store its route
because losing it mid-journey *"strands it on a Segment it never leaves, which is an `adr/0006`-class
leak presenting as a road busy for ever with nothing on it"*. **A demotion that discarded the route
cursor would do deliberately what the second forbids happening by accident.**

`TripEngine.AdvanceTravellers` settles it: the build walks `TravellerTable.CurrentHop` and calls
`Leave` and `Enter` on every crossing, which is `adr/0041`'s attribution. ***Two cursors one word
apart*** — the **route** cursor survives a demotion and must; position along the **Lane** is what goes.

⚠ **And *"a Statistical Traveller resumes from its arrival Tick"* is false of the built Statistical
tier**, which is the whole build, since fidelity is unbuilt. It describes `03 §3.1`'s *summary table*
rather than the mechanism `adr/0099` shipped four months later — [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
exactly. ⚠ **Its offered reconstruction is gone too**: *from the route and the clock* held while a
Segment's cost was a function of the plan, and `adr/0099` prices each Segment on entry from its live
volume — so ***the journey whose position is least recoverable is the congested one, which is the only
journey ever promoted.*** Withdrawn in `adr/0075`, carried in `adr/0107` and in `03 §4`.

⚠ **The framing of that finding was corrected before it was left, with the user in the room, and the
correction is worth more than the finding.** The first draft said the sentence was *"false of the
built Statistical tier"*. **It is a conflict between two decisions** — `03 §3.1`'s table says
*time-advanced*, `adr/0041` requires *"a next Segment every Tick"* and `adr/0099` prices Segment by
Segment — and the build only shows **which reading was implemented**. ***Citing the build as the
ground rather than as corroboration is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
run backwards***: that ADR warns against trusting a description of the build, and the mirror failure is
letting the build's current shape stand in for an argument. Every claim in `adr/0107` now names a
**decision** as its ground and the build as corroboration.

### ⚠ A fifth finding, and it is about `adr/0007` rather than about demotion

**`adr/0007`'s *"unstressed segments are free-flow… not an approximation but an exact answer"* is an
idealisation, and it was load-bearing.** Milestone 5c put the volume-delay function on **every**
Segment (`adr/0099`), so a Statistical Segment's travel time equals free flow only at zero volume: at
the shipped `α = 15%`, `β = 4` it runs **6.1%** slow at `v/c` 0.8 and **15%** slow at 1.0.

⚠ ***The idealisation is worst exactly at the promotion boundary***, because a Segment just below
`T_high` is the most loaded one still Statistical — which is the only place a promotion ever reads it.
**`adr/0107`'s first draft derived a promoted Traveller's entry velocity from that clause** — *free
flow, as a fact about the tier it came from* — which would have shipped **a plausible default wearing
a derivation**, wrong by 6–15% in the one regime it is evaluated in. The rule now reads `adr/0099`'s
**dwell**, which is exact, already computed, and needs no idealisation; the ADR was **renamed** for it,
since its claim was *rebuilds at free flow* and is now *nothing it discards has to be invented*.

***A sentence that idealises for the sake of an argument becomes a premise for the next argument***,
and this one held for four months before anything needed it to be exact. `adr/0007` is amended and
**the tier split is untouched** — the accurate form is that the VDF runs everywhere and is *replaced*
where it is weak, rather than being *used* only where it is strong.

### ⚠ Three aftershocks, found by Q2 and belonging to Q1

**Q1 was committed and then Q2 read `adr/0016`, which nothing in Q1 had opened.** All three are
repaired in place; the middle one is a defect in `adr/0107` itself.

⚠ **The retired list has a *source*, and Q1 traced it only to the document that quoted it.**
[`adr/0016`](../docs/adr/0016-the-lane-is-the-entity-not-the-car.md) is where *queue position, headway,
and any in-progress Switch Lane traversal* was written; `adr/0075` and `adr/0062` both quote it, the
latter **by name**, and `plans/0017` and `plans/0019` carry it. So it was **four copies and not two**.
***Tracing a wrong sentence to the document that quoted it is not tracing it to the document that wrote
it*** — and the tell was in `adr/0062` in plain sight, in the words *"which `adr/0016` names"*.
**Nothing in this corpus could have caught the list except holding it against `adr/0016`'s own opening
sentence**, which is where *vehicles hold no references to each other* is stated and is what makes
headway not a field.

⚠ **`adr/0107` committed the error it had just diagnosed, one bullet away from where it fixed it.** Its
reconstruction rule for *position* read *"the Segment's entry point"*, citing `adr/0041`'s attribution
on entry. **That is a plausible default wearing a derivation** — the exact failure the *velocity*
bullet had been rewritten to avoid hours earlier. `adr/0041` says volume is *attributed* on entry; it
does not say a promotion *happens* on entry, and a Segment crosses `T_high` on whatever Tick its volume
crosses it, with every Traveller already on it partway through. ***An ADR about when a counter moves is
not an ADR about when a Traveller is promoted.*** The rule is now the fraction of `adr/0099`'s dwell
already elapsed, so **position and velocity are one derivation read once** rather than two rules that
happened to agree — which is a tighter ADR than the one that was right by accident. It also makes the
guarantee independent of a real ambiguity in `03 §4`'s queue guard (*"non-empty queue"* reads against
`§5`'s **data structure** or against a **jam**, and the two gave different answers under the entry-point
rule and give the same one now).

⚠ **`adr/0107` knocked a leg out from under a decision it cited and did not check.** `adr/0062` refuses
eviction on the ground that *"a Segment that is already Microscopic holds state that cannot be
reconstructed"* — and `adr/0107`'s whole second half is that **each of the four is** reconstructed.
That decision survives on its other leg, which is stated two lines below in its own text: eviction
destroys the **queue**, and with it hysteresis at the instant it would have been observed. **Same shape
as `adr/0105` firing `adr/0096`'s revisit trigger** — one of two legs falls and the decision stands —
and it is worth noticing that ***a consequence bullet naming the ADRs a decision affects is not a
search for the ADRs that rest on it***.

⚠ **And a fourth, which is about a spike rather than about Q1.** `adr/0016`'s S5 amendment says the
enumeration is *"short by one"* — the **arrival Tick**, from L3's *150,943 of 186,624 Vehicles had no
arrival Tick to convert to* (that fixture is 2,592 Segments × 72 Vehicles and is an **upper bound**,
S2 R2's uniform origin-destination draw being the longest-trip distribution available; the **share** is
what is read here and it is the fixture's, since that Segment carries exactly jam density). **It is not
a fifth field.** An arrival Tick is not *discarded* by a
demotion; it is a conversion that cannot be *performed*, and a list of losses cannot hold an item that
never existed to be lost. ⚠ **More than that, the demotion L3 measured is prohibited**: `03 §4`
invariant 3's *"a segment with a non-empty queue does not demote, regardless of stress"* has been in
that document **verbatim since 2026-07-30**, twelve days before L3 ran, and a jammed Segment has a
non-empty queue by definition. ***A spike measured a forbidden operation and filed the result as a gap
in the enumeration***, and three documents carried it onward. The guard is *conservative* relative to
the failure — it bars demotion on occupancy where the failure needs Vehicles at rest — so the number is
**evidence for the guard**, not a debt against the list. Its real home is `adr/0062`'s **refused second
lossy path**, which it prices at **80.9%** and which nobody had connected to it.

## Q2 — What happens when the audit keeps finding divergence?

`§3.5` closes the metric and leaves the response open in its own words — *"the audit rate, and what
happens on repeated divergence (permanent promotion? a flag for us?)"* — and `§6.1` repeats it.
**This is milestone 23's deliverable**, and it is the half that decides whether the Audit is an
instrument the developer reads or a mechanism the city runs.

⚠ **The two candidates in that parenthesis are not alternatives on one axis.** *Permanent promotion*
spends the **Microscopic Cap** on a Segment that is not stressed, which is a claim on the scarcest
resource in the tier and one `adr/0062` gives an admission order for. *A flag for us* spends nothing
and reaches no player. A third position — feed it back to `§5.1`'s suite, which `§5.1` already names
as **the discovery route** — is the one the corpus has half-written and nobody has taken.

### ✅ CLOSED — [`adr/0108`](../docs/adr/0108-repeated-divergence-is-a-bug-report-and-the-queue-decides-how-long-a-promotion-lasts.md), 2026-08-16

**There is no escalation mechanism, and the rate's *unit* is settled while its value stays unset.**
`03 §3.5`'s open question and `03 §6` question 1 both close on the escalation half.

**Half the question was answered in the same document, on the same day, five sections later.** `§5.1`'s
*the audit is the discovery route* names the consumer and says in its own words that it is *"closing a
loop previously open at both ends: the audit had no consumer for what it found"* — and both sentences
date from the repository's **first commit**. So *a flag for us* was the answer all along, under-described
rather than wrong. ⚠ ***A question that offers its own candidate answers stops being read as open in the
ordinary way***: a reader arriving at `§3.5` is handed a menu and does not go looking outside it, and
`§6` copied the parenthesis forward for four months. `plans/0012` **Cause 1** inside one file — the same
shape `05 §3` produced for session J, four sections apart, and this one five.

**Permanent promotion is refused on three decisions already in force** — `adr/0006` and invariant 5
(the permanently-promoted set is monotone in Ticks, and it is a standing claim on the *scarcest*
resource in the tier), invariant 4's *"a function of stress and tick. **Nothing else**"* (it would add
audit **history** as a third term), and the live-read family `adr/0053`, `adr/0063` and `adr/0102` have
now chosen three times over a remembered tally.

⚠ **And it is unnecessary, because the persistence it reaches for already exists and nobody had joined
the two sentences.** `§4` invariant 3's queue guard bounds a promotion; `§3.5`'s own detector paragraph
says the divergence *is* a queue (*"the statistical model predicts free-flow and the simulation finds a
queue"*). So a genuinely failing Segment holds itself until it stops, **keyed on the failure rather than
on a memory of it**, with no new state, no counter and no threshold. ⚠ **The guard also discriminates
the two *signs* of divergence without being told they exist** — upward leaves a queue and holds,
downward leaves none and demotes, which is correct, and invariant 6 already routes both to the same
place as *"a bug report about the statistical model"*. **One guard written for hysteresis turned out to
do three jobs.**

⚠ **The steelman is real and its cost belongs to `complexity_factor`.** A junction that fails through
turning conflicts fails every peak, every Day, so the city spends most of its life mis-simulating
something it has already diagnosed. But what that wants is a **lower threshold**, and `§3.3` already has
the term — `§3.6` names it as the standing mitigation. **A junction the audit repeatedly catches is by
construction one whose complexity factor is too low**, so repeated divergence is a *measurement of that
parameter* and under `adr/0043` may not be settled by machinery here. ***Holding the Segment promoted
would pay for ever for a parameter that is wrong once.*** ⚠ **A learned complexity factor was drafted
and refused** — it is attractive, bounded and self-limiting, and it puts a **feedback term** in a
traffic model 5c task 8 established has none by decision (`adr/0046`, *congestion is a cost paid and
never a cost avoided*), opens a learning rate with no ratifier to avoid opening a measurement that has
one, and lands on the exact term **Q3** is about. Named so reaching for it later is a decision.

⚠ **The audit is a *third* claim on the Cap and `adr/0062` enumerated two** — the same failure Q1 found
in the demotion list, one question earlier. ***An enumeration is written against the members that exist
when it is written, and nothing re-counts it.*** It ranks **last**, on that ADR's own ordering
principle (force-promotion buys correctness, stress-promotion buys accuracy, the audit buys knowledge),
and the asymmetry is what makes last rank cheap: ***an audit refused is deferred, not lost***, because
rotation is a function of Tick and `§3.5` already budgets for that latency in its own words. ⚠ **And it
almost never has to be refused, *because* `adr/0062` counts Vehicles** — an audited Segment is
unstressed by construction and therefore nearly empty, where the Segment denomination that ADR retired
would have had a sample of *N* Segments compete head-on with *N* jammed arterials. ***Choosing the right
unit made the audit affordable in a section that decision never mentions***, which its own consequence
list could not have predicted.

⚠ **The rate is stated in the unit `adr/0059` retired.** `§3.5`'s *"the sample size is a constant"* is
`02 §5.7`'s defect verbatim: any constant makes the time to look at every Segment once proportional to
how much road the city has — 33,024 Segments today against **525,312** on a fully paved 512-Cell map —
and it is the same **bundling** error, since bullet 3 pairs *deterministic* with *fixed-cost* exactly as
`02 §5.7` paired cost with pacing. The constant delivers the cost claim and silently sets the coverage
period that **bullet 1** depends on, so ***a constant sample makes the audit's only stated benefit
inversely proportional to the size of the city's road network***. A Ruleset states a **coverage period**
and the engine derives the count — `adr/0059`'s shape for the **fifth** time.

⚠ **Unlike `adr/0059` there is no forced default, and the reason became statable one Day ago.**
`adr/0101` gave the Day a **shape** on 2026-08-15, so a rotation *"selected deterministically by tick"*
whose period is commensurate with `TICKS_PER_DAY` visits every Segment at the **same phase of the Day
for ever** — structurally blind to a junction that fails at peak and recovers, which is the entire
phenomenon `§3.6` describes. **The period must precess against the Day**, which is a property of the
pair rather than of either. ***Before `adr/0101` the Day had no shape and this hazard could not have
been stated***, so `§3.5` is not wrong to omit it.

**The period stays unset — a gap, not a debt — and its ratifier is nameable in all three parts**
(`adr/0052` as amended 2026-08-15, plus `adr/0098`'s *a ratifier names a machine, a world **and a
quantity***): `adr/0106`'s reference class; a `CommandKind.Connect` city containing a low-volume turning
failure, because 5c task 8 measured that ***the same number sizes both the demand and the supply*** in a
generated one; and **detection latency**, refuted upward against `§3.5`'s own standard that the player
must not be the detector, and downward on the Cap share taken from stress-promotion. ⚠ **None of the
three exists**: the tier is milestone 21, the audit is 23, and the world needs 21's Overlaps.

**Milestone 23 is smaller than its roadmap row implies** — rotation, comparison, record, Cap-ordered
admission, and **no escalation machinery at all**.

## Q3 — Do Stress and Sight read the same quantity? As specified, they do not

[`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)'s
last consequence states the requirement plainly:

> **The Microscopic Cap gains a second consumer of the same signal.** Sight reads live `v/c` at a
> junction; Promotion reads Stress on a Segment. **They must read the *same* quantity** or the city
> will divert around a jam it never promotes, which is `01 §7`'s contradiction again.

**They are not the same quantity as the two documents define them.** `03 §3.3` gives
`stress(segment) = volume / capacity × complexity_factor(junction)`; `adr/0046` gives Sight live
`v/c`. **The complexity factor is in one and not the other**, and it is not decorative — `§3.3` says
it *"lowers the effective threshold for junctions with many conflicting movements"* and `§3.6` makes
it the sole standing mitigation for the blind spot the Audit exists to cover.

**So the divergence runs in the direction `adr/0046` warned about, and it is worse at exactly the
junctions the complexity term was added for.** A complex junction promotes at a volume at which a
driver, reading bare `v/c`, sees no reason to divert at all. `adr/0046` wrote the requirement and
neither document has ever been checked against it — an ADR issuing a write to another document that
did not land, which is [`0012`](0012-corpus-audit.md) **Cause 2**.

⚠ **This is arguable and the question is which quantity moves**, not which document is wrong. Giving
Sight the complexity factor changes a driver's behaviour; taking it off Stress deletes `§3.6`'s
mitigation. A third reading — that the requirement is about *volume* being one exact count rather
than about the whole expression — is available and has to be refused or taken explicitly.

### ✅ CLOSED — [`adr/0109`](../docs/adr/0109-stress-and-sight-share-a-volume-and-not-an-expression-and-the-static-term-belongs-to-habit.md), 2026-08-16

**The third reading is the true one, and it was already paid.** The requirement holds at the level of
**volume** — one per-Segment count, incremented on entry and decremented on exit — and
[`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) made it
one count for an unrelated reason, so there is **no second number able to disagree with the first**.
***The requirement was discharged by a decision taken for another reason***, which is the **third**
time in this session that an open question's answer was already in the corpus under a different
heading — after Q1's enumeration inside `adr/0075` and Q2's consumer inside `§5.1`.

⚠ **Identity above volume is not merely unnecessary — it is *unsatisfiable*, and it fails hardest
exactly where the requirement is supposed to bite.** On a Microscopic Segment travel time is
**emergent** (`adr/0016`, in terms), while Stress goes on reading `v/c` there because that is what
decides demotion. ***The cost of a Segment and the load on a Segment stop being the same kind of thing
at the promotion boundary***, and the promotion boundary is where every jam is. **Stress is a load;
Sight is a cost.** A requirement stated as identity, which becomes unsatisfiable precisely at the
Segments it is about, was stated one level too high.

⚠ **The divergence is real and the brief had the direction right where `adr/0046` had it wrong.** The
factor *lowers the effective threshold*, so it is **≥ 1** and `stress ≥ v/c` always: a complex junction
**promotes at a `v/c` at which a driver sees no reason to divert**. That is *promote without divert*,
and `adr/0046`'s feared *divert without promote* **cannot arise from this term at all**. The direction
is **arithmetic, not a judgement**.

⚠ **And it is not `01 §7`'s contradiction, so that clause is struck rather than softened.** In this
direction the Segment **is** Microscopic, so congestion is exact rather than modelled and the overlay
is honest by construction: the player sees a real jam and drivers driving into it. That is
`adr/0046`'s own stated position four paragraphs above the consequence (*"so is every driver in the
city routing around a jam the instant it forms"*) and 5c task 8's measurement — ***congestion is a cost
paid and never a cost avoided***. ***A wrong reason attached to a right instruction is what gets quoted
later***, which is why the clause goes.

⚠ **Sight is refused the factor on `adr/0046`'s own layer decomposition and on nothing else.** It is
*static geometry, computed once*, so it contributes the same amount at every crossing and is a fact
about the **network** — Habit's layer, not the live one. **The symmetry ceiling is deliberately not
cited**: a static term added to both sides is symmetric and survives session D's argument intact, so
reaching for it would be `adr/0093` run backwards — citing a nearby decision because it points the
same way.

⚠ **The finding outranks the question. `complexity_factor` is not in Habit's cost basis either**, so
**every Habit Route in the design as written is computed as though every junction is a simple
through-road** — `§3.6`'s under-pricing, in the one layer that could route around it, applied
permanently and to every Trip rather than to whatever a driver happens to see. `§3.3` calls the factor
*"a partial mitigation for §3.6"* while it sits in one of the three places that could use it, and the
place it is missing from is the one that **routes**. It belongs there; **how much it buys is measurable
and is routed rather than claimed** — ***a term being in the right place is a decision; how much it
buys is a measurement.***

⚠ **`adr/0046`'s *slow-moving cost basis* is named underspecified.** That ADR never said what is in it
and nothing else does; the only basis that exists is 5c's **free-flow** matrix, which carries no
junction term at all. The enumeration is owed to `adr/0046` rather than to `adr/0109`. **That is the
third unwritten enumeration this session has found** — after `03 §4` invariant 3's and `adr/0062`'s
admission order — which is enough of a pattern to say out loud: ***this corpus writes the members it
needs and never the list.***

**Not turning movements coming off `deferred.md`**: that document defers a per-movement **accumulator**,
a junction **overlay** and a package of lane-level **player tools** on `00`'s anti-goal. A static
geometric factor on an existing routing cost is none of the three, and `§3.3` **already computes it** —
the only question was which consumers read it.

## Q4 — Where do 22 and 23 sit?

`06` marks both **position provisional — session E moves it**. Take the positions, or state what
they are still waiting on and why that is not this session's to give.

### ✅ CLOSED — 2026-08-16, in [`06`](../docs/06-roadmap.md) rather than in an ADR

**Session K's precedent**: a sequencing decision lands in `06` and `PROCESS.md`, not in a decision
record — K re-derived the whole of Phase 2 and wrote no ADR for the ordering. An ADR saying *22 comes
after 21* would be a decision record for a derivation.

**`06` rule 1 decides the shape of the question before any of its content**: *"No dates. Pure
dependency ordering… what **is** stable is what depends on what, so that is all this document commits
to."* **So a position in that table is a dependency claim and nothing else**, and E's job was never to
pick a slot — it was to say what 22 and 23 depend on. ***Read that way the answer is a derivation and
E has no discretion at all.***

**22 is 21 + 1 and 23 is 22 + 1, and neither has any other freedom.** Fidelity is a ladder between two
tiers, so a Segment cannot be promoted before there is something to promote it into; the audit
**promotes**, so it needs 22's machinery and the Cap-ordered admission `adr/0108` ranks it last in.

⚠ **`06` marked three rows provisional and gave them two owners, when the three are one chain with one
degree of freedom.** Marking 22 and 23 independently implies they could move independently, and they
cannot — **two of the three markings were never questions**. ***A shared blocker written as several
independent waits reads as several choices***, which is `0002` §D2's *four rows wait on one mechanism
under four names*, arriving on positions instead of on numbers. **The cluster's position is 21's, and
21's is session G's**, so E's contribution is to reduce three open positions to one.

⚠ **The stated reason for the provisionality is withdrawn, and it is the substantive half of Q4.**
`06` → *What is parked, and why* says the missing feedback loop costs **21 and 22's acceptance
criteria, which cannot be written until the loop closes**. It does not. ***A milestone's acceptance
criteria and the ratification of the numbers it produces are different things***, and that sentence was
holding a position for the second while naming the first — `adr/0043` and `adr/0052`'s split arriving
at a roadmap. **An unratified number is not a reason to hold a position**, or nothing in that table
could be sequenced.

- **21's criterion is `03 §5.1`, in full, with an admission rule, and has been since the first commit.**
  Its three scenarios are **constructed load profiles** — a junction held at capacity, a queue that
  fills its Segment, a volume spike that falls away — and **none reads a route choice**. `§5.1`'s own
  admission rule proves one never could: *"a scenario earns a place when it names a phenomenon the
  statistical tier structurally cannot produce"*, and **diversion is not such a phenomenon**. ***The
  suite is defined over what the tier produces, and route choice is not it.***
- **22's is `03 §4`'s invariants plus the hysteresis scenario**, and its one genuinely missing piece was
  invariant 3's unwritten enumeration — **Q1, the same day**. The brief said so in its own words:
  *"a promotion/demotion mechanism whose lossy direction is unenumerated has no acceptance criterion,
  which is the concrete reason 22 cannot be scoped today."*
- **23's is `adr/0108`** — Q2, the same day.

**So two of the three were made writable by this session and the third had been writable for four
months.** What the missing loop genuinely costs is the **ratification** of `T_high`/`T_low` — `§3.3`
measures them by a sweep over a city whose congestion pattern is wrong without diversion — plus
`adr/0062`'s *does the Cap bind in ordinary play*. Both are numbers, both were always measurable, and
**neither was ever E's**, per task 0's table.

⚠ **Two dependency edges were already stated in `06`'s own milestone rows and the graph carried
neither** — **7 → the cluster's numbers** (*"every congestion figure taken before it is taken on a
journey missing both ends"*) and **12 → the cluster's numbers** (*"every Commute Budget rung and
congestion figure in the corpus is calibrated against"* one Trip generator; S2 R4 priced the draw at
18.52% against **128.82%**, *"a different city"*). ***A dependency stated in a row's prose is not a
dependency the graph knows about***, and that graph is what `06` calls *the sequence's warrant*. **All
four new edges are already satisfied by the existing order**, so nothing moves — which is the point:
they convert a position that happened to be right into one that is derived.

⚠ **And *"20 through 23"* in that section is wrong by one.** **20** is Life Stages and self-generation,
gated on `adr/0011` and repaired by **18**, with no provisional marking on its own row. The cluster is
**21 through 23**, which is what `CLAUDE.md` says — so the two documents disagreed and the one with the
error is `06`, which owns the sequence. ***A range written as arithmetic includes whatever the
arithmetic reaches***, and nobody checked the row it reached because the number looked adjacent.

**Merging 23 into 22 was considered and refused** on `06` rule 4: they retire **different** risks, and
under the corollary — *once the risk is retired, the milestone is done, whatever else remains undone in
it* — a merged row would retire the camera risk and be declared done with `§3.6`'s blind spot
uncovered. ***Rule 4's stopping rule is what makes a merged milestone dangerous rather than merely
untidy.***

---

## What this session does not do

- **It does not touch `adr/0005`'s decision half.** *Decisions are never shared* is untouched by
  `0007`, is the part that ADR's own last line calls *"worth defending"*, and nothing is blocked on
  it. Grilling it here would be running an argument session because it is available, which is the
  board's standing rule read backwards. **It stays 🔴 in `0002` §F2 and keeps session E's name.**
- **It sets no measurable parameter**, per the table above.
- **It does not design a fallback tier below Microscopic.** `adr/0070`.

## Two corrections, which are not decisions

1. **`adr/0005` states *"a Citizen record is roughly 40 bytes"*** against S0a's measured **85.98 MiB
   at 1M**, ≈90 B/Citizen across the tables — **2.25× out**. `03 §2.1` already admits the figure is
   stale and the ADR still states it, so the corpus fixed the derived copy and left the source: the
   polarity `adr/0007`'s own invariant-6 amendment had, and ***the document a reader reaches for
   first is the one still wrong.*** ⚠ The neighbouring *"about 1% of a core"* is **measurable and
   unmeasured** (`0002` §B) and is corrected by nobody here — strike the byte figure, not the sentence.
2. **`03 §3.4` describes self-correction as *route choice* responding to travel time**, which
   `adr/0046` changed: the loop *"was global and slow… it is now **local and fast**"*, carried by
   Sight at a junction rather than by re-routing. The section reads as though nothing moved.

⚠ **Neither is a licence to restate `adr/0046` as refusing `03 §3.4`.** It does the opposite — it
**keeps** the loop and rejects free-flow routing **by name**, as *"cheap and hollow"*, precisely
because *"that loop only closes if routing reads the VDF."* What is true of the **build** is a
different sentence: 5c routes on free flow because Habit, Sight and Temperament are **unbuilt**, so
the shipped city implements the option `adr/0046` rejected. That is `adr/0070`'s *unbuilt* class and
the answer is build it, not amend anything here.

## What would make this session wrong

If Q1's enumeration turns out to be derivable from `adr/0075`'s Leg and Traveller rather than
chosen — a Traveller is a cursor, so what a demotion discards may be fully determined by what the
cursor does not carry — then it is not a decision at all and this session should say so and close it
as **void as posed**. That is the outcome to watch for, because it is the one that looks like work.
