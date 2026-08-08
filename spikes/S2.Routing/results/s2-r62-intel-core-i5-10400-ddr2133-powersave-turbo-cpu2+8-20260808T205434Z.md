## S2 R6.2 — the eviction policy, and who is to blame for a miss

- **Captured** 2026-08-08 20:54:34 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** powersave
- **Processors allowed** 2,8 of 12
- **Build** Release

**No eviction policy is stated anywhere in the corpus.** `adr/0012` permits caching and says nothing about what leaves; `adr/0017` shows the pattern — fixed capacity, least-used eviction — and nobody has written it down for routes. `RouteCache` implements neither: it is **direct-mapped with one slot**, and an insert whose slot is taken simply overwrites.

**This section reports blame rather than a rate**, because a hit percentage cannot tell a cache that is too small from one throwing away entries it still holds, and those want opposite repairs. *Cold* is a first reference and is unavoidable. *Capacity* is a miss a perfect cache of the same size would also have taken. **Conflict is a miss a perfect cache of the same size would have avoided, and it is the only column that is a defect.** 1,024 entries, 16,384 Trips drawn with repetition.

| Pool | Load | Scheme | Hit | Cold | Capacity | **Conflict** | Mean probes |
|---|---|---|---:|---:|---:|---:|---:|
| 256 | 0.25× | `direct`, modulo — **shipped** | 87.6% | 1.5% | 0.0% | **10.7%** | 1.00 |
| 256 | 0.25× | `direct`, high bits | 89.6% | 1.5% | 0.0% | **8.8%** | 1.00 |
| 256 | 0.25× | 2-way LRU | 94.9% | 1.5% | 0.0% | **3.4%** | 2.00 |
| 256 | 0.25× | 4-way LRU | 97.3% | 1.5% | 0.0% | **1.0%** | 4.00 |
| 256 | 0.25× | 8-way LRU | 98.4% | 1.5% | 0.0% | **0.0%** | 8.00 |
| 256 | 0.25× | fully associative — *bound* | 98.4% | 1.5% | 0.0% | **0.0%** | 1024.00 |
| 512 | 0.50× | `direct`, modulo — **shipped** | 76.7% | 3.1% | 0.0% | **20.0%** | 1.00 |
| 512 | 0.50× | `direct`, high bits | 75.6% | 3.1% | 0.0% | **21.1%** | 1.00 |
| 512 | 0.50× | 2-way LRU | 86.1% | 3.1% | 0.0% | **10.6%** | 2.00 |
| 512 | 0.50× | 4-way LRU | 92.9% | 3.1% | 0.0% | **3.8%** | 4.00 |
| 512 | 0.50× | 8-way LRU | 95.4% | 3.1% | 0.0% | **1.4%** | 8.00 |
| 512 | 0.50× | fully associative — *bound* | 96.8% | 3.1% | 0.0% | **0.0%** | 1024.00 |
| 1,024 | 1.00× | `direct`, modulo — **shipped** | 61.4% | 6.2% | 0.0% | **32.2%** | 1.00 |
| 1,024 | 1.00× | `direct`, high bits | 61.0% | 6.2% | 0.0% | **32.6%** | 1.00 |
| 1,024 | 1.00× | 2-way LRU | 69.0% | 6.2% | 0.0% | **24.7%** | 2.00 |
| 1,024 | 1.00× | 4-way LRU | 77.4% | 6.2% | 0.0% | **16.2%** | 4.00 |
| 1,024 | 1.00× | 8-way LRU | 82.5% | 6.2% | 0.0% | **11.2%** | 8.00 |
| 1,024 | 1.00× | fully associative — *bound* | 93.7% | 6.2% | 0.0% | **0.0%** | 1024.00 |
| 2,048 | 2.00× | `direct`, modulo — **shipped** | 40.6% | 12.5% | 29.8% | **16.9%** | 1.00 |
| 2,048 | 2.00× | `direct`, high bits | 40.2% | 12.5% | 30.0% | **17.1%** | 1.00 |
| 2,048 | 2.00× | 2-way LRU | 44.3% | 12.5% | 30.7% | **12.3%** | 2.00 |
| 2,048 | 2.00× | 4-way LRU | 47.0% | 12.5% | 31.7% | **8.7%** | 4.00 |
| 2,048 | 2.00× | 8-way LRU | 47.6% | 12.5% | 33.8% | **6.0%** | 8.00 |
| 2,048 | 2.00× | fully associative — *bound* | 47.9% | 12.5% | 39.5% | **0.0%** | 1024.00 |
| 512, 8 sites | 0.50× | `direct`, modulo — **shipped** | 65.6% | 3.1% | 0.0% | **31.2%** | 1.00 |
| 512, 8 sites | 0.50× | `direct`, high bits | 75.1% | 3.1% | 0.0% | **21.7%** | 1.00 |
| 512, 8 sites | 0.50× | 2-way LRU | 87.0% | 3.1% | 0.0% | **9.8%** | 2.00 |
| 512, 8 sites | 0.50× | 4-way LRU | 92.7% | 3.1% | 0.0% | **4.1%** | 4.00 |
| 512, 8 sites | 0.50× | 8-way LRU | 96.0% | 3.1% | 0.0% | **0.8%** | 8.00 |
| 512, 8 sites | 0.50× | fully associative — *bound* | 96.8% | 3.1% | 0.0% | **0.0%** | 1024.00 |

**At R5.3's own rung — 512 pairs into 1,024 entries — the shipped scheme's misses are 20.0% conflict and 0.0% capacity.** Every one of them is a lookup a perfect cache **of the same size** would have served. **R5.3's 28–31% floor is not a property of cache size and never was**, and reading it as one is what made it look like a fact of life rather than a bug with a fix.

**Associativity is the lever, and four ways is where it stops paying.** At the same rung conflict falls 20.0% → 10.6% → 3.8% → 1.4% across 1, 2, 4 and 8 ways, against a fully-associative bound of 0.0%. **Four ways recovers most of the gap at four probes**, and the probes are contiguous — on the cache line an entry already occupies, close to free. This is `adr/0017`'s fixed-capacity least-used pattern, sized, and it is the first number the corpus has for it.

**The index function is not the lever, and this section predicted that it would be.** The hypothesis on file was that `RouteCache.Slot` multiplies by the golden-ratio constant — driving entropy upward — and then takes `% capacity`, which reads the low bits, so the modulo discards exactly what the multiply created. **Measured, that is wrong on random keys**: high-bit indexing reads 21.1% against modulo's 20.0% at 0.50× load and 32.6% against 32.2% at 1.00× — **level or slightly worse.** A route key is already a pair of well-spread node ids, so there is no structure in the low bits for the modulo to expose.

**Where it does help is exactly where R6.1b found the damage: structured keys.** On the eight-destination pool, high-bit indexing takes conflict 31.2% → 21.7%, and four ways takes it to 4.1%. **So the index function is a robustness fix rather than a throughput one** — it costs nothing and it is what stops a concentrated city falling off a cliff the uniform draw never shows. **Both changes are worth making and only one of them shows up in the average case**, which is the argument for measuring the concentrated rung at all.

**One honest limit on that row.** R6.1b's worst case — 15.9% hit — was the `access-point` key, and this table keys on `nearest-node` throughout, where the same pool reads 65.6%. The conflict column is clearly elevated against the unconcentrated rung (31.2% against 20.0% at identical load), so the mechanism is confirmed. **The magnitude is not**, and this table does not reproduce R6.1b's extreme.

**Load is the axis R5.3 never swept, and it dominates everything above.** Conflict at four ways runs 1.0% → 3.8% → 16.2% across 0.25×, 0.50× and 1.00×, and **capacity misses appear only at 2.00×**, where they reach 29.8%. R5.3 measured one load and called the result a floor; **it is a point on a curve that triples.**

**What this section does not depend on.** Every figure is a *conditional* claim — given a lookup that repetition would have made a hit, whose fault is it that it was not? — so it survives the cache's absolute hit rate being unmeasurable until Trip generation exists. **The cold column is the part hostage to the invented pool** and it is reported separately rather than folded in, so a reader can see which half of the table moves with the invention. The conflict column does not.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 0.49 s — from 20:54:34 UTC to 20:54:34 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.65 / 1.85 / 2.04 (1 / 5 / 15 min)
- **Load average, at end** 1.65 / 1.85 / 2.04 (1 / 5 / 15 min)
- **CPU stall** 4,148 µs over the run — 0.83% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 0 µs over the run — 0.00% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
