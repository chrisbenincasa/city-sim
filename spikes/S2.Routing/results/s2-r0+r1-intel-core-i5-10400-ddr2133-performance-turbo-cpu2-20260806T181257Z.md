## S2 R0 — the synthetic Road Graph

- **Captured** 2026-08-06 18:12:57 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 1 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Build** Release

The graph is directed, one arc per permitted direction, mode masks on the arc.
Cost is time in Q16.16 Ticks; see `Graph/Units.cs` for why it is not whole Ticks.

### Road density against block size

The `~30,000 Segments` placeholder is a road-density assumption nobody has argued.
This is what each density implies on a 4096² map.

| Block | Segments | Nodes | Arcs | Segments/km² | km road/km² | Mean Segment | Foot-admitting |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 128 Tiles | 2,072 | 1,145 | 4,144 | 7 | 4.19 | 135 Tiles | 94% |
| 96 Tiles | 3,539 | 1,905 | 7,078 | 13 | 5.35 | 101 Tiles | 97% |
| 64 Tiles | 8,200 | 4,281 | 16,400 | 30 | 8.17 | 66 Tiles | 98% |
| 48 Tiles | 14,503 | 7,452 | 29,006 | 54 | 10.76 | 49 Tiles | 99% |
| 32 Tiles | 33,018 | 16,697 | 66,036 | 123 | 16.20 | 32 Tiles | 99% |
| 24 Tiles | 58,408 | 29,297 | 116,816 | 217 | 21.38 | 24 Tiles | 99% |
| 16 Tiles | 132,781 | 66,105 | 265,562 | 494 | 32.24 | 16 Tiles | 99% |

**A Cell is 32 Tiles.** The 32-Tile rung is one Street on every Cell boundary, and it is the rung that reproduces the corpus's placeholder — which is worth stating, because it means the placeholder was never arbitrary. Whether it is *right* is the km/km² column, against a real city.

### Footprint against Segment count

`(saved AND hashed)` and `(derived AND rebuilt)` separated, because the second half is
what `adr/0040` makes free to change forever and what a later optimisation may delete.

| Block | Segments | Saved | Derived | Total | Bytes/Segment | Managed heap Δ |
|---:|---:|---:|---:|---:|---:|---:|
| 128 Tiles | 2,072 | 61 KiB | 73 KiB | 134 KiB | 66 | 135 KiB |
| 96 Tiles | 3,539 | 104 KiB | 124 KiB | 229 KiB | 66 | 94 KiB |
| 64 Tiles | 8,200 | 241 KiB | 288 KiB | 530 KiB | 66 | 300 KiB |
| 48 Tiles | 14,503 | 426 KiB | 510 KiB | 937 KiB | 66 | 414 KiB |
| 32 Tiles | 33,018 | 968 KiB | 1.1 MiB | 2.0 MiB | 66 | 1.1 MiB |
| 24 Tiles | 58,408 | 1.6 MiB | 2.0 MiB | 3.6 MiB | 66 | 1.5 MiB |
| 16 Tiles | 132,781 | 3.7 MiB | 4.5 MiB | 8.3 MiB | 65 | 4.6 MiB |

The managed-heap column is the K0 discipline — a computed figure cannot see what the allocator actually charges — and here it also carries the generator's transient `List<T>` scaffolding, so it is an upper bound on the graph rather than a measurement of it.

### What per-direction `volume / capacity` costs

`plans/0010` forbids R0 from settling this and requires it parameterised. This is the price;
what it buys is not visible until R2 has volume to attribute.

| Block | Segments | Per Segment | Per direction | Δ | Δ as share of total |
|---:|---:|---:|---:|---:|---:|
| 128 Tiles | 2,072 | 134 KiB | 142 KiB | 8 KiB | 5% |
| 96 Tiles | 3,539 | 229 KiB | 243 KiB | 13 KiB | 5% |
| 64 Tiles | 8,200 | 530 KiB | 562 KiB | 32 KiB | 5% |
| 48 Tiles | 14,503 | 937 KiB | 993 KiB | 56 KiB | 5% |
| 32 Tiles | 33,018 | 2.0 MiB | 2.2 MiB | 128 KiB | 5% |
| 24 Tiles | 58,408 | 3.6 MiB | 3.9 MiB | 228 KiB | 5% |
| 16 Tiles | 132,781 | 8.3 MiB | 8.8 MiB | 518 KiB | 5% |

### Per column, at the working rung

Block 32 Tiles, 8 Arterials, 33,018 Segments, 16,697 nodes, 66,036 arcs.

| Column | Group | Count | Bytes each | Bytes | Declaration |
|---|---|---:|---:|---:|---|
| `NodeX` | Nodes | 16,697 | 4 | 65 KiB | `(saved AND hashed)` |
| `NodeY` | Nodes | 16,697 | 4 | 65 KiB | `(saved AND hashed)` |
| `SegmentNodeA` | Segments | 33,018 | 4 | 128 KiB | `(saved AND hashed)` |
| `SegmentNodeB` | Segments | 33,018 | 4 | 128 KiB | `(saved AND hashed)` |
| `SegmentLengthTiles` | Segments | 33,018 | 4 | 128 KiB | `(saved AND hashed)` |
| `SegmentCapacity` | Segments | 33,018 | 4 | 128 KiB | `(saved AND hashed)` |
| `SegmentFreeFlow` | Segments | 33,018 | 4 | 128 KiB | `(saved AND hashed)` |
| `SegmentModes` | Segments | 33,018 | 1 | 32 KiB | `(saved AND hashed)` |
| `SegmentFidelity` | Segments | 33,018 | 1 | 32 KiB | `(saved AND hashed)` |
| `Volume` | Volume | 33,018 | 4 | 128 KiB | `(saved AND hashed)` |
| `ArcStart` | Arcs | 16,698 | 4 | 65 KiB | `(derived AND rebuilt)` |
| `ArcTarget` | Arcs | 66,036 | 4 | 257 KiB | `(derived AND rebuilt)` |
| `ArcSegment` | Arcs | 66,036 | 4 | 257 KiB | `(derived AND rebuilt)` |
| `ArcModes` | Arcs | 66,036 | 1 | 64 KiB | `(derived AND rebuilt)` |
| `ArcCarTicks` | Arcs | 66,036 | 4 | 257 KiB | `(derived AND rebuilt)` |
| `ArcFootTicks` | Arcs | 66,036 | 4 | 257 KiB | `(derived AND rebuilt)` |

**Total 2.0 MiB**, against S4's K0 figure of **172.3 MiB** for the whole world at 1M. The Road Graph is not the memory problem, which is the same thing K0 found about the Microscopic Cap and is worth stating before any router is measured on top of it.

### What the Arterials do to the grid

An Arterial occupies the ground it crosses, so every Street it crosses is deleted or kept
as a foot crossing. This is the detour the router will be asked about, and it is the only
way `CONTEXT.md` → Severance is observable at all.

| Arterials | Runs | Ramps | Mean run | Segments | Severed | Foot crossings | Car-admitting | Foot-admitting |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 0 | — | 33,709 | 0 | 0 | 33,024 | 33,709 |
| 2 | 17 | 19 | 512 Tiles | 33,464 | 281 | 93 | 32,686 | 33,428 |
| 4 | 28 | 32 | 512 Tiles | 33,293 | 476 | 158 | 32,450 | 33,233 |
| 8 | 48 | 56 | 512 Tiles | 33,018 | 795 | 264 | 32,069 | 32,914 |
| 16 | 84 | 100 | 512 Tiles | 32,492 | 1,401 | 466 | 31,341 | 32,308 |
| 32 | 170 | 202 | 512 Tiles | 31,321 | 2,760 | 920 | 29,716 | 30,949 |

**Severed count is a road-network fact, not yet a Severance measurement.** Whether a neighbourhood is actually cut off on foot is a reachability question over the foot subgraph, and answering it needs a search — which is R0's second half. What this table establishes is that there is something to find: the graph has genuine barriers in it rather than a grid with decorative diagonals drawn over the top.


## S2 R0 — the denominator, and the heuristic ladder

- **Captured** 2026-08-06 18:12:57 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 1 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Build** Release

Working rung: block 32 Tiles, 8 Arterials, 33,018 Segments, 16,697 nodes, 66,036 arcs.

Cost is time, Q16.16 Ticks. The query is `(Segment, offset) → (Segment, offset)`,
seeded from both endpoints of the origin Segment and terminated on either endpoint of
the goal Segment plus the offset remainder — never node to node.

### The denominator — one uncached A\* search

No hierarchy, no cache. Every rung timed, not just the admissible ones, because **the
expansion count is not the cost**: a tighter metric that has to be computed can lose to a
looser one that does not, and nothing in the plan's ladder would have shown it.

| Query | Heuristic | Mean expanded | Bootstrap | Search | Total | ns per expansion |
|---|---|---:|---:|---:|---:|---:|
| drive | `None` | 8,217 | 295 ns | 1,240,382 ns | 1,240,677 ns | 150 ns |
| drive | `Manhattan` | 2,813 | 375 ns | 429,300 ns | 429,676 ns | 152 ns |
| drive | `Octile` | 3,506 | 256 ns | 474,677 ns | 474,934 ns | 135 ns |
| drive | `Chebyshev` | 4,121 | 249 ns | 418,260 ns | 418,510 ns | 101 ns |
| drive | `EuclideanFloor` | 3,712 | 465 ns | 715,282 ns | 715,747 ns | 192 ns |
| walk | `None` | 276 | 179 ns | 20,391 ns | 20,570 ns | 73 ns |
| walk | `Manhattan` | 32 | 246 ns | 4,040 ns | 4,286 ns | 126 ns |
| walk | `Octile` | 46 | 217 ns | 5,261 ns | 5,478 ns | 114 ns |
| walk | `Chebyshev` | 58 | 221 ns | 6,359 ns | 6,581 ns | 109 ns |
| walk | `EuclideanFloor` | 50 | 358 ns | 10,320 ns | 10,678 ns | 206 ns |

**Bootstrap is the query shape's fixed overhead** — seeding both origin endpoints and resolving both goal remainders — measured by re-running the same query set with the search loop omitted, rather than by a per-query stopwatch whose own cost would be a visible share of it. The queries are drawn before the clock starts. It is reported separately because a node-to-node denominator would not have paid it at all, and every figure in this spike divides by this one.

**The `ns per expansion` column is the one that surprised.** `EuclideanFloor` is the tightest safe metric and expands the fewest nodes of the two safe rungs — and it is not the fastest, because its exact integer square root is a sixteen-iteration loop run twice for every node pushed. `Chebyshev` computes in three instructions and expands more. Which one is actually cheaper is a measurement, and it is the reason this table times every rung rather than reporting expansions and calling that cost.

### The denominator by origin-destination distance

**R1 has not run, so R0 does not guess its distribution.** Queries are drawn uniformly and
reported per distance bucket, so R1's distribution applies afterwards as weights over
buckets that already exist — no re-run, and R1's result composes with R0's.

| Query | O-D distance | Count | Mean expanded | Mean Segments | Mean cost |
|---|---|---:|---:|---:|---:|
| drive | < 1 km | 29 | 101 | 8 | 8.69 Ticks |
| drive | 1–2 km | 60 | 373 | 19 | 19.19 Ticks |
| drive | 2–4 km | 227 | 1,110 | 35 | 35.92 Ticks |
| drive | 4–8 km | 647 | 2,913 | 55 | 60.71 Ticks |
| drive | 8–16 km | 992 | 5,767 | 69 | 89.59 Ticks |
| drive | > 16 km | 45 | 7,987 | 73 | 117.88 Ticks |
| walk | < 1 km | 831 | 23 | 6 | 61.80 Ticks |
| walk | 1–2 km | 1,166 | 72 | 12 | 121.04 Ticks |
| walk | 8–16 km | 3 | 4,409 | 120 | 1121.30 Ticks |

### The denominator's own quality

Against `Chebyshev`, the rung R0 publishes. A weak denominator flatters every ratio
built on it — HPA\*'s speedup, the cache's value, R2's crossover — so it is stated here.

| Query | A\* expanded | Dijkstra expanded | A\* as share | Expanded per path Segment |
|---|---:|---:|---:|---:|
| drive | 3,969 | 8,104 | 48% | 68 |
| walk | 66 | 280 | 23% | 7 |

### The heuristic ladder

Judged against Dijkstra ground truth on **the same query**, run through the same loop —
`HeuristicKind.None` is not a second implementation that could disagree for its own reasons.

**The non-optimal counts are a lower bound, and the reason is worth carrying forward.**
The heuristic converts Tiles to Ticks by multiplying by a floored reciprocal rather than
dividing, which is what removes four hardware divisions per node. The reciprocal's own
rounding leaves roughly two parts in ten thousand of slack, and that slack **partially
cancels an overestimating metric's error**. Measured: switching the exact division for the
reciprocal moved walking `Manhattan` from **35 of 300** to **4 of 300** while leaving
driving `Manhattan` at 13 — short walks are where the two errors are comparable in size.

So an implementation detail chosen for speed makes an unsafe heuristic *look* safer, and
it does so most exactly where the design cares most — `adr/0008`'s walk Legs. The verdict
below does not rest on the rate: `Manhattan` and `Octile` overestimate on this graph by
construction, and a rate that moves with an unrelated optimisation is not the evidence.

| Query | Heuristic | Admissible on | Mean expanded | vs Dijkstra | Non-optimal |
|---|---|---|---:|---:|---:|
| drive | `None` | —, it *is* the ground truth | 8,104 | 100% | 0 of 300 |
| drive | `Manhattan` | 4-connected only | 2,716 | 33% | 13 of 300 |
| drive | `Octile` | 8-connected only | 3,380 | 41% | 2 of 300 |
| drive | `Chebyshev` | any graph | 3,969 | 48% | 0 of 300 |
| drive | `EuclideanFloor` | any graph | 3,572 | 44% | 0 of 300 |
| walk | `None` | —, it *is* the ground truth | 280 | 100% | 0 of 300 |
| walk | `Manhattan` | 4-connected only | 29 | 10% | 4 of 300 |
| walk | `Octile` | 8-connected only | 53 | 18% | 0 of 300 |
| walk | `Chebyshev` | any graph | 66 | 23% | 0 of 300 |
| walk | `EuclideanFloor` | any graph | 58 | 20% | 0 of 300 |

### The verdict R0 owes — the Arterial density at which admissibility breaks

Non-optimal routes returned, out of routes found, against the number of freeform Arterials.
Driving only: an Arterial carries no pedestrian edges, so it cannot shortcut a walk.

| Arterials | Severed | `None` | `Manhattan` | `Octile` | `Chebyshev` | `EuclideanFloor` | 
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 0 of 300 | 0 of 300 | 0 of 300 | 0 of 300 | 0 of 300 | 
| 2 | 281 | 0 of 300 | 7 of 300 | 1 of 300 | 0 of 300 | 0 of 300 | 
| 4 | 476 | 0 of 300 | 19 of 300 | 3 of 300 | 0 of 300 | 0 of 300 | 
| 8 | 795 | 0 of 300 | 13 of 300 | 2 of 300 | 0 of 300 | 0 of 300 | 
| 16 | 1,401 | 0 of 294 | 22 of 294 | 5 of 294 | 0 of 294 | 0 of 294 | 
| 32 | 2,760 | 0 of 267 | 36 of 267 | 4 of 267 | 0 of 267 | 0 of 267 | 

### Walking: severed, or merely far

`plans/0010`: whether the router can tell **severed** from **merely far** is two different
Trip Fates and two different player-facing diagnoses, and a search-radius bound chosen for
performance would collapse them into one. This search has **no radius bound**, so the
distinction is real rather than definitional: *no route found* is Severance, and a long
route is a long walk.

**The first capture reported zero unreachable walks, and zero is exactly the reading that
cannot be trusted on its own** — it is equally consistent with *this city is well connected*
and with *this instrument cannot see Severance*. So the crossing density is swept until the
count moves. A measurement that has never been observed to fire is not evidence.

| Arterials | Foot crossing every | Crossings | No route found | Mean cost when found |
|---:|---|---:|---:|---:|
| 8 | every severed Street | 1,059 | 0 of 300 | 732.31 Ticks |
| 8 | 4th severed Street | 264 | 0 of 300 | 732.30 Ticks |
| 8 | 16th severed Street | 66 | 0 of 300 | 750.82 Ticks |
| 8 | never | 0 | 0 of 300 | 837.92 Ticks |
| 32 | 4th severed Street | 920 | 0 of 300 | 752.67 Ticks |
| 32 | 16th severed Street | 230 | 9 of 300 | 932.18 Ticks |
| 32 | never | 0 | 230 of 300 | 722.71 Ticks |

**The instrument fires, so its zeroes mean something.** At the working rung the foot network stays connected — 32,914 of 33,018 Segments admit a pedestrian and the crossings leave no island — and that is now a finding rather than an absence of evidence, because the same measurement reaches 230 of 300 unreachable once the crossings are removed at 32 Arterials.

**Severance is a property of crossing density, not of Arterial count.** Eight Arterials with no crossings at all sever nothing, because eight lines do not partition a plane into pieces anyone wants to walk between; thirty-two with no crossings sever almost everything. The parameter that decides is the one a player actually controls when they choose whether to build a bridge, which is the right place for it to live.

**One column reads backwards and it is not an error.** *Mean cost when found* falls at 32 Arterials with no crossings — 722 Ticks, below the 932 of the rung above it — because by then only the nearby pairs are reachable at all. It is survivorship: the long walks did not get slower, they stopped being in the sample. A mean conditioned on success cannot be read beside a failure count without saying so.


## S2 R1 — the travel-time matrix

- **Captured** 2026-08-06 18:13:22 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 1 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Build** Release

The matrix is **District**-to-District. `plans/0010` says *zone* in six places and `CONTEXT.md` → Zone is a permission set over land, while `CONTEXT.md` → District is *"the granularity of the travel-time matrix"*. The plan is owed the correction.

Every congested figure below rests on a **synthetic** monocentric peak — there are no Travellers in S2 and R2 is where volume comes from a routed load. So no number here says how asymmetric a real city is. Each says: *at directional imbalance `i`, this.*

### R1.1 — the partition, and what a District rung actually is

| Per side | Districts | Cells each | Area | Empty | vs the corpus |
|---:|---:|---:|---:|---:|---|
| 4 | 16 | 1,024 | 16.77 km² | 0 | — |
| 8 | 64 | 256 | 4.19 km² | 0 | — |
| 10 | 100 | 164 | 2.68 km² | 0 | `plans/0001`'s 100–400 band |
| 11 | 121 | 135 | 2.21 km² | 0 | **`CONTEXT.md` → District's 128-Cell anchor** |
| 16 | 256 | 64 | 1.04 km² | 0 | — |
| 20 | 400 | 41 | 0.67 km² | 0 | `plans/0001`'s 100–400 band |
| 32 | 1,024 | 16 | 0.26 km² | 0 | — |
| 45 | 2,025 | 8 | 0.13 km² | 0 | `plans/0010`'s 2,000-District row |
| 64 | 4,096 | 4 | 0.06 km² | 0 | — |

**Cell-aligned, never Chunk-aligned** (`CONTEXT.md` → District). The anchor — 128 Cells, 2.10 km² — lands at 11 a side, which is inside `plans/0001`'s 100–400 figure. That agreement is arithmetic, not corroboration: `plans/0001` predates the 1M target.

### R1.2 — cold build, and resident size measured twice

A cold build is **one forward one-to-all Dijkstra per District**, not one search per pair: a forward search from District *i* fills the whole of row *i*. Nothing is halved by symmetry — the matrix is asymmetric and the reverse row needs a backward search.

**Resident size is reported twice** because `03 §3.3` needs both: the scalar matrix the choice loop reads, and the cached District-pair *routes* volume would be distributed along. They differ by far more than a constant factor — *n²* integers against *n²* variable-length Segment sequences.

| Districts | Searches | Cold build | Per search | Settled | Scalar matrix | Mean route | Route store |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 16 | 16 | 23.08 ms | 1,442,675 ns | 16,685 | 1.00 KiB | 56 Segments | 56.00 KiB |
| 64 | 64 | 94.67 ms | 1,479,227 ns | 16,685 | 16.00 KiB | 64 Segments | 1.00 MiB |
| 100 | 100 | 145.92 ms | 1,459,245 ns | 16,685 | 39.06 KiB | 63 Segments | 2.40 MiB |
| 121 | 121 | 177.20 ms | 1,464,488 ns | 16,685 | 57.19 KiB | 65 Segments | 3.63 MiB |
| 256 | 256 | 378.37 ms | 1,478,045 ns | 16,685 | 256.00 KiB | 65 Segments | 16.25 MiB |
| 400 | 400 | 581.92 ms | 1,454,806 ns | 16,685 | 625.00 KiB | 65 Segments | 39.67 MiB |
| 1,024 | 1,024 | 1502.20 ms | 1,466,999 ns | 16,685 | 4.00 MiB | 70 Segments | 280.00 MiB |
| 2,025 | 2,025 | 2976.88 ms | 1,470,065 ns | 16,685 | 15.64 MiB | 69 Segments | 1.05 GiB |
| 4,096 | 4,096 | 6069.97 ms | 1,481,928 ns | 16,685 | 64.00 MiB | 65 Segments | 4.06 GiB |

*Settled* is nodes settled by the last search of the rung — constant across rungs by construction, because a one-to-all has no goal to prune toward. **That is why cold build is linear in District count, and it is also why a departure from linearity is readable as an artefact rather than as a finding.** The whole sweep is walked once untimed before any of it is timed, because four weaker warm-ups each left a per-search cost falling smoothly with District count — the shape a reader would most readily believe, and the process leaving tier 0. `OneToAll.Run` is called once per District, so the early rungs are the ones that call it too few times.

*Route store* is `n² × mean route length × 4 B`, the Segment sequences at four bytes each. **It is the figure that fails first**, and it fails on `adr/0006` grounds rather than performance ones — see R1.7, where the same store turns out to be what a dirty-region rebuild needs in order to be sound.

### R1.3 — the read, in both access patterns

**The read is the measurement that matters most.** `02 §5.8` makes *never resolve a route inside the choice loop* a rule — named as the one thing UrbanSim gets architecturally right that this design must not violate. If the matrix read is not cheap, that rule is unenforceable and the finding is larger than S2.

`references.md §2` describes the choice loop **twice in one sentence** — *"what is the commute from this candidate dwelling to any job?"*, which is one origin against many destinations and therefore a sequential **row scan**, and *"many-to-many, evaluated tens of thousands of times per cycle"*, which reads as **scattered**. Both are timed, so which one the choice loop performs becomes a design question with a priced answer rather than a detail settled by whoever writes the loop.

| Districts | Resident | Row scan | Scattered | Scattered ÷ row | vs K2 |
|---:|---:|---:|---:|---:|---:|
| 16 | 1.00 KiB | 0.71 ns | 1.13 ns | 1.59× | 0.08× |
| 64 | 16.00 KiB | 0.50 ns | 1.15 ns | 2.30× | 0.08× |
| 100 | 39.06 KiB | 0.58 ns | 1.23 ns | 2.13× | 0.09× |
| 121 | 57.19 KiB | 0.56 ns | 1.14 ns | 2.04× | 0.08× |
| 256 | 256.00 KiB | 0.53 ns | 1.30 ns | 2.44× | 0.09× |
| 400 | 625.00 KiB | 0.51 ns | 1.52 ns | 2.95× | 0.11× |
| 1,024 | 4.00 MiB | 0.57 ns | 1.64 ns | 2.88× | 0.12× |
| 2,025 | 15.64 MiB | 0.58 ns | 2.88 ns | 4.95× | 0.21× |
| 4,096 | 64.00 MiB | 0.57 ns | 5.00 ns | 8.76× | 0.36× |

**The tripwire, and it is a real threshold rather than a tautology.** `plans/0010` rewrote this wire during grilling for exactly the reason the numbers now show: *"a lookup into an n×n array is O(1) by construction, so the original wire — 'not O(1) and cheap' — could not fire on any plausible implementation."* What binds is where the matrix lives. The wire that replaced it fires if a read costs more than S4's K2 random gather, **13.6 ns per handle** on this machine — the `SoaScattered` figure, which is the closest thing in the corpus to a priced cache miss.

The row scan is immune to the ceiling and the scattered read is not, which is the whole of the finding: **District count has a ceiling set by L3, and the ceiling only exists for one of the two access patterns.** A player drawing thousands of Districts would be drawing a performance cliff, so District count is not a free UI decision — but it is only a cliff if the choice loop scatters.

### R1.4 — asymmetry, and the decision that decides whether it exists

**The volume-scope question and the `adr/0020` exposure are one question.** R0 was forbidden from settling `volume / capacity` per Segment or per direction and priced it at 5% of the graph, saying *"what it buys is not visible until R2 has volume to attribute"*. That is not quite right, and R1 is where it shows: under **per Segment** the two directions share one counter, so the VDF returns the same delay both ways and **the matrix is symmetric to the bit** — which makes `adr/0020`'s union-find exactly correct, not by evidence but by construction.

| Scope | Imbalance | Pairs | Mean | Median | p90 | Max | Mean, relative |
|---|---:|---:|---:|---:|---:|---:|---:|
| per Segment | 0.00% | 7,260 | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00% |
| **per direction** | 0.00% | 7,260 | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00% |
| per Segment | 20.00% | 7,260 | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00% |
| **per direction** | 20.00% | 7,260 | 0.78 Ticks | 0.33 Ticks | 2.22 Ticks | 8.15 Ticks | 1.31% |
| per Segment | 50.00% | 7,260 | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00% |
| **per direction** | 50.00% | 7,260 | 2.19 Ticks | 0.91 Ticks | 6.27 Ticks | 24.44 Ticks | 3.54% |
| per Segment | 80.00% | 7,260 | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00% |
| **per direction** | 80.00% | 7,260 | 4.13 Ticks | 1.69 Ticks | 12.03 Ticks | 50.39 Ticks | 6.27% |
| per Segment | 100.00% | 7,260 | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00 Ticks | 0.00% |
| **per direction** | 100.00% | 7,260 | 5.88 Ticks | 2.43 Ticks | 17.05 Ticks | 75.89 Ticks | 8.47% |

Every per-Segment row reading exactly zero is **the instrument working, not failing**: it is the same measurement R0.5 had to sweep crossing density to obtain, arrived at from the other side. A zero that can be made non-zero by moving one parameter is evidence; a zero that has never been observed to move is not.

### R1.5 — `adr/0020`: union-find against the definition it claims to compute

`adr/0020` computes a Settlement as *"a **connected component** of the District graph… **a union-find** over data already being maintained, at effectively no cost"*. `CONTEXT.md` → Settlement defines one as *"a maximal set of Districts **mutually** reachable within the Commute Budget."* **Union-find computes weak connectivity; "mutually reachable" is strong connectivity**, and the two coincide only on a symmetric matrix.

So the test is not the asymmetry distribution — that is a number about travel times. The test is whether the two algorithms **disagree about the city**, because a Settlement is an object the game is made of. The Commute Budget has no value anywhere in the corpus, so it is swept: at a Budget longer than any commute everything is one Settlement under both readings, and at a Budget shorter than any it is all singletons. **The divergence lives in the middle, which is where the game is played.**

| Budget | Union-find | Tarjan SCC | Largest weak | Largest strong | One-way pairs |
|---:|---:|---:|---:|---:|---:|
| 20 Ticks | 6 | 8 | 90 | 70 | 13 |
| 30 Ticks | 2 | 2 | 120 | 120 | 45 |
| 40 Ticks | 1 | 1 | 121 | 121 | 76 |
| 50 Ticks | 1 | 1 | 121 | 121 | 146 |
| 60 Ticks | 1 | 1 | 121 | 121 | 208 |
| 80 Ticks | 1 | 1 | 121 | 121 | 264 |
| 120 Ticks | 1 | 1 | 121 | 121 | 47 |

**One-way pairs are the exposure in one number** — District pairs within budget in exactly one direction, which is precisely what union-find merges and Tarjan does not. The headline case is the one `plans/0010` named before the numbers arrived: inbound within budget at the morning peak, outbound not.

**The one-way count rises and then falls, and the fall is the curve rather than an error.** A pair can only be one-way while the Budget sits between its two directions' costs, so a Budget below every commute produces none and a Budget above every commute produces none either. **The exposure is a band, not a threshold** — which matters more than the peak height, because it says the disagreement cannot be designed away by choosing a generous Commute Budget. A Budget generous enough to close it is a Budget that has stopped bounding anything, and `CONTEXT.md` → Commute Budget exists to make geography matter.

### R1.6 — what the matrix entry is wrong by

**Not in `plans/0010`, and it should have been.** The plan measures four things — build, rebuild, size, read — and all four are about cost. None asks how *wrong* an entry is. A District-to-District entry is one number standing for every Access Point pair inside those two Districts, and **if its error exceeds what the Commute Budget resolves, "the matrix carries the choice loop" is false however fast the read is.** That is a prior question to every figure above.

Measured against the query the game actually issues: R0's `(Segment, offset) → (Segment, offset)` A\* with `Chebyshev`, drawn inside the two Districts, against the same congested arc costs the matrix was built on.

| Districts | Pairs | Searches | Mean error | p90 | Max | Mean, relative |
|---:|---:|---:|---:|---:|---:|---:|
| 16 | 374 | 2,244 | 15.04 Ticks | 31.62 Ticks | 72.93 Ticks | 24.70% |
| 64 | 395 | 2,376 | 9.38 Ticks | 20.87 Ticks | 64.98 Ticks | 16.82% |
| 121 | 395 | 2,376 | 6.73 Ticks | 14.04 Ticks | 77.62 Ticks | 11.32% |
| 400 | 396 | 2,382 | 4.00 Ticks | 7.42 Ticks | 61.52 Ticks | 6.91% |
| 1,024 | 399 | 2,400 | 2.41 Ticks | 4.14 Ticks | 50.45 Ticks | 3.80% |

**The error is a property of District extent, and it shrinks the way the resident size grows.** That is the trade R1 exists to price, and it is the reason District count cannot be chosen on cache behaviour alone: the rung that fits in L3 is the rung whose entries stand for the most ground.

### R1.7 — incremental rebuild, and what a dirty region cannot see

`02 §6` describes the matrix as rebuilt at a *slow cadence, dirty regions only*. **That is a spatial invalidation mechanism, and routes invalidate against a scalar Epoch** — `plans/0010` R5 flags that *"two invalidation mechanisms are in the corpus and nothing relates them, so the matrix and the cache can disagree about what the network currently is"*, and requires R1 to state whether the matrix carries an Epoch at all. **It does not, and it must not be given one silently** — a version counter would imply a relationship to the route cache that nobody has argued.

The measurement below is what makes that more than bookkeeping. One District's roads are bulldozed — `plans/0010`'s *"in a city builder link deletion is the core verb"* — and the matrix is rebuilt three ways, each checked against a full rebuild's ground truth.

**Two edit sites, because one of them is degenerate and a single row would not have shown it.** A central District is on the shortest path between most pairs on the map; a corner District is on almost none. The mechanism's worth is the difference between them, and a report that bulldozed only the middle would have concluded that incremental rebuild is worthless.

| Edit site | Rung | Rows rebuilt | Cost | Entries missed | Sound |
|---|---|---:|---:|---:|---|
| **Centre** | Full rebuild | 121 | 177.56 ms | 0 | yes, by definition |
| | Dirty region — rows whose District touches it | 1 | 0.16 ms | 309 of 429 | **no** |
| | Routes crossing it — needs the route store | 121 rows, 430 entries | 176.83 ms | 0 of 429 | yes |
| **Corner** | Full rebuild | 121 | 173.78 ms | 0 | yes, by definition |
| | Dirty region — rows whose District touches it | 1 | 0.00 ms | 132 of 252 | **no** |
| | Routes crossing it — needs the route store | 121 rows, 253 entries | 175.51 ms | 0 of 252 | yes |

The centre edit severs **484 arcs** and moves **429** of the matrix's 14,641 entries; the corner edit severs **552** and moves **252**.

**The dirty region is a spatial test on a non-spatial quantity, and that is the finding.** A path from District *i* to District *j* can cross the edited ground without either endpoint being near it, so rebuilding by *which Districts the region overlaps* misses exactly the long routes the matrix exists to serve — and it misses them **silently**, leaving entries that are stale rather than merely coarse. Making it sound requires knowing **which routes crossed the region**, which means keeping the route store R1.2 priced, and that store is the one that does not fit. **The matrix's cheap invalidation and its cheap storage are the same trade, taken twice.**

**And the sound rung rebuilds every row anyway, which is a finding about the matrix rather than about the edit.** A one-to-all fills a whole row, so the build granularity *is* the row — and every row holds the entry addressed *to* the edited District, whose route necessarily ends inside it. So however few entries an edit invalidates, **at least one lands in every row and the incremental path collapses into the full one.** The entries column is the work genuinely needed; the rows column is the work the structure forces. An incremental rebuild worth having would need a build kernel finer than one-to-all — a point-to-point search per entry, which R0 priced at 435 µs against this row's ~1.5 ms for a hundred and twenty-one of them.

### R1.8 — time resolution: one matrix, or five

**A second unstated axis, and `plans/0010` is right that it interacts with everything else.** A single Day-average matrix cannot represent the peak every other figure in this spike is measured at: morning inbound and evening outbound cancel, and the asymmetry the directed graph exists to carry vanishes into the mean. A per-phase matrix multiplies build cost and resident size by five and gives the choice loop a travel time that matches the moment being asked about.

| Resolution | Build | Resident | Mean asymmetry | p90 | One-way pairs at 40 Ticks |
|---|---:|---:|---:|---:|---:|
| Day average, one matrix | 252.65 ms | 57.19 KiB | 0.08 Ticks | 0.23 Ticks | 1 |
| — of which `Dawn` | | 57.19 KiB | 0.00 Ticks | 0.02 Ticks | 0 |
| — of which `MorningPeak` | | 57.19 KiB | 2.19 Ticks | 6.27 Ticks | 76 |
| — of which `Midday` | | 57.19 KiB | 0.00 Ticks | 0.00 Ticks | 0 |
| — of which `EveningPeak` | | 57.19 KiB | 1.79 Ticks | 5.10 Ticks | 64 |
| — of which `Night` | | 57.19 KiB | 0.00 Ticks | 0.00 Ticks | 0 |
| **Per phase, five matrices** | 970.26 ms | 285.95 KiB | | | |

**The Day-average row is the one to read, and it should be read against the peak rows rather than against the others.** It is the matrix a single-resolution design gives the choice loop, and what it reports about the morning peak is what a Household would be deciding on.

The average is taken over the **cost**, not over the volume. BPR is convex, so the delay at the mean volume is strictly less than the mean of the delays — averaging volumes first would give a Day-average matrix describing a city with no rush hour in it at all, rather than one whose rush hour has been smeared. It is also **unweighted**, because the sun arc's phase widths are `plans/0010`'s open decision 5a and an unweighted mean is the only average available while they are unsized.

**And that is the refresh-cadence decision arriving from a direction nobody expected.** `plans/0010` decision 2 files the matrix refresh cadence as *almost certainly hash-bearing* — two cadences produce two cities, so it is a design change under `05 §4` rather than a free knob. Time *resolution* is the same class of decision and the corpus has not named it at all: a Day-average matrix and a per-phase one give the choice loop different answers to the same question, so they are different cities, and **the choice belongs beside cadence in whatever settles that.**


---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 77.65 s — from 18:12:57 UTC to 18:14:14 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.46 / 1.40 / 1.24 (1 / 5 / 15 min)
- **Load average, at end** 1.19 / 1.32 / 1.23 (1 / 5 / 15 min)
- **CPU stall** 591,539 µs over the run — 0.76% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 11,292 µs over the run — 0.01% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
