## S4 baseline — `ddr2133-powersave-noturbo`

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
| Governor | powersave |
| Turbo | disabled (intel_pstate/no_turbo=1) |
| Energy/performance preference | balance_performance |
| Frequency range | 800 - 2900 MHz |
| SMT | on |
| Transparent huge pages | always [madvise] never |
| Process CPU affinity | 2 |

### Copy rate by working-set size (burst)

Two buffers of the stated size. Best-of-64 and median, single-threaded.

| Buffer | Best copy | Median copy | Median traffic |
|---|---|---|---|
| 16 KiB | 61.1 GB/s | 60.5 GB/s | 120.9 GB/s |
| 64 KiB | 40.8 GB/s | 39.5 GB/s | 78.9 GB/s |
| 512 KiB | 24.4 GB/s | 24.1 GB/s | 48.3 GB/s |
| 4 MiB | 20.8 GB/s | 20.3 GB/s | 40.6 GB/s |
| 32 MiB | 13.8 GB/s | 13.1 GB/s | 26.3 GB/s |
| 256 MiB | 12.7 GB/s | 12.6 GB/s | 25.1 GB/s |

### Sustained single-threaded `memcpy` — the denominator

256 MiB per copy, 460 copies.

| Statistic | Copy rate | Traffic |
|---|---|---|
| best | 12.8 GB/s | 25.5 GB/s |
| **median** | **12.6 GB/s** | **25.2 GB/s** |
| p95 (slow tail) | 11.7 GB/s | 23.4 GB/s |
| worst | 6.9 GB/s | 13.8 GB/s |

Clock during the window: 2899 MHz at start, 2899 MHz mean, 2899 MHz at end.

### Sustained single-threaded read (secondary)

256 MiB scanned 147 times: median **13.2 GB/s**, best 14.2 GB/s, p95 12.5 GB/s. Read-only, so copy rate and traffic are the same figure.
