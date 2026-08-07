## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-06 20:33:18 UTC
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
| 1 | 3.17 ms | 17,505 | 193 ns | 6 |
| 1, reduced | 2.08 ms | 17,505 | 127 ns | 4 |
| 1, reduced + paths | 3.56 ms | 18,592 | 217 ns | 7 |
| 2 | 7.46 ms | 68,766 | 1,821 ns | 15 |
| 2, reduced | 4.89 ms | 68,766 | 1,195 ns | 10 |
| 2, reduced + paths | 6.61 ms | 137,352 | 1,613 ns | 13 |
| 4 | 11.52 ms | 206,142 | 11,254 ns | 24 |
| 4, reduced | 15.79 ms | 206,142 | 15,428 ns | 33 |
| 4, reduced + paths | 20.52 ms | 412,218 | 20,047 ns | 42 |
| 8 | 23.38 ms | 574,207 | 91,345 ns | 48 |
| 8, reduced | 26.54 ms | 574,207 | 103,672 ns | 55 |
| 8, reduced + paths | 45.03 ms | 1,148,375 | 175,922 ns | 94 |
| 16 | 40.63 ms | 2,014,227 | 634,895 ns | 85 |
| 16, reduced | 59.87 ms | 2,014,227 | 935,582 ns | 125 |
| 16, reduced + paths | 96.04 ms | 4,028,411 | 1,500,696 ns | 201 |
| 32 | 76.47 ms | 9,861,622 | 4,779,711 ns | 160 |
| 32, reduced | 75.32 ms | 9,861,622 | 4,707,835 ns | 157 |
| 32, reduced + paths | 171.91 ms | 19,723,213 | 10,744,796 ns | 359 |
| 64 | 157.39 ms | 67,626,483 | 39,349,702 ns | 329 |
| 64, reduced | 153.39 ms | 67,626,483 | 38,349,897 ns | 321 |
| 64, reduced + paths | 339.67 ms | 135,252,966 | 84,917,647 ns | 711 |

One flat `Chebyshev` drive search in this process: **477,609 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **477,609 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 455,039 ns | 1.04× | 489,441 ns | 0.97× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 457,600 ns | 1.04× | 466,607 ns | 1.02× | 4,138 + 4 | 15,961 + 16 | 58 |
| 1, reduced + paths | 477,239 ns | 1.00× | 480,448 ns | 0.99× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 574,465 ns | 0.83× | 575,964 ns | 0.82× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 504,658 ns | 0.94× | 519,549 ns | 0.91× | 4,089 + 12 | 15,780 + 48 | 58 |
| 2, reduced + paths | 451,744 ns | 1.05× | 491,434 ns | 0.97× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 557,923 ns | 0.85× | 619,685 ns | 0.77× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 377,459 ns | 1.26× | 386,457 ns | 1.23× | 2,988 + 38 | 11,442 + 156 | 58 |
| 4, reduced + paths | 368,125 ns | 1.29× | 364,380 ns | 1.31× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 461,732 ns | 1.03× | 479,221 ns | 0.99× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 280,603 ns | 1.70× | 303,122 ns | 1.57× | 1,708 + 131 | 6,351 + 527 | 58 |
| 8, reduced + paths | 215,097 ns | 2.22× | 237,325 ns | 2.01× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 332,179 ns | 1.43× | 476,498 ns | 1.00× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 166,010 ns | 2.87× | 316,993 ns | 1.50× | 886 + 464 | 3,143 + 1,852 | 58 |
| 16, reduced + paths | 154,570 ns | 3.08× | 181,554 ns | 2.63× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 360,148 ns | 1.32× | 440,041 ns | 1.08× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 175,937 ns | 2.71× | 671,240 ns | 0.71× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 32, reduced + paths | 187,765 ns | 2.54× | 210,385 ns | 2.27× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 878,207 ns | 0.54× | 1,015,393 ns | 0.47× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 773,998 ns | 0.61× | 1,297,496 ns | 0.36× | 166 + 8,360 | 544 + 33,095 | 58 |
| 64, reduced + paths | 824,441 ns | 0.57× | 746,193 ns | 0.64× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **1,401,307 ns**, second **477,609 ns** — a spread of 193.40%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — *one Leg against 530–574 arrivals per Tick, ~400 ms of searching per 15.60 ms of Tick budget* — and that test applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to consume the whole budget on its own — or, put the way that depends on nothing derived, **routing fits only while fewer Trips start per Tick than the break-even column below.**

| Rung | Per route | **Break-even Trips/Tick** | At the working 550 | Fits |
|---|---:|---:|---:|---|
| **flat** | 477,609 ns | **32** | 262.68 ms | 16.83× over |
| 1, reduced + paths | 480,448 ns | **32** | 264.24 ms | 16.93× over |
| 2, reduced + paths | 491,434 ns | **31** | 270.28 ms | 17.32× over |
| 4, reduced + paths | 364,380 ns | **42** | 200.40 ms | 12.84× over |
| 8, reduced + paths | 237,325 ns | **65** | 130.52 ms | 8.36× over |
| 16, reduced + paths | 181,554 ns | **85** | 99.85 ms | 6.40× over |
| 32, reduced + paths | 210,385 ns | **74** | 115.71 ms | 7.41× over |
| 64, reduced + paths | 746,193 ns | **20** | 410.40 ms | 26.30× over |

**The break-even column is the finding; the two columns right of it are a marker on it.** *Break-even Trips/Tick* is a measured per-route cost divided by a world constant and contains nothing derived — it stays true when the arrival rate is finally measured. **550 is not measured and cannot be measured here**: it comes from ~56,000 Trips in flight, which rests on a mean Trip duration the corpus records as provisional, and S2 has no Travellers, no Trip generation and no Event Wheel to produce one. A tripwire whose denominator is a guess is a tripwire that can fire on the guess, so this one is stated in the form that does not depend on it.

**No cluster size fits, and the shape of the curve says none can.** The load is U-shaped in cluster size and both ends are pinned by the same thing: a small cluster makes the abstract search approach the flat search, a large one makes the *insertion* approach it. `adr/0040` admits only whole-Chunk clusters that tile the map, so the admissible rungs are the divisors of 128 and the minimum sits at one of them with its two neighbours worse. **This is a floor, not a rung that was missed.**

**Two exits, and neither is free.** A **cache** — `adr/0012` permits one keyed by origin-destination pair, and `plans/0010` R6 owns it — would have to reach roughly a **92% hit rate** to fit routing into half a Tick at the best rung. **That makes R6 load-bearing rather than an optimisation.** Or **threads**: invariant 4 is thread-count equivalence, so the best rung's load spread over eight cores fits — by spending the whole Tick budget of eight cores on routing, which is a mortgage rather than a solution.

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
| 1 | 206 | 552 | 2 | 6.73 ms | 55,975 ns | 8.53× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 6.03 ms | 56,495 ns | 8.45× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 12.21 ms | 104,860 ns | 4.55× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 19.83 ms | 187,363 ns | 2.54× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 42.87 ms | 339,039 ns | 1.40× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 52.04 ms | 148,691 ns | 3.21× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted. Only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Rung | Operation | Cost | Clusters touched | Share of cold build | Edits in one build |
|---|---|---:|---:|---:|---:|
| 1 | re-cost | 829 ns | 1.96 | 0.02% | 3,828 |
| 1, reduced | rebuild cluster | 468,223 ns | 1.96 | 22.45% | 4 |
| 1, reduced + paths | rebuild cluster | 474,617 ns | 1.96 | 13.29% | 7 |
| 2 | re-cost | 3,156 ns | 1.37 | 0.04% | 2,363 |
| 2, reduced | rebuild cluster | 404,838 ns | 1.37 | 8.26% | 12 |
| 2, reduced + paths | rebuild cluster | 296,107 ns | 1.37 | 4.47% | 22 |
| 4 | re-cost | 15,367 ns | 1.06 | 0.13% | 749 |
| 4, reduced | rebuild cluster | 200,766 ns | 1.06 | 1.27% | 78 |
| 4, reduced + paths | rebuild cluster | 243,022 ns | 1.06 | 1.18% | 84 |
| 8 | re-cost | 87,024 ns | 1.03 | 0.37% | 268 |
| 8, reduced | rebuild cluster | 313,478 ns | 1.03 | 1.18% | 84 |
| 8, reduced + paths | rebuild cluster | 375,723 ns | 1.03 | 0.83% | 119 |
| 16 | re-cost | 754,767 ns | 1.03 | 1.85% | 53 |
| 16, reduced | rebuild cluster | 753,829 ns | 1.03 | 1.25% | 79 |
| 16, reduced + paths | rebuild cluster | 1,296,680 ns | 1.03 | 1.35% | 74 |
| 32 | re-cost | 3,988,156 ns | 1.03 | 5.21% | 19 |
| 32, reduced | rebuild cluster | 4,778,975 ns | 1.03 | 6.34% | 15 |
| 32, reduced + paths | rebuild cluster | 10,417,111 ns | 1.03 | 6.05% | 16 |
| 64 | re-cost | 39,465,750 ns | 1.00 | 25.07% | 3 |
| 64, reduced | rebuild cluster | 41,910,835 ns | 1.00 | 27.32% | 3 |
| 64, reduced + paths | rebuild cluster | 77,482,496 ns | 1.00 | 22.81% | 4 |

**Two operations, and which one is sound is a property of the rung.** A complete abstract graph keeps every intra-edge, so *re-costing* the slots is exact. A reduced one removed edges whose redundancy is a property of the costs, so an edit can make a removed edge necessary again — no amount of re-costing brings it back, and the cluster's edge set must be **decided again**. That is the cost the recommended configuration actually pays on an edit, and R3 measures it rather than deriving it from the per-cluster build column, which is what an earlier draft did.

**The rebuild column below 8 Chunks is mostly this harness and should not be read as a property of the design.** A rebuilt cluster's edge list is spliced back into one global CSR — kept global so the query path measured above is the one a real implementation would run — and the splice copies every edge in the graph. At 16 Chunks that is 11,768 edges and a couple of percent; at one Chunk it is 64,134 edges plus a shift of 16,694 portal offsets, and it is most of the 469 µs. Per-cluster edge lists would remove it and would cost the query an indirection per portal expanded.

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

- **Run duration** 76.28 s — from 20:33:18 UTC to 20:34:34 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 2.55 / 2.60 / 2.13 (1 / 5 / 15 min)
- **Load average, at end** 2.84 / 2.70 / 2.20 (1 / 5 / 15 min)
- **CPU stall** 842,883 µs over the run — 1.10% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 26,576 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
