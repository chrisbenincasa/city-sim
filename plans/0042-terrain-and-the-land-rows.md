# 0042 — Terrain and the land rows: `06` milestone 24's first half

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). The milestone and its named risk are
> [`06`](../docs/06-roadmap.md)'s. What is done is [`0003`](0003-build-plan.md)'s. This document owns
> this slice's decisions, its tasks and its findings, and nothing else.

**Numbering**: this document is `0042`; ADRs from this milestone take **0150 onward**. ⚠ **It was
`0040` taking `0143`–`0149` until 2026-08-23, and it collided** — the milestone-25 branch committed a
different `plans/0040` and a different `adr/0143` to `main` while this branch held those numbers, so
this branch renumbered and **`0144`–`0149` are free**. ***A number is claimed against every live
worktree, not against the one you are standing in*** — [`0012`](0012-corpus-audit.md)'s naming hazard,
fourth recurrence and the first that actually collided.

---

## Status

🔵 **SCOPED 2026-08-22, out of sequence, and SPLIT in the scoping.** **Tasks 1, 2, 3, 4, 5, 6a, 8a and 8b are DONE.** ⚠ **Every decision this half owes is now SETTLED** (2026-08-24), so **tasks 6b and 9 are startable**. Task 3 was
CODE-COMPLETE and BLOCKED for one day — see F7; it was built ahead of task 2 because the Sealing write path needs
no terrain, and running it turned up a whole-map cost that
[`0002`](0002-open-questions.md) §C now owns. **Task 2 landed 2026-08-23** (`a23b46f`, re-baselined in `79efc64`) — **see F8**, whose finding is that the column's home was a third option neither candidate named. **Decisions 1, 1b, 2, 3, 4 and 7 were settled first**
([`adr/0153`](../docs/adr/0153-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md)–[`adr/0150`](../docs/adr/0150-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md));
~~**5, 6 and 10 are open**~~ — **all three closed 2026-08-24**. ⚠ **Task 8 was SPLIT 2026-08-24 into 8a and 8b** on the line task 3 and task 4
are already split on — the write path without the rate — after the survey found the task's own subject
had **no disposition anywhere in the corpus** and its rate had **no owner**
([`adr/0158`](../docs/adr/0158-woodland-is-a-tile-count-per-cell-bounded-by-sealing-because-the-ground-has-one-budget-and-not-two.md),
decisions 8, 9 and 10). ***It was written as a one-line task with no decision behind it and no
Definition-of-done clause; it owed three decisions and a §D1 row.***

⚠ **It is scoped out of sequence deliberately, and the reason is stated rather than assumed.**
Milestone **12** is the live row and is being built in another worktree; milestone **18** was scoped
early on 2026-08-21 for its own stated reason ([`0036`](0036-the-coarse-day-wheel.md)), so
out-of-sequence work has a precedent and a shape. The reason here is that **terrain has exactly one
producer and nothing upstream of it** — [`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)
generates it from the world seed, and no milestone between 12 and 24 supplies an input to it. ***A row
with no upstream is a row that can be built at any time, and its position in the table records what
waits on it rather than what it waits on.***

🔴 **The scoping split the milestone, and `adr/0124` is where the split was first offered.** That ADR's
own *What would trigger revisiting* says: *"Milestone 24 is split. It now carries terrain generation,
Shocks, the Intensity Dial and five land rows … If 24 is scoped and found to be two milestones, this is
where the split was first available."* **It is two milestones.** This document is the first half. The
second half is recorded in **F2** below and is **blocked on 13, 15 and 16**.

✅ **The split is settled and recorded in
[`adr/0153`](../docs/adr/0153-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md),
2026-08-22.** `06`'s row 24 and both inventory rows are rewritten; the Shocks-and-Dial half is
**UNPLACED**. ⚠ **The trigger fired on the half `adr/0124` did not expect** — that ADR offered a *land
systems on terrain* seam, and the seam turned out to be the **dial**.


### Ordinals reserved against milestone 27 — agreed 2026-08-24

⚠ **Two long-lived branches renumber the same corpus, and two of the four collisions this caused
MERGED CLEANLY.** The merge of `main` into this branch collided on `PurposeTag` 22, `Invariant` 54,
`plans/0041` and the `adr/0048` recount. Only the first conflicted. `Invariant` 54 was caught by the
compiler's `CA1069`; the duplicate `plans/0041` was caught by **somebody looking**, because
`PlanIdentityTests` compares a heading to *its own* filename and never to another file's.
***A duplicate enum member is a duplicate declaration; a duplicate document number is two filenames
that agree with themselves.***

**The split, agreed with the milestone 27 session on 2026-08-24 and held on both sides:**

| Ordinal | This branch | Milestone 27 |
|---|---|---|
| ADR | `0150`–`0158` | `0145`–`0149` |
| `PurposeTag` | 23 `TerrainType`, 24 `Woodland` | 25 onward |
| `Invariant` | 55 | 56 onward |
| `plans` | `0042` | `0041` |

Neither side crosses into the other's range without saying so first. ⚠ **This is a convention and it
is the same convention that already failed** — the durable fix is a corpus check for duplicate
document numbers across `plans/` and `docs/adr/`, which milestone 27 is filing in
[`0012`](0012-corpus-audit.md) with this branch's sighting as the evidence. **The ADR half is the
worse half**: an ADR is reached by a *number in a sentence* rather than by a link, so a duplicate
makes those sentences ambiguous while every link still opens and every check stays green. ⚠ **This
said *nine documents* and the estimate was checked rather than taken** — milestone 27 measured it and
the figure lives in [`0012`](0012-corpus-audit.md), which owns it. ***Bare-number citation is the
dominant form and not one form among several***, which is the half of the claim the guess got wrong.

---

## The named risk

`06`'s row for 24 names the risk as *"that the ground under a standing city cannot change"*. **That
sentence covers both halves and this half retires only part of it.** What this half retires:

> **That every Layer hole the corpus has left open is left open by the same absence, and nobody has
> checked whether that absence is one thing.** Five inventory rows were moved onto 24 by
> [`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
> on the finding that each needs terrain and terrain has one source. **Until terrain exists, that is a
> hypothesis about a producer nobody has built.**

⚠ **And the risk statement to be careful of is the one `06` gives for milestone 12**, arriving here
unchanged: ***a milestone whose named risk is a single `throw` reads as a milestone with a single
obstacle***. `MapLayers.Fertility` throws by name (`Space/MapLayers.cs:578`), exactly as
`RuleEngine.cs:871` does for `Scope.Pool`. **The `throw` is the symptom.** This scoping went looking
for the rest and **F3** is what it found.

---

## The gate assessment — ✅ RAN 2026-08-22

**No document names a gate on milestone 24.** Checked: `06`'s milestone table and its *What is parked*
section; [`0003`](0003-build-plan.md)'s Phase 2 ledger, whose row for 13–17 and 19–24 reads *"Not
started"* with no gate column entry; [`0002`](0002-open-questions.md)'s open items, none of which names
24 as blocked or blocking; and [`0000`](0000-board.md)'s *Blocked* section, whose only red gate is
Phase 3's presentation design.

⚠ **`06`'s threading-policy row says session R *"gates nothing in 6–24"*** and was amended once to say
that is true of milestones and false of a decision. **The amendment does not reach this milestone** —
its subject is the wall-clock budget's thread count, and nothing here is priced against a budget.

**Conclusion: ungated.** The gate column in [`0003`](0003-build-plan.md) takes *"assessed 2026-08-22 —
nothing names one"*, the same wording milestone 11 and milestone 18 carry.

---

## The split — what this half is, and what leaves

**Milestone 24 as `06` states it is three bodies of work, and they sit in three different places in the
dependency order.** [`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)
is what makes the difference visible, because it settles **which milestone authors each Hinterland
field**:

| Hinterland field | Authored at | Built? |
|---|---|---|
| edge identity, and what its emigrants carry | **11** | ✅ |
| price per Good | **13** | ✗ |
| median wage | **15** | ✗ |
| depth and recovery rate | **16** | ✗ |
| median rent, service levels, the commute figure | **16** | ✗ |

Now read [`01 §5.4`](../docs/01-player-experience.md)'s three sub-dials against that table:

| Sub-dial | Scales | Which lands at |
|---|---|---|
| **The Bill** | a Hinterland's **price level**, and **the rate it lends at** | **13** — and the lending rate is placed by no document at all |
| **The Clock** | a Hinterland's **depth** and **recovery rate** | **16** |
| **Acts of God** | the **frequency interval** for Flood and Fire | 24's own second half |

And [`01 §5.2`](../docs/01-player-experience.md) defines a Shock as *"a movement in a Hinterland's
authored figures, and nothing else"*, naming **prices, wage, rent, population, composition**.

🔴 **Not one of those five figures exists in the build.** A `[[hinterland]]` block carries exactly three
keys — `edge`, `emigrant_balance_min`, `emigrant_balance_max` (`rulesets/bordered.toml:144-163`). What
it does carry is **what an emigrant carries**, which is the one figure `01 §5.2`'s list does not name.

**So the split falls here:**

| | Ships in this half | Blocked, and on what |
|---|---|---|
| Terrain generation and the baked suitability column | ✅ | — |
| Fertility | ✅ | — |
| Sealing's write path, cadence and rate | ✅ | — |
| Woodland and replanting | ✅ | — |
| Water Bodies, and desirability's shoreline term | ✅ | — |
| Hazard Regions — *derived from terrain, per `CONTEXT.md`* | ✅ | — |
| **Shocks** | ✗ | **13**, **15**, **16** — every figure a Shock moves |
| **Disasters** | ✗ | terrain (this half) **and** milestone **15** for fire-service reachability |
| **The Intensity Dial, Modes and the lock policy** | ✗ | **13** and **16**, via `adr/0131` |

⚠ **Hazard Regions ship in this half and Disasters do not, and that is not a contradiction.**
`CONTEXT.md` → Hazard Region is *"ground where a Disaster can occur, derived from terrain at world
generation"*. It is a **product of the generator**, so it belongs with the generator; what has no home
yet is the thing that fires on it. ***Deriving where something could happen is the terrain milestone's;
scheduling it is the milestone that has something to schedule.***

---

## What the build already holds — surveyed 2026-08-22

### ✅ What exists and works

- **The Cell grid and its per-Cell table machinery.** `Space/CellGrid.cs:24` — `TilesPerCell = 32`,
  `WorldCells = 512`. Two per-Cell tables exist and both are the pattern a third would follow:
  `Space/LayerCellTable.cs:37` (eight columns) and `Space/DistrictCellTable.cs:49`.
- **A world-creation pass that is already a pass.** `Space/RoadGenerator.cs:79` refuses a graph that
  already holds Segments — *"the generator is a world-creation pass"* — and
  `Entities/SyntheticCity.cs:362` (`LayLand`) is the caller. **There is a place to put a terrain bake
  and it already has the right refusal on it.**
- **The seed reaches the generator.** `WorldKey.FromSeed(log.Seed)` at `Input/Replay.cs:105`, carried
  in the Input Log header and in `SaveHeader.Key`.
- **The Layer machinery, its cadence table and its diffusion.** `Space/MapLayers.cs:206` (`Step`),
  `Space/LayerSchedule.cs:126` (`For`). Adding a scheduled operator is adding a branch, not a system.
- **Bins owned by things that are not Buildings.** `BinOwnerKind` (`Rules/Rules.cs:43`) already has
  `Household`, `Business` and `Treasury` live, with dispatch at `Entities/MoneyLedger.cs:124`. **A
  Water Body is a fourth of the same shape**, which is a smaller job than it looked.

### 🔴 Precondition 1 — **there is no terrain, and no document defines its unit**

**Nothing in `src/` or `tests/` holds a terrain type, an elevation, a height, a biome, water, a
shoreline or a slope.** Zero columns, zero enums, zero Ruleset keys. Every textual hit is a comment
saying it is absent.

⚠ **And the term the whole milestone turns on is undefined.** `terrain suitability` appears in six
documents — `CONTEXT.md:187`, `02:230`, `04:70`, `06:88`, `06:103`, `adr/0022:14`, and throughout
`adr/0124` — and **always as a term inside the Fertility formula, never with a definition of its own.**
There is **no `CONTEXT.md` entry for Terrain at all**, and none for terrain suitability. No unit, no
range, no sign convention.

***The artefact `adr/0124` says this milestone owes is named in six places and defined in none***, and
`CLAUDE.md`'s rule is that a concept needing a name gets it in `CONTEXT.md` **first**. That is task 1
and it is not paperwork: Fertility is a subtraction, so a suitability without a stated range makes
`terrain suitability − Sealing − pollution` an expression whose sign nobody can predict.

✅ **RESOLVED 2026-08-22 by decision 1 and [`adr/0154`](../docs/adr/0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md).**
The term is renamed **Base Fertility**, `CONTEXT.md` gains it and **Terrain**, and the undefined
quantity turned out to be **Ruleset data rather than a field**. 🔴 ***The missing definition was not a
documentation gap — it was load-bearing***: `adr/0124` specified a per-Cell column by reasoning from a
name that no entry constrained, and the column it specified is the one thing this decision removes.
⚠ **The half that remains open is the arithmetic**, now decision **1b**.

### 🔴 Precondition 2 — **Sealing has THREE blockers and `adr/0124` enumerated two**

`adr/0124` records that Sealing's decay is *"blocked twice"*: `sealing_decay_tau = 0` in every shipped
Ruleset, and `MapLayers.Step` never calling `DecaySealing`. **Both are true. Both are downstream of a
third, which no document names.**

> ✅ **DISCHARGED 2026-08-23 by task 3** (`1c9ebec`). `World.CreateBuilding` now Seals the kind's
> footprint at `Entities/World.cs:2362` — the single door every Building comes through, the populator's
> and the Zone Rule's alike. **Sealing is non-zero on a generated city**, and the two blockers
> `adr/0124` did enumerate are what remain. ⚠ **The paragraph below is kept as written** because it is
> the record of what the survey found, and its finding — that an enumeration counts the members that
> exist when it is written — is the reason precondition 2 exists.

🔴 **`MapLayers.Seal(Cells, Cells, int)` — `Space/MapLayers.cs:393` — had NO caller in `src/` when this
was surveyed on 2026-08-22.**
Verified: every call site is a test (`LayerFieldsTests.cs:132,135,140,153,171,395`,
`LayerQueryTests.cs:71`, `LayerLongRunTests.cs:220`, `FactorioTests.cs:359`). `LayerCellTable.Sealing`
(`:56`) is a `Saved<int>` column — **saved, folded into the State Hash, and identically zero on every
world this build can generate.** Nothing in the running simulation ever seals a Tile: not
`SyntheticCity.RaiseDwellings`, not `ZoneRuleEngine.Create`, not `RoadGenerator.LayInto`.

⚠ **This inverts the repair order `adr/0124` implies.** Setting the rate and scheduling the operator
would give a decay pass over a field that is always zero — **two hash-bearing numbers chosen to drive
an operator with no input.** ***A decay is the second half of a mechanism, and enumerating its blockers
without asking what writes the field counts the half that is visible.***

⚠ **It is the enumeration defect this corpus keeps finding, at its fourth sighting** — after
`adr/0062`'s two Cap admission ranks where there are three, `03 §4`'s three demotion fields where there
are four, and `adr/0117`'s four grounds re-partitioned into four blockers with one lost.
***An enumeration is written against the members that exist when it is written, and nothing re-counts
it.***

⚠ **And one document states the consequence backwards.** [`deferred.md`](../docs/deferred.md):52 reads
*"a tree-planting programme raises what the ground absorbs and shows up slowly, over the whole area,
exactly as `Sealing`'s terrain-keyed recovery **already does**."* Sealing's terrain-keyed recovery does
nothing, and cannot, because nothing writes its input. **Filed in [`0012`](0012-corpus-audit.md)** under
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
on the day and before working around it.

### ✅ Precondition 3 — **the generator version is NOT owed here, and a document already says why**

It looks owed. `adr/0021` requires *"the generator's version pinned in the save header"* and `06:330`
records the three version numbers as **paid**. `SaveHeader` (`Persistence/SaveHeader.cs:52`) carries
`FormatVersion`, `Key`, `RulesetInForce`, `TicksPerDay`, `WheelSize`, `WorldCells`, `TilesPerCell` and
`StateHash` — **and no generator version.**

**That is deliberate and it is written down.** `05 §7`'s third row is struck by
[`adr/0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md):
a generator version is needed only by a load path that **regenerates**, and none does. It returns *"the
day `adr/0021`'s seed + edits ships"* — and `adr/0111` states the trap explicitly: **a placeholder
inverts the guard**, because a pinned number that agrees with itself defeats the refusal an absent one
achieves through the format version.

⚠ **So the question this milestone must answer is not *do we add it* but *does seed + edits ship here*
— and the recommendation is no.** A baked suitability column is an ordinary `Saved` column; the save
carries it; nothing regenerates on load; `adr/0111` holds unchanged. **Recorded so that a later sitting
does not read the absent field as an omission**, which is the failure `adr/0111` was written against.
It is **decision 6** below.

### ⚠ Precondition 4 — **world creation runs inside a Tick phase, and `adr/0021`'s rule is about reading**

`SyntheticCity.PopulateInto` is not called from the `World` constructor. It is dispatched from
`Simulation.cs:391` on `CommandKind.Populate` — **inside Phase 0 (Input) of some Tick.** So "world
creation" in this build is *an event in the log*, not a moment before the clock starts.

**This is a question rather than a defect.** `adr/0021`'s checkable rule is *"if a terrain value is
**read** inside a Tick phase, something has gone wrong"*, and the bake **writes** one. But the rule's
value is that it can be checked mechanically, and a check phrased as *no terrain read in any phase*
would go red on the generator itself. ✅ **Decision 3 restated it against STATE** — *terrain height is
not state* — and `adr/0156` is what makes that checkable, by storing no height at all. ⚠ **There is no
bake left to place**, and the mechanical check is **owed with terraforming** rather than now.

---

## Open decisions this half owes — ✅ **ALL TWELVE SETTLED.** 1, 1b, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 and 12

⚠ **None is settled and none should be settled by argument if a measurement would settle it**
([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)).
Each carries its type.

### 1. ✅ SETTLED 2026-08-22 — what *is* terrain suitability? **It is Base Fertility, and it is not a field**

✅ **[`adr/0154`](../docs/adr/0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md),
with the user in the room.** `terrain suitability` is renamed **Base Fertility** and is **Ruleset data
keyed by terrain type**. The stored per-Cell column is **terrain type**, `(saved AND hashed)`, and
**two** Ruleset values are keyed by it — Base Fertility and the Sealing decay rate.

**Suitable for what, which is the question that cracked it:** for **farming**, and nothing else. Every
one of the term's six occurrences in the corpus is inside the Fertility formula, and `CONTEXT.md` →
Fertility defines fertility as *"Agricultural capacity"*. So the quantity is **Fertility's ceiling** —
what untouched, unpolluted ground would yield — and not terrain's contribution to buildability, land
value or desirability, which are height and water and have their own consumers.

🔴 **`adr/0124`'s baked column is superseded, and the name is what produced it.** Read *terrain* in
*terrain suitability* and a terrain-derived per-Cell field is the natural inference; `02 §2.3` refuses it
in one sentence — *"the generator places Woodland and nothing else. Fertility is not on the map"* — and
`adr/0022`, which `adr/0124` cites throughout, argues against that field at length. ***A badly named term
is a design defect waiting for somebody to reason from the name***, and `CONTEXT.md` had no entry to
reason from instead.

⚠ **`adr/0124` found a real hole and named the wrong occupant.** A per-Cell terrain column *is* owed —
`CONTEXT.md` → Sealing has required one since it was written, because the decay rate is *"keyed by terrain
type"* and nothing stores the type. **One column, two consumers.**

⚠ **And baking it is what `adr/0022` forbids for the value beside it**: *"decay rates are Ruleset data
keyed by terrain type, **never stored per Tile** … storing a rate as state would freeze it into every
save"*. Base Fertility had the same disposition available and nobody had asked for it.

⚠ **Whether Base Fertility varies is a Ruleset stance and the mechanism is free.** Keyed by type, varying
it is one key and no storage. **The shipped demonstration states it uniform**, so `adr/0022` holds in a
default world; ***varying it amends `adr/0022` rather than tuning within it***, and both ADRs say so.
***What the ground decides in the shipped world is not where you may farm, but whether your damage is
reversible.***

### 1b. ✅ SETTLED 2026-08-22 — **weights, following Desirability — and one of the two is derived**

✅ **[`adr/0155`](../docs/adr/0155-fertility-composes-with-weights-and-only-one-of-them-is-a-number-anybody-chooses.md),
with the user in the room.** `fertility = base fertility − w_s·Sealing − w_p·pollution`, Q16.16, weighted
the way `MapLayers.Desirability` already is. **Base Fertility is a fraction with `1.0` meaning fully
fertile**, so Fertility is a proportion by construction.

🔴 **`w_s` is DERIVED and gets no Ruleset key.** `CONTEXT.md` → Sealing makes a Cell at Sealing = 1024 one
whose every Tile is built on, so it has **no farmland** — an endpoint, not a preference. That pins the
term at `base × Sealing / 1024`, with `1024` already `CellGrid.TilesInCell`. ***A coefficient with an
endpoint is not a tuning knob, and offering it as one invites a Ruleset to state that a fully paved Cell
still farms.*** [`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s
shape, and it takes this decision from **two** unratified numbers to **one**.

⚠ **The measured magnitudes are why the bare subtraction could never stand**: pollution is *"about 12 in
kernel units under a strong source"* (`DesirabilityWeights.Default`) against a Sealing count of 0–1024, so
unweighted, **Sealing outweighs pollution about 85:1 — an artefact of the units and not a claim about
cities.**

✅ **The scale was decided by the readout.** With `1.0` fully fertile,
[`adr/0022`](../docs/adr/0022-land-is-a-stock-the-city-spends.md)'s own Evidence specimen — *"41% — ground
sealed 12%, pollution from Eastfield Industrial 47%"* — falls out of the arithmetic with no conversion and
no denominator anybody has to name.

✅ **Negative is kept and it does not clamp**, because **Sealing decays**: two exhausted Cells are at
different distances from farming again, and a clamp makes them the same number. ⚠ **The decomposition is
not the reason** — Evidence reads the terms — **the recovery ordering is**, and that ordering is what
`adr/0022`'s cyclical land-use arc runs on. ✅ **It saturates rather than throwing**, on
`LineSourceQueries.Saturate`'s stated reasoning that a read-only query must not throw on a world somebody
is allowed to build.

*The question as first written:*

`base fertility − Sealing − pollution` subtracts **three quantities in three units**: a Ruleset value, a
**Tile count** (0–1024 per Cell), and a **Q16.16 stock** in kernel units. Nothing in the corpus
reconciles them, and the bare subtraction every document states is a **shape rather than an
implementation**.

✅ **The build has solved this exact problem once.** `MapLayers.Desirability` (`Space/MapLayers.cs:637`)
does not subtract raw quantities — it applies **weights**, and the comment at `:648` says why: *"pollution
is a count and the weight is a ratio, so the product is already Q16.16."* Fertility has no weights
written anywhere.

**Open sub-questions:** weights or a common normalisation; may Fertility go **negative**, or clamp at
zero; and does it **saturate rather than throw**, which is the choice `Desirability` made on
`LineSourceQueries.Saturate`'s reasoning that *a read-only query must not throw on a world somebody is
allowed to build*. ⚠ **Split from decision 1 rather than bundled** — a status coarser than the claims it
covers is [`0012`](0012-corpus-audit.md)'s granularity defect.

### 2. ✅ SETTLED 2026-08-22 — **height is generated and stored nowhere**

✅ **[`adr/0156`](../docs/adr/0156-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md),
with the user in the room.** The generator **computes and reads** height while it works; it stores only
the **outputs** — terrain type, the water graph with its downstream edges, and floodplain depth **where
the floodplain is**. **No height column, at Tile or Cell resolution.** `adr/0021`'s *the world is not
flat* is untouched.

🔴 **The reason is not cost, and the trace is the finding.** `adr/0021`'s table gives height four jobs; a
consumer-by-consumer check leaves **one** live:

| Job | Status |
|---|---|
| vehicle speed, routing, junction geometry | **excluded by `adr/0021` by name** |
| feed land value and desirability | **no height term exists** — `02 §2.4` is pollution, noise, amenity, shoreline, and shoreline is *water* |
| construction cost via earthwork volume | **construction cost does not exist**; `adr/0035` denominates it in Lane-Tiles, milestone **21** |
| force bridges and water crossings | `adr/0021`: *"a buildability exception plus a rendering variant, **not a system**"* — Phase 3 |
| **maximum buildable grade** | 🔴 **the one live consumer, and it is a REFUSAL** |

***So height's entire net effect on a milestone-24 city is that some Lots decline to build*** — and
`adr/0021` refuses exactly that: *"Without terraforming, terrain is a **wall**. With it, terrain is a
**price**."* ***A mechanism whose only built half is the refusal is not a partial delivery of the design;
it is the alternative the design refused.***

⚠ **Terraforming is not available to fix it.** `adr/0021` calls it *"a player verb"*; **`01 §2` has six
verbs and terraforming is not one, and `01` never mentions it at all**. Filed as an open question in
[`0002`](0002-open-questions.md) — a seventh verb is a change to a section whose shortness is stated as
deliberate.

⚠ **The memory figures corroborate and are NOT the reason.** A per-Tile height is ~**512 MiB** against
**86 MiB** for every table in a 1M world, and at 1M the generator paves **8.6%** of the map — so nine
tenths of the field would describe ground nobody builds on. Cell resolution is ~512 KiB and **useless**,
because a grade on a Lot is Tile-scale. ***Deferring for a cost you have not been asked to pay is
deferring for the wrong reason*** (`adr/0100`'s discipline generalised), so the wall/price argument is
what decides and this paragraph is a check that the answer costs nothing.

✅ **Decision 6's recommendation survives because of this.** Height computed once, consumed once and
discarded **regenerates nothing on load**, so `adr/0111` holds and no generator version returns.

*The question as first written:*

`adr/0021` says *"height is real and the world is not flat"* and that height enters *"only at
construction time"*. **Nothing in this half reads a height.** Buildability by grade needs it; terraforming
needs it; Fertility does not; Water Bodies need drainage direction, which may or may not need it.
⚠ **Building a height field with no consumer is `adr/0070`'s forbidden move** — an unbuilt mechanism is
not a design constraint, and *given height does not exist, should suitability compensate?* is that ADR's
void question. **Recommendation: name the absence, ship suitability, and let the first consumer of a
grade bring the height field with it.**

### 3. ✅ SETTLED 2026-08-22 — **its own pass, before `LayLand`, and the rule is restated against state**

⚠ **Decision 2 dissolved half of this question before it was opened: there is no bake.**

✅ **Placement — a pass of its own, called from `SyntheticCity.PopulateInto` BEFORE `LayLand`**, with
`RoadGenerator.LayInto`'s already-populated refusal shape (`Space/RoadGenerator.cs:133` — *"the generator
is a world-creation pass"*). The order becomes `RefuseIfPopulated` → **terrain** → `LayLand` →
`PeopleInto` → `EvaluateDistricts`.

✅ **Nothing in `LayLand` needs to consult it, which is why the placement is nearly free.** Roads do not
avoid water — `adr/0021`: *"A Street or Arterial may span unbuildable water; the Road Graph does not know
the difference"* — Woodland is *"not a clearing verb and not an obstacle"* (`CONTEXT.md`), and **buildable
grade does not ship** (`adr/0156`). Terrain goes first because it is the ground, not because anything
downstream reads it.

✅ **The rule is restated against STATE rather than against time**, and
[`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) is amended
in place rather than a fourth ADR being written — the design content is `adr/0156`'s, and **a second home
for one decision is [`0012`](0012-corpus-audit.md)'s Cause 1**:

> **Terrain height is not state.** It lives as a local inside the generator's call and dies with it. The
> only terrain the world stores is the **terrain type** column, and a Tick reading a stored column and
> looking a value up in the Ruleset is not a terrain read.

🔴 **The original phrasing was not checkable and was nearly read as violated by the thing that satisfies
it.** `SyntheticCity.PopulateInto` runs from `CommandKind.Populate` **inside Phase 0**, so the generator
reads height *inside a Tick phase* on the Tick that makes the world. ⚠ **And nothing enforces phase
discipline anyway** — `TickPhase` is referenced only by its own file and `Simulation.cs`, so a phase-aware
analyser is machinery that does not exist, the same standing as `05 §4`'s **lint 4**.
***A rule about what may be read every Tick is enforced by what exists, not by where the reader stands.***

⚠ **The mechanical check is OWED rather than written, and the trigger is named**: it arrives with
terraforming, because `adr/0021`'s *seed + edits* stores heights on edited Chunks and makes the forbidden
read reachable again.

⚠ **Minor, and not repaired here: `SyntheticCity.LayLand` lays roads and Lots, not land.** With terrain
arriving beside it the name gets actively misleading. Filed as an observation rather than a rename,
because the churn is not this decision's.

*The question as first written:*

See precondition 4. Candidates: inside `SyntheticCity.LayLand` beside `RoadGenerator.LayInto` (where
the world's other ground is made), or in a pass of its own with `RoadGenerator`'s
already-populated refusal. **The rule's restatement is the deliverable, not the placement** — it must
name a symbol rather than a phase, per
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md).

### 4. ✅ SETTLED 2026-08-22 — **it ships here, it authors no number, and a road seals where it is laid**

✅ **[`adr/0150`](../docs/adr/0150-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md),
with the user in the room.** A Building seals **1** Tile, which is `CONTEXT.md` → Sealing's own sentence
rather than a new figure; a road seals **one Tile per Tile of its run**, width 1, which is 4 m
(`Tiles.Metres`). 🔴 **No `[roads]` width key, no per-kind width, and therefore ZERO
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
rows for the whole decision** — decision 1b opened one; this opens none.

🔴 **The attribution rule is where the work was, and the rule that suggests itself is wrong on a shipped
world.** `MapLayers.Seal` (`Space/MapLayers.cs:392`) writes to **one Cell**, and a Segment is never in
one Cell: at `block_tiles = 32` a Street runs Cell boundary to Cell boundary, and `severance.toml`'s
**256** spans **8**. Splitting a Segment's Tiles between its two endpoint Cells puts **128 Tiles into
each** — 12.5% of a Cell from one road — and **nothing into the six Cells the road runs through**.
***That is not an approximation of the right quantity; it is a different quantity.***

✅ **So Sealing is written at the point of laying, from the geometry the writer holds**, never
reconstructed from a Segment's stored endpoints. `Layout.WalkArterial` (`:444`) already walks every Tile
of an Arterial — it calls `MarkSevered` per step — and `LayStreets` (`:366`), `LayFootPaths` (`:684`) and
`ConnectToGrid` (`:606`) each lay a straight run between two Tile positions they compute.
**Every case is exact, nothing needs a stored path column, and there is no fallback branch.**

⚠ **The Segment whose geometry cannot be recovered is the one that never needs recovering.** The Arterial
trunk is the only Segment whose `LengthTiles` is arc length rather than endpoint distance
(`Space/RoadSegmentTable.cs:26` says so and says why) — and it is the case the generator holds a true
per-Tile walk for.

⚠ **Two facts the earlier brief had wrong, corrected here so nobody reasons from them again.**
`arterial_count` is **16** in `bordered.toml`, `crowded.toml` and `severance.toml`, not 0 everywhere —
`CLAUDE.md`'s constants table quotes `minimal.toml`'s value. And the foot-path diagonal is **fully
determined** (`Intersection(e, n)` → `Intersection(e+1, n+1)`), not a path nothing records.

⚠ **The consequence is a stance and it is recorded rather than assumed**: a built-out Cell at
`block_tiles = 32` reaches **~7%** — ~64 road Tiles against ~10 Building Tiles of 1024 — so **Sealing is
roughly 86% a road statistic**. `adr/0022`'s specimen *"ground sealed 12%"* is a **farm panel mockup**,
is not this quantity, and is **left alone**.

*The question as first written:*

`CONTEXT.md`:181 is unambiguous that **roads Seal and so does every other built Tile**, and that the
**verge beside an Arterial is never built on**. So the write path touches `RoadGenerator`,
`SyntheticCity.Subdivide` and `ZoneRuleEngine.Create`. 🔴 **It moves the State Hash on every shipped
Ruleset** — a column that is identically zero becomes non-zero — which under
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
costs nothing and **must not be cited as a reason to defer, narrow or split this task.**

### 5. ✅ SETTLED 2026-08-24 — **the ratifier is Sealing's own trajectory, and the blocker was one level too far downstream**

**Two hash-bearing numbers under [`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)** — a cadence in
`LayerSchedule.For` and a rate **keyed by terrain type** — each owing a machine, a world and a quantity
under [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).
The **world** half was discharged on 2026-08-23: `rulesets/varied.toml` ships, and every world has
varied terrain anyway (**F8**).

🔴 **The quantity half was closed as unanswerable on 2026-08-23 and that was wrong.** The reasoning
(`ec28329`) was: Sealing's decay is *about land recovering*, `MapLayers.Fertility` is Sealing's only
reader against terrain, and **F9** found nothing reads Fertility — so *how fast does land recover* has
no readout. ***That is true of FARMLAND recovering. It is not true of GROUND recovering, and the
Ruleset key controls the second.***

`CONTEXT.md` → Sealing already states the design intent, **in units and in an ordering**: *"rock may
never recover, floodplain may recover over hundreds of Days."* Sealing is a `Saved` column, and since
task 3 every Building seals its footprint through `World.CreateBuilding`. So the quantity is computable
today with **nothing unbuilt anywhere in the path**.

✅ **Named ratifier: a long run on `rulesets/varied.toml`, on the reference machine. Quantity: Days
from a Cell's last demolition to its Sealing reaching zero, per terrain type.**

⚠ **It named `rulesets/evicted.toml` for four hours on the day it was settled, and that was wrong
twice.** That file states **no `[[terrain]]`**, so a rate keyed by terrain type has nothing to key on;
and it **condemns nothing** — its whole subject is the failure moving from the premises to the tenant,
at *929 tenancies ended against 0 condemned*. ***A tenancy ending is not a Building being demolished.***
The world was picked from its reputation for cycling rather than from reading what cycles in it, which
is the same error as this decision's original blocker one level along. ⚠ **Refuting in both
directions** — every type recovering in the same time means the key is not keyed on anything, and rock
recovering at all means it is too fast. **Two `plans/0002` §D2 rows opened; task 4 moves them to §D1 on
the day it writes the numbers.** ✅ **Task 4 is UNBLOCKED.**

⚠ **The lesson is not about Sealing.** ***A ratifier looked for one level downstream of the key it
ratifies waits for a mechanism it does not need*** — the search went looking for the *consequence* of
the decay (a farm yielding more) when the *effect* of the decay (ground unsealing) is what the number
actually sets, is already observable, and is what `CONTEXT.md` states an intent about.

### 6. ✅ SETTLED 2026-08-24 — **no. `adr/0111` stands, and the absent field is a decision**

See precondition 3. **Nothing on the load path regenerates**, so a generator version buys nothing —
and [`adr/0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md)
states the trap that makes shipping one actively worse than not: ***a placeholder inverts the guard***,
because a pinned number that agrees with itself defeats the refusal an absent one achieves through the
format version.

✅ **Terrain type, Woodland and the water graph are ordinary `Saved` columns; the save carries them;
nothing regenerates on load; `adr/0111` holds unchanged.** **Recorded so that a later sitting reads the
absent `SaveHeader` field as a decision and not as an omission**, which is the failure `adr/0111` was
written against.

⚠ **Precondition 3's wording is stale and the argument is not.** It says *"a baked suitability column"*,
and there is no baked artefact since [`adr/0154`](../docs/adr/0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)
half-superseded `adr/0124` — the Cell stores a **type** and Base Fertility is a Ruleset lookup. ***The
column changed and the reasoning about the save did not***, because the argument never rested on what
the column held, only on nothing regenerating from it.

### 7. ✅ SETTLED 2026-08-23 — **what terrain types ship, and does Base Fertility vary across them?**

🔴 **NEW, and decomposition found it: the milestone's central column had no members.**
✅ [`adr/0157`](../docs/adr/0157-terrain-is-five-types-and-base-fertility-varies-across-them-because-a-category-exclusion-is-not-an-overlay.md),
with the user in the room. **Five types — `ordinary`, `rock`, `floodplain`, `marsh`, `thin_soil`** — and
**Base Fertility varies**: `1.0`, `0.2`, `1.0`, `0.5`, `0.6`.

🔴 **Nothing in the corpus enumerated terrain.** `adr/0154` made Base Fertility *"Ruleset data keyed by
terrain type"* and presumed *"a small enumeration"* without naming a member; `CONTEXT.md` and `adr/0022`
name `rock` and `floodplain` only as examples inside sentences about **recovery**. ***A key was specified
before the thing it keys on***, and task 2 found it on the day it tried to write the column — **the same
shape as milestone 12 decision 10 and this milestone's own precondition 1**, where the artefact named in
six documents was defined in none.

🔴 **It amends [`adr/0022`](../docs/adr/0022-land-is-a-stock-the-city-spends.md) and the amendment was
predicted by name.** That ADR's 2026-08-22 amendment said *"varying it amends this document rather than
tuning within it, and comes back here and to `0154` rather than to a Ruleset review"*. **It came back
here.** ⚠ **The refusal is narrowed, not overturned**: `0022` argues against a generated fertility
**field** — *"fertile valleys here, poor ground there"*, read off an overlay — and **there is still no
field**, no column and nothing baked. What exists is a Ruleset number looked up by the stored type.

⚠ **The half that is not free, recorded rather than dodged**: five ranked values **are** a lookup, and
calling them realism does not change that. **It is taken because the lookup is not the interesting fact**
— Sealing runs 0–1024 against a ceiling of `1.0`, so play moves a Cell across the whole range the five
values sit inside. ***The generator sets where you start; play sets where you end.*** ⚠ **`rock` is 0.2
and not 0**, on `0022`'s own *scarcity is a gradient, never a wall*.

🔴 **All five were chosen against NO CONSUMER**, and that is the row's largest caveat. `MapLayers.Fertility`
throws, task 5 builds it, and **no milestone in `06` builds a farm at all**. Under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) they cannot be argued
from what farming needs, because farming is **unbuilt**. **One `plans/0002` §D1 row covers the five** —
they share one ratifier and are meaningless apart, since what a farm reads is the **ratio between two
Cells** — naming the first farm Rule's yield readout, `rulesets/varied.toml`, and the rock-to-ordinary
yield ratio at equal Sealing and pollution. ⚠ **The trigger is expected to fire**, and that is not a
failure case.

### 8. ✅ SETTLED 2026-08-24 — **what IS Woodland? It is a Tile count per Cell, and Sealing is its ceiling**

🔴 **NEW, and the survey found it: the task's own subject had no disposition anywhere in the corpus.**
✅ [`adr/0158`](../docs/adr/0158-woodland-is-a-tile-count-per-cell-bounded-by-sealing-because-the-ground-has-one-budget-and-not-two.md),
with the user in the room. **A `(saved AND hashed)` count of wooded Tiles per Cell**, on a dense
**`WoodlandCellTable`** of its own — Sealing's semantics in `TerrainCellTable`'s shape — and **bounded
by `TilesInCell − Sealing`**.

🔴 **The ADR was corrected hours after it was written, and about the table rather than the quantity.**
Its first draft put the column on `LayerCellTable`, which is right about the *category* and wrong about
the *address*: **F7** had already recorded that *"the Cell table's sparsity was load-bearing and stated
nowhere"*, and **Woodland is the one quantity that exists where the city is not** — so that placement
would make the table dense on every world. ⚠ **The cost is already measured and it is task 2's rather than this decision's** — `TerrainCellTable`'s header records whole-map Cell residency on `minimal.toml` taking the land value target pass from about **2.5 ms** to about **114 ms**, on a machine that was not quiet, so **no document may quote it** and what survives is the **ratio and the sign**. ⚠ **F7's 88 seconds is a different cost** and that same header says so — ***I quoted it anyway in the ADR's first correction block and had to withdraw it***, which is `plans/0012` **Cause 5** committed an hour after reading the rule against it.

⚠ ***F8's third option, a second time in one milestone***: both candidates in the room were wrong again, and the answer was again a table of its own — **which task 2 had already decided, for the same reason, with the measurement attached.** ***The argument did not need re-deriving and the measurement did not need re-taking; both were in the file next door.***

🔴 **Woodland was named in eleven places and given a disposition in none.** `CONTEXT.md` has a
first-class entry, `adr/0022` decides its whole arc, `adr/0090` closes the generator's remit around it,
and not one says whether it is a column, a flag, a Layer or a Building. ***That is the fourth artefact
in this milestone named in many documents and defined in none*** — after precondition 1, decision 7's
terrain enumeration, and **F8**'s column that lived in neither candidate. **The pattern is now the
expected case rather than the surprise**, which is a finding about the milestone rather than about
Woodland.

⚠ **`CONTEXT.md` said *"Forest **Tiles**"* and the disposition is per **Cell**, so the entry is
corrected rather than the design bent to it.** A bit per Tile is **268 million bits, 33 MB** saved and
hashed at 16,384², for a quantity every consumer reads per Cell — and no ground quantity in this design
is per Tile.

✅ **The bound is the part that is not bookkeeping, and three things fall out of it.** Sealing clears
forest **with no verb and no event**, which is what `adr/0091`'s closed set and `adr/0022`'s own
amendment require; the extraction frontier migrates as arithmetic rather than as a system
(`adr/0022:42`); and **Woodland area becomes a second reader of Sealing**. ⚠ **That last one does not
ratify decision 5 and the trap is worth stating**: if 8b's rate is itself unratified, two unratified
numbers are measuring each other. What the bound supplies is a **readout**, not a ratifier.

### 9. ✅ SETTLED 2026-08-24 — **replanting does not ship here, and it is *undesigned* rather than unbuilt**

`adr/0022:68` specifies replanting as *"a Rule and a land designation rather than a system"*. **There is
no land designation in the build or in the design**: `adr/0091` closed the verb set at six with none of
them clearing or designating ground, `01-player-experience.md` does not mention replanting anywhere —
not in §5.4's dial table, not in the Policy chapter — and no Policy body, predicate or price exists.
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) types that
**undesigned**, whose answer is *design it*, and which is explicitly **not** the *refused* class that
counts as evidence. ⚠ **Building a minimal version would author the designation by accident**, in a task
whose subject is ground — the shape `01 §5.5` forbids by name. **Recorded so the absence is not later
read as an omission**, which is decision 6's shape.

### 10. ✅ SETTLED 2026-08-24 — **the ratifier is Woodland against Sealing over a long run**

🔴 **It is the one [`adr/0022`](../docs/adr/0022-land-is-a-stock-the-city-spends.md) calls load-bearing
by name**: *"the first response is more reboot levers, not faster regrowth; **regrowth speed is the
load-bearing constant** and loosening it deletes the arc."* **It had never had an owner** — no ADR, no
`plans/0002` row, no named ratifier, no Ruleset key, and `adr/0052` had never been applied to it.
***A hash-bearing constant an ADR calls load-bearing, carried for the life of the project by nothing
but a sentence.***

**Its stated condition was *stays open until 8a lands*, and 8a landed 2026-08-24.** Woodland is now a
`Saved` column that accumulates, so the quantity exists rather than being composed at the point of use.

✅ **Named ratifier: milestone 24's long run, on a world stating the regrowth keys, on the reference
machine. Quantity: total Woodland Tiles against total Sealing over 100k+ Ticks — does forest reach a
steady state, and does cleared ground recover on a timescale a player feels?** ⚠ **Refuting in both
directions** — regrowth so fast that clearing costs nothing **deletes `adr/0022`'s arc**, which is the
failure that ADR names by name; so slow that a cleared map never recovers is a one-way ratchet, which
is [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)'s concern wearing the other
sign. **One `plans/0002` §D2 row opened; task 8b moves it to §D1 on the day it writes the numbers.**

### 11. ✅ SETTLED 2026-08-24 — **where does water sit? A stated sea level, and it authors a number**

**The recommendation was *basins only* and the user refused it.** Water fills depressions: priority-flood
the height field, water where the filled level exceeds the ground, outflow where it spills. That authors
**zero** numbers — no `plans/0002` §D row, no ratifier owed, nothing hash-bearing chosen — which under
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) is
a real advantage rather than a tidy one.

***It was refused because that world has no sea, no bay and no coast in it.*** `CONTEXT.md` → Water Body
names all five — *"a pond, lake, river, bay or stretch of coast"* — and basins deliver the first two. On
a map [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) sizes at
65.5 km a side, a city on a coast is the reference case the design was written for.

**So `[water] sea_level_percent` is stated, one key, optional, refused at both ends**
([`adr/0159`](../docs/adr/0159-a-sea-level-is-authored-ruleset-data-and-a-world-without-water-is-a-world-and-not-a-hole.md)).
⚠ **There is no coverage key and that absence is the decision** — a share is an outcome, and authoring it
would make the generator solve for a number instead of laying a world. **One §D1 row.**

### 12. ✅ SETTLED 2026-08-24 — **it was never four positions, and the split was already in the corpus**

**The question was *what family is Waste*, and the answer is that the corpus had already answered it and
two sentences reached past the answer.** Of the four readings:

- `CONTEXT.md` → Water Body and [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md) §4
  are the **same sentence, copied** — *"a Bin holding the Waste family"*. `plans/0012` **Cause 1**, and
  **both were corrected** rather than filed, because the fix is one word and the debt ledger is for what
  is owed rather than what is done.
- `04 §1`'s *"service capacity … coverage rather than a Good"* is the **exact escapee**
  [`adr/0031`](../docs/adr/0031-one-resource-abstraction-and-depth-not-count.md) quotes in its own table
  and generalises away. ***Superseded prose is not a rival position***, and reading it as one is how the
  count got to four.
- `CONTEXT.md` → Resource is the live answer and it **already contains the split**: **Waste** is a
  member of **Good**, **Sewage** is a member of **Utility**.

***So the seam [`docs/references.md`](../docs/references.md) §10 found four commercial lineages arriving
at independently was already in this corpus, and the Water Body sentence named the wrong half of it.***
A Water Body moves its contents **along an edge of the water graph**, which is a Utility's movement; a
Good is by definition *a Resource whose movement between Districts requires a Vehicle*.

✅ **No `ResourceFamily` member added and none needed. Task 6b is UNBLOCKED**, and it still owes its sixth
`BinOwnerKind`, because a Bin's owner is a Water Body whatever family it holds.

⚠ **One question was deliberately left open rather than settled here**: `02:256` gives water pollution two
sources, *"dumping, runoff"*, and dumping is plausibly refuse — a **Good**. A Good sitting in a Water
Body's Bin and moving downstream would be a Good moving with no Vehicle, contradicting the one axis
`adr/0031` uses to define it. **Whether a Water Body's Bin holds exactly one Utility-family Resource is
task 6b's to answer**, and it is *arguable* rather than measurable.

---

## Tasks

⚠ **Provisional below task 2**: decisions 1 and 2 change what tasks 3 onward build.

| # | Task | Depends on |
|---|---|---|
| **1** | ✅ **DONE 2026-08-22** — `CONTEXT.md` gains **Terrain** and **Base Fertility**, the rename lands in `02 §2.3`, `04 §1` and `MapLayers`, `adr/0022` and `adr/0124` are amended rather than rewritten, and `06`'s row 24 is rewritten for the split ([`adr/0153`](../docs/adr/0153-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md), [`adr/0154`](../docs/adr/0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)) | decisions 1, 2 |
| **2** | ✅ **DONE 2026-08-23** (`a23b46f`, re-baselined `79efc64`) — see **F8**. **The terrain generator and the per-Cell terrain TYPE column** — `(saved AND hashed)`, from the `WorldKey`, with a `[terrain]` Ruleset table keying **Base Fertility** and the **Sealing decay rate** off the type, plus **a shipped Ruleset with varied terrain**. ⚠ **The column holds the type and nothing is baked** (`adr/0154`). ⚠ **The world is part of this task and not a follow-up** | 1, decision 3 |
| **3** | ✅ **DONE 2026-08-23** (`1c9ebec`), built 2026-08-22 and blocked on a cost for one day — see F7. **The Sealing write path** — construction Seals, **at the point of laying and never reconstructed from a Segment's endpoints** ([`adr/0150`](../docs/adr/0150-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md)). Touches the four `RoadGenerator.Layout` writers, `SyntheticCity.Subdivide` and `ZoneRuleEngine.Create`. ⚠ **Authors no number and opens no §D row.** Precondition 2's third blocker, and upstream of the two `adr/0124` names. 🔴 Moves every State Hash | 2 |
| **4** | ✅ **DONE 2026-08-24** — see **F12**. **Sealing's decay** — `LayerSchedule.Sealing` at **period `TICKS_PER_DAY`, offset 48**, `DecaySealing` scheduled in `MapLayers.Step`, and the rate keyed by terrain type as a `sealing_decay_tau` on each `[[terrain]]` table. 🔴 **The `[layers]` key is REFUSED rather than ignored**, naming where it went. 🔴 **It found a real defect**: integer exponential decay **stalls**, so the step is floored at one Tile and the tail is linear. **Moved ONE `plans/0002` §D2 row to §D1** — the other is task 8b's and stays. ⚠ **Moves no State Hash on any shipped world**, because `minimal.toml` and its ten siblings state no `[[terrain]]` and `varied.toml` is not a fixture; what moves is **`minimal.toml`'s Ruleset content fingerprint**, from a comment edit | 3 |
| **5** | ✅ **DONE 2026-08-23** (`6f9187c`). **Fertility** — the `throw` in `MapLayers.Fertility` is a composition at the point of use, `base − base·Sealing/1024 − w_p·pollution`, with `long` intermediates and saturation at the `int` bounds. Sets **one** §D1 row: `[layers] fertility_pollution_percent` = **4**, stated in `rulesets/varied.toml` only. ⚠ **It moves no State Hash and needs no re-baseline** — nothing is stored, nothing is scheduled, and no shipped file that a fixture loads was edited. 🔴 **It also has no consumer**, so the whole task is a producer nobody reads; see the note below | 2, 3 |
| **6a** | ✅ **DONE 2026-08-24** — see **F11**. **The water graph** — a sparse `WaterCellTable` of wet Cells with a dense `WaterResidency` beside it, and a `WaterBodyTable` whose one column is a `downstream` handle into itself. Laid by `WaterGenerator` from the **same height field terrain reads** (`adr/0156`, so **no new `PurposeTag`**), bounded by `[water] sea_level_percent` on a new shipped `rulesets/coastal.toml` ([`adr/0159`](../docs/adr/0159-a-sea-level-is-authored-ruleset-data-and-a-world-without-water-is-a-world-and-not-a-hole.md)). **Opens ONE §D1 row.** 🔴 Moves every State Hash. ⚠ **It has no consumer** — nothing reads a Water Body — so it is **F9** a third time and is taken anyway | 2, decision 11 |
| **6b** | **A Water Body's Bin** — the capacity, the outflow rate and a **sixth** `BinOwnerKind`. ✅ **UNBLOCKED 2026-08-24 by decision 12** — the family question was a correction and not a design sitting: Waste is a **Good**, Sewage is a **Utility**, the split was already in `CONTEXT.md` → Resource, and two copies of one sentence named the wrong half. **No `ResourceFamily` change.** ⚠ **It still owes one *arguable* answer**: whether a Water Body's Bin holds exactly one Utility-family Resource, or whether `02:256`'s *"dumping"* puts a **Good** in it. ⚠ **It must take the two water tables back out of `_writableTables`**, because a Bin's level is a write. ⚠ **This row said *a fifth* `BinOwnerKind` and was correct on the day it was written** — milestone 27 landed `Business = 3` on `main`. ***A merge made a plan row stale without touching the plan.*** | 6a, decision 12 |
| **7** | **Desirability's shoreline term** — `w₅`, and the caveat test `adr/0123` requires. ⚠ **It depends on 6b and NOT on 6a**, and this row said *6* until 2026-08-24: `adr/0034`, `CONTEXT.md` → Water Body and `02:256` all make the term's intensity **the Bin's level**, so a shoreline term built on the graph alone would be present and permanently zero — the working-mechanism-that-says-something-false failure `adr/0123` exists to prevent | 6b |
| **8a** | ✅ **DONE 2026-08-24** — see **F10**. **Woodland is placed and cleared** — a `Saved<int>("woodland")` Tile count on a dense `WoodlandCellTable` of its own, placed by the generator, and **bounded by `TilesInCell − Sealing`** so that sealing clears forest with no verb and no event ([`adr/0158`](../docs/adr/0158-woodland-is-a-tile-count-per-cell-bounded-by-sealing-because-the-ground-has-one-budget-and-not-two.md)). ⚠ **Authors no number and opens no §D row**, exactly as `TerrainGenerator` authors none. 🔴 Moves every State Hash. ⚠ **It has no consumer** — the Timber chain is unplaced — so it is **F9** a second time and is taken anyway | 2, 3, decision 8 |
| **8b** | ✅ **DONE 2026-08-24** — see **F13**. **Woodland's regrowth** — `LayerSchedule.Woodland` at **period `TICKS_PER_DAY`, offset 80**, `RegrowWoodland` in `MapLayers.Step`, and `[layers] woodland_regrowth_days = 512` in `varied.toml`. 🔴 **This is `adr/0022`'s *"regrowth speed is the load-bearing constant"*, and it had never had an owner** — no ADR, no §D row, no ratifier and no Ruleset key. ⚠ **It needed a COLUMN the scoping did not anticipate**: `WoodlandCellTable.Potential`, because regrowth needs a ceiling and both ceilings that need no column are wrong. **Moved the §D2 row to §D1.** 🔴 Moves every State Hash | 8a |
| **9** | **Hazard Regions** — derived at generation, never read in a Tick. ⚠ **Floodplain depth is stored SPARSELY, where the floodplain is** (`adr/0156`), because `01 §5.2` spreads Flood *by depth* and a whole-map height field is what this milestone does not build | 2 |
| **10** | **The long run** — 100k+ Ticks, no collection and no magnitude trending at steady state | all |

---

## What this half must not do

- **It must not build a Shock, a Disaster, the Intensity Dial, a Mode or a lock policy.** Every one is
  blocked on 13, 15 or 16 — see the split table. ⚠ **A partial dial is worse than none**: `01 §5.4`'s
  *"the list introduces no new parameters"* is what makes the dial structural rather than aspirational,
  and a dial that authors its own parameters because the Hinterland has none is the difficulty modifier
  `01 §5.5` forbids by name.
- **It must not author a height field with no consumer** — decision 2, and `adr/0070`.
- **It must not write a placeholder generator version** — precondition 3, and `adr/0111` says why.
- **It must not cite hash movement as a reason to defer, narrow or split any task** — `adr/0100`.
- **It must not store a height column** — `adr/0021` as amended, and decision 3 is what keeps its rule
  checkable rather than what relaxes it. ***Terrain height is not state.***

---

## Definition of done

`CLAUDE.md`'s list, plus:

- **Fertility returns a number on a world with varied terrain**, and `MapLayers.cs:578`'s `throw` is
  gone rather than moved.
- **Sealing is non-zero on a generated city, decays, and reaches a steady state** — all three, because
  precondition 2 shows the first is what the other two rest on.
- **A shipped Ruleset demonstrates varied terrain**, carries its own header saying what it exists to
  show, and is what decision 5's ratifiers are read against.
- **Woodland is placed on a generated city, is bounded by Sealing, and the bound is an invariant** —
  `Woodland + Sealing ≤ TilesInCell`, checked `O(1)` at the write site, which is where `02 §10` puts a
  check of that frequency. ⚠ **8a stops there**: regrowth is 8b's and carries the rate.
- **Every hash-bearing number written has a `plans/0002` §D1 row naming a machine, a world and a
  quantity** on the day it is written.
- **There is something to look at** — a headless dump of the terrain and Fertility fields.

---

## What scoping found

### F1 — the gate assessment: nothing names a gate on 24, and the one row that looks like it does is about a budget

Recorded above. The threading-policy row's *"gates nothing in 6–24"* amendment is about the wall-clock
budget's thread count and does not reach a milestone with nothing priced against a budget.

### F2 — milestone 24 is two milestones, and `adr/0124` said where the split would first be available

`adr/0131` fixes which milestone authors each Hinterland field; `01 §5.2` and `§5.4` define a Shock and
the dial entirely in terms of those fields; **the build carries none of the five**. So the Shocks-and-Dial
half is blocked on **13**, **15** and **16** — and on nothing this half builds. ***The terrain half has
no upstream at all and the dial half has three; they were one row because they share a subject, not
because they share a position.***

⚠ **`01 §5.4`'s *"the list introduces no new parameters"* is true of the design and false of the build**,
and the gap is `adr/0131`-shaped rather than an error: the fields are specified and unbuilt. This is
`plans/0003`'s 5a-bis lesson — ***a design document's precondition is a hypothesis about the build*** —
at its next sighting.

### F3 — Sealing has three blockers and `adr/0124` enumerated two, and the missing one is upstream of both

`MapLayers.Seal` has no `src` caller, so `LayerCellTable.Sealing` is a saved, hashed column that is
identically zero on every world this build can generate. Setting the rate and scheduling the operator
without it would choose **two hash-bearing numbers to drive a pass over a field that is always zero**.
⚠ **`adr/0124` needs an amendment**, and `deferred.md`:52's *"exactly as `Sealing`'s terrain-keyed
recovery already does"* is filed in [`0012`](0012-corpus-audit.md).

### F7 — Sealing was built, and what it found was a whole-map sweep that had never run over a full map

✅ **UNBLOCKED AND LANDED 2026-08-23 (`1c9ebec`), and the blocker was dissolved rather than answered.**
The sentence below stood for one day and is kept because the reasoning that produced it was wrong in a
way worth reading.

🟡 ~~**Task 3 is CODE-COMPLETE and must not be committed as done.** Sealing is written, measured, and
correct; the assertion tier is not usable with it in until `plans/0002` §C's *does the land value target
pass stagger?* is answered.~~

⚠ **§C did not need answering, because *the remaining defect is the pass and not the query* was a
conclusion drawn from one division.** 6,578 ms over 1,048,576 queries gives 15 ns each, which looked
like a floor and was not: it assumed every query had to do the work it was doing.
`LineSourceQueries.Level`'s **first pass resolves two node handles and projects a point for every
Segment in the window before `Contribution` ever looks at a volume** — ***so the expensive half of a
traffic query is the half that never looks at traffic.*** Where nothing within range carries Vehicles
the whole query is **provably** zero, and `TrafficPresence` skips pass one: **7,353 ms → 80 ms, 85×,
and hash-preserving.** The assertion tier went **31m38s → 3m17s**, and then **2,002 passed / 0 failed**
once the golden traces were re-baselined (`af3f5fd`).

🔴 ⚠ **The 85× is measured on a world with five Vehicles in it, and that is a fact about the world
rather than a caveat on the fix.** `bordered.toml` at 4,000 Citizens peaks at **5** Vehicles in motion
across a whole Day where `congested.toml` at 16,000 peaks at **937**. **Why is open and filed at
[`0002`](0002-open-questions.md) §B** — the two candidates are the far-gate Commute Budget refusing
nearly every job, which is `adr/0095` working, and `[jobs]` assignment not reaching Citizens, which is
a defect. ***Until that is answered, every figure in [`0013`](0013-tick-budget.md) taken on
`bordered.toml` is taken on a world where almost nobody drives.***

⚠ **The stagger question stays OPEN in §C and stops blocking anything.** A whole-world sweep is still
the shape `02 §10` names as wrong and *when* a Cell retargets is still the designer's number; what has
gone is the performance gun. And [`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)
**contradicts itself** about who owns a stagger's *phase* — noticed there, not settled.

**What shipped:** `[[building]] footprint_tiles` (default **1**, refused below one); the Seal at
`World.CreateBuilding`, which is the single door every Building comes through; `SealRun`/`SealTile` in
`RoadGenerator.Layout`, called as each road is laid; and `CellGrid.ToCellsClamped` for the fencepost —
a lattice paved to the boundary has its far grid line at Tile `WorldTiles`, one past the last Tile and
in a Cell that does not exist, which threw on `bordered.toml` and `crowded.toml`.

**What it measured** — `tests/.../SealingMeasurementTests.cs`, 4,000 Citizens:

| | `minimal.toml` |
|---|---|
| mean over sealed Cells | **6.3%** |
| **peak Cell** | **117 Tiles = 11.4%** |
| roads' share | **93%** (99% on `severance.toml`) |

🔴 **Two claims in [`adr/0150`](../docs/adr/0150-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md)
were refuted by running it, and the ADR now carries a correction banner.** `adr/0022`'s *"ground sealed
12%"* — dismissed there as a mockup — **is right within rounding**. And *"roads are ~86%"* was 93%.
⚠ **The stance that Sealing is a road statistic is an artefact of `footprint_tiles = 1`**, which came
from `CONTEXT.md` → Sealing's *illustration of the unit* while `CONTEXT.md` → **Building** twenty-five
lines away specifies a footprint outright and `RuleEngine.cs:936` says *"Sealing is a property of a
footprint"* in an error message. ***The entry that specified the mechanism was never opened, and two
sentences of equal standing were held to different standards.***

🔴 **And the cost.** Making the Cell table dense for the first time took `MapLayers.SetLandValueTargets`
from free to **88 seconds on one Tick in 256**. ⚠ **It is not the Decide guard** — `--no-decide-guard`
changes it by 1.0×. The cause was `LineSourceQueries.Level` scanning **every off-lattice Segment**, whose
doc comment called the linear scan deliberate on `adr/0014`'s *sparse Arterials* premise — ⚠ **true of
Arterials, and falsified silently by foot paths**, which are off-lattice and authored **per block**, so
the set grew with the map: **12,581**, about **10,500** of them foot paths.

✅ **Fixed to 6,578 ms — 13.4× — and hash-preserving**, by bucketing the off-lattice set by block
midpoint in `StreetGrid` and widening the query window by the recorded reach.
🔴 **Still 421× the budget**, and no query-level work closes it: the pass is 1,048,576 queries against a
15.6 ms budget. ***The remaining defect is the pass and not the query***, it is a design change because
a stagger moves the hash, and it is filed in [`0002`](0002-open-questions.md) §C and
[`0013`](0013-tick-budget.md) rather than done.

⚠ **This was latent and Sealing did not cause it.** The pass is `O(live Cell rows)`, and a row existed
only where pollution had been emitted — one shipped Ruleset in ten. ***The Cell table's sparsity was
load-bearing and stated nowhere.*** Map-wide pollution would have tripped the same wire.

### F8 — the column's home was a third option, and the two candidates on offer were both wrong

**Task 2 owed one decision — where the per-Cell terrain type column lives — and the two candidates
carried into the session were *a column on `LayerCellTable`, written sparsely* and *the same column,
made dense*. Neither survived contact.**

🔴 **The sparse candidate cannot work at decision 3's settled position, and that is arithmetic rather
than a preference.** The pass runs between `RefuseIfPopulated` and `LayLand`, and **at that moment the
Cell table has zero rows** — `LayLand`'s Seal calls are what create them. *Write the type only where a
row already exists* therefore writes **nothing at all**, and collapses to *there is no column*, which
`adr/0157` decided against. ***A placement and a storage shape were settled on different days and only
one of them could hold.***

⚠ **And the dense candidate costs four Layer passes the whole map in every world.** MEASURED:
whole-map Cell residency took the land value target pass on `minimal.toml` from about **2.5 ms** to
about **114 ms**, on the one Tick in 256 it fires. 🔴 **Neither figure is quotable and the reason is
recorded rather than assumed**: another session was running `Borough.Tests` on the same six cores
throughout, so `CLAUDE.md`'s *a capture names nothing else running in this repository as its first
control* was violated knowingly. **They are upper bounds, which is the one thing a spoiled reading is
still good for** — and what the decision needed was the **ratio and the sign**, not the number.
⚠ **F7's 88 seconds is not this cost and must not be quoted as one.**

✅ **What shipped is a table of its own.** `TerrainCellTable` — dense, one row per Cell, **the slot IS
`CellGrid.Index`**, every row allocated in the constructor and none ever freed, so there is no
residency index, no `Ensure` and no coordinate columns. `LayerCellTable` stays sparse and the four
Layer passes are untouched.

***The general shape is worth keeping: the two tables are per-Cell alike and their LIFETIMES are
opposite.*** A Layer row means *something happened here*, so that table is sparse by design and every
pass over it is `O(live rows)`. Terrain is dense by nature — every Cell is made of something on the
Tick the world is made. **Putting a dense fact in a sparse table is not a storage change; it is a cost
change to four passes that never asked about terrain**, which is F7's wire approached from the other
side and tripped deliberately rather than by surprise.

⚠ **The generator authors NO number, and that was a constraint rather than a flourish.** A shape
constant in `TerrainGenerator` would be both a tuning number outside the Ruleset (`adr/0015`) and a
hash-bearing number with no ratifier (`adr/0052`), and the `[[terrain]]` schema deliberately carries
no share, threshold or feature size. So every quantity is **derived**: the octave ladder from the map
being a power of two Cells across, the amplitudes from the octave doubling, the five bands from there
being five types. **The bands are equal in WIDTH against the range that key actually produced**, which
buys two properties at once — a bell-shaped field puts most Cells in the middle band, so `ordinary` is
the plurality and rock and marsh are uncommon **by construction rather than by a share anybody chose**;
and the lowest and highest Cells fall in the outer bands **whatever the seed**, so *all five types
exist* is a property of the construction and not of the fixture's luck.

⚠ **Two things the suite caught that this task owed elsewhere** (`adr/0073`), and both are worth
naming because neither was predicted:

- **`Ruleset.WithLayers` is a hand-spelled `with`** and silently drops a new property.
  `RulesetWithLayersTests` exists for exactly this and fired.
- 🔴 **`PopulateCommandTests` asserted *the city is a function of its size and not of the seed*, and
  `adr/0157` makes that false ON PURPOSE.** The assertion moved from the State Hash down to the tables
  under it, and is **stronger** for it: the old form said *nothing differs* and could only ever be
  relaxed to *something differs*, where the new one **names the single table that may**, so a second
  table starting to draw fails it — which the hash comparison could no longer do at all.
  ***A test whose claim a decision falsifies is narrowed to what still holds, never deleted.***

🔴 **MEASURED 2026-08-23 ON A QUIET MACHINE, and the tier really did move: 3m11s → 4m19s.** Same box,
same command, `b0d0905` against `978e682`, with nothing else running in this repository —
**+68 seconds** for **+35 tests**. ⚠ **The contended pair that preceded it (4m12s) was within noise of
the quiet reading**, so ***contention was never the cause and the earlier hedge was hedging the wrong
variable.***

🔴 **AND BOTH PREDICTED CAUSES WERE WRONG, which is why this is a finding and not a line in a table.**
`TerrainCellTable`'s constructor allocates its 262,144 rows in **1.9 ms**, and
`TerrainGenerator.LayInto` costs **9.9 ms** — per *world*, and the suite does not make six thousand
worlds. ***Two plausible stories were priced and neither could pay the bill***, which is `adr/0043`
arriving at a performance claim: the likelier story is not the measurement.

🔴 **THE COST IS THE FOLD, AND IT IS PAID PER TICK.** `Simulation.VerifyDecideWritesNothing` is **on
by default** and folds the whole world **twice a Tick**. Measured on `minimal.toml` at 1,000 Citizens
(`TerrainFoldCostTests`):

| | |
|---|---|
| the terrain table's fold alone | **1.89 ms** |
| a whole-world fold, terrain included | **2.08 ms** |
| **one Tick, decide-guard ON** (the default) | **4.14 ms** |
| **one Tick, decide-guard OFF** | **0.03 ms** |

***Terrain is now ninety per cent of what a fold walks***, and the guard walks it twice a Tick, so a
Tick under the guard is **138×** a Tick without it.

⚠ **This is the hazard `CLAUDE.md` already records, arriving from a third direction.** That file warns
that a full-world fold on `bordered.toml` is ~19 ms and that the guard makes a run on it *~75× slower
and says nothing* — **because that file paves the lattice to the boundary and makes the LAYER table
dense**. F7 hit the same wire by giving Sealing a write path. **This makes a *terrain* table dense in
every world, so the warning that was a property of one shipped file is now a property of all of them.**
***A caveat attached to one Ruleset did not travel, because the thing it was about was never the
Ruleset.***

⚠ **It was a GUARD's cost and not the city's, and that distinction is what made it fixable.** Nothing
in a Tick writes the terrain column — there is no terraforming — so ***the guard was folding, twice a
Tick, a table that cannot have changed.***

✅ **FIXED, with the user in the room, and the fix is a narrowing rather than a removal.** The guard
now folds **`World.TablesAPhaseCanWrite`** — the composition minus the tables no phase writes — while
**the real State Hash still folds everything**, because that is `05 §4`'s coverage guarantee and a
column outside it is a column the hash cannot see.

🔴 **What pays for the exclusion is the second half, and skipping it would have been the silent hole
this project keeps finding.** `WorldInvariants.TerrainIsUnchangedSinceItWasLaid` runs at the
**end-of-run** tier and compares the table against its fingerprint at the moment the ground was laid.
It is **broader** than what the guard was saying — the guard could only report that *Decide* had not
moved terrain between two folds, and this reports that **no phase at all** did. `02 §10`'s own rule is
that invariants are sorted by frequency; ***a check on a thing that cannot change belongs at the
lowest frequency there is***, and running it twice a Tick was the defect rather than the diligence.

| after the fix | |
|---|---|
| one Tick, decide-guard ON | **0.37 ms** — was 4.14 |
| one Tick, decide-guard OFF | 0.06 ms |
| **the assertion tier** | **3m10s at 2,101 tests** — against **3m11s at 2,064** before the milestone |

🔴 ⚠ **The first version of the invariant asked the WRONG QUESTION and 63 tests said so.** It asked
*does the terrain match the world key*, which is the strongest statement available and is unrunnable:
**a world built through the cold API is never populated**, so its ground has never been laid, and **a
loaded world is restored rather than generated**. ***A check derived from the seed cannot be run
against worlds that never met the seed.*** It asks *has it changed* instead — and a load restoring the
wrong terrain is already [`adr/0112`](../docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md)'s
job, done, so the narrower question leaves no gap.

⚠ **Two smaller things the build refused, both correctly.** `BOR0901` rejected the remembered
fingerprint as a field of a `[Table]` type — every field there is a declared column (`adr/0003`), and
a fingerprint is not per-row state — so it lives on `MapLayers` and the table only computes it. And
generating the terrain and recording the fingerprint are now **one call**, `MapLayers.LayTerrain`:
***two steps that must not come apart belong behind one door***, since a caller doing the first and
forgetting the second leaves a world that reports as corrupt at the end of a run for no reason
connected to the cause.

### F9 — task 5 found nothing, and the reason it found nothing is the finding

**Fertility was built on 2026-08-23 and the build raised no question the scoping had not already
answered.** `adr/0155` had decided the shape, the weighting, the derivation of `w_s`, the no-clamp and
the saturation before a line of it existed; the task was transcription. ***That is what a decision
session is supposed to buy, and it is worth recording on the one occasion it plainly did.***

🔴 **What the build did surface is that the producer has no consumer, and it is sharper at the
keyboard than it was on paper.** Every test in `FertilityTests` reads `MapLayers.Fertility` directly,
because there is nothing else to read it through — no farm Rule, no panel, no Layer. So **every
assertion in the file is arithmetic**, and not one of them can fail because the *city* is wrong.
Under [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) that is
*unbuilt* rather than a defect, and the answer is the first farm and not a compensating knob here.

⚠ **`w_p` = 4 is ANCHORED on a mock-up, and the anchor is a specimen in `adr/0022` rather than a
measurement.** It is the only sentence in the corpus that says what a plume ought to cost a farm.
[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
is satisfied because nothing is being *claimed* as settled — the §D1 row names milestone 24's long run
as the ratifier and states the refuting reading in both directions.

⚠ **A second, smaller thing the unit itself raised.** A whole percent is coarse at this scale: one
step is `0.01 × 12 = 0.12` of a ceiling of `1.0` under a strong plume, so the weight has roughly eight
usable settings before it sterilises unsealed ground. ***The ratifier may reopen the unit rather than
the value***, and §D1 now says so — which is a thing `adr/0052`'s "named ratifier" habit does not ask
for and probably should.

⚠ **One test-writing trap, recorded because the next person meets it too.** The first Cell of a given
terrain type on a generated map is usually a Cell the city is standing on — `SyntheticCity` lays roads
and Buildings before any test sees the world, and both Seal. A test that read *the first `rock` Cell*
and expected the Ruleset's number back failed for a reason with nothing to do with Fertility.
`FertilityTests.UntouchedCellOf` is the fixture helper that has to exist, and its second half is the
load-bearing one.

### F10 — task 8a's subject had no disposition, its rate had no owner, and the generator's output had never been looked at

**Three findings, and the middle one is the largest.**

🔴 **Woodland was named in eleven places and given a disposition in none**, which is recorded in
decision 8 and is *the fourth artefact in this milestone* with that shape — after precondition 1,
decision 7 and **F8**. ***The pattern has stopped being a surprise and should now be the expected
case***: a task specified in one line, in a milestone whose other tasks each needed three decisions,
is a task nobody has costed rather than a task that is small.

🔴 **`adr/0022:137` calls regrowth speed *"the load-bearing constant"* and it has never had an
owner** — no ADR, no §D row, no named ratifier, no Ruleset key, and `adr/0052` never applied. It is
now decision 10 and task 8b's. ***A hash-bearing constant an ADR calls load-bearing, carried for the
life of the project by one sentence in one document.***

🔴 **And the ADR was corrected hours after it was written, about the table.** See decision 8. The
correction cost an hour and would have cost nothing: `TerrainCellTable`'s own header already carried
both the argument and the measurement, so [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
was paying out and nobody had read the payment. ⚠ **The first draft also quoted F7's 88 seconds into
that correction block**, which the same header forbids by name — ***`plans/0012` Cause 5, committed an
hour after reading the rule against it, by the author of the correction.*** Withdrawn the same
sitting.

### What the generator actually plants, which nobody had asked

⚠ **`WoodlandGenerator` scales against `ValueNoise.Ceiling` — the range the sum *could* produce —
rather than against the range a key realised, and that is the opposite of what `TerrainGenerator`
does.** Terrain self-normalises so that *all five types exist* cannot become a property of the seed;
Woodland must not, because `adr/0022:38` rests a design decision on *"a heavily forested seed"*.
***The same noise, read two ways, and each reading has its reason.***

🔴 **The realised output was measured and it is a band in the middle of the Cell, never the whole of
it.** `WoodlandMeasurementTests`, five keys:

| key | min | max | mean | cover | bare Cells | full Cells |
|---|---|---|---|---|---|---|
| `0x0001` | 119 | 857 | 522 | 51% | **0** | **0** |
| `0x0002` | 174 | 824 | 456 | 44% | **0** | **0** |
| `0xBEEF` | 271 | 824 | 542 | 52% | **0** | **0** |
| `0xF0E5` | 146 | 748 | 457 | 44% | **0** | **0** |
| `0x5EA1` | 253 | 908 | 531 | 51% | **0** | **0** |

⚠ **Two consequences a reader needs and will not guess.** ***There is no bare ground anywhere on any
world***, so every first Building clears forest and the *"clear for lumber now, or keep the forest"*
decision `01 §3` puts in the opening exists in every Cell equally rather than being a siting question.
And ***`adr/0022`'s "heavily forested seed" is weakly satisfied at best*** — cover spans **44% to 52%**
across five keys, which is a difference nobody would feel. The per-Cell spread is real (119 to 908 is a
**7.6× Timber yield difference**) and it is the spread rather than the total that carries the design.

⚠ **Neither is a defect and both were derived rather than chosen**, which is why they are recorded here
instead of being tuned away. A sum of uniforms concentrates; that is arithmetic. **Changing it means
authoring a shape**, which is an `adr/0052` number and a decision this task deliberately does not take.
🔴 **It is filed rather than fixed, and the trigger is the first consumer** — the Timber chain is
unplaced, so nothing can yet say whether a uniformly half-wooded map plays badly.

### The cost, and one test that predicted its own failure

⚠ **`PopulateCommandTests.The_city_is_a_function_of_its_size_and_the_seed_reaches_only_the_ground`
failed on the day 8a landed, and its own doc-comment had predicted exactly that** — *"a second table
starting to draw fails it, which the hash comparison could no longer do at all."* Woodland is the
second table. ***The test was written as a prediction and paid out as one***, and the exception list is
now two tables asserted to move **separately**, because they draw on different `purpose_tag`s and one
fold would pass whenever either moved.

🔴 **Woodland is dense AND writable, so unlike terrain it cannot leave the Decide guard.**
`World.TablesAPhaseCanWrite` excludes terrain because no phase writes it; `MapLayers.Seal` writes
Woodland every time a Building is created, so excluding it would be the silent hole that document
warns about. **Measured, `TerrainFoldCostTests`:** a Tick with the guard **on** went from **4.14 ms**
(post-task-2, recorded in `World.cs`) to **4.92 ms** — about **+19%** — against **0.01 ms** with the
guard off. ⚠ **The machine was not verified quiet and this reading names none**, so under
[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)
***no document may quote it***; what a spoiled reading still settles is the **ratio and the sign**, and
those are that the guard's cost rose by about a fifth and nothing else moved. **A figure for
[`0013`](0013-tick-budget.md) is a deliberate act on the reference machine and has not been taken.**


### F4 — the milestone's central term was named in six documents and defined in none, and the missing definition was load-bearing

`terrain suitability` had no `CONTEXT.md` entry, no unit, no range and no sign, and there was no entry for
**Terrain** either. ✅ **Settled the same day by decision 1 and
[`adr/0154`](../docs/adr/0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)**:
the term is renamed **Base Fertility**, it means *the yield of untouched ground* — **Fertility's ceiling**
— it is **suitable for farming and nothing else**, and it is **Ruleset data keyed by terrain type** rather
than a field.

🔴 **The finding is not that a term was undefined but that the gap produced a design decision.**
`adr/0124` specified *"terrain suitability baked at world creation into a stored per-Cell column"* by
reasoning from the word **terrain** in a name no `CONTEXT.md` entry constrained — while `02 §2.3` says
*"the generator places Woodland and nothing else"* and `adr/0022`, which that ADR cites throughout,
argues against the field at length. ***A badly named term is a design defect waiting for somebody to
reason from the name.***

⚠ **The corollary is the one to carry**: `adr/0124` **sensed a real hole and named the wrong occupant**.
A per-Cell terrain column is genuinely owed — `CONTEXT.md` → Sealing keys the decay rate by terrain type
and nothing stores the type. **One column, two consumers**, and the milestone's size is unchanged.
⚠ **Fertility's three-unit subtraction survives as decision 1b**, which is split out rather than bundled.

### F5 — the generator version's absence is a decision, not a hole, and reading it as a hole would invert a guard

`adr/0111` struck `05 §7`'s third row and named the trigger for its return. **A placeholder
`generator_version = 1` agrees with itself and defeats the refusal that an absent version achieves
through the format version.** Recorded here because the absence looks exactly like the omission
`06:330` says was paid.

### F6 — world creation is a command inside Phase 0, so "at world creation" and "never inside a Tick" need reconciling

`SyntheticCity.PopulateInto` runs from `CommandKind.Populate` at `Simulation.cs:391`. `adr/0021`'s rule
survives — it governs **reads** and the bake is a **write** — but a mechanical check phrased against
phases would go red on the generator. ✅ **Discharged by decision 3**, which restated the rule against
**state** and amended `adr/0021` in place: *terrain height is not state.* ⚠ **Nothing enforces phase
discipline in this build anyway** — `TickPhase` is referenced by its own file and `Simulation.cs` and by
nothing else, so a phase-shaped rule had no enforcer to be checkable by.

### F11 — task 6's number was found by trying to avoid it, and two defects in the drainage were found by testing

**The task was scoped as *the water graph and a `BinOwnerKind`* and neither half survived contact.**

**The `BinOwnerKind` half is not 6a's at all.** `CONTEXT.md` → Water Body and `adr/0034` §4 both said
a Water Body's Bin holds *"the Waste family"* — ⚠ **both corrected 2026-08-24, see decision 12** — and
on the day 6a was scoped **what family Waste is appeared to have four answers in the corpus**:
`CONTEXT.md` → Water Body calls it a family of its own, `CONTEXT.md` → Resource puts it in **Good**,
`04 §1` calls it *"service capacity … coverage rather than a Good"*, and `adr/0031`'s own table lists
it among the four escapees quoting `03 §1`'s *"production → flow → treatment"*. **`ResourceFamily` has
no `Waste` member and nothing declares `Utility` either.** So the Bin was split out as **6b** and
[`docs/references.md`](../docs/references.md) **§10** was written: four independent lineages ship
**two** waste mechanisms and put the seam on `adr/0031`'s own axis.

⚠ **The four answers turned out to be one split and two stale sentences, and 6b was unblocked the same
day** — decision 12. ***The survey that found the seam is what made the corpus's own copy of it legible;
the conflict was real to read and not real to resolve.*** **This is the finding to keep**: two copies of
one wrong sentence, plus a quotation of superseded prose, read as a three-way disagreement for long
enough to split a task around it.

**The graph half needed a number, and the recommendation was to avoid it.** The offer was *basins
only* — priority-flood the height field, no threshold, no §D row, no ratifier. **It was refused because
that world has no sea, no bay and no coast**, all three of which `CONTEXT.md` names. `adr/0159` is the
record; §D1 carries the row.

🔴 **Two defects in the drainage walk were found by MEASURING and neither would have been found by
argument.** This is the finding worth keeping.

1. **The spill walk fell back into the body it came from.** A rim Cell is by construction the lowest
   *dry* ground touching a body, so its lowest neighbour is almost always the body itself: the walk
   descended straight back in, terminated at the body's deepest Cell, and reported *no downstream*.
   The instrument said **60–70% of bodies drain nowhere**, which read as a property of the terrain and
   was a property of the code. Excluding the source body **roughly doubled** the edge count.
2. **The graph then had cycles in it.** Two basins spill into each other across a ridge and each walk
   is individually correct. `WaterTests.Every_outflow_reaches_the_map_edge` caught it — an assertion
   written *because reading the walk made it look plausible*, not because anything had failed. The fix
   is a strict order on **spill elevation**, ties broken by Cell index.

⚠ **After both fixes 35–60% of bodies are still endorheic, and that one is real.** A spill can descend
into a *dry* hollow, and the model stops there; going further needs to know how full the hollow gets,
which is a volume, which is a Bin. ***The coarseness and the blocked task are the same fact.***

⚠ **The instrument was itself wrong first.** It counted *"draining off-map"* as one number, conflating
the designed terminus with the coarseness — so the first reading looked like a clean result. Splitting
the counter is what exposed both defects. ***A measurement that sums two causes reports neither.***

**And the ordinals cost a message rather than a merge.** 6a needed `adr/0159`, one past the block
reserved on 2026-08-24; the milestone 27 session was told before it was taken and agreed. The same
protocol had already found that `PlanIdentityTests` cannot see two documents claiming one number —
that check now exists on `main`, written by that session from this branch's sighting.

---

## Where this sits

**`06` milestone 24's first half.** Ungated. Scoped out of sequence on 2026-08-22 because terrain has
one producer and no upstream. The second half — **Shocks, Disasters, the Intensity Dial, Modes and the
lock policy** — is **UNPLACED** pending 13, 15 and 16, and F2 is its record.

### F12 — Sealing's decay had never run, and the operator waiting for it was broken

**Task 4 scheduled a method with no callers and found it did not work.**

`MapLayers.DecaySealing` shipped with milestone 9, correct-looking and never invoked: `MapLayers.Step`
did not call it, `LayerSchedule.For` answered `Never` for `Layer.Sealing`, and every shipped Ruleset
stated `[layers] sealing_decay_tau = 0`. **Three independent reasons for it to do nothing**, each
documented as deliberate, and between them they hid that the arithmetic underneath was wrong.

**The defect is that `value -= RoundDiv(value, tau)` never reaches zero.** The decrement rounds to
nothing once the value falls below `tau ÷ 2`, so ground settles at a permanent residue. Measured before
the fix: **tau 8 stalls at 3, tau 64 at 31, tau 600 at 299** — and **tau 2400 never moves at all**,
because `RoundDiv(1024, 2400)` is zero on the *first* update and a fully-sealed Cell takes no first
step. ***An endpoint stated in Days cannot be delivered by a curve that never arrives***, and
`CONTEXT.md` → Sealing states one: *"floodplain may recover over hundreds of Days."*

**Fixed by flooring the step at one Tile while the value is positive**, which makes the tail linear
where the quantity is small and leaves it exponential where it is large.

🔴 **And the paper derivation of what that costs was wrong, which is the second finding.** The caveat
first written into three documents was *a tau is not a recovery time and the two differ by about
7.4×* — from `tau × ln(1024) + tau ÷ 2`, which is the recovery time of a tau smaller than the residue
and wrong for every value actually shipped. `SealingRecoveryMeasurementTests` was written to check it
and refuted it on its first run: the real multiple is **tau-dependent**, `tau × ln(2 × TilesInCell ÷
tau) + tau ÷ 2`, running from **4.1×** at tau 48 down to **2.9×** at tau 160. ⚠ **Floodplain's tau
moved 32 → 48 in the same edit**, because the measurement put 32 at **143 Days** and `CONTEXT.md` says
*hundreds of Days*. **Measured, from a Cell sealed to all 1,024 Tiles:**

| type | tau | Days to bare ground | half gone at |
|---|---|---|---|
| `floodplain` | 48 | **197** | 33 |
| `marsh` | 64 | **244** | 44 |
| `ordinary` | 96 | **329** | 66 |
| `thin_soil` | 160 | **468** | 112 |
| `rock` | 0 | **never** | — |

⚠ ***A multiplier derived rather than measured is a caveat that travels wrong***, which is `plans/0012`
**Cause 5** happening to the clause written to prevent it. The three documents now say **quote the
Days, never the tau**, and carry the table rather than a ratio.

**Three things about how it was found are worth more than the fix.**

⚠ **A stated absence is not the same as a tested one.** `sealing_decay_tau = 0` was *deliberate* and
every Ruleset header explained it — and a deliberate zero exercises the same amount of code as a
forgotten one. `adr/0152` even leaned on this key as its **precedent** for an admissible zero rate,
citing a mechanism nobody had run. ***A precedent taken from an untested path carries the path's
silence with it.***

⚠ **The key had to MOVE, and a moved key is refused where it used to be.** `02 §2.4` keys the rate by
terrain type, so a single `[layers]` global was always a placeholder for a lookup; the reconciliation
was filed as task 4's by `adr/0157` and is discharged here. A file still writing the old key is
**refused with a message naming `[[terrain]]`**, rather than having its stated rate silently ignored —
which is `adr/0123`'s *present and permanently zero* failure arriving through a stale key instead of a
stale term.

⚠ **And the cadence was chosen in the unit the design states its intent in.** `adr/0044` makes a Layer
cadence the designer's number; this one is **one Day** because `CONTEXT.md` says *hundreds of Days*, so
the tau is a count of Days and nothing converts. ⚠ **That is a coincidence being relied on and it is
flagged as one** in the §D1 row: the tau is authored as a raw count of updates rather than as a
duration, so ***moving the Sealing period would silently rescale all five values*** — the
`pollution_decay_ticks` trap (`plans/0015` decision owed 2) arriving at a second key and **not** dodged.

**Routed on the day** (`adr/0073`): the stalled decay is fixed in `MapLayers` rather than worked around;
the two doc-comments in `LayerSchedule` still quoting a **Day as 8,192 Ticks** — stale since `adr/0094`
made it 2,048 — were corrected in passing, and they are `plans/0012` **Cause 1** in the one place the
corpus checks cannot see, because ***every mechanical check in `tests/Borough.Tests/Corpus/` is
document-to-document and a doc-comment is neither end of that.***

### F13 — regrowth needed a ceiling, and both ceilings that cost nothing were wrong

**The task was scoped as *a cadence and a rate* and it needed a saved column.**

`WoodlandGenerator` lays forest from its own noise field and `MapLayers.Seal` takes it away; nothing
recorded **what had been there**. So the first question regrowth asks — *grow back to what?* — had no
answer in the build, and the two answers that need no storage are both wrong:

- **Toward the bare Cell**, `TilesInCell − Sealing`. Every unbuilt Cell becomes full forest given
  time, which erases the property `adr/0022` put Woodland in for: ***"a heavily forested seed is a
  Materials-rich, farmland-poor start" is a statement about the SEED***, and a map that converges on
  uniform forest has no seeds.
- **Toward the terrain type's own share**, from `[[terrain]]`. It would disagree with the generator on
  the **first Tick**, because `WoodlandGenerator` reads `PurposeTag.Woodland`'s field and not terrain —
  so a Cell laid above its type's ceiling would start shrinking, which looks exactly like a defect.

**So `WoodlandCellTable` gained `Potential`, saved, written once by `Lay`.** ⚠ **Saved rather than
derived is forced rather than chosen** — it is a function of the `WorldKey`, so it looks derivable, but
`World`'s own note on `_tables` says a save does not carry the `WorldKey` back into the generator,
which is why `TerrainCellTable` is saved for the same reason. ***A column nothing can rebuild is not
derived state, however cheap its formula looks.***

✅ **And the pair turned out to be a readout the design had already asked for.** `adr/0022` is titled
*land is a stock the city spends*; `Potential − Tiles` summed over the map is exactly what has been
spent. The column was added for the ceiling and the readout came free.

**Three things this task did differently because of F12, one milestone-task earlier.**

⚠ **The curve is LINEAR, and that is task 4's lesson applied rather than a modelling preference.** An
exponential approach would need a caveat about the multiplier between its time constant and the felt
duration — and F12 is that caveat being derived on paper, written into three documents, and refuted by
the first instrument that measured it. ***A linear rate makes the authored number and the felt number
the same number***, so there is nothing to mis-quote.

⚠ **The step is floored at one Tile and the loader refuses a duration past a Cell, which is F12's
stall wearing the other sign.** `RoundDiv(1024, days)` is zero past `TilesInCell`, so an authored 2,000
Days would put back *nothing, for ever* while reading as a very slow rate. It is guarded **twice**,
because the first line of defence for the identical bug turned out to be nobody at all.

⚠ **The whole-map sweep was measured on the day it was written**, which is `adr/0073` and **F7** — this
milestone has already been burned once by an unmeasured whole-world pass. **1.357 ms a pass**, on the
reference machine, `varied.toml` at 4,000 Citizens: **0.004% of the budget amortised and 8.7% of a
single Tick**, and it is the spike rather than the average that `05 §9` cares about. Routed to
[`plans/0013`](0013-tick-budget.md), which also gained a row saying **task 4's decay pass is still
unmeasured**.

🔴 **And the measurement found a caveat about what the key means.** The instrument put **26.6%** of a
map's forest back in 65 passes of a 512-Day rate, where the duration alone predicts 12.7%. The step is
**absolute**, so ***the authored duration is a FULL Cell's recovery time and not every Cell's*** — a
Cell the seed left a quarter wooded returns in a quarter of the stated Days. Scaling each Cell's step
by its own ceiling is refused, because that division rounds to zero on exactly the thinly wooded Cells
it would exist to slow: the fix reintroducing the defect. **The clause is written into the key's
doc-comment, `varied.toml`'s header, `CONTEXT.md` and the §D1 row, and asserted by a test**, on
`plans/0012` **Cause 5** — ***found by measuring, and it would not have been found by reading.***

⚠ **The two halves of milestone 24 are one loop and neither task closes it.** Task 4 lets Sealing fall;
8b lets Woodland rise into the room it leaves. A paved Cell takes its ground back over **197–468 Days**
and its trees back over **512** — roughly 700 end to end, about eighteen hours of play at 1× — and that
compound is the ratchet `adr/0022` is protecting. ⚠ **`adr/0158`'s stated trap is now live**: both
numbers are unratified, so a long run reading one against the other has **two unratified numbers
measuring each other**. ***The bound supplies a readout and not a ratifier***, so task 10 reads each
against its own stated duration.

