# 0017 — Session D: the traffic model (`03 §5`)

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). The design being argued is
> [`03-agent-architecture.md`](../docs/03-agent-architecture.md) §5. Status and cross-track order are
> [`0000`](0000-board.md); every open question is [`0002`](0002-open-questions.md).
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

---

## Status

**RUN AND CLOSED, 2026-08-10 — all five tasks, in one sitting.** Produced
[`adr/0060`](../docs/adr/0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md),
[`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md),
[`adr/0062`](../docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md), an
amendment to `adr/0046` **withdrawing a ratification**, and one spike (**S5**).

**Its three decisions owed, answered.** **(1)** `03 §2`, the Citizen model, is **not** in scope and is
named as its own sitting rather than discovered later. **(2)** *One ADR or several*: **three**, one per
separable decision, which is the corpus's convention. **(3)** The **fidelity boundary stays with session
E** — D took the Cap's *shape* and not the tiers themselves, and E inherits two constraints rather than
a free hand: nothing is ever evicted, and force-promotion outranks stress-promotion.

**And it obeyed its own guard.** *D is booked to produce the traffic model as a design, not to produce
more sessions* — it produced **one spike** (the permitted exception, out of task 0) and **one named
successor sitting**, and routed everything else to instruments that already exist or are already owed.

*(Original status follows.)* ~~**Task 0 has run. Nothing has been argued yet.**~~ The board promoted D on 2026-08-10; the typing pass
ran the same day, before the first argument, which is the order this brief requires. Its record is
[below](#task-0s-record--the-cluster-typed), it emitted **one spike (S5)** and it moved four documents.
The sitting opens on **task 1**, the diversion policy.

Sessions do not contend with the code track, so the rest may be booked whenever there is a sitting.

## Why this one, and why now

**D is the first session in this project booked against a *number* rather than against a document
being ungrilled**, and the standing rule was applied rather than suspended. The board's rule is *an
argument session runs when something concrete is blocked on it, never because it is available*. What
is blocked:

- [`0013`](0013-tick-budget.md)'s **dominant row**. Routing carries **60–67 of the ledger's ≥114
  points at 4×**; without it the ledger reads 42–48% and fits at 4× with room. **So the headline
  *fits at 2×, does not fit at 4×* is a statement about routing and almost nothing else.**
- That row's multiplicand **counts the wrong event** (S2 R6.3), and the only thing that can replace it
  is **Trip generation — milestone 5b, which D gates**. So the question *does the simulation fit*
  cannot be answered without this session.
- The correction with a known direction points **up**, not down.

**A second, independent line arrives at the same place.** [`0002`](0002-open-questions.md) §F's
rebuild found that the corpus's remaining 🔴 is **essentially one cluster** — `03 §5` plus `adr/0005`,
`0007`, `0008`, `0009`, `0012` and `0016` — which is the traffic and movement model. **One wall, not
many small gaps**, gating every Phase 2 milestone.

## Gate

**None.** D's old *"partly — the half that wants S2's numbers waits for R1–R3"* caveat is discharged:
R0 through R8 have all reported.

## What it must read first, and the caveats that travel with each

**Design under argument:** `03 §5` (the traffic model), and `03 §3.3`/`§3.4`/`§3.8`, whose joint
rewrite is two-thirds done — **the third clause is a decision this session may be the right owner
of**: *force-promotion must stand on its own second argument or go*.

**Decisions in the cluster:** [`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md),
[`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md),
[`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) **including session M's
amendment**, `adr/0016`, `adr/0008`, `adr/0009`, `adr/0005`, `adr/0007`.

**Measurements:** [`spike-results`](../docs/spike-results.md) → *S2 R6.3*, *R6.4*, *R8*, *R5.6*.

> ### Three caveats, and they are not decoration
>
> **1. The frozen cost basis.** Everything **R1–R5** published invalidated a route because a road was
> *bulldozed*, never because one got *busy*. R8 closed that loop for itself only. **Quote nothing from
> R1–R5 as a statement about a congested city.**
>
> **2. The invented O-D draw.** S2's origin-destination family is invented and only Trip generation can
> replace it. **No figure derived from it may be cited without naming the rung** — uniform is the
> *longest-trip* distribution available, and the same table's detour runs 18.52% on it and **128.82%**
> on the tightest local rung.
>
> **3. ⚠ The 37k–111k in-flight band is a defective derivation, not a number.** It conflates duration
> sensitivity with peaking and is owed a re-derivation on both axes ([`0002`](0002-open-questions.md)
> §D3). **R6.3's 795.91% floor and 2,387.73% ceiling are computed from it and inherit the defect.**
> **861.87% is the figure to argue against** — it is R6.3's own rung, 40,000 Travellers on a 7-Day
> Habit, and it does not depend on the band. This is [`0013`](0013-tick-budget.md)'s own lesson
> arriving again: **type the two halves of an estimate separately before acting on it.**

---

## Tasks

### 0. The typing pass — and it runs **before** anything is argued — **RUN 2026-08-10**

> **Done, and its record is [below](#task-0s-record--the-cluster-typed).** Eleven claims typed
> *measurable*, of which **six were new to [`0002`](0002-open-questions.md) §B**; one spike emitted,
> **S5, the Lane kernel**; two `adr/0007` defects filed to [`0012`](0012-corpus-audit.md); two
> unset hash-bearing number-sets filed to §D2. The task text below is kept as written, because it is
> the instruction the record answers.

**Type every claim in the cluster *arguable* or *measurable*** per
[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md):
`03 §5`, and `adr/0005`, `0007`, `0008`, `0009`, `0012`, `0016`. The test is *can you name the number
that would refute this, and the machine that would produce it?*

**Rule, and it binds this session against itself: if a claim types measurable, D must not close it.**
Route it to a named spike with the refuting number written down, and no document may cite it as
decided until that number exists.

**Why first rather than after.** `adr/0043` exists because two claims passed through 🟢 rows of the
coverage map and were later measured false — and **neither carried the decision its ADR was about**,
which is exactly how a supporting sentence survives a session unread. D is about to grill six
documents **none of which has ever been typed**, and [`0002`](0002-open-questions.md) §F flags three of
them ⚠ — *reads decided, has no number*:

| ADR | The untyped claim |
|---|---|
| **`adr/0016`** the lane is the entity | **Carries the order-of-magnitude claim the whole Microscopic tier rests on.** `adr/0043`'s top remaining suspect |
| **`adr/0009`** parking is modelled supply | Its `adr/0006`-class occupancy leak is named and its invariant unspecified. **R5.6 has already measured the shed's invalidation** and found the two consumers do not want the same mechanism |
| **`adr/0008`** walking is a simulated Leg | Makes 5b *the irreversible milestone*, so it is owed **before** the Leg model is built rather than after |

**Expected output:** a typed list, and **plausibly a new spike**. A session that emits a spike here has
succeeded, not failed.

### 1. The diversion policy, made a design — **SETTLED 2026-08-10**

> **Closed into [`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md)
> and [`adr/0060`](../docs/adr/0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md),
> and it took task 2 with it.** A Rejoin is **local descent toward the node the Traveller declined to
> enter**, never a search; a failed Rejoin points the same rule at the destination and continues, with
> **no new Trip Fate**, on `adr/0009`'s precedent; and a failed Rejoin is **counted rather than
> corrected** — an **Aggravation** fraction that, at threshold, switches the Citizen to another
> **variant** of its Habit Route.
>
> **Three findings outlived the sitting.**
>
> **1. The compromise everybody reaches for fails on arithmetic.** *Re-search on failure only* lands at
> **≈181 re-searches a Tick against a band of 32–147** at the measured rungs. It does not rescue the
> design, it re-admits it in proportion to the failure rate — so the tripwire to publish is *fits only
> below roughly 2–12% rejoin failure*, which is a far harder demand than 85.74% success.
>
> **2. `adr/0046`'s own first row was false, and reading it back is what produced the answer.** Its
> table maps `adr/0017`'s *"short, sticky list"* onto **Habit**, which was built as **one** route — and
> a list of one member cannot be switched, only discarded and recomputed. Every other actor class in
> this simulation satisfices over several known options. Making that row true is `adr/0060`, and it is
> **the structural answer to task 2**: route diversity comes from the population being distributed
> across candidates, with no congestion feedback and no global knowledge anywhere.
>
> **3. The session designed a classification and then deleted it.** An earlier form split the failure
> at the instant it happened — a **strand** is evidence about what exists and could mark the Habit
> stale, a **spent budget** is congestion and could not — which worked, and required the simulation to
> tell a jammed one-way pair from a severance. The Aggravation counter needs no classification, so one
> mechanism replaces two, and session M's stale bit keeps its **two** setters rather than gaining a
> third. `adr/0059`'s direction, arrived at from a different door.
>
> **What did not get settled, and correctly so.** `k`, the Rejoin crossing budget and the Aggravation
> threshold are all hash-bearing and all **measurable** — three new §D2 rows, each with a named
> ratifier. And `adr/0060` spends part of the route cache's hit rate, which is the one quantity
> routing rests on and nobody has measured, so the ADR is written to be continuous in `k` with `k = 1`
> restoring exactly today's behaviour.

### 1. The diversion policy, made a design *(the brief as written)*

**Session M settled it in principle — *rejoin the Habit Route rather than re-search* — and nobody has
ever made it a design.** It is the lever against the largest number in the corpus, and it is free by
construction, which is why it appears nowhere: no benchmark proposed it.

What is owed, and none of it is measurable:

- **What *rejoin* means mechanically.** R6.4.2 measured the cost and found **the Sight Horizon is two
  parameters wearing one name**: rejoin success cliffs **19.14% → 85.74% at Horizon 3**, identically on
  all five O-D rungs, because rejoining means going round a block and a block on this graph is three
  Segments. **1 is *noticing a choice*; 3 is *recovering a route you have left*.** `adr/0046` sets
  neither and the corpus does not separate them.
- **What happens when a rejoin fails**, which is the `HONEST DEGRADATION` question.
- **Which of the three levers carries the load** — the Temperament threshold, the Sight Horizon toward
  its 1-Segment floor, or the rejoin. **Between 32 and 147 diversions per Tick fit; R8.3 measured
  1,269.51.** The route cache cannot be the answer: it would need **88.5%** at 40,000 in flight, on
  R6.1b's worst input.

### 2. Decision 11 on the other axis — *the network runs out of routes, not road* — **ANSWERED IN SUBSTANCE by task 1**

> **[`adr/0060`](../docs/adr/0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md)
> supplies the diversity, and it arrived sideways** — out of task 1's question about what a frustrated
> driver recomputes against, not out of this task's own framing. Concentration is **not** a congestion
> phenomenon and could never have been fixed by a congestion response: `adr/0047` removed one shared
> route per *(node, District)* pair and left one shared route per *(node, node)* pair, because a single
> shared cost basis returns one answer. **The cure is more routes, and the supply is the population
> being distributed across them.**
>
> **What remains here is measurable, not arguable**: whether variants actually disperse traffic is
> R8's concentration column re-run over a variant-supplied route set at `06` 5b, and it is a §B row.
> If it barely moves, the degeneracy is upstream in the road layout and belongs to `adr/0014`.

### 2. The task as briefed

**R8's largest finding, and the one thing S2 explicitly could not close.** **87.25% of traffic on 1% of
the carriageway, 90.87% of it empty, at 13% of holding capacity with capacity confirmed realistic** —
because one free-flow tree per District means **one route per (node, District) pair in the whole
model.**

`adr/0047` has since deleted the District key. **So what supplies route *diversity*?** That is a design
question about the movement model, not an algorithm, and it is the question a player would notice
before any of the others: a city whose traffic uses 1% of its roads does not look like a city.

### 3. `03 §5` itself, and the Microscopic Cap's *shape* — **SETTLED 2026-08-10, and it settled less than it was asked to, on purpose**

> **Closed into [`adr/0062`](../docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md).**
> The Cap counts **Vehicles**, not Segments; **nothing is ever evicted**, so a full Cap refuses;
> **force-promotion outranks stress-promotion**, because spillback is correctness and better travel
> times are accuracy; at the Cap the boundary refuses entry, which `03 §3.9` already specified; and
> the **virtual queue** is refused *and named*, so reaching for it later is a decision rather than a
> drift.
>
> **The unit was wrong in three places at once** — `03 §3.9`, `CONTEXT.md` → Microscopic Cap, and
> `CONTEXT.md` → Segment, the last of which carries it inside a list of things counted per Segment,
> which is exactly how a wrong unit propagates without anybody deciding anything. It is the family
> `adr/0053` and `adr/0059` already named, and it cost a sentence to repair because nothing is built
> on it; S0b's Zone Rule finding is what it costs after a balance pass.
>
> **The session made an argument and withdrew it, which is the part worth keeping.** *A full Cap
> should shed the newest stress because the VDF is least wrong at the onset of congestion* — that is
> an axis of **age**, `adr/0007`'s claim is an axis of **saturation**, and the difference was invented
> in the sitting. It is measurable (divergence against Ticks since the threshold crossing) and
> therefore inadmissible here, so the no-eviction rule stands on **unreconstructible state** instead:
> a queued Segment holds queue position, headway and a Switch Lane traversal that nothing can rebuild,
> and a Segment that has just crossed the threshold holds none.
>
> **Four questions were routed rather than closed, and they share one instrument** — `03 §5.1`'s
> three-scenario acceptance suite, which is **milestone 6's work and not a spike**, so D emitted no
> second spike. Each carries a pass/fail; the full table is in `adr/0062` and the row is in
> [`0002`](0002-open-questions.md) §B.
>
> **And the typing pass caught something after it had finished.** `03 §3.3`'s *force-promotion must
> stand on its own second argument or go* is filed across the corpus as an open **decision**; it is
> **measurable** — run `§5.1`'s spillback scenario with the mechanism disabled and see whether the
> upstream Segment blocks. **A session may not close it**, which retires it from this session's own
> agenda and from the board's *Owed* list as a session item.

### 3. The task as briefed

The section is the most detailed unargued design in the project. Argue the model.

> **~~and now carries transit vehicles, which arrived in session five and were never costed against
> it~~ — struck by task 0, against the document itself.** `03 §5`'s text contains no transit vehicle:
> the Lane model does not distinguish vehicle classes at all. Transit appears in `03 §3.7`'s recorded
> revisit trigger (*a stop is a queue with a capacity, and a platform is the one pedestrian context
> where the no-saturation argument genuinely fails*) and in `§6`'s open questions 4 and 5, both of
> which hand ownership to `01-player-experience.md`. **The coupling this line meant is real and lives
> in [`0002`](0002-open-questions.md) §C** — *does traffic distribute across parallel Lanes at all*,
> which is **dwell time specifically**, a bus stopping in a Lane with three empty ones beside it.
> Corrected rather than deleted under [`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md):
> **a planning document cites and a design document owns, so when the brief and `03` disagree the
> brief is what moves.** *(This is the third time in the corpus that a quotation was copied forward
> instead of checked; the other two are on [`0000`](0000-board.md)'s* Owed *list.)*

**The Cap's *value* is not available** — it needs a built traffic model, and S2 R2 informs it and
cannot set it. **Its *shape* is arguable and is `03 §3.9`'s**: what the Cap caps, and what happens at
it. Note `03 §3.9` already settled that **reaching the Cap is not a failure mode**.

### 4. The unset numbers, under `adr/0052` — **SETTLED 2026-08-10, and the instruction paid off once in three**

> **The brief said *look for the derivation before reaching for a value*, and one of the three had one.**
>
> **Sight Horizon — DERIVED at 1, row deleted.** The floor was already graph geometry; the ceiling is
> **comparison symmetry**, and it is the same number. A driver *has* its own route, so it can read `N`
> live arcs along it; it has **no** route down an alternative past the first arc, because nothing
> searches. At 1 the comparison is one live arc against one live arc; above 1 it is `N` of its own bad
> news against one of the alternative's, and the bias grows without bound. **R8 saw the effect and did
> not name the cause** — *"the horizon stops behaving like a monotone knob"*, with a saturated arc at
> 39.4× free-flow. **The second §D2 row to leave by derivation rather than by choice**, and the
> parameter this name was also wearing survives separately as the Rejoin crossing budget.
>
> **`T` — no derivation, ratifier unchanged, and two things recorded that were not on the row.**
> `adr/0060` made a re-formation compute `k` routes instead of one, so **`T`'s drain is `k` times more
> expensive** than when the bound was chosen — two numbers entered separately that turn out to be
> coupled. And **`T` is now the only recompute left anywhere in the routing model**: everything else
> adapts by switching between candidates that never change, so this is the single line on which route
> computation still happens.
>
> **Temperament — no derivation, and a named ratifier at last.** R8.4's own instrument, which exists
> and which nothing had ever pointed at this number. Its known defect is the reframe: the damping
> response is *"a cliff, not a gradient, and the rungs after it are closer together than the instrument
> resolves"*, so **the thing being looked for is where the cliff is, not where a curve is best**. The
> re-run needs finer rungs, the variant structure, and **both** herds live — `adr/0061` added one at
> the switch and nothing damps it.
>
> **Tally for `adr/0052`.** This session **entered four** numbers (`k`, the Rejoin crossing budget, the
> Aggravation threshold and its spread), **deleted one** (the Sight Horizon), **named two ratifiers
> that had none** (Temperament, and the Cap's two-sided ratio), and **withdrew one ratification**
> (task 5). The rule's stated cheapest path — find the derivation — worked once and failed twice,
> which is roughly the rate the two previous cases would have predicted.

### 4. The task as briefed

Three of [`0002`](0002-open-questions.md) §D2's unset rows are D's, and
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
binds: **a hash-bearing number is chosen with a *named* ratifier written down beside it on the day it
is chosen, or it is not chosen.** A category is not a name.

| Number | Note |
|---|---|
| **`T`**, the Habit staleness bound | Hash-bearing. Spent at Trip start. Ratifier already named: the first `06` 5b run producing a steady-state `P(stale)` and a Trip start rate |
| **Temperament base and spread** | **The routing model's weakest number** — `0002` records that *the base/jitter blend weight has no argument behind it at all* |
| **Sight Horizon** | Floor **derived** at 1 Segment. See task 1 — it is two parameters |

**The cheapest way to satisfy `adr/0052` is to find the derivation.** Both numbers that have left §D2
so far — tau and the arming stagger — turned out to need no choice at all, and `adr/0059` **deleted** a
row rather than filling it. **Look for the derivation before reaching for a value.**

### 5. `adr/0046`'s two open qualifications — **SETTLED 2026-08-10, one closed and one withdrawn**

> **They were not symmetric, and the session's own earlier work is what decided the first.**
>
> **Qualification 1 — the deleted structure. The ratification is WITHDRAWN, and it is the first this
> corpus has taken back.** The ground is not doubt: R8 wrote its own limit clause beside the number —
> its diversion fire rate *"must not be carried to any scheme that gives a Traveller more than one
> candidate route"* — and [`adr/0060`](../docs/adr/0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md),
> written three hours earlier in this same sitting, is that scheme. So the row was not *pending an
> argument nobody had made*; the argument was available, it was made, and it went the other way. **The
> ratifier had been cited rather than applied** — `adr/0044`'s failure, and the reason `adr/0052`
> exists.
>
> **Withdrawal costs nothing, which is what made it affordable to be strict.** It used to mean
> re-admitting a Habit refresh cadence — a maintenance scheme and a hash-bearing number, the prize
> static Habit won. [`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md)
> supplies adaptation as a **switch between candidates that were computed once and never change**, so
> no cadence returns whatever the re-measurement says. **A withdrawal with no machinery behind it is
> the cheapest possible time to be honest about a green mark**, and it is worth recording that the
> corpus reached one.
>
> **Qualification 2 — topology. CLOSED rather than withdrawn**, on bookkeeping and not on a new claim.
> *Static under congestion, never under topology change* reads as a hole and is not one: `adr/0046`
> owns *nothing recomputes because a road got busy*, and `adr/0012` owns *how long a Habit may be
> wrong about a road that appeared* — bounded by `T`, checked at use, and explicitly **not** static.
> Two claims, two owners, both already stated. What was missing was the sentence saying so.
>
> **And R8.5 measured one responder where there are now two.** Sight acts per crossing; Aggravation
> acts per `N` journeys. Two feedback loops on one signal at very different periods is the shape that
> oscillates, and it is strictly larger than the switch-herd `adr/0061` records as owed. Nothing in R8
> speaks to it, and the re-ratifier must carry it.
>
> **The two orphan neighbours are rows at last** — R8's **14.08%** diversion fire rate and
> Temperament's **92.28%** damping, both measured on the deleted structure and both previously rows
> nowhere. The first has had its *consumer* changed rather than only its provenance questioned: under
> `adr/0061` a diversion no longer costs a search, so the number that was the multiplicand behind
> 861.87% now prices rejoin volume and Aggravation accrual instead.

### 5. The task as briefed

The Habit-refresh row is marked **RATIFIED** and carries two unresolved qualifications, both of which
are `adr/0044`'s *citing is not applying* in a new costume:

1. **R8.5 ran on a District-granular free-flow tree that `adr/0047` has since deleted** — using R8's own
   concentration column as one of the grounds for deleting it. R8 states the limit itself: its fire
   rate *"must not be carried to any scheme that gives a Traveller more than one candidate route"*,
   which is precisely the scheme `adr/0047` chose. The likely direction is safe, **but that is an
   argument nobody has made.**
2. **What was ratified is *static under congestion*, never under topology change** — R8.5 ran no edits,
   and a road being built is not a cost signal. `adr/0012` states the topology half separately and it
   is **not** static.

**Two neighbours inherit the same defect and are not rows anywhere**: R8's **14.08% diversion fire
rate**, which is the multiplicand behind R6.3's 1,269.51/Tick, and Temperament's **92.28% damping**.

---

## Task 0's record — the cluster, typed

**Ran 2026-08-10, before anything was argued.** Seven documents read end to end: `03 §5` and the whole
of `03`, and `adr/0005`, `0007`, `0008`, `0009`, `0012`, `0016`. The test applied to every claim was
`adr/0043`'s — *can you name the number that would refute this, and the machine that would produce it?*

**The rule that binds: nothing in the first table may be closed by this session.** Where a row is new
to [`0002`](0002-open-questions.md) §B it is marked so, and §B is the ledger — this is a record of the
pass, not a second home for the entries.

### Measurable — routed, and D must not close them

| Claim | Where it is asserted | Refuting number | Machine |
|---|---|---|---|
| A Citizen record is *"roughly 40 bytes"*; 1M is 40 MB | `adr/0005`, `03 §2.1` | bytes per Citizen across the saved tables | **S0a — already ran.** 85.98 MiB at 1M, ≈90 B/Citizen. `03 §2.1` admits the figure is stale; the ADR still states it. A `0012`-class correction rather than a session's |
| The event-driven Statistical tier *"costs about 1% of a core"* at 1M | `adr/0005` | Statistical-tier cost per Tick at a real Trip rate | `06` **5b**, then an S0b-class capture. **New §B row.** Not a row in [`0013`](0013-tick-budget.md) |
| Microscopic work is *"bounded by network stress, not by population"* | `adr/0007`, `adr/0016`, `03 §3.2` | Segments stressed simultaneously at 1M under a real draw | `06` **5b** + the trigger. **New §B row.** S2 R2's 2,592 of 33,018 over an 80% threshold is the only adjacent number, it is an **upper bound** (R2's uniform draw is the longest-trip distribution available), and it must not be quoted without that |
| *"Roughly 400,000 individually simulated cars on a single core"* — *"the number that makes microscopic traffic affordable at all"* | `adr/0016` | Vehicles updated per Tick per core in **our** structure and **our** integer arithmetic | **S5 — did not exist. Emitted by this pass** |
| O(n) car-following *"with the constant of a `memcpy`"*, and no spatial index | `adr/0016` | as above | **S5** |
| Promotion materialises Lane queues affordably | `adr/0016` | promotion cost against the cost of running the queues | **S5.** The ADR's own revisit trigger names the condition — *"promotion cost dominating the traffic budget"* — and names no machine |
| The three acceptance scenarios *"each fail loudly under an all-statistical run"* | `03 §5.1` | whether an all-statistical run passes any of the three | The three headless scenarios: specified, unbuilt. **D may argue the suite's admission rule; it may not declare the tier correct** |
| Trip object count *"roughly triples"* | `adr/0008` | mean Legs per Trip | `06` **5b**. **New §B row.** An input to the Trip table's sizing and to [`0013`](0013-tick-budget.md) both |
| Pedestrian networks *"do not saturate at this scale"* | `adr/0008`, `03 §3.7`, `CONTEXT.md` | peak pedestrian density per block face at 1M against the density at which walking speed falls | `06` **5b**. **New §B row.** Asserted in three places, measured in none, and it is what makes half the Legs in the city free |
| Parking scarcity *"converts a cliff into a gradient"* | `adr/0009` | walk-Leg length distribution as shed occupancy approaches 1 | `06` **5b** + parking. **New §B row** — with the shed **query** cost, which `adr/0009` pays on arrival and which R5.6 never measured (it measured invalidation) |

**Already typed correctly by their own text, and the pass confirms it rather than moving them:**
`adr/0012`'s *does the cache pay for itself* (machine named: 5b's hit rate); `03 §3.3`'s `T_high` and
`T_low` (*"measured, not chosen"*, with the sweep specified); `03 §3.9`'s three reopening measurements
(*"unknowable before there is a build"*). **Three of nineteen carried their own machine.**

### Arguable — this session may close them

The diversion policy (task 1) and what a failed rejoin does; route diversity (task 2); the Microscopic
Cap's **shape**, never its value (task 3); whether force-promotion survives on its second argument
alone (`03 §3.3`, `§3.8`, and `adr/0041`'s standing demand); the Sight Horizon being **two parameters**
— naming them, never valuing them; freight weighting and lane distribution; and the two the cluster
explicitly defends rather than reopens — `adr/0005`'s **decision** half and `adr/0012`'s core, both of
which name performance as an inadmissible ground for revisiting.

### Not a claim — the `§D3` bucket, and it caught four

Recorded because `0002` §D2's triage found the same thing about numbers: **five of eighteen were not
numbers at all**, and a list whose members are of different kinds stops being checkable.

- **`adr/0009`'s conserved-occupancy invariant** — *"needs an explicit invariant plus a headless test"*
  is **owed slice work**, not a claim to type. It is `adr/0006`-class and belongs to whichever slice
  builds parking.
- **`adr/0016`'s *"IDM parameters in the Ruleset"*** — a restatement of `CLAUDE.md`'s standing rule,
  which settles *where they live* and leaves them unset. → §D2.
- **`adr/0008`'s *"the road network needs a pedestrian layer"*** — a scope statement about work, and
  the ADR says so itself: *"real work, not a free consequence"*.
- **Two sets of hash-bearing numbers that were in no §D2 row at all** — the **Parking Shed's radius**
  and the **IDM's three tuning parameters**. Both now filed. **An unset number is a gap rather than a
  debt**, but a gap nobody has opened is neither, and that is the failure mode §D2 exists to prevent.

### Three findings that outrank the list

1. **`adr/0007` states invariant 6 inverted** — against `03 §4`, which records that binding it to
   Microscopic Segments *"inverted it"* and says why. → [`0012`](0012-corpus-audit.md).
2. **`adr/0007` still describes the `in_flight` counter `adr/0041` deleted**, unbannered, while the
   design section quoting the same sentence carries a banner. → [`0012`](0012-corpus-audit.md).
   Together these are *Cause 2* running **backwards**: the correction reached the design document and
   not the ADR that governs it, which is the direction the sweep never looked in.
3. **[`0013`](0013-tick-budget.md) has no row for the Microscopic tier at all** — not even *unbuilt*,
   which the Event Wheel and Commit both get. So the movement subsystem is priced in halves: routing
   carries **60–67 of the ledger's ≥114 points at 4×** and the Lane model carries **nothing**, on a
   number from somebody else's engine. This is what made S5 a spike rather than a note.

### The spike this pass emitted — **S5, the Lane kernel**

**A spike out of task 0 is the one permitted exception to *D does not produce more sessions*, and this
is `adr/0043` working rather than scope growing.** Registered in
[`spike-results`](../docs/spike-results.md) and in §B.

- **What it measures.** Vehicles updated per Tick per core under `adr/0016`'s structure — sorted 1-D
  queue, Overlaps exchanged once per Tick, IDM car-following — in **integer/Q16.16**, because
  `adr/0003` forbids the floats Citybound's 400,000 was measured with. Then the derived quantity that
  is the actual product: **how many Microscopic Segments fit in 15.6 ms**, plus the promotion and
  demotion cost of materialising a queue from in-flight Trips.
- **Its tripwire.** If that count lands below the stressed-Segment count a real city produces, the
  clause that fails is `adr/0007`'s *"it scales"* — not the Cap's value. `03 §3.9`'s *"reaching the
  Cap is not a failure mode"* is an argument about the **rare** case, and it does not survive the
  common one.
- **Why it is runnable now.** S4-class: `spikes/`, arithmetic substrate compiled in by source, no
  content, no Trip generation, no Ruleset. **It blocks on nothing and contends with nothing**, which
  makes it the rarest thing in the current corpus — the spike track has been down to R7's tail.
- **What it must not do.** Set the Cap. It supplies one side of a ratio whose other side is 5b's.

---

## Acceptance

- **Every claim in the cluster is typed**, and nothing that typed *measurable* was closed here.
- **`03 §5` is argued**, and the diversion policy exists as a design rather than as a principle.
- **Every number written down has a named ratifier and a revisit trigger**, or was not written down.
- **`0002` is updated in the same sitting** — closures struck in §A/§C, new numbers into §D with their
  ratifiers, and **§F's marks moved** for every document the session touched.
- The board's D row is struck and `0000` → *Do these next* re-derived.

## What this session deliberately does not do

- **It does not set the Microscopic Cap's value.** It needs the model this session designs.
- **It does not re-derive the path source or the invalidation contract.** `adr/0047` and `adr/0012`
  own them; session M ran.
- **It does not touch the playtest questions** `0002` names as design questions in disguise — car
  ownership especially, which is adjacent and is not arguable.
- **It does not produce more sessions.** This is the guard the board asks for by name: the last time the
  argument track led, *the design was generating design* — `adr/0046` alone spawned four unratified
  numbers. **D is booked to produce the traffic model as a design.** A spike out of task 0 is the one
  permitted exception, because that is `adr/0043` working rather than scope growing.

## Decisions owed, found while briefing

**1. Is `03 §2`, the Citizen model, in scope?** It is unargued, it is in the same document, and session
six closed its §2.1 sizing tension while **leaving the record itself unargued and its 40-byte figure
stale**. Coupled, because the Microscopic Cap binds far harder at 1M. *Recommendation: a separate
sitting, named now so it is scheduled rather than discovered.*

**2. One ADR or several?** `03 §5` is one document and at least four separable decisions — the
diversion policy, route diversity, the Cap's shape, and the fidelity boundary D shares with session E.
**An ADR per decision is the corpus's convention** and the reason `adr/0042` gives is that a series
whose every entry is load-bearing is what makes it worth reading.

**3. Does D or E own the fidelity boundary?** `adr/0005`/`0007` are session E's and the traffic model
sits on top of them. Whoever runs first inherits it; **say which in the sitting rather than after.**
