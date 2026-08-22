# A market needs two sides, so 12 ships a Provider and the price moves

**Milestone 12 ships a second `[[building]]` kind — a Provider, which draws inputs from the District
Pool and sells its output into it — and the Pool price is a damped tâtonnement, per Good per District,
recomputed on a `Ticks.PerDay` boundary from Pool level against recent consumption, bounded above by
`[[hinterland]]`'s authored price per Good, which 12 also authors.**

Three things this settles by exclusion: **the anchor is exogenous** and is never derived from city
prices; **there is no haulage term at 12**, deliberately; and **there is no dependency on milestone 18**.

`EMERGENCE` `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

## Why

### A price with one side is not unobservable — it is undefined

Every shipped Ruleset's emitting kind is `dwelling`. With no Provider, **nothing sells into the Pool**,
so the Pool holds no stock and consumes nothing, and a price computed from *"Pool level against recent
consumption"* is computed from **two zeroes**. ⚠ **This is a stronger failure than the two the corpus has
already accepted this milestone.** Milestone 9's land value and this milestone's inter-District Shipments
are *built, correct, and with nothing to look at*; a one-sided market is **built and structurally unable
to produce a number that means anything.** ***A mechanism whose inputs are identically zero has not been
demonstrated by running it.***

[`adr/0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)
already made this argument in the language of the actor rather than the number: *"If a Business's inputs
arrive free down an authored ladder and its outputs vanish free into the Pool, it has **no margin, cannot
go bankrupt, and has nothing to satisfice over.**"* One kind is one side, one side is no counterparty, and
no counterparty is no margin. And [`06`](../06-roadmap.md)'s obligations table places *"a Ruleset that
models a city"* here, on the ground that **12 is the first row whose mechanism cannot run on a single-kind
Ruleset**.

### The anchor must be authored, because a ceiling derived from what it bounds bounds nothing

`CONTEXT.md` → Hinterland: *"the **one authored anchor** under every price in the design — Goods, rents
and wages all bound to it, so a designer authors four objects and **never writes a price anywhere
else**."* `adr/0050` gives the reason rather than the rule: *"An emergent price needs an anchor or it can
run away, and an unbounded price is an unbounded integer arriving at a money Bin."*

⚠ **The tempting inversion — that the outside price emerges from the city's — was raised and is
refused.** It is the runaway with an extra step: the tâtonnement's only bound would be a function of the
tâtonnement. **What the Hinterland does do is move.** `04 §4`'s open question 7 is closed — *"Should
Outside Connection prices drift? The mechanism is settled; the tuning is not"* — and `01 §5` makes a
shock *"a movement in a Hinterland's authored figures, and nothing else."* ***So the anchor is authored
and dynamic, but exogenous: it moves, and it does not move because of the city.*** The one genuine
city→Hinterland feedback in the design is the **population stock** — drawing it down *"raises its rate
and skews its mix"* — and that is labour, not prices.

### Authoring the price at 12 repairs a built mechanism rather than filling a gap

[`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md) files the absent Hinterland
price as *precondition 2*, which reads as a gap. It is worse than that.
[`adr/0045`](0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md)'s ladder is **already built and
running**, and `adr/0050` reframes it: *"local Bin → District Pool → Shipment → import → terminal is
not only a source ladder; it is a **price** ladder, monotone increasing, and `04 §4`'s 'the Outside
Connection price is a ceiling' is exactly what guarantees the monotonicity. The rungs are in that order
**because each one costs more than the last.**"* With no import price there is nothing guaranteeing it, so
**the shipped ladder is unordered** — a live defect in running code, not an absence.

⚠ **And the ceiling is `Hinterland price + haul`, not the Hinterland price alone** (`adr/0050`, quoted in
`plans/0033`). ***Haulage being priced into a ceiling is already this corpus's principle***, which is
worth recording because
[`adr/0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)
reads as introducing it. It did not; it extended it inward.

### No dependency on milestone 18, and that worry is discharged by reading

`04 §4` says *"recomputed each Day"*, and `plans/0037` decision 4 flagged a possible scheduling dependency
on **18**, the coarse Day wheel, which a parallel session is building. **There is none.** A Day boundary
is already computed in the build without a wheel — `World.cs:1073` floor-divides by `Ticks.PerDay` and
`CommuteEngine.cs:103` takes `tick.Raw % Ticks.PerDay`. 18's wheel exists so that *many* Day countdowns
share a structure ([`adr/0056`](0056-the-event-wheel-is-two-levels-ticks-and-days.md)); one recompute per
Good needs none of it. ⚠ **This cadence is not a Map Layer's and must not be added to `[layers]` by
resemblance** — `adr/0044` owns that one, and
[`adr/0134`](0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)
carries the same caution for the District's own cadence.

## Rejected

**Clear every trade at the ceiling, and defer tâtonnement.** The Pool price is a fixed fraction of the
import price; the ladder is ordered and nothing moves. Rejected because a constant price is **a price
list, not a market**: the Pool's level would feed nothing, so *"this District cannot feed itself"* has no
numeric expression, and `04 §4`'s three properties — damped, local, bounded — reduce to the third
alone. ***Shipping the bound without the thing it bounds ships the safety rail and not the road.***

**Derive the Hinterland price from city prices.** Circular, and it is `adr/0050`'s runaway. See above.

**Ship the haulage charge at 12.** `adr/0133`'s payee is unsolved — a cost with no counterparty destroys
money and [`adr/0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) forbids it —
and the collapse `adr/0133` guards is a **scale** risk that one District over a small city does not reach.
⚠ **This is a deliberate *no* and not an omission**, which is [`adr/0117`](0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md)'s
lesson applied on the day rather than a milestone later. **Trigger: city size, or the payee resolving.**

**Put the Provider's output on the `dwelling` kind.** The shortcut that would avoid a second
`[[zone_rule]]`. Refused because it makes every dwelling a factory and deletes the ownership boundary the
milestone exists to cross.

## Consequences

- 🔴 **A second kind costs three content decisions, and `rulesets/minimal.toml`'s own header already
  enumerated them**: *"A second kind needs **a second `[[zone_rule]]`** to raise it, **a second decline
  Rule** so that it churns rather than accumulating until the city is all offices, and **a land-use
  split**."* ⚠ **All three are content, and that header's first line is that the file does not make
  content decisions.** So the Provider does not go in `minimal.toml`; it needs a Ruleset of its own, and
  that file is the same one `plans/0037`'s Definition of done already owes. ***The cheapest reading of
  this milestone was one that never counted the Ruleset.***
- **`[[hinterland]]` gains `price` per Good**, under [`adr/0131`](0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md)'s
  rule that a Hinterland field is authored in the milestone that reads it. 12 is the reader.
- 🔴 **Two or three more hash-bearing numbers, unset, owed `plans/0002` §D2 rows**: the **damping factor**,
  the **per-Day move cap**, and an **initial price** if the tâtonnement needs a seed. They join
  `adr/0134`'s four. ⚠ **Unlike those four, these are tunable as soon as the Provider exists** — a
  two-sided market on one District produces a moving price, so the world that ratifies them is 12's own
  demonstration Ruleset rather than milestone 15's.
- **A new per-`(District, Good)` accumulator** for recent consumption. With one District that is one row
  per Good, so the cost is not the storage — it is that **a rate needs a window**, and the window length
  is one of the numbers above.
- **The ladder's Shipment rung never fires at 12**, because `adr/0134` gives one District per world and
  there is nowhere to ship from. Not a defect; it is what the world contains.
- **`04 §4`'s *"two Districts of the same city can have different Food prices, and the gap is what makes
  inter-District Shipments profitable"* stays unobservable** until the two-settlement Ruleset exists —
  the same bullet, doing a third job.

## What would trigger revisiting

- **The tâtonnement oscillating despite damping.** `04 §4` predicts the pathology it damps against —
  *"everyone piles into the profitable Good, it crashes, everyone piles out"* — and if damping is not
  enough, the loop is wrong rather than the constant.
- **The Provider kind filling the city.** `minimal.toml`'s header names this exactly: without a decline
  Rule a second kind accumulates *"until the city is all offices."* If it happens with one, the churn
  mechanism is wrong.
- **The price never leaving the ceiling.** If local supply never undercuts import, the Pool is decorative
  and the ladder has two live rungs rather than three. That would mean production is too slow or the
  ceiling too low, and it is measurable on 12's own Ruleset.
- **A payee arriving for `adr/0133`'s charge.** Then the *no haulage term* above is re-taken, not
  inherited.
