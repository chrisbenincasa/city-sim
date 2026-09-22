# Combining labour deposits and storing only expiring Bins

Measured 2026-09-22, 11:51–12:00 UTC, on branch base `e25cacc`, following the
[first measurements](../README.md).
The deliverable is a measured prototype, not a production implementation. Core, shipped Rulesets,
save schema and golden fixtures remain unchanged.

## Interpretation of the first measurements

The original million-Citizen, 129,427-worker case added 20.80 ms per wage pass for deposits with
expiry. The target for the entire Tick is 15.6 ms. The baseline wage pass itself was already
59.72 ms, so labour was not the first consumer to exceed that target in this constructed workload.
The conclusion is that a per-worker deposit path has a material marginal cost and that the wider
million-Citizen budget also needs work. Neither measurement is a complete-Tick throughput claim.

Cost per worker rose with city size in the controlled rosters. Extrapolating from the small-world
reading would underprice the large case. These experiments do not collect hardware counters and
cannot specifically attribute that rise to cache misses rather than other working-set effects.

Four buckets plus a clock cost 40 bytes per allocated Bin slot. The original 147.53 MiB scenario
also included a 71.24 MiB increase in existing Bin columns caused by capacity doubling, plus
7.63 MiB of Citizen remainders. Those costs are distinct from expiry storage itself. A compact
expiry representation cannot eliminate the underlying Bin allocator's growth.

Zero timed allocations/GC in the first run means its measured increments were not collection
pauses. It does not prove that all production integration costs have been measured.

## A semantic constraint on combining writes

A probe using the actual `World.Subscribe`, `World.Deposit` and `World.Withdraw` establishes:

| Two waiters each need 6; available quantity changes by 6 then 1 | Woken waiters |
|---|---:|
| Individual writes | 2 |
| One combined write of 7 | 1 |
| Guarded combination | 2, with the same Core State Hash as individual writes |

This holds for both Supply and Space wait lists. `World.Drain` starts from the current quantity
on every call, and waking a Rule does not consume its requirement immediately. Therefore write
granularity affects admission. This is existing behaviour, not a new defect diagnosed here.

The candidate combines only while **both wait lists are empty**; otherwise it applies that
worker's contribution immediately through the existing Bin door. Nonempty queues therefore
preserve individual write order. Pending sums use a bounded per-Business array plus a first-touch
list, flushed before the wage pass returns. Each worker keeps their own fractional remainder.
No daily deposit, changed attendance predicate or changed productivity rule is introduced.

This relies on labour being non-market and the accrual pass being serial: nothing can subscribe
a Rule to an initially empty labour Bin between its first accumulated contribution and the flush.
Rules awakened by an individual write arm for Tick + 1. Neither deferred Rule execution nor the
shuffle alone makes unguarded aggregation equivalent—the admission set already differs.

The timed proxy Bins have empty queues. These are measurements of the branch where combining is
possible. Supply/Space probes validate the fallback separately; a real production world's frequency
of nonempty queues and the cost of actual Rule wakes are not measured here. Phase 2 live-level
reads, Rule execution, hashing and save work are also outside the timed pass.

## Candidate storage and validation

Dense storage allocates four `long` buckets plus one `ulong` clock per allocated Bin slot.
The compact alternative uses real `Rows<Expiry>` columns:

- 16 bytes of allocator identity, generation and free-list state.
- An 8-byte saved handle identifying the Bin.
- 32 bytes of buckets and an 8-byte clock.
- A separate derived 4-byte Bin-to-expiry-row index per allocated Bin slot.

Thus its row is **64 bytes**, not the 40-byte bucket payload alone. The table starts at eight
slots and uses the existing doubling allocator. No row is allocated for a non-expiring Bin.
The saved Bin handle makes the index reconstructible. Self-checks exercise rebuilding, removal
and slot reuse, and reject a stale reverse mapping. Registration with the actual World's table
list, save/reload and Ruleset migration are future integration work.

All four deposit/storage variants are compared over eleven Tick positions, including bucket
boundaries and an untouched interval longer than the shelf life. They must produce identical
Core State Hashes, wages, wage remainders, individual labour remainders, Bin levels, clocks,
buckets and discarded quantities. The prototype's extra state is compared separately because
its table is not registered in World. Timed setup and verification are outside the capture window.

## Method

Release, .NET 10.0.12, Intel i5-10400, one thread pinned to logical CPU 2, JIT tiering disabled.
The governor and turbo are left unchanged. Each process records host CPU load and sampled
frequency; the machine is not reserved. Across full processes, the SMT sibling averaged
1.0–4.6% busy, other CPUs averaged 3.2–4.9% busy, and sampled frequency ranged from 0.8 to
4.11 GHz. These summaries include setup and are not function-level measurements. These remain desktop diagnostics, not quiet-machine
certification. Do not subtract today's candidates from the earlier report's baseline: compare
the fresh paired modes within each capture.

Seven batches of 32 passes alternate mode order across original wage-only baseline,
individual+dense, guarded-combined+dense, individual+compact and guarded-combined+compact.
Each variant warms before capture. A flush and all queue checks are inside timing. Wages and
prototype state reset outside timing. Each batch seeds eight units in each age bucket; a
32-Tick cycle ensures expiration during the capture. Schedule Tick stays frozen while the
expiry clock advances, so these remain isolated accrual-pass costs, not complete simulation Ticks.

The aged 10,000-Citizen checkpoint uses its observed roster. Generated 100,000- and million-Citizen
worlds set 50% AtWork, scattering employers exactly as in the first experiment; actual schedule
and illness checks determine the reported on-duty workers. Generated worlds are not aged or
claimed to be balanced. Seed 1 supplies an independent million-Citizen control.

The high rate is 4097 units/Day before the same prototype grading as the first instrument;
every eligible worker contributes whole units each pass. A 16-unit/Day control retains individual
fractional accrual but emits whole units less often. Initial worker remainders are spread
uniformly by `(citizenSlot × 37) % 2048`, avoiding a synchronized all-zero interval at low rates.
These rates are measurement parameters, not selected Ruleset defaults. CPU arithmetic remains
integer; wall-clock statistics use the instrument's floating-point reporting.

## Results

Each increment below is the median paired difference from that capture’s wage-only baseline.
Medians of different paired differences need not add or subtract exactly.

| Capture | On-duty workers | Wage-only ms | Individual+dense extra ms | Combined+dense extra ms | Individual+compact extra ms | Combined+compact extra ms |
|---|---:|---:|---:|---:|---:|---:|
| 100000-seed0-high | 13,015 | 5.130 | 1.056 | 0.786 | 0.992 | 0.856 |
| 1000000-seed0-high | 129,427 | 59.290 | 18.530 | 11.454 | 22.010 | 13.352 |
| 1000000-seed0-low | 129,427 | 58.055 | 2.883 | 2.964 | 2.957 | 2.904 |
| 1000000-seed1-high | 130,242 | 58.478 | 19.538 | 13.569 | 22.861 | 13.816 |
| aged-10000 | 1,974 | 0.286 | 0.095 | 0.061 | 0.101 | 0.064 |

The combined+compact candidate compared directly with individual+dense:

| Capture | Paired change, ms | Full paired range, ms |
|---|---:|---:|
| 100000-seed0-high | -0.175 | -0.461 to +0.195 |
| 1000000-seed0-high | -5.533 | -7.129 to -2.626 |
| 1000000-seed0-low | -0.103 | -2.113 to +1.276 |
| 1000000-seed1-high | -6.171 | -9.646 to -4.616 |
| aged-10000 | -0.030 | -0.035 to -0.027 |

Negative is faster. These ranges describe seven observed pairs, not confidence intervals.
All **175 timed batches** reported **zero allocations and zero GC collections**.

Measured compact-table payload, after adding one labour Bin per Business:

| Capture | Dense expiry MiB | Compact expiry + index MiB | Combining scratch MiB |
|---|---:|---:|---:|
| 100000-seed0-high | 6.866 | 1.687 | 0.172 |
| 1000000-seed0-high | 68.665 | 14.866 | 1.717 |
| 1000000-seed1-high | 68.665 | 14.866 | 1.717 |
| aged-10000 | 0.686 | 0.131 | 0.010 |

For seed 0 at one million, compact expiry uses 131,072 allocated rows for 84,670 live
labour Bins: 8 MiB of row payload plus 6.87 MiB for the Bin index. Dense expiry costs
68.66 MiB over 1,800,000 allocated Bin slots. That is **53.80 MiB less expiry storage**
(78.35%); combining needs a separate **1.72 MiB** of scratch.
Business-to-Bin handles and waste counters add 1.14 MiB each in either prototype.
The 71.24 MiB of existing Bin-column growth and 7.63 MiB of Citizen remainders from the
original scenario remain. This is not a 78% reduction in total city memory.

Calculated content sensitivity for that same seed and allocation policy:

| Existing Goods Bins also expiring | Expiring Bins including labour | Compact capacity | Compact expiry + index MiB | Dense expiry MiB |
|---|---:|---:|---:|---:|
| 0% | 84,670 | 131,072 | 14.866 | 68.665 |
| 25% | 196,401 | 262,144 | 22.866 | 68.665 |
| 50% | 308,133 | 524,288 | 38.866 | 68.665 |
| 100% | 531,596 | 1,048,576 | 70.866 | 68.665 |

These content mixes are calculations, not new observed worlds. At 100%, the compact table
crosses another capacity boundary and costs slightly more than dense storage. A compact
representation is justified for sparse expiry, not automatically for every future Goods mix.

Actual potential deposit calls over each 32-pass capture, calculated outside timing from
the same roster, rates and initial fractions (all timed proxy queues are empty):

| Capture | Individual calls | Combined calls |
|---|---:|---:|
| 100000-seed0-high | 416,480 | 183,232 |
| 1000000-seed0-high | 4,141,664 | 1,804,896 |
| 1000000-seed0-low | 40,519 | 40,519 |
| 1000000-seed1-high | 4,167,744 | 1,813,888 |
| aged-10000 | 63,168 | 20,128 |

## Interpretation and recommended next step

**Guarded combining earns its place at the high rate.** It saves 6.71 ms (seed 0) and 6.19 ms
(seed 1) against individual+dense in fresh paired comparisons; every high-rate million-Citizen
pair improves. It removes repeated Bin work, not the Citizen scan or individual grading/fractions.
For seed 0, 129,427 individual calls become 56,403 calls each pass, about 56% fewer. The complete
accrual pass only falls from roughly 78 to 70–72 ms because the existing wage work still dominates.
The 100,000-Citizen result has mixed-sign paired changes, so it does not establish a reliable
speed-up at that size. The aged small-world improvement is consistent but only about 0.03 ms.

**Compact storage is a memory trade-off with measurable indexing cost.** At the high rate,
individual+compact is 3.48 ms slower than individual+dense on seed 0 and 2.51 ms slower on seed 1.
Combining more than recovers that penalty in these empty-queue captures: combined+compact saves
5.53 / 6.17 ms against individual+dense, leaving increments of 13.35 / 13.82 ms over wage-only.
The 53.80 MiB expiry saving is useful for labour-only expiry. It neither erases the Bin allocator's
71.24 MiB growth nor proves compact storage is best when almost every Good expires.

**The authored rate matters.** At 16 units/Day the increment is around 2.9 ms for all variants.
Individual and combined paths both make exactly 40,519 whole-unit writes over 32 passes in this
fixture: there are no same-Business contributions to merge within a Tick. The combined+compact
paired difference is -0.103 ms with a range of -2.113 to +1.276 ms, so no reliable speed-up is
established. Individual fractional accounting still runs each Tick. A lower rate also changes
labour quantity and time granularity; no recipes were rescaled or demonstrated, so this is not a
recommendation to lower the rate simply to improve a benchmark.

**Recommended next step:** use compact expiry storage and guarded per-Business combining as the
candidates for the integrated labour/recipe slice. Preserve immediate individual writes whenever
either queue has waiters. Exercise actual recipe-generated waiters, Rule wakes, expiry, fractional
progress, save/reload and replay while measuring how often the combining path is usable. Choose
work units and bucket precision against the production behaviour being authored. Revisit dense
storage or capacity sizing when the expiring-Goods mix crosses the measured allocation boundary.

The research step is complete; production integration and the existing plan's acceptance checks
remain. These component gains do not establish the million-Citizen whole-Tick target. The observed
10,000-Citizen combined+compact increment is 0.064 ms, so the measured cost is small in that founding-
scale fixture while the large-city budget remains a separate performance concern.

## Reproduction and evidence

From the repository root:

```sh
python3 spikes/LabourCost/prepare.py
dotnet build spikes/LabourCost -c Release --no-restore --nologo -m:1 -nr:false
python3 spikes/LabourCost/capture_followup.py
python3 spikes/LabourCost/summarize_followup.py
python3 spikes/LabourCost/render_followup.py
python3 spikes/LabourCost/finalize.py combined
```

The capture script unpacks the first report's checked-in checkpoint; it does not regenerate or
replace the original measurements. `machine.json` identifies source fingerprints, runtime,
Ruleset and checkpoint. Each JSONL retains all paired timings, allocations, GC counters, counts,
memory widths, semantic probes and equivalence checks. Host logs retain frequency and per-CPU
load over the whole process including setup. `summary.json` contains paired differences and full
ranges, which are not confidence intervals. Storage sensitivity rows are calculated from measured
widths/counts and allocator policy; they are not observations of future perishable content.

## Validation

The Release spike build passed with zero warnings and errors. The working test lane passed
all 3,969 tests (zero failures or skips); see `validation.txt`. Whitespace verification passed
for the prototype C# files; the formatter emitted a workspace-loading warning. All five
capture equivalence checks and ten Supply/Space guarded-wake comparisons passed, including
compact-index rebuild and slot-reuse checks. All 175 timed batches reported zero allocations
and GC collections. Source and checkpoint fingerprints were checked against the captured
metadata. Production save/reload of the candidate table remains an integration check.
