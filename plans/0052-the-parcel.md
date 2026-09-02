# 0052 — The Parcel

Scoped 2026-09-02, against `main` at `ff3bfa8`. **A Lot gains extent.**
Grilled 2026-09-02; findings **G1**–**G8** below, and the design is the post-grill one.

⚠ **Written under an explicit, temporary relief from [`0045`](0045-amnesty.md)**, granted for this
document only. The amnesty's rule stands everywhere else, and the relief is for *planning* — the
ADR this plan names is written when the shape below is agreed, not now.

---

## The claim

**`CONTEXT.md` already calls a Lot *"a developable parcel with road frontage"*. The schema disagrees
with the vocabulary, and it is the schema that is wrong.**

A Lot today is four saved columns — `east`, `north`, `zone`, `side` — a point on a Segment plus which
kerb it steps to. It has no extent. So **anything that needs to know how much ground a Building
occupies must invent it**, and **five** separate inventions now exist:

| Invention | Where | What it guesses |
|---|---|---|
| `PlotDepthMetres` = 26 m | `Main.cs:179` | how far back a Building reaches |
| `Depth(block, shape)` | `Main.cs:2233` | that, jittered, capped at half the block |
| `Deepest(block)` | `Main.cs:2206` | the deepest a *neighbour* could be, to reserve against it |
| `Kerb` / `PastTheCorner` | `Main.cs:2120-2199` | which stretch of kerb is this Lot's, and who owns the corner |
| 🔴 **`KindDefinition.FootprintTiles`** | **`Rules/Ruleset.cs:681` — in `Borough.Core`** | **how many Tiles the Building covers, and therefore Seals** |

**They collided.** [`plans/0049`](0049-visuals.md) **F21**: four faces claimed the corner ground
independently, and two inventions landed on one patch — **28 overlapping pairs, 2,610 m²**. The repair
was a sixth invention, an arbitrary allocation rule (*east–west keeps the corners, north–south
yields*) whose own doc comment says what it is:

> ⚠ **THE FIX IS ALLOCATION AND NOT GEOMETRY**, which is what every other city builder does — a
> SimCity 4 lot is a rectangle of tiles and a tile belongs to exactly one lot; a Cities: Skylines
> block is 8 m zoning cells, each claimed by exactly one road's strip.

***A design that forces five independent re-inventions of the thing it refused is a design leaking.***

### 🔴 The fifth is in the SIMULATION, and it cites `adr/0078` as the reason it exists

**G4.** `KindDefinition.FootprintTiles` — *"How many Tiles a Building of this kind covers, and
therefore how many it Seals."* Its own remarks:

> **It exists because `CONTEXT.md` → Building says a Building *"has a footprint (the set of Tiles it
> covers)"* and *"interacts with Map Layers through that footprint"*, and nothing in the build carried
> one.** A Lot stores a position and no extent, and **`adr/0078` refused it a depth on purpose** — so
> the footprint cannot be derived from geometry and has to be declared.

⚠ **That paragraph is this plan's argument, written by somebody who hit the wall inside `Borough.Core`
rather than in the renderer.** It matters three ways and each is separately sufficient:

1. **It refutes `adr/0078`'s premise from inside the simulation.** *Nothing reads land area* is not
   merely overtaken by a renderer that postdates the decision — the **sim** reads it, at
   `World.cs:3425`, on the path to `MapLayers.Seal`.
2. **It is hash-bearing and it runs a chain.** Sealing → Fertility (`base × Sealing / 1024`) →
   desirability, and Sealing → **Woodland**, because [`adr/0159`](../docs/adr/0159-woodland-is-a-tile-count-per-cell-bounded-by-sealing-because-the-ground-has-one-budget-and-not-two.md)
   makes a Cell's Tiles one budget and `MapLayers.Seal` takes the trees down itself.
3. **29 of the 31 shipped Ruleset files leave it at 1.** Only `pictured.toml` and `schooled.toml`
   author it, both at **4**. Its own comment calls one Tile *"16 m², which is smaller than any real
   building"* and flags *"a kind holding three Households in one Tile"*.

### What it costs, measured rather than argued

`SealingMeasurementTests`, 4,000 Citizens, Cell = 1,024 Tiles:

| | `minimal.toml` | `twinned.toml` | `severance.toml` |
|---|---|---|---|
| Buildings | 481, at `footprint_tiles = 1` | 481 | 481 |
| Buildings' share of all Sealing | **4%** | 4% | 🔴 **0%** |
| Roads' share | 95% | 95% | 99% |
| Mean over sealed Cells | 6.2% | 6.0% | 3.5% |
| **Peak Cell** | **115 Tiles = 11.2%** | 117 = 11.4% | 198 = 19.3% |

🔴 ***The most built-up Cell in a generated city keeps 89% of its farmland.*** A city drawn as five-
and six-storey tenements stands on ground that is agriculturally almost untouched, and the Buildings
are a rounding error against the roads — in `severance.toml` they round to **zero**.

⚠ **Taken on a dirty tree that paves about 1.8× the area `main` does**, so the *road share* is
overstated and the Buildings' share understated. The Building figures are exact and need no run:
**8 Lots per block × 1 Tile = 8 Tiles of a 1,024-Tile block**, a block being exactly one Cell at
shipped figures. ⚠ **Re-take the shares on a clean tree before any document quotes them.**

### And it makes `adr/0025`'s central axis inexpressible

[`adr/0025`](../docs/adr/0025-density-is-a-cap-and-it-trades-land-for-materials.md) calls the
Land-against-Materials trade *"not a tuned trade-off; it is the physical one"* — **sprawl spends Land,
height spends Materials.** Today **every Building spends 16 m² of Land whatever it is**, so neither
side of the trade moves and the axis cannot be felt. ***The strategic lever the ADR is built around is
not merely unbuilt; it is contradicted by a constant.***

---

## What [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md) decided, and which half is being revisited

It made **two** claims and they have completely different standing.

**(a) Frontage is `(derived AND rebuilt)`, never saved.** ✅ **This survives untouched and this plan
extends it.** The reasoning is exact and still holds: a fact computed from other saved facts, stored
beside them, is the shape `adr/0063` and `adr/0064` were each deleted for — *"a number copied onto a
row at creation and never re-derived, so the edit reaches everywhere except where it matters"* — and
the edit that would not reach a saved frontage is **the player bulldozing the Street**.

**(b) Depth does not exist, because nothing reads land area.** 🔴 **The premise is false, `adr/0078`
names two of the four consumers itself, and the fourth is in the simulation.**

| Consumer | Standing | Named by `adr/0078`? |
|---|---|---|
| 🔴 **`FootprintTiles` → Sealing → Fertility and Woodland** | **Exists today, in `Borough.Core`, hash-bearing** | No — and it cites the ADR as its own excuse |
| **The renderer** — five inventions, one collision, one arbitrary tie-break | **Exists today, in the tree, shipping** | No. It could not be: the drawing postdates the decision |
| **`adr/0025`'s density bands** — *"trades Land for Materials"* | Unbuilt | ✅ *"the moment a band reads land area, depth acquires a consumer and stops being a number with nothing to be a property of"* |
| **`CONTEXT.md` → Frontage's own trade** — *"a stock the player spends… narrow terraced Lots eat the available street edge, where stacking preserves it"* | Unbuilt, and **already in the authoritative vocabulary** | Indirectly, via the bands |

⚠ **So this is not an overturn. It is the collection of a trigger the ADR wrote for itself**, plus two
consumers nobody enumerated — one because the picture did not exist yet, one because it was added
*after* the decision and worked around it in place.

***The honest correction is narrow:*** *nothing reads land area* was true on 2026-08-11, stopped being
true the day a Building was drawn as a box, and stopped being true **a second time, inside the
simulation**, the day `footprint_tiles` was declared.

### 🔴 The other trigger, and it is going uncollected

**G6.** `adr/0078`'s *What would trigger revisiting* also names **a Street Segment that is not a block
face**. That case is **live in the build**: `StreetGrid.OffLatticeCount` (`StreetGrid.cs:109`) with its
own per-block index, populated by Arterials — `LineSourceQueryTests:229` asserts one lands there.

⚠ **Today the consequence is benign and under a partition it stops being benign.** `SubdivideBlock`
lays nothing off the lattice, so an Arterial simply gets no Lots. Once every Tile in a block belongs to
exactly one Lot, **a road crossing the block has to take ground from a named owner.** This plan does
not solve it and must not pretend to — see *Open questions* **Q5**.

---

## The design

### One sentence

**A Lot's parcel is a rectangle of ground, `(derived AND rebuilt)` exactly like its frontage; the
subdivider produces it by PARTITIONING a block rather than by sizing each Lot independently; and a
Building's footprint is its parcel's area, so `footprint_tiles` is deleted rather than retuned.**

### Why derived rather than saved

The same argument `adr/0078` made for frontage, and it is the reason this plan does not contradict
that ADR at all. A saved parcel goes stale against a road edit; a derived one cannot. It also means:

- ✅ The parcel is a pure function of saved state, so determinism is by construction.
- ✅ `LotTable.BuildingSlot`, `FrontageSlot` and `FrontageOffset` are the precedent and it is exact.
- ✅ **Save-compatible.** `Rows.cs:512` — a load *"restores the `Saved` columns and nothing else"*, and
  derived columns are rebuilt by `World.RebuildDerived`. Verified rather than assumed.

⚠ **`Rows.Derived` allocates and does not rebuild.** `World.RebuildDerived` must populate the parcel
columns or `DerivedRebuildAuditTests` will name them — which is the check that caught
`car_park.segment_next` on milestone 7, and it is the one test that would catch this going wrong.

🔴 ⚠ **THE STATE HASH MOVES ANYWAY, AT STAGE 1, AND THAT IS CORRECT.** Not because of the parcel — the
parcel is derived and outside the hash — but because **deleting `footprint_tiles` changes Sealing, and
Sealing is saved.** An earlier draft of this plan made *stage 1 moves no hash* a design goal and let it
argue for keeping an invented number in place. That is exactly what
[`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)
forbids: ***never cite hash movement as a reason to defer, narrow or split work.*** What survives is
attribution — the commit says why.

### Why the footprint derives with NO new number

**`adr/0025` already decided this and the sentence is unambiguous:**

| | What it is | Verdict |
|---|---|---|
| Road **type** granting permission for height | a **gate** | **Rejected.** It pre-empts the simulation |
| Block **geometry** determining parcel size and frontage | a **physical consequence** — how much land is in the Lot | ✅ **Kept. It is not a rule at all; it is arithmetic over what the player drew** |

***`footprint_tiles` is an authored constant standing exactly where the design says arithmetic
belongs.*** So the parcel supplies the arithmetic and **the key is deleted, not replaced.**

⚠ **Two derivations were on the table and only one introduces no number.**

| | Derivation | Introduces a number? |
|---|---|---|
| ✅ **Taken** | **footprint = the parcel's area.** The Lot's whole ground is spent, garden included | **No** |
| Deferred | footprint = parcel area × a coverage fraction | Yes — but it would be `adr/0025`'s **band**, a lever the design already wants |

The first reads [`adr/0022`](../docs/adr/0022-land-is-a-stock-the-city-spends.md)'s *"Land is a stock
the city spends"* literally: **you cannot farm somebody's back garden.** The second can arrive later as
a multiplier defaulting to 1, without the first having been wrong.

⚠ **It stretches one word in `CONTEXT.md` → Sealing** — *"the count of Tiles in a Cell **ever built
on**"* — because a garden is developed rather than built on. ***That is a corpus correction and not a
design problem***, filed in [`0012`](0012-corpus-audit.md).

⚠ **Sealing will saturate and the arithmetic must be checked, not assumed.** A block is exactly one
Cell at shipped figures (`block_tiles = 32`, `CellGrid.TilesInCell = 1024`), so the parcels of one
block plus its carriageway may exceed 1,024 Tiles. `MapLayers.Seal` **clamps** at the write site — no
correctness break — but a saturated Cell has Fertility 0 and stops distinguishing between two
differently-built Cells. **Measure the peak before and after.**

### Why a partition rather than a size

**This is the whole point and it is what makes the collision impossible rather than merely fixed.**
Five independent sizings can overlap. A partition of the block's ground cannot: every Tile belongs to
exactly one Lot or to the interior, by construction, at every block size.

**The geometry is already worked out, in the shell, by whoever hit F21.** `PastTheCorner`'s comment:

> **two faces take the corners and two give them up: the horizontal pair runs the block's whole
> length, the vertical pair starts a depth in from each end. Four rectangles, no overlap, no hole, at
> every block size.**

⚠ **So the move is a LIFT rather than a design.** [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md)
is explicit that a workaround in code the spike does not own must be routed to the code that owns it,
*on the day*. This one was not, and the interest on that is the five inventions above.

⚠ **The reserve becomes exact rather than worst-case.** `Deepest` reserves against *the deepest a
neighbour could be*, because *"the neighbour's draw is not knowable from here"*. Under a partition it
is knowable, so the corner gives back the ground it was holding in reserve.

### 🔴 Stage 1 makes the picture MORE uniform, and this must be said out loud

**G3.** `Depth(block, shape)` is **jittered per Building** — `Main.cs:2220` says so in as many words
(*"the caller's `deep`, which is jittered per Building"*). The only clean stage-1 derivation, half the
block minus the carriageway, is **the same number for every parcel in the world**.

***So stage 1 deletes a jitter and installs a constant.*** It is still worth doing — F21 becomes
impossible, five implementations become one, and the footprint becomes real — but **a reader expecting
the frame to improve at stage 1 will be disappointed, and the frame will get flatter.** Either stage 1
owns a jitter in the core, or it accepts the flattening and says so in its commit. **It must not
discover this after the fact.**

### What the interior becomes

At a parcel depth of half the block the frame fills it and there is no interior. Below that there is
one. **So depth is the dial that decides whether a block has a dead middle** — and `02 §2.2`'s
*"a larger block has a proportionally larger dead interior with no number governing it"* becomes
*governed*, deliberately.

🔴 ⚠ **That sentence is a claim `adr/0078` is proud of, and it does not survive contact with the
build. TWO DIFFERENT MECHANISMS WERE CONFLATED, and only one of them exists.**

`02 §2.2` states them in one breath — *"Land that cannot be given frontage stays unlotted and
undevelopable — this is how bad street layouts punish the player **mechanically** rather than through
a penalty number"* — and `adr/0078` then folds the block interior in beside it: *"a larger block has a
proportionally larger dead interior with no number governing it… the mechanism has no number in it at
all."*

| | Land the network cannot reach | The interior of every block |
|---|---|---|
| Fires | **conditionally**, on a block with no Street on any face | **unconditionally**, on every block of every world ever run |
| Player lever | ✅ lay a Street to that face | 🔴 **none, at any skill level** |
| Is it a punishment? | ✅ yes, and it works — `SubdivideBlock` returns 0 | 🔴 **no. A response that is identical whatever the player does is a constant** |

**The second column has no lever, and that is checkable rather than arguable.** `ApplyConnect` snaps
the click **down to the lattice** — `FloorDiv(east, block_tiles)`, `Simulation.cs:797-799`, which is
`adr/0014`'s *"Streets snap to the grid"* — so ***a player cannot lay a Street inside a block***.
`zone` subdivides the block a Tile falls in. `demolish` acts on a Lot. **No verb changes a block's
size**, and `block_tiles` is world-creation Ruleset data, refused on reload. The interior is therefore
outside player control by construction.

⚠ **And even where the lattice is coarse, there is no correct action to take.** A punishment teaches by
having a remedy; without parcels there is no way to build in the middle of a block at any block size,
so the lesson has no answering move. ***A penalty with no counterplay is not a mechanic, it is a
constant subtracted from every board.*** At the shipped figures it is **68 m of every 128 m block**,
larger than the built rank on either side.

✅ **The first column is untouched by this plan and keeps working.** What is withdrawn is only the
claim that the *interior* is an instance of it.

***What replaces the second column is the same lesson with a lever on it*** — deep parcels consume the
interior and spend land; shallow ones keep it and spend frontage — which is `CONTEXT.md` → Frontage's
own trade, written down since before there was a Road Graph and unbuildable to this day.

### 🔴 Half of *does the interior become a Lot* is already settled

**G5.** [`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)
is explicit: *"a **vacant** Lot that loses frontage is deleted and its land returns to unlotted."* An
interior Lot with no frontage **is vacant on the Tick it is created**, so it cannot exist — it would be
deleted before anything could be built on it.

✅ **So the interior belongs to a *frontage* Lot: deep parcels reach inward.** That half was recorded as
*arguable* and is in fact checkable, and it is settled.

🔴 **What it raises is a question this plan did not previously ask.** `PastTheCorner` arbitrates
between **perpendicular** faces. Two **opposing** faces both reaching the middle is F21's collision
rotated 90°, and the allocation rule has no answer for it. That is **Q2** below, and it is genuinely
open.

---

## The stages

⚠ **RESTRUCTURED BY THE GRILL.** The previous draft had four stages and claimed each was
*"independently valuable and independently abandonable"*. **G2 refutes that for the middle pair**, so
they are now one stage.

| # | Stage | Hash | Save | What it buys |
|---|---|---|---|---|
| **1** | **The parcel exists, the shell reads it, and the footprint derives from it.** Parcel columns on `LotTable`, `(derived AND rebuilt)`; the subdivider partitions the block; `World.RebuildDerived` rebuilds it. The shell **deletes** `Depth`, `Deepest`, `Kerb` and `PastTheCorner`. The core **deletes** `footprint_tiles`, its loader refusal and its schema entry | **yes** — Sealing moves | compatible | F21 becomes impossible rather than repaired. Six implementations become one. 🔴 **Fertility, Woodland and the Land half of `adr/0025`'s axis become real quantities instead of a constant** |
| **2** | **Parcels vary, kinds match them, and occupancy scales with the band.** A block's subdivision becomes a *pattern* rather than a fixed count; a Zone Rule gains a term matching a kind to a parcel size; `adr/0025`'s bands supply the occupancy. ⚠ **The old stages 2 and 3, merged — see G2** | **yes** | re-record | **Big buildings that hold the people they look like they hold.** The skyline stops being uniform |
| **3** | **Land is a stock the player spends.** The frontage-against-stacking trade in `CONTEXT.md` → Frontage becomes a real choice with a real cost | **yes** | re-record | The density trade the vocabulary has described since before there was a Road Graph |

### 🔴 Why the middle pair cannot be separated

**G2.** `Occupants` is a property of the **kind**, read as `world.Rules.Kind(kind).Occupants`
(`Evidence.cs:56`, `ZoneRuleEngine.cs:997`). It is not a property of the ground and nothing scales it.

| | Lots per block | × occupancy | Households per block |
|---|---|---|---|
| Shipped | 8 | 4 | **32** |
| Parcels vary, occupancy unchanged | 2 | 4 | 🔴 **8** |

***Bigger parcels alone make the city four times LESS dense, which is the opposite of the goal.***
The coupling that fixes it is `adr/0025`'s density bands — *"a band expresses itself as which kinds a
Lot permits, and a kind declares its occupancy"* — which the previous draft filed as a separate,
later, optional stage. **It is neither separate nor optional.**

### 🔴 And a big parcel does not summon a big Building

**G1.** `ZoneRuleEngine.Create` takes the kind from the **rule**, not the Lot —
`_world.CreateBuilding(..., definition.Kind, ...)` at `ZoneRuleEngine.cs:611`. Admission is a **zone
bitmask only** — `_world.Lots.Zone[lot] & definition.Admits`, `ZoneRuleEngine.cs:276`. ***Nothing
anywhere keys on a Lot's extent, because until now there is none.***

So on the day parcels start varying, **a 4-occupant cottage gets built on a whole-block parcel** and
the block holds one house in a field. Stage 2 needs a **matching term between parcel size and kind** —
new machinery, hash-bearing, and almost certainly new `[[building]]` keys. ⚠ **The previous draft
budgeted none of it and called stage 2 a subdivision change.**

### 🔴 The Building density field is a consumer, and it is fragile

**G7.** `BuildingsInCells.Density` feeds **both** `ZoneRuleEngine.Score` (`ZoneRuleEngine.cs:422`) and
[`adr/0134`](../docs/adr/0134-a-district-is-a-centre-and-its-basin-so-the-count-follows-centres-and-not-a-ceiling.md)'s
District watershed. Stage 2 changes how many Lots a block yields, therefore the field's shape,
therefore **Districts can split**.

⚠ **The evidence that this is a real sensitivity rather than a theoretical one is sitting in the
worktree.** An uncommitted change that paved further while building the same number of Buildings took
`BuildingDensityFieldTests` from **one concentration to two, and two to four**, at every city size —
*exactly* doubled rather than noisy, so it is a shape change and not a tolerance, and it took four
`DistrictWatershedTests` with it. ***A change that only moved where Buildings sit relative to pavement
doubled the District count.***

### ✅ And the scorer has been waiting for exactly this

**G8.** `ZoneRuleEngine.Score`'s own remarks record **three signals considered and refused as NOT
CHEAP** — *near other shops*, *not too near other shops*, *footfall*. **A parcel's area is an O(1)
column read**, so the parcel hands the scorer a cheap new term it went looking for and could not
afford. ⚠ **Until somebody adds it, siting a big Building is decided by density and centrality alone**,
which is G1 arriving one subsystem over.

---

## What this does not change

Stated because each is a thing somebody will reasonably fear.

- ✅ **Frontage stays derived.** `Frontage.Locate` recovers a Segment from a saved position and is
  untouched. The one-to-one derivation `lots_per_segment ≤ block_tiles` protects is unaffected.
- ✅ **A Lot still holds exactly one Building** (`02 §2.2`), checked in both tiers of `§10`.
- ✅ **Lots are still generated, not painted.** Nothing here gives the player a parcel brush, which
  would be `01 §2`'s *"the player never places a Building that Citizens live or work in"* by proxy.
- ✅ **[`adr/0079`](../docs/adr/0079-a-building-outlives-its-frontage-and-an-address-that-has-none-is-a-hole-the-trip-model-reports.md)
  holds.** A Building whose Street is bulldozed keeps standing with no Address — and now with no
  parcel either. ⚠ **The shell must draw a Building that has no parcel**, which is a case it does not
  have today because it always invented one. **This is stage 1's one new failure mode** and it needs a
  test.
- ✅ **No new tuning `const` in simulation source.** Stage 1 **removes** one authored number and adds
  none.

---

## Costs and risks, with the ones that are not yet measured marked

| | Estimate | Standing |
|---|---|---|
| Memory | ⚠ **4 × `int` per Lot, not 2.** The corner allocation moves the start of the vertical pair's run inward, so a parcel's origin is **not** the Lot's `East`/`North` and has to be stored beside the extent. **~3.6 MB** at `World`'s 225,000-Lot sizing for 1M Citizens | arithmetic, not measured |
| `RebuildDerived` cost | Wholesale, on the Epoch. `adr/0078` already names *"frontage becoming expensive to rebuild"* as a revisit trigger and notes the Epoch **already carries the per-Segment granularity an incremental rebuild would need** | 🔴 **unmeasured, and nothing has ever measured the existing frontage rebuild either** |
| Sealing saturation | A block is one Cell; parcels plus carriageway may exceed 1,024 Tiles and `MapLayers.Seal` clamps. No correctness break; a saturated Cell has Fertility 0 and stops discriminating | 🔴 **unmeasured. Take the peak before and after** |
| Stage 1 golden re-record | Sealing is saved, so every hash-bearing fixture moves. `rulesets/pictured.toml` and `schooled.toml` lose their `footprint_tiles = 4` | expected; `adr/0100` says attribution, not scheduling |
| Stage 1 test cost | The shell's geometry gains a core owner, so it gains core tests. F21's overlap check moves from a shell probe to an invariant | not estimated |

🔴 **The one number in this plan that would change the design if it were wrong is the rebuild cost**,
and it is unmeasured on both sides. ***Measure the existing frontage rebuild before stage 1, so the
new figure has a baseline to be compared against*** — otherwise stage 1 produces a cost with nothing
to say whether it is new.

---

## Alternatives considered

| Alternative | Why not |
|---|---|
| **Centralise the shell's inventions into one shell-side allocator.** Cheaper; no core change; fixes F21 | 🔴 **G4 kills this outright.** The fifth invention is already in `Borough.Core` and is hash-bearing, so a shell allocator leaves Sealing, Fertility and Woodland running off a constant. A Rule cannot read a shell allocator either, so `adr/0025`'s bands stay unbuildable — and `adr/0073` says a finding about shared code must reach it rather than be worked around a second time |
| **Retune `footprint_tiles` instead of deriving it** | It is an authored number standing where `adr/0025` says *"arithmetic over what the player drew"* belongs. Retuning it picks a better invented number; it does not stop inventing. And it cannot express two differently-sized Buildings of one kind, which is the whole of stage 2 |
| **Save the parcel rather than derive it** | `adr/0078`'s frontage argument applies unchanged: it goes stale on the edit the mechanism exists to allow |
| **Give the block two dimensions instead** (`plans/0049` **F20**) — Manhattan's 61 × 244 m | Solves a different problem. A non-square block changes the *lattice*; it does not give a Lot extent, so all five inventions survive it |
| **`block_tiles` 32 → 16** (`plans/0049` row 4) | Measured at **33 assertion failures**, and it buys a geometry Barcelona does not have — a 128 m block with a ~60 m courtyard is the Eixample, near enough. Superseded by this plan |

---

## What it would produce

**One ADR, written when the shape above is agreed and the amnesty permits:** *A Lot is a parcel with
extent, the extent is derived like its frontage, and a Building's footprint is its parcel.* It
**amends** `adr/0078` rather than superseding it — the disposition half is adopted wholesale, and only
the *depth does not exist* claim is withdrawn, on the trigger `adr/0078` names.

Possibly a second, if stage 1's partition proves contentious: *a block's ground is partitioned rather
than allocated per Lot*, which is the no-overlap-by-construction claim and is the part that makes F21
unrepeatable.

---

## The grill's findings, in one table

Routed under [`adr/0073`](../docs/adr/0073-a-local-workaround-is-not-a-discharge-and-a-finding-about-shared-code-must-reach-it.md).

| # | Finding | Effect on this plan | Routed |
|---|---|---|---|
| **G1** | A Zone Rule's kind comes from the **rule** and admission is a zone bitmask; nothing reads a Lot's extent | Stage 2 needs a parcel↔kind matching term it did not budget | stage 2 |
| **G2** | `Occupants` is per-kind, so bigger parcels alone make the city **4× less dense** | Old stages 2 and 3 merge; *independently abandonable* withdrawn | stages |
| **G3** | The shell's depth is **jittered**; a derived depth is uniform | Stage 1 flattens the picture and must say so | stage 1 |
| **G4** | 🔴 `FootprintTiles` is a fifth invention **in the core**, citing `adr/0078`; Buildings are **4% of Sealing** and the peak Cell is **11.2%** | The plan's strongest argument; stage 1 gains the footprint derivation and loses *no hash* | stage 1 |
| **G5** | `adr/0079` already settles half of *does the interior become a Lot*: it cannot | Q2 narrows to **opposing-face arbitration**, which is new | Q2 |
| **G6** | Off-lattice Segments are live (`StreetGrid.OffLatticeCount`); `adr/0078` names them as its own trigger | Named as **out of scope**, in Q5, rather than silently assumed away | Q5 |
| **G7** | The Building density field feeds `Score` **and** the District watershed, and doubled its concentration count under a change that only moved pavement | Named as a stage-2 consumer with a hash-bearing consequence | stage 2 |
| **G8** | ✅ `Score` refused three signals as *not cheap*; a parcel area is O(1) | An unclaimed benefit, and a stage-2 obligation | stage 2 |

---

## Open questions

Owed to [`0002`](0002-open-questions.md) when this leaves planning.

| # | Question | Type |
|---|---|---|
| **Q1** | **What is a parcel's depth derived FROM at stage 1?** Half the block minus the carriageway is the only answer that fills the frame exactly, and it makes every parcel in a world the same depth — which is **G3**, and which is the uniformity this plan is meant to end | *arguable* |
| **Q2** | 🔴 **When two OPPOSING faces both reach the interior, what arbitrates?** `PastTheCorner` arbitrates perpendicular faces only. ⚠ **Narrowed by G5** — whether the interior becomes a Lot is settled by `adr/0079` and is no longer part of this question | *arguable* |
| **Q3** | **What does the rebuild cost, before and after?** | *measurable*, and it is the gate on stage 1 |
| **Q4** | **Does `lots_per_segment` survive as a world number through stage 2?** Merged stage 2 says no, and `adr/0078` predicts it: *"at that point `lots_per_segment` also stops being one number and becomes one per band"* | *arguable* |
| **Q5** | 🔴 **What happens to a partitioned block that an off-lattice Segment crosses?** **G6.** Out of scope here and it must not be discovered during stage 2 | *arguable* |
| **Q6** | **Does Sealing saturate once the footprint is the parcel, and does Fertility stop discriminating?** | *measurable*, and cheap — `SealingMeasurementTests` already prints the peak |
