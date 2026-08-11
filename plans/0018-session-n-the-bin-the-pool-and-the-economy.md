# 0018 — Session N: the Bin, the Pool and the economy (`02 §4`–`§5`, `04`)

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). The designs being argued are
> [`02-simulation-model.md`](../docs/02-simulation-model.md) §4–§5 and
> [`04-economy-and-goods.md`](../docs/04-economy-and-goods.md) entire. Status and cross-track order are
> [`0000`](0000-board.md); every open question is [`0002`](0002-open-questions.md).
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

---

## Status

**Briefed 2026-08-10. Task 0 has *partly* run — this document is its record. Task 1 is DECIDED, by
[`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md),
and SHIPPED the same day** — [`0003`](0003-build-plan.md)'s hash-moving queue items 0 and 2, together, as
one commit.
Its record is [below](#task-1s-record--the-predicate-was-wrong-on-both-sides).

**Task 3 is DECIDED, by
[`adr/0064`](../docs/adr/0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md),
and is NOT shipped** — it is [`0003`](0003-build-plan.md)'s hash-moving queue **item 3**, behind slice 10
task 11, because nothing it fixes is live until the game ships patches. Its record is
[below](#task-3s-record--the-question-named-two-positions-and-the-answer-was-a-third).

**Task 4 is DECIDED, by
[`adr/0065`](../docs/adr/0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md),
and is NOT shipped** — it merges into queue **item 3** with `adr/0064`, because the two change the same
two columns and a baseline that moved for both would be attributable to neither. Its record is
[below](#task-4s-record--it-was-filed-as-a-semantic-question-and-the-answer-was-a-width).

**Task 2 is DECIDED, by
[`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)
and [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md),
and SHIPPED 2026-08-11** as [`0003`](0003-build-plan.md)'s hash-moving queue items **4** and **5**, in
two commits — the queue had been empty since 2026-08-10 and this task reopened it. **The second ADR is the one the task found underneath itself**, and it is the answer:
the occupancy question was never an occupancy question. Its record is
[below](#task-2s-record--the-question-was-filed-twice-and-both-filings-had-the-wrong-subject).

**What shipping task 2 found is [below](#what-shipping-task-2-found--the-adr-was-wrong-about-the-outcome-and-about-the-numbers),
and it is the session's second methodological finding.** Both ADRs made a prediction that the build
refuted: `adr/0068` predicted a derived column that turned out not to be needed, and `adr/0069` predicted
an equilibrium that does not close and a number count of zero that turned out to be three. **That is
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) running forwards** —
the rule was written for absences generating design *positions*, and an absence generating a *prediction*
has the same base rate.

**The sitting continues at task 6** (`04 §8`'s three genuinely open questions).

**It is booked to run beside session D and shares no document with it.** D's brief claims the whole of
`03` and `adr/0005`, `0007`, `0008`, `0009`, `0012`, `0016`, `0046`, `0047`; N claims `02 §4`–`§5`, `04`
and `adr/0013`, `0024`, `0026`, `0031`, `0033`, `0050`. **The intersection is empty at the document
level**, which is the property that makes two sittings safe rather than careful sequencing.

## Why this one, and why now

**The board's rule is *an argument session runs when something concrete is blocked on it, never because
it is available*.** This is the only place outside session D where a **measurement is waiting on an
argument** — which is the ground on which D was promoted. Four defects, all found by running code, none
of them tunable, and every one of them filed rather than fixed because the design has no answer:

| | The defect | Owner |
|---|---|---|
| 1 | **The shortage regime is not expressible.** A recorded shortfall is the deficit at the *instant of failure*; a Bin drains from the head only while the **single arriving quantity** covers it. A consumer short of three is never woken by three arrivals of one, and both parties sleep for ever with the Bin full | [`0011`](0011-rule-engine-bins-and-rules.md) finding 41 |
| 2 | **The city settles five-sixths homeless.** ~60 of 121 Lots built on, **~300 of 360 Households homeless**, nothing trending, every table recycling. Demolition evicts a Building's whole occupancy and creation rehouses **one**, so every cycle nets **+2** into the Pool. **A Building has no declared occupancy at all** | [`0014`](0014-zone-rules-and-the-sweep-family.md) task 10 |
| 3 | **The first Ruleset deadlocked in about two hundred Ticks** — flour to 60, bread to 20, no sink: every Bin full, every Rule failed on headroom, every Rule subscribed, nothing left that could drain a Bin to wake one. Total, correct, honest deadlock, demonstrated rather than argued | [`0011`](0011-rule-engine-bins-and-rules.md) finding 40 |
| 4 | **No reload of any kind moves a live Bin's capacity.** A retuned capacity reaches the next Building raised and never the ones standing — `adr/0015`'s acceptance test failing on a **second** number | [`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md) finding 4 |

**They are one question wearing four costumes**, and that is the case for a session rather than four
tickets: each is a place where **the Ruleset cannot express a quantity the mechanism needs**, and three
of the four converge on the same named hole.

**`pool` is that hole.** A production chain between Buildings crosses an ownership boundary, which is
the District Pool, which is *a named hole that throws* — so the only shape the simulation can express
today is a Building producing into and drawing from its own Bins. Defect 1's trigger is `pool` by name
(`0002` §C); slice 7's **task 10b — the proving chain — is re-filed against `pool`** and therefore
against Phase 2; and `04 §1`'s five Goods and three sinks describe a city none of which can be built.
**So the economy is not behind code. It is behind a decision.**

**A second, independent line arrives at the same place.** `06`'s *Mechanisms with no milestone* table
lists **seventeen** settled mechanisms that appear in no milestone anywhere, and **five of them are
this cluster** — conserved Money and the treasury (`adr/0024`, `0031`), Office, wages, the labour market
and Skill Tiers (`adr/0026`), the nine-Resource abstraction (`adr/0031`), infrastructure pricing and
Upkeep (`adr/0035`), and **Policy as a Sweep Rule, and the Sweep Rule family entire** (`adr/0033`). Two
more are adjacent: density as a cap (`adr/0025`) and arrival through the gate (`adr/0023`), the gate
being money's only source and sink. `06` says of Phase 2 as written that **"it has no money in it,
nobody employed, and no way for anyone to arrive"**, and that placing these is the re-derivation's job.
**So N is the largest single input to K2**, which the board schedules last and which cannot be run on a
cluster nobody has argued.

**What N is not booked to do is price anything.** Unlike D it stands over no row of
[`0013`](0013-tick-budget.md), and it must not pretend otherwise: the Rule engine's row is measured
(552 ns a due Rule, 2.8× its synthetic figure) and the multiplicand it still owes is **10b's**, which
is `pool`, which is N's output rather than N's argument.

## Gate

**None.** Every measurement N cites has been taken. Slice 8 has closed, so nothing is editing
`RulesetLoader` or the golden baselines.

## What it must read first, and the caveats that travel with each

**Designs under argument:** `02 §3` (Resources and Bins), `02 §4` (the Rule families, and `§4.1`'s
worked example), `02 §5` — `§5.2` placement and the Unplaced Pool, `§5.4` the choice model, `§5.5` Lot
competition and the bid, `§5.6`, `§5.7` the sweep's cost, `§5.9` abandonment — and **the whole of
`04`**, whose §6 chain is the thing the economy exists to produce and has never been grilled.

**Decisions in the cluster:** [`adr/0013`](../docs/adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md),
[`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md),
[`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md),
[`adr/0031`](../docs/adr/0031-one-resource-abstraction-and-depth-not-count.md),
[`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md),
[`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md),
plus `adr/0017` (satisficing), `adr/0022`, `adr/0035`, `adr/0045`, `adr/0049`, `adr/0054`.

**Measurements:** [`0011`](0011-rule-engine-bins-and-rules.md) findings 40–43,
[`0014`](0014-zone-rules-and-the-sweep-family.md) task 10's table,
[`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md) findings 3–4, and
`rulesets/minimal.toml`, **which says in its own header that it models no city**.

> ### Three caveats, and they are not decoration
>
> **1. The shipped Ruleset runs in surplus, and that is the shape of the evidence rather than a
> preference.** The producer's quantum must be at least as large as any consumer's deficit, so the Rule
> that fails must be the **producer, on headroom**. Every steady state the project has ever observed is
> therefore a *surplus* steady state. **Nothing in the corpus has ever observed a shortage**, which is
> the regime `04 §6` is entirely about.
>
> **2. Every occupancy figure is an artefact of a mismatch, not of a mechanism.** `SyntheticCity` puts
> **3** Households in a Building; `ZoneRuleEngine.Create` places **1** — `adr/0054`'s *drained blind*,
> because there is no rent, commute or tolerance to place more on. **No tuning done before occupancy
> exists is worth keeping**, and the five-sixths equilibrium will move when it does.
>
> **3. ⚠ `02 §4.3`'s worked example destroyed money for six slices**, because the transcription into the
> loader's own fixture dropped the money line — a green suite agreeing with the code instead of with the
> document, and the **first of four** such. **Quote nothing from `02 §4`'s worked example without
> checking it against `rulesets/minimal.toml`.**

---

## Tasks

### 0. The typing pass — and it runs **before** anything is argued — **PARTLY RUN 2026-08-10**

> **This document is its record**, [below](#task-0s-record--the-cluster-typed). Read so far: **`04`
> entire**, `02 §3`–`§5` claim by claim, and `adr/0013`, `0017`, `0022`, `0024`, `0026`, `0031`, `0033`,
> `0035`, `0045`, `0049`, `0050`. **Not yet read: `adr/0023`, `0025`, `0027`, `0032`, `0054`, and `02
> §6`** — the last of which the pass should take because [`0000`](0000-board.md)'s *Owed* list already
> owes it a section-number correction, and N is the sitting that will be in the file.

**Type every claim in the cluster *arguable* or *measurable*** per
[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
The test is *can you name the number that would refute this, and the machine that would produce it?*

**Rule, and it binds this session against itself: if a claim types measurable, N must not close it.**

**Why first rather than after.** The same reason D's brief gives, plus a sharper local one: this cluster
sits almost entirely inside [`0002`](0002-open-questions.md) §F's **blanket row** — `adr/0010`–`0022` is
*one* 🟢 row covering thirteen ADRs, which `adr/0043` calls a defect in its own right — so a 🟢 mark here
is not evidence any sentence in `adr/0013`, `0017` or `0022` was ever examined.

### 1. What a wait list wakes on — the arrival, or the state — **DECIDED AND SHIPPED 2026-08-10, `adr/0063`**

> **The task text below is kept as written, because the record answers it and because the framing it
> states is part of what the sitting found wrong.** Its two candidates are **not symmetric**: option A is
> a complete fix for one bug and option B is an incomplete fix for the other. See
> [the record](#task-1s-record--the-predicate-was-wrong-on-both-sides).

**Defect 1, and it is a fairness question, which is what makes it arguable rather than measurable.** Two
candidate answers, and they differ in **what they promise the second waiter in the queue**:

- **Decrement the recorded shortfall as arrivals accumulate.** Preserves the queue's promise to its
  head; a trickle eventually wakes it.
- **Re-derive the shortfall from the Bin.** Cheaper to reason about, and **it fixes both faces at
  once** — because slice 8 found a second one: a tuning reload changes no row, so a Rule asleep on a
  Bin keeps a shortfall recorded against the **old** Ruleset's quantities. *Halve a Rule's input
  requirement and the bakery starving for the old amount is the one Building the edit never reaches.*
  **Decrement fixes neither face.**

That asymmetry is the argument, and it was not available when the question was filed. `HONEST
DEGRADATION` is the axis: whichever answer wins, *what a waiter is promised* has to be statable in one
sentence a player could be shown.

**Do not settle it without walking it through the phase table.** `0002` records that the round-robin
wait list was written, recorded in an ADR, and **did nothing** — Phase 3's sorted-key settle order
picked the winner regardless, so the head of the queue lost every time, and it was caught only by
tracing two named bakeries through all eight phases across three Ticks. *The mechanism that fixed the
decorative queue is the cause of this hole.*

### 2. What a Building holds — and the typing conflict comes first — **DECIDED 2026-08-10, `adr/0068`, and `adr/0069` underneath it**

> **The task text below is kept as written, because the record answers it and because the framing it
> states is most of what the sitting found wrong.** Its three candidate shapes are not the space, its
> disqualification of `adr/0025` is false against that ADR's own heading, and the typing conflict it
> opens with is not a conflict about the type. See
> [the record](#task-2s-record--the-question-was-filed-twice-and-both-filings-had-the-wrong-subject).

**Defect 2. Before arguing it, resolve how it is filed**, because
[`0002`](0002-open-questions.md) types it **twice and differently**: §B calls it *"a measurement rather
than a worry"* with the machine named (`ZoneRuleLongRunTests`), and §C calls it **"arguable now and
measurable later"**. `plans/0014` files it to §B in its status line and to `0002` unqualified in task 6.
**A claim filed in two sections with two types is the shape `adr/0043` exists to prevent**, and it is
the first thing this task must fix.

The split the pass proposes: **the *number* is measurable and N must not choose it; the *shape* is
arguable and N may.** Three shapes, and the third is a position rather than an absence:

1. a per-kind Ruleset figure;
2. a figure derived from the density band — noting `adr/0025` is **adjacent and does not cover it**:
   density caps *what may be built*, this is *how many families fit in what was built*;
3. **no capacity at all** — coherent, and it makes **overcrowding rather than homelessness** the
   failure mode a shortage produces.

**And the Pool has a designed sink that nobody has built.** `02 §5.2` already says a Household in the
Unplaced Pool **departs the city permanently after a bounded number of failed cycles**, counted as an
unhoused Departure — **and the bound is not named anywhere.** That is why the 100,000-Tick run's Pool
high-water mark plateaus at a *structural* ceiling (the population, since a Household is in the Pool at
most once) rather than at a designed one. **So five-sixths homeless is not only an occupancy defect; it
is a missing sink**, and it is `adr/0006`'s shape wearing a bound instead of a collection. Milestone
**9a** is *Households, the Unplaced Pool and Departure*, and `06` already notes 9a **"has the Pool and
Departure but nothing says where Households come from"**.

### 3. Is a Bin's capacity a property of the Bin as built, or of the Ruleset in force? — **DECIDED 2026-08-10, `adr/0064`**

**Defect 4, and the two consistent answers are both or neither** — writing capacity in the migration's
refit alone would give one edit two behaviours depending on whether some *unrelated* declaration also
moved, which is worse than the gap. The second-order question is the one to argue rather than the first:
**what happens to a Bin holding more than its new ceiling**, where clamping **destroys Goods** — against
`04 §2`'s *if a hundred units entered the District, a hundred must be accounted for* — and leaving it
over-full breaks `Invariant.BinLevelIsWithinCapacity` (id 14).

### 4. What "full" means for a Bin with no capacity — **DECIDED 2026-08-10, `adr/0065`**

**`adr/0031` flagged the unbounded-Bin comparison as a determinism hazard and never resolved it.** Slice
7 settled the **storage** half — a money Bin is unbounded and the loader refuses an authored ceiling —
and left the **comparison** half open. The code is already on the right side of the arithmetic
(`HeadroomAt` is `Capacity − level`, never `level + delta > capacity`, which overflows against a
sentinel and silently inverts), so what is owed is the *semantic*: whether *full* is a state an
unbounded Bin can be in at all, and what a Rule that fails on headroom against `int.MaxValue` means.
Note that defect 3's deadlock had **both** failure modes in front of it: bounded deadlocked, and
**unbounded ran for ever and grew a magnitude without bound**, failing the half of acceptance the first
one passed.

### 5. `04 §6` — how a shortage becomes an unhappy person — **DECIDED 2026-08-10, `adr/0067`, and `adr/0066` underneath it**

> **Three things settled so far, and one ADR that the task found underneath itself.**
>
> **Step 4 is a Trip.** A Household travels to a shop on its Provider List; `adr/0032` is the precedent
> — *a Service reaches people by someone making a journey* — and Goods have the stronger claim, since a
> grocery's Bin is a physical stock in a place. The consequence is uncomfortable and is accepted: every
> Household's shopping is **Trip generation**, so `04 §6` is a load on milestone 5b rather than an
> economy question with a cost of its own.
>
> **Step 5 costs one Trip per shopping occasion, not `N`.** A Household that finds nothing goes home;
> the Need degrades, and it tries a different entry at the *next* occasion. `04 §6` as written reads
> sequential, which would make the shortage path amplify itself by the list length at exactly the moment
> the city is failing — shortage → shopping Trips → congestion → Trip failures → Failure Pressure →
> abandonment. **The lag this introduces is accepted as realism**: a Household with three known
> groceries takes three occasions to discover the District is dry.
>
> **Trip Fate has four outcomes and none of them is *arrived and the shelf was empty*.** The
> enumeration is about the **journey**; this is about the **transaction**, and the journey succeeded.
> S0a's own Household field list already carries *"failed-attempt counter, refusal reason"*, so the
> footprint model has been assuming this for longer than the design has said it.
>
> **And the task found [`adr/0066`](../docs/adr/0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md)
> underneath itself**: the Provider List is the only per-entity collection in the design ever modelled
> as an inline fixed array, against `05 §4`'s rule that every variable-length collection is an intrusive
> index list. It suspends two of S0a's conclusions and turns the cap into a behavioural bound rather
> than a memory parameter.
>
> **What a Household remembers is a cursor, not a timestamp.** Advance on failure, reset on success, so
> a provider that failed is skipped for exactly **one occasion**. The per-provider *last failed at*
> timestamp was argued for and **is not refused on `adr/0053`** — it is a duration, not a tally, and
> that ADR endorses the shape; the sitting claimed otherwise first and was wrong. It is refused because
> its decay window must be **picked**, where the cursor's falls out of the mechanism: the natural
> derivation is *as long as the shop takes to restock*, and `BOUNDED KNOWLEDGE` is the rule that forbids
> a Household knowing it. **Fourth time the cheapest way to satisfy `adr/0052` was to find the
> derivation.**
>
> **Owed**: `04 §6`'s correction to steps 4 and 5, filed to [`0012`](0012-corpus-audit.md). **Routed**:
> shopping occasions per Household per Day, to `0002` §B at milestone 5b — the affordability half that
> could still force a choice between the Evidence chain and the Tick budget.

**Never grilled, and it is the chain the whole economy exists to produce.** Seven steps, each of which
must be individually inspectable through Evidence. `LEGIBLE CAUSE`

The step to argue is **4**: *a Household must actually attempt the purchase and actually fail, because
that failure is the evidence.* Everything about the design's cost depends on that being a real
per-Household act against a short, sticky **Provider List** — `BOUNDED KNOWLEDGE` — and nothing in the
corpus has priced it or specified the list's maintenance. **The pass could not type step 4 at all**,
which is recorded below as the honest result rather than smoothed over.

### 6. `04 §8`'s three genuinely open questions

Three remain of eight; the other five are struck, and **one of them sat here as open after the ADR that
closed it**, which is the granularity defect `0000` names.

- **Is there a labour market price?** (§8.4) Wages responding to scarcity closes the loop between job
  shortages and household income and **adds a second tâtonnement running against the goods one** —
  `adr/0024` names exactly this as *"the largest known risk this ADR creates"* and calls it *"the
  predicted failure"*. Arguable, and `adr/0024`'s revisit section already names the honest options.
- **How does construction consume Materials?** (§8.5) Over Days, or one transaction. Raised in priority
  by `adr/0022`, and it decides how sharply growth stalls and how visible a shortage is.
- **Does industrial pollution use the same Map Layer machinery as everything else?** (§8.6) *"Assumed
  yes"*, and the interaction between a production rate and an emission rate is unspecified. **Slice 6
  and `adr/0051` have since built the machinery**, so this may be closable by excavation rather than by
  decision — which is what `adr/0051` itself turned out to be, `02 §2.4` having said pollution *decays*
  since the day it was written.

### 7. The numbers, under `adr/0052`

**A hash-bearing or world-creation number is chosen with a *named* ratifier written down beside it on
the day it is chosen, or it is not chosen. A category is not a name.** The cluster's:

| Number | Note |
|---|---|
| **μ**, the choice model's sharpness | Already in §D2, unset. Constrained: Q16.16 underflows `exp` below −11.09. `§5.4` argues `μ ≈ 1` *with utilities scaled so meaningful differences are 1–3 units* — **which is a statement about the scaling, not about μ** |
| **`adr/0026`'s forty** | *"a forty-worker submarket damped is still a forty-worker submarket"*. **Appears twice, derived nowhere, and the entire shrinkage mechanism is justified on it.** In no §D row at all |
| **The shrinkage weight** | `adr/0026`'s revisit section names it as the first lever against oscillation. **A lever with no value.** In no §D row |
| **The ±10% housing clamp** | Imported into `adr/0026` from `02 §5.6` and restated as settled |
| **`02 §5.2`'s give-up bound** | Cycles-to-Departure. Unnamed, hash-bearing, and task 2's missing sink |
| **`02 §5.7`'s build-rate throttle** | Projects started per cycle. Unnamed — **and see finding 2 below, because the unit is wrong as well as the value** |

**The cheapest way to satisfy `adr/0052` is to find the derivation.** Both numbers that have left §D2 so
far needed no choice at all, and `adr/0059` **deleted** a row rather than filling it.

---

## Task 1's record — the predicate was wrong on both sides

**Argued 2026-08-10. Settled by [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md).**
Four things came out of the sitting that the framing on this brief had hidden.

**1. It is two bugs, not one question with two faces.** The predicate is
`Shortfall[head] > remaining`, and each side carried an independent defect: the **budget** was one write's
delta (`remaining = arriving`), so a requirement coarser than the granularity of supply is never reached;
and the **requirement** was computed under the Ruleset in force when the waiter failed and never
re-derived. `0002` §C held them as one entry, and that framing concealed the consequence — **option B as
briefed fixes nothing about the trickle**, since `3 > 1` holds however the 3 was obtained. Option A was
also dominated: it fixes the budget bug by writing a saved, hashed column on every deposit for every
waiter, where moving the budget to `level` fixes it with no writes at all. **Neither candidate on this
brief was the answer**, and the grid that shows why has two axes rather than a list.

**2. The end state was already illegal, and the missing instrument is the same omission.** A consumer
asleep beside a Bin holding what it needs is `adr/0033`'s *no Rule is asleep with all inputs satisfiable*
— one of two mitigations it calls **both required** — restated in `02 §10` and `plans/0008`, and
implemented nowhere. So the defect and finding 1 of this brief are one thing seen from two ends, which is
how it survived two 100,000-Tick acceptance runs.

**3. The corpus's justification for the old budget was a document mis-citing itself.** `02 §4.1`
justified draining by shortfall on *"§1.1's **sorted-key** settle order"* picking a permanent winner.
`02 §1.1` says contention is resolved *"by a counter-based random shuffle"* and warns in its own voice
that a sorted key applied to chronic shortage *"produces permanent starvation rather than a gradient"*.
The code agrees with §1.1 — the key is a draw over `(seed, instance, **tick**, purpose)`. **One error,
three homes** — `02 §4.1`, `CONTEXT.md` → Bin, and `World.Drain`'s doc-comment — **and the original is a
mis-citation of a section of the same document that says the reverse.** All three corrected. This is
*Cause 2* with a new twist: not a correction that failed to propagate, but an error that propagated from
a document's misreading of itself.

**4. Partial acquisition is demoted rather than rejected, and the argument that demotes it is
arithmetic.** Three consumers each needing 6, a Pool supplying 12: dividing each arrival gives 4, 4, 4 —
**zero firings and twelve units immobilised** — where serving the head completely and rotating gives
**two firings and immobilises nothing.** So `02 §4.1`'s *degrades evenly* survives, and survives
*because* servings are complete and turns rotate: it is evenness **over time**, never within an arrival,
and the two readings are not equivalent. Accumulation stays authorable as content — an acquisition Rule
at `min = 1` feeding the consumer's own Bin, which is `CONTEXT` → Building's own sentence — with
**`04 §8.5` (construction drawing Materials) as its first real case**, which is task 6 of this session.

**What the decision costs, and it is not settled here.** Deriving the requirement at the write site is a
partial `Check` per waiter examined — commonly one per Bin write — against 82.84 ns synthetic and 552 ns
in a real world. The claim *it costs less than the Bin write it accompanies* is **measurable**, the
machine exists, and `adr/0043` bars this session from closing it. → `0002` §B.

**5. Building `adr/0033`'s invariant found the defect live in the committed golden baseline, which
reversed the sitting's own conclusion twice.** The reasoning had been: a violation needs a Bin written in
instalments smaller than a waiter's requirement; `local` cannot produce one; `rulesets/minimal.toml` is
safe because `restock`'s deficit is **1**, the smallest quantity expressible. All of that is true. **The
golden session reloads into `rulesets/minimal-tuned.toml` at Tick 128, and the one number that file
changes is `restock`'s output amount, 1 → 2.** A producer with a headroom deficit of 2, drawn down one
unit at a time by the **occupancy-1** Buildings a Zone Rule creates, is never woken. At Tick 256 the
committed trace holds a `restock` asleep on headroom **3** against a recorded shortfall of **2**.

Three things follow. **The headroom twin is not hypothetical** — it is the half the shipped content
actually runs on, and it has been broken since slice 8. **`minimal.toml`'s header states the condition
that keeps it honest and not the mirror**: *producing in a quantum at least as large as any consumer's
deficit* has a counterpart, *consuming in a quantum at least as large as any producer's headroom deficit*,
which the tuned file breaks and nobody wrote down. And **the sequencing changed on evidence rather than on
argument**: the invariant cannot be committed green without the predicate fix, so
[`0003`](0003-build-plan.md)'s queue is corrected to ship items 0 and 2 together. Retuning
`minimal-tuned.toml` to dodge the violation was considered and refused by name — four instances of a
green suite agreeing with the code instead of the claim are already on the record, and that would be the
fifth.

**Documents moved:** `adr/0063` written; `CONTEXT.md` → Bin and → Rule Instance corrected; `02 §1.1` and
`02 §4.1` amended in place with the superseded wording recorded rather than deleted;
`World.Drain`'s doc-comment bannered, ~~because **the code still implements the old predicate**~~ —
**struck: it does not, as of the same day.**

### What shipping it found

**Implemented 2026-08-10**, items 0 and 2 of [`0003`](0003-build-plan.md)'s queue in one commit.
`World.Drain` takes its budget from `LevelAt`/`HeadroomAt` and its requirement from
`RuleEngine.Requirement`, which `Invariant.WaiterIsBlockedByTheBinItNames` shares rather than
reimplements; `RuleInstance.shortfall`, `RuleVerdict.Shortfall` and `Subscribe`'s quantity parameter are
all gone. **825 tests green**, and the two acceptance fixtures in `BinWaitListTests` were **inverted
rather than deleted**, as the file's own remarks instructed.

**6. The prediction about baselines was wrong, and the way it was wrong is a coverage finding.** The ADR
said *all three golden baselines re-record*; **only `session-trace.txt` moved.** `world-hash.txt` is built
by `GoldenFixtures.Build()`, which raises Buildings through `Buildings.Create` rather than
`World.CreateBuilding` — so it holds **no Rule Instance rows**, and the deleted column was under no
committed hash in it. **The `rule_instance` table's saved columns are covered by the session trace alone**,
which is exactly the coverage `world-hash.txt` exists to supply for tables a session cannot reach. Sitting
beside `0003`'s standing note that **lint 6 does not exist**, that means a save-format change had one
witness rather than three. → [`0003`](0003-build-plan.md).

**7. The fixture change is larger than the engine change, and that is the shape of the decision rather
than churn.** `BinTests` used to fabricate a subscription with an arbitrary quantity —
`Subscribe(waiter, flour, Blocking.Level, shortfall: 6)` — which the new model cannot express, because a
requirement is a property of the waiter's **Rule**. Its Ruleset now declares a Rule per requirement the
suite asks for and a sleeper picks one. **A test that could no longer be written is the honest signal
that a number stopped being state**, and it is worth more than the assertion it replaced.

**8. `rulesets/minimal.toml` and `minimal-tuned.toml` still describe the defect as an engine property, and
correcting them is a separate commit.** Both headers carry *"THE SHORTAGE REGIME IS NOT AVAILABLE AND THAT
IS AN ENGINE PROPERTY RATHER THAN A TASTE"*, citing finding 41, which is now false. **A Ruleset comment is
hashed content** (`RulesetFile.HashOfContent` normalises line endings *and nothing else*), so editing
either header moves both content hashes, `session.borough`'s recorded reload line, two constants in
`GoldenFixtures`, and every sample in the trace. Doing that in this commit would put two unrelated causes
behind one hash move, which is `0003`'s stated reason for keeping re-records separable. **→
[`0012`](0012-corpus-audit.md).**

---

## Task 3's record — the question named two positions and the answer was a third

**Argued 2026-08-10. Settled by
[`adr/0064`](../docs/adr/0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md).**
Capacity is `derived AND rebuilt` from the Ruleset in force, keyed on `(kind, Resource)`; a Bin over its
new ceiling is left to drain. Four things came out of the sitting that this brief's framing had hidden.

**1. Both named positions assumed capacity is *state*, and it is a function.** *As built* and *in force*
were held as the whole space. The third position — no saved column at all — was not in the brief, in
`0002`, or in `plans/0015`, and it is the one that makes the brief's own *both or neither* rule come out
right: **neither** path writes a saved capacity, because there is no saved capacity to write. It is the
move [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)
made on `shortfall` four hours earlier, and the session could name **no property distinguishing the two
columns**: neither is pointed at by live state, both are authored numbers, both are read on the hot path.

**2. The second-order question's second horn does not exist, and it was checked rather than argued.**
The brief said leaving a Bin over its ceiling *"breaks `Invariant.BinLevelIsWithinCapacity` (id 14)"*.
Id 14 lives at exactly two **write-site guards**, in `Deposit` and `Withdraw`; there is no standing
whole-world check. An over-full Bin has negative headroom, refuses every deposit at the engine's own
affordability test, accepts every withdrawal, and **drains back under its ceiling unaided**. So the
choice was never *destroy Goods or violate an invariant* — the winning option was already implemented
and nobody had noticed.

**3. The argument that decided it is not about hot reload.** Filed as a reload defect, and the objection
that nearly reversed it is that a reload is design-time and rare. What answers that is the **patch**: a
shipped Ruleset change under a live city, which is what `05 §7`'s provenance trail exists for. Under *as
built*, a patch that retunes a capacity splits the city permanently into Buildings raised before and
after it — identical in kind, different in their ceilings, with the cause recoverable from **no state the
world holds**. A reload is how a designer would notice; a patch is where it bites.

**4. The verification found the lint doing work nobody aimed it at.** The decision rests on `Deposit`'s
assertion being unreachable, which was checked: `RuleEngine.Fire` is the only production caller, it
applies **net** deltas, net-zero deltas are skipped on both sides, and `Check` refuses on negative
headroom via `IntegerMath.FloorDiv`. That last one is load-bearing in a way that is easy to lose —
`FloorDiv` rounds toward negative infinity, so a **derived floor of 0** against a headroom of −1 gives
−1 and is refused, where C#'s truncating `/` would have given **0**, passed the guard and reached the
assertion. **`BOR0203`, the raw-division lint, is what makes this safe.**

**What it left behind.** Two obligations ride with the implementation as `0003`'s queue item 3 — a loader
refusal making `(kind, Resource)` a key, and an end-of-run check that the rebuild was actually run. The
first **closes a defect that is live today and independent of this decision**: `World.FindBin` returns
the first Bin matching a Resource and `Fit` creates only Bins it cannot find, so a kind declaring one
Resource twice makes a **refit build one Bin where construction built two**. And task 4 is left cheaper
rather than closed: the row still holds an `int`, so *unbounded* still arrives as `int.MaxValue`, but an
explicit marker can now be a second **derived** column at no cost in the save or the hash.

---

## Task 4's record — it was filed as a semantic question and the answer was a width

**Argued 2026-08-10. Settled by
[`adr/0065`](../docs/adr/0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md).**
A Bin's `level` and `capacity` are `long` along the whole write path; *unbounded* is `long.MaxValue`;
*full* stays uniform with no special case; an approach to a ceiling is `adr/0006`'s magnitude clause.

**1. The corpus had already chosen, and one half of it did not know.** `Money` is
`readonly record struct Money(long Raw)` and `HouseholdTable` holds `Column<Money>`; `BinTable` held
`Column<int>`. Since `adr/0031` puts money **in a Bin** and `adr/0050` makes every cross-boundary trade
a payment, **a purchase narrowed 64 bits to 32**. The sitting spent its first exchanges arguing whether
to widen, and the argument was already over — this is `adr/0062`'s shape (*a wrong unit in three places
at once*) with the correct value present in the same assembly.

**2. The semantic question could not be answered at the old width, which is why it had sat open.**
*Is `full` a state an unbounded Bin can be in* depends entirely on whether the ceiling is reachable, and
at `int.MaxValue` it was: a city-aggregating money Bin overflows at **5,965 units per Household** across
360,000 Households, before `adr/0024`'s balance of payments accumulates anything. At `long.MaxValue` the
same figure is ~2.5×10¹³ and the question dissolves. **The session's own first answer — skip the
headroom branch for unbounded Bins — was withdrawn as a workaround for the width**, and it carried its
own objection: removing the check moved the failure from *a rich seller sleeps* to *a rich seller
crashes*.

**3. Truly unbounded is architecturally unavailable rather than merely hard.** `BigInteger` is managed
and allocates; lint 7 and `adr/0036` require `unmanaged`. So the honest sentence is *there is no
unbounded, only a ceiling whose approach is a defect* — and the permanent escape hatch is
**denomination**, since money's unit is a Ruleset choice. No future argument needs a 128-bit quantity.

**4. Checking the arithmetic found one live latent defect and cleared two suspects.**
`RuleEngine.Requirement`'s `floor × |net|` is an **unbounded `int` product** — on overflow the
requirement goes negative, the drain wakes a waiter it cannot satisfy and then *increases* its budget by
subtracting a negative. It is `adr/0063`'s own arithmetic, four hours old, and is unreachable only
because the single declared Readout is `occupancy`. `Check`'s `delta × applications` is safe **by
construction** and is recorded as such so nobody widens it as a precaution. And `Band`'s
`readout × Percent` is *the door money walks through* — `Readouts.Read` returns `int` while `02 §4.1`'s
spelling of a derived band is *income × 15 / 100*, so widening the Bin alone would have left the same
contradiction one level up.

**What it left behind.** Two deliberate absences, both under `adr/0052` and `adr/0043`: **no §B row** for
*is 2⁶³ enough* — the standing long-run magnitude clause already watches it, and a row nobody will check
is noise — and **no *nearly full* threshold**, because that would be a chosen constant with no ratifier
where a trend needs none.

---

## Tasks 3 and 4's implementation record — one commit, and five findings

**Shipped 2026-08-10**, as [`0003`](0003-build-plan.md)'s hash-moving queue item 3, and as **one commit**
for the reason `adr/0065` gives: the two decisions touch the same two columns, so a baseline that moved
for both would be attributable to neither if they shipped apart. Everything both ADRs specify is built —
capacity is `Rows.Derived<long>`, `World.CreateBin` no longer takes a ceiling, `RebuildCapacities` runs at
load and inside `Adopt`, the level and the whole write path are `long`, and the end-of-run check is
`Invariant.BinCapacityMatchesItsDeclaration`, id **29**.

**1. `adr/0064` recorded a live defect that was fixed two slices before it was written, and the reason is
a missing test.** Its Consequences said *"`RulesetLoader` refuses nothing of the sort today"* about a
duplicate `(kind, Resource)` Bin declaration. It has refused it since slice 7 task 8, in its own words —
*"this kind declares two Bins for one Resource"*. The refusal was the **one guard in that loader with no
test**, `RulesetLoaderTests` is where you look to find out what the loader refuses, and the sitting looked
there. So the reasoning that reached the key was sound and the claim about the code was never checked
against the code. **This is `plans/0012` *Cause 1* on a different axis**: not a second copy of a fact
drifting from the first, but a fact with **no copy at all** being re-derived wrongly from the shape of its
absence. Amended in the ADR; the test ships here. The generalisation worth keeping: *a guard with no test
is invisible to every future reader, including the one who is about to decide it does not exist.*

**2. The world fixture's baseline did not move, and that is a coverage statement rather than a relief.**
This change removed a column from the State Hash and doubled another's width, and `world-hash.txt` was
byte-identical — because `GoldenFixtures.Build()` builds a city with **no Bins at all**. Only
`session-trace.txt` noticed. The two artefacts are supposed to be complementary (`Golden/README.md`), and
on this change one of them was simply blind; nothing is wrong with it, but a reader who saw one file move
and one hold still could reasonably have concluded the change was narrower than it was.

**3. The over-full drain could not be tested with the fixture that was there, for an instructive reason.**
`RulesetMigrationTests`' `Both` declares `upkeep`, a Rule drawing on a Resource **nothing produces** — which
is what makes it useful for wait lists and useless here: an over-full Bin that nothing consumes stays
over-full for ever and cannot distinguish draining from clamping. A producer-and-consumer pair
(`Roomy`/`Tighter`) was needed, and with it the composition `adr/0064` predicts is directly observable:
the ceiling falls 12 → 2 under a **numbers-only** reload with no migration and no degradation, the level
stays above it, `restock` stops because `FloorDiv` makes negative headroom unaffordable, `eat` is untouched,
and the Bin comes back into range by being spent.

**4. `long` reached a boundary the ADRs did not name: the Map Layer.** `RuleEngine.Emit` computes
`emission.Amount × applications` and hands it to `Layers.EmitPollution`, whose cell is an `int`. Widening
the applications count turned an `int` product into a `long` one crossing into a narrower store, and the
answer is a **loud guard** rather than a cast — a Rule emitting more than `int.MaxValue` into a Layer
throws with the number in the message. `adr/0065` enumerated three products inside the Rule engine and
this is a fourth, at the engine's edge; the lesson is that *the whole write path* includes the paths that
leave.

**5. The test suite stopped being able to ask for a capacity, which is the ADR made visible.** `BinTests`
built its Bins by hand with `capacity: BinCapacity.Of(100)` and its fixture Ruleset deliberately *declared
no Bins*. Under `adr/0064` there is nowhere for that argument to go: the only way to ask for a Bin holding
100 is to declare one. The fixture now declares both Bins and every assertion below is unchanged — a
compile error standing in for the design claim, which is the cheapest form of enforcement available.

---

## Task 2's record — the question was filed twice and both filings had the wrong subject

**Argued 2026-08-10. Settled by
[`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md),
with [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)
underneath it.** A Building's occupancy is declared per `[[kind]]` and is `derived AND rebuilt` from the
Ruleset in force; an over-capacity Building evicts the overflow into the Pool by a draw under its own
`purpose_tag`; and **`02 §5.2` step 2, Household placement, is a mechanism of its own** — construction
raises a Building and houses nobody.

**The typing conflict dissolved rather than resolving, and that is the finding.** The brief's first
instruction was to fix a claim filed in two sections with two types. It is not one claim: it is a
**defect** (two populators disagreeing, owed to code), a **shape** (arguable, closed here), and a
**value** (a number under `adr/0052`, which is §D and never §B — §B holds claims a measurement could
*refute*, and *how many families fit in a flat* is a choice). **Neither filing was right, and they were
not disagreeing about the type.** They were agreeing about the wrong subject.

### Four things checked against the code or the text, none of them argued

**1. `adr/0025` covers it, and both `0002` §C and this brief say it does not.** The disqualifier used in
both is *"density is a cap on what may be built … this is how many families fit in what was built"*.
`adr/0025`'s section is headed **Capacity, not quality** and opens *"Density says how many Occupants a
Lot may carry"*, with a consequence reading *"A Building holds many Occupants"*. **Third sighting in one
session of a decision re-derived as absent** — tasks 3 and 4 met it as `RulesetLoader`'s untested guard,
and here the copy exists and is in a ratified ADR.

**2. A Rule already reads occupancy, and the shipped Ruleset does.** §C's *"it breaks nothing today — an
occupant list is unbounded and no Rule reads its length"* has been false since slice 7 task 10a:
`Readout.Occupancy` is the **only declared Readout**, and `rulesets/minimal.toml:100` is
`apply = { derived = "occupancy" }` on `consume`. Both shipped Rulesets scale a dwelling's drawdown by
how many Households live in it.

**3. The mismatch is not inert — it decided whether the committed golden baseline was broken.**
`Invariant.cs:370`, written by task 1 four hours earlier: *"a producer whose deficit is 2, drawn down by
the **occupancy-1** Buildings a Zone Rule creates, is withdrawn from one unit at a time and never
woken."* At occupancy 3 the deficit is covered on the first withdrawal and the violation does not exist.
**So the sentence *it breaks nothing today* appears in the same ledger entry as the defect it broke**,
and neither of the two documents holding this question noticed.

**4. `World.Place` has exactly one caller, inside `ZoneRuleEngine.Create`.** Nothing puts a Household
into a Building that already stands. Of `02 §5.2`'s six steps only **step 5** exists. That is
`adr/0069`, and it is what actually settles the equilibrium.

### The methodological finding, and it is the largest thing here

**The sitting spent its first exchanges asking whether construction should fill a Building to capacity,
and that question is circular.** It is only a question because placement does not exist — and the answer
to *nothing fills the Building* is to build the thing, not to distort construction into standing in for
it. **The session was letting the current shape of the code generate design positions.**

That is the exact mirror of what tasks 3 and 4 found, and the pair is worth stating together:

| | Direction | Instance |
|---|---|---|
| Tasks 3–4 | an **ADR** was wrong about the **code** | `adr/0064` said the loader refused nothing; it had refused it since slice 7 |
| Task 2 | the **code** was allowed to be wrong about the **design** | `§5.2` step 2 is unbuilt, so two ledger entries concluded a *number* settles an equilibrium a *mechanism* settles |

**Both are a fact with no copy being re-derived from the shape of its absence** — `plans/0012` *Cause 1*
— and the second is more dangerous, because a missing mechanism does not look like a gap. It looks like
a constraint. → [`0012`](0012-corpus-audit.md).

### What the eviction argument turned on

The sitting proposed transplanting `adr/0064` whole — *refuse admission and let it drain* — and it fails
on one property: **a Bin has a consumer and occupancy has none.** There is no housed departure and no
moving, so there is no attrition for the drain to run on, and the Building would sit permanently over a
ceiling its kind says is impossible while `consume` bands on the real number. **That is the split city
`adr/0064` called its own deciding argument**, arriving worse: a Bin's over-fullness spends itself and
this one never would.

It is also the same fixture problem tasks 3 and 4 hit — *an over-full Bin that nothing consumes cannot
distinguish draining from clamping* — met a second time as a **design condition** rather than as a
difficulty in writing a test. Recorded because the first sighting read as an accident of `upkeep`.

### What was deliberately not decided

- **The number.** → `0002` §D, with a ratifier. `adr/0043` bars this session from it and the brief says
  so twice.
- **The acceptance filter** — `§5.2` step 2b's *affordable? a reachable job in budget?* — the sampler's
  bias, the scored choice and `μ`. Those are `§5.4`'s and milestone 9a's, and `adr/0069` declines them
  by name for the reason `adr/0054` declined 9a's pieces.
- **Whether an evicted Household is preferred over a chooser when placement runs.** The Pool records no
  entry route and `CONTEXT` → Unplaced Pool says all four enter **on equal terms**, which is a decision
  already taken. Raising it here would have reopened it for a convenience.

### What shipping task 2 found — the ADR was wrong about the outcome, and about the numbers

**Both items shipped green and both ADRs are amended in place.** Five findings, ordered by how much
they change what somebody should believe.

**1. The five-sixths equilibrium does not close, and predicting that it would was this session's own
error running forwards.** *Five-sixths homeless* was 83%; it is **53%**, and the residue is not a
mechanism gap — `rulesets/minimal.toml` demolishes every dwelling it raises, which its header states at
length and on purpose. **What the pass actually fixes is vacancy**: before it, **45% of the housing
stock stood empty** while 70% of the population queued; after it, **10%**, which is the floor a city
that is continuously building carries. So `PlacementLongRunTests` asserts **vacancy and not
homelessness** — *everybody is housed* is a property of a Ruleset's **balance**, and the shipped Ruleset
explicitly declines to have one. **The acceptance criterion written in the queue was a content
prediction wearing a mechanism's clothes.**

**2. Three hash-bearing numbers, where the ADR predicted none.** `adr/0059`'s precedent derives the
*sample* from a duration and that half held — but the **duration** is a free parameter and so is
**`candidates`**, and neither is derivable from anything. `0002` §D gained three rows rather than losing
a question. `revisit_ticks` shipped at **8192**, one Day, copied from `adr/0059`'s default — and one Day
is how often the *development industry surveys the city*, where a family without a home looks more often
than that. At 8192 it left 45% of the stock empty; it is **1024**.

**3. The candidate draw is over Lots, not Buildings, and the first implementation had it wrong.** The
Building table is a **recycling** table: under the shipped Ruleset roughly 55% of Building slots stand
freed at any instant, so three candidates bought about **1.3** real looks, and lowering the demolition
rate would have silently raised the effective candidate count. A Lot is a place in the city and the Lot
table's slot count is the size of the city. **A number whose meaning moves with an unrelated rate is not
a number a designer can author**, which is `adr/0059`'s finding in a second shape.

**4. `adr/0068`'s derived column was not needed, and the ADR is amended.** It predicted
`derived AND rebuilt` on the strength of `adr/0064`'s Bin capacity. A Bin needed a column because
`HeadroomAt` is hot-path; occupancy is read at a guard that runs once per placement, and the Building
already carries its `Kind`. **The row loses an obligation rather than gaining one** — the end-of-run
derivation check mirroring id 29 is struck.

**5. The Census gained a fourth metric family, and writing it exposed that the third has no test.**
`considered` and `placed` are two flows for the reason `evaluations − due` are: a queue looked at and
not housed is a city out of dwellings, a queue not looked at is a mechanism that has stopped, and one
counter cannot separate them. Nothing in the suite reads a `ZoneCounter` back through a `Census` —
**`adr/0064`'s id-29 shape**, a block written and never read — so placement ships with one and the Sweep
family's gap is filed. **Two sightings of *a thing with no test is invisible* in two days.**

**One tripwire moved and it is recorded rather than quietly widened.** `ZoneRuleLongRunTests`' Pool
high-water convergence tolerance went from 1/32 to 1/16. The Pool used to drain one Household per
Building raised; it is now a queue worked off at a rate, so its excursions are wider and the largest is
sampled later — the plateau arrived at 37,000 Ticks and now arrives at 70,000. **Measured out to
300,000 Ticks the mark holds at 235 from 70,000 onward**, so the shape is unchanged and only the
100,000-Tick window straddles the knee. And the same file's Pool **level** assertion was **moved rather
than deleted**: a Household left the Pool only when a Zone Rule built, so the level was a statement
about that engine; it is placement's now, and it lives in `PlacementLongRunTests`.

---

## Task 0's record — the cluster, typed

**Partly ran 2026-08-10, before anything was argued.** `04` end to end, `02 §3`–`§5` claim by claim, and
eleven ADRs. **Nothing in the measurable list may be closed by this session.**

### Six findings that outrank the list

**1. `adr/0033` requires an invariant nobody has built, and it is the check both live defects need.**
The ADR states two mitigations for the silent failure mode subscription introduces and says **"both
required"**: Bins are not public fields (**in place** — `BinTable`'s write function and the wait-list
heads), and **a sweep invariant, *no Rule is asleep with all inputs satisfiable*, "unaffordable per Tick
and trivial at the end of a headless run"**. `02 §10`'s end-of-run tier names it too, and so does
[`0008`](0008-tick-and-replay.md). A sweep for `satisfiab` across the tree returns those three
statements of intent, one doc-comment on an unrelated enum, and **no implementation and no test.** It
would have caught defect 3 in CI, and it is the only proposed instrument that would notice defect 1
happening in a running city. **This is not a claim to type — it is owed slice work**, and it is the item
most likely to move work out of the argument track and into the code track, which is the direction
[`0000`](0000-board.md) asks for.

**2. `02 §5.7`'s build-rate throttle is an *absolute* cap on projects per cycle — which is the defect
`adr/0059` deleted, sighted a second time and filed nowhere.** Slice 10 task 11 is currently fixing the
Zone Rule `sample` because an absolute count means a Lot is visited once per 0.12 Day at 1,000 Citizens
and once per **117 Days at 1M**. The throttle paces in the same unit. **So the open code item is fixing
one instance of a two-instance defect**, and the second instance is in a design document rather than in
a Ruleset. *A unit that is wrong is worse than a value that is wrong, because tuning cannot reach it.*

**3. `adr/0033` still carries the arithmetic slice 10's tripwire falsified.** Its *"eight Policies over
ten thousand Households is ~10 integer comparisons per Tick amortised, and it stays affordable an order
of magnitude up"* rests on population as the denominator; the tripwire measured **1.56×** over a 1,000×
Zone against a control that moved **989×**, so the cost is `O(sample)` and the variable is the **working
set**. [`0012`](0012-corpus-audit.md) holds the row against `02 §5.7` and **not** the row against the
ADR that governs it. **That is *Cause 2* running backwards — the correction reached the design document
and not the decision — which is exactly the direction session D found twice in `adr/0007`.** Two
sessions independently finding the same asymmetry makes it a sweep rather than two filings. → `0012`.

**4. `adr/0031`'s *"conservation becomes one invariant"* is false against the code and against
`adr/0050`.** The ADR claims *nothing is created or destroyed except at the gate* is now a single check
across all nine Resources. What exists is `RulesetLoader.RefuseUnbalancedMoney` — **per-Rule, and
explicit money terms only**, refused in either direction because destroying and creating money are one
defect with the sign flipped. `adr/0050`'s payment is *implicit in the scope* and therefore outside that
refusal by construction. → `0012`, and it sharpens task 4.

**5. `adr/0031`'s *maximum chain depth of three* has `adr/0045`'s withdrawn-cap-of-5 shape.** It is
presented as settled and sourced to `04 §1`'s own words, and it is a depth number derived from the
current graph rather than measured. `adr/0045` types depth **measurable** and names the instrument —
and **the instrument has run**: slice 7 task 9 measured a chain rung at **53.6 ns** against the head's
82.84, which is two-thirds, and is the first evidence that depth is the cheap axis. **So a number the
corpus withdrew once is still standing in a neighbouring ADR**, on a claim the machine can now settle.

**6. `04 §6` step 4 could not be typed.** *A Household must actually attempt the purchase and actually
fail* is the design's central evidentiary commitment and it is neither arguable nor measurable as
written, because **nothing states what a Provider List costs, how long it is, or how it is
maintained** — so there is no number to name and no mechanism to argue against. That is a third
category: **not a claim, and not a number — a mechanism named without being specified.** It goes to §E,
owed work, rather than to §B or §C.

### Measurable — routed, and N must not close them

Grouped, because the pass found more than twenty and a list that long stops being read. **Every row
below has no number and no named machine**, which makes them `adr/0043` suspects rather than deferred
measurements.

| Cluster | The claims | Machine |
|---|---|---|
| **The price ladder is a price ladder** | `02 §4.1`: *local → Pool → Shipment → import is monotone increasing in cost, which is why the rungs are in that order.* **No prices exist** | `pool`, then the economy. This is the load-bearing one: the fallback chain's *order* is justified by an ordering nobody has computed |
| **Gradient degradation** | `02 §4.1`: *under half supply every bakery bakes half as often*, rather than half the bakeries running and the rest starving. Number: the distribution of per-bakery firing rates at 50% supply | The Rule engine **with a Pool** — which throws. **Not currently producible**, and it is the claim defect 1 most threatens |
| **Greedy versus fixed quantum** | `02 §4.1`: a greedy Rule *crosses that boundary more often than a fixed quantum of the same throughput*. **The machine exists** — slice 7's counters — and no number was ever taken | slice 7's counters |
| **The choice model's cost** | `02 §5.4`: the softmax *costs nearly nothing per agent*; Gumbel-max is *cheaper*; `log(1+x)` is what stops the centre winning for ever. Three claims, no ns figure, no share-of-placements figure | 5b-class, plus the choice model, unbuilt. **[`0013`](0013-tick-budget.md) has no row for placement at all** |
| **The bid mechanism** | `02 §5.5`, `04 §1`: Office *outbids every other use for the most accessible land*; a pure office core *strangles itself on Segment Stress*; with only two of three agglomeration forces, *cities grow without bound* | The bid mechanism, **which does not exist** |
| **Abandonment terminates** | `02 §5.9`: *a cycle, not a spiral*, damped at the bottom because land value eventually falls far enough that redevelopment pencils. Number: that land value | The bid mechanism |
| **Money's cost and behaviour** | `adr/0024`: one integer per actor is *immaterial*; velocity is emergent and *no velocity constant is authored*; a net drain *can seize*; poverty is an absorbing state. **The sizing machine has run for a neighbouring claim** — S0a, 85.98 MiB at 1M — and was not run for this one | S0a-class for the bytes; 9a + money for the rest |
| **`adr/0026`'s labour claims** | The **forty**-worker twitch; the shrinkage weight; *a school 15% understaffed is 15% worse* — the proportionality is a choice, the **linearity** is an untyped empirical assertion | The labour market, unbuilt |
| **The Bin walk** | `02 §3.2`: *a Building holds few enough Bins that a walk beats a search*. Number: the crossover, and the actual per-Building Bin count | A microbenchmark. Cheap, and nobody has run it |

**Already typed correctly by their own text:** `adr/0031`'s unbounded-Bin arithmetic (settled in code,
with invariant 14 and `MoneyIsRepresentable` behind it); `02 §3.1`'s conservation invariant (the
sentence names its own debug pass); `adr/0045`'s depth claim. **Three of more than twenty carried their
own machine** — the same ratio D found.

### Arguable — this session may close them

What a wait list wakes on, and what a waiter is promised (task 1); the **shape** of a Building's
occupancy, never the number (task 2); whether a capacity is a historical fact about a Building or a live
property of the Rules, and what happens to an over-full Bin (task 3); what *full* means for an unbounded
Bin (task 4); `04 §6`'s chain, and whether step 4 survives as stated (task 5); the labour price,
Materials drawdown and the pollution-machinery question (task 6); **where private capital comes from** —
*probably derived, with a floor*, and the loop it adds could deadlock a struggling city; and
**multiplicative rather than additive utility** in `§5.4`, where SILO multiplies so zero on any
component yields zero total — *no amount of cheapness compensates for zero reachable jobs*, expressed
structurally rather than as a penalty.

### Not a claim — the §D3 and §E buckets

- **`adr/0033`'s satisfiability invariant** → owed **slice work** (finding 1). It is `adr/0006`-class
  and belongs to whichever slice next touches the Rule engine.
- **`04 §6` step 4's Provider List** → §E, a mechanism named without being specified (finding 6).
- **`adr/0031`'s `storage` field** → a design commitment, unbuilt; `RulesetLoader.RefuseStorage`
  refuses it outright as a named hole. Not a claim to type.
- **`adr/0026`'s worked diagnostic** — *130 vacancies. 1,610 excluded — commute exceeds budget* — is
  **illustrative**, and the diagnostic requirement behind it is real. Keep them apart.

### Does this session emit a spike?

**On the pass so far, no — and that is the answer to check rather than assume.** Almost every measurable
above routes to `pool`, to 10b or to a Phase 2 milestone rather than to a harness, because **the
machine that would settle them is the simulation itself**. Two exceptions are cheap enough to be worth
naming: the **Bin-walk crossover** (a microbenchmark, S4-class) and the **per-actor byte cost** of
`adr/0024`'s balances (an S0a re-run with balances present). Neither is a spike; both are benchmarks
that belong to a slice. **If the sitting finds itself wanting a spike, that is a finding and should be
recorded as one** — a spike out of task 0 is `adr/0043` working, and anything later is scope growing.

---

## Acceptance

- **Every claim in the cluster is typed**, and nothing that typed *measurable* was closed here.
- **Defects 1–4 each have a design answer or a written statement of what blocks one**, and `pool`'s
  shape is decided far enough that **slice 7 task 10b is specifiable**.
- **The occupancy question is filed once**, in one section, with one type.
- **Every number written down has a named ratifier and a revisit trigger**, or was not written down.
- **`0002` is updated in the same sitting** — closures struck in §C, new numbers into §D with their
  ratifiers, the §E entries added, and **§F's marks moved** for every document the session touched.
  **§F's blanket `adr/0010`–`0022` row is split for every ADR this session read**, since a status whose
  granularity is coarser than the claims it covers cannot be checked.
- **`0012` gains findings 2, 3 and 4** as corrections owed to documents.
- An ADR per decision, and each cites a guiding concept from `CONTEXT.md`'s tag table.

## What this session deliberately does not do

- **It does not choose an occupancy figure.** The number is measurable and the machine exists
  (`ZoneRuleLongRunTests`); N may only decide the shape it would be declared in.
- **It does not price anything.** N stands over no row of [`0013`](0013-tick-budget.md), and the Rule
  engine's owed multiplicand is 10b's.
- **It does not touch `03`, or `adr/0005`, `0007`, `0008`, `0009`, `0012`, `0016`, `0046`, `0047`.**
  Those are session D's, running in parallel. Where the economy needs a movement fact — freight,
  Shipments, the Commute Budget — **cite D's cluster and do not argue it.**
- **It does not touch the playtest questions** `0002` names as design questions in disguise: health,
  recreation, Service variants, and whether Outside Connection price drift is switched on, which `04
  §8.7` already routes to `01 §5`'s shock layer.
- **It does not produce more sessions.** Same guard D carries, for the same reason: the last time the
  argument track led, *the design was generating design*.

## Decisions owed, found while briefing

**1. Is this one session or two?** The cluster splits cleanly at a seam: **the Bin** (tasks 1, 3, 4 and
finding 1 — the Rule engine's semantics, all four defects, entirely inside `02 §3`–`§4`) and **the
economy** (tasks 5, 6, 7 and the occupancy shape — `04`, `02 §5`, and every unplaced `06` mechanism).
The first is small, sharply blocked and could run in an evening; the second is `04`-wide and closer to D
in size. *Recommendation: run the Bin half first as its own sitting, because it is what `pool` and 10b
wait on, and it does not need the economy argued to be answered.*

**2. ~~Does N or a slice own the satisfiability invariant — and now `adr/0063`'s implementation with
it?~~ SETTLED 2026-08-10 — sequenced in [`0003`](0003-build-plan.md) → *The hash-moving queue*, and
*shipped* the same day.** Three
code items, ordered **invariant → task 11 → wake predicate**: the invariant is hash-neutral so it costs
no baseline and goes first, task 11 is the only one of the three whose defect is live *now* (the shipped
Ruleset builds nothing at 1M), and the predicate is last because it cannot manifest until `pool` exists.
**Deliberately two baseline re-records rather than one combined pass** — combining two unrelated
mechanisms into one hash move is `0013`'s *right by cancellation* hazard in the hash trace, and a
re-record is a command where a mis-attributed hash move is a bug hunt.

**Sequencing it also corrected `adr/0063`.** The brief and the ADR both implied the invariant would
demonstrate the fix; it does not. It **passes before and after**, because producing a violation needs a
trickle-filled Bin, which needs `pool`. The invariant is the **regression guard**; the acceptance test is
three `Deposit(1)` calls against a waiter requiring 3, which needs no `pool` at all. Amended in the ADR
in place. An obligation specified in three documents and built in none is how `HouseholdHomeExists` came
to be reported by nothing — and *treating a guard as a test* is how it would have happened again.

**3. Which letter is this?** Filed as **N** — the lettered menu runs A–M and the economy has never had
a letter, being carried in `0002` §C as *"an economy session"*. If the sitting splits per decision 1,
**N1** and **N2** rather than a second letter, since K1/K2 is the corpus's precedent.
