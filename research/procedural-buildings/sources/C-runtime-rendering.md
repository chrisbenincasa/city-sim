# C. Runtime and rendering side of procedural buildings (Godot 4.7, C#)

Scope: how generated buildings reach the GPU in Godot 4, how they are LOD'd at city scale, how they are regenerated incrementally and kept stable, how module-selection solvers behave at runtime, and how a Blender→glTF→Godot kit should be authored. Every factual claim carries a source number (see §9). Statements marked **[inference]** are my reasoning, not something I read.

---

## 1. Output strategies and their costs in Godot 4

### 1.1 MultiMesh of authored modules

**What the engine does**

- A MultiMesh is "a single draw primitive that can draw up to millions of objects in one go" [1].
- It culls as one unit. There is "no *screen* or *frustum* culling possible for individual instances", so instances are "*always* or *never* drawn"; the docs' workaround is "several MultiMeshes for different areas of the world" [1]. The class reference adds that instances "are spatially indexed as one, for the whole object" [2].
- Mesh LOD is also chosen once per MultiMesh. "All instances will be drawn with the same LOD level at a given time", based on the AABB point nearest the camera; far-apart instances "should be placed in a separate MultiMeshInstance3D node" [3]. Visibility ranges likewise measure from "the center of the instance's AABB", which for a MultiMesh is the whole batch [4].
- The whole MultiMesh counts as one object for lighting; once the per-object light limit is used up, further instances "will **not** receive any lighting" [2]. **[inference]** This matters for night scenes with many street/window lights hitting a large chunk batch; Forward+ clustered lighting has its own 512-element view cap [21].
- Blend shapes are ignored in a MultiMesh [2].

**Per-instance data** (from the RenderingServer reference and the RD storage source):

| Field | Floats (3D) | Notes |
|---|---|---|
| Transform | 12 | 3×4 |
| Colour (optional) | 4 | `use_colors`; set before `instance_count` |
| Custom data (optional) | 4 | `INSTANCE_CUSTOM` in shader |
| Max stride | 20 floats = 80 bytes | [19][20] |

- Components are 32-bit in Forward+ and Mobile, packed to 16-bit in Compatibility [2].
- `use_colors` / `use_custom_data` can only be changed when `instance_count` is 0 [2]. Allocate the maximum and lower `visible_instance_count` [1][2].
- Per-instance shader uniforms (`instance uniform`) are set per GeometryInstance3D, max 16 per shader, no textures/arrays [18]. **[inference]** They are per node, so every instance inside one MultiMesh shares them; per-building variation inside a MultiMesh must live in colour/custom data (8 floats) or in a data texture indexed by `INSTANCE_ID` [1].
- The engine refuses a buffer smaller than `instance_count × transform size`, so a transform per instance is mandatory; a proposal thread asks for transform-less/indirect instancing [7].

**Upload behaviour (read from Godot master source [20])**

- `multimesh_set_buffer` uploads the entire buffer every call, then recomputes the AABB over all instances unless `custom_aabb` is set [20]. The docs say setting `custom_aabb` "prevents costly runtime AABB recalculations" [2].
- Per-instance setters (`set_instance_transform`, etc.) mark 512-instance dirty regions. At end of frame the engine uploads only dirty regions, unless more than 32 regions (or more than half the visible regions) are dirty, in which case it uploads the whole visible buffer [20].
- When any viewport uses motion vectors (TAA, FSR2-style upscalers) the buffer is double-sized to hold previous-frame data, so a set writes into both halves on first enable [20].
- **[inference]** So one building change in a chunk MultiMesh of ≤16k instances costs one ≤80 KB region upload through per-instance setters, which is cheap. Re-sending the whole buffer with `multimesh_set_buffer` on every edit costs `instances × 80 B`; for a 20k-instance chunk that is 1.6 MB, which is still fine per edit but not per frame across many chunks.
- Uploads flow through RenderingDevice staging buffers: 256 KB blocks, 128 MB cap by default, and exceeding the cap makes "the GPU … stall and wait for previous frames to finish" [21].

**What shipped engines do with instancing**

- Cities: Skylines 2 (Unity HDRP, custom BatchRendererGroup renderer): "almost every draw call the game makes uses instancing"; one large per-frame instance buffer; buildings use "about 50 floats per instance". A 1,000-population town still made 6,705 draw calls, 36 M rasterised triangles and 87.8 ms frames. The main causes were missing prop LODs, frustum-only culling, and shadows: 4,828 of 6,705 draws, about 40 ms, with every object a shadow caster [5].
- Matrix Awakens / City Sample: ~7,000 buildings "made of thousands of modular pieces", with "hundreds of instances" per building; "none make use of traditional levels of detail" because everything is Nanite [8][9]. The city holds seven million instanced assets [9]. This only works because Nanite does per-cluster culling and LOD on the GPU. Godot has no equivalent (see §2.4).

**Cost of the 15-parts-per-building worry**

- **[inference]** At 15 instances × 80 B the instance data is 1.2 KB per building, which is 1.2 GB at a million buildings and 12 MB at 10,000. Draw calls are not the problem, since instances are grouped by (chunk × module mesh). Vertex throughput and shadow passes are the problem, as the CS2 frame shows [5]. Each module must also run the full depth-prepass → opaque → shadow-cascade chain, so 15 small meshes cost far more than one box with a facade shader.
- **[inference]** Distinct module meshes multiply draw calls: chunks × module kinds × passes. With 64 visible chunks, 40 module meshes and 1 + 4 passes (opaque plus four shadow cascades), the ceiling is 12,800 draws; CS2's 6,705 draws already hurt [5]. Keep near-kit rendering to the chunks within a few hundred metres.

### 1.2 Merged ArrayMesh per building or per chunk

**API facts**

- `ArrayMesh` is "slightly faster than using a SurfaceTool"; SurfaceTool adds `generate_normals()`, `index()` [10]. MeshDataTool is slow [10]. Godot cannot generate geometry on the GPU [10].
- `RenderingServer.mesh_create_from_surfaces` is "more efficient for creating meshes with multiple surfaces" than adding surfaces one by one [19].
- In-place partial updates exist: `surface_update_vertex_region` / `attribute_region` / `skin_region` take a byte offset and PackedByteArray [11]. **[inference]** These need a fixed vertex layout and count, so they suit window-light or damage state baked into vertex attributes, not topology changes.
- `add_surface_from_arrays` accepts precomputed LOD index arrays in a `lods` dictionary keyed by distance [11]. `ArrayMesh` has no runtime LOD generator, and `SurfaceTool.generate_lod` is deprecated because "it does not preserve normals or UVs" [12]. `ImporterMesh.generate_lods()` is the replacement [13]. `ImporterMesh` is registered as a normal scene class [22], and the meshoptimizer module is built in release templates ("Having this on release by default … a lot of users like to do procedural stuff") [23]. **[inference]** So runtime LOD generation for generated meshes is available: build an ImporterMesh, call `generate_lods`, then `get_mesh()`. The cost is meshoptimizer simplification per building, which should run off the main thread.
- `shadow_mesh`: an optional position-only mesh "for rendering shadows and the depth prepass"; it must match source vertex positions [11]. **[inference]** For generated buildings, emitting a welded position-only shadow mesh is a cheap win given CS2's shadow numbers [5].
- Forward+ auto-instances MeshInstance3D nodes that share mesh and material (opaque or alpha-tested only) [14]. Unique merged meshes get no instancing benefit, so every merged building or chunk is its own draw per pass.

**Chunk merge vs per-building merge**

| | Per-building mesh | Per-chunk merged mesh |
|---|---|---|
| Draws | 1 per building per pass | 1 per chunk per material per pass |
| Culling granularity | Building | Chunk |
| Rebuild on edit | That building | Whole chunk (or a sub-cell) |
| Memory | Unique verts per building | Same verts, fewer headers |
| Engine precedent | CS1 LOD meshes merged per render group with a shared atlas [15] | UE HLOD Merged/Simplified layers [16] |

- Cities: Skylines 1 packs all building LOD textures into one 4096² atlas and requires LOD UVs in 0–1; mods warn above 600 tris / 1,000 verts per LOD and the game rejects building LODs above ~8,100 verts [15]. **[inference, from background knowledge noted as such in the search result]** CS1 merges LOD meshes per grid "RenderGroup" into combined meshes, which is why per-LOD vertex caps exist.
- Zylann's Voxel Tools, the most mature runtime-meshing project on Godot, meshes on worker threads and uploads on the main thread under a time budget (`voxel/threads/main/time_budget_ms`, default ~8 ms). Raising mesh block size from 16 to 32 "reduces the number of draw calls, but may increase the time it takes to modify voxels" [17]. It also reports that on Vulkan, destroying many small meshes when thousands exist causes slowdowns, and freeing is deferred to end of frame so it cannot be threaded [17]. **[inference]** Churning many small per-building meshes has a real cost in Godot 4; prefer fewer, larger, reused buffers.
- No published per-building generation timing for a Godot city builder turned up. **[inference]** A 4-wing courtyard building of a few thousand triangles produced from arrays in C# is in the tens to hundreds of microseconds on one core; upload and `generate_lods` will dominate. Measure before trusting.

### 1.3 Shader-driven facades on simple proxies

- Interior mapping (van Dongen, CGI 2008) raycasts rooms in the pixel shader. "The number of rooms has no effect on framerate or memory use"; the test building was 10 polygons and 1 draw versus 158 polygons and 5 draws modelled [24]. It has shipped in SimCity (2013), Assassin's Creed, Overwatch and Forza Horizon 4, and probably Marvel's Spider-Man [24].
- Townscaper cuts windows with a stencil after module placement rather than modelling them into tiles [25], which is another shipped example of openings kept out of the geometry.
- **How far it goes [inference]:** a box shader can own storey/bay splits, window placement, lit/unlit rooms, curtains, abandonment (boarded, dark, broken glass), brick/render variation, and ground-floor shopfront bands, all driven by 8 floats of per-instance data plus a data texture. It cannot give a true silhouette: cornices, balconies, dormers, roof pitch, chimneys, stepped setbacks and courtyards seen obliquely. The eye reads silhouette at street level and at grazing angles; parallax helps only within the depth budget of the proxy face.
- Roofs: **[inference]** a flat-roof proxy plus a roof shader works top-down. A pitched or hipped roof over an arbitrary polygon needs real geometry, e.g. a straight-skeleton roof. City Sample, despite everything being kit-built, still uses "custom roofs for non-rectangular buildings" [9].
- The shader's per-pixel cost stays the same at every LOD. The Godot docs note LOD "reduces vertex counts" but "per-pixel shading load for materials remains identical"; they suggest simpler distant materials [4]. The team's existing ~500 m fade of openings is exactly this material-level LOD.

### 1.4 Hybrids (what shipped cities converge on)

- City Sample: Houdini shape-grammar per building → point cloud of module instances → UE "Rule Processor" converts them to "thousands of instances" + a few custom meshes (roofs, collision) [9].
- CGA / CityEngine: mass model → component split into faces → per-face repeat/subdivide splits with floating sizes (`~`) so a whole number of floors or bays always fills the face [26][27].
- CS1/CS2: authored per-building assets with authored LODs, instanced; LODs atlas-merged per region in CS1 [5][15].
- **[inference]** The hybrid that fits Borough is a generated shell mesh (walls from the footprint polygon, roof from the polygon) plus the existing facade shader for openings, plus instanced detail modules (cornices, balconies, shopfronts, chimneys) only near the camera. This keeps the per-building instance count small (1 shell + a few near-only detail instances) instead of 15 everywhere.

---

## 2. LOD at city scale

### 2.1 Godot visibility ranges (manual LOD / HLOD)

- Each GeometryInstance3D has begin/end distances measured to its AABB centre, plus begin/end margins that act as hysteresis in Disabled fade mode or as a fade width otherwise [4].
- Fade modes: Disabled (instant switch; "best performance"), Self, Dependencies. Both fading modes force transparent rendering during the fade, which "has a performance impact" [4]. "Godot currently only supports alpha-based fading for visibility ranges" [4].
- The cheaper alternative is BaseMaterial3D Distance Fade set to Object Dither; dithering "is faster to render compared to alpha blending", avoids sorting glitches, looks noisy, and suits two LOD levels [4].
- HLOD: a `visibility_parent` hides children while the parent is in range, and children hide only "once the parent node is fully faded out" [4]. The documented example is literally BatchOfHouses → House1–4 [4].
- Visibility ranges also apply to MultiMeshInstance3D [4], and are exposed at server level via `instance_geometry_set_visibility_range(instance, min, max, min_margin, max_margin, fade_mode)` [19].
- Mesh LOD's `threshold_pixels` does not affect visibility ranges [21].

### 2.2 Godot automatic mesh LOD

- It uses meshoptimizer at import for glTF/.blend/FBX, is selected by a screen-space metric (default threshold 1 px, "perceptually lossless"), and has per-node `lod_bias` [3].
- Low-vertex meshes get no LODs at all [21]. **[inference]** Individual kit modules (a window frame, a cornice run) are mostly too small to benefit; automatic LOD helps merged shells and large chunk meshes more.
- It works on MultiMesh, but with one LOD level per MultiMesh [3]. An issue thread recommends a MultiMesh per 16×16-tile chunk for this reason [28].

### 2.3 Impostors and far proxies

- Octahedral impostors capture views at octahedral lattice points into an atlas and reconstruct them on a card, blending the three nearest frames. The technique came from Fortnite [29].
- Godot 4 options: zhangjt93/godot-imposter (native 4.x, baked in-editor); wojtekpil's original is 3.x-only [30].
- **[inference]** Impostors are a poor fit for Borough. Every building is unique (footprint, storeys, state), so a per-building bake would be needed and redone on every change. Impostors also do not show state such as lit windows. A box-proxy + facade shader is already a better "impostor" for boxy buildings: it is geometrically exact for the mass, carries per-instance state, and costs 12 triangles.
- UE World Partition HLOD layer types are Instancing (ISM with lowest LODs), Merged Mesh, and Simplified Mesh (merge and decimate) [16]; community practice for buildings is "Instancing → Merged/Simplified" [31]. UE HLODs are built offline by a commandlet, and a rebuild is incremental only for changed source actors [16]. **[inference]** A live-edited city cannot rely on offline HLOD; the Godot equivalent must be rebuilt at runtime per dirty chunk.

### 2.4 Nanite and why it is not available here

- City Sample runs thousands of instances per building with no authored LODs because Nanite handles cluster LOD and culling on the GPU [9]. UE 5.7 adds Nanite Assemblies (instanced parts inside one asset), documented for foliage only [32].
- Godot has no Nanite equivalent, no GPU-driven per-instance culling in MultiMesh [1][7], and no GPU geometry generation [10]. **[inference]** Any design copied from City Sample's "hundreds of instances per building" will hit Godot's per-MultiMesh culling and LOD and its vertex throughput. Borough has to LOD structurally: kit near, shell mid, box far.

### 2.5 Layering proposal for Borough, with hysteresis **[inference]**

| Band | Representation | Batching | Switch mechanism |
|---|---|---|---|
| Near (< ~150–250 m) | Shell + instanced detail modules | MultiMesh per (small chunk × module) | Visibility range on the detail MultiMeshes, margin ≥ 10–20 m, Object Dither fade |
| Mid (~250–500 m) | Shell mesh (generated), facade shader with openings | Merged per chunk, or MultiMesh of shells | Shader keeps openings |
| Far (> ~500 m) | Unit-box MultiMesh (current system), openings faded | MultiMesh per large chunk | Existing |
| Whole-city | Merged per-large-chunk box mesh or the same boxes | 1 draw per chunk | Visibility parent |

- Hysteresis comes free from the visibility-range margins [4]. Dither fade avoids the transparent pass [4].
- Chunk size is a trade-off. Culling, LOD and range decisions happen per chunk [1][3][4], so near bands want small chunks and far bands want big ones. Use different chunk sizes per band.
- Shadows: cap shadow casting to near/mid bands, or use the proxy for far shadows. CS2 lost ~40 ms to shadows of everything [5].

---

## 3. Incremental regeneration

### 3.1 Threading rules in Godot 4

- "Interacting with the active scene tree is **not** thread-safe"; building node subtrees outside the tree is fine; attach with `CallDeferred` [6].
- Server access from threads "is supported", but for the rendering server "thread-safe operation must be enabled in the project settings first" [6]. Avoid GPU-touching calls on threads, "such as creating new textures or modifying and retrieving image data", because they need synchronisation with the RenderingServer [6].
- The `Separate` rendering thread model is still marked experimental: "several known bugs which can lead to crashing … Not recommended for use in production at this stage" [21]. A 2026 proposal wants to make it default and cites a ~6× speedup in one project [33]. A reported case shows glTF loading on a background thread under Separate taking 17 minutes due to CommandQueueMT lock contention with `_draw()` [34].
- "Modifying a unique resource from multiple threads is not supported" [6].
- **[inference, the safe pattern]** Worker threads (plain .NET `Task`s, no Godot objects) build `Vector3[]`/`int[]`/`float[]` buffers or a raw `byte[]` vertex buffer from an immutable per-building input record. The main thread, under a per-frame millisecond budget, turns them into ArrayMesh surfaces / `mesh_create_from_surfaces`, swaps the mesh RID on the instance, and frees the old one. This is exactly Voxel Tools' design [17].
- MultiMesh buffers are plain `float[]`. Workers can fill the next chunk buffer; the main thread calls `multimesh_set_buffer` once. The docs name this as the peak-performance path: build the array "on multiple threads and upload it in one call" [1].

### 3.2 Dirty tracking **[inference; grounded in the engine behaviours above]**

- Key visuals by Building id. Keep an input fingerprint per building (footprint, storeys, frontage, kind, visible state, style seed) and regenerate only when it changes.
- Separate what changes often from what changes rarely:
  - Rare (geometry): footprint, storeys, wings, kind. These trigger a mesh rebuild.
  - Frequent (state): occupancy, lit windows, abandonment, damage. These go to per-instance colour/custom data or a per-building row in a data texture. Updating them is a 512-instance dirty region upload [20] or a texel write, never a remesh.
- Chunk membership is spatial. An edit dirties the building's chunk only. Neighbour-dependent geometry (shared party walls, corner pieces, WFC-style adjacency) widens the dirty set to adjacent buildings. Keep that set explicit.
- Swap atomically: keep drawing the old mesh until the new one is uploaded, so an edit never shows a hole.

### 3.3 Upload budgeting

- Voxel Tools defaults to ~8 ms per frame for main-thread uploads [17]. Staging buffers default to 128 MB, and exceeding that stalls the GPU [21].
- **[inference]** For "live edits visible within N frames", order the queue: player-edited and on-screen near chunks first, then state-only updates, then far chunks. At 60 fps a 2–4 ms upload budget covers a few merged chunk meshes or dozens of building shells per frame. The first-frame visibility of an edit then depends on the queue position, not on generation cost.

---

## 4. Determinism and stability of visuals

- Seed every visual choice from a counter-based hash of (world seed, Building id, purpose tag, and, where needed, face/bay/storey index). Noise-based RNG (Squirrel Eiserloh, GDC 2017) is "like looking up a value in an infinitely large table"; SquirrelNoise5 fixes a high-bit weakness in the version from the talk [35]. The simulation already uses the same shape of counter hash for simulation randomness (project CLAUDE.md). **[inference]** Visual hashes should use distinct purpose tags and live only in the shell, so appearance never feeds the simulation State Hash.
- Never draw from a sequential RNG shared across buildings. Iteration order then leaks into appearance, and adding one building reshuffles all later ones. **[inference]**
- Neighbour-independence: a building's appearance should be a pure function of its own inputs and seed. Where neighbour context is needed (corner treatment, party walls, a terrace continuing a cornice line), derive it from stable neighbour attributes, not from solver state. A solver that re-solves a region can flip unrelated modules (see §5). **[inference]**
- Bay and storey counts: derive them from integer-snapped dimensions with fixed rounding rules, so tiny footprint changes do not flip counts back and forth. Float determinism across machines is not needed. **[inference]**
- Reload identity follows automatically if all inputs are saved simulation state plus the world seed and the generator version. Version the generator: a kit or rule change legitimately changes looks, and a version tag tells a bug apart from an upgrade. **[inference]**

---

## 5. Constraint and solver-based module selection

### 5.1 WFC / Model Synthesis

- Model Synthesis (Merrell 2007) predates WFC; Merrell states "WFC reimplements the model synthesis algorithm" [36]. Failure (an empty catalog) is unavoidable in some cases unless P = NP, and the chance of success on hard tile sets drops quickly with output size [36][37].
- Modifying in Blocks solves overlapping blocks against fixed borders. A contradiction restarts only one block, and repeated failure can fall back to a "known good" tile arrangement. It needed "careful tuning", and "performance isn't too great" because overlapping blocks redo work [37].
- Basic WFC restarts the whole output on contradiction. Backtracking is guaranteed to terminate but rarely needed with sensible tiles; constraint problems are "typically exponential" in the worst case [38].
- **Suitability for incremental edits [inference]:** re-solving a region around an edit can change modules that the player did not touch, which is the flicker you want to avoid (§4). Pinning everything outside the edit block as fixed borders (Modifying in Blocks style) bounds the change. It can still fail, so a guaranteed fallback set is mandatory.

### 5.2 Townscaper (marching-cubes / dual-grid modules)

- The grid is an irregular quad grid made by splitting and relaxing a hex grid [25]. Modules are corner segments chosen marching-cubes-style from the fill state of cell corners; Stålberg says tile meshes "go on the dual grid, of course" [39][40]. In 2D a 4-corner case table has 16 cases, 6 up to symmetry; stacked it is 2⁸ = 256 [41].
- WFC with priorities picks among variants; decoration runs as a separate later pass. An edit "ripples" through the connected structure, so on big builds the decoration shows up later. On unsatisfiable constraints Townscaper is "allowed to fail silently" [25]. Tile counts: Bad North had over 300 tiles, and a Townscaper reskin would need ~500 [25].
- Stålberg on authoring the case set: "Towards the end I just built random shapes and fixed cases as they appeared" [42].
- **[inference]** A pure marching-squares/cubes lookup with no solver is O(cells), deterministic, local (an edit changes only the cells touching changed corners) and cannot fail. That makes it the best-behaved option for live edits. The WFC layer on top is what adds ripple and failure.

### 5.3 Fit for Borough **[inference]**

- Borough's simulation already owns the footprint polygon, storeys and frontage, so it does not need a solver to decide massing. A deterministic split grammar (CGA-style [26][27]) is local, cannot fail, and is stable under edits.
- Reserve solver-like logic for small local decisions, such as corner treatment or which ground-floor bay holds the door. Keep that logic as pure functions of the building's own inputs and seed.

---

## 6. Modular kit authoring (Blender → glTF → Godot)

### 6.1 Grid, pivots, sockets

- Skyrim kits: every piece fits a "footprint" bounding box on a shared grid; designers snap at half-footprint. Pivots go at ground-plane centre by default, with deliberate exceptions such as edge pivots for swinging pieces. Numeric suffixes `01/02/03` mark visual variants of the same role and footprint. Off-grid angles used "Snap to Reference" and a "Pivot and Flange" kit, at a cost in art and design complexity [43][44].
- **[inference, convention]** Use 1 Blender unit = 1 m. glTF is metres, Y-up; Blender's exporter converts from Z-up. Pick a bay module width, e.g. 1 m or 0.5 m sub-grid with 3–4 m bays, and a fixed storey height per style. Put the pivot at bottom-left-outer-face for wall modules so they chain along a frontage, and at bottom-centre for props. Express sockets as Blender empties named `socket_<role>` so an import script can read them.

### 6.2 Getting metadata into Godot

- Blender custom properties export as glTF `extras` when "Include › Custom Properties" is on [45]. Godot 4.4+ imports them as `metadata/extras`, sometimes on the Mesh resource rather than the node [46].
- Godot import suffixes: `-noimp`, `-col`/`-convcol`/`-colonly`, `-occ`/`-occonly` (occluder from mesh), `-navmesh`, `-alpha`, `-vcol`, etc. They are case-insensitive and accept `-`, `$` or `_` [47]. **[inference]** Use `-occonly` for coarse building occluders authored in Blender, and `-noimp` for authoring helpers.
- Occlusion culling is CPU-side (Embree raster). An occludee is culled only if its AABB is fully hidden. Occluders can be generated procedurally via ArrayOccluder3D; moving one is expensive, toggling visibility is cheaper [48]. Forward+ already has a depth prepass, and the biggest win is on Mobile [48]. **[inference]** At street level in dense blocks, box occluders per building are worth testing; at a top-down whole-city camera they will rarely pay off.

### 6.3 Textures: trim sheets and atlases

- Insomniac's "Ultimate Trim" (Sunset Overdrive, GDC 2015) uses horizontal trim strips with 45° normal-map bevels and a standardised UV layout. Any material can be swapped without re-UVing, which cut memory and art time for a large city built by 8–12 environment artists [49].
- CS1 packs all building LOD textures into a 4096² atlas and requires LOD UVs inside 0–1 [15]. CS2 uses a custom virtual texture with tile atlases up to 16368×8448 [5].
- **[inference]** For Borough: one or a few trim sheets and texture arrays per style, shared by every module and by the generated shell, so the whole near band draws with a handful of materials. Per-instance variation picks the array layer, tint and grime from custom data. The existing stochastic brick tiling fits this.

### 6.4 Arbitrary frontage widths

- CGA handles this with repeat splits plus floating sizes: `~` sizes stretch so an integer count fills the face [26][27].
- **[inference]** The usual kit answer has three parts. (1) Repeat whole bays: n = floor(width / nominal bay), then stretch each bay by width/n. A stretch of ±15% is invisible on plain wall and visible on windows, so let the shader draw windows at fixed size inside a stretched bay. (2) Absorb the remainder in filler pieces: pilasters, downpipes, corner quoins. (3) Scale-safe modules: only X-scaled pieces that have no features to distort, or 9-slice-style meshes whose ends stay fixed while the middle stretches.
- Corners: at 90° use dedicated inner and outer corner pieces. At other angles use a mitred corner generated in the shell, with a vertical trim piece to cover the seam, the "pivot and flange" idea [43]. Townscaper shows skewed pieces "look fine from street level" [25].
- Courtyard wings: **[inference]** treat the inner ring as its own frontage set with its own style rule (plainer, service). Because the ring is enclosed it needs no far LOD beyond the roof.

---

## 7. Measured numbers (collected)

| Quantity | Value | Source |
|---|---|---|
| MultiMesh per-instance data (3D, all fields) | 20 floats / 80 B | [19][20] |
| MultiMesh partial upload granularity | 512 instances; full upload if >32 dirty regions or >½ visible | [20] |
| 1 M MultiMesh instances | ~144 fps (anecdotal, unknown HW/mesh) | [7] |
| CS2 frame, 1,000-pop town | 87.8 ms, 6,705 draws, 36 M tris, 121 M verts, shadows ~40 ms | [5] |
| CS2 building instance data | ~50 floats per instance | [5] |
| CS2 typical low-density house | < 10 k verts | [5] |
| City Sample | ~7,000 buildings, 7 M instanced assets, hundreds of instances per building, 24 kits / 2,000+ meshes | [8][9] |
| CS1 building LOD | atlas 4096²; mod warning ≥ 600 tris/1,000 verts; reject > ~8,100 verts | [15] |
| Voxel Tools main-thread upload budget | ~8 ms default | [17] |
| Godot staging buffer | 256 KB blocks, 128 MB cap | [21] |
| Interior mapping example | 10 polys/1 draw vs 158 polys/5 draws | [24] |
| Townscaper / Bad North tile counts | ~500 / >300 | [25] |
| Chunked MultiMesh devlog (manual cull) | ~30–40% speedup; 3080 Ti usage fell from 60–80% to 30% | [50] |

No published figure turned up for per-building procedural mesh generation time in a Godot city builder. That number needs a local measurement.

---

## 8. Engineering consequences of design choices

Each row names the requirement, what it pushes toward, and what it rules out.

**8.1 Arbitrary polygon footprints, including courtyard wings and non-90° corners**
- Pushes toward a generated shell mesh from the polygon (walls per edge, a roof via straight skeleton or flat roof), with the facade shader doing openings in each face's local UV space.
- Rules out a pure unit-box MultiMesh for mid/near range; boxes cannot represent L/U/courtyard plans or skewed corners. It also rules out kits that only do 90° corners unless the shell supplies mitred corners.

**8.2 Live edits visible within N frames**
- Pushes toward per-building dirty tracking, pure generation on worker threads from an immutable input record, a prioritised main-thread upload queue with a ms budget, and old-mesh-until-new-is-ready swaps. State-only changes go through instance custom data or a data texture.
- Rules out offline HLOD (UE-style) and per-chunk merged meshes with large chunks near the camera, since one edit forces a whole-chunk remesh. Global solvers are out too, because re-solving is unbounded and can fail.

**8.3 Identical look every frame and after reload**
- Pushes toward counter-hash seeding from (world seed, Building id, purpose tag, element index), appearance as a pure function of saved inputs, integer-snapped bay/storey counts, and a generator version tag.
- Rules out sequential RNGs, iteration-order-dependent generation, and WFC-style solving whose result depends on solve order or on neighbours' solver state.

**8.4 Neighbour edits must not change unrelated buildings**
- Pushes toward per-building generation with explicit, minimal neighbour inputs (party-wall flags, corner adjacency) and a dirty set that includes only those neighbours.
- Rules out region re-solving (WFC/Model Synthesis) unless everything outside the edit is pinned. Even then, a fallback tile set is required because block solves can fail [37].

**8.5 Very large counts (tens of thousands up to a million Buildings)**
- Pushes toward the far band staying at one instance per building (the box and shader), large far chunks, and near-only kit detail behind visibility ranges. Shadows should be limited by band.
- Rules out 15 instances per building everywhere. At 1 M that is 15 M instances and ~1.2 GB of instance data, all vertex-shaded and shadowed [inference, §1.1]. It also rules out one global MultiMesh (no culling and one LOD for all [1][3]) and per-building impostor bakes.

**8.6 Strong close-up detail**
- Pushes toward real geometry for silhouette near the camera (cornices, balconies, roofs, shopfronts), instanced from a kit in small near chunks. Keep the shader for openings and interiors. Use trim sheets and texture arrays to keep material count low.
- Rules out a shader-only facade for close range, because it gives no silhouette and parallax only goes as deep as the proxy face. Large near chunks are out too, because the whole chunk loses culling and LOD granularity.

**8.7 Visible state (abandonment, lit windows, occupancy, damage)**
- Pushes toward state in per-instance custom data (4 floats) plus colour, or a per-building data texture row indexed by a building slot. The shader reads it for window lighting, boarding, grime and dark storeys. Updates then cost a dirty-region upload, not a remesh [20].
- Rules out baking state into textures or impostors, and per-instance `instance uniform`s for MultiMesh-drawn buildings (they are per node [18]). Remeshing on state change is out as well.

**8.8 Whole-city zoom with smooth transitions**
- Pushes toward visibility ranges with margins as hysteresis, and Object Dither distance fade rather than alpha fade [4]. Nested visibility parents per chunk give HLOD [4], and chunk size should vary by band.
- Rules out alpha-blended LOD fades on thousands of objects (transparent pass cost [4]). Relying on automatic mesh LOD for small kit pieces is out too, since they get no LODs [21].

**8.9 Blender-authored kits as the source of truth**
- Pushes toward metre units, a fixed bay/storey grid, pivot-at-outer-face-bottom-left for wall pieces, and sockets as named empties. Metadata goes in glTF extras → `metadata/extras`. Use Godot suffixes for occluders and collision, and name variants `role_01..0n`.
- Rules out per-piece unique textures (they defeat batching and swap), and pieces whose features distort under X-stretch unless they are flagged repeat-only.

---

## 9. Sources

1. Godot docs, Optimization using MultiMeshes — https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html
2. Godot docs, MultiMesh class — https://docs.godotengine.org/en/stable/classes/class_multimesh.html
3. Godot docs, Mesh level of detail — https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html
4. Godot docs, Visibility ranges (HLOD) — https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html
5. Paavo Huhtala, Why Cities: Skylines 2 performs poorly — https://blog.paavo.me/cities-skylines-2-performance/
6. Godot docs, Thread-safe APIs — https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html
7. godot-proposals discussion #8647 (indirect rendering, multimesh without transform) — https://github.com/godotengine/godot-proposals/discussions/8647
8. Epic, Introducing The Matrix Awakens — https://www.unrealengine.com/en-US/blog/introducing-the-matrix-awakens-an-unreal-engine-5-experience
9. Epic docs, City Sample project — https://dev.epicgames.com/documentation/unreal-engine/city-sample-project-unreal-engine-demonstration?lang=en-US
10. Godot docs, Procedural geometry — https://docs.godotengine.org/en/stable/tutorials/3d/procedural_geometry/index.html
11. Godot docs, ArrayMesh class — https://docs.godotengine.org/en/stable/classes/class_arraymesh.html
12. Godot docs, SurfaceTool class — https://docs.godotengine.org/en/stable/classes/class_surfacetool.html
13. Godot docs, ImporterMesh class — https://docs.godotengine.org/en/stable/classes/class_importermesh.html
14. Godot docs, Optimizing 3D performance — https://docs.godotengine.org/en/stable/tutorials/performance/optimizing_3d_performance.html
15. Cities: Skylines modding, Building asset creation — https://cslmodding.info/asset/building/
16. Epic docs, World Partition HLOD — https://dev.epicgames.com/documentation/en-us/unreal-engine/world-partition---hierarchical-level-of-detail-in-unreal-engine
17. Voxel Tools docs, Performance — https://voxel-tools.readthedocs.io/en/latest/performance/ (and https://github.com/Zylann/godot_voxel/blob/master/doc/source/performance.md)
18. Godot docs, Shading language (per-instance uniforms) — https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/shading_language.html
19. Godot source, RenderingServer class reference XML (`multimesh_set_buffer`, `mesh_create_from_surfaces`, `instance_geometry_set_visibility_range`) — https://github.com/godotengine/godot/blob/master/doc/classes/RenderingServer.xml
20. Godot source, RD MeshStorage (MultiMesh stride, dirty regions, set_buffer) — https://github.com/godotengine/godot/blob/master/servers/rendering/renderer_rd/storage_rd/mesh_storage.cpp
21. Godot source, ProjectSettings reference XML (thread_model, staging_buffer, mesh_lod, cluster limits) — https://github.com/godotengine/godot/blob/master/doc/classes/ProjectSettings.xml
22. Godot source, register_scene_types.cpp (ImporterMesh registered) — https://github.com/godotengine/godot/blob/master/scene/register_scene_types.cpp
23. Godot source, meshoptimizer module config.py / register_types.cpp — https://github.com/godotengine/godot/tree/master/modules/meshoptimizer
24. Joost van Dongen, Interior Mapping (CGI 2008) — https://www.proun-game.com/Oogst3D/CODING/InteriorMapping/InteriorMapping.pdf ; blog — http://joostdevblog.blogspot.com/2018/09/interior-mapping-real-rooms-without.html
25. Game Developer, How Townscaper Works — https://www.gamedeveloper.com/game-platforms/how-townscaper-works-a-story-four-games-in-the-making
26. Müller et al., Procedural Modeling of Buildings (SIGGRAPH 2006) — https://dl.acm.org/doi/10.1145/1141911.1141931
27. Esri CityEngine, Tutorial 6: Basic shape grammar — https://doc.arcgis.com/en/cityengine/latest/tutorials/tutorial-6-basic-shape-grammar.htm
28. Godot issue #76436 (MultiMesh LOD per whole MultiMesh; chunking advice) — https://github.com/godotengine/godot/issues/76436
29. Ryan Brucks, Octahedral Impostors — https://shaderbits.com/blog/octahedral-impostors/ ; 80.lv, Impostor Baker for UE4 — https://80.lv/articles/impostor-baker-for-ue4
30. zhangjt93/godot-imposter — https://github.com/zhangjt93/godot-imposter ; wojtekpil/Godot-Octahedral-Impostors — https://github.com/wojtekpil/Godot-Octahedral-Impostors
31. Polycount, 5.1 world partition, landscape HLODs — https://polycount.com/discussion/232455/5-11-world-partition-landscape-hlods-and-crashes ; Simplygon UE5 HLOD — https://documentation.simplygon.com/SimplygonSDK_10.4.304.0/ue5/concepts/hlod.html
32. Epic docs, Nanite Assemblies (5.7) — https://dev.epicgames.com/documentation/unreal-engine/nanite-assemblies?lang=en-US
33. godot-proposals #14578, Promoting the separate thread model — https://github.com/godotengine/godot-proposals/issues/14578
34. Godot issue #112452, glTF load slow under Separate thread model — https://github.com/godotengine/godot/issues/112452
35. Squirrel Eiserloh, Math for Game Programmers: Noise-Based RNG (GDC 2017) — https://www.gdcvault.com/play/1024365/Math-for-Game-Programmers-Noise ; SquirrelNoise5 — https://gist.github.com/kevinmoran/0198d8e9de0da7057abe8b8b34d50f86
36. Paul Merrell, Model Synthesis — https://paulmerrell.org/model-synthesis/ ; Comparing Model Synthesis and WFC — https://paulmerrell.org/wp-content/uploads/2021/07/comparison.pdf
37. BorisTheBrave, Model Synthesis and Modifying in Blocks — https://www.boristhebrave.com/2021/10/26/model-synthesis-and-modifying-in-blocks/
38. BorisTheBrave, Wave Function Collapse Explained — https://www.boristhebrave.com/2020/04/13/wave-function-collapse-explained/
39. Oskar Stålberg on X (dual grid) — https://x.com/osksta/status/1459485788987613190?lang=en ; https://x.com/OskSta/status/1448248658865049605
40. Stålberg, Organic Towns from Square Tiles (IndieCade Europe 2019) — https://www.youtube.com/watch?v=1hqt8JkYRdI
41. kai-denrei/oskar-procedure, dual grid and tiles — https://github.com/kai-denrei/oskar-procedure/blob/main/docs/03-dual-grid-and-tiles.md
42. Stålberg on X (building the case set) — https://x.com/osksta/status/1189122653048717312?lang=en
43. Joel Burgess, Skyrim's Modular Level Design (GDC 2013 transcript) — http://blog.joelburgess.com/2013/04/skyrims-modular-level-design-gdc-2013.html
44. The Level Design Book, Modular kit design — https://book.leveldesignbook.com/process/blockout/metrics/modular
45. Blender manual, glTF 2.0 add-on — https://docs.blender.org/manual/en/3.3/addons/import_export/scene_gltf2.html
46. Godot forum, glTF import metadata/extras — https://forum.godotengine.org/t/gltf-import-how-does-one-get-metadata-extras/105711 ; PR #39024 — https://github.com/godotengine/godot/pull/39024
47. Godot docs, Node type customization using name suffixes — https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/node_type_customization.html
48. Godot docs, Occlusion culling — https://docs.godotengine.org/en/stable/tutorials/3d/occlusion_culling.html
49. Morten Olsen, The Ultimate Trim (GDC 2015) — https://www.gdcvault.com/play/1022324/The-Ultimate-Trim-Texturing-Techniques ; transcript — https://archive.org/details/GDC2015Olsen2
50. MM's Maze Madness devlog, seamless and scalable wall rendering — https://maclyn.itch.io/mms-maze-madness/devlog/898004/reasonably-seamless-and-scalable-wall-and-ground-rendering
