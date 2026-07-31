# 0008 — Slice 5: the Tick, the Input Log and replay

> Slice 5 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 1**. Governed by
> [`02 §1.1`](../docs/02-simulation-model.md), [`02 §8`](../docs/02-simulation-model.md),
> [`02 §10`](../docs/02-simulation-model.md),
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md),
> [`adr/0002`](../docs/adr/0002-simulation-is-an-engine-agnostic-library.md),
> [`05 §7`](../docs/05-technical-architecture.md), [`05 §8`](../docs/05-technical-architecture.md).

**`step(inputs)`, the eight-phase ordering, a `u64` Tick counter, the Input Log, and replay.** After
this slice the project has the tool every later debugging session is downstream of: a run that can be
reproduced exactly, and a hash trace that names the Tick a bug entered.

**Risk retired.** Determinism, which is close to impossible to retrofit. Every later tool — replay,
bisection, save/reload equivalence, crash forensics, the golden-hash regression — is a consumer of
this slice and none of them can be built before it. `adr/0037` made it more load-bearing rather than
less: with the full-world double buffer deleted, **replay reconstructs any later state** is now the
entire mechanism of crash forensics.

**Also retired, quietly:** the phase ordering itself. *Ordering is not an implementation detail — it
is the determinism contract.* Writing the eight phases as named, ordered, mostly-empty methods now
means every later mechanism arrives into a slot that already exists, rather than being appended
wherever it fit.

---

## Gate

**Cleared, session eight.** `adr/0003` is closed: the hash function is normative with literal
constants, `purpose_tag` is specified, the save-and-hash field declaration is settled, and `02 §10`'s
invariant tiers are settled. `02 §8`'s rule list was grilled alongside it.

## Prerequisites

Slices 2, 3 and 4. The hash needs the field declaration; `draw()` needs the purpose-tag enum; the
phases need something to write to.

---

## Tasks

### 1. `step(inputs)` and the phase skeleton

Signature, verbatim from `02 §1`: `step(inputs) -> ()`. No wall-clock, no camera, no renderer, no
filesystem. A Tick is an unsigned integer counter and the host decides when to advance it.

Write all eight phases as named, ordered methods, most of them empty:

| Phase | Name | Concurrency | What it will do |
|---|---|---|---|
| 0 | **Input** | serial | Apply player commands from the Input Log |
| 1 | **Wake** | serial | Drain the Event Wheel bucket for this Tick |
| 2 | **Decide** | parallel, **read-only** | Evaluate Rules and Needs against the Past. Emits *intents*, never a mutation |
| 3 | **Settle** | serial, sorted | Apply intents in shuffle order. Re-check atomicity. Losers take their fallback |
| 4 | **Move** | parallel | Lanes advance Vehicles; Statistical trips check arrival |
| 5 | **Layers** | parallel | Map Layer diffusion for whatever is scheduled |
| 6 | **Growth** | serial | Zone Rules sample Lots; Buildings with accumulated failure decline |
| 7 | **Commit** | serial | Schedule next events, re-evaluate Stress, emit the State Hash if due |

Two things to encode rather than comment:

- **Phase 2 writes nothing, and that is load-bearing** (`adr/0037`). It is what permits every entity
  table to be single-buffered. A future decision to parallelise Decide must not also make it
  mutating. Assert it: in debug, Phase 2 runs against a write-guarded view.
- The concurrency column is **permission, not implementation**. `02 §1.1` states what may be
  parallel; `05 §6` states what is. Phase 1 runs everything serially and the two documents do not
  currently say that they differ, which is a noted contradiction — write the code so the distinction
  is visible.

### 2. The command model and the Input Log

The log is exactly:

> `(world seed, configuration, Ruleset content hash, player commands per Tick)`

— and nothing more. **There is no camera input to record**, because `adr/0007` derives Fidelity from
Stress rather than from the camera, and `adr/0002` removed the simulation from the camera in the
other direction.

- Commands as unmanaged structs, tagged by kind, applied in Phase 0.
- Ruleset content hash carried per Tick, with a reload appearing as a **transition carrying both
  hashes** rather than as an event — a replay needs the Rules' *content*, not the news that they
  changed. Slice 8 fills this in; stub the field now so the format does not change later.
- Size expectation, worth holding as a design check: a ten-hour session is **kilobytes**, because a
  player issues a handful of commands a minute. **A bug report is an attachment.**

### 3. Replay

`run(log)` → a hash sequence. This is CI lint 5: **two runs of the same Input Log produce identical
State Hash sequences.**

The property that makes it work is already built: randomness is `draw(seed, entity, tick, purpose)`,
counter-based, so results are independent of evaluation order — which is also what will let Phase 2
be parallelised later with no coordination and bit-identical output.

### 4. The golden-hash baseline, and its re-baselining procedure

A stored Input Log with a recorded hash sequence, committed.

**The point is not that the hash never moves — it is that it never moves without someone saying so.**
Write the procedure down beside the baseline: what a deliberate re-baseline looks like, who records
why, and the standing example — swapping softmax for Gumbel-max is *hash-breaking but
distributionally neutral*, which is safe to do deliberately with a re-baseline and unsafe to do
silently.

### 5. The headless runner

`Borough.Headless` is the primary interface for the whole of Phase 1 and most of Phase 2, and it is
*the project most likely to be dismissed as a nicety and the one that decides whether this simulation
ever gets balanced*.

```
--seed N            --ruleset PATH        --log PATH
--ticks N           --hash-every N        --out PATH
--strict            (replay mode: refuse an unaccounted Ruleset mismatch)
```

- Dumps a hash trace and aggregate series. `series(metric, window)` is on the cold API explicitly for
  panels **and this runner**.
- **Replay mode is strict and play mode is lenient** (`05 §7`). `Borough.Headless` is the strict one:
  a different Ruleset is a different simulation and the hash will diverge — that is arithmetic, not a
  bug. A replay whose Ruleset does not match **refuses to run** rather than diverging silently.
- Must build and run with Godot uninstalled. `dotnet build src/Borough.Headless` is the continuous
  check that the boundary still holds.

### 6. The invariant tiers

`02 §10`, and the shape matters more than the contents. **Invariants sort by frequency, never by
build configuration** — the earlier debug-build gate was backwards, because the runs that surface
these bugs are headless balance runs, millions of Ticks long, in **release**.

| Tier | When | What goes here |
|---|---|---|
| **Per Tick** | every build | Only `O(1)` and `O(changed)` — no Bin negative or over capacity **at the write site**, parking occupancy conserved, no Trip without a Fate |
| **Staggered** | every build, one slice per Tick | The `O(n)` sweeps, amortised the same way Sweep Rules are: Goods conserved, no Citizen in two places, every Household's home exists and lists them as an occupant |
| **End of run** | headless suite | The whole-world walks: **money conserved** — the overflow detector `adr/0003` relies on — every cross-table handle valid, and *no Rule asleep with all its inputs satisfiable* |

Build the three registries and wire whatever exists. Most tiers will be nearly empty after this slice
and that is correct; what matters is that the next mechanism has a tier to register into and cannot
default into a debug-only check.

### 7. The long-run test

100,000+ Ticks in CI, in seconds, asserting the city is still coherent — and asserting that **no
collection and no magnitude trends upward once the city reaches steady state**.

`adr/0006` for collections, `adr/0003`'s extension for quantities. Not merely finite: *not trending
upward*. It belongs in CI from here rather than being added once something has already leaked — the
failure is invisible at design time, takes hours of play to manifest, and the corpus has already
written it twice on paper.

This needs a **collection-size census hook**: a per-Tick or per-N-Tick sample of every
variable-length structure's length, which the intrusive-list pattern from slice 4 makes uniform.

### 8. The crash artifact

Catch at the **Tick boundary** and emit the last checkpoint plus the Input Log since it, with the
Ruleset content hash and the panic Tick — `(checkpoint @ 4096, log 4096..5000)` for a panic at Tick
5000. You then replay to Tick 4999 and single-step into the failure under a debugger, rather than
dumping a corpse.

There are no checkpoints until milestone 10, so **for Phase 1 the artifact is the seed plus the whole
log**, which is equivalent and smaller. Write it in the checkpoint-shaped form so milestone 10 fills
in a field rather than replacing a mechanism.

`adr/0037` made this *stronger* rather than weaker, and with no new machinery — it is determinism plus
the Input Log, which the project already has.

---

## Acceptance

- `dotnet run --project src/Borough.Headless -- --ticks 100000 --hash-every 1000` completes **in
  seconds** and prints a hash trace.
- Two runs of the same log produce **byte-identical** traces.
- A test that mutates one hashed field mid-run and asserts the trace diverges at **exactly** the
  expected Tick — this is the bisection property, and it should be proven rather than assumed.
- A run with a mismatched Ruleset hash **refuses to start** in `--strict`.
- The long-run test passes with no collection and no magnitude trending upward at steady state.
- The three invariant tiers run in **release** builds.
- `dotnet build src/Borough.Headless` succeeds on a machine with no GPU and no Godot.
- **Something to look at:** the hash trace itself, diffable against a previous run. This is the first
  artefact in the project that can catch a bug nobody was looking for.

## Decisions owed by this slice

- **Which thread runs `step()`** — `05 §6` never says, and `adr/0037` made it consequential: with one
  live state, what the renderer and the saver read is a design decision rather than a free
  consequence. **Phase 1 does not need the answer** (it is single-threaded and headless) but the
  runner's shape should not foreclose it. The recommendation on the table is *the simulation owns a
  thread, and `threads=1` means it runs on the caller's thread rather than meaning no sim thread* —
  because `02 §1.2` forbids Tick skipping, so a saturated sim on the main thread takes the camera
  down with it.
- **Whether hot-path results need generation tagging** — `adr/0002`'s rules table leaves the row
  explicitly open and it is a threading question, not a hot/cold one. Not needed in Phase 1.
- **The Input Log's on-disk encoding.** Nothing states one. It should be trivially diffable and
  append-only, and it should carry a format version from the first byte, because it is the artefact a
  bug report is made of.

## What this slice deliberately does not do

No Bins, no Rules, no Event Wheel — slices 7 through 9, all gated. No save format; the crash artifact
uses the log rather than a checkpoint precisely so that it does not need one. **No thread-count
equivalence test** — Phase 1 is single-threaded, and a test asserting equivalence across no
parallelism passes vacuously and then keeps passing after the parallelism arrives. It is written when
the first parallel phase lands.
