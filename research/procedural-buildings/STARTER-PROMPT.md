# Starter prompt — procedural Buildings, after the measurements

Paste the block below into a fresh session.

```text
We are continuing the procedural Building generator. The four REPORT §3
measurements are done. The next job is the first test street.

State at hand-off (2026-09-23):
- main is 4 commits ahead of origin/main and unpushed: 3ad76f8, 192a937,
  c434cd5, 71e5e82. Ask me before pushing.
- The `procedural-buildings` branch stops at 192a937 and is already in main.
  The later work was committed on main directly. Pick one home for new
  work and say which.
- The ShellBuilder prototype is in src/Borough.Appearance, a library with no
  Godot reference. ShellBuilder turns a WingShape (width, depth, storeys,
  street face, roof form, seed) into a mesh with bays, reveals, panes, a door
  and a roof. It has winding tests and benchmarks in tests/Borough.Tests.
- The shell hook `ui shell-band RADIUS CHUNK KIT on|off` (Main.ShellBand.cs)
  replaces boxes near the camera with generated shells, plus 15 placeholder
  kit boxes per Building. It is a measuring rig, not the production path.
- InstanceLayer.ChunkMetres now defaults to 1024 m. That took the whole-city
  opening camera from 12.9 to 67 fps.

Read first:
- plans/evidence/procedural-buildings/README.md has findings 1-14 and
  their conditions.
- research/procedural-buildings/optimizations.md holds optimisation leads.
  Keep adding to it as you find more.
- research/procedural-buildings/SESSION.md holds the ten decisions. Q8 sets
  the style as a present-day US Pacific Northwest preset.
- research/city-architecture/output/03-context-and-construction/REPORT.md
  is pass 03, which holds the Building briefs for the test street.
- docs/adr/0173-a-building-is-drawn-realistically-at-every-distance-from-its-own-facts.md

Jobs, in order:
1. Check that a moving city is fine at 1024 m chunks. Every capture so far
   ran paused, and the Traveller movement index (Main.MovementDrawing.cs)
   shares ChunkMetres. Unpause the 1M-Citizen stress city, then compare
   256 and 1024 m with BOROUGH_RENDER_PROFILE=1 and
   BOROUGH_RENDER_CHUNK_METRES. Use scripts/measure-shell-band.py as the
   model. Record the results in the evidence README.
2. Build the first test street from pass 03's briefs, US-sourced bodies
   first. Author the kit in Blender, following docs/07-the-drawing.md
   #building-authoring-procedure, and watch the result with the drive skill.

Measuring notes:
- For a Release capture, build the shell into the Debug path, and rebuild
  Debug afterwards:
    dotnet build src/Borough.Godot -c Release -p:OutputPath=$PWD/src/Borough.Godot/.godot/mono/temp/bin/Debug/
    dotnet build src/Borough.Godot -t:Rebuild
- Put --listen sockets in $XDG_RUNTIME_DIR, because the path limit is 108
  characters.
- The machine is rarely quiet. Record the load average, and treat every
  figure as an upper bound.
- Do not use `pkill -f`. It matched and killed its own shell.

Constraints:
- Another session owns "Start the shell from a saved city" in
  .claude/worktrees/city-save-fast-start. Its plan is uncommitted there.
  Do not touch it. Until it lands, --start-at 600 replays every Tick, about
  130 ms each at 1M Citizens.
- Ask me before touching `worktree-row-31-attracts-people` or
  `catchment-fold` (../city-sim-terrain). Preserve every worktree.
```
