# 0063 — Visual exploration and the neighbourhood fidelity study


## Current direction and live work — 2026-09-05

The player strongly liked the fidelity terrace, welcomed material-scale variation and approved
building the neighbourhood sample. The structure and warm colours are the working direction;
this is not final production acceptance. The latest neighbourhood gallery is ready for review.
This section supersedes the original three-treatment sequence and exact-composition requirement
below. The earlier brief and round notes remain as history. `preferences.json` retains the player’s
words; this plan owns progress, findings and the live work list.

### Work completed

| Round | What it established | Review / authority |
|---|---|---|
| Asset path and initial kit | Blender source → exported GLB → Godot; material-rich, middle and sculpted comparisons | `review.sh kit`; `kit.py`, `facade.py` |
| Extremes and styles | Stronger detail and distinct shading languages; retained comparison anchors | `review.sh extremes` / `styles` |
| Model round | Different house, tower and car geometry in realistic, city-builder, voxel and low-poly interpretations | `review.sh models`; `model-round.py` |
| Palette round | Matched recolouring of preferred geometry; original, warm slate and earth | `review.sh palettes`; `palettes.json` |
| Expanded kit | Remaining agreed building/vehicle/figure roster, tree forms, street and repeated housing fixtures | `review.sh expanded`; `expanded-kit.py` |
| Construction pilot | Real apertures, finer joinery, roofs and glazing on townhouse, shop and factory | `review.sh construction`; `construction-study.py` |
| Fidelity pilot | Textured brick, sash details, curtains, cornices, entrance courts and leaf geometry | `review.sh fidelity`; `fidelity-study.py` |
| Brick scale | Same model and texture files at smaller, current and larger apparent brick sizes | `review.sh brick-scale`; `brick-scale-gallery.py` |
| Neighbourhood | Terraces, a detailed corner shop and realistic-derived tower; trees, people, cars, paving and furniture; street, neighbourhood, city-distance and detail cameras | `review.sh neighbourhood`; `neighbourhood.py`, `ExpandedStudy.Neighbourhood` |

Galleries and their run manifests live under `artifacts/visual-study/<round>/`. The latest entry is
[the neighbourhood gallery](../artifacts/visual-study/neighbourhood/index.html). Sources and export
manifests live under `art/visual-study/`; runtime assets under `src/Borough.Godot/assets/visual-study/`.
`review.sh open-neighbourhood` opens the interactive review. Author scripts, not generated blends.

This checkpoint also includes the earlier live-shell finish in `Main.Assets`, `Main.Foliage`,
`Main.Travellers`, ground and building shaders. Those are separate from the static review scenes;
the new neighbourhood models have not been substituted into the live simulation renderer.

### What we discovered

- Geometry matters: shader changes on identical boxes did not answer the requested comparison.
  Realistic models were generally liked; the city-builder tower also appealed. Low-poly executions
  were disliked. Voxel was insufficiently cube-dense, not conclusively rejected as a direction.
- Naturalistic construction and warm colours can coexist. Grey naturalistic palettes and smooth
  clay shapes were disliked. House palettes favour slate and earth; all car palettes and both
  preferred tower designs remain available. Quiet vehicles are not a rule.
- The expanded kit remained too cartoonish. The fidelity terrace received strong positive feedback:
  believable construction, material variation and surroundings matter together. Commercial-game
  reference quality remains an aspiration, not an achieved or measured equivalence.
- Materials require export checks and visual inspection. A texture can import yet sample collapsed
  UVs; the active render UV layer needed fixing. Brick size must be checked against openings rather
  than accepted from catalogue metadata. Uniform UV scaling also changes mortar and weathering.
- Direct mesh construction made detailed tower iteration practical; repeated interactive Blender
  operations were a bottleneck. Local Blender CLI works; failed MCP/Serena hooks need not be retried.
- Poly Haven brick and ambientCG asphalt are used with source/licence records in
  `neighbourhood-1/materials.json` and `fidelity-1/research.json`. Both use CC0; cgbookcase textures
  also use CC0. ShareTextures has a custom licence with redistribution/download restrictions;
  it is a candidate resource, not an interchangeable unrestricted CC0 source.
- Captures expose limitations that mesh checks miss: blocked framing, residential curtains inherited
  by shop windows, sparse surroundings and simpler people/trees. The first two were corrected.
  Triangle/surface counts describe assets; they do not measure rendering cost.

### The study reached the city — 2026-09-06

**Everything above this section happened to a specimen.** Nine rounds compared shading languages,
palettes, model designs and authored geometry, and every one of them was rendered in an isolated art
fixture — `KitStudy`, `ExpandedStudy`, `VisualStudy` — that the running game has never loaded. The
plan said so plainly: *the new neighbourhood models have not been substituted into the live
simulation renderer.* Two findings have now crossed that line, and the crossing is the point:
***a treatment nobody can see while playing has not been adopted, however well it was reviewed.***

**The structural reason the crossing is not a substitution.** The study authors buildings at one
size. `neighbourhood.py`'s `shop()` writes a 14 × 12 × 9.65 m corner with its window positions
listed out by hand; `tower()` writes eleven floors and exports 174,492 triangles. `Main.Massing`
draws every Building as a **unit box scaled** to `LotTable`'s footprint and storey count, and
`buildings.gdshader` derives the openings back out of that transform. ***A specimen has a size and a
city has a distribution***, so no study GLB can be dropped in as it stands. The kit is what bridges
them, and it is a separate slice.

**What crossed, and what it cost.** Both are shell-side; no State Hash moves.

| Landed | Where | What it changes |
|---|---|---|
| Photographed masonry | `buildings.gdshader`, `assets/city/` | Poly Haven CC0 brick, sampled per fragment and applied as a **ratio over each Building's own paint**. Costs no instance, no mesh and no draw call |
| Woodland order | `Main.Ground.Scatter` | The Cell walk is sorted by distance from the standing city, so the layer's ceiling drops the furthest crowns rather than an arbitrary band |

⚠ **The masonry is a ratio and not an albedo, and that is the whole reason it can go citywide.**
`Main.Massing.Rendered` spends a Building's scramble on value and warmth so a terrace reads as one
stone weathered differently. A photograph pasted flat over that gives **every masonry Building in the
city the same red** and throws the terrace away — which is exactly what the study's own terrace did,
correctly, because a specimen may borrow a photograph's colour and a city may not. `masonry_hue` is
the dial between the two readings and neither end is right.

**F1 — the city had no trees at all, and the tree layer was full.** Measured on `pictured.toml` at
2,000 Citizens, Tick 4,696: the tree layer stood at **65,536 of 65,536** — its ceiling — and **not
one crown was within a kilometre of the city**. Trees spanned z −16,128 to −11,648 m while the
Buildings stood at z ≈ −19,563 m, so the budget was exhausted three to eight kilometres short of the
city's own latitude and the settlement stood on bare green in every photograph ever taken of it.
⚠ **The ceiling is not the defect and the ORDER is**: whatever the ceiling is, some world exceeds it,
so the only question a scatter answers well is *which* crowns it gives up. Sorted from the city
outward the truncation becomes a horizon instead of a guillotine. Within 1 km of the city centre:
**0 → 1,358**. ⚠ **The anchor is `_foliageBuildings` and not `_paved`** — a single long arterial drags
the Road Graph's centre kilometres into open country.

**F2 — the brick scale in the corpus was a rounding, and the catalogue figure is not a scale at
all.** `fidelity-1/research.json` recorded *about five stretchers across, mapped over 1.2 m*. Counted
on the file: **5 stretchers across and 15 courses down**, which against a 215 mm brick with a 10 mm
perpend and a 65 mm brick with a 10 mm bed is **1.125 m on both axes** — the image is square in the
world as well as in pixels. The pilot counted right and then rounded, and the 6% is the difference
between a 225 mm brick and a 240 mm one. Poly Haven's own **3 m** is the size of the *wall that was
photographed*; read as an instruction it gives bricks the size of a door. ⚠ **None of these is a
CHOSEN apparent brick size** — the brick-scale round compared three and the player selected none, so
`masonry_metres` is the size a brick is and not a size anybody preferred.

**F3 — the sample's mean has to be taken in linear light, and the two differ by the whole transfer
curve.** `masonry_albedo` is declared `source_color`, so a sample is decoded before it is divided
out. The brick image averages **(0.396, 0.290, 0.239)** as stored and **(0.140, 0.078, 0.052)**
decoded; dividing by the stored average leaves every brick wall in the city roughly three times too
bright and wrongly warm. `MASONRY_MEAN` is a property of the file, the command that retakes it is in
`assets/city/materials.json`, and ⚠ **nothing checks that the two agree.**

**F4 — the layer ceiling is pinned by the per-frame rebuild and not by memory, which reorders the
kit against the renderer.** `LayerInstances = 65_536` is one shared `const` across eighteen layers
with no argument behind it, and its own remark concedes `bordered.toml`'s 535,817 Segments exceed it
eightfold. But `Main.Draw` runs from `_Process` — **every frame** — and `Massings(Buildings())` walks
every Building slot and rewrites every instance transform and colour from scratch. ***So the ceiling
is an `O(world)`-per-frame CPU cost wearing a memory constant's clothes***, and raising the `const`
alone buys a frame-rate collapse rather than a bigger city. ⚠ **A modular kit multiplies instances
per Building**, so it makes this worse — which puts incremental, culled drawing **ahead** of the kit
in the queue rather than after it. ⚠ **No timing figure is claimed here and none was taken**
(`adr/0106`, `adr/0121`); this is a statement about what the code does per frame, not about how long
it takes.

**F5 — the photograph tiled visibly, and the generic repair is wrong for brick.** At
`masonry_metres` 1.125 a wall repeats every 1.1 m, and a pale patch in the image becomes a grid of
pale patches marching up the elevation — plainly readable at the distance a player edits at. The
standing repair is stochastic tiling: skew the UV onto a triangular lattice, draw a random offset
per lattice cell, sample three times and blend by the barycentric weights. ⚠ **That technique
assumes a STOCHASTIC texture and brickwork is the opposite of one.** Brick is a lattice with a
running bond, so an arbitrary offset lands a course half a brick out of register and the blend
draws the mortar line twice. The offsets are therefore **snapped to the bond**: whole stretchers
across — the image is five wide, so 1/5 — and **even numbers of courses** down — fifteen tall and
the bond alternates every course, so 2/15. Every draw then lands on a brick that could have been
there, and the three taps have nothing to disagree about. ⚠ **The tap count is 3 for albedo and 1
for the normal map**: relief is bond-aligned across every offset by construction, so blending it
buys a difference nobody can see for a third again as many fetches. The same lattice gives each
Building its own starting brick from `v_custom.a`, so neighbours do not begin the pattern together.
⚠ **Verified by eye and by nothing else** — an A/B from one camera with `masonry_shuffle` at 0 and
1, cropped at the same region; the patch grid is in the first and absent from the second, with no
blend seam and no doubled mortar. ***There is no assertion that a wall does not tile***, and a
screenshot is a spot check (`plans/0048` §7).

**Materials are shipped under `src/Borough.Godot/assets/city/`, separately from `art/visual-study/`,
and the split is deliberate**: a file there is evidence of a comparison already made, and a file here
is drawn every frame. `assets/city/materials.json` carries the author, URL, licence and SHA-256 of
each image, and the SHA-256s match the fidelity pilot's recorded downloads byte for byte.
⚠ **ShareTextures is NOT interchangeable with the CC0 three** — its licence permits commercial use
but restricts redistribution and automated downloading, so a file from it may not be committed to
this tree on the reasoning that admits a Poly Haven one.

### Player review and comparison round — 2026-09-06

The player rejected the flat overlay and plaster treatment, and asked for visible abandonment
wear consistent with the base material. The first live-facade gallery is a rejected/provisional
comparison, not final art. `preferences.json` retains the feedback verbatim. This review takes
priority over spreading the current treatment across more assets.

The [second comparison](../artifacts/visual-study/surfaces-2/index.html) switches among flat fill,
fixed face shading, and face shading with edges on the same live city at two distances. Neutral
surroundings retain shape. `overlay-buildings.gdshader` uses a fixed diagram light independent of
Daylight. This historical gallery predates the roof-crease repair below.
The publisher's [Cities: Skylines II electricity view](https://www.paradoxinteractive.com/games/cities-skylines-ii/features/electricity-water)
is a visual reference for shaded categorical colour and visible context, not evidence about its
shader implementation. [SimCity's manual](https://akamai.cdn.ea.com/eadownloads/u/f/manuals/GAME-SIMCITY/SimCity_2013.pdf)
supplies the task-specific Data Map/system-information reference.

`FacadeMaterials` compares untreated, photographed painted plaster and worn plaster. Abandonment
keeps the Building's paint and layers grime, chipped render revealing the existing masonry, and
worn trim through its explicit condition flag. Photographed normals and roughness replace the
plaster's procedural grain. All maps are Poly Haven CC0; `material-study/sources.json` records
source URLs, byte hashes and linear-light means. Scales and wear strengths remain PROVISIONAL.
The worn-plaster candidate is visibly busy even on occupied premises; it is an alternative, not
an accepted default. This is a facade study: damaged roof geometry and time-evolving wear are unbuilt.

Reproduce with `python3 scripts/compare-surfaces.py` after a Godot Debug build and texture import.
The gallery holds the camera, geometry and paint constant; its manifest identifies source bytes.
The driven overlay runs compare full/incremental geometry and repeated paused uploads. Texture
choice, scale, wear strength and overlay style still need player judgement. A dedicated material
review now has a repeatable comparison surface; none of these samples declares final-stage art.

### Roof review — 2026-09-06

The player rejected the roof art and identified missing overlay lines. `RoofMeshes.Create` supplies
physical crease metadata to `overlay-buildings.gdshader`; coplanar triangulation edges are suppressed.
`Checks.CheckRoofs` covers all three production families. `RoofStudy` and
`scripts/compare-roofs.py` retain the [roof comparison](../artifacts/visual-study/roofs/index.html),
including the original failure, corrected normals, a provisional course-aligned surface, and driven
Age/Rung views. The live checks compare repeated geometry and uploads. Roof construction and
proportions still need review; neither primitive shapes nor procedural tiles are final art.

The follow-up rejected the general roof art despite improved overlays. The
[proportion comparison](../artifacts/visual-study/roof-proportions/index.html) pairs the same city
camera with smaller roof spans, a short-ridge hip, wall-coloured gable ends and photographed slates.
`RoofMaterials` uses Poly Haven's source scale; `material-study/roof-sources.json` records provenance.
`RoofStudy` includes enlarged tiles and human-height references. Footprints remain simulation-owned;
the comparison separates their scale from the former oversized roof volumes. No roof art is accepted.

The player found the proportion comparison better and authorised a checkpoint. Next, ground the
model families in measured architecture: choose a geographic/period reference, study ordinary
building types and their street relationships, then compare a small kit in-game before expanding it.
The footprint-to-occupancy relationship belongs in that review alongside construction and materials.

### Priority and implementation pass — 2026-09-06

1. Repair roof outlines and compare roof surfaces alongside materials and layered wear before propagating the treatment.
2. Keep repeatable camera evidence and renderer assertions, then choose projected-size detail and
   update cadence from observed failures. UI refresh can proceed independently of asset authoring.
3. Review the neighbourhood with the player before propagating its construction across the roster
   and modular kit. Repetition, moving-camera and lighting studies accompany that work.
4. Profile before occlusion, GPU-driven rendering or worker preparation; their gates remain.

`FacadeAppearance`, `Main.Massing` and `buildings.gdshader` implement the first group provisionally.
Commercial Goods storage in the Building kind selects a shopfront, even while vacant; it does not
promise stock or opening hours. Household and Business tenants both contribute to occupancy.
Abandonment has its own flag, independent of paint. Tower shafts suppress shopfronts. Debug washes
use the existing unshaded override; hip and mansard roofs now receive it alongside gables. Plaster keeps the stable
material-family draw and adds restrained surface variation.

`FacadeStudy` and `scripts/check-facades.py` retain the [review gallery](../artifacts/visual-study/live-facades/index.html):
four occupancy/condition samples across two materials, residential/commercial fronts and day/night/
debug wash, plus the live shopping city. The captures exposed disabled mipmaps on both masonry
images; their import settings now enable them. Brick relief no longer crosses onto glazing.
The script checks repeated paused draws for unchanged geometry and uploads. Renderer checks cover
mixed-tenancy occupancy, explicit condition, upper-part flags and GPU custom-data packing.
A separate driven shopping run compared incremental and full regeneration through Ticks 600–664.
These are behavioural and visual checks, not a cost measurement or player acceptance.
Live frontage/storey edits, continuous camera motion and broader lighting review remain open.

### Repetition beyond the tile — 2026-09-07

**F5 repaired the material and the walls still read as repetitive, because repetition has four
scales and F5 owns one of them.** A survey of what other city games and open-world city renderers
ship separates the four cleanly, and the separation is the finding: ***the repair for one scale does
nothing for the other three.***

| Scale | What repeats | The published repair | Where this build stands |
|---|---|---|---|
| **Material** — a metre | the photograph, every `masonry_metres` | stochastic tiling (Heitz & Neyret, HPG 2018); the practical form is Mikkelsen's hex-tiling (JCGT 2022), which restores the contrast plain blending loses | **landed** — `masonry_grid`, bond-snapped, **F5** |
| **Instance** — a Building | one wall against its neighbour's | per-instance colour masks; a per-instance offset into an atlas of facades | **landed in part** — `Main.Massing.Rendered`'s paint, and `v_custom.a`'s starting brick |
| **Composition** — a facade | the window grid itself | authored modular parts; a varied bay, a differentiated ground storey, a cornice and a string course | **unbuilt** — and **F6** is what is identical today |
| **Depth** — a pane | a flat wall reads as wallpaper however varied its colour | interior mapping (Oliveira & Policarpo 2006) | **unbuilt** |

**F6 — every window in the city is the same window, at the same height, on the same pitch.** Read
off `buildings.gdshader` against `Main.StoreyMetres`, on every wall the massing builds from a storey
count. `Main.Massing` writes a height of `storeys * StoreyMetres` exactly and the shader recovers the
count exactly, so `lift` is **3.5 m citywide, without exception**. `paneH = min(1.6, lift * 0.52)` is
then `min(1.6, 1.82)` = **1.6 m**; `sill = (lift - paneH) * 0.62` = **1.178 m**; and
`paneW = min(1.15, wide * 0.42)` is **1.15 m** for every bay wider than 2.74 m, which `BAY` all but
guarantees because `wide = span / floor(span / BAY)` is never below **3.6 m**. ⚠ **So the only thing
that varies across the whole city's glazing is the horizontal spacing** — `jamb = (wide - paneW) * 0.5`
— and it is a function of the footprint alone. ***Two Buildings on one footprint have one facade***,
but for which panes the occupancy draw shuts and which bay takes the door.

⚠ **F6 IS A READING OF THE CODE AND NOT OF A PICTURE.** It establishes what is identical; it does not
establish that identity is what the eye reports, and the two are different claims. Which scale
dominates is *measurable* under `adr/0043` — the A/B F5 already ran for `masonry_shuffle`, repeated
once per scale — and it has not been run. **No figure in this section is a measurement of this
build.**

**What other teams ship, and which of it is cheap here.**

- **Cities: Skylines**, both games, spends the variety budget on **authoring** rather than on the
  sampler: three colour masks per asset, mapped to mutually exclusive regions and multiplied over the
  diffuse, with the colours drawn per instance from the asset's own list. CS2's building pipeline
  permits a facade to tile outside 0–1 **only on the condition that the image carry no repetitive
  visual element** — the repetition is authored out of the source rather than blended out at the
  fragment.
  [buildings](https://cs2.paradoxwikis.com/index.php?title=Asset_Pipeline%3A_Buildings),
  [surfaces](https://cs2.paradoxwikis.com/Asset_Pipeline:_Surfaces),
  [asset creation](https://cslmodding.info/asset/building/)
- **SimCity 2013** textured some 900 building models through one shader that **overlays two UV regions
  from a single atlas**, each independently offset to space windows and detail elements. It is the
  nearest published thing to what this build derives procedurally from the transform.
  [GDC 2013](https://gdcvault.com/play/1017823/Building-SimCity-Art-in-the),
  [pipeline](https://eugenewong.artstation.com/projects/dD5Ae)
- **Interior mapping** — raycast into a virtual box behind the glass and sample a room from a cube or
  an atlas. Shipped in SimCity 2013, GTA V, Watch_Dogs, BioShock Infinite, Spider-Man PS4 and Forza
  Horizon. ⚠ **It costs no instance, no mesh and no draw call**, which is the property that let the
  masonry cross into the city at all. Against **F6** it is the strongest single item here, and for a
  reason worth stating plainly: ***it does not change the grid, it makes every cell of the grid
  different.***
  [technique](https://www.gamedeveloper.com/programming/interior-mapping-rendering-real-rooms-without-geometry),
  [implementation](https://halisavakis.com/my-take-on-shaders-interior-mapping/)
- **Macro variation** — very-low-frequency noise multiplied over albedo at 50–200 m — is the standard
  landscape repair and is **weak here**. The largest wall is tens of metres, so at that scale it
  degenerates into a per-instance tint, and `Rendered` already draws one.
  [stochastic texturing](https://unity.com/blog/engine-platform/procedural-stochastic-texturing-in-unity),
  [hex-tiling](https://jcgt.org/published/0011/03/05/paper-lowres.pdf),
  [histogram-preserving blending](https://eheitzresearch.wordpress.com/722-2/)

⚠ **These are what other teams shipped and none of them is evidence about this build's picture.** The
standing sentence in *What we discovered* is unchanged: commercial-game reference quality remains an
aspiration, not an achieved or measured equivalence.

### The three scales, built and photographed — 2026-09-07

**Built in the order composition → depth → measurement, and the plan's own ordering claim was wrong.**
The item above said the measurement gated the other two. It cannot: F5's A/B worked because
`masonry_shuffle` was a dial that could be set to 0, and neither composition nor depth had one.
***There is nothing to turn off until it is built.*** What the measurement gates is ACCEPTANCE.

**F7 — the two dials exist, they are drivable, and each buys something at a different distance.**
`pictured.toml`, seed 0, 2,000 Citizens, Tick 2,731 — Day 1, Tuesday, 13:00, 176 Buildings — one
camera per distance, three legs differing in **nothing but the environment**.

| Leg | `BOROUGH_FACADE_VARIETY` | `BOROUGH_INTERIOR_DEPTH` |
|---|---|---|
| A | 0 | 0 — the facade **F6** measured |
| B | 1 | 0 — composition alone |
| C | 1 | 2.6 m — both |

| Distance | `scale` | What the comparison shows |
|---|---|---|
| **45 m**, tilt 12° | — | **Composition is plain.** The bay differs Building to Building, the ground storey reads as a ground storey, and the string course and cornice give the elevation a count and a top. **Depth reads as depth** once the room's faces have contrast between them |
| **130 m** | **40.6 / 39.2 px a Tile** | Composition is visible and modest. ⚠ **Depth makes no difference the eye reports at all** |
| **420 m** — where a player edits | — | Composition still separates one Building's grid from its neighbour's, and the cornice is the mark that survives. ⚠ **What dominates at this distance is roof colour, massing and footprint**, none of which this touches |

🔴 **THE ROOM'S FACES NEED CONTRAST BETWEEN THEM AND THE FIRST VERSION HAD NONE.** Every face was
within a factor of two of every other, and the whole effect read at 45 m as ***the windows went
darker*** rather than as depth — a correct trace, a correct perspective, and no legibility. What
makes a room read through a window is that its ceiling catches light and its floor does not, so
***the effect is the SPREAD between the faces and not the average of them.*** The ceiling is now
7× the floor.

⚠ **SO INTERIOR MAPPING IS A NEAR-CAMERA MARK AND THE PLAN SAID SO BEFORE IT WAS BUILT** — *a room
nobody can resolve is a fetch nobody sees.* The measurement puts the line between 45 m and 130 m and
does not narrow it further. ⚠ **AND THE TRACE IS NOT BRANCHED.** `room_on` masks the RESULT at
distance; the slab test runs at every fragment of every wall regardless. **No timing figure was
taken and none is claimed** (`adr/0106`, `adr/0121`); this is a statement about what the code does
per fragment. It belongs with the profiling item below, not ahead of it.

⚠ **VERIFIED BY EYE, FROM SIX CAPTURES, AND BY NOTHING ELSE** — which is exactly what F5 says about
itself. ***There is still no assertion that a wall does not repeat***, and a screenshot is a spot
check (`plans/0048` §7). What did change is that the comparison is now **re-runnable from one
binary**: `Main.FacadeDial` reads all three dials from the environment, so a leg is a command line
rather than a source edit.

**F8 — a per-Building offset cannot break a per-Building repeat, and the corner is where that shows.**
F5's bond-snapped offset is drawn once per Building, so it separates a house from its neighbour and
does **nothing** for the two walls of one house meeting at a corner — which is the one place two
walls are seen at once. Two of the four faces now **mirror** the coordinate, chosen off the outward
normal so it is a property of the wall rather than a draw. ⚠ **The bond survives a mirror**: a
running bond is symmetric about a vertical line, so the courses stay level, the perpends stay plumb,
and what changes is which bricks are burnt — F5's own statement of what the eye locks onto.
🔴 **The relief's x flips with it or the brick lights from the wrong side**, which is precisely the
failure this file's tangent-frame remark is about. **Checked on a mirrored face at 60 m**: courses
level, no doubled mortar, no inverted embossing.

**The slab trace is branched, and the branch is safe for a reason worth stating.** Nothing inside it
takes a derivative — no texture fetch, no `dFdx`; `grain` was taken outside — so the block may be
skipped without the undefined mip level a fetch in divergent control flow would get. ⚠ **What it
skips is a whole warp and not a fragment**: the distant wall, the shuttered Building, the shell with
no glass in it. A pane at the edge of the fade shares its warp with wall and pays anyway.
***A branch on a smoothly-varying mask buys the extremes and not the middle.*** ⚠ **No timing figure
was taken and none is claimed** (`adr/0106`).

🔴 **F9 — WHAT A NIGHT FRAME VERIFIES IS NOT THE DAYTIME READ, AND IT WAS NOT RUN FOR ONE.** It
answers two questions daylight cannot ask at all.

1. **Whether the room composes with the lit-window emission.** `night_phase` lights panes from an
   emission that knows nothing about a box behind the glass, and the two had never met.
2. **Whether the changed opening arithmetic leaks at a seam.** ⚠ **This file has already had that
   defect and its own comment says night is where it was reported** — a quarter-open pane is grey on
   grey by day and a quarter of a lamp on black at night. **F6's repair moved every opening's size
   and position**, so it is the cheapest check that the new arithmetic does not leak.

**Both answers, on `pictured.toml` at Tick 3,542 — Day 1, 22:30 — one camera, `interior_depth` 0
against 4:**

- ✅ **No seam leaked.** No hairline of light at any bay or storey boundary, on any wall in frame.
- 🔴 **The room is invisible at night in BOTH directions.** A lit pane's emission swamps it entirely;
  an unlit pane is crushed to black by the dim before any face of the box can be told from another.
  ***So `interior_depth` is a daylight mark, and at night it is a trace that buys nothing*** —
  `room_on` does not carry `night_phase` and could. ⚠ **Not done here**, because gating on night
  would foreclose the lit room the emission ought eventually to show; it is a choice and not a
  cleanup.

⚠ **AND AN OBSERVATION THIS SECTION CANNOT EXPLAIN.** The two night captures differ in the **ground**
— one shows a pale road and pavement, the other a dark road with a single lamp pool — at the same
Tick, the same camera and the same Ruleset. `buildings.gdshader` draws neither, so it is not this
change; **it is recorded rather than explained, and nobody has looked.**

⚠ **The night pair does NOT discharge the daylight/evening/night box below.** It is one hour, one
camera, and street lighting was not the subject.

### Live work list

Update these boxes here as work lands; record player judgements in `preferences.json`.

- [x] Establish editable assets, export/import validation and retained comparison galleries.
- [x] Explore geometry and palettes; produce the positively reviewed fidelity terrace.
- [x] Compare brick scale without replacing the geometry or texture images.
- [x] Assemble and inspect the static neighbourhood at multiple distances.
- [ ] Review the neighbourhood with the player; record strengths and weak executions before
  propagating the treatment. No single tower design or brick size must win by default.
- [ ] Refine people, tree branching/foliage, window/interior variation, blank end elevations,
  roof/site detail and the building-to-ground junctions identified by review. **Window and interior
  variation is now specified** by the two composition and depth items below; the rest stands.
- [ ] Apply the accepted construction/material workflow across the remaining agreed roster in
  `comparison-kit.json`; retain genuine differences between building families and earlier anchors.
- [ ] Test deliberate block/city repetition with controlled variations in massing, material and
  brick scale. The current city-distance image is a small corner viewed farther away. ⚠ **Vary one
  scale at a time** — *Repetition beyond the tile* names four, and a block that varies all of them
  together reports which picture was preferred and never which change bought it.
- [ ] Capture a repeatable moving camera path; inspect shimmer, transparent surfaces, shadows and
  detail transitions. Add appropriate LODs and texture mip/filter choices based on visible failures.
- [ ] Compare ordinary daylight, evening and night with the same scene; establish window and street
  lighting without using attractive lighting to hide geometry defects.
- [~] Integrate approved assets/materials into the live shell and verify occupied/partly occupied/
  abandoned appearances, overlays on/off and frontage/storey changes against actual state.
  **Started 2026-09-06** — the masonry crossed and the woodland order was fixed; the state
  responses now have controlled facade samples and live overlay checks above; live frontage/storey
  edits and broader state-transition coverage remain open.
- [~] **The shopfront and the render family** — implemented, then reopened by player review above.
  Commercial premises select the frontage; the stable material draw selects brick or plaster.
  Visual acceptance remains with the player.
- [x] **Stop the masonry repeating** — bond-snapped stochastic tiling in `buildings.gdshader`,
  landed 2026-09-06. **F5** owns the finding and the reason the generic technique needed changing.
  ⚠ **It is the MATERIAL scale and only that one.** The other three scales repetition has are in
  *Repetition beyond the tile* above; closing this box did not close them.
- [x] **Measure which scale the repetition is at** — done 2026-09-07, three legs at three distances,
  **F7** owns the readings. ⚠ **The ordering claim this item used to make was wrong** and F7 says why:
  an A/B needs a dial, and a dial does not exist until the variation does. What it gates is player
  ACCEPTANCE, which is still open. ⚠ **Still no assertion that a wall does not repeat.**
- [x] **Stop one Building's four walls sharing a phase** — two of the four faces mirror the masonry
  coordinate, with the relief's x flipped to match, landed 2026-09-07. **F8** owns it. ⚠ **This is
  the INSTANCE scale and F5's offset never reached it** — that offset is per Building, and a corner
  is two walls of one Building.
- [x] **Vary the facade's composition** — landed 2026-09-07 behind `facade_variety`, whose zero end is
  bit-for-bit the facade **F6** measured. The bay is drawn per Building from a 3.0–5.2 m band and
  fitted to the span; pane width, pane height and sill each take their own independent draw; the
  ground storey drops its sill clear of the plinth and grows the opening upward; and the existing
  cornice gained a shadow while a string course marks every storey line above the first. ⚠ **The
  storey COUNT and the pitch are untouched** — a Building three storeys tall still has three rows of
  windows, and the exact recovery F6 rests on is unchanged. No Ruleset data, no State Hash movement.
  ⚠ **Every band and share here is PROVISIONAL and none was chosen against a reference.**
- [x] **Interior mapping behind the glazing** — landed 2026-09-07 behind `interior_depth`, whose zero
  end is the flat pane. A slab trace against a box whose front face is the window's own aperture,
  resolved in the tangent frame the relief already builds; four flat faces, a per-window room draw,
  no texture and therefore no asset and no licence record. **F7** owns what it buys and where it
  stops buying it; the trace is branched and the night frame is looked at, both in **F8**/**F9**.
  ⚠ **What F9 leaves open is a CHOICE and not a gap**: the room is invisible at night in both
  directions, so the trace buys nothing after dark, and whether `room_on` should carry `night_phase`
  depends on whether a lit room is wanted later.
- [x] **Remove the fixed instance ceiling and retain spatial batches** — implemented in
  [`0066`](0066-retained-spatial-rendering.md), which owns verification and the remaining renderer
  costs. The paused camera-sorted prototype is superseded by that implementation.
- [ ] **Zoom-dependent rendering detail and update cadence** — requested by the player 2026-09-06;
  owned by [`0066`](0066-retained-spatial-rendering.md). Use projected screen size to simplify distant
  geometry, materials, shadows and Traveller presentation; refresh distant visual state less often
  where the delay is imperceptible. Bound that delay, refresh when approaching or inspecting, and
  use hysteresis to prevent detail levels oscillating. Camera distance must never reduce simulation
  updates or change the State Hash. Verify a repeatable near-to-city camera path for popping,
  stale state and legibility, and compare CPU update/upload cost and GPU cost at each scale.
  Thresholds and cadences remain PROVISIONAL until measured.
- [ ] **Spatial picking** — query nearby Chunks before testing individual geometry for hover or
  selection. Preserve nearest-hit and tie-breaking behaviour; verify picking after edits and
  detail transitions. The batch query is implemented; `0066` owns verification and remaining costs.
- [ ] **Event-driven UI refresh** — update panels when their inputs change and throttle aggregate
  readouts, avoiding per-frame string construction and full-world summaries. Selection and player
  actions must refresh promptly; bounded display delays must not conceal stale information.
- [ ] **Bounded streaming and uploads** — budget geometry preparation and uploads per frame,
  prioritising visible areas. Exercise rapid zooms and camera jumps; inspect stalls and missing
  geometry as well as average frame cost. Thresholds remain PROVISIONAL until measured.
- [ ] **Renderer regression checks** — retain repeatable camera paths and counters for rebuilds,
  uploads, allocations and visible batches. Assert that an unchanged paused scene performs no
  geometry rebuilds or uploads; keep performance captures separate from behavioural assertions.
- [ ] **Occlusion culling — PROFILE BEFORE IMPLEMENTING.** Pursue only if captures show geometry
  hidden behind other geometry costs enough to justify occluder maintenance and culling overhead.
  Compare against the spatial batching and distance-detail baseline, including shallow views.
- [ ] **GPU-driven rendering — PROFILE BEFORE IMPLEMENTING.** Pursue only if CPU visibility,
  submission or instance preparation remains limiting after retained batching. Establish renderer
  compatibility and a fallback before investing in a GPU-managed visibility and draw pipeline.
- [ ] **Multithreaded geometry preparation — PROFILE BEFORE IMPLEMENTING.** Pursue only if geometry
  preparation still causes material CPU cost or stalls after incremental updates. Workers must read
  immutable snapshots; discard stale results and apply them through a bounded upload path that
  respects Godot's thread restrictions. Measure snapshot, scheduling and synchronisation costs too.
- [ ] **The modular kit** — the study's construction re-authored as parts snapped to a Lot's frontage
  and storey count rather than a building modelled at one size, with an LOD chain. ⚠ **Gated on the
  incremental, culled renderer item above.**
- [ ] Measure baseline versus candidate frame/update/shadow costs and memory on a quiet machine,
  recording renderer, resolution, scene and camera path. Optimise the limiting work, then remeasure.
- [ ] Consolidate the reusable visual brief, material library and export/LOD conventions after
  review and measurements. Plan 0063 stays open until the outstanding visual/state/cost checks land.

### Validation and restart

The neighbourhood run manifest checks fixed scene/lighting across its cameras, distinct new asset
geometry, imported bounds, normals/materials, separated building envelopes and preserved anchors.
Its six captures were visually inspected. Godot Debug build and the targeted Corpus checks passed
before this checkpoint. The commit also runs the ordinary assertion gate; its retained test log
owns the exact result. No frame-budget or production-scale performance claim is made.

Capture commands require the installed Blender CLI and graphical Godot; `BLENDER_BIN` and
`GODOT_BIN` overrides are supported. Build Godot in Debug before captures. `models`, `expanded`,
`construction`, `fidelity` and later rounds retain historical manifest hashes: changing a helper
later does not retroactively validate an earlier capture against the new source. Keep historical
captures/manifests together; write a fresh run when rebuilding. Application caches and downloaded
third-party reference pages are disposable; authored review evidence is retained.

## Original brief and chronological round notes

Started 2026-09-04. Asset path verified; player agreed the expanded kit and specimen-first sequence.
Child of [0049](0049-visuals.md), following the visual finish pass. This is the implementation plan
for the discussion in [visual-direction](../published-artifacts/visual-direction.md).

**The risk to retire: choosing an art direction we cannot reliably produce, or choosing a rendering
style because we preferred the architecture in its example.** `FAST ITERATION`, `LEGIBLE CAUSE`.

**Historical scope amendment — model round 1:** the player requested genuinely different model designs:
realistic, SC2013-inspired city-builder, crisp voxel and angular low-poly. This supersedes the exact
composition constraint below for this round only. Keep specimen role, approximate scale, warm
palette and cameras comparable; vary silhouettes, proportions, massing and construction. Earlier
rounds remain intact. Naturalistic detail was liked, its grey palette was not; rounded clay miniature
was rejected. `art/visual-study/preferences.json` owns the working feedback. No final choice.
`art/visual-study/next-session-prompt.md` is the requested resume prompt.

## 1. What we are doing

Build a representative kit and render each specimen in three deliberately different treatments.
The first comparison holds setting, architectural family, layout, Building identity, frontage,
storeys, occupancy, camera, light and population constant. Detail geometry and materials may vary;
functional envelope and architectural composition may not.

The setting is a contemporary city containing inherited buildings. Near-future architecture remains
an option for a later comparison within the selected treatment. Era, architectural character, visual
treatment and the distribution of detail across viewing distances are independent choices.

| Treatment | Shape and construction | Surface and colour |
|---|---|---|
| Material-rich | Detailed reveals, frames, roof edges and junctions | Restrained brick/plaster/metal/glass variation, roughness and subtle normal detail |
| Sculpted | Clean, strongly readable volumes; deliberate depth at openings and edges | Broad material regions, controlled colour, minimal fine texture |
| Middle | Selective depth where entrances, silhouettes and use benefit | Quiet surfaces with concentrated detail at focal features |

All three receive comparable finishing effort. Sculpted is not an unfinished material-rich model.
Begin with the same palette and exposure; any later palette or lighting experiment is a separately
labelled comparison. Judge ordinary noon views before allowing sunset or photographic blur to hide
weaknesses. Treatment names describe experiments, not a ranking.

**Agreed comparison kit:** `art/visual-study/comparison-kit.json` owns the specimen roster and
sequence. Low-density housing: detached house and attached townhouse. Medium-density housing:
walk-up and courtyard apartments. Towers: residential, office and mixed-use. Commerce: small and
corner shops. Industry: factory with attached office. Civic: school. Scale figures: car, bus and
walker. Streets, courtyard and tree forms provide shared context.

**Work specimen by specimen across all three treatments**, beginning with a detached house,
residential tower and car. Review that trio before expanding the families. Individual views expose
construction; an assembled street and repeated neighbourhood test coherence and repetition. A bus
or mixed-use tower here is an art specimen, not a claim that its simulation is implemented.

The original small block is retained as the pipeline experiment. It no longer stands for the
whole comparison. The player authorised the first treatment while reviewing layout and then
agreed this broader kit; that agreement permits the initial trio without another scope gate.

Use real Lot dimensions and a fixed Input Log where possible. If an isolated art-review scene is
needed to arrange the specimen, label it as such: it demonstrates rendering, never simulation
behaviour. State-response checks must also run in the live shell. A school or shop must read from
its entrance, ground floor and composition; a sign alone is insufficient.

This work covers appearance and its production path. It does not change capacity, siting, road
layout, driving progress or simulation Fidelity. The urban-form work in [0062](0062-the-urban-fabric.md)
is separate: freeze the specimen's geometry/version rather than absorbing new form changes during
an A/B comparison. Novel functional infrastructure and a player-facing style selector come later.

## 2. Tools and ownership

**Blender authors reusable parts; Godot assembles and renders the city.** Use Blender's bundled
Python to create/edit meshes, assign material slots, unwrap or bake where needed, and export glTF
2.0 binary (`.glb`). Keep authoring sources outside Godot's resource directory. Exported meshes go
inside it. Godot recommends glTF; direct `.blend` import itself invokes Blender to convert to glTF.
Explicit export makes the shipped asset independent of a local Blender installation.
[Godot import documentation](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html).

Blender node materials are not a portable shader language. Use export-supported materials and baked
textures where appropriate; implement occupancy, condition and other live responses in Godot.
Review final appearance in Godot, including its lighting, colour handling and import settings.

Planned homes, created during implementation:

- `art/visual-study/`: editable `.blend` sources and a small provenance/version manifest.
- `scripts/art/`: generation, validation and export scripts; source of truth for scripted assets.
- `src/Borough.Godot/assets/visual-study/`: exported `.glb` files and texture assets.
- Godot review scene and treatment definitions under `src/Borough.Godot/`, using shared mesh
  resources and explicit material slots. A treatment is a named renderer preset.
- `artifacts/visual-study/`: reproducible captures, a comparison gallery and run manifests.

Choose one authoring authority per asset: a scripted model is changed through its script; a manually
edited model uses its saved `.blend` as source. Rebuilding must not silently overwrite manual work.
Record application versions, units, axes, origins, material slots and export options. Use metres;
verify orientation and ground contact after import. Retain editable source and exported assets;
exclude disposable application caches. Rebuildability means equivalent geometry/materials, not a
promise that exported binary bytes are identical.

Godot, the C# toolchain and Blender are installed. `art/visual-study/facade.json` records the
authoring version. The MCP connection was unavailable; background Blender supplied the asset path. No paid modelling,
texturing or image-to-3D service is required for this experiment. Additional tools are introduced only
for a demonstrated need. Concept images may guide discussion but do not count as rendered outputs.

The assistant owns tool setup, modelling scripts, meshes, materials, export/import, integration,
captures and checks. The player supplies art direction and judges the pictures; modelling or
programming knowledge is not a prerequisite.

## 3. Sequence and outputs

| Task | Reviewable output | Completion condition |
|---|---|---|
| 1. Prove the asset path | One reusable facade module, editable source, export script and Godot import | A requested dimension/material edit survives regeneration and reimport with correct scale, normals and ground contact |
| 2. Define the kit | Agreed roster, specimen geometry and fixed cameras | Shared composition is held across each comparison; no final style choice required |
| 3. Produce the treatments | Each specimen across all three presets, then assembled and repeated contexts | Each visibly follows its stated treatment; no changes to the city's facts or architectural family |
| 4. Compare in motion and state | Labelled gallery, short camera-path clips, live-shell state checks and cost report | No material import failures, obvious flicker, geometry gaps or broken state readings; costs and limitations are visible |
| 5. Select and consolidate | Player's preferred treatment or specified combination, rejected qualities and next-kit scope | A short reusable visual brief and retained source assets can guide the next Building family |

**PROVISIONAL capture matrix:** three treatments × three distances (city, street, close photograph)
× three light conditions (noon, evening, night): 27 comparable stills. Derive camera distances from
the block's actual size and record viewport, transforms, Tick and exposure. Include one identical
camera path per treatment to reveal aliasing and detail transitions. Compare side by side or by a
labelled switch, without changing the camera. Keep the current renderer as a baseline.

Additional live-shell checks: occupied, partly occupied and abandoned Buildings; an overlay enabled
and then removed; a changed frontage/storey configuration. These use actual state or isolated,
explicitly labelled fixtures, not paint chosen to suggest a state the city does not hold.

The cost report compares baseline and treatments on the same installed renderer, machine, resolution,
world and camera path. Record frame time, CPU geometry/update cost, GPU/shadow cost where available,
visible instances, triangles, material passes and memory. Mark unavailable counters. Record actual
Citizens and Buildings separately. A repeated neighbourhood is a screening test, not proof of
million-Citizen performance. Collect timings with the machine quiet; no invented frame budget or
claim of target-scale readiness.

## 4. What success looks like

1. **A meaningful choice:** the player can name the qualities to retain and reject from matched
   images. If all three feel interchangeable or all are rejected, revise the treatment definitions
   or execution; do not declare a winner by default.
2. **A place at the playing camera:** homes, shops and the school can be distinguished without UI
   labels; the repeated block reads as a neighbourhood rather than an obvious repeated stamp.
3. **Detail survives use:** the selected look remains appealing in ordinary noon light, close views
   and motion, with no dependence on blur or sunset. Geometry sits on the ground and joins cleanly.
4. **The city remains legible:** occupancy/abandonment and overlays retain their meaning. Cosmetic
   finish adds no false wear, service or infrastructure claims. Artist choices do not affect State
   Hashes; replay/save checks apply if integration touches their boundary.
5. **We can make the next asset:** regenerate/export/import the specimen from retained sources and
   demonstrate a requested edit without manual repair in Godot. Shared modules fit the tested
   frontage widths and storey counts without visibly stretched doors or windows.
6. **The cost is reviewable:** disclose the measured cost against baseline and the limiting pass.
   A visually preferred but expensive treatment may be selected provisionally with a concrete
   optimisation experiment; production acceptance waits for that experiment. “Uses instancing”
   alone is not performance evidence.

Use visual review for aesthetic judgements and automated checks for objective asset/import contracts.
Run `dotnet build src/Borough.Godot`, the relevant assertion lane and driven shell checks after
integration. The ordinary milestone obligations still apply to any later milestone claim.

## 5. How the player helps

Review the blockout and the matched gallery. Optional references are welcome, especially an ordinary
street or building and one sentence identifying the appealing feature. Feedback can be plain:
“the materials are too noisy”, “keep that window depth”, “these trees feel plastic”. Name at least
one disliked quality as well as favourites. The assistant turns those observations into changes.

No final era decision is needed to begin. After selecting a treatment, compare contemporary and
near-future architecture within it. Preserve the other treatments as review artefacts rather than
quietly substituting one for another.

## 6. Implementation handoff

Task 1 is verified by `scripts/art/review.sh verify`: a width/material edit survives export and
Godot import, then the baseline is restored and checked. `VisualStudy.ValidateFacade` checks scale,
ground contact, front orientation, normals and material names/colour. An sRGB/linear mismatch was
found and corrected in `scripts/art/facade.py`. The script owns the asset; edit it rather than the
generated `.blend`.

The original task 2 blockout remains a retained experiment; the agreed kit above supersedes its scope. `VisualStudy.tscn` is an isolated art fixture, with
provisional dimensions rather than a live Lot capture. `art/visual-study/specimen.json` owns its
brief. Run `scripts/art/review.sh capture` for the gallery at `artifacts/visual-study/index.html`,
or `scripts/art/review.sh open` for the four keyboard-selectable cameras. `run.json` records source
hashes and capture settings. The repeated view deliberately copies the specimen unchanged.

Next: compare the first trio across all treatments, revise weak executions, then extend the roster.
Live-shell state responses, baseline comparisons, motion, night views and cost measurements remain
tasks 3–4; the blockout proves none of them.

Validation: `dotnet build src/Borough.Godot`, `scripts/art/review.sh verify`, the four Godot captures,
and `scripts/test.sh -- --no-restore` passed. Captures and logs are local, regenerable artefacts.

The player authorised starting the first test while layout review is pending. The material-rich
architectural pass is available through `scripts/art/review.sh material-rich`; it writes matched
views to `artifacts/visual-study/material-rich.html`. `facade.py --treatment material-rich` owns
its separate Blender source and GLB, with embedded masonry normal/roughness textures.
`VisualStudy.Materials` adds door joinery and roof edges without changing the fixture's layout.
Joining wall faces stay unbevelled: bevels there exposed every module boundary in the close view.
This is a first architectural test, not task 3 completion: trees and scale figures retain their
blockout shapes, and motion, live state checks and rendering costs remain outstanding.

`KitStudy.tscn` renders the first trio exported by `scripts/art/kit.py`. The export manifest checks
common core composition across variants. `scripts/art/review.sh kit` rebuilds, imports, captures
and validates nine matched noon views; `open-kit` opens the viewer. The gallery is
`artifacts/visual-study/kit/index.html`. These are initial executions, not final style candidates
or completion of the full kit.

Working preferences from the first trio live in `art/visual-study/preferences.json`: material-rich
house and tower, middle car. The player asked to keep exploring, including stronger extremes;
none is a final selection. `review.sh extremes` retains those assets as comparison anchors and
exports separate detailed/sculpted extremes. The sculpted extreme explicitly changes the shading
model; the detailed extreme increases masonry variation. `extremes-gallery.py` checks composition
against the original kit and matches cameras and lighting in whole and close views.

The player also liked the extra-detailed house and car, so quieter vehicles are a hypothesis,
not a rule. `preferences.json` keeps both candidates. The next requested comparison changes art
language: painterly, graphic, miniature and naturalistic. `review.sh styles` creates separate
assets and a gallery at `artifacts/visual-study/styles/index.html`, with matched whole/close cameras.
`KitStudy.Styles` owns palettes and material response; the gallery names the changed shading,
miniature bevels, sun softness and ambient occlusion. Painterly marks are procedural and naturalistic
glazing remains opaque. These are initial executions, with no final style selection.

`review.sh models` produces the geometry-led first round at `artifacts/visual-study/models/index.html`.
`model-round.py` owns twelve separate models and `models-gallery.py` checks distinct geometry,
scale envelopes and matched whole/close capture controls. The warm palette is shared. These are
initial interpretations. The player liked the realistic trio and city-builder tower; low-poly was
disliked and voxel construction was less cube-dense than expected. No final direction selected. The requested fresh-session prompt is `art/visual-study/next-session-prompt.md`.

The palette concern is compared through `review.sh palettes`: preferred model GLBs stay fixed while
`art/visual-study/palettes.json` supplies original, warm-slate and earth-terracotta colours through
`KitStudy.Palettes`. Gallery: `artifacts/visual-study/palettes/index.html`; `palettes-gallery.py`
checks geometry and capture controls against model round 1. Only albedo varies. No palette selected.

`review.sh expanded` extends the agreed roster through `expanded-kit.py`, using the realistic
construction direction and approved palette range. `ExpandedStudy` reads `expanded-layout.json`
for street/courtyard and repeated housing fixtures; the gallery is `artifacts/visual-study/expanded/`.
Earlier GLBs remain the anchors. New assets await feedback; motion, night, live state and cost remain open.

The player found the expanded pass slightly cartoonish and asked for a balance closer to realism.
`review.sh construction` compares three revised specimens through `construction-study.py`, using
the preserved expanded cameras. `artifacts/visual-study/construction/` keeps the before/after views;
this pilot awaits feedback before propagation. The palette preferences stand.

The player likes the structure and colours as a first cut but requests higher fidelity and reference
research. `review.sh fidelity` tests one terrace through `fidelity-study.py`; its comparison and
attributed research live in `artifacts/visual-study/fidelity/` and `art/visual-study/fidelity-1/research.json`.
This is a capability pilot, not the end state or approval to propagate it across the roster.

The fidelity pilot was positively reviewed. The player authorised a complete street-corner sample;
`review.sh neighbourhood` and `neighbourhood.py` own the new assets and six-view gallery.
`art/visual-study/neighbourhood-1/materials.json` records imported materials and licences.
This static scene awaits feedback; motion, night, live responses and measured costs remain open.
