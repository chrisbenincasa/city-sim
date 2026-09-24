# Procedural Buildings — Design Session

A session to settle how Borough's Buildings should behave on screen. Each answer fixes part of the
generator. Findings and evidence are in [REPORT.md](REPORT.md).

## How to run it

- Take the questions in order. Earlier answers narrow later ones.
- Each question lists options, what each forces in the system, and a recommendation.
- Record each answer in the table at the end. The table becomes the plan's requirements.

## Fixed inputs

These are already decided and the session does not reopen them.

- Appearance is derived in the shell from the snapshot and the Building id. It never enters the State
  Hash (ADR 0150).
- The player authors ground and streets, never Buildings (drawing pillar 1.2).
- Every mark reports a fact the city holds, or is marked as invented (pillar 1.3).
- Footprints are grid rectangles, courtyard wings or podium/tower. Storeys are 3.5 m. Buildings do not
  shrink, and redevelopment replaces them.
- Building assets are authored in Blender first.
- The settled look holds. The default camera is high. Kinds announce themselves. The palette is
  vibrant but not garish. Light follows the clock and windows light from occupancy.

---

## Q1. What must a Building tell the player, and from how far?

A fact shown far away must live in the far band, which is one box and a shader. A fact shown only
close up can use geometry.

| Fact | Far (whole district) | Mid (a street) | Near (a doorway) |
|---|---|---|---|
| Kind (house, shop, works, school) | ? | ? | ? |
| State (occupied, empty, abandoned) | ? | ? | ? |
| Wealth or upkeep | ? | ? | ? |
| Age or era of construction | ? | ? | ? |
| Form (terrace, courtyard, tower) | ? | ? | ? |
| Activity (deliveries, queues, workers) | ? | ? | ? |

- A fact marked Far forces a shader or colour channel in per-instance data, which holds 8 floats.
- A fact marked Near only can use kit pieces and can wait for the near band.
- Age needs care. Pass 03 keeps original date, intervention date and condition independent. The
  simulation may not hold all three, and pillar 1.3 forbids inventing them silently.

**Recommendation.** Kind and state at Far, form and wealth at Mid, era and activity at Near. This
matches what the far box can already carry.

## Q2. How much does close-up beauty matter?

This is the undecided fifth pillar in `07-the-drawing.md` §5. It sets whether a near band exists
and how much kit it needs.

| Option | Behaviour | System |
|---|---|---|
| A. Reading city | Close zoom shows the same box with more shader detail | No near band. Mid band optional. Smallest build |
| B. Photographable streets | Close zoom shows silhouettes, including cornices, balconies, shopfronts and real roofs | Near band with generated shells plus a kit of ~20–60 pieces per style |
| C. Walkable street level | Close zoom holds up at eye height | B plus interiors, props and street furniture. Large authoring cost |

**Recommendation.** B. The four cheap pillars already produced a photograph worth taking. C costs an
art team Borough does not have.

## Q3. How alike may two Buildings of the same kind and size look?

This sets family count, kit size and seeding depth.

| Option | Behaviour | System |
|---|---|---|
| A. Recognisably the same | Same family, tints and props vary | One family per kind and form. Seed colour and props only |
| B. Same family, different faces | Bay patterns, doors, roofline and materials vary | Families with weighted choices per rule, one seeded value per rule per Building |
| C. Every Building distinct | No two share a composition | Many families per kind or deep grammars. Tuning cost grows fast |

- Repetition is the standing complaint in Cities: Skylines. Shipped kits fight it with clutter and
  tint before they add pieces.

**Recommendation.** B.

## Q4. What is the unit of choice?

Pass 03 and the drawing doc agree on bay repetition. They differ on where the choice happens.

| Option | Behaviour | System |
|---|---|---|
| A. Family per Building | One family chosen by eligibility dresses the whole Building. A terrace house looks like a terrace house on every face | Eligibility filter, then a per-family ruleset. Authored fallback when nothing fits |
| B. Pieces per face | Rules pick pieces per face or bay from a shared kit. Faces of one Building can mix freely | One large rule set over one kit. Coherence must be enforced by a control grammar |
| C. Family per Building, face rules within | A as the outer choice. Inside the family, rules differ by edge label (street, party wall, courtyard, back) | A plus edge labels from the simulation's frontage data |

**Recommendation.** C. It keeps pass 03's identity per family and uses the edge labels the simulation
already owns.

## Q5. What happens when no family fits?

| Option | Behaviour | System |
|---|---|---|
| A. Authored fallback | A plain generic Building of the right size appears | Every kind needs one fallback family that accepts any footprint |
| B. Visible gap marker | The Building shows as a marked placeholder | A debug-style family, plus a report of which inputs failed |
| C. Both, by build | Fallback in play, gap marker in development builds | A and B, switched by build flag in the shell |

**Recommendation.** C. Pillar 1.3 favours showing that something is invented, but players should not
see debug art.

## Q6. How do streets and districts cohere?

Instant Architecture keeps choices consistent within one Building only. Coherence across a street
needs an input.

| Option | Behaviour | System |
|---|---|---|
| A. None | Each Building is styled alone | Nothing beyond Q4 |
| B. Terraces read as one | Attached runs share cornice line, material and bay rhythm | A run id and shared seed passed to each member. The simulation must say which Buildings form a run |
| C. District style | A neighbourhood has a style, such as Victorian brick or post-war render | A style attribute per area. It must come from a simulation fact (build date, Ruleset region) or be marked invented |

- Cross-building queries are out in every option. They cascade on edits (Spider-Man) and cost heavily
  (CityEngine).

**Recommendation.** B now, C later if the simulation gains a fact to key it on.

## Q7. How does a Building change during its life?

| Option | Behaviour | System |
|---|---|---|
| A. Swap at redevelopment only | Appearance is fixed from construction until replacement | Remesh only on replacement |
| B. A plus state wear | Lights, boarding, grime and dark storeys follow condition and occupancy | State in per-instance data. No remesh |
| C. B plus additions | Extensions, extra storeys, rooftop additions appear | Needs a simulation fact for each addition. Remesh on change |
| D. Construction shown | Scaffolding and staged build | Extra kit and staged state. Needs a construction phase in the simulation |

- Buildings do not shrink, so C only adds.

**Recommendation.** B. C and D wait until the simulation holds the facts that would drive them.

## Q8. Where does style come from?

| Option | Behaviour | System |
|---|---|---|
| A. One house style | Every city looks like one region | One kit, one family set |
| B. Context preset per city | The player picks a region at map start (northern English, Pacific Northwest, Dutch) | One kit and family set per preset. Pass 03 recommends this |
| C. Blend within a city | Styles mix by era or district | B plus Q6's district attribute |

- A slider is out. Pass 03 recommends presets over a slider.
- The player never authors Buildings, so a preset chosen at map start does not break pillar 1.2.

**Recommendation.** A for the first build, designed so B is a second data set.

## Q9. How many Buildings must be drawn, how fast, on what?

| Parameter | Proposed | Notes |
|---|---|---|
| Buildings in the largest city | ~120,000 | `plans/0013` count for a 1M-Citizen city |
| Buildings in the near band at once | ? | Decides near chunk size and kit budget |
| Frame budget for Buildings | ? | Shares 16.6 ms with terrain, agents and UI |
| Delay before a new Building appears | ? | Sets the upload queue. Old mesh stays until the new one is ready |
| Target hardware | ? | Performance claims need a named machine |

**Recommendation.** Answer these with the measurements in REPORT §3 before committing band distances.

## Q10. Who authors, and can players mod it?

| Option | Behaviour | System |
|---|---|---|
| A. Team only | Families and kits ship with the game | Rules may live in C# |
| B. Moddable data | Families and rules are data files. Modders add kits in Blender | Rules as data (TOML or JSON). A family schema, validation and a loader |
| C. Full grammar | Modders write rules in a small language | A parser and interpreter. Rule authoring is programming, per Instant Architecture's authors |

- Cities: Skylines depends on its Workshop to survive repetition complaints.
- Designer tuning already lives in TOML Rulesets. Appearance data would be a shell-side sibling.

**Recommendation.** B, with the schema designed now and modding tooling deferred.

---

## What the answers set

| Component | Set by |
|---|---|
| Per-instance data layout and far shader | Q1, Q7 |
| Whether a near band exists and its kit size | Q2, Q3, Q9 |
| Family model and eligibility filter | Q3, Q4, Q5 |
| Edge labels the simulation supplies | Q4, Q6 |
| Run ids or district attributes | Q6, Q8 |
| Remesh triggers and the upload queue | Q7, Q9 |
| Rule format and loader | Q10 |
| Number of kits and families to author | Q2, Q3, Q8 |

## Resulting system

Session held 2026-09-23. These follow from the Decisions table below.

| Part | Decided shape |
|---|---|
| Detail levels | Far keeps today's box and shader, redrawn in real material colour, plus landmark pieces. Mid adds a generated shell per Building with per-wing roofs. Near adds instanced kit pieces in small chunks. The split into storeys and bays matches at every level |
| Selection | One Appearance Family per Building, chosen by eligibility outside Godot. Faces follow edge labels. Distinct families draw from a larger pool |
| Seeding | Counter hash of world seed, Building id and purpose tag. Attached runs share a seed from block face and era bucket |
| State | Occupancy, time empty, time abandoned, age and emissions go through per-Building data with no remesh. A construction stage is reserved |
| Fallback | A generic Appearance Family per kind in play, a marker in development builds, and a headless coverage report |
| Style | Presets as data sets. First preset is present-day US Pacific Northwest |
| Authoring | Blender kits plus data-file Appearance Families with a schema and a checker |
| Camera | The eye rises over Buildings and never drops below ~10 m |
| Performance | 120k Buildings, 60 fps at the default camera, on the dev machine. Quality presets later |

## Follow-ups outside the generator

- Camera clearance: the eye currently enters Buildings. This is shell work and can ship alone.
- Construction phase: a simulation item with a Ruleset duration in game Days.
- Measurements in REPORT §3, on the named machine, after a reboot fixes the NVIDIA driver mismatch.
- First test street: pass 03's briefs, starting with US-sourced bodies.

## Decisions

| Q | Answer | Notes |
|---|---|---|
| Q1 | A Building shows only what the real building would show. Every distance draws a lower-detail version of the same building, and no distance uses code colours | Kind reads through architecture, as in "a school looks like a school". Information colours belong to overlays only. The random wall colour becomes the family's real material. The far version keeps mass, roof form, the kind's landmark feature (spire, chimney, sawtooth roof), material colour and lit windows. Openings, doors and trim fade out. |
| Q2 | B. Street from above. Detail holds up from rooftop height over a street. No interiors | The camera must never enter a Building. Today it can, because the nearest standoff is 32 m from a ground focus with a lowest tilt of 4°, which puts the eye about 2 m up with no collision (`Main.Camera.cs:363`, `Main.cs:55`). Windows keep a shader-painted room at most. When a move would put the eye inside a Building, the eye rises to clear nearby roofs and keeps its focus point. The eye never drops below about 10 m. |
| Q3 | B by default. A family may be marked distinct (option C), and those draw from a much larger pool of compositions and landmark pieces | Distinctness is a setting on the appearance family, since kinds come from the Ruleset. It holds by odds only. No city-wide assignment, so each Building's appearance stays a function of its own facts and id. Suited to civic kinds that appear a few times per city. |
| Q4 | C. One Appearance Family per Building, chosen by eligibility. Inside it, each wall follows its edge label: street, party wall, side, courtyard or back | Term added to `CONTEXT.md`. Street edge comes from the Lot's `Side`. Party wall depends on whether the adjacent Lot holds an attached Building, so a neighbour's build or demolition redraws that one wall. |
| Q5 | C. An authored fallback Appearance Family per kind in play. A marked placeholder and a miss log in development builds. The headless runner reports fallback use per kind over a long run | The eligibility filter must live outside Godot so the headless runner can call it. It still never enters `Borough.Core` or the State Hash. |
| Q6 | B. Attached Buildings on one block face built in the same era share Appearance Family, cornice line, material and bay rhythm. Later infill differs. Era-driven eligibility (option C) is prepared but off | Run seed = hash of block face and an era bucket of `RaisedAt`. No cross-Building queries. District styles by area are ruled out, since no per-area fact exists. Era bucket length is settled in implementation. |
| Q7 | B. Wear from recorded facts: occupancy, time empty, time abandoned, age and emissions, all through per-Building data with no remesh. D, construction shown, is wanted as a real simulation state planned separately. C, additions, is deferred | Construction phase is its own simulation item. A Building under construction cannot be occupied, its duration is a Ruleset value in game Days tuned for play, not realism, and Materials use may follow. It is a deliberate State Hash change. The drawing reserves a construction stage in its per-Building data. |
| Q8 | B. Style comes from a preset the player picks at map start. First preset is pure present-day US Pacific Northwest, with post-war stock, older survivors and recent infill. Dutch and German presets follow | Each preset is a data set of kit and Appearance Families. Pass 02 already retired northern English for the present-day direction. Pass 03's briefs are the first test street, with US-sourced bodies first. No blending across regions inside a preset. |
| Q9 | ~120,000 Buildings. 60 fps at the default camera, 45 fps at close street views. ~6 ms of Building frame share. New or changed Buildings appear in under 0.5 s, and state changes appear on the next frame. Target machine is the dev machine: i5-10400, GTX 1080 8 GB, 62 GB RAM | High fidelity first. Quality presets that scale band distances, shadow reach and shader detail follow later. Landmarks stay at far distance on every preset. The hardware floor is undecided, and the machine's UHD 630 iGPU is available as a low-end test. Band distances and chunk size come from the REPORT §3 measurements. |
| Q11 | Added 2026-09-24 after the test street's material review. A pitched roof is at least 18°. A shallower roof is flat behind a parapet | The 11° and 6° gables on A2 and W2 read as a flat roof with a crease from the game camera, and the long flat-roofed bodies read better. Pitched roofs belong on houses |
| Q10 | B. Appearance Families and their rules are data files with a schema and a checker, and kits are modelled in Blender | The schema is designed now. Modding tools and packaging are deferred. The files are shell-side siblings of the TOML Rulesets. |
