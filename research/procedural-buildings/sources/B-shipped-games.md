# B. How shipped games produce building visuals

Research input for Borough's building-appearance decision. Every factual claim carries a source number (see the Sources list at the end). Anything marked **[inference]** is my reading, not something a source states. Video-only talks (GDC Vault, YouTube) could not be watched. Where a talk's content matters, I say what its abstract or a secondary write-up confirms and flag the gap.

## 0. Findings up front

- **City builders with tens of thousands of buildings nearly all use a hand-authored asset library** (Cities: Skylines 1 and 2, Anno, SimCity 2013). The variety comes from lot-size × level × theme sets, colour masks, random props and shader tricks, such as CS2's occupancy-driven window lights. They buy authoring throughput from modders, which is why they need a Workshop.
- **Games whose footprints come from player-drawn or irregular shapes generate geometry to fit.** Examples are Manor Lords, Foundation houses, Tiny Glade, Townscaper and Citystate Metropolis. The shipped examples keep either the building count small (Tiny Glade, Townscaper) or the style narrow and the footprints roughly rectangular (Manor Lords, Foundation, Citystate).
- **Open-world cities use an offline procedural pipeline that places authored modular kit pieces.** Spider-Man, The Matrix Awakens City Sample and Cyberpunk's megabuildings all work this way. The City Sample is the clearest documented case: Houdini building volumes are styled by a shape grammar, and each building becomes hundreds of kit instances.
- **Visible change over time is usually coarse.** CS2 has unique meshes only at levels 1, 3 and 5. Foundation has a 3×3 density × quality matrix. Manor Lords makes a house "taller and better kept" at level 2. SimCity 2013 made state visible through cheap signals: cars parked, lights on, graffiti, attached modules.
- **Kit discipline (Bethesda) is the baseline for any assembled approach.** It needs one footprint, sub-kits whose footprints are multiples of each other, fixed pivots, a few standard connectors, and clutter to fight repetition.

## 1. Townscaper and Bad North (Oskar Stålberg)

**How visuals are produced.** Both games choose pre-modelled tiles with a constraint solver (Wave Function Collapse, WFC) and place them on a grid. Townscaper combines three earlier pieces of work [1]:
- Brick Block's marching-cubes-style tile selection, with tiles "authored as corner segments rather than whole building chunks" [1]. The tile for a cell is chosen by the filled/empty state of its corners. Four boolean corners give 16 cases, which reduce to 6 under rotation and reflection. Stacking floors makes it a 3D (2⁸) problem [2, community reconstruction].
- Bad North's WFC, which picks which tiles are valid for the shape the player made [1].
- An irregular grid first tried on Night Call, where grids were fitted to Paris blocks. Stålberg found "you can skew the pieces like quite a lot before it starts to become a problem" [1]. The grid is built by filling a hexagon with triangles, randomly merging pairs into quads, subdividing everything into quads and relaxing the result [1][3][4].

Tiles sit on the **dual** grid, meaning the tile's corners are at cell centres. Stålberg has argued publicly that others should cut tiles this way [5]. Hand-made tile meshes are deformed to fit each irregular quad. Community write-ups describe bilinear deformation in plan with extrusion upward, which stays seamless because neighbouring tiles share corner positions [2][6]. Stålberg himself describes "pretty radical cage deformation on the cells" in a later project [7].

**Inputs.** Players "can only add or remove blocks and choose their colour" [1]. Colour matters to the output. Stone garden walls appear at house corners or where colours change, which suggests property lines [1].

**Variety and repetition.** Every module has a priority. Gardens are "super high priority" and appear only in enclosed flat areas [1]. Hidden high-priority "recipes" produce stairwells, statues and floating buildings that players can discover [1]. Decoration runs after the solve. Windows, chimneys, bushes and benches go on available surfaces, and windows are cut with a stencil buffer so they never split across corners [1]. Stålberg's stated intent is "I want the larger shapes to feel very predictable, but the smaller shapes are allowed to vary quite a bit" [1].

**Change over time.** Each edit re-solves the connected structure. Detail can pop in after a block appears, and larger builds take longer [1]. Townscaper "is actually allowed to fail silently", which is most visible on long thin steel supports [1]. Bad North resets to a saved state when the solve fails [1].

**Authoring and cost.** Bad North has "over 300 tiles", built in Maya. Their edges were scanned in Unity to work out adjacency automatically [1]. A Townscaper reskin would need "somewhere in the region of 500 tiles", and Stålberg would not want to spend "like two months" redoing the set [1]. A Blender-based clone, TownBuilder, stores socket metadata per module [8] (not Stålberg's pipeline).

**LOD/performance.** Bad North merges the finished island into one mesh to cut draw calls [1]. Neither game needs city-scale LOD **[inference]**, because both are diorama-sized.

**Relevance to Borough.** The corner-driven, dual-grid tile approach suits a whole city block with irregular footprints and courtyards, since the tiles follow corners and courtyards are just empty cells. It does not directly give per-kind identity (school versus shop). Townscaper's buildings have no function. Solve time grows with connected size [1] **[inference: Borough would need to solve per Building or per Lot, not per contiguous block]**.

Talks: EPC 2018 "Wave Function Collapse in Bad North" [9]; IndieCade Europe 2019 "Organic Towns from Square Tiles" [10]; SGC 2021 "Beyond Townscapers" [11]; Konsoll 2021 "The Story of Townscaper" [12].

## 2. Tiny Glade (Pounce Light)

**How visuals are produced.** Geometry is generated at runtime from player strokes. The game began as a weekend "procedural wall generator" in Bevy (Rust) [13][14]. There is no master algorithm. There are "a lot of algorithms and generators, each tailored to a specific element of the game", each supporting "creation, assembly, morphing, or deforming" [14]. Generation runs on CPU and GPU, and compute shaders were essential [14]. Opara says "I create every single rule that goes into the system" [14].

**Inputs.** Players draw walls and paths and drag building outlines. Towers can be raised, and roofs react. A "tile-jumping" roof effect plays when a tower rises quickly [15]. Buildings can be "glued" together, with tower roofs hiding the seams [16]. Windows damage the plaster around them automatically [17].

**Change over time.** Walls "would just rearrange themselves" as the player edits. Stachowiak calls this the most important thing in the game [14]. One player reports that the game "redraws every brick each edit" and has a 1-million-brick cap [18] (a player claim, not official).

**Variety.** Variety comes from the rules. Each brick, stone and roof tile is placed by a generator, so no two walls repeat **[inference from 14, 15]**.

**Authoring cost.** The authoring cost is very high in engineering and low in asset count. Two people hand-wrote every rule [14]. Adding hand-placed doors led to long redesigns once the feature was "plugged into the crazy procedural machinery" [14]. Elevating buildings for bridges brought in a new subsystem with its own problems (supports, overlaps) [14].

**LOD.** No source covers it.

**Relevance.** Tiny Glade is the reference for gorgeous freeform procedural architecture. It is built around a few dozen player-shaped buildings, not tens of thousands **[inference]**. No technical talk on its algorithms turned up. The public record is demo clips and one interview [14][15][16][17].

## 3. Manor Lords (Slavic Magic)

**How visuals are produced.** Houses are fitted to player-drawn burgage plots. The player drags a free polygon, and the first edge sets which way the houses face [19][20]. There is no grid [21]. Before confirming, the player sees house "shadows", extension space and backyard space, and can press +/− to change the number of houses [19][22].

**Inputs.** Plot width sets how many houses fit and whether a double-width plot can take a "living space extension". Plot depth sets whether a backyard exists and how big it is [19][22]. Backyard extensions include vegetable garden, orchard, chicken coop, goat shed, apiary and artisan workshops [22][23]. Backyard size affects the output of gardens and orchards only [19].

**Change over time.** Building is staged. At level 2 "the building will become noticeably taller and better kept" [19]. Level-2 extensions are locked once chosen [19].

**Authoring.** No developer write-up on the generator turned up. The nearest is Styczeń's 2025 remark that fitting rectangular plots into circular areas is "not pleasant", along with a planned rework [24]. **[Inference]** Visually, the houses look like a small set of authored timber-frame house types placed along the street edge, with authored backyard props filling the rear. The mesh itself does not appear to be generated to the polygon. The polygon drives count, spacing and yard contents.

**Relevance.** Manor Lords is the closest shipped analogue to Borough's Lot → Building flow. The frontage sets the count and orientation, depth sets the rear use, and level changes the height. It works at village scale (hundreds of buildings) **[inference]**.

## 4. SimCity (2013) / GlassBox (Maxis)

**Design principle.** Ocean Quigley's GDC 2013 talk "Building SimCity: Art in the Service of Simulation" says the art provides "a toolkit for city creation", sustains "the illusion of a living city", and shows "what's going on in the underlying simulation". It also describes the "dynamically composable world" and the building authoring techniques [25][26]. The video was not watched. In interview Quigley said "if we can assign meaning to something, then we assign meaning to it", and called the city "UI that's telling you the state of the simulation" [27].

**State shown on buildings** [27]:
- A car parked in front means someone is inside.
- Lights on means power is on.
- Graffiti means a crime occurred there.
- Industrial architecture follows city education level. Low education gives 1890s Lowell-style mills, middle gives a 1970s–80s look, and high gives Silicon-Valley office parks or biotech plants. The look regresses if schools are cut.
- Power plant architecture dates to its technology (coal 1950s–60s through to "gleaming" advanced plants).

**Zoned buildings.** Wealth (§, §§, §§§) and density (low, medium, high) are independent axes [28][29]. Density follows the capacity of the adjacent road and each building's happiness. Wealth follows land value and can flip "almost instantly" [28]. **[Inference]** This implies authored building sets indexed by zone × wealth × density that swap when the state changes.

**Player-placed buildings** are modular. Players attach modules such as extra fire-truck garages, police cells, school classrooms and helipads wherever they choose on the building, and each module changes both look and function [30][31].

**Process.** Quigley prototyped looks in Maya and in-game, Andrew Willmott tied each look to the simulation, and a lead artist's team scaled the content [27].

**Relevance.** SimCity's lesson is about which facts to render, not how to model them. Borough already derives appearance from the snapshot, and SimCity shows that cheap per-building signals (lights, cars, graffiti, era-coded style) carry more simulation meaning than geometry variety.

## 5. Cities: Skylines 1 and 2 (Colossal Order)

**Approach.** Both games use a hand-authored asset library. Every growable is an authored mesh for a fixed zone, lot size and level.

**CS2 inputs and lots.** A cell is 8×8 m. Zoned lots run from 1×2 up to 6×6 cells (width along the road × depth), and the zoning algorithm picks the deepest lot first, then the widest [32]. As land value rises, large plots split into smaller lots [33]. There are 5 levels, but the base game has unique meshes only at levels 1, 3 and 5. Levels 2 and 4 are "non-visual upgrades" [32]. Custom zones should ship "at least one complete level 1–5 set for every lot size" [32]. Levels rise when rent exceeds upkeep and residents reinvest [33].

**CS2 variety mechanisms** [32][34][35]:
- A ControlMask with three mutually exclusive RGB colour channels. Paradox calls the resulting combinations "effectively limitless" [34].
- Props with per-prop spawn probability sliders [35]. Generic items like AC units and fire escapes are "best made as props" [32].
- Procedural window interiors. Window UVs map to 25 room variations per UV map, with curtains and night lights "randomized based on building occupancy". Interiors are selected by mesh-name keywords (Industrial, Office, ResidentialHigh, and so on) [32].
- Procedural foundations generated from geometry at y=0, a shader for glass, and terrain projected onto grass [32].
- Pubs in the UK pack come in three sizes per style [35].

**CS2 LOD and budgets.** Two LODs are required. LOD1 is under 50% of main triangles and shares textures. LOD2 is baked and at most 500 triangles, with 512–1024 px textures, plus an LOD2 window sub-mesh with identical UVs [32]. Average and soft-max polygon budgets are, for example, 8k/15k for low-density houses, 6k/14k for row housing and 50k/95k for high-density housing [32]. Virtual texturing downsizes textures by settings [32].

**CS1.** A single `_lod` mesh is used. The suggested limit is about 100 triangles for growables and 200 for monuments, with 128×128 LOD textures, and "the LOD model often has a bigger effect on runtime performance than the high-detail version" [36]. Up to 4 colour variations come through a one-channel `_c` mask [37][38]. Sub-buildings do not appear on growables [38]. Night windows use the random illumination range of the illumination map [39].

**Authoring and modding.** The pipeline expects FBX, one material per main mesh, a bottom-centre pivot and exact naming [32]. Blender is not named, only "3D software of your choice" [32]. **[Inference]** Each look is a whole authored building for one lot size × level × zone × theme. The matrix is large, so a studio cannot fill it alone. The Workshop exists to multiply authoring throughput, and players install thousands of assets to beat repetition. Workshop authors themselves say "what the game need most is variety" [38].

**Relevance.** The CS2 per-building shader tricks transfer directly to any approach Borough picks, because they need only a stable id and occupancy: occupancy-driven window lights, colour masks keyed by id, and probabilistic props. The library approach itself does not fit arbitrary polygon footprints or four-wing courtyards.

## 6. Other city builders and modular-piece systems

### 6.1 Anno 1800 / Anno 117 (Ubisoft Blue Byte)

Anno uses fully authored hero assets on a grid. A high-poly model, sometimes over a million polygons, is baked to a low-poly one. The budget is "250 polygons and a texture resolution of 256 pixels" per grid cell [40]. Each residence tier is a "snapshot" of a social class, so the art tells the story [41]. Residences have several model variants, cycled with Shift+V [42]. Players complained at launch that "the designs are very good, just not varied" [43]. Skyscrapers (The High Life DLC) are modular. They stack up to 5 levels for Investors and 3 for Engineers, with variants per module, and the style follows the base residence [42]. In Anno 117 an Albion house can upgrade to a Roman or a Celtic line [44]. Cosmetic DLC reskins houses and walls [45].

### 6.2 Foundation (Polymorph Games)

Foundation uses two systems:
- **Houses** are generated from painted Residential zones. Villagers place foundations, and the layout is "determined automatically when its foundations are first placed, depending on the available Residential zone". The fence shape then stays fixed through upgrades [46]. Density tiers 1 and 2 share a footprint, while tier 3 "will fill out the fence boundary almost entirely" and "adjust its size and shape around" objects placed in the garden [46]. Quality tiers change the materials, going from wattle and daub with thatch, to half-timber with shingles, to masonry [46]. That gives a 3×3 density × quality matrix, and houses can degrade [46][47]. Plots are irregular and "no two plots are the same" [48].
- **Monuments** (churches, taverns, castles) are assembled by the player from authored parts. Parts come in a `BuildingPartList` with `RequiredPartList` constraints, for example CORE×1, EXTENSION×2, DOOR×1. Constructor types include scalable towers and bridges, and players can resize some parts vertically and cycle roof options [49][50].

**Relevance.** Foundation is the best shipped analogue for "the simulation decides the footprint, the shell derives the look, and the look changes with tiers". The runtime assembly technique is not publicly documented.

### 6.3 Citystate Metropolis (solo developer, Early Access pending)

Residential, commercial and industrial buildings "are procedurally generated". Lots can be any polygon or curve, but the building footprint "is based on a grid inside the lot", while "the assets used to feed the procedural generation can still have curves" [51]. Players can draw the building, set setbacks, floors and parking, or pick a preset and zone many lots [51][52]. Its predecessor, Citystate II, used 300+ unique authored buildings [52]. **Relevance:** it targets exactly Borough's design point of polygon lots, generated buildings and a large city. It is unreleased, so it is evidence of intent only.

### 6.4 Workers & Resources: Soviet Republic

Construction is simulated per site. Materials and workers are delivered per stage, starting with groundworks [53][54]. Sources confirm staged construction with visible vehicles but do not describe the stage visuals.

### 6.5 Sim Settlements / Sim Settlements 2 (Fallout 4 mod)

Players zone plots (residential, commercial, industrial, agricultural) and settlers build random plans that upgrade over several levels, with every spawned item refreshed at each stage [55]. SS2 ships 120+ building plans, tagged by theme "so new buildings can automatically fit in" [55]. Some add-on plans show no visual change across levels [55]. **Relevance:** this is a plot-plus-staged-plan system built entirely from a pre-existing modular kit (Fallout 4's), and the community extends it with plans.

### 6.6 Islanders, Dorfromantik

Islanders procedurally generates islands, while its buildings are fixed authored models [56]. Dorfromantik's tiles are a data structure of seven sections (six edges plus a centre) with authored content per type [57]. Neither is relevant to building generation.

## 7. Open-world cities

### 7.1 Marvel's Spider-Man (Insomniac, 2018)

Houdini was used to "author the ground, buildings, traffic, vignettes and crimes, props, and impostors" [58]. Santiago's GDC 2019 talk covers scoping procedural systems "so their results can still be refined by hand". It says the tools ended up used to "author, modify and monitor much more content than planned" [59]. The technical postmortem covers "procedural tools for marking up Manhattan" [60]. The talks were not watched, so details of facade generation are unconfirmed. **[Inference]** The pattern is an offline procedural pass plus hand refinement, with impostors for distance.

### 7.2 The Matrix Awakens / UE5 City Sample (Epic)

This is the best-documented procedural city. Houdini generates the city shape, roads, freeway and building placement, and exports "a huge point cloud" with metadata. A UE5 "Rule Processor" converts it into instances [61]. "Buildings are constructed from a volume". "The building generator uses a shape grammar language to style the building volume", and "each different building style has a different set of rules". One volume can take one style for its base and another above [61]. "Each building in the city is made up of hundreds of instances" of modular pieces. Custom geometry is kept to collisions, the freeway deck and "custom roofs for non-rectangular buildings" [61]. All meshes are Nanite, with no traditional LODs [61]. The city holds about 7,000 buildings built from thousands of modular pieces and 7 million instanced assets. It shipped 22 modular building kits [62]. UE 5.8 adds a PCG-built version with building primitives [63].

**Relevance.** The City Sample is the clearest precedent for "a simulation-owned volume, then grammar rules per style, then authored kit instances". Non-rectangular roofs are the one part generated as custom geometry, which is exactly where Borough's polygon footprints will hurt.

### 7.3 Microsoft Flight Simulator (Blackshark.ai)

About 1.5 billion buildings were reconstructed. A neural network segments footprints, height, rooftop and type from imagery. Facades are then added "based on location and context" from typology-sorted "mix-and-match parts" [64][65]. The CEO estimated perhaps "20% of the buildings are off", and said "you cannot do traditional Q&A anymore" [65]. **Relevance:** at extreme counts, the only viable approach is footprint + height + roof type + typology, then kit parts, while accepting visible errors.

### 7.4 Cyberpunk 2077, GTA V, Watch Dogs

- **Cyberpunk.** Megabuildings are one modular exterior/interior set reused across Night City [66]. A polish pass reworked the colour masks "to support multiple schemes from one mask texture", added exterior plate variations and unique numbers per district for landmarking, and built distant proxy meshes [67].
- **GTA V.** The city was whiteboxed by hand, then built out over about four years [68][69]. Rockstar describes GTA VI as deliberately bespoke [69].
- **Watch Dogs: Legion.** The public procedural work covers citizens (Census), not buildings [70].

## 8. Bethesda modular kits (Burgess & Purkeypile, GDC 2013)

This is the authored-kit baseline [71][72]:
- A kit is "a system", more than the sum of its parts. Fallout 3's pipe kit had 4 pieces [71].
- **Global standards come first.** A character is 128 units tall, one door-frame size is used game-wide, the narrowest space is two character widths, and slopes are 30–45° [71].
- **Footprint.** Every piece fits a bounding footprint and touches its edge only at snap points. Sub-kit footprints must be multiples of each other: a 512³ room tiles with a 256³ hall, but a 384 room "will eventually create gaps and/or overlaps". Grid snap is half the footprint [71].
- **Tiling.** "Avoid attempting to create a kit which tiles on all axes." Halls tile on one axis and rooms on two, and vertical stacking gets its own shaft sub-kit [71].
- **Pivots** are bottom-centre by default and fixed at graybox, because changing them later breaks hundreds of placements [71].
- **Connectors.** Glue kits bridge different kits, archways hide pivot-and-flange angle gaps, directional kits need a "De-Twist" piece, and editor-only helper markers show snap points [71].
- **Scale economics.** 2 kit artists made 7 kits used in "well over 400 cells" over about 2.5 years. The Cave kit was used over 200 times, with 7 sub-kits and about 50 pieces in one sub-kit [71].
- **Art fatigue.** Players notice repeated clutter before repeated architecture. Kits should be decoupled from gameplay, mixed with each other ("kit-bash"), allowed off the 90° grid, and their seams hidden with lighting and AO. Kits that are always square "will always feel like a kit" [71].
- Burgess later summed it up as "embrace and respect some core rules and break them very deliberately" [73].

## 9. Comparison table

| Game | Generation approach | Inputs | Change over time | LOD approach | Authoring cost |
|---|---|---|---|---|---|
| Townscaper | Corner/marching-cubes tile selection + WFC on irregular dual grid; tiles deformed to cells; post-solve decoration | Filled cells + colour | Re-solve on each edit; silent failure allowed | Not needed (diorama) | ~500 tiles per style; months per tileset [1] |
| Bad North | WFC over >300 Maya tiles, multi-cell tiles, nav constraints | Island seed + constraints | Static per level | Merge island to one mesh | >300 tiles [1] |
| Tiny Glade | Runtime per-element generators (CPU+GPU) | Player strokes, heights, glue | Live re-generation per edit | Not documented | 2 people hand-writing all rules; heavy engineering [14] |
| Manor Lords | Authored houses fitted along a drawn plot; count by width, backyard by depth | Plot polygon, house count, extension choice | Level 2 taller/better kept; staged construction | Not documented | Moderate (inference) |
| SimCity 2013 | Authored sets by zone × wealth × density; player-attached modules on civic buildings | Zone, road capacity, land value, happiness, education | Swap on wealth/density change; modules; state decals (lights, cars, graffiti) | Not documented | Large studio team [27] |
| Cities: Skylines 2 | Authored library per lot size (1×2..6×6) × level × zone × theme | Zone, lot, level, occupancy | Unique meshes at levels 1, 3, 5; window lights by occupancy | 2 LODs (≤50%, ≤500 tris) + virtual texturing | Very high; relies on modders [32] |
| Cities: Skylines 1 | Authored library, 4 colour tints | Zone, lot, level | Level swap | 1 LOD, ~100 tris | Very high; Workshop [36][38] |
| Anno 1800 | Authored hero assets; modular skyscraper stacks | Tier, player variant choice | Tier upgrade = new model | Not documented | 250 polys/cell budget; large team [40] |
| Foundation | Houses generated to painted-zone plots; monuments from authored parts | Zone shape, density, quality, garden objects | 3×3 density × quality; tier 3 fills plot; degrade | Not documented | Moderate; parts are moddable [46][49] |
| Citystate Metropolis | Procedural buildings on grid inside polygon lots | Drawn footprint, floors, setbacks, parking | Unknown (unreleased) | Unknown | Solo dev [51] |
| Sim Settlements 2 | Authored staged "plans" from Fallout kit on plots | Plot type/size, theme | Staged levels | Engine default | 120+ plans, community extensible [55] |
| Spider-Man | Offline Houdini procedural + hand refinement | Markup of Manhattan | Static | Impostors [58] | Large team + procedural tooling [59] |
| City Sample | Houdini volumes → shape grammar per style → kit instances | Volume, style rules | Static | Nanite, no LODs | 22 kits; ~7k buildings [61][62] |
| MSFS 2020 | ML footprint/height/roof/type → typology kit parts | Satellite imagery | Static | Streaming | ~50 people for 1.5B buildings; ~20% wrong [64][65] |
| Skyrim (kits) | Hand-placed authored kit pieces on grid | Designer placement | Static | Engine | 2 artists → 7 kits → 400+ cells [71] |

## 10. What each approach buys and costs

### 10.1 Hand-authored library (CS1/CS2, Anno, SimCity 2013 zoned buildings, Islanders)

- **Buys.** It gives the highest per-building quality and identity per kind. Artists control every silhouette, the LODs are hand-tuned, and the result is cheap at runtime. CS2 runs tens of thousands of buildings on 2-LOD meshes with instancing and virtual textures [32].
- **Costs.** Footprints must come from a fixed catalogue of lot sizes (CS2 uses 1×2 to 6×6 cells [32]). Content grows multiplicatively: lot size × level × zone × theme. Repetition is the constant complaint [38][43], and a modding Workshop becomes the answer. Change over time happens as discrete swaps, often only at some levels [32].
- **Pushed toward by** strong identity per kind, rectangular lot catalogues, a big art team or a modding community, and huge counts with modest per-building variety.
- **Pushed away by** player-drawn or irregular footprints, courtyard or wing shapes, and small teams.

### 10.2 Modular authored kit assembled by rules (City Sample, Spider-Man, Cyberpunk megabuildings, MSFS facades, Foundation monuments, Anno skyscrapers, SimCity modules, Sim Settlements plans, Bethesda kits)

- **Buys.** A few kits cover thousands of buildings. The City Sample's 22 kits cover about 7,000 buildings [62]. Artists still author every visible surface in Blender or Maya, so quality stays high. Kits scale with instancing, and a style change reaches everything at once [71]. Kits map naturally onto simulation facts: storey count sets how many floor modules stack, frontage sets bay count, and kind picks the kit or style ruleset **[inference]**. Parts can also represent growth, as with SimCity's attached modules [30], Anno's stacked skyscraper levels [42] and Foundation's CORE/EXTENSION parts [49].
- **Costs.** It needs Bethesda-style discipline: a fixed module footprint, multiple-of-each-other sub-kits and fixed pivots [71]. Non-rectangular footprints are the weak spot. The City Sample generates custom roofs for non-rectangular buildings [61], and Burgess warns that kits which stay square "feel like a kit" [71]. Kit repetition shows up first in clutter [71]. Assembly rules (a grammar or placement code) become their own engineering project [59][61].
- **Pushed toward by** huge building counts, per-kind identity through kit choice, visible change from adding modules or floors, simulation-driven dimensions, and modding (new kits and rules as data).

### 10.3 Grammar / procedural geometry (City Sample's shape grammar, Citystate Metropolis, MSFS reconstruction, Spider-Man Houdini passes)

- **Buys.** It fits arbitrary volumes and lots. A split grammar turns a footprint plus height into floors, bays and roof, and a different style is just a different ruleset [61]. It can run offline (City Sample, Spider-Man) or at runtime (Citystate Metropolis [51]).
- **Costs.** Rules are hard to debug and tune. Legion's team called procedural systems "a complex process that requires massive tuning" [70] (said of characters, but the point is general). Error rates at scale can be high, around 20% in MSFS [65]. Identity per kind depends on how good each ruleset is. Every shipped example still decorates with authored kit pieces rather than generating detail from nothing [61][64].
- **Pushed toward by** simulation-owned polygon footprints, dimensions that vary continuously (storeys, frontage), huge counts, and district style variety.

### 10.4 Constraint / WFC module selection (Bad North, Townscaper)

- **Buys.** It gives organic, coherent massing from a small set of hand-authored corner tiles. It handles irregular grids, courtyards and junctions between neighbouring masses. Tiles come with emergent "recipes" and decoration [1].
- **Costs.** It needs a large tileset, 300–500 tiles per style [1]. Solve cost grows with connected size, and the solver can fail or backtrack [1]. It needs a grid, even if irregular, and Borough's footprints are free polygons. Function is not expressed. Townscaper houses have no kinds, and per-kind identity would multiply the tileset **[inference]**.
- **Pushed toward by** irregular or organic footprints and blocks, wanting buildings to fuse with neighbours, and a small asset team with strong technical-art skills.

### 10.5 Runtime freeform mesh generation (Tiny Glade, parts of Foundation houses and Manor Lords plots)

- **Buys.** Geometry follows any player or simulation shape, change can be animated live, and no two buildings repeat [14][15].
- **Costs.** Each element (wall, roof, window, door) needs its own generator [14]. Every feature interacts with the "procedural machinery" [14]. Nothing public shows it at tens of thousands of buildings. Tiny Glade players report a 1-million-brick cap [18]. The shipped city builders that fit plots (Manor Lords, Foundation) keep the style narrow and the buildings smallish, and appear to swap authored variants per tier **[inference]**.
- **Pushed toward by** player-drawn shapes, visible continuous change (extension, growth), strong "handmade" charm, and small counts.

### 10.6 Cross-cutting techniques every family can use

These techniques key off a stable id plus simulation state, which Borough's snapshot already provides:
- Colour masks with id-seeded tints [34][37][67].
- Props with spawn probabilities [35].
- Occupancy-driven window lights and parallax interiors [32].
- SimCity-style state signals such as lights, parked cars and graffiti [27].
- Era or quality material swaps per tier [27][46].
- Distant proxies and impostors [32][58][67].

## Sources

1. Game Developer / AI and Games, "How Townscaper Works: A Story Four Games in the Making" — https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making
2. kai-denrei, oskar-procedure, "Dual grid and tiles" (community reconstruction) — https://github.com/kai-denrei/oskar-procedure/blob/main/docs/03-dual-grid-and-tiles.md
3. BorisTheBrave, Sylves "Townscaper Grid" tutorial — https://boristhebrave.com/docs/sylves/1/articles/tutorials/townscaper.html
4. Stålberg on X, "Fairly even irregular quads grid in a hex" (Jul 2019) — https://x.com/osksta/status/1147881669350891521
5. Stålberg on X, tile meshes on the dual grid — https://x.com/osksta/status/1459485788987613190
6. John Wigg, "Creating an organic grid on a sphere" — https://john-wigg.dev/SphereScaper/nodemo
7. Stålberg on X, irregular grid + multi-cell tiles, cage deformation (Oct 2022) — https://x.com/OskSta/status/1579483114178768898
8. Élie Michel TownBuilder (DeepWiki) — https://deepwiki.com/eliemichel/TownBuilder
9. EPC 2018, "Wave Function Collapse in Bad North" — https://www.youtube.com/watch?v=0bcZb-SsnrA
10. IndieCade Europe 2019, "Organic Towns from Square Tiles" — https://www.youtube.com/watch?v=1hqt8JkYRdI
11. SGC 2021, "Beyond Townscapers" — https://www.youtube.com/watch?v=Uxeo9c-PX-w
12. Konsoll 2021, "The Story of Townscaper" — https://konsoll.org/talks/the-story-of-townscaper/
13. Wikipedia, Tiny Glade — https://en.wikipedia.org/wiki/Tiny_Glade
14. 80 Level, "Tiny Glade Developers on Bevy, Proceduralism, Publishers & Cozy Games" — https://80.lv/articles/exclusive-tiny-glade-developers-discuss-bevy-proceduralism-publishers-cozy-games
15. 80 Level, "A Look at Procedural Tower Roofs in Tiny Glade" — https://80.lv/articles/a-look-at-procedural-tower-roofs-in-tiny-glade
16. 80 Level, "This Upcoming Game Lets You Procedurally Glue Buildings Together" — https://80.lv/articles/this-upcoming-game-lets-you-procedurally-glue-buildings-together
17. 80 Level, "Procedural Windows in Pounce Light's Tiny Glade Get an Upgrade" — https://80.lv/articles/procedural-windows-in-pounce-light-s-tiny-glade-get-an-upgrade
18. Steam discussion (player claim, brick limit) — https://steamcommunity.com/app/2198150/discussions/0/4845401462973097136
19. Manor Lords Official Wiki, Burgage plot — https://wiki.hoodedhorse.com/Manor_Lords/Burgage_plot/en
20. GamesRadar, Manor Lords level 2 burgage plot — https://www.gamesradar.com/manor-lords-burgage-plot-level-too-low/
21. Steam discussion, no grid system — https://steamcommunity.com/app/1363080/discussions/0/4355620138225704118
22. Gamer Guides, Burgage Plots Guide — https://www.gamerguides.com/manor-lords/guide/basics/buildings/burgage-plots-guide-levels-designs-extensions-and-more
23. Game8, List of Backyard Extensions — https://game8.co/games/Manor-Lords/archives/452930
24. PC Gamer, Manor Lords major overhaul (2025) — https://www.pcgamer.com/games/strategy/runaway-city-building-success-manor-lords-is-getting-a-major-overhaul-to-its-systems-its-not-just-a-new-feature-or-two-its-a-full-rework/
25. GDC Vault, "Building SimCity: Art in the Service of Simulation" — https://gdcvault.com/play/1017823/Building-SimCity-Art-in-the
26. Internet Archive, GDC2013Quigley — https://archive.org/details/GDC2013Quigley
27. Game Developer, "How Do You Put the Sim in SimCity?" — https://www.gamedeveloper.com/design/how-do-you-put-the-sim-in-i-simcity-i-
28. Parsimonious SimCity 2013 guide, zoning & density — http://www.parsimonious.org/simcity5/zoning-density.html
29. SimCity 2013 Wiki, Density — https://simcity2013wiki.com/wiki/Density
30. SimCity 2013 Wiki, Modules — https://simcity2013wiki.com/wiki/Modules
31. Wikipedia, SimCity (2013) — https://en.wikipedia.org/wiki/SimCity_(2013_video_game)
32. CS2 Wiki, Asset Pipeline: Buildings — https://cs2.paradoxwikis.com/index.php?mobileaction=toggle_view_desktop&title=Asset_Pipeline%3A_Buildings
33. Paradox, CS2 Feature Highlight #4: Zones & Signature Buildings — https://www.paradoxinteractive.com/games/cities-skylines-ii/features/zones-signature-buildings
34. Paradox, "Adding Custom Assets" (CS2) — https://www.paradoxinteractive.com/games/cities-skylines-ii/news/adding-custom-assets
35. Steam, CS2 UK Region Pack Developer Diary — https://steamcommunity.com/games/949230/announcements/detail/4486243201365051619
36. CS1 Wiki, Asset Editor — https://skylines.paradoxwikis.com/Asset_Editor
37. cslmodding.info, Building Asset Creation — https://cslmodding.info/asset/building/
38. Simtropolis, Colour Variations thread — https://community.simtropolis.com/forums/topic/69691-colour-variations-selection-and-asset-editor-question/
39. cslmodding.info (illumination map ranges) — https://cslmodding.info/asset/building/
40. Anno Union, "Of 3D Architects and Construction Workers" — https://www.anno-union.com/devblog-of-3d-architects-and-construction-workers/
41. Anno Union, "Residential Tiers" — https://www.anno-union.com/devblog-residential-tiers/
42. Anno Union, "The High Life" — https://www.anno-union.com/devblog-the-high-life/
43. Steam discussion, "Too Homogeneous?" — https://steamcommunity.com/app/916440/discussions/0/3203652426707223322/
44. Xbox Wire, Anno 117 Celtic or Roman — https://news.xbox.com/en-us/2025/11/12/anno-117-pax-romana-choices/
45. Anno Union, Echoes of Kassandra Cosmetic Pack — https://www.anno-union.com/devblog-the-echoes-of-kassandra-cosmetic-pack/
46. Foundation Wiki, Housing — https://wiki.polymorph.games/foundation/Housing
47. Polymorph, Devlog #17 — https://www.polymorph.games/foundation/news/2023/08/22/devlog-17/
48. Galaxus, Foundation review — https://www.galaxus.at/en/page/foundation-is-a-brilliantly-chaotic-and-creative-building-game-36583
49. Foundation modding docs, monuments — https://www.polymorph.games/foundation/modding/monuments
50. Foundation Wiki, Buildings — https://wiki.polymorph.games/foundation/Buildings
51. Citystate, "Citystate Metropolis — Development update and first screenshots" — https://www.citystategame.com/post/citystate-metropolis-development-update-and-first-screenshots
52. Steam, Citystate Metropolis / Citystate II — https://store.steampowered.com/app/2828020/Citystate_Metropolis/
53. W&R Official Wiki, Construction — https://wiki.hoodedhorse.com/Workers_Resources_Soviet_Republic/Construction
54. W&R Fandom, Construction — https://workers-resources.fandom.com/wiki/Construction
55. Nexus Mods, Sim Settlements 2 / Industrial Revolution — https://www.nexusmods.com/fallout4/mods/47976 , https://www.nexusmods.com/fallout4/mods/25213
56. Game World Observer, Islanders — https://gameworldobserver.com/2019/06/14/islanders
57. Dorfromantik Wiki, Tiles — https://dorfromantik.fandom.com/wiki/Tiles
58. SideFX/Vimeo, "Marvel's Spider-Man, meet Houdini" (Santiago, GDC 2019) — https://vimeo.com/326448655
59. GDC Vault, "Procedurally Crafting Manhattan for Marvel's Spider-Man" — https://www.gdcvault.com/play/1026415/Procedurally-Crafting-Manhattan-for-Marvel
60. GDC Vault, "Marvel's Spider-Man: A Technical Postmortem" — https://gdcvault.com/browse/gdc-19/play/1026496
61. Epic, City Sample Project documentation — https://dev.epicgames.com/documentation/unreal-engine/city-sample-project-unreal-engine-demonstration
62. CG Channel, free City Sample assets — https://www.cgchannel.com/2022/04/download-epic-games-free-city-sample-assets-for-ue5/
63. Unreal Engine, City Sample PCG update — https://www.unrealengine.com/learning/city-sample-gets-a-major-update-with-pcg-and-unreal-mcp-workflows
64. The Architect's Newspaper, Flight Simulator 2020 — https://www.archpaper.com/2020/10/flight-simulator-2020-provides-a-worldwide-playset-for-architects-and-urbanists/
65. TechCrunch, "Meet the startup that helped Microsoft build the world of Flight Simulator" — https://techcrunch.com/2020/08/17/meet-the-startup-that-helped-microsoft-build-the-world-of-flight-simulator
66. Krzysztof Olborski, Megabuilding / Cyberpunk 2077 — https://www.artstation.com/artwork/WmWPDQ
67. Mark Foreman, Megabuilding Appearances — https://www.artstation.com/artwork/rAdYwJ
68. MCV/Develop, Inside Rockstar North Part 4 — https://mcvuk.com/development-news/inside-rockstar-north-part-4-the-art/
69. Arkhitekton, Building the GTA V City Model — https://arkhitekton.net/2016/05/15/building-the-gta-v-city-model/ ; Techtroduce on GTA VI — https://www.techtroduce.com/gta-vi-every-room-hand-crafted
70. Ubisoft News, "Watch Dogs: Legion – The Tools That Built London" — https://news.ubisoft.com/en-us/article/4po3S9Pwp1YcgBmGPmQxAh/watch-dogs-legion-the-tools-that-built-london
71. Game Developer, "Skyrim's Modular Approach to Level Design" (Burgess transcript) — https://www.gamedeveloper.com/design/skyrim-s-modular-approach-to-level-design
72. SlideShare, "Modular Level Design for Skyrim" — https://www.slideshare.net/JoelBurgess/gdc2013-kit-buildingfinal
73. 80 Level, "Building Huge Open Worlds: Modularity, Kits & Art Fatigue" — https://80.lv/articles/building-huge-open-worlds-modularity-kits-art-fatigue
