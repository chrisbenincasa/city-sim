# Procedural Buildings — REPORT §3 Measurements

Taken 2026-09-23 for [REPORT §3](../../../research/procedural-buildings/REPORT.md#3-measurements-the-design-needs).
Optimisation leads found along the way are in
[`optimizations.md`](../../../research/procedural-buildings/optimizations.md).

## Conditions

| | |
|---|---|
| Machine | `zeus`: i5-10400 (6 cores, 12 threads), GTX 1080 8 GB, driver 580.178.04, 62 GB RAM. `machine.json` |
| Governor | `powersave` |
| Load | Not quiet. Load average 1.6–2.3 during every run from other sessions, so every figure is an upper bound |
| Build | Release. Godot 4.7.2 mono, Vulkan Forward+, window 2560×1371, VSync off, frame limit 0 |
| Generator | Prototype `Borough.Appearance.ShellBuilder` on branch `procedural-buildings`, uncommitted at capture |
| Material | Shells use a plain `StandardMaterial3D` with vertex colour. Boxes use `buildings.gdshader` |
| Kit | 15 placeholder boxes per shelled Building, one MultiMesh per chunk. No real kit exists |

## 1. Generation time (`generation/`)

BenchmarkDotNet 0.15.8, in-process toolchain, .NET 10.0.12. One worker, arrays reused.
Reproduce with `dotnet run --project tests/Borough.Tests -c Release -- --filter '*ShellBuilder*' --inProcess`.

| Building | Wings | Vertices | One thread | Per Building, 1,000 across 12 threads |
|---|---|---:|---:|---:|
| House, 8×10 m, 2 storeys, gable | 1 | 726 | 7.8 µs | 1.3 µs |
| Terrace, 6×12 m, 3 storeys, parapet | 1 | 1,172 | 12.6 µs | 4.8 µs |
| Courtyard, 5 storeys, flat | 4 | 13,160 | 137.8 µs | 22.4 µs |
| Podium and 20-storey tower | 2 | 20,016 | 196.4 µs | 34.1 µs |

- Cost is about 10 ns per vertex on one thread. Steady-state generation allocates nothing.
- Generation is not a constraint on the 0.5 s appearance target.

## 2–4. Upload, near-band frame cost and shadows (`frames/`)

World: `stress-shopping.toml`, 1,000,000 Citizens, seed 0, paused at Tick 600 (12:01, Day 0),
86,924 Buildings. The city is low-rise. Its densest 250 m cell averages about three storeys.
Reproduce with `scripts/measure-shell-band.py --output DIR` after a Release build of the shell.
Each row is the median of 8 one-second samples after a 4 s warm-up.

| View | Eye distance from focus | Tilt |
|---|---|---|
| Opening | 25,293 m, whole city | 35° |
| Street | 150 m, focus Tile (1781, 1656) | 35° |
| District | 600 m, same focus | 35° |

Radius is the distance from the eye to a wing's centre. Chunks are 64 m unless stated.

| View | Case | Shelled Buildings | Vertices | FPS | Frame ms | GPU ms | Render CPU ms |
|---|---|---:|---:|---:|---:|---:|---:|
| Opening | Boxes | 0 | 0 | 12.9 | 77.7 | 20.6 | 54.6 |
| Opening | Boxes, sun shadows off | 0 | 0 | 12.8 | 77.9 | 16.7 | 54.8 |
| Street | Boxes | 0 | 0 | 116.5 | 8.58 | 5.86 | 0.83 |
| Street | Boxes, sun shadows off | 0 | 0 | 122.0 | 8.19 | 3.83 | 0.50 |
| Street | Shells to 150 m | 25 | 86,058 | 116.6 | 8.58 | 5.34 | 0.84 |
| Street | Shells to 250 m | 82 | 276,674 | 115.6 | 8.65 | 4.92 | 0.88 |
| Street | Shells to 500 m | 327 | 1,096,992 | 113.2 | 8.83 | 4.59 | 0.98 |
| Street | 250 m, shells cast no shadow | 82 | 276,674 | 116.0 | 8.62 | 4.73 | 0.85 |
| Street | 500 m, shells cast no shadow | 327 | 1,096,992 | 114.8 | 8.71 | 4.32 | 0.88 |
| Street | 500 m, sun shadows off | 327 | 1,096,992 | 120.7 | 8.28 | 3.00 | 0.56 |
| Street | 250 m, one node per Building | 82 | 276,674 | 112.4 | 8.90 | 4.84 | 0.97 |
| Street | 250 m, no kit | 82 | 276,674 | 114.4 | 8.74 | 4.89 | 0.86 |
| District | Boxes | 0 | 0 | 70.6 | 14.16 | 8.50 | 4.61 |
| District | Boxes, sun shadows off | 0 | 0 | 83.7 | 11.95 | 6.07 | 2.96 |
| District | Shells to 500 m | 174 | 593,002 | 69.1 | 14.47 | 8.42 | 4.85 |
| District | 500 m, shells cast no shadow | 174 | 593,002 | 70.7 | 14.15 | 8.28 | 4.62 |
| District | 500 m, sun shadows off | 174 | 593,002 | 83.1 | 12.03 | 5.95 | 3.05 |

Main-thread upload, from the same runs (`ArrayMesh.AddSurfaceFromArrays`, node creation and kit MultiMesh):

| Case | Buildings | Chunks | Upload ms | Per Building | Per vertex | Slowest chunk |
|---|---:|---:|---:|---:|---:|---:|
| Street, 150 m | 25 | 13 | 13.4 | 0.54 ms | 156 ns | 7.38 ms (first upload) |
| Street, 250 m | 82 | 44 | 22.2 | 0.27 ms | 80 ns | 0.91 ms |
| Street, 250 m, one node per Building | 82 | 82 | 26.3 | 0.32 ms | 95 ns | 0.80 ms |
| Street, 500 m | 327 | 175 | 96.9 | 0.30 ms | 88 ns | 3.04 ms |
| District, 500 m | 174 | 96 | 46.6 | 0.27 ms | 79 ns | 0.87 ms |

## Findings

| # | Finding |
|---|---|
| 1 | Generation costs about 10 ns per vertex on one thread, 8 µs for a house and 196 µs for a tower |
| 2 | Upload costs about 80–90 ns per vertex on the main thread, or 0.3 ms per Building in this world. A 2 ms per-frame budget admits about 7 Buildings a frame. A single redevelopment appears on the next frame. A full 500 m band (97 ms) needs about 50 frames at that budget, so a fast camera move can outrun it |
| 3 | The first upload costs 7 ms. It is most likely the shell material's pipeline compiling, which argues for warming the material at load |
| 4 | Shells out to 500 m add at most 0.25 ms per frame at the street view and 0.3 ms at the district view. GPU time falls because the plain shell material is cheaper than the box shader. Shell geometry is not the constraint in this low-rise world, but the comparison does not yet include a production façade material |
| 5 | Sun shadows cost 2.0 ms GPU at the street view and 2.4 ms at the district view. Shells casting shadows add 0.1–0.3 ms GPU at 500 m. This world does not reproduce CS2's 40 ms shadow cost |
| 6 | One node per chunk uploads 16% faster than one node per Building and draws 0.25 ms faster at 250 m |
| 7 | The opening camera runs at 12.9 fps with boxes alone: 54.6 ms render CPU and 20.6 ms GPU. The 60 fps target at the default camera fails by about 5× before the generator adds anything. See `optimizations.md` #3 |
| 8 | A 250 m near band is empty whenever the eye is more than about 250 m from every wing, as in the district view. Band distances therefore interact with how low the camera goes |

## Limits

- The world has no towers, so tall shells were benchmarked but not drawn.
- The 15 placeholder kit pieces are single boxes. Real kit meshes carry more vertices and materials.
- The shell material is not the production façade material, so GPU comparisons between shells and boxes are indicative only.
- Each case ran once on a machine that was not quiet.

## Opening-camera profile (`opening/`)

Same world, machine and build. Load average 0.9–3.2, so still upper bounds. `chunk-N/` runs the
opening suite with `BOROUGH_RENDER_CHUNK_METRES=N`. `band-chunk-1024/` reruns the full band suite
at 1024 m. Reproduce with `scripts/measure-shell-band.py --suite opening --chunk-metres N --output DIR`.
Draw calls are Godot's visible-pass count for the last frame.

| Case | Draws, 256 m | Render CPU ms | GPU ms | Draws, 1024 m | Render CPU ms | GPU ms | FPS, 1024 m |
|---|---:|---:|---:|---:|---:|---:|---:|
| Everything | 32,184 | 55.2 | 20.7 | 2,694 | 3.4 | 14.7 | 67.4 |
| Sun shadows off | 32,184 | 54.3 | 16.8 | 2,694 | 3.1 | 10.9 | 83.8 |
| Trees and rocks hidden | 32,184 | 54.3 | 20.6 | 2,694 | 2.7 | 14.6 | 68.0 |
| Streets hidden | 18,665 | 32.5 | 15.3 | 1,714 | 1.6 | 11.5 | 85.8 |
| Buildings hidden | 14,862 | 20.5 | 10.8 | 1,259 | 1.1 | 8.5 | 113.8 |
| Ground layers hidden | 32,038 | 50.5 | 21.1 | 2,656 | 2.6 | 14.5 | 68.3 |
| All instance layers hidden | 1,197 | 1.0 | 4.7 | 241 | 0.3 | 4.5 | 135.3 |

512 m chunks give 9,374 draws, 13.9 ms render CPU and 39.6 fps.

| View | FPS, 256 m | FPS, 1024 m | GPU ms, 256 → 1024 | Render CPU ms, 256 → 1024 |
|---|---:|---:|---|---|
| Opening, boxes | 12.9 | 67.3 | 20.6 → 14.6 | 54.6 → 2.8 |
| Street, boxes | 116.5 | 127.9 | 5.86 → 6.15 | 0.83 → 0.36 |
| Street, shells to 500 m | 113.2 | 126.9 | 4.59 → 4.88 | 0.98 → 0.55 |
| District, boxes | 70.6 | 105.2 | 8.50 → 9.25 | 4.61 → 0.74 |

### Findings

| # | Finding |
|---|---|
| 9 | Render CPU at the opening camera tracks draw calls at about 1.7 µs each. 256 m chunks give 32,184 draws. Every instance layer keeps every chunk resident, so the whole city is one draw per chunk per layer |
| 10 | Trees and rocks cost nothing at the opening camera. Their `DetailDistance` already empties far chunks. Building layers (body, four roof families, yards) cost the most: 17,322 draws and 35 ms. Streets come next: 13,519 draws and 23 ms |
| 11 | 1024 m chunks cut draws 12× and raise the opening camera to 67 fps, which meets the 60 fps target. The street and district views also get faster. Coarser culling costs 0.3–0.7 ms more GPU there. 1024 m is the default after this profile |
| 12 | At 1024 m the opening camera is GPU-bound at 14.6 ms, drawing 5.5M primitives. Building layers account for 3.5M of them. That leaves about 2 ms of GPU headroom for the far band's real materials and landmark pieces |
| 13 | Godot reports zero shadow-pass objects even while sun shadows cost 3.8–4 ms of GPU. The shadow counter does not appear to cover directional shadows, so shadow cost is only visible by toggling |
| 14 | A Building edit re-uploads its whole chunk. At 1024 m that is about 540 Buildings in the densest part of this city, or roughly 43 KB per layer, well inside the 8 MB per-frame allowance |

## Moving city (`moving/`)

Same world, machine and Release build, taken 2026-09-23 on a free GPU (`other_gpu_users` empty in
both manifests). Load average 1.3–4.0, so still upper bounds. Each view is paused, then the clock is
set to 1× and 4×. Reproduce with `scripts/measure-shell-band.py --suite moving --chunk-metres N --output DIR`.
Uploads and queries are totals over each 8 s window; the other columns are medians of one-second samples.

| View | Clock | FPS, 256 m | FPS, 1024 m | Render CPU ms, 256 → 1024 | Mover uploads, 256 → 1024 | Mover upload ms, 256 → 1024 | Movement queries, 256 → 1024 |
|---|---|---:|---:|---|---|---|---|
| Opening | Paused | 12.6 | 67.5 | 55.0 → 2.8 | – | – | – |
| Opening | 1× | 12.2 | 65.1 | 57.3 → 3.4 | 0 → 0 | – | 0 → 0 |
| Opening | 4× | 12.0 | 63.5 | 56.9 → 3.3 | 0 → 0 | – | 0 → 0 |
| Street | Paused | 109.1 | 126.4 | 0.86 → 0.37 | – | – | – |
| Street | 1× | 125.7 | 134.7 | 1.08 → 0.49 | 1,001 → 445 | 8.7 → 5.4 | 1,274 → 2,399 |
| Street | 4× | 126.7 | 135.2 | 1.08 → 0.46 | 1,611 → 454 | 11.3 → 6.4 | 2,482 → 3,830 |
| District | Paused | 71.5 | 106.2 | 4.56 → 0.65 | – | – | – |
| District | 1× | 81.4 | 87.7 | 5.78 → 0.82 | 11,685 → 2,858 | 83.2 → 66.3 | 30,596 → 92,020 |
| District | 4× | 67.5 | 94.1 | 5.93 → 0.81 | 16,272 → 2,301 | 122.1 → 50.3 | 47,347 → 47,745 |

An earlier pair of runs overlapped another session's 1M-Citizen Godot capture on the same GPU. Its
GPU times were 5–10 ms high and it was discarded. The script now records other GPU users.

### Findings

| # | Finding |
|---|---|
| 15 | 1024 m chunks hold with the city moving. The opening camera keeps 64–65 fps at 1× and 4×, against 12 fps at 256 m. Street and district views stay above 87 fps, faster than 256 m at every clock setting |
| 16 | Larger chunks upload Traveller and car instances less often: 2–5× fewer chunk uploads and 20–60% less main-thread upload time per window. Each upload is bigger, but the total cost falls |
| 17 | The movement index tests 1.5–3× more candidates at 1024 m, because a visible chunk holds more movers. Shell CPU per frame still falls, so the extra tests cost less than the uploads saved |
| 18 | The clock cannot reach 4× in this world. The simulation delivers 5–8 Ticks per second at both 1× and 4×, and about 90% of frames wait on it. A 4× figure is a 1× figure with a longer backlog |
| 19 | Paused frames spend 6–11 ms of shell CPU against 1–3 ms when moving, at every view and chunk size. The cause is unknown. It does not cap the paused frame rate here, but see `optimizations.md` #12 |

## First test street (`test-street/`)

Pass 03's five bodies as monochrome Blender blockouts on its 80 × 64 m study site, plus the A2 alternative. Source is
`scripts/art/test-street.py`; editable `.blend` files are in `art/test-street/`. Rebuild and capture with
`BLENDER_BIN=… scripts/art/review.sh test-street`; browse with `open-test-street`. Blender 5.2.1, Godot 4.7.2,
Debug build, `ExpandedStudy --test-street`.

| Body | Brief | Built | Vertices |
|---|---|---|---:|
| H1 attached range | 24 × 12 m, 2 × 3.2 m, four 6 m modules, 18° roof across the depth | 2 × 3.5 m, party-wall upstands, distinct end walls, street doors and rear garden doors | 2,100 |
| A1 apartment | 24 × 16 m, 3 × 3.2 m, corridor, two stairs, parapeted membrane roof | 3 × 3.5 m, one central entrance, stair glazing and doors on both end walls | 2,688 |
| M1 corner | 24 × 16 m, 3 × 3.2 m, two shops, core on the side street, rear receiving | 3 × 3.5 m, shopfront in 3 m bays, residential door and stair on the side street, roller door and scuppers at the back, plant curb | 4,080 |
| A2 stair-access range, on its own pad | 32 × 12 m, 3 × 3.2 m, two stair stacks serving two flats a floor, no corridor, 11° membrane gable | 3 × 3.5 m, two stair entrances with stair glazing, garden doors and balconies, exposed eaves and verges | 3,802 |
| W1 workplace (G001) | 36 × 20 m, 2 × 3.5 m, six 6 m bays, receiving behind | As briefed, pilasters on the 6 m grid, two receiving doors, crickets, scuppers and overflows | 2,138 |
| W2 workshop | 32 × 16 m, 2 × 3.2 m, 8 m grid, 3.5 m receiving door | 2 × 3.5 m, 6° metal roof with rooflights, panel joints on the grid | 1,782 |

- Every storey is 3.5 m, the simulation's storey height. The briefs propose 3.2 m, so H1, A1, A2, M1 and W2 are taller than briefed.
- The monochrome review on 2026-09-24 kept 3.5 m storeys for every body, so the drawn height always matches the simulation's.
- A2 sits on its own pad east of the side street, as the brief asks. The `a1-front`/`a2-front` and `a1-garden`/`a2-garden` views use matched camera offsets for the unlabelled comparison.
- The street is a study fixture. No body is a simulation Building, and nothing is tied to a Lot or an Appearance Family yet.

### Surface materials (`test-street-materials-*`)

Step 4 of the pass 03 review protocol, on unchanged geometry. `scripts/art/test-street-materials.py` fetches the CC0 maps
into `art/materials/test-street/` with their sources, hashes and measured tile sizes. `scripts/art/test-street.py` gives
each body its finish, projects world-scale UVs (u level along a face, v up it or up a roof's slope) and embeds 1024 px
maps in the GLB. Colours follow the warm-slate palette: warm walls, dark slate-grey roofs.

| Body | Wall | Roof | Tile | Paint |
|---|---|---|---|---|
| H1 | ambientCG Wood Siding 009, 150 mm lap | Authored asphalt shingles, 142.9 mm courses | 2.4 × 1.2 m; 4 m | `b9ad97`, ends `a39985`; roof `353e44` |
| A1, A2 | Poly Haven Painted Plaster Wall | Poly Haven Bitumen membrane | 2 m; 20 m | `cdc6b6` on both, as the brief asks |
| M1 | Poly Haven Brick Wall 001, the brick the live shell uses | Bitumen membrane | 1.125 m; 20 m | Photograph's own colour |
| W1 | Poly Haven Concrete Block Wall, 400 × 200 mm blocks | Bitumen membrane | 2.0 × 1.6 m; 20 m | `c3bcaa`, pilasters `ada691` |
| W2 | Poly Haven Box Profile Metal Sheet, 200 mm ribs | The same sheet | 2 m | `5d6a6e`; roof `a3a8a9` |

- A painted surface's albedo is greyscale at linear mean luminance 0.7. The glTF colour factor is the paint colour divided
  by that mean, so the paint reads true on average. The shingles divide by their own recorded mean, 0.178.
- Poly Haven states 2 × 2 m for the block wall. Its 8 courses and 5 blocks give 2.0 × 1.6 m at true block size, so the
  tile is mapped anisotropically.
- Seen at close range, the membrane shows its 20 m repeat as faint bands. The ground is still flat colour.
- The Godot import extracts each model's maps beside it, so every body carries its own copy.
