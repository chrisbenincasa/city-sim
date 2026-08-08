## S2 R6.3 — routing's Tick bill, with both consumers in it

- **Captured** 2026-08-08 21:58:53 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** powersave
- **Processors allowed** 2,8 of 12
- **Build** Release

**Every budget figure in this corpus counts Trip starts.** R3's tripwire counts them and `plans/0013`'s routing row counts them. But `adr/0046` introduced **Sight**, R8 measured **1,269.51 diversions per Tick at 40,000 Travellers**, and `adr/0047` then deleted the next-hop table — the one path source that served a diversion cheaply. **Nobody has multiplied those together.** This section does.

### The cost basis, measured in this process

| Event | Cost | Mean arcs |
|---|---:|---:|
| Whole-journey search — a **Habit formation** | 439.17 µs | 58 |
| Remainder search from mid-journey — a **diversion** | 105.70 µs | 28 |
| Cache hit — lookup and compare | 39 ns | — |
| *the same whole-journey loop, measured last* | *453.72 µs* | — |

**The denominator is taken twice**, first and last, because R3 found that a denominator measured first carries a systematic error and instructed R4, R5 and R6 to do the same. The two readings are 439.17 µs and 453.72 µs; **every ratio below uses the last.**

**The diversion search is shorter than a whole journey and that is the point of pricing it separately** — a Traveller diverting halfway has half a journey left. R8.6 captured its sites from a live fleet; this takes the midpoint node of a drawn journey, which is the same shape and is stated rather than passed off as the same measurement.

### The bill

Diversions are applied at R8.3's measured intensity — **1,269.51 per Tick at 40,000 Travellers**, carried as a per-Traveller rate. **The in-flight rungs are S0a's own band for a 1,000,000-population city** — 37,000 / 56,000 / 111,000, swept because the mean Trip duration behind them is unmeasured — plus R8.3's 40,000. **None of them is a small city.** Habit formations are `Travellers / (lifetime × 8,192 Ticks)`. The budget is **15.600 ms**, itself unratified — `plans/0013`.

| In flight | Habit lifetime | Formations/Tick | Diversions/Tick | Formation bill | Diversion bill | **Total, of budget** |
|---:|---:|---:|---:|---:|---:|---:|
| 37,000 | 1 d | 4.516 | 1,174 | 2.049 ms | 124.094 ms | **808.61%** |
| 37,000 | 7 d | 0.645 | 1,174 | 0.292 ms | 124.094 ms | **797.35%** |
| 37,000 | 30 d | 0.150 | 1,174 | 0.068 ms | 124.094 ms | **795.91%** |
| 40,000 | 1 d | 4.882 | 1,269 | 2.215 ms | 134.135 ms | **874.04%** |
| 40,000 | 7 d | 0.697 | 1,269 | 0.316 ms | 134.135 ms | **861.87%** |
| 40,000 | 30 d | 0.162 | 1,269 | 0.073 ms | 134.135 ms | **860.31%** |
| 56,000 | 1 d | 6.835 | 1,777 | 3.101 ms | 187.832 ms | **1223.93%** |
| 56,000 | 7 d | 0.976 | 1,777 | 0.442 ms | 187.832 ms | **1206.89%** |
| 56,000 | 30 d | 0.227 | 1,777 | 0.102 ms | 187.832 ms | **1204.71%** |
| 111,000 | 1 d | 13.549 | 3,522 | 6.147 ms | 372.282 ms | **2425.83%** |
| 111,000 | 7 d | 1.935 | 3,522 | 0.877 ms | 372.282 ms | **2392.05%** |
| 111,000 | 30 d | 0.451 | 3,522 | 0.204 ms | 372.282 ms | **2387.73%** |

**The two consumers are not the same order of magnitude and nothing in the corpus says so.** At R8's own rung — 40,000 Travellers, a 7-Day Habit — formations cost 0.316 ms and diversions cost 134.135 ms, which is **424.15× the formation bill and 99.76% of routing's total**. Every budget figure the corpus publishes counts only the other one.

**Habit is doing exactly what `adr/0046` claims.** A Citizen that computes a route once and keeps it for a week costs well under one search per Tick across a whole fleet — R3's *85 Trip starts* is not the binding constraint and never was, because a Trip start under static Habit is a **lookup**. The constraint is the thing the same ADR introduced in its next paragraph.

### The feasible region, inverted

Published R3's way — a threshold on a quantity, not a multiple over a guess — because the diversion rate is the number most likely to move and the least likely to be measured soon.

| Policy | Cost per diversion | Diversions/Tick that fit | R8's rate is |
|---|---:|---:|---|
| **re-search** — what the corpus specifies today | 105.70 µs | 147 | **over**, by 8.63× |
| *the same, on R8.6's own diversion cost* | *485.50 µs* | *32* | *over, by 39.65×* |
| cache-served | **no hit rate exists to price this** | — | *see below* |
| **rejoin** the Habit Route, no search — *unproposed* | — | unbounded | **free — no search at all** |

**The two ends of the basis disagree by 4.59× and the conclusion survives both.** This section's remainder search is the optimistic end — a midpoint site on a graph nobody is bulldozing. R8.6's 485.50 µs is the pessimistic end, taken on sites a live fleet actually diverted at, and it is close to a *whole-journey* cost rather than half of one, which is itself worth someone's attention. **Between 32 and 147 diversions per Tick fit. R8 measured 1,269.**

**`adr/0046` and `adr/0047` are not jointly affordable under the policy the corpus currently specifies.** Sight makes diversion routine; `adr/0047` removed the only thing that served one cheaply; and a re-search per diversion is over the whole Tick budget at R8's own measured rate, under the optimistic basis, **at every rung of the in-flight band S0a derived for the target city** — not at some extrapolated future size. The band's own midpoint is 56,000.

#### What the cache would have to do, since nobody may quote what it does

**R6.2 says in terms that no document may cite a hit rate from it** — it reports *blame* precisely because the absolute rate rests on Trip repetition that needs `06` milestone 5b. So this row is not priced. It is **inverted**: a cached diversion costs `hit + miss-rate × search`, and the question is what miss rate makes it fit.

| In flight | Diversions/Tick | Required hit rate, optimistic basis | Required hit rate, R8.6's basis |
|---:|---:|---:|---:|
| 37,000 | 1,174 | **87.5%** | **97.3%** |
| 40,000 | 1,269 | **88.5%** | **97.5%** |
| 56,000 | 1,777 | **91.8%** | **98.3%** |
| 111,000 | 3,522 | **95.9%** | **99.1%** |

**And R6.1b is the reason those are not attainable.** A diversion is keyed on *(wherever I now am → destination)*, and a mid-journey position is an arbitrary point along a route rather than a Building. R6.1b established that a coarse key collapses **nothing** unless trips coincide at *both* ends — and diversion origins coincide far less than trip origins do, because they are wherever congestion happened to be. **The cache is being asked for its best case on its worst input.**

**Three levers, and two of them are Ruleset numbers that are already unset.** Raise the **Temperament** threshold so fewer drivers act on what Sight shows them; shrink the **Sight Horizon** toward its 1-Segment floor; or take the last row — **let a diversion rejoin the Habit Route without re-searching**, which is what a driver with a map in their head actually does, and which nothing in the corpus proposes. The third is free by construction and is a design question rather than a tuning one.

**What this section does not do is pick one.** `05 §4` says a different route is a different city, and all three change which route a Traveller takes — so this is session **M**'s to answer and not a benchmark's. What R6.3 supplies is that **it must be answered**, which the corpus did not previously know, because the two facts sat in different documents and were never multiplied.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 0.99 s — from 21:58:53 UTC to 21:58:54 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.43 / 1.70 / 1.50 (1 / 5 / 15 min)
- **Load average, at end** 1.43 / 1.70 / 1.50 (1 / 5 / 15 min)
- **CPU stall** 1,318 µs over the run — 0.13% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 184 µs over the run — 0.01% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
