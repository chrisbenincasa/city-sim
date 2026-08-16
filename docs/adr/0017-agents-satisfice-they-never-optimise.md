# Households and Businesses satisfice; they never optimise

**A Household or Business keeps a short, sticky Provider List — the shops, workplaces, suppliers and services it already knows about — and switches only when a *known* alternative is *substantially* better.** It addresses its biggest unmet Need first, re-evaluated a few times a Day. No actor in this simulation ever solves an optimisation problem. There is no "best job" anywhere in the design; there is only *the best of the four this Household has heard of, and only if it beats the incumbent by enough to be worth the bother.*

## Why

This is Citybound's most expensive lesson, and it is available to us for free.

Eickhoff first modelled households as rational actors planning their day by evaluating hypothetical sequences of activities. It produced *"a horrible combinatorial explosion"*, and he eventually identified the formal problem precisely: a **Multiobjective Time-dependent Arc Orienteering Problem with Time Windows** — NP-hard, seconds of CPU **per household**. He spent months there before retreating.

**The reason he retreated is the centre of this ADR, and it is not performance.** He realised he had been modelling *"perfect knowledge and rational, motivated and flexible people"*, when in reality people *"might just have heard about a couple other options and only if the best option they know about is much better than what they currently have, they are motivated enough to change something."*

That distinction decides everything downstream. Had the problem been merely expensive, the honest fix would have been an approximation — a heuristic solver, a cached plan, a coarser tier — and every one of those fixes would have been the same wrong model, computed faster. **Satisficing is not an approximation of optimisation. It is a different and more accurate model of a person**, which happens also to be cheap. Reaching for the cheap version of the wrong model is the failure this project is most likely to repeat under performance pressure, and it is why the ADR is written around the modelling claim rather than the cost. `SOLVE THE ACTUAL PROBLEM`

Three benefits arrive together, which is unusual enough to be worth listing separately.

**1. O(1) per decision.** Scoring a handful of known options is bounded by the length of the Provider List — a Ruleset constant ([`0015`](0015-all-tuning-data-is-hot-reloadable.md)) — not by the size of the city. A Household in a city with 200 shops does exactly the same work as one in a city with five. Nothing here scales with the thing that grows.

**2. It is more realistic than optimality.** No household enumerates every bakery in the city each morning; it goes to its bakery. Stickiness is also what makes decline *gradual and attributable*: a shop does not lose its customers the instant a marginally better one opens, it loses them when something becomes enough better, which is what lets the game say "this shop closed because its customers' commutes got too long" and mean it literally. `BOUNDED KNOWLEDGE`

**3. It prevents synchronised-herd behaviour — the one that actually breaks the game.** Optimal actors sharing a world discover the same best option at the same instant and stampede it. Next cycle it is congested, expensive and full, so they all leave at once. The economy oscillates forever and never settles, and the player watches a city that thrashes for reasons no inspector can explain.

**This is the same oscillation pathology that damped congestion feedback exists to solve** — smoothed cost signals, staggered re-evaluation, per-actor perceived-cost noise. We are buying the same medicine twice in two unrelated subsystems because it is one disease: synchronised actors reading a shared signal and reacting in lockstep. Recognising it as one disease is what makes the treatment consistent rather than ad hoc.

It also reinforces the `UNIQUE INDIVIDUALS` commitment in [`0005`](0005-two-fidelity-tiers.md) from the other end. That ADR forbids sharing a decision across Households on the grounds that identical Households must choose differently. Satisficing over a private list is what makes that mechanically true rather than merely asserted: two Households with identical attributes know **different providers**, because they have different histories. The variation has a cause, not just a random seed.

## Rejected

**Optimal choice with a performance escape hatch** — full evaluation, then a cheaper approximation once profiling demands it. Rejected because the approximation would inherit the herd behaviour, which is a *behavioural* defect and therefore survives every optimisation. The expensive version and the cheap version are wrong in the same way.

**A shared global ranking of providers**, recomputed periodically and read by everyone. Superficially the same as a Provider List and structurally the opposite: one ranking means one decision applied to thousands of Households, which is precisely what [`0005`](0005-two-fidelity-tiers.md) rejects, arriving through the back door where it is harder to see. It is also the same error as routing intent living in the world ([`0012`](0012-routing-intent-lives-in-the-agent.md)) — a shared structure making a choice on a specific Household's behalf.

**Perfect knowledge plus noise.** A logit over *every* provider in the city, relying on the scale parameter `μ` to prevent stampedes, is the standard discrete-choice move and it is not equivalent. It damps the stampede without preventing it, and it costs O(all providers). Our location-choice model already samples a small candidate set and applies the logit to that (`docs/02-simulation-model.md` §5.3–5.4); the Provider List is the same commitment on the recurring-choice timescale. **Knowledge is bounded first, noise second** — in that order, because the bound is the behaviour model and the noise is what we chose not to simulate.

## Consequences

- **The Provider List is persistent state** — saved, migrated, and pruned when a Building is demolished. It is **fixed-capacity** with eviction of the least-used entry, because a list that grows with elapsed time is exactly what [`0006`](0006-no-collection-grows-with-elapsed-time.md) prohibits.
- **How a Household *learns* of a provider becomes a first-class design surface.** Nothing enters the list by omniscience. Entries come from proximity to Trips already being made, from a Life Stage change, from an incumbent failing, and from a bounded random discovery rate. **That discovery rate is the real dial governing how fast the economy equilibrates**, and it is Ruleset data.
- **The city equilibrates slowly, and that is the design, not a bug to be tuned away.** A new bakery does not capture its neighbourhood the day it opens. If playtesting reads this as unresponsiveness, the lever is the discovery rate and the switching threshold — never a switch to global evaluation.
- **A Business's supplier list is the same object as a Household's Provider List.** One mechanism, two users, so stickiness in the supply chain comes for free rather than being invented separately.
- **"Nobody optimises" is a testable anti-invariant.** Headless tests: given two providers differing by less than the switching threshold, no Household switches; given a large city over many Days, no single cycle produces a mass migration to one provider. Herd behaviour becomes a detectable regression rather than a vibe.
- **Switching must be reportable.** A Household that changed shops recorded why — the old one was starved of Goods, the commute grew — because that chain is what an inspector shows when the player asks why a Business is failing.

- **A rule this ADR owes and does not yet state: a failed Trip must demote the option that produced
  it.**
  > ⚠ **NARROWED 2026-08-13 by [`adr/0097`](0097-a-reach-failure-is-counted-on-the-citizen-and-a-stock-failure-is-not-remembered-at-all.md),
  > and half of it had been discharged since 2026-08-10 without this note knowing.** **The sentence
  > below covers two different failures.** An option can fail because it is **full** — the shelf is
  > empty, the post is taken — or because it is **unreachable**, which is the entry-error case
  > `adr/0047` was actually writing about.
  > **[`adr/0067`](0067-a-shopping-attempt-is-a-trip-and-a-household-tries-one-provider-per-occasion.md)
  > settled the stock half three days after the debt was filed**: a consecutive-failed-occasions count
  > plus a cursor that advances on failure and resets on success, skipping a provider for exactly one
  > occasion — *"a duration derived from the mechanism rather than chosen"*. **The tell that this note
  > always meant the other half is its own candidate list**, which offers a demotion, a cooldown and
  > Habit's weight, and **not a cursor**.
  > **The reachability half is now `adr/0097`** for the **job** case, where a Citizen's candidate is
  > refused by the Commute Budget after a real walk search: it is **counted on the Citizen**, resets on
  > employment, drives nothing yet, and its consumer is milestone 19's Departure. **For the *provider*
  > case this note describes, the question is void as posed** under
  > [`adr/0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) — the Provider List is unbuilt
  > with no milestone, `Scope.Pool` is a named hole that throws, no Ruleset declares a shop, and the
  > matrix is 5c's — so what survives here is **the reachability half of the provider case, and nothing
  > else**. ⚠ **And `adr/0047`'s loop is not reachable in the build**: the job pass draws candidates on a
  > key containing the **Tick**, so every occasion looks at different Buildings. The loop needs
  > *deterministic* re-evaluation, which is 5c; what is live today is the same defect as a **recurring
  > search bill** rather than as a loop.
  > *Original note follows, unchanged, because it is the record of a debt that was filed correctly and
  > read as one thing for three days.*

  [`adr/0047`](0047-routing-never-keys-on-the-district.md) names the defect precisely — this ADR
  re-evaluates *"immediately on a failed Trip"* against the same information, which still says the
  same wrong thing, so a Household can choose, fail, re-evaluate and **choose the same unreachable
  option for ever**. At S2's worst-case entry error of 77.62 Ticks that is a different Trip, not an
  estimate. The satisficing loop needs a memory of what it just tried; what that memory *is* — a
  per-Household demotion, a cooldown, or Habit's own weight moving — is unsettled, and `02 §9` wants
  the diagnostic either way. **Recorded here rather than invented**, because picking the mechanism is
  a design decision and this note is not one.

## What would trigger revisiting

- **Nothing about performance.** The decision is already O(1) per choice; if decision cost is ever the bottleneck, the levers [`0005`](0005-two-fidelity-tiers.md) names apply — sample fewer candidates, decide less often. Evaluating more options is not on the list, and a proposal to do so should be read as a proposal to reintroduce the herd.
- **Households persistently failing to notice something a real person obviously would** — a large new employment centre nobody hears about for twenty Days. That is first a discovery-rate defect; only if no rate produces both plausible awareness and stable behaviour does the model itself come into question.
- **A decision class with no incumbent to be sticky about.** A Household newly arrived in the Unplaced Pool has no Provider List at all. That case is owned by the sampled-alternatives choice in §5.4, not by this ADR — but if the two mechanisms ever produce visibly different behaviour for the same Household, they need reconciling rather than coexisting.
