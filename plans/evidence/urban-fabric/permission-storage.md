# Geographic permission storage sizing

2026-09-18, source inspected at `ebab65ffb4e0aff6d2164b4e20f854c0e6f5c697`.
Run on the local x86_64 workspace with Python 3.12.3. This is a deterministic storage-shape model,
not a Core implementation, timing benchmark, measured C# heap profile or quiet-machine performance
claim. No simulation state, Ruleset or golden fixture was modified.

Reproduce:

```sh
python3 plans/evidence/urban-fabric/permission-storage.py
```

[Source](permission-storage.py) and [captured JSON](permission-storage-results.json).

## Model and conditions

The current world is 16,384 x 16,384 Tiles. A Cell is 32 x 32 Tiles, so the world has 262,144 Cells.
Use a Cell as a storage page, not a permission granularity: rectangles and queries remain exact to
individual Tiles, including paint that crosses Cell boundaries. Subdivision and Street geometry do
not determine the page grid or move permissions.

The model compares split-only rectangles, deterministic page normalisation, and the arithmetic
cost of dense Tile storage. Normalisation emits maximal equal-value horizontal runs and extends
identical runs vertically. It is deterministic and exact, not a minimum-rectangle optimiser.
Split-only is a diagnostic baseline demonstrating why a production implementation needs no-op
detection/compaction; it is not the previously proposed full split-and-coalesce implementation.

Every paint is checked against an independent dense Tile oracle, with non-overlap and encoding
idempotence checks. The 20,385 operations include repeated no-ops, stripes, four parcel-sized
areas, a two-permission checkerboard, deterministic random rectangles (seed 620018), repeated
clear/repaint cycles and repeated full fragmentation/restoration. Python randomness generates
evidence workloads only; it is not a proposed simulation random source.

Projected rectangle storage is 48 bytes per allocated slot: four int32 coordinates/extents (16),
one packed uint64 permission (8), existing Rows id/generation/free-link columns (16), and two
derived int32 index columns (8). A packing can hold the existing 16-bit use mask, 8-bit band,
an explicit restriction-presence flag and a 16-bit form mask. This is a proposed schema budget,
not `sizeof` measured on an implemented permission table. Array headers, allocator bookkeeping,
other World tables and transient paint staging are excluded and discussed separately below.

## Results

| Workload | Peak normalised records | Final normalised records | Peak split-only records | Modelled allocated slots |
|---|---:|---:|---:|---:|
| 1,000 identical full-page paints | 1 | 1 | 1 | 8 |
| Four parcel-sized painted areas | 4 | 4 | 4 | 8 |
| Alternating one-Tile stripes | 32 | 32 | 32 | 32 |
| Alternating Tile checkerboard | 1,024 | 1,024 | 1,024 | 1,024 |
| 2,000 random rectangular paints | 92 | 69 | 122 | 128 |
| Same permission repainted one Tile at a time | 1 | 1 | 1,024 | 8 |
| 1,000 parcel-paint/clear cycles | 4 | 0 | 4 | 8 |
| 10 checkerboard/uniform cycles | 1,024 | 1 | 1,024 | 1,024 |

The model follows Rows-style doubling and reuse, with a minimum of eight slots. Ten versus 1,000
parcel/clear cycles keep the same eight-slot capacity. Clearing records does not shrink the
high-water allocation; a fragmented page returned to uniform still retains its modelled peak of
1,024 slots. Production reuse and saved allocator identity need their own tests.

Memory projections under the proposed schema:

- A fully dense whole-world uint64 permission grid: **2 GiB payload**, even before table overhead.
- Whole-world one-rectangle-per-Tile storage: **12 GiB** including the modelled row/index columns.
- Dense payload for one allocated page: **8 KiB**. Normalised rectangles win for simple pages;
  dense pages win for severe fragmentation. A hybrid remains a possible later optimisation, but
  this model does not implement its save schema or switching machinery.
- A derived int32 directory covering every Cell: **1 MiB**.
- An illustrative 10,000 painted Cells with four rectangles each: 40,000 live records, **3 MiB**
  after rounding global record capacity to 65,536. This is a scaling projection, not a measured city.

## Initial bound and preflight design

Use **1,048,576 records** as the provisional world-wide permission-table limit. Under the proposed
48-byte budget this bounds allocated record columns at **48 MiB**, plus the 1 MiB directory.
It allows four records in every world Cell, or more detailed painting in a smaller area. It also
allows 1,024 completely checkerboard-painted Cells. Those are capacity illustrations, not promises
about acceptable painting latency. The limit is an engineering budget, not a measured hardware
maximum; verify it against the actual C# schema and representative saved cities when implemented.

Because this limit affects accepted Input and therefore behaviour, make it explicit in the Ruleset
contract and retain it with the world's Ruleset; the final key name belongs to schema implementation.
Do not silently change it with available RAM or table growth. Reject lowering it below the existing
saved slot high-water mark in the initial release. `Rows.Grow` currently doubles without a hard
limit, so a permission-specific guard and a non-mutating capacity-preparation API are required.

Before changing a multi-Cell paint:

1. Decode and normalise each affected page in scratch, preserving unedited permission fields.
   Compare effective old/new values; a no-op must allocate no saved ids or alter the hash.
2. Count the final world total using all affected pages. Do not reject because an early page grows
   if a later page in the same paint frees enough records. Refuse if the complete result exceeds
   the limit; preserve every old permission and the saved allocator state.
3. Stage the changed-page output and prepare record-column capacity before mutation. Free changed
   pages' old records before allocating their replacements so old-plus-new live rows never become
   the storage requirement. Commit in stable page/rectangle order, rebuild the derived page links,
   and publish only after the entire paint is complete. No concurrent simulation reader observes
   the intermediate table. Expected refusal has no partial effect.

Bound temporary memory separately: one dense page is 8 KiB and its maximum normalised output is
24 KiB of geometry/permission payload. Staging up to the full record limit costs at most **24 MiB**
of that payload; an int32 list of replaced rows can add **4 MiB**, and page directory/flags and
array overhead are additional. Thus **48 MiB is not the peak process or operation memory**.
Use a count pass before staging the entire result so a rejected giant paint cannot allocate an
unbounded intermediate result. Runtime allocation failure remains a process-level failure, not
a promised transactional recovery from out-of-memory.

The prototype verifies exact-limit acceptance, over-limit multi-page refusal returning the original
state, and successful compaction with a small test limit. It does not exercise Rows generations,
golden hashes, save/load or actual command batching; those belong to Core tests during implementation.

The player-facing consequence at the bound is a clear refusal to add that paint's complexity, with
the old paint intact. Clearing or simplifying permissions remains possible when its final record
count fits. Never merge unlike restrictions to make an operation fit, and never partially paint
only the early Cells. A dense-page fallback can be reconsidered if real usage approaches this bound.
