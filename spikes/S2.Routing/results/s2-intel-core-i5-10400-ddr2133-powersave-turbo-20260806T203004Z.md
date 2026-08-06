## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-06 20:28:48 UTC
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
| 1 | 4.97 ms | 17,505 | 303 ns | 10 |
| 1, reduced | 2.48 ms | 17,505 | 151 ns | 5 |
| 1, reduced + paths | 3.10 ms | 18,592 | 189 ns | 6 |
| 2 | 4.98 ms | 68,766 | 1,215 ns | 10 |
| 2, reduced | 8.30 ms | 68,766 | 2,028 ns | 16 |
| 2, reduced + paths | 5.87 ms | 137,352 | 1,435 ns | 11 |
| 4 | 11.14 ms | 206,142 | 10,881 ns | 22 |
| 4, reduced | 10.12 ms | 206,142 | 9,888 ns | 20 |
| 4, reduced + paths | 19.61 ms | 412,218 | 19,152 ns | 39 |
| 8 | 28.34 ms | 574,207 | 110,728 ns | 57 |
| 8, reduced | 30.33 ms | 574,207 | 118,497 ns | 61 |
| 8, reduced + paths | 53.09 ms | 1,148,375 | 207,402 ns | 107 |
| 16 | 37.78 ms | 2,014,227 | 590,467 ns | 76 |
| 16, reduced | 43.25 ms | 2,014,227 | 675,853 ns | 87 |
| 16, reduced + paths | 84.91 ms | 4,028,411 | 1,326,745 ns | 172 |
| 32 | 65.73 ms | 9,861,622 | 4,108,581 ns | 133 |
| 32, reduced | 94.92 ms | 9,861,622 | 5,932,522 ns | 193 |
| 32, reduced + paths | 147.58 ms | 19,723,213 | 9,223,767 ns | 300 |
| 64 | 155.87 ms | 67,626,483 | 38,967,611 ns | 317 |
| 64, reduced | 158.68 ms | 67,626,483 | 39,671,376 ns | 322 |
| 64, reduced + paths | 303.13 ms | 135,252,966 | 75,784,780 ns | 616 |

One flat `Chebyshev` drive search in this process: **491,655 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **491,655 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 511,441 ns | 0.96× | 475,730 ns | 1.03× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 461,208 ns | 1.06× | 472,064 ns | 1.04× | 4,138 + 4 | 15,961 + 16 | 58 |
| 1, reduced + paths | 613,225 ns | 0.80× | 480,214 ns | 1.02× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 507,223 ns | 0.96× | 597,649 ns | 0.82× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 524,566 ns | 0.93× | 492,339 ns | 0.99× | 4,089 + 12 | 15,780 + 48 | 58 |
| 2, reduced + paths | 482,222 ns | 1.01× | 562,983 ns | 0.87× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 485,043 ns | 1.01× | 548,620 ns | 0.89× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 377,253 ns | 1.30× | 404,417 ns | 1.21× | 2,988 + 38 | 11,442 + 156 | 58 |
| 4, reduced + paths | 418,422 ns | 1.17× | 405,078 ns | 1.21× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 440,141 ns | 1.11× | 470,475 ns | 1.04× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 224,367 ns | 2.19× | 291,037 ns | 1.68× | 1,708 + 131 | 6,351 + 527 | 58 |
| 8, reduced + paths | 213,357 ns | 2.30× | 253,383 ns | 1.94× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 320,424 ns | 1.53× | 412,146 ns | 1.19× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 145,227 ns | 3.38× | 326,937 ns | 1.50× | 886 + 464 | 3,143 + 1,852 | 58 |
| 16, reduced + paths | 136,669 ns | 3.59× | 141,587 ns | 3.47× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 334,014 ns | 1.47× | 482,057 ns | 1.01× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 190,353 ns | 2.58× | 581,534 ns | 0.84× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 32, reduced + paths | 194,204 ns | 2.53× | 189,154 ns | 2.59× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 903,405 ns | 0.54× | 1,048,970 ns | 0.46× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 802,341 ns | 0.61× | 1,374,073 ns | 0.35× | 166 + 8,360 | 544 + 33,095 | 58 |
| 64, reduced + paths | 755,667 ns | 0.65× | 769,090 ns | 0.63× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **1,385,008 ns**, second **491,655 ns** — a spread of 181.70%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — *one Leg against 530–574 arrivals per Tick, ~400 ms of searching per 15.60 ms of Tick budget* — and that test applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to consume the whole budget on its own — or, put the way that depends on nothing derived, **routing fits only while fewer Trips start per Tick than the break-even column below.**

| Rung | Per route | **Break-even Trips/Tick** | At the working 550 | Fits |
|---|---:|---:|---:|---|
| **flat** | 491,655 ns | **31** | 270.41 ms | 17.33× over |
| 1, reduced + paths | 480,214 ns | **32** | 264.11 ms | 16.93× over |
| 2, reduced + paths | 562,983 ns | **27** | 309.64 ms | 19.84× over |
| 4, reduced + paths | 405,078 ns | **38** | 222.79 ms | 14.28× over |
| 8, reduced + paths | 253,383 ns | **61** | 139.36 ms | 8.93× over |
| 16, reduced + paths | 141,587 ns | **110** | 77.87 ms | 4.99× over |
| 32, reduced + paths | 189,154 ns | **82** | 104.03 ms | 6.66× over |
| 64, reduced + paths | 769,090 ns | **20** | 422.99 ms | 27.11× over |

**The break-even column is the finding; the two columns right of it are a marker on it.** *Break-even Trips/Tick* is a measured per-route cost divided by a world constant and contains nothing derived — it stays true when the arrival rate is finally measured. **550 is not measured and cannot be measured here**: it comes from ~56,000 Trips in flight, which rests on a mean Trip duration the corpus records as provisional, and S2 has no Travellers, no Trip generation and no Event Wheel to produce one. A tripwire whose denominator is a guess is a tripwire that can fire on the guess, so this one is stated in the form that does not depend on it.

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
| 1 | 206 | 552 | 2 | 5.00 ms | 44,630 ns | 11.01× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 6.04 ms | 56,496 ns | 8.70× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 12.56 ms | 101,913 ns | 4.82× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 28.84 ms | 192,024 ns | 2.56× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 41.50 ms | 344,665 ns | 1.42× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 43.53 ms | 140,048 ns | 3.51× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted, the abstract graph **repaired rather than rebuilt**: only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Rung | Operation | Cost | Clusters touched | Share of cold build | Edits in one build |
|---|---|---:|---:|---:|---:|
| 1 | re-cost | 771 ns | 1.96 | 0.01% | 6,457 |
| 1, reduced | rebuild cluster | 439,461 ns | 1.96 | 17.68% | 5 |
| 1, reduced + paths | rebuild cluster | 469,128 ns | 1.96 | 15.10% | 6 |
| 2 | re-cost | 4,117 ns | 1.37 | 0.08% | 1,209 |
| 2, reduced | rebuild cluster | 213,506 ns | 1.37 | 2.56% | 38 |
| 2, reduced + paths | rebuild cluster | 251,826 ns | 1.37 | 4.28% | 23 |
| 4 | re-cost | 10,358 ns | 1.06 | 0.09% | 1,075 |
| 4, reduced | rebuild cluster | 121,520 ns | 1.06 | 1.20% | 83 |
| 4, reduced + paths | rebuild cluster | 231,569 ns | 1.06 | 1.18% | 84 |
| 8 | re-cost | 111,942 ns | 1.03 | 0.39% | 253 |
| 8, reduced | rebuild cluster | 246,901 ns | 1.03 | 0.81% | 122 |
| 8, reduced + paths | rebuild cluster | 260,595 ns | 1.03 | 0.49% | 203 |
| 16 | re-cost | 630,377 ns | 1.03 | 1.66% | 59 |
| 16, reduced | rebuild cluster | 660,038 ns | 1.03 | 1.52% | 65 |
| 16, reduced + paths | rebuild cluster | 1,285,465 ns | 1.03 | 1.51% | 66 |
| 32 | re-cost | 4,288,794 ns | 1.03 | 6.52% | 15 |
| 32, reduced | rebuild cluster | 4,652,593 ns | 1.03 | 4.90% | 20 |
| 32, reduced + paths | rebuild cluster | 9,227,389 ns | 1.03 | 6.25% | 15 |
| 64 | re-cost | 40,185,017 ns | 1.00 | 25.78% | 3 |
| 64, reduced | rebuild cluster | 40,259,282 ns | 1.00 | 25.37% | 3 |
| 64, reduced + paths | rebuild cluster | 79,469,812 ns | 1.00 | 26.21% | 3 |

**Two operations, and which one is sound is a property of the rung.** A complete abstract graph keeps every intra-edge, so *re-costing* the slots is exact. A reduced one removed edges whose redundancy is a property of the costs, so an edit can make a removed edge necessary again — no amount of re-costing brings it back, and the cluster's edge set must be **decided again**. That is the cost the recommended configuration actually pays on an edit, and R3 measures it rather than deriving it from the per-cluster build column, which is what an earlier draft did.

**Deletion only, and the limit is structural rather than an omission.** Both operations work over the portals the build found, so either may cost an edge out of existence but neither can create a portal that did not exist — which is what *drawing* a road across a cluster boundary does. R5's edit storm is where the drawing half belongs.

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

- **Run duration** 75.81 s — from 20:28:48 UTC to 20:30:04 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 2.80 / 2.68 / 1.98 (1 / 5 / 15 min)
- **Load average, at end** 3.05 / 2.80 / 2.08 (1 / 5 / 15 min)
- **CPU stall** 807,222 µs over the run — 1.06% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 28,369 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
