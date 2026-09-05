# 0064 — The information interface

The player chose the spacing of composition B in both light and dark themes. Hover stays in a
fixed lower-left corner. Clicking opens a persistent inspector on the right, with expandable
sections and Building → Household → back navigation. A visible close button and Escape clear
selection. Debug information remains independently available, including Tile/Cell coordinates,
Zone and the technical readout. These decisions supersede the earlier hover position.

`Main.Information` owns presentation and shell-only selection. `Evidence.OfBuilding` and
`Evidence.OfCitizen` supply inspection facts; `Pointing` retains the technical description.
`Main.PickInformation` intersects the drawn Building and road meshes; the nearest surface wins.
Driven Tile clicks cast vertically. Address rings and road highlights distinguish hover and selection.
`Main.RoadInformation` reads directional permissions, travel costs, endpoint connections and frontage
from the Road Graph and Lots. Vehicle counts mean Vehicles present, not flow. Driving estimates
include the next Vehicle entering. Frontage links support Road → Building → Household and back;
connection links select the adjacent Segment. `RoadDebug` retains routing values in the debug overlay.

`LayoutInformation` owns the PROVISIONAL dimensions and breakpoint. Short inspectors scroll beneath
their fixed identity, condition, breadcrumb and close button. Narrow windows use a bottom sheet;
the tool tray remains available through Tools. The minimum window is 480 × 640. `ThemeInformation`
changes colours without rebuilding inspection state; preferences live in `user://information.cfg`.

Supply shortfalls appear in the main explanation. Scheduled activities and waits for output space
belong in Activities: a full stock is not, by itself, a warning. Missing subjects retain an explicit
message instead of resolving a recycled slot. `Main.CloseInspection` also clears selection when the
tuner creates a new World.

## Verification

Build Debug before launching Godot:

```
dotnet build src/Borough.Godot
godot --path src/Borough.Godot -- --ruleset rulesets/diagnosed.toml \
  --citizens 256 --start-at 512 --drive scripts/ui/information.drive \
  --listen /private/tmp/borough-hud.sock
python3 scripts/ui/check-information.py /private/tmp/borough-hud.sock
```

`check-information.py` checks real Evidence, independent hover, both themes, four window sizes,
scroll/section retention, breadcrumb restoration, mouse close with Demolish held, and unchanged
paused State Hash. Captures and state readings go to `artifacts/hud-live/`.

`scripts/ui/information-lifecycle.drive`, launched with the same fixture and `--start-at 4000`,
watches the selected Building become abandoned and be demolished. `DriveScriptTests` checks
round-tripping information actions and the stopped-clock guard. `ui read PATH` exports inspection
state and control rectangles; `ui press X Y` exercises actual viewport input inside visible panels.
`check-roads.py` checks perspective picking, empty ground, road facts, nested breadcrumbs, connection
links, both themes and narrow layouts. `ui map-press X Y` exercises viewport clicks with Look held;
real selections record subject ids. `road-lifecycle.drive` watches removal and replacement without
letting the inspector adopt a recycled slot.

This is the first inspection pass. Citywide dashboards, graphs, notifications, persistent Pins and
new art assets remain outside its scope. The existing Policy and tuner panels are not redesigned.
