# CLAUDE.md

Guidance for Claude Code working in this repository.

## What this is

A city-builder where the city is made of people you can actually meet, the economy is made
of Goods that actually move, and when something goes wrong the game can say exactly why.
Godot 4.7 is the host; the simulation is an engine-agnostic C# library.

**Current state: Phase 1, through slice 5 task 4.** The repository is ~7,000 lines of design
documents and 41 ADRs, plus the first four slices of `plans/0003-build-plan.md` — the scaffolding,
spike S4, the arithmetic substrate, the analysers, and the typed tables with the per-field
declaration and the State Hash — and the first four tasks of slice 5: `step(inputs)` with the
eight phases, the command model and the Input Log, replay, and the golden-hash baseline.
`dotnet run --project src/Borough.Headless` prints a table report and a hash.
**Slice 5 tasks 5–8 are next, and `plans/0008` carries a progress table** — the runner's flags and
the text codec, the invariant tiers, the long-run test and the crash artifact.

**`plans/0000-board.md` is the first thing to read on any cold start** — a flat view of what is done,
what to do next and what is blocked. It is a *view*: `plans/0003-build-plan.md` owns the slice order
and its gates when picking the *code* up cold, and `plans/0002-open-questions.md` owns the reasoning
when picking up the *design*. When the board disagrees with either, they win. Slices 7 onward are gated and must
not be started before their gate clears. The corpus is still being grilled and several decisions
on the critical path are open, so do not write implementation code beyond the current slice
unless asked.

## Repository map

| Path | What it is |
|---|---|
| `CONTEXT.md` | **The domain vocabulary. Authoritative.** Every term, with exactly one meaning |
| `docs/00-vision.md` | Pillars, anti-goals, the argument against this design and the answer |
| `docs/01-player-experience.md` | Verbs, panels, notifications, overlays |
| `docs/02-simulation-model.md` | World model, Tick phases, Rule families, determinism rules, testing strategy |
| `docs/03-agent-architecture.md` | Movement, fidelity tiers, Trips and Legs |
| `docs/04-economy-and-goods.md` | The five Goods, chains, Office |
| `docs/05-technical-architecture.md` | Project layout, sim/render boundary, data layout, threading, saves |
| `docs/06-roadmap.md` | Phases and milestones, in dependency order. No dates |
| `docs/adr/` | 39 numbered decision records |
| `docs/deferred.md` | What is deliberately not being built, with retrofit costs and revisit triggers |
| `docs/references.md` | Reference games and prior art, with standing of each decision |
| `plans/0000-board.md` | **The board. Read this first on any cold start** — done, next, unblocked, owed, blocked. A view over `0002` and `0003`, never a source |
| `plans/0002-open-questions.md` | The live ledger of design questions, and where the *reasoning* lives |
| `plans/0003-build-plan.md` | The ordered slice ledger for Phase 0 and Phase 1, with a gate board. **Start here when picking up the *code* cold.** Supersedes `06`'s Phase 0/1 ordering |
| `plans/0004`–`0009` | One plan document per unblocked slice: S4, the arithmetic substrate, the analysers, typed tables, the Tick and replay, Map Layers |
| `docs/spike-results.md` | Recorded spike numbers and the decision each produced. Empty until S4 runs |
| `docs/dev-environment.md` | Setting up a machine to work on this |

`plans/0001-foundational-design.md` predates ADRs 0005–0011 and is stale. `docs/06-roadmap.md`
supersedes its build order. Do not trust it without checking.

## Working with the corpus

**`CONTEXT.md` governs vocabulary.** Domain terms are capitalised in prose — a Household, a
Bin, a Trip, a Segment, the Event Wheel. If a concept needs a name that isn't in `CONTEXT.md`,
add it there first. The file ends with a *Terms we deliberately do not use* section; those are
banned outright, and several of them (Agent, Cohort, Demand, Region) name failure modes the
design has already rejected.

**Decisions live in ADRs, not in prose.** If a design question gets settled, it gets an ADR in
`docs/adr/NNNN-lowercase-hyphenated-claim.md`. The filename is the claim, stated as a sentence.
The structure is: title as a claim, the decision in bold up front, `## Why`, `## Consequences`,
`## What would trigger revisiting`. That last section is not optional — a decision with no
revisit trigger is a decision nobody can reopen honestly.

**Every significant decision cites a guiding concept** from `CONTEXT.md`'s tag table —
`EMERGENCE`, `LEGIBLE CAUSE`, `UNIQUE INDIVIDUALS`, `BOUNDED KNOWLEDGE`,
`SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`, `PLAYER GOVERNS`, `NO VERDICT`,
`FAST ITERATION`. A decision that cites none is a decision without a justification.

**Prose style is British** — modelled, behaviour, optimise, serialisation, sterilise. Documents
cross-reference by section (`02 §4.1`) and link relatively. The register is dense and
argumentative: state the claim, then the reasoning that survives objection. Match it.

**Superseded documents get a banner, never a deletion.** See the top of
`docs/adr/0005-two-fidelity-tiers.md` for the form.

## Architecture invariants

These are enforced mechanically because they fail silently. Full list in `docs/05 §4`.

1. **No Godot reference from `Borough.Core`, transitively** (`adr/0002`)
2. **No `float`/`double`** in simulation state or arithmetic — integers and Q16.16 only (`adr/0003`)
3. **No `Dictionary`/`HashSet` enumeration** in simulation code; no `System.Random` anywhere in it —
   build one and look up in it freely, never walk it
4. **Thread-count equivalence** — `run(log, threads=1).hash() == run(log, threads=8).hash()`
5. **Replay equivalence** — two runs of one Input Log produce identical State Hash sequences
6. **Save/reload equivalence** — the Factorio test: run N, save, reload, run M; vs run N+M
7. **No reference types in simulation state** — every struct in `Borough.Core` satisfies `unmanaged`
   (`adr/0036`), unless it carries `[ColdPath("why")]`, which is the hot/cold axis and the only
   exception: the hot path runs inside `step()` every Tick and holds no references; the cold path
   runs on a click and may

**Lints 1–3 and 7 are live.** `Borough.Analysers` reports them as build **errors**, ids `BOR0201`–`BOR0206`
(floating point, `Math.*`, raw `/`, masked shift counts, wall clock, unstable identity), `BOR0301`–`BOR0302`
(hash-map enumeration, `System.Random`), `BOR0701` (managed state) and `BOR0801`–`BOR0803` (the
`purpose_tag` enum). `BOR0901` is `adr/0003`'s per-field declaration — storage in a `[Table]` type
that is not a declared `Column` or the table's own `Rows`. Neither `BOR08xx` nor `BOR0901` is one of
the seven lints; the count stays seven. Lints 4, 5 and 6 need machinery that does not exist yet.
Every diagnostic has a test that writes the violation and watches it fire — do not add one without.

**Every field in a table is declared once** as `(saved AND hashed)` or `(derived AND rebuilt)`, and
declaring it through `Rows.Saved`/`Rows.Derived`/`Rows.SavedHandle` is what *allocates* it — so the
State Hash cannot have a coverage hole. The hash folds values, never identity: a handle column folds
the target row's monotonic never-reused id, not the recycled slot index. Composition order is
**tables in declaration order, arrays in index order**.

Also banned in the core: `DateTime`, `Stopwatch`, `Environment.TickCount`, `Guid.NewGuid()`,
default `object.GetHashCode()`, and parallel loops accumulating into shared state.

Randomness is `hash(world_seed, entity_id, tick, purpose_tag)` — counter-based, never a stream.
Every distinct use gets a distinct `purpose_tag`; reusing one correlates two decisions invisibly.

Every variable-length collection in `Borough.Core` is an **intrusive index list** — a head index
on the owner, a `next` index on the element, both in flat arrays. Never a per-entity collection
object.

`Borough.Core.Arithmetic` is the one namespace exempt from the raw-`/` and shift lints, because it is
where their replacements are implemented. There is no `Math.*` anywhere, including there.

**No tuning number is a `const` in simulation source.** Everything the designer would want to
change lives in the TOML Ruleset and is hot-reloadable (`adr/0015`). A `const` where a Ruleset
value belongs is a defect, not a shortcut.

**A change is an optimisation if the State Hash is unchanged, and a design change otherwise** —
however it was motivated. This is the test that decides whether something may be tuned freely.

## Project layout

Five projects, one repository, two toolchains. The split is the architectural decision. A sixth,
`Borough.Analysers`, is a build-time input rather than part of the runtime architecture and is
deliberately not counted among the five (`05 §1`) — the test being that it does not ship.

| Project | Contents |
|---|---|
| `Borough.Core` | Pure C# library, zero Godot references. Typed tables, integer maths, Event Wheel, Ruleset interpreter, `step(inputs)`. **This is the game** |
| `Borough.Tests` | xUnit and BenchmarkDotNet. Determinism, invariants, save/reload, allocation benchmarks |
| `Borough.Headless` | Console runner. Loads a Ruleset and an Input Log, fast-forwards, dumps State Hashes |
| `Borough.Formats` | The artefacts that spell things in words: the Input Log codec (`.borough`), and the crash artifact that wraps it. References `Core`; referenced by both shells, which may never parse or emit a log themselves (`adr/0039`). Not the save — that is an array dump generated from the field declaration and stays in `Core` |
| `Borough.Godot` | Thin shell. Per-Chunk `MultiMeshInstance3D`, `Control` UI, per-frame snapshot |
| `Borough.Analysers` | `netstandard2.0` Roslyn analysers for `05 §4`'s lints 2, 3 and 7 and the `purpose_tag` check. Referenced by `Borough.Core` as an **analyser**, never as a dependency, so nothing in it reaches the running sim |

**The headless runner must never require Godot to be installed.** That constraint is the
cheapest continuous check that the boundary still holds.

**`Core` returns ids and numbers, never human-readable strings.** The shell owns every string a
human reads, resolved through the Ruleset. The real leak vector is not `using Godot;` — it is a
method that returns a formatted string because a panel wanted one.

```
dotnet build                  # must succeed with no GPU and no Godot installed
dotnet test                   # must be green
dotnet run --project src/Borough.Headless
```

## Constants

| Constant | Value | Kind |
|---|---|---|
| `TICKS_PER_DAY` | 8192 | world-creation, baked into the save, not hot-reloadable |
| `WHEEL_SIZE` | 8192 Ticks | world-creation. Set by the longest routine sleep |
| Reference tick rate | 16 Ticks/s → a Day is 8m32s | host-side, runtime only |
| Cell | 32×32 Tiles (≈128 m) | **design constant, never tuned** — it changes the State Hash |
| Chunk | a multiple of the Cell, ≥32×32 | tuning, hash-preserving, unvalidated |
| Map | 4096² Tiles, 2048² documented fallback | open |
| Target population | 10,000 first hour / 1,000,000 late game | sizing |
| Tick budget | 15.6 ms at 4× speed | |
| Microscopic Cap | **unset** | fixed world constant, still open |

## Definition of done for any milestone

From `docs/06-roadmap.md`. These are cumulative obligations, not milestones of their own.

- `dotnet build` succeeds and `dotnet test` is green, on a machine with no GPU and no Godot
- Debug invariant assertions pass: Goods conserved, no Bin negative or over capacity, no Citizen
  in two places, every Household's home a Building that lists them as an occupant
- The long-run test passes — 100k+ Ticks with no collection trending upward at steady state
  (`adr/0006`)
- There is something to *look at* showing the milestone doing its job

Every milestone names the specific risk it retires. A milestone that cannot name one is either
not necessary yet or not understood well enough to start.

## Things to be careful about

- **Don't reach for an ECS.** `adr/0004` rejected it explicitly: the population is homogeneous
  and ECS earns its complexity through heterogeneous composition.
- **Don't add a demand scalar.** There is no RCI meter. The Unplaced Pool *is* the demand signal.
- **Don't collapse Citizens into groups.** No Cohorts, no shared decisions, ever (`adr/0005`).
- **Don't let fidelity depend on the camera.** Fidelity is a property of place, driven by Stress
  (`adr/0007`). The renderer cannot influence the simulation.
- **Don't add a collection without a sink.** `adr/0006` — nothing grows with elapsed time.
- **Don't move a mechanism between Rule families for performance.** Bin Rules and Sweep Rules
  differ in observable behaviour, so moving one is a change to the city (`adr/0033`).
- **Prefer off-the-shelf infrastructure.** `adr/0018` — Citybound shipped ten bespoke libraries,
  three engine rewrites, and no game. A bespoke component requires a written exception naming the
  property no library provides.
