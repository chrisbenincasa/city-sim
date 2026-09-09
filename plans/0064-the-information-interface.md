# 0064 — The information interface

## Work queue

**2026-09-08 — Desktop viewport decision, agreed with the player:** design and review at
1440×960 and larger. Keep text scaling, but stop tiny-window screenshots and layout work;
480×640 is no longer an acceptance requirement. This supersedes the narrow-screen requirements
below; existing small-screen captures remain historical evidence, not obligations for future passes.

This plan owns the UX queue: outstanding work, scope changes, completion checks and recommendations.
Queued work starts with design agreement before implementation. The player approved the research-led
queue revision and starting row 10 on 2026-09-08. The player's 2026-09-06 expansion
below supplies the remaining scope; individual open choices do not block independent work.
Historical rows retain their previous verification; row 10 records its new checks below.

| Order | Status | Work | Completion check |
|---|---|---|---|
| 1 | Complete | Console pointer reading (superseding fixed-corner hover); Building and Household inspectors; expandable sections, breadcrumbs and mouse close; independent debug; light/dark themes and responsive layout | `check-information.py` and `information-lifecycle.drive` below |
| 2 | Complete | Road inspection, surface picking, directional travel conditions, endpoint links and frontage navigation | `check-roads.py`, `check-road-types.py` and `road-lifecycle.drive` below |
| 3 | Complete | Everyday HUD controls: agree placement and hierarchy for time, map layers and active-tool feedback as one composition | Reviewed in both themes at 1440 × 960 and 480 × 640. **Composition C, the console, is chosen**, with the pointer reading folded into it |
| 4 | Complete | Visible pause/resume and speed buttons with an unmistakable current state | `check-console.py` below |
| 5 | Complete | Named map-layer picker and persistent legend | `check-console.py` below |
| 6 | Complete | Clear active tool, mouse-accessible cancel and readable action refusals | `check-console.py` below |
| 8 | Complete | Larger text and shared sizing; keyboard help; visible camera controls | `check-discovery.py`; both themes at 1440 × 960 and 480 × 640, including 150% text and preference restoration after restart |
| 9 | Complete | Expandable tool browser, tool icons and contextual options; paint-to-zone with automatic subdivision | Choose, preview, paint and cancel without shortcuts; adding tools does not widen the console |
| 10 | Complete | Game menu: Save, Load, Settings, Help, Credits and protected Quit | `CitySaveTests` and `scripts/ui/check-menu.py`; saved content travels with the city, failed loads preserve it, and menu controls work with the mouse |
| 18 | Partial; independent | Content fitting, shadows, nested radii, quieter headings, type roles and tabular readings built; motion and full acceptance pending | Measured against the same `ui read` dumps: chrome share falls, no panel is stretched past its content, spacing values sit on one grid, panel changes preserve orientation without delaying input, live numbers stop reflowing; reduced motion preserves clear states |
| 13 | Partial: supply diagnosis built | Evidence-backed status explanations: normal, routine waiting, trouble and unavailable explanation; concurrent causes with a primary summary and expandable detail | Removing one of two causes reveals the remaining cause; unavailable evidence never becomes normal; every emitted code has a shell sentence |
| 14 | Partial: current supply marks built | Persistent marks in the world on troubled subjects, as a channel separate from row 7's feed | A troubled Building is discoverable without selection; retries retain one issue; resolution clears its mark; recurrence is identifiable; history labels resolved events and deleted subjects leave no broken links |
| 15 | Partial: SVG family integrated | An icon family and redundant visual cues: chrome, Goods, zone permissions, map layers, entity kinds, selection and severity | Essential distinctions survive without hue in both themes and enlarged text, using shape, labels or patterns; licences recorded for Credits |
| 16 | Partial | Road, Building, Household and Business navigation built, including Workplace links; remaining subject types pending | No displayed subject id is unnavigable; `docs/01 §6`'s no-orphan-figures rule holds over the shipped inspectors |
| 17 | Proposed | Separate developer controls from the player's console and Help | Debug overlays leave the `O` cycle; the tuner, log write and debug readout leave the player's shortcut catalogue |
| 7 | Proposed after first diagnosis pass | Compact current-issues list; event history remains later | Group by subject and cause; member counts reconcile; every entry opens an affected subject; resolved conditions leave the current list; routine waits do not automatically demand attention |
| 11 | Proposed | Citizen names and sex; Life Stage in inspection | Stable identity across replay/load and row reuse; inspection updates when the Household changes Stage |
| 12 | Proposed; foundation built | Vary setbacks by built form; deterministic parcel setbacks already exist | Visible variation without parcel escapes or disagreement between drawing, picking and occupied ground |

Rows 4–6 were one implementation slice and it is built. Its shared acceptance check — pause, change
speed, choose a map layer, inspect a subject and cancel an editing tool entirely with the mouse, in
both themes and at narrow sizes, with hover, inspector navigation and the independent debug overlay
intact — is `scripts/ui/check-console.py`, and it presses real button rectangles rather than issuing
the actions, so a control that is drawn but unreachable fails exactly as a missing one does.

Broader dashboards, graphs, notifications, persistent Pins and tuner redesign remain deferred.
Rows 8–12 promote the requested interface work, including tool icon assets and access to Policies.

## Icon family — 2026-09-08

The player chose custom SVGs with **outline as the default and filled on hover**. `UiIcons`
loads paired assets from `assets/icons`, embedded in the shell assembly, and shares textures.
Buttons retain labels, tooltips and selected-state styling; selection alone does not fill an icon.
Disabled controls remain outlined. Non-interactive readings stay outlined.

The family covers tools, zone permissions, menu and camera controls, map-layer choices,
inspector subjects and links, stock readings and supply marks. Unrecognised Resource names use
an explicitly generic stock symbol. `scripts/ui/check-icons.py` checks actual texture changes,
unchanged hit rectangles, selection, themes and text scaling; `check-diagnosis.py` still owns the
complete diagnosis interaction. Credits identifies the icons as project-created assets.

Row 15 remains partial: redundant patterns on Map Layers and stronger selection cues still need
an in-context design and verification. An icon beside a layer name does not make the layer itself
readable without hue. The comparison sheet remains under `art/ui-study`.

## Next pass — current issues

The first supply diagnosis interaction is built. Next is row 7's compact current-issues list,
using the same Evidence: group affected subjects and causes, reconcile counts, open a subject,
and remove resolved conditions. Other status families, historical events and Citizen inspection
remain pending. Large-city save/load responsiveness remains unmeasured.

Row 18's desktop comparisons remain independent; preserve text scaling and both themes.

## First diagnosis interaction — 2026-09-08

`Evidence.SupplyOfBuilding` supplies the mark and primary summary through `ReadRule`; the primary
is the greatest missed-firing count, with Rule Instance identity breaking ties. `RuleEvidence`
also exposes the blocking Bin, its reading, Business ownership and Rule Instance identity.
`Main.Diagnosis` owns current supply marks and Business inspection. `Main.Information` owns
concurrent explanations, Household finances and Workplace navigation. Marks use generational
Building handles, disappear on resolution or deletion, and expose the current episode's starting
Tick in inspection. They retain no event history. Routine space waits receive no trouble mark;
missing evidence remains explicitly unavailable. The diagnosis is scoped to supply, not an
assertion that every aspect of an unmarked Building is healthy.

The driven fixture starts at Tick 512 with 256 Citizens. Building 2 has four Households waiting
for money. A rebate of 1,000 reaches one Household while three remain blocked; the summary and
mark retain those three. A smaller rebate of 100 is the subsequent recovery check. These are
PROVISIONAL fixture amounts, not balance or responsiveness measurements. The fixture derives
from `taxed.toml` without modifying it.

Reproduce after a Debug shell build:

```sh
python3 scripts/ui/diagnosis-fixture.py /tmp/borough-diagnosis
godot --path src/Borough.Godot -- --ruleset /tmp/borough-diagnosis/diagnosis.toml \
  --citizens 256 --start-at 512 --drive /tmp/borough-diagnosis/diagnosis.drive \
  --listen /tmp/borough-diagnosis.sock
python3 scripts/ui/check-diagnosis.py /tmp/borough-diagnosis.sock
```

The check uses real button rectangles, follows Household → finances → Workplace → back,
sets the rebate through Policies, and captures both desktop themes plus partial and complete
resolution under `artifacts/hud-live/diagnosis-*`. `EvidenceTests` covers concurrent causes,
resolution, recurrence, missing wait targets and deleted subjects. The assertion lane passed
3,018 tests. World-mark scanning and large-city interaction costs have not been measured.

## Research — 2026-09-08

Outside evidence gathered for rows 13, 14, 15 and 16. Read the sources rather than these lines.

**Factorio attaches a closed status enum to every entity** — roughly seventy values including
`item-ingredient-shortage`, `full-output`, `no-path`, `waiting-for-space-in-destination` — rendered as one
coloured dot plus one sentence in the entity's own window, and exported to mods. The vocabulary includes
`normal` and `none`; this does not prove complete causal knowledge. Here `LotEvidence` can report
several reasons or an unavailable explanation, which row 13 must preserve. Players file bug reports when
the *wrong reason word* is chosen, which is the standard the affordance is held to.
<https://lua-api.factorio.com/latest/types/EntityStatus.html>, <https://forums.factorio.com/viewtopic.php?t=69086>

⚠ **Half of this vocabulary is already specified here.** `docs/01 §1` — *Diagnose has two halves* — says
selecting an empty Lot re-runs the Zone Rule's predicate and reports which clause failed, and
`CONTEXT.md` → Frontage owns the six clauses. Row 13 generalises a design the corpus already carries.

**Separate current conditions from historical events.** Factorio splits transient global alerts from persistent
warning icons drawn on the entity; RimWorld splits condition-alerts from event-letters. Conflating them
is what produces spam. `docs/01 §6` specifies only the event feed, which is why row 14 is separate from
row 7 rather than part of it. <https://wiki.factorio.com/Alerts>

**Klei's stated pivot for Oxygen Not Included was notification to standing dashboard** — the Colony
Diagnostics panel *"gives more ongoing awareness of the game's status, rather than relying on
notifications when things go wrong."* Klei then had to ship two follow-ups: individual rows became
click-to-navigate, and trigger conditions were narrowed after false positives.
<https://oxygennotincluded.wiki.gg/wiki/Versions/FA-471618>

**Two failure modes that are cheap to avoid at model-design time and expensive to retrofit.** Terms in an
itemised breakdown must be comparable in kind — The Sims 4's ambient decor moodlets give every sim a
standing `+1` to `+3`, which drowns the event-driven causes in the sum. And per-event causal messages need
deduplication by cause and subject — Dwarf Fortress emits an announcement each time a job cancels and
dwarves retry, so the correct answer buries itself. The second is the direct risk to row 7.
<https://roburky.itch.io/sims4-meaningful-stories/devlog/63570/true-happiness-features>,
<https://steamcommunity.com/app/975370/discussions/0/5828254465015388771/>

**The Dwarf Fortress Steam redesign removed nothing from the model.** Its win was retrieval cost: data
stopped being *"hidden behind many keypresses and scattered through many different windows and screens"*,
consolidating into tabbed sheets addressable by mouse, with explanation moved into hover and the map
moved from glyphs to tiles. Reviews credit it with making the game controllable rather than understood.
<https://www.pcgamesn.com/dwarf-fortress/menus>

**Icons must separate by shape and orientation, not hue alone.** A dashboard developer's critique of
Manor Lords praises that *"every different icon (nearly) is clearly recognisable at a glance through
differences in shape, colour and orientation"*, naming RimWorld as the negative case; the Game
Accessibility Guidelines require that no essential information be carried by a fixed colour alone and ask
for pattern fills on map areas. Every map layer here is a colour ramp.
<https://steamcommunity.com/app/1363080/discussions/0/598539452432936012/>,
<https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/>

**The genre is spending its interface budget here.** Cities: Skylines 2's most recent patch, *First Frost*
(1.5.4f1, 18 February 2026), is a HUD redesign whose headline is new icon families across road services,
info-view categories, pollution types, building level, land value, wealth and profitability, shipped with
a legacy opt-out. Frostpunk 2 delayed launch to rebuild its interface and said so. Against the Storm's
rebuild post opens *"The old UI was pretty, but a bit unreadable in places."*
<https://www.paradoxinteractive.com/games/cities-skylines-ii/news/patch-notes-first-frost>,
<https://www.pcgamesn.com/frostpunk-2/ui-improvements>, <https://eremitegames.com/interface-update/>

**And one line that corroborates the amended Definition of done from outside.** Stone Librande, on
SimCity: *"I can't look at anybody's city as a screenshot and tell you what's going on; I have to see it
live and moving before I can fully understand if your roads are OK, if your power is flowing."*
<https://bldgblog.com/2013/05/sim-city-an-interview-with-stone-librande/>

## Row 18 — the visual complaint, measured — 2026-09-08

**Partial implementation:** `f61878b` landed `Main.Information.FitPanel`, shared shadows,
nested radii, quieter headings and revised type roles in `InformationUi`, plus tabular clock and
frame readings. The table below is the **pre-change baseline**, not the current interface.
Panel transitions remain unbuilt. Full completion still needs a spacing audit and matched desktop
`ui read`/capture comparisons for chrome, content fitting and changing numbers.

The player's report is that the interface is *"visually unappealing and weird — it doesn't flow well for a
human."* It is separable from rows 13–17, which are about what the interface can say. This row is about how
it sits on the screen. Every figure below is read off `artifacts/hud-live/zoning-light-1440-118.json` and
the matching capture, at 1440 × 960 with text at 118%. ⚠ **These are measurements of this composition at
this window size**, not properties of the design (`plans/0012` Cause 5).

| Measured | Reading |
|---|---|
| Tools 224 × 728, inspector 479 × 728, console 1392 × 160 — **734,600 of 1,382,400 px** | chrome holds 53% of the frame and every panel colour in `InformationUi.Apply` is opaque with no alpha |
| Tools content 325 px of 728; inspector content 519 px of 728 | **both side panels stretch to the frame rather than to their content**, so 12% of the whole frame is blank opaque panel over the city |
| Console row 1 ends at x = 1064 with a 243 px hole mid-row; row 2 ends at x = 687 | the right 40% of a full-width bar is empty |
| Type roles 14 / 16 / 22 pt | the 14 → 16 step is 1.14×, below what reads as hierarchy |
| Spacing constants in use: 6, 8, 10, 12, 16, 18, 22, 30, 36 | nine values on no grid; 18 and 22 fit neither a 4 pt nor an 8 pt system |
| `Box()` sets a 1 px border on all four sides of every element, and one 4 px radius everywhere | a collapsed section is three nested rectangles, and inner and outer radius are equal |
| `Section` `#d9e6f7` fills **every** section heading | the accent marks all sections equally, so only the amber attention band carries signal |
| No `StyleBoxFlat` sets a shadow; nothing in the shell animates | no elevation and no transitions |
| `InformationUi.Button` sets `TooltipText = text` | every tooltip repeats its own label |

**What the outside evidence says to do about it.** Read the sources; these lines are pointers.

**Weak clickability cues had a measured cost on webpages, not in this game.** Nielsen Norman Group's eyetracking found users spent **22% more time and
made 25% more fixations** on pages with weak clickability signifiers than with strong ones. Their
prescription is *Flat 2.0* — keep the simplicity, restore subtle shadows on interactive components,
layering and the card metaphor.
<https://www.nngroup.com/articles/flat-ui-less-attention-cause-uncertainty/>,
<https://www.nngroup.com/articles/flat-design/>

**Three named spacing rules.** *Internal ≤ external*: padding inside a group must never exceed the margin
around it, and violating it is what makes a row of controls read as an undifferentiated smear. *Inner
radius = outer radius − padding*. And **common region** — a shared background behind a subgroup — *"can
override proximity and similarity"*, which is the cheapest way to give a flat button row structure.
<https://cieden.com/book/sub-atomic/spacing/spacing-best-practices>,
<https://uxplanet.org/corner-radius-of-nested-elements-in-ui-design-4c27bb24a854>,
<https://www.nngroup.com/articles/common-region/>

**Motion guidance to try, not an acceptance threshold.** NN/G put the useful range at 100–500 ms, with 200–300 ms for a panel entering
and 400 ms already very slow; Material's desktop figure is tighter at 150–200 ms for small elements. Exit
runs shorter than enter, ease-out on the way in and ease-in on the way out, because *"completely linear
motion looks weird and unnatural to users."* Sibling rows stagger 30–80 ms.
<https://www.nngroup.com/articles/animation-duration/>, <https://m3.material.io/styles/motion/easing-and-duration>

**Live numbers need tabular figures.** Proportional digits have different widths, so a counter that changes
every frame reflows the text beside it. `Day 0 · midday 11:00` and the FPS reading both do this today. The
fix is an OpenType `tnum` feature on the font used for readings.
<https://www.mediaatelier.com/en/Posts/Tabular-Figures/>

**Anno 1800's designer states the restraint rule out loud**: *"If there are too many visual elements, the UI
itself would start competing with the actual game for the player's attention"* — and persistent chrome uses
**darker, desaturated hues** because *"bright and flashy color schemes could quickly become tiring for the
eyes"*, with bright colour reserved for transient notifications. That is the argument for demoting the
section fill and keeping the accent for the attention band.
<https://www.anno-union.com/devblog-user-interface-2/>

⚠ **Two cautions against redesigning far.** Frostpunk 2's white chrome drew a sustained backlash for
fighting the game's own fiction and for small text on white, and 11 bit restored a dark option; Cities:
Skylines 2 shipped its *First Frost* HUD redesign **with a legacy-UI toggle**. Studios report players react
badly to interface changes as such.
<https://steamcommunity.com/app/1601580/discussions/0/4699035966525967575/>,
<https://www.paradoxinteractive.com/games/cities-skylines-ii/news/patch-notes-first-frost>

**One claim withdrawn.** *On-screen camera buttons signal a discoverability failure* was asserted in
conversation on 2026-09-08 and **no source supports it**. Row 8 added the camera group deliberately, for
mouse-only reach. It stands.

**One premise corrected.** *Two or three type sizes beats six* is not the practitioner position; the common
answer in this source is around seven defined roles, not a required count for this interface. The defect here is not the count — it is that two of the three roles
are 2 pt apart and so do not read as distinct.
<https://cieden.com/book/sub-atomic/typography/establishing-a-type-scale>

**Reference for comparison.** The Game UI Database's Inspector Tool reports per-screenshot colour hex codes
and font sizes across 1,300+ games, so a panel here can be measured against a shipped one rather than
guessed at. <https://www.gameuidatabase.com/>

## Proposed next passes — 2026-09-06

These rows cover the player's ten requests. Rows 8–9 are built; row 10 is partial; rows 11–12 remain planned.
Preserve the chosen bottom console and both themes. Row 9 follows the selected rectangle treatment below.

**8 — Readability and discovery (requests 2, 6, 8): built.** `Main.Discovery` owns
shared sizing, Help, shortcut handling and camera controls. The implementation and verification
sections below retain the original acceptance evidence; Settings now owns sizing at a 100% default.

**9 — Tools that can grow (requests 3, 4, 5, 9): built.** `Main.ToolBrowser` and
`Main.ToolDefinitions` own the left column, categories, labelled SVG choices, contextual options
and shared shortcuts. Policies opens its own panel. Search remains conditional on list growth.
The rectangle treatment below supersedes the original browser-above-console proposal.

**Agreed 2026-09-06:** replace Subdivide with the familiar paint-to-zone interaction. The player
chooses permitted development and paints land; subdivision happens automatically. This supersedes
the original block-click proposal. `LotSubdivider.SubdivideAt` is an implementation starting
point, not a constraint on the brush: it currently creates Lots and does not repaint claimed frontage.

**2026-09-08: option B selected, then revised by the player to a left-side column.**
The column carries vertically stacked categories and contextual choices with distinct SVG icons.
Long instructions are removed; brief labels and tooltips carry the actions.
Wheel events over the column stay in the interface, including at the end of its scroll range. Zoning drags a rectangle between whole-block corners, including unequal blocks;
release applies, Escape cancels the stroke, and releasing over a panel cancels it. The console retains
the selected tool and Cancel. The [three treatments](../artifacts/visual-study/zoning-treatments/index.html)
remain available for comparison.

`Main.ToolDefinitions` supplies choices and shortcuts to the browser and Help. `Main.Zoning` owns
preview and commit; `LotSubdivider.PaintAt` updates existing Lot permissions and subdivides new
frontage. Erase removes permissions. Existing Buildings remain, and changed permissions govern future
development. Ground without Streets retains its permissions. Identical painting reports no change.
Other tools retain their existing behavior; Policies opens its own panel. No new Road or service
interaction is implied by its category.

**10 — Game menu (request 10).** A visible menu button opens Save, Load, Settings, Help, Credits and
Quit. `Main.Menu` supplies the menu lifecycle and reuses `Main.Settings` and Help. Opening this menu pauses the city; closing it restores the previous pace. Help alone leaves the pace unchanged. Load and Quit protect unsaved
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
frontages and more variation among detached Buildings. Ranges are PROVISIONAL.
`LotRuleset.Footprint` already draws deterministic parcel setbacks; built-form variation remains owed.
Trace imported-model placement in `Main.Assets`; `Main.Massing.Buildings` reads the
Lot footprint directly. Keep the footprint within its parcel and preserve coherent corner geometry.
Do not shift only the mesh. Prefer shifting an unchanged footprint where room permits; any resizing
must also update the simulation quantities derived from its area. Variation must survive reload and
remain stable while the Building stands. Review street-level and wider views before tuning further.

**Checks per pass.** Build Godot Debug and use the drive skill to inspect the result. Extend the
existing real-button UI checks for new interactions; check both themes at 1440×960 and larger,
including enlarged text, scrolling, focus, Escape, tool cancellation and inspector coexistence.
Exercise Save → Load → continue against an uninterrupted run. Identity and footprint changes need
targeted determinism, save/reload and invariant checks. Run `scripts/test.sh` before committing;
update golden fixtures through their recorder when simulation changes move the State Hash.

## Agreed design and implementation

The player initially chose composition B in both themes with fixed lower-left hover;
row 3 superseded that placement with the console pointer row. Clicking opens a persistent inspector on the right, with expandable
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
the tool tray remains available through Tools. The code minimum remains 480 × 640; review starts at 1440 × 960. `ThemeInformation`
changes colours without rebuilding inspection state; preferences live in `user://information.cfg`.

Supply shortfalls appear in the main explanation. Scheduled activities and waits for output space
belong in Activities: a full stock is not, by itself, a warning. Missing subjects retain an explicit
message instead of resolving a recycled slot. `Main.CloseInspection` also clears selection when the
tuner creates a new World.

## Row 8 — implemented 2026-09-06

`Main.Discovery` owns shared typography roles, the saved text-size preference, Help and the shortcut
catalogue used by keyboard actions and camera buttons. The original default was 118%, PROVISIONAL; the Settings corrections below supersede it
with 100% and move the size controls there. Help leaves pace
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

## Row 9 verification

`check-zoning.py` exercises button rectangles and injected pointer strokes, checks an unchanged
paused State Hash during preview and cancellation, and covers erasure, repainting, both themes,
480×640 and 1440×960, and enlarged text with inspection open. Captures are under
`artifacts/hud-live/zoning-*.png`. The left column scrolls at short heights, with its close control fixed above the choices.
On narrow windows the inspector sits to its right. At 150% text, long choice labels truncate
with full instructions in tooltips. Closing either panel restores space.

`ZoningPaintTests` covers the affected block, retained Buildings, permission-index invalidation,
no-op repainting, replay and continued save/load equivalence. The assertion lane passed 2,971 tests;
Godot Debug built without warnings. Existing golden fixtures did not move.
`check-information.py`, `check-console.py` and `check-discovery.py` also passed. The console
check waits for stable button rectangles before clicking after a layout change.


### Review corrections — Zoning and Settings

`Main.ZonedBlocks` supplies zone-colored block interiors while Zoning is held;
`ZoneInterior` also clips vacant parcel pads. Both sit below Street and path surfaces.
Tools opens beside the left column. `Main.Settings` groups theme, text size, Debug and Help;
100% replaces the earlier default and reset size. Button spacing, truncation and tool labels
use the shared components. Theme changes publish one notification; Debug only changes visibility.
This also removes the deferred theme work that delayed the initial HUD.

`check-settings.py` covers navigation, narrow layouts, unchanged State Hash, zone surface heights,
and action-to-next-frame responsiveness. Its latency assertion fails with the original theme
notifications. Startup was compared through the first rendered frames; temporary probes were removed.

Godot Debug and the three Zoning assertions passed after these corrections. Settings, Zoning,
discovery, console and inspector interaction checks passed; captures remain in `artifacts/hud-live/`.

## Row 10 — implemented 2026-09-08

`Main.Menu` owns the centred menu, pace restoration, file picker, unsaved-progress choices and
window-close protection. Load and Quit offer Save and continue, Discard and continue, or Cancel.
Cancelling a picker cancels the pending action. Credits reads `assets/credits.json`; its Back
button stays outside the scrolling content. Settings and Help remain independently accessible.

`CitySave` packages the Core dump with the exact Ruleset text and seed in `.borough-city` files.
Saving happens at an owned Tick boundary without advancing the paused city, writes a sibling
temporary file, flushes it and replaces the destination. Loading validates content identity,
seed, saved State Hash and end-of-run invariants before installing the world, rebuilding its
drawing and clearing selection. Names come from the embedded content. Save/load is synchronous;
large-city responsiveness has not been measured. Standalone Input Log export after loading is
explicitly refused because the log lacks its starting world.

Six `CitySaveTests` passed, including continued equivalence under `minimal.toml` and `care.toml`,
damaged packages and failed replacement cleanup. The assertion lane passed 3,016 tests. Godot
Debug built without warnings. `scripts/ui/check-menu.py` passed on a driven 64-Citizen city at
1440×960: both themes, Settings/Help/Credits, modal input, pause restoration, cancelled actions,
failed-load preservation, exact restored State Hash, continued simulation and Save-before-Quit.
The final Quit flow used 150% text. Captures are `artifacts/hud-live/menu-*.png`.

Watching found Credits could scroll its Back button away and the light file picker inherited
dark default surfaces; both were corrected. The driver now reads popup buttons and file-item
rectangles. Its load-path command opens the directory without silently selecting the file; the
check clicks the entry and then Open, so it exercises the player's selection path.
