# 0003 — The build plan

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Phase intent and risk framing in
> [`docs/06-roadmap.md`](../docs/06-roadmap.md). Readiness derivation in
> [`plans/0002-open-questions.md` §Readiness](0002-open-questions.md).
>
> **This document supersedes [`06-roadmap.md`](../docs/06-roadmap.md)'s ordering for Phase 0 and
> Phase 1 only.** The roadmap's phases, risk fields and the argument for each remain authoritative;
> what is re-derived here is the *order*, because the readiness review of session eight moved tables
> ahead of the hash and pulled three items out of Phase 1's milestones that were never milestones —
> the scaffolding, the arithmetic substrate, and the analysers. Phase 2 and Phase 3 in the roadmap
> are untouched and are not planned here.

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
migration written for no reason but vanity. **The trigger is therefore milestone 10, and there is a
second, softer one: the first time the name is shown to somebody who is not the author.** If it is
going to change, it changes before slice 10.

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
| **5** | **The Tick, the Input Log and replay** | **1** | cleared | 3 | [`0008`](0008-tick-and-replay.md) |
| **6** | **Map Layers** — **all ten tasks done.** Cell grid and the Cell/Chunk type split, the sparse double-buffered `LayerCellTable` — the project's first `Buffering.TwoCopies` — the separable integer convolution, the staggered schedule as a table, incremental re-diffusion proved bit-identical, the three real Layers, the named holes that throw, `layer_cells(aabb, layer)` and the end-of-run magnitude check. Produced `adr/0044`, which **settles owed decision 2 by measurement and finds it false** — and then got its own second half wrong by argument and withdrew it rather than amending it away | **3c** (Layers half) | cleared | 3 | [`0009`](0009-map-layers.md) |
| — | *the Phase 1 gate closes here* | | | | |
| **7** | **Rule engine — Bins and Bin Rules** — **done.** Bins with no public level column, the two wait lists, the Ruleset loader and its five refusals, quoted decimals, atomicity over net deltas, the apply band, the Readout set, `on_fail` chains, `02 §4`'s counters, and **task 10a's wiring: the first Ruleset ever to reach a `World`.** Produced `adr/0049` and `adr/0050`, took the **Resource family** out of order to stop a money leak six slices old, and measured the first price the Tick has ever had on it — **82.84 ns an evaluation**. ~~Task 10 ships the first Ruleset with content in it~~ — **split while being planned**: it asked for a production chain over two or three Goods, which is `pool`, which this plan's own decision owed 3 had already made a named hole that throws. **10a shipped and closed the slice**, with a `rulesets/minimal.toml` the golden session now runs under and an arming stagger that turned out to have **no number to choose**. **10b is the content, re-filed to Phase 2** | **3a** | ✅ **cleared** by session A → `adr/0048` | 3 | [`0011`](0011-rule-engine-bins-and-rules.md) |
| **8** | **Rule engine — hot reload. DONE — all tasks.** Tasks 1–3 shipped first; **tasks 4, 5 and 6 merged into one** before any was written, because none is reachable alone (a kind removal trips `KindCount`, `RuleCount` and `ResourceCount` at once, and the structural refusal lifts once or the reload half-happens). The swap at the top of Phase 0 — **and there is no new verb**, because `Command` is 12 bytes and the transition machinery (`RulesetHashAt`, `TickInput.RulesetHash`) is already in the tree unused — the transition in the Input Log, the world-creation refusal and the loader's second entry point, the three degradations (**derelict flag**, dropped Bins, wait lists re-armed on slice 7's derived stagger), the provenance trail with `adr/0006`'s cap, **the Layer cadence and rates from a file**, and the runner accepting more than one Ruleset so `adr/0015`'s seconds test can be run at all. **Planning found a live defect**: the industrial pollution kernel radius was a `const` in `SeparableKernel.cs` and `adr/0015`'s world-creation category never exempted it from the Ruleset — so task 3's refusal had nothing to refuse until it moved. **Task 3 moved it and built the whole `[layers]` table**, which absorbs most of task 8: the loader's refusal count goes **8 → 11 at load plus a 12th on reload**, and the reload check is the project's first that is a property of a file *against a world*. It also found that **`adr/0015`'s world-creation enumeration has four members and only one of them is Ruleset data**. **Task 4 then found the larger one**: a Ruleset declaration's id is its position in the file, so *every removal the degradations exist for is also a reordering* — `02 §4.3` describes them as though ids were stable across two files and nothing made them so. Fixed by a key per declaration. **Derelict turned out to need no flag at all**. **Tasks 7–10 closed the slice.** The provenance trail is a capped table on the `World` — `05 §7`'s degradation-as-state, 16 transitions retained and older ones aggregated to counts — and *the cap is world-creation-fixed on a self-referential argument*: a designer must not be able to reload a smaller window, because the file whose adoption the history is about would be truncating that history. The Layer numbers are now **stated** by `rulesets/minimal.toml` and `adr/0044`'s claim runs end to end from TOML text to State Hash, on a fixture built for it — **the golden session cannot see its own cadence**, because it emits no pollution and diffusing zero at any period gives zero. The runner takes `--ruleset` repeatedly and refuses a transition nobody supplied, which **`--force-ruleset` may not waive**; `Replay.Start` turned out to check the catalogue against the world it had just built from that same catalogue and never against the **log**. And `adr/0015`'s acceptance test **ran: 0.70 s**, against the 60–120 second warm rebuild the ADR was written on — **it could not have been run through a recorded log at all**, because a log names a Ruleset by content hash and editing the file *is* the loop, so `--reload-at N` builds the transitions on the run | **3b** | ✅ **cleared** by session A → `adr/0048` | 3 | [`0015`](0015-hot-reload-and-the-ruleset-as-a-thing-that-changes.md) |
| **9** | **Event Wheel — all four tasks done.** Session C settled the design and **narrowed the scope**: the **fine wheel only**, because the coarse Day wheel has no consumer until Life Stages arrive in Phase 2, and building it now would be writing past the slice. Finish what slice 7 half-built, keep `Arm`'s refusal above `WHEEL_SIZE` with its message re-pointed at `adr/0056`, and state the invariant the session extracted — *every live scheduled row is in exactly one of {armed, waiting}, and is unlinked when its owner row is freed* | **4** | ✅ **cleared** by session C → `adr/0056` | — | **done** → [`0016`](0016-the-event-wheel.md). **776 tests, no baseline moved** — hash-neutral, which was the slice's own acceptance test. **Two findings outrank the four tasks.** The end-of-run tier had been stamping every violation **Tick 0** in both 100,000-Tick runs, because each called `CheckEndOfRun()` on a *fresh* `Simulation` over an already-run world — invisible for as long as no end-of-run invariant read the stamp, and caught by the first one that did. And `Simulation._tick` is the **next** Tick to run rather than the last one run, which is why the period window is half-open at the bottom. Three `BinTests` fixtures also turned out to run time **backwards** — popping a row for Tick 1 and then depositing on Tick 0 — the fourth instance of a green suite agreeing with the code rather than the claim, and the first in a fixture's clock. The slice's shape is that **all three checks are relative to a *now* the wheel does not have**, which is the wall slice 8's `Adopt` hit too. **The plan found the fine wheel is not half-built but built**, and both halves of the invariant session C extracted have been in the tree, registered and tested, since slice 7 — so the four tasks are four *corrections* rather than the construction the row anticipated: a refusal message naming a design `adr/0056` refuted, a missing write-site refusal against a double-arm, a whole-world check **blind to a whole period** because it is written modulo the period, and an `Unlink` that discards the one signal saying whether it unlinked anything. Two of the three holes are reachable only through a **Ruleset reload** and a **save/reload** — the first in flight in slice 8 this week, the second guarded by the invariant session C found has never run |
| **10** | **Zone Rules — the second Rule family — all ten tasks done.** The Lot's permission set, the derived Lot→Building index, the `[[zone_rule]]` table and its three refusals, the sample and its third `purpose_tag`, the trigger in **Tick phase 6**, create, demolish, eviction, the tripwire, **both halves** of the long-run trend assertion, and `--zones`. **Planning inverted the slice's name** and the build inverted it back: the create predicate had to exist because a slice that only demolishes leaves `slots` flat against a *falling* `live`. Produced `adr/0053`–`0055`, **deleted one of its four unratified numbers by deriving it away**, and **amended `adr/0053` twice from the code** — the signal is a Rule asleep short of an *input*, and the clock lives on the Rule Instance. Three findings outlive it: the growth cycle **cannot be entered from a standing start**; the tripwire reads **1.56×**, so `02 §5.7` is *false in the letter and true in the substance* and the variable is the **working set**, not the Zone; and the city settles **five-sixths homeless** because **a Building has no declared occupancy** — filed, not tuned | **3c** (Sweep half) | ✅ **cleared** — *slice 7 was a dependency and not a gate, and it has since closed* | 3 | [`0014`](0014-zone-rules-and-the-sweep-family.md) |

**Running in parallel, on their own track:**

| # | Spike | Gate | Note |
|---|---|---|---|
| **S2** | Routing — travel-time matrix first, then HPA\* versus DSDV distance-vector | cleared | **The project's top risk.** Headless, needs no Godot. It decides whether 1M is reachable and it owns the **pathfinding cluster** size — which `adr/0040` decoupled from Chunk size, since the cluster is derived and rebuilt while the Chunk is in the save. **Planned in [`0010`](0010-s2-routing.md)** now that slice 1 has reported |
| **S1** | 20k Buildings via chunked `MultiMeshInstance3D` | none | Track B. Godot only |
| **S3** | One data panel with a live multi-series graph | none | Track B. Godot only. **The spike most likely to be skipped and most likely to change the decision** |
| **S0a** | The world at target size — 1M Citizens in `Borough.Headless` | cleared | **DONE.** The tables hold 1M in 86 MiB with an order of magnitude spare, and 100,000 Ticks at the target run in 11.75 s. It found that **run mode had never had a city in it** — capacity, zero rows — so every Tick figure before it was taken over an empty world. Numbers and six findings in [`spike-results`](../docs/spike-results.md) → *S0a* |
| **S0b** | The Tick with work in it — Event Wheel, Bin Rules with wait lists, a Sweep Rule pass, a routing load | 🔴 slices **7**, **9**, **10** | **Not run, and not runnable.** [`0002`](0002-open-questions.md) specifies S0 as four clauses and only the first is reachable today. **This is the half that carries `06`'s stated risk** — the sizing question is closed and the Tick-budget question is not |

### The hash-moving queue

**Phase 1's code is no longer *closed but for task 11*: there are three items, and two of them re-record
the same three golden baselines.** Session **N1** produced the second and third
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
| **1** | **Slice 10 task 11 — `revisit_ticks`** ([`adr/0059`](../docs/adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)) | yes | **First of the two hash-movers, because it is the only defect of the three that is live *now*.** `sample` is an absolute count, so a Lot is visited once per 0.12 Day at 1,000 Citizens and once per **117 Days at 1M**, and at target scale the shipped Ruleset builds **nothing**. Already sequenced behind slice 8, which has re-recorded |
| **2** | ~~**`adr/0063`'s wake predicate**~~ — derived requirement, level budget, `RuleInstance.shortfall` deleted — **DONE**, one baseline re-recorded | yes | **Ships with item 0**, which is red without it. ~~It cannot manifest until `pool` exists~~ — **struck: it is manifesting in the committed baseline now.** Acceptance is `BinWaitListTests`, which needs no `pool`: three `Deposit(1)` calls against a waiter requiring 3, plus the `Withdraw`/headroom mirror |

~~**Whether items {0, 2} come before or after item 1 is now the only open ordering question.**~~
**SETTLED by running them: {0, 2} went first, on the stated ground that a red suite outranks a defect at
a scale nothing currently runs at.** Item 1 is unblocked and holds the queue alone.

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

**Phase 2 and Phase 3.** Not from lack of interest but because the readiness review is unambiguous:
Phase 2's wall is `03 §5`, the traffic model — still the most detailed unargued design in the
project, now carrying transit vehicles under a Microscopic Cap whose value is unset — plus six
🔴 ADRs and S2. Planning it now would be writing task lists against decisions that a grilling
session will move. The instruction the corpus gives itself is *do not open Phase 2 content until S0
has run*, and S0 is slice 11.

**S0 has since split, and the instruction needs reading against the split.** **S0a is done** and it
closes the *sizing* half — 1M rows fit, with an order of magnitude spare, and nothing trends over
100,000 Ticks. **S0b is not runnable**, because the Event Wheel, Bin Rules and a Sweep Rule pass are
slices 9, 7 and 10. So the instruction cannot be read as *Phase 2 planning is now open*: what was
validated is that the tables hold the target, and what `06` actually names as the risk — that every
system **sized** against 1M rests on an unvalidated assumption — is closed for row counts and open for
the Tick. **The honest position is that K2 is unblocked on sizing and still blocked on the Tick
budget**, and the only spike with a number in that column is S2.

**The Godot shell.** [`dev-environment.md`](../docs/dev-environment.md) Track B stands up the
project and proves the boundary; S1 and S3 measure the ceilings. Nothing else in `Borough.Godot` is
planned until Phase 3, and `Borough.Godot` is deliberately absent from the Track A solution so that
the constraint *the headless runner never requires Godot* is enforced by there being nothing to
require.

**A save format.** Milestone 10, and it lands last in Phase 2 for a stated reason: a save format
written before the tables have settled is a migration chain written against nothing. Slice 4 builds
the **field declaration** the serialiser will one day be generated from, which is the part that is
expensive to retrofit; it does not build the serialiser.

**Content.** No Goods, no Zone families, no Policies, no Ruleset beyond what slices 7 and 8 need to
prove reload works. Content follows the Ruleset work and the axis on which variants differ is itself
still open.
