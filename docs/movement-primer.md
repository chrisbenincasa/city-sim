# Movement — a primer

> **This document owns nothing.** It is an orientation aid: the movement and routing model rebuilt
> from first principles, in the order it was discovered rather than the order it is filed in, so that
> somebody who has been away from it can page it back in.
>
> **Everything here is owned elsewhere and cited here.** `03-agent-architecture.md` owns the model,
> `CONTEXT.md` owns the vocabulary, `docs/adr/` owns the decisions, `plans/0013` owns what a Tick
> costs, `docs/spike-results.md` owns the measurements. **When this file disagrees with any of them,
> they are right and this one is stale.**
>
> **It deliberately stores no status and almost no numbers**, which is the property that keeps a
> fourth copy from becoming a fourth thing to maintain — `plans/0012` *Cause 1*: every document that
> stored status drifted, and the one large document that stores none came back clean. Where a figure
> appears it is there because the *shape* of the argument needs it, and it names its owner.

---

## The whole thing in one sentence

**A million people need to get to work on a road network, a Tick is 15.6 ms at 4×, and a path search
per journey costs more than the entire budget — so nobody searches. A driver remembers a route,
glances at what is immediately in front of it, and is stubborn about changing its mind.**

Every term in the model is one of those three behaviours, or a consequence of not being able to afford
the alternative.

---

## Building it up

### 1. The road network is a graph

Junctions are nodes and **Segments** are the edges between them. A **Trip** is one journey with a
purpose — an origin, a destination, an ordered sequence of **Legs**, and a **Trip Fate** when it ends.

A car commute is never fewer than three Legs — `walk → drive → walk` — because Buildings connect to
the **pedestrian** network rather than to the road graph ([`adr/0008`](adr/0008-walking-is-a-simulated-leg.md)).
That is not a flourish: it is the one decision in the movement model that cannot be retrofitted, and
it is why a corner shop is viable because people can physically reach it rather than because a
desirability formula says so.

### 2. The obvious design is unaffordable, and everything else descends from this

If every Trip computes a shortest path when it starts, routing fits below **~85 Trip starts a Tick**
(S2 R3), and a city of a million people starts far more than that. **So route-finding cannot happen
per journey.** Read the rest of this document as consequences of that sentence.

### 3. So a driver remembers instead — the Habit Route

The route a **Citizen** normally takes between two places, worked out once from a slow-moving cost
basis and reused across many Trips. Starting a Trip becomes a **lookup** rather than a search.

This is not a performance hack bolted onto a routing model. It is
[`adr/0017`](adr/0017-agents-satisfice-they-never-optimise.md) — the rule the rest of the game already
uses for shops, jobs and suppliers: *keep a short, sticky list of the places you already know, and
switch only when a known alternative is substantially better.* Roads were the last actor class nobody
had applied it to.

It belongs to the **Citizen**, never to the **Traveller**. A Traveller is the temporary embodiment
that exists only while somebody is actually travelling and is released on arrival, and anything reused
across many Trips has to outlive that.

### 4. A remembered route can be wrong in two entirely different ways

Keeping them apart is most of the design.

| | *The road got busy* | *The network changed* |
|---|---|---|
| Example | rush hour on your usual road | the player bulldozed it, or built a bypass |
| Is the Habit wrong? | **No — it is deliberately out of date about this** | **Yes — it is wrong about what exists** |
| Repaired by | **Sight**, below | recomputing the route |

**Nothing recomputes a Habit Route because a road got busy.** That is
[`adr/0046`](adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)'s
*static Habit*, and it is ratified over exactly that case and no other.

What does get recomputed is a route that is wrong about the **map**, and
[`adr/0012`](adr/0012-routing-intent-lives-in-the-agent.md) states the promise precisely:

- **Never wrong about a road that was removed.** The test is exact — *does this route contain that
  Segment* — and it runs at the instant of the edit. No driver ever drives through a bulldozed road.
- **Boundedly wrong about a road that was added**, for at most `T` Ticks of that Citizen's own
  travelling, and never longer. `T` is the answer to *how long after you build a bypass does the city
  start using it*.

The asymmetry is forced rather than chosen: for a removal an exact test exists; for an addition it
cannot, because the new Segment is on no existing route, so containment has nothing to match.

### 5. Sight — what you can see from where you are standing

At each junction the driver reads the **live** cost of the next Segment or few along its route, and
the alternatives leaving *this* junction. That is the whole of its live knowledge. No global cost
field, no view of the city, no satnav — a city where every driver has a satnav is a specific, modern,
unusual city, and the design would be choosing it by omission.

The **Sight Horizon** is how far that reading reaches. Its **floor** is derivable from the Road Graph
rather than tunable: looking fewer Segments ahead than the distance to the next junction with a real
choice gives a driver a signal it is structurally unable to act on.

### 6. Temperament — how bad it has to get before you bother

A per-Citizen threshold: a **stable base**, which is character and persists for life, plus
**per-decision jitter**, which is what kind of morning this is.

This is load-bearing, not flavour. Without it every driver at the same junction reads the same numbers
and makes the same decision — the whole flow switches to the side street, the side street jams, the
whole flow switches back, and the city oscillates for ever at the period of that loop. Temperament is
the only thing in the model that breaks the tie differently for different people, and it is the case
where `UNIQUE INDIVIDUALS` is what makes the city **work** rather than what makes it interesting.

**Habit, Sight and Temperament are the entire routing model.** Remember a route; glance one junction
ahead; be stubborn.

### 7. Which produces the problem the model is currently working on

Sight makes diverting **routine** rather than exceptional — it is an every-junction possibility for
the whole fleet. And the moment a driver takes the side street it is *off its remembered route and has
no route at all*. If it works out a new one, that is a search, and a search at the rate a congested
city diverts is **the largest number in the corpus** (S2 R6.3, and it must be quoted with its rung).

So it does not get a new route. **It goes back to the old one.**

```
        habit route:  A ──► B ──► C ──► D ──► work
                       \                ▲
      jam on A→B, so    \               │  rejoin: at each junction, take the
      Sight diverts:     ▼              │  arc that gets you closer to B.
                         X ──► Y ───────┘  No search. The same rule as Sight,
                                           pointed at a different target.
             rejoin target = B, the node it declined to enter
```

A **Diversion** is leaving the Habit Route at a junction. A **Rejoin** is getting back onto it by the
same local rule, never by a search. Going round a block takes about three junctions, which is where
the measured rejoin success rate jumps (S2 R6.4.2).

### 8. The other axis entirely — fidelity

This is about **simulating cars**, not about choosing routes, and the two are independent.

You cannot run vehicle physics on every road in a million-person city. So **a Segment is Microscopic
when it is under stress and Statistical when it is not**
([`adr/0007`](adr/0007-stress-driven-simulation-detail.md)):

- **Statistical** — there are no vehicles at all. A Traveller is an origin, a destination and an
  arrival Tick, and travel time is `distance / speed`, which on a free-flowing road is not an
  approximation but the exact answer.
- **Microscopic** — real vehicles in a **Lane** queue, following each other with a real car-following
  model ([`adr/0016`](adr/0016-the-lane-is-the-entity-not-the-car.md)). Travel time is emergent.

The crucial part: **fidelity is a property of place, never of person, and never of the camera.** Detail
arrives where congestion is, which is the only place detail could tell you anything, and looking at a
jam cannot change it. A **walk** Leg is always Statistical, because pedestrian networks do not saturate
at this scale.

### 9. Stress is what promotes a Segment

`volume ÷ capacity`, times a static junction-complexity factor, with hysteresis so Segments do not
flicker. **Volume is counted by the Traveller** — you increment the Segments you actually drive on
([`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)). That sounds
obvious, and it replaced a scheme that counted District-to-District flows and distributed them over
cached routes, which turned out to report jams **in the wrong place** rather than merely late.

There is a second, event-driven trigger: a Segment whose downstream neighbour is Microscopic and full
is **force-promoted**, so the simulated region keeps pace with a queue rather than trailing it.

### 10. The circularity, and why it is survivable

Route choice depends on travel time; travel time depends on whether a Segment is Microscopic; that
depends on volume; volume depends on route choice.

It holds because **both errors push toward detection**. Underestimate congestion and routing over-uses
the Segment, volume rises, the threshold is crossed, and microscopic simulation finds the truth.
Overestimate it and traffic diverts, raising volume — and therefore detection — somewhere else. The
chain closes only because the Segments a Traveller *uses* are the Segments it raises the volume of,
which under `adr/0041` is a structural property rather than an assumption.

### 11. One deletion worth carrying

Routing used to key on the **District** and does not any more
([`adr/0047`](adr/0047-routing-never-keys-on-the-district.md)). A District is the boundary within
which Goods pool without physical transport; it had no business being the unit of route-finding, and
using it meant **one route per (junction, District) pair in the entire model**, which concentrated the
overwhelming majority of the city's traffic onto a tiny fraction of its road. A route is now
`(Segment, offset) → (Segment, offset)`, served from a cache keyed by node pairs.

---

## One driver, one morning

Ana lives on Elm and works across town.

1. **The Trip starts.** Three Legs: walk to the car, drive, walk from the parking space. She is a
   **Traveller** now; the Citizen record stays put and owns everything that matters — money, job,
   home. A Traveller is a view, not an owner.
2. **Does she search?** No. She reads her **Habit Route**. One check first: is it stale — older than
   `T`, or near a road built recently? If it is, *this* is where a recomputation is paid, at Trip
   start, on a budget line that already exists. Today it is fresh, so the whole thing is a lookup.
3. **She drives.** Each Segment she enters increments that Segment's **volume**; each one she leaves
   decrements it. Most of her route is quiet, so those Segments are **Statistical** — no vehicles
   exist anywhere on them, and she simply has arrival Ticks.
4. **Third junction: something is wrong ahead.** **Sight** reads the live cost of the next Segment
   along her route and of the arcs leaving this junction. The route ahead is bad.
5. **Does she divert?** That depends on her **Temperament** — her base, plus today's jitter. Her
   neighbour, at the same junction on the same Tick reading the same numbers, might not. That is the
   point of the layer.
6. **She diverts.** She is now off-route, carrying a **Rejoin Target**: the junction she declined to
   enter. At each following junction she takes the arc that closes on it, and two or three junctions
   later she is back on her Habit Route. **No search has happened anywhere in this story.**
7. **Meanwhile the road she avoided got worse.** Its `volume ÷ capacity` crossed the threshold, so it
   **promoted to Microscopic**: the queue on it is now real vehicles, and the queue backing into the
   junction upstream force-promotes that one too. A Trip crosses both regimes freely, transitioning at
   Segment boundaries.
8. **She arrives, and does not circle for parking — ever.** Arrival queries the **Parking Shed**, the
   Car Parks within acceptable walking distance of the destination, nearest-first, and takes the
   first with space ([`adr/0009`](adr/0009-parking-is-modelled-supply-never-search.md)). The walk from
   it is her third **Leg**. Scarcity shows up as that walk getting longer, not as a failure — and a
   Trip fails only when it genuinely blows its **Commute Budget**.
9. **Trip Fate.** It completed. Had it not, the failure names a Citizen, an origin and a destination,
   so the city can say **why**. That is the thing SimCity 2013 structurally could not do: its agents
   descended a shared gradient field toward *the nearest thing advertising Home*, so there was no fact
   of the matter about where anybody had been trying to go.

---

## Every term, one line

| Term | What it is |
|---|---|
| **Segment** | one edge of the road graph, between junctions |
| **Trip** | one journey: origin, destination, Legs, and a Fate |
| **Leg** | one mode-homogeneous piece of a Trip. `walk → drive → walk` at minimum |
| **Traveller** | the temporary embodiment of a Citizen while travelling. A view, never an owner |
| **Habit Route** | the route a Citizen normally takes. Owned by the Citizen. Out of date about traffic on purpose; boundedly out of date about the map |
| **Sight / Sight Horizon** | the live view of the next Segment(s) and the arcs out of this junction; and how far that reaches |
| **Temperament** | how much better an alternative must be before this person bothers. Stable base plus per-decision jitter |
| **Diversion** | leaving the Habit Route at a junction because Sight found something better by more than Temperament |
| **Rejoin / Rejoin Target** | getting back onto the route by local steps toward the junction you skipped. Never a search |
| **Stress** | `volume ÷ capacity` × junction complexity. Decides fidelity, with hysteresis |
| **Statistical / Microscopic** | no vehicles and exact free-flow arithmetic / real vehicles in Lane queues |
| **Lane** | the entity that owns its Vehicles as a sorted queue and updates them all in one pass |
| **Microscopic Cap** | the ceiling on how many Segments may be Microscopic at once. A world constant, and reaching it is not a failure of the city |
| **Parking Shed** | the Car Parks within walking distance of a destination. Nearest-first, never a search |
| **Epoch** | the road graph's version stamp, which tells a stored route it may be wrong |
| **`T` / `d`** | how long a Habit may stay wrong about a new road / how near a new road must be to flag it |

---

## Where it is genuinely soft

Kept short and pointed at owners, because this is the part most likely to go stale.

- **Route diversity.** Every driver on the same pair remembers the *same* route, so traffic
  concentrates. Deleting the District key helped and did not answer it. `plans/0017` task 2.
- **The Microscopic Cap is a ratio nobody has both halves of** — how many Segments we can afford to
  simulate (**S5**) against how many a real city stresses at once (Trip generation, `06` 5b). Nobody
  has ever compared them. `plans/0002` §B.
- **The unset numbers.** `T`, the Sight Horizon — which is two parameters wearing one name — the
  Temperament base and blend, the Parking Shed's radius, and the Rejoin's crossing budget.
  `plans/0002` §D2.
- **Everything measured so far ran on invented demand.** Nothing generates Trips yet, so every
  origin-destination distribution S2 used was made up, and the same table's detour reads very
  differently on two of them. **Name the rung or do not quote the figure.**
