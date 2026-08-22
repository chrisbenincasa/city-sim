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
| **12** | Goods between Buildings — the District Pool | none | [`0037`](0037-goods-between-buildings-the-district-pool.md) | 🟢 **LIVE.** Scoping started 2026-08-21, no task begun. Decisions **1, 4 and 8 settled** (`adr/0132`–`adr/0135`); **nine open**, and the survey found three preconditions no document had listed |
| **18** | Needs and the coarse Day wheel — *wheel half only* | ✅ assessed 2026-08-21 — nothing names one | [`0036`](0036-the-coarse-day-wheel.md) | 🟡 **SCOPED 2026-08-21, out of sequence.** It is the repair for milestone **20**'s `adr/0011` gate, which is why it was scoped early |
| **13**–**17**, **19**–**24** | — | — | [`06`](../docs/06-roadmap.md) | Not started. **`06` holds them and no plan document is owed until a row is next.** 20–23 are sequenced provisionally, pending sessions **E** and **G** |

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

> ⚠ **ITEM 8, added 2026-08-13 by session P: a waiter whose own requirement falls is never re-checked.**
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
> **It moves the hash** — waking a Rule Instance is a change to what the city does — so it re-records.
> **Position: last.** Nothing shipped provokes it, and the two candidate fixes are a real design
> question rather than a correction: re-examine a Bin's queue when a **Readout** the requirement depends
> on changes (a write nobody currently issues, and `02 §5` has no such edge), or sweep the wait lists on
> a cadence, which is [`adr/0033`](../docs/adr/0033-two-rule-families-scheduled-and-swept.md)'s
> forbidden move — a Bin Rule becoming a Sweep Rule is a change to the city, not an implementation
> detail. **Do not pick one inside another item's commit.**

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
| **11** | ~~⚠ **`Invariant.WaiterIsBlockedByTheBinItNames` fires on EVERY Ruleset for any run shorter than 64 Ticks**~~ — ✅ **FIXED 2026-08-22**, both golden traces re-recorded, `world-hash.txt` unmoved. **`RuleEngine.Stop` drains the Bin it has just joined**, and `World.Drain` is `internal` to allow it. ⚠ **The repair was the open half of this row and the corpus closed it rather than a sitting**: the alternative — re-checking a Phase 2 failure against the Future in Phase 3 — is *refused* and not merely worse, because `Apply`'s re-check **may lower and may never raise**, and a Rule rescued by a deposit applied after it decided spends a quantity that did not exist when it decided ([`adr/0049`](../docs/adr/0049-a-rules-apply-count-is-decided-against-the-past-and-settle-may-only-reduce-it.md)). `World.Wake` arms for `tick + 1`, so the fix cannot rescue a Rule into firing on the Tick it failed. ⚠ **The filing's unchecked clause is discharged**: *identically on three Rulesets* had been read on `minimal.toml` alone, and `diagnosed.toml`, `congested.toml` and `taxed.toml` all panic at `--ticks 4` before the change while **all eleven shipped Rulesets are clean after it**. ⚠ **The regression test asks the invariant after EVERY Tick rather than once at the end, and that is the whole reproduction** — a spurious park is healed by the next write to the same Bin, so an end-of-run check catches one only if the run stops while a waiter is mis-parked. ***The shortness of the run was the sampling and never the cause***, which is why this row's headline named a run length. Six seeds, because the settle order is a draw and only `seed: 4` exposes it on that fixture. *Original entry:* filed unfixed 2026-08-20 by milestone 9 task 7, ✅ **DIAGNOSED 2026-08-21**, and the **fix is held rather than owed**. 🔴 ⚠ **The cause is NEITHER of the two readings this row was filed with.** `RuleEngine.Evaluate` is **Phase 2 — *evaluate every due Rule against the Past, writing nothing*** — and Phase 3 applies the verdicts, so a Rule judged short in Phase 2 is **made whole by another Rule's deposit applied earlier in the same Phase 3**, and `RuleEngine.Stop` subscribes it anyway. `World.Drain` has already run on that Bin, against a queue the waiter had not yet joined. ***A waiter parks on a Bin that the same Tick has already refilled, and the wake it needed was spent before it was owed one.*** 🔴 **The invariant is RIGHT and the engine is wrong** — this is `05 §9`'s asleep-for-ever reached through a missed drain, which is the failure this invariant was registered to catch, arriving through the one door nobody watched: not a drain that was skipped, but a drain that was **early**. ⚠ **The repair is a design choice, which is why the fix is held and not merely queued.** The smallest is `World.Subscribe` draining the Bin it has just joined — it reuses `Drain`'s own requirement check rather than standing up a second predicate, and it never re-arms on a timer, which `02 §4.1` refuses by name. ⚠ ~~**It is held ONLY because it re-records the golden baselines while milestone 11 is re-recording the same files**~~: a collision on a shared artefact, and expressly ***not a hash concern*** — [`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) forbids citing hash movement as a reason to defer, and it is not cited here. ✅ **THE HOLD IS RELEASED, 2026-08-21. Milestone 11 closed, so the collision is over and this item can be picked up.** ⚠ **Releasing it restores the design question that was underneath**: the hold was never the blocker, the *repair being a design choice* is, and ***an item held on a scheduling collision reads as blocked on the collision, so clearing it looks like clearing the item.*** What is owed before a line is written is which repair — the smallest candidate is `World.Subscribe` draining the Bin it has just joined, and it is a candidate rather than the decision. ⚠ **It was filed as *item 6* and 6 was taken** — by `SyntheticCity` paving what it populates, shipped 2026-08-13 — **renumbered to 11 on 2026-08-20 while closing the milestone.** The table above stops at the items that have a row; **items 7 to 10 live in the prose blocks below it**, so a number taken by reading the table is a number checked against part of the set. ***A ledger whose rows and whose prose hold entries of the same kind is one ledger for the purpose of numbering*** | **yes** — ~~unknown~~, answered 2026-08-21. Every candidate repair fires Rules that previously slept, so it moves every State Hash and re-records the golden baselines | **Found while loading a new Ruleset, and it is not that Ruleset's**. `dotnet run --project src/Borough.Headless -- --ruleset rulesets/minimal.toml --ticks N` panics at the end-of-run check. 🔴 ⚠ **THREE PARTICULARS OF THE ORIGINAL FILING ARE WRONG AND ARE STRUCK.** ~~*same rows every time (166, 111)*~~ — the rows move with `N`, and **so does the queue**: `N=4` → (166, 111) on **Supply**, `N=16` → (1336, 891) on Supply, `N=32` and `N=48` → (24, 17) on **Space**. ~~*clean at 64 and 128*~~ understates it — **zero violations for every `N` from 64 to 96**, so the quiet is systematic rather than sampled. And ~~*either a waiter is legitimately parked in a state the re-derivation does not reproduce … or the invariant is right and something arms wrong*~~ is a false pair; it is a third thing, in this row's second column. ***A filing records what its reporter saw, and a particular quoted from a single sighting reads as a property of the defect.*** ⚠ **The *identically on three Rulesets* half was NOT re-checked** — the diagnosis ran on `minimal.toml` only, and that clause stands as the original reporter's and not as this sitting's. **The trace, on `minimal.toml` bin 111 at Tick 1**: `DEPOSIT amount=16` runs `Drain(Supply)` against an **empty** queue, and `STOP:Supply` then subscribes instance 166 with a **requirement of 12 against a level of 16**. **Measured over 2,048 Ticks at 10,000 Citizens: 36 spurious parks** — 19 on Supply in Ticks 1–8, 17 on Space in Ticks 25–31, and **none after Tick 31**. ⚠ **All 36 were rescued by a later write to the same Bin**, up to ~30 Ticks on, so ***that it can strand a Rule permanently is an argument from the mechanism and not a measurement***. It is **measurable** ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)) and the world that would settle it is one whose producer stops writing the Bin. ⚠ **Nothing in the suite runs the headless runner for under 64 Ticks**, which is why a panic on every shipped Ruleset has stood unseen. Routed here rather than worked around, under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md) |
| **12** | ~~⚠ **A Rule may name `layer = "land-value"` or `"sealing"` in its outputs, load clean, and panic the first time it fires**~~ — ✅ **FIXED 2026-08-22**, with item 10, because the two are one defect wearing two faces. `RulesetLoader.ReadEmission` refuses it at the parse site under `adr/0048`, and the predicate has one home — **`MapEmission.IsEmittable`** — asked by the loader and by `RuleEngine.Emit`'s surviving backstop, because ***a rule stated in two places is two rules and the one that drifts is the one nobody runs***. ⚠ **It gets its own sentence rather than reusing the unknown-name refusal beside it**: `land-value` is not a typo for a Layer, it *is* a Layer, so *"is not a Map Layer"* would be false — ***two mistakes that resolve to one message is a message that is wrong for one of them.*** ⚠ **`RefusalCountTests` caught the filing debt on the spot** and named it precisely: `adr/0048`'s site count moves to **118** *and* the new refusal joins that ADR's enumeration, which is the eighth recount. ***A document-to-code check is the only one that notices a number describing the build.*** **Moves no hash.** *Original entry:* — filed unfixed 2026-08-20 while closing milestone 9 | no | **A load-time refusal is missing where one already exists three lines away.** `RulesetLoader.TryLayer` (`RulesetLoader.cs:3532`) resolves all three Layer names and `ReadEmission` (`:887`) checks nothing further; `RuleEngine.Emit` (`RuleEngine.cs:859`) then throws `NotSupportedException` on anything but pollution, and its message is correct — **land value is chased towards a target and Sealing is a property of a footprint, so neither is a quantity a Rule adds per application**. So the refusal is *written*, and it is written **one Tick too late**: [`adr/0048`](../docs/adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) puts validation where the Ruleset is parsed. ⚠ ***A refusal in the engine is a refusal the designer meets as a crash***, which is queue item 10 in a second place. **Found by reading the symbols while closing milestone 9, not by a test** — nothing authors such an emission, so nothing has ever hit it |
| **13** | ⚠ **The assertion tier has at least TWO intermittently-failing tests, neither caused by the change that found them** — filed 2026-08-20, **measured 2026-08-21** by milestone 11 task 1 | no | ✅ **MEASURED RATHER THAN SUSPECTED, and the suspicion was wrong.** Filed after one failure of `ZoneRuleTriggerTests.Sweeping_allocates_nothing_after_the_first_trigger` in four runs, suspecting task 1's two new calls in `World.CreateBuilding`. **20 full assertion-tier runs, sequential, baseline `35df747` in a separate worktree against the working tree**: baseline **1 / 10**, treatment **0 / 10**. ***The test fails on a tree that does not contain the change it was filed against***, so task 1 is exonerated and the flake is pre-existing. ⚠ **Sequential by design** — if a flake is load-related, concurrent runs change the thing being measured, which is `plans/0000`'s parallelism-capture rule applied to a *failure rate* rather than to a duration. ⚠ **And the probe found a SECOND one nobody had filed**: `JobSearchBoxTests.The_box_is_not_where_the_pass_spends_its_time`, **1 / 10** on the treatment side. 🔴 **The two are different animals and only one is a mystery.** The Zone one asserts `GC.GetAllocatedBytesForCurrentThread()` is **exactly unchanged** across 500 `Step` calls — a counter, not a clock, so noise is not an explanation and the standing hypothesis is a **tiered-JIT rejit allocating on the measuring thread**, which is unproven. The JobSearchBox one asserts a **`Stopwatch` wall-clock ratio** stays under 2.2, and ***its own comment predicts this exact failure rate***: *"a band transplanted from a quieter quantity fails one run in ten with nothing wrong under it."* It then failed one run in ten. **That one is arguably not a defect at all but a MISCLASSIFICATION**: under [`plans/0032`](0032-test-tiers.md)'s own axis — *what would you do on the day it failed, find out what broke or paste the new number* — a wall-clock ratio that flakes with nothing wrong under it is an **instrument** wearing an assertion's clothes, and [`adr/0121`](../docs/adr/0121-the-commit-gate-is-the-assertion-tier-and-a-long-test-runs-post-submit-on-a-machine-that-is-not-yours.md)'s *a quiet machine is a control on a capture* says a test that takes a capture inside the gate has put a capture's controls on the gate. **The behaviour it guards is real and worth guarding** — box walking going from ~18% to ~28% of the pass — so the repair is to count box-walk operations, which is deterministic, rather than to time them or to retag it. ✅ **THAT HALF IS DONE 2026-08-22, and the repair was the one this row named.** `EmploymentActivity.BoxWalks` counts the walks — one per seeker to size the box, then one per candidate drawn — and the test asserts `walks == seekers × (1 + candidates)` at both ceilings, plus that the two walk totals are **equal**. Measured at 40,000 Citizens: **33,277 seekers, 133,108 walks, 4.00 per seeker, identical in both arms**, against boxes of **841** and **4,489** Cells. ***Growing the box 5.34× adds no walks at all***; what it scales is Cells-per-walk, which is geometry and derivable (112M against 598M Cells visited, printed rather than asserted). ⚠ **It is a NARROWER claim than the stopwatch made, and that is the trade taken deliberately.** The old assertion was a *share of run time* — box walking at ~18% of the pass, failing at ~28% — and **no operation count can reproduce that**, because a route search is a variable-cost search rather than one operation. What is asserted instead is the structural fact underneath it: *the pass walks the box a constant number of times per seeker, and box width does not move that constant.* ***A guard that fires on a real change every time beats one that fires on a bigger change nine times in ten.*** ⚠ **The counter counts WALKS and not CELLS on purpose** — a Cell counter would put an increment inside the loop being measured. **Verified by regression**: adding a second `CountIn` to `TryEmploy` fails the test immediately. **Moves no hash** — it is an instrument, and no golden baseline moved. 🔴 **The `ZoneRuleTriggerTests` half of this row is UNTOUCHED and still open**, and it is the one that is a mystery rather than a misclassification. ⚠ **What a flaky gate costs is not the run; it is that it trains its user to re-run rather than to read**, and the next real regression arrives wearing the same face. 🔴 ⚠ **UPDATED 2026-08-21 by milestone 11 task 9 — `plans/0035` **F33**, and the update is mostly about THIS ROW AND `plans/0002` §B HOLDING TWO HYPOTHESES FOR ONE EVENT WITH NEITHER CITING THE OTHER.** This row says *tiered-JIT rejit allocating on the measuring thread, which is unproven*; §B says *a per-thread allocation context that a collection on another thread flushes*. They are different mechanisms for the same firings, filed two days apart in two ledgers, and ***a fact with two homes is `plans/0012` Cause 1 whether or not the two agree.*** **What the evidence now says.** Task 9's longer tier fired `LayerQueryTests` once in four runs — **3,376 bytes, 1 gen0** — and reading the probe's rows for it turned up an **unread** firing at row 300 of `alloc-probe-archive/2026-08-21-cumulative-105-processes.csv`: this row's own `ZoneRuleTriggerTests`, **5,208 bytes, `0 gen0 / 0 gen1 / 0 gen2`**. `GC.CollectionCount(0)` is process-wide and gen0 is non-concurrent, so no collection completed anywhere in that window. ✅ **That refutes §B's mechanism as NECESSARY** — a jump happened with no collection to flush anything — **and leaves this row's tiered-JIT hypothesis standing**, because a rejit allocates without needing a collection. ⚠ **Standing is not confirmed**: nothing has measured a rejit inside a firing window, and the obvious machine is a run with `DOTNET_TieredCompilation=0`, which is a config a document may not quote a figure from without saying so. **What both rows agree on is the BOUND** — six firings, all under **8,192**. ⚠ **The two rows are not merged here**, because §B owns a *measurable* and this owns a *defect*; what is owed is that each names the other, and this half is done. ✅ **The re-run-rather-than-read cost this row predicts is now closed** — the eight assertions go through `AllocationProbe.Check`, whose failure message names the delta, the collection counts, the 8,192 band and the file, and says to read it BEFORE re-running. ***The 5,208 was lost to exactly the behaviour this row warned about***, which makes it the row's own prediction landing before its repair did |
| **14** | 🔴 ⚠ **`World.Drain` and `Invariant.WaiterIsBlockedByTheBinItNames` contradict each other, and a committed test asserts the state the invariant calls a violation** — filed unfixed 2026-08-22 by queue item 11, **found beside it and expressly not fixed inside it**. The drain **stops rather than skips** at an uncovered waiter, deliberately: *"skipping an uncovered waiter to reach a smaller one behind it would starve every large waiter permanently"* (`World.Drain`'s own remarks, `adr/0063`). So a small waiter queued **behind** a large one it could outrun stays parked on a Bin that covers it. `WorldInvariants.CheckQueueStillBlocks` then walks **every** waiter on the list — not the head, and with no positional budget — and `RuleEngine.BinStillBlocks` compares each requirement against the Bin's **whole** level. ⚠ **The invariant is therefore stated more strongly than the drain can deliver.** | **unknown** — it depends which side moves. Narrowing the invariant to what the drain guarantees moves no hash; making the drain skip is a change to who gets served and moves every hash | ⚠ **MEASURED, not reasoned** ([`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)): adding `world.Invariants.RunEndOfRun(world)` to `BinTests.The_drain_stops_at_an_uncovered_waiter_rather_than_skipping_it` — which subscribes a waiter needing **10** ahead of one needing **2**, then deposits **6** — fires `WaiterIsBlockedByTheBinItNames` at Tick 0. **That test asserts both stay waiting and is correct to**; the probe was reverted and is not committed. 🔴 ⚠ **It is latent ONLY because no shipped Ruleset puts two Rules on one Bin**, which [`BinWaitListTests`](../tests/Borough.Tests/Rules/BinWaitListTests.cs)' own header already says: *"under `local` scope no two Rules share a Bin they do not both own."* ***`Scope.Pool` is precisely the mechanism that ends that***, so **milestone 12 is what makes this reachable** — it must be settled before the Pool ships, not after. ⚠ **The repair is a design question and must not be taken inside another item's commit**: either the invariant narrows to the drain's guarantee, or the drain stops starving small waiters, and those are different cities |

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
