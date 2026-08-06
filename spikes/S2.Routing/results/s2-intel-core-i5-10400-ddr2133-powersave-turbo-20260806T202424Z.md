## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-06 20:23:15 UTC
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
| 1 | 3.81 ms | 17,505 | 232 ns | 7 |
| 1, reduced | 2.23 ms | 17,505 | 136 ns | 4 |
| 1, reduced + paths | 4.97 ms | 18,568 | 303 ns | 9 |
| 2 | 4.85 ms | 68,766 | 1,186 ns | 9 |
| 2, reduced | 4.08 ms | 68,766 | 996 ns | 7 |
| 2, reduced + paths | 7.05 ms | 134,292 | 1,721 ns | 13 |
| 4 | 12.31 ms | 206,142 | 12,030 ns | 23 |
| 4, reduced | 10.70 ms | 206,142 | 10,449 ns | 20 |
| 4, reduced + paths | 16.58 ms | 387,122 | 16,195 ns | 31 |
| 8 | 23.09 ms | 574,207 | 90,216 ns | 44 |
| 8, reduced | 34.95 ms | 574,207 | 136,551 ns | 67 |
| 8, reduced + paths | 43.36 ms | 949,587 | 169,377 ns | 83 |
| 16 | 41.25 ms | 2,014,227 | 644,649 ns | 79 |
| 16, reduced | 44.90 ms | 2,014,227 | 701,613 ns | 86 |
| 16, reduced + paths | 73.02 ms | 2,682,823 | 1,140,992 ns | 140 |
| 32 | 92.11 ms | 9,861,622 | 5,756,958 ns | 177 |
| 32, reduced | 76.71 ms | 9,861,622 | 4,794,723 ns | 147 |
| 32, reduced + paths | 134.17 ms | 10,989,761 | 8,386,012 ns | 258 |
| 64 | 204.63 ms | 67,626,483 | 51,159,963 ns | 393 |
| 64, reduced | 160.84 ms | 67,626,483 | 40,212,116 ns | 309 |
| 64, reduced + paths | 298.97 ms | 69,693,374 | 74,743,493 ns | 575 |

One flat `Chebyshev` drive search in this process: **519,778 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **519,778 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 473,154 ns | 1.09× | 490,633 ns | 1.05× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 474,534 ns | 1.09× | 503,511 ns | 1.03× | 4,138 + 4 | 15,961 + 16 | 58 |
| 1, reduced + paths | 561,256 ns | 0.92× | 474,465 ns | 1.09× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 496,985 ns | 1.04× | 606,779 ns | 0.85× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 483,800 ns | 1.07× | 504,498 ns | 1.03× | 4,089 + 12 | 15,780 + 48 | 58 |
| 2, reduced + paths | 476,322 ns | 1.09× | 526,684 ns | 0.98× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 536,959 ns | 0.96× | 593,165 ns | 0.87× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 353,376 ns | 1.47× | 444,417 ns | 1.16× | 2,988 + 38 | 11,442 + 156 | 58 |
| 4, reduced + paths | 401,258 ns | 1.29× | 354,458 ns | 1.46× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 443,120 ns | 1.17× | 474,820 ns | 1.09× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 289,272 ns | 1.79× | 283,853 ns | 1.83× | 1,708 + 131 | 6,351 + 527 | 58 |
| 8, reduced + paths | 213,639 ns | 2.43× | 222,639 ns | 2.33× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 356,703 ns | 1.45× | 359,490 ns | 1.44× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 143,994 ns | 3.60× | 297,876 ns | 1.74× | 886 + 464 | 3,143 + 1,852 | 58 |
| 16, reduced + paths | 157,322 ns | 3.30× | 160,207 ns | 3.24× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 400,222 ns | 1.29× | 433,061 ns | 1.20× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 175,943 ns | 2.95× | 519,961 ns | 0.99× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 32, reduced + paths | 179,876 ns | 2.88× | 199,265 ns | 2.60× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 937,039 ns | 0.55× | 973,230 ns | 0.53× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 827,432 ns | 0.62× | 1,244,575 ns | 0.41× | 166 + 8,360 | 544 + 33,095 | 58 |
| 64, reduced + paths | 727,895 ns | 0.71× | 751,713 ns | 0.69× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **1,630,022 ns**, second **519,778 ns** — a spread of 213.59%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — *one Leg against 530–574 arrivals per Tick, ~400 ms of searching per 15.60 ms of Tick budget* — and that test applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to consume the whole budget on its own — or, put the way that depends on nothing derived, **routing fits only while fewer Trips start per Tick than the break-even column below.**

| Rung | Per route | **Break-even Trips/Tick** | At the working 550 | Fits |
|---|---:|---:|---:|---|
| **flat** | 519,778 ns | **30** | 285.87 ms | 18.32× over |
| 1, reduced + paths | 474,465 ns | **32** | 260.95 ms | 16.72× over |
| 2, reduced + paths | 526,684 ns | **29** | 289.67 ms | 18.56× over |
| 4, reduced + paths | 354,458 ns | **44** | 194.95 ms | 12.49× over |
| 8, reduced + paths | 222,639 ns | **70** | 122.45 ms | 7.84× over |
| 16, reduced + paths | 160,207 ns | **97** | 88.11 ms | 5.64× over |
| 32, reduced + paths | 199,265 ns | **78** | 109.59 ms | 7.02× over |
| 64, reduced + paths | 751,713 ns | **20** | 413.44 ms | 26.50× over |

**The break-even column is the finding; the two columns right of it are a marker on it.** *Break-even Trips/Tick* is a measured per-route cost divided by a world constant and contains nothing derived — it stays true when the arrival rate is finally measured. **550 is not measured and cannot be measured here**: it comes from ~56,000 Trips in flight, which rests on a mean Trip duration the corpus records as provisional, and S2 has no Travellers, no Trip generation and no Event Wheel to produce one. A tripwire whose denominator is a guess is a tripwire that can fire on the guess, so this one is stated in the form that does not depend on it.

**No cluster size fits, and the shape of the curve says none can.** The load is U-shaped in cluster size and both ends are pinned by the same thing: a small cluster makes the abstract search approach the flat search, a large one makes the *insertion* approach it. `adr/0040` admits only whole-Chunk clusters that tile the map, so the admissible rungs are the divisors of 128 and the minimum sits at one of them with its two neighbours worse. **This is a floor, not a rung that was missed.**

**Two exits, and neither is free.** A **cache** — `adr/0012` permits one keyed by origin-destination pair, and `plans/0010` R6 owns it — would have to reach roughly a **91% hit rate** to fit routing into half a Tick at the best rung. **That makes R6 load-bearing rather than an optimisation.** Or **threads**: invariant 4 is thread-count equivalence, so the best rung's load spread over eight cores fits — by spending the whole Tick budget of eight cores on routing, which is a mortgage rather than a solution.

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
| 1 | 206 | 552 | 2 | 5.38 ms | 48,240 ns | 10.77× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 6.60 ms | 60,212 ns | 8.63× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 11.33 ms | 99,052 ns | 5.24× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 21.22 ms | 184,209 ns | 2.82× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 39.83 ms | 342,639 ns | 1.51× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 58.23 ms | 167,029 ns | 3.11× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted, the abstract graph **repaired rather than rebuilt**: only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Chunks | Repair | Clusters touched | Share of cold build | Repairs in one build | Edits |
|---:|---:|---:|---:|---:|---:|
| 1 | 1,211 ns | 1.96 | 0.03% | 3,149 | 32 |
| 2 | 1,450 ns | 1.37 | 0.02% | 3,350 | 32 |
| 4 | 11,776 ns | 1.06 | 0.09% | 1,046 | 32 |
| 8 | 112,393 ns | 1.03 | 0.48% | 205 | 32 |
| 16 | 512,826 ns | 1.03 | 1.24% | 80 | 32 |
| 32 | 4,073,444 ns | 1.03 | 4.42% | 22 | 32 |
| 64 | 38,889,986 ns | 1.00 | 19.00% | 5 | 32 |

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

- **Run duration** 68.89 s — from 20:23:15 UTC to 20:24:24 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 2.67 / 2.23 / 1.53 (1 / 5 / 15 min)
- **Load average, at end** 3.83 / 2.67 / 1.74 (1 / 5 / 15 min)
- **CPU stall** 778,243 µs over the run — 1.12% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 26,121 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
