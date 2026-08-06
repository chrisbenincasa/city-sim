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
| **S2** | Routing ceiling — travel-time matrix, then HPA\* versus DSDV distance-vector. Owns the pathfinding cluster; *informs* Chunk size (`adr/0040`) | in progress. **R0 done** — the graph, the density curve, the denominator and the heuristic verdict. Raw capture in `spikes/S2.Routing/results/`; this section is owed by R7. R1 next. [`plans/0010`](../plans/0010-s2-routing.md) |
| **S1** | Rendering ceiling — 20k Buildings via chunked `MultiMeshInstance3D` | not run |
| **S3** | UI ceiling — one data panel with a live multi-series graph, and how long it took | not run |
| **S0** | Synthetic 1M-Citizen city in `Borough.Headless` | not run. Gated on the Phase 1 slices |

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

**Task R0 of [`plans/0010`](../plans/0010-s2-routing.md) is done: the synthetic Road Graph, the road
density a 268 km² city implies, the uncached point-to-point denominator, and the heuristic ladder's
verdict on admissibility.** R1 — the travel-time matrix, the prescribed first measurement — is next.
Raw capture in `spikes/S2.Routing/results/r0-i5-10400.md`; this section is owed a rewrite by R7, which
also records the deleting commit.

S2 is the project's top risk and the only one argument cannot close. `adr/0020` makes route
computation the binding constraint on world size — *"the map-size question is the routing question in
disguise"* — and until R0 the corpus had never measured any part of it.

### The machine, and a capture defect stated up front

Measured **2026-08-06** on the Linux desktop: Intel i5-10400, Ubuntu 24.04.4, .NET 10.0.10, Release.

> **The capture ran under the `powersave` governor, unpinned, and S4's own protocol says not to.**
> `spikes/S4.Kernels/tools/kernel-run.sh` exists precisely for this: it pins to one physical core with
> the SMT sibling idle, sets `performance`, disables turbo variation and puts the configuration in the
> label, because *a kernel run under a different frequency configuration than its denominator is a
> ratio between two machines rather than a measurement of one.* R0's harness prints its governor —
> which is how this is known rather than assumed — and prints `powersave`.
>
> **What this touches and what it does not.** Every **count** in this section is exact and
> machine-independent: Segment and node counts, footprint bytes, nodes expanded, non-optimal route
> counts, unreachable walk counts. Those are the load-bearing figures and they carry no error bar at
> all. Every **nanosecond** figure carries an unquantified one. The *comparisons* between heuristics
> are the least exposed, being alternating loops within one process minutes apart — the same argument
> that carries S4's *vs variant* columns — but the absolutes are provisional.
>
> **This is the third capture in this corpus to carry a machine-state defect**, after the desktop's
> background load and the M4 Pro's stale denominator, and it is the first one caught by the harness
> before anybody drew a conclusion from it. **The timing table is owed a re-capture under
> `kernel-run.sh`.** Nothing else in this section is.

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
| drive | `None` (Dijkstra) | 8,217 | 294 ns | 779,150 ns | 94 |
| drive | `Manhattan` | 2,813 | 318 ns | 301,887 ns | 107 |
| drive | `Octile` | 3,506 | 327 ns | 395,581 ns | 112 |
| drive | **`Chebyshev`** | **4,121** | **320 ns** | **435,503 ns** | **105** |
| drive | `EuclideanFloor` | 3,712 | 521 ns | 794,940 ns | 214 |
| walk | `None` (Dijkstra) | 276 | 254 ns | 21,754 ns | 78 |
| walk | `Manhattan` | 32 | 288 ns | 4,124 ns | 128 |
| walk | `Octile` | 46 | 263 ns | 5,769 ns | 125 |
| walk | **`Chebyshev`** | **58** | **292 ns** | **7,001 ns** | **120** |
| walk | `EuclideanFloor` | 50 | 391 ns | 11,045 ns | 220 |

**Bootstrap is ~300 ns and it is fixed overhead the design chose.** It is the price of Access Points
being offsets rather than nodes, and `CONTEXT.md` makes that choice structural — promoting them to
nodes would put the graph at 150,000–300,000 edges instead of ~30,000. Against a drive search of
435 µs it is 0.07%, so **the query shape the corpus committed to costs essentially nothing**, which is
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

**`EuclideanFloor` expands 11% fewer nodes than `Chebyshev` and takes 1.8× as long. Against plain
Dijkstra it cuts expansions by 55% and is not faster at all.** Its exact integer square root is a
sixteen-iteration loop, run twice for every node pushed, and it costs more than the expansions it
saves: 214 ns per expansion against `Chebyshev`'s 105.

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
- **The `(Segment, offset)` query shape is free.** ~300 ns of bootstrap against a 435 µs search.
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
