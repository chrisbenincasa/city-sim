# Starter prompt — procedural Buildings, family bodies

Paste the block below into a fresh session.

```text
We are continuing the procedural Building work. Appearance Families can
now generate their own body at each Building's size, and the
office-warehouse is the first family with one. The next job is bodies for
the corridor apartments and the rowhouses.

State at hand-off (2026-09-24):
- Three stacked PRs are open. Check which have merged before branching,
  and branch from the newest unmerged one (family-models) or from main.
  - #24 (exact-game-bodies): exact Blender bodies placed on live
    Buildings with `ui exact-bodies on|off`.
  - #35 (appearance-families): the Style Preset format, reader, schema,
    weighted picker, headless coverage report (--appearance DIR) and the
    `overlay family` debug wash.
  - #36 (family-models): `[family.body]` in the preset,
    FamilyBodyBuilder in Borough.Appearance, `ui family-bodies on|off` in
    the shell, and the rename of the test-street bodies.
- The test-street bodies were renamed. H1 is now rowhouses, A1
  corridor-apartments, A2 walkup-apartments, M1 corner-shops-with-flats,
  W1 office-warehouse, W2 workshop and G003 two-unit-apartments. Older
  evidence keeps the old codes.
- How a body works now:
  - A family's [family.body] table sets the bay width, a bay list for
    each wall's ground and upper storeys (street_, back_, side_), a
    parapet, pilasters, rooftop plant, and the GLB whose materials
    dress it. A token ending in `*` fills the spare bays.
  - Bay kinds: blank, window, shop, entry, door, roller, stair. Opening
    sizes are fixed per kind in FamilyBodyBuilder.Openings.
  - FamilyBodyBuilder ports scripts/art/test-street.py. It writes in
    Blender's Z-up frame and converts each polygon to Godot's frame, so
    the two stay comparable line for line.
  - The office-warehouse at 36 x 20 m has the Blender body's exact
    bounds. Five generated ones stand in shopping.toml at 400 Citizens,
    Tick 600.
- Known limits of bodies:
  - Drawn only with no overlay showing; no wash or paint yet.
  - A Building raised after the toggle gets no foliage footprint until
    the next full pass.
  - One mesh per Building, no chunking, no far level, not measured.

The job, in order:
1. Corridor apartments first, because they need the least new
   vocabulary. Compare scripts/art/test-street.py corridor_apartments()
   with what [family.body] can express. It needs at least a window width
   per family or per row (1.6 m windows on 3 m bays), a central entrance
   with its own canopy, and end-wall doors under stair windows. Extend
   the format only as far as the body needs. Check the generated body
   against the Blender one at 24 x 16 m by bounds, then photograph both.
2. Rowhouses second. They need a gable roof (18 degrees across the
   depth, shingles), a repeating house module (door and window below,
   two windows above), party-wall upstands, chimneys and front steps.
   Before building, put this decision to the user as prose with costs:
   is one rowhouses Building a single house or a whole range? The family
   admits frontages of 4 to 28 m, and attached Buildings on one block
   face share a family (SESSION.md Q6). Party walls should follow edge
   labels (SESSION.md Q4), so a side wall against a neighbour is blank.
3. Photograph each family with the drive skill: near, neighbourhood and
   a side-by-side with its Blender body. Record the results in
   plans/appearance-families.md and keep evidence in
   plans/evidence/appearance-families/.

Read first:
- plans/appearance-families.md: decisions, pieces, acceptance and limits.
- src/Borough.Appearance/FamilyBodyBuilder.cs and StylePreset.cs.
- appearance/test-street/preset.toml, the office-warehouse body.
- scripts/art/test-street.py, rowhouses() and corridor_apartments().
- research/procedural-buildings/SESSION.md, decisions Q1-Q11.
- docs/07-the-drawing.md#building-authoring-procedure.

Tools and paths:
- Build Debug before every capture: dotnet build src/Borough.Godot
- Probe run: --ruleset rulesets/shopping.toml --citizens 400
  --start-at 600, then `ui family-bodies on`. Placement prints
  family_body lines with each Building's size and tile.
- scripts/test.sh --filter 'FullyQualifiedName~Appearance' covers the
  reader, picker, builder and schema.
- The schema is hand-written: appearance/appearance.schema.json.
  StylePresetSchemaTests keeps its keys equal to the reader's. Lint with
  npx @taplo/cli lint 'appearance/*/*.toml'
- BLENDER_BIN=~/.local/opt/blender-5.2.1-linux-x64/blender. Re-exporting
  the test street changes no geometry hash unless the geometry changes;
  art/test-street/bodies.json records them.
- After new or renamed assets, run
  godot --headless --path src/Borough.Godot --import
  and commit the generated .import and .cs.uid files.

Constraints:
- Keep 3.5 m storeys everywhere. A pitched roof is at least 18 degrees.
- The "minor details" pass (scuppers, downpipes, crickets) comes later.
  Do not start it.
- Put --listen sockets in $XDG_RUNTIME_DIR, because the path limit is
  108 characters.
- Do not use `pkill -f`. It matched and killed its own shell.
- Preserve every worktree. Ask the user before touching
  worktree-row-31-attracts-people or catchment-fold (../city-sim-terrain).
  Another session owns .claude/worktrees/city-save-fast-start.
- research/textures/candidates.html is deliberately untracked. Leave it.
```
