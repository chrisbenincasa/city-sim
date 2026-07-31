# 03 — Agents and Movement

> Vocabulary in [`CONTEXT.md`](../CONTEXT.md). Pillars in [`00-vision.md`](00-vision.md). The world model and growth mechanics in [`02-simulation-model.md`](02-simulation-model.md).
>
> Governing decisions: [`adr/0005`](adr/0005-two-fidelity-tiers.md) (two tiers, individual decisions), [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md) (bounded collections), [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) (stress-driven detail).

---

## 1. What has agents, and what doesn't

The first thing to establish, because three earlier drafts of this design got it wrong: **most of the city is not agent-based, and that is not a compromise.**

| Subsystem | Representation | Agents? |
|---|---|---|
| Air, water, noise pollution | Scalar field with diffusion | **No** — a field *is* how pollution works |
| Power, water, sewage | Network flow over a graph | **No** |
| Waste | Production → flow → treatment | Collection vehicles only |
| Goods | Pooled within a District, shipped between | Inter-District freight only |
| Citizens' lives and choices | Individual records, individual decisions | **Yes — this is the pillar** |
| Commute and freight movement | See §3 | The only genuine fidelity question |

A diffusion field is not a cheap approximation of a swarm of pollution particles; it is the correct model, and simulating particles would be *less* accurate as well as vastly more expensive. The same holds for electrical networks.

**Agents exist for two reasons only: to make decisions, and to move.** Everywhere else, fields and graphs are both cheaper and better.

This matters because "agent-based simulation" is easy to treat as a global architectural stance, and then to apply the fidelity ladder to systems that never needed one. **The ladder governs vehicular movement. Nothing else** — not pollution, not utilities, and not walking, which has one fidelity permanently for the reason given in §3.7.

---

## 2. The Citizen model

### 2.1 Identity is permanent and never fudged

Every Citizen in the population count is a real record with a real home and a real job. **The population number is never a multiplier, an estimate, or a scale factor.** `UNIQUE INDIVIDUALS`

This is a direct response to SimCity 2013, which displayed a population that was a multiplier over simulated agents past a threshold. That is worse than its famous pathfinding problems, because it means the number the player is optimising isn't real. Any future proposal that would require scaling a displayed count should be treated as a proposal to abandon this design.

A Citizen record is small — on the order of 40 bytes hot plus cold fields — so a million Citizens is roughly 40 MB. Identity is cheap. What is expensive is *movement*, and that is what §3 is about.

**The million is the spec, not an illustration.** It was long unclear whether this figure was headroom or a stale ambition, while a 10k figure drifting in from elsewhere quietly sized decisions against a target 100× smaller. Settled: **1M Citizens on a 4096² map is the late-game floor**, because the design's endgame is sprawling polycentric cities with interdependent Settlements, and nothing smaller exercises the machinery `adr/0020` built for it. 10k is the *first hour*, not the ceiling. See [`05` — the budget](05-technical-architecture.md).

This sizing is stale in one respect and should be recomputed rather than trusted: session five added a schooling accumulator, experience, and car ownership to the record, and none are reflected in the 40 bytes.

### 2.2 Decisions are always individual

Every Household and Business evaluates its own choices and draws its own outcome. Decisions are never shared across a group, even between Citizens with identical attributes. See [`adr/0005`](adr/0005-two-fidelity-tiers.md).

The argument is behavioural, not performance-based: the entire premise of the choice model in `02-simulation-model.md` §5.4 is that identical Households choose *differently*, because the random component stands in for preferences we chose not to simulate. Sharing a decision asserts the opposite and reintroduces herd behaviour through the back door.

When decision cost needs reducing, the levers are **sampling fewer candidates** or **deciding less often**. Never deciding collectively.

### 2.3 Bounded, sticky, satisficing

Households hold a short **Provider List** — the shops, workplaces, and services they already know about — and switch only when a *known* alternative is *substantially* better. `BOUNDED KNOWLEDGE`

This is cheaper than optimising and also more realistic, and it prevents the synchronised stampede that optimal agents produce. Citybound arrived here the expensive way: its first design had households evaluating hypothetical activity sequences, which turned out to be an NP-hard orienteering problem requiring seconds of CPU per agent. The retreat to satisficing was a design improvement, not just a performance fix.

### 2.4 Sinks

Citizens must be removed at a rate comparable to the rate they are added, or cost grows with elapsed time — see [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md).

The housed population is bounded physically: Citizens live in dwellings, dwellings sit on Lots, Lots come from zoned land, and the map is finite. The dangerous collection is the **Unplaced Pool**, which has no physical ceiling. Households give up after a bounded number of failed cycles and depart permanently. See `02-simulation-model.md` §5.2.

---

## 3. Movement fidelity

### 3.1 Fidelity is a property of place, not person

This is the central decision, and it inverts what three earlier drafts assumed.

> **A road segment is Microscopic when it is under stress, and Statistical when it is not. A Traveller is microscopically simulated while on a Microscopic segment, and time-advanced while on a Statistical one.**

There is no per-Citizen promotion pool, no eviction policy, and no per-Citizen budget. There is only which segments are stressed.

| Segment state | How Travellers on it are handled | Travel time from |
|---|---|---|
| **Statistical** | Time-advanced; position interpolable for rendering | `distance / speed` — free-flow, exact |
| **Microscopic** | Real vehicles: 1-D lane queues, car-following, junction conflicts | Emergent from simulation |

### 3.2 Why stress is the trigger

Four properties, in order of importance:

**It puts the volume-delay function where it is strong.** The VDF is only used on unstressed segments, where traffic is free-flowing and travel time is `distance / speed` — not an approximation but the exact answer. The saturated regime, where VDFs are known to diverge from reality because they cannot represent queueing, spillback, or hysteresis, is handled by actual simulation.

**The visualisation gap closes by itself.** A gap between what is shown and what is simulated can only exist where behaviour is complex, and behaviour is only complex where there is congestion — which is precisely what gets simulated. On an empty street, a car travelling at the speed limit is trivially correct. **Detail arrives exactly where scrutiny does, without the camera ever being consulted.**

**Observation cannot change outcomes.** Stress is simulation state. Nothing about the trigger depends on where the player is looking, so determinism holds and there is no exploitable fidelity boundary.

**It scales.** Only so many segments can be stressed at once. Microscopic work is bounded by network stress rather than by population, which is what makes a 1M-Citizen city feasible with honest counts.

### 3.3 The trigger

```
stress(segment) = volume / capacity  ×  complexity_factor(junction)

promote to Microscopic when stress > T_high
demote to Statistical when stress < T_low        (T_low < T_high — hysteresis)
```

**Volume is a count, not a model.** `in_flight[origin_District][dest_District]` is incremented on departure and decremented on arrival, then distributed onto segments along cached District-pair routes each congestion cycle. Capacity is a static property of the segment. **We are not using the VDF to decide where the VDF can be trusted.**

*(An earlier draft wrote this as `origin_zone`/`dest_zone`, borrowing the traffic-planning term. **Zone** in this project is a permission set over land and has nothing to do with movement; the **District** is *"the granularity of the travel-time matrix"* per `CONTEXT.md`, so the matrix and this counter are the same partition — which is the point.)*

**Hysteresis is mandatory.** Without `T_low < T_high`, segments flicker between regimes as volume oscillates around a single threshold, thrashing the promotion machinery every cycle.

**The two thresholds are measured, not chosen.** `T_high` and `T_low` are the only free constants in the scheme, and guessing them is avoidable: run one **Input Log** at a sweep of threshold values and compare the resulting cities. Where the promotion boundary sits stops mattering to the outcome is where it belongs. This is the standing *prefer a crossover to a threshold* preference applied to the two numbers that most need it, and it costs nothing — determinism and headless replay already exist for other reasons.

**There is a second trigger, and it is event-driven.** Stress decides where microscopic simulation *begins*; **downstream blocking** decides where it *extends*.

```
force-promote(segment) when its downstream neighbour is Microscopic and full
```

An earlier draft assumed contiguity was handled naturally, on the grounds that spillback raises the neighbour's volume and the stress trigger picks it up. It does not, for two reasons. **Timing:** volume is distributed onto segments each congestion cycle, but a jam propagates backward at roughly 15 km/h — faster than any cycle worth running — so a cycle-driven region always lags the jam during exactly the event it exists to capture. **And the boundary had no defined behaviour:** a Statistical segment computes arrival as `distance / speed` and is structurally blind to the downstream being full, so it delivers vehicles into a segment with no room. Admitting them anyway would delete spillback, which §5.1 makes an acceptance criterion.

Force-promotion is therefore immediate rather than cyclical, which is what lets the Microscopic region keep pace with a queue rather than trail it. When the **Microscopic Cap** (§3.9) leaves no slot, the fallback is a virtual queue held at the boundary — vehicles are refused entry rather than admitted into a full segment. Refusing is a smaller lie than over-filling.

**The complexity factor** lowers the effective threshold for junctions with many conflicting movements, so a complex junction enters microscopic simulation at lower volume than a simple through-road. It is derived from static geometry — number of approaches, number of conflicting turn paths — computed once and free per tick. This is a partial mitigation for §3.6.

### 3.4 The circularity, and why it is survivable

Volume attribution depends on which route a zone-pair takes; route choice depends on travel time; travel time comes from the VDF. So the trigger is not fully independent of the model it is protecting us from.

It holds because **the error is self-correcting in the dangerous direction**:

- VDF **underestimates** congestion → routing over-uses the segment → volume rises → threshold crossed → **microscopic simulation finds the truth.** The failure feeds the detector.
- VDF **overestimates** congestion → routing avoids the segment → volume stays low → it remains Statistical. But it is genuinely uncongested, so nothing is mis-modelled there. The diverted traffic raises volume elsewhere, triggering detection there.

Both errors push toward detection rather than away from it. **This is the load-bearing assumption of the whole scheme** and should be stated in any future revision, because a change that breaks it breaks everything downstream.

### 3.5 The rotating audit

The trigger can only detect problems it anticipates. The audit covers the rest.

> **Each cycle, microscopically simulate a small rotating sample of unstressed segments, selected deterministically by tick. Where an audited segment's observed behaviour diverges from what the statistical model predicted, promote it and record the divergence.**

Three things this buys:

1. **It catches failures the trigger structurally cannot** — principally §3.6.
2. **It is a continuous running validation of the statistical layer.** Divergence is a measurable, always-on quality signal rather than something discovered by a player complaint.
3. **It is deterministic and fixed-cost.** Rotation is a function of tick; the sample size is a constant.

The audit is *not* a replacement for the trigger. Its coverage rate is low by design, so a problem may persist for some time before its segment comes up. That is acceptable where the symptom is visible — a player who notices something wrong will look, and looking is how they would have found it anyway.

**The divergence metric is travel time, and the audit is the only place it is meaningful.** Observed traversal time on an audited segment against what the statistical model predicted for it. This works precisely *because* the population is unstressed: free-flow travel time is `distance / speed`, which §3.1 calls not an approximation but the exact answer, so any material gap is a real fault rather than a modelling limit. The same comparison on a *stressed* segment measures nothing, since disagreement there is what the tier is for — see invariant 6.

It also gives §3.6 its detector. A junction failing at 40% of capacity is invisible to `v/c` by construction, but it is loud on this metric: the statistical model predicts free-flow and the simulation finds a queue.

> **Open:** the audit rate, and what happens on repeated divergence (permanent promotion? a flag for us?). The metric is settled.

### 3.6 Known blind spot

**Junctions that fail at low volume because of turning conflicts.** V/C reports them as healthy at 40% of capacity, so they stay Statistical, so the failure is never simulated — and the player watches vehicles flow serenely through a junction that should be gridlocked.

This is the deferred turning-movement work in [`deferred.md`](deferred.md) resurfacing as a *correctness* problem rather than a UI one, and the coupling is tighter than that document suggests. Mitigated by the complexity factor (§3.3) and the audit (§3.5); **not eliminated.**

### 3.7 Walking is always Statistical, and there is one graph

Half the Legs in this city are walk Legs — `adr/0008` guarantees a car commute is never fewer than three — so the fidelity ladder has to say what happens to them. It says: nothing. **Walking has one fidelity, permanently.**

The reason is the same one that justifies the ladder in the first place, applied honestly. The Microscopic tier exists because the VDF fails under saturation; **pedestrian networks do not saturate at this scale.** Walking speed is independent of density until crowding levels real cities reach only at stadium exits. So `distance / speed` is not an approximation for a walk Leg, it is the exact answer, and there is nothing a second tier could discover.

Four consequences:

- **§1's claim narrows.** The ladder governs *vehicular* movement. Nothing else.
- **Pedestrians never contribute to Stress.** Stress is `volume / capacity` over vehicles. A crowded pavement must never promote a Segment.
- **Drawing walkers is honest, not a dodge.** §3.8's argument for interpolating cars on free-flowing roads — trivially correct — holds for walkers permanently. Local avoidance so they do not visibly overlap is a renderer concern and must not touch arrival time.
- **Parking's gradient is exact.** [`adr/0009`](adr/0009-parking-is-modelled-supply-never-search.md) promises scarcity arrives as the walk Leg growing. Were that modelled rather than exact, `01-player §7`'s rule that *an overlay must never be sharper than the simulation underneath it* would bite. It does not.

> **The trigger for revisiting this: transit.** A stop is a queue with a capacity, and a platform is the one pedestrian context where the no-saturation argument genuinely fails. If transit is ever built, this decision is reopened — recorded here so it is a trigger rather than a rediscovery.

**Pedestrian and vehicle edges live in one graph with mode masks**, not two graphs. `CONTEXT.md` already implies it: **Severance** works because *"Arterials carry no pedestrian edges except at authored junction pieces"* — which describes an edge property, not a missing graph. One graph means one **Epoch** covering both networks, one revalidation path, and a multi-Leg Trip routed by a single mode-aware search rather than stitched across two structures. It is also what makes Severance emergent rather than scripted: nobody removes a pedestrian route, the mask simply never granted one.

### 3.8 Rendering is independent

Simulation fidelity is stress-driven and deterministic. **Rendering is camera-driven and has no effect on simulation state whatsoever.**

- A Traveller on a Statistical segment is *animated* from its trip — position interpolated along its route. On free-flowing roads this is trivially correct.
- A Traveller on a Microscopic segment off-screen is simulated but not drawn.
- Nothing the renderer does can influence the simulated set.

This separation is what makes the camera safe. Any future feature that would let rendering influence simulation reopens the observation problem and should be treated as an architectural change, not a tweak.

### 3.9 The Microscopic Cap is a world constant, and it is not a failure mode

A ceiling on how many segments may be Microscopic at once still exists. It is called the **Microscopic Cap** — renamed from *Fidelity Budget*, because the corpus already has a **Commute Budget** that the player reads and acts on, and sharing the word let an internal ceiling borrow a gameplay concept's authority. That conflation caused the second half of this section.

**It is set from world configuration, identical on every machine, and not player-adjustable** — the same category as `TICKS_PER_DAY` under [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md), and derived from Segment count or map size rather than being a bare number, since a ceiling sensible for 512² is not sensible for 1024².

The argument is the one `CONTEXT.md` already makes about Speed: *"purely a host concern — the simulation cannot observe it, so **no speed setting can change any outcome**."* A budget fails that test. It decides which segments get simulated, simulated segments produce different travel times, and different travel times produce different route choices, different Commute Budget failures, and a different city. A host-tunable budget would mean **the same Input Log producing two different cities on two different machines** — which costs portable replay, headless regression testing, crash forensics, and the two experiments in §3.3 and §5.1 that work by changing one variable at a time.

This also separates two degradations the design was conflating:

| | Governed by | Response |
|---|---|---|
| The **simulation** hits its limit | `HONEST DEGRADATION` — *nothing fails silently* | more of the traffic overlay reads *modelled* rather than *exact* |
| The **hardware** hits its limit | Speed — a host concern the simulation cannot observe | fewer Ticks per second, plus a *simulation running behind* indicator |

The pillar only ever made the first demand. Routing hardware limits into the second box costs a weak machine smoothness in an already-failed city; routing them into the first box costs the design its reproducibility and makes a player-facing figure depend on their GPU.

**Reaching the Cap is not a failure of the city.** What happens is that the most congested segments get their travel times from the VDF, which §3.2 establishes is structurally wrong there — so **the simulation becomes less accurate exactly where accuracy mattered most.** That is a fact about the simulator, not about the city.

An earlier draft made it `01-player §6`'s indicator for the **Gridlock** trajectory. That was wrong twice over. A failure mode triggered by a number in a config file fails the project's own rule that *an authored constant is acceptable only when it is the same thing the player is shown*; and it let a resource limit borrow a gameplay diagnosis's authority, which is a category error a table of city trajectories cannot afford. Gridlock now reads off the commute-time distribution approaching the Commute Budget wedge — a real city state that works whether or not the Cap is ever approached.

**`HONEST DEGRADATION` is satisfied without an event.** `01-player §7` already requires that an overlay never be sharper than the simulation beneath it, and that a modelled number be marked as modelled. Reaching the Cap simply means more of the traffic overlay is marked modelled. Nothing is announced, and nothing new was needed.

**Three measurements would reopen the world-constant decision**, and they are unknowable before there is a build:

1. **What the Cap realistically is.** If it lands high enough that ordinary play never approaches it, the whole trade is free.
2. **How often it is reached.** The argument above rests on it happening only in cities already congested past the ceiling. If it is common in healthy play, the exposure is not narrow and the trade is worse than priced.
3. **Whether exact replay is genuinely used.** If bug reports arrive as video and regression testing ends up synthetic, the benefit bought here is theoretical and the cost to low-end hardware is not.

> **Open, and needing real investigation:** what the Cap actually is in a built system, and more generally **how gameplay mechanics stay decoupled from simulation resource constraints.** Gridlock was one instance of the coupling and has been severed; there is no guarantee it was the only one, and the general rule — *a trajectory names something happening to the city; if an indicator would change when the simulation is optimised, it is not one* — needs auditing against every figure the game displays, not just this table.

---

## 4. Invariants

Enforced as assertions in debug builds. These are the properties that make fidelity transitions safe.

1. **Conserved quantities live on the Citizen record, never on the embodiment.** Money, goods, health, and employment are fields on the persistent row; the moving entity is a view onto it, not the owner.
2. **Promotion is reconstructible.** Given a Citizen and a tick, its position must be computable. Because no record is ever collapsed or discarded ([`adr/0005`](adr/0005-two-fidelity-tiers.md)), this holds *by construction* rather than by discipline.
3. **Demotion is lossy only in enumerated ways.** Write down what is discarded when a Traveller leaves a Microscopic segment. Anything not enumerated is a bug.

   > **A segment with a non-empty queue does not demote, regardless of stress.** The queue is precisely the thing demotion would discard: forty queued vehicles become arrival times computed by a VDF that believes the road is free-flowing, so the jam evaporates — and **hysteresis**, the third entry in §5.1's acceptance suite, is deleted at the moment it would have been observed. This is a state-based guard sitting alongside the `T_low` threshold, and it is the direct mechanical expression of hysteresis, which the design otherwise implements only as a gap between two numbers.
4. **Fidelity never depends on render state.** The simulated set is a function of stress and tick. Nothing else.
5. **No collection grows with elapsed time.** [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md).
6. **The statistical model stays accurate where it is trusted.** On **audited unstressed** segments (§3.5), observed travel time must match the statistical prediction within tolerance. **Exceeding it is a bug report about the statistical model, not grounds for widening the tolerance** — incremental widening is exactly how this design would slide into the failure mode it exists to prevent.

   > **Scope is the whole invariant.** An earlier draft bound this to *Microscopic* segments, which inverted it: those are the saturated ones, and §3.2's entire justification for simulating them is that the VDF **is wrong there**. Requiring agreement would have made the tier's success condition an assertion failure, and a Microscopic segment whose travel time matched its VDF prediction would be an expensive way to recompute a number already available for free. Divergence on a stressed segment is the product. Divergence on an unstressed one is the defect.

---

## 5. Traffic model on Microscopic segments

Lineage: Citybound, whose traffic design was its strongest engineering work.

**The Lane is the entity; the vehicle is not.** A Lane holds its vehicles as a **sorted 1-D queue** and updates all of them in one pass. Vehicles do not hold references to each other and are not independently scheduled. This gives O(n) car-following with perfect cache locality and no spatial index.

**Two-dimensional behaviour emerges from one-dimensional queues via Overlaps.** Lanes that physically interact — parallel, opposing, crossing — declare an Overlap and exchange their vehicles' projected positions each tick as obstacles mapped into each other's coordinate space.

**Lane changing is a first-class object, not a special case.** A **Switch Lane** spans the Overlap between two parallel Lanes; the parallel Lanes connect to it rather than to each other. Merging — normally the nastiest special case in traffic simulation — becomes an ordinary object obeying ordinary rules, including merges the driver aborts when required braking exceeds comfort.

**Car following** uses the Intelligent Driver Model (Treiber, Hennecke & Helbing 2000). See [`references.md` §3](references.md).

### 5.1 What counts as correct

The tier has to be falsifiable, or "the traffic model works" is an opinion. Its acceptance criterion is derived from its own justification rather than invented: **the VDF is wrong under saturation for three specific structural reasons, so the test is whether the tier produces those three things.**

| VDF blind spot | Scenario | Pass condition |
|---|---|---|
| **Queueing** | a junction held at capacity | a queue forms, grows, and discharges — rather than travel time rising smoothly along a curve |
| **Spillback** | a queue that fills its Segment | the **upstream** Segment blocks. The VDF cannot express this at all, since it never reads a neighbour |
| **Hysteresis** | volume spikes, then falls away | the jam **persists after** the volume that caused it is gone |

Three small deterministic scenarios, headless and fast, each failing loudly under an all-statistical run. An implementation passing all three is doing the job [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) hired it for; one failing any of them is an expensive VDF.

**The suite is founding, not exhaustive**, and it has an admission rule so it does not become a junk drawer:

> **A scenario earns a place when it names a phenomenon the statistical tier structurally cannot produce.**

A proposed test that would also pass all-statistical is testing something else. Likely next entrants: **capacity drop** (discharge rate after breakdown falling below free-flow capacity), **stop-and-go waves**, and **merge failure** — which is where Switch Lanes and the MOBIL gap below would surface.

**The audit is the discovery route.** §3.5 already describes it as continuous running validation whose divergences are an always-on quality signal. A divergence found in a live city, reduced to a minimal deterministic scenario, *is* a new suite entry. That closes a loop previously open at both ends: the audit had no consumer for what it found, and the suite had no source of new cases.

> **Note:** Citybound's lane changing is weaker than its reputation — it is triggered purely by proximity to the end of a switchable stretch, with no incentive or politeness criterion, so there is **no discretionary lane changing at all**. If we want lanes chosen for reasons other than the next turn, MOBIL is the missing half.

---

## 6. Open questions

1. **Audit rate and response to repeated divergence.** §3.5. ~~Divergence metric~~ **settled** — travel time against the statistical prediction, on audited unstressed segments only. Rate and escalation policy still open.
2. ~~**Do Microscopic segments form contiguous regions?**~~ **Closed in §3.3 — yes, by a second, event-driven trigger.** The assumption that spillback would handle it naturally was wrong on timing and left the tier boundary undefined. Force-promotion on downstream blocking, a virtual queue at the boundary when the Cap is reached, and a demotion guard on non-empty queues (§4, invariant 3). **Revisit once there is a build:** the interaction between force-promotion and the Cap is the untested part — a single arterial jam could in principle cascade promotions along its whole length and consume the Cap by itself.
3. ~~**What is the microscopic budget, and is it player-facing?**~~ **Closed in §3.9.** Renamed the **Microscopic Cap**; a world constant, not player-facing, and **not a failure mode** — it was removed from `01-player §6` entirely. `HONEST DEGRADATION` demanded it be *visible*, never *adjustable*, and §7's existing overlay-honesty rule already provides the visibility. Still genuinely open: the **number**, and the broader question of keeping gameplay mechanics decoupled from simulation resource constraints.
4. **Transit.** ~~Multi-modal trips.~~ Settled in part: walking is a simulated Leg, so the Trip model is genuinely multi-modal rather than nominally so — see [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md). Whether *transit* is ever implemented remains open and is owned by `01-player-experience.md`, but it is no longer an irreversible decision: a bus is a Leg type inserted into machinery that already handles Legs.
5. **Car ownership.** Settled: parking is modelled supply with no search — see [`adr/0009`](adr/0009-parking-is-modelled-supply-never-search.md). What remains open is what *generates* parking demand. Every Household owning a car is the simple assumption; making ownership a choice influenced by walkability and transit access would close the loop, letting parking pressure feed back into whether people drive at all. Owned by `01-player-experience.md` alongside transit, since the two only become interesting together.
6. **Freight.** Inter-District Shipments use the same movement machinery, but whether freight vehicles contribute to segment stress identically to commuters, or are weighted, is undecided.
