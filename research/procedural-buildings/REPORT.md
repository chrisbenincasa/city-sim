# Procedural Buildings — Findings

How building generators work, how shipped games build their cities, and what Godot allows at
Borough's scale. This report combines three new research passes with the existing architecture
research. It decides nothing. [SESSION.md](SESSION.md) poses the decisions.

| Input | Scope |
|---|---|
| [sources/A-grammar-engines.md](sources/A-grammar-engines.md) | CGA/CityEngine, split and control grammars, Houdini, Unreal PCG, Blender tools, procedural extrusions |
| [sources/B-shipped-games.md](sources/B-shipped-games.md) | City builders, Townscaper, Tiny Glade, Manor Lords, open-world cities, Bethesda kits |
| [sources/C-runtime-rendering.md](sources/C-runtime-rendering.md) | Godot 4 output strategies, LOD, threaded regeneration, determinism, kit authoring |
| [../city-architecture/](../city-architecture/) | Passes 01–03. Building families, eligibility, construction rules, model briefs |
| [docs/07-the-drawing.md](../../docs/07-the-drawing.md) | Pillars, settled look, pipelines A/B/C, tiers, kit cost |

Every claim in the source reports carries a URL. Lines marked *inference* there are the
researcher's reading. This report marks its own inferences the same way.

---

## 1. Findings

### 1.1 Every working system has three layers

| Layer | What it does | Made how |
|---|---|---|
| Mass | Footprint and height become volumes, then wall faces and a roof | Generated |
| Placement | A 1D pattern splits each face into storeys and bays and picks a piece per slot | Rules (grammar, pattern string, code) |
| Terminals | Window bays, doors, cornices, shopfronts, roof pieces | Authored in a modelling tool |

- No production system generates detail from nothing. CGA, Instant Architecture, Houdini, Spider-Man
  and the UE5 City Sample all insert authored terminal pieces.
- The industry has shrunk the grammar since 2006. Houdini's Labs Building Generator and Unreal PCG
  keep only split-and-repeat along an edge and up a cross-section. Everything else lives in the pieces.
- Borough's wall shader already runs the placement layer in miniature. It splits on storeys, repeats
  on bays and picks a door face. It descends from Parish & Müller 2001, who drew façades as layered
  grid textures to reach city scale.

### 1.2 Shipped games sort by footprint source and building count

| Family | Examples | Buys | Costs |
|---|---|---|---|
| Authored library | Cities: Skylines 1/2, Anno, SimCity 2013 | Best identity per kind, cheap at runtime | Fixed lot catalogue, repetition complaints, needs a modding Workshop |
| Kit assembled by rules | UE5 City Sample, Spider-Man, Cyberpunk megabuildings | 22 kits cover ~7,000 buildings, style change reaches everything | Needs strict kit discipline, rules become their own project |
| Grammar geometry | City Sample massing, Citystate Metropolis, MSFS | Fits any lot and height | Hard to tune, ~20% error rate in MSFS, still decorates with kit pieces |
| Constraint solver (WFC) | Townscaper, Bad North | Organic fusion between neighbours | 300–500 tiles per style, solves can fail, no notion of building kind |
| Freeform runtime mesh | Tiny Glade, Manor Lords, Foundation | Follows any shape, animates change | One generator per element, small counts only, narrow style |

- Every open-world city generated offline and baked. Houdini Engine cannot run in a shipped game.
- Runtime generation exists in Unreal PCG, Citystate, Citybound and Tiny Glade. None shows tens of
  thousands of changing buildings.
- Shipped games show change coarsely. CS2 has unique meshes at levels 1, 3 and 5 only. Foundation
  uses a 3×3 density × quality grid. SimCity 2013 signals state with lights, parked cars and graffiti.

### 1.3 Borough needs runtime generation, and its footprints are the easy case

- Buildings are built, abandoned and replaced during play, so an offline bake cannot serve. The
  offline pipelines are evidence about authoring practice only.
- Footprints are axis-aligned rectangles on the 4 m Tile grid. A courtyard Building is four
  rectangular wings (`BuildingPlan.Hollow`). Podium/tower is two stacked rectangles.
- Reports A and C assumed arbitrary polygons. That assumption is wrong for Borough, and it removes the
  hardest problems in the literature.
  - No non-90° corners, so no mitred trim.
  - No straight skeleton. Every roof is per-rectangle, with an explicit join at courtyard corners.
  - No `innerRectangle` workaround, the CityEngine fix for irregular lots.
- Straight-skeleton roofs fail most often on footprints with holes. Borough's courtyards would be that
  case, so per-wing roofs are also the robust choice.

### 1.4 Context comes from edge labels, never from neighbour queries

- CGA's occlusion queries are expensive and opt-in. Spider-Man's cross-building dependencies cascaded
  through whole blocks on small edits. WFC re-solves can change modules the player never touched.
- Kelly & Wonka and Blosm label each edge as street, side or back, and style by label.
- *Inference.* The simulation already owns frontage, so it can supply labels per edge. The useful set
  is street, party wall, side, courtyard and back. A Building then depends only on its own snapshot,
  its id and its labels. An edit dirties that Building and at most the neighbours whose party-wall
  labels change.
- Street-level coherence needs a separate input. Instant Architecture keeps choices consistent within
  one building through a control grammar, but offers nothing across a street. A street or district
  style must be passed in as an attribute.

### 1.5 Seeding and stability are solved problems

- Seed each choice from a counter hash of world seed, Building id and a purpose tag. Add the face,
  bay or storey index where the choice is per element.
- Instant Architecture fixes one random value per rule per building, so the same rule chooses the same
  window everywhere on that building. Borough's hash scheme gives this for free.
- Derive bay and storey counts from integer dimensions with fixed rounding, so small changes do not
  flip counts.
- Version the generator. A version tag tells a deliberate style change apart from a bug.
- All of this lives in the shell. Appearance never enters the State Hash (ADR 0150).

### 1.6 Godot has no Nanite, so detail must be banded by distance

- A MultiMesh culls and picks its LOD as one unit. Per-instance data is at most 80 bytes.
- CS2's shipped frame spends 6,705 draws and ~40 ms on shadows. Shadows of small pieces are the real
  cost, more than draw calls.
- Report C proposes three bands, with visibility-range margins for hysteresis and dither fade.

| Band | Distance | Representation |
|---|---|---|
| Near | < ~150–250 m | Generated shell + instanced kit pieces, small chunks |
| Mid | ~250–500 m | Generated shell + façade shader |
| Far | > ~500 m | Today's box MultiMesh + shader |

- Keep the split structure identical across bands so windows do not jump at a transition.
- Visible state (lit windows, abandonment, occupancy) goes in per-instance data or a data texture.
  A state change costs a region upload. It never remeshes.
- Build geometry on worker threads as plain arrays. Upload on the main thread under a per-frame budget.
  Zylann's Voxel Tools uses this design.
- No published figure exists for per-building generation time in Godot. It needs a local measurement.

### 1.7 The instance-count alarm is overstated

- `07-the-drawing.md` §3.2 prices the kit at 15 instances × a million Buildings = 15M instances.
- `plans/0013-tick-budget.md` counts 120,001 Buildings in a 1M-Citizen city.
- *Inference.* At 120k Buildings, 15 instances each is 1.8M instances and ~144 MB of instance data
  if every Building carried the kit. Under banding only near chunks carry it. The kit is affordable
  near the camera and still unaffordable city-wide. The conclusion in §3.2 stands, but the number
  behind it is ~8× too high.

### 1.8 Authoring conventions for a Blender kit

- 1 Blender unit = 1 m. Fixed bay width and storey height per style. Borough's storey is 3.5 m.
- Pivot at the bottom-left of the outer face for wall pieces, bottom-centre for props.
- Sockets as empties named `socket_<role>`. Metadata in glTF extras, imported as `metadata/extras`.
- Shared trim sheets and texture arrays per style, so the near band draws with few materials.
- Fit a frontage by repeating whole bays and stretching each slightly. Keep windows at fixed size
  inside the stretch. Absorb the remainder in filler pieces such as pilasters and downpipes.
- Bethesda's rules still hold. Use one module footprint, sub-kits that are multiples of it, fixed
  pivots and few connectors. Fight repetition with clutter.

---

## 2. Where the internal positions stand

### 2.1 Families versus kit is a smaller conflict than it looked

| Source | Position |
|---|---|
| Architecture pass 03 | Choose a whole Building family by eligibility (footprint, storeys, use, access, party walls, ground). Vary length only by repeating bays. Never interpolate vertices, roofs or stairs. Report a gap or use an authored fallback when nothing fits |
| Drawing doc, pipeline C | Assemble a kit of parts procedurally along the frontage |

- *Inference.* Pass 03's family is exactly a ruleset over a kit. "Vary length only by repeating bays"
  is the 1D split grammar of §1.1. The two positions describe one system at different levels.
- The real open question is the unit of selection. One option picks a family per Building and lets
  its rules fill every face. The other lets placement rules pick pieces per face or per bay from a
  shared kit. The session decides this.

### 2.2 Stale or contradicted claims found

| Where | Claim | Status |
|---|---|---|
| `research/city-architecture/README.md` | The research was not conducted | Passes 01–03 exist under `output/` |
| `docs/07-the-drawing.md` §6.1 | Travellers are one cube | Stale |
| `docs/07-the-drawing.md` §6.1 | Sealing row | Contradicts the overlays row in the same table |
| `docs/07-the-drawing.md` §1.4, §3.2 | A million Buildings | A 1M-Citizen city holds ~120k (`plans/0013`) |

All four were corrected on 2026-09-23.

---

## 3. Measurements the design needs

| Question | Why it matters | How |
|---|---|---|
| Time to generate one Building shell in C# | Sets the upload queue and the redevelopment latency | Microbenchmark on worker threads, Release |
| Main-thread upload cost per shell | Sets how many edits appear per frame | Driven shell run with a per-frame budget |
| Frame cost of the near band at a dense street view | Sets band distances and chunk size | Driven capture, named machine and world |
| Shadow cost with shells versus boxes | CS2 lost ~40 ms here | Same capture, shadows toggled per band |

Taken 2026-09-23 on the dev machine, not quiet, so upper bounds. Full conditions in
[the evidence](../../plans/evidence/procedural-buildings/README.md).

| Question | Result |
|---|---|
| Generation | About 10 ns per vertex on one thread: 7.8 µs for a house, 196 µs for a 20-storey tower |
| Upload | 80–90 ns per vertex on the main thread, about 0.3 ms per Building in a low-rise 1M-Citizen city. The first upload costs 7 ms |
| Near band | Shells out to 500 m (327 Buildings, 1.1M vertices) add 0.25 ms per frame at a street view. The whole-city opening camera already runs at 12.9 fps with boxes alone |
| Shadows | Sun shadows cost 2.0–2.4 ms GPU. Shells casting them add 0.1–0.3 ms |
