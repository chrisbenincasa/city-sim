# 0020 — The Road Graph (milestone 5a)

> The slice brief for [`06`](../docs/06-roadmap.md) milestone **5a**, *Road Graph and Streets*.
> Decisions built: [`adr/0014`](../docs/adr/0014-streets-snap-arterials-are-freeform.md),
> [`adr/0040`](../docs/adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk.md),
> [`adr/0047`](../docs/adr/0047-routing-never-keys-on-the-district.md),
> [`adr/0012`](../docs/adr/0012-routing-intent-lives-in-the-traveller.md) (the Epoch half only).
> Design realised: [`02 §2`](../docs/02-simulation-model.md), [`03 §3.7`](../docs/03-agent-architecture.md).
>
> **This is a planning document and therefore cites rather than owns**
> ([`adr/0042`](../docs/adr/0042-a-planning-document-cites-and-a-design-document-owns.md)). Every
> figure below names its owner. If this document and its owner disagree, the owner is right.

## Status

**BUILT 2026-08-11. All seven tasks, and the definition of done is met but for its last bullet** —
`spikes/S2.Routing/` is still on disk, deliberately, and the paragraph below says why. The suite is
**1,060 green**; the graph is in the State Hash, `--roads` prints it, and the golden baseline has been
re-recorded because two tables joined `World._tables` and both Rulesets changed content.

**Four findings outlive the slice, and the first two are corrections to this brief.**

**Task 1's *Arcs as typed tables* could not be built as written, and the reason is a property of the
State Hash rather than a preference.** `Rows.Fold` folds the allocator's four scalars —
`_slotCount`, `_liveCount`, `_freeHead`, `_nextId` — **before** it consults any column's disposition,
so a wholly-derived table registered in `World._tables` folds its own rebuild history. Two worlds
identical in every Segment would hash differently because one had rebuilt its adjacency more often
than the other. The Arcs are therefore a **non-`[Table]` array holder** (`CellResidency`'s precedent),
which costs the per-field declaration and buys a hash that is a function of the city. Recorded in
`RoadArcs`'s own docstring, because the next person to reach for `[Table]` here will have the same
good instinct.

**Task 2's mode mask is saved on the *Arc*, and the spike had it backwards from its own argument.**
`spikes/S2.Routing/Graph/Modes.cs` argues at length that the mask must be per direction — one-way
streets are the whole reason — and `RoadGraph.cs` then saves the *Segment's* mask and derives the
Arc's from it. **The spike never generates a one-way street, so no measurement it ran could have
noticed.** Settled the other way by
[`adr/0072`](../docs/adr/0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md), with
an invariant watching the two agree. *A benchmark cannot refute a claim about a case it never
constructs* — which is `adr/0043` seen from underneath, and it is why porting reasoning is not the
same act as porting code.

**Severance is a property of the grid's fineness relative to the barrier, not of the barrier**, and
the acceptance test found this by failing. Written first against a 512-Tile block with two Arterials
it reported no Severance at *any* crossing density — correctly: an Arterial destroys the Streets it
runs over, so on a coarse lattice it destroys a handful of enormous Streets and everybody walks round
the end of it. **The crossing dial does nothing at all until there is enough Arterial per unit of
grid, and then it does a great deal.** At 256 Tiles and eight Arterials the largest walkable component
goes 72 → 289 of 353; at 512, or at two Arterials, it does not move. Both halves are now `§D1` rows.

> ### ⚠ AMENDED 2026-08-11 — the sentence above is right and the inference drawn from it is backwards
>
> **The conclusion `§D1` took from this — *"the shipped 32/8 sits well inside the regime where the
> dial works"* — is false, and the shipped Ruleset severs nothing at all.** A sweep of **240**
> `[roads]` configurations, each normalised against an Arterial-free control of the same lattice
> (`--roads`, 2026-08-11), reads:
>
> | block / Arterials | crs=1 | crs=2 | crs=4 | crs=8 | crs=16 | *never* |
> |---|---|---|---|---|---|---|
> | **32 / 8** — **shipped** | 0.0% | 0.0% | **0.0%** | 0.0% | 0.1% | 2.0% |
> | 32 / 32 | 0.0% | 0.0% | 0.2% | 0.7% | 2.7% | 78.1% |
> | 128 / 8 | 0.0% | 0.0% | 0.1% | 0.4% | 0.5% | 35.3% |
> | 256 / 8 | 0.0% | 0.0% | 0.7% | 17.3% | 67.8% | 75.1% |
> | 512 / 8 | 0.0% | 0.0% | 46.9% | 61.7% | 80.2% | 81.5% |
>
> **Severance is monotone *increasing* in block size at fixed Arterial count**, and the two
> observations this brief generalised from — *512 with two Arterials severs nothing, 256 with eight
> severs a lot* — **differ in two variables**. Attributing the difference to Arterial-per-unit-grid
> and concluding that finer is safer reads the confound in the wrong direction. ***Two observations
> differing in two variables support no claim about either.***
>
> **The mechanism is now derived rather than observed, which is what makes the direction predictable.**
> `foot_crossing_every` states a **ratio** and what reconnects a city is an **absolute count**. An
> Arterial severs one Street per block it crosses, so halving the block size doubles the Streets it
> severs and the same ratio leaves twice as many crossings standing. Measured, at eight Arterials and
> a dial of 4:
>
> | block size | 32 | 64 | 128 | 256 | 512 |
> |---|---|---|---|---|---|
> | crossings kept | 306 | 150 | 73 | 35 | **16** |
> | walkable nodes cut off | 0.0% | 0.0% | 0.1% | 0.7% | **58.0%** |
>
> Severance appears at **16 crossings and not at 35**, on both axes, which is why the Arterial count
> and the block size are the same dial seen twice.
>
> **Three consequences, and the last is the largest.**
>
> **`--roads` announced Severance over a city that has none, for the whole of the slice.** Its verdict
> was `FootComponents > CarComponents`, and the shipped world reads **66 against 8** because an
> Arterial lays its own junction nodes, those carry no foot Segment, and a node with no foot Segment
> is trivially its own foot component. **65 of the 66 are motorway junctions.** `RoadSeveranceTests`
> found this on day one and filtered it out with a `Walkable` predicate — and **the filter never left
> the test file**, so the runner kept the bug the suite had already diagnosed. *A predicate discovered
> in a test and left there is a correction with a blast radius of one call site.*
> `RoadConnectivity.StrandedOnFoot` is now the measurement and `--roads` prints it.
>
> **`foot_paths_per_thousand_blocks` is a second Severance dial and nothing named it as one.** At the
> shipped lattice with crossings set to *never*, cut-throughs at 40 give **2.0%** and at 0 give
> **43.6%** — a 22× difference, on the axis `CLAUDE.md` and `minimal.toml` both describe as though
> `foot_crossing_every` were the only lever.
>
> **⚠ Every Severance figure in this corpus, the tables above included, was a single draw — because
> `--roads` refused `--seed`.** The Arterial polyline comes from the world key, so Severance is a
> function of the seed. It is not a small effect: at 256 Tiles and *eight* Arterials with the dial at
> 8, twelve seeds range from **0.0% to 23.5%**, and **four of them are under 1%**. `--roads` now
> accepts `--seed`; the tables above are still one draw per cell and their marginal rows should be
> read as noise. Only `rulesets/severance.toml`'s own rows are characterised over seeds. ***A
> generator whose output cannot be varied cannot be characterised***, and the guard that stopped it
> being varied was written to refuse something else — a seed reached the refusal by category, as *a
> flag that implies a fresh session*, rather than by anyone asking what a Road dump reads.
>
> **What the slice owes as a result.** `rulesets/severance.toml` is the demonstration rung — 256-Tile
> blocks, 16 Arterials, a dial of 16 — chosen because its **floor across seeds is 32.3%** where eight
> Arterials would have been a coin toss. `RoadSeveranceTests` asserts both files over eight seeds.
> **And all of it measures *disconnection*, which is the smaller half**: a pedestrian who must walk
> four hundred metres to the nearest crossing is severed in every sense a player would recognise and
> is perfectly connected here. That half needs a shortest path over the foot subgraph, and **nothing
> in `Borough.Core` searches the graph yet**, which is milestone 5b's first unowned decision.

**A component count over a subgraph counts the *other* mode's nodes as isolated singletons.** The
severance test's first version found a severed pair in every graph including the fully-crossed one,
because an Arterial lays its own nodes, those carry no foot Arc, and a node with no foot Arc is
trivially its own foot component. The pair it kept returning was *an intersection, and a point in the
middle of a dual carriageway*. Severance is a claim about two places pedestrians **use** being cut
apart, and that predicate has to be stated rather than inferred from a count — which is why
`RoadConnectivity` reports `LargestFoot` beside `FootComponents` and why `--roads` prints the caveat.

**Two mis-documented behaviours were found by tests written against prose rather than code**, both
outside this slice's own surface and both fixed here. `TravelTime.Impassable`'s remarks claimed that
adding to it *"would overflow, which is the correct failure"*; ambient arithmetic in `Borough.Core` is
unchecked, so it wrapped to a large **negative** cost that a relaxation step would have preferred to
every real route — `operator +` now saturates. And `Speed.FromKilometresPerHour`'s exception text put
the format's ceiling at ~682 km/h when the guard enforces **44,739**; 682 is the loader's *sanity*
bound, which is a different thing in a different place. **A documented failure mode with no
implementation is `0012` *Cause 1* with the copy count set to zero.**

**Why the harness is still on disk.** The deletion's closing condition — *the port is done and nothing
reads the harness* — is met on both halves: nothing in `src/` or `tests/` compiles against
`spikes/S2.Routing/`, and the only remaining references are two doc-comments citing files by path.
**⚠ RE-BLOCKED 2026-08-11, on a new gate and not the old one.** Another session is doing further research *inside* `spikes/S2.Routing/`, so the harness is live work rather than a spent artefact. **The 5a gate is discharged and this is a different one**: the closing condition is no longer *the port is done and nothing reads the harness* — both halves of that are met — but **that session finishing with it**. Keep the two apart; a deletion blocked twice for unrelated reasons is exactly the kind of row that gets struck for the wrong one. It is a **29,719-line deletion across 51 tracked C# files** — 92 tracked
files and 42,914 lines including the 36 `results/` reports. *(The first figure written here, 58 files
and 29,934 lines, counted generated sources under `obj/`; a deletion is measured in what git tracks.)* `Borough.slnx` still lists the project.

---

## Why this slice, and why now

**No session gates 5a.** The board's argument-track gate table routes every open session at something
that *runs on* the graph rather than at the graph: **E** → 7a/7b, **F** → 5b, **G** → 6, **H** → 8,
**I** → 5c, **J** → 10, **L** → Phase 3, **M** → R6's remainder. Session **D** has run. **5a appears
in no row.**

The one entry that could be read as covering it is **K2**, *"planning Phase 2 at all"* — which
re-derives Phase 2's ordering and places the seventeen mechanisms `06` leaves with no milestone.
**5a is the one milestone whose position in that ordering cannot move**: 5b, 5c, 6, 7a, 7b and 8 all
run on the Road Graph, and nine of the seventeen homeless mechanisms are road-dependent. Building it
pre-empts nothing K2 decides. This is the test `docs/spike-results.md` already writes down for
exactly this situation — *for each blocked row, ask what the gate's reason does not cover, and check
whether that remainder is runnable today.*

**5a's own risk is retired in prose.** `06` names it: *geometry leaks into the simulation and the
routing graph stops being uniform*. `adr/0014` answers it — Streets snap to the grid, only Arterials
are freeform, and **the Road Graph is uniform nodes and edges regardless of which of the two a road
is**; the simulation never sees a spline. `adr/0040` moved the pathfinding partition off the Chunk
and `adr/0047` keeps routing off the District. There is no argued question standing in front of the
build.

**And the absence is now the largest single blocker in the corpus.** A survey of every road-traced
dependency found **13 named holes in `src/`**, **one fully-empty Tick phase**, **~30 documented
mechanisms**, **23 ADRs**, **21 rows in `plans/0002`** and **4 board-level blockers**. Three of those
outrank the rest:

1. **The `pool` scope** — `RuleEngine.cs:803` throws, and that one throw gates the District Pool,
   per-District prices, the whole Goods economy, Utilities distribution, Shipments and slice 7's
   re-filed task 10b. It is the reason `02 §4.3`'s **own worked bakery example is unreachable**.
2. **Trip generation (5b, immediately behind this slice)** — the multiplicand behind
   [`0013`](0013-tick-budget.md)'s dominant row (routing is **60–67 of the ledger's 114 points at
   4×**, and R6.3 showed its multiplicand counts the wrong event), and the named owner of 13 ledger
   rows.
3. **The line-source and catchment queries** — Noise, near-road pollution and Amenity, which cascade
   into Desirability → the land-value target → the choice model. **The land-value Layer is built,
   double-buffered, and runs every 256 Ticks chasing a column nothing writes.**

Under [`adr/0070`](../docs/adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) every one of
those absences classifies as **unbuilt**, not *refused* — so none of them may generate a compensating
design position, and the answer to all of them is the same: **build the mechanism.**

---

## What this slice is

**A Road Graph that exists, hashes, saves, and can be asked what is connected to what.** Nothing
moves on it. That is 5b.

| In | Why |
|---|---|
| Nodes, Segments and Arcs as typed tables in `Borough.Core.Space` | The thing itself |
| Per-field `saved AND hashed` / `derived AND rebuilt` declaration | `adr/0003`; the arcs are derived |
| Mode masks **on the arc** | `03 §3.7` — one graph with a pedestrian subgraph, never two networks |
| A generator, so a world has roads | `adr/0014`, ported from the spike |
| The per-Segment **Epoch** | `adr/0012`; the invalidation contract every later consumer needs |
| **Connectivity components** | What makes `pool` a real membership question rather than a lookup |
| `--roads` in the headless runner | `06` rule: there is something to *look at* |
| Invariants + State Hash coverage + a long run | The definition of done |

| Out | Owner |
|---|---|
| Routing, travel-time matrix, route cache | 5c, and S2 has measured all of it |
| Trips, Legs, the pedestrian Leg | 5b |
| Lanes, IDM, Microscopic fidelity | 6, 7a; S5 has priced the kernel |
| Parking, sheds | 8 |
| The `pool` scope's implementation | **Immediately after this slice** — see below |
| The Lot subdivider | **5a-bis** — see *What this excludes and who owns it* |
| A `build_road` command | **5a-bis** |

**`pool` is deliberately just outside.** This slice delivers the *connectivity* it has been waiting
on, but `RuleEngine.cs:803` is explicit that the remaining hazard is economic rather than spatial —
*"implementing this as a Bin lookup ships an unconserved economy, and no refusal can catch that."*
`adr/0050` makes crossing an ownership boundary a trade. So `pool` is a **separate, small, adjacent
slice** whose blocker this one clears, and it must not be smuggled in as a fifth task.

---

## The prior art, and what a port actually costs

`spikes/S2.Routing/` holds **~2,900 lines** of directly relevant, measured, integer-only code. This
is the strongest starting position any slice in this project has had, and it is why board row 4 and
[`0010`](0010-s2-routing.md) now carry a **⚠ HOLD, 2026-08-11** against deleting the harness: the
deletion moves to the **end of this slice**, and its closing condition is *the port is done and
nothing reads the harness* rather than *the report is written*.

| Source | Lines | Verdict |
|---|---|---|
| `Graph/RoadGraph.cs` | 311 | **Ports nearly whole.** Directed CSR, columns already split saved/derived, `AssertWellFormed`, per-column footprint |
| `Graph/GraphGenerator.cs` | 569 | **Ports with edits.** Realises `adr/0014` with genuine Arterial severance, foot crossings and ramps |
| `Graph/Modes.cs`, `IntegerGeometry.cs`, `Units.cs` | 255 | **Port, with `Units` split** — see the cost-unit decision |
| `Graph/GraphParameters.cs` | 90 | **Becomes Ruleset data**, not a parameter struct |
| `Graph/CounterHash.cs` | 125 | **Discard** — `Core` has `Randomness` |
| `Matrix/Connectivity.cs` | 263 | **Ports.** The component test `pool` needs |
| `Matrix/{TravelTimeMatrix,OneToAll,Districts,Congestion,Phase}.cs` | 1,294 | **Out of scope** — 5c |

The honest caveat: the spike compiles the arithmetic substrate **in by source** and can name nothing
else of `Core`. A port is not a file move — every column must be re-declared through
`Rows.Saved`/`Rows.Derived`, which is what allocates it and what closes the State Hash's coverage
hole. Budget the port as a rewrite with a very good reference, not as a copy.

---

## Tasks

**1. Nodes, Segments and Arcs as typed tables.** `RoadGraphTable` in `Borough.Core.Space`, on
`LayerCellTable`'s pattern. Saved and hashed: node coordinates, segment endpoints, length, capacity,
free-flow, mode mask, and the Epoch. Derived and rebuilt: the whole CSR arc array — `ArcStart`,
`ArcTarget`, `ArcSegment`, `ArcModes`, traversal costs — because an arc is a function of the
Segments. Fidelity is derived (`adr/0007` — it follows Stress, which does not exist yet, so it is a
declared column with a constant writer and a named hole).

**The traversal and speed columns are typed by
[`adr/0071`](../docs/adr/0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md)**
— Q16.16 Ticks and Q16.16 Tiles/Tick, each its own `readonly record struct` over an `int`, offering
only dimensionally sound operations. The State Hash does not move: a record struct over an `int` folds
what the `int` folded.

**2. Mode masks on the arc, and severance.** `03 §3.7` — *pedestrian and vehicle edges live in one
graph with mode masks*, and severance works because *Arterials carry no pedestrian edges except at
authored junction pieces*. A test that walks the foot subgraph across an Arterial and fails without a
crossing is the whole point of the task.

**3. The per-Segment Epoch.** `adr/0012`'s invalidation contract, built now so every later consumer
(routes, Parking Sheds, Amenity catchments) inherits it rather than inventing one. The rung is
**per-Segment on both consumers**, measured — 96% retention against a single counter's 9%, and
cheaper; and R5.6 found per-Segment-witnessed-by-paths is the only shed rung that fits, at 26.10% of
a Tick against per-cluster's 1,351.24%.

**The known unsound edge is road *addition*** — a new Segment invalidates nothing, because nothing
references it — **and the route half of that is closed.** `adr/0012` carries the contract: a **bound
checked at use**, with a proximity wake over it, and explicitly *not* a rotation. Build that here.
The **shed** half inherits the shape and not the parameter, which is session **M**'s and belongs to
milestone 8; nothing in 5a needs it, and `0002` notes nobody has yet typed what a shed's *use* even
is.

**4. The generator.** Port `GraphGenerator`. Its parameters become **Ruleset data** under a
`[roads]` table, not a `const` and not a parameter struct — `adr/0015`, and the same rule that put
`[layers]` and `[placement]` in TOML. Density is the one number with a measurement already:
R0 swept it and reports **16.20 km/km²**, unratified, with `0002` recording that nothing yet says
whether that describes a real city.

**This task owes `0002` §D rows and `adr/0071` does not discharge them.** The free-flow speeds — 50
km/h for a Street, 90 for an Arterial, 5 for a walk — are **hash-bearing** the moment they enter a
Ruleset, and `adr/0052` wants a named ratifier beside each on the day it is written down. Choosing a
representation ratifies no value. Road density is a fourth.

**5. Connectivity components.** Port `Matrix/Connectivity.cs`. Union-find over the Segment set,
derived and rebuilt on the Epoch. This is the task that turns *"a District whose internal Road Graph
is broken must still fail to distribute"* (`04 §6`) from prose into something testable, and it is the
deliverable the `pool` slice consumes.

**6. `--roads` in the headless runner.** The milestone's *something to look at*. `--zones` set the
precedent — including refusing to run rather than degrading when the Ruleset declares nothing, so an
empty picture never reads as a broken mechanism. Print the graph and its components.

**7. Invariants, hash and the long run.** `AssertWellFormed` becomes real invariants on `02 §10`'s
staggered tier. The graph folds into the State Hash. A 100,000-Tick run with generation and edits
shows no collection and no magnitude trending (`adr/0006`) — and note the standing warning from
slice 10 task 11: **a baseline records what a run *did*, so assert that both the severance and the
crossing branches were actually reached.**

---

## Decisions this slice must close

Three were open when the slice was scoped. **Two are settled by the prior art and one is genuinely
open** — and the open one is *arguable* under `adr/0043`, so a sitting may close it and no spike is owed.

**The cost unit — CLOSED 2026-08-11 by [`adr/0071`](../docs/adr/0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md).**
Travel time is **Q16.16 Ticks** and speed is **Q16.16 Tiles/Tick**, each as its own type over an
`int`, sharing `Fixed`'s audited arithmetic; `05 §121` is amended in place to say that **Q16.16 is a
scale rather than a meaning**. The finding that settled it was not in either candidate: `05 §121` and
`02 §2` **have been contradicting each other since before the spike ran** — one forbids the
representation of a speed that the other mandates — and `Units.cs` resolved it silently in `02`'s
favour. Applied literally, the old sentence rounds a walking pace of 3.66 Tiles/Tick to 3. **The
original text of this entry follows, because it is the reasoning the ADR was written against.**

*Original entry:* **The cost unit — OPEN, and the spike explicitly declined to settle it.** `Units.cs` reasons it
through and stops: the cost must be **time** (`02 §5.9`'s SC4 lesson — *the cost function used for
routing must be the same quantity used to judge trip failure, and the same quantity shown to the
player*); it must be **sub-Tick**, because a vehicle covers about one Segment per Tick so whole-Tick
costs make A\* minimise **hop count** while appearing to route on time; and the spike therefore used
**Q16.16 Ticks** while writing down that this violates `05 §121`'s *"Q16.16 is for sub-Tile positions
and nothing else"*, that the alternative is *"the same representation wearing a different name"*, and
that **the decision is for the corpus rather than for a benchmark.**
*Recommendation:* a **distinct named fixed-point type for travel time**, identical in bits to Q16.16
and convertible, so the arithmetic is shared and `05 §121` stays true. It costs one type and settles
the question in the direction that does not give one representation two meanings. Wants an ADR.

**Volume scope — SETTLED per-direction.** `GraphParameters.VolumeScope` swept both;
`RoadGraph.VolumeIndex` implements both; the cost is ~5% of graph footprint either way. `adr/0041`
attributes volume when a Traveller **enters** a Segment, and a one-way pair is not the same road in
both directions. Take the finer one.

**Cluster size — DEFERRED, and it is free to defer.** `adr/0040` narrowed it to 8 or 16 Chunks a side
without closing it, and R3 found **no cluster size fits routing into the Tick budget** — which is
5c's problem, not 5a's. The partition is *derived and rebuilt*, so it can arrive later without a save
migration. Do not build it here.

Still unowned and **not** blocking this slice: the junction complexity factor's derivation
(`03 §3.3`, needed by 7a), the Junction piece's data shape, diagonals, and foot-only Segment sizing.

---

## What this excludes, and who owns it

**The Lot subdivider.** `02 §2.2` specifies it fully — *Lots are generated, not painted*, every Lot
needs **frontage on at least one Street**, **Arterials grant no frontage**, and land that cannot be
given frontage stays unlotted. It has **no milestone anywhere**, which `plans/0012:373` flags: it
belongs *"either in 5a explicitly or in the no-milestone table"* and is currently in neither. `02`
also carries the admission this slice makes newly urgent: *"every Building is on the Road Graph by
construction is currently true **because there is no Road Graph**, not because of construction."*

**Decision: it is 5a-bis, planned here so it stops being homeless, and built after 5a lands.** The
reasoning is that 5a retires a risk about *the graph's uniformity* and the subdivider retires a
different one about *Lots being honest*; folding them doubles the slice and couples two acceptance
tests that fail for unrelated reasons. `Simulation.cs:311` already states the trade correctly —
*painting a region of Lots would stand in for more of 5a than painting one does, and every Lot it
invented would be one the real subdivider would have refused.* One painted Lot remains the right
placeholder until the generator is real.

**A `build_road` command** joins it in 5a-bis. `CommandKind.Connect` is declared at `Command.cs:18`
and throws at `Simulation.cs:332`; `01 §2` counts it among the player's five verbs and the corpus
calls road editing *the player's core verb* — and **nowhere specifies its command surface.** 5a's
generator gives the graph a producer without needing one, and putting edits in the Input Log is what
makes the Epoch exercised by replay, which is worth doing deliberately rather than as a seventh task
here.

---

## Definition of done

`CLAUDE.md`'s cumulative list, plus:

- The graph is in the State Hash, and a run with a different `[roads]` table produces a different hash
- Replay equivalence holds across a world with roads
- The foot subgraph is severed by Arterials and reconnected only at crossings, with a test that
  watches the unsevered variant fail
- Connectivity components are correct against a hand-built disconnected fixture
- `--roads` prints a graph, and refuses rather than degrading when the Ruleset declares none
- 100,000 Ticks with no collection and no magnitude trending, **and an assertion that both branches
  under test were reached**
- `spikes/S2.Routing/` is deleted, and nothing reads it

**Risk retired:** geometry leaking into the simulation. After this slice the simulation holds nodes,
edges, integer Tile coordinates and integer costs, and holds no splines, no metres and no seconds —
and `03 §3.7`'s severance is a property of the mode masks rather than of a second network.
