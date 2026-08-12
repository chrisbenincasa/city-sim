# The behavioural clock is global and car-following sub-steps inside it

**The Tick is a behavioural unit. `TICKS_PER_DAY = 8192` stands, a Day is 24 in-world hours, and a
Tick is therefore 10.546875 s of in-world time — a derived quantity, not a free one.** Car-following
gets its own finer clock, taken as an integer **sub-step ratio** inside Tick phase 4 and visible to
nothing outside the Lane kernel. [`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)'s
conclusion survives whole; its **derivation is replaced**, and the sentence *the traffic model is
upstream of the clock* is inverted.

`SOLVE THE ACTUAL PROBLEM`

## Why

### The corpus holds three answers and they are two clocks, not three

| Source | Tick, in seconds | A vehicle at 50 km/h | 8192 Ticks is |
|---|---|---|---|
| `Speed.cs`, shipped, and every authored `*_speed_kph` | **10.546875** | 36.6 Tiles/Tick | **24 h** |
| [`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) — 4.2 m/Tick at ~65 km/h | 0.2326 | 1.05 Tiles/Tick | **31.8 min** |
| `02 §1.2` — *"~0.5 Tile/Tick"* | 0.144 | 0.5 Tile/Tick | **19.7 min** |

The lower two rows are within 1.6× of each other and are both car-following figures. The top row is
45–73× away from both and is the calendar. **So this is one disagreement, not three**, and it is
between a clock that makes a Day a day and a clock that makes a vehicle a vehicle.

The shipped row is the one that is *right about the world*: `--trips` reports a median walk of 1.4 min
at 128 m and 18.3 min at 2,048 m, which is 5 km/h to the digit. A person walks at walking pace in this
simulation today. What is wrong is the table a developer reads to find out why.

### The freedom `0019` spent both exchange rates from has been spent

`0019` rests on a categorical claim: Ticks → seconds and Tiles → metres are *"both invented outside the
simulation and both free"*, and *"the metre is a number in a wiki. Nothing reads it."*

**That was true when it was written and is false now, and the Ruleset is what made it false.** The
`[roads]` table authors speeds in **km/h**. `05 §26` fixes a Tile at **~4 m**. `02 §2` mandates that
speed be stored in **Tiles per Tick**. Those three quantities determine the fourth: given a speed in
km/h, a Tile in metres and a speed in Tiles/Tick, **the duration of a Tick in seconds is derived**.
There is one degree of freedom across the four and the corpus has been spending it three times
independently.

***A degree of freedom is spent by the first document that uses it, and nothing announces the
spending.*** That is why this drifted without anybody being careless: no edit made `0019` wrong. A
Ruleset key did, in another file, years of documents later. It is the shape [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
governs read backwards — that ADR forbids reasoning from an absence that may since have been filled,
and this forbids reasoning from a **freedom** that may since have been spent.

### There are two Ticks→seconds rates wearing one name, and that is why the ratio was thought safe

`0019`'s exchange-rate table has **one** row for time: *Ticks → real seconds, chosen by the host, effect
on simulation **none***. There are two, and only one of them is the host's.

| Rate | Who sets it | Free? |
|---|---|---|
| Tick → **wall-clock** seconds | the host, via the speed ladder | **Yes.** The simulation cannot see it |
| Tick → **in-world** seconds | derived from `TICKS_PER_DAY` and a 24-hour Day | **No.** Every authored km/h is denominated in it |

**Collapsing the two is what let `0019` believe its headline ratio was invariant.** It claims of
*480 Ticks ÷ 8192 = 5.9% of a life spent driving* that *"that number survives every exchange rate…
It is the only time-related quantity in the design that is about the world."* **It does not survive.**
A commute of fixed physical length takes `D ÷ (v · t)` Ticks, where `t` is the Tick's in-world
duration; the Day is pinned at 8192 Ticks regardless. So the ratio scales with `t`, and pinning the
Day in Ticks while changing what a Tick *is* changes how much travel fits inside a Day. It survives
the **speed ladder**, which is the rate `0019` had a row for, and not the one it did not.

**Corrected, the number moves 4.3×.** A full 16.4 km map crossing at 50 km/h is 1,180 s — **112 Ticks**,
or **1.4% of a Day** one way and 2.7% both, against the stated 5.9% and 11.7%. Since *share of life in
transit* is the same quantity as *share of the population on the road at any instant*, **the corpus has
been assuming roughly four times the standing traffic the shipped numbers actually produce.** The 480
was self-consistent with `02 §1.2`'s 0.5 Tile/Tick and travelled with it; at that speed "cross-town"
was about a kilometre, which is a District, and the row predates the 4096² map.

The direction is *favourable* — fewer vehicles in flight than every estimate assumed — and that is worth
distrusting on the board's own standing lesson. It belongs to the same exception S5's `FloorDiv`
correction did: **a number improves when the thing being measured was never what the measurer thought.**
It is not a fixture meeting a real world, so it does not weaken the rule that unit costs come in worse.

### The two constraints are both real and 45× apart

**① Behaviour.** A day's travel must fit inside a Day, or the Commute Budget, work shifts, sleep and
Life Stages are all meaningless. This wants a Day of 24 in-world hours.

**② Car-following.** A vehicle must not advance far relative to its safe following distance in one
step, or a car approaching a stopped queue overshoots before it can react — SUMO's documented
emergency-braking failure, and Treiber's Δt ≤ 0.5 s. This is the entire ground on which
[`0007`](0007-stress-driven-simulation-detail.md) rejected vehicles-as-animation.

At 10.546875 s a car clears a 128 m Segment in 0.9 Ticks and Lane queues cannot form. At 0.2326 s a
Day is half an hour. **Both constraints are correct and one number cannot satisfy them**, which is the
whole of the problem and is why no amount of choosing between the three rows above would have worked.

Appearance is **not** a third constraint. `02 §1.2`'s *"traffic reads as ~130 km/h"* column is
65× adrift of the `Day` column beside it — see [`plans/0012`](../../plans/0012-corpus-audit.md) — because
once a Day is 8m32s of wall clock, *everything* on screen moves at 168.75× and there is no freedom left
to spend on making a car look like a car. **A speed picked to satisfy appearance is bought with
currency the pacing decision had already spent.**

### The global fine clock does not fit, and the measurement already exists

`0019` chose ② as the global clock. Price that against the two figures a real 1M city has produced:

- S0b: a Tick with work in it costs **8.72 ms at 1M**, 55.9% of the 15.6 ms budget at 4×.
- S0a: an **empty** Tick at 1M costs **0.112 ms**.

Under ② an in-world Day is 86,400 ÷ 0.2326 = **371,420 Ticks**, which is 45.3× more Ticks for the same
in-world time. To hold the same wall-clock pacing, a Tick must fall to 8.72 ÷ 45.3 = **0.192 ms**. The
empty Tick already costs 0.112 ms of that, leaving **0.080 ms** for the 8.61 ms of work S0b measured —
**108× short**.

No optimisation anywhere in the simulation recovers 45×. **The global fine clock is refused on
arithmetic, not on preference**, and this is the first time the question has been put to a measured
Tick rather than to a design argument — the whole of `0019`'s derivation predates any Tick figure taken
over a city.

### Sub-stepping does not create that cost; it localises it

The 45× is real either way: car-following genuinely needs 0.23 s of resolution. The only question is
**what else pays for it.** A global fine clock bills every Household, every Bin Rule, every Map Layer
diffusion and every walk Leg for a resolution only vehicles asked for. Two clocks confine the bill to
its cause, which is `SOLVE THE ACTUAL PROBLEM` and is [`0007`](0007-stress-driven-simulation-detail.md)'s
own principle on a third axis — detail arriving where scrutiny does, here resolution arriving where the
physics needs it.

**Nothing outside the Lane kernel needs the finer clock, and the corpus already says so twice.**
`0019` itself: *"if vehicles did not exist, this simulation could run at 24 Ticks per Day and nothing
would suffer."* And `03 §3.8` on the other mode: the interpolation argument *"holds for walkers
permanently"* and local avoidance *"must not touch arrival time"* — a walk Leg is a departure, an
arrival and a cost, with nothing between them a finer step would reveal. **Walking is not a continuous
coupling**, which is what makes the asymmetry clean: ② is a *sub-model's* constraint and ① is
everyone's.

### 8192 survives, and the reason it survives is not the reason it was chosen

Under the behavioural clock, `TICKS_PER_DAY` is bounded below by the finest behavioural event worth
scheduling distinctly and above by Tick throughput. At 8192 a Tick is 10.5 s: a two-hour departure
spread has 683 distinct slots, a 128 m walk is 8.7 Ticks, a half-hour commute is 171. It sits
comfortably inside both bounds. **The number is right and the derivation was wrong** — the same shape
as [`0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md) and
[`0064`](0064-a-bins-capacity-is-a-property-of-the-ruleset-in-force-and-an-over-full-bin-drains-rather-than-clamps.md),
and it is why this decision **moves no State Hash and re-records no baseline**.

Of `0019`'s three reasons for declining 4096-with-doubled-speed, **visual honesty falls** with the
column it was derived from, **queue fidelity moves** to the sub-step ratio where it now belongs, and
**within-Day scheduling resolution survives as the only one left**. The argument listed third and
looking like a makeweight is the one that was load-bearing all along.

### It does not break the one-clock rule, and that objection is the first one to answer

[`0010`](0010-one-clock-and-demographics-by-sorting.md) forbids a second time base, and `CONTEXT.md`
→ Day states why: *"a conversion factor between two time scales would break the literal truth of
statements like 'this shop closed because its customers' commutes got too long.'"*

**That rule bars a second *slower* base — a calendar for growth running beside a clock for behaviour —
and a sub-step is the opposite object.** It is faster, it lives inside one Tick phase, and it never
escapes it. No simulation quantity is denominated in sub-steps; a Traveller's arrival is an arrival
**Tick**; the Event Wheel, the Census, every Rule rate and the Commute Budget are all untouched; and
nothing outside the Lane kernel can observe that sub-stepping happened at all. **What `0010` protects
is that every causal statement in the game is expressible in one unit, and it still is.**

The precedent is already in the corpus twice over: `05 §1`'s *fixed sim Tick, interpolated render* is
the same shape one layer out, and every reference game with a physics step does exactly this.

## Consequences

- **`0019` is amended, not superseded.** Its headline — *`TICKS_PER_DAY` is a balance constant, not a
  pacing knob* — is strengthened: with traffic content removed it is now a **purely behavioural**
  balance constant. What is struck is the causal chain, the *traffic model is upstream of the clock*
  claim, and the visual-honesty bullet.
- **`CONTEXT.md` → Tick is wrong and is corrected.** *"The Tick is fine-grained because of traffic and
  nothing else"* inverts: the Tick is **coarse-grained because of behaviour**, and traffic is the one
  process that does not inherit it.
- **The sub-step ratio is a new hash-bearing number, and it is unset** ([`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)).
  It changes where vehicles are, so it changes the State Hash. Its ceiling is Treiber's Δt ≤ 0.5 s,
  which is a ratio of **21**; `0019`'s comfort figure of 12% of following distance is **45**. Named
  ratifier: **S5**, re-run with the kernel sub-stepping. **Its storage class is deliberately not
  decided here** — the Lane kernel is unbuilt, and choosing a parameter's storage class for a consumer
  that does not exist is exactly what [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)
  forbids.
- **The Microscopic Cap falls by the sub-step ratio, and this is the decision's real price.** S5 gives
  27.4 ns a Vehicle a kernel step, so 15.6 ms holds ~569,000 Vehicles at one step and **~12,600 at 45
  or ~27,100 at 21**. That is a *second* independent reason the Cap is unset, and it is the one that
  moves it most.
- **⚠ The Cap's demand side does not obviously fit inside that, and the gap must not be buried.** S2 R2
  puts 2,592 of 33,018 Segments over an 80% stress threshold; at S5's 72 Vehicles a Microscopic Segment
  that is **186,624 Vehicles**, which is **7–15× the sub-stepped supply**. Heavily caveated — R2's
  uniform origin-destination draw on a frozen cost basis is the *longest-trip* distribution available,
  so it is an upper bound — but the direction is bad and the honest reading is that
  [`0007`](0007-stress-driven-simulation-detail.md)'s *"it scales"* clause is what would fail, not the
  Cap's value. Already open as `0002` §B's *how many Segments are stressed at once at 1M*.
- **⚠ Every in-flight vehicle count in the corpus is denominated in the struck ratio and wants re-deriving.**
  `0019`'s 11.7%-of-life-in-transit is 2.7% corrected, so the standing traffic a given population
  produces is **~4× lower** than the figure the corpus has been reasoning from. This reaches
  [`plans/0013`](../../plans/0013-tick-budget.md) — routing's multiplicand, already flagged as counting
  the wrong event — and S2's 37k–111k in-flight band, whose *own* correction found the two axes enter as
  a product. **Do not net the two corrections by hand**: one is a ratio and one is a distribution, and
  the honest move is to re-derive from a measured commute distribution, which is milestone **5b-bis**'s
  to produce. Filed rather than applied.
- **The Commute Budget is denominated in Ticks at 5.6889 Ticks per clock minute**, so a 30-minute budget
  is 171 Ticks. This is the number milestone 5b-bis was waiting on, and it is the only thing that slice
  needs from this decision.
- **`Speed.cs`'s factor is unchanged and correct.** What changes is its comment: the Tick's duration in
  seconds is a **derived simulation fact**, not a host exchange rate the core cannot see.
- **`02 §1.2`'s two tables are corrected** — the *reads as* column deleted rather than restated, and the
  `~0.5 Tile/Tick` row split into the car-following ceiling (which survives, and now belongs to the
  sub-step ratio) and the number (which does not).

## What would trigger revisiting

- **The sub-step ratio measuring badly enough that the Microscopic tier cannot be afforded at any
  ratio.** The fallback is not a finer global clock — that is 108× worse — but a smaller Cap and more
  of the network resolved statistically, which is a change to
  [`0007`](0007-stress-driven-simulation-detail.md)'s scaling claim rather than to this one.
- **A second continuous process arriving.** The asymmetry this rests on is that exactly one sub-model
  is a continuous coupling. A second — pedestrian crowding at a transit platform is the candidate
  `03 §3.7` already names — would mean two sub-step ratios, and at that point the question is whether
  sub-stepping is a general mechanism rather than a Lane-kernel detail.
- **A behavioural event needing finer than 10.5 s resolution.** Nothing in the design has one today.
  If one appears, `TICKS_PER_DAY` rises and the sub-step ratio falls to compensate, leaving the
  vehicle's effective step unchanged — which is the property that makes the two clocks independent and
  is worth testing before it is relied on.
