# 0038 — Terrain and the land rows: `06` milestone 24's first half

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). The milestone and its named risk are
> [`06`](../docs/06-roadmap.md)'s. What is done is [`0003`](0003-build-plan.md)'s. This document owns
> this slice's decisions, its tasks and its findings, and nothing else.

---

## Status

🔵 **SCOPED 2026-08-22, out of sequence, and SPLIT in the scoping.** **Task 1 is done. Task 3 is
CODE-COMPLETE and BLOCKED — see F7**; it was built ahead of task 2 because the Sealing write path needs
no terrain, and running it turned up a whole-map cost that
[`0002`](0002-open-questions.md) §C now owns. **Task 2 has not started.** **Decisions 1, 1b, 2, 3 and 4 are settled**
([`adr/0139`](../docs/adr/0139-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md)–[`adr/0143`](../docs/adr/0143-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md));
**5 and 6 are open**.

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
[`adr/0139`](../docs/adr/0139-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md),
2026-08-22.** `06`'s row 24 and both inventory rows are rewritten; the Shocks-and-Dial half is
**UNPLACED**. ⚠ **The trigger fired on the half `adr/0124` did not expect** — that ADR offered a *land
systems on terrain* seam, and the seam turned out to be the **dial**.

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

✅ **RESOLVED 2026-08-22 by decision 1 and [`adr/0140`](../docs/adr/0140-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md).**
The term is renamed **Base Fertility**, `CONTEXT.md` gains it and **Terrain**, and the undefined
quantity turned out to be **Ruleset data rather than a field**. 🔴 ***The missing definition was not a
documentation gap — it was load-bearing***: `adr/0124` specified a per-Cell column by reasoning from a
name that no entry constrained, and the column it specified is the one thing this decision removes.
⚠ **The half that remains open is the arithmetic**, now decision **1b**.

### 🔴 Precondition 2 — **Sealing has THREE blockers and `adr/0124` enumerated two**

`adr/0124` records that Sealing's decay is *"blocked twice"*: `sealing_decay_tau = 0` in every shipped
Ruleset, and `MapLayers.Step` never calling `DecaySealing`. **Both are true. Both are downstream of a
third, which no document names.**

🔴 **`MapLayers.Seal(Cells, Cells, int)` — `Space/MapLayers.cs:393` — has NO caller in `src/`.**
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
not state* — and `adr/0142` is what makes that checkable, by storing no height at all. ⚠ **There is no
bake left to place**, and the mechanical check is **owed with terraforming** rather than now.

---

## Open decisions this half owes — **1, 1b, 2, 3 and 4 SETTLED; OPEN: 5, 6**

⚠ **None is settled and none should be settled by argument if a measurement would settle it**
([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)).
Each carries its type.

### 1. ✅ SETTLED 2026-08-22 — what *is* terrain suitability? **It is Base Fertility, and it is not a field**

✅ **[`adr/0140`](../docs/adr/0140-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md),
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

✅ **[`adr/0141`](../docs/adr/0141-fertility-composes-with-weights-and-only-one-of-them-is-a-number-anybody-chooses.md),
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

✅ **[`adr/0142`](../docs/adr/0142-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md),
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
grade does not ship** (`adr/0142`). Terrain goes first because it is the ground, not because anything
downstream reads it.

✅ **The rule is restated against STATE rather than against time**, and
[`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) is amended
in place rather than a fourth ADR being written — the design content is `adr/0142`'s, and **a second home
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

✅ **[`adr/0143`](../docs/adr/0143-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md),
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

### 5. What ratifies Sealing's decay cadence and rate? — *measurable*, and it needs `plans/0002` §D rows

Two hash-bearing world-creation numbers under
[`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md),
each owing a **machine, a world and a quantity** under
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).
⚠ **The world does not exist**: `adr/0124` names *a world with varied terrain* as the ratifier's world,
and no shipped Ruleset has any terrain at all. ***So the world is part of the deliverable***, which is
milestone 12 task 1's lesson (`twinned.toml` was a WORLD and not code) arriving before this milestone
instead of during it.

### 6. Does `adr/0021`'s **seed + edits** save ship here? — *arguable*. **Recommendation: no**

See precondition 3. Shipping it brings the generator version back and inverts a guard that currently
works. **Recommendation: the baked column is `Saved`, the save carries it, `adr/0111` stands, and this
decision is recorded so the absence is not later read as an omission.**

---

## Tasks

⚠ **Provisional below task 2**: decisions 1 and 2 change what tasks 3 onward build.

| # | Task | Depends on |
|---|---|---|
| **1** | ✅ **DONE 2026-08-22** — `CONTEXT.md` gains **Terrain** and **Base Fertility**, the rename lands in `02 §2.3`, `04 §1` and `MapLayers`, `adr/0022` and `adr/0124` are amended rather than rewritten, and `06`'s row 24 is rewritten for the split ([`adr/0139`](../docs/adr/0139-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md), [`adr/0140`](../docs/adr/0140-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)) | decisions 1, 2 |
| **2** | **The terrain generator and the per-Cell terrain TYPE column** — `(saved AND hashed)`, from the `WorldKey`, with a `[terrain]` Ruleset table keying **Base Fertility** and the **Sealing decay rate** off the type, plus **a shipped Ruleset with varied terrain**. ⚠ **The column holds the type and nothing is baked** (`adr/0140`). ⚠ **The world is part of this task and not a follow-up** | 1, decision 3 |
| **3** | 🟡 **BUILT 2026-08-22, BLOCKED ON A COST** — see F7. **The Sealing write path** — construction Seals, **at the point of laying and never reconstructed from a Segment's endpoints** ([`adr/0143`](../docs/adr/0143-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md)). Touches the four `RoadGenerator.Layout` writers, `SyntheticCity.Subdivide` and `ZoneRuleEngine.Create`. ⚠ **Authors no number and opens no §D row.** Precondition 2's third blocker, and upstream of the two `adr/0124` names. 🔴 Moves every State Hash | 2 |
| **4** | **Sealing's decay** — a cadence in `LayerSchedule.For`, a rate keyed by terrain type, `DecaySealing` scheduled in `MapLayers.Step`. Two §D1 rows with named ratifiers | 3, decision 5 |
| **5** | **Fertility** — the `throw` at `MapLayers.cs:578` becomes a composition at the point of use | 2, 3 |
| **6** | **Water Bodies** — the water graph, and a fifth `BinOwnerKind`. ⚠ **The downstream ordering is generator OUTPUT, not a height computation** (`adr/0142`): `CONTEXT.md` → Water Body states *an outflow rate to the next body downstream*, which is an **edge** | 2 |
| **7** | **Desirability's shoreline term** — `w₅`, and the caveat test `adr/0123` requires | 6 |
| **8** | **Woodland and replanting** — regrowth on unsealed, unoccupied land | 2, 3 |
| **9** | **Hazard Regions** — derived at generation, never read in a Tick. ⚠ **Floodplain depth is stored SPARSELY, where the floodplain is** (`adr/0142`), because `01 §5.2` spreads Flood *by depth* and a whole-map height field is what this milestone does not build | 2 |
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

🟡 **Task 3 is CODE-COMPLETE and must not be committed as done.** Sealing is written, measured, and
correct; the assertion tier is not usable with it in until `plans/0002` §C's *does the land value target
pass stagger?* is answered.

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

🔴 **Two claims in [`adr/0143`](../docs/adr/0143-sealing-authors-no-width-and-a-road-seals-where-it-is-laid-not-where-its-endpoints-are.md)
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

### F4 — the milestone's central term was named in six documents and defined in none, and the missing definition was load-bearing

`terrain suitability` had no `CONTEXT.md` entry, no unit, no range and no sign, and there was no entry for
**Terrain** either. ✅ **Settled the same day by decision 1 and
[`adr/0140`](../docs/adr/0140-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)**:
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

---

## Where this sits

**`06` milestone 24's first half.** Ungated. Scoped out of sequence on 2026-08-22 because terrain has
one producer and no upstream. The second half — **Shocks, Disasters, the Intensity Dial, Modes and the
lock policy** — is **UNPLACED** pending 13, 15 and 16, and F2 is its record.
