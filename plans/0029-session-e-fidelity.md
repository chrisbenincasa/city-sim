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

## Q4 — Where do 22 and 23 sit?

`06` marks both **position provisional — session E moves it**. Take the positions, or state what
they are still waiting on and why that is not this session's to give.

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
