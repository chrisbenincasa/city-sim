# A market's level is what its sellers hold, and the price divides by the sum while a wake spends the maximum

**A District Pool holds nothing, so every question of the form *how much is there?* is answered by
walking the row's sellers. There are two such questions and they take different answers: what the
market **holds** is the sum of its sellers' stock, and is the cover a price divides by; what the
market can **serve** is its largest single seller's stock, and is the budget a wake may spend.**
This completes [`0139`](0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md),
which moved the stock out to the sellers and left three call sites reading a level off a Bin that is
empty by construction.
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `EMERGENCE`

**Taken on 2026-08-26, closing `plans/0003` queue items 21 and 22**, both filed unfixed by milestone
26 tasks 8 and 9 because ***what cover MEANS for a market that is not a store*** was arguable and owed
a record ([`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)).

---

## Why

### `0139` left a hole shaped like a number, and three call sites each filled it differently

`0139` made a Pool **a market and not a store**: stock stays in the selling Building's own Bin, and the
Pool row carries the price, the wake target and the reachable sellers. What it did not do is say what
the row's own `Bin` column *means* afterwards. It means nothing — the Bin exists to be a wait-list
anchor and its level is zero in every row of every world, for ever.

**Three places went on asking that Bin how much there was, and no two of them agreed.**

| Site | What it read | What that made true |
|---|---|---|
| `World.RepriceDistrictPools` | the Pool Bin's own level — **structurally zero** | 🔴 cover collapsed to the rate, the target became `ceiling × rate ÷ rate` = the ceiling exactly, and **no price had ever moved on any world** |
| `World.RingMarket` | the **depositing** seller's level | 🟡 a buyer needing five, with a neighbour holding ten, slept through an arrival of three |
| `RuleEngine.Stop` → `World.Drain` | the Pool Bin's own level — **structurally zero** | 🟡 a waiter joining a market queue drained against **nothing**, so the rescue `Stop` exists to perform could never fire on a market |

⚠ **Only the first was filed as a defect, and it was filed as a one-line fix that it is not.** The
other two were found while writing this record, by asking the same question of every site rather than
of the one that had a symptom. ***A quantity nobody defined is not wrong in one place; it is guessed
in every place***, which is [`0093`](0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)'s
failure mode with no description to be wrong.

### The two answers are different because a purchase takes its whole batch from one seller

`RuleEngine.Buy` walks the row from a drawn offset and takes the **first seller holding the whole
batch** ([`0167`](0167-a-purchase-picks-its-seller-by-a-draw-and-waits-on-the-market-rather-than-on-a-shop.md)).
It does not aggregate across sellers, and nothing in the design proposes that it should — a Household
buys its sundries from **a shop**, not from four shops at once.

**So the sum and the maximum answer different questions and substituting either is a defect rather than
an approximation.** A row whose four sellers hold three units each:

- **can serve** a batch of three and **cannot serve** a batch of five. The sum says twelve and would
  wake a buyer nobody in that market can fill — an evaluation spent to re-park.
- **is carrying** twelve units against the District's daily draw, and is **not scarce**. The maximum
  says three and would price a well-stocked market as though it were empty.

⚠ **The pricing half is the one that reads oddly and it is the one that is right.** `0135`'s
tâtonnement divides a *stock* by a *rate* to get Days of cover; the stock in a market that is not a
store is what is on its shelves, wherever those shelves stand. ***That a buyer cannot reach all of it
in one transaction is a fact about the transaction, not about how much the city is carrying.***

### The measurement, which is what settles that this is built rather than argued

**Before, on every shipped Ruleset ever run: eight market rows, zero price changes.** After, at 2,000
Citizens over 24,576 Ticks:

| World | Sellers | Stock | Rate/Day | Price | Moves |
|---|---|---|---|---|---|
| `provisioned.toml` — the tier-1 city | 2 | 192 | 357 | **100 = the ceiling** | **0** |
| `oversupplied.toml` — the tier-0 city | 10 | 948 | 545 | **100 → 58** | **6** |

🔴 **`provisioned.toml` still prints a flat price and THAT IS THE MECHANISM WORKING.** It holds half a
Day's cover; a market with less than a Day of supply prices at its import ceiling, because there is
nothing to undercut it with. ⚠ **The old failure and the new correct result are the same digits**, so
***the flat column is no longer evidence of anything on its own*** — read the `stock` and `rate/Day`
columns beside it, which is why `--market` prints them on one line. `MarketDumpTests` asserts the
distinction rather than the column.

**The two Provider Rulesets now demonstrate the two halves of the price as well as the two halves of
the shop's life** ([`0170`](0170-a-shop-is-selected-rather-than-sited-so-the-birth-signal-is-coarse-and-death-does-the-correcting.md)
condition 4): scarcity prices at the ceiling, glut takes the two stocked rows to **58** and **50**,
and the diff between the files is still two keys.

### A boundary that moves under a sleeping buyer strands it, and the ring is unconditional

A buyer short of a pooled Good parks on the market row's Bin (`0167`). Its Building's District then
changes — the watershed re-evaluates and a Cell changes hands. Its `pool` term now resolves to a
**different** row, so no term names the Bin it is asleep on, `RuleEngine.Requirement` answers nothing,
and `Invariant.WaiterIsBlockedByTheBinItNames` fires. **Measured at Tick 362,496 on
`oversupplied.toml`**, reached by no shorter run in this repository.

⚠ **The requirement of zero is the SYMPTOM.** The Rule is right and the queue is stale, and reading it
the other way fixes this in `Requirement`, where nothing is wrong.

**`World.EvaluateDistricts` ends by sweeping every market row's wait list and then draining it.**

🔴 **A DRAIN FROM THE HEAD WAS TRIED FIRST AND IS NOT ENOUGH, WHICH WAS MEASURED RATHER THAN REASONED TO.** The argument for it was tidy: `Drain` walks from the head, re-derives each requirement and stops
at the first waiter its budget cannot cover — the same walk, from the same end, with the same
derivation as the invariant — so the invariant would be left true by construction. ***What that misses
is that the head changes.*** A stranded waiter parked *behind* a legitimately blocked one survives the
drain and is invisible to the invariant too, until the waiter in front of it wakes for its own reasons
and the stranded one becomes the head. **Shipping the head-only ring moved the violation from Tick
362,496 on `oversupplied.toml` to Tick 32,768 on `provisioned.toml`, which had been clean.**

**So the sweep looks anywhere in the queue, and a requirement of zero is the test.**
`RuleEngine.Requirement` walks a Rule's terms against one Bin; zero means no term names it, which for
a market row means the `pool` term now resolves elsewhere. That is the invariant's own predicate, and
it is exact: ***the Rule is right and the queue is stale***, so the waiter is removed and re-armed
rather than counted as unsatisfiable.

⚠ **A sweep is not a `WakeAll`, and the difference is the whole economics of the design.** It wakes
only waiters whose Rule has stopped naming this Bin. A Household genuinely short of sundries in a
market that has none stays asleep, so `02 §4.1`'s *a starved District costs nothing at all until supply
arrives* is untouched.

**The drain that follows is a second mechanism and not the same one.** A boundary move changes *which
sellers a row has* as well as which buyers: a shop migrating into a District is stock arriving with no
deposit to announce it, and `RingMarket` — which fires on deposits — would never ring for it.

**Both run on every row, unconditionally, rather than on the Districts the evaluation touched.**
Membership changes in four places in `DistrictWatershed.Evaluate` and only one is `Migrate`: a Cell
whose incumbent District is dying moves for free, and a newly built Cell is filed without moving at
all. ***A conditional ring has to be right about all four, and the two that are not `Migrate` are the
two a reader forgets.***

⚠ **Its cost rides on a cadence, not on an event.** One sweep and one drain per Good per District, on
`[districts] revisit_ticks`, immediately behind a whole-map flood that has just run. ⚠ **The sweep
re-walks the queue after each removal**, because `IndexList.Remove` clobbers the removed node's own
`next` and a walk cannot outlive it — bounded by how many waiters one evaluation strands, which is
bounded by how far a boundary may move.

## Rejected

**Cover as a production rate rather than a stock.** The other reading of *what a market that is not a
store has*. Refused by the arithmetic rather than by taste: `0135`'s target is `ceiling × rate ÷ cover`,
so a rate in the denominator gives `ceiling × rate ÷ rate` — the ceiling, for ever, which is
***precisely the bug being fixed, re-derived from the other end.*** ⚠ **It is not merely wrong here; it
is unreachable** — a flow cannot be divided by a flow to yield Days.

**The sum as the wake budget too, for symmetry.** Tempting, and one fewer concept. Refused because it
over-wakes by construction: `Buy` needs one seller with the whole batch, so a sum that clears the
requirement guarantees nothing. The cost is only a wasted evaluation, ***which is exactly why it would
never have been noticed*** — a spurious wake leaves no trace, and `WaiterIsBlockedByTheBinItNames` is
one-directional.

**The depositing seller's level as the wake budget, kept as-is.** What `RingMarket` did. Refused as a
strict under-approximation of what the market can serve: the depositor need not be the largest seller,
and the largest seller's own deposit rang the queue *before* this waiter joined it. ***An
under-approximation of a wake budget is a Rule asleep beside stock that would have covered it***, which
is `0033`'s *no Rule is asleep with all inputs satisfiable* arriving one indirection out.

**Re-homing the stranded waiter eagerly in `DistrictWatershed.Migrate`.** The placement `plans/0003`
queue item 22 named first. Refused on two counts. It is **incomplete** — `Migrate` is one of the four
places membership changes, and the free moves happen elsewhere in the same method. And it is **the
wrong direction of lookup**: a migration knows Cells, and getting from a Cell to the Rule Instances
asleep on a market row means Cell → Buildings → occupants → Rule Instances, ***where the ring gets the
same set by walking the queue that already exists.***

**Leaving it to the Rule's next evaluation.** The lazy half of the same fork. Refused because there is
no next evaluation: the waiter is asleep and `02 §4.1`'s *does not re-arm* is the whole economics of
the design. ***A stranded waiter is not late, it is gone*** — nothing on its own account will ever ring
the Bin it is on.

## Consequences

- 🔴 **The State Hash moves on every world with a market in it**, which today is `provisioned.toml` and
  `oversupplied.toml`. Prices that never moved now move, and a wake that never fired now fires.
  [`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) applies: it
  costs nothing, and what it owes is a commit subject that explains it.
- **`Space.Offered` is the one definition and `DistrictMarkets.Stock` is the one walk.** Both call sites
  read the same struct, so the price and the wake can never again disagree about what a market has.
- ⚠ **`World.Drain`'s two overloads collapse into one.** The budget is derived from the Bin rather than
  passed in, which is what stopped three callers each supplying their own. ***The private overload
  existed to let one caller substitute a budget, and every use of that freedom was a guess.***
- ⚠ **A flat price column is no longer a defect report.** `--market`'s prose is rewritten and
  `MarketDumpTests` now asserts *the price moves where there is a glut and holds at the ceiling where
  there is not*, on two Rulesets, rather than asserting a defect on one.
- **`0170`'s condition-4 pairing widens.** The two Provider Rulesets already differed on shop birth and
  shop death; they now differ on price as well, from the same two-key diff.
- ⚠ **The wake is measurably more expensive and the measurement does not exist.** Every market drain
  now walks the row's sellers. `plans/0013` carries the row **UNMEASURED**, beside `0139`'s own
  outstanding per-firing seller-lookup row — ***the same walk, owed twice.***
- **`RingMarket`'s owner-kind pre-check moves down into `World.Budget`** and is documented there, because
  it now guards every Bin write in the city rather than every deposit.
- 🔴 **IT UNCOVERED A THIRD MISSED WAKE UNDERNEATH, AND THAT ONE IS NOT ABOUT MARKETS.** With the
  stranded waiter gone, `provisioned.toml` began breaking the same invariant at Tick 32,768 in a
  different shape: a larder holding 294 with a waiter needing 280, where ***the Bin never moved and the
  requirement came down to meet it*** — 320, 280, 240. `RuleEngine.Band` derives a Rule's application
  count from a **readout**, so the requirement is a function of the city while every drain hangs off a
  Bin write. ⚠ **Pre-existing and unmasked, checked with a probe against the previous commit rather
  than assumed** — filed as `plans/0003` queue item 23, and deliberately *not* fixed here because a
  readout has no single site to re-drain at, which is the one property this record relied on.

## What would trigger revisiting

- **A purchase that aggregates across sellers.** If `Buy` ever fills a batch from more than one shop,
  `Largest` stops being what the market can serve and the two numbers collapse into one. ***That is the
  change that would make the symmetry rejected above correct***, and it is a `0167` decision rather than
  this record's.
- **Haulage between Districts** — [`0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md).
  A market whose sellers are not all equidistant has a cover that is not a plain sum, and the import
  ceiling stops being a property of the Ruleset at the same moment (`0135`).
- **A shop that can hold stock it will not sell.** `DistrictMarkets.OfferedIn` currently makes a
  Business-owned Good Bin **the offer**, with no third state. A reserved or committed quantity would
  make `Held` an overstatement of what is for sale.
- **A world in which the ring's tail bites.** If a market queue is deep enough that waiters behind a
  blocked head strand for many `revisit_ticks`, the head-narrowed discharge is too weak and the answer
  is a walk of the whole list rather than a drain from its front. ***Nothing measures queue depth
  today***, and `plans/0002` §B carries the question.
- **Districts becoming numerous.** The ring is one drain per Good per District. At two Districts and two
  Goods it is four; a city of two hundred Districts makes it a scan worth conditioning after all, and
  the condition would then have to be right about all four membership paths.
