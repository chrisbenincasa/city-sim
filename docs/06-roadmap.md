# 06 — Roadmap

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Pillars in [`00-vision.md`](00-vision.md). The world model in [`02-simulation-model.md`](02-simulation-model.md), movement in [`03-agent-architecture.md`](03-agent-architecture.md).
>
> This document supersedes the build order in [`plans/0001-foundational-design.md`](../plans/0001-foundational-design.md), which predates ADRs 0005 through 0011 and is stale in several specific ways. Where they disagree, this document is correct; the divergences are called out where they occur so the older text is not silently trusted.
>
> **Phase 0 and Phase 1 ordering is re-derived in [`plans/0003-build-plan.md`](../plans/0003-build-plan.md), which is correct where it disagrees with the tables below.** Three departures, each argued there: tables land before the hash, because `adr/0003` made the State Hash a property of the field declaration; the arithmetic substrate and the analysers are named slices rather than folded into milestones 1 and 2; and milestone 3c is split, because its Zone Rules half is gated on the Rule engine and its Map Layers half is not. **The phases, the milestone contents and the risk each retires are unchanged and remain authoritative here.** Phase 2 and Phase 3 are not planned there at all.

---

## How this roadmap works

Four rules govern everything below. The first three are pacing decisions; the fourth is a review criterion.

**1. No dates. Pure dependency ordering.** The developer works somewhere between sustained evenings-and-weekends and casual exploratory, and that rate is not predictable a month out. A dated plan would be wrong immediately and would then be quietly ignored, which is worse than having no plan. What *is* stable is what depends on what, so that is all this document commits to.

**2. Every slice leaves the project in a working, runnable state.** There will be gaps of weeks. The project must be re-enterable cold — check out, build, run, see something happen. A milestone that ends with the build broken, or with a half-migrated data format, has failed regardless of how much of it is written. In practice this means each slice ends with a test that passes and a thing you can watch.

**3. Slices are sized for one or two sittings wherever possible.** A milestone that cannot be finished in a session tends not to get finished — it gets 60% done, then sits for three weeks, and re-entry costs more than the remaining work. Where a milestone genuinely cannot be compressed to that size it is split below into explicitly numbered sub-slices, each independently completable and each leaving the build green.

**4. Every milestone names the specific risk it retires.** This is a visible field, not prose, and it is a filter: a milestone that cannot name a risk is either not necessary yet or is not understood well enough to start. "Risk retired" means *after this, we know something we did not know, or a class of failure is now structurally impossible.*

### What "runnable" means concretely

Rule 2 is easy to nod at and easy to violate, so it is worth pinning down. At the end of every milestone below, all of the following hold:

- `dotnet build` succeeds and `dotnet test` is green, on a machine with no GPU and no Godot installed. The headless runner is the primary interface for the whole of Phase 1 and most of Phase 2.
- The invariant assertions run in debug builds and pass: Goods conserved, no Bin negative or over capacity, no Citizen in two places, every Household's home a Building that lists them as an occupant.
- The long-run test passes — a headless run of 100k+ Ticks in which no collection trends upward once the city reaches steady state. This is the [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md) guard, and it belongs in CI from Milestone 4 onward rather than being added once something has already leaked. The failure it catches is invisible at design time and takes hours of play to manifest; we have already written it twice on paper.
- There is something to *look at* — a hash trace, a headless summary, a rendered city — that shows the milestone doing its job.

These are cumulative obligations, not milestones of their own. Adding a collection without a sink, or a code path that breaks the hash, breaks the milestone that introduced it.

### Open decisions that block milestones

Dependency ordering includes unresolved design questions, not only code. Four of the open questions in [`02-simulation-model.md` §11](02-simulation-model.md) and [`00-vision.md`](00-vision.md) sit on the critical path, and each should be settled before the milestone it blocks is started rather than during it.

| Open question | Blocks | Why it blocks |
|---|---|---|
| ~~How long is a Day in real seconds?~~ **Settled** — [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md) | ~~**4** (Event Wheel)~~ | It blocks the Wheel far less than assumed — `WHEEL_SIZE` is set by the longest routine sleep, which merely happens to be bounded by a Day. What the question actually governed was the traffic balance — the ratio of commute Ticks to Day Ticks — so it blocks **5c** and **6** instead |
| ~~What is the map — fixed, procedural, or a region of tiles?~~ **Settled** — [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md)–[`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md) | **10** (Save/load), still | The shape is fixed — bounded procedural rectangle, sparse Chunks, save is seed + edits, three version numbers in the header. What still blocks **10** is narrower: **map size** and the **Outside Connection layout** |
| ~~Are Districts player-drawn or automatic?~~ **CLOSED — `02 §2.1`: both.** Automatic by default, player-adjustable as an advanced action | ~~5c, 8~~ — blocks nothing | District boundaries decide whether a Goods movement is free or is a Shipment, and are the granularity of the travel-time matrix. **What was actually open was extent, not authorship**: `02 §2.1` bounds it by the pooling abstraction's own validity, so *"the count is physics rather than a design choice"*. Working anchor in `CONTEXT.md` → District — **128 Cells, 2.10 km²** |
| Is transit in the vision at all? | Nothing structurally, now | [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) removed the irreversibility. It is a content question, not an architectural one |

The last row is the interesting one: it was the highest-priority open question in the original plan, and a decision taken for adjacent reasons demoted it to a scoping question that can be answered whenever.

---

## Phase 0 — three spikes, before committing to Godot

Throwaway code. Not a foundation, not a prototype to grow — three questions with numeric answers, each answered by the cheapest thing that answers it. The output of Phase 0 is three numbers and a decision, and then the code is deleted.

[`adr/0001`](adr/0001-godot-and-csharp.md) chose Godot 4.7 and C# on the strength of two properties — data-dense UI ergonomics and cheap instanced rendering. Those are claims, and Phase 0 tests them before the project is shaped around them.

| # | Spike | The one question | Risk retired |
|---|---|---|---|
| **S1** | 20k Buildings via chunked `MultiMeshInstance3D`, rotating camera | What is the rendering ceiling? | That the per-Chunk MultiMesh split ([`adr/0001`](adr/0001-godot-and-csharp.md): one shared AABB per MultiMesh) does not actually deliver acceptable draw counts and culling at city scale |
| **S2** | 30k Travellers traversing a synthetic Road Graph | What is the pathfinding ceiling, and does HPA\* or distance-vector win? | The single most consequential open technical question in the design — and the one that collapsed Cities: Skylines 2 |
| **S3** | **One** data panel with a live multi-series graph | What is the UI ceiling, and how long does one panel actually take to build? | That Godot's `Control` UI cannot carry the Evidence drill-down, which is the load-bearing reason the engine was chosen at all |

**S2 has a prescribed order, and it is not the obvious one.** Build the **zone-to-zone travel-time matrix first**, then measure what work is left. [`references.md` §2](references.md) makes the argument: the dominant query is not point-to-point, it is *"what is the commute from this candidate dwelling to any job?"*, asked tens of thousands of times per placement cycle. That is a many-to-many query, and the matrix answers it. If the matrix carries accessibility and commute queries, the residual routing problem is narrow vehicle steering and HPA\* is the low-risk answer. If the matrix proves too coarse or too stale, distance-vector's unified answer becomes attractive — **with DSDV sequence numbers**, because classical distance-vector without them suffers count-to-infinity on link deletion, and link deletion is this game's core player verb rather than a rare fault.

**S3 is the spike most likely to be skipped and most likely to change the decision.** It is the least fun of the three, it produces the least impressive screenshot, and it answers a question that feels like it can be deferred. It cannot. **A city sim is mostly UI** — inspectors, budget panels, overlay controls, and above all the Evidence drill-down that Pillar 4 depends on. If one panel takes a fortnight, the project has a schedule problem that no amount of simulation quality will fix, and it is better to learn that from one panel than from thirty. Godot's own editor is built from the same `Control` nodes, which is the existence proof — S3 checks whether that proof transfers to a developer who has never used them.

**Exit criterion for the phase:** three recorded numbers — Buildings drawn at an acceptable frame time, Travellers routed per second and by which algorithm, and hours spent on one panel — and a written decision to proceed on Godot or to revisit [`adr/0001`](adr/0001-godot-and-csharp.md). The spikes are worthless if their results are held in the developer's head, because the whole value is being able to re-read them in a year when a performance question resurfaces. Record them; delete the code.

---

## Phase 1 — the foundation

No graphics in this phase. Everything here is `Borough.Core`, `Borough.Tests`, and `Borough.Headless`, and everything is verifiable from a terminal. The boundary in [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) is enforced from the first commit by the CI reference check, not retrofitted once it has already been violated.

| # | Milestone | Contents | Risk retired |
|---|---|---|---|
| **1** | **Tick and determinism harness** | `step(inputs)`, integer Tick counter, counter-based RNG `hash(seed, id, tick, purpose)`, State Hash, Input Log record and replay | Determinism, which is close to impossible to retrofit. Every later debugging tool is downstream of this one |
| **2** | **Typed tables and handles** | Citizens, Buildings, Segments as struct-of-arrays with generational handles `{index, generation}`; hot/cold field split; **one live world state, with double buffering only where a parallel phase reads *and* writes** ([`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md)) | That an ECS or a naïve object graph gets baked in, that save/load and deterministic iteration order become hard later, and that a full-world double buffer gets baked in and quietly cancels the Event Wheel |
| **3a** | **Rule engine — Bins and Rules** | Bins, atomic Rule application, `rate`, apply-count, `on_fail` chaining, TOML Ruleset | That the economy is not conserved and failures are silently partial |
| **3b** | **Rule engine — hot reload path** ([`adr/0015`](adr/0015-all-tuning-data-is-hot-reloadable.md)) | Ruleset swap at a phase boundary, dropped-resource and derelict-Building semantics, reload recorded in the Input Log | **The Citybound failure.** A simulation that is slow to tune is a simulation nobody tunes, and it then becomes unbalanceable |
| **3c** | **Map Layers and Zone Rules** | One value per **Cell**, double-buffered integer **convolution** (never relaxation), staggered schedule; Zone Rules sampling Lots. Noise and near-road pollution are **queries**, not Layers — [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) | That growth cost scales with Zone size rather than staying constant |
| **4** | **Event Wheel** | `next_event_tick` per Citizen, Building and Household; bucket drain in the Wake phase; mutators wake observers rather than entities polling | That cost scales with *number of Citizens* rather than *number of Citizens with something happening now* |

**3b is not optional and must not slip behind 3c.** The temptation is to build the Rule engine, get it working, and add reload "once the format settles" — which is precisely the order in which it never gets built, because by then there is a working system and reload is a refactor rather than a feature. The test the project holds itself to is stated in [`00-vision.md`](00-vision.md): **changing a production ratio and seeing the effect takes seconds, not a rebuild.** That test is meaningless if it is written after the fact.

**Milestone 4 has a specific proof obligation**: run a headless city of 100k idle Citizens and demonstrate that per-Tick cost is flat in population and linear only in the size of the current bucket. If idle Citizens cost anything measurable, the Event Wheel is wrong and everything sized against it is wrong too. The discipline that makes it work is that **entities do not poll; mutators wake observers** — every mutation site must know who cares. That is more code than polling, and it is the difference between a city that scales and one that does not.

**At the end of Phase 1 there is a city with no movement in it.** Buildings run Rules, Bins fill and empty, Zone Rules create and destroy Buildings on sampled Lots, Map Layers diffuse, and the whole thing fast-forwards thousands of Ticks per second in a terminal while emitting a hash trace you can diff against a previous run. Goods already work within a District, because a District Pool is just a Bin per Good and [`adr/0013`](adr/0013-goods-are-pooled-within-a-district-and-shipped-between.md) invents nothing new for the pooled case — physical Shipments arrive with the Road Graph in Phase 2. Nothing is drawn. This is the correct state to be in, and it is worth resisting the pull to render something early: the boundary in [`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md) is cheapest to hold when there is nothing on the other side of it yet.

---

## Phase 2 — the simulation proper

This is the longest phase and the one where the plan's build order is most stale. Three corrections, stated plainly:

- **There are no three LOD tiers and no cohort GC.** [`adr/0005`](adr/0005-two-fidelity-tiers.md) dropped the Cohort tier; [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) then moved Fidelity from person to **place** entirely. There is no per-Citizen promotion pool, no eviction policy, and no demoted population to collect. Milestone 7 below is what replaced all of it.
- **Walking is not deferred.** [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) makes walking a simulated Leg from the first line of code, and parking is modelled supply from [`adr/0009`](adr/0009-parking-is-modelled-supply-never-search.md). Both are inside the movement work, not after it.
- **Households and Life Stages are a real milestone.** [`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md) postdates the plan entirely, and there is **one clock and no calendar** ([`adr/0010`](adr/0010-one-clock-and-demographics-by-sorting.md)) — a Life Stage countdown is an ordinary event on the Event Wheel, denominated in Days, not a second time base.

| # | Milestone | Contents | Risk retired |
|---|---|---|---|
| **5a** | **Road Graph and Streets** ([`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md)) | Grid-snapped Streets, nodes and edges, Epoch counter, lazy route revalidation. The simulation never sees a spline | That geometry leaks into the simulation and the routing graph stops being uniform |
| **5b** | **Trips, Legs and the pedestrian layer** | `Trip` as an ordered sequence of Legs with a Trip Fate; sidewalk edges and crossings; pedestrian and vehicle Access Points; Commute Budget spanning modes | **The irreversible one.** A single-Leg Trip model propagates into Lot valuation, cost functions and every balance constant, and is what Citybound could never undo |
| **5c** | **Statistical resolution and the travel-time matrix** | Every Segment Statistical; travel time `distance / speed`; whichever router won S2; Trip Fates recorded, never swallowed | That routing intent leaks into the world ([`adr/0012`](adr/0012-routing-intent-lives-in-the-agent.md)) — the GlassBox failure |
| **6** | **Lane-as-entity traffic** | Lanes as sorted 1-D queues, IDM car-following, declared Overlaps, Switch Lanes. IDM parameters in the Ruleset | That the vehicle becomes the entity, which costs a spatial index, cache locality, and roughly an order of magnitude ([`adr/0016`](adr/0016-the-lane-is-the-entity-not-the-car.md)) |
| **7a** | **Stress-driven Fidelity with hysteresis** | `stress = volume/capacity × complexity_factor`; promote above `T_high`, demote below `T_low`; materialise and dissolve Lane queues at Segment boundaries; enumerate what demotion discards | That fidelity depends on the camera, which makes observation change outcomes and destroys replay |
| **7b** | **The rotating Audit** | A deterministic tick-keyed sample of unstressed Segments simulated microscopically anyway; divergence recorded against a fixed tolerance | The known blind spot — junctions that fail at *low* volume through turning conflicts, which V/C structurally cannot see |
| **8** | **Parking** | Parking Bins on Buildings and Segments; nearest-first Parking Shed query on arrival; the walk Leg lengthens rather than the Trip failing | That parking is abstracted into a District average, losing the specific diagnosis — and that occupancy leaks, which is an [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md)-class permanent capacity loss |
| **9a** | **Households, the Unplaced Pool and Departure** | Provider Lists; sampled candidate Lots, hard filters, logit choice; recorded refusal reasons; bounded Pool with unhoused and housed Departures counted separately | That growth is driven by a global demand scalar. The Pool with per-Household reasons *is* the demand signal, and it is a diagnosis rather than a bar chart |
| **9b** | **Life Stages and self-generation** | Young → Family → Mature Family → Childless / Empty Nest on a per-Household Day countdown; fertility and dissolution as drawn decisions; spawned Households entering the Pool on equal terms | That the city has no internal demographic engine, so every dwelling change comes from immigration and the housing market has no churn of its own |
| **10** | **Save/load** | Version header, migration chain, per-Chunk serialisation, asynchronous save off **a copy taken at save time** ([`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md)); checkpoints double as the crash artifact | **Unsaved state**, which the save→reload→compare-hash test is the only reliable way to find |

**The ordering of 5c, 6 and 7 is deliberate and is not the intuitive one.** Build the statistical regime everywhere first (5c), then build the microscopic regime (6), then build the machinery that switches between them (7). Each of those is independently runnable and independently testable, which is rule 2; and it means the transition machinery is written against two regimes that already work rather than being designed speculatively alongside them. It also means 5c alone produces a city whose commutes resolve and whose Trips can fail — a thing worth watching, months before any vehicle is drawn.

**Milestone 5b is the one to protect if the phase runs long.** It is also the one whose value is least visible while it is being built: a pedestrian edge layer alongside the street edges, crossings at junctions, and two Access Points per Building look like overhead for a game in which everybody drives. The payoff is **Severance** — a neighbourhood cut off on foot by an Arterial with no crossing — which emerges rather than being scripted, because Arterials simply carry no pedestrian edges except at authored Junction pieces. A city can be perfectly well connected for cars and broken for people, and the game can say so. Sidewalks essentially never stress, so under [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) walk Legs stay Statistical approximately always and cost a `distance / speed` lookup; the expense here is topology, not simulation.

**Milestone 7a carries a hard invariant** and it should be asserted in tests from the day it lands: where a Segment is Microscopic, observed travel times must match the volume-delay function's predictions within a fixed tolerance. **Exceeding it is a bug report about the VDF, not grounds for widening the tolerance.** Incremental widening is exactly how this design would slide into the failure mode it was built to prevent.

**Milestone 9b is where the population becomes a feedback loop**, and it needs a damper from the start rather than after the first runaway. The damper is physical rather than a tuning constant: fertility responds to available dwelling *space* as well as to price, and large dwellings consume land. Expect the balance work here to exceed the implementation work by a wide margin — [`adr/0011`](adr/0011-household-life-stages-and-self-generating-population.md) puts the compute cost at roughly a tenth of one percent of the decision volume already committed to, so the cost of this milestone is balance, not cycles.

**Milestone 8 has an invariant that must ship with it rather than after it.** Parking occupancy is conserved state, and a Traveller that vanishes without releasing its space destroys capacity permanently — an [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md)-class defect that manifests as a district slowly strangling itself for no visible reason. Assert every Tick that total occupied spaces equals total parked Vehicles. Note also that a Trip must remember where it parked in order to walk back to the car, which is small state that must survive Milestone 10.

**Milestone 10 lands last in this phase, not first**, because a save format written before the tables have settled is a migration chain written against nothing. But it must land *before* Phase 3: rendering work that cannot be resumed from a saved city is rendering work that requires rebuilding a city from scratch on every session, which violates rule 2 in the most expensive possible way. The test that makes it real is Factorio's — run N Ticks, save, reload, run M more; separately run N+M; compare hashes. That catches unsaved state, which is otherwise nearly impossible to find, and it is only cheap because Milestone 1 exists.

**At the end of Phase 2 the city is alive and still invisible.** Households form, look for housing, fail with recorded reasons, move in, commute on multi-Leg Trips, park, age through Life Stages, spawn Households that either find housing or become Departures, and generate congestion that promotes specific Segments into microscopic simulation. All of it is inspectable only through headless summaries and the hash trace. Phase 3 is where that stops being true.

---

## Phase 3 — making it visible

| # | Milestone | Contents | Risk retired |
|---|---|---|---|
| **11** | **First render** | Chunked MultiMesh Buildings, orbit camera, Travellers interpolated from `visible_agents(aabb, alpha)`. Fixed sim Tick, interpolated render | That the sim/render boundary ([`adr/0002`](adr/0002-simulation-is-an-engine-agnostic-library.md)) leaks under contact with an actual renderer, and that S1's numbers do not survive real data |
| **12** | **Evidence** | Drill from any aggregate to its named constituents: departures to Households, a congested Segment to the Trips using it, a shortage to the starved Buildings, a vacant Lot to *why* | The Simulator Effect critique; Pillar 4; and the absence of any viable debugger for emergent behaviour |
| **13** | **First map overlays** | Traffic volume, land value, pollution, service coverage — composed at point of use, drilling into Evidence rather than terminating in a colour | That the player can see *that* something is wrong but has no route from the colour to the cause |

**Milestone 11 carries two disciplines that are cheap now and expensive later.** The first is that **not every Citizen is rendered**, and that this is a design decision rather than a technical compromise: every visible agent is a promise you have to keep, and rendering an individual invites the player to judge its behaviour. Render Vehicles, which are sparse and visually meaningful, and a sampled subset of nearby pedestrians. If 200 pedestrians are visible in a district that contains 8,000, nobody will know. The second is **aggressive distance LOD and frustum culling from day one, not as a later optimisation pass** — Cities: Skylines 2 shipped 121 million input vertices per frame with no occlusion culling and fully-modelled teeth on character models. Its simulation was fine; its renderer was not, and that is the cautionary tale this project is most likely to repeat given how much attention the simulation gets.

**Milestone 12 is deliberately early, and it is worth being explicit about why, because it will look like a detour from the simulation.** It is three things at once.

First, it is **the answer to the strongest objection to this entire design**. Will Wright's *Simulator Effect*, as Don Hopkins put it: players imagine a simulation is vastly deeper than it is, and that magical misunderstanding is one you should not talk them out of. Andrew Willmott reached the same conclusion independently from the other direction — closing the visualisation gap removes the player's grace to rationalise. The objection is correct as stated, and the answer is not to argue that our simulation is deeper. **The payoff of microscopic simulation is not realism; it is explicability.** A statistical model can report that a neighbourhood is dying. A microscopic one can say *which* named Household's commute crossed the budget, and that is a benefit no amount of clever faking provides, because the causal chain either exists in the data or it does not.

Second, it is **Pillar 4 made real**. Every summary retaining a pointer to its constituents is a constraint on the simulation rather than a UI feature — [`02-simulation-model.md` §9](02-simulation-model.md) is explicit that if a figure cannot name its constituents, we are computing it in a way that discards what the player needs. Cheap if designed in, expensive if retrofitted.

Third, and most immediately useful: **it is the only viable debugger for emergent behaviour.** By Phase 3 the simulation produces outcomes nobody designed, which is the point, and there is no other tool for asking why one happened. Citybound never built this. The "why did that happen" question went permanently unanswered, and its author's own final devblog concedes he had been abandoning the simulation aspect for a while — in part because the parts he could see and iterate on were the ones that got worked on.

Concretely, Milestone 12 is done when the simulation can answer three questions on demand, and the third is the hard one:

- For a **Building** — its Occupants, its Bins with current levels, which Rule it last ran and whether it succeeded, which fallback chain it walked and where that chain terminated, and the specific conditions feeding its accumulated failure pressure.
- For a **Citizen** — home, workplace, current activity, current or last Trip with its Fate, Need satisfaction, and Household finances.
- For a **Lot** — *why* it is vacant. Not the fact of vacancy: no frontage, no Household in the Unplaced Pool that would accept it, conditions below tolerance, or no capital. **"Why is nothing building here?" is the question every city-builder player asks and no city builder answers**, and it is the sharpest single demonstration that the causal chain is real.

The implementation constraint is that accumulators keep entity references — or, preferably, a **bounded sample** of them. Five example Households out of 380 is what the UI shows anyway, and a bounded sample keeps the accumulator fixed-size, which [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md) requires.

Note what Evidence does **not** require: free-roam browsing of the population. Clicking any of a hundred thousand dots is diagnostically worthless, because you would be hunting for a problem rather than being handed one. **Pin** covers the real want hiding inside that idea — long-term attachment to somebody you were *introduced to*, with a fixed-size ring of recent Trips.

---

## Where the project stops being an engineering exercise

**Phase 3 is the transition, and getting there matters more than getting any individual earlier step perfect.**

Everything before Milestone 11 is a simulation that only its author can see, verified by hashes and headless assertions. That is the right way to build it and the tests are load-bearing forever, but a headless simulation is not a game and cannot be judged as one. The moment the city is visible and drillable is the moment the design starts producing feedback — patterns nobody designed, failures nobody predicted, and the first honest answer to whether the causal chain in [`00-vision.md`](00-vision.md) is actually interesting to follow.

The practical consequence for sequencing: when an earlier milestone starts consuming sessions on polish rather than on retiring its named risk, that is the signal to stop and move on. The risk field exists partly to make that judgement easy — once the risk is retired, the milestone is done, whatever else remains undone in it.

This is the failure mode with the best documented precedent in the reference material, and it is not a lack of skill. Citybound reached roughly 400,000 individually simulated cars on a single core, which is more headroom than this design needs — and then spent its remaining years on a bespoke actor runtime, an allocator, a compact-memory trait, a geometry kernel, two renderers, and a procedural-geometry library, none of which has a user today. The thesis survived; the yak-shave did not. Nothing in Phase 1 or Phase 2 is more interesting than the moment the city is visible and drillable, and the ordering above exists to keep that moment from receding.

The smell tests in [`00-vision.md`](00-vision.md) are the honest measure of whether the roadmap worked: every number on screen drills into something coherent, a declining Building names the condition it stopped meeting, changing a production ratio takes seconds, a bug report is an Input Log that reproduces exactly, and the city produces patterns nobody designed. Three of those five are testable at the end of Phase 1. The last two need Phase 3.

---

## What is deliberately not in this roadmap

Full entries, with retrofit costs and revisit triggers, in [`deferred.md`](deferred.md). The four that most obviously look missing:

**Public transit.** Not in scope, and possibly never. What matters is that **it is no longer an irreversible omission**, which is the single thing [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) bought: because walking is a real Leg, a mundane car commute is already multi-modal — `walk → drive → walk`, with mode transitions, transfer points, and a cost function trading walking minutes against driving minutes — with zero transit code written. Adding a bus later is inserting a Leg type into machinery that already handles Legs. That is incremental. The jump from one Leg to many is not, and it is precisely the jump that never happened in Citybound.

**Turning-movement diagnosis and traffic-management tools.** Parked as a package, and the packaging is the point: building the diagnosis without the tools shows the player a problem they cannot act on, which is worse than not showing it. **Diagnosis should be exactly as fine-grained as the player's ability to act, and no finer.** The anti-goal in [`00-vision.md`](00-vision.md) — not a traffic-management game — settles it. Retrofit cost is medium-high and the accumulator shape is the reason: turning movements are per-(inbound, outbound) pair rather than per-Segment, which changes the volume-delay function, the travel-time derivation, the overlay rendering, the cached routes, and all existing balance tuning. This is the one deferral worth periodically re-examining, because it also resurfaces as the correctness blind spot in Milestone 7b.

**Illegal parking as an overflow channel.** Graceful shed-widening ships instead. Retrofit cost is low — one more tier consulted after the legal ones in a lookup that already returns "no capacity found" — and the trigger is playtesting showing the gradient is *too* gradual to notice.

**Free-roam citizen following.** Evidence and click-what's-rendered ship instead. Worth noting that [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) quietly collapsed this deferral's cost from "a mechanic with a budget interaction to design" to "camera work", because Fidelity now belongs to Segments and every Traveller therefore has a renderable position at all times. Nobody prompted that re-check, and the general lesson is worth carrying: **a deferral's retrofit cost is not fixed, and decisions taken for unrelated reasons can change it silently.**
