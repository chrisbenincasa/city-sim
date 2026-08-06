## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-06 20:21:13 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 1 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** powersave
- **Build** Release

Working rung: block 32 Tiles, 8 Arterials, 33,018 Segments, 16,697 nodes, 66,036 arcs. Free-flow car costs, `Chebyshev`, the query shape and the heuristic R0 published against.

**Every figure below is wall-clock.** Expansions appear beside the clock as a work column and never in place of it — R0 measured a case where the two disagree, and *nodes expanded* is the currency HPA\* results are conventionally quoted in.

The reverse index the goal insertion needs is 581.13 KiB and is a property of the Road Graph rather than of the partition, so it is stated once here and kept out of every resident-size column below.

### R3.1 — the partition, and what `adr/0014` was claiming

| Chunks | Cluster | Clusters | Largest | Portals | Portals each | Abstract edges | Resident |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 32 Tiles | 16,384 | 4 nodes | 16,694 | 1 | 64,142 | 1011.31 KiB |
| 1, reduced | 32 Tiles | 16,384 | 4 nodes | 16,694 | 1 | 64,134 | 1011.22 KiB |
| 1, reduced + paths | 32 Tiles | 16,384 | 4 nodes | 16,694 | 1 | 64,134 | 1.47 MiB |
| 2 | 64 Tiles | 4,096 | 9 nodes | 16,482 | 4 | 80,146 | 1.12 MiB |
| 2, reduced | 64 Tiles | 4,096 | 9 nodes | 16,482 | 4 | 63,356 | 952.44 KiB |
| 2, reduced + paths | 64 Tiles | 4,096 | 9 nodes | 16,482 | 4 | 63,356 | 1.53 MiB |
| 4 | 128 Tiles | 1,024 | 25 nodes | 11,875 | 11 | 133,116 | 1.68 MiB |
| 4, reduced | 128 Tiles | 1,024 | 25 nodes | 11,875 | 11 | 45,236 | 692.11 KiB |
| 4, reduced + paths | 128 Tiles | 1,024 | 25 nodes | 11,875 | 11 | 45,236 | 1.18 MiB |
| 8 | 256 Tiles | 256 | 81 nodes | 6,665 | 26 | 151,350 | 1.84 MiB |
| 8, reduced | 256 Tiles | 256 | 81 nodes | 6,665 | 26 | 24,586 | 406.41 KiB |
| 8, reduced + paths | 256 Tiles | 256 | 81 nodes | 6,665 | 26 | 24,586 | 765.43 KiB |
| 16 | 512 Tiles | 64 | 291 nodes | 3,337 | 52 | 133,816 | 1.62 MiB |
| 16, reduced | 512 Tiles | 64 | 291 nodes | 3,337 | 52 | 11,768 | 229.45 KiB |
| 16, reduced + paths | 512 Tiles | 64 | 291 nodes | 3,337 | 52 | 11,768 | 453.37 KiB |
| 32 | 1,024 Tiles | 16 | 1,097 nodes | 1,481 | 92 | 101,464 | 1.23 MiB |
| 32, reduced | 1,024 Tiles | 16 | 1,097 nodes | 1,481 | 92 | 4,832 | 133.48 KiB |
| 32, reduced + paths | 1,024 Tiles | 16 | 1,097 nodes | 1,481 | 92 | 4,832 | 233.61 KiB |
| 64 | 2,048 Tiles | 4 | 4,248 nodes | 518 | 129 | 62,126 | 797.33 KiB |
| 64, reduced | 2,048 Tiles | 4 | 4,248 nodes | 518 | 129 | 1,678 | 88.95 KiB |
| 64, reduced + paths | 2,048 Tiles | 4 | 4,248 nodes | 518 | 129 | 1,678 | 136.07 KiB |

*Largest* is the node count of the fullest cluster — the bound on one insertion search, and the reason the query column below turns back up. The graph has 16,697 nodes, so the portal column is also the share of the network the abstract graph re-describes.

**The bottom rung is `adr/0014`'s claim taken literally** — *"the Chunk grid is already the pathfinding cluster"* — at Phase 1's provisional Chunk = Cell. `05 §5` predicted the pathfinding role wants *larger, and loudly*, at 32×32.

### R3.2 — preprocessing, in flat searches

Priced in flat searches rather than in milliseconds alone, because that is the question: preprocessing is only affordable if the queries it saves outnumber it. `adr/0040` makes the abstract graph `(derived AND rebuilt)`, so this cost is paid on every load and after every change to the cluster size — never amortised into a save.

| Chunks | Cold build | Nodes settled | Per cluster | In flat searches |
|---:|---:|---:|---:|---:|
| 1 | 3.70 ms | 17,505 | 225 ns | 7 |
| 1, reduced | 3.39 ms | 17,505 | 207 ns | 7 |
| 1, reduced + paths | 4.63 ms | 18,568 | 283 ns | 10 |
| 2 | 4.32 ms | 68,766 | 1,055 ns | 9 |
| 2, reduced | 3.71 ms | 68,766 | 907 ns | 8 |
| 2, reduced + paths | 7.16 ms | 134,292 | 1,748 ns | 15 |
| 4 | 11.58 ms | 206,142 | 11,309 ns | 24 |
| 4, reduced | 9.43 ms | 206,142 | 9,211 ns | 20 |
| 4, reduced + paths | 19.21 ms | 387,122 | 18,763 ns | 41 |
| 8 | 33.49 ms | 574,207 | 130,834 ns | 72 |
| 8, reduced | 36.46 ms | 574,207 | 142,446 ns | 78 |
| 8, reduced + paths | 57.91 ms | 949,587 | 226,249 ns | 124 |
| 16 | 41.00 ms | 2,014,227 | 640,742 ns | 88 |
| 16, reduced | 44.44 ms | 2,014,227 | 694,457 ns | 95 |
| 16, reduced + paths | 78.95 ms | 2,682,823 | 1,233,711 ns | 170 |
| 32 | 71.10 ms | 9,861,622 | 4,443,872 ns | 153 |
| 32, reduced | 73.58 ms | 9,861,622 | 4,599,359 ns | 158 |
| 32, reduced + paths | 136.68 ms | 10,989,761 | 8,542,696 ns | 294 |
| 64 | 166.85 ms | 67,626,483 | 41,713,573 ns | 359 |
| 64, reduced | 156.77 ms | 67,626,483 | 39,193,383 ns | 338 |
| 64, reduced + paths | 319.44 ms | 69,693,374 | 79,861,067 ns | 688 |

One flat `Chebyshev` drive search in this process: **463,702 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **463,702 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 453,484 ns | 1.02× | 589,719 ns | 0.78× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 477,502 ns | 0.97× | 478,918 ns | 0.96× | 4,138 + 4 | 15,961 + 16 | 58 |
| 1, reduced + paths | 468,687 ns | 0.98× | 480,470 ns | 0.96× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 512,987 ns | 0.90× | 644,735 ns | 0.71× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 496,571 ns | 0.93× | 534,775 ns | 0.86× | 4,089 + 12 | 15,780 + 48 | 58 |
| 2, reduced + paths | 468,303 ns | 0.99× | 510,185 ns | 0.90× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 546,213 ns | 0.84× | 559,602 ns | 0.82× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 357,022 ns | 1.29× | 420,010 ns | 1.10× | 2,988 + 38 | 11,442 + 156 | 58 |
| 4, reduced + paths | 364,855 ns | 1.27× | 392,979 ns | 1.17× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 594,068 ns | 0.78× | 484,368 ns | 0.95× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 245,285 ns | 1.89× | 314,297 ns | 1.47× | 1,708 + 131 | 6,351 + 527 | 58 |
| 8, reduced + paths | 228,885 ns | 2.02× | 242,835 ns | 1.90× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 337,404 ns | 1.37× | 385,547 ns | 1.20× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 143,732 ns | 3.22× | 302,060 ns | 1.53× | 886 + 464 | 3,143 + 1,852 | 58 |
| 16, reduced + paths | 141,648 ns | 3.27× | 149,871 ns | 3.09× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 372,861 ns | 1.24× | 426,283 ns | 1.08× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 217,974 ns | 2.12× | 534,252 ns | 0.86× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 32, reduced + paths | 169,608 ns | 2.73× | 193,256 ns | 2.39× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 831,818 ns | 0.55× | 1,003,181 ns | 0.46× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 826,990 ns | 0.56× | 1,371,486 ns | 0.33× | 166 + 8,360 | 544 + 33,095 | 58 |
| 64, reduced + paths | 748,619 ns | 0.61× | 764,609 ns | 0.60× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **1,428,136 ns**, second **463,702 ns** — a spread of 207.98%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — *one Leg against 530–574 arrivals per Tick, ~400 ms of searching per 15.60 ms of Tick budget* — and that test applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to consume the whole budget on its own.

| Rung | Per route | × arrivals/Tick | Against the budget | Fits |
|---|---:|---:|---:|---|
| **flat** | 463,702 ns | 255.03 ms | 16.34× over | no |
| 1, reduced + paths | 480,470 ns | 264.25 ms | 16.93× over | no |
| 2, reduced + paths | 510,185 ns | 280.60 ms | 17.98× over | no |
| 4, reduced + paths | 392,979 ns | 216.13 ms | 13.85× over | no |
| 8, reduced + paths | 242,835 ns | 133.55 ms | 8.56× over | no |
| 16, reduced + paths | 149,871 ns | 82.42 ms | 5.28× over | no |
| 32, reduced + paths | 193,256 ns | 106.29 ms | 6.81× over | no |
| 64, reduced + paths | 764,609 ns | 420.53 ms | 26.95× over | no |

**No cluster size fits, and the shape of the curve says none can.** The load is U-shaped in cluster size and both ends are pinned by the same thing: a small cluster makes the abstract search approach the flat search, a large one makes the *insertion* approach it. `adr/0040` admits only whole-Chunk clusters that tile the map, so the admissible rungs are the divisors of 128 and the minimum sits at one of them with its two neighbours worse. **This is a floor, not a rung that was missed.**

**Two exits, and neither is free.** A **cache** — `adr/0012` permits one keyed by origin-destination pair, and `plans/0010` R6 owns it — would have to reach roughly a **90% hit rate** to fit routing into half a Tick at the best rung. **That makes R6 load-bearing rather than an optimisation.** Or **threads**: invariant 4 is thread-count equivalence, so the best rung's load spread over eight cores fits — by spending the whole Tick budget of eight cores on routing, which is a mortgage rather than a solution.

**R2's next-hop table is the rung this arithmetic does not touch**, because it does no per-Trip search at all — 0 ns to start a Trip and 32 ns per crossing. That is a structural advantage over both hierarchies rather than a faster constant, and it is **R4's** to press.

### R3.5 — the detour, because a different route is a different city

HPA\* returns the best route that **respects the partition**, which is never cheaper than the flat optimum and is sometimes dearer. R2 established the detour as this spike's correctness currency and measured 18.52% for a next-hop table against 36.01% for a shared District route; those are the figures this column stands beside. **The mean is over every query compared, optimal ones included at zero**, which is what makes it the same quantity R2 published rather than a mean over survivors.

| Chunks | Optimal | Mean detour | Worst detour | Cheaper than flat | Compared | Audited | Audit failures |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 1, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 1, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 2 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 2, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 2, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 4 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 4, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 4, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 8 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 8, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 8, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 16 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 16, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 16, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 32 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 32, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 32, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 64 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 64, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 64, reduced + paths | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |

**Two columns here are printed to read zero.** *Cheaper than flat* would mean the hierarchy had found a route the unconstrained search could not, which is not a shortcut but a bug — and its natural hiding place is the detour mean. *Audit failures* re-walks a sample of refined routes and requires the entry partial, the arc costs and the exit remainder to sum **exactly** to the cost the query reported, with the arcs forming an unbroken chain. R2's harness published a peak `v/c` of 883× with every other column healthy, and the check that would have caught it had been specified in advance and not run.

### R3.6 — the sparser abstraction, at 16 Chunks

**Keeping every crossing is what makes the abstraction complete, and completeness is what R3.5's zero is made of.** Botea's HPA\* keeps one or two transitions per entrance and accepts a detour for it. This is that lever, swept at the cluster size R3.3 makes the best of — chosen from the measurement rather than in advance.

| Transitions | Portals | Abstract edges | Edges each | Cold build | Query | vs flat | Optimal | Mean detour | Worst |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 206 | 552 | 2 | 5.37 ms | 45,423 ns | 10.20× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 6.60 ms | 60,128 ns | 7.71× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 12.15 ms | 98,814 ns | 4.69× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 22.92 ms | 188,401 ns | 2.46× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 43.00 ms | 334,856 ns | 1.38× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 76.42 ms | 148,519 ns | 3.12× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted, the abstract graph **repaired rather than rebuilt**: only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Chunks | Repair | Clusters touched | Share of cold build | Repairs in one build | Edits |
|---:|---:|---:|---:|---:|---:|
| 1 | 1,622 ns | 1.96 | 0.04% | 2,282 | 32 |
| 2 | 1,297 ns | 1.37 | 0.02% | 3,334 | 32 |
| 4 | 9,670 ns | 1.06 | 0.08% | 1,197 | 32 |
| 8 | 113,273 ns | 1.03 | 0.33% | 295 | 32 |
| 16 | 555,300 ns | 1.03 | 1.35% | 73 | 32 |
| 32 | 4,065,523 ns | 1.03 | 5.71% | 17 | 32 |
| 64 | 41,814,852 ns | 1.00 | 25.06% | 3 | 32 |

**Deletion only, and the limit is structural rather than an omission.** The edge slots are fixed at build, so a repair may re-cost an edge and may cost it out of existence — but it cannot create a portal that did not exist, which is what *drawing* a road across a cluster boundary does. R5's edit storm is where the drawing half belongs.

### R3.8 — the bypass, and how local a walk Leg actually is

`plans/0010` makes the same-Segment and adjacent-Segment bypass **mandatory rather than an optimisation**: with five Buildings on a Segment, a share of walk Legs never leave their own Segment or its neighbour, and routing those through the abstract graph costs more than the answer.

**The share per Tick is not measurable here, and reporting one would be a guess wearing a measurement's clothes.** S2 has no Leg distribution — R0 said so in its own sampler and bucketed everything instead, so that whatever distribution arrives later is applied as *weights over buckets that already exist*. This table is that bucketing. Read it as: *of Legs whose origin and destination are this far apart, this share never enters the hierarchy.*

**walk**, 20,000 drawn Legs.

| O-D distance | Legs | Same Segment | Adjacent | Bypassed |
|---|---:|---:|---:|---:|
| ≤ 32 Tiles (one block) | 175 | 15.42% | 62.85% | 78.28% |
| ≤ 64 | 341 | 0.00% | 1.75% | 1.75% |
| ≤ 128 | 1,717 | 0.00% | 0.00% | 0.00% |
| ≤ 256 (1 km) | 6,296 | 0.00% | 0.00% | 0.00% |
| ≤ 512 (2 km) | 11,419 | 0.00% | 0.00% | 0.00% |
| ≤ 1,024 (4 km) | 4 | 0.00% | 0.00% | 0.00% |
| ≤ 2,048 (8 km) | 12 | 0.00% | 0.00% | 0.00% |
| > 2,048 | 36 | 0.00% | 0.00% | 0.00% |

**drive**, 20,000 drawn Legs.

| O-D distance | Legs | Same Segment | Adjacent | Bypassed |
|---|---:|---:|---:|---:|
| ≤ 32 Tiles (one block) | 3 | 33.33% | 33.33% | 66.66% |
| ≤ 64 | 12 | 0.00% | 0.00% | 0.00% |
| ≤ 128 | 40 | 0.00% | 0.00% | 0.00% |
| ≤ 256 (1 km) | 142 | 0.00% | 0.00% | 0.00% |
| ≤ 512 (2 km) | 675 | 0.00% | 0.00% | 0.00% |
| ≤ 1,024 (4 km) | 2,191 | 0.00% | 0.00% | 0.00% |
| ≤ 2,048 (8 km) | 6,577 | 0.00% | 0.00% | 0.00% |
| > 2,048 | 10,360 | 0.00% | 0.00% | 0.00% |

**The bypass is a property of the query, not of the cluster size**, so it is measured at one rung and applies to every row of R3.1. What it costs to *decide* is two Segment-id comparisons and, at most, four endpoint comparisons.


---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 69.93 s — from 20:21:13 UTC to 20:22:23 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 2.43 / 1.90 / 1.33 (1 / 5 / 15 min)
- **Load average, at end** 3.03 / 2.20 / 1.48 (1 / 5 / 15 min)
- **CPU stall** 720,815 µs over the run — 1.03% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 38,137 µs over the run — 0.05% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
