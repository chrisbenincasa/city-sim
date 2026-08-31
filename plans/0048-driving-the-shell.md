# 0048 — Driving the shell, and what a driven run returns

Scoped 2026-08-31, against `main` at `63bb181`. **Written under the amnesty deliberately**: the
shell is the only instrument that can answer *did you watch it happen*, which is what
[`0045`](0045-amnesty.md)'s amended Definition of done now requires of every milestone, and it
cannot be driven without a hand on the keyboard.

---

## Status

🟢 **ALL FOUR TIERS LANDED 2026-08-31.**

**Where tier 2 lives:** `Borough.Formats/DriveScript.cs` (the grammar), `Borough.Tests/Formats/
DriveScriptTests.cs` (23 assertions), and `Main.Apply` / `Main.Drive` / `Main.Driven` /
`Main.Shoot` / `Main.Caption` in the shell.

---

## The thesis: the picture is not the output

🔴 **THE FIRST VERSION OF THIS PLAN WAS BUILT ON SCREENSHOTS AND THAT WAS THE WRONG OUTPUT.** A frame
has to be *looked at* to mean anything, which makes every reading a judgement instead of an
assertion, and no test can be written against one.

**Two questions want two channels, and only one of them needs a display at all.**

| Question | Instrument | Display? |
|---|---|---|
| ***What is the city doing?*** | `Borough.Headless` — **twenty-odd dumps already exist** (`WatchDump`, `DayDump`, `FloodDump`, `EvidenceDump`, `CensusReport`, …) | no |
| ***Does the drawing agree with the city?*** | the shell, and **its answer must be the draw list rather than the frame** | yes |

⚠ **Almost nothing the shell reads is the shell's.** `VisibleAgents.In` is in
`Borough.Core/Movement/`, `LayerCells` likewise; `Main.Draw` derives transforms and colours from
them and hands the result to a `MultiMeshInstance3D`. ***So a driven shell that returned world state
would be returning what the headless runner already returns, one display server later.***

**What only the shell holds is the derivation** — `Main.Buildings()`, `Main.Pave()`, `Main.Cells()`,
`Main.Travellers()` — the per-instance transform and colour. That is the class of defect
[`0045`](0045-amnesty.md) records and the ASCII dump could not catch: `Basis.Scaled` scales in the
**parent** frame, so every east–west Segment drew 8 m long and 128 m wide, and *half the road network
was missing in a picture nobody could assert on*. ***A draw-list dump would have failed that
mechanically*** — every Segment's drawn length equals its Tile length, or it does not.

---

## What tier 1 proved, today

**Zero source changes.** The shell's existing keyboard surface, driven from outside.

```
DISPLAY=:1 godot --path src/Borough.Godot -- \
  --ruleset rulesets/flooded.toml --citizens 2000 --start-at 6101
W=$(DISPLAY=:1 xdotool search --pid $PID --name Borough | tail -1)
DISPLAY=:1 xdotool key --window $W space      # every key in Main._UnhandledInput
DISPLAY=:1 import   -window $W frame.png      # the shell's window only
```

| | Finding |
|---|---|
| **F1** | **The display is real and so is the GPU.** `:1` is 2560×1440 on a GTX 1080, Vulkan Forward+ — ***not* Xvfb and *not* llvmpipe**, so the shell renders exactly what a person sees. Xvfb and lavapipe are both installed and both are the fallback, not the path taken |
| **F2** | **Godot accepts a synthetic key.** `xdotool key --window` reaches `_UnhandledInput` **without taking focus**, so a driven run does not fight the person at the desk. ⚠ **Tier 1 rests entirely on this** — many toolkits drop `XSendEvent` keys, and the day Godot does, tier 1 dies and tier 2 is the only answer |
| **F3** | **`import -window <id>` captures the shell alone.** The root window holds the user's desktop; the shell's does not. ⚠ **A capture is scoped for privacy, not for tidiness** |
| **F4** | 🔴 **TIER 1 ADDRESSES WALL-CLOCK MOMENTS AND THE CITY IS ADDRESSED IN TICKS.** A capture lands on whatever Tick the frame happened to hold — the two frames taken 2 s apart here read Tick 6,457 and 6,567. ***So the same script run twice observes two different cities***, and no output of tier 1 is reproducible. **This is the whole reason tier 2 exists** |
| **F5** | **The city agreed with the readout, which is the point rather than a result.** `flooded.toml` at Tick 6,567: `235 ruined, 5 swept`, and the greyed shells stand massed against the water with the pale stock inland — a depth gradient, exactly as `01 §5.2` says a flood's severity is *the flood level minus the ground*. ⚠ **Confirmation and not a surprise.** It is recorded because *checking* it took one command |

---

## The queue

Ordered. Each tier is usable on its own, and each is the fallback if the next proves wrong.

| | Work | State |
|---|---|---|
| **1** | **The borrowed keyboard.** `xdotool` + `import` against the shipped shell. No code | ✅ **31-08** |
| **2** | **`--drive`, the Tick-addressed script.** The reproducible channel | ✅ **31-08** |
| **3** | **The draw-list dump.** What only the shell can answer, as data rather than pixels | ✅ **31-08** |
| **4** | **The socket.** Live exploration, and its log is a tier-2 script | ✅ **31-08** |

### Tier 2 — `--drive`

| | Task |
|---|---|
| **2a** | ✅ **The grammar and its parser, in `Borough.Formats`.** `<tick> <verb> [argument]`, `#` comments. Ten verbs: `pause`, `resume`, `speed`, `roads`, `cells`, `turn`, `zoom`, `shoot`, `readout`, `quit` |
| **2b** | ✅ **One control surface.** `Main.Apply(DriveCommand)`. The keyboard builds a command and calls it; so does the script |
| **2c** | ✅ **`--drive FILE`**, and 🔴 **the clock is CLAMPED at the next command's Tick** — see **F6** |
| **2d** | ✅ **`shoot <path>`** writes the frame and the readout together, and 🔴 **redraws first** — see **F8** |
| **2e** | ✅ **`--quit-at TICK`**, implemented as one more `quit` command rather than a second mechanism |

**Every verb is an absolute and none is a toggle**, which is the property tier 1 lacked: `roads off`
means the same thing whatever ran before it, where `g` does not. ***The keyboard is what toggles*** —
it reads the current state on the way in — so there is one applier and not two behaviours.

```
godot --path src/Borough.Godot -- --ruleset rulesets/flooded.toml --citizens 2000 \
  --start-at 6000 --drive flood.drive --quit-at 6500
```

```
# flood.drive
6101 pause
6101 shoot /tmp/run/a.png     # writes a.png and a.txt together
6101 roads off
6101 turn left
6101 shoot /tmp/run/b.png     # the same Tick, the same city, a different drawing
6101 speed 8
6400 readout /tmp/run/c.txt   # no picture, no display needed
```

### Tier 3 — the draw list ✅

**The `draw <path>` verb**, and it is the tier that pays for the other two. A TSV: a `layer` line per
layer with **instances against capacity and whether it is visible**, then one `row` per drawn
instance — layer, index, id, origin, scale, yaw, colour.

🔴 **THE GEOMETRY IS READ BACK OFF THE `MultiMesh` AND NOT FROM THE ITERATOR THAT FILLED IT.** A dump
built by walking the tables a second time would be a second derivation, free to agree with the world
while disagreeing with the picture — ***which is the entire class of defect this exists to catch.***
The mesh is what the GPU is handed, so the mesh is what is asked.

⚠ **Identity comes the other way, from the fill**, because a `MultiMesh` knows only an instance index
and an index is a recycled slot rather than a name. `Fill` records the id of what it painted; the
Traveller's is **resolved** rather than read off the `Handle`, whose index is `internal` to the core
precisely because a slot is not identity. Two layers know an entity and the six that draw ground
say `-`.

⚠ **This is where the image stops being the instrument and becomes the spot check.** A picture still
catches what no assertion names — lighting, occlusion, whether a thing is *legible*.

### What tier 3 found, in its first dump

`flooded.toml`, 2,000 Citizens, Tick 6,101, 21,516 rows.

| | Finding |
|---|---|
| **F13** | ✅ **THE ASSERTION THAT WOULD HAVE CAUGHT `Basis.Scaled` NOW PASSES MECHANICALLY.** Every Street draws **8 × 128 m** — `[roads] block_tiles` 32 × 4 m a Tile — where the defect drew each one 8 long and 128 wide. ***It is one `awk` over a column***, where before it was half a road network missing from a picture |
| **F14** | ✅ **The renderer drops nothing.** 149 Segments drawn against **149 in the graph**, cross-checked against `Borough.Headless --roads` at the same population. ⚠ **The comparison had to be re-taken at 2,000 Citizens**: the first read 149 against 627, because ***a lattice paves what its population needs*** and the runner defaults to 10,000 |
| **F15** | ✅ **235 derelict boxes against 121 standing, and the readout says `235 ruined`.** Exactly two distinct colours in the layer, and the dark one's count equals the city's own. ***The picture and the census agree, and now that is a number rather than an impression*** |
| **F16** | 🔴 **A FOOTPATH IS DRAWN AT CARRIAGEWAY WIDTH.** Five of the 149 Segments came back at **8 × 181.019 m and 135°** — 128√2, the diagonal cut-throughs — and `--roads` confirms **5 FootPaths** at that population. `RoadWidthMetres` is applied to every Segment whatever its kind, so a path carrying **24,000 Vehicles/Day at 5 km/h draws as wide as a Street carrying 86,400 at 50**. ⚠ **Found by a column of numbers and not by looking**: at this zoom five diagonals in a lattice read as scenery. ✅ **Filed as [`0003`](0003-build-plan.md) hash-moving queue item 27** under `adr/0073`, with the repair named: the shell invents **three** thicknesses where it invents one, and none of them is a Ruleset key |
| **F17** | 🔴 **UNDER `--headless` EVERY TRANSFORM READS BACK AS THE IDENTITY, AND THE FILE STILL LOOKS RIGHT.** The counts are CPU-side and stay true, so the first headless dump had the correct layers, the correct row count and **21,504 lines of zeros** — ***structurally valid and silently wrong***, the one failure that survives being eyeballed. ⚠ **Same shape as the screenshot's headless defect [`0045`](0045-amnesty.md) records twice**: the handle is fine and the emptiness is in what it points at. **The rows are now withheld and the reason is written into the file**, because a file that exists is a file somebody parses |
| **F19** | ✅ **The Traveller rows carry real Citizens.** `minimal.toml` at Tick 752: **64 drawn against the readout's 64**, **64 distinct ids, none unresolved**, none outside the map. ⚠ **Finding the Tick took a scan** — a `readout` every 16 Ticks across the morning — because ***the Day is a comb***: the counts cluster near 496, 576, 656, 752 and 848, roughly 85 Ticks apart, which is one in-world hour. [`0045`](0045-amnesty.md) row 18 owns that, and this is a second instrument agreeing with it |
| **F18** | ✅ **Two draw lists of one script are byte-identical**, so a row that moves is a renderer change. **F11** said this of the pixels; this says it of the geometry, which is the half a test can name |

### Tier 4 — the socket ✅

**`--listen PATH`** opens a Unix domain socket; **`--record FILE`** writes every applied command back
out as a drive script. A wire line is the same grammar with the Tick supplied — `DriveScript.Line`
prepends *now* and calls `Parse`, so a verb cannot come to mean two things depending on how it was
sent.

```
--listen /tmp/door --record session.drive
```

**One line in, one line out, and the read blocks until the reply comes**, so a driver sends and reads
in lock step without a clock of its own. A reply is `ok<TAB>tick<TAB>readout` or
`refused<TAB>reason`. **An empty line is a poll** — no commands, and the state comes back anyway.

⚠ **Lines are applied on the main thread at a Tick boundary, never where they arrive.** The socket
thread enqueues and blocks; nothing off the main thread touches the world.

### What tier 4 found

| | Finding |
|---|---|
| **F20** | 🔴 ✅ **AN INTERACTIVE SESSION REPLAYED AS A BATCH RUN AND PRODUCED A BYTE-IDENTICAL DRAW LIST.** Driven live over the socket — pause, `speed 8`, `roads off`, `draw`, `quit` — `--record` wrote `150 pause / 150 speed 8 / 184 roads off / 184 draw … / 186 quit`, and feeding that back through `--drive` reproduced the dump `cmp`-equal. ***This is what makes a wall-clock channel into a deterministic simulation defensible rather than merely tolerated***, and it is the reason the socket is last rather than first: it is worth nothing without tier 2 to replay into |
| **F21** | 🔴 **THE DOC COMMENT CLAIMED THE POLL BEFORE THE CODE HAD IT.** *An empty line is a poll* was written into `Answer`'s remarks and the grammar refused a blank line by name — prepending a Tick to nothing makes a lone Tick. ⚠ **Found by sending one at a running shell**, which is [`adr/0093`](../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md) inside a single method: ***the sentence was about the trigger and the trigger was the part that was wrong*** |
| **F22** | ⚠ **`--record` covers the KEYBOARD too, and free.** It writes from `Apply`, which is the one surface every channel arrives at, so a session driven by hand at the desk records as readily as one driven down the socket. ***A log complete by construction rather than by three call sites remembering***. ✅ **Measured**: seven keystrokes at the window — `space g e ] 2 space esc` — recorded as `74 pause / 74 roads off / 74 turn right / 74 speed 1 / 74 speed 6 / 80 pause / 80 quit`, and that file **replays through `--drive` with no refusal**. ⚠ **The Ticks are the interesting column**: five keys landed on one Tick and two on another, so ***what a hand does in a second is a script with almost no time in it*** |

### What tier 2 found

| | Finding |
|---|---|
| **F6** | ✅ **A COMMAND LANDS ON THE TICK IT NAMES, AND THAT IS THE WHOLE POINT.** `_Process` steps no further than the next command's Tick, so the Tick a command lands on is a property of the **script** and not of the frame rate, the rung or the machine's load. Measured: two `shoot`s and a `readout` landed on Ticks 6,101, 6,101 and 6,400 exactly. ***This is **F4** repaired*** |
| **F7** | 🔴 **A PAUSED SCRIPT DEADLOCKS, AND IT IS REFUSED AT PARSE RATHER THAN DISCOVERED AT RUN.** `pause` stops the clock, and a clock that is stopped never reaches a later Tick — so the first script written against this hung until a timeout killed it, **having already written every file it was going to write**. ⚠ **A hang is the worst refusal because it looks like work.** The check is `DriveScript.Stopped`, and it caught the example script inside this plan's own test class on the first run |
| **F8** | 🔴 **THE CAPTION WAS COMPOSED BEFORE THE COMMANDS IT DESCRIBES.** `Draw` runs before `Drive` in a frame, so `pause` then `shoot` on one Tick wrote a caption reading `speed 1x` over a picture of a stopped city. ***A caption written from a stale compose disagrees with its own picture***, which is the one thing writing them in a single act was meant to prevent. `Shoot` and `Caption` now recompose first |
| **F9** | 🔴 **`GetTree().Quit()` IS DEFERRED TO THE END OF THE FRAME, so a refused run drew one frame against a world `_Ready` never built** — and the refusal a person is meant to read scrolled past under a `NullReferenceException` and forty lines of backtrace. ⚠ **This was already true of BOTH Ruleset refusals and predates this work**; the drive script only made it happen often enough to notice. `Main.Stop` sets a flag `_Process` checks. ***A message printed above a stack trace is a message nobody reads*** |
| **F12** | ✅ **A DRIVEN RUN NEEDS NO DISPLAY AT ALL, AND THAT IS THE ANSWER TO *can this be done without pictures*.** `godot --headless --path src/Borough.Godot -- --drive …` runs the world, applies every verb and writes every `readout` — measured on `minimal.toml`, Tick 200 and Tick 400 exactly. ⚠ **`shoot` degrades rather than failing**: it says *no picture at Tick 400* and **still writes the caption**, so a script written for a screen returns its numbers on a machine without one. ***The picture is the only part of this that needs a display*** |
| **F11** | ✅ **TWO RUNS OF ONE SCRIPT ARE BYTE-IDENTICAL — THE PICTURES AS WELL AS THE CAPTIONS.** `flooded.toml`, 2,000 Citizens, `--start-at 6000`, two `shoot`s and a `readout`: every one of the five files `cmp`s equal across independent runs. ⚠ **This is a stronger claim than the simulation's own determinism** — it says the *drawing* is a function of the world and the script, so a frame that differs is a renderer change and nothing else. ***It is what makes a golden frame possible***, and tier 3's draw list is how one would be diffed without an eye |
| **F10** | ⚠ **`BOROUGH_SHOT` is gone, replaced by the `shoot` verb.** [`0045`](0045-amnesty.md) records **four** defects in that env-var mechanism, every one of them about when the picture was taken. It was a second control surface with no grammar and no test; the verb has both |

---

## Decisions owed

Under the amnesty these are stamped and not ratified ([`0045`](0045-amnesty.md) standing order 4).

| | Decision |
|---|---|
| **D1** | **Does a driven command enter the Input Log?** The six verbs must, or a driven session is not replayable by `Borough.Headless`. **Speed, camera and layer toggles must not** — they are host-side and runtime-only, and an Input Log holding a camera pose is a save file holding a monitor. ⚠ **STILL OPEN, and it has not bitten because the grammar has no city-changing verb in it at all** — every verb shipped moves the clock, the eye or a file. ***The day a `zone` or a `demolish` verb is added is the day this must be answered***, and `--record` is the thing that will make the gap obvious: it will spell a command the Input Log did not receive |
| **D2** | ✅ **CLOSED: both, because they cost one column each.** The instance index diffs a row against the `MultiMesh`; the entity id diffs it across Ticks. ⚠ **The id is the monotonic never-reused one and not the `Handle`'s index**, for `05 §4`'s reason — a list keyed on a recycled slot names a different Citizen after a collection, which is the one thing a list meant for diffing must never do |
| **D3** | ✅ **CLOSED: no.** `--drive` does not imply an end — a script a person is watching should keep running when it runs out of instructions. Wanting an end means writing `quit` or passing `--quit-at`, and **both arrive at the same applier as one more command**. ⚠ **`--quit-at` before the script's last command is refused**, and so is one after a `pause`, for **F7**'s reason |

---

## What none of this buys

⚠ **NOT ONE TIMING FIGURE.** A driven run is a run under a synthetic keyboard on a machine that is
not necessarily the reference one, and `--drive` forces nothing about the frame clock.
[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)
governs: ***a number produced this way is not the number it looks like.*** This plan buys
**observation and reproduction**, and measurement stays where it was.
