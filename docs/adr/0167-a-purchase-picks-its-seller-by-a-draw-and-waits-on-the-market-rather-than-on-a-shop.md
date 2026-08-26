# A purchase picks its seller by a draw, and waits on the market rather than on a shop

**Resolution of a `pool` term chooses one seller by a **counter-based start offset** over the market
row's seller list and a first-fit walk from there, keyed on the buying Rule Instance. When no seller in
the District holds a whole batch, the Rule waits on the **market row's Bin** and never on any
individual seller's. ***One rule underneath both halves: no shop may hold a standing advantage over
another, and no buyer may hold a standing dependence on one shop.***** `EMERGENCE` `LEGIBLE CAUSE`

**This settles the two questions
[`plans/0044`](../../plans/0044-the-purchase-and-the-provider-that-answers-it.md) decomposition found
and no ADR held** — its open decisions **2** and **3**. Both were taken by the user on 2026-08-26,
during milestone 26 task 4, and both are **hash-bearing**.

**It completes
[`0139`](0139-a-district-pool-is-a-market-and-not-a-store-so-stock-stays-with-the-seller.md) rather
than amending it.** That record decided that a `pool` term names a **counterparty**, that resolution
chooses **one** seller, and that a blocked buyer waits on the market row — and it says **nothing about
which seller**, which it flags in its own words as *"what makes the market a market rather than a
queue."* Everything here is that gap filled.

---

## Why

### Cheapest is not available, and it is worth saying why before saying what replaced it

The obvious rule — *buy from whoever is cheapest* — cannot be written in this build, and the reason is
structural rather than temporary. `Space.DistrictPoolTable.Price` is keyed by `(District, Good)`, so
**every seller in a District charges the same number**, and a comparison over them is a comparison of
one value with itself.

⚠ **`0139` says the opposite and the build went the other way**: *"The `Price` field moves from the
market row to the seller, so per-seller dispersion is expressible from the first day rather than
retrofitted."* Milestone 12 task 6 put it on the row, `CONTEXT.md` records that reading — *the row is
the price, the wake target and the reachable sellers* — and nothing has reconciled the two.
**This record does not reconcile them either.** What it does is refuse to pretend the discriminator
exists: ***a tie-break dressed as a price comparison would read as price competition in a world that
has none.***

### So the choice is between an order and a draw, and an order is a subsidy

With every seller at one price the candidates are a **list**, and the cheapest possible rule is to walk
it from the head. That is
[`02 §8`](../02-simulation-model.md)'s rule 5 failing in its own stated form, with **list position**
standing in for entity id:

> *"contested outcomes are settled by a counter-based shuffle, never by arrival and never by entity id,
> because ordering by id is **biased** — the same Building would win every contested draw for the life
> of the city."*

A head-first walk means one shop takes every sale in its District for ever while the others stand full.
⚠ **And full is not a failure state**: `RuleEngine.Stop` clears the pressure clock for every blocking
reason but `Supply`, so an unsold shop stops on `Blocking.Space` and is **immortal**
([`0166`](0166-a-business-runs-rules-and-its-rules-live-as-long-as-its-tenancy.md), and
`rulesets/provisioned.toml`'s header at length). ***A city where list order decides who trades would
therefore not correct itself — it would accumulate permanent, invisible, unexplained losers***, which
is the exact readout failure `LEGIBLE CAUSE` exists to prevent.

**The draw picks a START and the walk is first-fit from there**, rather than shuffling the list or
scoring every seller. That keeps the common case an early exit — the cost is one draw plus the walk to
the first stocked seller — while removing the standing advantage entirely.

⚠ **It is keyed on the buying Rule Instance and not on the market row**, and that is not a detail. Keyed
on the row, every buyer in a District would be sent to the same seller on the same Tick: the bias would
not be removed, it would be **rotated in step by the whole city**, which is one herd where there had
been one favourite. `PurposeTag.SellerChoice` carries the rest, including why it is not
`PurposeTag.PoolDraw` despite the name.

### A seller below one batch cannot sell, and that is what being out of stock is

The walk accepts the first seller holding `floor × amount` — a whole batch, the least the Rule can fire
at. `0139` argued this out and its conclusion stands: because the seller is chosen **in resolution**,
resolution may choose one that has a batch, so ***a Rule fails only when no seller in the District has
one, which is a genuine district-wide shortage and the correct failure.***

## Why the market and not the shop

### A waiter names one Bin, so waiting on the seller you happened to draw is waiting on the wrong thing

`RuleInstanceTable.WaitingOn` is a single handle and `QueueNext` is a single link column, so
**subscribing to N sellers is not expressible and never will be** — `0139`'s finding, unchanged. What
this record adds is that the alternative it rules out is not merely *unavailable*, it is **wrong**:

A buyer parked on one shop's Bin is woken by **that shop alone**. Every other seller in the District can
restock and the buyer sleeps through all of it, waking only when the one shop the draw happened to
touch is refilled. ***A draw taken to prevent a standing advantage would have created a standing
dependence in the same act.*** The market row is the only address that means *any of them*.

**So `RuleEngine.Bin` resolves a `pool` term to the market row's Bin** — the address every caller but
`Check` wants, because `BinAt`, `AccumulateClaims` and `Requirement` all reason about *where a waiter
sleeps* rather than about *who supplied it*. `Check` alone expands the term into its three deltas,
because a term is 1:1 in that engine and a purchase is **1:3**.

**And a seller's deposit rings the market** — `World.RingMarket`, which drains the market row's queue
against **the depositing seller's own level**. ⚠ The market Bin holds nothing by construction, so its
own level would be a budget of zero; and the arriving delta is what
[`0063`](0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)
refused, because a consumer short of three fed by arrivals of one sleeps for ever.

### Short of money is a different failure and blames the buyer's own balance

The Good leg is touched before either money leg, so `RuleEngine.Check`'s existing rule — *the first
short Bin, in touch order* — blames the market when a purchase is short of both. **A district-wide
shortage is the more informative cause and the one a player can act on**; destitution is reached anyway,
because a buyer woken by a restock re-checks, fails on money, and subscribes to its own balance. ***Both
converge and only one of them says something about the city.***

⚠ **The wait can therefore bounce between the market row and the buyer's balance**, which
[`plans/0044`](../../plans/0044-the-purchase-and-the-provider-that-answers-it.md) **P5**
names and no record arbitrates. It is legal, it converges, and nothing about it is stored:
[`0137`](0137-the-wait-list-knows-which-bin-and-evidence-does-not-so-bankruptcy-needs-one-field.md)
gives `RuleEvidence` the blocking Bin, which is milestone 26 task 5.

## Consequences

**`Touch` gains a blame target, and it is the only place in the engine where the Bin that fails and the
Bin that is named differ.** Everywhere else they are the same Bin and the parameter defaults to itself.

**A third answer exists that neither open decision saw: *no market at all*.** A Building raised between
two watershed evaluations stands in no District for up to `[districts] revisit_ticks`, and there is no
Bin to wait on — so the Rule **fires at zero applications and re-arms on its rate**, which is
`RuleVerdict.Succeeded`'s own documented reading: *"it re-arms on its rate, waits on nothing, and moves
nothing, because there is no Bin that could ever wake it."* ⚠ **A Ruleset that states no `[districts]`
table is a different case and throws**, because that condition is permanent rather than early; the
refusal belongs at load and the loader has no such check yet.

**One new `purpose_tag`, no new number, and no `plans/0002` §D2 row.** The draw is a start offset over a
list whose length the world decides. `plans/0002` **§D1** carries the *decision* rather than a value,
because it is hash-bearing and would otherwise sit in no ledger at all.

🔴 **The cost is measured and it is not comfortable.** [`plans/0013`](../../plans/0013-tick-budget.md)
carries the reading: **0.237 / 0.585 / 1.267 ms a Tick at 10,000 / 20,000 / 40,000 Citizens** on the
reference machine, which is **~n^1.2** and the ledger's only super-linear consumer. ⚠ **That figure is
the whole purchase and not the seller walk**, and `0139`'s named fallback — *an index on the market row*
— **was built by the same task**, so it is included rather than available. The question is re-opened in
`plans/0002` **§B** as a new one, and ***nothing here may be optimised until an attribution says where
the time goes.***

## What would trigger revisiting

- **Per-seller prices.** `06` milestone **13**, the price surface, which is `0139`'s own revisit
  trigger — *a second Building kind selling the same Good in one District*. ***The draw is retired
  rather than ratified on that day***: cheapest becomes a discriminator, and a rule that reads a price
  is better than a rule that refuses to pretend one exists.
- **[`0133`](0133-a-pool-draw-pays-for-its-haulage-so-the-boundary-is-a-gradient-and-not-a-cliff.md)'s
  haulage charge.** It gives sellers at different distances different **delivered** prices, which is
  dispersion arriving through geometry rather than through pricing — and it reaches the same trigger by
  the other door, without any Ruleset authoring a price.
- **An attribution showing the draw or the walk is where the Tick goes.** The measurement above cannot
  say. If it turns out to be the walk, the answer is a cheaper candidate structure and not a cheaper
  rule — ***a biased choice bought back for speed is `02 §8` rule 5 traded away for a constant.***
- **A buyer that must reach a specific seller.** Loyalty, contracts, a Household's Provider List
  ([`0066`](0066-the-provider-list-is-an-intrusive-index-list-and-its-ruleset-length-is-a-cap-rather-than-an-allocation.md))
  — any of them makes *which seller* a property of the buyer rather than of the draw, and the market
  subscription would then be waiting on a set the buyer does not actually shop in.
