# Start the shell from a saved city

## Outcome

The shell opens a saved city from the command line and is ready at the saved Tick without
simulating any Tick before it. Headless writes the same save, so a mature 1M-Citizen city is
simulated once and opened many times.

## Why

- `--start-at` simulates every Tick up to the one asked for. A 1M-Citizen Tick costs about 130 ms on
  the dev machine, so each in-world Day of age adds about 4.5 minutes of loading.
- Measured 2026-09-23 on `zeus` (i5-10400, one simulation thread, not quiet), `stress-shopping.toml`
  at 1,000,000 Citizens, seed 0, 600 Ticks. The mean is 144 ms per Tick at `8f635e7` and 132 ms at
  `192a937`, 118 s and 109 s of wall time. PR #16 did not cause the slowness.
- The shell can open a save only through its menu (`Main.PickCityFile`). Headless `--save` writes a
  bare `SaveFile` without the Ruleset and seed the shell's `CitySave` envelope needs.

## Current state

| Piece | Where | State |
|---|---|---|
| City envelope (zip: `city.json`, Ruleset bundle, `world.save`) | `src/Borough.Godot/CitySave.cs` | Godot-free; linked into `Borough.Tests` by a `Compile Include` |
| Envelope round trips | `tests/Borough.Tests/Shell/CitySaveTests.cs` | Write, read and State Hash comparison already covered |
| Shell startup | `Main._Ready` → `PrepareCity` → `CityPreparation` | Builds and steps the world on a worker thread behind a loading screen; drive commands are answered meanwhile |
| Open a city | `Main.PickCityFile`, open branch | Menu only; main thread; assumes a built scene |
| Headless save and load | `Session` `--save`, `--load` | Bare `SaveFile`; the Ruleset comes from `--ruleset` |

Headless and the shell build the same city: both issue one `Populate` command at Tick 0, and the
shell always uses seed 0.

## Decisions

| Decision | Reason |
|---|---|
| Move `CitySave` and `SavedCity` to `Borough.Formats` unchanged, with their tests | Both hosts then write and read one format. The menu keeps its path |
| `--load` reads the save inside `CityPreparation` | A second constructor reads the `CitySave` on the worker thread, then steps to an explicit `--start-at`. The loading screen, Cancel and drive answering keep working. The existing callback installs the city and builds the scene |
| The menu and `--load` share one helper after `CitySave.Read` | It sets the capture, names, seed, Citizen count, Input Log and `_resumedFromSave`. The menu keeps its own UI reset |
| Without `--start-at`, a loaded city opens at the saved Tick | Stepping to the next 08:00 could cost minutes at 1M Citizens. A night save opens at night |
| An explicit `--start-at` is an absolute Tick at or after the saved one | Drive scripts keep absolute Ticks. An earlier Tick is refused, not rewound |
| `--load` refuses `--ruleset`, `--citizens` and `--empty` | The save carries its Ruleset, population and seed |
| A loaded city runs at once, like any other launch | Scripts that need a still frame pause through their drive script |
| Headless `--save-city PATH` writes a `CitySave` at the end of a run | The name says the direction. `--save` keeps its bare format for replay and profiling |
| Saves are cached outside the repository by `measure-shell-band.py` | A save carries its own Ruleset. After a Ruleset or code change it still loads but shows the old city. A `SaveHeader.FormatVersion` change makes it refuse to load |
| The cache key is Ruleset content hash, Citizens, seed, Tick and format version | Leaving out the commit reuses a city across code changes, which suits rendering work. `--fresh` forces a rebuild |
| No `save PATH` drive command | Nothing needs it. Revisit when someone wants to save a city built by driving the shell |

## Steps

1. Move `CitySave` and `SavedCity` to `Borough.Formats` as public types. Move `CitySaveTests` to
   `tests/Borough.Tests/Formats` and drop the `Compile Include` link.
2. Add a `CityPreparation` constructor that reads a `CitySave` and steps to an optional Tick.
   Extract the helper the menu and startup share after `CitySave.Read`.
3. Parse `--load PATH` in `Main.Arguments`. Record whether `--start-at` was given, apply the
   refusals, and route startup through the new constructor.
4. Add headless `--save-city PATH`. Make headless report the Ruleset content hash and save format
   version so a script can build the cache key. Document both options in `Options.Usage` and the
   drive skill's argument table.
5. Add `--load` and the cache to `scripts/measure-shell-band.py`, with `--fresh`.
6. Measure launch-to-ready on `zeus` at 1M Citizens and Tick 600, in one session. Record `--load`,
   `--start-at 600` and a fresh start at Tick 1 here, and the same for 10k Citizens.

## Acceptance

- `godot --path src/Borough.Godot -- --load CITY --listen SOCK` answers at the saved Tick with no
  Ticks simulated. The readout Tick and State Hash equal those of a headless run stepped to that
  Tick with the same Ruleset, Citizens and seed.
- A city saved from the shell menu opens with `--load`, and a headless `--save-city` save opens
  from the menu.
- `--load` together with `--ruleset`, `--citizens`, `--empty`, or an earlier `--start-at`, is
  refused with a message naming the conflict.
- At 1M Citizens and Tick 600, `--load` reaches ready faster than `--start-at 600`. The target is
  5× faster. A smaller gain still ships, with the measured ratio recorded.
- A driven demonstration opens a 1M-Citizen save and photographs a street, with the time from
  launch to ready recorded.
- `scripts/test.sh` passes.

## Results

Measured 2026-09-23 on `zeus` (i5-10400, GTX 1080, Release shell) with `scripts/measure-shell-band.py
--suite ready`, `stress-shopping.toml`, seed 0, Tick 600. The machine was not quiet: load average
2.6 to 4.7 across the runs.

| Launch | 10k Citizens | 1M Citizens |
|---|---|---|
| `--load` (cached save) | 6.4 s | 29.0 s, 29.1 s |
| `--start-at 600` | 7.4 s | 138.0 s |
| Fresh start, `--start-at 1` | 6.1 s | 46.4 s |
| Save file size | 2.4 MB | 30.6 MB |

- At 1M Citizens `--load` is 4.8× faster than `--start-at 600`, just short of the 5× target.
- `--load` beats the fresh start, so reading the save costs less than generating the city.
  Building the scene is the likeliest remaining cost; it is not yet split out.
- Building the 1M cache entry with headless `--save-city` took about 2.5 minutes, once.
- The shell's Tick and State Hash after `--load` equal headless at the saved Tick
  (10k: Tick 600, `7319E1F4328D346B`; 1M: Tick 600, `91651FCF980AB06B`) and after stepping on (`--start-at 640`, read at Tick 650:
  `3BC4B7718AAD941E`).
- A driven run opened the 1M save, photographed a street at Tick 600, and exited in 21.7 s.
- The menu path shares `CitySave.Read` and `CitySave.Write` with `--load` and `--save-city`. No
  drive command reaches the file picker, so the menu round trip was not driven.

## Open questions

- Where the 29 s of a 1M `--load` goes, between reading the save and building the scene.
