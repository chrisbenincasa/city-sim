# Ticks per Day is a balance constant, not a pacing knob

> **⚠ AMENDED A SECOND TIME 2026-08-13 by [`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md) — and this time the *conclusion* falls. `TICKS_PER_DAY = 2048`.**
>
> **The title's claim is withdrawn.** Under `0082`'s clock a Tick's duration is **derived from**
> `TICKS_PER_DAY`, so a commute's length in Ticks scales with the constant and the ratio this ADR calls
> *"the only time-related quantity in the design that is about the world"* is **invariant** — 1.39% of a
> Day at 8192 and 1.39% at 2048. `TICKS_PER_DAY` is a **sampling rate**, and lowering it buys four times
> as much world per real second at no cost to any balance.
>
> **Four things below are struck.** *Why shortening the Day is not a pacing change* in its entirety,
> including **"same city, same population, same roads, twice the vehicles"** — the premise it needs is a
> Tick with a duration fixed independently of the Day, which is what this ADR believed and what `0082`
> replaced. The *"free in another currency"* claim closing *Why we do not compensate*: the speed ladder
> buys world by running **more Ticks**, and a Tick costs what a Tick costs, so the two are
> interchangeable only for what is Tick-denominated. The **speed ladder table**'s Day column, superseded
> by `01 §1`'s five-rung ladder. And the revisit trigger's **direction**, which is now backwards — see
> the banner there.
>
> **What survives is the prohibition and the third bullet.** *Traffic pressure has two legitimate levers
> and one illegitimate one* still binds: the Day must never be used to tune a system-wide **outcome**,
> and `0094` does not — it changes how fast the world is stepped through, not what the world does. And
> *within-Day scheduling resolution*, which `0082` promoted from makeweight to sole criterion, is the
> whole of the argument `0094` had to make.
>
> **State Hashes move and all three golden baselines are re-recorded**, which is the difference from the
> first amendment.

> **AMENDED 2026-08-12 by [`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md), on measurement. The decision stands and the derivation does not.**
>
> **`TICKS_PER_DAY = 8192` survives, and so does everything this ADR concludes about pacing.** What is
> replaced is the chain that reaches it. This ADR makes the Tick a **car-following** unit — 4.2 m per
> Tick at ~65 km/h, which is **0.2326 s** — and then calls 8192 of them a Day, which makes a Day
> **31.8 minutes**. `0082` makes the Tick a **behavioural** unit: a Day is 24 in-world hours and a Tick
> is **10.546875 s**, which is what `Speed.cs` has shipped all along and what `--trips` reports a real
> pedestrian walking at. The two are **45.3× apart** and both constraints behind them are real, so
> car-following takes an integer **sub-step ratio** inside Tick phase 4 instead of setting the global
> clock.
>
> **Three things below are struck, and each is marked in place.** The causal chain in *Why the traffic
> model owns the Tick*; the **visual honesty** bullet, whose source column `plans/0012` measured 65×
> adrift; and the *"both free"* exchange-rate claim in *The units, and which of them are real*, which
> the `[roads]` Ruleset table spent without telling anybody. **Within-Day scheduling resolution — listed
> third and looking like a makeweight — is the argument that survives**, and it is now the only thing
> setting `TICKS_PER_DAY`.
>
> **No State Hash moves.** The number was right; only the reason was wrong.

**`TICKS_PER_DAY = 8192`, fixed at world creation and baked into the save.** The reference tick rate is **16 Ticks per real second**, making a Day **8m32s** at the default speed. Pacing is delivered entirely by the host's speed ladder, which the simulation never sees. Traffic pressure is tuned with vehicle speed and city grain — **never** with the length of the Day.

This ADR exists because `TICKS_PER_DAY` looks like a pacing setting and is not one. That misreading is easy, natural, and would silently rebalance the entire traffic model.

## The units, and which of them are real

The simulation core contains exactly two quantities relating to time and space: an integer **Tick** counter and integer **Tile** coordinates. There are no seconds in the library and no metres. A vehicle's speed is *Tiles per Tick*. A commute is *N Ticks*.

Seconds and metres are supplied by two exchange rates, both invented outside the simulation and both free:

| Exchange rate | Chosen by | Effect on simulation |
|---|---|---|
| Ticks → real seconds | the host, when it decides how often to call `step()` | none |
| Tiles → metres | the artist, when deciding how big to draw a building | none |

The second is more purely fictional than it looks: declaring a Tile to be 4 m rather than 8 m requires redrawing everything half as large, and the screen is identical. ~~The metre is a number in a wiki. Nothing reads it.~~

> **⚠ STRUCK by [`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md). Both rates were free when this was written and neither is now, and no edit to this file made it false.** The `[roads]` Ruleset table authors speeds in **km/h**; `05 §26` fixes a Tile at **~4 m**; `02 §2` mandates that speed be stored in **Tiles per Tick**. Those three determine the fourth, so **a Tick's duration in seconds is derived rather than chosen** — and the metre is read, by `Speed.FromKilometresPerHour`, on every Ruleset load. There is **one** degree of freedom across the four quantities and the corpus has been spending it three times independently. ***A degree of freedom is spent by the first document that uses it, and nothing announces the spending.***

**`TICKS_PER_DAY` is not a third exchange rate.** A Day is not an external unit being converted to — it is a *simulation object*, the period of a Household's routine. So `TICKS_PER_DAY` states how many Ticks that cycle takes, which puts it inside the simulation alongside "a commute takes 480 Ticks." Two durations both measured in Ticks have a **ratio**, and that ratio is a real, dimensionless fact about the world:

```
480 Ticks (commute) ÷ 8192 Ticks (Day) = 5.9% of a life spent driving, one way
```

That number survives every exchange rate. It is the only time-related quantity in the design that is about the world rather than about how we are looking at it.

Everyday intuition fails here because reality welds these together: you cannot shorten your day without shortening your life. In a simulation they are separate dials, and it is easy to reach for the wrong one.

## Why the traffic model owns the Tick

Ask what actually needs a fine time base. Households make a handful of decisions per Day. Buildings fire Rules a few times a Day. Map Layers diffuse every 32–64 Ticks. Life Stages advance over dozens of Days.

Every one of those is a **discrete event**, and discrete events can be scheduled at any granularity — that is what the Event Wheel is for. If vehicles did not exist, this simulation could run at **24 Ticks per Day** and nothing would suffer.

Vehicles are the exception. Car-following is the only *continuous* process in the simulation, and continuous processes carry a resolution requirement: a vehicle must not advance far relative to the safe following distance in a single Tick, or a car approaching a stopped queue overshoots before it can react. This is the mechanism behind SUMO's documented "unwanted emergency braking and collisions at higher step-lengths," and behind Treiber's guidance that IDM wants Δt ≤ 0.5 s.

So the causal chain runs one way only:

> ~~car-following resolution → floor on Ticks per commute → floor on Ticks per Day → *and only then* a Day length in real seconds, chosen freely afterwards.~~

~~**The traffic model is upstream of the clock.** Everything else in the simulation is a passenger on a clock it did not ask for.~~

> **⚠ STRUCK and INVERTED by [`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md).** The chain propagates one sub-model's resolution requirement to the global tick rate, and **the arithmetic refuses it**: at 0.2326 s a Tick an in-world Day is **371,420 Ticks**, so holding the same pacing needs a Tick costing **0.192 ms** — against S0a's **0.112 ms empty Tick** and S0b's **8.72 ms with work in it** at 1M. That leaves 0.080 ms for 8.61 ms of measured work, **108× short**, and no optimisation anywhere recovers 45×. *(This ADR's whole derivation predates any Tick figure ever taken over a city.)*
>
> **The chain now reads the other way**: behavioural scheduling resolution → `TICKS_PER_DAY` → a Tick's duration in seconds → **and car-following takes a sub-step ratio inside phase 4**, visible to nothing outside the Lane kernel. **Behaviour is upstream of the clock, and the traffic model is the one process that does not inherit it.** The paragraph below — *"if vehicles did not exist, this simulation could run at 24 Ticks per Day and nothing would suffer"* — is the observation that makes this work, and it is this ADR's own.

## ~~Why shortening the Day is not a pacing change~~

> **⚠ STRUCK IN FULL by [`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md).** The section is valid reasoning from a premise `0082` deleted: it needs *"a commute takes 480 Ticks"* to be a fact about the world, and under a Tick whose duration is derived from `TICKS_PER_DAY` it is a fact about the **sampling rate**. A commute is 20 clock minutes; that is 114 Ticks at 8192 and 28 Ticks at 2048, and **1.39% of a Day in both**. The constant cancels. There is no doubling of vehicles, no earlier congestion and no wider road. ***A section can be internally sound and load-bearing on a premise another document removed, and nothing recomputes it when that happens*** — `0089`'s *the obligation a deletion creates is a re-derivation, not a retraction*, third sighting in two days.

~~A commute takes 480 Ticks because of distance and speed. It does not know what a Day is and does not change when the Day changes.~~

~~Halving `TICKS_PER_DAY` therefore does not shorten the drive — it shortens **the life around the unchanged drive**. The commute goes from 11.7% of a Citizen's day (both directions) to 23%. And "share of life in transit" is the same quantity as "share of the population on the road at any instant."~~

~~**Same city, same population, same roads, twice the vehicles.** Congestion arrives twice as early, the Microscopic Segment budget is exhausted twice as fast, and every road needs to be twice as wide. That is a different game, not a faster one.~~

## Why we do not compensate by doubling vehicle speed

Preserving the ratio by halving both is sound reasoning and it works — **once**. At roughly 65 km/h, where safe following distance is ~36 m:

| Ticks/Day | Distance per Tick | Share of following distance | Verdict |
|---|---|---|---|
| 8192 | 4.2 m | 12% | comfortable |
| 4096 | 8.4 m | 23% | works; queues get blockier |
| 2048 | 16.8 m | 47% | SUMO's documented failure zone |

So 4096 with doubled vehicle speed is a real option, not an impossible one. We decline it because of what it costs rather than because it cannot be done:

- ~~**Visual honesty.** Apparent speed is car-lengths per second — dimensionless, so the metres-per-Tile art scale cannot rescue it. At the current figures traffic reads as ~65 km/h at Study speed and ~130 km/h at Normal. Doubling vehicle speed doubles both, and we lose the property that **the game looks true at exactly the speed where you slow down to inspect it**.~~ **⚠ STRUCK by [`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md): the *"reads as"* column it rests on is **65× adrift** of the `Day` column beside it in `02 §1.2`, measured in [`plans/0012`](../../plans/0012-corpus-audit.md). Once a Day is 8m32s of wall clock, everything on screen moves at **168.75×** and there is no freedom left to spend on making a car look like a car. **Appearance is a consequence of the calendar rate, never a constraint on the simulated speed** — and this is the failure [`01`](../01-player-experience.md) diagnoses in Cities: Skylines at 112×, committed in our own table three documents away from the paragraph naming it.**
- ~~**Queue fidelity.**~~ **MOVED by `0082`**, and it is correct where it now lives: 12% → 23% of following distance per Tick coarsens shockwave propagation and stop-and-go waves, which is the phenomenon Microscopic Segments exist to show. **That is a constraint on the sub-step ratio and no longer one on `TICKS_PER_DAY`.** Treiber's Δt ≤ 0.5 s is the ceiling — a ratio of **21** — and this ADR's 12% figure is a ratio of **45**.
- **Within-Day scheduling resolution** halves, chunking the rush-hour departure spread. **⚠ This is the only one of the three left standing, and under `0082` it is the *whole* of what sets `TICKS_PER_DAY`.** *The argument listed third and looking like a makeweight was the load-bearing one all along.*

~~And what it buys — a Day half as long in real seconds — **the speed ladder already provides for free**. Never pay in fidelity for something available free in another currency.~~

> **⚠ STRUCK by [`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md), and it is this ADR's one substantive error of fact.** The speed ladder buys world by running **more Ticks per second**, and a Tick costs what a Tick costs; `TICKS_PER_DAY` buys world by putting more of it **inside** a Tick. They are interchangeable only for what is Tick-denominated, and the twenty-hour marker `01 §4` is built on is denominated in **Days**.

## ~~The speed ladder~~

> **⚠ SUPERSEDED by `01 §1`'s five-rung ladder** (`0094`). The `Day` column is stated at 8192 and the *"traffic reads as"* column was struck by `0082`. The two riders below the table survive whole and are the only part still current.

The host runs Ticks at whatever rate it likes. The simulation is unaffected, because it only ever sees Ticks.

| Speed | Ticks/s | Day | Cross-town trip | Traffic reads as |
|---|---|---|---|---|
| **Study** (½×) | 8 | 17m04s | 60 s | ~65 km/h — **visually honest** |
| **Normal** (1×) — default | 16 | **8m32s** | 30 s | ~130 km/h |
| **Fast** (2×) | 32 | 4m16s | 15 s | time-lapse |
| **Very fast** (4×) | 64 | 2m08s | 7.5 s | time-lapse |

Traffic looks true at exactly the speed at which it is inspected — the same principle as [`0007`](0007-stress-driven-simulation-detail.md), detail arriving where scrutiny does, reappearing on a different axis for free.

Two riders. **64 Ticks/s is Factorio's rate**, so the top rung is a performance budget rather than a design choice. And when the host cannot keep up it must **dilate wall-clock time, never skip Ticks** — Factorio's documented behaviour, and mandatory here because skipping Ticks would break replay and the State Hash.

## Consequences

- **`TICKS_PER_DAY` is a world-creation constant, not hot-reloadable.** Every event on the Event Wheel was scheduled in Ticks against an assumed Day length; changing it mid-save silently reinterprets every pending Life Stage countdown and decline window. See [`0015`](0015-all-tuning-data-is-hot-reloadable.md).
- **`WHEEL_SIZE` is set by the longest common event horizon, not by the Day.** These are independent questions that happen to share an answer: the longest routinely-scheduled event is a work shift or a sleep, both bounded by one Day, so `WHEEL_SIZE = 8192`. That equality is a coincidence worth stating, because the tempting shortcut — sizing the wheel to a fraction of a Day to save memory — silently breaks the wheel's defining property. An entity sleeping longer than `WHEEL_SIZE` wraps and is touched several times instead of once, which is the entire benefit gone in exchange for 64 KB of bucket heads. Multi-Day Life Stage countdowns exceed any wheel and need the overflow tier regardless.
- **The default speed is not 1× in the sense of "the slowest."** Study is half the reference rate and is the speed at which the simulation is visually truthful. A player who never touches the control sees traffic running ~2× fast forever. This is a deliberate, recorded concession: the *mechanics* are identical at every speed, and mechanics are what the player is scored on.
- **Raising the tick rate buys nothing but cost.** It is the same action as raising the speed multiplier. It does not improve car-following fidelity, because that constraint is expressed in Tiles per Tick, not in Hertz.
- **Traffic pressure has two legitimate levers and one illegitimate one.** Slower vehicles per road class (local, visible, already a game concept, and it *improves* visual honesty) and coarser city grain (experienced as geography, actionable by zoning). The Day is the illegitimate one: it is a single hidden global constant that tunes a system-wide outcome with no causal story — structurally the same object as SimCity's RCI scalar, which `00-vision.md` pillar 1 exists to forbid. `LEGIBLE CAUSE`

## What would trigger revisiting

> **⚠ [`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md) gives this trigger a cheaper lever first.** The vehicle count is still the expensive thing and halving `TICKS_PER_DAY` still halves it — but under two clocks the **sub-step ratio** is the dial that prices the Lane kernel, and it moves without touching the calendar, the Event Wheel or a single scheduled event. **Reach for the ratio before the Day.** Only one of the two fidelity costs below is still payable: visual honesty is struck, and queue fidelity is now the ratio's constraint rather than the Day's.

> **⚠ THE DIRECTION BELOW IS BACKWARDS, struck by [`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md).** Under `0082`'s clock, lowering `TICKS_PER_DAY` makes the vehicle cost **worse by the factor it is reached for to improve**. Vehicles in flight is `arrival rate × journey duration`, both invariant in in-world terms, so the population on the road does not move; what moves is the **sub-step ratio**, since car-following needs a fixed in-world Δt and a longer Tick must integrate more sub-steps inside it. Halving the Day doubles the vehicle cost. **This instruction has stood unread since `0082` inverted the chain** — `plans/0012` *Cause 2*, with the write landing in one half of a file and not the other.

~~**Performance.** The ratio fixes the vehicle count, and the vehicle count is the expensive thing. If profiling shows 8192 Ticks/Day of vehicles-in-flight is unaffordable, then 4096 with doubled vehicle speed is not a compromise — it is the correct response, because it halves Tick throughput while changing neither the ratio nor the traffic load. The price is the two fidelity costs enumerated above, and they should be paid consciously.~~

~~Nothing about pacing, session length, or "days feel too long" should ever reopen this. Those live in the speed ladder.~~

> **⚠ STRUCK by `0094`, which reopened it on exactly that ground and was right to.** What survives is the narrower prohibition in the last consequence bullet: the Day must never tune a system-wide **outcome**, because that is a hidden global with no causal story and `00-vision.md` pillar 1 forbids it. Changing how fast the world is stepped through is not an outcome.
