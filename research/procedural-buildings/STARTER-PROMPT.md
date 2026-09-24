# Starter prompt — procedural Buildings, step 5

Paste the block below into a fresh session.

```text
We are continuing the procedural Building work. The test street has its
materials and variants. The next job is step 5 of the pass 03 review
protocol: decide whether to put exact game bodies into Godot, and build
them if so.

State at hand-off (2026-09-24):
- PR #21 (procedural-buildings) is merged. It holds the ShellBuilder
  prototype, the chunk measurements and the monochrome test street.
- PR #22 (branch test-street-materials) is open and unmerged. It holds:
  - CC0 wall and roof materials at true scale. scripts/art/
    test-street-materials.py fetches them into art/materials/test-street/,
    with provenance in materials.json.
  - A filter that divides out the membrane's broad tonal bands.
  - Flat roofs behind parapets on A2 and W2.
  - Three variants on unchanged geometry: H1 reroofed, M1 with a repaired
    shopfront, W1 with rooftop solar. Show them with --variants.
  - A pre-commit hook, scripts/hooks/pre-commit, that runs
    scripts/format.sh --check when C# is staged. Install it per clone with
    ln -s ../../scripts/hooks/pre-commit .git/hooks/pre-commit
  Check whether #22 has merged before branching.
- Decisions the user made:
  - Keep 3.5 m storeys everywhere.
  - A pitched roof is at least 18 degrees. Anything shallower is a flat
    roof behind a parapet (SESSION.md Q11).
  - A "minor details" pass comes later (docs/deferred.md). Do not start it.
- Known gaps in the test street:
  - Fine streaks remain in the membrane roofs after the band filter. An
    authored membrane texture is the fallback if they bother the user.
  - The ground is flat colour.
  - The camera can pass inside a Building. That may deserve a board row.

Read first:
- research/city-architecture/output/03-context-and-construction/
  model-briefs.md. Step 5 is line 52. Line 28 sets the G003 rules.
- research/city-architecture/output/03-context-and-construction/
  simulation-audit.md explains the capacity ceilings. G003 is 24 x 16 m
  with 10.5 m walls and a two-tenancy ceiling. The twelve-flat A1 is a
  content experiment, not a replacement for G003.
- plans/evidence/procedural-buildings/README.md, sections "First test
  street", "Surface materials" and "Variants on the same geometry".
- research/procedural-buildings/SESSION.md for decisions Q1-Q11.
- docs/07-the-drawing.md#building-authoring-procedure.

Step 5, in order:
1. Put the decision to the user first, as prose with costs. The choice is
   whether to build one exact-body W1 and one G003 study labelled with its
   two-tenancy capacity, or to stop at the test street.
2. If the user says yes, author both in Blender, following the Building
   authoring procedure. W1 keeps its pass 03 dimensions exactly. G003
   keeps 24 x 16 m and 10.5 m walls and displays its capacity.
3. Place them in the shell with the drive skill. Build Debug before every
   capture:
     dotnet build src/Borough.Godot
4. Review five views: near, neighbourhood, city, a moving camera and the
   overlays. Record what each view exposed in the evidence README.

Tools and paths:
- BLENDER_BIN=~/.local/opt/blender-5.2.1-linux-x64/blender, GODOT_BIN=godot
- scripts/art/review.sh test-street exports, imports, builds and captures
  into artifacts/test-street/. open-test-street and
  open-test-street-variants open the scene.
- When a GLB is byte-identical, Godot skips its reimport. If extracted
  textures go missing, delete .godot/imported/<model>.glb-* and reimport.

Constraints:
- Put --listen sockets in $XDG_RUNTIME_DIR, because the path limit is
  108 characters.
- Do not use `pkill -f`. It matched and killed its own shell.
- Preserve every worktree. Ask the user before touching
  worktree-row-31-attracts-people or catchment-fold (../city-sim-terrain).
  Another session owns .claude/worktrees/city-save-fast-start.
- research/textures/candidates.html is deliberately untracked. Leave it.
```
