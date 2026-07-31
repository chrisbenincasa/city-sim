# Household life stages and a self-generating population

**Households advance through Life Stages; Citizens still do not age.** A Household holds a stage and a countdown in Days, transitioning on the Event Wheel. Two of the transitions are **decisions** rather than schedule — how many children to have, and when to dissolve — drawn from the same discrete-choice machinery used for housing and provider choice.

The population therefore **generates itself**: Households spawn new Households, and dissolve at the end of their life. Immigration and Departure remain, but are no longer the only channels.

> **The Life Stage table defined here has become the most load-bearing data in the design, and that is worth knowing before editing it.** Six results depend on it, and every one is an argument for why a monoculture city fails:
>
> | Preference axis | What fails without it |
> |---|---|
> | dwelling size | all-high-density starves families, and therefore internal generation ([`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md)) |
> | mixed-use tolerance | universal mixed use becomes strictly optimal |
> | job-access weighting | retirement Settlements cannot form ([`0023`](0023-immigration-arrives-through-the-gate.md)) |
> | school access, rent elasticity, willingness to move | Retention, gentrification, and the internal housing market |
>
> Two things follow. **A stage supplies a base and a *range*, not a constant** — each Household draws its own position and keeps it for life, so a mis-authored midpoint shifts a distribution rather than flipping an entire class and failing several of the above at once. See [`0027`](0027-preference-is-drawn-per-household-and-persists-for-life.md). And **the width of a range is a design decision in its own right**: it encodes how much a stage agrees with itself, which is why Empty Nest is wide and Family is narrow.
>
> Also extended by [`0023`](0023-immigration-arrives-through-the-gate.md): the population no longer generates itself *alongside* an immigration channel of unbounded size. Hinterlands are finite stocks, so **internal generation is the only growth channel that survives the late game** — which is what makes housing Families structurally necessary rather than merely encouraged.

## Why

[`0010`](0010-one-clock-and-demographics-by-sorting.md) froze Household composition and argued the loss was narrative. **That was wrong, and the objection that found it is worth recording.** Freezing composition deletes a mechanical channel, not a story: **household life stage is one of the primary drivers of residential mobility in real cities.** Families form, outgrow dwellings, and empty out, and each of those is a move. Without stages, every dwelling change comes from immigration or a job change, and the city's internal housing market has no churn of its own.

Schools are the visible symptom rather than the problem. Under frozen composition, elementary capacity is a **stock** commitment consumed permanently by whoever arrived with young children — you could never grow into a school, and a seat could only be freed by someone leaving town. Real school capacity is a **flow**, serving a rolling population.

### This does not compromise one clock

`0010` implied life stages would reintroduce the two-clock problem. It would not. What one-clock rejects is a **conversion factor between two global time bases**, which is what breaks the literal truth of "commutes got long, so the shop closed." A per-Household countdown denominated in Days is an ordinary event on the Event Wheel — the same clock, just a rare event on it.

The argument that *was* sound is narrower than it was written: aging at realistic human timescales is arithmetically impossible under one clock, since eighty years at a few minutes per Day is over a thousand real hours. Compressed *stages* are not. `0010` conflated the two.

Individual Citizens still have no advancing age. Adults carry a static age drawn on formation; a child's schooling tier is derived from the Household's stage rather than from a per-Citizen counter.

### Cost

At a million Citizens there are roughly 400,000 Households. Each passes through five stages over a life spanning on the order of a thousand Days, making two or three genuine decisions in total — about **1,000 stage decisions per Day**. The same Households make shopping, work, and trip decisions *every* Day, comfortably exceeding a million per Day.

**Stage decisions are around a tenth of one percent of the decision volume already committed to.** The cost of this decision is balance, not compute.

## The stages

| Stage | Composition | Exit |
|---|---|---|
| **Young** | 1–2 adults, no children | **Decision: how many children, possibly zero.** Conditioned on housing cost, available dwelling size, job security. Zero sends the Household to Childless; otherwise to Family. |
| **Family** | adults + young children | Scheduled. Generates primary-tier school demand. Strongly mobility-averse — moving mid-schooling is costly. |
| **Mature Family** | adults + teens | Scheduled. Secondary-tier school demand. **On exit the children become adults and form new Young Households**, entering the Unplaced Pool. |
| **Childless** | adults who never had children | **Decision: when to dissolve.** |
| **Empty Nest** | adults whose children have left | **Decision: when to dissolve.** |

**Childless and Empty Nest behave identically going forward and are deliberately kept separate**, because they are different diagnoses. A large Childless population means the city is too expensive to *start* a family. A large Empty Nest population whose spawned Households all became Departures means it is too expensive to *stay* in. Same root cause, different symptom, different remedy.

### Replacement is a fact, not a constant

Children become adults and form new Households, so **two children per Household is exact Citizen replacement** — two replacing two. That threshold falls out of conservation rather than being chosen, which means it can be shown to the player as a diagnosis: *"your city averages 1.4 children per Household; replacement is 2.0."*

Dissolution removes its adult Citizens outright. No archive, no record — see [`0006`](0006-no-collection-grows-with-elapsed-time.md).

## The dynamic this creates

**Affordability drives internal generation; attractiveness drives immigration; and attractiveness raises prices.** The two levers work against each other, and a city can be dying of its own desirability while every attractiveness indicator looks excellent. Distinguishing them is what Pool size and Departure rate are for.

The stagnation spiral is real and slow: an expensive city produces zero-child draws, those Households go straight to Childless, they dissolve without replacing themselves, and the population declines even as the city remains desirable. This is the actual demography of expensive real cities and it arrives as a gradient the player can watch developing.

And one loop closes with machinery that already exists: **spawned Households enter the Unplaced Pool like any other**, so an unaffordable city fails to house its own children and they become Departures. *"You raised them and priced them out"* is a sentence the simulation can support literally, with named constituents behind it. The derived metric is **retention** — of the Households the city generated, how many found housing here.

## Consequences

- **Population is now a feedback loop and needs a damper.** The runaway is in the pleasant direction: cheap city → high fertility → more Households → more demand → still cheap if the player keeps building → exponential. The damper should be that **fertility responds to space as well as price** — a Household wanting three children needs a dwelling that fits them, and large dwellings consume land. That is a physical constraint rather than a tuning constant, which is the right kind of damper.
- **Departure and Pool reporting gain a Life Stage dimension** on top of the existing unhoused/housed and composition splits. This is the primary lens on demographic trajectory.
- **Family Households must be mobility-averse**, or school demand thrashes and the stickiness that makes neighbourhoods coherent never develops.
- **School capacity becomes a flow**, so provision must be planned against the *rate* of Households entering each stage rather than the standing count. This is a more interesting planning problem and a harder one to convey; it needs UI support.
- **Citizen count is conserved across the spawn transition** — children become the adults of the new Households — which makes the invariant testable rather than asserted.

## What would trigger revisiting

If the fertility decision proves impossible to balance — specifically, if plausible parameter sets yield only runaway or only collapse with no stable band between them. The fallback is to make fertility partly exogenous: a base rate from the world seed that city conditions modulate within bounds, rather than determining outright. That trades some causal honesty for stability and should not be taken before the balance problem is demonstrated rather than anticipated.
