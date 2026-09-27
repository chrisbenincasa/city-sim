# Starter prompt — commercial Lot types, phase 2

Let's build phase 2 of [the commercial Lot types plan](../commercial-lot-types.md): Units and the
car-park centre. This is an implementation session. The design is settled, so build rather than
redesign, and bring me only the decisions the code forces.

Work in the `commercial-lot-types` worktree on its branch. Follow CLAUDE.md and PROCESS.md: check
Git status, recent commits, `git worktree list` and the board row before claiming the work. Claim
the "Commercial Lot types and trade forms" row as phase 2 in progress. Leave the `pictured-jobs`
worktree's uncommitted prototypes alone; read them for reference only.

## Read first

- `plans/commercial-lot-types.md`: the Units, Jobs, World creation, Storeys and parking sections,
  the geometry contract, the phase table and the Interfaces table.
- Issue #63 (`gh issue view 63`).
- CONTEXT.md: Building, Business, Car Park, Unplaced Pool.
- The code the phase touches. Verify each against the current tree, because descriptions go stale:
  - `World.TryDeclaredJobs`, `World.Fit`, `World.CreateBuilding`, `World.Premise`/`Unpremise`,
    `World.EvictOverflow`, `World.LosingTenant`
  - `BusinessTable`, `BuildingTable`, `LotTable`, `CarParkTable`
  - `BlockPatterns.ForBand`, `LotSubdivider`, `SyntheticCity.RaiseDwellings`
  - `DistrictWatershed.HeldForTrade`

## Settled; do not reopen

- A Unit is one tenancy's space inside a Building: a rectangle within the footprint, a first
  storey, a storey span, a facing side and an anchor flag. Units are an intrusive list per
  Building in flat arrays. A Business's premises name a Building and a Unit.
- Existing kinds (`dwelling`, `shopfront`, `officeblock`, `school`, `college`) hold one equal Unit
  per tenancy. Households and Businesses still compete for the same tenancies. Storey and side
  carry no meaning on those Units.
- A Business's posts are its Unit's floor over `[capacity] floor_tiles_per_job`. `pictured.toml`
  states 1. Every other Ruleset keeps 3, because their floors are about 4× a real building's. Do
  not retire the key. The reason and the measurement are in the plan's Jobs section.
- `pictured.toml` declares no bands, so every trade block becomes a car-park centre.
- Margin-driven staffing, the opening staff, hire and cut thresholds, anchor catchment and
  re-letting belong to the Employment feedback row. Do not build them here.

## Order of work

1. **Diagnose #63 before building anything.** Name the cause with a measurement: job-to-home
   distance, route cost, parking search or budget size. Then fix it or state why the failures are
   correct. The prototypes in `pictured-jobs` reproduce it. If the fix belongs outside this plan,
   file it and continue.
2. **The Unit table.** Add the table, point premises at a Unit, and make placement and eviction
   match a Business to a free Unit. Derive posts from the Unit's floor. Existing worlds should
   keep their behaviour. Show that with replay, save/reload and thread-count equivalence and
   `DerivedRebuildAuditTests` before going further. Write the CONTEXT.md Unit entry, noting that
   GlassBox used "Unit" to mean a Building.
3. **The car-park centre.** Add the block pattern, the rear footprint, the Unit row of mixed widths
   with an anchor at one end, and the stall layout: 2.5 × 5 m stalls, 6 m aisles, double rows
   perpendicular to the Unit row. Capacity is the stall count. One Car Park row per Building.
   Settle the new meaning of `DistrictWatershed.HeldForTrade` first, because merging trade Lots
   changes it.
4. **World-creation raising.** The populator raises each trade block's centre and fills its Units
   with Businesses, as it raises dwellings now.

## Working rules

- Serena may be bound to another worktree. Check where its first edit lands, with `git status`, and
  fall back to the built-in editor if it edits elsewhere.
- Use TDD for the Unit table and placement matching. Use `scripts/test.sh <Area>` while iterating.
  Run the full suite at the end of each numbered step.
- Tuning numbers go in the Ruleset. Stall and aisle sizes are real-world dimensions, so state them
  in metres and derive Tiles.
- Re-record goldens once, deliberately, with the procedure in `tests/Borough.Tests/Golden/README.md`.
- Use the drive skill for the screenshot. The graphics session owns the Blender models. Draw
  centres as plain placeholder geometry until phase 3's facts exist.

## Acceptance

- `pictured.toml` at 2,000 Citizens employs a large share of working-age Citizens at world
  creation. Report employed, posts and working-age counts, and commutes over budget, all over 20
  Days. Compare them with the phase 1 baseline: 1,023 of 1,025 Businesses founded without
  premises, and no job assignments.
- A driven screenshot shows centres with their car parks.
- Replay, save/reload and thread-count equivalence hold, and `DerivedRebuildAuditTests` passes.

Commit each numbered step separately. End with a short account of what shipped, the #63 finding,
the measurements, and any decision that needs me.
