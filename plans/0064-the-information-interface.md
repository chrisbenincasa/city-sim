# 0064 — The information interface

## Work queue

This plan owns the UX work queue. Keep outstanding work, agreed scope changes and completion checks
here as decisions develop; recommendations in conversation must be carried into this queue.
Queued work starts with design agreement before implementation. The player's 2026-09-06 expansion
below is the next proposed sequence; individual open choices do not block independent work.

| Order | Status | Work | Completion check |
|---|---|---|---|
| 1 | Complete | Fixed-corner hover; Building and Household inspectors; expandable sections, breadcrumbs and mouse close; independent debug; light/dark themes and responsive layout | `check-information.py` and `information-lifecycle.drive` below |
| 2 | Complete | Road inspection, surface picking, directional travel conditions, endpoint links and frontage navigation | `check-roads.py`, `check-road-types.py` and `road-lifecycle.drive` below |
| 3 | Complete | Everyday HUD controls: agree placement and hierarchy for time, map layers and active-tool feedback as one composition | Reviewed in both themes at 1440 × 960 and 480 × 640. **Composition C, the console, is chosen**, with the pointer reading folded into it |
| 4 | Complete | Visible pause/resume and speed buttons with an unmistakable current state | `check-console.py` below |
| 5 | Complete | Named map-layer picker and persistent legend | `check-console.py` below |
| 6 | Complete | Clear active tool, mouse-accessible cancel and readable action refusals | `check-console.py` below |
| 8 | Complete | Larger text and shared sizing; keyboard help; visible camera controls | `check-discovery.py`; both themes at 1440 × 960 and 480 × 640, including 150% text and preference restoration after restart |
| 9 | Direction agreed; interaction design next | Expandable tool browser, tool icons and contextual options; paint-to-zone with automatic subdivision | Choose, preview, paint and cancel without shortcuts; adding tools does not widen the console |
| 10 | Proposed | Game menu: Settings, Save, Load, Quit, Help and Credits | Save/load continues the same city; failed loads preserve it; menu is usable entirely with the mouse |
| 11 | Proposed | Citizen names and sex; Life Stage in inspection | Stable identity across replay/load and row reuse; inspection updates when the Household changes Stage |
| 12 | Proposed | Vary Building setbacks within the authoritative footprint model | Visible variation without parcel escapes or disagreement between drawing, picking and occupied ground |
| 7 | Later: design | Citywide “what needs attention,” with links into the existing inspectors | Agree which evidence qualifies, how repeated issues are grouped and how resolved issues leave the view; demonstrate discovering an issue and opening its affected subject |

Rows 4–6 were one implementation slice and it is built. Its shared acceptance check — pause, change
speed, choose a map layer, inspect a subject and cancel an editing tool entirely with the mouse, in
both themes and at narrow sizes, with hover, inspector navigation and the independent debug overlay
intact — is `scripts/ui/check-console.py`, and it presses real button rectangles rather than issuing
the actions, so a control that is drawn but unreachable fails exactly as a missing one does.

Broader dashboards, graphs, notifications, persistent Pins and tuner redesign remain deferred.
Rows 8–12 promote the requested interface work, including tool icon assets and access to Policies.

## Proposed next passes — 2026-09-06

These rows cover the player's ten requests. Row 8 is built; rows 9–12 remain planned. Preserve the
chosen bottom console and both themes. Row 9 starts with the focused tool and Zoning design choices below.

**8 — Readability and discovery (requests 2, 6, 8).** Increase text by roughly 15–20%, PROVISIONAL,
and use shared typography roles across the console, inspector, tooltips and auxiliary panels.
`Main.Information.InformationLabel` and the debug label currently override the shared sizing;
changing `Main.Readout`'s point constants alone will miss them. Persist a text-size preference and
resize containers with it. Add a scrollable Help window opened by `?` (including Shift+/), with
mouse gestures and shortcuts grouped by task. Escape closes the topmost window first; typing in
an input must not fire game shortcuts. Keep shortcut descriptions and bindings together.
Add a compact camera group with rotate left/right, tilt up/down and zoom in/out; show shortcuts
in tooltips and reuse the existing camera actions. Help and modal panels must consume clicks.

**9 — Tools that can grow (requests 3, 4, 5, 9).** A compact Tools button opens a browser above the
console. Categories: Roads, Zoning, Services and Policies. The console retains the selected tool
and Cancel; the browser owns tool choices and their contextual options. Services can gain Schools,
Health, utilities and other special Building kinds as they become available. Use scrollable groups
and search when the list warrants it; do not put every future tool permanently in the console.
Policies opens its own panel. Keep developer Ruleset editing separate from ordinary tool options.

Use a consistent SVG icon family with visible labels, selected states and shortcut tooltips;
icons alone are insufficient. A tool definition should supply its label, icon, category,
availability, shortcut, options and action so browser and help cannot quietly disagree.

**Agreed 2026-09-06:** replace Subdivide with the familiar paint-to-zone interaction. The player
chooses permitted development and paints land; subdivision happens automatically. This supersedes
the proposed block-click interaction above. `LotSubdivider.SubdivideAt` is an implementation starting
point, not a constraint on the brush: it currently creates Lots and does not repaint claimed frontage.

Tools needing behavioural decisions get focused design sessions before implementation. For Zoning,
settle brush shape/size, snapping and paintable ground, drag preview and commit/cancel, erasing,
rezoning and treatment of existing Buildings. Preserving existing Buildings while new permissions
govern future development remains a proposal. These details are open; the paint-to-zone direction is
settled. Refusals and no-ops must explain the affected ground, not expose bitmasks or TOML declarations.
Other tools' behaviour is not approved merely by assigning them a category in the browser.

**10 — Game menu (request 10).** A visible menu button opens Save, Load, Settings, Help, Credits and
Quit. Settings contains text size and theme initially. Opening this menu pauses the city and closing
it restores the previous pace. Help alone leaves the pace unchanged. Load and Quit protect unsaved
progress; saving reports success only after writing completes. Use the Core save mechanism already
used by `Borough.Headless.Session`, with a file picker, explicit errors and safe replacement of saves.
Validate a loaded world before replacing the live one, then rebuild rendering and clear stale
selection. Preserve the required Ruleset/name data. Credits should read a maintained asset list
with creator, source and licence information, including the 3D models, and scroll as it grows.

**11 — People (request 7).** Draw name identity and sex at Citizen creation, including arrivals and
births, through deterministic distinct draws. Keep numeric identity in Core and name text in content
resolved by the shell. Persist identity so a renamed/reordered name pool cannot rename an existing
Citizen. Names need not be unique. Sex is descriptive in this pass, with no new behavioural effects.
**Open:** the setting of the first name pool; contemporary English-language names are the provisional
fallback. Sex categories and distribution need a concrete content choice before this row starts.
Show names consistently wherever Citizens appear. Show the current Household Life Stage under that
label: `HouseholdTable.LifeStage` owns it, and `CitizenTable.Age` is not a live age clock. Do not invent
a personal age or Stage. Undeclared Life Stages get an explicit unavailable reading.

**12 — Setbacks (request 1).** Vary front-wall distance by built form, with coherent attached
frontages and more variation among detached Buildings. Ranges are PROVISIONAL. First trace footprint
derivation and the imported-model placement in `Main.Assets`; `Main.Massing.Buildings` reads the
Lot footprint directly. Keep the footprint within its parcel and preserve coherent corner geometry.
Do not shift only the mesh. Prefer shifting an unchanged footprint where room permits; any resizing
must also update the simulation quantities derived from its area. Variation must survive reload and
remain stable while the Building stands. Review street-level and wider views before tuning further.

**Checks per pass.** Build Godot Debug and use the drive skill to inspect the result. Extend the
existing real-button UI checks for new interactions; check both themes at 1440×960 and 480×640,
including enlarged text, scrolling, focus, Escape, tool cancellation and inspector coexistence.
Exercise Save → Load → continue against an uninterrupted run. Identity and footprint changes need
targeted determinism, save/reload and invariant checks. Run `scripts/test.sh` before committing;
update golden fixtures through their recorder when simulation changes move the State Hash.

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

## Row 8 — implemented 2026-09-06

`Main.Discovery` owns shared typography roles, the saved text-size preference, Help and the shortcut
catalogue used by keyboard actions and camera buttons. The default is 118%, PROVISIONAL; Help offers
100–150% and Reset size. Settings can reuse this preference when row 10 is built. Help leaves pace
unchanged, consumes map clicks, and keeps its heading and size controls above scrolling content.
Escape closes Help, then the frontmost auxiliary panel, then inspection, then the editing tool.
Focused text inputs retain their keys. Auxiliary panels now scroll within the viewport. An overfull
console scrolls its controls vertically while reserving inspection space. The pointer reading and
refusal stay fixed beneath them; the existing tool tray still scrolls horizontally.

`check-discovery.py` exercises actual button rectangles, injected keys and wheel input through the
socket. It checks camera movement, text changes, Help scrolling and shielding, focused input, Escape
order, inspector coexistence, both themes and window sizes, Help at a running pace, and an expanded
console with tools and layers at maximum text size. Run it with
an additional `128` argument after restarting Godot to verify the preference it leaves saved; that
check restores 118%. `check-console.py` and `check-information.py` also passed, as did the Godot Debug
build and the assertion lane. Captures are under `artifacts/hud-live/discovery-*.png`.

At 480 × 640 with 150% text, the inspector and console remain bounded but leave little city visible.
The scalable tool browser remains row 9; this pass retains the existing tool tray.

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


## Review — pace, clock and visual hierarchy

The player requested speed at the left, play/pause between the slower and faster controls, a
highlighted pause state instead of a separate “paused” label, and removal of “UNDER POINTER.”
The continuous day/night path is agreed: dawn at the left, daylight above the horizon and night
below. It is a clock diagram, not the moon’s physical position in the sky.

Typography is reduced to metadata, body/controls and titles. The player selected **Civic blue**
from `artifacts/visual-study/interface-palettes/index.html`. `InformationUi` owns the shared light/dark
palette, text roles, spacing, buttons, links and section cards; the inspector and Help compose those
components. New screens inherit the shared Theme. Blue marks sections and active controls; amber
marks blocked activity and refusals.

`check-sky.py` checks the marker at 05:00, 06:00, noon, 18:00 and midnight in fresh driven runs.
The console check also asserts pace order, pause highlight and absence of the removed captions.
Console scrollbar space is reserved even when hidden: changing its available width while the
layer picker wrapped produced an unbounded Godot layout queue at the new text sizes.
