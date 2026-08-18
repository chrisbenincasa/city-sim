# 0031 — Parking

`06` milestone **7**. The brief.

---

## Status

🟡 **SCOPED 2026-08-17. Eight tasks, four open decisions, none of them started. Ungated** — session
**H** cleared this row on 2026-08-12 and the clearance is written in
[`0002`](0002-open-questions.md) §F2 as well as on the board, so both copies agree.

**This is the best-specified row in the Phase 2 spine and it is not the smallest.** Three ADRs settle
most of it in advance — [`adr/0009`](../docs/adr/0009-parking-is-modelled-supply-never-search.md)
*parking is modelled supply, never search*,
[`adr/0083`](../docs/adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md)
*a shed's use is the arrival query*, and
[`adr/0084`](../docs/adr/0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md)
*parking occupancy is two checks* — and two questions in [`0002`](0002-open-questions.md) already name
**this milestone's own first run** as their ratifier. What scoping added is below, and three of the four
findings are preconditions nobody had counted.

⚠ **The sharpest finding is that the thing which holds a parking space does not exist, and two ADRs
each put it on an object that cannot hold it.** `adr/0009` says *"a **Trip** must remember where it
parked"*; `adr/0084` says the **Traveller** carries the Bin, and calls the two requirements landing on
one field *"corroboration"*. **Both are freed at the end of the journey**, and `adr/0009`'s own
canonical case is a car that is parked when no journey is happening — *"residential parking is mostly
static occupancy — a household's car sits at home overnight."* Since
[`adr/0101`](../docs/adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md)
made a commute **two journeys**, the space is held across a gap in which neither object exists.
***What is freed is not always the subject***, which is milestone 6 task 7's rule
([`0028`](0028-evidence-the-accumulators.md)) arriving on the milestone immediately after it was
coined — and arriving from the other end, because there the subject survived and here **the subject has
never been built**. `adr/0098` makes car ownership a **derived Household property and never a column**,
so there is no vehicle row anywhere in `src/`. **Decision 1 below.**

⚠ **That finding carries a live defect in `adr/0084`.** Its conservation sum is stated as *"Σ occupied
over all parking Bins = count of Travellers currently parked"*, and a car parked overnight has **no
Traveller**, so the invariant as specified fails on the design's own canonical case. The right-hand
side has to be denominated in whatever decision 1 settles. The ADR is not wrong about the **tier**, the
**split** or what each half catches; it is wrong about one operand, and it was writable only because
the state did not exist — which is that ADR's own argument working against it.

⚠ **The renumber has seven live traps in `src/` and `tests/`, and no instrument in this corpus can see
them.** Eight code comments say *milestone 8*; **seven mean parking and are now 7**, and **one**
(`tests/Borough.Tests/Rules/CondemnationTrailTests.cs:317`, the derived-column rebuild risk) means
Save/load and is **correct**. They are the same string. `plans/0000`'s *"the renumber's one live trap
and it was four"* counted only the four in `06`, because every mechanical check this corpus has
compares a **document to another document** ([`0012`](0012-corpus-audit.md) check 8's own lesson).
[`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
repair — *name a symbol, never a time* — does not reach this: **a milestone number is neither a symbol
nor a time**, and it is the one citation form that goes stale without anybody editing the thing it
cites. Task 5 fixes the seven, and the general form is filed to `0012`.

⚠ **A generated city cannot vary parking occupancy, for 5c task 8's reason on a third axis.** Capacity
is per building **kind** and demand is per **Citizen**, so occupancy is a ratio of two quantities the
generator sizes from the same population — ***the same number sizes both the demand and the supply***,
which is `foot_crossing_every` and the job-search box's structural inertness a fourth and fifth time.
The shed radius's ratifier is *"the walk-Leg length distribution as shed occupancy approaches 1"*, and
**nothing this project can generate approaches 1**. So it is named here, before task 1, rather than
discovered at task 8: the ratifier needs **a machine, a world and a quantity** —
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
as amended twice, the second amendment being 5c's own. **Decision 3.**

---

## Why this milestone exists, in one paragraph

**A car has nowhere to be.** `TripEngine.Itinerary` (`TripEngine.cs:409-432`) builds
[`adr/0008`](../docs/adr/0008-walking-is-a-simulated-leg.md)'s three-Leg car commute correctly and
sets both middle waypoints to `World.VehicleAccessPoint`, which is `PedestrianAccessPoint`'s body
under another name (`World.cs:1092` → `:1152`) — so **Leg 0 runs `X → X` and Leg 2 runs `Y → Y`**, both
priced at `TravelTime.Zero`. That is session F's named placeholder, chosen deliberately and documented
at the site. The whole of parking is prose: **no type, no table, no column, no `Resource`, no Ruleset
section, no `Invariant` member.** `[parking]` is a hard load refusal (`RulesetLoader.cs:383-389`), a
Bin has no Address (`BinTable.cs:59` — only a `Handle<Building>`), and there is no proximity scope
(`Ruleset.cs:15`). What this milestone builds is the place a car goes, and the walk it costs you to
leave it there.

---

## The named risk

**That parking is abstracted into a District average, losing the specific diagnosis — and that
occupancy leaks, which is an [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md)-class
permanent capacity loss.** `adr/0009` states the payoff as a sentence a District average cannot
produce: not *"downtown averages eight minutes from your car"* but *"you parked three blocks away
because the two nearer garages were full"* — which is the **Evidence** pattern, built one milestone
ago, with named constituents rather than a statistic wearing a face.

⚠ **The second half of the risk is the one with a schedule, and it is why this row carries two
invariants rather than one.** A Traveller that disappears without releasing its space destroys capacity
**for ever**, silently, in a quantity that never recovers. `adr/0084` splits the check because the two
halves catch different things and **neither substitutes for the other**: the write-site check cannot
see a Traveller that vanished without reaching a write site, and the sum cannot say *which* one leaked.

⚠ **A third risk is recorded rather than retired, and it points the other way.** `adr/0008` states the
direction of error out loud: **until this milestone, driving carries no access cost at all**, so every
balance struck between 5c and here is *optimistic about cars*, and **this milestone makes every car
Trip worse rather than better.** Every congestion figure and every Commute Budget rung in the corpus
was taken on a journey missing both ends. That is not this milestone's to re-take — it is a note that
those figures are provisional and the direction is known.

---

## What is already decided, and must not be re-opened

Recorded compactly because a reader arriving here will otherwise re-derive it, and because two of these
are **prohibitions** that become enforceable for the first time in this milestone.

| | Decided | By |
|---|---|---|
| **Supply, never search** | Arrival queries the shed **nearest-first** and takes the first Bin with capacity. Cars never drive around hunting. A proximity **Rule scope** was refused outright — *movers choose; Rules transform* | `adr/0009`, `02 §4.3` |
| **One caller, one occasion** | The shed is consulted on **arrival** and on nothing else. A release does not consult it — a departing car knows its Bin. A **second caller reopens the use rate** and is that ADR's own revisit trigger | `adr/0083` |
| **No `T`, no rotation, no proximity wake** | The shed inherits [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md)'s invalidation **shape** and not its parameter, because a stale shed returns a Bin that exists, has capacity and is merely not the nearest — an error **bounded by the radius and already priced by the Commute Budget** | `adr/0083`, `05 §3` |
| **Per-Segment, witnessed by paths** | The witness set is the Segments on the walk paths to the Bins the shed **kept**, not the ball it explored. At 400 m the ball touches 22 Segments and the kept paths touch **2** | `adr/0083`, S2 R5.6 |
| **Two invariants, neither per-Tick** | Write-site `O(1)` release check; end-of-run conservation sum. `02 §10` sorts tiers **by frequency, never by importance** | `adr/0084` |
| **No *no parking* Trip Fate** | The Fate set is closed at four and this was refused **by name**: pressure arrives as a **gradient of rising walk times** before it arrives as failure | `adr/0009`, [`adr/0076`](../docs/adr/0076-the-trip-fate-set-is-closed-at-four-and-a-fate-names-the-journey.md) |
| ⚠ **The vehicle Access Point is replaced, never supplemented** | At this milestone that path is **replaced**. It was 5c's *sole* path to a parking location and must not survive as a fallback | `adr/0008`, session F |
| ⚠ **An exhausted Shed widens and may never return the vehicle Access Point** | Otherwise **a full car park costs less than an empty one** and the player is paid for the shortage, with the payment growing as the shortage does | `adr/0008`, `World.cs:1084-1090` |

⚠ **The last two are the only prohibitions in this corpus that have never been enforceable.** 5c
examined the drive-Leg endpoint trap and found it had **no ground** — a fallback from an exhausted Shed
cannot occur when no Shed exists ([`0026`](0026-statistical-resolution-and-the-travel-time-matrix.md)).
**This milestone is where the ground arrives**, and the failure mode they forbid is one a reasonable
implementation reaches for on its own: the query returns *no capacity*, and the nearest valid Address
in scope is the one already in the array.

---

## Tasks

**Eight. The order is a dependency chain and is not free**: the shed needs Bins and a radius, the query
needs a shed, the endpoint swap needs the query, and the run needs all of it.

### Task 1 — the parking Bin

The supply. A row with an **Address** — `(Segment, offset, side)`, which is what every *where is it*
query in this project takes — a capacity, and an occupancy. Capacity per building kind is Ruleset data
under `adr/0009` (*"parking capacity per building type is Ruleset data, not code"*), so it is declared
on `[[building]]` beside `occupants` and `jobs`, and it is **derived from the Ruleset in force** on
[`adr/0068`](../docs/adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s
rule, now on a sixth axis.

⚠ **It is very probably not a `BinTable` row, and decision 2 settles it.** `CONTEXT.md:252` reserves
the `Bin` type for **Goods and Money** by name; a `BinTable` Bin is located by `Handle<Building>` and
has no Address; its `Resource` is a `ResourceId` drawn from the Ruleset's `[[resource]]` list; and its
two **wait lists** are meaningless here, because `adr/0009`'s superseding note turns on exactly that —
***nothing about parking ever waits.*** Four structural mismatches against one name.

⚠ **The over-capacity rule needs stating rather than inheriting.** `adr/0068` **evicts** and
[`adr/0064`](../docs/adr/0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md)
**drains**, and the two differ on whether the quantity has a consumer. A parked car has a **holder**,
like a job and unlike a Good — so the shape to copy is `[[building]] jobs`' **dismissal**, and what a
dismissed car does is a question this task must answer rather than discover.

### Task 2 — the `[parking]` table and the shed radius

A new Ruleset section holding the radius, plus the loader's refusal message, which enumerates the
sections in prose and would otherwise be wrong the moment a twelfth is added
(`RulesetLoader.cs:383-389`).

⚠ **The radius is hash-bearing and has been unratified since 2018-era prose** — it decides which Bin a
car takes, which decides the walk Leg, which counts against the Commute Budget, which decides whether
the Trip fails. `0002` §D2 holds it. **Two constraints are stated and neither is a value:** it is
bounded above by the Commute Budget's walk allowance, since a shed wider than a Trip can afford to walk
has outer Bins that can never be taken; and its cost gradient is measured at **110 Bins found at 400 m
against 596 at 800 m**, so doubling the radius is roughly 5× the shed. ⚠ **Do not reach for the
walking-time intuition** — five minutes at 5 km/h is 417 m, and `adr/0044` had to measure exactly that
sort of number back out of three documents that cited it as settled. **Decision 3 names what ratifies
it**, and that must be written on the day the number is, not at task 8.

### Task 3 — the Parking Shed

The cached per-Building membership: **the ordered set of parking Bins within the radius of a
pedestrian Access Point**, `(derived AND rebuilt)`, an **intrusive index list** — a head on the owner
and a `next` on the element, per `05 §4`'s rule that makes lint 7 satisfiable, and `IndexList.cs:31`
already names the Parking Shed as one of its three intended consumers.

It passes `05 §3`'s saved-versus-derived test where the wait list fails it: **its order is distance**,
a pure function of the road graph and two positions, all saved — so a rebuild reproduces the same
*order* and not merely the same members. Ties break on the target Bin's index, legal **only** because
the query is rebuilt wholesale rather than accumulated.

⚠ **This is the per-Segment Epoch's second reader**, after 5c's route cache, and the first non-routing
one after 5a-bis's frontage — which is the evidence that `CONTEXT.md` → Epoch's *when you pay / what
survives* distinction generalises rather than being a routing idiom.

### Task 4 — arrival, acquire, and what holds the space

The query, nearest-first, taking the first Bin with capacity, and the write that records who holds it.
**Decision 1 settles what the holder is**, and this task is where it lands.

The **write-site release check** ships here rather than in task 6, and the reason is `adr/0084`'s own:
it is a check *at the write site*, `O(1)`, on a condition that holds **by construction** — which is
`TripHasAFate`'s shape, and construction is exactly what a later edit breaks. Registering it later
would be writing it at the moment its author is least able to notice the pairing is wrong, which is
5b's argument for shipping `SegmentVolumeIsConserved` vacuously, running forwards.

### Task 5 — the endpoint swap, and the walk that stops being free

`TripEngine.cs:423-424` — two lines. `waypoints[1]` and `waypoints[2]` stop being
`World.VehicleAccessPoint` and become **the Address of the Bin the car took**, so the flanking Legs
stop being `X → X` and acquire a real cost. Release happens on departure and consults **no shed**.

⚠ **Three things this task must do that are not the swap.** `World.AccessPoint(int, TravelMode)`
(`World.cs:1139`) is a mode dispatcher with **no production caller** — written for this milestone and
dead since — so this task either gives it one or deletes it; a dispatcher nobody calls is a claim about
the build that no test holds. The two `adr/0008` prohibitions become **enforceable** and want a test
each, in both directions, because the failure they forbid pays the player for the shortage and is
therefore invisible as a bug and visible as a balance problem. And the **seven stale `milestone 8`
comments** are repaired here, since this is the task that touches most of their files.

⚠ **This is the task that moves every number.** All three golden baselines re-record; the Trip cost
histogram, the three Commute Budget rungs and `jobs beyond budget` all move, and the **direction is
known and is worse**. `adr/0100`: moving the hash costs nothing while nobody is carrying a save, and
citing hash movement as a reason to defer or split is itself a defect.

### Task 6 — the conservation sum

`ParkingOccupancyIsConserved`, end of run, on `TrafficIsConserved`'s and
`BinCapacitiesMatchTheirDeclarations`' precedent. **The next free `Invariant` id is 40** — the enum runs
`None = 0` through `CitizenIsInExactlyOneWorkplace = 39` — and `adr/0084` refuses to reserve ids in
advance, because an id travels in a crash artifact and a reused id cannot be un-reused.

⚠ **Its right-hand side is not what `adr/0084` says it is**, and this task carries the correction: the
sum is against **whatever decision 1 makes the holder**, not against *Travellers currently parked*.

⚠ **This task is also where `plans/0012` check 7 stops being hypothetical for its founding entry.**
That check — every invariant `02 §10` names has an enum member, and every member is registered or
explicitly marked unbuilt with the milestone that owes it — was **filed by session H off this very
obligation**, which was at *four documents and zero builds*. Building it here does not build the check;
it removes its worked example, so the check should be built **before** this task rather than after, or
its motivating case will have evaporated.

### Task 7 — something to look at

The **tenth** runner mode, after `--traffic` (eighth) and `--evidence` (ninth). The quantity is
`adr/0009`'s own sentence: **where people parked against where they were going**, and the walk between.
A grid of occupancy is a grid of parking supply and `--zones` already draws the land use that produced
it — so the thing to print is the **gap**, which is the same reasoning that made `--commute` print a
balance rather than a count.

### Task 8 — the long acceptance run, and the two questions it ratifies

The `adr/0006` obligation over whole **Days** rather than a round 100,000 Ticks, on 5c task 8's
precedent: every parking figure taken from Tick 0 is taken with employment still ramping, and one of
5c's was an artefact of exactly that.

**It is the named ratifier for three things**, and `0002` names two of them itself:

1. **The shed radius** — `0002` §D2, via the walk-Leg length distribution as occupancy approaches 1.
2. **Does parking scarcity degrade as a gradient?** — `0002` §B. `adr/0009` chose graceful degradation
   *"because it converts a cliff into a gradient"*, and **the refuting number is the same
   distribution**: a jump is a cliff, whatever the mechanism intended. The number and the claim ratify
   together and neither needs its own machine.
3. **What a shed query costs per arrival** — `0002` §B, and `adr/0083`'s own third revisit trigger:
   R5.6 measured the shed's **invalidation** and *"the query has never been measured"*, while `adr/0009`
   pays it on **every arrival**. It is the one number that decision assumes rather than knows.

⚠ **All three need a world, and the generated city is not it.** See decision 3.

⚠ **A fourth reading is available for free and is `adr/0083`'s own first revisit trigger**: the
fraction of Trips failing on Commute Budget **whose shed was stale about an addition**, measurable by
rebuilding the shed on failure and re-querying. If it is material the repair is `adr/0012`'s proximity
wake over the witness set, and `d` already exists as a number.

---

## What this milestone must not do

- **Not reintroduce search.** It is the ADR's title and it survives every amendment to it.
- **Not add a fifth Trip Fate.** *No parking* was refused by name, the set is closed at four, and the
  outcome it would express already has a channel — the Commute Budget.
- **Not let the vehicle Access Point survive as a fallback**, and not let an exhausted Shed resolve to
  it. Both are `adr/0008` prohibitions and both become live here for the first time.
- **Not give the shed a staleness parameter.** No `T`, no rotation, no proximity wake — that is
  `adr/0083`'s central saving and paying for it here would be paying for a bound the Commute Budget
  already provides.
- **Not build illegal parking as an overflow tier.** `deferred.md` parks it, retrofit cost **low**, and
  its trigger is a playtest finding that the gradient is *too* gradual. Its slot is named — one more
  tier consulted after the legal ones — and nothing structural changes when it lands.
- **Not build a player toggle for street parking.** `adr/0009`'s tool table names *allow or ban street
  parking per Road Segment side*, and that is a **seventh verb** against a list
  [`adr/0091`](../docs/adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)
  fixed at six with the user in the room. ⚠ **It is in no verb list, no inventory row and no
  milestone**, which is a filing this milestone owes `06` whichever way decision 4 goes.
- **Not balance residential and commercial parking with one number.** `adr/0009` says the two *"must be
  balanced separately"*, and the reason is structural rather than aesthetic: residential occupancy is
  **static overnight** and commercial occupancy is a **flow through the day**.
- **Not tune the Commute Budget to absorb this milestone's cost.** The rungs are percentiles of a
  distribution that is about to change, in a known direction, for a modelled reason. Re-reading them is
  `adr/0095`'s fourth revisit trigger and belongs to whatever takes that reading, not to a task here
  that wants its numbers to look unchanged.

---

## Definition of done

The four cumulative obligations from `CLAUDE.md`, plus:

- **Both invariants exist, at their stated tiers, and each has a test that makes it fire.** Not one
  invariant, not one tier, and not a sum standing in for the write-site check.
- **A shed that is full widens**, and a test asserts it does **not** return the vehicle Access Point —
  the assertion written in the direction of the prohibition rather than of the happy path, because the
  forbidden behaviour is one that reads as generous rather than as broken.
- **The walk Leg's cost is non-zero for at least one Citizen in the committed golden session**, so the
  baseline covers the mechanism. ⚠ Slice 10 task 11's rule: *a baseline records what a run **did**, so
  a change that narrows what the run **reaches** is invisible in it by construction* — this is a
  positive coverage assertion, of the kind `GoldenSessionCoverageTests` holds, and not a hash.
- **The long run reports no leak over whole Days**, with the collection half stated on the same *who
  reads it* axis milestone 6 had to use if the occupancy magnitude turns out to have no sink.
- **The State Hash moves, deliberately, once**, with a commit whose subject says why. All three golden
  baselines re-recorded.
- **The tenth runner mode prints something no other mode can produce**, and it prints its numbers
  before it asserts anything — 5c task 8's rule, because an acceptance run that speaks only on success
  is one you cannot use on the day it fails.

---

## Open decisions this milestone owes, before the task that needs them

### 1. What holds a parking space — and it is not a Trip or a Traveller. **Owed before task 1**

**The problem.** `adr/0009` puts the field on the **Trip**; `adr/0084` puts it on the **Traveller** and
reads the agreement as corroboration. `TravellerTable` carries `CurrentLeg`, so a Traveller does span
all three Legs of one journey and `O(1)` release is workable *within* a journey. **Both are freed when
the journey ends.** `adr/0101` made a commute **two journeys**, and `adr/0009`'s own canonical case is a
car parked while no journey exists at all — *"a household's car sits at home overnight"*, named there as
the reason residential and commercial sheds must be balanced separately.

**So the space is held across a gap in which neither candidate exists**, and `adr/0084`'s conservation
sum — *Σ occupied = count of Travellers currently parked* — reads **0 = high** every night.

**What is missing is a car.** `adr/0098` settles car ownership as **derived from the Ruleset in force
and never a column**, deliberately, so nothing in `src/` is a vehicle. Under
[`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) the absence must be
**classified before it is reasoned from**, and the classification is not obvious: ownership is
*refused* as a column, which is evidence — but a **vehicle as a locatable thing** is a different
question that no ADR has been asked, which makes it *undesigned* and puts it in the class `06`'s
inventory structurally cannot list.

**Recommendation, for the sitting rather than for this document.** The subject that outlives both
journeys is the **Household** — it is what owns the car under `adr/0098`, it is what `adr/0009`'s
overnight sentence is about, and a Household is not freed between Trips. **A car's location is then a
Household property and the invariant sums against Households whose car is parked**, which is a
right-hand side that does not go to zero at night. ⚠ **It is a recommendation and not a decision**,
because it is one Household one car by construction and `01 §8` ledger #2 does not say that.

### 2. Is a parking Bin a `BinTable` row, or its own table? **Owed before task 1**

Four structural mismatches against the shared name, listed under task 1: a `BinTable` Bin has **no
Address** and is located only by `Handle<Building>` (`BinTable.cs:59`); its `Resource` comes from the
Ruleset's `[[resource]]` list; it carries **two wait lists** in a mechanism where *nothing ever waits*;
and `CONTEXT.md:252` reserves the type for **Goods and Money** by name. Against that, `CONTEXT.md:784`
says *"a parking Bin **will** have"* an Address — future tense, and the only sentence in the corpus that
reads as though the two are one type.

**Recommendation: its own table**, and `CONTEXT.md` gains the distinction rather than the word being
stretched. ⚠ **Whichever way it goes, the vocabulary decision is the load-bearing half** — this project
capitalises domain terms and holds one meaning per term, so *Bin* meaning two things is a `CONTEXT.md`
edit, not an implementation detail.

### 3. What ratifies the shed radius — a machine, a world, **and a quantity**. **Owed before task 2**

`adr/0052` as amended twice. The machine is task 8's run. **The world is the open half**: occupancy is
capacity-per-kind against Citizens-per-kind, both sized by the generator from one population, so a
generated city holds occupancy **flat** across every population — ***the same number sizes both the
demand and the supply***, exactly as it did for `v/c` in 5c and for the job-search box in 5b-bis.

**The precedent is `rulesets/congested.toml`**, which exists because `minimal.toml` cannot demonstrate
congestion and *that is measured*. The parallel is close enough to be worth stating and not close
enough to copy: that file made a Street absurd (400 Vehicles an hour against a shipped 3,600) and said
so in its own header. **A sixth Ruleset making parking scarce is the likely shape**, and it should be
authored the way the fifth was — one number changed, the sweep that chose it carried in the header, and
**the word *demonstration* rather than *city* in the first line**.

⚠ **The quantity clause is the one 5c had to add and is easy to lose here.** `car_ownership_percent`'s
refuting readings were named against **reach** and its live consequence turned out to be **congestion**,
so both readings fired and refuted the wrong thing. The radius's live consequence is the **walk**, so
its refuting readings must be denominated in walk length and not in Trip failures — a radius that never
produces a walk longer than the shortest band is too small, and one at which the distribution never
moves as occupancy climbs is not being reached.

### 4. Does supply exist at Road Segments in this milestone, or only at Buildings? **Owed before task 1**

`adr/0009` says parking is *"a Bin held by Buildings **and by Road Segments**"*, and street parking is
therefore half the supply model as designed. Its **player tool** is a seventh verb and is refused above.
Its **capacity** is authorable without one.

**Recommendation: Buildings only in this milestone, with the structure forbidden to foreclose Segments** —
a parking Bin carries an Address by decision 2, and an Address is `(Segment, offset, side)`, so a
Segment-held Bin needs **no new mechanism** and no new column. What it needs is content and a balance
pass, which is a second thing to tune in a milestone that already owes a radius and a per-kind capacity.
⚠ **File the omission on the day**, in `06`'s inventory, with the verb beside it — this milestone is
about to make *parking exists* true, and a half-built supply model reads as complete from outside,
which is `plans/0000`'s *a partially-shipped milestone reports as shipped* on a smaller scale.

---

## What this milestone hands to the ones after it

- **17 — Decline** gets a pressure source it can read: parking scarcity arrives as Commute Budget
  failures with a named cause, which is one of the three `CONTEXT.md` sources and is **not** the one
  milestone 6 routed there.
- **21–23 — the traffic cluster** gets its congestion figures taken on **whole journeys** for the first
  time. `06`'s own edge — *7 → the traffic cluster's numbers* — is what this discharges, and it was one
  of four edges session E had to add because *a dependency stated in a row's prose is not a dependency
  the graph knows about*.
- **Nothing is unblocked by this row that was blocked by it**, and that is worth saying plainly: like
  milestone 6, this row's position is argued from what it costs to delay rather than from a dependency
  anybody is waiting on.
