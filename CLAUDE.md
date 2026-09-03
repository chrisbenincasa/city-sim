# CLAUDE.md

Guidance for Claude Code working in this repository.

---

🔴 **AMNESTY IN FORCE. READ [`plans/0045-amnesty.md`](plans/0045-amnesty.md) AND NOTHING ELSE ON A
COLD START.** It is one page, it is the only thing in flight, and it supersedes *Where to look* below
for the duration. **Do not read the board.** ⚠ **It ends at a RATIO and not on a date** — 30 words of
prose per line of simulation, **52 on 2026-08-31** — so no calendar lifts it and
`CorpusBudgetTests.The_amnesty_has_not_yet_earned_its_end` is what reports it earned. ⚠ **The cap is a RATIO and not a word count** — prose
written beside new simulation is free, prose written alone is refused, and doc-comments count on
the numerator so that nothing escapes `docs/` by relocating. No new ADRs may be written, and
`adr/0043` and `adr/0052` are suspended. ⚠ **A session that ends
without a change under `src/` is not committed.** Everything below this line is reference material to
be consulted when a task needs it — it is no longer a reading list.

---

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
| `docs/07-the-drawing.md` | **What the city LOOKS like, and how the picture gets made.** The four pillars of the drawing, the three asset pipelines and the recommendation, the reference games, and what a screenshot actually costs. ⚠ **It owns the look and nothing else** — a sentence here about a mechanism is `06`'s or `02`'s and belongs there. 🔴 **Its §5 holds an open question that belongs in `plans/0002`** and is parked here because that file is at its word ceiling |
| `docs/movement-primer.md` | **Orientation only, and it owns nothing.** Movement and routing rebuilt from first principles, for paging the subsystem back in. Stores no status and almost no numbers, which is what keeps it from drifting. `03`, `CONTEXT.md` and the ADRs win against it always |
| `docs/adr/` | The decision records, numbered from `0001`. `0028` is reserved and unwritten. **Count them rather than quoting a total** — a count in prose is a fact that drifts |
| `docs/ruleset-reference.md` | **Every Ruleset key, with what it does. GENERATED — do not edit it.** The key set comes from `RulesetLoader`'s own record of what its readers asked for; the sentences are authored once in `src/Borough.Formats/RulesetKeyNotes.cs`, and **a test refuses both a key with no sentence and a sentence with no key**. ⚠ **It states no values, no defaults and no ranges** — the loader carries the range and delivers it in the refusal, each file in `rulesets/` carries what it demonstrates, and `plans/0002` §D carries what is ratified. Regenerate with `--key-reference`; `RulesetReferenceTests` compares bytes |
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
| `plans/0004`–`0037` | **One document per slice, spike or session, and each owns its own findings in full.** The board's third column is a pointer to these, never a summary — so read the plan rather than any description of it. `0001` predates ADRs 0005–0011 and is **stale**; `06` supersedes its build order |
| `.github/workflows/` | **The post-submit lane, and the repository's first CI.** `commit.yml` runs the assertion tier on every push; `post-submit.yml` runs the whole suite and **three** long headless balance runs — ⚠ **count them rather than trusting this number**; it said *two* until 2026-08-23 — one on `minimal.toml`; one on `fouled.toml`, the only shipped file whose Rules emit and therefore the only long run that reaches the Map Layers at all; and one on `evicted.toml`, **the only world in which a TENANCY ENDS**, where the other two condemn Buildings and end none. ⚠ **The third is there for a COLLECTION rather than a crash** — a tenant's Rule Instances and Bins live exactly as long as the tenancy, and that file cycles by construction — ***but the runner cannot answer `adr/0006` and is not asked to***: nothing reads the census, so what the lane buys is that the world stays reachable for whoever next takes the measurement. **on every push to `main` and nightly as a backstop**. ⚠ **A runner is not the reference machine**, so nothing it prints is a figure a document may quote ([`adr/0121`](docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)) |
| `rulesets/` | **Ruleset content, in TOML** — data the binary interprets, hot-reloadable under `adr/0015`. Each is a **demonstration rather than a city** (⚠ **count them rather than quoting a total**; this cell has been wrong about the count three times). 🔴 **THIS CELL WAS 28,065 CHARACTERS AND IS NOW A POINTER, WHICH IS [`plans/0050`](plans/0050-the-ruleset-sweep.md)'s FINDING APPLIED TO THE DOCUMENT THAT DESCRIBED IT.** It was a second copy of thirty file headers — it named 27 of 31 files, described `monetised.toml` as *the first to declare a `family = "money"` Resource* long after that moved into the baseline, and still listed it after it was retired. ***A cell that summarises thirty documents drifts from all thirty at once***, which is `plans/0012` **Cause 1** at its largest scale in this corpus. **Every file carries its own header saying what it exists to show and what it must not be read as — read the header, not a description of it** ([`adr/0093`](docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)). What still lives here, because it is about the *directory* rather than about any file: ⚠ **Three files are hash-bearing** — `declining.toml`, `declining-tuned.toml` and `congested.toml` — so editing one, **comments included**, moves a recorded content hash in `GoldenFixtures`, in the committed `.borough` logs and in the trace headers; re-record with the command in `tests/Borough.Tests/Golden/README.md` rather than pasting numbers by hand. ⚠ **A demonstration Ruleset is a test fixture**, so a content edit moves what the suite *covers* and not only what it hashes. ⚠ **`minimal.toml` is the one file that carries the argument for a shared key**; the other 29 comment only what they change, and say so in one line at the top of their body. 🔴 **No shipped world can express *balance → unbalance → balance*** — see *Things to be careful about*, which owns that constraint in full. 🔴 **Read a file's header before quoting any number out of a run of it**: most of them state, in their own words, that the numbers they produce ratify nothing |

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
live via `ReplayTests` and the golden baseline. **Lint 6 is live as of milestone 8** — `FactorioTests`,
and stronger than a suite: a save's header carries the State Hash of the world it holds, folded from the
**copy**, so every load restores, rebuilds, recomputes and refuses a mismatch
([`adr/0112`](docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md)).
**Lint 4 alone still needs machinery that does not exist yet.**
**Every diagnostic ships with a test that writes the violation and watches it fire** — do not add one
without.

**Every field in a table is declared once** as `(saved AND hashed)` or `(derived AND rebuilt)`, and
declaring it through `Rows.Saved`/`Rows.Derived`/`Rows.SavedHandle` is what *allocates* it — so the
State Hash cannot have a coverage hole. The hash folds values, never identity: a handle column folds
the target row's monotonic never-reused id, not the recycled slot index. Composition order is
**tables in declaration order, arrays in index order**.

⚠ **Declaring a column `Derived` allocates it; it does not make anything rebuild it.** The
allocation-by-declaration trick closes the *hash*'s coverage hole and leaves the *rebuild*'s wide open —
a column can be declared `Derived` while the structure that derives it lives outside the `World`
entirely, in which case a load restores the rows and the index is simply never built. Nothing fails,
because a column nobody reads yet is a column nobody reads yet. ***A structure that lives outside the
world is not derived state, however it is declared.*** `DerivedRebuildAuditTests` is the only thing that
asks — it clears every derived column, rebuilds, and names the ones no fixture populates — and it caught
exactly this on milestone 7's `car_park.segment_next`.

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

**Do not run the whole suite on every change, and do not run it before every commit either.** A full
`dotnet test` is **36m22s** in Release, of which **34m22s is one test** — and that test prices an
allocator rather than asking whether the city is correct. **Three lanes**
([`adr/0121`](docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)):
what you run while working, what gates a commit, and what a runner does afterwards while nobody
waits.

| When | Command | Cost |
|---|---|---|
| **While working** — the default, and what you should be running nearly all the time | `scripts/test.sh` | **6m16s**, 2,441 tests |
| **Narrower still** — while iterating on one area | `scripts/test.sh Policy` | seconds |
| **Before a commit** — the gate, and deliberately the same command as the default | `scripts/test.sh` | **6m16s** |
| **Post-submit** — `.github/workflows/post-submit.yml`, **on every push to `main`** and nightly, on a runner | `dotnet test -c Release`, then three long headless runs | nobody's |
| **At a milestone** — the Definition of done, on the reference machine | `dotnet test -c Release` | **~36m** |

⚠ **`scripts/test.sh` is `dotnet test` with the failure list kept, and nothing else** — same lane,
same filter, same exit status, so it is a wrapper rather than a fourth lane. What it adds is that a
failing run **re-prints the failed test names last**, after the stack traces, and **tees the whole
run to a file** it names on the way in and out. ***A run that costs minutes must never have to be
repeated in order to be read***: reading a result is a `grep` against that log, never a second run.
`--all` runs the whole suite; `--filter 'EXPR'` takes an explicit expression; anything after `--`
goes to `dotnet test`. ⚠ **It also surfaces a BUILD error**, which `dotnet test` otherwise buries
above thousands of lines of restore output and which reads, at a truncated tail, exactly like a test
failure with no name.

⚠ **The lane's cost names *nothing else running in this repository* as its first control**, and
most readings here did not: 1m52s and 50s were taken while a second session ran `Borough.Tests` on
the same six cores, **and so was the 6m16s above** — a second testhost was up for all of it. They are
recorded as **upper bounds** for that reason, which is the one thing a spoiled measurement is still
good for. ***A test-cost capture is a parallelism
measurement, so it takes a parallelism measurement's controls*** — the rule `plans/0000` already
carried from a threading capture that read bimodally on 2026-08-14.

🔴 ⚠ **THE 42s THAT STOOD HERE WAS ELEVEN DAYS STALE.** Measured 2026-08-19 at 1,690 tests; the lane
was **8m03s by 08-25** and **9m46s on 08-30**, and nothing re-took it. The instrument that would have
said so is `TierBudgetTests`' slowest-ten list, which prints through `ITestOutputHelper` — **xUnit
surfaces that only for a FAILING test.** It passed every run, so the list was computed and thrown away
every run. ***An instrument whose output nobody can see is not an instrument.*** ⚠ **And the budget it
guards could not have caught this**: `TierBudget.PerTest` is denominated **per test** at 4 minutes
while xUnit serialises **per class**, so `FoundingTests` was a **420s critical path made of five
individually-green tests**. ***A per-test budget cannot see a class-shaped cost.***

⚠ **`Simulation.VerifyDecideWritesNothing` is OFF by default since 2026-08-30, and that default was
43% of this lane's CPU** — it folds every column of every table twice a Tick, `O(world)` against a
phase meant to be `O(woken)`. Measured on `FoundingTests`: **5m24s guarded against 55s unguarded**.
The lane went **4,186s → 2,863s of CPU**. A test wanting the proof now **asks** for it — eight cheap
classes do, at 37s the lot — and `Borough.Headless` still defaults it **on** through
`Options.DecideGuard`, so the long balance runs are guarded exactly as before. ***A cost nobody opted
into is a cost nobody reviews.*** ⚠ **The critical path is now `ReplayTests` at 294.7s**, and the
residue below it is genuine acceptance work rather than a default.

⚠ **Past five minutes a test stifles iteration and ten is the ceiling** — the band `adr/0121` records,
and it is a preference about a working loop rather than a claim about the city, so no measurement
settles it and [`adr/0043`](docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
does not reach it.

⚠ **A QUIET MACHINE IS A CONTROL ON A CAPTURE AND NOT ON A RUN**, and the paragraph above is about
*taking a reading*. It was misread on 2026-08-20 as a reason not to run the full suite detached while
doing other work. It is not: read from the test rather than from prose about it
([`adr/0093`](docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)),
`ParkingArrivalStreamTests`' **only two assertions in the whole class** are *the stream was not empty*,
and **nothing in it names a clock**. Noise cannot fail it. What noise costs is the **accuracy of the
figure it prints**, which is nobody's until somebody quotes it. ***So the 36-minute suite may be run
detached alongside other work, including other tests*** — the only thing lost is a figure nobody was
going to take ([`adr/0121`](docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md),
amended). ***A gate asks whether the city is correct; a capture asks how fast it is; only the second
needs the room silent.***

⚠ **A runner may report that an instrument *broke*; it may never supply a number a document
quotes.** Every timing figure in this corpus names the reference machine
([`adr/0106`](docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md))
and a hosted runner is not it — its class is not even stable between runs. ***A number produced on
an unnamed machine is not the number it looks like.*** **Producing a figure stays a deliberate act
on the reference machine**; CI tells you to go and re-measure, and is not the measurement.

⚠ **Release, not Debug.** Every figure above is Release. Debug is several times slower and is not
what any measurement in this corpus was taken on — a Debug full run was still going at **42 minutes**
with the three slowest *classes* excluded. ***A duration quoted without its build configuration is not a
duration***, which is [`adr/0106`](docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)'s
rule about a wall-clock budget arriving one level down.

**The axis is `assertion` against `instrument`, never small/medium/large** ([`plans/0032`](plans/0032-test-tiers.md)).
An **assertion** fails when the city changes and must run every time. An **instrument** produces a
figure for a document to quote, and re-running it re-derives a constant that did not move. The test
is *what would you do on the day it failed* — find out what broke, or paste the new number into a
document.

**The default is assertion, by absence.** A new test needs no attribute. Only an instrument opts out,
with `[Trait(Tier.Key, Tier.Instrument)]` — so **filter on `tier!=instrument` and never on
`tier=assertion`**, because the positive form selects only the seventeen tests that said what they
were and drops the sixteen hundred that did not.

**Two things keep this honest, and they are tests rather than conventions.** `TierBudgetTests` times
every test through an assembly-level hook and fails if an assertion-tier one exceeds **4 minutes** —
so a slow test landing untagged goes red rather than quietly becoming the new critical path. And
`TierDeclarationTests` refuses a third tier and asserts instruments stay under a quarter of the
suite. ⚠ **Neither is a licence to raise the budget**: a test over it is either an instrument that
forgot to say so, or an assertion that has become a real regression in the city.

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
| A `[[kind]]`'s occupancy | **4** in 28 of the 34 declarations; **3** in three and **1** in three | tuning, hash-bearing — `[[building]] occupants` ([`adr/0068`](docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)). Derived from the Ruleset in force, so lowering it **evicts** the overflow |
| A `[[kind]]`'s employment | **8**, on `dwelling` | tuning, hash-bearing — `[[building]] jobs`. Counts **Citizens**, never Households. **Derived rather than chosen**, and it puts full employment out of reach by construction. ⚠ It sits on the *dwelling* kind because a workplace kind needs a second `[[zone_rule]]` or the city fills with offices |
| Placement pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hash-bearing — `[placement]` ([`adr/0069`](docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)). The **sample is derived** from the duration ([`adr/0059`](docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)); the duration is not. ⚠ **`revisit_ticks` is a RATE and not coverage** — the sample is drawn **with replacement**, so about `1/e` of the Pool goes unlooked-at in any period. The doc comment saying otherwise is filed in `plans/0012` |
| The Pool's give-up bound | **120 Days** — `[placement] gives_up_after_days`, `rulesets/bordered.toml` only | tuning, hash-bearing ([`adr/0130`](docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)). How long a Household keeps looking before it gives up and leaves. **A duration, and the occasion count derives from it** — authoring the count would make the felt quantity move whenever a cadence was retuned. ⚠ **Required of any Ruleset declaring a gate kind and refused elsewhere**: a Pool with an inflow and no sink is `adr/0006`. **Absent means nobody ever gives up**, which is only coherent in a file with no door in it |
| Job assignment pacing | `interval = 32`, `revisit_ticks = 1024`, `candidates = 3` | tuning, hash-bearing — `[jobs]` ([`adr/0081`](docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)). **There is no search radius key and that absence is the decision** — the box derives from the Commute Budget, and `[jobs]` without one is refused at load |
| Road Graph geometry | `block_tiles = 32`, `arterial_count = 0`, `arterial_junction_tiles = 512`, `foot_crossing_every = 4`, `foot_paths_per_thousand_blocks = 40` | world-creation, Ruleset data, hash-bearing — `[roads]`. ⚠ `foot_crossing_every` is **inert** at the shipped lattice rather than merely unratified; `foot_paths_per_thousand_blocks` is the stronger Severance lever. `arterial_count` went to 0 because an Arterial is a player tool that does not belong in a generator |
| `lots_per_segment` | **5** | world-creation, Ruleset data, hash-bearing — `[lots]` ([`adr/0078`](docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)). **Derived rather than chosen**: it is `CONTEXT.md` → Address's own *five Buildings share a Segment*, and therefore the premise of the *an Address is never a Node* refusal. **A Lot has no depth and there is no depth key** |
| Free-flow speeds and capacities | 50 / 90 / 5 km/h; 3,600 / 12,000 / 1,000 Vehicles per hour | tuning, hash-bearing. Free-flow is `(derived AND rebuilt)`, so retuning a speed moves the **standing** city. The speeds have a source outside the corpus; the capacities do not |
| The crossing cost | **30 s** | tuning, hash-bearing — `[trips]` ([`adr/0074`](docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)). What it costs on foot to reach the other side of a Segment. ⚠ **The derivation was looked for and there is none** — both candidates rest on unbuilt mechanisms, so it is chosen against a stated band |
| Commute Budget | **three rungs — fast 20, moderate 40, unsavoury 50 clock minutes** | tuning, hash-bearing — `[trips]` ([`adr/0095`](docs/adr/0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)). **Only the ceiling refuses**; the two rungs below grade a commute that happens anyway. ⚠ They are percentiles of a **free-flow, foot-only** distribution — do not read them against a vehicle-denominated column |
| Volume-delay function | **α 15%, β 4, clamp 400%** — `rulesets/congested.toml` only | tuning, hash-bearing — `[traffic]` ([`adr/0099`](docs/adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md)). BPR, priced **on entry** from that Segment's volume at that instant. **Absent means roads never slow down.** ⚠ **It is a loop and not a formula** — congestion slows a Vehicle, a slower Vehicle dwells longer, longer dwell *is* higher volume |
| The import ceiling on a Good | **the MINIMUM `[[hinterland]] prices` entry across every declared Hinterland** — `rulesets/twinned.toml` only | tuning, hash-bearing ([`adr/0135`](docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md), [`adr/0050`](docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)). **The one authored anchor under every price in the design.** ⚠ **The `min` is DERIVED and not chosen** — there is no haulage term at 12, so every gate is equidistant and a city buys at the cheapest; when `adr/0133`'s charge ships it becomes a per-District `min(price + haul)` and stops being a property of the Ruleset. ⚠ **Required of any file that states `[districts]` and refused nowhere else** — a Pool with no ceiling is not unanchored, it is free |
| The market's damping | **`decay_percent` 50, `move_cap_percent` 10** — `[market]`; ⚠ **three files state it** — `twinned.toml`, `provisioned.toml`, `oversupplied.toml` | tuning, hash-bearing ([`adr/0135`](docs/adr/0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md)). **Neither key is a price** — a Pool opens at the ceiling and moves from there, so no seed exists and none is needed. **Absent means every trade clears at the ceiling for ever**, which is the city the other ten shipped files have. ⚠ **`decay_percent` 0 is ALLOWED and means no smoothing; 100 is refused** as a rate that never moves. ✅ **THE MECHANISM RUNS as of [`adr/0171`](docs/adr/0171-a-markets-level-is-what-its-sellers-hold-and-the-price-divides-by-the-sum-while-a-wake-spends-the-maximum.md)**: a Pool holds nothing, so the cover is **the sum over the row's sellers**, and `oversupplied.toml` goes **100 → 58** with 11 changes across eight rows — ⚠ **at 2,000 Citizens over 24,576 Ticks, which is the reading's world and not a property of the file**. ⚠ **It had moved no price on any world before that date and the reason changed TWICE** — first `Scope.Pool` threw, then the cover was read off the Bin [`adr/0139`](docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) had emptied. 🔴 ⚠ **`provisioned.toml` STILL PRINTS A FLAT PRICE AND THAT IS NOW THE MECHANISM**: it holds under a Day's cover, and a market under a Day's cover prices at its import ceiling because there is nothing to undercut it with. ***All three states print the same column***, so a flat price is evidence of nothing on its own — read `stock` and `rate/Day` beside it. 🔴 ⚠ **THE COVER ON THIS FILE IS A READING AND IT HAS MOVED TWICE.** It was *192 sundries against a 357/Day draw*; at `plans/0053`, with occupancy deriving from the ground, the same command reads **188 against 622**, and `MarketDumpTests` had to move to **4,000 Citizens** because at 2,000 the over-supplied world came *under* cover too and the two files stopped differing. ⚠ **Both files glut given long enough** — 98,304 Ticks takes `provisioned.toml` to 100 → 21 — so ***the scarce/glutted distinction is a property of a population and a horizon, never of a Ruleset***. **Both keys stay UNRATIFIED**; `plans/0002` §D1 holds them |
| Household car ownership | **100%** — `rulesets/congested.toml` only | tuning, hash-bearing — `[households] car_ownership_percent` ([`adr/0098`](docs/adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md)). A Citizen drives everywhere or walks everywhere. ⚠ **Mode choice is a different question, settled by no ADR** and *undesigned* rather than unbuilt. **Absent means nobody drives**, reached by omitting the table rather than by a defaulted key |
| The Shift model | `shift_start_earliest_hour`/`latest_hour` **6–10**; `[jobs] shift_hours_min`/`max` **6–10**; `[jobs] arrive_early_max_minutes` **15** | tuning, hash-bearing ([`adr/0101`](docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md)). A commute is **two journeys** anchored on a Shift start hour belonging to the **Workplace**, so a Citizen stores no start hour at all. **The Day's shape is emergent.** Tick 0 is midnight |
| The Parking Shed's radius | **400 m** — `[parking] radius_metres`, in every Ruleset that states `[parking]` | tuning, hash-bearing ([`adr/0009`](docs/adr/0009-parking-is-modelled-supply-never-search.md), [`adr/0120`](docs/adr/0120-a-car-park-is-not-a-bin-and-supply-is-at-buildings-until-a-segment-needs-one.md)). How far a driver will walk from a Car Park |
| The Parking Shed's cap | **24** — `[parking] shed_keeps`, in every Ruleset that states `[parking]` | tuning; **hash-neutral today, hash-bearing once the shed is materialised**. How many Car Parks a shed keeps, and therefore how far the query walks before it stops. **Not redundant with the radius** — the cap bounds the work and the radius bounds the walk, and they bind in different worlds |
| Microscopic Cap | **unset** | fixed world constant, still open. **It counts *Vehicles*, not Segments** ([`adr/0062`](docs/adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md)), and it is priced against the **design speed's** budget rather than the top rung ([`adr/0096`](docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md)). Its value is a ratio nobody has both halves of |
| Sight Horizon | **1 Segment — derived, and there is nothing to choose** | **not tuning.** The floor is graph geometry; the ceiling is comparison symmetry ([`adr/0046`](docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)). ⚠ The other parameter this name was wearing is the **Rejoin crossing budget**, which is unset and is a different number |
| Temperament base and spread | **unset** | tuning. **The base/jitter blend weight has no argument behind it at all** and is the routing model's weakest number |
| Habit refresh cadence | **infinite — static per world** | ⚠ **RATIFICATION WITHDRAWN**; the value stands and nothing now rests on the ratifier. Adaptation is supplied as a *switch between static candidates*, so no cadence and no hash-bearing number returns |

## Definition of done for any milestone

This list is owned here; `docs/06-roadmap.md` rule 2 requires it and cites it. Cumulative
obligations, not milestones of their own. Refined per slice by `plans/0003 §Definition of done`.

- `dotnet build` succeeds and **`dotnet test` — the whole suite, unfiltered — is green**, on a
  machine with no GPU and no Godot. ⚠ **This sentence is a *milestone*'s gate and always was; a
  commit's is the assertion tier, and a runner's is neither**
  ([`adr/0121`](docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)).
  Not one word of it changed when the tiers landed, and the tiers are a filter you pass rather than
  a default that was moved. ***Narrowing what the gate names is an ADR and not a config edit***
  ([`plans/0032`](plans/0032-test-tiers.md)) — which is what `adr/0121` is, and what makes the
  narrowing legible rather than silent
- The invariants pass. **Sorted by frequency, never gated on build configuration** (`02 §10`) —
  `O(1)` at the write site per Tick, `O(n)` staggered, whole-world at end of run. The runs that
  surface these bugs are the million-Tick headless balance runs, and those are release builds
- The long-run test passes — 100k+ Ticks with **no collection and no magnitude** trending upward at
  steady state (`adr/0006`, and `adr/0003`'s extension of it to quantities)
- ~~There is something to *look at* showing the milestone doing its job~~ 🔴 **AMENDED 2026-08-26 —
  that clause was being satisfied by a column of hexadecimal.** ***A milestone is done when you have
  watched it happen and something surprised you*** ([`plans/0045`](plans/0045-amnesty.md))

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
- 🔴 **No shipped world can express *balance → unbalance → balance*, and this is the standing constraint
  on every demonstration Ruleset.** A premises Rule needing a **scarce** input has five places to draw
  on and **all five are shut**: `local` is circular (the chain must bottom out in a no-input Rule, which
  never fails); `pool` **is SHIPPED as of 2026-08-26 and REFUSES THE PREMISES SPECIFICALLY** — a premises Rule with a `pool` term throws at `RuleEngine.Buy`, because *a Building never holds money*; `global` is **money-family only** and
  [`adr/0113`](docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)
  says a Building never holds money, so a premises Rule **cannot buy anything**; `map` is **write-only
  by construction** — a Layer cell has no capacity to exceed, so a map term can never fail and no Rule
  ever waits on one; and a term reaching the **tenant's** Bins is **refused at the parse site**, because
  *a term crossing an ownership boundary is a trade, which is `pool`*. ***So a premises Rule chain today
  is always-succeeds or never-succeeds.*** `declining.toml` is one pole and `maintained.toml` is the
  other. 🔴 ⚠ **THIS BULLET WAS AMENDED 2026-08-26 AND THE TWO HALVES HAVE PARTED COMPANY.** It said
  *every one of the five is unbuilt rather than refused*, so none of it was evidence and the answer was
  **build scarcity**. `Scope.Pool` then shipped and split it. ✅ **A TENANT now has a middle** — its
  `pool` input fails on `Blocking.Supply` when the market is short, and `RuleEngine.Stop` arms the
  pressure clock on `Supply` and nothing else — so what the tenant threshold lacks is a **world**, not a
  mechanism. 🔴 **A PREMISES got a REFUSAL**, and under
  [`adr/0070`](docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) ***refused is the one
  classification that IS evidence***: with `pool` shut by decision rather than by absence, no premises
  Rule can fail on anything outside its own Building and the design says so on purpose. ***So the
  premises threshold is no longer waiting on a mechanism; it is waiting on an argument about what a
  premises Rule is for.*** ⚠ **It is why no decline number in
  [`plans/0002`](plans/0002-open-questions.md) §D1 can be ratified** — ***a threshold measured in a world
  where failure is certain, or impossible, is measuring a stopwatch and not a design*** ([`adr/0168`](docs/adr/0168-a-decline-threshold-is-a-duration-and-the-premises-and-the-tenant-get-one-each.md)).
- **Don't move a mechanism between Rule families for performance.** Bin Rules and Sweep Rules
  differ in observable behaviour, so moving one is a change to the city (`adr/0033`).
- **Prefer off-the-shelf infrastructure.** `adr/0018` — Citybound shipped ten bespoke libraries,
  three engine rewrites, and no game. A bespoke component requires a written exception naming the
  property no library provides.
