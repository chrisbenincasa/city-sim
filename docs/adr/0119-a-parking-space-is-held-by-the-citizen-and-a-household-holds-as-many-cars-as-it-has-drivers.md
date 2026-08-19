# A parking space is held by the Citizen, and a Household holds as many cars as it has drivers

**The object that holds a parking space is the `Citizen` — one saved column naming the Car Park it is
parked in, sentinel when it is parked in none.** Not the `Trip`, which
[`0009`](0009-parking-is-modelled-supply-never-search.md) names; not the `Traveller`, which
[`0084`](0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)
names and reads the agreement with `0009` as corroboration; and **not the `Household`**, which is the
subject that survives both journeys and is nevertheless the wrong one.
[`0084`](0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)'s
conservation sum is amended in place to count **Citizens holding a Car Park** rather than *Travellers
currently parked*.

Guiding concepts: `UNIQUE INDIVIDUALS`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) —
which object carries a column is a question about what the design already says, and no measurement
decides it. ⚠ **But the argument that settles it is a reading of the build rather than of the corpus**,
which is [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) working
as intended: the mechanism was opened (`World.ModeOf`, `World.cs:1117`) rather than inferred from the
sentences about it, and what the mechanism does is not what three documents' prose implies.

## Why

### Both named candidates are freed, and the corpus knew it one milestone ago

`0009` says *"a **Trip** must remember where it parked"*. `0084` says the **Traveller** carries the Bin,
and records the two requirements landing on one field as corroboration. `TravellerTable` does carry
`CurrentLeg`, so a Traveller spans all three Legs of one journey and an `O(1)` release is workable
*within* a journey.

**Both are freed when the journey ends**, and since
[`0101`](0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md) made a commute
**two journeys** the space is held across a gap in which neither object exists. `0009`'s own canonical
case is a car parked while no journey is happening at all — *"residential parking is mostly static
occupancy — a household's car sits at home overnight"* — named there as the reason residential and
commercial sheds must be balanced separately. So `0084`'s sum reads **0 = high** every night, on the
design's own headline case.

***What is freed is not always the subject, and it is the subject that decides the shape.*** That is
milestone 6 task 7's rule ([`plans/0028`](../../plans/0028-evidence-the-accumulators.md)), coined on the
milestone immediately before this one and arriving here **from the same end**: there the subject was the
Citizen and the freed thing was the Trip, and it is the Citizen again.

### The Household is the obvious repair and the build refuses it

The Household outlives both journeys, it is what owns the car under
[`0098`](0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md),
and it is what `0009`'s overnight sentence is grammatically about. `plans/0031` recommended it.

**It cannot hold the space, because a Household drives more than one car.** `World.ModeOf(citizenSlot)`
resolves the Citizen's Household, asks `HouseholdRuleset.OwnsCar`, and returns `TravelMode.Car` for
**every member of an owning Household**. A Household of three workers therefore puts three cars on three
Segments at three destinations at the same hour. One column on the Household holds one Car Park, so the
second driver's acquire would overwrite the first driver's holding — an acquire with no matching
release, which is precisely the [`0006`](0006-no-collection-grows-with-elapsed-time.md)-class permanent
capacity loss this milestone's named risk is about. **The repair for the invariant's operand would have
been the defect the invariant exists to catch.**

⚠ **`0098` says this out loud, in its own revisit trigger, and nobody had read it against parking**:
*"The one-car-per-Household assumption producing a visible artefact. A Household of three workers with
one car puts three cars on the road here."* It is filed there as a **fidelity** complaint — the fleet is
larger than a real one — and it is also a **structural** fact about where a car's location can live. ***A
revisit trigger names the reading that would reopen a decision, and says nothing about which other
decisions rest on the same fact.***

### The same argument is already written down for jobs, and it transfers without amendment

`CONTEXT.md` → Building, on `[[building]] jobs`:

> *"**It counts Citizens and never Households**: employment sits on the person, beside Experience and
> Skill Tier, and **two adults in one Household working opposite sides of the city is the case a
> per-Household count could not express**."*

Two adults in one Household **driving** to opposite sides of the city is the case a per-Household car
location could not express. This is not an analogy borrowed from a neighbouring mechanism; it is the
same sentence about the same two people, and the reason is the same both times — **a journey has a
traveller, and the traveller is a person.**

So this decision is [`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
family arriving on a further axis rather than a new claim, and it lands beside `PlannedCommute`
(`0101`), `LastTripFate` and `LastTripEndedDay` (milestone 6 task 7) — three columns already on the
Citizen for the identical reason, all placed after `0009` and `0084` were written.

### The missing vehicle is *undesigned*, and classifying it is what keeps this decision small

Under [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the absence must be classified
before it is reasoned from, and it splits in two:

- **Car ownership as a column is *refused*.** `0098` decides it is derived from the Ruleset in force,
  deliberately, because the owner set must be **nested** under a moving threshold. That is evidence.
- **A vehicle as a locatable thing is *undesigned*.** No ADR has been asked whether a car is a row. It
  is in neither of `06`'s inventories, for the reason `0098` already found about mode choice: ***an
  inventory of unplaced mechanisms structurally cannot list a mechanism nobody designed.***

Only the first is evidence, and it does not reach this question. **A `VehicleTable` is therefore not
refused here — it is not on the table**, and this decision is written so that it costs one column to
adopt later: a Citizen's holding becomes a Vehicle's holding, and the sum's left-hand side never moves.

## Consequences

- **`CitizenTable` gains one saved, hashed column.** It names a Car Park, sentinel for none. Saved
  rather than derived: where a car is parked is a fact about a past world, and *a value drawn once is
  derivable and a value measured once is not* (`0101`).
- **The write-site check stays `O(1)` and gets stronger.** `0084`'s `ParkingSpaceIsReleasedOnce` asserts
  *a release names a Car Park this Citizen holds*. One column makes holding two spaces **unrepresentable**
  rather than checked, which is the `Rule Instance` armed/waiting precedent — the shape the corpus
  already prefers to an assertion.
- **The conservation sum's right-hand side no longer goes to zero at night.** Σ occupied over all Car
  Parks = the count of Citizens whose column is set. A driving Citizen at home holds their home Car
  Park, which is `0009`'s static residential occupancy arriving as a consequence rather than as a
  special case.
- **A driving Citizen holds exactly one space at all times after their first drive**, and none while in
  motion: release on departure, acquire on arrival, which is the matched pair `0084`'s split assumes.
- ⚠ **Parking demand is denominated in *drivers*, not in *cars owned*, and the two differ.** A
  car-owning Household with no commuting member occupies nothing, so the overnight residential figure is
  *the count of driving Citizens at home* rather than *the count of cars in the city*. Under the fleet
  `0098` builds these coincide for everybody who commutes and diverge for everybody who does not.
  **Stated rather than fixed**: fixing it means giving a car a location independent of a driver, which
  is the `VehicleTable` this decision declines to force.
- **Nothing about the Household changes.** Ownership stays derived, `ModeOf` is untouched, and no column
  is added to `HouseholdTable`.

## What would trigger revisiting

- **A Citizen becoming able to travel in more than one mode on one Day.** Mode choice is `0098`'s
  *undesigned* half. A Citizen who drives to work and walks home holds a car park at a Workplace with no
  journey coming back for it, and the release site would have to be restated before that ships.
- **A `VehicleTable` arriving** — from `0098`'s fleet-count refutation, from freight, or from car
  ownership becoming endogenous (`01 §8` ledger #2's open half). The column moves from the Citizen to
  the Vehicle and the sum's left-hand side is unchanged; that is the cost, and it is one column.
- **A car being displaced without its holder acting** — a bulldozed garage, or illegal parking as an
  overflow tier (`deferred.md`). `0084` already names this as a second mutation site that would need the
  write-site predicate restated; it is named again here because the *holder* is what such a site would
  have to find, and finding every Citizen holding a demolished Car Park is a scan this design has no
  index for.
