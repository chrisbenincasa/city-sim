# Routing never keys on the District

**The District is where Goods pool. It is not where routing happens.** Two applications, and they settle four open ledger entries between them:

- **The travel-time matrix's granularity is the routing partition, not the District** — [`adr/0040`](0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md)'s cluster, **with its size expressed in Cells rather than in Chunks** for the reason below. The size itself is **measurable and unset**.
- **A route is `(Segment, offset) → (Segment, offset)`, served from the route cache.** The maintained District-granular **next-hop table is out**, on four grounds and not one of them is cost.

`PLAYER GOVERNS` `LEGIBLE CAUSE` `BOUNDED KNOWLEDGE` `SOLVE THE ACTUAL PROBLEM`

## Why

### The welding, and it is the third instance of a pattern the corpus already names

[`CONTEXT.md`](../../CONTEXT.md) → District: *"the boundary within which Goods pool without physical transport, **and** the granularity of the travel-time matrix."* That **and** is doing unargued work, and the give-away is where the number comes from. The same entry bounds a District's extent by *"the area within which ignoring transport is a defensible simplification"*, anchors it at 128 Cells, and says plainly that *"what actually pools convincingly is a playtesting question, and the number should be expected to move."*

**So routing's granularity — and therefore its error — is a side effect of a Goods playtest.** Nothing in that derivation consulted routing, and S2 R1 has since measured what the error does across the sweep: **24.70% down to 3.80%.** The corpus has had that curve since R1 and could not use it, because the axis was owned by another mechanism.

[`05 §5`](../05-technical-architecture.md) states the rule this violates — *"a constant welded to two decisions is governed by whichever of them is louder"* — and `adr/0040` counted five instances. This is the seventh, after `adr/0034`'s Chunk case and `adr/0040`'s own.

### [`adr/0041`](0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md) already took this decision once, for the other half of the same object

Its reason transfers verbatim: *"redrawing a boundary changes volume attribution → Stress → Fidelity → travel times → the city, and therefore the State Hash… `PLAYER GOVERNS` means the player governs the city, not the physics."*

`plans/0010` decision 10 recorded that **both surviving path-source rungs put it straight back** — a shared route is keyed by District pair, a next-hop column is a tree rooted at a District's representative — and did not take the decision, because it belonged with `02 §2.1` and `03 §3.9` rather than inside a spike. It belongs here, and the answer is the one `adr/0041` already gave.

### The table is out on four grounds and not one of them is speed

S2 R8 supplied two of these and they are the ones that decide it.

| | The District-granular next-hop table |
|---|---|
| **Structural error** | 16.58% mean detour on the uniform draw, **149.73%** on the local one — fixed, visible every Tick, and unmoved by a storm deleting 1,021 Segments (R5.5) |
| **Concentration** | **87.25% of all traffic on the busiest 1% of the road, 90.87% of it empty**, at 13% of holding capacity with capacity confirmed realistic. One tree per District means **one route per (node, District) pair in the entire model** (R8.0) |
| **Physics** | It is the mechanism `adr/0041` refused, restored one layer up |
| **Capacity** | A tree holds **one** route to a place; a cache holds many. Only one of the two can disperse traffic at all |

Its one real advantage — a mid-journey diversion is free, at **3.18–3.45%** of the Tick budget against a stored route's **3,951%** (R8.6) — is an argument for making the cache work, not for keeping the tree. And R8 established that Sight makes diversion *routine* rather than exceptional, so that column matters more than it did, not less.

> **⚠ AMENDED 2026-08-11 — the figure is a range, and it was published as a point estimate.** This sentence read **3.18%** until S2 R7 took a second canonical R8 capture on a matched machine. R8.6's next-hop table read is **391 ns** in the 2026-08-07 capture and **424 ns** in the 2026-08-11 one, which is **3.18%** and **3.45%** of the Tick budget. **Nothing in the decision moves**: the whole force of the row is the comparison against a stored route's 3,951%, and at three orders of magnitude either reading is the same sentence — which is exactly why the point estimate survived unexamined.
>
> **It is amended anyway, and the reason is the rule rather than the number.** S2 R7 established that **a maximum, a point estimate and a spread are three different claims**, after finding the routing row of [`0013`](../../plans/0013-tick-budget.md) quoting `10.37 ms` — a *maximum* with a 9.37–10.51 ms span — as though it were a point. The two captures that produce this range are the first **matched-machine** pair S2 has ever had (CPU stall 0.13% against 0.11%), and they agree on 724 of 744 cells within ±2% with every non-timing figure bit-identical, so **3.18–3.45% is the honest width of a timing figure on this harness** and not evidence of instability. A figure quoted from one run is an assertion whatever its precision.

**The table was never a path source. It was the fallback if the cache did not work.** [`plans/0010`](../../plans/0010-s2-routing.md) R3 already said a cache is *"one of only two exits — the other being to spend eight cores' whole Tick budget on routing"*, and this removes the third option nobody had noticed was being held in reserve. **R6 is now the only exit**, which raises its stakes rather than lowering them.

### The dependency argument, which is what actually decides the matrix

`06` still has **player-drawn versus automatically derived Districts** open, blocking milestone 5c. Keying the matrix on the District silently makes that a *routing* decision too: if Districts are player-drawn, redrawing a boundary re-partitions the matrix, changes what Households estimate, and changes which jobs they take. The game then cannot answer *why did this Household change jobs* — the honest answer is *you moved a line* — which is `LEGIBLE CAUSE` failing on a verb the player operates directly.

**Decoupling means `06` may settle Districts on Goods and player-agency grounds alone**, which is where that argument belongs, and routing stops betting on its outcome.

### The size must be in Cells, and that is a correction `adr/0040` is owed

`adr/0040` split the cluster from the Chunk on a **permanence** axis — the Chunk is in the save, the cluster is `(derived AND rebuilt)` — and sized it as *"a whole number of Chunks"*. It did not run the **hash** test on the dependency that creates. The Chunk is declared *"tuning, hash-preserving"*, so anything hash-bearing expressed in Chunks makes turning that knob change the city.

**This is live today and this decision did not create it**: HPA\*'s routes depend on cluster size, R3 measured their detour, and different routes are a different city under `05 §4`. Keying the matrix on the cluster only makes it louder.

The repair is small and lands where `adr/0040` already says the check belongs — *"with the world-creation constants, validated where `TICKS_PER_DAY` and `WHEEL_SIZE` are."* **The routing partition's size is a multiple of the Cell**, which is frozen; **Chunk size is constrained to divide it**, which preserves `05 §5`'s alignment argument and the shift-not-conversion property in both directions. Chunk stays hash-preserving, at the cost of being tunable only to divisors — a real restriction and a checkable one.

### What is argued here and what is not

| Claim | Type | Where |
|---|---|---|
| Routing must not key on the District | **arguable** | settled here |
| The next-hop table is out | measured | R5.5, R8.0, R8.6 |
| The routing partition's size must be in Cells, not Chunks | **arguable** | settled here |
| **What that size should be** | **measurable** | **unset** — R1's entry-error curve re-read at routing granularity, with the route store out of the denominator |
| What the route cache's key granularity should be | **measurable** | **R6**, and it is a *different* number with a *different* error |

## Rejected

**Keeping the matrix on the District.** Its best case is real and should be recorded rather than caricatured: a matrix is what a Citizen has been *told*, `BOUNDED KNOWLEDGE` describes exactly that, knowledge plausibly does follow neighbourhood structure, and the District is already the unit the player names and reports against. It costs nothing today and adds no concept. **It loses on two asymmetries.** Reversibility: a routing partition that turns out always to equal a District collapses into one, whereas introducing a partition *late* lands after Settlements, Policy scope, saves and Goods have all assumed a single one — and matrix granularity is hash-bearing, so it is a design change and not an optimisation. And dependency: it makes routing wait on `06`'s open question.

**Multiple representatives per District.** This was the shape decision 11 had been arguing for, and R8 measured that it addresses the wrong object: the representative **funnel does not bind** — excluding it, and then a four-Segment convergence zone around it, gives readings identical to the printed digit. The binding term is the **tree upstream**, and a District with a hundred access nodes still has one shortest-path tree per destination.

**A Segment-granular tail on a District-granular route.** Same objection: it repairs the funnel, which is not what concentrates the traffic.

**A new *Routing Zone* concept.** Rejected in favour of reusing `adr/0040`'s partition, which was created for exactly this purpose and is already sized by routing measurement. Inventing a second routing partition would be the welding failure committed deliberately.

## Consequences

- **`CONTEXT.md` → District loses a role** and keeps the rest. It is the Goods pooling boundary, the Policy override scope, and a named region — and it is **not** the granularity of the travel-time matrix.
- **`06`'s District question is decoupled** and may be settled without reference to routing. `plans/0010`'s standing note that S2 *"does not choose Districts"* stops being a limitation and becomes correct by construction.
- **Ledger entries 10, 11, 13 and 15 close together**, which is what they were circling. **Decision 8 does not** — see below; it turned out to be a different question.
- **`adr/0012`'s owed amendment is half discharged.** The cache's **key** is an origin-destination pair at a granularity **routing owns**, never the District. The **eviction policy** remains owed to R6, with R5's evidence that it is the bigger lever below the highest edit rates — 28–31% of lookups missing on direct-mapped collisions before a road is touched.
- **The matrix's ceiling changes, and in the useful direction.** R1 found the binding constraint was not L3 but the **route store — 4.06 GiB at 4,096 zones against a 172.3 MiB world.** That store existed to hold a *route* per pair. With routes served from the cache the matrix holds only *times*: 4,096² × 4 B is ~67 MiB and 1,024² is ~4.2 MiB. **The thing that capped matrix granularity is the thing this decision removes**, so R1's error curve becomes usable for the first time.

  > ⚠ **AMENDED 2026-08-14 by 5c task 4, and the amendment is that this bullet stops at the matrix.**
  > It is correct about the **matrix** — which holds times, is ~8.3 MB at 1M, and whose error 5c task 2
  > measured. What was never checked is the other half of the sentence: *"served from the cache"*.
  >
  > **The measurement that matters is not memory, it is what a key on *pairs* buys over a key on
  > *travellers*, and this ADR's sibling said it could not be taken.**
  > [`adr/0012`](0012-routing-intent-lives-in-the-agent.md): *"the price of the key is settled exactly
  > and the benefit cannot be settled at all until Trip generation exists (`06` 5b)."* It exists. On a
  > real commute draw the share of commutes that **share a node pair with another commute** is
  > **17.78% at 4,000 Citizens and 7.52% at 16,000** — *falling as the city grows*, because the paved
  > extent grows with it. **That is the whole of what a shared pair-keyed store can buy**, and it is
  > small and shrinking.
  >
  > **And the access pattern defeats the eviction policy this ADR's sibling specifies.** A commute is a
  > **once-per-Day cyclic scan** — every employed Citizen departs once a Day and `CommuteRoster` fixes
  > the order — which is the pattern LRU is provably worst on. At 16,000 Citizens with a 1,024-entry
  > store against a **21.30%** ceiling: LRU **2.83%**, Random **3.79%**, MRU **19.54%**, refuse-to-displace
  > **22.41%**. Random fails because a four-way set churns out even under a random victim; it is the
  > fully-associative case the textbook result is about.
  >
  > ⚠ **What this amendment does NOT carry is a memory figure, and an earlier draft of it did.** That
  > draft extrapolated a route length from a **maximum** where memory scales on a median, and multiplied
  > it by an employment ratio taken from `rulesets/minimal.toml` — a file whose own header says it models
  > no city. Both are withdrawn. ***An extrapolation is a claim about a mechanism, not about a curve***:
  > foot route length is capped absolutely by the Commute Budget (50 minutes at 5 km/h is 4.17 km, about
  > 32 blocks) no matter how large the city grows, and the draft's fitted curve ran straight through
  > that ceiling. **The car distribution — which is the one this milestone is about — does not exist
  > until 5c task 5 builds a drive Leg.** Where routes live at 1M is [`0002`](../../plans/0002-open-questions.md)
  > §C and is **not answerable today**.
  >
  > ⚠ **The four grounds for retiring the next-hop table are untouched and must not be reopened on any
  > of this.** None of them was cost, and none of them was a hit rate.

## What would trigger revisiting

- **`06` settling Districts as automatically derived and at roughly routing granularity.** The distinction would then be one nobody could observe, and collapsing it is a deletion rather than a migration. **This is the trigger this ADR most expects to fire, and it is why the decision was taken in this direction rather than the other.**
- **R6 failing to make the cache affordable.** The table was the fallback and this removes it, so there is no third exit — the alternatives would be spending a whole multi-core Tick budget on routing, or the 2048² map `05` documents. Both are named elsewhere and neither is this decision's to take.
- **A routing partition that must straddle a severance.** A District is contiguous by construction; a regular partition is not, so a zone split by an Arterial with no crossing has road on both sides of a boundary its representative sits on one side of. R1's empty-District handling is the machinery, and the failure mode is new.
- **The abstract graph ceasing to be derived.** `adr/0040`'s permanence asymmetry disappears, and with it the argument that put the routing partition outside the save.
- **A measured entry error that no affordable granularity brings inside what the choice loop can absorb.** That would mean the matrix is the wrong instrument for the choice loop rather than the wrong size, and `02 §5.8`'s rule would reopen — which R1 has already tested once and found enforceable.
