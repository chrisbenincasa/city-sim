# 0009 — Slice 6: Map Layers

> Slice 6 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 3c**, Layers half.
> Governed by [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md),
> [`02 §2.4–2.5`](../docs/02-simulation-model.md),
> [`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md),
> [`05 §9`](../docs/05-technical-architecture.md).

**A Map Layer is one integer per Cell, produced by convolving a source field with a bounded
kernel — never by iterative relaxation.** This slice builds the Cell grid, the integer separable
convolution, the double buffer, the staggered schedule, and incremental re-diffusion. It closes the
Phase 1 gate: after it, the project has a world with a coarse environment in it that changes for
reasons, and a hash trace that proves it changes the same way twice.

**Risk retired.** Two, and the second is the interesting one. That growth cost scales with Zone size
rather than staying constant — the field is per Cell rather than per Tile, which is *a 1024×
reduction in work and visually indistinguishable once upsampled*. And that layer arithmetic is
**order-dependent**: in-place diffusion is both a determinism hazard and a visible directional smear,
which is a bug that looks like an art decision and would be found late or never.

**The convolution rule is what makes the rest cheap, and it is not an optimisation.** Convolution is
linear, so twenty factories **superpose exactly** — no interaction to model, no ordering to get
wrong — and the incremental scheme is **exact rather than approximate**. Under relaxation-to-steady-state
neither holds, one changed source perturbs the whole field, and *saves would diverge for reasons
nobody could find*.

---

## Status

**All ten tasks are done, and the acceptance criteria pass.** The Cell grid and the Cell/Chunk type
split, the sparse double-buffered `LayerCellTable` (the project's first `Buffering.TwoCopies`), the
separable integer convolution with its three properties asserted, the staggered schedule as a table,
incremental re-diffusion proved **bit-identical** to a full recompute, the three real Layers, the
named holes that throw rather than answering, the recorded home of the line-source queries,
`layer_cells(aabb, layer)` — allocation-free and string-free — and the end-of-run magnitude check.
`Borough.Core.Space`, plus `dotnet run --project src/Borough.Headless -- --layer pollution`, which is
**the first time the project shows a field rather than a number**.

**Both decisions this slice owed are settled, and one of them is settled by measurement** —
[`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md).
**Six findings came out of building it that the plan did not anticipate**, one of which is a wrong
answer this slice published and then had to withdraw; they are recorded under *What building it
found* below.

**The 100,000-Tick acceptance run lives in the test suite rather than the headless runner, and that
is a correction to the plan.** No session can place a pollution source, because sources come from
industry and industry needs Rules (slice 7) — so a headless run would diffuse an empty map 1,562
times and report that nothing trended upward, which is exactly the **vacuous assertion** slice 5 task
7 refused to write. The churn is injected through the cold API instead, which is the only thing that
can supply it today.

**The golden baselines were re-recorded once**, because a fifth table entered the State Hash's
composition. Nothing about the simulation's behaviour moved; the composition did.

## Gate

**Cleared, session seven.** `adr/0034` sorted fields by source geometry, split the Cell from the
Chunk, and produced `02 §2.5`'s classification procedure. The Cell is frozen at 32×32 Tiles.

**The one contested thing inside the gate is settled** — the diffusion cadence's classification, by
measurement. See *Decisions owed*.

## Prerequisites

Slices 2, 3, 4 and 5. Layer cells are the first table declared `double`, and the staggered schedule
needs a Tick counter to be staggered against.

---

## Tasks

### 1. The Cell grid

**32×32 Tiles. World-creation constant, baked into the save, never tuned** — it is the resolution of
pollution, which feeds Fertility and therefore the choice model, so **it changes the State Hash**.
On the 4096² map that is a 128×128 Cell grid for the whole world.

- Sparse storage: an undeveloped region is a null, not an array. `02 §2.1` is explicit that Chunks
  are stored sparsely and the same reasoning applies to layer storage.
- **Chunk = Cell (1:1) for now**, a strict multiple of itself, recorded as provisional. The Chunk is
  hash-preserving and belongs to the profiler; S2 owns its real value because pathfinding has the
  strongest claim on it. Because the Chunk is a **strict multiple** of the Cell, every conversion is
  a shift and no boundary can disagree — which is what made the split cost nothing.

The distinction is worth encoding in type names rather than comments, because welding the two back
together is the specific failure `adr/0034` exists to prevent: *a constant welded to two decisions is
governed by whichever of them is louder*, and the performance role had a profiler while the design
role had nobody.

### 2. Layer storage and the double buffer

One `i32` per Cell per Layer, **double-buffered** — the first table to satisfy `adr/0037`'s rule,
because Phase 5 is parallel and both reads and writes it.

Declare it through slice 4's per-table buffering property rather than as a special case.

### 3. Integer separable convolution

**Two 1-D passes rather than one 2-D pass**, integer arithmetic, explicit rounding through slice 2's
stated-rounding helper.

The tests are the deliverable as much as the code:

- **Superposition is exact.** Twenty sources diffused together equal the sum of twenty diffused
  separately, bit for bit. This is the property that makes incremental re-diffusion legal and it
  should be asserted rather than trusted.
- **No directional smear.** The result is invariant under transposing the source field and the
  result. An in-place implementation must **fail** this test — write it, watch it fail, then delete
  it, the same discipline `dev-environment.md` A5 applies to the Godot guard.
- Rounding is stated, so the same source produces the same field on every machine.

### 4. The staggered schedule

`05 §9`'s slot. Pollution on `tick % 64 == 0`, land value every 256, each layer offset so no single
Tick spikes.

- The schedule is a table, not a scatter of magic numbers in phase 5.
- **The cadence's classification is contested** — see *Decisions owed*. Implement it as a
  world-creation constant read from the Ruleset, which is the safe direction: it can be relaxed to
  tuning if the argument goes that way, and cannot be tightened after saves exist.

### 5. Incremental re-diffusion

Maintain a set of Cells whose **sources** changed; re-diffuse only those plus a **halo of radius
*r***. This is **exact, not approximate**, because the kernel has bounded support — the whole reason
the convolution rule was worth stating.

The test: the incremental result is **bit-identical** to a full recompute. Not close; identical. If
it is merely close, the design has silently become a relaxation and the save-divergence failure is
back.

### 6. The three real Layers

Only what is actually a Layer under `adr/0034`'s classification:

| Field | Representation | Cadence |
|---|---|---|
| **Industrial pollution** | stored, diffused. Point sources, real plumes run 1–10 km | every 64 Ticks |
| **Land value** | stored, slow-moving. **The exception, and stored because it has momentum** — it moves toward current desirability rather than tracking it, which is both realistic and a stabiliser against oscillation | every 256 Ticks |
| **Sealing** | stored per Cell, **not diffused**. A count of Tiles ever built on, decaying at a Ruleset rate keyed by terrain type | on build |

### 7. Compose at the point of use — and build nothing that composes yet

**Compose at the point of use; do not bake composites into stored layers.** A stored desirability
layer would need invalidating whenever any input changed, and would drift.

```
Desirability = w₁·land_value − w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline
fertility(cell) = terrain suitability − Sealing − pollution
```

Both are **derived and never stored**. Both also need inputs that do not exist yet — noise and
amenity need the Road Graph, terrain suitability needs the generator. **Leave named holes rather
than placeholders.** A placeholder returning zero is a value that will be read, believed, and tuned
around; a hole that fails loudly is a hole.

### 8. Record the queries that are not Layers

Noise and near-road pollution **stopped being Map Layers** in `adr/0034` and this is the slice where
somebody would re-add them by reflex — *"add a Map Layer" was the reflex answer four times running
and was the right answer once.*

They are **line sources**: short-ranged, logarithmic, 50–300 m, and the whole gradient fits inside
one Cell, so a Cell-resolution field degrades into *is there a road here*. A line source is a
**distance query**, exact at Tile resolution, and quantising it to any grid is worse than not
quantising it. Finer Cells were considered and rejected.

There is no Road Graph in Phase 1, so there is nothing to query. What this slice owes is the **note
in the code where the query will live**, plus the property that will constrain it: the query **sums**
rather than taking the nearest source, and enumerates **by loudness rather than by road class** —
every linear source in range whose contribution exceeds the ambient background, which is a crossover
rather than an authored threshold.

### 9. `layer_cells(aabb, layer)`

The hot query from `adr/0002`. Allocation-free, a flat span of value types, ids and numbers only,
**no strings** — the shell owns every string a human reads. It is the first hot entry point the
project has and it sets the pattern for the rest.

### 10. Magnitudes are bounded too

`adr/0003` extended `adr/0006` from collections to quantities: **no quantity accumulates without
bound.** A diffusing layer with a source and no decay is exactly the shape that violates it. Register
the check in slice 5's **end-of-run** tier and let the long-run test find it.

---

## Acceptance

- `dotnet test` green; `dotnet build src/Borough.Headless` builds with no Godot.
- Superposition exact over twenty sources; transpose invariance holds; the in-place variant is seen
  to fail before it is deleted.
- Incremental re-diffusion is **bit-identical** to full recompute over a randomised source-change
  sequence.
- A 100,000-Tick headless run with all three Layers scheduled: hash trace reproducible across two
  runs, no collection trending upward, **no layer magnitude trending upward**.
- **Something to look at:** an ASCII or CSV dump of a Layer's Cell grid from the headless runner,
  before and after a source change, plus the halo that was actually recomputed. It is the first time
  the project shows a *field* rather than a number, and it is the direct ancestor of the Phase 3
  overlay.

## What building it found

Six things the plan did not anticipate, each of which changed what was built — and one of them
changed a decision this slice had already published.

**1. The rounding cannot live inside a pass, and the plan's own acceptance criteria are what forbid
it.** Task 3 asks for *explicit rounding through slice 2's stated-rounding helper* **and** for
*superposition exact, bit for bit*. Those are incompatible if the rounding is per-pass: integer
division is not linear — `RoundDiv(41, 81)` is 1 and so is `RoundDiv(82, 81)`, so two sources of 41
diffuse to 2 apart and 1 together — and superposition is precisely the claim that the operator is
linear. So the passes accumulate exactly, **a Layer is stored pre-normalised in kernel units**, and
the one stated division happens at the point of use. It is cheaper as well as exact: one division per
read rather than one per Cell per pass. Recorded in `adr/0044`.

**2. The convolution form already contains half of what "double-buffered" was asked for, and the
half it does not contain is the half that matters.** `02 §2.4` justifies the double buffer by saying
in-place diffusion is order-dependent — but under `adr/0034 §3` the read field (**sources**) and the
write field (**the diffused value**) are already different columns, so the read-write hazard that
argument describes is not present for pollution. What genuinely needs two copies is **land value**,
which moves *toward* a target and therefore reads its own previous value. The table is declared
`TwoCopies` anyway, per `adr/0037`'s per-table rule, and the buffer is now real rather than declared:
`Rows.PrepareBack()` seeds the write half and `Rows.SwapBuffers()` makes it live.

**3. `PrepareBack` seeds rather than clears, and incremental re-diffusion is why.** A partial writer
that swapped an unseeded write half would swap in values from two cycles ago everywhere its halo did
not reach — a field flickering between two states on the diffusion cadence, which reads as an art
problem. It seeds **every** column rather than the ones the phase intends to write, because a swap
that moved a stale `id` or `generation` would resurrect freed rows.

**4. The integer first-order lag has a dead band, and a dead band is path dependence in stored
state.** Land value closes a fraction of the gap to its target each cycle — `RoundDiv(gap, tau)` —
and at `tau = 8` a gap of 3 rounds to a step of 0. So a Cell settles up to `tau/2` short of its
target, *on whichever side it approached from*: two cities with identical desirability holding
different land values because of their histories. Under `05 §4` that is two cities, so it is a defect
rather than a rounding detail. The fix is a **minimum step of ±1** whenever the gap is non-zero,
which cannot oscillate because a gap of one moves by exactly one. Caught by testing convergence
**from both directions**; a test that only approached from below would have passed.

**5. A trend measured before steady state is a measurement of the transient, and the acceptance run
found this the expensive way.** The first churn drew sources at random across the map, and peak
pollution rose from 1.06M to 1.44M over the run's second half — which reads exactly like the
unbounded accumulation `adr/0006` forbids, and was the field *filling in*. A random walk over 16,384
Cells has not covered them by Tick 100,000, so the run never reached the state the rule is stated
about. The churn now **sweeps a bounded region round-robin** with each Cell's emission a fixed
function of the Cell, so the source field converges to a known constant after one sweep and the
assertion becomes **exact equality across the tail** rather than a trend line. That is a stronger
test as well as a correct one: an accumulating implementation fails on the first sample after
convergence. **The lesson generalises past this slice** — every long-run assertion `0003` owes needs
a stated argument that the run reaches steady state, and slice 5 task 7's owed trend assertion
inherits it.

**6. `adr/0044` published a wrong classification and had to withdraw it, one draft later.** Its first
version filed the cadence as a **world-creation constant**, reasoning that `adr/0015`'s Ruleset is *by
definition* the set of numbers a designer may change without changing the city. `adr/0015` says the
opposite in its own words — the Ruleset's content hash feeds the State Hash, and reload is a logged
simulation event precisely so hash-bearing changes stay replayable — and its world-creation category
carries a **membership test** (*was existing state recorded in units of the constant?*) that the
cadence fails and the kernel radius passes. The ADR had cited `adr/0015` without running the test it
states. **Citing an ADR is not applying it**, and that is the finding: the corpus's rules are only
worth what somebody executing them against the case at hand is worth. Cost here was one document,
because no code depended on the wrong half — `LayerRuleset` was already a constructor argument, and
on the corrected classification the `WorldConfiguration` field and log-format bump the first draft
owed are **not owed at all**.

## Decisions owed by this slice

> **SETTLED, both of them.**
> [`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md).
> The cadence is **hash-bearing** — two cadences produce two hash traces, so it is a design change
> under `05 §4` — which makes it **the designer's number and not the profiler's**, and it stays
> ordinary hot-reloadable Ruleset data. The kernel is a **separable tent reaching 1,024 m (8 Cells)**,
> authored in metres, recorded **unratified**, and it *is* world-creation-fixed, because a Cell is
> stored in kernel units. `05 §9`'s multiplier bullet is corrected and `02 §1.2`'s row gains the hash.
> What follows is the question as it was posed.
>
> **The ADR said *world-creation* for the cadence too, for one draft, and that was wrong** — see its
> own record of it. The correction is the fourth thing building this slice found, and it is filed
> below with the other three.
>
> **The measurement also found something the question did not ask for: the divergence is
> transient.** Once emissions stop and both cadences have fired the fields are bit-identical, because
> a Layer is a convolution of its sources and not a function of its own history. The cadence does not
> change what the field settles to — it changes *when a source becomes visible to a Rule*, which is
> the sentence below, now with a number under it. **That makes the case stronger, not weaker: a city
> is never in the settled state.**
>
> **And it found that the plume band fails the corpus's own guard rule.** `02 §2.4`'s *1–10 km* is a
> 10× span, and `02 §2.5` guard rule 1 says two ranges more than ~5× apart are two fields wearing one
> name. Either industrial pollution is two fields or the band describes the spread across industries.
> **No argument can tell those apart; it wants a source.** Filed, not fixed.

**The diffusion cadence is classified as tuning and probably is not.** `02 §1.2` lists *Map Layer
diffusion, every 32–64 Ticks, staggered* in the **tuning** column. But the cadence decides when a
source's contribution becomes visible to a Rule that reads the Cell, so two runs at different
cadences produce different cities — which makes it a **design change** under `05 §4`'s State Hash
rule, not a free knob.

This is the same welding failure `adr/0034` found in Chunk size, one document later, and it is worth
naming as such: `05 §4`'s hash rule is only as good as somebody running it against each number **by
name**. Recommended handling: reclassify as hash-bearing and world-creation-fixed, or produce the
argument for why the visible-effect lag is not observable. Implement the safe direction meanwhile.

**Also owed:** the **kernel radius and shape** for industrial pollution. `02 §2.4` grounds the range
in reality — 1–10 km plumes — but no kernel is stated, and *author in domain units, never in utility
units* applies to the machinery as well as to the balance constants. Whatever is chosen must be
recorded as unratified and stated as a distance before it is stated as a cell count.

## What this slice deliberately does not do

**No Zone Rules.** `06` bundles them into milestone 3c and they are Sweep Rules, which are the Rule
engine, which is gated on `02 §4`'s residue. Splitting them out is what lets this half land now.

No noise or near-road pollution queries — no Road Graph to query. No Amenity — it is a **walkable
catchment on the Road Graph**, a *time* rather than a distance, and it needs the same thing. No
Fertility as a live number — it composes from terrain suitability, which needs the world generator.
No water pollution: it is a **Bin per Water Body** plus a shoreline line source, which is network
transport rather than spread, and it belongs with the economy.
