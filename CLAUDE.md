# CLAUDE.md

Guidance for Claude Code working in this repository.

## What this is

A city-builder where the city is made of people you can actually meet, the economy is made
of Goods that actually move, and when something goes wrong the game can say exactly why.
Godot 4.7 is the host; the simulation is an engine-agnostic C# library.

**Current state: Phase 1 gate closed; S0a run; slice 7 is next and is gated on session A.** The repository is ~7,000 lines of design
documents and 43 ADRs, plus the first four slices of `plans/0003-build-plan.md` — the scaffolding,
spike S4, the arithmetic substrate, the analysers, and the typed tables with the per-field
declaration and the State Hash — and all eight tasks of slice 5: `step(inputs)` with the
eight phases, the command model and the Input Log, replay, the golden-hash baseline, the
headless runner with `Borough.Formats`, the three invariant tiers, the Census with
`series(metric, window)`, and the crash artifact. `dotnet run --project src/Borough.Headless` prints
a table report and a hash; `--log PATH --ticks N --hash-every N` replays a session and prints a hash
trace; `--census` adds what every collection did over the run; a panic writes a crash artifact that
the runner accepts back wherever it accepts a log.
**Slice 6, Map Layers ([`plans/0009`](plans/0009-map-layers.md)), is closed — all ten tasks — so
nothing in the code column stands in front of the Phase 1 gate.** `Borough.Core.Space` has the Cell
grid with the Cell and the Chunk as **two types**, the sparse `LayerCellTable` — the project's **first
`Buffering.TwoCopies`**, which made slice 4's declared-but-unimplemented double buffer real — the
separable integer convolution, the staggered schedule as a table, incremental re-diffusion that is
**bit-identical** to a full recompute, the three real Layers, **named holes that throw** where
Fertility, Desirability and the line-source queries will go, and `layer_cells(aabb, layer)`, the
project's first hot query — allocation-free and string-free, both checked. **Superposition is exact
over twenty sources and the in-place variant is kept in the suite watching itself fail.**
`dotnet run --project src/Borough.Headless -- --layer pollution` prints a field, which is the first
thing here that is not a number. The slice's owed decision is settled *by measurement*: `adr/0044`
makes the diffusion cadence **hash-bearing**, the **sixth claim in the corpus measured false and the
first outside S2**. **That ADR then got its own second half wrong by argument** — it filed the cadence
as world-creation-fixed while citing `adr/0015` without running the membership test `adr/0015` states,
which the cadence fails and the kernel radius passes. Withdrawn and recorded rather than amended away;
**citing an ADR is not applying it**. On the parallel spike track, **S2 R0 through R5.5 are done** — the synthetic
Road Graph and the denominator, the travel-time matrix, which **carries the choice loop**, the path
source, which **revived the DSDV case rather than retiring it**, HPA\*, which **narrowed the
pathfinding cluster to 8 or 16 Chunks a side without closing it, weakened its own standing** to 2.63×
the flat search once a route has to come back with arcs, and found that **no cluster size fits routing
into the Tick budget** — which promotes R6's route cache from a tidy-up to load-bearing — and then
distance-vector, which is **out because it costs 2.13× the rebuild it exists to avoid** and is beaten
by a scheme the plan never named (dynamic subtree repair, **4.71 ms** against **234.74 ms**). R4 also
found that **S2's uniform origin-destination draw had been hiding a conclusion**: it is the
longest-trip distribution available, and on a local-trip draw a District-granular route's detour goes
from R2's 18.52% to **128.82%**, which under `05 §4` is a different city. The draw is now a swept
family, which is what makes R6 runnable at all. **R5, the edit storm, is done through R5.5** — it fired a
tripwire (a single-counter Epoch *is* a global flush), found **no Epoch rung both affordable and correct**
across the whole core verb because addition is monotone-*improving* and per-Segment structurally cannot
notice it, and then measured the way out: a **TTL rotation** at 0.40 forced refreshes per Tick clears the
wrongly-valid count 38 → 0 while retaining 97.08%. It also **retired the shared District route on a number**
and established that the two survivors are wrong in **different currencies** — structural against temporal —
which is session M's question and not a benchmark's. **The whole spike so far ran on a frozen cost basis**:
nothing in S2 has ever invalidated a route because a road got *busy*, only because one was bulldozed.
`adr/0046` settles the structure that fixes this — **Habit, Sight and Temperament**, which is `adr/0017`'s
satisficing rule reaching the one actor class nobody had applied it to — and sets no parameter.
**R8 then measured it and all three layers survive**: `03 §3.4`'s self-correction closes on the local
layers alone (Sight settles **42.62%** below a control under sustained demand), so **static Habit holds
and there is no refresh cadence to argue about**; the Sight Horizon's floor is **1 Segment**, derived
from the graph; Temperament damps by **92.28%** where a herd exists. **R8's largest finding is none of
those.** *The network runs out of routes, not road* — **87.25% of traffic on 1% of the carriageway,
90.87% of it empty, at 13% of holding capacity, with capacity confirmed realistic** — because one
free-flow tree per District means one route per (node, District) pair in the whole model. That is
**decision 11 on a different axis** and it is now the top item on the board. `spikes/S2.Routing/` compiles the arithmetic substrate in by source
and can name nothing else of `Core`, so it has changed no simulation code. Task 7 shipped its instrument and **not** its trend assertion — nothing in
the world churns yet, so the assertion would have been vacuous. It is owed by slice 7; the board's
*Owed* section says how.

**S0a is done and the Phase 1 gate is closed.** `CommandKind.Populate` fills a world through **Phase 0**,
so the population is in the Input Log and replay reproduces it by construction; `Borough.Core.Entities.SyntheticCity`
is the one populator, replacing two drifted copies that lived in the shells and were therefore outside
the arithmetic lints — moving it into `Core` made `BOR0203` fire three times. **The spike's largest
finding is what it was not looking for: run mode had never had a city in it.** `--citizens 1000000`
allocated capacity and stepped an empty world, so **every Tick figure in the corpus, slice 6's
100,000-Tick acceptance run included, was taken over nothing.** The numbers at 1M: **85.98 MiB** of
tables and ~94 MiB resident, linear to 343.91 MiB at 4M; an empty Tick is **0.112 ms**; **one State
Hash is 32.47 ms — 2.08 Tick budgets**, against a `05 §9` that does not mention it and that records
`adr/0037` deleting the full-world double buffer for costing *less*; and 100,000 Ticks run in **11.75 s**
with nothing trending. **The Decide guard was `O(world)`, on by default and had no runner switch** —
76.4 ms per Tick, 95% of a run — so `--no-decide-guard` now exists with the correctness check still the
default. **S0 also split while being run**: `plans/0002` names four clauses and three of them are slices
9, 7 and 10, so **S0b — the Tick with work in it — is not runnable, and it is the half carrying `06`'s
stated risk.** 1M is a spec for **row counts** and still a hope for **the Tick**. The capture is
`powersave` and owes a re-take, which is stamped rather than hidden.

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
| `docs/06-roadmap.md` | **The phase model, the four pacing rules, and the risk each milestone retires. Nothing else** — it sequences work and never describes the simulation (`adr/0042`). Also names the mechanisms with no milestone yet |
| `docs/adr/` | 45 decision records, numbered to `0046` — `0028` is reserved and unwritten |
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

**A claim a measurement could settle must not be settled by argument** (`adr/0043`). Type every claim
before settling it: *can you name the number that would refute this, and the machine that would produce
it?* If you can, it is **measurable** — route it to a named spike with that number written down, and do
not let any document cite it as decided until the number exists. If you cannot, it is **arguable** and
a session may close it. Five claims in the corpus have been measured false so far and **two of them sat
in documents `0002` marks fully argued**, so a green mark is not evidence a sentence was examined.

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
| Chunk | a multiple of the Cell, ≥32×32 | tuning, hash-preserving, unvalidated. **Provisionally 1:1 with the Cell** |
| Map Layer cadence | pollution every 64 Ticks at offset 0; land value every 256 at offset 16 | tuning, hot-reloadable, **hash-bearing** — the designer's number and not the profiler's, measured in `adr/0044` |
| Industrial pollution kernel | separable tent, 1,024 m (8 Cells) | world-creation. **UNRATIFIED** — the 1–10 km band is 10× wide and wants a source |
| Map | 4096² Tiles, 2048² documented fallback | open |
| Target population | 10,000 first hour / 1,000,000 late game | sizing |
| Tick budget | 15.6 ms at 4× speed | |
| Microscopic Cap | **unset** | fixed world constant, still open |
| Sight Horizon | **unset** | tuning, hot-reloadable. Its **floor** is a Road Graph property — the distance to the next node with a real choice — and S2 R8.1 derives it (`adr/0046`) |
| Temperament base and spread | **unset** | tuning. Stable base plus per-decision jitter, two `purpose_tag`s. **The base/jitter blend weight has no argument behind it at all** and is the routing model's weakest number |
| Habit refresh cadence | **unset**, provisionally **infinite** | static per world is the null hypothesis. **Hash-bearing if it is ever finite**; S2 R8.5 is what could refute it |

## Definition of done for any milestone

This list is owned here; `docs/06-roadmap.md` rule 2 requires it and cites it. Cumulative obligations,
not milestones of their own. Refined per slice by `plans/0003 §Definition of done`.

- `dotnet build` succeeds and `dotnet test` is green, on a machine with no GPU and no Godot
- The invariants pass. **Sorted by frequency, never gated on build configuration** (`02 §10`) —
  `O(1)` at the write site per Tick, `O(n)` staggered, whole-world at end of run. The runs that
  surface these bugs are the million-Tick headless balance runs, and those are release builds
- The long-run test passes — 100k+ Ticks with **no collection and no magnitude** trending upward at
  steady state (`adr/0006`, and `adr/0003`'s extension of it to quantities)
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
