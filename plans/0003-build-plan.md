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
| **1** | **S4 — the kernel benchmark** — **tasks 1–10 done on two machines, no tripwire fired; task 11 unblocked, XMP re-sweep now optional** | Phase 0 | none | 2 | [`0004`](0004-s4-kernel-benchmark.md), results in [`spike-results`](../docs/spike-results.md) |
| **2** | **The arithmetic substrate** — typed quantities, fixed point, tabulated `exp`/`log`, `draw()`, purpose tags | — | none | 3 | [`0005`](0005-arithmetic-substrate.md) |
| **3** | **The analysers** — CI lints 2, 3 and 7, plus `purpose_tag` uniqueness | — | none | 2 | [`0006`](0006-analysers-and-lints.md) |
| **4** | **Typed tables and the field declaration** | **2** | cleared | 3 | [`0007`](0007-typed-tables.md) |
| **5** | **The Tick, the Input Log and replay** | **1** | cleared | 3 | [`0008`](0008-tick-and-replay.md) |
| **6** | **Map Layers** | **3c** (Layers half) | cleared | 3 | [`0009`](0009-map-layers.md) |
| — | *the Phase 1 gate closes here* | | | | |
| **7** | Rule engine — Bins and Rules | **3a** | 🔴 `02 §4` residue | — | stub |
| **8** | Rule engine — hot reload | **3b** | 🔴 `adr/0015` | — | stub |
| **9** | Event Wheel | **4** | 🔴 `02 §7`, `adr/0006` | — | stub |
| **10** | Zone Rules | **3c** (Sweep half) | 🔴 depends on 7 | — | stub |

**Running in parallel, on their own track:**

| # | Spike | Gate | Note |
|---|---|---|---|
| **S2** | Routing — travel-time matrix first, then HPA\* versus DSDV distance-vector | cleared | **The project's top risk.** Headless, needs no Godot. It decides whether 1M is reachable and it owns Chunk size. **Planned in [`0010`](0010-s2-routing.md)** now that slice 1 has reported |
| **S1** | 20k Buildings via chunked `MultiMeshInstance3D` | none | Track B. Godot only |
| **S3** | One data panel with a live multi-series graph | none | Track B. Godot only. **The spike most likely to be skipped and most likely to change the decision** |
| **S0** | Synthetic 1M-Citizen city in `Borough.Headless` | slices 4–6 | Runs after the Phase 1 gate closes. Until it exists, 1M is a hope |

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
| **7** — Bin Rules | `02 §4` residue | **Fallback chain depth and cycle checking.** `on_fail` chains are the whole diagnostic story and are currently unbounded, with nothing stating a limit or a cycle check. Nine Resources and a Policy layer will make them longer. Also: whether `mean_workforce_experience` is a legitimate Building Readout, and what a predicate may read |
| **8** — hot reload | `adr/0015` | Never grilled. The roadmap says plainly it **must not slip behind 3c**, so it is gated on an argument, not on more code |
| **9** — Event Wheel | `02 §7`, `adr/0006` | Both never grilled. `02 §7` is partly spoken for by `adr/0033` and should be read against it rather than fresh |
| **10** — Zone Rules | slice 7 | A Zone Rule is a Sweep Rule. There is nothing to sample with until the Rule engine exists |

**Also owed, and not blocking Phase 1:** a TOML parser library is unnamed
([`dev-environment.md`](../docs/dev-environment.md) flags it as needed by slice 7). `adr/0003`
requires any core dependency be argued against it explicitly, so a determinism liability entering
the core needs a written exception. That argument is cheap and should happen before slice 7, not
during it.

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

**2. Map Layer diffusion cadence is called tuning and is not.** `02 §1.2` lists *"Map Layer
diffusion, every 32–64 Ticks, staggered"* in the **tuning** column. But cadence decides when a
source's contribution becomes visible to a Rule that reads the Cell, so two runs at different
cadences produce different cities — which makes it a **design change** under `05 §4`'s State Hash
rule, not a free knob. This is the same welding failure `adr/0034` found in Chunk size, one document
later. *Blocks: slice 6. Recommended handling: reclassify cadence as hash-bearing and Ruleset-authored
but world-creation-fixed, or produce the argument for why it is not.*

**3. The CI lint count disagrees across three documents.** `05 §4` enumerates seven; `adr/0036`
calls itself *"a sixth CI lint"* and says *"of the six CI rules this project enforces"*;
[`0002`](0002-open-questions.md) line 151 calls it the seventh. It is one rule with three counts.
Cosmetic, but it is exactly the kind of drift that makes a checklist stop being checkable. *Blocks:
nothing. Fix in `adr/0036` while slice 3 is open.*

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
