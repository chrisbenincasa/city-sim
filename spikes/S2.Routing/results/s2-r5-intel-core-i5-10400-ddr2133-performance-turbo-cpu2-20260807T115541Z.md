## S2 R5 — the edit storm, and the Epoch ladder

- **Captured** 2026-08-07 11:55:41 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 1 logical processors
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

- Full abstract-graph build at **8 Chunks**: 214.94 ms measured first, 43.99 ms measured last, 4.88× apart.
- Full abstract-graph build at **16 Chunks**: 81.24 ms measured first, 76.13 ms measured last, 1.06× apart.

**The *repair ÷ rebuild* column is the one a decision rests on, and it is the column R5's first draft got wrong.** That draft measured the rebuild at 8 Chunks once and divided the 16-Chunk repair figures by it — a denominator from a different experiment wearing the right units. A rebuild at 16 Chunks is a different amount of work from a rebuild at 8, so every ratio in the second half of the table was against a partition that was not the one being repaired. **This is R3's denominator finding arriving a fourth time**, in the one form it had not yet taken: not measured once instead of twice, but measured on the wrong rung.

**Coalesced against naive is the finding, not an implementation note.** A cluster's edge set is a function of its arcs, so it has to be decided once however many Segments inside it were deleted. The naive column is what a per-Segment repair loop costs — the spelling R3 and R4 measured, which is correct and indistinguishable from the coalesced one at a gesture of 1.

| Cluster | Gesture | Asked | Got | Clusters | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced | Coalesced as % of rebuild |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 8 | drag | 1 | 1 | 1 | 1.07 ms | 1.31 ms | 0.86 ms | 1.30 ms | 0.80× | 0.50% |
| 8 | drag | 4 | 4 | 1 | 1.12 ms | 1.66 ms | 3.62 ms | 4.30 ms | 3.22× | 0.52% |
| 8 | drag | 16 | 16 | 2 | 2.24 ms | 2.55 ms | 11.33 ms | 13.62 ms | 5.05× | 1.04% |
| 8 | drag | 64 | 64 | 8 | 6.71 ms | 8.86 ms | 44.95 ms | 55.96 ms | 6.69× | 3.12% |
| 8 | drag | 256 | 173 | 15 | 10.72 ms | 16.68 ms | 97.69 ms | 186.08 ms | 9.10× | 4.98% |
| 8 | scattered | 1 | 1 | 1 | 0.41 ms | 0.92 ms | 0.25 ms | 0.28 ms | 0.61× | 0.19% |
| 8 | scattered | 4 | 4 | 4 | 1.21 ms | 1.93 ms | 1.58 ms | 2.92 ms | 1.31× | 0.56% |
| 8 | scattered | 16 | 16 | 17 | 4.88 ms | 5.97 ms | 5.80 ms | 9.60 ms | 1.18× | 2.27% |
| 8 | scattered | 64 | 64 | 62 | 20.31 ms | 22.81 ms | 21.39 ms | 29.05 ms | 1.05× | 9.45% |
| 8 | scattered | 256 | 256 | 172 | 52.91 ms | 59.35 ms | 79.34 ms | 96.83 ms | 1.49× | 24.61% |
| 16 | drag | 1 | 1 | 1 | 1.31 ms | 2.36 ms | 1.05 ms | 1.60 ms | 0.80× | 1.62% |
| 16 | drag | 4 | 4 | 1 | 1.57 ms | 2.92 ms | 4.41 ms | 6.64 ms | 2.80× | 1.93% |
| 16 | drag | 16 | 16 | 2 | 3.30 ms | 4.59 ms | 23.30 ms | 38.30 ms | 7.05× | 4.06% |
| 16 | drag | 64 | 64 | 4 | 6.02 ms | 10.67 ms | 71.56 ms | 85.00 ms | 11.87× | 7.41% |
| 16 | drag | 256 | 173 | 7 | 6.52 ms | 11.21 ms | 179.98 ms | 275.05 ms | 27.58× | 8.02% |
| 16 | scattered | 1 | 1 | 1 | 1.42 ms | 2.26 ms | 1.45 ms | 2.30 ms | 1.01× | 1.75% |
| 16 | scattered | 4 | 4 | 3 | 5.49 ms | 6.39 ms | 4.95 ms | 5.92 ms | 0.90× | 6.76% |
| 16 | scattered | 16 | 16 | 14 | 17.26 ms | 20.09 ms | 19.36 ms | 21.44 ms | 1.12× | 21.25% |
| 16 | scattered | 64 | 64 | 41 | 50.53 ms | 59.99 ms | 83.62 ms | 88.15 ms | 1.65× | 62.19% |
| 16 | scattered | 256 | 256 | 63 | 78.50 ms | 97.83 ms | 324.71 ms | 344.63 ms | 4.13× | 96.62% |

8 gestures per row, each applied, repaired, reverted and repaired again, so every rung starts and ends on the same abstract graph and the timed figure is one repair. **Worst is the worst single gesture, not a quantile** — a gesture is one player action and a quantile over eight of them would hide the event S4's K6 was about.

### R5.3 — the Epoch ladder, and what *never a global flush* is worth

**`CONTEXT.md` → Epoch commits to lazy revalidation and *never a global flush*, and the Epoch as written is a single counter on the whole Road Graph.** A counter carries no location, so a route computed at Epoch 5 and used at Epoch 6 cannot tell whether the edit touched it. **`Lazy` describes when you pay, not what survives** — under one counter the answer to *what survives* is *nothing*, and the flush is total however lazily it is paid. This section prices the two rungs that carry a location.

**The zero-edit row is the instrument check, and it is why it is in the table.** With no edits every rung must read a near-total hit rate, because the pool is smaller than the cache and nothing invalidates. A rung reading low there has a broken cache rather than a strict Epoch, and R2 published byte-identical peaks from exactly that kind of silence. Under the global rung hit rate is **not a property of the O-D draw at all** — it is a property of how recently the player touched anything — so a throughput figure could be reported with a cache that had quietly stopped working.

| O-D rung | Epoch | Edit every | Deleted | Hit | Stale | Miss | Unroutable | Revalidation words | Mean Tick | Worst Tick |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | global | never | 0 | 71.63% | 0.00% | 28.36% | 0.00% | 0.71 | 3267.74 µs | 13025.18 µs |
| uniform | per-cluster | never | 0 | 71.63% | 0.00% | 28.36% | 0.00% | 7.56 | 3191.14 µs | 11557.38 µs |
| uniform | per-Segment | never | 0 | 71.63% | 0.00% | 28.36% | 0.00% | 41.97 | 3105.66 µs | 16601.04 µs |
| uniform | global | 64 Ticks | 64 | 49.36% | 22.26% | 28.36% | 0.00% | 0.71 | 2182.89 µs | 6973.08 µs |
| uniform | per-cluster | 64 Ticks | 64 | 70.62% | 1.00% | 28.36% | 0.00% | 7.56 | 1415.84 µs | 4994.17 µs |
| uniform | per-Segment | 64 Ticks | 64 | 71.60% | 0.02% | 28.36% | 0.00% | 41.97 | 1306.06 µs | 5936.53 µs |
| uniform | global | 16 Ticks | 256 | 20.87% | 50.75% | 28.36% | 0.00% | 0.71 | 3241.97 µs | 6933.41 µs |
| uniform | per-cluster | 16 Ticks | 256 | 66.25% | 5.37% | 28.36% | 0.00% | 7.60 | 1464.54 µs | 5218.92 µs |
| uniform | per-Segment | 16 Ticks | 256 | 70.19% | 1.44% | 28.36% | 0.00% | 42.79 | 1216.16 µs | 5100.40 µs |
| uniform | global | 4 Ticks | 1021 | 6.59% | 65.03% | 28.36% | 0.00% | 0.71 | 3834.77 µs | 8881.54 µs |
| uniform | per-cluster | 4 Ticks | 1021 | 57.49% | 14.13% | 28.36% | 0.00% | 7.64 | 1854.11 µs | 4800.69 µs |
| uniform | per-Segment | 4 Ticks | 1021 | 68.99% | 2.63% | 28.36% | 0.00% | 42.80 | 1431.53 µs | 5652.23 µs |
| decay L=256 | global | never | 0 | 69.31% | 0.00% | 30.68% | 0.00% | 0.69 | 207.65 µs | 910.03 µs |
| decay L=256 | per-cluster | never | 0 | 69.31% | 0.00% | 30.68% | 0.00% | 2.55 | 207.27 µs | 932.75 µs |
| decay L=256 | per-Segment | never | 0 | 69.31% | 0.00% | 30.68% | 0.00% | 15.20 | 204.33 µs | 1051.74 µs |
| decay L=256 | global | 64 Ticks | 64 | 47.99% | 21.31% | 30.68% | 0.00% | 0.69 | 361.01 µs | 2877.55 µs |
| decay L=256 | per-cluster | 64 Ticks | 64 | 68.53% | 0.78% | 30.68% | 0.00% | 2.55 | 225.24 µs | 1516.67 µs |
| decay L=256 | per-Segment | 64 Ticks | 64 | 69.21% | 0.09% | 30.68% | 0.00% | 15.20 | 215.28 µs | 1474.05 µs |
| decay L=256 | global | 16 Ticks | 256 | 20.50% | 48.80% | 30.68% | 0.00% | 0.69 | 579.70 µs | 3171.22 µs |
| decay L=256 | per-cluster | 16 Ticks | 256 | 66.40% | 2.90% | 30.68% | 0.00% | 2.56 | 372.32 µs | 2955.65 µs |
| decay L=256 | per-Segment | 16 Ticks | 256 | 68.57% | 0.73% | 30.68% | 0.00% | 15.35 | 281.99 µs | 2903.47 µs |
| decay L=256 | global | 4 Ticks | 1021 | 6.51% | 62.71% | 30.76% | 0.09% | 0.69 | 1176.42 µs | 3197.04 µs |
| decay L=256 | per-cluster | 4 Ticks | 1021 | 61.27% | 7.95% | 30.76% | 0.09% | 2.56 | 597.55 µs | 3072.91 µs |
| decay L=256 | per-Segment | 4 Ticks | 1021 | 67.84% | 1.39% | 30.76% | 0.09% | 15.36 | 557.04 µs | 3090.17 µs |
| monocentric L=512 | global | never | 0 | 69.77% | 0.00% | 30.22% | 0.00% | 0.69 | 893.02 µs | 3287.71 µs |
| monocentric L=512 | per-cluster | never | 0 | 69.77% | 0.00% | 30.22% | 0.00% | 6.55 | 900.93 µs | 3106.86 µs |
| monocentric L=512 | per-Segment | never | 0 | 69.77% | 0.00% | 30.22% | 0.00% | 37.58 | 793.96 µs | 2795.74 µs |
| monocentric L=512 | global | 64 Ticks | 64 | 48.63% | 21.14% | 30.22% | 0.00% | 0.69 | 1570.96 µs | 5707.81 µs |
| monocentric L=512 | per-cluster | 64 Ticks | 64 | 69.16% | 0.61% | 30.22% | 0.00% | 6.55 | 1048.36 µs | 4792.00 µs |
| monocentric L=512 | per-Segment | 64 Ticks | 64 | 69.77% | 0.00% | 30.22% | 0.00% | 37.58 | 1133.68 µs | 4136.66 µs |
| monocentric L=512 | global | 16 Ticks | 256 | 20.94% | 48.82% | 30.22% | 0.00% | 0.69 | 2629.29 µs | 7448.12 µs |
| monocentric L=512 | per-cluster | 16 Ticks | 256 | 65.42% | 4.34% | 30.22% | 0.00% | 6.55 | 1252.92 µs | 5529.86 µs |
| monocentric L=512 | per-Segment | 16 Ticks | 256 | 68.67% | 1.09% | 30.22% | 0.00% | 37.85 | 1072.04 µs | 5861.60 µs |
| monocentric L=512 | global | 4 Ticks | 1021 | 6.44% | 63.33% | 30.22% | 0.00% | 0.69 | 3052.99 µs | 7497.34 µs |
| monocentric L=512 | per-cluster | 4 Ticks | 1021 | 57.10% | 12.67% | 30.22% | 0.00% | 6.56 | 1533.02 µs | 4138.26 µs |
| monocentric L=512 | per-Segment | 4 Ticks | 1021 | 67.70% | 2.07% | 30.22% | 0.00% | 37.87 | 1292.97 µs | 8674.37 µs |

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


---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 40.20 s — from 11:55:41 UTC to 11:56:21 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 2.44 / 2.41 / 2.35 (1 / 5 / 15 min)
- **Load average, at end** 2.95 / 2.54 / 2.40 (1 / 5 / 15 min)
- **CPU stall** 1,481,741 µs over the run — 3.68% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 12,525 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
