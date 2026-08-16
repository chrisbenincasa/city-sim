# The price of a far Hinterland is paid in your own traffic

**An Outside Connection is an ordinary Building on a map edge, and choosing a distant one costs
congestion rather than distance.** There is **one abstraction**: road, rail and port are `[[building]]`
kinds differing in throughput ceiling, price and which Goods they favour, and in the **mode mask**
([`0072`](0072-the-mode-mask-is-saved-on-the-arc-and-the-segments-is-derived.md)) their Access Point's
Arc carries. The **generator places a small number** at plausible edge locations and the **player may
build more at cost**. Its throughput is the **lower** of its declared ceiling and the capacity of the
Segment its Access Point sits on. `CONTEXT.md` → Hinterland's *"at the cost of longer hauls"* is
withdrawn — under [`0085`](0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md) the
far edge is under forty free-flow minutes away — and what replaces it is better: a haul from the far edge
crosses your city and pays for the crossing in volume on every Segment it uses.

`LEGIBLE CAUSE` `EMERGENCE` `PLAYER GOVERNS`

## Why

### The representation question was answered by a milestone nobody connected to it

`CONTEXT.md` → Outside Connection has said *"a special Building at the map edge"* since it was written,
and the corpus has treated the layout as an open design question anyway. It is not, any more. Milestone
5a-bis gave **every Building an Access Point** — an Address of `(Segment, offset, side)` — carved against
a Lot's Street frontage, and [`0031`](0031-one-resource-abstraction-and-depth-not-count.md) gives every
Building its kind's Bins. Between them, everything an Outside Connection needs already exists:

| What it must do | What supplies it |
|---|---|
| Absorb surplus Goods and supply deficits | Bins, per `0031`, declared by its `[[building]]` kind |
| Be somewhere, so Shipments and Trips can reach it | its Access Point, per 5a-bis |
| Be reachable by some modes and not others | the mode mask on its Access Point's Arc, per `0072` |
| Bound arrivals by throughput | a declared ceiling, and the Segment it sits on |
| Have prices | its **Hinterland**, which is per **edge** |

**No new table, no new column, no new mechanism.** It is a Building kind with an unusual Bin set and a
position constrained to an edge. That is worth stating plainly because *"Outside Connection layout"* has
been carried in `plans/0002` as an open fork for long enough that it reads as a subsystem.

### The edge is the whole of its economy, so "where on the edge" is a road question

`CONTEXT.md` → Hinterland is unambiguous: *"the economy behind one map edge, **shared by every Outside
Connection on that edge**."* Prices, wages, rents and population composition are properties of the edge
and of nothing finer. So the four-way question ledger #3 asks — how many, where on the edge, who places
them, whether the modes are distinct — splits cleanly, and only one part of it is economic:

- **Which edge** is the economic decision. It selects a market.
- **Where along that edge** selects nothing economic at all. It selects which Segment carries the freight,
  which is a Road Graph decision the player makes with the same verb they use everywhere else.
- **How many on one edge** buys throughput into a market already being drawn down, exactly as
  `CONTEXT.md` says, and buys no new prices.

That is a real simplification and it comes from taking the Hinterland's own definition seriously rather
than from a new argument.

### The friction the design claimed was distance, and there is no distance here

`CONTEXT.md` → Hinterland closes on the sentence that makes edge choice a decision: *"a port on the far
edge buys a different economy, **at the cost of longer hauls**."* `0085` measured what a long haul costs
on this map. Corner to corner is **158–224 Ticks** at free flow — under forty clock minutes, 2.7% of a
Day — because the map's diameter was priced with `02 §1.2`'s 0.5 Tile/Tick and the real figure is 36.6.
**A tariff of forty minutes on a permanent import relationship is not a decision**, and if edge choice
cost only that, a mature city would simply trade with whichever of the four Hinterlands is cheapest and
the other three would be scenery. That would hollow out *"four comparable markets are each other's
referent"*, which is the one thing making the Outside legible.

Two ways out, and the second is the one the design already had.

**A distance tariff is refused.** Pricing a Shipment by Tiles travelled is a number nobody can read off
anything, it is not the currency the player is judged in, and it would sit alongside congestion pricing
the same journey twice. It is the SC4 failure `CONTEXT.md` → Commute Budget exists to prevent, arriving
through the freight door: *optimising for distance while scoring on time.*

**Congestion is the friction, and it is already built.** `CONTEXT.md` → Good makes a Good's movement *"a
**Shipment** — a Vehicle on the Road Graph, contributing congestion."* A haul from the far edge is a
Vehicle on every Segment between there and the consumer, raising volume on all of them, for the whole
journey, every time. On an empty map that is nearly free and it **should** be nearly free — a small city
with clear roads genuinely can import from anywhere. On a mature city it is the dominant cost, it is
concentrated on exactly the corridors the player can see, and it scales with import dependence, which is
[`0022`](0022-land-is-a-stock-the-city-spends.md)'s macro-arc.

So the mechanism survives and its ground is replaced, which is the third time in this sitting. It is also
a **better** ground on the design's own terms: *"the port on the far side is strangling the east–west
arterial"* is a `LEGIBLE CAUSE` sentence with Segments to point at, and *"hauls from the far edge cost
more per Tile"* is a line item.

### Throughput is two ceilings and the binding one is the diagnosis

`CONTEXT.md` → Outside Connection says throughput is *"infrastructure the player built, not a constant
someone chose."* Taken literally that is only reachable if the gate has no ceiling of its own, which
cannot be right — a single-track rail head is not a motorway however well the player connects it. Both
bounds are real and the resolution is to keep both and say which binds:

```
throughput = min(the kind's declared ceiling, the Access Point's Segment capacity)
```

The declared ceiling is Ruleset data on the `[[building]]` kind and is what distinguishes a port from a
lay-by. The Segment capacity is the Road Graph's, already stored, already the thing Stress reads. **Which
of the two is binding is the whole readout**: *your port is at capacity, build another* and *your port is
fine and the road to it is not* are different problems with different fixes, and a single number would
report them identically. `HONEST DEGRADATION`

This is also what makes `04`'s Office claim true without a rule: *"a metropolis cannot export through one
lane"* is the second term binding, and nothing has to be written for it to happen.

### Generator places, player extends, and the reason is the unlock rule

The recommendation ledger #3 carried — *"generator places a small number at plausible edge locations; the
player may build more at cost"* — is adopted, and it acquires a reason it did not have. Under
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) the map is bounded and
procedural and terrain is generator output, so a river mouth or a rail alignment is a fact about the
world the player did not choose; siting the initial gates on those features is the generator saying what
the ground is for. And `01 §8 Q3`'s unlock-by-serviceability means an edge the player has not reached is
not a connection, so **the generator's placements are opportunities rather than endowments** — the first
one works because the city starts near it, and the other three are things to go and get. That is the
pacing job ledger #1 handed to Hinterlands, landing on the object that carries it.

Player-built gates cost money and are permanent infrastructure, which under `0022` is what the late game
is short of.

## Consequences

- **`CONTEXT.md` → Hinterland's closing sentence is amended.** *"At the cost of longer hauls"* becomes the
  congestion statement. The rest of the entry — four edges, four drifting markets, the stock the city
  spends — is untouched.
- **`CONTEXT.md` → Outside Connection gains the two-ceiling rule** and the statement that it is an
  ordinary Building, so a later reader does not re-open the representation.
- **`plans/0002` ledger #3 closes.** All four of its sub-questions are answered above; what remains is
  numbers, below.
- **Three unset hash-bearing numbers, and they are gaps rather than debts** (`0002` §D2): a kind's
  **throughput ceiling**, its **price offset** against the Hinterland's, and the **count and siting** the
  generator uses. Named ratifier for all three is the first Ruleset that models a city with an Outside
  Connection in it, which is milestone **8**'s. Nothing accretes on them until then, and `adr/0052` is
  satisfied by naming the ratifier rather than by choosing.
- **Milestone 8's scope is smaller than the roadmap implies.** It is a `[[building]]` kind, a Bin set,
  an edge constraint on placement, and a `min()`. The subsystem it looked like is `0031`'s Bins and 5a's
  graph, already built.
- **Freight's contribution to Segment Stress is now load-bearing for the economy**, not only for traffic.
  `plans/0002` design fork 14 — *whether freight vehicles contribute to Segment stress identically to
  commuters* — was a traffic question and is now also the thing that prices the Outside. It should be
  re-read with that in mind; this ADR does not settle it, and a weighting of zero would delete the
  friction argued for above.

## What would trigger revisiting

- **Freight turning out not to contribute congestion.** The entire replacement friction rests on a
  Shipment being a Vehicle on the Road Graph. If freight is ever moved to an abstract flow for
  performance, edge choice becomes free again and a distance tariff has to be re-argued from a worse
  position. Under [`0033`](0033-two-rule-families-scheduled-and-swept.md) that
  move is a change to the city and not an optimisation, which is the guard.
- **Congestion proving too weak at the scales that matter.** This is measurable and nothing has measured
  it: the question is what share of a mature city's Segment volume is freight from the farthest edge, and
  it needs Shipments, which no milestone builds yet. If the answer is a few percent, the friction is
  nominal and edge choice is decorative after all.
- **A fifth market.** Everything here assumes the Hinterland is per-edge and there are four. An offshore
  or overseas market not attached to an edge would break the *edge is the identity* simplification, and
  `CONTEXT.md`'s four-referents argument is what should be weighed against it.
- **A gate needing to be somewhere other than an edge.** An airport is the obvious candidate and it is
  not an edge object. It would be a different decision, not an extension of this one.
