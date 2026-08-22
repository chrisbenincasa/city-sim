# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done, and the one
place that orders the three tracks against each other.

---

## What is next

**The next code row is [`06`](../docs/06-roadmap.md) milestone **12** — Goods between Buildings, the
District Pool.** Ungated, scoping under way in
[`0037`](0037-goods-between-buildings-the-district-pool.md), **tasks 1 through 6 shipped 2026-08-22**. Decisions **1, 2, 4, 5, 6,
8 and 9 are settled** ([`adr/0132`](../docs/adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md)–[`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md));
**open: 3 and 10**, and **`0037` owns both**.

✅ **DECOMPOSED 2026-08-22 — ten tasks, tasks 1 through 6 shipped.** **3 is an obligation, not a fork** — `adr/0052`
requires a ratifier be *named*, not that the number be settled. **7 is largely pre-answered** by
`adr/0134`. 🔴 **10 is new and decomposition found it**: the Pool is the counterparty on **both** sides
of a trade and the two sides happen at different Ticks, so *where the money sits between a Provider's
deposit and a consumer's draw* is unanswered by `adr/0050`, `adr/0135` and `adr/0114` alike. ***Ordering
the work asked what each task needed and found a question seven decisions had not.***

⚠ **Two more things decomposition turned up, and both are the reason to do it before starting.**
🔴 **Task 5 found a defect it does not own and routed it rather than fixing it:
[`0003`](0003-build-plan.md) queue item 15** — a Ruleset edit that **inserts** a `[[resource]]` crashes
the swap, on the **treasury**, on `rulesets/minimal.toml`, with no District anywhere. `RulesetMigration`
maps Resources by name and `World.Migrate` applies that map to **Building Bins only**. ***The migration
is right and its reach is short.***

✅ **Task 5's blocker — [`0003`](0003-build-plan.md) queue item 14 — was settled 2026-08-22 and the row
has left the board**: the invariant narrowed to the head of the wait list, on `adr/0063`'s own argument
rather than on the cheaper repair, and the narrowing turned up a second half nobody had reasoned to — a
woken waiter records no claim, so the drain's guarantee is true of an *instant*. **Moves no hash.**
⚠ ***Decomposition found it and decomposition is what unblocked it***, a week before `Scope.Pool` would
have. And a **fourth precondition** still stands: `BinOwnerKind` has four members, none is a District,
and `BinTable.Owner` is a `HandleColumn<Building>`.

✅ **Task 1 was a WORLD and not code, and it shipped 2026-08-22** — `rulesets/twinned.toml` and the
`[[lattice]]` key, two lattices **joined by a Street corridor** so that only the density field can split
them. ***That is milestone 11 task 3's lesson arriving before the milestone instead of during it.***
⚠ **The key is not spelled `settlement`** and `CONTEXT.md` → Lattice says why. ✅ **Task 2 found its field already built** — `BuildingResidency`, 5b-bis — so it shipped a name and a measurement.

⚠ **Two things a reader of this row needs and will not guess.** The survey found **three preconditions
no document had listed as blockers** — the largest being that ***there is no District in the build at
all***. And **Upkeep is no longer part of it**
([`adr/0136`](../docs/adr/0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md),
2026-08-22), and **neither is freight nor `adr/0088`'s `min()`**
([`adr/0138`](../docs/adr/0138-freight-is-unbuilt-so-the-min-follows-it-and-neither-is-at-twelve.md),
same day) — 🔴 ***three mechanisms found parked at 12 on an assumption their authors did not check***,
each placed by a document that was not scoping the milestone. **`06`'s rows for all three are now
UNPLACED.** ⚠ **The consequence to carry forward: 12 makes import real as a PRICE and not as traffic**,
so a distant gate costs nothing until freight lands and `adr/0088`'s thesis is deliberately inert. ***A milestone whose named risk is a single `throw` reads as a milestone with a single
obstacle***, and the `throw` is the symptom.

**Status for every other milestone is [`0003`](0003-build-plan.md)'s Phase 2 ledger.** This section
carried it until 2026-08-22 and had grown to **551 lines**; see *How to read this file*.

---

## How to read this file

**This is a view, not a source, and it owns nothing.** Three documents answer three questions:
***why in this order*** is [`06`](../docs/06-roadmap.md)'s; ***what is done and what gates it*** is
[`0003`](0003-build-plan.md)'s — both ledgers and both gate boards, Phase 0 through Phase 2;
***what is next*** is this file's, and it is nothing but an index over the other two.
[`0002`](0002-open-questions.md) owns **every open question** and the **§F coverage map**;
`docs/adr/` the decisions; [`0013`](0013-tick-budget.md) what a Tick costs;
[`0012`](0012-corpus-audit.md) what a document says wrongly. **When they disagree, they win.**

**Three rules keep it a view rather than a second ledger.** ⚠ **All three were broken by 2026-08-22.**

1. **Do not write an open question here** — that is how it once held 63 while the file named *open
   questions* held none.
2. **A cell is at most three sentences.** One had reached 15 sentences and 3,986 characters.
3. **A closed row leaves**, to [`0000a`](0000a-board-archive.md), one line each.

✅ **All three are enforced mechanically as of 2026-08-22** — `BoardShapeTests`, in the assertion tier,
so a breach fails the commit gate rather than waiting for somebody to notice. A fourth check caps the
whole file. ⚠ **They catch the symptom and not the cause**: when one fires, the fix is not to delete
lines but to find ***the document that should have held them***.

⚠ **Cleared three times — 2026-08-12, 2026-08-15 and 2026-08-22 — and the third had a different
cause.** The first two were hand-clearings and both grew back within days. The third found that
`0003` covered only Phase 0 and Phase 1, so per-milestone status for eleven shipped Phase 2 milestones
had nowhere to live and this file grew a **551-line** *What is next* doing a ledger's job.
***A document that declines a layer does not thereby abolish it***, so the repair was to give `0003` a
Phase 2 ledger **first**. [`0000a`](0000a-board-archive.md) holds the recovery pointers.

> ⚠ **This file is the document most likely to be read instead of the build.** On 2026-08-13 a sitting
> read a paragraph here to answer *what is next* and reported work that had shipped an hour earlier in
> the same tree. [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
> applies to the board itself.

---

## State of play

**What the project is.** A city-builder whose simulation is an ordinary C# library with no game engine
inside it. Godot will be the display layer and has not been started.

**Where it is.** **Phase 2, between milestones.** Phase 1 is closed; Phase 2 has shipped **5a through
11** and **12** is the live row; the hash-moving queue is open on items **8, 10, 12, 13 and 14**.
⚠ **Enumerated in [`0003`](0003-build-plan.md), never totalled here** — a total in prose is a fact
that drifts.

**What works.** Typed tables with every field declared once, integer-only arithmetic, a deterministic
eight-phase Tick, replay and save/load that both recompute their own hash, Map Layers with diffusion,
two Rule families, a Road Graph with Lots and Access Points, and a movement stack through to a
vehicular Leg and a volume-delay function. **Runnable commands are [`CLAUDE.md`](../CLAUDE.md)'s.**

**What does not exist.** Two of the eight Tick phases are empty. There is **no supply chain** — that
crossing is the District Pool, the named hole that throws — and **no land use**, so every Building has
the same occupants and posts.

**Known problems, none urgent, none owned here.** Routing does not fit the Tick budget
([`0013`](0013-tick-budget.md)); the network runs out of routes rather than road
([`0002`](0002-open-questions.md) §C); the job-search box does not filter and cannot in a foot-only
world ([`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)); the
synthetic fixture and `World`'s table sizing disagree with nothing checking; and **every S2 and S0a
absolute is `powersave`, mis-pinned, or both**
([`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md)).

⚠ **Two things no shipped Ruleset can demonstrate**, both measured: `minimal.toml` severs **0.0%** of
pedestrians at every dial value, and **a generated city cannot congest itself** — `v/c` peaks at 0.44
at every population. **Read a Ruleset's own header before quoting anything out of it.**

### The five numbers to hold in your head

**[`0013`](0013-tick-budget.md) owns the bill and [`spike-results`](../docs/spike-results.md) owns the
captures.** These five are here because no single document holds all five, and a reader needs them
together. ⚠ ***Quote the sentence, never the digits.***

| | Number | What it means |
|---|---|---|
| **The good one** | **8.72 ms a Tick at 1M — 55.9% of the budget at 4×** (S0b) | The **only** Tick figure ever taken from a real running city |
| **The sum** | [`0013`](0013-tick-budget.md) reads **≥44–50 ms a Tick** | ⚠ ***Carry the bill, not the percentage.*** Against a settled 15.6 ms at 4× on one core, the gap is **~3×** |
| **The row that decides it** | **Routing is 37.6–42.0 ms of those 44–50 — 85% of the bill** | Its unit came off a synthetic harness and its multiplicand counts the wrong event |
| **The correction with a known direction** | A diverting Traveller re-searching costs **134.135 ms a Tick** at target scale | Points **up**, and the cache cannot rescue it. **Answered rather than reduced** by [`adr/0061`](../docs/adr/0061-a-diversion-rejoins-by-local-descent-and-a-rejoin-is-never-a-search.md) |
| **Scale** | 1M Citizens in **86 MiB** of tables; 100,000 Ticks in 11.75 s | Sizing risk retired. **One State Hash is 32.47 ms — 2.08 Tick budgets** |

**The meta-figure, and it is the one to be uneasy about.** *Every* time a fixture has been replaced by
a real world, the number came in **worse**. [`0013`](0013-tick-budget.md) states the general form —
***a unit cost is a hypothesis until a real world has produced one*** — and routing's has never met a
world. The enumeration and its one counterexample are
[`0019`](0019-s5-lane-kernel.md):348 and [`0013`](0013-tick-budget.md):594–608.

---

## Do these next

**Build. The argument track is not the constraint**, and treating it as one is how this project starts
going in circles. **The standing rule: an argument session runs when something concrete is blocked on
it, and never because it is available.**

⚠ **The three tracks contend — for conclusions, for cores, and for names.** A capture whose subject is
parallelism names *nothing else running in this repository* as its first control.
[`0012`](0012-corpus-audit.md) holds all three sightings.

| | Track | Task | Plan | Why this one |
|---|---|---|---|---|
| **1** | code | **Milestone 12 — the District Pool.** Decomposed into ten tasks; **1 through 6 shipped 2026-08-22** — the two-centre world, the density field, the watershed, re-evaluation, the Pool Bins, and the price. **Task 7 is next: the purchase, and `Scope.Pool` stops throwing**, 🔴 **blocked on decision 10** | [`0037`](0037-goods-between-buildings-the-district-pool.md) | The ungated row at the head of the sequence, and **the only root with a consumer already in the build** |
| **2** | spike | ⚠ **Do NOT delete `spikes/S2.Routing/`.** The 5a gate is discharged, but another session is doing research inside it, so it is live work. 51 tracked C# files, 29,719 lines | [`0010`](0010-s2-routing.md) → *R7* | ⚠ ***A deletion held twice for unrelated reasons is the row that gets struck when the wrong one clears*** |
| **3** | spike | **S5 owes two captures** — the 4-thread Lane kernel rung, which is bimodal, and the canonical `performance` re-capture. 2 threads is settled at 1.84–1.93× | [`0019`](0019-s5-lane-kernel.md) | ⚠ **Quote the supply-side multiple as *at least 1.84× and plausibly near 4×*, never as 4× bare** |
| **4** | code | **Hash-moving queue item 8** — a waiter whose own requirement falls is never re-checked. Filed unfixed | [`0003`](0003-build-plan.md) → *queue* | ***A live predicate with an event-driven trigger is only correct if every input to the predicate is an input to the trigger.*** Both repairs are design questions |
| **5** | tidy | **Delete `spikes/S4.Kernels/`** — S4 task 11, open since the spike closed, gated on nothing | [`0004`](0004-s4-kernel-benchmark.md) | A deletion that size is taken deliberately, not as a consequence of a green suite |

**Closed rows are in [`0000a`](0000a-board-archive.md)**, one line each with the document that owns the
record. **The argument track has no promoted row.**

---

## Done

**[`0003`](0003-build-plan.md) owns both ledgers and this file keeps no copy.** Phase 0 and Phase 1 are
its *slice ledger*, slices 0–10; Phase 2 is its *Phase 2 ledger*, keyed by `06`'s milestone number.
Each row there names the gate, links the plan document that owns the tasks and findings, and states
what is done.

⚠ **A copy of that table lived here until 2026-08-22 and had already drifted** — it stopped at
milestone 10 while 11 and 12 stood. ***A view that keeps its own copy of the source is no longer a
view.***

### Spikes

**[`spike-results`](../docs/spike-results.md) owns every number and [`0003`](0003-build-plan.md)'s
spike table owns the gates.** S4, S0a, S0b, S2 and S5 have all run; **S1 and S3 have not** — Track B,
Godot only, ungated, and the empirical inputs to session **L**.

⚠ **One caveat travels with every S2 figure**: R1–R5 ran on a **frozen cost basis**, so quote nothing
from them as a statement about a congested city ([`0010`](0010-s2-routing.md):1016).

---

### Sessions

**An index, not a record** — every finding belongs to the linked document. Kept because sessions are a
board-tracked axis and nothing else lists them in one place.
[`PROCESS.md`](../PROCESS.md) → *Numbering* owns the lettering.

**Closed:** A, B, C, D, E, F, H, J, K, M, P, Q, T, *eight*, *nine* — records in
[`0017`](0017-session-d-the-traffic-model.md), [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md),
[`0024`](0024-session-j-the-save-the-map-and-the-outside.md), [`0025`](0025-the-player-model.md),
[`0027`](0027-session-t-the-target-speed.md), [`0029`](0029-session-e-fidelity.md) and the ADRs each
produced. **Open:** N, and what is open is task 5's residue. **Never opened:** G, R, L.

---

## Open tracks

### The argument track — a menu, not a queue

**Nothing in it gates a slice.** Take from it when something concrete is waiting, and leave it alone
otherwise. **Closed sessions are in [`0000a`](0000a-board-archive.md).**

| | Session | What is missing | Unblocks |
|---|---|---|---|
| **G** | `adr/0016` — the lane is the entity | Carries the order-of-magnitude claim the whole microscopic tier rests on. ⚠ **Partly discharged by S5** | milestone **21** |
| **R** | `05 §6`'s threading policy | The obligation `06` could not give a milestone | lint 4 |
| **L** | **A presentation design** | **It does not exist.** Every other phase is backed by a design document; rendering has none | **Phase 3** |

### Not arguable, and the audit still owed

The **Microscopic Cap** needs a built traffic model and **S2** is measurement — argument closes
neither. [`0002`](0002-open-questions.md) names a set of **playtest questions wearing design-question
clothing** this track must not drift into; ⚠ **session P grilled all three `01` sections and left that
set intact**, because ***an examined section is not thereby a settled one.***

⚠ **OPEN — type every claim `arguable` or `measurable`**
([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)),
**across every section rather than only the ungrilled ones**: two claims measured false sat in 🟢 rows
of the coverage map. **Read the suspect marks from [`0002`](0002-open-questions.md) §F**, which owns
them — an enumeration here is how the last one went stale.

---

## Owed — documentation debt, none of it blocking

**Not held here.** [`0002`](0002-open-questions.md) owns every corpus debt as an open item, in fuller
form than a board cell can carry — the stale `05` figures, the `05 §3` and `03 §3.8` corrections, the
`06` spike specifications, and the **33 occurrences across 22 files** of a term `CONTEXT.md` bans
outright. [`0012`](0012-corpus-audit.md) owns what a document says *wrongly*, with its Causes and its
mechanical checks. ⚠ **A debt in two ledgers is the defect `0012` exists to diagnose**, which is why a
copy stopped living here on 2026-08-22.

---

## Blocked

**There is no red gate anywhere in the corpus.** [`0003`](0003-build-plan.md) owns both gate boards —
Phase 1's is its *gate board*, Phase 2's is the *Gate* column of its Phase 2 ledger.

| | Blocked on | Which is |
|---|---|---|
| **Phase 3** | 🔴 **a presentation design that does not exist** | session **L**. ⚠ **S1 and S3 are themselves ungated**, so the head of the chain is runnable; what stops it is that **Track B has never been stood up**. **The chain is S1 + S3 → L → Phase 3 is plannable** |

**Phase 3 is undesigned, not unplanned**, and the distinction describes an absence rather than a
choice.
