# Spike results

> Recorded numbers from the spikes. Plans in [`plans/0003-build-plan.md`](../plans/0003-build-plan.md).

[`06-roadmap.md`](06-roadmap.md) is explicit about why this file exists: *the spikes are worthless if
their results are held in the developer's head, because the whole value is being able to re-read them
in a year when a performance question resurfaces.* **Record them; delete the code.**

Each entry states the machine, the numbers, and — separately from the numbers — **the decision they
produced**. A spike that records data and no verdict has not finished.

| Spike | Question | Status |
|---|---|---|
| **S4** | Kernel benchmark — the machine's response to the shapes this design makes | in progress. Task 1 recorded below; K0–K6 not yet run. [`plans/0004`](../plans/0004-s4-kernel-benchmark.md) |
| **S2** | Routing ceiling — travel-time matrix, then HPA\* versus DSDV distance-vector. Also owns Chunk size | not run. **The project's top risk** |
| **S1** | Rendering ceiling — 20k Buildings via chunked `MultiMeshInstance3D` | not run |
| **S3** | UI ceiling — one data panel with a live multi-series graph, and how long it took | not run |
| **S0** | Synthetic 1M-Citizen city in `Borough.Headless` | not run. Gated on the Phase 1 slices |

---

## S4 — the kernel benchmark

**Task 1 of [`plans/0004`](../plans/0004-s4-kernel-benchmark.md) is done: the machine is recorded and
the denominator measured.** Still owed by this section: the recomputed Citizen hot-row size and the
derived target row counts (task 2); per kernel K0–K6 the hand-computed ideal, the achieved figure and
the ratio; K6's p99.9 under each GC configuration; the verdict against the tripwire; and the commit
at which `spikes/S4.Kernels/` was deleted.

Measured 2026-07-31 on **two machines** — a Linux desktop via
`spikes/S4.Kernels/tools/baseline-sweep.sh`, and an Apple M4 Pro laptop via `tools/mac-sample.sh`.
The second exists because a single sample cannot tell a property of the design from a property of
the box it was measured on, and on this evidence it could not: one of the conclusions below reverses
between them. Raw captures are in `spikes/S4.Kernels/results/`, recoverable from the deleting commit
recorded at the end of this section once task 11 runs.

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
once, is never argued, and contradicts [`adr/0021`](adr/0021-terrain-is-generated-and-sparse.md),
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

### What task 2 changed, and what it owes

**Changed:** the Citizen row is two numbers rather than one, and neither is 40. The ownership rule is
settled and `03 §4` invariant 1 needs a wording pass. `Unemployment` becomes derived. The 400k
Household figure is corroborated for the first time. The Trip count is 4.75× the corpus's figure, and
`adr/0037`'s 23,000 is identified as a restatement rather than a second source.

**Owed, and blocking nothing in slice 1:** an employment rate, a mean Trip duration, a
Households-per-Building figure, a workers-per-Business figure, and the Microscopic Cap. Also a
`buildable_fraction` that survives `adr/0021`, and a decision on whether labour is a Household↔Business
relationship or an input Bin — the latter changes the Business row and is already queued for slice 7.
