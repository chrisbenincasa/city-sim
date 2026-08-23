# Sealing authors no width, and a road seals where it is laid, not where its endpoints are

**A built Tile seals one Tile, and so does a Tile of road.** A Building seals **1**, which is
`CONTEXT.md` → Sealing's own *"one house seals 1/1024 of its Cell"* rather than a new figure; a road
seals **one Tile per Tile of its run**, a width of 1 Tile, which is 4 m (`Tiles.Metres`). 🔴 **There is
no `[roads]` width key and no per-kind width**, so the whole decision opens **zero**
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) rows.

🔴 **And Sealing is written where road is laid, from the geometry the writer holds at that moment — never
reconstructed afterwards from a Segment's stored endpoints.** `RoadGenerator.Layout.WalkArterial` already
walks an Arterial one Tile at a time; `LayStreets`, `LayFootPaths` and `ConnectToGrid` each lay a straight
run between two Tile positions they compute. **Every case is exact, none needs a stored path, and there is
no fallback branch.**

Guiding concepts: `SOLVE THE ACTUAL PROBLEM`, `LEGIBLE CAUSE`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
What seals, and at what width, is a modelling choice about the unit; the decay rate that follows it is
measurable and belongs to [`0040`](../../plans/0040-terrain-and-the-land-rows.md)'s decision 5, filed
rather than settled here.

> ## ⚠ CORRECTED 2026-08-22, by measuring it
>
> **Two claims below were argued from prose and the measurement refuses both.**
> `tests/Borough.Tests/Space/SealingMeasurementTests.cs` is the instrument.
>
> | Claim as written | What a 4,000-Citizen city actually does |
> |---|---|
> | *"about 7%"* mean, of which *"roads are roughly 86%"* | **6.3% mean**, roads **93%** — and **99%** on `severance.toml` |
> | `adr/0022`'s *"ground sealed 12%"* is a mockup, not this quantity, so **leave it alone** | 🔴 **A peak Cell is 11.4%.** The specimen is right, within rounding, and the reasoning that dismissed it was wrong |
>
> 🔴 **And the decision's central stance does not survive at all.** *"Pavement is what seals ground"* is
> not a property of the city; it is a property of `footprint_tiles = 1`. Every Building seals exactly its
> footprint, so the sensitivity is arithmetic rather than another run: at a realistic **47 Tiles**
> (~750 m²) the 481 Buildings seal **22,607** against the roads' **7,310** — Buildings go from **6% to
> 76%** of Sealing and the mean Cell from 6% to roughly **24%**.
>
> ⚠ **The value 1 was taken from `CONTEXT.md` → Sealing's *"one house seals 1/1024 of its Cell"* while
> `CONTEXT.md` → Building — twenty-five lines away — says a Building *"has a footprint (the set of Tiles
> it covers)"* and *"interacts with Map Layers through that footprint"*. `RuleEngine.cs:936` says the same
> thing in an error message: *"Sealing is a property of a footprint."* **The entry that specified the
> mechanism was never opened.** ***Two sentences of the same standing were held to different standards:
> one was dismissed as illustrative and the other promoted to specification, and the difference was which
> one suited the cheaper answer.***
>
> ✅ **What this ADR still gets right is the attribution rule and the refusal to author a width.** Both
> stand. What it gets wrong is treating a number it inherited as derived.
> **`[[building]] footprint_tiles` now exists, defaults to 1, and is the dial** — so the question is
> a Ruleset edit and a reading rather than an argument.

## Why

### A width key would be a lever aimed at a target nobody has authored

The corpus holds exactly one sealed-ground figure: [`0022`](0022-land-is-a-stock-the-city-spends.md)'s
specimen panel string *"41% — ground sealed 12%, pollution from Eastfield Industrial 47%"*. ⚠ **That is a
mockup of a farm's yield panel, not a ratified quantity** — and a farm Cell is by construction not built
out, so it is not even the same measurement as a built-out Cell's sealed fraction. ***Authoring a
coefficient to hit an illustrative string is choosing a number against a target that was never claimed to
be one.***

The alternative costs nothing. `CONTEXT.md` already denominates Sealing in Tiles and already fixes a
Building at one of them; extending the same unit to a road introduces no quantity at all. Under
[`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md) a width key would
owe a machine, **a world** and a quantity on the day it was written — and the world does not exist, which
is the same absence already blocking decision 5's ratifier. ***A number whose ratifier is blocked on a
world nobody has built is a number better not written.***

### The consequence is that Sealing is mostly a road statistic, and that is the stance

At `[roads] block_tiles = 32` a block is exactly one Cell, because a Cell is 32×32 Tiles. A built-out Cell
therefore carries about two Segments' worth of frontage — **~64 Tiles of road** — and `[lots]
lots_per_segment = 5` puts about **10 Buildings** on it at 1 Tile each. **~74 Tiles of 1024, about 7%**,
of which roads are roughly **86%**.

⚠ **A player who builds dense housing on few streets seals very little; a sprawl of streets seals a great
deal.** That is intended, and it is the direction `CONTEXT.md` → Sealing had already committed to when it
added *"Roads Seal, and so does every other built Tile"* and noted that Sealing *"had only ever been
discussed through Buildings"*. ***Pavement is what seals ground, and a model in which housing density is
the sealing term would be modelling the wrong cause.***

### Attribution needs a rule, and the obvious rule is wrong on a shipped world

`MapLayers.Seal(Cells east, Cells north, int tiles)` — `Space/MapLayers.cs:392` — writes to **one Cell**.
A Segment does not sit in one Cell. Every lattice Street runs intersection to intersection, which at
`block_tiles = 32` is Cell boundary to Cell boundary, so **a Street's two endpoints are never in the same
Cell**; and `rulesets/severance.toml` sets `block_tiles = 256`, where one Segment spans **8**.

Splitting a Segment's Tiles evenly between the Cells of its two endpoint Nodes is the rule that suggests
itself, because it needs no geometry. 🔴 **`severance.toml` refutes it**: 128 Tiles into each endpoint
Cell — **12.5% of a whole Cell from a single road** — and **nothing whatever into the six Cells the road
actually runs through**. ***A rule that seals ground the road never touches while leaving untouched the
ground it does is not an approximation of the right quantity; it is a different quantity.***

### The path is not stored, and it does not need to be, because the writer still has it

The even split is reached for because both of these are true: no Segment stores its covered Tiles, and
there is no Segment → Cell helper anywhere in `src/`. Neither matters. **Sealing is written at
construction, and every writer of a road knows that road's geometry at the moment it writes it:**

| Writer | The geometry it holds |
|---|---|
| `Layout.LayStreets` — `Space/RoadGenerator.cs:366` | axis-aligned, exactly `block_tiles` long, both endpoints computed from the lattice index |
| `Layout.LayFootPaths` — `:684` | an exact 45° diagonal, `Intersection(e, n)` to `Intersection(e+1, n+1)` |
| `Layout.ConnectToGrid` — `:606` | a straight run between two Tile positions it computes, its length the floored Euclidean `Distance` |
| `Layout.WalkArterial` — `:444` | **every Tile, one at a time** — it already calls `MarkSevered(tileEast, tileNorth, nextEast, nextNorth)` on each step of the walk |

⚠ **The Arterial trunk piece is the one Segment whose stored `LengthTiles` is arc length rather than the
distance between its endpoints**, which `Space/RoadSegmentTable.cs:26` states and explains: *"A freeform
Arterial curves between its Junction pieces, so its Segment is longer than the straight line joining
them."* It is therefore the single case a chord reconstruction would get wrong — and it is exactly the
case where the generator is already holding the true path in a loop it is already running.
***The one Segment whose geometry cannot be recovered from the world is the one Segment that never needs
recovering.***

### The rule this leaves behind, stated against a symbol

**Nothing may reconstruct a Segment's Cells from `RoadNodeTable.East`/`North`.** Sealing has no
after-the-fact reader; it is a write at the point of laying. If some later consumer genuinely must
attribute an **existing** Segment to Cells, the estimator is the straight chord between its endpoint
Nodes, apportioned by Tiles per Cell and scaled so the total equals `LengthTiles` — and it is an estimator
for the Arterial trunk and exact for everything else. **That paragraph is a contingency, not a
deliverable**, and writing the helper before a consumer exists is
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md)'s forbidden move.

## Consequences

- **`LayerCellTable.Sealing` becomes non-zero on every shipped Ruleset, so every State Hash moves.** Under
  [`0100`](0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md) that costs
  nothing and **must not be cited as a reason to defer, narrow or split the work.**
- **[`0040`](../../plans/0040-terrain-and-the-land-rows.md)'s precondition 2 third blocker clears**, and
  the two [`0124`](0124-terrain-suitability-is-baked-at-world-creation-and-the-layer-holes-that-need-it-move-to-milestone-24.md)
  did enumerate — `sealing_decay_tau = 0` everywhere, and `MapLayers.Step` never calling `DecaySealing` —
  become reachable for the first time.
- **[`0148`](0148-fertility-composes-with-weights-and-only-one-of-them-is-a-number-anybody-chooses.md)'s
  `base × Sealing / 1024` gets a non-zero numerator**, so its derived `w_s` starts doing something.
- ⚠ **Width 1 is uniform across every road kind**: an Arterial, a Street and a foot path each seal 4 m of
  width. That is coarse and it is named here rather than left to be discovered — the revisit trigger below
  is what a per-kind width would arrive through.
- ⚠ **Road with no graph edge still seals.** `WalkArterial` lays Tiles before its first Junction anchors a
  Segment — its own comment says so — and that is pavement. Sealing at the writer picks it up; a
  per-Segment rule would silently miss it. ***This is the second thing writing at the point of laying gets
  for free.***
- [`0022`](0022-land-is-a-stock-the-city-spends.md)'s 12% specimen is **left as it stands**. It is a panel
  mockup and it is not the quantity this decision produces, so amending it would be correcting a sentence
  that is not wrong.

## What would trigger revisiting

- **A per-kind width is wanted** — a six-lane Arterial sealing what a foot path seals becomes visible in
  play or in a Fertility panel. That is a `[roads]` key per `RoadKind`, an amendment, and one §D row per
  value with a named machine, world and quantity.
- **A consumer needs an existing Segment's Cells** — the chord estimator above stops being a contingency
  and becomes a helper, and the Arterial trunk's arc/chord gap becomes a real error somebody must price.
- **Roads are built outside the generator** — a player build-road verb, or milestone 21's construction
  cost, creates Segments at a site that must seal for itself. The rule survives; the list of writers does
  not.
- **Demolition, terraforming or the verge unseal.** `CONTEXT.md` → Sealing is *"ever built on"* and
  `CONTEXT.md`:181 gives the Arterial verge as ground that stays unsealed. If either becomes a *subtraction*
  rather than a decay, sealing at the point of laying gains a mirror at the point of removing.
- **`block_tiles` stops dividing the Cell evenly.** The 7% arithmetic and the Cell-boundary claim about
  Street endpoints both assume it does; nothing enforces it.
- **The 7% is played and found too weak or too strong.** That is the width key's occasion, and it needs the
  varied-terrain world decision 5 already owes before the reading means anything.
