# 0000 — The board

**Read this first.** A flat, scannable status of everything planned and everything done.
It is a *view*, not a source: [`0003`](0003-build-plan.md) owns the slice order and its gates,
[`0002`](0002-open-questions.md) owns the reasoning, `docs/adr/` owns the decisions. When they
disagree, they win. Update this file whenever a task lands.

**Where the project is:** Phase 1, **slice 5 closed** — all eight tasks, less task 7's trend assertion,
which was deliberately not written. The State Hash has a committed baseline under it,
`Borough.Headless` replays a `.borough` log and prints a diffable hash trace, the three invariant
tiers run in release on every Tick and at the end of every run, `--census` prints what every
collection did over a run, and a panic writes a crash artifact that replays back into the same panic.
**Slice 6, Map Layers, is the last slice before the Phase 1 gate closes.**

---

## Do these next

| | Task | Where | Why this one |
|---|---|---|---|
| **1** | **S2 R0 — the synthetic Road Graph and the denominator** | [`0010`](0010-s2-routing.md) | Parallel track, blocks nothing and is blocked by nothing. The project's **top risk**, and the best-specified work in the repository. The net it wanted under it now exists |
| **2** | **Slice 6 — Map Layers** | [`0009`](0009-map-layers.md) | The next slice in `0003`'s order, and the first one whose cadence is a hash-changing decision the corpus records as owed |

*Why S2 first now:* the argument for delaying it was that the golden baseline should exist before
throwaway spike code starts changing `Core`. It does, and the runner is what a person uses to look at
what moved. The remaining slice-5 tasks are no longer in front of it.

---

## Done

### Phase 0 / Phase 1 slices

- [x] **Slice 0 — solution scaffolding.** Four projects, build config, the three reflection guards, CI
- [x] **Slice 1 — S4, the kernel benchmark.** Tasks 1–10, all seven kernels on two machines, **no
      tripwire row fired**. Results in [`spike-results`](../docs/spike-results.md)
  - [ ] *task 11 — delete `spikes/S4.Kernels/`.* Held pending the XMP re-sweep, which is now **optional**
- [x] **Slice 2 — the arithmetic substrate.** All 7 tasks. Typed quantities, fixed point, tabulated
      `exp`/`log`, `draw()`, purpose tags → produced `adr/0038` and an amendment to `adr/0003`
- [x] **Slice 3 — the analysers.** All 6 tasks. Twelve diagnostics covering CI lints 2, 3 and 7 and the
      `purpose_tag` row → produced the rule-7 exception axis in `adr/0036`
- [x] **Slice 4 — typed tables and the field declaration.** All 11 tasks. Handles, columns, the single
      declaration, the State Hash, intrusive lists, `ResourceMap`, the first four tables → produced
      `BOR0901` and the project's first State Hash
- [x] **Slice 5 task 1** — `step(inputs)` and the eight-phase skeleton
- [x] **Slice 5 task 2** — the command model and the Input Log *(less the text codec)*
- [x] **Slice 5 task 3** — replay
- [x] **Slice 5 task 4** — the golden-hash baseline. A committed session trace *and* a committed world
      hash, because the session reaches one table in four until the player has verbs; the re-baselining
      procedure sits beside them
- [x] **Slice 5 task 5** — the headless runner, and **`Borough.Formats`, the fifth project**
      (`adr/0039`): the `.borough` codec, the hash-trace format the runner and the baseline share, and
      the Ruleset content hash. `--strict` inverted to a default refusal with `--force-ruleset` as the
      escape, per `05 §7`. `series(metric, window)` deferred to task 7, where the census gives it a
      second caller
- [x] **Slice 5 task 6** — the invariant tiers. Per-Tick at the write site, staggered by slice, and
      the whole-world walks at the end of every headless run. Throws by default so task 8 can catch at
      the Tick boundary; `Collect` is the switch for a balance run
- [x] **The tiers, costed.** A BenchmarkDotNet job in `Borough.Tests` against a constructed city:
      the staggered tier is **0.06% of a Tick at 100k Citizens**, and a full sweep of every row costs
      **a fifth of one State Hash**. `adr/0033`'s *unaffordable per Tick, trivial at the end of a run*
      is worth three orders of magnitude here. Numbers in [`0008`](0008-tick-and-replay.md)
- [x] **Slice 5 task 7 — the instrument, not the assertion.** The **Census** (`CONTEXT.md`), the
      `series(metric, window)` cold API deferred from task 5, and the runner's `--census` report.
      Three counters per table, because *slots climbing while live is flat* is the leak a row count
      cannot show. The ring is finite by construction — a census that grew with elapsed game time
      would be `adr/0006` in the instrument written to catch it — and an outrun window is **marked**
      incomplete rather than silently shortened
  - [ ] *the trend assertion.* Deliberately not written; see *Owed* below
- [x] **Slice 5 task 8 — the crash artifact.** `05 §8`'s reproduction rather than a dump: the log
      wrapped verbatim, the Tick that panicked, and the Ruleset actually in force. The runner takes
      an artifact wherever it takes a log, so **the loop closes** — one fed back panics at the same
      Tick and emits an identical file. `from` is the checkpoint-shaped field, zero until milestone
      10, and a reader that meets a non-zero one **refuses** rather than replaying a different city
- [x] **Slice 5 closed.** All eight tasks, less task 7's assertion

### Planning and design

- [x] **S2 planned** — [`0010`](0010-s2-routing.md), and its gate cleared by defining **Segment** in
      `CONTEXT.md`
- [x] **S2 plan grilled before any code.** Thirteen findings; see *Owed* below for what it left behind
- [x] **`adr/0040`** — the pathfinding cluster is a multiple of the Chunk, not the Chunk
- [x] **`adr/0041`** — volume is attributed by the Traveller, not the District pair

---

## Unblocked, in order

### Main track

- [ ] Slice 5 task 7 — the long-run test *(carries `series(metric, window)`)*
- [ ] Slice 5 task 8 — the crash artifact
- [ ] **Slice 6 — Map Layers** — [`0009`](0009-map-layers.md). Gate cleared
- [ ] *the Phase 1 gate closes here*

### Parallel track — S2, routing ([`0010`](0010-s2-routing.md))

- [ ] R0 — the synthetic Road Graph, and the denominator
- [ ] R1 — the travel-time matrix *(the prescribed first measurement)*
- [ ] R2 — searched against looked-up path, and the crossover *(attribution half now settled by `adr/0041`)*
- [ ] R3 — HPA\*, and the cluster size it owns
- [ ] R4 — DSDV distance-vector *(conditional on R2)*
- [ ] R5 — the edit storm, and the Epoch ladder
- [ ] R6 — the two caches, and `adr/0006`
- [ ] R7 — the report, the verdict, and deleting the harness

### Parallel track — Godot (Track B, no gate)

- [ ] **S1** — 20k Buildings via chunked `MultiMeshInstance3D`
- [ ] **S3** — one data panel with a live multi-series graph. *The spike most likely to be skipped and
      most likely to change the decision*

---

## Owed — documentation debt, none of it blocking

Small, and each one is a place the corpus currently says something known to be wrong.

- [ ] **`03 §3.3`, `§3.4`, `§3.6` — joint rewrite**, owed by `adr/0041`. The District-pair counter goes;
      the circularity argument becomes structural; **force-promotion must stand on its own second
      argument or go**
- [ ] **`adr/0012` amendment** — the route cache's **eviction policy** *and* its **key** (`adr/0012`'s
      *"keyed by origin-destination pair"* is ambiguous between nodes² and Buildings²)
- [ ] **`spike-results`** — the 37k–111k in-flight band conflates duration sensitivity with peaking and
      must be re-derived on both axes
- [ ] **`05`** — strike the ~400k Trips/Day figure, known wrong and still standing in the authoritative
      document
- [ ] **`05 §3`** — Parking Shed invalidation needs the *when you pay / what survives* correction
      `CONTEXT.md` → Epoch has taken
- [ ] **`06`** — the S2 specification (*"30k Travellers"*) and S1's (*"20k Buildings"*) are stale
- [ ] **`adr/0012`, and two other filenames, use "Agent"** — banned outright by `CONTEXT.md`. 33
      occurrences across 22 files

## Owed — findings that change a later task

- [ ] **The long-run trend assertion is owed by slice 7, and the instrument for it now exists.**
      *Decided:* task 7 shipped the Census and `series(metric, window)` and deliberately did not ship
      the assertion. Nothing in the world grows or shrinks yet — no Event Wheel, no Rules, no Trips —
      so *no collection trends upward at steady state* would pass against an empty world and a static
      one equally, and an assertion that cannot fail reads as covered. **Switch it on when slice 7
      gives the world churn**: sample on the trace cadence, take a series per metric over the tail of
      a 100k-Tick run, and fail on a positive trend in `slots` with `live` flat. The `--census` report
      prints the numbers today, and printing them is what makes the vacuity checkable rather than
      argued
- [ ] **The end-of-run tier allocates on the Large Object Heap at scale** — ~544 KB at 100k Citizens,
      ~5.4 MB extrapolated at 1M, Gen2 collections at the top of the measured range. Once per run,
      after the trace is written, so it perturbs nothing today. Fix is a scratch buffer on the
      registry; **do it when S0 shows a real 1M city**, not on an extrapolation

## Owed — decisions, and who owns them

- [ ] **Volume attribution's price** — S2 R2a. Decided by `adr/0041`; the cost is still unmeasured
- [ ] **The `adr/0020` exposure** — union-find computes weak connectivity, *"mutually reachable"* is
      strong. Settled by S2 R1's asymmetry figure
- [ ] **The travel-time matrix refresh cadence** — filed as tuning, almost certainly hash-bearing
- [ ] **The sun arc's phase widths** — named in `02 §1.2` and `01 §7`, never sized, so no peaking factor
      exists anywhere. Probably hash-bearing
- [ ] **Zone count, road density, cluster size, the Epoch's granularity** — all S2's, all swept
- [ ] **The routing Tick budget share** — 10% is a stated guess and **cannot** be ratified until the
      Tick's other consumers are priced
- [ ] **A save migration path for a Chunk size change.** Chunk size is on the *cannot be retrofitted*
      list and nothing describes what happens if a profile later says it should move. **Its own session**
- [ ] **The Microscopic Cap** — still unset. Needs a built traffic model; S2 R2 only informs it

---

## Blocked

| | Blocked on |
|---|---|
| **Slice 7** — Rule engine, Bins and Rules | 🔴 `02 §4` residue |
| **Slice 8** — Rule engine, hot reload | 🔴 `adr/0015` |
| **Slice 9** — Event Wheel | 🔴 `02 §7`, `adr/0006` |
| **Slice 10** — Zone Rules | 🔴 depends on slice 7 |
| **S0** — synthetic 1M-Citizen city | slices 4–6. *Until it exists, 1M is a hope* |
| **Phase 2 and Phase 3** | unplanned by design, and stay that way until S0 runs |
