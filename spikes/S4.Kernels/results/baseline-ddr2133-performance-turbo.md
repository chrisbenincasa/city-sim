## S4 baseline — `ddr2133-performance-turbo`

Recorded 2026-07-31 12:03 UTC. Sustained window 10s. GB means 1e9 bytes. Copy rate is bytes delivered; traffic counts the read as well.

### Machine

| Field | Value |
|---|---|
| CPU | Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz |
| Topology | 6 physical cores, 12 hardware threads (cpu0 siblings: 0,6) |
| L1d | 32K, 8-way, 64 B lines, shared by cpus 0,6 |
| L1i | 32K, 8-way, 64 B lines, shared by cpus 0,6 |
| L2 | 256K, 4-way, 64 B lines, shared by cpus 0,6 |
| L3 | 12288K, 16-way, 64 B lines, shared by cpus 0-11 |
| RAM total | 62.6 GiB |
| RAM configuration | Controller0-ChannelA 32 GB CMK64GX4M2E3200C16 running at 2133 MT/s; Controller0-ChannelB 32 GB CMK64GX4M2E3200C16 running at 2133 MT/s |
| Theoretical DRAM peak | 34.1 GB/s (2 channels x 2133 MT/s x 8 B) |
| OS | Ubuntu 24.04.4 LTS |
| Kernel | 6.8.0-136-generic |
| SDK / runtime | .NET 10.0.10, X64 |
| GC mode | server=False, concurrent=True, latency=Interactive |

### Frequency and scheduler state

| Field | Value |
|---|---|
| Scaling driver | intel_pstate |
| Governor | performance |
| Turbo | enabled (intel_pstate/no_turbo=0) |
| Energy/performance preference | performance |
| Frequency range | 800 - 4300 MHz |
| SMT | on |
| Transparent huge pages | always [madvise] never |
| Process CPU affinity | 2 |

### Copy rate by working-set size (burst)

Two buffers of the stated size. Best-of-64 and median, single-threaded.

| Buffer | Best copy | Median copy | Median traffic |
|---|---|---|---|
| 16 KiB | 81.4 GB/s | 74.5 GB/s | 149.0 GB/s |
| 64 KiB | 54.1 GB/s | 52.9 GB/s | 105.8 GB/s |
| 512 KiB | 36.3 GB/s | 35.6 GB/s | 71.3 GB/s |
| 4 MiB | 30.8 GB/s | 29.7 GB/s | 59.5 GB/s |
| 32 MiB | 14.6 GB/s | 14.1 GB/s | 28.2 GB/s |
| 256 MiB | 13.3 GB/s | 13.0 GB/s | 26.0 GB/s |

### Sustained single-threaded `memcpy` — the denominator

256 MiB per copy, 485 copies.

| Statistic | Copy rate | Traffic |
|---|---|---|
| best | 13.5 GB/s | 27.0 GB/s |
| **median** | **13.3 GB/s** | **26.6 GB/s** |
| p95 (slow tail) | 12.1 GB/s | 24.1 GB/s |
| worst | 7.1 GB/s | 14.2 GB/s |

Clock during the window: 4099 MHz at start, 4089 MHz mean, 4100 MHz at end.

### Sustained single-threaded read (secondary)

256 MiB scanned 172 times: median **15.5 GB/s**, best 16.4 GB/s, p95 14.4 GB/s. Read-only, so copy rate and traffic are the same figure.
