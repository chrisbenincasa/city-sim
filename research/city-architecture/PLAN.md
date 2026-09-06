# Research plan: believable city architecture

## Purpose

Establish an evidence-backed basis for city-sim's building models: credible scale, construction,
variation and street relationships, with the colourful material direction the player likes.

The central question is: **what real building types could plausibly occupy the game's footprints,
serve their uses and capacities, and form convincing streets?**

The research must explain how buildings are put together, not just collect attractive facades.

## Starting point

Visual checkpoint: `d9addc3`, on `codex/visual-treatment-followups`.

The player likes the increasingly detailed buildings and warm/slate palettes. Flat plaster,
flat overlays and the original roof forms were rejected. Roof outlines and the subsequent
proportion comparison were judged better; no final architectural style has been accepted.

The checkpoint replaces truncated-pyramid roofs with smaller parallel spans on broad buildings,
adds short-ridge hips and wall-coloured gable ends, and compares photographed roofing materials.
These are provisional experiments to investigate, not architectural constraints to defend.

The shopping fixture at Tick 600 with 400 Citizens drew 52 building components, with separate
median width/depth of 28 m and 22 m, and wall heights of 7–10.5 m. Some dimensions reached 60 m.
These are measurements of that fixture's rendered components, not a distribution of real houses,
not necessarily unique Buildings, and not a citywide conclusion. Reproduce before relying on them.

Relevant repository entry points, relative to the repository root:

- `plans/0045-amnesty.md`: cold-start repository instructions.
- `art/visual-study/preferences.json`: player feedback and its standing.
- `plans/0063-the-visual-treatment-comparison.md`: visual comparison history and current questions.
- `artifacts/visual-study/roof-proportions/index.html`: latest roof comparison.
- `artifacts/visual-study/neighbourhood/`: earlier detailed street samples.
- `docs/07-the-drawing.md`: drawing principles; `CONTEXT.md`: authoritative domain vocabulary.
- `src/Borough.Godot/Main.Massing.cs`, `RoofMeshes.cs`, `RoofMaterials.cs`, and
  `buildings.gdshader`: current appearance implementation.

Read relevant sections as needed; this is not an instruction to consume the entire corpus.

## Research sequence

### 1. Establish a coherent reference setting

Compare two or three plausible geographic directions and their building traditions. Consider
climate, construction materials, block patterns, roof forms and periods of development. Explain
how each fits the existing preferred art and the game's mix of uses.

Recommend one starting context with a few compatible building eras, including later infill.
Seek the player's preference early if they are available. Continue collecting evidence for the
alternatives while waiting; if no preference is supplied, finish with an explicitly provisional
recommendation. Do not silently turn it into a globally applicable style.

Deliverable: a compact illustrated comparison and a reasoned recommendation.

### 2. Build a measured atlas of ordinary buildings

Start with six families: detached houses, terraces, corner shops with homes above, small apartment
blocks, courtyard blocks, and workshops/light industry. Adjust the list if the chosen context
requires it, explaining why. Prefer roughly three independent examples per family; report gaps
rather than padding the sample with copies or unsupported measurements.

For each example collect:

- Place, date/period, use and a source identifier.
- Street, rear and roof views where available; plan, elevation or section where available.
- Footprint, plot dimensions, storeys, floor-to-floor heights, setbacks and attached neighbours.
- Entrance locations, facade bays, window dimensions and spacing, stairs/access arrangement.
- Roof type, spans, rise/pitch, eaves, ridges, valleys, drainage and wall/roof junctions.
- Material module sizes, construction details, maintenance and plausible forms of deterioration.
- Documented dwelling or use capacity when available. Distinguish dwellings, households, people
  and floor area; do not infer occupancy from a window count.

Select ordinary streets and service sides as well as notable examples. Include recent buildings
and alterations so a heritage archive does not accidentally define the whole city.

### 3. Audit scale against the simulation

Trace current footprints, height and capacity to their owning data/code. Determine whether a
rendered component represents a house, an attached range, part of a block or a whole complex.
Compare a small reproducible game sample against the atlas using consistent units.

Separate findings into:

- Appearance problems that shell geometry/materials can address.
- Footprint, subdivision, use or capacity questions requiring simulation/content decisions.
- Missing evidence or unresolved art preferences.

Do not hide a mismatch by shrinking the picture away from simulation-owned ground. Do not assume
every large footprint is wrong: establish what architecture and access would make it plausible.
Do not change gameplay or Rulesets during this research stage.

### 4. Derive construction rules for reusable models

Translate examples into bounded, explainable model parameters: facade bays, structural spans,
storeys, entrances, roof assemblies, courtyards, attachments and material modules.

Explain what changes when a building gets wider or deeper. Identify incompatible combinations
and when a different family is needed. Avoid treating independent random dimensions as believable
architectural variation. Distinguish observed ranges from provisional simplifications for the game.

Account for instancing and reuse. Separate silhouette geometry, close-range construction detail,
material detail and condition changes. Performance assumptions remain hypotheses until measured.

### 5. Specify the prototype comparison

Prepare model briefs for a terrace, corner mixed-use building and small apartment block, or justify
better first candidates from the atlas. Show scaled footprint/elevation comparisons against the
current game forms, with human and material-module references. Use dimension-derived drawings or
simple blockouts; generated imagery is not evidence of real dimensions or construction.

Specify a short street that exercises corners, party walls, entrances, backs, roof junctions and
repetition. Review untextured massing before materials, then occupied/abandoned appearance.

The later playable prototype should be compared at street, neighbourhood and city distances,
with a moving camera, daylight/night and overlays. Keep camera and lighting matched when isolating
one change. The research session supplies the evidence and model briefs; building a production
asset library is follow-on work, not a condition for completing this research.

## Sources and evidence rules

Prefer measured surveys, published plans/sections, municipal character studies, architectural
inventories, manufacturers' technical drawings and documented projects. Use photographs to
corroborate geometry and relationships. Treat perspective-derived dimensions as estimates and
state the method and uncertainty.

Useful starting collections, not a substitute for selecting relevant examples:

- [Library of Congress HABS/HAER/HALS](https://www.loc.gov/pictures/collection/HH/): measured
  drawings, photographs and historical documentation, especially for American directions.
- [Historic England building-type guides](https://historicengland.org.uk/listing/selection-criteria/listing-selection/):
  histories and examples of building types, especially for English directions.
- Local planning/character studies and contemporary project documentation for the selected setting.
- Material suppliers' dimensions and laying details; [Poly Haven](https://polyhaven.com/) for
  candidate asset maps whose physical scale and license can be checked.

Cite the exact page, drawing sheet or figure supporting each important measurement. Label values
`documented`, `estimated`, or `proposed`; include units and preserve relevant caveats. Record
access dates and distinguish a source's original claim from an inference. Track image/asset rights;
link to material that cannot be redistributed instead of assuming public access permits reuse.
A texture library establishes material samples, not building proportions.

## Required output

Write the research bundle under `output/`:

| File or directory | Purpose |
|---|---|
| `REPORT.md` | Recommendation, findings, limitations and the next model experiment |
| `atlas/index.html` | Illustrated examples grouped by family, with source links and identifiers |
| `dimensions.csv` | One measurement per row: example/source IDs, quantity, value/range, units, evidence status, method and caveat |
| `sources.csv` | Source ID, title, publisher/author, exact URL, sheet/page, access date and reuse standing |
| `simulation-audit.md` | Reproduction details, code/data owners and mismatches separated by responsibility |
| `model-briefs.md` | Three initial model specifications, supported ranges, exclusions and provisional choices |
| `scale-comparison.svg` or `.html` | Dimension-derived comparison of reference and current game forms |

Keep the report concise and put detailed evidence in the atlas and tables. Missing dimensions
remain missing; completeness must not be manufactured. A researcher without repository access
can finish the architecture work and explicitly mark the simulation audit as unverified.

## Completion and next review

The research is ready for review when:

- A starting setting is recommended without implying unreceived player approval.
- The selected families have traceable examples and useful dimensional evidence or explicit gaps.
- Scale claims distinguish real buildings, game Buildings and rendered components.
- Roof construction, entrances and street relationships have been studied alongside facades.
- The proposed three models can be built from the briefs without inventing every proportion anew.
- A visual scale comparison tests the explanation for the current oversized-house appearance.
- Remaining questions and the next bounded model experiment are explicit.

After review: build the small untextured street, resolve its proportions, then run the dedicated
material/aging comparison. Expand the reusable kit only after those comparisons support it.
Keep findings here; do not create ADRs, alter simulation state or weaken repository checks to
complete this research. Leave work uncommitted unless the user requests a commit.
