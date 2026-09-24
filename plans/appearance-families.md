# Appearance Family format and picking rule

## Outcome

A Style Preset is a folder of TOML files under `appearance/<preset>/`. Each file declares
Appearance Families and the Building facts each one admits. The shell and the headless runner
pick one family per Building from those facts, the same way on every run. The shell does not yet
draw family geometry. A debug wash shows which family each Building would draw.

## Decisions

| Decision | Reason |
|---|---|
| Family files name the kinds they draw; the Ruleset names no family | Content stays free of drawing concerns, and one Ruleset can take several presets |
| Only facts fixed when a Building is raised choose its family | Kind, frontage, depth, storeys, block pattern, zone and the Day it was raised. Land value and other live readings never repaint a standing Building (`adr/0173`) |
| A weighted draw picks among the eligible families, with no specificity ranking | Authors control frequency directly. A narrower family does not silently win |
| A kind's fallback draws only what no other family admits | A missing fallback shows up as magenta in the wash and as `missing` in the coverage report |
| Attached Buildings on one block face in one era share a draw | The seed is the Segment id, the street side and the era. Later infill on the same face draws afresh |
| `era_days = 30` in `test-street` is provisional | Tune it once real terraces are drawn |
| The preset lives in `Borough.Appearance`; `PurposeTag.AppearanceFamily = 59` | Shell-side, with no Godot reference. The draw never touches a Tick or the State Hash |

## Pieces

| Piece | Where |
|---|---|
| Facts, reader, picker | `src/Borough.Appearance/BuildingFacts.cs`, `StylePresetReader.cs`, `FamilyPicker.cs` |
| Test preset | `appearance/test-street/preset.toml`: the six test-street bodies, the two-unit apartments study, and a `box` fallback for every shipped kind |
| Schema | `appearance/appearance.schema.json`, associated in `.taplo.toml`; `StylePresetSchemaTests` keeps it equal to the reader's key sets |
| Coverage report | `dotnet run --project src/Borough.Headless -- --ruleset R --appearance DIR` |
| Debug wash | `overlay family` in the shell; `--appearance DIR` picks the preset, default `appearance/test-street` |

## Acceptance

- The reader refuses each mistake at its file and line. Covered by `StylePresetTests`.
- The shell and the headless report agree. On `shopping.toml` at 400 Citizens, seed 0, Tick 600,
  both give corridor-apartments 9, walkup-apartments 1, two-unit-apartments 1, office-warehouse 6 and box 35.
- The wash is driven and photographed: `plans/evidence/appearance-families/family-wash.drive`
  and its two captures, taken on `zeus` on 2026-09-24.

## What the demonstration showed

- 33 of 50 dwellings fall back to `box`. The test street covers only a few size bands, so this
  is the coverage report doing its job.
- No shopfront or workplace kind matches a test-street family on this fixture.
- The wash colours the roof with the body, so the family reads from above at 260 m.

## Generated bodies

A family's `[family.body]` table says how it builds its body at any size it admits:
- the bay width
- the bays of each wall's ground and upper storeys, where a `*` token fills the spare bays
- opening sizes for window, door and stair bays, where the builder's own do not fit
- a parapet or an 18° or steeper gable, pilasters, rooftop plant, vents, a roof hatch, a chimney
  and front steps
- the authored model whose materials dress it

Spare bays split evenly among the `*` tokens. A remainder goes in pairs to the outermost tokens and
a last odd bay to the middle one, so `["window*", "hall*", "window*"]` keeps its entrance central
at any bay count. Neighbouring `hall` bays share one door. A ground door with a stair window above
it gets a small canopy.

A rowhouses Building is one house. The shell probes half a metre outside the middle of each side
wall; where another Building's footprint covers the probe, that wall is a party wall. A party wall
is blank, the gable stops at it rather than overhanging, and a half-thickness upstand rises above
it, so two neighbours make one whole upstand. `rulesets/rowhouses.toml` is the fixture: perimeter
blocks carved into 8 × 12 m house plots with no setback. Its three denser bands admit only zone
bit 1, which nothing builds on, so the middle of the city stays empty.

`FamilyBodyBuilder` ports the vocabulary of `scripts/art/test-street.py`. The shell draws the result
in place of the massing with `ui family-bodies on`.

| Check | Result |
|---|---|
| Office-warehouse at 36 × 20 m against its Blender body | Same bounds: ±18.2 m, 11.4 m to the street canopy, 12.2 m to the receiving canopy, 8.5 m to the plant |
| Live city, `shopping.toml`, 400 Citizens, Tick 600 | Five generated office-warehouse bodies at 32×24, 32×20, 36×16, 32×20 and 36×24 m; captures in `plans/evidence/appearance-families/w1-body-*`, named before the rename |
| Walls hold whole bays | 32 m gives five 6.4 m bays and 36 m gives six 6 m bays |
| Corridor apartments at 24 × 16 m against the Blender body | Same bounds: ±12.5 m to the end-door canopies, 9.2 m to the entrance canopy, 8.06 m to the back sills, 11.4 m to the vents |
| Live city, `shopping.toml`, 1,000 Citizens, Tick 600 | Nine generated corridor bodies at 20, 24 and 28 m. The Blender body stands at Tile 68 116 and a generated 24 × 16 m one at Tile 2084 116; captures in `plans/evidence/appearance-families/a1-body-*` from `a1-body.drive` |
| Four attached 6 × 12 m rowhouses against the Blender row | Same bounds: ±12.2 m to the free eaves, 7.0 m to the front steps, 6.4 m to the back eaves, 10.05 m to the chimneys |
| Live city, `rowhouses.toml`, 1,000 Citizens, Tick 600 | 54 generated rowhouses, all 8 × 12 m and three storeys, in two terraces of 27 on one block's north and south faces. 50 share both side walls, and each terrace has one free end each side. Captures in `plans/evidence/appearance-families/h1-body-*` from `h1-body.drive`; the Blender row is rendered by `blender-render.py` |
| Rowhouse side-by-side | Upstands, chimneys, steps, doors and the windowed free end match. Each house has one window size, so the narrow window over the Blender door is as wide as the other. End walls take the wall siding, not the darker end siding |
| Corridor side-by-side | Walls, openings, canopies and roof hatch match. The two vents stand 4 m either side of the centre, where the Blender body puts them at 8 m. The ground windows sit 5 cm lower, and the back door is 10 cm taller |

Limits of this slice:
- A body is drawn only with no overlay showing, because its materials take no wash or paint.
- A Building raised after the toggle gets no foliage footprint for its body until the next full pass.
- One mesh per Building, with no chunking or far level yet.
- Scuppers, downpipes and roof crickets are left for the minor-details pass.
- A body reads its neighbours only when it is placed, so a neighbour raised later leaves the shared
  wall windowed until the next full pass. Each placement scans every live Building; unmeasured.
- Where a terrace meets the Building on the next face at a block corner, the two roofs run into
  each other. Corners are not resolved.
- The rowhouse fixture's perimeter houses are all three storeys, since a perimeter block adds one
  storey to `house_storeys`. The family admits two and three.

## Next

- Bodies for the other test-street families: walkup-apartments, two-unit-apartments, corner-shops-with-flats and workshop.
- Resolve block corners, and refresh a body when its neighbour is raised or removed.
- Wash and paint for bodies, so overlays work with bodies on.
- Chunked upload and the far level, measured against the 6 ms Building frame share.
- Author the rest of the families. The coverage report says which size bands and kinds need them.
