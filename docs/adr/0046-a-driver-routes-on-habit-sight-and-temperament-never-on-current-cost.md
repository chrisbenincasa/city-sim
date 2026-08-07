# A driver routes on habit, sight and temperament, never on current cost

**Route choice reads a *lagged* expectation of the network plus a *live* view of what is immediately in front of it. It never reads a current global cost field.** Three layers, each answering a different question, and each with a different refresh rate:

- **Habit** — *the route I normally take.* Computed once from a slow-moving cost basis and reused across many Trips. **Static per world is the null hypothesis** and it must be measured false before anything that maintains itself is built.
- **Sight** — *what I can see from here.* At a junction, the live cost of the next few Segments along the Habit Route, against the alternatives leaving that junction. The **Sight Horizon** is a Ruleset number whose *floor* is a property of the Road Graph rather than a preference.
- **Temperament** — *how bad it has to get before I bother.* A per-Citizen threshold: a **stable base**, which is part of who that Citizen is, plus **per-decision jitter**. Two purpose tags, never one.

`BOUNDED KNOWLEDGE` `UNIQUE INDIVIDUALS` `EMERGENCE` `LEGIBLE CAUSE` `HONEST DEGRADATION`

> **Measured by S2 R8, the same session this was written.** All three layers survive and the structure
> stands. **Static Habit is not refuted**, which shuts the refresh-cadence question this ADR expected
> to be its first trigger. The claim table below carries each reading and
> [`spike-results`](../spike-results.md) → *S2 R8* carries the working.
>
> **Two things R8 found that this ADR did not anticipate, and both belong to its successor rather than
> to an amendment here.** First, **the base threshold and the Sight Horizon are not independent
> numbers**: BPR at `β = 4` clamped at 4.00 makes a saturated arc 39.4× its free-flow time, so a
> lookahead of even two arcs can carry more live cost than the free-flow remainder it is weighed
> against — the detour is priced at free-flow, looks cheap, and the horizon stops behaving like a
> monotone knob. Second, and larger than anything in this ADR: **the Habit layer as built concentrates
> 87.25% of a city's traffic onto 1% of its road**, because one free-flow shortest-path tree per
> District means one route per (node, District) pair in the whole model. Sight is then relieving a jam
> its own null hypothesis created. *That is why Habit is named a layer and not a baseline* — and it is
> a question about what a routing destination may be, which is `plans/0010` decision 15 and is not this
> ADR's to take.

## Why

### This is [`adr/0017`](0017-agents-satisfice-they-never-optimise.md) applied to routing, and it should have been written the day that one was

`adr/0017` settles how every actor in this simulation chooses: *"keeps a short, sticky Provider List — the shops, workplaces, suppliers and services it already knows about — and switches only when a **known** alternative is **substantially** better."* Read that sentence with a road network substituted for a Provider List and the three layers fall out of it verbatim.

| `adr/0017` | Here |
|---|---|
| a short, **sticky** list | **Habit** — the route I already take |
| a **known** alternative | **Sight** — the alternatives I can see from this junction |
| **substantially** better, *by enough to be worth the bother* | **Temperament** — my threshold for bothering |

So the decomposition is not invented for traffic; it is the corpus's existing choice model arriving at the one actor class nobody had applied it to. That matters twice over. It means **the burden of proof runs the other way** — a router that evaluates the network optimally is the thing that needs defending, not the one that satisfices — and it means the failure mode is already named: `adr/0017` exists because Citybound spent months on a household planner that was NP-hard per household, and a live global cost field re-read per Traveller per Tick is the same mistake with a smaller exponent.

It also supplies the one thing this decision would otherwise be short of. `adr/0017` says *substantially* and never says how much, because the number is per-actor and emergent. Temperament is what that word has been waiting for.

### The obvious scheme is priced out and the cheap scheme is closed on a commitment

Routing on the *current* network state means every Traveller re-deciding whenever the cost basis moves, and under [`adr/0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) the volume column moves **every Tick**. [`plans/0010`](../../plans/0010-s2-routing.md) R3 published the ceiling in the form that survives a missing denominator: *routing fits while fewer than 85 Trips start per Tick*, with flat A\* reaching **12.4 ms** — most of a 15.6 ms Tick — at sixteen. A scheme that re-decides for the whole fleet every Tick is not over budget by a factor to be optimised away; it is asking a different order of question than the budget can answer.

The scheme at the other end is routing on **free-flow** costs, which is nearly free and has already been refused. [`03 §3.4`](../03-agent-architecture.md) makes the VDF's self-correcting circularity *"the load-bearing assumption of the whole scheme"* — a travel time that overstates congestion diverts traffic, which lowers the volume, which lowers the estimate. **That loop only closes if routing reads the VDF.** Route on free-flow and the VDF becomes a display quantity: it is computed, it is shown to the player, and nothing in the city responds to it. `plans/0010`'s tripwire already refuses a lagging *detector* on this exact ground; a router that never reads the detector at all is the same failure with the instrument intact.

So the response to congestion must be **lagged and local**. That is a conclusion, not a compromise.

### Which is what a driver actually has

[`CONTEXT.md`](../../CONTEXT.md) → `BOUNDED KNOWLEDGE` is not a performance concession and this is the case that shows it. A Citizen holding a live global cost field is a Citizen with a satnav — and a city where every driver has a satnav is a specific, modern, unusual city, not a neutral default. **The design would be choosing it by omission.** Nothing in [`00`](../00-vision.md) or [`01`](../01-player-experience.md) asks for it, and `01 §7`'s rule that a number must never be caught contradicting what the player is watching cuts against it: a player who can see a jam and a driver who cannot is a legible failure, but so is every driver in the city routing around a jam the instant it forms.

Habit and Sight are the two things a driver without a satnav has. Splitting them is what lets each be refreshed at its own rate — which is the entire cost argument, because the expensive layer is the one that touches the whole network and the cheap layer is the one that touches four arcs.

### Sight has a floor, and the floor is a graph property

The temptation is to set the Sight Horizon at one Segment: a driver sees the road ahead, and it is the cheapest thing that could possibly work. The counter-argument was raised as *drivers arrive at congestion too late to do anything about it*, and its precise form matters, because the loose form is wrong.

A Traveller decides at a **node**, so seeing one Segment ahead *is* actionable in principle — the alternatives are the other arcs out of the node it is standing on. What breaks is that **not every node is a decision**. R3 measured this network at degree ~3 on average, which means some far-nodes are degree 2: mid-block, no alternative. A Traveller looking one Segment ahead from such a node receives a signal it is structurally unable to act on, and it will receive that signal again at the next node, and so on until the jam.

So the useful quantity is not *N Segments* but **N against the distance to the next actionable junction**. That distance is measurable directly off the Road Graph with no traffic at all, and it sets the floor for `N` before any behavioural argument runs. This is the one place in the routing model where a parameter's lower bound is derivable rather than tuned, and it should be derived.

### Temperament is load-bearing, not flavour

Without it, every driver standing at the same node in the same Tick reads the same numbers and makes the same decision. That is a Cohort in everything but storage, which [`adr/0005`](0005-two-fidelity-tiers.md) bans on principle — and here the principle has a mechanical consequence rather than only a philosophical one. **An identical rule over an identical input produces a herd**: the whole flow switches to the alternative together, the alternative jams, the whole flow switches back, and the city oscillates for ever at the period of the loop. `03 §3.4`'s self-correction becomes self-*excitation*.

Temperament is the only thing in the model that breaks that tie differently for different people. `UNIQUE INDIVIDUALS` is usually argued as what makes the city *interesting*; this is the case where it is what makes the city *work*, and that is worth recording because it is the strongest available answer to `00`'s own stated objection that per-Citizen simulation is an expensive way to buy charm.

**Stable base plus per-decision jitter**, and both halves earn their place. A purely per-decision threshold makes each driver a fresh coin flip, so no Citizen is ever *the sort of person who takes the back roads* — the population is diverse in aggregate and uniform in character, which is `adr/0005`'s failure wearing a distribution. A purely stable threshold is deterministic per Citizen, so the same driver takes the same decision on the same data every Day and the flow re-synchronises into a smaller, permanent herd. The base is who you are; the jitter is what kind of morning you are having.

The blend between them is a number nobody has argued. It is recorded here as owed rather than chosen, because inventing it would be exactly the failure [`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) exists to name.

### What is settled here and what is not

The **structure** is arguable and this decision closes it: the layer decomposition, the refusal of live global cost, the refusal of free-flow, and the requirement that a per-Citizen threshold exist at all. Every **parameter** is measurable and none of them is set here.

| Claim | Type | Where it went | What it read |
|---|---|---|---|
| Route choice cannot read live global costs | measured | R3 | Confirmed — flat A\* fits below 85 Trip starts per Tick |
| **Static Habit suffices** | measurable | **R8.5** | **Not refuted.** Under a *sustained* demand asymmetry Sight settles **42.62% below** a control with identical physics and no ability to respond; the control settles *above* its own pre-surge level and Sight *below* it. `03 §3.4`'s loop closes on the local layers alone, **so no refresh cadence exists to argue about** |
| **A Sight Horizon of one is actionable** | measurable | **R8.1** | **Not refuted.** **98.02%** of arrivals are already at a node with a real choice; the floor is **1 Segment**, taken at p90 |
| **Temperament damps the herd** | measurable | **R8.4** | **Damps by 92.28%** where a herd exists, on an instrument shown able to separate a maximal-herd control. **The wire stated on *monotonicity* is REFUTED as written** — the response is a cliff, not a gradient, and the rungs after it are closer together than the instrument resolves |
| **Sight's per-crossing cost fits the budget** | measurable | **R8.3** | **5.64%** of the Tick at the derived floor. But **`Refresh` alone is ~10%** — the per-Tick VDF sweep costs more than the whole traveller loop and is flat in fleet size |

## Rejected

**Live global costs.** Priced out by R3 and by `adr/0041`'s per-Tick volume column. It is the scheme every parameter here exists to avoid needing.

**Free-flow routing.** Breaks `03 §3.4`'s circularity, which the corpus calls load-bearing. Cheap and hollow.

**A periodic global re-route** — every Traveller recomputes every K Ticks. Rejected twice over. It is a **global flush**, which `plans/0010`'s tripwire already refuses on a design commitment rather than on a number. And it is the herd installed deliberately: every driver reacting at the same instant on the same data is precisely the synchronisation Temperament exists to break, so the scheme creates the pathology and then pays for it.

**Temperament baked into a stored route.** [`adr/0012`](0012-routing-intent-lives-in-the-agent.md) keys the route cache by origin–destination pair, **never by agent**, and that is not negotiable here. So Temperament applies **at the decision**, per Traveller, per crossing — it may not be a property of the path that was stored. This constrains where the layer lives and it is the constraint most likely to be violated by accident during implementation.

**An adaptive Habit, now.** Not rejected — deferred to measurement, and deliberately entered as the *alternative* hypothesis rather than the default. If static Habit closes `03 §3.4`'s loop through Sight alone, the entire refresh-cadence problem disappears: no maintenance scheme, no hash-bearing cadence, and R4.6's incremental-versus-rebuild break-even stops selecting an algorithm. That is a large enough prize that the cheap hypothesis must be tested before the expensive one is built, which is the ordering R2 already forced on this spike once.

## Consequences

- **`03 §3.4`'s loop survives, and it changes shape.** It was global and slow — volume feeds a matrix, the matrix feeds route choice, route choice feeds volume. It is now **local and fast**: volume on a Segment feeds the cost seen by a driver at that Segment's junction, this Tick. The correction arrives sooner and reaches less far. Whether that is still a *closing* loop is R8's first-order question and the reason it is a measurement.
- **Two new purpose tags**, `TemperamentBase` and `TemperamentJitter`, and the base's counter is fixed rather than the Tick. Folding them into one would correlate a Citizen's character with its mood, which is the correlation the whole layer split exists to avoid.
- **Four unratified Ruleset numbers** enter [`0002`](../../plans/0002-open-questions.md) on the day this is written: the Sight Horizon, the base Temperament threshold, the base/jitter blend weight, and the Habit refresh cadence. Only the first has a derivable floor.
- **The Habit refresh cadence is hash-bearing if it is not infinite.** [`adr/0044`](0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md) established the test and then got its own second half wrong by citing [`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md) without running its membership test. That test is to be **run** here, not cited — and it is only reached at all if static Habit is refuted.
- **`CONTEXT.md` gains three terms** — Habit Route, Sight Horizon, Temperament — because the model is now named vocabulary rather than an implementation detail, and `01 §5`'s notification surface will have to say *why this driver went that way* in exactly these words.
- **The path-source decision gains a third axis.** Under a next-hop table a mid-journey diversion is free: the Traveller follows the table from wherever it ends up. Under a stored route it costs a fresh search from the current node. Sight makes diversion a *routine* event rather than an exception, so this stops being a footnote and becomes a per-Tick cost that scales with how congested the city is. R8 must price it; session M's structural-versus-temporal argument must then take it as an input.
- **The Microscopic Cap gains a second consumer of the same signal.** Sight reads live `v/c` at a junction; Promotion reads Stress on a Segment. They must read the *same* quantity or the city will divert around a jam it never promotes, which is `01 §7`'s contradiction again.

## What would trigger revisiting

- **R8 measuring that static Habit does not close the loop** — an over-used Segment whose volume does not recover. Habit then needs a refresh cadence, that cadence is hash-bearing, and `adr/0015`'s membership test decides whether it is Ruleset data or a world constant. This is the trigger this ADR most expects to fire.
- **R8 measuring that Temperament does not damp** — amplitude flat or non-monotone in spread. The layer would then be buying diversity and not stability, and the oscillation would need a different mechanism: staggered decision Ticks, or hysteresis on the diversion itself. Both are cheaper than Temperament and neither serves `UNIQUE INDIVIDUALS`, so the trade would be explicit.
- **A Sight Horizon whose per-Tick cost cannot be afforded even at its measured floor.** The fallback is not a smaller horizon — the floor is a floor — but deciding less often than every crossing, which is a different model and would need its own record.
- **The city acquiring navigation as a mechanic.** A Policy, a Service or a Phase 2 technology that legitimately gives Citizens network-wide knowledge. The layers survive it: a satnav is a Habit Route with a short refresh cadence and a Sight Horizon of the whole network, which is the retrofit path and is why the decomposition is worth having even if it is never varied.
- **Milestone 6's Lane model changing what a junction costs.** `adr/0016` makes the Lane the entity, and Sight currently reads a Segment-level `v/c`. If turn movements acquire their own queues, the quantity Sight reads is the wrong one and the horizon would have to be re-derived over a different graph.
