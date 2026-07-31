# 0004 — Slice 1: S4, the kernel benchmark

> Slice 1 of [`0003-build-plan.md`](0003-build-plan.md). Spike definition in
> [`0002-open-questions.md` §Not a fork — work that must be scheduled](0002-open-questions.md).
> Decision under test: [`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md),
> [`adr/0003`](../docs/adr/0003-deterministic-integer-simulation.md).

**S4 measures the machine's response to the *shapes* this design makes, at target row counts, with
no simulation in it.** Six kernels, a few hundred lines, written once in C#. It is a **tripwire, not
a gate**: the expected answer is *fine*, and a second implementation is written only in the branch
where a kernel says otherwise.

**Risk retired.** Three claims the corpus rests on have no arithmetic behind them. `adr/0036` chose
C# for the core partly on the assertion that GC pauses are manageable once the hot tables are
unmanaged structs — its own named revisit trigger is a K6 p99.9 beyond 15.6 ms. `adr/0003`'s
overflow policy asserts that `checked` inside the fixed-point library is cheap, which it calls *the
only claim here without arithmetic behind it*. And ledger #29's block-copy cost, though the copy
itself is now deleted by `adr/0037`, still sizes the async save and the transform history. S4 settles
all three **before the first line of core exists**, which is the point.

**What it must not become: a benchmark of the simulation.** That is S0, it is expensive, and running
it against two languages is the exact cost this spike exists to avoid.

---

## Gate

**None.** S4 has never had a blocker. It needs no engine, no Ruleset, no design question closed, and
no entity to exist.

## Prerequisites

Slice 0 only, and weakly — S4 needs somewhere to live and a pinned SDK, both of which
[`dev-environment.md`](../docs/dev-environment.md) Track A supplies. It must **not** reference
`Borough.Core`; there is nothing in `Core` yet and the whole point is that there never needs to be.

---

## Where the code lives, and that it dies

```
spikes/S4.Kernels/          console project, added to Borough.sln for convenience
                            references BenchmarkDotNet, references nothing of ours
docs/spike-results.md       the numbers. NEW FILE, created by this slice
```

`06` is explicit that spike code is throwaway and that the value is entirely in the recorded numbers:
*the spikes are worthless if their results are held in the developer's head, because the whole value
is being able to re-read them in a year when a performance question resurfaces.* The last task of
this slice deletes `spikes/S4.Kernels/` and records the deleting commit's parent in
`docs/spike-results.md`, so the code is recoverable and is not in the way.

---

## Tasks

### 1. Machine baseline and harness

Before any kernel means anything, the denominator has to exist. Every kernel below reports against
**measured `memcpy` bandwidth and a hand-computed ideal**, never against a Tick budget — these are
not Ticks.

- Record the machine: CPU model, core count, cache sizes at each level, RAM configuration and
  channel count, OS, kernel, SDK version.
- Measure sustained single-threaded `memcpy` bandwidth and record it. This is the denominator for
  K1, K3 and parts of K2.
- Disable or record turbo and frequency-governor state. A benchmark whose variance is the governor's
  is a benchmark of the governor.
- BenchmarkDotNet for K1–K5. **K6 is not a BenchmarkDotNet job** — it is a ten-minute sustained loop
  with a histogram, and BDN's warmup-and-discard model is exactly wrong for a kernel whose whole
  subject is the tail.

### 2. Derive the row schema and the target row counts — **and recompute the stale figure**

K0 cannot allocate the world without knowing what the world is. This task is a real piece of design
archaeology and it is worth more than it looks.

- Derive the hot and cold column list per entity type from [`CONTEXT.md`](../CONTEXT.md),
  [`03 §2.1`](../docs/03-agent-architecture.md) and [`05 §3`](../docs/05-technical-architecture.md).
  Eight to fifteen types, all known at compile time: Citizen, Household, Building, Business, Lot,
  Segment, Lane, Trip, Shipment, Vehicle.
- **`05 §3`'s "on the order of 40 bytes hot" for a Citizen is admitted stale.** Session five added a
  schooling accumulator, experience, and car ownership, and none is reflected in it. Recompute it
  rather than trusting it — S4's own definition flags this.
- Derive the row counts at target rather than asserting them:
  `target = map_area × mature_density × buildable_fraction`, against the 4096² map. `adr/0003` uses
  ~400k Households against 1M Citizens; check that against the derivation rather than inheriting it.
- **Record both the schema and the counts in `docs/spike-results.md`.** They are inputs to K0, K2,
  K3 and K5, and they will be the starting point for slice 4's real tables.

### 3. K0 — the world's actual footprint

Allocate the whole world at 1M and report the real footprint, per table, hot and cold separately.

*Decides:* the size of everything downstream — the async save's copy, the transform history's
budget, and whether the recomputed Citizen row is closer to 40 bytes or to 80.

### 4. K1 — linear scan and update, `checked` and `unchecked`

Scan-and-update over three struct-of-arrays columns, 1M rows, in two variants.

*Decides:* the throughput ceiling, whether bounds checks elide, and **the cost of `checked`** — the
last unmeasured claim in `adr/0003`'s overflow policy. Report the two variants' ratio explicitly;
that ratio is the whole reason the variant exists.

### 5. K2 — random gather by generational handle

~2,000 handles into 1M rows, three columns each.

*Decides:* **the Event Wheel wake pattern.** This is the memory-bound kernel and the one `05 §6`'s
Factorio rule is about — *parallelise work that is compute-dense and read-only; do not parallelise
work that is memory-bound and pointer-chasing.* If K2 is close to its ideal, the Event Wheel's
sparse-wake premise holds at scale. If it is not, everything sized against the Wheel is mis-sized.

### 6. K3 — bulk copy of the K0 footprint

*Decides:* **ledger #29 directly.** It must be isolated in its own kernel or the number will be
misattributed to the language, which is how the question got asked in the first place. `adr/0037`
deleted the per-Tick copy, but the async save still takes one real copy at save time and this is its
price.

### 7. K4 — many lookups into small sorted arrays

≤ 9 entries per array, matching the `ResourceMap` (`05 §3`, nine Resources under `adr/0031`).

*Decides:* whether the no-hash-maps rule costs anything. This is **cache behaviour, not algorithmic**
— at nine entries the complexity class is irrelevant and only the layout matters.

### 8. K5 — wheel bucket drain and reschedule

Across 8,192 buckets, matching `WHEEL_SIZE`.

*Decides:* random writes across a large structure — **the Wheel's own cost, which nothing in the
corpus has ever sized.** The Wheel is described as the single largest performance lever in the
project and its overhead has never had a number put on it.

### 9. K6 — the GC tail

Hold the whole K0 heap live and run K1–K5 in a loop for **ten minutes**; histogram the per-iteration
time; report **p99.9**, not the median.

*Decides:* `adr/0036`'s named revisit trigger, and it is **the only kernel that can genuinely
surprise and the only one a median hides.** Run it under both GC configurations —
`ServerGarbageCollection` and `ConcurrentGarbageCollection`, each on and off — and report all four.
The GC mode is a lever on this answer and reporting one number for it would understate what is
actually available.

### 10. The report

Write `docs/spike-results.md`. Per kernel: the hand-computed ideal, the achieved figure, the ratio,
and the measured `memcpy` denominator. For K6, the p99.9 per GC configuration. Then the verdict
against the tripwire, stated as a decision and not as data.

### 11. Delete the code

Remove `spikes/S4.Kernels/`, record the parent commit hash in the report, and note in
[`adr/0036`](../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) that its
K6 trigger has been evaluated — whichever way it went.

---

## The tripwire, stated before the numbers arrive

Written here so it cannot be adjusted after seeing the result, which is the only way a tripwire
works.

| Condition | Response |
|---|---|
| Any kernel worse than ~3–4× off its hand-computed ideal | Write **that kernel only** in the second language and compare. Not the suite, not the core |
| **K6 p99.9 exceeds 15.6 ms** — the 4×-speed Tick — with the heap already pure unmanaged structs | This is `adr/0036`'s named trigger. The discipline is holding and the runtime is hitching anyway, and it is the one outcome that genuinely flips the language decision |
| Everything within tolerance | **The language is settled by argument and S4 has confirmed the argument rather than replaced it.** Record and move on |

The expected outcome is the third row. A tripwire beats a gate when the expected answer is *fine*.

---

## Acceptance

- `docs/spike-results.md` exists and contains, for each of K0 through K6, an ideal, an achieved
  figure and a ratio — plus the machine description and the measured `memcpy` bandwidth.
- The recomputed Citizen hot-row size is recorded, alongside the 40-byte figure it replaces.
- The verdict against the tripwire table is written as a sentence, not left to inference.
- `spikes/S4.Kernels/` is deleted and its last commit is recorded.
- `dotnet build` and `dotnet test` are green with the spike gone.

## Decisions owed by this slice

- **The row schema and target row counts** become the first ratified version of numbers that have
  been circulating unratified. They must land in [`0002`](0002-open-questions.md) as such.
- **The GC configuration** is a real choice this slice will surface and nothing in the corpus states
  one. If K6 says server GC and background collection matter, that is a `05 §6` decision, and §6 is
  unargued.

## What this slice deliberately does not do

No `Borough.Core` code. No tables that survive. No simulation, no gameplay, no Rules, no routing, no
city. Every temptation to make S4 "a bit more realistic" is a step toward S0, which costs an order of
magnitude more and answers a different question.
