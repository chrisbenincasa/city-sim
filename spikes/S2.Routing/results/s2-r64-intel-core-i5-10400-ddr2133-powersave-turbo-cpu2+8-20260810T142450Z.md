## S2 R6.4 — what a per-Citizen Habit Route costs

- **Captured** 2026-08-10 14:24:50 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** powersave
- **Processors allowed** 2,8 of 12
- **Build** Release

Working rung: 33,018 Segments, 16,697 nodes, 66,036 arcs, 32,069 admitting cars, 104 of them Arterial. The gesture is **4 of 4 requested** Arterial Segments — R5.4's rung, drawn at the same index, so it is the same road. Store is 2,048 searched routes per O-D rung, built against the **damaged** graph and then compared against a full recompute on the restored one.

### R6.4.1 — the branch-point compression ratio

A stored route is mostly **forced**. R3 measured this network at degree ~3, so most nodes on a route are degree 2 and mid-block, and the arc leaving them is the only one there is once the way back is discounted. A route can therefore be stored as the decisions taken at the nodes where a decision existed, and reconstructed by walking forward and taking the only onward arc everywhere else. **`k` is that count.** The branch test is R8.1's, reused rather than reinvented: *at least two onward car arcs once the arrival Segment is discounted*, evaluated at the node **before** each arc, because the last node of a route needs no decision.

| O-D rung | Routes | Mean arcs `L` | Mean `k` | p50 `k` | p90 `k` | max `k` | `L / k` | Bytes / Citizen | **At 1M** |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | 2,048 | 61.52 | **59.05** | 56 | 97 | 170 | 1.04× | 236 B | **225.06 MiB** |
| decay L=1024 | 2,045 | 47.01 | **45.16** | 42 | 78 | 159 | 1.04× | 181 B | **172.61 MiB** |
| decay L=512 | 2,044 | 35.07 | **33.61** | 30 | 62 | 147 | 1.04× | 134 B | **127.79 MiB** |
| decay L=256 | 2,031 | 23.24 | **22.08** | 17 | 47 | 111 | 1.05× | 88 B | **83.92 MiB** |
| monocentric L=512 | 2,047 | 56.59 | **54.58** | 52 | 86 | 147 | 1.03× | 218 B | **207.90 MiB** |

**The same measurement over the routes as they stand *after* the addition**, which is the second reading the board's *two measurements that agree to the last digit are not two measurements* asks for — and it is stated in advance that this is a **weak** second reading rather than an independent one. Four restored Arterial Segments move few routes, so agreement here is the expected result and disagreement would be the surprise. What it does check is that `k` is a property of the graph rather than of the damage: a `k` that moved materially when four Segments came back would mean the first table was measuring the hole and not the network.

| O-D rung | Routes | Mean arcs `L` | Mean `k` | p50 `k` | p90 `k` | max `k` | `L / k` | Bytes / Citizen | **At 1M** |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | 2,048 | 59.00 | **56.53** | 54 | 90 | 139 | 1.04× | 226 B | **215.53 MiB** |
| decay L=1024 | 2,045 | 45.46 | **43.60** | 41 | 75 | 145 | 1.04× | 174 B | **165.93 MiB** |
| decay L=512 | 2,044 | 34.35 | **32.88** | 30 | 61 | 130 | 1.04× | 132 B | **125.88 MiB** |
| decay L=256 | 2,031 | 22.91 | **21.76** | 17 | 47 | 93 | 1.05× | 87 B | **82.96 MiB** |
| monocentric L=512 | 2,047 | 55.40 | **53.39** | 52 | 83 | 139 | 1.03× | 214 B | **204.08 MiB** |

*Bytes per Citizen* is `k × 4`, one 32-bit arc id per decision, and **at 1M** is that times the late-game population — the axis the per-Citizen row of session M's trilemma was chosen against, and the one it is refuted on. The uncompressed comparator is the trilemma's own **232.7 MiB**, which is `L × 4 × 1M` at `L = 61`.

### R6.4.2 — the rejoin cost

One arc off a stored path, search back to it, bounded by the Sight Horizon. This is what a per-Citizen stored route has to do that a next-hop tree gets for free: the tree answers *where next* from wherever the Traveller actually is, and a stored path only answers it from on the path. **The diversion point is drawn among the route's branch points**, not uniformly along it, because a Traveller cannot diverge where there is nothing to diverge onto — R6.4.1 is what makes that distinction quantitative.

**The same sample serves every horizon**, deliberately: a sample redrawn per rung shrinks wherever the rung is harsher, and this spike has three times manufactured a trend out of survivorship that way. So the *attempted* column is flat down each rung by construction and only *rejoined* moves.

**What is timed is a cost-ordered bounded search**, not a breadth-first walk: the Traveller wants the cheapest way back, and the hop cap is on Segments because that is the unit R8.1 derived the Horizon's floor in. Suffix marking is timed **apart** and reported beside it, because *is this node on my route* is not free against a branch-point-compressed store and pretending otherwise would price the winning representation using the losing one's data structure.

| O-D rung | Horizon | Attempted | Rejoined | Suffix mark | Search alone | **Total** | p50 | p90 | max | × 1,269 diversions | of 15.6 ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| uniform | 1 | 512 | 75 (14.64%) | 236 ns | 441 ns | **677 ns** | 426 ns | 529 ns | 1.68 µs | 0.859 ms | 5.50% |
| uniform | 2 | 512 | 98 (19.14%) | 236 ns | 1.05 µs | **1.28 µs** | 1.10 µs | 1.29 µs | 7.44 µs | 1.634 ms | 10.47% |
| uniform | 3 | 512 | 439 (85.74%) | 236 ns | 2.08 µs | **2.32 µs** | 2.39 µs | 2.66 µs | 8.11 µs | 2.950 ms | 18.91% |
| uniform | 4 | 512 | 451 (88.08%) | 236 ns | 3.12 µs | **3.36 µs** | 3.47 µs | 4.56 µs | 8.07 µs | 4.268 ms | 27.36% |
| uniform | 8 | 512 | 474 (92.57%) | 236 ns | 4.47 µs | **4.70 µs** | 3.67 µs | 8.02 µs | 28.36 µs | 5.973 ms | 38.28% |
| uniform | 16 | 512 | 475 (92.77%) | 236 ns | 9.10 µs | **9.33 µs** | 3.76 µs | 8.20 µs | 152.85 µs | 11.848 ms | 75.95% |
| decay L=1024 | 1 | 512 | 82 (16.01%) | 76 ns | 470 ns | **546 ns** | 446 ns | 601 ns | 1.26 µs | 0.692 ms | 4.44% |
| decay L=1024 | 2 | 512 | 104 (20.31%) | 76 ns | 1.07 µs | **1.15 µs** | 1.13 µs | 1.39 µs | 1.95 µs | 1.463 ms | 9.37% |
| decay L=1024 | 3 | 512 | 430 (83.98%) | 76 ns | 2.08 µs | **2.15 µs** | 2.40 µs | 2.72 µs | 6.69 µs | 2.739 ms | 17.56% |
| decay L=1024 | 4 | 512 | 446 (87.10%) | 76 ns | 3.07 µs | **3.15 µs** | 3.42 µs | 4.64 µs | 7.14 µs | 3.999 ms | 25.64% |
| decay L=1024 | 8 | 512 | 462 (90.23%) | 76 ns | 4.86 µs | **4.94 µs** | 3.84 µs | 7.96 µs | 31.29 µs | 6.273 ms | 40.21% |
| decay L=1024 | 16 | 512 | 464 (90.62%) | 76 ns | 11.04 µs | **11.11 µs** | 3.88 µs | 8.20 µs | 188.47 µs | 14.108 ms | 90.44% |
| decay L=512 | 1 | 512 | 76 (14.84%) | 61 ns | 420 ns | **481 ns** | 405 ns | 498 ns | 953 ns | 0.610 ms | 3.91% |
| decay L=512 | 2 | 512 | 102 (19.92%) | 61 ns | 1.02 µs | **1.08 µs** | 1.10 µs | 1.21 µs | 6.05 µs | 1.381 ms | 8.85% |
| decay L=512 | 3 | 512 | 419 (81.83%) | 61 ns | 2.06 µs | **2.12 µs** | 2.35 µs | 2.62 µs | 16.62 µs | 2.700 ms | 17.31% |
| decay L=512 | 4 | 512 | 436 (85.15%) | 61 ns | 3.13 µs | **3.19 µs** | 3.54 µs | 4.72 µs | 9.22 µs | 4.054 ms | 25.99% |
| decay L=512 | 8 | 512 | 458 (89.45%) | 61 ns | 5.24 µs | **5.30 µs** | 3.70 µs | 14.02 µs | 34.25 µs | 6.735 ms | 43.17% |
| decay L=512 | 16 | 512 | 458 (89.45%) | 61 ns | 13.40 µs | **13.46 µs** | 3.82 µs | 36.38 µs | 192.17 µs | 17.082 ms | 109.50% |
| decay L=256 | 1 | 512 | 88 (17.18%) | 41 ns | 397 ns | **438 ns** | 388 ns | 456 ns | 894 ns | 0.555 ms | 3.56% |
| decay L=256 | 2 | 512 | 126 (24.60%) | 41 ns | 967 ns | **1.00 µs** | 1.07 µs | 1.17 µs | 1.68 µs | 1.279 ms | 8.19% |
| decay L=256 | 3 | 512 | 400 (78.12%) | 41 ns | 2.03 µs | **2.07 µs** | 2.36 µs | 2.71 µs | 7.41 µs | 2.638 ms | 16.91% |
| decay L=256 | 4 | 512 | 413 (80.66%) | 41 ns | 3.09 µs | **3.13 µs** | 3.50 µs | 5.19 µs | 6.05 µs | 3.982 ms | 25.52% |
| decay L=256 | 8 | 512 | 431 (84.17%) | 41 ns | 6.06 µs | **6.10 µs** | 3.74 µs | 21.61 µs | 29.92 µs | 7.743 ms | 49.63% |
| decay L=256 | 16 | 512 | 432 (84.37%) | 41 ns | 16.65 µs | **16.69 µs** | 4.03 µs | 73.15 µs | 208.18 µs | 21.184 ms | 135.79% |
| monocentric L=512 | 1 | 512 | 57 (11.13%) | 83 ns | 401 ns | **484 ns** | 388 ns | 462 ns | 1.04 µs | 0.614 ms | 3.93% |
| monocentric L=512 | 2 | 512 | 82 (16.01%) | 83 ns | 1.00 µs | **1.08 µs** | 1.08 µs | 1.18 µs | 1.96 µs | 1.375 ms | 8.81% |
| monocentric L=512 | 3 | 512 | 427 (83.39%) | 83 ns | 2.04 µs | **2.12 µs** | 2.32 µs | 2.58 µs | 7.28 µs | 2.700 ms | 17.31% |
| monocentric L=512 | 4 | 512 | 439 (85.74%) | 83 ns | 3.24 µs | **3.32 µs** | 3.48 µs | 4.79 µs | 10.16 µs | 4.218 ms | 27.03% |
| monocentric L=512 | 8 | 512 | 469 (91.60%) | 83 ns | 5.17 µs | **5.25 µs** | 3.93 µs | 10.35 µs | 30.34 µs | 6.671 ms | 42.76% |
| monocentric L=512 | 16 | 512 | 470 (91.79%) | 83 ns | 9.83 µs | **9.91 µs** | 3.77 µs | 10.83 µs | 147.03 µs | 12.582 ms | 80.65% |

**Sample size per rung, which is the survivorship guard:**

- `uniform`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `decay L=1024`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `decay L=512`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `decay L=256`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `monocentric L=512`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.

*× 1,269 diversions* multiplies by R8.3's measured **1,269 diversions per Tick** at N = 1, uniform, 40,000 Travellers — which is a figure from a different fleet size than the 1M store above, and is printed because R6.4.2's own threshold is stated as a product against it. The flat denominator this section could have used instead reads **1.08 ms** a search, so a rejoin that costs less than a thousandth of one is the interesting case and a rejoin that costs a tenth of one is the row losing outright.

### R6.4.3 — the addition wake's fan-out, and what `d` should be

`adr/0012`'s contract wakes routes within `d` of a newly added Segment. R1.7 measured that a proximity test over a **matrix** missed 309 of 429 changed entries and missed them silently; this runs the same method over a **route store**. `C` is ground truth — the routes a full recompute on the restored graph returns cheaper than the stored one — and `W(d)` is what the reverse index wakes.

**A caveat stated in advance and running against us**: R1.7's entries are *pairs* and these are *paths*. A path is a longer object with more chances to pass near an edit, so a route store's fan-out should be **worse** than R1.7's at the same `d`, not better. Nothing below is read as the earlier number improving.

**The wake sets a bit; it does not recompute.** So `|W(d)|` is priced in marks, and the refutation is `P(stale)` approaching 1 at every `d` that catches a useful share of `C` — one road edit marking most of the city, every subsequent Trip start recomputing, and the drain bounding nothing.

**O-D rung `uniform`** — 2,048 comparable routes of 2,048 drawn (2,048 found on the damaged graph, 2,048 on the restored one). **`|C|` = 202**, mean improvement 13.69%, best 42.76%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 202, **193 improve by more than 1%** (95.54% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **346** | **36** | 180 | **16.89%** | 82.17% | **82.38%** (159 of 193) | 29.92 µs | 4,014 |
| 1 | 128 | **362** | **31** | 191 | **17.67%** | 84.65% | **84.97%** (164 of 193) | 38.22 µs | 6,026 |
| 2 | 256 | **368** | **31** | 197 | **17.96%** | 84.65% | **84.97%** (164 of 193) | 48.73 µs | 8,241 |
| 4 | 512 | **424** | **26** | 248 | **20.70%** | 87.12% | **87.56%** (169 of 193) | 97.62 µs | 12,936 |
| 8 | 1,024 | **488** | **25** | 311 | **23.82%** | 87.62% | **88.08%** (170 of 193) | 153.65 µs | 28,767 |
| 16 | 2,048 | **650** | **20** | 468 | **31.73%** | 90.09% | **90.67%** (175 of 193) | 360.84 µs | 68,204 |

Reverse index: **225,505 memberships** over 16,384 Cells — **110 a route** — **3.98 MiB** singly linked against the store's 500.21 KiB (8.15×), 4.94 MiB with the `previous` array. Insert **6.45 µs** a route. Evict **51.93 µs** over 6,951 chain steps singly linked, against **2.06 µs** over 110 doubly linked (25.16×). Live entries after the evictions: **0** against a high water of 225,505 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 110 memberships × four ints is **1760 B a route**, so at 1M Citizens the index alone is **1678.46 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `decay L=1024`** — 2,045 comparable routes of 2,048 drawn (2,045 found on the damaged graph, 2,046 on the restored one). **`|C|` = 143**, mean improvement 14.53%, best 51.51%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 143, **140 improve by more than 1%** (97.90% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **256** | **22** | 135 | **12.51%** | 84.61% | **85.00%** (119 of 140) | 20.69 µs | 3,044 |
| 1 | 128 | **270** | **19** | 146 | **13.20%** | 86.71% | **87.14%** (122 of 140) | 26.12 µs | 4,489 |
| 2 | 256 | **277** | **19** | 153 | **13.54%** | 86.71% | **87.14%** (122 of 140) | 32.84 µs | 6,151 |
| 4 | 512 | **333** | **14** | 204 | **16.28%** | 90.20% | **90.71%** (127 of 140) | 50.00 µs | 9,948 |
| 8 | 1,024 | **403** | **12** | 272 | **19.70%** | 91.60% | **91.42%** (128 of 140) | 103.43 µs | 22,576 |
| 16 | 2,048 | **537** | **11** | 405 | **26.25%** | 92.30% | **92.14%** (129 of 140) | 261.74 µs | 54,809 |

Reverse index: **148,436 memberships** over 16,384 Cells — **72 a route** — **3.07 MiB** singly linked against the store's 383.57 KiB (8.20×), 3.80 MiB with the `previous` array. Insert **3.08 µs** a route. Evict **15.74 µs** over 2,021 chain steps singly linked, against **1.11 µs** over 72 doubly linked (14.11×). Live entries after the evictions: **0** against a high water of 148,436 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 72 memberships × four ints is **1152 B a route**, so at 1M Citizens the index alone is **1098.63 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `decay L=512`** — 2,044 comparable routes of 2,048 drawn (2,044 found on the damaged graph, 2,044 on the restored one). **`|C|` = 88**, mean improvement 16.39%, best 51.51%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 88, **86 improve by more than 1%** (97.72% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **178** | **6** | 96 | **8.70%** | 93.18% | **93.02%** (80 of 86) | 60.51 µs | 2,231 |
| 1 | 128 | **189** | **6** | 107 | **9.24%** | 93.18% | **93.02%** (80 of 86) | 32.98 µs | 3,238 |
| 2 | 256 | **202** | **5** | 119 | **9.88%** | 94.31% | **94.18%** (81 of 86) | 39.37 µs | 4,407 |
| 4 | 512 | **245** | **4** | 161 | **11.98%** | 95.45% | **95.34%** (82 of 86) | 59.31 µs | 7,226 |
| 8 | 1,024 | **307** | **3** | 222 | **15.01%** | 96.59% | **96.51%** (83 of 86) | 121.30 µs | 16,526 |
| 16 | 2,048 | **439** | **3** | 354 | **21.47%** | 96.59% | **96.51%** (83 of 86) | 224.50 µs | 40,310 |

Reverse index: **100,829 memberships** over 16,384 Cells — **49 a route** — **2.32 MiB** singly linked against the store's 288.05 KiB (8.27×), 2.87 MiB with the `previous` array. Insert **3.41 µs** a route. Evict **6.46 µs** over 760 chain steps singly linked, against **774 ns** over 49 doubly linked (8.34×). Live entries after the evictions: **0** against a high water of 100,829 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 49 memberships × four ints is **784 B a route**, so at 1M Citizens the index alone is **747.68 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `decay L=256`** — 2,031 comparable routes of 2,048 drawn (2,031 found on the damaged graph, 2,031 on the restored one). **`|C|` = 46**, mean improvement 18.63%, best 59.61%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 46, **43 improve by more than 1%** (93.47% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **113** | **6** | 73 | **5.56%** | 86.95% | **88.37%** (38 of 43) | 12.30 µs | 1,336 |
| 1 | 128 | **125** | **6** | 85 | **6.15%** | 86.95% | **88.37%** (38 of 43) | 13.25 µs | 1,988 |
| 2 | 256 | **142** | **3** | 99 | **6.99%** | 93.47% | **95.34%** (41 of 43) | 15.87 µs | 2,799 |
| 4 | 512 | **195** | **1** | 150 | **9.60%** | 97.82% | **97.67%** (42 of 43) | 23.73 µs | 4,727 |
| 8 | 1,024 | **260** | **0** | 214 | **12.80%** | 100.00% | **100.00%** (43 of 43) | 52.60 µs | 10,992 |
| 16 | 2,048 | **389** | **0** | 343 | **19.15%** | 100.00% | **100.00%** (43 of 43) | 131.03 µs | 27,849 |

Reverse index: **61,927 memberships** over 16,384 Cells — **30 a route** — **1.58 MiB** singly linked against the store's 192.41 KiB (8.41×), 1.94 MiB with the `previous` array. Insert **1.52 µs** a route. Evict **2.54 µs** over 250 chain steps singly linked, against **470 ns** over 30 doubly linked (5.39×). Live entries after the evictions: **0** against a high water of 61,927 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 30 memberships × four ints is **480 B a route**, so at 1M Citizens the index alone is **457.76 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `monocentric L=512`** — 2,047 comparable routes of 2,048 drawn (2,047 found on the damaged graph, 2,048 on the restored one). **`|C|` = 122**, mean improvement 10.42%, best 51.51%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 122, **117 improve by more than 1%** (95.90% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **219** | **24** | 121 | **10.69%** | 80.32% | **80.34%** (94 of 117) | 20.55 µs | 2,540 |
| 1 | 128 | **232** | **18** | 128 | **11.33%** | 85.24% | **85.47%** (100 of 117) | 24.57 µs | 3,800 |
| 2 | 256 | **237** | **17** | 132 | **11.57%** | 86.06% | **85.47%** (100 of 117) | 30.21 µs | 5,094 |
| 4 | 512 | **280** | **13** | 171 | **13.67%** | 89.34% | **88.88%** (104 of 117) | 66.12 µs | 7,788 |
| 8 | 1,024 | **329** | **11** | 218 | **16.07%** | 90.98% | **90.59%** (106 of 117) | 91.64 µs | 17,282 |
| 16 | 2,048 | **492** | **11** | 381 | **24.03%** | 90.98% | **90.59%** (106 of 117) | 217.98 µs | 41,678 |

Reverse index: **195,039 memberships** over 16,384 Cells — **95 a route** — **3.67 MiB** singly linked against the store's 460.50 KiB (8.17×), 4.55 MiB with the `previous` array. Insert **3.54 µs** a route. Evict **47.14 µs** over 5,495 chain steps singly linked, against **581 ns** over 95 doubly linked (81.06×). Live entries after the evictions: **0** against a high water of 195,039 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 95 memberships × four ints is **1520 B a route**, so at 1M Citizens the index alone is **1449.58 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

### The denominator, measured twice

R3's finding, addressed to R6 by name on the board: *a denominator measured once has no error bar, and a denominator measured first has a systematic one.* The same 256 uniform flat searches, before anything else in this section and again after all of it.

| Reading | Mean per search |
|---|---:|
| first, cold | 1.08 ms |
| last, warm | 421.08 µs |
| spread | **2.58×** |



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 6.79 s — from 14:24:50 UTC to 14:24:56 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.49 / 1.61 / 1.86 (1 / 5 / 15 min)
- **Load average, at end** 1.45 / 1.60 / 1.86 (1 / 5 / 15 min)
- **CPU stall** 28,202 µs over the run — 0.41% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 1,410 µs over the run — 0.02% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
