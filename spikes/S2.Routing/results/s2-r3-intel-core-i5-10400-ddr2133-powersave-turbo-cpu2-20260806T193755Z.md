## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-06 19:37:55 UTC
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
| 2 | 64 Tiles | 4,096 | 9 nodes | 16,482 | 4 | 80,146 | 1.12 MiB |
| 2, reduced | 64 Tiles | 4,096 | 9 nodes | 16,482 | 4 | 63,356 | 952.44 KiB |
| 4 | 128 Tiles | 1,024 | 25 nodes | 11,875 | 11 | 133,116 | 1.68 MiB |
| 4, reduced | 128 Tiles | 1,024 | 25 nodes | 11,875 | 11 | 45,236 | 692.11 KiB |
| 8 | 256 Tiles | 256 | 81 nodes | 6,665 | 26 | 151,350 | 1.84 MiB |
| 8, reduced | 256 Tiles | 256 | 81 nodes | 6,665 | 26 | 24,586 | 406.41 KiB |
| 16 | 512 Tiles | 64 | 291 nodes | 3,337 | 52 | 133,816 | 1.62 MiB |
| 16, reduced | 512 Tiles | 64 | 291 nodes | 3,337 | 52 | 11,768 | 229.45 KiB |
| 32 | 1,024 Tiles | 16 | 1,097 nodes | 1,481 | 92 | 101,464 | 1.23 MiB |
| 32, reduced | 1,024 Tiles | 16 | 1,097 nodes | 1,481 | 92 | 4,832 | 133.48 KiB |
| 64 | 2,048 Tiles | 4 | 4,248 nodes | 518 | 129 | 62,126 | 797.33 KiB |
| 64, reduced | 2,048 Tiles | 4 | 4,248 nodes | 518 | 129 | 1,678 | 88.95 KiB |

*Largest* is the node count of the fullest cluster — the bound on one insertion search, and the reason the query column below turns back up. The graph has 16,697 nodes, so the portal column is also the share of the network the abstract graph re-describes.

**The bottom rung is `adr/0014`'s claim taken literally** — *"the Chunk grid is already the pathfinding cluster"* — at Phase 1's provisional Chunk = Cell. `05 §5` predicted the pathfinding role wants *larger, and loudly*, at 32×32.

### R3.2 — preprocessing, in flat searches

Priced in flat searches rather than in milliseconds alone, because that is the question: preprocessing is only affordable if the queries it saves outnumber it. `adr/0040` makes the abstract graph `(derived AND rebuilt)`, so this cost is paid on every load and after every change to the cluster size — never amortised into a save.

| Chunks | Cold build | Nodes settled | Per cluster | In flat searches |
|---:|---:|---:|---:|---:|
| 1 | 2.51 ms | 17,505 | 153 ns | 5 |
| 1, reduced | 3.07 ms | 17,505 | 187 ns | 7 |
| 2 | 5.40 ms | 68,766 | 1,320 ns | 12 |
| 2, reduced | 3.19 ms | 68,766 | 778 ns | 7 |
| 4 | 9.56 ms | 206,142 | 9,341 ns | 22 |
| 4, reduced | 9.42 ms | 206,142 | 9,205 ns | 22 |
| 8 | 21.53 ms | 574,207 | 84,129 ns | 50 |
| 8, reduced | 24.26 ms | 574,207 | 94,791 ns | 56 |
| 16 | 37.28 ms | 2,014,227 | 582,563 ns | 87 |
| 16, reduced | 41.57 ms | 2,014,227 | 649,578 ns | 97 |
| 32 | 71.79 ms | 9,861,622 | 4,487,362 ns | 168 |
| 32, reduced | 72.87 ms | 9,861,622 | 4,554,506 ns | 170 |
| 64 | 152.73 ms | 67,626,483 | 38,184,309 ns | 358 |
| 64, reduced | 152.04 ms | 67,626,483 | 38,010,402 ns | 356 |

One flat `Chebyshev` drive search in this process: **426,451 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **426,451 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 417,123 ns | 1.02× | 481,988 ns | 0.88× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 497,315 ns | 0.85× | 433,480 ns | 0.98× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 475,267 ns | 0.89× | 478,374 ns | 0.89× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 425,529 ns | 1.00× | 433,644 ns | 0.98× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 475,796 ns | 0.89× | 484,890 ns | 0.87× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 322,214 ns | 1.32× | 343,705 ns | 1.24× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 385,791 ns | 1.10× | 405,402 ns | 1.05× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 205,438 ns | 2.07× | 261,867 ns | 1.62× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 295,991 ns | 1.44× | 342,251 ns | 1.24× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 139,264 ns | 3.06× | 277,655 ns | 1.53× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 310,203 ns | 1.37× | 406,277 ns | 1.04× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 166,633 ns | 2.55× | 511,130 ns | 0.83× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 773,069 ns | 0.55× | 913,937 ns | 0.46× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 685,690 ns | 0.62× | 1,158,888 ns | 0.36× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **1,277,635 ns**, second **426,451 ns** — a spread of 199.59%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the detour, because a different route is a different city

HPA\* returns the best route that **respects the partition**, which is never cheaper than the flat optimum and is sometimes dearer. R2 established the detour as this spike's correctness currency and measured 18.52% for a next-hop table against 36.01% for a shared District route; those are the figures this column stands beside. **The mean is over every query compared, optimal ones included at zero**, which is what makes it the same quantity R2 published rather than a mean over survivors.

| Chunks | Optimal | Mean detour | Worst detour | Cheaper than flat | Compared | Audited | Audit failures |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 1, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 2 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 2, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 4 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 4, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 8 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 8, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 16 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 16, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 32 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 32, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 64 | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |
| 64, reduced | 100.00% | 0.00% | 0.00% | 0 | 1,000 | 200 | 0 |

**Two columns here are printed to read zero.** *Cheaper than flat* would mean the hierarchy had found a route the unconstrained search could not, which is not a shortcut but a bug — and its natural hiding place is the detour mean. *Audit failures* re-walks a sample of refined routes and requires the entry partial, the arc costs and the exit remainder to sum **exactly** to the cost the query reported, with the arcs forming an unbroken chain. R2's harness published a peak `v/c` of 883× with every other column healthy, and the check that would have caught it had been specified in advance and not run.

### R3.5 — the sparser abstraction, at 16 Chunks

**Keeping every crossing is what makes the abstraction complete, and completeness is what R3.4's zero is made of.** Botea's HPA\* keeps one or two transitions per entrance and accepts a detour for it. This is that lever, swept at the cluster size R3.3 makes the best of — chosen from the measurement rather than in advance.

| Transitions | Portals | Abstract edges | Edges each | Cold build | Query | vs flat | Optimal | Mean detour | Worst |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 206 | 552 | 2 | 4.46 ms | 39,244 ns | 10.86× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 5.39 ms | 52,098 ns | 8.18× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 10.34 ms | 86,040 ns | 4.95× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 19.62 ms | 161,728 ns | 2.63× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 37.00 ms | 299,297 ns | 1.42× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 43.01 ms | 129,377 ns | 3.29× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.6 — invalidation, which is the half of the core verb R3 can price

One Segment deleted, the abstract graph **repaired rather than rebuilt**: only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Chunks | Repair | Clusters touched | Share of cold build | Repairs in one build | Edits |
|---:|---:|---:|---:|---:|---:|
| 1 | 1,481 ns | 1.96 | 0.05% | 1,695 | 32 |
| 2 | 1,230 ns | 1.37 | 0.02% | 4,398 | 32 |
| 4 | 9,843 ns | 1.06 | 0.10% | 971 | 32 |
| 8 | 84,186 ns | 1.03 | 0.39% | 255 | 32 |
| 16 | 569,254 ns | 1.03 | 1.52% | 65 | 32 |
| 32 | 3,990,275 ns | 1.03 | 5.55% | 17 | 32 |
| 64 | 37,025,364 ns | 1.00 | 24.24% | 4 | 32 |

**Deletion only, and the limit is structural rather than an omission.** The edge slots are fixed at build, so a repair may re-cost an edge and may cost it out of existence — but it cannot create a portal that did not exist, which is what *drawing* a road across a cluster boundary does. R5's edit storm is where the drawing half belongs.

### R3.7 — the bypass, and how local a walk Leg actually is

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

- **Run duration** 45.93 s — from 19:37:55 UTC to 19:38:41 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 4.75 / 1.88 / 1.25 (1 / 5 / 15 min)
- **Load average, at end** 3.65 / 1.99 / 1.31 (1 / 5 / 15 min)
- **CPU stall** 318,401 µs over the run — 0.69% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 9,190 µs over the run — 0.02% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
