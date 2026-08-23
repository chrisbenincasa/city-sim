# Session report — 2026-08-22, milestone 24 task 3

**Read this, then the starter prompt at the bottom.** Nothing is committed. The working tree holds
everything described here.

---

## The headline

**Sealing is built and measured. It cannot be committed as done, because running it uncovered a
whole-map sweep that costs 421× the Tick budget, and fixing that is a design decision that is yours.**

Three things happened, in this order: I settled a decision by argument, the argument was wrong, and
building the thing told us so in about four minutes.

---

## 1. What got built

| Thing | Where |
|---|---|
| `[[building]] footprint_tiles` — how many Tiles a Building covers and Seals. Default **1**, refused below one | `Ruleset.cs`, `RulesetLoader.cs` |
| The Building's Seal, at the single door every Building comes through | `World.CreateBuilding` |
| The road's Seal, written **as each road is laid** | `RoadGenerator.Layout.SealRun` / `SealTile` |
| `CellGrid.ToCellsClamped` — the map-boundary fencepost | `CellGrid.cs` |
| Off-lattice Segment **spatial index** (the performance fix) | `StreetGrid.Bucket`, `LineSourceQueries.Level` |
| Two instruments | `SealingMeasurementTests`, `SealingCostTests` |

**One genuine bug found by running it:** a lattice paved to the boundary puts its far grid line at Tile
`WorldTiles` — one past the last Tile, in a Cell that does not exist. Both the road path and the gate-Lot
path threw on `bordered.toml` and `crowded.toml`. `ToCellsClamped` is deliberately a *separate* method so
plain `ToCells` stays strict.

---

## 2. What the measurement said — and what it refuted

`SealingMeasurementTests`, 4,000 Citizens on `minimal.toml`:

| | |
|---|---|
| mean over sealed Cells | **6.3%** |
| **peak Cell** | **117 Tiles = 11.4%** |
| roads' share | **93%** (99% on `severance.toml`) |

🔴 **Two claims I wrote into `adr/0143` yesterday were wrong, and the ADR now carries a correction
banner.**

1. I argued `adr/0022`'s *"ground sealed 12%"* was a throwaway mockup that should be corrected. **A peak
   Cell is 11.4%.** The specimen was right and my reasoning for dismissing it was wrong.
2. I wrote "roads are roughly 86%". It is **93%**.

🔴 **And the decision's central stance does not survive.** *"Pavement is what seals ground"* is not a
property of the city — it is a property of `footprint_tiles = 1`. Every Building seals exactly its
footprint, so the sensitivity is arithmetic, not another run: at a realistic **47 Tiles** (~750 m²) the
481 Buildings seal **22,607** against the roads' **7,310**. Buildings go from **6% to 76%** of Sealing;
the mean Cell from 6% to about **24%**.

⚠ **Where the 1 came from, and why it is the lesson of the day.** I took it from `CONTEXT.md` → Sealing's
*"one house seals 1/1024 of its Cell"* — an illustration of the unit. Twenty-five lines away,
`CONTEXT.md` → **Building** says a Building *"has a footprint (the set of Tiles it covers)"* and
*"interacts with Map Layers through that footprint"*. `RuleEngine.cs:936` says the same in an error
message: *"Sealing is a property of a footprint."* **I never opened the entry that specified the
mechanism.** In the same document I dismissed one prose sentence as illustrative and promoted another to
specification, and the difference was which one suited the cheaper answer.

**The good news:** `footprint_tiles` now exists and defaults to 1, so this is a Ruleset edit and a
reading rather than an argument.

---

## 3. The cost, which is the real story

Giving Sealing a write path made `LayerCellTable` **dense for the first time** — 262,144 rows on
`bordered.toml`, the whole 512×512 map. That took one Tick in 256 from free to **88 seconds**.

⚠ **It is not the Decide guard.** `--no-decide-guard` changes it by **1.0×**. That was my first
hypothesis and the measurement refuted it.

**The chain, every link measured:**

| Term | Figure |
|---|---|
| `LayerCellTable` live rows | **262,144** |
| desirability samples per Cell | **4** |
| `LineSourceQueries.Noise` calls per pass | **1,048,576** |
| off-lattice Segments scanned, **twice** per call | **12,581** |
| off-lattice visits per pass | 🔴 **26,384,269,312** |

**Cause:** `LineSourceQueries.Level` scanned *every off-lattice Segment in the world*. Its own doc
comment called that deliberate, resting on `adr/0014`'s *grid plus sparse Arterials*: *"It is a linear
scan on purpose … adr/0014's layout is what makes the set small."*

⚠ **That premise was true of Arterials and a foot path falsified it silently.** A foot path is
off-lattice too, and `foot_paths_per_thousand_blocks` is a rate **per block** — so the set grew with the
map. Of the 12,581, about **10,500 are foot paths**.

### ✅ Fixed, and verified

`StreetGrid` now files each off-lattice Segment under the block of its **midpoint** and records
`OffLatticeReachBlocks`; `Level` widens its existing window by that reach and walks buckets.

| | Tick 16, `bordered.toml` |
|---|---|
| before | **88,085 ms** |
| bucketed by endpoint | **10,233 ms** |
| bucketed by **midpoint** (shipped) | **6,578 ms** |

**13.4×, and hash-preserving.** The argument is not "it looked the same": `Contribution` returns zero
beyond `source.Range`, so a Segment outside the window contributes zero as the background *and* zero
through `Above`, which compares it against that same zero. `LineSourceQueryTests` and
`DesirabilityTests` pass unchanged — 20 assertions.

### 🔴 What is NOT fixed — and it needs you

**6,578 ms is still 421× the 15.6 ms budget, and no query-level work closes it.** The pass is 1,048,576
queries; the budget allows about **15 ns each**. ***The remaining defect is the pass, not the query.***

`MapLayers.SetLandValueTargets` walks **every live Cell row every 256 Ticks**. `02 §10`'s own rule is
*`O(1)` at the write site per Tick, `O(n)` **staggered**, whole-world at end of run* — a whole-world
sweep is the shape it names as wrong. Staggering to 1/256 of the Cells per Tick gives **~4,096 queries ≈
25 ms**, the right order.

⚠ **Staggering moves the State Hash, so it is a design change, not an optimisation.** `adr/0044` makes
the Layer cadence **the designer's number rather than the profiler's**, and *when a Cell retargets* is
that number. That is why it is filed and not done.

⚠ **This was latent and Sealing did not cause it.** A Cell row existed only where something emitted
pollution — one shipped Ruleset in ten. ***The Cell table's sparsity was load-bearing and stated
nowhere.*** Map-wide pollution would have tripped the same wire.

---

## 4. Where it is filed

| Document | What went in |
|---|---|
| `plans/0013` | The cost, with every multiplicand **measured**, and the before/after |
| `plans/0002` §C | 🔴 **Does the land value target pass stagger?** — open, *arguable*, blocks task 3 |
| `plans/0040` | **F7**, and task 3 marked 🟡 built-but-blocked |
| `docs/adr/0143` | Correction banner — the 12%, the 86%, and the footprint stance |
| `docs/adr/0048` | Refusal count 129 → **130** |
| `StreetGrid` doc comment | Corrected — it described a scan that no longer exists |

---

## 5. 🔴 Two things needing you, before anything else

### a. Another checkout has my stray edits

Early in the session, Serena's project root pointed at **`/home/christian/Code/city-sim`**, not this
worktree. Five source edits landed there before I noticed:

```
src/Borough.Core/Rules/Ruleset.cs
src/Borough.Formats/RulesetLoader.cs
src/Borough.Core/Entities/World.cs
src/Borough.Core/Space/RoadGenerator.cs
src/Borough.Core/Entities/SyntheticCity.cs
```

That checkout is on branch `milestone-12-task-1-two-centres` with its own live work from the same
evening. **Git is blocked for me there in both directions**, so I could neither restore nor commit it.
The same changes now live correctly in this worktree, so **nothing is lost by reverting those five paths
there** — but check that session's work first, because I could not.

### b. Nothing is committed here

The working tree holds all of it. I did not commit because you did not ask, and because task 3 should not
land as done while the land-value pass is unresolved.

---

## 6. Honest notes on how this session went

- I settled decision 4 by argument, then had to correct it twice — once from reading the code, once from
  measuring. Both corrections were things a measurement would have given me first.
- I asserted the Decide guard was the cause of the slow gate. It was not; the guard multiplier is 1.0×.
- I misread `ps` lifetime-average CPU as "the process is blocked". It was at 200%.
- Your instinct that we were treating ADRs as gospel was right, and the specific failure was **selective
  citation** — quoting the line that suited the answer and never opening the entry that specified the
  mechanism.

**What actually worked:** building the thing and looking at it. Every real finding in this report came
from a run, not from a reading of the corpus.

---

## 7. Verification status at sign-off

| Check | Result |
|---|---|
| `dotnet build -c Release` | ✅ green |
| Corpus checks (31) | ✅ green |
| `LineSourceQueryTests` + `DesirabilityTests` (20) | ✅ green — the perf fix changes no answer |
| `BuildingResidencyTests`, `PopulateCommandTests`, all `District*` (68) | ✅ green |
| `SealingMeasurementTests`, `SealingCostTests` | ✅ green, and they print the figures above |
| Full assertion tier | 🟡 **FINISHED: 1,999 passed, 3 failed — all `GoldenHashTests`, the expected hash movement from Sealing.** ⚠ **31m38s against a ~50s baseline**, which is the open land-value cost and not a new defect |

⚠ **The slow gate IS the finding, not a blocker on the finding.** Do not re-run the full tier expecting
it to be quick until the stagger question is answered.

✅ **Nothing is broken.** Every assertion about whether the city is *correct* passes. The only failures
are the golden traces, which record hashes of a world where `Sealing` was identically zero and now is
not — `adr/0100`'s "costs nothing while nobody is carrying a save", and the regeneration command is in
`tests/Borough.Tests/Golden/README.md`. ⚠ **Do not regenerate them until the stagger decision is made**,
or they will be regenerated twice.

---

## 8. Starter prompt for a fresh session

Copy everything in the block below.

```
Read plans/0000-board.md, then SESSION-REPORT-2026-08-22.md at the repo root, then
plans/0040-terrain-and-the-land-rows.md section F7.

Context: we are on branch milestone-24-terrain-scoping. Milestone 24 task 3 (the Sealing
write path) is CODE-COMPLETE and UNCOMMITTED in the working tree. Building it exposed a
pre-existing cost: MapLayers.SetLandValueTargets walks every live Cell row on one Tick in
256, which was free only because the Cell table was empty on eight of ten shipped Rulesets.
Sealing makes it dense. I fixed the query underneath it 13.4x (StreetGrid now buckets
off-lattice Segments spatially) but the pass itself is still 6,578 ms on bordered.toml,
which is 421x the 15.6 ms Tick budget.

THE DECISION I OWE YOU, and it is the first thing to settle:
plans/0002 section C, "Does the land value target pass stagger?" — 02 section 10 says
O(n) work should be staggered, and staggering to 1/256 of the Cells per Tick gives about
4,096 queries and roughly 25 ms. But a stagger moves the State Hash and adr/0044 makes the
Layer cadence the designer's number, not the profiler's. So it is my call, not yours.
Put the options to me with their consequences before writing any code.

Two rules for this session, both learned the hard way yesterday:
1. Do not settle a claim by argument if a measurement could settle it. Build the smallest
   thing that produces a number, run it, and read the number. adr/0043 is the rule and I
   broke it three times yesterday.
2. Before quoting any sentence from CONTEXT.md or an ADR as authority, check whether a
   different entry in the SAME document specifies the mechanism more directly. Yesterday I
   quoted CONTEXT.md's Sealing entry and never opened its Building entry, which said the
   opposite and was right.

Also outstanding, and please deal with it before anything else:
/home/christian/Code/city-sim (branch milestone-12-task-1-two-centres) has five stray source
edits of mine from yesterday — Ruleset.cs, RulesetLoader.cs, World.cs, RoadGenerator.cs,
SyntheticCity.cs. The same changes now live correctly in this worktree, so reverting them
there loses nothing, but check that session's own work first. I was sandboxed out of that
checkout and could not verify it.

Do not commit anything until I say so.
```

### If you would rather skip the decision and just tidy up

The alternative first move is to commit what exists as a work-in-progress, so the branch has a
restore point before anything else changes. The gate is green apart from `GoldenHashTests`, and
`adr/0100` is explicit that hash movement costs nothing while nobody is carrying a save — it only
asks that the commit subject explain the move. A suitable subject:

```
milestone 24 task 3: sealing writes, and a whole-map sweep nobody had run over a full map

Moves every State Hash: LayerCellTable.Sealing was identically zero on every world and is
now written by construction. adr/0100 — costs nothing, and the attribution is this subject.
```
