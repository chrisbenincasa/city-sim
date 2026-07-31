# S4 handoff — where we are and what to do next

Scratch note, not corpus. Lives inside `spikes/S4.Kernels/`, so task 11 deletes it with
everything else. Nothing in here is a decision; decisions go to `docs/spike-results.md`,
`plans/0002-open-questions.md`, or an ADR.

Written at `2f60637` (S4 task 3), updated after tasks 4, 5 and 8.

---

## Where slice 1 stands

`plans/0004-s4-kernel-benchmark.md` is an 11-task spike. Tasks 1–5 and 8 are done.

| Task | What | State |
|---|---|---|
| 1 | Machine baseline and harness | done — two machines, four governor×turbo configs, scaling curve |
| 2 | Row schema and target row counts | done — `Kernels/WorldSchema.cs` |
| 3 | K0, the world's footprint | done — settles ledger #29 |
| 4 | K1 linear scan, `checked` vs `unchecked` | **done** — `adr/0003`'s overflow claim now has arithmetic |
| 5 | K2 random gather by generational handle | **done** — the Wheel's sparse-wake premise holds |
| 8 | K5 wheel bucket drain, 8,192 buckets | **done** — the Wheel has a number for the first time |
| 6 | K3 bulk copy of the K0 footprint | not written — cheap, partly duplicates the baseline |
| 7 | K4 sorted-array lookup, ≤9 entries | not written — cheap, low risk |
| 9 | K6 ten-minute GC tail, p99.9, four GC configs (not a BDN job) | not written — **the only one that can surprise** |
| 10 | The report | — |
| 11 | Delete `spikes/S4.Kernels/`, record the parent commit | — |

## What can actually be run today

```bash
dotnet build                                      # no GPU, no Godot
dotnet test                                       # 3 boundary tests
dotnet run -c Release --project spikes/S4.Kernels -- k0
dotnet run -c Release --project spikes/S4.Kernels -- baseline
dotnet run -c Release --project spikes/S4.Kernels -- scaling

spikes/S4.Kernels/tools/kernel-run.sh             # K1, K2, K5, pinned and labelled
sudo spikes/S4.Kernels/tools/kernel-run.sh        # ...under the canonical performance+turbo
spikes/S4.Kernels/tools/kernel-run.sh '*K2*'      # one of them
```

`kernel-run.sh` is the companion to `baseline-sweep.sh` and exists for one reason: a kernel run
under a different governor than its denominator is a ratio between two machines. It pins to core 2,
puts the governor and the DIMM rate in the label, and writes `results/kernels-<label>.md`.

`HarnessSmoke` is gone — its own doc comment said to delete it when K1 landed.

## The numbers, against `baseline-ddr2133-powersave-turbo.md`

**Captured under `powersave`+turbo, not the canonical `performance`+turbo, because the sudo capture
needs a password this session could not supply.** The denominators differ by 3% on copy (12.9 vs
13.3 GB/s) and not at all on read (15.6 vs 15.5 GB/s), so the ratios below are sound and the
absolute figures are ~3% pessimistic. Re-run with `sudo` for the report.

Denominators used: **25.8 GB/s copy traffic**, **15.6 GB/s read**. GB is 1e9.

### K1 — linear scan and update, 1M rows, three SoA columns

Traffic is 16 MB read plus 8 MB written = 24 MB per invocation. **Ideal 0.930 ms.**

| Variant | Mean | vs ideal | vs unchecked |
|---|---:|---:|---:|
| `SpanUnchecked` | 1.033 ms | 1.11× | 1.00 |
| `PointerUnchecked` | 1.025 ms | 1.10× | 0.99 |
| `SpanChecked` | 1.316 ms | 1.42× | 1.27 |
| `PointerCheckedWalked` | 1.313 ms | 1.41× | 1.27 |
| `PointerChecked` | 1.733 ms | 1.86× | 1.68 |

Three findings, and the disassembly was read rather than guessed at. BenchmarkDotNet's artifacts
directory is gitignored, so `kernel-run.sh` copies the `--disasm` export out to
`results/asm-<label>-K1LinearScan.md` — a claim about which instructions the JIT emitted is worth
nothing if the listing it rests on is not in the repository.

1. **Bounds checks elide completely.** `SpanUnchecked`'s hot loop contains no `cmp`/`jae` at all —
   the JIT hoists all three length comparisons above the loop. Span and raw pointer are within 2%.
   `unsafe` buys nothing here, which is worth knowing before slice 4 writes the real tables.
2. **`checked` costs 27%**, and it is the same 27% under spans and under walked pointers. The cause
   is visible: the memory-destination `add [r10],rdi` splits into a load, an `add`, a `jo` and a
   store, and the narrowing cast adds a `cmp`/`jne`. Nothing vectorises in either variant, so this
   is not a lost-vectorisation story.
3. **`PointerChecked`'s extra 41 points are a footgun, not the overflow policy.** `checked` is a
   *block* in C#, so it also covers the address arithmetic: `accumulator[i]` on a raw pointer is
   `i * 8`, and that multiply gets its own `imul`/`jo` per iteration. `PointerCheckedWalked` does
   identical arithmetic with pointer increments instead of indexing and lands exactly on
   `SpanChecked`. **These two numbers must not be recorded as one**, which is why the fifth variant
   exists.

### K2 — random gather by generational handle, 2,000 handles into 1M rows

| Variant | Mean | ns/handle | Ideal | vs ideal | vs SoA scattered |
|---|---:|---:|---:|---:|---:|
| `SoaScattered` | 28.79 µs | 14.39 | 25.2 µs | 1.14× | 1.00 |
| `SoaSorted` | 26.65 µs | 13.33 | 25.2 µs | 1.06× | 0.93 |
| `AosScattered` | 19.31 µs | 9.65 | 8.4 µs | 2.30× | 0.67 |
| `AosSorted` | 19.21 µs | 9.61 | 8.4 µs | 2.29× | 0.67 |
| `SoaSequential` | 2.84 µs | 1.42 | 2.6 µs | 1.09× | 0.10 |

Ideals are bandwidth ideals: SoA touches three columns, so three cache lines per handle
(6,000 × 64 B = 384 KiB); AoS touches one (128 KiB); sequential touches 40 KiB.

1. **The Event Wheel's sparse-wake premise holds, and holds hard.** A scattered gather across a
   20 MB working set runs at **1.14× the machine's streaming read bandwidth** — 4.80 ns per cache
   line, which is DRAM bandwidth, not DRAM latency. The core sustains enough outstanding misses
   (~17) that the gather is bandwidth-bound rather than latency-bound. Nothing sized against the
   Wheel is mis-sized.
2. **Sorting the wake list is not worth doing.** 7%, and it moved between 4% and 7% across three
   runs, so treat it as noise-adjacent rather than as a real win. The corpus has never decided
   whether to keep a bucket's intrusive list ordered by row index; this says it does not matter and
   the question can be closed cheaply.
3. **SoA survives the random gather, which was the real risk.** AoS touches a third of the lines and
   is only 33% faster, because it wins on traffic and loses on parallelism — it is 2.30× its own
   ideal where SoA is 1.14× of its. Interleaving the wake tier into one packed row is *not* worth
   the 33% padding waste and the loss of columnar scans. `05 §3`'s choice stands on measurement now.

### K5 — wheel bucket drain and reschedule, 8,192 buckets, 1,560,000 scheduled entities

Reported per woken entity (`OperationsPerInvoke`), so the figures compare across wake rates.

| Mean wake interval | Wakes/Day | `Revolution` | `SequentialFloor` | Chase penalty |
|---:|---:|---:|---:|---:|
| 4096 Ticks | 2 | 37.00 ns | 10.99 ns | 3.37× |
| 1024 Ticks | 8 | 35.44 ns | 11.11 ns | 3.19× |
| 256 Ticks | 32 | 36.37 ns | 10.87 ns | 3.35× |

The per-wake cost is **flat across a 16× range of wake rates**, which is itself a result: the Wheel's
cost is linear in wakes and the wake rate is the only lever on it.

The floor is the ideal, measured rather than asserted: identical reschedule arithmetic, identical
scattered write into the bucket heads, identical loads — entities visited in index order instead of
by chasing `wheel_next`. The 3.3× is the price of the chase and nothing else. It is a *floor* on the
real penalty, because both variants pay the reschedule hash inside the timed loop.

**The Wheel's own state is ~18.7 MB** — 6.2 MB of `wheel_next` and 12.5 MB of `next_event_tick` —
against a 12 MB L3. It does not fit, and would not at half the population, so this is not an
L3-resident measurement.

**The note's earlier "~190 wakes per Tick" was wrong, and the correction matters more than the
error.** Bucket occupancy is only uniform if the reschedule delay is uniform over the whole ring;
occupancy is in fact triangular. An entity waking every M Ticks is drained 1/M of the time, so the
drain rate is **N/M per Tick, not N/8192**. At M = 4096 that is 381 wakes per Tick; at M = 256 it is
6,094 — a factor of 32 across a range the corpus has never fixed.

## What the Tick actually costs, with K2 and K5 composed

K5 is the Wheel's own cost and K2 is the woken entity's payload gather. They were deliberately kept
apart so neither number could be misattributed; the Tick pays both.

| Wakes/Day | Wakes/Tick | Wheel (K5) | Gather (K2) | Total | of a 15.6 ms Tick |
|---:|---:|---:|---:|---:|---:|
| 2 | 381 | 14.1 µs | 5.5 µs | 19.6 µs | 0.13% |
| 8 | 1,523 | 54.0 µs | 21.9 µs | 75.9 µs | 0.49% |
| 32 | 6,094 | 222 µs | 87.7 µs | 309 µs | **2.0%** |

**K0's conclusion survives its own correction.** The wake rate was under-counted by up to 32×, and
the Tick's wheel-and-wake cost is still under 2% of budget at the most pessimistic wake rate the
design plausibly wants. The Tick is not bandwidth-bound and it is not wheel-bound.

## Against the tripwire

`plans/0004`'s table: *any kernel worse than ~3–4× off its hand-computed ideal* triggers writing that
kernel in a second language. **Nothing is close.** Worst ratio in the suite is K2's `AosScattered` at
2.30×, and that is the variant the design does not use. Every shape the design actually commits to —
columnar scan, columnar gather, intrusive-list drain — is between 1.06× and 1.42× of ideal.

The one row that can still fire is **K6's p99.9 against 15.6 ms**, and it is unwritten.

## What to do next

**K6 (task 9) is the priority, and it is now the only kernel that can change a decision.** It is
`adr/0036`'s named revisit trigger, it is not a BenchmarkDotNet job, and it needs K1/K2/K5 to exist —
which they now do. Hold the K0 heap live, run the three in a loop for ten minutes, histogram the
per-iteration time, report p99.9 under all four `ServerGarbageCollection` × `ConcurrentGarbageCollection`
configurations.

K3 (task 6) and K4 (task 7) are both cheap and neither can surprise. Backfill them alongside K6 or
immediately after; they are half a session together.

## Open items carried forward

**Measurement owed:**
- **Re-run `sudo spikes/S4.Kernels/tools/kernel-run.sh`** under `performance`+turbo, so the kernels
  and their denominator share a configuration. Ratios will not move; absolutes should improve ~3%.
- XMP re-sweep after a reboot: `sudo spikes/S4.Kernels/tools/baseline-sweep.sh`. Labels carry the
  configured MT/s automatically, so it cannot overwrite the DDR-2133 results.
- **K0 on the Mac** — still not run. Thirty seconds:
  ```bash
  dotnet run -c Release --project spikes/S4.Kernels -- k0 --label apple-m4-pro \
    --out spikes/S4.Kernels/results/k0-apple-m4-pro.md
  ```
- K1/K2/K5 on the Mac, once K0 is done there. `kernel-run.sh` is Linux-only (it reads
  `/sys/devices/system/cpu`); on macOS run the binary under `bench --filter '*'` directly.

**Corpus edits owed:**
- `03 §4` invariant 1 wording pass
- `CONTEXT.md` Citizen entry still lists `home`; task 2 moved it to the Household
- `CONTEXT.md` needs an Unemployment definition — "a Household where no Citizen holds a job"
- `CLAUDE.md:130` still says Map is "open" though the ledger closed it
- **New, from K1:** `adr/0003` says `checked` inside the fixed-point library is cheap and calls it
  the only claim there without arithmetic behind it. It now has arithmetic: **27% on a scan that
  does nothing but the multiply**, which is the worst case and is not nothing. Whether that counts
  as "cheap" is a judgement the ADR should make explicitly rather than inherit.
- **New, from K1:** the `checked`-block-scope footgun deserves a sentence wherever the overflow
  policy is stated. Scoping `checked` to a loop body that indexes a raw pointer silently prices the
  address arithmetic; the fix is to walk pointers or use spans, and it is worth 34 points.
- **New, from K5:** the mean wake interval is an unratified input with a 32× range and it drives the
  Wheel's entire cost. It belongs in `plans/0002` alongside the other task-2 guesses.

**Unratified inputs the schema currently guesses at** — all flagged in
`plans/0002-open-questions.md` under "From slice 1, running S4 task 2":
employment rate, mean Trip duration, Households-per-Building, workers-per-Business,
Provider List length, `buildable_fraction`, and the **Microscopic Cap** (still unset; K0 reports it
as a curve, 3.0 MiB at 1,000 segments to 90.3 MiB at 30,000).

**Known-contaminated measurement:** the 4 MiB burst row in the baselines is shared-L3 contention,
not a property of the kernel. L3 is shared by all 12 hardware threads and `taskset` isolates the
core but not the cache. Do not quote the L3-resident rows.

## K0's result, for reference

At 1,000,000 Citizens (a floor, not a cap — counts in `WorldSchema.cs` are per 1,000):

| Table | Rows | Per-Tick | Wake | Cold | Total |
|---|---:|---:|---:|---:|---:|
| Citizens | 1,000,000 | 12.4 MiB | 36.2 MiB | 4.8 MiB | 53.4 MiB |
| Households | 360,000 | 4.1 MiB | 58.0 MiB | 13.0 MiB | 75.2 MiB |
| Buildings | 150,000 | 1.7 MiB | 16.2 MiB | 3.7 MiB | 21.6 MiB |
| Businesses | 50,000 | 586 KiB | 6.0 MiB | 586 KiB | 7.1 MiB |
| Lots | 225,000 | 0 | 3.0 MiB | 4.5 MiB | 7.5 MiB |
| Trips in flight | 56,000 | 1.3 MiB | 1.5 MiB | 55 KiB | 2.9 MiB |
| Legs in flight | 140,000 | 1.6 MiB | 1.7 MiB | 0 | 3.3 MiB |
| Segments | 30,000 | 117 KiB | 615 KiB | 615 KiB | 1.3 MiB |
| **Total** | | **21.8 MiB** | **123.3 MiB** | **27.3 MiB** | **172.3 MiB** |

Working set +174.2 MiB (1.1% over requested), allocated and first-touched in 52 ms.
Headroom 372× on the desktop. At 13.3 GB/s the async save's copy costs **13.6 ms**, inside
`adr/0037`'s asserted 8–15 ms band.
