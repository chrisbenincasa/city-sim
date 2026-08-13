# 02 — Simulation Model

> Vocabulary is defined in [`CONTEXT.md`](../CONTEXT.md). Pillars and anti-goals in [`00-vision.md`](00-vision.md).
> This document specifies *what the simulation is*. Agent fidelity is in `03-agent-architecture.md`; goods and markets in `04-economy-and-goods.md`; the code structure in `05-technical-architecture.md`.

---

## 1. The shape of the thing

The simulation is a pure function of its inputs:

```
step(inputs) -> ()
```

It has no concept of wall-clock time, no camera, no renderer, and no filesystem. A **Tick** is an unsigned integer counter, and the host decides when to advance it. This is what makes fast-forward, headless testing, and replay free rather than features we'd have to build.

Everything in this document is expressed in integers. Money, goods, population, positions, layer values, and time are all integral or fixed-point. **There is no float arithmetic in the core at all** — not merely no floats in stored state, since a float temporary cast to an integer is exactly as non-deterministic (§8 rule 1). `SOLVE THE ACTUAL PROBLEM` does not extend to floating-point physics we don't need.

**Transcendentals are not banned — they are tabulated.** This section previously said *"no transcendental functions anywhere,"* which was never true of this document: §5.4's choice model is a softmax over `exp`, and §2.4's noise falloff is logarithmic. `exp` and `log` exist in the core as **fixed-point tables with defined rounding**, and no `sin` is needed. See [`adr/0003`](adr/0003-deterministic-integer-simulation.md), which also records why the table's *resolution* is a stated figure rather than an implementation detail: it perturbs the effective `μ`, and `μ` is what stops stampedes. The figure itself is [`adr/0038`](adr/0038-the-transcendental-tables-are-sized-by-the-representation.md): **256 entries each with rounded linear interpolation and base-2 range reduction**, chosen so the table stops being the limiting factor — about 0.12 ULP of a roughly 1 ULP total, the remainder being Q16.16's own rounding, which no table size can improve. The entry count, the rounding and the committed values are **hash-bearing world-creation constants**.

### 1.1 Tick phases

Every tick runs the same ordered phases. The ordering is not an implementation detail — it is the determinism contract.

| Phase | Name | Concurrency | What happens |
|---|---|---|---|
| 0 | **Input** | serial | Apply player commands from the Input Log. Zoning, road edits, budget changes. Nothing about the camera — fidelity is derived from Stress, not recorded. |
| 1 | **Wake** | serial | Drain the Event Wheel bucket for this tick. Everything with something to do is now in a work list. |
| 2 | **Decide** | parallel, read-only | Woken Buildings evaluate their Rules against the **Past**. Households evaluate Needs. Output is a list of *intents*, never a mutation. |
| 3 | **Settle** | serial, sorted | Apply intents in deterministic key order against the **Future**. Re-check atomicity. Losers take their fallback path. |
| 4 | **Move** | parallel | Lanes advance their Vehicles. Statistical trips check for arrival. |
| 5 | **Layers** | parallel | Map Layer diffusion, for whichever layers are scheduled this tick. |
| 6 | **Growth** | serial | **Placement drains the Unplaced Pool into vacant capacity**, then Zone Rules sample Lots. Buildings that have accumulated failure decline. |
| 7 | **Commit** | serial | Swap Past and Future. Schedule next events. Re-evaluate Segment Stress and apply promotions/demotions. Emit the State Hash if due. |

Two properties this buys:

**Phase 2 can be parallelised safely** because it only reads the Past and writes nothing. This is the phase that scales with city size, and it is the one we want on many cores. Phase 3 is the serial bottleneck by construction, and it is cheap because it is only applying already-computed decisions.

> **"The Past" is a property of this ordering, not a second copy of the world.** [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) replaced the full-world double buffer with **one live state**, double-buffering only the two tables a *parallel* phase both reads and writes — Lane dynamics (Phase 4) and Map Layer cells (Phase 5). The Past is therefore *the state as of the start of this Tick*, which Phase 2 observes because nothing has written yet. Everything this section says is unchanged; only the mechanism is. **The consequence for this table: Phase 2's "writes nothing" is now load-bearing rather than merely tidy** — it is what permits every entity table to be single-buffered, and a future decision to parallelise Decide must not also make it mutating. Phases 4 and 5 are marked parallel here and are the two exceptions for exactly that reason.

> **Placement's position inside Phase 6 is the decision, not its presence there**
> ([`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)). It runs
> **ahead** of the Zone Rules, which is what makes §5.6's create predicate a statement about *vacancy*
> rather than about population: a Household still in the Pool when a developer looks is one the standing
> stock could not house. This table calls the ordering the determinism contract, and here that is the
> whole content of the decision. **Decided and unbuilt** — see §5.2.

**Contention is resolved honestly.** Two bakeries evaluating in parallel may both observe six flour in the Pool and both decide to consume it. In Phase 3 the first one — by a **counter-based random shuffle**, per §8 rule 5 — succeeds, and the second finds the condition no longer holds, fails atomically, and takes its fallback. Nobody gets phantom flour, the loser's failure is a real event that can be reported, and no Building holds a standing advantage over its neighbours.

**This must stay the occasional case, not the structural one.** A sorted key applied to *chronic* shortage produces permanent starvation rather than a gradient — the same Building loses every time and no player could see why. What keeps it occasional is the requirement-aware wait list in §4.1: a consumer that failed is asleep subscribed rather than awake competing, and a write wakes only as many waiters as **the Bin's level can actually complete** (`adr/0063`). Note the subscription is on the Bin that **stopped** the Rule — its own `local` Bin — and never on the Pool it would have escalated to; a Pool delivery reaches those consumers through the link that refills them, not directly. Phase 3 contention is then what it should be — a Rule that was already awake racing one that was just woken.

### 1.2 Rates and scales

Every rate in the project is defined here and nowhere else. Other documents cite this section rather than restating numbers.

This section is written to be re-read, because the reasoning is counter-intuitive and will be reached for again.

#### The simulation has no units

The core holds exactly two quantities relating to time and space: an integer **Tick** counter and integer **Tile** coordinates. There are no seconds in the library and no metres. Vehicle speed is stored as *Tiles per Tick*; a commute is *N Ticks*.

Seconds and metres arrive from two exchange rates, both invented outside the simulation, both free to change, and **neither visible to the simulation**:

- **Ticks → real seconds** is the host deciding how often to call `step()`. That is the speed control.
- **Tiles → metres** is the artist deciding how large to draw a building. Declaring a Tile to be 4 m rather than 8 m means redrawing everything half-size, and the screen is identical. ~~Nothing in the game reads the metre.~~

> **⚠ AMENDED 2026-08-12 by [`adr/0082`](adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md): neither rate is free any more, and the Ruleset is what spent them.** Keep the distinction above — the core's *arithmetic* still holds only Ticks and Tiles, and that is what the paragraph is for. But the `[roads]` table authors speeds in **km/h**, `05 §26` fixes a Tile at **~4 m**, and `02 §2` mandates that speed be stored in **Tiles per Tick**. Three of those four quantities determine the fourth, so **a Tick's duration in seconds is derived**: 86,400 s over `TICKS_PER_DAY` is **10.546875 s**, and `Speed.FromKilometresPerHour` reads the metre on every Ruleset load. ***A degree of freedom is spent by the first document that uses it, and nothing announces the spending.***
>
> **A Tick is a behavioural unit.** Car-following needs ~45× finer and takes a **sub-step ratio** inside Tick phase 4; making it global costs **108×** the measured Tick budget. See `adr/0082`.

#### The one number that is not an exchange rate

`TICKS_PER_DAY` looks like a third exchange rate and is not. A **Day** is not an external unit being converted to — it is a simulation object, the period of a Household's routine. So `TICKS_PER_DAY` lives *inside* the simulation, next to "a commute takes 480 Ticks," and the **ratio** between them is a real dimensionless fact:

```
480 Ticks (commute) ÷ 8192 Ticks (Day) = 5.9% of a life spent driving, one way
```

That figure is invariant under both exchange rates. It is the only time-related quantity in the design that describes the world rather than our view of it — and it is the traffic balance, because *share of life in transit* is the same number as *share of the population on the road at any instant*.

**Shortening the Day does not shorten the drive. It shortens the life around the unchanged drive**, putting proportionally more vehicles on the road at every moment. See [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md), which also records why the traffic model — the only continuous process here — is what forces the Tick to be fine-grained at all.

#### Normative values

| Constant | Value | Kind | Notes |
|---|---|---|---|
| `TICKS_PER_DAY` | **8192** | world-creation; baked into the save | Not hot-reloadable. Changing it reinterprets every scheduled event |
| Reference tick rate | **16 Ticks/s** | host-side, runtime | Defines the default speed. Invisible to the simulation |
| `WHEEL_SIZE` | **8192** Ticks | world-creation | Set by the longest *common* event horizon, which is one Day — so it equals `TICKS_PER_DAY` for an independent reason, not by definition. Overflow tier handles multi-Day countdowns |
| Vehicle free-flow speed | ~~**~0.5 Tile/Tick**~~ **Ruleset `*_speed_kph`; 50 / 90 / 5 km/h shipped, which is 36.6 / 65.9 / 3.66 Tiles/Tick** | Ruleset; per road class | ⚠ **CORRECTED 2026-08-12 ([`adr/0082`](adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md)).** Two claims were welded here. **The car-following ceiling is real** — above ~1 Tile/Tick Lane queues stop being meaningful — but it is a constraint on the **Lane kernel's sub-step**, not on a Tick, and it does not set this row. The **~0.5** was derived from the *reads as* column below, which is 65× adrift of its own `Day` column, and it was **73× off the shipped speeds**. It cannot be rescued by scaling the pedestrian down to match: at 0.5 Tile/Tick a walker is 0.05 and a 3 km walk is **1.8 Days** — and the walk-to-drive ratio is the exact quantity `adr/0008`'s single-currency Commute Budget exists to compare, so Severance would stop costing anything. *Under the old row you could have a realistic walk or a realistic mode choice, not both.* |
| Cross-town trip | ~~**~480 Ticks**~~ **~112 Ticks** — the full 16.4 km map width at 50 km/h | derived | ⚠ **CORRECTED 2026-08-12, and it moves `adr/0019`'s headline ratio by 4.3×.** The 480 was self-consistent with ~0.5 Tile/Tick and travelled with it: at that speed *"cross-town"* was **~1 km**, a District rather than a town, and the row predates the 4096² map. Corrected, a one-way crossing is **1.4% of a Day**, not 5.9% — **2.7% both ways against a stated 11.7%**. Since *share of life in transit* is the same quantity as *share of the population on the road at any instant*, **the corpus has been assuming ~4× the standing traffic the shipped numbers produce.** Name the speed when quoting this: a real commute is shorter than a map crossing and mixes 50 with 90 km/h arterials, so treat 112 as an upper bound on the *distance* and a rough one on the *speed* |
| Cell | **32×32** Tiles | world-creation; baked into the save | **Design constant.** It is the resolution of pollution, so it changes the State Hash. Never tuned — [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md) |
| Chunk | **≥ 32×32** Tiles, a multiple of the Cell | tuning; unvalidated | Hash-preserving, so it is a measurement. Probably wants to be larger. See [`05 §5`](05-technical-architecture.md) |
| Map Layer diffusion | pollution every **64** Ticks at offset 0; land value every **256** at offset 16 | tuning; **hash-bearing** | **The designer's number, not the profiler's** — measured, not argued, in [`adr/0044`](adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md). Hot-reloadable; the dirty set is what makes a mid-run change lossless. §2.4 |
| Industrial pollution kernel | separable tent, **1,024 m** — 8 Cells | world-creation; baked into the save; **unratified** | A Cell is *stored* in kernel units, so this meets [`adr/0015`](adr/0015-all-tuning-data-is-hot-reloadable.md)'s membership test where the cadence above does not. Authored in metres per §2.5 question 2. `02 §2.4`'s 1–10 km band is 10× wide and fails guard rule 1; filed, not fixed. [`adr/0044`](adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md) |

Derived, for orientation only — none of these are inputs:

| Speed | Ticks/s | Day | Fast-forward | Cross-town trip (16.4 km) |
|---|---|---|---|---|
| **Study** (½×) | 8 | 17m04s | 84.4× | 14.0 s |
| **Normal** (1×) — default | 16 | **8m32s** | **168.75×** | 7.0 s |
| **Fast** (2×) | 32 | 4m16s | 337.5× | 3.5 s |
| **Very fast** (4×) | 64 | 2m08s | 675× | 1.7 s |

> **⚠ The *Traffic reads as* column was deleted 2026-08-12, not restated** ([`adr/0082`](adr/0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md); diagnosed in [`plans/0012`](../plans/0012-corpus-audit.md)). It claimed ~65 km/h at Study and ~130 at Normal, marking the first *"visually honest"*. **It was 65× adrift of the `Day` column beside it**, and an orientation figure that disagrees with the row next to it orients nobody. The **fast-forward** column replaces it because that is the quantity the `Day` column actually implies: once a Day of 86,400 s is shown in 512 s, *everything* on screen moves at 168.75×, and a 50 km/h car reads as ~8,400 km/h.
>
> **The deleted claim rested on a category error worth keeping.** Appearance was treated as a **constraint on the simulated speed** when it is a **consequence of the calendar rate** — and the calendar rate was already spent by `TICKS_PER_DAY` and the reference tick rate, so there was no freedom left to buy a car that looks like a car. ***A speed picked to satisfy appearance is bought with currency the pacing decision had already spent.*** The compensation is uniform, so the **ratios survive** — a car still reads as ten times a pedestrian — and only the absolute claim was false.
>
> ⚠ **This is the failure [`01`](01-player-experience.md) diagnoses in another game, committed in our own table.** `01` → *time is an arc, not a clock*: *"Cities: Skylines' calendar runs 112× faster than its own day/night cycle, which is why its players report cars taking 'weeks' to cross town."* This table committed the same mismatch at 65×, three documents from the paragraph naming it. **`01`'s remedy is intact** — a sun arc makes no numeric claim, so nothing shown to a *player* was lying. What was lying is a table a **developer** reads.

~~Traffic looks true at exactly the speed where the player slows down to inspect it — the same principle as [`adr/0007`](adr/0007-stress-driven-simulation-detail.md), arriving free on a different axis.~~ **Struck with the column it describes.** [`adr/0007`](adr/0007-stress-driven-simulation-detail.md)'s principle is untouched and reappears instead in the place `adr/0082` puts it: **resolution arriving where the physics needs it**, via the Lane kernel's sub-step, rather than detail arriving where the camera is.

#### Rules that follow

- **Pacing is never a simulation change.** Session length, "days feel long," and difficulty-by-reaction-time are all served by the speed ladder, which the core cannot observe.
- **Never skip Ticks to keep up.** When the host cannot sustain the rate, wall-clock time dilates and the Tick sequence stays intact. Skipping would break replay and the State Hash. This is Factorio's documented behaviour.
- **Traffic pressure is tuned with vehicle speed or city grain, never with the Day.** Both of those are things the player can see and act on; the Day is a hidden global constant, and a hidden global constant that tunes a system-wide outcome is the object `00-vision.md` pillar 1 exists to forbid. `LEGIBLE CAUSE`
- **There is no hour and no minute.** Time of day is a sun arc with named phases — dawn, morning peak, midday, evening peak, night. 8192 is not divisible by 24, so an hour would not land on a Tick boundary; more importantly, an arc makes no numeric claim and so cannot be caught lying. Commute Budget is drawn as a wedge on that same arc, which removes the last conversion factor between the clock and the thing being measured against it. See [`01-player-experience.md` §7](01-player-experience.md).

---

## 2. Space

### 2.1 The hierarchy

```
Tile          1×1    the atom. terrain, zone designation, ownership
Cell         32×32   the resolution the environment varies at. one Map Layer value
Chunk        ≥32×32  the technical partition. a multiple of the Cell (see 05 §5)
District       ~n    gameplay region. goods pool freely inside one
Lot        variable   a developable parcel with road frontage
Settlement  derived   districts mutually within the Commute Budget
World      bounded   one rectangle, one Tick counter, all of it live
```

**The World is one bounded rectangle and all of it is simulated all the time.** There are no separately-saved city tiles and no frozen neighbours — that model is a second clock, and is foreclosed by [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md). Chunks are stored **sparsely**, so an undeveloped Chunk is a null rather than an array and map extent costs nothing until it is built on. The **edge is load-bearing**: Outside Connections live there, and finite land is a pressure source, which is why the world is bounded rather than infinite. See [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md).

**Settlement is derived, not drawn.** A connected component of the District graph where an edge exists between Districts within the Commute Budget of each other — a union-find over the travel-time matrix, recomputed when it rebuilds. Settlements appear, merge when a road connects them, and split when congestion pushes them apart, so the region view is a readout of the simulation rather than a decoration. It is a **reporting unit only**: nothing pools by Settlement and no Rule reads one.

**The Cell and the Chunk used to be one thing, and splitting them is [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md).**

The **Cell** is a **design constant**: 32×32 Tiles, the resolution at which the environment varies, and the storage unit of every Map Layer. Its size is the resolution of pollution, which feeds Fertility and Desirability and therefore the choice model — so **changing it changes the State Hash**, and it is permanently unavailable for tuning.

The **Chunk** is a **performance partition** and is a multiple of the Cell. It remains deliberately overloaded, carrying dirty tracking, save serialisation, parallel work assignment, aggregate caching, pathfinding cluster, and render streaming. Keeping *those six* unified is a large simplification and any proposal to split one off should still be treated as suspect. But every one of them is hash-preserving, so the Chunk's size is a question for a profiler — see [`05 §5`](05-technical-architecture.md).

The split is between the two kinds of decision, not between seven roles. Welding a design constant to a performance knob meant the profiler was permanently entitled to an opinion about the pollution map.

**District is a gameplay concept, Chunk is a technical one.** They are not the same and should never be conflated. A District is the boundary within which Goods move without physical transport, and the granularity of the travel-time matrix. Districts are contiguous sets of **Cells** — either player-drawn or derived automatically from road topology and land use.

> **Corrected: this said "contiguous sets of Chunks", and it contradicted [`05 §4`](05-technical-architecture.md).** That section lists Chunk size as **hash-preserving and free to tune against a profile**. But District extent decides Goods pooling and matrix granularity, so a Chunk-aligned boundary would let a profiler change the city — the welding failure `05 §201` names in its own words, *"a constant welded to two decisions is governed by whichever of them is louder"*, with the profiler as the loud one. **The Cell is the correct unit**: frozen by `adr/0034`, a strict divisor of the Chunk so the alignment costs nothing, and already *"the resolution at which the city's environment varies"* — which is what a District boundary is. Its own sentence above says the two should never be conflated.

> **Settled: both.** Automatic by default, player-adjustable as an advanced action. The finding that resolves it is that **District extent is bounded by the pooling abstraction's own validity** — a District can only be as large as the area within which "ignore transport" is a defensible simplification — so the *count* is physics rather than a design choice. That matters more than it sounds: a single-District map would pool Goods across the whole world instantly, deleting Shipments and silently collapsing [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md), which warns explicitly that *"if Shipment cost is ever tuned toward zero, this design silently collapses."*
>
> The early city therefore has one District because the city **is** one neighbourhood, not because of a rule; more appear as it outgrows the pooling radius. Splitting and redrawing is a late-game advanced action that arrives exactly when one end of the map genuinely differs from the other, and it is also what makes District-scoped Policy targetable — you cannot aim a policy at a boundary you did not choose.

### 2.2 Lots

Lots are **generated, not painted**. When a player zones an area, the system subdivides zoned land against the Street network into parcels with road frontage. A Lot is either vacant or holds exactly one Building.

Subdivision rules:
- Every Lot must have frontage on at least one Street. **Arterials grant no frontage and carry no Access Point, so nothing zones onto one** — see [`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md), which settles this and notes the consequence that Arterial corridors need something to be made of. Streets snap to the grid; Arterials are freeform splines meeting only at authored Junction pieces, and the Road Graph is uniform regardless.
- ~~Lot depth and width targets vary by zone density; the subdivider fits what it can.~~ **Width only, and it is derived rather than authored** ([`adr/0078`](adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md)). A Segment carries `lots_per_segment` Lots at evenly spaced offsets with the side alternating — odd-and-even house numbering, which is where the word *Address* comes from — and the value is `CONTEXT` → Address's own *"five Buildings share a Segment"*, the premise the ~30,000-Segment figure rests on. **Depth does not exist**: a Lot has no extent, so a depth would be a hash-bearing number chosen for a consumer nobody has designed. *"Vary by zone density"* is unbuilt and waits on [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s bands.
- Land that cannot be given frontage stays unlotted and undevelopable — this is how bad street layouts punish the player *mechanically* rather than through a penalty number. `LEGIBLE CAUSE` **Lots hang on a block's faces, so a block interior is unlotted *structurally* rather than because a depth ran out — a larger block has a proportionally larger dead interior with no number governing it.**

Re-subdivision happens when the street network changes, and must preserve existing Buildings — only vacant land re-parcels. **The preservation rule is keyed on occupancy and not on frontage** ([`adr/0079`](adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)): a **vacant** Lot that loses its frontage is deleted and becomes land again, and an **occupied** one is kept with no Address at all. A Building whose last Street is bulldozed keeps standing and keeps its Occupants; nothing pressures it and nothing refuses the edit, because refusing would be the road-derived cap `adr/0025` rejected wearing a different hat. The consequence arrives in milestone **5b**, where a Trip to or from an Address that does not exist ends *no route found*. **The invariant is therefore *every **vacant** Lot has frontage*, never *every Lot*** — an invariant that failed on the correct behaviour would be the tier that gets disabled to ship.

> **The subdivider is built, as of 5a-bis** ([`plans/0022`](../plans/0022-the-lot-subdivider-and-build-road.md)), **and the opening sentence is now true of the build.** A `zone` command subdivides the block its Tile falls in, against that block's four Street faces; land the Streets do not reach yields no Lots; and frontage is `(derived AND rebuilt)` from the Road Graph on the Epoch, so bulldozing a Street takes it away. So *"every Building is on the Road Graph by construction"* — which `CONTEXT` → Frontage leans on to delete the utility network entirely — is standing on something at last, rather than being true because there was no Road Graph.
>
> *Its one qualification is the honest one:* a Building **can** end up off the network, by the player bulldozing the Street it fronted, and `adr/0079` keeps it standing with no Address rather than refusing the edit. That is not a hole in the claim so much as its boundary: every Building is *placed* on the Road Graph, and the player may afterwards take the road away.
>
> *Previously:* ~~The subdivider does not exist, and every Lot this project has ever run has been painted… **nothing has ever refused a Lot for want of frontage**.~~ The debt was in [`plans/0012`](../plans/0012-corpus-audit.md).

**A Lot's Zone is a permission set, and it governs what may be built rather than what may die.** A Zone Rule's declared permission bit is a term in its *create* predicate; it is never a filter on the Lots the Rule samples ([`adr/0055`](adr/0055-a-zone-rules-permission-set-scopes-what-it-builds-never-which-lots-it-looks-at.md)). Scoping the population instead would mean a player who repainted a Lot removed its Building from every Rule's reach for ever — immortality by paintbrush. Repainting therefore does nothing immediate to what already stands there, which is the symmetric reading of [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md)'s *"upzoning a built block does nothing until its Buildings go"*, and why `01 §5` lists rezoning and clearance of abandoned stock as two separate recovery levers.

*A Lot is either vacant or holds exactly one Building* is checked in both tiers `§10` provides: at the write site, where a second Building would be built, and whole-world as a bijection between the Lot's reverse index and the Building's Lot handle.

### 2.3 Terrain, and the land as a stock

Terrain is generated procedurally from the world seed, which the Input Log already carries. Seeds are shareable, the map need not be stored in the save — only the Chunks the player has changed — and the **generator's version is pinned in the save header**, because a changed generator would silently load every existing save onto different ground.

**The world is not flat, and terrain enters at construction time only.** Height decides what can be built and what it costs; it never touches the Road Graph, vehicle speed, travel time, or anything the Move phase reads. The checkable rule: *if a terrain value is read inside a Tick phase, something has gone wrong.* This keeps grade-dependent vehicle performance and sloped junction geometry — the cost `adr/0014` confines — permanently outside the simulation.

**Terraforming is priced, not capped.** Cost is volume moved × haul distance to where the spoil goes, with cut and fill balancing. Small levelling is cheap, notching a ridge is moderate, removing a mountain is ruinous because the spoil has nowhere near to go — a cost curve that emerges from a real mechanism rather than a tuned exponent. Water is immutable; occupied Tiles are locked; earthworks cost money rather than Materials. See [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md).

**The generator places Woodland and nothing else.** Fertility is not on the map — all land begins fertile and the player's own development degrades it:

```
fertility(chunk) = terrain suitability − Sealing − pollution
```

**Sealing** is the count of Tiles in a **Cell** ever built on, decaying at a Ruleset rate keyed by terrain type. **Pollution** is the existing layer, already diffusing. Fertility itself is *composed at the point of use and never stored*, per the rule in §2.4 — so this needs no new layer, and a farm reading its yield is an ordinary Rule reading Map Layer Cells under its footprint.

Two consequences the rest of the design depends on. Farms emit into pollution, so agriculture and housing repel each other with no rule saying so — and farms therefore retreat outward as the city grows until their workers fall out of the city's commute shed, at which point the farm village **becomes its own Settlement**. And Woodland regrows slowly on unsealed, unoccupied land, so the logging frontier migrates outward through ordinary Building decline rather than through a system written for it. See [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md).

### 2.4 Map Layers

Coarse scalar fields, stored at **one value per Cell**, not per Tile. This is a 1024× reduction in work versus per-Tile fields and is visually indistinguishable once upsampled. Original SimCity did the same thing — coarse grids, tiny kernels, few iterations — and it looked fine.

**Not everything the player perceives as a field is one.** Fields are sorted by the geometry of the thing that emits them, and only wide-range point sources end up on the Cell grid. See [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md); the procedure for classifying a new one is §2.5.

| Field | Representation | Sources | Update | Notes |
|---|---|---|---|---|
| **Industrial pollution** | stored Layer, diffused | industry (point sources) | every 64 ticks | real plumes run 1–10 km. **A stock the environment absorbs** ([`adr/0051`](adr/0051-industrial-pollution-is-a-stock-the-environment-absorbs.md)); wind advection is a later addition |
| **Land value** | stored Layer, slow-moving | composite + accessibility | every 256 ticks | has momentum; see below |
| **Sealing** | stored per Cell, **not diffused** | construction | on build | a count, not a field |
| Noise | **point-of-use query** | frontage Street volume + Arterials within ~300 m | on read | line source, 50–300 m, log falloff. Never stored |
| Near-road pollution | **point-of-use query** | as Noise, different weights | on read | line source, 150–300 m |
| Amenity | **walkable catchment** on the Road Graph | destinations within ~400 m on foot | cached, Epoch-invalidated | a *time*, not a distance |
| Water pollution | **Bin per Water Body**, plus a shoreline line source | dumping, runoff | on transaction | network transport, not spread. `CONTEXT` → Water Body |
| Service coverage | **overlay only** | reachability the Trips actually use | on demand | demoted by [`adr/0032`](adr/0032-services-are-delivered-by-trips-not-by-coverage.md) — nothing reads it |
| Desirability, Fertility | **derived, never stored** | composed at point of use | — | see below |

**A Layer is a convolution of a source field with a bounded kernel — never an iterative relaxation.** This is the property everything else rests on. Convolution is linear, so twenty factories superpose exactly with no interaction to model and no ordering to get wrong; and the incremental scheme below is then *exact* rather than an approximation. Under relaxation-to-steady-state neither holds, one changed source perturbs the whole field, and saves would diverge for reasons nobody could find.

**Two fields left this table and it is worth saying why, because it looks like an omission.** Noise and near-road pollution are **line sources**, short-ranged and logarithmic: the whole gradient fits inside one Cell, so a Cell-resolution field cannot hold its shape and degrades into *is there a road here*. Finer Cells were considered and rejected — the honest fix is that a line source is a distance query, not a spread, and the query is exact at Tile resolution and costs nothing.

**The query sums, and it enumerates by loudness rather than by road class.** Summing is required because noise superposes — a *nearest-source* query understates a Lot caught between two busy roads. Enumerating by loudness means the query takes every linear source within range **whose contribution exceeds the ambient background**, where the background is the local-Street level it already computes. That is a crossover rather than a threshold: nobody authors a number, and the enumerated set is small *by definition*, since standing out above the background is what makes a source enumerable.

What this depends on is **bimodally distributed traffic volume** — a uniform background plus a bounded set that stands out, with nothing between them. That is a property of [`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md)'s grid-plus-sparse-Arterials layout, and the dependency was not visible when `0014` was written. It is robust to new road *classes*: `adr/0029`'s **Separated** transit band is already an Arterial (`CONTEXT` → Arterial reads *"highway, rail, major boulevard"*) and is equally rare. The band that stresses it is **Reserved**, which puts Arterial-scale volume onto an ordinary grid Street — the middle case — and which enumerating by loudness catches and enumerating by class would miss.

> **The trade-off, recorded for a profile rather than argued now.** Enumerating by loudness is a spatial search; enumerating Arterials was a lookup against a small fixed set. If the search appears in a profile, the cheap version is available — at the cost of missing a Reserved-band corridor one block away. Note that swapping them **changes the State Hash**, so it is a design change under [`05 §4`](05-technical-architecture.md) and not a free optimisation.

**Composition rule: compose at the point of use, do not bake composites into stored layers.** Desirability is `w₁·land_value − w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline`, evaluated lazily where it's needed. A stored desirability layer would need invalidating whenever any input changed, and would drift.

Land value is the exception — it is stored because it has *momentum*. It moves slowly toward the current desirability rather than tracking it instantly, which is both realistic and a stabiliser against oscillation.

**Diffusion mechanics:**
- Double-buffered. In-place diffusion is order-dependent, which is a determinism hazard *and* produces a visible directional smear.
- Integer arithmetic with explicit rounding. No floats. **The rounding is at the point of use, not inside a pass, and the placement is forced rather than chosen** — integer division is not linear, so a rounded pass would fail the superposition property below. A Layer is therefore stored **pre-normalised in kernel units**. [`adr/0044`](adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md).
- Separable kernels — two 1-D passes rather than one 2-D pass.
- **Staggered** across ticks so no single tick spikes: pollution on `tick % 64 == 0`, land value on `tick % 256 == 16`. **The periods and the offsets are hash-bearing, so they are the designer's numbers and not the profiler's** — measured, not argued, in [`adr/0044`](adr/0044-the-map-layer-diffusion-cadence-is-the-designers-number-not-the-profilers.md). They stay ordinary hot-reloadable Ruleset data, because the dirty set means a skipped diffusion has deferred work rather than lost it. *Noise is not on this list and never was scheduled: it stopped being a Layer at all in [`adr/0034`](adr/0034-fields-are-sorted-by-source-geometry.md), and this bullet's old example outlived the decision that removed it.*
- **Incremental** where possible: maintain a set of Cells whose *sources* changed, and re-diffuse only those plus a halo of radius *r*. **Exact**, not approximate, because the Layer is a convolution and our kernels have bounded support. **Decay is in tension with this and the tension is measurable, not arguable** — a decaying source is a changing source, so every emitting Cell is dirty on every cadence and the incremental set converges on the occupied set. Routed to a machine rather than settled here ([`adr/0051`](adr/0051-industrial-pollution-is-a-stock-the-environment-absorbs.md)).

**A source is a stock, and the environment absorbs it** ([`adr/0051`](adr/0051-industrial-pollution-is-a-stock-the-environment-absorbs.md)). A Rule's `map` output adds to the Cell's source; the source decays toward zero at a Ruleset rate. **The ceiling is emergent rather than authored** — a steady emitter settles where what it adds each cycle equals what is absorbed, so the level is proportional to the emission *rate*, which is what a source field is supposed to hold. This is what the table's *"Decays"* has meant since it was written; it went unimplemented through slice 6, which built the diffusion and not the removal, and surfaced when the first Rule with a `map` output was planned. **`+=` is right across emitters and wrong across firings**: twenty factories in one Cell must sum, and one factory firing twenty thousand times must not. A clamp was the obvious alternative and breaks superposition, which is why it was rejected rather than merely not chosen.

### 2.5 Admitting a new field

Every field above was classified by the same procedure, and it should be run before any new one is built. It exists because *"add a Map Layer"* was the reflex answer four times running and was the right answer once.

Answer in order. Each answer eliminates representations.

| # | Question | What it decides |
|---|---|---|
| 1 | **What is the source geometry** — point, line, area, or network? | The most eliminative question, so it is asked first. **Area reduces to line**: an area's influence on land is its perimeter, which is why a coastline and a pond are one geometry at two lengths |
| 2 | **What is its actionable range in metres, and can you defend the figure from reality?** | A range you cannot source is a balance hazard. *Author in domain units* |
| 3 | **Does it superpose, or does the nearest source dominate?** | Whether a query is admissible at all. This is what nearly broke Noise |
| 4 | **Isotropic, or directional?** | Advection and network flow are different machinery from diffusion |
| 5 | **Does it have memory?** | Land value and groundwater persist; noise does not |
| 6 | **Does a Rule read it, or is it only displayed?** | Display-only is an **overlay**, never a Layer. `adr/0032` demoted service coverage on exactly this test |
| 7 | **Can it be composed from what is already stored?** | If yes it is derived and never stored |

The representation then falls out:

| Representation | Admitted when | Examples |
|---|---|---|
| Derived at point of use | composable from stored fields | Desirability, Fertility |
| Local query | short range **and** the source set is small or known by construction | Noise, near-road pollution |
| Catchment on the Road Graph | the range is a walk or drive *time*, not a distance | Amenity |
| Diffused Cell Layer | point sources, range ≫ a Cell, superposing, isotropic | industrial pollution |
| Advected Cell Layer | as above, directional, with memory | groundwater, wind-blown pollution |
| Network flow | transport along a fixed 1-D graph, directional | water pollution |
| Overlay, not a Layer | nothing but the player reads it | service coverage |

Five guard rules:

1. **One field, one geometry, one range.** Two source geometries, or two ranges more than ~5× apart, means two fields wearing one name. This is what caught the old `Pollution` row: it was fed by industry (a point, kilometres) and traffic (a line, 150 m) on one grid with one kernel, so one of them was always wrong.
2. **A field must resolve finer than the player's unit of action, or a gradient reads as a wall.** Failing this sends a field to a *query*, not to a finer grid.
3. **A stored field that could be composed is a defect**, and **if no Rule reads it, it is an overlay.**
4. **A new grid must be a strict divisor or multiple of the Cell**, or it must argue against the unified-partition rule in [`05 §5`](05-technical-architecture.md).
5. **A modelling refinement is admitted when a player decision distinguishes it, and deferred otherwise.** Not on cost — on whether anyone can act on the difference. Downstream river flow passes loudly; depth stratification and tidal action do not, and are parked in [`deferred.md`](deferred.md).

---

## 3. Resources

### 3.1 Two representations, deliberately

| | **Goods** | **Needs** |
|---|---|---|
| Represented as | absolute integer count in a Bin, `[0, capacity]` | relative scalar, **0 = ideal**, negative = deficit |
| Conserved? | yes, strictly | no |
| Lives on | Buildings, District Pools, Outside Connections | Households |
| Why | supply chains must be auditable and conserved | absolute stockpiles make balancing unstable |

The split is load-bearing. Citybound's warning about need modelling:

> "Having to rely on absolute amounts for resources makes balancing very hard and amplifies bugs and makes the system potentially very unstable."

That is true for satisfaction, and false for flour. A bakery that consumes six flour must consume exactly six flour that came from somewhere, or the economy stops being explicable. But a household's hunger is not a stockpile — it is a statement about how well they are doing, and modelling it as a quantity produces a system where small imbalances compound into absurdity.

**Conservation is a testable invariant.** A debug pass asserts every tick that total Goods in existence equals what was produced minus what was consumed. Anything else is a bug, and this is how we find it. This assertion is also the primary guard against the LOD boundary leaking or duplicating resources.

### 3.2 Bins

A Bin is `{ owner: Handle<Building>, resource: ResourceId, level: int, capacity: int }` plus its two wait-list heads, invariant `0 <= level <= capacity`. **The level is signed deliberately**, because headroom is `capacity − level` and an unsigned subtraction there wraps instead of failing. **A money Bin is unbounded** (`adr/0024`): its capacity is the representation's ceiling and the loader **refuses** an authored one, because a warehouse limit on money is what made `§4.3`'s own worked example destroy a unit per baking.

**A hash map is never iterated in simulation code.** Iteration order in .NET depends on insertion history and, for string keys, on per-process hash randomisation, so a walk over one is not reproducible. Build one and look up in it freely; never walk it.

> **This paragraph previously said Bins live in a `ResourceMap`.** They do not. A Bin is a row in the Bin table owned by a Building handle, and a Building's Bins are an **intrusive index list** walked linearly — a Building holds few enough Bins that a walk beats a search, and the list is what every variable-length collection in the core already is. `ResourceMap` exists and would earn its place against a contiguous block per Building, which is not the shape that was built.

Capacity is what makes production chains interesting: a bakery with capacity for 20 flour cannot stockpile indefinitely, so its supply must be continuous rather than bursty.

---

## 4. The Rule engine

Lineage: this is GlassBox's model, which was genuinely excellent and is the part of SimCity 2013 worth taking.

**There are two execution models, not one.** This was obscured for a long time by Zone Rules being treated as an anomaly bolted onto the side of the engine. They are not an anomaly — they are the second model, and Policies are its second instance. The test that sorts a mechanism into one or the other:

> **Subscribe when waiting on a specific named thing. Poll when sweeping a population.**

| | **Bin Rule** | **Sweep Rule** |
|---|---|---|
| Attached to | one Building | the city, or a District |
| Acts on | Bins, atomically | a population, testing real simulation state |
| Dispatch | **scheduled** — `rate` re-arms it on the Event Wheel | **time trigger** — polled by design |
| On failure | **subscribes** to the Bin that was short | acts on whom it can, reports, retries next trigger |
| Cost control | the Event Wheel: most entities are asleep | stagger, Chunk partition, and sometimes sampling |
| Instances | production, consumption, transfers on one Building | Zone Rules (§5.3), Policies |

**Which model a mechanism uses is a property of the mechanism, fixed at design time.** It is never a tuning decision, and a mechanism never migrates between families for performance reasons — because the two models differ in *observable* behaviour and not merely in cost. They differ in how long an arriving input takes to propagate, in who wins a contested resource, and in the sentence `Evidence` prints when they fail. Migrating one would change the city, which makes it a design change however it was motivated. See [`05 §4`](05-technical-architecture.md) for the general form of that rule.

### 4.1 Bin Rules

A **Bin Rule** is an atomic transformation over Bins. It declares inputs and outputs against four target scopes:

| Scope | Meaning | Whose is it |
|---|---|---|
| `local` | Bins on the Building running the rule | **mine — free** |
| `pool` | Bins on the Building's District Pool (requires road connectivity) | **somebody else's — a trade** |
| `global` | City-wide Bins — the treasury, aggregate statistics | the city's |
| `map` | Map Layer cells under the Building's footprint — **write-only** | nobody's |

**The third column is load-bearing and is the subject of [`adr/0050`](adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md).** A scope is not answering *where do I look for this Bin*, it is answering *whose is it* — and a term crossing an **ownership boundary** is not a transformation but a **trade**, with the Good moving one way and money the other at the prevailing price. There is **no Ruleset syntax for that payment**: the price is emergent by §4, the quantity is already the term's `amount`, and the counterparty is implied by the scope, so nothing is left for a designer to write. This is what keeps `amount` a fixed integer permanently, since a variable *rate* is the one thing a derived apply count provably cannot express — `n` cancels out of a ratio. **`pool` is therefore a market, and an implementation treating it as a wider Bin lookup ships an unconserved economy.** It also makes `adr/0045`'s fallback ladder a **price** ladder as well as a source ladder: local → Pool → Shipment → import is monotone increasing in cost, which is *why* the rungs are in that order.

**Four, and there is no proximity scope.** `CONTEXT` previously described the non-local scope as *"Bins in nearby Buildings,"* which is a category error: proximity selection belongs to whatever is moving, never to the Rule engine. A Parking Shed (`adr/0009`), an Amenity set, a Provider List and `adr/0030` dispatch are all *nearest-first choices made by a Trip or a Household*. **Movers choose; Rules transform.** The Rule engine's one non-local scope is the District Pool, and the two radii cannot coincide: a District is bounded by where transport can be *ignored* (§2.1), and a shed is bounded by where transport must be *measured*, because the walk Leg is its whole output.

> **Two of the four are named holes, and this is a statement of design rather than of what runs.** Only `local` and `map` are implemented. `pool` needs road connectivity and `global` needs somewhere for a Bin nobody owns to live; both **throw when a Rule reaches one**, and neither is refused at load — deliberately, because a Ruleset naming a Pool is well-formed and merely early. The practical consequence is that **the worked chain below loads clean and throws the first time `draw_flour_from_pool` is reached**, and the contention example in §1's phase table, which contends over the Pool, is not reachable yet.

`map` is **write-only**, which removes it from the subscription question entirely. It is also not a
scope of a *term* in the way the other three are: an authored `map` output is split off at load into a
separate emission list, so nothing ever resolves a `map` reference to a Bin. **Only pollution is
implemented**; land value and sealing are named holes that throw when reached. Map Layer cells are written by a staggered double-buffered diffusion pass ([`05 §9`](05-technical-architecture.md)) rather than by a mutator, and they have no capacity to exceed — so a `map` output can never fail, and no Rule ever waits on one.

**Atomicity is the core semantic.** A Rule applies in its entirety or not at all. If any input is insufficient or any output would exceed capacity, nothing happens and the Rule *fails*. There is no partial application. This is what makes the economy conserved and what makes failure a real, reportable event rather than a silent partial-completion.

**Rate is a reschedule interval, not a polling period.** A Rule that fires successfully re-arms on the Event Wheel at `+rate`; nothing ever walks the Building list looking for work. A Rule that **fails does not re-arm on a timer at all** — it subscribes to the specific Bin that was short, or, if the failure was a full output Bin, to that Bin draining. The mutator that writes the Bin wakes the Rule. This is §7's *entities do not poll, mutators wake observers* applied to the Rule engine, and it is what stops simulation cost from scaling with how broken the city is: under polling, a starved District pays its entire `on_fail` chain every `rate` Ticks for as long as the shortage lasts, which is the moment the frame budget is already gone.

It also removes a lag nobody authored. Under polling, each level of an `on_fail` chain carries its own rate and its own phase offset — whichever Tick that Building was built on — so an arriving Shipment reaches the thing that needed it after a delay determined by construction order. Deterministic, but unreadable and unauthored.

Each Bin therefore carries **two** wait lists — one for Rules blocked on **level**, one for Rules blocked on **headroom** — and a subscription records **which Bin stopped the Rule and in which direction**. Two lists rather than one because the failure has two directions: a single list would wake a Rule waiting for the Bin to *fill* when it had just been *drained*, and neither would ever make progress. On a write the Bin drains **from the head, only while the Bin's current level — or headroom — covers the head's requirement, which is derived at the drain rather than recorded when the waiter failed** ([`adr/0063`](adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)), and **a Withdraw drains the headroom list exactly as a Deposit drains the level list**. A Rule that fires goes to the back of the queue.

> **This paragraph said *arriving quantity* until `adr/0063`, and the difference is a defect rather than a nuance.** A budget taken from one write's delta cannot be reached by a requirement larger than the granularity of supply: a consumer short of three fed by arrivals of one sleeps for ever while the Bin fills to its ceiling behind it — which is precisely the state [`adr/0033`](adr/0033-two-rule-families-scheduled-and-swept.md) and §10 both declare inadmissible, *no Rule asleep with all inputs satisfiable*. It went unnoticed for two 100,000-Tick acceptance runs because the invariant that would have caught it has never been implemented, and because `local` is the only scope with a Bin behind it, so no wait list in a running world has ever held two waiters.

**The requirement is what makes the queue mean anything**, and omitting it silently deletes the whole mechanism. Waking every subscriber instead would put them all into Phase 2 and all into Phase 3, where **small waiters would beat large ones on quantity** — a waiter needing six loses to waiters needing one that were woken beside it. Draining by requirement keeps contention out of Phase 3 in the chronic case entirely: a Bin holding six wakes exactly the one bakery that needs six.

> **An earlier revision of the sentence above justified this by *"§1.1's sorted-key settle order"* picking a permanent winner. That was a mis-citation of this document by itself.** §1.1 says contention is resolved *"by a counter-based random shuffle"* and warns in its own voice that a sorted key applied to chronic shortage *"produces permanent starvation rather than a gradient"*. The code agrees with §1.1 — the settle key is a draw over `(seed, instance, **tick**, purpose)`, re-randomised every Tick — so a permanent winner is not possible and never was. `CONTEXT.md` → Bin and the mutator's own doc-comment both inherited the error from here. Corrected in place; the worry it was reaching for is real and is the size bias now stated above.

The result is that the District degrades as a gradient — under half supply every bakery bakes half as often, rather than half the bakeries running normally while the rest starve. **This holds because servings are complete and turns rotate, and it is a claim about evenness *over time* rather than within a single arrival.** Dividing each delivery among the waiters instead would leave all three of three bakeries holding four of the six flour each needs and none of them able to bake — **zero output from twelve units of supply, where rotation produces two firings and immobilises nothing.** Even division of an indivisible threshold is therefore strictly worse than queueing, which is why accumulation is something a Ruleset may *author* — an acquisition Rule at `min = 1` feeding the consumer's own Bin — and never what the wait list does. Note the gradient was a defect under polling too, and an invisible one: every bakery polls, every bakery contends, and the lowest sort key wins permanently. Subscription did not introduce the problem; it gave it somewhere to be fixed.

A waiter's requirement can no longer go stale, because nothing stores it — which closes the same hole on two sides. A waiter whose own local Bins changed while it slept re-checks atomicity in Phase 2, fails, and resubscribes; a waiter whose *Ruleset* changed under it is now measured against the numbers in force rather than the ones it failed under, which is `adr/0015`'s acceptance test reaching the shortage path for the first time.

**Apply count** lets one rule evaluation apply `1..n` times. This is how throughput scales without adding entities — a large factory is not more buildings, it is a higher apply count. Important for keeping entity counts down.

**`n` is authored per Rule, and greed is one of the choices.** A Rule declaring `{min, max}` applies as many times as its inputs allow within that band, and **fails if it cannot reach `min`** — subscribing to the Bin that stopped it, against a requirement of `min × delta` that is **derived when the drain asks rather than recorded when the Rule failed** ([`adr/0063`](adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)). **The `delta` is the Rule's *net* movement on that Bin, not the term's amount** — a Rule naming one Bin on both sides moves only the difference, and a Bin drawn from and returned to in equal measure bounds nothing at any count. Atomicity then has a well-defined referent: `n` is settled first, and *those* `n` applications happen in their entirety or not at all.

**The count is decided against the Past, and Settle may only reduce it** ([`adr/0049`](adr/0049-a-rules-apply-count-is-decided-against-the-past-and-settle-may-only-reduce-it.md)). `n` is settled in Phase 2 against a single state; Phase 3's re-check may **reduce** it, or fail the Rule outright, and may **never raise** it. So Settle serves a greedy Rule *short* rather than enlarging it, and §8 rule 5's shuffle decides **who goes short when there is not enough — never how much anyone takes when there is**. Re-deriving the count from part-way through Phase 3 would make the same city on the same Tick eat either 12 or 24 flour depending on settle order, which is a tie-breaker becoming a mechanism.

**One case sits outside that rule, and it is not a hole in it.** A Rule that loses the Phase 3 re-check takes its fallback (§4.1's chain, below), and a link reached that way was never evaluated against the Past, so there is no count to reduce. Nor is the harm the same: what `adr/0049` forbids is a *producer's throughput* moving with settle order, whereas a rescue drawn from a contended shared Bin is precisely the *who goes short* the shuffle exists to decide — the contention case above contends over the **Pool**, and a `local` Bin has only one owner.

**Fixed is the degenerate case, not a second mechanism.** `min = max` is a Rule that performs a defined quantum or fails, so one form expresses both and there is no third spelling. Which one a Rule uses is a **modelling decision fixed at design time**, never a performance one — the same discipline §4 applies to the choice between the two Rule families, and for the same reason, since the two differ in observable behaviour:

> **Greedy when the actor works through its stock. Fixed when the actor owes a quantum.**

A bakery bakes the flour it has and a clinic treats who arrives; infrastructure **Upkeep** ([`adr/0035`](adr/0035-infrastructure-is-priced-by-what-it-consumes.md)) draws `construction cost ÷ effective life` per Day and must never draw more because the treasury happens to be full. Both cases already exist in the corpus, which is why greed cannot be a property of the engine.

**Burstiness is therefore authored rather than incurred**, and that matters because of what actually costs under subscription. A chain is walked **once on entry into shortage** and a chronically starved District walks nothing — so the cost driver is how often a Bin crosses the supplied/short boundary, not chain depth and not how broken the city is. This sharpens `adr/0033`: under polling the simulator is most expensive when the city is most *broken*, and under subscription it is most expensive when the city is most *unstable*. A greedy Rule drains to the floor and crosses that boundary more often than a fixed quantum of the same throughput, so a designer choosing greed is choosing a visible behaviour and paying for it. **Slice 7 ships the counters** — Rule evaluations per Tick, and walked chain depth per §9 — and the tripwire is stated over measured cost rather than over a depth nobody can price yet. **Both shipped in task 9, and a third went with them.** An evaluation is now one atomicity check — a head, each non-terminal link walked below it, and Phase 3's re-check — rather than a *due Rule Instance*, which "does not see a chain link at all" and would have left the tripwire unfalsifiable against the one quantity chain walking cannot move. The third counter is **due Rule Instances**, kept precisely because it is what the first is read against: *evaluations − due* is the entire cost of chain walking and re-checking, and neither number alone separates a larger city from a less stable one.

**Measured** ([`plans/0011`](../plans/0011-rule-engine-bins-and-rules.md) task 9): one evaluation is **82.84 ns**, flat to 1.8% across two orders of magnitude, so the 15.6 ms Tick budget holds about **188,000 evaluations**. ⚠ **That is a *synthetic* unit and a real world has since produced 552 ns** — 2.8× higher, attributed in `plans/0011` findings 42–43 to terms, scatter and population. It is quoted here because the *ratio* between a head and a rung is what this paragraph turns on and that ratio holds; the absolute must not be used as a budget row without its replacement (`plans/0013`, `plans/0012` **Cause 5**). A chain rung's *marginal* cost is **53.6 ns** — two-thirds of the head that failed, because the per-Rule overhead is paid once and amortised down the ladder — which is the first evidence behind this section's own claim that depth is not the cost driver. **What the same number says about the city is less comfortable**: at the unratified 450 Rule Instances per 1,000 Citizens and §4.3's own rate of 8, a 1M-Citizen city spends roughly **10.5 ms a Tick on the Rule engine alone — two-thirds of a 15.6 ms budget** — a whole-Tick figure, since measured, that includes Phase 3's intent sort. That sort is the one part of the engine that is not linear in the due count, and it costs 63% more per Rule at 100,000 due than at 1,000. The running total across every priced consumer is [`plans/0013`](../plans/0013-tick-budget.md), which also prices the same rows against 1×, 2× and 8×.

There is a second way to specify `n`, and it is introduced below: a Rule declares **either `{min, max}` or `derived`, never both**.

**Apply count may be derived rather than literal**, and this is what buys proportionality without an expression language. `amount` stays a fixed integer; `apply` may be computed from a **Readout** — a named read-only scalar. **The declared set currently has one member, `occupancy`**, because a Readout is admitted when a Rule reads it (§4.1, below); the examples in this section describe the mechanism and are refused by name at load until the state they read exists. *"15% of gross income"* is one unit of money applied `gross_income * 15 / 100` times — integer, deterministic, no floats, no parser. This is also why `CONTEXT` → Policy prefers percentages to flat amounts: **a percentage is an apply count; a flat amount is an amount.**

**A Rule declares either `{min, max}` or `derived`, never both**, and the discriminator is the Bin/Readout distinction arriving in the apply count:

> **Greed handles what is *consumed*. A derived apply count handles what is *consulted*.**

Flour and labour are spent, so stock decides `n` and no Readout is involved. **Fertility** is why derived counts remain necessary on Bin Rules rather than only on Policies: a farm's yield scales with fertility, fertility is never consumed, and greed cannot see it. Admitting both specifications on one Rule would also collide their failure semantics — below `min` is a failure, and a derived zero is a success.

**`mean_workforce_experience` was listed here and is struck.** Staffing already dissolves into a **labour input Bin** filled by arriving commute Trips (below), and experience folds into it as a **per-worker deposit multiplier** — a skilled worker deposits more labour units than a novice. Quality becomes quantity, which is what a production function is, and greedy apply then scales throughput with experience for free. Keeping it as a Readout would have cost three things: a **mean** is a division, so `sum / count` and then `× 15 / 100` round twice on a hash-bearing path where a sum rounds not at all; it would have leaned on the letter of `CONTEXT` → Building's *"a field that would have to be averaged across its Occupants is a Cohort forming"* on the ground that workers are not Occupants, which is the move that invariant exists to catch; and an unstaffed Building would have had a derived apply count of **zero**, which is a *success*, so it would re-arm on `rate` and produce nothing for ever without a failure, an `on_fail` chain, or a word of Evidence — the *silent non-event* this section bans predicates for. Summed workforce experience remains worth **displaying**; §2.5's test is what separates the two, and display-only is a display, never a Readout.

Readouts are what a Rule *consults*; Bins are what it *spends*. **The readable set is declared in the simulation, and every declared Readout is inspectable** — a Rule may read any of them, and none of them is hidden from the player. `CONTEXT` → Readout is the definition of record: *a named scalar an entity exposes*.

**The converse does not hold.** The inspectable surface is much larger than the Readout set — Bin levels, Occupants, which Rule a Building last ran, a Trip's Fate — and declaring a scalar a Readout is not a decision to *show* it but a decision to let every future Rule *act* on it. The test is §2.5's, the one that demoted service coverage to an overlay: **does a Rule read it, or is it only displayed?** Display-only is a display, never a Readout.

This bound used to run the other way, inward from §9, and that was an error rather than a ratchet. §9 is an obligation to **expand** an aggregate into its constituents, not an enumeration of scalars; there is no set there to validate an `apply` against. Worse, it made the Rule engine depend on a presentation design that does not exist — the shell owns how a Readout is *rendered*, never which ones *exist*, or `Core`'s no-strings rule is being enforced at the wrong boundary. Inverted, `LEGIBLE CAUSE` is discharged **by construction** rather than by reference: no Rule can act on a quantity the player has no way to inspect, because the declaration is the thing the inspector reads. It is `adr/0015`'s enumerability argument — *the balance surface is a file listing rather than an archaeology exercise* — applied to what Rules read rather than to what tunes them.

**A derived apply count of zero is a success, not a failure.** A farm on zero fertility applied zero times, re-arms normally on `rate`, and explains itself through its Readouts. Nothing is missing, so nothing is waited on — which matters, because a Readout is not subscribable and a zero-apply *failure* would have nothing to sleep on.

**Bin Rules have no predicates**, only derived apply counts. Predicates belong to Sweep Rules (§4.2), and admitting one here would break both halves of this section:

- **It would have nothing to subscribe to.** A predicate fails against a Readout, and Readouts are not subscribable, so the Rule would have to re-arm on a timer — reintroducing polling through a side door, on the mechanism that most wants to sleep.
- **It would be a silent non-event.** A Bin Rule's whole diagnostic story is the `on_fail` chain. A false predicate produces no chain, no reportable condition, and no Evidence — the only way in the design for a Building to do nothing without saying why.

Every case that appears to want one dissolves elsewhere: *"only produces if staffed"* becomes a **labour input Bin** filled by arriving commute Trips, which is better design because a failed commute then *causes* a legible production failure; *"only in dense Buildings"* is a different `kind`; *"only during trading phases"* is scheduling, since a phase change is an event and events wake things. When a case still seems to need a predicate, the tell is a physical input that has not been modelled yet.

**Fallback** (`on_fail`) chains to another Rule when this one fails. This is where supply-chain substitution lives, and it is free:

```
bake_bread            (consume local flour)
  on_fail → draw_flour_from_pool
      on_fail → request_shipment         (fills local flour, asynchronously)
          on_fail → mark_input_starved   (a reporting terminal — the chain stays failed)
```

That chain is the entire "why is this bakery not producing?" diagnostic, expressed as data. `LEGIBLE CAUSE`

**A link is not a Rule Instance of its own.** It has a `rate` like any Rule and is never armed on
it: a link exists to rescue its head and is reached only by walking a chain that failed. Arming one
would run it independently of the head it serves, and a reporting terminal armed that way would
report at its own rate for ever — the polling defect, arriving through the Rule Instance table rather
than through the walk. So a Building's armed Rules are its chain **heads**, and the links hang off
them as data.

**A rescue re-arms on the link's rate, not the head's.** The link is the Rule that actually ran, and
the head did not fire this Tick — it does not bake, because a link *refills its Bin* rather than doing
its work by another route. The head is then woken by the link's own deposit, through the wait list,
which is §7's mechanism and not a retry.

**A failed chain subscribes once, at its head.** Not once per link. The substitution this engine
models is *source* substitution — `CONTEXT` → Rule's *can't source locally → import* — and
[`04 §1`](04-economy-and-goods.md)'s Goods table is strictly linear, one input per Good, so every link
in a well-formed chain rescues by relieving the same Bin the head failed on. One subscription
therefore wakes on every rescue path, and **chain depth costs no subscriptions at all**. Depth is
then a `LEGIBLE CAUSE` question and never a cost one, which is also what makes collapsing a chain at
load time checkable against `05 §4`: it is an optimisation exactly while the head Bin, the
derived requirement and the wait-list insertion order are unchanged.

**Well-formedness is a load-time law, and it is refused rather than warned.** Every link must
**relieve** the head's blocking Bin — **outputting to** it, **drawing from** it where the failure was a
full output, or **declaring** that it fills it later: `fills = { scope, resource }`.
The declaration is what an asynchronous rescue needs: `request_shipment` dispatches a Shipment and
outputs nothing this Tick, so without it the link is indistinguishable from one that rescues nothing.
*Blocking* generalises over both failure modes — refill if the Bin was short, drain if it was a full
output. A chain that cannot satisfy this is a malformed Ruleset and is rejected with a file, a line
and a rule name (`adr/0015`), by the same load-time walk that rejects a cycle. **A chain that does not
end in a reporting terminal is refused by that walk too** — a chain which simply ends leaves the
Building failed with nothing recorded, which is the *silent non-event* this section bans predicates
for, so the terminal is a law and not a convention. The terminal check runs **before** the relieving
check, because a chain missing its terminal fails both and *"no Bin is relieved"* would name the head
for a defect in the tail. **The `on_fail` graph
is static**, so both checks are Ruleset validation and neither is a runtime guard.

**A reporting terminal is not a Rule that succeeds.** `mark_input_starved` records a reportable
condition and leaves the chain *failed*, so the Building sleeps on its subscription. Were it an
ordinary Rule it would succeed — recording has no input that can be short — and re-arm the head on
`rate`, walking the whole chain again every `rate` Ticks for as long as the shortage lasts. That is
precisely the polling cost `adr/0033` says subscription exists to remove, reintroduced through the
last link.

### 4.2 Sweep Rules

A **Sweep Rule** fires on a time trigger, walks a population, tests real simulation state via a **predicate over Readouts**, and acts on the members that qualify. Predicates live here and nowhere else — a Sweep Rule polls by design, so a predicate over an unsubscribable Readout costs it nothing, where the same predicate on a Bin Rule would force it awake (§4.1). It is attached to the city or to a District rather than to a Building, and it is **polled by design** — there is no named Bin for it to wait on, and an entity cannot know whether it qualifies without being evaluated, so the Event Wheel has nothing to sleep through. Polling here is the correct answer rather than a concession.

Two instances, and they differ on one axis:

| | **Zone Rule** | **Policy** |
|---|---|---|
| Population | Lots in a Zone | Households, Businesses, or Buildings matching a predicate |
| Coverage | **samples** a small random set | **sweeps** all of them |
| Acts by | creating, upgrading, downgrading or demolishing a Building | moving money, or constraining what may be built |

**Sampling versus sweeping is a semantic distinction, not a performance one.** A Zone Rule samples because sampling *is the behaviour model* (§5.3): developers genuinely do not evaluate every Lot, so a sample is more faithful than a scan as well as cheaper. A Policy sweeps because a transfer is not a behaviour, it is an **entitlement** — paying a random subset of the eligible would be a bug, not a model. Anything reaching for sampling to make a Policy affordable has confused the two.

**A sweep is cheap, and it is cheap the same way Map Layers are cheap** ([`05 §9`](05-technical-architecture.md)): low frequency plus stagger plus Chunk partition. Eight Policies over ten thousand Households is eighty thousand integer comparisons per Day — roughly ten per Tick once amortised — and it stays affordable at an order of magnitude more population. The instinct to distribute the work onto the entities themselves, arming a Rule on each Household, is a false economy: it performs the same evaluations, adds a wheel entry and a subscription apiece, requires every immigrating Household to be armed with every active Policy at spawn, and is *worse* at the one thing that motivated calling a Policy a Rule at all — `Evidence` expansion, where a centralised sweep **is** the expansion.

**Contention among sweepers is resolved by rotating the scan start.** A Policy paying out of a treasury that runs dry pays whom it reaches and reports where it stopped, which is legible; but a fixed scan order would pay the same low-index Households first for the life of the city. Rotating the start per trigger makes exhaustion a gradient across the population rather than a permanent boundary — the same argument as the round-robin wait list in §4.1, and cheaper to implement once centrally than distributed across ten thousand queues.

> **This paragraph read *"contention is resolved by rotating the scan start"* and was generalised from its one worked example, which is the Policy.** It does not reach a Zone Rule, and could not: **a sampler has no scan to start.** Nor is the bias the same one — rotation exists here because a treasury *is exhausted*, so a fixed order permanently excludes the tail of the population. Nothing a Zone Rule contends for is exhausted: the Rule that loses a Lot this trigger samples elsewhere on the next, and two Rules overlap at all only about `sample² ÷ Lots` of the time. **Zone Rule contention is settled by declaration order**, which is already hash-bearing because the Rule's index is a coordinate of its draw. Found by building the trigger, and `adr/0055` carried the same over-generalisation into a consequence line.

**Nor do a Zone Rule's costs need the stagger this section opens with.** Low frequency, stagger and Chunk partition are three mitigations for a per-trigger cost that *grows with the population* — which is a sweep's shape and not a sample's. A Zone Rule's per-trigger cost is the sample size in the Ruleset and nothing about the city, so its trigger is a plain `tick % interval` with no offset. Staggering it would be armour against the cost §5.7's tripwire exists to prove absent, and would put a second hash-bearing coordinate into the trigger to buy it.

**A Sweep Rule never subscribes.** Its trigger interval is a balance constant — a Policy paying daily is a different city from one paying weekly — so it is authored, recorded in the Ruleset, and not a scheduling knob.

### 4.3 Format

Rules live in data files, loaded at runtime, **hot-reloadable**. The compiled binary is a stable interpreter for the Ruleset. See `adr/0015`. `FAST ITERATION`

Starting format is **TOML**. Not because it is the most elegant expression of a rule, but because it requires no *bespoke* parser and `adr/0018` sets a standing bias against building bespoke infrastructure. ~~it requires no parser~~ — **struck, and it is the sentence that hid a dependency for six slices.** .NET has no TOML reader, so one is taken: **Tomlyn**, in `Borough.Formats`, named and argued in [`adr/0048`](adr/0048-the-ruleset-is-validated-where-it-is-parsed-and-only-integers-and-strings-cross-into-the-core.md), which also settles what may cross from it into the core. GlassBox's custom DSL was more readable, and a DSL remains a possible later ergonomics improvement — but it is a parser to write, test, and produce good error messages for, and that is not the first thing this project should spend effort on.

```toml
[[rule]]
name    = "bake_bread"
kind    = "bakery"
rate    = 10
apply   = { min = 1, max = 4 }
on_fail = "draw_flour_from_pool"

inputs = [
  { scope = "local", resource = "flour", amount = 6 },
]
outputs = [
  { scope = "local", resource = "bread",     amount = 1 },
  { scope = "map",   layer    = "pollution", amount = 2 },
]
```

**Hot reload is a day-one requirement, not a convenience.** Citybound's simulation became unbalanceable because a warm rebuild took 60–120 seconds, and its author's own final devblog admits he had "been abandoning the simulation aspect for a while" in favour of the parts he could iterate on. The test we hold ourselves to: **changing a production ratio and seeing the effect takes seconds.**

**There is no money term, and the absence is the whole of [`adr/0050`](adr/0050-crossing-an-ownership-boundary-is-a-trade-and-payment-is-implicit-in-the-scope.md) applied.** Every term here is `local`: the bakery's own flour becoming the bakery's own bread. No ownership boundary is crossed, so nothing is bought and there is nobody to pay. The money appears one rung down the fallback ladder — `draw_flour_from_pool` names `pool`, which *is* a boundary, so that Rule is a trade and its payment is implicit at the prevailing price. A designer never writes the money side of a purchase, because §4.1's third column has already said who owns what.

Money is nonetheless **held** at `local` scope by every actor that has any — a tax, a wage, a subsidy is paid *from somebody's own Bin* (`adr/0024`). No actor's balance is ever `global`, and that is not a detail: with balances at `global`, every money-moving Rule in the city would subscribe to a single Bin and every tax collection would wake ten thousand of them.

`global` names the **treasury**, and it appears only as the far end of an explicit **transfer** — `local` money out, `global` money in, balancing within the one atomic Rule. That is the shape the loader accepts and the only spelling available for a counterparty that is not a market: a `pool` term is a *purchase* under `adr/0050` and authors no payment at all, and a `local`→`local` money pair nets to zero and bounds nothing. So the treasury is a destination, never a balance-holder in the sense actors are. **No transfer executes today**, since `global` throws.

> **This example previously drew one money and output none — it destroyed money, which `adr/0024` forbids, the Outside Connection being money's only sink.** Found while grilling slice 7 task 6, six slices after it was written; the loader now refuses the shape outright (refusal 4), so the document and the interpreter cannot disagree about it again. The finding worth keeping is *why* it survived: the transcription into the loader's own test fixture had silently dropped the money line, so the corpus's flagship example was never actually run. Recorded in [`plans/0011`](../plans/0011-rule-engine-bins-and-rules.md).

Reload semantics: the Ruleset is swapped at a phase boundary. Bins whose resource no longer exists are dropped with a logged warning. Buildings whose kind no longer exists are marked derelict rather than deleted. **All wait lists are dropped and every Rule is woken with a stagger**, since a subscription taken under the old Ruleset may name a Bin the new one does not have — which also means a wait list is never cross-version state. Reload is recorded in the Input Log as a **transition carrying both Ruleset content hashes**, not merely as an event. The **hash** is what travels in the log, and the **content** travels beside it: a log is shared between people who have the Rulesets in a repository, and a crash artifact is attached to an issue by somebody who may not, which is why [`05 §7`](05-technical-architecture.md) makes the artifact the thing that carries content. Carrying both hashes rather than one is what lets a *spliced* log be caught at parse time, since each transition states the Ruleset it leaves as well as the one it enters. Because the degradation above is a pure function of `(state, old Ruleset, new Ruleset)`, a logged transition replays exactly; see [`05 §7`](05-technical-architecture.md).

---

## 5. How growth emerges

This is the section the whole design exists for. `EMERGENCE` `PLAYER GOVERNS`

> **Prior art note.** What follows is a game-shaped version of an established academic field: **integrated land-use and transport (LUTI) microsimulation**. UrbanSim (Waddell et al. 2003) does substantially this — household location choice via discrete choice models over sampled alternatives, coupled to accessibility from a transport model, with developer behaviour responding to unmet demand. See [`references.md` §1](references.md). The structure is worth stealing; the calibration apparatus is not, since a game has no census data to fit against and player legibility matters more than statistical fidelity. **This section may be revised once that literature is properly digested.**

### 5.1 The thing we are not doing

In SimCity, RCI demand is a global scalar produced by a formula over tax rates, existing zone ratios, and external pressure. Zones then grow toward that number. It works, it is cheap, and it is a lie the player can never see through — which also means the player can never *reason* through it.

**There is no demand scalar in this design.** Growth has a specific cause every time, and that cause is inspectable.

### 5.2 The loop

Adapted from UrbanSim's architecture, which has been the operational design of a production planning model since roughly 1998. Runs on a slow cadence — a matter of Days rather than a Tick. There is no calendar; see `adr/0010`.

```
1. Transitions        new Households and jobs enter the unplaced pool;
                      some existing Households decide to move and re-enter it
2. Household placement    for each unplaced Household, in seeded order:
     a. sample N candidate Lots/dwellings (see 5.3)
     b. hard-filter: affordable? at least one reachable job in budget?
     c. score survivors → utility
     d. probabilistic pick (see 5.4)
     e. consume the dwelling — capacity is real
        if no survivors: stay in the pool, WITH A RECORDED REASON
3. Business placement     same shape; commercial seeks unserved needs in reach,
                      industrial seeks reachable inputs and reachable buyers
4. Price adjustment   per submarket: price *= clamp(demand/supply, 0.9, 1.1)
                      2–3 iterations
5. Development        per Lot × allowed form: revenue(price) − cost > hurdle?
                      build the best feasible, throttled by a build rate
6. Accessibility      refresh travel-time matrix and reachability fields
                      (slow cadence; "dirty regions only" is UNSOUND — see below)
```

> **Step 6's *dirty regions only* is unsound, and it was measured rather than argued** — S2 R1.7, in
> [`spike-results`](spike-results.md). *(The corpus cites this step as `02 §6`; the sentence it quotes
> is this one.)*
>
> **A dirty region is a spatial test on a non-spatial quantity.** A route from District *i* to District
> *j* can cross the edited ground without either endpoint lying anywhere near it, so rebuilding *the
> Districts the edit overlaps* misses exactly the long entries the matrix exists to serve — **309 of
> 429** changed entries on a central edit, **132 of 252** on a corner one. It misses them **silently**,
> leaving entries stale rather than merely coarse, which is a different and worse failure than
> coarseness because nothing downstream can tell the two apart. Both edit sites are quoted because one
> of them is degenerate: a central District lies on the shortest path between most pairs and a corner
> one on almost none, and the contrast is what says the mechanism has a shape rather than a verdict.
>
> **The sound test exists, is very nearly exact, and collapses into the full rebuild.** *Which stored
> routes crossed the edited region* identifies **430 entries against the 429 that actually changed**, so
> as a predicate it is essentially perfect — but a one-to-all search fills a whole row, so the build
> granularity **is** the row, and every row holds the entry addressed *to* the edited District, whose
> route necessarily ends inside it. **However few entries an edit invalidates, at least one lands in
> every row.** So the two columns say different things and both are the finding: **430 entries is the
> work genuinely needed — 2.9% of the matrix; 121 rows is the work the structure forces — 100%.** Going
> finer means a point-to-point search per entry, priced at 418 µs, so filling one row that way costs
> **50.6 ms** against the one-to-all's **1.46 ms** — entry-granular invalidation is **34× more expensive
> per row** than the rebuild it exists to avoid, and pays only when fewer than one row in thirty-four is
> dirty. R1.7 showed every row is.
>
> **The cadence survives; the region does not.** This is [`CONTEXT.md`](../CONTEXT.md) → Epoch's *when
> you pay* / *what survives* distinction arriving at the matrix instead of at the cache, and the two
> halves come apart here exactly as they do there. *Slow cadence* is a statement about **when you pay**
> and it stands — a matrix rebuilt every *N* Ticks is stale by at most *N*, and everything reading it
> can be told so. *Dirty regions only* is a statement about **what survives** an edit, and a spatial
> predicate cannot make it. **The matrix carries no Epoch and R1 declined to give it one**, on the
> grounds that a version counter would imply a relationship to the route cache nobody has argued.
> **Which rebuild rung the matrix takes is therefore open** — see [`plans/0002`](../plans/0002-open-questions.md) —
> and nothing here chooses one; what is settled is that the rung written above is not available.

> **Which of these six steps exist, stated here because this section has twice been read as a
> description of the build.** **Steps 2 and 5 are built**; steps 1, 3, 4 and 6 are not, and step 6's
> *dirty regions only* is separately unsound, below. Step 5 is slice 10's Zone Rules. **Step 2 shipped
> 2026-08-11** ([`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md)):
> a sampled Phase 6 pass ahead of the Zone Rules, draining the Pool into vacant capacity declared by a
> Building's kind ([`adr/0068`](adr/0068-a-buildings-occupancy-is-declared-by-its-kind-and-an-over-capacity-building-evicts.md)),
> stated in a Ruleset's `[placement]` table and **blind** for `adr/0054`'s stated reasons — **step 2b's
> hard filter is the one part of step 2 that does not exist**, because it needs a price surface and a
> Commute Budget and has neither. A seeker looks at `candidates` Lots and takes the first dwelling with
> room.
>
> **What the omission cost is worth recording.** Until session N task 2, `World.Place` had exactly one
> caller, inside the Zone Rule's create predicate — so construction was doing placement's job one
> Household deep, and a 100,000-Tick run settled at ~300 of 360 Households homeless — with **45% of the
> housing stock standing empty**, which is the figure that says it was a mechanism and not a number.
> Building it moved that to 10%. **Two ledger entries
> read that as a missing *number*** (an occupancy nobody had declared) rather than as a missing
> *mechanism*, because an unbuilt step in a specified loop does not look like a gap. It looks like a
> constraint. → [`plans/0012`](../plans/0012-corpus-audit.md).

**The unplaced pool with per-Household refusal reasons is our replacement for the RCI meter, and it is a strictly better interface primitive.** "412 Households want to move in; 380 can't find anything under §900; 32 can't reach a job inside their Commute Budget" is a diagnosis. A bar chart is not. `LEGIBLE CAUSE`

Note that this loop does not guarantee everyone gets housed, does not solve an assignment problem, and does not globally optimise. It processes agents, consumes units, and leaves a residual. **The residual is the demand signal** — and it is a list of specific frustrated Households with specific reasons, which the commercial and development logic can read directly.

**Households give up.** After a bounded number of failed cycles, a Household in the Unplaced Pool departs the city permanently and is counted as an **unhoused departure**. This is not merely a bound on the Pool (though it is that, and see `adr/0006`) — it models a real part of demand. People do not wait indefinitely for housing in a city they cannot get into, however attractive it looks.

More importantly, it produces a **second, distinct demand signal**. Pool size is a *stock* of latent demand; departure rate is a *flow* measuring how badly the city is failing to convert its own attractiveness into capacity:

| | Low departures | High departures |
|---|---|---|
| **Low attractiveness** | Stagnant — nobody wants in | Dying — people leaving for other reasons |
| **High attractiveness** | Healthy growth — supply keeping pace | **Housing crisis** — a desirable city starved of housing |

That bottom-right state is invisible in Pool size alone, and it is exactly the failure a city sim should be able to name.

**Unhoused departures and housed departures are counted separately** — the first says *build more*, the second says *fix what you have*. Lumping them into one emigration number destroys the diagnosis.

**Rejected alternative: gating immigration on vacancy.** Not creating Households the city cannot house would also bound the Pool, and it is the tempting fix because it prevents the problem at source. But it inverts the causality the entire growth model rests on: demand must be *observed and unmet* for prices to rise and the development trigger in §5.6 to fire. A city where nobody ever fails to find housing never builds any.

### 5.3 Sampling is a behaviour model, not an optimisation

Households evaluate a sample of `N` candidate dwellings, not all of them. Two things about this:

**First, it is theoretically sound.** Under *uniform* random sampling of alternatives, a logit choice model needs **no correction term** — the sampling probability is identical for every alternative and cancels out of the choice probability entirely. (McFadden's correction becomes necessary only under deliberately biased sampling, and even then only for *estimating* coefficients from observed data, which we never do because we author them.)

**Second, and more important: in simulation this is not an approximation of anything.** It *is* the behaviour model. A Household that considers twenty dwellings and picks the best-with-noise is a bounded-rationality model, and it is more realistic than one that surveys every vacant unit in the city. `BOUNDED KNOWLEDGE`

Therefore **`N` is a gameplay dial, not a performance setting**:

| `N` | Effect |
|---|---|
| small (5–10) | Households are myopic. Search is local, the city becomes sticky and path-dependent, and pockets of persistent vacancy sit next to shortages. |
| ~30 | UrbanSim reports allocation results stabilising here — roughly where the market stops being noticeably frictional. |
| large (50+) | Households are near-omniscient. The market clears efficiently and the city is smooth and boring. |

We should sit deliberately below 30.

**Bias the sampler on purpose.** Sample near the Household's workplace, or near their current dwelling, or near where they have social ties. Encoding *how people search* in the sampler is cheaper and far more legible than encoding it in the utility function. Real households search near their job or their old neighbourhood; that belongs in the sampling step.

Two implementation notes: **sample without replacement** (UrbanSim's own code samples *with* replacement, which double-counts an alternative's weight — a real if minor defect), and **sample at the Lot or Building level rather than the individual-dwelling level**, which sidesteps most of the substitution-pattern problem discussed in 5.4.

> **The Zone Rule's sampler does draw with replacement, and that is not this rule being broken.** The defect above is a doubled **weight**, which needs a score to double; `§5.7`'s create predicate is a boolean that admits or declines, so a repeated draw there costs one wasted evaluation and biases nothing. The rule stated here governs **this** sampler — a Household ranking dwellings — and it becomes binding on the Zone Rule the day `§5.4`'s choice model gives it something to rank. Recorded because the two samplers are one sentence apart in this document and the distinction between them is a *scored* choice, not a scale.

### 5.4 The choice itself

Score each surviving candidate, then pick probabilistically rather than deterministically:

```
P(i) = exp(μ · V_i) / Σ_j exp(μ · V_j)
```

That is a softmax over the scoring function — about ten lines of code, closed form, no iteration. This is the multinomial logit model, and the reason to use this form rather than any other is precisely that it has a closed form and therefore costs nearly nothing per agent.

> **`exp` here is a tabulated fixed-point function, not `Math.Exp`** — [`adr/0003`](adr/0003-deterministic-integer-simulation.md) bans the latter, and this line is why the ban needed an exception written rather than assumed. **The table's resolution is a stated figure**, because it perturbs the effective `μ` below, and `μ` is what prevents the stampede described two paragraphs down.
>
> **The algorithm is recorded, not frozen.** The interface is *a scored candidate list in, one choice out*, which makes the internals cheap to swap — so they are left to be settled by how the housing market **feels** rather than decided upfront. **Gumbel-max** — `argmax_i(μ·V_i + G_i)` with `G_i` from a fixed Gumbel quantile table — is *exactly* the same distribution, needs no `exp` on the hot path, and is cheaper; a coarser or finer softmax table is the other lever. Note that swapping is **hash-breaking but distributionally neutral**, so it invalidates stored replays and State Hash baselines and must be done deliberately with a re-baseline, per `adr/0003`.

**The framing worth internalising:** the randomness is not "agents behave randomly." It stands in for preferences we chose not to simulate — the view, the neighbour, the fact that the landlord was rude. Under random utility theory, `V_i` is the part we model and the noise is everything else. That gives us a *principled* reason for stochastic agents rather than "we added jitter so it looks organic."

**`μ` (the scale parameter) is a free design knob**, and the literature will not tell you this because it is fixed at 1 by convention for reasons that only apply when fitting coefficients to data:

- `μ → ∞` — deterministic. Every Household picks the single best dwelling. The city becomes degenerate: everyone stampedes to the best block, prices spike, everyone stampedes elsewhere. This is the same oscillation pathology that damped congestion feedback exists to prevent.
- `μ → 0` — uniform random. Utility is ignored entirely.
- `μ ≈ 1`, with utilities scaled so meaningful differences are 1–3 units — pleasant. Better options are meaningfully more likely; the city does not degenerate.

**When the city feels too herdy or too random, tune `μ`, not the coefficients.** Consider exposing it as a difficulty or realism setting.

Two consequences of the logit form that matter:

**Only utility differences matter.** Adding a constant to every alternative changes nothing. So "everything available is terrible, nobody moves in" requires an explicit **stay-put / no-choice alternative** with its own utility. We need that anyway.

**That alternative is the Hinterland**, and it is not a special case in the choice model — it is a row like any other. Each map edge carries an economy described in the same fields a District exposes (median rent, median wage, service levels, a commute figure), so a prospective Household compares *staying outside* against *moving here* with the identical utility function. This is what makes immigration a consequence of the choice model rather than a system beside it, and it is why the anchor is authored in **domain units rather than utility units** — `rent §620` is a number a designer can defend and a player can read off a panel. See [`adr/0023`](adr/0023-immigration-arrives-through-the-gate.md).

**Use `log(1 + x)` for count-like terms** — jobs reachable, shops nearby. Diminishing returns, so the 500th reachable job matters less than the 5th. Without it the city centre wins forever.

**Hard constraints are filters, soft trade-offs are utility.** "Can I afford this at all" and "is any job reachable" should eliminate candidates *before* scoring, not appear as large negative coefficients. Faster, numerically safer, and far more legible — we can tell the player "37 Households left because no job was reachable," which a probability shift cannot.

> **Alternative worth evaluating: multiplicative utility.** SILO aggregates utility components as a *product* rather than a sum, so that **zero on any single component yields zero total**. That expresses "no amount of cheapness compensates for zero reachable jobs" structurally rather than via a filter or a steep penalty, and makes constraints feel like constraints. Possibly a better fit for our needs model than additive scoring. Open.

### 5.5 Commercial and industrial growth

Same machinery, different scoring terms.

**Commercial** opens where there is an **unserved need in reach** — Households within an acceptable trip cost whose Provider List has no entry for a Good, or whose current provider is failing them on price or distance. A real query over real Households, not a percentage. Commerce therefore follows residential with a lag and clusters where accessibility is good, emergently.

**Industrial** opens where inputs are reachable and outputs have a buyer — another Building that consumes them, or an Outside Connection. Industry clusters near freight access and near its own upstream without being told to. Its two families differ in what fixes them: **Extraction** is pinned to the *ground* (Fertility, Woodland), **Processing** to *reachability* of inputs and buyers.

**Office** opens where **labour of the right mix is reachable** — highest tier-3 share of any use, but janitors and administrators too, so it cannot staff itself in a city with no tier-1 or tier-2 employment. See [`adr/0026`](adr/0026-wages-are-posted-locally-and-never-cleared.md).

Its location behaviour is unlike anything else here, because **it is the only use with no logistics constraint at all.** It ships no Good, so no freight access is required and no Vehicle carries its output; accessibility to workers is its *only* spatial input. Given the bid mechanism below, it therefore outbids every other use for the most accessible land in the city.

> **That is a central business district, emerging.** Nothing declares a centre, no rule mentions one, and no Zone is named "downtown."

Two forces stop it becoming a monoculture, and both are already here. Offices house nobody, so a pure office core makes **every** worker commute in at the same phase of the sun arc — it strangles itself on Segment Stress and Commute Budgets before it can finish forming. And *Amenity* rewards walkable variety, so a centre of offices, shops and homes outperforms a centre of offices alone.

Its exports still need a **gate**: Office generates Trips to Outside Connections in proportion to export volume — business travel — so a metropolis cannot export through one lane, and Office wants to be central *and* well-connected outward. Which is where real central business districts sit.

**Agglomeration is several distinct forces, not one**, and each needs its own term or the clustering behaviour will be wrong:

- *Productivity* — industry near industry is more productive. Benefits businesses.
- *Amenity* — **the count of distinct Business types reachable on foot**, with `log(1+x)` diminishing returns. Benefits Households. *Walkable* is load-bearing: it is what makes mixed use pay, and it is the payoff [`adr/0008`](adr/0008-walking-is-a-simulated-leg.md) promised — *"a corner shop is viable because people can physically reach it on foot."*
- *Congestion and cost* — density raises rents and commute times. Hurts everyone.

With all three present, agglomeration *and its limits* both emerge. With only the first two, cities grow without bound.

**Resolving competition for the same Lot: bid price in a single currency.** Rather than a separate placement system per Zone family fighting the others, every use expresses its desire for a Lot as a bid, and the highest bidder wins. Unified, and legible to the player — "a developer outbid the shop for this lot."

### 5.6 Prices, and what actually triggers construction

This is the loop closure, and it is what makes growth self-regulating without a demand scalar.

**Price adjustment is a tâtonnement using our own choice model as the demand function:**

```
for a few iterations:
    demand[submarket]  = Σ choice probabilities over that submarket
    supply[submarket]  = count of available dwellings
    price *= clamp(demand / supply, 0.9, 1.1)
```

Note the elegance: demand here is not a separate estimate — it is the sum of the same choice probabilities the Households use. Raise the price, utility falls, probability falls, demand falls. We get the demand curve for free once the logit exists.

Tighter clamping and fewer iterations than UrbanSim's defaults (±25%, five iterations), so players see prices drift rather than snap.

**We deliberately do not implement a hedonic price model.** UrbanSim regresses `log(price)` on building and location attributes, fitted to observed sales data. That is a statistical summary of a real market we do not have and cannot fit. Initial prices come from a cheap proxy — construction cost plus a land-value field — and the tâtonnement does all the work from there. Keep mechanisms, discard statistical summaries.

**Construction trigger: local price × buildable capacity versus cost.** For each Lot and each form allowed by zoning, estimate revenue from the current price surface and cost from construction plus land, and build when return clears a hurdle rate.

> **None of that exists, and what stands in for it is a documented vacancy reason rather than an approximation of it.** There is no price surface, no capital and no bid, so slice 10's create predicate is **vacant AND permitted AND a Household in the Unplaced Pool would take it**. The third term is not a weakened pro-forma: `CONTEXT` → Frontage lists the four answers to *why is this Lot vacant*, and *"no Household in the Unplaced Pool that would accept it"* is one of them, **beside** *no capital* rather than downstream of it. What is missing is therefore missing rather than faked, and the Pool is also what makes growth self-limiting.
>
> **⚠ AMENDED by [`adr/0069`](adr/0069-placement-is-a-mechanism-of-its-own-and-construction-houses-nobody.md). The superseded wording was:** *"creation drains the signal that authorised it, so no Ruleset can build past its demand however wide its sample."* **Construction now houses nobody**, so that sentence has nothing to drain and the property comes from the **ordering** instead: placement runs ahead of the Zone Rules in Phase 6, so the Pool a developer reads holds only Households the standing stock could not house. **The replacement is strictly stronger than what it replaces** — the old predicate read a Pool that construction drained one Household at a time, so a wide sample *could* build ahead of demand by up to the sample size within a single trigger, and post-placement it cannot.
>
> **Which Household moves in is drawn, never queued** — and under `adr/0069` it is **placement** that draws, not creation. The drain is blind ([`adr/0054`](adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)) because acceptance needs rent, a commute and a tolerance, so any member would take the dwelling and nothing is contested — but §8 rule 5's *reason* still binds. A Pool that never fully drains is what a housing shortage **is**, and under any fixed order the same Households would remain unhoused for the life of the city with nothing in any readout to explain why.

So the full causal chain is:

> Households can't find housing → they stay in the pool → demand/supply in that submarket exceeds 1 → prices rise → the pro-forma flips positive → a developer builds → supply appears → prices relax.

No global demand scalar anywhere in it, and every step is inspectable.

### 5.7 Development rate limiting

Growth is paced by four mechanisms, all diegetic:

- **Sampling.** Zone Rules evaluate a small random set of Lots per cycle rather than scanning. Cost is constant regardless of Zone size — GlassBox's trick, and the right one.
- **Construction time.** A Building under construction occupies its Lot and produces nothing.
- **Capital.** Development draws on private capital fed by economic activity. A poor city builds slowly.
- **Build rate throttle.** A cap on projects started per cycle, so a price spike does not produce instant citywide construction.

> **Measured by S0b, and the first bullet is doing two jobs that point in opposite directions.**
> See [`spike-results`](spike-results.md) → S0b, findings 1–3.
>
> **The cost claim is true and stronger than stated.** Raising the sample **117×** at 1,000,000
> Citizens — enough to look at every one of 120,001 Lots once a Day — cost nothing outside noise. Cost
> has never been the constraint here. ~~and `ZoneSample.Draw`'s `O(sample²)` duplicate scan is
> amortised by the interval.~~ **Struck: the scan is gone** — it was quadratic in a quantity that is
> now proportional to the map, and deduplicating within a trigger never bought coverage in the first
> place, only a skipped second look at a Lot. Sampling is with replacement, which `02 §5.3`'s
> criticism of UrbanSim permits here and only here: a duplicate costs an evaluation and not a
> **weight**, because this create predicate is a boolean with no score. `§5.4`'s choice model is what
> would reverse that.
>
> **The pacing claim does not survive.** `sample ÷ interval` is an **absolute** throughput while the
> population it draws from is the Lot table, so the period in which a given Lot is looked at once is
> `Lots ÷ (sample ÷ interval)` — **0.12 Day at 1,000 Citizens and 117 Days at 1,000,000**, on the
> shipped `[[zone_rule]]`. The whole occupancy trajectory collapses onto
> **τ = (sample ÷ interval) × Ticks ÷ Lots**, confirmed to 1.6 points across a 1,000× span in Lots and a
> 117× span in sample, so this is structural and not a badly chosen number. **An absolute throughput
> against a growing city is a decelerating city**, and *sampling* therefore paces **cost** rather than
> **growth**. Of the four mechanisms above only **capital** scales with the city; the **build rate
> throttle** is absolute too, so two of the four pace in a unit this section never names.
>
> **Settled by [`adr/0059`](adr/0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)**:
> a Zone Rule authors a **revisit period** and the engine derives the count, so the first bullet above is
> split — **sampling paces cost, and capital paces growth.** Until capital exists, growth is bounded by
> the Unplaced Pool, which is `CLAUDE.md`'s *the Pool is the demand signal* doing the job on its own.

### 5.8 Where we deliberately deviate from the academic models

Recorded explicitly, because each deviation is a decision someone will later question.

| Deviation | Reason |
|---|---|
| Author coefficients directly; never estimate them | No ground truth exists. This deletes the single largest cost of every model in this literature — the calibration apparatus — and the correctness criterion is legibility and feel, not matching a census. |
| Treat `μ` and `N` as design dials | Both are fixed by convention in the literature for identification reasons that do not apply to us. They control herding and market friction respectively. |
| Small `N` on purpose | Bounded rationality produces sticky, path-dependent, interesting cities. Large `N` produces efficient, smooth, boring ones. |
| Hard filters (or multiplicative utility) for hard constraints | Legibility. "No job in reach → excluded" beats a large negative coefficient the player cannot see. |
| Keep the tâtonnement, discard the hedonic model | Mechanisms transfer; statistical summaries of a real market do not. |
| One seeded run, no Monte Carlo averaging | We are not forecasting. Variance is content, not error. But this makes **per-agent seeded RNG streams** a hard requirement so that iteration order and parallelism cannot change outcomes — see §8. |
| Sub-annual cadence | UrbanSim's annual step is an artifact of census and travel-survey data cadence. Players need feedback in seconds. |
| Never resolve a route inside the choice loop | The one thing UrbanSim gets architecturally right that we must not violate. Accessibility is a slowly-refreshed field read in O(1). See §6 of `references.md` on why this constrains the routing decision. |

### 5.9 Decline and abandonment

The reverse process, and it must be as legible as growth.

A Building accumulates **failure pressure** from:
- Trips to or from it failing (no route, over Commute Budget, stranded)
- Rules repeatedly hitting their terminal fallback (input starvation)
- Local conditions falling below its occupants' tolerance

Past a threshold, it **loses occupancy and quality**. Past a further threshold, it is **abandoned — emptied, and left standing on its Lot.**

⚠ **This line said *"its Lot returns to vacant"* and contradicted the paragraph twelve lines below it, which is the reading that stands** ([`adr/0091`](adr/0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md)). A condition cannot be *"retained on the Building"* by a Building whose Lot has gone back to vacant, and the build implemented the wrong half — `World.DestroyBuilding` frees everything. Three things depend on the shell standing: contagion needs a **carrier**, since bare ground has no dereliction term in the desirability composition; `01 §6`'s sustained-detection duration is *derived from* how long contagion takes to reach neighbours, so with no contagion it is somebody's guess again; and `01 §6` and §5.2 above both treat clearance of abandoned stock as a lever separate from rezoning, which needs something to clear. The **sink** that keeps standing abandoned stock inside [`adr/0006`](adr/0006-no-collection-grows-with-elapsed-time.md) is the one an occupied Lot already has — redevelopment when land value falls far enough to pencil (§5.5), plus the player's `Demolish` and the `Govern` clearance programme. It is bounded by the Lot count, never by elapsed time.

**An abandoned Building is not a *derelict* one and the two share no machinery.** `CONTEXT.md` → Derelict is what a **Ruleset edit** does to a Building whose kind is no longer declared ([`adr/0057`](adr/0057-dereliction-is-a-design-time-state-and-it-is-derived-rather-than-recorded.md)); abandonment is what the **city** does to one, and only the second has a cause worth reporting.

**Pressure is a *duration*, not a tally of failure events** ([`adr/0053`](adr/0053-failure-pressure-is-a-duration-not-a-tally.md)). It measures how long the Building has been continuously failing, and it resets the instant the Building stops — so recovery is total rather than a debt worked off, and it needs no decay rate to stay bounded because nothing accumulates.

> **Counting the events instead inverts severity, and the reason is not local to this section.** A Rule that fails does not retry; it subscribes to what it is short of and sleeps ([`adr/0045`](adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md), and `§4.1`'s *"a starved District costs nothing until supply arrives"*). So a **comprehensively** starved Building emits exactly one failure event and then silence, while an **intermittently** supplied one wakes and fails repeatedly. Tally them and the healthier Building is condemned while the dead one is preserved. The threshold is therefore authored in **missed firings** rather than Ticks, so that a Ruleset which retunes every `rate` cannot silently retune every Building's lifespan.

**Sampling reads that duration; it never produces it.** A Zone Rule's sample is when the city *notices* a condemned Building, not when the Building fails — the pressure was already true before the sample arrived. That distinction is what makes sampled decline legitimate at all, since `CONTEXT` → Zone Rule justifies sampling by *developers do not evaluate every Lot*, an argument about an actor choosing among alternatives, and **abandonment has no actor**.

**The Occupants are evicted into the Unplaced Pool, with their Money and Savings intact** ([`adr/0054`](adr/0054-a-demolished-buildings-households-are-evicted-into-the-unplaced-pool.md)). Eviction is the Pool's fourth entry route and the only one the Household did not choose. Destroying them instead would delete their Money, which [`adr/0024`](adr/0024-money-is-conserved-and-the-city-has-a-balance-of-payments.md) forbids — the Outside Connection is money's only sink — and would be an unbounded population sink with no Departure record.

**Buildings do not shrink.** An earlier version of this section said a Building "declines a density level," which is physically incoherent — nothing gets shorter. The density ladder is walked at construction only, and the band is re-tested when the Lot redevelops. See [`adr/0025`](adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md).

**Abandonment is contagious, and that is deliberate.** It feeds the desirability composition negatively, so an abandoned Building raises its neighbours' failure pressure. This is a **cycle, not a spiral** — the bid-price mechanism in §5.5 damps it at the bottom, because land value eventually falls far enough that redevelopment pencils. It is also what stops neglect being *containable*: without it, ignoring a poor District would be free. **The specific accumulated condition is retained on the Building and shown in the inspector** — "abandoned: 74% of work trips exceeded commute budget over 30 days" rather than a sad-face icon.

This is where the SimCity 4 lesson binds: **the cost function used for routing must be the same quantity used to judge trip failure, and the same quantity shown to the player.** SC4 routed on distance while the player was scored on time, and the traffic system became unlearnable as a result.

---

## 6. Goods movement

Detail lives in `04-economy-and-goods.md`; the model boundary belongs here.

**Within a District:** Goods pass through the District **Pool** instantly, subject to the Building being road-connected to the district network. No Vehicle is simulated. This is Anno's abstraction and it is deliberate — logistics is only simulated where the player is meant to make decisions.

**Between Districts, and to/from Outside Connections:** a **Shipment** is created and carried by a Vehicle on the Road Graph, contributing real congestion.

The boundary is **swappable per Good**. If profiling or playtesting shows a particular Good should be physically moved within districts too, that is a configuration change rather than a rewrite. This is deliberate hedging on the one abstraction most likely to be wrong.

GlassBox made everything agent-carried, and every carried unit became a pathfinding query — a direct contributor to its 2km map cap. We are not repeating that, but we are also not certain the line is in exactly the right place.

---

## 7. Sleeping and the Event Wheel

The single largest performance lever, and it costs no concurrency complexity. `FAST ITERATION`

Every scheduled row carries a `next_event_tick`. Buckets are keyed by that value; each tick, only the current bucket is processed. A Citizen at work for a third of a Day sits in exactly one bucket and is touched once — which requires `WHEEL_SIZE` to be at least as long as the longest routine sleep. See §1.2.

> **Corrected by session C, on three counts.** See [`adr/0056`](adr/0056-the-event-wheel-is-two-levels-ticks-and-days.md).
>
> 1. **This sentence used to read *"every Building, Household, and Citizen"*, and named the wrong owners.** The column belongs to whichever **table** is scheduled — there is one wheel per scheduled table, because `adr/0004` rejected an ECS and `adr/0036` bans reference types, so a single wheel over heterogeneous entities has nowhere to put a `(kind, slot)` tag that is not itself a collection. Today the only consumer is the **Rule Instance**. A Building has no event of its own at all, and **a wheel is added when its consumer exists** rather than in advance.
> 2. **The last clause is satisfied for the fine wheel and was false for the design as a whole.** `WHEEL_SIZE` is one Day, and `adr/0011` schedules Life Stages in **Days** — so the longest routine sleep exceeded the period by orders of magnitude from the moment that ADR was written. The repair is a **second level** whose bucket is one Day; a long sleep is never carried by a wrap.
> 3. ~~**The paragraph below states a number nobody has measured.**~~ **S0b has since measured it and it is false by about 30×.** At 1,000,000 Citizens under `rulesets/minimal.toml`, **11,191 Rule Instances come due a Tick against 359,943 live — 3.1%**, where *"a few hundred"* is roughly 0.1%. **The Wheel is not at fault and the mechanism is not in question**: the variable is the Ruleset's rates, which run 8–32 Ticks in the only Ruleset that exists, where this sentence was imagining a Citizen asleep for a third of a Day. The figure attributes exactly — `consume` at rate 32 gives 3,750, each of its draws wakes a `restock` that fills and then fails on headroom for about 7,500 more, and `upkeep` sleeps for ever at 0, predicting 11,250 against 11,191 measured. **The peak is 29,620 and it is a world-creation transient**, not a city state: `populate` arms every Rule Instance inside one `rate`, so the stagger cannot spread the first firing, and it decays to 11,295 by Tick 64. See [`spike-results`](spike-results.md) → S0b, finding 5. The sentence below is kept and struck rather than rewritten, because *what number belongs here* depends on a designed Ruleset that does not exist — that is task 10b's.

~~This converts cost from *number of entities* to *number of entities with something happening right now* — typically a few hundred out of hundreds of thousands.~~ This converts cost from *number of entities* to *number of entities with something happening right now* — **measured at 3.1% of live scheduled rows, not the *"few hundred"* this originally claimed.**

**The discipline that makes it work: entities do not poll, mutators wake observers** — for the family that waits on a named thing, which is not every family. [`adr/0033`](adr/0033-two-rule-families-scheduled-and-swept.md) has since created a second one: a **Sweep Rule** fires on a time trigger, walks a population and never waits, and it was admitted precisely *because* subscription is wrong for a population — an entity cannot know whether it matches a predicate without being evaluated, so the wheel buys nothing and costs a wheel entry apiece. The sorting test is `adr/0033`'s: **subscribe when waiting on a specific named thing; poll when sweeping a population.** What follows in this paragraph governs the first case. When a District Pool's contents change, it wakes the Buildings registered as interested in that resource. When a road is edited, it wakes what depended on it. Every mutation site must know its observers. That is more code than polling, and it is the difference between a city that scales and one that doesn't. Factorio measured a 40× improvement on roboports from exactly this.

**Consequence for rule design:** a Rule that fails should register a wake-up on the condition that would let it succeed, not retry on a timer. `bake_bread` failing on flour registers interest in **its own local flour Bin**, which is the one that stopped it. Not the Pool: under [`adr/0045`](adr/0045-a-fallback-chain-is-a-source-ladder-over-one-bin.md) every link of a chain relieves the Bin the *head* failed on, so the Pool rung is a link that **refills local flour**, and its deposit is what wakes the head. One subscription therefore covers every rescue path, and chain depth costs no subscriptions at all.

---

## 8. Determinism rules for this layer

Binding constraints. Violations are bugs regardless of whether they are currently observable.

1. **No float arithmetic anywhere in the core — not merely no floats in state.** Integers, or Q16.16 where sub-Tile precision is needed. This rule previously read *"no floats in simulation state"*, which permitted `int r = (int)(a * 1.5f)` — a float temporary whose result is stored as an integer, and which is **exactly as non-deterministic** as a stored one: x87 80-bit intermediates, FMA contraction, differing SIMD widths. `05 §4` had the same wording and is corrected with it.
2. **No hash map iteration.** Sorted arrays or dense arrays indexed by generational handle. `Dictionary<string,T>` iteration order differs between runs of the same binary because .NET randomises string hashing per process. (Subsumed in practice by [`adr/0036`](adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md)'s ban on reference types in simulation state, but stated independently because the reason is different.)
3. **No `System.Random`.** Randomness is `draw(world_seed, entity_id, tick, purpose_tag)` — counter-based, so results are independent of evaluation order and Phase 2 can be parallelised with no coordination and bit-identical output.

    **The function is normative and is written out in [`adr/0003`](adr/0003-deterministic-integer-simulation.md)** — SplitMix64's finalizer, with literal constants. It had been cited in four documents and defined in none, which is not a documentation gap: **the RNG is a format**, because an Input Log reproduces a run only if the hash is bit-identical. Changing it is a save-format-class change under `05 §7`, never a free optimisation.

    **`purpose_tag` is a compile-time integer constant from one central enum, never a string** — a string would need string hashing, which rule 2 bans, and a mistyped one collides silently. Uniqueness over that enum is a **build-time check**.

3b. **Every field is declared once as either `(saved AND hashed)` or `(derived AND rebuilt)`**, and the save serialiser and the State Hash are generated from that single declaration. Without it, **a field that is saved but not hashed is invisible to every tool here**: two runs diverge on it, the hashes agree, replay reports success, and the save/reload test passes because the field *is* saved. The oracle certifies a divergence it cannot see. Composition order falls out — tables in declaration order, arrays in index order. See `adr/0003`.

3c. **Division goes through a stated rounding helper, and non-constant shifts through a range-checked one.** C# truncates division toward zero, so `-7/2 == -3` while `7/2 == 3` — deterministic but **asymmetric**, producing a directional bias at every zero crossing. Shift counts are silently masked, so `x << 32` is `x << 0`.

3d. **Arithmetic follows `adr/0003`'s overflow policy:** wide integers by default, Q16.16 for positions only, fixed×fixed on **dimensionless ratios**, `checked` inside the fixed-point library, and quantities carried as typed value types so `Tiles × Tiles` does not compile. Overflow is the one defect class the State Hash cannot see — both runs wrap identically — so it is prevented structurally rather than detected.
4. **Work partitioning is by Chunk, never by thread count.** A partition that depends on core count produces machine-dependent results.
5. **Intent ordering in Phase 3 is a counter-based random shuffle**, never arrival order and never entity id: `hash(world_seed, tick, "settle_order", entity_id)`. Deterministic by rule 3, identical on every machine, and reshuffled every Tick.

    **Entity id was the original answer and it was wrong twice over.** It is *biased* — the same Building wins every contested draw for the life of the city, and no player can see why. And it is not even stable: `05 §3` reuses row indices when a row is freed, so a sort on a raw handle index means **demolishing an unrelated Building can change who wins a flour race downtown**. Hashing with the Tick removes both problems at no cost, since the RNG is mandated anyway. Where a raw id is still needed as a tiebreak of last resort inside the hash, it must be a **monotonic never-reused id**, not the handle index.

    A random tiebreak is also the more honest one. *"Two bakeries reached for the same six flour and one got it"* is a complete explanation; *"it has a lower table index"* explains nothing and cannot be acted on. Note this is a **tiebreak, not a priority** — fairness under sustained shortage belongs to §4.1's requirement-aware wait list, and a second fairness system layered here would need a rate normalisation constant that could not be stated in domain units.
6. **Layer diffusion is double-buffered.** In-place is order-dependent.
7. **Nothing reads the camera.** Earlier drafts made a fidelity focus point a recorded input so that camera-driven detail stayed replayable; `adr/0007` removed the input entirely by deriving fidelity from simulation Stress. The rule survives in its general form — nothing enters simulation state except through the Input Log — and it is still the one most likely to be violated by accident.

---

## 9. Evidence — what the simulation must be able to answer

Stated here rather than in the UI doc because it is a **constraint on the simulation**: if the sim cannot produce these answers, the sim is wrong. `LEGIBLE CAUSE`

**The general rule: every aggregate figure must be able to name its constituents.** A departure count knows which Households departed. A congested segment knows which Trips are using it. A shortage knows which Buildings are starved. If a number cannot be expanded into the entities that produced it, we are computing it in a way that discards the thing the player needs.

This is cheap if designed in and expensive if retrofitted — it means accumulators keep entity references (or a bounded sample of them) rather than only totals. A bounded sample is usually sufficient and is preferred: five example Households out of 380 is what the UI shows anyway, and it keeps the accumulator fixed-size, which `adr/0006` requires.

Specific requirements:

For a **Building**: its occupants, its Bins with current levels, which Rule it last ran and whether it succeeded, if it failed then which fallback chain it walked and where it terminated, its accumulated failure pressure and the specific conditions contributing to it.

For a **Citizen**: home, workplace, current activity, current or last Trip with its Fate, need satisfaction, and household finances.

For a **Lot**: why it is vacant. Not "vacant" — *why*. No frontage, no household in the queue that would accept it, conditions below tolerance, or no capital.

That last one is the hardest and the most valuable. "Why is nothing building here?" is the question every city-builder player asks and no city builder answers.

---

## 10. Testing strategy

The simulation is a pure function, which makes it unusually testable for a game. This is the main reason for the architecture.

**Invariants sort by frequency, never by build configuration.** This section previously gated its assertions on *debug builds*, and that is backwards: the runs that surface these bugs are the **headless balance runs**, millions of Ticks long, and they are release builds. The gate would have been closed exactly where the exposure is. It is also unaffordable as written — *"Goods conserved, no Citizen in two places"* is `O(n)` per Tick, which was defensible at 10k and is not at 1M. [`adr/0033`](adr/0033-two-rule-families-scheduled-and-swept.md) already found the right shape for this and it was not applied here: *"unaffordable per Tick and trivial at the end of a headless run."*

| Tier | When | What |
|---|---|---|
| **Per Tick** | every build | Only `O(1)` and `O(changed)` checks — no Bin negative or over capacity at the write site, parking occupancy conserved, no Trip without a Fate |
| **Staggered** | every build, one slice per Tick | The `O(n)` sweeps, amortised across the population the same way Sweep Rules are: Goods conserved, no Citizen in two places, every Household's home exists and lists them as occupant |
| **End of run** | headless suite | The expensive whole-world walks: **money conserved** (the overflow detector `adr/0003` relies on), every cross-table handle valid, *no Rule asleep with all inputs satisfiable* (`adr/0033`) — **built at last as `WaiterIsBlockedByTheBinItNames`, and narrower than that wording on purpose: it asks whether the Bin a waiter *named* still blocks it, which also catches a waiter resubscribed to the wrong Bin** ([`adr/0063`](adr/0063-a-wait-list-wakes-on-the-bins-state-and-a-shortfall-is-derived-rather-than-stored.md)) |

The rest:

- **Headless fast-forward in CI:** run 100,000 ticks in seconds and assert the city is still coherent.
- **No collection grows with elapsed time.** Over a long headless run, assert that every collection's size is bounded — not merely finite, but not trending upward once the city reaches steady state. This is the only reliable way to catch this class of bug: it is invisible at design time, takes hours of play to manifest, and we have already written it twice. See `adr/0006`. **And the same assertion for magnitudes**, per `adr/0003`: no quantity accumulates without bound.
- **State-hash divergence:** two runs of the same input log must produce identical hash sequences. First divergence identifies the exact tick a bug entered. **Its coverage is guaranteed structurally rather than tested** — §8 rule 3b generates the hash and the save format from one field declaration, so the save/reload test below transitively proves the hash is complete.
- **Save/reload equivalence:** run N ticks, save, reload, run M more; separately run N+M; compare hashes. This catches *unsaved state*, which is otherwise nearly impossible to find. Factorio uses this as their primary correctness harness.
- **`purpose_tag` uniqueness, at build time.** Reusing one across two decisions correlates them **invisibly** (`05 §4`), so nothing at runtime can catch it. A build-time check over the central enum is the only detector, and it costs nothing.
- **Thread-count equivalence:** `run(log, threads=1).hash() == run(log, threads=8).hash()`. If we cannot run single-threaded on demand, we cannot debug determinism. *Cannot run until Phase 2 — Phase 1 is single-threaded — so it is written when the first parallel phase lands, not before.*
- **A stored Input Log with a recorded hash sequence, deliberately re-baselined.** A golden-hash test would fail on every legitimate change, so the point is not that the hash never moves — it is that it never moves **without someone saying so**. Same posture as `adr/0003`'s note on swapping the choice algorithm: hash-breaking changes are safe, deliberate, and re-baselined, never silent.

---

## 11. Open questions

Genuine forks. Each needs resolving before the system it governs is built.

1. ~~**How many Ticks make a Day.**~~ ~~Whether the city ages.~~ **Settled.** One clock and no calendar (`adr/0010`); Citizens never age and Households advance through Life Stages instead (`adr/0011`); and `TICKS_PER_DAY = 8192` at a reference rate of 16 Ticks/s, fixed at world creation, with pacing delivered entirely by the speed ladder — see §1.2 and [`adr/0019`](adr/0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md). The figure that turned out to matter is not the Day's length in real seconds, which is free, but the **ratio** of commute Ticks to Day Ticks, which is the traffic balance.
2. ~~**What the map is.**~~ **Settled.** One bounded procedural rectangle, sparse Chunks, all of it live — [`adr/0020`](adr/0020-one-live-world-and-settlements-are-derived.md), [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md), [`adr/0022`](adr/0022-land-is-a-stock-the-city-spends.md). The region-of-tiles option died on `adr/0010`, not on effort: frozen neighbours are a second clock. What replaced it — Settlements derived from commute range — turned out stronger than what it replaced. **Still open: the map's actual size, and the Outside Connection layout.** See `plans/0002-open-questions.md`.
3. **Districts: player-drawn or automatic?** Player-drawn gives the player a real lever over the logistics abstraction and is more legible; automatic is less to explain and less to get wrong. Leaning player-drawn with an automatic default.
4. **Where private capital comes from.** A simple regenerating pool is legible but arbitrary. Deriving it from business profits and household savings is causally honest but adds a feedback loop that could deadlock a struggling city. Probably: derived, with a floor.
5. **Does the player place service buildings directly, or zone for them?** Pillar 3 says govern-don't-place, but a fire station appearing wherever the sim likes is probably bad play. Likely exception: the player places civic buildings, the sim places everything else. Worth naming as a deliberate inconsistency rather than discovering it later.
6. **TOML now, DSL later — or DSL now?** TOML is zero parser work and consistent with `adr/0018`. But the Ruleset is the file we will read and write most, and GlassBox's DSL was significantly more readable. Deferring, with the trigger being "the TOML has become painful enough to measure."
