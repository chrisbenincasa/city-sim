# Desirability ships without its only positive term, and a caveat that must travel gets a test

**Milestone 9 composes two terms: `− w₂·pollution − w₃·noise`.** Amenity and shoreline are **absent
from the formula rather than defaulted to zero**, `MapLayers.Desirability` keeps its name and stops
throwing, and the resulting field is **a disamenity field** — every Cell rests at or below zero, and the
most valuable land in the city is empty land far from everything. **That shortfall is stated in
`02 §2.4`, in the composition's doc-comment and in the milestone's acceptance run, and a corpus check
refuses any document to claim desirability is composed without naming amenity's absence.**

Guiding concepts: `HONEST DEGRADATION`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
What a milestone composes is a question about what a milestone costs, and no number refutes an answer
to it. The facts it rests on were read out of `src/` and are not in dispute.

## Why

### An absent input is two different things and the corpus was treating it as one

[`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) decision 2 listed the
unbuilt terms together and asked what to do about them. Read against the build, they split, and the
split is what decides the milestone:

| Term | Present? | Is zero the truth? | |
|---|---|---|---|
| **pollution** | ✅ ships, diffuses | — | composes |
| **noise** | 🟡 unbuilt, **buildable** | **yes, where nobody drives** | composes |
| **shoreline** | ~~🔴 unbuilt~~ ✅ **built, milestone 24 task 7** | **it was yes; it is now a property of the world** | ~~absent~~ **composes** |
| **amenity** | 🔴 unbuilt | **no — and it is the only positive term** | absent |

⚠ **AMENDED 2026-08-24 by milestone 24 task 7, and the amendment is this table's own reasoning
arriving.** Shoreline's row said *zero in every world that exists* — that was a fact about the build,
because nothing had made a Water Body, and it stopped being true the moment
[`0160`](0160-a-sea-level-is-authored-ruleset-data-and-a-world-without-water-is-a-world-and-not-a-hole.md)
generated water and runoff filled a Bin. **`w₅` now composes.** ⚠ **What it composes is a term that is
still absent on most worlds** — a Ruleset omitting `[water]`, or stating it with no Bin, drops `w₅`
rather than adding a zero, which is this table's distinction applied one level down at the *Ruleset*
rather than at the *build*. 🔴 **The decision itself is unchanged and the caveat test stays**, because
the decision was never about shoreline: **amenity is the only positive term** and it is still missing,
so desirability is still bounded above by zero. ***Closing one of two holes is not closing the
caveat***, and the test's remarks now say so, because milestone 24 was the first occasion on which
somebody could have deleted it by mistake.

***Absent in the world and absent in the build are not the same absence.*** A term that is zero because
the thing it measures is not happening is a **correct** reading, and composing it is not the placeholder
[`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) shape (a) refuses. A term
that is zero because nobody built the mechanism is a **false statement about the city**. Only amenity is
the second kind, and the whole decision reduces to it.

### Noise is buildable, and more of what it needs already exists than the plan recorded

Segment volume is real, saved and maintained: `RoadSegmentTable.VolumeForward`/`VolumeBackward`,
incremented in `TripEngine` when a Vehicle enters a Segment and decremented when it leaves, **and the
increment does not depend on `[traffic]`** — the volume-delay table changes what a Segment *costs*,
never whether its traffic is counted. The occupancy-to-rate conversion noise needs also already exists:
`RoadSegmentTable.LoadOf` divides the standing count by free-flow travel time, so it is a flow and not a
headcount. **This was checked because it looked like a defect and it is not one.**

⚠ **But only two of the eight shipped Rulesets have anyone driving** — `congested.toml` and
`scarce.toml` at `car_ownership_percent = 100`, `taxed.toml` at an explicit `0`, and the rest by
omission. So in six of eight worlds noise is identically zero everywhere. **That is honest**, and
[`0098`](0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md)
already says so: *a city before the motor car is a legitimate city and is not a placeholder*. ⚠ It does
mean **the noise term is exercised in two Rulesets and inert in six**, which is where the acceptance run
has to be pointed, and it is milestone 7 task 5's finding arriving again — ***a term that varies only
where cars exist changes nothing in a world with no cars.***

### Amenity's blocker is narrower and different from the one on record

[`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) says amenity *"needs
Businesses and the Provider List"*. **Businesses shipped with milestone 10**, which is what
[`06`](../06-roadmap.md)'s own note records — *"milestone 10 builds the entity and its balance and
nothing else of it"* ([`0113`](0113-a-business-is-an-occupant-with-its-own-balance-and-a-building-never-holds-money.md)).
Read from `src/`, `BusinessTable` holds exactly three columns — `building`, `balance`, `building_next` —
and **no kind**. Amenity is *the count of distinct Business types reachable on foot*, so what is missing
is not the entity and not the Provider List: **it is that a Business has no type to be distinct in.**
One column and a walkable catchment query, and both belong to milestone **15**, which owns Agglomeration
by `06`'s own placement.

### What shipping without it actually does, stated rather than implied

The four-term formula has three negatives and one positive. Removing the positive leaves
`− w₂·pollution − w₃·noise`, which is **bounded above by zero**. Land value therefore rests at zero in a
clean, quiet, empty Cell and below zero everywhere else, and the field's maximum is unoccupied ground.

⚠ ***This is not a hole that fails loudly; it is a working mechanism that says something false about
cities, and it will be looked at and believed.*** It is the opposite failure from the one the named-hole
discipline was built for. A hole that throws is safe because nothing can read it. **A field that returns
plausible numbers for the wrong reason is read, believed and tuned around** — which is exactly the
sentence `MapLayers.Desirability`'s own doc-comment uses to justify throwing, now true of the thing that
replaces the throw.

**It ships anyway, because the producer is the milestone.** Holding `Desirability` closed until
milestone 15 means milestone 9 writes no caller, and a caller is the whole of what it exists to add to a
structure that has been in the tree since slice 3c. The alternative on offer — a Ruleset base value to
restore an upside — was refused: it invents a hash-bearing number whose only job is to stand in for an
absent term, so it could never be ratified against anything under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md), which asks a
ratifier to name a machine, a world and a quantity.

### The name stays and the caveat gets a test

`02 §2.4` says land value moves toward **desirability**, and `CONTEXT.md` uses the word throughout.
Shipping the partial composition under a different name would falsify that sentence and leave the corpus
carrying two concepts where it wants one, so `MapLayers.Desirability` keeps its name.

**Which means the caveat is doing all the work, and a caveat is the thing this corpus knows does not
travel.** `plans/0012` **Cause 5** is the standing record of clauses staying behind while their digits
move, and the **disqualifier registry** is the answer it already built: a test that refuses to let a
figure appear in any document without the phrase that says what it measures. **This extends that idiom
from a figure to a claim.** A document may say desirability is composed only if it also says amenity is
absent from it.

⚠ **The check is written by the task that makes the claim true, not before it.** Today no document
claims desirability is composed, so the test would pass vacuously — and a vacuously-passing obligation is
milestone 7 task 8's finding, ***an obligation naming a fixture that cannot satisfy it is not a demanding
obligation, it is an unread one***. It is scheduled against
[`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) task 2's Definition of done
rather than left as an intention, because ***an obligation nobody scheduled is indistinguishable from one
nobody wrote down*** (`plans/0012` check 7).

## Consequences

- **Two weights to author, `w₂` and `w₃`**, which is what
  [`0034`](../../plans/0034-the-land-value-target-and-the-composed-layers.md) decision 5 must find
  ratifiers for. Combined with [`0122`](0122-land-value-is-not-a-term-in-its-own-target-and-a-term-on-both-sides-of-a-lag-is-a-gain.md)
  deleting `w₁`, the milestone went from five weights to two.
- **Noise is built here**, as a point-of-use query on the Road Graph, summing and enumerating by
  loudness per `02 §2.4`. It is exercised in `congested.toml` and `scarce.toml` and inert in the other
  six, and **the acceptance run must be pointed at a Ruleset where it is not inert** or it asserts over a
  city with no traffic.
- **`MapLayers.Desirability` stops throwing.** `Fertility` does not — its blocker is the world generator
  at milestone 24 and is untouched by this.
- **A new corpus check**, owed by task 2 and scheduled in its Definition of done.
- **Milestone 15 gains a specific, small prerequisite**: a kind column on `BusinessTable`. It was
  previously recorded as *Businesses and the Provider List*, both of which exist.
- **The field is bounded above by zero for as long as amenity is absent**, so any downstream consumer
  reading land value before milestone 15 is reading a disamenity field. `04 §7`'s gentrification damper
  and `02 §5.9`'s contagion damper are the two most likely to be surprised by it.

## What would trigger revisiting

- **Milestone 15 lands amenity.** Then the positive term arrives, the field stops being bounded above by
  zero, and the corpus check is deleted rather than kept — a check policing an absence outlives its
  subject the day the absence ends.
- **A consumer is built that reads land value and assumes an upside.** That is the failure this ADR
  predicts; if it happens before 15, the answer is to bring amenity forward rather than to add a base.
- **The noise term is found to be inert in the acceptance run's own Ruleset.** Then the run is not
  testing the composition it claims to, and the Ruleset moves rather than the assertion loosening.
- **Somebody proposes a base value again.** It is refused here for a stated reason — no ratifier can
  exist for a number standing in for an absent term — and that reason expires the moment the term is
  present, at which point a base is a different proposal about a different thing.
