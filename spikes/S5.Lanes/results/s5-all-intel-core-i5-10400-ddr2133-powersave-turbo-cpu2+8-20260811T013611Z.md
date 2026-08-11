# S5 — the Lane kernel

- **Captured** 2026-08-11 01:36:11 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 2 logical processors visible
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** **powersave** — every absolute below is a lower bound on this machine's ability
- **Turbo** enabled
- **Processors allowed** 2,8
- **Build** Release
- **Stopwatch** high resolution, 1000000000 Hz

> `plans/0019-s5-lane-kernel.md`. **S5 does not set the Microscopic Cap.** It measures one side of a ratio — Vehicles affordable in 15.6 ms on one core — whose other side is how many Vehicles a real city stresses at once, and that is milestone 5b's.

## L0 — the fixture, the row, and the denominator

Every figure in this capture is Q16.16 Tiles and Tiles per Tick, through `Borough.Core.Arithmetic.Fixed` including its `checked` narrowing. Nothing here is a `double`, which is the difference between this measurement and the one `adr/0016` quotes.

### The units, and where each comes from

| Quantity | Value | Source |
|---|---:|---|
| Tile | 4 m | Cell = 32×32 Tiles ≈ 128 m |
| Segment | 32 Tiles = 128 m | S2 R0; `CONTEXT` → Segment |
| Lanes per Segment | 4 | `CONTEXT` → Segment |
| Free-flow speed | 1.050 Tiles/Tick | `adr/0019`: 4.2 m per Tick at 8192 Ticks/Day |
| Vehicle length | 1.250 Tiles | 5 m |
| `s0` minimum gap | 0.500 Tiles | Treiber, 2 m |
| `T` desired headway | 6.449 Ticks | Treiber 1.5 s ÷ 0.2326 s/Tick |
| `a` | 0.018 Tiles/Tick² | Treiber 1.4 m/s² |
| `b` | 0.027 Tiles/Tick² | Treiber 2.0 m/s² |
| Jam spacing | 1.750 Tiles | derived, `s0` + length |
| **Vehicles per Lane at a standstill** | **18** | derived, Segment ÷ jam spacing |

**One IDM step per Tick, and no substepping to price.** `adr/0019` derives `TICKS_PER_DAY` *from* car-following resolution and states the row this table uses — 4.2 m per Tick is 12% of the ~36 m safe following distance, against Treiber's Δt ≤ 0.5 s. The Tick *is* the integration step.

### The row

Four `int` columns — position, velocity, desired speed, Traveller id — so **16 bytes per Vehicle**, struct of arrays, one arena. A Segment at a standstill is 72 Vehicles and therefore 1152 bytes of queue.

### The denominator

The same walk over the same arrays with the arithmetic removed: three loads, two stores, the ring bookkeeping. Measured here rather than divided against S4's recorded bandwidth, because S4's figure was taken under a different governor on a different day and dividing across that is a ratio between two machines.

| Lanes | Vehicles | ns/Vehicle | GB/s of Vehicle row |
|---:|---:|---:|---:|
| 64 | 1,152 | 1.24 | 12.83 |
| 1,024 | 18,432 | 1.24 | 12.83 |
| 16,384 | 294,912 | 1.57 | 10.19 |
| 262,144 | 4,718,592 | 1.56 | 10.21 |

The 16,384-Lane row is the one L1 and L2 divide against: **1.57 ns per Vehicle**, at a working set (4,608 KiB) past this machine's L2 and inside its L3.

## L1 — the queue pass

One Tick of IDM car-following down a sorted one-dimensional queue, no Overlaps, no spatial index, no indirection. Every Lane is a ring, so every Vehicle has a leader and the kernel runs in the congested regime the Microscopic tier exists for rather than in free flow.

### Queue length, at a fixed working set

294,912 Vehicles at every rung, redistributed across more or fewer Lanes. A rung that swept the queue length *and* the working set together would report their product and call it the queue length.

| Vehicles/Lane | Lanes | ns/Vehicle | ns/Vehicle (median) | vs L0 |
|---:|---:|---:|---:|---:|
| 4 | 73,728 | 42.05 | 42.94 | 26.78× |
| 8 | 36,864 | 42.34 | 43.34 | 26.96× |
| 18 | 16,384 | 42.33 | 43.13 | 26.96× |
| 32 | 9,216 | 40.74 | 44.64 | 25.95× |
| 128 | 2,304 | 41.78 | 44.69 | 26.61× |
| 512 | 576 | 39.56 | 41.38 | 25.19× |
| 4096 | 72 | 40.08 | 40.30 | 25.52× |

**The rung that counts is 18** — what one Lane of a 128 m Segment holds at a standstill. Every longer rung is a queue no Segment in this design has, and quoting one would be S0b's finding again: a unit cost taken on a fixture the world does not produce.

### Regime

The kernel has three data-dependent branches — the interaction term going negative, the gap ratio hitting its cap, and the velocity flooring at zero — so its cost is not obviously flat across traffic states. Occupancy is Vehicles as a percentage of what the Lane holds at a standstill, at 18 per Lane.

| Occupancy | ns/Vehicle | vs 70% |
|---:|---:|---:|
| 25% | 42.06 | 1.02× |
| 50% | 42.64 | 1.03× |
| 70% | 41.00 | 1.00× |
| 90% | 42.58 | 1.03× |
| 100% | 42.64 | 1.03× |

### Where the time goes

The IDM as written divides **three times per Vehicle per Tick** and two of those denominators never vary — `2√(ab)` is a constant of the Ruleset and `v0` is a constant of the driver. A 64-bit integer division is tens of cycles and does not pipeline; a floating-point division is a handful and does. That is exactly where a transplant from a float engine should be expected to cost, so the two forms are measured rather than argued about. The third division, `s*/s`, has the gap to the vehicle in front as its denominator and no reciprocal exists for it — **this is the floor of what the substitution can buy, not an alternative implementation.**

| Form | Divisions/Vehicle | Row | ns/Vehicle | vs L0 | Vehicles in 15.6 ms |
|---|---:|---:|---:|---:|---:|
| As written | 3 | 16 B | 41.25 | 26.27× | 378,163 |
| Reciprocal | 1 | 20 B | 23.59 | 15.02× | 661,213 |

Removing two of the three divisions moves the pass by 1.74×. **Read this as an attribution and not as a recommendation.** A reciprocal changes the arithmetic, so it changes the State Hash, so under `CLAUDE.md`'s own test it is *a design change however it was motivated* — and the fifth column is state a promotion has to materialise.

## L2 — the network, and the Overlap exchange

Every Lane holds 18 Vehicles — one Lane of a 128 m Segment at a standstill — and the rung is the number of Lanes. This is the sweep that answers the question, because the Microscopic tier's cost is a whole-network cost and a per-queue figure is a laboratory number until a network has produced one.

| Lanes | Vehicles | Segments | Vehicle rows | ns/Vehicle | vs L1 | Vehicles in 15.6 ms |
|---:|---:|---:|---:|---:|---:|---:|
| 16 | 288 | 4 | 4 KiB | 38.62 | 0.91× | 403,925 |
| 64 | 1,152 | 16 | 18 KiB | 38.19 | 0.90× | 408,473 |
| 256 | 4,608 | 64 | 72 KiB | 40.03 | 0.94× | 389,697 |
| 1,024 | 18,432 | 256 | 288 KiB | 57.66 | 1.36× | 270,523 |
| 4,096 | 73,728 | 1,024 | 1,152 KiB | 40.34 | 0.95× | 386,665 |
| 16,384 | 294,912 | 4,096 | 4,608 KiB | 41.12 | 0.97× | 379,340 |
| 65,536 | 1,179,648 | 16,384 | 18,432 KiB | 41.65 | 0.98× | 374,540 |
| 262,144 | 4,718,592 | 65,536 | 73,728 KiB | 42.61 | 1.00× | 366,076 |

**The self-consistent rung is the one whose Vehicle count is closest to the Vehicle count it says fits in a Tick** — here 294,912 Vehicles at 41.12 ns each. Reading any other row as the answer states a cost for a working set the answer does not have.

### The Overlap exchange

`adr/0016` states that Overlapping Lanes exchange their Vehicles' projected positions once per Tick and states no cost for it, because the cost depends on how a Lane finds the Vehicle near the conflict point — a data-structure decision nobody has taken. Both plausible answers are measured. **Scan** walks the partner's queue from its head, which is what a first implementation writes. **Cursor** keeps the queue index found last Tick, which is O(1) amortised and is state promotion must materialise and demotion must discard.

At 16,384 Lanes / 294,912 Vehicles, exchange plus queue pass, against the same rung with no Overlaps at all.

| Overlaps/Lane | Exchange | ns/Vehicle | vs no Overlaps | Vehicles in 15.6 ms |
|---:|---|---:|---:|---:|
| 0 | — | 41.09 | 1.00× | 379,617 |
| 1 | cursor | 45.20 | 1.09× | 345,109 |
| 1 | scan | 47.59 | 1.15× | 327,758 |
| 2 | cursor | 47.32 | 1.15× | 329,656 |
| 2 | scan | 49.32 | 1.20× | 316,288 |
| 4 | cursor | 50.93 | 1.23× | 306,290 |
| 4 | scan | 53.55 | 1.30× | 291,289 |

**Two Overlaps per Lane is the row L4 carries.** A Lane on a four-Lane Segment has a Switch Lane on each side that it is not on the edge of, plus whatever crosses it at the node — so one is optimistic and four is a busy intersection. Nothing in the corpus states this number, and it is not S5's to choose: it is a property of the Road Graph the geometry pass produces, which does not exist.

## L3 — promotion and demotion

`adr/0016` names *"promotion cost dominating the traffic budget"* as a condition that would reopen it and names no instrument. The condition is a ratio, so the answer is one: promotion plus demotion per Vehicle, divided by the cost of running that Vehicle for one Tick, gives a **break-even residency** in Ticks — the number of Ticks a Segment must stay Microscopic for the queue to have been worth materialising.

2,592 Segments of 4 Lanes, 72 in-flight Travellers each, found through an intrusive index list threaded in arrival order rather than in memory order — which is the structure `CLAUDE.md` mandates and is what a promotion will actually walk.

| Conversion | ns/Vehicle | Vehicles in 15.6 ms |
|---|---:|---:|
| Promotion | 43.20 | 361,111 |
| Demotion | 24.69 | 631,783 |
| **Round trip** | **67.89** | 229,776 |

**Break-even residency: 1 Ticks.** Below this, a Segment spends more on changing representation than on being simulated. Against it: a Vehicle crosses a 128 m Segment at free flow in 30 Ticks, so a residency requirement above that means the average promoted Segment is paying for a conversion it does not use.

**150,943 of 186,624 Vehicles had no arrival Tick to convert to** on the last demotion — they were at rest, and `distance / speed` is undefined for them. `03` invariant 3 requires what demotion discards to be enumerated, and this is a class of discard the corpus does not name: not queue position or headway, which are listed, but the arrival time itself. A Segment demoted while jammed cannot say when its Vehicles arrive.

## L4 — the derived product

One core, one 15.6 ms Tick at 4× speed, `adr/0016`'s structure in `adr/0003`'s arithmetic. **S5 supplies one side of a ratio and does not set the Microscopic Cap** — the other side is how many Vehicles a real city stresses at once, which is milestone 5b's and does not exist.

| Quantity | Figure |
|---|---:|
| ns per Vehicle per Tick, no Overlaps | 41.12 |
| ns per Vehicle per Tick, 2 Overlaps per Lane by cursor | 47.32 |
| **Vehicles per Tick per core, no Overlaps** | **379,340** |
| **Vehicles per Tick per core, with Overlaps** | **329,656** |
| Vehicles in a Segment at a standstill | 72 |
| **Microscopic Segments in 15.6 ms, no Overlaps** | **5,268** |
| **Microscopic Segments in 15.6 ms, with Overlaps** | **4,578** |
| Promotion + demotion, ns per Vehicle | 67.89 |
| **Break-even residency** | **1 Ticks** |

### The tripwire, evaluated

Transcribed from `plans/0019`, which stated every threshold before anything ran.

| # | Condition | Reading | Fired? |
|---|---|---:|---|
| T1 | Vehicles/Tick/core below 400,000 — `adr/0016`'s transplanted headline does not survive our arithmetic and our Tick | 329,656 | **FIRED** |
| T2 | Vehicles/Tick/core below 186,624 (2,592 Segments × 72) — `adr/0007`'s *bounded by network stress, not by population* fails at the only adjacent count the corpus holds | 329,656 | no |
| T3 | Queue pass worse than 4× the bare walk — `adr/0016`'s *with the constant of a `memcpy`* is false in the letter | 26.19× | **FIRED** |
| T4 | Break-even residency above 30 Ticks — one Segment traversal at free flow — so `adr/0016`'s own revisit trigger *promotion cost dominating the traffic budget* fires | 1 Ticks | no |
| T5 | Network rung more than 1.50× the fixed-working-set queue rung — the single-queue figure is a fixture number and may not be published as the unit cost | 0.97× | no |

**What a fired tripwire does and does not mean.** T1 firing is a statement about a sentence in `adr/0016`, not about the design: the ADR's structural argument — no spatial index, predecessor is the previous array element, scheduling granularity is the Lane — is untouched by the constant. T2 firing is the one that reaches a decision, and even then it reaches `adr/0007`'s scaling clause rather than the Cap's value, which S5 must not set.

---

**Contention** — 239891 µs of CPU stall accumulated during this run (Linux PSI `cpu total`, end minus start). A run with a quiet window reads near zero.

