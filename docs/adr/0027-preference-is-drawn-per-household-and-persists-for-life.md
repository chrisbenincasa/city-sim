# Preference is drawn per Household and persists for life

**A Life Stage supplies a base and a *range*; each Household draws its own position within it.** Preference coefficients — quiet against variety, space against centrality, rent against commute — are not constants shared by every Household in a stage. They are private, drawn at formation from the Household's own seeded stream, and **they persist for life**: when a Household changes Life Stage the base and range move, the position within them does not.

Someone who always valued quiet still values quiet once they have children.

## What forced it: a disagreement that was not one

The question was whether Empty Nest Households prefer mixed-use centres or quiet periphery. One argument said centres — services within reach, walkability, noise no longer a concern. The other said periphery — people move away from the bustle.

**Both are correct, and that is a range rather than a disagreement.** Some retirees downsize into walkable town centres; others leave for somewhere quiet. Under stage constants one of those has to lose, and the map only ever produces the winning behaviour. Under stage-constrained draws the map produces **both**, and the retirement Settlements [`0023`](0023-immigration-arrives-through-the-gate.md) predicts emerge alongside downtown Empty Nests.

The disagreement was the evidence. A design question that two reasonable arguments answer oppositely, with both describing real behaviour, is usually a question about a *distribution* being mistaken for a question about a *value*.

## Why the choice model's random component is not already this

The logit already produces variation through its error term. That is not a substitute, and the difference is the entire point:

| | Re-drawn | Effect on a Household |
|---|---|---|
| **`ε`** | every decision | randomly **inconsistent** — it chooses differently each time for no reason |
| **Taste** | once, at formation | consistently **itself** — it has a character |

**A Pinned family that reliably chooses quiet neighbourhoods is a character. One that chooses differently every time is noise wearing a name.** `Pin` exists to deliver long-term attachment to individuals, and attachment requires the individual to be predictable enough to *recognise*. `UNIQUE INDIVIDUALS`

Statistically this moves variance from **unexplained** (`ε`) to **explained** (a Household attribute). Total variance should stay roughly similar, so **`μ` needs retuning rather than leaving alone** — otherwise the same variation is being introduced twice, and the city will read as more random than intended.

## It is the same move [`0017`](0017-agents-satisfice-they-never-optimise.md) already made, one layer over

That ADR gave *knowledge* a cause: *"two Households with identical attributes know different providers, because they have different histories. The variation has a cause, not just a random seed."*

This gives *taste* a cause on identical terms. Together they mean two Households that look the same in every modelled attribute differ in **what they know** and in **what they want**, both persistently and both inspectable. [`0005`](0005-two-fidelity-tiers.md) asserted that identical Households must choose differently; these two ADRs are what make it mechanically true rather than an appeal to noise.

## Range width is as expressive as the midpoint

The spread encodes **how much a stage agrees with itself**, and that is a real design lever rather than a tuning detail:

| Stage | Spread on the quiet-versus-variety axis | Why |
|---|---|---|
| **Empty Nest** | widest | real retirement choices genuinely diverge |
| **Family** | narrow | schools matter to nearly all of them |

A stage with a narrow range behaves predictably in aggregate. A wide one produces divergent behaviour from identical circumstances. Authoring a range is therefore authoring *how much a demographic is a demographic*.

## Why this reduces risk rather than adding it

Six results now depend on the Life Stage preference table, and every one of them is an anti-monoculture argument:

- dwelling-size preference — why all-high-density fails ([`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md))
- mixed-use tolerance — why universal mixed use fails
- job-access weighting — why retirement Settlements can exist at all ([`0023`](0023-immigration-arrives-through-the-gate.md))
- school access, rent elasticity, willingness to move — Retention, gentrification, and the internal housing market ([`0011`](0011-household-life-stages-and-self-generating-population.md))

That is a heavy concentration on one unauthored table, and it was a concern raised against this design before the draws were proposed. **Ranges are the mitigation, not the risk.** With point constants, a mis-authored midpoint flips an entire class of Household at once, and several anti-monoculture results fail simultaneously for the same invisible reason. With ranges, the same error shifts a *distribution* — the results degrade smoothly and partially rather than together and completely.

## Consequences

- **`Evidence` must expose a Household's own preferences**, or an odd choice is inexplicable. *"This family weights quiet unusually low"* is the difference between an individual and a bug. This is the same requirement [`0022`](0022-land-is-a-stock-the-city-spends.md) placed on farm yields — a figure that cannot decompose itself is not legible.
- **Storage is a few scalars per Household, not a full vector.** Tastes modulate a shared stage table rather than replacing it. Immaterial against records that already exist.
- **No new determinism cost.** `02-simulation-model.md` §5.8 already makes per-agent seeded RNG streams a hard requirement, precisely so iteration order and parallelism cannot change outcomes.
- **Taste survives Life Stage transitions**, which means a Household has continuity of character across its whole life. This is what makes a long-Pinned Household feel like a person rather than a slot.
- **Households only, for now.** Businesses satisfice on margin and need no equivalent; a Business with private tastes would be a second actor model to balance for no identified gain.

## What would trigger revisiting

- **Ranges so wide that stages stop being distinguishable in aggregate.** The point of a stage is that it produces a *trend*; if Family and Childless Households behave indistinguishably at the population level, the ranges have eaten the mechanism they were meant to soften. This is the failure to watch for, and it is quiet.
- **Balance testing showing distributions cannot be reasoned about** — designers unable to predict the effect of a change because every outcome is a spread. The recovery is narrower ranges, not constants.
- **Preference drift proving wanted** — Households whose tastes change with experience rather than only with stage. That is additive to this decision rather than in tension with it, but it would need a cause, and "people change" is not one.
