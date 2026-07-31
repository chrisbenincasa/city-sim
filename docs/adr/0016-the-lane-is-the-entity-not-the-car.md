# The Lane is the entity, not the car

**A Lane owns its Vehicles as a sorted one-dimensional queue and updates all of them in a single pass. Vehicles are not independently scheduled and hold no references to each other.** Two-dimensional behaviour emerges from Overlaps between Lanes; lane changing is a first-class Switch Lane object rather than a special case. Car following uses the Intelligent Driver Model (Treiber, Hennecke & Helbing 2000).

**This model runs only on Microscopic Segments.** Statistical Segments have no Lane queues at all — see [`0007`](0007-stress-driven-simulation-detail.md).

## Why

Citybound's traffic design was its strongest engineering work, and Anselm Eickhoff's formulation is the decision in one sentence:

> "Cars themselves are not actors, one lane is the atomic actor, it updates all the cars on it in one go."

The instinct is to make the car the entity, because a car is what the player sees. That instinct costs you everything:

| Car as entity | Lane as entity |
|---|---|
| Each car queries "who is in front of me?" | Predecessor is the previous element of the queue |
| Needs a spatial index — quadtree, grid, or sort per Tick | **No spatial index at all** |
| Random access across memory per car | Linear scan of a contiguous array, perfect cache locality |
| Scheduling granularity is one car | Scheduling granularity is one Lane |

Car following down a sorted queue is O(n) in the number of Vehicles with the constant of a `memcpy`. Eickhoff reached roughly 400,000 individually simulated cars **on a single core** with this structure, which is the number that makes microscopic traffic affordable at all.

**Overlaps are how one dimension buys two.** Lanes that physically interact — parallel, opposing, or crossing — declare an Overlap and exchange their Vehicles' projected positions once per Tick, mapped into each other's coordinate space as ordinary obstacles. A crossing conflict, an opposing-traffic gap, and a parallel neighbour are then all the same thing to the solver: something in front of you at a known distance. There is no separate junction algorithm to write, debug, or tune.

**The Switch Lane is the idea that pays for itself immediately.** An invisible Lane spans the Overlap between two parallel Lanes, and — the load-bearing part — **the two normal Lanes are not connected to each other, only each to the Switch Lane.** Merging, normally the nastiest special case in traffic simulation, becomes an ordinary Lane obeying ordinary rules: entering it is an ordinary connection, finding a gap is ordinary car following, and a merge the driver aborts because required braking exceeds comfort is an ordinary failure to enter, not an exception path. The hardest part of traffic simulation collapses into something cheap and debuggable. `SOLVE THE ACTUAL PROBLEM`

## The constraint that makes it affordable

The Lane model is expensive per Lane and we run it on very few Lanes. [`0007`](0007-stress-driven-simulation-detail.md) settled that a Segment is Microscopic when it is under stress and Statistical when it is not, and that boundary is where this ADR stops:

- **Microscopic Segment** — real Lanes, real queues, IDM, Overlaps, Switch Lanes. Travel time is emergent.
- **Statistical Segment** — **no Lanes exist.** A Traveller is an origin, a destination, and an arrival Tick. Travel time is `distance / speed`, which on a free-flowing road is not an approximation but the exact answer.

The consequence to state plainly: **the number of Lanes running the full model is bounded by network stress, not by population.** A million Citizens do not produce a million queue slots, because only so many Segments can be stressed simultaneously. This is the entire reason a 1M-Citizen metropolis is affordable with an honest population count, and it is why "just run the Lane model everywhere" — the option Citybound took — is not available to us.

Promotion therefore has to **materialise** Lane queues from in-flight Trips, and demotion has to convert them back to arrival Ticks. Invariant 3 of `docs/03-agent-architecture.md` applies with full force: what is discarded on demotion must be enumerated. Queue position, headway, and any in-progress Switch Lane traversal are exactly the state at risk.

## Consequences

- **A Vehicle is addressed as (Lane, index), not as a global handle.** Anything holding a bare Vehicle reference across a Tick is a defect, because the queue is compacted as Vehicles leave.
- **Overlaps are declared, not discovered.** They are computed from geometry when the Road Graph is edited and revalidated against the Epoch, never searched for per Tick.
- **The IDM needs tuning parameters in the Ruleset**, not in code — desired headway, comfortable braking, acceleration exponent. `FAST ITERATION`
- **Lane-level tools remain an anti-goal.** Signal phasing and turn restrictions being *simulable* does not make them player-facing; see [`0009`](0009-parking-is-modelled-supply-never-search.md) for where that line sits.
- **Determinism is nearly free here.** Iteration order is queue order, and Overlap exchange happens once per Tick against the Past. There is no per-car scheduling order to get wrong.

## What would trigger revisiting

- **Discretionary lane changing.** Citybound's lane changing is weaker than its reputation: it is triggered purely by proximity to the end of a switchable stretch, with no incentive and no politeness criterion, so there is **no discretionary lane changing at all** — a driver never moves left because the left lane is faster. **MOBIL** (Kesting, Treiber & Helbing) is the missing half, and it composes with the Switch Lane rather than replacing it: MOBIL decides *whether* to change, the Switch Lane executes it. Deferred until lanes need to be chosen for reasons other than the next turn.
- **Promotion cost dominating the traffic budget.** If materialising queues costs more than running them, the answer is wider hysteresis in [`0007`](0007-stress-driven-simulation-detail.md), not a third representation.
- **A junction type Overlaps cannot express.** Roundabouts and weaving sections are the candidates. Authored Junction pieces are the intended escape hatch and should be tried before the model is generalised.
