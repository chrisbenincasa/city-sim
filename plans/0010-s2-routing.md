# 0010 — S2: the routing ceiling

> Spike S2 of [`0003-build-plan.md`](0003-build-plan.md), on the parallel track. Spike definition in
> [`06-roadmap.md` §Phase 0](../docs/06-roadmap.md). Prior art and the standing of each option in
> [`references.md` §2](../docs/references.md). Decisions under test:
> [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md),
> [`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md),
> [`adr/0020`](../docs/adr/0020-one-live-world-and-settlements-are-derived.md),
> [`adr/0034`](../docs/adr/0034-fields-are-sorted-by-source-geometry.md).

**S2 is the project's top risk and it is the only one argument cannot close.** It decides whether the
4096² map survives, it owns the Chunk's real size, and it produces the router that milestone 5c
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

**One, and it is cheap.** Not a design argument — a missing definition.

**`Segment` has no entry in [`CONTEXT.md`](../CONTEXT.md), and S2 cannot proceed without one.** The
word is used twenty times in that file — in Fidelity, Stress, the Audit, the Microscopic Cap,
Promotion and Demotion, Traveller, the VDF and Upkeep — and defined nowhere. It is the unit the
Microscopic Cap counts, the unit Fidelity attaches to, the unit the VDF is evaluated on, and the unit
[`adr/0035`](../docs/adr/0035-infrastructure-is-priced-by-what-it-consumes.md) prices Upkeep against.
`CONTEXT.md`'s own first rule is that a term is added there before it is used.

S2 is the first work that must *count* Segments, and it therefore has to pick a meaning: a Road Graph
edge, a run between Junctions, or a Tile-length edge. **Those differ by more than an order of
magnitude on the same city**, which makes the ~30,000-Segment placeholder meaningless until the word
is fixed. Picking it silently inside a spike is how a definition gets welded in by a benchmark.

*Handling: one `CONTEXT.md` entry, before task R0. It is not an ADR — it is a word the corpus already
uses and already means something by.*

**Not a gate, but a parameterisation requirement.** Whether Districts are player-drawn or automatic is
open and [`06`](../docs/06-roadmap.md) says it blocks milestone 5c. It does not block S2, because a
District is *the granularity of the travel-time matrix* and S2 can take the zone count as a parameter
and report a curve — which is exactly what K0 did with the unset Microscopic Cap, and the precedent is
good. **S2 must not choose a zone count. It must report against a swept one.**

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
| Trips started per Tick | **~232** | 1.9M ÷ 8,192 |
| Segments | ~30,000 | **placeholder** — no road-density figure exists in the corpus |

**S2 is sized against the derived figures, and the ledger says why:** *a routing design that passes at
400k/Day and fails at 1.9M is the exact failure that spike exists to catch.* Against `06`'s 30k the
spike is undersized roughly 2× on Trips and 5× on Legs.

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

### R0. The synthetic Road Graph, and the denominator

No routing number means anything without a graph that resembles the real one and a plain-search cost
to divide by.

- Generate a 4096² graph: grid-snapped Streets falling out of the Tile grid, freeform Arterials with
  authored Junction pieces, **one graph with mode masks** rather than two networks
  (`CONTEXT.md` → Road Graph, and `03 §3.7`). Mode masks are not optional decoration here — a
  multi-Leg Trip is routed by a single mode-aware search, and a router measured without them is
  measuring the wrong search.
- Parameterise Segment count and report footprint as a curve, as K0 did for the Microscopic Cap. The
  ~30,000 figure is a placeholder and S2 is the first thing able to say what road density a 268 km²
  city at 1M actually implies.
- **The denominator: one uncached point-to-point search** — A\* with a Euclidean heuristic, integer
  costs, no hierarchy and no cache — over the O-D distribution R1 derives. Every later figure is
  reported against this, never against a Tick budget.
- Record machine, SDK, governor and the denominator's own timestamp.

### R1. The travel-time matrix — the prescribed first measurement

- Build the zone-to-zone matrix. **Sweep zone count** rather than choosing one; the corpus's only
  figure is ~100–400 zones from [`plans/0001`](0001-foundational-design.md), which predates the 1M
  target and cannot be carried forward unexamined.
- Measure four things separately, because they size different decisions: **cold build cost**,
  **incremental rebuild against a dirty region**, **resident size**, and **the O(1) read** the choice
  loop performs.
- The read is the one that matters most. `02 §5.8` makes *never resolve a route inside the choice
  loop* a rule, named as the one thing UrbanSim gets architecturally right that this design must not
  violate. **If the matrix read is not genuinely O(1) and cheap, that rule is unenforceable** and the
  finding is larger than S2.

*Decides:* whether the matrix can carry the choice loop, and therefore whether R4 is worth running.

### R2. What work is actually left — and the question nobody has asked

**This is the task the prescribed order exists to reach, and it contains a question the corpus has
never put in writing.**

A Traveller on a Statistical Segment is defined as *an origin, a destination, and an arrival Tick*
(`CONTEXT.md` → Traveller). If the matrix supplies the arrival Tick, **that Traveller may never need a
concrete path at all** — and the routing load collapses by orders of magnitude.

But Stress is `volume / capacity` (`CONTEXT.md` → Stress), and volume has to be accumulated from
somewhere. Something must know which Segments a Trip traverses, or no Segment ever has a volume, the
VDF has no input, and Fidelity has no trigger.

**So R2 asks: does a Statistical Trip need a concrete path, or only an arrival time and an
assignment?** The two answers give different spikes and a different game:

- **Arrival time plus an assignment** — volume comes from a periodic traffic assignment over the
  matrix, and per-Trip pathfinding is needed only for Travellers that become Microscopic. Routing load
  falls to the Microscopic Cap's scale, and the Cap is the constraint rather than the router.
- **Concrete path per Trip** — ~232 searches per Tick minimum, before cache hits, and the router is
  the constraint.

Measure both. **Report the ratio between them, because it is the number that decides how much of the
rest of this spike matters.**

*Decides:* the real per-Tick routing load, and whether R3 and R4 are answering a live question.

### R3. HPA\*, and the Chunk size it owns

[`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md) makes a claim S2 must test rather than
inherit: the Road Graph *"arrives pre-partitioned, because the Chunk grid is already the pathfinding
cluster, which is most of what HPA\* wants handed to it."* If true, HPA\*'s usual preprocessing cost
is largely already paid.

- Build HPA\* over the Chunk grid. **Sweep Chunk size** — this is where S2 discharges its ownership of
  it, and `05 §5`'s role table says pathfinding wants *larger, and loudly* at 32×32, where a cluster
  is 128 m on a map whose commutes cross tens of kilometres.
- Measure per size: preprocessing cost, per-query cost against R0's denominator, resident size, and
  **invalidation cost on a single edit** — the abstract graph's repair, not a rebuild.
- Report the render-streaming side of the Chunk trade as *not measured here*. `05 §5` says that role
  is two-sided with a bottom to find, and S2 can only see one side of it. **A Chunk size chosen from
  pathfinding alone is a recommendation, not a decision**, and the plan says so now rather than
  letting the spike's silence imply otherwise.

*Decides:* Chunk size, on the pathfinding axis. Still on the *cannot be retrofitted* list — cheap now,
pinned by the save format from milestone 10.

### R4. DSDV distance-vector, if R2 leaves it live

**Conditional on R2.** If the matrix carries the choice loop and Statistical Trips need no concrete
path, the many-to-many argument for distance-vector has evaporated and this task is written up as
*not run, and why* rather than skipped silently.

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

### R5. The edit storm — the test the city actually imposes

The measurement that separates a routing design that works from one that works on a static graph.

- Apply a realistic edit rate — roads drawn, roads deleted, a district bulldozed — while routing at
  full load, and measure both candidates through it.
- **Epoch semantics are under test here, not just performance.** Cached routes record the Epoch they
  were computed under and revalidate lazily on next use, **never a global flush**
  (`CONTEXT.md` → Epoch). A candidate that needs a global flush on edit has failed a stated design
  commitment regardless of its throughput.
- Record the worst single-Tick cost, not the mean. S4's K6 established that the quantile hides the
  event: a run whose worst iteration was 100.2 ms read 2.462 ms at p99.9.

*Decides:* whether either router survives the core verb, and it is the task most likely to reverse
R3's and R4's ranking.

### R6. The route cache, and `adr/0006`

[`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-agent.md) permits route caching **keyed
by origin-destination pair, never by agent**, invalidated lazily against the Epoch. That is exactly
the pending-state class [`adr/0006`](../docs/adr/0006-no-collection-grows-with-elapsed-time.md) calls
dangerous, and `adr/0006`'s reversal criteria are *"Nothing."*

**No eviction policy is stated anywhere in the corpus.** `adr/0017` shows the pattern to follow —
fixed capacity, least-used eviction — and nobody has written it down for routes.

- Fixed-capacity, O-D keyed, Epoch-invalidated. Measure hit rate against R1's real O-D distribution
  rather than a uniform one, which would flatter it.
- Run long enough to demonstrate **no collection and no magnitude trending upward at steady state**,
  per the definition of done. A cache that grows is not a cache.

*Decides:* the eviction policy, which is owed to `adr/0012` as an amendment.

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
| Routing exceeds **10% of the 15.6 ms Tick budget** at 1M on the *desktop* (i5-10400, DDR4-2133), sustained, with matrix refresh amortised | The map falls back to **2048²**, which `05` already documents as the fallback |
| The travel-time matrix read is **not O(1) and cheap** | Larger than S2. `02 §5.8`'s rule is unenforceable and the choice loop's design reopens |
| Either router needs a **global flush** on a Road Graph edit | That candidate is out on a design commitment, not on a number |
| The route cache **grows at steady state** with no bound | `adr/0006` violated. Fix the cache, not the ADR |
| DSDV's routing tables exceed the **whole world's 172.3 MiB footprint** | Distance-vector is out on memory alone |

**The 10% figure is chosen here and nobody has ratified it.** The corpus states no routing budget. It
is offered against two anchors: S4 measured the Event Wheel and its wake gather at **1.80%** of the
Tick at the most pessimistic wake rate, and
[`adr/0037`](../docs/adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) names the
order of suspicion for a slow Tick as *the Microscopic Cap, routing, and the Sweep Rule schedule*.
Routing being allowed five times the Wheel and still leaving 90% is defensible; it is not derived.
**It is recorded as unratified in [`0002`](0002-open-questions.md) on the day it was chosen.**

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

**3. The routing Tick budget share** — the 10% above.

**4. Zone count for the matrix, and road density.** Both swept rather than chosen, both owed a figure
once S2 reports. Road density is the input the ~30,000-Segment placeholder rests on and it exists
nowhere in the corpus.

**5. The route cache eviction policy** — owed to `adr/0012` as an amendment, per R6.

**6. `06`'s S2 specification is stale and should be struck.** "30k Travellers" predates the 1M target;
S1's "20k Buildings" is stale for the same reason and is not S2's to fix.

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
