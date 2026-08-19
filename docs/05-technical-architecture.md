# 05 — Technical Architecture

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Pillars in [`00-vision.md`](00-vision.md). The world model and tick phases in [`02-simulation-model.md`](02-simulation-model.md); movement fidelity in [`03-agent-architecture.md`](03-agent-architecture.md).
>
> Governing decisions: [`adr/0001`](adr/0001-godot-and-csharp.md) (Godot 4.7 is the host), [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) (the core is C#, argued separately), [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) (engine-agnostic library), [`adr/0003`](adr/0003-deterministic-integer-simulation.md) (determinism), [`adr/0004`](adr/0004-typed-tables-over-ecs.md) (typed tables), [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) (stress-driven detail).
>
> This document specifies *how the code is arranged*. It does not restate the simulation rules — where a rule already lives in `02` or `03`, this document points at it.

---

## The budget this document is sizing against

Every decision below is justified by *scale*, and for a long time the scale was never named — leaving a 100× gap between `03 §2.1`'s million-Citizen sizing and a 10k figure that had drifted in from elsewhere and was quietly deciding things it had no business deciding.

**Two figures, because they justify different decisions:**

| Figure | What it is | What it justifies |
|---|---|---|
| **10,000 Citizens** | the first hour, on a few hundred Tiles of unlocked land | *responsiveness* — it must feel immediate on a laptop |
| **1,000,000 Citizens** | the late game, on a fully-developed 4096² map | *does it still run* — the Event Wheel, chunking, coarse layers, sampled pedestrians, and the two-tier fidelity model exist for this figure and for no other reason |

**1M is a floor, not an aspiration.** The design's late game is sprawling polycentric cities with interdependent Settlements, which `adr/0020` built machinery for and which no smaller target exercises. The population is expressed as a derivation rather than a constant, so it stays correct if the map changes:

```
target = map_area × mature_density × buildable_fraction
       = 4,295 km² (16384² Tiles @ ~4 m) × ~3,700/km² × ~0.063  ≈  1,000,000
```

⚠ **Read that as a consistency check and never as a derivation.** `mature_density` is an **output of the 1M target**, not an input to it — `plans/0002` §1's column is headed *"1M implies"* — so substituting it back reproduces 1M by construction and confirms nothing (`plans/0012` **Cause 5**). What does bracket it are two figures derived from the build: `lots_per_segment = 5` over 5a's 33,024 Segments, and `World`'s 225 Lots per 1,000 Citizens, giving **2,738 and 5,136/km²** at the shipped occupancy.

~3,700/km² is Los Angeles — sprawl rather than density, which is the point, and it is the **developed** density rather than the map-wide one. The buildable fraction is what changed on 2026-08-12: [`adr/0089`](adr/0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) takes the map to **16384² Tiles, 65.5 km a side**, so a 1M city occupies **270 km² — the whole of the old map — on 6.3% of the new one.** The city does not change size; it acquires a region around it. Unbuilt ground is a null ([`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)), so the other 94% costs four derived `int[]` arrays totalling 4.2 MB.

**The term this derivation was missing is distance, and it is why the map moved.** Area and density do not say how far apart two places are, and every mechanism the map is load-bearing for — the Commute Budget, Settlements, Severance, Hinterland edge choice — is a claim about travel. At 4096² the whole map is **0.9 Commute Budgets** across, so no Trip can exceed the Budget and the decline mechanism it exists to drive is inert everywhere; at 16384² it is **3.7–5.2**. A map is therefore sized by *how many commutes fit across it*, and the area figure is a consequence.

~~⚠ **The constant has not moved.** `CellGrid.WorldCells` is still 128, gated on road generation being scoped to developed land — `RoadGenerator` currently paves the whole map, which is the one place `adr/0021`'s *developed area, not map area* is false. See `plans/0002` ledger #2.~~ ✅ **BOTH HALVES ARE STALE — corrected 2026-08-16 by session T.** `CellGrid.WorldCells = 512` in `src/Borough.Core/Space/CellGrid.cs`, and the gate cleared first: `plans/0003` queue item 6 scoped `RoadGenerator.LayInto` to an extent in Tiles derived from `World`'s own 225 Lots per 1,000 Citizens, shipping **2026-08-13**, and the map flip landed the same day. ⚠ **The sentence complied with [`adr/0093`](adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) and went stale anyway** — it names a **symbol** rather than a time, so one lookup settles it. ***Naming a symbol makes a claim checkable; it does not make anybody check it.***

**What makes a map this large survivable is decided elsewhere and is load-bearing here:** progressive unlock by serviceability plus `adr/0021`'s sparse Chunks mean unbuilt land costs nothing, so map size is decoupled from early-game sparsity. Without both, 4096² would be an unplayable first hour.

**Two consequences that this figure, and only this figure, creates:**

- **Route computation is the project's top risk.** `adr/0020` already calls it *"the binding constraint on world size"*; at 4096² the Road Graph is 16× the 1024² case with ~400k Trips per Day behind it. Open question 1 (HPA\* versus distance-vector) and spike **S2** are therefore not an optimisation choice but the decision that determines whether this target is reachable. **2048² is the documented fallback** if S2 comes back badly.
- **The Microscopic Cap binds much harder.** It is a fixed world constant, so at 1M a far larger share of the network wants Microscopic treatment than the Cap can grant, and most of the city is permanently Statistical. `03 §3.5`'s divergence audit is consequently the thing standing between this design and a traffic model that is quietly wrong at scale.

*Named, not solved:* 1M Citizens is not the same problem as 1M **governable** things. A player placing individual service Buildings across 268 km² is a player-experience problem this corpus has no answer for, and `01-player §4` has never been grilled. It is a debt this target creates, and it should be paid before milestone 8 rather than discovered there.

---

## 1. Project layout

Five projects, and the split between them is the architectural decision. Everything else in this document follows from it. A sixth exists and is deliberately not one of the five — see below.

> **This said *four* until [`adr/0039`](adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md).** `Borough.Formats` was added when slice 5 settled the Input Log's encoding as text and found no home for the codec: the core may not spell verbs in words, and a copy in each shell would put two implementations behind a format whose entire purpose is that a log written by the game replays in the headless runner.

| Project | Contents | What it buys |
|---|---|---|
| `Borough.Core` | Pure C# class library, **zero Godot references**. Typed tables, integer and fixed-point maths, the Event Wheel, the Ruleset interpreter, `step(inputs)`. **This is the game.** | Reversibility of every other choice here |
| `Borough.Tests` | xUnit and BenchmarkDotNet. Determinism, invariants, save/reload equivalence, thread-count equivalence, allocation and tick-time benchmarks. | A correctness harness that needs no GPU |
| `Borough.Headless` | Console runner. Loads a Ruleset and an Input Log, fast-forwards thousands of Ticks per second, dumps State Hashes and aggregate series. | Balance testing as a batch job |
| `Borough.Formats` | The artefacts that spell things in words: the Input Log codec, the crash artifact that wraps it, and **the Ruleset loader — the TOML parse, every refusal, and name→id resolution** (`adr/0048`). References the core; referenced by both shells. | One implementation of a format two shells must agree on, and the one place a human-readable string may be produced |
| `Borough.Godot` | Thin shell. Per-Chunk `MultiMeshInstance3D` rendering, `Control`-based UI, a per-frame snapshot read out of the sim. | A renderer and a UI, and nothing else |

**A *shell* is a host application that drives `Borough.Core`, and there are exactly two — `Borough.Godot` and `Borough.Headless`.** The word is defined here rather than in [`CONTEXT.md`](../CONTEXT.md) because it names the project layout and not the city, and that file's siblings divide on exactly that line: `CONTEXT.md` names the city, [`PROCESS.md`](../PROCESS.md) names the calendar, and neither owns the architecture. A shell owns what the core may not — a renderer, a UI, file and console I/O, and **every string a human reads**. ⚠ **The word had two meanings until 2026-08-16 and one of them was inside the glossary**: `CONTEXT.md` used *shell* for the standing structure of an abandoned Building, which is the carrier of `02 §5.9`'s contagion, in a file whose stated rule is *every term, with exactly one meaning*. That use is now *the standing Building*, and the cost asymmetry is why this sense kept the word — one use against thirty-eight. ***A term with no entry is not thereby a term with one meaning***, and neither sense had an entry anywhere.

**`Borough.Core` is where the value is, and it is the only project that is expensive to replace.** The other three are consumers. If Godot were abandoned tomorrow the loss would be the shell; if the boundary were ever breached the loss would be the option to abandon Godot at all. That asymmetry is why [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) makes the zero-engine-types rule a **CI check** rather than a convention. A single `using Godot;` added under pressure would not be noticed for months, and by then it would not be one.

**`Borough.Headless` is the project most likely to be dismissed as a nicety and it is the one that decides whether this simulation ever gets balanced.** Citybound's warm rebuild took 60–120 seconds and its author's own final devblog admits he had been abandoning the simulation in favour of the parts he could iterate on. The headless runner plus a hot-reloadable Ruleset is the direct answer: change a production ratio, run a hundred simulated Days across a dozen seeds, diff the outcomes, keep or discard — with no window, no camera, and no wall-clock waiting. `FAST ITERATION`

**`Borough.Tests` is unusually powerful here because the simulation is a pure function.** Most games cannot assert much about themselves. This one can assert Goods conservation, parking occupancy conservation, Citizen count conservation across Household spawn transitions, bounded collection growth, and bit-identical replay — all headlessly, all in CI, all in seconds. The testing strategy in [`02-simulation-model.md` §10](02-simulation-model.md) is the specification; this project is where it runs.

**`Borough.Formats` exists because a format with two implementations is a format that drifts.** The Input Log is line-oriented text, and text spells verbs in words — which the core may not do, since [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) gives it ids and numbers and gives the shell every string a human reads. The alternative to a project was a codec in each shell, and that is the one arrangement the format cannot tolerate: **a log written by `Borough.Godot` must replay in `Borough.Headless`**, so writer and reader agreeing is not a nicety but the whole purpose, and a divergence caused by two codecs disagreeing would present as a State Hash divergence with no cause. It owns the log and the crash artifact; it does **not** own the save, which is an array dump generated from the field declaration and therefore stays with the declaration. See [`adr/0039`](adr/0039-the-text-formats-are-a-fifth-project-not-a-core-exception.md), which also records the cost: until the Godot shell writes a log, this project has one consumer and will read as over-structure.

**`Borough.Analysers` is the sixth project and is deliberately not counted among the five.** It is a `netstandard2.0` Roslyn analyser assembly holding §4's lints 2, 3 and 7 and the `purpose_tag` check, and `Borough.Core` references it with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`. That is not a formality: it means the analyser is a **build-time input** rather than a dependency, no type declared in it can be named from the core, and nothing it contains reaches the running simulation. [`adr/0003`](adr/0003-deterministic-integer-simulation.md) requires every core *dependency* be argued explicitly because a dependency is a determinism liability that ships inside the sim; this one ships nothing, so the argument it would need does not arise. It is excluded from the five for the same reason: the five are the *runtime* architecture, and adding a build tool to that list would blur what the split is claiming. That test is what `Borough.Formats` fails and therefore what makes it a genuine fifth rather than a second footnote — it ships.

Two toolchains, one repository. The .NET solution and the Godot project build separately, and **the headless runner must never require Godot to be installed.** That constraint is the cheapest possible continuous check that the boundary still holds.

---

## 2. The sim/render boundary

The public surface of `Borough.Core` has **two flavours of query, split on the cadence of the caller**, plus persistence, which is not a query at all:

```
hot    step(inputs)                   advance exactly one Tick
       visible_agents(aabb, alpha)    interpolated transforms inside a box the host supplies
       layer_cells(aabb, layer)       overlay values, one per Cell
       chunk_aggregates(aabb)         cached per-Chunk population, pollution, land value, employment

cold   inspect_*(handle)              §9's Evidence requirements, per entity kind
       expand(aggregate)              an aggregate's constituents, as a bounded sample
       preview(command)               legality and cost of a command not yet issued
       drain_notifications()          the outbound queue, emptied by the host
       series(metric, window)         aggregate history for panels and the headless runner
```

Full specification in [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md); what each cold method must be able to *answer* is [`02 §9`](02-simulation-model.md). **Persistence — save, load, migration, Ruleset load, crash dump — is specified in §7 and §8** and deliberately does not sit on this axis: it returns bytes rather than answers, touches the whole world rather than a bounded sample, and the async save runs concurrently with a Tick.

**The split is on the caller's cadence, never on the data's location.** A cold Evidence query reads hot fields constantly — `§9` wants a Building's Bins at current levels — and it is still cold, because the host calls it on a click. This is the third application of the axis: `§3` splits **tables** hot/cold, [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) uses it to decide where the no-reference-types lint applies, and this splits the **API**. The consequence that matters here: **the hot path allocates nothing and holds no references; the cold path may do both**, because it runs on a click rather than on a Tick.

**The library never knows what a camera is.** It does not know the frame rate, the viewport, the aspect ratio, or whether anything is being drawn at all. The host passes a box and an interpolation alpha and gets transforms back. Every call is a **query, not a subscription** — the shell asks and the library never pushes, which is the usual route by which engine types leak across a boundary like this one. Notifications are no exception: they are an outbound queue the host **drains**, not a callback the library invokes.

**And the shell owns every string a human reads.** `Core` returns ids and numbers, resolved to display text by the shell through the Ruleset. This is the second CI check `adr/0002` requires, and it exists because the real leak vector was never `using Godot;` — it is a method that returns a formatted string because a panel wanted one.

**Fixed sim Tick, interpolated render.** This is built first because everything assumes it. The precedents are unambiguous about how much slack it buys: Cities: Skylines 1 simulates car movement at **4 Hz** and interpolates between two sim frames, and nobody notices; Factorio *extrapolates* robot positions from a known spline and velocity while updating them once per **20 Ticks**. Render smoothness is a presentation problem with a presentation-layer solution, and paying for it in simulation rate is a category error.

The interaction with [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) is the part worth stating explicitly, because the two decisions were taken independently and each is what makes the other cheap:

> **Rendering may be freely camera-driven precisely because Fidelity is not.**

Simulation detail follows network Stress, which is simulation state. Nothing the renderer does — frustum culling, distance LOD, drawing a sampled subset of pedestrians, skipping offscreen Chunks entirely — can influence which Segments are Microscopic. A Traveller on a stressed Segment offscreen is still fully simulated and simply not drawn. `0007` removed the camera from the simulation; `0002` removed the simulation from the camera. The result is that the renderer can be as aggressive as it likes with no correctness argument to have.

This also means there is **no camera-derived input to record.** An earlier design made a fidelity focus point an explicit input so that replay would reproduce it. Deriving Fidelity from Stress deleted the input altogether, which is why the Input Log is `(world seed, configuration, Ruleset content hash, player commands per Tick)` and nothing more. The Ruleset hash is there because a replay needs the Rules' *content*, not the news that they were reloaded — see §7.

**The UI reads the Past, never the Future.** Reading a half-computed Tick would show the player states that never existed — a `LEGIBLE CAUSE` defect long before it is a threading bug.

---

## 3. Data layout

Entities are **rows in typed tables**, not components in an ECS. See [`adr/0004`](adr/0004-typed-tables-over-ecs.md) for the full argument; the short form is that ECS earns its complexity through heterogeneous composition, and this population is homogeneous. Every Citizen has identical fields. There are perhaps a dozen entity types and all of them are known at compile time.

Four properties of the layout, each load-bearing for something else in this document:

**Structure-of-arrays.** Each field is its own contiguous array. The hot loops — Lane queues, Event Wheel buckets, layer diffusion, choice scoring — touch a few fields across many rows, which is exactly the access pattern SoA exists for. It is also what keeps `Span<T>` useful and what makes the C# performance bet in [`adr/0001`](adr/0001-godot-and-csharp.md) hedgeable rather than merely hopeful.

**Generational handles.** A reference is `{ index: u32, generation: u32 }`. The index addresses the row; the generation is bumped when a row is freed, so a stale handle is *detectably* stale rather than silently pointing at whoever moved in.

**Typed quantities, which are the same argument one level down.** `Money`, `Ticks`, `Tiles` and `Ratio` are distinct `readonly record struct` wrappers over their integer representation, so `Tiles × Tiles` does not compile and a `Tick` cannot be added to a `Tile`. [`adr/0004`](adr/0004-typed-tables-over-ecs.md) types *identities* so a Citizen handle cannot index the Building table; this types *quantities* for the same reason, and it erases to the underlying integer at runtime. It is what makes [`adr/0003`](adr/0003-deterministic-integer-simulation.md)'s ratio rule structural rather than a convention — no analyser can distinguish a ratio from an absolute unless the units are in the type. **Cheap now and genuinely expensive to retrofit**, since it touches every arithmetic site in the core.

**Widths are stated once, not chosen per site.** Money and accumulators **i64**; counts and Bin levels **i32**; positions **i32** in sub-Tile units; Ticks **u64**. ~~**Q16.16 is for sub-Tile positions and nothing else**~~ — **AMENDED by [`adr/0071`](adr/0071-travel-time-is-sub-tick-and-q16-16-is-a-scale-rather-than-a-meaning.md). Q16.16 is a *scale*, not a meaning**, and three quantities are carried at it: **sub-Tile position** in Tiles, **speed** in Tiles/Tick, and **travel time** in Ticks. Each is a distinct type and none is assignable to another; **a bare `int` standing for any of them is the defect this clause exists to prevent**, which a type forbids and a prohibition did not. The old sentence was also in **direct contradiction with `02 §2`** — which mandates that speed be stored as Tiles per Tick — and applied literally it would round a walking pace of 3.66 Tiles/Tick to 3, a 20% error on the quantity the whole pedestrian layer is made of. The clause's *first* sentence is the one that always carried the weight: widths are stated once, not chosen per site. Q16.16's ±32,768 range is ample against a 4096-Tile map, and against a travel time it is four Days versus a Commute Budget of order a hundred Ticks. Fixed×fixed multiplication operates on **dimensionless ratios**, which is what keeps it inside the format; a genuine product of absolutes widens to Q32.32 per site with a written reason. Full argument and the overflow policy in `adr/0003`.

**A handle index must never be used as a sort key in simulation logic**, and this is a determinism rule rather than a style note. Indices are recycled by the free list, so ordering by one means an unrelated demolition on the far side of the city can silently change who wins a contested draw downtown. [`02 §8`](02-simulation-model.md) rule 5 settles the case that prompted this — Phase 3 intents are ordered by a counter-based random shuffle — but the general prohibition belongs here, next to the structure that causes it. Where a stable per-entity key is genuinely needed, it is a **monotonic never-reused id**, carried as its own field. Handles are values, dense, and trivially serialisable. Note the interaction with [`adr/0016`](adr/0016-the-lane-is-the-entity-not-the-car.md): a Vehicle is addressed as `(Lane, index)` and **not** by a global handle, because the Lane queue compacts as Vehicles leave. Anything holding a bare Vehicle position across a Tick is a defect.

**Hot/cold splitting.** Fields touched every Tick live in one table; fields touched only on transactions or inspection live in another, keyed by the same handle. A Citizen is on the order of 40 bytes hot. Household economics — income, expenses, savings, purchases missed — is the planned next layer and goes entirely in the cold table, which is why `deferred.md` can call its retrofit cost low. The split is what makes that promise true rather than aspirational.

**No hash maps in simulation code, ever.** `ResourceMap` — a sorted array with binary lookup — is the replacement wherever a keyed lookup is genuinely needed. (**Not for Bins**: a Building's Bins are an intrusive index list walked linearly, and `02 §4.1` carries the argument.) `Dictionary<string,T>` iteration order differs between two runs of the same binary, because .NET randomises string hashing per process. This is a determinism rule (§4) but it is also just a better data structure at these sizes.

**Two per-entity structures that are not tables, and must be named or they will be discovered.** Both are intrusive index lists. **One is saved and one is rebuilt, and which is which is decided by a rule rather than by taste:**

> **A list is `(derived AND rebuilt)` only if its *order* is recoverable from saved state, not merely its membership.**

A rebuild has one order available to it — index order, because walking the owning table is the only thing it can do. So a list whose order carries meaning has to be state. The failure this prevents is quiet in a way worth spelling out: appending in *arrival* order and rebuilding in *index* order agree until the free list recycles a slot, after which a saved-and-reloaded city drains its queues in a different order from a continuously-run one — and **nothing reports it**, because a derived field is outside the State Hash by declaration. Found in slice 4 while building the rebuild; recorded in [`plans/0002`](../plans/0002-open-questions.md) §*Slice 4*.

- **A wait list per Bin — `(saved AND hashed)`.** The subscription queue from [`02 §4.1`](02-simulation-model.md), drained round-robin and therefore an ordered list rather than a set. **Arrival order is the whole content of "round-robin" and is recoverable from nothing else**, so it fails the rule above: membership could be rebuilt from each waiter's own unmet need, but the order in which they queued could not. Reconstructing it from a stored join-Tick would not help, because ties would then need a tiebreak and the obvious one — entity id — is the biased ordering [`02 §8`](02-simulation-model.md) rule 5 rejects for exactly this reason. It is on the hot path of every Bin write, which is why Bins are not public fields: one write function, so draining cannot be forgotten. *This paragraph previously said the list was rebuilt.*
- **Cached Parking Shed membership per Building — `(derived AND rebuilt)`.** The ordered set of **Car Parks** (~~parking Bins~~ — `adr/0120`) within walking distance of a pedestrian Access Point, ~~invalidated by the **Road Graph Epoch**~~ **invalidated per-Segment, witnessed by the walk paths to the Car Parks the shed kept** ([`adr/0083`](adr/0083-a-sheds-use-is-the-arrival-query-and-a-stale-shed-is-wrong-by-a-bounded-walk.md)).

  > **⚠ CORRECTED 2026-08-12. *Invalidated by the Road Graph Epoch* named when you pay and not what survives, and it implied a granularity nothing had chosen.** A single counter carries no location, so **every edit anywhere invalidates every shed** — 159,825 of them at **255.560 ms**, which is **1,638.20% of a Tick** — and *lazily* only spreads that bill rather than shrinking it. Since `adr/0009` pays the query **on arrival**, spreading turns one stall into a **stampede across arriving vehicles**, which is the opposite of what laziness was being relied on for. S2 R5.6 measured the rungs: global 1,638.20%, per-cluster 13.04%, per-Segment by **ball** 1.12%, per-Segment by **paths 0.10%**. At 400 m a shed's walk ball explores 22 Segments where the walks to the Bins it keeps touch **2**, so the conservative witness is **11× its own answer** — which is why the rung is stated as *paths* rather than as *per-Segment*.
  >
  > **The shed needs no staleness parameter**, unlike a route: its use is the arrival query and nothing else, and a stale shed returns a Bin that exists and has capacity and is merely not the nearest — an error bounded by the radius and already scored by the Commute Budget. **No `T`, no rotation, no proximity wake.**

  It passes the rule where the wait list fails it: its order is *distance*, which is a pure function of the road graph and of two positions, all of them saved — so a rebuild reproduces the same order rather than merely the same members. Ties are broken by the target Bin's index, which is legal here only because the query is rebuilt wholesale rather than accumulated, so the ordering never outlives the epoch that produced it. [`adr/0009`](adr/0009-parking-is-modelled-supply-never-search.md) named this only as a contingency should shed queries appear in a profile; it belongs here, because a nearest-first query on every vehicle arrival is not a thing to discover late.

**A third list arrives in slice 9 and its classification is genuinely open: the Event Wheel buckets.** Slice 4 declared the Citizen's `wheel_next` link `(derived AND rebuilt)` on the argument that the bucket a Citizen sits in is a pure function of `next_event_tick`, which is saved. That is true of *membership*. Whether it is true of *order within a bucket* depends on a claim slice 9 has to make explicitly rather than inherit: that **nothing observable depends on the order Citizens are woken in on a single Tick.** The claim is plausible — Phase 2 is read-only, so Decide cannot be order-dependent, and [`02 §8`](02-simulation-model.md) rule 5 settles contested outcomes in Settle by counter-based shuffle rather than by arrival — but it is load-bearing, and if it fails the buckets reclassify to saved exactly as the wait list did.

The payoff that matters most is elsewhere in this document: **a save is an array dump** (§7), and **a partition is a range of indices** (§5). Both of those are consequences of the layout, not separate systems.

### Buffering: one live world, and hazards are per table

Defined here because it is caused by the layout above and because it is no longer domain language. Full argument in [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md).

> **A table is double-buffered if and only if a parallel phase both reads and writes it.**

That is **two tables**, not all of them: **Lane dynamics**, because Phase 4 (Move) is parallel and a Vehicle crossing a Junction reads another Lane's queue; and **Map Layer cells**, which §9 already double-buffers for the same reason plus directional smear. Roughly 2 MB combined. Everything else — Citizens, Households, Businesses, Buildings, Lots — is written only by the **serial** phases Settle and Growth, and a serial writer has no peer to race.

**"The Past" is a phase-discipline fact, not a storage fact.** It is *the state as of the start of this Tick*, and Phase 2 observes it because nothing has written yet. [`02 §1.1`](02-simulation-model.md)'s semantics are unchanged; only the mechanism is. **Phase 2's read-only-ness is therefore load-bearing** — if Decide ever becomes parallel *and* mutating (§6 defers it as a build-order item), every entity table silently reclassifies.

The full-world copy this replaces was **~150 MB per Tick against ~1 MB of actual writes** — 8–15 ms, memory-bandwidth bound, and it touched every sleeping Citizen every Tick, cancelling the Event Wheel. The three consumers that genuinely need a complete snapshot get one at their own cadence instead: the **saver** takes one real copy at save time, the **renderer** reads a published transform history one generation deep, and a **panic** emits the last checkpoint plus the Input Log, which replays into the failure (§8).

---

## 4. Determinism rules

Determinism is the highest-leverage investment available to a solo developer on a project this complex, and it must exist from commit #1 because retrofitting it is close to impossible. The binding rules are enumerated in [`02-simulation-model.md` §8](02-simulation-model.md) and argued in [`adr/0003`](adr/0003-deterministic-integer-simulation.md). Summarised here as the enforceable list, because this is the document a reader arrives at asking "what am I not allowed to write?":

| Banned construct | Instead | Why |
|---|---|---|
| `float` / `double` **in any arithmetic**, not merely in stored state | integers, or Q16.16 fixed-point for sub-Tile positions — and, per `adr/0071`, for speed and travel time | Cross-platform and cross-JIT reproducibility. This row and `02 §8` rule 1 both used to say *"in simulation state"*, which permitted a float temporary cast to an integer — **exactly as non-deterministic**, via x87 80-bit intermediates, FMA contraction and differing SIMD widths |
| Raw `/` and non-constant `<<` | stated rounding and range-checked shift helpers | C# truncates division toward zero (`-7/2 == -3`, `7/2 == 3`), which is a **directional bias at every zero crossing**; and shift counts are silently masked, so `x << 32` is `x << 0` |
| `Math.Exp` / `Math.Log` and every other `Math.*` | **tabulated fixed-point `exp` and `log` with defined rounding**, in the core | The claim here used to be *"a city sim needs zero transcendental functions."* It was false: `02 §5.4`'s choice model is a softmax and `02 §2.4`'s noise falloff is logarithmic. See [`adr/0003`](adr/0003-deterministic-integer-simulation.md) — the table's **resolution is a stated figure**, because it perturbs the logit's `μ` and `μ` is what prevents stampedes |
| Iterating a `Dictionary` or `HashSet` | sorted arrays, or dense arrays indexed by generational handle | Per-process string hash randomisation in .NET |
| `System.Random` | `draw(world_seed, entity_id, tick, purpose_tag)` — **the function is normative and written out in [`adr/0003`](adr/0003-deterministic-integer-simulation.md)** | Its algorithm changed in .NET 6 and is not documented as stable. And **ours is a format, not an implementation detail**: an Input Log reproduces a run only if the hash is bit-identical, so changing it is a save-format-class change under §7 |
| A `string` `purpose_tag` | a compile-time integer constant from one central enum, **uniqueness checked at build time** | A string needs string hashing, which is banned two rows up; a mistyped one collides silently, and the resulting correlation is invisible at runtime |
| Reading wall-clock time | the `u64` Tick counter | The host decides when to advance; the library has no clock |
| Partitioning work by thread count | partitioning by Chunk | A partition that depends on core count produces machine-dependent results |
| In-place layer diffusion | double buffers | Order-dependent, and it smears directionally |
| Reading anything render-side | nothing — there is no such input | See §2 |

**Counter-based RNG deserves its own paragraph** because it is nearly free and it eliminates the nastiest class of parallel bug in advance. Deriving randomness from `hash(seed, entity, tick, purpose)` rather than drawing from a shared mutable stream makes every draw independent of evaluation order. That means the Decide phase can be parallelised later with **zero coordination and bit-identical results**, and it means a single-Household draw can be reproduced in a test without replaying the run that produced it. Every distinct use gets a distinct `purpose_tag`; reusing one across two decisions correlates them invisibly.

**CI lints, not code review.** These rules fail silently and are violated by accident, so they are enforced mechanically:

1. **No Godot reference from `Borough.Core`, transitively.** The boundary check from [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md). *Shipped — a reflection test, which is what a reference check is good at.*
2. **No floating-point types** in simulation state or arithmetic — a Roslyn analyser over the `Borough.Core` assembly. *Shipped — `BOR0201`, with `BOR0202`–`BOR0207` covering the rest of the banned-construct table above: `Math.*`, raw `/`, masked shift counts, the wall clock, process-unstable identity, and a ratio pre-scaled by a large constant then divided in 32 bits. That last one is the newest and the only one added because a defect got through: a Q16.16 quantity is 65,536 times its whole value, so `part * 10_000 / whole` in `int` wraps at **3.3 whole units** and wraps **negative**, deleting the largest inputs from a mean. `BOR0203` is what leads into it — every division goes through `IntegerMath`, whose `FloorDiv`, `CeilDiv` and `RoundDiv` all have `int` overloads, so the call site reads as though the widening were handled while the argument has already overflowed.*
3. **No `Dictionary`/`HashSet` enumeration** in simulation code, and no `System.Random` anywhere in it. *Shipped — `BOR0301` and `BOR0302`. Note the shape the diagnostic has to teach: a hash map may be **built** and **looked up**, and may not be **walked**. A lint that banned the type would be worked around rather than obeyed.*
4. **Thread-count equivalence:** `run(log, threads=1).hash() == run(log, threads=8).hash()`. If we cannot run single-threaded on demand, we cannot debug determinism at all. *Deliberately unwritten. Phase 1 is single-threaded, so this test today would assert a property against no parallelism and pass vacuously forever; it is written when the first parallel phase lands.*
5. **Replay equivalence:** two runs of the same Input Log produce identical State Hash sequences. *Delivered by slice 5* — `ReplayTests`, plus the committed golden hash trace.
6. **Save/reload equivalence** — see §7. *Shipped by milestone 8 — `FactorioTests`, green over seven cases and two Rulesets, which is the first machinery this rule has ever had. ⚠ **And it is a property of every load as of task 10, not only of the suite**: a save's header carries the State Hash of the world it holds, folded from the copy rather than from the live world, so a load restores, rebuilds, recomputes and refuses a mismatch ([`adr/0112`](adr/0112-the-saved-set-is-the-hashed-set-so-a-save-can-compute-its-own-state-hash.md)). ***A test asserts this for the cases somebody wrote down; the header asserts it for the ones nobody did.*** (Milestone numbering: this was **old 10**, which is now **8** — see `06`'s retired-numbering table.)*
7. **No reference types in simulation state.** A Roslyn analyser asserting that every table row type and every derived structure satisfies the `unmanaged` constraint. Added by [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md), and it is the only one of the seven that is C#-specific — the other six are needed in any candidate language, which is the concrete measure of how little the language is being fought. *Shipped — `BOR0701`.*

**Two build-time checks sit beside the seven and are deliberately not numbered among them**, because neither is a rule this section states. The `purpose_tag` uniqueness row of the banned-construct table above is one (`BOR0801`–`BOR0803`). The other is [`adr/0003`](adr/0003-deterministic-integer-simulation.md)'s **per-field declaration** (`BOR0901`): every field in a table is declared once as `(saved AND hashed)` or `(derived AND rebuilt)`, and both the save serialiser and the State Hash are generated from that one declaration. Most of that rule enforces itself — declaring a column through `Rows` is what *allocates* it, so an undeclared column has no storage — and the analyser closes the one route around it, which is a bare array written beside the columns. ⚠ **There is a third declaration as of milestone 8** — **scratch**, for a phase-local intermediate that is neither saved nor rebuilt ([`adr/0110`](adr/0110-scratch-is-a-third-disposition-because-derived-is-a-claim-a-scratch-column-does-not-make.md)) — and it is what keeps such an intermediate *inside* the declaration rather than escaping it as the bare array `BOR0901` rejects. **Keep both out of the count.** A checklist that cannot agree on its own length has stopped being checked, which is the correction `adr/0036` carries and the reason the seven stay seven.

**Rule 7 is not primarily a performance rule, and the structures it protects are not the tables.** Arrays of unmanaged structs are opaque to the GC, so the tables were never at risk. The risk is in the three derived, variable-length, per-entity structures this design grew around them — the **wait list per Bin** (§3), the **cached Parking Shed per Building** (§3), and the **Event Wheel buckets** (§9). As per-entity collection objects those are on the order of a million long-lived traced references at the 1M target. Hence the rule that makes rule 7 satisfiable:

> **Every variable-length collection in `Borough.Core` is an intrusive index list — a head index on the owner and a `next` index on the element, both in flat arrays. Never a per-entity collection object.**

It allocates nothing, traces nothing, gives [`adr/0033`](adr/0033-two-rule-families-scheduled-and-swept.md)'s round-robin drain its deterministic order for free, and survives a port unchanged.

**The exceptions were enumerated in slice 3 and came out as an axis rather than a list** ([`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md)). The Ruleset interpreter and the `Evidence` surface are not two special cases; they are one case, and it is the hot/cold split this document already uses in §10: **the hot path runs inside `step()` every Tick and holds no references; the cold path runs on a click, a reload or a save, and may.** A type is entitled to the exception when no code path from `step()` reaches it, and it claims the exception by carrying `[ColdPath("why")]` — at the declaration, with the argument written out, where the next reader is.

**The State Hash is also the boundary of what optimisation is permitted to touch**, and stating that as a general rule closes a gap the project has already had to close twice by hand — the Microscopic Cap as a world constant ([`03 §3.9`](03-agent-architecture.md)), and gridlock's removal as a failure mode:

> **A change is an optimisation if the State Hash is unchanged, and a design change otherwise — however it was motivated.**

Thread count, **Chunk size and the partitioning of work by Chunk**, render sampling (§10), wait-list internals, rebuilding the travel-time matrix rather than saving it: all hash-preserving, all free to tune against a profile.

**That list was wrong once, and the way it was wrong is instructive.** It read *"Chunk partitioning"* while the Chunk was also the storage unit of every Map Layer — so Chunk size set the resolution of pollution, pollution feeds Fertility and Desirability, and the number this section declared free to tune was silently deciding farm yields across the map. The rule did not fail; nobody applied it. **The fix was to split the hash-bearing role out** into the **Cell** (§5), after which the claim above is true rather than merely asserted. The general lesson is worth more than the instance: *a constant welded to two decisions is governed by whichever of them is louder*, and the State Hash is the test that separates them. Moving a mechanism between the two Rule families in [`02 §4`](02-simulation-model.md) is not, because the families differ in propagation latency, in who wins a contested resource, and in what `Evidence` reports on failure. It is therefore not available as a performance measure at any price. The rule generalises the standing worry in the ledger — *if an indicator would change when the simulation is optimised, it is not a trajectory* — from indicators to mechanisms.

The payoffs are worth naming because they are what justifies the discipline. A ten-hour session's Input Log is kilobytes, so **a bug report is an attachment**. State Hashes taken every N Ticks turn "the economy went wrong somewhere in the last hour" into **the exact Tick a divergence entered**. Neither is available at any price to a project that did not build determinism first.

---

## 5. Chunking as the universal partition

**There are two grids, nested, and the line between them is the State Hash.** They were one grid until [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md).

| | **Cell** | **Chunk** |
|---|---|---|
| Size | **32×32 Tiles (≈128 m). Frozen.** | a multiple of the Cell. **Open — measure it** |
| Kind of decision | **design** — it changes the State Hash | **performance** — hash-preserving |
| Decided by | the mechanic it serves | a profiler |
| Roles | Map Layer storage, Sealing | the six below |

The **Cell** carries exactly one job: it is the resolution at which the environment varies. That is a gameplay decision, because Cell size is pollution resolution, pollution feeds Fertility and Desirability, and Sealing is a fraction of a Cell — one house seals 1/1024 of one. It is therefore permanently unavailable for tuning, and it does not appear again in this document.

The **Chunk** keeps the other six roles, and remains deliberately overloaded:

| Role | What the Chunk provides |
|---|---|
| Dirty tracking | The granularity at which Map Layer sources are marked changed |
| Save serialisation | A save is a sequence of Chunk records plus global tables (§7) |
| Parallel work assignment | A fixed partition independent of core count — a determinism requirement |
| Aggregate caching | Per-Chunk population, pollution, land value, employment |
| Pathfinding cluster | **The grid the HPA\* cluster aligns to — not the cluster itself.** See [`adr/0040`](adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md) |
| Render streaming | One MultiMesh set per Chunk (below) |

**Making these the same partition is a major simplification, and it is worth being explicit about what the alternative costs.** Six independent partitions means six invalidation stories, six sets of boundary conditions, six places where an index is converted from one grid to another, and — worst — six opportunities for two subsystems to disagree about which side of a line something is on. Unifying them means that "this Chunk is dirty" is a single fact with six consumers. A proposal to split any one of these onto its own grid should be treated as suspect and should have to argue why the coupling it removes is worth the five it breaks.

**Note what the split did *not* cost.** The Cell is a strict divisor of the Chunk, so there is no index conversion that is not a shift, no boundary that does not align, and no possibility of the two disagreeing about which side of a line something is on. The unification argument survives intact; what left it was a role that was never a performance role.

**It is also what makes the MultiMesh problem tractable, and that problem is real work rather than a footnote.** `MultiMeshInstance3D` draws thousands of low-poly Buildings in a handful of draw calls, which is the whole reason [`adr/0001`](adr/0001-godot-and-csharp.md) selects Godot on rendering grounds. But **every instance in one MultiMesh shares a single AABB for culling.** A city-wide MultiMesh is therefore either always drawn or never drawn, which converts the engine's best rendering feature into a way of defeating frustum culling entirely.

The fix is to split the city into a grid of per-Chunk MultiMeshes — and the Chunk grid already exists, so this is a reuse rather than a new axis. Concretely, `Borough.Godot` maintains, per Chunk, one MultiMesh per mesh archetype, rebuilt when the Chunk's Buildings change and streamed in and out by distance and frustum. That state is **entirely disposable**: it lives in the shell, is rebuilt from a snapshot, and nothing in it survives a reload.

The consequence to plan for is that **the Chunk now has a rendering responsibility on the critical path.** Buffer rebuilds must be incremental and amortised — a single Building completing must not repack a whole Chunk's transforms every frame — and Chunk-boundary Buildings must belong to exactly one Chunk by a stated rule rather than by whichever code path got there first.

**How large should the Chunk be?** An earlier draft named a four-way tension between the roles. Three of the four dissolve under arithmetic at 4096², and the survivors point the same way:

| Role | Was claimed to want | At 32×32 (16,384 Chunks) | Actually wants |
|---|---|---|---|
| Save record | larger — *"per-record overhead disappears"* | ~4k developed Chunks × ~16 B ≈ **64 KB** | **nothing.** Already negligible |
| Map Layer diffusion | coarser — *"nearly free"* | separable, integer, incremental, staggered | **gone** — it is the Cell's problem now, and it was never a cost problem |
| Parallel partition | count ≫ cores | 16,384 vs ~16 | **nothing.** Non-binding at any size ≤ 512² |
| Pathfinding cluster | larger | 128 m clusters on a map where a commute crosses tens of km² | **larger**, and loudly |
| Render streaming | *smaller* — finer culling | draw calls = visible Chunks × archetypes, and MultiMesh exists to *collapse* draw calls | **two-sided.** Too small pays draw calls; too large drags off-screen geometry in at the frustum edge |

So the honest position is that the Chunk probably wants to be **larger than 32×32**, that the pathfinding role has the strongest claim, and that the render role has a genuine optimum with a bottom to find. All of it is hash-preserving, so **finding it is a measurement, not an argument** — which is exactly what could not be said while the Map Layer was riding on this number. It is still on the *cannot be retrofitted* list (§11, open question 3): cheap now, expensive once saves exist. See §11 and spike **S2**.

**Amended by [`adr/0040`](adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md): the role with the strongest claim no longer makes a claim.** The HPA\* cluster is a whole number of Chunks rather than one Chunk, because the two differ on an axis this section does not consider — **a save is a sequence of Chunk records, and the router's abstract graph is `(derived AND rebuilt)`.** Changing the Chunk after milestone 8 is a migration; changing the cluster is a recomputation, forever. Unifying them imported permanence onto a structure that never had any. The burden this section sets is met rather than waived: four of the five couplings are unrelated to routing, and **dirty tracking** survives because a strict *multiple* aligns by a shift exactly as the Cell does as a strict *divisor*. What remains is that the Chunk is now sized by **rendering, saves and work partitioning alone** — a two-sided optimum with a bottom, rather than a tug-of-war between a permanent decision and a free one.

---

## 6. Threading policy

Stated in **build order**, because the order is the decision. Each step is taken only after the previous one has been measured.

**Step 1 — the Event Wheel, which is not concurrency at all.** The largest available performance lever has zero concurrency complexity, and taking it first is what prevents reaching for threads to solve a problem that was never about parallelism. See §9.

**Step 2 — Chunk-partitioned work.** Map Layer diffusion and per-Chunk aggregate recomputation are read-only over the Past and write to disjoint outputs. The partition is fixed by geometry, not by core count, so results are identical at any thread count. This is nearly free and it is where the phase table in [`02-simulation-model.md` §1.1](02-simulation-model.md) marks the Layers phase as parallel.

**Step 3 — pathfinding on a worker pool.** By far the best first genuine parallelisation target, for four reasons: route computation is **pure** (a function of the Road Graph and an origin–destination pair), **compute-dense** rather than memory-bound, **read-only** with respect to simulation state, and **latency-tolerant** — a route requested this Tick can be consumed next Tick without anyone noticing. Requests are queued, workers drain them against the immutable Past, and **results are applied in deterministic request order** during a serial phase. The determinism argument is entirely contained in that last clause; nothing about how many workers ran, or in what order they finished, can reach simulation state.

**Deliberately deferred, and this list is a commitment rather than a backlog:**

| Deferred | Why |
|---|---|
| Parallel Household and Business decision evaluation | Phase 2 is *safe* to parallelise by construction — counter-based RNG makes it order-independent — but it is not yet the bottleneck, and until it is, threading it buys complexity |
| A general job graph or task scheduler | Infrastructure for a problem we have not demonstrated. See the standing bias against bespoke infrastructure (ADR-0014) |
| Lock-free data structures | The Past/Future split means shared mutable state is already rare. A lock-free structure is a solution to contention we have designed out |

> **This section is unargued and one row above is now stale.** [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) replaced the full-world double buffer with a single live state, so *"the Past/Future split means shared mutable state is already rare"* rests on a structure that no longer exists in that form. The conclusion may well survive — writes are still confined to serial phases, and the two parallel writers are double-buffered by the §3 rule — but **it now follows from phase ordering rather than from a second copy of the world**, and that is a different argument. §6 also never states which thread runs `step()`, which `0037` made consequential: with one live state, what the renderer and the saver read is a design decision rather than a free consequence. Both are the opening subject of the next grilling session.

**The evidence for this conservatism is specific and it is worth carrying.** Factorio is the best-optimised simulation game ever shipped and it remains **largely single-threaded**. Both of its documented parallelisation attempts made things *slower*. The instructive one is electric network updates: the work was **memory-throughput bound**, so adding threads meant all threads waited on memory instead of one thread waiting on memory. Parallelism converted a serial memory stall into a parallel memory stall and added coordination overhead on top.

That generalises into the rule this project uses:

> **Parallelise work that is compute-dense and read-only. Do not parallelise work that is memory-bound and pointer-chasing.**

Pathfinding is the first kind. Walking Citizen records to check whether anything needs doing is the second — and the correct fix for the second is not threads, it is the Event Wheel, which deletes the work rather than distributing it.

---

## 7. Save format and migration

**A save is array dumps.** This is the direct payoff of typed tables ([`adr/0004`](adr/0004-typed-tables-over-ecs.md)): each hot and cold table is a contiguous run of value types, and serialising one is a length plus a block of bytes. There is no object graph to walk, no reference cycle to break, and no reflection-driven serialiser whose behaviour changes when a field is reordered.

**Nothing authors the layout.** [`adr/0086`](adr/0086-a-save-has-no-schema-of-its-own-and-the-field-declaration-is-the-format.md) settles that the file **is** `§4`'s per-field declaration, read out in the order the State Hash already folds it: for each table in declaration order, the allocator's four scalars, then every column whose disposition is `Saved`, over the full slot range. The `header / global / chunks / tables` listing this section used to carry is **deleted rather than corrected** — it was a second copy of a fact the declaration owns, it had no reader, and by the time anyone read it against the code it was wrong in four of its five lines.

Three properties follow, and none of them is obvious from *"array dumps"*:

- **The save's content set is the State Hash's coverage set** — same tables, same columns, same slot range, because both answer one question. There is one ordering rule, `§4`'s, and the file does not get a second.
- **The save is slot-exact.** The free list and the monotonic id counter are hashed, and every slot in `[0, slotCount)` is folded including recycled ones holding a dead row's residue. **Compaction on save is therefore forbidden**: it moves the hash, so under `§4` it is a design change and not a size optimisation, however it was motivated.
- **A migration is a pure function over *saved* columns only.** A derived column is rebuilt on load, so adding, removing or altering one needs no migration whatsoever — which is where most schema churn lands.

**Three version numbers, not one.** [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) requires three and this section long implied two. They are separate because they fail in three different ways and are repaired in three different ways:

| Number | Versions | Repaired by |
|---|---|---|
| **Format version** | the declaration set — which tables, which columns, which disposition | the migration chain below |
| **Ruleset content hash** | the content the Rules are made of | the two policies below, plus degradation and the provenance trail |
| ~~**Generator version**~~ | ~~the generator's terrain output for a given seed~~ | ~~nothing. It is **pinned**, because a moved landscape has no migration~~ |
| **The world-creation constants** | `TICKS_PER_DAY`, `WHEEL_SIZE`, `CellGrid.WorldCells`, `CellGrid.TilesPerCell` — every value that is a `const` in the binary rather than a column in the file | nothing. Each is written individually so the refusal names which one moved |

⚠ **The third row was struck and replaced 2026-08-17 by [`adr/0111`](adr/0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md), which built the header.** The generator version and this section's old *world seed* field are **one requirement rather than two** — a seed is consumed only by something that regenerates from it, and no load path calls a generator: `Rows.Restore` reads columns back and `World.RebuildDerived` recomputes the rest from them. It returns the day `adr/0021`'s *seed + edits* ships. **Writing a placeholder now would invert the guard**: that number is *pinned*, so a terrain build must refuse every pre-terrain save, which an absent version achieves through the format version and a `generator_version = 1` defeats by agreeing with itself. The header does carry the **world key**, which is not a column and which `World.RebuildDerived` cannot run without — and whose absence the State Hash cannot notice, because the key folds nothing.

**Version header plus a migration chain.** Each save records the format version it was written under; loading an older save runs the migrations from that version forward, one step at a time, each migration being a small pure function from version *n* to version *n+1*. Migrations are never rewritten to skip steps and are never deleted, because the chain is the only thing that makes an old save loadable at all.

### Ruleset version is a different axis from format version

Format version is **schema**; Ruleset version is **content**. A save can be structurally perfect and still refer to Rules that no longer exist, so it gets its own answer rather than a migration.

**The Ruleset is identified by a content hash, and the Input Log references it.** `§2`'s tuple is therefore `(world seed, configuration, Ruleset content hash, player commands per Tick)`, and a hot reload is logged as a *transition* carrying both hashes. A replay bundle is the log plus every Ruleset it references, held in a content-addressed sidecar — which keeps the log itself kilobytes, dedupes identical reloads for free, and needs no bespoke diff format (`adr/0018`). The hash earns a second keep immediately: **a replay whose Ruleset does not match refuses to run** instead of diverging silently, which is otherwise the most confusing possible failure — a replay that reproduces nothing because the data files moved underneath it.

**Cross-Ruleset loading has two policies, because there are two reasons to load a save:**

| | Cross-Ruleset load | Why |
|---|---|---|
| **Play** — continue a city | permitted, with warnings | Bins whose resource no longer exists are dropped; Buildings whose kind no longer exists become derelict rather than vanishing. Refusing would mean **every patch bricks every save**, which is how a city-builder loses its players |
| **Replay / verify** — reproduce a run | refused on an **unaccounted** mismatch | A different Ruleset is a different simulation and the State Hash will diverge. That is arithmetic, not a bug |

The discriminator is §4's rule again: *is the State Hash expected to mean anything here?* This maps onto the projects in §1 — `Borough.Godot` is play mode and lenient, `Borough.Headless` is replay mode and strict, which is correct given it exists to produce comparable numbers.

**"Unaccounted" is doing real work in that table.** The degradation step is a *pure deterministic function* of `(state, old Ruleset, new Ruleset)`, so a transition **the log itself recorded** is replayable and is not refused. What genuinely defeats replay is a changed **binary**, and no amount of logging fixes that. Note also that there are two replay bases, and bug reports use the weaker one: *replay from seed* needs every Ruleset in the chain, while *replay from save* needs only the save and the log after it — which is exactly the artifact §8 already builds, and it is why a city that has crossed several patch boundaries stays diagnosable.

**A save carries a provenance trail**, because one class of bug survives all of the above: a defect *caused* by a degradation three patches ago and surfacing now, whose cause is upstream of every snapshot anyone holds. No replay can reach it. So degradation stops being a logged warning and becomes state — *"at Ruleset `a91f…`: 412 `coal` Bins dropped, 3 Buildings derelicted."* That is `Evidence`-shaped, since it names constituents, and it turns *"why does this city have three derelict buildings nobody built"* into a line item. It grows with patches survived rather than with elapsed time, so `adr/0006` is satisfied — but it is unbounded in principle and therefore caps at the last N transitions, with older entries aggregated to counts.

**A save loaded across an unaccounted mismatch is permanently marked hash-broken**, and the mark propagates to every save descended from it. Without it, a divergence report eventually arrives for a city whose State Hash was never comparable to anything, and it costs days.

**The Factorio save/reload test is the primary correctness harness**, and it catches a class of bug that is otherwise nearly impossible to find:

```
run N ticks → save → reload → run M more   →  hash A
run N+M ticks                              →  hash B
assert A == B
```

**What it finds has changed, and the sentence that used to be here is now describing an impossible bug.** The original class was *unsaved state* — a cached value, a dirty flag, an accumulator, a lazily-built index never written to the file and never restored. Every item on that list is now a declared column: `Saved`, in which case it is in the file by construction, or `Derived`, in which case it was never meant to be, and `BOR0901` is a build error on storage that is neither. `adr/0086` makes the omission unrepresentable.

What survives is the other half, and it is live:

> **A derived column that does not rebuild to the value it had.**

A reload lands a world in exactly the pre-rebuild state where that fails, and 5a-bis already sighted one — *a derived structure caching a Ruleset value reads as **absent** rather than as **stale** before its first rebuild*, and absent is the state every guard is written against. So the test measures the **rebuild**, not the write. It still produces no error, no crash and no visible symptom until the city drifts hours later; two runs and a hash comparison still find it in seconds; and it still belongs in CI from the first save format rather than after the first bug report.

**Saves are written asynchronously from a copy taken at a phase boundary.** The conclusion is unchanged and the mechanism is not: [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) deleted the full-world double buffer, so there is no immutable Past for a serialiser to walk — see `§3`, which already gives the saver *one real copy at save time*. [`adr/0087`](adr/0087-a-save-is-copied-at-save-cadence-not-read-from-a-past-that-no-longer-exists.md) prices that copy and settles when it is taken: at the **end of Phase 7**, which is serial, is where hashes are already written, and is the one moment both double-buffered tables have settled. The copy blocks for ~10 ms at the 1M target; the serialise-and-write blocks nothing.

**The arithmetic is the part that was missing, and it is a denominator rather than a saving.** `adr/0037` deleted the per-Tick copy at **8–15.6 ms a Tick**, which is 50–100% of a 4× budget. The same bytes at one copy per in-world Day amortise to **0.008% of a Tick** — the structure is identical and the frequency differs by four orders of magnitude. Autosave therefore costs no hitch, which matters more than it sounds: an autosave that stutters is an autosave the player turns off, and a save nobody has is a bug report nobody can reproduce.

---

## 8. Crash forensics

**Determinism is what makes a crash reproducible, and it costs nothing to arrange.** This used to be justified by the Past/Future double buffer — *a Tick that panics while computing the Future leaves the Past intact* — but [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) deleted that copy, and the replacement is both cheaper and stronger. So the failure path is not "log a stack trace and die." It is:

1. Catch at the Tick boundary rather than unwinding the process.
2. Emit the **last checkpoint plus the Input Log since it** — `(checkpoint @ 4096, log 4096..5000)` for a panic at Tick 5000. The autosave is already taking the checkpoints, and §7 already observes that *replay from save needs only the save and the log after it*.
3. Attach the Ruleset content hash and the Tick at which the panic occurred.

The result is **a reproduction of the crash**, replayable on demand and small enough to attach to an issue. Combined with deterministic replay (§4), that turns the hardest category of bug in a simulation of this kind — the one that appears after four hours of play in a city nobody can rebuild — into a file.

**And it is strictly better than dumping the dead world was.** A dump lets you *inspect* the aftermath; a reproduction lets you replay to Tick 4999 and **single-step into the failure under a debugger**, as many times as you like. The old formulation could only ever show you the corpse.

Citybound went one step further and kept an **inspector alive after a panic** rather than terminating, so the state that caused the failure could be browsed rather than merely dumped. That is worth adopting once the inspector exists, because the inspector is being built anyway as the primary debugger for emergent behaviour, and pointing it at a dead world costs almost nothing extra.

For a simulation that will be debugged over years, this is worth more than it looks. It is also only available because rendering reads the Past and mutates nothing — a renderer that wrote into simulation state would leave both buffers suspect at exactly the moment they are needed.

---

## 9. Performance strategy in build order

Order matters more than any individual item, because each step changes what the next measurement says.

**1. Event Wheel and sleeping entities.** The single biggest lever, with zero concurrency complexity. Every Citizen, Household, and Building carries `next_event_tick`; buckets are keyed by `next_event_tick % WHEEL_SIZE`, and each Tick processes exactly one bucket. **A Citizen at work for a third of a Day consumes zero CPU for a third of a Day** — it sits in one bucket and is touched once, on waking. This is why `WHEEL_SIZE` must not be smaller than the longest routine sleep; see §11. This converts cost from *number of entities* to *number of entities with something happening right now*, which is typically a few hundred out of hundreds of thousands. Factorio measured a **40× improvement on roboports** from exactly this.

The discipline that makes it work is stated in [`02-simulation-model.md` §7](02-simulation-model.md) and is the part that gets skipped: **entities do not poll, mutators wake observers.** A Rule that fails on flour registers interest in flour arriving in the Pool rather than retrying on a timer. Every mutation site must know its observers. That is more code than polling, and it is the difference between a city that scales and one that does not.

**This makes `rate` a reschedule interval rather than a polling period, and it puts a wait list on every Bin.** The semantics are in [`02-simulation-model.md` §4.1](02-simulation-model.md); what lands here is the cost. Three consequences, and the first is not optional:

- **The failure mode inverts, and the new one is silent.** Polling is self-healing — miss a wake and a Building is merely slow. Subscription is not: a Bin written without draining its wait list leaves that Building asleep forever, with no error and no timer to rescue it. The mitigation is structural, not disciplinary: **Bins are not public fields.** Every write goes through one function that drains the list, so *"every mutation site knows its observers"* is satisfied by there being exactly one. Backed by a sweep invariant — *no Rule is asleep with all its inputs satisfiable* — which is unaffordable per Tick and trivial at the end of a headless run. **It was specified here, in `adr/0033` and in `02 §10`, and built in none of them for the life of Phase 1**; it is now `WaiterIsBlockedByTheBinItNames`, registered in `WorldInvariants` rather than left to a caller, and it fired on the committed golden session within minutes of existing ([`adr/0063`](adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)). Same species of bug, and same shape of cheap assertion, as the save/reload test in §7.
- **Wait lists are not saved; they are rebuilt.** Saving them creates exactly the stale-derived-state class the save/reload test exists to catch. On load, wake every Rule with a stagger — one wheel's worth of evaluations spread over a few Ticks, deterministic, and it re-seeds every subscription from nothing. Same reasoning as open question 5 on the travel-time matrix: derived state is a cache.
- **The drain order is round-robin, and it is a balance decision as much as a determinism one.** A Rule that fires goes to the back of its Bin's queue, so a District under half supply degrades everywhere rather than starving its late-built Buildings. Strict FIFO would satisfy `adr/0003` equally and produce a wall, which the design forbids elsewhere.

The scope of a Rule's inputs is therefore also a statement about *what it can subscribe to*: a scope is only well-formed if it names a bounded set of Bins with a single defined mutation site.

**1b. Not copying the world every Tick.** §3 and [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md). Listed second because it is the **second-largest lever** and because it is the same lever as the first, wearing a different hat: the Event Wheel makes cost proportional to activity, and a full-world double buffer was quietly undoing that by touching every sleeping entity every Tick. **~150 MB copied against ~1 MB written**, 8–15 ms at 1M — 50–100% of the budget at 4× speed. Deleting it costs nothing and adds no bookkeeping; it is subtraction, not cleverness. It is placed here rather than lower because **it is not retrofittable**: every table's buffering is decided by the rule in §3, and reclassifying them after saves exist is a format change.

**2. Chunking.** §5. Dirty tracking, aggregate caching, and coarse layer storage all fall out of the partition at once.

**3. Map Layers coarse and staggered.** Three multipliers make the work affordable, and **this list used to offer all three as levers. Only the first is one** — measured in [`adr/0044`](adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md), which is the second time a number in this document was welded to two decisions and governed by the louder one. §4's rule did not fail; nobody ran it against these numbers.

- **Coarse.** Diffusion at one value per **Cell** rather than per Tile is a 1024× reduction, and upsampled it looks identical. The original SimCity did exactly this — coarse grids, tiny kernels, few iterations — and it looked fine. *This one is settled rather than free: the Cell is a frozen design constant ([`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md)), so the reduction is already banked and there is no further coarsening available.*
- **Low frequency.** Every 64 or 256 Ticks, not every Tick. Pollution does not move fast enough for anyone to tell — but **it is not a knob on this page.** The period decides when a source becomes visible to a Rule reading the Cell, which moves the State Hash, so under §4 it is a design change and belongs to the designer. It stays hot-reloadable Ruleset data; what it stops being is something a profile may move.
- **Staggered.** Pollution on `tick % 64 == 0`, land value on `tick % 256 == 16`, so no single Tick carries every layer at once. A spike every 64 Ticks is a visible stutter; the same work spread across those 64 Ticks is not. **The *offsets* are hash-bearing by the same measurement, so what remains available here is the freedom to give a *new* Layer an offset nobody else holds** — which is the whole of the smoothing benefit and none of the design change. *This bullet used to offer noise as the second example; noise stopped being a Map Layer in [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md).*

Diffusion is **double-buffered**, and not merely for tidiness: in-place diffusion is order-dependent, which is simultaneously a determinism hazard and a visible directional smear in the resulting field. Kernels are separable — two 1-D passes rather than one 2-D pass — and arithmetic is integer with explicit rounding.

**4. Sweep Rules, staggered and partitioned.** Zone Rules and Policies ([`02 §4.2`](02-simulation-model.md)) poll a population on a time trigger, and they are the one place in the design where cost is deliberately proportional to entity count rather than to activity. They stay cheap by the same three multipliers as the Map Layers above — low frequency, stagger so no Tick carries every sweep, and Chunk partition where the population is spatial. The figure to hold: eight Policies over ten thousand Households is ~10 integer comparisons per Tick amortised. If that ever stops being true, the fix is a longer interval or a finer stagger — **never** re-homing the mechanism as a Bin Rule, per the State Hash rule in §4.

**5. Only then, pathfinding on a worker pool.** §6, step 3. Deferred to fourth on purpose: the first three steps change the shape of the profile enough that measuring pathfinding cost before taking them would measure the wrong thing.

**What is not on this list** is as important as what is. No parallel agent updates, no job graph, no lock-free structures, and no SIMD until something in a profile demands it. Every one of those is a plausible-sounding optimisation that would have to be maintained for years, and the Factorio evidence in §6 is that at least two of them can make things measurably worse.

---

## 10. Rendering

**Do not render every Citizen.** This is a design decision, not a technical compromise, and it is the biggest single win available on the render side. The rule from [`00-vision.md`](00-vision.md):

> **Every visible agent is a promise you have to keep.** If a behaviour cannot be afforded at full fidelity, do not draw it individually.

Concretely: render Vehicles, which are sparse and visually meaningful and whose behaviour on a Microscopic Segment is genuinely simulated, and render a **sampled subset** of nearby pedestrians. If 200 pedestrians are visible in a District that contains 8,000, no player will know — and the ones drawn are drawn correctly, which is the whole point. Willmott's observation is the argument in reverse: closing the visualisation gap removes the player's ability to rationalise, so every individual you draw is one whose behaviour is now being judged.

Note that this is *purely* a render-side sampling decision. It changes nothing about the simulation: all 8,000 Citizens exist, hold real Trips, and are advancing normally, and [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) guarantees that which ones happen to be drawn cannot affect any of it.

**Aggressive distance LOD and frustum culling from day one, not as a later optimisation pass.** Cities: Skylines 2 is the cautionary tale and the specifics are worth remembering: it shipped **121 million input vertices per frame**, no LOD variants on many assets, no occlusion culling, and character models with fully-modelled teeth. *Its simulation was fine.* Its renderer was not, and no amount of simulation quality survived the impression the renderer created. A renderer that is fast from the beginning also stays fast, because the constraint shapes the assets; one retrofitted later has to fight every asset already made.

The render pipeline, in outline:

| Concern | Approach |
|---|---|
| Buildings | Per-Chunk MultiMesh per archetype (§5); Chunks culled by frustum and distance before any instance is considered |
| Building LOD | Distinct meshes per distance band, selected per Chunk rather than per Building so a Chunk swaps as a unit |
| Vehicles and pedestrians | MultiMesh, populated per frame from `visible_agents(aabb, alpha)`; pedestrians sampled |
| Interpolation | Positions interpolated between two sim states by `alpha`; the sim rate is free to be far below the frame rate |
| Hot path escape hatch | `RenderingServer` driven directly, bypassing the scene tree, if node overhead ever shows up in a profile |

Low-poly is a permanent commitment rather than a placeholder ([`00-vision.md`](00-vision.md)), and it is precisely what makes the instance counts affordable.

---

## 11. Open questions

Genuine forks. Each is deliberately unresolved, and each names what would settle it.

1. **Microscopic-Segment routing: HPA\* versus distance-vector.** Deferred to the Phase 0 spike, where it can be settled with numbers rather than argument. The suggested resolution is to build the zone-to-zone travel-time matrix first — it is needed for Statistical Segments regardless — and then measure how much routing work is actually left over. If the matrix carries accessibility and commute queries, the residual problem is narrow and HPA\* is the low-risk answer; if it proves too coarse or too stale, distance-vector's unified answer becomes attractive, **with DSDV sequence numbers**, which Citybound's implementation lacks. Full standing of the decision in [`references.md` §2](references.md).
2. ~~**Tick rate, and how many Ticks make a Day.**~~ **Settled** — `TICKS_PER_DAY = 8192`, reference rate 16 Ticks/s, world-creation constant. See [`02-simulation-model.md` §1.2](02-simulation-model.md) and [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md). Three corrections this produced that matter here specifically: **tick rate is not a simulation parameter** — it is the same action as the speed multiplier, costs CPU linearly and buys no car-following fidelity, since that constraint is expressed in Tiles per Tick rather than in Hertz. **`WHEEL_SIZE` is set by the longest common event horizon rather than by the Day** — which lands on the same number, 8192, because the longest routinely-scheduled event is a work shift. Sizing the wheel *below* a Day to save memory would make sleeping entities wrap and be touched repeatedly, discarding the wheel's entire benefit for 64 KB. And **the Tick loop must dilate wall-clock time rather than skip Ticks** when it saturates, or replay and the State Hash break.
3. **Chunk size.** 32×32 is the working figure and it is unvalidated. The roles in §5 pull in different directions — render culling wants to be fine, layer cells want to be coarse, save records want to be big enough to amortise overhead. **Pathfinding no longer pulls at all:** [`adr/0040`](adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md) makes the HPA\* cluster a whole number of Chunks, sized independently, because the cluster is `(derived AND rebuilt)` while the Chunk is in the save — so this question is now settled by the **render end alone**, and spike S2 informs it rather than deciding it. It is cheap to change now and expensive once save files exist, and **there is no written migration path for changing it afterwards** — recorded as open in [`plans/0002`](../plans/0002-open-questions.md).
4. **The snapshot interchange format.** `visible_agents` returning flat spans of value types read directly by the shell is the intended shape, but the exact form — and whether it is a per-frame copy or a read directly out of the Past — is unmeasured. [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) names snapshot marshalling cost as a trigger for revisiting the boundary, so this wants measuring early rather than late.
5. **Whether the travel-time matrix is saved or rebuilt on load.** Saving it is simple and makes load instant; rebuilding it keeps the save smaller and removes a whole class of stale-derived-state bug, which is exactly what the save/reload test exists to catch. Leaning rebuild, with the matrix treated as a cache rather than as state.
6. ~~**Where the Ruleset version boundary sits for saves.**~~ **Settled** — see §7. Two policies, not one: **play** permits a cross-Ruleset load with degradation and warnings, **replay** refuses an *unaccounted* mismatch, and the discriminator is §4's State Hash rule. A logged transition is replayable because degradation is a pure function; what defeats replay is a changed binary. The Input Log gains a **Ruleset content hash**, saves gain a **provenance trail** of what each transition destroyed, and an unaccounted load marks a save **hash-broken** for the rest of its descent.
