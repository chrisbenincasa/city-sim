# Freeform local Streets with road-aligned Lots

State: scoping. Survey of current code done 09/27/2026 against `main` at `919290a5`.

## Outcome

The player lays curved and angled Streets, following Cities: Skylines. Each Segment grows
rectangular Lots along both sides, aligned to its local direction. Overlapping Lots are removed.
Wedge land on curves stays unbuilt or becomes yard. Adopting this amends
[`adr/0014`](../docs/adr/0014-grid-streets-with-freeform-arterials.md), which snaps Streets to the
Tile grid.

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

### Needs a design decision

1. **Lot generation.** `LotSubdivider` carves one lattice block at a time through `BlockPatterns`.
   The choice is Skylines-style fixed-depth strips per road side, or enclosed faces recovered from
   the road graph. Strips give up courtyard, perimeter and back-garden forms, which read a block's
   interior (`Space/BlockPattern.cs:62-136`).
2. **The saved block.** `BlockTable` saves lattice coordinates and a `Pattern`. `ZoneBlock` and
   `BandBlock` paint a whole block. Decide what these re-key to (road side, Segment or enclosed
   face) or whether they go.
3. **Frontage.** `Frontage.Locate` derives a Lot's Segment and offset from its position on a lattice
   line (`Space/Frontage.cs:140-170`). Either store the Segment on the Lot or find it by nearest
   Segment. Both reopen [`adr/0078`](../docs/adr/0078-frontage-is-derived-on-the-epoch-and-a-lots-width-is-the-segments-own-building-count.md).
4. **Street representation and the edit command.** Pick the curve form for Q16.16 (arcs, polyline
   or control points). The `connect` command packs an axis bit and derives the far endpoint from the
   lattice (`Input/Command.cs:573-599`). A curved Street needs endpoints plus shape, which bumps
   `InputLogCodec.Version` and re-records committed logs. Also decide endpoint snapping, merging and
   mid-Segment splitting. `TripPayload` addresses destinations in lattice blocks.
5. **Junction geometry.** Arbitrary angles need junction polygons. Shared with the Arterials and
   Junction construction row.
6. **Wedge land.** Leave it empty, give it to the adjacent Lot as yard, or allow parks.
7. **Grid tool.** Keep the lattice as a drawing aid alongside freeform, or drop it.

Decisions 1 to 3 are one question in practice: whether a Lot belongs to a block or to a Segment.
The rest follow from it.

## Found in passing

- `TrafficPresence` buckets nodes with `FloorDiv(node.East, block_tiles)`
  (`Space/TrafficPresence.cs:138-141`), not `BlockLattice.LineAt`. On a varying lattice
  (`block_spread_tiles > 0`) this likely puts nodes in the wrong bucket. Not yet confirmed by a test.

## Next step

Decide decision 1. A design session comparing strips with enclosed faces against the current
`BlockPatterns` forms should settle 1 to 3 together.
