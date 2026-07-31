# Services are delivered by Trips, not by coverage

**A Service reaches people by someone making a journey.** Education, health, and recreation are **Attended** — the Household travels. Fire, police, and waste are **Dispatched** — the Service travels. Only the Utilities are **Networked**, and nobody moves.

The service coverage Map Layer is demoted from **mechanism** to **overlay**, composed from the same reachability the Trips use.

## What was there before

`02 §2.4` specified *"Service coverage | stored, multi-source distance | each service building type | one layer per service type."* That is SimCity 4's model — a radius that decays, sampled by a Building, feeding desirability.

But `CONTEXT` → Household and `03 §2.3` both define a Provider List as *"a short, sticky set of known shops, workplaces, **and services**."* **The word "services" was already there and nothing had ever used it.**

The two are incompatible, and a single case decides it: a school across an uncrossable Arterial. A distance field says it is 200 m away and therefore excellent. A Trip says **there is no route.** `01 §7` requires that an overlay never be sharper than the simulation beneath it — under the field model, service coverage was the one place that rule plainly did not hold.

## The axis is who moves

Not what the Service provides. Sorting the bundles by who makes the journey is what separates their mechanics, and it also exposed that `adr/0026`'s *"public jobs are demand-determined by catchment"* is **not universal** — a water tower and a park have no catchment population setting their headcount.

| Mode | Who moves | Services | Machinery |
|---|---|---|---|
| **Attended** | the Household | education, health, recreation | Provider List + Legs. Existed; unused |
| **Dispatched** | the Service | fire, police | one new Trip purpose — ledger #14c |
| **Networked** | nobody | power, water, sewage | flow over the District graph. See [`0031`](0031-one-resource-abstraction-and-depth-not-count.md) |

Waste left this taxonomy entirely: it needs a Vehicle, so it is a **Good** and its collection is a **Shipment**. See `0031`.

**Recreation needs no new machinery at all.** `CONTEXT` → Amenity is already *"the count of distinct Business types reachable **on foot**"*; widen *Business* to *destination* and a park is an Amenity entry.

## The school run is the payoff

The largest consequence, and the one that justifies the cost:

**`adr/0010`'s Sorting finally has a mechanism.** It has always been an assertion — *"good schools attract already-educated Households"* — with nothing underneath it. If a school is a Provider List entry scored by the same logit that scores a job, school quality enters residential utility **automatically**. And the ugly corollary arrives free: good schools raise land value, which prices out the Families the school was built for. `NO VERDICT`, unwritten.

**Severance gets its sharpest instance.** `CONTEXT` justifies walking with *"a city can be perfectly well connected for cars and broken for people."* An Arterial between a neighbourhood and its primary school is far sharper than a corner shop, because a Family cannot solve it by driving further.

**The cost is real but points the right way.** School Trips are roughly +50% on the commute peak at the 10k target. But `CONTEXT` → Fidelity makes a walk Leg permanently Statistical and pedestrians never contribute to Stress — so a walked school Trip is a time-advance on the Event Wheel and **nothing in any Lane queue**.

> **The expensive part of the school run is exactly the part the player is meant to be designing out.** A city where children walk pays nothing for it, in congestion or in CPU. A city where they must be driven pays in both.

Simulation cost and design intent pointing the same way is rare, and is the strongest evidence the model is right.

## Chained Trips, without a planner

A drop-off is **a waypoint on the parent's commute, not a Trip of its own** — `CONTEXT` → Trip is already *"an ordered sequence of Legs."* Resolved **once when the school is chosen**, never replanned. The child walks if the walk Leg fits their own budget; otherwise the school attaches to a parent's commute. A crossover, not a threshold.

This is the line Citybound failed to draw, and it is the load-bearing constraint on the whole model:

> **A Household chooses providers and modes. It never chooses an itinerary.**

`03 §2.3` records what happens otherwise: evaluating hypothetical activity sequences is an NP-hard orienteering problem costing seconds of CPU per agent. The activity set and order are fixed; only *which* provider and *how* you get there vary, and both are ordinary discrete choices the logit already makes.

**Mode is an attribute of a Provider List entry**, not a per-Trip decision — *how I get to work* is decided when the job is taken. **Re-evaluation is an Event Wheel countdown, or immediate on a failed Trip**, so a network edit never forces a global invalidation, and ridership on a new line **ramps rather than snaps**.

## An unused Service still counts, and it counts as Taste

Presence contributes to desirability — but as an [`0027`](0027-preference-is-drawn-per-household-and-persists-for-life.md) **Taste axis**, never a flat bonus. That was the objection: a fire station that never sees a fire does nothing mechanically and is nonetheless always worth building, which is a lever with one correct setting.

As a Taste axis it stops being one:

> **Under-provide safety and you do not get a worse city. You get a different population.**

Life Stage supplies the base and the range, and Families weight safety hardest — so an under-protected city quietly loses its **internal generation**, and reads it in Replacement Rate and Retention rather than in a satisfaction bar. The sixth instance of *every specialisation starves an input only a different specialisation produces*.

## Consequences

- `02 §2.4`'s service coverage row is now an **overlay** specification, not a mechanism.
- **Health belongs with schools, not with fire.** A clinic is visited routinely; it is Attended. Only fire and police are genuinely insurance, which makes that category two services out of nine rather than the half the group looked like.
- The Provider List's re-evaluation rule now covers **every** provider, closing a question that had been open since `03 §2.3`.
- `01 §5.3`'s claim that containment is *"an ordinary Trip that can fail"* now has the machinery it presumed. Ledger #14c is most of the way closed.
