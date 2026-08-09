# Spike results

> Recorded numbers from the spikes. Plans in [`plans/0003-build-plan.md`](../plans/0003-build-plan.md).

[`06-roadmap.md`](06-roadmap.md) is explicit about why this file exists: *the spikes are worthless if
their results are held in the developer's head, because the whole value is being able to re-read them
in a year when a performance question resurfaces.* **Record them; delete the code.**

Each entry states the machine, the numbers, and — separately from the numbers — **the decision they
produced**. A spike that records data and no verdict has not finished.

| Spike | Question | Status |
|---|---|---|
| **S4** | Kernel benchmark — the machine's response to the shapes this design makes | in progress. **K0–K6 recorded below and the verdict reached, all seven on two machines.** Owed: the deleting commit. [`plans/0004`](../plans/0004-s4-kernel-benchmark.md) |
| **S2** | Routing ceiling — travel-time matrix, then HPA\* versus DSDV distance-vector. Owns the pathfinding cluster; *informs* Chunk size (`adr/0040`) | in progress. **R0 and R1 done** — the graph and the denominator, then the matrix, which **carries the choice loop** and leaves R4 resting on R2 alone. Raw captures in `spikes/S2.Routing/results/`; both sections owed a rewrite by R7. R2 next. [`plans/0010`](../plans/0010-s2-routing.md) |
| **S1** | Rendering ceiling — 20k Buildings via chunked `MultiMeshInstance3D` | not run |
| **S3** | UI ceiling — one data panel with a live multi-series graph, and how long it took | not run |
| **S0a** | The world at target size — 1M Citizens in `Borough.Headless`, footprint and the empty Tick | **done, and it found the runs had never had a city in them.** The tables hold 1M with an order of magnitude spare; **one State Hash costs 2.08 Tick budgets** and the Decide guard costs 4.9. Capture is `powersave` and owes a re-take. Recorded below |
| **S0b** | The Tick with work in it — Event Wheel, Bin Rules with wait lists, a Sweep Rule pass, a routing load | **not run, and not runnable.** Three of the four are slices 9, 7 and 10. This is the half that carries `06`'s stated risk |

---

## S4 — the kernel benchmark

**Tasks 1–10 of [`plans/0004`](../plans/0004-s4-kernel-benchmark.md) are done: the machine is recorded,
the denominator measured, the schema derived, K0–K6 run and the verdict reached.** Still owed by this
section: the commit at which `spikes/S4.Kernels/` was deleted.

**The second-machine capture is what most changed the reading**, and it changed it in the direction the
two-machine rule was written to catch. Four conclusions were properties of the desktop rather than of
the design — the threading payoff, the array-of-structs advantage in K2, the per-column copy penalty
in K3, and the sign of server GC's effect on the unmanaged arm in K6 — and one methodological defect
surfaced only from the disagreement: a ratio against a bandwidth ideal stops being a verdict once the
loop is no longer bandwidth-bound.

K1–K5 were captured by `spikes/S4.Kernels/tools/kernel-run.sh`, which pins to one physical core with
its SMT sibling idle and puts the governor and the configured DIMM rate in the label. It exists
because a kernel run under a different frequency configuration than its denominator is a ratio
between two machines rather than a measurement of one.

Measured 2026-07-31 on **two machines** — a Linux desktop via
`spikes/S4.Kernels/tools/baseline-sweep.sh`, and an Apple M4 Pro laptop via `tools/mac-sample.sh`.
The second exists because a single sample cannot tell a property of the design from a property of
the box it was measured on, and on this evidence it could not: one of the conclusions below reverses
between them. Raw captures are in `spikes/S4.Kernels/results/`, recoverable from the deleting commit
recorded at the end of this section once task 11 runs.

**K0–K6 have since been captured on the M4 Pro as well** (2026-08-03 and 2026-08-04,
`results/k0-apple-m4-pro.md` and `results/kernels-apple-m4-pro.md`), and each kernel below carries an
*On the second machine* subsection stating what travelled and what did not. **Every kernel now rests on
two machines.** K6's second capture went to the terminal rather than to a file — the invoking command
omitted the redirection `tools/k6-run.sh` performs — and its eight reports were recovered from
scrollback rather than from `results/`.

**Three things to hold while reading those subsections.** The M4 Pro capture is **not pinned** — macOS
cannot pin a thread to a core at all — so its absolutes are a shape and its variant ratios are the
comparable part. The M4 Pro's L2 is **16 MiB cluster-shared** against the desktop's 12 MB L3, which is
large enough that two of the kernels are partly cache-resident there and are not measuring what their
desktop counterparts measure. And the third is a defect in the capture itself:

> **The M4 Pro kernels do not share a sitting with their own denominator, and the capture says they
> do.** `results/kernels-apple-m4-pro.md` states *"Divide against `baseline-apple-m4-pro.md`, measured
> in the same sitting"*. The baseline is stamped **2026-08-03 00:42 UTC** and the kernels
> **2026-08-04 19:26 UTC** — **42 hours 44 minutes apart**, on a machine with no governor control, no
> turbo switch and no thread pinning. **Every *vs ideal* figure in the M4 Pro subsections divides by
> that stale denominator and is provisional until the baseline is re-measured.** The *vs variant*
> columns do not: they are ratios within a single BenchmarkDotNet process and are unaffected.
>
> The desktop does not carry this defect, and the difference in wording is exactly where it shows.
> `results/kernels-ddr2133-performance-turbo.md` claims its baseline was *"measured under the same
> **configuration**"* — 59 hours earlier, but pinned to one core, under a set governor, at a labelled
> DIMM rate, all of which are recorded and were re-established for the run. That is a defensible claim
> about a controlled machine. *"Same sitting"* is a claim about a moment, it was not true, and it was
> made about the one machine that has no configuration controls to fall back on.
>
> **The two machines carry different defects, and it is worth being exact about which is which.** The
> **desktop was running other work during its captures**; the **M4 Pro was quiet during its own**. So
> the desktop's exposure is contention and the M4 Pro's is a stale denominator, and neither machine is
> the clean one. What limits the desktop's exposure is that `kernel-run.sh` pins to one physical core
> with the SMT sibling idle, so another process must be scheduled onto that core to interfere —
> **except through DRAM bandwidth, which pinning does not protect and which K1, K2 and K3 are bound
> by.** See *What the desktop's background load can and cannot have moved*, below.
>
> **This is the third instance of the pattern this corpus keeps finding — a value never checked
> against what it claimed to describe** — after the GC sweep that reported the configuration it asked
> for rather than the one it got, and `adr/0036`'s trigger whose statistic could not detect its own
> event. Recorded in [`plans/0002`](../plans/0002-open-questions.md) with the other two.

**What survives this and what does not** is separated explicitly in each subsection below, because the
division is not intuitive: almost every *conclusion* drawn from the M4 Pro is a within-run variant
ratio and is untouched, while almost every *headline number* is a ratio to ideal and is provisional.

#### What the desktop's background load can and cannot have moved

**Immune.** Every *vs variant* ratio, which is where almost all of the conclusions live — two variants
in one BenchmarkDotNet process, minutes apart, under whatever load was present for both. And all of
K6's collector figures: `GC.GetTotalPauseDuration()` and the generation counts are reported by the
runtime, not inferred from wall clock, and no other process creates gen2 collections in this one.

**Exposed, and by how much is unknown.** Absolute ns/op, and every ratio to ideal — because the
desktop's baseline and its kernels are 59 hours apart, so they need not have shared a load even though
they shared a configuration. Pinning to one physical core with the SMT sibling idle means another
process must be scheduled onto that core to steal cycles, which is real protection. **It is no
protection at all against DRAM bandwidth contention, and K1, K2 and K3 are bandwidth-bound** — that is
the desktop's actual exposure, and it is the same kernels whose ratios-to-ideal matter most.

**The direction that would be dangerous is the one the evidence argues against.** A loaded *baseline*
reads low, which makes the hand-computed ideal too easy and flatters every kernel — the direction that
could hide a tripwire. Against that: K1 achieves **91% of the desktop's measured copy ceiling**, a
plausible fraction for a read-only scan against a copy, and nowhere near the ≥100% that an understated
ceiling produces. On the M4 Pro the same loop reaches only 50% of a ceiling measured on a quiet machine.
**If the desktop ceiling were badly understated, K1 would be pressed against it or through it, and it is
not.** That is an argument, not a measurement, and the honest position is that the desktop's absolutes
carry an unquantified error bar in an unknown direction while its comparisons do not.

**K6 is the desktop capture most exposed**, because it is deliberately unpinned — server GC wants every
core. Its arm-separation conclusion survives anyway, by the same argument that carries it on the M4 Pro:
the arms alternated run by run, and all four managed maxima exceed all four unmanaged maxima, which no
drift or single burst can produce.

### The machine — Linux desktop

| Field | Value |
|---|---|
| CPU | Intel Core i5-10400 @ 2.90 GHz (Comet Lake, 6 cores / 12 threads, 4.3 GHz max turbo) |
| L1d | 32 KiB per core, 8-way, 64 B lines |
| L2 | 256 KiB per core, 4-way |
| L3 | 12 MiB, 16-way, **shared by all 12 hardware threads** |
| RAM | 2 × 32 GiB Corsair `CMK64GX4M2E3200C16`, one per channel, **running at 2133 MT/s** |
| Theoretical DRAM peak | **34.1 GB/s** — 2 channels × 2133 MT/s × 8 B |
| OS | Ubuntu 24.04.4 LTS, kernel 6.8.0-136 |
| SDK / runtime | .NET 10.0.110 SDK, runtime 10.0.10, x64 RyuJIT `x86-64-v3` |
| THP | `madvise` |

**The DIMMs are rated DDR4-3200 and are running at 2133 because XMP is off in firmware, and the
i5-10400's memory controller is specified only to 2666 in any case.** This is not a footnote: it sets
the ceiling that K1, K2, K3 and K5 are all measured against. A re-sweep with XMP enabled is owed, and
until it lands every DRAM-bound figure here is a figure for *this* configuration rather than for this
machine's best.

Each measurement is pinned with `taskset` to one hardware thread of one physical core, leaving the
SMT sibling idle. Single-threaded throughout — the kernels that will be parallelised are decided by
`05 §6`'s Factorio rule, not by this baseline.

### The denominator — sustained single-threaded `memcpy`

256 MiB per copy, well past L3, over a ten-second window per configuration. GB means 1e9 bytes.
**Copy rate** is bytes delivered to the destination; **traffic** is twice that, since every byte
copied is a byte read and a byte written, and traffic is the figure to compare against the 34.1 GB/s
DIMM ceiling.

| Configuration | Clock in window | Copy median | Copy p95 | Copy worst | Read median | 16 KiB burst |
|---|---|---|---|---|---|---|
| **performance + turbo** | 4089 MHz | **13.3 GB/s** | 12.1 | 7.1 | 15.5 GB/s | 74.5 GB/s |
| powersave + turbo | 4083 MHz | 12.9 GB/s | 9.9 | 5.9 | 15.6 GB/s | 86.6 GB/s |
| performance, no turbo | 2899 MHz | 12.9 GB/s | 11.8 | 6.6 | 13.2 GB/s | 59.3 GB/s |
| powersave, no turbo | 2899 MHz | 12.6 GB/s | 11.7 | 6.9 | 13.2 GB/s | 60.5 GB/s |

**The denominator is 13.3 GB/s copy rate, 26.6 GB/s traffic — 78% of the 34.1 GB/s theoretical
peak.** Sustained single-threaded read is 15.5 GB/s, 45% of peak, which is the expected shape: a
single core cannot saturate two channels because it runs out of outstanding line-fill buffers long
before it runs out of DRAM.

**Read and copy have different denominators and later kernels must use the right one.** K3 is a copy
and divides by 13.3 GB/s. K1 is a read-modify-write scan and K2 is a gather; neither writes as much
as it reads, so a pure-copy denominator would make both look better than they are.

### What the frequency sweep decided

Running all four combinations was worth it, because the governor's effect is not one number — it
depends entirely on where the working set sits.

- **DRAM-bound copy barely notices the clock.** 12.6 to 13.3 GB/s across a 1.41× clock range: a 5.6%
  spread. At 256 MiB the core is waiting on memory, and a faster core waits faster.
- **Cache-resident copy is very nearly clock-proportional.** 16 KiB goes from ~60 GB/s at 2.9 GHz to
  ~74–87 GB/s at 4.1 GHz, a 1.43× ratio against a 1.41× clock ratio. K4's small sorted arrays live
  here, and K4's numbers will therefore be a statement about clock speed as much as about layout.
- **Streaming read sits in between and does follow the clock** — 13.2 → 15.5 GB/s, an 18% gain —
  because the rate at which misses can be issued is a core-side limit, not a DRAM-side one.
- **The tail is where the governor actually shows up.** The two turbo rows have identical medians and
  materially different p95s: 12.1 GB/s under `performance` against 9.9 GB/s under `powersave`. A
  benchmark reporting only medians would have concluded the governor does not matter, and K6's whole
  subject is a tail.

**Decision: K0–K6 run under `performance` with turbo enabled, pinned to one hardware thread.** It has
the highest median, the tightest tail, and it is the configuration a player's desktop is actually in
— nobody disables turbo to play a city-builder. The clock held at 4089 MHz mean over the ten-second
window with no downward drift, so single-core turbo on a 6-core part is stable enough to run K6's ten
minutes against; if that stops being true, K6's own histogram will say so.

### Aggregate bandwidth against thread count

Everything above is one thread on one core. This is the same two kernels run on 1 to 12 threads,
each with its own private 64 MiB buffers — shared buffers would measure cache coherence, which is a
different subject with a different answer. Threads fill distinct physical cores first and only double
up on SMT siblings past six, so that the curve is not partly a measurement of CPU enumeration order.
Measured under `performance` + turbo, five seconds per point.

| Threads | Placement | Copy aggregate | vs 1 | Copy per-core | Read aggregate | vs 1 | Read per-core |
|---|---|---|---|---|---|---|---|
| 1 | 1 core, siblings idle | 13.2 GB/s | 1.00× | 13.2 GB/s | 15.8 GB/s | 1.00× | 15.8 GB/s |
| 2 | 2 cores, siblings idle | **13.7 GB/s** | **1.03×** | 6.8 GB/s | 22.1 GB/s | 1.40× | 11.1 GB/s |
| 3 | 3 cores, siblings idle | 13.5 GB/s | 1.02× | 4.5 GB/s | 24.2 GB/s | 1.53× | 8.1 GB/s |
| 4 | 4 cores, siblings idle | 12.9 GB/s | 0.98× | 3.2 GB/s | 26.0 GB/s | 1.65× | 6.5 GB/s |
| 5 | 5 cores, siblings idle | 13.1 GB/s | 0.99× | 2.6 GB/s | 27.9 GB/s | 1.77× | 5.6 GB/s |
| 6 | 6 cores, siblings idle | 12.9 GB/s | 0.97× | 2.2 GB/s | **28.9 GB/s** | **1.83×** | 4.8 GB/s |
| 8 | 6 cores + 2 siblings | 12.7 GB/s | 0.96× | 1.6 GB/s | 28.8 GB/s | 1.82× | 3.6 GB/s |
| 10 | 6 cores + 4 siblings | 12.6 GB/s | 0.95× | 1.3 GB/s | 28.0 GB/s | 1.77× | 2.8 GB/s |
| 12 | 6 cores + 6 siblings | 12.5 GB/s | 0.95× | 1.0 GB/s | 27.9 GB/s | 1.76× | 2.3 GB/s |

**Copy-shaped streaming does not scale at all.** It peaks at 1.03× on two threads and is *below* one
thread from four threads onward. A single core already reaches 26.5 GB/s of traffic against a
34.1 GB/s ceiling, so there is nothing left for the other five to have. Read-and-write streaming is a
solved problem on this machine at one core, and adding cores to it is not a speedup, it is a way to
spend five more cores producing 3% and then losing it again.

**Read-only streaming scales to 1.83× on six cores and then stops**, at 28.9 GB/s — 85% of
theoretical peak, which is the practical ceiling of this memory system. One core reaches only 45% of
peak because it runs out of outstanding misses rather than DRAM, which is exactly why the read curve
has somewhere to go and the copy curve does not.

**SMT contributes nothing to either.** Past six threads both curves decline slightly. A second thread
on a core already streaming finds no bandwidth the first one left behind; it only adds contention.

**The per-core share is the number a parallel Tick phase actually receives, and it collapses.** Read
falls from 15.8 GB/s alone to 4.8 GB/s each across six cores — every thread gets 3.3× less memory
than it would have had to itself.

### The second machine — Apple M4 Pro

A deliberately unlike sample, and it earns its place by disagreeing. 10 performance cores + 4
efficiency, 128 KiB L1d per performance core, a 16 MiB cluster-shared L2, **128-byte cache lines**,
24 GiB unified memory, macOS 26.5.2, .NET 10.0.2 on arm64. Apple **publishes** 273 GB/s for this
part; that figure is not measured here and is used only to state percentages.

| | Desktop (i5-10400) | MacBook Pro (M4 Pro) |
|---|---|---|
| Cache line | 64 B | **128 B** |
| Single-thread copy | 13.2 GB/s (26.5 traffic) | **63.2 GB/s** (126.5 traffic) |
| Single-thread read | 15.8 GB/s | **66.6 GB/s** |
| One core's share of the ceiling | copy 78%, read 45% | copy 46%, read **25%** |
| Copy, best aggregate | 13.7 GB/s at 2 threads, **1.03×** | 118.8 GB/s at 5 threads, **1.87×** |
| Read, best aggregate | 28.9 GB/s at 6 cores, **1.83×** | 251.8 GB/s at 12 threads, **3.75×** |
| Read aggregate vs ceiling | 85% | 92% |

**The read curve does keep climbing, and the caveat written above it was the right caveat.** Read
scales to 3.75× where the desktop stops at 1.83×, and the mechanism is visible in the single-core
share: one M4 Pro core takes only 25% of its machine's read ceiling against the desktop core's 45%,
so there is far more left for the others to have. **"Memory-bound work does not parallelise" is a
statement about bandwidth-starved desktops, not a general truth**, and anything built against the
stronger reading would have been built against a machine-specific accident.

**Three things do generalise, and they are the ones worth keeping.** Streaming *writes* saturate
early on both — one core on the desktop, about four on the M4 Pro — and adding cores past that point
does nothing on either. Extra hardware threads past the real core count contribute nothing on either
(SMT siblings there, efficiency cores here). And the per-core share collapses on both: it is 3.3×
down at six threads on the desktop and 2.7× down at ten on the M4 Pro.

**The cache hierarchy is nearly flat for streaming on the M4 Pro, which the desktop's is not.** Its
burst sweep reads 98.7 GB/s at 64 KiB, then **54–64 GB/s at every size from 512 KiB to 256 MiB** —
DRAM is fractionally *faster* than L2-resident. Only L1 residency buys anything. The desktop falls
off a cliff instead, from 31.8 GB/s at 512 KiB to 12.8 at 256 MiB. **A layout tuned to "fit in L2"
would be worth a great deal on one machine and nothing at all on the other**, which is worth knowing
before slice 4 chooses a table layout for that reason.

**Absolute single-threaded speed differs by 4.8×**, which makes the 15.6 ms Tick budget at 4× speed
meaningless unless it names a machine. It has to be validated on the slowest target, not the fastest
one to hand.

### What the curve settles, and what it hands to `05 §6`

`05 §6`'s Factorio rule — *parallelise work that is compute-dense and read-only; do not parallelise
work that is memory-bound and pointer-chasing* — is **confirmed in its direction and refuted in its
magnitude.** The rule's shape holds on both machines. Its strength does not travel: read-only
streaming gains 1.83× on the desktop and 3.75× on the M4 Pro, and read-write streaming gains
essentially nothing on the desktop and 1.87× on the M4 Pro. **The rule is right; any constant
attached to it is a property of the machine and must not be hard-coded.**

The crossover is worth stating in cycles, since that is the form a Tick phase can be judged against —
and it must be stated per machine, which is itself the finding:

| | Line every, saturated | Line every, alone | Crossover |
|---|---|---|---|
| Desktop | 13.3 ns / 64 B | 4.1 ns | ~55 cycles per 64-byte line |
| M4 Pro | 5.2 ns / 128 B | 1.9 ns | ~23 cycles per 128-byte line |

Per byte touched that is 0.86 cycles against 0.18 — **the M4 Pro needs roughly five times less
arithmetic per byte before threading pays.** Both assume compute and memory overlap perfectly, which
is optimistic, so both are floors rather than targets.

Four consequences, none of which `05 §6` currently states:

- **The parallelisation decision is a runtime one, not a source-code one.** A phase worth threading
  on the M4 Pro is not worth threading on the desktop, and the difference is 2× on reads. This is
  precisely the case the host-adaptive policy in [`0002`](../plans/0002-open-questions.md) admits:
  thread count and whether a phase is parallelised at all sit inside the guarantee invariant 4
  already makes, so they may be chosen from measurement at startup. Nothing else on this page has
  earned that.
- **A memory-bound phase should still default to single-threaded**, and this is a performance
  argument arriving at the same place the determinism argument does. Thread-count equivalence
  (`run(log, threads=1).hash() == run(log, threads=8).hash()`) is cheapest to hold where there are no
  threads, and the phases hardest to make equivalent — cross-partition writes, the Wheel's random
  reschedules — are precisely the memory-bound ones that gain least on the desktop and are latency-
  rather than bandwidth-bound on both.
- **`02 §1.1`'s parallel-phase permissions cost less than they appear to.** The contradiction flagged
  in [`0002`](../plans/0002-open-questions.md) — the phase table states permission, `§6` states
  implementation, and neither says so — can be resolved knowing that permission granted to a
  memory-bound phase is permission worth nothing.
- **The figure to compare a phase against is the per-core share, not the aggregate.** A phase sized
  against 28.9 GB/s when it will run on one of six threads is mis-sized by 6×.

**The constants are not universal and the second machine proved it** — two channels of DDR4-2133 is
the low end of the target class, and the M4 Pro doubles the read scaling. What generalises is the
shape: writes saturating early, extra hardware threads past the core count contributing nothing, the
per-core share collapsing. **Nothing here has been measured for random access**, which is K2's
subject and which will be worse than either curve on both machines.

### Three caveats on the numbers above

**The L3-resident rows are contaminated and should not be quoted.** L3 is shared by all twelve
hardware threads, so a 4 MiB working set measures whatever else the box was doing: the same
measurement returned 11.5, 13.7, 20.3, 28.1 and 29.7 GB/s across five runs whose only intended
difference was the governor. Pinning does not help — it isolates the core, not the shared cache.
Anything K1–K5 reports at an L3-resident size needs an idle machine and a stated variance, and the
5.6% stability of the DRAM figures is not evidence about the L3 ones.

**The 16 KiB burst figure is the least stable number here**, and the two turbo rows disagree by 16%
in the wrong direction. It is a best-of-64 over samples a few microseconds long; treat it as an order
of magnitude for L1 and nothing finer.

**The M4 Pro curve was measured with no control over thread placement whatsoever.** macOS cannot pin
a thread, and the fallback — asking for the user-interactive quality-of-service class, which prefers
a performance core — was **refused for all 162 threads**, almost certainly because .NET sets an
explicit scheduling policy on the threads it creates and `pthread_set_qos_class_self_np` returns
`EPERM` for those. The harness counted the refusals rather than assuming the hint took, which is the
only reason this is known. So every M4 Pro thread was placed by the scheduler with no input from us
and could land on an efficiency core; the non-monotonic jump in its read column between three and
four threads is the visible symptom. Read that curve as a shape. Fixing it would mean creating
threads natively rather than through .NET, which is not worth doing for a spike — but it is worth
knowing before anyone measures a *real* threaded Tick phase on Apple Silicon and believes the result.

**The scaling curve is five seconds per point on a machine that was not verified idle.** The copy
column wobbles by ±4% with no trend, which is the measurement's noise and not structure — do not read
the dip at four threads as an effect. The conclusions drawn from it are 1.0× against 1.83× against
6×, differences far larger than that noise, and none of them would survive being restated to two
significant figures.

---

## S4 task 2 — the row schema and the target row counts

**The task was to derive these rather than inherit them. The first finding is that the derivation the
plan names cannot be performed as written**, and the second is that the figure it was meant to replace
was never well-defined in the first place. Both are recorded before the numbers, because they change
what the numbers are worth.

### The derivation in `05` is circular

`05 §the budget` gives `target = map_area × mature_density × buildable_fraction`, and
[`0002`](../plans/0002-open-questions.md) §1 shows where `~3,700/km²` came from: the column it sits in
is headed **"1M implies"**. The density is an *output* of the 1M target, not an input to it, so
feeding it back through the formula re-derives the assumption and confirms nothing.

Of the three factors, **only `map_area` is ratified.** `buildable_fraction ≈ 1.0` appears exactly
once, is never argued, and contradicts [`adr/0021`](adr/0021-the-map-is-bounded-procedural-and-terrain-never-enters-a-tick.md),
which requires water bodies and a maximum buildable grade. `05` also folds roads and parks *into* the
density anchor, which means either the anchor is gross of roads and `buildable_fraction` must be well
below 1, or it is net and 3,700/km² is the wrong anchor. Nothing resolves this.

**What the formula is good for is a consistency check, not a derivation**, and every count below is
therefore built up from the mechanisms that create rows — who lives where, who works, what generates
a Trip — with each assumption named. That is slower and it is the only honest form available.

### The rule that settles who owns a field

Citizen-versus-Household ownership was contradictory across the corpus: `03 §4` invariant 1 puts
*"Money, goods, health, and employment"* on the Citizen record, while `CONTEXT.md`'s Household entry
holds *"Needs, money, and a Provider List"*. It is a 1M-row against 400k-row difference and it had to
be settled before anything could be counted.

**It is settled by a rule the corpus already contains**, stated for Buildings and generalised here:

> `CONTEXT.md` on Buildings: *"If a field would differ between two Occupants, it lives on the
> Occupant."* Generally: **a field lives at the level at which it can differ.**

That gives a clean split, and `03 §4` invariant 1 turns out not to be wrong so much as mis-grouped —
its own next clause (*"the moving entity is a view onto it, not the owner"*) shows the contrast it is
actually drawing is persistent record versus **embodiment**. It is forbidding money on the Traveller,
not on the Household.

| Owner | Fields | Why |
|---|---|---|
| **Household** | money, savings, goods, Needs, Provider List, Life Stage, Taste, car ownership, Schooling accumulator, dwelling, failed-attempt counter, refusal reason | None can differ between members of a group defined as *"sharing a dwelling and finances"* |
| **Citizen** | health, age, Skill Tier, experience, employment, workplace, current activity, `next_event_tick` | All differ between two adults of one Household |

**Two consequences.** The Citizen loses `home` — a dwelling cannot differ between members, so it is
the Household's — which `CONTEXT.md`'s Citizen entry currently lists. And **`Unemployment` stops being
a field and becomes a derived readout**: *"a Household where no contained Citizen holds a job"*,
against `CONTEXT.md`'s current *"A Household with no workplace"*. If a profile later wants a cached
bit, it is declared `(derived AND rebuilt)` under `adr/0003`'s field rule, never saved, never hashed,
with a debug invariant asserting it matches the walk — a stale bit is a Household that believes it is
employed and never seeks work, which is silent and hash-bearing. Because it is derived, deferring it
costs no save migration.

### The Citizen row, recomputed — and the 40 bytes was never one number

`05 §3` and `03 §2.1` both say *"on the order of 40 bytes hot"*, and `03 §2.1` flags it stale because
session five added a schooling accumulator, experience and car ownership. **Recomputing it surfaces a
worse problem than staleness: "hot" was never given a definition, and the two available definitions
are 4× apart.**

**Structure-of-arrays removes per-row padding from the question.** Each field is its own contiguous
array, so a row's cost is the sum of its column widths with no alignment waste — which is why the
figures below are sums and not rounded up to a boundary. Widths are `05 §3`'s: Money and accumulators
`i64`, counts `i32`, Ticks `u64`, handles `{u32, u32}` = 8 B.

| Column | Width | Touched when |
|---|---|---|
| `next_event_tick` | 8 | every Tick — it is the Wheel bucket key |
| wheel bucket `next` | 4 | every Tick — the intrusive list link |
| current activity | 1 | every Tick — what the wake mutates |
| **Per-Tick subtotal** | **13 B** | |
| `entity_id` (monotonic) | 4 | on every draw the Citizen makes |
| `generation` | 4 | whenever the row is addressed |
| Household handle | 8 | on wake |
| workplace handle | 8 | on wake |
| experience (`i64` accumulator) | 8 | on production evaluation |
| Skill Tier | 1 | on wake |
| employment | 1 | on wake |
| occupant-list `next` | 4 | on wake |
| **Wake working set** | **51 B** | |
| age, health | ~5 | inspection only |
| **Cold** | **~5 B** | |

**So the Citizen row is 13 bytes under one reading of "hot" and 51 under the other, and the 40-byte
figure matches neither.** The per-Tick figure is the one the Event Wheel argument implies — the whole
point of the Wheel is that a sleeping Citizen is touched only through its bucket — while 51 B is what
a *woken* Citizen costs. `plans/0004` asks whether the recomputed row is "closer to 40 bytes or to
80"; the answer is that the question needs splitting before it can be answered, and **K0 must report
both, because the two drive different things**: the per-Tick figure sizes the Wheel drain (K5) and
the wake gather (K2), and the working-set figure sizes the world (K0) and the save copy (K3).

At 1M Citizens: **13 MB per-Tick hot, 51 MB working set, ~56 MB including cold.** Against `03 §2.1`'s
*"roughly 40 MB"*, the total is ~40% larger and the genuinely per-Tick part is a third of what was
claimed.

### Row counts, derived from the generators

Every figure below names its assumption. **Those marked provisional are unratified and are recorded
in [`0002`](../plans/0002-open-questions.md) as such.**

**Households — 400k survives, and now has an argument.** `adr/0011` gives five Life Stages over a life
of *"on the order of a thousand Days"*, with compositions but no stage shares. Taking equal stage
durations and the ADR's own compositions — Young 2, Family 2+2, Mature Family 2+2, Childless 2, Empty
Nest 2 — gives a mean of **2.8 Citizens per Household** and **~357,000 Households** at 1M. The
asserted ~400k implies 2.5. The two agree within 12%, which moves 400k from *asserted* to *asserted
and corroborated* — the first time that figure has had anything behind it. Use **~360k**, derived.

*Provenance, since it was asked and the answer is instructive.* The 400k figure appears in exactly two
places: `adr/0003`'s overflow-headroom sum and `adr/0011`'s decision-volume sum. **Both are arguments
that survive being wrong by 2×**, which is why neither derives the figure and neither states a
household size — it was never load-bearing where it was written. It became load-bearing later, by
being repeated. Note also that `adr/0011` asserts 400k in its *Cost* section and then specifies, further
down the same document, a stage model implying 357k. And **400,000 is already the corpus's most-repeated
number for something else entirely** — Citybound's individually-simulated cars, in `adr/0007` twice,
`adr/0016`, `adr/0018` and `06` — as well as being the Trips/Day figure. Three unrelated quantities,
one number. Each has an innocent explanation and contamination is unproven, but the shape is the same
as the 10k incident and it is recorded rather than assumed away.

**Workers — ~500k, and the employment rate is the missing input.** 360k Households × 2 adults = ~715k
adults, leaving ~285k children. Removing Empty Nest adults as retired and applying a modest
unemployment rate gives **~500,000 employed Citizens, 50% of population**. *Provisional: the corpus
contains no employment rate anywhere*, and this is the single largest assumption in the count.

**Trips — ~1.9M per Day, against the corpus's 400k.**

| Generator | Derivation | Trips/Day |
|---|---|---|
| Commute | 500k workers × 2, there and back | 1,000,000 |
| School | ~250k children in education × 2 | 500,000 |
| Shopping and services | ~1 round trip per Household every other Day | 360,000 |
| Freight Shipments | inter-District only, provisional | ~50,000 |
| **Total** | | **~1.9M** |

**The school row independently corroborates the method**: 500k against a 1M commute base is +50%,
which is exactly what [`0002`](../plans/0002-open-questions.md) states school adds to the peak,
arrived at here from child counts rather than copied. That agreement is the main reason to trust the
rest of the column.

`05 §35`'s **~400k Trips/Day is 0.4 per Citizen and is very close to the Household count** — it reads
as one Trip per Household per Day, the outbound commute with the journey home never counted. At 1.9M
the rate is 1.9 Trips per Citizen per Day, still conservative against real travel surveys at 3–4.

**Trips in flight — ~56,000.** In-flight count is `Trips/Day × mean duration ÷ TICKS_PER_DAY`. The
corpus gives only 480 Ticks, and `02 §1.2` defines that as **cross-town**, not typical. At a mean of
240 Ticks — half of cross-town, *provisional* — 1.9M × 240 ÷ 8192 ≈ **56,000**. The sensitivity is
worth stating plainly, because it is the whole disagreement:

| Mean Trip duration | In flight |
|---|---|
| 160 Ticks (⅓ cross-town) | 37,000 |
| **240 Ticks (½, provisional)** | **56,000** |
| 480 Ticks (all cross-town) | 111,000 |

This resolves the corpus's 5× contradiction by explaining both ends. **`adr/0037`'s ~23,000 is not
independent evidence** — it is exactly `400k × 480 ÷ 8192`, the same unratified figure restated, and
it is too low because it omits the return journey. **`adr/0019`'s ~12% (≈120,000) is too high**
because it prices every journey at cross-town. The derived figure sits between them, which is where
it should be.

**Legs — ~140,000 in flight.** `adr/0008` requires a car commute to be *"never fewer than three
Legs — `walk → drive → walk`"*, and says the Trip count *"roughly triples"*. At a mean 2.5 Legs per
Trip against walk-only journeys, ~2.5 × 56,000.

**Vehicles — cannot be derived, and the reason is a decision, not a gap.** A Vehicle exists only on a
Microscopic Segment, and the **Microscopic Cap is unset**. The Vehicle and Lane tables are therefore
sized *by that constant* rather than by population, and `05` notes that at 1M *"most of the city is
permanently Statistical"*. **K0 must take the Cap as a parameter and report the footprint as a
function of it** rather than picking a number — which also makes K0 the natural place to inform what
the Cap should be.

| Table | Count | Basis |
|---|---|---|
| Citizens | 1,000,000 | ratified target |
| Households | ~360,000 | derived from `adr/0011` stage compositions |
| Trips in flight | ~56,000 | derived; provisional mean duration |
| Legs in flight | ~140,000 | `adr/0008`'s 2.5–3 Legs per Trip |
| Buildings | ~150,000 | provisional — no Households-per-Building figure exists |
| Businesses | ~50,000 | provisional — no workers-per-Business figure exists |
| Lots | ~225,000 | provisional — Buildings plus vacancy |
| Segments | ~30,000 | provisional — road density against 268 km² |
| Lanes, Vehicles | **parameterised** | bounded by the unset Microscopic Cap |

**1M is a floor, not a cap — so the counts above are the wrong form and these are the right one.**
`00-vision`'s scope commitment is *at least* a million, and [`0002`](../plans/0002-open-questions.md)
already states the principle: *"Sizing is a derivation, not a constant."* Every row count here is
linear in population, so it is recorded per 1,000 Citizens and stays correct at 2M or at 250k:

| Per 1,000 Citizens | Rows |
|---|---|
| Households | 360 |
| Trips in flight | 56 |
| Legs in flight | 140 |
| Trips per Day | 1,900 |
| Buildings / Businesses / Lots | ~150 / ~50 / ~225, all provisional |

**This also fixes how K0's result must be read.** A footprint that merely *fits* at 1M is a failure,
not a pass: the design claims to handle at least a million, so the question K0 answers is how much
headroom exists above the floor, not whether the floor is reachable. Report the footprint and the
multiple of 1M the machine could hold.

### What task 2 changed, and what it owes

**Changed:** the Citizen row is two numbers rather than one, and neither is 40. The ownership rule is
settled and `03 §4` invariant 1 needs a wording pass. `Unemployment` becomes derived. The 400k
Household figure is corroborated for the first time. The Trip count is 4.75× the corpus's figure, and
`adr/0037`'s 23,000 is identified as a restatement rather than a second source.

**Owed, and blocking nothing in slice 1:** an employment rate, a mean Trip duration, a
Households-per-Building figure, a workers-per-Business figure, and the Microscopic Cap. Also a
`buildable_fraction` that survives `adr/0021`, and a decision on whether labour is a Household↔Business
relationship or an input Bin — the latter changes the Business row and is already queued for slice 7.

---

## K0 — the world's actual footprint

Allocated for real against task 2's schema and first-touched page by page, not computed — a sum cannot
see what the allocator and the operating system actually charge. Both figures are reported and the
gap between them is the only part of this kernel that could have surprised.

| Table | Rows | Per-Tick | Wake | Cold | Total |
|---|---:|---:|---:|---:|---:|
| Citizens | 1,000,000 | 12.4 MiB | 36.2 MiB | 4.8 MiB | 53.4 MiB |
| **Households** | 360,000 | 4.1 MiB | 58.0 MiB | 13.0 MiB | **75.2 MiB** |
| Buildings | 150,000 | 1.7 MiB | 16.2 MiB | 3.7 MiB | 21.6 MiB |
| Lots | 225,000 | — | 3.0 MiB | 4.5 MiB | 7.5 MiB |
| Businesses | 50,000 | 0.6 MiB | 6.0 MiB | 0.6 MiB | 7.1 MiB |
| Legs in flight | 140,000 | 1.6 MiB | 1.7 MiB | — | 3.3 MiB |
| Trips in flight | 56,000 | 1.3 MiB | 1.5 MiB | 0.1 MiB | 2.9 MiB |
| Segments | 30,000 | 0.1 MiB | 0.6 MiB | 0.6 MiB | 1.3 MiB |
| **Total** | | **21.8 MiB** | **123.3 MiB** | **27.3 MiB** | **172.3 MiB** |

The process working set grew by **174.2 MiB against 172.3 MiB requested — 1.1% overhead**, allocated
and touched in 52 ms. The allocator charges essentially nothing beyond the request, which is what
structure-of-arrays over native memory should do and is worth having confirmed rather than assumed.

### `adr/0037` is vindicated, with a number it never had

`05 §3` says the full-world copy `adr/0037` deleted was *"~150 MB per Tick against ~1 MB of actual
writes — 8–15 ms, memory-bandwidth bound"*. Both halves of that now have measurements behind them:

- **The world is 172.3 MiB (180.7 MB), against the asserted ~150 MB.** The estimate was low by 20%
  and was the right order — unusually good for a figure nobody had allocated.
- **At this machine's measured 13.3 GB/s copy rate, that copy costs 13.6 ms.** `adr/0037` asserted
  8–15 ms. The measurement lands inside its own band.

**That is ledger #29 answered.** A 13.6 ms copy against the **15.6 ms Tick budget at 4× speed** would
have consumed 87% of the Tick to move data that was 99% unchanged. `adr/0037` deleted it on an
argument; the argument was right, and it was closer to the cliff than the ADR knew. On the M4 Pro the
same copy is 2.9 ms — so the decision was correct on the fast machine too, just less obviously.

The copy has not gone away, it has moved: the **async save** takes one real copy at save time, and
13.6 ms is its price. That is fine for something that happens on a save rather than on a Tick, and it
is the figure the save's threading design should be built against. **K3 will measure this directly
rather than deriving it from the denominator**, which is the point of having K3 at all.

### Three things the footprint says that the corpus does not

**Households are the largest table in the world, and it is not close.** 75.2 MiB against the Citizens'
53.4 MiB, from 2.8× *fewer* rows — a 219-byte Household row against a 56-byte Citizen. The driver is
the **Provider List at 104 bytes, 47% of the Household row and roughly 21% of the entire world**. Two
consequences worth stating plainly: the design's memory is dominated by *what Households know*, not by
how many Citizens exist; and `adr/0017` makes the list length **a Ruleset constant**, which means a
tuning knob controls a fifth of the world's footprint. The 8 entries used here are provisional — the
corpus states no length — and every entry is ~4.5 MiB at 1M.

**Only 13% of the world is touched on an ordinary Tick.** 21.8 MiB of 172.3 MiB, and that is the
*addressable* per-Tick set rather than the traffic — the Wheel drains one bucket of 8,192, so real
per-Tick traffic is far smaller again. This is the Event Wheel's premise made arithmetic: the
overwhelming majority of the world is untouched on any given Tick, which is exactly why `adr/0037`'s
copy was indefensible and why K2's sparse-gather pattern is the one that matters.

**The Microscopic Cap is not memory-bound, so memory must not be what sets it.**

| Microscopic Segments | Lanes | Vehicles at jam | Footprint |
|---:|---:|---:|---:|
| 1,000 | 4,000 | 128,000 | 3.0 MiB |
| 5,000 | 20,000 | 640,000 | 15.0 MiB |
| 10,000 | 40,000 | 1,280,000 | 30.1 MiB |
| 30,000 (the whole network) | 120,000 | 3,840,000 | 90.3 MiB |

Making the *entire* road network Microscopic costs 90 MiB — half the world again, on a machine with
64 GiB. The Cap is a compute and behavioural constant, not a memory one, and any future argument that
sizes it against footprint is arguing from the wrong quantity.

### Headroom, which is the actual result

**372×.** This desktop could hold roughly 372 million Citizens' worth of rows; the 24 GiB M4 Pro was
projected at about 140 million and **has since measured 143×**, which is as close as a projection from
available memory can be expected to land. Since `00-vision` commits to *at least* a million rather than
exactly a million, the multiple is the finding and the absolute figure is only how it was reached.
**Memory is not a constraint on this design at any population it will plausibly reach**, and the binding
constraints are elsewhere — per-Tick compute, the routing graph, and the Microscopic Cap.

**The footprint itself is identical on both machines at 172.3 MiB**, which is the expected result and
is worth one line of confirmation: the schema is a sum of column widths with no per-row padding under
struct-of-arrays, so it cannot vary with cache line size or allocator. What differs is the allocator's
overhead — **1.0% on the M4 Pro against 1.1% on the desktop** — and the first touch, which takes
**9 ms there against 52 ms here**. The second figure is the one to remember when world generation is
built: first-touching the world is a memory-bandwidth operation and it will be roughly six times slower
on the slow target.

---

## K1 — linear scan and update, `checked` and `unchecked`

Scan-and-update over three struct-of-arrays columns at 1,000,000 rows, in the arithmetic
[`adr/0003`](adr/0003-deterministic-integer-simulation.md) actually specifies: a Q16.16
multiply-accumulate, `(int)(((long)a * b) >> 16)` into an i64. The two places it can overflow — the
narrowing cast and the accumulate — are the two the overflow policy is about, so under `checked` they
become `conv.ovf.i4` and `add.ovf` and under `unchecked` they are free. Measuring any other expression
would have measured the wrong instruction.

Traffic is 4 MB of rate read, 4 MB of weight read and 8 MB of accumulator read-modify-written = **24 MB
per pass**. Against the desktop's measured 26.6 GB/s copy traffic, the **ideal is 0.902 ms**.

| Variant | Mean | vs ideal | vs unchecked |
|---|---:|---:|---:|
| `SpanUnchecked` | 0.992 ms | 1.10× | 1.00 |
| `PointerUnchecked` | 0.995 ms | 1.10× | 1.00 |
| `PointerCheckedWalked` | 1.257 ms | 1.39× | 1.27 |
| `SpanChecked` | 1.277 ms | 1.42× | 1.29 |
| `PointerChecked` | 1.655 ms | 1.84× | 1.67 |

The disassembly was read rather than inferred; it is kept at
`spikes/S4.Kernels/results/asm-ddr2133-performance-turbo-K1LinearScan.md` because a claim about which
instructions the JIT emitted is worth nothing if the listing it rests on is not in the repository.

### Bounds checks elide, so `unsafe` earns nothing

`SpanUnchecked`'s hot loop contains no `cmp`/`jae` at all. The JIT hoists all three length comparisons
above the loop and drops into a checked path only if they fail. Span and raw pointer are within 0.3%
of each other, and both sit at 1.10× of a `memcpy`-derived ideal for a loop that also does arithmetic.

**The decision: `Borough.Core`'s table access does not need `unsafe`.** Slice 4 can write its scans
over `Span<T>` and lose nothing measurable, which is worth knowing before the tables are written
rather than after a pointer-based API has been committed to. This is the cheapest of S4's results and
not the least useful.

### `checked` costs 27%, and that is a real number rather than a small one

The cause is visible in the listing and it is not lost vectorisation — nothing vectorises in either
variant. The memory-destination `add [r10],rdi` splits into a load, an `add`, a `jo` and a store, and
the narrowing cast adds a `cmp`/`jne`. The overflow branch sits between the load and the store and
lengthens the dependency chain.

`adr/0003` asserts that `checked` inside the fixed-point library is cheap and names this as *the only
claim here without arithmetic behind it*. **It now has arithmetic: 27% on a scan that does nothing
but the multiply.** That is the worst case — a real Rule does other work, against which the check
amortises — but 27% is not nothing, and whether it counts as "cheap" is a judgement the ADR should
now make explicitly rather than inherit.

### `checked` is a block, and the block includes the address arithmetic

`PointerChecked` costs 67% rather than 27%, and the extra 40 points are **not the overflow policy**.
`checked` in C# scopes to a *block*, so it also covers address computation: `accumulator[i]` on a raw
pointer is `i * 8`, and that multiply gets its own `imul`/`jo` every iteration — as does `i * 4` for
each of the other two columns. No overflow policy has ever been about a byte offset.

`PointerCheckedWalked` does identical arithmetic with pointer increments instead of indexing, so
nothing can overflow and no check is emitted for it. It lands on `SpanChecked` to within 2%.

**The decision: scope `checked` to the value expression, or index through a `Span`, and never wrap a
loop body that indexes a raw pointer.** This is a footgun with a named fix and it belongs wherever the
overflow policy is stated. The two numbers must not be recorded as one.

### On the second machine — every conclusion survives, and two get stronger

24 MB against the M4 Pro's 126.5 GB/s copy traffic gives an **ideal of 0.190 ms**.

| Variant | Mean | vs ideal | vs unchecked | vs unchecked (desktop) |
|---|---:|---:|---:|---:|
| `SpanUnchecked` | 377.7 µs | 1.99× | 1.00 | 1.00 |
| `PointerUnchecked` | 417.9 µs | 2.20× | **1.11** | 1.00 |
| `SpanChecked` | 486.9 µs | 2.57× | **1.29** | 1.29 |
| `PointerCheckedWalked` | 503.6 µs | 2.65× | 1.33 | 1.27 |
| `PointerChecked` | 859.3 µs | 4.53× | **2.28** | 1.67 |

**`checked` costs 29% on arm64 against 27% on x86-64, so the overflow policy's price is a property of
the arithmetic and not of the ISA.** That is the useful form of the number, and it means the judgement
`adr/0003` owes — whether 27% counts as "cheap" — can be made once rather than per target.

**The block-scope footgun is more than twice as expensive on arm64.** Indexing a raw pointer inside a
`checked` block costs **95 points on top of the walked form** here against 40 points on the desktop.
The mechanism is the same and the fix is the same; only the price differs, and it differs in the
direction that makes the sentence the overflow policy owes more worth writing, not less.

**`unsafe` does not merely earn nothing on arm64 — it costs 11%.** On the desktop `PointerUnchecked`
and `SpanUnchecked` were within 0.3%; here the raw pointer is the slower of the two. The decision that
`Borough.Core`'s table access does not need `unsafe` now has a machine on which taking `unsafe` anyway
would be a measurable regression.

**The ratio-to-ideal column is not comparable across the two machines, and the reason is worth
recording.** The ideal is a *bandwidth* ideal. On the desktop this loop achieves 91% of the measured
copy ceiling and is genuinely bandwidth-bound, so 1.10× means what it looks like. On the M4 Pro the
same loop achieves **50%** of a ceiling 4.8× higher, which is to say it has stopped being
bandwidth-bound and is now limited by the multiply-accumulate itself. **A bandwidth ideal measures
nothing once the loop is not bandwidth-bound**, so K1's arm64 ratios must not be read as a regression
against the tripwire — see the verdict section, where this is why `PointerChecked`'s 4.53× does not
fire.

**This particular figure divides by the stale denominator and survives it**, which is worth stating
since most of the others do not. Erasing a 91%-against-50% gap would need the M4 Pro baseline to be
overstated by roughly 1.8×, and the baseline is a sustained 256 MiB `memcpy` measured over a ten-second
window — the most robust number in the whole capture and the one least able to be inflated by
background load, which drags a throughput figure *down*. If the baseline is wrong it is wrong low, and
the gap is then wider than stated rather than narrower.

---

## K2 — random gather by generational handle

2,000 handles into the 1,000,000-row tables, three columns each, with the generational check performed
on every one — a wake dereferences `{index, generation}` by loading `generation[index]` and comparing
before it touches anything else. Every handle here is live and every check passes, because the Wheel's
list only holds live entities; what is measured is the validated dereference, not the stale branch.

Handles are drawn from a permutation of all 1,000,000 rows in windows of 2,000, so the tables are swept
in their entirety before any row repeats. Without that, 2,000 handles would touch 384 KiB — inside L3 —
and the kernel would have measured L3 latency and reported it as DRAM.

Ideals are bandwidth ideals against the measured 15.5 GB/s read rate: struct-of-arrays touches three
distinct lines per handle (6,000 × 64 B = 384 KiB), array-of-structs touches one (128 KiB), and the
sequential control touches 40 KB.

| Variant | Mean | ns/handle | Ideal | vs ideal | vs SoA scattered |
|---|---:|---:|---:|---:|---:|
| `SoaScattered` | 27.31 µs | 13.66 | 25.4 µs | 1.08× | 1.00 |
| `SoaSorted` | 25.75 µs | 12.88 | 25.4 µs | 1.01× | 0.94 |
| `AosScattered` | 19.17 µs | 9.58 | 8.5 µs | 2.27× | 0.70 |
| `AosSorted` | 18.94 µs | 9.47 | 8.5 µs | 2.24× | 0.69 |
| `SoaSequential` | 2.81 µs | 1.41 | 2.6 µs | 1.09× | 0.10 |

### The Event Wheel's sparse-wake premise holds, and holds hard

**A scattered gather across a 20 MB working set runs at 1.08× the machine's streaming read bandwidth.**
4.55 ns per cache line is DRAM *bandwidth*, not DRAM *latency* — the core sustains roughly seventeen
outstanding misses, enough that the gather saturates the memory system instead of waiting on it.

[`plans/0004`](../plans/0004-s4-kernel-benchmark.md) put the stakes plainly: *if K2 is close to its
ideal, the Event Wheel's sparse-wake premise holds at scale. If it is not, everything sized against the
Wheel is mis-sized.* It is close to its ideal. **Nothing sized against the Wheel is mis-sized.**

This also settles what `05 §6`'s Factorio rule was pointing at. The rule — *parallelise work that is
compute-dense and read-only; do not parallelise work that is memory-bound and pointer-chasing* — treats
the wake gather as the canonical do-not-parallelise case. It is memory-bound, and it is bound at the
*bandwidth* limit rather than the latency limit, which is a stronger reason not to parallelise it than
the rule gives: extra threads on one memory controller cannot buy back bandwidth that is already
saturated.

### Sorting the wake list is not worth doing

6%, and it moved between 4% and 7% across three captures. The corpus has never decided whether to keep
a bucket's intrusive list ordered by row index, and on this evidence **it does not matter and the
question can be closed cheaply.** The prefetcher cannot exploit an ascending order that is still sparse
across 20 MB, which is why the ordering buys so little.

### Struct-of-arrays survives the random gather, which was the real risk

Struct-of-arrays is the obvious choice for a linear scan and that is why `05 §3` made it. A wake is the
opposite access: three columns of *one* row, which is three cache lines under SoA and one under AoS.
The worry was a 3× penalty on the design's most frequent random access.

**The penalty is 43%, not 3×, and the reason is instructive.** AoS touches a third of the lines and is
only 30% faster because it wins on traffic and loses on parallelism — it is 2.27× its own ideal where
SoA is 1.08× of its. Fewer misses means less to overlap, so AoS spends its time waiting where SoA
spends its time streaming.

**The decision: do not interleave the wake tier into a packed row.** The 30% would cost 33% padding
waste (a 20-byte row padded to 32 so it never straddles a line), the loss of columnar scans over the
same fields, and a second layout to keep consistent with the first. `05 §3`'s choice now stands on a
measurement rather than on an argument.

### On the second machine — the array-of-structs advantage reverses, and the run is confounded

**This is the kernel where the M4 Pro's 16 MiB L2 does the most damage to comparability, and the
numbers say so before any argument does.** Ideals are against 66.5 GB/s read with 128-byte lines:
struct-of-arrays touches 2,000 × 3 × 128 B = 750 KiB, array-of-structs one third of that, and the
sequential control 40 KB as on the desktop.

| Variant | Mean | ns/handle | vs ideal | vs SoA scattered | vs SoA scattered (desktop) |
|---|---:|---:|---:|---:|---:|
| `SoaScattered` | 3.692 µs | 1.85 | **0.32×** | 1.00 | 1.00 |
| `SoaSorted` | 3.429 µs | 1.71 | 0.30× | 0.93 | 0.94 |
| `SoaSequential` | 1.124 µs | 0.56 | 1.87× | 0.30 | 0.10 |
| `AosScattered` | 5.203 µs | 2.60 | 1.35× | **1.41** | 0.70 |
| `AosSorted` | 5.136 µs | 2.57 | 1.33× | 1.39 | 0.69 |

**`SoaScattered` beats its own bandwidth ideal by 3×, which is not possible for a gather that reaches
DRAM.** At **0.615 ns per cache line** — roughly 2.8 cycles — this is cache latency, not memory
latency and certainly not memory bandwidth. The working set is ~20 MB against a 16 MiB cluster-shared
L2, so most of it is resident. **The M4 Pro run does not measure the thing the desktop run measures**,
and the sequential control agrees: it is 0.30 of the scattered case here against 0.10 on the desktop,
because on a machine where the scattered case is already cheap there is far less for sequentiality to
win back.

**So the Event Wheel's sparse-wake premise is *not* independently confirmed here.** That conclusion —
a scattered gather across a 20 MB working set running at 1.08× the machine's streaming read bandwidth
— rests on the desktop alone, and it is the more demanding of the two tests. Reproducing it on Apple
Silicon would need a working set several times the L2, which is a different kernel and is not worth
building for a spike that has already answered the question on the machine where the answer is
harder.

**Array-of-structs reverses cleanly, and it is the one conclusion in S4 that does.** AoS is 30%
*faster* on the desktop and **41% slower** here. The cause is the same mechanism read from the other
end: AoS wins on traffic and loses on memory-level parallelism, and a machine where traffic is nearly
free collects only the loss. **The decision is unaffected and is better supported than before** —
`05 §3`'s struct-of-arrays choice was at risk only from the desktop's AoS advantage, that advantage is
the strongest case AoS makes anywhere in S4, and it does not survive a change of machine. Interleaving
the wake tier into a packed row would have bought 30% on one target and cost 41% on the other, on top
of the padding waste and the second layout.

**Sorting the wake list is worth 7% on both machines**, which is the tightest agreement in this
section and closes that question on two samples rather than one.

---

## K3 — bulk copy of the K0 footprint

Source and destination are one contiguous block each and the columns are offsets into them, so every
variant moves identical bytes across identical addresses and **only the call structure differs**.
Allocating the columns separately would have made this a comparison of two allocation layouts as well,
which is a different kernel.

**Captured under the canonical `performance`+turbo**, with the `powersave` capture retained beside it.
The ideal is governor-specific because the denominator is: 180.65 MB against 13.3 GB/s sustained copy
gives **13.58 ms** under `performance`, and against 12.9 GB/s gives **14.00 ms** under `powersave`.

| Variant | Mean (canonical) | vs ideal | Mean (`powersave`) | vs ideal | vs single block |
|---|---:|---:|---:|---:|---:|
| `SingleBlock` — one call | **13.90 ms** | 1.02× | 14.30 ms | 1.02× | 1.00 |
| `Chunked` 8 MiB | 14.16 ms | 1.04× | 14.71 ms | 1.05× | 1.02 |
| `Chunked` 32 MiB | 14.29 ms | 1.05× | 14.72 ms | 1.05× | 1.03 |
| `PerColumn` — 104 calls | **17.18 ms** | 1.27× | 17.70 ms | 1.26× | 1.24 |
| `Chunked` 1 MiB | 18.97 ms | 1.40× | 19.49 ms | 1.39× | 1.36 |
| `Chunked` 64 KiB | 19.06 ms | 1.40× | 19.48 ms | 1.39× | 1.37 |

**Absolutes improve 2–3% and not one ratio moves** — the same result the governor sweep produced for
K1, K2 and K5, now confirmed for the kernel that most needed it, since K3 is the only kernel whose
verdict is an absolute judged against an asserted band rather than a ratio.

K0 predicted this figure by division and said *K3 will measure this directly rather than deriving it
from the denominator, which is the point of having K3 at all.* That turned out to be the right
instinct, because **the copy is not one number.**

### The mechanism, measured rather than assumed

A 3.4 ms gap across ~104 calls would have to be 33 µs per call to be call overhead, which is absurd.
The chunk sweep exists to find what it actually is, and it finds a **clean step between 1 MiB and
8 MiB worth 33%**. The arithmetic names it:

- At 13.90 ms the copy moves 361 MB — source read plus destination written, two streams — at
  26.0 GB/s against the baseline's measured 26.6 GB/s copy traffic.
- At 19.06 ms it moves roughly 542 MB. **Three streams.** Below the threshold the copy stops using
  non-temporal stores and reads each destination line for ownership before overwriting it, and that
  read is pure waste on a line that is about to be entirely replaced.

`PerColumn` sits between the two because the columns straddle the threshold: a few are tens of MB, most
are one or two.

### The decision: this constrains `Core`'s allocator, not its copy

[`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md) asserts an **8–15 ms band** for the
async save's copy. On this machine, only the arena copy is inside it.

**If `Borough.Core` arena-allocates its table columns from one contiguous region, the save makes one
call and 13.9 ms is the price. If each column is its own allocation, the save cannot, and pays 24% to
land at 17.2 ms — outside the band `adr/0037` asserted.** That is a constraint on how slice 4 lays out
its tables, and it is worth writing down before slice 4 starts rather than being discovered by it.

**The canonical capture was taken specifically because this verdict sat close to a band edge, and it
holds with more room than before.** Under `powersave` the arena copy cleared the 15 ms ceiling by 4.7%;
under the canonical configuration it clears it by **7.3%**. The per-column copy remains outside the band
under both. The margin is larger, the conclusion is unchanged, and it is no longer resting on the wrong
governor.

### On the second machine — the 24% penalty is a property of this desktop, not of the design

**This is the kernel the second machine changed most, and its central finding is denominator-free.**
180.65 MB against the M4 Pro's 63.3 GB/s sustained copy gives an **ideal of 2.854 ms**. At
`SingleBlock` the run moves 361.3 MB of traffic in 3.077 ms — **117.4 GB/s against a 126.5 GB/s
ceiling, 93%** — which says this is genuinely memory-bound rather than cache-served as K2 was.
**That 93% is provisional on the stale denominator**; the finding below is not, because it is a ratio
between two variants in the same process.

| Variant | Mean | vs ideal | vs single block | vs single block (desktop) |
|---|---:|---:|---:|---:|
| `SingleBlock` — one call | 3.077 ms | 1.08× | 1.00 | 1.00 |
| `Chunked` 32 MiB | 3.078 ms | 1.08× | 1.00 | 1.03 |
| `Chunked` 8 MiB | 3.079 ms | 1.08× | 1.00 | 1.02 |
| `PerColumn` — 104 calls | 3.105 ms | 1.09× | **1.01** | **1.24** |
| `Chunked` 1 MiB | 3.119 ms | 1.09× | 1.01 | 1.36 |
| `Chunked` 64 KiB | 3.868 ms | 1.36× | 1.26 | 1.37 |

**The per-column penalty is 24% on the desktop and 1% here.** The threshold that causes it exists on
both machines — the 64 KiB row loses 26% here, which is nearly the desktop's 37% — but it **sits at a
different size**: between 1 MiB and 8 MiB on the desktop, and somewhere below 1 MiB on the M4 Pro.
`PerColumn` is not a fixed cost. It is a bet on where the host's threshold falls relative to the
column sizes, and the K0 schema's columns — a few tens of MB, most one or two — straddle the desktop's
threshold and clear the M4 Pro's.

**The mechanism does not transfer, and it is more honest to say so than to assume it.** On the desktop
the three-stream arithmetic closes exactly: 542 MB at 19.06 ms is 28.4 GB/s against a 26.6 GB/s
ceiling. Applying the same reading here gives 542 MB in 3.868 ms = 140 GB/s, which is **above** the
machine's measured ceiling and therefore cannot be what is happening. Something else costs the M4 Pro
26% at 64 KiB — per-call overhead at ~287 ns across 2,757 calls would account for it, but that was not
measured and is not asserted here. **The M4 Pro's small-copy penalty is observed and its cause is
unidentified.**

**The revised decision: arena-allocate, and take the argument from portability rather than from 24%.**
The 24% is real on the machine it was measured on and absent on the other, so quoting it as *the* cost
of per-column allocation would be recording a fact about a DDR4-2133 desktop as a fact about the
design — the exact failure the two-machine rule exists to catch, and the second time S4 has caught it.
What survives on both machines is the shape: **one contiguous arena makes the save's cost independent
of where the host's copy threshold sits, and per-column allocation makes it a host-dependent number
between 1% and 24%.** That is a better reason to arena-allocate than the penalty was, because it does
not expire when the target hardware changes.

**What this does to the constraint on slice 4:** it softens from a precondition to a strong default.
`adr/0037`'s band is met with a single block on both machines and missed with per-column allocation on
one, so slice 4 should arena-allocate — but a slice-4 design that finds arena allocation genuinely
expensive to build is no longer choosing between compliance and non-compliance, it is choosing to make
the save cost host-dependent. That is a decision someone can now take knowingly.

**One thing `adr/0037`'s band does not survive intact.** Both M4 Pro figures are ~3 ms, well *below*
the band's 8 ms lower bound. The band was stated without naming a machine, which is the same defect
already recorded above for the 15.6 ms Tick budget: a range meant as *acceptable* reads as *expected*,
and a fast host simply beats it. The ADR should say which machine class the 8–15 ms describes.

### The XMP re-sweep is now a refinement, not a gate

The deletion of `spikes/S4.Kernels/` was held for one stated reason: these DIMMs are rated 3200 MT/s
and are running at **2133** with XMP off, so `adr/0037`'s band — and the allocator constraint above —
were being judged against a machine not running to its own specification.

**The second machine has answered the question the re-sweep was being held for.** The question was
whether the per-column penalty is a property of the design or of this box; it is a property of this
box. A re-sweep at 3200 would move the desktop's threshold and shrink its penalty, which is the same
conclusion by a weaker route.

**What it would still buy, and why it is worth doing eventually:** the desktop is the more
representative target — a player's machine is x86 with DIMMs, not an M4 Pro — so knowing whether *it*
meets the band at its own rated speed has value the M4 Pro cannot supply. The arithmetic predicts
~9.5 ms and ~11.7 ms at 1.5× the bandwidth, both inside the band, which would soften the constraint
further. **That is a prediction and not a measurement, and it is recorded here as a prediction.**

---

## K4 — many lookups into small sorted arrays

2,000 scattered lookups into 200,000 ResourceMaps — the Buildings and Businesses that carry a `bins[9]`
column — at no more than nine entries each, matching the nine Resources under
[`adr/0031`](adr/0031-one-resource-abstraction-and-depth-not-count.md). Each map holds a *subset*: a bakery holds Flour and Bread, not
all nine, so filling every map would have collapsed the lookup into an array index and measured nothing.

Binary search is deliberately absent. [`plans/0004`](../plans/0004-s4-kernel-benchmark.md) says this is
*cache behaviour, not algorithmic — at nine entries the complexity class is irrelevant and only the
layout matters*, so what is measured is three layouts and the banned alternative.

A row is 81 bytes at a stride of 81 and therefore always straddles two cache lines: 2,000 × 2 × 64 B =
256 KiB against 15.5 GB/s gives an **ideal of 16.9 µs**. **Captured under the canonical
`performance`+turbo**, with `powersave` retained beside it. Read bandwidth is the one denominator the
governor barely touches — 15.5 against 15.6 GB/s — so K4's ideal is effectively governor-independent
and the absolutes improve 3–8% on their own.

| Variant | Mean (canonical) | ns/lookup | vs ideal | Mean (`powersave`) | vs interleaved |
|---|---:|---:|---:|---:|---:|
| `KeysThenValuesVector` | **34.76 µs** | 17.4 | 2.06× | 35.66 µs | 0.62 |
| `KeysThenValues` | 53.87 µs | 26.9 | 3.19× | 55.07 µs | 0.95 |
| `EntryInterleaved` | **56.52 µs** | 28.3 | **3.34×** | 58.56 µs | 1.00 |
| `DictionaryLookup` | 100.77 µs | 50.4 | — | 109.01 µs | 1.78 |

### The no-hash-maps rule costs nothing — it pays 78%

The banned shape is the slowest thing in the kernel by a wide margin. `Dictionary` is barred from
simulation code for enumeration-order determinism as much as for speed, and the rule has always been
argued on determinism; **it turns out not to need the argument.** A hash of the key, a probe into a
structure with no locality to the caller's access pattern, and a bucket chase cost more than scanning
nine bytes ever could.

### The plan predicted cache behaviour; it is branch behaviour

This is the one place S4 contradicts its own framing, and the contradiction is the useful part.

- Moving the nine keys to the front of the row — **the pure layout change**, same 81 bytes, same
  stride, same memory, different order within it — buys **5%**.
- Comparing all nine keys in a single 128-bit operation and masking by the entry count — **removing
  the data-dependent branch** — buys **38%**.

The scan is short enough that the mispredict, not the line, is what costs. Which entry matches is
unpredictable by construction, and a branch predictor cannot learn a subset that differs per Building.

### The decision: change `bins[9]`, and vectorise the probe

`WorldSchema`'s `bins[9]` is entry-interleaved — nine `{resource, amount, capacity}` entries packed end
to end, keys spread across the row at a stride of nine. **It is the slowest of the three permitted
layouts.** Keys-then-values within the same 81 bytes is never worse and costs nothing to adopt, because
it is the same memory in a different order; `05 §3` owns layout and should say which. The vector probe
is a further 35% on top and needs no layout change beyond that one.

**The layout change is worth 5% on x86-64 and nothing at all on arm64** (see below), so the reason to
adopt it is that it is free and that the vector probe wants the keys contiguous anyway — not the 5%.
**The vector probe is the finding here; the relayout is what makes it expressible.**

### One row sits inside the tripwire band, and the report must say so

`EntryInterleaved` at **3.34×** is inside the tripwire's ~3–4×. Recorded here with both arguments
intact, because the wire was written before the numbers arrived precisely so it could not be reasoned
around afterwards:

- **Against firing:** the remedy the tripwire names is to write the kernel in a second language. The
  measured cause is branch misprediction, which is not a property of the language, and the C# vector
  form recovers it to 2.06× without leaving C#. A second implementation would learn nothing that the
  fourth variant has not already shown.
- **For firing:** 3.34× is inside the band as written, and the band was written to be applied rather
  than argued with.

**The verdict taken: the wire does not fire, on the ground that its remedy cannot address its cause.**
The reasoning is recorded above so that a later reader can disagree with it on the evidence rather than
having to reconstruct it.

### On the second machine — the tripwire row reproduces, and so does the argument against firing

The straddle arithmetic differs by machine and has to be redone rather than carried over. An 81-byte
row at a stride of 81 **always** crosses a 64-byte line, but crosses a 128-byte line only when its
offset exceeds 47 — 80 cases in 128, so **1.625 lines per lookup** rather than 2. That gives
2,000 × 1.625 × 128 B = 406 KiB against 66.5 GB/s, an **ideal of 6.26 µs**.

| Variant | Mean | ns/lookup | vs ideal | vs ideal (desktop) | vs interleaved |
|---|---:|---:|---:|---:|---:|
| `KeysThenValuesVector` | 12.87 µs | 6.4 | **2.06×** | **2.06×** | 0.58 |
| `KeysThenValues` | 22.10 µs | 11.1 | 3.53× | 3.19× | **1.00** |
| `EntryInterleaved` | 22.12 µs | 11.1 | **3.54×** | **3.34×** | 1.00 |
| `DictionaryLookup` | 33.72 µs | 16.9 | 5.39× | — | 1.52 |

**The vector form lands on 2.06× of ideal on both machines — and that agreement should be distrusted
rather than celebrated.** It is the closest agreement anywhere in S4, it divides by a denominator
measured 43 hours earlier on an uncontrolled machine, and a figure that lands on three matching
significant figures through that much noise is more likely to be a coincidence than a measurement.
**The claim to make instead is the denominator-free one:** the vector probe recovers `EntryInterleaved`
by a factor of **1.72 here against 1.63 on the desktop**, both within a single process, and that is
what puts the tripwire argument on two ISAs rather than one.

**`EntryInterleaved` sits inside the tripwire band on both machines — 3.34× and 3.54× — which is the
outcome that most strengthens the verdict taken above.** Had the row been a property of Comet Lake's
branch predictor it would have moved; it did not. A mispredict costs what a mispredict costs, the
remedy the wire names is a second *language*, and the second *machine* has now confirmed that the
cause is not one a language change addresses. The wire still does not fire, and the reason it does not
fire has been tested rather than argued.

**The pure layout change buys nothing on arm64.** `KeysThenValues` and `EntryInterleaved` are 22.10
and 22.12 µs — a 0.1% difference inside the error bars, against 5% on the desktop. **"Strictly better"
was too strong and has been corrected above**: it is better on x86-64, neutral on arm64, and worse
nowhere, which is still enough to adopt it but is not the reason to.

**The no-hash-maps rule pays 52% here against 78% on the desktop.** The margin narrows and the
ordering does not: `Dictionary` remains the slowest thing in the kernel on both machines, at 5.39× of
ideal where the shape the design commits to is at 2.06×.

**One caveat, stated because K2's was not obvious either.** The 200,000 ResourceMaps are 16.2 MB
against a 16 MiB cluster-shared L2, so residency here is marginal in a way it is not on the desktop.
That would inflate cache-driven differences and deflate none of them — and the measured result is that
the layout difference vanished while the branch difference held, which is the opposite of what
residency would manufacture. **The finding is robust to the caveat**, which is why it is recorded as a
caveat rather than as a confound.

---

## K5 — wheel bucket drain and reschedule

Across 8,192 buckets, matching `WHEEL_SIZE`, with **1,560,000 scheduled entities** — every table in the
schema carrying a `wheel_next` column, which is Citizens, Households, Buildings and Businesses. The
Wheel's own state is the bucket heads, the intrusive link on every scheduled entity, and that entity's
`next_event_tick`: 32 KiB, 6.2 MB and 12.5 MB, so **~18.7 MB against a 12 MB L3**. It does not fit, and
would not at half the population.

What this kernel excludes is the woken entity's wake-tier columns. That gather is K2, deliberately, so
that neither number can be misattributed to the other; the Tick pays both and they are composed below.

Reported per woken entity, so the figures compare across wake rates that differ by a factor of sixteen.
The floor is measured rather than asserted: identical reschedule arithmetic, identical scattered write
into the bucket heads, identical loads, with the entities visited in index order instead of by chasing
`wheel_next`.

| Mean wake interval | Wakes/Day | `Revolution` | `SequentialFloor` | Chase penalty |
|---:|---:|---:|---:|---:|
| 4096 Ticks | 2 | 32.61 ns | 10.61 ns | 3.07× |
| 1024 Ticks | 8 | 32.94 ns | 10.63 ns | 3.10× |
| 256 Ticks | 32 | 32.37 ns | 10.58 ns | 3.06× |

**The Wheel costs ~32.6 ns per woken entity and the pointer chase is 3.1× of that.** The per-wake cost
is flat across the whole range, which is itself a result: the Wheel's cost is linear in wakes, and the
wake rate is the only lever on it. The 3.1× is a *floor* on the real penalty, because both variants pay
the reschedule hash inside the timed loop.

**These figures replace an earlier capture of the same configuration that read ~35 ns, and the
replacement is a small lesson in reading error bars.** `Revolution` moved 9% between two runs of the
identical `performance`+turbo configuration while `SequentialFloor` moved under 2% — and the earlier
capture carried a standard deviation up to **1.77 ns on `Revolution` against 0.10 ns on the floor**,
where this one is inside 0.66 ns throughout. The chase is latency-bound and therefore the noisier of the
two by construction; the tighter capture is the one to trust, and the earlier 3.3× was an artefact of
the noisy numerator rather than a real difference. **No conclusion moves** — the cost is flat across
wake rates, the chase dominates the floor by ~3×, and the composed Tick cost below stays under 2%.

Nothing in the corpus had ever put a number on this. The Event Wheel is described as the single largest
performance lever in the project, and until now its overhead was unmeasured.

### The wake rate was under-counted by up to 32×, and it is an unratified input

It is tempting to reason that 1,560,000 entities over 8,192 buckets is ~190 wakes per Tick. **That is
wrong, and the error is worth stating because it is easy to make.** Bucket occupancy is only uniform if
the reschedule delay is uniform over the whole ring, and it is not — occupancy is triangular. An entity
that wakes every M Ticks is drained 1/M of the time, so **the drain rate is N/M per Tick, not
N/8,192**: 381 wakes at a 4,096-Tick mean interval, 6,094 at 256.

**The mean wake interval is therefore an unratified input with a 32× range that drives the Wheel's
entire cost**, and it belongs in [`plans/0002`](../plans/0002-open-questions.md) beside the other
figures task 2 had to guess at.

### On the second machine — flatness travels, and the chase costs slightly more

| Mean wake interval | `Revolution` | `SequentialFloor` | Chase penalty | Penalty (desktop) |
|---:|---:|---:|---:|---:|
| 4096 Ticks | 8.458 ns | 2.361 ns | 3.58× | 3.07× |
| 1024 Ticks | 8.425 ns | 2.366 ns | 3.56× | 3.10× |
| 256 Ticks | 8.624 ns | 2.367 ns | 3.64× | 3.06× |

**The result K5 exists to establish reproduces exactly: the per-wake cost is flat across a 16× range
of wake rates on both machines.** The Wheel's cost is linear in wakes and the wake rate is the only
lever on it — that is now a two-machine finding, and it is the one the design leans on, because it is
what makes the unratified mean wake interval a *scalar* on the Wheel's cost rather than a shape
change.

**The chase penalty is 3.6× here against 3.1× on the desktop**, so the corpus should carry it as
**~3–3.6×** rather than as 3.1×. It moves in the direction the mechanism predicts: the chase is
latency-bound, the M4 Pro's advantage is overwhelmingly in bandwidth rather than in dependent-load
latency, so the floor speeds up more than the chase does. **Pointer chasing is the one thing the
faster machine is not much better at**, which is worth knowing about a structure the corpus calls its
single largest performance lever.

**The Wheel is 3.8× cheaper per wake in absolute terms** — 8.5 ns against 32.6 ns — and this run is
the most stable in S4, with `SequentialFloor` inside 0.003 ns across all three rates.

**The same residency caveat applies and matters less here.** The Wheel's own state is ~18.7 MB against
a 16 MiB L2, so the M4 Pro figures are partly cache-served. But both variants traverse the *same*
working set with the same reschedule arithmetic, and the penalty is a ratio between them, so
residency inflates both numerators equally. The 3.6× is the comparable part; the 8.5 ns is not.

---

## What K2 and K5 compose to — the Tick's wake cost

K5 is the Wheel's own cost and K2 is the woken entity's payload gather. Composed, against the **15.6 ms
Tick budget at 4× speed**:

| Wakes/Day | Wakes/Tick | Wheel (K5) | Gather (K2) | Total | of the Tick budget |
|---:|---:|---:|---:|---:|---:|
| 2 | 381 | 12.4 µs | 5.3 µs | 17.7 µs | 0.11% |
| 8 | 1,523 | 50.2 µs | 21.0 µs | 71.2 µs | 0.46% |
| 32 | 6,094 | 197.3 µs | 84.2 µs | 281.5 µs | **1.80%** |

**The conclusion survives a 32× correction to its own input.** Even at the most pessimistic wake rate
the design plausibly wants — every Citizen waking thirty-two times a Day — the Wheel and the wake
gather together cost under 2% of the Tick.

Combined with K0's finding that only 13% of the world is addressable on an ordinary Tick, this closes
the question K0 opened: **the Tick is not bandwidth-bound and it is not wheel-bound.** What remains is
per-Tick compute, the routing graph, and the Microscopic Cap — none of which S4 measures, and all of
which are S2's and S0's to answer.

**On the M4 Pro the same composition gives 0.03%, 0.10% and 0.40%** — 8.5 ns per wake against 1.85 ns
of gather. That is recorded for completeness and **carries no weight in the conclusion**, because both
of its inputs are partly cache-served on that machine and the figure is therefore an optimistic bound
rather than a measurement. **The desktop's 1.80% is the number to design against**: it is the slower
machine, both of its inputs are honestly DRAM-bound, and a budget validated on the fastest box to hand
is the failure this section has already recorded twice.

---

## Against the tripwire — the verdict

[`plans/0004`](../plans/0004-s4-kernel-benchmark.md)'s table, applied to K0–K6. **Every row is now
measured and no row fires.**

| Condition | Status |
|---|---|
| Any kernel worse than ~3–4× off its hand-computed ideal | **One row inside the band and argued above: K4's `EntryInterleaved`, at 3.34× on the desktop and 3.54× on the M4 Pro. Taken as not firing, on the ground that the remedy the wire names — a second language — cannot address a branch mispredict, and the second machine has now tested that ground rather than leaving it argued.** Next worst among shapes the design uses is K1's `SpanChecked` at 2.57× on the M4 Pro. Two rows exceed the band and neither is a candidate: K1's `PointerChecked` at 4.53× on arm64 is the footgun variant the overflow policy exists to forbid, and K4's `DictionaryLookup` at 5.39× is banned outright |
| **K6 p99.9 exceeds 15.6 ms** with the heap already pure unmanaged structs | **Does not fire, in any of the four GC configurations, on either machine.** Worst unmanaged p99.9 is 5.664 ms (desktop) and 1.108 ms (M4 Pro); worst single iteration is 6.984 ms and 5.626 ms. Across **6,062,762** unmanaged iterations on two machines, **zero** over budget. **The trigger's statistic is itself unsound and `adr/0036` should restate it** — see K6 |
| Everything within tolerance | On the desktop, every shape the design actually commits to is between **1.02× and 1.42×** of its ideal — columnar scan, columnar gather, arena copy, intrusive-list drain. On the M4 Pro the same shapes run **1.08× to 2.57×**, and the widening is a property of the *ideal* rather than of the code: a bandwidth ideal stops meaning anything once a 4.8× faster memory system leaves the loop compute-bound, which K1 shows directly |

**The verdict on S4: the design's shapes are within tolerance on both machines, and `adr/0036`'s language
decision stands on measurement rather than on argument alone.** That was the expected outcome, and a
spike whose expected outcome arrives is still worth its cost only if it produced something the corpus did
not already know. This one produced eight such things:

1. `checked` costs **27%**, not nothing — `adr/0003` asserted cheap without arithmetic (K1).
2. `checked` is a *block*, so on a raw pointer it silently prices the address arithmetic too — a further
   34 points, and a footgun the overflow policy should name (K1).
3. The async save's cost under per-column allocation is **host-dependent between 1% and 24%**, not a
   fixed 24%; `adr/0037`'s band is missed on the desktop and cleared on the M4 Pro. The reason to
   arena-allocate is that it removes the host dependence, not that it buys a number (K3).
4. The ResourceMap's cost is a **branch mispredict, not a cache line** — so the fix is vectorising the
   probe, not relayouting, and `WorldSchema`'s `bins[9]` is the slowest legal shape (K4).
5. The Wheel's wake rate had been under-counted by up to **32×**, and the corrected rate is an unratified
   input driving the Wheel's entire cost (K5).
6. `adr/0036`'s own revisit trigger **cannot detect the failure it names** — p99.9 ranks the rejected
   design above the chosen one in half the desktop's GC matrix, and on the M4 Pro separates the two arms
   by at most **2.3%** while max separates them by up to **36.8×** (K6).
7. **A ratio against a hand-computed ideal is only a verdict while the ideal binds.** K1 is 91% of the
   desktop's copy ceiling and 50% of the M4 Pro's, so the same loop is bandwidth-bound on one machine
   and not on the other, and its ratio-to-ideal degrades from 1.10× to 1.99× without the code changing.
   The tripwire is written in those ratios. **It needs to name the machine class it applies to**, or it
   will fire on a fast host for the same reason it stays quiet on a slow one (K1, K2, K3).
8. **Server GC's effect on the core reverses between hosts** — 2.8× worse on the desktop, 5.1× better on
   the M4 Pro — so `05 §6` can adopt only half of the GC recommendation as a constant. Background
   collection on is prohibition-grade on both machines; server versus workstation is a host setting
   worth up to 6.7× in either direction (K6).

Findings 6 and 7 are the ones worth dwelling on, and they are the same failure at two scales: a tripwire
that would not have tripped is a tripwire that was never protecting anything, and a tripwire whose
threshold moves with the host is one that will trip for the wrong reason. It took running S4 on two
machines to find either.

**Findings 3 and 8 are also the same finding twice**, and the repeat is what makes it a pattern rather
than an anecdote: on both occasions the single-machine reading named a winner, the second machine
reversed it, and the decision that survived was the one that *removes* the host dependence rather than
the one that picks a side. That is now the default posture for any S4 number that a configuration knob
can move.

---

## K6 — the GC tail

Ten minutes per run, eight runs: **four GC configurations × two heap arms**, ~430,000 iterations each.
One iteration is K1's scan, K2's 2,000-handle gather, K4's 2,000 lookups and K5's 2,000-wake drain —
about 1.35 ms, dominated by K1. Not pinned: server GC places a heap and a collection thread per core,
and pinning would have measured a server GC that was never allowed to be one. Captured under
`powersave`+turbo; K1/K2/K5 established that the governor moves no ratio, and K6 is about pauses rather
than bandwidth, so it matters less here than anywhere — but this is not the canonical configuration.

**Two arms, differing in nothing but the live set.** `unmanaged` is the design as `adr/0036` specifies
it — the 172.3 MiB world in native memory. `managed objects` is the counterfactual `adr/0004` and
`adr/0036` rejected: ~1.56M linked class instances holding the same data, the shape that makes a gen2
mark expensive. Both take identical churn.

**The churn rate is an assumption, and it is the most challengeable number in this kernel.** 64 KiB per
iteration with one object in sixteen promoted — **44 to 52 MB/s achieved** — modelling the shell, the UI
and the per-frame snapshot. Nothing in the corpus states what that figure should be. Without churn there
is no collection and no pause whatever is held live, so this one number sets the scale of every result
below. The report prints the achieved MB/s beside every run so the assumption can be argued with rather
than inherited.

| GC | Arm | p99.9 | p99.99 | max | gen2 | Total pause | Over 15.6 ms |
|---|---|---:|---:|---:|---:|---:|---:|
| workstation, non-concurrent | unmanaged | 2.549 ms | 3.024 ms | 5.088 ms | 1 | 2,127 ms | **0** of 453,577 |
| workstation, non-concurrent | managed | 2.462 ms | 2.757 ms | **100.200 ms** | 2 | 2,125 ms | 3 of 437,876 |
| workstation, background | unmanaged | 2.187 ms | 2.771 ms | 5.073 ms | 419 | 2,767 ms | **0** of 435,775 |
| workstation, background | managed | 2.197 ms | 3.693 ms | **34.425 ms** | 11 | 2,521 ms | 2 of 435,465 |
| server, non-concurrent | unmanaged | **5.664 ms** | 5.914 ms | 6.854 ms | 1,070 | 5,990 ms | **0** of 428,081 |
| server, non-concurrent | managed | 2.693 ms | 2.855 ms | **71.741 ms** | 4 | 1,220 ms | 5 of 433,579 |
| server, background | unmanaged | 2.495 ms | 2.754 ms | 6.984 ms | 535 | 3,078 ms | **0** of 427,456 |
| server, background | managed | 2.692 ms | 2.835 ms | 15.574 ms | 4 | 968 ms | **0** of 432,590 |

### `adr/0036`'s trigger does not fire, under any of the four configurations

**The unmanaged arm never exceeded the Tick budget once, across 1,744,889 iterations and all four GC
configurations.** Its worst single iteration in eighty minutes of running was **6.984 ms — 2.2× inside**
the 15.6 ms the trigger names — and its worst p99.9 was 5.664 ms, 2.8× inside.

`adr/0036` chose C# for the core partly on the assertion that GC pauses are manageable once the hot
tables are unmanaged structs. **That assertion holds, and the second arm is what makes it a measurement
rather than an absence of evidence.** Hold the same data as ~1.56M linked objects instead, take
identical churn, change nothing else, and the worst iteration is **100.200 ms — 6.4× past the budget** —
with ten over-budget iterations across the matrix against the unmanaged arm's zero. Same machine, same
churn, same work: **the discipline is doing this, not the hardware.**

### The trigger measures the wrong quantile — and in half the matrix it inverts the answer

This is the more useful finding, and the full matrix makes it far sharper than the half-matrix did.

`adr/0036` names **p99.9**. Read the trigger's own statistic across the two arms, configuration by
configuration:

| GC | unmanaged p99.9 | managed p99.9 | p99.9 says | unmanaged max | managed max | max says |
|---|---:|---:|---|---:|---:|---|
| workstation, non-concurrent | 2.549 ms | **2.462 ms** | managed better by 3% | 5.088 ms | 100.200 ms | managed **19.7× worse** |
| workstation, background | 2.187 ms | 2.197 ms | a tie | 5.073 ms | 34.425 ms | managed **6.8× worse** |
| server, non-concurrent | 5.664 ms | **2.693 ms** | managed better by 2.1× | 6.854 ms | 71.741 ms | managed **10.5× worse** |
| server, background | 2.495 ms | 2.692 ms | managed worse by 8% | 6.984 ms | 15.574 ms | managed **2.2× worse** |

**In two of four configurations, `adr/0036`'s chosen statistic ranks the design the ADR rejected above
the design it chose** — and in the worst of them it does so while that design is stalling for a tenth of
a second. In no configuration does p99.9 separate the two arms by more than 2.1×, while max separates
them by up to 19.7×. The statistic is not merely insensitive; it is anti-correlated with the thing being
measured.

The reason is arithmetic. A ten-minute run is ~438,000 iterations and produces 2 gen2 collections in the
worst-behaved configuration. p99.9 discards the top **438** samples and p99.99 discards the top **44**;
the three over-budget iterations sit at **p99.9993**. **Neither quantile the report prints can see the
event the trigger exists to detect** — the run whose worst iteration is 100.200 ms reads 2.462 ms at
p99.9 and 2.757 ms at p99.99.

**And the converse holds, which is what makes this a statement about the statistic rather than about the
arms.** The one genuinely elevated p99.9 in the whole matrix belongs to an *unmanaged* run — server GC,
non-concurrent, 5.664 ms — where 1,070 gen2 collections and 5,990 ms of total pause make the tail
frequent enough for a quantile to find it. Nothing in that run is remotely near budget; its max is
6.854 ms. **p99.9 detects frequent-and-small. `adr/0036` is worried about rare-and-large. It named the
statistic for the other failure mode.**

**Recommendation for `adr/0036`: restate the revisit trigger as a maximum per run, or as a count of
Ticks exceeding budget over a stated window, not as p99.9.** A GC pause is a rare, large, correlated
event, and rare-large-correlated is precisely what a high quantile smooths away. This report records
max, the over-budget count and the total pause beside the percentiles for that reason.

### Background collection *off* is the one setting that is unambiguously wrong

The half-matrix predicted that turning background collection off would hurt the managed arm materially
and barely move the unmanaged one. **Both halves of the prediction hold, and the first is larger than
predicted:**

| Arm | GC | background on | background off | Change |
|---|---|---:|---:|---:|
| managed | workstation | 34.425 ms | **100.200 ms** | **2.9× worse** |
| managed | server | 15.574 ms | **71.741 ms** | **4.6× worse** |
| unmanaged | workstation | 5.073 ms | 5.088 ms | +0.3% |
| unmanaged | server | 6.984 ms | 6.854 ms | −1.9% |

The mechanism is the expected one and needs no more than a sentence: a non-concurrent gen2 is fully
blocking, so it is paid inside a single iteration; the managed arm has ~1.56M objects to trace and the
unmanaged arm has a few hundred array references. The two unmanaged deltas are inside run-to-run noise
in both directions, which is what "nothing to mark" looks like.

**This is a live risk rather than a curiosity, and that is why it earns a section.**
`<ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>` is exactly the knob a
latency-conscious developer reaches for on the reasoning that background collection adds overhead — and
in this matrix it is the *worst available setting* for the shell and worth nothing to the core. This
spike's own csproj had that property baked in, which is what broke the first sweep. **`05 §6` should
state it as a prohibition, not a preference.**

### Server GC is a lever for the shell, and the core's indifference is conditional

The half-matrix reported that server GC costs the unmanaged arm almost nothing. **With the other half in,
that holds only when background collection is on**, and the corrected claim is narrower.

For the managed arm, server GC helps in both concurrency modes: worst case 34.425 → 15.574 ms with
background on (−55%) and 100.200 → 71.741 ms with it off (−28%), total pause 2,521 → 968 ms (−62%) and
2,125 → 1,220 ms (−43%). **Server plus background is the only run in the managed arm with zero
over-budget iterations.**

For the unmanaged arm the cost depends entirely on the other axis. With background on it is small —
p99.9 2.187 → 2.495 ms, total pause 2,767 → 3,078 ms, +11%. With background off it is not: p99.9
2.549 → 5.664 ms and total pause 2,127 → 5,990 ms, a **2.8× increase**, with gen2 collections rising
from 1 to 1,070. Still nowhere near budget, and still zero over-budget iterations — but "immaterial" was
too strong.

**`05 §6` states no GC configuration and this slice was flagged as owing one. What it should adopt is
server GC with background collection on** — .NET's default for a server workload, the best cell in this
matrix for a large managed live set, and cheap for the core. The narrow decision survives the correction:
*the shell may want server GC; the core is close to indifferent to it, and that indifference is itself a
sign the discipline is working.*

### Why this sweep was run twice

The first sweep asked for four GC configurations and silently ran two, each twice, and its numbers were
discarded rather than kept. `DOTNET_gcServer` overrides `runtimeconfig.json` and **`DOTNET_gcConcurrent`
does not**, and the csproj baked `<ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>` — so
the concurrency axis never varied.

**It was caught because the report prints the *effective* GC settings rather than the requested ones** —
and then only partly, because the first implementation read `AppContext.TryGetSwitch`, which reports what
`runtimeconfig.json` asked for and not what the collector is running. The honest source is
`GC.GetConfigurationVariables()`, key `ConcurrentGC`. Both faults were fixed — the csproj sets neither
property, and the printback reports the effective value — and the fix is visible in the results above:
the unmanaged arm's gen2 count moves from 1 to 419 across the concurrency axis under workstation GC, and
from 1,070 to 535 under server GC. An axis that does nothing does not do that.

The general lesson outlives the spike: **a configuration sweep must report the configuration the system
actually adopted, not the one it was asked for.** Had the label simply echoed the environment variable,
this would have been recorded as four configurations agreeing closely — a tidy and completely false
result, and one that would have hidden the largest number in the matrix.

### On the second machine

Eight runs of ten minutes on the M4 Pro, 2026-08-04 19:59–21:09 UTC, same matrix and same harness.
**1,072,000 iterations per run against the desktop's ~435,000** — the iteration is 2.4× faster
(p50 0.55 ms against 1.36 ms), consistently across all eight cells.

| GC | Arm | p99.9 | p99.99 | max | gen2 | Total pause | Over 15.6 ms |
|---|---|---:|---:|---:|---:|---:|---:|
| workstation, non-concurrent | unmanaged | 1.108 ms | 1.150 ms | 3.915 ms | 1 | 2,227 ms | **0** of 1,092,233 |
| workstation, non-concurrent | managed | 1.104 ms | 1.154 ms | **112.652 ms** | 1 | 2,354 ms | 2 of 1,062,301 |
| workstation, background | unmanaged | 0.972 ms | 1.021 ms | 1.659 ms | 1,163 | 2,999 ms | **0** of 1,074,920 |
| workstation, background | managed | 0.950 ms | 3.661 ms | **22.756 ms** | 54 | 2,886 ms | 2 of 1,083,780 |
| server, non-concurrent | unmanaged | 0.693 ms | 2.045 ms | 5.626 ms | 1 | 434 ms | **0** of 1,077,247 |
| server, non-concurrent | managed | 0.707 ms | 2.049 ms | **206.977 ms** | 1 | 638 ms | 2 of 1,063,321 |
| server, background | unmanaged | 0.696 ms | 2.099 ms | 3.590 ms | 1 | 450 ms | **0** of 1,073,473 |
| server, background | managed | 0.705 ms | 2.088 ms | **55.425 ms** | 1 | 480 ms | 2 of 1,072,727 |

**The M4 Pro is running a harder test, not an easier one.** Churn is per *iteration*, so a 2.4× faster
iteration is 2.5× more allocation pressure per second — **122–126 MB/s against the desktop's 44–52 MB/s**.
Every M4 Pro number below is produced under more GC load per unit wall clock than its desktop
counterpart, which is the direction that makes the better results mean something rather than less.

#### The verdict travels, and the margin widens

**Zero over-budget iterations in the unmanaged arm, again, across all four configurations.** Worst
single iteration in eighty minutes: **5.626 ms, 2.8× inside** the budget, against the desktop's 6.984 ms
and 2.2×. Combined across both machines the unmanaged arm now stands at **zero over-budget iterations in
6,062,762**, and the managed arm at **18 in 6,021,639**. `adr/0036`'s trigger does not fire on either
host, and the positive control fires on both.

The separation between the arms is *larger* here, not smaller:

| GC | unmanaged max | managed max | ratio | desktop ratio |
|---|---:|---:|---:|---:|
| workstation, non-concurrent | 3.915 ms | 112.652 ms | **28.8×** | 19.7× |
| workstation, background | 1.659 ms | 22.756 ms | **13.7×** | 6.8× |
| server, non-concurrent | 5.626 ms | 206.977 ms | **36.8×** | 10.5× |
| server, background | 3.590 ms | 55.425 ms | **15.4×** | 2.2× |

The worst managed stall on this machine is **206.977 ms — 13.3× past budget**, against the desktop's
100.200 ms and 6.4×. The intrusive-index-list rule is buying more on the faster machine than on the
slower one, which is the opposite of the usual shape and follows directly from the churn rate: the
collector is asked to run more often against the same 1.56M-object graph.

#### The quantile finding strengthens, but its *demonstration* does not reproduce

On the desktop, p99.9 was **anti-correlated** with the thing being measured — it ranked the rejected
design above the chosen one in two of four configurations, by as much as 2.1×. On the M4 Pro it is
simply **blind**:

| GC | unmanaged p99.9 | managed p99.9 | p99.9 says | max says |
|---|---:|---:|---|---|
| workstation, non-concurrent | 1.108 ms | 1.104 ms | a tie, 0.4% | managed **28.8×** worse |
| workstation, background | 0.972 ms | 0.950 ms | managed better by 2.3% | managed **13.7×** worse |
| server, non-concurrent | 0.693 ms | 0.707 ms | managed worse by 2.0% | managed **36.8×** worse |
| server, background | 0.696 ms | 0.705 ms | managed worse by 1.3% | managed **15.4×** worse |

**The statistic moves by at most 2.3% in either direction while the quantity it exists to detect moves by
up to 36.8×.** That is a cleaner statement of the defect than the desktop produced, and `adr/0036`'s
restated trigger — any single Tick over budget, with max and over-budget count reported — is now carried
by two machines.

**But one supporting claim in the desktop reading is host-specific and should be read as such.** The
desktop's argument that *p99.9 detects frequent-and-small* rested on a concrete converse: an *unmanaged*
run with an elevated p99.9 of 5.664 ms, driven by 1,070 gen2 collections under server GC with background
off. **That run has no counterpart here** — the same configuration on the M4 Pro produces **1** gen2
collection and a p99.9 of 0.693 ms, the lowest in the matrix. The mechanism described is real and was
observed; it is not a property of the design or of server GC generally, and the sentence should not be
read as predicting it on any host.

#### Background collection off is worse here than the desktop showed

The prohibition holds and gains a second argument. On the managed arm, background off costs **4.95×** on
worst pause under workstation GC (22.756 → 112.652 ms) and **3.73×** under server GC (55.425 → 206.977
ms). On the desktop those were 2.9× and 4.6×.

**And on this machine it costs the unmanaged arm too**, where the desktop recorded it as neutral: worst
pause 1.659 → 3.915 ms under workstation GC (**2.36×**) and 3.590 → 5.626 ms under server GC (1.57×).
Small in absolute terms and nowhere near budget, but consistent in direction across both configurations,
where the desktop's two deltas were ±2% and in opposite directions. **There is now no cell on either
machine, in either arm, where background collection off is better than on.**

#### The server GC axis reverses between the two machines

**This is the conclusion that did not travel, and it is the fourth such finding in S4.** On the desktop,
turning on server GC *cost* the unmanaged arm: total pause 2,127 → 5,990 ms non-concurrent (**2.8×
worse**) and 2,767 → 3,078 ms with background on (+11%). Here it *saves* it, decisively:

| Concurrency | Machine | workstation | server | Effect |
|---|---|---:|---:|---|
| non-concurrent | desktop | 2,127 ms | 5,990 ms | **2.82× worse** |
| non-concurrent | M4 Pro | 2,227 ms | 434 ms | **5.13× better** |
| background | desktop | 2,767 ms | 3,078 ms | 1.11× worse |
| background | M4 Pro | 2,999 ms | 450 ms | **6.67× better** |

The two hosts also disagree about which cell is *best* for the managed arm's worst pause: the desktop
says server plus background (15.574 ms), the M4 Pro says workstation plus background (22.756 ms).

**So `05 §6` should adopt the half of the recommendation that is portable and treat the other half as a
host setting.** Background collection on is a prohibition-grade finding on two machines. Server versus
workstation is worth up to 6.7× in either direction depending on the box, which makes it exactly the kind
of thing that must be a startup configuration read from the host rather than a constant compiled in.
This is the same shape as K3's per-column copy penalty: **the durable decision is the one that removes
the host dependence, not the one that picks the winner on the machine that happened to be to hand.**

#### Conditions, and one thing left unexplained

**This is the quieter of the two captures, which is the opposite of what the M4 Pro's other defects
would suggest.** The M4 Pro was idle for these eighty minutes; **the desktop was running other work
during its own K6 sweep**, and K6 is the one kernel captured unpinned on both machines. So where the
two disagree, the M4 Pro numbers are the better-conditioned ones — and the stale-denominator defect
that makes the M4 Pro's other kernels provisional does not touch K6 at all, because K6 divides by
nothing. **It reports milliseconds against a fixed 15.6 ms budget.**

The 206.977 ms outlier is a collector event rather than an interruption, on three counts. The arms
**alternated** run by run at ten-minute spacing, so no drift or single burst can sort itself by arm —
yet all four managed maxima exceed all four unmanaged maxima, which under random assignment is 1 in 70.
The over-budget counts are **0, 0, 0, 0 against 2, 2, 2, 2**, split the same way. And that run's own
total pause is 638 ms, so the outlier is a third of all collector time in it, which is what one
expensive blocking gen2 over a 1.56M-object graph looks like.

**The over-budget count is exactly 2 in all four managed runs, and that is not explained.** The graph is
built before the clock starts and eight warmup iterations precede it, so it is not construction. The
desktop's counts were 3, 2, 5 and 0 under the same code, so it is not structural either. With counts this
small the useful statement is the sign — nonzero on managed, zero on unmanaged, on both machines — and
not the value.

#### A reporting defect in the harness, found while reading these numbers

The report line *"plus 325.2 MiB of managed objects"* is `GC.GetTotalMemory(precise: false)` — **the whole
managed heap at the end of the run, not the live graph.** The graph is fixed at 1.56M `ManagedEntity`
instances of 40 bytes plus an 11.9 MiB reference array — **about 71 MiB, identical on both machines and in
every cell.** The rest is the 18.3 MiB sample buffer, the kernel setup allocations, and uncollected churn,
which is why the printed figure tracks allocation rate (M4 Pro 233–325 MiB against the desktop's
175–194 MiB) rather than the graph, which does not vary at all.

**The unmanaged arm prints no managed figure whatsoever**, because the harness suppresses it when the
graph is null — so it silently omits that arm's own sample buffer and kernel allocations. The two arms'
live-set lines are therefore not comparable, and neither is a measurement of the thing the arm varies.

No conclusion above moves: what makes the managed arm expensive is 1.56M objects to trace, and that is
what the arm sets and what the gen2 costs reflect. But the figure is stated in a way that invites a reader
to attribute the difference to a live set 3–5× larger than it is. **Recorded rather than fixed — the
harness is scheduled for deletion in task 11, and the raw captures are what survive.**

---

## S2 — the routing ceiling

**R0 through R8 of [`plans/0010`](../plans/0010-s2-routing.md) are done, less R6's invalidation half.**
R0: the synthetic Road Graph, the road density a 268 km² city implies, the uncached point-to-point
denominator, and the heuristic ladder's verdict on admissibility. R1: the travel-time matrix, **and it
carries the choice loop** — the finding R4 was made conditional on. R2: the path source, which revived
the task it was meant to retire. R3: HPA\* and the cluster it owns. R4: distance-vector, out on cost.
R5: the edit storm, the Parking Shed, and the TTL rotation. R6.0–R6.3: the key, the eviction policy,
and the two consumers multiplied. R8: the congestion loop and `adr/0046`'s three layers.

**R7 is this section's closing report and it is in progress.** What it cannot do is **close the
spike**: R6's remaining half is the route cache's *invalidation contract*, which is session **M**'s and
is typed *arguable* under `adr/0043` — no measurement settles it. So `spikes/S2.Routing/` is **not
deleted**, and the deleting commit is not recorded here yet.

~~Raw captures in `spikes/S2.Routing/results/`; both sections are owed a rewrite by R7~~ — raw captures
are still in `spikes/S2.Routing/results/`, and **the rewrite is owed over every section rather than
two**, because what counts as a canonical capture changed underneath the spike while it ran. See the
machine block immediately below.

S2 is the project's top risk and the only one argument cannot close. `adr/0020` makes route
computation the binding constraint on world size — *"the map-size question is the routing question in
disguise"* — and until R0 the corpus had never measured any part of it.

### The machine, and a capture defect closed rather than declared

> **R7: the definition of *canonical* changed while the spike was running, and the capture named below
> no longer meets it.** This block was written on 2026-08-06 and declares its capture canonical partly
> *because* the SMT sibling was idle. **R5.3 then measured that the idle sibling is the defect** — it
> starves the tiered JIT's background compilation of anywhere to run, so it shares the measured core
> with whatever is timed first, 4.88× apart within a single capture. The canonical configuration is now
> `performance` **and** `cpu2+8`, both threads of the physical core, and for R0, R0d, R1, R2, R3 and R4
> **no such capture exists**: the `performance` runs are `cpu2`, and the correctly-pinned re-run of
> 2026-08-09 is `powersave`. Only **R5 and R8** were ever taken under the configuration this corpus now
> calls canonical.
>
> **So every absolute in R0–R4 below is currently mis-pinned, `powersave`, or both**, and the two
> confounds cannot be separated by comparing the captures that exist, because no two of them differ in
> only one variable. `plans/0010` states the standing rule — *R7 must not publish a first-timed
> absolute without re-running the ladder* — and the re-run is the outstanding item. **Counts, in-process
> ratios and every conclusion resting on them are unaffected**, which is not a hope: every count is
> bit-identical across all captures, including `powersave`, and that is the determinism check the debt
> bought on the way past.
>
> **The general lesson is the one worth keeping, and it is about protocol rather than about this
> machine.** A capture is labelled with its configuration so that two captures can be compared. That
> only works while the *set* of configuration variables is fixed. R5.3 added a variable — which threads,
> not just which governor — and in doing so **retrospectively unlabelled every capture taken before
> it**, because they record a value for a dimension nobody knew was a dimension. Nothing in the naming
> scheme could have caught that; the filename was complete against the schema of its day.

Measured **2026-08-06** on the Linux desktop: Intel i5-10400, Ubuntu 24.04.4, .NET 10.0.10, Release.
Every timing below is from `spikes/S2.Routing/results/s2-r0+r1+r2-intel-core-i5-10400-ddr2133-performance-turbo-cpu2-20260806T185053Z.md`
— ~~**the canonical capture**~~ **superseded, see the banner above**: `performance`, turbo enabled,
pinned to one physical core with its SMT sibling idle, taken by
`spikes/S2.Routing/tools/routing-run.sh`, which is S4's `kernel-run.sh` with the BenchmarkDotNet
machinery removed because S2's harness times its own loops.

> **R0's first capture ran under `powersave`, unpinned, and S4's own protocol says not to.** That was
> **the third capture in this corpus to carry a machine-state defect**, after the desktop's background
> load and the M4 Pro's stale denominator, and the first one caught by the harness before anybody drew
> a conclusion from it — R0 prints its own governor, which is how it was known rather than assumed.
> **The debt is now discharged**: R0 and R1 were re-captured together, and then the whole thing was
> captured a second time under the identical configuration fourteen minutes later. **The second run
> was taken for an unrelated reason** — to replace a contention block whose arithmetic was wrong, see
> R1's note below — **and it is the more valuable of the two, because two runs of one configuration
> are an error bar and one run is an assertion.**
>
> **Every count is bit-identical across all three captures**, `powersave` included — Segment and node
> counts, footprint bytes, nodes expanded, non-optimal route counts, unreachable walk counts, and
> every one of R1's asymmetry, Settlement, entry-error and dirty-region figures. Only nanoseconds and
> the ratios taken over them moved. That is the determinism the spike is supposed to have, and nobody
> had checked it.
>
> **The error bar, measured rather than asserted.** Across the two canonical runs, reproducibility
> tracks the size of the quantity being timed. **Drive searches, at 0.4–1.2 ms, reproduce within 2%**;
> R1.2's per-search cost within **3.3%** past the first rung. **Walk searches, at 4–20 µs, reach 8%**,
> and the **bootstrap column — a few hundred nanoseconds recovered by *difference* between two loops —
> reaches 29%**, which is the cost of subtracting two nearly equal timings and is why R0 reports it in
> a column of its own rather than folding it into the search.
>
> **The scattered read's largest move is the rung with a mechanism behind it.** R1.3 at 64 MiB went
> 5.71 → 5.00 ns, **12%** — the largest in that column — while the two L3-scale rungs either side of
> the cliff, 4 MiB and 15.6 MiB, held to **1.2% and 0.7%**. The 64 MiB rung is the only one whose cost
> is set by DRAM bandwidth, **which is exactly what pinning does not protect and what S4 already
> recorded about this machine.** The corroboration is worth more than the number.
>
> **One row moved far more than the governor can explain, and it is recorded rather than smoothed.**
> Driving `None` — plain Dijkstra, the first thing the process times — went from 779,150 ns unpinned
> to 1,278,071 ns pinned under `powersave`, and reads 1,237,578 and 1,240,382 in the two pinned
> `performance` runs. **Those two agree to 0.2%, so this is reproducible rather than noisy, and the
> movement tracks pinning and not frequency** — driving `Chebyshev` moved 0.04% across the same
> change. The standing hypothesis is that `taskset` leaves the process one visible logical processor —
> the report line says so — and the tiered-JIT background compilation that had eleven idle cores to
> work on now shares the measured one, which lands hardest on whatever is timed first. **The second
> capture strengthened it from the other end**: R1.2's *first* rung is also its least reproducible,
> moving 5.8% where every rung after it held within 3.3%. **It is a hypothesis with an obvious check**
> — re-run the ladder in reverse order, or with tiered compilation disabled — **and until that check
> runs, the absolute of any first-timed row is the least trustworthy number in this section.** R1's
> own tiering artefact, below, is the same failure mode caught from the other side.
>
> **Only the second capture survives as a file, and that is why the spread above is prose.** The
> script labels its output by configuration, so two runs of the canonical configuration collide by
> design — and the second is worth taking exactly when the first is worth keeping. `routing-run.sh`
> now archives an existing capture under **its own modification time** before writing, so the suffix
> says when that run was taken rather than when it was displaced. **A defect in the tooling, not in
> the measurement**, and the last one this sitting produced.

### R0.1 — the road density nobody had argued

`CONTEXT.md` → Segment states the working figure and immediately disowns its basis: *"~30,000 Segments
at 1,000,000 Citizens… That figure rests on a road-density assumption nothing in this corpus has yet
argued, and it is spike S2's to replace."* R0 sweeps the block size and reports what each density
implies on a 4096² map, at 4 m per Tile.

| Block | Segments | Nodes | Arcs | Segments/km² | km road/km² | Mean Segment |
|---:|---:|---:|---:|---:|---:|---:|
| 128 Tiles | 2,072 | 1,145 | 4,144 | 7 | 4.19 | 135 Tiles |
| 96 Tiles | 3,539 | 1,905 | 7,078 | 13 | 5.35 | 101 Tiles |
| 64 Tiles | 8,200 | 4,281 | 16,400 | 30 | 8.17 | 66 Tiles |
| 48 Tiles | 14,503 | 7,452 | 29,006 | 54 | 10.76 | 49 Tiles |
| **32 Tiles** | **33,018** | **16,697** | **66,036** | **123** | **16.20** | **32 Tiles** |
| 24 Tiles | 58,408 | 29,297 | 116,816 | 217 | 21.38 | 24 Tiles |
| 16 Tiles | 132,781 | 66,105 | 265,562 | 494 | 32.24 | 16 Tiles |

**The placeholder was never arbitrary, and nobody had noticed why.** ~30,000 Segments is what falls out
of **one Street on every Cell boundary** — the 32-Tile rung — and at that density the mean Segment is
32 Tiles, or 128 m, which is exactly the *"roughly a block-length link"* the definition claims for it.
Two independent statements in `CONTEXT.md` → Segment turn out to be the same statement.

**What R0 does *not* do is discharge the debt.** The quantity that decides whether the density is right
is **16.20 km of road per km²**, and that is a claim about a real city that a benchmark cannot check.
It is on the high side for a target the corpus justifies by citing Los Angeles sprawl at 3,700
people/km². **`CONTEXT.md` → Segment keeps its disclaimer until somebody sources the figure**; what
has changed is that there is now a number to source against, and a curve either side of it.

### R0.2 — the footprint, and what it settles

| Block | Segments | `(saved AND hashed)` | `(derived AND rebuilt)` | Total | Bytes/Segment |
|---:|---:|---:|---:|---:|---:|
| 32 Tiles | 33,018 | 968 KiB | 1.1 MiB | **2.0 MiB** | 66 |
| 16 Tiles | 132,781 | 3.7 MiB | 4.5 MiB | 8.3 MiB | 65 |

**The Road Graph is not a memory problem and this closes the question.** 2.0 MiB at the working
density against K0's **172.3 MiB** for the entire world at 1M Citizens — 1.2% of it. Even at four times
the road density it is 8.3 MiB. Bytes per Segment is flat across the whole sweep, so the structure has
no size-dependent overhead to discover later.

**Slightly over half of it is `(derived AND rebuilt)`** — the CSR offsets, the arc arrays and the
cached per-mode traversal times. Under `adr/0040` that half is free to change forever and is never
written to a save, which is worth separating because it is the half a later optimisation is allowed to
delete outright.

**Per-direction `volume / capacity` costs 5% of the graph, at every rung.** `plans/0010` forbids R0
from settling the per-Segment-versus-per-direction question and requires it parameterised; this is the
price, and it is small enough that the decision should be taken on Stress's behaviour rather than on
storage. What it *buys* is not visible until R2 has volume to attribute.

### R0.3 — the denominator, and the query shape it had to have

The query is `(Segment, offset) → (Segment, offset)`, seeded from **both** endpoints of the origin
Segment at their partial costs and terminated on **either** endpoint of the goal Segment plus the
offset remainder. `plans/0010` is explicit that a node-to-node denominator *"measures a query the game
never issues, and every figure in this spike divides by it."*

Cost is **time** — `02 §5.9`'s SC4 argument — in **Q16.16 Ticks**; see below for why not whole Ticks.
2,000 queries per row, drawn before the clock starts.

| Query | Heuristic | Mean expanded | Bootstrap | Search | ns/expansion |
|---|---|---:|---:|---:|---:|
| drive | `None` (Dijkstra) | 8,217 | 295 ns | 1,240,382 ns | 150 |
| drive | `Manhattan` | 2,813 | 375 ns | 429,300 ns | 152 |
| drive | `Octile` | 3,506 | 256 ns | 474,677 ns | 135 |
| drive | **`Chebyshev`** | **4,121** | **249 ns** | **418,260 ns** | **101** |
| drive | `EuclideanFloor` | 3,712 | 465 ns | 715,282 ns | 192 |
| walk | `None` (Dijkstra) | 276 | 179 ns | 20,391 ns | 73 |
| walk | `Manhattan` | 32 | 246 ns | 4,040 ns | 126 |
| walk | `Octile` | 46 | 217 ns | 5,261 ns | 114 |
| walk | **`Chebyshev`** | **58** | **221 ns** | **6,359 ns** | **109** |
| walk | `EuclideanFloor` | 50 | 358 ns | 10,320 ns | 206 |

**Bootstrap is 180–470 ns and it is fixed overhead the design chose.** It is the price of Access Points
being offsets rather than nodes, and `CONTEXT.md` makes that choice structural — promoting them to
nodes would put the graph at 150,000–300,000 edges instead of ~30,000. Against a drive search of
418 µs it is 0.07%, so **the query shape the corpus committed to costs essentially nothing**, which is
the first evidence either way.

**The denominator's own quality, which `plans/0010` requires stated beside every ratio built on it:**
`Chebyshev` expands **48%** of Dijkstra's nodes driving and **23%** walking, at 68 and 7 expansions per
Segment of returned path. That is a real but not a strong heuristic, and the reason is structural
rather than fixable: a distance heuristic on a time-cost graph must divide by the map's maximum
free-flow speed, so a car search is divided by the Arterial's 90 km/h while nearly every edge it
expands is a 50 km/h Street — loose by 1.8× almost everywhere. **Every speedup S2 later reports against
this denominator is a speedup against a router with that much slack still in it, and should be read
that way.**

#### The result that reverses the obvious choice

**`EuclideanFloor` expands 11% fewer nodes than `Chebyshev` and takes 1.71× as long.** Its exact
integer square root is a sixteen-iteration loop, run twice for every node pushed, and it costs more
than the expansions it saves: 192 ns per expansion against `Chebyshev`'s 101.

> **The first capture said something stronger here and the canonical one withdraws it.** It read
> *"against plain Dijkstra it cuts expansions by 55% and is not faster at all"* — true of the
> `powersave`, unpinned numbers, where `EuclideanFloor`'s 794,940 ns sat just above Dijkstra's
> 779,150. Under both canonical captures Dijkstra costs about 1.24 M ns and `EuclideanFloor` is
> comfortably faster than it, so the sentence is false and is struck rather than restated. **The claim that
> survives is the one between two rungs measured seconds apart in the same loop; the claim that died
> spanned the widest gap in per-expansion cost in the table** — which is the same reason S4 trusts its
> *vs variant* columns and distrusts its absolutes, arriving here from the other side.

**`plans/0010`'s ladder specified nodes expanded, path cost and optimality — and expansions alone pick
the wrong rung.** Adding a clock to the ladder is R0's own amendment to the plan, and it is the finding
most likely to matter to R3: HPA\*'s speedup will be quoted in expansions saved, and this is a measured
case where that currency does not convert.

#### What routing on time costs in the inner loop

Converting a distance to a time is a division, the search evaluates the heuristic against **both** goal
endpoints for every node it pushes, and `Fixed.Div` routes through `IntegerMath.FloorDiv`, which costs
a `/` and a `%`. That is **four 64-bit hardware divisions per node**. Inverting once per query — a
floored reciprocal, multiplied — removes all four and cost the drive search 872 → 776 µs before the
heuristic rung was even chosen.

**This is the first price anyone has put on `02 §5.9`'s commitment**, and the answer is *nothing, if you
invert once per query, and a great deal if you do not.* It belongs with the design rather than with the
spike, because the obvious implementation is the slow one.

### R0.4 — the verdict: the Arterial density at which admissibility breaks

Non-optimal routes returned, against Dijkstra ground truth **on the same query through the same loop**,
by number of freeform Arterials. Driving only — an Arterial carries no pedestrian edges.

| Arterials | Streets severed | `Manhattan` | `Octile` | `Chebyshev` | `EuclideanFloor` |
|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 0 of 300 | 0 of 300 | 0 of 300 | 0 of 300 |
| 2 | 281 | **7 of 300** | 1 of 300 | 0 of 300 | 0 of 300 |
| 4 | 476 | 19 of 300 | 3 of 300 | 0 of 300 | 0 of 300 |
| 8 | 795 | 13 of 300 | 2 of 300 | 0 of 300 | 0 of 300 |
| 16 | 1,401 | 22 of 294 | 5 of 294 | 0 of 294 | 0 of 294 |
| 32 | 2,760 | 36 of 267 | 4 of 267 | 0 of 267 | 0 of 267 |

**The answer is one. Admissibility breaks at the first Arterial.** `plans/0010` anticipated that
`CONTEXT.md` → Arterial's *"deliberately rare"* would make the tight metrics admissible *almost*
always, and named that as the trap. It is worse than the framing suggested: there is no low-Arterial
regime in which Manhattan is safe, only a regime in which it is wrong less often. At the working rung
it returns a different route on **4% of drives**, and under `05 §4`'s test a different route is a
different Trip and therefore **a different city** — a design change, never a tuning knob.

**Octile is not a middle ground.** It fails at two Arterials as well, merely an order of magnitude less
often, which is the worst property a defect can have.

> **The rates are a lower bound, and the reason generalises.** The heuristic converts Tiles to Ticks by
> multiplying by a **floored reciprocal**, which leaves roughly two parts in ten thousand of slack —
> and that slack *partially cancels an overestimating metric's error.* Measured directly: replacing the
> exact division with the reciprocal moved walking `Manhattan` from **35 of 300 to 4 of 300**, while
> leaving driving at 13. Short walks are where the two errors are comparable in size.
>
> So **an optimisation chosen purely for speed made an unsafe heuristic look safer, and did it worst
> exactly where the design cares most** — `adr/0008`'s walk Legs. The verdict above does not rest on
> the rate: Manhattan and Octile overestimate on this graph by construction, and a rate that moves with
> an unrelated optimisation is not the evidence. **A future measurement of an error rate should ask
> what else in the pipeline rounds in the same direction.**

**Decision: `Chebyshev` is the heuristic, and the denominator every later S2 figure divides by.**
Admissible on any graph, no square root, and 1.8× faster than the tightest safe metric. `05 §4`'s rule
that an inadmissible heuristic is a design change rather than a tuning knob is what makes this a
one-line choice rather than a trade-off.

### R0.5 — Severance, and an instrument that was not evidence until it fired

The generator's Arterials genuinely occupy the ground they cross: every Street an Arterial runs over is
deleted, or kept as a designated foot crossing. Walk searches have **no radius bound**, so *no route
found* is Severance rather than an artefact of a cutoff — which is the distinction `plans/0010` requires,
since *severed* and *merely far* are different Trip Fates and different player-facing diagnoses.

| Arterials | Foot crossing every | Crossings | No route found | Mean cost when found |
|---:|---|---:|---:|---:|
| 8 | every severed Street | 1,059 | 0 of 300 | 732.31 Ticks |
| 8 | 4th | 264 | 0 of 300 | 732.30 Ticks |
| 8 | 16th | 66 | 0 of 300 | 750.82 Ticks |
| 8 | never | 0 | 0 of 300 | 837.92 Ticks |
| 32 | 4th | 920 | 0 of 300 | 752.67 Ticks |
| 32 | 16th | 230 | 9 of 300 | 932.18 Ticks |
| 32 | never | 0 | **230 of 300** | 722.71 Ticks |

**The first capture reported zero unreachable walks, and this table exists because zero was not
evidence.** A count that has never been observed to move is equally consistent with *this city is well
connected* and with *this instrument cannot see the thing it is named after* — the same vacuity the
Census's trend assertion was deliberately not written to avoid (`plans/0000` → *Owed*). Sweeping the
crossing density until the count moves is what converts the working rung's zero into a finding.

**Severance is a property of crossing density, not of Arterial count.** Eight Arterials with no
crossings at all sever nothing; thirty-two with none sever almost everything. The parameter that
decides is the one a player controls when they choose whether to build a bridge, which is the right
place for it to live and is an argument for `adr/0008` rather than a measurement of it.

**One column reads backwards and it is not an error.** *Mean cost when found* falls at 32 Arterials with
no crossings — 722 Ticks, below the 932 of the rung above — because by then only nearby pairs are
reachable at all. Survivorship: the long walks did not get slower, they left the sample. **A mean
conditioned on success cannot be printed beside a failure count without saying so.**

### Three defects found in R0's own harness

Recorded because the corpus keeps finding the same shape — *a value never checked against what it
claimed to describe* — and this is the fourth, fifth and sixth instance.

1. **Every Arterial died in one step, and four tables looked healthy while it happened.**
   `CounterHash.Below` is multiply-high, so it consumes the **top** bits of the word; three call sites
   drew a second value from one hash by pre-shifting it, and `Below(h >> 32, 2001)` reads bits that are
   now zero and returns 0 every time. Every Arterial therefore drew the same cross-heading, entered the
   map at a corner pointing straight back off it, and left immediately. The density curve, the
   footprint curve, the per-column table and the volume-scope table were all unaffected and all
   correct, **because a graph with no Arterials in it is still a graph.** What caught it was a
   severance count that should obviously have been large and read 3. *`Below` now re-mixes, so a caller
   cannot destroy entropy it does not know the function depends on; the call sites draw with distinct
   counters.* **The lesson is to report a quantity you expect to be boring.**

2. **The bootstrap column was mostly the sampler.** O-D pairs were drawn inside both timed loops, and
   the walk sampler rejection-samples up to 256 times to land inside its radius — which read as a
   3,956 ns "bootstrap" against a drive's 619 ns. The *search* column was never affected, being a
   difference of two loops that both paid it. Queries are now drawn before the clock starts, and the
   figure is 292 ns.

3. **The exact square root spent most of its time warming up.** `SqrtFloor` located its starting power
   of four by searching down from `1L << 62`, which is ~30 iterations before any work on the small
   distances that dominate. `BitOperations.Log2` gives the exponent outright. **A denominator that
   spends its time in a helper's warm-up is measuring the helper** — and the fix, worth 940 → 872 µs,
   was still not enough to save `EuclideanFloor`, which is how R0 learned that the square root itself
   was the cost rather than its preamble.

### What R0 decided, and what it did not

**Decided.**

- **The heuristic is `Chebyshev`**, and the ladder's other three rungs are out — two on admissibility,
  one on cost. This also settles that R3's HPA\* must be quoted against `Chebyshev` and not against
  whichever metric flatters it.
- **The Road Graph is not a memory constraint.** 2.0 MiB at the working density, 1.2% of the world.
- **The `(Segment, offset)` query shape is free.** ~250 ns of bootstrap against a 418 µs search.
- **Per-direction volume costs 5%** of the graph, so that decision is not a storage decision.

**Not decided, and owed.**

- **The cost unit.** R0 routes in **Q16.16 Ticks**, and it had to: a Tick is ~10.5 in-world seconds and
  a vehicle crosses about one Segment per Tick, so a cost accumulated in whole Ticks gives nearly every
  Segment a cost of 1 and A\* silently minimises **hop count** rather than time — while appearing to
  route on time. But `05 §121` says *"Q16.16 is for sub-Tile positions and nothing else"*, and sub-Tick
  time is not a sub-Tile position. The alternative spelling — an integer count of a fixed fraction of a
  Tick — measures identically, so nothing here rests on it. **What is owed is whether the core acquires
  a second Q16.16 meaning, and that is the corpus's decision rather than a benchmark's.**
- **Road density.** 16.20 km/km² at the placeholder rung is a number to check against a real city, not
  a replacement for the assumption. `CONTEXT.md` → Segment keeps its disclaimer.
- **The timing re-capture** under `tools/kernel-run.sh`, per the defect stated above.
- Zone count (R1), cluster size (R3), the routing Tick-budget share, and the Microscopic Cap are all
  untouched by R0 and remain where `plans/0010` puts them.

---

## S2 R1 — the travel-time matrix

**The prescribed first measurement, and it dissolves half of what S2 was sent to decide.** The
instruction to *"build the zone-to-zone travel-time matrix first, then measure what work is left"*
appears in four places in the corpus, and `references.md §2` gives the reason: if the matrix carries
the choice loop, *"the detailed-tier router only handles vehicle steering, and the many-to-many
argument for distance-vector largely evaporates."* **It carries it.**

Raw capture in `spikes/S2.Routing/results/`; this section is owed a rewrite by R7 alongside R0's.

### The word this task had to correct first

`plans/0010` said **zone** throughout. `CONTEXT.md` → Zone is *"a permission set over land"* — what
a player may build there — and `CONTEXT.md` → District is what was actually being swept: *"the
granularity of the travel-time matrix."* The banned-terms section makes the same assignment from the
other side, sending *region* to *"District for a Goods-pooling region"*.

**The inconsistency is wider than the plan and is filed rather than fixed.** `05 §422` and
`references.md §2` both say *"zone-to-zone travel-time matrix"*, and `plans/0010` quotes the second
of those verbatim — so correcting the quote would break it. Two different objects wearing one word,
in the authoritative technical document, is exactly the failure the vocabulary rule exists to prevent.
R1's code and report say District; the corpus is owed the sweep.

### The machine, and the capture defect this task closes

Measured on the Linux desktop: Intel i5-10400, Ubuntu 24.04.4, .NET 10.0.10, Release, **pinned to one
physical core under `performance` with turbo enabled**, by
`sudo spikes/S2.Routing/tools/routing-run.sh` — the canonical configuration, taken in the same sitting
as R0's re-capture so the two sections are one measurement of one machine rather than two.

> **The `vs K2` column is now a ratio within one governor**, which matters because it is the column
> the tripwire reads. It fires at S4's `SoaScattered` figure of **13.66 ns** per handle and the worst
> rung measured reads **5.00 ns**, so the margin is 2.7× at the extreme rung and 12× at the working
> anchor. The pinning that makes this comparable is also what R0's note above identifies as the
> likeliest cause of its own first-timed row moving, so *pinned* is not a synonym for *quiet* — the
> machine-state block at the foot of the capture is what makes that checkable, and it reports
> **0.76% CPU stall and 0.00% memory stall over 77.65 seconds**, against a load average of 1.4.
>
> **That block is the fourth machine-state defect in S2 and the first that belonged to the harness
> rather than the machine.** Its first version computed elapsed time as
> `ticks × 1e9 / Stopwatch.Frequency`, which overflows signed 64-bit before it divides —
> `Stopwatch.Frequency` is 1e9 on Linux, so a 78-second run is 7.8e19 against a ceiling of 9.2e18. It
> wrapped, and reported **4.21 s for a 78-second run and 15.82% CPU stall for an actual 0.8%.** The
> stall *counters* were raw kernel deltas and were never wrong; only the duration they were divided by
> was. **The instrument was added to stop a figure being quoted without being checked against what it
> claimed to describe, and shipped exactly that defect inside itself.** Fixed in `Harness/Capture.cs`
> — `Stopwatch.GetElapsedTime`, and the run's start and end instants are printed beside the duration
> so it is checkable by subtraction rather than trusted. **The re-run that replaced the artefact is
> what gave R0's note above its error bar**, which is the second time in this spike that chasing a
> defect produced the measurement nobody had planned.

### The synthetic peak, stated before anything rests on it

There are no Travellers in S2 and R2 is where volume comes from a routed load, so R1 deposits a
**synthetic monocentric commute field** — volume rising toward the map centre, leaning in the phase's
direction — into the graph's own volume column, and reads it back through BPR
(`free_flow × (1 + α(v/c)^β)`, α = 0.15, β = 4, `CONTEXT.md` → VDF). **No figure below says how
asymmetric a real city is.** Each says: *at directional imbalance `i`, this much.*

**A free-flow matrix could not have answered the question at all**, and that is the reason the field
exists rather than a convenience. A Segment has one length and one free-flow speed, so a free-flow
matrix is symmetric to the bit and its asymmetry is exactly zero — which is the vacuity this corpus
keeps catching itself in, after R0.5's severance count and the Census's unwritten trend assertion.

### R1.1 — the partition, and what a District rung is

Cell-aligned, never Chunk-aligned (`CONTEXT.md` → District: *"The Cell is frozen and the Chunk is
tunable… so boundaries made of Chunks would let a profiler move what a District **is**"*). The map is
128 Cells across, so a rung of *k* a side gives Districts of 128/*k* Cells a side.

| Per side | Districts | Cells each | Area | Standing |
|---:|---:|---:|---:|---|
| 4 | 16 | 1,024 | 16.77 km² | — |
| 8 | 64 | 256 | 4.19 km² | — |
| 10 | 100 | 164 | 2.68 km² | `plans/0001`'s 100–400 |
| **11** | **121** | **135** | **2.21 km²** | **`CONTEXT.md` → District's 128-Cell anchor** |
| 16 | 256 | 64 | 1.04 km² | — |
| 20 | 400 | 41 | 0.67 km² | `plans/0001`'s 100–400 |
| 32 | 1,024 | 16 | 0.26 km² | — |
| 45 | 2,025 | 8 | 0.13 km² | `plans/0010`'s 2,000-District DRAM row |
| 64 | 4,096 | 4 | 0.06 km² | — |

**The anchor lands inside `plans/0001`'s only figure, and that is arithmetic rather than
corroboration.** `plans/0001` predates the 1M target and the 4096² map; that its 100–400 band
contains a number derived from the pooling abstraction's validity is a coincidence worth one line.

No rung has an empty District, so no average below is taken over a row of unreachables.

### R1.2 — cold build, and resident size measured twice

A cold build is **one forward one-to-all Dijkstra per District**, not one search per pair: a forward
search from District *i* fills the whole of row *i*. Nothing may be halved by symmetry — the matrix is
asymmetric and the reverse row needs a backward search.

Resident size is reported twice because `03 §3.3` needs both: the **scalar matrix** the choice loop
reads, and the cached **District-pair routes** volume would be distributed along. They differ by far
more than a constant factor — *n²* integers against *n²* variable-length Segment sequences.

| Districts | Cold build | Per search | Nodes settled | Scalar matrix | Mean route | Route store |
|---:|---:|---:|---:|---:|---:|---:|
| 16 | 23.08 ms | 1.443 ms | 16,685 | 1.00 KiB | 56 Segments | 56.00 KiB |
| 64 | 94.67 ms | 1.479 ms | 16,685 | 16.00 KiB | 64 Segments | 1.00 MiB |
| 100 | 145.92 ms | 1.459 ms | 16,685 | 39.06 KiB | 63 Segments | 2.40 MiB |
| **121** | **177.20 ms** | **1.464 ms** | **16,685** | **57.19 KiB** | **65 Segments** | **3.63 MiB** |
| 256 | 378.37 ms | 1.478 ms | 16,685 | 256.00 KiB | 65 Segments | 16.25 MiB |
| 400 | 581.92 ms | 1.455 ms | 16,685 | 625.00 KiB | 65 Segments | 39.67 MiB |
| 1,024 | 1,502.20 ms | 1.467 ms | 16,685 | 4.00 MiB | 70 Segments | 280.00 MiB |
| 2,025 | 2,976.88 ms | 1.470 ms | 16,685 | 15.64 MiB | 69 Segments | 1.05 GiB |
| 4,096 | 6,069.97 ms | 1.482 ms | 16,685 | 64.00 MiB | 65 Segments | 4.06 GiB |

**Cold build is linear in District count and that is by construction rather than by measurement.** A
one-to-all has no goal to prune toward, so it settles the same 16,685 nodes whatever the partition is;
the per-search column is the quantity and the total is *n* times it. **It is flat within 2.7% across a
256× range of District counts**, which is the check that the warm pass below actually worked.

> **Four warm-up schemes in a row made it look sublinear, and that is the finding about the harness.**
> Warming inside the loop let each rung inherit the rungs before it and rung 4 read 4.5 ms per search
> against a settled 1.5. Hoisting the warm-up out at 64 searches made it worse. Raising it to 400 did
> not fix it. Building each rung twice and timing the second did not fix it either — which is what
> ruled out the assumption the first three shared: **the cost is per-process, not per-rung.**
> `OneToAll.Run` is called once per District, so the small rungs call it too few times to leave
> tier 0, and at tier 0 the binary heap's `Push` and `Pop` are real calls rather than inlined.
>
> **Every one of the four produced a smooth curve descending with District count** — which is
> precisely the shape a reader hopes a sweep will discover, and it was the process warming up. This is
> the same lesson as R0's second harness defect, where the "bootstrap" column was mostly the sampler,
> and it is worth stating in its general form: **an artefact that varies with the swept axis is not
> distinguishable from a result by looking at it.** Only a warm pass over the entire sweep removes it.

### The route store is the figure that fails, and it fails on `adr/0006`

**4.06 GiB at 4,096 Districts, against a whole world of 172.3 MiB** (K0). Even at the anchor it is
3.63 MiB against a 57 KiB scalar matrix — sixty-five times the thing it accompanies, because the mean
District-pair route is ~65 Segments. It grows as *n²* with a constant of 260 bytes.

**That matters more than it first looks, because R1.7 finds the route store is not optional.** It is
what a sound dirty-region rebuild needs in order to know which entries an edit invalidated. So the
choice is not *"cache routes or do not"* — it is *"keep an n²-Segment-sequence store, or rebuild the
whole matrix on every edit"*, and both sides of that are expensive at any District count past the
anchor.

### R1.3 — the read, and the tripwire that does not fire

**This is the measurement that mattered most, and it is the one that reaches furthest past S2.**
`02 §5.8` makes *never resolve a route inside the choice loop* a rule, named as the one thing UrbanSim
gets architecturally right that this design must not violate. If the matrix read is not cheap, that
rule is unenforceable.

`references.md §2` describes the choice loop **twice in one sentence** — *"what is the commute from
this candidate dwelling to any job?"*, which is one origin against many destinations and therefore a
sequential **row scan**, and *"many-to-many, evaluated tens of thousands of times per cycle"*, which
reads as **scattered**. `plans/0010` required both timed so that which one the loop performs becomes a
design question with a priced answer rather than a detail settled by whoever writes it.

| Districts | Resident | Row scan | Scattered | Scattered ÷ row | vs K2 |
|---:|---:|---:|---:|---:|---:|
| 16 | 1.00 KiB | 0.71 ns | 1.13 ns | 1.59× | 0.08× |
| 64 | 16.00 KiB | 0.50 ns | 1.15 ns | 2.30× | 0.08× |
| 100 | 39.06 KiB | 0.58 ns | 1.23 ns | 2.13× | 0.09× |
| **121** | **57.19 KiB** | **0.56 ns** | **1.14 ns** | **2.04×** | **0.08×** |
| 256 | 256.00 KiB | 0.53 ns | 1.30 ns | 2.44× | 0.09× |
| 400 | 625.00 KiB | 0.51 ns | 1.52 ns | 2.95× | 0.11× |
| 1,024 | 4.00 MiB | 0.57 ns | 1.64 ns | 2.88× | 0.12× |
| 2,025 | 15.64 MiB | 0.58 ns | 2.88 ns | 4.95× | 0.21× |
| 4,096 | 64.00 MiB | 0.57 ns | 5.00 ns | 8.76× | 0.36× |

**The tripwire does not fire, at any District count.** The wire reads *"the travel-time matrix read
costs more than S4's K2 random gather, at the District count the design needs"* — 13.66 ns per handle
(`SoaScattered`). The worst rung measured is **5.00 ns, at 4,096 Districts and 64 MiB**, and the
working anchor is **1.14 ns**. `02 §5.8`'s rule is enforceable with an order of magnitude in hand.

**The L3 cliff is visible and it is not the binding ceiling.** The i5-10400 has 12 MB of L3, and the
scattered read steps from 1.64 ns at 4 MiB to 2.88 at 15.6 MiB to 5.00 at 64 MiB — the transition the
plan predicted, in the place it predicted. But it arrives *below* the threshold that was supposed to
follow from it, so **District count's ceiling is set by the route store and by the entry error, not by
the cache.** The plan's own framing — *"a player drawing thousands of Districts would be drawing a
performance cliff"* — survives as a shape and not as a limit.

**The row scan is flat at 0.50–0.58 ns across a 64,000× range of resident sizes**, which is a hardware
prefetcher doing exactly what it exists for. So the two phrasings in `references.md`'s single sentence
differ by **8.8×** at 4,096 Districts and are **indistinguishable at 121** — the plan expected an order
of magnitude at 2,000 and indistinguishability at 100, and both halves are right. At the anchor the
question does not need answering; past it, it does.

> **`plans/0010` rewrote this tripwire during grilling and the rewrite is vindicated.** The original
> read *"not O(1) and cheap"*, and the plan noted that *"a lookup into an n×n array is O(1) by
> construction, so the original wire could not fire on any plausible implementation, which is the same
> effect as a wire reasoned around, arrived at earlier."* The replacement is a real threshold: it is
> approached, it is approached on the axis the plan named, and it is not reached.

### R1.4 — the asymmetry, and the decision that decides whether it exists at all

**The volume-scope question and the `adr/0020` exposure turn out to be one question, which
`plans/0010` filed as two.** R0 was forbidden from settling whether `volume / capacity` is stored per
Segment or per direction, priced it at 5% of the graph, and concluded *"what it buys is not visible
until R2 has volume to attribute."* It is visible here, and what it buys is the asymmetry itself.

At the anchor, 121 Districts, morning peak, 7,260 District pairs with a route both ways:

| Scope | Imbalance | Mean | Median | p90 | Max | Mean, relative |
|---|---:|---:|---:|---:|---:|---:|
| per Segment | any | **0.00 Ticks** | 0.00 | 0.00 | 0.00 | **0.00%** |
| per direction | 0.00 | 0.00 Ticks | 0.00 | 0.00 | 0.00 | 0.00% |
| per direction | 0.20 | 0.78 Ticks | 0.33 | 2.22 | 8.15 | 1.31% |
| **per direction** | **0.50** | **2.19 Ticks** | **0.91** | **6.27** | **24.44** | **3.54%** |
| per direction | 0.80 | 4.13 Ticks | 1.69 | 12.03 | 50.39 | 6.27% |
| per direction | 1.00 | 5.88 Ticks | 2.43 | 17.05 | 75.89 | 8.47% |

**Under per-Segment volume the matrix is symmetric to the bit, at every imbalance.** The two
directions of a Segment share one counter and sum into it, so the VDF returns the same delay both
ways — and `adr/0020`'s union-find is exactly correct, **not by evidence but by construction**. The
per-Segment row is not a measurement of a small effect; it is a structural zero.

**The zeroes are the instrument working, and they are only evidence because the parameter beside them
moves.** This is R0.5's lesson taken from the other side: a count that has never been observed to move
is equally consistent with *the effect is absent* and *the instrument cannot see it*. Here the same
graph, the same partition and the same field produce 0.00% under one scope and 8.47% under the other,
which is what makes the zero readable.

**So the corpus cannot have both.** `CONTEXT.md` → Lane makes Lanes directional queues; the gate
section of `plans/0010` argues per-Segment volume makes Stress understate at exactly the moment it
matters and promotes to Microscopic late; and `CONTEXT.md` → Settlement requires *mutual*
reachability, which is only a distinct idea on an asymmetric matrix. **Per-Segment volume makes three
separate parts of the design vacuous at once, at a saving of 5% of the Road Graph** — 100 KiB on a
2.0 MiB structure that is 1.2% of the world.

### R1.5 — `adr/0020` measured against the definition it claims to compute

`adr/0020` computes a Settlement as *"a **connected component** of the District graph… **a union-find**
over data already being maintained, at effectively no cost"*. `CONTEXT.md` → Settlement defines one as
*"a maximal set of Districts **mutually** reachable within the Commute Budget."* **Union-find computes
weak connectivity; "mutually reachable" is strong connectivity**, and the two coincide only on a
symmetric matrix.

**The asymmetry distribution above is the wrong instrument for this and `plans/0010` asked for it
anyway.** A distribution is a claim about travel times; a Settlement is an object the game is made of,
and a Building's Trips fail or do not depending on which algorithm is right. So the test is whether
the two **disagree about the city**. The Commute Budget has no value anywhere in the corpus, so it is
swept — at the anchor, morning peak, per-direction volume, imbalance 0.50:

| Commute Budget | Union-find | Tarjan SCC | Largest weak | Largest strong | One-way pairs |
|---:|---:|---:|---:|---:|---:|
| **20 Ticks** | **6** | **8** | **90** | **70** | 13 |
| 30 Ticks | 2 | 2 | 120 | 120 | 45 |
| 40 Ticks | 1 | 1 | 121 | 121 | 76 |
| 50 Ticks | 1 | 1 | 121 | 121 | 146 |
| 60 Ticks | 1 | 1 | 121 | 121 | 208 |
| 80 Ticks | 1 | 1 | 121 | 121 | **264** |
| 120 Ticks | 1 | 1 | 121 | 121 | 47 |

**`adr/0020` is owed an amendment, on evidence.** At a tight Budget the two readings return **six
Settlements against eight**, and the largest component differs by twenty Districts out of 121 — a
fifth of the map assigned to a Settlement it is not mutually reachable within. Strongly connected
components is Tarjan and it is still cheap; it is simply not `adr/0020`'s claim.

**The exposure is a band, not a threshold, and that is the part that cannot be designed around.** A
pair is one-way only while the Budget sits between its two directions' costs, so a Budget below every
commute produces none and a Budget above every commute produces none either — which is why the count
rises to 264 and then falls to 47. **A Budget generous enough to close the gap is a Budget that has
stopped bounding anything**, and `CONTEXT.md` → Commute Budget exists to make geography matter.

**One reading is worth stating plainly for whoever writes the amendment.** Settlement *counts* agree
at every Budget above the tightest, so the practical consequence is smaller than the exposure sounds —
but it is largest exactly where the city is fragmenting, which is the moment Settlements are load
bearing rather than decorative. A mechanism that is only wrong when it matters is the same shape as
the aggregate-attribution lag `adr/0041` rejected.

### R1.6 — what a matrix entry is wrong by, which nothing asked for

`plans/0010` measures four things and all four are about cost: build, rebuild, size, read. **None asks
how wrong an entry is.** A District-to-District entry is one number standing for every Access Point
pair inside those two Districts, and if its error exceeds what the Commute Budget resolves, *"the
matrix carries the choice loop"* is false however fast the read is. That is a prior question to every
figure above and it was not on the list.

Measured against the query the game actually issues — R0's `(Segment, offset) → (Segment, offset)` A\*
with `Chebyshev`, over the same congested arc costs the matrix was built on, ~2,400 searches per rung:

| Districts | Pairs | Searches | Mean error | p90 | Max | Mean, relative |
|---:|---:|---:|---:|---:|---:|---:|
| 16 | 374 | 2,244 | 15.04 Ticks | 31.62 | 72.93 | **24.70%** |
| 64 | 395 | 2,376 | 9.38 Ticks | 20.87 | 64.98 | 16.82% |
| **121** | **395** | **2,376** | **6.73 Ticks** | **14.04** | **77.62** | **11.32%** |
| 400 | 396 | 2,382 | 4.00 Ticks | 7.42 | 61.52 | 6.91% |
| 1,024 | 399 | 2,400 | 2.41 Ticks | 4.14 | 50.45 | **3.80%** |

**The error is a property of District extent and it shrinks exactly as the resident size grows.** That
is the trade R1 exists to price, and it is why District count cannot be chosen on cache behaviour
alone: **the rung that fits in L3 is the rung whose entries stand for the most ground.** At the anchor
a Household reading the matrix is deciding on a commute figure wrong by 11.3% on average — 6.73 Ticks
against a plausible 40-Tick Budget, or about a sixth of it.

**Whether that is acceptable is not a question a benchmark can answer**, and this is now a decision
owed by R7: an error of 6.73 Ticks is free against a Budget the player reads to the nearest half hour
and disqualifying against one read to the minute. `CONTEXT.md` → Commute Budget gives no number and
`01 §7` draws it as a wedge on the sun arc, which is a granularity of a sort but not a stated one.
**R6 is owed the same question about its cache key** — that task must report *"the induced error
against the Commute Budget, which is the only thing that consumes it"* — so the two should be settled
together.

> **A harness defect caught here, and it is the corpus's recurring shape for the third time in S2.**
> The first capture drew Access Points uniformly over the map and rejected those outside the named
> District, with a bounded draw count. At 1,024 Districts a hit is one draw in a thousand, so the
> sample silently collapsed to **nine searches** — and that row was printed beside rows built from
> 2,244. **A sample that shrinks with the swept axis manufactures a trend out of its own
> survivorship**, which is R0.5's *mean cost when found* defect wearing different clothes. An index of
> Segments per District costs one pass and cannot collapse; every rung above rests on ~2,400 searches.

### R1.7 — the matrix carries no Epoch, and a dirty region cannot invalidate it

`plans/0010` R5 requires R1 to state whether the matrix carries an Epoch, because the corpus holds
**two unrelated invalidation mechanisms**: routes invalidate against a scalar Epoch
(`CONTEXT.md` → Epoch), while `02 §6` describes the matrix as rebuilt at a *slow cadence, dirty regions
only* — a spatial one. *"Two invalidation mechanisms are in the corpus and nothing relates them, so
the matrix and the cache can disagree about what the network currently is."*

**It carries none, and R1 declined to give it one.** A version counter would imply a relationship to
the route cache that nobody has argued, and the disagreement is better visible in the code than papered
over. What R1 found is worse than the ambiguity.

One District's roads are bulldozed — `plans/0010`'s own *"in a city builder link deletion is the core
verb"* — and the matrix is rebuilt three ways, each checked against a full rebuild's ground truth. At
the anchor, 121 Districts, 14,641 entries:

| Edit site | Rung | Rows rebuilt | Cost | Entries left stale | Sound |
|---|---|---:|---:|---:|---|
| **Centre** | Full rebuild | 121 | 177.6 ms | 0 | yes, by definition |
| | Dirty region — Districts the edit touches | 1 | 0.16 ms | **309 of 429** | **no** |
| | Routes crossing the edit | 121 rows, **430 entries** | 176.8 ms | 0 of 429 | yes |
| **Corner** | Full rebuild | 121 | 173.8 ms | 0 | yes, by definition |
| | Dirty region — Districts the edit touches | 1 | 0.00 ms | **132 of 252** | **no** |
| | Routes crossing the edit | 121 rows, **253 entries** | 175.5 ms | 0 of 252 | yes |

**`02 §6`'s dirty region is a spatial test on a non-spatial quantity, and it is unsound.** A path from
District *i* to District *j* can cross the edited ground without either endpoint being near it, so
rebuilding *the Districts the region overlaps* misses exactly the long routes the matrix exists to
serve — 72% of the changed entries on a central edit, 52% on a corner one. It misses them **silently**,
leaving entries stale rather than merely coarse, which is a different and worse failure than the
coarseness R1.6 measures.

**Two edit sites, because one of them is degenerate and a single row would have concluded the
mechanism is worthless.** A central District lies on the shortest path between most pairs on the map
and a corner one on almost none — 429 entries moved against 252 — and the contrast is what says the
mechanism has a shape rather than a verdict.

**The sound test works and cannot be afforded, for a structural reason rather than a cost one.**
*Which routes crossed the region* identifies the changed set almost exactly — **430 entries against
429 actually changed** — so as a predicate it is essentially perfect. But it needs the **route store**
R1.2 priced, and it still touches **every row**: a one-to-all fills a whole row, so the build
granularity *is* the row, and every row holds the entry addressed *to* the edited District, whose route
necessarily ends inside it. **However few entries an edit invalidates, at least one lands in every row
and the incremental path collapses into the full one.**

So the two columns say different things and both are the finding. **430 entries is the work genuinely
needed — 2.9% of the matrix. 121 rows is the work the structure forces — 100%.** An incremental
rebuild worth having needs a build kernel finer than one-to-all, which means a point-to-point search
per entry — and R0 priced that at **418 µs**, so filling one row of 121 that way costs **50.6 ms**
against the one-to-all's **1.46 ms**. **Entry-granular invalidation is 34× more expensive per row than
the row-granular rebuild it would be avoiding**, so it only pays when fewer than one row in thirty-four
is dirty, and R1.7 has just shown that every row is.

**`02 §6` is owed a correction either way**, and `CONTEXT.md` → Epoch's *when you pay* / *what
survives* distinction is the same correction arriving at the matrix instead of at the cache.

### R1.8 — one matrix or five, and a hash-bearing decision the corpus never named

`plans/0010` required the matrix's **time resolution** swept as a second axis, on the argument that a
single Day-average matrix cannot represent the peak every other figure in this spike is measured at:
morning inbound and evening outbound cancel, and the asymmetry the directed graph exists to carry
vanishes into the mean. At the anchor, per-direction volume, imbalance 0.50:

| Resolution | Build | Resident | Mean asymmetry | p90 | One-way pairs at a 40-Tick Budget |
|---|---:|---:|---:|---:|---:|
| **Day average, one matrix** | 252.7 ms | 57.19 KiB | **0.08 Ticks** | 0.23 | **1** |
| — `Dawn` | | 57.19 KiB | 0.00 Ticks | 0.02 | 0 |
| — **`MorningPeak`** | | 57.19 KiB | **2.19 Ticks** | 6.27 | **76** |
| — `Midday` | | 57.19 KiB | 0.00 Ticks | 0.00 | 0 |
| — **`EveningPeak`** | | 57.19 KiB | **1.79 Ticks** | 5.10 | **64** |
| — `Night` | | 57.19 KiB | 0.00 Ticks | 0.00 | 0 |
| **Per phase, five matrices** | 970.3 ms | 285.95 KiB | | | |

**The Day average reports one one-way District pair where the morning peak has seventy-six.** The
cancellation the plan predicted is not partial — it is near-total, because the two peaks are
deliberately opposite in sign and an unweighted mean of five phases is dominated by the three balanced
ones. A single-resolution matrix hands the choice loop a city with almost no directional structure in
it at all.

**And that makes resolution a hash-bearing decision, which the corpus has never named.**
`plans/0010`'s decision 2 files the *refresh cadence* as almost certainly hash-bearing — two cadences
decide when a changed travel time becomes visible to the choice loop, so two cadences produce two
cities, a design change under `05 §4` rather than a free knob. **Resolution is the same class of
decision and it is not in the ledger anywhere.** A Household choosing where to live decides
differently under a Day-average matrix than under a per-phase one, measurably so; that is two cities
by the corpus's own test. It is filed as decision 2a and belongs wherever cadence is settled, because
separating them is how one of the pair gets treated as a knob.

**The price of the honest option is small and should not be the deciding factor.** Five matrices are
five times the build and five times the resident size — 286 KiB at the anchor, still nothing — and the
argument against averaging is a correctness one rather than a budget one.

> **The average is taken over the cost, not the volume, and the choice is not cosmetic.** BPR is
> convex, so the delay at the mean volume is strictly less than the mean of the delays: averaging
> volumes first would give a Day-average matrix describing a city **with no rush hour in it at all**,
> rather than one whose rush hour has been smeared. Neither is right. This one is at least wrong in
> the direction that does not flatter the abstraction being tested.
>
> It is also **unweighted**, because the sun arc's phase widths are `plans/0010`'s open decision 5a
> and an unweighted mean is the only average available while they are unsized. A weighting would make
> the Day-average row a function of a number nobody has chosen.

### What R1 decided, and what it did not

**Decided.**

- **The matrix carries the choice loop.** 1.14 ns scattered at the anchor and 5.00 ns at 4,096
  Districts, against a tripwire at S4's K2 gather of 13.66 ns. `02 §5.8`'s *never resolve a route
  inside the choice loop* is enforceable with an order of magnitude in hand. **This is the finding R4
  was made conditional on**; what remains of the distance-vector case now rests on R2 alone.
- **Volume scope is not a storage decision.** Per-Segment volume makes the matrix symmetric to the
  bit, which makes `adr/0020`'s union-find correct by construction, `CONTEXT.md` → Settlement's
  *mutually* reachable a distinction without a difference, and Stress blind to a directional peak. It
  saves 5% of a structure that is 1.2% of the world.
- **District count's ceiling is not L3.** The cache cliff is real and arrives below the threshold that
  was supposed to follow from it. What binds is the **route store** — 4.06 GiB at 4,096 Districts
  against a 172.3 MiB world — against the **entry error**, which falls from 24.70% to 3.80% across the
  same sweep.
- **`02 §6`'s dirty-region rebuild is unsound**, missing 72% of changed entries on a central edit, and
  the sound alternative collapses into a full rebuild because a one-to-all fills a row.

**Not decided, and owed.**

- **`adr/0020` needs an amendment.** Six Settlements against eight at a tight Commute Budget is
  evidence, and the ADR's *"connected component… union-find"* is not what `CONTEXT.md` defines.
- **The Commute Budget's granularity**, which is what makes the 11.32% entry error acceptable or not.
  R6 is owed the same question about its cache key and the two should be answered once.
- **The matrix's time resolution**, filed as decision 2a beside the refresh cadence.
- **Why plain Dijkstra's absolute moved 1.64× under pinning** while every other rung held. The
  standing hypothesis is tiered-JIT background compilation sharing the one visible core, and the check
  is to re-run the ladder in reverse order or with tiering disabled. Until it runs, **the first-timed
  row of any S2 table is the least trustworthy number in it** — which is a claim about the harness, so
  it is owed by R7 rather than by a measurement task.
- The **directional imbalance a real city actually has** is not measurable by S2 — it needs a
  generator mix and a sun arc with widths. Every asymmetry figure above is conditional on it.

---

## S2 R2 — the path source, the crossover, and the attribution lag

**Captured** 2026-08-06 18:52 UTC, i5-10400 @ DDR4-2133, `performance` governor with turbo, pinned to
one physical core, .NET 10.0.10, Release — the canonical configuration
`spikes/S2.Routing/tools/routing-run.sh` produces. Raw capture in
`spikes/S2.Routing/results/s2-r0+r1+r2-intel-core-i5-10400-ddr2133-performance-turbo-cpu2-20260806T185053Z.md`. **Every count in R0
and R1 is bit-identical to the previous canonical capture** and the timing columns move within 2%,
which is the determinism check carried forward rather than re-argued.

### The task that arrived smaller than it was planned, and larger than it was scoped

`plans/0010` framed R2 as two axes. [`adr/0041`](adr/0041-volume-is-attributed-by-the-traveller-not-the-district-pair.md)
has since settled one of them — volume is attributed by the Traveller, not the District pair — on
correctness grounds, and explicitly left R2 *"the price, not the choice"*. So R2a became a
measurement of what rejecting the cheaper scheme cost.

**And the surviving axis turned out to have three rungs rather than two.** `adr/0041` requires a
vehicular Traveller to increment the Segment it *enters*, every Tick. What that needs is a **next
Segment**, and a path is only one way to supply it: a **next-hop table** supplies one and stores no
path at all, which is what distance-vector routing *is*. That rung is not in `plans/0010`, because
`plans/0010` predates the ADR. Measuring only searched-against-shared would have priced two routers
against a workload the design no longer has — the precise failure the spike's prescribed order exists
to avoid, arriving one task later than the order was built to catch it.

### R2.1 — the ladder, and the correctness content `adr/0041` says is not there

| Rung | Build | Resident | Per Leg at spawn | Per crossing | Detour, mean | p90 |
|---|---:|---:|---:|---:|---:|---:|
| **searched** | — | 13.03 MiB | 716,800 ns | 24 ns | 0.00% | 0.00% |
| **shared** | 195.54 ms | 3.46 MiB | 2,617 ns | 26 ns | **36.01%** | **71.39%** |
| **next-hop** | 474.47 ms | 7.70 MiB | 0 ns | 32 ns | **18.52%** | **40.70%** |

At the 121-District anchor. *Searched* resident is `in-flight × mean route × 4 B` at the derived
56,000 rather than the measured pool's footprint, and the report says which figure is which.

**The searched rung is out on arithmetic and does not need a verdict.** One Leg costs 716,800 ns
against 530–574 arrivals per Tick, which is ~400 ms of searching per 15.6 ms of Tick budget. The
harness could not afford to run it either, and drew from a precomputed pool instead — *that* is the
result, not a limitation of the measurement.

**`adr/0041` calls the path source *"a performance axis with no correctness content."* It has
correctness content, on two counts.**

- **Statistically**, in the detour columns. Two of the three rungs aim a Traveller at a District
  **representative** rather than at where it is going. A shared route is coarse at *both* ends — the
  Traveller must reach the origin representative before the stored route means anything — while a
  next-hop table is followed from wherever the Traveller actually is, so it is **exact on the origin
  side and coarse only on the destination side**. That structural difference is worth almost exactly
  half the error, measured. A Traveller driving 36% further is a different Trip, which under `05 §4`
  is a different city.
- **Structurally**, and this is the larger one. Under either coarse rung *every* Trip bound for a
  District arrives through that District's **one representative node** — a shared route ends there, a
  next-hop column is a tree rooted there. So the arcs into a representative carry the whole of a
  District's inbound traffic. R2b measures the consequence: the same surge drives the watched arc to
  **412%** `v/c` under shared and **130%** under searched. **The representative is not a summary of
  the District under these rungs; it is a hole every Trip is threaded through**, and a fidelity model
  that promotes on `volume / capacity` promotes there and nowhere else.

The detour figures are measured at node granularity and are therefore an **upper bound**: the two
Access Point remainders leave each detour unchanged in Ticks and raise the denominator. 300 Legs
sampled, sample size reported per rung as the board's owed finding requires.

#### The two coarse rungs scale on different axes and cross

| Districts | Route store | Next-hop table |
|---:|---:|---:|
| 16 | 58.13 KiB | 1.01 MiB |
| 100 | 2.33 MiB | 6.36 MiB |
| **121** | **3.46 MiB** | **7.70 MiB** |
| 256 | 15.15 MiB | 16.30 MiB |
| 400 | **36.84 MiB** | **25.47 MiB** |

A route store is `n² × mean route`; a next-hop table is `nodes × n`. **Quadratic against linear**, so
the cheaper structure depends entirely on where District count lands — and R1 left that as an open
trade rather than a settled number. The crossing is between 256 and 400 Districts on this graph. At
4,096 the store is 4.06 GiB (R1's figure) and the table 261 MiB; neither is a rung anybody should
reach, which is why both are printed.

### R2.2 — the crossing rate, which `adr/0041` assumed and S2 can now measure

| Path source | Arc costs | Crossings/vehicle/Tick | Arrivals/Tick | Volume conserved |
|---|---|---:|---:|---|
| Searched | free flow | 0.82 | 574 | yes |
| Searched | morning peak | 0.79 | 545 | yes |
| Shared | free flow | 0.83 | 562 | yes |
| Shared | morning peak | 0.79 | 530 | yes |
| NextHop | free flow | 0.83 | 556 | yes |
| NextHop | morning peak | 0.79 | 529 | yes |

`adr/0041` prices direct attribution from *"a vehicle crosses about one Segment per Tick"* and names
the rate as its own revisit trigger. **The measured rate is 0.79–0.83, not 1.0**, so the ADR's
~80,000 increment/decrement pairs per Tick at 1M is an overestimate by about a fifth rather than the
underestimate the trigger anticipated. Reported at free flow *and* at the peak because the ADR's
estimate is a free-flow one and the simulation is not — congestion lowers the rate and lowers the
scheme's cost with it, and quoting only the congested figure would credit direct attribution for a
saving the jam paid for.

**The *volume conserved* column caught a real defect and is the reason it exists.** `adr/0041`
requires *"summed Segment volume equals the number of in-flight vehicular Travellers, every Tick"*
and names the failure precisely: *"a Traveller that vanishes without decrementing destroys the
reading permanently… presents as a road that looks busy forever."* The next-hop rung was written with
exactly that defect — arrival tested *after* entering the last arc — and the first capture reported a
peak `v/c` of **883×** with every other column in the report looking healthy. **The invariant the ADR
asked for found it on the first run it was printed.** Recorded because it is the fourth instance in
this spike of a quantity that only earns its place on the day it reads wrong.

### R2a — the crossover, priced rather than chosen

| In flight | Crossings/Tick | Direct attribution/Tick | Per crossing |
|---:|---:|---:|---:|
| 37,000 | 25,596 | 138,225 ns | 5,400 ps |
| **56,000** | **38,865** | **139,437 ns** | **3,587 ps** |
| 111,000 | 76,823 | 323,195 ns | 4,207 ps |
| 170,000 | 117,537 | 419,150 ns | 3,566 ps |

Timed over the **real** crossing distribution — the arcs an advancing fleet actually entered and
left, captured and replayed — because whether the volume column sits in L2 is a property of how
scattered those indices are, and drawing them would have measured the draw.

| Districts | Arc writes | Aggregate per cycle | **Crossover cycle** |
|---:|---:|---:|---:|
| 16 | 14,626 | 0.43 ms | 3 Ticks |
| 100 | 584,527 | 10.11 ms | 72 Ticks |
| **121** | **851,358** | **14.72 ms** | **105 Ticks** |
| 256 | 2,309,725 | 41.58 ms | 298 Ticks |
| 400 | 2,979,576 | 53.91 ms | 386 Ticks |

**The crossover is at 105 Ticks at the anchor, an order of magnitude past `adr/0041`'s estimate of
~10.** The ADR reasoned from an assumed crossing rate and an unweighted smear; the rate is now
measured and the smear implemented in its conserving form. **So direct attribution is the cheaper
scheme for any congestion cycle shorter than about 105 Ticks**, and the ADR's *"we are knowingly
paying for correctness"* understates its own case — at plausible cycle lengths it is not paying at
all.

The smear is the **conserving** form: a Traveller on a route of total time `T` contributes `t_s / T`
to each Segment, so the shares sum to one and the ADR's invariant holds. Adding the whole pair count
to every Segment would be cheaper per write and would put one vehicle on fifty Segments at once. **A
rejected alternative implemented weakly makes the price of rejecting it look smaller than it is.**

#### Where the crossover inverts, across the peaking sweep

| Congestion cycle | Aggregate/Tick | Peaking factor that inverts it |
|---:|---:|---:|
| 10 Ticks | 1,481,906 ns | 10.62× |
| 25 Ticks | 592,762 ns | 4.25× |
| 50 Ticks | 296,381 ns | **2.12×** |
| 100 Ticks | 148,190 ns | **1.06×** |
| 200 Ticks | 74,095 ns | 0.53× |

`plans/0010` asked for exactly this number, on the argument that only one side of the crossover moves
with the peak. **The inversion is reachable only at cycles of 50 Ticks or longer**: at 25 Ticks it
needs a 4.25× peak, and the corpus's own generator mix caps the peak near 3× — 79% of Trips are
commutes and school runs. The peaking factor itself is still unsized (decision 5a), so this is a
curve and not a verdict.

### R2b — the lag, and the finding that is worse than a lag

| Path source | Watched peak, direct | Watched peak, aggregate | Aggregate lag | Peak compression |
|---|---:|---:|---:|---:|
| Searched | 130.21% | 28.09% | **never** | 0.61–0.63× |
| Shared | 412.33% | 18.41–20.14% | **never** | 0.21× |
| NextHop | 108.51% | **0.00%** | **never** | 0.62× |

Across every congestion cycle from 1 to 200 Ticks. Direct lag is zero at every rung, printed anyway
because a column that cannot be anything else is the one worth checking.

**`03 §3.3` predicted a lag. The measurement says the aggregate scheme does not report the jam late —
it does not report it at all.** A column of identical *never*s is the shape of a broken instrument,
so the two watched columns exist to tell that apart: they give the highest `v/c` each scheme ever
reads on the *same* arc. If aggregate reached a large number that merely arrived late, the failure
would be cadence. It never reaches one — and *never* appears at a **one-Tick cycle**, where there is
no cadence left to blame. Under the next-hop rung the smear deposits **nothing at all** on a Segment
direct reports at 108%.

**That is `adr/0041`'s first argument, measured**: *"a Traveller experiences congestion on its own
route and deposits congestion on the District pair's route, so the failure feeds a **different**
detector, watching different Segments."* The corpus filed this as a *timing* defect and compensated
for it with force-promotion on downstream blocking. It is a *place* defect, and no cadence fixes a
place. **Force-promotion therefore loses its remaining bundled justification here** and must stand on
`03 §3.3`'s second argument alone — that a Statistical Segment is structurally blind to a full
downstream neighbour — which is a smaller claim than the one it was bundled with.

**Peak compression is the column `plans/0010` asked for**: aggregate's peak over direct's. At 0.21×
under shared routes the scheme understates the true peak nearly fivefold, so under `adr/0007` it
would promote late *and*, because demotion uses a lower threshold, demote early.

| Threshold | Segments over, direct | over, aggregate |
|---:|---:|---:|
| 80% | 2,592 | 2,714 |
| 100% | 1,918 | 1,860 |
| 120% | 1,422 | 1,412 |

**The two schemes agree closely on *how many* Segments are stressed and disagree completely on
*which*** — which is the whole finding in one pair of tables, and it is the shape most likely to pass
an aggregate sanity check while being wrong about every individual road.

**Read every `v/c` above comparatively and never as an absolute level.** A Traveller in this harness
passes through a Segment regardless of load — there is no queue, because `plans/0010` forbids the
spike simulating traffic — so `v/c` is unbounded and a monocentric surge drives it far past anything
a real Segment reaches. What is compared is two readings of one load, and that comparison is
unaffected.

### What R2 decided, and what it did not

**Decided.**

- **The searched rung is out**, on arithmetic rather than on a benchmark: 716,800 ns per Leg against
  ~550 arrivals per Tick.
- **Direct attribution is cheaper than aggregate below a 105-Tick congestion cycle**, so `adr/0041`
  is not the trade-off it described itself as at plausible cadences.
- **The aggregate scheme fails `plans/0010`'s tripwire outright** — *a scheme that cannot report a jam
  within the congestion cycle it happens in is out on a design commitment, not on a number* — and
  fails it harder than the wire anticipated, by reporting the jam in the wrong place rather than at
  the wrong time. `adr/0041` had already excluded it; this is the evidence, arriving after the
  decision and agreeing with it.
- **The crossing rate is 0.79–0.83 per vehicle per Tick**, not 1.0.

**Not decided, and owed.**

- **`adr/0041` is owed an amendment**: *"a performance axis with no correctness content"* is wrong on
  two counts, the statistical detour and the structural representative funnel. **The ADR's
  substantive claim survives untouched** — experience and contribution remain the same list of
  Segments under every rung, because a Traveller increments whatever it actually drives — so this
  amends a sentence, not a decision.
- **R4 is live and `plans/0010`'s condition for retiring it must not be applied as written.** The plan
  retires DSDV *"if the matrix carries the choice loop and Statistical Trips need no concrete path"*.
  R1 settled the first clause. The second is **false**, and false for a reason that favours
  distance-vector: `adr/0041` needs a next Segment every Tick, not a path, and that is the data
  structure DSDV maintains natively. This is an argument rather than a measurement, and R2 does not
  settle R4 with it — R4's own subject is **convergence after an edit**, which nothing here touches.
- **The path source is not chosen**, and R2 deliberately does not choose it. Searched is out; the two
  survivors rank differently at different District counts, have different error profiles, and differ
  most in a property R2 cannot see — **invalidation**, which is R5's. The next-hop rung's attraction
  is that it needs no per-route invalidation at all; its exposure is that *in a city builder link
  deletion is the core verb*.
- **The representative funnel is a finding larger than R2** and nothing in the corpus addresses it. If
  a Statistical Trip's route is ever District-granular, every Trip into a District passes through one
  node, and Stress on that node is an artefact of the partition rather than a property of the city —
  the same defect class `03 §3.9` rejects for the Microscopic Cap and `adr/0041` rejects for volume
  attribution, arriving a third time by a different door.

## S2 R3 — HPA\*, the cluster it owns, and the reduction that decides it

**Task R3 of [`plans/0010`](../plans/0010-s2-routing.md) is done.** It was written to decide **cluster
size, outright** — `adr/0040` makes the abstract graph `(derived AND rebuilt)`, so the decision costs a
recomputation to change and nothing else, forever. It narrows that choice to two rungs and cannot
separate them. It also produces the figure the plan's *current standing favours HPA\** was resting on,
and produces a second one the plan did not ask for: **no cluster size fits routing into the Tick
budget**, which promotes a task that was scheduled as a tidy-up.

Raw capture in `spikes/S2.Routing/results/`; R7 owes this section a rewrite and records the deleting
commit.

### The capture is `powersave`, and the canonical one is owed

**Stated first because R0's own first capture was rejected for exactly this.** The figures below come
from `s2-r3-intel-core-i5-10400-ddr2133-powersave-turbo-cpu2-20260806T203318Z.md` — taken by `tools/routing-run.sh`, so it is
**pinned to one physical core with its SMT sibling idle**, but the governor is `powersave` because
the canonical configuration needs root and this sitting did not have it. `docs/dev-environment.md`'s
protocol is `sudo spikes/S2.Routing/tools/routing-run.sh --cluster`, and it is owed.

**What that does and does not put at risk.** R0 established that every *count* in this spike is
bit-identical across governors — only nanoseconds and the ratios over them move — and R3's decisions
rest on portal counts, edge counts, optimality shares and **ratios taken between two figures measured
in the same process**. The one figure that would be unsafe to quote as an absolute is the flat
search's 477,609 ns, and R3 quotes it as one in exactly one place: R3.4's Tick-budget arithmetic,
which is flagged there.

### The denominator moved by 200%, and the instrument that caught it was a second reading

**R3's first pinned capture read 1,240,143 ns for the flat search against 425,803 ns for the same
code in the same configuration unpinned, while every hierarchical rung stood still.** Every ratio in
R3 divides by that number, so an artefact living in it decorates the entire task — and it very nearly
did.

The cause is position rather than pinning: the flat loop was **the first timed thing in the process**,
and under `powersave` the clock had not ramped. The harness now measures the denominator **twice, on
either side of the sweep**, and publishes both: first pass **1,401,307 ns**, second **477,609 ns**, a
spread of **193.40%**. The ratios divide by the second, because every hierarchical rung is measured
after the warm sweep and shares its process state while the first pass does not. The two passes
returned **0** differing route costs out of 1,000, printed because it must read zero.

**This is the fifth instance in S2 of R0's *"an argument for reporting a quantity you expect to be
boring"*, and the first where the boring quantity was the denominator itself.** The four before it
were R0's dead Arterials, R0.5's mean-cost-when-found, R1's shrinking sample and R2's volume
conservation. It generalises past this spike: **a denominator measured once has no error bar, and a
denominator measured first has a systematic one.**

### R3.1 — `adr/0014`'s claim is wrong by a factor of 256 in area

[`adr/0014`](adr/0014-grid-streets-with-freeform-arterials.md) claims the Road Graph *"arrives
pre-partitioned, because the Chunk grid is already the pathfinding cluster."*
[`adr/0040`](adr/0040-the-pathfinding-cluster-is-a-multiple-of-the-chunk-not-the-chunk.md) had already
corrected the *identity* half of that on structural grounds. R3 measures how far off it was.

| Chunks per cluster | Cluster | Clusters | Largest | Portals | Abstract edges | Reduced edges | Reduced resident | + paths |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 32 Tiles | 16,384 | 4 nodes | 16,694 | 64,142 | 64,134 | 1011.22 KiB | 1.47 MiB |
| 4 | 128 Tiles | 1,024 | 25 nodes | 11,875 | 133,116 | 45,236 | 692.11 KiB | 1.18 MiB |
| **8** | **256 Tiles** | **256** | **81 nodes** | **6,665** | 151,350 | **24,586** | 406.41 KiB | **765.43 KiB** |
| **16** | **512 Tiles** | **64** | **291 nodes** | **3,337** | 133,816 | **11,768** | 229.45 KiB | **453.37 KiB** |
| 64 | 2,048 Tiles | 4 | 4,248 nodes | 518 | 62,126 | 1,678 | 88.95 KiB | 136.07 KiB |

**At one Chunk the abstract graph *is* the Road Graph** — 16,694 portals against 16,697 nodes, and a
query that expands exactly the 4,138 nodes the flat search expands. That is not a coincidence to be
explained away; it is `adr/0014`'s claim evaluated, and it says the claim describes a hierarchy that
abstracts nothing. **`05 §5`'s prediction that the pathfinding role wants *larger, and loudly* is
right, and the margin is 16× in side and 256× in area.**

**Resident size decides nothing at any rung.** The largest abstract graph in the sweep is **1.84 MiB**
against a 172.3 MiB world, and the configuration R3 recommends is **453.37 KiB** including the stored
paths. That last column is the one an earlier draft feared: storing the concrete arcs of every
intra-cluster edge was estimated at ~1.1 MiB and measured at **223.92 KiB of arcs**, because the
reduction had already removed 91% of the edges that would have carried them. **It is not an
`adr/0006` hazard**: the arena is a function of the partition and the road network, both bounded, and
it is rebuilt rather than appended to.

### R3.2 — preprocessing, priced in flat searches

Priced in flat searches rather than milliseconds alone, because that is the question: preprocessing is
only affordable if the queries it saves outnumber it. `adr/0040` keeps this out of the save
deliberately, so it is paid on **every load** and after every change to the cluster size.

| Chunks | Cold build | Nodes settled | In flat searches |
|---:|---:|---:|---:|
| 8, reduced | 26.54 ms | 574,207 | 55 |
| **8, reduced + paths** | **45.03 ms** | 1,148,375 | **94** |
| 16, reduced | 59.87 ms | 2,014,227 | 125 |
| **16, reduced + paths** | **96.04 ms** | 4,028,411 | **201** |
| 32, reduced + paths | 171.91 ms | 19,723,213 | 359 |
| 64, reduced + paths | 339.67 ms | 135,252,966 | 711 |

**Storing the paths doubles the settled count and roughly doubles the build**, because the arcs are
recovered by a second confined search per portal rather than retained from the first — an
implementation choice this spike made for clarity, and one a real implementation would not have to
make. 201 flat searches is the entry fee at the recommended rung, and it is cheap against a session.

### R3.3 — the query, and the currency that does not convert, a second time

*Cost only* answers *how long does this Trip take*; *+ refine* answers *which arcs*. They are timed
apart because they have different customers: R1 showed the travel-time matrix already answers the
first more cheaply than any search can, and `adr/0041` needs the second.

| Chunks | Cost only | vs flat | + refine | vs flat | Settled (abstract + insert) | Relaxed |
|---|---:|---:|---:|---:|---:|---:|
| flat A\* | 477,609 ns | 1.00× | — | — | 4,138 | 16,442 |
| 8 | 461,732 ns | 1.03× | 479,221 ns | 0.99× | 1,708 + 131 | 39,298 + 527 |
| 8, reduced | 280,603 ns | 1.70× | 303,122 ns | 1.57× | 1,708 + 131 | 6,351 + 527 |
| **8, reduced + paths** | 215,097 ns | 2.22× | **237,325 ns** | **2.01×** | 1,708 + 131 | 6,351 + 527 |
| 16 | 332,179 ns | 1.43× | 476,498 ns | 1.00× | 886 + 464 | 36,440 + 1,852 |
| 16, reduced | 166,010 ns | 2.87× | 316,993 ns | 1.50× | 886 + 464 | 3,143 + 1,852 |
| **16, reduced + paths** | 154,570 ns | **3.08×** | **181,554 ns** | **2.63×** | 886 + 464 | 3,143 + 1,852 |
| 32, reduced + paths | 187,765 ns | 2.54× | 210,385 ns | 2.27× | 422 + 1,731 | 1,381 + 6,866 |
| 64, reduced + paths | 824,441 ns | 0.57× | 746,193 ns | 0.64× | 166 + 8,360 | 544 + 33,095 |

**HPA\* over the complete abstraction expands 4.7× fewer nodes than the flat search and is 1.43×
faster.** That is R0's finding arriving a second time by a different door, and it is worth stating in
the general form: **a hierarchy that saves expansions has not yet saved anything.** The mechanism is
visible in the work columns rather than inferred. The flat graph has a mean degree of **3**; the
complete abstract graph at 16 Chunks has a mean degree of **40**, and the hierarchical search relaxes
**36,440** abstract edges where the flat search relaxes **16,442** concrete arcs. It settles a fifth as
many nodes and examines twice as many edges. **A road network is degree-3 and sparse, which is
precisely the graph on which an all-pairs abstraction is a bad trade.**

**The two halves of the *Settled* column move in opposite directions, and that is the whole shape of
the curve.** A larger cluster means fewer portals to search over and a larger insertion at each end;
at 64 Chunks the insertion alone settles 8,360 nodes — twice the flat search's whole expansion — and
the hierarchy is slower than no hierarchy at all.

**Storing paths converts the refined column from an upper bound into a measurement.** Without it,
recovering an intra-cluster edge's arcs re-runs the confined search that produced its cost, and
refinement costs 151,000 ns at 16 Chunks — more than the abstract search itself. With the arena, it
costs 27,000 ns. **This is R6's question answered early for the intra-edge half**, and it is why the
recommendation below is a *reduced + paths* rung rather than a *reduced* one.

### R3.4 — the Tick budget, which is the test R2 already wrote down

**A speedup is not a verdict.** R2 retired the searched path source on arithmetic, and that test
applies unchanged to **any** per-Trip search, including this one. A route must cost **28,363 ns** to
consume a whole 15.6 ms Tick on its own.

| Rung | Per refined route | **Break-even Trips/Tick** |
|---|---:|---:|
| flat A\* | 477,609 ns | **32** |
| 4, reduced + paths | 364,380 ns | **42** |
| 8, reduced + paths | 237,325 ns | **65** |
| **16, reduced + paths** | **181,554 ns** | **85** |
| 32, reduced + paths | 210,385 ns | **74** |
| 64, reduced + paths | 746,193 ns | **20** |

> **SUPERSEDED by R7's re-capture, and the whole table is one processor's.** Every row here was taken
> under `taskset -c 2`. Correctly pinned the best rung reads **138,641 ns and a break-even of 112**,
> not 85 — the corpus's most-quoted routing figure is **32% low** — and 8 reads 69 rather than 65.
> The tripwire's *form* survives, being a measured cost over a world constant; its number did not.
> Retained rather than corrected in place, because what the number was when the recommendation below
> was written is the record.

**The break-even column is the finding, and it is stated this way deliberately.** *Break-even
Trips/Tick* is a measured per-route cost divided by a world constant and contains nothing derived, so
it stays true when the arrival rate is finally measured. The obvious alternative — multiplying by the
working figure of ~550 Trip starts per Tick and reporting *6.4× over budget* — **buries a guess inside
a tripwire**. 550 comes from ~56,000 Trips in flight over a mean Trip duration the corpus records as
provisional, and S2 has no Travellers, no Trip generation and no Event Wheel to produce a better one.
**A tripwire whose denominator is a guess is a tripwire that fires on the guess.** The general rule,
and it outlives this spike: *gather a tripwire as direct data where the data exists, and where it does
not, invert the derivation until what is published is measured.*

**A route is requested per Trip, not per Tick per Traveller.** An earlier draft of this section said
"a concrete route every Tick", which is wrong and flatters the problem in the wrong direction: a
Traveller in flight consults a route it already holds, and the per-Tick per-Traveller cost is
advancing along it. What costs a search is a Trip *starting*. That is the quantity the break-even
column is denominated in.

**No cluster size fits, and the shape of the curve says none can.** The load is U-shaped in cluster
size and both ends are pinned by the same thing R3.3's *Settled* column shows: a small cluster makes
the abstract search approach the flat search, a large one makes the *insertion* approach it.
`adr/0040` admits only whole-Chunk clusters that tile the map, so the admissible rungs are the
divisors of 128 and the minimum sits at one of them with both neighbours worse. **This is a floor,
not a rung that was missed.**

**Two exits, and neither is free.** A **cache** — `adr/0012` permits one keyed by origin-destination
pair, and `plans/0010` R6 owns it — would have to serve all but ~85 route requests per Tick at the
best rung, which at the working arrival figure is roughly a 92% hit rate. **That promotes R6 from a
late tidy-up to a load-bearing task**, and it is the single largest change R3 makes to the plan. Or
**threads**: invariant 4 is thread-count equivalence, so the best rung's load spread over eight cores
fits — by spending the whole Tick budget of eight cores on routing, which is a mortgage rather than a
solution.

**R2's next-hop table is the rung this arithmetic does not touch**, because it does no per-Trip search
at all — 0 ns to start a Trip and 32 ns per crossing. That is a structural advantage over both
hierarchies rather than a faster constant, and it is **R4's** to press.

### R3.5 — the detour reads zero, and it is a property of the design

Every rung: **100% optimal, 0.00% mean detour, 0 routes cheaper than the flat optimum, 0 audit
failures over 200 refined routes per rung.** Beside R2's 18.52% for a next-hop table and 36.01% for a
shared District route, that is the column HPA\* wins outright. **The mean is over every query
compared, optimal ones included at zero**, which is what makes it the same quantity R2 published
rather than a mean over survivors.

**A zero at every rung is exactly the shape R2 caught a defect wearing**, so it is argued rather than
asserted. Keeping every crossing as a transition makes the abstraction **complete**: any concrete path
decomposes into cluster crossings, and between two consecutive crossings the path is confined to one
cluster and runs portal-to-portal, whose confined optimum is exactly what an intra-edge stores. The
insertion is lossless for the same reason — a route's first exit from the origin's cluster is a portal
reachable within it. **The abstraction cannot lose a route, so it cannot return a longer one.**

Botea's ~1% suboptimality comes from **entrance grouping**, which exists because a tile grid's cluster
boundary is a solid run of hundreds of walkable cells. A road network's boundary is already sparse —
it is crossed only where a Street or an Arterial crosses it — so the grouping step has nothing to
group.

**A zero correctness column is not evidence until the instrument is shown to move**, and R3.6 is where
it moves: the same column reads 80.49% under transition sampling, on the same graph and the same query
set. **The audit is the other half of that.** It re-walks refined routes and requires the entry
partial, the arc costs and the exit remainder to sum **exactly** to the cost the query reported, with
the arcs forming an unbroken chain — an equality rather than a tolerance, because Q16.16 addition is
exact. It is R2's *"an invariant is worth printing on the run where it reads yes"* applied in advance
for once, rather than after a harness had published a `v/c` of 883×.

### R3.6 — the sparser abstraction, and what Botea's lever actually costs here

Swept at 16 Chunks, the rung R3.3 makes the best of — chosen from the measurement rather than in
advance.

| Transitions per boundary | Portals | Edges | Degree | Query | vs flat | Optimal | Mean detour | Worst |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 206 | 552 | 2 | 55,975 ns | 8.53× | 9.48% | 80.49% | 345.62% |
| 2 | 428 | 2,208 | 5 | 56,495 ns | 8.45× | 13.84% | 65.57% | 609.76% |
| 4 | 872 | 9,072 | 10 | 104,860 ns | 4.55× | 20.12% | 60.78% | 603.86% |
| 8 | 1,746 | 36,470 | 20 | 187,363 ns | 2.54× | 26.50% | 36.99% | 239.62% |
| all | 3,337 | 133,816 | 40 | 339,039 ns | 1.40× | 100.00% | 0.00% | 0.00% |
| **all, reduced** | **3,337** | **11,768** | **3** | **148,691 ns** | **3.21×** | **100.00%** | **0.00%** | **0.00%** |

**Two findings, and the second is the one that changes the answer.**

**Sampling transitions is catastrophic on this graph.** One transition per boundary buys 8.53× and
returns a route **80% longer on average**, with a worst case of 3.5×. `05 §4`'s test is not ambiguous
about what that is: a different route is a different Trip and therefore a different city, so this is
not a faster router but a wrong one. Whatever HPA\*'s literature speedups are, **they are bought with
a currency this design does not have.**

**The transitive reduction is lossless, and skipping it is what made HPA\* look weak.** An intra-edge
is redundant when another portal of the same cluster lies on a route between its ends costing no more.
Removing those is safe because every arc cost is strictly positive, so both hops of a replacement are
strictly cheaper than the edge they replace and removals cannot cascade — and **R3.5's optimality
column is the proof rather than the argument**, reading 100% for every reduced rung. It cuts the
abstract graph from 133,816 edges to **11,768**, degree 40 to **3** — the flat graph's own degree —
resident size to 229.45 KiB, and **more than doubles the speedup at zero correctness cost**.

**What it costs is repairability, and that is a finding rather than a caveat.** Redundancy is a
property of the *costs*, so an edit that lengthens one route can make a removed edge necessary again,
and no amount of re-costing the remaining slots brings it back. **A reduced cluster's edge set must be
decided again, not repaired** — which is a cluster-local rebuild, not a whole-graph one, and R3.7
measures it rather than deriving it.

### R3.7 — invalidation, which is the half of the core verb R3 can price

One Segment deleted. Only the clusters holding that Segment's endpoints can have changed, so only
their portals' confined searches re-run. *In a city builder link deletion is the core verb*, which is
this plan's own argument against distance-vector without sequence numbers, and it cuts at a hierarchy
too.

| Rung | Operation | Cost | Clusters touched | Share of cold build |
|---|---|---:|---:|---:|
| 8, reduced | rebuild cluster | 313,478 ns | 1.03 | 1.18% |
| **8, reduced + paths** | rebuild cluster | **375,723 ns** | 1.03 | 0.83% |
| 16 | re-cost | 754,767 ns | 1.03 | 1.85% |
| 16, reduced | rebuild cluster | 753,829 ns | 1.03 | 1.25% |
| **16, reduced + paths** | rebuild cluster | **1,296,680 ns** | 1.03 | 1.35% |
| 32, reduced + paths | rebuild cluster | 10,417,111 ns | 1.03 | 6.05% |
| 64, reduced + paths | rebuild cluster | 77,482,496 ns | 1.00 | 22.81% |

**Two operations, and which one is sound is a property of the rung.** A complete abstract graph keeps
every intra-edge, so *re-costing* the slots is exact. A reduced one must decide its edge set again, per
R3.6. An earlier draft of this section derived the reduced figure from the per-cluster build column
and quoted ≈1.2 ms; the measurement is 1,296,680 ns, so the derivation was close — **and being close is
not the point.** The rule R3.4 states about tripwires applies to any number a decision rests on:
measure it where the measurement is available, and this one was three hours of work away.

**Below 8 Chunks the rebuild column is mostly this harness**, and the capture says so in place: a
rebuilt cluster is spliced back into one global CSR, kept global so the query path measured above is
the one a real implementation would run, and the splice copies every edge in the graph. At one Chunk
that is 64,134 edges and most of the 469 µs; at 16 Chunks it is 11,768 and a couple of percent.

**Deletion only, and the limit is structural rather than an omission.** Both operations work over the
portals the build found, so either may cost an edge out of existence but neither can create a portal
that did not exist — which is what *drawing* a road across a cluster boundary does. **R5's edit storm
owns the drawing half**, and it is also the task that weighs these figures, because what weighs them
is the edit rate.

### R3.8 — the bypass, and a plan assumption that S2 cannot confirm

`plans/0010` makes the same-Segment and adjacent-Segment bypass **mandatory rather than an
optimisation**, on the argument that with five Buildings on a Segment a meaningful share of the ~464
walk Leg routes per Tick never leave their own Segment or its neighbour.

**S2 cannot report that share, and reporting one would be a guess wearing a measurement's clothes.**
The spike has no Leg distribution — R0 said so in its own sampler and bucketed everything instead, so
whatever distribution arrives later is applied as weights over buckets that already exist. Bucketed,
over 20,000 drawn walk Legs:

| O-D distance | Legs | Same Segment | Adjacent | Bypassed |
|---|---:|---:|---:|---:|
| ≤ 32 Tiles (one block) | 175 | 15.42% | 62.85% | **78.28%** |
| ≤ 64 | 341 | 0.00% | 1.75% | 1.75% |
| ≤ 128 and beyond | 19,484 | 0.00% | 0.00% | 0.00% |

**The bypass is worth everything inside one block and nothing outside it**, and the cliff is sharp
rather than gradual: it is 78% at a block and 1.75% at two. So the plan's claim is **conditional on a
distribution nobody has measured** — it is true if and only if walk Legs are overwhelmingly
single-block, and the corpus has never said they are. **What the bypass costs to decide is two Segment
comparisons and at most four endpoint comparisons**, so it stays mandatory on cost grounds regardless
of how the weights land; but *the plan's reason for it is not yet evidence.*

### What R3 decided, and what it did not

**Decided.**

- **Cluster size is narrowed to 8 or 16 Chunks a side, with the bias on 16 — and R3 does not close
  it.** The plan says R3 decides cluster size *outright*; it cannot, and the reason is a measurement
  rather than a reluctance. The axis that separates the two rungs is the **edit rate**, and **R5**
  measures it.

  | Rung | Refined query | Break-even Trips/Tick | Cold build | Edit | Resident |
  |---|---:|---:|---:|---:|---:|
  | 8, reduced + paths | 237,325 ns (2.01×) | 65 | 45.03 ms | **375,723 ns** | 765 KiB |
  | **16, reduced + paths** | **181,554 ns (2.63×)** | **85** | 96.04 ms | 1,296,680 ns | **453 KiB** |

  **The bias is on 16 because the query is a per-Tick cost and the edit is a per-click one.** 16 is
  1.31× faster on the column that has a customer and carries 31% more Trips per Tick before the
  budget breaks; it pays for that with **0.92 ms more on each deleted Segment**, at an edit rate a
  human generates by hand. Both figures are under 1.3 ms and a player cannot perceive the difference
  between them; the query difference is spent every Tick of the session. **What could still overturn
  it is a drag that deletes hundreds of Segments in one gesture**, which is exactly R5's storm, so
  the decision defers rather than closes.

  **Two earlier drafts of this bullet got it wrong in opposite directions**, and both are recorded
  because the error is instructive. The first published 16 outright and justified it on the
  *cost-only* column — the same column it had just argued the travel-time matrix serves better. The
  second corrected to 8 on the refined column, before the path store existed; storing paths moved
  16's refined query from 1.53× to 2.63× and moved the answer back. **A recommendation is only as
  stable as the configuration it was measured on.**

  Chunk size is *informed and not decided*, per `adr/0040`. At Phase 1's provisional Chunk = Cell
  this is a cluster **8–16× the Chunk in side**; the Chunk is in the save and the cluster is not, so
  this decision costs a recomputation to change and nothing else, forever.
- **The stored path arena is mandatory alongside the reduction.** It converts refinement from a
  re-run confined search into an array copy, moves the refined query from 1.50× to 2.63× at 16
  Chunks, and costs **223.92 KiB** — a fifth of the estimate it was feared at, because the reduction
  had already removed 91% of the edges that would have carried arcs.
- **`adr/0014`'s *"the Chunk grid is already the pathfinding cluster"* is measured false.** At one
  Chunk the abstract graph is the Road Graph — 16,694 portals against 16,697 nodes — and the query
  expands exactly what the flat search expands. `adr/0040` corrected the claim structurally; this is
  the number.
- **The transitive reduction is mandatory.** Lossless, 11.4× fewer abstract edges, degree 40 → 3,
  double the speedup, and R3.5's optimality column proves it. An HPA\* implementation that skips it
  measures a hierarchy that is barely faster than no hierarchy.
- **Transition sampling is out.** 80.49% mean detour at one transition per boundary is a different
  city under `05 §4`'s own test, whatever it buys.
- **HPA\* is optimal on this graph**, which is the column it wins outright: 100% against R2's 18.52%
  and 36.01%.
- **`plans/0010` R6 — the route cache — is promoted from a late optimisation to load-bearing.** No
  cluster size fits routing into the Tick budget, and the gap is 6.4× at the best rung against the
  working arrival figure. A cache is one of only two exits and the only one that does not mortgage
  eight cores. **R6 is now a task the router choice depends on rather than one that tidies up after
  it**, and R4's comparison should be read knowing that whichever router wins will need it.

**Not decided, and owed.**

- **The router is not chosen, and R3 weakens the standing rather than confirming it.**
  `plans/0010` records *current standing favours HPA\**. What HPA\* actually buys is **3.08× on a
  cost-only query and 2.63× when it must return arcs** — and R1 already showed the travel-time matrix
  answers the cost-only question at 1.14 ns, so the larger figure is against a customer that has a
  better answer already. R4 must now run against a genuinely open comparison, and it runs it knowing
  neither candidate fits the budget unaided.
- **The cluster is not closed, and `plans/0010`'s *decides cluster size, outright* is owed a
  correction.** R3 narrows it to two rungs and cannot separate them, because the axis that separates
  them is the edit rate.
- **The O-D draw is uniform over the map, and R0 flagged that as a placeholder that was never
  replaced.** R0 said it could not have the distribution it was supposed to use and would take R1's;
  R1 produced none, and R3 inherited the uniform draw unchanged. **A uniform draw over a 4,096-Tile
  map produces long routes**, and long routes are where a hierarchy wins by the widest margin — so
  every speedup in R3.3 is measured on the distribution most favourable to HPA\*. It does not
  threaten the optimality columns, which are counts, and it does not threaten the *ranking* of the
  rungs against each other; it does mean **the speedup over flat A\* is an upper bound**, and R3.8's
  bypass table shows how thin the short end of the real distribution might be.
- **The canonical `performance` capture is owed**, and until it exists no absolute nanosecond figure
  in this section should be quoted outside it — including R3.4's, which is the one place R3 divides
  a measured absolute by a world constant.
- **The plan's argument for the bypass is unconfirmed.** The bypass stays on cost grounds; the
  reason given for it needs a walk-Leg distribution the corpus does not have.
- **The largest exposure in R3 is one it measured around rather than through: the abstract graph is
  built on free-flow costs, and the simulation routes on congested ones.** `CONTEXT.md` → Epoch bumps
  on *any edit* — a topology change — and the corpus says nothing anywhere about invalidating anything
  when **congestion** changes. But the VDF makes a Segment's travel time a function of its
  `volume / capacity`, `adr/0041` moves volume **every Tick**, and `02 §5.9` requires the router to
  route on that same quantity. **The flat search has no exposure at all** — it reads arc costs at
  query time and is always current, which is a structural advantage of the denominator that R3 never
  priced. **HPA\*'s intra-cluster edges are shortest-path costs *over* those arc costs**, so every one
  of them goes stale with no Epoch bump to flag it.

  **This is R1.7's finding arriving a third time.** R1 recorded *"two invalidation mechanisms are in
  the corpus and nothing relates them"* — the matrix by dirty region, routes by scalar Epoch. Cost
  drift is a third, with **no mechanism at all**, and it is the one carrying 96 ms of rebuild.

  **It does not favour either router**: a next-hop table built over costs is stale by exactly the same
  argument, so R4 inherits it unchanged. What decides it is the **refresh cadence of the routing cost
  basis**, which the corpus has never stated. R1.8 found the matrix is built per time-of-day phase; if
  routing shares that cadence, the abstract graph rebuilds a handful of times a Day at 96 ms and the
  exposure evaporates. If routing must track volume continuously, **HPA\* and the next-hop table are
  both dead** against a 15.6 ms Tick budget. **R5 must have that cadence as an input before its edit
  storm means anything**, because an edit storm over a cost basis that is already being rebuilt
  continuously is measuring the wrong storm.

---

## S2 R4 — distance-vector, and the table that has to stay current

> `plans/0010-s2-routing.md` R4. Raw capture in
> `spikes/S2.Routing/results/s2-r4-intel-core-i5-10400-ddr2133-performance-turbo-cpu2-20260806T224402Z.md`; the `powersave`
> capture it is checked against is beside it as `…-powersave-turbo.md`.

**R4 is not a beauty contest between two routers**, and reading it as one would miss what R3 handed
it. R3 found that no cluster size fits a per-Trip search into the Tick budget — the best rung breaks
even at 85 Trip starts — and named R2's next-hop table as *"the rung this arithmetic does not touch…
a structural advantage over both hierarchies rather than a faster constant, and it is R4's to
press."* That table asks for **no search at all** when a Trip starts and 32 ns per Segment crossing
after. At the derived 56,000 Travellers in flight and R2's measured 0.79 crossings each per Tick,
that is **1.24 ms of a 15.6 ms Tick**. It fits.

**So R4's subject is what that table costs to maintain**, and its answer arrives in two halves that
point opposite ways: the maintenance question resolves cleanly and in favour of a scheme nobody had
named, and the **granularity** question — which R2 opened and R4 was not looking at — gets very much
worse.

### The capture, and what it is not

**Canonical.** `performance` governor, turbo enabled, pinned to one physical core, run as root via
`spikes/S2.Routing/tools/routing-run.sh --vector`; 23.69 s, CPU stall 1.41%, memory stall 0.00%.
Every figure below is from that capture.

**The determinism check is the strongest this spike has produced.** Against the `powersave` capture
taken twelve minutes earlier, **every non-timing row is bit-identical** — relaxations, rounds,
wrong-entry counts, stranded counts, arc counts, detour percentages, footprints and the whole O-D
distribution table. Only nanosecond columns move, and they move by 2–7%. **A governor change moved
nothing that R4 concludes from**, which is what a capture protocol is for.

### R4.1 — the origin-destination distribution, which S2 had been guessing since R0

R0 drew pairs uniformly and said so, flagging it as a placeholder for the distribution R1 would
derive. **R1 derived none; R2 and R3 inherited the placeholder unchanged**, and R3 had to publish
every speedup as an upper bound because of it.

| Shape | Mean | Median | p90 | Mean route | Draws/pair | Exhausted |
|---|---:|---:|---:|---:|---:|---:|
| **uniform** | **8.53 km** | 8.38 km | 14.05 km | 71.52 Ticks | 1.00 | 0 |
| decay L=1024 | 5.24 km | 4.65 km | 10.13 km | 47.26 Ticks | 5.32 | 0 |
| decay L=512 | 3.30 km | 2.79 km | 6.66 km | 35.04 Ticks | 14.97 | 0 |
| decay L=256 | 1.83 km | 1.51 km | 3.58 km | 23.22 Ticks | 50.53 | 0 |
| monocentric L=512 | 7.22 km | 7.06 km | 11.70 km | 61.72 Ticks | 11.14 | 0 |

2,000 pairs per rung; route cost over the first 150. **This does not close the hole; it makes the
hole an axis.** Nobody can produce the real distribution until Trips exist, and inventing one and
calling it *the* distribution would bake a guess into every downstream figure while making it look
like a measurement — which is precisely why R0 drew uniformly and was right to. What is available is
this plan's own precedent: **report a curve, do not choose a number.**

**Uniform is a rung of the same sampler rather than a separate path**, so a difference between rows
is the shape and cannot be the machinery. This spike has twice caught an instrument that could not
move and once caught two rungs that were secretly the same rung; a family whose null case is a member
of the family is the cheap defence against both.

**The finding is that uniform is not a neutral default — it is the extreme rung.** At 8.53 km mean on
a 16.4 km map it is the longest-trip distribution available, and R4.8 shows that is exactly where the
next-hop table looks best.

### R4.2 — the memory wire fires, and it fires on granularity rather than on the protocol

| Destinations | Granularity | Unsequenced | **DSDV** | Against the world |
|---:|---|---:|---:|---:|
| 121 | District, 11 a side | 15.41 MiB | **23.12 MiB** | 0.13× |
| 400 | District, 20 a side | 50.95 MiB | 76.43 MiB | 0.44× |
| 1,024 | District, 32 a side | 130.44 MiB | 195.66 MiB | 1.13× |
| 16,697 | **node** | 2.07 GiB | **3.11 GiB** | **18.51×** |

Against K0's 172.27 MiB world. Sequence numbers are a third of the table, so DSDV costs 50% more
than the bare next-hop table R2 measured at 7.70 MiB.

**At District granularity the wire does not fire.** At node granularity — the destination set an
actual routing table carries, and the one Citybound's does — it fires by 18.5×, and **sequence
numbers neither cause that nor would removing them fix it.** So distance-vector in this design can
only ever address Districts, which is not a footnote about memory: **it is what imports R2's detour
and the representative funnel**, because a destination the table can afford to name is a District and
not a place. The correctness cost is *caused by* the memory constraint, and the corpus has discussed
the two separately everywhere.

### R4.3 — cold start, and the rung that was expected to lose does not

| Build | Whole table | Per column | Relaxations | Worst rounds | Entries wrong |
|---|---:|---:|---:|---:|---:|
| backward Dijkstra | 423.47 ms | 3,499,760 ns | — | — | — |
| vector exchange | **109.52 ms** | 905,163 ns | 14,333,149 | 175 | **0** |

Bellman-Ford with an active set beats a binary-heap Dijkstra on this graph, because a degree-3
network with well-behaved costs settles nearly in order anyway and the heap is pure overhead. **An
earlier draft of this paragraph asserted the opposite**, written before the column existed; it is
recorded because a spike whose prose predicts its own numbers will eventually publish the prediction
instead of the number.

**This does not show distance-vector is cheap.** Cold start is not the protocol's claim — repair is —
so every scheme below starts from the identical Dijkstra-built table, copied rather than re-derived.

### R4.4 — one deleted Segment, which is the core verb

| Scheme | Per edit | Against rebuild | Relaxations | Wrong cost | Stranded |
|---|---:|---:|---:|---:|---:|
| **rebuild** — every column | 234.74 ms | — | — | — | — |
| DSDV, sequenced | 500.69 ms | **2.13× slower** | 36,982,307 | 0 | 0 |
| DSDV, unsequenced | 32.74 ms | 7.16× faster | 2,510,526 | 0 | 0 |
| **dynamic repair** — affected subtree | **4.71 ms** | **49.76× faster** | 201,014 | **0** | **0** |

8 deleted Segments, each repaired across all 121 columns; 16,162,696 entries audited per scheme
against a table rebuilt on the edited graph.

**DSDV is slower than the rebuild it exists to avoid, and the reason is structural rather than a
constant.** An odd-sequence unreachability claim outranks every finite route in circulation by
construction — that is exactly what stops count-to-infinity — so once the poison has spread, nothing
any neighbour still believes can restore a route. Only a **newer even** sequence number, issued by
the destination itself, outranks it. **One broken link therefore obliges the destination to re-flood
its entire tree**, because every node must at minimum accept the new number. *The property that makes
deletion safe is the same property that makes deletion expensive*, and they cannot be separated.

**The scheme that wins was not on the ballot.** Invalidating the affected subtree and re-deriving it
from its own valid boundary is not distance-vector, needs no sequence numbers and no Epoch, and was
measured only because pricing solely the candidate a plan names is how a spike produces a verdict it
has not earned.

### R4.5 — a severed destination, and the claim `references.md` had only argued

`references.md` is categorical — *"if we adopt distance-vector routing, we take DSDV's version, not
Citybound's"* — because Citybound's entries carry no sequence numbers and link deletion
count-to-infinities. **Under `adr/0043` that is a measurable claim that had never been measured.**

| Scheme | Rounds | Relaxations | Converged | Wrong cost |
|---|---:|---:|---|---:|
| DSDV, sequenced | 121 | 133,258 | yes | **0** |
| DSDV, unsequenced | 4,096 | **215,894,753** | **no** — hit the cap | **16,684 of 16,697** |
| **dynamic repair** | 0 settles | 130,114 | yes | **0** |

Every arc into and out of one District's representative deleted — a bulldozed cul-de-sac.

**The claim is confirmed, and by a wide margin.** The unsequenced version does **1,620× the work**,
fails to converge within 4,096 rounds, and leaves all but 13 of 16,697 entries wrong. *Take DSDV's
version, not Citybound's* is now a finding rather than a reading. **It does not rescue DSDV**, which
loses R4.4 to a scheme that converges here too, at comparable cost and with nothing wrong.

### R4.6 — congestion drift, the exposure R3 left unpriced

The Epoch bumps on an *edit*; the VDF makes travel time a function of `volume / capacity`;
`adr/0041` moves volume **every Tick**. **A deleted road is the core verb; a changed travel time is
every Tick.**

| Arcs moved | Rebuild | DSDV, sequenced | Dynamic repair | Repair vs rebuild |
|---:|---:|---:|---:|---:|
| 0.10% (57) | 213.80 ms | 378.13 ms | 44.00 ms | **4.85×** |
| 1.00% (635) | 225.87 ms | 376.60 ms | 125.16 ms | **1.80×** |
| 10.00% (6,474) | 248.49 ms | 479.18 ms | 393.53 ms | 0.63× |
| 100.00% (64,138) | 236.62 ms | **5,059.97 ms** | 357.74 ms | 0.66× |

The 10% and 100% repair columns differ by less than the spread between the two captures, so **the
curve flattens past the break-even rather than continuing to fall**: once the affected subtree is
most of the graph, re-deriving it *is* a rebuild with bookkeeping attached.

**The break-even is between 1% and 10% of arcs moved**, and nothing in the corpus says which side the
design lands on, because the refresh cadence is `plans/0010` decision 2 and still open. Below it,
incremental repair wins; above it, **a plain rebuild wins and every incremental scheme is doing extra
work to reach the same table.** So **the cadence chooses the maintenance scheme** — which nobody has
said, about a decision filed as tuning.

### R4.7 — the rolling refresh, which needs none of this machinery

| Columns per Tick | Cost per Tick | Share of 15.6 ms | Worst staleness |
|---:|---:|---:|---:|
| 1 | 1,682,673 ns | **10.78%** | 121 Ticks |
| 4 | 6,730,692 ns | 43.14% | 31 Ticks |
| 121 | 203,603,433 ns | 1305.14% | 1 Tick |

**A rebuild is not the fallback anybody feared** — 1.68 ms per column, so a full rotation every 121
Ticks costs a tenth of one Tick. What it cannot do is answer an *edit* promptly, because a rotation
is a cadence and an edit is an event. **Drift wants a slow rotation and the core verb does not**, so
the two consumers need different mechanisms.

### R4.8 — the finding R4 was not looking for, and it is the largest one

R2 measured a next-hop table's mean detour at **18.52%** — on the uniform draw R4.1 has now shown to
be the longest-trip rung available. Aiming a Traveller at a District **representative** is a roughly
fixed error in Ticks charged against a shrinking journey, so it should worsen as trips get shorter.

| Shape | Mean O-D | Mean detour | p90 | Worst |
|---|---:|---:|---:|---:|
| uniform | 8.53 km | **20.14%** | 45.65% | 815.18% |
| decay L=1024 | 5.24 km | **36.04%** | 69.24% | 1347.93% |
| decay L=512 | 3.30 km | **62.02%** | 154.58% | 1835.31% |
| decay L=256 | 1.83 km | **128.82%** | 241.79% | 5165.15% |
| monocentric L=512 | 7.22 km | 24.97% | 51.59% | 791.76% |

At the morning peak, ~197 samples per rung. The tail search starts at a Segment incident to the
representative rather than at the node, which can only make the followed route look cheaper, so
**these are lower bounds**; R2's, at node granularity, was an upper one, and the two bracket rather
than contradict.

**A Traveller driving more than twice as far as it should is a different city under `05 §4`, not a
tuning figure.** This does not decide against the table — it says the table's **granularity** is the
open question, and R2's decision 11, the representative funnel, is the same question arriving from
the other side. **The two must be answered once.**

### What R4 decided, and what it did not

**Decided.**

- **Distance-vector is out, on none of the three grounds anybody expected.** Not memory — at District
  granularity it is 23.12 MiB against a 172.27 MiB world and the wire does not fire. Not correctness
  — with sequence numbers it converges to exactly the rebuilt table, on a deleted Segment and on a
  severance alike. **It is out because it costs more than the rebuild it exists to avoid** (2.13×)
  and 106× more than the scheme this plan never named.
- **`references.md`'s sequence-number claim is confirmed by measurement**, 1,620× the work and still
  wrong without them. Under `adr/0043` it had been an argument; it is now a number.
- **Dynamic subtree repair is the maintenance scheme**, at 4.71 ms against a 234.74 ms rebuild, 0
  entries wrong, and it converges on a severance too.
- **A full rebuild is affordable as a rotation** — 10.78% of a Tick for a full pass every 121 Ticks —
  and unaffordable as an edit response.

**Not decided, and owed.**

- **The next-hop table's error is far worse than R2 measured**, and R4 does not know what to do about
  it. R4.8 is a granularity question, not a routing one, and it belongs with R2's representative
  funnel.
- **The drift break-even sits between 1% and 10% of arcs moved**, so the matrix refresh cadence
  chooses the maintenance scheme. Filed as tuning; it is not.
- **The O-D family is an axis, not a measurement.** What would replace it is Trip generation, which
  does not exist. Every figure in R4.8 — and every speedup R3 published — is a point on a curve
  nobody can yet locate.

**Four defects in R4's own harness, all caught by instruments rather than by reading.**

- **The sequenced protocol was missing DSDV's acceptance rule** — a node must *reject* an
  advertisement older than what it already holds, not merely prefer newer ones. Without it a poisoned
  node kept its odd sequence number while adopting a neighbour's stale finite cost, then advertised
  that stale cost under the high sequence its own poison had earned. **The first capture read 232
  seconds per edit and would have published *distance-vector loses by three orders of magnitude*.**
  With the rule it is 500.69 ms. What flagged it was R2's recorded lesson: the two protocols were
  reporting near-identical relaxation counts and *identical* wrong-entry counts, and **two
  measurements that agree that closely are not two measurements.**
- **The poison phase was a silent no-op**, seeded with the nodes that detect the break rather than
  the nodes they advertise to. In DSDV the detector *advertises*; the advertisement is the event, not
  something a node discovers by looking around. It converged in 2 rounds and 24 relaxations while
  leaving 16,680 of 16,697 entries wrong — **and reported "converged: yes", because a phase that does
  nothing does it very quickly.**
- **The audit counted the destination itself as stranded**, one phantom per column, which read as a
  suspiciously round 121. **A defect that produces a plausible number is worse than one that produces
  an absurd one.**
- **The elapsed-time helper overflowed.** `elapsed × 1,000,000,000` passes `long.MaxValue` at ~9.2 s
  on a nanosecond clock, and the first capture published **−8,267.51 ms** for the rung that then took
  four minutes. Every earlier S2 section times loops far below that threshold, so the expression has
  been correct everywhere else in this harness — **a helper is only as safe as the largest quantity
  anybody has yet asked it to measure.**

### R3's denominator finding reproduces a third time, and it reconciles R2

R4.3 measures 121 backward Dijkstras at **423.47 ms**; R4.4 measures the identical rebuild at
**234.74 ms** — **1.80× apart in one process**, the earlier one first. That is R3's *a denominator
measured first has a systematic error* arriving again without being looked for, and it substantially
explains why R2 published 474.47 ms for the same operation: R2's was also a first-timed measurement.
**Every R4 ratio is taken in-process against R4's own figure**, per R3's rule, so no conclusion moves
— but two S2 tasks publishing different absolutes for one operation is R7's to reconcile.

---

## S2 R5 — the edit storm, the gesture, and the Epoch that has to carry a location

> `plans/0010-s2-routing.md` R5. Raw capture in
> `spikes/S2.Routing/results/s2-r5-…-performance-turbo-cpu2+8-20260807T151916Z.md`, with two earlier
> captures of the identical configuration retained beside it and the unpinned `powersave` run as
> `s2-r5-…-powersave-turbo-unpinned-20260807T033838Z.md`.
> **Seven of R5's sections. R5.6, the Parking Shed, is not run**, and the plan says it is the one most
> likely to move the verdict below. **Two sections were not in the plan at all**: R5.4, *the
> addition*, exists because R5.3's recommended rung turned out to have a hole only a measurement
> could size; and R5.5.4, *the rotation*, exists because R5.5 would otherwise have published a
> mechanism's cost with no measurement of its benefit.

**R5 is where the two earlier tasks' unit turns out to have been wrong.** R3 priced one deleted
Segment and R4 priced one deleted Segment, and both said in their own words that the case they could
not reach was hundreds of Segments in a single gesture. **A player does not delete a Segment; a
player drags.** Everything below follows from measuring the gesture instead of the edit.

### The capture, which is canonical — and the artefact that had to be fixed to make it so

**Canonical: `performance`, turbo enabled, pinned, run as root through
`spikes/S2.Routing/tools/routing-run.sh --storm`.** The unpinned `powersave` capture is retained
beside it.

**The determinism check passes across four captures, and the absolutes now carry a spread rather
than a disclaimer.** Every count, share and percentage in R5.1, R5.3, R5.4, R5.5.2 and R5.5.3 is
**bit-identical** across three canonical runs and the unpinned `powersave` one — hit, stale, miss and
unroutable shares, gesture collection counts, clusters touched, revalidation words, detours, sample
sizes, forced-refresh rates, and the whole R5.4 addition table including its 9.22% / 16.71% / 62.65%
row. The only percentages that move are R5.2's *% of rebuild* column and R5.5.1's *naive ÷ coalesced*,
both ratios of two timings.

**The millisecond columns reproduce to about 1% except where the absolute is small.** Across the two
full canonical captures the next-hop gesture repair reads 20.28 and 20.38 ms, the shared rebuild
181.61 and 179.32 ms, and the flat control's mean Tick 6,821 and 6,664 µs — while the cache's
2.75 ms gesture repair also read 3.67 ms, a **1.33× spread on the smallest absolute in the section**.
Read the small numbers as bands.

**Getting a canonical capture took two attempts, and the failed one is the finding.** The protocol
pinned with `taskset -c 2` — a physical core with its SMT sibling deliberately left idle. On a
machine with one logical processor visible, the .NET tiered JIT's background compilation has nowhere
to run but the measured core, and it lands on whatever is timed first. Measured directly, in one
capture, by the denominator this task already takes twice:

| Configuration | Rebuild @8, first vs last | Rebuild @16, first vs last | CPU stall |
|---|---:|---:|---:|
| `powersave`, unpinned (12 processors) | 50.88 / 50.43 ms — 1.00× | 78.88 / 83.89 ms — 0.94× | 0.88% |
| `performance`, `taskset -c 2` (1 processor) | **214.94 / 43.99 ms — 4.88×** | 81.24 / 76.13 ms — 1.06× | **3.68%** |
| **`performance`, `taskset -c 2,8` (2 processors)** | **43.14 / 46.64 ms — 0.92×** | 75.31 / 81.31 ms — 0.92× | **1.26%** |

The one-processor run inflated the whole first-timed half of R5.2's table by ~3× and **reversed the
8-versus-16 cluster verdict on its face** — 8 read 10.72 ms against 16's 6.52 ms, where both the
unpinned and the correctly-pinned captures put 8 ahead by ~1.9×. It also read a *slower* 8-Chunk
rebuild measured first than `powersave` managed while reading a *faster* one measured last, which is
the shape that identifies it as first-timed contamination rather than a slow machine.

**This is `plans/0000-board.md`'s open question about R0, answered.** *Why plain Dijkstra's absolute
moved 1.64× under pinning* was filed as a hypothesis — *taskset leaves one visible logical processor
and tiered-JIT background compilation shares the measured core* — with the check named as *re-run in
reverse order, or with tiering disabled*. Neither was needed: the twice-measured denominator makes
the artefact visible **within a single capture**, at two rungs, and giving the JIT the SMT sibling
removes it. `routing-run.sh` now reads `thread_siblings_list` from the kernel and pins to both
threads of the named core. **Sixth instance in S2 of *an argument for reporting a quantity you expect
to be boring*, and the first where the boring quantity diagnosed the harness rather than the result.**

**Every earlier `performance` capture in this spike was taken at `taskset -c 2`** — R0, R1, R3 and R4
alike, including the session-eleven canonical re-capture this document quotes throughout. Their
counts and in-process ratios are unaffected, and their **first-timed absolutes are not**. Filed as a
debt below.

### R5.1 — the gesture, which is the unit R3 and R4 could not reach

A drag follows the road network from a drawn Segment, preferring to continue straight. A scattered
gesture draws the same count uniformly over the map and is a **control, not a scenario** — nobody
drags scattered; the row exists so the drag's locality has something to be locality *against*.

| Gesture | Asked | Collected | Arcs | Clusters @8 | Worst @8 | Clusters @16 | Worst @16 |
|---|---:|---:|---:|---:|---:|---:|---:|
| drag | 1 | 1.00 | 2.00 | 1.00 | 1 | 1.00 | 1 |
| drag | 16 | 16.00 | 32.00 | 2.75 | 3 | 2.12 | 3 |
| drag | 64 | 64.00 | 128.00 | 8.00 | 11 | 4.50 | 7 |
| **drag** | **256** | **173.12** | 346.25 | 15.87 | 23 | **7.00** | 11 |
| scattered | 16 | 16.00 | 32.00 | 17.25 | 18 | 14.50 | 16 |
| scattered | 64 | 64.00 | 128.00 | 62.37 | 64 | 41.25 | 45 |
| **scattered** | **256** | **256.00** | 512.00 | 172.12 | 178 | **63.12** | 64 |

8 gestures per row. The partition is 256 clusters at 8 Chunks and **64** at 16.

**A drag saturates at 173 of 256.** It runs into road it has already deleted and stops; the shortfall
is reported rather than topped up from a fresh start elsewhere, because topping up would silently
turn one gesture into several and flatter every locality figure that follows.

**Locality is large and it is the whole finding.** A 256-Segment drag touches **7 clusters of 64** at
16 Chunks; the same count scattered touches **63 of 64** — which is to say a scattered gesture is a
full rebuild wearing a repair's name. Every cost in R5.2 is a function of this column and nothing
else.

### R5.2 — what a gesture costs to repair, and the loop that must not be written

The rebuild is the denominator, measured on both sides of the sweep **and at both cluster rungs**.

- Full abstract-graph build at **8 Chunks**: 43.14 ms first, 46.64 ms last — 0.92× apart.
- Full abstract-graph build at **16 Chunks**: 75.31 ms first, 81.31 ms last — 0.92× apart.

| Cluster | Gesture | Got | Clusters | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced | % of rebuild |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 8 | drag 1 | 1 | 1 | 0.26 ms | 0.53 ms | 0.31 ms | 0.58 ms | 1.20× | 0.60% |
| 8 | drag 64 | 64 | 8 | 2.00 ms | 3.20 ms | 12.44 ms | 18.68 ms | 6.21× | 4.64% |
| **8** | **drag 256** | 173 | 15 | **3.74 ms** | **5.42 ms** | 27.65 ms | 43.58 ms | 7.38× | 8.67% |
| 8 | scattered 256 | 256 | 172 | 42.12 ms | 44.18 ms | 63.18 ms | 66.73 ms | 1.50× | 97.62% |
| 16 | drag 1 | 1 | 1 | 1.19 ms | 1.81 ms | 1.21 ms | 1.93 ms | 1.01× | 1.59% |
| 16 | drag 64 | 64 | 4 | 4.38 ms | 7.42 ms | 81.05 ms | 124.70 ms | 18.47× | 5.82% |
| **16** | **drag 256** | 173 | 7 | **7.20 ms** | **11.63 ms** | **167.71 ms** | **253.22 ms** | **23.26×** | 9.57% |
| 16 | scattered 256 | 256 | 63 | 77.54 ms | 82.15 ms | 358.55 ms | 411.70 ms | 4.62× | **102.96%** |

**The naive per-Segment repair loop is the spelling R3 and R4 both measured, and on a drag it is a
catastrophe.** A cluster's edge set is a function of its arcs, so it must be decided **once** however
many Segments inside it were deleted; repairing per Segment re-decides the same few clusters dozens
of times. At 16 Chunks the two spellings differ by **23.26×**, and the naive worst case is
**253.22 ms — sixteen times the entire 15.6 ms Tick budget, from one player gesture.** The two
spellings are *identical at a gesture of one*, which is the only size either earlier task measured,
so this cost was structurally invisible to both. **It is not an implementation note. A per-edit
repair API invites the loop that produces it**, which is why the coalescing now lives on
`AbstractGraph` rather than in the harness.

**Repair loses to rebuild, and the crossover is on the map.** A scattered 256-Segment gesture at 16
Chunks costs **103% of a full rebuild** — repairing is strictly worse than starting again. This is
R4.6's incremental/rebuild break-even arriving at the abstract graph rather than at the matrix, and
it lands where a gesture stops being local. **A production repair path needs the rebuild as its own
fallback**, chosen on clusters touched.

#### R3's cluster size closes, and it closes against R3's bias

R3 narrowed cluster size to 8 or 16 Chunks a side, **put the bias on 16** on a 1.31× faster refined
query, and said explicitly that it could not close it because the axis separating them is an edit
rate R5 owns. R5 owns it, and it points the other way:

| | Cluster 8 | Cluster 16 |
|---|---:|---:|
| 256-Segment drag, coalesced | **3.74 ms** | 7.20 ms |
| …worst gesture | **5.42 ms** | 11.63 ms |
| …naive worst | **43.58 ms** | 253.22 ms |
| Full rebuild | **43.14 ms** | 75.31 ms |

**8 is cheaper on every edit row measured**, by 1.9× on the coalesced drag and 5.8× on the naive
worst case. R3's own framing was that 16 costs 0.92 ms more per deleted Segment — *a per-Tick cost
against a per-click one* — and left the weighing to the edit rate. The weighing now has both sides,
and the query advantage 16 holds is 1.31× against an edit penalty of 2×. **The recommendation is 8**,
and R3's *current standing favours 16* is withdrawn.

> **The recommendation stands; this sentence's arithmetic does not.** R7's re-capture makes the query
> advantage **1.61×**, not 1.31×, which against a 1.9× edit penalty is a coin toss rather than a
> decision. **What carries 8 now is S2 R5.6** — the Parking Shed prefers it by **2.9×**, on every
> gesture and every size. `plans/0010` warned that a ladder chosen on routes alone would be chosen on
> the cheaper consumer; corrected for pinning, routes alone would have chosen **wrongly** rather than
> merely narrowly.

**This is not yet the whole trade.** R5.4 and R5.5 are unrun, and a Parking Shed is inherently local
in a way that may favour a coarser cluster. The verdict is recorded as *R5.2 decides it against 16 on
the edit axis*, not as *cluster size is closed*.

### R5.3 — the Epoch ladder, and the tripwire it fires

`CONTEXT.md` → Epoch already carries the distinction this task needed — *"never a global flush" is a
claim about **when you pay**, never about **what survives**"* — and already states that **S2 settles
the granularity by measurement**. This is that measurement, for the route consumer.

The workload is 256 Ticks × 16 Trip starts, drawn with repetition from a pool of 512 distinct
origin-destination pairs into a fixed-capacity cache of 1,024 entries at 8 Chunks per cluster, with
16-Segment drags applied at a swept rate and **never reverted**. Uniform draw shown; the other two
O-D rungs agree on every ranking.

| Epoch | Edit every | Deleted | Hit | Stale | Revalidation words | Mean Tick | Worst Tick |
|---|---:|---:|---:|---:|---:|---:|---:|
| global | never | 0 | 71.63% | 0.00% | 0.71 | 1,069.84 µs | 4,599.86 µs |
| per-cluster | never | 0 | 71.63% | 0.00% | 7.56 | 989.04 µs | 3,515.95 µs |
| per-Segment | never | 0 | 71.63% | 0.00% | 41.97 | 1,020.92 µs | 5,558.00 µs |
| global | 64 Ticks | 64 | 49.36% | 22.26% | 0.71 | 1,894.18 µs | 6,402.68 µs |
| per-cluster | 64 Ticks | 64 | 70.62% | 1.00% | 7.56 | 1,097.14 µs | 3,811.33 µs |
| per-Segment | 64 Ticks | 64 | 71.60% | 0.02% | 41.97 | 1,068.26 µs | 3,909.86 µs |
| global | 16 Ticks | 256 | 20.87% | 50.75% | 0.71 | 3,050.73 µs | 8,329.87 µs |
| per-cluster | 16 Ticks | 256 | 66.25% | 5.37% | 7.60 | 1,344.45 µs | 5,841.88 µs |
| per-Segment | 16 Ticks | 256 | 70.19% | 1.44% | 42.79 | 1,283.66 µs | 5,243.22 µs |
| **global** | **4 Ticks** | 1,021 | **6.59%** | 65.03% | 0.71 | 3,713.37 µs | 7,187.22 µs |
| **per-cluster** | **4 Ticks** | 1,021 | 57.49% | 14.13% | 7.64 | 1,984.14 µs | 5,885.38 µs |
| **per-Segment** | **4 Ticks** | 1,021 | **68.99%** | 2.63% | 42.80 | 1,409.97 µs | 4,513.86 µs |

**Ladder monotonicity: 12 triples checked, 0 violations.** Each rung is strictly less conservative
than the one above — *anything moved* implies *a crossed cluster moved* implies *an own Segment
moved* — so hit rate must be non-decreasing down the ladder everywhere. It is printed on the run
where it reads zero because that is the only run on which it is worth anything.

**The tripwire fires on the Epoch as the corpus writes it.** `plans/0010`: *either router needs a
global flush on a Road Graph edit → that candidate is out on a design commitment, not on a number.*
A single counter **is** a global flush; the ceiling with no edits at all is 71.63%, and under a
continuous storm **per-Segment retains 96% of that ceiling and global retains 9%.** The design
commitment and the number now agree.

**The trade this section was written to find does not exist, and that is the finding.** `plans/0010`
frames the ladder as *hit rate against revalidation cost*, on the reasonable expectation that
O(path length) is what per-Segment charges for its precision. It charges **42 words a lookup against
global's 0.71 — and its mean Tick is lower at every edit rate measured**, because the searches the
precision avoids cost orders of magnitude more than the words it reads. There is no rung on this
ladder that trades accuracy for speed. **Per-Segment is cheaper *and* more precise**, storage is
129 KiB of version words against a 172.3 MiB world, and the plan's own framing was the thing that
needed measuring.

**Per-Segment is exact under deletion and unsound under addition, and the asymmetry is structural.**
Removing road elsewhere can make a route invalid or slower but never *better*, so a route whose own
Segments are untouched is still the route the search would return. **A newly drawn road can shorten a
cached route without touching a single Segment that route uses**, and no version on that route would
ever move. R5 measures deletion only — inherited from R3, where adding a road creates a portal the
abstract graph's build reserved no slot for — so *exact* must never be quoted about this rung without
the verb attached. **A production per-Segment Epoch needs a second mechanism for addition**, and
nothing in the corpus has one.

### R5.4 — the addition, and the fact that only one rung is sound

R5.3 recommends per-Segment. **This section is the argument against it, and it is a measurement
rather than an argument because the corpus requires that** (`adr/0043`).

**The asymmetry is a property of shortest paths, not of this implementation.** Deletion is
monotone-**worsening**: remove an arc that is not on route `R` and `R`'s cost is unchanged while
every alternative can only rise, so `R` stays optimal — a rung watching only `R`'s own Segments
misses nothing, which is why per-Segment reads as exact in R5.3. Addition is
monotone-**improving**, and that inverts it: a new arc can create a cheaper path bearing no relation
to `R` at all. **A route computed before a road existed cannot contain that road**, so no version the
per-Segment rung watches can ever move. **Per-cluster fails for the same reason** — a new fast link
in a cluster the route never enters still beats it. **Only the global rung is sound under addition,
and R5.3 measured that rung as unusable.**

**Addition turns out to be measurable, and the trick is the contribution.** R3 deferred it because
drawing a road across a boundary creates a portal the abstract graph's build reserved no slot for.
So: **build the abstract graph on the full graph — reserving every portal — then delete a set of
Segments, cache routes, and restore them.** Restoration *is* addition and needs no new portal.
`RebuildCluster` re-derives its crossing arcs from the cost array and re-applies the reduction, so a
restored arc genuinely comes back rather than being re-costed into a frozen edge set.

| Added | Got | Epoch | Resident | Improvable | Declared valid | **Wrongly valid** | Mean detour | Worst |
|---|---:|---|---:|---:|---:|---:|---:|---:|
| street drag | 16 | global | 412 | 0.00% | 0.00% | **0.00%** | — | — |
| street drag | 16 | per-cluster | 412 | 0.00% | 83.73% | **0.00%** | — | — |
| street drag | 16 | per-Segment | 412 | 0.00% | 100.00% | **0.00%** | — | — |
| street drag | 126 | global | 412 | 0.00% | 0.00% | **0.00%** | — | — |
| street drag | 126 | per-cluster | 412 | 0.00% | 75.48% | **0.00%** | — | — |
| street drag | 126 | per-Segment | 412 | 0.00% | 100.00% | **0.00%** | — | — |
| **arterial** | **4** | global | 412 | 9.22% | 0.00% | **0.00%** | — | — |
| **arterial** | **4** | per-cluster | 412 | 9.22% | 88.10% | **3.64%** | 6.04% | 25.48% |
| **arterial** | **4** | **per-Segment** | 412 | 9.22% | 100.00% | **9.22%** | **16.71%** | **62.65%** |

512 uniform O-D pairs cached on the damaged graph, then priced against a fresh search once the
Segments are restored. **The *Improvable* column is the instrument check** — ground truth,
rung-independent, and identical across all three rungs of a gesture as it must be. Had it read zero
everywhere, every *wrongly valid* column would read zero too and prove nothing, which is the shape
R3's 0.00% detour wore until sampling transitions drove it to 80.49%.

**The prediction holds exactly.** Global is sound (0.00% wrongly valid, at the price of declaring
nothing valid). Per-cluster catches **60%** of the improvable entries. **Per-Segment catches none of
them — 9.22% improvable, 9.22% wrongly valid**, because it declares 100.00% of the cache valid and
structurally cannot do otherwise.

**Restoring ordinary Street improves nothing, and that is this graph rather than a fact about
cities.** One Street per Cell boundary at a uniform speed means very many *equal-cost* shortest
paths; deleting a line leaves an equal-cost alternative one block over, so the cached cost never
moved and restoring gives the search nothing to find. **The zero is real and does not generalise** —
a real network has heterogeneous speeds and far fewer ties, and `CONTEXT.md` → Segment still carries
R0's disclaimer that nobody has checked this graph against a real city. **Read the Arterial row.**

**The Arterial gesture collects 4 Segments and the smallness strengthens the conclusion.** That is
~512 m of new fast road, the smallest addition worth drawing. If half a kilometre of Arterial leaves
a per-Segment Epoch serving stale routes on 9.22% of entries at a mean **16.71%** detour and a worst
of **62.65%**, a larger addition cannot do better. **The figure is a floor.** For scale, R2's
next-hop table was treated as a serious correctness finding at 18.52%, and `05 §4` holds that a
different route is a different city.

**Unlike every other error in this spike, it does not heal.** A stale entry under per-Segment has no
mechanism that will ever notice it: the road it should be using is one the route does not contain, so
no version it watches will move again. The only thing that removes it is **eviction** — and
`adr/0012` keys the cache by origin-destination pair **rather than by agent**, so the entry is not one
driver's habit but every driver's route, and **a hot pair is the least likely to be evicted precisely
because it is hot**. The error is permanent and concentrated on the busiest pairs in the city.

#### The five ways out, none of them free

| | Option | Sound? | Cost |
|---|---|---|---|
| **A** | Global bump on addition, per-Segment on deletion | yes | every road *drawn* flushes the cache — R5.3's 9% rung for half the core verb |
| **B** | Weaken the contract to *feasibility, not optimality* | yes, for feasibility | free, but the error lands exactly as described above |
| **C** | Rolling refresh — invalidate a slice every N Ticks | bounded, not exact | amortised; **R4.7 already measured a rolling refresh** |
| **D** | Geometric bound — invalidate routes whose detour ellipse contains the new arc | yes, given admissible bounds | a spatial index; R1's matrix supplies bounds at 11.32% error, so it needs slack |
| **E** | R1's matrix as an O(1) detector — cached cost against the matrix entry | large improvements only | nearly free, reuses an existing structure |

**Option B deserves naming rather than dismissing**, because it has a real design fit: `BOUNDED
KNOWLEDGE` says drivers are not omniscient, and not knowing about a new road is plausible ignorance.
**The question the corpus has to answer is whether that ignorance is modelled or accidental** — a
stated learning rate is a design decision, and a cache artefact concentrated on the busiest pairs is
not. **Option E is also the argument R1 declined to have**: R1 refused the matrix an Epoch because
*"a version counter would imply a relationship to the route cache that nobody has argued"*, and E is
that relationship arriving from the other side.

### R5.5.1 — the edit response, which is what the player is waiting through

**The rungs, and what each has to do to be correct again after a gesture.** A 256-Segment drag
collects 173 Segments and touches 15 clusters at 8 Chunks.

| Rung | Coalesced | Worst | Naive | Worst | Naive ÷ coalesced |
|---|---:|---:|---:|---:|---:|
| `cache` (HPA\*, per-Segment Epoch) | **2.75 ms** | 4.10 ms | — | — | — |
| `cache+ttl` | 3.54 ms | 6.78 ms | — | — | — |
| `nexthop` (`RepairSubtree`) | **20.38 ms** | **45.64 ms** | 29.42 ms | 85.21 ms | **1.44×** |
| `shared` (rebuild) | **179.32 ms** | 188.38 ms | — | — | — |
| `flat` | **none** | — | — | — | — |

**Tripwire 4 fires negative, and that is a result worth having.** `plans/0010` asked whether R5.2's
**23.26×** per-edit repair penalty is a corpus-wide API shape rule or a property of one structure.
Looping `RepairSubtree` per Segment rather than handing it the gesture costs **0.91× / 1.08× / 1.22×
/ 1.51× / 1.44×** across drags of 1, 4, 16, 64 and 256. **It is not a general law.** A cluster's edge
set must be *decided* once however many of its arcs moved, which is what makes re-deciding it dozens
of times catastrophic; a shortest-path subtree is *repaired* from the boundary inward, and feeding it
one arc at a time repeats a traversal rather than a decision. **The 0.91× at a gesture of one is the
identity check** — there the two spellings are the same computation, so anything far from 1.00×
would have meant the harness was comparing two different things.

**`shared` is flat in gesture size at ~180 ms, and that is the shape of its retirement.** A rebuild
does not care how much was deleted. Against a 15.6 ms Tick that is **eleven Tick budgets from one
drag, and eleven from a single deleted Segment equally.** R2 declined to choose between shared and
next-hop and said the axis was invalidation; this is that axis, and it separates them by an order of
magnitude before correctness is considered at all.

**`flat` costs nothing because it is never stale**, which is the structural advantage the denominator
has always had and which R3 named without pricing. It pays instead in R5.5.2.

### R5.5.2 — the storm, and which kind of wrong each rung is

The flat search is the denominator and it is measured on both sides of the sweep: **911.68 µs
measured first, 402.74 µs measured last, 2.26× apart.** R3's finding survives the pinning repair —
see *The capture* above — so the twice-measured denominator stays mandatory rather than becoming a
formality.

Uniform draw, no edits and the highest edit rate. 256 Ticks, 16 Trip starts per Tick.

| Rung | Mean Tick | Worst Tick | Hit | Mean detour | p90 | Worst |
|---|---:|---:|---:|---:|---:|---:|
| `cache`, never | 969 µs | 3,367 µs | 71.63% | 0.00% | 0.00% | 1.12% |
| `cache`, 4 Ticks | 1,283 µs | 5,394 µs | 68.99% | 0.00% | 0.00% | 1.12% |
| `nexthop`, never | **1.29 µs** | 15 µs | — | **16.58%** | 26.97% | 913.61% |
| `nexthop`, 4 Ticks | 584 µs | 41,383 µs | — | 16.69% | 28.80% | 913.61% |
| `shared`, never | **1.43 µs** | 13 µs | — | **31.21%** | 56.46% | 953.61% |
| `shared`, 4 Ticks | **45,297 µs** | 187,119 µs | — | 31.27% | 56.46% | 953.61% |
| `flat`, never | **6,821 µs** | **12,435 µs** | — | 0.00% | 0.00% | 0.00% |

**The two kinds of wrong are visible in one table and they do not trade against each other.** A
maintained table answers a Trip in **about one microsecond** — three orders of magnitude under the
cache and four under a flat search — and is wrong by a fixed structural margin that does not move
with edit rate at all (16.58% → 16.69% across a storm that deletes 1,021 Segments). The cache is
near-exact and costs a millisecond a Tick. **Neither is the other's rung on a ladder**; they are
different answers to *what may a route be wrong about*.

**The detour is a property of the draw, and R4.8 arrives a second time by a different door.** On the
local-trip rung the same next-hop table reads **149.73%** and the shared route **211.94%**; on
monocentric they read 16.58% and 33.21%. R2 measured 18.52% and 36.01% on the uniform draw with a
different harness and a different composition, and R5.5 reads 16.58% and 31.21% — **an independent
reproduction to within two points**, which is the strongest cross-task agreement in this spike.

**`flat` at 16 Trip starts already reaches a worst Tick of 12.44 ms against a 15.6 ms budget**, with
a mean of 6.82 ms. R3 published *routing fits while fewer than 85 Trips start per Tick* from a mean
per-route cost. At **16** — a fifth of that — the control is already at 80% of the budget on its
worst Tick. **A mean per-route cost does not bound a Tick**, and this is the third instance after
S4's K6 and R5.3.

#### The TTL, priced as a rate

| Rotation | **Forced refreshes / Tick** | Uniform hit | Mean Tick |
|---|---:|---:|---:|
| none | — | 71.63% | 969 µs |
| every 1024 Ticks | **0.34** | 70.06% | 1,015 µs |
| every 256 Ticks | **1.44** | 64.74% | 1,208 µs |
| every 64 Ticks | **5.26** | 46.63% | 1,787 µs |

**The rate is the finding and the period is not.** A period of 256 Ticks over this harness's
1,024-entry cache is 4 slots swept a Tick; the same period over a real hot set is a different bill
entirely, and it is the *rate* that has to fit the routing budget. Stated the R3 way: **a rotation
costs about seven points of hit rate at 1.44 forced refreshes per Tick and about twenty-five at
5.26**, and what period that buys is a function of a cache size nobody has measured.

**The `cache+ttl 1024` row prices cost and says nothing about the bound**, because it completes 25%
of one sweep inside a 256-Tick run. R5.5.4 is where the bound is measured, on a window sized to a
full rotation.

### R5.5.3 — what the cache *holds*, as against what it *serves*

**R5.5.2's detour column cannot answer the question R5.5 exists to ask, and this section is why.**
Under a per-Segment Epoch a stale entry is *detected* and recomputed, so what is *served* is never
stale — the column prices freshly-computed HPA\* routes and is invariant across edit rate by
construction. It is a result rather than a silence, but it is the wrong instrument for a hole in
what the cache *retains*. So this section walks the entire pool at the end of the storm and prices
every resident entry the Epoch declares good, whether or not any Trip asked for it.

**48 rows: `wrongly valid` reads 0.00% on all of them, and `improvable` reads 0.00% on all of them.**
That is R5.4's monotone-worsening argument measured rather than argued — under deletion, a rung
watching a route's own Segments misses nothing.

**The zero is worth less than the identity it comes with.** On **48 of 48 rows**,

> resident − declared valid **==** entries holding a deleted Segment

The set the Epoch refuses is *exactly* the set containing a bulldozed road, neither more nor fewer.
A zero column proves nothing on its own — R3.5's 0.00% detour wore that shape until R3.6 drove the
same instrument to 80.49% — whereas an equality between two independently counted quantities fails
loudly if either side is miscounted.

**This confirms the asymmetry; it does not endorse the rung.** A storm that only deletes cannot say
anything about addition, which is the half R5.4 measured and where per-Segment is the *worst* rung
available. R5.5.4 is that half.

### R5.5.4 — does the rotation actually clear the addition hole

R5.4's technique exactly: build the abstract graph on the **full** graph so every portal slot is
reserved, delete the Arterial gesture (**4 Segments, ~512 m**, the smallest addition worth drawing),
cache the pool against the damaged graph, then restore — restoration *is* addition. What is new is a
**1,024-Tick window afterwards with ordinary Trip traffic and a rotation running**, sampled at eight
points, sized to one full sweep of the longest period so every rate is entitled to a statement about
the bound.

**Traffic is load-bearing and the design says so.** A rotation alone only empties slots; what makes
an entry *correct* is the next Trip missing on it and searching the graph as it now is. A window
with a rotation and no traffic would measure a cache being deleted and report it as a cache being
taught.

| Rotation | Refreshes/Tick | Tick 0 | 16 | 64 | 128 | 256 | 512 | 768 | 1024 | Resident retained |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| **none** | 0.00 | 38 | 29 | **23** | 23 | 23 | 23 | 23 | **23** | 100.00% |
| every 64 | 5.79 | 38 | 24 | 0 | 0 | 0 | 0 | 0 | 0 | 59.70% |
| every 256 | 1.60 | 38 | 25 | 18 | 10 | **0** | 0 | 0 | 0 | 87.13% |
| **every 1024** | **0.40** | 38 | 27 | 19 | 18 | 18 | 10 | 3 | **0** | **97.08%** |

*Wrongly-valid entries, as counts. Resident population 412 at Tick 0.*

**The control is the finding, and it passes the instrument check decisively.** With no rotation the
count falls 38 → 29 → **23 by Tick 64 and then does not move for 960 Ticks**, with the resident
population constant at 412. **60.52% of the error survives every Tick in the window.** R5.4's *the
error does not heal* is now measured rather than argued, and **the flatness is what makes it
permanent rather than merely slow** — a curve still descending at the right-hand edge would have
meant the window was too short to conclude anything. The early 38 → 23 decay is collision eviction,
which is precisely the only removal mechanism R5.4 named.

**A share that falls because the population shrank is a hole being hidden, not closed, and the count
column is what separates them.** The **64**-Tick rotation drives the count to zero by **Tick 64** —
and sheds **40% of the resident cache** doing it. That row cannot carry the conclusion. The row that
can is **`every 1024`: 38 → 0 while resident falls only 412 → 400, a 97.08% retention, at 0.40
forced refreshes per Tick.** The denominator held, so those entries were genuinely taught the new
road rather than deleted.

**The surviving error is worse than the cleared error, and that sharpens `adr/0012`.** In the control
the mean detour *rises* from 16.35% to 19.31% while the count falls 38 → 23: collision eviction
removes the mild errors and leaves the severe ones. `adr/0012` keys the cache by origin-destination
pair **rather than by agent**, so a hot pair is the least likely to be evicted precisely because it
is hot. **What persists is every driver's route, on the busiest pairs, at the largest detours** — so
a residual quoted as a count understates it, and this is the second time R5 has reached that
conclusion from a different direction.

**Tick 0 re-measures R5.4 and agrees, which bounds a units worry R5.5.3 raised.** R5.4 published
**9.22% improvable / 16.71% mean / 62.65% worst** computed on *arc sums*; Tick 0 here reads
**9.22% / 16.35% / 62.41%** on *whole journey cost*. Improvable is identical to the digit and the
detours move by about a third of a percentage point. **R5.4's figures carry the arc-sum residual at
the level of rounding rather than as a factor** — worth stating, because R5.5.3's own draft had that
same residual manufacture a phantom wrongly-valid entry out of nothing, and the two outcomes would
otherwise read as contradictory.

### What R5 found that it was not looking for

- **`HpaSearch` cannot see a Segment deleted under a Trip's own feet — pre-existing, and it reaches
  R5.3 and R5.4.** The forward seed and goal remainders call
  `SegmentEntry.CostToEndpoint(graph, null, …)`, and a **null** cost array reads `graph.ArcCarTicks`
  — the pristine array — while the storm deletes into a shadow clone. The confined searches and
  abstract edges read the live costs; only the two Access Point remainders do not. **So when the
  player bulldozes the Segment a Trip starts on, the hierarchy seeds both of its endpoints anyway and
  returns a route down a road that is not there.** Over R5.5's sweep `flat` found **416** unroutable
  where the four cache rungs found **16** between them, on the same graph at the same Tick. It is
  **common-mode across all three Epoch rungs**, so the ladder comparison cancels it and no R5.3 or
  R5.4 conclusion moves — but ***Unroutable* on any hierarchical row is zero by construction and is
  evidence of nothing**, and **R6 must fix it before it caches anything.** Recorded rather than
  repaired, because repairing it moves R5.3's published hit rates and that is a re-capture.
- **The miss column is the eviction policy's bill, not staleness.** It sits at 28–31% and does not
  move with edit rate at all — the tell that it is collisions rather than invalidation. **A
  direct-mapped cache at 2× over-provisioning loses about three lookups in ten before a single road
  is touched.** That gives `adr/0017`'s fixed-capacity least-used pattern a number for the first
  time, and the decision belongs to **R6**, which owns eviction. Reported here because the figure
  fell out and a reader would otherwise read it as the Epoch's doing.
- **The worst Tick is far worse than R3's framing implies.** R3 published *routing fits while fewer
  than 85 Trips start per Tick*, from a mean. At **16** Trip starts the worst Tick already reaches
  **10.37 ms** on monocentric/global against a 15.6 ms budget — 66% of it, from an arrival rate 5×
  below the one R3's row permits. A mean per-route cost multiplied by an
  arrival rate does not bound a Tick, and S4's K6 said so first: a run whose worst iteration was
  100.2 ms read 2.462 ms at p99.9.
- **R3's denominator finding arrives a fourth time, in a form it had not taken.** R5's first draft
  measured the rebuild at 8 Chunks and divided the **16**-Chunk repair figures by it — not measured
  once instead of twice, but **measured on the wrong rung**. A rebuild at 16 Chunks is a different
  amount of work. Caught before publication; the table now carries a per-rung denominator.
- **The gesture generator needed a control before it needed a result.** A contiguous drag touches few
  clusters *by construction*, so a ladder rung keyed on clusters is flattered by the generator rather
  than by the design. The scattered row exists so that the drag's locality is a measurement rather
  than a definition — this spike's own rule about an instrument that cannot move, applied before the
  numbers rather than after.
- **A Segment→arc index had to exist first.** R3 and R4 apply an edit by scanning all 66,036 arcs and
  comparing, which is invisible when one edit sits outside the clock. R5 deletes hundreds inside the
  timed span, where the same spelling would have put ~17 million comparisons in the measurement and
  published them as the cost of the edit.

### Decisions R5 has produced, and the ones it has not

- **Cluster size: 8 Chunks a side**, against R3's bias on 16, on the edit axis. `plans/0010`'s
  *current standing favours 16* is withdrawn. **Conditional on R5.5**, which the plan says is the
  section most likely to rank the ladder differently.
- **The Epoch must carry a location.** The single counter is out on the design commitment it fails
  and on a hit rate of 9% of ceiling.
- **But no rung on this ladder is both affordable and correct across the whole core verb**, and R5.4
  measured that rather than arguing it. Per-Segment is the recommendation **for deletion**, where it
  is exact and cheapest; **under addition it is the worst rung available**, declaring 100% of the
  cache valid and serving stale routes on 9.22% of entries at a mean 16.71% detour that never heals.
  **The recommendation is therefore conditional and a second mechanism is required**, chosen from
  R5.4's five options. Naming a rung without naming that mechanism would ship the hole.
- **`CONTEXT.md` → Epoch's *"spike S2 settles it by measurement"* is discharged for the route
  consumer and not for the Parking Shed**, which is the second Epoch consumer, scales with Buildings
  rather than with routes, and is inherently a *neighbourhood* rather than a *path* — so per-Segment
  has no obvious meaning for it. **The vocabulary entry must not be updated until R5.6 runs** — the renumbering that moved the path source to R5.5 moved the Parking Shed to R5.6, and it is the Parking Shed this waits on.
- **The shared District route is retired, on a number.** R2 left it live and said the axis was
  invalidation. It is **~180 ms per gesture flat in gesture size** — a rebuild does not care what was
  deleted — for **45.3 ms mean and 187.1 ms worst** per Tick under a storm, against a next-hop table
  that is exact-again in 20.38 ms and answers a Trip in a microsecond. It is also worse on error at
  every O-D rung. That is a retirement measured rather than argued, which is what R2 asked for.
- **The path source is not one choice, and R5.5 is what establishes that.** A maintained table and a
  route cache are wrong in **different currencies**: the table's error is structural, fixed and
  visible (16.58% uniform, **149.73%** local); the cache's is temporal, near-zero while it lasts, and
  — under addition and without a rotation — **permanent**. Neither is a rung on the other's ladder.
  **Which the city should have is `05 §4`'s question rather than a benchmark's**, and it is session
  **M**'s to close with these numbers under it.
- **A TTL rotation closes the addition hole, and it is affordable.** **0.40 forced refreshes per Tick
  takes the wrongly-valid count from 38 to 0 within one rotation while retaining 97.08% of the
  resident cache**, against a control that plateaus at 23 and never moves again. This is R5.4's
  option **C** measured, and it is what makes option **B** a design position rather than a defect:
  `BOUNDED KNOWLEDGE` permits drivers not to know about a new road **if the ignorance is modelled
  with a stated learning rate**, and a rotation period is exactly that — a number a designer sets,
  priced here as a rate so it survives a cache size nobody has measured.
- **Not settled: R5.6, the Parking Shed**, the second Epoch consumer. `CONTEXT.md` → Epoch must not
  be updated until it runs.
- **Discharged: the canonical pinned `performance` capture.** Every figure in this section is quoted
  from it. What it cost to obtain is written up above, because the first attempt at it was wrong in a
  way that changed a verdict.
- **Owed, and it is new: every earlier `performance` capture in S2 carries the one-processor
  artefact.** R0, R1, R3 and R4 were all taken at `taskset -c 2`. Counts and in-process ratios are
  untouched; **first-timed absolutes are not**, and R3's denominator finding — 1,401,307 ns first
  against 477,609 ns last — is very likely this rather than a cold clock. Re-capture is cheap now
  that the harness pins correctly. **R7 owes it**, and it should be done before the report quotes an
  absolute from any of them.
- **Owed as a lesson rather than a task: R5's first write-up quoted figures that exist in no retained
  capture.** `3.79 ms`, `7.61 ms`, `161.79 ms`, `219.50 ms`, `21.25×` and a 13.26 ms worst Tick
  appear in no file under `spikes/S2.Routing/results/`, because the harness wrote every run to one
  filename keyed on the machine configuration alone — so a `--storm` run displaced a whole-run
  capture and a later one displaced it in turn. **A published absolute with no artefact behind it is
  not a measurement**, and the corpus was carrying six of them. The harness now names every capture
  by section, CPU set and capture time, and never overwrites.

---

## S2 R8 — the congestion loop, and the finding that outranks it

> `plans/0010-s2-routing.md` R8. Raw capture in
> `spikes/S2.Routing/results/s2-r8-…-performance-turbo-cpu2+8-20260807T204634Z.md`.
> **All seven sections, R8.0 through R8.6.** The structure under test is
> [`adr/0046`](adr/0046-a-driver-routes-on-habit-sight-and-temperament-never-on-current-cost.md),
> written the same session and settling **no parameter** — R8 produces the five numbers it names.
> **Two sections were not in the plan**: R8.0, *the load this network carries*, exists because the
> first build inherited R2's fleet size and measured a gridlocked network; and R8.4's second half,
> *is the herd killed by the threshold or by the variation*, exists because the first answer was a
> siting artefact.

**Every figure S2 published before this ran on a frozen cost basis.** R1's matrix, R2's ladder, R3's
hierarchy, R4's protocols and R5's storm all route over an arc-cost array computed once and never
moved. The storm invalidates a route because a road was **bulldozed**; nothing in this spike had ever
invalidated one because a road got **busy** — and under `adr/0041` the volume column moves every
Tick. R8 closes the loop on both sides: fleet volume → live BPR cost → *both* the Sight decision at
each crossing *and* the traversal time the Traveller is charged → volume.

### The finding, before anything else — the network runs out of routes, not road

**At 12.98% of this network's holding capacity, 87.25% of all traffic is on the busiest one per cent
of its road and 90.87% of it is carrying nothing.** The median volume index reads 0.00 and so does the
ninetieth percentile.

**Capacity is not the culprit, and the derivation says so cleanly.** A Street is 3,600 veh/h
whole-Segment — 1,800 per direction — which at 50 km/h free-flow puts 9.21 vehicles on a 128 m Segment
at `v/c = 1`, one every ~28 m, a **two-second headway**. That is the textbook saturation headway for
an urban lane. The network holds **308,016 vehicles** at `v/c = 1` everywhere and turns over at
40,000, which is **71.42%** of S4's derived 56,000-vehicle Day average and 23.52% of the top of the
peaking band.

**So it is neither of the two answers the question was framed around.** The network does not run out
of road; it runs out of **routes**. `Habit` is a single shortest-path tree on free-flow costs, one per
District, so every Traveller bound for a District follows the same tree into the same representative.
**There is exactly one route per (node, District) pair in the entire model**, and no amount of empty
parallel carriageway can be reached from it.

Three consequences, and each lands somewhere different.

**It is decision 11 arriving from a third side, and it moves the axis.** R2 measured the representative
**funnel** at 412% `v/c`. R8 went looking for it inside a closed loop and **could not make it bind**:
excluding the one-hop funnel, and then a four-Segment convergence zone, gives readings identical to
the printed digit at every rung of the load sweep — `v/c` p99 of 1.18, 1.18 and 1.29, maxima of 64.34,
64.34 and 64.34. The reason is arithmetic and it is the honest one: **only destinations converge**
under this O-D model, origins are scattered real nodes, and arrivals divide across every non-empty
District. R2's 412% was measured with **both** endpoints pinned, which is a harsher query shape.
**The binding term is not the node where routes converge, it is the tree upstream of it** — and
decision 11 has been argued as a question about *how many access nodes a District exposes*. A District
with a hundred access nodes still has one shortest-path tree per destination. **That is a different
fix from the one the question has been asking for.**

**It is why there may be no good operating load, and the sweep answers that rather than leaving it to
be inferred.** Two terms, given numbers before the sweep ran: *congested* is p99 over occupied indices
reaching free-flow saturation (1.00); *resolvable* is fewer than 10% of top-64 readings past the BPR
clamp. **No rung is both.** The largest resolvable rung is 5,000, where p99-occupied is 0.42 — not
congested in any sense the statistic can see. The smallest congested rung is 20,000, where 78.12% of
top-64 readings are already past the clamp. **Under a District-granular free-flow tree there is no
load at which this network is both congested and resolvable**, and the concentration is what closes
the gap: the traffic is on one per cent of the road, so the busiest arcs pass the clamp long before
the network as a whole has anything worth calling congestion on it.

**It is a fourth defect for session M, and it is not a cost.** M has been choosing between a maintained
next-hop table and a cached route on structural error and temporal error, with R8.6 adding diversion
cost as a third. This is the fourth and it is in the same column as the first three: **a maintained
free-flow table does not merely go stale, it concentrates.** The table's error is not distributed over
the fleet, it is **correlated across all of it**, and the correlation presents as a saturated skeleton
beside an empty network. A route cache does not get this for free either — it depends on what seeded
the routes — but a scheme that gives a Traveller more than one candidate route to begin with is the
only kind that can.

**Two limits on how far this travels**, both stated in the capture. It is a **synthetic grid** whose
Arterials were placed to be severable rather than to carry a city, and it runs **one Traveller per
Trip with no departure-time spread**, so the whole fleet is on the road at once. Both make
concentration worse than a real city's. **Neither is capable of making an empty network look full**:
the zero-volume share is a direct reading and depends on neither.

### R8.1 — the actionable-junction distance, and it needs no traffic

`adr/0046` makes this the one routing parameter whose lower bound is **derivable rather than tuned**,
so it is derived before any behavioural argument runs. For every arrival — a node *and* the arc
arrived by, because whether a node is a choice depends on how you reached it — the distance to the
nearest node with at least two onward car arcs once the way back is discounted.

| Distribution over | Count | At distance 0 | p50 | p90 | max |
|---|---:|---:|---:|---:|---:|
| arrivals, Segments | 64,103 | **98.02%** | 0 | 0 | 5 |
| arrivals, free-flow Ticks | 64,103 | 98.02% | 0.00 | 0.00 | 10.60 |
| nodes, worst arrival, Segments | 16,660 | 96.24% | 0 | 0 | 3 |

**The floor is 1 Segment**, taken at the p90 of the arrival distribution rather than the median —
a horizon set at the median is structurally useless to half the crossings in the city. `adr/0046`'s
claim that *a Sight Horizon of one is actionable* is **not refuted at either quantile.**

**This is the graph's answer and not the driver's**, and it weights a cul-de-sac nobody uses as
heavily as the arterial ramp the whole city crosses. R8.3's *no-alternative* column is the same
finding weighted by where drivers actually are — **2.41% of crossings at N = 1** — and neither may be
published without the other.

### R8.0 — the load, and the criterion that had to be stated before the sweep

**The first build inherited R2's 40,000 Travellers on the grounds that figures should be comparable,
and that was the wrong reason**: R2 was pricing attribution and did not care whether the network was
gridlocked. With live residuals and BPR at `β = 4`, an arc at the clamp costs **39.4×** free-flow, so
its Travellers dwell 39× longer, so its volume rises further. That is positive feedback and it pins at
the clamp from any load high enough to reach it.

Nine rungs at Horizon 0 — no routing response at all, so what is measured is the network and the
physics and nothing else.

| Travellers | v/c p99 | p99 occupied | max | Zero-volume | Mean v/c, top-64 | Past the clamp | Arrivals/Tick | Mean journey |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1,000 | 0.09 | 0.20 | 0.62 | 97.89% | 0.04 | 0.00% | 13.86 | 71.74 |
| 3,500 | 0.12 | 0.42 | 1.25 | 93.83% | 0.15 | 0.00% | 48.07 | 71.72 |
| 5,000 | 0.20 | 0.42 | 25.39 | 92.67% | 1.30 | 6.25% | 59.35 | 83.40 |
| 10,000 | 0.20 | 0.85 | 41.77 | 91.00% | 7.21 | 39.06% | 83.11 | 113.90 |
| 20,000 | 0.42 | 18.00 | 52.51 | 91.12% | 14.01 | 78.12% | 95.96 | 144.28 |
| **40,000** | **1.18** | **25.17** | 64.34 | 90.87% | 18.30 | 81.25% | 119.38 | 162.75 |
| 80,000 | 10.07 | 31.89 | 82.14 | 90.40% | 24.01 | 84.43% | 145.19 | 175.06 |

**The loop converges. It does not run away, at any load, including 80,000** — every rung reaches
steady state on the two-window test. The system is closed, so dwell-time feedback redistributes rather
than diverges, and **throughput self-limits**: 80× the load buys 10.5× the arrivals while mean journey
time goes 71.74 → 175.06 Ticks. That is the dwell-time feedback doing exactly what it should.

**The two criteria disagreed, and the capture reports at both loads rather than at the one that
suits it.** The stated p99 criterion selects **40,000**; the retired clamp-share criterion selects
**5,000**. The stated one governs, as written — and then the capture records its defect where it
happened rather than retuning it: **p99 is taken over every car-carrying index and 90.87% of them are
empty**, so it looks *past* the jam and selects a load at which 81.25% of top-64 readings are past the
clamp, which is the exact condition R8.0 exists to prevent. R8.3's Horizon sweep is therefore repeated
in full at 5,000. **The answer is the same at both loads**, which is the more comfortable of the two
outcomes and the less interesting one: R8.0's disagreement changed what the section reports and not
what it concludes.

### R8.3 — the Sight sweep, and the column that is more persuasive than the tripwire

Temperament spread 0, uniform rung, 40,000 Travellers.

| N | v/c p99 | p99 occupied | Mean v/c, top-64 | **Past the clamp** | Diversions/Tick | No alternative | Refresh ns/Tick | Sight ns/Tick | of 15.6 ms |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0.96 | 23.43 | 22.96 | **92.18%** | 0.00 | — | 1,551,234 | — | — |
| 1 | 1.73 | 16.48 | 15.64 | 83.30% | 1,269.51 | 2.41% | 1,487,143 | 881,328 | 5.64% |
| 2 | 3.14 | 13.01 | 10.34 | 51.95% | 4,887.31 | 1.82% | 1,487,378 | 2,011,637 | 12.89% |
| 8 | 3.35 | 6.93 | 5.82 | **40.56%** | 6,722.03 | 1.69% | 1,521,764 | 4,504,834 | 28.87% |
| 32 | 2.48 | 5.75 | 5.46 | 50.99% | 7,313.80 | 0.94% | 1,522,492 | 12,233,900 | 78.42% |

**The most persuasive number here is not the one the tripwire reads.** The share of readings past the
BPR clamp falls **92.18% → 40.56%**. Past that ceiling the delay multiplier is constant and the router
cannot tell a bad jam from a catastrophic one, so **Sight is pulling the busiest arcs out of the region
the congestion model is structurally blind inside** — more than halving it. That is `adr/0046`'s middle
layer doing exactly what is claimed for it, measured on the one column that cannot be argued about.
**It is not substituted for the wire**: the wire says p99 and p99 is what it is scored on.

**`Refresh` is a finding on its own and at this load it is the dominant one.** Recomputing the live
cost array is `O(arcs)`, costs **more than the entire traveller loop at every Horizon below 8**, and is
**flat in fleet size** — so it does not get better at 1M, it only gets relatively cheaper against work
that grows. **The conclusion is not that the sweep is expensive; it is that a sweep is the wrong
shape.** Under `adr/0041` volume is written by Travellers entering and leaving arcs, so the set of arcs
whose cost moved in a Tick is exactly the set somebody crossed — a few hundred, not 66,036 — and it is
already enumerated by the loop that caused it. A per-Tick VDF sweep recomputes a number that did not
change for roughly ninety-nine arcs in a hundred, which is the same shape of mistake as diffusing a Map
Layer nothing has touched. **Whatever ships must update the arcs the Tick wrote and leave the rest
alone.** A staggered cadence would bound the cost and would also make a driver's Sight depend on which
stagger bucket the arc in front of him fell into — `adr/0044`'s hash-bearing problem arriving in the
routing layer.

**`v/c` does not fall monotonically in `N`, on either the conditioned or the unconditioned ladder**,
so the explanation is not an artefact of the statistic. BPR at `β = 4`, `α = 0.15`, clamped at 4.00
makes one saturated arc **39.4×** its free-flow time — a Street that runs in 0.87 Ticks runs in 34,
against a mean journey of order 80. **So a lookahead of even two arcs can put more live cost in front
of a driver than the free-flow remainder behind it**, the detour is charged at free-flow and looks
cheap, and `N` stops behaving like a monotone knob. That is what a live-versus-lagged comparison does
when the live half is bounded only by the clamp and the lagged half is not bounded at all, and it is a
constraint on the base threshold that **nothing in `adr/0046` anticipates.**

### R8.4 — what a threshold is a threshold *on*

**78.35% of decisions are offered an improvement of exactly zero** — an alternative existed, was
scored, and lost. Not small: **zero**, and the explanation is structural. Habit is a shortest-path tree
on free-flow costs, so at every node the habit arc is **optimal by construction and was optimal before
any Traveller moved**. An alternative is by definition off the tree and pays a free-flow penalty
immediately and in full. Both branch scores carry a free-flow remainder computed from that same tree,
so **an alternative can only win when live congestion on the first `N` arcs exceeds the penalty of
leaving the tree.** Below that the comparison is decided by arithmetic congestion never enters.

Cross-checked against a different column: only **2.41%** of crossings had *no surviving alternative at
all*. *Nowhere to go* and *nowhere better to go* are different things with different consequences, and
both are printed.

**The consequence is the useful part: Sight fires rarely, and that is what makes it affordable.**
243,747 diversions from 1,730,568 crossings — **14.08%**, against a 21.64% ceiling. **R8.6's per-Tick
bill must multiply the cost of one re-search by the diversion rate and not by the crossing rate**; a
reader who assumes every crossing re-plans overstates Sight by more than an order of magnitude.

**And there is a sting in it.** The same structure that makes Sight cheap is the structure R8.0 found
concentrating the fleet: **one free-flow tree per District is both why alternatives rarely win and why
there is congestion for them to win against.** Sight is relieving a jam its own null hypothesis
created, using a score that null hypothesis anchors. That is not an argument against `adr/0046` — it is
why Habit is named a *layer* and not a *baseline* — but the ~21.64% fire rate is a property of
District-granular routing and must not be carried to any scheme that gives a Traveller more than one
candidate route.

#### Is the herd killed by the threshold, or by the variation?

**Temperament read REFUTED twice before it read true, and both refutations were siting artefacts.**
The positive control herds at 7.51 and every swept rung sat two orders below it — **a switch, not a
gradient** — which raises a question the sweep as sited could not answer: *where did it happen?*

| Base threshold | Quantile | Oscillation | Synchrony | Diversions/Tick |
|---:|---|---:|---:|---:|
| 0.000000 *(positive control)* | — | **7.51** | 8.02% | 9,337.50 |
| 0.000976 | p10 | **6.41** | 7.46% | 7,879.46 |
| 0.031250 | p25 | **9.87** | 10.08% | 4,841.93 |
| 0.125000 | p50 | **0.55** | 4.72% | 1,193.21 |
| 0.500000 | p90 | 0.46 | 3.05% | 885.15 |

**A threshold damps** — 9.87 → 0.46, a factor of **21.22×** against a bar of four stated beforehand.
**And the transition is inside the ladder, not at its first step**: the smallest non-zero base still
reads 6.41, the same order as the positive control. The herd survives a small threshold and dies at a
larger one.

**The spread ladder had been sited at the improvement distribution's median — which is past the
transition.** It swept Temperament through a regime where the base threshold had already killed the
herd, and read flat for that reason. Re-sited at the non-zero base with the *highest* measured
oscillation, blend held at 0.50:

| Spread, of base | Oscillation | Synchrony | Effective arcs | Diversions/Tick |
|---:|---:|---:|---:|---:|
| 0.00 | **9.87** | 10.08% | 25.60 | 4,841.93 |
| 0.06 | **0.63** | 5.29% | 49.31 | 3,991.98 |
| 0.12 | 0.65 | 7.96% | 37.89 | 3,687.55 |
| 0.25 | *5.71* | *15.38%* | *17.00* | *6,051.57* |
| 0.50 | **0.76** | 6.61% | 53.46 | 2,651.13 |

**Per-Citizen variation damps where there is something to damp**: 9.87 → 0.76, **92.28%** against a
25.00% bar. `adr/0046`'s third layer is **not refuted**, its mechanical justification survives, and it
does not collapse to a single Ruleset number.

**The 0.25 rung was tested rather than shrugged at**, because three independent columns moved together
on it — synchrony highest in the report, effective arcs lowest, diversions up — which is the signature
of a real herd rather than a noisy amplitude. The rule was stated before reading: a property of the
*spread* must survive a change of *blend*. Re-measured at the two blend weights the ladder did not use,
it returned 0.86 and 0.65. **It survived 0 of 2**, so it is one noisy rung and the net-fall reading is
the honest one. It is recorded rather than removed.

**The wire is scored as written and REFUTED.** `plans/0010` states it on **monotonicity**; the first
three rungs read 9.87, 0.63, 0.65 and that is non-monotone. It is not softened and the net fall is not
offered in its place. **Beside it, an argument that does not depend on how the ladder fell**: the two
blend rows above were measured at *one* spread with nothing else changed and returned 0.86 and 0.65 —
**a 0.20 floor on what this instrument resolves between adjacent rungs**, established by a measurement
taken for another purpose. The step that breaks the wire is **0.02**. *A monotonicity test over values
the instrument cannot separate is not a test*, and that holds whichever way they had fallen.

### R8.5 — `03 §3.4`'s loop closes, and it is the claim `adr/0046` is most exposed on

**The version that ran in the previous build could not answer the question, and the reason was in the
harness.** It retargeted 40% of the fleet at one District and watched the network recover — and it
recovered, five times of five, at Horizon 0 as reliably as under Sight. This fleet respawns a Traveller
the instant it arrives, so a one-off retarget is **a pulse with a half-life of one journey**, and any
system at all recovers from a pulse it stops receiving. **Control and Sight settling identically was
not a null result; it was no result.**

**Sustained now**: 40% of every respawn for the whole 640-Tick window, which is R1's monocentric
morning peak as it actually behaves — people keep leaving for the centre for hours. **And that changes
the shape of the question**: not *does it recover* but **does it reach a bounded steady state, and at
what level.** The rule was stated before the run and not touched after, on p99 over *occupied* indices,
across five destination Districts.

| Rung | Bounded | Settling level, min | median | max | Median over pre-surge |
|---|---:|---:|---:|---:|---:|
| control, N=0 | **5 / 5** | 24.70 | **27.62** | 39.48 | **+16.83%** |
| Sight, N=1 | **5 / 5** | 15.01 | **15.84** | 17.45 | **−26.50%** |

**Both bound, and Sight settles 42.62% below the control against a 5.00% bar.** The control settles
*above* its own pre-surge level and Sight settles *below* it. **`03 §3.4`'s self-correction closes with
only the local layers reading the VDF** — tested under a demand asymmetry that does not go away,
against a control carrying identical physics and no ability to respond. **So static Habit survives as
the null hypothesis**, and with it the whole maintenance question stays shut: no refresh cadence, no
hash-bearing number, and R4.6's incremental-versus-rebuild break-even does not select an algorithm.

**The caveat runs in the safe direction, which is unusual enough to state.** 4 of 5 control runs and 0
of 5 Sight runs reached their highest sample inside the last quarter of the window, so the control may
still be climbing and its settling level is a **lower bound** — which *understates* the control's
plateau and therefore Sight's advantage. A longer window widens the gap rather than closing it.

**One limit, stated because it bounds the conclusion either way.** The surge is sustained on the
**destination only** — everybody heading for one District rather than everybody leaving one place for
it. A real morning peak is asymmetric at both ends, and R2's 412% was the both-ends shape. This is the
milder one and its result is a lower bound on how hard a real peak would press.

### R8.6 — what a diversion costs, by path source

At N = 1, uniform, 40,000 Travellers, **1,269.51 diversions per Tick** measured in R8.3. 512 of 512
re-searches found a route. It does **not** go through `HpaSearch`, whose pristine-seeding defect R5.5
found and R6 owns.

| Path source | Per diversion | × diversions/Tick | of 15.6 ms |
|---|---:|---:|---:|
| next-hop table read | **391 ns** | 496,380 ns | **3.18%** |
| flat A\* over live costs | **485,507 ns** | 616.35 ms | **3,951%** |

**This is the third axis session M is owed** — structural error, temporal error, and now diversion
cost. Sight makes a mid-journey re-decision **routine rather than exceptional**, so a path source's
diversion cost stops being a footnote and becomes a per-Tick bill that scales with how congested the
city is. R7 states the verdict; R8 decides nothing on its own.

### The tripwires, and each one's verdict

| # | Tripwire | Verdict | Reading |
|---:|---|:-:|---|
| 1 | Sight lowers p99 `v/c` against the control | **FIRED** | 1.73 against 0.96, at N = 1 |
| 1a | *Advisory, stated after the numbers and scoring nothing*: the same wire over **occupied** indices | would pass | 16.48 against 23.43 |
| 2 | The instrument is connected | **PASS** | mean `v/c` 22.96 → 15.64, 31.87%; control diversions 0 |
| 3 | Conservation, every Tick, every rung | **PASS** | 0 volume, 0 unplaced, 0 bounded, over **42 rungs** |
| 4 | Steady state established, never assumed | **FIRED** | 2 of 42 rungs outside 25%, each marked in its own table |
| 5 | Sight's cost measured, never derived | **PASS** | `Move(N) − Move(0)`; `Refresh` timed separately |
| 6 | Every table names its O-D rung and its load | **PASS** | |
| — | Temperament damps — amplitude falls **monotonically** in spread | **REFUTED** | scored as written; the instrument's resolution floor is 0.20 and the breaking step is 0.02 |
| — | *A different statistic, beside it and never in its place*: the **net fall** | **damps** | 9.87 → 0.76, 92.28% against a 25.00% bar |
| — | Sight is a mechanism — p99 `v/c` falls with `N` | **non-monotone** | on both ladders; see R8.3 |
| — | **`03 §3.4` closes under sustained demand with only the local layers** | **not refuted** | both bound; Sight 42.62% lower |

**Tripwire 1 fired on a redistribution, and that is the finding rather than an embarrassment.** p99
went *up* while the mean over the busiest 64 went *down*, and so did the same quantile over indices
carrying anything, and so did the clamp share, and so did journey time. All are true and they describe
one behaviour: **Sight takes load off an extreme tail and puts it onto arcs that were carrying
nothing.** A percentile of a population that is nine parts empty **must** rise the moment previously
empty arcs start carrying traffic. **A router that spreads a jam over more road cannot avoid raising an
unconditioned middling quantile — that is what spreading is.**

**Beside it sits an argument about the instrument that does not depend on the outcome**, which is what
separates this from reasoning around a wire that fired: across the seven rungs of the cross-load
Horizon sweep the unconditioned p99 takes **one distinct value**, against five for the conditioned one.
That is checkable by inspecting the ladder without knowing what any rung did. **It does not unfire the
wire; it bounds what the wire is evidence of.** A fourth version belongs in R8's successor and should
read the **share past the clamp** — the only column that is a statement about whether the model can
still see what it is simulating.

### The lesson R8 produced three times, and it is an addition to `adr/0043`

**A maximum over 33,018 volume indices** was chosen before anyone knew the distribution was nine parts
empty. **An unconditioned p99** was chosen before anyone knew the same thing. **A monotonicity test**
was chosen before anyone knew the response was a cliff — the first spread rung alone carries a factor
of 15.59× and everything after it is flat inside the noise.

**Each was a statistic chosen before the shape of what it would measure was known**, and each survived
into a published wire because nothing in the process asks that question. `adr/0043` requires a claim a
measurement could settle to **name the number that would refute it**. R8 adds a second requirement:
**name the shape you expect, because a number read off the wrong shape is not evidence.** A wire should
be re-derived once the first measurement shows what the response looks like, and the re-derivation
**stated and scored separately rather than swapped in**.

**And a siting lesson beside it, which is the most transferable thing here.** The spread ladder was
sited at the **median of the measured improvement distribution** — a defensible choice made precisely
to avoid sweeping around a number nobody had grounded, and R3's denominator rule applied correctly.
It still put every rung past the transition. ***A sweep across a measured distribution is not
automatically a sweep across the regime the mechanism operates in.*** Measuring the denominator is
necessary and it is not sufficient; siting a sweep means locating the **regime**, which means finding
the transition first.

### What R8 decided, and what it did not

- **Settled: `03 §3.4`'s self-correction closes with only the local layers reading the VDF**, under a
  sustained asymmetry, 42.62% below a control with identical physics. **Static Habit survives as the
  null hypothesis** and the refresh-cadence question stays shut — no maintenance scheme, no
  hash-bearing cadence, and R4.6's break-even does not select an algorithm after all.
- **Settled: the Sight Horizon's floor is 1 Segment**, derived from the graph with no traffic, at the
  p90 of the arrival distribution. 98.02% of arrivals are already at a node with a real choice.
- **Settled: Temperament damps**, by 92.28% where a herd exists, on an instrument shown able to
  separate a maximal-herd positive control from every swept rung. **The wire stated on monotonicity is
  REFUTED as written** and the two readings stand side by side.
- **Settled: a stored route cannot afford Sight.** 3,951% of the Tick budget against 3.18% for a
  next-hop read. Session M's third axis.
- **Not settled, and it outranks the rest: what to do about the tree.** 87.25% of traffic on 1% of the
  road is a property of District-granular free-flow routing, not of the city, and no rung of the load
  sweep is both congested and resolvable. **This is decision 11 on a different axis** and it is not
  S2's to take.
- **Owed to `03 §3`: nothing guarantees Sight and Promotion read the same congestion quantity.** Sight
  reads live `v/c` at a junction; Stress drives Promotion. If they diverge the city routes around a jam
  it never promotes, which is `01 §7`'s contradiction rule arriving between two parts of the simulation
  rather than between the simulation and a panel.
- **Owed: the `Refresh` sweep must become incremental.** `O(arcs)` per Tick, flat in fleet size, costing
  more than the entire traveller loop. The arcs whose cost moved are already enumerated by the loop that
  moved them.
- **Owed: four Ruleset numbers remain unset** — Sight Horizon above its floor, base Temperament
  threshold, base/jitter blend weight, Habit refresh cadence (provisionally infinite, and R8.5 is why).
  **R8 reports curves and chooses none of them**, exactly as R1 did for the District count.

---

## S2 R6 — the two caches

**Ordered after session M on the board, and started ahead of it on a gate audit.** `plans/0000`
gates R6 on session **M**, and [`adr/0047`](adr/0047-routing-never-keys-on-the-district.md) has since
closed M's path-source half by name — its table carries the same three figures
`plans/0002`'s row does (16.58% uniform, 149.73% local, unmoved across a storm deleting 1,021
Segments) and concludes *"the table was never a path source."* **What survives of M against R6 is the
invalidation contract**, and R6's own two questions are the **key** and the **eviction policy**,
neither of which invalidation touches: the key's cost is a detour, and R5.3 measured the miss column
*before a road is touched*. **This is `plans/0000`'s own diagnostic — a gate whose stated reason
covers only part of what it blocks — applied to a spike row rather than a slice row.**
`plans/0002`'s path-source row is stale and belongs in [`0012`](../plans/0012-corpus-audit.md).

### R6.0 — the pristine-seeding repair, and what a dead column was hiding

R5.5 recorded that `HpaSearch` priced the two Access Point remainders against a **null cost array** —
the pristine `graph.ArcCarTicks` — while the storm deletes into a shadow clone, so the hierarchy
returned routes down roads the player had just bulldozed. It declined to repair it, because repairing
it re-baselines R5.3, and instructed R6 to fix it before caching anything.

**The defect was wider than it was filed.** Not the two remainders but **eight call sites**, and the
four R5.5 did not name are the worse ones:

| Site | Path | Why it is worse |
|---|---|---|
| `Hierarchical:273,274,283,284` | goal remainders, forward seeds | what R5.5 named |
| `Run:131` | same-Segment bypass | returns `HpaOutcome(true, …)` **directly**, never entering a confined search |
| `Through:243,244` | adjacent-Segment bypass | the same, from `Run:137` |
| `BypassFor:149` | R3.8's share column | classifies a bulldozed Segment as a bypass |

The seeding defect at least left the confined search reading live costs downstream. **A bypass reads
the pristine array and returns**, so nothing downstream can catch it — and R3.8 puts the bypass at
**78.28%** of Legs inside one block against 1.75% at two, so the defect was heaviest exactly where the
local-trip O-D rung lives, which is the rung `adr/0047` quotes 149.73% from. **The control had it
right throughout**: `PointToPoint` threads its cost array through every call including
`SameSegmentCost`; `HpaSearch` threaded it through none. That asymmetry is the whole 416-against-16 gap.

**Repaired, a column R5.5 called *"zero by construction… evidence of nothing"* becomes the sharpest
staleness instrument in the section.** Over the same 12 rows, against a control finding 416 severed
lookups:

| Rung | Forced refreshes / Tick | Unroutable | Share of the control's |
|---|---:|---:|---:|
| cache, no rotation | 0.00 | 345 | 82.93% |
| cache+ttl 1024 | 0.34 | 350 | 84.13% |
| cache+ttl 256 | 1.35 | 377 | 90.62% |
| cache+ttl 64 | 5.08 | 412 | **99.03%** |
| flat — the truth | — | 416 | 100.00% |

Before the repair each rung read about **4**.

**The residual gap is not a defect — it is the cache serving routes down roads that are gone, and it
is monotone in the refresh rate with the control as the asymptote.** The only thing separating those
four rungs is how often an entry is forced to look again, and severance is precisely what a route that
never looks again cannot discover. That is a causal demonstration rather than a correlation.

**It corroborates R5.5.4 through a different column entirely.** R5.5.4 measures staleness as *detour*
against a truth search; this measures it as *severance*. Two independent instruments now agree that a
rotation clears what the Epoch rung structurally cannot — **evidence session M did not have.**

**`nexthop` and `shared` report zero severed lookups at every rung**, because both answer through a
District representative and always return *something*. That zero is the unwired-instrument shape, not
a result — a fourth argument against the table, arriving after `adr/0047` closed it on four others.

### The re-baseline, bounded by checksum

| Section | |
|---|---|
| R5.1, R5.2, R5.4, R5.5.1, R5.5.3, R5.5.4 | **bit-identical** |
| R5.3 | hit −0.1 to −0.6pp, stale +0 to +0.4pp, miss +0.03 to +0.7pp, `Unroutable` 0.00% → 0.14–2.75%; evictions 29,373 → 28,953 |
| R5.5.2 | the repair itself |

**Nothing session M leans on moved.** R5.5.4's verdict table — 0.40 forced refreshes/Tick, 97.08%
resident retained, wrongly-valid 38 → 0 against a control plateauing at 23 — is bit-identical, because
R5.5.4 runs an *addition* scenario in which no cached route ever points at a missing Segment, so the
pristine/live distinction cannot bite. **R5.3's conclusions survive**: the ladder ranking per-Segment >
per-cluster > global holds at every rung, and the miss column reads **28.39–31.49%**, still flat across
a 16× edit-rate range — which is R6.2's premise, intact.

**Every count above is bit-identical across three captures**, two unpinned and one pinned. **No timing
figure from them may be quoted**: all three are `powersave`, and the root capture is owed alongside
R7's, exactly as S0a's is.

### A denominator that survived because everything it divided was near zero

R5.5 published the disagreement as **16 against 416** — but the 16 summed **four** cache rungs where
the 416 was **one** control rung. The two sides never had the same denominator. It cost R5.5 nothing,
both figures being about 1% of the control. Summed the same way after the repair it reads **1,484
against 416**, which invites exactly the wrong reading — *the hierarchy is 3.6× worse* — where per rung
it is 82.93–99.03% of it. **A broken denominator survives while every number it divides is near zero**,
and the repair is what made it visible. Sixth instance in S2 of *an argument for reporting a quantity
you expect to be boring*, and the first where the arithmetic rather than the sampling was wrong.

### R6.1a — what a coarser key costs, and the column that should be quoted instead

**The measurement R5.5.2 named and declined to make.** Its detour column compares an *arc sum* with an
*arc sum*, so a cached route is never charged the remainders its key forces — which is why every cache
row there reads about 0.00%, and why that section says in terms that *"correcting it would mean
charging the served route the remainders its own endpoints imply… and it is not made here."* Made here,
in a new `--key` section. **No storm runs in it**: the key's error is structural, present on a graph
nobody has touched, every Tick, for ever — and mixing it with invalidation would confound two errors
that heal differently, which is the distinction R5.5 drew between a structural and a temporal currency.

Every figure is a **whole-journey** cost — arcs plus both Access Point remainders — against a flat
search on the same graph. `SearchOutcome.CostTicks` already carries exactly that quantity, so nothing
had to be reconstructed.

| O-D rung | Key | Mean detour | p90 | Worst | Mean, Ticks | Worst, Ticks | Sample |
|---|---|---:|---:|---:|---:|---:|---:|
| uniform | `node-a` | 1.84% | 3.85% | 37.83% | 0.93 | 11.61 | 2,019 |
| uniform | `nearest-node` | 0.80% | 1.84% | 23.77% | 0.41 | 7.52 | 2,028 |
| uniform | `best-endpoint` | 0.00% | 0.00% | 2.70% | 0.00 | 1.52 | 2,045 |
| uniform | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,048 |
| decay L=1024 | `node-a` | 3.09% | 6.81% | 106.66% | 0.90 | 11.61 | 2,017 |
| decay L=1024 | `nearest-node` | 1.55% | 3.31% | **128.20%** | 0.41 | 7.52 | 2,025 |
| decay L=1024 | `best-endpoint` | 0.00% | 0.00% | 7.43% | 0.00 | 1.52 | 2,040 |
| decay L=1024 | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,048 |
| decay L=256 | `node-a` | 9.70% | 23.77% | 552.95% | 0.86 | 12.21 | 2,011 |
| decay L=256 | `nearest-node` | 4.22% | 10.77% | 128.20% | 0.42 | 7.68 | 2,021 |
| decay L=256 | `best-endpoint` | 0.02% | 0.00% | 9.41% | 0.00 | 1.52 | 2,039 |
| decay L=256 | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,045 |
| monocentric L=512 | `node-a` | 1.91% | 4.20% | 42.55% | 0.94 | 12.32 | 2,008 |
| monocentric L=512 | `nearest-node` | 0.85% | 1.86% | 29.78% | 0.42 | 7.52 | 2,022 |
| monocentric L=512 | `best-endpoint` | 0.00% | 0.00% | 4.02% | 0.00 | 1.63 | 2,038 |
| monocentric L=512 | `access-point` | 0.00% | 0.00% | 0.00% | 0.00 | 0.00 | 2,048 |

**`access-point` is the control and reads exactly zero on every rung**, measured by a second
independent search rather than assigned from the truth — a zero from `served = truth` would prove the
assignment worked and nothing else. **Routes cheaper than the unconstrained optimum: 0**, printed on
the run where it reads zero, because it is the one way this instrument could be wrong in the direction
that flatters its subject.

**The headline is in the absolute columns, and it is that the percentage columns should not be
quoted.** `node-a`'s mean error is **0.86–0.94 Ticks across the whole O-D family** and
`nearest-node`'s is **0.41–0.42** — flat to two decimal places — while the *percentage* for the same
key swings **1.84% → 9.70%**, better than five-fold. The worst absolute is equally flat: 11.61–12.32
Ticks for `node-a` on every rung. **The key's error is bounded by Segment geometry and has nothing to
do with the trip distribution**; the percentage is a statement about journey length wearing a statement
about the key.

**This is R4.1's finding reproduced one layer down.** There, a District-granular detour went 18.52% →
128.82% because the error was fixed in Ticks and the journey was not — which the corpus treats as a
serious correctness finding and `adr/0047` cites. Same shape, same cause, a different mechanism. **The
difference is that here the rung-invariant number exists**, so the right repair is not *name the rung
beside every figure* but *carry the absolute instead*.

**`node-a` costs exactly twice what `nearest-node` does, on every rung**, and the factor is geometric
rather than empirical: node A is an arbitrary end, so a traveller pays a half-Segment on average at
each end where choosing the nearer end pays a quarter. **The fix is free** — one comparison per Access
Point at insert, key space unchanged at nodes² — and `adr/0012`'s owed amendment can state it in a
sentence.

**`best-endpoint` reads 0.00% mean on three rungs and 0.02% on the fourth**, which settles the question
the ladder exists to separate: **a nodes² key is essentially free if the endpoint is chosen well.** The
error is not intrinsic to keying on nodes; it is an artefact of choosing badly. What no nodes² key can
recover is the tail — `best-endpoint`'s worst is still 2.70–9.41%, because forcing a journey through a
node forbids the partial-Segment shortcut, and short trips pay for that.

**But the greedy choice is not monotone, and the tail is where it shows.** On decay L=1024
`nearest-node`'s worst reads **128.20%** against `node-a`'s **106.66%** — the coarser key wins that
column. *Nearer along the Segment* is not *better for the journey*: the near endpoint can point away
from the destination, and the traveller then pays the Segment twice. **A mean improved by 2× and a tail
made worse is a trade rather than a strict win**, and `05 §4` says to look at the shape rather than the
average.

**Every count reproduces bit-identically across three captures**, two unpinned and one pinned; all are
`powersave` and no timing figure from them is quotable. Same-Segment pairs are excluded and the sample
size is printed per row — they are answered by R3.8's bypass without consulting any cache, so charging
a key for them would credit the key with a case it never sees.

**What R6.1a cannot say is anything about hit rate, and the reason is a modelling gap rather than an
omission.** Hit rate is a property of how many *distinct* keys a population of Buildings generates, and
**S2 has no Buildings** — it draws Access Points at random offsets on random Segments, so no two pairs
share a Segment except by accident. `plans/0010`'s *"the five Buildings sharing a Segment share one
entry instead of minting five"* is a statement about a population this spike does not have. Measuring
it needs an invented Buildings-per-Segment pool, and **an invented pool must be swept or its level is a
guess wearing a measurement's clothes** — R5.3's debt, in the same words. That is R6.1b.

### R6.1b — the key space, and a claim S2 cannot confirm

**`plans/0010` argues the key on hit rate, and S2 cannot draw the population the argument is about.**
*"Keyed on those, the space is Buildings² ≈ 2.25 × 10¹⁰ and the hit rate is approximately zero. Keyed
on the endpoints… the space collapses to nodes² and the five Buildings sharing a Segment share one
entry instead of minting five."* That is a claim about **Buildings**, and this spike has none. Two
inventions were built and swept — Buildings per Segment (1, 5, 20) and destination sites
(unrestricted, 128, 32, 8) — on R5.3's rule that an invented pool must be swept or its level is a
guess wearing a measurement's clothes.

**The collapse column reads 1.00× on every row of both sweeps, and after two attempts to move it that
is the finding rather than the failure.** A node-keyed entry collapses two Trips only when they share
a Segment at **both** ends. Concentrating destinations onto 8 sites leaves 512 distinct origins, so
the pairs stay distinct; adding Buildings to a Segment mints Access Points without making two Trips
end together. **Collapse is a property of the ratio between the Trip population and the Segment-pair
space**, and the working graph has **33,018 Segments — about 1.09 × 10⁹ ordered pairs.** No pool S2
can draw is dense in that.

**So `plans/0010`'s argument for the coarse key is unconfirmed — not refuted, and not available to be
cited either.** The five-Buildings sentence holds only if those Buildings' Trips also *end* on a
shared Segment. Against 10⁹ Segment pairs a real city's Trips may be sparse enough that a node key
collapses almost nothing, in which case the hit rate comes from **the same person repeating the same
journey**, which no key affects — and the coarse key would be paying R6.1a's detour for very little.
**Settling it needs a Trip population, which is `06` milestone 5b.** R6.1a settles the price side
exactly, and settles that the price is avoidable at no cost in key space; that asymmetry is the
useful part.

**Two things did come out of it, and one belongs to R6.2.**

- **The miss floor reproduces from outside R5.** Every unrestricted row sits near 70% hit — a **~30%
  miss with no storm, no Epoch and nothing stale** — which is R5.3's *28–31% of lookups missing on
  direct-mapped collisions before a road is touched*, on a different harness and a different pool.
  **R6.2's premise is independently confirmed.**
- **The slot function degrades on structured keys, and it is not a capacity effect.** As destinations
  concentrate, `access-point`'s hit rate falls **71.9% → 15.9%** with evictions rising 738 → 3,362,
  while `node-a` on the same pools falls only to 55.6%. **Distinct keys stay at 511–512 throughout**,
  so the cache is not full and the key space has not shrunk. `RouteCache.Slot` is one multiply and one
  xor-shift, and where the low half of the key takes few values it clusters. **A cache can lose two
  lookups in three to its hash while holding every entry it needs.** R5.3's 28–31% is the same defect
  at a gentler input.

**No document may cite a hit rate from this section.** Both axes are invented, neither moved the
column it was built to move, and every level is a property of a 512-pair pool standing in for Trip
repetition that does not exist. What carries out is structural: **collapse needs coincidence at both
ends**, and **the slot function degrades on structured keys**.

### R6.2 — the eviction policy, and who is to blame for a miss

**No eviction policy is stated anywhere in the corpus.** `adr/0012` permits caching and says nothing
about what leaves; `adr/0017` shows the pattern — fixed capacity, least-used eviction — and nobody has
written it down for routes. `RouteCache` implements neither: **direct-mapped, one slot**, and an
insert whose slot is taken overwrites.

**This is the one part of R6 that does not depend on the number nobody has measured.** The cache's
absolute hit rate rests entirely on Trip repetition, which needs `06` milestone 5b. But a lookup that
*should* have hit and did not is a pure loss whatever the repetition rate turns out to be. So the
section reports **blame** rather than a rate, on the standard three-way split: *cold* (first
reference), *capacity* (a perfect cache of the same size would also have missed), and **conflict** (a
perfect cache of the same size would have hit, and this scheme missed anyway). **Conflict is the only
column that is a defect.**

1,024 entries, 16,384 Trips, `nearest-node` keys, load swept against capacity.

| Load | Scheme | Hit | Cold | Capacity | **Conflict** | Probes |
|---|---|---:|---:|---:|---:|---:|
| 0.50× | `direct`, modulo — **shipped** | 76.7% | 3.1% | 0.0% | **20.0%** | 1 |
| 0.50× | `direct`, high bits | 75.6% | 3.1% | 0.0% | **21.1%** | 1 |
| 0.50× | 2-way LRU | 86.1% | 3.1% | 0.0% | **10.6%** | 2 |
| 0.50× | 4-way LRU | 92.9% | 3.1% | 0.0% | **3.8%** | 4 |
| 0.50× | 8-way LRU | 95.4% | 3.1% | 0.0% | **1.4%** | 8 |
| 0.50× | fully associative — *bound* | 96.8% | 3.1% | 0.0% | **0.0%** | — |
| 1.00× | **shipped** | 61.4% | 6.2% | 0.0% | **32.2%** | 1 |
| 1.00× | 4-way LRU | 77.4% | 6.2% | 0.0% | **16.2%** | 4 |
| 2.00× | **shipped** | 40.6% | 12.5% | 29.8% | **16.9%** | 1 |
| 2.00× | 4-way LRU | 47.0% | 12.5% | 31.7% | **8.7%** | 4 |
| 0.50×, 8 sites | **shipped** | 65.6% | 3.1% | 0.0% | **31.2%** | 1 |
| 0.50×, 8 sites | `direct`, high bits | 75.1% | 3.1% | 0.0% | **21.7%** | 1 |
| 0.50×, 8 sites | 4-way LRU | 92.7% | 3.1% | 0.0% | **4.1%** | 4 |

**R5.3's 28–31% floor is not a property of cache size and never was.** At its own rung the shipped
scheme's misses are **20.0% conflict and 0.0% capacity** — every one a lookup a perfect cache of the
same size would have served. Reading it as a floor is what made it look like a fact of life rather
than a bug with a fix.

**Associativity is the lever and four ways is where it stops paying.** Conflict falls 20.0% → 10.6% →
3.8% → 1.4% across 1, 2, 4 and 8 ways against a bound of 0.0%. **Four ways recovers most of the gap at
four contiguous probes** — on the cache line an entry already occupies, close to free. This is
`adr/0017`'s pattern sized, and the first number the corpus has for it.

**The index function is not the lever, and this task predicted that it would be.** The hypothesis on
file was that `RouteCache.Slot` multiplies by the golden-ratio constant, driving entropy upward, then
takes `% capacity`, which reads the low bits — so the modulo discards what the multiply created.
**Measured, that is wrong on random keys**: high-bit indexing reads 21.1% against 20.0% at 0.50× and
32.6% against 32.2% at 1.00%, level or slightly worse. A route key is already a pair of well-spread
node ids, so the low bits carry no structure for a modulo to expose. **Recorded rather than quietly
dropped**, because the draft prose asserted the fix worked and would have published a hypothesis as a
result — the defect this spike has now caught three times in its own documents.

**Where it does help is exactly where R6.1b found the damage.** On the eight-destination pool, high
bits take conflict **31.2% → 21.7%** and four ways takes it to **4.1%**. So the index function is a
**robustness** fix rather than a throughput one: it costs nothing and stops a concentrated city
falling off a cliff the uniform draw never shows. **Both changes are worth making and only one shows
up in the average case**, which is the argument for measuring the concentrated rung at all.

**One honest limit.** R6.1b's worst case — 15.9% hit — was the `access-point` key; this table keys on
`nearest-node` throughout, where the same pool reads 65.6%. The conflict column is clearly elevated
against the unconcentrated rung at identical load (31.2% against 20.0%), so **the mechanism is
confirmed and the magnitude is not.** This table does not reproduce R6.1b's extreme.

**Load is the axis R5.3 never swept, and it dominates.** Conflict at four ways runs 1.0% → 3.8% →
16.2% across 0.25×, 0.50× and 1.00×, and capacity misses appear only at 2.00×, reaching 29.8%. **R5.3
measured one load and called the result a floor; it is a point on a curve that triples.**

**Every count reproduces bit-identically across a pinned and an unpinned capture.** All `powersave`;
no timing figure is quoted because the section publishes none.

### Two labels that asserted what nobody had checked

**Both found while checking whether R6's own captures were quotable, and both are provenance defects
rather than measurement ones.** `Capture.cs` closed the machine-state block with *"A run whose memory stall
is a rounding error is a run the pinning actually protected."* That was **static prose**. The block
read the governor and never read the affinity mask, so **every unpinned capture declared itself
pinned** — in the section that exists so a reader *"need not reason about the machine afterwards."*

This is R5's *a published absolute with no artefact behind it is not a measurement* moved one layer
further out, from **retention** to **provenance**. R5 fixed the filenames so the configuration is
visible in an `ls`; the report went on asserting the opposite of what the filename said.

Fixed: the block reads `Cpus_allowed_list` and prints the branch it measured, condemning an unpinned
capture in terms. **The fix had the same defect one level down and it is worth recording.**
`Environment.ProcessorCount` **respects the affinity mask**, so under `taskset -c 2,8` it returns 2 and
`allowed < ProcessorCount` compares 2 with 2 and answers *not pinned*. Untested, the new instrument
would have condemned every canonical capture in `results/`. **It was caught only by running the pinned
branch rather than reasoning about it** — the corpus's rule about a correctness column that reads zero,
arriving at a boolean.

**The second is the same shape in `routing-run.sh`.** Its filename encodes which sections a capture
holds, from a `case` list that did not know about `--key` — so an unknown section flag fell through to
`all` and R6.1's first pinned capture was written as `s2-all-…` holding **one** section. The runner
accepted the flag and the labeller did not, and nothing connected them. Unknown flags are now refused
outright rather than silently mislabelled, and the misnamed artefact was deleted rather than kept.

**Three instances in one sitting, and they are one defect.** The machine-state block asserted a pinning
it never read; the filename asserted a scope it never checked; and R5.5's *16 against 416* asserted a
comparison whose two sides had different denominators. **Each is a claim written once and thereafter
carried by prose rather than by measurement** — which is R5's *a published absolute with no artefact
behind it is not a measurement* generalised past absolutes to labels. `spike-results` already records
that the retention layer had this problem. It has it at the provenance layer too.

### R6.3 — the two consumers, multiplied

**Every budget figure in this corpus counts Trip starts.** R3's tripwire — *routing fits while fewer
than 85 Trips start per Tick* — counts them, and `plans/0013`'s routing row counts them. But
`adr/0046` introduced **Sight**, R8.3 measured what it does — **1,269.51 diversions per Tick at 40,000
Travellers** — and `adr/0047` then deleted the next-hop table, which was the only path source that
served a diversion cheaply. **Nobody had multiplied those together.** A new `--budget` section does,
pricing its own basis in-process rather than quoting absolutes across captures.

| Event | Cost | Mean arcs |
|---|---:|---:|
| Whole-journey search — a **Habit formation** | 439.17 µs | 58 |
| Remainder search from mid-journey — a **diversion** | 105.70 µs | 28 |
| Cache hit — lookup and compare | 39 ns | — |
| *the same whole-journey loop, measured last* | *453.72 µs* | — |

**The bill at R8's own rung — 40,000 Travellers, a 7-Day Habit — is 0.316 ms of Habit formation and
134.135 ms of diversion.** That is **424.15× the formation bill and 99.76% of routing's total**, or
**861.87% of the Tick budget**. The in-flight rungs are **S0a's own band for a 1,000,000-population
city** — 37,000 / 56,000 / 111,000, swept because the mean Trip duration behind them is unmeasured —
so **nothing here is a small city being extrapolated from**. The band's floor is 795.91% and its
ceiling 2,387.73%.

**An earlier draft of this section said the opposite**, calling 40,000 in flight *"a fleet 4% of the
target population"* — true as arithmetic against a 1,000,000 **population**, and wrong about what the
number is, which is a count of Trips *in flight* that S0a derived **for that same population**. It
would have read as *and it gets 25× worse at full size* when the measurement is already at full size.
**Seventh instance of the session's recurring defect and the third that is mine**: a figure carried by
a label rather than by its derivation.

**Habit is doing exactly what `adr/0046` claims, and that is why the finding is not about Habit.** A
Citizen that computes a route once and keeps it for a week costs well under one search per Tick across
a whole fleet. **R3's 85 Trip starts is not the binding constraint and never was**, because a Trip
start under static Habit is a *lookup*. The constraint is the thing the same ADR introduced in its next
paragraph — and no document counts it.

Published R3's way, as a threshold on a quantity rather than a multiple over a guess:

| Policy | Cost per diversion | Diversions/Tick that fit | R8's rate is |
|---|---:|---:|---|
| **re-search** — what the corpus specifies today | 105.70 µs | 147 | **over**, by 8.63× |
| *the same, on R8.6's own diversion cost* | *485.50 µs* | *32* | *over, by 39.65×* |
| cache-served | **no hit rate exists to price this** | — | see below |
| **rejoin** the Habit Route, no search — *unproposed* | — | unbounded | **free** |

**The two ends of the basis disagree by 4.59× and the conclusion survives both.** This section's
remainder search is the optimistic end — a midpoint site on a graph nobody is bulldozing. R8.6's
485.50 µs is the pessimistic end, on sites a live fleet actually diverted at, and it sits close to a
*whole-journey* cost rather than half of one, which is itself worth someone's attention. **Between 32
and 147 diversions per Tick fit. R8 measured 1,269.**

**The cache is not priced here, because R6.2 says in terms that no document may cite a hit rate from
it.** It is inverted instead — a cached diversion costs `hit + miss-rate × search`, so the question is
what hit rate makes it fit:

| In flight | Diversions/Tick | Required hit rate, optimistic basis | Required hit rate, R8.6's basis |
|---:|---:|---:|---:|
| 37,000 | 1,174 | **87.5%** | **97.3%** |
| 40,000 | 1,269 | **88.5%** | **97.5%** |
| 56,000 | 1,777 | **91.8%** | **98.3%** |
| 111,000 | 3,522 | **95.9%** | **99.1%** |

**R6.1b is why those are not attainable.** A diversion is keyed on *(wherever I now am → destination)*,
and a mid-journey position is an arbitrary point along a route rather than a Building. R6.1b
established that a coarse key collapses **nothing** unless trips coincide at *both* ends — and
diversion origins coincide far less than trip origins do, because they are wherever congestion happened
to be. **The cache is being asked for its best case on its worst input.**

**So `adr/0046` and `adr/0047` are not jointly affordable under the policy the corpus currently
specifies.** Three levers exist and two are Ruleset numbers already unset — raise the **Temperament**
threshold so fewer drivers act on what Sight shows them, or shrink the **Sight Horizon** toward its
1-Segment floor. The third is the row nothing in the corpus proposes: **let a diversion rejoin the
Habit Route without re-searching**, which is what a driver with a map in their head actually does, and
which is free by construction.

**R6.3 does not pick one, and cannot.** `05 §4` says a different route is a different city and all
three change which route a Traveller takes, so this is session **M**'s to answer. What R6.3 supplies is
that **it must be answered** — which the corpus did not previously know, because the two facts sat in
different documents and were never multiplied.

**This is also the counter-example to the board's claim that the three tracks do not contend.** They do
not contend for *files*, which is what the claim was about. But `adr/0047` was ratified on the ADR
track and it **invalidated a spike's denominator** — R3's tripwire went on counting Trip starts after
the event that dominates routing's bill stopped being one. **A decision track can silently retire a
measurement track's question**, and nothing in the process notices.

### What R6 decided, and what it did not

**Written by R7, and late.** Every other round closes with one of these; R6 has run four sub-rounds
without a round-level verdict, and the reason is the same fact as the open gate — **R6 is the only
round that cannot finish**, so nobody wrote the section that would have to say so. That was a mistake
in the other direction: R6.0 through R6.3 decided a great deal, and none of it was collected.

#### Decided

- **Four-way LRU, and it is the corpus's first sized statement of `adr/0017`'s pattern.** Conflict
  falls **20.0% → 3.8%** at four contiguous probes against a fully-associative bound of 0.0%, and four
  ways is where the lever stops paying. The probes land on a cache line the entry already occupies.
- **High-bit indexing, as a *robustness* fix and not a throughput one.** It is level-or-worse on random
  keys — 21.1% against 20.0% — and takes conflict **31.2% → 21.7%** on a concentrated eight-destination
  pool. It costs nothing and stops a concentrated city falling off a cliff the uniform draw never
  shows. **Both changes are worth making and only one of them appears in the average case.**
- **The key is `nearest-node`, repaired by one comparison at insert.** `node-a` costs exactly **2×**
  `nearest-node` on every rung, geometrically, and the key space is unchanged by the fix. **Quote the
  absolute and never the percentage**: the mean error is **0.86–0.94 Ticks, flat across the entire O-D
  family**, while its percentage swings 1.84% → 9.70% with the trip distribution. R4.1's finding one
  layer down, except that here the rung-invariant number exists.
- **R5.3's 28–31% miss floor is not a floor, and never was.** At its own rung it is **20.0% conflict
  and 0.0% capacity** — every miss a lookup a perfect cache of the same size would have served. It is a
  bug with a fix, not a fact of life, and reading it as a property of cache size is what hid that.
- **Load is the axis R5.3 never swept and it dominates** — conflict at four ways runs 1.0% → 3.8% →
  16.2% across 0.25×, 0.50× and 1.00× load. One point on a curve that triples was published as a floor.
- **The diversion, not the Trip start, is routing's bill** — **99.76%** of it. R3's *85 Trip starts per
  Tick* is not the binding constraint and never was, because a Trip start under static Habit is a
  lookup.

Together these are `adr/0012`'s owed amendment, which can now be written: the **key** (nodes², nearest
node, one comparison at insert), the **eviction policy** (fixed capacity, 4-way LRU, high-bit index),
and — from R5.6 rather than from R6 — that **the two consumers do not want the same mechanism**, routes
needing a temporal answer and Parking Sheds needing no rotation at all.

#### Not decided, and three of the four cannot be

- **The invalidation contract.** Session **M**'s, and it is R6's gate. Neither the key nor the eviction
  policy is downstream of it, which is why this round ran ahead of the gate — see below.
- **The cache's absolute hit rate.** It rests on Trip repetition, which needs `06` milestone 5b.
  **This is the round's shape and it is worth naming: R6.1a settles the price exactly and R6.1b cannot
  settle the benefit.** The two halves of the same question have different epistemic status, and only
  one of them was ever going to close inside a spike with no Trip generation in it.
- **Which of the three diversion levers.** Raise Temperament, shrink the Sight Horizon, or **rejoin the
  Habit Route without re-searching** — the third being the one nothing in the corpus proposes and the
  only one that is free by construction. All three change which route a Traveller takes, and `05 §4`
  says that is a different city, so it is a design decision. **R6.3's contribution is that it must be
  answered**, which the corpus did not previously know.
- **`plans/0010`'s five-Buildings argument for the coarse key is unconfirmed and may not be cited.** A
  node key collapses two Trips only if they share a Segment at **both** ends, and 33,018 Segments is
  1.09 × 10⁹ ordered pairs — no pool S2 can draw is dense in that.

#### What R6 found that it was not looking for

- **R6.3 was not on the plan at all**, and it produced the round's largest result. It was run because
  the two halves of routing's bill had never been added up — `adr/0046` made diversion routine in one
  document and `adr/0047` deleted the cheap path source in another, and **nobody had multiplied them**.
- **R6.0's repair turned a dead column into the section's sharpest instrument.** The pristine-seeding
  defect was **eight call sites rather than the four filed**, and once repaired `Unroutable` stops being
  *"zero by construction"* and reads **82.93% → 99.03%** of the control's severed lookups, **monotone in
  the refresh rate**. That corroborates R5.5.4 through an entirely different column, and it is evidence
  session **M** did not have when the question was framed.
- **Two provenance defects, both about labels rather than measurements.** The machine-state block
  asserted a pinning it never measured — it read the governor and never the affinity mask, so **every
  unpinned capture declared itself pinned**, in the block that exists so a reader need not reason about
  the machine. And R5.5's *16 against 416* summed four cache rungs against one control rung, so the
  comparison never had a denominator.
- **The board's *the three tracks do not contend* has a counter-example.** They do not contend for
  **files**, which is what the claim was about. But `adr/0047` was ratified on the ADR track and
  **invalidated a spike's denominator** — R3's tripwire went on counting Trip starts after the event
  that dominates routing's bill stopped being one. A decision track can silently retire a measurement
  track's question, and nothing in the process notices.

#### The round was started ahead of its gate, and that was right

`plans/0000` gates R6 on session M. R6 ran anyway, on a gate audit, and the audit was correct:
`adr/0047` had already closed M's path-source half by name, and what survived — the invalidation
contract — touches neither of R6's two questions. **This is the corpus's own diagnostic, *a gate whose
stated reason covers only part of what it blocks*, applied to a spike row rather than a slice row**,
and it is the third instance after `adr/0003`'s split debt and `06`'s ordering. The general lesson is
already written down: for each blocked row, ask what the gate's reason *does not* cover, and check
whether that remainder is runnable today.

---

## S2 R5.6 — the Parking Shed, and the rung it disagrees with

**The second Epoch consumer, and `plans/0010` named it the one the ladder was most likely to be
decided by.** It scales with **Buildings** rather than routes, and `05 §3` declares it cached per
Building and *"invalidated by the Road Graph Epoch."* Harness at `spikes/S2.Routing/Storm/ParkingShed.cs`
and a `--shed` section; the storm, the gestures and the cluster partitions are R5's own, so the two
sections compare directly.

**Four rungs where routes had three, and the extra one is the whole result.** A route's witness is the
arcs it drives and it stores them anyway. A shed has no path, so *"my Segments"* is a **choice**, and
the two defensible answers differ by an order of magnitude: the **ball** — every Segment the walk
explored — or the **paths** — only the walks to the Bins the shed kept. Measuring the conservative one
alone would have condemned the rung on a definition rather than on a number.

### What a shed is

159,825 sheds — five Buildings a Segment, `CONTEXT.md`'s own working figure, on Segments admitting
**both** Car and Foot. Walking distance is swept because the corpus says *acceptable* and states no
number.

| Walk radius | Build | Bins found | Ball Segments | Path Segments |
|---:|---:|---:|---:|---:|
| 200 m | 1.76 µs | 22 | 4 | 1 |
| 400 m | 1.59 µs | 110 | 22 | 2 |
| 800 m | 4.37 µs | 596 | 122 | 2 |

**The path witness saturates and the ball does not.** The ball grows 4 → 22 → 122 with the radius; the
paths witness is **2 Segments at 400 m and still 2 at 800 m**, because it is bounded by the handful of
Bins the shed keeps rather than by how far a pedestrian may walk. **A witness that does not grow with
the parameter nobody has set is worth more than one that is merely small today.**

### The storm

At 400 m, 1.59 µs to rebuild one shed, mean over 24 gestures. *Asked* against *got* is reported because
an Arterial drag runs out of fast road — at 16 and at 256 it is **the same 3-Segment gesture**, and the
two rows must not be read as a trend.

| Gesture | Asked | Got | global | per-cluster (8) | per-cluster (16) | per-Segment (ball) | per-Segment (paths) |
|---|---:|---:|---:|---:|---:|---:|---:|
| drag | 1 | 1 | **1638.20%** | 13.04% | 37.51% | 1.12% | **0.10%** |
| drag | 256 | 199 | **1638.20%** | 164.48% | 264.20% | 51.98% | **14.95%** |
| scattered | 256 | 256 | **1638.20%** | 1351.24% | 1619.75% | 265.45% | **26.10%** |
| arterial | 256 | 3 | **1638.20%** | 37.60% | 100.99% | 0.00% | **0.00%** |

**The global rung is out, and the tripwire fired as written.** One deleted Segment anywhere invalidates
all 159,825 sheds, at **255.560 ms — 1,638.20% of a Tick**. `plans/0010` predicted it in words before
the harness existed. **The number is worse than the sentence**, because `adr/0009` pays the rebuild *on
arrival* — the moment a Trip is trying to finish — so it is not one stall but a stampede spread across
every arriving vehicle in the city, triggered by the player's most common action.

**`plans/0010`'s own prediction about the winner is measured false, and it is the seventh claim in the
corpus to go that way.** The plan argued that *"a per-cluster Epoch fits it far better than it fits
routes: a shed is inherently local, and 'did anything change in my cluster' is close to the right
question already."* It is the **worst surviving rung**: 127× the invalidation of per-Segment (paths) on
a single-Segment drag (1,273 against 10), and 1,351.24% of a Tick on a scattered storm. **The intuition
confuses two localities.** A shed *is* local — 22 Segments — but a cluster is not the shed's
neighbourhood, it is a fixed tile of map holding thousands of sheds, so cluster granularity answers a
question far coarser than the one asked.

**The Arterial rows are where the over-invalidation becomes visible rather than inferred.** Arterials on
this graph are `Modes.Car` only — a motorway has no pavement — so no Building fronts one and no walk
crosses one. Deleting three of them invalidates **zero** sheds under either per-Segment rung, correctly,
and **3,669 under per-cluster**. Every one of those is wrong. **A rung whose error is 100% on a gesture
the design expects is not a granularity that needs tuning.**

**The 8-versus-16 conditional resolves, and it resolves the way R3 already went.** The board carried
R3's cluster-size pick as *"conditional on R5.6, which may rank a Parking Shed differently, so the
sweep is not deleted."* It does not rank it differently: **8 beats 16 on every gesture and every size**,
by 2.9× at a single-Segment drag and 1.6× at a 256-Segment drag. The conditional is discharged and the
sweep may go.

| Rung | Reverse index | Resident |
|---|---:|---:|
| per-cluster (16) | 222,536 entries | 0.84 MiB |
| per-cluster (8) | 301,599 entries | 1.15 MiB |
| per-Segment (paths) | 319,655 entries | 1.21 MiB |
| per-Segment (ball) | 3,608,241 entries | 13.76 MiB |

**Storage decides nothing, exactly as R5 said it would not.** The winning rung's reverse index is
1.21 MiB against a world S0a measures at 85.98 MiB. **The rung that costs the most to store is not the
rung that costs the most to be wrong**, and the two orderings are nearly inverted.

### The verdict

**Per-Segment, witnessed by paths, and it is the only rung that fits.** Worst case **26.10% of a Tick**
against per-Segment (ball) at 265.45%, per-cluster at 1,351.24% and global at 1,638.20%. It carries the
same soundness qualifier `EditStorm` already states for routes — **exact under deletion, unsound under
addition**, because new road can shorten a walk without touching a Segment the walk used — so the
addition case needs the same answer routes get, and does not have one yet.

**And that is the disagreement the section was run to find.** R5 concluded that for routes **no Epoch
rung was both affordable and correct**, and the way out was a TTL rotation — a *temporal* answer. Sheds
need no rotation at all: a structural rung fits with 74% of the budget spare. **The two consumers do
not want the same mechanism**, which is the fact `adr/0012`'s owed amendment has to carry, and it is
also why `plans/0010` was right to insist they be measured together even though its prediction about
which way it would go was wrong.

**Two defects found in this harness before the numbers were believed, both of the session's recurring
kind.** The storm deletes by writing `Impassable` into a *car* cost array, and `SegmentEntry.ArcCost`
**ignores that array entirely when the mode is Foot** — a shed built against it would have walked down
bulldozed roads and reported a serene invalidation cost, which is R5.5's pristine-seeding defect
exactly, in a second consumer. And the path witness at first omitted the shed's **own** Segment, so
bulldozing the road a Building stands on left that Building's shed valid; it showed up as a 0.00%
column, which is the value this spike has learned to distrust on sight.

**One dead column is retained and labelled.** *Empty* — sheds finding no Bin — is **0 everywhere and
cannot be otherwise**, because every Building is a Bin site and so every shed contains its own. It is
kept in the harness rather than deleted so the next reader does not mistake its absence for an unasked
question, but nothing may be concluded from it.

**Capture is `powersave`.** The absolutes owe a root/`performance` re-take alongside R7's, exactly as
R6's and S0a's do.

---

## S2 R7 — the report: the re-capture, the conclusion that moved, and the tripwire scored

**R0, R1, R3 and R4 were all captured under `taskset -c 2` — a single logical processor.** R5 later
measured what that does: with the tiered JIT's background compilation having nowhere to run, its
abstract graph rebuild read **214.94 ms measured first against 43.99 ms measured last, 4.88× apart**,
where the same pair under `-c 2,8` reads 0.92×. The runner has pinned to the sibling pair ever since.
**This is the first correctly-pinned capture of any of those four sections.**

Artefact: `s2-r0+r0d+r1+r3+r4-…-powersave-turbo-cpu2+8`. **The governor half of the debt is not
discharged** — this is `powersave`, and the canonical `performance` capture needs root.

### R3 moved, and it is a clean comparison

**R3's earlier captures were `powersave` too**, so governor is held constant and pinning is the only
variable. Refined route cost, and the break-even the corpus quotes everywhere:

| Rung | Corpus (`-c 2`) | Re-capture (`-c 2,8`) | Break-even, corpus → now |
|---|---:|---:|---:|
| 8, reduced + paths | 237,325 ns | 223,173 ns | 65 → **69** |
| **16, reduced + paths** | **181,554 ns** | **138,641 ns** | 85 → **112** |
| 16 against 8 | **1.31×** | **1.61×** | — |

**The corpus's most-cited routing figure is 32% low.** *Routing fits while fewer than 85 Trips start
per Tick* was measured on one processor; correctly pinned it is **112**. The tripwire's *form* survives
— it is still a measured cost over a world constant, which is why R3 wrote it that way — but its
number was wrong and nothing downstream knew.

**And the 8-versus-16 weighing is no longer the weighing that was published.** `spike-results` records
*"the query advantage 16 holds is 1.31× against an edit penalty of 2×. The recommendation is 8."*
The query advantage is **1.61×**. Against R5's route-edit penalty of 1.9× on a coalesced drag, that is
no longer a decision — it is a coin toss, and the published one was decided by a number that does not
survive its own re-capture.

**What carries the recommendation now is R5.6, and that is the point `plans/0010` was making.** The
Parking Shed prefers **8 by 2.9×** on a one-Segment drag, on every gesture and every size. So the
verdict is unchanged — **8** — but its support has moved from a route-query margin that shrank to a
second consumer that was not measured when the recommendation was written. `plans/0010` warned that *"a
ladder chosen on routes alone would be chosen on the cheaper of the two consumers"*; this is that
warning arriving from the other direction, where routes alone would now have chosen **wrongly** rather
than merely narrowly.

### What did not move

- **R0.** `Chebyshev` is still the only heuristic admissible at every Arterial density — 0 non-optimal
  routes of 300 at every rung, where `Manhattan` reaches 19 and `Octile` 3 by four Arterials. The
  verdict and the ladder are unchanged.
- **R1.** The tripwire still does not fire, and not narrowly: the scattered read is **1.20 ns** at the
  121-District anchor and **5.37 ns** at 4,096, against S4's K2 gather at 13.6 ns. `02 §5.8`'s *never
  resolve a route inside the choice loop* remains enforceable. The row-scan/scatter split and the L3
  ceiling on the scattered pattern reproduce unchanged.
- **R4.** Distance-vector is still out, on the same ground and at nearly the same number: **2.17×** the
  rebuild it exists to avoid, against the 2.13× recorded. Subtree repair still wins by **64.81×** with
  0 entries wrong, the memory wire still does not fire at District granularity (23.12 MiB, bit-identical
  to the recorded figure), and node granularity is still 3.11 GiB and 18.51× the world.

**The counts are bit-identical across pinnings wherever they are counts.** Every column that is a
quantity rather than a duration — entries, wrong entries, non-optimal routes, resident bytes —
reproduces exactly. **Only the timing columns moved, which is the artefact behaving as diagnosed**
rather than a second unexplained difference riding along with it.

### The canonical capture, and the reconciliation it closed

**Taken 2026-08-09, `performance`, turbo, `cpu2+8`, all six sections in one process** —
`s2-r0+r0d+r1+r2+r3+r4-…-performance-turbo-cpu2+8-20260809T142233Z`. Run duration 194.06 s, **CPU stall
0.10% and memory stall 0.00% over the run**, which is the machine block doing the job it exists for.
This is the first capture of R0–R4 under the configuration this corpus calls canonical.

> **One run, not two.** Session eleven's precedent is two captures of one configuration fourteen
> minutes apart, on the argument that *two runs of one configuration are an error bar and one run is an
> assertion.* This is an assertion. Counts are still checkable — they are bit-identical against the
> `powersave` re-pin — but **no nanosecond figure below carries a measured error bar**, and a second
> run is cheap.

**R2's 474.47 ms is resolved, and R7's own diagnosis of it was wrong.** R7 recorded *"217.36 ms
reproduces, so the disagreement is not the pinning artefact and R2's figure is the one under
suspicion."* The conclusion was right and the reason was not: **the two figures were never the same
operation.**

| Operation | R2.1 | R4.3 | R4.4 |
|---|---:|---:|---:|
| 121 backward Dijkstras — the **shared route store** build | **195.44 ms** | **195.42 ms** | **194.94 ms** |
| The **next-hop table** build — a larger operation | **260.57 ms** | — | — |

Three independent measurements of the same operation agree to **0.3%**. What the corpus published as
*"the same 121 backward Dijkstras"* at **474.47 ms** was the **next-hop** build, which is a different
and larger thing, and under correct pinning it reads **260.57 ms**. That is a **1.82×** move — against
the **1.80×** R4 independently measured for the same first-timed JIT artefact. **One artefact, two
witnesses, and the *2.2× disagreement* was comparing two different operations the whole time.**

**Plain Dijkstra's absolute settles, and the standing hypothesis is confirmed.** Driving `None` reads
**723,306 ns** canonically, against **1,240,382** pinned to one logical processor and 779,150 unpinned.
The first-timed row was inflated **1.71×** by tiered-JIT contention, exactly as diagnosed, and the
check R0 asked for — *re-run the ladder, or disable tiering* — is discharged by the pinning fix
instead.

**The break-even holds, so pinning was the whole artefact.** R3's most-cited figure went 85 (one
processor) → 112 (`powersave`, pinned) → **111** (canonical). The governor is worth about 1%; the
correction is real and it is confirmed. The 8-versus-16 query advantage reads **1.49×** — between the
published 1.31× and the `powersave` re-pin's 1.61× — so the conclusion is unchanged: **routes alone
would now pick 16, and R5.6's Parking Shed is what keeps the answer at 8.**

**Tripwire row 2 does not fire under canonical conditions either** — **1.17 ns** at the 121-District
anchor and **5.25 ns** at 4,096, against K2's 13.6 ns.

#### A struck claim comes back, and it should be re-examined rather than re-instated

R0 originally reported that **`EuclideanFloor` is not faster than plain Dijkstra at all**, and the
board struck it as an artefact of the unpinned capture. Canonically, `EuclideanFloor` totals
**759,127 ns** against `None`'s **723,607 ns** — so it is **slower**, and the struck claim is true
under the configuration the corpus now calls canonical. The mechanism was always stated and is
unaffected by pinning: an exact integer square root is a sixteen-iteration loop run twice per node
pushed, which is why `EuclideanFloor` expands the fewest nodes of the safe rungs and costs **204 ns
per expansion** against `Chebyshev`'s 106. **Somebody should decide whether the strike stands**, and
the answer is not automatic: a claim struck for a bad reason can still be true.

#### A capture file is static prose plus generated tables, and only the tables are data

**Found while reconciling R2, and it is a provenance defect with corpus-wide reach.** In this
capture's own R4 section, the verdict prose reads *"472.53 ms against 217.36 ms … 2.17× slower"* and
*"3.35 ms against a 217.36 ms rebuild — 64.81×"*, while **R4.4's table in the same file** reads
rebuild **194.94 ms**, DSDV sequenced **450.80 ms**, dynamic repair **3.05 ms**, **63.86×**. The prose
is authored commentary with numbers written into it; the tables are regenerated per run. **They
disagree because the prose was written against an earlier capture and nothing re-derives it.**

Nothing here moves a conclusion — 63.86× and 64.81× decide the same thing. What it moves is a rule:
**a figure quoted out of a capture must come from a table**, and any figure this corpus took from
capture prose is provenance-unknown rather than merely stale. This is the fourth provenance defect S2
has found in its own reporting, after the mislabelled section filenames, the machine block that
asserted a pinning it never read, and R5.5's denominator-free comparison — and it is the same shape as
all three: **the part of a report a human wrote is the part that does not get re-measured.**

### Against the tripwire — every row, and the three that could not be scored as written

`plans/0010`'s wire has **seven rows**, written before any number arrived, on S4's stated practice:
*the wire was written before the numbers precisely so it could not be reasoned around afterwards.*
**No document has ever scored them.** R8 has a scoreboard and it covers R8's own six internal wires;
S4 has one and it is S4's. This is the first pass over the plan-level rows, and it is the one thing in
R7 that does not wait on the `performance` capture — every verdict below rests on a ratio, a count or
a decision, and row 1 clears its threshold by two orders of magnitude.

| | Row | Verdict | What decides it |
|---|---|---|---|
| 1 | Routing exceeds **10%** of the Tick at 1M, at the morning peak | **FIRES** | R6.3: diversion costs **861.87%** of the budget at R8's rung, and **795.91%–2,387.73%** across S0a's own 1M in-flight band — **80×–239× the allowance** |
| 2 | Matrix read costs more than S4's K2 random gather | **does not fire** | **1.20 ns** at the 121-District anchor and **5.37 ns** at 4,096, against K2's **13.66 ns**. An order of magnitude in hand; `02 §5.8` is enforceable |
| 3 | Either router needs a **global flush** on a Road Graph edit | **FIRES — against a different object** | R5.3: a single-counter Epoch retains **9%** of ceiling under a storm against per-Segment's 96%. R5.6: one deleted Segment invalidates all **159,825** sheds at **255.560 ms**, **1,638.20%** of a Tick |
| 4 | An attribution scheme cannot report a jam **within its cycle** | **FIRES — harder than written** | R2b: the aggregate scheme's lag is **`never`**. It does not report the jam late; it does not report it at all, and `never` appears at a **one-Tick cycle** where no cadence is left to blame |
| 5 | The route cache **grows at steady state** with no bound | **UNSCORABLE** | Nothing in S2 tests it. See below |
| 6 | The congestion loop **does not close** under Sight | **does not fire** | R8.5: both bound 5/5, and Sight settles **42.62% below** the control against a 5.00% bar. Static Habit survives as the null hypothesis |
| 7 | DSDV's tables exceed the world's **172.3 MiB** | **does not fire, at the granularity the design can use** | **23.12 MiB / 0.13×** at 121 Districts; **3.11 GiB / 18.51×** at node granularity. Distance-vector went out on **cost** instead — 2.17× the rebuild it exists to avoid |

**Three fire, three do not, and one cannot be scored. That last count is the finding.**

#### Row 5 could not have fired, and the harness is why

Every S2 harness that touched a route cache made it **fixed-capacity by construction** — R5.3 a
1,024-entry cache, R6.2 direct-mapped at one slot per index. A structure that evicts cannot grow
without bound, so *"grows at steady state with no bound"* was **not a representable outcome** in any
run this spike performed. The row was not tested and found safe; it was never testable.

Three nearby objects were each shown bounded, and none of them is the one the row names: R1.2's
**District-pair route store** grows as *n²* in District count (4.06 GiB at 4,096) — which is a static
configuration axis and not elapsed time; R3.1's **stored-path arena** is explicitly cleared of
`adr/0006` because it is rebuilt rather than appended to; and R6.2 shows the *cache* evicting rather
than growing, with capacity misses at 0.0% until 2.00× load. **Reading any of those as discharging row
5 would be the `adr/0006` mistake this project has already made once**, in slice 6, where a `map`
emission accumulated with no sink and the long-run test built to catch exactly that had been written
around it.

**What the row actually needs is a run with a sink and elapsed time in it**, which is a Phase 2
property: the cache's bound is only interesting once Trips are generated rather than drawn from an
invented family. It is not S2's to close, and R7 records it as **owed rather than clear**.

#### Two of the three that fired, fired at something other than what they named

**Row 3 names *either router* and no router failed it.** R3.7 repairs 1.03 clusters per deleted
Segment; R4.4 repairs a subtree in 4.71 ms. What fired the row is the **Epoch's granularity** — a
single global counter — which is a property of the invalidation scheme, an object the row does not
mention and which R5 had to invent a vocabulary for. The wire caught a real defect at the wrong
address, twice, and only because somebody scoring it was willing to read *"global flush"* as the thing
it describes rather than the thing it names.

**Row 1 carries a qualifier that was never computed.** *"With matrix refresh amortised"* appears in no
measurement in this spike — the word does not occur in S2's results at all. The nearest measurable
analogue is R8.3's maintenance column, and it is not the matrix but the arc-cost VDF sweep: **9.94%**
of a Tick at N=0, plus Sight at N=1 for another **5.64%**, so **15.18% combined before a single route
is computed.** The row's threshold is exceeded by the maintenance it was written to exclude. That does
not weaken the verdict — row 1 fires by 80× on routing alone — but it does mean the row **as written**
was never satisfiable, and nobody would have noticed while it was firing so hard.

#### What this says about writing a wire before the numbers

S4's practice is sound and this spike is not an argument against it. What S2 adds is the failure mode
S4 never met: **a wire written before the numbers can be unfireable, or can name the wrong object, and
neither is visible until somebody tries to score it.** Of seven rows, only **2, 6 and 7** were both
testable as written and tested as written. Rows 1 and 3 needed interpretation at scoring time — which
is exactly the *reasoning around afterwards* the practice exists to prevent, arriving through the back
door. Row 5 needed a harness nobody built.

The cheap repair is not a better wire. It is **scoring the wire early** — at the first round that
touches each row, rather than at the report — because a row that cannot be scored is a row that can
still be rewritten while the spike is running. Row 5 would have been caught at R5.3, the first time a
cache was built with a fixed capacity, and R5 would have cost one extra sweep instead of leaving a
debt no round owns. **This is the general form of the corpus's own repeated finding — citing a rule is
not applying it — pointed at tripwires**, and it is the third instance inside this plan.

### What S2 hands on — the decisions ledger, restated

`plans/0010` opened with a *Decisions owed by this spike* section, written while planning under
`0003`'s rule 6. **Nineteen entries.** R7's job is to say which closed, which S2 answered, and — for
the rest — **who answers now**, because a debt with no owner is how the corpus got the ones it already
has.

| | Entry | State | Who answers |
|---|---|---|---|
| 1 | `Segment` needs a `CONTEXT.md` entry | **closed** — it was S2's gate | — |
| 2 | Matrix refresh cadence is hash-bearing and filed as tuning | open | the cadence cluster, below |
| 2a | The matrix's **time resolution** | open | the cadence cluster |
| 3 | The routing Tick budget share — the 10% | open, **filed unratified in `0002` §D** | it cannot be ratified yet; see R7's tripwire scoring |
| 3a | How a Segment gets its volume | **closed** by `adr/0041` | — |
| 4 | District count, and road density | **half closed.** R1 swept the count; R0 swept density and reports **16.20 km/km²** | density needs **a source, not a sweep** — filed in `0002` §B |
| 4a | The **cost unit** — Q16.16 Ticks against an integer fraction | open | the corpus. **No number in S2 rests on it** |
| 5 | Route cache **key** and **eviction policy** | **ANSWERED by R6** | only `adr/0012`'s typing is owed |
| 5a | The sun arc's **phase widths** | open, and almost certainly hash-bearing | the cadence cluster |
| 6 | `06`'s S2 specification is stale | **closed** session nine, by deletion | — |
| 7 | R0's timing table is owed a re-capture | **half closed** — pinning done, governor not | R7, and it is the last thing owed |
| 8 | The **Commute Budget's granularity** | open | the error cluster, below |
| 9 | Does the matrix carry an Epoch; can a dirty region invalidate it | **answered — it does not, and `02 §6` is unsound** | a correction, not a question |
| 10 | Path source keys on the District | **closed** by `adr/0047` | — |
| 11 | The representative funnel | **closed** by `adr/0047`; R8 moved the axis first | the error cluster |
| 12 | The **maintenance scheme**, and the cadence that chooses it | **half closed** — subtree repair wins at 4.71 ms against 234.74 | the cadence cluster |
| 13 | What a District-granular route may be wrong by | **closed** by `adr/0047` | the error cluster |
| 14 | The **origin-destination distribution** | open, and **unmeasurable here** | Trip generation, `06` **5b** |
| 15 | A District-granular tree concentrates the city onto a skeleton | **closed** by `adr/0047` — it was the fourth ground | see the reconciliation below |

**The rest of this section is the four groups the open entries actually form.** They are not fifteen
questions; they are four, and each has already cost something by being filed as several.

#### The cadence cluster — 2, 2a, 12 and 5a are one argument about one object

The matrix's **refresh cadence**, its **time resolution**, the **maintenance scheme** and the **sun
arc's phase widths** are the same decision seen from four sides, and every one of them was filed as
*tuning*.

They are not tuning. Cadence decides when a changed travel time reaches the choice loop, and two
cadences produce two cities under `05 §4`. Resolution does the same and the corpus has **never named
it at all** — a Day-average matrix and a per-phase one differ on 76 one-way District pairs against 1,
so a Household picking where to live picks differently under each. Phase widths decide how
concentrated demand is, and **no peaking factor exists anywhere in the corpus**, so every load figure
it holds is a Day-average of a Day that has a rush hour.

**And R4 found the sting: a decision filed as tuning turns out to select an algorithm.** R4.6 puts the
incremental-versus-rebuild break-even between **1% and 10% of arcs moved per refresh**, so the cadence
does not merely tune the maintenance scheme — it **chooses** it. Settle these together or one of them
will be settled as a knob.

#### The error cluster — 8, 11 and 13 are one question nobody has asked

*What is a coarse routing answer allowed to be wrong by?* The corpus has no position, and three
entries circle it.

R1 measured the error: a District entry is wrong by **11.32% at the anchor — 6.73 Ticks — p90 14.04,
worst case 77.62.** That is neither obviously acceptable nor obviously fatal, **and it cannot be judged
at all**, because the only thing that consumes it is the Commute Budget and `CONTEXT.md` gives no
number for what a Commute Budget resolves. `01 §7` draws it as a wedge on the sun arc, which is a
granularity of a kind and not a stated one. **6.73 Ticks is free against a Budget read to the nearest
half hour and disqualifying against one read to the minute.**

`adr/0047` closed 11 and 13 by removing the District from routing, which removes *this* instance. It
does not answer the question, and R6's key raises it again in the same shape — R6.1a's **0.86–0.94
Ticks** is an induced error against the same unstated consumer. **Answer it once.**

#### Session M's — and R6.3 put a new question in front of the old one

M owns the **invalidation contract**, which is R6's gate and the reason S2 cannot close. R6.3 has since
added one that must be answered first, because it is the only routing question with a measured
overshoot behind it: **what does a diverting Traveller do about its route?** Re-search costs 861.87% of
the Tick budget at R8's own rung; the cache would need an 88.5% hit rate it has no claim to; and the
third option — **rejoin the Habit Route without re-searching** — is free by construction and appears
nowhere in the corpus.

#### Corrections, which are not questions

- **`02 §6`'s *slow cadence, dirty regions only* is unsound.** A spatial test missed **309 of 429**
  changed entries on a central edit and did so silently, leaving entries stale rather than coarse.
- **The origin-destination family is a placeholder with a named successor.** R4.1 replaced a silent
  uniform draw with a swept family, and **the family is invented** — it measures nothing. No document
  may cite a figure derived from it without naming the rung. The corpus already knows this failure's
  general form: a curve reported as a fact is how the ~400k Trips/Day figure survived.
- **Road density is owed a source, not a sweep.** 16.20 km/km² reproduces the corpus's own
  ~30,000-Segment placeholder and turns out to be one Street on every Cell boundary — so the input no
  longer *"exists nowhere"*. Whether it describes a real city is unchecked, and it is a **denominator**,
  so everything divided by it inherits the error.

### The reconciliation R7 owes, and it is larger than the ledger

**`adr/0047` deleted the structure three of S2's most-quoted results were measured on, and nothing has
said so.**

R8 ran on a **District-granular free-flow tree**. That is what `adr/0047` removed — and it removed it
*using R8's own evidence*, the concentration column: 87.25% of traffic on 1% of the road. The ADR is
right and decision 15 is properly closed. But R8 measured `adr/0046`'s three layers **on that same
tree**, and R8 says so itself, in terms:

> the ~21.64% fire rate is a property of District-granular routing and **must not be carried to any
> scheme that gives a Traveller more than one candidate route.**

`adr/0047` mandates exactly such a scheme — *"a tree holds **one** route to a place; a cache holds
many. Only one of the two can disperse traffic at all."* So the disclaimer fires on the design the ADR
chose, and **three live results inherit it**:

- **R8's diversion fire rate, 14.08% of crossings**, which is the multiplicand behind R6.3's 1,269.51
  diversions per Tick — the number that fires tripwire row 1.
- **R8.5's self-correction result**, which is the named ratifier for **Habit refresh cadence =
  infinite**, recorded as **RATIFIED** in `CLAUDE.md`'s constants table and in `plans/0002` §D. It is
  the first row ever struck from that section.
- **Temperament's 92.28% damping**, measured against a herd the same tree produced.

**What survives, and it is most of it.** Row 1's verdict is untouched: it fires by 80×–239×, and no
plausible movement in the fire rate closes two orders of magnitude. R8.5's ratification is very likely
safe and probably strengthened — more candidate routes means more places for a jam to redistribute to,
so self-correction should close at least as easily — **but that is an argument, and it is not the one
on file.** The ratification currently rests on *R8.5 ran and did not refute*, with no statement that
the run's structure was subsequently deleted.

**What is owed is small and it is not a re-run.** Each of the three needs one sentence saying which
direction the superseded basis pushes it, and `plans/0002` §D's Habit row needs that sentence before it
can keep the word *RATIFIED* — because `adr/0052`'s whole point is that a ratifier is named, and a
ratifier that measured a deleted structure has not been checked, only cited. **This is `adr/0044`'s
closing finding once more — citing is not applying — arriving this time at a ratification rather than
at a decision.**

**The general form is worth more than the three fixes.** `adr/0047` is a *decision-track* act that
invalidated a *measurement-track* basis, which is the second instance in this spike after R6.3 found
the same ADR retiring R3's denominator. The board's model is that the three tracks do not contend.
They do not contend for **files**. **They contend for the ground a measurement stands on, and nothing
in the process looks.**

### What R7 still owes

- ~~**The `performance` capture.**~~ **TAKEN**, 2026-08-09, all six sections — see above. **What
  remains is a second run of it**, because one capture is an assertion and two are an error bar, and
  **R5.6, R6.1, R6.2 and R6.3 are still `powersave`**. Those four publish no timing figure that a
  conclusion rests on, which is why they are last rather than urgent.
- ~~**R2's reconciliation.**~~ **CLOSED** — the two figures were never the same operation, and the
  1.82× move on the one that did shift matches R4's independently measured 1.80× artefact.
- **Re-verify the absolutes R0–R4 publish**, now that a canonical capture exists to verify them
  against. R3 already disclaims its own — *"until it exists no absolute nanosecond figure in this
  section should be quoted outside it"* — and that disclaimer can now be lifted or discharged rather
  than carried.
- **Whether R0's struck `EuclideanFloor` claim stands.** True under canonical conditions; struck for a
  reason that no longer applies. A claim struck for a bad reason can still be true.
- **Row 5 of the tripwire, which no round owns.** Recorded above as unscorable rather than clear. It
  wants a run with a sink and elapsed time in it, and that is Phase 2's.
- ~~**R6 has no closing verdict.**~~ **WRITTEN** — *What R6 decided, and what it did not*, above. It
  was missing because R6 is the round that cannot finish, so the absent verdict and the open gate were
  the same fact; four sub-rounds had nonetheless decided a great deal and nothing collected it. The
  section it produced is `adr/0012`'s owed amendment, stated: the **key**, the **eviction policy**, and
  R5.6's finding that **the two consumers do not want the same mechanism**.
- **R2's reconciliation.** R4 records that its rebuild denominator disagrees with R2's published build
  by 2.2× — 217.36 ms against 474.47 ms for the same 121 backward Dijkstras. **The re-capture does not
  close this**: 217.36 ms reproduces, so the disagreement is not the pinning artefact and R2's figure
  is the one under suspicion.
- **The three results measured on a structure `adr/0047` deleted.** One sentence each on which
  direction the superseded basis pushes them, and **`plans/0002` §D's Habit row needs that sentence
  before it can keep the word *RATIFIED*.** See *The reconciliation R7 owes*.
- **S2 cannot close.** R6's invalidation half is gated on session **M**, which `adr/0043` types
  *arguable* — no measurement settles it — and R6.3 has since put a second question in front of it.
  The harness therefore stays; deleting it is R7's last act and it is not owed yet.

---

## S0a — the world at target size

**S0 turned out to be two spikes filed as one, and only one of them was runnable.**
[`plans/0002`](../plans/0002-open-questions.md) specifies S0 as *"generate a 1M-Citizen city in
`Borough.Headless` and measure the Tick: tables at target size, the Event Wheel, Bin Rules with wait
lists, a Sweep Rule pass, and a routing load."* Three of those four are slices 9, 7 and 10, every one
of them gated on a grilling session. **What is measurable today is the first clause and nothing
else**, and calling that the whole spike would have retired a risk it does not touch. This section is
**S0a — the world at target size**; **S0b — the Tick with work in it** is recorded as not run, and
`plans/0003` now carries both rows.

**The finding that made the spike possible at all is that a run had never had a city in it.** Report
mode built a synthetic city and printed its footprint; run mode allocated capacity and stepped an
**empty world**. `--citizens 1000000 --ticks 512` reported four identical State Hashes and a census of
zeroes, which reads as a stable city and was nothing at all. **Every Tick figure this project holds
was taken over an empty world**, slice 6's 100,000-Tick acceptance run included. Populating now goes
through Phase 0 as `CommandKind.Populate`, so it is in the Input Log and replays by construction —
see *What building it found*, finding 1, for why that shape was chosen over the two cheaper ones.

### The machine, and what is wrong with the capture

Measured 2026-08-07 on the Linux desktop — **Intel i5-10400, 6 cores / 12 threads, 12 MiB L3, 62 GiB
RAM, .NET 10.0.110**, release build, `taskset -c 2,8` (one physical core plus its SMT sibling, which
is S2 R5.3's correction), three repeats per figure.

> **The governor is `powersave`, not `performance`, and this capture is therefore not comparable to
> S4's or S2's absolutes.** Setting it needs root and this session did not have it. **Stamped rather
> than hidden**, per R5.3, which found that a mis-stated machine state had silently reversed a verdict.
> What survives it: every **ratio** below is taken within one machine state, and `powersave`
> *understates* the machine — so each absolute is an **upper bound**, and the one verdict that leans on
> an absolute (the State Hash against the Tick budget) would need the machine to be **2.08× faster**
> under `performance` to change. A canonical re-capture is owed and is cheap.

Repeat spread is negligible and is quoted rather than asserted: the 2,048-Tick hashing run read
**67.30 / 67.37 / 67.23 s** across three captures, giving a per-hash figure of **32.44–32.51 ms**.

### The numbers

At 1,000,000 Citizens — 120,001 Lots, 120,001 Buildings, 360,000 Households.

| | Cost | Share of the 15.6 ms Tick budget |
|---|---|---|
| Table footprint | **85.98 MiB** (12.70 per-Tick, 57.11 wake, 14.46 cold) | — |
| Resident set | **94–101 MiB** | — |
| Build the city, JIT and walk the end-of-run invariants | **0.59 s**, once per run | — |
| **An empty Tick** | **0.112 ms** | **0.72%** |
| **One State Hash** | **32.47 ms** | **208%** |
| **A Tick with the Decide guard on** | **76.4 ms** | **490%** |
| 100,000 Ticks, hashing every 20,000 | **11.75 s** | — |

The empty Tick is a two-point slope over 2,048 / 16,384 / 65,536 Ticks — 0.82 / 2.44 / 7.92 s — which
is linear to three digits and leaves a fixed cost of 0.59 s. It is the phase skeleton, the staggered
invariant tier and the Layer schedule, and nothing else: seven of the eight phases are stubs.

**The footprint scales linearly and 1M is nowhere near a ceiling.** 8.60 MiB of tables at 100k, 85.98
at 1M, 343.91 at 4M, the last of these resident in 283 MiB and built in 0.61 s. `05`'s data layout
holds at the target with an order of magnitude to spare, and **ledger #29b's *rows never move* was
exercised rather than merely asserted** — a 100,000-Tick run over 1.6M rows moved none.

### What building it found

1. **A population must enter through Phase 0, and that decided the design.** `Simulation.ApplyInput`
   states that it is *"the only door into the simulation"* and that a mechanism reaching in from
   outside a Tick *"is a state change no replay can reproduce and no State Hash divergence can
   explain."* Populating from the shell — the cheap option, and the one both existing populators
   already were — would have made replay equivalence a claim somebody has to keep true instead of a
   construction. `CommandKind.Populate` is a verb no player will ever have and it is **expected to be
   deleted** when Zone Rules can grow a city instead of declaring one. It carries **no payload**: the
   size is `WorldConfiguration.Citizens`, which the log already states, and a count on the command as
   well would let one log assert two populations.

2. **The State Hash costs two Tick budgets at the target, and nothing in the corpus said so.**
   3.37 ms at 100k and 32.47 ms at 1M — linear, 9.65× for 10×. For comparison, `05 §9` item 1b records
   that the full-world double buffer was deleted by [`adr/0037`](adr/0037-the-world-is-single-buffered-and-hazards-are-per-table.md)
   for costing *"~150 MB copied against ~1 MB written, 8–15 ms at 1M — 50–100% of the budget at 4×
   speed."* **One State Hash is 2–4× worse than the thing that was deleted for being unaffordable.**
   It is *sampled* rather than per-Tick, so this is not a defect — but it makes `--hash-every` a cost
   decision at scale rather than a free knob, and the golden-baseline and bisection workflows are all
   downstream of it. **A per-Tick hash is not available at 1M**, and no document had noticed.

3. **The Decide guard is `O(world)` per Tick, was on by default, and had no switch in the runner.**
   `Simulation.VerifyDecideWritesNothing` folds every column of every table **twice** per Tick to prove
   Phase 2 wrote nothing. Its own remarks say *"turn it off for the 100,000-Tick test and leave it on
   everywhere else"* — and `Borough.Headless` had no flag that could. Measured at 1M it is **76.4 ms per
   Tick, 95% of the run**: 512 Ticks took **42.66 s** with it and **2.63 s** without, to an identical
   hash. **The guard that justifies deleting the double buffer costs five times what the double buffer
   did.** Fixed with `--no-decide-guard`; the polarity is deliberate, so the correctness check stays the
   default and the fast run is the thing asked for by name.

4. **Moving the populator into `Borough.Core` put it under the arithmetic lints for the first time,
   and it failed them.** `BOR0203` fired three times on raw `/`. Both previous copies lived in
   `Borough.Headless` and `Borough.Tests` — outside the analysers' reach — so thirty lines of
   simulation-shaped arithmetic had been running with `05 §4`'s rounding rule unenforced. **The lint
   boundary is the project boundary**, which is correct and worth knowing: fixture code that will one
   day be measured against is fixture code that should be built like the core.

5. **The populator existed twice and had already drifted.** `Report.Populate` and
   `Borough.Tests.Benchmarks.SyntheticCity.Of` were the same thirty lines except that the test copy
   assigned `Workplace` and the runner's did not — so the footprint report and the invariant benchmarks
   had been describing **two different cities** while both being called the synthetic city. One
   populator now, in `Core`, and both copies are deleted.

6. **The Household table is provisioned to exactly the synthetic population.** `World` sizes it at 360
   per 1,000 Citizens and the fixture creates exactly that, so `live == capacity == 360,000` with zero
   headroom: the first Household the simulation itself ever creates reallocates the table. Lots and
   Buildings are the other way — the fixture builds **120** of each per 1,000 against sizing of 225 and
   150, so those two are over-provisioned by 1.87× and 1.25×. **The fixture and the sizing derivation
   disagree and nothing checks that they agree.** Recorded for slice 7 rather than fixed here, because
   the right ratio is a design question and the fixture is not the place to settle it.

### The verdict

**1M is a spec rather than a hope, for everything S0a can see — and S0a can see less than S0 was
written to.** The tables hold it with an order of magnitude of headroom, the row count is not what
binds, and a 100,000-Tick run at the target completes in 11.75 s with no collection and no magnitude
trending. **What is not answered is the Tick itself**, because seven of its eight phases are empty: the
0.112 ms floor is the cost of a skeleton, not of a simulation. **S0b — the Event Wheel, Bin Rules with
wait lists, a Sweep Rule pass and a routing load — remains unrun and remains the risk `06` names.**
The corpus's instruction *"do not open Phase 2 content until S0 has run"* is discharged by S0a only to
the extent that it was about **sizing**; the part that was about the **Tick budget** is still owed, and
S2 is currently the only spike with a number in that column.
