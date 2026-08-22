# A Pool draw pays for its haulage, so the boundary is a gradient and not a cliff

**Intra-District movement stays unsimulated and stops being free.** A Pool draw is already a priced
transaction under [`adr/0050`](0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md);
it gains a **haulage charge**, so carrying a Good across a District costs something even though no Vehicle
is created and no routing query is issued. [`adr/0013`](0013-goods-are-pooled-within-a-district-and-shipped-between.md)
is **not** reopened: what it refuses is *embodying* intra-District movement, and this charges for it
without embodying it.

⚠ **The charge's form and value are UNSET and are not chosen here.** Three candidate forms are recorded
below; the leading one makes the extent bound self-enforcing. **Whatever is chosen is hash-bearing and
owes a [`plans/0002`](../../plans/0002-open-questions.md) §D row on the day it is written down**
([`adr/0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)). 🔴 **And it
owes a payee before it ships** — see *Consequences*, where the hard part is.

`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE` `EMERGENCE`

## Why

### `adr/0013` argued a simulation budget and was read as an economic claim

Its case is *"expensive simulation is reserved for decisions the player actually makes"*, elaborated as:
the mechanism worth modelling is *"this District cannot feed itself and the trucks that fix that are
clogging your one arterial"*, not *"a box gets from the bakery to the shop"*, and embodying the second
*"costs a routing query per unit moved"*. **Every clause of that is about query volume.** Nowhere does
`adr/0013` claim that intra-District carriage is *costless*; it claims that simulating it is unaffordable
and unrewarding. ***A decision not to model something is not a finding that it is worth nothing***, and
free-inside was inherited as the default reading of a budget argument.

### What the default reading leaves is a discontinuity with no physical referent

Two Buildings 100 m apart across a boundary pay a Shipment — a real Vehicle on the Road Graph
contributing real congestion. Two Buildings 1.45 km apart inside the same District pay nothing at all.
Nothing physical happens at that line, and no sentence in the corpus defends the step. **The cliff is
also what makes the boundary worth gaming**: a step function is exactly the shape that rewards moving the
line, whereas a gradient does not.

### `adr/0013` names the remedy in its own trigger list

> If that becomes the dominant strategy, Districts need **a size ceiling or a cost**.

[`adr/0132`](0132-the-district-is-derived-and-a-ward-is-what-the-player-draws.md) takes the player's pen
away, which stops a *player* from exploiting the cliff. **This ADR removes the cliff itself**, which is a
different job: with no player arm, the remaining way to draw one enormous District is for the *derivation*
to produce one — by a generator's parameters, a designer's Ruleset, or the extent number in
`plans/0037` decision 3 being set too large. ***A defect closed only at the interface the player touches
is still available to everyone else who can move the same value.***

### It is cheap for `adr/0013`'s own reason

`adr/0050` already makes a Pool draw a market transaction: the Good moves one way, money the other, at the
prevailing price, settled atomically with the Rule. **There is therefore already a place to put a charge**,
and nothing new is invented — which is the reason `adr/0013` itself gives for the pooled case being cheap
(*"the Pool is just a Bin per Good per District, so Rules, fallback chaining, and the Outside Connection's
import path all work on it unmodified"*).

### Candidate forms, and why the middle one is the leading one

| Form | What it does | Cost |
|---|---|---|
| **Flat carriage charge per unit drawn** | One number per District. Removes *free* and nothing else | Keeps the Bin exactly. Does not vary with how strained the abstraction is, so a huge District and a tiny one charge the same |
| **⭐ Charge scaled by the District's own extent** | A large District's internal carriage costs more, because ignoring transport across it is *less defensible* | Keeps the Bin — the term is a property of the District, not of the pair. **Makes the extent bound self-enforcing** |
| **Per-buyer distance term** | The delivered price depends on where the buyer is | Physically the most honest, and it **breaks** *"the Pool is just a Bin per Good per District"* — the delivered price is per-pair, not per-District, which is the property `adr/0013` calls *"most of why it is cheap"*. Not refused, but it must be **priced before it is chosen** |

⭐ **The middle form is attractive beyond its cost, because it converts an authored ceiling into an
emergent one.** `CONTEXT.md` bounds a District by *"the area within which ignoring transport is a
defensible simplification"* and then anchors it at **128 Cells**, admitting the number is *"a starting
point rather than a derivation"* with *"what actually pools convincingly is a playtesting question."* A
charge that scales with extent **makes the simulation bill the indefensibility** rather than forbidding it
at an authored line. ⚠ **That would materially change `plans/0037` decision 3**, which currently owes a §D
ratifier for a ceiling — a self-enforcing bound may need a *curve* with a ratifier instead of a *ceiling*
with one, and those are different obligations. `EMERGENCE`, and it is the reason to prefer this form
rather than a tidiness argument.

## Rejected

**Leave it free.** The status quo, and its only defence is that nobody has felt it yet — which is not a
defence, because no city exists to feel it in. The claim was never argued in the first place; it was the
residue of a budget decision.

**Simulate intra-District freight.** Precisely what `adr/0013` refuses, on GlassBox's evidence: every
carried unit becomes a pathfinding query against a shared gradient field, a direct contributor to that
engine's 2 km × 2 km map cap. **This ADR does not reopen it and must not be read as a step toward it.**

**Author the charge in a Rule term.** `adr/0050` refuses price syntax outright — *"offering syntax would
only offer a way to get it wrong"* — and haulage is a component of a price. It is Ruleset data at the
`[goods]`-or-District level ([`adr/0015`](0015-all-tuning-data-is-hot-reloadable.md)), never a term
a Rule author writes.

## Consequences

- 🔴 ⚠ **A cost with no counterparty destroys money, and
  [`adr/0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) forbids it.** This is
  the hard part and it is **open**. If a buyer pays a price plus carriage and the seller receives only the
  price, the difference has to arrive *somewhere* — and *"the Outside Connection is its only source and
  sink"*. The candidates are all unsatisfying today: the **treasury**, which makes carriage a tax and is
  wrong; a **haulage sector**, which is an actor nobody has built; or **the seller**, which makes carriage
  revenue rather than cost and removes the whole effect. ⚠ **This is [`adr/0117`](0117-upkeep-leaves-milestone-10-and-its-blocker-is-a-rule-with-no-actor.md)'s
  shape exactly — a charge with no actor — arriving on a second mechanism**, which is worth noticing:
  ***the corpus keeps producing costs before it produces the parties that receive them.*** **The payee is
  a blocker on shipping the charge, not on taking this decision.**
- **`04 §4`'s price gains an input.** The price stays *"not set by the player and not authored in the
  Ruleset"* — the charge is a **cost input** to a price that is still emergent from damped tâtonnement,
  not the price itself. That distinction is load-bearing and should survive into whatever authors it.
- **[`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md) decision 4 is where the
  form lands.** It already asks where a price comes from at 12, and 12 is where the Pool first becomes a
  market. ⚠ **Whether the charge ships *at* 12 is a separate call** — the payee problem above may push it
  out, and `adr/0117`'s lesson is that the deliberate *no* is worth more than the omission.
- **`adr/0013`'s trigger *"players gaming the boundary"* is partly discharged in advance**, by this and by
  `adr/0132` together. It stays in `adr/0013` as written; a trigger is not struck because a decision was
  taken for other reasons.
- **The intra-District movement remains invisible.** No Vehicle, no Trip Fate, no Segment volume. What
  becomes visible is a **number on a transaction**, which is the cheapest possible expression of the
  cost and the only one `LEGIBLE CAUSE` needs: a Building that could not afford a Good must be able to be
  told that carriage was part of what it could not afford.

## What would trigger revisiting

- **The payee question resolving in a way that changes the charge's shape.** If carriage ends up paid to a
  built actor, that actor's own economics may dictate the form, and the table above was written without
  one.
- **The charge making prices illegible.** If a player cannot tell a carriage cost from a scarcity price in
  `Evidence`, the cost is buying realism with the pillar the project is built on, and a flat per-District
  charge that is *shown separately* beats a scaled one that is folded in.
- **Measurement showing the cliff never mattered.** If a built city's Districts are small enough that
  intra-District carriage is a rounding error against Shipment cost, this is machinery for a discontinuity
  nobody can reach. ⚠ **That is measurable and the world that would settle it does not exist yet**
  ([`adr/0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)) — so it is
  a trigger and not a reason to defer.
- **The per-buyer form becoming affordable.** It is the honest one. If profiling ever says a per-pair
  delivered price fits, the Bin-shaped compromise is no longer worth its distortion.
