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

**The spike track has opened and moved twice.** **S2 R0 and R1 are done** — the synthetic Road Graph,
the density curve, the uncached denominator on the real `(Segment, offset)` query shape and the
heuristic verdict; then the travel-time matrix. Numbers and decisions in
[`spike-results`](../docs/spike-results.md).

**R1 answered the question the whole spike order was built around, and the answer is yes.** The matrix
carries the choice loop — **1.14 ns** scattered at the working District count against a tripwire at
13.66 ns — so `02 §5.8`'s *never resolve a route inside the choice loop* is enforceable and **the
many-to-many case for DSDV now rests on R2 alone**. It also settled three things nobody had a number
for: **`adr/0020` is owed an amendment** (union-find returns 6 Settlements where Tarjan returns 8),
**`02 §6`'s dirty-region rebuild is unsound** (it misses 72% of the entries an edit changes), and the
**volume-scope question R0 was told not to settle is the same question as the `adr/0020` exposure**.
**R2 is next.**

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
| **1** | spike | **S2 R2 — the path source, and the crossover** | [`0010`](0010-s2-routing.md) | The project's **top risk**, and the one blocker argument cannot close. **R0 and R1 are done**, and R1 dissolved half the headline question — the matrix carries the choice loop, so what remains of the DSDV case is whether Statistical Trips need a concrete path, which is R2's *path source* axis |
| **2** | code | **Slice 6 — Map Layers** | [`0009`](0009-map-layers.md) | Last slice before the Phase 1 gate closes. Settle its **diffusion cadence** inside it — `0003` files that as owed and blocking |
| **3** | argument | **`adr/0015` — hot reload** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | `06` says it **must not slip behind 3c**, and slice 6 *is* 3c's Layers half. By the corpus's own instruction this session is already due |
| **4** | argument | **`02 §4` residue** | [below](#the-argument-track--what-stands-between-here-and-phase-2) | The nearest gate on the code path. Slice 7 waits on it and slice 7 is next after 6 |

*Why S2 first now:* the argument for delaying it was that the golden baseline should exist before
throwaway spike code starts changing `Core`. It does, and the runner is what a person uses to look at
what moved. Slice 5 is closed and no longer in front of it. **R0 and R1 confirmed the delay cost
nothing** — the spike compiles the arithmetic substrate in by source and can name nothing else of
`Core`, so it has changed no simulation code at all.

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
- [x] **S2 R0 — the Road Graph, the denominator, and the heuristic verdict.** `spikes/S2.Routing/`,
      which compile-links the arithmetic substrate by source and can name nothing else of `Core`, with
      the analysers loaded so `BOR0201` carries the plan's no-floating-point prerequisite as a build
      error. Findings: **the ~30,000-Segment placeholder is one Street per Cell boundary**, and at that
      density the mean Segment is 128 m — two statements in `CONTEXT.md` → Segment turn out to be one;
      **the Road Graph is not a memory constraint** at 2.0 MiB against K0's 172.3 MiB; the
      `(Segment, offset)` query shape costs ~250 ns against a 418 µs search, so **the shape the corpus
      committed to is free**; and **admissibility breaks at the first Arterial** — Manhattan returns a
      different route on 4% of drives with two Arterials on the map, which under `05 §4` is a different
      city. **`Chebyshev` is the heuristic**, beating the tighter `EuclideanFloor` by 1.8× because an
      exact integer square root costs more than the expansions it saves — a case where *nodes expanded*
      picks the wrong rung, which R3 must not repeat. Three harness defects recorded, one of which hid
      a graph with no Arterials in it behind four healthy-looking tables
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

- [x] **R0 — the synthetic Road Graph, and the denominator.** Done. The density curve, the footprint,
      the `(Segment, offset)` denominator and the admissibility verdict. Numbers in
      [`spike-results`](../docs/spike-results.md)
- [x] **R1 — the travel-time matrix.** Done, and it is the task the prescribed order existed to
      reach. **The matrix carries the choice loop**: 1.14 ns scattered at the 121-District anchor,
      5.00 ns at 4,096, against a tripwire at S4's K2 gather of 13.66 ns — so the wire does not fire at
      any District count and `02 §5.8`'s rule is enforceable. **District count's ceiling is not L3**;
      the cache cliff arrives below the threshold that was supposed to follow from it, and what binds
      instead is the route store (4.06 GiB at 4,096 against a 172.3 MiB world) against the entry error
      (24.70% → 3.80% across the same sweep). **`adr/0020` is owed an amendment on evidence** — 6
      Settlements against Tarjan's 8 at a tight Commute Budget. **The volume-scope axis R0 was
      forbidden to settle turns out to be the `adr/0020` exposure itself**: per-Segment volume makes
      the matrix symmetric to the bit, which makes union-find right by construction and Stress blind to
      a directional peak, for a 5% saving on a structure that is 1.2% of the world. **`02 §6`'s
      dirty-region rebuild is unsound**, missing 309 of 429 changed entries on a central edit, and the
      sound alternative collapses into a full rebuild because a one-to-all fills a row. Two findings
      the plan never asked for: the **entry error** against a true query (11.32% at the anchor) and
      **time resolution** as a hash-bearing decision — a Day-average matrix reports 1 one-way District
      pair where the morning peak has 76
- [ ] **R2 — searched against looked-up path, and the crossover** *(attribution half settled by `adr/0041`)*
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
- [ ] **`adr/0020` amendment** — owed by S2 R1, on evidence. *"A connected component of the District
      graph… a union-find"* is not what `CONTEXT.md` → Settlement defines, and the two disagree about
      the city where the city is fragmenting. Tarjan is still cheap; it is simply not the ADR's claim
- [ ] **`02 §6` correction** — owed by S2 R1. *Slow cadence, dirty regions only* is **unsound**: a
      spatial test misses the long routes that cross an edit without ending near it — 309 of 429
      changed entries on a central edit. It is `CONTEXT.md` → Epoch's *when you pay* / *what survives*
      distinction arriving at the matrix instead of at the cache
- [ ] **"Zone" is used for the travel-time matrix's granularity, which is the District.** `CONTEXT.md`
      → Zone is *a permission set over land*; `CONTEXT.md` → District is *"the granularity of the
      travel-time matrix"*. `05 §422` and `references.md §2` both say *"zone-to-zone travel-time
      matrix"*, and `plans/0010` quoted the second verbatim — so this is a corpus-wide sweep and not a
      one-line fix, and a corrected quote is a broken one. Found by S2 R1, which spells it District
- [ ] **`spike-results`** — the 37k–111k in-flight band conflates duration sensitivity with peaking and
      must be re-derived on both axes
- [x] ~~**S2's timing tables are owed a canonical re-capture**~~ **DISCHARGED session eleven.**
      `sudo spikes/S2.Routing/tools/routing-run.sh` took R0 and R1 together under `performance`, turbo
      enabled, pinned to one physical core; `docs/spike-results.md` now quotes that capture throughout
      and the `powersave` run is retained beside it. **Captured twice, fourteen minutes apart, under
      the identical configuration** — so the nanosecond columns now carry a measured error bar rather
      than a disclaimer: drive-search absolutes reproduce within 2%, and the one DRAM-resident read
      within 12% — the exposure S4 already named for this machine — while a bootstrap recovered by
      difference between two loops reaches 29%. **Every count is bit-identical
      across all three captures**, which is the determinism check nobody had run. The tripwire column
      reads **0.36×** against a wire at 1.00×
- [ ] **Why plain Dijkstra's absolute moved 1.64× under pinning — NEW, produced by the re-capture.**
      Driving `None` went 779,150 ns unpinned → 1,278,071 pinned/`powersave` → 1,237,578 and 1,240,382
      in the two pinned `performance` runs, so the movement tracks **pinning, not frequency**, and
      **reproduces to 0.2%** rather than being noise; driving `Chebyshev` moved 0.04% across the same
      change. R1.2's first rung is likewise its least reproducible, at 5.8% against 3.3% for the rest. Hypothesis: `taskset` leaves one visible logical
      processor and the tiered-JIT background compilation now shares the measured core, which lands on
      whatever is timed first. **Check: re-run the ladder in reverse order, or with tiering disabled.**
      Until then the first-timed row of any S2 table is the least trustworthy number in it. Owed by R7.
      It already cost one claim — R0's *"`EuclideanFloor` is not faster than Dijkstra at all"* was true
      of the unpinned capture and is struck
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

- [ ] **R3 must not quote HPA\* in expansions saved.** R0 measured a case where the currency does not
      convert: `EuclideanFloor` expands **11% fewer** nodes than `Chebyshev` and takes **1.8× as
      long**, and against plain Dijkstra it cuts expansions by 55% while being no faster at all. The
      cost is its exact integer square root, run twice per node pushed. `plans/0010`'s ladder specified
      nodes expanded, path cost and optimality; **adding a clock is R0's amendment to the plan**, and
      R3 and R6 inherit it — a hierarchy or a cache that saves expansions has not yet saved anything
- [ ] **An artefact that varies with the swept axis is not distinguishable from a result.** R1 needed
      **four** warm-up schemes before its cold-build column stopped falling smoothly with District
      count — which is precisely the shape a reader hopes a sweep will discover, and was the process
      leaving tier 0. Ruling out the per-rung explanations is what identified it as per-process:
      `OneToAll.Run` is called once per District, so the small rungs never call it enough. **R3 and R5
      sweep cluster size and edit rate and are exposed to the same failure**; only a warm pass over the
      whole sweep removes it. This is R0's *"the bootstrap column was mostly the sampler"* in its
      general form, and it is the second time in S2
- [ ] **A sample that shrinks with the swept axis manufactures a trend out of survivorship.** R1's
      entry-error section first drew Access Points uniformly and rejected those outside the named
      District; at 1,024 Districts a hit is one draw in a thousand, so the sample silently collapsed to
      **nine searches** and was printed beside rows built from 2,244. Third instance of the corpus's
      recurring shape, after R0.5's *mean cost when found* and R0's dead Arterials. **Any later section
      that samples inside a swept partition must report its sample size per rung**
- [ ] **An error rate that moves with an unrelated optimisation is not evidence.** R0's heuristic
      multiplies by a floored reciprocal rather than dividing, to remove four hardware divisions per
      node. The reciprocal's ~2-in-10,000 slack **partially cancels an overestimating metric's error**:
      the same change moved walking `Manhattan` from 35 of 300 non-optimal to 4 of 300, worst exactly
      where `adr/0008`'s walk Legs live. **Any later measurement of an error rate — R2b's attribution
      lag, R5's hit rate — should ask what else in the pipeline rounds in the same direction**
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
- [x] ~~**The `adr/0020` exposure** — union-find computes weak connectivity, *"mutually reachable"* is
      strong~~ **SETTLED by S2 R1, against the ADR.** At a tight Commute Budget union-find returns
      **6 Settlements where Tarjan returns 8**, largest component 90 against 70 — a fifth of the map
      assigned to a Settlement it is not mutually reachable within. **`adr/0020` is owed an
      amendment**; see *Owed — documentation debt*. R1 also found the plan asked for the wrong
      instrument: an asymmetry distribution is a claim about travel times, and the test is whether the
      two algorithms disagree about the **city**. And the exposure is a **band, not a threshold** — the
      one-way pair count rises to 264 and falls back to 47 — so no generous Budget closes it
- [ ] **The travel-time matrix refresh cadence** — filed as tuning, almost certainly hash-bearing
- [ ] **The travel-time matrix's *time resolution*** — **NEW, produced by S2 R1**, and the corpus has
      never named it. A Day-average matrix reports **1** one-way District pair where the morning peak
      has **76**, so the two give the choice loop different answers to the same question and are
      therefore two cities under `05 §4`. Same class as the cadence above and should be settled with it
- [ ] **The Commute Budget's granularity** — **NEW, produced by S2 R1.** A matrix entry is wrong by
      **11.32%** (6.73 Ticks) against a true query at the working District count, and whether that is
      free or disqualifying depends on a granularity nothing states. **R6 is owed the same question
      about its cache key** and the two should be answered once
- [ ] **The sun arc's phase widths** — named in `02 §1.2` and `01 §7`, never sized, so no peaking factor
      exists anywhere. Probably hash-bearing
- [ ] **District count, cluster size, the Epoch's granularity** — all S2's, all swept. **District
      count is no longer an open sweep but an open trade**: R1 found the L3 ceiling everyone expected is
      *not* binding, and what binds is the route store against the entry error. **Road density is
      no longer among them**: R0 swept it and reports **16.20 km/km²** at the ~30,000-Segment rung.
      What is owed is not a sweep but a **source** — whether that density describes a real city — and
      `CONTEXT.md` → Segment keeps its disclaimer until somebody checks it
- [ ] **The cost unit for routing.** R0 routes in **Q16.16 Ticks**, and had to: a Tick is ~10.5
      in-world seconds and a vehicle crosses about one Segment per Tick, so whole-Tick costs make A\*
      minimise **hop count** while appearing to route on time. But `05 §121` says *"Q16.16 is for
      sub-Tile positions and nothing else"*. The alternative spelling — an integer count of a fixed
      fraction of a Tick — measures identically, so no number rests on this. **Whether the core
      acquires a second Q16.16 meaning is the corpus's decision, not a benchmark's.** Owed by R7
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
