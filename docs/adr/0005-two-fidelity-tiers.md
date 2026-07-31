# Two fidelity tiers, and decisions are never shared

> **Partially superseded by [`0007-stress-driven-simulation-detail.md`](0007-stress-driven-simulation-detail.md).** Two levels of movement fidelity survive, and "decisions are never shared" is untouched and load-bearing. What changed is what fidelity attaches to: a *Segment*, not a Citizen. Read every "Detailed tier" below as "the microscopic regime," and disregard the implication that a Citizen carries a tier. The Cohort rejection stands entirely.

Citizens vary in fidelity so that a large city stays affordable. We originally specified three tiers — Detailed (real position, per-tick movement), Statistical (trip resolved as departure/arrival ticks from a travel-time lookup), and Cohort (Citizens sharing home, work, and demographics collapsed into a single record with a count). **We have dropped the Cohort tier. There are two tiers, and the axis is movement alone: every Citizen keeps its own record and makes its own decisions at every tier.**

## Why

The Cohort tier conflated three independent axes — movement fidelity, decision fidelity, and storage fidelity — and dropped two of them at once when only one was ever justified.

**Storage was never the problem.** A Citizen record is roughly 40 bytes. A million Citizens is 40 MB, and the event-driven Statistical tier costs about 1% of a core at that scale. Deleting records to save memory solves a problem we do not have, at a scale three orders of magnitude past our target.

**Decision-sharing was the only real saving, and it is behaviourally wrong for this game.** The expensive part of a Citizen is not movement or storage but *choices* — sampling candidate dwellings, scoring them, drawing an outcome. Cohorting would evaluate that once and apply it to hundreds of Households.

But the entire premise of the choice model is that identical Households choose *differently*, because the random component stands in for preferences we chose not to simulate. Sharing a decision asserts the opposite. It would produce hundreds of Households moving to the same district in the same month off one draw — reintroducing exactly the herd behaviour that the logit scale parameter exists to damp, and through the back door where it is harder to see.

**It also made Pillar 2 dishonest.** The vision claims Citizens are persistent identifiable individuals "always, at every fidelity tier." A collapsed Cohort discards individual variation, so promoting a member requires *inventing* their state — meaning they were never a distinct person, only a procedural generation waiting to happen. A population that shares a brain is a statistic wearing faces.

## Consequences

- **Promotion is reconstructible by construction**, not by discipline. Nothing is ever discarded, so the record plus the current Tick always suffices to materialise a Citizen's position. What was an invariant to police is now a property of the design.
- **The Cohort garbage collector is deleted**, and with it the RimWorld `WorldPawnGC` failure mode — an unbounded demoted population that becomes the performance problem. There is no demoted population.
- **One LOD boundary to calibrate instead of two.** The Statistical tier must still reproduce the distributions the Detailed tier produces, or the boundary becomes an exploitable game mechanic.
- **Unbounded growth in the Citizen population remains a live risk** and is *not* addressed by this decision. Dwarf Fortress's cost grows with elapsed time because objects accumulate. Citizens need real sinks — emigration, death — independent of anything here.
- **If decision cost ever becomes the bottleneck**, the levers are sampling fewer candidates (a smaller `N`, which we already want for gameplay reasons) or deciding less often. Not deciding collectively.
- If a genuine need for group representation appears at some far larger scale, it can be added — with profiling data rather than speculatively.

## What would trigger revisiting

**Decision cost measurably dominating a profile at target scale**, after the two cheaper levers have been tried and exhausted: sampling fewer candidates per choice, and deciding less often. Both are already wanted for gameplay reasons, so they should be spent first.

Even then, group representation would need to answer the behavioural objection above, not merely the performance one. A shared decision applied to hundreds of Households reintroduces the herd behaviour the logit scale parameter exists to damp, and does it somewhere harder to observe. Any future proposal must show how identical Households still choose differently.

Note that the *fidelity* half of this ADR has already been revisited — [`0007`](0007-stress-driven-simulation-detail.md) moved it from person to place. The *decision* half has not, and is the part worth defending.
