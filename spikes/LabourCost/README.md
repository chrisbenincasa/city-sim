# Labour cost instrument

A disposable, engine-independent Release instrument for
[private production and labour](../../plans/private-production-and-labour.md).
It changes no production source, Ruleset, save schema or golden fixture. It is not in the solution.

From the repository root:

```sh
python3 spikes/LabourCost/prepare.py
dotnet build spikes/LabourCost -c Release -m:1 -nr:false
taskset -c 2 env DOTNET_TieredCompilation=0 dotnet spikes/LabourCost/bin/Release/net10.0/LabourCost.dll age 10000 15360 /tmp/labour-aged.borough > plans/evidence/private-production-and-labour/age-10000.jsonl
python3 spikes/LabourCost/capture.py
python3 spikes/LabourCost/summarize.py
python3 spikes/LabourCost/render_report.py
python3 spikes/LabourCost/finalize.py
```

`capture.py` overwrites its named captures. Change its CPU affinity when CPU 2 is unavailable;
record the new machine and compare only captures made under the same conditions. `finalize.py`
compresses the host logs and checkpoint and writes an evidence checksum manifest. No benchmark
should run alongside the test suite or another capture.

## What it measures

`prepare.py` copies `WorkSchedule.cs`, renames its class and inserts one call immediately after
wage accrual. It refuses a changed insertion site. The copy is generated and ignored by Git.
The four modes are the production wage pass, the copy with a no-op deposit, fractional labour
deposits through `World.Deposit`, and the same deposits with four lazy expiry buckets.

The prototype uses independent integer productivity factors (4097 units per Day, tier factor
`100 + tier * 25`, experience premium `min(20, experience / 100)`). These are instrument parameters,
not proposed Ruleset defaults. Per-worker fractions survive division by 2048. Productivity grading
has representative arithmetic, not the final loader or the exact proposed Jobs-key implementation.

An uncapped isolated Bin per live Business uses the existing Money Resource to take the
non-market deposit path. A direct Business-to-Bin handle array supplies lookup. These Bins have
empty wait lists: Rule wake-up and production execution costs are not measured. They are not linked
into the city; no simulation Steps, saves or world invariants are allowed after probe setup.
The expiry case also uses `World.Withdraw` and counts discarded units per Business. Its four
64-bit buckets are contiguous per Bin; a separate 64-bit clock records the last cycle. The cycle
is 32 Ticks, lifetime 128 Ticks, with the usual up-to-one-cycle age quantization.

Each batch restores wages and probe state outside timing. The expiry case starts with eight units
in each age bucket so even short captures include discarding expired stock. At least 64 passes
exercise cycle boundaries. The schedule Tick stays fixed; the expiry clock advances one Tick per
pass. Thus this isolates repeated accrual and deposit costs over a frozen arrangement, not city
throughput or a working-day production demonstration. The observed aged snapshot is unmodified
before probing. Controlled rosters scatter Citizens across live employers and set 0/25/50/100%
AtWork; real illness and schedule checks still decide the reported on-duty count. Tick 1024 is
17:00 under Borough's 05:00 Day boundary. The percentages do **not** mean that fraction is on duty.
These constructed rosters are throughput controls, not legal employment states or balanced cities.

Nine batches alternate mode order. The summary reports the median of paired differences against
the production baseline, their full range, and clone-control overhead. Every mode warms before
measurement, JIT tiering is disabled, and no setup/reporting/GC call is inside the timed interval.
Actual GC collections and allocations during each batch are recorded. One coordinator is pinned
to CPU 2; there are no route workers in the timed passes. Aged-world construction uses one route
worker and the Decide guard off, then runs end invariants before saving.

Self-checks compare every Citizen's wage and wage remainder with the production pass and verify
fractional labour conservation, bucket/level equality and actual expiry across 257 Ticks. Those
checks protect this instrument, not the unimplemented gameplay capability.

## Memory census

The census reads actual column widths, live rows, high-water slots and allocated capacities,
including intrinsic allocator columns. N = 2/4/8/16 measures array allocations for an inline
array of N `long`s plus one `ulong` clock per allocated Bin slot. It separately reports a
64-bit per-Citizen remainder and the live Business count for estimating additional labour Bins.
Uncompressed saved payload follows slot count; heap column payload follows allocated capacity.
Neither includes object headers unless explicitly labeled as measured array allocation.

Loading a checkpoint restores only its saved slots and can change spare capacity. Preserve both
the pre-save and restored censuses. The report also accounts for the next capacity doubling when
labour Bins are added; quoting just `liveBins × bucketWidth` understates that allocation.

No shipped Resource currently declares expiry. Goods and Business Bin counts are sensitivity
inputs for future content, not measured counts of perishable stock. The aged 10,000-Citizen fixture
and generated larger worlds cannot establish a balanced million-Citizen city's Resource mix.
