# The map is sized by how many commutes fit across it

**`WorldCells = 512` — a 16384² Tile map, 65.5 km on a side, 4,295 km².** A Tile stays ~4 m and
`TilesPerCell` stays 32; the only constant that moves is the Cell count. The map is sized by **how many
Commute Budgets fit across it** — 3.7–5.2 at a 30-minute Budget — because that ratio, and not area, is
what decides whether the player can build separate towns or only one. The 1M target is unchanged and the
**buildable fraction falls to ~6%**, which is the point rather than a cost: unbuilt ground is what
separates towns, and [`0021`](0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md)
already makes it free.

⚠ **The constant must not move yet.** `RoadGenerator` paves the entire map at world creation, which is
the one structure in the build that scales with map **area** rather than with development — and it makes
`0021`'s central claim false. That defect is this decision's named blocker.

Supersedes the map-size half of
[`0085`](0085-nothing-on-this-map-is-far-away-so-a-settlement-is-made-by-a-gap.md), whose findings stand.

`PLAYER GOVERNS` `EMERGENCE` `SOLVE THE ACTUAL PROBLEM`

## Why

### A map has never been sized against a distance in this corpus

`plans/0002` ledger #1 chose 4096² against **area** and **density**: 268 km², 3,700/km² at 1M, *"Los
Angeles"*. Both numbers are right and neither is a distance. `05`'s budget block states the same
derivation — `map_area × mature_density × buildable_fraction` — and there is no term in it for how far
apart two places are.

The distance was supposed to be covered by the *corner-to-corner* column, and `0085` found that column
wrong by 52–73×: a Tick is 10.546875 s of in-world time
([`0082`](0082-the-behavioural-clock-is-global-and-car-following-sub-steps-inside-it.md)), so crossing
4096² takes **28 minutes**, not 141% of a Day. **4096 Tiles at 4 m is 16.4 km, which is shorter than
Manhattan is long.**

So the map was sized twice by area and never once by distance, and the one column that looked like a
distance was an artifact. ***A quantity stated in area cannot be checked against a claim about
travel*** — and every claim this map is load-bearing for is a claim about travel.

### What breaks at 16.4 km is not Settlements, it is everything that spends distance

`0085` found two consequences and stopped at two. The list is longer, and it is what makes this a map
decision rather than a Settlement decision:

| Mechanism | What it needs | At 16.4 km |
|---|---|---|
| **Settlement** (`0020`) | places out of commuting range of each other | none exist. S2 R1.5 measures **1 Settlement, 121 of 121 Districts**, at every Budget from 40 Ticks up |
| **The Commute Budget** (`CONTEXT.md`) | Trips that **exceed** it, so a Building can decline and die | no Trip on the map can exceed 30 minutes. **The decline mechanism is inert everywhere** |
| **Hinterland edge choice** (`0088`) | the far edge to be a commitment | 28 minutes. Edge choice is nearly costless |
| **Severance** (`03 §3.7`) | a barrier to be expensive to go round | you drive round it in minutes |
| ***"Geography matters"*** (`CONTEXT.md` → Commute Budget) | distance to have consequences | it has almost none |

The Commute Budget row is the one that should have been found first. It is not a Settlement problem
at all: a ceiling nothing can exceed is a ceiling that does nothing, so `CONTEXT.md`'s *"a Building whose
Trips keep failing declines and is eventually abandoned"* has no failure case anywhere on the map.

### The player decides the shape, and today they cannot

The design's late game is *"sprawling polycentric cities with interdependent Settlements"*, and `0085`
treated that as a promise to be rescued by finding some other mechanism to produce separateness. That
was the wrong frame. **Whether a city is one blob, a few large towns, or many small ones is the
player's decision**, and the simulation's job is to be able to register whichever they choose.

At 16.4 km it cannot. Two areas joined by a road are one town, always, because everything is within
range of everything. The player's only lever is refusing to build the road, which is a binary and a
crude one. At 65.5 km, building compact gives one city, spreading out gives several, and — the part that
matters — **the middle is real**: a town at the edge of range, tipped in or out by a new bypass or by
congestion, which is `0020`'s merge and split rows finally having something to act on.

So this is not *choosing* polycentricity. It is restoring the **degree of freedom** that the units error
had quietly removed.

### The measurement, and what a rung buys

| `WorldCells` | Across | Land | Occupied by 1M | Commutes across (30 min) | Foot crossing |
|---|---|---|---|---|---|
| 128 — today | 16.4 km | 268 km² | ~100% | **0.9** | 3.3 h |
| 256 | 32.8 km | 1,074 km² | 25% | 1.9–2.6 | 6.6 h |
| 384 | 49.2 km | 2,416 km² | 11% | 2.8–4.0 | 9.8 h |
| **512** | **65.5 km** | **4,295 km²** | **6.3%** | **3.7–5.2** | **18.5–26.2 h** |

> **⚠ Two corrections to the table, 2026-08-13.**
>
> **The fourth column was headed *Buildable for 1M* and that is the wrong word.** It is the share of the
> map a 1M city **occupies** at the developed density, not a share the player is permitted to build on —
> nothing restricts where anybody builds, and [`0090`](0090-the-generator-makes-land-and-the-player-makes-every-road.md)
> makes that explicit. Renamed rather than recomputed; the numbers were right and the heading invited the
> opposite reading of the whole ADR.
>
> **The fifth column's 30-minute Budget no longer exists**
> ([`0095`](0095-a-commute-budget-is-three-rungs-and-only-the-last-one-refuses.md)), and the ratio has two
> values now: at 512, **5.6–7.8** crossings at the *fast* rung of 20 minutes and **2.2–3.1** at the
> ceiling of 50. **The ceiling is the one that governs**, because two places are one labour market exactly
> when somebody will take the commute between them. So this ADR's figure for the shipped design is
> **2.2–3.1** — below the 3.7–5.2 it was granted, far above the 0.9 it was written against, and therefore
> still several settlements rather than one blob. *Which rung governs is filed to `plans/0002` as
> measurable rather than settled here.*

Corner to corner at 512 is **633 Ticks** Euclidean and **895** Manhattan, against **171** for a
30-minute Budget — 7.7% and 10.9% of a Day. Real routes fall between, because Arterials at 90 km/h exist.

**512 is chosen rather than derived, and the thing that makes it defensible is the shape of the error on
each side.** Too small and the degree of freedom above does not exist — that is the state being repaired,
and it went unnoticed for the life of the project. Too large and the map is emptier than a player will
fill, which is visible in the first hour and costs nothing but scenery, because unbuilt ground is a null.
**The two failures are not symmetric**, so the honest move is to take the largest rung and let a real
city say whether it is too much. `0052` is satisfied by naming what would refute it, below, not by
pretending the number was calculated.

One figure is worth holding: at 6.3% buildable, **a 1M city occupies 270 km² — which is the whole of
today's map.** The city does not change size. What changes is that it now sits in a region.

### The density argument does not bind, and that is `0021` doing its job

`0085` refused a larger map on density: at 1M, 4,295 km² is 233/km², which is rural. That reasoning
assumed the map must be **filled**, and `0021` says plainly that it must not be — *"an undeveloped Chunk
is a null, not an array. Memory and save size therefore scale with developed area, not with map area — a
4096² map with a city on 5% of it costs what a 1024² map with the same city costs."*

So the density figure to check is the **developed** one, which stays at 3,700/km² by construction. ⚠ **And *by construction* is the whole of it — this is not evidence.** 3,700 is an **output of the 1M target** (`plans/0002` §1's column reads *"1M implies"*), so *the developed density is unchanged* says only that neither of the two numbers it is made of has moved. The figures that do bracket it come from the build — **2,738 and 5,136/km²**, from `lots_per_segment` over 5a's Segment count and from `World`'s Lots-per-Citizen — and 3,700 sits between them, which is the closest thing to corroboration available. `plans/0012` **Cause 5**. The
map-wide figure is not a density; it is a statement about how much open ground there is, and open ground
is the mechanism.

Checked against the build rather than argued: the only structures that scale with map area are the four
`int[]` arrays behind `CellResidency` and `BuildingResidency`, at `WorldCellCount` entries each. That is
**262 KB today and 4.2 MB at 512**, against 86 MiB of tables at 1M — and all four are **derived**, so
they enter neither the save nor the State Hash. `LayerCellTable` is sparse, Lots and Buildings are
per-entity, and the travel-time matrix is keyed on Districts, which exist where development does.

### The blocker, and it is `0021`'s own claim being false in one place

`RoadGenerator` lays a complete Street lattice over the whole map at world creation:

```
nodes = (WorldTiles ÷ block_tiles + 1)²
```

| | 128 Cells | 512 Cells |
|---|---|---|
| Nodes | 16,641 | **263,169** |
| Street Segments | 33,024 | **525,312** — 15.9× |
| Lots at `lots_per_segment = 5` | 165,120 | **2,626,560** |

`World` allocates **225,000** Lots for a 1M city. A 512-Cell map would generate **11.7× more Lots than
the population can ever occupy**, and the Zone Rule's sample is derived from the Lot count
([`0059`](0059-a-zone-rules-sample-is-a-revisit-period-so-the-ruleset-states-a-duration.md)), so Tick phase 6
would get 16× more expensive sweeping ground nobody will build on.

**This is a defect and it is `0021`'s sentence being false.** The generator is the one structure whose
cost is a function of map area rather than of development, and at 16.4 km it could not be noticed,
because a map that small is one a city genuinely does pave. It also sits directly on top of
`World.cs`'s existing comment marking the Lot-to-population ratio as a *"LIVE INCONSISTENCY"* — that
comment was describing this from the other end and nobody connected the two. ***A structure that
contradicts a claim only at a scale nobody has run is a claim with no test.***

The repair is not this ADR's, and it must not be guessed at here: it is `plans/0002` **ledger #2** —
*open map, or progressive land unlock?* — which has carried a recommendation (**unlock by
serviceability**) and no decision since session three. **Growing the map forces that question**, because
a full lattice is the *open map* answer implemented by default and nobody chose it.
[`0070`](0070-an-unbuilt-mechanism-is-not-a-design-constraint.md) applies in the other direction too: the
unlock rule is *undesigned*, not refused, so the answer is to design it rather than to work around it.

## Consequences

- **The constant does not move until road generation is scoped to developed land.** `WorldCells` stays
  128 in the build. This ADR is the decision; the flip is gated, and the gate is named so it cannot be
  struck for the wrong reason.
- **`plans/0002` ledger #2 is promoted from a recommendation to a blocker.** It now gates a decision that
  is taken, which is a much stronger position than it had.
- **The map size is hash-bearing and world-creation-fixed, and it is now in `0002` §D2 with a ratifier** —
  the first long run on the new size that reports a real developed-area distribution. Naming it does not
  choose it (`0052`).
- **`05`'s budget block gains a buildable-fraction term with a real value.** It currently reads `× ~1.0`,
  which was true only because the map was too small to be anything else.
- **Every distance-dependent claim in the corpus is re-opened favourably**, and none of them may be
  quietly updated: the Commute Budget's decline mechanism, Severance's cost, `0088`'s edge choice and
  `0020`'s Settlements all acquire a working range they did not have. They should be re-read after the
  flip, not before.
  > **Amended 2026-08-12: that list missed the largest one.**
  > [`0022`](0022-land-is-a-stock-the-city-spends.md)'s macro-arc — farms retreat outward as development
  > degrades fertility, until farm workers fall out of the commute shed and the farm village becomes its
  > own Settlement — is a distance claim that **cannot complete at 16.4 km**, because the far corner is
  > under thirty minutes from town. The four claims named above are mechanisms; this is the whole late
  > game, and it was inert. It was missed because the claim is worded in *retreat* and *reachability* and
  > never in metres: ***a claim about distance is not always a claim that says so***.
- **`0085` is superseded on its decision and stands on its findings.** The 73× correction, S2 R1.5's
  unread column, and *a connected component fragments because an edge is missing* are all still true —
  what changes is that at 65.5 km, **distance is one of the things that makes an edge missing**, which at
  16.4 km it was not. Its *"nothing on this map is far away"* title is true only of the old map.
- **S2's routing figures survive the map change and not the generator fix.** They are priced against
  ~30,000 Segments, which is what a *developed* 270 km² produces — the same city. What would move them is
  the full lattice, which is exactly what the blocker removes.

## What would trigger revisiting

- **A real city showing 6% buildable reads as empty rather than as regional.** This is the failure mode
  of taking the largest rung and it is the one to watch first. The symptom is a player who never leaves
  the first quarter of the map, and the repair is a smaller rung — 384 or 256 — not a change to anything
  else. **Nothing accretes on map size that a rung change would break**, since every dependent quantity
  is derived from it, which is what makes taking the large end cheap.
- **The Commute Budget landing far above 30 minutes.** Everything here is a ratio against it. At 60
  minutes the 512 map is 1.9–2.6 commutes across and behaves like the 256 rung; at 90 it is back to being
  one shed. 5b-bis produces the distribution the Budget is a percentile of, and this ADR should be
  re-read against the number rather than against the worked example.
- **The Tile ceasing to be ~4 m.** It is the other half of the exchange rate `0082` found spent three
  times over, it has never been ratified, and it is a second lever on exactly this quantity — 4096² at
  16 m is the same 65.5 km. It is refused here because a Tile is a *building-detail* unit, and changing
  it re-denominates every metre figure in the corpus while making a Lot too coarse to hold a house.
- **Routing costing more than linearly in map extent.** Everything above assumes the graph tracks
  developed area once the blocker clears. If a search's cost turns out to depend on the *bounding box*
  rather than on the reachable set, that is a different argument and it belongs to S2's successor.
