# S4 K6 — the GC tail — `apple-m4-pro`

Eight runs of 10 minutes: four GC configurations x two heap arms. Not pinned —
server GC wants every core, and macOS cannot pin a thread to a core at all.

**Provenance: recovered from terminal scrollback, not written by the harness.** The invoking
command omitted both `--out` and the shell redirection that `tools/k6-run.sh` performs, so the
eight reports went to stdout only. The text below is those reports verbatim, reassembled in the
order they were produced. **The machine was otherwise idle for these eighty minutes**, which the
desktop's own K6 sweep was not; see the caveats subsection under *On the second machine* in
`docs/spike-results.md`.


Recorded 2026-08-04 19:59 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=off concurrent=off

1,092,233 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.546 ms |
| p90 | 0.560 ms |
| p99 | 0.623 ms |
| **p99.9** | **1.108 ms** |
| p99.99 | 1.150 ms |
| max | 3.915 ms |

Collections: 4,435 gen0, 2,216 gen1, 1 gen2. Total GC pause **2227.3 ms** across the run, 0.371% of wall clock.

Churn 64 KiB per iteration — **126 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 1.108 ms is **14x inside** the 15.6 ms Tick budget. 0 of 1,092,233 iterations exceeded it.


Recorded 2026-08-04 20:09 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=off concurrent=off

1,062,301 iterations over 600s. Live: 172.3 MiB of native world plus 232.6 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.558 ms |
| p90 | 0.583 ms |
| p99 | 0.655 ms |
| **p99.9** | **1.104 ms** |
| p99.99 | 1.154 ms |
| max | 112.652 ms |

Collections: 4,332 gen0, 2,137 gen1, 1 gen2. Total GC pause **2353.8 ms** across the run, 0.392% of wall clock.

Churn 64 KiB per iteration — **122 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 1.104 ms is **14x inside** the 15.6 ms Tick budget. 2 of 1,062,301 iterations exceeded it.


Recorded 2026-08-04 20:19 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=off concurrent=on

1,074,920 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.553 ms |
| p90 | 0.570 ms |
| p99 | 0.731 ms |
| **p99.9** | **0.972 ms** |
| p99.99 | 1.021 ms |
| max | 1.659 ms |

Collections: 10,471 gen0, 3,489 gen1, 1,163 gen2. Total GC pause **2999.1 ms** across the run, 0.500% of wall clock.

Churn 64 KiB per iteration — **124 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 0.972 ms is **16x inside** the 15.6 ms Tick budget. 0 of 1,074,920 iterations exceeded it.


Recorded 2026-08-04 20:29 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=off concurrent=on

1,083,780 iterations over 600s. Live: 172.3 MiB of native world plus 278.9 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.547 ms |
| p90 | 0.562 ms |
| p99 | 0.681 ms |
| **p99.9** | **0.950 ms** |
| p99.99 | 3.661 ms |
| max | 22.756 ms |

Collections: 9,438 gen0, 2,399 gen1, 54 gen2. Total GC pause **2886.1 ms** across the run, 0.481% of wall clock.

Churn 64 KiB per iteration — **125 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 0.950 ms is **16x inside** the 15.6 ms Tick budget. 2 of 1,083,780 iterations exceeded it.


Recorded 2026-08-04 20:39 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=on concurrent=off

1,077,247 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.554 ms |
| p90 | 0.569 ms |
| p99 | 0.617 ms |
| **p99.9** | **0.693 ms** |
| p99.99 | 2.045 ms |
| max | 5.626 ms |

Collections: 547 gen0, 117 gen1, 1 gen2. Total GC pause **434.3 ms** across the run, 0.072% of wall clock.

Churn 64 KiB per iteration — **124 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 0.693 ms is **22x inside** the 15.6 ms Tick budget. 0 of 1,077,247 iterations exceeded it.


Recorded 2026-08-04 20:49 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=on concurrent=off

1,063,321 iterations over 600s. Live: 172.3 MiB of native world plus 306.1 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.560 ms |
| p90 | 0.578 ms |
| p99 | 0.623 ms |
| **p99.9** | **0.707 ms** |
| p99.99 | 2.049 ms |
| max | 206.977 ms |

Collections: 501 gen0, 2 gen1, 1 gen2. Total GC pause **637.9 ms** across the run, 0.106% of wall clock.

Churn 64 KiB per iteration — **122 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 0.707 ms is **22x inside** the 15.6 ms Tick budget. 2 of 1,063,321 iterations exceeded it.


Recorded 2026-08-04 20:59 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — unmanaged, server=on concurrent=on

1,073,473 iterations over 600s. Live: 172.3 MiB of native world.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.556 ms |
| p90 | 0.571 ms |
| p99 | 0.617 ms |
| **p99.9** | **0.696 ms** |
| p99.99 | 2.099 ms |
| max | 3.590 ms |

Collections: 546 gen0, 128 gen1, 1 gen2. Total GC pause **449.8 ms** across the run, 0.075% of wall clock.

Churn 64 KiB per iteration — **124 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 0.696 ms is **22x inside** the 15.6 ms Tick budget. 0 of 1,073,473 iterations exceeded it.


Recorded 2026-08-04 21:09 UTC. Not a BenchmarkDotNet job: warmup-and-discard is exactly wrong for a tail.

### K6 — managed objects, server=on concurrent=on

1,072,727 iterations over 600s. Live: 172.3 MiB of native world plus 325.2 MiB of managed objects.

| Statistic | Per-iteration |
|---|---:|
| p50 | 0.555 ms |
| p90 | 0.575 ms |
| p99 | 0.623 ms |
| **p99.9** | **0.705 ms** |
| p99.99 | 2.088 ms |
| max | 55.425 ms |

Collections: 499 gen0, 2 gen1, 1 gen2. Total GC pause **480.0 ms** across the run, 0.080% of wall clock.

Churn 64 KiB per iteration — **123 MB/s** of short-lived managed allocation, one object in sixteen held long enough to be promoted. Without churn there is no collection and no pause, whatever is held live; the rate is stated so it can be argued with.

p99.9 of 0.705 ms is **22x inside** the 15.6 ms Tick budget. 2 of 1,072,727 iterations exceeded it.
