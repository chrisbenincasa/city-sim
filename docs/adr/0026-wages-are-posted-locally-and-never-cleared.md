# Wages are posted locally and never cleared

**There is no labour market clearing.** Each Business posts a wage and adjusts it by its own fill rate — raise it when vacancies persist, let it fall when applicants queue. No global solve, no iteration, no equilibrium. **The Hinterland wage is the anchor**, exactly as the import price already anchors Goods, and thin markets shrink toward it in proportion to how few workers they hold.

Wages move in **both** directions, which is the part that pays for itself: a Business nobody can reach must pay a premium, and a well-connected one need not. **The wage surface is therefore a readout of accessibility**, and a transport failure becomes visible in money to a player who never opens a traffic overlay.

## What forced this, and the miscount found on opening it

[`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) made Money conserved, so it must reach Households through a channel. Wages are that channel, and a wage is a price.

The ledger recorded this as adding a *second* price system. That was wrong — there are already two, in different documents, which is presumably why nobody noticed:

| System | Where | Mechanism | Anchor |
|---|---|---|---|
| Housing | [`02-simulation-model.md`](../02-simulation-model.md) §5.6 | tâtonnement over submarkets, clamped ±10% | — |
| Goods | [`04-economy-and-goods.md`](../04-economy-and-goods.md) §4 | damped tâtonnement, Pool level vs consumption, per-District | **import price is a ceiling** |

**And the real risk is not a third market.** Housing prices and Goods prices are currently almost independent — they touch only weakly, through Household budgets. Wages couple to *both* ends at once: they are Business cost feeding Goods prices, and Household income feeding housing demand. **Wages are the edge that closes three loosely-related markets into one circuit**, and that circuit has a name and a reputation: the wage–price spiral.

Everything below is chosen to keep that circuit damped by construction rather than by tuning.

## Why posted, and not the two obvious alternatives

**A Ruleset schedule** — wage as a function of job type and education tier — is stable and trivially legible, but it cannot respond to scarcity. A city with no available workers would have no way to bid for them, so a labour shortage would produce silent failure instead of a price signal. Wrong failure mode for a design whose premise is that pressure is visible before it is fatal.

**A tâtonnement over labour submarkets** would be symmetric with the two existing systems and need no new explanation. It is rejected because it is a third *global clearing loop* placed at exactly the point that closes the circuit — the most dangerous possible location for an iterative solver in this design.

**Posted wages are [`0017`](0017-agents-satisfice-they-never-optimise.md) applied to employers**, and `04-economy-and-goods.md` §4 already committed to it in another context: *"A Business does not solve for optimal output; it notices its margin is bad and considers a small number of known alternatives."* Wage adjustment is that sentence with a different noun. The coupling stays local and therefore damped — a wage rise propagates one Business at a time through actual hiring, not instantly through a solved equilibrium. **The spiral has to walk.**

It also produces wage *dispersion* for free: identical Businesses pay differently because they have different hiring histories, which is the same shape as Provider Lists producing different choices for identical Households, and it is where spatial inequality acquires a cause.

## The anchor, and what to do about thin markets

Every price system in this design now anchors to the same authored object — the **Hinterland**, per [`0023`](0023-immigration-arrives-through-the-gate.md), which carries a median rent and a median wage per map edge in domain units:

- Goods ← bounded by the Outside Connection import price
- Rents ← bounded by Hinterland rent; price past it and people stop coming, then start leaving
- **Wages ← bounded by Hinterland wage**; below it nobody accepts the job

Damping handles *volatility*; it does nothing for *thinness*. A forty-worker submarket damped is still a forty-worker submarket, and wage signals computed from forty agents twitch in a way that reads as the simulation malfunctioning.

**The fix is shrinkage toward the anchor, weighted by pool size.** Small pool → mostly anchor. Large pool → mostly local signal. Self-correcting as the city grows, with no threshold and no `if small city then` anywhere.

What makes this a mechanism rather than a numerical hack is that **it is true**: a city with forty tier-3 workers genuinely *is* wage-anchored to the outside, because the outside option dominates a market that thin.

Explicitly rejected: *calculate less often*. Accumulating several Days and then adjusting yields jumpy-but-rare rather than smooth, which is worse for legibility than continuous drift.

## A labour-starved Business has two levers, not one

The first draft of this decision had only *raise the wage*, which walks a Business up to bankruptcy and makes business death the headline symptom of a labour shortage. The second lever already exists in §5.9: **decline**.

Under [`0017`](0017-agents-satisfice-they-never-optimise.md) this is one satisficing rule with two known alternatives — *margin is bad → pay more, or be smaller* — and which one a Business picks falls out of its margin:

- **Fat-margin Businesses pay up** and win scarce labour.
- **Thin-margin Businesses contract** or relocate.

That is agglomeration sorting emerging from a two-option rule. And it corrects the symptom: **a labour-starved district does not watch its businesses die, it watches them get smaller.** Employment capacity contracts to match accessibility, which is what happens in real isolated places. Bankruptcy remains, as the tail case.

## The Commute Budget: universal cost, individual compensation

Wages and travel time are substitutes, so the Budget cannot be a flat constant — but per-Household budgets would break the rule `CONTEXT.md` treats as sacred, that *"the cost function must be the same quantity the player is scored on."*

The resolution is a distinction rather than a compromise. **The cost is universal; the compensation is individual.**

Every Household evaluates commute with the *same* disutility curve and the *same* hard ceiling. What differs is what they are being paid to endure it. Both halves are already specified in §5.4 and neither is new:

| Behaviour | Existing mechanism |
|---|---|
| longer commutes become disproportionately distasteful | commute as a utility term → the logit decays acceptance exponentially in time |
| offset by wages | wage as a competing utility term |
| a limit past which no wage suffices | the Commute Budget as a **filter**, applied before scoring |

Negative-exponential deterrence is also the standard form in the gravity-model literature already catalogued in `references.md` §1, so the shape is not invented.

Consequence: **the labour market partially clears through price and never fully clears through it.** Vacancies and unemployment coexist — spatial *mismatch* — and money stretches the shed without escaping it. Transport remains the only thing that genuinely fixes mismatch, which is the correct conclusion for this game to reach.

The overlay must **not** weight volume by cost. An uncongested Segment where everyone drives forever is sprawl; a jammed Segment where every trip is short is density outrunning capacity. Weighted volume gives both the same colour. Segment Stress stays a raw count of the network — `CONTEXT.md` notes its trustworthiness comes from consuming *a count, not a model* — and commute burden is reported separately, as a property of the population.

## Skill, and the ladder the city cannot skip

**Three tiers.** Jobs specify a **minimum**, not a match, so an over-qualified worker can take a lower job — which makes underemployment measurable, and it is the leak detector [`0010`](0010-one-clock-and-demographics-by-sorting.md) needs: *"34% of your advanced-tier workforce is in basic-tier jobs"* says education spending is not landing. A fourth tier is parked in [`deferred.md`](../deferred.md) with a low retrofit cost.

**Experience carries tier 1 → 2. Only schooling reaches tier 3.** Implemented as an Event Wheel countdown scheduled when a Citizen takes a job — the same machinery [`0011`](0011-household-life-stages-and-self-generating-population.md) uses for Life Stages, so no Citizen ages and no counter ticks.

The constraint is not flavour. Tier 3 staffs Office, Office earns exports, and exports pay the Food and Materials bill that [`0022`](0022-land-is-a-stock-the-city-spends.md) makes the endgame. **If experience reached tier 3, the endgame's revenue engine would have a bypass requiring the player to have built nothing.** So the tier-3 workforce comes only from schools or immigration — [`0010`](0010-one-clock-and-demographics-by-sorting.md)'s two demographic engines pulling against each other on a concrete axis.

**Employers require a mix, not a tier.** A tier-3 employer needs tier-2 support; a factory needs tier-2 supervision over tier-1 labour. Since tier 2 is produced *by tier-1 jobs*, **the city cannot skip stages** — an all-Office city is structurally impossible, because the export sector depends on having had an industrial base. This is the same anti-monoculture shape as Hinterland exhaustion forcing a density mix in [`0025`](0025-density-is-a-cap-and-it-trades-land-for-materials.md), arrived at independently. It is also where the depth a fourth tier was wanted for actually comes from, at no extra market.

**Job search is sticky.** `CONTEXT.md`'s Provider List already reads *"a short, sticky set of known shops, workplaces, and services"* — workplaces are in the sentence. A Household switches jobs only when a *known* alternative is *substantially* better. This is load-bearing twice over: churn would reset promotion countdowns and silently delete tier 2, and a frictionless market would re-sort workers instantly and dissolve the spatial mismatch that makes the map matter.

## Public employment cannot absorb unemployment

Service Buildings employ Citizens, and under conserved Money the treasury pays them — so service funding has a fiscal multiplier, and austerity in a struggling city makes it more struggling by a traceable chain.

The obvious exploit is to fix any labour shortage by raising service funding. It is blocked **structurally, not punitively**: public jobs are **demand-determined**. A school needs teachers in proportion to the children in its catchment. *You cannot fix unemployment by hiring everyone as a teacher, because the number of teachers is set by the number of children.*

Public pay lags private pay, and understaffing degrades service quality **proportionally** — a school 15% understaffed is 15% worse, visible and purchasable. An earlier draft made a boom cause service collapse and thereby decline; that was rejected under `NO VERDICT`, because it admitted exactly one outcome and was therefore an argument rather than a mechanism. **Booms already decay by spending their own preconditions** — Hinterland drawdown, rising rents, sealed land, rising Materials costs — and needed no fifth mechanism that punishes.

A boom is deliberately **not named in the interface**. Per the rule settled here: *announce what the city cannot show; never announce what it already shows.* A boom has a visual signature — scaffolding, arrivals queuing at the gate, Segments going Microscopic — so labelling it adds nothing and converts an observation into a goal.

## Consequences

- **Taxes become a velocity control, not only a revenue one.** They are the sole private→public conversion, moving money from Households who were saving to a treasury that spends immediately. A tax rise is simultaneously contractionary and expansionary, and which dominates depends on who was taxed. `04-economy-and-goods.md` §5 needs a third second-order entry.
- **Transit acquires an economic argument it previously lacked.** It widens labour sheds, which lets employers cut wages, which improves margins, which supports employment. Its payoff becomes measurable in the same units as everything else, and it is a **destitution exit** — infrastructure as anti-poverty policy. This changes the case in `00-vision.md`'s open question.
- **Three emergent results to protect in balance testing**, because each is the kind of thing a tuner deletes without knowing it was wanted: **brain drain** (underemployed tier-2 workers leave for a Hinterland paying their tier), **job stability produces skill** (churn resets promotions, so Retention is no longer purely demographic), and **compensating wage differentials** (isolated employers pay more).
- **Vacancy panels must decompose by reason.** *"130 vacancies. 1,610 excluded — commute exceeds budget. 210 considered, chose elsewhere: 190 for higher wage."* Without the last line, *"nobody can reach you"* and *"everyone who can reach you chose someone else"* are indistinguishable, and they have opposite remedies.
- **An industrial closure produces a traceable cluster of destitution.** Sticky search plus a hard commute filter means a Household whose workplace closes had chosen its home, and its known alternatives, relative to a job that no longer exists. The sharpest available illustration of `BOUNDED KNOWLEDGE`, and it needs watching in playtest for whether it reads as consequence or as punishment.

## What would trigger revisiting

- **The circuit oscillating despite local adjustment** — wages and prices chasing each other citywide. First response is the shrinkage weight and the per-Day adjustment bound, not a return to a clearing solve.
- **Posted wages producing a market that visibly never clears** in a way players read as broken rather than as mismatch. That is a diagnosis-surface failure first: if the vacancy panel cannot distinguish unreachable from outbid, fix that before touching the mechanism.
- **Tier 2 failing to materialise** because job churn outpaces promotion countdowns. That would mean stickiness is tuned too loose, and it deletes the skill ladder silently rather than loudly — worth an explicit test rather than observation.

---

## Superseding note — session five: the tier wall is a category boundary, and experience is continuous beneath it

**The wall stands, and its justification changes.** This ADR enforced *"experience carries 1 → 2; only schooling reaches 3"* on the grounds that it **protects the education → Office → exports chain from a bypass** — a balance argument, and therefore a wall, in a design whose most-repeated rule is that *scarcity is a gradient, never a wall*. The rule was challenged on exactly those grounds and survived, for a better reason:

> **Tiers 1 and 2 are separated by learnable skill. Tiers 2 and 3 are separated by a credential.**

An apprentice becomes a technician by doing the work; a technician does not become an analyst by staying longer. The boundaries are not the same kind of boundary, which makes the asymmetry self-explaining rather than tuned — and it is the one place in this design where *gradient, never a wall* correctly does not apply, because **a category is not a quantity.**

**Within a tier, experience is continuous with a ceiling.** A Citizen accumulates experience on the job and becomes more valuable, up to the band's limit and never past it. This does **not** thin the labour market: thinness is a property of the number of market *segments*, and `jobs specify a minimum, not a match` keeps those at three. A within-tier attribute splits no pool.

**It is the design's only source of productivity growth.** Without it a city of 10,000 produces exactly the same on Day 100 and Day 5,000 — the simulation has an extensive growth margin and no intensive one. It also softens a fragility named above: churn currently **resets a countdown** and deletes tier 2, a hard cliff. Against a continuous accumulator, churn costs smoothly.

**Experience raises output, not the wage.** The wage rise is a *consequence* — this ADR posts wages by fill rate, so a Business with fatter margins from productive workers is exactly the one that bids up when it cannot fill a vacancy. The distinction is not cosmetic under [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md): money is conserved, so a wage rise with no output rise is pure redistribution and pure inflation.

## The city has no central bank, and that is correct

The wage-price spiral flagged above has no monetary remedy here, because **a city has no monetary sovereignty.** The lending rate is set by the Intensity Dial (`01 §5.4`), which by `§5.5` acts only outside the map — so interest rates are a fact about the world, not a lever. Cities do not set them.

What damps the circuit instead is what damps a real small open economy: **external anchors**, and this design already has three.

| Stabiliser | Mechanism | Already in |
|---|---|---|
| **Import competition** | local price above Hinterland price + haul → buyers import. The gate is a **price ceiling** | `04 §1` |
| **Labour supply** | local wage above the Hinterland wage → more willing arrivals → pool grows → wages damp | [`0023`](0023-immigration-arrives-through-the-gate.md) |
| **Business exit** | compressed margins → shrink or leave | `02 §5.9` |

This ADR's own finding — *"the Hinterland wage anchors it, as import price already anchors Goods; all three markets anchor to the same authored object"* — is that object doing central-bank duty, through trade and migration rather than credit.

**Both wage growth and output growth occur, and they are distinguishable.** A tight labour market raises wages without output: inflation, self-limiting, no growth. An experienced workforce raises output first: real growth. Identical wage movement, two causes, and **the discriminator is whether output moved** — readable directly off the balance of payments, with nothing announced. The city's only macro lever remains **tax**, already named a velocity control above, which is the only kind of macro lever a city actually has.
