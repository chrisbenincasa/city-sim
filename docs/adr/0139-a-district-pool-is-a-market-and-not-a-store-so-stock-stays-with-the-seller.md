# A District Pool is a market and not a store, so stock stays with the seller

**A `pool` term names a *counterparty*, never a container. The Goods a Building offers for sale stay in
that Building's own Bin; the `(District, Good)` row is the **market** — the price it clears at, the
thing a blocked buyer waits on, and the set of sellers a District's connectivity makes reachable. A
buyer's `pool` input resolves to **one seller's Bin**, chosen at term-resolution time. A seller's `pool`
output deposits into **its own** Bin and rings the market.**
`SOLVE THE ACTUAL PROBLEM` `EMERGENCE` `LEGIBLE CAUSE`

**This amends [`0013`](0013-goods-are-pooled-within-a-district-and-shipped-between.md) rather than
superseding it.** Everything `0013` decided survives: Goods move freely within a District and are
physically shipped between them, no Vehicle is simulated inside one, and no routing query is issued per
unit moved. ***What `0013` decided is REACH, and this record is about CUSTODY*** — two questions that
one sentence had been answering at once.

The record of the sitting is [`plans/0038`](../../plans/0038-session-u-the-pool-or-the-seller.md),
session U. **It carries the evidence in full and this record carries the conclusion.**

## Why

**`0013` never asked who the bakery buys from, and its own *Rejected* section is the proof.** It names
exactly two alternatives — ***ship everything, including within a District***, and ***pool everything,
city-wide*** — and **both are about movement**. Its case is a simulation-budget argument about query
volume: *"a routing query per unit moved"*, GlassBox embodying freight. **Direct bilateral trade inside
a District issues no routing query and creates no Vehicle**, so it is compatible with every word of that
argument. *"The Pool is just a Bin per Good per District"* appears only in that record's
**Consequences**, as a note that nothing new had to be invented — ***which is an observation about
implementation convenience, and it was read for four milestones as a decision about ownership.***

**The corpus had already caught this exact reading, on this exact record, one day earlier.** `04 §4`,
amended by [`0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md):
*"`adr/0013` is not reopened: its case was a **simulation-budget argument about query volume**, and it
never claimed carriage was worth nothing — **free-inside was the default reading of a budget decision,
not a finding.**"* ***The Pool as counterparty is the same shape of reading.*** One budget decision was
found carrying two unexamined riders in two days, and that is a fact about how budget decisions are
read rather than about this one.

**Nothing had ever examined it.** [`plans/0018`](../../plans/0018-session-n-the-bin-the-pool-and-the-economy.md)
— session N, the sitting that took this cluster — records `adr/0010`–`0022` sitting in **one blanket 🟢
coverage row over thirteen ADRs**, and says in its own words that *"a 🟢 mark here is not evidence any
sentence in `adr/0013`, `0017` or `0022` was ever examined."* **It flagged the gap and did not take it.**

**The engine was never shaped against this.** A term is `BinRef(Scope Scope, ResourceId Resource)` —
two fields. ***It names whose and what, and never which Bin.*** `RuleEngine.Bin` turns that into a slot
at evaluation time: `Scope.Local` walks the Building's own Bin list, `Scope.Global` walks the
treasury's. **Choosing a counterparty is already what that function does**, and a seller lookup is a
third case beside two that exist. `Scope`'s own remark states the rest: ***"A scope answers whose is
it, not where do I look"***, and ***"No payment is ever authored in a Rule."***

**And the code's named hole says so outright.** `RuleEngine`'s `Scope.Pool` throw, written when slice 6
shipped: ***"the Pool is a MARKET, not a wider Bin lookup … Implementing this as a Bin lookup ships an
unconserved economy, and no refusal can catch that."*** ⚠ **Read for what it is** — a warning about the
**money leg**, binding on any implementation — **it is not evidence between the two custody models.**
What it is evidence of is that *the Pool is a market rather than a container* is the corpus's fourth
independent statement of the same thing, in a fourth place, by a fourth author, none of them citing the
others.

**What the change buys is a second side.** Under a shared store the Pool is the counterparty to every
trade, which forces a question with no good answer: **who holds the money between a Provider's deposit
and a consumer's draw.** A District owns nothing — `MoneyLedger` resolves `Treasury / Household /
Business` — so the answer is either a fifth `BinOwnerKind` doing double duty, or consignment, which
[`0003`](0003-deterministic-integer-simulation.md) forces anyway because the design never holds negative
money and paying at deposit deadlocks at Tick 0. ***A seller's money Bin is a Business balance that
already exists.*** [`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md)
decision 10 is **void as posed** rather than answered: it was never a question about money, it was the
shape of a missing counterparty.

**The objections were read against the build rather than argued, and four did not survive.** The full
list is `plans/0038` **U2**–**U6**; the two that mattered:

- ***Term resolution is 1:1 and a purchase is 1:2, so direct trade makes it 1:3.*** **Withdrawn.** A
  term names a scope and a Resource; the money leg is implicit under `0050` in either model. **Nothing
  about the term structure changes.**
- ***A Rule is atomic, so a small seller cannot supply a large buyer.*** **Substantially wrong.**
  `02 §4.1`'s band means a Rule needs `min × delta`, and every shipped production Rule declares
  `{ min = 1, max = 4 }` — one batch, not a day's appetite. The fixed form occurs **once** in the
  corpus, on a tax circuit, which is the *"actor owes a quantum"* case. ⚠ **And because the seller is
  chosen in resolution, resolution may choose one that holds a batch** — so a Rule fails only when **no
  seller in the District has one**, which is a genuine district-wide shortage and the correct failure.
  ***The consequence that survives is that a seller below one batch cannot sell, and that is what being
  out of stock is.***

## Consequences

**A blocked buyer waits on the market row, and a seller's deposit rings it.** This is the one mechanism
the sitting could not read off the build, because it does not exist. A waiter names **one** Bin —
`RuleInstanceTable.WaitingOn` is a `HandleColumn<Bin>` and `QueueNext` is a **single** link column
shared with the Event Wheel — so ***subscribing to N sellers is not expressible and never will be.***
The market row is therefore the subscription target, and a seller depositing drains that row's list
**against its own level**. ⚠ **One thing gets simpler**: today `RuleEngine.Requirement` nets terms by
resolving each to a slot and comparing slots; a waiter parked on the market compares **`BinRef`**
instead — two fields, no resolution — which is the set arithmetic `RulesetLoader`'s chain validator
already performs at load. ⚠ **A woken waiter still reserves nothing**, so several may wake against one
seller's stock and re-fail. **That is inherited from [`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)
and not created here** — the drain's guarantee was always about an instant.

**Every seller carries a price, and today every price is the import ceiling.** The `Price` field moves
from the market row to the seller, ***so per-seller dispersion is expressible from the first day rather
than retrofitted*** — which is the whole point of deciding it now. **But no price-formation mechanism
ships with this record.** A seller opens at `Ruleset.ImportCeiling(resource)`, which is already
authored per-Good, already the value a Pool opens at, and already the price every trade clears at on
the ten shipped files that state no `[market]`. ⚠ **So the stand-in costs NO new number and NO new
ratifier**, and `plans/0002` §D gains nothing. ⚠ **A Ruleset-authored per-kind price was considered and
refused**: `04 §4` says the price is emergent and *"neither the player's nor the Ruleset's"*, and a
ceiling is a **bound** rather than an authored price, so clearing at it contradicts nothing. **Per-kind
prices matter only where two kinds sell one Good, which no shipped file does.**

**A `pool` output deposits locally and marks the stock as offered.** *Whose is it* is answered by the
scope: a `local` output is the Building's to keep, a `pool` output is the Building's **to sell**. The
units do not move on production and they do not move on sale — ***delivery inside a District was always
instant and free, and that is `0013` unchanged.***

**`04 §4`'s *Local* bullet loses its stated derivation and keeps its conclusion.** It reads *"Prices are
per-District, **because** Goods are pooled per-District."* The premise goes. **Prices remain local**
because a buyer may only reach sellers its District's connectivity reaches — ***which is `0013`'s reach
argument doing the work the pooling was credited with.*** The gap that makes inter-District Shipments
profitable survives intact.

**What milestone 12 must now build**, and it is smaller than the milestone as scoped: `Scope.Pool`
resolving to a seller, the market row as a wake target, the money leg under `0050`, and a
`(District, Resource) → row` index that
[`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md) task 7 already owed.
**Most of tasks 5 and 6 stands** — `MarketRuleset` survives with its signature unchanged, because
`Reprice` takes the level as a plain `long` and only its caller changes; the whole `[market]` and
`[[hinterland]]` loader path survives; **20 of 20 `MarketRulesetLoadTests` cases and 22 of 26
`PoolPriceTests` cases survive.** What goes is the `Bin` column and what reaches through it,
`BinOwnerKind.District` and its three uses, `RetirePool`'s stock transfer, and
`Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool`. ⚠ **`DistrictPoolTable` reads its Good off the Bin
and will need a `Resource` column added back.**

**[`plans/0003`](../../plans/0003-build-plan.md) queue item 17 is retired by this rather than fixed by
it.** `World.RetirePool` raises the heir Bin's level with a raw `Bins.Move` and never drains it; with
no stock in a market row there is no transfer and no heir Bin. ⚠ **It must be struck deliberately when
the code lands and not assumed** — a defect that disappears because its method disappeared is still a
defect until the method does.

⚠ **The per-firing seller lookup is *measurable* and remains UNMEASURED.**
[`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) binds: **no
document may cite it as decided until a number exists.** What this record establishes is only **where
the lookup goes** — into resolution, beside `FindBin` and `FindTreasuryBin` — ***which is a statement
about position and not about cost.*** ⚠ **The analogy to
[`plans/0013`](../../plans/0013-tick-budget.md)'s job search does not reach and must not be quoted as
if it did**: that bill is the **spatial box**, 841 Cells and ~3,400 `int` reads before any routing, and
its own finding is *"where it goes is not the search."* A District's sellers are a **list**.

## What would trigger revisiting

- **A measured seller-lookup cost that does not fit.** The number does not exist. If resolution over a
  District's sellers prices above its share of the Tick budget at target scale, the fallback is **not**
  a return to a shared store — it is an index on the market row, which is the shape
  `DistrictPoolTable` already has. ***Reopen this record only if an index does not close the gap.***
- **A second Building kind selling the same Good in one District.** The stand-in price collapses: two
  sellers at one authored ceiling cannot be told apart, and the dispersion this record exists to make
  expressible would be unexpressed. **That is the trigger for real price formation**, and
  [`06`](../06-roadmap.md) milestone **13, the price surface**, is where it lands.
- **A wait-list requirement that `BinRef` comparison cannot express.** The market subscription rests on
  a waiter's need being answerable from `(scope, resource)` alone. A term whose requirement depends on
  **which** seller was chosen would break it.
- **[`0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)'s
  haulage charge landing.** It makes the import ceiling per-District, and a within-District carriage
  cost gives sellers at different distances different delivered prices — ***which is dispersion arriving
  through geometry rather than through pricing***, and it may make per-kind prices unnecessary for
  longer than expected.
- **Freight inside a District ever becoming visible.** `0013`'s reach argument is the load-bearing half
  of this record. If a Vehicle is ever simulated for a within-District movement, both records reopen
  together.
