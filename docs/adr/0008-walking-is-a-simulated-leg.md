# Walking is a simulated Leg with real cost

**Every Trip is a sequence of Legs, and walking is one of them — simulated, costed, and capable of failing. Buildings connect to a pedestrian network, not directly to the road graph.** A car commute is therefore never one Leg; it is at minimum `walk → drive → walk`. This holds from the first line of code, with no transit implemented and possibly none ever.

## Why

The received framing of this decision is "do we want public transit?" That framing is a trap, and it is the one Citybound fell into. Eickhoff built cars first and planned to add pedestrians and transit later; cars-only silently shaped the lane model, the routing model, the Trip model, and the zoning model, and pedestrians were still unbuilt when the project stopped. The recurring community critique — *"if your only mode of transport is cars you can hardly call what you're building a city"* — was never answered.

**The thing that cannot be retrofitted is not transit. It is walking.**

In a car-only Trip model, a Building *is* a node on the road graph and a Trip is a single Leg. That one assumption propagates much further than it appears to: a Lot's value becomes a pure function of road access, density carries no access cost, and Trip cost is entirely drive time. Undoing it later means rewriting the Trip model, the cost function, the Lot valuation, and every piece of balance tuning built on top of them.

Whereas the moment walking is real, a mundane commute is already multi-modal — mode transitions, transfer points, and a cost function that must trade walking minutes against driving minutes — with zero transit code. Adding a bus later becomes *inserting a Leg type into machinery that already handles Legs*. That is incremental. The jump from one-Leg to many-Leg is not, which is precisely why it never happened in Citybound.

Two things make this unusually cheap for us in particular:

- **Sidewalks essentially never stress.** Under [`0007`](0007-stress-driven-simulation-detail.md), fidelity follows congestion, and pedestrian ways are the least congested thing in the city. Walk Legs will be Statistical approximately always — a `distance / speed` lookup. They almost never enter the expensive regime.
- **It makes walkability emerge rather than be scored.** Mixed-use, density, and street life are otherwise formula terms bolted onto Lot desirability. Here a corner shop is viable because people can physically reach it on foot, which is `EMERGENCE` and `LEGIBLE CAUSE` doing real work rather than being asserted.

And it creates a failure mode worth having. **Severance** — a neighbourhood cut off by an arterial with no crossing — becomes something the player can accidentally build and then diagnose, rather than something the game has no vocabulary for. The inability to walk is as interesting as the ability.

## Rejected

**Buildings on the road graph, pedestrians as rendering decoration.** Cheaper today, and it is what SimCity does. Rejected because it is the specific irreversible decision this ADR exists to avoid, and because decorative pedestrians violate the standing rule that every visible agent is a promise you have to keep.

**Transit first, walking later.** Incoherent in the other direction — transit *is* walking plus a vehicle Leg. There is no version of a bus trip that does not begin and end on foot.

## Consequences

- **Trip object count roughly triples.** Every car commute is 3+ Legs instead of 1. Legs are small and mostly Statistical, but the Trip table must be sized for this rather than for a Leg-per-Trip assumption.
- **The road network needs a pedestrian layer**: sidewalk edges alongside street edges, and crossing edges at junctions. This is real work, not a free consequence. Arterials deliberately have no sidewalk and no crossings except at authored junction pieces — which is the mechanism that makes severance emerge rather than being a scripted penalty.
- **Buildings need a pedestrian access point distinct from a vehicle access point.** These are usually the same place and occasionally are not, and the distinction is what lets parking later become a real location without restructuring anything.
- **The Commute Budget now spans modes.** Walking minutes and driving minutes are the same currency and both count against it. This is a direct application of the standing rule that path cost must be the quantity the player is scored on.
- **Parking becomes an open question rather than a non-question.** The walk Leg has to start and end *somewhere*. Deferring a real parking model is defensible; pretending the question does not exist is not.
- **Unwalkable geography is now expressible**, which means it is now possible to build a city that fails in a way earlier drafts could not represent. That is the point, but it needs to be legible: a Trip Fate of *no route found* for a destination 50m away must be diagnosable, not mysterious.

## What would trigger revisiting

Only one thing plausibly does: if pedestrian Leg volume turns out to dominate simulation cost at metropolis scale despite being Statistical. The mitigation to reach for first is coarser sidewalk topology — one pedestrian edge per block face rather than per street segment — not deleting the Leg. Removing walking after the fact reintroduces exactly the irreversibility this decision was taken to avoid.
