# Simulation and appearance audit

**The recorded sample is reproducible; it is not a new run of HEAD.** Recalculation confirms the plan's 52 components, separate 28 m / 22 m axis medians, and 7–10.5 m wall heights. The full sample contains 52 distinct Building IDs, each appearing once in the wall layer, and the companion readout reports 52 Buildings. This particular sample is therefore one solid wall component per Building. That relationship must not be assumed for other scenes.

## Provenance and reproduction

- Visual checkpoint: `d9addc3` on `codex/visual-treatment-followups`.
- Working tree inspected: `7916db4022066a741531087aa1ed6325a448303f`, branch `main`; initially clean.
- Input: `artifacts/visual-study/roof-proportions/daylight-near.tsv`, companion repeated TSV, `.txt`, `.drive` and `.png`, read directly from the checkpoint with `git show`.
- Fixture: `shopping.toml`, Tick 600, 400 Citizens. The fixture header says shopping demonstration, synthetic shop supply and opening independent of staffing; it is not a balance sample.
- Stored camera commands: pause at 600; focus `(88,92)` at distance 160; tilt 32; overlay none. The stored absolute capture paths belonged to the earlier worktree.
- TSV SHA-256: `918824632344f4cadf63bab85017be95b15b26f3f128d142511e45705f21aa32`.
- The two recorded draw files compare byte-for-byte equal. This is a check of the archived pair, not evidence of a newly replayed run.

From repository root:

```sh
python3 research/city-architecture/output/audit_snapshot.py
python3 research/city-architecture/output/build_bundle.py
```

The first recalculates geometry, capacities and roof angles. The second also regenerates the atlas, tables and scale drawing. Neither runs Godot or the simulation. See [all 52 rows](evidence/simulation-sample.csv), [summary](evidence/simulation-summary.json) and [checkpoint file hashes](evidence/reproduction.json). The compact comparison uses the first four draw indices plus index 7, the first maximum-area component: IDs 1,2,3,4,8. No selection by visual attractiveness.

The checkpoint-to-HEAD diff of `Main.Massing.cs` changes Health overlay colouring, not these dimensions. `BuildingPlan.cs` and `shopping.toml` are unchanged across those revisions. This supports a **geometry/content interpretation**, not a claim that current simulation execution would reproduce the same population and Building inventory. Current schedules and other mechanisms have changed.

## Owners and units

| Quantity | Owning symbols / files | Interpretation |
|---|---|---|
| Parcel and footprint | `LotTable.Parcel*`, `Footprint*`; `BlockPatterns` / `BuildingPlan` in Core | Parcel is held ground; footprint is ground covered by the Building. Axis-aligned dimensions need not be street frontage/depth |
| Storeys / occupied floor | `LotTable.Storeys`, `FloorTiles`; `BuildingPlan.FloorTiles`, `HabitableTiles`, `Tower` | Floor area includes the form's hollow/tower rules; do not use outer box for every pattern |
| Draw dimensions | `Main.Massing.Buildings`, `Wings`; `Main.cs` | Four metres per Tile; 3.5 metres per drawn storey; walls use a unit BoxMesh |
| Roofs | `Main.Massing.CapFor`, `RoofHeight`, `CapBasis`; `RoofMeshes.Source` | Gable, short-ridge hip, paired gable; flat case has no separate roof instance |
| Window/door geometry | `buildings.gdshader` | Nominal bay 3.6 m; actual bay divides wall width into an integer count; window caps 1.15 × 1.6 m; principal door selection, shopfront logic |
| Tenancies | `World.TryDeclaredOccupancy`; `CapacityRuleset.Holds` | Shared capacity eligible for Households and Businesses. Not a count of Citizens |
| Jobs | `World.TryDeclaredJobs` | A premised Business receives its share of floor divided by the jobs rate; not all Building floor again |
| Fixture rates | `rulesets/shopping.toml` `[capacity]` | 25 floor Tiles per tenancy; 3 per job; 12 per parking space; unchanged |
| Recorded rows | `Main.Channels.DrawList`; `Main.Rendering` / instance IDs | Uploaded local mesh transform scale; distinguish axis-aligned wall from rotated roof basis |

One square Tile corresponds to **16 m²**, not 4 m². The fixture's tenancy denominator is therefore 400 m² of counted floor. The jobs denominator is 48 m² within each tenancy's share. These are content conversions, not empirical occupancy standards. Both fixture Building kinds allow housing and premises; whether a particular tenancy is occupied or used commercially is not inferred by this audit.

## Reconstructed sample

Dimensions below are east–west × south–north, **not automatically frontage × depth**. Capacity is the computed ceiling, not actual residents or workers.

| Building ID | Footprint m | Wall m / storeys | Counted floor m² | Tenancies | Jobs per premised Business |
|---|---:|---:|---:|---:|---:|
| 1 | 36 × 20 | 7 / 2 | 1,440 | 3 | 10 |
| 2 | 32 × 24 | 10.5 / 3 | 2,304 | 5 | 9 |
| 3 | 24 × 16 | 10.5 / 3 | 1,152 | 2 | 12 |
| 4 | 52 × 24 | 7 / 2 | 2,496 | 6 | 8 |
| 8 | 28 × 60 | 7 / 2 | 3,360 | 8 | 8 |

For these solid, ground-founded bodies: `floorTiles = width_m × depth_m × storeys / 16`; tenancies = `max(1, floorTiles // 25)`; jobs per Business = `max(1, (floorTiles // tenancies) // 3)`. Zero-rate/no-floor cases are excluded by this fixture and must retain their source semantics elsewhere.

Across all 52 rows: footprint areas range **320–1,680 m²**, median 640 m²; reconstructed tenancy capacities 2–11. The independent axis maxima of 60 m do not imply a 60 × 60 m Building. Likewise 28 × 22 m is not a chosen “median building”. Roof layers contain nine gables, one hip and 42 paired gables.

For G003, the roof basis has 16.7 m across the eaves and 4.125 m rise: `atan(4.125 / 8.35) ≈ 26.3°`. The .35 m eaves make its angle differ slightly from calculation on the 16 m wall span. The TSV rounds to three decimals; angle precision is not greater than its inputs. For paired gables the roof mesh halves the local X span; the audit applies that division. A hip's different slope planes cannot be reduced to one universal pitch; the CSV angle is its cross-span plane only.

## Findings by responsibility

**Appearance:** the shader has no complete dwelling/access plan; regular windows alone can make a large mixed Building resemble one enlarged house. Roof selection by squareness and size cannot establish construction type. Paired roofs create valleys without a corresponding resolved drainage assembly. `Flat` supplies no separate cap assembly. The shed is explicitly a shell invention, not another addressed Building; its placement needs checking against yard access and parcel boundaries. These are model/geometry/material questions.

**Simulation or content:** G003's counted 1,152 m² permits two shared tenancies, whereas M3's proposed layout tests 12 dwellings. That is a material content question, not permission to paint twelve occupied homes onto two tenancies. No measured reference establishes that the current rate is wrong, but ordinary dwelling areas in the atlas make it a specific question to resolve. The four-metre Tile and footprint/subdivision system also cannot directly express every proposed 4.8 m house as its own Lot. Drawing subdivisions may be feasible for a multi-tenancy Building; representing each as a separately addressed house needs an explicit decision.

**Missing evidence / preference:** the audit is one demonstration at one Tick, not the city's distribution. Most historic reference footprints remain unmeasured; material texel size was not measured from the screenshot. The oversized-house diagnosis is supported by scale and access evidence, but its perceptual contribution versus shading awaits the matched prototype.

Courtyard Buildings can draw four wings sharing an ID; towers can draw podium and shaft. Count floor only once through Core's form, then decide whether a component is a wing or whole building. Keep allocated ground and sealing intact: a proposed yard carved out of a solid footprint is not an appearance-only correction.
