## S2 R6.1 — the cache key's granularity

- **Captured** 2026-08-08 20:20:24 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** powersave
- **Processors allowed** 2,8 of 12
- **Build** Release

Working rung: 33,018 Segments, 16,697 nodes, 66,036 arcs. Free-flow car costs, `Chebyshev`, no storm and no Epoch — **the key's error is structural and is present on a graph nobody has touched.**

### R6.1a — what a coarser key costs the traveller who shares it

**The trade is stated in `plans/0010` and has never been measured**: *"two Buildings at opposite ends of a long Segment share a route that is wrong for one of them by up to a Segment length."* Every figure below is a whole-journey cost — the arcs **plus both Access Point remainders** — against a flat search on the same graph, which is the quantity both a driver and the Commute Budget actually consume.

| O-D rung | Key | Mean detour | p90 | Worst | Mean, Ticks | Worst, Ticks | Sample |
|---|---|---:|---:|---:|---:|---:|---:|
| uniform | `node-a` | 1.84% | 3.85% | 37.83% | 0.93 | 11.61 | 2,019 |
| uniform | `nearest-node` | 0.80% | 1.84% | 23.77% | 0.41 | 7.52 | 2,028 |
| uniform | `best-endpoint` | 0.00% | 0.00% | 2.70% | 0.00 | 1.52 | 2,045 |
| uniform | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,048 |
| decay L=1024 | `node-a` | 3.09% | 6.81% | 106.66% | 0.90 | 11.61 | 2,017 |
| decay L=1024 | `nearest-node` | 1.55% | 3.31% | 128.20% | 0.41 | 7.52 | 2,025 |
| decay L=1024 | `best-endpoint` | 0.00% | 0.00% | 7.43% | 0.00 | 1.52 | 2,040 |
| decay L=1024 | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,048 |
| decay L=256 | `node-a` | 9.70% | 23.77% | 552.95% | 0.86 | 12.21 | 2,011 |
| decay L=256 | `nearest-node` | 4.22% | 10.77% | 128.20% | 0.42 | 7.68 | 2,021 |
| decay L=256 | `best-endpoint` | 0.02% | 0.00% | 9.41% | 0.00 | 1.52 | 2,039 |
| decay L=256 | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,045 |
| monocentric L=512 | `node-a` | 1.91% | 4.20% | 42.55% | 0.94 | 12.32 | 2,008 |
| monocentric L=512 | `nearest-node` | 0.85% | 1.86% | 29.78% | 0.42 | 7.52 | 2,022 |
| monocentric L=512 | `best-endpoint` | 0.00% | 0.00% | 4.02% | 0.00 | 1.63 | 2,038 |
| monocentric L=512 | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,048 |

**The headline is in the two absolute columns and not in the three percentage ones.** `node-a`'s mean error is **0.86–0.94 Ticks** across the whole O-D family, and `nearest-node`'s is **0.41–0.42** — flat to two decimal places while the *percentage* for the same key swings from 1.84% to 9.70%, better than five-fold. **The key's error is bounded by Segment geometry and has nothing to do with the trip distribution**; the percentage is a statement about journey length wearing a statement about the key. **This is R4.1's finding reproduced one layer down** — there, a District-granular detour went 18.52% → 128.82% because the error was fixed in Ticks and the journey was not. Same shape, same cause, different mechanism.

**So a percentage must not be quoted for this key without its rung, and the absolute should be preferred to it.** `plans/0010` already requires the rung to be named beside every figure; what this table adds is that for *this* quantity the rung-invariant number exists and is the better one to carry.

**`node-a` costs exactly twice what `nearest-node` does, on every rung**, and the factor is geometric rather than empirical: node A is an arbitrary end of the Segment, so a traveller pays a half-Segment on average at each end, where choosing the nearer end pays a quarter. **The fix is free** — it is one comparison per Access Point at insert, the key space is unchanged at nodes², and `adr/0012`'s owed amendment can state it in a sentence.

**But the greedy choice is not monotone, and the tail is where it shows.** On decay L=1024 `nearest-node`'s worst reads **128.20%** against `node-a`'s **106.66%** — the coarser key wins that column. *Nearer along the Segment* is not *better for the journey*: the near endpoint can point away from the destination, and then the traveller pays the Segment twice. **A mean improved by 2× and a tail made worse is a trade, not a strict win**, and it is the shape `05 §4` says to look at rather than an average.

**Routes cheaper than the unconstrained optimum: 0.** Forcing a journey through a chosen node can only add cost, so this column must read zero and is printed on the run where it does. A negative detour here would mean the composition was crediting the key with a shortcut the truth search had not found, which is the one way this instrument could be wrong in the direction that flatters its subject.

**Read the `access-point` rows first: they are the control and they must read zero.** They are measured by a second independent search rather than assigned from the truth, so a zero says the composition is right and not that an assignment worked — the argument R5.5.2 makes about its own flat rung. Any non-zero there is an instrument defect and invalidates every other row.

**`node-a` is what the harness implements today, and it is the coarsest key on the ladder**: every Access Point on a Segment is routed through that Segment's node A, however far along it the traveller actually is. Its mean detour runs from **1.84%** on uniform to **9.70%** on decay L=256.

**`best-endpoint` is the floor of the nodes² family and is not implementable as a key**, because choosing the best endpoint pair means already knowing the answer. It is here to separate two explanations that a single coarse row cannot: whether the error is **intrinsic** to keying on nodes, or an artefact of choosing the endpoint badly. The gap between `node-a` and `best-endpoint` is the part a better key could recover; the gap between `best-endpoint` and `access-point` is the part no nodes² key can.

**The absolute columns are the ones the Commute Budget consumes, and they are why this table reports both.** A percentage cannot be judged without a journey length, and `plans/0010` decision 8 records that the Budget's granularity is undecided — an error of a few Ticks is free against a Budget read to the nearest half hour and disqualifying against one read to the minute. `Units.cs` puts the Budget at *order a hundred Ticks*. **This section reports the error and does not judge it**, which is the same handling R1 gave the matrix's own 11.32%; the two are owed the same answer and `plans/0010` says to answer them once.

**Same-Segment pairs are excluded and the sample size is printed per row.** A pair whose origin and destination share a Segment is answered by R3.8's bypass without consulting any cache, so charging a key for it would credit the key with a case it never sees. The control retained 2,048 of 2,048 drawn pairs.

**What this table cannot say is anything about hit rate.** Hit rate is a property of how many *distinct* keys a population of Buildings generates, and **S2 has no Buildings** — it draws Access Points at random offsets on random Segments, so no two pairs share a Segment except by accident. `plans/0010`'s *"the five Buildings sharing a Segment share one entry instead of minting five"* is a statement about a population this spike does not have. Measuring it needs an invented Buildings-per-Segment pool, and **an invented pool must be swept or its level is a guess wearing a measurement's clothes** — R5.3's debt, in the same words.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 23.80 s — from 20:20:24 UTC to 20:20:48 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.46 / 1.69 / 1.80 (1 / 5 / 15 min)
- **Load average, at end** 1.42 / 1.67 / 1.79 (1 / 5 / 15 min)
- **CPU stall** 23,557 µs over the run — 0.09% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 6,545 µs over the run — 0.02% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
