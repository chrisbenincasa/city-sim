# 0010 — S2: the routing ceiling

> Spike S2 of [`0003-build-plan.md`](0003-build-plan.md), on the parallel track. Spike definition in
> [`06-roadmap.md` §Phase 0](../docs/06-roadmap.md). Prior art and the standing of each option in
> [`references.md` §2](../docs/references.md). Decisions under test:
> [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md),
> [`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md),
> [`adr/0020`](../docs/adr/0020-one-live-world-and-settlements-are-derived.md),
> [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md),
> [`adr/0040`](../docs/adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md).

**S2 is the project's top risk and it is the only one argument cannot close.** It decides whether the
4096² map survives, it owns the pathfinding cluster's size, and it produces the router that milestone 5c
takes as given. [`0002`](0002-open-questions.md) puts it plainly: routing and scale are on the short
list of things *validated early or not at all*.

**Risk retired.** [`adr/0020`](../docs/adr/0020-one-live-world-and-settlements-are-derived.md) makes route computation the binding
constraint on world size — *"the map-size question is the routing question in disguise"* — and the
corpus has never measured it. Cities: Skylines 2 is the named counter-example and it is a specific
one: player instrumentation traced its sim-speed collapse directly to the count of pending
pathfinding queries, after CS1 had shipped with agent caps and openly cheating re-routes. **The
failure S2 exists to catch is not "pathfinding is slow"; it is "pathfinding is slow at a load nobody
measured, on a map already committed to".**

**What it must not become: a traffic simulator.** Lane-as-entity, the IDM and Overlaps are milestone 6
and [`adr/0016`](../docs/adr/0016-the-lane-is-the-entity-not-the-car.md) already settles their shape. S2 routes
Travellers over a graph. It never simulates a vehicle.

---

## The prescribed order, and why it is not the obvious one

**Build the zone-to-zone travel-time matrix first, then measure what work is left.** This instruction
appears in four places — [`06 §Phase 0`](../docs/06-roadmap.md),
[`05` open question 1](../docs/05-technical-architecture.md),
[`references.md` §2](../docs/references.md), and [`0002`](0002-open-questions.md)'s ledger — and it
inverts what a routing spike would naturally do first.

The reason is in `references.md` and it is the single most important sentence for planning this
spike:

> *Our design already answers the many-to-many query another way — the zone-to-zone travel-time
> matrix serving the Statistical tier. If that matrix carries the choice loop (and it should; §5.8
> makes "never resolve a route inside the choice loop" a rule), then the detailed-tier router only
> handles vehicle steering, and the many-to-many argument for distance-vector largely evaporates.*

**So the headline question — HPA\* or DSDV — may substantially dissolve once the matrix exists**, and
running the comparison first would price two routers against a workload the design does not actually
have. Tasks R1 and R2 below therefore come before R3 and R4, and R2's result is allowed to make R4
unnecessary. A spike that answers a question the design stopped asking has not saved anything.

---

## Gate

**Cleared.** It was one item and it was a missing definition rather than a design argument.

**`Segment` had no entry in [`CONTEXT.md`](../CONTEXT.md)**, though the word appeared twenty times in
that file — in Fidelity, Stress, the Audit, the Microscopic Cap, Promotion and Demotion, Traveller,
the VDF and Upkeep. It is the unit the Cap counts, the unit Fidelity attaches to, the unit the VDF is
evaluated on, and the unit
[`adr/0035`](../docs/adr/0035-infrastructure-is-priced-by-what-it-consumes.md) prices Upkeep against.
S2 is the first work that must *count* Segments, so it would have had to pick between a Road Graph
edge, a run between Junctions and a Tile-length edge — **which differ by more than an order of
magnitude on the same city.** Picking it silently inside a spike is how a definition gets welded in by
a benchmark.

**Now defined, as the Road Graph edge**: one run of road between two adjacent nodes, carrying
capacity, free-flow speed, mode mask, volume and Fidelity, and owning Lanes when Microscopic. The
entry recovers the meaning the rest of the corpus already implied rather than choosing a new one — the
K0 schema's ~30,000 Segments at about four Lanes each is only consistent with the middle reading.

**What the definition did not settle, and S2 must not settle silently:** whether `volume / capacity` is
per Segment or per direction. Lanes are directional queues and a Segment carries about four of them,
so a Segment jammed inbound at the morning peak reads roughly half-loaded if the two directions are
summed — which would make Stress understate exactly when it matters and promote to Microscopic late.
Recorded in [`0002`](0002-open-questions.md); **R0 must parameterise it rather than assume it.**

**That framing is the milestone-6 half of it, and it is not why S2 cares.** The **VDF** is the
travel-time function and is *"evaluated on one Segment's own `volume / capacity`"* (`CONTEXT.md` → VDF),
so this choice sets **the cost function S2 routes on**. Three structural consequences, all of which must
be right from R0's first line because none is a local retrofit:

- **The graph is directed** — `cost(A→B) ≠ cost(B→A)` on one Segment.
- **The route cache key is an *ordered* node pair.** Under R6's key a cache treating `{u,v}` as
  unordered returns the wrong route for half its hits, silently.
- **The travel-time matrix is asymmetric**, so nothing may halve it by symmetry and R1's resident-size
  figures stand at the full *n²* either way.

**And it exposes [`adr/0020`](../docs/adr/0020-one-live-world-and-settlements-are-derived.md).** That
ADR computes a Settlement as *"a **connected component** of the District graph… **a union-find** over
data already being maintained, at effectively no cost"*, while `CONTEXT.md` → Settlement defines one as
*"a maximal set of Districts **mutually** reachable within the Commute Budget."* **Union-find computes
weak connectivity; "mutually reachable" is strong connectivity, and the two coincide only on a
symmetric matrix.** The divergence's headline case is the same one cited above — inbound within budget
at the morning peak, outbound not — so union-find would merge two Districts into a Settlement that is
not mutually reachable at all. Strongly connected components is Tarjan: still cheap, but not
`adr/0020`'s claim. **Recorded before the numbers arrive, so it cannot be reasoned around afterwards.**
**Not a gate, but a parameterisation requirement.** A District is *the granularity of the travel-time
matrix*, so S2 takes the zone count as a parameter and reports a curve — exactly what K0 did with the
unset Microscopic Cap, and the precedent is good. **S2 must not choose a zone count. It must report
against a swept one.**

*Corrected during grilling.* This paragraph previously said *"whether Districts are player-drawn or
automatic is open and `06` says it blocks milestone 5c."* **It is not open**: `02 §2.1` settles it as
***both*** — automatic by default, player-adjustable as an advanced action — and adds the finding that
matters more here, that **District extent is bounded by the pooling abstraction's own validity**, so
*"the count is physics rather than a design choice."* `06:42` still carries the closed question and is
stale. `CONTEXT.md` → District now records the working anchor — **128 Cells, 2.10 km²** — and S2's sweep
should bracket it rather than range freely.

## Prerequisites

Slices 0 and 2. S2 needs somewhere to live and a pinned SDK, and it needs the arithmetic substrate,
because a router that computes travel times in `double` measures a thing the simulation will never
run. If slice 2 has not landed, S2 may use integer milliseconds directly and record that it did —
**but it may not use floating point**, and the Q16.16 path must be measured before the verdict is
written.

It must **not** reference `Borough.Core` beyond the substrate.

---

## The scale S2 runs at, and the correction it starts from

**[`06`](../docs/06-roadmap.md) specifies "30k Travellers traversing a synthetic Road Graph". That
figure is stale and must not be used.** `06` is 🔴 — never grilled — and it predates both the 1M
target and the 4096² map. S4 task 2 derived the load from the generators instead:

| Quantity | Figure | Standing |
|---|---:|---|
| Trips per Day | **~1,900,000** | derived from commutes, school, shopping, freight |
| Trips in flight | **~56,000** | derived; band 37k–111k on mean Trip duration |
| **Legs in flight** | **~140,000** | derived, 2.5 Legs/Trip under `adr/0008` |
| Trips started per Tick | **~232** | 1.9M ÷ 8,192 — **a Day-average, and the Day has a rush hour** |
| **Leg routes wanted per Tick** | **~580** — ~232 drive, ~464 walk | 232 × 2.5 Legs/Trip under `adr/0008`. Also a Day-average |
| Segments | ~30,000 | **placeholder** — no road-density figure exists in the corpus. Road edges; walking adds none, being a mask bit |

**The router is handed Legs, not Trips, and the majority of them are walks.** Every figure in this plan
that reads *"~232 searches per Tick"* is a floor on the **drive** half only. A car commute is never
fewer than three Legs (`CONTEXT.md` → Leg), and a walk Leg needs a route for two reasons that are not
about travel time: `distance / speed` is exact only over a **network** distance, which is a search; and
whether a walk route *exists* is the whole of **Severance**, which `CONTEXT.md` calls *"the clearest
payoff of treating walking as real."* **A spike that never routes a walk cannot see the design's
flagship emergent behaviour.**

The two classes are measured separately throughout, because their cost profiles are nothing alike —
walks are short searches over a dense local subgraph at high count, drives are long searches over a
sparse global one at lower count. And walk routing is owed a **failure-mode** measurement as well as a
cost one: whether the router can distinguish **severed** from **merely far**. Those are different Trip
Fates (*no route found* against *exceeded commute budget*) and different player-facing diagnoses, and a
search-radius bound chosen for performance would silently collapse them into one — a `LEGIBLE CAUSE`
failure introduced by an optimisation.

**S2 is sized against the derived figures, and the ledger says why:** *a routing design that passes at
400k/Day and fails at 1.9M is the exact failure that spike exists to catch.* Against `06`'s 30k the
spike is undersized roughly 2× on Trips and 5× on Legs.

### The peak, which every figure above averages away

**Every load figure in the derivation is a flat mean over the Day, and the design has a rush hour.**
`02 §1.2` and `01 §7` give the sun arc five named phases — *dawn, morning peak, midday, evening peak,
night* — and the generator mix is overwhelmingly peak-bound: **79% of Trips are commutes and school
runs**, which are peak-bound by definition, against 410,000 shopping and freight Trips that are
genuinely spread. Outbound commute plus outbound school is ~750,000 Trips landing in one phase; at a
fifth of a Day that is ~458 Trips/Tick and at an eighth — and peaks are normally the narrow phases —
it is ~732. **So the real ask at peak is 2–3× every figure above**, or something like **1,200–2,300 Leg
routes per Tick** against a plan that said 232.

- **The peaking factor is swept, not chosen.** It is nearly derivable already — the generator mix says
  which Trips are peak-bound — and the only missing input is the **phase widths on the sun arc, which
  the corpus names and never sizes.** Same precedent as zone count and the Microscopic Cap: report a
  curve, do not pick a number.
- **Every S2 figure is reported at peak, with the mean as a secondary column.**
- **The 37k–111k in-flight band conflates two axes and must be re-derived on both.** It is presented as
  sensitivity to *mean Trip duration*, but 56,000 × a 2–3× peaking factor is 110,000–170,000 — so the
  top of that band is roughly the **provisional** duration figure at peak rather than the pessimistic
  duration at mean. Two independent uncertainties are wearing one range, and the range understates the
  combination. **`spike-results` is owed the correction**, because a figure known to be wrong and left
  standing in the authoritative document is the exact failure this corpus already names about the ~400k
  Trips/Day figure.
- **R2a's crossover is evaluated across the peaking sweep**, because only one side of it moves — direct
  attribution scales with vehicles in flight and is peak-sensitive; aggregate scales with
  `zone count² × route length` and is not. **Report the peaking factor at which the crossover inverts.**
  That single number decides more than either scheme's absolute cost, and the crossover already landed
  near a ~100-Tick congestion cycle on mean figures, so a 2–3× shift on one axis is likely to reverse it
  rather than refine it.

**The ~400k Trips/Day figure still stands uncorrected in [`05`](../docs/05-technical-architecture.md)
and in this ledger's item #20.** It is one Trip per Household per Day — the outbound commute with the
journey home never counted. Striking it is owed and is not S2's job, but S2 must not read it.

---

## Where the code lives, and that it dies

```
spikes/S2.Routing/          console project, added to Borough.slnx for convenience
                            references the arithmetic substrate and nothing else of ours
docs/spike-results.md       the numbers, appended alongside S4's
```

Same discipline as S4: the last task deletes `spikes/S2.Routing/` and records the deleting commit's
**parent** in `docs/spike-results.md`, so the harness stays recoverable and is not in the way.

**S4's lesson applies before the first line is written.** Every figure divides by a denominator, that
denominator has a machine and a moment, and **the capture must state both truthfully** — S4's M4 Pro
capture claimed a sitting it did not share, and the claim went unchecked because nothing printed the
denominator's own timestamp beside the figure. S2's harness prints it.

---

## Tasks

### R0. The synthetic Road Graph, and the denominator — **DONE**

No routing number means anything without a graph that resembles the real one and a plain-search cost
to divide by.

> **Done. Numbers and the decision each produced in [`spike-results`](../docs/spike-results.md) §S2;
> raw capture in `spikes/S2.Routing/results/s2-r0-intel-core-i5-10400-ddr2133-powersave-turbo-unpinned-20260806T152301Z.md`.** Headlines, because three of them change
> a later task in this plan rather than merely closing this one:
>
> - **The ~30,000-Segment placeholder is one Street per Cell boundary**, and at that density the mean
>   Segment is 128 m — so `CONTEXT.md` → Segment's *"~30,000 Segments"* and its *"roughly a
>   block-length link"* turn out to be the same statement. **The density debt is not discharged**: the
>   quantity that decides is 16.20 km of road per km², and that needs a source rather than a sweep.
> - **The Road Graph is not a memory constraint** — 2.0 MiB against K0's 172.3 MiB for the whole world.
> - **The `(Segment, offset)` query shape is free**: ~250 ns of bootstrap against a 418 µs search. The
>   plan was right that a node-to-node denominator measures the wrong query; it turns out the right one
>   costs nothing to measure.
> - **Admissibility breaks at the *first* Arterial.** This plan anticipated that *"deliberately rare"*
>   would make the tight metrics safe *almost* always and called that the trap. It is worse: there is no
>   low-Arterial regime in which Manhattan is safe, only one in which it is wrong less often — 4% of
>   drives with two Arterials on the map. Octile fails too, an order of magnitude less often, which is
>   the worst property a defect can have.
> - **`Chebyshev` is the heuristic and the denominator.** Not the tightest safe rung — see R3 below for
>   why that matters to this plan and not just to R0.
> - **The graph is directed, mode masks are on the arc, and volume scope is a parameter**, all as the
>   gate section requires. Per-direction volume costs 5% of the graph at every rung.

- Generate a 4096² graph: grid-snapped Streets falling out of the Tile grid, freeform Arterials with
  authored Junction pieces, **one graph with mode masks** rather than two networks
  (`CONTEXT.md` → Road Graph, and `03 §3.7`). Mode masks are not optional decoration here — a
  multi-Leg Trip is routed by a single mode-aware search, and a router measured without them is
  measuring the wrong search.
- Parameterise Segment count and report footprint as a curve, as K0 did for the Microscopic Cap. The
  ~30,000 figure is a placeholder and S2 is the first thing able to say what road density a 268 km²
  city at 1M actually implies.
- **The denominator: one uncached point-to-point search** — A\*, integer costs, no hierarchy and no
  cache — over the O-D distribution R1 derives. Every later figure is reported against this, never
  against a Tick budget.
- **The cost function is time, and R0 states it rather than leaving it silent.** `02 §5.9`: *"the cost
  function used for routing must be the same quantity used to judge trip failure, and the same quantity
  shown to the player. SC4 routed on distance while the player was scored on time, and the traffic
  system became unlearnable as a result."* The **Commute Budget** is drawn as a wedge on the sun arc
  (`01 §7`), so the quantity is time. **A denominator that routes on distance is measuring the SC4
  failure with a stopwatch.**
- **The heuristic is a swept ladder, not a choice — and it needs no square root.** The plan previously
  said *Euclidean*, which the substrate cannot express: `Borough.Core.Arithmetic` has `Abs`,
  `FloorDiv`, `CeilDiv`, `RoundDiv`, the shifts, `Fixed.Mul`/`Div`/`Lerp` and the tabulated
  `exp`/`log`, and **no `Sqrt` anywhere**. It does not need one: admissibility requires only a *lower
  bound* on the true distance, so an underestimating integer approximation suffices and it lives in the
  spike, which dies. **Nothing enters the substrate.**

  A distance heuristic on a **time**-cost graph is admissible only when divided by the map's maximum
  free-flow speed — and since Arterials are the fast edges on a network that is mostly slow Streets,
  that division makes the tightest-looking heuristic nearly uninformative where most of the graph is.
  Worse, the tight grid metrics are not safely admissible at all:

  | Heuristic | Admissible on | Needs sqrt |
  |---|---|---|
  | **Chebyshev** `max(\|dx\|,\|dy\|)` | any graph — a lower bound on Euclidean by construction | no |
  | **Euclidean, underestimating integer approximation** | any graph | no |
  | **Octile** | 8-connected only — a freeform Arterial breaks it | no |
  | **Manhattan** | 4-connected only — a freeform Arterial breaks it | no |

  `adr/0014` snaps Streets to the Tile grid, so Manhattan is exact there; **Arterials are freeform
  splines at arbitrary angles, so a diagonal Arterial makes the true distance shorter than Manhattan,
  the heuristic overestimates, and A\* silently returns a non-optimal path.** `CONTEXT.md` → Arterial
  calls them *"deliberately rare"*, which makes the tight metrics admissible *almost* always — and
  *almost always* is the trap, because **a non-optimal path is a different Trip and therefore a
  different city.**

  Measure all four, and report per rung: nodes expanded, path cost, and — **the column that decides** —
  **how often the returned path is not optimal against Dijkstra ground truth on the same query.** The
  verdict S2 owes is **the Arterial density at which admissibility breaks**, which is a decision
  argument cannot reach and one function swap can.
- **Report the denominator's own quality, because every ratio in this spike divides by it.** Nodes
  expanded against final path length, and the ratio against plain Dijkstra on the same query. If A\*
  expands within a few percent of Dijkstra, **S2 says so beside every ratio it publishes** — a weak
  denominator flatters HPA\*'s speedup, the cache's value and R2's crossover alike. S4's lesson was
  that a denominator must state its machine and its moment truthfully; this is the same lesson one
  level up, about its *quality*.
- **If the heuristic proves too weak, the fix is not to make it inadmissible.** An inadmissible
  heuristic returns a different path, and under `05 §4`'s test a changed result is a **design change**,
  never a tuning knob. Written here so it cannot be reasoned around once the numbers are inconvenient.
- **The denominator's query shape is `(Segment, offset) → (Segment, offset)`, not node to node**
  (`CONTEXT.md` → Access Point). An Access Point is an offset along a Segment because five Buildings
  share one at the working figures, and promoting them to nodes would put the graph at 150,000–300,000
  edges rather than ~30,000. So the search seeds its open set with **both endpoints of the origin
  Segment** at their partial costs and terminates on either endpoint of the goal Segment plus the
  offset remainder. **Report the bootstrap and termination cost separately from the search**, so later
  tasks can tell fixed overhead from work — a node-to-node denominator measures a query the game never
  issues, and every figure in this spike divides by it.
- Record machine, SDK, governor and the denominator's own timestamp.

### R1. The travel-time matrix — the prescribed first measurement — **DONE**

> **Done. Numbers and the decision each produced in [`spike-results`](../docs/spike-results.md) §S2;
> raw capture in `spikes/S2.Routing/results/`.** Headlines, because four of them change a later task
> in this plan rather than merely closing this one:
>
> - **The matrix carries the choice loop, and the tripwire does not fire at any District count.** The
>   scattered read tops out at **5.00 ns at 4,096 Districts** against S4's K2 gather at 13.66 ns, and
>   costs **1.21 ns** at the working anchor. `02 §5.8`'s *never resolve a route inside the choice
>   loop* is enforceable. **This is the finding R4 was made conditional on** — see R2.
> - **The volume-scope question and the `adr/0020` exposure are the same question**, which this plan
>   filed as two. Under per-Segment volume the matrix is symmetric **to the bit** and union-find is
>   correct by construction; under per-direction it is not. R0 priced the scope at 5% of the graph and
>   said *"what it buys is not visible until R2 has volume to attribute"* — what it buys is visible
>   here, and it is the asymmetry itself.
> - **The `adr/0020` exposure is real and it is a band rather than a threshold.** Union-find and
>   Tarjan return **6 Settlements against 8** at a tight Commute Budget, and the one-way pair count
>   rises and then falls — so no generous Budget closes the gap, it only moves past it.
> - **A dirty-region rebuild is unsound, and the sound version collapses into a full rebuild.** The
>   spatial test misses **309 of 429** changed entries on a central edit. The route-crossing test
>   identifies the changed set almost exactly — 430 entries against 429 — but touches **all 121 rows**,
>   because a one-to-all fills a row and every row holds the entry addressed *to* the edited District.
> - **Two things this plan did not ask for and should have.** The matrix's own **error** against a
>   true `(Segment, offset)` query is **11.32%** at the anchor, and **time resolution** is a
>   hash-bearing decision the corpus has never named — see decisions 8 and 9 below.

**Terminology, corrected by this task.** This section said *zone* and *zone count* throughout, and
`CONTEXT.md` → Zone is **a permission set over land** — what a player may build there — while
`CONTEXT.md` → District is what was actually being swept: *"the granularity of the travel-time
matrix"*. The banned-terms section makes the same assignment from the other side, sending *region* to
*District for a Goods-pooling region*. **The corpus is inconsistent about this beyond this plan** —
`05 §422` and `references.md §2` both say *"zone-to-zone travel-time matrix"* — and that is filed as
documentation debt rather than silently rewritten, because `plans/0010` quotes `references.md`
verbatim and a corrected quote is a broken one. R1's code and report say District.

- Build the District-to-District matrix. **Sweep District count** rather than choosing one; the corpus's only
  figure is ~100–400 zones from [`plans/0001`](0001-foundational-design.md), which predates the 1M
  target and cannot be carried forward unexamined. Bracket `CONTEXT.md` → District's working anchor of
  128 Cells rather than ranging freely.
  > **Swept 16 → 4,096 Districts, Cell-aligned.** The anchor lands at 11 a side — 121 Districts, 135
  > Cells, 2.21 km² — and inside `plans/0001`'s 100–400, which is arithmetic rather than
  > corroboration.
- **Sweep the matrix's time resolution, which is a second unstated axis.** A single Day-average matrix
  cannot represent the peak that every other figure in this spike is now measured at — morning inbound
  and evening outbound cancel, and the asymmetry the directed graph exists to carry vanishes into the
  mean. A per-phase matrix (the sun arc's five phases) multiplies resident size by five and gives the
  choice loop a travel time that matches the moment being asked about. **Report cost and size against
  both**, because the answer interacts with the peaking factor and with the `adr/0020` exposure below.
  > **The Day average reports 1 one-way District pair where the morning peak has 76**, at five times
  > the resident size and five times the build for the per-phase alternative. The asymmetry does
  > vanish into the mean exactly as anticipated. **And the axis turns out to be hash-bearing** — see
  > decision 9.
- **Report the matrix's asymmetry as a measured quantity** — the distribution of
  `|matrix[i][j] − matrix[j][i]|` at peak. **This is what settles the `adr/0020` exposure**: if the
  asymmetry is negligible, that ADR's union-find survives on evidence rather than on assumption; if it
  is not, Settlements need strongly connected components and `adr/0020` is owed an amendment.
  > **`adr/0020` is owed the amendment.** At a tight Commute Budget union-find returns **6
  > Settlements against Tarjan's 8**, largest component 90 against 70. But the distribution alone
  > would not have settled it, and this bullet asked for the wrong instrument: an asymmetry figure is
  > a number about travel times, while a Settlement is an object the game is made of. **The count is
  > the test**, and the one-way pair count beside it is the exposure in one number.
- Measure four things separately, because they size different decisions: **cold build cost**,
  **incremental rebuild against a dirty region**, **resident size**, and **the O(1) read** the choice
  loop performs.
  > **All four are about cost and none asks how wrong an entry is**, which R1 added as a fifth — see
  > decision 8. Cold build is linear in District count by construction, at ~1.47 ms per one-to-all
  > search; three successive shared warm-ups each made it look sublinear, which is the shape a reader
  > would most readily believe.
- **Resident size is measured twice** — once for the scalar matrix the choice loop reads, and once for
  the **cached District-pair routes** `03 §3.3` distributes volume along. They differ by more than a
  constant factor: *n²* integers against *n²* variable-length Segment sequences.
- The read is the one that matters most. `02 §5.8` makes *never resolve a route inside the choice
  loop* a rule, named as the one thing UrbanSim gets architecturally right that this design must not
  violate. **If the matrix read is not cheap, that rule is unenforceable** and the finding is larger
  than S2.
- **The risk is cache, not complexity, and the tripwire row was rewritten to say so.** A lookup into an
  *n*×*n* array is O(1) by construction, so the original wire — *"not O(1) and cheap"* — could not fire
  on any plausible implementation, which is the same effect as a wire reasoned around, arrived at
  earlier. What actually binds is where the matrix lives:

  | Zones | Entries | At 4 B | Where it lives |
  |---:|---:|---:|---|
  | 100 | 10,000 | 40 KB | L2 |
  | 400 | 160,000 | 640 KB | L2/L3 |
  | 2,000 | 4,000,000 | 16 MB | **DRAM — every read is a miss** |

  **So District count has a hard ceiling set by L3 rather than by memory**, and `plans/0001`'s only
  figure — 100–400 — sits near its edge. **Report the District count at which the matrix leaves L3.**
  That figure is the practical ceiling on District count and it reaches past S2: a player drawing
  thousands of Districts would be drawing a performance cliff, which makes District count not a free
  UI decision.
  > **The cliff is real, is visible at 2,025 Districts, and never reaches the tripwire.** Scattered
  > reads go 1.64 ns at 4 MiB → 2.88 at 15.6 MiB → 5.00 at 64 MiB, against K2's 13.66. **So L3 is not
  > the binding ceiling on District count.** The *route store* is, at 4.06 GiB against the whole
  > world's 172.3 MiB — and the entry error is what argues the other way.
- **Measure the read in both access patterns, because the corpus uses both phrasings for one
  operation.** `references.md §2` describes the choice loop as *"what is the commute from this
  candidate dwelling to any job?"* — one origin, many destinations, a **row scan**, sequential and
  largely immune to the table above — and in the same sentence as *"many-to-many, evaluated tens of
  thousands of times per cycle"*, which reads as scattered. **The two differ by an order of magnitude at
  2,000 zones and are indistinguishable at 100.** Report them separately, so which one the choice loop
  performs becomes a design question with a priced answer rather than a detail settled by whoever writes
  the loop.

*Decides:* whether the matrix can carry the choice loop, and therefore whether R4 is worth running.

> **It can. R4's premise now rests on R2 alone**, which is where the remaining half of the argument
> lives: if Statistical Trips need no concrete path, the many-to-many case for distance-vector has
> evaporated and R4 is written up as *not run, and why*.

### R2. Two axes, not one — and the crossover nobody has looked for — **DONE**

**This is the task the prescribed order exists to reach, and grilling found that the corpus had
already answered it twice, differently, in the same document.**

> **Done. Numbers and the decision each produced in [`spike-results`](../docs/spike-results.md) §S2 R2;
> raw capture in `spikes/S2.Routing/results/`.** Headlines, because three of them change a later task
> in this plan rather than merely closing this one:
>
> - **The path source has three rungs, not two, and the third is R4's router.** `adr/0041` requires a
>   Traveller to increment the Segment it *enters* every Tick — so what it needs is a **next Segment**,
>   not a path, and a next-hop table supplies one while storing no path at all. **R4's condition below
>   is therefore false, and false in DSDV's favour** — see the correction in R4 itself.
> - **The searched rung is out on arithmetic**: 716,800 ns per Leg against ~550 arrivals per Tick, or
>   ~400 ms of searching per 15.6 ms Tick. The harness could not afford it either and had to pool.
> - **`adr/0041`'s *"no correctness content"* is wrong on two counts**, and the second is the larger.
>   Statistically, a shared route costs **36.01%** mean detour and a next-hop table **18.52%** — the
>   structural difference between coarse-at-both-ends and coarse-at-one-end is worth almost exactly
>   half. Structurally, **every Trip into a District arrives through its one representative node**, so
>   that node's Stress is an artefact of the partition rather than a property of the city.
> - **Direct attribution is the *cheaper* scheme below a 105-Tick congestion cycle**, an order of
>   magnitude past `adr/0041`'s estimate of ~10. The ADR's *"we are knowingly paying for correctness"*
>   understates its own case.
> - **The aggregate scheme does not lag the jam; it misses it.** *Never* at every cycle including one
>   Tick, where no cadence is left to blame, and the smear deposits **0.00%** on a Segment direct
>   reports at 108%. `03 §3.3` filed a *timing* defect and compensated with force-promotion; it is a
>   **place** defect and no cadence fixes a place.
> - **The crossing rate is 0.79–0.83 per vehicle per Tick, not 1.0** — `adr/0041`'s own revisit
>   trigger, discharged in the direction the trigger did not anticipate.

Stress is `volume / capacity` (`CONTEXT.md` → Stress), so every Segment needs a volume, and something
must decide which Segments a Trip deposits volume on. `03 §3.3` answers one way —
`in_flight[origin_District][dest_District]`, *"distributed onto segments along cached District-pair
routes each congestion cycle"*. `03 §3.6` answers another — a Traveller *"animated from its trip,
position interpolated along its route"*. **Those are two different routes per Trip, and nothing in the
corpus reconciles them.**

> **The attribution axis is now settled by
> [`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) —
> direct, by the Traveller.** R2 no longer chooses between the two; it **prices** the one that was
> chosen, and R2a's crossover is what says how much correctness cost. The **path source** axis below is
> still fully open and is what R2 decides.

**The mistake in the original framing of this task was treating it as one choice. It is two, and they
are independent:**

| | **Path source** — where a Trip's Segment sequence comes from | **Attribution** — how a Segment gets its volume |
|---|---|---|
| Options | **searched** per Trip, or **shared** per O-D pair and cached | **direct** — the Traveller increments each Segment it enters and decrements on exit — or **aggregate** — counts smeared along cached routes once per congestion cycle |
| Cost scales with | Trips started per Tick (~232), less cache hits | direct: **vehicles in flight**. aggregate: **zone count² × route length** |

`03 §3.3` picks *(shared, aggregate)*. The **hybrid *(shared, direct)* has never been considered and may
dominate both**: one cached route per O-D pair keeps routing cheap, while the Traveller traversing it
increments the Segments it actually drives, which makes volume exact every Tick.

#### R2a. The crossover

The two attribution schemes scale on **independent axes**, so there is a crossover and S2 can find it
rather than assume it. First-cut arithmetic, which the spike replaces with measurement:

- **Direct** — a Tick is ~10.5 in-world seconds and a Segment is roughly a block, so a vehicle crosses
  **about one Segment per Tick**: order 80,000 increment/decrement pairs per Tick into a 30,000-entry
  array of 120 KB, which sits in L2. **Independent of zone count.**
- **Aggregate** — at 400 Districts, 160,000 pairs × ~50 Segments per route ≈ **8M writes per congestion
  cycle**. **Independent of population.**

Equal at a congestion cycle of roughly 100 Ticks — which is close enough to the design's plausible
operating point that neither scheme is obviously the cheap one.

- **Sweep zone count jointly with R1**, and sweep vehicles in flight across the derived band
  (37k–111k). Report the surface and mark where the design actually sits.
- **S4's K2 is a head start.** Random gather by generational handle is the direct scheme's inner loop
  and it is already measured on two machines.

#### R2b. The attribution error, which is a correctness axis and not a speed one

`03 §3.3` confesses the aggregate scheme's defect in its own text:

> *"a jam propagates backward at roughly 15 km/h — faster than any cycle worth running — so a
> cycle-driven region always lags the jam during exactly the event it exists to capture."*

That admission is why `03 §3.3` had to invent a **second trigger** — force-promotion on downstream
blocking — as compensation. **Under direct attribution volume is exact every Tick and the lag has
nowhere to live**, which raises the possibility that a whole mechanism exists to patch a compression
nobody priced.

- Drive a jam through the synthetic graph and report **the lag** — Ticks between a Segment's true
  `volume/capacity` crossing `T_high` and the scheme reporting it — for both schemes.
- Report **peak Segment volume** under each on the same O-D distribution. A scheme that understates the
  peak promotes late, and `adr/0007` demotes on a *lower* threshold, so an understated peak also
  demotes early.
- **A scheme that cannot report a jam within the cycle it happens in has failed a design commitment**,
  in the same way a candidate needing a global flush has — regardless of its throughput.
- **Ask what else in the pipeline rounds in the same direction as the error being measured.** R0 found
  this the expensive way: its heuristic multiplies by a floored reciprocal rather than dividing, to
  remove four hardware divisions per node, and that optimisation's ~2-in-10,000 slack **partially
  cancels an overestimating metric's error** — moving walking `Manhattan` from 35 of 300 non-optimal to
  4 of 300 while leaving driving at 13. An implementation detail chosen purely for speed made an unsafe
  result look safer. R2b's lag and R5's hit rate are both error rates measured through a pipeline, and
  **a rate that moves with an unrelated optimisation is not evidence.**

*Decides:* the real per-Tick routing load, whether R3 and R4 are answering a live question, and three
things that are not S2's but which S2 is the only thing able to price:

- whether **force-promotion** (`03 §3.3`) is a necessary trigger or a patch on the aggregate scheme;
- whether `03 §3.4`'s self-correcting circularity — *"the load-bearing assumption of the whole
  scheme"* — holds, since under *(·, aggregate)* a Traveller experiences congestion on one route and
  contributes it to another, and the failure stops feeding the detector;
- whether **a player-drawn District can change the State Hash**. Under `03 §3.3` the District pair keys
  both the counter and the cached route, so redrawing a boundary changes Fidelity and therefore the
  city — the defect class `03 §3.9` rejects for the Microscopic Cap in words that transfer unchanged.
  Under direct attribution the District stops being a routing object and the defect cannot arise.

> **Answered, in order.** The per-Tick routing load is **139,437 ns of attribution at the derived
> 56,000 in flight** and a path-source read of 24–32 ns per crossing; R3 and R4 are both live and R4
> more so than before. **Force-promotion is a patch** — its lag argument is gone under `adr/0041` and
> R2b shows the compression it was compensating for is a *place* error rather than a timing one, so
> the patch never addressed it. **The circularity holds under direct attribution and is broken under
> aggregate by construction rather than by degree**, which R2b measured as a smear depositing 0.00% on
> a Segment carrying 108%. And **the District-changes-the-hash defect cannot arise for volume** —
> `adr/0041` removed it — **but it reappears through the path source**: a shared route and a next-hop
> tree are both keyed by District, so redrawing a boundary changes which Segments a Traveller drives,
> which is a different Trip and therefore a different city. *That is a new finding and it is filed as
> decision 10 below.*

### R3. HPA\*, and the cluster size it owns — **DONE**

[`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md) makes a claim S2 must test rather than
inherit: the Road Graph *"arrives pre-partitioned, because the Chunk grid is already the pathfinding
cluster, which is most of what HPA\* wants handed to it."* If true, HPA\*'s usual preprocessing cost
is largely already paid.

- Build HPA\* over the cluster grid. **Sweep cluster size**, expressed as a whole number of Chunks per
  [`adr/0040`](../docs/adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md).
  `05 §5`'s role table says pathfinding wants *larger, and loudly* at 32×32, where a cluster is 128 m on
  a map whose commutes cross tens of kilometres.
- Measure per size: preprocessing cost, per-query cost against R0's denominator, resident size, and
  **invalidation cost on a single edit** — the abstract graph's repair, not a rebuild.
- **Report wall-clock, not expansions saved. R0 measured a case where the two disagree**, and it is an
  amendment to this plan rather than a note on R0: `EuclideanFloor` expands 11% fewer nodes than
  `Chebyshev` and takes 1.8× as long, and against plain Dijkstra it cuts expansions by 55% while being
  no faster at all — the exact integer square root costs more than the expansions it saves. **A
  hierarchy that saves expansions has not yet saved anything**, and *nodes expanded* is the currency
  HPA\* results are conventionally quoted in. R6's cache inherits the same instruction.
- **A same-Segment and adjacent-Segment bypass is mandatory, not an optimisation.** With five Buildings
  on a Segment, a meaningful share of the ~464 walk Leg routes per Tick never leave their own Segment or
  its neighbour, and routing those through the abstract graph costs more than the answer. Measure the
  share that takes the bypass — it is also the first real figure on how local walk Legs actually are.
- Report the render-streaming side of the Chunk trade as *not measured here*, and note that under
  `adr/0040` this **no longer matters**. S2 decides the cluster and only informs the Chunk, so it never
  needed the side of the trade it cannot see.

*Decides:* **cluster size, outright.** It is `(derived AND rebuilt)`, never written to a save, and
therefore **free to change forever** — which is the whole of `adr/0040`. Chunk size is *informed*, not
decided, and stays on the *cannot be retrofitted* list for the reasons that are genuinely its own:
rendering, saves and work partitioning.

> **Answered, one clause of the task's own charter is answered *no*, and a later task is promoted.**
> **The cluster is 8 or 16 Chunks a side, the bias on 16, and R3 cannot close it** — *Decides: cluster
> size, outright* above is owed that correction. The axis that separates the two rungs is the **edit
> rate**, and that is **R5's**. The bias is on 16 because it is **1.31× faster on the refined query**,
> the column with a customer, and pays for it with **0.92 ms more per deleted Segment** — a per-Tick
> cost against a per-click one, both under 1.3 ms. A drag that deletes hundreds of Segments in one
> gesture could still overturn it, which is exactly R5's storm. And `adr/0014`'s *"the Chunk grid is
> already the pathfinding cluster"* is **measured false by 16× in side and 256× in area**: at one Chunk
> the abstract graph *is* the Road Graph, 16,694 portals against 16,697 nodes, and the query expands
> exactly the 4,138 nodes the flat search expands. `05 §5`'s *larger, and loudly* was right.
>
> **No cluster size fits routing into the Tick budget, and R6 is promoted because of it.** The
> recommended rung refines a route in **181,554 ns**, so **85 Trips may start per Tick** before routing
> owns the whole 15.6 ms — a figure stated as a break-even rather than as *6.4× over budget* precisely
> because the arrival rate is a guess and S2 cannot measure it. The load is U-shaped in cluster size
> and pinned at both ends (small → the abstract search approaches flat; large → the *insertion*
> approaches flat), so **this is a floor rather than a rung that was missed.** The two exits are a
> **cache** and **eight cores**. **R6 is therefore load-bearing, not a late tidy-up**, and R4's
> comparison runs knowing whichever router wins will need it.
>
> **The plan's *current standing favours HPA\** is weakened rather than confirmed.** HPA\* buys
> **3.08×** on a cost-only query and **2.63×** when it must return arcs — and R1 already showed the
> matrix answers the cost-only question at 1.14 ns, so the larger number is against a customer that
> has a better answer already. **R4 now runs against an open comparison.**
>
> **Three findings the plan did not ask for and one it did.** The **transitive reduction of the
> intra-cluster edges is mandatory and lossless** — 133,816 abstract edges to 11,768, mean degree 40 to
> 3, double the speedup, 100% optimal throughout — and an implementation that skips it measures a
> hierarchy barely faster than none. **Storing each intra-edge's arcs is mandatory alongside it**: it
> turns refinement from a re-run confined search into an array copy, moves the refined query from 1.50×
> to **2.63×**, and costs **223.92 KiB** because the reduction had already removed 91% of the edges
> that would have carried arcs — *R6's question answered early for the intra-edge half*. **Botea's
> transition sampling is out**: one transition per boundary buys 8.53× and returns routes **80.49%**
> longer on average, which is a different city under `05 §4`. And the reduction **costs repairability**
> — redundancy is a property of the costs, so a reduced cluster's edge set is **decided again rather
> than re-costed**, at a measured **1,296,680 ns** per deleted Segment at 16 Chunks. **R5 weighs that,
> because the weight is the edit rate.**
>
> **R0's amendment landed for a second time and in a third place.** The hierarchy expands 4.7× fewer
> nodes and is 1.43× faster unreduced, because a road network is degree-3 and the complete abstraction
> is degree-40 — *a hierarchy that saves expansions has not yet saved anything*. And **the denominator
> itself carried the artefact this time**: measured first in the process it read 1,401,307 ns against
> 477,609 ns measured last, so the harness now measures it twice and publishes both. **A denominator
> measured once has no error bar, and a denominator measured first has a systematic one.**
>
> **The O-D draw is uniform and R0 flagged that as a placeholder that was never replaced.** R0 said it
> would take R1's distribution; R1 produced none. A uniform draw over 4,096 Tiles produces long routes,
> and long routes are where a hierarchy wins widest, so **every speedup above is an upper bound.** It
> does not move the optimality counts or the ranking of the rungs against each other.
>
> **The bypass stays mandatory on cost and its stated reason is unconfirmed.** It is worth 78.28%
> inside one block and 1.75% at two, so the plan's claim holds if and only if walk Legs are
> overwhelmingly single-block — which the corpus has never said and S2 cannot measure.

### R4. DSDV distance-vector, if R2 leaves it live — **DONE**

> **Done. Numbers and the decision each produced in [`spike-results`](../docs/spike-results.md) §S2 R4;
> raw capture in `spikes/S2.Routing/results/s2-r4-intel-core-i5-10400-ddr2133-*.md`.** Headlines, because four of them change a
> later task in this plan rather than merely closing this one:
>
> - **Distance-vector is out, and on none of the three grounds this plan anticipated.** Not memory —
>   at District granularity the table is **23.12 MiB** against a 172.27 MiB world and **the tripwire
>   below does not fire**. Not correctness — with sequence numbers it converges to *exactly* the
>   rebuilt table, on a deleted Segment and on a severance alike. **It is out because it costs more
>   than the rebuild it exists to avoid**: 500.69 ms against 234.74 ms for one deleted Segment,
>   **2.13× slower**, and 106× more than a scheme this plan never named.
> - **The reason is structural and no tuning recovers it.** An odd-sequence unreachability claim
>   outranks every finite route in circulation *by construction* — which is exactly what stops
>   count-to-infinity — so only a **newer even** number from the destination itself can restore a
>   route. One broken link therefore obliges the destination to **re-flood its whole tree**. *The
>   property that makes deletion safe is the property that makes deletion expensive.*
> - **The scheme that wins was not on the ballot: dynamic subtree repair**, at **4.71 ms** against a
>   234.74 ms rebuild with **0 entries wrong**, and it converges on a severance too. It is not
>   distance-vector, needs no sequence numbers and no Epoch, and was measured only because pricing
>   solely the candidate a plan names is how a spike produces a verdict it has not earned.
>   **R5's ladder is owed this rung.**
> - **`references.md`'s sequence-number claim is confirmed by measurement** — 1,620× the work and
>   still 16,684 of 16,697 entries wrong without them. Under `adr/0043` it had been an argument.
> - **And the finding R4 was not looking for, which is the largest one.** R2's **18.52%** mean detour
>   was measured on the uniform draw, which R4.1 shows is the **longest-trip distribution available**
>   at 8.53 km mean on a 16.4 km map. Aiming a Traveller at a District representative is a roughly
>   fixed error charged against a shrinking journey, so the detour rises to **36.04%**, **62.02%** and
>   **128.82%** as trips shorten. **A Traveller driving more than twice as far as it should is a
>   different city under `05 §4`.** This does not decide against the table; it says the table's
>   **granularity** is the open question, and it is R2's decision 11 arriving from the other side.
> - **The O-D draw is no longer a silent placeholder.** R4.1 replaces it with a **swept family** —
>   uniform, distance-decay at three lengths, monocentric — with uniform as a rung of the same
>   sampler so a difference between rows cannot be the machinery. **R5 and R6 inherit it**, which is
>   what makes R6 runnable at all.
> - **Congestion drift is priced and the break-even is between 1% and 10% of arcs moved.** Below it
>   incremental repair wins; above it a plain **rebuild** wins outright. **So the matrix refresh
>   cadence chooses the maintenance scheme** — a decision this plan files as tuning, which it is not.
> - **A full rebuild is affordable as a rotation and not as an edit response**: 10.78% of one Tick for
>   a full pass every 121 Ticks. *Drift wants a cadence; the core verb is an event.*
>
> **Four defects in R4's own harness**, all caught by instruments rather than by reading, and one of
> them nearly cost a verdict: the sequenced protocol was missing DSDV's **acceptance rule**, and the
> first capture read **232 seconds** per edit — it would have published *distance-vector loses by
> three orders of magnitude*. What flagged it was R2's own recorded lesson, that **two measurements
> agreeing that closely are not two measurements**. Also a poison phase that was a silent no-op and
> reported *converged: yes*, an audit that counted the destination itself as stranded, and an
> elapsed-time helper that **overflowed past 9.2 seconds** and published −8,267.51 ms.
>
> **R3's denominator finding reproduces a third time and reconciles R2 on the way past.** The same
> 121 backward Dijkstras read **423.47 ms** measured first in the process and **234.74 ms** measured
> later — 1.80× — which substantially explains R2's 474.47 ms for the same operation. R7 owes the
> reconciliation.

#### The task as it was written, before the numbers


~~**Conditional on R2.** If the matrix carries the choice loop and Statistical Trips need no concrete
path, the many-to-many argument for distance-vector has evaporated and this task is written up as
*not run, and why* rather than skipped silently.~~

**The condition has two clauses and they resolved in opposite directions. R7 must not apply it as
written.** R1 settled the first — the matrix does carry the choice loop. **The second is false**, and
it was written before
[`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md): that
ADR requires a vehicular Traveller to increment the Segment it *enters*, every Tick, so a Statistical
Trip needs a **next Segment** continuously. A path is one way to supply that; a **next-hop table** is
another, and it stores no path at all. R2 built and measured that rung — 7.70 MiB at the anchor,
**18.52%** mean detour against a shared route's 36.01%, 32 ns per crossing — and it is
distance-vector's data structure arrived at from the attribution side rather than the routing side.

**So the clause does not merely fail to retire R4; it fails in DSDV's favour.** R2 does not settle R4
with that, because R4's own subject is **convergence after an edit**, which R2 does not touch — and
R5's edit storm is where the two survivors are actually separated. What R2 changes is that R4 must
run.

If it is live:

- **Sequence numbers are non-negotiable.** `references.md` is explicit: *if we adopt distance-vector
  routing, we take DSDV's version, not Citybound's.* Citybound's entries carry `distance`,
  `distance_hops`, `outgoing_idx` and `learned_from` and **no sequence numbers**, so link deletion
  count-to-infinities; its `routing_timeout` and `forget_routes` are ad-hoc mitigations. The reason
  this matters here and not in a network: *in a normal network, link deletion is a rare fault. In a
  city builder it is the core verb.*
- Measure: convergence time after an edit, per-query cost after convergence, **resident size** — a
  table per node per destination is the frightening axis and the one most likely to fail `adr/0006` —
  and behaviour under R5's edit storm.

*Decides:* the router, jointly with R3. **Current standing favours HPA\***, for two reasons that came
from reading rather than first principles: the grid already supplies the regular tiling HPA\*'s
clusters assume, and distance-vector without sequence numbers imports a failure triggered by the
game's most common player action. **PRA\* is out** and was rejected structurally — its abstraction is
derived from connectivity, so a topology edit changes the partition.

> **Both standings are now measured, and the second one held for a reason nobody had checked.**
> Distance-vector's failure without sequence numbers is real — 1,620× the work and still wrong — but
> *with* them the protocol is correct and simply too expensive, so the reading was right about the
> hazard and wrong about which property kills it. **What R4 does not do is decide the router**, and
> it is worth being explicit that the task's *Decides* line above overstates what it could reach: R4
> settled **maintenance**, and maintenance turns out to be a separable question from **path source**.
> Dynamic subtree repair maintains a next-hop table, and it would maintain HPA\*'s abstract graph on
> the same argument. **R5 and R6 still own the router.**

### R5. The edit storm — the test the city actually imposes — **R5.1–R5.4 DONE, R5.5–R5.6 open**

> **What is done, and what it moved.** R5.1 measured the *gesture* rather than the edit — the unit R3
> and R4 both said they could not reach. R5.2 priced repairing one, and R5.3 ran the Epoch ladder.
> Numbers in [`spike-results`](../docs/spike-results.md) → *S2 R5*.
>
> - **The tripwire fires.** *Either router needs a global flush on a Road Graph edit → out on a
>   design commitment.* A single counter **is** a global flush, and it now has a number too: against
>   a no-edit ceiling of 71.63%, **per-Segment retains 96% of it under a continuous storm and global
>   retains 9%.**
> - **The ladder's framing was wrong and that is a finding.** This plan frames it as *hit rate
>   against revalidation cost*. Per-Segment costs **42 words a lookup against global's 0.71 and has a
>   lower mean Tick at every edit rate**, because the searches its precision avoids cost far more
>   than the words it reads. **No rung here trades accuracy for speed.**
> - **Cluster size closes against R3's bias.** 8 Chunks is ~2× cheaper than 16 on a coalesced
>   256-Segment drag and 4× on the naive worst case. **R3's *current standing favours 16* is
>   withdrawn**, conditional on R5.5.
> - **The repair loop R3 and R4 both wrote is a catastrophe on a gesture.** Repairing per Segment
>   rather than per touched cluster costs **23.26×** at 16 Chunks — a worst case of **253.22 ms, or
>   sixteen Tick budgets, from one player gesture**. The two spellings are identical at a gesture of
>   one, which is the only size either earlier task measured.
> - **Repair loses to rebuild above ~63 clusters touched** — a scattered 256-Segment gesture at 16
>   Chunks costs **107% of a full rebuild**. R4.6's break-even arriving at the abstract graph.
> - **No rung is both affordable and correct across the whole core verb — R5.4, and it is a section
>   this plan did not have.** Deletion is monotone-worsening, so per-Segment is exact. Addition is
>   monotone-improving, and a route computed before a road existed **cannot contain it** — so
>   per-Segment declares **100.00%** of the cache valid and structurally cannot notice, and
>   **per-cluster fails identically**. **Only global is sound under addition**, and R5.3 measured
>   global as unusable. Sized: **4 restored Arterial Segments, ~512 m**, leave **9.22%** of entries
>   stale at a mean **16.71%** detour, worst **62.65%** — **a floor, and it never heals**, because
>   only eviction removes it and `adr/0012` keys by O-D rather than by agent. **Five ways out in
>   [`spike-results`](../docs/spike-results.md) → R5.4**; two are the corpus's call, not a
>   benchmark's.
> - **Addition is measurable, which R3 had concluded it was not.** Build the abstract graph on the
>   **full** graph so every portal slot is reserved, then delete a set of Segments and restore them —
>   restoration is addition and needs no new portal. **R6 inherits the technique.**
>
> **Still open: R5.5, the path source** — R2 left shared-route and next-hop live and this is the task
> that was to choose between them. **And R5.6, the Parking Shed**, which this plan calls the consumer
> the ladder is most likely to be decided by. **`CONTEXT.md` → Epoch must not be updated until R5.6
> runs**, because a shed is a neighbourhood rather than a path and per-Segment has no obvious meaning
> for it.
>
> **The numbering shifted and this note is why.** R5.4 was *the path source* when this plan was
> written. The addition measurement was not a section at all — it exists because R5.3's recommended
> rung turned out to have a hole only a measurement could size — and it took R5.4 because that is
> the order the work ran in. The path source and the Parking Shed are now **R5.5** and **R5.6**.
>
> **Discharged: the canonical pinned `performance` capture**, and taking it found the protocol's own
> pinning to be wrong — one logical processor, which starves the tiered JIT onto the measured core.
> See `spike-results` → *S2 R5* → *The capture*. Every earlier `performance` capture in S2 carries it.

### R5.5 — the path source — **DONE**

> **What it decided.** **Shared District route is retired on a number** — ~180 ms per gesture, *flat
> in gesture size*, because a rebuild does not care what was deleted. **The remaining two are not
> rungs on one ladder**: a maintained next-hop table is wrong **structurally** (16.58% uniform,
> **149.73%** local, unmoved across a storm deleting 1,021 Segments) and a cache is wrong
> **temporally** (near-zero while it lasts, permanent under addition). No measurement ranks a fixed
> 16.58% against an occasional 62.41% that never heals — **`05 §4` does, and session M owns it.**
>
> **The TTL works and it is cheap.** **0.40 forced refreshes per Tick: wrongly-valid 38 → 0 within
> one rotation, 97.08% of the cache retained**, against a control that plateaus at **23** and does
> not move for 960 Ticks. R5.4's *does not heal* is measured. **Every tripwire below was honoured**;
> tripwire 4 **fired negative** — R5.2's 23.26× does not generalise (0.91–1.51×) — and tripwire 7's
> instrument-can-move requirement is what the control row exists to satisfy.
>
> **A pre-existing defect found on the way**: `HpaSearch` seeds Access Point remainders against the
> **pristine** graph, so the hierarchy returns routes down bulldozed roads. Common-mode across the
> Epoch rungs, so nothing moves — but ***Unroutable* on a hierarchical row is evidence of nothing**
> and **R6 must fix it.** Numbers in [`spike-results`](../docs/spike-results.md) → *S2 R5.5*.

#### The design, as written before it was measured

**R5.4 changed what this section is about, and the change is worth stating before any number.** R2
handed R5 a choice between a shared District route and a next-hop table, to be decided on
*invalidation*. R5.3 and R5.4 then measured the rung that was not on R2's ballot — HPA\* behind a
route cache — and found its error is **temporal, permanent and concentrated on the busiest pairs**.
A maintained table's error is **structural, bounded and identical every Tick**. So the section is no
longer *which is faster*; it is **which kind of wrong the city should have**, and only the first of
those is a benchmark.

**The rungs.** Four, against a control.

| | Path source | What an edit costs it | What it is wrong by |
|---|---|---|---|
| **a** | HPA\* + `RouteCache`, per-Segment Epoch | O(1) per lookup; exact under deletion | **permanently** stale under addition — R5.4 |
| **b** | **a** plus a **TTL rotation** — NEW | a fixed slice of the cache expired per Tick | bounded by the rotation period |
| **c** | next-hop table maintained by `DistanceVector.RepairSubtree` | 4.71 ms per single edit — R4 | District granularity: 18.52% uniform, **128.82%** local — R4.8 |
| **d** | shared District route, **rebuild only** | a full rebuild; no repair is written | 36.01% — R2 |
| **control** | flat A\* on the current graph | nothing; it is never stale | nothing; it is the truth |

**Rung b is `adr/0043` applied to a proposal rather than to a document.** R5.4 tabulated five ways
out and typed two of them *arguable*. A TTL is **option C**, and pairing it with option B is what
makes B legitimate rather than a defect: `BOUNDED KNOWLEDGE` permits drivers not to know about a new
road **if the ignorance is modelled with a stated learning rate**, and a rotation period *is* a
stated learning rate — a number a designer sets and a player can be told. The question is whether it
is affordable, which is measurable, so it is measured here rather than argued in session M.

**Rung d is priced as a rebuild and is not given a repair.** R4 established maintenance is
*separable* from path source, so a repair written for `RouteStore` would measure the repair. If d
loses by an order of magnitude that is a retirement on a number, which is what R2 asked for.

**Tripwires, stated before the measurement.**

1. **A rung that cannot be made correct again within one Tick budget after a plausible gesture is out
   on a design commitment, not on a number.** The player is waiting.
2. **A rung whose error does not heal is out** unless the corpus adopts option B *with* a stated
   rate — because R5.4 established **permanence**, not magnitude, as the disqualifying property.
3. **The TTL's refuting number, named in advance:** the forced refresh rate needed to bound staleness
   at *N* Ticks exceeds the routing budget. Published the R3 way — *a rotation of period N fits while
   fewer than X refreshes per Tick are forced* — never as a multiple over the guessed arrival rate.
4. **`RepairSubtree` takes a changed-arc set, so it has a coalesced spelling.** R5.2's **23.26×**
   finding is therefore testable for generality here. If looping it per Segment over a drag is a
   catastrophe too, that is a corpus-wide API shape rule and not a routing note.
5. **Sample size per rung is printed or the row is void** — R1's survivorship defect, and this would
   be its fourth instance.
6. **The flat-search denominator is measured first and last** — R3's finding, which R5's own capture
   has just proved live at 4.88×.
7. **The detour column must have a rung expected non-zero**, or a zero is indistinguishable from an
   instrument that is not wired up — R3.5, and R3.6 is how that was established.

**Two caveats travel with every figure this section produces**, both inherited: the hit-rate *levels*
rest on R5.3's **invented pool** standing in for Trip repetition, and the Street half of the
addition measurement reads zero because **the synthetic grid is degenerate**. Neither is fixable
without Trip generation. The ratios between rungs under one pool are what the section is for.

> **R4 hands R5 four things and one of them changes the ladder below.**
>
> - **A fourth rung is owed on the ladder: dynamic subtree repair.** R4 measured it at **4.71 ms**
>   against a 234.74 ms rebuild on a single deleted Segment, correct, and correct on a severance too.
>   The three-rung Epoch ladder below is about *what survives an edit*; this is about *what it costs
>   to make the survivors correct again*, and they compose rather than compete.
> - **The O-D draw is no longer uniform.** R4.1's swept family is in the harness and R5's storm
>   should run against it, because hit rate under any invalidation rung is a property of the
>   distribution and R3's figures were an upper bound for exactly this reason.
> - **The refresh-cadence input R3 said R5 must have is now priced.** R4.6 puts the incremental /
>   rebuild break-even between **1% and 10% of arcs moved per refresh**. R5 no longer needs the
>   cadence as an unknown input to *every* figure — it needs it to know which side of that break-even
>   the design sits on, which is a narrower and answerable question.
> - **A drag deleting hundreds of Segments in one gesture is still the open case**, and it is what
>   R3 deferred cluster size to R5 for. R4 measured single edits only.

The measurement that separates a routing design that works from one that works on a static graph.

- Apply a realistic edit rate — roads drawn, roads deleted, a district bulldozed — while routing at
  full load, and measure both candidates through it.
- **Epoch semantics are under test here, not just performance.** Cached routes record the Epoch they
  were computed under and revalidate lazily on next use, **never a global flush**
  (`CONTEXT.md` → Epoch). A candidate that needs a global flush on edit has failed a stated design
  commitment regardless of its throughput.
- **But the Epoch as written is a single counter on the whole Road Graph, and that makes the commitment
  half true.** A counter carries no location, so a route computed at Epoch 5 and used at Epoch 6 cannot
  tell whether the edit touched it — leaving only *treat it as stale* (a total invalidation, merely paid
  lazily) or *re-walk its Segments to check* (O(path length) on every hit). **`Lazy` describes when you
  pay, not what survives**, and the corpus's phrase conflates the two. It bites hardest here for the
  reason this plan already gives about DSDV: *in a city builder link deletion is the core verb*, so the
  Epoch moves every few seconds of play.
- **Measure a three-rung ladder, because all three are conservative and the State Hash is unchanged
  across them** — which makes this an optimisation under `05 §4`'s own test, and optimisations are
  settled by measurement rather than argument. A revalidated route is recomputed deterministically over
  the same graph and comes back identical; the rungs differ only in how often they say *might be stale*,
  so over-invalidation costs work and never correctness.

  | Rung | Revalidation test | Cost |
  |---|---|---|
  | **Global** — as written today | one counter moved → assume stale | O(1) check; hit rate collapses on any edit |
  | **Per-cluster** — riding `adr/0040`'s partition | any cluster the route crosses moved | O(clusters crossed); over-invalidates near misses; composes with HPA\*'s own per-cluster repair |
  | **Per-Segment** | max version among the route's own Segments | O(path length); exact |

  Storage decides nothing — a version word per Segment is 30,000 × 4 B ≈ **120 KB**. The comparison is
  hit rate against revalidation cost, which the edit storm already drives.

  **MEASURED, and the comparison this bullet names turns out to be a comparison between a rung that
  is better on both axes and two that are worse on both.** Per-Segment costs 42 revalidation words a
  lookup against global's 0.71 and still has the **lower mean Tick at every edit rate**, because a
  revalidation word is arithmetic and the thing it avoids is a search. Storage indeed decided
  nothing — 129 KiB measured — but neither did revalidation cost. **The rung table above should be
  read as three *precisions*, not as a ladder with a price at each step.**
- **Report cache hit rate as a function of edit rate, not just throughput.** That curve is the actual
  finding. Under the global rung, hit rate is not a property of the O-D distribution at all — it is a
  property of how recently the player touched anything — so an edit-storm throughput figure could be
  reported with a cache that had silently stopped working.
- **Two things the benchmark cannot settle, and they are not deferred to it.** `CONTEXT.md` → Epoch
  needs the *when you pay* / *what survives* distinction written in regardless of the numbers; and R1
  must state **whether the travel-time matrix carries an Epoch at all**, because R1 currently
  invalidates it by *dirty region* — a spatial mechanism — while routes invalidate by a scalar. **Two
  invalidation mechanisms are in the corpus and nothing relates them**, so the matrix and the cache can
  disagree about what the network currently is.
- Record the worst single-Tick cost, not the mean. S4's K6 established that the quantile hides the
  event: a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9.

- **A gesture needs a coalesced repair and a rebuild fallback, and a per-edit API invites neither.**
  NEW, produced by R5.2. A cluster's edge set is a function of its arcs, so it must be decided once
  however many Segments inside it were deleted — but `RebuildFor(segment)` is the natural shape and
  looping it over a drag costs up to **23.26×** what coalescing costs. Above ~63 clusters touched the
  coalesced repair loses to a **full rebuild** outright. So the repair path has two thresholds, not
  none, and both are properties of *clusters touched* rather than of Segments deleted.

*Decides:* whether either router survives the core verb, and it is the task most likely to reverse
R3's and R4's ranking.

### R6. The two caches, and `adr/0006` — **PROMOTED by R3. Load-bearing, not a tidy-up**

> **R3 promoted this task.** No cluster size fits routing into the Tick budget — the best rung breaks
> even at **85 Trip starts per Tick** — and a cache is one of only two exits, the other being to spend
> the whole Tick budget of eight cores on routing. **Whichever router R4 picks will need this**, so R6
> stops being an optimisation measured after the choice and becomes a condition the choice depends on.
> It also inherits a partial answer: R3's stored path arena already caches the intra-cluster half, at
> 223.92 KiB, so R6's remaining question is the O-D half.

> **R4 removes R6's blocker, which was that this task was not runnable as written.** The bullet below
> says *"measure hit rate against R1's real O-D distribution rather than a uniform one, which would
> flatter it"* — and R1 produced no such distribution. A hit rate measured on a uniform draw is close
> to meaningless, because what makes a route cache work is that real Trips **repeat**. **R4.1
> supplies a swept family instead** — uniform, distance-decay at three lengths, monocentric — so R6's
> instruction becomes *report hit rate as a curve across the family, naming the rung beside every
> figure*. Same handling this plan already gives District count, the Microscopic Cap and the peaking
> factor. **It is still not the real distribution**, which needs Trip generation; R6 must say so
> beside its numbers rather than let a curve read as a fact.

[`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) permits route caching **keyed
by origin-destination pair, never by agent**, invalidated lazily against the Epoch. That is exactly
the pending-state class [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) calls
dangerous, and `adr/0006`'s reversal criteria are *"Nothing."*

**No eviction policy is stated anywhere in the corpus.** `adr/0017` shows the pattern to follow —
fixed capacity, least-used eviction — and nobody has written it down for routes.

- Fixed-capacity, O-D keyed, Epoch-invalidated. Measure hit rate against R1's real O-D distribution
  rather than a uniform one, which would flatter it.
- **The key is the node pair the search spans, never the Access Point pair.** `adr/0012` says only
  *"keyed by origin-destination pair"*, and that phrase is ambiguous now that an Access Point is known
  to be a `(Segment, offset)`: keyed on those, the space is Buildings² ≈ 2.25 × 10¹⁰ and the hit rate is
  approximately zero. Keyed on the **endpoints of the origin and destination Segments**, with the
  offsets applied as an arithmetic correction afterwards, the space collapses to nodes² and the five
  Buildings sharing a Segment share one entry instead of minting five. **Hit rate is a property of the
  key before it is a property of the distribution**, and R6 must state the key beside every figure.
- **The trade the key makes must be measured, not assumed.** Two Buildings at opposite ends of a long
  Segment share a route that is wrong for one of them by up to a Segment length. Report the induced
  error against the **Commute Budget**, which is the only thing that consumes it — if the Budget is
  never decided at a granularity finer than a block, the error is free and the key is strictly better.
- Run long enough to demonstrate **no collection and no magnitude trending upward at steady state**,
  per the definition of done. A cache that grows is not a cache.

**There is a second cache and this plan did not know about it.** `05 §3` declares *"cached Parking Shed
membership per Building — `(derived AND rebuilt)`. The ordered set of parking Bins within walking
distance of a pedestrian Access Point, **invalidated by the Road Graph Epoch**."* It is the Epoch
consumer that scales with **Buildings** rather than with routes, and it is the one R5's ladder is most
likely to be decided by.

- **Under a global Epoch, one road edit invalidates all ~150,000 sheds at once.** `adr/0009` promises
  arrival is *"a handful of lookups, never a search"* — a promise that holds only while the shed is
  warm. After any edit, every arriving vehicle in the city pays a rebuild first, and it pays it **on
  arrival**, which is the moment a Trip is trying to finish. That is a stampede triggered by the
  player's most common action.
- **It ranks the ladder differently from routes, which is why it must be measured beside them.** A shed
  is *a set of Bins reachable within a walk*, not a path — so a **per-Segment** Epoch does not obviously
  help, because the Segments it is versioned against are its whole reachable neighbourhood rather than a
  route. A **per-cluster** Epoch fits it far better than it fits routes: a shed is inherently local, and
  *"did anything change in my cluster"* is close to the right question already.
- **Measure both caches against the same edit storm**, and report **rebuild cost at the moment of edit**
  alongside steady-state hit rate. A ladder chosen on routes alone would be chosen on the cheaper of the
  two consumers.
- **`05 §3` is owed the same correction `CONTEXT.md` → Epoch just took** — *invalidated by the Road
  Graph Epoch* is a statement about when the rebuild is paid, not about how much survives, and under one
  counter the answer is *none of it*.

*Decides:* the eviction policy, which is owed to `adr/0012` as an amendment.

### R8. The congestion loop, and whether three layers make it natural — **NEXT. Runs before R5.6 and R6**

> **Numbered by creation, not by run order.** R5.6 and R6 have published figures citing their own
> numbers and renumbering a task with citations is how a corpus loses its references — R5.4→R5.5
> already cost this plan one correction. R8 runs first because it is the last thing standing between
> routing and a verdict, and because its diversion-cost column is an **input to R6**: Sight makes a
> mid-journey re-decision routine rather than exceptional, and what one costs depends entirely on
> which path source R6 caches.

**Everything S2 has measured so far ran on a frozen cost basis.** R1's matrix, R2's ladder, R3's
hierarchy, R4's protocols and R5's storm all route over an arc-cost array that is computed once and
never moves. Nothing in this spike has ever invalidated a route because a road got busy — and under
[`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) the
volume column moves **every Tick**, so every precomputed structure S2 has priced is stale the Tick
after it is built. That is not a defect in those tasks; it is the question they deferred, and it is
this one.

[`adr/0046`](../docs/adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)
settles the **structure** by argument, on the grounds that
[`adr/0017`](../docs/adr/0017-agents-satisfice-they-never-optimise.md) already settled it for every
other actor class: sticky incumbent, known alternatives, switch only when substantially better. What
it settles nothing about is **every parameter**, and it names five claims and the number that refutes
each. R8 produces those numbers. **It chooses no Ruleset value** — it reports curves, exactly as R1
did for the District count, and the corpus decides.

#### What is being built

A **closed loop**, which S2 has not had: fleet volume → live BPR cost → a Sight decision at each
crossing → a diversion or not → volume. `Traffic/Fleet.cs` already carries volume attribution,
arrivals and the conservation check; `Matrix/Congestion.cs` already carries BPR at β = 4. The new part
is the decision, and the fact that the cost array it reads is the one the fleet is writing.

**Habit is the free-flow next-hop table, and that choice is a constraint worth stating.** R4 and R5.5
measured a District-granular next-hop table as structurally wrong by 16.58% on the uniform draw and
**149.73%** on the local one, so R8's Habit carries a known granularity error. It is used anyway, for
two reasons: it is what exists, and **diversion under it is free** — a Traveller that leaves the
Habit Route resumes by reading the table from wherever it now is, with no search. Under a stored
route the same diversion costs a fresh search, which is what R8.6 prices. The consequence for
reading R8: **its stability conclusions carry to either path source and its cost column does not.**

#### The rungs

| | Layer set | What it is for |
|---|---|---|
| **control** | Habit only — Sight Horizon 0 | Nothing responds to congestion. It must **not** oscillate |
| **a** | Habit + Sight, Temperament spread 0 | The instrument check. It **must** oscillate |
| **b** | Habit + Sight + Temperament | The proposal |

#### The first build was wrong, and it was wrong in a way worth recording rather than fixing quietly

**The loop was closed on the routing side and open on the physical side.** Travellers consumed
**free-flow** residuals: the VDF was computed, routing read it, and *nothing in the world slowed down*.
`03 §3.4`'s chain is *volume → travel time → route choice → volume*, and the middle arrow was missing —
so "volume" was a count of concurrent users of an arc rather than an accumulation, and the
amplification that makes a jam a jam (slower → longer dwell → more volume → slower) was absent
entirely.

It presented as three separate oddities and they were one defect: `v/c` ran **26–141** with **58% of
the busiest indices past `MaximumVolumeCapacity`**, so BPR returned the same number for *bad* and
*impassable* and **the router was blind across most of the congested core**; and peak `v/c` came back
**non-monotone in the Sight Horizon**, which cannot be read at all from inside a clamped region.

Two things follow, and the second is the more useful.

**The tripwires were written for the wrong model and must be re-derived before the corrected one
runs.** The original wording is kept: *"the control must not move — Horizon 0 must show near-zero
oscillation."* Under free-flow residuals a Traveller's dwell time is independent of congestion, so a
control with no routing response is genuinely inert and that wire was sound. **Under live residuals it
is not**: the control now has real dynamics — arcs slow, Travellers dwell, volume rises — and it still
has no ability to *respond*, which is the whole point of it. So the instrument check moves to the
comparison that isolates routing: **Sight must change the volume trajectory against a control with
identical physics and no way to answer it.** Restated **before** the run and recorded here so it is
not mistaken later for a wire that was loosened after it fired. *A tripwire is only worth what the
model underneath it is worth, and that is a new failure mode this plan had not seen.*

**And it exposed a prerequisite nobody had named — R8.0 below.**

#### R8.0 — the load this network carries, and it runs before everything

The first build inherited R2's fleet size of 40,000 on the grounds that figures should be comparable.
That was the wrong reason: **R2 was pricing attribution and did not care whether the network was
gridlocked.** With live residuals and BPR at β = 4, an arc at the clamp costs **39.4× free-flow**, so
Travellers dwell 39× longer, so volume rises further — **positive feedback that will pin at the clamp
from any load high enough to reach it.** A congestion-response measurement taken inside that region
measures the clamp.

So: sweep fleet size across at least a decade at Horizon 0 with no routing response. Report peak
`v/c`, the share of the top-64 at or above the clamp, arrivals per Tick, mean journey time, and
**whether the rung reaches steady state at all**. The operating load is the largest that leaves the
busiest arcs inside the range BPR can resolve, by a criterion stated as a number before the sweep
runs. Every later rung uses it and every table names it.

**The sweep is a result and not a calibration step.** It says what load this network gridlocks at —
and it must be read against **decision 11, the representative funnel**, where every Trip into a
District arrives through one node and which R2 already measured at **412%** `v/c`. Report `v/c` over
all indices *and* with the funnel arcs excluded, so a reader can see how much of the gridlock belongs
to the partition rather than to the city. **If this network gridlocks far below the load the map
should carry, that is a finding about District-granular routing** and it outranks everything below it.

#### R8.1 — the actionable-junction distance. No traffic at all, and it runs first

For every node, the distance — in Segments, and in Q16.16 Ticks of free-flow car time — to the
nearest node with a **real choice**: out-degree ≥ 2 counting only arcs that are not the reverse of the
one used to arrive. Report the distribution, not a mean: p50, p90, max, and the share of nodes at
distance 0.

**This sets the floor for the Sight Horizon before any behavioural argument runs**, and it is the one
parameter in `adr/0046` whose lower bound is derivable rather than tuned. A Traveller looking `N`
Segments ahead at a node whose next choice is `N + 2` Segments away receives a signal it is
structurally unable to act on.

**The unweighted mean over nodes is the wrong denominator and must not be published alone.** What a
driver experiences is the distribution weighted by *where drivers actually are*, which needs traffic
— so R8.3 reports the crossing-weighted version and R8.1 reports the graph's. Naming both is this
spike's standing lesson about denominators arriving for the fourth time.

#### R8.2 — the loop closes, and the instrument moves

Rung **a** at the horizon R8.1 sets. The deliverable is **not** a number about traffic; it is the
right to publish the rest of the section.

- **Oscillation amplitude**, defined before it is measured: over the top-64 volume indices by mean
  volume, the mean absolute Tick-over-Tick change in `v/c`, in Q16.16, over the measurement window.
- It must be **materially above the control**, which is rung *control* at Horizon 0. If it is not,
  the loop is open — costs are being computed and not read — and **no figure in R8.3 to R8.6 may be
  published.** This spike has shipped an instrument that could not move three times.

#### R8.3 — the Sight sweep

`N ∈ {0, 1, 2, 4, 8, 16, 32}`, Temperament spread 0, across R4.1's swept O-D family.

Columns: peak `v/c`; mean `v/c` over the top-64; oscillation amplitude; **diversions per Tick**;
**the share of crossings at which the driver had no alternative** — R8.1's finding, traffic-weighted;
mean journey time; and **ns per Tick for the Sight pass alone**, against 15.6 ms.

The expectation being tested is that peak `v/c` falls with `N`, cost rises with `N`, and the
no-alternative share is what explains whatever `N = 1` does. If peak `v/c` is flat in `N`, Sight is
not a mechanism and `adr/0046`'s middle layer is wrong.

#### R8.4 — the Temperament sweep

At the horizon R8.3 selects.

**The first build swept spread against a *fixed* base threshold and the diversion rate moved by under
3% across the whole sweep — which refutes nothing, because a sweep aimed away from where the decisions
live has no purchase on them.** So R8.4 publishes its own denominator first: at every crossing with at
least one surviving alternative, record `(habitScore − bestScore) / habitScore` and print the p10, p50
and p90 of that distribution. **The base threshold is then swept across the measured distribution
rather than across a guess**, and spread is swept at the base nearest the median. This is R3's rule
about denominators — *invert the derivation until what is published is measured* — reaching a
behavioural parameter rather than a timing one.

- **Spread** ∈ `{0, 1/16, 1/8, 1/4, 1/2}` of the base threshold.
- **Blend weight** ∈ `{0, 1/2, 1}` — pure jitter, even, pure character. `adr/0046` argues *both
  endpoints fail*, for different reasons, and that is a claim with a number attached: pure jitter
  should show diversity in aggregate and no persistent character, pure character should re-synchronise
  into a smaller permanent herd.

Columns: oscillation amplitude, peak `v/c`, mean journey time, diversions per Tick.

**Amplitude must fall monotonically in spread across at least the first three rungs.** Flat or
non-monotone refutes the layer, and `adr/0046` already names what would replace it.

> **Result, and it changed twice.** Temperament first read **REFUTED** on a sweep that moved by under
> 3%, then **REFUTED** again on a sweep that genuinely varied — and is finally **NOT REFUTED**, because
> both readings were **siting artefacts**. The base ladder had been sited at the *median* of the
> improvement distribution, which is **past the transition**: the herd survives a small threshold
> (0.031250 → 9.87) and dies at a larger one (0.125000 → 0.55), so every spread rung had been swept in
> a regime with no herd left in it. Re-sited at the non-zero base with the highest measured
> oscillation, spread takes amplitude **9.87 → 0.76, a 92.28% fall against a 25.00% bar**. **Both terms
> damp, independently.** So `adr/0046`'s mechanical justification survives and the layer does not
> collapse to a single Ruleset number — and `adr/0005`'s principled ban on shared decisions, which
> would have carried it either way, was never the thing in question.
>
> **The wire as written is still not met, and it is scored that way.** The ladder reads 9.87, 0.63,
> 0.65, 5.71, 0.76 — non-monotone. What stands beside it is an argument that does not depend on which
> way those values fell: a separate blend re-measurement at the same spread returned **0.86 and 0.65**,
> establishing a noise band of roughly 0.6–0.9, and **three of the five rungs sit inside it**. *A
> monotonicity test over values the instrument cannot separate is not a test.*
>
> **And the general lesson is the third of its kind in this task.** A wire stated on **monotonicity**
> cannot distinguish *nothing happened* from *it saturated at the first rung* — and this mechanism is
> a cliff rather than a gradient, dropping 15× at the first spread rung alone. That is the same defect
> as the max over 33,018 indices and the unconditioned p99: **a statistic chosen before anyone knew the
> shape of what it would measure.** R8 supplies the rule and it generalises past routing: *state a wire
> over a statistic only after establishing that the phenomenon has the shape the statistic assumes.*
>
> **A fourth rule, and it is the most transferable thing in the section.** *A sweep across a measured
> distribution is not automatically a sweep across the regime the mechanism operates in.* R8.4 had
> already applied R3's denominator rule correctly — it measured the improvement distribution instead of
> guessing at it — and **still sited the sweep wrong**, because the median of what is *offered* is not
> where the mechanism *acts*. Measuring the denominator is necessary and it is not sufficient.

#### R8.5 — does `03 §3.4`'s loop actually close?

The load-bearing one, and the reason static Habit is the null hypothesis rather than the assumption.
`Fleet.Surge` already exists and R2b already used it: replace a slice of the fleet with Travellers
bound for one District, which is R1's monocentric morning peak.

Report the peak `v/c` reached on the worst arcs, the Tick it peaks at, whether it returns below a
stated multiple of its pre-surge level, and **in how many Ticks**. The control at Horizon 0 cannot
respond and must never recover; if it does, the recovery is the fleet's respawn process and not the
routing.

**Refuted if**: with Sight on, the volume does not come back down — or it comes down and re-peaks
without settling. Either result puts an adaptive Habit refresh cadence back on the table, and with it
a hash-bearing number, `adr/0015`'s membership test, and R4.6's break-even selecting an algorithm.

**The surge had to be *sustained*, and the first two builds got it wrong in a way that produced no
result at all.** `Fleet.Surge` retargets Travellers who then respawn on arrival, so the disturbance is
a **pulse with a half-life of one journey** — and any system whatever recovers from that. Control and
Sight both settled, five runs of five, and the section reported a null that was really an absent
contrast. The fix is to weight the **respawn pool** toward the surged District for the whole window,
which is also the honest model of R1's monocentric morning peak: demand stays asymmetric, so a control
with no way to respond cannot come down by waiting.

**It also changes the shape of the question, and the rule was rewritten before the run rather than
after.** Under a sustained load the question is no longer *does it recover* but **does it reach a
bounded steady state, and at what level** — control and Sight should differ in the level they settle
at, not in whether they settle at all. *The re-peak rule written for the pulse era went through three
versions and is retired rather than deleted; a rule rewritten after a reading is not a rule.*

> **Result: `adr/0046`'s most exposed claim is NOT REFUTED.** Both rungs bound. The control settles
> **16.83% above** its own pre-surge level; Sight at Horizon 1 settles **26.50% below** it — **42.62%
> under the control against a 5.00% bar.** `03 §3.4`'s self-correction closes with only the local
> layers reading the VDF, so **static Habit survives as the null hypothesis** and the whole
> refresh-cadence problem stays shut: no maintenance scheme, no hash-bearing cadence, and R4.6's
> break-even does not select an algorithm after all. **The error runs in the safe direction** — 4 of 5
> control runs peak inside the last quarter, so the control may still be climbing and its plateau is a
> *lower* bound, which understates Sight's advantage rather than flattering it. The successor
> condition is stated: run until the peak sample is not in the last quarter.

#### R8.6 — what a diversion costs, by path source

ns per diversion, two ways: reading a different arc out of the next-hop table, and one flat A\* from
the current node to the destination Access Point over the **live** cost array. Multiplied by R8.3's
measured diversions per Tick, this is the per-Tick bill each path source pays for having Sight at all.

**It does not go through `HpaSearch`**, whose pristine-seeding defect R5.5 found and R6 owns. R8's
diversion search reads the live array directly, so nothing here inherits it.

*Decides:* nothing on its own. It produces the five numbers `adr/0046` names, plus the third axis
session M is owed — **structural error, temporal error, and now diversion cost** — and R7 states the
verdict.

---

### R7. The report, and the verdict

Into `docs/spike-results.md`, in the form S4 established: the machine, the numbers, and — separately —
**the decision each produced.** A spike that records data and no verdict has not finished.

Then delete `spikes/S2.Routing/` and record the deleting commit's parent.

---

## The tripwire, written before the numbers arrive

S4's practice, and its stated reason: *the wire was written before the numbers arrived precisely so it
could not be reasoned around afterwards.* S4 also found the harder lesson — **a threshold whose
meaning depends on an unstated machine is not a threshold** — so each row names its machine.

| Condition | Response |
|---|---|
| Routing exceeds **10% of the 15.6 ms Tick budget** at 1M on the *desktop* (i5-10400, DDR4-2133), **at the morning peak**, with matrix refresh amortised | The map falls back to **2048²**, which `05` already documents as the fallback |
| ~~The travel-time matrix read costs **more than S4's K2 random gather**, at the District count the design needs, on the *desktop* (i5-10400, DDR4-2133)~~ **MEASURED by R1, and it does not fire** | Larger than S2. `02 §5.8`'s rule is unenforceable and the choice loop's design reopens. **The wire fires at 13.66 ns and reads 1.14 ns at the working anchor, 5.00 ns at 4,096 Districts** — approached on the axis the plan named, and not reached. `02 §5.8` is enforceable |
| Either router needs a **global flush** on a Road Graph edit | That candidate is out on a design commitment, not on a number |
| An attribution scheme **cannot report a jam within the congestion cycle it happens in** (R2b) | That scheme is out on a design commitment, not on a number. `03 §3.4`'s self-correcting circularity is the load-bearing assumption of the fidelity model and a lagging detector breaks it |
| The route cache **grows at steady state** with no bound | `adr/0006` violated. Fix the cache, not the ADR |
| The congestion loop **does not close** — an over-used Segment's volume never recovers under Sight (R8.5) | `03 §3.4`'s self-correction is then a property of a scheme nobody has built, not of the design. An adaptive Habit refresh is forced, and with it a hash-bearing cadence, `adr/0015`'s membership test, and R4.6's break-even selecting an algorithm. **This is the row `adr/0046` is most exposed to** |
| ~~DSDV's routing tables exceed the **whole world's 172.3 MiB footprint**~~ **MEASURED by R4, and it does not fire — at the granularity the design can use** | Distance-vector is out on memory alone. **At the 121-District anchor DSDV is 23.12 MiB, 0.13× the world; at node granularity it is 3.11 GiB, 18.51×.** So the wire fires on *granularity* and not on the protocol, and sequence numbers neither cause it nor would removing them fix it. **Distance-vector went out on cost instead** — 2.13× a full rebuild — which no row here anticipated |

**R8 adds a rule about what a wire may be stated *over*, and it is S4's K6 lesson arriving from the
other side.** *A tripwire may not be read off a maximum over a large noisy population.* R8's first two
builds used **peak `v/c` — a max over 33,018 volume indices — as the headline column**, and three
separate "findings" rested on it: a Sight ladder that was not monotone, a blend-weight row that
contradicted `adr/0046`'s prediction, and a surge that read as re-peaking. A max over tens of
thousands of indices moves several-fold on noise alone, so none of the three was evidence of anything
until the column became a quantile ladder. S4 already knew this — *a run whose worst iteration was
100.2 ms read 2.462 ms at p99.9* — and the wire above quotes that sentence, **which did not stop the
same statistic being used as a headline two builds running.** Citing a lesson is not applying it,
which is `adr/0044`'s closing finding for the third time and the second time inside this plan.

**And a second rule, about instruments rather than statistics.** *An instrument validated for
movement is not thereby validated for the thing being swept.* R8's tripwire 1 established that the
oscillation metric moves between Horizon 0 and Horizon N. It established nothing about whether the
metric responds to **herding**, which is what `adr/0046` actually predicts — and amplitude and
synchrony are different quantities. A refutation published on the first while claiming the second
would have been this spike's fourth dead instrument, and the first one to fail *after* passing a
tripwire written to catch exactly that.

**R3 adds a rule about how a wire is stated, and it applies to every row above.** *Gather a tripwire
as direct data wherever the data exists, and where it does not, invert the derivation until what is
published is measured.* R3's Tick-budget row was first drafted as *routing is 6.4× over budget*, which
multiplies a measured per-route cost by **550 Trip starts per Tick** — a figure resting on a mean Trip
duration the corpus records as provisional, in a spike with no Travellers and no Trip generation to
produce a better one. **A wire whose denominator is a guess fires on the guess.** Stated the other way
round — *routing fits while fewer than 85 Trips start per Tick* — the published quantity is a measured
cost divided by a world constant, it contains nothing derived, and it stays true when the arrival rate
is finally measured somewhere that can measure it. The same inversion is available to the 10% row and
is the reason its own defence below is so long.

**The 10% figure is chosen here and nobody has ratified it.** The corpus states no routing budget. It
is offered against two anchors: S4 measured the Event Wheel and its wake gather at **1.80%** of the
Tick at the most pessimistic wake rate, and
[`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) names the
order of suspicion for a slow Tick as *the Microscopic Cap, routing, and the Sweep Rule schedule*.
Routing being allowed five times the Wheel and still leaving 90% is defensible; it is not derived.
**It is recorded as unratified in [`0002`](0002-open-questions.md) on the day it was chosen.**

**It stays a stated guess, and grilling established that it *cannot* be ratified yet rather than merely
that nobody has.** Two reasons, and both are worth keeping visible so the figure is not mistaken for a
derivation later:

- **The denominator does not exist.** 15.6 ms is the whole Tick at 4× speed. The Tick at 1M also
  contains Rules, Map Layers, the Sweep schedule and the Microscopic tier, and **only the Event Wheel
  has been measured** — S4's 1.80%. A share is a claim about a whole that is ~90% unpriced. *This is the
  denominator lesson a third time: S4 established that a denominator needs its machine and its moment,
  R0 adds that it needs its quality stated, and this adds that it needs to exist.*
- **The symptom it fires on is one the design elsewhere calls harmless.** `CONTEXT.md` → Speed makes
  tick rate *"purely a host concern — the simulation cannot observe it"*, and `03 §3.9` absorbs hardware
  limits by **a slow machine advancing fewer Ticks per second**. So blowing the Tick budget is not a
  correctness failure: nothing breaks and the city is identical, the player simply does not get 4×.
  What the row is really protecting is **the point at which the top speed setting stops being
  deliverable**, which matters under `01 §7`'s rule that a number must not be caught contradicting what
  the player is watching — a control labelled 4× that delivers 1.5× is exactly that.

**And the response is under-argued as well as the threshold.** 2048² is `05`'s documented fallback, but
nothing establishes that **map size** is the right lever for a routing cost rather than a coarser matrix
cadence or a lower top speed setting. Falling back to 2048² changes the *world* in answer to a *pacing*
symptom, in a design that has an explicit home for pacing and it is not the map. **S2 should report the
absolute per-Tick figure at peak on the named machine regardless of where the 10% lands**, so the
fallback decision can be taken on evidence once the Tick's other consumers are priced.

**The first row said *sustained* and now says *at the morning peak*, which is a correction rather than a
tightening.** *Sustained* reads as excluding transients, but a peak is hundreds of Ticks and is
sustained under any reading — so against a Day-average load the row fired on a condition the game never
experiences and stayed silent on the one it does. This is S4's K6 lesson in its own words: *a run whose
worst iteration was 100.2 ms read 2.462 ms at p99.9.*

---

## Decisions owed by this spike

Rule 6 of [`0003`](0003-build-plan.md), applied while planning rather than after.

**1. `Segment` needs a `CONTEXT.md` entry — the gate above.** Blocks R0.

**2. The travel-time matrix refresh cadence is almost certainly hash-bearing and is filed as
tuning.** `02 §1.2` lists accessibility refresh among the tunable knobs and `02 §6` describes it as
*slow cadence, dirty regions only*. But cadence decides when a changed travel time becomes visible to
the choice loop, and two cadences therefore produce two cities — **a design change under `05 §4`'s
State Hash test, not a free knob.** This is the **fifth** instance of the welding failure `adr/0034`
found in Chunk size, after Map Layer diffusion cadence, and it is the second one found by reading
`02 §1.2`'s tuning column specifically. *Recommended handling: reclassify as Ruleset-authored but
world-creation-fixed, or produce the argument for why it is not. `05 §4` lists "rebuilding the
travel-time matrix rather than saving it" as hash-preserving, which is a different claim and is
correct — a deterministic rebuild is hash-preserving; a variable cadence is not.*

**2a. The matrix's time resolution — NEW, produced by R1, and it is decision 2's twin.** Decision 2
files the *refresh cadence* as almost certainly hash-bearing. **Resolution is the same class of
decision and the corpus has never named it at all.** A Day-average matrix and a per-phase one give
the choice loop different answers to the same question — measured, 1 one-way District pair against 76
— so a Household deciding where to live decides differently under each, and under `05 §4`'s test that
is **two cities**. It costs five times the resident size and five times the build. *Recommended
handling: settle it wherever cadence is settled, because they are the same argument about the same
object and separating them is how one of them gets treated as a knob.*

**3. The routing Tick budget share** — the 10% above.

**3a. ~~How a Segment gets its volume.~~ DISCHARGED —
[`adr/0041`](../docs/adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md): the
Traveller attributes it, not the District pair.** Decided rather than measured, because the two schemes
produce **different cities** and `05 §4`'s rule says that is not a benchmark's decision to make. What
remains for S2 is the **price**, not the choice: R2a's crossover now measures what direct attribution
costs. `03 §3.3`, `§3.4` and `§3.6` are owed a joint rewrite, and force-promotion loses the lag argument
that justified it.

**4. ~~Zone count for the matrix~~ District count, and ~~road density~~. PARTLY DISCHARGED by R1, and
the remainder is not what this plan thought it was.**

R1 swept 16 → 4,096 Districts and the ceiling everybody expected — L3 — **is not the binding one.**
The scattered read never reaches the tripwire at any rung. What binds instead is a trade this plan did
not name: **the route store against the entry error.** At the anchor, 121 Districts, the scalar matrix
is 57 KiB and the route store 3.63 MiB and the mean entry is wrong by 11.32%; at 4,096 the error falls
to 3.80% and the route store is 4.06 GiB, against a whole world of 172.3 MiB. **The rung that fits is
the rung whose entries stand for the most ground**, and that is a design question rather than a cache
one. *What is owed is the Commute Budget's own granularity* — decision 8 — because the error is only
too large relative to something, and nothing in the corpus says what.

**Road density is PARTLY DISCHARGED by R0**, and the remainder is a different kind of debt. R0 swept
it and reports **16.20 km of road per km²** at the density that reproduces the ~30,000-Segment
placeholder — which also turns out to be one Street on every Cell boundary, with a mean Segment of
128 m. So the input *"exists nowhere in the corpus"* is no longer true. **What is owed is not a sweep
but a source**: whether 16.20 km/km² describes a real city, against a target the corpus justifies by
citing Los Angeles. `CONTEXT.md` → Segment keeps its disclaimer until somebody checks it, and R7 must
not record the debt as closed on a curve alone.

**4a. The cost unit for routing — NEW, produced by R0.** R0 routes in **Q16.16 Ticks** and had to: a
Tick is ~10.5 in-world seconds and R2a's own arithmetic puts a vehicle at about one Segment per Tick,
so a cost accumulated in whole Ticks gives nearly every Segment a cost of 1 and A\* silently minimises
**hop count** while appearing to route on time. But `05 §121` says *"Q16.16 is for sub-Tile positions
and nothing else"*, and sub-Tick time is not a sub-Tile position. The alternative spelling — an integer
count of a fixed fraction of a Tick — measures identically, so **no number in this spike rests on the
answer.** What rests on it is whether the core acquires a second Q16.16 meaning. *Owed by R7, and it is
the corpus's decision rather than a benchmark's.*

**5. The route cache eviction policy, and its key** — both owed to `adr/0012` as an amendment, per R6.
The key is the newer half: `adr/0012`'s *"keyed by origin-destination pair"* was written before anyone
knew an Access Point is a `(Segment, offset)`, and the phrase is ambiguous between a key space of
nodes² and one of Buildings².

**8. The Commute Budget's granularity — NEW, produced by R1, and it is what decides whether the
matrix is fit for purpose.** R1 measured what this plan did not ask for: a District-to-District entry
against the true `(Segment, offset) → (Segment, offset)` search it stands for. **The mean entry is
wrong by 11.32% at the anchor — 6.73 Ticks — with a p90 of 14.04 and a worst case of 77.62.** That is
not obviously acceptable and not obviously fatal, and it cannot be judged at all until somebody says
what the Commute Budget resolves: an error of 6.73 Ticks is free against a Budget read to the nearest
half hour and disqualifying against one read to the minute. `CONTEXT.md` → Commute Budget gives no
number and `01 §7` draws it as a wedge on the sun arc, which is a granularity of a kind but not a
stated one. **This is the same shape as R6's key, which is owed the same question** — that task must
report *"the induced error against the Commute Budget, which is the only thing that consumes it"* —
so the two should be answered once. *Owed by R7, and it is the corpus's decision rather than a
benchmark's.*

**9. Whether the matrix carries an Epoch, and whether a dirty region can invalidate it — NEW,
produced by R1, and it is a correctness question rather than a performance one.** R5 requires R1 to
state whether the matrix carries an Epoch. **It does not, and R1 declined to give it one**, because a
version counter would imply a relationship to the route cache that nobody has argued. What R1 found
is worse than the ambiguity: **`02 §6`'s *dirty regions only* is unsound.** A spatial test missed 309
of 429 changed entries on a central edit and 132 of 252 on a corner one, and it missed them silently —
leaving entries stale rather than merely coarse. The sound test is *which routes crossed the region*,
which identifies the changed set almost exactly (430 entries against 429) and **needs the route store
to exist**; and even then it touches every row, because a one-to-all fills a row and every row holds
the entry addressed *to* the edited District. **So the matrix's cheap invalidation and its cheap
storage are the same trade, taken twice**, and `02 §6` is owed a correction regardless of which way it
is taken.

**10. The path source keys on the District, so redrawing a boundary changes the city — NEW, produced
by R2.** `adr/0041` removed the District from *volume attribution* and closed that defect explicitly:
*"redrawing a boundary changes volume attribution → Stress → Fidelity → travel times → the city, and
therefore the State Hash… `PLAYER GOVERNS` means the player governs the city, not the physics."*
**Both surviving path-source rungs put it straight back.** A shared route is keyed by District pair; a
next-hop column is a tree rooted at a District's representative. Under either, moving a boundary moves
the representative, which changes the Segments a Traveller actually drives — a different Trip, and
under `05 §4` a different city. Only the **searched** rung is free of it, and R2 priced that rung out.
*Recommended handling: this is `02 §2.1`'s player-adjustable District meeting `03 §3.9`'s rule for the
third time, and it belongs with them rather than inside S2. R7 states it; the corpus decides.*

**12. The maintenance scheme, and the cadence that chooses it — NEW, produced by R4.** R4 measured
four ways to keep a precomputed routing structure correct after the graph changes, and the winner —
**dynamic subtree repair**, 4.71 ms against a 234.74 ms rebuild — is not in this plan anywhere. What
is owed is not the choice, which is measured, but the observation underneath it: **R4.6 puts the
incremental-versus-rebuild break-even between 1% and 10% of arcs moved per refresh**, so the
**travel-time matrix refresh cadence (decision 2) chooses the maintenance scheme.** A decision filed
as *tuning* turns out to select an algorithm. *Recommended handling: settle it with decisions 2 and
2a, which are already the same argument about the same object.*

**13. The District-granular route's error is a granularity decision, not a routing one — NEW,
produced by R4, and it is decision 11 arriving from the other side.** R2 published **18.52%** mean
detour for a next-hop table and R4 found that figure to be a property of the **draw**: on the uniform
distribution every S2 task had inherited it is 20.14%, and on a plausible local-trip distribution it
is **128.82%**. Aiming a Traveller at a District representative is a roughly fixed error in Ticks
charged against a shrinking journey, so the coarser the destination the worse short trips get — and
short trips are most trips in most cities. **A Traveller driving more than twice as far as it should
is a different city under `05 §4`.** The corpus has no position on how coarse a routing destination
may be. *Recommended handling: answered once, with decision 11 and with decision 8's question about
what the Commute Budget resolves — all three are the same question about what a District-granular
answer is allowed to be wrong by.*

**14. The origin-destination distribution — NEW as a *named* debt, though R0 flagged it and four
tasks inherited it.** R4.1 replaces the silent uniform draw with a swept family and every R4 figure
names its rung, which is this plan's standing practice. **What it does not do is measure anything**:
the family is invented, and what would replace it is Trip generation. *Recommended handling: R7
records the family as a placeholder with a named successor, and no document may cite a figure derived
from it without naming the rung. The board already carries the general form of this failure — a curve
reported as a fact is how the ~400k Trips/Day figure survived.*

**11. The representative funnel — NEW, produced by R2, and nothing in the corpus addresses it.** Under
either coarse rung *every* Trip bound for a District arrives through that District's single
representative node, so the arcs into it carry the whole of a District's inbound traffic: measured,
**412%** `v/c` against **130%** for the same surge under searched routes. **Stress on those arcs is an
artefact of the partition, not a property of the city**, and a Microscopic Cap spent promoting them
would be spent on the abstraction. *Recommended handling: whatever resolves it — multiple access nodes
per District, a Segment-granular tail on a District-granular route, or the searched rung after all —
is a design decision about what a District-granular route means, and R2 deliberately does not take it.*

**15. A District-granular free-flow tree concentrates the city onto a skeleton, and it is decision 11
arriving from a third side — NEW, produced by R8.** R2 found the **representative funnel** at the
destination node and priced it at 412% `v/c`. R8 went looking for it in a closed loop and **could not
make it bind**: excluding the one-hop funnel and excluding a four-hop convergence zone give readings
identical to the printed digit, because only *destinations* converge — origins are scattered real
nodes, and arrivals divide across 121 Districts.

**What binds is the tree, upstream of the representative.** At the operating load R8.0 selects,
**87.3% of all volume sits on the busiest 1% of volume indices and 90.9% of car indices are empty**,
at **13% of the network's holding capacity** — with capacity itself confirmed realistic (3,600 veh/h
per Segment reduces to a two-second headway, textbook saturation flow). **The network runs out of
*routes*, not road.** One shortest-path tree per District funnels a whole city onto a skeleton and
leaves nine tenths of the road unused, which is *why* Sight has somewhere to redistribute to and why
it lowers every conditioned congestion measure while raising an unconditioned quantile.

Three things follow and they land in different places. **Decision 11 has been argued as a question
about access nodes and is really a question about shortest-path trees**, which is a different fix —
multiple representatives do not disperse a tree. **Session M is owed it as a third defect in the same
column**: if a maintained table is structurally wrong by 16.58%/149.73% *and* concentrates traffic
onto 1% of the network, that is not the same trade M has been arguing. And it is the reason **there
may be no load at which this network is both congested and resolvable** under such a tree — dilute at
one end, past the BPR clamp at the other. *Recommended handling: R7 states it; it belongs with
decisions 11 and 13 and it is the same question all three are circling.*

**5a. The sun arc's phase widths, which the corpus names and never sizes.** *Dawn, morning peak,
midday, evening peak, night* appear in `02 §1.2` and `01 §7` with no durations, so **no peaking factor
exists anywhere** and every load figure in the corpus is a Day-average of a Day that has a rush hour.
S2 sweeps it and reports a curve; it does not choose. But the widths are almost certainly
**hash-bearing** for the same reason the matrix refresh cadence is — they decide how concentrated
demand is, and two peak widths produce two cities. Filed alongside decision 2.

**6. ~~`06`'s S2 specification is stale and should be struck.~~ DISCHARGED, session nine**, by deletion
rather than correction, per `adr/0042`: `06` no longer carries spike specifications at all. `0003` and
`spike-results` own them.

**7. R0's timing table is owed a re-capture — NEW, produced by R0. DISCHARGED by R1's sitting.** It
ran under the `powersave` governor, unpinned, which is exactly the machine-state defect
`spikes/S4.Kernels/tools/kernel-run.sh` exists to prevent; the harness printed its own governor, which
is how this was known rather than assumed. `tools/routing-run.sh` now takes R0 and R1 together under
`performance`, turbo enabled, pinned to one physical core, and `spike-results` quotes that capture
throughout. **Every count is bit-identical between the two captures**, which is the determinism check
the debt turned out to buy on the way past.

> **It leaves one thing behind, and R7 owns it.** Driving `None` — the first row the process times —
> moved **1.64×** between the unpinned and pinned captures while driving `Chebyshev` moved 0.04%, and
> the two *pinned* captures agree to 0.2%, so the cause is pinning rather than frequency and it is
> reproducible rather than noisy. The standing
> hypothesis is tiered-JIT background compilation sharing the one visible logical processor
> `taskset` leaves. *R7 must not publish a first-timed absolute without re-running the ladder in
> reverse order or with tiering disabled* — the old instruction, narrowed to the case that survived it.

---

## What this spike deliberately does not do

**It does not simulate traffic.** No Lanes, no IDM, no Overlaps, no Switch Lanes. Milestone 6 owns
those and `adr/0016` already settles their shape. S2 stops at *which Segments does this Trip
traverse, and how long does it take*.

**It does not settle the Microscopic Cap.** The Cap is a compute and behavioural constant — S4 proved
it is not a memory one, at 90.3 MiB for the entire 30,000-Segment network Microscopic — and setting it
needs a built traffic model. S2 may *inform* it via R2's ratio, which is the first quantitative thing
anyone will have been able to say about it.

**It does not choose Districts.** Player-drawn versus automatic is a design question owned by `06`
and blocking milestone 5c. S2 sweeps the zone count and hands back a curve.

**It does not open Phase 2.** [`0003`](0003-build-plan.md) is explicit that Phase 2 stays unplanned
until S0 runs, and S2 reporting does not change that. What S2 produces is a number and a router, not
a schedule.
