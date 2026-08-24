# Fields are sorted by source geometry, and the Cell is a design constant

**Spatial fields are classified by the geometry of what emits them, not by what they represent.** Wide-range point sources become diffused Map Layers; short-range line sources become point-of-use queries and are never stored; area sources reduce to line sources along their perimeter; transport along a fixed graph is network flow. And the grid those Layers live on — the **Cell**, 32×32 Tiles — is **split out of the Chunk and frozen as a design constant**, because its size changes the State Hash and the Chunk's does not.

## Context

`05 §5` overloaded one 32×32 grid with seven roles and named the size *"the working figure and it is not yet validated against any of these."* Validating it exposed two problems, and the second was the real one.

**The four claimed tensions mostly weren't.** At 4096², per-Chunk save overhead is ~64 KB, layer diffusion is already at the noise floor, and the parallel partition is non-binding at any plausible size. Pathfinding wants much larger clusters; render streaming turns out to be two-sided rather than *smaller is better*, since draw calls scale with visible Chunks × archetypes and MultiMesh exists to collapse draw calls. Almost everything wanted **larger**.

**Exactly one role wanted smaller, and it was not a performance role at all.** Map Layers are stored one value per Chunk, so Chunk size *is* pollution resolution — and pollution feeds Fertility (`adr/0022`), Desirability, and therefore the choice model. Sealing is literally a fraction of the grid square: *one house seals 1/1024 of its Chunk*.

**So Chunk size changed the State Hash, while `05 §4` listed it among the things that are *"hash-preserving, all free to tune against a profile."*** Under this project's own rule — *a change is an optimisation if the State Hash is unchanged, a design change otherwise* — a profiler was entitled to an opinion about farm yields. The rule did not fail; nobody had applied it to this number.

Sizing the grid for the mechanic then exposed the deeper defect. The fields that wanted a *fine* grid — noise, near-road pollution — turned out not to want a grid at all.

## Decision

### 1. The Cell is split from the Chunk and frozen

- **Cell** — 32×32 Tiles (≈128 m). Map Layer storage and Sealing. A **design** decision, permanently unavailable for tuning.
- **Chunk** — a strict multiple of the Cell. Dirty tracking, save serialisation, parallel work, aggregate caching, pathfinding cluster, render streaming. A **performance** decision, settled by measurement.

The split costs almost nothing precisely because it is a strict divisor: every index conversion is a shift, every boundary aligns, and the two grids cannot disagree about which side of a line something is on. `05 §5`'s unification argument survives; what left it was a role that was never a performance role.

### 2. Fields are sorted by source geometry

| Geometry | Representation | Instances |
|---|---|---|
| **Point**, range ≫ a Cell | diffused Cell Layer (advected if directional) | industrial pollution, groundwater |
| **Line**, short range | **point-of-use query**, exact at Tile resolution, never stored | noise, near-road pollution |
| **Area** | **reduces to line** along its perimeter | a coastline and a pond are one geometry at two lengths |
| **Network** | flow along a fixed 1-D graph | water pollution |
| composable from the above | derived, never stored | Desirability, Fertility |
| read by nobody but the player | overlay, not a Layer | service coverage (`adr/0032`) |

**One field, one geometry, one range.** Two source geometries, or two ranges more than ~5× apart, means two fields wearing one name. The old `Pollution` row was fed by industry (a point, 1–10 km) and traffic (a line, 150–300 m) through one kernel, so one of them was always wrong.

The classification procedure and its five guard rules are in [`02 §2.5`](../02-simulation-model.md).

### 3. A Layer is a convolution, never a relaxation

A Map Layer is a source field convolved with a bounded kernel. This is what makes many sources **superpose exactly** and what makes incremental re-diffusion **exact rather than approximate**. Under relaxation-to-steady-state neither property holds and one changed source perturbs the whole field.

### 4. Water bodies are Bins on the water graph

Every Water Body is a Bin holding a **Utility-family** Resource with a **capacity** and an **outflow rate** to the next body downstream, terminating in a Hinterland.

> ⚠ **Corrected 2026-08-24 by milestone 24 decision 12. It said *the Waste family*, and that was wrong twice.** There is no Waste family: [`0031`](0031-one-resource-abstraction-and-depth-not-count.md) leaves exactly three — Good, Utility and Money — and `CONTEXT.md` → Resource lists **Waste** as a member of **Good** and **Sewage** as a member of **Utility**. And the family named was the wrong one of the two: a Good is defined as *a Resource whose movement between Districts requires a Vehicle*, while what a Water Body does with its contents is move them **along an edge of the water graph** — which is a Utility's movement exactly. ⚠ **`CONTEXT.md` → Water Body carried the identical sentence and is corrected with it**; two copies of one claim is `plans/0012` **Cause 1**. ***The split this sentence reached past was already in the corpus***, and [`docs/references.md`](../references.md) §10 records four independent commercial lineages arriving at the same one.
 Those two numbers produce ponds, rivers and seas without a taxonomy of water types, and the body's level is the intensity of a **shoreline line source** onto adjacent land.

**Nothing is an infinite sink**, because the map holds only a section of the ocean and a section is bounded. Capacity decides whether pollution behaves as a *debt* (small body: accumulates, permanent) or a *rent* (large body: tracks throughput, recovers) — a gradient rather than two categories.

## Considered and rejected

**A finer Cell (16×16, 64 m) to give noise a real gradient.** Cost was never the obstacle — 65k cells is 1.6 MB. It was rejected because it solves the wrong problem: a line source is a distance query, not a spread, and quantising it to *any* grid is worse than not quantising it. Guard rule 2 sends short-range fields to queries, not to finer grids.

**Keeping one grid and freezing it at 32×32 on gameplay grounds.** Coherent, and it was the live alternative. Rejected because it permanently forfeits the pathfinding and rendering gains for a coupling that buys nothing — the Map Layer never needed the Chunk's other six consumers.

**A coarse HPA\* super-Chunk above the Chunk.** The fork the ledger expected. Unnecessary once the Cell splits off downward, since the Chunk is then free to grow to whatever the pathfinder wants.

**Modelling water depth, stratification, and tides.** Deferred under guard rule 5: no player decision distinguishes them. Depth enters as one number — Bin capacity.

## Consequences

- **The Chunk's size becomes a measurement.** It probably wants to be larger than 32×32; pathfinding has the strongest claim and rendering has a genuine optimum with a bottom to find. Still on the *cannot be retrofitted* list — cheap now, expensive once saves exist.
- **`adr/0014` became load-bearing for the noise model, retroactively** — but for **bimodal traffic volume**, not for having exactly two road classes. The query enumerates linear sources whose contribution exceeds the ambient background, so it is indifferent to labels and survives a new class being added. `adr/0029`'s **Separated** band is already an Arterial and equally rare; **Reserved** is the one that manufactures the middle case by putting Arterial-scale volume on a grid Street, and loudness-enumeration catches it. What would genuinely break the model is a *volume distribution* with no gap in it.
- **Sealing's denominator is now a property of the Cell**, which supersedes the wording in [`adr/0022`](0022-land-is-a-stock-the-city-spends.md) — the arithmetic is unchanged, since the Cell inherits 32×32.
- **`adr/0015`'s world-creation constants gain a member.** Cell size joins `TICKS_PER_DAY` and `WHEEL_SIZE`: fixed at world creation, baked into the save, and a reload that changes it is refused. Chunk size *leaves* that category in spirit but not in practice — it is hash-preserving, but the save is a sequence of Chunk records, so it stays pinned by the format.
- **A river is an uncosted Waste export.** `adr/0024` conserves Money so that exports have a price; pollution crossing the map edge currently does not. Named, not fixed.
- **Water pollution is the first candidate causal chain for Health** — the one Service with no stated purpose (ledger #26). A lead, not an answer.
- **Lake remediation is an endgame lever** of the same shape as brownfield unsealing: pay to undo a stock you accumulated (ledger #4).
