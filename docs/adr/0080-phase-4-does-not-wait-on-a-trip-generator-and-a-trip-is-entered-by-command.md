# Phase 4 does not wait on a Trip generator, and a Trip is entered by command

**Milestone 5b's Phase 4 machinery — the Traveller cursor, volume on the Segment, Fate resolution, the
Census sink `adr/0006` requires, and the three Movement tables entering `World._tables` at all — is
built now, driven by a new `CommandKind.Trip`. **A Trip therefore enters through the Input Log**, on
`CommandKind.Populate`'s precedent, so replay reproduces it by construction and nothing in the
simulation invents one. **The generator is not built and is not 5b's**: every generator the corpus
names is unmilestoned, so it is a scheduling defect rather than a design gap, and it is discharged by
[`0081`](0081-the-commute-is-the-first-trip-generator-and-a-job-is-taken-by-satisficing-on-distance.md)
and a new milestone. **Milestone 5b closes on tasks 1, 2, 3, 5 and 7**; tasks 4, 6 and 8 move behind
that milestone, because each of them measures a *distribution* and none can be honest without one.**

Settled by the sitting on [`plans/0002`](../../plans/0002-open-questions.md) §A — *what a Trip's
destination is* — which [`plans/0021`](../../plans/0021-trips-legs-and-the-pedestrian-layer.md) opened
and could not close, and which turned out to be void as posed under
[`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md).

`HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM` `FAST ITERATION`

---

## Why

### Every generator the corpus names is unmilestoned, and that is the fact the question turned on

`plans/0021` enumerates seven candidate generators, each owned by a decision that is not about Trips:
the **commute** (`CONTEXT.md` → Provider List), **shopping** ([`0067`](0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)),
**school** ([`0032`](0032-services-are-delivered-by-trips-not-by-coverage.md)), **dispatch**
([`0030`](0030-crime-is-an-incident-with-no-perpetrator.md)), **immigration**
([`0023`](0023-immigration-arrives-through-the-gate.md)), **Office export**, and **freight** (`03 §6.6`).

The brief typed the question as *which generator has a destination set that exists today*, and
recommended shopping. **All seven appear in [`06`](../06-roadmap.md) → *Mechanisms with no milestone*.**
Phase 2 as sequenced runs 5b → 5c → 6 → 7a → 7b → 8 → 9a → 9b → 10, and **not one of those milestones
produces a place a person would go.** So the question has no answer at any point in the plan, and no
reordering of Phase 2 produces one.

That reclassifies it. `adr/0070` requires an absence to be named and classified before anything is
decided on the strength of it, and warns specifically about questions of the form *given X does not
exist, should Y compensate?* — *"if X is unbuilt, the question is void and the answer is **build X**."*
Every one of the seven is **unbuilt**. The corpus had already reached the right verdict and stopped one
step short: it concluded *therefore not shopping*, where the conclusion available was *therefore
nothing, until a milestone exists*. **A generator is not a task in a slice about Legs; it is the
milestone that slice was scheduled in front of.**

### The risk 5b exists to retire is retired, and the generator was never what retired it

`06` names 5b **the irreversible one**, and states the risk exactly: *"a single-Leg Trip model
propagates into Lot valuation, cost functions and every balance constant, and is what Citybound could
never undo."*

The commitment that risk is about is the **shape of the Trip**, and it has been taken.
[`0075`](0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md) split it three ways and tasks 1–3 built
it: a `TripTable` with `leg_head`/`leg_tail` over an intrusive list, a `LegTable` with `next`, a
`TravellerTable` with `current_leg`, and every Leg created eagerly at Trip creation. **A single-Leg
model is not expressible in that structure**, which is what retiring the risk means.

What tasks 4, 6 and 8 add is **measurement**, and measurement is not what makes a milestone
irreversible. Holding 5b open for it keeps the project's most protected milestone
(`06`: *"the one to protect if the phase runs long"*) open on work that could not be done, which is how
a milestone stops being a unit of risk and becomes a container.

### A sampled generator is refused on the measurements it would corrupt, not on principle

`plans/0021` refuses a sampler by citing [`0069`](0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)
— *a number does not settle what a mechanism settles.* That is the right instinct with the wrong
warrant, because it would refuse a sampler used for anything, including instrumentation, and `--trips`
is a sampler this slice shipped on purpose.

The specific reason is stronger and is **already measured elsewhere**. Every measurement tasks 4, 6 and
8 exist to produce is a property of the **origin-destination distribution**:

| What it measures | Owner | Why the distribution is the whole quantity |
|---|---|---|
| Peak pedestrian density per block face | `0002` §B-16 | A uniform draw concentrates nowhere; real trips concentrate on whatever the city clusters |
| The Commute Budget as a percentile | `0002` §D2, 5b task 6 | A percentile **of a Trip cost distribution**, and meaningless before one exists |
| The walk search's multiplicand | [`0013`](../../plans/0013-tick-budget.md) | §B measured the unit and found there is none: cost is ≈37 ns × distance², so the **length distribution** is the lever |
| Mean Legs per Trip | `0002` §B-17 | The mode mix, which is a property of what people are travelling *for* |

**S2 R4 already ran this experiment and it is in the corpus as a warning**: the spike's uniform
origin-destination draw *"had been hiding a conclusion"* — a District-granular route's detour is 18.52%
on that draw and **128.82%** on a local-trip draw, *"which under `05 §4` is a different city."* A
sampler here would not be an approximation of the answer; it would be a **fabricated answer with the
right shape**, which is the failure mode that costs most to discover late.

### A command is honest in a way a sampler cannot be, and the precedent is `Populate`

Phase 4 is empty (`Simulation.Move` — *"Empty until Phase 2 of the roadmap"*), and the three Movement
tables are **not in `World._tables`**, so they are outside the State Hash and nothing constructs a row.
None of that is blocked by the absent generator. What is blocked is only the question *which pair*.

`CommandKind.Trip` answers it by refusing to. A commanded Trip is in the **Input Log**, which is
`CommandKind.Populate`'s exact shape and for the same reason — S0a moved the populator into Phase 0 so
that *"the population is in the Input Log and replay reproduces it by construction"*. It makes no claim
about what residents do, because a human asked for it, so nothing downstream can mistake it for one.
That is `HONEST DEGRADATION`: a named absence that announces itself, against a plausible number that
does not — the `Scope.Pool` precedent, one layer up.

It also unblocks the parts of task 5 that are real work in their own right and have nothing to do with
destinations: increment-on-entry and decrement-on-exit volume attribution
([`0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)), the Traveller advancing
its cursor, and — the one `adr/0006` actually requires — **a Fate reaching the Census before the row is
freed.** `TripTable`'s own doc comment flags that as owed: *"this table needs a sink, and *Trips are
transient* does not supply one."*

## Consequences

- **Milestone 5b closes on tasks 1, 2, 3, 5 and 7.** Tasks 4 (the generator), 6 (the Census family and
  the Commute Budget) and 8 (the 100,000-Tick run) move behind the milestone `0081` opens. 5b's
  *payoff* — Severance — is already delivered: `--trips` measures detour, and the finding that
  **Severance is a tail rather than a median** (p90 140% → 306%, p99 663% → 913%, median 100% in both a
  healthy and a severed city) came out of task 7 with no generator anywhere near it.
- **`CommandKind.Trip` is a new verb in the Input Log**, so it is save-format and replay surface. It
  takes an origin and a destination **Building**, not an Address — an Address is derived through the
  Lot on the Epoch (`0078`), and a command that named one would be recording a value that a road edit
  can invalidate.
- **The three Movement tables join `World._tables`**, which is what puts them in the State Hash and
  gives every column its `saved AND hashed` / `derived AND rebuilt` declaration a consequence. They are
  hashed from the moment the verb exists, so the golden baseline gains them and the re-record is owed
  in the same commit.
- **A commanded Trip must be indistinguishable from a generated one to everything downstream.** Phase 4,
  volume, Fate resolution and the Census may not learn where a Trip came from. If they could, the
  generator would arrive to find a second code path waiting for it, which is the *two drifted copies*
  failure with the copies in the same file.
- **`0013` gains no row from this and that is deliberate.** A commanded Trip has no rate, so it prices
  nothing; the walk-search row stays half-open with its multiplicand owed to the new milestone.
- **The Commute Budget stays unset and 5b still may not choose it** (`adr/0052`). Its named ratifier —
  *the first 5b run long enough to produce a Trip cost distribution* — is transferred to the new
  milestone rather than discharged, and the row must say so, because a ratifier that silently changes
  owner is a ratifier nobody is waiting on.

## What would trigger revisiting

- **A generator arriving before the machinery is finished.** If `0081`'s milestone lands first, the
  command's job is done and it becomes a test and debugging affordance rather than the only door. It
  should not then be deleted — `Populate` did not stop being useful when the city could grow — but the
  claim *Phase 4 does not wait* has been discharged and this ADR is history.
- **A second consumer wanting to create a Trip from inside a Tick.** The command is Phase 0; anything
  creating a Trip in Phase 4 or Phase 6 is a generator by another name, and it must be argued as one
  rather than added as a call site. That is `0069`'s lesson pointing the other way: `World.Place` had
  one caller and the mechanism was missing; a `Trip.Create` with one caller in the Rule engine would be
  a mechanism nobody argued.
- **The Fate set proving too small once real Trips exist.** [`0076`](0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md)
  closed it at four on a two-clause rule, against commanded Trips that cannot exercise
  `ExceededCommuteBudget` at all — because the Budget does not exist. The first real distribution is
  the first thing that can test that closure.
