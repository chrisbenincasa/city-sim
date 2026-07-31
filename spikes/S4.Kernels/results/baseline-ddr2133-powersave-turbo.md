## S4 baseline — `ddr2133-powersave-turbo`

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
| Turbo | enabled (intel_pstate/no_turbo=0) |
| Energy/performance preference | balance_performance |
| Frequency range | 800 - 4300 MHz |
| SMT | on |
| Transparent huge pages | always [madvise] never |
| Process CPU affinity | 2 |

### Copy rate by working-set size (burst)

Two buffers of the stated size. Best-of-64 and median, single-threaded.

| Buffer | Best copy | Median copy | Median traffic |
|---|---|---|---|
| 16 KiB | 87.4 GB/s | 86.6 GB/s | 173.3 GB/s |
| 64 KiB | 54.9 GB/s | 52.9 GB/s | 105.8 GB/s |
| 512 KiB | 34.5 GB/s | 34.2 GB/s | 68.4 GB/s |
| 4 MiB | 29.7 GB/s | 28.1 GB/s | 56.1 GB/s |
| 32 MiB | 14.6 GB/s | 14.1 GB/s | 28.3 GB/s |
| 256 MiB | 12.5 GB/s | 12.3 GB/s | 24.7 GB/s |

### Sustained single-threaded `memcpy` — the denominator

256 MiB per copy, 461 copies.

| Statistic | Copy rate | Traffic |
|---|---|---|
| best | 13.1 GB/s | 26.1 GB/s |
| **median** | **12.9 GB/s** | **25.8 GB/s** |
| p95 (slow tail) | 9.9 GB/s | 19.7 GB/s |
| worst | 5.9 GB/s | 11.7 GB/s |

Clock during the window: 4099 MHz at start, 4083 MHz mean, 4100 MHz at end.

### Sustained single-threaded read (secondary)

256 MiB scanned 173 times: median **15.6 GB/s**, best 16.4 GB/s, p95 14.5 GB/s. Read-only, so copy rate and traffic are the same figure.
