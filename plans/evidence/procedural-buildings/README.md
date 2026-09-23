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
