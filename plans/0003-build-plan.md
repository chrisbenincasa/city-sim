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
| **7** | **Rule engine — Bins and Bin Rules** — **tasks 1–9 of 10 done.** Bins with no public level column, the two wait lists, the Ruleset loader and its five refusals, quoted decimals, atomicity over net deltas, the apply band, the Readout set, `on_fail` chains, and `02 §4`'s counters. Produced `adr/0049` and `adr/0050`, took the **Resource family** out of order to stop a money leak six slices old, and measured the first price the Tick has ever had on it — **82.84 ns an evaluation**. ~~Task 10 ships the first Ruleset with content in it~~ — **split while being planned**: it asked for a production chain over two or three Goods, which is `pool`, which this plan's own decision owed 3 had already made a named hole that throws. **10a is the wiring** — the first Ruleset ever to reach a `World`, plus a hash-bearing arming stagger — and closes the slice. **10b is the content, re-filed to Phase 2** | **3a** | ✅ **cleared** by session A → `adr/0048` | 3 | [`0011`](0011-rule-engine-bins-and-rules.md) |
| **8** | Rule engine — hot reload | **3b** | ✅ **cleared** by session A → `adr/0048` | — | stub |
| **9** | Event Wheel | **4** | 🔴 `02 §7`, `adr/0006` | — | stub |
| **10** | Zone Rules | **3c** (Sweep half) | ✅ **cleared** — *waits on slice 7, which is a dependency and not a gate* | — | stub |

**Running in parallel, on their own track:**

| # | Spike | Gate | Note |
|---|---|---|---|
| **S2** | Routing — travel-time matrix first, then HPA\* versus DSDV distance-vector | cleared | **The project's top risk.** Headless, needs no Godot. It decides whether 1M is reachable and it owns the **pathfinding cluster** size — which `adr/0040` decoupled from Chunk size, since the cluster is derived and rebuilt while the Chunk is in the save. **Planned in [`0010`](0010-s2-routing.md)** now that slice 1 has reported |
| **S1** | 20k Buildings via chunked `MultiMeshInstance3D` | none | Track B. Godot only |
| **S3** | One data panel with a live multi-series graph | none | Track B. Godot only. **The spike most likely to be skipped and most likely to change the decision** |
| **S0a** | The world at target size — 1M Citizens in `Borough.Headless` | cleared | **DONE.** The tables hold 1M in 86 MiB with an order of magnitude spare, and 100,000 Ticks at the target run in 11.75 s. It found that **run mode had never had a city in it** — capacity, zero rows — so every Tick figure before it was taken over an empty world. Numbers and six findings in [`spike-results`](../docs/spike-results.md) → *S0a* |
| **S0b** | The Tick with work in it — Event Wheel, Bin Rules with wait lists, a Sweep Rule pass, a routing load | 🔴 slices **7**, **9**, **10** | **Not run, and not runnable.** [`0002`](0002-open-questions.md) specifies S0 as four clauses and only the first is reachable today. **This is the half that carries `06`'s stated risk** — the sizing question is closed and the Tick-budget question is not |

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
| **9** — Event Wheel | `02 §7`, `adr/0006` | Both never grilled. `02 §7` is partly spoken for by `adr/0033` and should be read against it rather than fresh |
| **10** — Zone Rules | slice 7 | A Zone Rule is a Sweep Rule. There is nothing to sample with until the Rule engine exists. **It has also inherited an obligation**: slice 5 task 7's long-run trend assertion is stated over a rising `slots` against a flat `live`, and the Rule engine creates no rows — a Rule Instance's life is its Building's — so **no Ruleset can make a slot count trend**. Buildings arriving and being demolished is what churns rows, and that is this slice. Slice 7 keeps only the **flow** half (`0011` finding 36) |

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
