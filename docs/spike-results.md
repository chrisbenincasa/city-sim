# Spike results

> Recorded numbers from the spikes. Plans in [`plans/0003-build-plan.md`](../plans/0003-build-plan.md).

[`06-roadmap.md`](06-roadmap.md) is explicit about why this file exists: *the spikes are worthless if
their results are held in the developer's head, because the whole value is being able to re-read them
in a year when a performance question resurfaces.* **Record them; delete the code.**

Each entry states the machine, the numbers, and — separately from the numbers — **the decision they
produced**. A spike that records data and no verdict has not finished.

| Spike | Question | Status |
|---|---|---|
| **S4** | Kernel benchmark — the machine's response to the shapes this design makes | not run. [`plans/0004`](../plans/0004-s4-kernel-benchmark.md) |
| **S2** | Routing ceiling — travel-time matrix, then HPA\* versus DSDV distance-vector. Also owns Chunk size | not run. **The project's top risk** |
| **S1** | Rendering ceiling — 20k Buildings via chunked `MultiMeshInstance3D` | not run |
| **S3** | UI ceiling — one data panel with a live multi-series graph, and how long it took | not run |
| **S0** | Synthetic 1M-Citizen city in `Borough.Headless` | not run. Gated on the Phase 1 slices |

---

## S4 — the kernel benchmark

*Not yet run.* When it is, this section records: the machine description; measured single-threaded
`memcpy` bandwidth; the recomputed Citizen hot-row size and the derived target row counts; per kernel
K0–K6 the hand-computed ideal, the achieved figure and the ratio; K6's p99.9 under each GC
configuration; the verdict against the tripwire in
[`plans/0004`](../plans/0004-s4-kernel-benchmark.md); and the commit at which `spikes/S4.Kernels/`
was deleted.
