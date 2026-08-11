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

> **⚠ AMENDED 2026-08-11 — both quantitative claims in the paragraph above were measured, and both are wrong. The structural argument is untouched.** Spike **S5** ([`plans/0019`](../../plans/0019-s5-lane-kernel.md), numbers in [`spike-results`](../spike-results.md) → *S5*) built this structure in `adr/0003`'s Q16.16 arithmetic — sorted queue, Overlaps, IDM, `checked` narrowing — and ran it against a bare walk over the same arrays.
>
> - **The constant is not a `memcpy`'s. It is 26–29× a bare walk** (S5 tripwire **T3**, threshold 4×, fired at 6.5× the threshold). **The `O(n)` half is exactly right** — the pass is flat across queue lengths from 4 to 4,096 Vehicles per Lane and across networks from 4 KiB to 72 MiB of Vehicle rows. What the constant actually is: **three integer divisions per Vehicle per Tick**, two of whose denominators never vary. Replacing those two with precomputed reciprocals moves the pass **1.63–1.75×**, which is an *attribution and not a recommendation* — a reciprocal changes the arithmetic and therefore the State Hash, so under `CLAUDE.md`'s own test it is a design change however it was motivated.
> - **400,000 is not reproduced in our arithmetic: we measure ~325,000–330,000 with Overlaps and ~379,000–381,000 without** (tripwire **T1**, fired on both readings). **The figure quoted above was transplanted** — Eickhoff's engine is Rust, its arithmetic is **floating point**, and its denominator is a frame rather than our Tick. **The gap is the arithmetic, not the structure**, and it is a price `adr/0003` chose deliberately.
>
> **Do not quote our number as final yet.** Every S5 absolute is a **`powersave` lower bound**; the no-Overlaps figure needs 1.05× to clear 400,000 and the with-Overlaps figure needs 1.23×, both inside the 1.77× this corpus has measured between mismatched captures. **The honest statement is that the headline is not reproduced, not that it is refuted**, and the canonical `performance` capture is owed. What does not depend on the governor is T3, which is a ratio taken within one machine state.
>
> **Nothing in the table above this paragraph moves.** No spatial index, predecessor is the previous array element, linear scan of a contiguous array, scheduling granularity is one Lane — every one survived the transplant, along with zero allocation per Tick and determinism being nearly free. **The order of magnitude survives and the constant does not**, which is a statement about a sentence rather than about a design.

> **⚠ THE AMENDMENT ABOVE IS ITSELF AMENDED, 2026-08-11, HOURS LATER — and its first clause is withdrawn.** S5's **L5** re-measured the same kernel after correcting the substrate, and **400,000 is reproduced**: the Lane model runs at **~533,000–570,000 Vehicles per Tick per core with Overlaps** and ~577,000–580,000 without, against the 351,000–371,000 the same rung read before. **`adr/0016`'s transplanted headline stands.** T1 does not fire.
>
> **What changed is not the arithmetic and not the model. It is one line of `IntegerMath.FloorDiv`.** The substrate spelled its flooring correction `(n % d != 0) && ((n < 0) != (d < 0))`, so the **modulo was the first operand of the `&&` and ran on every call** — and RyuJIT does not fuse it with the division above it, so **every `Fixed.Div` in this project was two 64-bit divisions**. The IDM has three division sites and two of them can never have disagreeing signs. Swapping the two conditions short-circuits the modulo away, is **commutative over two side-effect-free conditions and therefore bit-identical by construction**, and is worth **1.50–1.51×** of the entire Lane kernel. Verified rather than argued: 294,912 Vehicles stepped 64 Ticks agree with the shipped kernel in **every position and every velocity**, and the repository's **1,060 tests and all three golden State Hash baselines are unmoved**.
>
> **So the gap this ADR was charged 1.7× for was never `adr/0003`'s integer division.** It was a redundant modulo, and integer arithmetic was carrying the blame for it. **The second clause stands untouched**: the queue pass is still **17.5–20.9×** a bare walk against T3's threshold of 4×, because a division remains a division and the `memcpy` claim is false in every form of the kernel.
>
> **The approximate-reciprocal form is now refused rather than deferred.** It buys 1.21× over the free correction, and it is the only one of the three that moves the State Hash — a different city, measurably so within 64 Ticks. The exact multiplier-and-shift alternative is bit-identical and is **also refused**, for the opposite reason: it is a **dead heat** with the free correction (27.0–27.5 ns against 26.9–27.0) and costs 8 bytes a Vehicle to achieve it. **Neither is worth a decision, so `plans/0002` §D2's row retires rather than fills** — the fast spelling and the correct spelling turned out to be the same one.

**Overlaps are how one dimension buys two.** Lanes that physically interact — parallel, opposing, or crossing — declare an Overlap and exchange their Vehicles' projected positions once per Tick, mapped into each other's coordinate space as ordinary obstacles.

> **⚠ PRICED 2026-08-11 — the exchange this ADR states and costs nowhere costs 1.15–1.18× the queue pass at two Overlaps per Lane** (S5 L2). The Overlaps-per-Lane count is **not S5's to choose and not this ADR's**: it is a property of the Road Graph the geometry pass produces, so the figure is published at 1, 2 and 4 per Lane and the headline carries 2. **The finding worth carrying is which implementation wins**: a **cursor** kept between Ticks — O(1) amortised, and state a promotion must materialise and a demotion must discard — beats a naive **scan** of the partner's queue by only **3–7%**, which is inside the run-to-run spread at 4 Overlaps per Lane. **Write the scan first.** The clever structure does not pay for the entry it would add to `03` invariant 3's enumeration until a Road Graph says a Lane has more Overlaps than this. That ordering could invert above ~4 per Lane and is milestone 5a's to settle. A crossing conflict, an opposing-traffic gap, and a parallel neighbour are then all the same thing to the solver: something in front of you at a known distance. There is no separate junction algorithm to write, debug, or tune.

**The Switch Lane is the idea that pays for itself immediately.** An invisible Lane spans the Overlap between two parallel Lanes, and — the load-bearing part — **the two normal Lanes are not connected to each other, only each to the Switch Lane.** Merging, normally the nastiest special case in traffic simulation, becomes an ordinary Lane obeying ordinary rules: entering it is an ordinary connection, finding a gap is ordinary car following, and a merge the driver aborts because required braking exceeds comfort is an ordinary failure to enter, not an exception path. The hardest part of traffic simulation collapses into something cheap and debuggable. `SOLVE THE ACTUAL PROBLEM`

## The constraint that makes it affordable

The Lane model is expensive per Lane and we run it on very few Lanes. [`0007`](0007-stress-driven-simulation-detail.md) settled that a Segment is Microscopic when it is under stress and Statistical when it is not, and that boundary is where this ADR stops:

- **Microscopic Segment** — real Lanes, real queues, IDM, Overlaps, Switch Lanes. Travel time is emergent.
- **Statistical Segment** — **no Lanes exist.** A Traveller is an origin, a destination, and an arrival Tick. Travel time is `distance / speed`, which on a free-flowing road is not an approximation but the exact answer.

The consequence to state plainly: **the number of Lanes running the full model is bounded by network stress, not by population.** A million Citizens do not produce a million queue slots, because only so many Segments can be stressed simultaneously. This is the entire reason a 1M-Citizen metropolis is affordable with an honest population count, and it is why "just run the Lane model everywhere" — the option Citybound took — is not available to us.

Promotion therefore has to **materialise** Lane queues from in-flight Trips, and demotion has to convert them back to arrival Ticks. Invariant 3 of `docs/03-agent-architecture.md` applies with full force: what is discarded on demotion must be enumerated. Queue position, headway, and any in-progress Switch Lane traversal are exactly the state at risk.

> **⚠ AMENDED 2026-08-11 — that enumeration is short by one, and the missing entry is the thing the Statistical representation is made of.** S5's L3 measured a demotion of jammed Segments and found **150,943 of 186,624 Vehicles had no arrival Tick to convert to**: they were at rest, and `distance / speed` is undefined for a stopped Vehicle. **A Segment demoted while jammed cannot say when its Vehicles arrive** — and a Segment is Microscopic *because* it is stressed, so this is the ordinary case rather than the corner. The proportion is the fixture's (that Segment carries exactly jam density) and **the phenomenon is not**. `03` invariant 3's list needs the arrival time itself, and the question of what a demotion does when it cannot compute one is `03 §5.1`'s to answer.
>
> **`adr/0016`'s own revisit trigger was also measured, and it does not fire.** *Promotion cost dominating the traffic budget* is a ratio and S5 built the instrument this ADR named none for: promotion plus demotion is **67.9–69.5 ns a Vehicle** against the cost of running that Vehicle for one Tick, a **break-even residency of 1 Tick** against a threshold of 30 — **2–3 Ticks after L5**, whose correction moved the denominator while leaving the numerator un-remeasured, and still an order of magnitude inside the threshold — one Segment traversal at free flow. **A Segment that stays Microscopic for even a moment has paid for its own conversion.** The consequence reaches [`0007`](0007-stress-driven-simulation-detail.md) rather than staying here: its hysteresis window has **no floor imposed from this direction** and may be set on behavioural grounds alone.

## Consequences

- **A Vehicle is addressed as (Lane, index), not as a global handle.** Anything holding a bare Vehicle reference across a Tick is a defect, because the queue is compacted as Vehicles leave.
- **Overlaps are declared, not discovered.** They are computed from geometry when the Road Graph is edited and revalidated against the Epoch, never searched for per Tick.
- **The IDM needs tuning parameters in the Ruleset**, not in code — desired headway, comfortable braking, acceleration exponent. `FAST ITERATION`
- **Lane-level tools remain an anti-goal.** Signal phasing and turn restrictions being *simulable* does not make them player-facing; see [`0009`](0009-parking-is-modelled-supply-never-search.md) for where that line sits.
- **Determinism is nearly free here.** Iteration order is queue order, and Overlap exchange happens once per Tick against the Past. There is no per-car scheduling order to get wrong.

## What would trigger revisiting

- **Discretionary lane changing.** Citybound's lane changing is weaker than its reputation: it is triggered purely by proximity to the end of a switchable stretch, with no incentive and no politeness criterion, so there is **no discretionary lane changing at all** — a driver never moves left because the left lane is faster. **MOBIL** (Kesting, Treiber & Helbing) is the missing half, and it composes with the Switch Lane rather than replacing it: MOBIL decides *whether* to change, the Switch Lane executes it. Deferred until lanes need to be chosen for reasons other than the next turn.
- ~~**Promotion cost dominating the traffic budget.**~~ **MEASURED 2026-08-11 and it does not fire** — break-even residency is **1 Tick** against a threshold of 30 (S5 tripwire **T4**). The trigger stands as written for a future re-measurement; what has changed is that it now names an instrument, which it did not when it was written. If materialising queues ever does cost more than running them, the answer remains wider hysteresis in [`0007`](0007-stress-driven-simulation-detail.md), not a third representation.
- **A junction type Overlaps cannot express.** Roundabouts and weaving sections are the candidates. Authored Junction pieces are the intended escape hatch and should be tried before the model is generalised.
