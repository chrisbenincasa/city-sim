# S5 — BenchmarkDotNet cross-check

The same kernels and the same fixtures as the self-timed capture, under a different instrument.
Not the primary measurement: S5's deliverable is a derived ratio, which BenchmarkDotNet does not
produce. It is here because a self-timed loop is an instrument nobody has calibrated, and this
corpus has three recorded machine-state defects a second instrument would have caught.

- **Governor** powersave — turbo enabled, pinned to `taskset -c 2,8`
- **Job** ShortRun (3 warmups, 3 iterations). The error bars are wide by construction; what is
  being checked is agreement with the self-timed figures, not a tighter estimate of them.
- **Divide by 294,912 Vehicles** (16,384 Lanes × 18) for L0/L1/L2, and by 18,432 (256 Segments ×
  72) for L3 — the promotion fixture is deliberately smaller here than in the L3 capture.

| Method                               | Lanes | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------- |------ |------------:|------------:|----------:|------:|--------:|----------:|------------:|
| 'L0 bare walk'                       | 16384 |    449.6 us |    576.8 us |  31.62 us |  1.00 |    0.09 |         - |          NA |
| 'L1/L2 queue pass'                   | 16384 | 13,050.8 us | 12,110.4 us | 663.81 us | 29.12 |    2.18 |         - |          NA |
| 'L2 exchange by cursor + queue pass' | 16384 | 14,016.3 us |  5,537.2 us | 303.51 us | 31.28 |    1.98 |         - |          NA |
| 'L3 promotion'                       | 16384 |    654.4 us |  1,012.2 us |  55.48 us |  1.46 |    0.14 |         - |          NA |


## Agreement with the self-timed capture

| Kernel | BenchmarkDotNet | Self-timed | Agreement |
|---|---:|---:|---:|
| L0 bare walk | 1.52 ns/Vehicle | 1.41 ns/Vehicle | 1.08× |
| L1/L2 queue pass | 44.25 ns/Vehicle | 40.90 ns/Vehicle | 1.08× |
| L2 exchange by cursor + queue pass, 2/Lane | 47.53 ns/Vehicle | 47.32 ns/Vehicle | 1.00× |
| L3 promotion | 35.50 ns/Vehicle | 43.20 ns/Vehicle | 0.82× — **different fixture** |

**Three of four agree inside 8%, which is what a `powersave` machine and a ShortRun job can be
expected to do**, and the direction is consistent: BenchmarkDotNet's mean sits above the self-timed
minimum, which is the relationship the two estimators are supposed to have. The self-timed loop is
therefore calibrated rather than merely internally consistent.

**The fourth row is not a disagreement.** BenchmarkDotNet's promotion fixture is 256 Segments and the
L3 capture's is 2,592 — 288 KiB of Traveller rows against 2.9 MiB — so the smaller one is cheaper, in
the direction and roughly the amount L2's own working-set curve predicts. It is left different on
purpose: a cross-check that shares every fixture cannot detect a fixture that is wrong.

**Allocated: zero bytes on every kernel, including promotion.** The queue is an arena and the
intrusive list is an index, so a Tick of the Lane model allocates nothing — which is what
`adr/0036`'s GC argument requires of the hot path and what S4's K6 was about.
