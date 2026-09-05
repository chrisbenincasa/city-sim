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

### Live work list

Update these boxes here as work lands; record player judgements in `preferences.json`.

- [x] Establish editable assets, export/import validation and retained comparison galleries.
- [x] Explore geometry and palettes; produce the positively reviewed fidelity terrace.
- [x] Compare brick scale without replacing the geometry or texture images.
- [x] Assemble and inspect the static neighbourhood at multiple distances.
- [ ] Review the neighbourhood with the player; record strengths and weak executions before
  propagating the treatment. No single tower design or brick size must win by default.
- [ ] Refine people, tree branching/foliage, window/interior variation, blank end elevations,
  roof/site detail and the building-to-ground junctions identified by review.
- [ ] Apply the accepted construction/material workflow across the remaining agreed roster in
  `comparison-kit.json`; retain genuine differences between building families and earlier anchors.
- [ ] Test deliberate block/city repetition with controlled variations in massing, material and
  brick scale. The current city-distance image is a small corner viewed farther away.
- [ ] Capture a repeatable moving camera path; inspect shimmer, transparent surfaces, shadows and
  detail transitions. Add appropriate LODs and texture mip/filter choices based on visible failures.
- [ ] Compare ordinary daylight, evening and night with the same scene; establish window and street
  lighting without using attractive lighting to hide geometry defects.
- [ ] Integrate approved assets/materials into the live shell and verify occupied/partly occupied/
  abandoned appearances, overlays on/off and frontage/storey changes against actual state.
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
