# Chunked scene preparation

Owns the board row *Bounded scene preparation and uploads*. State: design. Worktree
`chunk-preparation`, based on `lot-index` at `f049ab4e`. Steps 1 to 4 are implemented.

## Outcome

Every drawn component prepares only the owners its camera can reach. It caches each owner's plan,
evicts plans that fall out of reach, and re-plans only the owners a city change touched. Draw lists
stay byte-identical to the current shell at fixed cameras.

## Measurements, 2026-10-22

Conditions for every figure below:

- `stress-shopping.toml`, 1,000,000 Citizens, saved at Tick 256 with headless `--save-city` (55 MB).
- i5-10400 (12 threads), GTX 1080, Godot 4.7.2 Vulkan Forward+, `scripts/build-shell.sh --optimized`.
- Load average 1.10–1.47 at launch. Three runs of each view, plus a fourth for the per-layer fill split.
  Phases agree within 5% across runs.
- Wall-clock probes around each producer, added for the measurement and then removed. The patch is
  not committed. `render_regate` rows agree with the probe sums.
- Cameras come from one drive script: the opening camera at Tick 257, `focus 1875 1875 700` at 262,
  `focus 3000 1875 700` (a 4,500 m pan) at 271, and `focus 1875 1875 300` at 281.

### First full pass

`UpdateBuildings` takes 1.51–1.55 s on the first `Draw`.

| Part | ms | Notes |
|---|---:|---|
| `PlanGardens` | 646–671 | Gathering Lots into blocks 166–176; planning 12,499 blocks 476–492 |
| `Massings` | 383–404 | Every Building, plus every foliage footprint |
| `FillGardens` | 232–246 | Ground 172, hedges 42, all gated layers together 32 |
| `ZonedBlocks` fill | 158–162 | Every permission rectangle |
| `FillCarParks` | 29 | Car parks and module-drawn Buildings |
| `Plots` fill | 20–22 | Vacant Lots |

`FinishReady` also runs `Pave` once. Footways take 459–463 ms, roads 138 ms and the Address
snapshot 48 ms. Kerbs, trees and rocks cost nothing there, because a gated build before the camera
exists is empty. The first frame's `FlushInstances` takes 231–233 ms of uploads.

### Rebuilds on camera moves

| Rebuild | Total ms | Kerbs | Scatter | Garden fences | Garden trees | Railings |
|---|---:|---:|---:|---:|---:|---:|
| Descent to 700 m | 708–715 | 378–384 | 138–140 | 84–100 | 63–64 | 19 |
| 4,500 m pan | 289–294 | 68–73 | 103–111 | 66–72 | 24–27 | 12 |
| Descent to 300 m | 269–298 | 45–48 | 80–82 | 74–79 | 39–51 | 18 |

- Kerb planning is 297–304 ms of the descent, for 19,097 Segments, about 16 µs each. The rest of the
  kerb row writes 137,331 instances.
- The pan plans 5,089 newly reached Segments in 32–38 ms. Most of every pan row is rewriting
  instances that were already drawn.
- `Scatter` has no cache. Each rebuild re-plans 21,600–25,344 Cells and walks all 204,557 foliage
  footprints to index them, which takes 25–26 ms of the scatter row.
- At 700 m, 12% of garden props and 44% of garden trees are within reach.

### Memory and plan sizes

| Measure | Value |
|---|---|
| Process RSS at opening, 700 m, after the pan | 2.65–2.72 GB, 3.35–3.41 GB, 3.78–3.84 GB |
| Managed heap at the same points | 1,070 MB, 1,701 MB, 1,812 MB |
| Garden plan, whole city | 980,075 pieces and 183,410 panel runs, about 82 MB |
| Ungated part of it (ground and hedges) | 432,562 pieces, about 31 MB |
| Largest block plan | 137 entries |
| Kerb cache | 24,743 Segments after the pan; it empties only on `Pave` |

### Fence sharing

`PlanGardens` deduplicated 37,922 fence keys within a block and 4,132 across blocks. Every
cross-block case was a block meeting the key already drawn by its west neighbor, `(column − 1, row)`.
The 25,000-Citizen city breaks that pattern: block (500, 510) shares a key with an earlier block
that is neither its west nor its south neighbor. The owner rule below does not rely on adjacency.

### Edits

Over 344 Ticks at speed 8 the city changed one Building slot and ran no geometry pass. This save
cannot measure the cost of a structural edit. The code sets these costs instead:

- Every non-empty batch of player Commands calls `WorldChanges.Invalidate()` (`Main.Verbs.cs`), so
  each Command costs a full pass, 1.51–1.55 s here. A Street, Connect or Street demolition also
  re-runs `Pave`, about 0.65 s.
- A Building edit that changes geometry re-plans every garden, re-indexes every foliage footprint,
  re-scatters, and refills plots, zones, car parks and gardens.
- A wash change, and every Tick under the Age wash, runs the full pass. Gardens, kerbs and scatter
  do not read the wash.

### Repeatability of the draw list

Draw lists from three runs are byte-identical at all four cameras once `traveller` and `car` rows
are removed. Those two layers follow frame timing. Byte equality is therefore a usable check.

## Component inventory

"Reads" lists what a component reads beyond its owner's own row. Each of these reads crosses an
owner boundary, so the owner rules below must account for it.

| Component | Owner | Reads | Gate | Instance order |
|---|---|---|---|---|
| Garden dressing | Block `(column, row)` | Member Lots via `GardenLotAt`; `ZoneInterior`, which reads the four bounding Segments' kinds; the shared fence set | Tree 4,000 m, props 2,000 m | Block order, then Lot order |
| Garden ground and hedges | Block | As above | None | As above |
| Garden ground as clearance | Block | As above | Feeds `Scatter` | Footprint list order |
| Wild trees and rocks (`Scatter`) | Cell (128 m) | Woodland and terrain of the Cell; water Cells; foliage footprints within 8 m; block lattice (`InAYard`); `_paved` | Trees 8,000 m, rocks 4,000 m | Cells by distance from `CityAnchor`, ties by north then east |
| Kerbs | Segment | Its two Nodes, mitres at both ends, its slice of the Address snapshot | 5,000 m | Segment slot order |
| Roads and footways (`Pave`) | Segment | Nodes; `Mitre` reads neighbor Segments at both Nodes | None | Segment slot order |
| Building massing, roofs, yards | Building | Own Lot, Address Segment, Units, wash | None | Building slot order |
| Family bodies | Building | Neighbor Buildings through `Attached` probes | Near band 500 m | One `Replace` per Building |
| Far fade | Building | Shader dither only | 450–500 m | n/a |
| Foliage footprints | Building, car park, block | Massing output, car park surfaces, garden ground | Feeds `Scatter` | Massing order |
| Plots | Lot | `ParcelFrame`, `ZoneInterior` | None | Lot slot order |
| Zones | Permission rectangle | `LotsInCells` near the rectangle | None | Rectangle slot order |
| Car parks and modules | Building | Own Lot, block ground, Units | None | Building slot order |
| Ground, hazard, water, flood | World | Cell tables | None | Slot order |

`WorldChanges` records Building slots only. Lot changes the simulation makes without a Command are
recorded nowhere. They reach the drawing only when some other edit forces a full pass.

## Decisions

### 1. Owner rules

Every piece belongs to exactly one owner. An owner's plan is a pure function of a fixed set of
inputs, and it lists its pieces in the order the whole-city walk writes them today.

| Dependency | Rule | Why it reproduces today's drawing |
|---|---|---|
| Fences across blocks | The global layout walks blocks in `(column, row)` order and records, for each block, the attempted keys that an earlier block attempted first. The block's dressing drops a `Boundary` with one of those keys. A `Railing` always draws and still claims its key. | The earlier block wins, as in the whole-city walk. A block's set of attempted keys depends only on its own layout, never on what an earlier block suppressed. The rule needs no claim about which blocks can share a key. |
| Fences within a block | Unchanged. The block plans as one unit. | Lot order inside a block is untouched. |
| Garden ground clears wild trees | Split gardens into a layout and a dressing. The layout holds member Lots, garden areas, ground rectangles and attempted fence keys, and it draws no random numbers. A Cell's scatter reads the ground rectangles of blocks whose land, grown by 8 m, overlaps the Cell. | Clearance is a point-in-any-rectangle test, which does not depend on order. Ground rectangles come from the layout alone, before any `GardenDraw`. |
| Building footprints clear wild trees | Keep a footprint index by Cell, updated per Building, in place of rebuilding it on every `Scatter`. | The same test over the same rectangles. |
| Scatter order | Keep `CityAnchor` global. It is the bounding box of all footprints, including garden ground from the layout. | The layout is global, so the anchor stays exact and needs no change of order. |
| Kerbs read Addresses | A Segment's input is its slice of the Address snapshot and its mitres. The snapshot is still taken at `Pave`. | `SegmentKerbs` already reads only that slice. Kerbs keep showing the Addresses of the last repave, as today. |
| Gardens depend on a block's Lots | The layout's inputs are the block's live Lots (slot, id, face, parcel, footprint, occupancy) and the block's land. | `PlanBlock` reads nothing else. |
| Family bodies probe neighbors | Unchanged. These stay global, with the existing neighbor refresh. | Out of reach-gating scope. |

### 2. Unit of preparation

- The owner is the unit of planning: block, Segment, Cell or Building. An owner is planned whole
  or not at all.
- A fixed grid of 256 m squares is the unit of scheduling and eviction. An owner belongs to the
  square that holds the center of its bounds. A square records the union of its owners' bounds per
  component.
- The grid is independent of `InstanceLayer.ChunkMetres`. That 1,024 m chunk sets draw calls at the
  whole-city camera (0066) and must stay free to change.
- 1,024 m squares would over-plan the 2,800 m prop reach by up to 2.3 times its area. 256 m squares
  over-plan it by up to 1.3 times. The 1M city has 3,210 squares holding blocks, with at most 4 blocks
  each, and no block land wider than 120 m.
- Pieces are still filtered one by one through `InstanceLayer.Reaches` when written. Squares only
  decide which owners have a plan. Draw output is unchanged as long as every owner with a reached
  piece has a plan.

### 3. Residency classes

| Class | Plans held | Components |
|---|---|---|
| Reach-gated | Owners in squares within plan reach | Garden dressing (gated layers), kerbs, wild trees and rocks |
| Global | Every owner, never evicted, re-planned per owner on change | Garden layout, garden ground and hedges (decision D1), Buildings and bodies, footprint index, roads and footways, plots, zones, car parks and modules, ground and water |
| Far stand-in | Replaces a global layer beyond a distance | Garden ground and hedges, if the user picks a stand-in from rendered options (D1). Far Building bodies belong to the board row *Rendering detail across zoom levels*. |

### 4. Lifecycle

```
Absent ──enters plan reach──▶ Queued ──budget──▶ Planned ──budget──▶ Written
   ▲                            ▲                                   │
   │                            └────── input changed (epoch + 1) ──┤
   └──────────── leaves evict reach: remove instances, drop plan ────┘
```

- Plan reach is the largest `Reach` among the component's gated layers. `Reach` is the detail
  distance × 1.15 plus a quarter of the detail distance, the same quantity `InstanceLayer` uses.
- Evict reach is 1.5 × plan reach (provisional).
- A rebuild triggers at the same moment as today, when `InstanceLayer.Stale` says the camera moved
  past the slack.
  - Owners whose bounds lie wholly within reach from both the old and new eye keep their instances.
  - Owners that straddle either boundary are rewritten from their cached plan.
  - Owners now wholly out of reach lose their instances.
- An invalidated owner keeps its old instances until the replacement is written, so nothing flickers.
- `draw` drains the queue before writing, as it already drains uploads. Budget deferral therefore
  never changes a draw list.
- A chunk keeps its 15% residency margin only if it was resident when the eye last moved
  (`InstanceBuffer.Batch.Held`). Without that, a chunk first uploaded while it held only far owners
  lost the margin, and stayed hidden at a still camera after nearer owners arrived.

### 5. Invalidation

| Change | Source | Owners touched |
|---|---|---|
| Building created, destroyed, occupied, vacated, abandoned, let | `WorldChanges.Buildings` | The Building; its block's layout and dressing (occupancy decides gardens); footprint Cells within 8 m; car park and module owners |
| Lot created, removed, resubdivided, fronted, occupied or vacated | Lot journal in `WorldChanges` | Block layout and dressing; plots; zones over it; kerbs on its Segment at the next `Pave` |
| Segment laid, changed or demolished | Segment journal in `WorldChanges` | Its kerbs, roads and footways; neighbors at both Nodes, before and after (mitres, dead ends); the up to two blocks whose land it bounds; tree and rock Cells within `StreetGrid.SpanLimit` + 1 blocks of its ends (`InAYard` merges blocks across missing Streets) |
| Permission rectangle painted or erased | Permission rectangle journal in `WorldChanges` | That rectangle's zone |
| Woodland count of a Cell | None. Each Cell plan stores the count it used. | That Cell, found stale when its plan is next written |
| Wash change | Shell | Building colors only, through a recolor pass |
| Ruleset adoption, tuner rebuild, study toggles | `Invalidate()` | Everything (unchanged) |

- Player Commands stop calling `Invalidate()`. The journals carry what those Commands change.
  The journals stay unsaved and unhashed, so no State Hash moves.
- The journals keep recording while `Full` is set. A consumer with its own baseline, such as
  `Repave`, reads them on a Tick that also runs a full Building pass.
- Each journal is fed by its table's `Rows`. `Rows` reports every allocation and free to an optional
  observer, and a table method that writes a drawn field calls `Rows.Report`. `World.Changes`
  attaches the observers. A table may hold only declared columns (`BOR0901`), so the observer lives
  on `Rows`.
- `WorldChangesTests` steps a city through a Street, a demolition, a Service placement, a Zone and
  a Street demolition, then 120 quiet Ticks. It fails if any Lot, Segment or rectangle field the
  shell draws changes without its slot in the journal.
- A changed owner bumps its epoch. A plan or written result tagged with an older epoch is discarded.

### 6. Budgets

- Each frame spends a preparation allowance on the main thread, nearest square first across all
  components. Planning and writing share it. The allowance is 4 ms (provisional, decision D4).
- These are the measured rates:
  - Kerb planning: 16 µs per Segment.
  - Garden block planning: about 38 µs per block.
  - Scatter, planning and writing together: about 6.4 µs per Cell.
  - Writing: 0.4–0.6 µs per instance.
- The descent to 700 m holds about 440 ms of planning (kerbs 300, scatter 140) and about 270 ms of
  writes. At 4 ms per frame it fills in over about 3 s at 60 fps, nearest first. At 8 ms it takes
  about 1.5 s. These figures are estimates from the rates above.
- The memory bound is the plans of owners within evict reach. A hard cap, 64 MB across reach-gated
  components (provisional), evicts the farthest squares first. A profile row reports plan bytes per
  component.

### 7. Workers

Workers are deferred (decision D3).

- Writes must stay on the main thread, because `InstanceBuffer` and Godot are main-thread only.
- Four workers would take the descent's 440 ms of planning off the main thread. The 270 ms of writes
  would remain, so fill-in at 4 ms per frame would fall from about 3 s to about 1.1 s.
- The World belongs to the Step thread while a batch runs (`SimulationThread`). A worker therefore
  needs each owner's inputs copied out on the main thread between batches, plus epoch rejection.
- Garden dressing needs no World access once the layout exists, so it is the first candidate if
  workers are justified.

### 8. Write path and canonical order

- `InstanceBuffer` gains owner-scoped writes. `Write(owner, values, ids)` replaces one owner's
  instances, and `Remove(owner)` drops them. Entries keep their `(id, part)` keys, and each records
  its owner's order key and its local index.
- `DrawList` prints instances sorted by `(owner order key, local index)` and numbers them in that
  order. Today's instance index is exactly that order, so the printed list stays byte-identical
  even though runtime order then depends on camera history.
- The order key per component:
  - Block: `(column, row)`.
  - Segment: its slot.
  - Cell: distance from `CityAnchor`, then north, then east, computed when the list is printed.
- Picking and the parallel `ids` lists read `IdAt` instead of a list kept in instance order.

## Contract

```csharp
/// One drawn component. Plans are pure: equal inputs give equal pieces in equal order.
internal interface IPrepared<TOwner, TInput, TPlan> where TOwner : unmanaged
{
    Residency Class { get; }                          // ReachGated, Global or FarStandIn
    IReadOnlyList<InstanceLayer> Layers { get; }

    void Owners(Vector2I square, List<TOwner> into);  // From an owner index kept current by Touched
    Rect2 Bounds(TOwner owner);                       // Every piece's ground bounds, with its layer's extent added
    OrderKey Order(TOwner owner);                     // The whole-city walk's order

    TInput Capture(TOwner owner);                     // Main thread, between Step batches
    TPlan Plan(in TInput input);                      // Pure; may run on a worker
    void Write(TOwner owner, in TPlan plan, OwnerWriter into); // Filters each piece through Reaches

    void Touched(in ShellChanges changes, ISet<TOwner> into);
}

/// Shared by every component: squares, owner states and epochs, the queue, the budget and eviction.
internal sealed class Preparation
{
    public void Register<TOwner, TInput, TPlan>(IPrepared<TOwner, TInput, TPlan> component);
    public void Invalidate(in ShellChanges changes);  // From UpdateBuildings, in place of whole-city passes
    public void Regate(Vector3 eye);                  // Queues squares entering reach, evicts squares leaving it
    public void Spend(TimeSpan allowance);            // Once per frame, before FlushInstances
    public void Drain();                              // draw and shoot; tests
    public PreparationStats Stats { get; }            // Owners, plans, bytes and milliseconds per component
}
```

`ShellChanges` holds the Building, Lot and Segment slots from `WorldChanges`, the wash change and
`Full`.

## Migration order

Each step lands alone. Each must pass the equivalence checks below before the next step starts.

| Step | Change | Expected effect on the 1M save |
|---|---|---|
| 1 | Owner-scoped writes and canonical draw order in `InstanceBuffer`; `DrawList` prints canonical order | None. Byte-identical draw lists prove the canonical order. |
| 2 | `Preparation` with squares, queue, eviction, the frame budget and `render_prepare` rows; kerbs become its first reach-gated owners | Removes the 300 ms of kerb planning from the descent frame. Bounds the kerb cache. |
| 3 | Wild trees and rocks plan per Cell, with a per-Building footprint index | A pan plans only entering Cells and stops walking 204,557 footprints |
| 4 | Gardens split into a global layout and reach-gated dressing, with the fence owner rule | Gated garden pieces are held only near the camera. Garden rebuilds rewrite only straddling blocks. |
| 5 | Lot and Segment journals; Commands stop forcing `Full`; a wash change recolors only; roads, footways, plots and zones become per-owner global components | A Command touches its owners instead of a 1.5 s full pass. A Street avoids the whole 0.65 s `Pave`. |
| 6 | D1: a far stand-in for garden ground and hedges, once the user picks one from rendered options | Up to about 0.7 s off the opening full pass: garden dressing (about 0.48 s) and the ground and hedge fill (0.21 s), less the stand-in's own cost |
| 7 | D3: workers for garden dressing, then kerbs, if step 4's measurement justifies them | Shorter fill-in after jumps |

## Equivalence checks

| Check | How | Pass |
|---|---|---|
| Fixed cameras, 1M save | The drive script above, old and new build, `traveller` and `car` rows removed | Byte-identical at all four cameras |
| Smaller city | 25,000 Citizens, `focus 120 120 900` (0066's check) | Byte-identical, same exclusions |
| Cross-block fences | Same draws. The 1M save holds 4,132 shared keys. | Covered by the byte-identical draws |
| Running city | Speed 8 from Tick 257 to 600 at 700 m, then `draw` | Byte-identical |
| Edits | A driven Zone, Street and Demolish sequence on a 25,000-Citizen `shopping.toml` city, with `draw` before and after each | Byte-identical. This check comes into force at step 5. |
| In-process | `BOROUGH_RENDER_VERIFY=1` re-plans each migrated component for the whole city after each preparation pass and compares keyed instances and order, as `VerifyBuildingDrawing` does | No difference |
| Cost | `render_prepare` and `render_regate` rows on the optimized shell, load below 2, three runs | Reported with conditions, against the tables above |

### Step 1 result, 2026-10-22

- Optimized shell, i5-10400, load 2.65–3.25. These runs check equality only, so no timing is recorded.
- The 1M save at all four cameras and the running city at Tick 600 match the pre-change draw lists
  byte for byte, `traveller` and `car` rows removed.
- The 25,000-Citizen city at `focus 120 120 900` matches a build of `f049ab4e` byte for byte over
  3,746,663 lines.
- No layer writes by owner yet, so these runs prove the refactored whole-layer and `Replace` paths.
  The renderer checks (`scripts/test-renderer.sh`) cover owner writes, canonical order and the refusal
  to mix the two write modes.

### Step 2 result, 2026-10-22

- `ScenePreparation` (`src/Borough.Godot/ScenePreparation.cs`) holds squares, owner states, the
  queue, the 4 ms frame allowance, evict reach and the 64 MB plan cap. Kerbs are its only component.
  The name avoids `Main._preparation`, which is the city load's `CityPreparation`.
- A component registers a name, a layer, an owner lister and a plan function. A plan is the owner's
  pieces in walk order. `Capture` and the input type wait for workers (D3), and a reset of the whole
  component stands in for `Touched` until the journals of step 5.
- `Pave` resets the kerb component. Vanished Segments lose their instances at once; the rest keep
  theirs until rewritten.
- `draw` and `shoot` drain the queue. `BOROUGH_RENDER_VERIFY=1` re-plans every reached owner after
  each drain and compares ids, transforms and colors in canonical order.
- Optimized shell, i5-10400, load 4.8–21 with two shells running. These runs check equality only,
  so no timing is recorded.
- Against a build of step 1 (`7eb91a09`), `traveller` and `car` rows removed, draw lists match byte
  for byte at all four cameras on the 1M save, for the running city at Tick 600, and for the
  25,000-Citizen `shopping.toml` city at `focus 120 120 900`.
- Counts on the 1M save: 52,454 kerb owners; 19,097 planned on the descent; 5,089 newly planned on
  the pan, matching the measured figure; plans hold 9.9–10.7 MB.
- The verify run on the 25,000-Citizen city panned four times with no difference. That city has
  1,227,001 kerb owners.
- The renderer checks cover reach, canonical order, owners kept whole across a regate, the
  one-owner minimum per frame, eviction and reset.

### Step 3 result, 2026-10-24

- Trees and rocks are two `ScenePreparation` components. Each Cell is one owner. A plan holds the
  Cell's trees or its rocks in the whole-city walk's order.
- The owner list holds only Cells that could ever draw. A tree Cell needs a seeded woodland count
  (`Potential`), which woodland never grows past. A rock Cell needs rocky terrain, which is fixed at
  world creation.
- The footprint index is kept by Cell and updated per footprint as Massing records it. Each indexed
  Cell carries a stamp that changes with its footprints.
- A Cell's inputs are its woodland count, terrain, water and footprint stamp. A regate re-plans each
  reached Cell whose inputs changed since its plan.
- Inputs shared by every Cell reset both components: the World, the scatter window, the paving, Lots
  per Segment and the count of `Pave` runs. `Pave` now scatters, so a new Street clears its crowns at
  once. The whole-city scatter kept them until the next camera rebuild or footprint change.
- `InstanceBuffer.SortOwners` puts Cells in draw order: distance from `CityAnchor`, then north, then
  east. It runs when a list is printed, so a moved anchor re-plans nothing.
- The per-piece reach test keeps the whole-city scatter's ground point (`ScatterReaches`).
- Optimized shell, i5-10400, load 1.1 at the start, four shells running together. These runs check
  equality only, so no timing is recorded.
- Against step 2 (`d8cb4de4`), `traveller` and `car` rows removed, draw lists match byte for byte
  at all four cameras on the 1M save, for the running city at Tick 600, and for the 25,000-Citizen
  `shopping.toml` city at `focus 120 120 900`.
- Counts on the 1M save: 220,160 tree owners and 38,961 rock owners. The descent planned 19,104 tree
  Cells. The pan planned 5,031 more, where the whole-city scatter re-planned 21,600–25,344. Tree
  plans hold 3.2–4.4 MB.
- The verify run on the 25,000-Citizen city, with four camera moves and a Street, found no
  difference after `Pave` began to scatter. Before that change it caught a Cell planned on the old
  lattice.
- The renderer checks cover an owner order set on the layer, re-planning only the owner whose
  inputs changed, and `Refresh`.

### Step 4 result, 2026-10-25

- Each block keeps a layout (`GardenBlock`) for the whole city: garden areas, the green, ground
  rectangles, attempted boundary keys, the keys earlier blocks claim, and a stamp. A block whose land
  and Lots are unchanged keeps its layout. The layout draws no garden stream.
- Garden ground and hedges are dressed for every block on each garden pass (D1).
- The ten gated layers are `ScenePreparation` components: garden trees, seven modules, fence panels
  and railing panels. The owner is the block, keyed `column << 32 | row`, so default owner order is
  walk order. One dressing serves a block's ten components through a one-block cache.
- Fence and railing panels are plan pieces. `PanelRun` is gone.
- A component's inputs are the block's stamp. A garden pass resets the components when the list of
  block owners or their bounds changes, and otherwise refreshes them.
- The running-city check exposed the residency defect described under Lifecycle. The renderer
  checks now cover it.
- Against step 3 (`7b578ca7`), `traveller` and `car` rows removed, draw lists match byte for byte
  at all four cameras on the 1M save, for the running city at Tick 600, and for the 25,000-Citizen
  city at `focus 120 120 900`. The verify run on that city, with four camera moves and a Street,
  found no difference.
- Counts on the 1M save: 10,868 block owners among 12,499 laid-out blocks. The descent planned
  4,894 blocks for garden trees and 1,414 for props. Plans at the descent: gardens 34.2 MB, all
  components 47.3 MB. At 300 m: gardens 41.3 MB, all components 56.3 MB, against the 64 MB cap.
- Optimized shell, i5-10400, load 2.9–3.9. This is a busy machine, so no timing is recorded. D3
  still needs a quiet-machine measurement.

### Step 5, slice 1 result, 2026-10-26

- Roads and footways are written by owner, one owner per Segment slot. Owner order is slot order,
  which is the whole-city walk's order.
- A full `Pave` runs only at load and on a rebuild. A Street, Connect or Street demolition runs
  `Repave` on the Segment journal instead. It rewrites the changed Segments and every Segment that
  meets one at a Node, before or after the change. It relists their kerb owners and discards
  their kerb plans.
- `Repave` still retakes the whole Address snapshot. Kerbs re-plan each Segment whose run of
  Addresses changed.
- The scatter basis holds the `BlockLattice` in place of the count of `Pave` runs. The lattice is
  re-laid only on Ruleset adoption. A road edit discards the tree and rock plans of the Cells within
  `StreetGrid.SpanLimit` + 1 blocks of the Segment's ends, before and after.
- `ScenePreparation.Touch` discards one owner's plan. `Relist` moves, adds or removes one owner.
  The renderer checks cover both.
- Commands still force a full Building pass (slice 3). The journals now record while `Full` is set,
  so `Repave` sees the Segments a Command changed.
- Against `41a3c23c`, `traveller` and `car` rows removed, the 25,000-Citizen city matches byte for
  byte at `focus 120 120 900` and through the verify script's Street. A second script lays a Street,
  removes it, then removes two generated Streets, drawing after each. All four draws match, and the
  `BOROUGH_RENDER_VERIFY=1` run found no difference.
- On the 1M save, a full `Pave` took 883 ms and a one-Segment `Repave` took 51 ms cold and 18 ms
  warm. Optimized shell, i5-10400, load 4.9 with other shells running, one run. These figures show
  the scale of the change, not a quiet-machine timing. Most of the `Repave` time is the Address
  snapshot.

## Decisions settled, 2026-10-22

The user accepted each recommendation.

| Id | Question | Options and cost | Settled |
|---|---|---|---|
| D1 | Garden ground and hedges at any distance | Keep them global: no visual change, but the opening frame still plans every block (about 0.65 s) and fills 432,562 pieces (0.21 s). Or gate them with a far stand-in: saves up to about 0.7 s, changes the picture beyond the chosen distance, and needs rendered options first. | Global through step 5. Rendered stand-in options go to the user before step 6. |
| D2 | Write path | Owner-scoped writes with canonical draw order: no rebuild spikes, plus a `DrawList` change. Whole-layer rewrites: simpler, but every rebuild keeps its 100–300 ms spike. | Owner-scoped writes |
| D3 | Workers | Defer: fill-in after the descent takes about 3 s at 4 ms per frame. Add: about 1.1 s, at the cost of input capture and epoch rejection. | Deferred until step 4 is measured |
| D4 | Frame allowance | 4 ms (fill-in about 3 s) or 8 ms (about 1.5 s, a heavier frame) | 4 ms, provisional |
| D5 | Lot and Segment journals in `Borough.Core` | Required for per-owner invalidation and for Commands to stop forcing full passes. Unsaved and unhashed. | Added in step 5 |

## Outside this plan

- The 231–233 ms first-frame upload and the 8 MB transfer allowance stay with 0066 and issue #217.
- Far Building aggregation stays with the board row *Rendering detail across zoom levels*.
