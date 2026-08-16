# 0000a — What the board used to carry

**This is not a source and not a record.** It is an index of rows that were once live on
[`0000`](0000-board.md) and are now closed, kept **one line each** so that a reader who remembers a row
can find where it went. **Every finding, number and argument belongs to the document in the *Owner*
column**, which holds the full version — this file deliberately holds none of them.

**Why it exists at all.** The board is a *view*, and a view that carries its own history stops being
scannable. It has now been cleared twice: the 999-line long-form board went on **2026-08-12**
(`git show db6f19f:plans/0000-board.md`), and ~400 lines of closed-row narrative went on **2026-08-15**
(`git show 26eeaf8:plans/0000-board.md`). Both times the deleted text was a **second copy** — the eight
close-outs cut in 2026-08-15 were each verified against their owning plan before removal, and each owner
held the fuller version.

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

## Closed prose that sat beneath that table

Each of these was several paragraphs on the board narrating something already shipped.

| What it narrated | Closed | Owner |
|---|---|---|
| Queue item 7, clock half — `Ticks.PerDay` and `EventWheel.Size` go to **2048** (`ce7686e`), and the ADR's own rescaling inventory being wrong in two places | 2026-08-13 | [`0003`](0003-build-plan.md), [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md) → *What building it found* |
| Queue item 7, Goods half — the ×4 rescale was **two** rescales (`85c314a`), and the commit's own larder claim was withdrawn by measurement as behaviour-neutral (`378dc1b`) | 2026-08-13 | [`0003`](0003-build-plan.md) |
| Queue item 8 — a waiter whose **own** requirement falls is never re-checked. ⚠ **Filed unfixed and still open**; its two candidate repairs are a design question | — | [`0003`](0003-build-plan.md) → *The hash-moving queue*, item 8 |
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

## The *Blocked* blanket row, and why it is worth remembering

`Phase 2 milestones 5a–10` was **one row over milestones with different gates**. It was narrowed five
times — 2026-08-11, twice on 2026-08-12, 2026-08-13 and 2026-08-14 — each narrowing correct, and each
leaving intact the structure that produced the next one. On the fifth it was split per-milestone, which
its own text had been prescribing since the second. On 2026-08-14 the sweep then found that the split
had **dropped 7b, 9a and 9b entirely**.

Two rules came out of it and both are live on the board: ***a blanket row is a status whose granularity
is coarser than the claims it covers***, and ***a per-milestone table that omits the cleared ones is how
the missing ones stay missing***.
