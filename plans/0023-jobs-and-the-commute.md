# 0023 — Jobs and the commute (milestone 5b-bis)

> The slice brief for [`06`](../docs/06-roadmap.md) milestone **5b-bis**, *Jobs, the commute, and the
> first Trip generator*.
> Decisions to be built: [`adr/0081`](../docs/adr/0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)
> (the milestone's own), [`adr/0017`](../docs/adr/0017-agents-satisfice-they-never-optimise.md),
> [`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md),
> [`adr/0069`](../docs/adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md),
> [`adr/0074`](../docs/adr/0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md),
> [`adr/0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md).
> Design realised: `02 §5.3`, `03 §3`, `CONTEXT.md` → Commute Budget, Trip, Workplace.
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

## Status

**🔨 IN FLIGHT (2026-08-12), and nothing gates it.** Milestone 5b closed the same day
([`0021`](0021-trips-legs-and-the-pedestrian-layer.md)); this milestone was created by the sitting that
closed it, and both of its ADRs were written then. There is no session in front of it and no measurable
claim it must wait on.

**It inherits three tasks from 5b** — the generator, the Trip Census family with the Commute Budget, and
the 100,000-Tick run — because each measures an origin-destination **distribution** and 5b had none to
measure.

## Why this milestone exists, in one paragraph

**Phase 2 as sequenced never produced a place a person would go.** All seven Trip generators the corpus
names sit in `06`'s *Mechanisms with no milestone*, so `0002` §A — *what generates a Trip* — had no
answer at any point in the plan, and 5b's tasks 4, 6 and 8 were each void as posed. The fix under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) is **build X**, and
`adr/0081` chooses which X: **the commute**, because it is the Trip the rest of the corpus is already
written against and the **only candidate that exercises the Commute Budget**, whose named ratifier is
*the first run long enough to produce a Trip cost distribution*.

## The named risk

`06` rule 2: **that every number milestone 5b was to produce is taken against a fabricated
origin-destination draw and lands in [`0013`](0013-tick-budget.md) and [`0002`](0002-open-questions.md)
as measured fact.** This is not hypothetical — **S2 R4 ran the experiment on the corpus's own
instruments**: a uniform origin-destination draw put a District-granular route's detour at 18.52% where a
local-trip draw puts it at **128.82%**, *"which under `05 §4` is a different city"*. A sampler would not
approximate the answer; it would fabricate one with the right shape.

> **⚠ And this milestone carries a known bias, stated by `adr/0081` before anything is measured.**
> Job choice is a function of **wage and commute** ([`adr/0026`](../docs/adr/0026-wages-are-posted-locally-and-never-cleared.md))
> and this builds the second term only, so **nothing makes a Citizen travel past a nearer acceptable
> job**. The commutes come out **too short** and every quantity derived from them — the Commute Budget
> percentile, peak pedestrian density, `0013`'s walk-search multiplicand — is biased **low**. *A number
> biased in a known direction is usable; the same number with an unknown bias is not.* Every figure this
> milestone publishes must carry that qualifier at its site, not only here.

## Tasks

**1. The spatial index: a query from a place to the Buildings near it.** It exists nowhere —
`CellResidency` indexes Map Layer cells rather than entities, and `Frontage` maps a Lot to its Segment in
one direction only. **It is task 1 rather than a detail of task 4** because it is the piece that is
reused: job assignment needs it, [`adr/0067`](../docs/adr/0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)'s
Provider List acquisition will need it, [`adr/0032`](../docs/adr/0032-services-are-delivered-by-trips-not-by-coverage.md)'s
Attended services will need it, and `02 §5.3`'s candidate sampling **already assumes it**. Built inside
one consumer it acquires that consumer's shape and is then rewritten twice.

> **The shape is already set by two things in the tree and should not be re-derived.** `CellResidency`
> is the precedent for *where it lives*: a flat array over the 128² Cell grid, plus-one encoded so a
> zeroed entry reads as **absent**, `(derived AND rebuilt)`, and **beside the table rather than in it**
> — `BOR0901` rejects storage in a `[Table]` that is not a declared column, and a per-Cell head is not
> per-row storage. `CLAUDE.md`'s standing rule is the precedent for *what it is*: **an intrusive index
> list** — a head index on the owner, a `next` index on the element, both flat. So: a head array over
> Cells beside the table, a `next` column on the Building rows, and no per-entity collection object.
> The query itself follows `layer_cells(aabb, layer)`, the project's first hot query — **allocation-free
> and string-free, and both are checked**.
>
> **It must be `(derived AND rebuilt)` and the test for that is `05 §3`'s, not convenience.** A
> structure earns that classification only if its **order** is recoverable from saved state rather than
> merely its membership. A Building's Cell is a function of its Lot's saved coordinates, so a rebuild
> reproduces this **exactly rather than plausibly** — but *only if the walk that rebuilds it visits rows
> in slot order*. Get that wrong and two identical cities disagree, which is the failure `0020` found
> for a wholly-derived table joining `World._tables`.

**2. `[[building]] jobs`, a fourth key.** Beside `name`, `bins`, `condemn_after` and `occupants`.
**Tuning, hot-reloadable and hash-bearing**, and subject to `adr/0068`'s rule for occupancy: **derived
from the Ruleset in force rather than frozen at construction**, so lowering it **evicts** the overflow. A
job has a holder and no consumer, exactly as occupancy does — *a Bin drains because a Bin has a consumer;
occupancy and employment do not, so they evict.*

> **This is where a second `[[building]]` kind enters a shipped Ruleset for the first time.** `dwelling`
> has been the only kind in all three files since the Ruleset existed, so **every loader path and Zone
> Rule path that has only ever seen one kind gets its first real exercise**, and `adr/0055`'s
> permission-set scoping gets its first case with something to distinguish. Expect defects here that are
> nothing to do with jobs, and file them where they belong rather than working round them
> ([`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).

**3. The `[trips]` Ruleset table.** It does not exist. It is created here and it is where the **Commute
Budget** and the **crossing cost** land — both hash-bearing, both currently unset, and **neither may be
chosen by argument** (`adr/0052`). The Budget's named ratifier is *this milestone's own first long run*,
so it is set from a **measured percentile** or it is not set at all; `CONTEXT.md` → Commute Budget fixes
its currency as **clock minutes, one currency across modes, with no per-mode weight** (session F). The
crossing cost applies only where two Addresses **share a Segment and differ in side** (`adr/0074`), and
**the derivation should be looked for before a value is picked** — a crossing is a real duration and
*half a signal period* is a property of a junction.

> **⚠ §B says which percentile the Budget must not be: the median.** It is 100% of the grid ideal in a
> severed city and an intact one alike, so a median Budget cannot distinguish the two cities the
> Severance demonstration exists to tell apart.

**4. The assignment pass.** A Citizen with no Workplace samples candidate Buildings with a free job slot
and takes **the first acceptable one** — `adr/0017`, satisficing, never the nearest and never the best —
where *acceptable* means **within the Commute Budget on foot**. It writes `CitizenTable.Workplace`.

> **`SyntheticCity`'s `(i * 7) % buildings` stride is DELETED, not left as a fallback.** Two ways to
> acquire a workplace is `plans/0012` *Cause 1* with both copies live and executing. **This moves the
> State Hash**, so the golden baselines re-record.
>
> **⚠ Predict three hash-bearing numbers here, because `adr/0081` predicts none and `adr/0069` is the
> standing counter-example.** Placement's ADR predicted no numbers and the pass needed **`interval`,
> `revisit_ticks` and `candidates`** — and the first of those shipped at a value copied from a default
> that meant something else, leaving 45% of the housing stock empty. This pass is the same shape: a
> sampled sweep over a population looking for something. **Derive what `adr/0059` says to derive** — the
> *sample* comes from a **duration** and the duration is authored — and put every one of them in `0002`
> §D with a named ratifier on the day it is written.
>
> **What happens to a Citizen whose Workplace is demolished is a decision, not a detail.** `adr/0054`'s
> precedent for a demolished dwelling is that the Households go to the Pool **with their money intact**;
> the employment analogue is that the Workplace clears and the Citizen re-enters assignment. Say it
> explicitly, because the alternative — a `HandleColumn` quietly resolving to nothing — is `Column.cs`'s
> own warning about a walk that cannot tell *unemployed* from *corrupt*.

**5. The commute generator, and the daily occasion.** A Citizen with a Workplace generates a commute Trip
on a daily occasion, through the same door 5b built: the Trip, its Legs and its Traveller, resolved in
Tick phase 4 by `TripEngine`, which needs **no change** for this. `TripPurpose.Commanded` is deleted here
— it exists so that the absence of a generator was legible in data, and its own doc comment says it is
expected to go.

> **The occasion is this milestone's one open design question, and `adr/0081` says it is small.** A
> commute is **scheduled and periodic**, so the sampled-sweep shape the Zone Rule and placement already
> use is available and **generalising the Event Wheel to a second table is not required**. Argue it here
> rather than assuming it — `adr/0059` is the standing warning about deriving a sample from a duration
> against choosing one.

**6. The Trip Census family, and the Commute Budget measured.** 5b's task 6. The four `TripCounter` flows
already exist and are already read; what this adds is the **cost distribution** the Budget is a
percentile of, and `TripFate.ExceededCommuteBudget` becoming reachable for the first time — it is
structurally unreachable until the Budget exists as Ruleset data.

**7. Something to look at.** `--zones`, `--roads` and `--trips` set the precedent, including **refusing
rather than degrading** when the Ruleset declares nothing. The picture worth printing is not a Trip
count: it is **where people work against where they live**, and the same city after the jobs move.

**8. The 100,000-Tick run.** 5b's task 8, and `06`'s definition of done: no collection and no magnitude
trending upward at steady state (`adr/0006`, and `adr/0003`'s extension to quantities). Plus 5b's task 4,
**peak pedestrian density**, which is the measurement the whole re-scope was about.

> **⚠ Read slice 10 task 11 before recording anything from this run.** *A baseline records what a run
> did, so a change that narrows what the run reaches is invisible in it by construction* — and
> `GoldenSessionCoverageTests` exists because 5a-bis nearly retired a verb from the baseline while
> producing a full set of freshly correct hashes. A commute that never fails reaches none of the Fate
> branches this milestone exists to exercise.

## What this milestone must not do

- **No wage, no Business, no vacancy posting, no labour market.** `adr/0026` is still owed a milestone,
  and `06`'s no-milestone row keeps its entry with the **assignment half struck**. A row deleted because
  part of it shipped is `plans/0012` *Cause 3*.
- **No second destination per Citizen.** One Workplace, one Trip a Day. `adr/0067`'s shopping and
  `adr/0032`'s school make a Citizen's day a **schedule** rather than a repeated Trip, and that is the
  point at which the daily occasion stops being a sweep — it is `adr/0081`'s own revisit trigger, not
  this milestone's scope.
- **No Segment volume, and the reason is not the one that looks obvious.** 5b left
  [`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s
  attribution unbuilt because direct attribution *"needs a next Segment every Tick"* and
  [`adr/0075`](../docs/adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) gives a Leg **a cost and
  no path**. Adding a commute does not supply one. **Volume is 5c's**, and the conservation invariant
  shipped in 5b is waiting for it.
- **No `Fidelity`.** Volume is 5b's and 5c's; the threshold that reads it is 7a's.

## Definition of done

The cumulative list in `CLAUDE.md` applies unchanged. Specific to this milestone:

- A Citizen's Workplace is **acquired by a mechanism**, and `SyntheticCity` no longer strides.
- A commute Trip is generated, routed, resolved and counted, with **every one of `adr/0076`'s four Fates
  reachable** — and the run that proves it says which ones it actually reached.
- The **Commute Budget is set from a measured percentile of this milestone's own distribution**, with the
  percentile argued and the bias qualifier attached, or it is **not set** and `0002` §D says why.
- The spatial index is **allocation-free and string-free on the hot path**, both checked, and it has a
  test that a rebuild reproduces it **exactly** rather than plausibly.
- All three golden baselines re-recorded, with the commit message saying what moved and why.

## The record

*(Filled in as tasks close.)*

### Task 1 — the spatial index, built

**`Space/BuildingResidency.cs`**: a Cell → Buildings reverse index, `(derived AND rebuilt)`, plus the
hot query `In(CellRect, BuildingTable, Span<int>)`. Two flat `int` arrays over the 128² Cell grid for
head and tail; `BuildingTable.CellNext` is the element side. Maintained at `World.CreateBuilding` and
`World.DestroyBuilding`, rebuilt in `World.RebuildDerived` after the Lot reverse index. Nine tests.

**Nothing here was invented, and that is the report.** Every choice was already made somewhere in the
tree and the task was to find which precedent governed: `CellResidency` for *where a coordinate-owned
head lives* and why `BOR0901` makes that correct rather than a dodge; `CLAUDE.md`'s intrusive-index-list
rule for *what it is*; `MapLayers.LayerCells` for the hot query's shape, down to truncating rather than
throwing; `Frontage` for *why it is not a registered table* — `plans/0020`'s finding that a wholly
derived table folds the allocator's four scalars and makes the hash depend on how many times it was
rebuilt.

**⚠ It is not a catchment, and the terms had to be kept apart deliberately.**
`LineSourceQueries.Amenity` already defines a catchment as *"a walkable catchment on the Road Graph — a
**time** rather than a distance"*, whose range *"no geometry on the Cell grid can express"*. This
answers the strictly geometric question and answers nothing about reachability. **The two stages must
not be merged**: the box supplies candidates and the walk decides acceptability, so *a Building across
an unbridged motorway is a candidate that fails* — which is a Severance the model can report. An index
that pre-filtered by reachability would delete that reading and would need the Road Graph's Epoch.

**The one real decision was `IndexList` gaining a `Span<int>` constructor.** Its owner here is a **Cell**,
which has no row, so head and tail cannot be columns; the element side stays a column because an element
*is* a row. The asymmetry is the point rather than a compromise — what makes it an index list is the
threading, not where the head is kept. The alternative was re-implementing the linking inside
`BuildingResidency`, which is a duplicate of the one structure `CLAUDE.md` says every collection in the
core must be.

**The test that licenses the declaration is the recycled-slot one**, and it is the only one of the nine
that discriminates. `InsertOrdered`'s own remarks give the failing sequence — *create A and B, free A,
create C, and appending gives `B, C` where a rebuild gives `C, B`* — so a maintained list built with
`Append` passes every membership assertion and fails only there. In production it would present as a
saved-and-reloaded city draining a list in a different order from a continuously-run one, **with nothing
to report it**, because the list is derived and therefore not folded.

**No baseline moved**, because `cell_next` is derived and derived columns are not hashed. That is
worth stating rather than assuming: it is the reason this task could ship separately from task 4, which
does move the hash.
