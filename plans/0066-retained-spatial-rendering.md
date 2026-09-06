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
