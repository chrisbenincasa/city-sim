# Utilities slice 1 — a brownout and its recovery

A driven run of the shell over `rulesets/powered.toml`. It cuts the one road carrying Power into a
District, watches the next Day run at nothing delivered, lays the road again and watches the Day
after that come back whole.

```sh
godot --path src/Borough.Godot -- --load plans/evidence/utilities-slice-1/ridge.borough \
  --drive plans/evidence/utilities-slice-1/brownout.drive
```

## The city

`ridge.borough` is a ridge of 8 Cells along `north = 0`, saved at Tick 4,097. 44 Buildings, two
Districts, the boundary between Cell 5 and Cell 6. A plant stands in the west (Building 1, east 1)
and the one occupied dwelling stands in the east (Building 37, east 225, two Households). One
Street crosses the boundary. `ridge.geometry.txt` holds those Tiles and the fixture rewrites it
beside the save.

`powered.toml` gives Power `link_per_crossing = 2560` and `loss_per_road_tile = 1`. The east
District's whole Day of demand is 512 units, so one crossing carries it and no crossing carries
none of it.

## Trigger, response, consequence

| | | Shot |
|---|---|---|
| Opening state | Day 2, both Districts at 100%. The wash paints every Building green and the panel reads `power delivered at 100%` over `512 of the 512 units asked`. | `01`, `02` |
| **Trigger** | Shift-click at east 191 removes the crossing Street. The reading does not move: the settle at Tick 4,096 already fixed Day 2's fraction. | `03` |
| **Response** | The settle at Tick 6,144 finds a road graph with no crossing. The east network delivers 0 and the west stays whole. | `04`, `05` |
| **Consequence** | Every Rule in the east applies `floor(count × 0)` times and **none of them stops**. The panel's condition line reads `power delivered at 0% · no supply shortfalls reported`, the attention card names the Utility, and the wash paints the east red against the west's green. | `04`, `05` |
| Repair | Two clicks relay the Street from east 160 to east 192. | `06` |
| Recovery | The settle at Tick 8,192 finds the link again. Day 4 is back at 100%. | `07` |

Each `.png` has a `.txt` beside it holding the whole readout — HUD, hover, debug panel and every
row of the inspector — so the panel's wording is checkable without opening the picture.

## What observation exposed

- **A Lot built in code stands on one Tile**, and the shell's massing needs 6 m of frontage. The
  first run of this demonstration drew `Buildings 44 (0 drawn)` and the wash was invisible over an
  empty field. The fixture now states 3 × 3 Tiles of ground per Building. Occupancy counts
  occupants rather than places, so the wider ground moved no number the tests assert.
- **`shoot` holds one pending path and takes the picture on the next frame.** Two `shoot`s on one
  Tick throw the first away, and a camera moved after a `shoot` on the same Tick lands in that
  shot. Hence the Tick gaps in `brownout.drive`.
- **Clicking the dwelling's Tile selects the road**, because the Lots sit on the road line at
  `north = 0`. The script selects with `ui building 37`.
- **The HUD clock counts Days from 1 and the readout's header counts from 0**, so one picture
  carries `Day 4 · 05:04` in the bar and `Day 3` in the caption. Same Day.
- **A brownout appears in no field a stopped Rule fills.** `No supply shortfalls reported` is true
  of a Building running at nothing delivered, which is why the condition line carries the
  delivered clause and the attention card has a row of its own.

## Regenerating the save

The save is written by an instrument-tier test, so it is derived rather than authored.

```sh
BOROUGH_UTILITY_CITY=$PWD/plans/evidence/utilities-slice-1/ridge.borough \
  scripts/test.sh --filter 'FullyQualifiedName~Writes_the_ridge_city'
```

Without the variable the test writes to a temporary directory and asserts the same things.
