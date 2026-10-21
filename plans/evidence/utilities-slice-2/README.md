# Utilities slice 2 — a reserve covering an outage, and refilling after it

A driven run of the shell over the slice 2 demonstration Ruleset. It fills two water towers, cuts
the one road carrying Water into a District, watches three Days run at full delivery on the towers
alone, watches the fourth grade to half and the fifth go dark, lays the road again and watches the
Day after that come back whole with the towers filling.

```sh
godot --path src/Borough.Godot -- --load plans/evidence/utilities-slice-2/reservoir.borough \
  --drive plans/evidence/utilities-slice-2/reservoir.drive
```

## The city

`reservoir.borough` is slice 1's ridge of 8 Cells along `north = 0`, saved at Tick 2,049 on Day 1.
44 Buildings, two Districts, the boundary between Cell 5 and Cell 6. A pump stands in the west
(Building 1, east 1). The one occupied dwelling (Building 37, east 225, two Households) and two
towers (Buildings 38 and 39, east 229 and 233) stand in the east. One Street crosses the boundary.
`reservoir.geometry.txt` holds those Tiles, those ids and the measured arc, and the fixture writes
it beside the save.

`reservoir.toml` is `rulesets/watered.toml` with one number changed — a tower holds 1,024 units
rather than 65,536 — and the fixture writes it beside the save as well. The pump makes 4,096 units
a Day. The east District asks 512 for its two occupants and loses 64 to its 64 Tiles of road, so
its Day costs 576; one crossing carries 2,560, which covers that and leaves 1,984 for the towers.
Two towers at the authored ceiling hold 131,072, which is 67 Days of filling and 228 of draining at
those rates — a demonstration nobody can watch. At 1,024 a tower the reserve fills in two Days and
covers four.

## Trigger, response, consequence

| | | Shot |
|---|---|---|
| Opening state | Day 1 at 100%. The panel reads `District reserve 1,984 / 2,048 units. Yesterday drawn 0, refilled 1,984.` The reserve stands in Buildings, so tower 38's own Stocks line reads `992 / 1,024 units` — half the District's total, because a refill splits in proportion to each tower's free space. | `01`, `02` |
| Filling | The settle at Tick 4,096 finds 64 units of room left and adds exactly that. The towers are full at 2,048. | `03` |
| **Trigger** | Shift-click at east 191 removes the crossing Street, so nothing the pump makes can reach the east District. The reading does not move: the settle at Tick 4,096 already fixed Day 2's fraction. | `04` |
| **Response** | The settle at Tick 6,144 finds a road graph with no crossing and covers the east District's whole 576 out of its own towers. The panel reads `water delivered at 100%` with `drawn 576, refilled 0` beside it, and the wash stays green. **This is the whole difference from slice 1**, where Power declaring `storage = "none"` went to 0% at this same settle and stayed there. | `05` |
| | Two Days further down, 576 a Day — 512 of demand and 64 of road loss — and the panel reads `District reserve 320 / 2,048 units`. Delivery is still whole. | `06` |
| **Consequence** | Day 6 opens on 320 against a need of 576. The settle draws all of it and the Day grades to `water delivered at 50%`. Every Rule in the east applies `floor(count × ½)` times and **none of them stops**. | `07`, `08` |
| | Day 7 opens on empty towers. `water delivered at 0% · no supply shortfalls reported`, the attention card names the Utility, the wash paints the east red against the west's green, and tower 38's Stocks reads `0 / 1,024 units`. The brownout arrives five Days after the road went, and the towers paid for the four between. | `09`, `10` |
| Repair | Two clicks relay the Street from east 160 to east 192. | `11` |
| Recovery | The settle at Tick 16,384 finds the link again. Day 8 is back at 100% and the towers are filling: `drawn 0, refilled 1,984`. | `12` |

Each `.png` has a `.txt` beside it holding the whole readout — HUD, hover, debug panel and every row
of the inspector — so the panel's wording is checkable without opening the picture.
`reservoir.geometry.txt` carries all eight settles as the headless dump prints them, measured on
this world by the fixture that wrote it.

⚠ **The shot names follow the readout's status header, which counts Days from 0.** The bar in the
picture and the Utilities legend count from 1, so `09`'s caption reads `Day 7` where its bar reads
`Day 8 · 05:09`. Slice 1 found the same two numberings in one frame.

## Why the outage is the road rather than the pump

The outage is the District's supply cut off and not the pump demolished, and that is a limit of the
shell rather than a statement about Water. A `demolish` click would remove the pump and nothing in
the shell could put it back: the service tool is the only verb that raises a Building, it refuses a
kind declaring no `serves`, and a pump may not declare one, because `serves = "water"` is a
Networked Service the loader refuses (`adr/0032`). Removing and relaying the one crossing is the
reversible player act with the same effect on the east District, and it is slice 1's trigger — which
is what makes the two runs comparable at all.

## What observation exposed

- **A reserve reads in two places and the two are different quantities.** The dwelling's Utilities
  section reports the **District's** level and capacity; a tower's own Stocks section reports **that
  Building's** Bin. Shots `01` and `02` are one Tick apart and read 1,984 of 2,048 and 992 of 1,024.
  Neither panel says the first is a sum over the second, so a reader who meets one of them alone
  cannot tell which they have.
- **The Building holding the water is the one the wash says nothing about.** A tower draws no
  Utility, so the Utilities wash paints it grey while the dwellings around it go amber and then red
  — the two grey Buildings in the east end of `09`. The wash answers *what is this Building served*,
  and for a tower the answer is nothing, whatever stands in its Bin.
- **`drawn` and `refilled` are a Day's two flows and say nothing about the level.** Shot `03` is the
  fullest the towers ever get and reports the smallest refill of the run, 64, because 64 was all the
  room left. A tower full and untouched for a week reports 0. And `drawn` reads 576 against a Day's
  demand of 512, which is why the line ends `Drawn includes road loss.`
- **A graded Day is where a reserve is legible, and it lasts one Day.** Shot `07` is the only frame
  in the run that is neither whole nor dark. The towers held 320 against a need of 576, and the
  fraction is 50% rather than 56% because the roads take their 64 before anybody is served.
- **An expanded panel section does not survive the panel rebuilding, so a script has to re-assert
  it.** `ui section utilities on` holds until the Building's condition changes; the rebuild that
  follows leaves every section but `Current condition` collapsed. The first run of this
  demonstration photographed the recovery with its Utilities card shut, and the caption beside it
  carried the full text — ***a readout cannot report that the picture it came with is missing the
  panel it describes.*** Every `shoot` here re-asserts both sections on its own Tick.
- **The authored fixture's ceiling cannot be filled by the pump it ships with.**
  `rulesets/watered.toml` sizes a tower at eight Days of a 32-occupant District, and a 32-occupant
  District draws 8,192 a Day against the pump's 4,096 — so no world that file describes ever fills
  one. A fixture for assertions and a fixture for a demonstration want different numbers. This run
  states its own beside its save rather than editing the shared one.

## Regenerating the save

The save, the Ruleset beside it and the geometry are written by an instrument-tier test, so they are
derived rather than authored.

```sh
BOROUGH_RESERVE_CITY=$PWD/plans/evidence/utilities-slice-2/reservoir.borough \
  scripts/test.sh --filter 'FullyQualifiedName~Writes_the_watered_city'
```

The test also runs the arc — the crossing removed, five Days of settle, the crossing relaid — and
writes what it measured into `reservoir.geometry.txt`, so the drive script's Tick landings and the
figures above are read off a run rather than predicted. Without the variable it writes to a
temporary directory and measures the same arc.
