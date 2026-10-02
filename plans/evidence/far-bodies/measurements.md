# Far body measurements

Quiet-machine timing is deferred. Every figure below comes from a busy machine or a first frame after a camera jump, so none of them is a performance claim. The pending measurement compares `000db174` with this branch at 3,000, 1,000, 700, 475 and 400 m, using three interleaved runs per arm and `BOROUGH_PERFORMANCE_LOG` one-second buckets.

Measured October 1, 2026 on `zeus`, an Intel Core i5-10400 with 6 cores and 12 logical CPUs, 63 GiB RAM available to Linux, and an NVIDIA GTX 1080 using driver 580.178.04. The CPU governor was `powersave`. Godot 4.7.2 used Vulkan Forward+ at 2560×1371 with VSync and the 60 FPS limit enabled. The interactive desktop was otherwise idle, but the runs were not CPU-pinned.

The shell used its simulation worker and one route worker. Far mesh generation used the default .NET `Parallel.For` scheduler with 12 logical CPUs available. Both arms used `rulesets/platted.toml`, seed 0, 40,000 Citizens, family bodies enabled, focus Tile 556 560, tilt 40°, and the same heading. The comparison started its fixed shots at Tick 600 and then approached from 3,000 m to 400 m in 100 m steps.

## Method

- **Before** was B01 commit `000db174`, which draws distant massing boxes. Its detached worktree received only the `render_times` profile row used by the after arm.
- **After** was the B02 implementation in this commit with the `grid` far-window option.
- Both assemblies were built in Release into Godot's load path with `dotnet build src/Borough.Godot -c Release -p:OutputPath=ABSOLUTE_PATH/src/Borough.Godot/.godot/mono/temp/bin/Debug/`.
- `compare.drive` ran with `BOROUGH_RENDER_PROFILE=1`. Each profile was written one Tick after its screenshot so Godot's counters described the pictured frame.
- Render CPU and GPU durations come from `RenderingServer.ViewportGetMeasuredRenderTimeCpu/Gpu`. Draw calls come from `ViewportGetRenderInfo` for the visible pass. They exclude synchronous PNG and TSV file writing.
- The fixed-shot results are one first rendered frame after each camera jump, not a median or a quiet-camera throughput benchmark. This preserves the requested capture sequence and includes transition work.

## Far mesh generation

The after arm generated 210 cached far meshes. Three Release runs took 178.8, 180.0, and 189.2 ms. The median was **180.0 ms**, with a 178.8–189.2 ms range.

The full family-body build, which also generates near meshes and creates Godot meshes and layers, took 4,244.5–4,401.5 ms in those runs. B01 took 4,065.4 ms in its comparison run.

## Fixed-shot rendering

| Distance | Arm | Render CPU ms | Render GPU ms | Visible draw calls |
|---:|---|---:|---:|---:|
| 3,000 m | B01 massing boxes | 13.223 | 7.282 | 116 |
| 3,000 m | B02 `grid` | 26.558 | 9.399 | 1,118 |
| 1,000 m | B01 massing boxes | 0.422 | 11.615 | 82 |
| 1,000 m | B02 `grid` | 3.951 | 11.385 | 1,074 |
| 400 m | B01 near bodies | 2.010 | 51.158 | 855 |
| 400 m | B02 near bodies | 3.762 | 16.077 | 1,537 |

The 400 m first-visit GPU result moved in B02's favor despite more draw calls. That result is a transition frame, so it does not support a steady-state speedup claim. On the later 400 m approach shot, B01 measured 2.186 ms CPU and 11.095 ms GPU while B02 measured 3.869 ms CPU and 13.254 ms GPU.

## The reported 1,000 m stall

A separate Release run moved to 1,000 m before enabling family bodies, then held the camera, moved to 3,000 m, and returned to 1,000 m.

The exact 4 FPS reading did not reproduce. Cold enablement caused one **4,244.5 ms** family-body construction stall, including **180.0 ms** for far mesh generation. The wall-time sampler reported 39 frames over 4.903 seconds, or **7.954 FPS**, for the interval containing that frame. The shell returned to 59.9–60.1 FPS immediately afterward. The first settled 1,000 m interval measured 2.916 ms render CPU and 8.656 ms GPU; the repeat measured 2.874 ms render CPU and 7.442 ms GPU.

The slowdown is therefore a one-time family-body enablement stall, not a sustained 1,000 m rendering rate. The synchronous capture script also makes its on-screen FPS label read about 8 FPS while it writes files, so that label is not the rendering measurement.

## Visual and residency checks

The 1,000 m and 3,000 m captures show the authored mid-rise and tower silhouettes and roofs instead of massing boxes. At 500 m, near windows and fins line up with the far shader's bay and storey grid. No hole or doubled building is visible at the band edge.

The draw lists support that observation. The 3,000 m shot contains 216 detailed family-body IDs. All 216 occur exactly once in the 500 m draw list, with no missing or duplicate ID. The remaining family-body rows at 500 m are the near low-rise bodies, whose far representation intentionally remains a massing box.

The fixed shots are [`3,000 m`](grid/3000.jpg), [`1,000 m`](grid/1000.jpg), [`500 m`](grid/500.jpg), and [`400 m`](grid/400.jpg). The approach is in [`grid/approach-contact.jpg`](grid/approach-contact.jpg). The capture log, performance table and `*.profile.tsv` files are in [`grid/`](grid/).
