## S4 scaling curve — `apple-m4-pro`

Recorded 2026-08-03 00:44 UTC. Window 5s. GB means 1e9 bytes. Copy rate is bytes delivered; traffic counts the read as well.

### Aggregate bandwidth against thread count

64 MiB per buffer per thread, private to that thread. Copy traffic counts the read as well as the write; read traffic is the figure itself. Per-core is the share one thread gets, which is what a Tick phase spread across threads actually receives.

**Threads are not pinned on this platform.** Placement is the scheduler's, cores are heterogeneous, and a thread that lands on an efficiency core is not measuring the same machine as one that lands on a performance core. Read the shape of this curve, not its points. 162 threads were refused even the user-interactive quality-of-service hint.

| Threads | Placement | Copy aggregate | Copy traffic | Copy per-core | vs 1 thread | Read aggregate | Read per-core | vs 1 thread |
|---|---|---|---|---|---|---|---|---|
| 1 | 1 threads, OS-placed, 10 performance cores available | 64.5 GB/s | 129.0 GB/s | 64.5 GB/s | 1.00x | 67.1 GB/s | 67.1 GB/s | 1.00x |
| 2 | 2 threads, OS-placed, 10 performance cores available | 101.8 GB/s | 203.5 GB/s | 50.9 GB/s | 1.58x | 131.1 GB/s | 65.6 GB/s | 1.95x |
| 3 | 3 threads, OS-placed, 10 performance cores available | 112.1 GB/s | 224.2 GB/s | 37.4 GB/s | 1.74x | 157.0 GB/s | 52.3 GB/s | 2.34x |
| 4 | 4 threads, OS-placed, 10 performance cores available | 119.6 GB/s | 239.2 GB/s | 29.9 GB/s | 1.85x | 210.2 GB/s | 52.6 GB/s | 3.13x |
| 5 | 5 threads, OS-placed, 10 performance cores available | 119.7 GB/s | 239.4 GB/s | 23.9 GB/s | 1.86x | 247.6 GB/s | 49.5 GB/s | 3.69x |
| 6 | 6 threads, OS-placed, 10 performance cores available | 119.7 GB/s | 239.5 GB/s | 20.0 GB/s | 1.86x | 250.3 GB/s | 41.7 GB/s | 3.73x |
| 7 | 7 threads, OS-placed, 10 performance cores available | 119.5 GB/s | 239.0 GB/s | 17.1 GB/s | 1.85x | 249.9 GB/s | 35.7 GB/s | 3.72x |
| 8 | 8 threads, OS-placed, 10 performance cores available | 119.4 GB/s | 238.9 GB/s | 14.9 GB/s | 1.85x | 248.4 GB/s | 31.0 GB/s | 3.70x |
| 9 | 9 threads, OS-placed, 10 performance cores available | 119.2 GB/s | 238.5 GB/s | 13.2 GB/s | 1.85x | 247.4 GB/s | 27.5 GB/s | 3.69x |
| 10 | 10 threads, OS-placed, 10 performance cores available | 119.3 GB/s | 238.6 GB/s | 11.9 GB/s | 1.85x | 247.1 GB/s | 24.7 GB/s | 3.68x |
| 12 | 12 threads, OS-placed, past the 10 performance cores | 117.3 GB/s | 234.6 GB/s | 9.8 GB/s | 1.82x | 252.1 GB/s | 21.0 GB/s | 3.76x |
| 14 | 14 threads, OS-placed, past the 10 performance cores | 116.6 GB/s | 233.2 GB/s | 8.3 GB/s | 1.81x | 253.9 GB/s | 18.1 GB/s | 3.78x |
