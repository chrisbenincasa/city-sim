# Money is conserved, and the city has a balance of payments

**Money is a conserved stock that flows without transport.** It is never created or destroyed inside the city. It is held by Households, by Businesses, and by the treasury, and it moves between them through wages, purchases, rents, and taxes. **The Outside Connection is its only source and sink** — money enters as payment for exports and leaves as payment for imports.

Money is therefore *conserved like a Good* and *not a Good*: it needs no Shipment, occupies no Vehicle, and contributes no congestion.

## What forced the question

An Office Building employs educated Citizens and produces no Good. It had to output *something*, and both obvious answers were bad:

**A sixth Good.** Forbidden by [`04-economy-and-goods.md`](../04-economy-and-goods.md)'s own resource discipline — *"if a sixth Good is ever added it should replace something rather than extend the list."* Worse, its physical movement would be a fiction: nobody trucks consulting hours to a warehouse, so it would be a lie inside the one system that document insists must be conserved and auditable.

**Money from nothing.** This would have created the first Building in the design whose output is unconserved — money appearing where no Bin was drained. Not merely an auditing weakness: it makes an entire class of economic bug, runaway income, **invisible to the conservation checks that catch it everywhere else**.

The resolution was that the second answer was never wrong; it was incomplete. **Office does not create money, it earns it from outside.** The Outside Connection is already defined as the thing that *"absorbs surplus Goods and supplies deficits, at a price."* Make it the counterparty for money and the accounting closes with no sixth Good and nothing unconserved.

## Why conservation is worth the cost

- **"Where did the money go" becomes answerable**, which makes `Evidence` applicable to the economy rather than only to people and Goods.
- **Bankruptcy becomes a distinct diagnosis from input starvation.** A Business with empty Bins and a Business with an empty balance are different failures with different remedies; without balances they are the same event.
- **Inequality becomes emergent and spatial** rather than a statistic. Money accumulates unevenly because wages and prices differ, and the map shows it.

It is the circular flow, modelled literally: businesses pay wages, Households consume, consumption funds businesses, taxes divert a share to the treasury, and the treasury spends it back.

## The balance of payments is the endgame

This is the part that pays for itself, because it makes [`0022`](0022-land-is-a-stock-the-city-spends.md)'s late game mechanical rather than narrative.

| Flow | Direction | Scales with |
|---|---|---|
| Food imports | out | population |
| Materials imports | out | ambition |
| Service exports | **in** | educated labour |

`0022` commits to a mature city that permanently imports Food and Materials, and warned that without new levers *"the late game is nothing but bills."* Under conserved money that is no longer a bill paid from taxes on the same population generating it — it is a **structural trade deficit**. A city whose land is sealed and whose schools were never built imports everything, exports nothing, and watches its money supply drain.

The player can attack that in three different places — reduce imports, build the export sector, or borrow — and can see which one is failing.

**Borrowing is the damper.** A conserved money system with a net drain can seize, which is the classic failure of closed economic simulations. The `Fund` verb already includes *borrow*, so a deficit becomes a debt burden rather than a stop. Gradient, never a wall — the same shape as parking sheds in [`0009`](0009-parking-is-modelled-supply-never-search.md) and Timber hauls in [`0022`](0022-land-is-a-stock-the-city-spends.md).

> **Clarified by [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md): the damper must be *reached for*.** *"A deficit becomes a debt burden"* reads as an automatic overdraft, and an automatic one deletes a decision the player should be making. **Borrowing is a player action.** The treasury therefore genuinely empties, and what happens then is not a seizure but ordinary [`0033`](0033-two-rule-families-scheduled-and-swept.md) behaviour — the Rules that could not draw **wait**. The gradient claim survives intact; what changes is that the player chooses when to apply it, and declining to is a supported outcome with visible consequences. `PLAYER GOVERNS` `NO VERDICT`

## Velocity is emergent, and hoarding is bounded

The second classic failure is money pooling in Households until the loop starves. No velocity constant is authored.

A Household satisfices under [`0017`](0017-agents-satisfice-they-never-optimise.md): it buys Food because Sustenance demands it, holds a reserve sized by its **Life Stage** — a Young Household saving toward forming a family needs a deeper buffer than an Empty Nest — and spends the remainder on Consumer Goods, which is the Satisfaction Need already in the model. **Saving has a purpose and therefore a ceiling.** Wealthier Households spend more absolutely and save a larger fraction, which gives a marginal propensity to consume without anyone authoring one.

## Poverty is an absorbing state, and that is left standing

A Household at zero money cannot buy Food, cannot afford to move, and cannot reach a job requiring a car it cannot buy. **Every exit costs money it does not have.**

This is not corrected by the simulation and not prevented by it. It emerges from conservation and it is left in place, because the alternative is a simulation that quietly refuses to model something real. What the design commits to is that it is **visible and traceable** — `HONEST DEGRADATION` requires the trap be legible, not that it be solvable.

**There are five exits, and only one is a social program.** Destitution is a reachability failure wearing a money costume — the Household has no money *because* it cannot reach work — so most of the remedies are spatial: it downsizes into cheaper stock, transport reaches it, jobs are zoned near it, or its children escape through education a generation later. Transfers are the fifth, and their mechanical function is not to fix reachability but to **restore agency**: a Household at zero cannot act on any option, so money is what makes the other four available to the people who need them.

This matters for the design's neutrality. Had welfare been the *only* exit, declining to build it would not have been a choice — it would have been a wrong answer, and the game would have been prescriptive by omission.

What happens next is therefore a **Policy** decision among several. The player may spend the city's coffers to raise the floor, may fix it with buses and zoning, or may decline and let disparity express itself spatially. All are supported outcomes and the game takes no position on which is correct. Recorded explicitly because this is the most politically loaded mechanic in the design, and the temptation to encode a preferred answer — an advisor recommending welfare, or a score penalising inequality — should be recognised as a departure from `PLAYER GOVERNS` rather than a refinement of it.

## Consequences

- **Businesses hold money.** Closes the open question of whether they hold only Bins. They hold a balance; they do not hold Needs. *Business as a full economic actor with preferences is explicitly out of scope.*
- **Wages become a price, and this is no longer optional.** [`04-economy-and-goods.md`](../04-economy-and-goods.md) warns that two coupled price systems is where these models become unstable. Conserved money makes the second one mandatory, so the labour-market question is promoted from a fork to scheduled work. **This is the largest known risk this ADR creates.**
- **Every actor needs a balance**, which is one integer per Household and per Business — trivial against records that already exist.
- **Money moves instantly.** The conserved/transported distinction must stay sharp, or someone will eventually simulate armoured trucks. Goods move on the Road Graph; money does not.
- **The treasury is a participant, not an oracle.** Taxes are a diversion from a real flow, so a tax rate change has a traceable path to a specific Household's balance.
- **Save size grows by one integer per actor.** Immaterial.

## What would trigger revisiting

- **Deadlock in balance testing that borrowing does not damp** — cities seizing rather than declining. The first response is to examine the savings buffer, since that is where velocity is set, before relaxing conservation.
- **The wage system proving unstable when coupled to Goods prices.** This is the predicted failure. If it happens, the honest options are a fixed wage schedule by education tier or Ruleset-damped adjustment — not abandoning conservation, which is doing separate work.
- **Playtesting showing the trade balance is invisible** despite being the endgame's central pressure. A UI failure first, per the diagnosis order [`0022`](0022-land-is-a-stock-the-city-spends.md) establishes.
