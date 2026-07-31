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

**Parking is a real, spatial, player-controllable resource: a Bin held by Buildings and by Road Segments. Cars never search for a space.** Arrival queries the destination's **Parking Shed** — the parking Bins within acceptable walking distance of its pedestrian Access Point — nearest-first, and takes the first with capacity. Scarcity degrades **gracefully**: the shed widens, the walk Leg lengthens, and a Trip fails only when it exceeds its Commute Budget.

## Why

The instinct to abstract parking away came from conflating two separable things. **Parking supply** — spaces existing as placed, spatial, scarce resources — is cheap and interesting. **Parking search** — cars driving around hunting for a space — is the expensive agent behaviour that puts an unbounded query in the hot path of every arrival. Only the second is dangerous, and rejecting the first because of the second was an error.

**The "not a traffic-management game" anti-goal does not apply here.** That anti-goal targets *lane-level* tools: signal phasing, turn restrictions, dedicated turn lanes. Parking is a **land-use** decision. Choosing that a Lot becomes a garage rather than an apartment block is precisely the kind of choice this game is about. The anti-goal was never in tension with this; it simply addressed a different layer.

What modelling supply buys that a District-level parking average cannot:

- **Specific diagnosis instead of averaged diagnosis.** Not "downtown averages eight minutes from your car" but "you parked three blocks away because the two nearer garages were full." That is a fact with named constituents, which is the **Evidence** pattern working as designed rather than a statistic wearing a face.
- **Parking consumes land.** A garage occupies a Lot that could have held housing. This is the actual urban trade-off, and here it is *felt* through the zoning the player does rather than asserted through a desirability term.
- **It cashes in the Access Point split.** [`0008`](0008-walking-is-a-simulated-leg.md) separated pedestrian and vehicle Access Points as cheap optionality. A car's true access point is wherever it managed to park, which is not its destination — that is the whole mechanism.
- **It reuses machinery that already exists.** Parking is a Bin; acquisition and release are Rules; the Settle phase already provides serial, deterministically-ordered mutation of shared resources. Nothing new is invented.

**Graceful degradation was chosen over a hard failure** because it converts a cliff into a gradient. A dedicated *no parking* Trip Fate would mean a district that dies abruptly the moment supply is exceeded. Instead, pressure shows up first as rising walk times — visible, attributable, and correctable — and only becomes Trip failure when the Commute Budget is genuinely blown. This also avoids adding a second failure channel where an existing one already expresses the outcome. `HONEST DEGRADATION`

## New player tools this creates

Parking capacity per building type is Ruleset data, not code. The player's levers:

| Tool | Effect |
|---|---|
| Place a parking structure | A Building that is mostly a large parking Bin, consuming a Lot |
| Allow or ban street parking per Road Segment side | Trades kerb space against capacity |
| Residential parking provision by building type | Data-driven: a detached house carries a driveway, a tower may not |

## Consequences

- **Occupancy is conserved state, and leaks are permanent.** A Traveller that disappears without releasing its space silently destroys capacity forever. This is an [`0006`](0006-no-collection-grows-with-elapsed-time.md)-class defect and needs an explicit invariant plus a headless test asserting that total occupied spaces equals total parked vehicles at every Tick.
- **A Trip must remember where it parked** in order to walk back to the car. Small state, but it must exist, and it must survive save/load.
- **Residential parking is mostly static occupancy** — a household's car sits at home overnight. Residential sheds will look very different from commercial ones, which is realistic and probably good, but it means the two must be balanced separately.
- **Car ownership becomes a question worth asking.** Parking demand is determined by how many Households own cars, and if ownership were itself a choice influenced by walkability, parking pressure would feed back into it. Not decided here; noted as the natural next layer.
- **A new balance surface.** Shed radius, capacity per building type, and the walk-time cost that flows into the Commute Budget all need tuning, and they interact with mode choice once transit exists.

## What would trigger revisiting

- **Shed queries showing up in profiles.** The mitigation is a smaller shed radius or cached per-Building shed membership invalidated by the Road Graph Epoch — not reintroducing search.
- **Playtesting showing parking dominates attention.** If players spend more time on parking than on zoning, the balance is wrong, and the fix is generosity in the Ruleset rather than deleting the system.
