# City Simulation Game — Foundational Design & Documentation Plan

## Context

Christian wants to build a city-building simulation game he'd personally want to play. He is a strong general-purpose developer with **zero game development experience**. The repository (`~/Code/city-sim`) is empty — this is fully greenfield.

The game is a deliberate fusion of three traditions:
- **SimCity-style zoning** — the player paints R/C/I zones and lays infrastructure; the city grows itself.
- **Deep agent simulation** (Cities: Skylines / Dwarf Fortress) — citizens are real, persistent individuals with homes, jobs, and lives you can inspect.
- **Supply chains** (Anno) — goods are produced, moved, and consumed for real, and shortages have consequences.

This document is **not an implementation plan for the game**. It is a plan for producing the design documentation set that will govern implementation, and it records the architectural decisions already settled through research and discussion so they don't have to be relitigated.

### Decisions locked in this session

| Question | Decision |
|---|---|
| Core fantasy | God-mode zoning sim with a deep agentic simulation underneath |
| Simulation depth | Hybrid, but built as a single agent model with variable fidelity — never two parallel systems |
| Presentation | Low-poly 3D |
| Scale | Ship at ~10k citizens; architect so raising the ceiling is tuning, not rewriting |
| Pressure | Layered (economic + logistics + shocks) with **player-adjustable intensity**, from relaxing sandbox to hard challenge |
| Era | Single modern era; progression via unlocks, not eras |
| Supply chains | Shallow named chains, 3–8 goods |
| Goods transport | **Tiered** — pooled within a district, physically shipped between districts and to/from outside |
| Fidelity degradation | **Player-controlled fidelity budget**, surfaced honestly in the UI |
| Road network | **Grid-snapped local streets + a small number of freeform arterials** (highways, rail) |
| Citizen depth | **Ship with role + routine.** Design the record so household economics can be added without restructuring. |

---

## The research that drives everything below

Four deep research passes were run: game engines, city-sim prior art, large-scale agent-simulation technique, and Citybound specifically. The findings are not decoration — several of them overturn the obvious design.

### Finding 1 — GlassBox's failure has a single, identifiable cause

SimCity 2013's GlassBox engine attempted almost exactly this fusion. Its **production** model was excellent and should be adopted nearly verbatim. Its **movement** model was catastrophic, and Andrew Willmott stated the cause in his GDC 2012 talk *as a feature*:

> "No per-agent routing info."

Agents descended a shared cost-to-nearest-sink gradient field. A Sim therefore could not have *my* home or *my* job — only "the nearest thing advertising `Home`". Every mocked behaviour (memoryless sims, tourist packs, fire trucks clumping at one fire) follows inevitably. It also explains the 2km × 2km map cap.

**Rule for this project: routing intent lives in the agent, never in the world.** A flow field may tell an agent which way to turn; it must never tell the agent where it is going.

### Finding 2 — Cities: Skylines 1 supplies the missing piece

CS1 split the **entity** from its **embodiment**:
- `Citizen` — a cheap persistent record with a real home and real workplace. Cap ~1,048,576.
- `CitizenInstance` — the expensive materialised walking/driving thing. Cap 65,536, created on demand when travel is actually required, released on arrival.

This is not a compromise on "deep agent simulation" — it is strictly better than simulating everyone fully, because identity is preserved permanently while cost is paid only for motion. Watch Dogs: Legion's *Census* system and RimWorld's `WorldPawns` are the same idea.

### Finding 3 — Uncapping is worse than capping

CS1 capped agents and cheated openly (stuck vehicles teleport home; committed paths never re-route). CS2 removed the caps and gave every agent dynamic re-pathing. **CS2 is the one whose simulation collapses.** Per-agent replanning cost spikes exactly when the city is congested — precisely when there is no headroom. Player instrumentation traced sim-speed collapse directly to pending pathfinding query count.

A hard cap with visible, honest degradation beats an uncapped system that degrades into unplayability. This aligns with the chosen **player-controlled fidelity budget**.

### Finding 4 — The real performance killer is elapsed time, not city size

Dwarf Fortress's framerate death is driven by *monotonically accumulating item stacks*, not fortress size. Every persistent object is a permanent tax on hauling and spatial queries. Correspondingly, RimWorld had to ship a garbage collector for demoted world-pawns because the demoted set grew without bound.

**Every producer in this simulation needs a matching sink, and the demoted-agent set needs a GC from day one.**

### Finding 5 — Camera-driven LOD would silently destroy determinism

The intuitive design — "promote agents near the camera to full fidelity" — makes simulation state depend on render state. That breaks replay, save-as-input-log, and reproducible bug reports, which are the highest-leverage tools available to a solo developer on a project this complex.

**The LOD focus point must be a simulation input recorded in the input log, not a camera read.** The camera can *drive* that input, but the simulation only ever sees the recorded value.

### Finding 6 — An ECS is probably the wrong tool here

ECS earns its complexity through *heterogeneous composition*. A city sim's population is **homogeneous** — every citizen has identical fields, and there are perhaps 8–15 entity types, all known at compile time.

The better model is **entities as rows in typed tables** with generational handles (`{index: u32, generation: u32}`). This yields: trivial serialisation (save = array dumps), total control over iteration order and thus determinism, no framework upgrade silently reordering anything, and no engine coupling.

### Finding 7 — Citybound: the thesis survived, the yak-shave didn't

Anselm Eickhoff spent 2014–2020 solo on exactly this ambition — *"never use statistical models"*, every household and every trip microscopically simulated. **The project did not fail because microscopic simulation is impossible.** He got ~400,000 individually-simulated cars running **on a single core**, with a real production economy underneath. The thesis is viable and there is headroom he never touched.

It stalled for traceable, avoidable reasons:

- **He wrote everything himself**: a bespoke actor runtime (`kay`), allocator (`chunky`), compact-memory trait (`compact`), build-time codegen (`kay_codegen`), geometry kernel (`descartes`), renderer (`monet`, twice), and procedural-geometry library (`michelangelo`). Ten-plus libraries, three engine rewrites, zero shipped games. All are now dead code with no external users.
- **The bespoke runtime's central promise was never collected.** Kay's entire tax — a `Compact` constraint on every type, generated code in every module, nightly-only compiler features, a codebase contributors couldn't enter — was paid to buy transparent multi-core parallelism. The README's `- [ ] multiple cores` checkbox is still unticked, and the runtime is `Rc`/`RefCell` throughout. He bought distribution across *machines*, which he never needed, and never got parallelism across *cores*, which he did.
- **Compile times killed the simulation work.** Warm rebuilds took 60–120 seconds in a core that was *"highly interdependent and thus nearly impossible to break up into crates."* His own final devblog concedes he'd *"been abandoning the simulation aspect for a while"* in favour of procedural architecture and UI — the parts he could actually iterate on. **For a game whose entire value is emergent behaviour, an unbalanceable simulation is a fatal condition.**
- **8,100 GitHub stars produced approximately zero engineering help**, because *"the idiosyncrasy of the codebase made it hard for others to contribute more than housekeeping."*
- He then correctly diagnosed his bottleneck as *"the friction within and between [my] tools"*, chose to fix the tools rather than the game, and discovered the tools were a better business. Citybound was **outcompeted by its own yak-shave.**

The chosen grid-plus-arterials road model already sidesteps his single largest sink — freeform lane geometry, where he read *"hundreds of papers, master and PhD theses and books"*.

### Finding 8 — The Simulator Effect, and the strongest argument against this whole design

Don Hopkins, who worked with Will Wright on SimCity and The Sims, on Citybound:

> "Trying to simulate every microscopic detail doesn't necessarily translate to a fun game (or even a realistic simulation). … Will Wright defined the **'Simulator Effect'** as how game players imagine a simulation is vastly more detailed, deep, rich, and complex than it actually is: a magical misunderstanding that you shouldn't talk them out of. He designs games to run on two computers at once: the electronic one on the player's desk, running his shallow tame simulation, and the biological one in the player's head, running their deep wild imagination."

This deserves to be confronted rather than dismissed, because it is the most credentialed possible objection to the core premise. Note that it points the same direction as Willmott's independent observation that closing the visualisation gap removes the player's ability to rationalise.

**The resolution this project should adopt: the payoff of microscopic simulation is not realism — it is *explicability*.** A statistical model cannot answer "why is this neighbourhood dying?" A microscopic one can, all the way down to a named household whose commute got too long. That is a real, perceivable, defensible benefit that fakery cannot provide, and it is what pillar 4 (legible failure) is for.

The corollary is a hard requirement: **build the citizen/building inspector early, as a forcing function.** If the causal chain isn't visible to the player, the simulation's depth is invisible and Hopkins is right. It also happens to be the only viable debugger for emergent behaviour — without it, the sim cannot be debugged at all.

### Finding 9 — Engine choice is low-stakes *because* of the architecture

With the simulation as a pure library holding zero engine types, the engine becomes a swappable rendering and UI shell. Optimise the choice for **UI ergonomics and instanced rendering**, not "simulation power".

---

## Recommended technical architecture

### Stack

**Godot 4.7 + C#**, with the simulation as a standalone engine-agnostic .NET library.

```
Borough.Core       Pure C# class library. Zero Godot references.
                   Typed tables, integer/fixed-point math, fixed tick,
                   step(inputs) API. This is the game.
Borough.Tests      xUnit + BenchmarkDotNet. Determinism and invariant tests.
Borough.Headless   Console runner. Fast-forwards thousands of ticks/sec.
Borough.Godot      Thin shell: chunked MultiMeshInstance3D rendering,
                   Control-based UI, per-frame snapshot from the sim.
```

Rationale, briefly:
- A city sim is roughly 60% data-dense UI. Godot's entire editor — inspectors, dockable panels, tree views, graph editors — is built from the same `Control` nodes available to the game. No other engine offers that proof point for free.
- `MultiMeshInstance3D` handles thousands of low-poly buildings in few draw calls. **Caveat:** all instances in one MultiMesh share a single AABB for culling, so the city must be chunked into a grid of per-chunk MultiMeshes. This is real work, not a footnote.
- `RenderingServer` can be driven directly, bypassing nodes entirely for the hot path.
- Free, MIT, no revenue share, no vendor risk over a multi-year solo project.
- GDExtension is a genuine escape hatch: if profiling ever shows C# can't reach the target agent count after SoA + `Span<T>` + amortised ticking, the *sim core alone* can be rewritten in Rust and dropped in. The architecture makes that a contained change.

Runner-up: **Bevy (Rust)**, which would jump to first place if Christian were already a fluent Rust developer. Two real city builders are in development on it. Held back by: no editor, a UI toolkit that only gained a text input widget in mid-2026, long compile times, and a permanent migration tax pre-1.0.

Explicitly rejected: **Unreal 5** (optimised for the one thing not needed here; worst-in-class UI story), **custom engine** (2–4 years to reach day-one Godot parity), and **Unity DOTS** specifically — not Unity itself, but DOTS, because it would weld the simulation to Unity and directly contradict the engine-agnostic requirement.

### The simulation model — GlassBox's rules over integer bins

Adopt GlassBox's production model, which is genuinely excellent and data-driven:

- **Resources** live in **bins**: an integer in `[0, capacity]`. No floats, no continuous flows.
- **Units** (buildings) are collections of bins plus a spatial footprint.
- **Maps** are grids where each cell is a bin — used for both environmental stock and derived fields (pollution, land value, desirability).
- **Rules** move and convert resources between local bins, global bins, map cells under the footprint, and nearby units. A rule applies **atomically** — only if the entire result is valid. `rate N` strides ticks; `applyCount min max` scales throughput without more entities; `onFail <otherRule>` chains, which gives supply-chain fallback (e.g. "can't source locally → import") for free.
- **Zone rules** are the R/C/I growth layer: on a time trigger, sample a few random cells, run tests, and create/upgrade/destroy a unit. Sampling rather than scanning keeps zone growth O(1) regardless of zone size.

The whole thing should be data-driven and hot-reloadable. That was GlassBox's stated reason for existing, and it was the right reason.

### The core design synthesis — demand that emerges

This is the idea that distinguishes the game, and it should be stated as pillar #1 in the vision doc.

In SimCity, RCI demand is a **magic global number** derived from a formula. In this game, demand is an **observable consequence** of agents and goods:

- **Residential** grows when citizens can reach jobs within an acceptable commute, and shrinks when they can't. Not "residential demand is 40%" — rather, *these specific households* are looking for housing near *these specific jobs*, and a lot with good access gets developed.
- **Commercial** thrives where goods actually arrive and citizens actually walk in with money. A shop with no delivered stock has empty shelves and declines, regardless of how much "demand" a formula reports.
- **Industrial** grows where inputs are reachable and outputs have somewhere to go.

The mechanism is GlassBox's zone rules, but with the tests reading *real* simulation state rather than global scalars. A zone rule that samples three lots a day and asks "does this lot have road access, reachable jobs within the commute budget, and adequate services?" produces SimCity's felt experience while being causally honest all the way down.

**Adopt SC4's commute-time budget with trip failure.** A trip that can't reach its destination within a cost budget *fails*, and a building whose trips keep failing declines or is abandoned. This is a genuinely good mechanism on three counts: it makes geography matter, it's legible to the player, and it bounds pathfinding work per trip. SC4's own mistake — worth not repeating — was optimising path cost for *distance* while scoring the player on *time*, which made the traffic system unlearnable. **The path cost must be the same quantity the player is being scored on.**

### Agent LOD — three tiers, one identity

| Tier | State | Cost | Population |
|---|---|---|---|
| **Detailed** | Position on a road segment, velocity, per-tick movement, queueing | ~µs/tick | Hundreds–low thousands (**pool-capped**) |
| **Statistical** | Origin, destination, departure tick, arrival tick from travel-time lookup. No position. | ~ns, only on transitions | Tens of thousands |
| **Cohort** | N citizens sharing home/work/demographic bucket; per-capita aggregates | Amortised | Hundreds of thousands |

Three tiers, not eight. Eight is an academic result; solo developers ship three.

**Invariants that make promotion/demotion safe** — these are the part everyone gets wrong, and they belong in the design doc as explicit assertions:

1. **Conserved quantities live in the record, not the simulation.** Money, goods, health, employment are fields on the persistent row. The moving entity is a *view*, not the owner.
2. **Promotion must be reconstructible.** Given `(citizen_id, tick)`, the detailed state must be computable. If a demoted commute is "departs 480, arrives 512", promoting at tick 495 must place the citizen ~47% along the route. If it can't be reconstructed, it can't be demoted.
3. **The statistical model must be calibrated against the detailed model.** The detailed sim should *emit* the distributions the statistical tier consumes. Otherwise the LOD boundary becomes an exploitable game mechanic — this is exactly what happened with Bannerlord's autoresolve.
4. **Hysteresis at the boundary.** Promote at distance D, demote at 1.3·D, or oscillating entities re-run promotion every tick.
5. **Demotion is lossy only in enumerated ways.** Write down what is discarded. A detailed citizen "stuck in traffic" with no statistical equivalent gets silently teleported.
6. **Budget-driven, not merely distance-driven.** Fixed-size high-fidelity pool; evict lowest-relevance when full. This is what converts a soft frame-time ceiling into a hard one — and it is the mechanism behind the player-facing fidelity budget.
7. **A garbage collector for the cohort tier, built on day one.** Demotion is cheap, so the temptation is to demote instead of delete, and then the demoted population becomes the performance problem.

### Traffic — the lane is the entity, not the car

Citybound's single best engineering idea, and it should be adopted directly:

> "Cars themselves are not actors, one lane is the atomic actor, it updates all the cars on it in one go."

A lane is a **sorted 1-D queue** of vehicles. Car-following runs the Intelligent Driver Model along that queue — O(n) with perfect cache locality and no spatial index at all. Multi-lane behaviour emerges from a 1-D model plus an explicit **overlap relation**: interacting lanes exchange their vehicles' projected positions once per tick as obstacles mapped onto each other's coordinate space.

**Lane changing gets a first-class object rather than a special case.** An invisible "switch lane" spans the overlap region between two parallel lanes; the two normal lanes are not connected to each other, only each to the switch lane. Merging — the nastiest special case in traffic simulation — becomes a normal object obeying normal rules, including merges the driver aborts when required braking exceeds comfort.

This collapses the hardest part of traffic simulation into something cheap and debuggable.

### Pathfinding — two viable approaches, decide during the spike

There are two credible designs here and the choice should be made with profiling data, not in advance. **This is the single most consequential open technical question.**

**Option A — HPA\* + travel-time matrix** (the mainstream approach):
- **HPA\*** (hierarchical A\* over clusters) for the detailed tier. Its decisive property for this project: when the player bulldozes a road, only the affected cluster's intra/inter edges need recomputation — not the whole map. It also returns a *sequence of sub-problems*, so the first can be solved and movement begun while the rest is deferred or never computed.
- **Zone-to-zone travel-time matrix** for the statistical tier. ~100–400 zones; a 400-zone matrix is 640KB and rebuilds on a background thread in seconds. Statistical citizens do no pathfinding at all — they read `arrival = departure + T[home][work] + noise`.
- **Flow fields only for genuinely one-to-many queries** — nearest hospital, nearest fire station, nearest map exit. One field per *destination* makes them the wrong shape for commutes, which is precisely the mistake GlassBox made.
- **Congestion feedback must be damped.** Undamped, everyone piles onto the empty road, which congests, and next tick everyone piles back. Smooth the cost signal, stagger reroute decisions across ticks, and add per-agent perceived-cost noise. Note CS1's blunter answer: vehicles commit to a route and never recompute.
- **Path caching with epoch-based lazy invalidation.** The road network carries a monotonic epoch; cached paths store the epoch they were computed under and revalidate on next use. Never flush globally on edit. OpenTTD's history here is instructive: adding a road-vehicle path cache took tick time from 62.5ms to 19.75ms — and then shipped a bug where the cache missed network changes.

**Option B — distance-vector routing protocol** (Citybound's approach, and genuinely underrated):

Rather than searching per agent, route vehicles *like packets on the internet*. Certain lanes are elected **landmarks**; every other lane learns which landmark region it belongs to and its hop count. Each lane holds a routing table keyed by destination, and a lane whose routes changed pushes its table *backwards* to its predecessors with costs incremented. A vehicle carries only a destination and asks each lane "which successor do I take?" — one table lookup, no search.

The decisive property: **cost scales with graph churn, not with agent count.** In a game where the player constantly edits the network and there are 10⁵–10⁶ agents, that is exactly the right trade. Route storms after edits are damped by a timeout. Eickhoff's own framing:

> "assume that the graph constantly changes anyways (also because the player can add/remove roads quite rapidly) and route the cars more like packets on the internet than with static A\*-ish algorithms."

**Option B carries a mandatory prerequisite: DSDV sequence numbers.**

Reading Citybound's `pathfinding/mod.rs` directly turned up a defect. Its routing entries hold `distance`, `distance_hops`, `outgoing_idx`, and `learned_from` — and **no sequence numbers**. Classical distance-vector routing without per-destination sequence numbers suffers **count-to-infinity**: when a link is deleted, stale routes circulate and distances creep upward one hop at a time instead of the route being cleanly withdrawn. Its `routing_timeout` (15 ticks) and `forget_routes` are ad-hoc mitigations for exactly this.

In a normal network, link deletion is a rare fault. **In a city builder it is the core player verb.** Perkins & Bhagwat solved this in 1994 (DSDV). If we adopt Option B, we adopt DSDV's version, not Citybound's.

**How to decide:** the pathfinding spike implements the travel-time matrix first (needed for the statistical tier regardless), then benchmarks A and B for the detailed tier. Option A's risk is query volume, which is precisely what collapsed CS2. Option B's risks are convergence behaviour after large edits, routing-table memory, and the sequence-number work above.

**Current standing — evidence favours Option A**, for two reasons that emerged from reading rather than from first principles:

1. Citybound's landmark election exists partly to *impose* structure on an irregular freeform graph that has none. We chose grid-snapped roads, so we already have the regular tiling that HPA\*'s cluster abstraction assumes. The exotic-looking approach is partly a workaround for a problem our road decision already eliminated.
2. Option B without sequence numbers imports a known failure mode triggered by the game's single most common player action.

Not settled — the spike decides with numbers. But B is no longer the cheaper-looking option.

**PRA\* was evaluated and rejected.** Its headline feature — partial refinement, so an agent starts moving before planning completes on a 1 ms/frame budget — solves an RTS problem we don't have. And its dynamic-update story is one sentence and a never-written follow-up paper. Structurally, PRA\*'s abstraction is *derived* from connectivity, so a topology edit changes the partition itself; HPA\*'s clusters are a partition we chose, so invalidation is bounded by construction.

**The reframing worth carrying into the spike:** our dominant query is not point-to-point pathfinding. The household location-choice loop asks "what is the commute from this candidate dwelling to any job?" tens of thousands of times per cycle — a many-to-many query. That is answered by the zone-to-zone travel-time matrix, not by a router. If the matrix carries accessibility and commute queries (and §5.8 of the simulation model makes that a rule), the residual routing problem is narrow, and HPA\* is the low-risk answer. **Build the matrix first and measure what work is actually left.**

Full bibliography and reasoning in [`docs/references.md`](../docs/references.md).

### Multi-modal transport — decide before the routing layer, not after

**This is an open decision that must be resolved early**, because it is the one Citybound got wrong in a way that couldn't be retrofitted.

He built cars first and planned to add pedestrians and transit later. Cars-only shaped the lane model, the routing model, the trip model, and the zoning model — and multi-modal routing (walk → bus → walk, with transfers, schedules, and waiting) is *categorically* harder than single-mode routing, not incrementally harder. Pedestrians were still unbuilt when the project stopped, and the recurring community critique — *"if your only mode of transport is cars you can hardly call what you're building a city"* — was never answered.

If transit and walking are wanted at all, the trip abstraction must be multi-modal from the first line of code, even if only the driving mode is implemented. A `Trip` should be a sequence of legs, each with a mode, from the outset. That costs almost nothing now and is close to impossible to retrofit.

### Road network — hybrid grid, and why

Local streets snap to the tile grid; a small, bounded number of **arterials** (highways, rail, major boulevards) are freeform splines.

This is a scope decision more than an aesthetic one. Fully freeform multi-lane roads make intersection construction a hard computational-geometry problem: boolean operations on curved polygons, lane connectivity resolution across arbitrary junction fan-in, and junction mesh generation. It is the problem that consumed Citybound.

The hybrid confines that difficulty:
- Grid-to-grid intersections are trivial and the pathfinding graph falls out of the tile grid for free.
- Arterial-to-arterial junctions are **rare**, so they can be restricted to a set of authored junction pieces (cloverleaf, diamond, trumpet) that the player places, rather than procedurally generated from arbitrary geometry.
- Arterial-to-grid connections happen only at authored on/off-ramp pieces.

The routing graph is therefore uniform — nodes and edges — regardless of how a road is *drawn*. Geometry is a rendering concern, not a simulation concern. The simulation should never see a spline.

Consider allowing diagonals on the grid (SimCity 4 / Transport Tycoon style). It buys a meaningfully more organic look for near-zero cost, since the graph stays discrete.

### Citizen record — start narrow, leave room

Ship with **role and routine**: name, age, home, workplace, current activity, commute, and needs satisfaction. This is roughly 30–60 bytes per citizen and is cheap enough to keep a million of them.

Household economics (income, expenses, savings, purchases made and purchases missed) is the planned next layer, and it's the one that makes the supply chain legible at the individual level. **The record layout must accommodate it without restructuring** — which the hot/cold split does naturally: economics goes in the cold table, touched only when a household transacts or the UI inspects it.

Deliberately deferred, possibly forever: Dwarf Fortress-style life histories, relationships, and memories. If they're ever wanted, the Watch Dogs: Legion approach is the right one — generate detail deterministically from `hash(citizen_id, seed)` on demand rather than storing it. Persistent identity does not require persistent memory.

### Household behaviour — bounded, sticky, satisficing

Citybound's most expensive lesson, available here for free.

Eickhoff first modelled households as rational agents planning their day by evaluating hypothetical activity sequences. This produced *"a horrible combinatorial explosion"*, and he eventually identified the formal problem: a Multiobjective Time-dependent Arc Orienteering Problem with Time Windows — **NP-hard, seconds of CPU per agent**. He spent months there before retreating.

What replaced it is both cheaper *and* better:

- Households keep a **short, sticky list of favourite providers** — the shops, workplaces, and services they already know about.
- They switch only when a *known* alternative is *substantially* better, not marginally.
- They address the biggest unmet need first, re-evaluated a few times a day.

His reason for the change is the important part, and it isn't performance:

> He realised he was modelling *"perfect knowledge and rational, motivated and flexible people"* … instead, people *"might just have heard about a couple other options and only if the best option they know about is much better than what they currently have, they are motivated enough to change something."*

Three benefits at once: O(1) per decision; more realistic than optimality; and — critically — **it prevents the synchronised-herd behaviour that plagues optimal-agent economies**, which is the same oscillation pathology that damped congestion feedback exists to solve. Optimal agents all discover the same best option simultaneously and stampede.

### Resource values — relative for needs, absolute for goods

A split, taking the best of both references:

- **Physical goods** use GlassBox's integer bins: absolute counts in `[0, capacity]`. Necessary for supply chains to be conserved and auditable.
- **Household needs** (satiety, rest, satisfaction) use Citybound's relative scalars where **0 = ideal**, expressing *how well is this household doing* rather than a stockpile.

Eickhoff's warning is worth quoting because it's a balancing problem, not a performance one:

> "Having to rely on absolute amounts for resources makes balancing very hard and amplifies bugs and makes the system potentially very unstable."

Keep the resource taxonomy **minimal** — his shipped enum was ~10 entries with more deliberately commented out. The chosen 3–8 goods is the right ceiling; resist resource #20.

### Trips are first-class objects with an explicit fate

Every journey is an object with a recorded outcome — completed, failed to find a route, exceeded the commute budget, stranded by a demolished road. Failed trips must be **reportable, not silently swallowed**.

In a game whose central failure mode is "something is broken and I don't know why", this is the foundation of every diagnostic view. It is also the mechanism behind the commute-budget decline rule above.

### Hot-reloadable data from day one — a hard requirement

This is the requirement most likely to be skipped and most likely to be fatal, and Citybound is the proof.

Eickhoff's warm rebuild took 60–120 seconds in a core that was *"highly interdependent and thus nearly impossible to break up into crates."* His final devblog concedes he had *"been abandoning the simulation aspect for a while"* to work on procedural architecture and UI — the parts with a fast iteration loop. **The simulation didn't become unbalanceable because it was hard; it became unbalanceable because tuning it was slow, so tuning stopped happening.**

Therefore: **every** economy constant, production chain, household behaviour parameter, traffic parameter, zone growth rule, and architecture rule lives in reloadable data files. The compiled binary is a stable interpreter. This is the same conclusion GlassBox reached — hot-reloading everything was its stated reason for existing — and it is why the rule DSL is not an optional nicety.

The test: **changing a production ratio and seeing the effect must take seconds, not a rebuild.**

### Double-buffered world state — and crash forensics

Keep exactly two world states: **the past** (immutable, known-consistent) and **the future** (being computed). Costs 2× memory on the hot arrays and buys three things:

1. Safe parallel reads — any thread can read the past without coordination.
2. Async saves — snapshot the past and serialise it on a background thread while the sim continues.
3. **Crash forensics.** If the tick computing the future panics, the past is still consistent, so a savegame *of the crash* can be dumped and reloaded. Citybound went further and kept an inspector alive after a panic rather than dying.

For a simulation that will be debugged for years, (3) is worth more than it looks.

### Determinism

This is the highest-leverage investment available and it must exist from commit #1, because retrofitting it is close to impossible.

- **Integer and fixed-point math throughout the sim.** Money, goods, population, ticks, tile coordinates are naturally integral; sub-tile positions can be Q16.16. A city sim likely needs *zero* transcendental functions. Enforce with a CI lint.
- **Never iterate a `Dictionary`/`HashSet` in simulation code.** .NET randomises `string.GetHashCode()` per process, so `Dictionary<string,T>` iteration order differs between runs of the same binary. Use dense arrays indexed by generational handles.
- **Counter-based RNG**: `rng = hash(world_seed, entity_id, tick, purpose_tag)` rather than a shared mutable stream. This makes results independent of evaluation order, which means agent updates can be parallelised later with zero coordination and bit-identical results. Nearly free, and it eliminates the nastiest class of parallel-determinism bug. Do **not** use `System.Random` — its algorithm changed in .NET 6 and is not documented as stable.
- **Time is a `u64` tick counter.** The library never sees wall-clock time. `step(inputs)` — the host decides when to call it. Free fast-forward, free headless testing, free replay.

Payoffs to build early:
- **Save-as-input-log** — a 10-hour session is kilobytes. Bug reports become "attach your log."
- **State hashing as a bug oracle** — hash the sim every N ticks; divergence between two runs of the same log identifies the exact tick a bug entered.
- **The Factorio save/reload test** — run N ticks, save, reload, run M more; separately run N+M ticks; compare hashes. This catches *unsaved state*, which is otherwise nearly impossible to find.

### Performance strategy, in build order

1. **Event wheel / sleeping entities.** The single biggest lever, with zero concurrency complexity. A citizen at work for eight hours should consume *zero* CPU for eight hours. Every citizen carries `next_event_tick`; buckets are keyed by `next_event_tick % WHEEL_SIZE`. This turns "100k citizens × 30 ticks/sec" into "however many citizens have something happening right now" — typically a few hundred. Factorio got a 40× improvement on roboports this way.
2. **Chunking as the universal partition.** One 16×16 or 32×32 chunk grid simultaneously serves as the unit of dirty tracking, save serialisation, parallel work partitioning, aggregate caching, HPA\* clustering, and render streaming. Making these the *same* partition is a major simplification.
3. **Map layers coarse and staggered.** Diffusion at one cell per chunk rather than per tile is a 16–1024× reduction and looks identical. Run at low frequency (every 32–64 ticks), double-buffered (in-place diffusion is order-dependent — both a determinism hazard and a visible directional smear), and stagger layers across ticks so no single tick spikes.
4. **Only then** move pathfinding to a worker pool — it's pure, compute-heavy, and results can be drained in deterministic order. This is by far the best first parallelisation target.

Deliberately deferred: parallel agent updates, job graphs, lock-free structures. Factorio — the best-optimised simulation game ever shipped — is still largely single-threaded, and *both* of their documented parallelisation attempts made things slower. Electric network updates were memory-throughput bound; adding threads meant all threads waited on memory instead of one. Parallelise work that is compute-dense and read-only; do not parallelise work that is memory-bound and pointer-chasing.

### Rendering

- **Fixed sim tick, interpolated render.** Build this first; everything depends on it. CS1 simulates car movement at **4 Hz** and interpolates between two sim frames — nobody notices. Factorio goes further and *extrapolates* robot positions from a known spline and velocity, updating them once per 20 ticks.
- **Don't render every citizen.** This is a design decision, not a technical one, and it's the biggest available win. Render vehicles (sparse, visually meaningful) and a *sampled* subset of nearby pedestrians. If 200 pedestrians are visible in a district that "contains" 8,000, no player will know.
- **Aggressive distance LOD and frustum culling from day one**, not as a later optimisation pass. Cities: Skylines 2 shipped with 121 million input vertices per frame, no LOD variants on many assets, no occlusion culling, and character models with fully-modelled teeth. Its simulation was fine; its renderer wasn't.
- The public API surface should be roughly `step(inputs)` and `visible_agents(aabb, alpha)`. The library never knows what a camera is; the host passes an AABB and gets interpolated transforms back.

### The pressure model — layered, with an intensity dial

Three independent sources of tension, each separately tunable so the same game spans relaxing sandbox to genuine challenge:

1. **Economic** — service costs, tax tolerance, debt interest. The budget as antagonist.
2. **Logistics** — shortages when supply chains break under growth. Meshes directly with the goods pillar: if bread stops arriving, that must be visible in individual citizens' unmet needs, not just a global happiness number.
3. **Shocks** — recessions, migration waves, resource depletion, infrastructure failure.

Design constraints on the intensity dial:

- **It must scale parameters, not disable systems.** "Relaxing mode" should mean generous margins and rare shocks, not a different code path. A disabled subsystem produces a different game that has to be balanced and tested separately.
- **Shocks must be seeded and deterministic.** They come from the world seed and tick, never from wall-clock randomness — otherwise replay breaks.
- **Every pressure source needs a legible cause.** The failure mode here is a player who loses and doesn't know why. Each pressure should be traceable through the UI to the specific buildings, goods, or citizens involved.
- **Prefer pressure that emerges from the simulation** over pressure that is scripted on top of it. A recession that changes demand parameters and lets the consequences propagate is better than one that subtracts money.

### The design trap hiding in "low-poly 3D"

Willmott observed that statistical simulations let players rationalise random or even buggy behaviour as smart AI. GlassBox closed the visualisation gap and thereby removed that grace — every stupid agent decision became visible.

**Every visible agent is a promise you have to keep.** If a given behaviour can't be afforded at full fidelity, don't draw it individually.

### Goods transport — the tiered model

Per the decision above, and following Anno's example (which pools goods island-wide and only simulates ships, because inter-island shipping is the only part the player is meant to optimise):

- **Within a district:** goods flow through an abstract pool subject to connectivity. No trucks pathfind. Zero agent budget consumed.
- **Between districts, and to/from outside connections:** real vehicles carry real cargo and contribute real congestion.

This reserves the expensive simulation for decisions the player actually makes. GlassBox made everything agent-carried and every carried unit became a pathfinding query — a direct contributor to its map-size cap.

The transport layer should be **swappable per goods-type**, so the boundary can move once there's profiling data.

---

## Draft pillars and anti-goals

These seed `docs/00-vision.md` and should be argued with before they're accepted.

**Pillars**

1. **Causally honest.** Every number the player sees traces to something real in the simulation. No magic demand curves, no fudge factors the player can't reason about.
2. **The city is made of people.** Citizens have persistent identity, a real home, and a real job — always, at every fidelity level. You can follow any of them.
3. **You govern, you don't place.** The player zones, connects, funds, and regulates. The city decides what actually gets built.
4. **Legible failure.** When something goes wrong, the game can tell you exactly why, and point at the buildings, goods, or citizens responsible.

**Anti-goals**

- Not a factory game. Supply chains create meaningful pressure; they are not the primary optimisation surface. If zoning ever becomes an afterthought to logistics, the design has drifted.
- Not photorealistic. Low-poly is a permanent commitment, not a placeholder — it is what makes the agent counts affordable.
- Not a traffic-management game. Traffic is a consequence to be diagnosed, not a puzzle box of lane-level tools.
- Not multiplayer. (The deterministic core keeps the option open, but nothing in the design should be shaped by it.)
- Not moddable at launch — though the data-driven rule engine means it will be moddable almost by accident, which is a reason to keep the rule format clean.

## Proposed documentation set

To be created under `docs/`. The `CONTEXT.md` and `docs/adr/` conventions match the existing `grill-with-docs` and `improve-codebase-architecture` skills already installed.

| File | Purpose |
|---|---|
| `CONTEXT.md` | The domain language. Precise definitions of Citizen, Household, Unit, Zone, District, Bin, Rule, Trip, Cohort, Fidelity Budget. Every other doc and every identifier in the code uses these terms exactly. |
| `docs/00-vision.md` | The pillars, the fantasy, what makes this different from SimCity and Skylines, and — critically — the **anti-goals**. What this game is deliberately not. |
| `docs/01-player-experience.md` | Core loop, the player's verbs, session shape, what the first 10 minutes / 2 hours / 20 hours feel like, the pressure model and its intensity settings, failure states, and the information/UI design. |
| `docs/02-simulation-model.md` | The world model: tiles, chunks, units, bins, maps, rules, zones. The rule DSL specification. How R/C/I demand *emerges* rather than being a magic number. |
| `docs/03-agent-architecture.md` | The three LOD tiers, the seven promotion/demotion invariants, citizen identity and persistence, needs and daily schedules, the event wheel, and the cohort GC. |
| `docs/04-economy-and-goods.md` | The 3–8 goods and their chains, the tiered transport model, market clearing, budget and taxation, and how shortages propagate into citizen unhappiness. |
| `docs/05-technical-architecture.md` | Project layout, engine choice and its rationale, the sim/render boundary, the tick model, determinism rules, data layout, save format and migration, threading policy. |
| `docs/06-roadmap.md` | Vertical slices and milestones. What gets built first and what each slice proves. |
| `docs/adr/` | Decision records for the irreversible choices, each with context, options considered, decision, and consequences. |

**Adopt Citybound's guiding-concept vocabulary technique.** Define a small controlled tag set in `CONTEXT.md` — `EMERGENCE`, `LEGIBLE CAUSE`, `PLANNING`, `UNIQUE INDIVIDUALS`, `SOLVE THE ACTUAL PROBLEM`, `BOUNDED KNOWLEDGE` — and make every design decision link back to at least one. It makes unjustified decisions visible immediately, and it is the reason Citybound's design intent is still reconstructible six years after the last commit. It also self-diagnoses: the empty "Businesses" and "Real Estate Markets" pages in his doc were an accurate signal that the economy was his least-designed system.

Initial ADRs, each capturing a decision that would be expensive to reverse. **Every ADR must state what would cause it to be revisited** — a decision record with no reversal criteria is just an opinion:

- `0001-engine-and-language.md` — Godot 4 + C#, and the conditions under which this should be revisited.
- `0002-sim-render-separation.md` — the engine-agnostic core library as a hard boundary.
- `0003-deterministic-integer-simulation.md` — fixed tick, integer/fixed-point math, banned constructs, counter-based RNG.
- `0004-typed-tables-over-ecs.md` — why not an ECS.
- `0005-agent-lod-tiers.md` — three tiers, persistent identity, the seven invariants.
- `0006-routing-intent-in-the-agent.md` — the anti-GlassBox decision, stated explicitly so it is never accidentally reversed.
- `0007-tiered-goods-transport.md` — pooled locally, shipped regionally.
- `0008-capped-fidelity-with-honest-degradation.md` — the player-facing fidelity budget.
- `0009-hybrid-road-network.md` — grid streets plus authored-junction arterials, and the geometry work this deliberately avoids.
- `0010-data-driven-hot-reloadable-rules.md` — the iteration-speed requirement, framed as a project-survival concern rather than a convenience.
- `0011-lane-as-entity-traffic.md` — lanes own their vehicles; 1-D queues plus overlap relations.
- `0012-bounded-satisficing-agents.md` — no optimal agents, ever; the NP-hardness and herd-behaviour arguments.
- `0013-multi-modal-trips-from-the-start.md` — the `Trip`-as-leg-sequence commitment, even before transit exists.
- `0014-use-off-the-shelf-infrastructure.md` — an explicit standing bias against writing bespoke frameworks, with Citybound's ten dead libraries as the cited evidence. This one exists to be re-read whenever building something from scratch starts to feel reasonable.

---

## Build order for the implementation that follows

Documented here so the roadmap doc has a spine. Each milestone is a vertical slice that retires one specific risk.

**Calibration:** somewhere between sustained evenings/weekends and casual exploratory. Therefore the roadmap carries **no dates** — pure dependency ordering. Two constraints follow from that pace and should be treated as rules:

- **Every slice must leave the project in a working, runnable state.** There will be gaps of weeks; the project must be re-enterable cold.
- **Slices should be sized to be completable in one or two sittings** wherever possible. A milestone that can't be finished in a session tends not to get finished.

**Phase 0 — three spikes, before committing to Godot.** Each answers exactly one question, and they are throwaway code:
- 20k buildings via chunked MultiMesh with a rotating camera — *what is the rendering ceiling?*
- 30k agents traversing a road graph — *what is the pathfinding ceiling, and which routing approach wins?*
- One data panel with a live multi-series graph — *what is the UI ceiling, and how long does one panel actually take to build?*

The third is the one most likely to be skipped and the one most likely to change the decision. A city sim is mostly UI.

**Phase 1 — the foundation.**
1. **Tick and determinism harness.** `step(inputs)`, integer time, seeded counter-based RNG, state hashing, input-log record/replay. No graphics. First thing built — retrofitting determinism is close to impossible.
2. **Typed tables and handles.** Citizens, buildings, road segments as SoA arrays with generational handles and hot/cold field splitting.
3. **The rule engine, with hot reload working from the start.** Bins, units, rules, map layers, zone growth — all driven by reloadable data files. Testable entirely headless. *Do not defer the reload path; that is the Citybound failure.*
4. **Event wheel.** Sleeping citizens. Prove that idle citizens cost literally nothing.

**Phase 2 — the simulation proper.**
5. **The three LOD tiers**, with the invariants as runtime assertions, plus the cohort GC.
6. **Road network and routing.** Multi-modal `Trip` abstraction from the start (even if only driving is implemented). Lane-as-entity traffic. Zone-to-zone travel matrix; whichever detailed-tier router won the spike.
7. **Save/load** with a version header and a migration chain, plus the save→reload→compare-hash test.

**Phase 3 — making it visible.**
8. **First render.** Chunked MultiMesh buildings, interpolated agents, orbit camera.
9. **The inspector.** Click a citizen or a building and see the causal chain — where they live, where they work, what they need, why their last trip failed. **This is deliberately early.** It is simultaneously the answer to the Simulator Effect critique, pillar 4 made real, and the only viable debugger for emergent behaviour. Citybound never built it and the "why did that happen" question went permanently unanswered.
10. **First real data panel and map overlay.**

Note that steps 9 and 10 are the point at which the project stops being an engineering exercise and starts being a game. Getting there matters more than getting any individual earlier step perfect.

---

## Verification

This plan produces documents, not code, so verification is review-based:

- Each design doc should be readable standalone by someone who hasn't seen this conversation.
- Every term used in `docs/02` through `docs/04` must be defined in `CONTEXT.md`.
- Each ADR must state what would cause it to be revisited — a decision record with no reversal criteria is just an opinion.
- The roadmap's milestones must each name the specific risk they retire.
- Cross-check the finished set against the traps identified in research: routing intent in the world, uncapped agents, unbounded object growth, path cost misaligned with player scoring, over-promising via visible agents, unverified assumptions about where cost lives, silent cap exhaustion, slow iteration on simulation tuning, bespoke infrastructure, optimal agents, and single-mode transport.

## Immediate next step

Write the documentation set as specified above, starting with `CONTEXT.md` and `docs/00-vision.md` so the vocabulary and pillars are settled before the technical docs that depend on them, then the remaining docs and the fourteen ADRs.

Note for later: the `grill-with-docs` skill is installed and is a good fit for stress-testing these documents once they exist — particularly for finding places where two decisions above quietly conflict.

## Open questions deliberately left for the docs

These are real forks, not oversights. Each should be resolved in the document that owns it rather than guessed at now.

1. **Multi-modal scope.** Are pedestrians and public transit in the vision at all? The `Trip` abstraction must be multi-modal regardless, but whether transit is ever *implemented* changes the zoning model and the road hierarchy. Owned by `docs/01-player-experience.md`. **This is the highest-priority open question** — it's the one Citybound couldn't retrofit.
2. **Detailed-tier routing algorithm** — HPA\* versus distance-vector. Deliberately deferred to the Phase 0 spike, where it can be settled with numbers.
3. **Time scale.** How long is a game day in real seconds, and does the city age? This propagates into needs cycles, commute budgets, and the event wheel's resolution. Owned by `docs/02-simulation-model.md`.
4. **What the map is.** Single fixed map, procedurally generated terrain, or a region of connected tiles? Affects the outside-connection model for goods and the save format. Owned by `docs/02-simulation-model.md`.
5. **The exact goods list.** 3–8 named goods with real chains — but which ones, and what do they make citizens feel when they're missing? Owned by `docs/04-economy-and-goods.md`.
