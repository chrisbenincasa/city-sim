## S2 R0 — the synthetic Road Graph

- **Captured** 2026-08-11 11:25:34 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
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
| 48 Tiles | 14,503 | 426 KiB | 510 KiB | 937 KiB | 66 | 406 KiB |
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

- **Captured** 2026-08-11 11:25:34 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
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
| drive | `None` | 8,217 | 348 ns | 798,374 ns | 798,722 ns | 97 ns |
| drive | `Manhattan` | 2,813 | 357 ns | 319,624 ns | 319,981 ns | 113 ns |
| drive | `Octile` | 3,506 | 342 ns | 389,582 ns | 389,924 ns | 111 ns |
| drive | `Chebyshev` | 4,121 | 313 ns | 457,429 ns | 457,743 ns | 110 ns |
| drive | `EuclideanFloor` | 3,712 | 612 ns | 842,349 ns | 842,962 ns | 226 ns |
| walk | `None` | 276 | 396 ns | 21,049 ns | 21,445 ns | 76 ns |
| walk | `Manhattan` | 32 | 267 ns | 4,369 ns | 4,637 ns | 136 ns |
| walk | `Octile` | 46 | 399 ns | 7,411 ns | 7,810 ns | 161 ns |
| walk | `Chebyshev` | 58 | 274 ns | 6,953 ns | 7,228 ns | 119 ns |
| walk | `EuclideanFloor` | 50 | 429 ns | 10,909 ns | 11,338 ns | 218 ns |

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

- **Captured** 2026-08-11 11:26:01 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
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
| 16 | 16 | 25.38 ms | 1,586,709 ns | 16,685 | 1.00 KiB | 56 Segments | 56.00 KiB |
| 64 | 64 | 102.77 ms | 1,605,838 ns | 16,685 | 16.00 KiB | 64 Segments | 1.00 MiB |
| 100 | 100 | 167.81 ms | 1,678,117 ns | 16,685 | 39.06 KiB | 63 Segments | 2.40 MiB |
| 121 | 121 | 192.01 ms | 1,586,874 ns | 16,685 | 57.19 KiB | 65 Segments | 3.63 MiB |
| 256 | 256 | 521.50 ms | 2,037,118 ns | 16,685 | 256.00 KiB | 65 Segments | 16.25 MiB |
| 400 | 400 | 679.73 ms | 1,699,334 ns | 16,685 | 625.00 KiB | 65 Segments | 39.67 MiB |
| 1,024 | 1,024 | 1792.70 ms | 1,750,683 ns | 16,685 | 4.00 MiB | 70 Segments | 280.00 MiB |
| 2,025 | 2,025 | 3471.06 ms | 1,714,104 ns | 16,685 | 15.64 MiB | 69 Segments | 1.05 GiB |
| 4,096 | 4,096 | 7008.72 ms | 1,711,115 ns | 16,685 | 64.00 MiB | 65 Segments | 4.06 GiB |

*Settled* is nodes settled by the last search of the rung — constant across rungs by construction, because a one-to-all has no goal to prune toward. **That is why cold build is linear in District count, and it is also why a departure from linearity is readable as an artefact rather than as a finding.** The whole sweep is walked once untimed before any of it is timed, because four weaker warm-ups each left a per-search cost falling smoothly with District count — the shape a reader would most readily believe, and the process leaving tier 0. `OneToAll.Run` is called once per District, so the early rungs are the ones that call it too few times.

*Route store* is `n² × mean route length × 4 B`, the Segment sequences at four bytes each. **It is the figure that fails first**, and it fails on `adr/0006` grounds rather than performance ones — see R1.7, where the same store turns out to be what a dirty-region rebuild needs in order to be sound.

### R1.3 — the read, in both access patterns

**The read is the measurement that matters most.** `02 §5.8` makes *never resolve a route inside the choice loop* a rule — named as the one thing UrbanSim gets architecturally right that this design must not violate. If the matrix read is not cheap, that rule is unenforceable and the finding is larger than S2.

`references.md §2` describes the choice loop **twice in one sentence** — *"what is the commute from this candidate dwelling to any job?"*, which is one origin against many destinations and therefore a sequential **row scan**, and *"many-to-many, evaluated tens of thousands of times per cycle"*, which reads as **scattered**. Both are timed, so which one the choice loop performs becomes a design question with a priced answer rather than a detail settled by whoever writes the loop.

| Districts | Resident | Row scan | Scattered | Scattered ÷ row | vs K2 |
|---:|---:|---:|---:|---:|---:|
| 16 | 1.00 KiB | 0.78 ns | 1.26 ns | 1.61× | 0.09× |
| 64 | 16.00 KiB | 0.47 ns | 1.19 ns | 2.50× | 0.08× |
| 100 | 39.06 KiB | 0.55 ns | 1.24 ns | 2.22× | 0.09× |
| 121 | 57.19 KiB | 0.54 ns | 1.22 ns | 2.23× | 0.08× |
| 256 | 256.00 KiB | 0.52 ns | 1.38 ns | 2.63× | 0.10× |
| 400 | 625.00 KiB | 0.54 ns | 1.74 ns | 3.22× | 0.12× |
| 1,024 | 4.00 MiB | 0.64 ns | 2.00 ns | 3.12× | 0.14× |
| 2,025 | 15.64 MiB | 0.61 ns | 4.96 ns | 8.03× | 0.36× |
| 4,096 | 64.00 MiB | 0.65 ns | 8.06 ns | 12.32× | 0.59× |

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
| **Centre** | Full rebuild | 121 | 206.51 ms | 0 | yes, by definition |
| | Dirty region — rows whose District touches it | 1 | 0.20 ms | 309 of 429 | **no** |
| | Routes crossing it — needs the route store | 121 rows, 430 entries | 234.51 ms | 0 of 429 | yes |
| **Corner** | Full rebuild | 121 | 202.92 ms | 0 | yes, by definition |
| | Dirty region — rows whose District touches it | 1 | 0.00 ms | 132 of 252 | **no** |
| | Routes crossing it — needs the route store | 121 rows, 253 entries | 198.15 ms | 0 of 252 | yes |

The centre edit severs **484 arcs** and moves **429** of the matrix's 14,641 entries; the corner edit severs **552** and moves **252**.

**The dirty region is a spatial test on a non-spatial quantity, and that is the finding.** A path from District *i* to District *j* can cross the edited ground without either endpoint being near it, so rebuilding by *which Districts the region overlaps* misses exactly the long routes the matrix exists to serve — and it misses them **silently**, leaving entries that are stale rather than merely coarse. Making it sound requires knowing **which routes crossed the region**, which means keeping the route store R1.2 priced, and that store is the one that does not fit. **The matrix's cheap invalidation and its cheap storage are the same trade, taken twice.**

**And the sound rung rebuilds every row anyway, which is a finding about the matrix rather than about the edit.** A one-to-all fills a whole row, so the build granularity *is* the row — and every row holds the entry addressed *to* the edited District, whose route necessarily ends inside it. So however few entries an edit invalidates, **at least one lands in every row and the incremental path collapses into the full one.** The entries column is the work genuinely needed; the rows column is the work the structure forces. An incremental rebuild worth having would need a build kernel finer than one-to-all — a point-to-point search per entry, which R0 priced at 435 µs against this row's ~1.5 ms for a hundred and twenty-one of them.

### R1.8 — time resolution: one matrix, or five

**A second unstated axis, and `plans/0010` is right that it interacts with everything else.** A single Day-average matrix cannot represent the peak every other figure in this spike is measured at: morning inbound and evening outbound cancel, and the asymmetry the directed graph exists to carry vanishes into the mean. A per-phase matrix multiplies build cost and resident size by five and gives the choice loop a travel time that matches the moment being asked about.

| Resolution | Build | Resident | Mean asymmetry | p90 | One-way pairs at 40 Ticks |
|---|---:|---:|---:|---:|---:|
| Day average, one matrix | 293.83 ms | 57.19 KiB | 0.08 Ticks | 0.23 Ticks | 1 |
| — of which `Dawn` | | 57.19 KiB | 0.00 Ticks | 0.02 Ticks | 0 |
| — of which `MorningPeak` | | 57.19 KiB | 2.19 Ticks | 6.27 Ticks | 76 |
| — of which `Midday` | | 57.19 KiB | 0.00 Ticks | 0.00 Ticks | 0 |
| — of which `EveningPeak` | | 57.19 KiB | 1.79 Ticks | 5.10 Ticks | 64 |
| — of which `Night` | | 57.19 KiB | 0.00 Ticks | 0.00 Ticks | 0 |
| **Per phase, five matrices** | 1130.26 ms | 285.95 KiB | | | |

**The Day-average row is the one to read, and it should be read against the peak rows rather than against the others.** It is the matrix a single-resolution design gives the choice loop, and what it reports about the morning peak is what a Household would be deciding on.

The average is taken over the **cost**, not over the volume. BPR is convex, so the delay at the mean volume is strictly less than the mean of the delays — averaging volumes first would give a Day-average matrix describing a city with no rush hour in it at all, rather than one whose rush hour has been smeared. It is also **unweighted**, because the sun arc's phase widths are `plans/0010`'s open decision 5a and an unweighted mean is the only average available while they are unsized.

**And that is the refresh-cadence decision arriving from a direction nobody expected.** `plans/0010` decision 2 files the matrix refresh cadence as *almost certainly hash-bearing* — two cadences produce two cities, so it is a design change under `05 §4` rather than a free knob. Time *resolution* is the same class of decision and the corpus has not named it at all: a Day-average matrix and a per-phase one give the choice loop different answers to the same question, so they are different cities, and **the choice belongs beside cadence in whatever settles that.**


## S2 R2 — the path source, the crossover, and the attribution lag

- **Captured** 2026-08-11 11:27:01 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
- **Build** Release

**The attribution axis is not open and nothing here reopens it.** `adr/0041` settled it on three correctness grounds and recorded which way that cut against convenience — aggregate is the cheaper scheme and the one `03 §3.3` already wrote down. R2a prices what rejecting it cost. **The path source is open**, and R1 left it carrying the whole of what remains of the DSDV question.

Every figure is at the **morning peak**, on the same synthetic monocentric field R1 used, at `v/c = 1.2` and imbalance 0.50. Travellers advance by each arc's own free-flow traversal time and **nothing feeds back** — a jam does not slow the Travellers in it. That omission is deliberate: a feedback loop would put an unargued shape inside the lag figures R2b exists to measure.

### R2.1 — the path source ladder, and the rung the plan did not have

Three ways a Traveller can be told which Segment to enter next. **The third is not in `plans/0010`** — it follows from `adr/0041`, which makes a Traveller need a *next Segment* every Tick rather than a *path*, and a next-hop table is exactly that and stores no path at all. That is distance-vector's data structure, so measuring only searched-against-shared would have answered a question the design had moved past.

| Rung | Build | Resident | Per Leg at spawn | Per crossing | Detour, mean | p90 |
|---|---:|---:|---:|---:|---:|---:|
| **searched** | — | 13.03 MiB | 751,628 ns | 40 ns | 0.00% | 0.00% |
| **shared** | 219.07 ms | 3.46 MiB | 2,827 ns | 48 ns | 36.01% | 71.39% |
| **next-hop** | 333.22 ms | 7.70 MiB | 0 ns | 65 ns | 18.52% | 40.70% |

At the anchor, 121 Districts. *Searched* resident is `in-flight × mean route × 4 B` at the derived 56,000, **not** the pool's own footprint — the pool reuses each route across many Travellers and would understate it. The pool is 2,999 routes of 3,000 drawn; the rest found no route and are excluded rather than counted as short ones.

**The detour columns are the finding, and `adr/0041` says they should not exist.** That ADR calls the path source *"a performance axis with no correctness content"*. It has correctness content: two of the three rungs aim a Traveller at a District **representative** rather than at where it is going. A shared route is coarse at both ends — the Traveller must reach the origin representative before the stored route means anything — while a next-hop table is followed from wherever the Traveller actually is, so it is **exact on the origin side and coarse only on the destination side**. 300 Legs sampled, drives drawn across the whole map.

**And there is a second correctness cost that is structural rather than statistical.** Under either coarse rung, *every* Trip bound for a District arrives through that District's one representative node — a shared route ends there and a next-hop column is a tree rooted there. So the arcs into a representative carry the whole of a District's inbound traffic, and R2b measures the consequence: a monocentric surge drives them to a `v/c` an order of magnitude past what the same surge produces under searched routes. **The representative is not a summary of the District under these rungs; it is a hole every Trip is threaded through**, and a fidelity model that promotes on `volume / capacity` would promote there and nowhere else.

Measured at **node granularity**, so both percentages are an **upper bound**: adding the two Access Point remainders leaves each detour unchanged in Ticks and raises the denominator, which lowers every percentage above. Stated because the alternative — quoting them as exact — is the shape of error this spike has already published once.

#### Resident size against District count

**The two coarse rungs scale on different axes and cross.** A route store is `n² × mean route`; a next-hop table is `nodes × n`. The store is quadratic in District count and the table is linear in it, so the rung that is cheaper depends entirely on where District count lands — which R1 left as an open trade rather than a settled number.

| Districts | Route store | Next-hop table | Mean route | vs the world's 172.3 MiB |
|---:|---:|---:|---:|---|
| 16 | 58.13 KiB | 1.01 MiB | 60 Segments | both inside it |
| 64 | 982.89 KiB | 4.07 MiB | 61 Segments | both inside it |
| 100 | 2.33 MiB | 6.36 MiB | 60 Segments | both inside it |
| 121 | 3.46 MiB | 7.70 MiB | 61 Segments | both inside it |
| 256 | 15.15 MiB | 16.30 MiB | 59 Segments | both inside it |
| 400 | 36.84 MiB | 25.47 MiB | 59 Segments | both inside it |

The sweep stops at the last rung either structure can actually be built at on this machine. R1 already reports the store's arithmetic beyond it — 4.06 GiB at 4,096 Districts — and the table's is `nodes × n × 4 B`, which at 4,096 is 261 MiB. **Neither is a rung anybody should reach**, and that is the point of printing them.

### R2.2 — the crossing rate, which `adr/0041` assumed and S2 can measure

`adr/0041` prices direct attribution from *"a vehicle crosses about one Segment per Tick"*, and names the rate as its own revisit trigger: *"if the Segment turns out much shorter than a block — S2 owns the road-density figure that decides it — the crossing rate rises and this should be re-priced before it is re-argued."* R0 measured the density. This is the rate.

**Reported at free flow and at the peak, because the ADR's estimate is a free-flow one and the simulation is not.** A Segment under BPR at `v/c = 1.2` takes about 1.3× its free-flow time, so congestion *lowers* the crossing rate and lowers direct attribution's cost with it. Quoting only the congested figure would credit the scheme for a saving the jam paid for.

| Path source | Arc costs | Crossings/vehicle/Tick | Arrivals/Tick | Mean route | Volume conserved | Bounded |
|---|---|---:|---:|---:|---|---:|
| Searched | free flow | 0.82 | 574 | 58 Segments | yes | 0 |
| Searched | morning peak | 0.79 | 545 | 58 Segments | yes | 0 |
| Shared | free flow | 0.83 | 562 | 61 Segments | yes | 0 |
| Shared | morning peak | 0.79 | 530 | 61 Segments | yes | 0 |
| NextHop | free flow | 0.83 | 556 | 61 Segments | yes | 0 |
| NextHop | morning peak | 0.79 | 529 | 61 Segments | yes | 0 |

Fleet of 40,000, warmed 40 Ticks and measured over 60. **Scale the rate, not the fleet**: `adr/0041`'s ~80,000 increment/decrement pairs per Tick is the in-flight count times this column.

**The *volume conserved* column is the one that earned its place.** `adr/0041` requires *"summed Segment volume equals the number of in-flight vehicular Travellers, every Tick"*, and names the failure it catches: *"a Traveller that vanishes without decrementing destroys the reading permanently, which is an `adr/0006`-class defect that presents as a road that looks busy forever."* The next-hop rung was written with exactly that defect — arrival was tested *after* entering the last arc — and the first capture reported a peak `v/c` of **883×** without anything else in the report looking wrong. **The invariant the ADR asked for is what found it**, on the first run it was printed. *Bounded* is the advance loop's crossings-per-Tick guard, and a non-zero figure means a zero-cost arc: a graph defect, not a result.

### R2a — the crossover, priced rather than chosen

The two schemes scale on **independent** axes — direct with vehicles in flight, aggregate with `District count² × route length` — so there is a congestion-cycle length at which they cost the same, and S2 can find it rather than assume it. `adr/0041` has already chosen direct; this is what that choice costs.

#### Direct, against vehicles in flight

| In flight | Crossings/Tick | Attribution/Tick | Per crossing | Standing |
|---:|---:|---:|---:|---|
| 37,000 | 25,596 | 152,032 ns | 5,939 ps | band floor |
| 56,000 | 38,865 | 156,686 ns | 4,031 ps | **the derived Day-average** |
| 111,000 | 76,823 | 393,056 ns | 5,116 ps | band ceiling / 2× peak |
| 170,000 | 117,537 | 541,293 ns | 4,605 ps | 3× peak |

Timed over the **real** crossing distribution — the arcs an advancing fleet actually entered and left, captured and replayed — rather than over drawn indices, because whether the volume column sits in L2 is a property of how scattered those indices are and drawing them would have measured the draw.

#### Aggregate, against District count

| Districts | Pairs in flight | Arc writes | Per cycle | Crossover cycle |
|---:|---:|---:|---:|---:|
| 16 | 256 | 14,626 | 0.58 ms | 3 Ticks |
| 64 | 3,919 | 244,728 | 5.68 ms | 36 Ticks |
| 100 | 9,230 | 584,527 | 10.86 ms | 69 Ticks |
| 121 | 13,181 | 851,358 | 16.52 ms | 105 Ticks |
| 256 | 36,050 | 2,309,725 | 45.12 ms | 288 Ticks |
| 400 | 46,218 | 2,979,576 | 58.97 ms | 376 Ticks |

*Crossover cycle* is the cycle length at which one smear costs what direct attribution costs over the same span, at the derived 56,000 in flight. **Longer than this, aggregate is cheaper; shorter, direct is.** `adr/0041`'s own arithmetic put it near 10 Ticks from an assumed crossing rate; R2.2 measured the rate.

The smear is the **conserving** form — a Traveller on a route of total time `T` contributes `t_s / T` to each Segment, so the shares sum to one and `adr/0041`'s invariant holds. Adding the whole pair count to every Segment would be cheaper per write and would put one vehicle on fifty Segments at once. **A rejected alternative implemented weakly makes the price of rejecting it look smaller than it is.**

#### Where the crossover inverts, across the peaking sweep

`plans/0010`: *"only one side of it moves — direct attribution scales with vehicles in flight and is peak-sensitive; aggregate scales with `zone count² × route length` and is not. **Report the peaking factor at which the crossover inverts.**"* At the anchor's District count:

| Congestion cycle | Aggregate/Tick | Peaking factor that inverts it |
|---:|---:|---:|
| 1 Ticks | 16,252,729 ns | 103.72× |
| 10 Ticks | 1,625,272 ns | 10.37× |
| 25 Ticks | 650,109 ns | 4.14× |
| 50 Ticks | 325,054 ns | 2.07× |
| 100 Ticks | 162,527 ns | 1.03× |
| 200 Ticks | 81,263 ns | 0.51× |

A factor **below 1.00×** means aggregate is already the cheaper scheme at the Day-average load and no peak is needed to invert it. **Above 3.00× means the inversion is out of reach**, because the corpus's own generator mix caps the peak near 3× — 79% of Trips are commutes and school runs, and `02 §1.2`'s sun arc has five phases. The peaking factor itself is still unsized: decision 5a.

### R2b — the lag, and the peak

`03 §3.3` confesses the aggregate scheme's defect in its own text: *"a jam propagates backward at roughly 15 km/h — faster than any cycle worth running — so a cycle-driven region always lags the jam during exactly the event it exists to capture."* That admission is why `03 §3.3` had to invent **force-promotion on downstream blocking** as compensation. This measures the lag it was compensating for.

The jam is a surge: 40% of a 40,000 fleet redirected to the central District — the monocentric morning peak R1 modelled — **replacing** Travellers rather than adding them, so the surge changes where the fleet is going and not how large it is. Lag is Ticks between the watched Segment's **true** `v/c` crossing the threshold and the scheme's own reading crossing it.

| Path source | Cycle | Direct lag | Aggregate lag | Watched peak, direct | Watched peak, aggregate | Peak, direct | Peak, aggregate | Compression |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Searched | 1 | 0 | **never** | 130.21% | 28.09% | 1074.22% | 676.77% | 0.63x |
| Searched | 10 | 0 | **never** | 130.21% | 27.54% | 1074.22% | 676.77% | 0.63x |
| Searched | 25 | 0 | **never** | 130.21% | 27.25% | 1074.22% | 669.22% | 0.62x |
| Searched | 50 | 0 | **never** | 130.21% | 27.18% | 1074.22% | 663.90% | 0.61x |
| Searched | 100 | 0 | **never** | 130.21% | 23.76% | 1074.22% | 660.71% | 0.61x |
| Searched | 200 | 0 | **never** | 130.21% | 23.45% | 1074.22% | 660.71% | 0.61x |
| Shared | 1 | 0 | **never** | 412.33% | 20.14% | 7779.98% | 1660.76% | 0.21x |
| Shared | 10 | 0 | **never** | 412.33% | 19.75% | 7779.98% | 1659.66% | 0.21x |
| Shared | 25 | 0 | **never** | 412.33% | 19.85% | 7779.98% | 1659.66% | 0.21x |
| Shared | 50 | 0 | **never** | 412.33% | 18.67% | 7779.98% | 1659.66% | 0.21x |
| Shared | 100 | 0 | **never** | 412.33% | 18.41% | 7779.98% | 1659.66% | 0.21x |
| Shared | 200 | 0 | **never** | 412.33% | 18.41% | 7779.98% | 1659.66% | 0.21x |
| NextHop | 1 | 0 | **never** | 108.51% | 0.00% | 2669.28% | 1658.57% | 0.62x |
| NextHop | 10 | 0 | **never** | 108.51% | 0.00% | 2669.28% | 1658.57% | 0.62x |
| NextHop | 25 | 0 | **never** | 108.51% | 0.00% | 2669.28% | 1658.57% | 0.62x |
| NextHop | 50 | 0 | **never** | 108.51% | 0.00% | 2669.28% | 1658.57% | 0.62x |
| NextHop | 100 | 0 | **never** | 108.51% | 0.00% | 2669.28% | 1658.57% | 0.62x |
| NextHop | 200 | 0 | **never** | 108.51% | 0.00% | 2669.28% | 1658.57% | 0.62x |

**Direct lag is zero by construction and is printed anyway**, because a column that cannot be anything else is the one worth checking: a non-zero entry would mean the advance loop and the volume column had come apart.

**A column of identical *never*s is the shape of a broken instrument, so the two watched columns are printed to tell the two apart.** They give the highest `v/c` each scheme ever reads on the *same* arc across the window: if aggregate reaches a large number that merely arrived late, the lag is a cadence problem; if it never reaches one, the smear has put the volume **somewhere else** and no cadence recovers it. **The columns say it is the second**, and *never* appears at a one-Tick cycle — where there is no cadence left to blame — which is the same conclusion arrived at from the other side.

That is `adr/0041`'s first argument, measured: *"a Traveller experiences congestion on its own route and deposits congestion on the District pair's route, so the failure feeds a **different** detector, watching different Segments."* The lag was never the whole defect — it is the part that has a number, and the part that does not is worse. It also means **force-promotion loses its remaining bundled justification here** and must stand on `03 §3.3`'s second argument alone, which the board already records as owed.

**Compression is the column `plans/0010` actually asked for** — *"report peak Segment volume under each on the same O-D distribution. A scheme that understates the peak promotes late, and `adr/0007` demotes on a *lower* threshold, so an understated peak also demotes early."* It is aggregate's peak over direct's.

**Read the `v/c` columns comparatively and never as absolute levels.** A Traveller here passes through a Segment regardless of how loaded it is — there is no queue, because `plans/0010` forbids this spike simulating traffic — so `v/c` is unbounded and a monocentric surge drives it far past anything a real Segment reaches. **What is being compared is two readings of one load**, and that comparison is unaffected.

#### Against the threshold, which the corpus does not state

`CONTEXT.md` → Stress gives the mechanism — *"Microscopic above a high threshold and back below a lower one"* — and no numbers, exactly as it gives the Microscopic Cap none. So the threshold is swept and not chosen, at a 50-Tick cycle and the shared path source.

| Threshold | Direct lag | Aggregate lag | Segments over, direct | over, aggregate |
|---:|---:|---:|---:|---:|
| 80.00% | 0 | **never** | 2,592 | 2,714 |
| 100.00% | 0 | **never** | 1,918 | 1,860 |
| 120.00% | 0 | **never** | 1,422 | 1,412 |

*Segments over* is how many the scheme places above the threshold at the end of the window, and it is the column that decides the **Microscopic Cap**'s exposure: under `adr/0007` those are the Segments competing for slots, and a scheme that names a different set names a different city. S2 does not set the Cap — that needs a built traffic model — but this is the first quantitative thing anyone has been able to say about how many Segments would want one.

### What R2 decides, and what it hands on

**R4's condition has moved and R7 must not apply it as written.** `plans/0010` retires DSDV *"if the matrix carries the choice loop and Statistical Trips need no concrete path"*. R1 settled the first clause — it does. The second was written before `adr/0041`, which requires a vehicular Traveller to increment the Segment it **enters**, every Tick. What that needs is a next Segment, not a path; a next-hop table supplies one and stores no path at all. **So the second clause is false, and it is false for a reason that favours distance-vector rather than merely failing to retire it.**

That is an argument, not a measurement, and R2 does not settle R4 with it — R4's own subject is **convergence after an edit**, which nothing here touches. What R2 changes is that R4 is **live**, and R5's edit storm is where it is decided: a next-hop table's attraction is that it needs no per-route invalidation, and its exposure is that *in a city builder link deletion is the core verb*.

**`adr/0041` is owed a correction, small and worth making.** It calls the path source *"a performance axis with no correctness content"*. R2.1's detour columns are correctness content: a Traveller handed a coarse route drives a different Trip — **36.01% longer** under the shared District route and **18.52%** under the next-hop table, at the mean over 300 sampled Trips, with p90s of 71.39% and 40.70% — which under `05 §4`'s test is a different city. The ADR's substantive claim survives intact — experience and contribution stay the same list of Segments under every rung, because a Traveller increments whatever it actually drives — so this amends a sentence and not a decision.

**The crossing rate is now measured and `adr/0041`'s own revisit trigger is the place to record it.** Its cost arithmetic assumes **one** Segment per Tick; R2.2 reports what the graph actually produces at R0's density, which is **0.79 crossings per Traveller per Tick**, identical across all 3 path sources. Every figure in R2a scales linearly on that number, so the assumed rate overstates the bill it is used to compute.

**What R2 does not settle.** The peaking factor is still unsized (decision 5a), so the inversion table is a curve and not a verdict. District count remains R1's open trade, and R2.1 adds a second structure to it — the next-hop table is linear in District count where the route store is quadratic, so the two rungs rank differently at different District counts and neither ranking is a reason to pick one. And nothing here prices **invalidation**, which is R5's.


## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-11 11:27:34 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
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
| 1 | 1.97 ms | 17,505 | 120 ns | 3 |
| 1, reduced | 2.02 ms | 17,505 | 123 ns | 3 |
| 1, reduced + paths | 3.12 ms | 18,592 | 190 ns | 5 |
| 2 | 4.65 ms | 68,766 | 1,136 ns | 8 |
| 2, reduced | 4.86 ms | 68,766 | 1,187 ns | 8 |
| 2, reduced + paths | 6.15 ms | 137,352 | 1,502 ns | 11 |
| 4 | 10.67 ms | 206,142 | 10,424 ns | 19 |
| 4, reduced | 10.59 ms | 206,142 | 10,341 ns | 19 |
| 4, reduced + paths | 16.91 ms | 412,218 | 16,520 ns | 30 |
| 8 | 22.55 ms | 574,207 | 88,088 ns | 41 |
| 8, reduced | 28.92 ms | 574,207 | 112,987 ns | 52 |
| 8, reduced + paths | 48.01 ms | 1,148,375 | 187,570 ns | 87 |
| 16 | 39.07 ms | 2,014,227 | 610,508 ns | 71 |
| 16, reduced | 55.84 ms | 2,014,227 | 872,535 ns | 101 |
| 16, reduced + paths | 83.04 ms | 4,028,411 | 1,297,515 ns | 151 |
| 32 | 80.14 ms | 9,861,622 | 5,009,188 ns | 146 |
| 32, reduced | 75.92 ms | 9,861,622 | 4,745,007 ns | 138 |
| 32, reduced + paths | 188.88 ms | 19,723,213 | 11,805,290 ns | 344 |
| 64 | 167.31 ms | 67,626,483 | 41,829,101 ns | 304 |
| 64, reduced | 187.35 ms | 67,626,483 | 46,837,849 ns | 341 |
| 64, reduced + paths | 364.97 ms | 135,252,966 | 91,242,859 ns | 665 |

One flat `Chebyshev` drive search in this process: **548,637 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **548,637 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 450,188 ns | 1.21× | 471,223 ns | 1.16× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 479,735 ns | 1.14× | 464,787 ns | 1.18× | 4,138 + 4 | 15,961 + 16 | 58 |
| 1, reduced + paths | 476,688 ns | 1.15× | 458,787 ns | 1.19× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 504,102 ns | 1.08× | 548,820 ns | 0.99× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 476,961 ns | 1.15× | 482,214 ns | 1.13× | 4,089 + 12 | 15,780 + 48 | 58 |
| 2, reduced + paths | 506,679 ns | 1.08× | 507,063 ns | 1.08× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 553,105 ns | 0.99× | 654,187 ns | 0.83× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 374,048 ns | 1.46× | 428,632 ns | 1.27× | 2,988 + 38 | 11,442 + 156 | 58 |
| 4, reduced + paths | 360,890 ns | 1.52× | 370,384 ns | 1.48× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 475,368 ns | 1.15× | 487,157 ns | 1.12× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 223,605 ns | 2.45× | 274,414 ns | 1.99× | 1,708 + 131 | 6,351 + 527 | 58 |
| 8, reduced + paths | 222,541 ns | 2.46× | 225,171 ns | 2.43× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 332,131 ns | 1.65× | 389,806 ns | 1.40× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 155,510 ns | 3.52× | 325,136 ns | 1.68× | 886 + 464 | 3,143 + 1,852 | 58 |
| 16, reduced + paths | 185,849 ns | 2.95× | 151,383 ns | 3.62× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 354,121 ns | 1.54× | 521,755 ns | 1.05× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 226,519 ns | 2.42× | 607,686 ns | 0.90× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 32, reduced + paths | 249,154 ns | 2.20× | 207,627 ns | 2.64× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 956,663 ns | 0.57× | 1,106,051 ns | 0.49× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 889,232 ns | 0.61× | 1,492,756 ns | 0.36× | 166 + 8,360 | 544 + 33,095 | 58 |
| 64, reduced + paths | 844,247 ns | 0.64× | 825,599 ns | 0.66× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **468,227 ns**, second **548,637 ns** — a spread of -14.65%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — *one Leg against 530–574 arrivals per Tick, ~400 ms of searching per 15.60 ms of Tick budget* — and that test applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to consume the whole budget on its own — or, put the way that depends on nothing derived, **routing fits only while fewer Trips start per Tick than the break-even column below.**

| Rung | Per route | **Break-even Trips/Tick** | At the working 550 | Fits |
|---|---:|---:|---:|---|
| **flat** | 548,637 ns | **28** | 301.75 ms | 19.34× over |
| 1, reduced + paths | 458,787 ns | **34** | 252.33 ms | 16.17× over |
| 2, reduced + paths | 507,063 ns | **30** | 278.88 ms | 17.87× over |
| 4, reduced + paths | 370,384 ns | **42** | 203.71 ms | 13.05× over |
| 8, reduced + paths | 225,171 ns | **69** | 123.84 ms | 7.93× over |
| 16, reduced + paths | 151,383 ns | **103** | 83.26 ms | 5.33× over |
| 32, reduced + paths | 207,627 ns | **75** | 114.19 ms | 7.32× over |
| 64, reduced + paths | 825,599 ns | **18** | 454.07 ms | 29.10× over |

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
| 1 | 206 | 552 | 2 | 7.75 ms | 46,134 ns | 11.89× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 6.31 ms | 58,336 ns | 9.40× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 11.65 ms | 103,522 ns | 5.29× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 21.64 ms | 185,406 ns | 2.95× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 40.97 ms | 341,409 ns | 1.60× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 44.86 ms | 145,697 ns | 3.76× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted. Only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Rung | Operation | Cost | Clusters touched | Share of cold build | Edits in one build |
|---|---|---:|---:|---:|---:|
| 1 | re-cost | 696 ns | 1.96 | 0.03% | 2,833 |
| 1, reduced | rebuild cluster | 266,585 ns | 1.96 | 13.14% | 7 |
| 1, reduced + paths | rebuild cluster | 282,689 ns | 1.96 | 9.05% | 11 |
| 2 | re-cost | 2,377 ns | 1.37 | 0.05% | 1,958 |
| 2, reduced | rebuild cluster | 190,053 ns | 1.37 | 3.90% | 25 |
| 2, reduced + paths | rebuild cluster | 159,581 ns | 1.37 | 2.59% | 38 |
| 4 | re-cost | 12,550 ns | 1.06 | 0.11% | 850 |
| 4, reduced | rebuild cluster | 55,061 ns | 1.06 | 0.51% | 192 |
| 4, reduced + paths | rebuild cluster | 66,665 ns | 1.06 | 0.39% | 253 |
| 8 | re-cost | 107,908 ns | 1.03 | 0.47% | 208 |
| 8, reduced | rebuild cluster | 148,695 ns | 1.03 | 0.51% | 194 |
| 8, reduced + paths | rebuild cluster | 258,342 ns | 1.03 | 0.53% | 185 |
| 16 | re-cost | 552,963 ns | 1.03 | 1.41% | 70 |
| 16, reduced | rebuild cluster | 764,291 ns | 1.03 | 1.36% | 73 |
| 16, reduced + paths | rebuild cluster | 1,302,220 ns | 1.03 | 1.56% | 63 |
| 32 | re-cost | 4,238,033 ns | 1.03 | 5.28% | 18 |
| 32, reduced | rebuild cluster | 5,211,241 ns | 1.03 | 6.86% | 14 |
| 32, reduced + paths | rebuild cluster | 10,001,024 ns | 1.03 | 5.29% | 18 |
| 64 | re-cost | 44,869,822 ns | 1.00 | 26.81% | 3 |
| 64, reduced | rebuild cluster | 45,509,224 ns | 1.00 | 24.29% | 4 |
| 64, reduced + paths | rebuild cluster | 90,637,630 ns | 1.00 | 24.83% | 4 |

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


## S2 R4 — distance-vector, and the table that has to stay current

- **Captured** 2026-08-11 11:28:51 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
- **Build** Release

**R4 is not a beauty contest between two routers, and reading it as one would miss what R3 handed it.** R3 found that no cluster size fits a per-Trip search into the Tick budget — the best rung breaks even at 85 Trip starts — and named R2's next-hop table as *"the rung this arithmetic does not touch… a structural advantage over both hierarchies rather than a faster constant, and it is R4's to press."* That table asks for **no search at all** when a Trip starts, and 32 ns per Segment crossing thereafter. At the derived 56,000 Travellers in flight and R2's measured 0.79 crossings each per Tick, that is **1.24 ms of a 15.6 ms Tick**. It fits.

**So the question R4 actually answers is what that table costs to maintain.** It is built by 121 backward Dijkstras and R2 measured the build at 474.47 ms — thirty Ticks — while the corpus's own framing of the routing problem is that *in a city builder link deletion is the core verb*. Distance-vector is the answer `plans/0010` names. It is not the only one, and measuring only the named candidate is how a spike produces a verdict it has not earned, so four maintenance schemes are measured against the same edits and a fifth against staleness.

### R4.1 — the origin-destination distribution, which S2 has been guessing since R0

R0 drew origin-destination pairs uniformly and said so, flagging the draw as a placeholder to be replaced by the distribution R1 derived. **R1 derived none, and R2 and R3 inherited the placeholder unchanged.** R3 had to publish every speedup it measured as an upper bound because of it. The debt is now four tasks old and R6 cannot be run against it at all — a cache hit rate measured on a uniform draw is close to meaningless, because what makes a route cache work is that real Trips repeat.

**This does not close the hole; it makes the hole an axis.** Nobody can produce the real distribution until Trips exist, and inventing one and calling it *the* distribution would bake a guess into every downstream figure while making it look like a measurement — which is exactly why R0 drew uniformly, and it was right to. What is available is this plan's own precedent, used for the Microscopic Cap, District count and the peaking factor alike: **report a curve, do not choose a number.**

**Uniform is a rung of the same mechanism rather than a separate code path**, so a difference between two rows is the shape and cannot be the machinery. This spike has twice caught an instrument that could not move and once caught two rungs that were secretly the same rung; a family whose null case is a member of the family is the cheap defence against both.

| Shape | Mean | Median | p90 | Mean route | Draws/pair | Exhausted | Sample |
|---|---:|---:|---:|---:|---:|---:|---:|
| uniform | 8.53 km | 8.38 km | 14.05 km | 71.52 Ticks | 1.00 | 0 | 2000 |
| decay L=1024 | 5.24 km | 4.65 km | 10.13 km | 47.26 Ticks | 5.32 | 0 | 2000 |
| decay L=512 | 3.30 km | 2.79 km | 6.66 km | 35.04 Ticks | 14.97 | 0 | 2000 |
| decay L=256 | 1.83 km | 1.51 km | 3.58 km | 23.22 Ticks | 50.53 | 0 | 2000 |
| monocentric L=512 | 7.22 km | 7.06 km | 11.70 km | 61.72 Ticks | 11.14 | 0 | 2000 |

Distances are straight-line between Segment midpoints, converted at ~4 m per Tile. *Mean route* is the flat A\* cost in Ticks over the first 150 pairs of each rung — the same denominator R0 established, re-measured in this process. *Draws/pair* is the rejection sampler's mean attempt count and *exhausted* is how many draws hit the 65,536-attempt cap; the second column is reported because the five instruments in this spike that earned their place did so on the day they read something other than zero.

### R4.2 — the memory axis, and the tripwire that fires on it

`plans/0010` calls a table per node per destination *"the frightening axis and the one most likely to fail `adr/0006`"*, and writes the wire before the numbers: **DSDV's routing tables exceeding the whole world's footprint puts distance-vector out on memory alone.** The whole world is K0's 172.27 MiB.

| Destinations | Granularity | Entries | Unsequenced | **DSDV** | Against the world |
|---:|---|---:|---:|---:|---:|
| 16 | District, 4 a side | 267,152 | 2.03 MiB | 3.05 MiB | 0.01× |
| 121 | District, 11 a side | 2,020,337 | 15.41 MiB | 23.12 MiB | 0.13× |
| 256 | District, 16 a side | 4,274,432 | 32.61 MiB | 48.91 MiB | 0.28× |
| 400 | District, 20 a side | 6,678,800 | 50.95 MiB | 76.43 MiB | 0.44× |
| 1,024 | District, 32 a side | 17,097,728 | 130.44 MiB | 195.66 MiB | 1.13× |
| 16,697 | **node** | 278,789,809 | 2.07 GiB | 3.11 GiB | 18.51× |

An entry is a cost, a next arc and — for DSDV — a sequence number, at 4 B each. **The sequence numbers are a third of the table**, so the protocol `references.md` insists on costs 50% more than the bare next-hop table R2 measured at 7.70 MiB.

**The wire fires, and it fires on the granularity rather than on the protocol.** At node granularity — the destination set an actual routing table would carry, and the one Citybound's does — the table is three orders of magnitude past the whole world. Sequence numbers do not cause that and removing them does not fix it. **So distance-vector in this design can only ever address Districts**, which is not a footnote about memory: it is what imports R2's 18.52% mean detour and the representative funnel, because a destination the table can afford to name is a District and not a place. The correctness cost is caused by the memory constraint, and the two have been discussed separately everywhere in the corpus.

### R4.3 — cold start, which is not the protocol's claim but is owed a number

| Build | Whole table | Per column | Relaxations | Worst rounds | Entries wrong |
|---|---:|---:|---:|---:|---:|
| backward Dijkstra | 222.46 ms | 1,838,586 ns | — | — | — |
| vector exchange | 111.80 ms | 923,979 ns | 14,333,149 | 175 | 0 |

At the 121-District anchor, over 16,697 nodes. 0 column(s) hit the 4,096-round cap.

**Both arrive at the same table, and the one that was expected to lose does not.** Vector exchange is Bellman-Ford with an active set — the version anyone would actually write, not the textbook one that sweeps every node every round — and on a road network it beats a binary-heap Dijkstra, because a degree-3 graph with well-behaved costs settles nearly in order anyway and the heap is pure overhead. **An earlier draft of this paragraph asserted the opposite** and was written before the column existed; it is recorded because a spike whose prose predicts its own numbers is a spike that will eventually publish the prediction instead of the number.

**What it does not show is that distance-vector is cheap**, and the next three sections are where that is decided. Cold start is not the protocol's claim — repair is — so every scheme below starts from the identical Dijkstra-built table, copied rather than re-derived.

### R4.4 — one deleted Segment, which is the core verb

A Segment is deleted, the whole table is brought back to correct by each scheme in turn, and every scheme is audited against a table rebuilt from scratch on the edited graph. **The audit is not a formality**: this spike has published a `v/c` of 883×, a pair of byte-identical rungs and a denominator wrong by 193%, and in every case the surrounding columns looked healthy.

| Scheme | Per edit | Against rebuild | Relaxations | Rounds / settles | Wrong cost | Stranded |
|---|---:|---:|---:|---:|---:|---:|
| **rebuild** — every column | 218.83 ms | — | — | — | — | — |
| DSDV, sequenced | 482.77 ms | **2.20× slower** | 36,982,307 | 24,137 | 0 (0.00%) | 0 |
| DSDV, unsequenced | 33.77 ms | 6.47× faster | 2,510,526 | 3,047 | 0 (0.00%) | 0 |
| **dynamic repair** — affected subtree | 3.39 ms | 64.41× faster | 201,014 | 17,494 | 0 (0.00%) | 0 |

8 deleted Segments, drawn uniformly, each repaired across all 121 columns — 16,162,696 entries audited per scheme. Columns that hit the 4,096-round cap: sequenced 0, unsequenced 0.

The rebuild denominator read 223,033,508 ns on the first edit and 226,249,745 ns on the last, both published because R3 found the same quantity moving 193% between its first and last measurement in one process. **It also disagrees with R2's published build of 474.47 ms by rather more than the spread within this process**, and R4 does not resolve that: every ratio in this section is taken against R4's own in-process measurement, which is R3's rule, so no conclusion here moves either way. The discrepancy is owed to R7.

### R4.5 — a severed destination, and the failure sequence numbers exist for

`references.md` is categorical — *"if we adopt distance-vector routing, we take DSDV's version, not Citybound's"* — because Citybound's entries carry no sequence numbers and link deletion count-to-infinities. **Under `adr/0043` that is a measurable claim and it has never been measured**: it names a failure, a trigger and a mechanism, so it has a refuting number, and R4 is the machine.

Every arc into and out of one District's representative is deleted, which is a bulldozed cul-de-sac and makes that destination unreachable from everywhere. Only that District's column is repaired, because that is the column the severance breaks.

| Scheme | Rounds | Relaxations | Converged | Wrong cost | No route |
|---|---:|---:|---|---:|---:|
| DSDV, sequenced | 121 | 133,258 | yes | 0 | 0 |
| DSDV, unsequenced | 4,096 | 215,894,753 | **no**, hit the 4,096-round cap | 16,684 | 0 |
| **dynamic repair** | 0 settles | 130,114 | yes | 0 | 0 |

8 arcs deleted, 16,697 entries audited per scheme. *Wrong cost* counts entries whose cost differs from a table rebuilt on the severed graph; *no route* counts entries whose next hops do not reach the destination in `nodes` steps, which by the pigeonhole principle means a cycle. **The second column is the `adr/0006`-class defect `adr/0041` names in words** — a Traveller that never arrives, and *"a road that looks busy forever."*

### R4.6 — congestion drift, which nothing in the corpus invalidates on

**R3 left this as the largest unpriced exposure in the spike and it belongs here.** The Epoch bumps on an *edit*; the VDF makes travel time a function of `volume / capacity`; `adr/0041` moves volume **every Tick**. So every precomputed structure in S2 is stale the Tick after it is built — HPA\*'s intra-cluster edges, R2's next-hop table and R1's matrix alike — and a flat search reads arc costs at query time and is always current. That is a structural advantage of the denominator nobody has priced. **A deleted road is the core verb; a changed travel time is every Tick.**

The sweep is the fraction of arcs whose cost moved between one refresh and the next. Nothing in the corpus says what that fraction is — it depends on the refresh cadence, which is `plans/0010` decision 2 and still open — so it is swept and the **break-even fraction** is what is published. That is the invertible form R3's rule requires: a measured cost over a swept axis, containing no guess, still true when somebody finally measures the cadence.

| Arcs moved | Rebuild | DSDV, sequenced | Dynamic repair | Repair vs rebuild | Wrong cost |
|---:|---:|---:|---:|---:|---:|
| 0.10% (57) | 224.41 ms | 394.20 ms | 41.40 ms | 5.41× | 0.00% |
| 1.00% (635) | 217.63 ms | 393.53 ms | 137.28 ms | 1.58× | 0.00% |
| 10.00% (6,474) | 236.30 ms | 422.17 ms | 349.71 ms | 0.67× | 0.00% |
| 100.00% (64,138) | 197.93 ms | 4700.66 ms | 341.61 ms | 0.57× | 0.00% |

At the 121-District anchor. Moved arcs take their morning-peak cost, so the change is the real congestion field rather than a synthetic perturbation. *Wrong cost* is the dynamic-repair rung audited against the rebuild.

### R4.7 — the rolling refresh, which needs none of this machinery

**The scheme a person would write if nobody had said the words *distance vector*.** Rebuild *k* columns every Tick, forever, in rotation. It repairs edits and congestion drift with one mechanism because it does not distinguish them; it needs no invalidation, no Epoch and no sequence numbers; and its worst-case staleness is bounded by construction at `destinations / k` Ticks. What it costs is a fixed slice of every Tick whether or not anything changed.

| Columns per Tick | Cost per Tick | Share of 15.6 ms | Worst staleness |
|---:|---:|---:|---:|
| 1 | 1,663,839 ns | 10.66% | 121 Ticks |
| 2 | 3,327,678 ns | 21.32% | 61 Ticks |
| 4 | 6,655,356 ns | 42.66% | 31 Ticks |
| 8 | 13,310,712 ns | 85.32% | 16 Ticks |
| 121 | 201,324,519 ns | 1290.53% | 1 Ticks |

One column costs 1,663,839 ns at the 121-District anchor. **Staleness is in Ticks and a Tick is ~10.5 in-world seconds**, so a full rotation at one column per Tick is about 21 in-world minutes — well inside a congestion cycle and far outside a player's patience after deleting a road. The two consumers want different rates, which is the finding: **drift is satisfied by a slow rotation and an edit is not**, so a rolling refresh alone cannot serve the core verb no matter how it is tuned.

### R4.8 — what the table's routes cost, on a distribution that is not uniform

R2 measured a next-hop table's mean detour at **18.52%** against a shared District route's 36.01% — on the uniform draw R4.1 has now replaced. A detour caused by aiming at a District representative instead of at a destination is **a fixed error in Ticks against a shrinking journey**, so it should worsen as trips get shorter, and the uniform draw is the longest-trip rung there is. This is the recalibration.

**Measured at the morning peak, on the same synthetic field R1 and R2 used**, because R2's figure was and a detour compared against a free-flow one would be comparing two graphs. The tail search starts at a Segment incident to the representative rather than at the node itself, which can only make the followed route look cheaper, so **every figure below is a lower bound** — R2's, measured at node granularity, was an upper one, and the two bracket rather than contradict.

| Shape | Mean detour | p90 | Worst | Sample |
|---|---:|---:|---:|---:|
| uniform | 20.14% | 45.65% | 815.18% | 197 |
| decay L=1024 | 36.04% | 69.24% | 1347.93% | 197 |
| decay L=512 | 62.02% | 154.58% | 1835.31% | 197 |
| decay L=256 | 128.82% | 241.79% | 5165.15% | 197 |
| monocentric L=512 | 24.97% | 51.59% | 791.76% | 196 |

Sample size is reported per rung, per the board's owed finding — R1 published a row built from nine searches beside rows built from 2,244, because its sample shrank with the swept axis. Here a pair is dropped only if either search fails, so the column is a survivorship check as well as a sample size.

### What R4 decided, and what it did not

**Decided.**

- **Distance-vector is out, and on none of the three grounds anybody expected.** Not memory: at District granularity the table is 23.12 MiB against a 172.27 MiB world, and `plans/0010`'s wire does not fire. Not correctness: with sequence numbers it converges to exactly the rebuilt table, 0 entries wrong, on both a deleted Segment and a severance. **It is out because it costs more than the rebuild it exists to avoid** — 482.77 ms against 218.83 ms for one deleted Segment, **2.20× slower** — and 142.11× more than the scheme this plan never named.

  The reason is structural rather than a constant, which is why no tuning recovers it. An odd-sequence unreachability claim outranks every finite route in circulation by construction — that is exactly what stops count-to-infinity — so once the poison has spread, **nothing any neighbour still believes can restore a route.** Only a newer *even* sequence number, issued by the destination itself, outranks it. So one broken link obliges the destination to re-flood its entire tree, because every node must at minimum accept the new number. **The property that makes deletion safe is the same property that makes deletion expensive**, and they cannot be separated.

- **`references.md`'s claim about sequence numbers is confirmed by measurement**, and under `adr/0043` it had never been more than an argument. On a severed destination the unsequenced version does **215,894,753 relaxations against 133,258** — 1620.12× the work — fails to converge within 4,096 rounds, and leaves **16,684 of 16,697 entries wrong**. The sequenced version converges in 121 rounds with nothing wrong. *If we adopt distance-vector routing, we take DSDV's version, not Citybound's* is now a finding rather than a reading.

- **The scheme that wins was not on the ballot.** Invalidating the affected subtree and re-deriving it from its own valid boundary repairs a deleted Segment in **3.39 ms against a 218.83 ms rebuild — 64.41× — with 0 entries wrong**, and converges on a severance too. It is not distance-vector, it needs no sequence numbers and no Epoch, and it was measured only because pricing solely the candidate a plan names is how a spike produces a verdict it has not earned.

- **A rebuild is not the fallback anybody feared.** It is 218.83 ms for the whole 121-column table — 1.80 ms per column — which a rolling refresh spends at **11.59% of one Tick** for a full rotation every 121 Ticks. That is affordable. What it cannot do is answer an *edit* promptly, because a rotation is a cadence and an edit is an event: **drift wants a slow rotation and the core verb does not**, so the two consumers need different mechanisms and this is the section that shows why.

**Not decided, and owed.**

- **The next-hop table's error is far worse than R2 measured, and R4 does not know what to do about it.** R2's **18.52%** mean detour was taken on the uniform draw, which R4.1 shows is the longest-trip distribution available — 8.53 km mean on a 16.4 km map, and R4.8 reads 20.14% there. Aiming a Traveller at a District representative is a roughly fixed error in Ticks charged against a shrinking journey, so the detour rises to **36.04%** at a 5.24 km mean, **62.02%** at a 3.30 km mean, **128.82%** at a 1.83 km mean and **24.97%** at a 7.22 km mean. **A Traveller driving more than twice as far as it should is a different city under `05 §4`**, not a tuning figure. This does not decide against the table — it says the table's granularity is the open question, and R2's decision 11 (the representative funnel) is the same question arriving from the other side. **The two should be answered once.**

- **The congestion-drift break-even sits between 1% and 10% of arcs moved**, and nothing in the corpus says which side the design lands on, because the refresh cadence is `plans/0010` decision 2 and still open. Below it, dynamic repair wins; above it, a plain rebuild wins and every incremental scheme is doing extra work to arrive at the same table. **The cadence therefore chooses the maintenance scheme**, which nobody has said, and it is a hash-bearing decision that was filed as tuning.

- **The O-D distribution is an axis now and still not a measurement.** R4.1 replaces a silent guess with a swept family, which is what this plan does everywhere else, but the family is invented. What would replace it is Trip generation, and that does not exist. **Every figure in R4.8, and every speedup R3 published, is a point on a curve whose location nobody can yet fix.**

- **R4's rebuild denominator disagrees with R2's published build** — 218.83 ms against 474.47 ms for the same 121 backward Dijkstras, a factor of 2.16×. Every ratio here is taken in-process against R4's own measurement, per R3's rule, so no conclusion moves; but two S2 tasks now publish different absolutes for the same operation and R7 owes the reconciliation.

**Four defects in R4's own harness, all caught by instruments rather than by reading.**

- **The sequenced protocol was missing DSDV's acceptance rule** — a node must *reject* an advertisement older than what it already holds, not merely prefer newer ones. Without it a poisoned node kept its odd sequence number while adopting a neighbour's stale finite cost, then advertised that stale cost under the high sequence its own poison had earned. **The first capture reported 232 seconds per edit and would have published *distance-vector loses by three orders of magnitude*.** With the rule, the same measurement is 482.77 ms. What flagged it was R2's own recorded lesson: the sequenced and unsequenced rungs were reporting near-identical relaxation counts and *identical* wrong-entry counts, and **two measurements that agree that closely are not two measurements.**

- **The poison phase was a silent no-op.** It seeded the flood with the nodes that detected the break, which correctly reject every stale claim and therefore never change and never notify anybody. In DSDV the detector *advertises*; the advertisement is the event, not something a node discovers by looking around. The phase converged in 2 rounds and 24 relaxations while leaving 16,680 of 16,697 entries wrong — **and the report read "converged: yes", because a phase that does nothing does it very quickly.**

- **The audit counted the destination itself as stranded**, one phantom per column, which read as a suspiciously round 121. A defect that produces a plausible number is worse than one that produces an absurd one.

- **The elapsed-time helper overflowed.** `elapsed × 1,000,000,000` passes `long.MaxValue` at about 9.2 seconds on a nanosecond clock, and the first capture published **−8,267.51 ms** for the rung that then took four minutes. Every earlier S2 section times loops far below that threshold, so the same expression has been correct everywhere else in this harness — **a helper is only as safe as the largest quantity anybody has yet asked it to measure.**


## S2 R6.1 — the cache key's granularity

- **Captured** 2026-08-11 11:29:12 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
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


## S2 R6.2 — the eviction policy, and who is to blame for a miss

- **Captured** 2026-08-11 11:29:38 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
- **Build** Release

**No eviction policy is stated anywhere in the corpus.** `adr/0012` permits caching and says nothing about what leaves; `adr/0017` shows the pattern — fixed capacity, least-used eviction — and nobody has written it down for routes. `RouteCache` implements neither: it is **direct-mapped with one slot**, and an insert whose slot is taken simply overwrites.

**This section reports blame rather than a rate**, because a hit percentage cannot tell a cache that is too small from one throwing away entries it still holds, and those want opposite repairs. *Cold* is a first reference and is unavoidable. *Capacity* is a miss a perfect cache of the same size would also have taken. **Conflict is a miss a perfect cache of the same size would have avoided, and it is the only column that is a defect.** 1,024 entries, 16,384 Trips drawn with repetition.

| Pool | Load | Scheme | Hit | Cold | Capacity | **Conflict** | Mean probes |
|---|---|---|---:|---:|---:|---:|---:|
| 256 | 0.25× | `direct`, modulo — **shipped** | 87.6% | 1.5% | 0.0% | **10.7%** | 1.00 |
| 256 | 0.25× | `direct`, high bits | 89.6% | 1.5% | 0.0% | **8.8%** | 1.00 |
| 256 | 0.25× | 2-way LRU | 94.9% | 1.5% | 0.0% | **3.4%** | 2.00 |
| 256 | 0.25× | 4-way LRU | 97.3% | 1.5% | 0.0% | **1.0%** | 4.00 |
| 256 | 0.25× | 8-way LRU | 98.4% | 1.5% | 0.0% | **0.0%** | 8.00 |
| 256 | 0.25× | fully associative — *bound* | 98.4% | 1.5% | 0.0% | **0.0%** | 1024.00 |
| 512 | 0.50× | `direct`, modulo — **shipped** | 76.7% | 3.1% | 0.0% | **20.0%** | 1.00 |
| 512 | 0.50× | `direct`, high bits | 75.6% | 3.1% | 0.0% | **21.1%** | 1.00 |
| 512 | 0.50× | 2-way LRU | 86.1% | 3.1% | 0.0% | **10.6%** | 2.00 |
| 512 | 0.50× | 4-way LRU | 92.9% | 3.1% | 0.0% | **3.8%** | 4.00 |
| 512 | 0.50× | 8-way LRU | 95.4% | 3.1% | 0.0% | **1.4%** | 8.00 |
| 512 | 0.50× | fully associative — *bound* | 96.8% | 3.1% | 0.0% | **0.0%** | 1024.00 |
| 1,024 | 1.00× | `direct`, modulo — **shipped** | 61.4% | 6.2% | 0.0% | **32.2%** | 1.00 |
| 1,024 | 1.00× | `direct`, high bits | 61.0% | 6.2% | 0.0% | **32.6%** | 1.00 |
| 1,024 | 1.00× | 2-way LRU | 69.0% | 6.2% | 0.0% | **24.7%** | 2.00 |
| 1,024 | 1.00× | 4-way LRU | 77.4% | 6.2% | 0.0% | **16.2%** | 4.00 |
| 1,024 | 1.00× | 8-way LRU | 82.5% | 6.2% | 0.0% | **11.2%** | 8.00 |
| 1,024 | 1.00× | fully associative — *bound* | 93.7% | 6.2% | 0.0% | **0.0%** | 1024.00 |
| 2,048 | 2.00× | `direct`, modulo — **shipped** | 40.6% | 12.5% | 29.8% | **16.9%** | 1.00 |
| 2,048 | 2.00× | `direct`, high bits | 40.2% | 12.5% | 30.0% | **17.1%** | 1.00 |
| 2,048 | 2.00× | 2-way LRU | 44.3% | 12.5% | 30.7% | **12.3%** | 2.00 |
| 2,048 | 2.00× | 4-way LRU | 47.0% | 12.5% | 31.7% | **8.7%** | 4.00 |
| 2,048 | 2.00× | 8-way LRU | 47.6% | 12.5% | 33.8% | **6.0%** | 8.00 |
| 2,048 | 2.00× | fully associative — *bound* | 47.9% | 12.5% | 39.5% | **0.0%** | 1024.00 |
| 512, 8 sites | 0.50× | `direct`, modulo — **shipped** | 65.6% | 3.1% | 0.0% | **31.2%** | 1.00 |
| 512, 8 sites | 0.50× | `direct`, high bits | 75.1% | 3.1% | 0.0% | **21.7%** | 1.00 |
| 512, 8 sites | 0.50× | 2-way LRU | 87.0% | 3.1% | 0.0% | **9.8%** | 2.00 |
| 512, 8 sites | 0.50× | 4-way LRU | 92.7% | 3.1% | 0.0% | **4.1%** | 4.00 |
| 512, 8 sites | 0.50× | 8-way LRU | 96.0% | 3.1% | 0.0% | **0.8%** | 8.00 |
| 512, 8 sites | 0.50× | fully associative — *bound* | 96.8% | 3.1% | 0.0% | **0.0%** | 1024.00 |

**At R5.3's own rung — 512 pairs into 1,024 entries — the shipped scheme's misses are 20.0% conflict and 0.0% capacity.** Every one of them is a lookup a perfect cache **of the same size** would have served. **R5.3's 28–31% floor is not a property of cache size and never was**, and reading it as one is what made it look like a fact of life rather than a bug with a fix.

**Associativity is the lever, and four ways is where it stops paying.** At the same rung conflict falls 20.0% → 10.6% → 3.8% → 1.4% across 1, 2, 4 and 8 ways, against a fully-associative bound of 0.0%. **Four ways recovers most of the gap at four probes**, and the probes are contiguous — on the cache line an entry already occupies, close to free. This is `adr/0017`'s fixed-capacity least-used pattern, sized, and it is the first number the corpus has for it.

**The index function is not the lever, and this section predicted that it would be.** The hypothesis on file was that `RouteCache.Slot` multiplies by the golden-ratio constant — driving entropy upward — and then takes `% capacity`, which reads the low bits, so the modulo discards exactly what the multiply created. **Measured, that is wrong on random keys**: high-bit indexing reads 21.1% against modulo's 20.0% at 0.50× load and 32.6% against 32.2% at 1.00× — **level or slightly worse.** A route key is already a pair of well-spread node ids, so there is no structure in the low bits for the modulo to expose.

**Where it does help is exactly where R6.1b found the damage: structured keys.** On the eight-destination pool, high-bit indexing takes conflict 31.2% → 21.7%, and four ways takes it to 4.1%. **So the index function is a robustness fix rather than a throughput one** — it costs nothing and it is what stops a concentrated city falling off a cliff the uniform draw never shows. **Both changes are worth making and only one of them shows up in the average case**, which is the argument for measuring the concentrated rung at all.

**One honest limit on that row.** R6.1b's worst case — 15.9% hit — was the `access-point` key, and this table keys on `nearest-node` throughout, where the same pool reads 65.6%. The conflict column is clearly elevated against the unconcentrated rung (31.2% against 20.0% at identical load), so the mechanism is confirmed. **The magnitude is not**, and this table does not reproduce R6.1b's extreme.

**Load is the axis R5.3 never swept, and it dominates everything above.** Conflict at four ways runs 1.0% → 3.8% → 16.2% across 0.25×, 0.50× and 1.00×, and **capacity misses appear only at 2.00×**, where they reach 29.8%. R5.3 measured one load and called the result a floor; **it is a point on a curve that triples.**

**What this section does not depend on.** Every figure is a *conditional* claim — given a lookup that repetition would have made a hit, whose fault is it that it was not? — so it survives the cache's absolute hit rate being unmeasurable until Trip generation exists. **The cold column is the part hostage to the invented pool** and it is reported separately rather than folded in, so a reader can see which half of the table moves with the invention. The conflict column does not.


## S2 R6.3 — routing's Tick bill, with both consumers in it

- **Captured** 2026-08-11 11:29:39 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
- **Build** Release

**Every budget figure in this corpus counts Trip starts.** R3's tripwire counts them and `plans/0013`'s routing row counts them. But `adr/0046` introduced **Sight**, R8 measured **1,269.51 diversions per Tick at 40,000 Travellers**, and `adr/0047` then deleted the next-hop table — the one path source that served a diversion cheaply. **Nobody has multiplied those together.** This section does.

### The cost basis, measured in this process

| Event | Cost | Mean arcs |
|---|---:|---:|
| Whole-journey search — a **Habit formation** | 488.21 µs | 58 |
| Remainder search from mid-journey — a **diversion** | 109.93 µs | 28 |
| Cache hit — lookup and compare | 21 ns | — |
| *the same whole-journey loop, measured last* | *473.72 µs* | — |

**The denominator is taken twice**, first and last, because R3 found that a denominator measured first carries a systematic error and instructed R4, R5 and R6 to do the same. The two readings are 488.21 µs and 473.72 µs; **every ratio below uses the last.**

**The diversion search is shorter than a whole journey and that is the point of pricing it separately** — a Traveller diverting halfway has half a journey left. R8.6 captured its sites from a live fleet; this takes the midpoint node of a drawn journey, which is the same shape and is stated rather than passed off as the same measurement.

### The bill

Diversions are applied at R8.3's measured intensity — **1,269.51 per Tick at 40,000 Travellers**, carried as a per-Traveller rate. **The in-flight rungs are S0a's own band for a 1,000,000-population city** — 37,000 / 56,000 / 111,000, swept because the mean Trip duration behind them is unmeasured — plus R8.3's 40,000. **None of them is a small city.** Habit formations are `Travellers / (lifetime × 8,192 Ticks)`. The budget is **15.600 ms**, itself unratified — `plans/0013`.

| In flight | Habit lifetime | Formations/Tick | Diversions/Tick | Formation bill | Diversion bill | **Total, of budget** |
|---:|---:|---:|---:|---:|---:|---:|
| 37,000 | 1 d | 4.516 | 1,174 | 2.139 ms | 129.060 ms | **841.02%** |
| 37,000 | 7 d | 0.645 | 1,174 | 0.305 ms | 129.060 ms | **829.26%** |
| 37,000 | 30 d | 0.150 | 1,174 | 0.071 ms | 129.060 ms | **827.76%** |
| 40,000 | 1 d | 4.882 | 1,269 | 2.312 ms | 139.503 ms | **909.07%** |
| 40,000 | 7 d | 0.697 | 1,269 | 0.330 ms | 139.503 ms | **896.37%** |
| 40,000 | 30 d | 0.162 | 1,269 | 0.076 ms | 139.503 ms | **894.74%** |
| 56,000 | 1 d | 6.835 | 1,777 | 3.237 ms | 195.349 ms | **1272.99%** |
| 56,000 | 7 d | 0.976 | 1,777 | 0.462 ms | 195.349 ms | **1255.20%** |
| 56,000 | 30 d | 0.227 | 1,777 | 0.107 ms | 195.349 ms | **1252.92%** |
| 111,000 | 1 d | 13.549 | 3,522 | 6.418 ms | 387.180 ms | **2523.07%** |
| 111,000 | 7 d | 1.935 | 3,522 | 0.916 ms | 387.180 ms | **2487.80%** |
| 111,000 | 30 d | 0.451 | 3,522 | 0.213 ms | 387.180 ms | **2483.29%** |

**The two consumers are not the same order of magnitude and nothing in the corpus says so.** At R8's own rung — 40,000 Travellers, a 7-Day Habit — formations cost 0.330 ms and diversions cost 139.503 ms, which is **422.49× the formation bill and 99.76% of routing's total**. Every budget figure the corpus publishes counts only the other one.

**Habit is doing exactly what `adr/0046` claims.** A Citizen that computes a route once and keeps it for a week costs well under one search per Tick across a whole fleet — R3's *85 Trip starts* is not the binding constraint and never was, because a Trip start under static Habit is a **lookup**. The constraint is the thing the same ADR introduced in its next paragraph.

### The feasible region, inverted

Published R3's way — a threshold on a quantity, not a multiple over a guess — because the diversion rate is the number most likely to move and the least likely to be measured soon.

| Policy | Cost per diversion | Diversions/Tick that fit | R8's rate is |
|---|---:|---:|---|
| **re-search** — what the corpus specifies today | 109.93 µs | 141 | **over**, by 9.00× |
| *the same, on R8.6's own diversion cost* | *485.50 µs* | *32* | *over, by 39.65×* |
| cache-served | **no hit rate exists to price this** | — | *see below* |
| **rejoin** the Habit Route, no search — *unproposed* | — | unbounded | **free — no search at all** |

**The two ends of the basis disagree by 4.41× and the conclusion survives both.** This section's remainder search is the optimistic end — a midpoint site on a graph nobody is bulldozing. R8.6's 485.50 µs is the pessimistic end, taken on sites a live fleet actually diverted at, and it is close to a *whole-journey* cost rather than half of one, which is itself worth someone's attention. **Between 32 and 141 diversions per Tick fit. R8 measured 1,269.**

**`adr/0046` and `adr/0047` are not jointly affordable under the policy the corpus currently specifies.** Sight makes diversion routine; `adr/0047` removed the only thing that served one cheaply; and a re-search per diversion is over the whole Tick budget at R8's own measured rate, under the optimistic basis, **at every rung of the in-flight band S0a derived for the target city** — not at some extrapolated future size. The band's own midpoint is 56,000.

#### What the cache would have to do, since nobody may quote what it does

**R6.2 says in terms that no document may cite a hit rate from it** — it reports *blame* precisely because the absolute rate rests on Trip repetition that needs `06` milestone 5b. So this row is not priced. It is **inverted**: a cached diversion costs `hit + miss-rate × search`, and the question is what miss rate makes it fit.

| In flight | Diversions/Tick | Required hit rate, optimistic basis | Required hit rate, R8.6's basis |
|---:|---:|---:|---:|
| 37,000 | 1,174 | **88.0%** | **97.3%** |
| 40,000 | 1,269 | **88.9%** | **97.5%** |
| 56,000 | 1,777 | **92.1%** | **98.2%** |
| 111,000 | 3,522 | **96.0%** | **99.1%** |

**And R6.1b is the reason those are not attainable.** A diversion is keyed on *(wherever I now am → destination)*, and a mid-journey position is an arbitrary point along a route rather than a Building. R6.1b established that a coarse key collapses **nothing** unless trips coincide at *both* ends — and diversion origins coincide far less than trip origins do, because they are wherever congestion happened to be. **The cache is being asked for its best case on its worst input.**

**Three levers, and two of them are Ruleset numbers that are already unset.** Raise the **Temperament** threshold so fewer drivers act on what Sight shows them; shrink the **Sight Horizon** toward its 1-Segment floor; or take the last row — **let a diversion rejoin the Habit Route without re-searching**, which is what a driver with a map in their head actually does, and which nothing in the corpus proposes. The third is free by construction and is a design question rather than a tuning one.

**What this section does not do is pick one.** `05 §4` says a different route is a different city, and all three change which route a Traveller takes — so this is session **M**'s to answer and not a benchmark's. What R6.3 supplies is that **it must be answered**, which the corpus did not previously know, because the two facts sat in different documents and were never multiplied.


## S2 R5.6 — the Parking Shed, and the rung it disagrees with

- **Captured** 2026-08-11 11:29:40 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
- **Build** Release

**The second Epoch consumer, and `plans/0010` calls it the one the ladder is most likely to be decided by.** It scales with **Buildings** rather than with routes, and a shed is a *neighbourhood* rather than a *path* — so what *"my Segments"* even means is a choice, which is why there are **four rungs here where routes had three**. **R6.2 recommended 4-way LRU on routes alone**, which is the exact thing `plans/0010` warned against.

### R5.6a — what a shed actually is

| Walk radius | Sheds | Build | Bins found | Ball Segments | Path Segments | Empty |
|---:|---:|---:|---:|---:|---:|---:|
| 200 m | 159,825 | 1.17 µs | 22 | 4 | 1 | 0 |
| 400 m | 159,825 | 851 ns | 110 | 22 | 2 | 0 |
| 800 m | 159,825 | 5.30 µs | 596 | 122 | 2 | 0 |

**A shed is not a path, and the two witness columns are the whole argument.** At 400 m a shed's walk ball explores **22 Segments** while the walks to the Bins it keeps touch **2**. A route's witness is the arcs it drives and it stores them anyway; **a shed's conservative witness is 11.00× its own answer**, and it is a structure the shed has no other reason to carry.

**Rebuilding every shed in the city costs 136.011 ms** at 851 ns each. That is the figure every row below is denominated in, and it is why the global rung is a question about the Tick budget rather than about cache hygiene.

### R5.6b — the storm, and the stampede

At **400 m** — 159,825 sheds, 851 ns to rebuild one. Gestures are R5's own, so the two sections compare directly. Each row is the mean over 24 gestures.

| Gesture | Asked | Got | Rung | Sheds invalidated | Share | Rebuild at the edit | Of a Tick |
|---|---:|---:|---|---:|---:|---:|---:|
| drag | 1 | 1 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| drag | 1 | 1 | per-cluster (8) | 1,273 | 0.7% | 1.083 ms | **6.94%** |
| drag | 1 | 1 | per-cluster (16) | 3,660 | 2.2% | 3.114 ms | **19.96%** |
| drag | 1 | 1 | per-Segment (ball) | 110 | 0.0% | 0.093 ms | **0.60%** |
| drag | 1 | 1 | per-Segment (paths) | 10 | 0.0% | 0.008 ms | **0.05%** |
| drag | 16 | 16 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| drag | 16 | 16 | per-cluster (8) | 2,804 | 1.7% | 2.386 ms | **15.29%** |
| drag | 16 | 16 | per-cluster (16) | 6,662 | 4.1% | 5.669 ms | **36.34%** |
| drag | 16 | 16 | per-Segment (ball) | 590 | 0.3% | 0.502 ms | **3.21%** |
| drag | 16 | 16 | per-Segment (paths) | 119 | 0.0% | 0.101 ms | **0.64%** |
| drag | 256 | 199 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| drag | 256 | 199 | per-cluster (8) | 16,047 | 10.0% | 13.655 ms | **87.53%** |
| drag | 256 | 199 | per-cluster (16) | 25,776 | 16.1% | 21.935 ms | **140.61%** |
| drag | 256 | 199 | per-Segment (ball) | 5,072 | 3.1% | 4.316 ms | **27.66%** |
| drag | 256 | 199 | per-Segment (paths) | 1,459 | 0.9% | 1.241 ms | **7.95%** |
| scattered | 1 | 1 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| scattered | 1 | 1 | per-cluster (8) | 1,273 | 0.7% | 1.083 ms | **6.94%** |
| scattered | 1 | 1 | per-cluster (16) | 3,660 | 2.2% | 3.114 ms | **19.96%** |
| scattered | 1 | 1 | per-Segment (ball) | 110 | 0.0% | 0.093 ms | **0.60%** |
| scattered | 1 | 1 | per-Segment (paths) | 10 | 0.0% | 0.008 ms | **0.05%** |
| scattered | 16 | 16 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| scattered | 16 | 16 | per-cluster (8) | 18,693 | 11.6% | 15.907 ms | **101.97%** |
| scattered | 16 | 16 | per-cluster (16) | 47,033 | 29.4% | 40.025 ms | **256.57%** |
| scattered | 16 | 16 | per-Segment (ball) | 1,768 | 1.1% | 1.504 ms | **9.64%** |
| scattered | 16 | 16 | per-Segment (paths) | 169 | 0.1% | 0.143 ms | **0.92%** |
| scattered | 256 | 256 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| scattered | 256 | 256 | per-cluster (8) | 131,829 | 82.4% | 112.186 ms | **719.14%** |
| scattered | 256 | 256 | per-cluster (16) | 158,025 | 98.8% | 134.479 ms | **862.04%** |
| scattered | 256 | 256 | per-Segment (ball) | 25,898 | 16.2% | 22.039 ms | **141.27%** |
| scattered | 256 | 256 | per-Segment (paths) | 2,547 | 1.5% | 2.167 ms | **13.89%** |
| arterial | 1 | 1 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| arterial | 1 | 1 | per-cluster (8) | 1,797 | 1.1% | 1.529 ms | **9.80%** |
| arterial | 1 | 1 | per-cluster (16) | 5,191 | 3.2% | 4.417 ms | **28.31%** |
| arterial | 1 | 1 | per-Segment (ball) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 1 | 1 | per-Segment (paths) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 16 | 3 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| arterial | 16 | 3 | per-cluster (8) | 3,669 | 2.2% | 3.122 ms | **20.01%** |
| arterial | 16 | 3 | per-cluster (16) | 9,853 | 6.1% | 8.384 ms | **53.74%** |
| arterial | 16 | 3 | per-Segment (ball) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 16 | 3 | per-Segment (paths) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 256 | 3 | global | 159,825 | 100.0% | 136.011 ms | **871.86%** |
| arterial | 256 | 3 | per-cluster (8) | 3,669 | 2.2% | 3.122 ms | **20.01%** |
| arterial | 256 | 3 | per-cluster (16) | 9,853 | 6.1% | 8.384 ms | **53.74%** |
| arterial | 256 | 3 | per-Segment (ball) | 0 | 0.0% | 0.000 ms | **0.00%** |
| arterial | 256 | 3 | per-Segment (paths) | 0 | 0.0% | 0.000 ms | **0.00%** |

### What it costs to be able to ask

**A rung is not free merely because it invalidates less.** Checking *did any of my Segments move* needs a reverse index the shed would not otherwise hold, and that is the column routes never had to pay.

| Rung | Reverse index | Resident |
|---|---:|---:|
| per-cluster (8) | 301,599 entries | 1.15 MiB |
| per-cluster (16) | 222,536 entries | 0.84 MiB |
| per-Segment (paths) | 319,655 entries | 1.21 MiB |
| per-Segment (ball) | 3,608,241 entries | 13.76 MiB |

**The global rung is out, and the tripwire fired as written.** One deleted Segment anywhere invalidates all 159,825 sheds, and rebuilding them costs **136.011 ms — 871.86% of a Tick.** `plans/0010` predicted it in words before this harness existed. **The number is worse than the sentence**, because the rebuild is paid *on arrival* — the moment a Trip is trying to finish — so it is not one stall but a stampede spread across every arriving vehicle. **`05 §3`'s *invalidated by the Road Graph Epoch* is owed the correction `CONTEXT.md` → Epoch already took**: the phrase says when the rebuild is paid, not how much survives, and under one counter the answer is none of it.


## S2 R6.4 — what a per-Citizen Habit Route costs

- **Captured** 2026-08-11 11:29:42 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
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
| uniform | 1 | 512 | 75 (14.64%) | 219 ns | 465 ns | **684 ns** | 448 ns | 569 ns | 1.70 µs | 0.867 ms | 5.56% |
| uniform | 2 | 512 | 98 (19.14%) | 219 ns | 1.07 µs | **1.29 µs** | 1.12 µs | 1.34 µs | 6.11 µs | 1.639 ms | 10.50% |
| uniform | 3 | 512 | 439 (85.74%) | 219 ns | 2.09 µs | **2.31 µs** | 2.39 µs | 2.71 µs | 6.15 µs | 2.940 ms | 18.84% |
| uniform | 4 | 512 | 451 (88.08%) | 219 ns | 3.29 µs | **3.51 µs** | 3.64 µs | 4.89 µs | 12.25 µs | 4.463 ms | 28.60% |
| uniform | 8 | 512 | 474 (92.57%) | 219 ns | 4.54 µs | **4.76 µs** | 3.74 µs | 8.27 µs | 28.88 µs | 6.041 ms | 38.72% |
| uniform | 16 | 512 | 475 (92.77%) | 219 ns | 9.30 µs | **9.52 µs** | 3.79 µs | 8.21 µs | 154.13 µs | 12.091 ms | 77.50% |
| decay L=1024 | 1 | 512 | 82 (16.01%) | 75 ns | 454 ns | **529 ns** | 423 ns | 627 ns | 1.22 µs | 0.671 ms | 4.30% |
| decay L=1024 | 2 | 512 | 104 (20.31%) | 75 ns | 1.04 µs | **1.11 µs** | 1.08 µs | 1.39 µs | 2.47 µs | 1.418 ms | 9.09% |
| decay L=1024 | 3 | 512 | 430 (83.98%) | 75 ns | 2.11 µs | **2.19 µs** | 2.36 µs | 2.79 µs | 6.23 µs | 2.780 ms | 17.82% |
| decay L=1024 | 4 | 512 | 446 (87.10%) | 75 ns | 3.34 µs | **3.41 µs** | 3.62 µs | 5.34 µs | 9.54 µs | 4.333 ms | 27.77% |
| decay L=1024 | 8 | 512 | 462 (90.23%) | 75 ns | 5.05 µs | **5.12 µs** | 3.88 µs | 7.79 µs | 34.37 µs | 6.504 ms | 41.69% |
| decay L=1024 | 16 | 512 | 464 (90.62%) | 75 ns | 12.34 µs | **12.41 µs** | 4.06 µs | 8.67 µs | 217.78 µs | 15.757 ms | 101.00% |
| decay L=512 | 1 | 512 | 76 (14.84%) | 69 ns | 458 ns | **527 ns** | 415 ns | 639 ns | 1.26 µs | 0.668 ms | 4.28% |
| decay L=512 | 2 | 512 | 102 (19.92%) | 69 ns | 1.18 µs | **1.25 µs** | 1.13 µs | 1.73 µs | 6.24 µs | 1.595 ms | 10.22% |
| decay L=512 | 3 | 512 | 419 (81.83%) | 69 ns | 2.37 µs | **2.44 µs** | 2.44 µs | 3.80 µs | 8.57 µs | 3.102 ms | 19.88% |
| decay L=512 | 4 | 512 | 436 (85.15%) | 69 ns | 3.65 µs | **3.72 µs** | 3.77 µs | 6.10 µs | 12.84 µs | 4.727 ms | 30.30% |
| decay L=512 | 8 | 512 | 458 (89.45%) | 69 ns | 6.07 µs | **6.14 µs** | 4.06 µs | 14.72 µs | 37.38 µs | 7.794 ms | 49.96% |
| decay L=512 | 16 | 512 | 458 (89.45%) | 69 ns | 15.10 µs | **15.17 µs** | 4.05 µs | 41.46 µs | 218.50 µs | 19.250 ms | 123.40% |
| decay L=256 | 1 | 512 | 88 (17.18%) | 41 ns | 413 ns | **454 ns** | 398 ns | 495 ns | 1.05 µs | 0.576 ms | 3.69% |
| decay L=256 | 2 | 512 | 126 (24.60%) | 41 ns | 1.03 µs | **1.07 µs** | 1.07 µs | 1.57 µs | 2.27 µs | 1.367 ms | 8.76% |
| decay L=256 | 3 | 512 | 400 (78.12%) | 41 ns | 2.10 µs | **2.15 µs** | 2.35 µs | 2.76 µs | 8.32 µs | 2.728 ms | 17.48% |
| decay L=256 | 4 | 512 | 413 (80.66%) | 41 ns | 3.53 µs | **3.57 µs** | 3.72 µs | 5.90 µs | 10.66 µs | 4.536 ms | 29.08% |
| decay L=256 | 8 | 512 | 431 (84.17%) | 41 ns | 6.91 µs | **6.95 µs** | 3.95 µs | 25.09 µs | 39.82 µs | 8.827 ms | 56.58% |
| decay L=256 | 16 | 512 | 432 (84.37%) | 41 ns | 18.47 µs | **18.51 µs** | 4.14 µs | 83.18 µs | 207.81 µs | 23.495 ms | 150.61% |
| monocentric L=512 | 1 | 512 | 57 (11.13%) | 96 ns | 452 ns | **548 ns** | 414 ns | 612 ns | 1.66 µs | 0.695 ms | 4.45% |
| monocentric L=512 | 2 | 512 | 82 (16.01%) | 96 ns | 1.12 µs | **1.22 µs** | 1.08 µs | 1.68 µs | 2.33 µs | 1.551 ms | 9.94% |
| monocentric L=512 | 3 | 512 | 427 (83.39%) | 96 ns | 2.38 µs | **2.48 µs** | 2.44 µs | 3.69 µs | 9.55 µs | 3.152 ms | 20.20% |
| monocentric L=512 | 4 | 512 | 439 (85.74%) | 96 ns | 3.60 µs | **3.69 µs** | 3.69 µs | 5.71 µs | 12.65 µs | 4.694 ms | 30.08% |
| monocentric L=512 | 8 | 512 | 469 (91.60%) | 96 ns | 5.40 µs | **5.50 µs** | 3.92 µs | 10.83 µs | 38.70 µs | 6.985 ms | 44.78% |
| monocentric L=512 | 16 | 512 | 470 (91.79%) | 96 ns | 11.45 µs | **11.54 µs** | 4.02 µs | 14.20 µs | 213.63 µs | 14.651 ms | 93.92% |

**Sample size per rung, which is the survivorship guard:**

- `uniform`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `decay L=1024`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `decay L=512`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `decay L=256`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.
- `monocentric L=512`: **512 of 512** draws produced a diversion — 0 fell on a route with no branch point at all, 0 on a branch point whose only other arcs were the way back or the way the route already goes.

*× 1,269 diversions* multiplies by R8.3's measured **1,269 diversions per Tick** at N = 1, uniform, 40,000 Travellers — which is a figure from a different fleet size than the 1M store above, and is printed because R6.4.2's own threshold is stated as a product against it. The flat denominator this section could have used instead reads **441.47 µs** a search, so a rejoin that costs less than a thousandth of one is the interesting case and a rejoin that costs a tenth of one is the row losing outright.

### R6.4.3 — the addition wake's fan-out, and what `d` should be

`adr/0012`'s contract wakes routes within `d` of a newly added Segment. R1.7 measured that a proximity test over a **matrix** missed 309 of 429 changed entries and missed them silently; this runs the same method over a **route store**. `C` is ground truth — the routes a full recompute on the restored graph returns cheaper than the stored one — and `W(d)` is what the reverse index wakes.

**A caveat stated in advance and running against us**: R1.7's entries are *pairs* and these are *paths*. A path is a longer object with more chances to pass near an edit, so a route store's fan-out should be **worse** than R1.7's at the same `d`, not better. Nothing below is read as the earlier number improving.

**The wake sets a bit; it does not recompute.** So `|W(d)|` is priced in marks, and the refutation is `P(stale)` approaching 1 at every `d` that catches a useful share of `C` — one road edit marking most of the city, every subsequent Trip start recomputing, and the drain bounding nothing.

**O-D rung `uniform`** — 2,048 comparable routes of 2,048 drawn (2,048 found on the damaged graph, 2,048 on the restored one). **`|C|` = 202**, mean improvement 13.69%, best 42.76%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 202, **193 improve by more than 1%** (95.54% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **346** | **36** | 180 | **16.89%** | 82.17% | **82.38%** (159 of 193) | 33.14 µs | 4,014 |
| 1 | 128 | **362** | **31** | 191 | **17.67%** | 84.65% | **84.97%** (164 of 193) | 40.54 µs | 6,026 |
| 2 | 256 | **368** | **31** | 197 | **17.96%** | 84.65% | **84.97%** (164 of 193) | 49.63 µs | 8,241 |
| 4 | 512 | **424** | **26** | 248 | **20.70%** | 87.12% | **87.56%** (169 of 193) | 100.08 µs | 12,936 |
| 8 | 1,024 | **488** | **25** | 311 | **23.82%** | 87.62% | **88.08%** (170 of 193) | 169.20 µs | 28,767 |
| 16 | 2,048 | **650** | **20** | 468 | **31.73%** | 90.09% | **90.67%** (175 of 193) | 372.81 µs | 68,204 |

Reverse index: **225,505 memberships** over 16,384 Cells — **110 a route** — **3.98 MiB** singly linked against the store's 500.21 KiB (8.15×), 4.94 MiB with the `previous` array. Insert **6.27 µs** a route. Evict **24.92 µs** over 6,951 chain steps singly linked, against **2.42 µs** over 110 doubly linked (10.26×). Live entries after the evictions: **0** against a high water of 225,505 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 110 memberships × four ints is **1760 B a route**, so at 1M Citizens the index alone is **1678.46 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `decay L=1024`** — 2,045 comparable routes of 2,048 drawn (2,045 found on the damaged graph, 2,046 on the restored one). **`|C|` = 143**, mean improvement 14.53%, best 51.51%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 143, **140 improve by more than 1%** (97.90% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **256** | **22** | 135 | **12.51%** | 84.61% | **85.00%** (119 of 140) | 21.42 µs | 3,044 |
| 1 | 128 | **270** | **19** | 146 | **13.20%** | 86.71% | **87.14%** (122 of 140) | 26.40 µs | 4,489 |
| 2 | 256 | **277** | **19** | 153 | **13.54%** | 86.71% | **87.14%** (122 of 140) | 33.64 µs | 6,151 |
| 4 | 512 | **333** | **14** | 204 | **16.28%** | 90.20% | **90.71%** (127 of 140) | 60.68 µs | 9,948 |
| 8 | 1,024 | **403** | **12** | 272 | **19.70%** | 91.60% | **91.42%** (128 of 140) | 123.61 µs | 22,576 |
| 16 | 2,048 | **537** | **11** | 405 | **26.25%** | 92.30% | **92.14%** (129 of 140) | 298.47 µs | 54,809 |

Reverse index: **148,436 memberships** over 16,384 Cells — **72 a route** — **3.07 MiB** singly linked against the store's 383.57 KiB (8.20×), 3.80 MiB with the `previous` array. Insert **3.39 µs** a route. Evict **6.84 µs** over 2,021 chain steps singly linked, against **1.41 µs** over 72 doubly linked (4.85×). Live entries after the evictions: **0** against a high water of 148,436 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 72 memberships × four ints is **1152 B a route**, so at 1M Citizens the index alone is **1098.63 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `decay L=512`** — 2,044 comparable routes of 2,048 drawn (2,044 found on the damaged graph, 2,044 on the restored one). **`|C|` = 88**, mean improvement 16.39%, best 51.51%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 88, **86 improve by more than 1%** (97.72% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **178** | **6** | 96 | **8.70%** | 93.18% | **93.02%** (80 of 86) | 21.20 µs | 2,231 |
| 1 | 128 | **189** | **6** | 107 | **9.24%** | 93.18% | **93.02%** (80 of 86) | 24.34 µs | 3,238 |
| 2 | 256 | **202** | **5** | 119 | **9.88%** | 94.31% | **94.18%** (81 of 86) | 30.05 µs | 4,407 |
| 4 | 512 | **245** | **4** | 161 | **11.98%** | 95.45% | **95.34%** (82 of 86) | 38.65 µs | 7,226 |
| 8 | 1,024 | **307** | **3** | 222 | **15.01%** | 96.59% | **96.51%** (83 of 86) | 72.40 µs | 16,526 |
| 16 | 2,048 | **439** | **3** | 354 | **21.47%** | 96.59% | **96.51%** (83 of 86) | 209.37 µs | 40,310 |

Reverse index: **100,829 memberships** over 16,384 Cells — **49 a route** — **2.32 MiB** singly linked against the store's 288.05 KiB (8.27×), 2.87 MiB with the `previous` array. Insert **2.75 µs** a route. Evict **2.48 µs** over 760 chain steps singly linked, against **931 ns** over 49 doubly linked (2.67×). Live entries after the evictions: **0** against a high water of 100,829 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 49 memberships × four ints is **784 B a route**, so at 1M Citizens the index alone is **747.68 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `decay L=256`** — 2,031 comparable routes of 2,048 drawn (2,031 found on the damaged graph, 2,031 on the restored one). **`|C|` = 46**, mean improvement 18.63%, best 59.61%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 46, **43 improve by more than 1%** (93.47% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **113** | **6** | 73 | **5.56%** | 86.95% | **88.37%** (38 of 43) | 15.94 µs | 1,336 |
| 1 | 128 | **125** | **6** | 85 | **6.15%** | 86.95% | **88.37%** (38 of 43) | 16.80 µs | 1,988 |
| 2 | 256 | **142** | **3** | 99 | **6.99%** | 93.47% | **95.34%** (41 of 43) | 20.70 µs | 2,799 |
| 4 | 512 | **195** | **1** | 150 | **9.60%** | 97.82% | **97.67%** (42 of 43) | 29.64 µs | 4,727 |
| 8 | 1,024 | **260** | **0** | 214 | **12.80%** | 100.00% | **100.00%** (43 of 43) | 58.46 µs | 10,992 |
| 16 | 2,048 | **389** | **0** | 343 | **19.15%** | 100.00% | **100.00%** (43 of 43) | 156.61 µs | 27,849 |

Reverse index: **61,927 memberships** over 16,384 Cells — **30 a route** — **1.58 MiB** singly linked against the store's 192.41 KiB (8.41×), 1.94 MiB with the `previous` array. Insert **1.71 µs** a route. Evict **857 ns** over 250 chain steps singly linked, against **577 ns** over 30 doubly linked (1.48×). Live entries after the evictions: **0** against a high water of 61,927 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 30 memberships × four ints is **480 B a route**, so at 1M Citizens the index alone is **457.76 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

**O-D rung `monocentric L=512`** — 2,047 comparable routes of 2,048 drawn (2,047 found on the damaged graph, 2,048 on the restored one). **`|C|` = 122**, mean improvement 10.42%, best 51.51%. Routes made **dearer** by the addition: **0** — the conservation law, and it must read zero. Of those 122, **117 improve by more than 1%** (95.90% of `C`), and the `material` columns below are the same sweep against that subset. Whether `C` or material `C` is the set worth waking is a decision rather than a measurement, so both are printed.

| `d`, Cells | `d`, m | `\|W(d)\|` | `\|C \ W(d)\|` missed | `\|W(d) \ C\|` needless | `P(stale)` | Caught of `C` | **Caught of material `C`** | Query | Chain steps |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | **219** | **24** | 121 | **10.69%** | 80.32% | **80.34%** (94 of 117) | 48.82 µs | 2,540 |
| 1 | 128 | **232** | **18** | 128 | **11.33%** | 85.24% | **85.47%** (100 of 117) | 20.96 µs | 3,800 |
| 2 | 256 | **237** | **17** | 132 | **11.57%** | 86.06% | **85.47%** (100 of 117) | 18.73 µs | 5,094 |
| 4 | 512 | **280** | **13** | 171 | **13.67%** | 89.34% | **88.88%** (104 of 117) | 28.93 µs | 7,788 |
| 8 | 1,024 | **329** | **11** | 218 | **16.07%** | 90.98% | **90.59%** (106 of 117) | 68.40 µs | 17,282 |
| 16 | 2,048 | **492** | **11** | 381 | **24.03%** | 90.98% | **90.59%** (106 of 117) | 191.87 µs | 41,678 |

Reverse index: **195,039 memberships** over 16,384 Cells — **95 a route** — **3.67 MiB** singly linked against the store's 460.50 KiB (8.17×), 4.55 MiB with the `previous` array. Insert **3.64 µs** a route. Evict **17.68 µs** over 5,495 chain steps singly linked, against **1.47 µs** over 95 doubly linked (11.99×). Live entries after the evictions: **0** against a high water of 195,039 — `adr/0006`'s sink, printed on the run where it reads yes.

**The index's own cost per Citizen, which is the column session M's trilemma has no cell for**: 95 memberships × four ints is **1520 B a route**, so at 1M Citizens the index alone is **1449.58 MiB**, against the compressed route's own figure in R6.4.1. Every membership is a Cell the route enters, so this scales with **journey length** and not with `k` — and **nothing R6.4.1 compresses touches it.**

### The denominator, measured twice

R3's finding, addressed to R6 by name on the board: *a denominator measured once has no error bar, and a denominator measured first has a systematic one.* The same 256 uniform flat searches, before anything else in this section and again after all of it.

| Reading | Mean per search |
|---|---:|
| first, cold | 441.47 µs |
| last, warm | 428.32 µs |
| spread | **1.03×** |


## S2 R5.5 — the path source

- **Captured** 2026-08-11 11:29:49 UTC
- **Machine** Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz, 12 logical processors
- **OS** Ubuntu 24.04.4 LTS, X64
- **Runtime** .NET 10.0.10
- **Governor** performance
- **Processors allowed** 2,8 of 12
- **Build** Release

Working rung: 33,018 Segments, 16,697 nodes, 66,036 arcs, 32,069 of them admitting cars. **Run on its own rather than behind R5.1–R5.4**, so the flat denominator's first reading below is the first timed quantity in this process and its ratio against the last reading is load-bearing rather than decorative.

### R5.5.1 — the edit response, which is what the player is waiting for

**A path source is not chosen on what it costs to read; it is chosen on what it costs to make correct again.** `plans/0010`'s first tripwire for this section says so in the form that matters — *a rung that cannot be made correct again within one Tick budget after a plausible gesture is out on a design commitment, not on a number* — because the player is holding the mouse button down while it happens. This table is that quantity, per rung, swept over the gesture sizes R5.1 established the shape of.

**The naive columns exist because R5.2 found a spelling difference large enough to matter — R5.2 does not run in this capture, so its ratio is not restated here and this is where that finding is tested for generality.** `RepairSubtree` takes a changed-arc set, so it has a coalesced spelling and a per-Segment one, exactly as `AbstractGraph` did. If looping it per deleted Segment over a drag is a catastrophe too, then *a per-edit repair API invites the loop that produces it* stops being a routing note and becomes a corpus-wide rule about API shape. If it is not, R5.2's finding is a property of a cluster's edge set and belongs to the hierarchy alone.

**Two rungs have no naive spelling and the reason differs.** The cache rungs repair the abstract graph, which R5.2 has already priced both ways and which is reproduced here only so the columns are comparable. `shared` has no repair *by construction* — R4 established that maintenance is separable from path source, so writing a repair for `RouteStore` would measure the repair rather than the rung, and `plans/0010` prices it as a rebuild precisely so that a loss is a retirement on a number. `flat` has no edit response at all, which is the whole of what it is for: it is the row that gives this column a floor of nothing.

**`cache` and `cache+ttl` must read the same figure here, and printing both is the cheap check that they do.** A rotation is a per-Tick cost and not a per-gesture one, so the TTL cannot change what an edit costs; two rows that disagree would be two rungs that are secretly different experiments, which is the defect R2 shipped as byte-identical peaks and this spike has been paying for since.

| Rung | Gesture | Got | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced |
|---|---:|---:|---:|---:|---:|---:|---:|
| cache | drag 1 | 1 | 0.23 ms | 0.28 ms | — | — | — |
| cache | drag 4 | 4 | 1.18 ms | 7.19 ms | — | — | — |
| cache | drag 16 | 16 | 0.96 ms | 2.15 ms | — | — | — |
| cache | drag 64 | 64 | 2.21 ms | 3.50 ms | — | — | — |
| cache | drag 256 | 173 | 3.64 ms | 8.54 ms | — | — | — |
| cache+ttl | drag 1 | 1 | 0.38 ms | 0.39 ms | — | — | — |
| cache+ttl | drag 4 | 4 | 0.50 ms | 0.92 ms | — | — | — |
| cache+ttl | drag 16 | 16 | 0.59 ms | 0.74 ms | — | — | — |
| cache+ttl | drag 64 | 64 | 2.53 ms | 3.63 ms | — | — | — |
| cache+ttl | drag 256 | 173 | 4.53 ms | 8.65 ms | — | — | — |
| nexthop | drag 1 | 1 | 0.65 ms | 1.80 ms | 0.66 ms | 1.81 ms | 1.00× |
| nexthop | drag 4 | 4 | 1.30 ms | 2.68 ms | 1.16 ms | 2.59 ms | 0.89× |
| nexthop | drag 16 | 16 | 2.87 ms | 8.23 ms | 3.64 ms | 9.57 ms | 1.26× |
| nexthop | drag 64 | 64 | 14.78 ms | 43.64 ms | 21.80 ms | 84.55 ms | 1.47× |
| nexthop | drag 256 | 173 | 25.44 ms | 61.75 ms | 33.53 ms | 93.19 ms | 1.31× |
| shared | drag 1 | 1 | 209.00 ms | 222.29 ms | — | — | — |
| shared | drag 4 | 4 | 220.03 ms | 234.65 ms | — | — | — |
| shared | drag 16 | 16 | 203.77 ms | 212.72 ms | — | — | — |
| shared | drag 64 | 64 | 214.88 ms | 266.10 ms | — | — | — |
| shared | drag 256 | 173 | 205.97 ms | 216.74 ms | — | — | — |
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

- One uncached point-to-point search, arcs returned: 455.25 µs measured first, 498.37 µs measured last, 0.91× apart.

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
| uniform | cache | never | 1117.89 µs | 4116.70 µs | 71.63% | 0.00% | 28.36% | — | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | cache+ttl 64 | never | 2020.31 µs | 4481.58 µs | 46.63% | 0.00% | 53.36% | 5.26 | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | cache+ttl 256 | never | 1364.53 µs | 4086.40 µs | 64.74% | 0.00% | 35.25% | 1.44 | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | cache+ttl 1024 | never | 1123.47 µs | 4061.50 µs | 70.06% | 0.00% | 29.93% | 0.34 | 0 | 0.00% | 0.00% | 1.12% | 255 |
| uniform | nexthop | never | 2.16 µs | 13.26 µs | — | — | — | — | 0 | 16.58% | 26.97% | 913.61% | 254 |
| uniform | shared | never | 2.09 µs | 14.95 µs | — | — | — | — | 0 | 31.21% | 56.46% | 953.61% | 249 |
| uniform | flat | never | 7495.46 µs | 13238.25 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 256 |
| uniform | cache | 64 Ticks | 1150.89 µs | 4261.74 µs | 71.58% | 0.02% | 28.39% | — | 6 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | cache+ttl 64 | 64 Ticks | 2104.89 µs | 4794.59 µs | 46.58% | 0.02% | 53.39% | 5.26 | 7 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | cache+ttl 256 | 64 Ticks | 1385.12 µs | 4740.50 µs | 64.69% | 0.02% | 35.27% | 1.44 | 6 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | cache+ttl 1024 | 64 Ticks | 1208.64 µs | 5516.92 µs | 70.01% | 0.02% | 29.95% | 0.34 | 6 | 0.00% | 0.00% | 1.12% | 254 |
| uniform | nexthop | 64 Ticks | 20.07 µs | 1860.20 µs | — | — | — | — | 0 | 16.64% | 26.97% | 913.61% | 253 |
| uniform | shared | 64 Ticks | 3296.80 µs | 215574.91 µs | — | — | — | — | 0 | 31.28% | 56.46% | 953.61% | 248 |
| uniform | flat | 64 Ticks | 7702.98 µs | 16424.05 µs | — | — | — | — | 7 | 0.00% | 0.00% | 0.00% | 255 |
| uniform | cache | 16 Ticks | 1258.28 µs | 6270.09 µs | 69.89% | 1.70% | 28.39% | — | 26 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | cache+ttl 64 | 16 Ticks | 2122.20 µs | 6078.21 µs | 45.99% | 0.43% | 53.56% | 5.25 | 28 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | cache+ttl 256 | 16 Ticks | 1633.64 µs | 5253.31 µs | 63.42% | 1.12% | 35.44% | 1.44 | 27 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | cache+ttl 1024 | 16 Ticks | 1488.19 µs | 6504.17 µs | 68.38% | 1.66% | 29.95% | 0.34 | 26 | 0.00% | 0.00% | 1.12% | 253 |
| uniform | nexthop | 16 Ticks | 340.62 µs | 51632.28 µs | — | — | — | — | 0 | 16.47% | 26.97% | 913.61% | 252 |
| uniform | shared | 16 Ticks | 12956.13 µs | 219321.27 µs | — | — | — | — | 0 | 30.99% | 56.66% | 953.61% | 247 |
| uniform | flat | 16 Ticks | 8008.34 µs | 15695.04 µs | — | — | — | — | 29 | 0.00% | 0.00% | 0.00% | 254 |
| uniform | cache | 4 Ticks | 1425.48 µs | 5320.85 µs | 68.45% | 3.00% | 28.54% | — | 74 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | cache+ttl 64 | 4 Ticks | 2242.75 µs | 6333.63 µs | 45.31% | 0.83% | 53.85% | 5.20 | 87 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | cache+ttl 256 | 4 Ticks | 1645.37 µs | 4593.37 µs | 62.13% | 2.19% | 35.66% | 1.44 | 80 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | cache+ttl 1024 | 4 Ticks | 1586.83 µs | 6984.83 µs | 66.99% | 2.90% | 30.10% | 0.34 | 74 | 0.00% | 0.00% | 1.12% | 250 |
| uniform | nexthop | 4 Ticks | 704.40 µs | 48360.70 µs | — | — | — | — | 0 | 16.69% | 28.80% | 913.61% | 246 |
| uniform | shared | 4 Ticks | 51541.11 µs | 232317.65 µs | — | — | — | — | 0 | 31.27% | 56.46% | 953.61% | 241 |
| uniform | flat | 4 Ticks | 7617.48 µs | 16659.58 µs | — | — | — | — | 89 | 0.00% | 0.00% | 0.00% | 251 |
| decay L=256 | cache | never | 207.29 µs | 920.05 µs | 69.31% | 0.00% | 30.68% | — | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | cache+ttl 64 | never | 373.51 µs | 1052.67 µs | 45.94% | 0.00% | 54.05% | 5.07 | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | cache+ttl 256 | never | 287.84 µs | 936.46 µs | 62.54% | 0.00% | 37.45% | 1.37 | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | cache+ttl 1024 | never | 248.38 µs | 1099.98 µs | 67.43% | 0.00% | 32.56% | 0.39 | 0 | 0.01% | 0.00% | 3.22% | 251 |
| decay L=256 | nexthop | never | 1.25 µs | 12.06 µs | — | — | — | — | 0 | 149.73% | 340.00% | 5765.28% | 250 |
| decay L=256 | shared | never | 1.25 µs | 13.53 µs | — | — | — | — | 0 | 211.94% | 603.34% | 6098.61% | 248 |
| decay L=256 | flat | never | 1109.27 µs | 3717.19 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 254 |
| decay L=256 | cache | 64 Ticks | 238.06 µs | 1660.57 µs | 69.01% | 0.09% | 30.88% | — | 9 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 64 | 64 Ticks | 413.58 µs | 1786.56 µs | 45.60% | 0.02% | 54.37% | 5.04 | 21 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 256 | 64 Ticks | 291.99 µs | 2295.20 µs | 62.15% | 0.07% | 37.76% | 1.37 | 17 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 1024 | 64 Ticks | 247.31 µs | 1850.74 µs | 67.16% | 0.07% | 32.76% | 0.39 | 9 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | nexthop | 64 Ticks | 17.65 µs | 1764.61 µs | — | — | — | — | 0 | 138.91% | 340.00% | 5765.28% | 249 |
| decay L=256 | shared | 64 Ticks | 3315.20 µs | 228222.08 µs | — | — | — | — | 0 | 200.07% | 600.00% | 6098.61% | 247 |
| decay L=256 | flat | 64 Ticks | 1013.59 µs | 2668.19 µs | — | — | — | — | 21 | 0.00% | 0.00% | 0.00% | 253 |
| decay L=256 | cache | 16 Ticks | 317.58 µs | 2185.26 µs | 68.18% | 0.92% | 30.88% | — | 30 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 64 | 16 Ticks | 477.22 µs | 3901.63 µs | 45.28% | 0.29% | 54.41% | 5.03 | 36 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 256 | 16 Ticks | 363.79 µs | 3553.46 µs | 61.66% | 0.53% | 37.79% | 1.37 | 32 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | cache+ttl 1024 | 16 Ticks | 348.49 µs | 9602.68 µs | 66.38% | 0.85% | 32.76% | 0.39 | 30 | 0.01% | 0.00% | 3.22% | 250 |
| decay L=256 | nexthop | 16 Ticks | 329.43 µs | 49931.62 µs | — | — | — | — | 0 | 139.10% | 340.00% | 5765.28% | 249 |
| decay L=256 | shared | 16 Ticks | 12891.63 µs | 228321.86 µs | — | — | — | — | 0 | 201.36% | 600.00% | 6098.61% | 247 |
| decay L=256 | flat | 16 Ticks | 1015.41 µs | 2465.02 µs | — | — | — | — | 36 | 0.00% | 0.00% | 0.00% | 253 |
| decay L=256 | cache | 4 Ticks | 533.78 µs | 3516.82 µs | 66.77% | 1.73% | 31.49% | — | 96 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | cache+ttl 64 | 4 Ticks | 607.50 µs | 2795.18 µs | 44.31% | 0.58% | 55.10% | 4.96 | 113 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | cache+ttl 256 | 4 Ticks | 556.83 µs | 2755.71 µs | 60.25% | 1.29% | 38.45% | 1.36 | 107 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | cache+ttl 1024 | 4 Ticks | 504.98 µs | 2258.77 µs | 64.96% | 1.68% | 33.34% | 0.39 | 96 | 0.01% | 0.00% | 3.22% | 245 |
| decay L=256 | nexthop | 4 Ticks | 610.22 µs | 42696.16 µs | — | — | — | — | 0 | 141.67% | 340.00% | 5765.28% | 244 |
| decay L=256 | shared | 4 Ticks | 52654.10 µs | 233585.56 µs | — | — | — | — | 0 | 205.16% | 600.00% | 6098.61% | 242 |
| decay L=256 | flat | 4 Ticks | 981.62 µs | 3300.35 µs | — | — | — | — | 113 | 0.00% | 0.00% | 0.00% | 248 |
| monocentric L=512 | cache | never | 882.55 µs | 3337.61 µs | 69.77% | 0.00% | 30.22% | — | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 64 | never | 1568.75 µs | 3475.89 µs | 46.04% | 0.00% | 53.95% | 5.16 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 256 | never | 1115.00 µs | 3731.34 µs | 63.06% | 0.00% | 36.93% | 1.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 1024 | never | 909.11 µs | 2999.38 µs | 68.11% | 0.00% | 31.88% | 0.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | nexthop | never | 1.80 µs | 6.94 µs | — | — | — | — | 0 | 16.58% | 39.96% | 238.57% | 251 |
| monocentric L=512 | shared | never | 1.92 µs | 11.84 µs | — | — | — | — | 0 | 33.21% | 65.63% | 706.97% | 247 |
| monocentric L=512 | flat | never | 6271.30 µs | 14860.00 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 256 |
| monocentric L=512 | cache | 64 Ticks | 974.17 µs | 3944.82 µs | 69.77% | 0.00% | 30.22% | — | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 64 | 64 Ticks | 1643.21 µs | 4276.14 µs | 46.04% | 0.00% | 53.95% | 5.16 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 256 | 64 Ticks | 1106.91 µs | 3251.42 µs | 63.06% | 0.00% | 36.93% | 1.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | cache+ttl 1024 | 64 Ticks | 978.43 µs | 3408.12 µs | 68.11% | 0.00% | 31.88% | 0.35 | 0 | 0.03% | 0.00% | 1.58% | 255 |
| monocentric L=512 | nexthop | 64 Ticks | 18.37 µs | 1753.49 µs | — | — | — | — | 0 | 16.58% | 39.96% | 238.57% | 251 |
| monocentric L=512 | shared | 64 Ticks | 3289.57 µs | 220917.45 µs | — | — | — | — | 0 | 33.21% | 65.63% | 706.97% | 247 |
| monocentric L=512 | flat | 64 Ticks | 6270.16 µs | 11914.80 µs | — | — | — | — | 0 | 0.00% | 0.00% | 0.00% | 256 |
| monocentric L=512 | cache | 16 Ticks | 1095.79 µs | 4092.18 µs | 68.26% | 1.51% | 30.22% | — | 21 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | cache+ttl 64 | 16 Ticks | 1799.42 µs | 6718.28 µs | 45.36% | 0.46% | 54.17% | 5.15 | 21 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | cache+ttl 256 | 16 Ticks | 1284.78 µs | 4986.08 µs | 61.69% | 1.14% | 37.15% | 1.35 | 21 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | cache+ttl 1024 | 16 Ticks | 1118.07 µs | 11236.14 µs | 66.74% | 1.22% | 32.03% | 0.35 | 21 | 0.03% | 0.00% | 1.58% | 253 |
| monocentric L=512 | nexthop | 16 Ticks | 336.12 µs | 52032.21 µs | — | — | — | — | 0 | 16.67% | 43.05% | 238.57% | 249 |
| monocentric L=512 | shared | 16 Ticks | 13587.30 µs | 258664.60 µs | — | — | — | — | 0 | 33.41% | 66.09% | 706.97% | 245 |
| monocentric L=512 | flat | 16 Ticks | 5786.16 µs | 10558.62 µs | — | — | — | — | 21 | 0.00% | 0.00% | 0.00% | 254 |
| monocentric L=512 | cache | 4 Ticks | 1235.58 µs | 4484.80 µs | 66.74% | 2.78% | 30.46% | — | 83 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | cache+ttl 64 | 4 Ticks | 1980.46 µs | 5466.91 µs | 44.31% | 0.83% | 54.85% | 5.08 | 99 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | cache+ttl 256 | 4 Ticks | 1520.45 µs | 5114.03 µs | 60.47% | 1.95% | 37.57% | 1.35 | 87 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | cache+ttl 1024 | 4 Ticks | 1250.46 µs | 5392.35 µs | 65.18% | 2.41% | 32.39% | 0.34 | 88 | 0.03% | 0.00% | 1.58% | 250 |
| monocentric L=512 | nexthop | 4 Ticks | 652.33 µs | 45804.07 µs | — | — | — | — | 0 | 16.75% | 43.05% | 238.57% | 246 |
| monocentric L=512 | shared | 4 Ticks | 63223.76 µs | 292027.24 µs | — | — | — | — | 0 | 33.60% | 66.09% | 706.97% | 242 |
| monocentric L=512 | flat | 4 Ticks | 7753.47 µs | 15904.13 µs | — | — | — | — | 100 | 0.00% | 0.00% | 0.00% | 251 |

256 Ticks, 16 Trip starts per Tick, drawn with repetition from a pool of 512 distinct origin-destination pairs. The cache rungs hold 1024 entries at 8 Chunks per cluster and revalidate at the **per-Segment** Epoch, which is R5.3's recommendation and the rung R5.4 then found a hole in. The two District-granular rungs run at 121 Districts, which is R2's anchor and R4's. Gestures are 16-Segment drags and **the storm never reverts**, so the graph degrades monotonically across a run and the *Unroutable* column is what says whether a row is measuring severance rather than its rung.

**The *Unroutable* column was dead and is now the sharpest staleness instrument in this section — R6.0 repaired the defect R5.5 recorded, and repairing it turned a column that read *zero by construction* into one that moves monotonically with the refresh rate.** `HpaSearch` priced the step *onto* the origin Segment and *off* the destination Segment against a **null cost array** — the pristine `graph.ArcCarTicks` — while the storm deletes into a shadow clone, so the hierarchy returned routes down roads the player had just bulldozed. **The defect was wider than R5.5 filed it**: not the two Access Point remainders but **eight call sites**, and the four it missed are the worse ones, because the same-Segment and adjacent-Segment bypasses return *routable* directly from `Run` without ever entering a confined search. R3.8 puts the bypass at **78.28%** of Legs inside one block, so the defect was heaviest exactly where the local-trip O-D rung lives. The control had it right all along: `PointToPoint` threads its cost array through every call including `SameSegmentCost`, and `HpaSearch` threaded it through none.

**What the repaired column now says is that the residual gap is not a bug — it is the cache serving routes down roads that are gone, and the rotation closes it.** Against a control finding **416** severed lookups over the same 12 rows:

| Rung | Unroutable | Share of the control's |
|---|---:|---:|
| cache, no rotation | 345 | 82.93% |
| cache+ttl 1024 | 350 | 84.13% |
| cache+ttl 256 | 377 | 90.62% |
| cache+ttl 64 | 412 | 99.03% |
| flat (the truth) | 416 | 100.00% |

**Monotone in the refresh rate, with the control as the asymptote**, which is the causal demonstration rather than a correlation: the only thing separating these rungs is how often an entry is forced to look again, and severance is precisely what a route that never looks again cannot discover. **This corroborates R5.5.4 through a different column entirely** — R5.5.4 measures staleness as *detour* against a truth search, and this measures it as *severance*, and the two agree that a rotation clears what the Epoch rung structurally cannot. **It also discharges the corpus's own warning** that a correctness column reading zero is indistinguishable from an instrument that is not wired up: this one was not wired up, for three tasks, and nothing but the control's disagreement gave it away.

**One denominator defect became visible only once the numbers moved, and it is worth recording.** R5.5 published the disagreement as *16 against 416* — but the 16 summed **four** cache rungs where the 416 was **one** control rung, so the two sides never had the same denominator. It did not change R5.5's conclusion, both figures being about 1% of the control. Summed the same way after the repair it reads **1,484 against 416**, which invites exactly the wrong reading — *the hierarchy is 3.6× worse* — when per rung it is **82.93%–99.03%** of the control. **A broken denominator survives while every number it divides is near zero.**

**`nexthop` and `shared` report zero severed lookups at every rung, and that is the unwired-instrument shape rather than a result.** Both answer through a District representative and always return *something*, so neither can report a severance at all. The zero should be read as *this rung has no such column*, not as evidence.

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
| uniform | cache | 16 Ticks | 412 | 98.05% | 0.00% | **0.00%** | — | — | 8 | 1 | holds |
| uniform | cache+ttl 64 | 16 Ticks | 247 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| uniform | cache+ttl 256 | 16 Ticks | 364 | 99.17% | 0.00% | **0.00%** | — | — | 3 | 0 | holds |
| uniform | cache+ttl 1024 | 16 Ticks | 401 | 98.50% | 0.00% | **0.00%** | — | — | 6 | 1 | holds |
| uniform | cache | 4 Ticks | 410 | 95.60% | 0.00% | **0.00%** | — | — | 18 | 8 | holds |
| uniform | cache+ttl 64 | 4 Ticks | 240 | 99.16% | 0.00% | **0.00%** | — | — | 2 | 2 | holds |
| uniform | cache+ttl 256 | 4 Ticks | 358 | 96.92% | 0.00% | **0.00%** | — | — | 11 | 4 | holds |
| uniform | cache+ttl 1024 | 4 Ticks | 399 | 95.98% | 0.00% | **0.00%** | — | — | 16 | 8 | holds |
| decay L=256 | cache | never | 399 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 64 | never | 249 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache+ttl 256 | never | 366 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 1024 | never | 386 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache | 64 Ticks | 398 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 5 | holds |
| decay L=256 | cache+ttl 64 | 64 Ticks | 248 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 2 | holds |
| decay L=256 | cache+ttl 256 | 64 Ticks | 363 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 3 | holds |
| decay L=256 | cache+ttl 1024 | 64 Ticks | 385 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 4 | holds |
| decay L=256 | cache | 16 Ticks | 398 | 98.74% | 0.00% | **0.00%** | — | — | 5 | 4 | holds |
| decay L=256 | cache+ttl 64 | 16 Ticks | 247 | 99.59% | 0.00% | **0.00%** | — | — | 1 | 2 | holds |
| decay L=256 | cache+ttl 256 | 16 Ticks | 363 | 98.89% | 0.00% | **0.00%** | — | — | 4 | 3 | holds |
| decay L=256 | cache+ttl 1024 | 16 Ticks | 385 | 98.96% | 0.00% | **0.00%** | — | — | 4 | 3 | holds |
| decay L=256 | cache | 4 Ticks | 395 | 96.70% | 0.00% | **0.00%** | — | — | 13 | 8 | holds |
| decay L=256 | cache+ttl 64 | 4 Ticks | 239 | 98.74% | 0.00% | **0.00%** | — | — | 3 | 2 | holds |
| decay L=256 | cache+ttl 256 | 4 Ticks | 352 | 97.72% | 0.00% | **0.00%** | — | — | 8 | 3 | holds |
| decay L=256 | cache+ttl 1024 | 4 Ticks | 383 | 96.86% | 0.00% | **0.00%** | — | — | 12 | 7 | holds |
| monocentric L=512 | cache | never | 398 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | never | 238 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | never | 361 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | never | 383 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache | 64 Ticks | 398 | 99.74% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | 64 Ticks | 238 | 100.00% | 0.00% | **0.00%** | — | — | 0 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | 64 Ticks | 361 | 99.72% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | 64 Ticks | 383 | 99.73% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache | 16 Ticks | 398 | 97.48% | 0.00% | **0.00%** | — | — | 10 | 0 | holds |
| monocentric L=512 | cache+ttl 64 | 16 Ticks | 236 | 99.57% | 0.00% | **0.00%** | — | — | 1 | 0 | holds |
| monocentric L=512 | cache+ttl 256 | 16 Ticks | 359 | 98.60% | 0.00% | **0.00%** | — | — | 5 | 0 | holds |
| monocentric L=512 | cache+ttl 1024 | 16 Ticks | 382 | 97.64% | 0.00% | **0.00%** | — | — | 9 | 0 | holds |
| monocentric L=512 | cache | 4 Ticks | 397 | 94.20% | 0.00% | **0.00%** | — | — | 23 | 5 | holds |
| monocentric L=512 | cache+ttl 64 | 4 Ticks | 226 | 99.55% | 0.00% | **0.00%** | — | — | 1 | 1 | holds |
| monocentric L=512 | cache+ttl 256 | 4 Ticks | 352 | 96.59% | 0.00% | **0.00%** | — | — | 12 | 2 | holds |
| monocentric L=512 | cache+ttl 1024 | 4 Ticks | 379 | 94.45% | 0.00% | **0.00%** | — | — | 21 | 4 | holds |

*Resident* is entries of the 512-pair pool the cache still holds; *declared valid* is the share of those the per-Segment Epoch passes; *improvable* is the share a fresh search strictly beats, which is **ground truth and independent of the rung**; *wrongly valid* is the intersection. *Not comparable* is resident entries whose fresh search found nothing, excluded from *improvable* and printed so the denominator is visible rather than assumed. **Total wrongly valid across every cache row: 0. Total holding a Segment that no longer exists: 202. Identity breaks: 0 of 48 rows.**

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

- **Run duration** 400.61 s — from 11:25:34 UTC to 11:32:14 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 6.69 / 4.54 / 3.31 (1 / 5 / 15 min)
- **Load average, at end** 5.56 / 4.46 / 3.65 (1 / 5 / 15 min)
- **CPU stall** 6,655,862 µs over the run — 1.66% of it
- **Memory stall** 1 µs over the run — 0.00% of it
- **IO stall** 210,158 µs over the run — 0.05% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
