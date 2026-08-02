# S4 K6 — the GC tail — `ddr2133-powersave-turbo`

Eight runs of 10 minutes: four GC configurations x two heap arms. Not pinned —
server GC wants every core, and pinning would measure a server GC never allowed to be one.


Recorded 2026-08-01 22:49 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=off concurrent=off

453,577 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.298 ms |
| p90 | 1.386 ms |
| p99 | 1.709 ms |
| **p99.9** | **2.549 ms** |
| p99.99 | 3.024 ms |
| max | 5.088 ms |

Collections: 1,846 gen0, 1,842 gen1, 1 gen2. Total GC pause **2126.6 ms** across the run, 0.354% of wall clock.

Churn 64 KiB per iteration — **52 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.549 ms is **6x inside** the 15.6 ms Tick budget. 0 of 453,577 iterations exceeded it.


Recorded 2026-08-01 22:59 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=off concurrent=off

437,876 iterations over 600s. Live: 172.3 MiB of native world plus 177.8 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.349 ms |
| p90 | 1.426 ms |
| p99 | 1.658 ms |
| **p99.9** | **2.462 ms** |
| p99.99 | 2.757 ms |
| max | 100.200 ms |

Collections: 1,766 gen0, 1,634 gen1, 2 gen2. Total GC pause **2124.9 ms** across the run, 0.354% of wall clock.

Churn 64 KiB per iteration — **50 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.462 ms is **6x inside** the 15.6 ms Tick budget. 3 of 437,876 iterations exceeded it.


Recorded 2026-08-01 23:09 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=off concurrent=on

435,775 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.354 ms |
| p90 | 1.434 ms |
| p99 | 1.846 ms |
| **p99.9** | **2.187 ms** |
| p99.99 | 2.771 ms |
| max | 5.073 ms |

Collections: 5,450 gen0, 1,258 gen1, 419 gen2. Total GC pause **2766.9 ms** across the run, 0.461% of wall clock.

Churn 64 KiB per iteration — **50 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.187 ms is **7x inside** the 15.6 ms Tick budget. 0 of 435,775 iterations exceeded it.


Recorded 2026-08-01 23:19 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=off concurrent=on

435,465 iterations over 600s. Live: 172.3 MiB of native world plus 190.4 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.354 ms |
| p90 | 1.438 ms |
| p99 | 1.794 ms |
| **p99.9** | **2.197 ms** |
| p99.99 | 3.693 ms |
| max | 34.425 ms |

Collections: 5,038 gen0, 848 gen1, 11 gen2. Total GC pause **2520.8 ms** across the run, 0.420% of wall clock.

Churn 64 KiB per iteration — **50 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.197 ms is **7x inside** the 15.6 ms Tick budget. 2 of 435,465 iterations exceeded it.


Recorded 2026-08-01 23:29 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=on concurrent=off

428,081 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.363 ms |
| p90 | 1.479 ms |
| p99 | 1.935 ms |
| **p99.9** | **5.664 ms** |
| p99.99 | 5.914 ms |
| max | 6.854 ms |

Collections: 3,756 gen0, 1,071 gen1, 1,070 gen2. Total GC pause **5989.8 ms** across the run, 0.998% of wall clock.

Churn 64 KiB per iteration — **49 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 5.664 ms is **3x inside** the 15.6 ms Tick budget. 0 of 428,081 iterations exceeded it.


Recorded 2026-08-01 23:39 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=on concurrent=off

433,579 iterations over 600s. Live: 172.3 MiB of native world plus 174.7 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.361 ms |
| p90 | 1.454 ms |
| p99 | 1.659 ms |
| **p99.9** | **2.693 ms** |
| p99.99 | 2.855 ms |
| max | 71.741 ms |

Collections: 782 gen0, 665 gen1, 4 gen2. Total GC pause **1220.0 ms** across the run, 0.203% of wall clock.

Churn 64 KiB per iteration — **44 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.693 ms is **6x inside** the 15.6 ms Tick budget. 5 of 433,579 iterations exceeded it.


Recorded 2026-08-01 23:49 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=on concurrent=on

427,456 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.371 ms |
| p90 | 1.490 ms |
| p99 | 1.921 ms |
| **p99.9** | **2.495 ms** |
| p99.99 | 2.754 ms |
| max | 6.984 ms |

Collections: 4,296 gen0, 1,607 gen1, 535 gen2. Total GC pause **3077.5 ms** across the run, 0.513% of wall clock.

Churn 64 KiB per iteration — **49 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.495 ms is **6x inside** the 15.6 ms Tick budget. 0 of 427,456 iterations exceeded it.


Recorded 2026-08-01 23:59 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=on concurrent=on

432,590 iterations over 600s. Live: 172.3 MiB of native world plus 193.6 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 1.364 ms |
| p90 | 1.461 ms |
| p99 | 1.651 ms |
| **p99.9** | **2.692 ms** |
| p99.99 | 2.835 ms |
| max | 15.574 ms |

Collections: 785 gen0, 733 gen1, 4 gen2. Total GC pause **968.2 ms** across the run, 0.161% of wall clock.

Churn 64 KiB per iteration — **46 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 2.692 ms is **6x inside** the 15.6 ms Tick budget. 0 of 432,590 iterations exceeded it.

