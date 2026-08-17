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

**Volume is a count, not a model.** A Segment's volume is incremented when a vehicular Traveller **enters** it and decremented when it **leaves**, so a Traveller contributes congestion to exactly the Segments it experiences congestion on. There is no `in_flight[origin_District][dest_District]` counter and no periodic distribution of counts along cached District-pair routes — see [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md). Capacity is a static property of the segment. **We are not using the VDF to decide where the VDF can be trusted.**

> ✅ **BUILT 2026-08-14, milestone 5c task 6 ([`adr/0099`](adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md)).** A vehicular Leg stores its route, a Traveller holds a cursor onto it, and each Segment's dwell is priced by the VDF on entry from that Segment's volume at that instant.
>
> ⚠ **The `volume / capacity` above is a ratio between a *stock* and a *flow*, and the conversion is not free.** `volume` counts Vehicles **present**; `RoadSegmentTable.CapacityPerDay` counts Vehicles **passing**. Little's Law relates them exactly — a Segment at capacity holds `capacity per Tick × its free-flow crossing time` Vehicles, which at a Street is **9.2** on a 128 m block, a 14 m spacing. **The first cut of the build divided by the per-Tick flow alone**, on the strength of `adr/0041`'s *a vehicle crosses about one Segment per Tick* — a sentence that followed from `TICKS_PER_DAY = 8192` and that `adr/0094` retired without touching it, since the rate is now **~4.6**. The function came back inert at every population and only a measurement said so.
>
> ⚠ **And this section's ratio is not yet reachable on a city this project generates.** Peak load is `v/c` **0.44** at 4,000, 16,000 and 64,000 Citizens alike, because the paved extent scales with the square root of the population — ***the same number sizes both the demand and the supply***. So §3.3's threshold, §3.4's self-correcting chains and §3.9's Cap are all defined over a quantity that a **player** moves by laying too little road ([`adr/0090`](adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)) and that `CommandKind.Populate` cannot reach.

> **Superseded by [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md), then measured by S2 R2.** This paragraph read: *"`in_flight[origin_District][dest_District]` is incremented on departure and decremented on arrival, then distributed onto segments along cached District-pair routes each congestion cycle."* It carried a note that is kept because it is how the partition was pinned down: *"An earlier draft wrote this as `origin_zone`/`dest_zone`, borrowing the traffic-planning term. **Zone** in this project is a permission set over land and has nothing to do with movement; the **District** is 'the granularity of the travel-time matrix' per `CONTEXT.md`, so the matrix and this counter are the same partition — which is the point."*
>
> The ADR rejected the scheme on three correctness grounds — §3.4's self-correction, the lag below, and a player-adjustable District being able to move the State Hash — and it was rejected *knowing it was the cheaper option*. S2 R2 then measured it and the finding is worse than the price: **the aggregate scheme does not report a jam late, it does not report it at all.** The same surge reads **130.21%** `v/c` on the watched arc under direct attribution and **28.09%** under aggregate; under a next-hop path source the smear deposits **0.00%** on a Segment direct reports at **108.51%**. The two schemes agree closely on *how many* Segments are stressed — 2,592 against 2,714 over an 80% threshold, both an **upper bound**, since R2's uniform origin-destination draw is the longest-trip distribution available — and disagree completely on *which*, which is the shape most likely to pass an aggregate sanity check while being wrong about every individual road.
>
> **And it was not even the cheaper option at plausible cadences.** The crossover sits at **105 Ticks** of congestion cycle, an order of magnitude past the ADR's estimated ~10, with the measured crossing rate **0.79–0.83** per vehicle per Tick rather than the assumed 1.0. Direct attribution is cheaper for any cycle shorter than that.

**Hysteresis is mandatory.** Without `T_low < T_high`, segments flicker between regimes as volume oscillates around a single threshold, thrashing the promotion machinery every cycle.

**The two thresholds are measured, not chosen.** `T_high` and `T_low` are the only free constants in the scheme, and guessing them is avoidable: run one **Input Log** at a sweep of threshold values and compare the resulting cities. Where the promotion boundary sits stops mattering to the outcome is where it belongs. This is the standing *prefer a crossover to a threshold* preference applied to the two numbers that most need it, and it costs nothing — determinism and headless replay already exist for other reasons.

**There is a second trigger, and it is event-driven.** Stress decides where microscopic simulation *begins*; **downstream blocking** decides where it *extends*.

```
force-promote(segment) when its downstream neighbour is Microscopic and full
```

An earlier draft assumed contiguity was handled naturally, on the grounds that spillback raises the neighbour's volume and the stress trigger picks it up. It does not, for two reasons. **Timing:** volume is distributed onto segments each congestion cycle, but a jam propagates backward at roughly 15 km/h — faster than any cycle worth running — so a cycle-driven region always lags the jam during exactly the event it exists to capture. **And the boundary had no defined behaviour:** a Statistical segment computes arrival as `distance / speed` and is structurally blind to the downstream being full, so it delivers vehicles into a segment with no room. Admitting them anyway would delete spillback, which §5.1 makes an acceptance criterion.

> **The first of those two reasons is withdrawn, and whether the mechanism survives on the second is open.** *Timing* is an argument about a cycle-driven smear, and under [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) there is no smear and no cycle: volume is exact every Tick, so the lag has nowhere left to live. S2 R2b then measured the defect the argument described and found it was **never a lag at all** — direct lag is zero at every rung, and aggregate's reads *never* even at a **one-Tick** cycle, where no cadence remains to blame. The aggregate scheme reports the jam in the **wrong place**, and no cadence and no second trigger fixes a place. So force-promotion loses the justification it was bundled with and **stands on the second reason alone**: a Statistical segment computing arrival as `distance / speed` is structurally blind to a full downstream neighbour, and admitting vehicles into one anyway would delete spillback, which §5.1 makes an acceptance criterion.
>
> **That is a smaller claim than the one it replaced, and this section does not settle whether it is large enough.** `adr/0041` requires force-promotion to *stand on its own second argument or go*, and the question is open in [`plans/0002`](../plans/0002-open-questions.md) rather than answerable here. **Session D retyped it and the type is the useful part: it is *measurable*, not a decision** — run `§5.1`'s **spillback** scenario with force-promotion disabled and see whether the upstream Segment blocks. Under [`adr/0043`](adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) **no session may close it**, and it had been sitting on the argument track waiting for a sitting it did not need. The paragraphs below describe the mechanism as it currently stands and assume no answer either way.

Force-promotion is therefore immediate rather than cyclical, which is what lets the Microscopic region keep pace with a queue rather than trail it. When the **Microscopic Cap** (§3.9) leaves no slot, the fallback is a virtual queue held at the boundary — vehicles are refused entry rather than admitted into a full segment. Refusing is a smaller lie than over-filling.

**The complexity factor** lowers the effective threshold for junctions with many conflicting movements, so a complex junction enters microscopic simulation at lower volume than a simple through-road. It is derived from static geometry — number of approaches, number of conflicting turn paths — computed once and free per tick. This is a partial mitigation for §3.6.

> ✅ **WHICH CONSUMERS READ IT — settled 2026-08-16 by session E** ([`adr/0109`](adr/0109-stress-and-sight-share-a-volume-and-not-an-expression-and-the-static-term-belongs-to-habit.md)). **Stress reads it. Sight does not. Habit's cost basis should and does not.** This paragraph was written as a promotion term and read as though promotion were its only consumer — ***a term stated in one expression is not thereby a term nobody else needed.***
>
> ⚠ **[`adr/0046`](adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md) requires Stress and Sight to read *the same quantity*, and that requirement is satisfied at the level of *volume* and nowhere above it** — [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)'s single per-Segment count discharged it, for another reason, before it was asked. **Identity above volume is unsatisfiable**, because on a Microscopic Segment travel time is *emergent* ([`adr/0016`](adr/0016-the-lane-is-the-entity-not-the-car.md)): ***the cost of a Segment and the load on a Segment stop being the same kind of thing at the promotion boundary***, which is where every jam is. **Stress is a load; Sight is a cost.**
>
> ⚠ **The divergence is real and runs the *opposite* way from the one `adr/0046` feared.** The factor lowers the effective threshold, so it is **≥ 1** and `stress ≥ v/c` always — a complex junction **promotes at a `v/c` at which a driver sees no reason to divert**, which is *promote without divert*. The feared *divert without promote* **cannot arise from this term**, and that is arithmetic rather than a judgement. It is also **not** `01 §7`'s contradiction: in this direction the Segment is Microscopic, so congestion is exact and the overlay is honest, and drivers driving into a visible jam is `BOUNDED KNOWLEDGE` working — 5c task 8's ***congestion is a cost paid and never a cost avoided***.
>
> ⚠ **Sight is refused the factor on `adr/0046`'s layer decomposition, not on cost**: it is *static geometry, computed once*, so it contributes the same amount at every crossing and is a fact about the **network**, which is Habit's layer rather than the live one. ⚠ **And the finding that outranks the question is that Habit's basis does not carry it either** — so every Habit Route is computed as though every junction is a simple through-road, which is §3.6's under-pricing applied permanently and to every Trip, in the one layer that could route around it. **It belongs there**; how much it buys is measurable and routed. **This is not turning movements coming off [`deferred.md`](deferred.md)** — that document defers an accumulator, an overlay and a package of player tools, and this factor is already computed here.

### 3.4 The circularity, and why it is survivable

Volume attribution depends on which route a Traveller takes; route choice depends on travel time; travel time comes from the VDF. So the trigger is not fully independent of the model it is protecting us from.

It holds because **the error is self-correcting in the dangerous direction**:

- VDF **underestimates** congestion → routing over-uses the segment → volume rises → threshold crossed → **microscopic simulation finds the truth.** The failure feeds the detector.
- VDF **overestimates** congestion → routing avoids the segment → volume stays low → it remains Statistical. But it is genuinely uncongested, so nothing is mis-modelled there. The diverted traffic raises volume elsewhere, triggering detection there.

Both errors push toward detection rather than away from it. **Each chain closes only if the Segments a Traveller uses are the Segments it raises the volume of** — and under [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) they are the same list of Segments, necessarily, because a Traveller increments whatever it actually drives. **This was the load-bearing *assumption* of the whole scheme while volume was attributed by District pair, and it is now a structural property instead.** It is no longer something a future revision must remember to state and defend; it is something a future revision would have to *break* — by putting a partition, a cadence or a smear back between the failure and the detector — and the note to carry forward is that any such proposal is a change to everything downstream rather than an optimisation.

> **What is repaired, and what is not.** The repair is exact **at the Segment**: experience and contribution are one list, so nothing sits between the failure and the detector. **It is not a claim that the list is the right one.** If a Statistical Trip's route is ever District-granular — and both path-source rungs S2 R2 left standing are — then every Trip bound for a District is threaded through that District's **one representative node**, and the arcs into it carry the whole of its inbound traffic: the same surge reads **412%** `v/c` on the watched arc under a shared District route against **130%** under a per-Trip search, and a shared route costs **36.01%** mean detour against a search's zero. The detector still watches the Segments the Traveller drives, which is what this section needs. What it cannot promise is that those are the Segments a real Trip would have used, so **Stress on a representative node would be an artefact of the partition rather than a property of the city** — the same defect class §3.9 rejects for the Microscopic Cap and `adr/0041` rejects for volume attribution, arriving a third time by a different door. **Nothing in the corpus addresses it yet**; it is [`plans/0002`](../plans/0002-open-questions.md)'s routing cluster. Recorded here because this is the section where it would otherwise be mistaken for a solved problem.

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

> ~~**Open:** the audit rate, and what happens on repeated divergence (permanent promotion? a flag for us?). The metric is settled.~~
>
> ✅ **SETTLED 2026-08-16 by session E** ([`adr/0108`](adr/0108-repeated-divergence-is-a-bug-report-and-the-queue-decides-how-long-a-promotion-lasts.md)). **There is no escalation mechanism.** A divergent Segment is promoted, and how long it stays Microscopic is decided by §4 invariant 3's **queue guard** and by nothing else — the divergence that justifies promoting it *is* a non-empty queue (this section's own detector says so two paragraphs up), so a Segment that is genuinely failing holds itself until it stops, keyed on the failure rather than on a memory of it. **Permanent promotion is refused**: the permanently-promoted set is a collection that grows with elapsed time ([`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md), invariant 5) and a standing claim on the Cap, and it would make the simulated set a function of **audit history**, which invariant 4 forbids in one sentence.
>
> ⚠ **And *a flag for us* was the answer, with a consumer named in §5.1 since the first commit** — *the audit is the discovery route*, whose own paragraph says it is closing this loop. `plans/0012` **Cause 1** inside one file, five sections apart. ***A question that offers its own candidate answers stops being read as open in the ordinary way***: a reader arriving here is handed a menu and does not go looking outside it.
>
> ⚠ **Repeated divergence at one Segment is a measurement of `complexity_factor`** (§3.3), not an escalation trigger — a junction the audit repeatedly catches is by construction one whose complexity factor was too low. Holding it promoted would pay for ever for a parameter that is wrong once. A **learned** complexity factor was drafted and refused: it puts a feedback term in a traffic model that has none by decision ([`adr/0046`](adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)), and opens a learning rate to avoid opening a measurement.
>
> ⚠ **The rate's *unit* is settled and its value is not.** *"The sample size is a constant"* above is [`adr/0059`](adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s retired unit: any constant makes the time to look at every Segment once proportional to how much road the city has — 33,024 Segments today against **525,312** on a fully paved 512-Cell map. A Ruleset states a **coverage period** and the engine derives `sample = ceil(Segments × interval ÷ coverage_period)`. **Bullet 3 above bundles *deterministic* with *fixed-cost* exactly as `02 §5.7` bundled cost with pacing**, and the constant delivers the second while silently setting the period that **bullet 1** depends on. ⚠ **There is no forced default**: [`adr/0101`](adr/0101-a-commute-is-two-journeys-and-the-days-shape-is-a-property-of-the-job.md) gave the Day a shape, so a period commensurate with `TICKS_PER_DAY` would audit every Segment at the same phase of the Day for ever and be blind to a junction that fails at peak. **The period must precess against the Day**, and it stays **unset** — a gap, with a ratifier named in `adr/0108`.

### 3.6 Known blind spot

**Junctions that fail at low volume because of turning conflicts.** V/C reports them as healthy at 40% of capacity, so they stay Statistical, so the failure is never simulated — and the player watches vehicles flow serenely through a junction that should be gridlocked.

This is the deferred turning-movement work in [`deferred.md`](deferred.md) resurfacing as a *correctness* problem rather than a UI one, and the coupling is tighter than that document suggests. Mitigated by the complexity factor (§3.3) and the audit (§3.5); **not eliminated.**

### 3.7 Walking is always Statistical, and there is one graph

Half the Legs in this city are walk Legs — `adr/0008` guarantees a car commute is never fewer than three — so the fidelity ladder has to say what happens to them. It says: nothing. **Walking has one fidelity, permanently.**

> ⚠ **That guarantee is *built* as of 2026-08-14** ([`adr/0098`](adr/0098-a-citizen-travels-in-their-households-mode-and-mode-choice-is-undesigned-rather-than-unbuilt.md), milestone 5c task 5), and the sentence above turns out to be the load-bearing one. `TripEngine` builds a car commute as `walk → drive → walk`, the two flanking Legs being session F's named placeholder — pedestrian Access Point to vehicle Access Point, equal by construction and therefore **zero-length** until milestone 8. **The ratio this paragraph asserts is now exactly two thirds by construction**, so *half the Legs* is a floor rather than an estimate, and `adr/0008`'s *"Trip object count roughly triples"* is a property of the running build rather than a prediction about it. ⚠ **The first cut of that task built one Leg**, reasoning from a doc-comment that forbids the *fallback* rather than from the ADR that specifies the *shape* — `adr/0093`, and ***a doc-comment forbidding one shape is not a decision permitting the others***.

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

  > **This bullet is the half of the contradiction that survived.** The corpus answered *which route does a Trip have* twice, differently, in this one document: §3.3 attributed volume along a **District-pair** route while this bullet animates a Traveller along **its own**, which is two routes per Trip and nothing reconciled them. [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) moved attribution onto the Traveller, so there is one route, and this bullet is now in tension with nothing. *(`adr/0041` and `plans/0010` both cite this section as `03 §3.6`; the sentence they quote is this one.)*
- A Traveller on a Microscopic segment off-screen is simulated but not drawn.
- Nothing the renderer does can influence the simulated set.

This separation is what makes the camera safe. Any future feature that would let rendering influence simulation reopens the observation problem and should be treated as an architectural change, not a tweak.

### 3.9 The Microscopic Cap is a world constant, and it is not a failure mode

A ceiling on how much may be Microscopic at once still exists. It is called the **Microscopic Cap** — renamed from *Fidelity Budget*, because the corpus already has a **Commute Budget** that the player reads and acts on, and sharing the word let an internal ceiling borrow a gameplay concept's authority. That conflation caused the second half of this section.

**It is set from world configuration, identical on every machine, and not player-adjustable** — the same category as `TICKS_PER_DAY` under [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md), and derived from the world rather than being a bare number, since a ceiling sensible for 512² is not sensible for 1024².

> **The budget it is derived *against* is the design speed's — 62.5 ms at 1× — and not the top rung's** ([`adr/0096`](adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md), 2026-08-13). Pricing it at 15.6 ms would buy a stutter-free 4× by choosing **this section's own first row for every player, permanently, to avoid its second row on one machine** — and `01 §1` now states that 4× is the first thing a large city stops offering, so it is the budget of a speed the binding case is not running. A machine that cannot sustain 3× or 4× at this fidelity **dilates wall-clock time and says *simulation running behind***, which is the second row working as written and costs nothing in reproducibility, because the Cap is still one number on every machine.
>
> ⚠ **Two things are recorded there rather than decided, and both belong beside this paragraph.** **A fallback tier below Microscopic is foreseen**: what this section describes is binary — Microscopic, or the VDF that §3.2 calls structurally wrong exactly where the Cap binds — and a cheaper middle tier is the obvious repair. It is deliberately **not designed**, because the Cap's demand side has never been measured and *given the Cap is too small, should something compensate?* is [`adr/0070`](adr/0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s void question verbatim. And **every supply figure the Cap has ever been quoted from is one core** — a 2- and 4-thread measurement is owed to S5 and is the largest unclaimed multiple in the whole question.

> **It counts *Vehicles*, and this section said *segments* until [`adr/0062`](adr/0062-the-microscopic-cap-counts-vehicles-and-nothing-is-ever-evicted.md).** A Lane's per-Tick cost is one pass over its queue, so the bill scales with what a Segment *holds*: a six-lane arterial and a residential street are not one slot each, and occupancy varies most in the direction that binds, because a Segment holds the most Vehicles exactly when it is jammed — which is why it was promoted. A Segment-denominated ceiling is least accurate when it is doing its job. This is the family [`adr/0053`](adr/0053-failure-pressure-is-a-duration-not-a-tally.md) and [`adr/0059`](adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md) already named — *state the quantity in the unit the thing paces in* — and **nothing in the world-constant argument above depends on the unit**, since that argument is about who sets the number.
>
> **`adr/0062` also settles what happens when it binds, which this section left as a fallback rather than a rule.** Nothing is ever evicted: a queued Segment holds state nothing can rebuild, so a full Cap **refuses**. **Force-promotion is admitted ahead of stress-promotion** — spillback is one of `§5.1`'s acceptance criteria and is therefore correctness, while better travel times are accuracy — with ties broken on Stress and then on Segment id. A **virtual queue** as a third representation is refused and named, per `adr/0016`'s standing warning.

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
3. **Demotion is lossy only in enumerated ways**, and the enumeration is **four fields** ([`adr/0107`](adr/0107-a-demotion-discards-the-cursor-and-nothing-it-discards-has-to-be-invented.md)). What a Traveller loses when it leaves a Microscopic segment is its **position along the Lane**, its **velocity**, its **Lane assignment**, and a **Switch Lane traversal in progress** — all of it cursor state, none of it the plan, because the plan is on the Leg and the Leg is not the embodiment ([`adr/0075`](adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md)). **Anything not on that list is a bug**, which is what the list is for.

   > **None of the four has to be invented, so invariant 2 holds across a full demote/promote cycle rather than only across a promotion.** **Position and velocity are one derivation read once**, both out of [`adr/0099`](adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md)'s dwell, which it already computes on entry from that Segment's volume — position is the **fraction of that dwell already elapsed** times the Segment's length, which is not an approximation of where the Vehicle was but exactly where the Statistical model said it was, and velocity is that dwell over that length. *(`adr/0107` first said position was the Segment's **entry point** and was amended hours later: a Segment crosses `T_high` on whatever Tick its volume crosses it, and every Traveller already on it is partway through. That was a plausible default wearing a derivation — the same failure the velocity rule was written to avoid, one bullet away.)* Lane assignment follows from the next turn; and a Switch Lane traversal is always recovered as *none*, because a Traveller that has just entered a Segment has not begun one. **Force-promotion is the case that tests this**, and under the dwell rule it is not a special case — a Segment promoted because its downstream neighbour is full was Statistical until that Tick, so its vehicles enter at the speed they were actually making and meet the queue, which is what spillback should look like.
   >
   > ⚠ **Velocity is deliberately *not* free flow, and §3.2's *"not an approximation but an exact answer"* is why the distinction had to be made.** Since 5c the volume-delay function runs on Statistical Segments too, so free flow is the value only at zero volume: at the shipped `α = 15%`, `β = 4` a Segment at `v/c` 0.8 is **6.1%** slower and at 1.0 **15%** slower — and the gap is widest just below `T_high`, which is the only place a promotion ever reads it. [`adr/0007`](adr/0007-stress-driven-simulation-detail.md) is amended; ***a sentence that idealises for the sake of an argument becomes a premise for the next argument.***
   >
   > ⚠ **A Traveller holds two cursors one word apart and only one is lost.** The **route** cursor — which Segment of the Leg it is on — **survives a demotion and must**, because [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) attributes volume by entering and leaving a Segment, so a Traveller without a next Segment never decrements and leaves a `+1` standing for ever: a road busy for ever with nothing on it, which is [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md). What is discarded is position along the **Lane**, inside a Segment. *(An earlier `adr/0075` amendment said the opposite and is withdrawn there — it also offered *reconstructible from the route and the clock*, which [`adr/0099`](adr/0099-a-legs-cost-is-a-plan-and-a-drive-is-priced-segment-by-segment-as-it-is-met.md) removed by pricing each Segment on entry from its live volume: the journey whose position is least recoverable is the congested one, which is the only one ever promoted.)*
   >
   > ⚠ **Two things this list is *not*.** **Headway is not a field** — §5 has vehicles hold no references to each other, so it is computed inside the Lane's pass from two positions and is held across no Tick. And **Overlap projections are not fields**, for the same reason: §5 exchanges them *each tick*. An earlier list ([`adr/0075`](adr/0075-a-leg-is-a-plan-and-a-traveller-is-a-cursor.md)'s consequences) named *queue position, headway, a Switch Lane traversal* — which is one field, one derived quantity and one field, and omitted velocity and Lane assignment. ***A list that names a derived quantity as state cannot be checked against a structure***, and it fails in the unguarded direction: too **long** reads as more careful rather than less.

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

1. ~~**Audit rate and response to repeated divergence.**~~ **Closed in §3.5 except for one number** ([`adr/0108`](adr/0108-repeated-divergence-is-a-bug-report-and-the-queue-decides-how-long-a-promotion-lasts.md), 2026-08-16). ~~Divergence metric~~ **settled** — travel time against the statistical prediction, on audited unstressed segments only. **Escalation policy settled**: there is no escalation mechanism, invariant 3's queue guard bounds a promotion, permanent promotion is refused, and the record's consumer is §5.1's suite — which §5.1 had named since the first commit. **The rate's unit is settled** — a coverage period, not a sample size (`adr/0059`) — and **its value is unset and is a gap**, because the world that would ratify it needs milestone 21's Overlaps and a junction that fails at low volume.
2. ~~**Do Microscopic segments form contiguous regions?**~~ **Closed in §3.3 — yes, by a second, event-driven trigger.** The assumption that spillback would handle it naturally was wrong on timing and left the tier boundary undefined. Force-promotion on downstream blocking, a virtual queue at the boundary when the Cap is reached, and a demotion guard on non-empty queues (§4, invariant 3). **Revisit once there is a build:** the interaction between force-promotion and the Cap is the untested part — a single arterial jam could in principle cascade promotions along its whole length and consume the Cap by itself. **Half of the answer has since been withdrawn:** the *timing* argument for force-promotion went with `adr/0041`'s attribution change, and S2 R2b found the defect it named was never a lag at all. The structural argument is untouched, and whether it alone justifies the mechanism is **open** — see §3.3 and [`plans/0002`](../plans/0002-open-questions.md).
3. ~~**What is the microscopic budget, and is it player-facing?**~~ **Closed in §3.9.** Renamed the **Microscopic Cap**; a world constant, not player-facing, and **not a failure mode** — it was removed from `01-player §6` entirely. `HONEST DEGRADATION` demanded it be *visible*, never *adjustable*, and §7's existing overlay-honesty rule already provides the visibility. Still genuinely open: the **number**, and the broader question of keeping gameplay mechanics decoupled from simulation resource constraints.
4. **Transit.** ~~Multi-modal trips.~~ Settled in part: walking is a simulated Leg, so the Trip model is genuinely multi-modal rather than nominally so — see [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md). Whether *transit* is ever implemented remains open and is owned by `01-player-experience.md`, but it is no longer an irreversible decision: a bus is a Leg type inserted into machinery that already handles Legs.
5. **Car ownership.** Settled: parking is modelled supply with no search — see [`adr/0009`](adr/0009-parking-is-modelled-supply-never-search.md). What remains open is what *generates* parking demand. Every Household owning a car is the simple assumption; making ownership a choice influenced by walkability and transit access would close the loop, letting parking pressure feed back into whether people drive at all. Owned by `01-player-experience.md` alongside transit, since the two only become interesting together.
6. **Freight.** Inter-District Shipments use the same movement machinery, but whether freight vehicles contribute to segment stress identically to commuters, or are weighted, is undecided.
