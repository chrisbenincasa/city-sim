# Player construction tools

## Outcome

A player founds a city at playable speed with the construction tools: Street, Zone, Demolish,
service and gate placement.

## Acceptance

- The `scripts/ui/founding.drive` city founds in **at most 10 placement gestures**: a 4×4 block
  grid, housing with two trade blocks, one gate and one school. The script itself uses 38.
  Provisional; adjust after the hand-played founding.
- Every refusal met during the founding is predicted by the hover before the click.
- A gesture count is checked by a driven run; wall-clock time is not a criterion.

## Findings

- 10/13/2026, driven founding from `--empty`, seed `0x135283C`, 2000 Citizens: one Grid Street
  gesture, three zone drags, a gate click and a school click found the `founding.drive` city in
  **6 gestures and 8 clicks**, with 16 blocks and 128 Lots. The current tools meet the count.
- A queued Command steps one Tick even while paused (`Main.cs`, `_owed`), so Lots from a paused
  zone stroke exist on the next frame. A drive script that places on the zoning Tick is refused
  "no vacant Lot in this Cell"; place one Tick later, as `founding.drive` does.

## Decisions

### Undo reverses a construction gesture with the opposite Commands

- The shell keeps the last 16 construction gestures (provisional) and undoes them newest first.
  No redo.
- An undo sends the opposite Commands through the ordinary path: remove the Streets the gesture
  laid; repaint each block or face with the permission it held before. They enter the Input Log
  like any Command, so replay and determinism are unchanged and Core needs no new Command.
- An undo is offered only when the reversal is exact. It is refused, with a reason, once the
  gesture cleared Buildings or a Building has since been raised on one of its Lots.
- Undoing a zone stroke restores the permission; Lots it cut stay cut.
- Undo covers Street gestures and zone strokes. Service and gate placements are corrected with
  Demolish and Shift-click: a placement's price leaves the city, so no reversal is exact.
- Removing a Street does not join the halves of a Segment an earlier gesture split, so a gesture
  that split a Street is refused rather than approximated (10/13/2026).
- Undo never reaches past a construction act it cannot reverse. A Demolish, a Street removal, a
  service placement and a gate placement each push an entry the undo refuses with a reason, and so
  does any gesture the shell cannot record exactly (10/13/2026).
- One Zone Command paints one permission over the whole ground it covers, so a stroke that painted
  over mixed permissions is refused; the earlier mixture cannot be restored (10/13/2026).
- Rejected: rewinding to a saved state, because the city keeps running between gestures and a
  rewind discards what Citizens did meanwhile.

### The zone preview draws the Lots a stroke would create

- Every zone mode draws the Lots before commit, and the hover states how many it creates.
- Whole-block mode draws each block's parcels with `LotSubdivider.Preview`, as parcel mode does.
- Freeform faces and Street sides need a read-only freeform carve in Core that returns the plots
  without claiming them. Read-only, so no State Hash change.
- The shell caches the freeform result per hovered ground and road version, as the freeform wash
  does, so #129's per-Lot scan runs once per change and not per frame.

### A plot that crosses another Street is dropped

- The freeform carve's conflict rule treats another Street's carriageway like an existing Lot. A
  plot crossing it is dropped and its ground stays leftover ground.
- Fixes the acute-corner defect in [freeform Streets](freeform-streets.md): such a Lot was only
  partly painted and never built.
- Changes the carve, and so the State Hash, on ground with acute corners. Check the golden session
  for acute corners before re-recording.

### Leftover ground stays open ground

- Wedges and interiors stay open ground. The Lot preview shows the gaps before commit.
- What the ground becomes (yards, courtyards, plazas, parks) belongs to the board row
  "Building entrances and usable open ground".

## Slices

1. **Acute-corner drop.** Core carve change and golden check: [#152](https://github.com/chrisbenincasa/city-sim/issues/152).
2. **Whole-block Lot preview.** Shell: draw parcels and the Lot count in whole-block mode.
3. **Freeform Lot preview.** Core read-only carve, then the shell draws it with a cached result.
4. **Undo.** Shell: the gesture stack, the opposite Commands and the exactness refusals.

Each slice ends with a driven run; the last repeats the 6-gesture founding with a mistake undone.

## Out of scope

- Density in the Zone tool and re-cutting Lots on repaint: [0062](0062-the-urban-fabric.md#decisions-still-needed).
- A construction charge for laying a Street: the infrastructure construction row.
