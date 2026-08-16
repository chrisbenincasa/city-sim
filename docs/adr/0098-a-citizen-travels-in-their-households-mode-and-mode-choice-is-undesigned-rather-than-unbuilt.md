# A Citizen travels in their Household's mode, and mode choice is undesigned rather than unbuilt

**A Household keeps a car or does not, drawn from a `[households] car_ownership_percent` in the Ruleset;
a Citizen of one that does drives everywhere, and one of a Household that does not walks everywhere.**
Nobody weighs a walk against a drive on the day. That is not a simplification of a mechanism this
corpus specifies — **there is no such specification**, and the absence had never been classified.

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `LEGIBLE CAUSE`, `UNIQUE INDIVIDUALS`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) as
to the shape — whether mode follows a persistent Household state or a per-Trip comparison is a question
about what the design already says, and no measurement decides it. **The rate is not arguable** and
carries a named ratifier under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).

## Why

### The milestone needed a vehicular Leg and what actually blocked it was that nobody decides who drives

`plans/0026` task 5 is *the vehicular Leg at Statistical resolution*, and its stated blocker was a
decision about parking: [`0009`](0009-parking-is-modelled-supply-never-search.md) makes parking modelled
supply, session F warned that a drive Leg with no Parking Shed must not get a zero-length parking walk,
and milestone 7 owns the Shed.

⚠ **That decision had already been taken, in `adr/0008`, and reading it wrongly nearly shipped a
violation of it.** The first cut of this work built a car commute as **one** door-to-door Leg, on the
ground that `World.VehicleAccessPoint`'s doc-comment forbids the trap — which it does. But the trap it
forbids is a **fallback from an exhausted Shed**, and `adr/0008`'s decision line is unambiguous and
unamended: *a car commute is therefore never one Leg; it is at minimum `walk → drive → walk`*. Session F
went further and **named the placeholder**: the flanking Legs run from the pedestrian Access Point to the
vehicle one, *"which are equal by construction today, making those Legs zero-length"*, and the milestone-8
retrofit is *"one endpoint swap"*. So the design does not defer the three-Leg shape — it specifies it,
with the placeholder chosen and its cost named. **A car commute here is three Legs.**

***A doc-comment forbidding one shape is not a decision permitting the others.*** That is
[`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) exactly: the
comment was a description of the build, it was correct, it told us which symbol to read, and what was in
it was a *prohibition* rather than the *specification* — and a prohibition read as a specification is
silently permissive everywhere it does not reach.

The thing that did bind was upstream of all of it. `CommuteEngine` is the only Trip generator in the
project and it made every Citizen walk. To make one drive, something has to say who drives — and nothing
did.

### The absence is *undesigned*, which is the class this corpus has no instrument for

[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) requires an absence to be classified
before anything is decided on it: **unbuilt** (specified, no builder), **undesigned** (no specification),
or **refused** (a decision says no). Mode choice is:

- absent from every milestone row in `06`;
- absent from `06`'s ***Mechanisms with no milestone*** list, whose opening line is *"every row below is
  settled by an ADR and appears in no milestone anywhere in this document"*;
- named by no ADR;
- touched only obliquely — [`0032`](0032-services-are-delivered-by-trips-not-by-coverage.md) makes *mode* a Provider List
  attribute, which is about reaching a service and not about commuting.

So it is **undesigned**. And `adr/0070`'s own rule then applies: a question of the form *given mode choice
does not exist, should the commute compensate?* is **void as posed**, and the answer is to design the
thing rather than to pick a side.

⚠ **The finding that outlives this decision is about the inventory, not about modes.** `06`'s list of
unplaced mechanisms is the corpus's one instrument for *this exists and nothing schedules it*, and it can
only hold rows an ADR settled. ***An inventory of unplaced mechanisms structurally cannot list a mechanism
nobody designed*** — the undesigned class is invisible to the only place anybody would look for it. That
is why this reached a task before anybody noticed, and it is the **fourth consecutive milestone** to find
a precondition it had not finished counting: 5b task 4 had no destination set, 5b had no path, scoping 5c
found volume needs vehicles and not merely a path, and now the vehicles need a driver.

### The design already answers it, one level up, and names its own simple assumption

`01 §8` ledger #2 is *is car ownership a choice?* — **live and half-answered**. Session five settled the
half that matters: **ownership is a persistent Household state**, with a purchase price and a per-Day
running cost. What is open is only whether it is *endogenous* — bought when commutes get bad, sold under
pressure — and `01 §8` says of that in its own words: *every Household owning a car is the simple
assumption… only becomes interesting once transit exists.* Transit has no milestone.

So an exogenous ownership rate is **the design being followed**, not a patch over its absence. It also
lands on the entity the design puts it on. A Household owns a car; a *Trip* does not have a mode share.

**And it is the better shape for what this milestone needs.** A Household that owns a car drives every
day, so its commute route is stable — which is the property [`0060`](0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md)'s
Habit and 5c's route cache both rest on. A per-Trip coin flip would have given each Citizen a different
route on alternate mornings and made every cache measurement in this milestone meaningless.

### Ownership is derived from the rate, never stored

`HouseholdRuleset.OwnsCar` is `hash(seed, household id, tick 0, CarOwnership) % 100 < rate`. There is no
column.

This is [`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
rule and `TripRuleset.TryRung`'s, arriving on a fourth axis: **do not store what the Ruleset in force can
derive.** A saved bit would be
[`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)'s frozen-at-construction defect —
retuning the rate would move the Households built after the reload and leave every standing one carrying
the old file's opinion, which makes a key in a hot-reloadable file **silently world-creation-fixed**.

The draw's Tick coordinate is zero, the second such tag after `CommuteDeparture`, because it answers *what
sort of Household is this* rather than *what happens now*.

⚠ **What the derivation buys is that the owner set is *nested*.** Ownership is a fixed per-Household draw
compared against a moving threshold, so lowering the rate takes cars from the Households at the top of
their own ordering and disturbs nobody else. A saved column re-rolled on reload would churn the whole city
for a one-point change; a saved column not re-rolled would not respond at all. **Neither alternative is a
smaller version of this one — they fail in opposite directions**, and a test walks the rate down five
rungs asserting nobody ever acquires a car.

### A Citizen is judged for a job on the clock they travel on

`EmploymentEngine` now judges a candidate workplace in the seeker's own mode.

That is [`0008`](0008-walking-is-a-simulated-leg.md) rather than a refinement. Session F refused a
per-mode weight on the Commute Budget **precisely so that a walk and a drive are compared on one clock**,
and a single clock only works if it is read in the mode the journey is actually made in. A driver judged
on walking time would refuse jobs they can reach in ten minutes, and the shortfall would arrive in the
Census as a labour-market finding.

It is also the reason the rule lives on `World.ModeOf` and not in each engine. `CommuteEngine` makes the
journey and `EmploymentEngine` judges it; two copies of the rule would let a Citizen accept a job because
they could walk to it and then drive there. That is `plans/0012` **Cause 1** written in code — one fact
stored twice, and one copy drifting.

## Consequences

**A car commute is three Legs — `walk → drive → walk` — and the two walks are zero-length.**
`TripEngine.Start` takes Building slots rather than Addresses, so the per-Leg Access Point choice happens
**inside the one door** and no caller can get it wrong; `Itinerary` is the method a reader is sent to.
⚠ **The multi-Leg machinery 5b built had never been exercised** — every Trip in this project had exactly
one Leg, so `AdvanceTravellers`' cursor, `TripTable.Append`'s list and `mean Legs per Trip` were all
running on their trivial case. **Building the shape now is what keeps milestone 7's retrofit to one
endpoint swap**, which is the cost `adr/0008` priced it at.

**A drive Leg is free-flow.** No parking, no junction delay, no congestion.
`WalkRouting.Cost` takes a `TravelMode` and selects the subgraph, the connectivity labels and the speed
rule from it; a pedestrian's pace is capped at the walker's ceiling and a driver's is the road's own
free-flow. ⚠ **For a walk, free-flow is the exact answer and `03 §3.7` says why — pedestrian networks do
not saturate. For a drive it is an underestimate, in the one mode where the error grows with the city.**
5c task 6's volume-delay function is the term that fixes it, and until it lands every drive in this
project is quoted too cheap.

**The crossing cost is charged to pedestrians only.** [`0074`](0074-side-of-street-is-a-property-of-the-access-point-not-of-the-graph.md)'s
term is a *pedestrian* crossing. A driver's equivalent — the U-turn or the trip round the block that
reaching the far kerb actually costs — is a property of a **junction**, and this simulation models no turns
at all. Charging the pedestrian figure to a car would be inventing that mechanism at a number chosen for
something else. The visible artefact is that **driving across the street is cheaper than walking across
it**, which is true of this model and false of a city, and is bounded by one Segment.

**`[households]` is accepted in a Ruleset with no `[trips]`, and `[jobs]` is not.** The asymmetry is
deliberate and has its own test so nobody repairs it. `[jobs]` needs the Commute Budget because the search
box is *derived* from it; nothing derives anything from a car, so with no Trip model the rate is **inert
rather than unbounded**.

**The Trip Census family gains two counters** — `walk legs` and `drive legs`, Legs *created* by mode.
⚠ **The family now holds three denominators that do not cross**: the four Fates count Trips that *ended*,
the seven cost bands count Trips that were *created*, and these two count **Legs**. In a city of drivers
`walk legs` is **twice** `drive legs` and neither equals the Trip count. They reach `--census`, because
5b-bis task 6 established that ***a Census family with no reader is a family nobody can see***.

⚠ **The job-search box is still derived from the Commute Budget at *walking* speed, and for a driver it is
therefore far too small.** That is a live defect and it is already filed to `plans/0002` §C with a
measurement — the box covers 44.9× the golden fixture's city and holds 100.0% of the Buildings in the
world up to about 160,000 Citizens, so it filters nothing in any world this project can build and the walk
search is the real bound. **This decision does not repair it and makes it sharper**: a driver's catchment
is a pedestrian's box until a city outgrows the box, at which point the clipping starts and is silent.

**Nothing in the shipped Rulesets states `[households]`, so no State Hash moved.** Under `05 §4` this
whole change reads as an **optimisation** on the committed session however it was motivated. ⚠ That is a
safety net and not a result: it says the walk path is untouched and says nothing about whether the drive
path is right. The drive path's evidence is its own tests and the measurements below.

**Measured, at 100% ownership, on the shipped geometry** — the **drive** Leg's route length in Segments,
taken off running cities and **not fitted to anything**. The two flanking walks cross no Segment at all
today, which is what *zero-length* means and is why they are not in this table:

| Citizens | Commutes | Median | p90 | Max | Mean |
|---:|---:|---:|---:|---:|---:|
| 1,000 | 245 | 1 | 3 | 5 | 1.7 |
| 4,000 | 1,074 | 4 | 8 | 12 | 4.1 |
| 8,000 | 2,137 | 6 | 11 | 17 | 6.1 |
| 16,000 | 4,403 | 8 | 16 | 26 | 8.8 |

⚠ **Do not extend this table by fitting it.** That is exactly the error `plans/0012` **Cause 5**'s seventh
sighting records — 5c task 4 fitted `√population` through five points of the *foot* distribution and ran
straight through the mechanism that caps it. **The mechanism here is different and much weaker**: the
50-minute ceiling at a Street's 50 km/h reaches 41.7 km, wider than the paved extent of any city this
project can currently build, so on the car side **the map bounds the route and the Budget does not**. A
bound that comes from the fixture moves when the fixture does.

⚠ **A drive and a walk take the same Streets on this fixture, and that is a property of the fixture.**
Both shipped Rulesets set `arterial_count = 0`, so there is no Segment anywhere that admits one mode and
not the other, and the two distributions agree exactly — 1,074 routes, mean 4.08 Segments, in both modes.
***A comparison run on a fixture that lacks the mechanism under comparison measures the fixture.*** It is
recorded because the number changes the moment a player lays an Arterial, which
[`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) makes the only way one can now
exist.

**`01 §8` ledger #2 is half-closed rather than closed.** The exogenous half is built; the endogenous half
stays open on `01 §8`'s own terms, and its trigger is transit.

## What would trigger revisiting

**Transit existing.** `01 §8` names it as the condition that makes endogenous ownership interesting, and
[`0029`](0029-transit-is-in-and-right-of-way-is-the-only-axis.md) lists *"Ledger #3 (car ownership) goes live"* among the things transit unparks. At that point ownership stops
being a rate and starts being a purchase, and this ADR's first paragraph is what gets replaced.

**Anybody designing mode choice.** This decision is what a corpus does when a mechanism is undesigned:
follow the design one level up and say so. A specification for how a Citizen weighs a walk against a
drive — cost, comfort, weather, whatever it turns out to be — supersedes the *Household mode* half of this
outright, and leaves the ownership half standing.

**A car journey acquiring a cost that is not free-flow.** 5c task 6. Every number in this ADR is a
free-flow number, and the moment a drive can be slow the comparison between the modes changes shape rather
than magnitude — a congested drive can be *worse* than the walk it replaced, which nothing here can
express.

**A Parking Shed existing.** The two flanking walks stop being zero-length, `adr/0008`'s trap becomes
reachable for the first time, and the rule that a full car park must not cost less than an empty one goes
from a comment to a thing that has to be enforced. Every cost figure in this ADR moves upward, and the
job-search box's mismatch below stops being about speed alone.

**The one-car-per-Household assumption producing a visible artefact.** A Household of three workers with
one car puts three cars on the road here. That is what `01 §8`'s simple assumption says and it is wrong
about a city; the refuting reading is a Vehicle count that a real fleet cannot account for, and it arrives
with the Microscopic tier rather than with anything in 5c.

**The rate turning out to change what the city *is* rather than how fast it moves.** The named ratifier is
5c task 8's long run. The refuting readings are stated in both directions: an ownership rate at which
`jobs beyond budget` never leaves zero means the Budget has stopped binding on anybody and the labour
market has become supply-limited by accident; a rate at which the Commute Budget rungs' populations
collapse into *fast* means the three rungs have stopped being a vocabulary.
