# S4 handoff — where we are and what to do next

Scratch note, not corpus. Lives inside `spikes/S4.Kernels/`, so task 11 deletes it with
everything else. Nothing in here is a decision; decisions go to `docs/spike-results.md`,
`plans/0002-open-questions.md`, or an ADR.

Written at `2f60637` (S4 task 3), updated after tasks 4–9.

**The findings now live in [`docs/spike-results.md`](../../docs/spike-results.md), which is corpus and
survives task 11.** This note is session state only. If the two disagree, the corpus is right.

---

## Start here — the three things left

**1. A corrected K6 sweep is running and nobody has read it.** It was relaunched detached; it writes
`results/k6-ddr2133-powersave-turbo.md` incrementally, one section per run, ~80 minutes for eight.
Check it completed and that **all four GC labels appear** — `server=off/on` × `concurrent=off/on`. If
only two distinct labels appear, the fix did not take and the sweep is worthless; see below.

The first sweep's results were **discarded, not kept**. It asked for four configurations and ran two,
because `DOTNET_gcServer` overrides `runtimeconfig.json` and `DOTNET_gcConcurrent` does not, and the
csproj was baking `ConcurrentGarbageCollection`. Both faults are fixed and verified. The findings that
did survive it are in `docs/spike-results.md` under K6, marked provisional.

**2. Finish task 10's verdict.** K0–K5 are complete in `docs/spike-results.md`, and K6 is written but
**provisional** — it needs the background-collection-*off* rows folded in, its "what went wrong"
section trimmed to past tense, and the provisional banner removed. Then note in
[`adr/0036`](../../docs/adr/0036-the-cores-language-is-a-separate-decision-and-it-is-csharp.md) that
its K6 trigger has been evaluated — **whichever way it went**, which task 11 requires explicitly.

**3. Then task 11:** delete `spikes/S4.Kernels/`, record the deleting commit's parent in
`docs/spike-results.md`, and confirm `dotnet build` and `dotnet test` are green with the spike gone.

### What K6 already says, and the prediction to test

From the half-matrix that did land — background collection on, both server modes, both arms:

**`adr/0036`'s trigger does not fire.** The unmanaged arm never exceeded the Tick budget once across
1.73M iterations; p99.9 between 2.30 and 2.49 ms against 15.6 ms, worst single iteration 8.48 ms. The
managed-objects arm, same churn, hit **35 ms** on a single gen2 — so the discipline is doing the work
rather than the machine, which is exactly what the second arm exists to show.

**The trigger measures the wrong quantile, and that is the more useful finding.** The managed arm's
p99.9 is 2.32 ms while its max is 35.30 ms; ~11 gen2 events in ~430,000 iterations sit at p99.997, so
p99.9 averages over 430 samples of which 419 are ordinary. **`adr/0036` should restate its trigger as
a maximum or an over-budget count, not a quantile.** Read `max`, the over-budget count and the total
pause — K6 reports all three beside the percentiles for this reason.

**The prediction to test:** background collection *off* should hurt the managed arm materially — a
non-concurrent gen2 is fully blocking, so the 35 ms worst case should get worse — and should barely
move the unmanaged arm, which has almost nothing to mark. If the unmanaged arm's max stays under
15.6 ms with background collection off, the trigger is settled under every configuration the plan
asked for.

**Server GC is a real lever and `05 §6` still states no GC configuration.** It cut the managed arm's
worst case from ~35 ms to ~16 ms and total pause by 61%, while costing the unmanaged arm almost
nothing. The decision that supports is narrow: the shell may want server GC, the core is indifferent —
and the core's indifference is itself evidence the discipline is working.

### Caveats on the K6 capture, both of which are recorded in the file itself

- **`powersave`+turbo, not canonical.** Same as K3 and K4. K1/K2/K5 showed the governor moves no
  ratio, and K6 is about pauses rather than bandwidth, so this matters less here than anywhere — but
  it is not the canonical capture.
- **The churn rate is a guess: ~50 MB/s, one object in sixteen promoted.** It models the shell, the
  UI and the per-frame snapshot, and nothing in the corpus states what that should be. The report
  prints the achieved MB/s so the assumption is arguable; `--churn-kb` changes it. **This is the
  single most challengeable number in K6** and the write-up should say so rather than let a reader
  discover it.

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
| 6 | K3 bulk copy of the K0 footprint | **done** — and it constrains how `Core` allocates its tables |
| 7 | K4 sorted-array lookup, ≤9 entries | **done** — the no-hash-maps rule is free, and then some |
| 8 | K5 wheel bucket drain, 8,192 buckets | **done** — the Wheel has a number for the first time |
| 9 | K6 ten-minute GC tail, p99.9, four GC configs (not a BDN job) | **written and swept; results unread** |
| 10 | The report | K0–K5 written into `docs/spike-results.md`. **K6 and the verdict owed** |
| 11 | Delete `spikes/S4.Kernels/`, record the parent commit | — |

## What can actually be run today

```bash
dotnet build                                      # no GPU, no Godot
dotnet test                                       # 3 boundary tests
dotnet run -c Release --project spikes/S4.Kernels -- k0
dotnet run -c Release --project spikes/S4.Kernels -- baseline
dotnet run -c Release --project spikes/S4.Kernels -- scaling

spikes/S4.Kernels/tools/kernel-run.sh             # K1-K5, pinned and labelled
sudo spikes/S4.Kernels/tools/kernel-run.sh        # ...under the canonical performance+turbo
spikes/S4.Kernels/tools/kernel-run.sh '*K2*'      # one of them; the filter goes in the filename

spikes/S4.Kernels/tools/k6-run.sh                 # K6: 4 GC configs x 2 arms, 10m each = 80m
S4_MINUTES=1 spikes/S4.Kernels/tools/k6-run.sh    # ...as a smoke run, 8 minutes total
```

**`k6-run.sh` launches the binary once per run, so do not rebuild while it is going** — runs 2–8
would execute a different binary than run 1. To compile-check mid-sweep without touching `bin/`:
`dotnet build spikes/S4.Kernels/S4.Kernels.csproj -c Release --artifacts-path /tmp/check`.

The full suite is 26 benchmark cases and takes about eight minutes.

**BenchmarkDotNet will warn that it could not raise the process priority, even under sudo. That is
expected.** The run deliberately drops back to the invoking user, because BenchmarkDotNet builds a
generated project of its own and doing that as root would leave root-owned `obj/` and `bin/` behind
to break every later non-root build. The script takes the scheduling priority as root instead — a
negative nice value survives the privilege drop — so the warning is cosmetic and the thing it warns
about has already been handled.

`kernel-run.sh` is the companion to `baseline-sweep.sh` and exists for one reason: a kernel run
under a different governor than its denominator is a ratio between two machines. It pins to core 2,
puts the governor and the DIMM rate in the label, and writes `results/kernels-<label>.md`.

`HarnessSmoke` is gone — its own doc comment said to delete it when K1 landed.

## The numbers, against `baseline-ddr2133-powersave-turbo.md`

**Every table below is `powersave`+turbo**, because that is the one configuration all five kernels
have been measured under. Denominators used: **25.8 GB/s copy traffic**, **15.6 GB/s read**. GB is 1e9.

`results/kernels-ddr2133-performance-turbo.md` is the canonical capture and covers K1, K2 and K5 —
it was taken before K3 and K4 existed. It confirms the reading below rather than changing it:

| | powersave | performance | ratio under powersave | ratio under performance |
|---|---:|---:|---:|---:|
| K1 `SpanUnchecked` | 1.033 ms | 0.992 ms | 1.00 | 1.00 |
| K1 `SpanChecked` | 1.316 ms | 1.277 ms | 1.27 | **1.29** |
| K1 `PointerChecked` | 1.733 ms | 1.655 ms | 1.68 | **1.67** |
| K2 `SoaScattered` | 28.79 µs | 27.31 µs | 1.00 | 1.00 |
| K2 `AosScattered` | 19.31 µs | 19.17 µs | 0.67 | **0.70** |
| K5 `Revolution` @4096 | 37.00 ns | 35.99 ns | — | — |
| K5 `SequentialFloor` @4096 | 10.99 ns | 10.83 ns | 0.30 | **0.30** |

Absolutes improve 1–5%; **not one ratio moves outside its own error bar.** The governor is not a
variable in any conclusion here, which is what task 1's four-configuration sweep existed to establish.
**K3 and K4 still owe a canonical capture** — K3 especially, since its result is a 14.3 vs 17.7 ms
comparison against an asserted 8–15 ms band and 3% of headroom is not nothing there.

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

### K3 — bulk copy of the K0 footprint, 172.3 MiB

Source and destination are one contiguous block each and the columns are offsets into them, so every
variant moves the same bytes across the same addresses and only the call structure differs.
**Ideal 14.0 ms.**

| Variant | Mean | vs ideal | vs single block |
|---|---:|---:|---:|
| `SingleBlock` — one call | 14.30 ms | 1.02× | 1.00 |
| `Chunked` 32 MiB | 14.72 ms | 1.05× | 1.03 |
| `Chunked` 8 MiB | 14.71 ms | 1.05× | 1.03 |
| `PerColumn` — 104 calls | 17.70 ms | 1.26× | 1.24 |
| `Chunked` 1 MiB | 19.49 ms | 1.39× | 1.36 |
| `Chunked` 64 KiB | 19.48 ms | 1.39× | 1.36 |

**Ledger #29's answer is conditional on a decision slice 4 has not made yet, and that is the result.**
The copy is not one number. It is 14.3 ms if the world is one arena and 17.7 ms if it is copied
column by column, and `adr/0037` asserts an **8–15 ms band**. Only the arena copy is inside it.

The mechanism was measured rather than assumed, which is why `Chunked` exists: a 3.4 ms gap across
~104 calls would have to be 33 µs per call to be call overhead, which is absurd. The chunk sweep
shows a **clean step between 1 MiB and 8 MiB, worth 33%**, and the arithmetic identifies it. At
14.30 ms the copy moves 361 MB — two streams, source read and destination written, at 25.3 GB/s
against the baseline's measured 25.8 GB/s copy traffic. At 19.49 ms it moves ~542 MB: three streams,
because below the threshold the copy stops using non-temporal stores and starts reading the
destination for ownership before overwriting it. `PerColumn` sits between the two because the
columns straddle the threshold — a few are tens of MB, most are one or two.

**So this is a constraint on `Borough.Core`'s allocator, not on its copy.** If each column is its
own allocation the save cannot make one call and pays 24%. If the tables are arena-allocated from
one contiguous region it can, and the saving is free. That is worth knowing before slice 4 writes
the tables rather than after.

It also raises the stakes on the XMP re-sweep. These DIMMs are rated 3200 and are running at 2133;
at their rated speed every row above falls inside `adr/0037`'s band and the constraint softens
considerably. **The band is currently being judged against a machine that is misconfigured.**

### K4 — many lookups into small sorted arrays, 200,000 ResourceMaps, ≤9 entries

2,000 scattered lookups per batch. A row is 81 bytes at a stride of 81, so it always straddles two
cache lines: 2,000 × 2 × 64 B = 256 KiB. **Ideal 16.8 µs.**

| Variant | Mean | ns/lookup | vs ideal | vs interleaved |
|---|---:|---:|---:|---:|
| `KeysThenValuesVector` | 35.66 µs | 17.8 | 2.12× | 0.61 |
| `KeysThenValues` | 55.07 µs | 27.5 | 3.28× | 0.94 |
| `EntryInterleaved` | 58.56 µs | 29.3 | 3.49× | 1.00 |
| `DictionaryLookup` | 109.01 µs | 54.5 | — | 1.86 |

1. **The no-hash-maps rule costs nothing — it pays 86%.** The banned shape is the slowest thing in
   the kernel by a wide margin. The rule was argued on determinism; it turns out not to need the
   argument.
2. **The plan said this would be cache behaviour and it is not — it is branch behaviour.** Moving
   the nine keys to the front of the row, which is the pure layout change, buys 6%. Removing the
   data-dependent branch by comparing all nine keys in one 128-bit operation buys **39%**. The scan
   is short enough that the mispredict, not the line, is what costs.
3. **The current schema shape is the slowest of the three that are allowed.** `WorldSchema`'s
   `bins[9]` is entry-interleaved; keys-then-values within the same 81 bytes is strictly better and
   costs nothing to adopt, because it is the same memory in a different order.

**`EntryInterleaved` at 3.49× is inside the tripwire's ~3–4× band, and that is a judgement the
report has to make rather than one this note should quietly make for it.** The argument for not
firing: the tripwire's remedy is to write the kernel in a second language, the measured cause is
branch misprediction, and branch misprediction is not a property of the language — the C# vector
form recovers it to 2.12× without leaving C#. The argument for firing anyway: the wire was written
before the numbers arrived specifically so it could not be reasoned around afterwards. Both belong
in `docs/spike-results.md`; this note is not the place to settle it.

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
kernel in a second language.

**One row is inside the band: K4's `EntryInterleaved`, at 3.49×.** It is argued above, and the
argument is the report's to accept or reject, not this note's. Everything else is clear — the next
worst is K2's `AosScattered` at 2.30×, and that is a variant the design does not use. Every shape the
design actually commits to is between 1.02× and 1.42× of ideal, except the ResourceMap scan.

The one row that can still fire outright is **K6's p99.9 against 15.6 ms**, and it is unwritten.

## What to do next

**K6 (task 9) is the priority, and it is now the only kernel that can change the language decision.**
It is `adr/0036`'s named revisit trigger, it is not a BenchmarkDotNet job, and it needs K1–K5 to
exist — which they now all do. Hold the K0 heap live, run the kernels in a loop for ten minutes,
histogram the per-iteration time, report **p99.9** under all four `ServerGarbageCollection` ×
`ConcurrentGarbageCollection` configurations. BenchmarkDotNet's warmup-and-discard model is exactly
wrong for it, so it needs its own command in `Program.cs` beside `k0`, not a `[Benchmark]`.

After K6, tasks 10 and 11: write `docs/spike-results.md` and delete `spikes/S4.Kernels/`.

## Open items carried forward

**Measurement owed:**
- **Re-run `sudo spikes/S4.Kernels/tools/kernel-run.sh`** now that K3 and K4 exist. The existing
  canonical capture predates them and covers K1, K2 and K5 only. Ratios will not move — they did not
  move for the first three — but K3's absolutes are judged against a 7 ms band and want the good
  configuration.
- **XMP re-sweep after a reboot**, and K3 has raised its priority: `sudo
  spikes/S4.Kernels/tools/baseline-sweep.sh`, then `sudo tools/kernel-run.sh`. Labels carry the
  configured MT/s automatically, so it cannot overwrite the DDR-2133 results. These DIMMs are rated
  3200 and are running at 2133; `adr/0037`'s save-copy band is currently being judged against a
  misconfigured machine, and at the rated speed the judgement may well reverse.
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
- **New, from K3:** `adr/0037`'s 8–15 ms band for the async save's copy holds only if `Borough.Core`
  arena-allocates its table columns from one contiguous region. Per-column allocation puts the copy
  at 17.7 ms, outside the band. That is a slice-4 constraint and it should be written down before
  slice 4 starts, not discovered by it.
- **New, from K4:** `WorldSchema`'s `bins[9]` is entry-interleaved and keys-then-values is strictly
  better for the same 81 bytes. `05 §3` should say which, since it is the document that owns layout.

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
