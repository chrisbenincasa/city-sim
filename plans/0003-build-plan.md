# 0003 — The build plan

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Phase intent and risk framing in
> [`docs/06-roadmap.md`](../docs/06-roadmap.md). Readiness derivation in
> [`plans/0002-open-questions.md` §Readiness](0002-open-questions.md).
>
> **This document supersedes [`06-roadmap.md`](../docs/06-roadmap.md)'s ordering for Phase 0 and
> Phase 1 only.** The roadmap's phases, risk fields and the argument for each remain authoritative;
> what is re-derived here is the *order*, because the readiness review of session eight moved tables
> ahead of the hash and pulled three items out of Phase 1's milestones that were never milestones —
> the scaffolding, the arithmetic substrate, and the analysers. ~~Phase 2 and Phase 3 in the roadmap
> are untouched and are not planned here.~~
>
> **⚠ AMENDED 2026-08-22 — this document now holds Phase 2's *status*, and still not its order.**
> `06` keeps Phase 2's sequence and the risk each milestone retires; what arrives here is the
> per-milestone **gate, plan pointer and state**, which `06`'s own header forbids it to hold and
> [`0000`](0000-board.md)'s own rules forbid it to be a source of. It had been living on the board
> for want of anywhere else. Phase 3 is untouched and is still not planned here.

---

## What this document is

`06-roadmap.md` sequences **milestones**, each named by the risk it retires. That is the right unit
for deciding what to build next and the wrong unit for sitting down to build it: a milestone names an
outcome, not a task list, and three of Phase 1's five have prerequisites that are not milestones at
all. This document is the layer underneath — an ordered ledger of **slices**, each of which is a
thing you can start on a Tuesday evening and finish, with a per-slice plan document holding the
actual tasks.

The unit here is deliberately smaller than a milestone and deliberately larger than a task. A slice
is *the smallest amount of work that leaves the build green and retires something*.

**Two ledgers, two axes.** Phase 0 and Phase 1 are keyed by **slice**, which is this document's own
re-derived order. Phase 2 is keyed by **milestone**, because it has run one plan document per
milestone throughout and never used the slice axis at all. Both tables answer the same question —
***what is done, what gates it, and which document holds the record*** — and neither restates `06`'s
sequence or its risk fields.

### The rules this plan inherits

The four from [`06 §How this roadmap works`](../docs/06-roadmap.md), unchanged and binding:

1. **No dates. Pure dependency ordering.**
2. **Every slice leaves the project in a working, runnable state.**
3. **Slices are sized for one or two sittings wherever possible.**
4. **Every slice names the specific risk it retires.**

### Two rules this plan adds

Both come out of session eight's findings and both exist because the corpus has already been bitten
by their absence.

5. **Every slice names the design gate it needs cleared, and refuses to start until it is.** The
   readiness review established that most of what is ungrilled gates nothing you would build first —
   but three Phase 1 milestones *are* gated, and starting one anyway is how a task list gets written
   against a decision that then changes. The gate board is below.

6. **Every slice ends by recording the numbers it chose that nobody has ratified.** *An unratified
   number is more dangerous than an open question* is this corpus's own finding, arrived at after a
   figure nobody decided silently sized five decisions. Building will generate these faster than
   arguing did — a table resolution, a row count, a kernel radius — and each one must land in
   [`0002`](0002-open-questions.md)'s ledger as it is chosen, not after it has been repeated until
   it reads as settled.

---

## The name

**The project is `Borough`.** A self-governing town — administrative rather than architectural,
which is the reading that ties it to `PLAYER GOVERNS`: a borough is a place that runs itself, which
is the relationship the design wants the player to have with the city.

It was chosen against four filters, and the filters are worth keeping because they will apply again
to every Zone family, Good and Policy that gets named:

1. **Not a word already in [`CONTEXT.md`](../CONTEXT.md).** That file's rule is *every term, with
   exactly one meaning*, and a project named after a domain term breaks it permanently. This
   eliminated the two best candidates — `Ledger` (49 uses) and `Evidence` (89) — along with `Grid`,
   `Frontage`, `Trace`, `Grain`, `Verge`, `Provenance`, `Row`, `Block`, `Close`, `Green`, `Common`,
   `Parcel`, `Square` and `Kerb`.
2. **PascalCases into a namespace prefix** — it repeats four times in every file header in the
   project, so a plural trap or a compound is a permanent tax.
3. **No ecosystem collision.** `Fabric` was strong on merit and is dead three times over in .NET;
   `Trace` would have collided with `System.Diagnostics`; `Skyline` and `Witness` are taken.
4. **No collective noun.** `Populace`, `Citizenry`, `Multitude`, `Myriad`, `Swarm` and `Hive` all
   fail on *meaning* rather than availability: the design's central claim is that an aggregate must
   always be able to name its constituents, and [`adr/0005`](../docs/adr/0005-two-fidelity-tiers.md)
   refuses Cohorts permanently. Naming the project after a mass of undifferentiated people is naming
   it after the failure mode. The same filter, pointed at verbs, kills `Thrive` and `Flourish`
   against `NO VERDICT`.

**It is a working title, and this is where its revisit trigger lives.** Renaming is currently an
hour — a namespace refactor and a `git mv`. It stops being cheap at the **save format's magic
number** (`05 §7`), because from then on a rename either breaks every existing save or requires a
migration written for no reason but vanity. **The trigger is therefore milestone 8, and there is a
second, softer one: the first time the name is shown to somebody who is not the author.** If it is
going to change, it changes before slice 10.

⚠ **THE TRIGGER HAS FIRED, 2026-08-18, and it fired in silence four tasks before anybody looked.**
`SaveHeader` writes eight bytes of **`borosave`** at offset 0 of every save
([`plans/0030`](0030-save-load.md), task 4). Milestone 8's **D2** had *deferred* the magic number
precisely so this trigger would not fire — and then task 4 wrote one anyway, against that decision,
with four documents describing the header that was built rather than the one that was decided.
***A decision is reversed by the build far more quietly than by an argument***, and every mechanical
check in this corpus reads one document against another, so none of them can see a header.

~~**The window is still open and it is the user's call.**~~ ⚠ **The name is in a second place**, which
this section did not know when it priced the rename at an hour: `World.HashSeed` is
`0x426F_726F_7567_6802` — `"Borough"` plus a version byte — so a rename **moves every State Hash**,
which is one re-record command today and is exactly the cost
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
says not to defer work over.

## ✅ SETTLED 2026-08-18, with the user in the room — and by splitting the question

**`Borough` is the *codename*. It stays, and it is frozen.** A **public name** is a different decision
with different filters, it is **deferred**, and it has no deadline. ***The trigger is discharged rather
than met***, which is a better outcome than either answer it was asking for, because it removes the
deadline instead of meeting it.

**The line is one test: does a human who is not a developer read the string?**

| | | |
|---|---|---|
| **Codename — frozen, never changes** | the namespace prefix (793 occurrences in `src/`), the assembly and project names, `World.HashSeed`, and **`borosave` at offset 0 of a save** | none of it is legible without a hex editor or a decompiler |
| **Public — two strings, both cosmetic** | `InputLogCodec.Extension` (`.borough`) and `CrashArtifact.Extension` (`.borough-crash`) | **nothing dispatches on either**; their only use is naming a crash file the runner writes (`Session.cs:348`), and the reader sniffs the magic line *inside* the file |

**So the magic number is a codename in a file header, which is what a magic number usually is**, and the
thing this section priced the deadline against — *a rename either breaks every save or needs a migration
written for vanity* — applies to the magic number alone, which is now never renamed. The whole cost of
the split is **two `const string` lines**, changeable any day, with no format-compatibility burden at
all.

⚠ **The risk is this corpus's own and is named rather than waved at.** Two names for one project is
[`0012`](0012-corpus-audit.md) *Cause 1* by construction — two copies of one fact, and the second drifts.
What holds it: the two live in **disjoint domains** and never both describe one artefact, and this table
is the record of which is which. **Without it, in a year nobody knows whether `.borough` was a decision
or a leftover somebody forgot to rename.**

⚠ **The split changes the filters, and that is the useful half.** The four above were written for a
*codename*, and three of them are void for a public name: filter 2 (*PascalCases into a namespace
prefix*) and filter 3 (*no ecosystem collision*) are pure code concerns, and filter 1 (*not a
`CONTEXT.md` term*) exists so the codebase does not contain `Borough.Core.Ledger` inside a project called
Ledger. **Filter 4 survives** — it fails on *meaning*, and `NO VERDICT` and `adr/0005`'s refusal of
Cohorts are design pillars rather than conventions. So the two candidates this section calls the best,
`Ledger` and `Provenance`, were killed on filter 1 alone and **are back on the table** whenever the
public name is chosen.

**The remaining trigger is the soft one, unchanged**: the first time the name is shown to somebody who is
not the author. Nothing in the save format touches it.

Not recorded as an ADR, deliberately. The ADR series decides *how the city works*; a project name
decides nothing about the simulation and would dilute a series whose value is that every entry is
load-bearing. Reverse this if the name ever becomes a decision with consequences beyond a rename.

## The slice ledger

**Order is top to bottom.** *Roadmap* maps the slice back to `06`'s milestone numbering where one
exists. *Sittings* is a guess and is not a commitment; it exists to catch a slice that has silently
become three.

| # | Slice | Roadmap | Gate | Sittings | Plan |
|---|---|---|---|---|---|
| **0** | **Solution scaffolding** — four projects, build config, the three reflection guards, CI | — | none | 1 | [`dev-environment.md` Track A](../docs/dev-environment.md) |
| **1** | **S4 — the kernel benchmark** — **tasks 1–10 done, all seven kernels on two machines, no tripwire fired; task 11 unblocked, XMP re-sweep now optional** | Phase 0 | none | 2 | [`0004`](0004-s4-kernel-benchmark.md), results in [`spike-results`](../docs/spike-results.md) |
| **2** | **The arithmetic substrate** — **all seven tasks done.** Typed quantities, fixed point, tabulated `exp`/`log`, `draw()`, purpose tags. Produced `adr/0038` and an amendment to `adr/0003`'s normative hash | — | none | 3 | [`0005`](0005-arithmetic-substrate.md) |
| **3** | **The analysers** — **all six tasks done.** `Borough.Analysers`, twelve diagnostics covering CI lints 2, 3 and 7 and the `purpose_tag` row. Produced the rule-7 exception axis in `adr/0036` and fixed the lint count across three documents | — | none | 2 | [`0006`](0006-analysers-and-lints.md) |
| **4** | **Typed tables and the field declaration** — **all eleven tasks done.** Handles, columns, the single declaration, the State Hash, intrusive lists, `ResourceMap`, the first four tables. Produced `BOR0901`, answered ledger #29b for Phase 1, and gave the project its first State Hash | **2** | cleared | 3 | [`0007`](0007-typed-tables.md) |
| **5** | **The Tick, the Input Log and replay** — **all eight tasks done.** `step(inputs)` and the eight phases, the command model and the Input Log, replay to identical hashes, the golden-hash baseline, the headless runner with `Borough.Formats`, the three invariant tiers, the Census with `series(metric, window)`, and the crash artifact that replays back into its own crash. ⚠ **This row carried no status at all until 2026-08-14** — the only one in the ledger that did not, while every slice around it said *all N tasks done* — so the slice that built the Tick read as the least finished thing in the table. **Task 7 shipped its instrument and withheld its trend assertion** deliberately, nothing having churned yet; slice 10 task 10a discharged the flow half | **1** | cleared | 3 | [`0008`](0008-tick-and-replay.md) |
| **6** | **Map Layers** — **all ten tasks done.** Cell grid and the Cell/Chunk type split, the sparse double-buffered `LayerCellTable` — the project's first `Buffering.TwoCopies` — the separable integer convolution, the staggered schedule as a table, incremental re-diffusion proved bit-identical, the three real Layers, the named holes that throw, `layer_cells(aabb, layer)` and the end-of-run magnitude check. Produced `adr/0044`, which **settles owed decision 2 by measurement and finds it false** — and then got its own second half wrong by argument and withdrew it rather than amending it away | **3c** (Layers half) | cleared | 3 | [`0009`](0009-map-layers.md) |
| — | *the Phase 1 gate closes here* | | | | |
| **7** | **Rule engine — Bins and Bin Rules** — **done.** Bins with no public level column, the two wait lists, the Ruleset loader and its five refusals, quoted decimals, atomicity over net deltas, the apply band, the Readout set, `on_fail` chains, `02 §4`'s counters, and **task 10a's wiring: the first Ruleset ever to reach a `World`.** Produced `adr/0049` and `adr/0050`, took the **Resource family** out of order to stop a money leak six slices old, and measured the first price the Tick has ever had on it — **82.84 ns an evaluation**. ~~Task 10 ships the first Ruleset with content in it~~ — **split while being planned**: it asked for a production chain over two or three Goods, which is `pool`, which this plan's own decision owed 3 had already made a named hole that throws. **10a shipped and closed the slice**, with a `rulesets/minimal.toml` the golden session now runs under and an arming stagger that turned out to have **no number to choose**. **10b is the content, re-filed to Phase 2** | **3a** | ✅ **cleared** by session A → `adr/0048` | 3 | [`0011`](0011-rule-engine-bins-and-rules.md) |
| **8** | **Rule engine — hot reload. DONE — all tasks.** Tasks 1–3 shipped first; **tasks 4, 5 and 6 merged into one** before any was written, because none is reachable alone (a kind removal trips `KindCount`, `RuleCount` and `ResourceCount` at once, and the structural refusal lifts once or the reload half-happens). The swap at the top of Phase 0 — **and there is no new verb**, because `Command` is 12 bytes and the transition machinery (`RulesetHashAt`, `TickInput.RulesetHash`) is already in the tree unused — the transition in the Input Log, the world-creation refusal and the loader's second entry point, the three degradations (**derelict flag**, dropped Bins, wait lists re-armed on slice 7's derived stagger), the provenance trail with `adr/0006`'s cap, **the Layer cadence and rates from a file**, and the runner accepting more than one Ruleset so `adr/0015`'s seconds test can be run at all. **Planning found a live defect**: the industrial pollution kernel radius was a `const` in `SeparableKernel.cs` and `adr/0015`'s world-creation category never exempted it from the Ruleset — so task 3's refusal had nothing to refuse until it moved. **Task 3 moved it and built the whole `[layers]` table**, which absorbs most of task 8: the loader's refusal count goes **8 → 11 at load plus a 12th on reload** *(⚠ **this board no longer states the running count — 2026-08-18.** It carried a third copy of a number `adr/0048` calls the count of record, all three drifted, and the recount that day found the ADR's own copy stale by **36**. `adr/0048` states it, a test holds that ADR to the loader's `Refuse(` call sites, and this cell keeps only what slice 8 itself shipped)*, and the reload check is the project's first that is a property of a file *against a world*. It also found that **`adr/0015`'s world-creation enumeration has four members and only one of them is Ruleset data**. **Task 4 then found the larger one**: a Ruleset declaration's id is its position in the file, so *every removal the degradations exist for is also a reordering* — `02 §4.3` describes them as though ids were stable across two files and nothing made them so. Fixed by a key per declaration. **Derelict turned out to need no flag at all**. **Tasks 7–10 closed the slice.** The provenance trail is a capped table on the `World` — `05 §7`'s degradation-as-state, 16 transitions retained and older ones aggregated to counts — and *the cap is world-creation-fixed on a self-referential argument*: a designer must not be able to reload a smaller window, because the file whose adoption the history is about would be truncating that history. The Layer numbers are now **stated** by `rulesets/minimal.toml` and `adr/0044`'s claim runs end to end from TOML text to State Hash, on a fixture built for it — **the golden session cannot see its own cadence**, because it emits no pollution and diffusing zero at any period gives zero. The runner takes `--ruleset` repeatedly and refuses a transition nobody supplied, which **`--force-ruleset` may not waive**; `Replay.Start` turned out to check the catalogue against the world it had just built from that same catalogue and never against the **log**. And `adr/0015`'s acceptance test **ran: 0.70 s**, against the 60–120 second warm rebuild the ADR was written on — **it could not have been run through a recorded log at all**, because a log names a Ruleset by content hash and editing the file *is* the loop, so `--reload-at N` builds the transitions on the run | **3b** | ✅ **cleared** by session A → `adr/0048` | 3 | [`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md) |
| **9** | **Event Wheel — all four tasks done.** Session C settled the design and **narrowed the scope**: the **fine wheel only**, because the coarse Day wheel has no consumer until Life Stages arrive in Phase 2, and building it now would be writing past the slice. Finish what slice 7 half-built, keep `Arm`'s refusal above `WHEEL_SIZE` with its message re-pointed at `adr/0056`, and state the invariant the session extracted — *every live scheduled row is in exactly one of {armed, waiting}, and is unlinked when its owner row is freed* | **4** | ✅ **cleared** by session C → `adr/0056` | — | **done** → [`0016`](0016-the-event-wheel.md). **776 tests, no baseline moved** — hash-neutral, which was the slice's own acceptance test. **Two findings outrank the four tasks.** The end-of-run tier had been stamping every violation **Tick 0** in both 100,000-Tick runs, because each called `CheckEndOfRun()` on a *fresh* `Simulation` over an already-run world — invisible for as long as no end-of-run invariant read the stamp, and caught by the first one that did. And `Simulation._tick` is the **next** Tick to run rather than the last one run, which is why the period window is half-open at the bottom. Three `BinTests` fixtures also turned out to run time **backwards** — popping a row for Tick 1 and then depositing on Tick 0 — the fourth instance of a green suite agreeing with the code rather than the claim, and the first in a fixture's clock. The slice's shape is that **all three checks are relative to a *now* the wheel does not have**, which is the wall slice 8's `Adopt` hit too. **The plan found the fine wheel is not half-built but built**, and both halves of the invariant session C extracted have been in the tree, registered and tested, since slice 7 — so the four tasks are four *corrections* rather than the construction the row anticipated: a refusal message naming a design `adr/0056` refuted, a missing write-site refusal against a double-arm, a whole-world check **blind to a whole period** because it is written modulo the period, and an `Unlink` that discards the one signal saying whether it unlinked anything. Two of the three holes are reachable only through a **Ruleset reload** and a **save/reload** — the first in flight in slice 8 this week, the second guarded by the invariant session C found has never run |
| **10** | **Zone Rules — the second Rule family — all ten tasks done.** The Lot's permission set, the derived Lot→Building index, the `[[zone_rule]]` table and its three refusals, the sample and its third `purpose_tag`, the trigger in **Tick phase 6**, create, demolish, eviction, the tripwire, **both halves** of the long-run trend assertion, and `--zones`. **Planning inverted the slice's name** and the build inverted it back: the create predicate had to exist because a slice that only demolishes leaves `slots` flat against a *falling* `live`. Produced `adr/0053`–`0055`, **deleted one of its four unratified numbers by deriving it away**, and **amended `adr/0053` twice from the code** — the signal is a Rule asleep short of an *input*, and the clock lives on the Rule Instance. Three findings outlive it: the growth cycle **cannot be entered from a standing start**; the tripwire reads **1.56×**, so `02 §5.7` is *false in the letter and true in the substance* and the variable is the **working set**, not the Zone; and the city settles **five-sixths homeless** because **a Building has no declared occupancy** — filed, not tuned | **3c** (Sweep half) | ✅ **cleared** — *slice 7 was a dependency and not a gate, and it has since closed* | 3 | [`0014`](0014-zone-rules-and-the-sweep-family.md) |

> ~~**PHASE 2 HAS STARTED, AND THIS DOCUMENT DOES NOT OWN IT.**~~ **STRUCK 2026-08-22. The refusal
> below was right about the *order* and wrong about the *status*, and the difference is what the board
> has been carrying ever since.** `06` milestone **5a, the Road Graph**, shipped **2026-08-11** — the
> first Phase 2 slice, briefed and recorded in [`0020`](0020-the-road-graph.md), which is where its
> tasks, its findings and its gate live. ~~The ledger above stops at slice 10 on purpose: this file's
> scope is *"the ordered slice ledger for Phase 0 and Phase 1"*, and extending it would make it a second
> home for a slice order it does not own — `0012` *Cause 1* on the axis this corpus has been bitten by
> most.~~
>
> **Why it is struck.** The *order* does live in [`06`](../docs/06-roadmap.md), and restating it here
> would indeed be `0012` *Cause 1*. But **per-milestone status lives in neither**: `06`'s own header
> assigns live status to [`0000`](0000-board.md), and `0000`'s own rules forbid it to be a source. So
> the refusal left a layer with no owner, and the board filled it — 551 lines of *What is next* doing a
> ledger's job for milestone 12, which is the **third** time the board has inflated and been
> hand-cleared. ***A document that declines a layer does not thereby abolish it.*** The table below
> therefore restates **no order and no risk** — both stay `06`'s, cited by number — and holds only what
> `06` may not and `0000` may not: **gate, plan pointer, and state.**
>
> ⚠ **The slice axis stops at 10 and does not continue into Phase 2.** [`PROCESS.md`](../PROCESS.md) →
> *Numbering* keeps a slice and a milestone on different axes, and Phase 2 has run **one plan document
> per milestone** throughout — the milestone *was* the sitting-sized unit. Minting slice numbers 11–22
> now would invent a citation axis for work already recorded under another, which the same section
> forbids for shipped work: *a shipped milestone keeps its number for ever.*
>

> **What 5a changes for the rows above is one thing: the S2 harness deletion is unblocked.** The hold
> was that `spikes/S2.Routing/Graph/` is the reference implementation of 5a. The port is done, nothing
> in `src/` or `tests/` compiles against the harness, and the deletion — **51 tracked C# files and
> 29,719 lines of code, inside 92 tracked files and 42,914 lines all told**, the balance being the
> `results/` reports —
> is **blocked again on a different gate**: another session is doing further research inside the
> harness, so it is live work rather than a spent artefact. The 5a gate is discharged; this one
> is not, and the two must not be confused for each other.
>
> **What 5b-bis changes for the rows above is nothing, and that is worth writing down because it looks
> like it should.** `06` gained milestone **5b-bis** on 2026-08-12 ([`adr/0081`](../docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)),
> and it puts a **second `[[building]]` kind** in a shipped Ruleset for the first time — which reads
> like the thing **slice 7 item 10b** has been waiting for. **It is not.** 10b was re-filed to Phase 2
> because it asked for a production chain over two or three Goods, which is `pool`, which this plan's
> own *decisions owed* item 3 made a named hole that throws. The §A sitting **considered the path
> through `pool` and declined it**: shopping was the fully specified generator and was refused because
> its preconditions include the `Scope.Pool` market, whose own refusal site warns that getting it wrong
> *"ships an unconserved economy, and no refusal can catch that."* **5b-bis builds job slots, not a
> market**, so 10b's gate is untouched and `pool` still throws. *A milestone that moves near a blocked
> row without clearing it is exactly the shape `0012` Cause 3 is about — a gate cited once and never
> re-read — so the non-clearance is recorded rather than left to be inferred.*

### The Phase 2 ledger

**Keyed by [`06`](../docs/06-roadmap.md)'s milestone number, in the order built.** *Gate* is what had
to clear before the row could start; *Plan* is the document that owns the tasks, the findings and the
record. **The risk each row retires is `06`'s and is not repeated here** ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)).

⚠ **A cell in this table is at most three sentences.** It says what the row *was for* and where the
record is. Anything longer belongs in the plan document, and a cell that outgrows the rule is the
failure mode that produced this table.

| # | Milestone | Gate | Plan | State |
|---|---|---|---|---|
| **5a** | Road Graph and Streets | none | [`0020`](0020-the-road-graph.md) | ✅ **2026-08-11**, all seven tasks. Its definition of done is met but for the `spikes/S2.Routing/` deletion, held below |
| **5a-bis** | The Lot subdivider and the road editor | none | [`0022`](0022-the-lot-subdivider-and-build-road.md) | ✅ **2026-08-11**, all seven tasks, `adr/0077`–`0079`. ⚠ **It has no milestone row in `06` at all** — filed in [`0012`](0012-corpus-audit.md) |
| **5b** | Trips, Legs and the pedestrian layer | none | [`0021`](0021-trips-legs-and-the-pedestrian-layer.md) | ✅ **2026-08-12**, tasks 1, 2, 3, 5 and 7 — the whole of the re-scoped slice |
| **5b-bis** | Jobs, the commute, and the first Trip generator | none | [`0023`](0023-jobs-and-the-commute.md) | ✅ **2026-08-13**, all eight tasks |
| **5c** | Statistical resolution and the travel-time matrix | none | [`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md) | ✅ **2026-08-16**, all eight tasks. ⚠ Task 8 was the named ratifier for four hash-bearing numbers and **could not fire** |
| **6** | Evidence — the accumulators | none | [`0028`](0028-evidence-the-accumulators.md) | ✅ **2026-08-17**, all seven tasks, every owed decision closed |
| **7** | Parking | ✅ cleared by session **H**, 2026-08-12 | [`0031`](0031-parking.md) | ✅ **2026-08-19**, all eight tasks, all four decisions settled. `adr/0119`, `adr/0120` |
| **8** | Save/load | ✅ ungated by session **K**; `adr/0086`, `adr/0087` | [`0030`](0030-save-load.md) | ✅ **2026-08-18**, all ten tasks, all five open decisions closed. Lint 6 goes live |
| **9** | The land value target and the composed Layers | none | [`0034`](0034-the-land-value-target-and-the-composed-layers.md) | ✅ **2026-08-20**, all eight tasks, all six decisions settled, `adr/0122`–`adr/0127`. Scoped and closed inside one day |
| **10** | Conserved Money and the treasury | none | [`0033`](0033-conserved-money-and-the-treasury.md) | ✅ **2026-08-19**, all six decisions settled, `adr/0113`–`adr/0118`. Built ahead of 9 on its own branch |
| **11** | Hinterlands and arrival through the gate | ✅ assessed 2026-08-20 — nothing names one | [`0035`](0035-hinterlands-and-arrival-through-the-gate.md) | ✅ **2026-08-21**, all nine tasks, all ten decisions. Gate discharged by the unfiltered suite: **1,927 passed, 0 failed** |
| **12** | Goods between Buildings — the District Pool *(capped at task 6)* | none | [`0037`](0037-goods-between-buildings-the-district-pool.md) | ✅ **DEFINITION OF DONE DISCHARGED 2026-08-23 — the whole suite, unfiltered: 2,083 passed, 0 failed.** ⚠ **It had NOT run against this milestone until then**, and the pull request carrying the work said so on its own face; the obligation travelled to `main` with the merge and **a gate is discharged by the work and struck by somebody, and only the first happens on its own** — which is this table's own S0b row, applied to a row six lines above it. 🔴 ⚠ **THE DURATION IS NOT A QUOTABLE FIGURE AND IS RECORDED HERE ONLY AS AN UPPER BOUND**: the run was made **detached alongside other work**, with the corpus tier run against it three times, so it is a **spoiled measurement** in exactly the way `CLAUDE.md`'s 1m52s and 50s readings were — ***a test-cost capture is a parallelism measurement, so it takes a parallelism measurement's controls.*** **The gate is unaffected**, because a gate asks whether the city is correct and noise cannot fail an assertion ([`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md), amended). ***A gate asks whether the city is correct; a capture asks how fast it is; only the second needs the room silent.*** ✅ **CLOSED 2026-08-22 AT TASK 6, and the cap is a decision rather than an abandonment.** **Tasks 1–6 shipped**; ⚠ **its RISK IS REWRITTEN** in [`06`](../docs/06-roadmap.md) to what they retire — ***that a District is an administrative label rather than a derived thing with a market in it*** — because `Scope.Pool` **still throws**, so the original risk is not retired and ***a milestone must name a risk it actually retires.*** 🔴 **Tasks 7–10 and the original risk moved to milestone 26**, blocked on **milestone 25** — the Business is the actor, session V, [`0039`](0039-session-v-the-business-is-the-actor-and-the-building-is-premises.md). ⚠ **What stopped it: a purchase needs a payer.** Money lives on a Business, a Rule Instance names a Building, and a Building holds a **list** of Businesses — ***which `BusinessTable`'s doc comment predicted eight milestones early and no mechanical check could see.*** **Two ADRs came out of the milestone's last afternoon**: [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) (a Pool is a market, not a store) and [`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md) (a milestone number is an identity). *The original entry:* 🟢 ~~**LIVE.**~~ **Scoped and decomposed 2026-08-22 — ten tasks, and TASKS 1 THROUGH 4 SHIPPED the same day**: `rulesets/twinned.toml` and the `[[lattice]]` key, the first world this build can generate with **two centres** in it, its two lattices joined by a Street corridor so that only the density field can split them; the Building-density field, which was **already built** by 5b-bis as `BuildingResidency`'s per-Cell count, so task 2 shipped a name and the evidence rather than storage; and **the derivation itself** — `Space.DistrictWatershed`, a two-descent persistence watershed over that field, clipped to the **Foot** road component, writing `DistrictTable` and `DistrictCellTable` as `(saved AND hashed)` and run once from `SyntheticCity.PopulateInto`. 🔴 **The State Hash moved and every golden trace and the world baseline were regenerated** ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)). ⚠ **Nothing reads a District yet** — `Scope.Pool` still throws. ⚠ **One §D1 row was owed and written**, not four: `[districts] prominence_percent = 50`, unratifiable on every world that exists and the tests say so as a `[Theory]` at 1, 50 and 100 (`0037` **F17**). ⚠ **`DistrictId` did not ship and nothing is missing** — the identity is `Handle<District>` (`0037` **F11**). **Task 4 then made the extent state rather than a label**: `[districts]` goes from one key to four, `Evaluate` **reconciles rather than replaces**, identity travels through the **centre Cell**, a District no basin claims is **destroyed**, and the cadence runs last in **phase 6** after the Zone Rules. ⚠ **No golden hash moved this time** — the mechanism is gated on a Ruleset key no golden session states. 🔴 **Task 4 also corrected this milestone's own plan**: `0037` called the migration bound a *work* bound where `adr/0134` and `0002` §D2 both say it bounds how far the boundary MOVES, which would have been different code (`0037` **F20**, filed in [`0012`](0012-corpus-audit.md)). **Three more §D1 rows written on the day, all naming milestone 15.** (`0037` **F1**–**F23**). Decisions **1, 2, 4, 5, 6, 7, 8 and 9 settled** (`adr/0132`–`adr/0138`, and 7 with the user in the room); **open: 3 and 10**. ⚠ **Decision 7 closed as largely VOID AS POSED**: *subject to connectivity* is already spent by the watershed's road-component clip, so a District is connected by construction and a Building's District is its **Cell's**. ⚠ **Decision 10 was found by DECOMPOSITION, not by the sitting** — the Pool is the counterparty on both sides of a trade and the two sides happen at different Ticks, so *where the money sits in between* is unanswered by `adr/0050`, `adr/0135` and `adr/0114` alike. ✅ **Task 5 shipped the same day** — `BinOwnerKind.District` is the fifth member, `DistrictPoolTable` is a **saved join** because a derived list is only derivable when the element names its owner, every Pool Bin is **unbounded**, and a dying District's stock goes to whoever took its **centre Cell** — identity's own rule, used for succession. 🔴 **The State Hash moved again and all three golden baselines were re-recorded**, because an appended table folds its allocator even while empty. ⚠ **No money Bin and no lookup index**, both deliberate and both explained at the symbol. 🔴 **Task 5 found a defect it does not own and routed it: QUEUE ITEM 15** — a Ruleset that *inserts* a `[[resource]]` crashes the swap on the **treasury**, on `minimal.toml`, with no District anywhere. ~~Task 5 is blocked on QUEUE ITEM 14 below~~ — settled 2026-08-22 in its own commit, ahead of the task, exactly as that row required. ⚠ **The survey found three preconditions no document had listed**, and decomposition found a **fourth** — `BinOwnerKind` has four members, none is a District, and `BinTable.Owner` is a `HandleColumn<Building>`. ⚠ **Task 1 is a WORLD, not code**: on every world this build can generate the derivation yields one District and is untestable |
| **25** | 🔴 **The Business is the actor and the Building is premises** *(capped at group A, 2026-08-23)* | none — ✅ **session V RAN AND CLOSED 2026-08-22**, into [`adr/0141`](../docs/adr/0141-a-tenant-owns-what-leaves-with-it-and-the-premises-own-the-capacity.md) and [`adr/0142`](../docs/adr/0142-an-unpremised-business-emigrates-so-the-sink-is-the-one-households-already-use.md); 🔴 **one thing did not close and it is a NUMBER rather than a shape — what capitalises a Business**, owed a `plans/0002` §D ratifier | [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md) | ✅ **CLOSED 2026-08-23 — group A tasks 1, 2, 4, 5 AND THE CLOSING TASK all shipped, in one day.** **Task 1**: a Bin hangs off its owner ([`adr/0143`](../docs/adr/0143-a-bin-hangs-off-its-owner-and-the-polymorphic-column-stays-unbuilt.md)) — `HouseholdTable` and `BusinessTable` each gain a saved `BinHead`/`BinTail`, `BinTable` gains a saved `OwnerNext`, and **the polymorphic owner column `adr/0114` gestured at is NOT built**. 🔴 **`Balance` became DERIVED on both tables and that was not in the plan** (**F12**). **Task 2 is tasks 2, 3 and the stagger half of 4, merged** — ⚠ **`adr/0141` had already declined to split them in its *Rejected* section, and the decomposition split them anyway** (**F19**), which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) with the direction reversed: a plan describing work rather than reading the record that governs it. A Ruleset now says `owner = "occupant"` on a `[[building]] bins` entry; a **Rule's** side is **derived** from its own `local` terms, because an `owner` key on a `[[rule]]` would state a second time what the terms already state; a Rule addressing both sides is refused at load (**138 → 140** refusal sites, and `RefusalCountTests` — the corpus's only document-to-*code* check — is what noticed). 🔴 **The State Hash moved and four golden artefacts were re-recorded; the version byte was NOT bumped**, because the fold did not change. 🔴 **THE SHIPPED CITY NOW HOLDS THREE TIMES THE STOCK** — the draw is unchanged and the *supply* is not, since three families now run three greedy `restock`s against three larders where they shared one — and every one of the **twelve** edited Rulesets says so in its own header. ⚠ **`derived = "occupancy"`, the one declared Readout in the project, has lost its only caller** and is left declared. ⚠ **`bin.bin_next` stopped being written by any shipped world** and `DerivedRebuildAuditTests` caught it on the first run (**F29**) — repaired with a fixture, not with a second Bin on a shipped kind. 🔴 **Task 4 removed a defect TASK 2 HAD SHIPPED, and that is the cut working rather than failing** (`0040` **F30**): `Condemn` took the longest pressure across the Building's whole Rule list, which was correct while every Rule in it belonged to the premises — so the moment a tenant had Rules of its own, ***one starving Household condemned the Building its two neighbours were living in***. ⚠ **It was live for the length of one commit and no test failed**, because nothing in the suite had two tenants failing differently; that test did not exist until the fix did. `Worst(building, tenant, …)` now walks the list once and filters on the subject, the premises are judged first, and a failing tenant is evicted through `World.Unplace` while the premises stand — counted by `ZoneCounter.Ended`, separately from `Demolished`. ⚠ **No golden artefact moved**: a tenancy ending is reachable only past `condemn_after` and no golden session reaches it, so the task is hash-bearing in principle and moved nothing in practice. **`RuleEvidence` and `BinEvidence` gained `Tenant`** — which discharges **F28** and uncovered a second hole in the same panel (**F33**), since the bin table was assembled from the Building's own list alone and every `restock` row named a Bin that appeared nowhere above it. 🔴 **The agreement test knew about ONE list** (**F34**): `EvidenceTests` walked `BuildingBins` and asserted the *count*, and it is the **length assertion rather than the field comparisons** that caught the second source — a subset assertion would have stayed green while the panel grew a whole half nothing checked. 🔴 **Nothing records *why* a tenancy ended** (**F35**) — the condemnation trail is a **Lot's**, and an entry there would be a demolition record for a Building still standing; that channel is [`adr/0130`](../docs/adr/0130-the-pools-bound-is-a-duration-and-the-unhoused-channel-ships-with-the-gate.md)'s and ships with task 5. ✅ **Task 5 shipped the same day, and open decisions 1 and 3 were settled first, into [`adr/0144`](../docs/adr/0144-a-tenant-that-loses-its-premises-keeps-only-its-money-and-waits-a-households-wait.md).** `UnpremisedTable` is a **separate** table rather than a discriminated column on the Unplaced Pool — `adr/0143`'s refusal reaching a second relation — with two columns and two argued absences: a Business has no arrival gate, and a counter of premises looked at would read identically zero for ever. `DestroyBuilding` stops unlisting-and-freeing-nothing, `PlacementEngine.Retire` samples the pool on the **same trigger, sample derivation and bound** as the Household half, and `Depart` takes the balance out of `MoneySupply.Issued`. ⚠ **The pool ships with ONE exit and it is the sink** — nothing tenants a Business, so the placement half is milestone 27's and this is `adr/0006`'s bound arriving with the collection. 🔴 **The State Hash moved and all three golden baselines were re-recorded**; the version byte was not bumped. 🔴 **TWO LATENT DEFECTS FOUND AND ONE WAS ALREADY ON `main`** (`0040` **F36**, **F38**, **F39**): ***a saved table outside `World._tables` is not hashed***, which `UnpremisedTable` was, with **2,074 tests passing** — the corpus's *declaring a field is what allocates it, so the State Hash cannot have a coverage hole* is true **per column** and guarantees a column is folded only *if its table is walked*; and the census reserves room per family with a **hand-maintained constant**, wrong for `ZoneCounters` **since task 4**, so *tenancies ended* and *placement considered* printed the identical four numbers and nobody read them side by side. ⚠ **Both are the same shape** — a declaration and a hand-kept count with nothing checking they agree — and both are now closed by tests (`TableRegistrationTests`, `CensusFamilySizeTests`). ***The class is not closed***: nothing enumerates the remaining hand-kept counts. ✅ **The closing task shipped and it found the milestone's own mechanism was INVISIBLE** (`0040` **F43**): task 4 made a failing tenant end its tenancy while the premises stand, and on all twelve shipped files the premises' `upkeep` starves while the tenants are fed — so `minimal.toml` measures **2,610 condemned against 0 tenancies ended** and the thing to look at could not be pointed at. ⚠ **Every test of it built its Ruleset by hand**, so nothing noticed: ***a mechanism exercised only by hand-built fixtures is a mechanism with no world in it.*** **`rulesets/evicted.toml` is the thirteenth file** — `minimal.toml` with `restock` and `upkeep` deleted, because the failure only had to move from the premises to the tenant — and it measures **929 tenancies ended against 0 condemned**, guarded by a test so it cannot rot back. 🔴 **The long run's stated obligation was the WRONG COLLECTION** (**F44**): the plan named the unpremised pool, which is empty in every world, and ***the collection milestone 25 actually introduced is the tenant's Rule Instances and Bins***, whose lifetime task 2 tied to the tenancy. **131,072 Ticks at 4,000 Citizens, ~1,900 tenancies ending per window: `bin` and `rule_instance` slot counts FLAT at 3,362 and 1,440** — rows recycled rather than accumulated, which is `adr/0006`'s actual claim and what a live count alone could not show (**F45**). No invariant reported. **The whole suite, unfiltered, is the gate and was run.** *The decomposition entry:* 🔴 ~~**NEXT, and not started.**~~ **DECOMPOSED 2026-08-23 into ten tasks in two groups, then CAPPED AT GROUP A the same day, with the user in the room.** **`0039` V18 found the cleavage and declined to make the cut; [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md) ordered the whole milestone and marked it; the cut was made at task 5.** 🔴 **So 25 is tasks 1–5 plus the closing task, and its RISK IS REWRITTEN in [`06`](../docs/06-roadmap.md)** to ***that a Rule Instance names premises rather than an actor, so no money term can resolve to a payer*** — because group A makes the actor **nameable** and does not make one **exist**. ⚠ **Tasks 6–9 became milestone 27** and are kept in `0040` as written, because they are the specification 27 inherits. ⚠ **This is the second cap in two days and it is the scheme working**: it was placed by decomposition **before any code was written**, rather than by a milestone running out of road. **Group A is a repair driven end to end by the Household and needs nothing that does not exist; every entry in group B needs something that does.** 🔴 **Decomposition found one thing neither ADR answers and it is now open decision 1**: `adr/0141` keeps a Bin's capacity keyed on the **building kind** at the creation site, `adr/0142` makes **unpremised** a legitimate steady state, and `Businesses.Building` is `Reference.Severable` — ***so an unpremised Business owns Bins whose capacity is declared by premises it does not have***, which `RebuildCapacities` silently resolves to **0** while the Bin holds stock. ⚠ **`0039` V14 found that severable hop on the EMISSION path and stopped there.** ⚠ **The census also made the milestone SMALLER in one place**: V10's six *implicit* Rule sites all reach the Rule Instance through the derived `BuildingRules` list rather than the saved `Building` handle, whose only readers are `RuleEngine` (4) and the rebuild (1), with **none in tests** — ***the blast radius is the list, not the column***. ⚠ **The columns are cheap and the test surface is the milestone**: `BinTable.Owner` and `RuleInstanceTable.Building` are **6 sites each** against **44 `CreateBin`/`FindBin` call sites in `tests/`**. *The original entry:* 🔴 ~~**NEXT, and not started.**~~ **That the economic actor does not exist in the build, so no Rule can spend money.** ⚠ **On its main axis a CORRECTION rather than a design change** — [`adr/0113`](../docs/adr/0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md) decided it, [`adr/0114`](../docs/adr/0114-a-balance-a-rule-can-fail-on-is-a-bin-and-a-bins-owner-is-discriminated.md) wrote the target shape (***"`World.FindBin` takes an owner rather than a Building slot"***), and **the build never arrived**; `adr/0113`'s own revisit trigger names the blocker. ***A revisit trigger that has already fired is a decision waiting to be finished.*** 🔴 **Two places the corpus contradicts ITSELF are the real work and neither is settled**: which Bins belong to a tenant, and whether jobs are premises or employer |
| **27** | 🔴 **The Business is a thing the city contains** | ✅ **CLEARED 2026-08-23** — its one gate was **milestone 25**, the Occupant repair, and 25 closed that day. ⚠ **The dependency ran ONE WAY ONLY** (`0039` **V18**): jobs cannot move to the employer without something that creates employers, and nothing creates an employer without a **kind** to create it from | [`0041`](0041-the-business-is-a-thing-the-city-contains.md), over [`0040`](0040-the-business-is-the-actor-and-the-building-is-premises.md) tasks 6–9 | ✅ **DECOMPOSED 2026-08-24 into [`0041`](0041-the-business-is-a-thing-the-city-contains.md)** — which owns the **order** and the preconditions, while `0040` keeps the tasks as written. ✅ **TASK 6 SHIPPED 2026-08-24** — a second kind namespace end to end (`RulesetNames`, `RulesetLoader`, `Ruleset.BusinessKindKeys`, `RulesetMigration.BusinessKind`, two `RulesetShape` members), a **saved `BusinessTable.Kind`**, and [`rulesets/tenanted.toml`](../rulesets/tenanted.toml) as the **fourteenth** shipped file. 🔴 **The State Hash moved and exactly ONE of the four golden artefacts was re-recorded** — `world-hash.txt`, because `GoldenFixtures.Build()` is the only golden world holding a Business (`0041` **G16**) — and the fixture's two Businesses were given kinds **1** and **2** first, so the baseline covers the *value* rather than only the column (**G15**). **Refusal sites 140 → 141**, `adr/0048` updated. ✅ **TASKS 7, 8, 9 AND 10 SHIPPED 2026-08-24 AND THE MILESTONE IS CLOSED.** **Task 7** moved `jobs` and the Shift band to the trade and **emptied every shipped city** — 66 assertions on one sentence, *nobody is employed anywhere* — because `[founding]` was the only creator and one file states it; the content half is [`adr/0148`](../docs/adr/0148-a-premises-kind-may-declare-its-trade-and-instantiating-one-is-not-housing-anybody.md), a `[[building]]` kind naming one trade that construction **instantiates**, which surfaced `SyntheticCity` sizing the world by the **tenant** ceiling and building a quarter too few homes. **Task 8** is [`adr/0145`](../docs/adr/0145-a-business-is-founded-by-a-household-or-arrives-through-a-gate-and-both-land-in-the-pool.md)/`0146`'s founding channel and [`rulesets/founded.toml`](../rulesets/founded.toml). **Task 9** is [`adr/0149`](../docs/adr/0149-a-business-is-a-population-a-policy-sweeps-and-a-readout-names-every-entity-it-reads-against.md) — 🔴 **and it is NOT the task the plan described**: `0041` **G10** read `RuleInstanceTable`'s *"a Business gets its own column … which is milestone 27"* as an estimate, the column was built, and it **loaded and crashed on the Tick it fired** because `RuleEngine.Fire` resolves a Building from the instance. What shipped is the sentence the loader's own refusal already carried — *a Business has a balance and no pass that moves it* — so a **Policy sweeps Businesses**, and a Readout declares the **set** of entities it reads against. [`rulesets/levied.toml`](../rulesets/levied.toml) is the sixteenth file. 🔴 **TASK 10'S LONG RUN FOUND A DEFECT AND THAT IS THE TASK'S WHOLE VALUE** (**G43**): `adr/0148` identified *the trade this kind came with* **by kind**, and founding draws over every declared trade — so a founded shop and the instantiated one were interchangeable in a Building's list and demolition razed whichever came first. ***Two defects pointing opposite ways***: the founded shop's capital left the city through `Raze` (**23,983 of 354,562 per 20,480 Ticks**) and the instantiated one outlived its premises into the pool (**52 stranded, immortal**, against **0** on the files that found nothing). `BusinessTable.Origin` is the repair, `adr/0148` carries the amendment, and ⚠ **all three golden artefacts re-recorded — both session traces as well as `world-hash.txt`, where task 6's column moved only the world fixture, because `Fit` writes this one and every session raises Buildings**. 🔴 **What delayed finding it was a HEADER**: `founded.toml` opened by predicting its own money drain, so the drain was explained and nobody looked — ***naming an expected symptom is how you stop noticing the unexpected one that looks identical***. ⚠ **`--business` is the twelfth runner mode** and `BusinessLongRunTests` names its three collections **before** the run (`0040` **F44**'s correction applied). 🔴 **The shop count is bounded by the SOURCE EXHAUSTING and no sink has ever fired** (**G44**) — 7,165 premisings against **0** give-ups over 131,072 Ticks — ***so the bound reopens the day anything refills household money***, which is milestone 11's gate and 26's revenue. **`F43`'s question was asked first and answered: all sixteen shipped files now contain a Business the city created.** 🔴 **`FactorioTests` was found carrying three wrong column counts, one of which was never right** (**G14**). ~~🔴 **NOT STARTED**~~, **AND THE ORDER IS 6 → 9 → 8 → 7** rather than the specification's: run it as written and **task 7 empties the city of jobs**, because `jobs` on the dwelling kind is a **stand-in** for the workplace kind that does not exist (`0041` **G5**). ⚠ ***The dependency in this row's own gate cell said so and the task order did not apply it.*** 🔴 **UNGATED 2026-08-23** — ~~**BLOCKED ON 25.**~~ **This is milestone 25's ORIGINAL risk** — *that the economic actor does not exist in the build* — which 25 was capped short of on 2026-08-23. **That the Business is a TABLE and not an actor the city can create**: ~~**six** columns and **no kind**~~ ~~**seven, with a saved `kind`, as of task 6**~~ — **ten as of task 10**, counted at `BusinessTable` itself — so `Declares`, `BinsOf` and `sweeps = "business"` ~~have~~ **had** nothing to key on; ~~`jobs = 8` sits on the **dwelling** kind in all **fourteen** shipped Rulesets, so a fill rate has no employer to belong to~~ — **task 7 moved `jobs` and the Shift band to the trade and refused them on the premises kind**; and ~~🔴 ***nothing funds one***~~ — **`[founding]` and `adr/0148`'s instantiation both fund one**. ⚠ ~~**`World.CreateBusiness` has ZERO `src/` callers and SEVENTEEN in `tests/`** — the risk stated as a number.~~ ✅ **THE RISK IS RETIRED AND THE SAME NUMBER SAYS SO: `World.CreateBusiness` now has `src/` callers, and EVERY shipped Ruleset contains a Business the city created** (`0041` **F43**'s question, asked first and answered) — ⚠ **count them at `rulesets/` rather than quoting a total from here: this sentence said *sixteen* for the length of one afternoon on 2026-08-25 and milestone 24's merge landed `coastal.toml` and `varied.toml` the same day.** ***The rule this cell is about caught the cell twice in one edit.*** ⚠ **Those three counts read *three*, *twelve* and *twelve* until 2026-08-24 and *fourteen*, *ZERO* and *SEVENTEEN* until 2026-08-25** (`0041` **G1**); ***the risk was untouched twice and the figures stating it were not***, so quote `0041`'s census and not this cell — ⚠ **and a count here is a fact that drifts, which is why this one now names the symbol to count at.** **Its tasks are `0040`'s 6 through 9, kept there as written**, because they are the specification this milestone inherits. ⚠ **It owns [`0002`](0002-open-questions.md) §D2's capitalisation band**, hash-bearing and owed a named ratifier. ⚠ **It sits between 25 and 26 and the numbering is an identity, not an order** ([`adr/0140`](../docs/adr/0140-a-milestone-number-is-an-identity-and-the-roadmaps-order-is-the-sequence.md)) |
| **26** | The purchase — `Scope.Pool` stops throwing | 🔴 **TWO — it was FOUR until milestone 25 closed on 2026-08-23, THREE until milestone 27 closed on 2026-08-24, and it said `none` until the day before the first**: ~~**milestone 25** (the payer — the actor becomes *nameable*)~~ ✅ **discharged 2026-08-23**, ~~**milestone 27** (an actor that *exists*, and it owns the capitalisation band)~~ ✅ **discharged 2026-08-24, all five tasks** — and ⚠ **the capitalisation band it owned is FILED RATHER THAN SETTLED**: the founding half is [`0002`](0002-open-questions.md) **§D1** (`founding_band` 400, `reconsider_ticks` 8192) and in use, the **arrival** band is still unwritten in **§D2**, and neither is ratifiable here because a shop cannot yet earn. **The Provider kind's three content decisions** ([`0002`](0002-open-questions.md) §A) and **a world where a Building genuinely runs out of money** (`0037` task 10) — ✅ **THE FIRST IS OWNED AS OF 2026-08-25: session W, [`0043`](0043-session-w-the-provider-kinds-content.md)**, briefed and not yet run. 🔴 ⚠ **AND THAT BRIEF FINDS THE SECOND IS NOT A GATE, PENDING THE SITTING** (`0043` **W1**): the levy is **unfailable by construction** and nothing else makes a Business spend, so ***the bankrupt world is milestone 26's OUTPUT rather than its precondition*** — `Ruleset.cs:1515` names it as a Provider selling into a saturated Pool. ⚠ **The count above is left at TWO deliberately**: a gate is not struck on one session's finding before the sitting that owns it has run. ⚠ **And the phrase names the wrong entity** — `adr/0113`'s own title is ***a Building never holds money***, so what is meant is a **Business** (`0043` **W2**, owed to [`0012`](0012-corpus-audit.md)). ⚠ **The second is no longer *to be checked at task 8*, because task 10 checked it and the answer was no**: **7,165 premisings against ZERO give-ups** over 131,072 Ticks, so ***nothing in the build drains a Business's money and the bankrupt world is still unwritten*** (`0041` **G44**) | [`0044`](0044-the-purchase-and-the-provider-that-answers-it.md), over [`0037`](0037-goods-between-buildings-the-district-pool.md) tasks 7–10 | ✅ **DECOMPOSED 2026-08-25 — [`0044`](0044-the-purchase-and-the-provider-that-answers-it.md), ten tasks. TASK 1 LANDED THE SAME DAY**: `RuleInstanceTable` gains a Business subject, `BinTenancy` a third value, `World.FindLocalBin` a ternary, and `RebuildCapacities`' `NotSupportedException` for a Business-owned Bin an owner walk (`adr/0166`). ⚠ **It moves the State Hash** — a new saved column — so two golden traces are re-baselined in its own commit. `0037`'s four entries survive as the **specification** and are superseded as a **sequence**. 🔴 **The inherited order is backwards** (`0044` **P1**): a buyer's `pool` term resolves to a seller's Bin, a Business owns one Bin and it is the balance, and nothing can hang a Rule on one — so [`adr/0166`](../docs/adr/0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md) is a precondition of **the purchase** and not only of the decline half, which is what this cell and [`0000`](0000-board.md) both said. ⚠ **Four open decisions, all found by decomposition and none in any ledger**, and two of them could make the work smaller. 🔴 **Three ledger debts name this milestone and none was in `0037`'s task list** — queue item **17** below, [`0012`](0012-corpus-audit.md)'s two doc comments, and [`06`](../docs/06-roadmap.md)'s nine-Resource placement row. *The original cell follows.* 🔴 **BLOCKED ON 27 AND ON TWO THINGS 27 DOES NOT TOUCH** — ~~**BLOCKED ON 25 AND ON THREE THINGS 25 DOES NOT TOUCH**~~, ✅ **25 discharged 2026-08-23**. ⚠ **This cell said *"BLOCKED ON 25"* and stopped there until 2026-08-23**, which made *what is blocking 26* read as **one** thing when it is **four** — corrected while decomposing **25**, not 26. 🔴 **The three were never hidden and were never in a ledger**: the Provider's second `[[zone_rule]]`, second decline Rule and land-use split are enumerated in **`rulesets/minimal.toml`'s header at lines 190–192** and again in `0037` task 8, and the acceptance run's world is named in `0037` task 10. ***A blocker named inside a task entry is invisible to the ledger that owns what is next*** — the payer's own failure mode, which `BusinessTable`'s doc comment predicted eight milestones early. ⚠ **Two things that look like blockers and are NOT, listed so they are not counted twice**: the market-row wait list is **decided by [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) and unbuilt**, which is task 7's own work rather than a gate; and per-seller price formation is a **deliberate stand-in** — every seller opens at the import ceiling, costing no number and no ratifier, with real formation at milestone **13**. *The original cell follows.* ~~🔴 **BLOCKED ON 25.**~~ **That the Rule engine ships a scope it refuses**, inherited from 12 when that milestone was capped. Its tasks are `0037`'s 7 through 10 — the purchase, the Provider kind, something to look at, and the long acceptance run — **kept there as written, because they are the specification this milestone inherits** 🟢 **TASK 2 LANDED 2026-08-25 — the land-use split** ([`adr/0165`](../docs/adr/0165-a-zone-permits-building-kinds-so-the-split-is-exclusive-and-the-instrument-paints-it.md)). ⚠ **It moves the State Hash on every generated world**, so all three golden artefacts are re-baselined in its own commit — **`session.borough` too**, because it moved three zone commands in `GoldenFixtures`. 🔴 **Three lines of painting, five subsystems of cascade**: density field, watershed, an end-of-run invariant, the golden session, and `PlacementEngine`'s candidate draw — the last being a real defect the split exposed rather than caused, since a seeker drew uniformly over the *whole* Lot table and commercial land therefore made `candidates` quietly mean less. New `Space.ZonedLots`, ✅ **provably neutral where it should be** (reproduces `main` at 35 of 189, to the digit). ⚠ **`adr/0055` forbids the same narrowing for the Zone Rule's sample**, so that dilution stays by design. **`TradeBlockStride = 8` filed in [`0002`](0002-open-questions.md) §D1**, and ⚠ **it saturates above 8** — strides 8/12/16/32 are one world. **Queue item 19 filed** under `adr/0073`. 🟢 **TASK 3 LANDED THE SAME DAY — `rulesets/provisioned.toml`**, session W's W-Q4 answered: a second premises kind, its trade, a Zone Rule on the trade bit, and the dwelling's `restock` drawing from `pool` instead of from nothing. ⚠ **It moves no hash and touches no golden** — a new file changes no existing world. ⚠ **It LOADS AND DOES NOT RUN by design**, which is its own acceptance test, and the throw arrives from the end-of-run invariant rather than from a Rule firing (`0044` **F4**–**F19**) 🟢 **TASK 4 LANDED 2026-08-26 — `Scope.Pool` RESOLVES, and the milestone's own risk is the one it retires**: *that the Rule engine ships a scope it refuses*. A `pool` input now expands **1:3** in `RuleEngine.Check` alone — Good from a seller, money from the buyer's purse, money to the seller's till, settled atomically with the Rule ([`adr/0050`](../docs/adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)) — while `RuleEngine.Bin` resolves the term to the **market row's** Bin for every other caller, because `BinAt`, `AccumulateClaims` and `Requirement` all ask *where does a waiter sleep* rather than *who supplied it*. ✅ **New `Space.DistrictMarkets`** pays all three debts task 4 was owed: the `(District, Resource) → row` map `DistrictPoolTable`'s doc said **task 7 owes**, the **`Bin →` market row** reverse lookup, and — the one nothing had at all — **the sellers standing in each row**, which `adr/0139` needs and no path in the build could answer. Whole-rebuild and counting-sorted on `ZonedLots`' model, `(derived AND rebuilt)`. `DistrictPoolTable.Consumed` gets its **first writer** in `Fire`. ⚠ **It moves NO State Hash and touches no golden artefact** — no saved column, and every golden fixture runs on a file that states no `[districts]`. ✅ **[`adr/0167`](../docs/adr/0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md) settles open decisions 2 and 3**, both with the user in the room: a **counter-based start offset** over the seller list plus a first-fit walk, keyed on the **buying Rule Instance** rather than on the row — keyed on the row the whole city would rotate in step, which is one herd where there had been one favourite — and a blocked buyer waits on the **market row**, never on the shop it happened to draw. 🔴 ***Cheapest was not available***: `Price` is keyed by `(District, Good)`, so every seller in a District charges the same number and a comparison over them compares one value with itself. ⚠ **`adr/0139` says the opposite in its own Consequences and agrees with the build in its Decision line and its correction banner** — a record contradicting itself, filed in [`0012`](0012-corpus-audit.md) and struck in `04 §4` rather than settled here. 🔴 **A third answer neither decision saw**: a Building raised between two watershed evaluations stands in **no District** for up to `[districts] revisit_ticks`, so there is no Bin to wait on — the Rule **fires at zero applications and re-arms on its rate**, which is `RuleVerdict.Succeeded`'s own documented reading. It crashed the first headless run at Tick 2,532 before that was understood. A Ruleset stating **no `[districts]` table** is the permanent case and still throws; ⚠ **the refusal belongs at load and the loader has no such check — queue item 20**. 🔴 **Task 1's invariant had a hole this task walked into**: `BinCapacitiesMatchTheirDeclarations` sent a Business-owned Good Bin down the *must be unbounded* branch, because task 1 grew `RebuildCapacities` and not the end-of-run check, and **no world could reach it until a shop held stock**. Widened with a third traversal. ⚠ **`rulesets/provisioned.toml` needed ONE edit and it was not the scope**: it stated no `[households]`, so every buyer opened at zero and every purchase failed on the **money** leg at Tick 0 for ever — the shops filled and nobody bought. `opening_balance_min`/`max` filed in [`0002`](0002-open-questions.md) **§D1**, `max` derived from the file's own consumption rate against the import ceiling. 🔴 ⚠ **THE COST IS MEASURED AND IT IS THE LEDGER'S ONLY SUPER-LINEAR CONSUMER** — **0.237 / 0.585 / 1.267 ms a Tick at 10,000 / 20,000 / 40,000 Citizens**, about **n^1.2**, in [`0013`](0013-tick-budget.md) with the A/B table. ***`adr/0139`'s named fallback — an index on the market row — was BUILT BY THIS TASK***, so the escape it held in reserve is spent, and ***nothing may be optimised until an attribution says where the time goes***: re-opened as a new [`0002`](0002-open-questions.md) **§B** question rather than worked around on a stopwatch reading. (`0044` **F20**–**F27**) 🟢 **TASK 5 LANDED 2026-08-26 — Evidence, and it found a DEFECT IN TASK 4 that only an instrument could have found.** `RuleEvidence` gains the blocking Bin's **Resource** and its **`BinOwnerKind`** (`adr/0137`, amended: it says *one field* and task 4 created a wait target it could not have seen — a buyer sleeps on the **District market row**, so a Resource alone reports `sundries` for an empty larder and for a District with no sellers alike). `EvidenceDump` grows a `waiting on` column reading **larder / market / broke / full**, because a column written and never read is the shape `adr/0137` itself names. 🔴 **THE DEFECT: a buyer blocked on money NEVER SLEPT.** `RuleEngine.Stop` drains the Bin it has just joined; `World.Drain` asks `Requirement` what the waiter needs; **`Requirement` walks the Rule's terms and under `adr/0050` the payment has none**, so it answered **0** and the buyer was woken by its own stop. ***A wait undone by the drain that follows it is indistinguishable from no wait at all*** — it spun on its rate for ever, paid the purchase's full cost each time, and reported itself **armed**. Measured: **323,438 stops correctly named a money Bin and the wait list held 0**; after the fix, **61**, with spinning buyers falling from 60 to 3. Fixed by `RuleEngine.PoolDraw`, which prices the money leg from the **market row** — ⚠ **derivable only because `adr/0167` put the price on the row rather than on the seller**, so `adr/0063`'s *derived rather than stored* is kept rather than excepted. 🔴 ⚠ **`adr/0137` PREDICTED THE OUTCOME AND NAMED THE WRONG MECHANISM** — it said the cheapest implementation *subscribes to nothing*, so *subscribes to something* read as compliance, and this session concluded in writing that the half was satisfied before running anything. ***A prediction naming the wrong mechanism gets checked off by the wrong evidence***, which is `adr/0093` at its sharpest. ⚠ **Task 4's F26 cost figures are now an upper bound** — both A/B arms shared the defect, so they are not withdrawn. ⚠ **Two readout bugs fixed beside it**: `ok` printed over a live pressure clock (now `woken`) and `larder:` printed against a **full** Bin (now `full:`). **No State Hash moved** — `PoolDraw` is reached only by a `pool` term and `provisioned.toml` is the only file with one. (`0044` **F20**–**F33**) 🟢 **TASK 7 LANDED 2026-08-26, OUT OF ORDER AND AT THE USER'S INSTRUCTION — a shop can now GO BROKE, which is the world [`0037`](0037-goods-between-buildings-the-district-pool.md) task 10 has been waiting for and the thing this row's gate column called *not a gate*.** [`adr/0169`](../docs/adr/0169-a-standing-cost-needs-a-counterparty-so-a-trade-pays-rates-until-there-is-a-supplier-to-pay.md): a trade carries one recurring cost, a **levy to the treasury**, chosen by the user from three counterparties as *rates now, goods later*. ⚠ **The counterparty is structurally required rather than modelled** — money is conserved, so `RulesetLoader`'s **refusal 4** rejects a local money input with no matching output: *a cost paid to nobody is a leak, not a cost*. A **landlord** is `unbuilt` (a Building holds no money, `adr/0113`) and a **supplier** is buildable-and-unbuilt (a Hinterland is a price ceiling, not an actor), so the treasury is the only money-holding actor a shop can reach. ✅ **Eleven lines of TOML and NO ENGINE CHANGE** (`0044` **F35**) — tasks 1 and 4 were load-bearing: `ApplyTenancies` derives a Rule's subject from its **local** terms against the kind's declared Bins, so ***the trade owns the levy because the kind declares the till***. 🔴 **THE ORDER CHANGED BECAUSE TASK 6 WAS ABOUT TO BE DESIGNED AROUND THIS TASK'S ABSENCE** (`0044` **F34**): `adr/0163`'s claim reads a shop's *failure pressure*, and until today no shop could be under any — the user's *"are you recommending we sacrifice depth of the engine just because we haven't built the mechanisms yet?"* is `adr/0070` aimed at a session that had cited it three messages earlier. 🔴 **THE FIRST LEVEL SHIPPED WAS PRESENT, CORRECT AND UNOBSERVABLE** (`0044` **F36**): at `amount = 2048` — ~6% of a measured median shop's revenue — **no shop in the world blocked on money at any Tick**, and every assertion anybody would think to write passed. Raised to **8192** against the measured median; bankruptcy is now a **tail event**, 2 of 20 live shopfronts starving at end of run, treasury 4,702,208, money conserved. 🔴 **THE MEASUREMENT ALSO SAID *27 TENANCIES ENDED* AND THAT WAS NOT ABOUT SHOPS** — `ZoneRuleEngine.Condemn` walks `World.Occupants` and a Business occupies through `World.BuildingBusinesses`, which **nothing walks**, so ***a shop can go broke and cannot be turned out***; the 27 were dwelling evictions off a 32-Tick tenant clock (found by the milestone-17 session, verified here, `0044` **F42**). ⚠ **The test passed for the wrong reason twice first** (`0044` **F37**) — a shopfront opens at **zero** and cannot sell until the watershed gives it a District, so its first levies fail *by construction*; the assertion that means anything is *it once held a levy's worth and later could not pay one*. **Both `rate` and `amount` filed in [`0002`](0002-open-questions.md) §D1** with milestone 27 and `adr/0168`'s migration as their triggers. 🔴 **`adr/0168` on `milestone-17-decline-and-cleared-land` REFUSES `condemn_after` BY NAME**, and this file states it twice and reasons from it — routed to that branch under `adr/0073` rather than worked around (`0044` **F39**). ⚠ **Moves the hash** on `provisioned.toml` only, which no golden fixture runs. |
| **18** | Needs and the coarse Day wheel — *wheel half only* | ✅ assessed 2026-08-21 — nothing names one | [`0036`](0036-the-coarse-day-wheel.md) | 🟡 **SCOPED 2026-08-21, out of sequence.** It is the repair for milestone **20**'s `adr/0011` gate, which is why it was scoped early |
| **24** | Terrain, Shocks and the Intensity Dial — *terrain half only* | ✅ assessed 2026-08-22 — nothing names one | [`0042`](0042-terrain-and-the-land-rows.md) | 🔵 **SCOPED 2026-08-22, out of sequence, and SPLIT in the scoping.** Terrain has one producer and **no upstream at all**, which is why it can be built beside 12. 🔴 **It is two milestones — settled by [`adr/0154`](../docs/adr/0154-milestone-24-is-two-milestones-because-a-dial-cannot-scale-a-figure-nothing-authors.md), which is [`adr/0124`](../docs/adr/0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)'s own revisit trigger firing**: [`adr/0131`](../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md) authors every Hinterland field a **Shock** moves and every figure the **Intensity Dial** scales at **13**, **15** and **16**, and a `[[hinterland]]` block carries three keys, none of them among the five. **Shocks, Disasters, the Dial, Modes and the lock policy are UNPLACED** (`0041` **F2**). 🔴 **Sealing has THREE blockers and `adr/0124` enumerated two**: `MapLayers.Seal` has **no `src` caller**, so `LayerCellTable.Sealing` is a saved, hashed column that is identically zero on every world this build can generate — and both named blockers are downstream of it (`0041` **F3**). ✅ **DECISION 1 SETTLED THE SAME DAY** ([`adr/0155`](../docs/adr/0155-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)): the milestone's central term was defined nowhere — `terrain suitability`, named in six documents with no `CONTEXT.md` entry for it or for **Terrain** — and it is renamed **Base Fertility**, **Ruleset data keyed by terrain type**, with the stored per-Cell column holding the **type**. 🔴 **`adr/0124`'s baked column is superseded and the NAME is what produced it**; it sensed a real hole and named the wrong occupant, since `CONTEXT.md` → Sealing has needed a terrain-type column all along. ***A badly named term is a design defect waiting for somebody to reason from the name*** (`0041` **F4**). ✅ **DECISION 1b SETTLED THE SAME DAY** ([`adr/0156`](../docs/adr/0156-fertility-composes-with-weights-and-only-one-of-them-is-a-number-anybody-chooses.md)): **weights, following `MapLayers.Desirability`**, with Base Fertility a fraction at `1.0` fully fertile so Fertility is a proportion. 🔴 **`w_s` is DERIVED and gets no key** — full Sealing means every Tile built on and therefore no farmland — so the decision opens **one** §D1 row and not two. ⚠ **Unweighted, a Tile count of 0–1024 outweighs a pollution stock of about 12 by ~85:1**, which is representation and not design. ✅ ***The scale was decided by the readout*** — `adr/0022`'s *"41% — ground sealed 12%"* panel falls out with no conversion. **Negative is kept and does not clamp**, because Sealing decays and the ordering between exhausted Cells is what the recovery arc reads. ✅ **DECISION 2 SETTLED THE SAME DAY** ([`adr/0157`](../docs/adr/0157-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md)): **height is generated and stored nowhere** — the generator reads it and keeps only its outputs. 🔴 **The finding is the trace**: of `adr/0021`'s four jobs for height, routing and speed are excluded by that ADR, land value has **no height term**, and earthwork needs a construction cost that does not exist — leaving **maximum buildable grade, which is a refusal**. ***Without terraforming, terrain is a wall***, which `adr/0021` rejects by name. ⚠ **Terraforming is NOT a verb in `01 §2` and `01` never mentions it** — filed as an open question — and `06`'s inventory row claiming it *Placed: 24* is split and filed in [`0012`](0012-corpus-audit.md). ⚠ **The ~512 MiB per-Tile figure corroborates and is not the reason.** ⚠ **The generator version's absence is a decision and not a hole** — [`adr/0111`](../docs/adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md), and a placeholder would invert the guard (`0041` **F5**). ✅ **DECISION 3 SETTLED THE SAME DAY**, by an **amendment to [`adr/0021`](../docs/adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) rather than a fourth ADR**, because the design content is `adr/0157`'s and a second home for one decision is [`0012`](0012-corpus-audit.md)'s Cause 1. 🔴 **`adr/0021`'s checkable rule was not checkable**: *"if a terrain value is read inside a Tick phase, something has gone wrong"* would have gone red on the generator that satisfies it, since world creation is a `CommandKind.Populate` command dispatched **inside Phase 0** (`0041` **F6**) — and ⚠ **nothing enforces phase discipline in this build at all**, `TickPhase` being referenced only by its own file and `Simulation.cs`, the same standing as `05 §4`'s **lint 4**. ✅ **Restated against STATE: *terrain height is not state***, which [`adr/0157`](../docs/adr/0157-height-does-not-ship-until-terraforming-does-because-terrain-without-a-price-is-a-wall.md) is what makes true. ***A rule about what may be read every Tick is enforced by what exists, not by where the reader stands.*** ⚠ **The mechanical check is OWED with terraforming**, whose *seed + edits* save stores heights and makes the forbidden read reachable again. ✅ **Placement: a pass of its own, called from `SyntheticCity.PopulateInto` before `LayLand`**, with `RoadGenerator.LayInto`'s already-populated refusal — and it is nearly free because **nothing in `LayLand` consults terrain**: roads span water, Woodland is not an obstacle, and buildable grade does not ship. ✅ **TASK 2 DONE 2026-08-23** (`a23b46f`, re-baselined `79efc64`): the terrain generator, the dense per-Cell type column, the `[[terrain]]` Ruleset table and `rulesets/varied.toml`. 🔴 **Its finding is that the column's home was a THIRD option and both candidates were wrong** (`0041` **F8**) — the sparse one writes nothing at decision 3's settled position, because terrain runs before `LayLand` and the Cell table has **zero rows** then; the dense one costs four Layer passes the whole map in every world, MEASURED at about **2.5 ms → 114 ms** on the land value pass — ⚠ **on a contended machine, so an upper bound and not a quotable figure**. ✅ **`TerrainCellTable` is a table of its own**, dense, slot = `CellGrid.Index`, allocated entire in its constructor; `LayerCellTable` stays sparse. ***A Layer row means something happened here and a terrain row means the world exists; the two are per-Cell alike and their lifetimes are opposite.*** ⚠ **The generator authors NO number** — every quantity derived, so no new §D1 row was opened. ✅ **TASK 5 DONE 2026-08-23** (`6f9187c`): the `throw` in `MapLayers.Fertility` is now the composition, `base − base·Sealing/1024 − w_p·pollution`, with `long` intermediates and saturation at the `int` bounds — it **moves no State Hash and needed no re-baseline**, because nothing is stored and nothing is scheduled. **One §D1 row set**: `[layers] fertility_pollution_percent` = **4**, stated in `rulesets/varied.toml` alone, ⚠ **ANCHORED on `adr/0022`'s *"41% — ground sealed 12%, pollution 47%"* specimen and not measured**, with milestone 24's long run named as the ratifier. 🔴 **The producer has no consumer** — no farm Rule, no panel, no Layer — so every assertion in `FertilityTests` is arithmetic and none can fail because the city is wrong (`0041` **F9**). ⚠ **A whole percent is a coarse unit here**, one step being 0.12 of the scale under a strong plume, so the ratifier may reopen the **unit** rather than the value. 🔴 **THIS CELL'S PER-TASK NARRATIVE STOPS AT TASK 5 AND THE TASKS DID NOT** — it is `plans/0012` **Cause 1** in the one document that is allowed to hold slice status, so read [`0042`](0042-terrain-and-the-land-rows.md) and not this. ✅ **COMPLETE 2026-08-24: every task and all twelve decisions done.** 6a and 6b built water, its catchment and a Water Body's Bin; 8a, 8b and 9 built Sealing's decay, Woodland's regrowth and the Hazard Region; **runoff** gave the Bin an inflow and **task 7** composed `− w₅·shoreline`, so `02 §2.4` is three of four terms and only amenity is left. 🔴 **Dumping is deliberately UNBUILT** — it needs a `Scope` reaching a Water Body, and a Bin can *fail* where a Map Layer cell cannot. |
| **13**–**17**, **19**–**23** | — | — | [`06`](../docs/06-roadmap.md) | Not started. **`06` holds them and no plan document is owed until a row is next.** 20–23 are sequenced provisionally, pending sessions **E** and **G**. ⚠ **24 left this row 2026-08-22** — it was scoped out of sequence and has its own row above |

**Where the Phase 2 rows came from.** 5a–11 and the gate column were carried out of
[`0000`](0000-board.md) on 2026-08-22, which had been holding them because this document declined
them. The board keeps a **pointer** to this table and no copy of it.

**Running in parallel, on their own track:**

| # | Spike | Gate | Note |
|---|---|---|---|
| **S2** | Routing — travel-time matrix first, then HPA\* versus DSDV distance-vector | cleared | **The project's top risk.** Headless, needs no Godot. It decides whether 1M is reachable and it owns the **pathfinding cluster** size — which `adr/0040` decoupled from Chunk size, since the cluster is derived and rebuilt while the Chunk is in the save. **Planned in [`0010`](0010-s2-routing.md)** now that slice 1 has reported |
| **S1** | 20k Buildings via chunked `MultiMeshInstance3D` | none | Track B. Godot only |
| **S3** | One data panel with a live multi-series graph | none | Track B. Godot only. **The spike most likely to be skipped and most likely to change the decision** |
| **S0a** | The world at target size — 1M Citizens in `Borough.Headless` | cleared | **DONE.** The tables hold 1M in 86 MiB with an order of magnitude spare, and 100,000 Ticks at the target run in 11.75 s. It found that **run mode had never had a city in it** — capacity, zero rows — so every Tick figure before it was taken over an empty world. Numbers and six findings in [`spike-results`](../docs/spike-results.md) → *S0a* |
| **S0b** | The Tick with work in it — Event Wheel, Bin Rules with wait lists, a Sweep Rule pass, a routing load | ~~🔴 slices **7**, **9**, **10**~~ ✅ **cleared** | ✅ **DONE, in three of its four clauses**, and this row said *"not run, and not runnable"* until 2026-08-14. **A Tick with work in it is 8.72 ms at 1M — 55.9% of the budget at 4×**, and it is the only Tick figure this project has ever taken from a real running city ([`spike-results`](../docs/spike-results.md) → *S0b*). The **routing load could not be measured in situ** and is [`0013`](0013-tick-budget.md)'s standing gap; pollution decay could not be reached either, because `rulesets/minimal.toml` emits none, and is re-owned by **task 10b**. ⚠ **Both halves of the gate were false when this was read**: slices 7, 9 and 10 are all marked done *in the table above this one*, and `0002` §E and `spike-results` have recorded the spike as done since before that. **A gate is discharged by the work and struck by somebody, and only the first happens on its own** |

### The hash-moving queue

> ✅ **ITEM 6 SHIPPED 2026-08-13, AND THE FLIP IT GATED WENT IN THE SAME DAY — `CellGrid.WorldCells` is 512.** ~~⚠ **REOPENED AGAIN 2026-08-12 by session J, with item 6 — and this one is a gate on a decision that is
> already taken.**~~ [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md)
> settles the map at **`CellGrid.WorldCells = 512`** — 16384² Tiles, 65.5 km — and the constant **has not
> been flipped**, because flipping it today would generate **525,312 Street Segments and 2,626,560 Lots**
> against the **225,000** Lots `World` allocates for a 1M city.
>
> **Item 6 is `RoadGenerator` scoping its lattice to developed land**, and it is a defect before it is a
> feature: `adr/0021` states that *"memory and save size scale with developed area, not with map area"*,
> and the generator is the one structure in the build that makes that false. It lays
> `(WorldTiles ÷ block_tiles + 1)²` nodes at world creation regardless of what is built. At 128 Cells it
> could not be noticed, because a 16 km map is one a city genuinely does pave. ***A structure that
> contradicts a claim only at a scale nobody has run is a claim with no test.***
>
> ~~**It is gated in turn, and the gate is a design question rather than code.**~~ **⚠ UNGATED
> 2026-08-12 by session P, and the paragraph below was wrong in both halves.** `plans/0002` ledger #2 is
> **closed as refused** — the map is open, the player lays every Segment
> ([`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)) — so there
> is no design question left in front of this item. And **the generator does not lay anything at world
> creation**: it has exactly one production call site, `SyntheticCity`, reached only by
> `CommandKind.Populate`, *"a verb no player has"*. A player's world has had **no roads at all** since 5a,
> so `adr/0021` is not violated where this paragraph said it was.
>
> **Item 6 is therefore smaller and entirely mechanical: `SyntheticCity` should pave the area it
> populates rather than the map.** Capping it is now *correct* rather than the workaround
> [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
> forbids, because the design question it was standing in for has been answered rather than deferred.
> **It carries one more edit**: `CellGrid.cs`'s own comment says the lattice is laid *"at world
> creation"*, and that sentence is what both this item and `adr/0089` reasoned from — a defect of
> `adr/0073`'s class, routed here on the day rather than worked around.
>
> *Original paragraph, kept because a gate struck for the wrong reason is the failure `plans/0020` warns
> about:* ~~What the generator should lay at Tick zero is `plans/0002` **ledger #2** — *open map, or
> progressive land unlock* — which has carried a recommendation (*unlock by serviceability*) and no
> decision since session three. Under `adr/0070` the unlock rule is **undesigned, not refused**, so the
> answer is to design it. **Do not cap the generator and move on.**~~
>
> Item 6 moves every State Hash and re-records all three golden baselines, so it is one commit of its own
> and it must not ride along with a slice.

> **✅ BUILT 2026-08-13, and it moved every State Hash and all three golden baselines exactly as
> predicted.** `RoadGenerator.LayInto` takes an **extent in Tiles**; `SyntheticCity.PavedTiles` derives
> it from `world.Lots.Rows.Capacity`, which is `World`'s own **225 Lots per 1,000 Citizens**, and paves
> the smallest square lattice that yields at least that many. The golden fixture's world goes from
> **16,641 Nodes and 33,671 Segments to 36 and ~60**. At 1M the derivation asks for **150 blocks a
> side, 4,800 Tiles** — more than the 128-Cell map has, so it clamps and **nothing at target scale
> moves today**; on a 512-Cell map it is **8.6% of the area**, which is the buildable fraction
> `adr/0089` reasoned about. `CellGrid`'s *"at world creation"* comment is corrected, so
> **`adr/0089`'s stated blocker is discharged and flipping `WorldCells` to 512 is now a one-line commit
> that nothing stands in front of.**
>
> **The item's largest finding is that `arterial_count` is a per-map count, and it is why it is now
> zero in both shipped Rulesets.** A population-derived extent cannot carry a count of Arterials chosen
> against a 4,096-Tile map — eight of them across a 160-Tile lattice is a motorway every 20 Tiles.
> **But the fix is not to derive the count**, because an Arterial should not be there at all:
> [`adr/0077`](../docs/adr/0077-a-road-edit-is-one-segment-and-the-player-lays-streets-only.md) refuses
> Arterials in `CommandKind.Connect`, `adr/0090` says the generator makes land and the player makes
> every road, `adr/0014` grants an Arterial no frontage, and `RoadKind.Arterial` is constructed in
> exactly one place in the build — inside `RoadGenerator`. **It is a player tool nothing in the
> simulation can produce, sitting in the one structure the player does not author.** The 240-configuration
> sweep of 5a already measured those eight Arterials severing **0.0%**, so they were paying nothing
> either. `rulesets/severance.toml` keeps its sixteen: that file exists to demonstrate Severance and
> says so in its own header, and a demonstration is allowed to build what a city's generator must not.
>
> **Three test findings, and all three are the same shape: an assertion whose premise was the old
> geometry rather than the mechanism it names.**
>
> - **`PlacementLongRunTests` and `ZoneRuleLongRunTests` were asserting a distribution property from a
>   single draw.** Both bound the Unplaced Pool's drift over a 100,000-Tick tail, and the scoped
>   geometry pushed the golden seed's drift from within tolerance to **+8.9%**. Measured across ten
>   seeds the scoped world's drift band is **−3.4% … +1.5%**, *tighter* than the full map's
>   **−5.3% … +3.9%** — so the geometry is better behaved and the golden seed is an outlier under it.
>   Both tests now sweep **five seeds and assert the mean**, which is `--roads`' own lesson from 5a
>   arriving in the test suite: ***a generator whose output cannot be varied cannot be characterised***,
>   and a test that draws once is a test that cannot tell an outlier from a regression.
> - **`LotLongRunTests` named block (60, 60), which is off the edge of a lattice that is now 5×5.** A
>   coordinate literal is a premise about map size, and this one had been true for as long as the map
>   was the lattice. It is (3, 3) now — inside the lattice and outside the land the populator carves.
>   Its two peak assertions also gained `+ LotsOnAFace`, a constant **the file itself declares as
>   *the whole amplitude this run's oscillation can have*** and had never used in them.
> - **`RoadLongRunTests` and `RoadSeveranceTests` reached the graph through `SyntheticCity`**, so
>   scoping the populator silently shrank the graph they were characterising. Both now call
>   `RoadGenerator.LayInto` at `CellGrid.WorldTiles` directly, and the two that need Arterials restore
>   them with a `with { ArterialCount = 8 }`. **A test that wants a full map should ask for one**; going
>   through the populator to get it was reading a city's geometry as if it were the map's.
>
> **The golden fixture is 4,000 Citizens, up from 1,000, and it had to move.** A 1,000-Citizen world
> paves a **5×5** lattice, and the golden session's eleven zone commands and its Connect nodes were
> authored against rows and columns that no longer exist. Raising the population to 4,000 gives a
> **10×10** lattice with room to re-author them into rows 5–9, away from the strip the populator carves.
> **This is slice 10 task 11 again** — *a baseline records what a run did, so a change that narrows what
> the run reaches is invisible in it by construction* — and `GoldenSessionCoverageTests`, written for
> exactly that, is what held the line.
>
> ✅ **THE MAP FLIP, `CellGrid.WorldCells` 128 → 512, shipped the same day and is one line.**
> [`adr/0089`](../docs/adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md), whose *What
> building it found* section carries the record. **Four things, and the first is about `05 §4`.**
>
> - **It moved no State Hash.** All three golden baselines reproduced unchanged, where this item, the
>   ADR and `CellGrid`'s own comment all predicted every hash would move. `05 §4` says *a change is an
>   optimisation if the State Hash is unchanged, and a design change otherwise, however it was
>   motivated* — so by that test the map size reads as an **optimisation**, which it plainly is not.
>   ***The rule tests whether a change moves this city; a map size is a bound on the cities that are
>   reachable***, and a fixture in one corner never approaches it. It is neutral **because item 6
>   landed first** — before that the generator paved the map and this would have moved everything, so
>   item 6 did not only unblock the flip, it made the flip free. Neither document predicted that of it.
> - **It cost 11.6 MB at 1M, not the ~4 MB `adr/0089` accounted for** — 192,780 KB resident against
>   204,412 KB, same command, both measured. The ADR named four derived `int[]` residency arrays as
>   the set of structures scaling with map area; there is a fifth, `StreetGrid`'s node and edge index
>   at ~3.2 MB, **correctly sized from the map** because a player may lay a Street anywhere under
>   `CommandKind.Connect`. A correction to an inventory, not a defect — and *an inventory stated as
>   complete is a claim*.
> - **Three fixtures were laying at map extent and the flip broke all three the same way.**
>   `rulesets/severance.toml` stranded **0%** of pedestrians on the worst of eight seeds, so **the file
>   that exists to demonstrate Severance had stopped demonstrating it with no number in it changed**;
>   the walk-search benchmark's graph went 16,641 → 263,169 nodes, sixteen times the fixture for a
>   benchmark claiming to time *the shipped city*; and the `[roads]` loader's two spatial maxima were
>   `[InlineData]` literals of 4,096 and 4,097 — of which the refused one failed loudly and ⚠ **the
>   accepted one stayed green while ceasing to test a boundary at all**. Each now states the extent it
>   was characterised at, and `severance.toml`'s header **names the test's symbol rather than restating
>   the figure**. ***A paved extent is not a map size***, three files further on than item 6 found it.
> - **One cost moved and is routed rather than noted.** The Map Layer residency knee goes **256 →
>   8,192** emitters, so the headroom against a 1M city's 120,001 Buildings falls from **469× to
>   14.6×**. [`0013`](0013-tick-budget.md)'s Layer row rests on that headroom; the conclusion survives
>   and its ground is an order of magnitude weaker. `adr/0073`.

> **⚠ And it widened what the run reaches, which nothing was watching for.** At 4,000 Citizens the
> shipped 20-minute Commute Budget **starts refusing walks** — `JobAssignmentTests` had a test asserting
> it refused none, written by 5b-bis task 4 so the fact could not rot, and it failed. Measured across
> populations under the shipped geometry, `beyond` over 512 Ticks runs **0, 0, 3, 213** at 1,000, 2,000,
> 4,000 and 8,000 Citizens: the fixture sits on the **first rung that refuses anything at all**. So the
> committed baseline now exercises a branch it could not before, **acquired as a side effect of a change
> made for another reason**. Slice 10 task 11's finding runs *forwards* as well as backwards, and the
> only reason this was noticed is that somebody had written the negative assertion down.

> ✅ **ITEM 7 SHIPPED 2026-08-13 — the clock half. The Goods rescale is the separate commit it was always going to be.** ~~⚠ **ITEM 7, added 2026-08-13 by session P: `TICKS_PER_DAY` and `WHEEL_SIZE` go to 2048.**~~
> [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md).
> `Ticks.PerDay` and `EventWheel.Size`, two `const`s, and **the change is one line each and the
> consequences are not**. Every hash-bearing number denominated in **Ticks** keeps its value and changes
> its in-world meaning, which is a design change under `05 §4` even though no Ruleset text is edited —
> so all three golden baselines re-record.
>
> **It carries a second, separable edit that must not share the commit**: the Ruleset **Goods quantities
> scale ×4**, because a Rule moving *n* units every 64 Ticks now moves them over four times more in-world
> time. Cadences do not move and quantities do; keeping the two commits apart is what makes the split
> auditable afterwards, and it is the same reason item 6 stands alone.
>
> **Three things to check rather than assume.** `CommuteRoster` allocates `Ticks.PerDay` buckets and
> should simply get smaller. `LayerSchedule.DefaultPollutionDecayTicks = Ticks.PerDay` is Day-denominated
> and correct unchanged. And `TravelTime`'s `RawPerDay` is the conversion every Commute Budget goes
> through — **1.4222 Ticks per clock minute**, down from 5.6889 — so `--trips`' finest resolvable
> duration goes from 0.176 to 0.70 min, and 5b-bis task 7's sub-Tick truncation defect would be four
> times larger if it had not been fixed at the source.
>
> **It is not gated and it is not urgent.** Nothing downstream is blocked on it; what it unblocks is a
> playtest, which is the only instrument that can ratify the value. Do it before the first play session
> and not before that.

> **✅ BUILT 2026-08-13.** `Ticks.PerDay` and `EventWheel.Size` are 2048, all three golden baselines
> re-recorded, 1,279 green. Full record in
> [`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
> → *What building it found*. **This item said "the change is one line each and the consequences are
> not", and the consequences were larger than the three it named.**
>
> - **The ADR's rescaling inventory was wrong in two places, and both would have shipped in silence.**
>   It says the Goods quantities are *"the only rescaling"*; there were **three**, and two move **down**
>   by four while the Goods move **up** by four. `Speed.PerKilometrePerHour` was a **literal** 48,000 —
>   left alone, an authored `walk_speed_kph = 5` would have walked the city at **1.25 km/h**, with
>   nothing failing to compile and no Ruleset text wrong. And `revisit_ticks` / `pollution_decay_ticks`
>   are durations the Rulesets themselves call *one Day*, which kept at 8192 would quietly have meant
>   **four**. ⚠ **`revisit_ticks` was actively misclassified by the ADR's own table** and is struck from
>   it, with the user in the room: `adr/0059` makes it a duration, and the cost — the derived sample
>   quadrupling, so the Zone Rule costs ×4 per Tick and exactly what it did per Day — is accepted.
> - **The fix in every case was a derivation rather than a new value**, and `TravelTime` had always
>   written its half as one — its own remark says *"the same derivation `Speed` runs, and if one moves
>   both move"*. ***One fact, two files, an expression in one and a value in the other; the value is the
>   copy that drifted.*** `plans/0012` Cause 1 in code. The metre and the second now live in one place
>   each, which is what let the factor be written as arithmetic at all.
> - **`adr/0071`'s two illustrations moved in opposite directions.** A 32-Tile Street at 50 km/h goes
>   0.87 → **0.22** Ticks, so the sub-Tick argument is four times more load-bearing — under whole-Tick
>   resolution every Street would now be *free*. A 5 km/h walk goes 3.66 → **14.65** Tiles/Tick, so the
>   flooring error falls 20% → **4.4%**. ***One constant, two of an ADR's arguments, opposite
>   directions.***
> - **Five tests asserted a number where they meant a relation, and one read an instant where it meant a
>   run.** Three were `[InlineData]` literals, where an attribute argument cannot be an expression. The
>   sharpest is `GoldenSessionCoverageTests`: it found **zero** commute Trips, and the reason is that the
>   departure window fell 2,731 → 683 so the session now covers **every** departure phase instead of
>   three quarters. ***The baseline got better and the assertion measuring it went to zero.***
> - **Two long-run tests broke without measuring anything wrong.** `LayerLongRunTests` read `0 → 0` for a
>   contraction because the pollution tau fell 128 → 32 and the field had finished converging before its
>   window began. `LotLongRunTests`' vacuity guard fired on a run where **41 of 97** edits carved Lots,
>   because it read the first sample alone. ***A guard against vacuity that reads one sample can itself
>   be defeated by timing*** — the fourth single-draw failure in two days.
>
> ✅ **THE GOODS ×4 RESCALE SHIPPED 2026-08-13 as the separate commit**, and it turned out to be **two**
> rescales rather than one. Employment on the shipped Ruleset fell 6,844 → 2,791 of 10,000 over 2,048
> Ticks, which is the `revisit_ticks` decision rather than the clock — the Zone Rule surveys the whole
> city every Day instead of every four, so it condemns four times as fast per Tick.
>
> - **The Bin capacities had to move with the Goods, and nothing predicted it**: `sundries` 12 → **48**,
>   `repairs` 1 → **4**. ***`amount` and `capacity` are both in Goods and they belong to different rows
>   of `adr/0094`'s own table.*** `amount` is **Days**-denominated — hold Goods per Day, so the number
>   goes ×4. `capacity` is **Ticks**-denominated — what has to hold is **firings held**, and firings ×
>   `rate` is a *duration* — but it is written in Goods, so keeping that duration still **requires**
>   moving the number. ***The unit of a quantity is not its denomination***, which is `revisit_ticks`'
>   lesson one level down: **twice in one build, a class was read off a surface form** — a key name, then
>   a unit. Left at 12 the larder held **one** firing where it held four, and a Bin that must be *exactly
>   full* for one `consume` to succeed is a knife edge.
> - ⚠ **The knife edge exposed a live defect in the wake path — item 8 below, filed unfixed.**
> - ⚠ **AND THE COMMIT SHIPPED A CLAIM THAT IS FALSE, CORRECTED THE SAME DAY BY MEASURING IT.** It said
>   *a dwelling now holds 90 in-world minutes of sundries where it held 22.5, which is a change to the
>   world rather than a neutral rescale*. **It is behaviour-neutral**: the pre-rescale Ruleset and the
>   shipped one run for 4,096 Ticks give a **byte-identical 86-line census** — buildings 1,201 → 642, the
>   Unplaced Pool, rule evaluations, Trips, every row — and only the State Hash moved, because Bin levels
>   are four times larger. The larder is 4 firings × `rate` 32 = **128 Ticks**, and it was 128 Ticks
>   before the clock moved as well; **90-vs-22.5 is HEAD against the *pre-clock* world**, which is the
>   clock's already-recorded cost in one more place. The sentence never said which comparison it was.
>   `plans/0012` **Cause 5** — ***a number that is one half of a ratio says which half*** — committed
>   inside the same session that coined it, in a paragraph warning about it.
> - **Nothing came out of proportion because everything in that file is denominated in *firings***,
>   `condemn_after` included, which says so in its own comment: a dwelling's whole life is 4 missed
>   `upkeep` firings × `rate` 16 = **64 Ticks**, 11.25 → 45 in-world minutes by the same ×4.
> - **A sweep then found the larder is a *cost* dial, not a balance dial.** At capacities 24, 36, 48 and
>   96 **no row of the census moves**; only `rules due` and `rules evaluations` move, and they move **up**
>   — 20,134 → 25,023 at the first reading going 48 → 96 — because a deeper larder gives `restock` more
>   headroom to keep working. ***A bigger buffer costs more, not less.*** The larder decides nothing
>   because **it is deeper than the building's lifespan**: nothing produces `repairs`, so `upkeep`
>   condemns every dwelling at 64 Ticks and a 128-Tick larder absorbs a shock that never arrives.
> - **48 stands and no number was tuned to make it stand.** Restoring 22.5 in-world minutes needs either
>   2 firings held — one above a known crash — or `consume`'s `rate` quartered, which `adr/0094` refused
>   on cost. ***And 22.5 was never chosen***: it is `capacity = 12` in a file whose first line says it
>   models no city, so restoring it would be `adr/0070`'s shape — compensating for something that was
>   never a decision.

> ✅ **ITEM 8 CLOSED 2026-08-22 — IT WAS ITEM 11, AND THERE WAS NEVER A SECOND DEFECT.** No line of
> simulation code was written to close it: item 11's fix, shipped two days earlier, already had. **The
> reserved design question does not need answering** — neither candidate repair is needed, no Readout
> edge is issued and [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md)'s forbidden
> move is not made.
>
> **Measured, on item 8's own recorded reproduction**, walking both wait lists after every Tick rather
> than at end of run. With `RuleEngine.Stop`'s drain in place: **0 mis-parked waiters on each of 20,480
> Ticks**, against 434 waiters legitimately parked — so the check was exercised and not vacuous. With
> that one line removed: **a mis-parked waiter on 3,903 of those Ticks**, first at **Tick 2**, worst
> **11** simultaneously.
>
> ⚠ **The *falling requirement* hypothesis is REFUTED, and the way it survived is the lesson.** It was
> what remained once session P eliminated two others, and the elimination was sound — the defect it
> named simply was not among the candidates, because item 11's Phase 3 settle-order race had not been
> found yet. ***A surviving hypothesis is only as strong as the enumeration it survived***, and this
> item said so about itself in its own last paragraph. It was right to, and it is still what nearly
> bought a cadence sweep.
>
> ⚠ **The *parked for ever* reading was one long episode sampled from inside it.** With the drain
> removed, the longest single waiter's mis-park runs **3,901 consecutive Ticks** — Tick 2 to Tick 3903,
> most of two in-world Days — and is then rescued by a write to its Bin. Session P read the invariant at
> Ticks 512, 2052 and 4096, and all three fall inside that window. ***A defect sampled only at the end
> of a run cannot be told from a long one***, which is item 11's *shortness of the run was the sampling
> and never the cause* arriving from the opposite direction: there, a run too short to heal; here, three
> runs that all stopped before the healing.
>
> ✅ **This discharges item 11's open *measurable* clause as a side effect.** That row records *that it
> can strand a Rule permanently is an argument from the mechanism and not a measurement*, and names the
> world that would settle it — one whose producer stops writing the Bin. **This is that world**, and
> 3,901 Ticks is the number. The mechanism's claim was right; it now has a measurement under it. Item 11
> had measured 36 spurious parks all rescued within ~30 Ticks and could not tell how bad it got.
>
> **The regression test is `WaitListWakeTests`**, which runs the reproduction and asks after **every**
> Tick — 4,096 of them, one second, assertion tier. ⚠ **An end-of-run assertion here would pass against
> the bug it exists to catch**: with the drain removed the run is dirty on 95% of its Ticks and *clean
> at the last one*, which is why the recorded reproduction stopped reproducing and why this sat filed
> for nine days. It builds its Ruleset by editing the shipped `minimal.toml`, so it tracks that file
> rather than pinning a copy of it. **Moves no hash** — nothing in the simulation changed.
>
> *Original entry:*
>
> ⚠ ~~**ITEM 8, added 2026-08-13 by session P: a waiter whose own requirement falls is never re-checked.**~~
> **Filed unfixed, with a reproduction**, because it was found while building item 7's Goods half and
> fixing it there would have put an unrelated mechanism in that commit — this queue's own rule, and the
> reason items 1 and 2 did not share a re-record.
>
> **Reproduction.** `rulesets/minimal.toml` with the Goods amounts at ×4 and the Bin capacities left at
> their old `12` and `1`; 4,000 Citizens; 1,024 Ticks or more.
> `Invariant.WaiterIsBlockedByTheBinItNames` — item 0, the end-of-run tier — fires. The shipped
> capacities of 48 and 4 do **not** provoke it, which is why the queue can carry it rather than the
> commit.
>
> **What was observed, on four Bins simultaneously and stable from Tick 512 to 2052 and again at 4096:**
> queue **depth 1**, Bin `level 12`, `headroom 0`, waiter requirement **12**, and
> `RuleEngine.BinStillBlocks` returning **false**. A single waiter, asking for exactly what is there,
> parked.
>
> **Two hypotheses were tested and refuted**, and they are written down so nobody pays for them twice.
> *A derived requirement falling to zero* — refuted, the probe reads `requirement=12 buildingLive=True
> occupants=3`. *`World.Drain`'s collective budget legitimately parking a second waiter behind a first* —
> refuted by the depth of 1.
>
> **What is left is a trigger gap, and it is `adr/0063`'s own doing.** That ADR made the wake predicate
> read **live** state — the requirement is `RuleEngine.Requirement`, derived from a Readout on every
> call — and the only thing that calls `Drain` is a **write to the Bin**. So a waiter parked when its
> requirement was high and the Bin could not meet it stays parked for ever once its **own** requirement
> falls, because the Bin never changed and nothing else re-examines the queue. ***A live predicate with
> an event-driven trigger is only correct if every input to the predicate is an input to the trigger***,
> and occupancy is not. This is stated as the surviving hypothesis rather than as a diagnosis: the
> falling-requirement history was **not** directly observed, it is what remains once the two above are
> gone and the only two inputs are the Bin and the Readout.
>
> ~~**It moves the hash** — waking a Rule Instance is a change to what the city does — so it re-records.
> **Position: last.** Nothing shipped provokes it, and the two candidate fixes are a real design
> question rather than a correction: re-examine a Bin's queue when a **Readout** the requirement depends
> on changes (a write nobody currently issues, and `02 §5` has no such edge), or sweep the wait lists on
> a cadence, which is [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md)'s
> forbidden move — a Bin Rule becoming a Sweep Rule is a change to the city, not an implementation
> detail. **Do not pick one inside another item's commit.**~~ ⚠ **STRUCK 2026-08-22 — the whole
> paragraph.** It moved no hash and re-recorded nothing, because the repair had already shipped inside
> another item's commit, which is the one thing this paragraph told the reader not to do. ***An item
> that reserves a design question keeps reserving it until somebody re-runs the reproduction***, and
> what made this expensive is that the reservation read as a standing obligation rather than as a
> hypothesis with a shelf life.

> ⚠ **ITEM 9, added 2026-08-15: `CommandKind.Populate` and `CommandKind.Connect` are welded shut, so
> there is no door in the build through which a player-shaped network gets a population.**
> **Filed unfixed the same day** in [`0002`](0002-open-questions.md) §C under
> [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md),
> and promoted here because the workaround has become the **second copy of the populator**.
>
> **The mechanism, named rather than described** (`adr/0093`). `RoadGenerator.LayInto` **throws** when
> `graph.Segments.Rows.LiveCount != 0`, and its message is right: it is a world-creation pass, and
> editing a standing graph is `Connect`. `SyntheticCity.PopulateInto` calls it **unconditionally, at the
> top, before it builds a single row**. So Connect-then-Populate throws, and Populate-then-Connect gives
> a generated lattice with edits on it, which is not the thing. ***The defect is not the refusal.*** It
> is that `PopulateInto` does **two jobs** — it makes **land** (`LayInto`, then `Subdivide`) and it makes
> **people** (Buildings, Households, Citizens) — and no caller can ask for the second without the first.
>
> **Why it is a queue slot rather than a note.**
> [`adr/0090`](../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md) gives the
> generator land and the player every road, so **a player-shaped network is every city anyone will ever
> have**, and the only populator in the build cannot populate one.
> `ConnectedCityCongestionTests.Populate` is `PopulateInto`'s three loops with the road pass removed, and
> says so in its own remark. ***Two ways to make a population is `plans/0012` Cause 1 with both copies
> executing*** — which is the sentence `SyntheticCity` uses **in its own source**, in the comment
> deleting the workplace stride, about a hazard it has since become.
>
> ⚠ **Skipping `LayInto` is not the repair, and this is the thing to check before starting.** `Subdivide`
> is keyed on `StreetGrid.Blocks`, which is `WorldTiles ÷ block_tiles + 1` — **a property of the map and
> the Ruleset, never of what is laid** — so on a Connect-laid world it sweeps the whole map's lattice and
> carves Lots against whatever Segments it happens to find, zoning them as it goes. **The land half is
> wrong for a Connect world twice, independently, and only the first failure announces itself by
> throwing.** *(The keying is read; the sweep is not run.)*
>
> **It moves no hash on its own.** Every fixture and the golden session populate an **empty** graph and
> would take the same path. That is a fact about this item and **not an argument about when to do it**:
> [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
> retires hash movement as a scheduling term in both directions, and this item's claim on the queue is
> what it unblocks rather than what it costs.
>
> **What it unblocks, and both are ledger rows rather than opinions.** [`0002`](0002-open-questions.md)
> §D names *a long run over a city whose Streets were laid by `CommandKind.Connect` and deliberately
> under-provisioned* as the ratifier for **`[traffic]`'s α, β and clamp** and for **`[households]
> car_ownership_percent`**, and names the same world as the **producer for the Microscopic Cap's demand
> side** — the quantity that has had *three owners and no producer*, and that is a **fixture rather than
> a milestone**, which is why no milestone ever held it. `06` lists it under *Obligations no milestone
> can hold*. `ConnectedCityCongestionTests` proves such a world **can** be built; what it cannot do is
> build one **through the build's own door**, and a ratifier reached by copying a private method is a
> reading over a city nothing else can reproduce.
>
> **Position: ahead of item 8, and it is the only entry in this queue that is neither a correction to a
> shipped mechanism nor a design question.** The two candidate shapes — a parameter on `PopulateInto`
> separating land from people, or a second entry point — are a **command-model** question if the verb
> count moves and a one-signature question if it does not. **Do not pick one inside another item's
> commit**, and **do not delete `LayInto`'s refusal to get there**: that refusal is the only thing
> standing between a second `Populate` and a doubled lattice.

> ✅ **ITEM 9 BUILT 2026-08-15, the same day it was filed. 1,438 green, no baseline re-recorded, and
> the verb count did not move.**
>
> **The repair is the split and neither candidate shape.** `SyntheticCity.PeopleInto` is public beside
> `PopulateInto`; the land half (`LayInto` + `Subdivide`) and the shared derivations
> (`RefuseIfPopulated`, `Households`, `WantedBuildings`) are private helpers, and `PopulateInto` is now
> three calls. **A payload on `CommandKind.Populate` was considered and refused**: that verb *"is
> expected to be deleted when the player can grow a city instead of declaring one"* in its own remark,
> so a payload would have rested on something already scheduled to go. This is
> [`adr/0080`](../docs/adr/0080-phase-4-does-not-wait-on-a-trip-generator-and-a-trip-is-entered-by-command.md)'s
> precedent for `TripPurpose.Commanded` — **a test affordance rather than the only door**.
>
> **Hash-neutral exactly as predicted**, which is worth one line rather than none: every call site takes
> the same path in the same order, and all three golden baselines are untouched on disk. *(The suite's
> one red is `TrafficLongRunTests`, another session's in-flight 5c task 8, failing on its own flatness
> assertion by 166 vehicle-Ticks before this work started.)*
>
> **It retired two copies rather than one, and the second announced itself.**
> `ConnectedCityCongestionTests` also held a copy of `SyntheticCity`'s private `DwellingKind = 1`, under
> a remark that called itself `plans/0012` **Cause 1** and bounded the risk rather than removing it. It
> went with the populator copy, because `PeopleInto` knows the kind and the fixture no longer needs to.
> ***A duplicated mechanism drags its constants across with it, so retiring the mechanism retires
> them*** — and the honest note that had been written beside the constant is what made it findable.
>
> ⚠ **The sharpest finding is that the weld was hiding an incomplete clamp, and writing the test is what
> found it.** `PeopleInto` clamps the Building count to the standing Lots, and at **zero** Lots that
> clamp divided by zero in the Household loop. `PopulateInto` **structurally cannot reach zero** —
> `Subdivide`'s degenerate branch lays one Lot per wanted Building when there is no lattice, so it always
> returns at least one — so the case was unreachable for as long as the only caller was the one that laid
> its own land. ***A mechanism with one caller has only the edge cases that caller can produce, so opening
> a door widens the input domain before it widens anything else.*** It is **refused rather than degraded**,
> on `Subdivide`'s own stated principle: *a populator that makes no rows answers the sizing question with
> an empty world and reports success*. The land is the caller's to lay, so the caller is told it laid none.
>
> **Acceptance is `PopulatorDoorTests`, four tests, and one of them is negative on purpose.** A
> Connect-laid city can be populated; the generator **still refuses** a world that has Streets; the people
> half refuses a world that already has people; and it refuses a world with no Lots. The second exists
> because the obvious way to close this gap was to soften `LayInto`, and this queue entry said in as many
> words not to.

> ✅ **ITEM 10 FIXED 2026-08-22**, together with item 12, because the two are one defect wearing two
> faces: a refusal written in the wrong place, so the designer meets it as a crash. `Session.Resume`
> now catches on the read, prints the message and returns `Refused`. ⚠ **The guard is narrow and the
> narrowness is the point**: every refusal on that path is an `InvalidOperationException`, and
> `InvariantViolationException` derives from `Exception` rather than from it, so an invariant firing
> during a load still unwinds loudly — *that is a defect in this build, not a verdict on the file*.
> **A test asserts that hierarchy directly**, because widening the catch would silently turn every
> invariant failure on the load path into *this file cannot be resumed*, which is worse than the defect
> being fixed. `IOException` is caught beside it so a mistyped path is a message too. **Moves no hash.**
> *Original entry:*
>
> ⚠ **ITEM 10, added 2026-08-18 by milestone 8 task 10: every load refusal reaches the user as an
> unhandled exception with a stack trace.** `Session.Resume` has no catch, so *this is not a borough
> save*, *truncated save*, a format-version mismatch, a world-creation-constant disagreement and the new
> State Hash mismatch all arrive as a .NET crash dump with the message buried in line one.
>
> **It moves no hash** — it is in `Borough.Headless` and touches no state — and it is in this queue
> rather than in a milestone because it is a defect with no owner otherwise. ***A refusal a user meets as
> a stack trace is a refusal that reads as a crash***, which is the opposite of what
> [`adr/0086`](../docs/adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md)'s
> refusal messages were written to do: `SaveHeader.Read`'s own remark says *"the order of the checks is
> the point... a reader that checked them in any other order would report the wrong cause for the right
> refusal"*, and then the runner reports the right cause under a heading that says the program broke.
>
> ⚠ **It is filed rather than fixed, deliberately, and the rule is [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)'s.**
> The finding came out of a save commit and the cause is in the runner, so fixing it there would have put
> a `Borough.Headless` error-handling change inside a `Borough.Core` format change — where a reviewer
> looking for the format would not find it and a reviewer looking for the runner would not look. **The
> repair is small**: `Session.Resume` catches `InvalidOperationException`, prints the message, and exits
> non-zero. What it must not do is catch broadly enough to swallow a `Core` invariant throw, which is a
> crash and should look like one.

> ~~**✅ THE QUEUE IS EMPTY. All four items shipped 2026-08-10.**~~ **REOPENED the same day by session N
> task 2, with items 4 and 5 — and ✅ BOTH SHIPPED 2026-08-11, so it is empty again.** They were [`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)
> and [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md).
> Items 0–3 all shipped; what the first four leave behind is one standing gap rather than one standing
> item: **lint 6, save/reload equivalence, is still unbuilt**, and three of the four walked past it — see
> the two paragraphs at the foot of this section.
>
> **Item 5 is the first entry in this queue that is a *mechanism* rather than a correction**, and that is
> the argument track doing what [`0000`](0000-board.md) asks of it rather than a scope leak: every earlier
> item fixed something already built, and this one builds `02 §5.2` step 2, which the design has specified
> since it was written and no milestone has ever owned.
>
> **And being a mechanism rather than a correction is exactly where it went differently.** Items 0–4 each
> shipped the thing their ADR described. Item 5's ADR was **wrong about the outcome it would produce and
> wrong about the numbers it would need**, and both errors are the same one: a mechanism that does not
> exist cannot be reasoned about from the outside, which is
> [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) running *forwards*.
> The rule was written for absences generating design positions; this is an absence generating a
> **prediction**, and it has the same base rate.

**Phase 1's code is no longer *closed but for task 11*: there were four items, three of them re-recording
the same golden baselines, and only item 3 is left.** Session **N** produced the second, third and fourth
([`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md) task 1 →
[`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)),
which is the argument track moving work into the code track rather than generating more argument.
**No gate is red on any of them.**

> **✅ ITEMS 0 AND 2 SHIPPED 2026-08-10, together, as the correction below said they would have to.**
> `Invariant.WaiterIsBlockedByTheBinItNames` is registered in the end-of-run tier and
> [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)'s
> predicate is in `World.Drain`: the budget is `LevelAt`/`HeadroomAt`, the requirement is
> `RuleEngine.Requirement`, and `RuleInstance.shortfall` is gone. **Item 1 — slice 10 task 11 — is
> what remains, and it is now the only thing in front of Phase 1's close.**
>
> **One prediction in the ADR did not hold, and it is recorded rather than quietly corrected.** It said
> *all three golden baselines re-record*; **only `session-trace.txt` moved.** `world-hash.txt` is
> unchanged because `GoldenFixtures.Build()` raises Buildings through `Buildings.Create` rather than
> `World.CreateBuilding`, so that fixture holds **no Rule Instance rows at all** and the deleted column
> was under no committed hash there. The observation worth keeping is the coverage one: **the
> `rule_instance` table's saved columns are covered by the session trace alone**, and the artefact that
> exists to cover what a session cannot reach does not reach them.
>
> **⚠ CORRECTED BY BUILDING IT. Items 0 and 2 are one commit, not two, and the evidence is empirical.**
> This table first placed the invariant first *because it was free* — hash-neutral, and expected to pass
> on all existing content. **It was built on 2026-08-10 and fired on the committed golden session.** The
> golden session reloads into `rulesets/minimal-tuned.toml` at Tick 128; the one number that file changes
> is `restock`'s output amount, **1 → 2**; and a producer with a headroom deficit of 2, drawn down one
> unit at a time by the occupancy-1 Buildings a Zone Rule creates, is never woken. At Tick 256 the trace
> holds a `restock` asleep on headroom **3** against a recorded shortfall of **2**.
>
> **So the invariant cannot be committed green without item 2**, and the two ship together. The
> alternative — retuning `minimal-tuned.toml` to change a number that does not provoke it — is refused by
> name: this corpus has already shipped **four** instances of a green suite agreeing with the code instead
> of the claim, and editing a fixture to stop a real violation being reported would be the fifth.
>
> **The invariant paid for itself in minutes, which is the argument for building the specified-and-unbuilt
> checks rather than the argument against.**

| | Item | Moves the hash | Why this position |
|---|---|---|---|
| **0** | ~~**`adr/0033`'s satisfiability invariant**~~ — `Invariant.WaiterIsBlockedByTheBinItNames`, end-of-run tier — **DONE**, green with item 2 | no, on its own | **Ships with item 2**, for the reason in the correction above. Specified in **three** documents (`adr/0033`, `02 §10`, [`0008`](0008-tick-and-replay.md)) and built in none until now. It is narrower than `adr/0033`'s wording in what it inspects and stronger in what it catches: *asleep on a Bin that has stopped blocking it* also catches a waiter subscribed to the wrong Bin, which *would this Rule fire* is blind to |
| **1** | ~~**Slice 10 task 11 — `revisit_ticks`**~~ ([`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)) — **DONE**, two baselines re-recorded | yes | **Was: the only defect of the three live *now*.** `sample` was an absolute count, so a Lot was visited once per 0.12 Day at 1,000 Citizens and once per **117 Days at 1M**, and at target scale the shipped Ruleset built **nothing**. It now raises **2,898 Buildings** in 2,000 Ticks at 1M, and the Tick got **8% cheaper** doing 117× the Lot evaluations. **Its own finding is the one to carry**: the golden session silently stopped covering the create branch, because a derived sample of 1 at 132 Lots never lands on a cleared Lot in eight triggers — so the session was lengthened 256 → 2,048 Ticks and a test now asserts both branches ran ([`0014`](0014-zone-rules-and-the-sweep-family.md) → *Task 11 as built*) |
| **2** | ~~**`adr/0063`'s wake predicate**~~ — derived requirement, level budget, `RuleInstance.shortfall` deleted — **DONE**, one baseline re-recorded | yes | **Ships with item 0**, which is red without it. ~~It cannot manifest until `pool` exists~~ — **struck: it is manifesting in the committed baseline now.** Acceptance is `BinWaitListTests`, which needs no `pool`: three `Deposit(1)` calls against a waiter requiring 3, plus the `Withdraw`/headroom mirror |
| **3** | ~~**`adr/0064` + `adr/0065`, together**~~ — `Bins.Capacity` is `derived AND rebuilt`, `level` and `capacity` are `long` along the whole write path, the end-of-run derivation check is `Invariant.BinCapacityMatchesItsDeclaration` (id 29) — **DONE**, one baseline re-recorded | yes | **Last, and one commit rather than two, which is this queue's rule inverted for the reason the rule exists.** The two touch **the same two columns** — one changes a declaration, the other a width — so a baseline that moved for both is attributable to neither if they ship apart and the new trace is wrong. Nothing either fixes is live: `0064` fails under a **patch**, which cannot happen before the game ships patches, and `0065`'s overflow is unreachable while the only Readout is `occupancy`. ⚠ **One of the two obligations was already discharged and the ADR said otherwise**: the `(kind, Resource)` loader refusal has existed since slice 7 task 8 with **no test**, so `adr/0064` read the suite, found nothing and recorded a live defect that was not one. Amended there; the test ships here; the finding is in [`0018`](0018-session-n-the-bin-the-pool-and-the-economy.md) → *Tasks 3 and 4's implementation record* |
| **4** | ~~**`adr/0068`'s occupancy capacity**~~ **DONE 2026-08-11**, one baseline re-recorded. ⚠ **The ADR's shape prediction was wrong and is amended there**: there is **no column at all**. A Bin needed one because `HeadroomAt` is hot-path; this is read at a guard that runs once per placement, and the Building already carries its `Kind` — so the row *loses* an obligation rather than gaining one, and the end-of-run derivation check mirroring id 29 is struck. *Original entry:* **`adr/0068`'s occupancy capacity** — a `[[kind]]` declares `occupants`, the column is `Rows.Derived`, rebuilt at load and inside `Adopt`; the cap is a write-site guard at `Place`; an over-capacity Building evicts the overflow into the Pool by a draw under a **new `purpose_tag`**; `SyntheticCity.HouseholdsPerBuilding` stops being a `const` | yes | **Before item 5, because item 5 has nothing to fill without it** — vacant capacity is not a quantity until a capacity is declared. It is otherwise the smaller of the two and follows item 3's shape exactly, which is the point: `Rows.Derived`, `RebuildCapacities`' sibling, and an end-of-run check that the rebuild ran. **The eviction path is the one new thing** and it is `World.Evict`, which exists. ⚠ **Expect `world-hash.txt` not to move again** — `GoldenFixtures.Build()` raises Buildings through `Buildings.Create`, and on the evidence of items 2 and 3 the session trace will be the only artefact that notices |
| **5** | ~~**`adr/0069`'s placement pass**~~ **DONE 2026-08-11**, both baselines re-recorded and the Census gained a fourth metric family. ⚠ **Its stated acceptance was wrong in both halves and the ADR now says so.** The five-sixths equilibrium does **not** close — 83% homeless becomes **53%**, and the residue is `rulesets/minimal.toml` demolishing its whole housing stock on purpose. What the pass actually fixes is **vacancy**: 45% of the stock stood empty while 70% queued, and it is now 10%, which is what `PlacementLongRunTests` asserts. And **three numbers had to be chosen**, not none — `adr/0059`'s precedent derives the *sample* and leaves the duration it is derived from free. `revisit_ticks` shipped at one Day, measured badly, and is **1024**. *Original entry:* **`adr/0069`'s placement pass** — a sampled Phase 6 step **ahead of the Zone Rules**, draining the Unplaced Pool into vacant declared capacity; `ZoneRuleEngine.Create` stops calling `World.Place` and `PurposeTag.PoolDraw` moves to the new pass | yes, twice over | **Last, and it is the largest thing in this queue.** It changes the **phase ordering**, which `02 §1.1` calls the determinism contract, so it is hash-bearing by construction rather than by consequence. **Its acceptance is the five-sixths equilibrium closing without a number being tuned**: `ZoneRuleLongRunTests` reports ~300 of 360 Households homeless today, and eviction and re-housing using the same door is what balances the cycle. **Look for the derivation before authoring a cadence** — `adr/0059`'s precedent is a revisit period over the Pool with the count derived, in which case `0002` §D **loses** a question rather than gaining a row |
| **11** | ~~⚠ **`Invariant.WaiterIsBlockedByTheBinItNames` fires on EVERY Ruleset for any run shorter than 64 Ticks**~~ — ✅ **FIXED 2026-08-22**, both golden traces re-recorded, `world-hash.txt` unmoved. **`RuleEngine.Stop` drains the Bin it has just joined**, and `World.Drain` is `internal` to allow it. ⚠ **The repair was the open half of this row and the corpus closed it rather than a sitting**: the alternative — re-checking a Phase 2 failure against the Future in Phase 3 — is *refused* and not merely worse, because `Apply`'s re-check **may lower and may never raise**, and a Rule rescued by a deposit applied after it decided spends a quantity that did not exist when it decided ([`adr/0049`](../docs/adr/0049-a-rules-apply-count-is-decided-against-the-past-and-settle-may-only-reduce-it.md)). `World.Wake` arms for `tick + 1`, so the fix cannot rescue a Rule into firing on the Tick it failed. ⚠ **The filing's unchecked clause is discharged**: *identically on three Rulesets* had been read on `minimal.toml` alone, and `diagnosed.toml`, `congested.toml` and `taxed.toml` all panic at `--ticks 4` before the change while **all eleven shipped Rulesets are clean after it**. ⚠ **The regression test asks the invariant after EVERY Tick rather than once at the end, and that is the whole reproduction** — a spurious park is healed by the next write to the same Bin, so an end-of-run check catches one only if the run stops while a waiter is mis-parked. ***The shortness of the run was the sampling and never the cause***, which is why this row's headline named a run length. Six seeds, because the settle order is a draw and only `seed: 4` exposes it on that fixture. *Original entry:* filed unfixed 2026-08-20 by milestone 9 task 7, ✅ **DIAGNOSED 2026-08-21**, and the **fix is held rather than owed**. 🔴 ⚠ **The cause is NEITHER of the two readings this row was filed with.** `RuleEngine.Evaluate` is **Phase 2 — *evaluate every due Rule against the Past, writing nothing*** — and Phase 3 applies the verdicts, so a Rule judged short in Phase 2 is **made whole by another Rule's deposit applied earlier in the same Phase 3**, and `RuleEngine.Stop` subscribes it anyway. `World.Drain` has already run on that Bin, against a queue the waiter had not yet joined. ***A waiter parks on a Bin that the same Tick has already refilled, and the wake it needed was spent before it was owed one.*** 🔴 **The invariant is RIGHT and the engine is wrong** — this is `05 §9`'s asleep-for-ever reached through a missed drain, which is the failure this invariant was registered to catch, arriving through the one door nobody watched: not a drain that was skipped, but a drain that was **early**. ⚠ **The repair is a design choice, which is why the fix is held and not merely queued.** The smallest is `World.Subscribe` draining the Bin it has just joined — it reuses `Drain`'s own requirement check rather than standing up a second predicate, and it never re-arms on a timer, which `02 §4.1` refuses by name. ⚠ ~~**It is held ONLY because it re-records the golden baselines while milestone 11 is re-recording the same files**~~: a collision on a shared artefact, and expressly ***not a hash concern*** — [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) forbids citing hash movement as a reason to defer, and it is not cited here. ✅ **THE HOLD IS RELEASED, 2026-08-21. Milestone 11 closed, so the collision is over and this item can be picked up.** ⚠ **Releasing it restores the design question that was underneath**: the hold was never the blocker, the *repair being a design choice* is, and ***an item held on a scheduling collision reads as blocked on the collision, so clearing it looks like clearing the item.*** What is owed before a line is written is which repair — the smallest candidate is `World.Subscribe` draining the Bin it has just joined, and it is a candidate rather than the decision. ⚠ **It was filed as *item 6* and 6 was taken** — by `SyntheticCity` paving what it populates, shipped 2026-08-13 — **renumbered to 11 on 2026-08-20 while closing the milestone.** The table above stops at the items that have a row; **items 7 to 10 live in the prose blocks below it**, so a number taken by reading the table is a number checked against part of the set. ***A ledger whose rows and whose prose hold entries of the same kind is one ledger for the purpose of numbering*** | **yes** — ~~unknown~~, answered 2026-08-21. Every candidate repair fires Rules that previously slept, so it moves every State Hash and re-records the golden baselines | **Found while loading a new Ruleset, and it is not that Ruleset's**. `dotnet run --project src/Borough.Headless -- --ruleset rulesets/minimal.toml --ticks N` panics at the end-of-run check. 🔴 ⚠ **THREE PARTICULARS OF THE ORIGINAL FILING ARE WRONG AND ARE STRUCK.** ~~*same rows every time (166, 111)*~~ — the rows move with `N`, and **so does the queue**: `N=4` → (166, 111) on **Supply**, `N=16` → (1336, 891) on Supply, `N=32` and `N=48` → (24, 17) on **Space**. ~~*clean at 64 and 128*~~ understates it — **zero violations for every `N` from 64 to 96**, so the quiet is systematic rather than sampled. And ~~*either a waiter is legitimately parked in a state the re-derivation does not reproduce … or the invariant is right and something arms wrong*~~ is a false pair; it is a third thing, in this row's second column. ***A filing records what its reporter saw, and a particular quoted from a single sighting reads as a property of the defect.*** ⚠ **The *identically on three Rulesets* half was NOT re-checked** — the diagnosis ran on `minimal.toml` only, and that clause stands as the original reporter's and not as this sitting's. **The trace, on `minimal.toml` bin 111 at Tick 1**: `DEPOSIT amount=16` runs `Drain(Supply)` against an **empty** queue, and `STOP:Supply` then subscribes instance 166 with a **requirement of 12 against a level of 16**. **Measured over 2,048 Ticks at 10,000 Citizens: 36 spurious parks** — 19 on Supply in Ticks 1–8, 17 on Space in Ticks 25–31, and **none after Tick 31**. ⚠ **All 36 were rescued by a later write to the same Bin**, up to ~30 Ticks on, so ***that it can strand a Rule permanently is an argument from the mechanism and not a measurement***. It is **measurable** ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)) and the world that would settle it is one whose producer stops writing the Bin. ✅ **MEASURED 2026-08-22 while closing item 8, which turned out to be this defect and supplied exactly that world**: on `minimal.toml` with the Bin capacities at 12 and 1, one waiter is mis-parked for **3,901 consecutive Ticks** — most of two in-world Days — before a write to its Bin rescues it. **The mechanism's claim stands and the ~30-Tick figure was the fixture's and not the defect's.** Permanent is still not shown, and after this it is the wrong question: *how long* is what the argument was reaching for, and two Days of a Building starving beside a full larder is the answer. ⚠ **Nothing in the suite runs the headless runner for under 64 Ticks**, which is why a panic on every shipped Ruleset has stood unseen. Routed here rather than worked around, under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md) |
| **12** | ~~⚠ **A Rule may name `layer = "land-value"` or `"sealing"` in its outputs, load clean, and panic the first time it fires**~~ — ✅ **FIXED 2026-08-22**, with item 10, because the two are one defect wearing two faces. `RulesetLoader.ReadEmission` refuses it at the parse site under `adr/0048`, and the predicate has one home — **`MapEmission.IsEmittable`** — asked by the loader and by `RuleEngine.Emit`'s surviving backstop, because ***a rule stated in two places is two rules and the one that drifts is the one nobody runs***. ⚠ **It gets its own sentence rather than reusing the unknown-name refusal beside it**: `land-value` is not a typo for a Layer, it *is* a Layer, so *"is not a Map Layer"* would be false — ***two mistakes that resolve to one message is a message that is wrong for one of them.*** ⚠ **`RefusalCountTests` caught the filing debt on the spot** and named it precisely: `adr/0048`'s site count moves to **118** *and* the new refusal joins that ADR's enumeration, which is the eighth recount. ***A document-to-code check is the only one that notices a number describing the build.*** **Moves no hash.** *Original entry:* — filed unfixed 2026-08-20 while closing milestone 9 | no | **A load-time refusal is missing where one already exists three lines away.** `RulesetLoader.TryLayer` (`RulesetLoader.cs:3532`) resolves all three Layer names and `ReadEmission` (`:887`) checks nothing further; `RuleEngine.Emit` (`RuleEngine.cs:859`) then throws `NotSupportedException` on anything but pollution, and its message is correct — **land value is chased towards a target and Sealing is a property of a footprint, so neither is a quantity a Rule adds per application**. So the refusal is *written*, and it is written **one Tick too late**: [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) puts validation where the Ruleset is parsed. ⚠ ***A refusal in the engine is a refusal the designer meets as a crash***, which is queue item 10 in a second place. **Found by reading the symbols while closing milestone 9, not by a test** — nothing authors such an emission, so nothing has ever hit it |
| **13** | ⚠ **The assertion tier has at least TWO intermittently-failing tests, neither caused by the change that found them** — filed 2026-08-20, **measured 2026-08-21** by milestone 11 task 1 | no | ✅ **MEASURED RATHER THAN SUSPECTED, and the suspicion was wrong.** Filed after one failure of `ZoneRuleTriggerTests.Sweeping_allocates_nothing_after_the_first_trigger` in four runs, suspecting task 1's two new calls in `World.CreateBuilding`. **20 full assertion-tier runs, sequential, baseline `35df747` in a separate worktree against the working tree**: baseline **1 / 10**, treatment **0 / 10**. ***The test fails on a tree that does not contain the change it was filed against***, so task 1 is exonerated and the flake is pre-existing. ⚠ **Sequential by design** — if a flake is load-related, concurrent runs change the thing being measured, which is `plans/0000`'s parallelism-capture rule applied to a *failure rate* rather than to a duration. ⚠ **And the probe found a SECOND one nobody had filed**: `JobSearchBoxTests.The_box_is_not_where_the_pass_spends_its_time`, **1 / 10** on the treatment side. 🔴 **The two are different animals and only one is a mystery.** The Zone one asserts `GC.GetAllocatedBytesForCurrentThread()` is **exactly unchanged** across 500 `Step` calls — a counter, not a clock, so noise is not an explanation and the standing hypothesis is a **tiered-JIT rejit allocating on the measuring thread**, which is unproven. The JobSearchBox one asserts a **`Stopwatch` wall-clock ratio** stays under 2.2, and ***its own comment predicts this exact failure rate***: *"a band transplanted from a quieter quantity fails one run in ten with nothing wrong under it."* It then failed one run in ten. **That one is arguably not a defect at all but a MISCLASSIFICATION**: under [`plans/0032`](0032-test-tiers.md)'s own axis — *what would you do on the day it failed, find out what broke or paste the new number* — a wall-clock ratio that flakes with nothing wrong under it is an **instrument** wearing an assertion's clothes, and [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)'s *a quiet machine is a control on a capture* says a test that takes a capture inside the gate has put a capture's controls on the gate. **The behaviour it guards is real and worth guarding** — box walking going from ~18% to ~28% of the pass — so the repair is to count box-walk operations, which is deterministic, rather than to time them or to retag it. ✅ **THAT HALF IS DONE 2026-08-22, and the repair was the one this row named.** `EmploymentActivity.BoxWalks` counts the walks — one per seeker to size the box, then one per candidate drawn — and the test asserts `walks == seekers × (1 + candidates)` at both ceilings, plus that the two walk totals are **equal**. Measured at 40,000 Citizens: **33,277 seekers, 133,108 walks, 4.00 per seeker, identical in both arms**, against boxes of **841** and **4,489** Cells. ***Growing the box 5.34× adds no walks at all***; what it scales is Cells-per-walk, which is geometry and derivable (112M against 598M Cells visited, printed rather than asserted). ⚠ **It is a NARROWER claim than the stopwatch made, and that is the trade taken deliberately.** The old assertion was a *share of run time* — box walking at ~18% of the pass, failing at ~28% — and **no operation count can reproduce that**, because a route search is a variable-cost search rather than one operation. What is asserted instead is the structural fact underneath it: *the pass walks the box a constant number of times per seeker, and box width does not move that constant.* ***A guard that fires on a real change every time beats one that fires on a bigger change nine times in ten.*** ⚠ **The counter counts WALKS and not CELLS on purpose** — a Cell counter would put an increment inside the loop being measured. **Verified by regression**: adding a second `CountIn` to `TryEmploy` fails the test immediately. **Moves no hash** — it is an instrument, and no golden baseline moved. 🔴 **The `ZoneRuleTriggerTests` half of this row is UNTOUCHED and still open**, and it is the one that is a mystery rather than a misclassification. ⚠ **What a flaky gate costs is not the run; it is that it trains its user to re-run rather than to read**, and the next real regression arrives wearing the same face. 🔴 ⚠ **UPDATED 2026-08-21 by milestone 11 task 9 — `plans/0035` **F33**, and the update is mostly about THIS ROW AND `plans/0002` §B HOLDING TWO HYPOTHESES FOR ONE EVENT WITH NEITHER CITING THE OTHER.** This row says *tiered-JIT rejit allocating on the measuring thread, which is unproven*; §B says *a per-thread allocation context that a collection on another thread flushes*. They are different mechanisms for the same firings, filed two days apart in two ledgers, and ***a fact with two homes is `plans/0012` Cause 1 whether or not the two agree.*** **What the evidence now says.** Task 9's longer tier fired `LayerQueryTests` once in four runs — **3,376 bytes, 1 gen0** — and reading the probe's rows for it turned up an **unread** firing at row 300 of `alloc-probe-archive/2026-08-21-cumulative-105-processes.csv`: this row's own `ZoneRuleTriggerTests`, **5,208 bytes, `0 gen0 / 0 gen1 / 0 gen2`**. `GC.CollectionCount(0)` is process-wide and gen0 is non-concurrent, so no collection completed anywhere in that window. ✅ **That refutes §B's mechanism as NECESSARY** — a jump happened with no collection to flush anything — **and leaves this row's tiered-JIT hypothesis standing**, because a rejit allocates without needing a collection. ⚠ **Standing is not confirmed**: nothing has measured a rejit inside a firing window, and the obvious machine is a run with `DOTNET_TieredCompilation=0`, which is a config a document may not quote a figure from without saying so. **What both rows agree on is the BOUND** — six firings, all under **8,192**. ⚠ **The two rows are not merged here**, because §B owns a *measurable* and this owns a *defect*; what is owed is that each names the other, and this half is done. ✅ **The re-run-rather-than-read cost this row predicts is now closed** — the eight assertions go through `AllocationProbe.Check`, whose failure message names the delta, the collection counts, the 8,192 band and the file, and says to read it BEFORE re-running. ***The 5,208 was lost to exactly the behaviour this row warned about***, which makes it the row's own prediction landing before its repair did |
| **14** | ~~🔴 ⚠ **`World.Drain` and `Invariant.WaiterIsBlockedByTheBinItNames` contradict each other, and a committed test asserts the state the invariant calls a violation**~~ — ✅ **SETTLED AND FIXED 2026-08-22, and it is the INVARIANT that narrowed.** [`adr/0063`](../docs/adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md), amended. ⚠ **The fork was decided by the ADR's own argument rather than by the cheaper repair**: making the drain skip is the behaviour its *Atomic servings* section refuses by name, so the two candidates were never symmetric and *different cities* overstated the choice. `WorldInvariants.CheckQueueStillBlocks` now asks `HeadThatShouldHaveWoken` — **the head of the list and only the head**, because the head is the whole of what the drain promises. ***The drain was right and the sentence describing it was too strong***, which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) arriving inside an invariant: a check is a description of the build, and a description can overstate. 🔴 ⚠ **THE NARROWING EXPOSED A SECOND HALF THAT NO REASONING ABOUT QUEUE ORDER REACHES, and it is the one worth reading.** Three waiters needing three each against a deposit of six: two wake, the third parks, and **the third IS the head** — yet the Bin's whole level still reads six, because `World.Wake` clears `Blocked` and arms for `tick + 1` and ***nothing is drawn until those rows run***. **The drain's guarantee is true of an INSTANT**, and a check that compares the head against the whole level is asking after the budget has gone. `RuleEngine.AccumulateClaims` derives what every armed Rule Instance will draw and subtracts it — **derived rather than stored, because the alternative is a reserved column on the Bin, which is the reservation `adr/0063` already refused arriving as bookkeeping**. ⚠ **Neither half is reached by the other's repair**: nobody was skipped in the spent-down run, and nobody was over-claimed in the starvation run. ⚠ **`RuleEngine.BinStillBlocks` is GONE rather than left standing** — the walk was its only production caller, and a predicate only a test runs is the one that drifts; its two tests now assert against `RuleEngine.Requirement`, which is what they were ever about. 🔴 ⚠ **The repair was found already written and swept into milestone 12 task 4's commit, which then did not compile on its own** — `WorldInvariants.cs` went in and `RuleEngine.cs` did not. **Split back out by amending an unpushed commit and verified by building the amended tree in a detached worktree**, because ***a commit that does not build alone is a bisect that lies***, and this row's own last clause is the rule it broke. *Original entry:* 🔴 ⚠ **`World.Drain` and `Invariant.WaiterIsBlockedByTheBinItNames` contradict each other, and a committed test asserts the state the invariant calls a violation** — filed unfixed 2026-08-22 by queue item 11, **found beside it and expressly not fixed inside it**. The drain **stops rather than skips** at an uncovered waiter, deliberately: *"skipping an uncovered waiter to reach a smaller one behind it would starve every large waiter permanently"* (`World.Drain`'s own remarks, `adr/0063`). So a small waiter queued **behind** a large one it could outrun stays parked on a Bin that covers it. `WorldInvariants.CheckQueueStillBlocks` then walks **every** waiter on the list — not the head, and with no positional budget — and `RuleEngine.BinStillBlocks` compares each requirement against the Bin's **whole** level. ⚠ **The invariant is therefore stated more strongly than the drain can deliver.** | **no** — ~~unknown~~, answered 2026-08-22 by settling it. **Narrowing the invariant moves no State Hash**: the answer the city computes is untouched and only the question asked afterwards changed. The other candidate would have moved every hash, which is a fact about that candidate and was never an argument against it ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)) | ⚠ **The probe is KEPT rather than reverted, which is the whole of the regression test.** `world.Invariants.RunEndOfRun(world)` now stands at the end of `BinTests.The_drain_stops_at_an_uncovered_waiter_rather_than_skipping_it` **and** of `The_arriving_quantity_is_spent_down_across_the_waiters_it_covers` — one line for each half, and each fires without its own repair. ⚠ **A narrowed check that reported nothing at all would pass both**, so `BinTests.A_covered_head_that_nothing_woke_is_reported` writes the violation and watches it fire; it deposits through `BinTable.Move` rather than `World.Deposit`, **because a deposit drains and a drain is what it is asserting did not happen**. ⚠ **MEASURED, not reasoned** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)): adding `world.Invariants.RunEndOfRun(world)` to `BinTests.The_drain_stops_at_an_uncovered_waiter_rather_than_skipping_it` — which subscribes a waiter needing **10** ahead of one needing **2**, then deposits **6** — fires `WaiterIsBlockedByTheBinItNames` at Tick 0. **That test asserts both stay waiting and is correct to**; the probe was reverted and is not committed. 🔴 ⚠ **It is latent ONLY because no shipped Ruleset puts two Rules on one Bin**, which [`BinWaitListTests`](../tests/Borough.Tests/Rules/BinWaitListTests.cs)' own header already says: *"under `local` scope no two Rules share a Bin they do not both own."* ***`Scope.Pool` is precisely the mechanism that ends that***, so **milestone 12 is what makes this reachable** — it must be settled before the Pool ships, not after. ⚠ **The repair is a design question and must not be taken inside another item's commit**: either the invariant narrows to the drain's guarantee, or the drain stops starving small waiters, and those are different cities |
| **15** | 🔴 ⚠ **A Ruleset edit that inserts a `[[resource]]` anywhere but the END of the file CRASHES the swap, and it crashes on the treasury before it reaches anything else** — filed unfixed 2026-08-22 by milestone 12 task 5, **found beside it and expressly not fixed inside it**. `RulesetMigration` exists for exactly this and says so in its own words — ***"the map exists because an id is not an identity"*** — and it maps Resources **by name** across a swap. `World.Migrate` then applies that map by walking `Buildings.Rows` and calling `RemapBins` per Building, **and that is the whole of where it is applied.** A `ResourceId` is the declaration's POSITION, so inserting a resource renumbers every one after it; a Treasury, Household, Business or District Pool Bin keeps the OUTGOING file's id and now names something else. ⚠ **The migration is right and its reach is short**, which is the opposite of the defect it looks like. | **no** — it is a swap-path defect, and a world that never reloads a Ruleset never meets it. ⚠ **The repair's SHAPE is a design question**: a money Bin whose Resource the incoming file dropped has no honest answer yet — `FitTreasury` and `FitBalances` *add and never remove* precisely because [`adr/0024`](../docs/adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) puts the Outside Connection between money and non-existence, so a remap that frees such a Bin destroys conserved money | ⚠ **MEASURED, not reasoned** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)). A throwaway test on `rulesets/minimal.toml` — populate 1,000 Citizens, then `Adopt` the same file with one `[[resource]]` inserted before `repairs` — throws `NotSupportedException` from `World.RebuildCapacities`: ***"bin 0 holds a non-conserved Resource and is owned by Treasury rather than a Building"***. **Bin 0 is the treasury's money Bin**, whose id 3 was `money` and is now `repairs`. No `[districts]`, no District, no Pool — ***the defect is milestone 10's and task 5 merely walked into it.*** The probe was deleted and is not committed. 🔴 ⚠ **It is latent because nothing in the suite or the shipped files inserts a resource mid-list**: `minimal-tuned.toml` changes one number, and every test that swaps either appends or drops. ***A file edit nobody has made is not a file edit nobody will make***, and the crash is what a designer meets. ⚠ **Task 5 widened the blind spot without widening the defect**: a Pool Bin is unremapped for the same reason, and because its ceiling derives from its OWNER rather than its Resource it does not throw — ***it comes back holding a stock of whatever the id now names, silently***, which is the worse of the two failures and the one no test would report | 
| **16** | ~~🔴 ⚠ **A District's extent keeps Cells whose Buildings have all been demolished, and the end-of-run invariant fires on every run of `rulesets/twinned.toml` past its SECOND re-evaluation**~~ — ✅ **SETTLED AND FIXED 2026-08-22, and it is the INVARIANT that narrowed.** ⚠ **The mechanism was right and the sentence describing it was too strong**: the extent is derived on `[districts] revisit_ticks`, so ***between two evaluations it describes the city as of the last one.*** Measured rather than argued — a Cell demolished at Tick **1,152** keeps its membership until Tick **2,048**, and `DistrictWatershed.Evict` then clears it, so the eviction pass was working the whole time. ***Asking *every Cell names built ground* of the WORLD asserts that the cadence never ran.*** ⚠ **The tempting repair — evict at the demolition site — was refused, and the reason is symmetry**: a Cell that *gains* its first Building also waits for the cadence to join a District, so making removal instant while addition stays cadenced is an asymmetry with nothing behind it, and it would leave `revisit_ticks` doing something other than what `adr/0134` says it does. ***A structure derived on a cadence is stale between evaluations, and that is what a cadence IS*** — a Map Layer is stale between diffusions and nobody calls that a defect. **`Invariant.ADistrictCellNamesALiveDistrictAndBuiltGround` is now `ADistrictCellNamesALiveDistrict`** and keeps only the half that is true at all times; the built-ground half moved to where it is true, `ADistrictCellNamesBuiltGroundWhenEvaluated`, **a post-condition of the evaluation** asserted from `Evict` through `WorldInvariants.DistrictExtentIsBuiltGround`. ⚠ **It lives on `WorldInvariants` rather than inside `DistrictWatershed` so a test can write the violation and watch it fire** — the eviction pass frees anything filed over unbuilt ground before the check runs, so the violation is unreachable from outside. 🔴 ⚠ **A committed test asserted the state the narrowed check now permits**, exactly as item 14's did: `A_membership_row_over_unbuilt_ground_is_reported` demolished and expected a report. It is inverted rather than deleted — `A_demolition_leaves_the_extent_stale_and_that_is_not_a_violation` — because ***the thing it was wrong about is worth a test of its own.*** ✅ **The real regression test is the one nothing had**: `The_shipped_district_world_survives_three_days_of_its_own_cadence` drives `Simulation.Step` over `twinned.toml` for **three Days**, and three rather than two because two only reaches the first re-evaluation and says nothing. *Original entry:* end-of-run invariant fires on every run of `rulesets/twinned.toml` past its SECOND re-evaluation** — filed unfixed 2026-08-22 by milestone 12 task 6, **found beside it and expressly not fixed inside it**. `Invariant.ADistrictCellNamesALiveDistrictAndBuiltGround` requires that every `DistrictCellTable` row names ground with a Building on it, which `adr/0134`'s *the extent is built Cells only* is the reason for. `DistrictWatershed.Reconcile` migrates a boundary between two **living** Districts and bounds that movement by `[districts] migrate_cells` — ⚠ **but ground that stops being built is not a migration**, and there is no unconditional path that drops it. **It is task 3 or task 4's defect and task 6 merely ran the file for longer than anything had.** | **no** — ~~unknown~~, answered 2026-08-22 by settling it. **Narrowing the invariant moves no State Hash**: the answer the city computes is untouched and only the question asked afterwards changed, and the four-Day headless run reprints `0x7DEE62DBA74F52DA` at Tick 6,144 — byte-identical to the run that panicked there. ⚠ **The State Hash was never going to report this and that is the point**: a membership row over unbuilt ground folds exactly as well as one over built ground. *The original entry:* Dropping an unbuilt Cell inside the migration bound spends the bound on demolition, which is not what [`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md) says the bound is for; dropping it outside the bound is a second path through the extent. ⚠ **Those are different cities and the choice is a design question**, which is why this is a queue row rather than a line in task 6 | ⚠ **MEASURED, not reasoned** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)). `dotnet run --project src/Borough.Headless -- --ruleset rulesets/twinned.toml --citizens 2000 --ticks N` is **clean at N = 2048** and panics at **N = 4096** with *row 34, row 1536* — the second `Require` in `WorldInvariants.DistrictMembershipNamesLiveDistrictsAndBuiltGround`, whose `other` argument is the Cell index. **Reproduced at task 5's commit against the unmodified `twinned.toml`**, in a detached worktree, so it is not task 6's and not the Ruleset edit's. 🔴 ⚠ **NOTHING IN THE SUITE CATCHES IT**: the District tests build their worlds in code and evaluate once or twice by hand, and no golden trace or long run uses a file that states `[districts]`. ***The only shipped world with Districts in it has never been run for two Days***, and that is the finding behind the finding |
| **17** | ✅ **STRUCK 2026-08-26 BY MILESTONE 26 TASK 4, which is the code this row was waiting for, and the retirement is now CONFIRMED BY A RUNNING WORLD rather than by argument.** `Scope.Pool` resolves, so a Pool Bin can hold a waiter and the row's own *cannot be constructed* clause has expired — and the defect is still unreachable, for the **second** reason rather than the first: `World.RetirePool`'s `Bins.Move` pair is guarded by `held != 0`, and a market row holds nothing by construction, which `ProvisionedRulesetTests.It_runs_and_the_market_trades` asserts **every Tick of 6,144**. ⚠ ***So the transfer is dead code rather than a missing drain***, and what would revive the defect is a design change putting stock back in the row — which is `adr/0139` reversed, not a bug. ⚠ **The `WakeAll` on the dying Bin is doing real work now and was doing none before**: a waiter woken there re-checks and its `pool` term re-resolves against whatever District its Cell has joined. *The retirement note follows.* ⚠ **RETIRED BY DESIGN 2026-08-22 rather than fixed, and it must be STRUCK DELIBERATELY when the code lands** — [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) leaves no stock in a market row, so there is no transfer, no heir Bin and no drain to miss. ***A defect that disappears because its method disappeared is still a defect until the method does.*** *Original entry:* 🔴 ⚠ **`World.RetirePool` raises the heir Bin's level with a raw `Bins.Move` and never drains it, so a waiter on the heir sleeps through the stock it just inherited** — filed unfixed 2026-08-22 by **session U** ([`0038`](0038-session-u-the-pool-or-the-seller.md)), found while reading the wait list for a design question and **not while looking for a defect**. `World.Deposit` and `World.Withdraw` are the only two doors that move a level, and each calls `World.Drain` immediately after — `BinTable._level` is private and `BinTable.Move` is `internal` **precisely so the drain cannot be bypassed**. ⚠ **`RetirePool` is inside `World`, so it bypasses it from the one side the encapsulation does not cover**: `Bins.Move(bin, -held); Bins.Move(into, held);` with a `WakeAll` on the **dying** Bin and **nothing at all on the heir**. ***The guard was built against outside callers and the violation came from inside the house.*** | **no** — and it cannot move one yet, because it is unreachable: `Scope.Pool` throws, so no Rule can name a Pool Bin, so a Pool Bin's wait lists can never hold a waiter. ⚠ **It becomes reachable on the day task 7 lands**, which is the same task that would make it fire | ⚠ **READ, not measured** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)) — and **it is not yet writable as a test**, because the state that would fail it cannot be constructed while `Scope.Pool` throws. 🔴 **That is what makes it worth filing rather than fixing**: a defect with no reachable failing state is exactly the kind that ships. ⚠ **It survives the sitting's outcome either way** — under Pool-as-inventory `RetirePool` keeps moving stock and keeps needing the drain; under stock-with-sellers the method mostly disappears and **takes the defect with it**, which is a reason to settle the design before repairing the code rather than after |
| **18** | 🔴 ⚠ **`RunnerTests.cs:938` writes a FIXED path into `/tmp`, so two test hosts on one machine collide — and the collision CRASHES the host rather than failing a test, aborting the whole run** — filed unfixed 2026-08-23, found by running milestone 12's gate in a private worktree **to escape exactly this class of problem**, and colliding anyway. `A_save_that_cannot_be_read_is_refused_rather_than_thrown` does `Path.Combine(Path.GetTempPath(), "borough-runner-tests-not-a-save.borough")` — **a bare fixed name** — writes 27 bytes, and deletes it in a `finally`. ⚠ **Its own neighbours show the convention it breaks**: `CommuteDumpTests:148`, `TripDumpTests:129` and `:174`, and `SaveLongRunTests:96` all disambiguate by `Environment.ProcessId`, and `EvidenceDumpTests:187` by a content hash. ***It is the only fixed temp path in the test project.*** 🔴 **The observed error is not the one this test's own content produces** — *"truncated save: 60 more bytes were declared and the file has ended"* is a file whose header **parsed**, and `"this is not a borough save."` has no header at all. ***So the reader was handed a file this test did not write***, which is the collision rather than an inference about it. ⚠ **A worktree does not help and that is the finding's edge**: `/tmp` is per-machine, not per-tree, so isolating the source tree isolated nothing that mattered. ***The shared state was never only the tree.*** | **no** — it moves no State Hash. It costs a **run**, which is worse than a failure: `Test Run Aborted` after 2,078 of 2,082 tests, so ***the four that never ran are invisible and the abort reads as a crash rather than as a verdict.*** | ⚠ **READ from the abort message and the neighbouring call sites, not reproduced** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)) — and **the concurrent writer was NOT identified**, because nothing checked for a second `dotnet test` at the time and it cannot be reconstructed afterwards. 🔴 **A re-run will very likely pass, which is the reason to file rather than retry**: ***a defect whose reproduction depends on a race somebody else was running is one that gets retried away.*** ⚠ **The repair is one line and the harder half is the second question** — whether a load refusal escaping into the host is right, given `Session.Resume`'s guard is **deliberately narrow** so an invariant failure still unwinds loudly, which its own sibling test documents as the property being bought |
| **19** | ⚠ **A corrupted HANDLE column is refused by the resolver and never reaches [`adr/0112`](../docs/adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md)'s hash check, and WHICH refusal you get depends on what the corrupt bytes happen to address** — filed unfixed 2026-08-25 by milestone 26 task 2, **found beside it and expressly not fixed inside it**. The State Hash folds a handle as *the target row's monotonic never-reused id*, so folding one **resolves** it: a flipped byte in a handle column throws `StaleHandleException` (or an index error) out of `Rows.Resolve` before `SaveFile.Read`'s comparison runs. 🔴 ***THE LOAD REFUSES EITHER WAY, so this is not a hole in invariant 6*** — a corrupt save is still refused, and that is the property that matters. What is fragile is anything that names *which* refusal: `SaveHashTests.A_flipped_byte_in_the_body_is_refused_by_the_load` pointed its flip at `household.bin_head` and asserted the hash message, which made a save-format assertion depend on what the bin table happened to contain. ⚠ **`adr/0165`'s land-use split changed those contents, the same flipped byte started addressing a freed slot, and the test went red for a reason that had nothing to do with saving.** The flip now targets `lot.zone`, a value column nothing dereferences, and the test's remarks carry this finding. | **no** — it is a refusal-path defect on a corrupt file, and a world that never loads a damaged save never meets it. | **After anything that needs a save to be trustworthy, and it is not urgent.** ⚠ **The repair's SHAPE is the open part**: a handle column could be bounds-checked against the target table's slot count *before* the fold, which would make every corruption reach the hash check and produce one refusal message instead of three — but that is a check on the hot fold path, and whether it belongs there is a question about what `HashState` is allowed to cost. ***Nobody has priced it.*** |
| **20** | ⚠ **A Ruleset naming a `pool` term while stating no `[districts]` table LOADS CLEAN and throws the first time the Rule is reached** — filed unfixed 2026-08-26 by milestone 26 task 4, **found by building the refusal it belongs beside**. `RuleEngine.Bin` throws `NotSupportedException` naming the file's omission, which is a correct message arriving at the wrong moment: [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) puts **every refusal in `Borough.Formats`, where it is parsed** and where the file and the line still exist; this one fires from `Borough.Core` mid-Tick, with neither. 🔴 **The condition is PERMANENT and that is what makes it a loader's question** — a Ruleset's tables do not change under it, so *this file can never run this Rule* is knowable at parse time. ⚠ **It must not be confused with the TRANSIENT case beside it**, which is correct as it stands: a Building raised between two watershed evaluations stands in no District for up to `[districts] revisit_ticks`, and its Rule **fires at zero applications and re-arms**. ***One is a file that is wrong and one is a world that is early***, and a loader check that could not tell them apart would refuse working Rulesets. | **no** — it refuses either way and no world reaches the Rule. What changes is *when* and *what the message can name*: a load-time refusal quotes the line, a mid-Tick throw quotes a rule id. | ⚠ **READ from the two call sites, not measured** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)) — and `RuleEvaluationTests.A_scope_this_build_does_not_have_is_a_named_hole` currently **asserts the throw**, so the repair moves that test rather than deleting it. ⚠ **It also moves `adr/0048`'s recorded `Refuse(` count**, which `RefusalCountTests` holds to the loader, so the ADR is edited in the same commit |
| **21** | 🔴 ⚠ **THE DISTRICT POOL'S PRICE HAS NEVER MOVED, ON ANY WORLD, BECAUSE THE REPRICE READS A BIN THAT [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) EMPTIED** — filed unfixed 2026-08-26 by milestone 26 task 8, **found by the instrument that task built and by nothing else**. `World.RepriceDistrictPools` passes `Bins.LevelAt(bin)` as `MarketRuleset.Reprice`'s `level`, where `bin` is the market row's own Bin — and a Pool is a **market and not a store**, so that Bin is empty in every row of every world by construction. With `level` zero the cover is the rate, the target is `ceiling × rate ÷ rate` = the ceiling exactly, and a price that **opens** at the ceiling cannot move. **Measured: eight rows on `rulesets/provisioned.toml` at 2,000 Citizens, zero price changes over 24,576 Ticks.** ⚠ **`adr/0139` predicted this repair in its own words** — *"only its caller changes"* — and the caller is the one thing nobody changed, because the same sentence also said the `Bin` column would go and keeping it was **right**. 🔴 **IT IS NOT A ONE-LINE FIX AND MUST NOT BE TAKEN AS ONE**: summing what the reachable sellers hold is one line and moves the State Hash, but ***what cover MEANS for a market that is not a store*** — inventory, production rate, or something else — is **arguable** and owes a record ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)). Both ADRs are amended; `MarketDumpTests.The_price_does_not_move_and_the_dump_says_so_in_the_table` asserts the flat price **and** the zero level, so a fix goes red and names what changed. `plans/0044` **F50** |
| **22** | 🔴 ⚠ **A DISTRICT BOUNDARY MIGRATING UNDER A SLEEPING BUYER STRANDS IT ON THE MARKET ROW OF A DISTRICT IT HAS LEFT, AND `Invariant.WaiterIsBlockedByTheBinItNames` FIRES** — filed unfixed 2026-08-26 by milestone 26 task 9, **found by the acceptance run's horizon and by nothing shorter**. Read off the world at the Tick it fired, on `rulesets/oversupplied.toml` at 2,000 Citizens: Rule Instance **754**, a Household's `restock`, asleep on Bin **1652** — a market row's Bin, `owner = District`, `sundries` — with a `Requirement` of **zero**. Its Building stands in **District 7**, whose sundries row is row **8**; it is parked on row **2**, which belongs to **District 3**. Row 2 has three sellers and row 8 has none, ***so the buyer is asleep in a market it has left, waiting on stock it may not have.*** ⚠ **The requirement of zero is a SYMPTOM and reading it as the cause is how this gets fixed in the wrong place**: `RuleEngine.Requirement` walks the Rule's terms, the `pool` term now resolves to row 8, so no term names Bin 1652 and the walk answers nothing. ***The Rule is right and the queue is stale.*** ⚠ **It first appears at Tick 362,496 and every earlier test on these files stops at 32,768**, which is the argument for a long acceptance run stated as a number. 🔴 **THE FIX IS UNARGUABLE AND ITS PLACEMENT IS NOT**: [`adr/0139`](../docs/adr/0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) has a Household buy *only* from sellers in its own District, so a stranded waiter must be re-homed — but **eagerly** in `DistrictWatershed.Migrate` makes a boundary move cost a wait-list walk, and **lazily** at the Rule's next evaluation leaves the invariant true only between evaluations. That is a design question and it owes a record ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)). ⚠ **`provisioned.toml` reaches 524,288 Ticks clean** — it churns fewer Districts — so this is not visible on the milestone's own demonstration file. `MarketLongRunTests.The_only_invariant_violation_is_the_waiter_a_district_migration_stranded` is an **allowlist of exactly this one**, and it goes red on a second violation, a different invariant, the same one on `provisioned.toml`, **or a fix**. `plans/0044` **F53** |
~~**Whether items {0, 2} come before or after item 1 is now the only open ordering question.**~~
**SETTLED by running them: {0, 2} went first, on the stated ground that a red suite outranks a defect at
a scale nothing currently runs at.** **Item 1 then ran, then item 3, and the queue is empty** — item 3
sequenced behind item 1 on the same rule that ordered the first pair: a defect that is live at a scale
nothing runs at still outranks one that cannot occur until the game ships patches.

**Two baseline re-records rather than one combined pass, deliberately.** Combining items 1 and 2 would
save a re-record and buy a hazard this project has already been bitten by: [`0013`](0013-tick-budget.md)'s
Bin Rule row was **right by cancellation** — a unit cost 2.8× too low times a multiplicand ~5× too high —
and [`0000`](0000-board.md) records that as **worse than being wrong**, because nothing would have noticed
either factor moving. Two unrelated mechanisms moving one baseline in one commit is that hazard in the
hash trace: if the new trace is wrong, it cannot be attributed. **A re-record is a command; a
mis-attributed hash move is a bug hunt.**

**One gap travelled with item 2 and it stands after the fact.** Deleting a saved column changes the
**save format**, and **lint 6 — save/reload equivalence, the Factorio test — does not exist**; the
machinery is still unbuilt. So the golden hash was the *only* check on that half — and it turned out to
be a thinner check than expected, since `world-hash.txt` holds no Rule Instance rows and did not move.
**One artefact covered the deleted column, and nothing else could have.**

**Item 3 walked into the same gap, and further in.** `adr/0064` deletes a *second* saved column, and this
one is read by the Rule engine on every evaluation rather than only while a Rule sleeps — so the golden
session covers it densely, which is the opposite of item 2's problem. What is not covered is the same
half: with lint 6 unbuilt, **nothing checks that a world saved before the change and loaded after it
agrees with itself**, and under item 3 the loaded world's ceilings come from the Ruleset rather than from
the save. That is the intended behaviour and it is also exactly what a save/reload equivalence test would
be for. **Two decisions have now shipped past the same missing lint**, which is the argument for building
it that neither one on its own was.

**And item 3 found item 2's coverage observation has a mirror image.** Item 2's note is that
`world-hash.txt` holds no Rule Instance rows, so the session trace was the only artefact that could notice.
Item 3 changed a column's *declaration* and its *width* and `world-hash.txt` again did not move — this time
because `GoldenFixtures.Build()` holds **no Bins at all**. Three of the four items moved exactly one of the
two committed baselines, and the same one each time. The file that exists to cover what a session cannot
reach covers the four slice-4 tables and, on the evidence, very little that arrived after them.

---

## Why this order, and where it departs from `06`

Three departures, each with a reason that is not "it felt tidier".

**Tables before the hash — slice 4 before slice 5, milestone 2 before milestone 1.** This is the
readiness review's own reorder and it is low risk: tables do not encode determinism rules, the RNG
and the State Hash do. It is also now a hard dependency rather than a preference. `adr/0003` settled
that every field is declared once as `(saved AND hashed)` or `(derived AND rebuilt)` and that **both
the save serialiser and the State Hash are generated from that one declaration**. The hash is
therefore a property of the table layer. Writing it first would mean writing it twice.

**The substrate is not part of milestone 1.** `06` folds counter-based randomness into milestone 1
and says nothing about typed quantities or fixed point, because when it was written `adr/0003` had
not yet been opened. It has been since, and it produced a component list that is upstream of
everything: a fixed-point library with tabulated `exp` and `log`, division and shift helpers with
stated semantics, and `Money`/`Ticks`/`Tiles`/`Ratio` as distinct value types. Typed quantities are
flagged in [`0002`](0002-open-questions.md) as **the item most expensive to retrofit** — they touch
every arithmetic site in the core — which puts them before the first arithmetic site exists, not
after ten thousand of them do.

**The analysers are a slice, not a chore.** `05 §4` lists seven mechanically-enforced rules and
`dev-environment.md` implements three of them as reflection tests, which cover *state* and not
*arithmetic*, `Dictionary` enumeration, or the `unmanaged` constraint. The remainder need a real
analyser project. Scheduling that as a named slice before the first table lands is the difference
between a lint that shapes the code and a lint that condemns it.

**Milestone 3c is split.** `06` bundles Map Layers with Zone Rules. Zone Rules are Sweep Rules and
Sweep Rules are the Rule engine, which is gated; Map Layers are not gated by anything. Splitting
them is what lets the Layers half land inside the Phase 1 gate instead of behind it.

---

## The gate board

What blocks slices 7 through 10, stated so that the grilling sessions have a target and so that
nobody starts one of these by accident.

⚠ **This board is Phase 1's. Phase 2's gates are the *Gate* column of the Phase 2 ledger above** —
one board per axis, because the two are keyed differently and a single table would have to carry both
numberings. [`0036`](0036-the-coarse-day-wheel.md) recorded the absence on 2026-08-21 (*"`plans/0003`'s
gate board holds no Phase 2 row"*) and it is filled rather than explained. **No Phase 2 gate is red.**

| Slice | Blocked by | What specifically is missing |
|---|---|---|
| **7** — Bin Rules | ~~`adr/0015`'s Ruleset validator~~ **CLEARED** — [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) names the parser (**Tomlyn**, in `Borough.Formats`), puts the validator with it, and enumerates **three** refusals in one load-time walk: the `on_fail` cycle check, the `fills` check, and an unquoted decimal. **The build has five** — a chain not ending in a terminal, and money that does not balance, both arrived while writing it. The core receives ids and integers and never a string. ~~Slice 7 still owes **Rule evaluations per Tick** and **walked chain depth** (`02 §9`)~~ **DISCHARGED by task 9**, and the first of the two had to be rebuilt rather than wired up: it counted *due Rule Instances*, which a chain walk does not move | **`02 §4` residue is closed** ([`adr/0045`](../docs/adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md)) and closing it **moved this gate rather than clearing it**. Depth needed no cap — the source ladder bounds it and the number is measurable, routed to this slice's counters. What remains is load-time: the `on_fail` **cycle check** and the **`fills` check**, both refusals on `adr/0015`'s error surface, and both needing the TOML parser below. ~~Slice 7 also owes **Rule evaluations per Tick** and **walked chain depth** (`02 §9`)~~ **DISCHARGED by task 9**, which also measured what this gate's *number is measurable, routed to this slice's counters* was routing: a chain rung costs **53.6 ns** against a head evaluation's **82.84**, so **depth is the cheap axis** and session B's withdrawn cap of 5 would have bought the least available saving |
| **8** — hot reload | ~~`adr/0015`~~ **CLEARED** | Grilled by session A. **The *must not slip behind 3c* claim is retired**, not re-grounded — it was one unargued claim counted twice, and slice 6 falsified it at no cost because the no-`const` rule was doing the work. What replaces it is checkable: **slice 8 is not done until the Layer cadence and rates load from a file**. Reload's log representation is settled too — hashes travel in the Input Log, Ruleset **content** travels in the crash artifact |
| **9** — Event Wheel | ~~`02 §7`, `adr/0006`~~ **CLEARED** by session C → [`adr/0056`](../docs/adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md) | Grilled. **Reading `02 §7` against `adr/0033` rather than fresh is what found everything**, exactly as this row instructed. The Wheel is **two levels — Ticks and Days** — with **one wheel per scheduled table**, because the fine wheel's period is *exactly one Day* and `adr/0011` schedules Life Stages in Days, so every Life Stage transition the design specified was unrepresentable on the wheel it was specified to run on. **Slice 9's scope is narrowed by the same session**: the fine wheel only, since the coarse wheel has no consumer until Phase 2, with `Arm`'s refusal kept and re-pointed at the ADR. `adr/0006` needed no defending — the session added *why* the Wheel satisfies it, which is **partition rather than accumulation**. Two things fell out sideways: `adr/0033`'s *"wait lists are rebuilt, never saved"* is **half wrong** and the code already disagreed (invariant 6 would have caught it, and invariant 6 does not exist yet), and `02 §7`'s *"a few hundred out of hundreds of thousands"* is now typed **measurable** and unmeasured, with S0b named |
| **10** — Zone Rules | slice 7 | A Zone Rule is a Sweep Rule. There is nothing to sample with until the Rule engine exists. **It has also inherited an obligation**: slice 5 task 7's long-run trend assertion is stated over a rising `slots` against a flat `live`, and the Rule engine creates no rows — a Rule Instance's life is its Building's — so **no Ruleset can make a slot count trend**. Buildings arriving and being demolished is what churns rows, and that is this slice. Slice 7 keeps only the **flow** half (`0011` finding 36). **Slice 7 has closed, so the dependency is discharged and the plan is [`0014`](0014-zone-rules-and-the-sweep-family.md)** — which found that the obligation is larger than it reads here: a slice that only *demolishes* leaves `slots` flat against a falling `live`, which is not churn either, so discharging it needs creation as well and that is what forced the create predicate to exist at all |

~~**Also owed, and it is no longer merely adjacent to slice 7:** a TOML parser library is unnamed.~~
**SETTLED in session A** ([`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md)).
The parser is **Tomlyn**, and it goes in **`Borough.Formats`, not `Borough.Core`** — so
**`adr/0003`'s exception is not owed at all**, because there is no core dependency. What replaces it
is narrower and more useful: *nothing but integers and strings crosses from the parser into the
loader*. That is what actually protects determinism, since a bad parse poisons the simulation from
any distance and the assembly it sat in was never the question.

---

## Decisions owed, found while planning

Rule 6 applied to this document itself. Four items surfaced while decomposing the slices that are
not recorded anywhere in the corpus. None blocks slice 0 or slice 1; three block a slice below.

**1. The tabulated `exp`/`log` resolution has never been stated.** `adr/0003` promoted tabulated
transcendentals from a contingency to a required core component and was explicit that *the table's
resolution is a stated figure, not an implementation detail* — because it perturbs the effective `μ`
and `μ` is what prevents stampedes. **The figure itself appears in no document.** Slice 2 cannot
finish without choosing one, and choosing one silently is precisely the failure mode `0002`'s own
through-line warns about. It is also a hash-bearing world-creation constant by the `05 §4` test, so
it cannot be tuned later. *Blocks: slice 2. Recommended handling: build the table generator with
resolution as an explicit parameter, pick a provisional figure, and record it as **unratified** with
the validation owed against `adr/0005`'s herding behaviour.*

**2. ~~Map Layer diffusion cadence is called tuning and is not.~~ SETTLED in slice 6, by measurement.**
[`adr/0044`](../docs/adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md).
Under `adr/0043` the claim typed **measurable** rather than arguable — the refuting number was a State
Hash and the machine was the slice itself — so it was measured instead of argued, and it is **false**:
two worlds differing only in the diffusion period produce different hash traces. So the cadence is
**the designer's number and not the profiler's**, and it stays ordinary hot-reloadable Ruleset data.
`05 §9`'s performance-multiplier bullet is where it was actually mis-filed and is corrected; `02
§1.2`'s row keeps *tuning* and gains *hash-bearing*. **The sixth claim in the corpus measured false,
and the first outside S2.**

> **This entry said *world-creation-fixed* for one draft, and that was wrong** — an argument
> (*the Ruleset is by definition the numbers that do not change the city*) put in place of a stated
> test. `adr/0015` says the opposite in its own words: the Ruleset's content hash feeds the State
> Hash, and its world-creation category has a membership test — *was existing state recorded in units
> of the constant?* — that the cadence **fails** and the kernel radius passes. Recorded rather than
> silently amended, because it is the same failure the entry is about.

The original entry follows.

**2. Map Layer diffusion cadence is called tuning and is not.** `02 §1.2` lists *"Map Layer
diffusion, every 32–64 Ticks, staggered"* in the **tuning** column. But cadence decides when a
source's contribution becomes visible to a Rule that reads the Cell, so two runs at different
cadences produce different cities — which makes it a **design change** under `05 §4`'s State Hash
rule, not a free knob. This is the same welding failure `adr/0034` found in Chunk size, one document
later. *Blocks: slice 6. Recommended handling: reclassify cadence as hash-bearing and Ruleset-authored
but world-creation-fixed, or produce the argument for why it is not.*

**3. ~~The CI lint count disagrees across three documents.~~ Fixed in slice 3.** `05 §4` enumerates
seven; `adr/0036` called itself *"a sixth CI lint"* and said *"of the six CI rules this project
enforces"*; [`0002`](0002-open-questions.md) called it the seventh. One rule, three counts. `05 §4`'s
seven is now authoritative everywhere, `adr/0036` carries the correction and the reason it mattered —
*a checklist that cannot agree on its own length has stopped being checked* — and the diagnostic ids
in `Borough.Analysers` are derived from that numbering, so the count is now load-bearing rather than
prose.

**4. Spike results have no home.** `06` says *"Record them; delete the code"* and names no file. Four
spikes will produce numbers that must be re-readable in a year when a performance question resurfaces
— which is stated as the entire value of running them. *Blocks: slice 1's last task. Handling: this
plan creates [`docs/spike-results.md`](../docs/spike-results.md) and slice 1 writes the first entry.*

---

## Definition of done, per slice

Cumulative obligations from [`06`](../docs/06-roadmap.md), restated as the checklist each slice's
plan document closes on. These are not milestones of their own; a slice that breaks one has failed
regardless of what else it delivered.

- `dotnet build` succeeds and `dotnet test` is green, **on a machine with no GPU and no Godot
  installed**. `dotnet build src/Borough.Headless` is the continuous form of that check.
- Every invariant the slice introduces is registered in a **frequency tier** — `O(1)` per Tick,
  `O(n)` staggered, whole-world at end of run — and **never gated on build configuration**
  (`02 §10`). The runs that surface these bugs are release builds millions of Ticks long.
- No collection and no magnitude trends upward at steady state over a long headless run
  (`adr/0006`, and `adr/0003`'s extension of it to quantities).
- Any change to the State Hash was **deliberate and re-baselined**. The point is not that the hash
  never moves; it is that it never moves without someone saying so.
- **There is something to look at.** For the whole of Phase 1 that is a hash trace, a headless
  summary, or a benchmark report — not a rendered city, and resisting the pull to render one early
  is part of the plan.
- Every number the slice chose that nobody ratified is written into
  [`0002`](0002-open-questions.md)'s ledger before the slice closes.

---

## What is deliberately not in this plan

> **⚠ THE PHASE 2 HALF OF THIS SECTION IS STRUCK, 2026-08-22.** Every premise below was discharged
> and the corrections were written in place rather than acted on. **S0a and S0b have both run**; `06`'s
> Phase 2 was **re-derived by session K on 2026-08-16**, so it is no longer *"task lists against
> decisions a grilling session will move"*; and eleven Phase 2 milestones have since **shipped**, which
> settles the question empirically. ***A refusal whose stated reason has gone false is not thereby
> struck, and nobody struck this one*** — the same shape [`0000`](0000-board.md) recorded for S0b's
> gate, third data point. The **Phase 2 ledger** above is what replaces it. **Phase 3 stands: it is
> undesigned rather than unplanned**, and the rest of this section is unaffected.

~~**Phase 2 and Phase 3.**~~ **Phase 3.** Not from lack of interest but because the readiness review is unambiguous:
Phase 2's wall is `03 §5`, the traffic model — still the most detailed unargued design in the
project, now carrying transit vehicles under a Microscopic Cap whose value is unset — plus six
🔴 ADRs and S2. Planning it now would be writing task lists against decisions that a grilling
session will move. The instruction the corpus gives itself is *do not open Phase 2 content until S0
has run*, and ~~S0 is slice 11~~ **S0 is a spike and there is no slice 11** — the ledger above ends at
slice 10, and S0a/S0b are rows in the *spike* table. Corrected 2026-08-14; the instruction is unchanged
and **S0a and S0b have both since run**.

**S0 has since split, and the instruction needs reading against the split.** **S0a is done** and it
closes the *sizing* half — 1M rows fit, with an order of magnitude spare, and nothing trends over
100,000 Ticks. ~~**S0b is not runnable**, because the Event Wheel, Bin Rules and a Sweep Rule pass are
slices 9, 7 and 10. So the instruction cannot be read as *Phase 2 planning is now open*: what was
validated is that the tables hold the target, and what `06` actually names as the risk — that every
system **sized** against 1M rests on an unvalidated assumption — is closed for row counts and open for
the Tick.~~ **The honest position is that K is unblocked on sizing and still blocked on the Tick
budget**, and the only spike with a number in that column is S2.

> **⚠ STRUCK 2026-08-14 — every premise in the crossed-out sentences has been false for some time, and
> the conclusion beneath them survives for a different reason.** Slices 7, 9 and 10 are done, so S0b was
> runnable; **it ran**, in three of its four clauses, and **priced the Tick at 8.72 ms at 1M — 55.9% of
> the budget at 4×**. So *"still blocked on the Tick budget"* is wrong as written: the Tick budget has a
> measured number and it is the only one ever taken from a real city.
>
> **What survives is narrower and is about the fourth clause.** The **routing load** could not be
> measured in situ, and routing is [`0013`](0013-tick-budget.md)'s dominant row — 9.4–10.5 ms of a
> ≥17.8 ms bill — so the column K actually needs is still guessed. ***A gate whose stated reason has
> gone false can still be a gate, and re-deriving it is not the same as striking it*** — which is why
> this is struck in place with the replacement written rather than deleted.
>
> **This is the shape [`0000`](0000-board.md) has assigned itself a sweep for and never run**: *a gate
> whose stated reason covers only part of what it blocks, leaving a runnable remainder parked behind a
> session it does not need.* Third data point, after `adr/0003`'s owed validation and `06`'s K1.
>
> ✅ **RESOLVED 2026-08-16 — K ran, and the routing load did not decide anything.** The gate said K
> needed *the routing load in situ*, on the reasoning that routing is `0013`'s dominant row and therefore
> orders the back half of Phase 2. **It orders none of it.** Nothing in the re-derived sequence's demand
> spine — milestones **6 through 19**, which is where the value was — reads a routing cost at all; the
> only rows a routing figure could move are **21–23**, and those are held by sessions **E** and **G**
> anyway. ***A gate can be discharged by discovering that what it was protecting does not depend on
> it***, which is a fourth outcome beside cleared, struck and still-red, and this corpus had no word for
> it. **The tell was available in advance and nobody looked**: the gate named a *cost* and the thing it
> gated was a *dependency order*, and a cost has never ordered anything here.

**The Godot shell.** [`dev-environment.md`](../docs/dev-environment.md) Track B stands up the
project and proves the boundary; S1 and S3 measure the ceilings. Nothing else in `Borough.Godot` is
planned until Phase 3, and `Borough.Godot` is deliberately absent from the Track A solution so that
the constraint *the headless runner never requires Godot* is enforced by there being nothing to
require.

**A save format.** Milestone 8, and it lands last in Phase 2 for a stated reason: a save format
written before the tables have settled is a migration chain written against nothing. Slice 4 builds
the **field declaration** the serialiser will one day be generated from, which is the part that is
expensive to retrofit; it does not build the serialiser.

**Content.** No Goods, no Zone families, no Policies, no Ruleset beyond what slices 7 and 8 need to
prove reload works. Content follows the Ruleset work and the axis on which variants differ is itself
still open.
