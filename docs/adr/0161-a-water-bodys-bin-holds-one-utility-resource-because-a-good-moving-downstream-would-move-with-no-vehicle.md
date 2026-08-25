# A Water Body's Bin holds one Utility-family Resource, because a Good moving downstream would move with no Vehicle

**A Water Body's Bin holds exactly one Resource, and its family is `Utility`.** Dumping does not put a
Good in it. What a Rule dumps into water **becomes** the Utility the water carries, in the same way a
Rule that emits into a Map Layer does not put a Good in a Cell. **`ResourceFamily` gains no member and
`CONTEXT.md` → Water Body is confirmed rather than corrected.**

Guiding concepts: `LEGIBLE CAUSE`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
No measurement distinguishes the two answers — both produce a number in a Bin — and the facts it rests
on are [`0031`](0031-one-resource-abstraction-and-depth-not-count.md)'s own definition
and `CONTEXT.md`, both of which were read rather than inferred.

## Why

### The question `plans/0042` decision 12 left open

Decision 12 settled the **taxonomy**: `adr/0031` leaves three families, **Waste is a member of Good**
and **Sewage a member of Utility**, and `CONTEXT.md` → Water Body's *"the Waste family"* named a family
that does not exist. It deliberately did **not** settle a narrower question it had just created:

> `02:256` gives water pollution two sources, *"dumping, runoff"*, and dumping is plausibly refuse — a
> **Good**. A Good sitting in a Water Body's Bin and moving downstream would be a Good moving with no
> Vehicle, contradicting the one axis `adr/0031` uses to define it.

### The objection in that paragraph is decisive, and it decides against the Good

`0031`'s axis is a single question: ***does inter-District movement require a Vehicle?*** A Good's
answer is yes — that is what a Shipment is, and it is why a Good's movement congests roads. A Water
Body moves its contents **along an edge of the water graph**, downstream, with no Vehicle, no Trip and
no Segment.

So a Good in a Water Body's Bin is not a modelling awkwardness. **It is a counterexample to the
definition of Good**, sitting inside the build, which would leave `0031`'s axis describing every
Resource except the ones in the water. ***A taxonomy with one exception in it is not a taxonomy.***

### Dumping is a transformation, and the Map Layer precedent is exact

The Good reading survives only if *the refuse a lorry tips into a river is still refuse once it is in
the river*. It is not, and the build already says so somewhere else.

`Scope.Map` is **write-only**, and its doc-comment gives the reason: *a Layer cell has no capacity to
exceed, so a map output can never fail.* A Rule that emits industrial pollution is not moving a Good
into a Cell — **it is converting a Rule's operation into a quantity of a different kind**, held by a
different mechanism, obeying different movement. Nobody has ever suggested the pollution in a Cell is
the coal that produced it.

**Water is the same shape with a capacity added.** What crosses the waterline stops being a Good and
becomes the one thing the water carries. The Bin is what gives it a capacity to exceed, which is the
whole of the difference from a Layer, and is why `CONTEXT.md` says *nothing is an infinite sink*.

### One Resource and not several, because a taxonomy of water types is what this design refuses

`CONTEXT.md` → Water Body is explicit that **two parameters produce every behaviour with no taxonomy of
water types** — a pond fills, a river exports, a sea absorbs and still fills, and the difference is
capacity against outflow rather than a category. ***A second Resource in the Bin would be the taxonomy
arriving through the other door***: two levels to compare, two shoreline intensities to weigh, and a
weighting nobody has argued for.

⚠ **This is a decision about the Bin and not about what a Rule may name.** A Ruleset stays free to
declare as many Utility Resources as it likes; what this refuses is a Water Body holding more than one
of them.

## Consequences

- **`ResourceFamily` gains no member**, as decision 12 already concluded for its own reason.
- **`CONTEXT.md` → Water Body is confirmed.** Its *"a Bin holding a Utility-family Resource"* was the
  authoritative sentence throughout; `plans/0042`'s task 6b row was the stale copy, and it is the copy
  that said the question was open. ⚠ **`plans/0012` Cause 1 exactly** — two documents holding one fact,
  and the one that drifted is the one that also stores status.
- **Milestone 24 task 6b is unblocked on its design question** and owes only its two numbers.
- **Dumping needs a Rule that can reach a Water Body, and no such Scope exists.** That is
  [`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s *unbuilt*, named here so that
  nobody reads this ADR as having shipped an inflow. 🔴 **Until it exists every Bin's level is zero on
  every shipped world**, which is what the shoreline term at task 7 would read.
- **The runoff inflow is addressed through the catchment** (`plans/0042` **F14**), and dumping through
  proximity. Neither is built.

## What would trigger revisiting

- **A mechanism appears in which water moves something that a Vehicle also moves.** Barge freight is
  the obvious one — a Good travelling *on* water rather than *in* it — and it does not contradict this
  ADR, because the Good would be in a Vehicle's Bin and not in the Water Body's. If a design ever puts
  cargo in the body itself, this reopens.
- **A second thing a Water Body must hold appears with an argument behind it** — salinity, temperature,
  a fish stock. Each is a second level, and this ADR refuses them collectively rather than
  individually, so any one of them with a case reopens it.
- **`adr/0031`'s axis changes.** The whole argument is that a Good is defined by needing a Vehicle
  between Districts; a different axis would make a Good in water unremarkable.
