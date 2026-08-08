# Crossing an ownership boundary is a trade, and payment is implicit in the scope

**A Bin Rule *transforms* Bins belonging to one owner. A term whose scope crosses an **ownership boundary** is a *trade*: the Good moves one way and money the other, at the prevailing price, and there is no Ruleset syntax for the payment at all.** A term's `amount` therefore stays a fixed integer for ever, and no price is ever authored in a Rule. **Prices anchor to the Hinterland**, which gains a **price per Good** alongside the rent and wage it already carries — one authored object per map edge, bounding three markets. `EMERGENCE` `LEGIBLE CAUSE`

## Why

### An apply count scales both sides of a Rule, so it can never express a rate

`04 §4` requires prices to be **per-District**, recomputed **each Day**, and *"not set by the player and not authored in the Ruleset."* A Rule's term is the opposite of all three: `{ scope, resource, amount }` with `amount` a fixed integer, identical everywhere in the city.

`02 §4.1` already answers *varying numbers* with the derived apply count — *"`amount` stays a fixed integer; `apply` may be computed from a Readout […] a percentage is an apply count; a flat amount is an amount."* That works, and it works for more than it first appears: taxes, wages and Policy percentages are all **one-sided** flows, and one side scales freely.

It cannot work for a purchase, and the reason is arithmetic rather than taste:

```
n × (−1 money, +1 Food)   →   n money for n Food   →   price = 1, always
```

**The count cancels out of the ratio.** Raising `n` buys more Food *and* spends proportionally more money; it never changes what Food *costs*. A variable rate needs a **per-term** number, which is the expression language `02 §4.1` exists to avoid.

> A derived apply count expresses a **variable quantity at a fixed rate**. It can never express a **variable rate**. A purchase is a variable rate.

### The way out is that a purchase was never a transformation

Turning six flour into four bread happens **inside one Building, under one owner**. Nothing is exchanged, there is no counterparty, and there is no price — the Ruleset's fixed integers describe it exactly, because a production recipe genuinely is a constant.

Acquiring flour from the District Pool is a different act. It crosses an **ownership boundary**, and in the world being modelled that is not a transformation, it is a **trade**. Manufacturing is not a purchase; buying is.

So the scope column, which read as *where do I look for this Bin*, is really answering *whose is it*:

| Scope | What it is |
|---|---|
| `local` | my own Bin — **free, because it is already mine** |
| `pool` | the District market — **bought at the local price** |
| import | the Outside Connection — **bought at the ceiling price** |

Which reframes `adr/0045`'s fallback ladder. *local Bin → District Pool → Shipment → import → terminal* is not only a **source** ladder; it is a **price** ladder, monotone increasing, and `04 §4`'s *"the Outside Connection price is a ceiling"* is exactly what guarantees the monotonicity. The rungs are in that order **because each one costs more than the last.**

### A Business with no margin is not an economic actor

This is the objection that produced the decision, and it is decisive. `04 §4` says a Business *"notices its margin is bad and considers a small number of known alternatives"*, and [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) wants bankruptcy as *"a distinct diagnosis from input starvation."*

If a Business's inputs arrive free down an authored ladder and its outputs vanish free into the Pool, it has **no margin, cannot go bankrupt, and has nothing to satisfice over.** Making the Pool a trade rather than a wider Bin lookup is what makes a Business a participant: it buys inputs at Pool prices, sells outputs at Pool prices, and the difference is a margin nobody had to invent a mechanism for.

### The failure surface falls out, and it is the one `0024` asked for

Because a trade touches both a Good Bin and a money Bin, the existing atomicity and subscription machinery separates the two failures with no new code:

| What happened | Rule fails on | Diagnosis |
|---|---|---|
| the Pool is empty | the **Pool Bin**, `Blocking.Level` | input starvation |
| the Pool has stock, the buyer cannot afford it | the **money Bin**, `Blocking.Level` | **bankruptcy** |

Two Bins, two blame targets, two sentences from Evidence — `0024`'s *"different failures with different remedies"*, delivered by the wait list that already exists.

### Payment is implicit because a designer has nothing to say

The alternative is that a Rule declares its payment explicitly, which needs the variable term this ADR exists to avoid. But the stronger argument is that **there is no number left for a human to write**: the price is emergent by `04 §4`, the quantity is already the term's `amount`, and the counterparty is already implied by the scope.

Offering syntax would therefore only offer a way to get it **wrong**. A Rule that drew from the Pool and forgot to pay would be an unconserved economy authored by accident, in the one system `04 §2` stakes everything on — *"if a hundred units of Food entered the District, a hundred units must be accounted for."* Refusing to represent the mistake is the same move [`0034`](0034-fields-are-sorted-by-source-geometry.md) made in splitting the Cell out of the Chunk, and the same one [`0048`](0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md) made with quoted decimals.

### The boundary is authored; the interior is derived

An emergent price needs an anchor or it can run away, and an unbounded price is an unbounded integer arriving at a money Bin. [`0026`](0026-wages-are-posted-locally-and-never-cleared.md) already found the anchor and stated the general form:

> Every price system in this design now anchors to the same **authored object** — the **Hinterland**, per [`0023`](0023-immigration-arrives-through-the-gate.md) […] that object doing **central-bank duty**, through trade and migration rather than credit.

A Hinterland *"is never Ticked and never rendered — it is a small configuration, not a simulated place."* So a designer authors **one object per map edge** and never authors a price anywhere else. Local prices emerge by tâtonnement, bounded above by *Hinterland price + haul* — which makes the ceiling per-location without anybody writing a per-location number.

### …but the anchor had no Goods prices in it

`0026` claims all three markets anchor to *"the same authored object."* `0023` enumerates what a Hinterland holds: *"a population with composition, a rent, and a wage."* **No Goods prices.** So `0026`'s own sentence — *"the Hinterland wage anchors it, **as import price already anchors Goods**"* — treats the import price as a separate pre-existing thing while claiming one object anchors everything. As the corpus stood, the claim was **false by inspection**, with one anchor enumerated and a second implied but homeless.

Giving the Hinterland a price per Good makes the claim true, adds no object, and **generalises a decision the design already values**. `0023` made Outside Connection placement matter for *people* — *"another road on the same edge buys throughput into a market you are already draining; a port on the far edge buys a different economy."* With per-Hinterland Goods prices the same sentence becomes true of **trade**: importing Food through the north gate can be genuinely cheaper than through the south, and a port on a far edge is an economic decision rather than only a logistical one.

## Consequences

- **`02 §4.1`'s scope table gains a meaning column.** `pool` is **a market, not a wider Bin lookup**, and anyone implementing it as a lookup ships an unconserved economy. Slice 7's named hole says so.
- **A term's `amount` is a fixed integer permanently.** No expression language, no per-term multiplier, no parser — the pressure that would have introduced one is removed rather than resisted.
- **`derived` is confirmed as specified rather than widened.** It serves one-sided proportional flows. Slice 7 task 7 is unchanged by this ADR, which is the reason the ADR was written before it.
- **`adr/0023` gains a field**: a Hinterland carries a **price per Good**, in domain units, alongside its rent and wage. Four edges therefore price Goods independently, and `04 §8`'s still-open *"should Outside Connection prices drift?"* has somewhere to land.
- **`adr/0026`'s *"same authored object"* becomes true**, where it was an assertion the schema did not support.
- **Money's unit must be fine relative to prices.** Prices are integers, so the smallest expressible price is 1 and a coarse unit gives the economy no resolution. This is a sizing decision owed before the first priced Ruleset, not before slice 7.
- **A trade is atomic with the Rule that triggered it.** Goods and money move together or neither moves — the property `04 §6`'s evidence chain rests on, since *"a Household must actually attempt the purchase and actually fail."*

## What would trigger revisiting

- ~~**`02 §4.3`'s worked example draws `{ scope = "local", resource = "money", amount = 1 }` and outputs no money anywhere**~~ **SETTLED, and this trigger is discharged.** The example no longer carries a money term, the root cause was a `[[resource]]` that declared no `family`, and a load-time refusal now enforces `0024` on every Rule's money terms in both directions. The re-reading this trigger asked for was done and this ADR survived it: a **transfer** names both ends and a **purchase** has no syntax, so both mechanisms stand, the Outside Connection being money's only sink. This ADR does not settle it: an authored money *cost* with a named counterparty and an implicit trade payment are two different mechanisms, and whether both should exist is a decision owed. Filed in [`plans/0011`](../../plans/0011-rule-engine-bins-and-rules.md) → *Decisions owed*. **If the answer is that explicit money terms stay, this ADR's *no syntax for payment* needs re-reading against them.**
- **A Good whose price a designer genuinely must author.** The claim here is that no such number exists. One would mean the emergent-price commitment in `04 §4` is narrower than stated.
- **A trade whose counterparty is not implied by the scope.** Every case reachable today — local, Pool, import — has exactly one counterparty. A scope with two would break the implicitness rather than bend it.
- **Measured price runaway despite the anchor.** The ceiling bounds the *local* price by construction; it does not by itself bound a Hinterland price that drifts. Under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md) *"the anchor bounds the arithmetic"* is **measurable** — the number is the largest money amount a single trade ever presents to a Bin — and **nothing has measured it**, because no priced Ruleset exists.
