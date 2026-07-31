## S4 scaling curve — `apple-m4-pro`

Recorded 2026-07-31 13:08 UTC. Window 5s. GB means 1e9 bytes. Copy rate is bytes delivered; traffic counts the read as well.

### Aggregate bandwidth against thread count

64 MiB per buffer per thread, private to that thread. Copy traffic counts the read as well as the write; read traffic is the figure itself. Per-core is the share one thread gets, which is what a Tick phase spread across threads actually receives.

**Threads are not pinned on this platform.** Placement is the scheduler's, cores are heterogeneous, and a thread that lands on an efficiency core is not measuring the same machine as one that lands on a performance core. Read the shape of this curve, not its points. 162 threads were refused even the user-interactive quality-of-service hint.

| Threads | Placement | Copy aggregate | Copy traffic | Copy per-core | vs 1 thread | Read aggregate | Read per-core | vs 1 thread |
|---|---|---|---|---|---|---|---|---|
| 1 | 1 threads, OS-placed, 10 performance cores available | 63.5 GB/s | 126.9 GB/s | 63.5 GB/s | 1.00x | 67.1 GB/s | 67.1 GB/s | 1.00x |
| 2 | 2 threads, OS-placed, 10 performance cores available | 99.8 GB/s | 199.6 GB/s | 49.9 GB/s | 1.57x | 129.1 GB/s | 64.6 GB/s | 1.92x |
| 3 | 3 threads, OS-placed, 10 performance cores available | 110.9 GB/s | 221.8 GB/s | 37.0 GB/s | 1.75x | 155.7 GB/s | 51.9 GB/s | 2.32x |
| 4 | 4 threads, OS-placed, 10 performance cores available | 118.3 GB/s | 236.7 GB/s | 29.6 GB/s | 1.87x | 239.8 GB/s | 60.0 GB/s | 3.57x |
| 5 | 5 threads, OS-placed, 10 performance cores available | 118.8 GB/s | 237.7 GB/s | 23.8 GB/s | 1.87x | 245.3 GB/s | 49.1 GB/s | 3.65x |
| 6 | 6 threads, OS-placed, 10 performance cores available | 118.3 GB/s | 236.6 GB/s | 19.7 GB/s | 1.86x | 248.8 GB/s | 41.5 GB/s | 3.71x |
| 7 | 7 threads, OS-placed, 10 performance cores available | 117.9 GB/s | 235.8 GB/s | 16.8 GB/s | 1.86x | 247.7 GB/s | 35.4 GB/s | 3.69x |
| 8 | 8 threads, OS-placed, 10 performance cores available | 118.5 GB/s | 237.0 GB/s | 14.8 GB/s | 1.87x | 245.3 GB/s | 30.7 GB/s | 3.65x |
| 9 | 9 threads, OS-placed, 10 performance cores available | 118.4 GB/s | 236.7 GB/s | 13.2 GB/s | 1.87x | 245.7 GB/s | 27.3 GB/s | 3.66x |
| 10 | 10 threads, OS-placed, 10 performance cores available | 118.1 GB/s | 236.3 GB/s | 11.8 GB/s | 1.86x | 245.3 GB/s | 24.5 GB/s | 3.65x |
| 12 | 12 threads, OS-placed, past the 10 performance cores | 116.4 GB/s | 232.7 GB/s | 9.7 GB/s | 1.83x | 251.8 GB/s | 21.0 GB/s | 3.75x |
| 14 | 14 threads, OS-placed, past the 10 performance cores | 115.7 GB/s | 231.4 GB/s | 8.3 GB/s | 1.82x | 249.8 GB/s | 17.8 GB/s | 3.72x |
