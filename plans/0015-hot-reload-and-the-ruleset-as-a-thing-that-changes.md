# 0015 — Slice 8: hot reload, and the Ruleset as a thing that changes

> Slice 8 of [`0003-build-plan.md`](0003-build-plan.md). Roadmap **milestone 3b**.
> Governed by [`adr/0015`](../docs/adr/0015-all-tuning-data-is-hot-reloadable.md),
> [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md),
> [`02 §4.3`](../docs/02-simulation-model.md), [`05 §7`](../docs/05-technical-architecture.md),
> [`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md),
> [`adr/0051`](../docs/adr/0051-industrial-pollution-is-a-stock-the-environment-absorbs.md),
> [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md),
> [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

**A Ruleset stops being a thing the world is created with and becomes a thing that happens to it.**
Slice 7 made a Ruleset reach a `World`; this slice makes a *second* one reach the same World while it
is running, through the only door, recorded so a replay reproduces it exactly, with every removal
having a defined and deterministic consequence.

**The risk it retires** is the one [`adr/0015`](../docs/adr/0015-all-tuning-data-is-hot-reloadable.md)
calls project-survival: *tuning is slow, so tuning stops happening, so the simulation becomes
unbalanceable*. Citybound's 60–120 second warm rebuild is the evidence and its author's own devblog is
the confession. The ADR's acceptance test is one sentence — **changing a production ratio and seeing
the effect must take seconds** — and it is checkable rather than aspirational, which is why it is task
9 here rather than a closing remark.

**The second risk, and it is this slice's own:** reload is *specified* in four documents, *anticipated*
in six places in the tree, and **exercised nowhere** — so nobody has ever found out whether any of them
agree. Planning found that they do not. One document argues against itself, one design the board was
holding open turns out to have been settled three times over, the obvious implementation is refuted by
a 12-byte struct, and there is a live `const` defect underneath the slice's own definition of done.
See *What planning found*.

---

## Status

**Tasks 1–3 shipped.** The gate is clear and slice 10 has closed, so *The parallel session*'s
scheduling constraint is discharged.

- **Task 1** — the swap, at the top of Phase 0. **Decision owed 1 is settled as recommended** and
  asserted rather than assumed: the swap runs first, so a Tick has exactly one Ruleset. **It shipped
  narrower than the task describes, and the narrowing is the finding**: a reload may move **numbers**
  and may not move **structure**, where the deciding test is *what live state points at it* — which
  is precisely the width `adr/0015`'s acceptance test asks for, since a production ratio is a number.
  A structural reload is **refused by name** until tasks 4–6, and two refusals nobody planned turned
  out to be load-bearing: a transition this session cannot resolve, and a catalogue that does not open
  with the world's own Ruleset.
- **Task 2** — the transition in the Input Log. `RulesetHashAt` stopped discarding its argument and
  **cost zero call sites**, which is what the slice-5 stub was written for. **The format version did
  not move, and the rule that said it should is the finding**: *bump when a field is added to the
  header* is a proxy for *bump when an old reader would misread a new log*, and the proxy has now
  been wrong twice in the same direction. The redundant `from` hash earns its place by catching a
  **spliced log** at parse time. Four builder refusals, each a claim about what a log may say
  happened — including that a Tick carries at most one reload, which is the log's half of task 1's
  ordering.
- **Task 3** — the world-creation refusal, and the `const` that had to move first. The defect is
  discharged: the kernel radius is `[layers] kernel_metres`, and **the whole `[layers]` table was
  built at once rather than only its one world-creation member**, which absorbs most of task 8 and
  settles **decision owed 2**. Four things came out of it that were not in the plan. **`LayerRuleset`
  stopped being a constructor argument of `World` beside the Ruleset and became a member of it** — the
  old shape admitted a world whose cadence disagreed with the Rules in force, and the first reload
  would have silently reverted it. **The reload comparison is in Cells, not metres**, because Cells
  are the units the field is stored in, which is `adr/0015`'s membership test rather than an
  approximation of it. **The refusal count went 8 → 11 at load plus a 12th on reload**, and the twelfth
  is the first check in the project that is a property of a file *against a world*. And **`adr/0015`'s
  world-creation enumeration turned out to be one-quarter implemented**: `TICKS_PER_DAY`, `WHEEL_SIZE`
  and the Cell are all `const`s no Ruleset can state, so the ADR's own sentence about them is false of
  three of its four members. Filed to `0012` as a sentence to fix rather than a hole to plug — nothing
  is unguarded, because a file that cannot state a number cannot change it.
- **Task 4** — the degradation. **Tasks 4, 5 and 6 were merged before any of them was written**,
  because none is reachable alone: dereliction needs a kind removed, which trips `KindCount`,
  `RuleCount` and usually `ResourceCount` at once, and the structural refusal lifts once or the
  reload half-happens. **Sub-task A shipped**: a declaration's identity is now its **name**, not its
  id. `RulesetShape.Compare` had assumed ids were stable across two files and they are not — a
  reordering left every count and shape identical while every live Bin row started naming a different
  Good, and *removing a declaration from the middle of a file is a reordering*, so every real removal
  the degradations exist for had it. Two other things were settled without code: **derelict is
  derived, not a saved flag** — the only actor is a designer, whose commonest move is undo, and a mark
  that never clears leaves them a city of permanently inert bakeries — and **`adr/0055`'s consequence
  bullet is false**, because a Building with no Rules has no failures to die of.
- **Next: task 4 sub-tasks B and C** — the migration map, and the one pass over the world.

---

## The parallel session, and why this is a plan before it is code

**Slice 10 is in flight in another session** ([`0014`](0014-zone-rules-and-the-sweep-family.md)), and
the two slices collide in exactly the two places that merge worst:

| Where | Slice 10 | Slice 8 |
|---|---|---|
| `Borough.Formats.RulesetLoader` | adds `[[zone_rule]]` and three refusals "on the same walk as the other five" | adds a **second entry point** with a different signature, and a refusal class the walk cannot currently run (task 3) |
| The golden baselines | hash-bearing throughout — a saved permission set, a third `purpose_tag`, churning rows | hash-bearing — the Layer cadence stops being `LayerRuleset.Default` (task 8) |

Two sessions re-recording one baseline is not a conflict that merges; it is one that has to be re-run.
So this document is written now and the code waits for slice 10 to land. **That is not a cost.** Every
slice in this project that was planned before it was written found something before a line existed —
slice 7's planning found a live `adr/0006` defect in slice 6, slice 10's planning inverted its own
name — and this one is no exception.

**One item is jointly owed and should be watched rather than duplicated:** the **derelict flag**.
`0014`'s decision owed 2 names it, `0014`'s *what this slice does not do* assigns it here, and
`0011` finding 39 assigns it here too. Task 4 owns it. If slice 10 needs it first, it should take it
and this task becomes a read.

---

## Gate

**Cleared** by session A → [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md),
which cleared slices 7 and 8 together because both were waiting on the same ADR.

Two things came out of that session which this slice is downstream of, and both are load-bearing here
rather than incidental:

- **The *"must not slip behind 3c"* claim is retired, not re-grounded.** It was one unargued claim
  counted twice — `adr/0015` citing `06` citing `adr/0015`. Slice 6 falsified it at no cost, because
  the no-`const` rule was already doing the work `LayerRuleset` was supposed to need a milestone for.
  **What replaces it is checkable, and it is task 8: slice 8 is not done until the Layer cadence and
  rates load from a file.**
- **Reload's log representation is settled.** Hashes travel in the Input Log; Ruleset **content**
  travels in the crash artifact. See *What planning found*, item 1 — the board files this as an open
  doorstep item and it is not one.

---

## Prerequisites

Slices 4, 5, 6 and 7, all closed. Specifically: the Input Log and `Replay`, the command model and
`Simulation.ApplyInput` as the one door, the eight Tick phases, `LayerRuleset` as a **constructor
argument of `World`** rather than a constant, the Ruleset loader with its five refusals, `ContentHash`,
the crash artifact, and `World.CreateBuilding` with the arming stagger.

**Six pieces are already in the tree with this slice's name on them**, written by earlier slices as
debts rather than as guesses. They are unusually specific, and most of them answer a question rather
than only marking a hole. **Two are finished mechanisms with no caller at all**, which materially
changes what this slice has to build:

| Where | What it says |
|---|---|
| `src/Borough.Formats/RulesetRefusal.cs:96` | **`RulesetInForce` is built and nothing in `src/` references it.** `TryReplace` swaps only on a clean load and returns the refusals otherwise, so *"the previous Ruleset stays live"* **already has somewhere to be true**. Its own remark: *"Swapping at a phase boundary, dropping wait lists and logging the transition are slice 8's; this is the holder those need, and nothing more."* It also keeps a **refusal count rather than a flag**, because a count that climbs while the city runs is `adr/0048`'s quote-marks revisit trigger firing |
| `src/Borough.Core/TickInput.cs:53` | `TickInput.RulesetHash` exists, is populated every Tick from `Replay.cs:155`, and **no code in `Core` reads it.** Its doc names hot reload as the reason it exists |
| `src/Borough.Core/Input/InputLog.cs:141` | `RulesetHashAt(Ticks tick)` — **a stub with the right shape**. It discards the Tick and returns the one hash. Its remark states that slice 8 makes a reload *"a transition in this log carrying both hashes"*, that the **log format changes** and so does every caller, and — the sentence that settles a board item — *"the hash is what travels here; the content travels in the crash artifact"* |
| `src/Borough.Core/Space/LayerSchedule.cs:205` | *"It is a constructor argument of `World` rather than a constant… **Slice 8 will read it from the Ruleset**; until then a caller supplies it, and the ability to supply two of them is what let `adr/0044` compare two cadences' hash traces"* |
| `src/Borough.Core/Space/LayerSchedule.cs:177` | `PollutionTau` is **128**, and it is `TICKS_PER_DAY ÷ the pollution cadence`. **Derived, not picked** — and task 8 has to move a derivation into a file, not three integers. See *What planning found*, item 6 |
| `src/Borough.Core/Entities/World.cs:435` | `CreateBuilding`'s undeclared-kind early return, with the comment saying there is no derelict flag yet and it arrives with hot reload. `Ruleset.Declares(kind)` is the read side (`0011` finding 39) — **built; the write side is not** |

---

## What planning found, and it changes the slice's shape

Six items. The first three remove work; the last three add it. **Item 5 is a live defect** rather
than a question, in the family slice 7's planning found in slice 6.

### 1. The board's second doorstep item is not a decision — it is a correction to one sentence

The board says: *"`Replay.Start` takes a Ruleset while a log carries only its **content hash**, which
`02 §4.3` already says is not enough — 'a replay needs the Rules' content, not the news that they
changed'."* Read as filed, that is an open design question about what the Input Log carries.

**It is already answered, in three places, and none of them is `02 §4.3`.**

- [`05 §7`](../docs/05-technical-architecture.md): *"A replay bundle is the log plus every Ruleset it
  references, held in a **content-addressed sidecar** — which keeps the log itself kilobytes, dedupes
  identical reloads for free, and needs no bespoke diff format (`adr/0018`)."*
- Session A, via the gate board: hashes in the log, content in the crash artifact.
- `InputLog.cs:136`, in the tree: *"An Input Log is shared between people who **have the Rulesets in a
  repository**, and an artifact is attached to an issue by somebody who may not."*

So the log carries hashes, the *bundle* carries content, and the two statements were never in tension.
**What is actually wrong is `02 §4.3`'s sentence**, which states the transition carries both content
hashes and then, in the same breath, says a replay needs content rather than *"the news that they
changed"* — and a hash **is** the news that they changed. The sentence argues against itself. That is
the shape session A found twice (`02 §4.3`'s *"requires no parser"*, and `CONTEXT` → Input Log's
reason for carrying hashes), and this is the **third instance in the same section of the same
document**.

*Consequence:* no decision is owed. A correction to `02 §4.3` is, and it is filed under *Owed to other
documents*. The board item should be struck rather than answered.

### 2. A refused reload never reaches the Input Log, and that follows from `adr/0015` rather than being chosen

`adr/0015`: *"A malformed Rule reports a file, a line, and a rule name, and **the previous Ruleset
stays live** rather than the game dying."* If the previous Ruleset stays live then **no state
changed**, so there is nothing for a replay to reproduce, so the attempt does not belong in a log
whose job is to record what happened.

The tempting alternative — log the attempt, replay the refusal — is worse than it looks. It would
require the refusal itself to be **bit-stable across binaries**, and it is not: the refusal is
`Borough.Formats`' and its whole deliverable is a human-readable string that we intend to keep
improving. A log that replays only if nobody improved an error message is not a log.

*Consequence:* the transition is recorded **after** validation succeeds, never before. And the
mechanism that makes it true is already written and unused: `RulesetInForce.TryReplace` swaps on a
clean load and returns the refusals otherwise, with `Current` untouched. Task 1.

### 3. A reload is a *transition*, not a command — and the tree already says so twice

This is the item the draft of this plan got wrong, and it is worth recording how, because the wrong
answer is the one the board's own framing suggests.

The obvious design is a sixth `CommandKind` carrying the new Ruleset's content hash, on the S0a
precedent: `Populate` went through Phase 0 because `Simulation.ApplyInput` is *"the only door"* and a
state change entering any other way is one no replay reproduces. A reload is a larger state change
than a population, so the same argument should apply with more force.

**It does not survive contact with three facts.**

- **`Command` is 12 bytes and cannot carry a hash.** It is `(CommandKind kind, Tiles east, Tiles
  north, ushort zone)`, documented as having no padding. A `ulong` content hash does not fit, and
  widening the struct is a change to the **log format for every verb** and to the State Hash, paid so
  that one verb can carry a field the log already states elsewhere. `Populate` explicitly refused the
  same trade: *"it carries no payload: the size is `WorldConfiguration.Citizens`, which the log
  already states"*, because a command asserting a fact the log also asserts lets one log say two
  things.
- **The machinery for the transition already exists, unused, in two places.**
  `InputLog.RulesetHashAt(Ticks)` is a stub whose signature is already *"which Ruleset is in force at
  Tick T"*, and `TickInput.RulesetHash` is populated every Tick from it and **read by nothing**. Both
  were written for this slice and named it. A command verb would leave both of them stranded and
  build a third path.
- **The corpus never calls it a command.** `02 §4.3`: *"recorded in the Input Log as a **transition**
  … not merely as an event"*. `05 §7`: *"a hot reload is logged as a **transition** carrying both
  hashes"*, and `§2`'s tuple is `(world seed, configuration, Ruleset content hash, player commands per
  Tick)` — the Ruleset sits **beside** the commands, not among them.

**So the door argument is satisfied without a verb.** `TickInput` *is* Phase 0's input; the Ruleset
hash is already on it; nothing reaches in from outside a Tick. `Simulation` compares
`input.RulesetHash` against what is in force and, when it differs, performs the swap at the top of
Phase 0 — which is the phase boundary `02 §4.3` asks for.

*Consequence:* **no new `CommandKind`, and `Command` is not widened.** Task 1 changes shape
accordingly, and decision owed 1 is retired before it was filed rather than carried. The cost is one
thing the verb design gave for free and this does not: the shell must supply **every** Ruleset the
session references before the run starts, because `Core` cannot turn a hash into Rules. That is task
9's problem and `InputLog.cs:131` had already stated it.

### 4. Reload introduces a refusal class the loader structurally cannot run today

The five existing refusals are all properties of **the file alone** — a cycle in `on_fail`, a `fills`
mismatch, an unterminated chain, unbalanced money, an unquoted decimal. Validation needs the file and
nothing else, which is why `adr/0048` could put all five in one walk in `Borough.Formats` and be done.

**Reload adds refusals that are properties of the file *against a particular world*.** `adr/0015`'s
world-creation category is explicit: `TICKS_PER_DAY`, `WHEEL_SIZE`, the **Cell**, and the **industrial
pollution kernel radius** are read from the Ruleset but *fixed when a world is created and baked into
the save*, and **a reload that changes one is refused rather than applied**. Whether a value changed
is only knowable against the world it would be applied to.

This looks at first like a genuine collision between two ADRs — `adr/0048` puts every refusal in
`Formats` *"where the names are"*, and `adr/0002` forbids `Core` from producing the string. It is not,
and the resolution is small: **`Formats` does not need to see the world, only the world's frozen
constants**, which are a handful of integers and cross the boundary under `adr/0048`'s own rule
without difficulty.

*Consequence:* `RulesetLoader` grows a **second entry point** taking `(file, the world's world-creation
constants)`, and `adr/0048`'s *"five refusals in one walk"* count becomes **five plus a reload-only
class**. The count is stated in three documents and has drifted before — `plans/0012` *Cause 1* is
exactly that — so whichever number this lands on gets written in all three at once. Task 3.

### 5. The industrial pollution kernel radius is a `const`, and it is supposed to be Ruleset data

**A live defect, found while checking item 6 against the tree rather than against the documents.**

`adr/0015`'s world-creation category is not an exemption from the Ruleset. Its own words: these
constants *"live in the Ruleset like everything else and are **read** from it, but they are fixed when
a world is created and baked into the save"*. The category freezes a number **per world**; it does not
move it into the binary.

The **industrial pollution kernel radius** is an enumerated member of that category — `adr/0044` put
it there, and it is the one number in the whole `adr/0044` episode that **passed** `adr/0015`'s
membership test. It is currently `public const int IndustrialPollutionMetres = 1_024;` at
`src/Borough.Core/Space/SeparableKernel.cs:177`, with the kernel built from it at `:180`. It is not in
`LayerRuleset`, it is not in any file, and no loader has ever seen it.

`CLAUDE.md` states the rule without qualification: **a `const` where a Ruleset value belongs is a
defect, not a shortcut.** This is one.

It matters here rather than being merely tidy, because task 3's world-creation refusal has **nothing
to refuse** while the value lives in the binary — a designer cannot change it in a file, so a reload
cannot detect that they did. The refusal and the defect have to be fixed in the same task or the
refusal is untestable theatre.

*Also worth noting for whoever fixes it:* the radius is **UNRATIFIED** per `CLAUDE.md`'s constants
table — *"the 1–10 km band is 10× wide and wants a source"* — so moving it into a file does not settle
it, and the `0002` §D row stays open.

### 6. The Layer numbers are not independent, and a file listing three integers loses the derivation

Task 8's obligation reads like plumbing: move `LayerRuleset.Default` into TOML. It is not, because of
what is inside it.

`PollutionTau` is **128**, and `LayerSchedule.cs:177` says why: it is `TICKS_PER_DAY ÷ the pollution
cadence` — 8192 ÷ 64 — *"one Day, counted in the units the decay actually runs in"*, so the designer
sentence is *"a shut-down factory's plume fades over about a Day"* and **the number moves correctly on
its own if either constant it is built from ever changes**.

Now make the cadence hot-reloadable, which is exactly what this slice does and what `adr/0044`
requires (the cadence is hash-bearing tuning; the kernel radius is world-creation). A designer changes
the cadence from 64 to 128 in a running city. If the file lists `pollution_tau = 128` as a literal,
the derivation silently breaks and the plume now fades over **two** Days with nothing to indicate it.

The three spellings are: a literal (breaks), a derived value the loader computes (correct, but it is
a computation in the Ruleset, which is the DSL parked in `deferred.md`), or **tau expressed in Days
and converted at load** (correct, keeps the designer sentence, needs no expressions). The third is
recommended and it is decision owed 2.

**And this is where item 4 first bites for real, rather than hypothetically:** the cadence and the
kernel radius live in the same `LayerRuleset`, one is reloadable and one is world-creation-fixed, so
the same file must carry both categories at the same nesting level and the reload path must apply one
while refusing the other. `adr/0044`'s second half is the standing warning here — it filed the cadence
as world-creation-fixed *by argument*, citing `adr/0015` without running the membership test
`adr/0015` states, and was withdrawn. **Run the test per number.**

---

## Tasks

### 1. The swap, at the top of Phase 0 — and there is no new verb — **done**

**`CommandKind` gains nothing and `Command` is not widened.** *What planning found*, item 3, is the
whole argument; what follows is what gets built.

`Simulation` reads `input.RulesetHash` — a field that exists, is populated every Tick, and is read by
nothing today — at the **top of Phase 0**, before `ApplyInput` walks the commands. When it differs
from what is in force, the swap and the degradation (tasks 4–6) run there, and the commands in the
same Tick are then applied under the **new** Ruleset. That ordering is a choice and it is decision
owed 1: the alternative is to apply the Tick's commands under the old Rules and swap after, and the
argument for swapping first is that a Tick has one Ruleset, which is what `RulesetHashAt(tick)`'s
signature already promises.

**The door argument is satisfied and not weakened.** `TickInput` *is* Phase 0's input. Nothing reaches
in from outside a Tick, the transition is in the log, and a replay reproduces it by construction —
which is the whole of what S0a's finding 1 demanded of `Populate`.

`Core` cannot turn a hash into Rules, so the Rulesets a session references are supplied to it up
front. `RulesetInForce` (built, unreferenced) is the holder for the one currently live.

#### What building it changed — **done**

**Decision owed 1 is settled as recommended: the swap runs first, and a Tick has exactly one
Ruleset.** It is asserted rather than merely written down —
`The_commands_in_the_reloading_tick_run_under_the_new_rules` observes it through Bin capacity, which
is the one Ruleset number a command's effect reads immediately, so the two orderings are
distinguishable rather than a matter of taste.

**Task 1 shipped a narrower swap than the task describes, and the narrowing is the finding.** A
reload today may move **numbers** and may not move **structure**, and the line between them is not the
obvious one. `RulesetShape.Compare` decides each field by asking **what live state points at it**: a
Bin row holds a Resource id, a Rule Instance holds a Rule id, a Building holds a kind, so those are
structure — and a rate, a capacity, a quantity, an apply band and a condemnation threshold are only
ever read *through* a row, so they are numbers. That test is what puts `ApplyCount.IsDerived` on the
**safe** side despite the engine branching on it, and the Bin's Resource on the unsafe side despite
the capacity beside it being free to move.

**Which is exactly the width `adr/0015`'s acceptance test asks for.** Its one sentence is *changing a
production ratio and seeing the effect must take seconds*, and a production ratio is a number. So
task 1 is not a partial implementation of reload — it is the whole of the case the ADR names, with
the migration case refused by name until tasks 4–6 build the degradations. The refusal is `adr/0015`'s
own polarity: its revisit trigger names **silently ignoring** as the failure mode.

**Two refusals turned out to be load-bearing and neither was in the plan.**

- **A transition this session cannot resolve throws.** `Core` cannot turn a hash into Rules, so a
  session is handed a `RulesetCatalogue` up front — which makes *"`--ruleset PATH` names one file and
  a session that reloaded twice was played against three"* (`InputLog.cs:131`) a **refusal** rather
  than a divergence. An empty catalogue is `RulesetCatalogue.None` rather than a null, precisely so
  that a run meeting a transition with no Rules to swap to is told so.
- **A catalogue whose opening entry is not the Ruleset the `World` holds is refused at
  construction.** Otherwise the first reload swaps *away* from Rules the city had never been running —
  a divergence with no symptom, because both Rulesets load and both run.

**The safe half of the shape test is the half that would rot, and it is tested by name.** A missing
refusal is caught the first time somebody reloads a real migration and the world corrupts. A refusal
that is too **broad** has no symptom at all: a designer is told a tuning change is a migration, and
nobody ever finds out it was not. So every field that may move has a case in `RulesetShapeTests`
saying so, and that file is the list to re-run when the Ruleset grows a field.

**Nothing existing moved.** `RulesetCatalogue.None` is the default, so every world built before today
behaves exactly as it did and no baseline was touched. 27 new tests; 710 total.

### 2. The transition in the Input Log, and filling in `RulesetHashAt` — **done**

`InputLog.RulesetHashAt(Ticks)` stops discarding its argument. The log gains a transition list —
`(tick, from hash, to hash)` — and `RulesetHashAt` answers from it.

**The format changes and the stub's own remark says so**, having corrected an earlier claim that it
would not. Every caller that supplies a Ruleset changes with it, and the sharpest statement of the
problem is already written at `InputLog.cs:131`: ***"`--ruleset PATH` names one file and a session that
reloaded twice was played against three."*** That is task 9's problem as much as this one's.

The codec (`InputLogCodec.cs:78` writes `ruleset 0x…` as a single header line) gains the transitions.
`CrashArtifact` continues to carry **content**, per `05 §7` and the remark at `InputLog.cs:136` — and
an artifact from a session that reloaded twice carries three Rulesets, which is a size question worth
a glance and probably not worth an answer.

#### What building it changed — **done**

**`RulesetHashAt` cost one method body and zero call sites**, which is what the slice-5 stub bought.
`Replay.Trace` has called it every Tick since it was written and now drives reloads by doing so, with
nothing in it changed. *A stub with the right shape is worth writing before it can be implemented*
has been asserted around here for three slices; this is the first time it has been collected on.

**The format version did **not** move, and the rule that said it should is the finding.** The codec's
stated rule was *bump whenever a field is added to `Command`, to `WorldConfiguration` or to the
header*, and `reload` lines are header lines. But the rule is a **proxy** for the property that
matters — ***bump when an old reader would misread a new log*** — and the proxy has now been wrong
twice in the same direction. A new verb did not bump it, on the argument that an old reader refuses
`populate` by name; a `reload` line is refused by name too (*expected `--` between the header and the
commands*), and a new reader meeting an old log finds no reloads and runs one Ruleset throughout.
Neither direction misreads. So the comment now states the property and keeps the proxy as an
example — and records what *would* bump it: a change to a line that already exists, which an old
reader parses happily and gets wrong.

**The cost of getting that wrong was concrete**: a bump would have invalidated every log ever
written, the committed golden baseline included, to answer a question no reader was going to get
wrong. A test writes an old log out **literally** rather than round-tripping it, because a round trip
through today's writer proves only that this build agrees with itself.

**The `from` hash is redundant and is written anyway, and the refusal it enables is the reason.** The
`to` hashes alone reproduce a session. Carrying `from` means the parser can verify the chain, so a log
that has been hand-edited, truncated or **spliced from two sessions** is caught at parse time with a
line number — rather than parsing perfectly and replaying to a city neither session ever contained.
The builder *derives* `from` rather than taking it, so an inconsistent chain is unauthorable and the
check is one every honest writer passes by construction.

**Three builder refusals, and each is a claim about what a log may say happened.** A reload before the
previous one (append-only). **Two reloads on one Tick** — a Tick has exactly one Ruleset, which is the
log's half of task 1's swap-then-commands ordering. And **a reload on Tick 0**, which could never have
taken effect, because the opening Ruleset is the header's and the first Tick *establishes* rather than
swaps. A fourth is arguably the most useful: **loading the Ruleset already in force is refused**,
because a designer saving the same file twice is ordinary and recording it would make the reload count
report keystrokes rather than tuning.

**`Replay.Start` gained a catalogue overload, and the one-Ruleset form got strictly better.** It now
builds a catalogue of one, so a log carrying a transition it cannot resolve is **refused** — where
before slice 8 the same log replayed silently under its opening Ruleset and diverged, which is
arithmetic rather than a bug and indistinguishable from one.

**`InputLog.RulesetHash` quietly changed meaning** from *the* Ruleset to the **opening** one, and
`CrashArtifact.RulesetHash` inherits the consequence: a session that reloaded crashed under a later
Ruleset, so the artifact's hash is the one at the **panic Tick**. Both doc comments now say so. The
artifact needed no other change — it embeds the log through the codec, so reloads travel for free.

15 new tests; 725 total. No baseline moved.

### 3. The world-creation refusal, the loader's second entry point, and the `const` that has to move first — **done**

**Two halves, and the second is a prerequisite of the first.** *What planning found*, item 5: the
industrial pollution kernel radius is `const int IndustrialPollutionMetres = 1_024` at
`SeparableKernel.cs:177`, and a refusal cannot detect a change to a number that lives in the binary.
So the radius moves into the Ruleset file and is read at world creation, and only then does the
refusal have something to refuse. Moving it does **not** ratify it — the 1–10 km band is still 10×
wide and the `0002` §D row stays open.

`RulesetLoader` then gains an entry point that validates a file **against a live world's frozen
constants** and refuses a change to any member of `adr/0015`'s enumerated category: `TICKS_PER_DAY`,
`WHEEL_SIZE`, the Cell, and the kernel radius.

**Refused, not warned about, and not silently ignored** — `adr/0015`'s wording is *"refused rather than
applied"*, and its own revisit trigger names silent ignoring as the failure mode.

`adr/0048`'s refusal count is updated **in all three places at once** — the ADR, `adr/0015`, and
`0003`'s gate board. They have drifted apart before and `plans/0012` diagnoses why.

*The membership test is run per number rather than inherited* — `adr/0044`'s withdrawn second half is
what that instruction is made of.

#### What building it changed

**It took task 8's loader half with it.** The task as written moves one number; what shipped is the
whole `[layers]` table — `pollution_period`, `pollution_offset`, `pollution_decay_ticks`,
`land_value_period`, `land_value_offset`, `land_value_tau`, `sealing_decay_tau`, `kernel_metres` —
every key optional and defaulting to `LayerRuleset.Default`'s documented value, so a Ruleset written
before the section existed is still a complete Ruleset. Doing one number would have meant writing the
section, the reader and the refusal machinery for a single key and then writing them again. What is
left of task 8 is what it was always really about: **a Ruleset that actually states a cadence, and the
baselines that re-record when it does.**

**`LayerRuleset` stopped being a constructor argument of `World` and became a member of `Ruleset`, and
that was not on the list.** Every three-argument call site in the tree passed `LayerRuleset.Default`
beside a Ruleset that carried its own Layer data — so the shape admitted a world whose cadence
disagreed with the Rules in force, and the **first reload would have silently reverted it to whatever
the file said**. That is the exact failure class `adr/0015`'s revisit trigger names, arriving through
the door nobody was watching. `World(int, Ruleset)` replaces `World(int, LayerRuleset, Ruleset)`;
`Ruleset.WithLayers` is what a caller holding only a cadence uses, which keeps `adr/0044`'s
two-cadence measurement door open and gives it one source instead of two.

**The comparison is on Cells, not on metres, in both places that make it.** The radius is authored in
metres and used in Cells; 1,000 m and 1,024 m are both 8 Cells and build the same array, so refusing
between them would refuse a reload that reinterprets nothing. Comparing in the units the state was
recorded in *is* `adr/0015`'s membership test rather than an approximation of it — and it is what lets
`MapLayers.PollutionKernel` stay the one built at world creation, because an accepted reload provably
cannot move it.

**Two surfaces, and they are not redundant.** The loader's refusal is where a designer gets a file, a
line and a sentence; `MapLayers.Adopt` throws, and it is the backstop for a caller that never asked the
loader — a shell building a Ruleset in code reaches the swap without passing `Formats` at all. The
failure it prevents is silent rather than loud: every Cell not re-diffused would be read at the wrong
scale, producing a plausible field that is simply wrong.

**Three new refusals at load, sharing slice 10's symptom.** A **period of zero** freezes the field; an
**offset outside its period** makes `tick % period == offset` unsatisfiable so the Layer is never
recomputed; a **decay that rounds to never** inverts its own meaning. Each is a Ruleset that loads
clean, runs on schedule for ever and does nothing — which is what the count of record is a count of,
and the ADR now says so, because that is the line it would drift across next.

#### The finding that outlives the task

**`adr/0015`'s world-creation enumeration is one-quarter implemented, and nobody had checked.** The ADR
says its members *"live in the Ruleset like everything else and are read from it"*. After this task
that is true of the kernel radius. `TICKS_PER_DAY` had **no symbol at all** — it existed as prose in
three documents and as a bare `8192` in one populator, and task 3 had to name it (`Ticks.PerDay`)
before the tau derivation could cite it. `WHEEL_SIZE` is `EventWheel.Size`; the Cell is
`CellGrid.TilesPerCell`. So the sentence is false of **three of its four members**, and was false of
all four an hour ago.

**It is not the same defect the kernel radius was**, which is why it is filed rather than fixed. The
radius was tuning frozen per world that had simply never been offered to a designer. These three are
numbers the corpus argues a designer should **not** be handed — `adr/0019` is an entire ADR on
`TICKS_PER_DAY` not being a pacing knob, `CLAUDE.md` calls the Cell a *design constant, never tuned*.
So the correction owed is to the ADR's sentence: either the category admits a member that is Ruleset
data in principle and not in the file, or those three belong to the revisit trigger's *"a parameter
that genuinely cannot be data"* exception, which `adr/0015` already provides and which asks for a
written exception each. Nothing is unguarded meanwhile — **a file that cannot state a number cannot
change it**. `plans/0012`.

25 new tests; 750 total. No baseline moved, because `rulesets/minimal.toml` declares no `[layers]` and
the defaults did not change — the tau is now derived and derives to the 128 it was.

### 4. The degradation, whole — derelict, dropped Bins, wait lists, and identity across two files

> **Tasks 4, 5 and 6 were merged before any of them was written, and the merge is a finding rather
> than a tidy-up.** None of the three is reachable on its own. Dereliction requires a kind to stop
> being declared, which drops `KindCount`; removing a kind also removes the Rules declared on it, so
> `RuleCount` drops too, and usually a `ResourceCount` with it. `Simulation.Reload` refuses **any**
> structural change, and that refusal lifts once or the reload half-happens — which is the single
> thing `adr/0015` forbids. Three task numbers described one task.

**Sub-task A — identity. Shipped.** *An id is not an identity, and the comparison had assumed it was
for two tasks.* A `ResourceId` is declaration order, so deleting one `[[resource]]` shifts every id
below it — and a live Bin row holds the id, not the name. `RulesetShape.Compare` therefore called a
**reordering** of two same-shaped declarations *numbers only*, admitted the reload, and every live Bin
silently began naming the other Good. **This is not an exotic case: removing a declaration from the
middle of a file *is* a reordering, so every real removal the degradations exist to handle has it.**

`Ruleset` now carries a **key per declaration** — the content hash of its declared name, computed by
the loader. A number, so it crosses into `Core` under `adr/0048`'s own rule where the name it came
from does not; the core never renders it and never resolves a name with it, it only asks whether two
declarations are the same thing. `Compare` checks the key **before** anything else about that id,
because *a comparison of two declarations that are not the same declaration produces true sentences
about the wrong pair* — the swapped-kinds test would otherwise have reported `KindBins`. Rulesets
built in code default to **positional** keys, so every fixture in the suite compares exactly as it
did. There is no rename: a name is the only identity a TOML file carries, so *flour became grain* and
*flour went and grain arrived* are the same file, and guessing between them is what an explicit id
field would be for.

**Sub-task B — the map and the rewrite. Not started.** The remap runs at the swap, when both Rulesets
are in hand, so **no row needs a new column**: `Bins.Resource` and `Buildings.Kind` are rewritten from
old id to new id through the key. That is the whole reason the keys are on the `Ruleset` and not on
the rows.

**Sub-task C — the three degradations, in one pass over the world.** Order matters and it is
`DestroyBuilding`'s order for the same reason:

1. **Every Rule Instance in the world is dropped** — unlinked from the Wheel and from whatever wait
   list it is on, and freed. `02 §4.3`: *"all wait lists are dropped and every Rule is woken with a
   stagger"*, because a subscription taken under the old Ruleset may name a Bin the new one does not
   have — *"which also means a wait list is never cross-version state."*
2. **Bins are remapped, and those whose Resource is gone are dropped**, with a count reported.
3. **`Buildings.Kind` is remapped**; a kind that is gone leaves **0**, which `Ruleset.Declares`
   already answers false for. That is dereliction, and it needs no flag — see below.
4. **Every Building with a declared kind is refitted**: the Bins its kind declares that it does not
   have, and its kind's Rule Instances, armed on slice 7's stagger. That is `World.CreateBuilding`'s
   arming logic factored out and called again — the same code, not a second copy of it.

**This task has no number to choose, and that is worth stating loudly**, because it is the second
time the shape has appeared. Slice 7 task 10a expected the arming stagger to be a hash-bearing number
needing a ratifier under `adr/0052` and found there was nothing to choose: a Rule re-arms at `+rate`
for ever, so uniform over `[1, rate]` is the only offset that stays spread. A reload re-arms every
Rule Instance in the world at once, which is the largest possible instance of exactly that problem,
and **the answer slice 7 derived is already the right one.** Reuse it; do not re-derive it; do not add
a reload-specific stagger parameter.

#### Derelict is derived, and the question that settled it was *when does this happen to a player*

**Never.** A shipped game ships one Ruleset; nothing removes a Building kind from under a running
city. The Ruleset changes mid-city in exactly two situations, and one of them does not exist yet: a
**designer balancing**, which is `adr/0015`'s whole reason for being, and a **save made under one
Ruleset loaded under another**, which is `05 §7`'s cross-Ruleset load policy and milestone 10.

So the only actor is a designer, and a designer's commonest move is **undo** — remove `bakery`, watch
for five hundred Ticks, put it back. That kills the saved flag this task was planned around. A mark
that records *a reload removed your kind* and is never cleared leaves that designer with a city of
permanently inert bakeries and no fix but a restart, which is the failure `adr/0015` exists to
prevent, arriving through the mechanism written to serve it.

**So there is no `derelict` column.** Dereliction is `Kind == 0` — a Building the Ruleset in force
cannot describe — and it is recovered for free by sub-task C's refit, because a kind that comes back
brings its Rules with it. `plans/0015`'s own *"Saved and hashed — it is simulation state"* is
**struck**: under the reading that makes it saved, it is a cache of a two-compare predicate that is
also wrong after the one edit a designer makes most.

**The one thing that genuinely does not come back is a kind removed and re-added.** `Kind` is set to
0, so what the Building *was* is forgotten and the re-add does not restore it. That is stated rather
than fixed: remembering it costs a `ulong` key per Building row for a case that recovers by reloading
a save, and inventing a refit for it would be inventing a mechanism nobody has asked for.
`HONEST DEGRADATION`.

#### `adr/0055`'s consequence bullet is false, and the repair is forbidden

`adr/0055` says a derelict Building is *"still sampled and still dies of its own failures, rather than
becoming a permanent monument to a Ruleset edit"*. The mechanism it names **cannot fire**: a Building
with no Rules has no failures, so `ZoneRuleEngine.Condemn`'s threshold walk finds nothing and returns.
And the obvious repair — condemn a derelict Building on sight — is *silent deletion arriving through
the Zone Rule instead of through the reload*, which is precisely what `adr/0015` forbids and why the
state is called derelict rather than removed.

**So `adr/0015` wins, the Building stands, and clearing it is the player's** (`PLAYER GOVERNS`).
Filed to `plans/0012` as a correction owed to `adr/0055`, not to the code.

#### What is left, in order

- Sub-task B, the migration map: `oldResourceId → newResourceId` and `oldKind → newKind` by key, 0 for gone.
- Sub-task C, the pass over the world, inside `World.Adopt` — which needs the Tick and the `WorldKey`
  for the stagger, so `Adopt` grows both.
- **The residual refusal.** `RulesetChange.ResourceFamily` stays refused: a Good becoming Money with
  live Bins holding stock is `adr/0024` conservation, not a degradation. `ResourceIdentity` and
  `KindIdentity` become *inputs to the map* rather than refusals once B exists.
- **The logged warning.** `02 §4.3` and `adr/0015` both say dropped *with a logged warning*, and
  `Core` returns numbers (`adr/0002`). So: counts per reload — Buildings derelicted, Bins dropped,
  Rule Instances re-armed — which is also the shape task 7's provenance trail wants.
- Invariants: **a derelict Building runs no Rule Instances**, which is the claim sub-task C is most
  likely to break, because nothing in the shape of a re-arm loop makes the exclusion obvious.
- **Baselines re-record**, deliberately: the refit changes when Rule Instances are armed.

### 7. The provenance trail, and `adr/0006`'s sink

`05 §7` promotes degradation from a logged warning to **state**: *"at Ruleset `a91f…`: 412 `coal` Bins
dropped, 3 Buildings derelicted."* The reason is a class of bug no replay can reach — a defect *caused*
by a degradation three patches ago and surfacing now, upstream of every snapshot anyone holds.

**It is a collection, so `adr/0006` applies, and `05 §7` already wrote the sink**: it grows with
patches survived rather than with elapsed time, and it caps at the **last N transitions** with older
entries aggregated to counts. Build the cap with the collection, not after it — this project's
standing rule, and the one slice 6 broke.

**N is a saved, hash-bearing number**, so under [`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
it is written into `0002` §D **on the day it is chosen**, with a named ratifier and a trigger. A
category is not a name. Decision owed 3.

It is `Evidence`-shaped because it names constituents, and it is `Core` state, so it holds **ids and
counts and never a string** — the shell resolves the names. That is the constraint that decides its
layout rather than a stylistic preference.

### 8. The Map Layer cadence and rates from a file — the checkable obligation

**This is what "slice 8 is done" means**, per the gate board, and it is the one task with an external
definition of completion.

> **Task 3 took the loader half.** `[layers]` exists, every key is read, the derivation is in the code
> and the world-creation refusal runs on both surfaces. Doing one key's worth of section-reading and
> refusal machinery and then writing it again for the other seven was not a saving. **What is left is
> the half that was always the point**: a Ruleset that actually *states* a cadence rather than
> accepting the default, the hash trace that moves when it does, and the baselines re-recorded against
> it. Read the rest of this task as that.

`LayerRuleset.Default` stops being the source. The cadence, the offsets and the rates come from the
Ruleset file; the world-creation members are read at creation and **refused on reload** by task 3.

Two things make this larger than plumbing, both in *What planning found* items 5 and 6: `PollutionTau` is a
**derivation** and must stay one (decision owed 2), and the same structure carries both a reloadable
number and a world-creation-fixed one.

**It is hash-bearing and it re-records every baseline** — `adr/0044` established that two worlds
differing only in the diffusion period produce different hash traces, which is the whole reason the
cadence is the designer's number. Coordinate with slice 10, which is also re-recording.

### 9. The runner, and `adr/0015`'s acceptance test

`adr/0015`'s own consequence: ***"The headless runner is the real iteration loop."*** There is no Godot
shell, so **the acceptance test cannot be run at all without this task** — it is not a convenience
flag.

The runner needs to accept **more than one Ruleset**, because `InputLog.cs:131` already states the
problem: *"`--ruleset PATH` names one file and a session that reloaded twice was played against
three."* A replay resolves each transition's hash against the Rulesets it was given, and **refuses on
an unaccounted mismatch** rather than diverging — `05 §7`'s policy, and the mechanism already exists
(`--force-ruleset` stamps a trace hash-broken, `Session.cs:45`).

The seconds test, stated so it can fail: **change a production ratio in `rulesets/minimal.toml`,
reload into a running session, and see the Bin levels move — in seconds, without a rebuild.**

### 10. The golden session reloads

The committed trace gains a reload, so replay equivalence covers a session with a transition in it
rather than only sessions with one Ruleset. A second Ruleset file is needed and it should be
`minimal.toml` with **one number changed** — the smallest content that makes the transition observable,
and the same instinct that produced `minimal.toml` in the first place.

---

## Acceptance

- `dotnet build` clean, `dotnet test` green, no GPU and no Godot.
- **Replay equivalence across a transition**: a log containing a reload replays to an identical hash
  sequence. This is the slice's central claim and everything else is in service of it.
- **A refused reload changes nothing** — same State Hash before and after, the previous Ruleset still
  in force, and **nothing written to the log** (*What planning found*, item 2).
- ✅ **The industrial pollution kernel radius is read from a file**, not from
  `SeparableKernel.IndustrialPollutionMetres` — which is the half of task 3 that has to land before the
  next line can be tested at all. It is `[layers] kernel_metres`; the `const` is gone.
- ✅ **A world-creation change is refused**, by name, naming the constant — on the loader's surface
  with a file and a line, and in the core as the backstop.
- **A reload that removes a kind derelicts rather than deletes**, and the Building is still there.
- **A reload that removes a resource drops the Bins**, and no wait list survives the transition.
- **The Layer cadence comes from a file**, and a Ruleset with a different cadence produces a different
  hash trace — which is `adr/0044`'s measurement re-run through the file rather than through a
  constructor argument.
- **The provenance trail is capped**, demonstrated by a long-run test with more transitions than the
  cap, and it does not trend.
- **The seconds test**, run by a person and recorded in this document as a number.
- Baselines re-recorded, deliberately, with the change stated.

---

## Decisions owed, found while planning

**~~1. Whether the Tick's commands run under the old Rules or the new ones.~~ SETTLED — task 1, as
recommended: swap first.** *Which* boundary was never a question — `02 §4.3` says *"a phase
boundary"*, and Phase 0 is the only one that is also the door. What was open was the ordering
**within** Phase 0, and the swap runs before the commands, so a Tick has exactly one Ruleset — which
is what `RulesetHashAt(tick)`'s signature already promised, and a promise in a signature is cheaper
to keep than to explain away. **It is asserted rather than recorded**: a Building raised by a command
in the reloading Tick carries the *new* Ruleset's Bin capacity, so the two orderings are
distinguishable by a test rather than only by argument.

*(The question this replaces — whether a reload needs a new `CommandKind` — was retired during
planning rather than filed. See *What planning found*, item 3.)*

**~~2. How `PollutionTau` survives a reloadable cadence.~~ SETTLED — task 3: a duration in the file,
divided at load.** The recommendation was *Days*; the answer is **Ticks**, and the difference is the
only part worth recording. Days is closer to the designer's sentence — *a plume fades over about a
Day* — but it is a unit nothing else in a Ruleset uses, and any value under a Day would have needed
the quoted-decimal machinery to express. Ticks is what every `rate` and `interval` in the file is
already written in, and `8192` carries the comment `one Day` perfectly well. The file states
`pollution_decay_ticks`; `LayerRates.From` divides it by whatever cadence is in force. **It generated
a refusal that the literal spelling could not have had**: a duration shorter than the period it is
counted in rounds to a tau of 0, and 0 is the sentinel for *never* — so `pollution_decay_ticks = 30`
would read as *fades in seconds* and behave as *never fades*. Refused. **And it removed a literal from
the source**: `LayerRates.Default` is now the same derivation run on the default cadence, so `128`
appears nowhere, and `TICKS_PER_DAY` got a name (`Ticks.PerDay`) because a derivation cannot cite a
number that has none.

**3. `N`, the provenance trail's cap.** Saved and hash-bearing, so `adr/0052` applies and a named
ratifier is required on the day it is chosen. Measurable in principle — the refuting number is how far
back a real diagnosis had to reach — but nothing can produce that number until patches exist, so the
honest handling is a provisional value filed in `0002` §D as unratified, with *the first real
cross-patch diagnosis* as its ratifier.

**~~4. Whether the derelict flag is a boolean or a Building state.~~ SETTLED — task 4: neither, because
there is no flag.** The question presupposed a saved mark and the presupposition is what failed. Asked
*when does a kind stop being declared under a running city*, the answer is **never, to a player** — a
shipped game ships one Ruleset. The only actor is a **designer balancing**, and a designer's commonest
move is **undo**: remove a kind, watch, put it back. A mark that records the removal and never clears
leaves them a city of permanently inert Buildings, which is the failure `adr/0015` exists to prevent
arriving through the mechanism written to serve it. So dereliction is **derived** — `Kind == 0`, a
Building the Ruleset cannot describe — and the refit recovers it for free. **The instruction not to
decide it on one instance is what produced this**: looking at the second instance, slice 10's deferred
construction time, showed that one is not a state either — it is a completion Tick, so *under
construction* is `now < CompletesAt`, the same shape `adr/0053` gave failure pressure. With both gone
there was no enum to be the first member of, and then no flag to be a boolean.

---

## Owed to other documents, not questions

Corrections, per [`0012`](0012-corpus-audit.md)'s distinction: nobody has to decide anything, somebody
has to type.

- **`02 §4.3`'s reload sentence argues against itself.** *"A transition carrying both Ruleset content
  hashes… a replay needs the Rules' content, not the news that they changed."* A hash is the news. The
  design is `05 §7`'s content-addressed sidecar and it is correct; the sentence describing it is not.
  **Third instance in this section** of the shape session A found twice.
- ~~**`adr/0048`'s refusal count**, once task 3 lands — in the ADR, in `adr/0015`, and in `0003`'s gate
  board, all at once. They have drifted before.~~ **PAID by task 3**, in all three at once: **eleven**
  at load and a **twelfth** on the reload entry point. The ADR now also states what is *not* counted —
  duplicate names, unknown sections — because that is the line the number would drift across next: the
  count is of refusals that catch a Ruleset which would otherwise **load clean and misbehave in
  silence**, and everything in it has that in common.
- **`CONTEXT.md` → Ruleset** gains reload's transition semantics if the walk-through finds it silent
  on them. **`CONTEXT.md` is also silent on *Derelict***, which the corpus uses in four places and
  defines nowhere; it wants the entry, and the entry should say **derived** so nobody adds the column
  again.
- **`adr/0055`'s consequence bullet** — *a derelict Building "still dies of its own failures"* — is
  **false**, because a Building with no Rules has no failures. Filed to `0012`. It is a correction to
  one bullet and not to the decision the ADR makes.

---

## What this slice deliberately does not do

- **No save format.** `05 §7`'s save-side provenance trail, the hash-broken mark propagating to
  descendant saves, and the cross-Ruleset *load* policy all need a save, which is milestone 10. This
  slice builds the trail as **state** and the mark on a **trace**; persisting either is not available.
- **No content-addressed sidecar.** `05 §7` specifies it as part of a replay *bundle*. Resolving
  several Rulesets on the command line (task 9) is what Phase 1 needs, and a sidecar is a packaging
  format with no consumer yet.
- **No incremental reload of changed files.** `adr/0015` names it as the answer *if* reload cost
  becomes visible in the Tick. Nothing has measured that it has, and `adr/0043` forbids assuming it.
- **No `pool`, no chain content.** Slice 7's task 10b is re-filed to Phase 2 and this slice does not
  reach past it. The second Ruleset in task 10 is `minimal.toml` with one number changed.
