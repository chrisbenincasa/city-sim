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

**What is in front of the project is mostly argument, not code.** Slices 7–10 and every Phase 2
milestone are gated on designs written from research and never grilled — twelve sessions, tabulated
below, none of which touches slice 6 and almost all of which can run beside it. The board used to
list those gates as 🔴 marks against slices, which read as *wait*; they are work, and they are
available now. **One of the twelve, `06` itself, turned out to be two** — see session nine in *Done*,
and the audit note below it, which now has a diagnostic rather than just a suspicion.

---

## Do these next

**Three tracks, and they do not contend for anything.** The code track is somebody at a keyboard, the
argument track is a grilling session, the spike track is a machine running unattended. This board has
only ever ordered the first — which is why Phase 2 has looked further away than it is. **Almost
nothing standing between here and Phase 2 is code.**

| | Track | Task | Where | Why this one |
|---|---|---|---|---|
| **1** | spike | **S2 R0 — the synthetic Road Graph and the denominator** | [`0010`](0010-s2-routing.md) | The project's **top risk**, and the one blocker argument cannot close. Blocked by nothing, blocks nothing |
| **2** | code | **Slice 6 — Map Layers** | [`0009`](0009-map-layers.md) | Last slice before the Phase 1 gate closes. Settle its **diffusion cadence** inside it — `0003` files that as owed and blocking |
| **3** | argument | **`adr/0015` — hot reload** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | `06` says it **must not slip behind 3c**, and slice 6 *is* 3c's Layers half. By the corpus's own instruction this session is already due |
| **4** | argument | **`02 §4` residue** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | The nearest gate on the code path. Slice 7 waits on it and slice 7 is next after 6 |

*Why S2 first now:* the argument for delaying it was that the golden baseline should exist before
throwaway spike code starts changing `Core`. It does, and the runner is what a person uses to look at
what moved. Slice 5 is closed and no longer in front of it.

*Why an argument session sits this high for the first time:* every remaining Phase 1 slice and every
Phase 2 milestone is gated on one, and none of them is gated on code. Running them behind the code
rather than beside it is what would make the Phase 1 gate a wall instead of a line.

---

## The argument track — what stands between here and Phase 2

**Phase 2 is not blocked by code.** Every milestone in it (`06` 5a–10) waits on a design written from
research and never argued, on one spike, or on a number nobody has chosen. `0002`'s readiness review
states the shape plainly: *Phase 2's wall is one large item, not many small ones.*

**None of these touches Map Layers, so all of them run in parallel with slice 6** unless the last
column says otherwise. Ordered by what they unblock, soonest first. Each is a session, not a task.

*Slices and milestones share numbers and are not the same thing* — slice 8 is hot reload and
milestone 8 is parking — so the *Unblocks* column always says which.

| | Session | What is actually missing | Unblocks | With slice 6 |
|---|---|---|---|---|
| **A** | **`adr/0015`** — hot reload | Never grilled at all. `06` asserts it *must not slip behind 3c* and gives no reasoning for the assertion; it is gated on an argument, not on more code | slice 8 | **yes** |
| **B** | **`02 §4` residue** | Fallback chain depth and **cycle checking** — `on_fail` chains are the whole diagnostic story and are currently unbounded, and nine Resources plus a Policy layer make them longer. Also whether `mean_workforce_experience` is a legitimate Building Readout, and **what a predicate may read** | slices 7, then 10 | **yes** |
| **C** | **`02 §7` + `adr/0006`** — Event Wheel | Both never grilled. `02 §7` is partly spoken for by `adr/0033` and must be **read against it rather than fresh** | slice 9 | **yes** |
| **D** | **`03 §5`** — the traffic model | **The wall.** The most detailed unargued design in the project, now carrying transit vehicles. It is one large item and should be booked as more than one sitting | milestones 5b, 5c, 6, 7a | **partly** — the half that wants S2's numbers waits for R1–R3; the rest does not |
| **E** | **`adr/0005` + `adr/0007`** — fidelity | One session, not two: `0007` moved Fidelity from person to **place**, and `0005`'s tiers are what it moved. Written from research, not argued | milestones 7a, 7b | **yes** |
| **F** | **`adr/0008`** — walking is a simulated Leg | Written from research. It is what makes 5b *the irreversible milestone*, so the argument is owed before the Leg model is built rather than after | milestone 5b | **yes** |
| **G** | **`adr/0016`** — the lane is the entity | Written from research. Carries the order-of-magnitude claim the whole microscopic tier rests on | milestone 6 | **yes** |
| **H** | **`adr/0009`** — parking is modelled supply | Written from research. Its `adr/0006`-class occupancy leak is already named and needs the invariant specified with it | milestone 8 | **yes** |
| **I** | **`adr/0012`** — routing intent lives in the agent | Written from research, and already owes an amendment: the route cache's **eviction policy** and its **key** | milestone 5c | **after S2 R6** — the two caches are R6's subject |
| **J** | **`05 §7` format half**, plus **map size** and **Outside Connection layout** | The three things `06`'s open-decisions table still has blocking save/load, narrowed from the map question that `adr/0020`–`0022` otherwise closed | milestone 10 | **yes** |
| **K2** | **`06`'s Phase 2 ordering** | The ordering only. **K1 is done** — see *Done* — so what remains is re-deriving the sequence against conserved Money, Hinterlands, Office, the labour system, transit and every Service, and placing the **seventeen mechanisms `06` now lists as having no milestone** | Planning Phase 2 at all | **last** — A–J move what it sequences |
| **L** | **A presentation design** | **It does not exist.** Every other phase is backed by a design document; rendering has none, and `05 §2`'s sim/render boundary is on the never-argued list while `adr/0002` was re-argued to serve *inspection*. **Write it first, then grill it** — unlike A–K this is not a session against an existing document | Phase 3, and planning it at all | **yes**, but blocked on S1 and S3 |

**Not arguable, and it is worth being explicit about why.** The **Microscopic Cap**'s value needs a
built traffic model; S2 R2 only informs it. **S2** itself is measurement — argument cannot close it,
which is exactly why it sits at the top of the code-adjacent order rather than in this table.

**Cheap, and due before slice 7 rather than during it:** a **TOML parser library is unnamed**, and
`adr/0003` requires any core dependency be argued against it explicitly. A determinism liability
entering the core needs a written exception. `0003` calls this argument cheap and says it should not
happen mid-slice.

### What must *not* be grilled yet

`0002` names these as playtest questions wearing design-question clothing, and the argument track
should not drift into them: health (#26), recreation (#27), Service variants (#28), car ownership
(#3), private capital (#7), and `01-player §1/§3/§4`. The governability problem especially —
*268 km² of individually-placed service Buildings* — **is not answerable by argument.** Somebody has
to try placing them.

### Audit these for the shape `adr/0003`'s debt had

`0002` recorded a finding worth acting on before booking any of A–K: `adr/0003`'s owed validation sat
undischarged because **two separate debts had been filed as one**, and the runnable half was parked
behind a grilling session it did not actually need. Its own instruction — *worth auditing the other
🔴-blocked debts for the same shape* — has not been carried out. Doing it first is cheap and may
move work out of this table and into the code track.

**There are now two data points, not one.** Session nine found `06` to be the same shape by accident:
K was scheduled last because *"A–J move what it sequences"*, and that argument binds only the
**ordering**. Correcting claims that settled decisions falsify — K1 — depended on nothing, and ran in
one sitting. **The tell in both cases is a gate whose stated reason covers only part of what it
blocks.** The audit is still owed and now has a diagnostic to apply: for each 🔴 row, ask what the
gate's reason *does not* cover, and check whether that remainder is runnable today.

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
- [x] **Session nine — `06-roadmap.md`, and what a planning document may assert.** Taken **out of the
      board's order**, and legitimately: K was one blocked half and one runnable half filed as one.
      **K1 is done** — every claim a settled decision falsified is struck, not corrected. `06` lost its
      contents column entirely and its milestone rows are now **name plus risk retired**; Phase 0/1
      order points at `0003`, status at this board, mechanism at the design documents. It gained a
      table of **seventeen mechanisms with no milestone** — Money, Hinterlands, Office and the labour
      system, Density, Services, Crime, the nine Resources, Upkeep, Policy and the Sweep Rule family,
      transit, Taste, and more — and a short list of **instructions ADRs addressed to it and nobody
      executed**. Phase 2's *"the city is alive"* is replaced by what those ten milestones would
      actually produce: a transport and housing simulation with **no money in it, nobody employed, and
      no way for anyone to arrive**. Produced **`adr/0042`** — a planning document cites, a design
      document owns. **K2, the ordering, remains and stays last**

---

## Unblocked, in order

### Main track — code

- [ ] **Slice 6 — Map Layers** — [`0009`](0009-map-layers.md). Gate cleared. Settle the diffusion
      cadence inside the slice
- [ ] *the Phase 1 gate closes here*
- [ ] **S0** — the synthetic 1M-Citizen city. Unblocked the moment slice 6 lands, and **the corpus
      forbids opening Phase 2 content until it has run**
- [ ] Slices 7–10 — each behind a session in the argument track above, not behind code

### Parallel track — argument ([the table above](#the-argument-track--what-stands-between-here-and-phase-2))

- [ ] **A** — `adr/0015`, hot reload · **B** — `02 §4` residue · **C** — `02 §7` + `adr/0006`
- [ ] **D** — `03 §5`, the traffic model *(more than one sitting)*
- [ ] **E**–**I** — the six research-written ADRs *(`0005`, `0007`, `0008`, `0009`, `0012`, `0016`)*
- [ ] **J** — save/load's three: `05 §7`'s format half, map size, Outside Connection layout
- [ ] **L** — write a presentation design, then grill it. Blocked on S1 and S3
- [ ] **K2** — re-derive `06`'s Phase 2 ordering, last. *(K1 done — session nine)*

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

**These two have a job again.** `06` framed them as gating a commitment to Godot; `adr/0036` took the
core's language out of `adr/0001` and session eight confirmed the host argument, so there is no
decision left for them to gate. They are the **empirical inputs to session L** — a rendering ceiling
and a UI-cost figure — and L is what unblocks Phase 3. Their specifications in `06` were stale by
roughly an order of magnitude and have been struck; size them from `spike-results` and the 1M target.

- [ ] **S1** — chunked `MultiMeshInstance3D` at city scale. *Feeds L*
- [ ] **S3** — one data panel with a live multi-series graph. *Feeds L, and it is **the spike most
      likely to be skipped and most likely to change the decision***

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
- [x] ~~**`06`** — the S2 specification (*"30k Travellers"*) and S1's (*"20k Buildings"*) are stale~~
      **DISCHARGED session nine** by deletion rather than correction, per `adr/0042`: `06` no longer
      carries spike specifications at all. `0003` and `spike-results` own them
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

**Every row here but one names a session rather than a piece of work**, and that is the point of the
rework. S0 is the single row waiting on code; everything else is waiting on an argument nobody has
had, which means none of it has to wait for slice 6.

| | Blocked on | Which is |
|---|---|---|
| **Slice 7** — Rule engine, Bins and Rules | 🔴 `02 §4` residue | session **B**, and the TOML dependency exception |
| **Slice 8** — Rule engine, hot reload | 🔴 `adr/0015` | session **A** — *already* overdue against `06` |
| **Slice 9** — Event Wheel | 🔴 `02 §7`, `adr/0006` | session **C** |
| **Slice 10** — Zone Rules | depends on slice 7 | session **B**, transitively |
| **S0** — synthetic 1M-Citizen city | slice 6. *Until it exists, 1M is a hope* | the only row here blocked on code |
| **Phase 2 milestones 5a–10** | 🔴 `03 §5` and six research-written ADRs, plus S2 | sessions **D**–**J**, plus a spike |
| **Planning Phase 2 at all** | S0 must have run, and `06`'s ordering must be re-derived | session **K2** |
| **Phase 3** | 🔴 **a presentation design that does not exist** | session **L**, itself blocked on **S1** and **S3** |

**The Phase 3 row used to read *"unplanned by design, and stays that way"*, and that was wrong** — it
described a choice where the truth is an absence. Phase 3 is unplanned because rendering has never
been designed, never been argued, and has no document to argue: every other phase is backed by `02`,
`03`, `04` or `05`, and there is no equivalent for presentation. Worse, the interface it would build
on was **re-argued to serve something else** — `adr/0002` was rebuilt around hot and cold query
flavours on the finding that it had *"assumed a renderer because rendering is what an engine boundary
is usually for"*, when the actual consumer is an inspector. The chain is written down now, in `06` and
here: **S1 + S3 → L → Phase 3 is plannable.**
