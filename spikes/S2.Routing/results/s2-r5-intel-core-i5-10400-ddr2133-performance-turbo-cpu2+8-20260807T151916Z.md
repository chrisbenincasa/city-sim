## S2 R5 — the edit storm, and the Epoch ladder

- **Captured** 2026-08-07 15:19:17 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 2 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Build** Release

Working rung: 33,018 Segments, 16,697 nodes, 66,036 arcs, 32,069 of them admitting cars. Free-flow car costs, `Chebyshev`, the query shape R0 published against. The Segment→arc index the storm needs is 386.93 KiB and is a property of the Road Graph rather than of any rung, so it is stated once here and kept out of every resident-size column below.

### R5.1 — the gesture, which is the unit R3 and R4 could not reach

**A player does not delete a Segment; a player drags.** R3 priced one deleted Segment at 1.30 ms and R4 priced one at 4.71 ms against a 234.74 ms rebuild, and both said in their own words that the open case was hundreds of Segments in one gesture. This section measures the gesture's *shape* before anything is repaired, because every cost below is a function of how many clusters the gesture lands in and nothing else.

**The scattered row is a control and not a scenario.** A contiguous drag touches few clusters by construction, so a ladder rung keyed on clusters is flattered by the generator rather than by the design. Publishing only the drag would report a property of this harness as a property of the partition — the failure this spike has now made four times. Nobody drags scattered; the row exists so the drag's advantage has something to be an advantage *over*.

| Gesture | Asked | Collected | Worst shortfall | Arcs | Clusters @8 | Worst @8 | Clusters @16 | Worst @16 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| drag | 1 | 1.00 | 0 | 2.00 | 1.00 | 1 | 1.00 | 1 |
| drag | 4 | 4.00 | 0 | 8.00 | 1.25 | 2 | 1.25 | 2 |
| drag | 16 | 16.00 | 0 | 32.00 | 2.75 | 3 | 2.12 | 3 |
| drag | 64 | 64.00 | 0 | 128.00 | 8.00 | 11 | 4.50 | 7 |
| drag | 256 | 173.12 | 188 | 346.25 | 15.87 | 23 | 7.00 | 11 |
| scattered | 1 | 1.00 | 0 | 2.00 | 1.00 | 1 | 1.00 | 1 |
| scattered | 4 | 4.00 | 0 | 8.00 | 4.37 | 5 | 3.87 | 4 |
| scattered | 16 | 16.00 | 0 | 32.00 | 17.25 | 18 | 14.50 | 16 |
| scattered | 64 | 64.00 | 0 | 128.00 | 62.37 | 64 | 41.25 | 45 |
| scattered | 256 | 256.00 | 0 | 512.00 | 172.12 | 178 | 63.12 | 64 |

8 gestures per row. **Collected is reported rather than assumed equal to asked**: a drag follows the network and stops when it runs into road it has already deleted, and a sample that shrinks with the swept axis is how this spike has three times manufactured a trend out of survivorship. The partition at 8 Chunks is 256 clusters and at 16 Chunks is 64, so a *clusters touched* column approaching either figure is a gesture that has stopped being local.

### R5.2 — what a gesture costs to repair, against a rebuild

**The alternative to repairing is rebuilding, so the rebuild is the denominator** — and it is measured on both sides of the sweep rather than once. R3's first pinned capture read 1,401,307 ns for its denominator measured first and 477,609 ns for the same code measured last, a 193% spread, because the first timed thing in a process runs on a clock that has not ramped. Every ratio in this table divides by it.

- Full abstract-graph build at **8 Chunks**: 42.20 ms measured first, 40.34 ms measured last, 1.04× apart.
- Full abstract-graph build at **16 Chunks**: 72.21 ms measured first, 70.31 ms measured last, 1.02× apart.

**The *repair ÷ rebuild* column is the one a decision rests on, and it is the column R5's first draft got wrong.** That draft measured the rebuild at 8 Chunks once and divided the 16-Chunk repair figures by it — a denominator from a different experiment wearing the right units. A rebuild at 16 Chunks is a different amount of work from a rebuild at 8, so every ratio in the second half of the table was against a partition that was not the one being repaired. **This is R3's denominator finding arriving a fourth time**, in the one form it had not yet taken: not measured once instead of twice, but measured on the wrong rung.

**Coalesced against naive is the finding, not an implementation note.** A cluster's edge set is a function of its arcs, so it has to be decided once however many Segments inside it were deleted. The naive column is what a per-Segment repair loop costs — the spelling R3 and R4 measured, which is correct and indistinguishable from the coalesced one at a gesture of 1.

| Cluster | Gesture | Asked | Got | Clusters | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced | Coalesced as % of rebuild |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 8 | drag | 1 | 1 | 1 | 0.24 ms | 0.39 ms | 0.29 ms | 0.49 ms | 1.23× | 0.56% |
| 8 | drag | 4 | 4 | 1 | 0.26 ms | 0.47 ms | 0.81 ms | 1.04 ms | 3.06× | 0.63% |
| 8 | drag | 16 | 16 | 2 | 0.64 ms | 0.99 ms | 2.66 ms | 3.44 ms | 4.14× | 1.52% |
| 8 | drag | 64 | 64 | 8 | 1.83 ms | 2.74 ms | 13.61 ms | 18.09 ms | 7.40× | 4.35% |
| 8 | drag | 256 | 173 | 15 | 3.38 ms | 5.59 ms | 26.17 ms | 41.33 ms | 7.72× | 8.03% |
| 8 | scattered | 1 | 1 | 1 | 0.25 ms | 0.36 ms | 0.24 ms | 0.36 ms | 0.94× | 0.61% |
| 8 | scattered | 4 | 4 | 4 | 1.06 ms | 1.27 ms | 0.95 ms | 1.32 ms | 0.88× | 2.53% |
| 8 | scattered | 16 | 16 | 17 | 3.74 ms | 4.17 ms | 3.91 ms | 4.27 ms | 1.04× | 8.86% |
| 8 | scattered | 64 | 64 | 62 | 13.79 ms | 14.73 ms | 14.98 ms | 15.49 ms | 1.08× | 32.69% |
| 8 | scattered | 256 | 256 | 172 | 38.68 ms | 40.80 ms | 58.22 ms | 59.27 ms | 1.50× | 91.66% |
| 16 | drag | 1 | 1 | 1 | 0.99 ms | 1.50 ms | 1.06 ms | 1.62 ms | 1.07× | 1.37% |
| 16 | drag | 4 | 4 | 1 | 1.31 ms | 2.53 ms | 4.16 ms | 5.93 ms | 3.17× | 1.81% |
| 16 | drag | 16 | 16 | 2 | 2.25 ms | 2.96 ms | 16.37 ms | 25.13 ms | 7.25× | 3.12% |
| 16 | drag | 64 | 64 | 4 | 4.33 ms | 7.06 ms | 66.07 ms | 85.06 ms | 15.24× | 6.00% |
| 16 | drag | 256 | 173 | 7 | 6.34 ms | 10.68 ms | 147.74 ms | 200.27 ms | 23.28× | 8.78% |
| 16 | scattered | 1 | 1 | 1 | 0.98 ms | 1.61 ms | 0.97 ms | 1.49 ms | 0.98× | 1.37% |
| 16 | scattered | 4 | 4 | 3 | 4.07 ms | 4.96 ms | 4.24 ms | 5.50 ms | 1.04× | 5.63% |
| 16 | scattered | 16 | 16 | 14 | 15.88 ms | 18.33 ms | 18.33 ms | 20.69 ms | 1.15× | 21.99% |
| 16 | scattered | 64 | 64 | 41 | 46.45 ms | 52.99 ms | 75.21 ms | 79.85 ms | 1.61× | 64.32% |
| 16 | scattered | 256 | 256 | 63 | 72.39 ms | 73.97 ms | 309.90 ms | 324.49 ms | 4.28× | 100.25% |

8 gestures per row, each applied, repaired, reverted and repaired again, so every rung starts and ends on the same abstract graph and the timed figure is one repair. **Worst is the worst single gesture, not a quantile** — a gesture is one player action and a quantile over eight of them would hide the event S4's K6 was about.

### R5.3 — the Epoch ladder, and what *never a global flush* is worth

**`CONTEXT.md` → Epoch commits to lazy revalidation and *never a global flush*, and the Epoch as written is a single counter on the whole Road Graph.** A counter carries no location, so a route computed at Epoch 5 and used at Epoch 6 cannot tell whether the edit touched it. **`Lazy` describes when you pay, not what survives** — under one counter the answer to *what survives* is *nothing*, and the flush is total however lazily it is paid. This section prices the two rungs that carry a location.

**The zero-edit row is the instrument check, and it is why it is in the table.** With no edits every rung must read a near-total hit rate, because the pool is smaller than the cache and nothing invalidates. A rung reading low there has a broken cache rather than a strict Epoch, and R2 published byte-identical peaks from exactly that kind of silence. Under the global rung hit rate is **not a property of the O-D draw at all** — it is a property of how recently the player touched anything — so a throughput figure could be reported with a cache that had quietly stopped working.

| O-D rung | Epoch | Edit every | Deleted | Hit | Stale | Miss | Unroutable | Revalidation words | Mean Tick | Worst Tick |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | global | never | 0 | 71.63% | 0.00% | 28.36% | 0.00% | 0.71 | 1054.72 µs | 6385.92 µs |
| uniform | per-cluster | never | 0 | 71.63% | 0.00% | 28.36% | 0.00% | 7.56 | 956.69 µs | 3557.14 µs |
| uniform | per-Segment | never | 0 | 71.63% | 0.00% | 28.36% | 0.00% | 41.97 | 959.39 µs | 5166.95 µs |
| uniform | global | 64 Ticks | 64 | 49.36% | 22.26% | 28.36% | 0.00% | 0.71 | 1681.91 µs | 5092.35 µs |
| uniform | per-cluster | 64 Ticks | 64 | 70.62% | 1.00% | 28.36% | 0.00% | 7.56 | 1012.04 µs | 3551.44 µs |
| uniform | per-Segment | 64 Ticks | 64 | 71.60% | 0.02% | 28.36% | 0.00% | 41.97 | 993.09 µs | 4190.45 µs |
| uniform | global | 16 Ticks | 256 | 20.87% | 50.75% | 28.36% | 0.00% | 0.71 | 2651.55 µs | 5681.88 µs |
| uniform | per-cluster | 16 Ticks | 256 | 66.25% | 5.37% | 28.36% | 0.00% | 7.60 | 1272.40 µs | 4109.10 µs |
| uniform | per-Segment | 16 Ticks | 256 | 70.19% | 1.44% | 28.36% | 0.00% | 42.79 | 1125.22 µs | 5307.06 µs |
| uniform | global | 4 Ticks | 1021 | 6.59% | 65.03% | 28.36% | 0.00% | 0.71 | 3380.09 µs | 6667.19 µs |
| uniform | per-cluster | 4 Ticks | 1021 | 57.49% | 14.13% | 28.36% | 0.00% | 7.64 | 1740.91 µs | 4852.53 µs |
| uniform | per-Segment | 4 Ticks | 1021 | 68.99% | 2.63% | 28.36% | 0.00% | 42.80 | 1271.79 µs | 4194.33 µs |
| decay L=256 | global | never | 0 | 69.31% | 0.00% | 30.68% | 0.00% | 0.69 | 185.62 µs | 821.52 µs |
| decay L=256 | per-cluster | never | 0 | 69.31% | 0.00% | 30.68% | 0.00% | 2.55 | 191.07 µs | 831.30 µs |
| decay L=256 | per-Segment | never | 0 | 69.31% | 0.00% | 30.68% | 0.00% | 15.20 | 190.44 µs | 823.45 µs |
| decay L=256 | global | 64 Ticks | 64 | 47.99% | 21.31% | 30.68% | 0.00% | 0.69 | 341.24 µs | 1694.90 µs |
| decay L=256 | per-cluster | 64 Ticks | 64 | 68.53% | 0.78% | 30.68% | 0.00% | 2.55 | 208.12 µs | 1449.85 µs |
| decay L=256 | per-Segment | 64 Ticks | 64 | 69.21% | 0.09% | 30.68% | 0.00% | 15.20 | 201.89 µs | 1769.61 µs |
| decay L=256 | global | 16 Ticks | 256 | 20.50% | 48.80% | 30.68% | 0.00% | 0.69 | 547.76 µs | 3372.88 µs |
| decay L=256 | per-cluster | 16 Ticks | 256 | 66.40% | 2.90% | 30.68% | 0.00% | 2.56 | 262.10 µs | 1590.90 µs |
| decay L=256 | per-Segment | 16 Ticks | 256 | 68.57% | 0.73% | 30.68% | 0.00% | 15.35 | 258.28 µs | 2019.58 µs |
| decay L=256 | global | 4 Ticks | 1021 | 6.51% | 62.71% | 30.76% | 0.09% | 0.69 | 779.24 µs | 2084.87 µs |
| decay L=256 | per-cluster | 4 Ticks | 1021 | 61.27% | 7.95% | 30.76% | 0.09% | 2.56 | 465.41 µs | 2077.24 µs |
| decay L=256 | per-Segment | 4 Ticks | 1021 | 67.84% | 1.39% | 30.76% | 0.09% | 15.36 | 412.19 µs | 2122.65 µs |
| monocentric L=512 | global | never | 0 | 69.77% | 0.00% | 30.22% | 0.00% | 0.69 | 747.83 µs | 2719.23 µs |
| monocentric L=512 | per-cluster | never | 0 | 69.77% | 0.00% | 30.22% | 0.00% | 6.55 | 752.81 µs | 2734.36 µs |
| monocentric L=512 | per-Segment | never | 0 | 69.77% | 0.00% | 30.22% | 0.00% | 37.58 | 754.28 µs | 2815.78 µs |
| monocentric L=512 | global | 64 Ticks | 64 | 48.63% | 21.14% | 30.22% | 0.00% | 0.69 | 1390.85 µs | 4441.33 µs |
| monocentric L=512 | per-cluster | 64 Ticks | 64 | 69.16% | 0.61% | 30.22% | 0.00% | 6.55 | 787.58 µs | 2765.79 µs |
| monocentric L=512 | per-Segment | 64 Ticks | 64 | 69.77% | 0.00% | 30.22% | 0.00% | 37.58 | 796.31 µs | 3114.16 µs |
| monocentric L=512 | global | 16 Ticks | 256 | 20.94% | 48.82% | 30.22% | 0.00% | 0.69 | 2163.52 µs | 5284.59 µs |
| monocentric L=512 | per-cluster | 16 Ticks | 256 | 65.42% | 4.34% | 30.22% | 0.00% | 6.55 | 961.50 µs | 3362.92 µs |
| monocentric L=512 | per-Segment | 16 Ticks | 256 | 68.67% | 1.09% | 30.22% | 0.00% | 37.85 | 881.07 µs | 4415.15 µs |
| monocentric L=512 | global | 4 Ticks | 1021 | 6.44% | 63.33% | 30.22% | 0.00% | 0.69 | 2711.75 µs | 9373.28 µs |
| monocentric L=512 | per-cluster | 4 Ticks | 1021 | 57.10% | 12.67% | 30.22% | 0.00% | 6.56 | 1371.47 µs | 4429.81 µs |
| monocentric L=512 | per-Segment | 4 Ticks | 1021 | 67.70% | 2.07% | 30.22% | 0.00% | 37.87 | 1012.02 µs | 3490.05 µs |

256 Ticks, 16 Trip starts per Tick, drawn with repetition from a pool of 512 distinct origin-destination pairs into a cache of 1024 entries at 8 Chunks per cluster. Gestures are 16-Segment drags. **Routes refused for exceeding the slot: 0. Entries evicted by a colliding key: 29373.**

**The storm never reverts, which is what makes it a storm and also what the *Deleted* and *Unroutable* columns are for.** A player bulldozing continuously does not put the road back, so the graph degrades monotonically across the run and the later Ticks of a high-edit-rate row are routing on a materially different city from its first. Nothing negative is cached, so a pair the storm has severed pays a full failed search every Tick it is drawn. **A row whose *Unroutable* share is large is measuring severance rather than caching**, and its hit rate should not be read as the Epoch rung's doing.

**Ladder monotonicity: 12 triples checked, 0 violations.** Each rung is strictly less conservative than the one above it — *anything moved* implies *a crossed cluster moved* implies *an own Segment moved* — so hit rate must be non-decreasing down the ladder at every O-D rung and every edit rate. It is printed on the run where it reads zero because that is the only run on which it is worth anything: R2 published byte-identical peaks precisely because nothing was wired up to disagree with them.

**The pool is what stands in for Trips repeating, and it is invented.** A route cache works because real Trips recur — the same Household drives the same commute every Day — and nothing in S2 can produce that recurrence, because it needs Trip generation. Drawing fresh pairs every Tick would measure a cache with no reuse to exploit and report ~0% for every rung, which would compare nothing. A fixed pool sampled with repetition supplies reuse at a rate this harness chose. **So the absolute hit rates below are a property of the pool size and must not be quoted as the hit rate a route cache achieves**; what the pool cannot distort is the *ratio* between rungs under the same pool, which is what the ladder is for. Same handling R4.1 gave the O-D family, and for the same reason.

**The *Miss* column is the eviction policy's bill, and isolating it was not the point of this table.** It is flat at roughly 28–31% within an O-D rung and does not move with edit rate, which is the tell: misses here are collisions in a direct-mapped cache, not staleness. A pool of 512 keys into 1,024 slots loses **about three lookups in ten** to two keys wanting the same slot, at 2× over-provisioning and before a single road is touched. **That is an argument for `adr/0017`'s least-used policy carrying a number for the first time**, and it belongs to R6, which owns the eviction decision. It is reported here because the figure fell out and a reader would otherwise read it as the Epoch's doing.

**The trade this section was written to find does not exist.** `plans/0010` frames the ladder as *hit rate against revalidation cost*, on the reasonable expectation that an O(path length) check is what a per-Segment Epoch charges for its precision. It charges about 42 words a lookup against the global rung's 0.71 — and the mean Tick is **lower** at per-Segment than at global at every edit rate measured, because the searches the precision avoids cost orders of magnitude more than the words it reads. **There is no rung on this ladder that trades accuracy for speed.** Per-Segment is cheaper *and* more precise, and the plan's framing was the thing that needed measuring.

### R5.4 — the addition, and the fact that only one rung is sound

**R5.3 recommends the per-Segment rung, and this section is the argument against it.** The ladder above measures *deletion*, which is the half of the core verb R3 and R4 could price. Deletion and addition are not symmetric, and the asymmetry is not a detail of this implementation — it is a property of shortest paths.

**Deletion is monotone-worsening.** Remove an arc that is not on route `R` and `R`'s cost is unchanged while every alternative's cost can only rise, so `R` is still optimal. A rung that watches only `R`'s own Segments therefore misses nothing: if `R` became infeasible, one of its own Segments was the one deleted. **That is why per-Segment reads as exact above.**

**Addition is monotone-improving, and that inverts the argument.** A new arc can create a cheaper path bearing no relation to `R` whatsoever. A route computed before a road existed **cannot contain that road**, so no version the per-Segment rung watches can ever move — it declares every pre-existing entry valid, permanently. **And per-cluster is unsound for the same reason**: a new fast link in a cluster the route never enters can still beat it. **Only the global rung is sound under addition**, which is the rung R5.3 just measured as unusable. The ladder has no rung that is both affordable and correct across the whole core verb.

**Addition is measurable after all, and the trick is worth recording.** R3 deferred it because drawing a road across a boundary creates a portal the abstract graph's build reserved no slot for. So: **build the abstract graph on the full graph — reserving every portal — then delete a set of Segments, and then restore them.** Restoration *is* addition, and it needs no new portal. `RebuildCluster` re-derives its crossing arcs from the cost array and re-applies the reduction, so a restored arc comes back properly rather than being re-costed into a frozen edge set.

**The *Improvable* column is the instrument check and it is load-bearing.** It is the share of resident entries that a fresh search beats after the addition — ground truth, independent of any rung. **If it read zero, every *wrongly valid* column below would read zero too and would prove nothing**, which is the shape R3's 0.00% detour wore until sampling transitions drove it to 80.49%.

| Added | Asked | Got | Epoch | Resident | Improvable | Declared valid | **Wrongly valid** | Mean detour | Worst detour |
|---|---:|---:|---|---:|---:|---:|---:|---:|---:|
| street drag | 16 | 16 | global | 412 | 0.00% | 0.00% | **0.00%** | 0.00% | 0.00% |
| street drag | 16 | 16 | per-cluster | 412 | 0.00% | 83.73% | **0.00%** | 0.00% | 0.00% |
| street drag | 16 | 16 | per-Segment | 412 | 0.00% | 100.00% | **0.00%** | 0.00% | 0.00% |
| street drag | 64 | 64 | global | 412 | 0.00% | 0.00% | **0.00%** | 0.00% | 0.00% |
| street drag | 64 | 64 | per-cluster | 412 | 0.00% | 75.72% | **0.00%** | 0.00% | 0.00% |
| street drag | 64 | 64 | per-Segment | 412 | 0.00% | 100.00% | **0.00%** | 0.00% | 0.00% |
| street drag | 256 | 126 | global | 412 | 0.00% | 0.00% | **0.00%** | 0.00% | 0.00% |
| street drag | 256 | 126 | per-cluster | 412 | 0.00% | 75.48% | **0.00%** | 0.00% | 0.00% |
| street drag | 256 | 126 | per-Segment | 412 | 0.00% | 100.00% | **0.00%** | 0.00% | 0.00% |
| arterial | 256 | 4 | global | 412 | 9.22% | 0.00% | **0.00%** | 0.00% | 0.00% |
| arterial | 256 | 4 | per-cluster | 412 | 9.22% | 88.10% | **3.64%** | 6.04% | 25.48% |
| arterial | 256 | 4 | per-Segment | 412 | 9.22% | 100.00% | **9.22%** | 16.71% | 62.65% |

A pool of 512 uniform origin-destination pairs, cached on the graph with the Segments deleted, then priced against a fresh search once they are restored. *Wrongly valid* is the share of resident entries the rung declared good that a fresh search strictly beats. *Detour* is over the wrongly-valid entries only, arc cost against arc cost — **it excludes the Access Point offset remainders at both ends**, which are common to both routes and bounded by one Segment each. The graph holds 104 Arterial Segments against 32,069 admitting cars.

**Restoring ordinary Street improves nothing, and that is a property of this graph rather than a fact about cities.** The synthetic grid is one Street per Cell boundary at a uniform speed, so between any two points there are very many *equal-cost* shortest paths. Deleting a line of Street leaves an equal-cost alternative one block over, the cached route's cost is therefore unchanged, and restoring the line gives the search nothing to find. **The zero is real and it does not generalise**: a real network has heterogeneous speeds and far fewer ties, and R0's road-density figure already carries the disclaimer that nobody has checked this graph against a real city. **Read the Arterial row, not the Street rows** — an Arterial is the only thing on this map that breaks the degeneracy, which is exactly why it was added as a shape.

**The Arterial gesture collects 4 Segments and the smallness strengthens the conclusion rather than weakening it.** Four Segments is about 512 m of new fast road — the smallest addition a player would bother drawing. If half a kilometre of Arterial leaves a per-Segment Epoch serving stale routes on 9.22% of resident entries at a mean 16.71% detour, a larger addition cannot do better. **The figure is a floor.**

**And unlike every other error in this spike, it does not heal.** A stale entry under the per-Segment rung has no mechanism that will ever notice: the road it should be using is one the route does not contain, so no version it watches will move again. The only thing that removes it is **eviction** — and `adr/0012` keys the cache by origin-destination pair rather than by agent, so the entry is not one driver's habit but every driver's route, and a hot pair is the *least* likely to be evicted precisely because it is hot. **The error is permanent and it is concentrated on the busiest pairs in the city.**

### R5.5.1 — the edit response, which is what the player is waiting for

**A path source is not chosen on what it costs to read; it is chosen on what it costs to make correct again.** `plans/0010`'s first tripwire for this section says so in the form that matters — *a rung that cannot be made correct again within one Tick budget after a plausible gesture is out on a design commitment, not on a number* — because the player is holding the mouse button down while it happens. This table is that quantity, per rung, swept over the gesture sizes R5.1 established the shape of.

**The naive columns exist because R5.2 found a 23.26× spelling difference and this is where that finding is tested for generality.** `RepairSubtree` takes a changed-arc set, so it has a coalesced spelling and a per-Segment one, exactly as `AbstractGraph` did. If looping it per deleted Segment over a drag is a catastrophe too, then *a per-edit repair API invites the loop that produces it* stops being a routing note and becomes a corpus-wide rule about API shape. If it is not, R5.2's finding is a property of a cluster's edge set and belongs to the hierarchy alone.

**Two rungs have no naive spelling and the reason differs.** The cache rungs repair the abstract graph, which R5.2 has already priced both ways and which is reproduced here only so the columns are comparable. `shared` has no repair *by construction* — R4 established that maintenance is separable from path source, so writing a repair for `RouteStore` would measure the repair rather than the rung, and `plans/0010` prices it as a rebuild precisely so that a loss is a retirement on a number. `flat` has no edit response at all, which is the whole of what it is for: it is the row that gives this column a floor of nothing.

**`cache` and `cache+ttl` must read the same figure here, and printing both is the cheap check that they do.** A rotation is a per-Tick cost and not a per-gesture one, so the TTL cannot change what an edit costs; two rows that disagree would be two rungs that are secretly different experiments, which is the defect R2 shipped as byte-identical peaks and this spike has been paying for since.

| Rung | Gesture | Got | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced |
|---|---:|---:|---:|---:|---:|---:|---:|
| cache | drag 1 | 1 | 0.33 ms | 0.42 ms | — | — | — |
| cache | drag 4 | 4 | 0.30 ms | 0.39 ms | — | — | — |
| cache | drag 16 | 16 | 0.57 ms | 0.73 ms | — | — | — |
| cache | drag 64 | 64 | 1.51 ms | 2.17 ms | — | — | — |
| cache | drag 256 | 173 | 2.75 ms | 4.10 ms | — | — | — |
| cache+ttl | drag 1 | 1 | 0.21 ms | 0.24 ms | — | — | — |
| cache+ttl | drag 4 | 4 | 0.25 ms | 0.38 ms | — | — | — |
| cache+ttl | drag 16 | 16 | 0.55 ms | 0.79 ms | — | — | — |
| cache+ttl | drag 64 | 64 | 1.64 ms | 2.46 ms | — | — | — |
| cache+ttl | drag 256 | 173 | 3.54 ms | 6.78 ms | — | — | — |
| nexthop | drag 1 | 1 | 0.65 ms | 1.67 ms | 0.59 ms | 1.63 ms | 0.91× |
| nexthop | drag 4 | 4 | 0.91 ms | 1.76 ms | 0.99 ms | 2.36 ms | 1.08× |
| nexthop | drag 16 | 16 | 2.04 ms | 5.39 ms | 2.50 ms | 6.08 ms | 1.22× |
| nexthop | drag 64 | 64 | 11.93 ms | 38.22 ms | 18.14 ms | 71.63 ms | 1.51× |
| nexthop | drag 256 | 173 | 20.38 ms | 45.64 ms | 29.42 ms | 85.21 ms | 1.44× |
| shared | drag 1 | 1 | 180.53 ms | 182.31 ms | — | — | — |
| shared | drag 4 | 4 | 182.12 ms | 192.12 ms | — | — | — |
| shared | drag 16 | 16 | 182.44 ms | 185.87 ms | — | — | — |
| shared | drag 64 | 64 | 178.34 ms | 182.10 ms | — | — | — |
| shared | drag 256 | 173 | 179.32 ms | 188.38 ms | — | — | — |
| flat | drag 1 | — | none | none | — | — | — |
| flat | drag 4 | — | none | none | — | — | — |
| flat | drag 16 | — | none | none | — | — | — |
| flat | drag 64 | — | none | none | — | — | — |
| flat | drag 256 | — | none | none | — | — | — |

8 gestures per row, each applied, repaired, reverted and repaired again, so every row starts and ends on the same graph and the timed figure is one repair. **Worst is the worst single gesture and not a quantile** — a gesture is one player action, and S4's K6 established that a quantile over eight of them hides precisely the event this column exists to publish. The next-hop rung repairs all 121 District columns, which is the whole table and not a sample; the shared rung rebuilds all 14,641 ordered pairs, which is why its figure does not move with gesture size.

**Audited, because a repair that silently does nothing reports success otherwise.** After the largest gesture of each row the repaired table is compared entry by entry against a column freshly `Seed`ed on the damaged graph — 10,101,685 entries. **Coalesced: 0 wrong cost, 0 stranded. Naive: 0 wrong cost, 0 stranded.** R4 hit this exact failure mode and it is the reason the check is here rather than assumed: a scheme that returns without doing anything is the fastest scheme on the table and every surrounding column looks healthy.

### R5.5.2 — the storm, and which kind of wrong each rung serves

**One storm, five path sources.** Every row below runs the same seed, the same pool, the same Trip draw and the same gesture schedule, so a row differs from the row beside it by its path source and by nothing else. That is not a courtesy: R2 published two rungs with byte-identical peaks because the experiment had quietly removed the difference it existed to measure, and the cheapest defence against repeating it is to make the shared state shared by construction rather than by coincidence.

**The flat search is the denominator and it is measured on both sides of the sweep.** R3's first pinned capture read 1,401,307 ns for the same quantity measured first and 477,609 ns measured last, and R5's own capture has just found the artefact live at **4.88×** on a machine pinned to one logical processor. Neither reading is warmed, because the content of the first one is how cold the process was when it was taken.

- One uncached point-to-point search, arcs returned: 911.68 µs measured first, 402.74 µs measured last, 2.26× apart.

**Detour is what the rung actually served, against a flat search on the arc costs as they are at that moment.** Both sides are arc-cost sums, so both exclude the Access Point offset remainders at the two ends — common to both routes and bounded by one Segment each, which is R5.4's handling and for R5.4's reason. `flat` must therefore read **exactly 0.00%**, and it is computed through a second search instance rather than aliased to the truth so that the zero is a round trip through the whole pipeline. **A zero everywhere would be indistinguishable from an instrument that is not wired up** — R3.5's defect, which R3.6 is how the corpus learned to catch.

**The District-granular rungs are composed the way R4.8 composed them.** The next-hop rung is followed from wherever the Traveller is to the destination District's representative and then searched onward, so it is coarse at the destination end only. The shared rung is coarse at **both** ends — to the origin District's representative, along the stored route, and onward from the destination representative — which is R2's composition and the reason its error was roughly twice the next-hop rung's. Each leg starts at a Segment incident to a representative rather than at the node itself, which can only make the followed route look cheaper, so **every District-granular figure below is a lower bound**.

**Detour is sampled and the sample size is printed per row.** A truth search per Trip would cost more than every rung it prices put together, so it is taken on one Tick in 16, **after that Tick's clock has stopped** — the instrument must not land in the column it is measuring. A sample that shrinks with the swept axis has manufactured a trend three times in this spike, so the column is a survivorship check as much as a sample size: a pair is dropped only when a leg finds no route.

**The *Sample* column falls as the edit rate rises, and it is the storm removing routable pairs rather than the instrument losing interest.** The storm never reverts, so by the last Tick of a four-Tick-period row about a thousand Segments are gone; a sampled pair is dropped when the truth search finds no route at all, which happens when the player has bulldozed the Segment the Trip starts or ends on, or — rarely on a grid — when the pair has genuinely been severed. It tracks the control's *Unroutable* column exactly, which is what identifies it. **This is the survivorship shape the corpus has been caught by three times**, so it is named rather than left for a reader to notice: the rows at the highest edit rate are drawn from a slightly smaller and slightly better-connected population than the rows above them.

**The hierarchy is optimal, and the detour column's own units are not exact. Both halves are measured here rather than assumed.** The cache rows print a non-zero *worst* detour with **no edits applied at all**, which is either R3.5 — *100% optimal, 0.00% mean detour, no route cheaper than the flat optimum* — being wrong, or this section's arithmetic being wrong. On a pristine graph, per O-D rung:

| O-D rung | Sampled | HPA\* worse than flat | HPA\* better | Equal cost, unequal arc sum | Worst arc-sum gap |
|---|---:|---:|---:|---:|---:|
| uniform | 512 | **0** | 0 | 8 | 1.38% |
| decay L=256 | 512 | **0** | 0 | 6 | 5.26% |
| monocentric L=512 | 512 | **0** | 0 | 13 | 3.67% |

**Read the third column first: it is zero, so R3.5 stands and the hierarchy loses nothing.** On whole journey cost — arcs *plus* the two Access Point remainders, which is the quantity both searches actually minimise — HPA\* never returns a worse route than the flat search, and never a cheaper one either, which would have been an admissibility bug. **The residual is the column's units.** A cached route is a list of arcs and nothing else, so the detour column sums arcs and drops both remainders; two routes of *identical* whole-journey cost can then have different arc sums, because one enters the destination Segment from the far endpoint and trades a larger remainder for a smaller arc total. The last two columns size exactly that, over the whole pool rather than over the sampled Ticks — so the worst arc-sum gap **bounds** the worst detour the cache rows print, rung for rung, and does not equal it. That the bound holds in every rung is the check; had a cache row exceeded its own rung's gap, the residual would not have been the explanation.

**So the detour column has a resolution floor of about one Segment at each end, and it is not a property of any rung.** It bounds every number in the column, including the District-granular ones: `nexthop` and `shared` are composed against the same truth and carry the same residual. It is immaterial there, being a floor of a few per cent against errors of tens to hundreds of per cent — and it is the *whole* of the cache rungs' reading, which is why **no cache row's detour may be quoted as a cost of the hierarchy**. **The honest statement is that the cache rungs serve routes this instrument cannot distinguish from optimal.** Correcting it would mean charging the served route the remainders its own endpoints imply, which is a change to what the rung is credited with rather than to the truth, and it is not made here.

| O-D rung | Rung | Edit every | Mean Tick | Worst Tick | Hit | Stale | Miss | Forced refreshes / Tick | Unroutable | Mean detour | p90 | Worst | Sample |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | cache | never | 948.19 µs | 3334.08 µs | 71.63% | 0.00% | 28.36% | — | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | cache+ttl 64 | never | 1781.07 µs | 4326.86 µs | 46.63% | 0.00% | 53.36% | 5.26 | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | cache+ttl 256 | never | 1147.58 µs | 3305.62 µs | 64.74% | 0.00% | 35.25% | 1.44 | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | cache+ttl 1024 | never | 997.07 µs | 3386.85 µs | 70.06% | 0.00% | 29.93% | 0.34 | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | nexthop | never | 1.92 µs | 27.07 µs | — | — | — | — | 0 | 16.58% | 26.97% | 913.61% | 254 |
| uniform | shared | never | 0.72 µs | 11.02 µs | — | — | — | — | 0 | 31.21% | 56.46% | 953.61% | 249 |
| uniform | flat | never | 6934.00 µs | 12965.99 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 256 |
| uniform | cache | 64 Ticks | 975.91 µs | 6390.02 µs | 71.60% | 0.02% | 28.36% | — | 0 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | cache+ttl 64 | 64 Ticks | 1768.77 µs | 4218.14 µs | 46.60% | 0.02% | 53.36% | 5.26 | 0 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | cache+ttl 256 | 64 Ticks | 1217.57 µs | 5101.02 µs | 64.72% | 0.02% | 35.25% | 1.44 | 0 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | cache+ttl 1024 | 64 Ticks | 1040.93 µs | 7147.47 µs | 70.04% | 0.02% | 29.93% | 0.34 | 0 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | nexthop | 64 Ticks | 16.37 µs | 1577.55 µs | — | — | — | — | 0 | 16.64% | 26.97% | 913.61% | 253 |
| uniform | shared | 64 Ticks | 2828.67 µs | 183060.84 µs | — | — | — | — | 0 | 31.28% | 56.46% | 953.61% | 248 |
| uniform | flat | 64 Ticks | 6778.01 µs | 12453.31 µs | — | — | — | — | 7 | 0.00% | 0.00% | 0.00% | 255 |
| uniform | cache | 16 Ticks | 1104.25 µs | 4109.28 µs | 70.19% | 1.44% | 28.36% | — | 0 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | cache+ttl 64 | 16 Ticks | 1840.67 µs | 5204.95 µs | 46.24% | 0.39% | 53.36% | 5.26 | 0 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | cache+ttl 256 | 16 Ticks | 1279.23 µs | 5007.74 µs | 63.67% | 1.07% | 35.25% | 1.44 | 0 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | cache+ttl 1024 | 16 Ticks | 1159.19 µs | 5174.00 µs | 68.65% | 1.41% | 29.93% | 0.34 | 0 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | nexthop | 16 Ticks | 270.54 µs | 41016.74 µs | — | — | — | — | 0 | 16.47% | 26.97% | 913.61% | 252 |
| uniform | shared | 16 Ticks | 11300.27 µs | 183233.65 µs | — | — | — | — | 0 | 30.99% | 56.66% | 953.61% | 247 |
| uniform | flat | 16 Ticks | 6794.80 µs | 12728.52 µs | — | — | — | — | 29 | 0.00% | 0.00% | 0.00% | 254 |
| uniform | cache | 4 Ticks | 1283.11 µs | 5393.51 µs | 68.99% | 2.63% | 28.36% | — | 0 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | cache+ttl 64 | 4 Ticks | 2016.39 µs | 5261.50 µs | 45.84% | 0.78% | 53.36% | 5.26 | 0 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | cache+ttl 256 | 4 Ticks | 1493.74 µs | 4234.15 µs | 62.67% | 2.07% | 35.25% | 1.44 | 0 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | cache+ttl 1024 | 4 Ticks | 1343.32 µs | 3971.62 µs | 67.50% | 2.56% | 29.93% | 0.34 | 0 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | nexthop | 4 Ticks | 583.71 µs | 41383.16 µs | — | — | — | — | 0 | 16.69% | 28.80% | 913.61% | 246 |
| uniform | shared | 4 Ticks | 45297.27 µs | 187119.46 µs | — | — | — | — | 0 | 31.27% | 56.46% | 953.61% | 241 |
| uniform | flat | 4 Ticks | 6663.52 µs | 13914.72 µs | — | — | — | — | 89 | 0.00% | 0.00% | 0.00% | 251 |
| decay L=256 | cache | never | 188.47 µs | 821.61 µs | 69.31% | 0.00% | 30.68% | — | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | cache+ttl 64 | never | 325.17 µs | 965.45 µs | 45.94% | 0.00% | 54.05% | 5.07 | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | cache+ttl 256 | never | 228.86 µs | 802.96 µs | 62.54% | 0.00% | 37.45% | 1.37 | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | cache+ttl 1024 | never | 194.52 µs | 800.36 µs | 67.43% | 0.00% | 32.56% | 0.39 | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | nexthop | never | 1.40 µs | 13.51 µs | — | — | — | — | 0 | 149.73% | 340.00% | 5765.28% | 250 |
| decay L=256 | shared | never | 0.67 µs | 12.37 µs | — | — | — | — | 0 | 211.94% | 603.34% | 6098.61% | 248 |
| decay L=256 | flat | never | 909.36 µs | 2388.41 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 254 |
| decay L=256 | cache | 64 Ticks | 198.69 µs | 1278.01 µs | 69.21% | 0.09% | 30.68% | — | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 64 | 64 Ticks | 341.49 µs | 1392.14 µs | 45.92% | 0.02% | 54.05% | 5.07 | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 256 | 64 Ticks | 246.90 µs | 1275.13 µs | 62.47% | 0.07% | 37.45% | 1.37 | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 1024 | 64 Ticks | 221.73 µs | 1799.08 µs | 67.35% | 0.07% | 32.56% | 0.39 | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | nexthop | 64 Ticks | 14.76 µs | 1446.93 µs | — | — | — | — | 0 | 138.91% | 340.00% | 5765.28% | 249 |
| decay L=256 | shared | 64 Ticks | 2792.43 µs | 181252.30 µs | — | — | — | — | 0 | 200.07% | 600.00% | 6098.61% | 247 |
| decay L=256 | flat | 64 Ticks | 892.83 µs | 2375.61 µs | — | — | — | — | 21 | 0.00% | 0.00% | 0.00% | 253 |
| decay L=256 | cache | 16 Ticks | 244.75 µs | 1824.04 µs | 68.57% | 0.73% | 30.68% | — | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 64 | 16 Ticks | 393.79 µs | 2812.87 µs | 45.72% | 0.21% | 54.05% | 5.07 | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 256 | 16 Ticks | 319.57 µs | 2804.33 µs | 62.08% | 0.46% | 37.45% | 1.37 | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 1024 | 16 Ticks | 282.62 µs | 4512.42 µs | 66.77% | 0.65% | 32.56% | 0.39 | 0 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | nexthop | 16 Ticks | 277.08 µs | 41945.41 µs | — | — | — | — | 0 | 139.10% | 340.00% | 5765.28% | 249 |
| decay L=256 | shared | 16 Ticks | 11345.35 µs | 195329.47 µs | — | — | — | — | 0 | 201.36% | 600.00% | 6098.61% | 247 |
| decay L=256 | flat | 16 Ticks | 893.68 µs | 2343.73 µs | — | — | — | — | 36 | 0.00% | 0.00% | 0.00% | 253 |
| decay L=256 | cache | 4 Ticks | 413.51 µs | 3498.06 µs | 67.84% | 1.39% | 30.76% | — | 4 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | cache+ttl 64 | 4 Ticks | 532.67 µs | 3596.73 µs | 45.41% | 0.51% | 54.07% | 5.06 | 4 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | cache+ttl 256 | 4 Ticks | 437.34 µs | 1726.39 µs | 61.40% | 1.09% | 37.50% | 1.37 | 4 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | cache+ttl 1024 | 4 Ticks | 418.74 µs | 2086.25 µs | 66.04% | 1.31% | 32.64% | 0.39 | 4 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | nexthop | 4 Ticks | 596.49 µs | 41679.98 µs | — | — | — | — | 0 | 141.67% | 340.00% | 5765.28% | 244 |
| decay L=256 | shared | 4 Ticks | 45488.81 µs | 199044.92 µs | — | — | — | — | 0 | 205.16% | 600.00% | 6098.61% | 242 |
| decay L=256 | flat | 4 Ticks | 898.29 µs | 2845.91 µs | — | — | — | — | 113 | 0.00% | 0.00% | 0.00% | 248 |
| monocentric L=512 | cache | never | 774.16 µs | 2724.50 µs | 69.77% | 0.00% | 30.22% | — | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 64 | never | 1373.70 µs | 3114.36 µs | 46.04% | 0.00% | 53.95% | 5.16 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 256 | never | 936.39 µs | 2929.04 µs | 63.06% | 0.00% | 36.93% | 1.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 1024 | never | 796.16 µs | 2701.47 µs | 68.11% | 0.00% | 31.88% | 0.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | nexthop | never | 1.17 µs | 2.93 µs | — | — | — | — | 0 | 16.58% | 39.96% | 238.57% | 251 |
| monocentric L=512 | shared | never | 0.54 µs | 1.43 µs | — | — | — | — | 0 | 33.21% | 65.63% | 706.97% | 247 |
| monocentric L=512 | flat | never | 5337.11 µs | 10090.30 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 256 |
| monocentric L=512 | cache | 64 Ticks | 775.69 µs | 2732.35 µs | 69.77% | 0.00% | 30.22% | — | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 64 | 64 Ticks | 1450.07 µs | 3280.89 µs | 46.04% | 0.00% | 53.95% | 5.16 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 256 | 64 Ticks | 960.35 µs | 2755.11 µs | 63.06% | 0.00% | 36.93% | 1.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 1024 | 64 Ticks | 859.42 µs | 2897.51 µs | 68.11% | 0.00% | 31.88% | 0.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | nexthop | 64 Ticks | 14.72 µs | 1475.36 µs | — | — | — | — | 0 | 16.58% | 39.96% | 238.57% | 251 |
| monocentric L=512 | shared | 64 Ticks | 2844.43 µs | 183779.43 µs | — | — | — | — | 0 | 33.21% | 65.63% | 706.97% | 247 |
| monocentric L=512 | flat | 64 Ticks | 5470.91 µs | 10339.20 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 256 |
| monocentric L=512 | cache | 16 Ticks | 877.52 µs | 3410.32 µs | 68.67% | 1.09% | 30.22% | — | 0 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | cache+ttl 64 | 16 Ticks | 1524.25 µs | 5337.90 µs | 45.65% | 0.39% | 53.95% | 5.16 | 0 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | cache+ttl 256 | 16 Ticks | 1054.21 µs | 4609.21 µs | 62.08% | 0.97% | 36.93% | 1.35 | 0 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | cache+ttl 1024 | 16 Ticks | 910.63 µs | 6295.68 µs | 67.13% | 0.97% | 31.88% | 0.35 | 0 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | nexthop | 16 Ticks | 278.75 µs | 42277.13 µs | — | — | — | — | 0 | 16.67% | 43.05% | 238.57% | 249 |
| monocentric L=512 | shared | 16 Ticks | 11354.19 µs | 187895.02 µs | — | — | — | — | 0 | 33.41% | 66.09% | 706.97% | 245 |
| monocentric L=512 | flat | 16 Ticks | 5146.73 µs | 9964.24 µs | — | — | — | — | 21 | 0.00% | 0.00% | 0.00% | 254 |
| monocentric L=512 | cache | 4 Ticks | 1028.46 µs | 3589.58 µs | 67.70% | 2.07% | 30.22% | — | 0 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | cache+ttl 64 | 4 Ticks | 1604.54 µs | 4079.35 µs | 45.26% | 0.78% | 53.95% | 5.16 | 0 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | cache+ttl 256 | 4 Ticks | 1168.67 µs | 3597.16 µs | 61.42% | 1.63% | 36.93% | 1.35 | 0 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | cache+ttl 1024 | 4 Ticks | 1053.82 µs | 3458.84 µs | 66.18% | 1.92% | 31.88% | 0.35 | 0 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | nexthop | 4 Ticks | 590.94 µs | 41184.34 µs | — | — | — | — | 0 | 16.75% | 43.05% | 238.57% | 246 |
| monocentric L=512 | shared | 4 Ticks | 45274.94 µs | 189074.96 µs | — | — | — | — | 0 | 33.60% | 66.09% | 706.97% | 242 |
| monocentric L=512 | flat | 4 Ticks | 5097.40 µs | 10059.11 µs | — | — | — | — | 100 | 0.00% | 0.00% | 0.00% | 251 |

256 Ticks, 16 Trip starts per Tick, drawn with repetition from a pool of 512 distinct origin-destination pairs. The cache rungs hold 1024 entries at 8 Chunks per cluster and revalidate at the **per-Segment** Epoch, which is R5.3's recommendation and the rung R5.4 then found a hole in. The two District-granular rungs run at 121 Districts, which is R2's anchor and R4's. Gestures are 16-Segment drags and **the storm never reverts**, so the graph degrades monotonically across a run and the *Unroutable* column is what says whether a row is measuring severance rather than its rung.

**The *Unroutable* column disagrees between the control and the hierarchy, and the disagreement is a defect in the search rather than in the storm.** Over the whole sweep `flat` reports **416** lookups with no route where the four cache rungs report **16** between them, on the same graph at the same Tick. `HpaSearch` prices the step *onto* the origin Segment and *off* the destination Segment through `SegmentEntry.CostToEndpoint(graph, null, …)` — a **null cost array**, which is free-flow — so when the player bulldozes the Segment a Trip starts on, the flat search correctly finds nothing and **the hierarchy seeds both of that Segment's endpoints anyway and returns a route from a road that is not there**. The confined searches and the abstract edges all read the live costs; it is only the two Access Point remainders that do not. **The defect predates this section** — R5.3 and R5.4 run the same search — and it is recorded rather than corrected here, because correcting it would move R5.3's published hit rates and that is a re-capture and not an edit. What it costs immediately is the *Unroutable* column's meaning on every hierarchical row, which should be read as **zero by construction** rather than as evidence of anything.

**A worst Tick on a sub-microsecond rung is the runtime and not the rung.** The two District-granular rungs answer a Trip with one array read, so their mean Tick is under two microseconds — and their worst Tick at *never*, where nothing is edited at all, still reaches milliseconds, because a collection of the harness's own tens-of-megabytes tables lands inside a timed span. The column is honest about what happened and misleading about what caused it. Read a cheap rung's worst Tick as a bound on this harness; read an expensive rung's as a bound on the design.

**Detour samples dropped: 435, of which 0 were dropped because the route a rung served contains a Segment that no longer exists.** The second figure is the one to read: under a per-Segment Epoch and a deletion-only storm it must be zero, because a route whose own Segment was deleted has a version that moved. A non-zero reading there is a stamping defect and not a result.

**The next-hop table is audited at the end of every storm it survives**, against columns freshly `Seed`ed on the graph the storm left behind — 24,244,044 entries across every next-hop row. **0 wrong cost, 0 stranded.** A maintained table that has quietly stopped maintaining is the fastest rung on the board and every other column looks healthy while it does it; R4 hit exactly that, which is why the check is printed on the run where it passes rather than kept for the run where it fails.

**The rotation is published as a *rate* and never as a period, because the period is the half that does not transfer.** `plans/0010`'s third tripwire is explicit about the form — *a rotation of period N fits while fewer than X refreshes per Tick are forced* — and the reason is arithmetic: a period of 256 Ticks over this harness's 1024-entry cache sweeps 4 slots a Tick, where the same period over a real hot set of a different size is a completely different bill. **The *Forced refreshes / Tick* column is the transferable quantity**: occupied entries actually discarded per Tick, which is what the next lookup on each of those keys pays for as a full search. The hit-rate cost belongs to that rate and not to the period beside it.

**And the 1024-Tick rotation is excluded from every statement about the staleness *bound*.** At 256 Ticks it completes 25.00% of one sweep, so a quarter of the cache is never visited and no entry is guaranteed refreshed within the period. That row prices *the cost of the sweep* and prices it correctly; **it says nothing about the staleness the sweep would buy**, and the bound must not be quoted from it. Measuring the bound needs a run at least one full rotation long, which is a longer capture rather than a different instrument.

**The next-hop rung is priced at R2's charitable reading and the charity should be stated.** R2 gave it *0 ns per Leg at spawn*: a Traveller reads its next hop and drives, with no search anywhere. The tail leg that the detour column composes — from the destination District's representative to the actual destination — is therefore **not charged to the rung's Tick timings**, only to its error. If that tail were a search the rung would pay a flat search per Trip and lose to the control outright, so the *Mean Tick* column for `nexthop` should be read as a floor that assumes the coarse arrival is acceptable, which is exactly the question the detour column is asking.

### R5.5.3 — what the cache *holds*, as against what it *serves*

**The detour column in R5.5.2 is invariant across every edit rate, and the reason is structural rather than a wiring fault — but it also means that column cannot answer the question this section was written for.** Under a per-Segment Epoch a stale entry is *detected* at lookup and recomputed, so what a Trip is **served** is never stale: either the entry was valid, or it was replaced by a fresh search before the Trip saw it. A column that prices what was served therefore prices freshly-computed HPA\* routes at every edit rate, and it must read the same figure at *never* and at four Ticks. It does, to the digit, and **that invariance is a result rather than a silence** — but a column that cannot move with the axis is exactly the shape R3.5's 0.00% wore, and R3.6 is how the corpus learned not to accept one on its own word.

**So this table walks the pool instead of the Trip stream.** Every entry the cache still holds at the end of the storm is priced against a fresh search on the graph as the storm left it, whether or not any Trip asked for it. It is R5.4's instrument pointed at **deletion** rather than at addition, and R5.4's own argument predicts what it must say: deletion is monotone-worsening, so removing an arc that is not on route `R` leaves `R` optimal, and a rung watching only `R`'s own Segments misses nothing. **Predicted zero, and it is measured rather than left as an argument** — which is `adr/0043`'s rule applied to the recommendation R5.3 made rather than to a rung it rejected.

Priced hierarchy against hierarchy, as R5.4 priced it. A flat search on this side would fold the arc-sum residual sized above into the answer and report the instrument's own units as staleness.

| O-D rung | Rung | Edit every | Resident | Declared valid | Improvable | **Wrongly valid** | Mean detour | Worst | Holding a deleted Segment | Not comparable | Identity |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | cache | never | 412 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache+ttl 64 | never | 251 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache+ttl 256 | never | 367 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache+ttl 1024 | never | 402 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache | 64 Ticks | 412 | 99.75% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| uniform | cache+ttl 64 | 64 Ticks | 251 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache+ttl 256 | 64 Ticks | 367 | 99.72% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| uniform | cache+ttl 1024 | 64 Ticks | 402 | 99.75% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| uniform | cache | 16 Ticks | 412 | 98.54% | 0.00% | **0.00%** | — | — | 6 | 0 | holds |
| uniform | cache+ttl 64 | 16 Ticks | 251 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache+ttl 256 | 16 Ticks | 367 | 99.18% | 0.00% | **0.00%** | — | — | 3 | 0 | holds |
| uniform | cache+ttl 1024 | 16 Ticks | 402 | 98.75% | 0.00% | **0.00%** | — | — | 5 | 0 | holds |
| uniform | cache | 4 Ticks | 412 | 96.60% | 0.00% | **0.00%** | — | — | 14 | 0 | holds |
| uniform | cache+ttl 64 | 4 Ticks | 251 | 99.20% | 0.00% | **0.00%** | — | — | 2 | 0 | holds |
| uniform | cache+ttl 256 | 4 Ticks | 367 | 97.27% | 0.00% | **0.00%** | — | — | 10 | 0 | holds |
| uniform | cache+ttl 1024 | 4 Ticks | 402 | 96.76% | 0.00% | **0.00%** | — | — | 13 | 0 | holds |
| decay L=256 | cache | never | 399 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 64 | never | 249 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache+ttl 256 | never | 366 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 1024 | never | 386 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache | 64 Ticks | 399 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 64 | 64 Ticks | 249 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache+ttl 256 | 64 Ticks | 366 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 1024 | 64 Ticks | 386 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache | 16 Ticks | 399 | 99.49% | 0.00% | **0.00%** | — | — | 2 | 3 | holds |
| decay L=256 | cache+ttl 64 | 16 Ticks | 249 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache+ttl 256 | 16 Ticks | 366 | 99.45% | 0.00% | **0.00%** | — | — | 2 | 3 | holds |
| decay L=256 | cache+ttl 1024 | 16 Ticks | 386 | 99.74% | 0.00% | **0.00%** | — | — | 1 | 2 | holds |
| decay L=256 | cache | 4 Ticks | 399 | 97.99% | 0.00% | **0.00%** | — | — | 8 | 3 | holds |
| decay L=256 | cache+ttl 64 | 4 Ticks | 248 | 99.19% | 0.00% | **0.00%** | — | — | 2 | 2 | holds |
| decay L=256 | cache+ttl 256 | 4 Ticks | 365 | 98.63% | 0.00% | **0.00%** | — | — | 5 | 3 | holds |
| decay L=256 | cache+ttl 1024 | 4 Ticks | 386 | 98.18% | 0.00% | **0.00%** | — | — | 7 | 2 | holds |
| monocentric L=512 | cache | never | 398 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | never | 238 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | never | 361 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | never | 383 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache | 64 Ticks | 398 | 99.74% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | 64 Ticks | 238 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | 64 Ticks | 361 | 99.72% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | 64 Ticks | 383 | 99.73% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache | 16 Ticks | 398 | 98.24% | 0.00% | **0.00%** | — | — | 7 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | 16 Ticks | 238 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | 16 Ticks | 361 | 98.89% | 0.00% | **0.00%** | — | — | 4 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | 16 Ticks | 383 | 98.17% | 0.00% | **0.00%** | — | — | 7 | 0 | holds |
| monocentric L=512 | cache | 4 Ticks | 398 | 96.48% | 0.00% | **0.00%** | — | — | 14 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | 4 Ticks | 238 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | 4 Ticks | 361 | 97.50% | 0.00% | **0.00%** | — | — | 9 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | 4 Ticks | 383 | 96.34% | 0.00% | **0.00%** | — | — | 14 | 0 | holds |

*Resident* is entries of the 512-pair pool the cache still holds; *declared valid* is the share of those the per-Segment Epoch passes; *improvable* is the share a fresh search strictly beats, which is **ground truth and independent of the rung**; *wrongly valid* is the intersection. *Not comparable* is resident entries whose fresh search found nothing, excluded from *improvable* and printed so the denominator is visible rather than assumed. **Total wrongly valid across every cache row: 0. Total holding a Segment that no longer exists: 141. Identity breaks: 0 of 48 rows.**

**The *Improvable* column is the load-bearing one and it is the reason this table is weaker than R5.4's.** There it read 9.22% on the Arterial gesture, so the *wrongly valid* columns beside it had something to be wrong about. Here deletion cannot improve a route by construction, so improvable is zero and every rung passes trivially. **That is a confirmation of R5.4's asymmetry argument and not an endorsement of the rung**: it says the per-Segment Epoch is exact under the half of the core verb this storm applies, which is the half R5.3 already recommended it for. **The hole is under addition, it is measured in R5.4, and nothing in R5.5 closes it** — a deletion-only storm cannot, whatever it samples.

**Two things this table does check that R5.5.2 could not, and the first is the sharpest result in the section.** *Holding a deleted Segment* counts resident routes whose own arcs the storm has removed; the rung is allowed to hold those, and declaring one valid would be a stamping defect. **The *Identity* column tests, per row, whether resident minus declared-valid equals that count** — whether the entries the per-Segment Epoch refuses are precisely the entries containing a deleted Segment, neither one more nor one fewer. That is the rung's exactness claim written as an identity between two independently counted columns, and it is a far stronger check than a hit rate, because a hit rate cannot distinguish exactness from luck. **It is printed per row and totalled above, on the run where it holds**, which is the only run on which printing it is worth anything.

**And the *Resident* column is where a rotation that is expiring nothing stops looking like one that is working.** The hit column alone cannot tell those apart — an empty cache and a cache nobody invalidates both read as *no staleness* — whereas resident counts fall monotonically with the forced-refresh rate, which is the rotation leaving a footprint on something other than the column it is meant to move.

**Two caveats travel with every figure here and neither is fixable inside S2.** The hit-rate levels rest on R5.3's invented pool standing in for Trip repetition, which needs Trip generation to replace; and the Street half of R5.4's addition finding reads zero because the synthetic grid is degenerate. The ratios *between* rungs under one pool are what this section is for, and they are what it may be quoted on.

### R5.5.4 — does the rotation actually clear the addition hole

**R5.5.2 prices the rotation's cost and never prices its benefit, and that asymmetry is not publishable.** The hole a TTL exists to close is **addition** — R5.4's finding that a route computed before a road existed cannot contain it, so no version the per-Segment rung watches will ever move and the entry is stale *permanently*. Every storm in R5.5 so far only ever deletes, so nothing in the section has shown a rotation clearing anything. The benefit is arithmetically obvious, and `adr/0043` is exactly the rule that says obvious is not a reason to leave it unmeasured.

**The technique is R5.4's and it is reused rather than reinvented.** The abstract graph is built on the **full** graph so every portal slot is reserved; a set of Segments is deleted; the whole pool is cached against the damaged graph; then the Segments are restored. **Restoration *is* addition**, and it needs no new portal. The gesture is R5.4's Arterial rung — the smallest addition worth drawing, about half a kilometre of new fast road — because R5.4 established that a larger addition cannot do better and the figure is therefore a floor.

**What is new is the window.** After the restoration the cache is run forward with ordinary Trip traffic and a rotation active, and the wrongly-valid population is sampled at points across the window rather than at its end. **The curve is the deliverable**: a rotation period is a stated learning rate, so the quantity a designer sets and a player experiences is how fast this decays, not where it stops.

**The comparison is on whole journey cost, not on arc sums.** That is the correction R5.5.3 had to make and it carries here: comparing arc sums manufactures improvable entries out of two equal-cost routes that enter the destination Segment from opposite endpoints. **This matters for reading the *tick 0* row against R5.4's published 9.22%**, which was measured on arc sums.

| Rotation | Forced refreshes / Tick | Ticks since addition | Resident | Declared valid | Improvable | **Wrongly valid** | **Count** | Mean detour | Worst | Not comparable |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| **none** | 0.00 | 0 | 412 | 100.00% | 9.22% | **9.22%** | **38** | 16.35% | 62.41% | 0 |
| **none** | 0.00 | 16 | 412 | 100.00% | 7.03% | **7.03%** | **29** | 18.31% | 62.41% | 0 |
| **none** | 0.00 | 64 | 412 | 100.00% | 5.58% | **5.58%** | **23** | 19.31% | 62.41% | 0 |
| **none** | 0.00 | 128 | 412 | 100.00% | 5.58% | **5.58%** | **23** | 19.31% | 62.41% | 0 |
| **none** | 0.00 | 256 | 412 | 100.00% | 5.58% | **5.58%** | **23** | 19.31% | 62.41% | 0 |
| **none** | 0.00 | 512 | 412 | 100.00% | 5.58% | **5.58%** | **23** | 19.31% | 62.41% | 0 |
| **none** | 0.00 | 768 | 412 | 100.00% | 5.58% | **5.58%** | **23** | 19.31% | 62.41% | 0 |
| **none** | 0.00 | 1024 | 412 | 100.00% | 5.58% | **5.58%** | **23** | 19.31% | 62.41% | 0 |
| every 64 | 5.79 | 0 | 412 | 100.00% | 9.22% | **9.22%** | **38** | 16.35% | 62.41% | 0 |
| every 64 | 5.79 | 16 | 337 | 100.00% | 7.12% | **7.12%** | **24** | 19.81% | 62.41% | 0 |
| every 64 | 5.79 | 64 | 258 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 64 | 5.79 | 128 | 243 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 64 | 5.79 | 256 | 251 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 64 | 5.79 | 512 | 252 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 64 | 5.79 | 768 | 257 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 64 | 5.79 | 1024 | 246 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 256 | 1.60 | 0 | 412 | 100.00% | 9.22% | **9.22%** | **38** | 16.35% | 62.41% | 0 |
| every 256 | 1.60 | 16 | 400 | 100.00% | 6.25% | **6.25%** | **25** | 20.30% | 62.41% | 0 |
| every 256 | 1.60 | 64 | 375 | 100.00% | 4.80% | **4.80%** | **18** | 21.59% | 62.41% | 0 |
| every 256 | 1.60 | 128 | 362 | 100.00% | 2.76% | **2.76%** | **10** | 21.30% | 62.41% | 0 |
| every 256 | 1.60 | 256 | 367 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 256 | 1.60 | 512 | 359 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 256 | 1.60 | 768 | 366 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 256 | 1.60 | 1024 | 359 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |
| every 1024 | 0.40 | 0 | 412 | 100.00% | 9.22% | **9.22%** | **38** | 16.35% | 62.41% | 0 |
| every 1024 | 0.40 | 16 | 409 | 100.00% | 6.60% | **6.60%** | **27** | 19.03% | 62.41% | 0 |
| every 1024 | 0.40 | 64 | 404 | 100.00% | 4.70% | **4.70%** | **19** | 22.14% | 62.41% | 0 |
| every 1024 | 0.40 | 128 | 405 | 100.00% | 4.44% | **4.44%** | **18** | 21.59% | 62.41% | 0 |
| every 1024 | 0.40 | 256 | 402 | 100.00% | 4.47% | **4.47%** | **18** | 21.59% | 62.41% | 0 |
| every 1024 | 0.40 | 512 | 401 | 100.00% | 2.49% | **2.49%** | **10** | 21.30% | 62.41% | 0 |
| every 1024 | 0.40 | 768 | 396 | 100.00% | 0.75% | **0.75%** | **3** | 25.89% | 62.41% | 0 |
| every 1024 | 0.40 | 1024 | 400 | 100.00% | 0.00% | **0.00%** | **0** | — | — | 0 |

A pool of 512 uniform origin-destination pairs cached on the damaged graph, the Arterial gesture restored, then 1024 Ticks of 16 Trip starts each. **The window is one full sweep of the longest rotation on the ladder**, so every rate here is entitled to a statement about the staleness bound — which is what R5.5.2's 1024-Tick row explicitly was not. *Not comparable* is resident entries whose fresh search found nothing, excluded from the shares and printed so the denominator is visible.

**Read the *Count* column, not the share, and the reason is a trap this table was built to walk into.** A rotation evicts, and an evicted entry leaves the denominator as well as the numerator — so a wrongly-valid *share* can fall while not one stale route has been replaced by a correct one, purely because the population shrank underneath it. The absolute count cannot be got at that way. **The *Resident* column is printed beside it for exactly this reason**: a count falling while resident holds steady is entries being relearned, and a count falling in step with resident is entries being thrown away.

**The control is the check that the instrument can move, and it reads 38 wrongly-valid entries at the addition and 23 after 1024 Ticks with no rotation.** R5.4's claim is that this error has no mechanism that will ever notice it — the road the route should be using is one the route does not contain, so no version it watches will move again — and that **only eviction removes it**. A control that decayed to nothing on its own would mean R5.4's *does not heal* is wrong, which is a larger finding than this subsection and would be reported as one rather than published as a rotation's success.

**It plateaus, and the plateau is the finding.** The control falls to 23 entries by Tick 64 and then **does not move again for the remaining 960 Ticks**, with its resident population constant at 412 throughout. **That is R5.4's *does not heal* measured rather than argued**: 60.52% of the error survives every Tick this window contains, and the flatness is what distinguishes a permanent error from a slow one. A curve still descending at the right-hand edge would have meant the window was too short to conclude anything; this one stopped descending at Tick 64 and stayed stopped.

**Which row the conclusion rests on, stated rather than left to the reader.** A rotation can drive the wrongly-valid count to zero two ways — by teaching entries the new road, or by throwing them away — and only the first is closing the hole. The two are told apart by what happens to the resident population beside the count:

| Rotation | Forced refreshes / Tick | Wrongly valid cleared by | Resident retained | Verdict |
|---|---:|---:|---:|---|
| **none** | 0.00 | **never** — plateau at 23 | 100.00% | the hole is permanent |
| every 64 | 5.79 | Tick 64 | 59.70% | cleared partly by discarding the cache |
| every 256 | 1.60 | Tick 256 | 87.13% | relearned, at a visible cost in resident entries |
| every 1024 | 0.40 | Tick 1024 | 97.08% | **relearned** — the count fell and the population did not |

**The slowest rotation on the ladder is the one that settles it, and it settles it cheaply.** At 0.40 forced refreshes a Tick the whole hole is gone within one rotation period while the cache keeps 97% of its resident entries — so the count cannot have fallen because the denominator did, and the entries really were taught the new road rather than discarded. **The fastest rotation clears it sooner and sheds 40% of the cache doing it**, which is the same bill R5.5.2 charges as 25 points of hit rate. The conclusion rests on the slow row; the fast row is the one tripwire 4 was written about and it is exactly the row that cannot carry it.

**The error that survives is worse than the error that clears, and that is not reassuring.** The control's mean detour *rises* from 16.35% to 19.31% as its count falls from 38 to 23 — collision eviction is removing the mild errors and leaving the severe ones. It is the same mechanism `adr/0012` predicts from the other end: keyed by origin-destination pair rather than by agent, **a hot pair is the least likely to be evicted precisely because it is hot**, so what persists is every driver's route on the busiest pairs. A residual quoted as a count understates it; the surviving entries are the ones carrying the most traffic and the largest detour.

**Tick 0 is a re-measurement of R5.4 and it agrees, which also bounds the units worry.** R5.4 published **9.22% improvable, 16.71% mean detour, 62.65% worst** on this gesture, measured on **arc sums**; the row above reads **9.22%** improvable with a mean of **16.35%** and a worst of **62.41%**, measured on whole journey cost. The improvable share is identical to the digit and the detours move by about a third of a percentage point. **So R5.4's figures carry the arc-sum residual this section sized, and carry it at the level of rounding rather than as a factor** — which is worth stating explicitly, because R5.5.3 found the same residual manufacturing a *wrongly valid* entry out of nothing and the two outcomes could otherwise be read as contradicting each other.

**The residual decay in the control is the direct-mapped cache's collision eviction and not the Epoch noticing anything.** R5.3 measured the miss column flat at 28–31% regardless of edit rate and identified it as collisions rather than staleness; a colliding pair evicts whatever shares its slot, and the replacement is searched on the graph as it now is, so it comes back correct. That is precisely the *only* removal mechanism R5.4 named. It is reported here rather than subtracted, because the honest comparison for every rotation row is against a control that has the same collisions and differs only in the rotation.

**And `adr/0012` is why the collisions do not rescue the design.** The cache is keyed by origin-destination pair rather than by agent, so an entry is not one driver's habit but every driver's route — and **a hot pair is the least likely to be evicted precisely because it is hot**. Collision eviction clears the entries nobody is using. The rotation is the only mechanism on this table that touches an entry *because time has passed* rather than because something else wanted its slot.



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 153.05 s — from 15:19:17 UTC to 15:21:50 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 0.62 / 0.80 / 0.92 (1 / 5 / 15 min)
- **Load average, at end** 1.03 / 0.93 / 0.96 (1 / 5 / 15 min)
- **CPU stall** 176,697 µs over the run — 0.11% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 26,354 µs over the run — 0.01% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
