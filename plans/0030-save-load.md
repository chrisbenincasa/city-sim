# 0030 — Save/load

`06` milestone **8**. The brief.

---

## Status

✅ **DONE. Scoped 2026-08-17, ungated; tasks 1–6 shipped 2026-08-17, tasks 7–9 on 2026-08-18, and
task 10 the same day** — the rebuild audit and `Disposition.Scratch`, the column storage accessor,
`Rows.Restore`, the header, the writer and reader, the copy, **the Factorio test**, **`--save`/`--load`**,
**the long acceptance run**, and **the save's own State Hash**. ✅ **All five open decisions are closed**,
each by the task it blocked, and ✅ **both of the two questions that were not on the list are closed
with the user in the room, 2026-08-18.**

⚠ **The first was *does a save carry a verified State Hash?*, and it closed by the claim underneath it
turning out to be false.** Task 6 recorded that a hash could not be folded from the copy, because
`HandleColumn.Fold` folds the target row's monotonic id and a handle's bytes do not contain one. True of
**a column** and not of **the file**: `Rows` declares `id` and `generation` as `Saved` columns, so the id
is in another table's block of the same copy. ***A value absent from a column's own bytes can still be
present in the copy.*** So the hash is folded from the copy at **no cost to the simulation thread**,
`adr/0087`'s clause is honoured rather than overturned, and `05 §4` **invariant 6 is now a property of
every load**. That is **task 10** and [`adr/0112`](../docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md);
`adr/0087` and `adr/0111` are both amended.

⚠ **The second was *is `Borough` the name?*, and it closed by splitting the question.** `Borough` is the
**codename** and stays; a **public name** is deferred and is a different decision with different filters.
Task 4's magic number is a codename in a file header, which is what magic numbers usually are, so
`plans/0003 §The name`'s trigger is **discharged rather than met** — see that section.

**1,549 tests green and no baseline re-recorded**, because the milestone adds no table and no column,
`Scratch` was already outside the fold, the fold produces the same numbers from the same inputs, and a
header is not part of the city. ⚠ **`05 §4` invariant 6 — *run N, save, reload,
run M* — has machinery for the first time in the project's life**, and it is green over seven cases and
two Rulesets. Session **K** checked it and found nothing in front of
it: [`adr/0086`](../docs/adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md)
settles the format, [`adr/0087`](../docs/adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md)
settles the cadence, and `05 §6`'s threading policy — the one thing that looked like a gate — decides
nothing this milestone needs, because `adr/0087`'s opening sentence already decides the one threading
fact a save has.

**Ten tasks** — nine planned, and task 10 added on 2026-08-18 when one of the two leftover questions
turned out to want a mechanism rather than a decision. Three decisions were taken with the user in the room — *the magic number is deferred*
before scoping, and **the third `Disposition`** and **the synchronous write** on review of this brief —
and **the other two by task 5 and task 4** — the I/O boundary (D7), and the `Simulation`'s residual
fields plus the generator version (D5, D6). All five are struck in place under *Open decisions this
milestone owes*, with the task each blocked.

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

#### ⚠ AMENDED 2026-08-18 by task 9 — **this decision was reversed by task 4 and nobody said so**

**`SaveHeader` writes eight bytes of `borosave` at offset 0.** D2's decision was *no magic number*, and
its cost paragraph says so in the clearest possible terms — *"a file with no self-identifying prefix is
diagnosed by its first failing field rather than at byte 0"* — and then draws the consequence, *"it is
the reason to write the **format version** first in the header anyway (task 4)"*. **The shipped header
writes the version at offset 8, behind a magic number.** So the build does not do what the decision
says, and four documents (this brief's own task 4 table, `05 §7`, the board, `06`) describe the header
that was built rather than the one that was decided.

⚠ **The finding is not that the magic is wrong — it is that `plans/0003 §The name`'s trigger has fired
and nothing announced it.** That section makes the project name *"a working title"* whose revisit
trigger is *"the **save format's magic number**… from then on a rename either breaks every existing
save or requires a migration written for no reason but vanity"*, and names **milestone 8** as the
trigger. D2 disarmed it by deferring the magic; task 4 rearmed it by writing one, and the sentence that
would have connected the two was in a decision nobody re-read while building the task it governed.
***A decision is reversed by the build far more quietly than by an argument***, and this corpus's
mechanical checks are all document-to-document, so none of them can see a header.

**What is done about it: nothing, deliberately, and it is on the user's desk.** A project name is not a
decision a session takes. Both directions are still one line and one re-record — the format is
unreleased and `adr/0100` says the window is open *while nobody is carrying a save* — so the reversible
act is to leave the shipped header alone, record that the trigger is live, and hand it over. See
`HANDOFF-milestone-8.md`. ⚠ **Task 9's run does not close the window either**: it writes and reads
saves inside one process against one build, so *the first save carried across a build* has still not
happened.

⚠ **And the magic earned its place on the merits, which is what makes the silence expensive rather
than lucky.** `SaveFileTests.A_file_that_is_not_a_save_is_refused_before_any_table_is_read` corrupts
byte 0 and gets *"not a borough save"*; without the magic that file would have been diagnosed as an
unknown format version, which is the weaker sentence D2's own cost paragraph predicted. **A decision
overturned by a good reason is still a decision overturned**, and the reason belongs beside it.

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

### D5 — the five fields on the `Simulation` all dissolve, and three had answered in their own remarks

**Settled 2026-08-17 by task 4**, closing open decision 3. `plans/0002` carried `_phase` and `_inForce`
as *"§7's unargued format half"*; the brief recommended both dissolve and asked for a walk of `_opened`,
`_reloads` and `_degradation`. All five dissolve:

| Field | Why it is not in the save |
|---|---|
| `_phase` | `adr/0087` takes the copy at the end of phase 7, so it is `Commit` at every save. **A value with one possible value is not state** |
| `_inForce` | It **is** the Ruleset content hash, which is a header field. Saving it too is a second copy of a header entry |
| `_opened` | `true` in every save, on `_phase`'s argument: a world that has never stepped has never reached phase 7 |
| `_reloads` | *"since this **Simulation** started"* — per-run, by its own summary line |
| `_degradation` | *"the trail is world state because `05 §7` puts it in the save; **this is a Simulation's**, because a warning is about the run"* |

⚠ **The finding is where the answers were.** Three of the five were settled in the doc-comment on the
field, and the question had been open in `plans/0002` for two milestones — `adr/0093`'s reading half
(*a sentence about the build tells you which symbol to read*) paying out in the direction it is usually
quoted against.

⚠ **Dissolving out of the save is not dissolving out of the loader, and this is the half that would have
shipped as a defect.** A fresh `Simulation` has `_opened = false`, so the first step after a load takes
its Ruleset hash through the **opening** branch rather than the **transition** one: no provenance trail
entry, no degradation report, `Reloads` still 0. **A cross-Ruleset load — which `05 §7` explicitly
permits in play — would therefore be unrecorded**, which is the exact bug the trail exists to catch, at
the exact moment it was designed for. The loader must supply both fields; it is task 5's, and it is
written down here rather than discovered there.

⚠ **And one live defect falls out of it, filed rather than fixed** ([`0012`](0012-corpus-audit.md)).
`Session.cs:187-198` prints *"{Reloads} reload(s), of which {recorded} cost the city something"* — a
**per-session** count against a **per-world** total, in one sentence. It reads correctly today only
because no session has ever begun from a save. ***Two quantities agree for as long as the mechanism that
separates them does not exist***, and this milestone is that mechanism.

### D7 — the I/O boundary is two interfaces, and the save is streamed rather than assembled

**Settled 2026-08-17 by task 5**, closing open decision 1, **and the deciding argument came from
outside the three options the brief listed**. The question was `Stream` against `Span<byte>` against a
callback per table; what settled it is that ***a whole-buffer shape doubles the peak at the one moment
memory is already highest***. `adr/0087` spends a **copy of the world** at save time by decision, so a
staged file puts a second body of the same order beside it — **131.33 MiB at 1,000,000 Citizens**.

**`Stream` was out for the stated reason** — `Borough.Core` holds no `System.IO`, which is `adr/0039`'s
shape one level down: `Core` decides what the bytes are and never where they land. So:

```
public interface ISaveSink   { void Write(ReadOnlySpan<byte> bytes); }
public interface ISaveSource { void Read(Span<byte> into); }
```

⚠ **The payoff is that nothing proportional to the save is allocated at either end, and it costs
nothing to get.** A column's slots are contiguous, so `Column.StorageBytes` hands the sink a window
onto storage that already exists and hands the source the destination to fill. **The largest run of
bytes in play at any instant is the largest single column — `citizen.id` at 7.63 MiB at 1M, 5.81% of the
file — and the only buffers are 52 bytes of header and 20 of table scalars, both stack-allocated.**
***A format with no authored layout can be streamed, because there is nothing to lay out*** — which is
`adr/0086` paying for itself a third time, after the hash having no coverage hole and the file having
none either.

⚠ **A round trip cannot assert this and the test says so.** A writer that assembled the file and handed
it over in one call passes every equality test there is, so `MemorySave` records the **largest single
hand-over** and `SaveFileTests` bounds it by the widest column — a fact about the declaration rather
than about the writer.

⚠ **It also settles the shape D4 said must be *movable off the simulation thread whole***: what moves
is `SaveFile.Write` plus the sink, and the sink is where any real buffering would live, so the decision
about buffered I/O belongs to the shell that owns the file rather than to `Core`.

### D6 — there is no generator version, because a version number guards the artefact that re-derives

**Settled 2026-08-17 by task 4**, closing open decision 5, and it amends `adr/0086` and `05 §7`.
[`adr/0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md).

The brief asked what the number is *derived from* and observed that **both candidates read as a
guarantee** they do not provide. The answer is that neither is needed, for two reasons that compound:

- **The generator version and `05 §7`'s world seed are one requirement, not two.** A seed is consumed by
  exactly one thing — `WorldKey.FromSeed` — and every call site derives a key and discards the number;
  **`World` does not retain the seed at all**. The artefact that keeps one is the **Input Log**, because
  a replay *re-derives*: it re-runs `SyntheticCity` and `RoadGenerator` on every read. A save restores
  columns and calls no generator on any path, so `adr/0021`'s failure — *seed 42 produces different
  terrain* — cannot occur in a version-1 save.
- ⚠ ***A placeholder inverts the guard.*** `adr/0021` **pins** this number, so a build that grows a
  generator must **refuse** every save written before it. With no generator version in the header, the
  **format version** delivers that refusal for the right reason. With `generator_version = 1` in it, the
  terrain build compares 1 against 1, agrees, and loads a pre-terrain city onto a landscape that was not
  there when it was saved. ***The field added to prevent the failure is the field that permits it.***

**What replaces the third row is a class rather than a field**: a world-creation value that lives in the
binary rather than in a table. Four are built — `TICKS_PER_DAY`, `WHEEL_SIZE`, `CellGrid.WorldCells`,
`CellGrid.TilesPerCell` — the generator version is its unbuilt member, and **each of the four says
*baked into the save* in the file that owns it** while nothing had ever baked one.

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

#### ✅ Task 3 shipped 2026-08-17 — `Rows.Restore`, and the sharpest finding is a claim this document was right about and the test was wrong about

**Built:** `Rows.Restore(slotCount, liveCount, freeHead, nextId, savedBytes)` — **one call**, not a
scalars-then-columns pair, because the ordering is forced (columns cannot be read before `SlotCount` is
set; the consistency walk cannot run before they are read, since the generations and the free list
*are* columns) and a two-phase door has a forgettable half. `Rows.FreeHead` and `Rows.NextId` are the
two accessors that did not exist. `VerifyRestored` walks what the file claimed. All three are
**`internal`**, which is **D1 enforced mechanically rather than stylistically**: `Borough.Formats`
cannot call them, so the serialiser cannot live there. `RowsRestoreTests`, eight tests.

⚠ **1. The free list is *entirely* inside the State Hash, and this brief said so while the first draft
of the test said the opposite.** `Rows.Fold` folds `_freeHead` as one of its four scalars **and** folds
`free_next`, which is `Saved<int>(..., Touch.Cold)` — so head *and* chain are hashed, and a loader that
recomputed the free list diverges **at the load**, not downstream. Task 3's own text in this document
gets this exactly right (*"produces a different `_freeHead` and a different hash"*); the test was
written claiming the hash would miss it and the divergence would surface later. **Corrected before
commit, and recorded rather than quietly fixed**, because the error is instructive: it is more
flattering to a test to believe it catches something nothing else can. ***A second instrument for
something already covered earns its place by naming the consequence, not by catching more*** — and
what `The_next_allocation_lands_where_the_save_says_it_will` buys is a failure that reads *the next
Household went to a different slot* instead of two 64-bit numbers disagreeing.

⚠ **2. A generated city never frees a Household, so the free-list path's fixture had to be made on
purpose.** 512 Ticks of the shipped Ruleset leaves `HouseholdTable.FreeHead` at `NoSlot`, and that is
correct rather than surprising: [`adr/0054`](../docs/adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)
sends a demolished Building's Households to the Unplaced Pool **with their money intact**, so
demolition — the only thing in a generated city that destroys anything — never retires a Household
row. Four tests were written against a generated world and **all four failed on the fixture rather
than on the code**. ***A restore path tested only against append-only tables is a restore path whose
free list has never been read***, and it would have passed. Same shape as slice 10 task 11, and the
same shape as `GoldenFixtures.Build`'s own hand-written retirements, which say in their comment that
they exist *"so the free list and the never-reused id counter are both off their initial values"* —
**that fixture already knew, and a generated one still does not**.

⚠ **3. `AllocateSlot` does not clear the slot it hands out, which is safe today only because storage
starts zeroed.** It writes the generation, the id and `free_next` and leaves every other column
holding whatever was there. That is fine for a table that only ever grows into fresh arrays, and it
stops being fine the moment a restore can make `_slotCount` **shrink** — the next allocation past the
restored high-water mark would hand a new row the previous occupant's bytes, in columns nothing
initialises. `Restore` therefore clears `[slotCount, capacity)`. **Found by writing the restore rather
than by reading the allocator**, and it is the one place in this task where the new capability created
an obligation on old code instead of composing with it.

**What `VerifyRestored` checks, and why each is worth its line:** the free list terminates
(**bounded by the slot count**, because a cycle is the one shape that would hang the load rather than
fail it); every free-list slot has an even generation; the free list's length equals
`slotCount − liveCount`; live slots carry an id in `[1, nextId)`; **dead slots carry id 0**; and the
count of odd generations equals `liveCount`. ⚠ **The dead-slot id check is the cheapest available test
of *do not compact***: `FreeSlot` zeroes every column, so a non-zero id in a dead slot means the
residue was not produced by this allocator — which is what a compacting, reordering or hand-edited
file looks like from the inside.

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

#### ✅ Task 4 shipped 2026-08-17 — `SaveHeader`, and the third version number turned out to be a class with an unbuilt member

**`src/Borough.Core/Persistence/SaveHeader.cs`, 52 bytes, 13 tests, and two open decisions closed (D5,
D6).** One new ADR, [`0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md),
which amends `adr/0086`'s table of three and `05 §7`'s copy of it.

**The header, field by field:**

| Offset | Width | Field | Why it is here |
|---|---|---|---|
| 0 | 8 | magic `borosave` | this is a save at all |
| 8 | 4 | **format version** = 1, little-endian | versions the **declaration set**. ⚠ not `World.HashSeed`'s version byte, which signs a re-baseline of the *hash* and whose own remark says appending a table does **not** bump it |
| 12 | 8 | byte-order sentinel, **native** order | the one field written in the machine's order |
| 20 | 8 | **world key** | not a column, folds nothing, and `RebuildDerived` cannot run without it |
| 28 | 8 | **Ruleset content hash** | `Simulation._inForce`, and writing it here is what dissolves that field |
| 36–51 | 4×4 | `TICKS_PER_DAY`, `WHEEL_SIZE`, `CellGrid.WorldCells`, `CellGrid.TilesPerCell` | the world-creation constants that live in the binary rather than in a table |

**Three of the brief's own claims moved.**

⚠ **The generator version is not written, and the brief's *create it* was the wrong instruction** — D6.
Its failure cannot occur in a save that calls no generator, and a placeholder would *permit* the failure
the pin exists to refuse. What the row was reaching for is the **class**, whose four built members each
say *baked into the save* in their own file and had nowhere to be baked into until this task.

⚠ **The header carries the world *key* and not the world *seed*, and `WorldKey`'s own remark says the
opposite.** *"Save the seed, not the key"* is **true of the Input Log**, which writes `seed 0x…` and
derives on replay, and the sentence has been honoured by the one artefact it was written for the whole
time. **`World` cannot produce a seed** — every `FromSeed` call site derives and discards — so writing
one would have meant a four-call-site signature change and a hash move, for a value with **no reader**.
It is annotated rather than struck, and the type grows an `internal WorldKey.Restore(ulong)`, which
reopens exactly the guarantee its private constructor exists to give, for one caller.

⚠ **The Tick is not in the header either**, though `05 §7`'s deleted listing had it: `adr/0058` moved it
into `ClockTable`, so a header copy would be a second copy of a **saved column**. A save browser wanting
*Day 412* without reading 131 MiB is the reason it would come back, as a derived echo.

**Two findings outrank the header.**

⚠ ***A version number guards the artefact that re-derives, not the one that restores.*** The generator
version's failure is **live today** and it is the **Input Log's**: a replay re-runs `SyntheticCity` and
`RoadGenerator` from `log.Seed` on every read, so a change to either leaves every line parsing perfectly
and replays into a different city. `InputLogCodec.Version`'s rule already covers it in the letter — *"a
different meaning for `seed`"* — and all three of its examples are changes to that file, so nobody has
ever read the clause as reaching code it does not import. Routed there on the day, per `adr/0073`,
rather than worked around here.

⚠ ***The State Hash cannot notice a missing world key.*** `World.Key` folds nothing, so a loader that
dropped it would restore every column, **hash identically at the instant of the load**, and diverge on
the next Tick. That is a property of the *test*: the round-trip form of the Factorio test structurally
cannot catch it and only the run-N-more form can — which is why task 7 is specified as the long form.

**Smaller, and worth keeping.** The header's fixed fields are explicitly little-endian and the sentinel
is not, because the **body** is `MemoryMarshal.AsBytes` and therefore native-order — [`0012`](0012-corpus-audit.md)
item 5's defect reaching the save. This cannot fix it and can refuse it: ***a guard that cannot fix a
defect can still refuse to proceed into it***, and the alternative is discovering it 131 MiB in. And
`BOR0206` caught `HashCode.Combine` in the first build of the file, which is the analyser doing its job
on a type that never reaches `step()`.

### Task 5 — the writer and the reader

`World` → bytes and bytes → `World`, in `Borough.Core`, over tasks 2–4. Load ends by calling
`World.RebuildDerived()`, which task 1 has by then made trustworthy.

The reader refuses rather than degrades where `05 §7` says to: an unaccounted Ruleset mismatch in
replay, a generator version mismatch always, a format version **newer** than the binary always. A
format version older runs the migration chain — which has **no entries yet**, and shipping the chain
with none is correct: the version number is what makes the first migration writable later.

#### ✅ Task 5 shipped 2026-08-17 — `SaveFile`, and the buffer the brief assumed turned out to be unnecessary rather than merely large

**`src/Borough.Core/Persistence/SaveFile.cs` and `SaveStream.cs`, 7 new tests, 20 across the namespace,
and open decision 1 closed (D7).** `World` → bytes → `World`, in one pass each way, ending in
`RebuildDerived()`.

⚠ **The shape was decided by a memory objection rather than by the three options the brief listed**, and
the objection was right: `adr/0087` already spends a copy of the world at save time, so a staged file
would put a second body of the same order beside it at the one moment memory is highest — **131.33 MiB
at 1,000,000 Citizens**. **It is not needed at all.** A column's slots are contiguous, so the writer
hands the sink a window onto storage that already exists and the reader hands the source the destination
to fill.

| | Golden fixture | Stepped, 4,000 | 1,000,000 allocated |
|---|---|---|---|
| The file | 305,389 B | 595,101 B | **131.33 MiB** |
| Largest single hand-over | 16,384 B | 32,000 B | **7.63 MiB** (`citizen.id`) |
| Share of the file | 5.36% | 5.38% | **5.81%** |

⚠ **Task 2's `WriteBytes`/`ReadBytes` pair is deleted, and the deletion is the finding.** It was built to
copy a column *out to* and *in from* a buffer, and streaming needs neither — one `StorageBytes(slotCount)`
replaces both. ***A copy exists to bridge two layouts, and there is only one layout here.*** The width
check `ReadBytes` performed has not gone: it moved to the **source**, which is the thing that runs out of
file, so it fires once instead of once per column.

⚠ **Task 3's `Rows.Restore` keeps its indivisible door and changes what it is handed.** It took the
table's saved bytes as one span, which made a loader's peak the largest *table*'s saved set — 22.5 MB at
1M. It now takes the `ISaveSource` and pulls each column straight into its own storage. **Splitting it
into scalars-then-columns was considered and refused**, because task 3's own remark argues that the pair
has a forgettable half; the price is that `Rows` now names a `Persistence` type, and the door being
indivisible was the argued property.

⚠ **Two growth defects, and one of them was mine from task 3.** `Rows.GrowTo` doubled from the declared
capacity, and **`0 × 2` is `0`, so it did not terminate** — reachable directly, because the loader builds
its world at zero capacity and every table is sized per thousand Citizens. It now grows to the **exact**
slot count, which is also tighter: a restore is told its final size, so doubling would round a 132-slot
table to 256 and hold the difference for the life of the world. The allocator's own `Grow` **shares the
premise and fails differently**, returning a capacity of zero and then indexing past the end of an empty
array; it has a floor of one now. ***A doubling growth rule assumes a non-zero base, and neither site
said so*** — invisible while a world could only be built by construction, and `A_loaded_world_can_be_allocated_into`
is the test that would have caught it.

**What is deliberately not here.** The Ruleset policy: `SaveFile.Read` **reports** the header and
enforces nothing, because `05 §7` gives cross-Ruleset loading two answers — lenient in play, refused on
an unaccounted mismatch in replay — and which applies is a property of the **shell**, so deciding it here
would give `Borough.Godot` the headless runner's policy. ⚠ **The lenient half is more than a policy and
is not built**: a load into a *different* Ruleset must **degrade** — drop Bins whose Resource is gone,
derelict Buildings whose kind is gone — which is `World.Adopt`, and this constructs the world with the
Rules it is handed and calls `RebuildDerived` instead. A same-Ruleset load is complete and correct, and
that is every load this milestone tests. ***The cross-Ruleset load is a mechanism rather than a
branch***, and naming it here is what stops it being discovered as a gap in task 8. The migration chain: there is one version and
nothing to migrate from, and an empty chain is correct rather than missing. And the Factorio test, which
is task 7 — what is asserted here is a round trip, and ***a round trip cannot catch a derived column that
rebuilds to the wrong value***, because only running on carries a wrong derived value into saved state.

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

#### ✅ Task 6 shipped 2026-08-17 — `WorldSnapshot`, and *the end of phase 7* turned out not to be the end of the Tick

**`src/Borough.Core/Persistence/WorldSnapshot.cs` plus `Simulation.SaveAtEndOfTick`, 6 new tests, 30
across the namespace.** `adr/0087` is amended in place on two clauses, both found by building it.

**The shape.** `WorldSnapshot` **is an `ISaveSink`**, which makes the copy and the write one mechanism
rather than two: writing the world into a memory sink *is* the copy, so there is one writer used twice
against different sinks rather than a serialiser and a separate cloner that could disagree about what
the file contains. The buffer is allocated on the **first** save — a session that never saves must not
carry 131.33 MiB for the option — and reused after that, so an autosave costs no allocation.

⚠ ***"The end of phase 7" and "the end of the Tick" are different instants, and the difference is one
increment of saved state.*** The copy is taken after `World.Advance`, one statement later than
`adr/0087` says. Everything that ADR argues about phase 7 is **correct and untouched** — it is serial,
and both double-buffered tables have settled, because `MapLayers` swaps inside the **Layers** phase.
What it does not account for is that `Advance` increments the **Tick**, and the Tick has been saved
state since `adr/0058`. A copy at the end of Commit records the Tick that has just finished, so
reloading it **re-runs that Tick** — one duplicated Tick, no error, and a hash that diverges from the
run it was meant to continue. `adr/0058` shipped **before** `adr/0087`, and the phrase reads as though
the two are the same moment.

⚠ **The finding cost me the same error it is about, and that is worth more than the finding.** The first
write-up blamed the **buffer swap** — asserting that `Advance` publishes the double-buffered tables —
which is wrong: `Advance` is one line and it is the clock. The mechanism was inferred from the shape of
the failure rather than **read**, which is `adr/0093` exactly, committed *while writing up a finding
about an ADR making the same kind of mistake*. What caught it was a test sweep behaving differently from
the prediction: **all five cases failed where the swap theory predicted only the diffusion-boundary ones
would**. ***A theory that explains the failure is not thereby the mechanism that caused it***, and the
cheapest discriminator was a prediction the theory had to make about a case it had not been fitted to.

⚠ **`adr/0087`'s hash clause is not buildable as written, and this is the one thing task 6 leaves open.**
***(⚠ WRONG, and corrected by task 10 on 2026-08-18 — see below the paragraph.)***
*"That hash is computed on the background thread from the copy"* cannot be done: `HandleColumn.Fold`
folds the **target row's monotonic id**, which lives in another table and is not a function of the
handle's bytes — the very divergence `adr/0086` names — so a fold over the copy produces a number that
is **not** the State Hash. ***A hash that folds a value the bytes do not contain cannot be computed from
the bytes.*** The clause is conditional (*"if a save is to carry a verified hash"*) and this milestone
carries none, so nothing rests on it. **But D4's seam was drawn on the strength of it** — *hash +
serialise + write*, ~42 ms rather than ~10 — and with no hash the seam is back at *copy | write*, which
is `adr/0087`'s own shape table. **D4's conclusion survives and one of its two reasons falls**, which is
`adr/0088`'s *a decision given several grounds is load-bearing on whichever ones survive*. A negative
assertion holds the line: `A_fold_over_the_bytes_is_not_the_state_hash`.

⚠ **THE PARAGRAPH ABOVE IS WRONG AND IS KEPT RATHER THAN DELETED, because how it was wrong is the
milestone's sharpest finding.** Every sentence in it is true and the conclusion is too wide. *A fold over
the copy produces a number that is not the State Hash* holds for **a column's own bytes** and fails for
**the file**: `Rows.cs:72-73` declares `id` and `generation` as `Saved` columns, so the id a handle
resolves to is in the copy, in another table's block. ***A value absent from a column's own bytes can
still be present in the copy.*** **Task 10 folds it and the seam comes back to D4's shape after all** —
*copy | hash + serialise + write* — with the hash on the **movable** side, so it costs the simulation
thread nothing. `adr/0112`.

⚠ **And the negative assertion did not hold the line; it is what made the wrong line look held.**
`A_fold_over_the_bytes_is_not_the_state_hash` is still green and was always about something else — it
folds the buffer flat, byte after byte, which is a thing nobody would ever want. ***A test name is a
description of the build, so it says which symbol to read and never what is in it***, and a **negative**
test is the most quotable kind of all because it reads as a closed door. `adr/0093`, on a surface that
ADR does not list. Three documents cited this test; none of them opened it.

**⚠ What this milestone cannot test, stated so it is not mistaken for tested.** Nothing is parallel
around the save, so a writer that walked the live world would produce a correct file today — which is
the case `adr/0087` says is *still a defect*. What is asserted instead is that the save is taken **by
the Tick** rather than by the caller, that it reproduces the hash at that instant, and that the copy and
the write are a real function boundary a thread could take.

**Two smaller things.** The two halves cannot be **timed** here, because `Core` may not read a clock
(`05 §4`) — which is why the seam is a function boundary and task 9's two numbers are the runner's to
take. And one save may be outstanding: asking twice before a Tick runs **replaces** the destination
rather than queueing, because two saves of the same instant are one save written twice.

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

#### ✅ Task 7 shipped 2026-08-18 — the round trip holds, and both findings are about the *instruments* rather than about the save

**`tests/Borough.Tests/Persistence/FactorioTests.cs`, 9 tests, 1,530 green.** `05 §4` **invariant 6 has
machinery for the first time in the project's life** — one of the two lints that never had any — and it
is green on every case tried. Nothing in the format or the rebuild was found wrong by it.

**The shape.** Seven Factorio cases: N ∈ {0, 1, 64, 129, 256} over `minimal.toml` and N ∈ {64, 256} over
`congested.toml`, each running M further Ticks in **lockstep** against a control that never saved, and
comparing the State Hash **at every Tick** rather than at the end. Comparing only at the end names the
wrong Tick, and the Tick a divergence happened on is most of the diagnosis. N = 0 is in the sweep on
purpose: a world populated and never stepped is the state every guard is written against. `congested.toml`
is there because a format tested against one fixture is tested against the columns that fixture happens
to move — the Movement tables joined `World._tables` in 5b and `minimal.toml` puts nobody in them.

⚠ ***A structural test over one fixture measures the fixture's content as much as the structure.***
`adr/0086`'s owed test — *the file's column set is the hash's `Saved` set* — is asserted **by corruption**
(scribble a column, watch the file move) rather than by comparing two lists, because both the writer and
`Rows.Fold` read `SavedColumns` and a list-to-list comparison would compare an array with itself. **The
first version covered 170 of 187 columns and reported nothing wrong**, because a corruption test is
silent about a table with no rows. Two whole tables were empty: `route_hop`, since 5c made the path
source **opt-in** and nobody drives under `minimal.toml`; and **`layer_cell`, which stands at zero rows
under all four shipped Rulesets** — none of them emits pollution (each says so in its own header: *a
dwelling is not industry*) and `MapLayers.SetLandValueTarget` has only test callers, which is CLAUDE.md's
*two of the six roots are producers rather than subsystems* arriving from underneath. **The fixtures were
made to reach the tables rather than the hole being pinned**: `congested.toml` covers `route_hop`, a
test-local world emits pollution and sets a land-value target for `layer_cell`, and the residue is
asserted at **the empty string**, by name, so a new hole is a failing test rather than a count nobody
re-reads.

⚠ ***A test that allocates heavily is not a local decision in a suite that runs in parallel and asserts
on allocation.*** The first structural test wrote a fresh file per column — 187 files a world, ~300 MB
across the three — and **made two unrelated allocation assertions fail**:
`ZoneRuleTriggerTests.Sweeping_allocates_nothing_after_the_first_trigger` (5,672 then 5,696 bytes) and
`QuantityTests.Arithmetic_on_quantities_allocates_nothing` (6,768 bytes), **over arithmetic that cannot
allocate at all**. It passed on its own, and passed run beside those two tests alone; only the whole
suite reproduced it. **Four full runs settle the causation**: HEAD green at 1,521; HEAD plus the
allocating test failing, with a *different pair* of assertions each time; HEAD alone green again with the
file moved aside; HEAD plus the same test reusing one buffer green at 1,530. ⚠ **The mechanism is a
hypothesis and is written down as one** — `GC.GetAllocatedBytesForCurrentThread` is served out of a
per-thread allocation context and a collection forced by another thread plausibly flushes it — and
`adr/0043` is why it stays a hypothesis: the refuting number exists and nobody has taken it. The
**causation** is measured and that is what the fix rests on. Routed on the day per `adr/0073`, to
`TableAllocationTests`' own remark, which is the sentence every future author of an allocation assertion
reads and which said **`GC.GetAllocatedBytesForCurrentThread` is exact** with six files resting on it.
*"Exact" is a claim about what it counts, not about what it reads.*

**What it does not establish.** The Factorio test is green over seven cases and that is evidence about
the cases run, not a proof about the rebuild — `RebuildDerived` is exercised wherever those worlds reach,
and task 1's audit remains the direct measurement. The heaviest thing still untested is a **long** run
with saves in it, which is task 9.

### Task 8 — something to look at

`--save PATH` and `--load PATH` on the headless runner, which is where `05 §7`'s *replay from save*
becomes a thing somebody can do rather than a paragraph. The picture is a **round trip that agrees**:
save at a Tick, reload, run on, print the hash trace beside the unbroken run's.

⚠ **`Options.cs` is this milestone's one file-level collision with the parallel milestone 6 session**,
which is adding `--evidence` to the same switch. It is a merge, not a conflict of substance.

#### ✅ Task 8 shipped 2026-08-18 — two flags, not a ninth mode, and a trace's Tick label was a claim about where the loop started

**`--save PATH` and `--load PATH`, `src/Borough.Headless/SaveStreams.cs`, 7 new runner tests, 1,537
green.** `05 §7`'s *replay from save* is a thing somebody can type.

**Flags rather than a ninth mode, and the criterion is the runner's own.** Every mode in `Options`
builds a city of its own to photograph; `--series` is a flag because *"it builds no world… it is a
second rendering of a run that is already happening"*. These ride the run that is already happening,
so they are flags. ***A criterion already written down beats a fresh judgement about the same
question***, and this one was written for `--series` in slice 7.

**What each does.** `--save PATH` writes the world at the end of the run and then **reloads it and runs
both cities on**, printing the two hash traces side by side with a verdict — because ***a save that is
never loaded demonstrates nothing***, which is `--traffic`'s reason for stepping its city twice. The
further stretch is `--ticks` again rather than a number chosen in the runner, since a fixed tail would
be a hash-bearing quantity with no ratifier in the one place it is cheapest not to have one. `--load
PATH` resumes in a **later invocation**, which is the only property a save has that `WorldSnapshot`
does not: the file outlives the process.

⚠ ***A label derived from a loop counter is a claim about where the loop started, and this one was
wrong the moment a run could start anywhere but Tick 0.*** `Session.Write` labelled each trace sample
`(i + 1) × hash-every`. That is the Tick **only for a run beginning at zero**, which was every run this
runner could do until `--load` existed — so a resumed run labelled its samples **128 and 256 while
standing at Ticks 640 and 768**. Plausible numbers, wrong ones, in a file whose entire purpose is to be
diffed against another. The assumption was nowhere written down, because until this task nothing could
violate it. **A second one beside it**: the trace header carried `citizens 10000`, the flag's default,
for a resumed run that never supplied it — and that header exists to record *what would have to match
for two traces to be comparable*, so a default is the one kind of wrong value it must not carry. Both
fixed at the source; the resumed run now reads its Citizen count off the loaded world.

**Two refusals worth their reasons.** `--load` beside `--log` is refused because ***a save records a
world and a log records the session that made one***, and permitting both would replay one session's
commands into another session's world from whatever Tick the save sits at, with no divergence
attributable to either artefact. And **a save resumed under a Ruleset it was not saved under is
refused, with `--force-ruleset` marking the trace `hash-broken`** — `RulesetCheck`'s polarity applied
to a file instead of to a log, which is exactly the mark `05 §7` asks for and which the header's
Ruleset content hash is what makes checkable. Verified both ways on `minimal-tuned.toml`: refused by
default, and forced it runs and diverges at the first sample, as a different Ruleset must.

⚠ **The stream adapter is in the shell and is not a format.** `Borough.Core` has no `System.IO` and
this milestone did not give it any, so `SaveSink`/`SaveSource` wrap a `Stream` in D7's two interfaces
from the side that already owns files. It is **not** in `Borough.Formats`, because that project holds
the artefacts that spell things in words and both shells must agree on those (`adr/0039`) — a save has
no schema of its own (`adr/0086`), and *a `Stream` wearing an interface is not a thing two shells could
disagree about*. ***It does not become a format because a second caller appears.***

**What it looks like**, at 4,000 Citizens over `minimal.toml`, 512 Ticks then 512 more:

```
Saved 547,488 bytes at Tick 512 to /tmp/city.borosave
Reloaded at Tick 512, hash FCFBE5311BC66072 against FCFBE5311BC66072

        tick  unbroken          round-tripped
         640  1DF7341EAC0CF707  1DF7341EAC0CF707
         768  23CAB88C60182DD4  23CAB88C60182DD4
         896  07D10632CC7B9821  07D10632CC7B9821
        1024  E1509D616B842CCB  E1509D616B842CCB

The round trip agrees, over 4 samples and 512 Ticks.
```

⚠ **It is a demonstration and not the test.** `FactorioTests` is the assertion — seven cases, two
Rulesets, compared at **every** Tick — and this compares on the trace cadence over one invocation, so a
divergence between two samples would be reported at the sample after it. What it is for is a person
watching a round trip happen, which no test can be.

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

#### ✅ Task 9 shipped 2026-08-18 — the run holds, and a slot-exact file is sized by the high-water mark rather than by the population

**`tests/Borough.Tests/Persistence/SaveLongRunTests.cs`, 2 tests, 49 Days = 100,352 Ticks.** The
milestone is complete.

**Three claims, split between two tests on where each defect would show.** *The writer is an observer*
is asserted **exactly and with one world** — the city's own State Hash either side of the write, every
Day, for forty-nine of them. *`SaveAtEndOfTick`'s bookkeeping inside `Step` is an observer too* needs a
second world in lockstep and is asserted over **four** Days, because it fails on the first save or not
at all. ***A property that fails immediately does not need a hundred thousand Ticks to fail in*** — and
the split is worth the sentence, because two worlds over forty-nine Days is **12m13s** against **6m**,
measured both ways.

**The numbers.**

| | |
|---|---|
| Run | 49 Days, **100,352 Ticks**, `minimal.toml`, 4,000 Citizens, 49 saves |
| File | 598,166 → **604,157** bytes, and the declaration's computed total is **604,157** exactly |
| Copy | **0.08 ms** mean, 0.59 ms worst → **7.14 GB/s** |
| Write | **0.23 ms** mean, 0.36 ms worst → **2.57 GB/s**, to the page cache |

⚠ ***A slot-exact save is sized by the city's high-water mark and not by its population, so its size is
an `adr/0006` instrument in its own right.*** Lots stand at **488** for the whole run and Buildings
oscillate **231–286** with no trend — and the file still **grew 1.0% and then stopped**, flat from
Day 34 to Day 48. That is not a leak and not noise: the file's size is `SlotCount`, `adr/0086` forbids
compaction, and a slot count is an allocator **high-water mark**, so the file grows to the city's peak
occupancy and saturates there. It is `TrafficLongRunTests`' saturation finding on a new axis, and it
means **a file that keeps growing in a city that is not growing is a slot leak** — visible in one
number, with no table walk. ⚠ **What is asserted is that it never *falls***, which is the only claim
over the series that is not fitted to what the series happened to do: ***where it saturates is a
property of the run's length; that it never shrinks is a property of the format***, and a file that got
smaller would mean dead slots had been dropped, which `adr/0086` refuses by name.

⚠ **The copy is carried as a rate and not as a duration, and against `adr/0087` it is the same order
at roughly 2×.** 0.08 ms over 604 KB says nothing about 131.33 MiB and would be quoted as though it did
(`plans/0012` **Cause 5**), so what is published is **7.14 GB/s**. At task 2's computed 131.33 MiB that
is **~18 ms** against the ADR's **~10 ms** — inside its *"~40 ms… is visible"* revisit band and above
the figure the decision rests on. ⚠ **The error direction is unknown and both signs are present**: this
is a **Debug** build, which flatters the ADR, and a 604 KB file **fits in cache** where a 131 MiB one
will stream from DRAM, which flatters the measurement. ***A bandwidth measured on a file 222× too small
is a hypothesis about the large one.*** **What is owed is the copy at 1,000,000 Citizens**, which this
fixture cannot produce and which nothing in this milestone was going to.

⚠ **The write is the other side of D4's seam and it is the bigger half** — 2.57 GB/s against the copy's
7.14, so ~53 ms at 131.33 MiB, and it goes to the background thread. That is the shape D4 predicted
arriving with numbers, and it is *to the page cache*: what a platter costs is not measured here and is
not the simulation thread's problem either way.

⚠ **The magic number's revisit trigger did NOT fire by the route this task was supposed to fire it**,
and it had already fired by another. The brief says *"this is where the deferred magic number's revisit
trigger fires — the first save carried across a build"*; this run writes and reads saves **inside one
process against one build**, so no save has yet outlived a build. But **task 4 wrote a magic number
against D2's decision not to**, which is `plans/0003 §The name`'s trigger firing four tasks early and
in silence — see **D2's 2026-08-18 amendment**, and it is on the user's desk rather than settled here.
***A trigger can fire without the event it was written to wait for.***

⚠ **`CrashArtifact.From` is still zero and stays zero**, per *What this milestone must not do*.

⚠ **The suite's cost was predicted at ~7 minutes and measured at 40 seconds, and the prediction was
wrong for a reason worth keeping.** In isolation the two tests take **6m** and **1m21s**; the suite went
**8m58s → 9m38s**. xUnit runs collections in **parallel**, so a long test that is not the critical path
costs only what it adds *to* the critical path — and this one hides almost entirely inside the run that
was already nine minutes long. ***A test's cost in isolation is not its cost in a suite***, which is
the same shape as task 7's finding pointed the other way: there, a test's *allocation* in isolation was
not its allocation in a suite either. **Both are consequences of the same parallelism and neither is
visible from inside the test.** The prediction is recorded rather than deleted because it was written
down before it was checked, and checking it took one command.

### Task 10 — the save carries its own State Hash, folded from the copy

⚠ **Not on the original list. It is the second of the milestone's two open questions, settled with the
user in the room on 2026-08-18** — and it was settled by finding that the thing task 6 recorded as
unbuildable is buildable, so the decision it needed was much smaller than the one it was going to need.

**What was open.** Task 6 left one question: does a save carry a verified State Hash? The reversible
default — **no hash in the file** — shipped through tasks 7, 8 and 9. `adr/0087` says such a hash would
be *"computed on the background thread from the copy, never on the simulation thread as part of taking
it"*, and task 6 recorded that as not buildable, because `HandleColumn.Fold` folds the target row's
monotonic id and a handle's bytes do not contain one. That left two options, both bad: fold the **live
world** on the simulation thread, which contradicts the clause and puts a **~42 ms** hitch at 1M where
`adr/0087`'s own revisit trigger says ~40 ms is visible; or take a **typed** copy, at 170.49 MiB against
the byte copy's 131.33 and a second `World`-shaped object graph.

⚠ **There was a third and the corpus had the evidence for it the whole time.**

```csharp
_id         = Saved<ulong>("id", Touch.Wake);        // Rows.cs:72
_generation = Saved<uint>("generation", Touch.Wake); // Rows.cs:73
```

**The id a handle resolves to is saved state**, so it is in the file — in *another table's block* of the
same copy, which is the whole of the difficulty and none of the impossibility. ***A value absent from a
column's own bytes can still be present in the copy.*** So the hash comes off the copy, the clause is
honoured rather than overturned, and the **simulation thread pays nothing**.
[`adr/0112`](../docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md).

**What shipped.** `src/Borough.Core/Persistence/SaveHash.cs`, a ninth header field, and a refactor of
the fold so that there is **one implementation against two sources**:

- **`Column.Fold` takes its bytes and a `TargetIds`.** The live path passes the column's own storage and
  the live target table; the save path passes a slice of the copy and that table's `id` and `generation`
  bytes, located in the same buffer. `Rows.FoldScalars` is shared the same way. A second fold written
  beside the first would have been two copies of one rule that must agree for ever, which is
  [`0012`](0012-corpus-audit.md) *Cause 1* built on purpose. ***The abstraction to reach for is the one
  that makes the duplicate impossible, not the one that makes it convenient.***
- **`SaveHash.Of(world, body)` reproduces `World.HashState()` exactly**, and it can because the two
  walks are the same walk — same tables in the same order, same four allocator scalars, same saved
  columns over the same slots. **The save file is the hash's input, written down**, which is `adr/0086`
  arriving at a consequence `adr/0086` did not draw.
- **It reads the world for its schema and never for a value** — table order, column widths, and which
  table a handle column points at, all fixed at `Rows.Seal`. ***A schema read is not a state read***,
  and that is what makes the call a thread's to take.
- **The header is nine fields and 60 bytes**, amending `adr/0111` on the count and nothing else. **Zero
  is not a sentinel**: a world can genuinely hash to zero, so there is no *unverified save* — which is
  only available because the field went in before release rather than being retrofitted as optional.
- **`SaveFile.WriteBody` is the copy and the body; `SaveFile.Write` puts a header in front.** The header
  carries a number that is a function of the body, so it cannot precede it into the snapshot. ***A
  header is a statement about the build and a copy is a statement about the world***, and the two were
  only ever adjacent.
- **`SaveFile.Read` verifies.** Restore, `RebuildDerived`, recompute, compare, refuse. So **`05 §4`
  invariant 6 is a property of every load** rather than of the seven cases in the suite. The load side
  folds the *live* world at 32.47 ms at 1M, which is free — a load is not inside a Tick loop, and only
  the save side ever had a budget to protect.

**What it catches, stated as the case nothing else could.** `Rows.Restore`'s consistency walk already
refuses a flipped byte in `id`, `generation` or `free_next`. It cannot see a flipped byte in a
Household's money: the file is the right length, every table restores, the world loads, and it is a
different city. `A_flipped_byte_in_the_body_is_refused_by_the_load` is that case.

⚠ **The sharpest finding is about how the claim was pinned, not about the hash.** Task 6's conclusion
was held in place by a **test** — `A_fold_over_the_bytes_is_not_the_state_hash` — which is still green
and was always about something else: it folds the buffer flat, byte after byte, which is a thing nobody
would ever want. ***A test name is a description of the build, so it says which symbol to read and never
what is in it*** — [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
on a surface it does not list, and **a negative test is the most quotable kind of all, because it reads
as a closed door**. Three documents cited it and none of them opened it. The four sightings `adr/0093`
records are an ADR summary, a plan's recommendation and two doc-comments; this is the fifth and the
first where the false description is a **test**.

⚠ **The second finding is that the cheap-looking answer would have spent a revisit trigger.** Folding
the live world was defensible on `adr/0087`'s own arithmetic — 32.47 ms once per in-world Day is 0.03%
of a Tick budget amortised, and the clause forbidding it is a **per-Tick** argument applied to a
**per-autosave** event, which is the exact error `adr/0087` was written to correct in `adr/0037`. It was
the recommendation carried into this session. What it would also have done is take the save's blocking
half from ~10 ms to **~42 ms at 1M**, which is the size `adr/0087`'s own revisit trigger names as
*visible* — reached by adding a feature rather than by growing the city. ***An argument that a cost is
affordable is not a reason to pay it when it can be moved.***

**Two smaller things.** A **copy is now part of the format rather than an optimisation**: the hash comes
off it, so there is no way to write a version-1 save without taking one, and `adr/0087`'s mechanism is
load-bearing twice. And `Rows<T>.IsValid` now delegates to a non-generic `IsValidSlot`, so **handle
validity is stated once** where it was about to be stated twice — the save path needs the same rule and
cannot see the element type.

**Tests.** `SaveHashTests`, 10 cases. The load-bearing one is `A_fold_over_the_copy_is_the_state_hash`
at 0, 1, 64, 256 and 1,024 Ticks — stepped, because the interesting content is handles into recycled
slots, which is where folding an id and folding a handle come apart. `The_copied_hash_moves_when_the_world_does`
is the discriminator without which the first passes for a fold that returns a constant.

⚠ **No baseline moved.** The State Hash is unchanged — the fold produces the same numbers from the same
inputs — and a header is not part of the city. **1,549 green.**

⚠ **One thing seen and deliberately not fixed here, routed rather than absorbed** (`adr/0073`). Every
load refusal reaches the user as an **unhandled exception with a stack trace** — the new hash mismatch,
and equally the pre-existing *truncated save* and *this is not a borough save*. It is `Session.Resume`
having no catch, it predates this task, and the messages themselves are right; **fixing it inside a save
commit would have hidden a runner defect inside a `Core` change**. Filed to [`0003`](0003-build-plan.md)
as a runner paper cut. ***A refusal a user meets as a stack trace is a refusal that reads as a crash***,
which is the opposite of what these messages were written to do.

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

✅ **None remain.** Items 2 and 4 were settled on review of this brief, items 3 and 5 by task 4, and
item 1 by task 5 — each by the task it blocked. Each is struck in place, per the corpus rule that a
closed decision keeps its number and its reasoning.

### ~~1. Where the I/O boundary sits~~ — ✅ **SETTLED 2026-08-17 by D7. Two interfaces, and the file is never assembled.**

**The question was `Stream` against `Span<byte>` against a callback per table, and the user's objection
picked it: a whole-buffer shape inflates memory at the one moment it is already highest.** `adr/0087`
already spends a **copy of the world** at save time, and a staged file would put a second body of the
same order beside it — **131.33 MiB at 1,000,000 Citizens**. It is not needed, because a column's slots
are already contiguous: `Column.StorageBytes` hands the sink a window onto storage that exists, and hands
the source the destination to fill. **The largest run of bytes in play is the largest single column —
7.63 MiB at 1M, 5.81% of the file — and nothing proportional to the save is allocated at either end.**
The full reasoning is D7; the original text stands unedited beneath.

*Original, 2026-08-17 — **blocked task 5**:*

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

### ~~3. `_phase` and `_inForce`~~ — ✅ **SETTLED 2026-08-17 by D5. All five dissolve, and three had already answered.**

**The recommendation below was right about both fields and the walk it asked for is where the value
was.** `_opened` dissolves on `_phase`'s own argument — a world that has never stepped has never reached
phase 7, so a save of an unopened world does not exist — and `_reloads` and `_degradation` were **already
documented as per-run rather than per-world, in their own remarks**: *"since this Simulation started"*,
and *"the trail is world state because `05 §7` puts it in the save; this is a Simulation's, because a
warning is about the run."* ⚠ **Dissolving out of the save is not dissolving out of the loader** —
`_opened` and `_inForce` must be *supplied* at construction, or the first step after a load adopts its
Ruleset as an **opening** rather than a **transition** and writes no provenance trail entry. The full
reasoning is D5; the original text stands unedited beneath.

*Original, 2026-08-17 — **blocked task 4**:*

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

### ~~5. What is the generator version derived from?~~ — ✅ **SETTLED 2026-08-17 by D6. Nothing, and it is not written.**

**The asymmetry the text below points at was the answer, and the third option is not a better derivation
but no field.** A seed is consumed only by something that regenerates from it and nothing does, so the
generator version and `05 §7`'s world seed are **one requirement rather than two**; and because
`adr/0021` *pins* that number, **a placeholder inverts the guard it was added to provide** — a terrain
build compares 1 against 1, agrees, and loads a pre-terrain save. [`adr/0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md).
The full reasoning is D6; the original text stands unedited beneath.

*Original, 2026-08-17 — **blocked task 4**:*

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
