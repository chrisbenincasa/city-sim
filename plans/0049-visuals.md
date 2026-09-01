# 0049 — The visuals queue

Scoped 2026-09-01, against `main` at `790267e`. **The renderer's own document, and it iterates
separately from the simulation on purpose.**

[`0045`](0045-amnesty.md)'s *Situation* names the hole this fills in five words — ***no renderer, no
plan for one*** — and it has been true since the first commit. The shell draws a city; nothing has
ever said what the city is supposed to look like, so every visual decision has been taken inside a
method by whoever was there.

---

## Why this is a separate queue

**Every row here is shippable alone, and all but one moves no State Hash.** That is the property
worth protecting: the drawing can be worked on for a week without the simulation moving, and the
simulation can move without invalidating a row.

⚠ **The one exception is marked**, and it is marked because it is a Ruleset change wearing a visual
motive — `block_tiles` and `occupants` are the city's own numbers, and a skyline is what you get
rather than what you asked for.

**Three standing rules, and the first two already exist.**

1. **The renderer cannot influence the simulation** (`adr/0007`, `05 §4`). Nothing in this queue may
   feed back. A frame is a reading.
2. **Invented geometry is labelled as invention.** `PlotDepthMetres` already carries the sentence —
   *a thickness the city does not have, invented so the picture reads as a city rather than as a bar
   chart* — and every row below that adds geometry inherits it. ⚠ **It draws on no `purpose_tag` and
   must not**: the simulation's stream is for decisions, and a shape nobody in the city can perceive
   is not one.
3. **A row is done when somebody watched it and something surprised them** ([`0045`](0045-amnesty.md)'s
   amended Definition of done). `plans/0048`'s `--drive` is how a row is watched twice.

---

## The queue

| # | Row | Hash | Status |
|---|---|---|---|
| **1** | **Fill factor and roofline.** A Building filled 72–98% of its Lot's frontage, so a block face was two slabs meeting at the corners. Widen the band, and give it a roof | no | ✅ 01-09 |
| **2** | **The ground is drawn from the ground.** Five `[[terrain]]` types, a water graph, a catchment, a shoreline and a Hazard Region all exist in the world and all render as one olive plane | no | |
| **3** | **Block interiors.** Trees, rocks, yards, fields. ⚠ **Dressing, by rule 2** — the middle of a block is empty in the *model* and always will be | no | |
| **4** | **Density and height.** `[roads] block_tiles` down, `[[building]] occupants` up, and a second dwelling kind so the skyline is mixed rather than uniformly taller | 🔴 **yes** | |
| **5** | **Night.** Light follows the clock, and windows are lit from **occupancy** | no | |

⚠ **In order, cheapest first, and each is visible on its own** — which is the test that they really
are separable.

---

## What is already there, so that nobody rebuilds it

**More than the picture suggests.** `Main.Buildings()` already varies height, varies frontage,
derives storeys from the kind's `occupants`, sets back from the kerb by half the carriageway plus
half its own depth, and keys every draw on the Building's monotonic row id — so a Building keeps its
shape as long as it stands and a rebuild on the same Lot is visibly a different building.

⚠ **The MultiMesh is per *kind of thing* and not per Chunk**, which is not what `05 §2` says.
`_buildings`, `_travellers`, `_roads`, `_plots`, `_cells`, `_ground`, `_water`, `_flood`, `_hazard`
and `_cursor` are one apiece, with per-instance colour where it is wanted.

⚠ **A MultiMesh instance colour is multiplied into albedo in *linear* space**, which is recorded in
the shell already and is the trap anybody picking a palette will hit first.

---

## Findings

| | |
|---|---|
| **F1** | 🔴 **THE BUILDINGS ARE WIDE BECAUSE THE LOTS ARE, AND THE LOT WIDTH IS DERIVED.** `Frontage` is `2 × block_tiles × 4 m ÷ lots_per_segment`; at the shipped `block_tiles = 32` and `lots_per_segment = 5` that is a **128 m block face carrying 2.5 Lots of 51.2 m each**. The renderer then filled **72–98%** of it — `FrontageFill` 0.85 against a 0.85–1.15 jitter — so a **37–50 m** building sat on a 51.2 m Lot and the gap was too narrow to see. Beside a depth of 9.6–16.8 m and a height of 7.7–25.9 m, ***the shipped building was a slab three to four times wider than it is deep, and wider than it is tall.*** ⚠ **`lots_per_segment` IS NOT THE LEVER**: the 5 is `CONTEXT.md` → Address's own *five Buildings share a Segment* and is the premise of the *an Address is never a Node* refusal. **The levers are `block_tiles` and the renderer's own fill fraction**, and they are a Ruleset change and a free one respectively |
| **F2** | ⚠ **THE MIDDLE OF A BLOCK IS EMPTY IN THE MODEL AND IS NOT A DEFECT.** `adr/0078` — a Lot has no depth and there is no depth key; Lots hang on Segments. The renderer reaches 10–17 m back from the kerb and the remaining ~48 m to the centre of a 128 m block is nothing the city knows about. ***So it is the one large surface the renderer may dress without lying***, which is row 3. ⚠ **Filling it with BUILDINGS would be a design change against a settled ADR** and is not what row 3 is |
| **F3** | ⚠ **A MIXED SKYLINE IS BUILDABLE TODAY AND A *DOWNTOWN* IS NOT.** `ZoneRuleDefinition` carries one `Kind`, but several `[[zone_rule]]`s may target one zone and contend for Lots, with declaration order load-bearing — so two dwelling kinds at different `occupants` give a mixed skyline now, and the renderer draws the tower without being told to. **What is missing is the SIGNAL that would put the tall ones in the middle**: siting is best-of-N scored over the sample (`adr/0170`), and the score that would want centrality is land value, which is **zero everywhere by construction except `fouled.toml`** because nothing else emits. ***Unbuilt rather than refused*** (`adr/0070`) |
| **F4** | ⚠ **ROW 4 GOES STALE ON A DOZEN HEADERS THE DAY IT LANDS.** Raising `occupants` houses the same population in fewer Buildings, so the Pool drains faster and fewer Lots are consumed; every census figure quoted in the Ruleset headers and in `CLAUDE.md` — the *602 built, 385 abandoned, 387 vacant* line and the decline shares — is measured against the old value. ***That is a documentation refresh and not a reason to defer*** ([`adr/0100`](../docs/adr/0100-moving-the-state-hash-costs-nothing-until-somebody-is-carrying-a-save.md)) |
| **F5** | ✅ **ROW 1, AND THE BAND DID MORE THAN THE ROOF DID.** `BuildingFillLow`/`High` **0.42–1.0** puts a terrace beside a house with a yard where there were two slabs; **150 of 241** Buildings take a gable, the rest being over `PitchCeilingMetres` or drawn flat by the scramble. ⚠ **The gable is a `PrismMesh` off the shelf** ([`adr/0018`](../docs/adr/0018-prefer-off-the-shelf-infrastructure.md)) on a second MultiMesh, and **one derivation feeds both layers** — a second walk over the Building table re-deriving the same setback and footprint is `plans/0012` **Cause 1** with a frame between the copies |
| **F6** | ✅ **THE ROOF WAS CHECKED AGAINST ITS BUILDING RATHER THAN LOOKED AT.** A gable is a prism extruded along its own Z, so the east–west case turns a quarter — and ***a 90° error is the easiest thing in this row to commit and the hardest to see.*** `draw` dumps both layers with a yaw column; joined by id, **all 150 roofs match their body's footprint and centre to 0.01 m**. ⚠ **That is `plans/0048`'s thesis arriving** — the answer is the draw list and not the frame |
| **F7** | 🔴 **THE BLOCKS LOOKED HALF-EMPTY BECAUSE A CORNER LOT WAS CUT BY 69%, AND THE FIX IS TO SLIDE IT RATHER THAN SHRINK IT.** Measured off the `draw` dump, counting each **kerb** — one side of one 128 m Segment — separately: **59% of a kerb was built**, two or three Buildings on it, median gap **16.3 m** and the biggest **52.6 m**. ⚠ **A Segment's five Lots ALTERNATE SIDES**, so one kerb carries 2–3 of them at 51.2 m spacing, and `Frontage.OffsetOf` leaves the outermost half a spacing from the junction — which put a full-width Building **12.8 m into the cross street**. The old rule took the lesser of the wanted width and the room and cut a 51.2 m Lot to **16 m**. ✅ **Keeping the width and moving the centre** cut only a Building too wide for the whole block: **59% → 72%**, and 78% once `BuildingFillLow` followed to 0.55. 🔴 ⚠ **BOTH OF THOSE FIGURES WERE A SUM OF WIDTHS AND THE SUM DOUBLE-COUNTED OVERLAPS — see F9**, where the same kerbs measure **74% by union with nothing overlapping**. ⚠ **0.65 reaches further and was not taken** — it narrows the width range to 1.5× and the slab wall comes back |
| **F8** | ⚠ **THE BARE GRID IS THE LATTICE AND NOT A DEFECT.** `schooled.toml` at 2,000 Citizens paves **8×8 blocks of 128 m** — 144 Segments over 1,024 m square — and subdivides Lots into about **28 of the 81 block cells**, in raster order rather than outward from a centre. Every cell that has any has **exactly 10** (Buildings plus vacant Lots), so ***the built part is full and the empty part has no Lots at all***. 🔴 **A city that grows in reading order rather than from its middle is a finding about `SyntheticCity` and not about the drawing**, and it is the thing to look at before row 4 moves any density number |
| **F9** | 🔴 **A LOT'S FRONTAGE WAS RIGHT ON AVERAGE AND WRONG ON EVERY KERB, AND THE CORNER FIX TURNED THAT INTO BUILDINGS STANDING INSIDE EACH OTHER.** `Frontage.SideOf` is **odd-and-even house numbering** — indices 0, 2, 4 left and 1, 3 right — so a Segment's five Lots split **three and two**. At 51.2 m each that is **153.6 m of Lot on a 128 m kerb** on one side and **102.4 m** on the other: ***one side over-subscribed by a fifth and the other short by a fifth***, which is why the short kerbs measured 67% and could not have done better, and why the long ones measured "100%". ⚠ **F7's slide removed the cut that had been hiding it** and the over-subscribed side interpenetrated — **64 overlapping pairs, measured**, in a build that had already been committed. ✅ **A Lot's frontage is now the half-way line to its neighbours ON ITS OWN SIDE** (`Main.Kerb`, calling `Frontage.OffsetOf` rather than restating it), which tiles each kerb exactly and is asymmetric where the offsets are. **74% built by union on both kerb types, 0 overlaps, median gap 9.0 m, biggest 25.5 m** — and the two types now measure *the same*, which is the check that the geometry rather than the taste was fixed. ⚠ **What is left unbuilt is the width jitter and the junction clearance, and nothing else** |
| **F10** | ⚠ **A SUM OF WIDTHS IS NOT A COVERAGE, AND IT REPORTED 100% ON THE KERBS THAT WERE BROKEN.** The instrument that found F7 added the widths on a kerb and divided by 128 — so overlapping Buildings counted twice and the most defective kerbs scored highest. ***A measure that cannot go above its own maximum is the one to write***: the union of the intervals can, and does |

---

## Art style — open, and the constraints that bind it

**Undecided as of 2026-09-01.** What is settled is *the camera and two properties*, decided by the
player on 2026-08-31 and 09-01:

- **A high-level default camera.** *You can meet people; that is not the point of the game.* So
  silhouette, roofline, ground and colour do the work, and façade detail does not.
- **Light follows the clock, and a lit city at night is wanted.**
- **Kinds announce themselves heavily.** A school looks like a school.
- 🔴 **Not diagrammatic.** *This needs to look "city like", not just shallow, drab nothingness.*

⚠ **The declared preference is *vibrant and colourful, but not garish*, and small-cube voxel is under
consideration especially for terrain depth.**

**Four constraints, and they are not preferences.**

| | |
|---|---|
| **Instancing** | One MultiMesh per kind of thing, per-instance colour and transform. ***Variety through instancing is cheap and variety through unique assets is not.*** A kind that announces itself needs **one MultiMesh per appearance family**, each with its own mesh |
| **The Ruleset declares the look** | Otherwise the first `serves = "health"` kind anybody writes is an untextured cube for ever. Same rule as strings: `Core` hands over an id and the shell resolves it through the Ruleset |
| **The overlays** | Pollution and land value are the instrument the pitch rests on. ⚠ **The answer is that an overlay TAKES OVER rather than tints** — the city drops to an unlit base and the data owns all the colour. Two looks, one switch, neither compromised |
| **The night is data** | Windows are occupancy. A dark Building at 22:00 is a tenancy that ended; a block lit at 03:00 is a Shift nobody intended. ***So the richest thing on the list is also the most legible***, which is the argument that changed the author's mind |

**Candidates, none chosen.**

| | |
|---|---|
| **Small-cube voxel** | ✅ **The most distinctive, and terrain depth is where it pays** — a cut shoreline, a cliff, strata, a floodplain with a real edge. Night is nearly free: a window is an emissive voxel. 🔴 **It breaks the instancing architecture**: surface voxels over a 65.5 km map cannot be instances, so it wants greedy meshing into chunk meshes, which is a bespoke renderer against [`adr/0018`](../docs/adr/0018-prefer-off-the-shelf-infrastructure.md) unless an addon is taken. ⚠ **The built area is ~1.4 km across, not 65.5** — the volume is far smaller than the map |
| **Vibrant flat-shaded low-poly** | Hard normals, a controlled saturated palette, a strong key with real shadows, a coloured sky. **Cheapest by a wide margin** — it is what the shell already draws plus a palette, roof meshes, shadows and a sky, and it scales to a million. Least distinctive |
| **Painterly** | Ramp shading, warm grade, ambient occlusion in the block interiors, a paper grain. ⚠ *Vibrant not garish* comes from the **grade** rather than from restraint in the palette. Risk: post-process grain fights the overlays and goes muddy at borough distance |
| **Diorama** | Tilt-shift, a slightly oversaturated toy palette, visible material warmth, props. **The one that most rewards a high-level camera specifically** |

⚠ **A hybrid is the author's recommendation and is not a decision**: **voxel ground, instanced
buildings.** Terrain gets literal depth where the interest is and is a bounded volume; Buildings stay
instanced massing so a million of them still draw, with voxel-scale detail only in the window grid,
which is where the night look lives.

🔴 **No ADR may be written for this while [`0045`](0045-amnesty.md) stands** (standing order 1), and
the decision is *arguable* rather than *measurable* so [`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)
does not reach it either. ***It is decided here, in this section, and filed properly when the ratio
is earned.***

### What voxel would cost, filed so the question is not re-opened from scratch

🔴 **IT IS THE ONLY CANDIDATE THAT CHANGES THE ARCHITECTURE, AND THAT IS THE WHOLE OF THE DECISION.**
Everything the shell draws today is **one MultiMesh per kind of thing** with a per-instance transform
and colour, which is why 241 Buildings and 150 gables cost two draw calls and why a million of them
is arguable at all.

⚠ **A voxel ground cannot be instances.** Surface voxels over the map are far past what an instance
buffer holds, so it wants **greedy meshing into chunk meshes** — a different pipeline beside the one
that exists rather than a setting on it. Under
[`adr/0018`](../docs/adr/0018-prefer-off-the-shelf-infrastructure.md) that is a **bespoke component
needing a written exception naming the property no library provides**, unless an addon carries it.

✅ **The mitigating fact, and it is large: the built area is ~1.4 km across, not the map's 65.5 km.**
`plans/0042` **F17** and the `flooded.toml` header both turn on the same number. ***The volume is a
small fraction of what the map size suggests***, so the argument against is about the pipeline and
never about the extent.

⚠ **A hybrid keeps both** — voxel ground, instanced Buildings — because the two questions are
separable: the ground is one surface with depth, and the Buildings are a hundred thousand small
objects. **Nothing decided.**
