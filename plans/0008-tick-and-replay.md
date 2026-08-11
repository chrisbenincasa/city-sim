# 0008 — Slice 5: the Tick, the Input Log and replay

> Slice 5 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 1**. Governed by
> [`02 §1.1`](../docs/02-simulation-model.md), [`02 §8`](../docs/02-simulation-model.md),
> [`02 §10`](../docs/02-simulation-model.md),
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md),
> [`adr/0002`](../docs/adr/0002-simulation-is-an-engine-agnostic-library.md),
> [`05 §7`](../docs/05-technical-architecture.md), [`05 §8`](../docs/05-technical-architecture.md).

**`step(inputs)`, the eight-phase ordering, a `u64` Tick counter, the Input Log, and replay.** After
this slice the project has the tool every later debugging session is downstream of: a run that can be
reproduced exactly, and a hash trace that names the Tick a bug entered.

**Risk retired.** Determinism, which is close to impossible to retrofit. Every later tool — replay,
bisection, save/reload equivalence, crash forensics, the golden-hash regression — is a consumer of
this slice and none of them can be built before it. `adr/0037` made it more load-bearing rather than
less: with the full-world double buffer deleted, **replay reconstructs any later state** is now the
entire mechanism of crash forensics.

**Also retired, quietly:** the phase ordering itself. *Ordering is not an implementation detail — it
is the determinism contract.* Writing the eight phases as named, ordered, mostly-empty methods now
means every later mechanism arrives into a slot that already exists, rather than being appended
wherever it fit.

---

## Progress

**Tasks 1–6 are done.** Tick this table as tasks land; [`0003`](0003-build-plan.md)'s ledger records
the slice as a whole and should not be updated until the slice closes.

| Task | State | Where it landed |
|---|---|---|
| 1. `step(inputs)` and the phase skeleton | **done** | `Simulation.cs`, `TickPhase.cs`, `TickInput.cs` |
| 2. The command model and the Input Log | **done** | `Core/Input/` — `Command`, `InputLog`, `InputLogBuilder`, `WorldConfiguration`; the codec landed in task 5 |
| 3. Replay | **done** | `Core/Input/Replay.cs` |
| 4. The golden-hash baseline | **done** | `tests/Borough.Tests/Golden/` — the fixtures, two baselines, and the procedure |
| 5. The headless runner | **done** | `Borough.Formats/` — the codec, the trace format, the Ruleset hash; `Borough.Headless/` — `Options`, `Session`, `RulesetCheck`, `Report` |
| 6. The invariant tiers | **done** | `Core/Invariants/` — `InvariantRegistry`, `Invariant`, `Violation`, `WorldInvariants` |
| 7. The long-run test | **part done** — the instrument, not the assertion | `Core/Instruments/` — `Census`, `Metric`, `Series`; `Borough.Headless/CensusReport.cs`; `--census` |
| 8. The crash artifact | **done** | `Borough.Formats/CrashArtifact.cs`; `Borough.Headless` — the catch in `Session`, `--crash`, `RulesetCheck.InForce` |

### Decided while building tasks 1–3

**The Input Log's on-disk encoding is line-oriented text**, which the *Decisions owed* section below
left open. Weighed against binary records and against binary-with-a-dump-tool. The deciding
arguments were that the log is *attached* to a bug report far more often than it is diffed, so
**legible without tooling** beats *diffable*; that the crash artifact is emitted at the moment
tooling is least trustworthy; and that binary's usual advantage is size, which the task 2 sizing
check — *a ten-hour session is kilobytes* — deletes. Binary's real win, no locale exposure, is
answered by `InvariantGlobalization` and explicit invariant parsing. Sketch:

```
borough-log 1
seed 0x0B07000000000001
citizens 64
ruleset 0x0000000000000000
--
0 zone 0 0 1
1 zone 1 1 1
```

**Both of the codec's open questions are now settled, in [`adr/0039`](../docs/adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md):**

- **It lives in `Borough.Formats`, a fifth runtime project.** `Borough.Core` has no filesystem
  (`02 §1`) and does not own strings a human reads (`adr/0002`); a copy in each shell was rejected
  because a log written by `Borough.Godot` must replay in `Borough.Headless`, so two implementations
  sit behind the one property the format exists to guarantee. `05 §1` and `CLAUDE.md` now state five
  projects, with `Borough.Analysers` a sixth excluded on the test that it does not ship. **Task 5
  creates the project along with the codec** — an empty project ahead of its first file is churn.
- **The extension is `.borough`.** `.gitignore` ignores `*.inputlog` (line 494) and `*.log` (line
  100), both inherited from the .NET template, and the golden baseline is a *committed* log.

**Also found: `.gitignore` line 35 ignores `[Ll]og/`.** The first home for these types was
`src/Borough.Core/Log/`, which git would have silently refused to track. Named here because the
failure is invisible — the build succeeds, the tests pass, and the files are simply not in the
commit.

### Decided while building task 4

**The baseline is two artefacts, not one.** A committed session trace was the task's whole text, and
it turns out to cover **one table in four**: the only verb applied before slice 7 is `Zone`, and a
Zone command creates a Lot. Buildings, Households and Citizens are reachable only through the cold
API, so three tables' saved columns would have sat under no committed hash at all while the baseline
claimed to be the project's regression net. A second file — one hash over a hand-built city, with its
row counts beside it — closes that. **It is expected to be deleted**, not maintained: once the player
has verbs that build a city, the session absorbs its job.

**The session is a code fixture until task 5, deliberately.** The codec is `Borough.Formats`' and
task 5's (`adr/0039`); a reader written in the test project to load a text log today would have been
the second implementation that ADR exists to prevent. The order pays off — **task 5 inherits a free
and rather strong codec test**: the committed `.borough` must parse to a log that reproduces the
already-committed trace, which is a round trip through the real artefact rather than through a
fixture asserting against itself.

**There is no self-regenerating switch, and that is the mechanism rather than an omission.** Both
tests print the exact file they would commit and stop. An `--update-baselines` flag or an environment
variable is one CI misconfiguration away from a baseline that approves every change it sees, and a
baseline that approves everything has stopped being one. The procedure lives in `README.md` beside
the files, because it is read at the moment a build has just gone red.

**A sampled trace names a window, not a Tick.** The failure message says which cadence-wide window
the change entered and tells you to re-run at `hash-every 1` to name the Tick — claiming the exact
Tick would be a precision the sampling does not have, and the first person to trust it loses an
afternoon in the wrong Tick.

### Decided while building task 5

**`--strict` is inverted: refusal is the default and `--force-ruleset` is the escape.** The flag list
above lists `--strict` as an opt-in, which implies a lenient default — and `05 §7` denies there is
one, mapping the two policies onto the two shells explicitly: *`Borough.Godot` is play mode and
lenient, `Borough.Headless` is replay mode and strict.* An opt-in flag would have made the corpus's
strict runner lenient by default and the flag a lie. There is still a real question on the other side
of the refusal — *how far does this Ruleset change move the city* — so the escape exists; what it must
not do is produce numbers that look comparable, so the trace it writes carries a `hash-broken` line,
in the spirit of `05 §7`'s permanent mark on a save loaded across an unaccounted mismatch.

**A log naming a Ruleset nobody supplied is refused too, and this is the case worth getting right.**
It is not a mismatch — it is a match nothing can confirm, which from the far side is the same thing:
the run either was or was not against the right Rules and the runner cannot say which. `05 §7`'s word
is *unaccounted*, and this is the shape of it that arrives first. Nothing triggers it before slice 8,
because until a Ruleset has content every log in the repository names the empty one.

**`series(metric, window)` is deferred to task 7, not skipped.** `05 §2` puts it on the cold API for
panels *and this runner*, and task 5's text says the runner dumps aggregate series. But task 7 needs a
per-N-Tick census of every variable-length structure's length, which is the same mechanism — and there
is nothing to aggregate yet beyond four row counts. A cold API shaped by one caller is usually shaped
wrong; building it now and rebuilding it in task 7 is churn. The runner prints the hash trace, and
task 7 gives `series` a second caller and something to say.

**The trace format is `Borough.Formats`', and the golden baseline is now the runner's output.** The
runner's `--out` and the committed baseline are the same artefact read by the same code, so
re-baselining the session is a command whose diff is reviewed rather than a transcription between two
shapes. `world-hash.txt` keeps the copy-from-the-failure-message route because it is a hand-built
world rather than a session the runner can play — an asymmetry that disappears when slice 7 lets the
player raise a Building and the fixture is deleted.

**The Ruleset content hash needed a home before the Ruleset does.** `05 §7` says a Ruleset is
identified by a content hash and never says how. The split: `Borough.Core.Determinism.ContentHash`
folds bytes through the project's one normative mix — reaching for a cryptographic digest would have
been the reflex and wrong twice, since this defends against accident rather than adversary and a
second hash function is a second thing that can drift — and `Borough.Formats.RulesetFile` decides what
counts as the same content. **Line endings are normalised and nothing else is.** A Ruleset is text in
a repository cloned on Windows and on Linux; without normalising, the same file carries two hashes by
machine and `--strict` refuses to replay a log against the very Ruleset it was recorded against, a
failure invisible in a diff that would be blamed on the log. Normalising further means parsing, which
is slice 8's.

**The slice-4 table report is kept as a mode rather than superseded.** A replayed session contains a
handful of Lots and three empty tables until slice 7, so a report printed at the end of a run would
show nothing. Two modes, dispatched on whether a session flag was given; the no-argument behaviour
`CLAUDE.md` documents is unchanged.

**The option parser is hand-rolled, and `adr/0018` is satisfied rather than waived.** Nine flags, no
subcommands, no completion — below the threshold the ADR aims at, and a dependency is worth least in
the one project whose job is to prove it builds with nothing installed. If the surface grows
subcommands, take the library.

### Decided while building task 6

**The per-Tick tier is not a registry, and the other two are.** `02 §10` puts the cheap checks *at
the write site*, which is what keeps them `O(changed)` and what makes the resulting failure point at
the code that caused it rather than at whatever ran next. So there is nothing to register in that
tier: a write site calls `Require` and the registry either throws or records. Building all three as
identical lists of swept delegates would have been tidier and would have quietly moved the per-Tick
checks off the write site, which is the one property that tier has.

**A violation throws by default, and `Collect` is the switch.** Throwing is what composes with task
8: the artifact catches at the Tick boundary and emits the log plus the panic Tick, so you replay to
the Tick before and single-step in. Collecting exists for the balance run, which is millions of Ticks
long and would rather finish and report than die on the first bad Household — a different question,
*what is wrong with this city* rather than *where did this go wrong*, whose answers after the first
violation are worth less in the way compiler errors after the first are. A runtime switch rather than
a build configuration, for `02 §10`'s own reason.

**The end-of-run tier runs on every headless run, not behind a flag.** It is `O(world)` once, so it
costs nothing against a run of any length, and a check that is off by default is a check that is off.
The trace is written before it runs, so a violation does not cost the numbers the run was for.

**Where a corpus invariant splits across two tiers, neither half is complete alone, and that is the
tiering working rather than a compromise.** *No Citizen in two places* becomes an `O(changed)` check
at the write site — complete within one Household, blind across two — plus a whole-world count at the
end of the run, which is complete and unaffordable per Tick. The write-site half fires on a genuinely
realistic bug: a row freed without being unlinked stays in its owner's list, the next allocation
recycles that slot, and the recycled row is inserted into a list it is already in.

**The handle walk is driven by the columns, not by a list of fields.** Every `Column` answers whether
it dangles; non-handle columns answer false. A walk naming the fields it knew about would share its
blind spot with the bug it exists to find — the same argument the per-field declaration makes about
the State Hash, one level up.

**`Slices`, the stagger period, is a property with a default and lives outside the Ruleset.** The
no-`const` rule aims at numbers a designer would want to change; this is not one. `05 §4`'s test
settles it: invariants only read, so no setting of it can move the State Hash, so it is an
optimisation rather than a design change. There is a test asserting the trace is identical at
`Slices = 1` and `Slices = 997`, because that argument is worth checking rather than asserting.

**What is *not* checked, and why, is most of `02 §10`'s list.** Goods conserved needs Bins, no Trip
without a Fate needs Trips, parking occupancy conserved needs parking — all slice 7 or later. Money
conservation needs a treasury and transactions; what is checkable today is `adr/0003`'s overflow
detector, which is the visible end of the same bug. The plan predicted this: *most tiers will be
nearly empty after this slice and that is correct.*

### Measured, after the fact: what the tiers cost on a world with rows in it

Task 6 shipped with its cost unmeasured, because a replayed session holds a handful of Lots before
slice 7 and timing that measures dispatch and nothing else. A BenchmarkDotNet job in `Borough.Tests`
(`Benchmarks/InvariantCostBenchmarks`) answers it against a constructed city at S4 task 2's ratios.

| | 1,000 | 10,000 | 100,000 |
|---|---|---|---|
| **Staggered, one slice** — the per-Tick cost | 166 ns | 1.0 µs | 9.1 µs |
| **Staggered, full sweep** — every row once, over `Slices` Ticks | 11.4 µs | 68.9 µs | 655 µs |
| **End of run** — the whole-world walks, once | 44 µs | 462 µs | 4.84 ms |
| *State Hash, for scale* | 31.9 µs | 314 µs | 3.15 ms |

**The staggered tier is affordable and it is not close.** At 100,000 Citizens one slice costs
**0.06% of the 15.6 ms Tick budget**; the growth is linear, so 1M extrapolates to ~91 µs, or **0.6%
of a Tick**. The comparison that settles it is the last row: checking *every row in the city once*
costs about **a fifth of one State Hash**, which is a cost the project already takes on a cadence
and has never argued about.

**`adr/0033`'s claim now has a number behind it.** *Unaffordable per Tick and trivial at the end of a
headless run* — the end-of-run walk at 100k is 4.84 ms once, against 655 µs × 64 Ticks if it had been
swept, or 4.84 ms **per Tick** if it had been done the way `02 §10` originally wrote it. The tiering
is worth three orders of magnitude at this population.

**One finding the benchmark was not looking for: the end-of-run walk allocates, and at scale it
allocates on the Large Object Heap.** `Count` takes an `int[]` per element table to count list
appearances — 544 KB at 100k Citizens, extrapolating to ~5.4 MB at 1M, and the diagnoser shows Gen2
collections at the top population. It is paid once per run and the runner writes the trace *before*
calling the tier, so it cannot perturb the numbers a run was for. Recorded rather than fixed: the
fix is a scratch buffer on the registry, and it is worth doing when S0 puts a real 1M city in front
of it rather than on an extrapolation. **Revisit trigger:** the long-run test or S0 showing a GC
pause past `adr/0036`'s 15.6 ms p99.9.

**The benchmark is deliberately not a session, and not a runner flag.** The obvious way to get a
populated world is to let the runner seed one, and that is precisely what `Replay` forbids — *the
moment world state can arrive from somewhere the log does not describe, the log stops being a
complete account of a session and a divergence stops being attributable.* Nothing in the benchmark
touches a log, a session or a hash trace, so nothing in it can put a number in front of somebody
that looks reproducible and is not. `SyntheticCity` is small and marked deletable: **S0** is the
corpus's designated synthetic city and this must not quietly become it.

**Benchmarks are never assertions.** Nothing here fails a build. A timing threshold in CI goes red on
a busy machine and is then disabled, which leaves neither a benchmark nor a test.

### Decided while building task 7

**Task 7 shipped its instrument and deliberately did not ship its assertion.** The task as specified
is *100,000+ Ticks in CI asserting no collection and no magnitude trends upward at steady state*, and
that assertion is close to vacuous against the world as it stands. Every Tick phase except Input is
empty; the only structure that changes size during a run is `Lots`, one per `Zone` command, monotonic
and with no sink. So a trend assertion over a replayed session either asserts the flatness of
something already flat, or flags the player zoning as a leak. An empty world and a seeded static
world fail to exercise it *equally*, which is the tell.

An assertion that cannot fail is worse than no assertion, because it reads as covered. The census
hook and `series(metric, window)` are worth building on their own terms and were; **the assertion is
recorded as owed against slice 7**, which is what puts churn in the world.

**The Census is the instrument `adr/0006` has been owed since it was written.** Its constraint is
about a run rather than a moment, so nothing inspecting one Tick can see it violated. Three counters
per table rather than one, because they fail differently: live rows are the city's size and prove
nothing on their own, **slots** rise only when a create finds the free list empty — so *slots
climbing while live is flat* is the leak with the population held constant — and capacity is the one
that costs memory. Full entry in `CONTEXT.md`.

**The ring is finite because the alternative would be the defect it detects.** A census appending a
reading per cadence for the length of a run is a collection growing with elapsed game time —
`adr/0006` exactly, in the instrument written to catch it. The oldest reading is overwritten and that
overwriting is the sink. The cost is that a window can outrun the history, which `Series.Complete`
**reports** rather than hides: a silently shortened window would let a reader conclude *flat over
100,000 Ticks* from the last 1,000, a claim the data does not support and nothing downstream could
catch.

**It belongs to the run, not to the World** — the opposite of where the invariant registry sits, and
for the reason that decided that one. A registry's claims are about *this world* and hold for one
built by hand in a test; a census is a record of a run, and a world has no history until something
steps it. Putting it in simulation state would also have made it something the State Hash and the
save each needed an answer for, and an instrument that changes the hash is an instrument that changes
the city. There is a test that a census does not move the trace.

**`series(metric, window)` takes an id, never a name.** `adr/0002` gives the core ids and gives the
shell every string a human reads, and a metric keyed by string would have put the panel's vocabulary
inside the simulation. `Metric` is `(table index, counter)`, and the table index is declaration order
— the same order the State Hash folds tables in, so a metric means the same thing to a trace as it
does to a hash.

**There is no separate collection metric because the intrusive-list pattern removed the need for
one.** Every variable-length structure in the core is an intrusive index list whose nodes are rows,
so the total length of every list over a table *is* that table's live row count. A list missing a
live row is not a magnitude problem but a correctness one, and belongs to the invariant tiers. That
uniformity is what slice 4 bought, collected here.

**The census prints to standard output and never into the trace.** The hash trace is a committed
artefact reviewed as a diff; a census folded into it would change the file on every run whose sizes
moved, which is every interesting run, and would make the golden baseline unreviewable. Two reports,
because two questions: *did this change*, and *what is it doing*. `--census` is opt-in and implies a
run, since the table report is one moment and has no history to take a series over.

**What the report shows today is the vacuity, stated honestly.** A 100,000-Tick replay of the golden
session prints eleven Lots, flat, and three tables of zeroes. That is the correct output and it is
the argument for deferring the assertion, in a form somebody can run.

### Decided while building task 8

**The artifact wraps the log verbatim, and the runner accepts one wherever it accepts a log.** These
two are the same decision. `05 §8`'s claim is that a panic becomes *a reproduction* rather than a
corpse — and a reproduction nobody can run is a corpse with extra steps. So everything after the
header's separator is exactly what `InputLogCodec` writes, which means cutting the file yields a
replayable `.borough` with no tooling; and the runner sniffs the magic line rather than taking a
flag, because the person reproducing a crash should not have to already know something the file can
tell them. The loop closes: an artifact fed back to `--log` panics at the same Tick and emits an
identical artifact.

**The Tick an artifact names is the Tick that failed, and that rests on one line of `Simulation`.**
`Step` advances its counter *after* the phases, so a phase that throws leaves `simulation.Tick`
naming the failure rather than the one after it. That is why task 3 split `Trace` out of `Run` —
recorded then as *the crash artifact needs to name the Tick a panic landed on, and it cannot do that
from a method that owns the loop and returns only on success.* There is now a test pinning it, because
an artifact off by one Tick sends its reader somewhere nothing is wrong yet and the mistake reads as
the bug moving.

**`from` is the checkpoint-shaped field and it is zero for the whole of Phase 1** — the plan asked for
exactly this, so milestone 10 fills a field in rather than replacing a mechanism. The part worth
stating is what a *reader* does with a non-zero one: it **refuses**. A build with no way to load a
checkpoint that replayed from Tick zero instead would rebuild a different city and blame the
difference on the crash, which is the failure mode this slice exists to abolish.

**The catch is deliberately broad, and out-of-memory is the one exception.** A handler whose whole job
is turning any panic into a reproduction cannot be a list of the failures somebody already thought of
— those are the ones least in need of it. `OutOfMemoryException` is excluded because writing a file
needs memory and failing there would bury the original.

**The trace is not written when a run panics.** A partial trace is indistinguishable from a complete
one once it is a file, and would be diffed against a full run with the missing tail read as a
divergence — the `hash-broken` reasoning again. Nothing is lost: replaying the artifact regenerates
the trace up to the panic, which is what the artifact is for.

**The artifact records the Ruleset in force rather than the one the log names**, which needed a new
`RulesetCheck.InForce`. A run forced across a mismatch is running Rules the log does not describe, and
a reproduction attempted later against the log's Ruleset would diverge for a reason the crash had
nothing to do with.

**There is no flag to turn it off.** `--crash` names the destination and never whether. The mechanism
exists so a panic in an unattended run becomes a file, and one that produced nothing because nobody
passed a flag would be failing at the only moment it is needed.

**`connect` is what makes this testable today.** The format encodes all four verbs and the simulation
applies only `Zone`, so a log carrying a `connect` panics on demand without anything being broken to
arrange it — which is why task 5's *encode every verb, apply one* is worth more than it looked.

---

## Gate

**Cleared, session eight.** `adr/0003` is closed: the hash function is normative with literal
constants, `purpose_tag` is specified, the save-and-hash field declaration is settled, and `02 §10`'s
invariant tiers are settled. `02 §8`'s rule list was grilled alongside it.

## Prerequisites

Slices 2, 3 and 4. The hash needs the field declaration; `draw()` needs the purpose-tag enum; the
phases need something to write to.

---

## Tasks

### 1. `step(inputs)` and the phase skeleton

Signature, verbatim from `02 §1`: `step(inputs) -> ()`. No wall-clock, no camera, no renderer, no
filesystem. A Tick is an unsigned integer counter and the host decides when to advance it.

Write all eight phases as named, ordered methods, most of them empty:

| Phase | Name | Concurrency | What it will do |
|---|---|---|---|
| 0 | **Input** | serial | Apply player commands from the Input Log |
| 1 | **Wake** | serial | Drain the Event Wheel bucket for this Tick |
| 2 | **Decide** | parallel, **read-only** | Evaluate Rules and Needs against the Past. Emits *intents*, never a mutation |
| 3 | **Settle** | serial, sorted | Apply intents in shuffle order. Re-check atomicity. Losers take their fallback |
| 4 | **Move** | parallel | Lanes advance Vehicles; Statistical trips check arrival |
| 5 | **Layers** | parallel | Map Layer diffusion for whatever is scheduled |
| 6 | **Growth** | serial | Zone Rules sample Lots; Buildings with accumulated failure decline |
| 7 | **Commit** | serial | Schedule next events, re-evaluate Stress, emit the State Hash if due |

Two things to encode rather than comment:

- **Phase 2 writes nothing, and that is load-bearing** (`adr/0037`). It is what permits every entity
  table to be single-buffered. A future decision to parallelise Decide must not also make it
  mutating. Assert it: in debug, Phase 2 runs against a write-guarded view.
- The concurrency column is **permission, not implementation**. `02 §1.1` states what may be
  parallel; `05 §6` states what is. Phase 1 runs everything serially and the two documents do not
  currently say that they differ, which is a noted contradiction — write the code so the distinction
  is visible.

### 2. The command model and the Input Log

The log is exactly:

> `(world seed, configuration, Ruleset content hash, player commands per Tick)`

— and nothing more. **There is no camera input to record**, because `adr/0007` derives Fidelity from
Stress rather than from the camera, and `adr/0002` removed the simulation from the camera in the
other direction.

- Commands as unmanaged structs, tagged by kind, applied in Phase 0.
- Ruleset content hash carried per Tick, with a reload appearing as a **transition carrying both
  hashes** rather than as an event — a replay needs the Rules' *content*, not the news that they
  changed. Slice 8 fills this in; stub the field now so the format does not change later.
- Size expectation, worth holding as a design check: a ten-hour session is **kilobytes**, because a
  player issues a handful of commands a minute. **A bug report is an attachment.**

### 3. Replay

`run(log)` → a hash sequence. This is CI lint 5: **two runs of the same Input Log produce identical
State Hash sequences.**

The property that makes it work is already built: randomness is `draw(seed, entity, tick, purpose)`,
counter-based, so results are independent of evaluation order — which is also what will let Phase 2
be parallelised later with no coordination and bit-identical output.

### 4. The golden-hash baseline, and its re-baselining procedure

A stored Input Log with a recorded hash sequence, committed.

**The point is not that the hash never moves — it is that it never moves without someone saying so.**
Write the procedure down beside the baseline: what a deliberate re-baseline looks like, who records
why, and the standing example — swapping softmax for Gumbel-max is *hash-breaking but
distributionally neutral*, which is safe to do deliberately with a re-baseline and unsafe to do
silently.

### 5. The headless runner

`Borough.Headless` is the primary interface for the whole of Phase 1 and most of Phase 2, and it is
*the project most likely to be dismissed as a nicety and the one that decides whether this simulation
ever gets balanced*.

```
--seed N            --ruleset PATH        --log PATH
--ticks N           --hash-every N        --out PATH
--strict            (replay mode: refuse an unaccounted Ruleset mismatch)
```

- Dumps a hash trace and aggregate series. `series(metric, window)` is on the cold API explicitly for
  panels **and this runner**.
- **Replay mode is strict and play mode is lenient** (`05 §7`). `Borough.Headless` is the strict one:
  a different Ruleset is a different simulation and the hash will diverge — that is arithmetic, not a
  bug. A replay whose Ruleset does not match **refuses to run** rather than diverging silently.
- Must build and run with Godot uninstalled. `dotnet build src/Borough.Headless` is the continuous
  check that the boundary still holds.

### 6. The invariant tiers

`02 §10`, and the shape matters more than the contents. **Invariants sort by frequency, never by
build configuration** — the earlier debug-build gate was backwards, because the runs that surface
these bugs are headless balance runs, millions of Ticks long, in **release**.

| Tier | When | What goes here |
|---|---|---|
| **Per Tick** | every build | Only `O(1)` and `O(changed)` — no Bin negative or over capacity **at the write site**, parking occupancy conserved, no Trip without a Fate |
| **Staggered** | every build, one slice per Tick | The `O(n)` sweeps, amortised the same way Sweep Rules are: Goods conserved, no Citizen in two places, every Household's home exists and lists them as an occupant |
| **End of run** | headless suite | The whole-world walks: **money conserved** — the overflow detector `adr/0003` relies on — every cross-table handle valid, and *no Rule asleep with all its inputs satisfiable* — **the last of these was still unbuilt when this slice closed and stayed so until `adr/0063`; it is `WaiterIsBlockedByTheBinItNames`, and it found a live defect in the golden baseline the day it was registered** |

Build the three registries and wire whatever exists. Most tiers will be nearly empty after this slice
and that is correct; what matters is that the next mechanism has a tier to register into and cannot
default into a debug-only check.

### 7. The long-run test

100,000+ Ticks in CI, in seconds, asserting the city is still coherent — and asserting that **no
collection and no magnitude trends upward once the city reaches steady state**.

`adr/0006` for collections, `adr/0003`'s extension for quantities. Not merely finite: *not trending
upward*. It belongs in CI from here rather than being added once something has already leaked — the
failure is invisible at design time, takes hours of play to manifest, and the corpus has already
written it twice on paper.

This needs a **collection-size census hook**: a per-Tick or per-N-Tick sample of every
variable-length structure's length, which the intrusive-list pattern from slice 4 makes uniform.

> **Built, minus the assertion.** The Census, `series(metric, window)` and the runner's `--census`
> report shipped; the trend assertion did not, because nothing in the world churns yet and an
> assertion that cannot fail reads as covering a property it cannot see. See *Decided while building
> task 7* above, and the owed item on the board. The assertion is slice 7's to switch on.

### 8. The crash artifact

Catch at the **Tick boundary** and emit the last checkpoint plus the Input Log since it, with the
Ruleset content hash and the panic Tick — `(checkpoint @ 4096, log 4096..5000)` for a panic at Tick
5000. You then replay to Tick 4999 and single-step into the failure under a debugger, rather than
dumping a corpse.

There are no checkpoints until milestone 10, so **for Phase 1 the artifact is the seed plus the whole
log**, which is equivalent and smaller. Write it in the checkpoint-shaped form so milestone 10 fills
in a field rather than replacing a mechanism.

`adr/0037` made this *stronger* rather than weaker, and with no new machinery — it is determinism plus
the Input Log, which the project already has.

---

## Acceptance

- `dotnet run --project src/Borough.Headless -- --ticks 100000 --hash-every 1000` completes **in
  seconds** and prints a hash trace.
- Two runs of the same log produce **byte-identical** traces.
- A test that mutates one hashed field mid-run and asserts the trace diverges at **exactly** the
  expected Tick — this is the bisection property, and it should be proven rather than assumed.
- A run with a mismatched Ruleset hash **refuses to start** in `--strict`.
- ~~The long-run test passes with no collection and no magnitude trending upward at steady state.~~
  **Deferred to slice 7, deliberately.** What this slice can honestly deliver is the instrument: a
  Census on the trace cadence, `series(metric, window)`, and a runner report. The assertion needs a
  world with churn in it to have a steady state to be at, and this one has none.
- The three invariant tiers run in **release** builds.
- `dotnet build src/Borough.Headless` succeeds on a machine with no GPU and no Godot.
- A panic writes a **crash artifact naming the Tick it landed on**, and feeding that artifact back to
  the runner reproduces the same panic at the same Tick. The loop closing is the acceptance criterion;
  a file that merely records a crash is the dump `05 §8` rejected.
- **Something to look at:** the hash trace itself, diffable against a previous run. This is the first
  artefact in the project that can catch a bug nobody was looking for.

## Decisions owed by this slice

- **Which thread runs `step()`** — `05 §6` never says, and `adr/0037` made it consequential: with one
  live state, what the renderer and the saver read is a design decision rather than a free
  consequence. **Phase 1 does not need the answer** (it is single-threaded and headless) but the
  runner's shape should not foreclose it. The recommendation on the table is *the simulation owns a
  thread, and `threads=1` means it runs on the caller's thread rather than meaning no sim thread* —
  because `02 §1.2` forbids Tick skipping, so a saturated sim on the main thread takes the camera
  down with it.
- **Whether hot-path results need generation tagging** — `adr/0002`'s rules table leaves the row
  explicitly open and it is a threading question, not a hot/cold one. Not needed in Phase 1.
- ~~**The Input Log's on-disk encoding.**~~ **Settled.** Line-oriented text in a `.borough` file,
  carrying `borough-log 1` on its first line; the reasoning is in *Decided while building tasks 1–3*
  above and the project it lives in is `adr/0039`'s. It is append-only because the reader builds
  through `InputLogBuilder`, so a hand-edited log whose Ticks run backwards is refused by the same
  code that refuses it in memory.

## What this slice deliberately does not do

No Bins, no Rules, no Event Wheel — slices 7 through 9, all gated. No save format; the crash artifact
uses the log rather than a checkpoint precisely so that it does not need one. **No thread-count
equivalence test** — Phase 1 is single-threaded, and a test asserting equivalence across no
parallelism passes vacuously and then keeps passing after the parallelism arrives. It is written when
the first parallel phase lands.
