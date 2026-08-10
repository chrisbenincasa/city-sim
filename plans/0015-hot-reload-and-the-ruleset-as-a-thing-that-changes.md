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

**Task 1 shipped.** The gate is clear and slice 10 has closed, so *The parallel session*'s scheduling
constraint is discharged.

- **Task 1** — the swap, at the top of Phase 0. **Decision owed 1 is settled as recommended** and
  asserted rather than assumed: the swap runs first, so a Tick has exactly one Ruleset. **It shipped
  narrower than the task describes, and the narrowing is the finding**: a reload may move **numbers**
  and may not move **structure**, where the deciding test is *what live state points at it* — which
  is precisely the width `adr/0015`'s acceptance test asks for, since a production ratio is a number.
  A structural reload is **refused by name** until tasks 4–6, and two refusals nobody planned turned
  out to be load-bearing: a transition this session cannot resolve, and a catalogue that does not open
  with the world's own Ruleset.
- **Next: task 2** — the transition in the Input Log, and `RulesetHashAt` stopping discarding its
  argument. Everything task 1 built is driven directly through `TickInput` today; task 2 is what lets
  a *log* express a reload, and it changes the log format.

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

### 2. The transition in the Input Log, and filling in `RulesetHashAt`

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

### 3. The world-creation refusal, the loader's second entry point, and the `const` that has to move first

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

### 4. Degradation, part one — the derelict flag

`BuildingTable` gains the state `02 §4.3` has described for six slices: **a Building whose kind the new
Ruleset does not declare is marked derelict, not deleted.** Saved and hashed — it is simulation state,
and `05 §4`'s test is met trivially.

**The read side already exists.** `Ruleset.Declares(kind)` was written in task 10a precisely so that
this state could be *represented* before it could be *named* (`0011` finding 39), and the situation it
was written for is not hypothetical: **every ruleless world builds kind-1 Buildings**, so Phase 1
already contains Buildings of an undeclared kind today. This task retro-fits a name onto a state the
project already has, and only then extends it to reload.

**Silent deletion during a balance session is how a save gets quietly corrupted** — that is
`adr/0015`'s reason and it is worth quoting in the code, because the cheap fix will look tempting
every time somebody reads this path.

*Jointly owed with slice 10.* See *The parallel session*.

### 5. Degradation, part two — Bins whose resource no longer exists

Dropped, with a logged warning. `02 §4.3` and `adr/0015` agree and neither is ambiguous.

The care needed is that a Bin is not free-standing: it belongs to a Building and it may be named by a
Rule Instance and by a wait list. Task 6 handles the last of those by construction.

### 6. Degradation, part three — wait lists dropped, every Rule re-armed with slice 7's stagger

`02 §4.3`: ***"All wait lists are dropped and every Rule is woken with a stagger"***, because a
subscription taken under the old Ruleset may name a Bin the new one does not have — *"which also means
a wait list is never cross-version state."*

**This task has no number to choose, and that is worth stating loudly**, because it is the second time
this shape has appeared. Task 10a expected the arming stagger to be a hash-bearing number needing a
ratifier under `adr/0052` and found **there was nothing to choose**: a Rule re-arms at `+rate` for
ever, so uniform over `[1, rate]` is the only offset that stays spread. A reload re-arms every Rule
Instance in the world at once, which is the largest possible instance of exactly that problem, and
**the answer slice 7 derived is already the right one.** Reuse it; do not re-derive it; do not add a
reload-specific stagger parameter.

A Building's Rule Instances are its kind's, so where a kind's Rules changed, the instances are rebuilt
rather than patched. That is `World.CreateBuilding`'s arming logic factored out and called again — the
same code, not a second copy of it.

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
- **The industrial pollution kernel radius is read from a file**, not from
  `SeparableKernel.IndustrialPollutionMetres` — which is the half of task 3 that has to land before the
  next line can be tested at all.
- **A world-creation change is refused**, by name, naming the constant.
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

**2. How `PollutionTau` survives a reloadable cadence.** Literal, computed, or **expressed in Days and
converted at load** (recommended). *What planning found*, item 6. Arguable, and it should be settled
before task 8 rather than during it.

**3. `N`, the provenance trail's cap.** Saved and hash-bearing, so `adr/0052` applies and a named
ratifier is required on the day it is chosen. Measurable in principle — the refuting number is how far
back a real diagnosis had to reach — but nothing can produce that number until patches exist, so the
honest handling is a provisional value filed in `0002` §D as unratified, with *the first real
cross-patch diagnosis* as its ratifier.

**4. Whether the derelict flag is a boolean or a Building state.** Slice 10 wants decline and
construction time and `0014` explicitly defers both for want of *"a Building state that does not
exist"*. A boolean is right for this slice and probably wrong for the next one. **Do not decide it on
one instance** — the same instruction `0014`'s decision owed 4 gives itself.

---

## Owed to other documents, not questions

Corrections, per [`0012`](0012-corpus-audit.md)'s distinction: nobody has to decide anything, somebody
has to type.

- **`02 §4.3`'s reload sentence argues against itself.** *"A transition carrying both Ruleset content
  hashes… a replay needs the Rules' content, not the news that they changed."* A hash is the news. The
  design is `05 §7`'s content-addressed sidecar and it is correct; the sentence describing it is not.
  **Third instance in this section** of the shape session A found twice.
- **`adr/0048`'s refusal count**, once task 3 lands — in the ADR, in `adr/0015`, and in `0003`'s gate
  board, all at once. They have drifted before.
- **`CONTEXT.md` → Ruleset** gains reload's transition semantics if the walk-through finds it silent
  on them.

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
