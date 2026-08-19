# CLAUDE.md

Guidance for Claude Code working in this repository.

## What this is

A city-builder where the city is made of people you can actually meet, the economy is made of Goods
that actually move, and when something goes wrong the game can say exactly why. Godot 4.7 is the host;
the simulation is an engine-agnostic C# library.

## Where to look

**Read [`plans/0000-board.md`](plans/0000-board.md) first on any cold start.** It is the only view of
what is in flight.

⚠ **This file holds no status and no per-slice narrative, and that is deliberate.** It did, it became a
third copy of the board and the slice plans, and it was the copy that drifted — `plans/0012` **Cause 1**:
*every document that stores per-slice status drifted, and the only large one that did not stores none.*
The same goes for counts. **Do not add a milestone summary, a session outcome, an ADR total or a line
count here**; add it to the document that owns it and let this file point.

Five files answer five questions, one each. When the board disagrees with any of them, **they win**.

| File | The one question it answers |
|---|---|
| [`plans/0000-board.md`](plans/0000-board.md) | ***What is next*** — a view, never a source, and **never the home of an open question** |
| [`plans/0003-build-plan.md`](plans/0003-build-plan.md) | ***What is done*** — the slice ledger, its gates, and the hash-moving queue |
| [`plans/0002-open-questions.md`](plans/0002-open-questions.md) | ***What needs answering*** — every entry typed *measurable* or *arguable*, and **§D is the ledger of unratified numbers** |
| [`plans/0012-corpus-audit.md`](plans/0012-corpus-audit.md) | ***What a document says wrongly*** — corrections owed, which are not questions |
| [`plans/0013-tick-budget.md`](plans/0013-tick-budget.md) | ***What a Tick costs*** — one row per consumer, and whether its multiplicand was measured or guessed |

**The prose outweighs the simulation, it is known, and it is a standing concern on the board** — which
is why the board's rule is *an argument session runs when something concrete is blocked on it, never
because it is available.*

**A gated slice must not be started before its gate clears**, and several decisions on the critical path
are still open, so do not write implementation code beyond the current slice unless asked.

## Repository map

| Path | What it is |
|---|---|
| `CONTEXT.md` | **The domain vocabulary. Authoritative.** Every term, with exactly one meaning. Ends with *Terms we deliberately do not use* — those are banned outright |
| `PROCESS.md` | **The project vocabulary. Authoritative**, and `CONTEXT.md`'s sibling — slice, spike, gate, session, the numbering scheme, and the conventions every document is written to. `CONTEXT.md` names the city; this names the calendar |
| `docs/00-vision.md` | Pillars, anti-goals, the argument against this design and the answer |
| `docs/01-player-experience.md` | Verbs, panels, notifications, overlays |
| `docs/02-simulation-model.md` | World model, Tick phases, Rule families, determinism rules, testing strategy |
| `docs/03-agent-architecture.md` | Movement, fidelity tiers, Trips and Legs |
| `docs/04-economy-and-goods.md` | The five Goods, chains, Office |
| `docs/05-technical-architecture.md` | Project layout, sim/render boundary, data layout, threading, saves |
| `docs/06-roadmap.md` | **The phase model, the four pacing rules, and the risk each milestone retires. Nothing else** — it sequences work and never describes the simulation (`adr/0042`). Also names the mechanisms with no milestone yet |
| `docs/movement-primer.md` | **Orientation only, and it owns nothing.** Movement and routing rebuilt from first principles, for paging the subsystem back in. Stores no status and almost no numbers, which is what keeps it from drifting. `03`, `CONTEXT.md` and the ADRs win against it always |
| `docs/adr/` | The decision records, numbered from `0001`. `0028` is reserved and unwritten. **Count them rather than quoting a total** — a count in prose is a fact that drifts |
| `docs/deferred.md` | What is deliberately not being built, with retrofit costs and revisit triggers |
| `docs/references.md` | Reference games and prior art, with the standing of each decision |
| `docs/spike-results.md` | Recorded spike numbers and the decision each produced |
| `docs/dev-environment.md` | Setting up a machine to work on this |
| `plans/0000-board.md` | **The board. Read this first on any cold start** — what is next, then done, unblocked, owed and blocked. A view over `0002` and `0003`, never a source, and **never the home of an open question**. A closed row leaves the board |
| `plans/0000a-board-archive.md` | **An index, not a record.** One line per closed board row, naming the document that owns the full version. **Do not quote it** — a one-line summary is a caveat-free compression of somebody else's sentence, which is `plans/0012` **Cause 5** by construction. Follow the link |
| `plans/0002-open-questions.md` | ***What needs answering.*** Every entry typed *measurable* or *arguable* and grouped by what is blocked on it. **§D is the ledger of chosen-but-unratified numbers** — D1 in use, D2 unset, D3 moved out |
| `plans/0003-build-plan.md` | ***What is done.*** The ordered slice ledger for Phase 0 and Phase 1 with its gate board, plus the hash-moving queue. **Start here when picking up the *code* cold** |
| `plans/0012-corpus-audit.md` | ***What a document says wrongly.*** The debt ledger, its numbered Causes, the disqualifier registry and the mechanical checks. Delete it when everything in it is struck |
| `plans/0013-tick-budget.md` | ***What a Tick costs.*** One row per consumer, each citing its owner, and the column that is the point: whether the row's multiplicand was **measured or guessed**. A view, never a source |
| `plans/0004`–`0031` | **One document per slice, spike or session, and each owns its own findings in full.** The board's third column is a pointer to these, never a summary — so read the plan rather than any description of it. `0001` predates ADRs 0005–0011 and is **stale**; `06` supersedes its build order |
| `rulesets/` | **Ruleset content, in TOML** — data the binary interprets, hot-reloadable under `adr/0015`. Five files, each a **demonstration rather than a city**, and **every one carries its own header explaining what it exists to show and what it must not be read as**: `minimal.toml`, the smallest Ruleset that makes Bins move; `minimal-tuned.toml`, the same file with one number changed, which the golden session reloads into at Tick 128; `severance.toml`, the rung at which the Severance dial does anything; `congested.toml`, the only file that states `[traffic]` and `[households]`, because a generated city cannot congest itself; and `diagnosed.toml`, the only one authoring an `on_fail` chain, because otherwise nothing records *why* a Building fell down. **Read the header before quoting a number out of one** |

## Working with the corpus

**`CONTEXT.md` governs vocabulary.** Domain terms are capitalised in prose — a Household, a Bin, a
Trip, a Segment, the Event Wheel. If a concept needs a name that is not in `CONTEXT.md`, add it there
first. Its *Terms we deliberately do not use* section is a list of failure modes the design has already
rejected, several of them by name — Agent, Cohort, Demand, Region.

**Decisions live in ADRs, not in prose.** A settled design question gets
`docs/adr/NNNN-lowercase-hyphenated-claim.md`, where the filename is the claim stated as a sentence.
[`PROCESS.md`](PROCESS.md) → *Conventions* owns the required structure, the guiding-concept tag, the
prose register, and the rule that **superseded documents get a banner, never a deletion**.

**Five rules govern what a sitting may conclude.** Each is an ADR; read it before leaning on it, because
the ADR carries the worked examples and the amendments and this list carries neither.

| Rule | What it governs | Read |
|---|---|---|
| **A claim a measurement could settle must not be settled by argument** | **claims** — type every claim first: *can you name the number that would refute this, and the machine that would produce it?* If yes it is **measurable**, and no document may cite it as decided until that number exists | [`adr/0043`](docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) |
| **A hash-bearing number is chosen with a named ratifier or not at all** | **numbers** — on the day such a number is written down, record beside it in `plans/0002` §D the named thing that would ratify it and the trigger that would reopen it. **A category is not a name.** Amended twice: a ratifier names a machine, **a world**, and **a quantity** | [`adr/0052`](docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) |
| **An unbuilt mechanism is not a design constraint** | **absences** — name the mechanism and classify it *unbuilt*, *undesigned* or *refused*. **Only *refused* is evidence.** Most of this project does not exist, so *the simulation does not do X* almost always means nobody has built X, and the answer to *given X does not exist, should Y compensate?* is **build X** | [`adr/0070`](docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) |
| **A description of the build is where to look, and never what you found** | **what the build does** — a sentence about a mechanism, in an ADR, a plan or a doc-comment, tells you which symbol to read and never what is in it. Where such a sentence is wrong it is wrong about the **trigger**. Writing half: **name a symbol, never a time** | [`adr/0093`](docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) |
| **A local workaround is not a discharge** | **what a spike does with what it found** — when the cause lies in code the spike does not own, route the finding there **on the day and before working around it**. A defect → the code or `plans/0003`; a cost → `plans/0013`; a question → `plans/0002`; a document now wrong → `plans/0012` | [`adr/0073`](docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md) |

**And one rule about quotation, which is not about reasoning at all.** ***A caveat attached to a number
does not travel with it*** — somebody needs a number of that shape, finds it, and copies the **digits**;
the clause saying what it measures stays where it was, doing nothing. **Reading**: quote the *sentence*,
never the digits. **Writing**: name a number after what it measures, not after where it sits. The special
case is a share of a budget — ***carry the bill, not the percentage***, because a percentage hides which
side moved. `plans/0012` **Cause 5** holds the sightings, the two-half repair and the **disqualifier
registry** of figures a test refuses to let any document quote bare.

**The corpus checks itself mechanically.** `tests/Borough.Tests/Corpus/` holds them — citations resolve,
links open, tables render, no registry figure appears without its clause. **They are all
document-to-document**, so a number living in one place only, or in a doc-comment, is invisible to every
one of them. Read the tests for what is actually checked rather than any prose description of them.

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
   (`adr/0036`), unless it carries `[ColdPath("why")]`: the hot path runs inside `step()` every Tick
   and holds no references; the cold path runs on a click and may

**Lints 1–3 and 7 are live**, reported by `Borough.Analysers` as build **errors**: `BOR0201`–`BOR0207`
(floating point, `Math.*`, raw `/`, masked shift counts, wall clock, unstable identity, a ratio
pre-scaled in 32 bits), `BOR0301`–`BOR0302` (hash-map enumeration, `System.Random`), `BOR0701`
(managed state), `BOR0801`–`BOR0803` (the `purpose_tag` enum) and `BOR0901` (`adr/0003`'s per-field
declaration). `BOR08xx` and `BOR0901` are not among the seven lints; the count stays seven. Lint 5 is
live via `ReplayTests` and the golden baseline. Lints 4 and 6 need machinery that does not exist yet.
**Every diagnostic ships with a test that writes the violation and watches it fire** — do not add one
without.

**Every field in a table is declared once** as `(saved AND hashed)` or `(derived AND rebuilt)`, and
declaring it through `Rows.Saved`/`Rows.Derived`/`Rows.SavedHandle` is what *allocates* it — so the
State Hash cannot have a coverage hole. The hash folds values, never identity: a handle column folds
the target row's monotonic never-reused id, not the recycled slot index. Composition order is
**tables in declaration order, arrays in index order**.

Also banned in the core: `DateTime`, `Stopwatch`, `Environment.TickCount`, `Guid.NewGuid()`,
default `object.GetHashCode()`, and parallel loops accumulating into shared state.

Randomness is `hash(world_seed, entity_id, tick, purpose_tag)` — counter-based, never a stream.
Every distinct use gets a distinct `purpose_tag`; reusing one correlates two decisions invisibly.

Every variable-length collection in `Borough.Core` is an **intrusive index list** — a head index on
the owner, a `next` index on the element, both in flat arrays. Never a per-entity collection object.

`Borough.Core.Arithmetic` is the one namespace exempt from the raw-`/` and shift lints, because it is
where their replacements are implemented. There is no `Math.*` anywhere, including there.

**No tuning number is a `const` in simulation source.** Everything the designer would want to change
lives in the TOML Ruleset and is hot-reloadable (`adr/0015`). A `const` where a Ruleset value belongs
is a defect, not a shortcut.

**A change is an optimisation if the State Hash is unchanged, and a design change otherwise** —
however it was motivated. That is the test for whether something may be tuned freely. ⚠ **It
classifies a change; it does not price one** ([`adr/0100`](docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)):
moving the hash costs nothing while nobody is carrying a save, and **never cite hash movement as a
reason to defer, narrow or split work**. What survives is attribution — a hash move gets a commit
whose subject explains it.

## Project layout

Five projects, one repository, two toolchains. The split is the architectural decision. A sixth,
`Borough.Analysers`, is a build-time input rather than part of the runtime architecture and is
deliberately not counted among the five (`05 §1`) — the test being that it does not ship.

| Project | Contents |
|---|---|
| `Borough.Core` | Pure C# library, zero Godot references. Typed tables, integer maths, Event Wheel, Ruleset interpreter, `step(inputs)`. **This is the game** |
| `Borough.Tests` | xUnit and BenchmarkDotNet. Determinism, invariants, save/reload, allocation benchmarks |
| `Borough.Headless` | Console runner. Loads a Ruleset and an Input Log, fast-forwards, dumps State Hashes |
| `Borough.Formats` | The Input Log codec (`.borough`) and the crash artifact that wraps it. References `Core`; referenced by both shells, which may never parse or emit a log themselves (`adr/0039`). Not the save — that is an array dump generated from the field declaration and stays in `Core` |
| `Borough.Godot` | Thin shell. Per-Chunk `MultiMeshInstance3D`, `Control` UI, per-frame snapshot |
| `Borough.Analysers` | `netstandard2.0` Roslyn analysers for `05 §4`'s lints 2, 3 and 7 and the `purpose_tag` check. Referenced by `Borough.Core` as an **analyser**, never as a dependency |

**The headless runner must never require Godot to be installed.** That is the cheapest continuous
check that the boundary still holds.

**`Core` returns ids and numbers, never human-readable strings.** The shell owns every string a human
reads, resolved through the Ruleset. The real leak vector is not `using Godot;` — it is a method that
returns a formatted string because a panel wanted one.

```
dotnet build                  # must succeed with no GPU and no Godot installed
dotnet test                   # must be green
dotnet run --project src/Borough.Headless
dotnet run --project src/Borough.Headless -- --zones --ruleset rulesets/minimal.toml --ticks 5000
dotnet run --project src/Borough.Headless -- --commute --ruleset rulesets/minimal.toml --ticks 4096
dotnet run --project src/Borough.Headless -- --traffic --ruleset rulesets/congested.toml --citizens 16000 --ticks 512
dotnet run --project src/Borough.Headless -- --evidence --ruleset rulesets/diagnosed.toml --citizens 4000 --ticks 2048
dotnet run --project src/Borough.Headless -- \
  --ruleset rulesets/minimal.toml --reload-at 200 --ruleset rulesets/minimal-tuned.toml --ticks 400
```

## Constants

**This table states values, not arguments.** Each row's reasoning lives in the ADR named beside it, and
its ratification status lives in [`plans/0002`](plans/0002-open-questions.md) **§D** — D1 in use and
unratified, D2 unset. ⚠ **Nearly every number here is UNRATIFIED**, so treat §D as the authority on what
is settled and this table as the authority on nothing but the current value.

**Kind** is the property that decides how a number may be changed: *design* never moves; *world-creation*
is baked into the save; *tuning* is hot-reloadable Ruleset data. **Hash-bearing** means changing it is a
change to the city under `05 §4`, not an optimisation.

| Constant | Value | Kind |
|---|---|---|
| `TICKS_PER_DAY` — `Ticks.PerDay` | **2048** | world-creation, hash-bearing. A Tick is 42.1875 s of in-world time ([`adr/0094`](docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)). A `const` where `adr/0015` says it should be Ruleset data — filed in `plans/0012` |
| `WHEEL_SIZE` | **2048 Ticks** | world-creation. Set by the longest routine sleep, bounded by one Day, so it moves with `TICKS_PER_DAY` |
| Reference tick rate | 16 Ticks/s → a Day is 2m08s | host-side, runtime only. The ladder is pause / 0.5× / 1× / 2× / 3× / 4× (`01 §1`) |
| Cell | 32×32 Tiles (≈128 m) | **design constant, never tuned** — it changes the State Hash |
| Chunk | a multiple of the Cell, ≥32×32 | tuning, hash-preserving. **Provisionally 1:1 with the Cell** |
| Map | **16384² Tiles** — `CellGrid.WorldCells = 512`, 65.5 km a side | world-creation. Sized by how many Commute Budgets fit across it, never by area ([`adr/0089`](docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)). ⚠ **Not hash-bearing** — a map size bounds the cities that are *reachable*, and `05 §4` asks whether a change moves *this* city |
| Target population | 10,000 first hour / 1,000,000 late game | sizing |
| Tick budget | **15.6 ms at 4×, at 1,000,000 Citizens, on ONE CORE of the reference class** | ✅ settled ([`adr/0105`](docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md), [`adr/0106`](docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)). ⚠ **Quote the machine and the thread count with the number** — a 2020 six-core x86-64 desktop, i5-10400 class, `powersave`, **single-threaded**. Every rung is offered at every city size for ever; a host that cannot sustain one **dilates and says so**. **1× is the speed a capability is priced against**, and is not the target |
| Map Layer cadence | pollution every 64 Ticks at offset 0; land value every 256 at offset 16 | tuning, hash-bearing — the designer's number, not the profiler's ([`adr/0044`](docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md)) |
| Industrial pollution kernel | separable tent, 1,024 m (8 Cells) | world-creation, Ruleset data — `[layers] kernel_metres`, refused on reload |
| Provenance trail cap (`N`) | **16** transitions retained in full, older ones aggregated | world-creation, saved, hash-bearing. `RulesetTrailTable.Retained`. A `const` rather than Ruleset data on purpose: a designer must not be able to reload a smaller window |
| A `[[kind]]`'s occupancy | **3** | tuning, hash-bearing — `[[building]] occupants` ([`adr/0068`](docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)). Derived from the Ruleset in force, so lowering it **evicts** the overflow |
| A `[[kind]]`'s employment | **8**, on `dwelling` | tuning, hash-bearing — `[[building]] jobs`. Counts **Citizens**, never Households. **Derived rather than chosen**, and it puts full employment out of reach by construction. ⚠ It sits on the *dwelling* kind because a workplace kind needs a second `[[zone_rule]]` or the city fills with offices |
| Placement pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hash-bearing — `[placement]` ([`adr/0069`](docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)). The **sample is derived** from the duration ([`adr/0059`](docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)); the duration is not |
| Job assignment pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hash-bearing — `[jobs]` ([`adr/0081`](docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)). **There is no search radius key and that absence is the decision** — the box derives from the Commute Budget, and `[jobs]` without one is refused at load |
| Road Graph geometry | `block_tiles = 32`, `arterial_count = 0`, `arterial_junction_tiles = 512`, `foot_crossing_every = 4`, `foot_paths_per_thousand_blocks = 40` | world-creation, Ruleset data, hash-bearing — `[roads]`. ⚠ `foot_crossing_every` is **inert** at the shipped lattice rather than merely unratified; `foot_paths_per_thousand_blocks` is the stronger Severance lever. `arterial_count` went to 0 because an Arterial is a player tool that does not belong in a generator |
| `lots_per_segment` | **5** | world-creation, Ruleset data, hash-bearing — `[lots]` ([`adr/0078`](docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)). **Derived rather than chosen**: it is `CONTEXT.md` → Address's own *five Buildings share a Segment*, and therefore the premise of the *an Address is never a Node* refusal. **A Lot has no depth and there is no depth key** |
| Free-flow speeds and capacities | 50 / 90 / 5 km/h; 3,600 / 12,000 / 1,000 Vehicles per hour | tuning, hash-bearing. Free-flow is `(derived AND rebuilt)`, so retuning a speed moves the **standing** city. The speeds have a source outside the corpus; the capacities do not |
| The crossing cost | **30 s** | tuning, hash-bearing — `[trips]` ([`adr/0074`](docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)). What it costs on foot to reach the other side of a Segment. ⚠ **The derivation was looked for and there is none** — both candidates rest on unbuilt mechanisms, so it is chosen against a stated band |
| Commute Budget | **three rungs — fast 20, moderate 40, unsavoury 50 clock minutes** | tuning, hash-bearing — `[trips]` ([`adr/0095`](docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)). **Only the ceiling refuses**; the two rungs below grade a commute that happens anyway. ⚠ They are percentiles of a **free-flow, foot-only** distribution — do not read them against a vehicle-denominated column |
| Volume-delay function | **α 15%, β 4, clamp 400%** — `rulesets/congested.toml` only | tuning, hash-bearing — `[traffic]` ([`adr/0099`](docs/adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md)). BPR, priced **on entry** from that Segment's volume at that instant. **Absent means roads never slow down.** ⚠ **It is a loop and not a formula** — congestion slows a Vehicle, a slower Vehicle dwells longer, longer dwell *is* higher volume |
| Household car ownership | **100%** — `rulesets/congested.toml` only | tuning, hash-bearing — `[households] car_ownership_percent` ([`adr/0098`](docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md)). A Citizen drives everywhere or walks everywhere. ⚠ **Mode choice is a different question, settled by no ADR** and *undesigned* rather than unbuilt. **Absent means nobody drives**, reached by omitting the table rather than by a defaulted key |
| The Shift model | `shift_start_earliest_hour`/`latest_hour` **6–10**; `[jobs] shift_hours_min`/`max` **6–10**; `[jobs] arrive_early_max_minutes` **15** | tuning, hash-bearing ([`adr/0101`](docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md)). A commute is **two journeys** anchored on a Shift start hour belonging to the **Workplace**, so a Citizen stores no start hour at all. **The Day's shape is emergent.** Tick 0 is midnight |
| The Parking Shed's radius | **400 m** — `[parking] radius_metres`, all five Rulesets | tuning, hash-bearing ([`adr/0009`](docs/adr/0009-parking-is-modelled-supply-never-search.md), [`adr/0120`](docs/adr/0120-a-car-park-is-not-a-bin-and-supply-is-at-buildings-until-a-segment-needs-one.md)). How far a driver will walk from a Car Park |
| Microscopic Cap | **unset** | fixed world constant, still open. **It counts *Vehicles*, not Segments** ([`adr/0062`](docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md)), and it is priced against the **design speed's** budget rather than the top rung ([`adr/0096`](docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)). Its value is a ratio nobody has both halves of |
| Sight Horizon | **1 Segment — derived, and there is nothing to choose** | **not tuning.** The floor is graph geometry; the ceiling is comparison symmetry ([`adr/0046`](docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)). ⚠ The other parameter this name was wearing is the **Rejoin crossing budget**, which is unset and is a different number |
| Temperament base and spread | **unset** | tuning. **The base/jitter blend weight has no argument behind it at all** and is the routing model's weakest number |
| Habit refresh cadence | **infinite — static per world** | ⚠ **RATIFICATION WITHDRAWN**; the value stands and nothing now rests on the ratifier. Adaptation is supplied as a *switch between static candidates*, so no cadence and no hash-bearing number returns |

## Definition of done for any milestone

This list is owned here; `docs/06-roadmap.md` rule 2 requires it and cites it. Cumulative
obligations, not milestones of their own. Refined per slice by `plans/0003 §Definition of done`.

- `dotnet build` succeeds and `dotnet test` is green, on a machine with no GPU and no Godot
- The invariants pass. **Sorted by frequency, never gated on build configuration** (`02 §10`) —
  `O(1)` at the write site per Tick, `O(n)` staggered, whole-world at end of run. The runs that
  surface these bugs are the million-Tick headless balance runs, and those are release builds
- The long-run test passes — 100k+ Ticks with **no collection and no magnitude** trending upward at
  steady state (`adr/0006`, and `adr/0003`'s extension of it to quantities)
- There is something to *look at* showing the milestone doing its job

Every milestone names the specific risk it retires. A milestone that cannot name one is either not
necessary yet or not understood well enough to start.

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
