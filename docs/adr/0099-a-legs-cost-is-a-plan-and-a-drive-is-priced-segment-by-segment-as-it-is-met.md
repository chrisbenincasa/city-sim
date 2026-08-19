# A Leg's cost is a plan, and a drive is priced Segment by Segment as it is met

**A vehicular Leg stores its route and its free-flow cost when it is planned. A Traveller executing it
holds a cursor onto that route, and each Segment's dwell is priced when the vehicle *enters* it, from
that Segment's volume at that instant.** A Leg's stored cost is therefore what the journey was expected
to take, and the realised duration is the sum of what it actually met. `LEGIBLE CAUSE`

**And the volume/capacity ratio the delay function reads is a stock over a stock, converted by Little's
Law: a Segment running at capacity holds `capacity per Tick × its free-flow crossing time` Vehicles.**
`SOLVE THE ACTUAL PROBLEM`

This **narrows [`adr/0075`](0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md)**, which gives a Leg *a
cost and no path*, and **narrows `CONTEXT.md` → Traveller**, which says a Statistical Traveller is
*nowhere in particular* between its endpoints. Both narrowings are stated below rather than in an
amendment alone, because the sentence each ADR loses is smaller than the sentence a reader would
otherwise infer it lost.

## Why

### The plan and the execution have to be different numbers

[`adr/0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) attributes a Segment's
volume **on entry** and releases it on exit, so a vehicle is on exactly one Segment at a time and
somebody has to know which. `adr/0075` had given a Leg a cost and no path, which is why milestone 5b
shipped `Invariant.SegmentVolumeIsConserved` over two columns nothing incremented: **there was no next
Segment to move to.** 5b's own close-out found this and filed it to 5c, and it is the debt this ADR
pays.

Once the vehicle is on a Segment, the delay function has an input, and the delay is a *different number*
from the one the Leg was planned with. **Keeping both is not redundancy — they answer different
questions.** The plan is what a Citizen judged the journey by, which is what the Commute Budget is
measured against ([`adr/0095`](0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md));
the execution is what the road actually did, which is what a congestion overlay draws. Collapsing them
would mean either a person refusing a job because of a jam they could not have known about, or a
congestion reading taken off a free-flow number.

### The route lives on the Leg, not in the route cache

Milestone 5c task 4 built `adr/0060`'s shared route cache, and the obvious economy is to look the route
up again each Tick rather than storing it per Leg. **It is wrong, and the reason is a property of caches
rather than of this cache.** The cache is fixed-capacity and evicts; an entry disappearing under a
moving vehicle strands it on a Segment it never leaves, which is an
[`adr/0006`](0006-no-collection-grows-with-elapsed-time.md)-class leak that presents to a player as a road busy for
ever with nothing on it.

So the two structures answer different questions and both survive. The cache answers *what is the route
between these two nodes*, is shared, and is optional. `RouteHopTable` answers *what is **this** Traveller
doing*, is per-Leg, and is freed when the Trip ends. ***A shared cache is an optimisation and an
executing plan is state.***

### The direction is stored, not derived

Volume is per direction — `adr/0041` — and a route hop therefore carries one bit saying which way round
its Segment is crossed. It is **stored rather than derived from the arc**, because an arc index is not
stable across a graph edit and this table is saved. It is **stored rather than recomputed at exit**,
because the entry and the exit must key on the same value or a Segment's two counters drift in opposite
directions while their sum stays right — which the conservation invariant cannot see.

The two endpoint Segments take the direction of the arc beside them, since a vehicle starts part-way
along its own Segment and leaves by one of its two nodes. Where origin and destination share a Segment,
or sit on two that meet at a node, **there is no arc and nothing determines a direction**; forward is
then a convention. That is stated so nobody reads the resulting forward bias on very short journeys as a
property of the city.

### The sub-Tick carry is arithmetic, not polish

An arrival is an instant on the clock, so each hop's cost has to be floored to whole Ticks. A 32-Tile
Street at 50 km/h costs **0.22 Ticks** —
[`adr/0071`](0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md)'s own
illustration, restated under
[`adr/0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)'s
2048-Tick Day. **Flooring each hop independently makes every Street free**, and a twenty-Segment commute
arrives on the Tick it left. The Traveller therefore carries the sub-Tick remainder across hops *and
across Legs*, so a Trip's realised duration is its parts summed and floored once. That is also what keeps
[`adr/0008`](0008-walking-is-a-simulated-leg.md)'s `walk → drive → walk` from being cheaper than the one
journey it is.

### Little's Law, and the premise that quietly expired

`RoadSegmentTable.VolumeForward` counts Vehicles **present**. `CapacityPerDay` counts Vehicles
**passing**. A volume-delay function divides one by the other, so something has to relate them, and
Little's Law does it exactly: `present = passing × time spent`.

**The first cut of this mechanism skipped that term**, on the strength of `adr/0041`'s own *a vehicle
crosses about one Segment per Tick* — which would make the stock and the per-Tick flow the same number.
⚠ **That sentence was true when it was written and `adr/0094` made it false.** `adr/0041` states its
premise in the open: *"which follows from `TICKS_PER_DAY = 8192` and a block-length Segment"*. Under that
clock a Tick was 10.5 s and a 128 m Street took 9.2 s, so the rate was ~1.1. Under the 2048-Tick Day a
Tick is 42.19 s and the rate is **~4.6**.

The consequence was not subtle and was invisible without a measurement: the function came back **inert at
every population this project can build** — a peak of 4–6 Vehicles on one Segment against a denominator
of 42, ×1.0000 delay from 4,000 Citizens to 160,000. ***A premise licensing one quantity to stand in for
another is itself a measurement, and a constant moved in another document can retire it silently.***

**The corrected denominator is physically checkable and the old one was not.** A Street carries 3,600
Vehicles an hour and a 128 m Segment is crossed in 9.2 s, so it holds **9.2 Vehicles** at capacity — a
14 m spacing, a one-second headway, which is what a road at capacity looks like. Forty-two Vehicles on
128 m is a 3 m spacing, which is a car park.

## Consequences

- **`adr/0075` keeps its argument and loses one clause.** *A Leg is a plan* stands, and the plan is now
  *a cost **and** the Segments it was planned over*. **A walk Leg still stores no route** — nothing reads
  one, since `03 §3.7` keeps pedestrians out of Stress permanently — so the ADR's economy survives
  exactly where it was argued.
- **`CONTEXT.md` → Traveller keeps the distinction that matters.** What a Statistical Traveller still
  lacks is a *position within* a Segment: no offset, no lane, no headway. That is the whole of what
  separates it from Microscopic and is what `adr/0007`'s promotion is defined against.
- **`Invariant.SegmentVolumeIsConserved` is load-bearing with no edit to it.** It shipped in 5b knowing
  both sides were structurally zero, against slice 5 task 7's precedent of withholding a vacuous
  assertion — the distinction being between an assertion whose *shape* is wrong until the world changes
  and one that is **correct and temporarily trivial**. This is the payment on that judgement.
- **`plans/0013`'s attribution row is ~4× light.** `adr/0041` prices ~80,000 increment/decrement pairs a
  Tick at 1M from a crossing rate of ~1; the rate is now ~4.6 by the same arithmetic, and S2 R2a's
  measured 0.79–0.83 was taken under the old clock. That is a cost, so it is routed to `plans/0013`
  rather than settled here ([`adr/0073`](0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)).
- ⚠ **A generated city cannot congest itself, and this is the finding that outranks the mechanism.**
  Peak load is `v/c` **0.44** at 4,000 Citizens, 16,000 and 64,000 alike — a 16× population against an
  identical load — because the paved extent scales with the square root of the population, so the network
  grows with the traffic. ***The same number sizes both the demand and the supply.*** Congestion in this
  design is something a **player** makes by laying too little road for what they zoned, which is
  [`adr/0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md)'s whole point and is
  unreachable from `CommandKind.Populate`. Both sides are asserted: the shipped city is byte-identical
  with and without the function, and a city whose Streets carry 200 Vehicles an hour is not.

  > **The obvious repair — *lay less road* — was tried on three authored dials and none of them works.**
  > At 16,000 Citizens, each run twice:
  >
  > | dial | Segments | busiest one-way | `v/c` | vehicle-Ticks | loaded ÷ free |
  > |---|---|---|---|---|---|
  > | shipped | 769 | 4 | 0.44 | 23,760 | **×1.0000** |
  > | `lots_per_segment` 5 → 32 | 145 | 5 | 0.54 | 11,364 | **×1.0000** |
  > | `commute_peak_factor` 3 → 192 | 769 | 5 | 0.54 | 5,003 | **×1.0000** |
  > | both | 145 | **10** | **1.09** | 2,393 | **×1.0000** |
  >
  > ⚠ **Less road makes the city smaller rather than busier.** `SyntheticCity` sizes the paved extent to
  > hit a **Lot** count, so cutting roads per Building shrinks the map, shortens every commute and takes
  > total traffic **down 2.1×** while taking `v/c` up 0.1. ***Every supply dial this simulation exposes
  > moves demand with it***, which is the √population result on a second axis.
  >
  > ⚠ **And the last row is over capacity while still being byte-identical.** Mean load there is **0.032
  > Vehicles per Segment per Tick** — the road is empty ~97% of the time — so a busiest-Segment reading
  > of 10 is a coincidence of arrival times rather than a jam. ***A peak that a mean does not follow is a
  > coincidence, and a volume-delay function prices means.*** The aggregate is dominated by crossings at
  > volume 1, where the delay is 2×10⁻⁵ and below Q16.16's resolution.
  >
  > **So the constraint is trips, not road.** Everybody makes **one journey a Day** — the commute is the
  > only Trip generator built, and `06`'s *Mechanisms with no milestone* lists seven more. Even in the
  > sharpest burst it is ~8,000 departures into 769 Segments with a commute lasting about one Tick.
  > **The network is bigger than the fleet at every setting of every dial.** The evening leg is not the
  > answer either and is already refused rather than absent (`CommuteEngine`, on `adr/0070`): doubling
  > the trips doubles 0.032 to 0.064.
- **Every drive is still quoted too cheap at planning time**, because the Commute Budget is judged on the
  free-flow plan. That is deliberate — a person cannot see tomorrow's jam — and it is what
  `adr/0095`'s fourth revisit trigger is waiting for: the three rungs are percentiles of a **free-flow,
  foot-only** distribution, and this is the first mechanism that can produce a distribution that is
  neither.
- **`[traffic]` is a new optional Ruleset table with three required keys**, on `[households]`' polarity:
  omitting it is a city whose roads never slow down, which is also every city this project described
  before now, so a Ruleset written earlier still means what it meant. **α and the clamp are authored as
  percentages** because the file has no decimals and *the name of a quantity is not its denomination* —
  `alpha = 15` would be off by two orders of magnitude with nothing able to notice, which is the failure
  `adr/0094`'s `Speed.PerKilometrePerHour` literal actually committed.
- ⚠ **No run this project can currently perform ratifies α, β or the clamp**, which is a live
  qualification on the ratifier named below rather than a reason to delay it. The sweep above found no
  authored configuration that moves the function at all, so a flat reading from task 8 is evidence about
  the **fixture** and not about the parameters. ***A parameter can only be ratified by a run that
  reaches the range it governs.*** Recorded on `plans/0002` §D2's row so the ratifier cannot be
  discharged by a null result.
- ⚠ **α = 15%, β = 4 and a clamp of 400% are sourced and not ratified.** They are BPR's textbook figures
  and they are what spike S2 ran — recovered from S2 R8.0's published *an arc at the clamp costs 39.4×
  free-flow*, since `1 + 0.15 × 4⁴ = 39.4` exactly and the spike published the delay rather than the
  ratio. S2 is synthetic, so `adr/0052` keeps *sourced* and *ratified* apart. Named ratifier: **5c task
  8's long run**.
- **No shipped Ruleset states `[traffic]` or `[households]`**, so none of this is reached by the committed
  golden baseline. Stating three unratified numbers in a file where they cannot act would be authority
  accumulating without exercise; the place both tables are stated together is task 8. The test suite says
  so in its own remarks rather than leaving a reader to infer it from a green board.

### ⚠ The volume-delay function is a loop and not a formula

*Added 2026-08-15, from the first city that ever evaluated this function off its flat part.*

**Congestion slows a Vehicle, a slower Vehicle dwells on its Segment longer, and longer dwell *is* higher
volume** — because the volume this function reads is a **stock**, and the Little's Law term above is
exactly what puts the dwell inside it. Past `v/c` = 1 that feeds itself. On
`ConnectedCityCongestionTests`' dumbbell — two zoned districts joined by **one** Street corridor, every
Segment laid by `CommandKind.Connect`, at the **shipped** capacity — a free-flow control peaking at
**130%** gives a priced world peaking at **1,074%**, and 1,074% is not what BPR returns for 130%.

***A formula evaluated on its own output is a dynamical system, and the first reading off the steep part
is the first one that can say so.*** The corpus has only ever quoted BPR as a static curve, this ADR
included, and every figure above it was taken where the curve is nearly flat — which is the same defect
as citing a premise that has expired, one section up, arriving on the mechanism rather than on a
constant. It is `03 §3.2`'s *use it only where it is strong* with a number attached, and it is an
argument for the Microscopic tier from a direction nobody took it from. **It does not move α, β or the
clamp**: the clamp is what bounds the feedback, and it takes well under one percent of the loaded
Segment-Ticks over that ladder, so it is holding a quartic's tail without touching its body.

## What would trigger revisiting

- **A Segment gets short enough, or a Tick long enough, that a vehicle crosses several per Tick and the
  per-hop pricing becomes the dominant cost of Phase 4.** The rate is ~4.6 today. If it reaches a point
  where advancing cursors outweighs the Trips being made, the answer is a coarser cursor — dwell in
  *runs* of Segments — and not the removal of the cursor.
- **Anything makes a walk Leg's Segments readable.** `03 §3.7` says pedestrian networks do not saturate,
  and that is a permanent decision rather than a simplification; if it were reopened, the route would have
  to be recorded for walks too and this ADR's *only vehicular* economy goes with it.
- **The realised duration starts to matter to a decision rather than only to an overlay.** Today nothing
  reads it back: a Citizen judges a commute on the plan. A model in which yesterday's *actual* commute
  changes tomorrow's job choice is `adr/0046`'s Habit reaching the labour market, and it would need the
  realised cost stored somewhere that outlives the Trip.
- **The volume-delay function stops being the only reader of volume.** `adr/0007` makes Fidelity a
  property of Stress, and Stress is `volume / capacity` times a junction factor — so the moment promotion
  is built, the Little's Law denominator here becomes the definition of that ratio for two consumers, and
  it should be moved somewhere both can read it rather than duplicated.
- **A second Trip generator lands.** The sweep above says the binding constraint is one journey per
  person per Day, so the first generator that adds a second — shopping, school, or the evening leg once
  a schedule has a shape — is the first thing that can move mean load off 0.032, and this ADR's
  *inert on a generated city* consequence should be re-measured on the day rather than argued about.
- ⚠ **A player-built city reaches `v/c` above the clamp routinely.** The clamp exists so that two jammed
  routes are not compared on noise; if real play sits past it, the curve has stopped discriminating where
  the game is actually played and the clamp is the number to move, not α.
- ⚠ **A Segment's free-flow crossing takes more than a Tick.** Added 2026-08-14 by milestone 5c task 7,
  which measured it from the outside. Between the shipped 3,600 Vehicles/hour and about 600, this
  function changes what a crossing **costs** without changing how many Vehicles stand on a road at any
  **Tick boundary** — because at 0.22 Ticks a crossing, even a large multiplier stays sub-Tick. So the
  bill moves and every per-Tick reading of volume is identical, which is why `--traffic`'s two panels
  come out the same over most of the capacity range. ***A per-Tick snapshot cannot see a sub-Tick
  delay.*** That is harmless while the only consumer is the Traveller's own arrival time; it stops being
  harmless the moment volume itself is read by something else — `adr/0007`'s Stress is the named case
  above — because that consumer would be sampling a quantity this function provably moves and it
  provably cannot see. If a Segment ever costs more than a Tick free-flow, or a second consumer starts
  reading volume, re-derive what the per-Tick series is allowed to be used for.
