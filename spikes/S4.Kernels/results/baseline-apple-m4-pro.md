## S4 baseline — `apple-m4-pro`

Recorded 2026-08-03 00:42 UTC. Window 10s. GB means 1e9 bytes. Copy rate is bytes delivered; traffic counts the read as well.

### Machine

| Field | Value |
|---|---|
| CPU | Apple M4 Pro |
| Topology | 10 performance cores + 4 efficiency cores, 14 logical |
| L1d (performance) | 128 KiB per core |
| L2 (performance) | 16 MiB, shared by the cluster |
| L1d (efficiency) | 64 KiB per core |
| L2 (efficiency) | 4 MiB, shared by the cluster |
| Cache line | 128 B |
| RAM total | 24.0 GiB unified |
| RAM configuration | unified memory on package — no DIMMs, no channel count to derive a ceiling from |
| Theoretical DRAM peak | not derivable — see the report |
| OS | macOS 26.5.2 |
| Kernel | macOS 26.5.2 |
| SDK / runtime | .NET 10.0.2, Arm64 |
| GC mode | server=False, concurrent=True, latency=Interactive |

### Frequency and scheduler state

| Field | Value |
|---|---|
| Frequency control | not exposed by macOS — no governor, no turbo switch, no per-core clock |
| Core placement | not controllable — threads cannot be pinned on Apple Silicon |
| Process CPU affinity | not applicable |

### Copy rate by working-set size (burst)

Two buffers of the stated size. Best-of-64 and median, single-threaded.

| Buffer | Best copy | Median copy | Median traffic |
|---|---|---|---|
| 16 KiB | 90.2 GB/s | 89.9 GB/s | 179.8 GB/s |
| 64 KiB | 82.0 GB/s | 69.7 GB/s | 139.3 GB/s |
| 512 KiB | 54.3 GB/s | 52.9 GB/s | 105.9 GB/s |
| 4 MiB | 57.5 GB/s | 56.1 GB/s | 112.2 GB/s |
| 32 MiB | 64.7 GB/s | 60.5 GB/s | 120.9 GB/s |
| 256 MiB | 64.1 GB/s | 63.3 GB/s | 126.6 GB/s |

### Sustained single-threaded `memcpy` — the denominator

256 MiB per copy, 2295 copies.

| Statistic | Copy rate | Traffic |
|---|---|---|
| best | 64.7 GB/s | 129.3 GB/s |
| **median** | **63.3 GB/s** | **126.5 GB/s** |
| p95 (slow tail) | 55.8 GB/s | 111.5 GB/s |
| worst | 32.2 GB/s | 64.4 GB/s |

Clock during the window: not exposed by this OS, so drift cannot be ruled out.

### Sustained single-threaded read (secondary)

256 MiB scanned 740 times: median **66.5 GB/s**, best 72.0 GB/s, p95 62.6 GB/s. Read-only, so copy rate and traffic are the same figure.
