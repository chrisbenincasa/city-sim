# Dense blocks: a 64 m grid with merged 128 m blocks

Board row 52. Worktree `dense-blocks`.

## Outcome

The generated city lays a 64 m street grid. A dense core and scattered blocks outside it keep every
grid line. Elsewhere the generator leaves out the middle lines, so most of the city keeps 128 m
blocks with a garden interior (PR #192). The layout was chosen from the 10/17/2026 block-interior
samples.

## Why not the varied lattice

`BlockLattice.Varied` changes the width of whole rows and columns. A narrow column makes a strip of
oblong blocks across the map, so it cannot make isolated 64 m blocks.

## Current behaviour

- Zoning paints the whole enclosed face (`World.TryZoneGround`).
- `LotSubdivider.GatherBlock` carves one lattice square, with one Segment per face. `FaceSides`
  refuses a merged face, and a face with no Street carves no Lots.
- `(column, row)` names a block in `Frontage.BlockOf`, `World.BlockGroundRectangle`,
  `World.ZoneBlock`, `LotSubdivider.Preview` and the shell's `ZoneInterior`.
- `RoadGenerator.LayStreets` lays every lattice line.

## Steps

1. **Block spans in the core.** A block is the rectangle of lattice squares its Streets enclose,
   named by its south-west square. Subdivision carves the whole span, and each parcel fronts the
   Segment under its offset. A square whose component is not a rectangle carves alone, as today.
   Shipped Rulesets have only one-square blocks, so no State Hash moves.
   Check: on a 16-Tile lattice with the middle Streets removed, zoning a 2×2 span carves Lots on all
   four faces at the 128 m block's depths and leaves its interior free.
   Built: `StreetGrid.Block` finds the span, up to `SpanLimit` (4) squares a side.
   `MergedBlockTests` checks a 2×2 block on `rowhouses.toml`. Its house plots have a fixed 12 m
   depth, so the test checks both Segments of each face instead of depth.
2. **Generator.** New `[roads]` keys choose which grid lines to lay: a dense core radius and an
   outward probability of keeping a 64 m block. `base/city.toml` moves to `block_tiles = 16`.
   Built: `dense_core_percent` is a share of the town's radius, from `SyntheticCity.PavedTiles`,
   because base paves the whole map and a fixed distance put every merged block outside the town.
   The merge chance rises to `merged_block_percent` at the town's edge. A merged block draws its
   building pattern and height from the ladder of its own size. A group an Arterial crosses never
   merges, and `woodland_merge_percent` raises the chance on wooded ground. Water and hazard are
   not generator inputs yet. Base ships core 25%, merged 70% and woodland 30%, chosen from the
   10/18/2026 samples (option D).
   A player Street splits only where another Street meets it, so the founding's 128 m Streets on
   the 64 m lattice span two edges each. `StreetGrid` now holds a straight Street on every edge it
   spans, and frontage offsets count from the Street's own start (`StreetGrid.RunStart`). Without
   this the founding carved its blocks as loose roadside and housed 219 Households by Day 45
   instead of 786.
   The goldens and the founding package hash were re-recorded with this step. Walk search and
   road-graph size are measured in `plans/0013` ("The 64 m base grid"): 4.0× the nodes, 2.3× the
   Segments, and about 3× the settled nodes per walk at the same distance.
3. **Shell.** `ZoneInterior`, `Plots`, the readouts, the gardens and `BuildingFacts.IsCorner` use
   the span. `IsCorner` reads only the lattice, so it calls a Lot on an inner grid line a corner.
   Built: the zoning stroke visits each block once and sends one `zone` at its south-west Tile.
   The cursor box, interior clip, hover face count, tree yard test and rung overlay read
   `StreetGrid.Block`. The Street tool keeps `block_tiles` spacing because the lattice is still
   64 m. `scripts/ui/check-merged-blocks.py` asserts the hover and plot draw list on a hand-laid
   128 m block. Gardens read the span through `ZoneInterior` (PR #192).

## Open questions

- The 54 other Rulesets stay at `block_tiles = 32` unless a reason appears to move them.
- Whether a span larger than 2×2 squares is ever generated.
