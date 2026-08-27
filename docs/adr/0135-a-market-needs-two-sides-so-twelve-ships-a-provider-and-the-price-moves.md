# A market needs two sides, so 12 ships a Provider and the price moves

> ⚠ **AMENDED 2026-08-22 by [`0139`](0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md).** ***A market needs two sides is exactly right and this record understated its own claim.*** Read every *sells its output **into** the Pool* below as *offers its output **on** the market*: a `pool` output deposits into the **selling Building's own Bin** and marks the stock as for sale, and a buyer's `pool` input resolves to **one seller's Bin**. **The Pool holds no stock.** The price this record moves now sits on the **seller**, opening at the import ceiling; `[market]`'s two keys are untouched. 🔴 ⚠ **THE CLAUSE SAYING THE DAMPING ARGUMENT IS UNTOUCHED IS WITHDRAWN, 2026-08-26 by milestone 26 task 8.** The damping argument is *from Pool level against recent consumption*, and `0139` removed the Pool's stock — so it deleted one of the two inputs while assuring the reader it had not. ***Measured: the price has never moved on any world*** — eight rows on `rulesets/provisioned.toml`, zero changes over 24,576 Ticks. This record's own title claim, *the price moves*, was **unbuilt rather than wrong**. ✅ **BUILT 2026-08-26 by [`0171`](0171-a-markets-level-is-what-its-sellers-hold-and-the-price-divides-by-the-sum-while-a-wake-spends-the-maximum.md)**: cover is the sum over the row's sellers, and `rulesets/oversupplied.toml` goes **100 → 58** with 11 changes across eight rows. ⚠ **`decay_percent` and `move_cap_percent` still ratify nothing** and `plans/0002` §D1 still holds both — but the reason has changed for the second time, and this is the version to quote: they were inert because `Scope.Pool` threw, then inert because the cover was structurally zero, and they are now **live and unmeasured**. ***Two of those three states print the same flat column***, which is why `--market` prints `stock` and `rate/Day` beside the price. `plans/0044` **F50**, **F57**.

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
  ✅ **SHIPPED 2026-08-22, milestone 12 task 6, spelled `prices`** — an inline array of tables on
  `[[building]] bins`' idiom, one entry per Good. ⚠ **THE CEILING IS THE MINIMUM ACROSS THE HINTERLANDS
  AND THAT IS DERIVED RATHER THAN CHOSEN**, which this ADR left open by not asking: *no haulage term at
  12* means importing from the far edge costs what importing from the near one does, so ***with carriage
  free there is nothing to choose between four gates***. `Ruleset.ImportCeiling` is the `min`;
  `Ruleset.HinterlandPrices` keeps the per-edge figures, because they are what the future
  per-District `min(price + haul)` will be a minimum **over** and because four comparable markets being
  each other's referent is `CONTEXT.md`'s reason for the object existing.
  ⚠ **And a file that states `[districts]` and leaves a `good` unpriced is now REFUSED AT LOAD**, which
  this ADR's argument implies and does not state: a Pool with no ceiling is not unanchored, ***it is free
  everywhere for ever***.
- 🔴 **Two or three more hash-bearing numbers, unset, owed `plans/0002` §D2 rows**: the **damping factor**,
  the **per-Day move cap**, and an **initial price** if the tâtonnement needs a seed. They join
  `adr/0134`'s four. ⚠ **Unlike those four, these are tunable as soon as the Provider exists** — a
  two-sided market on one District produces a moving price, so the world that ratifies them is 12's own
  demonstration Ruleset rather than milestone 15's.
  ✅ **IT IS TWO, AND THE THIRD IS NOT DEFERRED — IT DOES NOT EXIST.** `[market] decay_percent` and
  `move_cap_percent` shipped as **§D1** rows, in use and unratified, on `rulesets/twinned.toml`. There is
  **no seed**: a Pool opens at `Ruleset.ImportCeiling` and moves from there, and a Pool nobody has traded
  in stays there — which keeps [`adr/0045`](0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md)'s
  ladder monotone from Tick 0 without anybody choosing anything. ***A Pool with no local supply in it
  should cost what importing costs, and a Pool nobody has traded in has no local supply by
  construction***, so the seed is not a choice — it is the answer the mechanism gives when asked before
  anything has happened. §D1 carries a **struck** row saying so, because otherwise the next reader adds a
  `[market] initial_price` and a §D row for a key nothing needed.
  🔴 **A THIRD number was found in the arithmetic rather than in the design, and it is a RESOLUTION and
  not a value.** The consumption rate is an integer in units per Day, and **flooring** the moving average
  made any draw below `100 / (100 - decay_percent)` units a Day fold to a rate of **zero** — which the
  recompute reads as *no trades* and answers by freezing the price. ⚠ **The threshold moves with the
  damping**, so retuning `decay_percent` would silently change which markets have prices at all: this
  ADR's own *knob that switches the mechanism off* pattern arriving **inside an expression, where no
  load-time refusal can reach it**. Repaired by rounding, at the cost that a rate of 1 never decays back
  to 0. Whether one unit a Day is fine enough is *measurable* and is filed on the `decay_percent` row.
- **A new per-`(District, Good)` accumulator** for recent consumption. With one District that is one row
  per Good, so the cost is not the storage — it is that **a rate needs a window**, and the window length
  is one of the numbers above.
  ✅ **SHIPPED as TWO columns on `DistrictPoolTable` rather than one**, plus the price itself: `Price`,
  `Rate` — the smoothed figure, in units per Day — and `Consumed`, the current Day's bucket. ⚠ **The
  split is deliberate**: one accumulator that decayed in place would hold a rate multiplied by a constant
  nobody could name the units of, and ***a number nobody can name the units of is a caveat that has
  already come off its digits***. All three went on `DistrictPools` because they are keyed by
  `(District, Good)`, which is that table's row identity exactly — ***a fact keyed by a row that already
  exists belongs on that row.***
  🔴 **NOTHING WRITES `Consumed` AND THE WHOLE MARKET IS THEREFORE INERT.** `Scope.Pool` still throws
  after task 6, so every rate is zero, every recompute reads *no trades*, and every price sits at its
  ceiling for the length of any run. **Task 7 is the writer.** ⚠ **That makes this the third producer
  this corpus has shipped without a consumer** — milestone 9's land value and task 5's Pool Bins — and it
  is accepted for the reason it was the first two: a purchase settling at a price needs the price to
  already exist.
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
