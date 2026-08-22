# Land is a stock the city spends, and maturity means dependence

**All land begins fertile. Development degrades it.** Fertility is not placed by the generator and never read from a table — it is **composed at the point of use** from what the player has done to the ground. **Woodland is the one generated resource**, it regrows slowly, and clearing it is what creates farmland.

Together these produce the game's intended macro-arc, which is chosen here rather than allowed to accumulate: **a city that starts self-sufficient and matures into a net importer of both Food and Materials.** `EMERGENCE`

## Why fertility is not on the map

The obvious design puts fertility in the generator — fertile valleys here, poor ground there. It is rejected because it makes the interesting fact a **lookup**. A player reading the fertility overlay is following instructions, and the map has a correct answer to where farms go before the player has done anything.

Inverting it puts the fact in the **consequence of play**:

```
fertility(chunk) = terrain suitability − sealing − pollution
```

- **Sealing** is the record of development: the count of Tiles in a Chunk ever built on. Its decay rate comes from the Ruleset **keyed by terrain type** — rock and clay may never recover, alluvial floodplain may recover over hundreds of Days.
- **Pollution** is the existing Map Layer, already diffusing, already decaying.

`CONTEXT.md` requires Map Layers to be *composed at the point of use rather than baked into derived layers*, and that rule does all the work here: **fertility is not a stored layer.** A Rule may already read Map Layer cells under a footprint, so a farm's yield reading composed fertility is an ordinary Rule and not new mechanism. The residential side is free too — farms emit into pollution, and desirability already composes from pollution, so *farms are bad neighbours* falls out without a rule saying so.

The mutual repulsion between agriculture and housing then **emerges** rather than being scripted, which is zoning's actual historical purpose arriving as a consequence instead of a feature.

## The loop this closes

Development degrades nearby fertility, so farms retreat outward as the city grows. Retreat far enough and farm workers fall out of the city's commute shed — at which point, under [`0020`](0020-one-live-world-and-settlements-are-derived.md), **the farm village becomes its own Settlement.**

The fertility model and the Settlement model turn out to be the same story told from two ends, and neither was designed to produce the other. Urban fringe consuming farmland is among the most-documented dynamics in real urban geography. `SOLVE THE ACTUAL PROBLEM`

## Woodland: generated, finite, and slow

Fertility being universal removes the map's intrinsic character, and woodland gives it back without a lottery:

- The generator places forest. Forest Tiles are **not farmable while wooded**.
- Clearing yields **Timber** as a one-time harvest and leaves fertile ground behind.
- Forest **regrows** on unsealed, unoccupied land — slowly.

So a heavily forested seed is a Materials-rich, farmland-poor start, and the first ten minutes gain a real decision: clear for lumber now, or keep the forest and import.

Three consequences arrived unplanned and are worth recording as intended:

**The extraction frontier moves itself.** A forestry Building whose local woodland is exhausted stops meeting its conditions, declines, and is abandoned — the ordinary decline path. Zone growth then finds a viable Lot further out. No "move the logging camp" system is written. It needs the same hysteresis as Segment Stress or it will flicker.

**Land-banking accrues a harvest.** Vacant land the player is holding reforests, so leaving a parcel alone is no longer a null action, and abandoned farmland returns to forest. Land use becomes cyclical rather than monotonic.

**Materials imports are a growth brake nobody designed.** Materials gate construction. So as the city outgrows its forests, building things gets steadily more expensive *as a direct function of how much has already been built* — a soft, emergent, causally honest deceleration where SimCity used arbitrary caps. This exists only because `04-economy-and-goods.md` put Materials on the construction chain rather than making them a Need.

## Scarcity is a gradient, never a wall

The same shape as [`0009`](0009-parking-is-modelled-supply-never-search.md), which decided it for parking: the shed widens, the walk lengthens, and failure arrives only on the Commute Budget. Timber does this one scale up — the logging frontier moves outward, hauls lengthen, Shipment costs rise, and eventually the Outside Connection is simply cheaper than the haul.

**There is never a moment where the game says "no timber."** `HONEST DEGRADATION`

## The endgame, chosen deliberately

Permanent-ish sealing and slow regrowth jointly determine what the last hours of a game feel like. Stating it so nobody later "fixes" the ratchet by speeding up regrowth and silently deletes the game's second half:

> **A mature city is a net importer of Food and Materials, and its growth costs rise with its size.**

The two chains stay asymmetric even though both end in imports, which is what keeps this from being one pressure wearing two hats:

| | Food | Materials |
|---|---|---|
| Import is a | **operating** cost | **capital** cost |
| Scales with | population | ambition |
| Failure mode | fast, hard — crisis | growth stalls, nobody unhappy |

**The endgame must offer ways to restart the core loop, and replanting is the first.** Designate land for reforestation and pay for it; regrowth there is much faster. It is a Rule and a land designation rather than a system, and it converts the endgame from a fate into a policy choice — the player who plans ahead stays partly self-sufficient, the one who does not buys Timber forever. **Without a lever of this kind the late game is nothing but bills**, and finding more such levers is open work recorded in `plans/0002-open-questions.md`.

## Three things this needs, or it fails quietly

- **A counter-force, or farms flee to the map edge.** Maximal repulsion would push agriculture as far from the city as possible — and the far edge is the Outside Connection, so the player would simply import. The counter-force exists: Produce must reach food processing as a Shipment under [`0013`](0013-goods-are-pooled-within-a-district-and-shipped-between.md), and distance costs congestion and money. **If Shipment cost is ever tuned toward zero, this design silently collapses.**
- **Evidence, or the causal chain is invisible.** The player builds a suburb; two Settlements away, yields drop. Nothing connects those in time or space, so a farm's panel must decompose its own yield — *"41% — ground sealed 12%, pollution from Eastfield Industrial 47%"* — naming specific sources. This is the difference between a deep mechanic and an inexplicable one. `LEGIBLE CAUSE`
- **A damper, or it is a death spiral.** City grows → fertility falls → Food shortage → Departures → city shrinks. The Outside Connection is the damper by construction, converting collapse into expense. Named here because the spiral is real enough to be worth watching for in balance testing.

## Consequences

- **Sealing is per Chunk**, an integer count of Tiles ever developed, so one house seals 1/1024 of its Chunk — naturally proportional, no special-casing, consistent with every other Map Layer.

  > **Superseded in wording by [`0034`](0034-fields-are-sorted-by-source-geometry.md), not in arithmetic.** Sealing is per **Cell**, the design-constant grid split out of the Chunk when it emerged that Chunk size was setting pollution resolution — and therefore Fertility — while `05 §4` listed it as free to tune against a profile. The Cell inherits 32×32, so *1/1024* is unchanged. What changed is that the denominator is now owned by the mechanic instead of by a profiler.
- **No floats.** Per [`0003`](0003-deterministic-integer-simulation.md), sealing and every derived quantity here are integer or fixed-point.
- **Decay rates are Ruleset data keyed by terrain type, never stored per Tile.** Storing a rate as state would freeze it into every save and make retuning a migration over every Tile — the exact failure [`0015`](0015-all-tuning-data-is-hot-reloadable.md) exists to prevent. The Ruleset version gives strictly more variance for less storage.
- **`04-economy-and-goods.md` §1 needed a correction**: Timber was listed as produced by "Forestry, quarries," and a quarry producing Timber is incoherent. Quarries are dropped. Adding Stone as a sixth Good is the option that document's own resource discipline forbids.
- **Sustainable yield is a first-class readout** — *"consumption 40 Timber/Day, regrowth 25/Day — you are drawing down the stock"* — which turns an invisible slow decline into something a player can see coming.

## Amended 2026-08-12: how the player clears forest, and the distance the arc needs

Two things this ADR asserted have now been settled where they belong, and both are recorded here because a
future reader would otherwise re-open them from this file.

**Clearing forest is not an act, and the verb list grew for an unrelated reason that does not reach it.** [`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) has since made `Demolish` `01 §2`'s sixth verb — it removes **Streets and Buildings**, never ground, so no verb clears Woodland and this paragraph stands as written. *(It originally read "there is no sixth verb", which was true of the verb list on the day and is no longer the reason.)* This ADR says the first ten minutes gain *"a real
decision: clear for lumber now, or keep the forest and import"*, and `01 §3` had no such step, which read
as though a clearing tool were owed. It is not. `CONTEXT.md` → Zone now states the general rule — **the
ground carries resources, and developing over one without extracting it forfeits it** — so the player zones
anything anywhere, Woodland is ordinary ground with something standing on it, and Timber is captured by
zoning **Industry — Extraction** or lost by building over it. Sequencing the two is intended play and is
priced in Days rather than in permission, because the Unplaced Pool does not wait. The rule is stated over
*resources* so a later ground-pinned one inherits it. `01 §3` carries the step.

**The macro-arc was inert on the old map, and nobody noticed because the arc is a *distance* claim.**
*Farms retreat outward until farm workers fall out of the commute shed, at which point the farm village
becomes its own Settlement* cannot happen at 16.4 km across: the far corner is under thirty minutes from
town, so no retreat is ever far enough.
[`0089`](0089-the-map-is-sized-by-how-many-commutes-fit-across-it.md) takes the map to 65.5 km and the arc
becomes reachable for the first time. That ADR's consequence list named four distance-dependent claims it
re-opened and missed this one, which is the largest of them — the others are mechanisms, this is the whole
late game. ***A claim stated in retreat and reachability is a claim about distance however it is worded***,
and grepping for the word would not have found it.

## Amended 2026-08-22: the first term is renamed, and this ADR's refusal now has a key that can defeat it

**`terrain suitability` is renamed **Base Fertility**** and is **Ruleset data keyed by terrain type**
([`0140`](0140-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)).
The formula reads `base fertility − Sealing − pollution`. **Nothing in this ADR's argument changes** — the
rename was made *because* of this ADR, whose *Why fertility is not on the map* section is the strongest
statement in the corpus that the first term must not be a generated field, and whose own consequence list
already required Ruleset-keyed-by-type disposition for the Sealing decay rate beside it.

⚠ **What is new, and what a later reader must not do by accident.** Because Base Fertility is keyed by
terrain type, **varying it costs one Ruleset key and no storage** — and a Ruleset that varies it
reintroduces exactly the generated fertility gradient this document refuses, where *"the map has a
correct answer to where farms go before the player has done anything."*

**The shipped demonstration Rulesets therefore state a uniform Base Fertility**, so a default world shows
this ADR's stance. ***Varying it amends this document rather than tuning within it***, and comes back
here and to `0140` rather than to a Ruleset review. The key exists so the question can be looked at, not
so it can be answered by tuning.

**What the ground decides in the shipped world is unchanged and is worth stating positively**: not where
you may farm, but **whether your damage is reversible** — this document's own *rock and clay may never
recover, alluvial floodplain may recover over hundreds of Days*. That is the map's character arriving
through recovery rather than through a lottery, which is what *Woodland gives it back without a lottery*
was reaching for.

## What would trigger revisiting

- **Playtesting showing the ratchet is unfun rather than dramatic** — specifically, players describing the late game as bookkeeping. The first response is more reboot levers, not faster regrowth; regrowth speed is the load-bearing constant and loosening it deletes the arc.
- **Fertility degradation proving illegible** despite Evidence — players surprised by yield collapse they could not have anticipated. That is a UI failure first and a model failure second, and should be diagnosed in that order.
- **Resource depletion wanted as an active shock layer** rather than a slow background, per `01-player-experience.md` §5. That is additive to this decision, not in tension with it.
