# Utilities slice 4 — a District with no plant buys its Power

A driven run of the shell over `rulesets/powered.toml`. It cuts the one road carrying Power into a
District, watches the next Day run at nothing delivered, raises an Outside Connection there, and
watches the Day after that come back whole on imported units the treasury pays for.

```sh
godot --path src/Borough.Godot -- --load plans/evidence/utilities-slice-1/ridge.borough \
  --drive plans/evidence/utilities-slice-4/import.drive
```

## The city

The world is slice 1's ridge, `../utilities-slice-1/ridge.borough`, saved at Tick 4,097: 8 Cells
along `north = 0`, 44 Buildings, two Districts with the boundary between Cell 5 and Cell 6, a plant
in the west (Building 1, east 1) and the one occupied dwelling in the east (Building 37, east 225,
two Households). `../utilities-slice-1/README.md` describes it and says how to rebuild it.

The east District has no plant of its own, so the crossing Street is its only supply. Its Day of
demand is 512 units and its 64 Tiles of road lose 64 more.

`powered.toml` prices Power at 2 per unit in the south Hinterland and gives a gate
`import_per_connection = 2560` a Day, so one Outside Connection covers the whole 576 and the bill
is 1,152 a Day. The city opens with a treasury of 4,194,304 and earns nothing, so the balance falls
by the bill and by nothing else.

⚠ **The ridge has no vacant Lot**, and a gate stands on a vacant edge Lot. The script demolishes the
filler dwelling on Lot 44 at east 253 to free one. That Lot is in the east District's Cell 7 and on
the south edge, which is the edge the Hinterland that prices Power sits behind.

## Trigger, response, consequence

| | | Shot | Tick |
|---|---|---|---|
| Opening state | Day 2, both Districts at 100%. The east is served over the crossing and imports nothing. Treasury 4,194,304. | `01` | 4,097 |
| **Trigger** | Shift-click at east 191 removes the crossing Street. The east District now has no plant, no neighbor and no gate. The reading does not move: the settle at Tick 4,096 already fixed Day 2's fraction. | `02` | 4,101 |
| | The settle at Tick 6,144 finds no crossing and no import source. Day 3 runs at `power delivered at 0%` and the wash paints the east red. | `03` | 6,150 |
| **Response** | Lot 44 is cleared and an Outside Connection opened on it. The gate draws no Utility, so the wash leaves it gray beside the red dwellings. The Day's fraction still does not move. | `04` | 6,156 |
| **Consequence** | The settle at Tick 8,192 buys the shortfall and Day 4 is back at 100% on nothing but imports. The attention card reads `importing 576 gross units at 1,152 money units per Day from the treasury`, and the Utilities section reads `The District imported 576 gross units, including road loss; the treasury paid 1,152 money units`. | `05`, `06` | 8,200, 8,202 |
| | The city budget's `Utility imports · out` row reads 1,152 today against 0 the Day before, the treasury has fallen to 4,193,152, and the panel's own arithmetic line reads `4,194,304 at the start, 0 in and 1,152 out, leaves 4,193,152.` | `07` | 8,204 |
| Recurrence | The settle at Tick 10,240 buys the same Day again. The running column reads 2,304 and the treasury 4,192,000. The bill is a standing cost of having no plant. | `08` | 10,248 |

`importing.borough-city` is the end state at Tick 10,250, written through the menu at save format
28. Opening it gives back the city the last two rows are about — the treasury at 4,192,000, the
gate standing on Lot 44 and the dwelling's panel still reporting 576 gross units bought for 1,152
— without replaying the run. ⚠ **The budget panel's own history does not survive the save.** Flows
are an instrument's and are not saved, so a reopened city opens its account at the Tick it was
loaded at and the running column starts again from zero there.

Each `.png` has a `.txt` beside it holding the whole readout — HUD, hover, debug panel and every row
of the inspector. The two budget shots also have a `.json` from `ui read`, because **a `Control` is
invisible to a caption**. The budget panel's figures are in the picture and in that file's `Fonts`
array, and in no `.txt` here.

## What observation exposed

- **The attention line and the panel line say different things on purpose, and both are needed.**
  The Utilities section reports the District's import as a quantity and a payment; the attention
  card says the Building depends on it. A District at 100% on imports reports no shortfall anywhere
  else, so without the attention row the dependence is invisible — which is the same hole slice 1
  found for a brownout, in the opposite direction.
- **Gross imports exceed what the Building is served**, and the wording has to carry it. 576 units
  are bought and 512 reach the dwelling; the 64 the roads take are bought at the same price. A
  reader who assumed `Imported` was a part of `Received` would read the panel as inconsistent.
- **The import is attributed to the District and the cost to the city.** The gate stands in Cell 7
  and nothing on it is charged; the units are charged to the District they were delivered to and
  paid for out of the one treasury.
- **`ui file-path` resolves a relative path against the Godot project directory**, not against the
  repository root that `shoot`, `readout` and `draw` use. A repo-relative path there silently saves
  the basename into `src/Borough.Godot/`.
- **The menu stops the clock**, so a driven save must be the last thing a script does. `_menuOpen`
  gates the step at `Main.cs`, so a script that opens the menu and then names a later Tick hangs
  rather than failing.
- **The picker's buttons are pressed at their unlaid coordinates.** A `--drive` script applies every
  command of a Tick in one frame, so the dialog the press lands on has never been laid out. Its
  Save button sits at the dialog's origin plus (44, 614) rather than where the picture shows it,
  and `ui read` reports those unlaid rects too. Hence `ui size 1440 960` before the save, which
  fixes the centered dialog's origin on any screen.
- **The inspector is anchored over the budget panel's figure columns.** The first run's budget shot
  showed the row labels with the numbers hidden behind the Building panel, so the script closes the
  inspector before photographing the budget.
