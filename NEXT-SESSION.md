# Starter prompt — milestone 24 task 2, second half

Paste everything below the line into a cold session started in
`/home/christian/Code/city-sim-q8`. Delete this file when the task is done.

---

You are picking up **milestone 24 task 2** in the `city-sim-q8` worktree, on branch
`milestone-24-terrain-scoping`. Do **not** run `/next` — this prompt is the orientation.

## Two environment facts that have already caused damage

1. **Serena is misconfigured in this repo and points at the WRONG directory.** The MCP server is
   started `--project-from-cwd`, and the only registered project is the *shared* checkout
   `/home/christian/Code/city-sim`. A `.serena/project.yml` naming `city-sim-q8` now exists here
   (gitignored), but **verify before trusting any Serena tool**: ask for
   `get_symbols_overview` on `src/Borough.Core/Space/TerrainKind.cs`. If it says the file does not
   exist, Serena is still pointed at the shared checkout — **use Bash and Read/Edit instead**, and
   tell the user. A previous session made edits in the wrong tree this way.
2. **Another session is working milestone 25 and commits directly to `main`.** Do not merge or rebase
   onto `main` without checking for number collisions first. This branch owns **`adr/0150`–`0157`**
   and **`plans/0041`**; `0144`–`0149` were deliberately freed for that session. Read `plans/0012`'s
   final section — *the naming hazard recurred a fourth time* — before claiming any new number. The
   next free ADR number for this branch is **`0158`**. ✅ **Numbering is CONFIRMED clear from both
   sides** as of 2026-08-23: that session checked, holds nothing at `0150`+, and says its next ADR
   will be `0144`.

   ⚠ **The MERGE SURFACE is not clear, and it is the thing to plan for.** `main` has moved on
   (`4a05a92`, milestone 25 task 4) and it edits **`CONTEXT.md`, `docs/02-simulation-model.md`,
   `plans/0000-board.md`, `plans/0003-build-plan.md`** and its own `plans/0040`. This branch edited
   **`CONTEXT.md`, `plans/0000` and `plans/0003`** in `472a6dd`. ***Three files are edited on both
   sides, so the next merge conflicts for real*** — which is the good case, because a conflict stops
   somebody, and it is exactly what the number collision did not do. **Do not merge `main`
   mid-task**: finish task 2, then merge deliberately with both branches' intent in hand.
   `d84440e`'s commit message is the precedent for how that was done last time.

## Where the task stands

The plan is [`plans/0041-terrain-and-the-land-rows.md`](plans/0041-terrain-and-the-land-rows.md).
**Read its Status, its decisions 1, 1b, 2, 3 and 7, and its task table.** Decisions 1, 1b, 2, 3, 4 and
7 are settled; **5 and 6 are open and belong to task 4**, not to you.

Tasks 1 and 3 are done. **You are finishing task 2.** Three commits so far:

| Commit | What |
|---|---|
| `472a6dd` | `adr/0157` — five terrain types, Base Fertility varies. Amends `adr/0022` and `adr/0154`, updates `CONTEXT.md`, opens one `plans/0002` §D1 row |
| `d6fb2a4` | The vocabulary half: `PurposeTag.TerrainType = 22`, `TerrainKind`, `TerrainRuleset`. **Nothing is wired in, nothing reads them, no hash has moved** |

**`adr/0157` is the decision you are implementing. Read it first** — it is short, and it records both
why Base Fertility varies and the half of that argument the project does not get for free.

## What is left, in order

1. **The loader.** `[[terrain]]` in `src/Borough.Formats/RulesetLoader.cs`. Copy the `[parking]`
   pattern end to end — field, `case "terrain":` in `Enumerate`, a `ReadTerrain()` returning
   `TerrainRuleset`, the call in `Parse`, and the property on `Ruleset`. ⚠ **Add `[[terrain]]` to the
   unknown-section refusal's list** (`RulesetLoader.cs`, the `default:` case naming every section) or
   a valid file is refused. Refusals worth having, and `TerrainRuleset.Kinds` says why: **all five
   types stated, each exactly once**, unknown name refused, duplicate refused, missing refused,
   `base_fertility_percent` required and refused above 100 (fully fertile is the top of the scale,
   `adr/0155`). Percent → Q16.16 is `IntegerMath.RoundDiv(Fixed.FromInt(percent), 100)`.
2. **The column.** `Terrain` on `LayerCellTable` (`src/Borough.Core/Space/LayerCellTable.cs`),
   declared in the constructor beside `Sealing` as `_rows.Saved<TerrainKind>("terrain")`.
   `Rows.Saved` takes any `unmanaged` type, so the `byte`-backed enum is legal directly.
3. **The generator pass.** Decision 3 settled the placement: **its own pass, called from
   `SyntheticCity.PopulateInto` between `RefuseIfPopulated(world)` and `LayLand(world, key)`** — see
   `SyntheticCity.cs:103-124`. Copy `RoadGenerator.LayInto`'s already-populated refusal shape. Height
   is computed as a **local and stored nowhere** (`adr/0156`); the pass keeps only the type. Draw with
   `Randomness.Draw(key, (ulong)CellGrid.Index(east, north), Ticks.Zero, PurposeTag.TerrainType)`.
4. **`rulesets/varied.toml`** — the world, and **it is part of this task rather than a follow-up**
   (plan task 2, and decision 5's ratifier needs it to exist). Give it a header in the house style;
   `rulesets/twinned.toml`'s is the model. It should say what it exists to show, what it must not be
   read as, and that its five Base Fertilities are **unratified and chosen against no consumer**.
5. **Tests.** `tests/Borough.Tests/Formats/TerrainRulesetLoadTests.cs`, mirroring
   `ParkingRulesetLoadTests.cs` — every refusal it states. Plus a generator test: the same `WorldKey`
   gives the same map, a different one gives a different map, and all five types appear on
   `varied.toml`.
6. **The re-baseline, as its own commit.** Read
   `tests/Borough.Tests/Golden/README.md` and follow it. The precedent to imitate is `1c9ebec` (code,
   hashes left stale) then `af3f5fd` (re-baseline alone, structured as WHAT MOVED / WHETHER IT WAS
   INTENDED / WHAT DID NOT MOVE). Expect the two golden traces and `world-hash.txt` to move;
   `GoldenFixtures.RulesetHash` and its two siblings move only if you edit an existing shipped
   Ruleset. **`adr/0100`: moving the hash costs nothing and must never be cited as a reason to defer,
   narrow or split the work.**

## The one real risk, and it is not hypothetical

⚠ **`LayerCellTable` is SPARSE, and terrain per Cell is dense by nature.** Task 3 already hit this:
giving Sealing a write path made the table dense on `bordered.toml` for the first time — **262,144
rows, and one Tick in 256 went from free to 88 seconds**. It is written up as **F7** in `plans/0041`
and `plans/0002` §C owns the cost.

**So do not call `CellResidency.Ensure` for every Cell in the generator.** Decide deliberately how
terrain is stored for a Cell that has no row, and write the reasoning at the symbol. Two candidates
worth weighing before you pick:

- **Write the type only where a row already exists**, and recompute from the `WorldKey` on demand
  elsewhere. Cheap, but then the column is not the whole truth and a save carries only part of the
  map's terrain.
- **Make the column dense deliberately** and pay the residency cost, having first measured what it
  actually costs at the shipped sizes rather than assuming F7's figure transfers.

⚠ **F7's 88 seconds was `bordered.toml` at whole-map residency — do not quote it as terrain's cost.**
Take a reading before putting any number in a document, and a reading needs a quiet machine
(`adr/0106`, and `CLAUDE.md`'s note that a capture names *nothing else running in this repository* as
its first control). **If this turns into a real cost question, it is a finding to route to
`plans/0013` and `plans/0002`, not something to work around locally** (`adr/0073`).

## Conventions that will bite you

- **`dotnet test -c Release --filter "tier!=instrument"`** is the gate. ~3m11s here at present, with
  other sessions running. Do not run the full suite unless you are at the milestone.
- **No `float`/`double`, no `Math.*`, no raw `/`, no `HashCode.Combine`.** The analysers are build
  errors, and `BOR0206` already caught one hand-written `GetHashCode` in this task.
- **No tuning number as a `const` in simulation source** — it belongs in the Ruleset (`adr/0015`).
- **Any new hash-bearing number needs a `plans/0002` §D1 row naming a machine, a world and a
  quantity, on the day it is written** (`adr/0052`). Task 2's five are already filed; do not open more
  without doing this.
- **A doc comment says where to look, never what you found** (`adr/0093`).

## When task 2 is done

Update `plans/0041`'s Status and task table, and the milestone 24 rows in
[`plans/0000-board.md`](plans/0000-board.md) and [`plans/0003-build-plan.md`](plans/0003-build-plan.md)
— **both**, they are two copies and they drifted once already this week. Then **task 5** (Fertility,
which deletes the `throw` in `MapLayers.Fertility`) becomes available, since it depends on 2 and 3 and
both will be done. Task 4 still waits on decision 5.
