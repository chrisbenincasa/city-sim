# A diversion rejoins by local descent, and a rejoin is never a search

**A Traveller that leaves its Habit Route at a junction carries a *Rejoin Target* — the node it declined to enter — and returns to the route by applying the rule it already has: at each following junction, take the arc that reduces the straight-line distance to the Target. There is no search, no re-plan, and no second path source. A rejoin costs one decision per crossing, which is the cost Sight already pays.**

**When the rejoin does not succeed, the Traveller does not search either.** It stops aiming at the Target, points the same rule at its destination, and continues. The cost lands as minutes, which the **Commute Budget** already scores, and there is no new Trip Fate.

**A failed rejoin is counted, not corrected.** A Citizen carries an **Aggravation** — the fraction of its recent journeys on this Habit that diverted or stranded — and crossing its threshold makes it **switch to another variant** of the Habit ([`adr/0060`](0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md)). It does not recompute anything.

`BOUNDED KNOWLEDGE` `HONEST DEGRADATION` `SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE`

## Why

### This is the answer to the largest number in the corpus

[`adr/0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md) made diversion **routine** — an every-junction possibility for the whole fleet — and [`adr/0047`](0047-routing-never-keys-on-the-district.md) then deleted the one path source under which a mid-journey diversion was free. **Nobody had multiplied the two.** S2 R6.3 did: at 40,000 Travellers on a 7-Day Habit, a diverting Traveller that re-searches costs **861.87% of the Tick budget**, which is 99.76% of routing's entire bill. The route cache cannot rescue it — it would need an **88.5%** hit rate on the input it is worst at, since a diversion's origin is *wherever congestion happened to be*.

Session M settled the principle — *rejoin rather than re-search* — and nobody had made it a mechanism. This is the mechanism, and the reason it is affordable is that **it is not a cheaper search. There is no search.**

### The obvious fallback fails on arithmetic before it fails on principle

*Re-search only when the rejoin fails* is the honest-looking compromise, and it does not fit. At R8.3's **1,269.51 diversions per Tick** and R6.4.2's **85.74%** rejoin success at a radius of three Segments, the residue is **≈181 re-searches per Tick against a band of 32–147 that fits.** The fallback is still **1.2–5.7× over budget** — it re-admits the problem in proportion to the failure rate rather than removing it.

*(Both figures are S2's, on an invented origin-destination draw and — for 1,269.51 — a District-granular tree `adr/0047` has since deleted. Neither may be quoted as a verdict. What survives any rung is the **shape**, and it is recorded here as a tripwire: **a re-search fallback fits only if rejoin failure comes in under roughly 2–12%**, which is a far harder demand than 85.74% success.)*

### Straight-line distance is the right instrument, including where it is wrong

A driver going round a block does not run a graph search; it knows the target is *that way*. Straight-line distance to the Rejoin Target needs no graph traversal, no visited set, and one comparison per arc on a node of degree ~3.

**It is wrong exactly where the map is deceptive** — across a river, along a one-way pair, at a severance — and those are the cases that strand. That is the correct behaviour rather than a defect: a driver who does not know the bridge is two miles north genuinely does get stuck, and `BOUNDED KNOWLEDGE` is the pillar that says so. It does mean **stranding is ordinary rather than exceptional**, which is why the failure path had to be as cheap as the success path.

### Aggravation replaces a classification, and that is why it is here

An earlier form of this decision split the failure at the moment it happened — a **strand** is evidence about what *exists* and could mark the Habit stale; a **spent crossing budget** is evidence about congestion and could not, because [`adr/0046`](0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md)'s static Habit is ratified over exactly that case. It worked, and it required the simulation to classify a failure at the instant of failure, using a test that cannot reliably tell a jammed one-way pair from a severance.

**A counter removes the need to classify.** Both outcomes are bad journeys; the threshold does the work the classification was doing, and one mechanism replaces two. This is [`adr/0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)'s direction — a decision that **removes** a quantity is worth more than one that sets it well.

### It is dimensionless, for the reason `adr/0053` already gave

[`adr/0053`](0053-failure-pressure-is-a-duration-not-a-tally.md) settled that failure pressure is a duration and not a tally, because a tally is **not scale-free**: something that fires twice as often accumulates twice as fast. The same bites here. A raw count of `N` bad journeys makes a Citizen who drives four times a Day switch four times as often as one who drives once, so a single Ruleset number would mean different things to different Citizens. **Aggravation is therefore a fraction — bad journeys out of recent journeys — and survives a Ruleset that retunes everything around it.**

### Nothing recomputes because a road got busy, and that is still true

Aggravation is caused by congestion, so it looks like the adaptive Habit `adr/0046` deferred to measurement and R8.5 declined to refute. **It is not one**, and the distinction is exact: an adaptive Habit *recomputes a route against a cost basis*; this *switches between candidates that were computed once and never change*. The candidate set is static. Nothing reads a live cost to build a route, no refresh cadence exists, and R8.5's ratification is untouched.

What is new is that the Citizen's **choice among static candidates** responds to experience — which is `adr/0017`'s switching rule, which every other actor class has always had.

## What is argued here and what is not

| Claim | Type | Where it goes |
|---|---|---|
| A rejoin is local descent and never a search | **arguable** | settled here |
| A failed rejoin continues on Sight to the destination, with no new Fate | **arguable** | settled here, on `adr/0009`'s precedent |
| Aggravation is a fraction, not a tally | **arguable** | settled here, on `adr/0053`'s |
| Switching a variant is not an adaptive Habit | **arguable** | settled here — the candidate set is static |
| **The crossing budget** before a rejoin is abandoned | **measurable** | **unset** — [`0002`](../../plans/0002-open-questions.md) §D2. R6.4.2's cliff at 3 Segments is the evidence and not the value |
| **The Aggravation threshold**, and its per-Citizen spread | **measurable** | **unset** — §D2. Hash-bearing |
| Rejoin success rate in a real city | **measurable** | `06` **5b**. R6.4.2 measured it on an invented draw |
| Whether switching destabilises `03 §3.4`'s loop | **measurable** | R8.5's instrument, static against switching — [`adr/0060`](0060-a-habit-route-is-a-small-set-of-variants-and-which-one-you-take-is-who-you-are.md) carries the row |

## Rejected

**Re-search on failure.** Refused on the arithmetic above, not on principle.

**A dedicated Trip Fate for a lost driver.** [`adr/0009`](0009-parking-is-modelled-supply-never-search.md) refused a *no parking* Fate for the same reason: a second failure channel where an existing one already expresses the outcome converts a gradient into a cliff. A driver that wanders spends minutes; minutes are what the Commute Budget scores; a Trip that wanders far enough fails on the Fate that already exists.

**A third setter on session M's stale bit.** The earlier form of this decision would have let a strand mark the Habit for recomputation, which makes three causes collapse onto one bit — and M had already recorded that one bit cannot say *why*. **Aggravation removes the need**: it is its own field with its own meaning, so [`adr/0012`](0012-routing-intent-lives-in-the-agent.md)'s contract keeps its **two** setters — age past `T`, and an addition within `d` — and M's second bit stays *available rather than taken*, unchanged.

**Remembering the nodes visited during a rejoin, to detect a loop.** The crossing budget already bounds the wander, and a visited set is per-Traveller storage bought to detect a condition the budget catches anyway, one crossing later.

## Consequences

- **A Traveller gains two transient fields** — a Rejoin Target and crossings spent — both of which die with the Traveller on arrival. Neither is conserved state, so `CONTEXT.md`'s rule that *a Traveller is a view, not an owner* holds.
- **A Citizen gains the Aggravation counter and its journey denominator.** Both drain to zero on a variant switch, so the pair has a **sink** and `adr/0006` is satisfied by construction rather than by an invariant somebody has to police.
- **Two hash-bearing numbers enter [`0002`](../../plans/0002-open-questions.md) §D2** — the crossing budget and the Aggravation threshold — each with a named ratifier on the day it is chosen, per [`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md).
- **The Sight Horizon is confirmed as two parameters and this ADR uses only one of them.** R6.4.2 found rejoin success cliffing **19.14% → 85.74% at 3 Segments**, identically on all five origin-destination rungs, because going round a block is three Segments. That is the **crossing budget**, not the horizon: `CONTEXT.md` defines the Sight Horizon as a lookahead *along the Habit Route*, which a Traveller that has left the route does not have. **The two must be named separately or the derived floor of 1 and the measured cliff at 3 will be read as disagreeing about one number.**
- **A slow herd is now possible and nothing yet damps it.** Temperament breaks the tie at the junction, on the Tick; nothing breaks the tie at the **switch**, so a population can become frustrated with the same variant in the same week and move together. The threshold needs Temperament's own treatment — a stable base plus per-Citizen spread — and this is recorded as owed rather than solved, because the spread's *value* is measurable and the *shape* belongs with the threshold when it is chosen.
- **`CONTEXT.md` gains *Diversion / Rejoin* and *Aggravation***, because `01 §5`'s notification surface will have to say *why this driver went that way* in exactly these words.
- **`03 §5` is owed the mechanism.** The section describes the traffic model on Microscopic Segments and says nothing about what a Traveller does when it changes its mind, which is how the largest cost in the corpus came to live in a section nobody was reading.

## What would trigger revisiting

- **Rejoin success coming back poor at `06` 5b**, on real demand rather than an invented draw. The fallback is a **bounded local search** whose goal set is any node on the unconsumed suffix of the Habit Route, capped by the same crossing budget — it is a search, but a bounded one, and it is named here so that reaching for it is a decision rather than a drift back toward re-search.
- **Straight-line descent proving pathological on this map** — long stranding runs on a river or coastal geometry, where the target is near in space and far on the graph. The mitigation is the bounded search above, not a longer budget.
- **The Aggravation threshold degenerating.** If most Citizens sit at the threshold, switching becomes a per-journey event and the variant set is being consumed rather than chosen from; if almost none reach it, the counter is inert and should be deleted rather than tuned.
- **A herd appearing at the switch** that a per-Citizen spread does not damp. That would mean the tie-break has to move off the Citizen and onto time — staggered switch eligibility — which is a different model and would need its own record.
