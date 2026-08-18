# Side of street is a property of the Access Point, not of the graph

**Which side of a Segment a place sits on is one saved bit on that place, and a crossing is a cost term — not a second footway edge.** A location on the Road Graph is therefore a triple, `(Segment, offset, side)`, and it has a name: an **Address**. A Building's pedestrian Access Point is a Building's Address; a Leg runs from one Address to another; ~~a parking Bin will have one~~ **a `Car Park` has one** ([`0113`](0113-a-car-park-is-not-a-bin-and-supply-is-at-buildings-until-a-segment-needs-one.md) — the prediction held and only the type's name changed). The graph is untouched — no Segment, no Arc and no Epoch changes because side of street exists.

Guiding concepts: `EMERGENCE`, `LEGIBLE CAUSE`, `SOLVE THE ACTUAL PROBLEM`, `HONEST DEGRADATION`.

This is an **arguable** claim under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md). No measurement distinguishes the storage arrangements: they answer *how far is the shop* identically for every pair not on the same Segment, and differ only in which cases remain expressible and what the graph costs. That is a question for a sitting, and session **F** took it.

## Why

**The question arrived as an objection to giving something up, and the objection was right.** [`0008`](0008-walking-is-a-simulated-leg.md) asked for *sidewalk edges alongside street edges*; [`0072`](0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md) refuses that, so the pedestrian network is a **subgraph** of one graph. Session F's first draft of the amendment concluded that side of street was therefore surrendered. It is not, and the two questions had been collapsed: **whether side of street is *modelled* is independent of whether it is *in the graph*.**

**Four rungs exist and only the fourth is refused.**

| | What it is | Standing |
|---|---|---|
| 1 | Centreline only; no side, and crossing is free | what the corpus had **by omission**, never by decision |
| 2 | **Side on the Access Point; a crossing is a cost term** | **this ADR** |
| 3 | A *footway-side* mask on the Segment — which sides have pavement at all | available, unbuilt, and still adds no Segments |
| 4 | Two footway edges per Street plus crossing edges at junctions | **refused** — `CONTEXT.md` → Road Graph, *"not two parallel networks"* |

Rung 4 is what costs something real: it roughly triples a graph whose working figure is ~30,000 Segments at 1M Citizens, and it loses the three things one graph buys — one Epoch, one revalidation path, and a multi-Leg Trip routed by a single mode-aware search. It would also reopen a question `plans/0002` has already closed, *is the ~30,000-Segment figure inflated by `adr/0008`'s pedestrian layer?* — answered **no**, on the subgraph reading.

**Rung 2 costs one bit and keeps the information.** A Segment's forward direction is already fixed — A→B by the endpoint columns, which is the fact [`0072`](0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md) rests on — so *left* and *right* of that direction are well-defined, deterministic, and require no geometry. The simulation never sees a spline; it does not need to, because side is an enumeration and not a coordinate.

**The cost term is scoped to one Segment, and that is a property rather than a limitation.** Side is defined relative to a Segment's own forward direction, so *the same side* stops meaning anything once a route turns a corner. The crossing term therefore applies exactly when an Address pair shares a Segment and differs in side, and is silent everywhere else. That is `HONEST DEGRADATION`: **exact where it can be computed and absent where it cannot**, rather than smeared across every route as a factor nobody can point at. And it is the case that motivated the term — the corner shop opposite is not the corner shop next door, which is the walkability `adr/0008` says should *emerge* rather than be scored.

**The case rung 2 cannot express is already modelled by a different mechanism.** Rung 2 assumes a Street may be crossed wherever you like. The road you may *not* cross at will is an **Arterial**, whose Arcs carry no foot bit at all and whose crossings are authored Junction pieces — which is **Severance**, and it is emergent because the mask never granted a route rather than because a penalty was applied. A Street is by definition grid-snapped and ordinary; making one uncrossable would be modelling an Arterial badly.

**The corpus had already asked for this without noticing.** [`0009`](0009-parking-is-modelled-supply-never-search.md)'s deferred table lists *"allow or ban street parking per Road Segment **side**"*. Side of a Segment was a latent concept with nowhere to live, and parking was always going to be the milestone that asked for it.

**And naming the triple is what makes milestone 7 cheap.** `CONTEXT.md` → Access Point already fixes the query shape — *"a routing query is `(Segment, offset) → (Segment, offset)` rather than node-to-node, **which is the query shape everything downstream must be measured on**"* — but the *value* in that shape had no name, so every consumer would have spelled its own. A Leg's endpoint is frequently not a Building's: the middle of `walk → drive → walk` is wherever the car is. With one named type, replacing a placeholder parking location with a real Parking Shed result is one endpoint swap.

## Consequences

**`CONTEXT.md` gains an `Address` entry, and `Access Point` becomes a role it plays.** An Address is a location on the Road Graph; an Access Point is *a Building's* Address. The word is chosen because a real street address **is** a distance along a street plus an odd/even side, which is this triple exactly. *Noted against it, and accepted: an `Address` type in `Borough.Core` can be misread as a memory address by a reader arriving from the systems side. The domain register wins, per `CLAUDE.md`.*

**`CONTEXT.md` also gains a `Node` entry, because an Address is defined against one.** *"Never a node"* is half of what an Address is, and `Node` was load-bearing in five places — the route cache key, the Rejoin Target, the Sight Horizon's floor, `adr/0040`'s pathfinding cluster, and the definitions of Segment and Access Point — while having **no vocabulary entry at all**. That is `plans/0012` *Cause 1* with the copy count at zero, and this ADR is where it was cheapest to fix.

**A Building carries two Addresses, pedestrian and vehicle, and they are equal by construction today.** Two `(saved AND hashed)` columns rather than a table: nothing in the design wants *N* doors, a corner Lot with two frontages still has one front door, and at ~150,000 Buildings per 1M Citizens two Addresses is single-digit megabytes. They diverge when the Lot subdivider gives a Lot real frontage (5a-bis), when parking acquires a location (milestone 7), and when freight needs a loading kerb (`03 §6.6`). **The vehicle one has no consumer in milestone 5b** — `CONTEXT.md` → Parking Shed keys the shed on the *pedestrian* Access Point — and it ships anyway, because `adr/0008`'s third consequence exists precisely so that parking does not restructure the Building table later.

**The crossing cost is Ruleset data, hot-reloadable and hash-bearing.** It changes a walk Leg's cost, therefore the Commute Budget, therefore a Trip Fate, therefore the city. Under [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) it owes a `plans/0002` §D row with a named ratifier on the day it is written, and **this ADR sets no value.** It is one of three such numbers milestone 5b will need — with walking speed and the Commute Budget — named in advance rather than discovered, which is the correction to `adr/0069` shipping three that its own ADR predicted none of.

**Rung 3 is available and is not built.** A footway-side mask on the Segment would let a Street have a pavement on one side only. It adds no Segments and no Arcs, so it remains a data question rather than a schema one — the same shape `adr/0072` left a one-way street in. Nothing generates such a Street and no command can draw one.

**Side never reaches the vehicle graph.** Lanes are directional queues on an Arc; a Vehicle's side is its direction, which the Arc already carries. This decision is about where a *place* sits, and only walking reads it.

## What would trigger revisiting

**A Street that cannot be crossed at will.** The whole of rung 2's approximation is that crossing a Street costs a constant wherever you stand. A Street with a central reservation, a fence, or a tram alignment breaks it — and the honest first response is to ask whether that road should have been an **Arterial**, since the design already has a mechanism for the uncrossable road. If ordinary Streets genuinely need per-location crossing opportunities, rung 3 comes first and rung 4 last.

**A measurement showing the same-Segment scope is too narrow.** The term is silent for every Address pair that does not share a Segment, on the argument that crossings along a longer route wash out. If milestone 5b's walk Leg distribution shows short cross-corner trips dominating — where the wash-out argument is weakest — the scope is the thing to re-examine, not the graph.

**A per-direction column arriving on the Arc that is not a mask.** `adr/0072` already names this: if an Arc acquires its own capacity, speed and volume, the Arc becomes the row and the Segment a grouping. Side of street would then be worth re-deriving from scratch against that shape rather than inherited from this one.

**Rendering needing more than a bit.** A renderer must place a walker on a pavement and a door on a façade, which is geometry the simulation does not hold. If that turns out to require the simulation to carry a lateral offset rather than a side, this decision is the wrong shape — but note that `03 §3.8` makes rendering independent by construction, so the burden is on the renderer to derive it.
