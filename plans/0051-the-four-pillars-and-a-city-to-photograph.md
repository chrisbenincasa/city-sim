# 0051 — The four pillars, and a city to photograph

Scoped 2026-09-01, against `main` at `3eb0c62`. **The first plan under
[`docs/07`](../docs/07-the-drawing.md), and the first work in this repository whose deliverable is a
picture rather than a mechanism.**

⚠ **Run in a git worktree**, because `ruleset-key-audit` held uncommitted edits to all 30 Rulesets,
`RulesetLoader.cs`, `SchemaDump.cs`, the schema and the golden fixtures at the time this was scoped.
***A new Ruleset authored on top of a moving key surface is a file that will be wrong before it is
read***, and a worktree is what made the two bodies of work separable at all.

---

## What this plan is for

[`docs/07 §9`](../docs/07-the-drawing.md) ranks what a screenshot actually costs and finds that
**most of it is not the asset pipeline**. The top four items are a sun, a camera, a lens and a
night — and none of them commits the project to a pipeline, an art style or a mesh.

**So they are the cheapest possible test of the fifth pillar** ([`docs/07 §5`](../docs/07-the-drawing.md)):
if a city with a low sun, a free camera, a tilt-shift and lit windows is not worth photographing,
then no amount of authored geometry was going to save it, and the question was answered before
anybody spent a week on meshes.

---

## The queue

| # | Row | Hash | Status |
|---|---|---|---|
| **1** | ✅ **01-09** — **A world with everything in it.** `rulesets/pictured.toml` — terrain, water, a coast, districts, gates, a market, businesses, a policy, life stages, needs, a service kind, traffic, parking, decline and maintenance, in one file. ⚠ **A visual fixture and not a demonstration** — see *What this Ruleset must not be used for* | no | |
| **2** | **The sun moves.** Light follows the clock the readout already prints, and the default frame is a low one | no | |
| **3** | **The camera can get low.** The pitch is `atan(1/√2)` and never moves. A free pitch, bounded, plus a framing that is not the orbit's | no | |
| **4** | **A lens.** Depth of field or tilt-shift, and the HUD off in a photograph | no | |
| **5** | **Night, lit from occupancy.** [`plans/0049`](0049-visuals.md) row 5, whose openings and occupancy share already ship | no | |

⚠ **Row 1 first and not because it is hardest.** Rows 2–5 are judged by eye, and *there is no shipped
world worth judging them against*: every Ruleset in the tree is a demonstration of one mechanism, so
a photograph of any of them is a photograph of an argument.

---

## What this Ruleset must not be used for

🔴 **`pictured.toml` RATIFIES NOTHING, MEASURES NOTHING, AND MUST NEVER BE CITED FOR A NUMBER.**

Every other shipped Ruleset carries a header saying it is *a demonstration rather than a city*, and
each isolates one mechanism so that a figure taken from it names one cause. **This file is the exact
opposite by construction**: it turns everything on at once, so a census taken from it has every
mechanism in the world as a candidate cause and can attribute an outcome to none of them.

⚠ **That is not a defect and it is not a compromise.** A city to photograph is a city with a coast
*and* shops *and* schools *and* traffic *and* ruins in the same frame, because a picture of a place
is a picture of things coexisting. ***What makes it useless as an instrument is exactly what makes
it useful as a subject.***

⚠ **It must therefore never enter the assertion tier as a source of a figure**, and any test that
loads it asserts a **shape** — that it parses, that it builds, that the tables are non-empty — and
never a quantity.

---

## Findings

| | |
|---|---|
| **F1** | 🔴 **A WORKTREE HAS NO GODOT ASSEMBLY, AND THE SHELL FAILS BY DRAWING NOTHING.** `godot --path src/Borough.Godot` in a fresh worktree opens a window, prints `Cannot instantiate C# script … Script: 'res://Main.cs'` **to stdout only**, and renders an empty scene — no city, no readout, no refusal a person sitting in front of it can see. ⚠ **A driven run HANGS rather than failing**: `Main` never runs, so no drive command is ever applied and `quit` never arrives. ***The fix is one command*** — `dotnet build src/Borough.Godot/Borough.Godot.csproj` — and `plans/0048` does not say so because tier 1 was written in the checkout that had already built it. ⚠ **`dotnet build` at the repository root does NOT do it**: the shell is deliberately outside the solution (`05 §1`), so the one build the corpus tells you to run is the one that leaves this broken |
| **F2** | 🔴 **A SHIPPED RULESET PANICKED AT END OF RUN AND NOTHING HAD EVER RUN IT TO ONE.** `rulesets/coastal.toml` fails `BinCapacityMatchesItsDeclaration` at the end-of-run walk, on **any** tick count: a Water Body's Bin has a real ceiling — its Cell count times `[water] capacity_per_cell` — and the invariant's owner split sent every non-Building Bin to the *must be `long.MaxValue`* branch. **`World.RebuildCapacities` knew** (milestone 24 task 6b); the check did not. ⚠ **The shape is the Business arm's, one owner later, and that comment is still in the file predicting it**: the rebuild grows an owner it can derive and the invariant keeps the old two-way split. ✅ **Fixed here**, and the State Hash is unchanged — `coastal.toml` at 64 Ticks is `0xD54C6045BD5EB417` before and after — so it is an optimisation under `05 §4` and not a design change |
| **F3** | ⚠ **A GATE AND TWO CENTRES ARE REFUSED TOGETHER, AND THE REFUSAL IS AT TICK 0 RATHER THAN AT LOAD.** `SyntheticCity.LayLand` throws: a gate stands on a map edge, so a world with a door paves to the boundary and leaves no gap for a second lattice. ***It is thrown rather than refused because neither fact is a property of the file alone*** — whether they collide depends on the population the world was allocated for. **The two centres won**, because a gate is a shed 30 km from the city and paving to the boundary turns 61 Segments into 535,817 of empty grid. `[[hinterland]]` stays; a Hinterland is a market and not a door |
| **F4** | 🔴 **`twinned.toml`'s 2,048-Tile GAP IS A DEMONSTRATION SETTING AND IT MAKES A PHOTOGRAPH IMPOSSIBLE.** Copied into this file it put two settlements **8.2 km apart**, and `Main.Frame` fits the eye to the **extent of every Lot** — so the default frame is 7.8 km up, each town is a smudge a few pixels wide, and the screen is ground. ⚠ **The gap is what `twinned.toml` EXISTS to author** (`adr/0134`'s saddle), so copying it copied the one number that had to change. **Halved to 1,024 Tiles here**, and at 20,000 Citizens the two read as two quarters of one conurbation |
| **F5** | ✅ **THE TOWN LOOKS LIKE A TOWN AT 250 m, AND THAT IS THE FIRST TIME ANYBODY HAS CHECKED.** Pitched clay roofs at varied angles, windows, gardens, a shoreline — `plans/0049`'s work reading as intended at the distance it was written for. ⚠ **Confirmation rather than a surprise**, recorded because *looking* took one drive script |
| **F6** | 🔴 **THE PHOTOGRAPH'S WORST FEATURE IS THE CURSOR, WHICH IS 128 m OF FLAT YELLOW.** At a 250 m framing the pick marker is roughly a sixth of the frame. It is a Cell-sized quad, correct as an instrument and ruinous as a subject — ***which is row 4's whole argument arriving before row 4***: a photograph needs the HUD, the readout, the tool bar and the cursor gone, and the cursor is the one that cannot be argued away as information |
| **F7** | 🔴 **THERE IS NOT ONE TREE ANYWHERE NEAR THE CITY, AND IT IS A SUPPRESSION RATHER THAN AN ABSENCE.** `Main.Trees` refuses a crown inside `_laid.Grow(_span * 0.5f)` — `plans/0049` **F8**'s guard against a tree standing in a carriageway — and `_span` is the **whole city's** extent, so the grown rectangle swallows the countryside for a full city-width in every direction. ***The larger the city, the further the woodland is pushed away***, which is the opposite of what a growing city should do to its surroundings. ⚠ **`varied.toml`'s five terrains and `woodland_regrowth_days` are in this file and invisible because of it** |
| **F11** | 🔴 **A FILE THAT TURNS EVERYTHING ON BREAKS EVERY *ONLY X DOES THIS* TEST, AND THERE ARE MORE OF THEM THAN ANYBODY HAD COUNTED.** Three fired on the first run — `TwinLatticeTests`, `DistrictWatershedTests`, `TerrainRulesetLoadTests` — each asserting that exactly one shipped Ruleset states a table. ⚠ **They are right and the file is right**: the suite encodes *one file, one mechanism*, which is what makes a census attributable, and `pictured.toml` is the deliberate exception. ***So every one of them needs an exemption, and the exemption is the same sentence each time*** — it ratifies nothing, so there is no figure for an exemption to protect. ⚠ **Expect more as the file grows**: only a mechanism that already HAS an exclusivity test can fail this way, so a green run is not evidence that the exemptions are complete |
| **F9** | 🔴 **THE WORLD AT SEED 0 IS VERY NEARLY TREELESS, AND NO RULESET CAN ASK IT NOT TO BE.** The draw list at 20,000 Citizens holds **3,509 crowns over a 22 km window** — about 5 Woodland Tiles in a 1,024-Tile Cell — so the countryside renders as bare green. ⚠ **`WoodlandGenerator` reads no Ruleset key at all, deliberately**: `adr/0022` wants *how much forest a world has* to vary from key to key, and self-normalising would delete that sentence. 🔴 **And `Borough.Godot` HAS NO `--seed`** — `Borough.Headless` does — ***so the shell can only ever show world zero***, and world zero is the one this fixture is bare on. A picture fixture that cannot choose its landscape is choosing it by omission |
| **F10** | ⚠ **`Main.Scatter` SUPPRESSED A CITY-WIDTH OF COUNTRYSIDE IN EVERY DIRECTION** — `_laid.Grow(_span * 0.5f)`, so the larger a city grew the further it pushed its own woodland away. ✅ **Replaced with the Road Graph's measured extent** plus one block of slack, which is a fact the Nodes already hold. ⚠ **It changed no picture on this world**, because F9 means there was nothing to un-suppress; it is recorded as a defect fixed rather than an improvement seen, which is the honest half |
| **F8** | ⚠ **A FLOOD'S INTERVAL IS HOW OFTEN THE DICE ARE ROLLED AND NOT HOW OFTEN ONE IS SEEN.** A flood seeds anywhere in the Hazard Region — 11,063 Cells over the whole map against a city occupying a few hundred — so **6 of 7 floods touched nothing** at `flood_every_days = 8`, and the single flood at 40 seeded 6 km from town. ⚠ **The one that did land ruined 113 of 257 standing Buildings**, so the distribution is *nothing at all* or *half the city*, with no middle |
