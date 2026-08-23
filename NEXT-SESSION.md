# Starter prompt — milestone 24 task 2, second half

Paste everything below the rule into a cold session started in `/home/christian/Code/city-sim-q8`.
Delete this file when task 2 closes.

---

Finish **milestone 24 task 2** — the terrain generator and the per-Cell terrain type column — in the
`city-sim-q8` worktree, branch `milestone-24-terrain-scoping`.

**Do not run `/next`.** This is the orientation, and `/next` would send you to milestone 25, which
another session owns.

**This is a build session. Write code.** The design is settled and the corpus paperwork is done; if
you find yourself editing `plans/0012` or negotiating with another session, stop and get back to the
build.

## Read these two, and nothing else first

1. **`docs/adr/0157-terrain-is-five-types-and-base-fertility-varies-across-them-because-a-category-exclusion-is-not-an-overlay.md`**
   — the decision you are implementing. Short.
2. **`plans/0041-terrain-and-the-land-rows.md`** — its Status, decision 3 (where the generator pass
   goes), and the task table.

Decisions 1, 1b, 2, 3, 4 and 7 are settled. **5 and 6 are open and belong to task 4, not to you.**

## What already exists

Tasks 1 and 3 are done. Commit `d6fb2a4` landed the vocabulary, wired to nothing:

- `PurposeTag.TerrainType = 22`
- `TerrainKind` — `Ordinary`, `Rock`, `Floodplain`, `Marsh`, `ThinSoil` (`byte`-backed)
- `TerrainRuleset` — Base Fertility per type, Q16.16, with `From(...)`, `None` and `BaseFertility(kind)`

No hash has moved yet. Everything below is new work.

## Build it in this order

1. **The loader.** `[[terrain]]` in `src/Borough.Formats/RulesetLoader.cs`. Copy the `[parking]`
   pattern end to end — a field, a `case "terrain":` in `Enumerate`, a `ReadTerrain()` returning
   `TerrainRuleset`, the call in `Parse`, and a `Terrain` property on `Ruleset`. Mirror
   `tests/Borough.Tests/Formats/ParkingRulesetLoadTests.cs` for the tests.
   - ⚠ **Add `terrain` to the unknown-section refusal's list** (the `default:` case that names every
     section) or a valid file is refused.
   - Refusals: **all five types stated, each exactly once** (`TerrainRuleset.Kinds` says why in its
     remarks), unknown name refused, duplicate refused, `base_fertility_percent` required and refused
     above 100 — fully fertile is the top of the scale (`adr/0155`).
   - Percent → Q16.16 is `IntegerMath.RoundDiv(Fixed.FromInt(percent), 100)`. **Authored as an integer
     percent**, because `adr/0048` refuses unquoted decimals on the path into the simulation.
2. **The column.** `Terrain` on `LayerCellTable` (`src/Borough.Core/Space/LayerCellTable.cs`),
   declared in the constructor beside `Sealing` as `_rows.Saved<TerrainKind>("terrain")`. `Rows.Saved`
   accepts any `unmanaged` type, so the enum works directly.
3. **The generator pass.** Decision 3 settled the placement: **its own pass, called from
   `SyntheticCity.PopulateInto` between `RefuseIfPopulated(world)` and `LayLand(world, key)`** —
   `SyntheticCity.cs:103-124`. Copy `RoadGenerator.LayInto`'s already-populated refusal shape
   (`RoadGenerator.cs:127-144`). Draw with
   `Randomness.Draw(key, (ulong)CellGrid.Index(east, north), Ticks.Zero, PurposeTag.TerrainType)`.
   ⚠ **Height is a local and is stored nowhere** (`adr/0156`) — the pass may compute it and keeps only
   the type.
4. **`rulesets/varied.toml`.** The world is **part of this task, not a follow-up** — decision 5's
   ratifier needs it to exist. Header in the house style; `rulesets/twinned.toml`'s is the model. Say
   what it exists to show, what it must not be read as, and that its five Base Fertilities are
   **unratified and were chosen against no consumer**.
5. **Tests.** The loader refusals above, plus: the same `WorldKey` gives the same map, a different one
   gives a different map, and all five types appear on `varied.toml`.
6. **The re-baseline, as its own commit.** Follow `tests/Borough.Tests/Golden/README.md`. Precedent:
   `1c9ebec` (code, hashes left stale) then `af3f5fd` (re-baseline alone). Expect the two golden traces
   and `world-hash.txt` to move. `adr/0100`: moving the hash costs nothing and **must never be cited as
   a reason to defer, narrow or split work**.

## The one real decision left, and it is not paperwork

⚠ **`LayerCellTable` is SPARSE. Terrain per Cell is dense by nature.**

Task 3 already hit this: giving Sealing a write path made the table dense on `bordered.toml` for the
first time — **262,144 rows, and one Tick in 256 went from free to 88 seconds**. Written up as **F7**
in `plans/0041`; `plans/0002` §C owns the cost.

**So do not call `CellResidency.Ensure` for every Cell.** Decide deliberately, and write the reasoning
at the symbol. Two candidates:

- **Write the type only where a row already exists**, recomputing from the `WorldKey` elsewhere. Cheap,
  but the column stops being the whole truth and a save carries only part of the map's terrain.
- **Make it dense deliberately** and pay the residency cost — having first *measured* it at the shipped
  sizes rather than assuming F7's figure transfers.

⚠ **F7's 88 seconds was `bordered.toml` at whole-map residency. It is not terrain's cost — do not quote
it as one.** If you take a reading, a reading needs a quiet machine (`adr/0106`, and `CLAUDE.md`'s note
that a capture names *nothing else running in this repository* as its first control). If this becomes a
real cost question, route it to `plans/0013` and `plans/0002` rather than working around it locally
(`adr/0073`).

## Two environment traps

1. **Serena may be pointed at the wrong directory.** The server runs `--project-from-cwd` and the only
   registered project is the *shared* checkout `/home/christian/Code/city-sim`. **Probe before trusting
   it**: `get_symbols_overview` on `src/Borough.Core/Space/TerrainKind.cs`. If it says the file does not
   exist, Serena is in the wrong tree — use Bash and Read/Edit, and tell the user. A previous session
   edited the wrong tree this way.
2. **Do not merge `main` mid-task.** Another session works milestone 25 and commits there directly.
   Four files are edited on both sides and the merge will conflict for real. Finish task 2 first; the
   file-by-file map and the one line that must be *recounted* rather than resolved are in commit
   `979b1a0`'s message and in `plans/0012`'s final section. This branch owns `adr/0150`–`0157` and
   `plans/0041`; the next free ADR number here is **`0158`**.

## Conventions that will bite

- **Gate:** `dotnet test -c Release --filter "tier!=instrument"` — ~3m here at present. Do not run the
  full suite unless you are at a milestone.
- **No `float`/`double`, no `Math.*`, no raw `/`, no `System.HashCode`.** The analysers are build
  errors; `BOR0206` already caught a hand-written `GetHashCode` in this task.
- **No tuning number as a `const`** — it belongs in the Ruleset (`adr/0015`).
- **A new hash-bearing number needs a `plans/0002` §D1 row naming a machine, a world and a quantity, on
  the day it is written** (`adr/0052`). Task 2's five are already filed; do not open more without one.
- **A doc comment says where to look, never what you found** (`adr/0093`). Do not assert what a test
  does without reading it — that mistake was made twice in this repo last week.

## When task 2 is done

Update `plans/0041`'s Status and task table, and milestone 24's row in **both** `plans/0000-board.md`
and `plans/0003-build-plan.md` — they are two copies and they drifted once already this week. Then
**task 5** (Fertility — deleting the `throw` in `MapLayers.Fertility`) becomes available. Task 4 still
waits on decision 5.
