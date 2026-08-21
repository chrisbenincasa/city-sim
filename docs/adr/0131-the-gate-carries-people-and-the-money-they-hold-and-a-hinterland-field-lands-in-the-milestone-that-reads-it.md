# 0131 — The gate carries people and the money they hold, and a Hinterland field lands in the milestone that reads it

**Guiding concept: a stock without an ordering is a wall, whatever the design calls it.**

**Status:** accepted, 2026-08-20, with the user in the room. Closes
[`plans/0035`](../../plans/0035-hinterlands-and-arrival-through-the-gate.md) decisions **5**, **6a**,
**7** and **8**.

---

## The decision

**Milestone 11's gate carries people and the money they hold. No Good crosses it.** Trade — the import
payment, its counterparty and Shipments — lands at **12**, with `Scope.Pool`, where the market is.

**An arriving Household's balance is drawn from its Hinterland**, which authors what its emigrants
carry. **A departing Household takes its balance with it.**

**A Hinterland field is authored in the milestone that reads it.** At 11 that is the edge identity and
the emigrant balance, and nothing else.

**`SyntheticCity` places the gates.** The generator's count and siting stays a `plans/0002` §D2 gap
owned by milestone **24**, where the generator lives.

---

## Why no Good crosses at 11

`CONTEXT.md` → Outside Connection describes a thing that *"absorbs surplus Goods and supplies deficits,
at a price"*, so refusing Goods here looks like refusing the object's purpose. It is not: it is refusing
to **build the market twice**.

A trade term is `Scope.Pool`, which throws today, and its message is explicit about why the shortcut is
worse than the wait:

> *"the Pool is a MARKET, not a wider Bin lookup. A pool term crosses an ownership boundary, so the Good
> moves one way and money the other at the prevailing price, settled atomically with the Rule.
> **Implementing this as a Bin lookup ships an unconserved economy, and no refusal can catch that.**"*

And `RulesetLoader.cs:1538` already records the shape of the gap: the money-balance refusal
**over-refuses an import payment on purpose**, because *"no scope can currently name"* its counterparty.
Naming one at 11 means inventing a gate scope one milestone before `Scope.Pool` arrives to supersede it.
***Two scopes for one idea, one milestone apart, is how a superseded mechanism acquires content.***

## Why money crosses anyway

Because it is carried by people, and people are what this milestone moves. Milestone 10 wrote its
conservation assertion anticipating exactly this — [`plans/0033`](../../plans/0033-conserved-money-and-the-treasury.md):
*"Write it that way and let milestone 11 add the term."* `MoneySupply.Issued` gains its second writer
through **migration** rather than through trade, and `Invariant.MoneyIsConserved` becomes a sum with a
flow term without a single Good moving.

This also answers, for Households, a question `plans/0002` §C had open — *where does a departing actor's
balance go?* It is a **gate crossing**: the money leaves with the Household and the supply of record
falls. The same question for a **Business** is untouched, because nothing destroys one yet.

## Why the arriving balance comes from the Hinterland

`[households] opening_balance_min`/`max` exists and is a **world-founding** key. Drawing arrivals from it
would make the four edges interchangeable as money sources, while each Hinterland separately authors a
median wage and a median rent describing an economy its own emigrants did not come from. ***An anchor
that does not reach the thing it anchors is decoration.***

⚠ **And it is the only thing that makes any Hinterland field readable at 11.** `plans/0035` **F7**
records that the anchor otherwise ships entirely unread until 12 and 13 — which is
[`adr/0125`](0125-a-ratifier-that-needs-a-consumer-nobody-built-is-not-reachable-so-the-weights-get-a-floor-and-a-debt.md)'s
unreachable ratifier repeating one milestone later. One readable field is the difference between a
number a run can refute and a number nobody can.

---

## Why the rest of the Hinterland waits, and why drawdown in particular

`CONTEXT.md` → Hinterland: *"A Hinterland is a stock the city spends … the city **takes the most willing
first**, so drawing it down raises its rate **and** skews its mix toward the stages that weight
cheapness hardest. Both effects have the same cause and neither needs a rule."* And, in the same entry:
*"**There is no population ceiling.** Drawdown is a gradient, not a wall … What runs out is *cheap*
immigration, never immigration."*

🔴 **Both properties come from the willingness ordering, and the ordering is the comparison, which is
milestone 16.** A stock that decrements at 11 has nothing to order by — so it cannot express *the
willing are taken first*, cannot raise a rate and cannot skew a mix. **The only thing it can express is
availability**: arrivals, then no arrivals. ***A stock without an ordering is a wall, whatever the design
calls it*** — and the wall is the population ceiling this entry refuses by name, arriving as an
implementation detail of the mechanism that was supposed to replace it.

**So `depth` and `recovery_rate` are authored at 16, with the ordering that makes them a gradient.** The
general rule this milestone adopts and the next ones inherit: **a Hinterland field is authored in the
milestone that reads it.**

| Field | Authored at | Read by |
|---|---|---|
| edge identity, emigrant balance | **11** | arrival |
| depth, recovery rate | **16** | drawdown, once willingness orders |
| price per Good | 13 | the price surface |
| median wage | 15 | the wage surface |
| median rent, service levels, commute figure | 16 | the comparison |

⚠ **This also settles `plans/0035` F9**, which found the field list differs in three documents. It is not
settled by choosing one document's list; it is settled by never needing the whole list at once.

## Why `SyntheticCity` places the gates

[`adr/0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md) makes gates generator
output, and [`adr/0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
puts the generator at **24**. Building a fragment of the generator at 11 would make the count and siting
hash-bearing world-creation state **now**, with no derivation available: the §D2 row says to derive the
count from *the unlock rule*, and `adr/0090` refused the unlock rule outright — *"The map is open — no
unlock, no serviceability gate, no boundary."*

So 11 places gates the way it builds every other test world, and the generator's number stays a gap
owned by 24. ***A number whose derivation was refused is not made choosable by the milestone that needs
a world.***

---

## Consequences

- **Shipments move off milestone 11.** `06`'s inventory said *"Placed: 11, behind 12"*, which 11 running
  before 12 makes impossible; they are behind 12 because freight needs something to carry.
- **`plans/0002` §D2's price-offset row stays a gap** — with no Goods crossing, it still has no consumer,
  and its ratifier moves to 12 rather than to 11.
- **The throughput ceiling becomes ratifiable at 11**, because arrivals are what it bounds.
- **One new authored object** — `[[hinterland]]`, with an edge and an emigrant balance — and **one new
  `[[building]]` kind** for the gate itself, per `adr/0088`'s *no new table, no new column, no new
  mechanism*.
- **`Invariant.MoneyIsConserved` stops being an exact equality**, and milestone 10's acceptance run's
  exactness expires exactly as `plans/0033` said it would.
