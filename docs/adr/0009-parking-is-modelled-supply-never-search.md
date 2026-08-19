# Parking is modelled supply, never search

> **Superseding note (session six).** *"Acquisition and release are Rules"* below is wrong, and correcting it produced a general rule worth more than the correction. Parking Bins are mutated by **Bin transactions performed by the Trip**, sharing the Settle phase's deterministic ordering but **not** the Rule engine's execution model ([`0033`](0033-two-rule-families-scheduled-and-swept.md)).
>
> The tell is that **nothing about parking ever waits.** A Bin Rule that fails on flour sleeps until flour arrives; a car whose nearest space is taken does not sleep until one frees — it takes the next Bin further out, and the longer walk *is* the cost. If the whole shed is full the Trip fails immediately with Fate *exceeded commute budget*, which is exactly why this ADR refused a *no parking* Fate. Parking has no `on_fail` chain and no subscription, so it cannot be a Rule.
>
> Nor is the shed a Rule *scope*. The general form: **movers choose; Rules transform.** Nearest-first selection among nearby options — this shed, an Amenity set, a Provider List, `0030` dispatch — always belongs to something that moves, never to a Building's Rule. This is what removed the proposed proximity scope from the Rule engine and fixed the scope list at four.
>
> It also explains why a shed can never borrow the District Pool's radius. A **District is bounded by where transport can be ignored**; a **shed is bounded by where transport must be measured**, because per [`0008`](0008-walking-is-a-simulated-leg.md) the walk Leg is its entire output. Same reason, opposite direction, so the two scales cannot coincide.
>
> Unchanged: everything about supply, the shed query, graceful degradation, and the player tools. Newly owed to `05 §3`: **cached per-Building shed membership invalidated by the Road Graph Epoch** is named below only under *what would trigger revisiting*, and it should be a data-layout item rather than a contingency.

**Parking is a real, spatial, player-controllable resource: ~~a Bin~~ a `Car Park` held by Buildings ~~and by Road Segments~~. Cars never search for a space.** Arrival queries the destination's **Parking Shed** — the ~~parking Bins~~ **Car Parks** within acceptable walking distance of its pedestrian Access Point — nearest-first, and takes the first with capacity. Scarcity degrades **gracefully**: the shed widens, the walk Leg lengthens, and a Trip fails only when it exceeds its Commute Budget.

> ⚠ **AMENDED IN PLACE 2026-08-18 by [`0120`](0120-a-car-park-is-not-a-bin-and-supply-is-at-buildings-until-a-segment-needs-one.md).** Two words in the headline, and **the title survives both**: *modelled supply, never search* is untouched, and so is every sentence about the shed, the query, graceful degradation and the player tools.
>
> **It is not a `Bin`.** Four structural mismatches against the shared word — a `BinTable` Bin has no **Address**, its Resource is a Good, it carries **two wait lists** in a mechanism this ADR's own superseding note says *never waits*, and `CONTEXT.md` reserves the type for **Goods and Money** by name. That reservation cites [`0068`](0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)'s test — *a Bin has a consumer and occupancy has none* — and a **parked car has a holder**, which puts it on the `jobs` side of that line. ***The corpus had already put parking outside the `Bin` type while calling it a parking Bin.***
>
> **Road Segments hold none *yet*, and that is an omission rather than a reversal.** Street parking is half the supply model as designed here and it needs content plus a second balance pass. Nothing structural is spent by waiting: a Car Park is located by an **Address**, which is already `(Segment, offset, side)`, so a Segment-held one needs no new column. ⚠ **The player tool stays refused** — *allow or ban street parking per Road Segment side*, in the table below, is a **seventh verb** against a list [`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) fixed at six. Its **capacity** needs no verb, so only the verb is refused.

## Why

The instinct to abstract parking away came from conflating two separable things. **Parking supply** — spaces existing as placed, spatial, scarce resources — is cheap and interesting. **Parking search** — cars driving around hunting for a space — is the expensive agent behaviour that puts an unbounded query in the hot path of every arrival. Only the second is dangerous, and rejecting the first because of the second was an error.

**The "not a traffic-management game" anti-goal does not apply here.** That anti-goal targets *lane-level* tools: signal phasing, turn restrictions, dedicated turn lanes. Parking is a **land-use** decision. Choosing that a Lot becomes a garage rather than an apartment block is precisely the kind of choice this game is about. The anti-goal was never in tension with this; it simply addressed a different layer.

What modelling supply buys that a District-level parking average cannot:

- **Specific diagnosis instead of averaged diagnosis.** Not "downtown averages eight minutes from your car" but "you parked three blocks away because the two nearer garages were full." That is a fact with named constituents, which is the **Evidence** pattern working as designed rather than a statistic wearing a face.
- **Parking consumes land.** A garage occupies a Lot that could have held housing. This is the actual urban trade-off, and here it is *felt* through the zoning the player does rather than asserted through a desirability term.
- **It cashes in the Access Point split.** [`0008`](0008-walking-is-a-simulated-leg.md) separated pedestrian and vehicle Access Points as cheap optionality. A car's true access point is wherever it managed to park, which is not its destination — that is the whole mechanism.
- ~~**It reuses machinery that already exists.** Parking is a Bin; acquisition and release are Rules; the Settle phase already provides serial, deterministically-ordered mutation of shared resources. Nothing new is invented.~~ ⚠ **WITHDRAWN — all three clauses fell, to three different decisions, over three sittings.** *Acquisition and release are Rules* went to this ADR's own superseding note (session six); *parking is a Bin* went to [`0120`](0120-a-car-park-is-not-a-bin-and-supply-is-at-buildings-until-a-segment-needs-one.md); the Settle phase's ordering is the one clause still standing. ***A bullet listing several reuses is several claims, and nothing re-reads the others when one is struck*** — this one survived a superseding note that refuted its own second clause, four sections above it. That is [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s *an `and` in a consequence is two consequences* (session F) arriving on a list rather than on a sentence.

**Graceful degradation was chosen over a hard failure** because it converts a cliff into a gradient. A dedicated *no parking* Trip Fate would mean a district that dies abruptly the moment supply is exceeded. Instead, pressure shows up first as rising walk times — visible, attributable, and correctable — and only becomes Trip failure when the Commute Budget is genuinely blown. This also avoids adding a second failure channel where an existing one already expresses the outcome. `HONEST DEGRADATION`

## New player tools this creates

Parking capacity per building type is Ruleset data, not code. The player's levers:

| Tool | Effect |
|---|---|
| Place a parking structure | A Building that is mostly a large ~~parking Bin~~ **Car Park**, consuming a Lot |
| Allow or ban street parking per Road Segment side | Trades kerb space against capacity |
| Residential parking provision by building type | Data-driven: a detached house carries a driveway, a tower may not |

## Consequences

- **Occupancy is conserved state, and leaks are permanent.** A Traveller that disappears without releasing its space silently destroys capacity forever. This is an [`0006`](0006-no-collection-grows-with-elapsed-time.md)-class defect and needs ~~an explicit invariant plus a headless test asserting that total occupied spaces equals total parked vehicles at every Tick~~ **two invariants, neither of them per-Tick — [`0084`](0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md).**

  > **⚠ AMENDED 2026-08-12 by session H.** The defect is real and the `0006` classification is right; what changes is the count and the tier. **A release is checked at its write site**, `O(1)` — *this Traveller holds this Bin, exactly once* — which catches a double release or a release of a Bin never acquired. **The conservation sum is end-of-run**, on the precedent of `SegmentVolumeIsConserved` (37) and `BinCapacityMatchesItsDeclaration` (29), both of which were written as every-Tick obligations and both of which landed whole-world: `02 §10` sorts tiers **by frequency, never by importance**, and *what holds every Tick is conservation **structurally**, from increment and decrement being paired — the sum is the check that the pairing was not broken.* This ADR was written before either precedent existed.
  >
  > **Neither can be written early, and the reason is not scheduling.** Milestone 5b shipped a *vacuously satisfied* invariant on purpose, which is the obvious precedent to copy — but it was writable because the **volume column existed**, so both sides were defined and merely zero. Parking has no Bin, no occupancy column and no parked-Traveller state anywhere in `src/`, so both sides would be **undefined**. ***A vacuously-satisfied invariant needs its state to exist. Zero is a value; undefined is not.***
  >
  > **The obligation is at four documents and zero builds** — here, `02 §10`, `05 §60` and `06`'s milestone 7 risk — which is `HouseholdHomeExists`'s shape at a larger count. **The fix is not a fifth statement**; it is a mechanical check comparing `02 §10`'s named list against the `Invariant` enum, routed to [`plans/0012`](../../plans/0012-corpus-audit.md).
- ~~**A Trip must remember where it parked** in order to walk back to the car.~~ **A `Citizen` remembers where it parked.** Small state, but it must exist, and it must survive save/load.

  > ⚠ **AMENDED IN PLACE 2026-08-18 by [`0119`](0119-a-parking-space-is-held-by-the-citizen-and-a-household-holds-as-many-cars-as-it-has-drivers.md).** Only the **subject** changes; *small state, must exist, must survive save/load* is untouched and is why the column is saved rather than derived.
  >
  > **A Trip is freed when the journey ends**, and [`0101`](0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md) made a commute **two journeys** — so on this ADR's own canonical case, *"a household's car sits at home overnight"* three bullets down, the car is parked while no Trip exists. [`0084`](0084-parking-occupancy-is-two-checks-and-an-invariant-over-absent-state-cannot-be-written.md) reached for the **Traveller** and hit the same wall. ***The return walk is a within-journey requirement and it selected a within-journey object; the overnight case is what says which object is actually the subject.***
  >
  > **Not the Household either**, which is the obvious repair: `World.ModeOf` drives *every* member of a car-owning Household, so a Household of three workers parks three cars and one column would overwrite two of them — the [`0006`](0006-no-collection-grows-with-elapsed-time.md)-class leak this ADR's own risk is about.
- **Residential parking is mostly static occupancy** — a household's car sits at home overnight. Residential sheds will look very different from commercial ones, which is realistic and probably good, but it means the two must be balanced separately.
- **Car ownership becomes a question worth asking.** Parking demand is determined by how many Households own cars, and if ownership were itself a choice influenced by walkability, parking pressure would feed back into it. Not decided here; noted as the natural next layer.
- **A new balance surface.** Shed radius, capacity per building type, and the walk-time cost that flows into the Commute Budget all need tuning, and they interact with mode choice once transit exists.

## What would trigger revisiting

- ~~**Shed queries showing up in profiles.** The mitigation is a smaller shed radius or cached per-Building shed membership invalidated by the Road Graph Epoch — not reintroducing search.~~

  > **⚠ DISCHARGED IN ADVANCE 2026-08-12 by [`0083`](0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md), and the caching is a data-layout item rather than a contingency** — which this ADR's own superseding note above already asked for, and which `05 §3` had taken. S2 R5.6 measured the invalidation **before any profile existed**: the rung is **per-Segment witnessed by the walk paths to the Bins the shed kept**, at **0.10% of a Tick** against a single counter's **1,638.20%**.
  >
  > **The phrase *"invalidated by the Road Graph Epoch"* says when you pay, not what survives**, and the two must not be read as one. A single counter carries no location, so **every edit anywhere invalidates all 159,825 sheds** — and because this ADR pays the query *on arrival*, laziness converts one 255.560 ms stall into a **stampede across arriving vehicles**.
  >
  > **A shed's *use* is the arrival query and there is no second occasion**, so it inherits [`0012`](0012-routing-intent-lives-in-the-agent.md)'s invalidation *shape* and **not** its parameter: **no `T`, no rotation, no proximity wake**. A stale shed returns a Bin that exists and has capacity and is merely not the nearest — an error bounded by the shed radius and already priced by the Commute Budget, where a stale **route**'s error is unbounded. *The two consumers differ in the **magnitude** of the addition error, not in the geometry of the witness.*
  >
  > **The radius itself is still unset and now has a named ratifier**, which it never had under [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md): milestone 7's first run reporting the **walk-Leg length distribution as shed occupancy approaches 1**. **Not reintroducing search** stands, untouched.
- **Playtesting showing parking dominates attention.** If players spend more time on parking than on zoning, the balance is wrong, and the fix is generosity in the Ruleset rather than deleting the system.
