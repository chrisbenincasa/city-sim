# Height does not ship until terraforming does, because terrain without a price is a wall

**Milestone 24 generates height and stores none of it.** The generator uses height while it works — to
decide where water sits, which ground floods, and what terrain type a Cell is — and stores only its
**outputs**: the terrain type column
([`0154`](0154-base-fertility-is-ruleset-data-keyed-by-terrain-type-and-the-old-name-invented-a-field.md)),
the water graph with its downstream edges, and floodplain depth **where the floodplain is**. There is no
height column, at Tile or at Cell resolution.

🔴 **The reason is not cost. It is that height's only live consumer is a refusal**, and
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) refuses that
configuration by name: *"Without terraforming, terrain is a **wall**. With it, terrain is a **price**."*
**Terraforming is not a verb in `01 §2` and is not placed in `06`**, so height ships with terraforming
whenever that is argued, and not before.

Guiding concepts: `PLAYER GOVERNS`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Whether an absent mechanism should be built is a sequencing and design question; the memory figures below
corroborate and do not decide.

## Why

### Height's impact was traced consumer by consumer, and almost all of it is excluded by decision

[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s table gives height four
jobs. Checked against the build and against this milestone's split:

| Job `0021` gives height | Status |
|---|---|
| Affect vehicle speed, routing cost, junction geometry | **Excluded by that ADR by name.** The whole point of the table's right-hand column |
| **Feed land value and desirability** | **No height term exists.** `02 §2.4` composes `− w₂·pollution − w₃·noise + w₄·amenity − w₅·shoreline`; shoreline is *water*, not height |
| **Set construction cost via earthwork volume** | **Construction cost does not exist.** No Ruleset authors a cost of anything, and [`0035`](0035-infrastructure-is-priced-by-what-it-consumes.md) denominates it in **Lane-Tiles**, which is milestone **21** |
| **Force bridges and water crossings** | `0021` itself: *"a buildability exception plus a rendering variant, **not a system**"*, and *"the Road Graph does not know the difference"*. Rendering is Phase 3 |
| **Decide what can be built (maximum buildable grade)** | 🔴 **The one live consumer, and it is a refusal** |

***So height's entire net effect on a milestone-24 city is that some Lots decline to build.*** That is the
whole impact, and it is worth stating plainly because
[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)'s table reads as though
height were doing four things.

### And that one effect is the design the ADR rejected

> *"Without terraforming, terrain is a **wall**. With it, terrain is a **price**. The second is
> straightforwardly more `PLAYER GOVERNS` — the player negotiates with the landscape by spending rather
> than being refused by the generator."*

**Shipping height without terraforming ships the first sentence.** It is the same principle
[`0022`](0022-land-is-a-stock-the-city-spends.md) states for resources — *"Scarcity is a gradient, never
a wall … There is never a moment where the game says 'no timber'"* — and the same shape
[`0009`](0009-parking-is-modelled-supply-never-search.md) settled for parking, where the shed widens and
failure arrives only on the Commute Budget.

***A mechanism whose only built half is the refusal is not a partial delivery of the design; it is the
alternative the design refused.***

### Terraforming is asserted as a verb by one document and absent from the one that owns verbs

[`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md) states *"Terraforming is
a player verb, priced by cut-and-fill haul distance."* **`01 §2` has six verbs** — Zone, Connect,
Service, Govern, Demolish, Inspect — *"and the list is short on purpose"*. **Terraforming is not among
them, and `01` does not mention it anywhere at all.**

⚠ **The corpus has counted this list once before and got a correction out of it.**
[`0022`](0022-land-is-a-stock-the-city-spends.md)'s 2026-08-12 amendment reasoned about whether clearing
forest needed a verb, and recorded that
[`0091`](0091-clearing-land-is-bought-rather-than-taken-and-demolish-is-the-sixth-verb.md) had made
Demolish the sixth. **Terraforming would be a seventh and nobody has argued for it** — which is a
question rather than a defect, and it is filed in `plans/0002` rather than answered here.

### `06`'s inventory row says both that it is placed and that it is not

`06`:330 reads, in one cell: *"✅ **Placed: 24.** a milestone. … **Terraforming still owes a
milestone**"*. The row covers three things and the version numbers are struck as paid, so *Placed: 24*
was presumably about the **generation guarantees** beside it — but the cell opens with the tick, and a
reader scanning the inventory takes terraforming as placed.

***A row that carries three mechanisms carries one status***, which is
[`plans/0012`](../../plans/0012-corpus-audit.md)'s granularity defect in an inventory table. Filed there,
and the row is split.

### The memory figures corroborate and are deliberately not the reason

`CellGrid.WorldTiles` is 16,384 a side, so the map is **268,435,456 Tiles**. A per-Tile height at two
bytes is about **512 MiB**, against **86 MiB** for every table in a 1M-Citizen world — roughly **six
times the entire rest of the simulation**. ⚠ **`0021`'s sparse-Chunk promise does not rescue it**: sparsity
works because *development* is sparse, and terrain exists everywhere. At **1M Citizens the generator
paves 8.6% of the map**, so over nine tenths of a stored height field would describe ground nobody ever
builds on.

**Cell resolution is affordable and useless**: 262,144 values, about 512 KiB — but a Cell is 128 m, and a
maximum buildable *grade* on a Lot or an earthwork *volume* for a Segment are Tile-scale quantities. ***The
cheap resolution cannot serve the consumers and the useful one costs more than the city.***

🔴 **This is recorded as corroboration and must not be quoted as the decision.**
[`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)'s discipline
generalises: **deferring for a cost you have not been asked to pay is deferring for the wrong reason.**
Had terraforming been placed, this ADR would have had to find the memory, and the shape that finds it is
already known — `0021`'s *seed + edits*, where only edited Chunks store heights.

### The generator still uses height; it just does not keep it

Nothing here says the world is flat. **`0021`'s *"the world is not flat"* is untouched** — the generator
computes height and reads it while laying water, floodplain and terrain type. What changes is that the
field is **not a column**, so no Tick can read it and no save carries it.

⚠ **This keeps [`0111`](0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md)
intact and is why decision 6's recommendation survives.** A height regenerated *on load* would make the
generator's version load-bearing and bring back the field that ADR struck. **Height computed once,
consumed once, and discarded regenerates nothing.**

## Consequences

- **Milestone 24 stores no height.** `plans/0042` task 2 builds the terrain type column; **task 6** (Water
  Bodies) takes the **downstream ordering as generator output**, per `CONTEXT.md` → Water Body's *"an
  outflow rate to the next body downstream"*, which is an edge rather than a computation; **task 9**
  (Hazard Regions) stores **floodplain depth sparsely**, where the floodplain is.
- **Maximum buildable grade does not ship**, and no Lot is refused for terrain at milestone 24. **This is
  a stated absence and not an oversight** — it is `0070`'s *unbuilt*, and the answer to *given
  terraforming does not exist, should buildability compensate?* is **build terraforming**.
- **Terraforming owes a verb argument before it owes a milestone.** Filed as an open question; `01 §2`'s
  count is six and its shortness is stated as deliberate, so a seventh is a change to that section rather
  than an addition to a table.
- **`06`'s inventory row 330 is split**, so terraforming's status stops travelling inside a cell that
  opens with a tick.
- ⚠ **This ADR does not defer *terrain*.** Terrain type, water, Woodland, Hazard Regions and Fertility
  all ship at 24. What is deferred is the **height field**, which is one output of the generator among
  several and the only one with no consumer that is not a refusal.

## What would trigger revisiting

- **Terraforming is argued and placed.** Then height ships with it, at Tile resolution, under `0021`'s
  *seed + edits* — and the generator version returns with it, which
  [`0111`](0111-a-save-that-re-derives-nothing-needs-neither-a-seed-nor-a-generator-version.md) already
  names as its own trigger.
- **A consumer of height appears that is a price rather than a refusal** — height feeding desirability
  as a view term, or a Segment's cost scaling with grade. Either would make height earn storage without
  terraforming, and neither exists.
- **Floodplain depth turns out not to be sparse.** If a shipped world's floodplain covers enough of the
  map, the sparse store stops being cheaper than a dense one and the storage question reopens on cost
  rather than on principle.
- **The generator cannot produce a coherent water graph without a persisted height.** That would be an
  implementation finding rather than a design one, and it argues for keeping height as generator scratch
  rather than for storing it.
