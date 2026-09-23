# Procedural building generators: grammar engines, tools and shipped pipelines

Research input for Borough's building-appearance design. Every factual claim carries a source URL in brackets. Text marked **(inference)** is my reading of the sources, not something a source states.

Source keys used below:

| Key | Source |
|---|---|
| [CGA06] | Müller, Wonka, Haegler, Ulmer, Van Gool 2006, https://peterwonka.net/Publications/pdfs/2006.SG.Mueller.ProceduralModelingOfBuildings.final.pdf |
| [PM01] | Parish & Müller 2001, https://graphics.ethz.ch/Downloads/Publications/Papers/2001/p_Par01.pdf |
| [IA03] | Wonka, Wimmer, Sillion, Ribarsky 2003, https://www.cg.tuwien.ac.at/research/publications/2003/Wonka-2003-Ins/Wonka-2003-Ins-Paper.pdf |
| [PE11] | Kelly & Wonka 2011, https://peterwonka.net/Publications/pdfs/2011.TOG.Kelly.ProceduralExtrusions.TechreportVersion.final.pdf |
| [SM19] | Santiago, GDC 2019 slides, https://media.gdcvault.com/gdc2019/presentations/santiago_david_procedurally_crafting_manhattan.pdf |
| [VIT] | Vitruvio usage doc, https://github.com/Esri/vitruvio/blob/main/doc/usage.md |
| [PCG] | Unreal PCG shape grammar, https://dev.epicgames.com/documentation/unreal-engine/using-shape-grammar-with-pcg-in-unreal-engine |
| [CB] | Citybound devblog, https://aeplay.org/citybound-devblog/how-im-implementing-procedural-architecture |

---

## 1. The lineage in one paragraph

Parish & Müller (2001) generated one building per lot with a stochastic L-system and put façade detail in layered-grid procedural textures [PM01]. Wonka et al. (2003) replaced the L-system with a split grammar, where every rule subdivides a box-shaped scope into child scopes that stay inside the parent, and added a control grammar to keep choices coherent across a façade [IA03]. Müller et al. (2006) generalised this into CGA shape, which added mass modelling, component splits and occlusion queries [CGA06]. CGA became the language of Esri CityEngine, and the same split/repeat idea now appears in Houdini's Labs Building Generator, Unreal's PCG grammar nodes and several open-source projects. Kelly & Wonka (2011) took a separate branch. Their procedural extrusions generate mass and roof together from a footprint plus per-edge profiles, using a generalisation of the straight skeleton [PE11].

---

## 2. CGA shape and CityEngine

### 2.1 Data model
- A **shape** has a symbol, geometry and a **scope**, which is an oriented bounding box (position, three axes, size) [CGA06].
- The axiom is a building lot. Mass models are unions of volumetric primitives, typically L, H, U or T shapes [CGA06].
- GIS masses that arrive without a grammar fall back to an extruded footprint with a straight-skeleton roof [CGA06].
- CityEngine rule files expose `attr` parameters. A model is created by choosing an `@StartRule` on an initial shape, which is usually a footprint or lot polygon [https://doc.arcgis.com/en/cityengine/latest/help/help-working-with-rules.htm], [https://www.e-education.psu.edu/geogvr/node/892].

### 2.2 Operations
- `split` cuts a scope along one axis. Sizes can be absolute, relative (`'`) or floating (`~`), and `*` repeats a pattern to fill the extent [https://doc.arcgis.com/en/cityengine/2023.1/cga/cga-split.htm].
- `comp` breaks a shape into faces, edges or vertices. Its selectors cover scope directions, object and world directions, street-facing sides, and roof-edge classes (eave, hip, valley, ridge). Its operators are `:`, `=` and `=:` [https://doc.arcgis.com/en/cityengine/latest/cga/cga-comp.htm].
- For an irregular face, `comp` sets the child's scope to the face's bounding box and generates trim planes at bisecting angles, so repeated elements are cut cleanly at corners [https://doc.arcgis.com/en/cityengine/latest/cga/cga-comp.htm].
- `setback` with `street.front` needs a `streetWidth` attribute, and CityEngine sets it automatically on dynamic lots [https://doc.arcgis.com/en/cityengine/2021.0/cga/cga-setback.htm], [https://doc.arcgis.com/en/cityengine/latest/tutorials/tutorial-8-mass-modeling.htm].
- Irregular lots are a known pain point. The common workaround is `innerRectangle`, which extracts the largest rectangle for the grammar to work on [https://community.esri.com/t5/arcgis-cityengine-questions/cga-strategies-for-dealing-with-irregular-shaped/td-p/1686741].
- `roofHip`, `roofGable` and `roofShed` build roofs from the footprint [https://doc.arcgis.com/en/cityengine/latest/cga/cga-roof-hip.htm]. CityEngine's roof code is built on CGAL's straight skeleton [https://geometryfactory.com/portfolio/roofs-and-the-straight-skeleton/].

### 2.3 Context: occlusion and snapping
- The 2006 paper adds occlusion queries that return none, part or full, and they exclude the shape's own ancestors [CGA06].
- The paper also proposes cheaper variants that test the scope instead of the shape, and sightline tests such as `Shape.visible("street")`. It uses an octree for acceleration [CGA06].
- Snap lines let one split align to features elsewhere in the building, such as floor lines on adjacent wings. The nearest split is modified to meet the snap line [CGA06].
- Modern CityEngine has `inside`, `overlaps` and `touches`. They can target the same model only (intra), other models (inter) or both, and they can filter by label. They only test against closed geometry [https://doc.arcgis.com/en/cityengine/latest/cga/cga-inside-function.htm], [https://doc.arcgis.com/en/cityengine/2023.0/cga/cga-context-queries.htm].
- Esri warns that occlusion queries are expensive [https://support.esri.com/en-us/knowledge-base/what-are-occlusion-queries-1462481760323-000011364].

### 2.4 Authoring and geometry
- Technical artists write the rules as CGA text. Detailed pieces such as window frames, cornices and ornaments are terminal assets modelled in a DCC tool. In the 2006 paper those pieces came from Maya [CGA06].
- Pompeii used 190 rules and 36 terminal objects [CGA06]. Beverly Hills used about 150 rules for about 1000 buildings [CGA06].
- Output is a mixture of generated mesh (masses, split faces, roofs) and inserted terminal meshes [CGA06].
- Datasmith export to Unreal can merge meshes per initial shape, globally or by material, and it can emit instances instead of merged meshes [https://doc.arcgis.com/en/cityengine/latest/help/help-export-unreal.htm].

### 2.5 Determinism and seeding
- Each shape has a `seedian` attribute in the range 0 to 714024. Random functions in the rules draw from it, so the same seed and attributes reproduce the same model [https://doc.arcgis.com/en/cityengine/latest/cga/cga-seedian-attribute.htm].
- CityEngine for Houdini reads the seed from a primitive attribute [https://github.com/Esri/cityengine_for_houdini].

### 2.6 Incremental regeneration
- The unit of regeneration is the initial shape. Changing an attribute regenerates that one model [VIT].
- Vitruvio, Esri's CityEngine plugin for Unreal, generates asynchronously and regenerates a model on every attribute change. It also has a batch actor that groups models on a grid. Any change inside a batch regenerates the whole batch [VIT].
- Inter-model occlusion is opt-in in Vitruvio [VIT]. **(Inference)** The reason is probably that inter-model queries make one model's output depend on its neighbours, which widens the regeneration set.

### 2.7 LOD
- CGA has no automatic LOD. Authors add an `attr LOD` and branch on it inside the rules [https://doc.arcgis.com/en/cityengine/latest/tutorials/tutorial-6-basic-shape-grammar.htm], [https://doc.arcgis.com/en/cityengine/2024.1/help/help-model-export-application-notes.htm].
- Pompeii came to about 1.4 billion polygons at high LoD, 31 million at middle LoD and 170 thousand at low LoD. The levels were made by swapping terminal objects by hand [CGA06].

### 2.8 Performance
- A 50k-polygon building took about 1 s to compute and 0.5 s to write, on 2006 hardware [CGA06].
- Beverly Hills came to about 700 million polygons. A billion-polygon city generated in under a day and was rendered with RenderMan instancing [CGA06].

### 2.9 Runtime use and licensing
- The Procedural Runtime (PRT) SDK executes compiled rule packages (RPKs). It is free for non-commercial use, and redistribution needs Esri's permission [https://esri.github.io/cityengine/cityenginesdk], [https://github.com/Esri/vitruvio].
- Vitruvio can run in the editor or in a packaged game. Its inputs are an RPK, an initial shape, attribute values and a seed. RPKs are limited to 2 GB [VIT].

### 2.10 Limitations
- Detail still has to be modelled outside the grammar [CGA06].
- Output on arbitrary GIS footprints can be implausible, because the rules assume box-like masses [CGA06]. The `innerRectangle` workaround shows the problem persists [https://community.esri.com/t5/arcgis-cityengine-questions/cga-strategies-for-dealing-with-irregular-shaped/td-p/1686741].
- There is no Godot integration and the runtime licence restricts commercial redistribution [https://esri.github.io/cityengine/cityenginesdk].

---

## 3. Parish & Müller 2001 (CityEngine's ancestor)

- **Lots.** Blocks are subdivided recursively into lots, which must be convex. Lots with no street access are discarded [PM01].
- **Buildings.** One building per lot is generated by a stochastic parametric L-system. There are three rule sets (commercial, residential, skyscraper), chosen from a zoning map [PM01].
- **Mass.** The axiom is the lot's bounding box. The L-system transforms, extrudes and branches it [PM01].
- **LOD.** The output of each L-system iteration is a coarser version of the final building, so iterations double as LOD levels [PM01].
- **Façades.** Façade detail lives in procedural textures built from layered grids of interval groups, not in geometry [PM01].
- **(Inference)** The layered-grid façade is the closest published relative of Borough's current shader, which runs a small split/repeat on instanced boxes. The 2001 paper shows the approach was chosen for scale, and CGA moved detail into geometry once offline rendering could afford it.

---

## 4. Split grammars and control grammars ("Instant Architecture", 2003)

### 4.1 Data model
- The split grammar operates on basic shapes with a scope. Every split keeps each child inside its parent, which prevents shapes growing into each other [IA03].
- Shapes carry attributes. Input is a set of ArcView footprints with per-building attributes [IA03].

### 4.2 Control grammar
- The control grammar is an attributed context-free grammar that runs alongside the split grammar. It distributes design ideas across the building, such as "the ground floor is a shop row" [IA03].
- Its terminals are tuples ⟨c, a, v⟩. `c` is a spatial locator such as a row, a column, first or last, or all. `a` is an attribute and `v` a value [IA03].
- Each shape stores a control-grammar start symbol. The control grammar is invoked at each split, and the parent's control grammar usually sets the children's start symbols [IA03].

### 4.3 Rule selection and determinism
- Selection has two stages [IA03].
  - A deterministic stage filters rules whose attribute intervals overlap or contain the shape's attributes (the paper calls this matching MDV). Priority flags break ties.
  - A stochastic stage picks among the survivors (MSV).
- One random value per rule is precalculated once per building, so the same rule makes the same choice everywhere in that building. This is how the paper keeps windows consistent across a façade [IA03].
- **(Inference)** The paper's consistency is within one building. It offers no mechanism for coherence across a street or neighbourhood. A street-level style would need to be passed in as input attributes.

### 4.4 Scale and performance
- The authors wrote about 250 rules, 40 attributes and 10 basic shapes in about two weeks. They estimate a production system would need 2000 to 3000 rules [IA03].
- A building is 1k to 100k polygons and takes 1 to 3 s on a 2 GHz Pentium 4 [IA03].

### 4.5 Limitations
- Complex detail has to be supplied as terminal shapes [IA03].
- The authors say rule editing is not trivial [IA03].

---

## 5. Houdini

### 5.1 Labs Building Generator
- **Input.** Artists supply low-poly blockout volumes. The tool slices them into floors, finds walls, corners and ledges, and places modules on them. A seed parameter controls variation [https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_generator-4.0.html].
- **Modules.** Modules are authored in the YZ plane and identified by an `@name` attribute [https://www.sidefx.com/tutorials/building-generator/].
- **Pattern syntax.** The Utility node defines it [https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_generator_utility-2.0.html].
  - `<A-B>` fills the span with a repeating pattern.
  - `[A]3` places a rigid block.
  - `*` introduces weighted variations.
  - Priority rules resolve conflicts, and hand-placed or volumetric overrides replace modules in chosen regions.
- **Floor patterns.** Building from Patterns assigns patterns per floor and accepts a `floor_pattern` override attribute [https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_from_patterns-1.1.html].
- **Roofs.** The docs do not describe roof generation. I found no statement either way.
- **Straight skeletons.** Houdini ships PolyExpand2D and a Labs Straight Skeleton 2D node [https://www.sidefx.com/docs/houdini/nodes/sop/polyexpand2d.html], [https://www.sidefx.com/docs/houdini/nodes/sop/labs--straight_skeleton_2D.html]. EPFL's Procedural Venice project built its roofs on them [https://fdh.epfl.ch/index.php/Procedural_Venice].

### 5.2 Houdini Engine and runtime
- Houdini Engine runs digital assets (HDAs) inside Unreal and Unity editors. The output must be baked to ordinary assets before packaging, and there is no runtime cooking, for licensing and technical reasons [https://www.sidefx.com/docs/houdini/unreal/packaging.html], [https://www.sidefx.com/forum/topic/61747/], [https://github.com/sideeffects/HoudiniEngineForUnreal-v2].
- **(Inference)** No shipped game in this survey generates buildings with Houdini at runtime. Every Houdini case below is an offline authoring pipeline.

### 5.3 Marvel's Spider-Man (Insomniac, 2018)
- **Scale.** 6 × 3 km of Manhattan, more than 8,300 buildings, more than 3,250 edifice prefabs and more than 350 storefronts [SM19].
- **Pipeline.** Curves define streets and blocks. Primitive buildings are "modularized with instanced architecture" [SM19].
- **Flow.** Changes flow downstream. Procedural output is never hand-edited, and in polish the systems regenerate from recorded "tweaks" [SM19].
- **Cascade.** A small upstream change can ripple into UV continuity around a block, prop placement and crime placement [SM19].
- **Lesson.** The talk advises locking elements with many dependents early [SM19]. The SIGGRAPH record is at https://history.siggraph.org/learning/procedural-system-assisted-authoring-of-open-world-content-for-marvels-spider-man-by-santiago/.

### 5.4 Ghost Recon Wildlands (Ubisoft, 2017)
- A village tool first made main streets, then grew grid roads and rows of buildings [https://80.lv/articles/procedural-world-building-in-ghost-recon-wildlands].
- Four Houdini artists built the tools. Content was locked before manual edits, and the team says there were "no magic seeds", meaning outputs were curated, not trusted blindly [https://80.lv/articles/procedural-world-building-in-ghost-recon-wildlands].

### 5.5 Far Cry 5 (Ubisoft, 2018)
- Houdini Engine ran inside the Dunia editor. The unit of work was a 64 × 64 m sector, and the whole map was regenerated nightly [https://80.lv/articles/houdini-procedural-world-generation-of-far-cry-5], [https://christianjmills.com/posts/procedural-tools-far-cry-5-notes/].
- The system covered terrain, biomes and vegetation. It did not generate buildings [https://christianjmills.com/posts/procedural-tools-far-cry-5-notes/].

### 5.6 Other Ubisoft and CD Projekt titles
- **Watch Dogs: Legion.** I found no procedural building generator. London was built from kits of more than 250 modules assembled by level artists [https://www.artstation.com/artwork/48kakl], [https://news.ubisoft.com/en-us/article/4po3S9Pwp1YcgBmGPmQxAh/watch-dogs-legion-the-tools-that-built-london].
- **Assassin's Creed Unity.** One weak secondary source claims AnvilNext 2.0 had rule-based building generation [https://www.mycplus.com/game-development/game-engines/ubisoft-anvil-game-engine/]. I could not verify it.
- **Cyberpunk 2077.** I found nothing documented about a building generator.

---

## 6. Blender generators

### 6.1 Buildify
- A free geometry-nodes library. Buildings are generated from the faces of a base mesh [https://www.cgchannel.com/2022/07/download-free-blender-3d-building-generator-buildify/].
- Each kit uses one module size, and modules are stretched to fit the wall [https://80.lv/articles/grab-a-free-geometry-nodes-library-for-procedural-buildings-in-blender].
- Pillars cover flat, concave and convex corners. Roofs are flat only, with props placed by recursive subdivision [https://80.lv/articles/grab-a-free-geometry-nodes-library-for-procedural-buildings-in-blender].

### 6.2 Building Tools
- An MIT-licensed add-on that generates mesh directly with `bmesh` operators, such as face-region extrusion, and assigns material groups [https://github.com/ranjian0/building_tools].
- It covers floorplans, floors, windows, doors, roofs, balconies and stairs. It is an interactive modelling aid, not a batch city generator [https://github.com/ranjian0/building_tools].

### 6.3 Blosm and PML
- PML is a style language for Blosm, a Blender add-on that builds cities from OpenStreetMap. It has an ANTLR grammar and is translated to Python [https://github.com/prochitecture/pml].
- Blosm classifies each façade as front, side or back by its visibility from streets, and styles can target those classes [https://github.com/prochitecture/blosm/discussions/1].
- I could only read PML at README level, because the example files returned 404.

### 6.4 Relevance to Borough
- **(Inference)** Blender generators are authoring tools, not runtimes. For Borough, Blender is best used to author the terminal modules (window bays, corners, cornices, roof pieces) that a runtime rule system places. Geometry nodes cannot run inside a Godot build.

---

## 7. Procedural extrusions and straight-skeleton roofs

### 7.1 Kelly & Wonka 2011
- **Inputs.** A plan made of polygons (holes allowed, clockwise orientation), a monotonic profile per edge, and anchors [PE11].
- **Anchors.** An anchor pins a feature, such as a window or chimney, to a profile position, either relative or absolute. Anchors solve the persistence problem, because features stay attached when the plan or profile changes [PE11].
- **Algorithm.** A sweep plane rises through the building, which is a generalisation of the straight skeleton. Two event types occur. Edges collide, as in a straight skeleton. Profiles also change direction at "natural steps" and offset events [PE11].
- **Numerics.** The implementation relies on floating-point heuristics with clustering tolerances δ1 = 1e-4 and δ2 = 1e-6 [PE11].
- **Styling.** Edges are labelled street, side or back, and profiles are assigned by building type and probability [PE11].
- **Scale.** An Atlanta test used 6000 footprints, 3 million polygons and 5 styles. Modelling took 20 minutes, extrusion 10 minutes and rendering 15 minutes [PE11].
- **Robustness.** Two roofs out of 6000 were wrong. Interactive editing lost a face about once every 5 minutes. The authors say the method is hard to implement and has no formal correctness guarantee [PE11].
- It was implemented in Java [PE11].

### 7.2 Straight skeletons in practice
- CityEngine uses CGAL's straight skeleton for its roofs [https://geometryfactory.com/portfolio/roofs-and-the-straight-skeleton/].
- Houdini ships its own [https://www.sidefx.com/docs/houdini/nodes/sop/labs--straight_skeleton_2D.html].
- Holes are a recurring failure. The open-source Stratum project rejects footprints with holes or courtyards for most roof types [https://github.com/haptixxx-dev/Stratum/issues/42].
- **(Inference)** Borough's courtyard Buildings, with four wings around a hole, are exactly the case most roof implementations handle worst. A robust route is to roof each wing as its own rectangle-like strip and treat the courtyard corners as an explicit join case, rather than feeding a holed polygon to a general skeleton. This also avoids floating-point sensitivity, which matters because appearance must be a pure function of snapshot and id.

---

## 8. Other open-source and engine generators

### 8.1 Unreal PCG shape grammar (5.5 and later)
- **Nodes.** Subdivide Segment, Subdivide Spline, Duplicate Cross-Section and Select Grammar [PCG].
- **Syntax.** `*` repeats, `+` repeats at least once, `[A,B]2` repeats a group a fixed number of times, `<>` gives a priority fallback list, and `{[A,P]:2,...}` gives a weighted random choice [PCG].
- **Seeding.** Every node takes a Seed or a Seed Attribute, so each building can be seeded from its own data [PCG].
- **Buildings.** A common pattern extrudes a footprint with Duplicate Cross-Section using a vertical grammar such as "Ground, Inter*, Roof", then subdivides each wall segment horizontally [https://www.docswell.com/s/EpicGamesJapan/KX6MX7-UEMeetupHiroshima03].
- **Runtime.** PCG can generate at runtime and hierarchically by grid cell, with a generation radius and scheduling policies [https://dev.epicgames.com/documentation/unreal-engine/using-pcg-generation-modes-in-unreal-engine].
- **Seams.** Seams can appear at cell boundaries when content depends on neighbouring cells [https://adventuresincausality.com/unreal/unreal-world-partition/pcg-integration/].
- **(Inference)** PCG is the closest mainstream analogue to what Borough needs. It is a 1D grammar over edges and cross-sections, seeded per element, run at runtime, and emitting instances of authored meshes.

### 8.2 Citybound
- Architecture is described as JSON rule "recipes". The hierarchy runs from building to lot and corpora, then to fundament, floors and roof. Each floor has front, left, back and right façades, with subdivision and decoration rules [CB].
- Variables can be ranges. Orientation comes from the lot's longest road edge [CB].
- If a style's constraints fail on a lot, the lot stays empty and another style is tried [CB].
- It supports gable and hip roofs [CB].

### 8.3 Subversion (Introversion)
- A city generator based on Parish & Müller. It was never released [http://pcg.wikidot.com/pcg-games:subversion], [https://news.ycombinator.com/item?id=16765632].

### 8.4 ShapeML
- A GPL-3.0 C++ reimplementation of a CGA-like language. It has conditional LOD rules and probabilistic rules, and no CSG [https://github.com/stefalie/shapeml].

### 8.5 Small projects
- **ProceduralToolkit.** MIT-licensed, for Unity. Its building generator uses pluggable façade and roof strategies [https://github.com/Syomus/ProceduralToolkit].
- **ShapeGrammarLanguage.** A Unity shape-grammar experiment [https://github.com/cosmicpotato137/ShapeGrammarLanguage].
- **cga-shape.** A small CGA reimplementation [https://github.com/LudwikJaniuk/cga-shape].
- **Godot.** I found no Godot CGA plugin.

---

## 9. Surveys

- **Smelik et al. 2014.** Surveys procedural modelling for virtual worlds with a focus on control and interactivity. It finds that artists rarely adopt procedural tools [https://onlinelibrary.wiley.com/doi/10.1111/cgf.12276]. I did not read its building section in full.
- **Kelly & McCabe 2006.** An earlier survey of city-generation techniques [https://arrow.tudublin.ie/itbj/vol7/iss2/5/].
- **Vanegas et al. 2010.** Surveys urban modelling and rendering. Two findings bear on Borough [https://www.cs.purdue.edu/cgvlab/www/resources/papers/Vanegas-CGF-2010-Modelling_the_Appearance_and_Behaviour_of_Urban_Spaces.pdf].
  - High-quality automatic simplification of urban geometry is unsolved. The practical alternatives are multiple procedural or hand-made versions of each building, or image-based impostors.
  - Procedural models help LOD because they carry semantics, such as which geometry belongs to which façade. The survey also notes that occlusion culling does little when the camera is high above the city.

---

## 10. Comparison

| System | Data model | Authoring | Geometry | Seeding | Regen unit | LOD | Runtime? | Main limit |
|---|---|---|---|---|---|---|---|---|
| Parish & Müller 2001 | Lot box, L-system | Rule sets per zone | Generated mass, textured façades | Stochastic L-system | Lot | Iteration depth | No (research) | Convex lots, box-like masses |
| Instant Architecture 2003 | Scoped shapes + attributes | Split + control grammar text | Generated + terminal shapes | One random value per rule per building | Building | Not addressed | No | Rule authoring cost |
| CGA / CityEngine | Scope, mass union, comp | CGA text + DCC terminals | Generated + inserted terminals | `seedian` per shape | Initial shape | Manual `attr LOD` branch | PRT/Vitruvio, restricted licence | Irregular lots, cost of context queries |
| Houdini Labs BG | Blockouts, modules by name | Houdini graph + modules | Instanced modules | Seed parameter | Asset cook | Not documented | No (bake) | Editor-only |
| Spider-Man | Curves, blocks, prefabs | Houdini + tweak lists | Instanced architecture | Not stated | Downstream cascade | Not in slides | No (bake) | Cascades on upstream change |
| Buildify | Base-mesh faces | Geometry nodes | Stretched modules | Node seed | Whole object | None | No | Flat roofs only |
| Building Tools | Interactive mesh | bmesh operators | Generated mesh | n/a | Operation | None | No | Manual tool |
| Procedural extrusions | Plan + profiles + anchors | Profile editor | Generated mesh | Probabilistic profile choice | Building | Not addressed | No | Float robustness, hard to implement |
| Unreal PCG grammar | Points, splines, segments | PCG graph + grammar string | Instanced meshes | Seed or seed attribute per node | Component or grid cell | Via hierarchical cells | Yes | Cell seams |
| Citybound | Lot, corpora, floors, façades | JSON recipes | Generated | Not documented | Lot | Not documented | Yes (in-game) | Unfinished |
| ShapeML | CGA-like | Text | Generated | Probabilistic rules | Model | Conditional LOD rules | Library | GPL, no CSG |

---

## 11. Design choices these systems force

### 11.1 Grammar, graph or module placement
- Three families exist.
  - **Text grammars** (CGA, ShapeML, Instant Architecture) express hierarchy and repetition compactly, but writing them is programming [IA03], [CGA06].
  - **Node graphs** (Houdini, geometry nodes, PCG) are visual and debuggable per step, but a graph is harder to diff and version than a text rule [https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_generator-4.0.html], [PCG].
  - **Module placement with pattern strings** (Labs Building Generator, PCG grammar nodes) keeps only the 1D split/repeat part of a grammar and delegates everything else to authored modules [https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_generator_utility-2.0.html], [PCG].
- **(Inference)** Borough's shader already implements the third family in miniature. The industry trend since 2006 is toward that family, with grammars restricted to 1D patterns along an edge and up a cross-section.

### 11.2 Authored parts or generated geometry
- Every production system relies on authored terminal parts for detail [CGA06], [IA03], [SM19].
- Generated geometry is used for masses, wall faces and roofs [CGA06], [PE11].
- **(Inference)** For Borough this means Blender authors the terminals and the shell's rule system only decides where they go. That matches the project's existing Blender-first rule for Building assets.

### 11.3 Offline bake or runtime generation
- Houdini-based pipelines bake [https://www.sidefx.com/docs/houdini/unreal/packaging.html]. Spider-Man, Wildlands and Far Cry 5 all regenerated offline and shipped static content [SM19], [https://80.lv/articles/procedural-world-building-in-ghost-recon-wildlands], [https://80.lv/articles/houdini-procedural-world-generation-of-far-cry-5].
- Runtime generation exists in Vitruvio, Unreal PCG and Citybound [VIT], [PCG], [CB].
- **(Inference)** Borough's Buildings change during play, so it needs runtime generation. The shipped offline pipelines are evidence about authoring practice, not about runtime architecture.

### 11.4 Per-building or per-block context
- Most systems generate each building from its own inputs. Context appears as street-facing selectors and street-width attributes [https://doc.arcgis.com/en/cityengine/latest/cga/cga-comp.htm], [https://doc.arcgis.com/en/cityengine/2021.0/cga/cga-setback.htm], [https://github.com/prochitecture/blosm/discussions/1], [PE11], [CB].
- True neighbour context comes from occlusion queries, which are expensive, restricted to closed geometry and opt-in at runtime [https://support.esri.com/en-us/knowledge-base/what-are-occlusion-queries-1462481760323-000011364], [VIT].
- Spider-Man shows the cost of cross-building dependencies, because a change in one place cascades through a block [SM19].
- **(Inference)** For Borough, the safest contract is that appearance depends only on the Building's own snapshot, its stable id, and a small set of precomputed edge labels (street-facing, party wall, courtyard). The simulation already owns frontage, so it can supply those labels without the shell querying neighbours. Party-wall suppression of windows is the one neighbour fact worth carrying, and it fits in an edge label.

### 11.5 Seeding
- Seeds are always per building or per element [https://doc.arcgis.com/en/cityengine/latest/cga/cga-seedian-attribute.htm], [PCG].
- Instant Architecture fixes one random value per rule per building so that repeated choices agree [IA03].
- **(Inference)** Borough should derive each rule's random value from a hash of the Building id and a rule tag. This mirrors the simulation's counter-hash convention, and it keeps a façade stable when unrelated parts of the Building change.

### 11.6 Regeneration granularity
- Regeneration happens per initial shape in CGA, per batch in Vitruvio, per grid cell in PCG and per sector in Far Cry 5 [VIT], [https://dev.epicgames.com/documentation/unreal-engine/using-pcg-generation-modes-in-unreal-engine], [https://christianjmills.com/posts/procedural-tools-far-cry-5-notes/].
- Anchors in procedural extrusions keep features attached across edits [PE11].
- **(Inference)** Per-building regeneration is the natural unit for Borough. Batching for draw calls should be a separate layer that can rebuild one Building's instances without regenerating its neighbours. Vitruvio's whole-batch regeneration is the pattern to avoid.

### 11.7 LOD strategy
- No system derives LOD automatically. CGA branches on an LOD attribute, Pompeii swapped terminals by hand, and ShapeML has conditional LOD rules [https://doc.arcgis.com/en/cityengine/latest/tutorials/tutorial-6-basic-shape-grammar.htm], [CGA06], [https://github.com/stefalie/shapeml].
- Parish & Müller use grammar depth as LOD [PM01].
- Vanegas et al. say automatic simplification of urban models is unsolved, and they recommend multiple procedural versions or impostors [https://www.cs.purdue.edu/cgvlab/www/resources/papers/Vanegas-CGF-2010-Modelling_the_Appearance_and_Behaviour_of_Urban_Spaces.pdf].
- **(Inference)** Borough's current shader-on-box is already a good far LOD. That is the layered-grid texture approach of 2001. Near LOD can stop the grammar at a shallower depth or swap terminals, and the split structure should be the same at every level so windows do not jump during transitions.

### 11.8 Roof robustness
- General straight skeletons fail on holes and degenerate input, and procedural extrusions depend on floating-point tolerances [https://github.com/haptixxx-dev/Stratum/issues/42], [PE11].
- CityEngine relies on CGAL, a large exact-arithmetic library [https://geometryfactory.com/portfolio/roofs-and-the-straight-skeleton/].
- **(Inference)** Borough should restrict roof forms to what it can compute robustly on its own footprints, such as flat, per-wing gable or hip, and mansard as an inset. Courtyard corners should get an explicit join rule. A general straight skeleton should come in only if a footprint class demands it.

---

## 12. Gaps in this research

- I found no documented building generator for Watch Dogs: Legion, Assassin's Creed Unity or Cyberpunk 2077. The Assassin's Creed claim rests on one unverified source.
- The Labs Building Generator docs say nothing about roofs.
- I did not read the Smelik survey's building section in full.
- I read PML only at README level.
- Performance figures for CityEngine and Instant Architecture come from 2003–2006 hardware.

---

## Sources

- https://peterwonka.net/Publications/pdfs/2006.SG.Mueller.ProceduralModelingOfBuildings.final.pdf
- https://graphics.ethz.ch/Downloads/Publications/Papers/2001/p_Par01.pdf
- https://www.cg.tuwien.ac.at/research/publications/2003/Wonka-2003-Ins/Wonka-2003-Ins-Paper.pdf
- https://peterwonka.net/Publications/pdfs/2011.TOG.Kelly.ProceduralExtrusions.TechreportVersion.final.pdf
- https://doc.arcgis.com/en/cityengine/latest/cga/cga-comp.htm
- https://doc.arcgis.com/en/cityengine/2023.1/cga/cga-split.htm
- https://doc.arcgis.com/en/cityengine/latest/cga/cga-inside-function.htm
- https://doc.arcgis.com/en/cityengine/2023.0/cga/cga-context-queries.htm
- https://support.esri.com/en-us/knowledge-base/what-are-occlusion-queries-1462481760323-000011364
- https://doc.arcgis.com/en/cityengine/latest/cga/cga-seedian-attribute.htm
- https://doc.arcgis.com/en/cityengine/latest/help/help-working-with-rules.htm
- https://www.e-education.psu.edu/geogvr/node/892
- https://doc.arcgis.com/en/cityengine/2021.0/cga/cga-setback.htm
- https://doc.arcgis.com/en/cityengine/latest/tutorials/tutorial-8-mass-modeling.htm
- https://community.esri.com/t5/arcgis-cityengine-questions/cga-strategies-for-dealing-with-irregular-shaped/td-p/1686741
- https://doc.arcgis.com/en/cityengine/latest/tutorials/tutorial-6-basic-shape-grammar.htm
- https://doc.arcgis.com/en/cityengine/2024.1/help/help-model-export-application-notes.htm
- https://doc.arcgis.com/en/cityengine/latest/cga/cga-roof-hip.htm
- https://geometryfactory.com/portfolio/roofs-and-the-straight-skeleton/
- https://doc.arcgis.com/en/cityengine/latest/help/help-export-unreal.htm
- https://esri.github.io/cityengine/cityenginesdk
- https://github.com/Esri/vitruvio
- https://github.com/Esri/vitruvio/blob/main/doc/usage.md
- https://github.com/Esri/cityengine_for_houdini
- https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_generator-4.0.html
- https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_generator_utility-2.0.html
- https://www.sidefx.com/docs/houdini/nodes/sop/labs--building_from_patterns-1.1.html
- https://www.sidefx.com/tutorials/building-generator/
- https://www.sidefx.com/docs/houdini/unreal/packaging.html
- https://www.sidefx.com/forum/topic/61747/
- https://github.com/sideeffects/HoudiniEngineForUnreal-v2
- https://www.sidefx.com/docs/houdini/nodes/sop/polyexpand2d.html
- https://www.sidefx.com/docs/houdini/nodes/sop/labs--straight_skeleton_2D.html
- https://fdh.epfl.ch/index.php/Procedural_Venice
- https://media.gdcvault.com/gdc2019/presentations/santiago_david_procedurally_crafting_manhattan.pdf
- https://history.siggraph.org/learning/procedural-system-assisted-authoring-of-open-world-content-for-marvels-spider-man-by-santiago/
- https://80.lv/articles/procedural-world-building-in-ghost-recon-wildlands
- https://80.lv/articles/houdini-procedural-world-generation-of-far-cry-5
- https://christianjmills.com/posts/procedural-tools-far-cry-5-notes/
- https://www.artstation.com/artwork/48kakl
- https://news.ubisoft.com/en-us/article/4po3S9Pwp1YcgBmGPmQxAh/watch-dogs-legion-the-tools-that-built-london
- https://www.mycplus.com/game-development/game-engines/ubisoft-anvil-game-engine/
- https://www.cgchannel.com/2022/07/download-free-blender-3d-building-generator-buildify/
- https://80.lv/articles/grab-a-free-geometry-nodes-library-for-procedural-buildings-in-blender
- https://github.com/ranjian0/building_tools
- https://github.com/prochitecture/pml
- https://github.com/prochitecture/blosm/discussions/1
- https://github.com/haptixxx-dev/Stratum/issues/42
- https://dev.epicgames.com/documentation/unreal-engine/using-shape-grammar-with-pcg-in-unreal-engine
- https://www.docswell.com/s/EpicGamesJapan/KX6MX7-UEMeetupHiroshima03
- https://dev.epicgames.com/documentation/unreal-engine/using-pcg-generation-modes-in-unreal-engine
- https://adventuresincausality.com/unreal/unreal-world-partition/pcg-integration/
- https://aeplay.org/citybound-devblog/how-im-implementing-procedural-architecture
- http://pcg.wikidot.com/pcg-games:subversion
- https://news.ycombinator.com/item?id=16765632
- https://github.com/stefalie/shapeml
- https://github.com/Syomus/ProceduralToolkit
- https://github.com/cosmicpotato137/ShapeGrammarLanguage
- https://github.com/LudwikJaniuk/cga-shape
- https://onlinelibrary.wiley.com/doi/10.1111/cgf.12276
- https://arrow.tudublin.ie/itbj/vol7/iss2/5/
- https://www.cs.purdue.edu/cgvlab/www/resources/papers/Vanegas-CGF-2010-Modelling_the_Appearance_and_Behaviour_of_Urban_Spaces.pdf
