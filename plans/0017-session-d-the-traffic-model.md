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

**Briefed, not booked.** The board promoted D on 2026-08-10 and it has not been sat. Sessions do not
contend with the code track, so it may be booked whenever there is a sitting; **slice 8 is in flight
and does not block it**.

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

### 0. The typing pass — and it runs **before** anything is argued

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

### 1. The diversion policy, made a design

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

### 2. Decision 11 on the other axis — *the network runs out of routes, not road*

**R8's largest finding, and the one thing S2 explicitly could not close.** **87.25% of traffic on 1% of
the carriageway, 90.87% of it empty, at 13% of holding capacity with capacity confirmed realistic** —
because one free-flow tree per District means **one route per (node, District) pair in the whole
model.**

`adr/0047` has since deleted the District key. **So what supplies route *diversity*?** That is a design
question about the movement model, not an algorithm, and it is the question a player would notice
before any of the others: a city whose traffic uses 1% of its roads does not look like a city.

### 3. `03 §5` itself, and the Microscopic Cap's *shape*

The section is the most detailed unargued design in the project and now carries **transit vehicles**,
which arrived in session five and were never costed against it. Argue the model.

**The Cap's *value* is not available** — it needs a built traffic model, and S2 R2 informs it and
cannot set it. **Its *shape* is arguable and is `03 §3.9`'s**: what the Cap caps, and what happens at
it. Note `03 §3.9` already settled that **reaching the Cap is not a failure mode**.

### 4. The unset numbers, under `adr/0052`

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

### 5. `adr/0046`'s two open qualifications

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
