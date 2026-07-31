# One live world, and Settlements are derived from commute range

**There is one world, one Tick counter, and everything in it is simulated all the time.** There are no separately-saved city tiles, no frozen neighbours, and no whole-city fidelity tier. What replaces the region is a **Settlement**: a maximal set of Districts mutually reachable within the Commute Budget, computed from the travel-time matrix rather than drawn by anyone.

The region view is therefore not a menu of tiles the player chose. **It is a diagram of the commute sheds the city actually has.**

## Why the SimCity 4 region model is unavailable

Not on effort grounds. It is foreclosed by decisions already taken.

In SC4's model the tile being played runs while its neighbours are frozen, and their time advances by fiat when the player switches. **That is a second clock**, and worse than the one [`0010`](0010-one-clock-and-demographics-by-sorting.md) rejected: the conversion factor between city A's elapsed time and city B's is not even constant, because it depends on where the player has been looking.

Two consequences follow immediately:

- Every causal statement crossing a tile boundary needs that factor to be literally true. *"The mill closed because the neighbouring town stopped buying"* is unanswerable when the neighbouring town has been paused for six hours. `LEGIBLE CAUSE`
- Simulation state becomes a function of **observation**, which is exactly the property [`0007`](0007-stress-driven-simulation-detail.md) was rewritten to eliminate.

The alternative repair — neighbours running at reduced fidelity rather than frozen — is a whole-city statistical tier, which is precisely the Cohort representation [`0005`](0005-two-fidelity-tiers.md) deleted, with the same unverifiable calibration and the same exploitable boundary.

And if the tiles all tick together and interact live, nothing has been built except one map with extra bookkeeping. So the region model resolves either into a second clock or into this ADR.

Worth noting that SC4 froze its neighbours because a 2003 machine could not simulate nine cities at once. The constraint produced the design; the design was never the goal. Cities: Skylines, the genre's next major entry, dropped regions in favour of one map with progressively purchased area.

## What a Settlement is

A **Settlement** is a connected component of the District graph, where an edge exists between two Districts if the travel-time matrix puts them within the Commute Budget of each other. It is recomputed when the matrix rebuilds — a union-find over data already being maintained, at effectively no cost.

Nothing about it is authored. Settlements **appear, merge, and split** as consequences:

| Event | Cause | What the player sees |
|---|---|---|
| A Settlement appears | Jobs cluster somewhere out of range of the existing centre | A second downtown, on the region map |
| Two Settlements merge | A new road brings them inside the Budget of each other | *"Northfield and Millbrook merged when Ridge Road opened"* |
| A Settlement splits | Congestion pushes travel times past the Budget | The city tearing in two, traceable to specific Segments |

The third is the one that could not exist under a fixed tile grid, and it is the reason contiguity was chosen over the archipelago variant recorded in [`deferred.md`](../deferred.md).

## Why this is better than what it replaces, not merely equivalent

**Specialisation acquires a cause.** In SC4 the farm tile was agricultural because the player decided it was — a convention with no mechanical basis. Here a farming Settlement is agricultural because nobody living there can reach a city job, so it must grow its own labour force and its own small centre. The specialisation is *forced by geography* rather than declared, which is the same upgrade the Unplaced Pool made over the RCI meter: a diagnosis instead of a label. `EMERGENCE`

**The fidelity saving the region model was reaching for already exists.** A quiet farming Settlement has no congested Segments, so all of them are Statistical, so its Travellers are Event Wheel entries costing nothing until they arrive. It is not unloaded — every Household is real and fully simulated — and it still costs near zero. [`0007`](0007-stress-driven-simulation-detail.md) delivers "quiet places are cheap" continuously and as a function of *whether anything is happening*, rather than of whether the player is looking.

## Consequences

- **`01-player-experience.md` §8 Q3 is not an independent question.** "Open map versus progressive land unlock" is the same question as this one and is resolved in the same place.
- **One world is one save.** A portfolio of independently-loadable cities is not available, and neither is sharing a single city tile with other players as SC4 allowed. This is the real loss and it is accepted.
- **The whole world shares one population and performance budget.** Not unbounded. Where the ceiling sits is measured by spike **S2**, because it is a routing-throughput question — see below.
- **The region view is UI over derived state**, so it costs a camera and a stats panel, not a subsystem.
- **Settlement is a reporting and diagnosis unit, not a simulation unit.** Nothing pools by Settlement, nothing is budgeted by Settlement, and no Rule reads one. Districts remain the granularity of Goods pooling and the travel-time matrix. If a Settlement ever acquires mechanical authority, this boundary should be re-examined deliberately rather than by drift.

## Where the ceiling actually is

Working through which resource binds first produced a result worth recording, because it is not the obvious one:

| Resource | Scales with | Binding? |
|---|---|---|
| Map extent | developed area, if Chunks are stored sparsely | No — see [`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) |
| Citizen and Household records | population | No — tens of MB at 100k |
| Entities woken per Tick | population ÷ `TICKS_PER_DAY` | No — ~120/Tick at 100k |
| Vehicles on Microscopic Segments | **nothing** — capped by the fidelity budget | No, by construction |
| Travellers on Statistical Segments | population, but ~0 CPU until arrival | No |
| **Route computation** | **population ÷ `TICKS_PER_DAY`, before cache hits** | **Yes** |

[`0007`](0007-stress-driven-simulation-detail.md) converted traffic simulation from *O(population)* to *O(microscopic budget)* — a bounded constant. That decision was taken for honesty reasons and turns out to be what makes scale affordable at all.

What remains linear in population, and expensive per unit, is **route computation** — which is precisely the failure that collapsed Cities: Skylines 2, where player instrumentation traced sim-speed collapse directly to pending pathfinding query count. **The map-size question is the routing question in disguise**, and it is already named as the most consequential open technical question in [`05-technical-architecture.md` §11](../05-technical-architecture.md).

No population ceiling is asserted here. Estimating one before spike S2 would be writing a guess in a place that reads as a commitment.

## What would trigger revisiting

- **S2 showing routing throughput is far worse than expected**, such that a single live world cannot hold a city worth playing. The recovery is not regions — it is the mitigations already in the design (travel-time matrix absorbing accessibility queries, epoch-based path caching, routing on a worker pool). Regions would still be a second clock.
- **Playtesting showing the ritual of starting a fresh tile matters more than the merge/split behaviour.** That pushes toward the archipelago variant in `deferred.md`, which keeps one Tick and disjoint sub-maps and loses only merging.
