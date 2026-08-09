## S2 R0 — the synthetic Road Graph

- **Captured** 2026-08-09 14:22:33 UTC
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

- **Captured** 2026-08-09 14:22:33 UTC
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
| drive | `None` | 8,217 | 300 ns | 723,306 ns | 723,607 ns | 88 ns |
| drive | `Manhattan` | 2,813 | 292 ns | 292,082 ns | 292,374 ns | 103 ns |
| drive | `Octile` | 3,506 | 325 ns | 374,148 ns | 374,473 ns | 106 ns |
| drive | `Chebyshev` | 4,121 | 292 ns | 440,388 ns | 440,681 ns | 106 ns |
| drive | `EuclideanFloor` | 3,712 | 502 ns | 758,625 ns | 759,127 ns | 204 ns |
| walk | `None` | 276 | 181 ns | 20,390 ns | 20,571 ns | 73 ns |
| walk | `Manhattan` | 32 | 312 ns | 3,981 ns | 4,293 ns | 124 ns |
| walk | `Octile` | 46 | 326 ns | 5,761 ns | 6,088 ns | 125 ns |
| walk | `Chebyshev` | 58 | 240 ns | 6,447 ns | 6,688 ns | 111 ns |
| walk | `EuclideanFloor` | 50 | 376 ns | 10,604 ns | 10,981 ns | 212 ns |

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

- **Captured** 2026-08-09 14:22:57 UTC
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
| 16 | 16 | 24.56 ms | 1,535,480 ns | 16,685 | 1.00 KiB | 56 Segments | 56.00 KiB |
| 64 | 64 | 96.27 ms | 1,504,296 ns | 16,685 | 16.00 KiB | 64 Segments | 1.00 MiB |
| 100 | 100 | 150.72 ms | 1,507,207 ns | 16,685 | 39.06 KiB | 63 Segments | 2.40 MiB |
| 121 | 121 | 185.52 ms | 1,533,258 ns | 16,685 | 57.19 KiB | 65 Segments | 3.63 MiB |
| 256 | 256 | 388.05 ms | 1,515,835 ns | 16,685 | 256.00 KiB | 65 Segments | 16.25 MiB |
| 400 | 400 | 616.04 ms | 1,540,100 ns | 16,685 | 625.00 KiB | 65 Segments | 39.67 MiB |
| 1,024 | 1,024 | 1556.90 ms | 1,520,415 ns | 16,685 | 4.00 MiB | 70 Segments | 280.00 MiB |
| 2,025 | 2,025 | 3071.63 ms | 1,516,856 ns | 16,685 | 15.64 MiB | 69 Segments | 1.05 GiB |
| 4,096 | 4,096 | 6216.74 ms | 1,517,761 ns | 16,685 | 64.00 MiB | 65 Segments | 4.06 GiB |

*Settled* is nodes settled by the last search of the rung — constant across rungs by construction, because a one-to-all has no goal to prune toward. **That is why cold build is linear in District count, and it is also why a departure from linearity is readable as an artefact rather than as a finding.** The whole sweep is walked once untimed before any of it is timed, because four weaker warm-ups each left a per-search cost falling smoothly with District count — the shape a reader would most readily believe, and the process leaving tier 0. `OneToAll.Run` is called once per District, so the early rungs are the ones that call it too few times.

*Route store* is `n² × mean route length × 4 B`, the Segment sequences at four bytes each. **It is the figure that fails first**, and it fails on `adr/0006` grounds rather than performance ones — see R1.7, where the same store turns out to be what a dirty-region rebuild needs in order to be sound.

### R1.3 — the read, in both access patterns

**The read is the measurement that matters most.** `02 §5.8` makes *never resolve a route inside the choice loop* a rule — named as the one thing UrbanSim gets architecturally right that this design must not violate. If the matrix read is not cheap, that rule is unenforceable and the finding is larger than S2.

`references.md §2` describes the choice loop **twice in one sentence** — *"what is the commute from this candidate dwelling to any job?"*, which is one origin against many destinations and therefore a sequential **row scan**, and *"many-to-many, evaluated tens of thousands of times per cycle"*, which reads as **scattered**. Both are timed, so which one the choice loop performs becomes a design question with a priced answer rather than a detail settled by whoever writes the loop.

| Districts | Resident | Row scan | Scattered | Scattered ÷ row | vs K2 |
|---:|---:|---:|---:|---:|---:|
| 16 | 1.00 KiB | 0.65 ns | 1.19 ns | 1.81× | 0.08× |
| 64 | 16.00 KiB | 0.45 ns | 1.17 ns | 2.56× | 0.08× |
| 100 | 39.06 KiB | 0.53 ns | 1.17 ns | 2.19× | 0.08× |
| 121 | 57.19 KiB | 0.53 ns | 1.17 ns | 2.21× | 0.08× |
| 256 | 256.00 KiB | 0.49 ns | 1.31 ns | 2.65× | 0.09× |
| 400 | 625.00 KiB | 0.52 ns | 1.55 ns | 2.96× | 0.11× |
| 1,024 | 4.00 MiB | 0.61 ns | 1.71 ns | 2.79× | 0.12× |
| 2,025 | 15.64 MiB | 0.58 ns | 2.81 ns | 4.80× | 0.20× |
| 4,096 | 64.00 MiB | 0.61 ns | 5.25 ns | 8.52× | 0.38× |

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
| **Centre** | Full rebuild | 121 | 176.86 ms | 0 | yes, by definition |
| | Dirty region — rows whose District touches it | 1 | 0.17 ms | 309 of 429 | **no** |
| | Routes crossing it — needs the route store | 121 rows, 430 entries | 180.54 ms | 0 of 429 | yes |
| **Corner** | Full rebuild | 121 | 176.81 ms | 0 | yes, by definition |
| | Dirty region — rows whose District touches it | 1 | 0.00 ms | 132 of 252 | **no** |
| | Routes crossing it — needs the route store | 121 rows, 253 entries | 177.57 ms | 0 of 252 | yes |

The centre edit severs **484 arcs** and moves **429** of the matrix's 14,641 entries; the corner edit severs **552** and moves **252**.

**The dirty region is a spatial test on a non-spatial quantity, and that is the finding.** A path from District *i* to District *j* can cross the edited ground without either endpoint being near it, so rebuilding by *which Districts the region overlaps* misses exactly the long routes the matrix exists to serve — and it misses them **silently**, leaving entries that are stale rather than merely coarse. Making it sound requires knowing **which routes crossed the region**, which means keeping the route store R1.2 priced, and that store is the one that does not fit. **The matrix's cheap invalidation and its cheap storage are the same trade, taken twice.**

**And the sound rung rebuilds every row anyway, which is a finding about the matrix rather than about the edit.** A one-to-all fills a whole row, so the build granularity *is* the row — and every row holds the entry addressed *to* the edited District, whose route necessarily ends inside it. So however few entries an edit invalidates, **at least one lands in every row and the incremental path collapses into the full one.** The entries column is the work genuinely needed; the rows column is the work the structure forces. An incremental rebuild worth having would need a build kernel finer than one-to-all — a point-to-point search per entry, which R0 priced at 435 µs against this row's ~1.5 ms for a hundred and twenty-one of them.

### R1.8 — time resolution: one matrix, or five

**A second unstated axis, and `plans/0010` is right that it interacts with everything else.** A single Day-average matrix cannot represent the peak every other figure in this spike is measured at: morning inbound and evening outbound cancel, and the asymmetry the directed graph exists to carry vanishes into the mean. A per-phase matrix multiplies build cost and resident size by five and gives the choice loop a travel time that matches the moment being asked about.

| Resolution | Build | Resident | Mean asymmetry | p90 | One-way pairs at 40 Ticks |
|---|---:|---:|---:|---:|---:|
| Day average, one matrix | 257.56 ms | 57.19 KiB | 0.08 Ticks | 0.23 Ticks | 1 |
| — of which `Dawn` | | 57.19 KiB | 0.00 Ticks | 0.02 Ticks | 0 |
| — of which `MorningPeak` | | 57.19 KiB | 2.19 Ticks | 6.27 Ticks | 76 |
| — of which `Midday` | | 57.19 KiB | 0.00 Ticks | 0.00 Ticks | 0 |
| — of which `EveningPeak` | | 57.19 KiB | 1.79 Ticks | 5.10 Ticks | 64 |
| — of which `Night` | | 57.19 KiB | 0.00 Ticks | 0.00 Ticks | 0 |
| **Per phase, five matrices** | 983.84 ms | 285.95 KiB | | | |

**The Day-average row is the one to read, and it should be read against the peak rows rather than against the others.** It is the matrix a single-resolution design gives the choice loop, and what it reports about the morning peak is what a Household would be deciding on.

The average is taken over the **cost**, not over the volume. BPR is convex, so the delay at the mean volume is strictly less than the mean of the delays — averaging volumes first would give a Day-average matrix describing a city with no rush hour in it at all, rather than one whose rush hour has been smeared. It is also **unweighted**, because the sun arc's phase widths are `plans/0010`'s open decision 5a and an unweighted mean is the only average available while they are unsized.

**And that is the refresh-cadence decision arriving from a direction nobody expected.** `plans/0010` decision 2 files the matrix refresh cadence as *almost certainly hash-bearing* — two cadences produce two cities, so it is a design change under `05 §4` rather than a free knob. Time *resolution* is the same class of decision and the corpus has not named it at all: a Day-average matrix and a per-phase one give the choice loop different answers to the same question, so they are different cities, and **the choice belongs beside cadence in whatever settles that.**


## S2 R2 — the path source, the crossover, and the attribution lag

- **Captured** 2026-08-09 14:23:50 UTC
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
| **searched** | — | 13.03 MiB | 733,410 ns | 25 ns | 0.00% | 0.00% |
| **shared** | 195.44 ms | 3.46 MiB | 2,671 ns | 29 ns | 36.01% | 71.39% |
| **next-hop** | 260.57 ms | 7.70 MiB | 0 ns | 35 ns | 18.52% | 40.70% |

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
| 37,000 | 25,596 | 131,400 ns | 5,133 ps | band floor |
| 56,000 | 38,865 | 134,944 ns | 3,472 ps | **the derived Day-average** |
| 111,000 | 76,823 | 268,025 ns | 3,488 ps | band ceiling / 2× peak |
| 170,000 | 117,537 | 396,923 ns | 3,377 ps | 3× peak |

Timed over the **real** crossing distribution — the arcs an advancing fleet actually entered and left, captured and replayed — rather than over drawn indices, because whether the volume column sits in L2 is a property of how scattered those indices are and drawing them would have measured the draw.

#### Aggregate, against District count

| Districts | Pairs in flight | Arc writes | Per cycle | Crossover cycle |
|---:|---:|---:|---:|---:|
| 16 | 256 | 14,626 | 0.42 ms | 3 Ticks |
| 64 | 3,919 | 244,728 | 4.40 ms | 32 Ticks |
| 100 | 9,230 | 584,527 | 10.68 ms | 79 Ticks |
| 121 | 13,181 | 851,358 | 15.04 ms | 111 Ticks |
| 256 | 36,050 | 2,309,725 | 43.11 ms | 319 Ticks |
| 400 | 46,218 | 2,979,576 | 59.66 ms | 442 Ticks |

*Crossover cycle* is the cycle length at which one smear costs what direct attribution costs over the same span, at the derived 56,000 in flight. **Longer than this, aggregate is cheaper; shorter, direct is.** `adr/0041`'s own arithmetic put it near 10 Ticks from an assumed crossing rate; R2.2 measured the rate.

The smear is the **conserving** form — a Traveller on a route of total time `T` contributes `t_s / T` to each Segment, so the shares sum to one and `adr/0041`'s invariant holds. Adding the whole pair count to every Segment would be cheaper per write and would put one vehicle on fifty Segments at once. **A rejected alternative implemented weakly makes the price of rejecting it look smaller than it is.**

#### Where the crossover inverts, across the peaking sweep

`plans/0010`: *"only one side of it moves — direct attribution scales with vehicles in flight and is peak-sensitive; aggregate scales with `zone count² × route length` and is not. **Report the peaking factor at which the crossover inverts.**"* At the anchor's District count:

| Congestion cycle | Aggregate/Tick | Peaking factor that inverts it |
|---:|---:|---:|
| 1 Ticks | 16,329,785 ns | 121.01× |
| 10 Ticks | 1,632,978 ns | 12.10× |
| 25 Ticks | 653,191 ns | 4.84× |
| 50 Ticks | 326,595 ns | 2.42× |
| 100 Ticks | 163,297 ns | 1.21× |
| 200 Ticks | 81,648 ns | 0.60× |

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

**`adr/0041` is owed a correction, small and worth making.** It calls the path source *"a performance axis with no correctness content"*. R2.1's detour columns are correctness content: a Traveller handed a coarse route drives a different Trip, which under `05 §4`'s test is a different city. The ADR's substantive claim survives intact — experience and contribution stay the same list of Segments under every rung, because a Traveller increments whatever it actually drives — so this amends a sentence and not a decision.

**The crossing rate is now measured and `adr/0041`'s own revisit trigger is the place to record it.** Its cost arithmetic assumes one Segment per Tick; R2.2 reports what the graph actually produces at R0's density. Every figure in R2a scales linearly on that number.

**What R2 does not settle.** The peaking factor is still unsized (decision 5a), so the inversion table is a curve and not a verdict. District count remains R1's open trade, and R2.1 adds a second structure to it — the next-hop table is linear in District count where the route store is quadratic, so the two rungs rank differently at different District counts and neither ranking is a reason to pick one. And nothing here prices **invalidation**, which is R5's.


## S2 R3 — HPA\*, and the cluster size it owns

- **Captured** 2026-08-09 14:24:19 UTC
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
| 1 | 2.95 ms | 17,505 | 180 ns | 6 |
| 1, reduced | 2.91 ms | 17,505 | 177 ns | 6 |
| 1, reduced + paths | 2.26 ms | 18,592 | 138 ns | 5 |
| 2 | 4.37 ms | 68,766 | 1,067 ns | 10 |
| 2, reduced | 3.75 ms | 68,766 | 917 ns | 8 |
| 2, reduced + paths | 5.66 ms | 137,352 | 1,382 ns | 13 |
| 4 | 9.90 ms | 206,142 | 9,670 ns | 23 |
| 4, reduced | 9.75 ms | 206,142 | 9,526 ns | 22 |
| 4, reduced + paths | 16.51 ms | 412,218 | 16,127 ns | 38 |
| 8 | 21.33 ms | 574,207 | 83,340 ns | 49 |
| 8, reduced | 26.52 ms | 574,207 | 103,617 ns | 61 |
| 8, reduced + paths | 42.40 ms | 1,148,375 | 165,634 ns | 98 |
| 16 | 37.62 ms | 2,014,227 | 587,948 ns | 87 |
| 16, reduced | 44.61 ms | 2,014,227 | 697,139 ns | 103 |
| 16, reduced + paths | 75.34 ms | 4,028,411 | 1,177,264 ns | 175 |
| 32 | 72.80 ms | 9,861,622 | 4,550,273 ns | 169 |
| 32, reduced | 77.42 ms | 9,861,622 | 4,838,790 ns | 180 |
| 32, reduced + paths | 154.19 ms | 19,723,213 | 9,637,036 ns | 358 |
| 64 | 168.70 ms | 67,626,483 | 42,176,344 ns | 392 |
| 64, reduced | 166.55 ms | 67,626,483 | 41,639,327 ns | 387 |
| 64, reduced + paths | 328.03 ms | 135,252,966 | 82,007,774 ns | 763 |

One flat `Chebyshev` drive search in this process: **429,660 ns**, 4,138 nodes expanded, 58 path Segments.

### R3.3 — the query, which is the column R3 exists for

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed apart because they have different customers: R1 showed the travel-time matrix already answers the first more cheaply than any search can, and `adr/0041` needs the second — a **next Segment**, every Tick, for every vehicular Traveller in flight.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled | Relaxed | Arcs |
|---:|---:|---:|---:|---:|---:|---:|---:|
| **flat** | **429,660 ns** | 1.00× | — | — | 4,138 nodes | 16,442 arcs | 58 |
| 1 | 431,399 ns | 0.99× | 422,767 ns | 1.01× | 4,138 + 4 | 15,962 + 16 | 58 |
| 1, reduced | 414,595 ns | 1.03× | 429,262 ns | 1.00× | 4,138 + 4 | 15,961 + 16 | 58 |
| 1, reduced + paths | 427,062 ns | 1.00× | 426,073 ns | 1.00× | 4,138 + 4 | 15,961 + 16 | 58 |
| 2 | 465,610 ns | 0.92× | 471,296 ns | 0.91× | 4,089 + 12 | 19,808 + 48 | 58 |
| 2, reduced | 435,523 ns | 0.98× | 472,904 ns | 0.90× | 4,089 + 12 | 15,780 + 48 | 58 |
| 2, reduced + paths | 426,058 ns | 1.00× | 434,344 ns | 0.98× | 4,089 + 12 | 15,780 + 48 | 58 |
| 4 | 486,540 ns | 0.88× | 533,274 ns | 0.80× | 2,988 + 38 | 33,621 + 156 | 58 |
| 4, reduced | 330,733 ns | 1.29× | 361,417 ns | 1.18× | 2,988 + 38 | 11,442 + 156 | 58 |
| 4, reduced + paths | 339,262 ns | 1.26× | 335,371 ns | 1.28× | 2,988 + 38 | 11,442 + 156 | 58 |
| 8 | 395,637 ns | 1.08× | 430,571 ns | 0.99× | 1,708 + 131 | 39,298 + 527 | 58 |
| 8, reduced | 240,913 ns | 1.78× | 265,142 ns | 1.62× | 1,708 + 131 | 6,351 + 527 | 58 |
| 8, reduced + paths | 204,399 ns | 2.10× | 208,558 ns | 2.06× | 1,708 + 131 | 6,351 + 527 | 58 |
| 16 | 316,022 ns | 1.35× | 358,590 ns | 1.19× | 886 + 464 | 36,440 + 1,852 | 58 |
| 16, reduced | 134,921 ns | 3.18× | 288,801 ns | 1.48× | 886 + 464 | 3,143 + 1,852 | 58 |
| 16, reduced + paths | 141,552 ns | 3.03× | 139,560 ns | 3.07× | 886 + 464 | 3,143 + 1,852 | 58 |
| 32 | 318,537 ns | 1.34× | 426,693 ns | 1.00× | 422 + 1,731 | 30,098 + 6,866 | 58 |
| 32, reduced | 180,329 ns | 2.38× | 548,279 ns | 0.78× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 32, reduced + paths | 177,978 ns | 2.41× | 186,982 ns | 2.29× | 422 + 1,731 | 1,381 + 6,866 | 58 |
| 64 | 876,509 ns | 0.49× | 1,043,535 ns | 0.41× | 166 + 8,360 | 19,705 + 33,095 | 58 |
| 64, reduced | 766,915 ns | 0.56× | 1,326,907 ns | 0.32× | 166 + 8,360 | 544 + 33,095 | 58 |
| 64, reduced + paths | 767,392 ns | 0.55× | 787,263 ns | 0.54× | 166 + 8,360 | 544 + 33,095 | 58 |

*Settled* and *Relaxed* are **abstract + concrete**: portals settled and abstract edges relaxed by the hierarchical search, plus nodes settled and arcs relaxed by the two insertions. **The two halves are what the clock column is made of, and they move in opposite directions** — a larger cluster means fewer portals and more insertion.

1,000 drive queries per rung, drawn once and shared by every rung and by the flat search, and **the refined column is a second pass over the same set** rather than over a prefix of it. Sample sizes are stated per rung throughout — R1's entry-error table published a row built from nine searches beside rows built from two thousand, because its sampler shrank with the swept axis.

**The denominator is measured twice, on either side of the sweep, and the ratios divide by the second.** First pass **448,134 ns**, second **429,660 ns** — a spread of 4.29%. The first pinned capture of this task read 1,240,143 ns against 425,803 ns for the same code unpinned while every hierarchical rung stood still, because the flat loop was the first timed thing in the process and the clock had not ramped. Every ratio here divides by this number, so it is the one place an artefact would decorate the whole task. The second pass is quoted because the rungs are all measured after the warm sweep and share its process state; the first does not.

The two passes returned **0** differing route costs out of 1,000 — printed because it must read zero. The same query set over the same graph is the same search, and a non-zero here would mean the flat baseline every correctness column is judged against had moved underneath them.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic — *one Leg against 530–574 arrivals per Tick, ~400 ms of searching per 15.60 ms of Tick budget* — and that test applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to consume the whole budget on its own — or, put the way that depends on nothing derived, **routing fits only while fewer Trips start per Tick than the break-even column below.**

| Rung | Per route | **Break-even Trips/Tick** | At the working 550 | Fits |
|---|---:|---:|---:|---|
| **flat** | 429,660 ns | **36** | 236.31 ms | 15.14× over |
| 1, reduced + paths | 426,073 ns | **36** | 234.34 ms | 15.02× over |
| 2, reduced + paths | 434,344 ns | **35** | 238.88 ms | 15.31× over |
| 4, reduced + paths | 335,371 ns | **46** | 184.45 ms | 11.82× over |
| 8, reduced + paths | 208,558 ns | **74** | 114.70 ms | 7.35× over |
| 16, reduced + paths | 139,560 ns | **111** | 76.75 ms | 4.92× over |
| 32, reduced + paths | 186,982 ns | **83** | 102.84 ms | 6.59× over |
| 64, reduced + paths | 787,263 ns | **19** | 432.99 ms | 27.75× over |

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
| 1 | 206 | 552 | 2 | 7.51 ms | 42,428 ns | 10.12× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 5.42 ms | 55,268 ns | 7.77× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 10.94 ms | 93,756 ns | 4.58× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 21.19 ms | 171,991 ns | 2.49× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 41.42 ms | 305,446 ns | 1.40× | 100.00% | 0.00% | 0.00% |
| all, reduced | 3,337 | 11,768 | 3 | 40.99 ms | 132,802 ns | 3.23× | 100.00% | 0.00% | 0.00% |

1,000 queries per rung, the same set R3.3 uses. *Edges each* is the abstract graph's mean degree, and the flat graph's is 3 — **the comparison this whole section exists to make.**

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted. Only the clusters holding that Segment's endpoints can have changed, so only their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy too.

| Rung | Operation | Cost | Clusters touched | Share of cold build | Edits in one build |
|---|---|---:|---:|---:|---:|
| 1 | re-cost | 572 ns | 1.96 | 0.01% | 5,173 |
| 1, reduced | rebuild cluster | 272,181 ns | 1.96 | 9.33% | 10 |
| 1, reduced + paths | rebuild cluster | 292,743 ns | 1.96 | 12.93% | 7 |
| 2 | re-cost | 1,860 ns | 1.37 | 0.04% | 2,350 |
| 2, reduced | rebuild cluster | 162,512 ns | 1.37 | 4.32% | 23 |
| 2, reduced + paths | rebuild cluster | 168,090 ns | 1.37 | 2.96% | 33 |
| 4 | re-cost | 9,742 ns | 1.06 | 0.09% | 1,016 |
| 4, reduced | rebuild cluster | 77,468 ns | 1.06 | 0.79% | 125 |
| 4, reduced + paths | rebuild cluster | 92,226 ns | 1.06 | 0.55% | 179 |
| 8 | re-cost | 99,922 ns | 1.03 | 0.46% | 213 |
| 8, reduced | rebuild cluster | 138,122 ns | 1.03 | 0.52% | 192 |
| 8, reduced + paths | rebuild cluster | 213,001 ns | 1.03 | 0.50% | 199 |
| 16 | re-cost | 543,119 ns | 1.03 | 1.44% | 69 |
| 16, reduced | rebuild cluster | 656,407 ns | 1.03 | 1.47% | 67 |
| 16, reduced + paths | rebuild cluster | 1,178,317 ns | 1.03 | 1.56% | 63 |
| 32 | re-cost | 4,017,148 ns | 1.03 | 5.51% | 18 |
| 32, reduced | rebuild cluster | 4,368,119 ns | 1.03 | 5.64% | 17 |
| 32, reduced + paths | rebuild cluster | 8,823,759 ns | 1.03 | 5.72% | 17 |
| 64 | re-cost | 40,674,600 ns | 1.00 | 24.10% | 4 |
| 64, reduced | rebuild cluster | 41,373,963 ns | 1.00 | 24.84% | 4 |
| 64, reduced + paths | rebuild cluster | 80,224,547 ns | 1.00 | 24.45% | 4 |

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

- **Captured** 2026-08-09 14:25:28 UTC
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
| backward Dijkstra | 195.42 ms | 1,615,047 ns | — | — | — |
| vector exchange | 106.21 ms | 877,787 ns | 14,333,149 | 175 | 0 |

At the 121-District anchor, over 16,697 nodes. 0 column(s) hit the 4,096-round cap.

**Both arrive at the same table, and the one that was expected to lose does not.** Vector exchange is Bellman-Ford with an active set — the version anyone would actually write, not the textbook one that sweeps every node every round — and on a road network it beats a binary-heap Dijkstra, because a degree-3 graph with well-behaved costs settles nearly in order anyway and the heap is pure overhead. **An earlier draft of this paragraph asserted the opposite** and was written before the column existed; it is recorded because a spike whose prose predicts its own numbers is a spike that will eventually publish the prediction instead of the number.

**What it does not show is that distance-vector is cheap**, and the next three sections are where that is decided. Cold start is not the protocol's claim — repair is — so every scheme below starts from the identical Dijkstra-built table, copied rather than re-derived.

### R4.4 — one deleted Segment, which is the core verb

A Segment is deleted, the whole table is brought back to correct by each scheme in turn, and every scheme is audited against a table rebuilt from scratch on the edited graph. **The audit is not a formality**: this spike has published a `v/c` of 883×, a pair of byte-identical rungs and a denominator wrong by 193%, and in every case the surrounding columns looked healthy.

| Scheme | Per edit | Against rebuild | Relaxations | Rounds / settles | Wrong cost | Stranded |
|---|---:|---:|---:|---:|---:|---:|
| **rebuild** — every column | 194.94 ms | — | — | — | — | — |
| DSDV, sequenced | 450.80 ms | **2.31× slower** | 36,982,307 | 24,137 | 0 (0.00%) | 0 |
| DSDV, unsequenced | 29.80 ms | 6.54× faster | 2,510,526 | 3,047 | 0 (0.00%) | 0 |
| **dynamic repair** — affected subtree | 3.05 ms | 63.86× faster | 201,014 | 17,494 | 0 (0.00%) | 0 |

8 deleted Segments, drawn uniformly, each repaired across all 121 columns — 16,162,696 entries audited per scheme. Columns that hit the 4,096-round cap: sequenced 0, unsequenced 0.

The rebuild denominator read 193,580,722 ns on the first edit and 195,444,648 ns on the last, both published because R3 found the same quantity moving 193% between its first and last measurement in one process. **It also disagrees with R2's published build of 474.47 ms by rather more than the spread within this process**, and R4 does not resolve that: every ratio in this section is taken against R4's own in-process measurement, which is R3's rule, so no conclusion here moves either way. The discrepancy is owed to R7.

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
| 0.10% (57) | 204.13 ms | 351.08 ms | 37.19 ms | 5.48× | 0.00% |
| 1.00% (635) | 197.47 ms | 362.72 ms | 123.04 ms | 1.60× | 0.00% |
| 10.00% (6,474) | 217.22 ms | 394.93 ms | 319.21 ms | 0.68× | 0.00% |
| 100.00% (64,138) | 187.50 ms | 4460.98 ms | 306.12 ms | 0.61× | 0.00% |

At the 121-District anchor. Moved arcs take their morning-peak cost, so the change is the real congestion field rather than a synthetic perturbation. *Wrong cost* is the dynamic-repair rung audited against the rebuild.

### R4.7 — the rolling refresh, which needs none of this machinery

**The scheme a person would write if nobody had said the words *distance vector*.** Rebuild *k* columns every Tick, forever, in rotation. It repairs edits and congestion drift with one mechanism because it does not distinguish them; it needs no invalidation, no Epoch and no sequence numbers; and its worst-case staleness is bounded by construction at `destinations / k` Ticks. What it costs is a fixed slice of every Tick whether or not anything changed.

| Columns per Tick | Cost per Tick | Share of 15.6 ms | Worst staleness |
|---:|---:|---:|---:|
| 1 | 1,611,209 ns | 10.32% | 121 Ticks |
| 2 | 3,222,418 ns | 20.65% | 61 Ticks |
| 4 | 6,444,836 ns | 41.30% | 31 Ticks |
| 8 | 12,889,672 ns | 82.62% | 16 Ticks |
| 121 | 194,956,289 ns | 1249.71% | 1 Ticks |

One column costs 1,611,209 ns at the 121-District anchor. **Staleness is in Ticks and a Tick is ~10.5 in-world seconds**, so a full rotation at one column per Tick is about 21 in-world minutes — well inside a congestion cycle and far outside a player's patience after deleting a road. The two consumers want different rates, which is the finding: **drift is satisfied by a slow rotation and an edit is not**, so a rolling refresh alone cannot serve the core verb no matter how it is tuned.

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

- **Distance-vector is out, and on none of the three grounds anybody expected.** Not memory: at District granularity the table is 23.12 MiB against a 172.27 MiB world, and `plans/0010`'s wire does not fire. Not correctness: with sequence numbers it converges to exactly the rebuilt table, 0 entries wrong, on both a deleted Segment and a severance. **It is out because it costs more than the rebuild it exists to avoid** — 472.53 ms against 217.36 ms for one deleted Segment, **2.17× slower** — and 145× more than the scheme this plan never named.

  The reason is structural rather than a constant, which is why no tuning recovers it. An odd-sequence unreachability claim outranks every finite route in circulation by construction — that is exactly what stops count-to-infinity — so once the poison has spread, **nothing any neighbour still believes can restore a route.** Only a newer *even* sequence number, issued by the destination itself, outranks it. So one broken link obliges the destination to re-flood its entire tree, because every node must at minimum accept the new number. **The property that makes deletion safe is the same property that makes deletion expensive**, and they cannot be separated.

- **`references.md`'s claim about sequence numbers is confirmed by measurement**, and under `adr/0043` it had never been more than an argument. On a severed destination the unsequenced version does **215,894,753 relaxations against 133,258** — 1,620× the work — fails to converge within 4,096 rounds, and leaves **16,684 of 16,697 entries wrong**. The sequenced version converges in 121 rounds with nothing wrong. *If we adopt distance-vector routing, we take DSDV's version, not Citybound's* is now a finding rather than a reading.

- **The scheme that wins was not on the ballot.** Invalidating the affected subtree and re-deriving it from its own valid boundary repairs a deleted Segment in **3.35 ms against a 217.36 ms rebuild — 64.81× — with 0 entries wrong**, and converges on a severance too. It is not distance-vector, it needs no sequence numbers and no Epoch, and it was measured only because pricing solely the candidate a plan names is how a spike produces a verdict it has not earned.

- **A rebuild is not the fallback anybody feared.** It is 217.36 ms for the whole 121-column table — 1.75 ms per column — which a rolling refresh spends at **11.19% of one Tick** for a full rotation every 121 Ticks. That is affordable. What it cannot do is answer an *edit* promptly, because a rotation is a cadence and an edit is an event: **drift wants a slow rotation and the core verb does not**, so the two consumers need different mechanisms and this is the section that shows why.

**Not decided, and owed.**

- **The next-hop table's error is far worse than R2 measured, and R4 does not know what to do about it.** R2's **18.52%** mean detour was taken on the uniform draw, which R4.1 shows is the longest-trip distribution available — 8.53 km mean on a 16.4 km map. Aiming a Traveller at a District representative is a roughly fixed error in Ticks charged against a shrinking journey, so the detour rises to **36.04%** at a 4.1 km mean, **62.02%** at 2.6 km and **128.82%** at 1.5 km. **A Traveller driving more than twice as far as it should is a different city under `05 §4`**, not a tuning figure. This does not decide against the table — it says the table's granularity is the open question, and R2's decision 11 (the representative funnel) is the same question arriving from the other side. **The two should be answered once.**

- **The congestion-drift break-even sits between 1% and 10% of arcs moved**, and nothing in the corpus says which side the design lands on, because the refresh cadence is `plans/0010` decision 2 and still open. Below it, dynamic repair wins; above it, a plain rebuild wins and every incremental scheme is doing extra work to arrive at the same table. **The cadence therefore chooses the maintenance scheme**, which nobody has said, and it is a hash-bearing decision that was filed as tuning.

- **The O-D distribution is an axis now and still not a measurement.** R4.1 replaces a silent guess with a swept family, which is what this plan does everywhere else, but the family is invented. What would replace it is Trip generation, and that does not exist. **Every figure in R4.8, and every speedup R3 published, is a point on a curve whose location nobody can yet fix.**

- **R4's rebuild denominator disagrees with R2's published build by 2.2×** — 217.36 ms against 474.47 ms for the same 121 backward Dijkstras. Every ratio here is taken in-process against R4's own measurement, per R3's rule, so no conclusion moves; but two S2 tasks now publish different absolutes for the same operation and R7 owes the reconciliation.

**Four defects in R4's own harness, all caught by instruments rather than by reading.**

- **The sequenced protocol was missing DSDV's acceptance rule** — a node must *reject* an advertisement older than what it already holds, not merely prefer newer ones. Without it a poisoned node kept its odd sequence number while adopting a neighbour's stale finite cost, then advertised that stale cost under the high sequence its own poison had earned. **The first capture reported 232 seconds per edit and would have published *distance-vector loses by three orders of magnitude*.** With the rule, the same measurement is 472.53 ms. What flagged it was R2's own recorded lesson: the sequenced and unsequenced rungs were reporting near-identical relaxation counts and *identical* wrong-entry counts, and **two measurements that agree that closely are not two measurements.**

- **The poison phase was a silent no-op.** It seeded the flood with the nodes that detected the break, which correctly reject every stale claim and therefore never change and never notify anybody. In DSDV the detector *advertises*; the advertisement is the event, not something a node discovers by looking around. The phase converged in 2 rounds and 24 relaxations while leaving 16,680 of 16,697 entries wrong — **and the report read "converged: yes", because a phase that does nothing does it very quickly.**

- **The audit counted the destination itself as stranded**, one phantom per column, which read as a suspiciously round 121. A defect that produces a plausible number is worse than one that produces an absurd one.

- **The elapsed-time helper overflowed.** `elapsed × 1,000,000,000` passes `long.MaxValue` at about 9.2 seconds on a nanosecond clock, and the first capture published **−8,267.51 ms** for the rung that then took four minutes. Every earlier S2 section times loops far below that threshold, so the same expression has been correct everywhere else in this harness — **a helper is only as safe as the largest quantity anybody has yet asked it to measure.**



---

## The machine's own state during this capture

**The load averages are a point sample and the stall counters are not.** Linux's PSI `total` fields are cumulative microseconds, so the figures below are stall that happened **during this run** — which is the question, where a load average read at the top of the report would have described the minute before it started.

- **Run duration** 194.06 s — from 14:22:33 UTC to 14:25:47 UTC, **which is what makes the duration checkable rather than asserted**
- **Load average, at start** 1.56 / 1.44 / 1.34 (1 / 5 / 15 min)
- **Load average, at end** 1.74 / 1.58 / 1.41 (1 / 5 / 15 min)
- **CPU stall** 196,765 µs over the run — 0.10% of it
- **Memory stall** 0 µs over the run — 0.00% of it
- **IO stall** 60,976 µs over the run — 0.03% of it

**A run whose memory stall is a rounding error is a run the pinning actually protected.** This process was confined to processors **2,8** of 12. Pinning to one physical core stops another process stealing cycles; it does nothing about L3 eviction or DRAM bandwidth, which is S4's recorded finding about this same machine and is the exposure R1.3's absolute nanoseconds live in. This block is what lets a later reader check that rather than reason about it afterwards.
