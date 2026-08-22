# Freight is unbuilt, so the `min()` follows it and neither is at 12

**[`0088`](0088-the-price-of-a-far-hinterland-is-paid-in-your-own-traffic.md)'s
`min(declared ceiling, Segment capacity)` does not ship at milestone 12, and neither do Shipments.**
`06` placed freight at 12 and [`plans/0037`](../../plans/0037-goods-between-buildings-the-district-pool.md)
never scoped it; **`Shipment` has no implementation in `Borough.Core` at all**, so the `min()`'s second
operand is as vacuous at 12 as it was at 11. Both are **unplaced** and pinned to each other: *the `min()`
ships in the milestone that ships Shipments*, and that milestone does not exist yet.

**And the consequence 12 must state rather than discover: import is priced but not embodied**, so a Good
crosses the gate with no Vehicle and no congestion — `adr/0088`'s thesis is **deferred, deliberately**,
and a far gate costs nothing until freight lands.

`HONEST DEGRADATION` `LEGIBLE CAUSE` `SOLVE THE ACTUAL PROBLEM`

## Why

### `adr/0088`'s own test, applied to 12 instead of 11

Its 2026-08-20 amendment moved the `min()` from 11 to 12 because the second operand is
`RoadSegmentTable.CapacityPerDay` — **whole Vehicles per Day** — and at 11 no Good crosses, so the term
bounded nothing. The rule it stated is general: ***"A term that is vacuous on the world the milestone runs
on is not a diagnosis."*** Shipping it anyway *"would report **the ceiling binds** on every world in the
build, for a reason that is not about the gate"* — milestone 9's **F13** shape, *a hole that throws is
safe, one that returns plausible numbers is a working mechanism saying something false.*

**The same test at 12 gives the same answer.** `Shipment` appears **once** in `Borough.Core`, as a
doc-comment on `ResourceFamily.Good`: *"Moves between Districts as a Shipment — a Vehicle, on the Road
Graph, in the traffic."* **A description of intent with no table, no engine and no Vehicle.** Nothing
crosses a Segment carrying cargo at 12, so `CapacityPerDay` bounds nothing at 12.

### The dependency was misrouted, and that is why nobody noticed

`plans/0037` decision 6 says *"whether freight itself is in this milestone is decision 8, so this one is
downstream of it."* Decision 8 asked whether a **Provider** ships — a second `[[building]]` kind selling
into the Pool — and it was answered **yes**
([`0135`](0135-a-market-needs-two-sides-so-twelve-ships-a-provider-and-the-price-moves.md)). ⚠ **That
answers nothing about freight**: intra-District movement is *pooled*, which `adr/0013` defines in
opposition to a Shipment. ***A decision routed to the wrong upstream reads as answered the moment the
wrong upstream closes***, and this one would have been marked settled by a reader checking only whether 8
was done.

### `06` places Shipments at 12 and 12's scoping document never mentions them

`06`'s row: *"Shipments — freight Vehicles between Districts and to the gate — **Placed: 12**."*
`plans/0037` surveyed the milestone, found **three preconditions no document had listed as blockers**, and
enumerated **nine decisions**. Freight is in none of them. ***A survey looks for what its author suspects
is missing, and a mechanism placed by a different document is not a suspicion*** — which is why the two
disagree without either contradicting itself.

### This is Upkeep's shape, one decision later

[`0136`](0136-upkeep-has-three-blockers-landing-at-three-times-so-it-has-a-queue-and-not-a-milestone.md)
refused a third pin for Upkeep on the ground that *"a placement made against the blocker its author was
holding is not a schedule."* `adr/0088` moved the `min()` to 12 because *"the second term follows freight
to 12"* — **an assumption about where freight was, not a check on it.** Pinning it to a number again
would be the same move a third time. ***So it is pinned to the mechanism instead of to a milestone***,
which is the form that cannot go stale: freight and the `min()` land together or neither does.

### What 12 must say out loud, because it is not obvious

12 makes **import** real as a price: it is `adr/0045`'s ladder rung 4, and `adr/0135` authors the ceiling
it clears at. **Without freight, that import arrives with no Vehicle and no congestion.** So a Good comes
in from the far edge for the price and nothing else — and `adr/0088`'s whole claim is that *the price of
a far Hinterland is paid in your own traffic.*

🔴 ⚠ **The thesis is inert at the milestone that first makes imports real, and that is worse than it
sounds**: `adr/0088` withdrew `CONTEXT.md`'s *"at the cost of longer hauls"* and put traffic in its place,
so between 12 and freight there is **no cost to a distant gate at all**. **Stated here so that a reading
of a 12-era city does not conclude gate placement is free.** It is `adr/0013`'s *pool everything* failure
scoped to the gate, accepted deliberately and for one milestone's reasons rather than inherited.

## Rejected

**Ship gate-freight at 12 so the `min()` works.** The honest alternative, and it is refused on **size**
rather than on principle: freight is Vehicles, Stress accounting under
[`0007`](0007-stress-driven-simulation-detail.md) and Trip Fates, landing on a milestone already carrying
the District — *the largest unscoped piece in it* — a Provider with three content decisions, the
tâtonnement and a new Ruleset. ⚠ **And it would be gate-only**, because
[`0134`](0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)
gives one District per current world, so *between Districts* has nowhere to go. **Trigger for
reconsidering: the traffic-free import above proving misleading in a way a header cannot fix.**

**Ship the `min()` with a vacuous second term and document it.** `adr/0088` already refused this by name
at 11 and the refusal did not weaken.

**Leave `06`'s row at 12 and let it lapse.** The failure `adr/0136` corrected an hour earlier.

## Consequences

- **`06`'s Shipments row moves out of 12** and joins the mechanisms with no milestone, as does the
  `min()`. Both name **freight's arrival** as what clears them, not a number.
- **`adr/0088` is amended a third time, in place.** Its two-ceiling rule stands; only *when the second
  term ships* moves, and this time it is pinned to a mechanism so it cannot need a fourth.
- **`plans/0037` decision 6 is settled *no*, and its stated dependency is corrected** — it was downstream
  of freight, never of decision 8.
- **12 loses nothing it was going to demonstrate.** Its named risk is `Scope.Pool`; freight was a
  passenger, exactly as Upkeep was.
- ⚠ **`04 §4`'s *"the gap is what makes inter-District Shipments profitable"* now has two reasons to be
  unobservable at 12** — one District, and no Shipment. The two-settlement Ruleset fixes the first and
  **not** the second.

## What would trigger revisiting

- **Freight being built anywhere**, for any reason. The `min()` goes with it, and so does the
  which-of-the-two-binds readout, which is the part that is actually a diagnosis.
- **A 12-era city being read as evidence that gate placement is cheap.** That is the traffic-free import
  above being believed, and it would mean the header was not enough and the mechanism is owed sooner.
- **A second mechanism needing `CapacityPerDay` as a bound.** Two claimants make freight's absence a
  blocker on more than one thing, which changes its priority rather than its argument.
