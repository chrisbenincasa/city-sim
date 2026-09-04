# CLAUDE.md

Guidance for Claude Code working in this repository.

---

🔴 **AMNESTY IN FORCE. READ [`plans/0045-amnesty.md`](plans/0045-amnesty.md) AND NOTHING ELSE ON A
COLD START — not the board.** It is the only thing in flight and it supersedes *Where to look* below
for the duration. ⚠ **It ends at a RATIO and not on a date** — 30 words of prose per line of
simulation, 52 as of 2026-08-31 — so no calendar lifts it, and
`CorpusBudgetTests.The_amnesty_has_not_yet_earned_its_end` is what reports it earned. ⚠ **A ratio is
not a word count**: prose written beside new simulation is free, prose written alone is refused, and
doc-comments count on the numerator so nothing escapes `docs/` by relocating. No new ADRs may be
written, and `adr/0043` and `adr/0052` are suspended. ⚠ **A session that ends without a change under
`src/` is not committed.** Everything below this line is reference material to consult when a task
needs it — it is not a reading list.

---

## What this is

A city-builder where the city is made of people you can actually meet, the economy is made of Goods
that actually move, and when something goes wrong the game can say exactly why. Godot 4.7 is the host;
the simulation is an engine-agnostic C# library.

## Where to look

**Read [`plans/0000-board.md`](plans/0000-board.md) first on any cold start.** It is the only view of
what is in flight.

⚠ **This file holds no status, no counts, no timings and no per-slice narrative, and that is
deliberate.** It held all four, became a third copy of the board and the slice plans, and was the copy
that drifted — `plans/0012` **Cause 1**. Put a figure or an outcome in the document that owns it and
let this file point at it. **This file names where to look and never what you found** (`adr/0093`).

Five files answer five questions, one each. When the board disagrees with any of them, **they win**.

| File | The one question it answers |
|---|---|
| [`plans/0000-board.md`](plans/0000-board.md) | ***What is next*** — a view, never a source, and **never the home of an open question** |
| [`plans/0003-build-plan.md`](plans/0003-build-plan.md) | ***What is done*** — the slice ledger, its gates, and the hash-moving queue. **Start here when picking up the code cold** |
| [`plans/0002-open-questions.md`](plans/0002-open-questions.md) | ***What needs answering*** — every entry typed *measurable* or *arguable*, and **§D is the ledger of unratified numbers** |
| [`plans/0012-corpus-audit.md`](plans/0012-corpus-audit.md) | ***What a document says wrongly*** — corrections owed, which are not questions |
| [`plans/0013-tick-budget.md`](plans/0013-tick-budget.md) | ***What a Tick costs*** — one row per consumer, and whether its multiplicand was measured or guessed |

**The prose outweighs the simulation, it is known, and it is a standing concern on the board** — which
is why the board's rule is *an argument session runs when something concrete is blocked on it, never
because it is available.*

**A gated slice must not be started before its gate clears**, and several decisions on the critical
path are still open, so do not write implementation code beyond the current slice unless asked.

## Repository map

| Path | What it is |
|---|---|
| `CONTEXT.md` | **The domain vocabulary. Authoritative.** Every term, with exactly one meaning. Ends with *Terms we deliberately do not use* — those are banned outright |
| `PROCESS.md` | **The project vocabulary. Authoritative**, and `CONTEXT.md`'s sibling — slice, spike, gate, session, the numbering scheme, and the conventions every document is written to |
| `docs/00-vision.md` | Pillars, anti-goals, the argument against this design and the answer |
| `docs/01-player-experience.md` | Verbs, panels, notifications, overlays |
| `docs/02-simulation-model.md` | World model, Tick phases, Rule families, determinism rules, testing strategy |
| `docs/03-agent-architecture.md` | Movement, fidelity tiers, Trips and Legs |
| `docs/04-economy-and-goods.md` | The five Goods, chains, Office |
| `docs/05-technical-architecture.md` | Project layout, sim/render boundary, data layout, threading, saves |
| `docs/06-roadmap.md` | **The phase model, the four pacing rules, and the risk each milestone retires. Nothing else** — it sequences work and never describes the simulation (`adr/0042`) |
| `docs/07-the-drawing.md` | **What the city LOOKS like, and how the picture gets made** — the pillars of the drawing, the asset pipelines, the reference games. ⚠ **It owns the look and nothing else**; a sentence here about a mechanism belongs in `06` or `02` |
| `docs/movement-primer.md` | **Orientation only, and it owns nothing.** Movement and routing from first principles, for paging the subsystem back in. `03`, `CONTEXT.md` and the ADRs win against it always |
| `docs/adr/` | The decision records, numbered from `0001`. `0028` is reserved and unwritten. **Count them rather than quoting a total** |
| `docs/ruleset-reference.md` | **Every Ruleset key, with what it does. GENERATED — do not edit it.** Keys come from `RulesetLoader`'s record of what its readers asked for; the sentences are authored in `src/Borough.Formats/RulesetKeyNotes.cs`, and a test refuses both a key with no sentence and a sentence with no key. ⚠ **It states no values, defaults or ranges.** Regenerate with `--key-reference`; `RulesetReferenceTests` compares bytes |
| `docs/deferred.md` | What is deliberately not being built, with retrofit costs and revisit triggers |
| `docs/references.md` | Reference games and prior art, with the standing of each decision |
| `docs/spike-results.md` | Recorded spike numbers and the decision each produced |
| `docs/dev-environment.md` | Setting up a machine to work on this |
| `plans/0000a-board-archive.md` | **An index, not a record.** One line per closed board row, naming the document that owns the full version. **Do not quote it** — a one-line summary is a caveat-free compression of somebody else's sentence, which is `plans/0012` **Cause 5** by construction. Follow the link |
| `plans/0004`–`0053` | **One document per slice, spike or session, and each owns its own findings in full.** The board's third column points at these, never summarises them — read the plan rather than any description of it. `0001` is **stale**; `06` supersedes its build order |
| `.github/workflows/` | **The post-submit lane.** `commit.yml` runs the assertion tier on every push; `post-submit.yml` runs the whole suite and the long headless balance runs, on every push to `main` and nightly. ⚠ **A runner is not the reference machine**, so nothing it prints is a figure a document may quote (`adr/0121`) |
| `rulesets/` | **Ruleset content, in TOML** — data the binary interprets, hot-reloadable under `adr/0015`. Each file is a **demonstration rather than a city**, and **each carries its own header saying what it exists to show and what it must not be read as — read the header, never a description of it** (`adr/0093`). **Count the files rather than quoting a total.** ⚠ **Three are hash-bearing** — `declining.toml`, `declining-tuned.toml`, `congested.toml` — so editing one, **comments included**, moves a recorded hash in `GoldenFixtures`, in the committed `.borough` logs and in the trace headers; re-record with the command in `tests/Borough.Tests/Golden/README.md` rather than pasting numbers by hand. ⚠ **A demonstration Ruleset is a test fixture**, so a content edit moves what the suite covers and not only what it hashes. ⚠ **`minimal.toml` is the one file that carries the argument for a shared key**; every other file comments only what it changes. 🔴 **No shipped world can express *balance → unbalance → balance*** — see *Things to be careful about* |

## Working with the corpus

**`CONTEXT.md` governs vocabulary.** Domain terms are capitalised in prose — a Household, a Bin, a
Trip, a Segment, the Event Wheel. If a concept needs a name that is not in `CONTEXT.md`, add it there
first. Its *Terms we deliberately do not use* section is a list of failure modes the design has already
rejected, several by name — Agent, Cohort, Demand, Region.

**Decisions live in ADRs, not in prose.** A settled design question gets
`docs/adr/NNNN-lowercase-hyphenated-claim.md`, where the filename is the claim stated as a sentence.
[`PROCESS.md`](PROCESS.md) → *Conventions* owns the required structure, the guiding-concept tag, the
prose register, and the rule that **superseded documents get a banner, never a deletion**.

**Five rules govern what a sitting may conclude.** Each is an ADR; read it before leaning on it,
because the ADR carries the worked examples and the amendments and this table carries neither.

| Rule | What it governs | Read |
|---|---|---|
| **A claim a measurement could settle must not be settled by argument** | **claims** — *can you name the number that would refute this, and the machine that would produce it?* If yes it is **measurable**, and no document may cite it as decided until that number exists | `adr/0043` |
| **A hash-bearing number is chosen with a named ratifier or not at all** | **numbers** — record beside it in `plans/0002` §D the named thing that would ratify it and the trigger that would reopen it. **A category is not a name**; a ratifier names a machine, a world and a quantity | `adr/0052` |
| **An unbuilt mechanism is not a design constraint** | **absences** — classify it *unbuilt*, *undesigned* or *refused*. **Only *refused* is evidence.** The answer to *given X does not exist, should Y compensate?* is usually **build X** | `adr/0070` |
| **A description of the build is where to look, and never what you found** | **what the build does** — a sentence about a mechanism tells you which symbol to read, never what is in it. Writing half: **name a symbol, never a time** | `adr/0093` |
| **A local workaround is not a discharge** | **what a spike does with what it found** — route the finding to the code that owns it **on the day**. A defect → the code or `plans/0003`; a cost → `plans/0013`; a question → `plans/0002`; a document now wrong → `plans/0012` | `adr/0073` |

**And one rule about quotation, which is not about reasoning at all.** ***A caveat attached to a number
does not travel with it.*** **Reading**: quote the *sentence*, never the digits. **Writing**: name a
number after what it measures, not after where it sits. The special case is a share of a budget —
***carry the bill, not the percentage***. `plans/0012` **Cause 5** holds the sightings, the repair and
the **disqualifier registry** of figures a test refuses to let any document quote bare.

**The corpus checks itself mechanically.** `tests/Borough.Tests/Corpus/` holds the checks. **They are
all document-to-document**, so a number living in one place only, or in a doc-comment, is invisible to
every one of them. Read the tests for what is actually checked rather than any prose description.

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
declaration). `BOR08xx`
and `BOR0901` are not among the seven lints; the count stays seven. Lint 5 is live via `ReplayTests`
and the golden baseline. **Lint 6 is live** — `FactorioTests`, and stronger than a suite: a save's
header carries the State Hash of the world it holds, folded from the **copy**, so every load restores,
rebuilds, recomputes and refuses a mismatch (`adr/0112`). **Lint 4 alone still needs machinery that
does not exist yet.**

**Every diagnostic ships with a test that writes the violation and watches it fire** — do not add one
without.

**Every field in a table is declared once** as `(saved AND hashed)` or `(derived AND rebuilt)`, and
declaring it through `Rows.Saved`/`Rows.Derived`/`Rows.SavedHandle` is what *allocates* it — so the
State Hash cannot have a coverage hole. The hash folds values, never identity: a handle column folds
the target row's monotonic never-reused id, not the recycled slot index. Composition order is
**tables in declaration order, arrays in index order**.

⚠ **Declaring a column `Derived` allocates it; it does not make anything rebuild it.**
***A structure that lives outside the world is not derived state, however it is declared.***
`DerivedRebuildAuditTests` is the only thing that asks — it clears every derived column, rebuilds, and
names the ones no fixture populates.

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
however it was motivated. ⚠ **It classifies a change; it does not price one** (`adr/0100`): moving the
hash costs nothing while nobody is carrying a save, and **never cite hash movement as a reason to
defer, narrow or split work**. What survives is attribution — a hash move gets a commit whose subject
explains it.

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

**Every run mode, with its flags and the Ruleset it needs, is printed by `--help`** — `Options.Usage`
in `src/Borough.Headless/Options.cs` is the one place they are listed. ⚠ **What `--help` does not carry
is a sizing**, which is why the block below keeps one per mode — and ***a sizing is a property of the
reading, never of the mode***.

```
dotnet build                  # must succeed with no GPU and no Godot installed
dotnet run --project src/Borough.Headless -- --help
dotnet run --project src/Borough.Headless
dotnet run --project src/Borough.Headless -- --zones --ruleset rulesets/minimal.toml --ticks 5000
dotnet run --project src/Borough.Headless -- --kinds --ruleset rulesets/provisioned.toml --citizens 4000 --ticks 24576
dotnet run --project src/Borough.Headless -- --commute --ruleset rulesets/minimal.toml --ticks 4096
dotnet run --project src/Borough.Headless -- --traffic --ruleset rulesets/congested.toml --citizens 16000 --ticks 512
dotnet run --project src/Borough.Headless -- --evidence --ruleset rulesets/diagnosed.toml --citizens 4000 --ticks 2048
dotnet run --project src/Borough.Headless -- --money --ruleset rulesets/taxed.toml --citizens 2000 --ticks 8192
dotnet run --project src/Borough.Headless -- --parking --ruleset rulesets/congested.toml --citizens 4000 --ticks 4096
dotnet run --project src/Borough.Headless -- --land-value --ruleset rulesets/fouled.toml --citizens 4000 --ticks 21163
dotnet run --project src/Borough.Headless -- --arrivals --ruleset rulesets/crowded.toml --citizens 1000 --ticks 8192
dotnet run --project src/Borough.Headless -- --market --ruleset rulesets/provisioned.toml --citizens 2000 --ticks 24576
dotnet run --project src/Borough.Headless -- --school --ruleset rulesets/schooled.toml --citizens 2000 --ticks 300000 --schools 4
dotnet run --project src/Borough.Headless -- --flood --ruleset rulesets/flooded.toml --citizens 2000 --ticks 40960
dotnet run --project src/Borough.Headless -- --schema --ruleset rulesets/minimal.toml > rulesets/ruleset.schema.json
dotnet run --project src/Borough.Headless -- --key-reference --ruleset rulesets/minimal.toml > docs/ruleset-reference.md
npx @taplo/cli lint 'rulesets/*.toml'   # the schema, checked the way an editor applies it
dotnet run --project src/Borough.Headless -- \
  --ruleset rulesets/minimal.toml --reload-at 200 --ruleset rulesets/minimal-tuned.toml --ticks 400
```

## Running the tests

**Do not run the whole suite on every change, and do not run it before every commit either.** **Three
lanes** (`adr/0121`): what you run while working, what gates a commit, and what a runner does
afterwards while nobody waits.

| When | Command |
|---|---|
| **While working** — the default, and what you should be running nearly all the time | `scripts/test.sh` |
| **Narrower still** — while iterating on one area | `scripts/test.sh Policy` |
| **Before a commit** — the gate, and deliberately the same command as the default | `scripts/test.sh` |
| **Post-submit** — `.github/workflows/post-submit.yml`, on every push to `main` and nightly | `dotnet test -c Release`, then the long headless runs |
| **At a milestone** — the Definition of done, on the reference machine | `dotnet test -c Release` |

⚠ **`scripts/test.sh` is `dotnet test` with the failure list kept, and nothing else** — same lane, same
filter, same exit status. It **re-prints the failed test names last**, after the stack traces, and
**tees the run to a file** it names on the way in and out. ***A run that costs minutes must never have
to be repeated in order to be read***: reading a result is a `grep` against that log, never a second
run. `--all` runs the whole suite; `--filter 'EXPR'` takes an explicit expression; anything after `--`
goes to `dotnet test`. It also surfaces a **build** error, which `dotnet test` otherwise buries.

⚠ **Every timing figure is Release, on the reference machine, with nothing else running** — and this
file records none of them, because the ones it recorded went eleven days stale. Take the reading when
you need it, and put it in the document that owns it. **A runner may report that an instrument broke;
it may never supply a number a document quotes** (`adr/0106`, `adr/0121`). ⚠ **A quiet machine is a
control on a *capture*, not on a *run*** — the 36-minute suite may be run detached alongside other
work, including other tests; the only thing lost is a figure nobody was going to take.

⚠ **`Simulation.VerifyDecideWritesNothing` is OFF by default** — it folds every column of every table
twice a Tick, `O(world)` against a phase meant to be `O(woken)`, and it was the largest single cost in
the working lane. A test wanting the proof now **asks** for it, and `Borough.Headless` still defaults
it **on** through `Options.DecideGuard`, so the long balance runs are guarded exactly as before.
***A cost nobody opted into is a cost nobody reviews.***

**The axis is `assertion` against `instrument`, never small/medium/large** ([`plans/0032`](plans/0032-test-tiers.md)).
An **assertion** fails when the city changes and must run every time. An **instrument** produces a
figure for a document to quote. The test is *what would you do on the day it failed* — find out what
broke, or paste the new number into a document.

**The default is assertion, by absence.** A new test needs no attribute. Only an instrument opts out,
with `[Trait(Tier.Key, Tier.Instrument)]` — so **filter on `tier!=instrument` and never on
`tier=assertion`**, because the positive form selects only the tests that said what they were and drops
the rest.

**Two things keep this honest, and they are tests rather than conventions.** `TierBudgetTests` times
every test and fails if an assertion-tier one exceeds **4 minutes**. `TierDeclarationTests` refuses a
third tier and asserts instruments stay under a quarter of the suite. ⚠ **Neither is a licence to raise
the budget**: a test over it is either an instrument that forgot to say so, or an assertion that has
become a real regression in the city. ⚠ **And a per-test budget cannot see a class-shaped cost** —
xUnit serialises per class, so a long class made of individually-green tests passes it (`plans/0012`).

## Constants

**This table states values, not arguments.** Each row's reasoning lives in the ADR named beside it, and
its ratification status lives in [`plans/0002`](plans/0002-open-questions.md) **§D** — D1 in use and
unratified, D2 unset. ⚠ **Nearly every
number here is UNRATIFIED**, so treat §D as the authority on what is settled and this table as the
authority on nothing but the current value. ⚠ **Do not quote a value out of this table into another
document** — the clause that says what it measures is in the ADR, and it will not travel with the
digits.

**Kind** decides how a number may be changed: *design* never moves; *world-creation* is baked into the
save; *tuning* is hot-reloadable Ruleset data. **Hash-bearing** means changing it is a change to the
city under `05 §4`, not an optimisation.

| Constant | Value | Kind | Read |
|---|---|---|---|
| `TICKS_PER_DAY` — `Ticks.PerDay` | **2048** — a Tick is 42.1875 s of in-world time | world-creation, hash-bearing. A `const` where `adr/0015` says it should be Ruleset data — filed in `plans/0012` | `adr/0094` |
| `WHEEL_SIZE` | **2048 Ticks** | world-creation. Set by the longest routine sleep, so it moves with `TICKS_PER_DAY` | — |
| Reference tick rate | 16 Ticks/s → a Day is 2m08s | host-side, runtime only. Ladder: pause / 0.5× / 1× / 2× / 3× / 4× | `01 §1` |
| Cell | 32×32 Tiles (≈128 m) | **design constant, never tuned** — it changes the State Hash | — |
| Chunk | a multiple of the Cell, ≥32×32 | tuning, hash-preserving. **Provisionally 1:1 with the Cell** | — |
| Map | **16384² Tiles** — `CellGrid.WorldCells = 512`, 65.5 km a side | world-creation. Sized by how many Commute Budgets fit across it, never by area. ⚠ **Not hash-bearing** | `adr/0089` |
| Target population | 10,000 first hour / 1,000,000 late game | sizing | — |
| Tick budget | **15.6 ms at 4×, at 1,000,000 Citizens, on ONE CORE of the reference class** | ✅ settled. ⚠ **Quote the machine and the thread count with the number.** **1× is the speed a capability is priced against**, and is not the target | `adr/0105`, `adr/0106` |
| Map Layer cadence | pollution every 64 Ticks at offset 0; land value every 256 at offset 16 | tuning, hash-bearing — the designer's number, not the profiler's | `adr/0044` |
| Industrial pollution kernel | separable tent, 1,024 m (8 Cells) | world-creation, Ruleset data — `[layers] kernel_metres`, refused on reload | — |
| Provenance trail cap (`N`) | **16** transitions retained in full, older ones aggregated | world-creation, saved, hash-bearing. `RulesetTrailTable.Retained`. A `const` on purpose: a designer must not be able to reload a smaller window | — |
| A `[[kind]]`'s occupancy | **`[[building]] occupants`**, declared per kind and derived from the ground | tuning, hash-bearing. Derived from the Ruleset in force, so lowering it **evicts** the overflow | `adr/0068` |
| A `[[kind]]`'s employment | **8**, on `dwelling` | tuning, hash-bearing — `[[building]] jobs`. Counts **Citizens**, never Households. **Derived rather than chosen** | — |
| Placement pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hash-bearing — `[placement]`. The **sample is derived** from the duration; the duration is not. ⚠ **`revisit_ticks` is a RATE and not coverage** — the sample is drawn with replacement | `adr/0069`, `adr/0059` |
| The Pool's give-up bound | **120 Days** — `[placement] gives_up_after_days`, `rulesets/bordered.toml` only | tuning, hash-bearing. **A duration, and the occasion count derives from it.** ⚠ **Required of any Ruleset declaring a gate kind and refused elsewhere.** **Absent means nobody ever gives up** | `adr/0130` |
| Job assignment pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hash-bearing — `[jobs]`. **There is no search radius key and that absence is the decision** — the box derives from the Commute Budget | `adr/0081` |
| Road Graph geometry | `block_tiles = 32`, `arterial_count = 0`, `arterial_junction_tiles = 512`, `foot_crossing_every = 4`, `foot_paths_per_thousand_blocks = 40` — `[roads]` | world-creation, Ruleset data, hash-bearing. ⚠ `foot_crossing_every` is **inert** at the shipped lattice; `foot_paths_per_thousand_blocks` is the stronger Severance lever | — |
| `lots_per_segment` | **5** | world-creation, Ruleset data, hash-bearing — `[lots]`. **Derived rather than chosen**: it is `CONTEXT.md` → Address's own *five Buildings share a Segment*. **A Lot has no depth and there is no depth key** | `adr/0078` |
| Free-flow speeds and capacities | 50 / 90 / 5 km/h; 3,600 / 12,000 / 1,000 Vehicles per hour | tuning, hash-bearing. Free-flow is `(derived AND rebuilt)`, so retuning a speed moves the **standing** city | — |
| The crossing cost | **30 s** | tuning, hash-bearing — `[trips]`. ⚠ **The derivation was looked for and there is none** — chosen against a stated band | `adr/0074` |
| Commute Budget | **three rungs — fast 20, moderate 40, unsavoury 50 clock minutes** | tuning, hash-bearing — `[trips]`. **Only the ceiling refuses.** ⚠ They are percentiles of a **free-flow, foot-only** distribution — do not read them against a vehicle-denominated column | `adr/0095` |
| Volume-delay function | **α 15%, β 4, clamp 400%** — `rulesets/congested.toml` only | tuning, hash-bearing — `[traffic]`. BPR, priced **on entry**. **Absent means roads never slow down.** ⚠ **It is a loop and not a formula** | `adr/0099` |
| The import ceiling on a Good | **the MINIMUM `[[hinterland]] prices` entry across every declared Hinterland** | tuning, hash-bearing. **The one authored anchor under every price in the design.** ⚠ **The `min` is DERIVED and not chosen.** ⚠ **Required of any file that states `[districts]` and refused nowhere else** | `adr/0135`, `adr/0050` |
| The market's damping | **`decay_percent` 50, `move_cap_percent` 10** — `[market]` | tuning, hash-bearing. **Neither key is a price.** **Absent means every trade clears at the ceiling for ever.** ⚠ **A flat price is evidence of nothing on its own** — read `stock` and `rate/Day` beside it, and note that ***the scarce/glutted distinction is a property of a population and a horizon, never of a Ruleset*** | `adr/0171`, `adr/0135` |
| Household car ownership | **100%** — `rulesets/congested.toml` only | tuning, hash-bearing — `[households] car_ownership_percent`. A Citizen drives everywhere or walks everywhere. **Absent means nobody drives**, reached by omitting the table rather than by a defaulted key. ⚠ **Mode choice is a different question**, *undesigned* rather than unbuilt | `adr/0098` |
| The Shift model | `shift_start_earliest_hour`/`latest_hour` **6–10**; `[jobs] shift_hours_min`/`max` **6–10**; `[jobs] arrive_early_max_minutes` **15** | tuning, hash-bearing. A commute is **two journeys** anchored on a Shift start hour belonging to the **Workplace**, so a Citizen stores no start hour at all. **The Day's shape is emergent.** Tick 0 is midnight | `adr/0101` |
| The Parking Shed's radius | **400 m** — `[parking] radius_metres` | tuning, hash-bearing. How far a driver will walk from a Car Park | `adr/0009`, `adr/0120` |
| The Parking Shed's cap | **24** — `[parking] shed_keeps` | tuning; **hash-neutral today, hash-bearing once the shed is materialised**. **Not redundant with the radius** — the cap bounds the work and the radius bounds the walk | — |
| Microscopic Cap | **unset** | fixed world constant, still open. **It counts *Vehicles*, not Segments**, and is priced against the **design speed's** budget rather than the top rung | `adr/0062`, `adr/0096` |
| Sight Horizon | **1 Segment — derived, and there is nothing to choose** | **not tuning.** ⚠ The other parameter this name was wearing is the **Rejoin crossing budget**, which is unset and is a different number | `adr/0046` |
| Temperament base and spread | **unset** | tuning. **The base/jitter blend weight has no argument behind it at all** | — |
| Habit refresh cadence | **infinite — static per world** | ⚠ **RATIFICATION WITHDRAWN**; the value stands and nothing now rests on the ratifier | — |

## Definition of done for any milestone

This list is owned here; `docs/06-roadmap.md` rule 2 requires it and cites it. Cumulative obligations,
not milestones of their own. Refined per slice by `plans/0003 §Definition of done`.

- `dotnet build` succeeds and **`dotnet test` — the whole suite, unfiltered — is green**, on a machine
  with no GPU and no Godot. ⚠ **This sentence is a *milestone*'s gate and always was; a commit's is the
  assertion tier, and a runner's is neither** (`adr/0121`). ***Narrowing what the gate names is an ADR
  and not a config edit*** ([`plans/0032`](plans/0032-test-tiers.md))
- The invariants pass. **Sorted by frequency, never gated on build configuration** (`02 §10`) — `O(1)`
  at the write site per Tick, `O(n)` staggered, whole-world at end of run
- The long-run test passes — 100k+ Ticks with **no collection and no magnitude** trending upward at
  steady state (`adr/0006`, and `adr/0003`'s extension of it to quantities)
- ~~There is something to *look at* showing the milestone doing its job~~ 🔴 **AMENDED 2026-08-26 —
  that clause was being satisfied by a column of hexadecimal.** ***A milestone is done when you have
  watched it happen and something surprised you*** ([`plans/0045`](plans/0045-amnesty.md))

Every milestone names the specific risk it retires. A milestone that cannot name one is either not
necessary yet or not understood well enough to start.

## Things to be careful about

- **Don't reach for an ECS.** `adr/0004` rejected it explicitly: the population is homogeneous and ECS
  earns its complexity through heterogeneous composition.
- **Don't add a demand scalar.** There is no RCI meter. The Unplaced Pool *is* the demand signal.
- **Don't collapse Citizens into groups.** No Cohorts, no shared decisions, ever (`adr/0005`).
- **Don't let fidelity depend on the camera.** Fidelity is a property of place, driven by Stress
  (`adr/0007`). The renderer cannot influence the simulation.
- **Don't add a collection without a sink.** `adr/0006` — nothing grows with elapsed time.
- 🔴 **No shipped world can express *balance → unbalance → balance*, and this is the standing constraint
  on every demonstration Ruleset.** A premises Rule cannot fail on anything outside its own Building:
  `local` is circular, `global` is money-family only and a Building never holds money, `map` is
  write-only, a term reaching the tenant's Bins is refused at the parse site, and **`pool` throws at
  `RuleEngine.Buy`**. ***So a premises Rule chain is always-succeeds or never-succeeds*** —
  `declining.toml` is one pole and `maintained.toml` the other. ⚠ **A tenant now has a middle**; a
  premises got a **refusal**, which under `adr/0070` is the one classification that *is* evidence. **So
  the premises threshold is waiting on an argument about what a premises Rule is for, not on a
  mechanism**, and it is why no decline number in `plans/0002` §D1 can be ratified — ***a threshold
  measured in a world where failure is certain, or impossible, is measuring a stopwatch and not a
  design.*** The full version is `adr/0168` and `plans/0002` §A; do not write a third copy here.
- **Don't move a mechanism between Rule families for performance.** Bin Rules and Sweep Rules differ in
  observable behaviour, so moving one is a change to the city (`adr/0033`).
- **Prefer off-the-shelf infrastructure.** `adr/0018` — Citybound shipped ten bespoke libraries, three
  engine rewrites, and no game. A bespoke component requires a written exception naming the property no
  library provides.
