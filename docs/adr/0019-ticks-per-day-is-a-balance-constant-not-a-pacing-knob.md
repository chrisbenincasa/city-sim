# Ticks per Day is a balance constant, not a pacing knob

**`TICKS_PER_DAY = 8192`, fixed at world creation and baked into the save.** The reference tick rate is **16 Ticks per real second**, making a Day **8m32s** at the default speed. Pacing is delivered entirely by the host's speed ladder, which the simulation never sees. Traffic pressure is tuned with vehicle speed and city grain — **never** with the length of the Day.

This ADR exists because `TICKS_PER_DAY` looks like a pacing setting and is not one. That misreading is easy, natural, and would silently rebalance the entire traffic model.

## The units, and which of them are real

The simulation core contains exactly two quantities relating to time and space: an integer **Tick** counter and integer **Tile** coordinates. There are no seconds in the library and no metres. A vehicle's speed is *Tiles per Tick*. A commute is *N Ticks*.

Seconds and metres are supplied by two exchange rates, both invented outside the simulation and both free:

| Exchange rate | Chosen by | Effect on simulation |
|---|---|---|
| Ticks → real seconds | the host, when it decides how often to call `step()` | none |
| Tiles → metres | the artist, when deciding how big to draw a building | none |

The second is more purely fictional than it looks: declaring a Tile to be 4 m rather than 8 m requires redrawing everything half as large, and the screen is identical. The metre is a number in a wiki. Nothing reads it.

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

> car-following resolution → floor on Ticks per commute → floor on Ticks per Day → *and only then* a Day length in real seconds, chosen freely afterwards.

**The traffic model is upstream of the clock.** Everything else in the simulation is a passenger on a clock it did not ask for.

## Why shortening the Day is not a pacing change

A commute takes 480 Ticks because of distance and speed. It does not know what a Day is and does not change when the Day changes.

Halving `TICKS_PER_DAY` therefore does not shorten the drive — it shortens **the life around the unchanged drive**. The commute goes from 11.7% of a Citizen's day (both directions) to 23%. And "share of life in transit" is the same quantity as "share of the population on the road at any instant."

**Same city, same population, same roads, twice the vehicles.** Congestion arrives twice as early, the Microscopic Segment budget is exhausted twice as fast, and every road needs to be twice as wide. That is a different game, not a faster one.

## Why we do not compensate by doubling vehicle speed

Preserving the ratio by halving both is sound reasoning and it works — **once**. At roughly 65 km/h, where safe following distance is ~36 m:

| Ticks/Day | Distance per Tick | Share of following distance | Verdict |
|---|---|---|---|
| 8192 | 4.2 m | 12% | comfortable |
| 4096 | 8.4 m | 23% | works; queues get blockier |
| 2048 | 16.8 m | 47% | SUMO's documented failure zone |

So 4096 with doubled vehicle speed is a real option, not an impossible one. We decline it because of what it costs rather than because it cannot be done:

- **Visual honesty.** Apparent speed is car-lengths per second — dimensionless, so the metres-per-Tile art scale cannot rescue it. At the current figures traffic reads as ~65 km/h at Study speed and ~130 km/h at Normal. Doubling vehicle speed doubles both, and we lose the property that **the game looks true at exactly the speed where you slow down to inspect it**.
- **Queue fidelity.** 12% → 23% of following distance per Tick coarsens shockwave propagation and stop-and-go waves — which is the phenomenon Microscopic Segments exist to show. Degrading it buys pacing with the currency that pacing was supposed to protect.
- **Within-Day scheduling resolution** halves, chunking the rush-hour departure spread.

And what it buys — a Day half as long in real seconds — **the speed ladder already provides for free**. Never pay in fidelity for something available free in another currency.

## The speed ladder

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

**Performance.** The ratio fixes the vehicle count, and the vehicle count is the expensive thing. If profiling shows 8192 Ticks/Day of vehicles-in-flight is unaffordable, then 4096 with doubled vehicle speed is not a compromise — it is the correct response, because it halves Tick throughput while changing neither the ratio nor the traffic load. The price is the two fidelity costs enumerated above, and they should be paid consciously.

Nothing about pacing, session length, or "days feel too long" should ever reopen this. Those live in the speed ladder.
