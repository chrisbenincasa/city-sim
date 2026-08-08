## S2 R6.1 — the cache key's granularity

- **Captured** 2026-08-08 20:40:47 UTC
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

### R6.1b — the key space, and the population this spike does not have

**`plans/0010` argues the key on hit rate and S2 cannot draw the population the argument is about.** *"Keyed on those, the space is Buildings² ≈ 2.25 × 10¹⁰ and the hit rate is approximately zero. Keyed on the endpoints… the space collapses to nodes² and the five Buildings sharing a Segment share one entry instead of minting five."* That is a claim about **Buildings**, and this spike has none — it draws Access Points at random offsets on random Segments, so two trips share a Segment only by accident. **Buildings are therefore invented here, and swept**, on the rule R5.3 established for its own pool: the *level* of any figure below is a property of the invention, and **only the ratio between key rungs under one pool may be quoted.**

Buildings are placed evenly along each Segment and a drawn Access Point is **snapped** to the nearest one, so the O-D shape is the swept family's and only the offsets are the invention. 512 distinct pairs, 4,096 trips drawn from them with repetition, a 1,024-entry direct-mapped cache — **R5.3's shape exactly**, so the hit columns are comparable with it.

| O-D rung | Buildings / Segment | Key | Distinct keys | of pairs | Collapse | Hit | Resident | Evictions |
|---|---:|---|---:|---:|---:|---:|---:|---:|
| uniform | 1 | `node-a` | 512 | 512 | 1.00× | 70.3% | 411 | 803 |
| uniform | 1 | `nearest-node` | 512 | 512 | 1.00× | 70.3% | 411 | 803 |
| uniform | 1 | `access-point` | 512 | 512 | 1.00× | 72.3% | 407 | 726 |
| uniform | 5 | `node-a` | 512 | 512 | 1.00× | 70.3% | 411 | 803 |
| uniform | 5 | `nearest-node` | 512 | 512 | 1.00× | 70.3% | 403 | 813 |
| uniform | 5 | `access-point` | 512 | 512 | 1.00× | 71.9% | 410 | 738 |
| uniform | 20 | `node-a` | 512 | 512 | 1.00× | 70.3% | 411 | 803 |
| uniform | 20 | `nearest-node` | 512 | 512 | 1.00× | 70.9% | 409 | 780 |
| uniform | 20 | `access-point` | 512 | 512 | 1.00× | 68.7% | 396 | 883 |
| decay L=1024 | 1 | `node-a` | 512 | 512 | 1.00× | 69.9% | 401 | 831 |
| decay L=1024 | 1 | `nearest-node` | 512 | 512 | 1.00× | 69.9% | 401 | 831 |
| decay L=1024 | 1 | `access-point` | 512 | 512 | 1.00× | 69.2% | 397 | 861 |
| decay L=1024 | 5 | `node-a` | 512 | 512 | 1.00× | 69.9% | 401 | 831 |
| decay L=1024 | 5 | `nearest-node` | 512 | 512 | 1.00× | 70.1% | 404 | 820 |
| decay L=1024 | 5 | `access-point` | 512 | 512 | 1.00× | 72.8% | 408 | 706 |
| decay L=1024 | 20 | `node-a` | 512 | 512 | 1.00× | 69.9% | 401 | 831 |
| decay L=1024 | 20 | `nearest-node` | 512 | 512 | 1.00× | 69.4% | 398 | 854 |
| decay L=1024 | 20 | `access-point` | 512 | 512 | 1.00× | 70.7% | 405 | 795 |
| decay L=256 | 1 | `node-a` | 512 | 512 | 1.00× | 70.5% | 398 | 807 |
| decay L=256 | 1 | `nearest-node` | 512 | 512 | 1.00× | 70.5% | 398 | 807 |
| decay L=256 | 1 | `access-point` | 512 | 512 | 1.00× | 71.3% | 401 | 774 |
| decay L=256 | 5 | `node-a` | 512 | 512 | 1.00× | 70.5% | 398 | 807 |
| decay L=256 | 5 | `nearest-node` | 512 | 512 | 1.00× | 69.2% | 401 | 859 |
| decay L=256 | 5 | `access-point` | 512 | 512 | 1.00× | 69.3% | 391 | 864 |
| decay L=256 | 20 | `node-a` | 512 | 512 | 1.00× | 70.5% | 398 | 807 |
| decay L=256 | 20 | `nearest-node` | 512 | 512 | 1.00× | 70.3% | 408 | 807 |
| decay L=256 | 20 | `access-point` | 512 | 512 | 1.00× | 71.8% | 407 | 744 |
| monocentric L=512 | 1 | `node-a` | 512 | 512 | 1.00× | 70.0% | 397 | 828 |
| monocentric L=512 | 1 | `nearest-node` | 512 | 512 | 1.00× | 70.0% | 397 | 828 |
| monocentric L=512 | 1 | `access-point` | 512 | 512 | 1.00× | 71.7% | 414 | 742 |
| monocentric L=512 | 5 | `node-a` | 512 | 512 | 1.00× | 70.0% | 397 | 828 |
| monocentric L=512 | 5 | `nearest-node` | 512 | 512 | 1.00× | 72.1% | 406 | 733 |
| monocentric L=512 | 5 | `access-point` | 512 | 512 | 1.00× | 73.7% | 422 | 653 |
| monocentric L=512 | 20 | `node-a` | 512 | 512 | 1.00× | 70.0% | 397 | 828 |
| monocentric L=512 | 20 | `nearest-node` | 512 | 512 | 1.00× | 72.5% | 408 | 718 |
| monocentric L=512 | 20 | `access-point` | 512 | 512 | 1.00× | 69.6% | 392 | 851 |

**Every row reads 1.00×, and that is the result rather than a disappointment.** Adding Buildings to a Segment mints more Access Points; it does not make two trips *end on the same Segment*, which is the only thing a node-keyed entry can collapse. With 512 pairs drawn over 33,018 Segments — 512 draws from about a billion Segment pairs — no two share one, whatever sits on them. **The column cannot move on this axis**, so it is evidence of nothing on its own and is published beside an axis where it does move.

**The hit column is not idle, though, and it corroborates R5.3 from outside.** It sits near 70% for every rung — a **~30% miss floor with no storm, no Epoch and nothing stale** — which is R5.3's *28–31% of lookups missing on direct-mapped collisions before a road is touched*, reproduced by a different harness on a different pool. **That is R6.2's premise confirmed independently**, and it is the strongest thing this sub-table says.


**The axis that does move: how many places the city's trips end at.** Same pool size, same cache, same keys, uniform origins — only the destination set shrinks. This is a second invention and is swept for the same reason the first is.

| Destination sites | Key | Distinct keys | of pairs | Collapse | Hit | Resident | Evictions |
|---|---|---:|---:|---:|---:|---:|---:|
| unrestricted | `node-a` | 512 | 512 | 1.00× | 70.3% | 411 | 803 |
| unrestricted | `nearest-node` | 512 | 512 | 1.00× | 70.3% | 403 | 813 |
| unrestricted | `access-point` | 512 | 512 | 1.00× | 71.9% | 410 | 738 |
| 128 | `node-a` | 512 | 512 | 1.00× | 70.3% | 395 | 820 |
| 128 | `nearest-node` | 512 | 512 | 1.00× | 72.3% | 414 | 719 |
| 128 | `access-point` | 512 | 512 | 1.00× | 63.9% | 367 | 1,109 |
| 32 | `node-a` | 512 | 512 | 1.00× | 70.7% | 406 | 792 |
| 32 | `nearest-node` | 512 | 512 | 1.00× | 66.9% | 378 | 977 |
| 32 | `access-point` | 512 | 512 | 1.00× | 42.3% | 230 | 2,133 |
| 8 | `node-a` | 511 | 511 | 1.00× | 55.6% | 309 | 1,506 |
| 8 | `nearest-node` | 510 | 511 | 1.00× | 60.8% | 340 | 1,264 |
| 8 | `access-point` | 511 | 511 | 1.00× | 15.9% | 82 | 3,362 |

**The collapse column reads 1.00× on every row of both tables, and after two attempts to move it that is the section's finding rather than its failure.** A node-keyed entry collapses two Trips only when they share a Segment at **both** ends. Concentrating destinations onto 8 sites leaves 512 distinct origins, so the pairs stay distinct; adding Buildings to a Segment mints Access Points without making two Trips end together. **Collapse is a property of the ratio between the Trip population and the Segment-pair space**, and this graph has 33,018 Segments — about 1.09 × 10⁹ ordered pairs. No pool S2 can draw is dense in that.

**Which puts a question mark against `plans/0010`'s argument for the coarse key, and the honest position is that it is unconfirmed rather than refuted.** *"The five Buildings sharing a Segment share one entry instead of minting five"* is true only if those five Buildings' Trips also **end** on a shared Segment. Against 10⁹ Segment pairs, a real city's Trips may well be sparse enough that a node key collapses almost nothing — in which case the hit rate comes from **the same person repeating the same journey**, which no key affects, and the coarse key would be paying R6.1a's detour for very little. **S2 cannot settle this**: it needs a Trip population, which is `06` milestone 5b. What R6.1 does settle is the price side, exactly, and that the price is avoidable at no cost in key space.

**The concentration sweep moved a different column hard, and it belongs to R6.2.** As destinations concentrate, `access-point`'s hit rate falls **71.9% → 15.9%** with evictions rising 738 → 3,362, while `node-a` on the same pools falls only to 55.6%. **The key space did not shrink — distinct keys stay at 511–512 throughout — so this is not capacity, it is the slot function.** `RouteCache.Slot` is one multiply and one xor-shift, and on structured keys, where the low half takes very few values, it clusters. **A cache can lose two lookups in three to its hash while holding every entry it needs**, and that is R6.2's subject arriving early — R5.3's 28–31% miss floor is the same defect at a gentler input.

**No document may cite a hit rate from this section.** Both axes are invented, neither moved the column it was built to move, and the level of every hit figure is a property of a 512-pair pool standing in for Trip repetition that does not exist. What may be carried out of here is structural: **collapse needs coincidence at both ends**, and **the slot function degrades on structured keys**.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 25.13 s — from 20:40:47 UTC to 20:41:12 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 4.93 / 2.90 / 2.06 (1 / 5 / 15 min)
- **Load average, at end** 4.32 / 2.92 / 2.09 (1 / 5 / 15 min)
- **CPU stall** 283,767 µs over the run — 1.12% of it
- **Memory stall** 1 µs over the run — 0.00% of it
- **IO stall** 9,570 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
