## S2 R5.6 — the Parking Shed, and the rung it disagrees with

- **Captured** 2026-08-09 00:15:34 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** powersave
- **Processors allowed** 2,8 of 12
- **Build** Release

**The second Epoch consumer, and `plans/0010` calls it the one the ladder is most likely to be decided by.** It scales with **Buildings** rather than with routes, and a shed is a *neighbourhood* rather than a *path* — so what *"my Segments"* even means is a choice, which is why there are **four rungs here where routes had three**. **R6.2 recommended 4-way LRU on routes alone**, which is the exact thing `plans/0010` warned against.

### R5.6a — what a shed actually is

| Walk radius | Sheds | Build | Bins found | Ball Segments | Path Segments | Empty |
|---:|---:|---:|---:|---:|---:|---:|
| 200 m | 159,825 | 1.76 µs | 22 | 4 | 1 | 0 |
| 400 m | 159,825 | 1.59 µs | 110 | 22 | 2 | 0 |
| 800 m | 159,825 | 4.37 µs | 596 | 122 | 2 | 0 |

**A shed is not a path, and the two witness columns are the whole argument.** At 400 m a shed's walk ball explores **22 Segments** while the walks to the Bins it keeps touch **2**. A route's witness is the arcs it drives and it stores them anyway; **a shed's conservative witness is 11.00× its own answer**, and it is a structure the shed has no other reason to carry.

**Rebuilding every shed in the city costs 255.560 ms** at 1.59 µs each. That is the figure every row below is denominated in, and it is why the global rung is a question about the Tick budget rather than about cache hygiene.

### R5.6b — the storm, and the stampede

At **400 m** — 159,825 sheds, 1.59 µs to rebuild one. Gestures are R5's own, so the two sections compare directly. Each row is the mean over 24 gestures.

| Gesture | Asked | Got | Rung | Sheds invalidated | Share | Rebuild at the edit | Of a Tick |
|---|---:|---:|---|---:|---:|---:|---:|
| drag | 1 | 1 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| drag | 1 | 1 | per-cluster (8) | 1,273 | 0.7% | 2.035 ms | **13.04%** |
| drag | 1 | 1 | per-cluster (16) | 3,660 | 2.2% | 5.852 ms | **37.51%** |
| drag | 1 | 1 | per-Segment (ball) | 110 | 0.0% | 0.175 ms | **1.12%** |
| drag | 1 | 1 | per-Segment (paths) | 10 | 0.0% | 0.015 ms | **0.10%** |
| drag | 16 | 16 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| drag | 16 | 16 | per-cluster (8) | 2,804 | 1.7% | 4.483 ms | **28.74%** |
| drag | 16 | 16 | per-cluster (16) | 6,662 | 4.1% | 10.652 ms | **68.28%** |
| drag | 16 | 16 | per-Segment (ball) | 590 | 0.3% | 0.943 ms | **6.04%** |
| drag | 16 | 16 | per-Segment (paths) | 119 | 0.0% | 0.190 ms | **1.21%** |
| drag | 256 | 199 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| drag | 256 | 199 | per-cluster (8) | 16,047 | 10.0% | 25.659 ms | **164.48%** |
| drag | 256 | 199 | per-cluster (16) | 25,776 | 16.1% | 41.215 ms | **264.20%** |
| drag | 256 | 199 | per-Segment (ball) | 5,072 | 3.1% | 8.110 ms | **51.98%** |
| drag | 256 | 199 | per-Segment (paths) | 1,459 | 0.9% | 2.332 ms | **14.95%** |
| scattered | 1 | 1 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| scattered | 1 | 1 | per-cluster (8) | 1,273 | 0.7% | 2.035 ms | **13.04%** |
| scattered | 1 | 1 | per-cluster (16) | 3,660 | 2.2% | 5.852 ms | **37.51%** |
| scattered | 1 | 1 | per-Segment (ball) | 110 | 0.0% | 0.175 ms | **1.12%** |
| scattered | 1 | 1 | per-Segment (paths) | 10 | 0.0% | 0.015 ms | **0.10%** |
| scattered | 16 | 16 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| scattered | 16 | 16 | per-cluster (8) | 18,693 | 11.6% | 29.890 ms | **191.60%** |
| scattered | 16 | 16 | per-cluster (16) | 47,033 | 29.4% | 75.205 ms | **482.08%** |
| scattered | 16 | 16 | per-Segment (ball) | 1,768 | 1.1% | 2.827 ms | **18.12%** |
| scattered | 16 | 16 | per-Segment (paths) | 169 | 0.1% | 0.270 ms | **1.73%** |
| scattered | 256 | 256 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| scattered | 256 | 256 | per-cluster (8) | 131,829 | 82.4% | 210.794 ms | **1351.24%** |
| scattered | 256 | 256 | per-cluster (16) | 158,025 | 98.8% | 252.681 ms | **1619.75%** |
| scattered | 256 | 256 | per-Segment (ball) | 25,898 | 16.2% | 41.410 ms | **265.45%** |
| scattered | 256 | 256 | per-Segment (paths) | 2,547 | 1.5% | 4.072 ms | **26.10%** |
| arterial | 1 | 1 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| arterial | 1 | 1 | per-cluster (8) | 1,797 | 1.1% | 2.873 ms | **18.41%** |
| arterial | 1 | 1 | per-cluster (16) | 5,191 | 3.2% | 8.300 ms | **53.20%** |
| arterial | 1 | 1 | per-Segment (ball) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 1 | 1 | per-Segment (paths) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 16 | 3 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| arterial | 16 | 3 | per-cluster (8) | 3,669 | 2.2% | 5.866 ms | **37.60%** |
| arterial | 16 | 3 | per-cluster (16) | 9,853 | 6.1% | 15.754 ms | **100.99%** |
| arterial | 16 | 3 | per-Segment (ball) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 16 | 3 | per-Segment (paths) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 256 | 3 | global | 159,825 | 100.0% | 255.560 ms | **1638.20%** |
| arterial | 256 | 3 | per-cluster (8) | 3,669 | 2.2% | 5.866 ms | **37.60%** |
| arterial | 256 | 3 | per-cluster (16) | 9,853 | 6.1% | 15.754 ms | **100.99%** |
| arterial | 256 | 3 | per-Segment (ball) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 256 | 3 | per-Segment (paths) | 0 | 0.0% | 0.000 ms | **0.00%** |

### What it costs to be able to ask

**A rung is not free merely because it invalidates less.** Checking *did any of my Segments move* needs a reverse index the shed would not otherwise hold, and that is the column routes never had to pay.

| Rung | Reverse index | Resident |
|---|---:|---:|
| per-cluster (8) | 301,599 entries | 1.15 MiB |
| per-cluster (16) | 222,536 entries | 0.84 MiB |
| per-Segment (paths) | 319,655 entries | 1.21 MiB |
| per-Segment (ball) | 3,608,241 entries | 13.76 MiB |

**The global rung is out, and the tripwire fired as written.** One deleted Segment anywhere invalidates all 159,825 sheds, and rebuilding them costs **255.560 ms — 1638.20% of a Tick.** `plans/0010` predicted it in words before this harness existed. **The number is worse than the sentence**, because the rebuild is paid *on arrival* — the moment a Trip is trying to finish — so it is not one stall but a stampede spread across every arriving vehicle. **`05 §3`'s *invalidated by the Road Graph Epoch* is owed the correction `CONTEXT.md` → Epoch already took**: the phrase says when the rebuild is paid, not how much survives, and under one counter the answer is none of it.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 2.46 s — from 00:15:34 UTC to 00:15:37 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.13 / 1.62 / 1.62 (1 / 5 / 15 min)
- **Load average, at end** 1.12 / 1.61 / 1.61 (1 / 5 / 15 min)
- **CPU stall** 5,663 µs over the run — 0.22% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 861 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
