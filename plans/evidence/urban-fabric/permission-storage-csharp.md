# Geographic permission storage: C# evidence

2026-09-18, `urban-permissions` worktree based on `main` at `e12c269`.
Intel Core i5-10400, 12 logical CPUs, Linux 6.8.0-139-generic x86_64,
.NET 10.0.12, Release. Measurements use one calling thread. Other test processes were
running: these are allocation measurements, not quiet-machine timings or a process RSS claim.

Reproduce the measurements:

```sh
scripts/test.sh --filter 'FullyQualifiedName~LandPermissionTests.Measure_actual|FullyQualifiedName~LandPermissionTests.Oversized' -- -m:1 --no-restore --logger 'console;verbosity=detailed'
```

The recorded output was `/tmp/borough-test-20260918-140159.log`; the results below preserve its
measurements without depending on that temporary file. `GC.GetAllocatedBytesForCurrentThread`
measures actual managed allocations. Construction is warmed before measurement. The full-budget
case constructs a table and directory, then reserves 1,048,576 slots through the production
capacity-preparation API. It has no Citizens, roads or Buildings and does not claim a representative
painted city's occupancy. The oversized-paint case starts with one painted Tile and a record limit
of eight, then attempts a different permission over the whole 16,384 × 16,384 Tile map.

| Measurement | Result |
|---|---:|
| Saved bytes per slot (allocator plus geometry and permission) | 40 |
| Derived bytes per slot (one intrusive page link) | 4 |
| Total column payload per allocated slot | **44 bytes** |
| Column payload at 1,048,576 slots | 46,137,344 bytes = **44 MiB** |
| Cell directory payload | 1,048,576 bytes = **1 MiB** |
| Measured full-budget construction and reservation, including headers, bookkeeping and initial small arrays | **47,188,128 bytes** |
| Measured oversized whole-map paint, refused before staging | **1,048,600 bytes** |

The original [projection](permission-storage.md) allowed 48 bytes per slot plus a 1 MiB directory:
49 MiB before overhead. The implemented payload is 45 MiB; the measured construction/reservation
allocates another 2,208 bytes, including the initial eight-slot arrays subsequently replaced by
growth. One derived link suffices because page membership is recovered from saved coordinates;
there is no second per-row index column. The save carries neither the directory nor the links.

These figures do **not** include other World tables, save snapshots, paint staging, or all garbage
awaiting collection. The maximum accepted paint stages 24 bytes per resulting changed-page record:
at the default limit, at most **24 MiB**, plus array overhead. Counts cost at most **1 MiB** across
262,144 pages. Per-call stack scratch is **36.25 KiB** of array payload: an 8 KiB dense page, 24 KiB
encoded rectangles, 4 KiB allocation-order slots, and two 128-byte run-index arrays. Retired row
ids need no additional array: the old page lists remain available until retirement. During column
growth, old arrays and replacements coexist; this is additional transient memory, not covered by
the 45 MiB payload. Clearing retains capacity for reuse. A lower Ruleset limit is checked against
saved slot count; already allocated headroom is not shrunk by reloading.

The whole-map refusal allocated only its page-count array. It preserved the table hash (including
free-list/generation/id state) and the eight-slot capacity. Allocation failure remains a process
failure; the transactional promise covers expected bounds and record-limit refusals.

Behavioral checks cover:

- exact cross-page painting and unaffected Tile values against a dense oracle;
- split/coalesce, form-only updates, unzoned gaps, use/form refusal and mixed intensity;
- absent versus explicitly empty form restrictions, including restrictions on unzoned ground;
- exact-limit acceptance, capped non-power-of-two growth, no-op identity and multi-page refusal;
- later-page compaction offsetting earlier-page growth before the budget decision;
- three checkerboard/uniform/clear cycles: 1,024 live records at fragmentation, one after
  simplification, zero after clearing, with 1,024 allocated/high-water slots throughout later cycles;
- save/load and subsequent identical painting producing the same complete World hash and ids;
- reload/load refusal below saved high-water, invalid saved geometry/packing, and rebuilt links;
- nonempty permission state in both the derived rebuild and save-column corruption audits.

Core save version 7 refuses earlier saves. The new table intentionally moves all three golden
hash outputs even when empty. Baseline Ruleset hashes and the hash algorithm stay unchanged.
Gameplay paint/readers, local assembly, automatic housing selection and shell integration are
outside this storage measurement and remain subsequent slices.

Validation: the working lane passed **3,766 tests** with
`scripts/test.sh -- -m:1 --no-restore` (log `/tmp/borough-test-20260918-140427.log`), including
persistence/Factorio coverage, replay and the existing long-run assertions. The two allocation
instruments passed separately. `scripts/format.sh --check -- --no-restore` and
`npx --yes @taplo/cli lint 'rulesets/*.toml'` passed. The entire instrument suite was not run.
