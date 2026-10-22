# 0066 — Retained spatial rendering

Owns the shell's rendering work and diagnostics, split from `0063` at the player's request.
The instance-cap repair is implemented; the broader performance work below remains open.

## Implementation

`InstanceLayer` / `InstanceBuffer` retain entity/component keys and partition by transform origin
into Chunks. Bounds include the complete transformed mesh. Buffers grow geometrically, empty
batches disappear, and `Replace` updates an entity without repacking unrelated Chunks. Godot owns
frustum and shadow culling; Buildings are not discarded by a camera-plane test.

`WorldChanges` is an optional, unsaved and unhashed journal of World Building mutations.
`Main.UpdateBuildings` consumes it, including reused slots. Ruleset adoption and player Commands
invalidate the standing view; a new World starts invalidated. Direct column writes are outside
this interface. Occupancy and abandonment refresh the affected Building. The Age overlay still
requires a full colour refresh as its time normalisation changes.

`Main.IndexMovement` makes a conservative swept spatial index once per rendered Tick.
`DrawMovement` queries candidates and calls `VisibleAgents.TryGet`, preserving the simulation's
route-based placement between Ticks. Neither query nor layer storage has a fixed population cap.

`FlushInstances` budgets transfers and `InstanceBuffer.Flush` prioritises nearby batches. A single
oversized batch is admitted alone to prevent starvation; this is a transfer allowance, not a hard
frame-time guarantee. Detail residency releases distant tree, rock, kerb and Traveller buffers;
CPU records survive. Woodland remains represented by the ground texture. Thresholds, hysteresis,
Chunk size and the transfer allowance are PROVISIONAL. Full map geometry remains resident for
Buildings and roads. `PickInformation` rejects whole batch bounds before testing triangles.

The masonry work paused in the other session is preserved. Uncapping foliage exposed decorative
trees in permanent Water Cells; `Scatter` now excludes those Cells.

## Verification and diagnostics

- `scripts/test-renderer.sh`: real Godot buffer checks, including a single Chunk with 70,000
  instances, unchanged and reordered passes, identity, removal, migration, transformed bounds,
  colour/custom-data packing, residency and deferred uploads. `--headless` omits GPU readback.
- `python3 scripts/check-renderer-scene.py`: large-world counts, repeat draws, camera turns and
  detail return. Build the shell in Debug first; output artifacts are retained in a temporary folder.
- `WorldChangesTests`: same-slot replacement, occupancy, abandonment and unchanged State Hash.
- `BOROUGH_RENDER_VERIFY=1`: compare incremental Building geometry against full regeneration and
  compare spatial Traveller selection against visible results from the full query. Expensive,
  deliberately opt-in.
- `BOROUGH_RENDER_PROFILE=1`: each `draw PATH` writes `PATH.profile.tsv`. `render_work` records
  full Building passes, Building edits, movement-index slot visits and frame placement queries.
  `render_frames` records frame count, accumulated CPU milliseconds and maximum CPU milliseconds;
  it includes stepping and UI work, excludes startup and deferred screenshot capture, and is not
  GPU frame time. Per-layer rows report batches, upload calls, instances, bytes, upload milliseconds
  and pending batches. Do not treat a driven capture as a reference-machine timing.
- `draw` drains pending uploads for a complete diagnostic. Its `layer` count is resident instances;
  `retained` records total instances and Chunks, including distance-suppressed detail. Geometry rows
  come from uploaded buffers. Normal frames retain the transfer allowance.

Observed with `bordered.toml`, seed 0, 100 Citizens at Tick 256: 530,914 road instances, 1,035,739
footway instances, 1,035,851 kerb instances and 475,448 tree records. The old buffers stopped at
65,536 or 262,144. A repeated paused draw issued no further uploads. This is a completeness check,
not a frame-rate claim.

The assertion lane passed. Driven comparisons covered `pictured.toml` camera turns, camera return
and overlays; `flooded.toml` Ticks 6000–6064; and `shopping.toml` Ticks 600–800. The latter two used
400 Citizens and the differential checks. The pictured view used 1,000 Citizens; its normal and
Age views were inspected with the preserved materials. The existing live UI check also passed
selection, hover, resizing and paused State Hash assertions against the new batches.

## Remaining performance work

`Main.Performance` adds an FPS indicator, tooltip and F3 timing breakdown. Simulation CPU is
reported per second; shell and viewport rendering CPU/GPU are per frame. An unsupported GPU
counter says unavailable. `BOROUGH_PERFORMANCE_LOG=PATH` writes one-second CSV windows;
`python3 scripts/measure-shell.py` captures running/paused `minimal.toml` with 1,000 Citizens
and checks paused drawing/uploads and the indicator. This is a development diagnostic, not a
reference-machine benchmark. Build the shell before running it.
Some driven runs acknowledged `quit` but stalled during shutdown; the capture script records
that separately and terminates its own process after preserving the measurements.

The provisional default frame limit is 60; Godot's `--max-fps 0` removes it and `--max-fps 30`
provides a lower-power comparison. `Daylight` retains unchanged paused lighting, the clock arc
redraws when its displayed minute changes, and hidden debug text is not rebuilt. These changes
preserve visual quality and simulation cadence. Metal returned no GPU timing samples during
verification; no GPU-time or power reduction is claimed. Pass-level GPU profiling remains open.

- Incremental foliage exclusion/scatter and vacant-Lot updates: a structural Building edit still
  refreshes these broadly. Geometry preparation itself is synchronous; the transfer allowance does
  not budget it. Road edits still regenerate paving.
- The movement index still scans Traveller slots and routes once per rendered Tick. Cache route
  geometry and maintain membership incrementally if this dominates at larger populations.
- Actual modular-kit LOD meshes and material/shadow simplification by projected size; current
  distance suppression is not that asset pipeline. Whole-map road and Building aggregation remain
  necessary candidates for city-scale views.
- Event-driven UI summaries, measured Chunk sizing, and a quiet-machine CPU/GPU/memory comparison
  across a repeatable camera path. Consider worker preparation only after profiling these costs.

These remain owned here; lifting the fixed instance limit is not a claim that arbitrary scene
complexity meets a frame budget.

## Rendering comparison, 2026-09-07

`InstanceLayer.ChunkMetres` now defaults to 256 m. In a local Release comparison on the Apple
M4 Pro, Godot 4.7.2 Metal Forward+, `minimal.toml`, seed 0, 1,000 Citizens and 88 Buildings,
paused at Tick 422, the median of three window medians was 189.6 FPS against 176.5 FPS after
returning to the original 1024 m batches. Camera: focus Tile (136,104), distance 608 m,
35° tilt, 45° yaw; viewport 3024×1834. VSync and the frame limit were disabled. A low-load
editor remained open: this is a local throughput comparison, not a reference-machine budget
reading, GPU duration or power measurement. The ordinary frame limit remains 60.

The paired images were inspected; no geometry or material simplification was applied. Renderer
GPU-readback checks and the large-world count/upload/camera checks passed. Smaller batches trade
tighter culling for more nodes; larger-world throughput and memory still need comparison.

## Chunk size in a large world, 2026-09-23

`InstanceLayer.ChunkMetres` defaults to 1024 m again. On the i5-10400 and GTX 1080 dev machine,
Godot 4.7.2 Vulkan Forward+, Release, `stress-shopping.toml`, seed 0, 1,000,000 Citizens and
86,924 Buildings paused at Tick 600, 256 m chunks drew 32,184 nodes at the whole-city opening
camera and spent 55 ms of render CPU on them (12.9 fps). 1024 m chunks drew 2,694 nodes and
reached 67 fps. The 150 m street view rose from 116.5 to 127.9 fps and the 600 m district view
from 70.6 to 105.2 fps. Coarser culling added 0.3–0.7 ms of GPU there. The machine was not quiet,
so the figures are upper bounds. Moving Travellers were not exercised, because every capture was
paused. Evidence: [`evidence/procedural-buildings/`](evidence/procedural-buildings/README.md#opening-camera-profile-opening).

With the original batches, isolated resolution and shadow-removal experiments improved throughput;
shrinking the atlas and reducing foliage range bought much less. These quality reductions remain
diagnostic probes rather than defaults. The early probes accidentally changed shadow depth precision;
the accepted comparison preserves `directional_shadow/16_bits` from ProjectSettings.

`scripts/compare-rendering.py --chunk-metres 1024` and `--chunk-metres 256` retain images, raw
windows, effective settings, build configuration, assembly hash and fixed-world/camera assertions.
`Main.RenderProbe` supplies the guarded `ui render-probe NAME` commands. They require
`BOROUGH_RENDER_PROFILE=1`; `BOROUGH_RENDER_CHUNK_METRES` accepts 256, 512 or 1024 for comparisons.
FPS sampling now uses measured wall time rather than Godot's smoothed frame delta.

For Release captures, build with `dotnet build src/Borough.Godot -c Release
-p:OutputPath=ABSOLUTE_PATH_TO/src/Borough.Godot/.godot/mono/temp/bin/Debug/` so the assembly Godot
loads is the one measured. Restore the normal Debug build afterwards with `dotnet build
src/Borough.Godot -t:Rebuild`. Godot's GPU counters were unavailable, Metal log timing fields were
redacted, and Screen Recording permission was absent; no native GPU-time comparison was obtained.

## Garden prop draw distance, 2026-10-21

Garden props are hidden beyond 2,000 m (`GardenPropDistance` in `Main.Gardens.cs`). The garden
tree layer keeps its own 4,000 m limit, and garden ground and hedges have none. Before the limit,
the opening camera of a 1,000,000-Citizen `stress-shopping.toml` city drew every fence and railing
in the city.

| Layer | Triangles per mesh | Instances | Triangles per frame |
|---|---:|---:|---:|
| fence-panel | 60 | 1,811,608 | 108.7 M |
| railing-panel | 144 | 374,264 | 53.9 M |
| seven other props | | | 14.1 M |

Conditions: i5-10400, GTX 1080, Godot 4.7.2 Vulkan Forward+, shell built with
`scripts/build-shell.sh --optimized`, `scripts/measure-shell.py`, 15 s per phase, load 1.65–1.95.

| Limit | GPU per frame | FPS | Primitives | Draw calls |
|---|---:|---:|---:|---:|
| None | 156–157 ms | 6.4 | 186.7 M | 5,059 |
| 1,500 m | 21.6–21.9 ms | 45 | 9.9 M | 2,899 |

The opening camera stands about 14.6 km above the ground, so any limit under that gives the same
opening figure. GPU time climbed to 157 ms over the first 10 s whether the city ran or paused, as
the 8 MB per-frame upload allowance made the garden batches resident ([#217](https://github.com/chrisbenincasa/city-sim/issues/217)).
The 2,000 m value was chosen from shots at 700 m and 2,000 m camera distances, where it matched
the unlimited picture.

With the limit, render probes at the paused opening camera split the remaining 21.8 ms of GPU time.
Load fell from 5.3 to 1.9 during the run, so the savings are rough.

| Probe | GPU per frame | Saving |
|---|---:|---:|
| `hide-building,roof,paired-roof,parapet,hip` | 14.9 ms | 6.9 ms |
| `scale50` | 15.6 ms | 6.2 ms |
| `hide-garden-ground,garden-hedge` | 17.3 ms | 4.6 ms |
| `hide-road,footway,kerb,plot,zone,cell` | 17.9 ms | 4.0 ms |
| `ssao-off` | 20.6 ms | 1.3 ms |

No single layer dominates. Garden ground and hedges still draw at any distance.

## Loading a million-Citizen save, 2026-10-21

A headless `--save-city` of `stress-shopping.toml` at 1,000,000 Citizens and Tick 256 (55 MB)
opens with `--load` and reaches its first frame in 14.8 s, of which the first frame takes 8.2 s.
Conditions: optimized shell, i5-10400, GTX 1080, load 1.3–2.0, five runs within 0.2 s.

A sampled trace of the main thread before the first frame:

| Work | Seconds |
|---|---:|
| `UpdateBuildings`: `Massings` and foliage `Scatter` | 2.7 |
| `UpdateBuildings`: garden `Fill` | 1.9 |
| `UpdateBuildings`: `PlanGardens` | 0.7 |
| `FinishReady`: `Pave`, facade materials, texture loads, surfaces | 2.9 |
| `FlushInstances`, mostly batch bounds | 1.5 |
| Suspended for garbage collection, within the rows above | 1.5 |

`InstanceBuffer` now keeps its instances in one array of structs addressed by handle, and mirrors
each batch's MultiMesh counts. GC suspension fell to 0.38 s and the load to 14.2–14.5 s. The
25,000-Citizen draw list is unchanged apart from Traveller rows, which follow frame timing.
`DOTNET_gcServer=1` took the old load to 13.9 s; its memory cost is unmeasured.

At the opening camera every foliage, kerb and garden prop chunk lies beyond its draw distance. Much
of the 4.9 s in `Scatter`, garden `Fill` and `PlanGardens` therefore builds instances the first
frame does not draw. Garden ground and hedges have no limit and are drawn.

### Gated layers

Trees, rocks, kerbs, garden trees and garden props are now built only where the camera could draw
them. Each such `InstanceLayer` is `Gated`:

- A producer calls `BuildFrom()`, which records the camera position, and skips anything
  `Reaches()` rejects.
- Reach is the detail distance plus its 15% hysteresis plus a slack of a quarter of the detail
  distance. An instance counts by its nearest point, so a long kerb band reaches by its near end.
- `Regate()` runs each frame before the flush. It writes a layer again once the camera is more
  than the slack from where that layer was built, or its detail distance has changed.
- A gated build before the camera exists is empty and stale, so the first frame builds it.
- `Scatter` clips its cell window to the reached ground. `Kerbs()` skips Segments out of reach and
  caches each Segment's pieces until the next `Pave()`, which also snapshots the Addresses.

Results on the same save, machine and conditions (load 1.2–1.5, three runs each):

| Measure | Before | Gated |
|---|---:|---:|
| First frame row after launch | 14.2–14.5 s | 8.8–9.0 s |
| First frame | about 8.2 s | 3.07–3.11 s |
| Rebuild on descending to `focus 1875 1875 700` | none | 730–755 ms |
| Rebuild on panning 4,500 m east at 700 m | none | 311–315 ms |

The descent pays for building about 137,000 kerbs and the nearby trees and garden props for the
first time, work the ungated shell did for the whole city at load. The render profile's
`render_regate` row gives the count, total and worst rebuild in milliseconds.

The 25,000-Citizen draw list at `focus 120 120 900` keeps every row it had. It gains 7,212 trees
and 16 kerbs 7–11 km out, because a freshly built chunk starts resident and keeps the 15%
hysteresis, where the ungated chunks had been dropped at the opening camera. Retained trees on
that city fell from 863,726 to 45,353.

While the simulation runs, a frame whose Buildings change runs `Scatter` and the garden `Fill`
again. Both now cover only the camera's reach rather than the whole city.
