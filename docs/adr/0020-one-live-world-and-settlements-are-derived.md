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

> **Exposure, recorded while grilling [`plans/0010`](../../plans/0010-s2-routing.md) and before any numbers exist.** This paragraph says **connected component** and **union-find**; `CONTEXT.md` → Settlement says *"mutually reachable"*. **Union-find computes weak connectivity and mutual reachability is strong connectivity**, and the two coincide only when the travel-time matrix is **symmetric**. If `volume / capacity` turns out to be per *direction* — still open, and `0010` R0 parameterises it — the matrix is asymmetric, and the headline case is the ordinary one: **inbound within Commute Budget at the morning peak, outbound not.** Union-find would then merge two Districts into a Settlement that is not mutually reachable, so Settlements would appear and merge for a reason the definition excludes. The fix is strongly connected components — Tarjan, still cheap, but not the claim made here. **`0010` R1 reports the distribution of `|matrix[i][j] − matrix[j][i]|` at peak and settles it either way**: negligible asymmetry means this paragraph survives on evidence rather than on an assumption nobody had noticed making.

> **AMENDED by S2 R1, on evidence. The exposure above was real, and the paragraph did not survive it.** *"A connected component of the District graph… a union-find"* is **not what [`CONTEXT.md`](../../CONTEXT.md) → Settlement defines**, and the two disagree about the city precisely where the city is fragmenting. Numbers in [`spike-results`](../spike-results.md) → *S2 R1.5*.
>
> **Six against eight, at the Budget where it matters.** At a 20-Tick Commute Budget union-find returns **6** Settlements where Tarjan's strongly connected components return **8**, and the largest component is **90** Districts against **70** — **a fifth of the map assigned to a Settlement it is not mutually reachable within**. At every looser Budget in the sweep the two agree exactly, so the practical consequence is smaller than the exposure sounds; it is also **largest exactly where the city is fragmenting**, which is the moment a Settlement is load-bearing rather than decorative. A mechanism that is only wrong when it matters is the same shape as the aggregate-attribution lag [`0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) rejects.
>
> **The instrument this exposure asked for was the wrong one, and that is the more useful half.** The paragraph above named the asymmetry *distribution* as the settling measurement. A distribution is a claim about travel times; a Settlement is an object the game is made of, and a Building's Trips fail or do not depending on which algorithm is right. **The test is to run both and compare the cities**, which is what R1.5 did. A negligible distribution would not have vindicated union-find either, because the question is whether *any* pair straddles the Budget rather than whether the typical pair does.
>
> **The exposure is a band, not a threshold, and no Commute Budget closes it.** A pair is one-way only while the Budget sits between its two directions' costs, so a Budget below every commute produces none and a Budget above every commute produces none either — the one-way count rises to **264** and falls back to **47** across the sweep. **A Budget generous enough to close the gap has stopped bounding anything**, which is the one thing `CONTEXT.md` → Commute Budget exists to do.
>
> **What is corrected is this paragraph's algorithm, not the definition — and the correction is downstream of a decision still open.** Under **per-Segment** volume the two directions of a Segment share one counter, the matrix is symmetric to the bit at every imbalance, and union-find is right *by construction* rather than by evidence (R1.4). This ADR is therefore exposed only under **per-direction** volume — which is the scope `CONTEXT.md` → Lane, `adr/0007`'s Stress and Settlement's own *mutually* each separately require, and R1 records that per-Segment scope makes all three vacuous at once to save 5% of a Road Graph that is 1.2% of the world. **The scope is not settled here**; it is [`plans/0002`](../../plans/0002-open-questions.md)'s. But on the scope the rest of the corpus requires, a Settlement is a **strongly** connected component, and union-find computes weak connectivity. Tarjan is still cheap and still runs over data already being maintained — it is simply not the claim this paragraph made.

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
- **The volume-scope decision landing on per-Segment**, which is the one outcome that discharges the amendment above rather than acting on it. A shared counter makes the matrix symmetric to the bit, so weak and strong connectivity coincide and this ADR's original union-find is correct without a line changing. **It would be correct for a reason the ADR never gave**, so it should be re-stated as *"symmetric matrix, therefore union-find"* and not left resting on the coincidence.
- **The matrix's time resolution landing on a Day average** (`plans/0010` decision 2a). R1.8 measured a Day average reporting **1** one-way District pair where the morning peak has **76**, because the two peaks are opposite in sign and cancel. That would shrink this exposure almost to nothing — **and it must not be read as vindication**, because the asymmetry would have been averaged away rather than found absent. If resolution is settled first, this amendment is re-measured at the peak regardless of what the Day average says.
