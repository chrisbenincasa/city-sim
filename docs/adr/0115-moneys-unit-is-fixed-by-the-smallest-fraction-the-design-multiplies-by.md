# Money's unit is fixed by the smallest fraction the design multiplies by

**There is no money unit to choose and no Ruleset key to author. The unit is fixed by the smallest fraction the design multiplies money by — and that fraction is already chosen at 10%, in two places — so any money quantity a fraction is ever taken of must be at least ten smallest units to move at all.** [`0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md)'s *"sizing decision owed before the first priced Ruleset"* is therefore a **scale discipline**, not a number, and [`plans/0002`](../../plans/0002-open-questions.md) §D2's row is retired rather than filled. `SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `NO VERDICT`

## Why

### Two sites multiply money by a fraction and floor, and both were already decided

`adr/0050` states the requirement without a way to meet it: *"**Money's unit must be fine relative to prices.** Prices are integers, so the smallest expressible price is 1 and a coarse unit gives the economy no resolution."* **Fine relative to what, by how much, was never derived.** It is derivable, and the two multiplying sites are already in the corpus:

| Site | The operation | Decided by |
|---|---|---|
| The price tâtonnement | `price *= clamp(demand / supply, 0.9, 1.1)` | [`02 §5.6`](../02-simulation-model.md), which chose ±10% over UrbanSim's ±25% *"so players see prices drift rather than snap"* |
| Every percentage apply count | `FloorDiv(readout × percent, 100)` (`RuleEngine.cs:763`) | [`02 §4.1`](../02-simulation-model.md) — *"a percentage is an apply count; a flat amount is an amount"* |

Both floor, and the second says so in its own remark: *"floor division, because a fraction of an application is not an application."*

**A quantity `Q` under a fractional step `f` can move only if `f·Q ≥ 1`.** At the design's smallest step that is `Q ≥ 10`, exactly and with no judgement in it. A price of 9 units is **frozen**: `floor(9 × 1.1) = 9`, so the tâtonnement is a permanent no-op on it and `02 §5.6`'s *"the tâtonnement does all the work from there"* is false for that Good for ever.

So the unit is not free. ***A resolution requirement is set by the smallest fraction a design multiplies by, and a design that has chosen its fractions has already chosen its unit.***

### There is no key, and `adr/0065` already said so without being read that way

[`0065`](0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md) says *"money's unit is a Ruleset choice"* and that redenominating is *"a Ruleset change rather than a code change"*. That does **not** describe a `unit = X` key — it describes the scale being **implied by the money quantities a Ruleset writes down**. Nothing declares it, so nothing can be authored wrongly, and there is no value for `adr/0052` to want a ratifier for.

This makes the row the **fourth** in `plans/0002` §D to be retired by losing its quantity rather than gaining a value, after `commute_peak_factor`, the Sight Horizon and the Zone Rule sample size. ***The cheapest way to satisfy `adr/0052` remains finding the derivation***, and four rows have now dissolved under it.

### The failure a coarse unit produces is a policy the game did not choose

This is the argument that makes the discipline worth stating rather than assuming.

`FloorDiv(readout × 15, 100)` is **zero** for every readout below 7. So under a coarse unit a percentage tax collects **nothing** from the poorest Households and the stated rate from everyone else — a **regressive** outcome, produced by rounding, in the system [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) calls *"the most politically loaded mechanic in the design"* and where that ADR requires the game *"take no position"*. Nobody chose it, no document describes it, and nothing would report it.

⚠ ***An arithmetic artefact that lands on a distributional outcome is a design position taken by accident***, and it is worse here than a wrong number would be, because a wrong number is visible in the file and this is visible nowhere. `NO VERDICT` is a commitment about what the simulation asserts, and flooring asserts something.

### Standard practice corroborates the shape and is not the source of the number

Representing money as an integer count of a minor unit is ordinary practice, and this project already does it — `Money` is *"currency, as a signed 64-bit integer count of the smallest unit"* (`Money.cs:20`). Financial systems also routinely carry **more** precision through intermediate percentage work than they report, for exactly the reason above: a rate applied to a small balance rounds away.

⚠ **Cited as corroboration and never as a source.** The unit here is derived from **this** design's own steps — `02 §5.6`'s clamp and `02 §4.1`'s apply count — and an outside practice agreeing with the shape is a reason for confidence, not a number to import. `plans/0012` **Cause 5**'s external form is the standing warning: `adr/0019`'s *"64 Ticks/s is Factorio's rate"* was wrong, and it survived because a figure landing near another product's read as corroboration. ***Two quantities sharing a unit are not thereby comparable.***

## Consequences

- **No `[money]` table, no `unit` key, and no `adr/0052` number.** `plans/0002` §D2's *Money's unit* row is **retired**, and its retirement is recorded there rather than deleted.
- **The discipline is one sentence, and it belongs with the Ruleset author**: any money quantity a fraction is ever taken of — an income a tax reads, a price the tâtonnement moves — must be **at least ten smallest units**, and wants materially more than ten to drift rather than snap.
- ⚠ **Ten is the floor and is exact; a comfortable working figure is a judgement.** One order of magnitude of headroom above the floor puts a 10% step at ten representable values, which is what *drift rather than snap* asks for — but that headroom is **chosen**, and this ADR does not pretend the arithmetic supplies it. Stated separately so a later reader can move one without disturbing the other.
- **Milestone 10 gains a ratifier the row never had**: the count of percentage applications that floor to zero. It names a **machine** (the tax circuit), a **world** (that milestone's fixture) and a **quantity**, which is `adr/0052`'s third clause as amended 2026-08-15. Built with the circuit it measures, in [`plans/0033`](../../plans/0033-conserved-money-and-the-treasury.md) task 5, not before.
- **Nothing is refused at load.** A loader cannot know a readout's magnitude at load time, so this cannot become a refusal, which is why it is an instrument instead. ***A discipline the loader cannot check needs a counter or it is a comment.***

## What would trigger revisiting

- **A third site that multiplies money by a fraction, at a step smaller than 10%.** The floor is `1/f`, so a 1% step anywhere moves the requirement by an order of magnitude. This is the trigger most likely to fire, because a tax rate finer than whole percent is an ordinary thing to want.
- **The tâtonnement clamp moving.** `02 §5.6` chose ±10% deliberately and against a cited alternative; if it widens or narrows, this ADR's floor moves with it and nothing else recomputes.
- **The floor-to-zero counter reading non-trivially in a shipped Ruleset.** That is the discipline being violated in content rather than in code, and the response is the Ruleset's numbers, not this rule.
- **Money ceasing to be a count of a smallest unit** — a scaled fixed-point representation with fractional money, say. `adr/0003` forbids the obvious version and `adr/0065` settled the width, so this would be a large reopening rather than a tweak.
