# 0000a — What the board used to carry

**This is not a source and not a record.** It is an index of rows that were once live on
[`0000`](0000-board.md) and are now closed, kept **one line each** so that a reader who remembers a row
can find where it went. **Every finding, number and argument belongs to the document in the *Owner*
column**, which holds the full version — this file deliberately holds none of them.

**Why it exists at all.** The board is a *view*, and a view that carries its own history stops being
scannable. It has now been cleared **three** times: the 999-line long-form board went on **2026-08-12**
(`git show db6f19f:plans/0000-board.md`), ~400 lines of closed-row narrative went on **2026-08-15**
(`git show 26eeaf8:plans/0000-board.md`), and the 925-line third inflation went on **2026-08-22**
(`git show ca91e86:plans/0000-board.md`). Both times the deleted text was a **second copy** — the eight
close-outs cut in 2026-08-15 were each verified against their owning plan before removal, and each owner
held the fuller version.

**The third clearing, 2026-08-22, had a different cause and a different repair.** The first two were
hand-clearings of narrative that had a home elsewhere. This one found that ~~the board was undisciplined~~
**a layer had no owner**: [`0003`](0003-build-plan.md) covered Phase 0 and Phase 1 and expressly declined
Phase 2, `06` is forbidden by its own header to hold live status, and so per-milestone status for eleven
shipped milestones had nowhere to live but here. It grew a 551-line *What is next* doing a ledger's job.
***A document that declines a layer does not thereby abolish it*** — so the repair was to give `0003` a
**Phase 2 ledger** first, and only then to cut. Cutting without it is what made the first two grow back
in four days and three.

⚠ **Do not quote this file.** A one-line summary is a caveat-free compression of somebody else's
sentence, which is `plans/0012` **Cause 5** by construction. Follow the link.

---

## Closed rows of *Do these next*

| Was | What it was | Closed | Owner |
|---|---|---|---|
| 1 | Slice 10 task 11 — `revisit_ticks`, a Zone Rule's sample derived from a duration | 2026-08-10 | [`0014`](0014-zone-rules-and-the-sweep-family.md), [`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) |
| 1b | Hash-moving queue item 3 — a Bin's capacity derives from the Ruleset in force, and a Bin holds a `long` | 2026-08-10 | [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md), [`0003`](0003-build-plan.md) → *The hash-moving queue* |
| 1c | Queue items 4 and 5 — declared occupancy, and placement as a mechanism of its own | 2026-08-11 | [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md), [`0003`](0003-build-plan.md) |
| 1d | Queue item 6 — `SyntheticCity` paves what it populates (`cd29b24`), which unblocked the map flip (`b891716`) | 2026-08-13 | [`0003`](0003-build-plan.md) → *The hash-moving queue*, [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) |
| 2 | S2 R7's tail — the second `performance` capture, the R0–R4 re-verification, the canonical R8 re-capture | 2026-08-11 | [`0010`](0010-s2-routing.md) → *R7* |
| 3 | Session **D** — `03 §5`, the traffic model. Five tasks, one sitting; emitted S5 | 2026-08-10 | [`0017`](0017-session-d-the-traffic-model.md) |
| 3 | Spike **S5** — the Lane kernel. Published 2026-08-11; **L6**, the 2- and 4-thread rungs, added 2026-08-14 (`1011c66`) | 2026-08-14 | [`0019`](0019-s5-lane-kernel.md), [`spike-results`](../docs/spike-results.md) → *S5* |
| 5 | Session **F** — `adr/0008`, walking is a simulated Leg. The last session that gated a slice | 2026-08-11 | [`0021`](0021-trips-legs-and-the-pedestrian-layer.md) → *What session F decided* |
| 6 | Milestone **5a-bis** — the Lot subdivider and the road editor, all seven tasks | 2026-08-11 | [`0022`](0022-the-lot-subdivider-and-build-road.md) |
| 7 | The Severance sweep — 240 `[roads]` configurations, run before 5b and inverting the claim 5b was to rest on | 2026-08-11 | [`0020`](0020-the-road-graph.md) → the 2026-08-11 amendment |
| 8 | Milestone **5b** task 5 — Phase 4 runs behind `CommandKind.Trip` (`3a6b3f7`) | 2026-08-12 | [`0021`](0021-trips-legs-and-the-pedestrian-layer.md) → *Task 5's findings* |
| 9 | Milestone **5b-bis** — jobs and the commute, all eight tasks (`d6809dd`) | 2026-08-13 | [`0023`](0023-jobs-and-the-commute.md) → *The record* |
| 10 | Milestone **5c** tasks 1–7 — the partition, the matrix, the path source, the route cache, the vehicular Leg, the volume-delay function, `--traffic` | 2026-08-14 | [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md) → *The record* |
| 11 | Milestone **5c** task 8 — the long acceptance run, which closed the milestone | 2026-08-16 | [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md) → *Task 8* |
| 12 | [`0003`](0003-build-plan.md) hash-moving queue **item 9** — the populator's land half and people half come apart, so a `Connect`-laid network can hold a population | 2026-08-15 | [`0003`](0003-build-plan.md) → *The hash-moving queue* |
| 13 | Argument — `04 §6` and `§7`, then the corpus-wide sweep for unscheduled mechanisms | 2026-08-15 | [`adr/0102`](../docs/adr/0102-a-housed-departure-is-a-comparison-the-household-re-runs-not-a-threshold-it-crosses.md)–[`adr/0104`](../docs/adr/0104-a-skill-tier-is-earned-by-attendance-and-the-credential-stays-a-wall.md), [`06`](../docs/06-roadmap.md) → *Mechanisms with no milestone* |
| 1b | Milestone **8** — Save/load, all ten tasks, and the two questions it added closed with the user in the room | 2026-08-18 | [`0030`](0030-save-load.md), [`adr/0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md), [`adr/0112`](../docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md) |
| 1b | Milestone **10** — Conserved Money and the treasury, every task, and the two decisions it filed to §D closed without either producing a number | 2026-08-19 | [`0033`](0033-conserved-money-and-the-treasury.md), [`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)–[`adr/0118`](../docs/adr/0118-govern-fits-the-four-fields-and-the-hard-part-is-that-it-writes-to-the-ruleset-rather-than-to-the-world.md) |
| 1 | Milestone **7** — Parking, all eight tasks, all four decisions settled with the user in the room before task 1 | 2026-08-19 | [`0031`](0031-parking.md), [`adr/0119`](../docs/adr/0119-a-parking-space-is-held-by-the-citizen-and-a-household-holds-as-many-cars-as-it-has-drivers.md), [`adr/0120`](../docs/adr/0120-a-car-park-is-not-a-bin-and-supply-is-at-buildings-until-a-segment-needs-one.md) |
| 1 | Milestone **9** — the land value target and the composed Layers, all eight tasks and all six decisions, and the ninth Ruleset it had to author before its own acceptance run could read anything | 2026-08-20 | [`0034`](0034-the-land-value-target-and-the-composed-layers.md), [`adr/0122`](../docs/adr/0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md)–[`adr/0127`](../docs/adr/0127-the-land-value-target-never-stops-moving-so-the-question-is-what-the-lag-rests-around.md) |
| 6 | [`0032`](0032-test-tiers.md) — test tiers. Proposed and built the same day; the assertion tier is the commit gate and the whole suite is still the milestone gate | 2026-08-19 | [`0032`](0032-test-tiers.md), [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md) |
| 1 | Milestone **11** — Hinterlands and arrival through the gate, all nine tasks and all ten decisions, plus an eleventh found by task 1 after the sitting recorded all ten closed. Its gate discharged by the unfiltered suite on the reference machine | 2026-08-21 | [`0035`](0035-hinterlands-and-arrival-through-the-gate.md), [`adr/0128`](../docs/adr/0128-the-gate-ships-before-the-comparison-that-walks-through-it.md)–[`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md) |
| 5 | Hash-moving queue **item 11** — the waiter parked on a Bin the same Tick had already refilled. Diagnosed 2026-08-21, fix held on a shared-artefact collision until milestone 11 closed, fixed 2026-08-22. Its open half was *which repair*, and the corpus closed it rather than a sitting | 2026-08-22 | [`0003`](0003-build-plan.md) → *The hash-moving queue* item 11 |
| 5 | Hash-moving queue **item 14** — `World.Drain` and `Invariant.WaiterIsBlockedByTheBinItNames` contradicting each other. Settled by narrowing the invariant to the head of the wait list, on `adr/0063`'s own argument; the narrowing exposed a second half — a woken waiter's claim — that no reasoning about queue order reaches | 2026-08-22 | [`0003`](0003-build-plan.md) → *The hash-moving queue* item 14, [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md) |
| 4 | Hash-moving queue **item 8** — the waiter whose **own** requirement falls and is never re-checked, filed 2026-08-13 with a reproduction and a reserved design question. Closed with no simulation code written: it was item 11, and neither candidate repair was needed | 2026-08-22 | [`0003`](0003-build-plan.md) → *The hash-moving queue* item 8 |
| 1 | Milestone **12** — Goods between Buildings, the District Pool. **Capped at task 6 and closed there**, its risk rewritten to what tasks 1–6 retire, because `Scope.Pool` still throws; tasks 7–10 and the original risk moved to milestone **26** | 2026-08-22 | [`0037`](0037-goods-between-buildings-the-district-pool.md), [`adr/0132`](../docs/adr/0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md)–[`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md) |
| 1 | Milestone **25** — the Business is the actor and the Building is premises. **Capped at group A by decomposition before a line of code was written**, its risk rewritten to what group A retires; tasks 6–9 became milestone **27** | 2026-08-23 | [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md), [`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md), [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md)–[`adr/0144`](../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md) |
| 1 | Milestone **27** — the Business is a thing the city contains, all five tasks, and milestone 25's **original** risk retired: the city creates and capitalises an economic actor. Its task 10 long run found a razing that identified a Building's own trade **by kind** | 2026-08-24 | [`0041`](0041-the-business-is-a-thing-the-city-contains.md), [`adr/0145`](../docs/adr/0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md)–[`adr/0149`](../docs/adr/0149-a-business-is-a-population-a-policy-sweeps-and-a-readout-names-every-entity-it-reads-against.md) |

## Closed prose that sat beneath that table

Each of these was several paragraphs on the board narrating something already shipped.

| What it narrated | Closed | Owner |
|---|---|---|
| Queue item 7, clock half — `Ticks.PerDay` and `EventWheel.Size` go to **2048** (`ce7686e`), and the ADR's own rescaling inventory being wrong in two places | 2026-08-13 | [`0003`](0003-build-plan.md), [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md) → *What building it found* |
| Queue item 7, Goods half — the ×4 rescale was **two** rescales (`85c314a`), and the commit's own larder claim was withdrawn by measurement as behaviour-neutral (`378dc1b`) | 2026-08-13 | [`0003`](0003-build-plan.md) |
| Queue item 8 — a waiter whose **own** requirement falls is never re-checked. ~~⚠ **Filed unfixed and still open**; its two candidate repairs are a design question~~ — ✅ **closed**, and **neither repair was needed**: the reproduction stopped reproducing because item 11's fix had already closed it | 2026-08-22 | [`0003`](0003-build-plan.md) → *The hash-moving queue* item 8 |
| `adr/0095`'s three Commute Budget rungs, built (`3b93ee1`) — and the finding that the ADR designed against one of `01 §4`'s two drivers | 2026-08-13 | [`adr/0095`](../docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md), `CLAUDE.md` → *Constants* |
| The "picking this up cold on or after 2026-08-11" box — 5b's two blocked tasks, the §A and §B rows, the `Space/Address.cs` merge, and the *how long is a Tick* question that became [`adr/0082`](../docs/adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md) | 2026-08-12 | [`0021`](0021-trips-legs-and-the-pedestrian-layer.md) |

## Closed rows of *Open tracks — the argument track*

| Was | Session | Closed | Owner |
|---|---|---|---|
| D | `03 §5` — the traffic model | 2026-08-10 | [`0017`](0017-session-d-the-traffic-model.md) |
| F | `adr/0008` — walking is a simulated Leg | 2026-08-11 | [`0021`](0021-trips-legs-and-the-pedestrian-layer.md) |
| H | `adr/0009` — parking is modelled supply. Cleared milestone 7, and discharged **M**'s remainder as a side effect | 2026-08-12 | [`adr/0083`](../docs/adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md), [`adr/0084`](../docs/adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md) |
| I | `adr/0012` — routing intent lives in the agent. ⚠ **Never needed running**: the amendment was already in the file, and this row held milestone 5c for two days | struck 2026-08-13 | [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) lines 47–63 |
| J | `05 §7`, map size, Outside Connection layout. Cleared milestone 8, then re-opened its own map half into [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) | 2026-08-12 | [`0024`](0024-session-j-the-save-the-map-and-the-outside.md) |
| M | The route cache's invalidation contract — the shed half | 2026-08-12 | [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md), [`adr/0083`](../docs/adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md) |
| P | The player model — `01 §1`, `§3`, `§4`. Twenty-four decisions, seven ADRs | 2026-08-13 | [`0025`](0025-the-player-model.md) |
| Q | The reach-failure memory, and the gate audit that ran first | 2026-08-13 | [`adr/0097`](../docs/adr/0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md) |
| U | The Pool or the seller — `adr/0013` reopened on custody, and stock left the Pool | 2026-08-22 | [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) |

## The *Blocked* blanket row, and why it is worth remembering

`Phase 2 milestones 5a–10` was **one row over milestones with different gates**. It was narrowed five
times — 2026-08-11, twice on 2026-08-12, 2026-08-13 and 2026-08-14 — each narrowing correct, and each
leaving intact the structure that produced the next one. On the fifth it was split per-milestone, which
its own text had been prescribing since the second. On 2026-08-14 the sweep then found that the split
had **dropped 7b, 9a and 9b entirely**.

Two rules came out of it and both are live on the board: ***a blanket row is a status whose granularity
is coarser than the claims it covers***, and ***a per-milestone table that omits the cleared ones is how
the missing ones stay missing***.

## Closed rows of *The argument track*

| Was | What it was | Closed | Owner |
|---|---|---|---|
| **E** | `adr/0005` + `adr/0007` — fidelity; all four questions answered | 2026-08-16 | [`0029`](0029-session-e-fidelity.md) |
| **K** | `06`'s Phase 2 ordering, re-derived — 5a–5c frozen, then 6–24 | 2026-08-16 | [`06`](../docs/06-roadmap.md), [`PROCESS.md`](../PROCESS.md) → *Numbering* |
| **T** | The target speed — opened and closed the same day | 2026-08-16 | [`0027`](0027-session-t-the-target-speed.md), [`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md) |
| **V** | The Business is the actor and the Building is premises — four of five questions closed; the fifth changed type on the way and left for `0002` §D2 | 2026-08-22 | [`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md), [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md), [`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md) |

## Closed rows of *Two audits the corpus assigned itself*

| Was | What it was | Closed | Owner |
|---|---|---|---|
| 2 | Re-check every 🔴-blocked debt for a gate whose stated reason covers only part of what it blocks — three clean positives, one partial, and a second failure mode found: stale gates whose stated reason had gone false | 2026-08-14 | [`0012`](0012-corpus-audit.md), [`0003`](0003-build-plan.md) |
