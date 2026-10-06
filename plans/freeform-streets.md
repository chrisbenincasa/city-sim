# Freeform local Streets with road-aligned Lots

State: design. Land model, saved frontage and arc Streets decided 10/03/2026; junctions, wedge use
and snap details remain open.
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

### Decided land model

Decided 10/03/2026. Recorded in
[`adr/0174`](../docs/adr/0174-lots-are-cut-along-segments-and-save-their-frontage.md).

| Decision | Choice |
|---|---|
| Carving | Every Lot is cut as a plot along one side of one Segment. No separate block carve exists |
| Block | A closed face of the Street graph. It sets each side's plot depth from its pattern and owns the leftover interior |
| Spur in a loop | The face walk passes both sides of the spur, so its plots join the block layout |
| Whole-block forms | Car-park center and other trade forms are one plot whose depth is the full block |
| Claim order | Existing Lots first. Then new plots by Segment id, side, offset |
| Conflict | An overlapping plot shrinks in depth to `[lots]` minimum depth, else it is dropped |
| Leftover ground | Wedges and interiors stay open ground. Yards and parks are a later decision |
| Lot geometry | Saved corner, facing direction (Q16.16), width and depth. The footprint uses the same form |
| Overlap test | Exact oriented-rectangle test in integers, through a uniform spatial hash |
| Block identity | Not saved. Recomputed from the graph after each edit |
| Block pattern | Function of band, world seed and the face's anchor (lowest Segment id on its boundary, side) |
| Existing Lots | Keep their saved form. A pattern change affects only unclaimed ground |
| Frontage | Saved: Segment handle (monotonic id), offset, side |
| Segment split | The original id keeps the A part. Lots past the split move to the new Segment, offset minus the A part's length |
| Bulldoze | Lots keep standing without frontage (`adr/0079`) |

`BlockTable`'s lattice columns and `Frontage.Locate`'s lattice lookup go away. `Lot.East`/`North`
stop encoding frontage.

### Decided Street shape: circular arcs

Decided 10/03/2026. Arcs over Béziers because Parallel mode and curved plot strips need offset
curves, and an offset arc is an arc.

| Item | Choice |
|---|---|
| Saved shape | One signed column on `RoadSegmentTable`: the sagitta, Q16.16 Tiles. Positive bulges left of A→B. Zero is straight |
| Endpoints | Integer-Tile Nodes, as today |
| Sweep limit | At most 90° per Segment. Longer bends chain Segments at Nodes |
| Minimum radius | Ruleset value in `[roads]` |
| Length | `LengthTiles` holds the arc length, computed at lay time |
| Derived geometry | Center, radius, start angle and sweep, rebuilt from the saved row |
| Angles | Q16.16 fractions of a turn |
| Arithmetic | Add integer Q16.16 `Sin`, `Cos` and `Atan2` to `Arithmetic.Transcendental` |
| Point and tangent at offset | From the derived center and angle. Serves frontage, plots, `VisibleAgents.TryEnds`, `LineSourceQueries.DistanceTiles` and sealing |
| Plots on a curve | Front edge is the chord between the plot's two offsets. Convex side fans out and leaves wedges; concave side converges and the conflict rule applies |
| Gestures | Shell only. Simple curve is the arc through start, bend point and end. Continuous is the arc tangent to the previous Segment through the new end |
| Parallel mode | Concentric arc. Endpoints round to Tiles, so the copy is concentric to within a Tile |
| Command | A new `Street` verb carries both endpoint Tiles and the sagitta. `Connect` keeps lattice lays and bulldozes, so no log line changes meaning and the format stays at version 1 |
| Drawing | The shell tessellates arcs from the same parameters |

### Decided: Streets demolish what they cross

Decided 10/03/2026.

- A Street drawn through Lots or Buildings clears every Lot its corridor touches.
- Each occupied Building costs `World.DemolitionPrice`, paid to the same payees as `Demolish`.
  Vacant Lots and unoccupied Buildings clear free, as they do under `Demolish`.
- The edit is atomic. If the treasury cannot pay the total, the whole Street is refused.
- The preview shows the Buildings to be cleared and the total price before commit.

### Decided: joining and splitting

Decided 10/03/2026.

- The simulation joins an endpoint to an existing Node only on an exact Tile match. Snapping
  tolerance lives in the shell, so the Input Log records the snapped result.
- A new Street that crosses or ends on an existing Segment adds a Node at the nearest Tile and
  splits every Segment there. Faces need these Nodes.
- Each half is refitted through its own ends and the original arc's point at its mid-offset. The
  road moves at most √2/2 Tile + 1/64 Tile. A split whose halves would exceed that bound or the
  quarter-turn sweep is refused. Frontage migrates by the split rule above.
- Two `[roads]` minimums refuse an edit: Segment length after a split, and crossing angle.

### Decided: block-addressed commands

Decided 10/04/2026. Commands keep their Tile and payload fields, so the Input Log format does not
change for this decision.

| Command | Freeform addressing |
|---|---|
| `Zone` | Paints the closed face that holds the Tile. A Tile outside every closed face paints the nearest Segment side within one plot depth; with no side in reach, it is refused |
| `ZoneParcel` | Unchanged. Paints the parcel that contains the Tile, by exact oriented containment |
| `Trip` | Kept. The origin is the occupied Building nearest the Tile. The payload offset counts steps of `BlockTiles` Tiles, and the destination is the occupied Building nearest that point |

Painting a face covers its interior, which stays open ground under the land model.

### Decided: slice 7 junctions, length step and preview

Decided 10/06/2026.

- Carriageways overlap at Nodes. Junction polygons belong to the Arterials and Junction
  construction row.
- The length snap step is one plot width. That is `[lots] residential_frontage_tiles` where a
  Ruleset sets it, and otherwise 2 × `block_tiles` / `lots_per_segment`.
- The preview shows the snapped line, the refusal sentence, the total clearing price and the
  Buildings it would clear, highlighted.
- A zone-grid snap to existing strip Lots, guideline snaps and building-side snaps wait for play
  to show a need.

Slice 7 is split. 7a is straight mode on `street` with snapping to Nodes and Segments, the preview,
and curves paved as short chords. 7b is simple-curve and continuous modes, length and angle snaps,
and the driven demonstration of the acceptance checks.

### Remaining design decisions

1. **Wedge use.** Leave open, give to the adjacent Lot as yard, or allow parks.

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

## Implementation slices

Each slice is one PR that leaves the lattice game working.
Slice 6 also replaces the split helper's per-Tile displacement guard (O(length)) with an analytic
bound, and replaces the lattice-only `Frontage.AttachTo` lookup. Slices 2–6 move the State Hash and
re-record goldens by the [procedure](../tests/Borough.Tests/Golden/README.md).

| # | Slice | Contents | Gated by |
|---|---|---|---|
| 1 | Arc arithmetic | Q16.16 `Sin`, `Cos`, `Atan2` in turns. A pure arc type: center, radius, point and tangent at offset, distance to a point, offset arc. Tests against reference values | — |
| 2 | Saved Segment shape | Sagitta column (all zero), derived centerline column, rebuild audit. `VisibleAgents.TryEnds` and `LineSourceQueries.DistanceTiles` read the centerline | 1 |
| 3 | Saved frontage | Lot saves Segment handle and offset. `Frontage.Locate` runs only at creation. Bulldoze leaves Lots unfronted. Split migration with a unit test | — |
| 4 | Oriented Lot ground | Parcel and footprint as corner, direction, width, depth. Exact overlap test. One uniform spatial hash replaces `StreetGrid` off-lattice buckets, `TrafficPresence._near` and the `LineSourceQueries` window. Shell massing faces the Segment | 3 |
| 5 | Segment-side carver | Planar face walk, strip carving per side, pattern depth per face, claim order and shrink-or-drop. Blocks become derived; `BlockTable` lattice columns go. Generation lays lattice Streets and carves with the new carver | 4 |
| 6 | Freeform `Street` verb | Endpoints plus sagitta, exact joins, crossing splits, minimum length and angle, demolition at the `Demolish` price. Seals the laid Street along its centerline | 2, 5 |
| 7 | Shell drawing | Straight, simple-curve and continuous modes. Snapping, preview with refusals and demolition cost, arc paving meshes. Driven demonstration of the acceptance checks | 6 |
| 8 | Batch modes | Grid and Parallel modes over a batch `Street` | 7 |

Slices 1 and 3 can run in parallel.

Slice 4a selects the background Street by exact centerline distance, then greater contribution
at the point, then lowest monotonic Segment id. At an exact tie between unequal sources, choosing
the louder background lowers total intensity by the quieter source's contribution.

## Next step

Slices 1 to 5 are built. Lattice squares keep the pattern carver. Every other Street side, curved
or off the lattice, is cut by `SegmentSide` into plots turned to the Street. Those plots take the
pattern of the face they front, or of their own Segment side when the roadside is open. Whole-block
forms carve as Perimeter strips there, and residential plot sizing applies where it does on the
lattice. A Street edit gathers both kinds of plot and claims them in one pass, by Segment id, then
side, then offset.
Slice 6 is built. `World.LayStreet` lays a Street between any two Tiles with a sagitta, joins
Nodes exactly, splits the Segments it crosses or ends on, enforces the three `[roads]` minimums,
seals its ground, and moves Lot, Car Park, Trip and Leg Addresses past each split. The split guard
stops refining at 1/256 Tile and refuses there. The `street` Input Log verb applies it. It refuses
by name for each lay refusal and for a treasury that cannot pay. It clears every Lot under the paved
width first and pays the displaced at the `Demolish` price. A route over a split Segment gains a hop
for the created half, in its direction of travel. A Vehicle already on that hop keeps the arrival
time it was priced at for the whole Segment. Slice 7 moves the shell from lattice `connect` to
`street`.
