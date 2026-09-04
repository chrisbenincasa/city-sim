---
name: drive
description: Drive the Godot shell programmatically — the --drive script channel, the --listen socket, --record, and the three files a driven run returns (shoot, readout, draw). Use whenever the shell has to be launched, photographed, stepped to a Tick, clicked at a Tile, or asserted against without a hand on the keyboard, and whenever a change to Main.cs or to anything it draws needs watching happen.
---

# Driving the shell

The shell has a full command surface that needs no keyboard. It is [`plans/0048`](../../../plans/0048-driving-the-shell.md),
five tiers, all landed 2026-08-31. Read that plan for the findings; this file is the operating
instructions.

🔴 **A driven run does not build, and `dotnet build -c Release` builds the configuration Godot does
not load.** Godot runs `.godot/mono/temp/bin/**Debug**`, and a shell whose build is stale starts
anyway on whatever was there last. `plans/0048` **F26**: a new MultiMesh layer was added, the shell
was driven, and `draw` reported nine layers and not ten. ***A screenshot cannot report that it is
stale and a draw list can.*** So, before any driven verification of a shell change:

```
dotnet build src/Borough.Godot          # no -c. The default is Debug and Debug is what loads
```

⚠ **`godot` on this Mac is a wrapper script and must stay one** — a symlink stalls the .NET module
on an invisible modal alert. If a run hangs at `.NET: Initializing module...`, that is the cause.

---

## 1 — Which channel

| Want | Channel | Display needed |
|---|---|---|
| A reproducible run somebody else can re-run | **`--drive FILE`** | only for `shoot` |
| To explore a running city and keep the session | **`--listen PATH` + `--record FILE`** | usually yes |
| To assert the drawing agrees with the city | **`draw`**, from either channel | ***yes*** — see §5 |

**The two channels are one grammar.** `DriveScript.Line` prepends *now* as the Tick and calls the
same parser, so a verb cannot come to mean two things depending on how it was sent. And `--record`
writes from `Main.Apply`, the one surface every channel arrives at — so a socket session, a script
run and a hand at the keyboard all record as the same drive script, and ***the log of an interactive
session replays as a batch run*** (**F20**: byte-identical draw list).

---

## 2 — Launching

```
godot --path src/Borough.Godot -- \
  --ruleset rulesets/flooded.toml --citizens 2000 --start-at 6000 \
  --drive /tmp/run/flood.drive --quit-at 6500
```

Everything after Godot's own `--` is the shell's. **Relative paths resolve against the repository
root**, not against the Godot project — for the Ruleset, the script, the socket and every file a verb
writes.

| Argument | Default | Notes |
|---|---|---|
| `--ruleset PATH` | `rulesets/minimal.toml` | Refused rather than defaulted if bad |
| `--citizens N` | `1000` | ⚠ **`Borough.Headless` defaults to 10,000.** A cross-check against the runner must pass the same figure — a lattice paves what its population needs (**F14**) |
| `--start-at TICK` | `0` | **Steps every Tick and skips nothing.** Slow and correct: a world jumped to is a different world |
| `--drive PATH` | — | The script. Does **not** imply an end (**D3**) |
| `--quit-at TICK` | — | One more `quit` command on the end of the script, not a second mechanism |
| `--listen PATH` | — | Unix domain socket. Unlinked before binding, deleted on exit |
| `--record PATH` | — | Appends every applied command as a script line |
| `--govern` | off | Opens the Policy panel at start, so a machine with no hands can photograph it |
| `--empty` | off | 🔴 **Declines to generate a city.** No lattice, no Lots, no Buildings, no Citizens — and see below, because it withholds the terrain too |
| `BOROUGH_LOG` (env) | — | Any value: writes the Input Log at `quit`, so play → write → replay → compare runs in a script |

🔴 **`--empty` is the only argument that changes what the shell is a picture of.** Everything else
sizes or times a generated city; this one declines to generate one. `CommandKind.Populate` ran at
Tick 0 unconditionally until 2026-09-04, so ***every screenshot, balance run and judgement about how
this city reads was taken on a lattice [`adr/0090`](../../../docs/adr/0090-the-generator-makes-land-and-the-player-makes-every-road.md)
says will not exist*** — and `CommandKind.Connect` has never been watched working. The flag is the
path to the other world.

✅ **It issues `CommandKind.Ground` rather than skipping the Command, since 2026-09-04.** It used to
skip it outright, which gave a world with no ground in it at all — measured on `flooded.toml` at 500
Citizens, Tick 10: populated drew hazard **11,063**, water **5,058**, tree **3,512**, and `--empty`
drew **0**, **0**, **0**. That is not `adr/0090`'s world either. `PopulateInto` is split at `LayLand`
now, so `Ground` lays terrain, Woodland, water and hazard and stops — and **both branches spend
exactly one Tick at boot**, so `--start-at` counts from 1 either way.

🔴 **A world booted this way has nobody in it, and `people` is how it gets somebody.** The chain is
Households → the Unplaced Pool → placement → Buildings, so an empty Pool builds nothing for ever:
measured on `minimal.toml` before the verb existed, 40 Street clicks and 16 zone clicks gave 40
Segments and 128 Lots and **0 Buildings, 0 Citizens** five in-world Days later. The full recipe is
**boot empty → `hold street` and click → `hold zone 1` and click → `people` → run**.

**`--headless` works** (**F12**): the world runs, every verb applies, every `readout` is written.
See §5 for the two things it silently cannot do.

---

## 3 — The grammar

`<tick> <verb> [argument]`, one per line, `#` to end of line is a comment. Blank lines are skipped.
The parser is `src/Borough.Formats/DriveScript.cs`; the applier is `Main.Apply`.

⚠ **Every verb is an absolute and none is a toggle.** `roads off` means the same thing whatever ran
before it. The keyboard is what toggles — it reads the current state and calls the absolute — so
there is one applier rather than two behaviours that can part company.

| Verb | Form | What it does |
|---|---|---|
| `pause` | `pause` | Stops the clock; remembers the rung |
| `resume` | `resume` | Starts it again at the remembered rung |
| `speed` | `speed <rung>` | Sets the rung, 0–8. **The shell clamps.** `speed 0` is a pause |
| `roads` | `roads on\|off` | The carriageway |
| `cells` | `cells on\|off` | The 128 m Cell lattice |
| `lens` | `lens on\|off` | HUD off **and** depth of field on. A picture for a person rather than for a record |
| `overlay` | `overlay <name>` | Tints the ground by a Map Layer. A **view**; changes no State Hash |
| `turn` | `turn left\|right` | A quarter turn of the eye |
| `tilt` | `tilt <degrees>` | Degrees above the horizon, 4–85, **absolute**. The shell clamps |
| `zoom` | `zoom in\|out [notches]` | Notches default to 4. **Clamped to the city's span** |
| `focus` | `focus <east> <north> [metres]` | Puts the camera over a Tile. ⚠ **The distance is NOT clamped** — the one place a script may do what a player cannot, and the reason it exists is that the zoom clamp otherwise confines a script to ~3 km of a 65.5 km map |
| `hold` | `hold <tool> [which]` | Chooses what the next `click` means |
| `click` | `click <east> <north> [shift]` | **Acts on the city** at a Tile |
| `release` | `release <east> <north>` | **The other half of a drag, and it changes nothing.** Says what the gesture asked the lattice for: with `street` held, a drag across two edges gets a sentence — there are no diagonal Streets, and a run is many edits. ⚠ **The second of a pair**, so a `release` with no `click` before it says so. `--record` writes one only where the drag left the edge it started on |
| `people` | `people` | **Puts people in the city** — Buildings, Households and Citizens on whatever Lots stand. `CommandKind.People`, and the one verb here with no key on the keyboard: it is an instrument in `CommandKind.Populate`'s family, and it acts **once per world**. Refused, in words, on a world that already has people or has no Lots yet |
| `shoot` | `shoot <path.png>` | The frame, **and the readout beside it** as `<path>.txt`, in one act |
| `readout` | `readout <path>` | The readout alone. Needs no display |
| `draw` | `draw <path>` | The draw list — every instance on screen, as TSV |
| `quit` | `quit` | Ends the run |

**The rung ladder** — `speed N` indexes it, and the shell opens at **5**:

| 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| paused | 1/16× | 1/8× | 1/4× | 0.5× | **1×** | 2× | 3× | 4× |

🔴 **The caption carries the HOVER as well as the readout, since 2026-09-04.** Every channel used to carry `_readout.Text` alone, so the panel that says *what a click would do here* — the Street tool's edge, the zone tool's free frontage, the foot path crossing the block — was invisible to every driven run. ***A caption that omits half the screen cannot be asserted against the half it omits.*** The tool palette and the two editing panels are still `Control`s and still invisible; only the hover joined.

**Tools for `hold`** — `look`, `zone`, `street`, `demolish`, `service`. The second word is *which
one*: a Zone Rule by declaration position, a service by its 1-based kind id. ⚠ **An unknown tool
disarms the hand to `look`** and prints a refusal, rather than leaving the previous tool loaded.
⚠ **`shift` on a `click` with `street` held is a bulldoze.**

**Layer names for `overlay`** — `off` (or `none`), `pollution`, `value` (or `land`, `land-value`),
`sealing` (or `sealed`). An unknown name is a refusal in the readout, not a silent `off`.

⚠ **Two more are DEBUG views and would not ship** — `rung` (or `pattern`) and `age` (or `vintage`).
They are **building** washes rather than ground ones: the ground goes dark and the massing keeps a
colour, which is the opposite of the three above. `rung` is categorical — five hues for the five
block patterns, sparsest to densest — and `age` is the ordinary ramp, **bright is old**, normalised
against the Tick now rather than against the oldest Building on screen. 🔴 **A generated city is one
vintage**, so `age` washes flat on a fresh world; that is the reading and not a fault.

### The four refusals a script gets at parse time

Every one of them is collected, so a script is fixed in one pass rather than one line per run.

1. **Ticks may not go backwards.** The shell cannot step back to reach one.
2. **Nothing runs after `quit`.**
3. 🔴 **A stopped clock never reaches a later Tick.** `pause` at 6,101 then `readout` at 6,400 is a
   **hang, not an error** — and a hang looks like work. Refused at parse (`DriveScript.Stopped`)
   for exactly that reason. `resume` or `speed N` before advancing.
4. **`--quit-at` before the script's last command** is refused, and so is one after a trailing
   `pause`.

### The clock

🔴 ***A Tick number in a script is not a fast-forward.*** `_Process` steps no further than the next
command's Tick — which is what makes the landing Tick a property of the **script** rather than of
the frame rate or the machine's load (**F6**) — but it never hurries. A script whose first command
is at Tick 2,000 waits **250 s** at rung 1. **`--start-at` is how a late Tick is reached and `speed`
is how a long stretch is crossed.**

---

## 4 — The socket

```
godot --path src/Borough.Godot -- --ruleset rulesets/minimal.toml \
  --listen /tmp/borough.sock --record /tmp/session.drive &
```

Then, one line in and one line out, **and the read blocks until the reply comes** — so a driver
sends and reads in lock step without a clock of its own:

```
printf 'pause\nreadout /tmp/a.txt\ndraw /tmp/a.tsv\nquit\n' | nc -U /tmp/borough.sock
```

| | |
|---|---|
| **Wire line** | The same grammar **with no Tick** — its Tick is *now*, stamped on arrival |
| **Reply** | `ok<TAB><tick><TAB><readout, newlines as tabs>` or `refused<TAB><reason>` |
| **A poll** | ⚠ **An empty line.** No commands, and the state comes back anyway |
| **Threading** | Lines are applied on the main thread at a Tick boundary, never where they arrive |
| **Clients** | One at a time (`Listen(1)`) |

⚠ **`quit` down the socket is how the run ends cleanly**, and it is what deletes the socket file.
A killed shell leaves it behind and the next run reports *address in use* — a message about this run
that reads as one about that one. `--listen` unlinks before binding, so it recovers on its own.

---

## 5 — What comes back, and what `--headless` costs

| Verb | File | Format |
|---|---|---|
| `readout` | one file | `tick <N>` on its own first line, then the readout, then a `hover —` line and the hover panel beneath it. **The Tick is on line one so a caller need not parse an em-dash** |
| `shoot` | `<path>.png` **and** `<path>.txt` | The frame, and the same readout beside it — written in one act so they cannot disagree about which Tick they are of |
| `draw` | one TSV | `tick`, `ruleset`, **`scale`**, one `layer` row per layer with **instances / capacity / visible**, then one `row` per instance: `layer index id x y z sx sy sz yaw r g b` |

**The seventeen layers, in draw order**: `ground hazard water flood road footway kerb cell plot building
roof hip mansard yard tree rock traveller`. ⚠ **`roof`, `hip` and `mansard` are the three PITCHED families and
a Building writes into at most one of them** — a flat-roofed Building writes into none, so the three
never sum to the Building count. Eight carry an entity id (`plot`, `building`, `roof`, `hip`,
`mansard`, `yard`, `footway`, `kerb`) and `traveller`
resolves one; the rest honestly say `-`.

⚠ **`footway` and `kerb` are the two layers whose id is not unique down its column** — both are
per-Segment-per-side, so a Segment appears at least twice, and their capacities are 2× and 4× every
other layer's for the same reason. ***Count Segments there with `sort -u`, or count double.***

⚠ **`kerb` holds two different things and the NAME does not separate them**: a kerb **band** is
`sx = 0.4` and `sy = 1`, and a **dropped** kerb — one per Address — is `sx = 1.4` and `sy = 0.467`,
which is the band's height scaled down to the carriageway's. ***Filter on the width, and the drop
count should equal the Lot count*** (`--zones` prints that).

⚠ **A `footway` or `kerb` row's `sz` is NOT its Segment's length**, and a strip may be **longer** than
the Segment it belongs to. Each end is mitred against whatever meets the Segment at that node — half a
carriageway where one crosses, nothing where none does, and a *negative* trim round the outside of a
bend. On `pictured.toml` at 2,000 Citizens the pavements come out **122.2 / 125.1 / 128.0 / 132.0 m**
against a 128 m block. ***So a strip length is a statement about the junction and not about the
Segment.***

⚠ **Both layers also hold a CAP across the head of a dead end**, which lies *across* the run rather
than along it — a pavement cap is `sx = 1.1` with `sz` the road's width plus two kerbs, and a kerb cap
is `sx = 0.4` with `sz` the carriageway. ***So `sz` is not a length down the street for those rows***,
and the honest way to count dead ends is the cap's `sz`. `severance.toml` at 2,000 Citizens has **12**.

🔴 **`scale` is the ruler, and it is the only row in the file that is about the CAMERA rather
than about the city.** `scale <px per Tile east> <px per Tile north> <eye distance m> <viewport w>
<viewport h>`, from three ground points a Tile apart put through the same projection the frame was
drawn with — so tilt, perspective and the window's own size are in the answer. ***Everything else the
draw list says is in metres and a screenshot is in pixels; nothing joined them until 2026-09-04***
([`plans/0060`](../../../plans/0060-the-plot-module.md) **F2**), so every judgement about whether a
thing *reads* was an eyeball. **Measured** at 3,024 × 1,834 and tilt 40°: **54.6 px a Tile at 100 m,
27.0 at 200, 13.4 at 400 — where a player edits — 5.35 at 1,000 with the city in frame, 2.67 at
2,000.** ⚠ **The two axes differ by the tilt**, so a frontage figure has to say which way the
street runs. ⚠ **And it is a property of the READING** — the same distance on a smaller window is
fewer pixels a Tile, which is why the viewport is in the row.

⚠ **`instances` against `capacity` is not bookkeeping.** A layer at capacity has silently dropped
whatever did not fit, and the picture shows a smaller city with nothing to say so — ***the one
failure a screenshot renders as success.***

🔴 **Two things `--headless` cannot do, and both used to look like success:**

- **`shoot` writes no picture.** It says *no picture at Tick N* and **still writes the caption**, so
  a script written for a screen returns its numbers on a machine without one.
- **`draw` withholds its rows.** Under the dummy renderer every transform reads back as the
  identity — the first headless dump had the correct layers, the correct row count and 21,504 lines
  of zeros (**F17**). The counts are CPU-side and stay true; the rows are withheld with the reason
  written into the file. ***A draw list needs a real display.***

**Why the draw list is the tier that pays for the others**: almost everything the shell reads is the
core's, so a driven shell reporting world state reports what `Borough.Headless` already reports. What
only exists here is **the derivation** — the transform and the colour this shell decided on.
`Basis.Scaled` scales in the parent frame, so every east–west Segment once drew 8 m long and 128 m
wide and half the road network was missing from a picture nobody could assert on. A row saying a
Segment is 8 long and 128 wide fails a test.

---

## 6 — Recipes

**Photograph one Tick from two angles.** The same city, two drawings:

```
# /tmp/run/flood.drive
6101 pause
6101 shoot /tmp/run/a.png
6101 roads off
6101 turn left
6101 shoot /tmp/run/b.png
6101 speed 8
6400 readout /tmp/run/c.txt
```

**Assert the drawing against the city**, which is the reason to run this at all:

```
6101 draw /tmp/run/one.tsv
awk -F'\t' '$1=="row" && $2=="road" {print $8, $10}' /tmp/run/one.tsv | sort -u
```

Then cross-check the count against `dotnet run --project src/Borough.Headless -- --roads` **at the
same population**.

**Act on the city:**

```
150 hold zone 1
150 click 8200 8200
150 hold street
152 click 8200 8232 shift     # shift is the bulldoze
160 shoot /tmp/run/after.png
```

⚠ **A driven click enters the Input Log by the same door a hand's does** — `Main.Act` builds a
`Command` and `Ordered()` drains it into `Step`. The camera, speed and layer verbs do not, which is
the other half unchanged. ⚠ **A click outside the map is refused in `Apply`**, because the driven
aim skips the ray that clamps a cursor.

**Explore, then keep it:** run with `--listen` and `--record`, drive it by hand or over the socket,
and the recorded file replays through `--drive`. That round trip is what makes a wall-clock channel
into a deterministic simulation defensible rather than merely tolerated.

---

## 7 — What none of this buys

⚠ **NOT ONE TIMING FIGURE.** `--drive` forces nothing about the frame clock, and a driven run is on
a machine that is not necessarily the reference one.
[`adr/0106`](../../../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)
governs: ***a number produced this way is not the number it looks like.*** This buys **observation
and reproduction**. Measurement stays where it was.

⚠ **And a picture is a spot check rather than an instrument.** It catches what no assertion names —
lighting, occlusion, whether a thing is *legible*. The draw list is the half a test can hold.
