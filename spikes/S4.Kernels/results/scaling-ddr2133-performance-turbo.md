## S4 scaling curve — `ddr2133-performance-turbo`

Recorded 2026-07-31 12:26 UTC. Window 5s. GB means 1e9 bytes. Copy rate is bytes delivered; traffic counts the read as well.

### Aggregate bandwidth against thread count

64 MiB per buffer per thread, private to that thread. Copy traffic counts the read as well as the write; read traffic is the figure itself. Per-core is the share one thread gets, which is what a Tick phase spread across threads actually receives.

| Threads | Placement | Copy aggregate | Copy traffic | Copy per-core | vs 1 thread | Read aggregate | Read per-core | vs 1 thread |
|---|---|---|---|---|---|---|---|---|
| 1 | 1 of 6 physical cores, SMT siblings idle | 13.2 GB/s | 26.5 GB/s | 13.2 GB/s | 1.00x | 15.8 GB/s | 15.8 GB/s | 1.00x |
| 2 | 2 of 6 physical cores, SMT siblings idle | 13.7 GB/s | 27.4 GB/s | 6.8 GB/s | 1.03x | 22.1 GB/s | 11.1 GB/s | 1.40x |
| 3 | 3 of 6 physical cores, SMT siblings idle | 13.5 GB/s | 27.0 GB/s | 4.5 GB/s | 1.02x | 24.2 GB/s | 8.1 GB/s | 1.53x |
| 4 | 4 of 6 physical cores, SMT siblings idle | 12.9 GB/s | 25.9 GB/s | 3.2 GB/s | 0.98x | 26.0 GB/s | 6.5 GB/s | 1.65x |
| 5 | 5 of 6 physical cores, SMT siblings idle | 13.1 GB/s | 26.2 GB/s | 2.6 GB/s | 0.99x | 27.9 GB/s | 5.6 GB/s | 1.77x |
| 6 | 6 of 6 physical cores, SMT siblings idle | 12.9 GB/s | 25.8 GB/s | 2.2 GB/s | 0.97x | 28.9 GB/s | 4.8 GB/s | 1.83x |
| 8 | 6 physical cores + 2 SMT siblings | 12.7 GB/s | 25.5 GB/s | 1.6 GB/s | 0.96x | 28.8 GB/s | 3.6 GB/s | 1.82x |
| 10 | 6 physical cores + 4 SMT siblings | 12.6 GB/s | 25.3 GB/s | 1.3 GB/s | 0.95x | 28.0 GB/s | 2.8 GB/s | 1.77x |
| 12 | 6 physical cores + 6 SMT siblings | 12.5 GB/s | 25.1 GB/s | 1.0 GB/s | 0.95x | 27.9 GB/s | 2.3 GB/s | 1.76x |

Theoretical DRAM peak is 34.1 GB/s. Best copy traffic reached 27.4 GB/s (80% of peak); best read reached 28.9 GB/s (85% of peak).
