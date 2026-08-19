# One Resource abstraction, and the constraint is depth not count

**Good, Utility, and Money are three families of one thing.** The abstraction is **Resource** — anything held in a Bin and moved by Rules — and the families are distinguished by a single axis: **whether moving it between Districts requires a Vehicle.**

The guard on the anti-goal changes with it. `04 §1` bounded the taxonomy by **counting** — *five Goods, and a sixth should replace something*. That is replaced by the constraint the same document already states and never enforced: **maximum chain depth of three.**

## What forced it

The Good abstraction had **four** escapees, each of which was given bespoke machinery:

| Escapee | Where | The exact words |
|---|---|---|
| **Money** | [`0024`](0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md), an entire ADR | *"conserved like a Good and not a Good: it needs no Shipment, occupies no Vehicle"* |
| Power, water, sewage | [`03 §1`](../03-agent-architecture.md) | *"network flow over a graph"* |
| Waste | `03 §1` | *"production → flow → treatment"* |
| Service capacity | [`04 §1`](../04-economy-and-goods.md) | *"coverage rather than a Good"* |

Four exceptions to a rule means the rule is wrong. And every one of them is excepted on **the same axis**: they are Good-shaped in every respect except transport. `0024` spent a full ADR proving Money is conserved-like-a-Good-but-not-a-Good, and that argument collapses to one boolean.

## The families stay named

The generalisation is at the **mechanism** level only. `CONTEXT.md` is domain language, not implementation, and *"they all sit in Bins"* is an implementation fact — flattening nine things into one player-facing list would make the model **less** legible, which is the opposite of the intent. A player thinks about a food chain and a power grid differently even when they compute identically.

So the move is the one `Zone` already makes: one concept, families that differ by what determines them.

| Family | Inter-District movement | Members |
|---|---|---|
| **Good** | a **Shipment** — a Vehicle on the Road Graph, contributing congestion | Produce, Food, Timber, Materials, Consumer Goods, **Waste** |
| **Utility** | flow along the District adjacency graph — no Vehicle, no congestion | Power, Water, Sewage |
| **Money** | none | — |

`Good` stops being the whole taxonomy and becomes the **transported** family. [`0013`](0013-goods-are-pooled-within-a-district-and-shipped-between.md) survives verbatim; `0024`'s central argument becomes a family membership; and **Waste answers itself** — it needs a Vehicle, so it is a Good, with no technicality required.

## Two parameters, and they are not the same field

- **Capacity** — the instantaneous ceiling on a Bin. Finite for everything physical; **unbounded** for Money, because no physical ceiling exists. Prefer an explicit *unbounded* to a large sentinel: a very large number pretending to be a bound will eventually be divided by a fullness gauge and answer meaninglessly. Write the Rule's test as `delta > capacity − level`, never `level + delta > capacity`, which overflows against a sentinel and silently inverts — a determinism bug in a system whose entire debugging story is the State Hash ([`0003`](0003-deterministic-integer-simulation.md)).
  > ⚠ **Both halves settled by [`adr/0065`](0065-a-bin-holds-a-long-and-unbounded-names-a-ceiling-whose-approach-is-a-defect-rather-than-a-refusal.md), and the first not as asked.** *Explicit unbounded* is **unachievable** — arbitrary precision means a managed type and lint 7 forbids one in the core — so the sentinel stays and is **named as a ceiling** at `long.MaxValue`, with its approach a defect the long-run test catches rather than a refusal. The fear was right and the escape happened: `BinTable.Create` dropped `IsUnbounded` and every consumer downstream saw only the number. The determinism half needed recording rather than deciding: the code has always written `capacity − level`, so nothing ever overflowed or inverted.
- **Storage** — whether a Bin carries over between periods. **Zero** for Power, which is what *"there is no electricity warehouse"* actually means; large for Water; filling for Sewage and Waste, where failure is at the **top** of the Bin rather than the bottom.

An earlier draft of this argument said *"Power has a Bin of capacity zero."* That was wrong — a Bin capped at zero can never be written, so generation would fail immediately. Capacity and storage are different fields, and separating them is what makes Money an **endpoint of a range the design already needed** rather than an exception: the bottom of *storage* was already required for Power.

## Depth, not count

The count rule cannot explain why Waste is fine and Steel is not; it can only observe that nine is more than five. The real anti-goal is stated in the same document — *"supply chains exist to create pressure that propagates into people, not to be an optimisation surface"* — and it is bounded by **chain depth**.

> **A terminal Resource, one hop from the edge of the graph, is nearly free. A link, with both an upstream and a downstream, adds a chain and is expensive.**

```
Produce ─▶ Food ─▶ Household              depth 2
Timber ─▶ Materials ─▶ Consumer Goods     depth 3   ← the ceiling
plant ─▶ Power ─▶ consumer                depth 1   terminal
consumer ─▶ Waste ─▶ treatment / export   depth 1   terminal
```

All four additions are depth-1 terminals. **The list grows from five to nine and the logistics complexity of the game does not change at all.** And the rule bites where the old one could not: `Ore → Steel → Materials → Consumer Goods` is depth four, over the ceiling, refused — and refused **with a reason**, which is the difference between a constraint and a quota.

## Consequences

- **Conservation becomes one invariant.** *Nothing is created or destroyed except at the gate* is now a single check across all nine Resources, catching a class of bug that previously needed separate Money and Goods tests. ✅ **BUILT 2026-08-19** (`06` milestone 10 task 4) as `Invariant.MoneyIsConserved`, in `02 §10`'s end-of-run tier — **over the conserved Resources only, and the qualifier is this ADR's own boolean rather than a shortfall**. A Good is conserved by nothing: it is produced from inputs and consumed into outputs, which is what a Rule *is*, so *created or destroyed* is not a failure for it and a check counting Goods would have to be a check about Rules. ⚠ **The gate does not exist yet** — money's only source and sink is the Outside Connection, which is milestone **11** — so it is an exact equality against a saved anchor rather than a sum with a flow term. ***The exactness is a property of the schedule and it expires***; the reading was taken while it holds.
- **Waste joins the balance of payments.** Paying the Outside to take your garbage is an ordinary import, priced, and a legitimate expensive strategy. `NO VERDICT`
- **`00-vision`'s oldest debt is paid.** It names CS1's failure as *"silent cap exhaustion — garbage piling up with no explanation."* Under Bins, garbage piling up **is** a Bin at capacity: visible, `Evidence`-expandable, and traceable to the Shipments a jam prevented.
- **`04-economy-and-goods.md` needs a wording pass**, and its five-Good rule needs replacing with the depth rule.
