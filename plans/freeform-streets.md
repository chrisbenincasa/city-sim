# Freeform local Streets with road-aligned Lots

State: design. Blocks + strips selected; geometry, saved ownership and edit details remain open.
Survey of current code done 09/27/2026 against `main` at `919290a5`.

## Outcome

The player lays curved and angled Streets. Street-enclosed ground gets a coordinated block layout
with multiple smaller rectangular Lots and individual Buildings. Road sides outside enclosed
blocks use strips of rectangular Lots aligned to the Street's local direction.

Blocks coordinate Lot layout and the interior; they do not require one long Building per face or
a courtyard in every block. Existing Lots and Buildings stay fixed when a new Street closes a
loop. The new block layout uses only the remaining unallocated ground.

Curves can leave wedge land; its use remains a design decision. Freeform local Streets require
amending [`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md), which still snaps
Streets to the Tile grid.

## Survey result

The work splits in two. The movement and land-accounting layers survive with mechanical changes.
The Lot layer is built on street-enclosed blocks and needs redesign.

### Survives unchanged

| Area | Why |
|---|---|
| Routing, Trip cost, congestion | Arc costs read `LengthTiles` and `FreeFlow` only (`Space/RoadGraph.cs:452-489`) |
| A* heuristic | Calibrates its unit per Arc (`Movement/WalkScratch.Direction.cs:33-80`); weaker on curves, still admissible |
| `Address`, parking sheds | Offset is measured along the road, which holds on a curve |
| `RoadNodeTable` | Nodes already sit at arbitrary Tiles (Arterial Junctions) |
| `RoutingPartition`, travel-time matrix | Keyed on Cells, not the lattice |
| Woodland, pollution, desirability, floods | Count per Cell |

A Cell is a fixed 32-Tile square (`Space/CellGrid.cs:27`), independent of `block_tiles`. It matches
a block only because shipped Rulesets set `block_tiles = 32`.

### Mechanical rework

| Area | Change |
|---|---|
| Segment geometry | Add a saved shape column to `RoadSegmentTable`; hash-bearing |
| Point on a Segment | `VisibleAgents.TryEnds` and `LineSourceQueries.DistanceTiles` interpolate the straight chord; read the stored shape instead. Already wrong for curved Arterials |
| Spatial indexes | `StreetGrid` off-lattice buckets, `TrafficPresence._near`, `LineSourceQueries` window become a uniform spatial hash |
| Sealing | Rasterize curved Streets Tile by Tile, as `RoadGenerator.WalkArterial` already does |
| Rotated footprints | Overlap, permission and party-wall checks move from axis-aligned boxes to oriented boxes or a Tile raster (`World.Overlaps`, `LocalLayout.cs:238`, `HousingConstruction.cs:168`, `Main.FamilyBodies`) |
| Massing | Facing comes from the Segment direction instead of four compass faces (`Main.Massing.cs:132-245`) |
| Road paving | One straight box per Segment today (`Main.Ground.cs:1161-1290`); curves need chained or swept meshes |
| Re-measure | Land-value 2×2 sample, District prominence threshold, flood buffer size; each was tuned where Streets run on Cell edges |

### Selected Lot layout

- Streets enclosing ground define a block whose Lots and interior are laid out together. The
  layout can coordinate courtyards, back gardens and back-to-back Lots without requiring a single
  Building along each face.
- Road sides outside enclosed blocks use independent roadside strips, including dead-end Streets
  and open curves. Both methods generate rectangular Lots and must not claim the same ground.
- Every Lot still needs frontage on a Street. Block coordination does not replace the Lot's
  access relationship to its frontage Segment.
- Closing a loop does not delete, relocate or re-subdivide existing Lots or Buildings. Existing
  Lots constrain the block layout, which allocates only the remaining ground.
- New Lot candidates that overlap existing Lots or Buildings are rejected. Conflicts between new
  candidates need a deterministic resolution rule.

### Selected drawing and snapping

The grid survives as a drawing aid, not an engine constraint, following Cities: Skylines II.
The simulation receives endpoints and shape. The shell's tool chooses them.

| Draw mode | Gesture |
|---|---|
| Straight | Start, end |
| Simple curve | Start, bend point, end |
| Continuous | Each Street starts tangent to the last |
| Grid | Two corners, then drag sideways to lay a block of Streets |
| Parallel | Lays a second Street at a set offset from the first |

| Snap toggle | Snaps to |
|---|---|
| Existing Streets | Nodes and points along existing Segments |
| Length | Multiples of a Ruleset step, so Lots tile evenly along a Street |
| Angle | 90° to an existing Street, with configurable finer steps |
| Guidelines | Lines extended from existing Streets |
| Building sides | Edges of existing Lots and Buildings |

- One master toggle turns all snapping off and takes a key binding.
- Snapping runs in the shell only. It changes which endpoints a command carries and adds no
  simulation state.
- Endpoints are integer Tiles and shapes are Q16.16. Repeated 90° and length snaps land exactly,
  so a grid cannot drift. Cities: Skylines II grids break from accumulated float error in
  endpoints.
- Grid and Parallel modes issue several Streets from one gesture. The command needs a batch form;
  `adr/0077`'s lattice run is the precedent.
- Snap defaults, the length step and angle steps go in the Ruleset or shell settings, not
  constants.
- Lattice-only Street runs (`ConnectPayload.Segments`) are the play-testing stopgap. This tool
  replaces them.

### Remaining design decisions

1. **Block geometry and identity.** Recover enclosed ground from the Street graph and choose how
   a block retains its identity through edits. `BlockTable` currently saves lattice coordinates
   and a `Pattern`. Decide how `ZoneBlock` and `BandBlock` target freeform blocks and how the
   equivalent controls apply to roadside strips.
2. **Subdivision and land claims.** Adapt the current `BlockPatterns` forms to irregular ground
   and existing Lots. Choose strip depth, corner treatment and deterministic candidate conflict
   rules. Define how strips and blocks divide ground without overlapping, including when a loop
   closes around existing strips.
3. **Frontage.** `Frontage.Locate` derives a Lot's Segment and offset from its position on a lattice
   line (`Space/Frontage.cs:140-170`). Choose saved Segment ownership or derived lookup, including
   preservation when a Segment splits. This reopens
   [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md).
4. **Street representation and the edit command.** Pick the curve form for Q16.16 (arcs, polyline
   or control points). The `connect` command packs an axis bit and derives the far endpoint from the
   lattice (`Input/Command.cs:573-599`). A curved Street needs endpoints plus shape, which bumps
   `InputLogCodec.Version` and re-records committed logs. Also decide endpoint snapping, merging and
   mid-Segment splitting. `TripPayload` addresses destinations in lattice blocks. A Street drawn
   through an existing Lot or Building needs an explicit refusal or demolition policy; preservation
   when a loop closes does not settle that separate edit.
5. **Junction geometry.** Arbitrary angles need junction polygons. Shared with the Arterials and
   Junction construction row.
6. **Wedge land.** Leave it empty, give it to the adjacent Lot as yard, or allow parks.
7. **Snap and preview details.** The grid is kept as a drawing aid (see above). Choose the length
   step relative to Lot widths, whether a zone-grid snap aligns new Streets to existing strip
   Lots, and what the preview shows before commit: snapped geometry, refusals and cost.

## Found in passing

- `TrafficPresence` buckets nodes with `FloorDiv(node.East, block_tiles)`
  (`Space/TrafficPresence.cs:138-141`), not `BlockLattice.LineAt`. On a varying lattice
  (`block_spread_tiles > 0`) this likely puts nodes in the wrong bucket. Not yet confirmed by a test.

## Acceptance checks

- A loop with a dead-end spur produces a coordinated block interior and roadside Lots outside
  it. Multiple individual Buildings can face each Street; no face requires a single long Building.
- An open curve produces roadside Lots without requiring an enclosed block.
- An angled or triangular block fits rectangular Lots without overlap and accounts for leftover
  ground explicitly.
- Closing a loop around existing strip Lots preserves their identities, geometry and Buildings.
  Only previously unallocated ground receives new Lots.
- Blocks and strips never claim the same ground. Lot frontage remains valid after supported
  Street edits.
- Replay, save/reload and thread-count equivalence hold. A driven demonstration shows these
  layouts and loop closure before the capability is marked complete.

## Next step

Design block geometry and land claims against the loop with a spur, open curve, triangular block
and closure around existing strip Lots. Settle saved block identity and frontage preservation with
those cases before choosing the implementation sequence.
