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

## Gate

**Cleared, session seven.** `adr/0034` sorted fields by source geometry, split the Cell from the
Chunk, and produced `02 §2.5`'s classification procedure. The Cell is frozen at 32×32 Tiles.

**One thing inside the gate is contested** — the diffusion cadence's classification. See *Decisions
owed*.

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

## Decisions owed by this slice

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
