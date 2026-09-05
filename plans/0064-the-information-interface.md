# 0064 — The information interface

## Work queue

This plan owns the UX work queue. Keep outstanding work, agreed scope changes and completion checks
here as decisions develop; recommendations in conversation must be carried into this queue.
Queued work starts with design agreement before implementation.

| Order | Status | Work | Completion check |
|---|---|---|---|
| 1 | Complete | Fixed-corner hover; Building and Household inspectors; expandable sections, breadcrumbs and mouse close; independent debug; light/dark themes and responsive layout | `check-information.py` and `information-lifecycle.drive` below |
| 2 | Complete | Road inspection, surface picking, directional travel conditions, endpoint links and frontage navigation | `check-roads.py`, `check-road-types.py` and `road-lifecycle.drive` below |
| 3 | Complete | Everyday HUD controls: agree placement and hierarchy for time, map layers and active-tool feedback as one composition | Reviewed in both themes at 1440 × 960 and 480 × 640. **Composition C, the console, is chosen**, with the pointer reading folded into it |
| 4 | Complete | Visible pause/resume and speed buttons with an unmistakable current state | `check-console.py` below |
| 5 | Complete | Named map-layer picker and persistent legend | `check-console.py` below |
| 6 | Complete | Clear active tool, mouse-accessible cancel and readable action refusals | `check-console.py` below |
| 7 | Later: design | Citywide “what needs attention,” with links into the existing inspectors | Agree which evidence qualifies, how repeated issues are grouped and how resolved issues leave the view; demonstrate discovering an issue and opening its affected subject |

Rows 4–6 were one implementation slice and it is built. Its shared acceptance check — pause, change
speed, choose a map layer, inspect a subject and cancel an editing tool entirely with the mouse, in
both themes and at narrow sizes, with hover, inspector navigation and the independent debug overlay
intact — is `scripts/ui/check-console.py`, and it presses real button rectangles rather than issuing
the actions, so a control that is drawn but unreachable fails exactly as a missing one does.

Broader dashboards, graphs, notifications, persistent Pins, Policy/tuner redesign and new art assets
remain deferred. They are not commitments in this queue; promote them explicitly when a concrete
player task calls for them. Row 7 expands the follow-up scope beyond the completed inspection pass.

## Agreed design and implementation

The player chose the spacing of composition B in both light and dark themes. Hover stays in a
fixed lower-left corner. Clicking opens a persistent inspector on the right, with expandable
sections and Building → Household → back navigation. A visible close button and Escape clear
selection. Debug information remains independently available, including Tile/Cell coordinates,
Zone and the technical readout. These decisions supersede the earlier hover position.

**Row 3, 2026-09-05. The everyday controls are one console.** The top bar and the bottom tool
palette become a single strip along the bottom: time at the left, tools in the centre, the map-layer
picker and its legend at the right, chrome folded behind one button, and a refusal along the bottom.
The top edge belongs to the picture. Three placements were drawn over the same city in the same two
states — one top bar that grows, one home each, and this console — and the console was chosen.

**The default view is what the condensing was for.** The shipped chrome is an 84 px bar plus a
116 px palette: 200 px on every frame before anything has happened. Idle, the console is 52 px. The
tool tray drops its hint line until a tool is armed, the layer control is one button until a layer
is on, and Cancel appears only when there is something to cancel. Nine ladder rungs are driven by a
pause button and a stepper, not nine buttons.

**Time reads arc-first.** A sun arc over a horizon with the sky filled beneath it, the spent share
of daylight solid and the rest faint, then the named phase, then the clock in muted type. Below the
horizon the same arc carries the moon. `docs/01 §7` says there is no hour and no minute; the build
prints one; both appear, the arc leading. The five phase names in `01 §7` have no boundaries
anywhere in the build, so the arc's ticks carry no labels.

**Settled: the pointer reading is a row inside the console.** Four answers were drawn; the fourth is
built. The 336 × 142 card is gone and its sentence is one line along the console's foot, under a
fixed `UNDER POINTER` caption that drops below 380 px of console width. **This supersedes row 1's
*hover stays in a fixed lower-left corner*** — a supersession rather than a refinement — and it is
why the pointer synopses were compacted to a single line each (`Synopsis`, `RoadSynopsis`).

**Built 2026-09-05, and what the condensing actually bought.** Idle, at 1440 × 960, the console is
**94 px against the two strips' 200 px**. ⚠ **The gain is a property of the window and not of the
design**: 138 px at 1024 × 640, 137 px at 640 × 720 and **205 px at the 480 × 640 minimum**, where
the five groups wrap onto four rows and the console is a third of the frame. It is measured with no
layer, no tool and no refusal — a layer adds its legend, a tool adds its hint and a refusal adds its
strip, and each is a state the player asked for. `plans/0012` **Cause 5**: these are heights of *this
console at these window sizes*, and none of them is *the HUD's height*.

**Four things the live build changed against the drawing.** Each was a defect the mockup could not
have: a wrapping `Label` in a flow container reports a one-character minimum width, so every console
reading is a non-wrapping Label and the console was 460 px until it was; the tool tray and the legend
each state a minimum width so the flow wraps around them rather than clipping them, and at 480 px
each of those numbers is wider than the console, so both are capped against the window every frame;
`Cancel` is first in the choices row and not last, because that row scrolls and `DEMOLISH`'s hint is
a full sentence; and the pace steppers are doubled arrows, because a paused console read
`▶ ◀ paused ▶` with two different verbs wearing one glyph. **The console sizes itself** — anchored to
the bottom edge and grown upward — because a height computed in `LayoutInformation` is always one
frame behind the change that caused it, which is exactly the frame a driven `shoot` catches.

**The chevrons are `▾` and `▸` rather than `⌄` and `›`.** The old pair sits on the text baseline and
read as dropped below the section title beside it.

**Not decided here.** A waxing and waning moon was raised and is not taken. `Main.Sky.cs:302` aims
one light from the sun's antipode at full energy every night — its own remark calls that *"a full
moon every single night, and is a fib"* — so a crescent in the HUD would contradict the picture.
It becomes honest only when `Daylight()` scales the moon's energy by the same phase, and the cycle
length is an authored constant. That is a row of its own.

`Main.Console` owns the console: the pace group, the sky arc, the tool slot, the layer picker and
its legend, the chrome, the pointer row and the refusal strip. `SkyArc` draws the day — the light's
position and the spent share of daylight are read off `Ticks.MinuteOfDay`; **the two horizon
crossings are a drawing convention at 06:00 and 18:00 and not the sun's real height**, and the
quarter ticks carry no labels because `01 §7`'s five phase names have no boundaries anywhere in the
build. `PhaseOfDay`'s boundaries are PROVISIONAL and chosen by taste. `Main.Panels` builds the tool
palette into the console's centre slot rather than a strip of its own, and `Panels()` now re-enters
`ThemeInformation()` — a Ruleset reload rebuilt both panels in the default Godot colours until the
next theme toggle.

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
`scripts/ui/check-console.py`, against the same fixture and socket, checks mouse-only pace through
the pause button and both steppers, the layer picker and its legend, that a debug wash is absent from
the picker while debug is off, arming and cancelling a tool, a refusal that reads in place while the
inspector stays open and un-covered, and a bounded console clear of the inspector in both themes at
1440 × 960, 1024 × 640, 640 × 720 and 480 × 640 with the paused State Hash unchanged throughout.

`check-roads.py` checks perspective picking, empty ground, road facts, nested breadcrumbs, connection
links, both themes and narrow layouts. `ui map-press X Y` exercises viewport clicks with Look held;
real selections record subject ids. `road-lifecycle.drive` watches removal and replacement without
letting the inspector adopt a recycled slot.
