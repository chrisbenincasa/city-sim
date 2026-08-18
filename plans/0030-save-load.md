# 0030 — Save/load

`06` milestone **8**. The brief.

---

## Status

🟢 **IN FLIGHT. Scoped 2026-08-17, ungated; task 1 shipped 2026-08-17** — the rebuild audit and
`Disposition.Scratch`, **1,479 tests green and no baseline re-recorded**, because the milestone adds no
table and no column and `Scratch` was already outside the fold. Session **K** checked it and found nothing in front of
it: [`adr/0086`](../docs/adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md)
settles the format, [`adr/0087`](../docs/adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md)
settles the cadence, and `05 §6`'s threading policy — the one thing that looked like a gate — decides
nothing this milestone needs, because `adr/0087`'s opening sentence already decides the one threading
fact a save has.

**Nine tasks.** Three decisions were taken with the user in the room — *the magic number is deferred*
before scoping, and **the third `Disposition`** and **the synchronous write** on review of this brief —
and **three remain open**; all three are listed under *Open decisions this milestone owes* with the
task each blocks.

⚠ **Scoping it moved the milestone's weight from the write side to the load side, and the survey is
what moved it.** `06` records that the milestone *"got smaller"* when `adr/0086` deleted the authored
layout, and that is true of the **format**. It is not true of the **build**: `Rows` has no restore path
at all — the four allocator scalars are private with no accessors and no setters, and
`AllocateSlot`/`FreeSlot` are `private protected` (`Rows.cs:279-320`), so **nothing outside the class
hierarchy can place a row in a chosen slot**. Writing bytes out is a new abstract member on `Column`.
Getting them back in is a new capability the table layer does not have. ***A format decision that
removes an authored layout removes work from the writer and none from the reader***, and `06`'s
sentence is about the file.

⚠ **The milestone's named risk has a sharper instrument than the Factorio test, both halves are
already in the tree, and it needs no save format at all.** `World.RebuildDerived()`
(`World.cs:888`) exists and its own doc-comment says *"this is what a load will call, and what proves
the declaration was honest"*; `Rows.FoldAll` (`Rows.cs:204`) folds derived columns as well as saved
ones. Rebuild on a running world, fold everything, compare — and a derived column that does not
rebuild to the value it had fires **immediately**, with no save, no reload and no M further Ticks.
That is **task 1**, and it is deliberately first: it attacks the residual risk before a byte of format
exists, and it is the one task that can fail on the day it is written.

⚠ **Three of the survey's findings are live defects or near-defects and are recorded under *What
scoping found*.** `layer_cell.pollution_pass` is declared `Derived` and **nothing rebuilds it**;
`road_segment.fidelity` rebuilds to a constant `0`; and `BOR0901`'s own diagnostic message already
tells the developer that *"both the save serialiser and the State Hash are generated from that one
declaration"* (`Diagnostics.cs:181-183`) — a description of a build half of which does not exist,
which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
in the one place a reader is least able to check it.

---

## Why this milestone exists, in one paragraph

**There is no save.** `src/Borough.Formats/` holds the Input Log codec, the Ruleset loader, the hash
trace and the crash artifact, and nothing that touches world state; `src/Borough.Core/` contains no
`System.IO` reference at all. `05 §4`'s **invariant 6** — *run N, save, reload, run M; versus run
N+M* — is one of the two lints in that list still described as needing machinery that does not exist,
and it is the one whose machinery is a milestone rather than a research problem. Three things in the
tree are already written against its absence and say so: `CrashArtifact.From` is declared, is
documented as *"the checkpoint-shaped field"*, is **always zero**, and its reader **refuses** a
non-zero value it cannot honour (`CrashArtifact.cs:35-41`, `:82`, `:140`, `:195`); `World.RebuildDerived()`
is written for a load that does not exist; and `BOR0901` describes a save serialiser to every
developer who trips it. This milestone builds the thing all three are waiting for.

---

## The named risk

**A derived column that does not rebuild to the value it had** (`adr/0086`, `05 §7`).

The class `05 §7` used to name — *unsaved state: a cached value, a dirty flag, an accumulator, a
lazily-built index* — has been made **unrepresentable**. Declaring a field through `Rows.Saved`,
`Rows.Derived` or `Rows.SavedHandle` is what *allocates* it (`Rows.cs:101-128`), and `BOR0901` is a
build error on storage in a `[Table]` type that is neither. So a field is in the file by construction
or was never meant to be, and there is no third case.

What survives is the other half, and it is live. **A derived column is outside the State Hash** —
`Rows.Fold` filters on `Disposition.Saved` (`Rows.cs:184`) — so a wrong rebuild moves no hash directly
and is invisible to replay, to the golden baseline and to the save/reload comparison alike, until it
propagates into saved state some number of Ticks later. `EmploymentTests.cs:28-31` says exactly this
about the worker list, in the file that maintains it.

⚠ **The risk is concentrated and the survey says where.** There are **28 derived columns across 9 of
the 18 tables**, and three of them — `lots.frontage_slot`, `lots.frontage_offset`,
`lots.building_slot` — have **two producers by design**: a maintained write path and a rebuild path
(`LotTable.cs:208-210`, which calls this *"the established pattern here rather than a hazard"* and
says *"a test that the two agree is what stops them drifting"*). That test is task 1. **Today's
coverage is 2 of 28** — `StateHashTests.cs:92` corrupts `OccupantHead` and `MemberHead` and checks
those two lists, and nothing rebuilds and compares all derived storage.

---

## What the build already holds — surveyed 2026-08-17

Recorded because four of the nine tasks are smaller than they look and two are larger, and the
difference is in the survey rather than in the ADRs.

| | What exists | Where |
|---|---|---|
| **Table enumeration** | `World.Tables` → `ReadOnlySpan<Rows>`, declaration order = hash composition order | `World.cs:623`, order at `:170-192` |
| **Column enumeration** | `Rows.Columns` → `ReadOnlySpan<Column>`, all columns, declaration order | `Rows.cs:98` |
| **The fold** | `Rows.Fold` — four allocator scalars, then `Saved` columns, slots `[0, _slotCount)` | `Rows.cs:171-191` |
| **The derived rebuild** | `World.RebuildDerived()`, argument-free, ordering constraints stated in its own comments | `World.cs:888-1016` |
| **The everything-fold** | `Rows.FoldAll`, `internal`, derived columns included | `Rows.cs:204-219` |
| **Ruleset content hash** | `ContentHash.Of`, and `Simulation._inForce` carries it | `RulesetFile.cs:71`, `Simulation.cs:55` |
| **Per-row size** | `Rows.BytesPerRow(Touch)` — so the saved-set total is computable | `Rows.cs:156` |
| **The phase-7 boundary** | `Simulation.Commit(Ticks)`, serial, currently the staggered invariant tier only | `Simulation.cs:782-790` |

| | What does **not** exist | Consequence |
|---|---|---|
| **Disposition-filtered enumeration** | No `Columns(Disposition)`, no `SavedColumns`. Three call sites each write the `if` by hand (`Rows.cs:184`, `Report.cs:78-87`, `CondemnationTrailTests.cs:272-277`) | A fourth hand-written filter, or one shared accessor. Task 2 |
| **A type-erased byte accessor on `Column`** | `Fold` is the only type-erased traversal. `Column<T>.Span` is typed; `Raw` is `private protected` | A new abstract member on `Column`, a sibling to `Fold`. Task 2 |
| **Any restore path on `Rows`** | `_slotCount`, `_liveCount`, `_freeHead`, `_nextId`, `_capacity` are private with **no accessors** for the middle two and **no setters** for any; `AllocateSlot`/`FreeSlot` are `private protected` | The largest single item. Task 3 |
| **A format version** | Nothing versions the declaration set | Task 4 |
| **A generator version** | `GeneratorVersion` appears nowhere in `src/` or `tests/` | Task 4 |
| **Any save code** | `Borough.Formats` holds eight files, none touching world state | Tasks 5–6 |

**Scale.** 18 tables; **116 declared columns plus 54 intrinsic** (`id`, `generation`, `free_next`, three
per table) = ~170; **28 derived**, so the saved set is ~142 columns. ⚠ **Nobody has totalled the saved
set in bytes** — `adr/0087` says so in as many words and calls it *"milestone 8's to report, not this
ADR's to guess"*. S0a's **85.98 MiB** at 1M is the saved **and** derived total and **must not be quoted
as the save's size** ([`0012`](0012-corpus-audit.md) *Cause 5*).

---

## What was settled

Each entry carries its own date. D1 and D2 predate the tasks; D3 and D4 were taken on review of this
brief, with the user in the room, and each strikes an open decision below.

### D1 — the serialiser lives in `Borough.Core`, and it is derived rather than chosen

`CLAUDE.md` already says the save *"stays in `Core`"*, and the survey supplies a mechanical reason
where there had been a stylistic one. **Two `internal`s decide it**: `Randomness.Mix` is `internal` to
`Borough.Core` (`Randomness.cs:68`), and `Handle<T>.Index` and `.Generation` are `internal`
(`Handle.cs:47`, `:50`). A writer in `Borough.Formats` can reach neither, so it could not read a handle
column field-wise nor verify a hash. Putting the save there does not cost an argument — it costs
**moving the assembly boundary**, which is `05 §1`'s decision and not this milestone's.

⚠ **This does not put file I/O in `Core`.** `Borough.Core` contains **zero** `System.IO` references
and that is a property worth keeping. `Core` serialises to and from bytes; who writes those bytes to a
disk is **open decision 1**.

### D2 — the magic number is deferred, with the user in the room, 2026-08-17

[`0003`](0003-build-plan.md) §*The name* makes **this milestone** the revisit trigger for the project
name, on the ground that a rename is an hour today and stops being cheap at *"the save format's magic
number"*. The user's call is **defer the magic number**, so the trigger does not fire here and the
name is not reopened.

⚠ **Record what deferring costs, because it is not nothing.** A file with no self-identifying prefix
is diagnosed by its first failing field rather than at byte 0, so a truncated file, a file of another
type, and a file from a different product all produce the same class of complaint. That is acceptable
at a stage where the only reader of a save is the binary that wrote it, and it is the reason to write
the **format version** first in the header anyway (task 4) — a version number in a known position is a
weak magic number.

⚠ **And the name is already inside a constant.** `World.HashSeed` is `0x426F_726F_7567_6802UL` —
`"Borough"` plus a version byte (`World.cs:58`). Moving it moves every hash, which under
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
costs one command **while nobody is carrying a save**, and this milestone is what ends that. So the
trigger `0003` names is real and deferring the magic number does not disarm it; it narrows it.
**Revisit when the first save is carried across a build**, which is task 9's run and not before.

### D3 — `Disposition` gains a third value, and the precedent is in the tree rather than in the argument

**Decided 2026-08-17, with the user in the room. Strikes open decision 2 and unblocks task 1.**
`layer_cell.pollution_pass` is redeclared **`Disposition.Scratch`**, the audit skips `Scratch` **by
declaration**, and there is no exemption list anywhere.

⚠ **The argument that decides it is not `adr/0070`'s — it is that this exact decision has already been
taken in this codebase, on one column, with its reasoning written down.** `Reference`
(`Declaration.cs:115-151`) exists because `02 §10`'s every-handle-resolves walk is **driven by the
column declarations**, and one column had to be allowed to dangle. Its own remark states the choice and
the ground: the walk is declaration-driven *"for a stated reason — **a list of fields shares its blind
spot with the bug it exists to find**"*, so the exempt column *"has to say so where it is declared, in
the same place and the same spirit as `Disposition`"*. **The rebuild audit is a declaration-driven walk
with one exempt column**, which is the same shape in the same file, and an exemption list in the test is
the blind-spot arrangement that reasoning refused.

⚠ **And the standing objection is *refutable* rather than arguable, which under
[`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
settles how it is answered.** *One column is not a general mechanism* is a prediction about what happens
to a declaration axis introduced for a single instance, and this repository has run that experiment
once. `Declaration.cs:127` says **"only one column needs it"**. It is now **seven columns across five
tables** — `LegTable.cs:74`, `:79`; `TripTable.cs:67`, `:72`; `RouteHopTable.cs:69`;
`CondemnationTrailTable.cs:120`; `CitizenTable.cs:63` — reached within two milestones of the axis being
added. The only evidence available points the other way from the objection, and the sentence carrying
it is filed to [`0012`](0012-corpus-audit.md) as a Cause 4 sighting, because it is what a future author
reads while deciding whether to add an axis.

⚠ **A third value of `Disposition`, and not a second axis, because `pollution_pass` is mis-declared
today rather than a sub-case of a correct declaration.** `Disposition.Derived`'s contract is stated at
`Declaration.cs:32-37`: choosing it *"is a claim that the field is a pure function of saved state, and
the claim is checkable"*. `pollution_pass` is a function of nothing — its content between two diffusions
*"is meaningless by declaration"* (`LayerCellTable.cs:96-102`) — so it does not satisfy that claim at
all, and a modifier narrowing `Derived` would be narrowing a claim this column never made. The enum's
own question is *what is this column for*, and scratch is a third answer to it. **The welding argument
does not forbid this**: `Declaration.cs:8-20` welds the two dispositions so that *saved but not hashed*
— the state with no detector — is unrepresentable, and `Scratch` is neither saved nor hashed, so it
reopens nothing.

⚠ **The rule the third value carries is a positive obligation, and that is what makes it better than an
exemption rather than merely tidier.** ***A `Scratch` column must be written before it is read within
the phase that uses it, and nothing outside that phase may read it.*** That is not a name in a test —
it is an assertion, and a cheap one: **fill every `Scratch` column with garbage at a phase boundary and
assert the hash trace is unchanged**. So the audit stops saying *do not look at this one* and starts
saying *prove it cannot matter*, which closes the hazard the exemption would have left open — scratch
content becoming a hidden input to the simulation, which is the unhashed-state divergence
`Disposition` exists to close, arriving through the one column declared not to matter.

✅ **The ADR is written: [`adr/0110`](../docs/adr/0110-scratch-is-a-third-disposition-because-derived-is-a-claim-a-scratch-column-does-not-make.md), 2026-08-17, after task 1 landed.**
It amends `adr/0003`'s field rule, which is the project's oldest structural commitment. **Deliberately
written after the build rather than with this decision** — `adr/0069` is the standing counter-example
in the other direction, and writing it second means it states the rule the test enforces rather than
the rule the brief predicted. ⚠ **It carries one consequence this entry did not predict**: `Scratch` is
a way to make a failing rebuild audit go away, and what stops that is the garbage fill failing instead
— **a property of the tests rather than of the declaration**, recorded in the ADR as such rather than
left as an unstated assumption.

### D4 — the write is synchronous in this milestone, and the seam goes around the hash

**Decided 2026-08-17, with the user in the room. Strikes open decision 4 and amends task 6.**
Build the copy at the end of phase 7 and serialise from it **synchronously**. No thread in `Core`. The
structural requirement — *the serialiser never reads a live table* — is discharged by the **copy** and
not by the thread, and a thread in `Core` is `05 §6`'s subject, which is **session R**'s and which this
brief already forbids this milestone settling.

⚠ **The correction is where the seam is drawn, and task 6 had it in the wrong place.** The split is not
*copy* against *write*. [`adr/0087`](../docs/adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md):87-90
says so in a clause no task cited:

> **a hash is not free at this size and a copy is.** S0a measured one State Hash at **32.47 ms** over
> the same state — 2.08 Tick budgets, and roughly 3× the copy. If a save is to carry a verified hash,
> that hash is computed on the background thread from the copy, **never on the simulation thread as
> part of taking it**.

So the real seam is **copy** — bounded, blocking, ~10 ms, and in the Tick for ever — against **hash +
serialise + write**, which is unbounded and leaves the Tick the day the host takes it. The hash alone is
~3× the copy, so a seam drawn at *copy | write* moves the wrong ~10 ms and leaves the larger half
behind.

⚠ **Quote the 32.47 ms with what it was taken over.** It is S0a's whole-world hash at 1M, over the
saved **and** derived total of 85.98 MiB, and the save's hash is over the **saved set** — ~142 of ~170
columns, totalled by task 2 and by nobody yet. So it is an **upper bound** on the save's hash and not a
prediction of it ([`0012`](0012-corpus-audit.md) *Cause 5*).

**What this asks of the code:** one call from the end of `Simulation.Commit`, with the hash **inside**
it, so the future thread wraps exactly one function boundary and moves the right ~42 ms rather than the
wrong ~10.

⚠ **It also answers the objection recorded against this recommendation.** *An unused seam is an
untested one* is true of a dormant thread and false of a function boundary that every save crosses: the
seam is exercised on every autosave in task 9, and it is called synchronously rather than left unbuilt.

---

## Tasks

Ordered. **Task 1 needs no format and can fail on the day it is written**, which is why it is first.
Tasks 2 and 3 are the table layer; 4–6 are the file; 7–9 are the proof and the picture.

### Task 1 — the rebuild is honest, and no file is involved

Build the whole-world rebuild audit `World.RebuildDerived`'s own doc-comment describes: fold every
column of every table (`Rows.FoldAll`), call `RebuildDerived()`, fold again, assert equal. Run it over
a **stepped** world at several Ticks and over the golden fixture, not over a freshly built one —
`RoadGraph.cs:55-64` records that a derived structure reads as *absent* rather than *stale* before its
first rebuild, and absent is the state every guard is written against.

**Coverage goes from 2 of 28 derived columns to all of them.** Where it fails, the fix belongs to the
column's owner and not to this milestone; route it per
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
**on the day**, before working around it.

⚠ **It cannot pass as written, and that is the task's first deliverable rather than a defect in it.**
`layer_cell.pollution_pass` is declared `Derived` (`LayerCellTable.cs:51`) and **nothing rebuilds
it** — not `MapLayers.RebuildDerived` (`:510`, a one-liner over residency) and not `World.RebuildDerived`'s
clear block. Its own declaration says its content between two diffusions *"is meaningless by
declaration"* (`LayerCellTable.cs:96-102`), so the intended answer is *do not check this one* — which
means the audit needs an **exemption**, and an exemption needs a **rule**. ✅ **That rule is D3, taken
2026-08-17**: the column is redeclared **`Disposition.Scratch`**, the audit skips `Scratch` **by
declaration** rather than by list, and the skip is paid for by an assertion rather than by a name.

**So the task has a second deliverable and it is the one that carries the risk.** ***A `Scratch` column
must be written before it is read within the phase that uses it, and nothing outside that phase may
read it*** — asserted by **filling every `Scratch` column with garbage at a phase boundary and checking
the hash trace does not move**. `pollution_pass` passes that by inspection today (it is written and read
inside one `LayerDiffusion` call), which is exactly why the assertion is worth writing now: it is green
on the day it lands and it fails the day somebody reads scratch across a phase.

⚠ **A second column to look at rather than to fix, and the author got there first.**
`road_segment.fidelity` is rebuilt to a constant `0` (`RoadGraph.cs:343`) and that is its only write
anywhere in `src/` or `tests/` — `adr/0007`'s named hole. The comment above it says the zero is
written *"rather than left alone so that a rebuild is idempotent over a column somebody may later
start writing"* (`:341-342`), which is this task's whole premise reached from the other side. So it
passes the audit today **by intent**, and it stops passing the moment milestone **22** writes real
Stress into it, at which point the rebuild would silently zero a live value. Assert the constant now,
so the day it becomes false is a red test rather than a divergence.

#### ✅ Task 1 shipped 2026-08-17 — `DerivedRebuildAuditTests`, and four of its five findings are about the instrument

**Built:** `Disposition.Scratch` and `Rows.Scratch<T>()`; `layer_cell.pollution_pass` redeclared;
`Report.cs`'s saved/derived count made three-way; and `tests/Borough.Tests/Tables/DerivedRebuildAuditTests.cs`
— eight tests over five worlds. **Coverage went from 2 of 28 to all 32 derived columns**, one of which
is covered by a stronger dedicated test rather than by the audit loop. Nothing in `Borough.Core`'s
behaviour moved: `Rows.Fold` already filtered on `Saved`, so a third disposition was invisible to the
hash by construction, and all three golden baselines stood.

⚠ **1. The audit this brief specified could not have failed on the column that motivated it.** The
task text says *fold every column, call `RebuildDerived()`, fold again, assert equal* — and that form
catches a rebuild producing the **wrong** value while being **vacuous** against a column nothing
rebuilds **at all**: an absent rebuild leaves the storage untouched, so the two folds agree and the
audit reports a pass. `layer_cell.pollution_pass` was raised in this document as *declared `Derived`
and nothing rebuilds it*, and under the specified form it would have sailed through. The brief's
*"it cannot pass as written"* is therefore **exactly backwards** — it passes as written, and it is the
**corrupt-first** form (clear every derived column, rebuild, compare to the original) that fails.
***An audit that cannot fail on its own motivating case is an audit of something else***, and the
distinction is invisible from outside the code because both forms describe as *rebuild and compare*.

⚠ **2. The golden fixture — the world every committed baseline is recorded from — was carrying an
empty derived index, and the audit found it on its first run.** `GoldenFixtures.Build()` creates
Buildings through `BuildingTable.Create`, which is the raw table door; the **Cell residency index** is
maintained by `World.CreateBuilding` (`World.cs:1189-1191`), which the fixture never calls. So the
fixture held four Buildings and a `building.cell_next` of all zeroes, and a rebuild **populated** a
structure the live world had never filled. **No hash could ever have reported this** — `CellNext` is
`(derived AND rebuilt)` and `BuildingResidency`'s head, tail and count are plain arrays rather than
columns, so the structure is outside the fold twice over. Repaired in the fixture by adding the call
the world's own door makes; switching it to `World.CreateBuilding` would also create the kind's Bins
and arm its chain heads, which **is** saved state and would re-baseline three artefacts to fix a
derived index. ⚠ **The question underneath it is not answered here**: whether a raw table door should
be reachable at all without the world's index. Filed to [`0002`](0002-open-questions.md) §C per
[`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md) —
***route the finding before working around it***, and the fixture repair is the workaround.

⚠ **3. The survey's count was wrong and the machine now owns it.** Scoping said **28 derived columns
across 9 tables**; there were **33**, and there are **32** now that `pollution_pass` is `Scratch`. The
tables were right and the count was not. `Every_derived_column_is_exercised_by_some_world` asserts it
in both directions, so a new derived column that no fixture populates fails **on the day it is
declared** rather than silently shrinking the coverage — slice 10 task 11's finding, mechanised.
***A hand count of a declaration is a measurement written into prose***, which is `plans/0026` task 8's
lesson arriving on a plan rather than on a doc-comment.

⚠ **4. A connectivity label in a connected city is all zeroes, so the corruption cannot reach it — and
the file that fixes that exists for a different reading.** `RoadConnectivity.Label` numbers components
from 0, so a city in one piece labels every live node `0`: clearing `road_node.car_component` changes
nothing and rebuilding it changes nothing, and a zeroing corruption reports a pass it has not earned.
The world that exercises it is **`rulesets/severance.toml`**, which exists because *`minimal.toml`
cannot demonstrate Severance and that is measured*. ***A fixture built to make a reading possible is
the fixture that makes the test of that reading's storage possible***, which is `plans/0024`'s *a
measurement answers every question its numbers bear on* one level down. The test asserts
`FootComponents > 1` rather than assuming it, so the day that Ruleset stops severing the coverage
claim fails instead of quietly reverting.

⚠ **5. One column no world can exercise, named with the test that covers it instead of excused.**
`road_segment.fidelity` is written to a constant `0` everywhere in `src/` and `tests/` —
`adr/0007`'s named hole — so no fixture can make it non-zero until milestone **22**. Its own test
fills it with `0xAB` and asserts the rebuild returns it to zero, which is **strictly stronger** than
anything the audit loop can do to it, and which turns milestone 22's first real Stress write into a red
test rather than a load that silently drops the field. That is a **pointer**, not an exemption: the
covering test is named in the assertion, and if it is deleted the coverage assertion fails.

**What task 1 did *not* find:** no derived column in the build rebuilds to a wrong value. The two
producers `LotTable.cs:208-210` warned about — the maintained write path and the rebuild path for
`lots.frontage_slot`, `frontage_offset` and `building_slot` — **agree on every world tested**, which is
the first time that claim has been checked rather than asserted.

### Task 2 — a column's bytes, out and in, and the saved set totalled

Add the type-erased sibling to `Column.Fold`: a member that writes a column's slots `[0, slotCount)`
to a byte span and one that reads them back. `Column<T>` implements it over
`MemoryMarshal.AsBytes(_values.AsSpan(0, slotCount))`, which is what `Fold` already does
(`Column.cs:208-243`) — **little-endian by existing decision**, via `BinaryPrimitives`, and the save
inherits that rather than choosing it.

**`HandleColumn<T>` does not override it, and that is the interesting half.** `Fold` *is* overridden
there, to fold the target row's monotonic id rather than the handle (`Column.cs:322-339`), because the
hash must be blind to slot recycling. The **file must store the handle** — `{index, generation}` —
because a load has to restore the same slots, and `Column.cs:258-263` already says raw handles *"would
be correct for both uses the hash has today"*. So the bytes and the fold diverge in exactly one place,
by design, and `adr/0086` names it: ***a save round-trip must preserve the hash and need not preserve
the bytes***.

Add the disposition filter as one shared accessor rather than a fourth hand-written `if`. **It is
three-way after D3**, and `Scratch` sits with `Derived` on the not-written side — the file's column set
is the `Saved` set and nothing else, which is what task 7's structural test asserts.

**Deliverable beyond the code: the saved set in bytes**, per table and totalled, at the golden fixture
and at 1M, from `Rows.BytesPerRow`. `adr/0087` names this as owed and nobody has computed it.

#### ✅ Task 2 shipped 2026-08-17 — the byte accessor, the shared filter, and the size `adr/0087` said not to guess

**Built:** `Column.WriteBytes`/`ReadBytes`, the type-erased sibling to `Fold`, implemented once on
`Column<T>` and **deliberately not overridden by `HandleColumn<T>`** — so a handle reaches the file as
its stored `{index, generation}` while the hash keeps folding the target's monotonic id, which is
`adr/0086`'s ***a save round-trip must preserve the hash and need not preserve the bytes***.
`Rows.SavedColumns` is the one shared filter, computed at `Seal()`, and the three hand-written `if`s
are gone — `Rows.Fold`, `Report.cs` and `CondemnationTrailTests`. `Rows.SavedBytesPerRow` is the width
of one row in the file. `ColumnBytesTests`, four tests.

**The saved set, per table and totalled** — `adr/0087` names this as owed and forbids guessing it.
Σ(declared saved column bytes × slots), so a **width** figure and not a resident-memory measurement:

| World | Saved | All storage | Saved share |
|---|---|---|---|
| Golden fixture | **305,389 B** (0.29 MiB) | 339,237 B | 90.0% |
| Stepped, 4,000 Citizens, 512 Ticks | **595,101 B** (0.57 MiB) | 746,881 B | 79.7% |
| **1,000,000 Citizens, allocated capacity** | **137,706,463 B — 131.33 MiB** | 178,770,767 B (170.49 MiB) | **77.0%** |

`citizen` is **64 of its 84 bytes saved and 46% of the whole file at 1M** — 64,000,000 B — followed by
`rule_instance` at 25,650,000 B, `bin` at 22,500,000 B and `household` at 14,760,000 B.
`wheel_bucket` and `unplaced` are **100% saved**; `building` is the loosest at 25 of 61 bytes, because
nine of its columns are derived list heads and tails.

⚠ **Those four are written in bytes rather than abbreviated to megabytes, and the reason is a finding
about check 6 rather than a style choice.** `bin`'s saved figure abbreviates to *22.5 MB*, and `22.5`
is a **registered disqualifier alternate**: it is `adr/0094`'s larder quantity in **in-world minutes**,
whose required phrase is *pre-clock*, and the registry test duly failed this document. **To say it
explicitly, as that test asks: the megabyte figures in the table above have nothing to do with the
pre-clock larder — they are bytes of column storage, and the only thing they share with a quantity in
in-world minutes is three characters.** ***A check for a caveat-free quotation cannot tell a quotation
from a coincidence of magnitude, and the shorter the figure the more coincidences there are.*** Filed
to [`0012`](0012-corpus-audit.md), which also records that the sentence you are reading tripped the
check a second time before it named the phrase.

⚠ **The number lands on `adr/0087`'s copy estimate, and it lands at the wrong end of it.** That ADR
prices the copy at **~10 ms**, citing `adr/0037`'s *8–15 ms for 80–150 MB*. The saved set at 1M is
**131 MiB**, which is the **top** of that band rather than the middle — so the copy is nearer **13–15 ms**
than 10, and under **D4** the copy is the half that **stays in the Tick for ever**. It is still one
copy per autosave against a 15.6 ms Tick, so `adr/0087`'s *0.008% amortised* conclusion is untouched
at an autosave per Day; what moves is the **single-occurrence hitch**, which is that ADR's own first
revisit trigger — *"the copy becoming unaffordable at a single occurrence"* — and it is now **~1 Tick
budget rather than two-thirds of one**. Reported here rather than acted on: task 9 measures the copy
and this is a prediction, not a measurement.

⚠ **It also does not reconcile with S0a's 85.98 MiB, and the two must not be subtracted.** S0a
measured **resident tables** in a populated 1M run in 2026-08; this is **declared width × allocated
capacity** on a `new World(1_000_000)`. Different instruments, and the corpus's standing rule is to
quote the sentence rather than the digits (`plans/0012` *Cause 5*). What the gap is **consistent with**
is that five milestones have added columns since — the Movement tables, the commute columns on
`citizen`, the worker list on `building` — and ***nothing re-measures a footprint when a column is
added***. Filed as a question rather than a correction: the honest reading is that **the 85.98 MiB
figure is old**, not that either number is wrong.

⚠ **The endianness discipline the file inherits is weaker than the comment claiming it.**
`Column.FoldBytes`'s remark says a State Hash whose value depends on the host's byte order *"is a hash
that reports a divergence on a port"*, and it assembles `ulong`s with `BinaryPrimitives` accordingly.
That fixes the **combination** step and not the **layout**: the bytes being combined come from
`MemoryMarshal.AsBytes` over a struct whose field order in memory is the machine's, so **a big-endian
host would already produce a different State Hash for the same city**, before any save existed. The
save copies the same representation, so it **inherits that exposure exactly and adds none** — which is
the reason to copy rather than invent a second byte order here: one representation, one place to fix.
***A byte order fixed at the point of combination is not a byte order fixed at the point of storage.***
Filed to [`0012`](0012-corpus-audit.md); not repaired, because per-field swapping over an arbitrary
`unmanaged` struct is not expressible without knowing its layout, and every platform .NET supports is
little-endian.

**What task 2 did not build:** the allocator restore. `ReadBytes` places bytes at slots
`[0, slotCount)` and changes no scalar, so the round-trip test restores the hash **because the four
allocator scalars were never disturbed**. That is task 3, and it is still the item with no precedent
in the tree.

### Task 3 — the allocator restore path, slot-exact

`Rows` gains the ability to be restored: `_slotCount`, `_liveCount`, `_freeHead` and `_nextId` set
from a file, and a column's bytes placed at a chosen slot range without going through `AllocateSlot`.
This is the largest item in the milestone and the one with no precedent in the tree.

**Slot-exactness is the whole constraint** (`adr/0086`). The free list and the id counter are *saved
state*, not bookkeeping to recompute: a loader that rebuilds the free list by scanning for dead rows
produces a different `_freeHead` and a different hash. Freed slots hold zeroes — `FreeSlot` clears
every column at the slot (`Rows.cs:330-333`) — so the residue is reproducible rather than arbitrary,
which is what makes a byte-exact round trip achievable at all.

⚠ **Two things a restore must not do.** It must not normalise a stale handle in a
`Reference.Severable` column to `default` — `HandleColumn.IsDangling` exempts those (`Column.cs:310-320`)
and **the stale handle is the state** (`Declaration.cs:145-150`). And it must not leave `_back`
holding old content on a `Buffering.TwoCopies` table: `Column.Clear` zeroes **both** halves precisely
because *"a `_back` left holding an old row would resurrect it on the next swap"* (`Column.cs:174-185`).

⚠ **Exactly one table declares `TwoCopies` today — `LayerCellTable` (`:44`)** — and the corpus's
standing sentence that *two* tables are double-buffered counts a Lane-dynamics table that does not
exist. `Fold` reads `_values` only (`Column.cs:208-209`), and `_back` is meaningful only inside
`MapLayers.Diffuse` between `PrepareBack` and `SwapBuffers` (`MapLayers.cs:593-607`), which is
synchronous within one phase. **So a save taken at a phase boundary may ignore `_back` entirely**, and
`adr/0087`'s *"both double-buffered tables have settled"* is right about the boundary and out by one
about the count. Filed to [`0012`](0012-corpus-audit.md).

⚠ **A `Scratch` column comes back zeroed and the unbroken run's holds residue, and that divergence is
intended** (D3). It is outside the fold, so no hash sees it, and task 1's garbage-fill assertion is the
standing proof that it cannot matter — **the restore relies on that assertion rather than on an argument
made here**, which is the whole reason task 1 is first.

### Task 4 — the header, and the three version numbers

Write the header `adr/0086` specifies. **Format version first**, then the rest.

| Number | Status today | This task |
|---|---|---|
| **Format version** — the declaration set | Does not exist | Create it. ⚠ **It is not `World.HashSeed`'s version byte** — see *What this milestone must not do* |
| **Ruleset content hash** — the content | Exists: `ContentHash.Of` (`RulesetFile.cs:71`), carried by `Simulation._inForce` | Write it; `05 §7`'s two load policies already say what a mismatch means |
| **Generator version** — the terrain for a seed | **Does not exist anywhere** | Create it. `adr/0021` **pins** it: no migration, because a moved landscape has no repair |

⚠ **The header must also carry `WorldKey`, and this is not obvious from either ADR.** `World.Key`
(`World.cs:349`) is not a column and folds nothing, and `World.cs:325-329` states that
`RebuildDerived` *"takes no arguments and must not start taking them"* and cannot reproduce the
commute roster without it. **So a save that omits the world key cannot rebuild a derived structure**,
which is the milestone's named risk arriving through the header rather than through a column.

**`_phase` and `_inForce` are the residue of [`0002`](0002-open-questions.md)'s open question and both
dissolve** — see *Open decisions*, item 3.

### Task 5 — the writer and the reader

`World` → bytes and bytes → `World`, in `Borough.Core`, over tasks 2–4. Load ends by calling
`World.RebuildDerived()`, which task 1 has by then made trustworthy.

The reader refuses rather than degrades where `05 §7` says to: an unaccounted Ruleset mismatch in
replay, a generator version mismatch always, a format version **newer** than the binary always. A
format version older runs the migration chain — which has **no entries yet**, and shipping the chain
with none is correct: the version number is what makes the first migration writable later.

### Task 6 — the copy at the end of phase 7

`adr/0087`'s structural requirement, and it is the one clause of that ADR that constrains the code
rather than pricing it:

> **The serialiser may never read a live table.** A writer that walks `World` directly is a defect
> even when it produces a correct file today, because it is correct only while nothing is parallel
> around it.

So: a copy of the saved columns taken at the end of `Simulation.Commit` (`Simulation.cs:782`), and the
serialiser runs over the copy. Phase 7 is serial, is where hashes are already written, and is the one
moment the double-buffered table has settled — so the file and the State Hash describe the same
instant, which is what lets the header's hash be a statement about the bytes beneath it.

✅ **The write is synchronous in this milestone and the thread is the host's** (D4, 2026-08-17). What
that decides is not *whether to defer* but **where the seam goes**: `adr/0087`:87-90 puts the file's
hash on the **background** side — one State Hash is ~3× the copy — so the boundary is **copy** against
**hash + serialise + write**, and not copy against write.

Build it as **one call from the end of `Simulation.Commit`, with the hash inside it**. Synchronous
today, and the day the host takes the write it wraps exactly that call and moves the right ~42 ms rather
than the wrong ~10. Taking the copy is still the part that has to be right first, because it is the part
that stops being correctable once callers exist.

### Task 7 — the Factorio test, and the structural test `adr/0086` owes

```
run N → save → reload → run M   → hash A
run N+M                         → hash B
assert A == B
```

In CI, over the golden fixture and over a Ruleset that actually churns. **It measures the rebuild,
not the write** — task 1 measures the rebuild directly and this measures it in the only place where a
wrong derived value has had time to reach saved state.

Beside it, the structural test `adr/0086` names in its consequences and asks not to be discovered
later as a gap: **the file's column set equals the hash's `Saved` set**, table by table, in order.
Cheap, and it is what stops the two answers to one question drifting.

### Task 8 — something to look at

`--save PATH` and `--load PATH` on the headless runner, which is where `05 §7`'s *replay from save*
becomes a thing somebody can do rather than a paragraph. The picture is a **round trip that agrees**:
save at a Tick, reload, run on, print the hash trace beside the unbroken run's.

⚠ **`Options.cs` is this milestone's one file-level collision with the parallel milestone 6 session**,
which is adding `--evidence` to the same switch. It is a merge, not a conflict of substance.

### Task 9 — the long acceptance run

`adr/0006`'s run with a save in it: 100,000+ Ticks with periodic autosaves, no collection and no
magnitude trending, and a reload at the end that reproduces the unbroken run's hash. Report the
saved-set size against task 2's computed total, and the save's cost as **two numbers, not one** — the
**copy** against `adr/0087`'s ~10 ms prediction, and **hash + serialise** separately. ⚠ **They go to
opposite sides of D4's seam**, so a single combined figure would read as a refutation of that ADR's
prediction when it is not one, and would tell nobody what the eventual thread is worth.

⚠ **This is where the deferred magic number's revisit trigger fires** — the first save carried across
a build — and where `CrashArtifact.From` could stop being zero. Neither is in scope; both are named in
*What this milestone must not do* so the day they are reached is a decision rather than a drift.

---

## What this milestone must not do

- **Do not compact.** Dropping dead slots is the obvious size win and it changes `_slotCount`, every
  live row's slot and the residue between. Under `05 §4` that is a **design change**, however it was
  motivated (`adr/0086`).
- **Do not reuse `World.HashSeed`'s version byte as the format version.** They version different
  things and the code says which: *"A NEW table joining `_tables` does not bump this... What this byte
  signs is the same city hashing differently"* (`World.cs:47-57`). A new table changes the file
  absolutely and that byte deliberately does not move. ***Two numbers versioning one thing is
  [`0012`](0012-corpus-audit.md) Cause 1 waiting to happen; one number versioning two is worse.***
- **Do not put the serialiser in `Borough.Formats`.** Not a style preference — `Randomness.Mix` and
  `Handle<T>.Index`/`.Generation` are `internal` to `Borough.Core` and the writer needs both (D1).
- **Do not put `System.IO` in `Borough.Core`.** It has none today.
- **Do not normalise a stale `Reference.Severable` handle on load.** The stale handle is the state.
- **Do not walk `World` from the serialiser.** `adr/0087`, task 6.
- **Do not settle threading policy.** `05 §6` is **session R**'s, and it is now load-bearing on
  [`0013`](0013-tick-budget.md)'s whole verdict. This milestone may put a write on a thread; it may
  not decide who owns `step()`.
- **Do not fill `CrashArtifact.From`.** It is the right consumer and the wrong milestone: a checkpoint
  in a crash artifact needs an autosave cadence, a retention policy and a bundle format, and `05 §8`
  owns all three. **Task 9 is what makes it possible; a later row is what does it.** The refusal at
  `CrashArtifact.cs:195` is correct in the meantime and must stay.
- **Do not fix the numbering citations in `src/` as part of a task.** See *What scoping found*, F2 —
  it is a sweep with an owner, not a line in a save commit.

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- `dotnet build` and `dotnet test` green with no GPU and no Godot.
- **`05 §4` invariant 6 is live** — the save/reload equivalence test runs in CI. It is one of two
  lints in that list that has never had machinery.
- **Every derived column is covered by the rebuild audit**, or declared `Disposition.Scratch` and
  covered by the garbage-fill assertion instead (task 1, D3). **No column is exempted by a list.**
- **The saved set's size is reported**, per table and totalled, at the fixture and at 1M — `adr/0087`
  names it as owed and forbids guessing it.
- **The file's column set equals the hash's `Saved` set**, asserted structurally (`adr/0086`).
- **Three version numbers in the header**, each refusing or migrating per its own row in task 4.
- The long run passes with autosaves in it, and reloads to the unbroken run's hash.
- **Something to look at**: `--save` / `--load` and a hash trace across a round trip.

**The risk this milestone retires:** *a derived column that does not rebuild to the value it had*, and
it retires it for **every milestone below** — fifteen rows each of which adds derived columns, which is
`06`'s stated reason for this row being third rather than sixteenth.

---

## Open decisions this milestone owes, before the task that needs them

**Three of the five remain.** Items 2 and 4 were settled on review of this brief and are struck in
place, per the corpus rule that a closed decision keeps its number and its reasoning.

### 1. Where the I/O boundary sits — **blocks task 5**

`Borough.Core` has zero `System.IO`. `Core` must own serialisation (D1) and cannot own the file.
**Recommendation: `Core` reads and writes a byte buffer, and the shell does the I/O**, which is
`adr/0039`'s shape for the Input Log read one level down. The question is whether the buffer is a
`Stream`, a `Span<byte>`, or a callback per table — and the answer interacts with task 6, because the
background write wants to stream a copy it does not own.

⚠ **D4 narrowed this and did not close it, recorded here because a decision's write to a neighbouring
open question is what *Cause 2* is.** There is **no background write in this milestone**, so the buffer
has one caller — a synchronous serialiser invoked from the end of `Simulation.Commit` — and nothing
this milestone builds can distinguish the three shapes. **That makes the choice free today and
load-bearing later**: it must be the shape a thread can take, which is a constraint on the answer and
not an answer. Choose it against D4's seam — the buffer is on the **hash + serialise + write** side, so
whatever owns it must be movable off the simulation thread whole.

### ~~2. Is `Derived` one class or two?~~ — ✅ **SETTLED 2026-08-17 by D3. A third `Disposition`.**

**The recommendation below was to take it to the user, and it was taken to the user, who settled it in
favour of the third value.** What decided it is not in the text below: **`Reference` is this same
decision already taken in this codebase, on one column, with its ground written down at
`Declaration.cs:120-135`** — and its *"only one column needs it"* is now seven columns across five
tables, which turns the standing objection into a prediction this repository has already tested and
refuted. The full reasoning is D3; the original text stands unedited beneath.

*Original, 2026-08-17 — **blocked task 1**:*

`layer_cell.pollution_pass` is `Derived` and nothing rebuilds it, on purpose: it is a **scratch
intermediate between two diffusions**, not a structure recoverable from saved state. Every other one
of the 28 is the second kind. The audit needs to tell them apart, and the options are a written
exemption list in the test, a third `Disposition`, or a `Touch`-style second axis.

⚠ **A third `Disposition` is the expensive answer and probably the right shape**, because an
exemption list in a test is a fact stored where no future column author will look — which is
`adr/0064`'s *a guard with no test* running backwards. **And the column's own declaration argues for
it**: `LayerCellTable.cs:96-102` says the field is a column rather than a bare array *"so that it is
declared once like everything else — `BOR0901` would reject the array, and it is right to: scratch
that escapes the declaration is scratch nobody audits."* So the author deliberately pushed scratch
**into** the declaration and the declaration had no word for it. ***A disposition set that forces a
third kind of field to pick one of two is a declaration with a hole, and the hole shows up as an
exemption in somebody else's test.***

⚠ **Against, and it is the standing rule**: `adr/0070` says an absence is not evidence, and **one
column is not a general mechanism**. Deciding a third `Disposition` on a single instance is how a
taxonomy grows a member nobody needed. **Recommend taking it to the user rather than settling it in a
task**, because the two arguments are genuinely balanced and the cost lands on every future column
author either way.

### 3. `_phase` and `_inForce` — **blocks task 4. Recommendation: both dissolve**

[`0002`](0002-open-questions.md) carries these as *"§7's unargued format half"*, left open when
[`adr/0058`](../docs/adr/0058-the-tick-is-state-so-the-world-holds-it-and-the-hash-folds-it.md) moved
the Tick to the World and left its two neighbours on the `Simulation` (`Simulation.cs:54-55`).
**Neither needs a decision and both need writing down:**

- **`_phase`** — the copy is taken at the *end of phase 7* by `adr/0087`, so `_phase` is `Commit` at
  every save. A value with one possible value is not state. ***The cadence decision answered the
  format question, in another ADR, and nothing recomputed it.***
- **`_inForce`** — it *is* the Ruleset content hash, and `adr/0086` already puts that in the header.
  Saving it as a field would be a second copy of a header entry.

⚠ **The rest of the `Simulation`'s private state is not covered by that and wants a walk**:
`_opened`, `_reloads` and `_degradation` (`Simulation.cs:56-58`) sit in the same position and no ADR
mentions them. **The provenance trail is safe** — `RulesetTrailTable` is a real saved table — but a
counter beside it is not the trail.

### ~~4. Is the write actually on a background thread in this milestone?~~ — ✅ **SETTLED 2026-08-17 by D4. Synchronous, and the seam moved.**

**The recommendation below was accepted and one thing in it was wrong.** The recommendation is right
that the copy discharges the structural requirement and the thread is `05 §6`'s; what it does not say is
**where the seam goes**, and task 6 had it at *copy | write* when `adr/0087`:87-90 puts the file's hash
on the background side at ~3× the copy's cost. The seam is **copy** against **hash + serialise + write**.
That clause was in the ADR and cited by no task. See D4; the original text stands unedited beneath.

*Original, 2026-08-17 — **blocked task 6**:*

`adr/0087` decides it *is* async and prices it. What it does not decide is whether the milestone that
builds the copy also builds the thread. **Recommendation: build the copy and serialise from it,
synchronously, and leave the thread to the host.** The structural requirement — *the serialiser never
reads a live table* — is discharged by the copy and not by the thread, and a thread in `Core` is
`05 §6`'s subject, which is session R's. ⚠ **Against it**: an unused seam is an untested one, and
`adr/0087` is explicit that the *unbounded* half is the write.

### 5. What is the generator version derived from? — **blocks task 4**

Nothing produces one. It must move when the generator's output for a given seed moves, and the
failure it prevents — the terrain moving under a city — is `adr/0021`'s. A hand-maintained constant
is the obvious answer and has the obvious defect: **it is bumped by whoever remembers**. A hash over
the generator's own inputs is checkable and does not catch a change to the generator's *code*, which
is the actual failure. ⚠ **Both candidates are worse than the format version's story and that
asymmetry is the decision to make honestly**, rather than shipping a number that reads as a guarantee.

---

## What scoping found

Four things, recorded here because three are about the corpus's own instruments rather than about
saving.

### F1 — `BOR0901` tells every developer the save serialiser exists

The diagnostic's message reads *"both the save serialiser and the State Hash are generated from that
one declaration"* (`Diagnostics.cs:181-183`), and its extended description reasons from a
*"save/reload test [that] passes because the field is saved"*. **The State Hash half is true and the
save half has never existed.** This is
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
landing on a surface that ADR's inventory does not name — not an ADR, a plan, a doc-comment or a test
suite's coverage, but **a compiler diagnostic**, which is the most persuasive description of the build
there is: it is current, it is emitted by the build itself, and it is read at the exact moment the
reader is being taught the rule and is least able to check it.

***A diagnostic is a description of the build that arrives with the build's authority.*** This
milestone makes the sentence true rather than correcting it, which is the good outcome and is
available exactly once.

### F2 — "milestone 8" now means two different milestones inside `src/`

Session K's renumber mapped old **8 → 7** (parking) and old **10 → 8** (Save/load). Both old numbers
are live in the source tree:

| Says | Means | Sites |
|---|---|---|
| *"milestone 10"* | **Save/load** — this milestone | `CrashArtifact.cs:36`, `:38`, `:82`, `:140`, `:195`; `LotTable.cs:14` |
| *"milestone 8"* | **Parking** — now milestone 7 | `TripEngine.cs:198`, `:395`; `World.cs:1082`, `:1135`; `AccessPointTests.cs:53`; `StatisticalTravelTimeTests.cs:243`; `CarOwnershipTests.cs:151` |
| *"milestone 8"* | **Save/load** — correct, new numbering | `CondemnationTrailTests.cs:264` |

So a reader grepping `milestone 8` for this milestone's obligations finds **parking six times out of
seven**, and the one correct hit was written yesterday by the parallel session. `06:390` warns about
exactly this — ***a retired-numbering table makes an old citation resolve and cannot stop a new one
being translated as though it were old*** — and the new form is that **the collision is now inside the
build**, where `06`'s table cannot reach it and no document-to-document check can see it.

⚠ **Not repaired here, and the reason is the warning itself**: the mapping is applied by reading each
citation's **subject**, never its digits, so this is a sweep by somebody who opens all thirteen sites.
Filed to [`0012`](0012-corpus-audit.md).

### F3 — the milestone's own risk had a cheaper instrument than the test it is named for

`World.RebuildDerived()` and `Rows.FoldAll` have both been in the tree for the life of the derived
declaration, and between them they measure *a derived column that does not rebuild to the value it
had* **directly, immediately, and with no save format**. `05 §7` and `adr/0086` both frame the risk
through the Factorio test, which measures it **indirectly** — a wrong rebuild has to propagate into
saved state over M Ticks before a hash comparison can see it — and which needs the entire milestone
built first.

Both instruments are worth having and they are not the same instrument. ***A risk named after the
test that would catch it gets scheduled behind that test***, and this one sat behind a save format for
the life of the project while the two halves that measure it directly sat in `World.cs` and `Rows.cs`.
The existing coverage is **2 of 28 derived columns**.

### F4 — a survey's numbers, for the record

18 tables; 116 declared columns + 54 intrinsic ≈ 170; **28 derived across 9 tables**; saved set ≈ 142
columns. `DerivedHandle` has **zero call sites** in the repository. `RouteCache` and `TravelTimeMatrix`
are instantiated **only in tests and in the S2 spike** — neither is a field on `World` or `Simulation`
— so a save does not have to reconstruct either, and both self-heal by version comparison
(`TravelTimeMatrix.cs:96-108`) or by the Saved per-Segment `Epoch` (`RouteCache.cs:377`) if they ever
become world state.

---

## Where this sits

| | |
|---|---|
| **Milestone** | `06` **8** — Save/load |
| **Gate** | None. Session K, 2026-08-16 |
| **Decides** | `adr/0086` (format), `adr/0087` (cadence). Both 🟢 |
| **Retires** | *A derived column that does not rebuild to the value it had*, for every milestone below |
| **Closes** | `05 §4` invariant 6 — the Factorio test |
| **Moves the State Hash?** | **No.** It adds no table and no column. The golden baselines do not move |
