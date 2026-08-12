# A shed's use is the arrival query, and a stale shed is wrong by a bounded walk

**A Parking Shed is consulted on exactly one occasion — a Trip's arrival — so *checked at use* means
*checked on arrival*, and there is no second event to choose between.** The shed inherits
[`0012`](0012-routing-intent-lives-in-the-agent.md)'s invalidation **shape** and **not** its parameter:
its witness is per-Segment along the walk paths to the Bins it kept, a removal is exact, and an
addition needs **no `T`, no rotation and no proximity wake** — because a stale shed returns a *valid*
Bin that is merely not the nearest, and that error is bounded by the shed radius where a stale route's
is not bounded at all.

`SOLVE THE ACTUAL PROBLEM` `HONEST DEGRADATION`

## Why

### The question named two occasions and they are one event

`0002` §A has carried, since session M closed the route half, the sentence *"what nobody has typed is
what a shed's **use** is — a parking search, or a Trip arrival."* **Both readings resolve to the same
event, which is why nobody could type it.**

[`0009`](0009-parking-is-modelled-supply-never-search.md) gives the shed exactly one caller: *"Arrival
queries the destination's Parking Shed … nearest-first, and takes the first with capacity."* There is
no other. A **release** does not consult the shed — a departing car knows which Bin it holds, so it
decrements that Bin directly. And *a parking search* under the reading where it means a car hunting for
a space is **refused by the ADR's own title**, which under
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) is the one class of absence that
counts as evidence. So on either reading, the answer is *arrival*, and the alternative was a
distinction with no difference.

**What falls out is the rate, and it is the thing R5.6 could observe but not explain.** A shed's use
rate is the **arrival rate**, bounded by the Trip rate. A route's is the Trip-start rate **plus every
diversion** — which S2 R6.3 measured at 1,269.51 a Tick against 32–147 affordable, the largest number
in the routing model. *Checked at use* is therefore a far smaller bill on a shed than on a route, by a
factor nobody had named, and that is a mechanism rather than R5.6's observation that a shed *"is
consulted far less often than a route is driven."*

### The two consumers' soundness stories differ in kind, not in degree

Session M's contract is *never wrong about a removal, boundedly wrong about an addition*. Both halves
change meaning here.

**Removal is exact and cheap, exactly as for routes.** The witness set is the Segments on the walk
paths to the Bins the shed actually kept — S2 R5.6's `PerSegmentPaths` rung — and the test is
containment. At 400 m a shed's walk *ball* explores 22 Segments while the walks to the Bins it keeps
touch **2**, so the conservative witness is 11× its own answer, and the storm figures are decisive:
per-Segment-by-paths costs **0.10% of a Tick** against a global counter's **1,638.20%**.

**Addition is where the shed and the route part company, and the difference is the size of being
wrong.** A route stale about an addition may send a Traveller across town the long way; the error is
unbounded and only `T` bounds it. **A shed stale about an addition returns a Bin that exists, has
capacity, and is within the radius — it is simply not the nearest one.** The error is a *walk*, it is
bounded above by the shed radius, and it is already scored: the longer walk Leg counts against the
Commute Budget, which is `0009`'s entire degradation story working as designed. **A mechanism whose
staleness produces exactly the outcome its degradation model already prices does not need a second
mechanism to bound it.**

So: **no `T`, no rotation, no proximity wake.** Rebuild when any Segment in the witness set bumps.
Accept bounded suboptimality otherwise.

### The residual case is named rather than waved away

The miss is a **new Segment outside the current witness set that would bring a nearer or emptier Bin
into range**. It is narrow — the witness paths radiate from the pedestrian Access Point, so a road
built near a Building is almost always already in its shed's witness — but it is not empty, and it has
a worst case worth stating: **a shed that is full, where a new road would have reached one that is
not.** There the stale answer is not a longer walk but a Trip that fails on Commute Budget when it
need not have.

**That is accepted, and the reason is a comparison rather than a shrug.** Adding `T` to the shed buys
a bound on a case that is already rare, at the cost of a per-Citizen staleness bit, a drain, and a
hash-bearing number — against a failure whose visible form is *the new car park took a while to catch
on*, which is a legible outcome rather than a broken one. `PLAYER GOVERNS` cuts the other way here from
how it cuts on routes: a player who builds a car park and sees it fill slowly is reading a true
statement about the city's habits. **The trigger for reopening is written below and is a number, not a
feeling.**

### It is one contract with two parameter sets, and the board's question resolves as *no*

The board carries session M's remainder as *"whether the invalidation contract is one contract, since
`05 §3`'s Parking Shed is a **neighbourhood** rather than a **path**."* The neighbourhood/path
distinction is real and is the reason the witness had to be measured separately — but it turns out not
to be what decides the question. **The contract is one *shape* — witness, exact-on-removal,
bounded-on-addition — instantiated with two different bounds, and the shed's bound is structural where
the route's is a parameter.** Session M was right that they are not one contract and right about which
axis, and wrong about why: it is the **magnitude of the addition error**, not the geometry of the
witness.

## Consequences

- **`05 §3`'s owed correction is paid.** *"Invalidated lazily against the Road Graph Epoch"* said **when
  you pay**, never **what survives**. Under a single counter every edit anywhere invalidates all
  159,825 sheds, and laziness merely spreads **255.560 ms** across arrivals instead of stalling once —
  which, because `0009` pays the query *on arrival*, converts one stall into a **stampede**. The rung is
  per-Segment witnessed by paths, and it is stated as a rung rather than as a contingency.
- **`0009`'s revisit trigger is discharged in advance.** It names cached shed membership as the
  mitigation *"should shed queries appear in a profile"*. R5.6 measured the invalidation before any
  profile existed, so this is a data-layout item, which is where `05 §3` already put it and which
  `0009`'s own superseding note asked for.
- **The Parking Shed radius stays unset and now has a named ratifier**, which it has never had
  ([`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)). **Ratifier:
  milestone 8's first run reporting the walk-Leg length distribution as shed occupancy approaches 1** —
  which is `0002` §B's *does parking scarcity degrade as a gradient* and is the same instrument, so the
  number and the claim ratify together. **Two constraints are stated now and neither is a value.** It is
  bounded above by the Commute Budget's walk allowance, since a shed wider than a Trip can afford to
  walk is a shed whose outer Bins can never be taken. And its cost gradient is measured: R5.6 gives 110
  Bins found at 400 m against **596 at 800 m**, so doubling the radius is roughly 5× the shed. **Do not
  reach for a value from the walking-time intuition** — five minutes at 5 km/h is 417 m and it is
  exactly the sort of number `0044` had to measure back out of three documents.
- **A shed carries no staleness bit and no epoch of its own beyond its witness set.** This is a
  *smaller* structure than routes need, and the saving is the point: `0012`'s belt-and-braces exists
  because a route's addition error is unbounded, and paying for it here would be paying for a bound the
  Commute Budget already provides.
- **`CONTEXT.md` → Epoch's *when you pay / what survives* distinction now has its second consumer
  written down**, after 5a-bis's frontage. Both are non-routing, which is the evidence that the
  distinction generalises rather than being a routing idiom.

## What would trigger revisiting

- **The full-shed miss turning out to be common rather than narrow.** The refuting number is **the
  fraction of Trips that fail on Commute Budget whose shed was stale about an addition** — measurable
  at milestone 8 by rebuilding the shed on failure and re-querying. If it is material, the repair is
  `0012`'s proximity wake applied to the witness set, and `d` is already a number that exists.
- **A second caller for the shed appearing.** The whole of this rests on the shed having exactly one.
  Illegal parking as an overflow tier (`deferred.md`) would be consulted *after* the legal ones on the
  same arrival and does not add an occasion — but a *departure* that had to find somewhere to move a car
  to, or a shed consulted at Trip *planning* to predict failure, would, and either would reopen the use
  rate.
- **Shed rebuild appearing in a profile at 1M despite the rung.** R5.6's 0.10% of a Tick is an
  *invalidation* figure; **the query has never been measured** and `0009` pays it on every arrival. That
  gap is `0002` §B's and it is the one number this decision assumes rather than knows.
