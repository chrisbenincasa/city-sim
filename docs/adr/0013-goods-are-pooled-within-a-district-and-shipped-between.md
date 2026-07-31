# Goods are pooled within a District and physically shipped between them

**Within a District, Goods move through an abstract Pool: instant, subject to connectivity, with no Vehicle simulated and no pathfinding query issued. Between Districts, and to and from Outside Connections, a Shipment is a real Vehicle carrying real cargo on the Road Graph, contributing real congestion.** The boundary between the two regimes is per-Good and must stay swappable.

## Why

Anno is the precedent and it is worth being precise about what it actually does. Goods produced anywhere on an island are available anywhere on that island — there is no intra-island haulage at all — and the only logistics the engine simulates is the trade route between islands, carried by ships the player schedules. This is not a shortcut Anno tolerates; it is the design. Inter-island shipping is the part the player is meant to optimise, so it is the part that gets a simulated vehicle. Everything the player has no verb for gets a Bin.

The general principle is that **expensive simulation is reserved for decisions the player actually makes**. `SOLVE THE ACTUAL PROBLEM` The mechanism worth modelling here is not "a box gets from the bakery to the shop"; it is "this District cannot feed itself and the trucks that fix that are clogging your one arterial". The second is a decision surface. The first is a rounding error the player will never act on, and simulating it costs a routing query per unit moved.

GlassBox is the counter-example, and it is the sharper of the two because it was attempting almost exactly this fusion. Every resource in SimCity 2013 was agent-carried — trucks physically moved freight, and each carried unit therefore became a pathfinding query against a shared gradient field. That volume is a direct contributor to the engine's 2km × 2km map cap, and it rode on the same shared-gradient routing model that produced its memoryless sims. GlassBox's production model is excellent and we adopt it nearly verbatim; its decision to embody every unit of that production is the one we refuse.

**The transport layer must be swappable per Good.** The pooled/shipped boundary is a guess about where cost lives, and unverified assumptions about where cost lives are exactly what this project has committed to avoiding. A Good should declare its transport mode in the Ruleset so that, given profiling data, a single high-value Good can be promoted to intra-District shipping — or the whole freight layer demoted to pooled inter-District flow — without touching the Rule engine.

**And the taxonomy stays small: three to eight Goods.** Eickhoff's shipped resource enum was around ten entries with further entries deliberately commented out, and his own conclusion was that absolute resource amounts make balancing very hard, amplify bugs, and destabilise the system. Every added Good multiplies four things at once: chain depth, Pools per District, Shipment types on the road, and diagnostic surface in the UI. Resist Good #20. The pressure to add one is almost always a request for a chain the existing Goods could already express.

## Rejected

**Ship everything, including within a District.** Causally honest and what Cities: Skylines does with cargo trucks. Rejected because it spends the entire freight budget on movements the player cannot influence — you do not zone the route from a warehouse to the shop two blocks away — and because it is GlassBox's failure with a different renderer.

**Pool everything, city-wide.** Cheapest of all, and it deletes freight congestion entirely. Rejected because it deletes geography with it: if Goods teleport across the map, industrial siting stops mattering, the Outside Connection stops being a place, and one of the three pressure sources has no physical expression.

## Consequences

- **District boundaries become simulation-visible, and therefore must be a real, legible concept — not a UI grouping.** Where a boundary falls decides whether a movement is free or is a truck. The player must be able to see boundaries, understand that pooling is what they mean, and be told when a Shipment exists because of one.
- **"Abstract Pool" must not mean "connectivity ignored".** A District whose internal Street network is broken must still fail. Pool membership is a connectivity test: a Building severed from the District's road component is not in the Pool and starves, exactly as if the truck could not reach it. Instant is not the same as unconditional, and the test is the whole reason this is a simulation rather than an accounting convenience.
- **Freight Vehicles enter Stress accounting under [`0007`](0007-stress-driven-simulation-detail.md).** Whether a truck weights a Segment the same as a commuter car, or more, is undecided and already logged in `docs/03-agent-architecture.md` §6. It is a balance question, not an architectural one, but it must be answered before freight volume is tuned.
- **Two transport regimes mean the failure vocabulary must cover both.** A Shipment gets a Trip Fate like any other journey; a Pool starvation gets a named cause naming the Building and the missing Good. Neither may be silently swallowed.
- **The Pool is just a Bin per Good per District**, so Rules, fallback chaining, and the Outside Connection's import path all work on it unmodified. Nothing new is invented for the pooled case, which is most of why it is cheap.

## What would trigger revisiting

- **Freight query volume showing up in profiles.** The first lever is per-Good demotion to pooled inter-District flow for low-value Goods, not deleting Shipments.
- **Players gaming the boundary** — drawing one enormous District to abstract away all freight. If that becomes the dominant strategy, Districts need a size ceiling or a cost, because a boundary the player draws to switch off a subsystem is a boundary that is not a gameplay concept.
- **Profiling showing intra-District movement is affordable after all** at target city size. Then the honest move is to promote Goods individually and measure, which the per-Good switch exists to make possible.
