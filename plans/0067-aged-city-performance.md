# 0067 — Aged-city performance

Investigate the expensive Ticks in `ProfileDump` with `stress-shopping.toml`, then fix the largest
measured cost without changing the city. Earlier `minimal.toml` readings do not establish capacity.

## Sequence and gates

1. **Attribute** — Release, M4 Pro, one simulation thread, seed 0, 100,000 Citizens;
   age 14,336 Ticks and capture the next 2,048. Keep untraced timings separate from managed
   stack samples. Preserve commands, assembly/Ruleset fingerprints, activity, trace and State Hash.
   Deliver a ranked list of functions, distinguishing inclusive from self samples and separating
   `Simulation.Step` from reporting. Stack samples are not CPU utilization measurements.
2. **Count** — instrument only the leading consumers: route requests and visited nodes;
   shopping candidates/retries; active Travellers; periodic scans or allocations, as indicated.
   Correlate work with expensive Ticks. A hypothesis must predict which count or cost changes.
3. **Control** — compare 1,000/10,000/100,000/1,000,000 Citizens, then vary population at fixed network,
   network at fixed population, and congestion at fixed population/network. Preserve the
   original fixture as a control. Record actual activity and rejected/failed journeys.
4. **Optimize** — choose the largest supported cost. First add a regression at its real call
   boundary, then fix it. Require unchanged State Hashes and activity on identical Inputs.
   Re-run untraced mean/p95/p99/max and allocations, a second seed, and relevant assertions.
   Retain an optimization only with a measured benefit and no unexplained regression.
5. **Broaden** — add functioning care/school and other implemented systems with activity checks;
   separate a functioning economy from distress. Run an older horizon and watch the city in Godot.
   Update `0013` only for costs actually isolated; no million-Citizen extrapolation from this fixture.

## Outstanding work

Work in the priority order below; item numbers remain stable identifiers. Apply step 4's
retention gate to every optimization. Establish repeatability, attribute and count before selecting
a candidate. Only then add a real-boundary regression and one bounded implementation, checking
untraced timings early. Stop candidates without a clear benefit before refining them. Rejection
does not close the targeted cost; inconclusive timings do not establish a regression.

| Priority | Item | Open work | Completion evidence |
|---|---|---|---|
| 1 | 11 | Sampled payroll boundaries below lower the priority of replacing the contiguous scan. Shift-length reuse failed the repeatability gate below; establish stable paired controls before more payroll candidates. | Measured baseline variation, attributed costs and a bounded savings opportunity justify one candidate or lowering payroll's priority. Any subsequent optimization must preserve per-Tick wages, departure order, activity and hashes, pass schedule invalidation/save-reload, and show two-seed Step benefit. |
| 2 | 12 | Reduce unsuccessful Shopping discovery work at its source; sale-Bin indexing and row-total reuse are rejected below. Investigate a larger measured source of unsuccessful discovery work. | Preserve sampling/provider order for an optimization. Changing eligible candidates requires a separate behavior decision; it is not authorized by this row. |
| 3 | 1 | Target remaining redundant routing work. Commute workers, cross-Tick reuse and directed routing have findings below; broader parallelism remains untested. | Name the avoidable work first; require two-seed benefit, matching activity/hashes and worker equivalence. Existing probes do not justify a general exact cache. |
| 4 | 8 | Finish scene preparation and independent view snapshots. Dedicated Step thread and background startup/regeneration are implemented. | Driven frame/responsiveness evidence at recorded population, age, speed and view; headless throughput alone is insufficient. Promote this row if responsiveness becomes the priority. |
| 5 | 6 | Broaden functioning care, school and other systems, including a functioning economy alongside distress. | Activity assertions and attributed costs for systems actually running. |
| 6 | 5 | Extend population/network/congestion controls to one million Citizens and another seed. | Separate multiplicands; preserve activity, failures and hashes. |
| 7 | 7 | Measure older horizons and weekly workloads, including sustained memory growth. | Comparable Days distinguish schedule, ageing and recurring allocation effects. |
| 8 | 4 | Revisit noise queries only after resolving the rejected candidate's seed-1 mean regression. | Controlled untraced benefit; isolated Layers improvement is insufficient. |
| 9 | 9 | Take quiet-machine reference captures; update `0013` only for isolated consumers. | Machine/thread/guard conditions and reproducible evidence; desktop diagnostics do not ratify budgets. |

Completed: row-prefix optimization and initial population ladder; item 2, restoration headroom
(two-seed and million-Citizen controls below); item 3, Shopping attribution and discovery counts
(not elimination of discovery cost); item 10, durable evidence with verified manifests.

### Next investigation — before another payroll implementation

The payroll sitting selected work from M4 attribution, tried implementations on the i5 before
adding payroll counts, and compared individual captures without CPU-frequency or external-load
control. That did not complete the attribute → count → control sequence. The retention gate
correctly refused the candidates, but the measurements do not explain their outcomes.

1. **Establish repeatability.** Repeat the unchanged Release build from one checkpoint, with the
   same warmup, measurement window, seed, worker count and guard setting. Control CPU affinity,
   frequency policy and competing workloads; record the conditions and exceptions. Preserve
   mean/tail variation, allocations, activity and hashes before judging any improvement. If the
   baseline is unstable, resolve that before changing implementation.
2. **Attribute on this machine.** Capture fresh i5 attribution within aged-city Steps. Separate
   payroll and commute costs into scanning, schedule calculations and routing; distinguish
   inclusive and self costs. Collect work counts before choosing a candidate. Keep intrusive
   diagnostics separate from untraced throughput readings.
3. **Bound the opportunity.** Identify the avoidable work and estimate the maximum Step saving
   from eliminating it, including the proposed replacement's maintenance and memory costs.
   Implement one candidate only when its expected benefit clearly exceeds measured variation.
   Use repeated paired comparisons with reversed order, then apply the existing two-seed
   retention gate. Classify results as improvement, regression or inconclusive.

This investigation ends with a supported optimization target or evidence that payroll deserves
lower priority. Another speculative payroll rewrite is not its deliverable. Fewer visits alone
do not establish a saving: contiguous scanning can outperform fewer scattered accesses.

### Threading decision gate

Evaluate threading after Move and allocation attribution (items 1–2); do not require exhausting
every possible serial micro-optimization first. Begin a bounded parallelism experiment when a
representative aged workload repeatedly misses the target on the named reference hardware, the
remaining cost is attributed, and independent work accounts for enough of it to matter.

The existing target is 15.6 ms per Tick at one million Citizens on one core of the reference class
([`0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md),
[`0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)).
Current M4 Pro desktop diagnostics justify investigating the gap, not redefining that contract.
Any multicore wall-clock claim must name the hardware and worker count explicitly.

Before committing to threading:

- Bound the opportunity: serial cost plus parallel work divided by worker count, plus scheduling,
  synchronization and memory costs. If the serial remainder already exceeds the target, threading
  that candidate cannot suffice. Measure scaling; do not infer it from CPU count.
- Prototype the dominant suitable consumer. Route work is a candidate; Layers and Decide alone
  cannot address a workload dominated by Move. Prove independence against the state actually read
  by current queries, including ordering-sensitive traffic and availability.
- Require end-to-end mean/tail improvement after overhead, with acceptable allocation and memory
  on representative workloads. Separate CPU work from waiting, GC and bandwidth limits.
- Add thread-count equivalence, including one versus eight threads, alongside replay/save-reload
  and activity checks. Worker completion order must not affect the city. Delaying route results to
  another Tick is not automatically hash-preserving.

Moving serial `Step` off the render thread is a separate shell responsiveness decision; it does
not by itself increase simulation throughput. `docs/05` §6 provides candidate build order, not a
measurement that the current route implementation is ready for workers.

## Initial hypotheses

Ranked before attribution: routing grows with search work; shopping repeats unsuccessful work;
congestion grows the active movement population; periodic scans/GC produce tail costs.
Changing one multiplicand must change its predicted cost before it becomes the explanation.

## Capture controls

The reference timing capture runs without other workloads. Record exceptions; local diagnostics do
not ratify a budget. Exclude ageing, reporting and end invariants from Tick timings, but keep
staggered invariants active. Explicitly disable the Decide-write guard to match the game. Profiling
must preserve the State Hash. Do not hide throughput costs by moving work onto another thread.

`stress-shopping.toml` has synthetic supply and declining successful purchases at the older horizon;
it is an aged stress fixture, not a balanced mature economy. Geometry and traffic distributions grow
with population, so the current ladder does not identify an algorithm's complexity by itself.

## Progress

- First daily attribution capture complete. `WalkScratch.Search`/`PopRoot` and
  `BuildingResidency.NthIn` lead; `ShoppingEngine.Step` is their main calling path.
  This is aggregate attribution, not a diagnosis of individual tail Ticks.
- Traced and untraced 100,000-Citizen runs from Tick 14,336 through 16,384 produced identical
  workload/allocation counters and State Hash `D551BD2FA385894A`. Local artifacts are under
  `/tmp/borough-attribution-steady`; the reproduction command below is the durable capture path.
- Counting and controlled fixtures complete. The row-prefix optimization preserves candidate order
  and city activity; validation and comparison results are below. Tick 14,608's additional cost is
  in the Layers phase. Noise distance queries are attributed, but the candidate optimization was
  backed out after a second-seed mean regression. Million-Citizen captures are below.
  Bounded commute workers and restoration headroom are measured below. **Next:** production viability, destination-directed routing and broader activity controls.
- `ProfileDump.TimedStep` preserves a stack frame despite JIT inlining. The analysis excludes
  the ending timestamp and reports managed leaf weights, including unresolved native time.

```sh
dotnet tool install dotnet-trace --tool-path /tmp/borough-profiler --version 10.0.731102
dotnet build src/Borough.Headless -c Release
python3 scripts/profile-simulation.py --trace-tool /tmp/borough-profiler/dotnet-trace \
  --output /tmp/borough-attribution
```

The output directory must be empty. `step.json`/`step.txt` filter the Speedscope trace to the
simulation boundary; the tool's unfiltered reports also include waiting and end-of-run checks.

## Work counts — 2026-09-07

`--profile-work` enables caller-owned `ShoppingWork`, `BuildingQueryWork` and `RouteWork` counters.
`ProfileDump.Measure` emits one work record per Tick outside the timed Step. Route requests include
shortcuts; searches count actual scratch initializations. `Settled` counts nodes, `Pops` includes
stale heap entries, and `Pushes` counts successful relaxations including seeds. Recorded-route
counters cover `TripEngine`'s Leg queries, separated by Shopping purpose; they are not every routing
consumer in the simulation. Candidate counters cover shopping discovery only.

Release .NET 10.0.2, M4 Pro, one simulation thread, seed 0, `stress-shopping.toml`, age 14,336,
measure 2,048 Ticks; Decide guard off, staggered invariants on. Chrome/iTerm desktop activity remained
present: **local diagnostics, not a quiet-machine budget reading**. Commands, assembly fingerprints,
per-Tick logs and summaries are in `/tmp/borough-work-20260907`.

| Citizens | Shopping estimate searches | Settled / estimate search | Settled / recorded Shopping search | Candidate calls | Cells / candidate |
|---:|---:|---:|---:|---:|---:|
| 1,000 | 3,677 | 7.71 | 6.14 | 28,937 | 126.53 |
| 10,000 | 21,689 | 42.83 | 38.89 | 683,200 | 324.82 |
| 100,000 | 122,080 | 402.80 | 309.01 | 10,289,995 | 1,112.09 |

These worlds also change geometry and activity. The ladder does **not** isolate population scaling.
Completed Trips were 4,094 / 41,680 / 416,610; purchases 63 / 2,414 / 2,448. All three completed end
invariants with zero no-route and over-budget Trip counts. Declining purchases per Citizen remain
part of the fixture's distress, not evidence of a functioning mature economy.

At 100,000 Citizens, the slowest 1% of counted Ticks averaged 403 settled nodes per shopping
estimate search versus 403 overall, and 301 per recorded Shopping search versus 309 overall.
They contained more searches, rather than larger average searches. Candidate selection visited
11.44 billion Cells over the measured Day but followed only 3.12 Building links per call on average.
Heap pops exceeded settled nodes by less than 0.02% in each measured route category. These counts
support investigating search request volume and Cell scanning; they do not establish identical
repeated queries, cache hit rates or an optimization's benefit.

Tick 14,608 remained the maximum at every population. At 100,000 it took 53.13 ms instrumented,
against 32.78 and 32.54 ms for its neighbours, with comparable total settled-node counts and zero
allocation on all three. It coincides with the land-value cadence; that is a lead, not attribution.
Daily stack attribution and the cause of one maximum are still different questions.

The 100,000-Citizen uncounted/counted runs matched all activity and allocation counters and State
Hash `D551BD2FA385894A`, also matching the prior capture. Uncounted mean/p95/p99/max were
11.68 / 24.03 / 28.96 / 48.20 ms; counted were 15.13 / 27.66 / 33.72 / 53.13 ms under the desktop
conditions above. **Counter instrumentation is materially intrusive.** JSON emission is outside
Step but can affect later GC and process scheduling; counted timings cannot replace the baseline.

Reproduce the counted capture after the Release build; omit `--profile-work` for its control:

```sh
dotnet src/Borough.Headless/bin/Release/net10.0/Borough.Headless.dll \
  --profile --profile-work --ruleset rulesets/stress-shopping.toml --citizens 100000 \
  --seed 0 --warmup-ticks 14336 --ticks 2048 --no-decide-guard > /tmp/borough-work.jsonl
python3 scripts/summarize-work-profile.py /tmp/borough-work.jsonl --output /tmp/borough-work-summary.json
```

Focused assertions covered shortcut counter resets, Cell scans including empty Cells, and profiling
through shopping without changing the city. The spatial-index change followed in the section below.

## Controlled fixtures and candidate-selection change — 2026-09-07

`--profile-population` separates the initial population from `--citizens`, which retains its network
and table sizing. `SyntheticCity.PeopleInto` accepts the smaller population; the default path is
unchanged. Conditions now include a fingerprint of node coordinates, Segment endpoints, modes,
lengths, speeds and capacities. This verifies the fixed network, rather than inferring it from a flag.

The controlled runs retain seed 0, the original Ruleset and the 14,336 + 2,048 Tick horizon. At
10,000 Citizens, increasing the network from 453 nodes / 758 Segments to 2,829 nodes / 5,514 Segments
increased settled nodes per shopping estimate from 42.83 to 68.11. Candidate calls were nearly
unchanged (683,200 / 680,935), but Cells inspected per candidate rose from 324.82 to 1,083.58.
At 100,000 Citizens on the identical larger network, those readings were 402.80 nodes per estimate,
10,289,995 candidate calls and 1,112.09 Cells per candidate. More Citizens also change occupied
geography and journey distributions; the fixed network alone does not hold those constant.

A separate copy changes only traffic `alpha_percent` from 15 to 0. On the identical 100,000-Citizen
network, peak Travellers fell from 42,763 to 11,443, while completed Trips rose from 416,610 to
575,887 and recorded Shopping searches from 356,608 to 706,254. Over-budget outcomes rose from
0 to 2,533, with no no-route outcomes in either run; purchases were 2,448 / 2,463. Faster journeys
permit more work, so active movement population alone does not predict cost. The over-budget
outcomes have not been attributed to a specific refusal path.

The first optimization targets the spatial cost these controls isolate. `BuildingResidency.PrefixRow`
builds row prefixes lazily; `Add`, `Remove` and `Rebuild` invalidate them. `CountIn` sums row ranges,
and `NthIn` skips whole rows and finds the selected Cell by binary search. Cell order and the
intrusive Building list order remain unchanged. Storage is bounded by the map dimensions. Work
records distinguish prefix reads and Cells read during rebuilding from the old linear Cell scans.

The shopping-boundary regression first failed with 8,620,049 index reads for 28,937 draws, then
passed after the change. Ordinal queries are checked against enumeration after warm reads,
demolition, slot reuse and rebuilding. Save/reload and derived-state assertions are included.
`--profile-work` now also records phase durations through `Simulation.PhaseCompleted`; the clock
remains in the host. These instrumented durations exclude Advance/save after Commit and carry
observer overhead; they are diagnostic, not replacements for untraced Step timings.

### Verification and remaining tail

The controlled artifacts, frozen baseline/optimized binaries, assembly fingerprints, commands and
comparison checks are in `/tmp/borough-controls-20260907`. All timing comparisons here are Release
on the M4 Pro, one simulation thread, with the desktop open; they are local diagnostics, not budget
ratification. The no-delay counted run overlapped validation during ageing. The first seed-0
baseline was accidentally paused inside its measurement window; its timing is discarded and the
`baseline-seed0-repeat` capture is the replacement. Exceptions are retained with the artifacts.

The counted comparison preserves candidate calls and links, every route-work counter, per-Tick
shopping activity, Travellers, Vehicles and allocation. Logical index work per candidate falls from
1,210.94 to 48.81, including the associated count queries and prefix rebuilding: a 95.97% reduction.
These are counter-defined lookups, not hardware memory transactions. The prefix array adds roughly
one MiB per world at the current map dimensions; measured Step allocation is unchanged.

At Tick 14,608 in the optimized counted run, Move costs 23.84 ms and Layers 19.08 ms. The preceding
and following Ticks spend 23.69 ms in Move and effectively no time in Layers. Thus the additional
spike is isolated to Layers, not inferred from the cadence alone. This phase reading does not
establish that all 19.08 ms belongs to one method; the internal attribution follows below.

The Release build and 57 focused assertions pass, including the shopping-boundary regression,
ordinal maintenance, save/reload and the derived-state audit. The full milestone suite was not run.

Retained after untraced comparisons on identical Inputs at 100,000 Citizens. Timing units below are
milliseconds per Tick under the machine, thread count and desktop conditions stated above.

| Seed | Build | Mean | p95 | p99 | Maximum |
|---:|---|---:|---:|---:|---:|
| 0 | before | 11.78 | 24.19 | 30.09 | 49.00 |
| 0 | after | 8.59 | 19.50 | 23.69 | 43.04 |
| 1 | before | 12.12 | 24.64 | 30.79 | 54.64 |
| 1 | after | 8.93 | 20.38 | 25.05 | 41.82 |

Mean Step cost fell 27.01% on seed 0 and 26.29% on seed 1. Every daily activity and allocation
counter matched; State Hashes remained `D551BD2FA385894A` and `FDD1DBA842295351`, respectively.
At 1,000 Citizens on seed 0, mean cost fell from 0.03170 to 0.02616 ms, with p95/p99/max also lower
and State Hash `A4763BF6939AD36B` unchanged. This is an observed small-fixture improvement, not a
claim that the prefix is cheaper for every possible query distribution.

The broader care/school/economy fixtures, older horizon and Godot observation in step 5 remain
outstanding. These results establish a benefit on the measured stress workload, not city capacity.

## Million-Citizen captures and Layers — 2026-09-07

**Outcome:** checkpoint support, GC/memory reporting and trace selection are retained. The noise
candidate and its temporary counters are **not retained**: the second seed failed the untraced mean
gate. All “after” readings below refer to that rejected candidate. The final Core matches “before”.

`ProfileDump` now accepts `--profile-save` and `--profile-load`. A resumed capture verifies the save's
Ruleset and seed; `--warmup-ticks` names the absolute capture Tick. The million-Citizen comparison
loads the same Tick-12,288 save, warms scratch for a full Day, and measures Tick 14,336–16,384.
Save restoration and reporting are outside Step timing. GC collection deltas cover measured Steps;
managed heap and working set are snapshots after each sample, not peak memory or simulation state.
`/usr/bin/time -l` records process peak resident memory separately, including setup and warmup.

The first-Day feasibility pilot at 1,000,000 Citizens completed with end invariants passing and State
Hash `E9D79CD2C256E113`. Release .NET 10.0.2, M4 Pro, one simulation thread, desktop open,
`stress-shopping.toml`, seed 0, Tick 0–2,048: mean 89.39 ms, p95 113.41 ms, p99 2,416.33 ms,
maximum 4,424.45 ms at Tick 0. Process peak resident memory was 676,790,272 bytes. This includes
startup scheduling and is **not an aged-city result**. Its network has 26,463 nodes and 53,498
Segments; network SHA-256 is `466C348F9BF185B31D90E2FD24FCE039917BCB3A4D1F29822F4DEA27F5F822F2`.
Artifacts are in `/tmp/borough-million-20260907`; the subsequent staging run overlaps focused
validation and its intermediate timings are not controlled comparisons.

The Layers-only stack selection (`summarize-simulation-profile.py --scope layers`) captures
112.11 ms of sampled stack weight from the 100,000-Citizen aged trace. All selected weight passes
through `MapLayers.SetLandValueTargets` and `LineSourceQueries.Level`: managed self shares are
44.44% in `Level`, 31.64% in `IntegerMath.SqrtFloor`, and 23.92% in `DistanceTiles`. These are
sample weights, not the phase's wall time. Presence rebuilding and land-value drift do not appear
in this selection; that does not establish zero cost. The trace, commands and frozen builds are
in `/tmp/borough-layers-20260907`.

The candidate's `LineSourceQueries.DistanceSquared` preserved rounded projection and delayed roots
until needed. A nearest-Street candidate must beat the current **floored** distance; a contribution is
outside range only at squared distance at least `(range + 1)²`. Tests cover both floor boundaries.
The work regression failed before the change with 1,552 square roots for 1,552 distances.
Its temporary `MapLayers.QueryWork` exposed query/distance/root counters in `--profile-work` records;
these covered noise queries made through MapLayers, not every possible line-source caller.

### Aged million-Citizen result

Release .NET 10.0.2 on the M4 Pro, one simulation thread, desktop open, seed 0, the same
`stress-shopping.toml` and Tick 14,336–16,384 window. Both builds include the row-prefix optimization;
“after” adds the squared-distance rejection. Captures ran sequentially without overlapping builds,
tests or staging. These remain local diagnostics, not quiet-machine budget ratification.

| Build | Mean ms | p95 ms | p99 ms | Maximum ms |
|---|---:|---:|---:|---:|
| before | 124.71 | 407.80 | 471.85 | 582.55 |
| after | 121.32 | 391.36 | 450.00 | 549.18 |

The mean is 2.72% lower in this pair; a single pair does not establish that all of that difference
comes from the changed query. Both maxima occur at Tick 14,608. State Hash `FAE56437750E05C7`,
every activity counter and measured allocation match. End invariants pass. At capture end there are
1,000,000 Citizens, 86,938 Buildings and 756,929 employed Citizens. The Day peaks at 358,826
Travellers and 340,862 Vehicles, completes 1,929,265 Trips and records 201,792 Shopping outings,
2,461 purchases and 89,624 delivered Goods; no NoRoute or OverBudget outcomes are reported.

Both builds allocate 925,778,216 bytes during measured Steps and record GC generation counts
2 / 1 / 1. Managed heap snapshots are approximately 1.668 GB in both. Process peak resident memory
is 1,364,017,152 bytes before and 1,540,145,152 after; these process peaks include restoration and
warmup and are not evidence of a simulation-state memory change. The save loader restores table
storage from slot counts; later capacity growth is a candidate explanation for allocation, not an
attributed finding. A full Day of warmup has **not** established allocation-free steady state.

The checkpoint's State Hash is `5D2B7C3AF9419C35` at Tick 12,288. Reproduction with a Release host:

```sh
dotnet src/Borough.Headless/bin/Release/net10.0/Borough.Headless.dll --profile \
  --ruleset rulesets/stress-shopping.toml --citizens 1000000 --seed 0 \
  --warmup-ticks 0 --ticks 12288 --no-decide-guard --profile-save /tmp/million-day6.save
dotnet src/Borough.Headless/bin/Release/net10.0/Borough.Headless.dll --profile \
  --ruleset rulesets/stress-shopping.toml --citizens 1000000 --seed 0 \
  --profile-load /tmp/million-day6.save --warmup-ticks 14336 --ticks 2048 --no-decide-guard
```

Use a new save path: profiling refuses to overwrite an existing file. Add `--profile-work` for
per-Tick phase/work attribution, separately from the untraced reading. `profile-simulation.py` also
accepts `--profile-load` and `--binary` for a traced run against the same save and frozen build.

### Query verification

The 100,000-Citizen counted comparison against the earlier prefix-optimized capture preserves
per-Tick route work, shopping selection, activity and allocation. Across 87,552 MapLayers noise
queries, 5,887,788 valid distance calculations now require 1,105,295 square roots: 81.23% avoid
one. Previously every valid distance calculation took a root. Counted Layers time totals
105.49 → 75.10 ms over the Day. At Tick 14,608, Layers costs 19.08 → 13.54 ms while Move costs
23.84 → 23.96 ms; this supports the query change as the cause of the reduced periodic spike.
These are observer-instrumented phase timings under the local diagnostic conditions above.

The million-Citizen counted capture also preserves activity, allocation and State Hash. Its Move
phase totals 243.24 s over the Day; Layers totals 0.71 s and Growth 1.35 s. At Tick 14,608,
Move is 417.53 ms and Layers 136.50 ms. These are phase costs, not an internal Move attribution.
Noise performs 48,289,696 valid distance calculations with 10,662,774 roots, avoiding 77.92%.

| Million-Citizen routing consumer | Searches during the Day | Settled nodes per search |
|---|---:|---:|
| Shopping estimates | 223,713 | 1,588.32 |
| Recorded Shopping routes | 563,465 | 1,134.41 |
| Other recorded routes | 1,522,277 | 1,690.99 |

The other recorded routes settle 2,574,155,452 nodes, versus 994,526,524 across the two Shopping
categories. These counters establish work, not a corresponding CPU-time share. Shopping draws
143,544,987 candidates and uses 75.88 counter-defined index reads per draw. Population scaling is
still confounded with the larger network and changed traffic; none of these is an isolated exponent.

Ticks 14,626 / 14,635 / 14,640 allocate 73,109,304 / 116,490,512 / 716,767,040 bytes respectively:
97.90% of the measured Day's total. All three spend nearly all their time in Move and none is the
slowest Tick. The allocation-owning call sites still need attribution. Work records are in
`million-counted.jsonl`, with derived totals in `million-work-summary.json` under the artifact path
above. The counted run's GC counts differ from the untraced pair; use it for attribution rather than
as another clean timing replicate.

The million-Citizen baseline repeat measures mean 124.89 ms, p95 407.49 ms, p99 474.35 ms and
maximum 572.25 ms at Tick 14,608, with identical state, activity and allocation. Its peak resident
memory is 1,273,708,544 bytes, demonstrating variation even within one frozen build. The separate
`time -l` peak-memory-footprint readings are 1,517,537,368 / 1,516,800,016 / 1,514,096,680 bytes
for before / after / before-repeat. Thus the higher after-run residency is not accompanied by a
higher managed heap or peak footprint. These process-wide observations do not isolate GC or OS
costs inside the measured window.

### Second-seed timing discrepancy

All seed-1 captures preserve State Hash `FDD1DBA842295351`, activity and allocation, with no measured
GC. Under the same local diagnostic conditions, the untraced readings are:

| Order | Build | Mean ms | p95 ms | p99 ms | Maximum ms |
|---|---|---:|---:|---:|---:|
| initial pair | before | 8.73 | 19.80 | 24.85 | 41.92 |
| initial pair | after | 9.83 | 23.84 | 30.31 | 60.37 |
| reverse-order repeat | after | 9.47 | 22.03 | 27.35 | 39.09 |
| reverse-order repeat | before | 8.90 | 20.31 | 25.03 | 41.89 |

The first after run reports 21,482 process-wide involuntary context switches versus 3,056 before,
and ending resident memory falls to 107,855,872 bytes from 262,766,592 despite nearly identical
managed heaps. Its maximum is at Tick 14,845, outside the Layers pass; the repeat after maximum is
at Tick 14,591. These observations suggest disturbed capture conditions but do not prove the cause.
Both after means exceed their before controls. The results are retained, not discarded.

The counted seed-1 pair preserves every per-Tick route, shopping, activity and allocation counter.
Layers totals 111.70 → 78.19 ms over the Day and costs 21.18 → 14.23 ms at Tick 14,608. Move totals
19.54 → 19.31 s; counted mean is 9.68 → 9.55 ms. This confirms the isolated query benefit on both
seeds, but does not resolve the repeated untraced mean regression. Under step 4, the candidate is
backed out rather than treated as a generally successful optimization. The captures, frozen builds,
candidate source and patch remain under `/tmp/borough-layers-20260907` (`candidate-source/`).
The two floor-boundary assertions remain; the rejected work-reduction assertion stays with the
candidate source. The earlier row-prefix optimization is unchanged.

Final verification: 45 focused assertions pass, including save/reload, profiling and floor boundaries;
the trace-summary scope checks pass. The rebuilt Core SHA-256 is exactly the frozen before-build
`BEE2D200BC8871F4516463C5F77958A1B08EA61648C2BB5E236EEB5CBB3B8172`, so the retained simulation
is the one measured by the million-Citizen baseline and repeat. The full milestone suite was not run.

## Move, route reuse and capacity attribution — 2026-09-07

Release .NET 10.0.2, M4 Pro, one simulation thread, seed 0, `stress-shopping.toml`,
checkpoint Tick 12,288, warm to 14,336. The managed trace measures the next 2,048 Ticks;
the new counted run measures 4,096. Desktop activity, validation and captures overlap.
These are attribution runs: **none of their timings ratifies a budget or an optimization**.
The trace uses the frozen baseline Core and ends at the expected `FAE56437750E05C7`, with
end invariants passing.

`summarize-simulation-profile.py --scope move` recognizes the inlined Advance wrapper through
`AdvanceTravellers`, `ReleaseEnded` and `CloseTick`. Of 284,386.98 ms of sampled Step stack weight,
281,153.34 ms is recognized as Move. The mutually exclusive search categories are:

| Work | Sampled weight, ms |
|---|---:|
| Other recorded route searches | 131,005.20 |
| Recorded Shopping route searches | 38,052.31 |
| Shopping estimate searches | 20,393.42 |
| Non-search Move | 91,702.41 |

This is managed stack weight, not CPU utilization or isolated wall time. `WalkScratch.PopRoot`
has 38.13% of Step self weight; `Search` has 28.49% self and 66.62% inclusive, **including**
`PopRoot`. `BuildingResidency.NthIn`, `CommuteEngine.Generate` and `WorkSchedule.Accrue` follow
at 6.06%, 5.59% and 5.05% self. Inclusive rows must not be added together.

`RouteReuseProbe` observes only actual searches, keyed by graph identity/version, mode and both
full Addresses. It resets each Tick and holds at most 65,536 distinct keys per category; overflow
is reported, not treated as unique evidence. No overflow occurred. Over Tick 14,336–16,384:

| Category | Searches | Exact repeats within the same Tick |
|---|---:|---:|
| Other recorded | 1,522,277 | 230 |
| Recorded Shopping | 563,465 | 88,928 |
| Shopping estimates | 223,713 | 6,739 |

This is an opportunity count, not an implemented cache or a cross-Tick hit rate. It gives little
reason to prioritize an exact within-Tick cache for the dominant category.

`TableGrowthProbe` reads capacities after Step. It counts replacement-array payload from the
column declarations, including intrinsic columns and both buffers, and accounts for multiple
doublings. Array headers and alignment are excluded. The three bursts are `Rows.Grow` calling
`Column<T>.Grow` through these owners:

| Tick | Table | Capacity before → after | New array payload, bytes |
|---:|---|---:|---:|
| 14,626 | Trip | 338,465 → 676,930 | 37,908,080 |
| 14,626 | Traveller | 338,465 → 676,930 | 35,200,360 |
| 14,635 | Leg | 987,200 → 1,974,400 | 116,489,600 |
| 14,640 | Route Hop | 12,358,040 → 24,716,080 | 716,766,320 |

`TripEngine.Start` creates Trips, Legs and Travellers; `RecordRoute` appends Route Hops.
The following Day, Tick 16,384–18,432, has **no table growth**, but still allocates 19,423,280
bytes in the counted Steps. Two Days do not establish sustained memory behavior. The continuous
run comparison remains open; the new control also resumes the checkpoint. The probes add
33,264 bytes to the first Day's measured allocation versus the frozen control, outside the three
bursts. Their timings and GC counts are therefore diagnostic only.

**Implementation choice (prototype below):** bounded route workers. `WalkRouting.SpeedOn` and
`RoadArcs.TimeFor` read free-flow costs, so current traffic volume is not a search input.
That establishes a useful computation boundary, **not independence of Trip creation**:
`TripEngine.Itinerary`, `Start`, parking and result application retain ordering-sensitive state.
Prove request preparation and ordered consumption before dispatching workers; require 1/8-thread,
replay, save/reload and activity equivalence. Search leaves substantial non-search work, so idealized
search-only parallelism is not a promise to reach the existing Tick target. Moving serial Step off
the render thread separately needs a snapshot/command ownership boundary for the shell's world reads.

Verification: 30 focused profiling/routing assertions and the synthetic stack-boundary checks pass.
Across both measured Days, the counted run and frozen control have identical activity and finish
at Tick 18,432 with State Hash `0540B89003B50764` and passing end invariants. Every first-Day
route-work, Shopping-work and activity record matches the earlier counted fixture. Second-Day
Step allocation matches the control exactly. This is not a milestone/full-suite validation.

Durable evidence is under `artifacts/aged-city-performance/20260907/`: `checks.json` exposes the
comparisons; `move.json` exposes ranked self/inclusive attribution. The two archives preserve
commands, fingerprints, raw traces, work logs, builds and source, including the rejected noise
candidate. Both manifests carry archive and per-file SHA-256 checks, verified after writing.
The prior archive excludes only the reproducible Tick-12,288 save. The new evidence is also in
`/tmp/borough-move-20260907-retry` and `/tmp/borough-growth-20260907` while those directories survive.
Items 3–9 remain open; the two-Day observation does not close weekly patterns, older horizons,
functioning-economy controls, quiet-machine captures or Godot observation.

## Bounded commute route workers — 2026-09-07

`Simulation.RouteWorkerCount` selects 1–8 workers, default 1. `CommuteEngine.Flush` prepares a
fixed window of up to 128 requests; `RouteBatch` gives each worker its own `WalkScratch` and
stores copied Arc paths in reusable slots. Workers only read the stable Road Graph. The caller
joins them before `TripEngine.StartPrepared` applies results in original Citizen/Trip/Leg order.
Each commute source flushes separately. Small networks and short request windows stay serial.
These are host execution thresholds, not Ruleset choices or saved state.

Preparation does not reserve parking or create Trips. `StartPrepared` runs the usual qualification
and itinerary checks; consuming a result requires matching graph version, mode, full endpoint
Addresses and crossing cost. A mismatch searches synchronously. Thread completion order never
chooses mutation order. This covers commute searches only: Shopping, civic journeys and all world
writes remain serial, and `Step` still runs on its caller. A dedicated simulation thread for Godot
requires the separate snapshot/command ownership work described above.

`RouteWorkerTests` exercises actual Input Log replay with per-Tick 1/2/8-worker State Hash checks,
save/reload while changing worker count, real result consumption, reversed worker completion,
unchanged state during preparation, stale-input fallback and parking refusal. The initial stub
failed because no prepared routes were consumed. The focused routing/commute/profiling group
passes 44 assertions; the final Input Log version passes all seven route-worker cases.

The profile-only CLI flag is `--route-workers N`. Allocation measurement now uses process-wide
managed allocation deltas around Step, including workers. Synchronous route-work counters exclude
worker searches; `routeBatch` separately reports prepared, used and fallback results. Worker count
is a concurrency limit including the caller, not a promise that every worker stays occupied.

Comparisons use frozen Release builds, .NET 10.0.2 on M4 Pro, `stress-shopping.toml`, warm to
Tick 14,336 and measure 2,048 Ticks, Decide guard off. The million-Citizen runs resume Tick 12,288.
Captures run sequentially without overlapping builds/tests; the desktop remains open. These are
local diagnostics, not quiet-machine ratification of the one-core Tick budget.

### Scaling and costs

On that million-Citizen M4 Pro fixture, with worker limits including the caller:

| Route workers | Mean ms | p95 ms | p99 ms | Maximum ms | Step allocation, bytes | Gen 0/1/2 collections |
|---:|---:|---:|---:|---:|---:|---|
| 1 | 120.79 | 393.55 | 452.66 | 568.52 | 925,780,664 | 2 / 1 / 1 |
| 2 | 96.64 | 289.48 | 327.71 | 462.27 | 949,194,704 | 4 / 2 / 1 |
| 4 | 84.88 | 239.70 | 272.58 | 416.91 | 956,396,232 | 5 / 2 / 1 |
| 8 | 77.40 | 208.29 | 232.91 | 381.97 | 970,245,872 | 6 / 2 / 1 |

Every maximum is Tick 14,608. Every run preserves State Hash `FAE56437750E05C7`, all recorded
activity and end invariants. Each parallel run prepares and consumes 3,721,812 route queries with
no fallback; these include cheap routing shortcuts and must not be compared directly with the
earlier count of actual searches. Eight workers reduce measured mean by 35.92% and p99 by 48.55%,
with diminishing returns. This neither meets nor changes the existing one-core target.

The worker configuration adds 44,465,208 allocated bytes to the eight-worker Day and increases GC
frequency. Managed heap at the end is 1,667,805,656 / 1,674,973,440 / 1,674,062,136 / 1,680,046,048
bytes for 1/2/4/8 workers. Process-wide peak memory footprint is 1,519,192,080 / 1,522,305,064 /
1,527,269,464 / 1,525,762,040 bytes; peak residency varies independently and is preserved in the raw
resource readings. Scheduling and retained scratch have costs; the attributed table-growth bursts
remain. These captures do not establish long-run memory stability.

At 100,000 Citizens under the same desktop conditions, the revised 1/8-worker comparison is:

| Seed | Workers | Mean ms | p95 ms | p99 ms | Maximum ms | Step allocation, bytes |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 1 | 9.05 | 20.52 | 24.76 | 43.75 | 3,836,440 |
| 0 | 8 | 8.41 | 17.12 | 21.89 | 42.47 | 9,077,512 |
| 1 | 1 | 8.78 | 20.07 | 25.01 | 41.71 | 3,842,088 |
| 1 | 8 | 7.87 | 16.20 | 21.08 | 39.65 | 9,146,168 |

Seed 0 runs eight then one; seed 1 reverses the order. No measured GC occurs in these four runs.
State Hashes remain `D551BD2FA385894A` / `FDD1DBA842295351`, with identical activity. All prepared
results are consumed: 335,248 / 340,607 by seed. The original serial build also measures 8.64 ms
mean on seed 0, so the eight-worker benefit against that control is smaller than against the new
serial reading. The initial prototype's two-seed means and all subsequent controls are preserved.

### Small-city and serial controls

The first prototype made the 1,000-Citizen M4 Pro fixture worse: mean 0.02561 → 0.04246 ms for
1 → 8 workers. It dispatched work too small to amortize scheduling. That behavior is rejected;
`CommuteEngine.QueueTravel` now bypasses workers below its network threshold. Three revised pairs
measure means 0.02598–0.02635 ms at one worker and 0.02603–0.02636 ms at eight, with no prepared
queries and identical Step allocation of 2,166,224 bytes.

An older serial control initially measured 0.02313 ms mean and 0.4537 ms maximum, while the revised
serial build showed occasional maxima near 1.9 ms. This was investigated rather than discarded.
The unchanged original build repeats at 0.02563 ms mean and 1.9537 ms maximum. The old profiler
driving the new Core measures 0.02528 / 1.9151 ms. Counted original/hybrid/revised runs have identical
per-Tick route, Shopping and activity work; all reproduce the Layers spike at Tick 14,608. With
`DOTNET_TieredCompilation=0`, original/revised means are 0.03513 / 0.03496 ms, p95 0.0801 / 0.0799 ms
and p99 0.0899 / 0.0905 ms. Those controls support runtime/capture sensitivity rather than added
simulation work; they do not identify a particular JIT event. The original adverse observations
remain in the archive. All these small runs finish at `A4763BF6939AD36B` with passing invariants.

The 100,000-Citizen seed-0 serial repeat is original/revised 8.49446 / 8.63290 ms mean,
19.0886 / 19.3248 ms p95 and 23.6518 / 23.7335 ms p99. To separate the profiler changes from Core,
the old profiler then drives the new Core: 8.47991 ms mean, 19.0830 ms p95, 23.4655 ms p99 and
42.6198 ms maximum, with the same activity and 3,836,440 allocated bytes. This control supports
retaining the Core change; the apparent serial penalty does not reproduce with the old measurement
host. It does not identify which profiler/runtime interaction changed the reading. New-profiler
worker comparisons share that host, and all original controls remain available.

**Retained, opt-in:** bounded commute route workers. Default execution stays serial. The subsequent
continuous/resumed capacity comparison is below; broader routing work, weekly/older
horizons, fixed-network controls, functioning-system fixtures, quiet captures and shell observation
remain open. Moving Step to its own thread remains a separate shell change.

Durable evidence: `artifacts/aged-city-performance/20260907/route-worker-comparisons.json` carries
activity/hash comparisons and raw measurement summaries. `route-worker-evidence.tar.gz` preserves
commands, frozen original/prototype/revised/hybrid builds, source, resource readings, rejected small
dispatch behavior and serial controls. `route-worker-manifest.json` verifies archive and per-file
SHA-256. The reproducible million-Citizen checkpoint remains excluded. The as-measured and final
source snapshots distinguish later comment edits and the strengthened save/reload assertion.

Final validation: `scripts/test.sh` collects 2,946 assertions: 2,943 pass and three fail. Two explicit
fixture allow-lists omitted `stress-shopping.toml`; they now name it while retaining the exact
lattice-count and District checks. The strengthened save/reload test initially finds no worker use
in its short post-load interval; a full-Day interval now requires actual consumption before and
after saving, alongside every-Tick hash equality and worker-count changes. All three pass the
focused rerun. Core logic is unchanged from the measured build; subsequent Core edits are comments.
Both runs and the initial failing prototype tests are archived. This is assertion-lane validation,
not an unfiltered milestone suite or a quiet-machine capture.

## Restoration headroom — 2026-09-07

`Rows.Restore` restored the saved high-water slot count into exact-sized arrays. For Trip, Leg,
Traveller and Route Hop tables this discarded the spare capacity of ordinary doubling growth.
`Rows.GrowTo` now supports `amortizeRestore`; those four declarations opt in. It computes the final
capacity before allocating each column once. Other tables retain their existing restoration policy.
Saved slots, free-list order, ids, file bytes and State Hashes are unchanged. Extra capacity is
unsaved storage, not extra rows; repeated loading does not accumulate padding.

`SaveCapacityTests` first reproduced a loaded Route Hop table growing 65 → 130 while the same
uninterrupted allocations fit its existing capacity of 128. The fix passes that regression and an
active-city test covering all four tables, byte-identical save rewriting, exact Citizen restoration,
and subsequent per-Tick hash equivalence. The focused restoration/save/reload group passes 26 tests.

The diagnostic harness uses the existing `ProfileDump.Measure` and frozen before/after Core builds
with the same measurement host. Continuous 1,000/100,000-Citizen runs write a checkpoint at Tick
12,288 and continue; separate processes resume those checkpoints. Two seeds cover 100,000 Citizens.
The million-Citizen pair resumes the existing checkpoint; **it is not a new continuous million run**.
All warm to 14,336 and measure two Days through 18,432, with eight route workers including the caller,
Decide guard off, Release .NET 10.0.2 on M4 Pro. Small/100,000 runs count per-Tick work; the million
pair is untraced. Captures are sequential without builds/tests, but the desktop remains open.
The preliminary small probes establish work/capacity behavior, not comparative timing.

The small fixture genuinely outgrows its previous peak in both runs: restoring normal growth buckets
does not eliminate that growth and can allocate a larger replacement than compact restoration.
At 100,000 Citizens, the continuous movement tables have enough capacity for the measured interval;
the old loader instead forces replacement arrays. The fixed loader recovers the continuous movement
capacities. The continuous seed-0 run also grows `known_shop` in the second Day; that separate table
and other restoration policies are unchanged. Per-Tick work and activity distinguish this genuine
growth from the four save-induced bursts.

At 100,000 Citizens, loading causes 60,303,808 / 61,723,636 bytes of movement replacement arrays
by seed; the continuous and fixed runs allocate none for those tables during the measured interval.
The initial counted after means are slightly higher. Reverse-order untraced controls instead show
first-Day means 7.618 → 7.502 / 8.050 → 7.968 ms and second-Day means 7.858 → 7.750 / 8.315 →
8.258 ms. Maxima are mixed, including seed 0's second Day at 39.05 → 43.56 ms. All readings are
retained; this is an allocation improvement, not a claim of a large or uniform throughput gain.

The untraced million-Citizen M4 Pro pair uses eight route workers, after then before:

| Start Tick | Build | Mean ms | p95 ms | p99 ms | Maximum ms | Step allocation, bytes | GC 0/1/2 |
|---:|---|---:|---:|---:|---:|---:|---|
| 14,336 | before | 78.08 | 206.85 | 233.14 | 378.88 | 970,410,744 | 6 / 2 / 1 |
| 14,336 | after | 77.27 | 205.78 | 232.03 | 381.31 | 64,000,528 | 5 / 1 / 0 |
| 16,384 | before | 83.64 | 230.05 | 256.98 | 407.98 | 58,345,088 | 5 / 0 / 0 |
| 16,384 | after | 82.49 | 224.51 | 250.27 | 408.59 | 182,098,104 | 5 / 0 / 0 |

The maxima occur at Tick 14,608 / 16,656. Hashes at Tick 16,384 / 18,432 remain
`FAE56437750E05C7` / `0540B89003B50764`, with identical activity and passing end invariants.
The first Day avoids the attributed replacement arrays. The second Day **genuinely exceeds** the
normal Leg capacity: 1,062,023 slots require growing 1,048,576 → 2,097,152, allocating 123,731,968
bytes of column payload. This byte count is unrelated to developed density **of the 1M target**;
the disqualifier registry matches a substring of the allocation figure. Thus the two-Day allocation reduction is 1,028,755,832 → 246,098,632 bytes,
including that later growth, rather than a claim that growth disappeared forever.

Loading allocates 151,846,172 more bytes of table capacity up front. Retained table payload is
301,336,008 bytes lower after the first Day and 239,470,024 lower after the second. Whole-process
peak memory footprint is 1,526,614,056 → 1,397,671,760 bytes; peak residency is
1,540,833,280 → 1,421,737,984 bytes. These are different scopes: table payload excludes headers,
non-table storage and unreclaimed arrays. Other tables can still grow during warmup or later play;
the four-table policy is not a claim about all loading costs or sustained memory stability.

Retained. `scripts/test.sh` passes all 2,948 assertions, including worker equivalence and save/reload;
the tested Core is byte-identical to the measured candidate. All 15 captures preserve matching
checkpoint/end hashes and activity, and all counted per-Tick work matches. Before/after storage
comparisons differ only in the four movement capacities. Evidence and reproduction commands are in
`artifacts/aged-city-performance/20260907/capacity-evidence.tar.gz`; `capacity-comparisons.json`
exposes the readings and `capacity-manifest.json` verifies archive and per-file SHA-256. Reproducible
checkpoints are excluded, with their fingerprints retained. This is not milestone or quiet-machine
validation. Next: the shell snapshot/command boundary and gameplay threading integration.


## Gameplay threading — 2026-09-07

`SimulationThread` runs normal gameplay Steps on a dedicated thread by default. `Main.Threading`
guards World and Simulation access while a batch is outstanding; `Main._Process` collects it before
running readers or deferred UI actions. The prepared Godot scene remains available to render, with
mouse pan/zoom and edge scrolling active. This is **exclusive handoff**, not an independent World
snapshot: inspector updates, movement preparation and world-dependent commands wait for a boundary.
Boot, `--start-at` ageing and tuner regeneration still execute synchronously.

Only one batch can be outstanding. Commands are copied before dispatch, logged at their applying
Tick and consumed only on the first Step. Scheduling caps accumulated debt and batch size; slow
batches shrink to one Tick. Paused edits buy one Tick, Drive barriers cap dispatch, and capture holds
the published Tick. Worker failures reach the shell and terminate the run; shutdown joins the worker.
Godot accepts `--route-workers 1..8`, default one, including the simulation thread. The existing
small-route bypass remains in Core. `--main-thread-sim` supplies the comparison control. Neither
switch enters saved or hashed state. `Main.Performance` reports worker wall time separately from
shell time and records frames that reused the prepared scene while the worker was busy.

The driven checks exposed an existing shutdown defect with both threading switches disabled:
`Socket.Dispose` waited while the listener remained inside native `accept`. The minimal headless
socket reproduction and stack sample distinguish it from rendering or simulation work. `Main.Serve`
now uses cancellable accept/read/reply waits; `_ExitTree` cancels and joins the listener before
socket disposal. The previously hanging reproduction now exits normally. Capture checks require
normal exit and removal of the listener socket; no timeout exception is accepted as success.

`SimulationThreadTests` covers copied input order, nonblocking completion polling during deliberately
blocked work, submitting/collecting only from the creating thread, failure propagation, joining active
work, and replay equivalence with one/eight route workers. The parallel fixture requires consumed
worker routes. A fixed Drive comparison also reaches Tick 7/8 barriers with a road edit and produces
byte-identical Input Logs and the same final hash with threaded/eight-worker and main-thread/one-worker
execution.

`scripts/check-shell-threading.py` verifies paused road removal/replacement, keyboard camera/view
input, stable paused geometry, matching headless replay, and clean shutdown. Visible runs use
Godot Debug, Metal Forward+, M4 Pro, the existing frame limit, `stress-shopping.toml`, seed zero,
4×, focus Tile (48,32) at 300 metres followed by the recorded camera inputs. The final small run
starts with 4,000 Citizens at Tick 512. The aged run starts with 16,000 Citizens at Tick 14,336 and
ends at Tick 14,570; it retains active travel and Shopping and observes frames while Step is still
running. A small city's Steps need not exceed the frame interval; zero such frames is not proof
that its worker was unused. The explicit `--require-busy-frames` check belongs on the aged workload.
The serial headless control also passes; its count check holds edge scrolling with the help panel
because there is no physical pointer. Screenshots were inspected alongside their readouts and draw
dumps. These are functional desktop observations, **not Release timing or budget claims**.

The attempted 100,000-Citizen Debug observation timed out during synchronous startup ageing before
opening a view. It supplies no gameplay performance result. Background startup remains a concrete
follow-up, alongside independent view snapshots and expensive scene preparation on the main thread.
The handoff does not remove those costs or make inspector/command latency independent of a long Tick.

Validation: the assertion lane passes 2,953 of 2,954 tests; its sole failure is the allocation-byte
substring matching the corpus density registry, clarified beside that original figure above. Both
Disqualifier tests pass the focused rerun, followed by all 52 corpus assertions after recording this
section. No other assertion fails. Godot Debug builds without
warnings. This is not the unfiltered milestone suite. Evidence, failed diagnostic attempts, source
snapshots and reproduction commands are retained in
`artifacts/aged-city-performance/20260907/threading-evidence.tar.gz`; `threading-manifest.json`
records file and archive hashes. `threading-checks.json` exposes the successful functional checks.


## Background city preparation — 2026-09-07

`CityPreparation` owns background World construction, the boot Command and every startup-ageing
Tick. `Main.Preparation` publishes the completed Simulation on the main thread. Normal gameplay
then uses the existing Step handoff. This supersedes the synchronous boot/regeneration limitation
in the preceding section; it does not reduce the computation needed to age a city.

Preparation exposes only completed-Tick progress until publication. Its worker uses no Godot API.
The loading view remains responsive and supports cancellation at the next simulation boundary.
A tuner regeneration retains the current World and Ruleset metadata until the replacement succeeds;
cancelling leaves the original city standing. Pending edits are discarded when regeneration begins.
Preparation failures terminate startup with the original exception. Forced shutdown joins the worker;
construction or an individual Step already running must finish before cancellation takes effect.

The existing socket channel can read or photograph the loading view and quit while preparation is
active. `ui key Escape` exercises cancellation; other requests wait for publication. Loading replies
explicitly say `preparing` and do not claim a published World. Screenshot captions use the previously
drawn loading text, since progress can advance between rendering a frame and taking its image.
No additional OS screen-capture permission is needed for this path.

`CityPreparationTests` compares serial and background boot/ageing, including Ground-only boot and
zero requested start Tick, verifies subsequent hashes, refuses premature completion, and checks
cancellation and failure propagation. The driven 16,000-Citizen `stress-shopping.toml` run starts
at Tick 14,336, completes paused road edits and gameplay, and replays its final Tick 14,435 hash.
The loading capture observes changing progress while Godot responds, then cancels and exits cleanly.
A separate regeneration check preserves the original Tick-64 hash when cancelled, then produces a
Tick-1 city whose Input Log replays exactly. These are Godot Debug functional observations on the
M4 Pro desktop, not reference timing or throughput claims.

`scripts/check-city-preparation.py` reproduces the loading/cancellation check. Evidence is retained
in `artifacts/aged-city-performance/20260907/preparation-evidence.tar.gz` with its file/archive hashes
in `preparation-manifest.json`. Scene geometry preparation still runs on the main thread, and
regeneration temporarily retains both the old and replacement Worlds. Independent view snapshots,
geometry preparation and large-world memory/throughput captures remain open.

Validation: Godot Debug builds without warnings; all 2,959 assertions pass through `scripts/test.sh`.
The loading, cancellation, regeneration and aged-gameplay observations above also pass.

## Production viability: destination-directed routing — 2026-09-08

The user prioritised proving a production path over further shell preparation. `WalkScratch.Direct`
now supplies a consistent lower bound toward both destination endpoints. `UnitBound` derives the
bound from every traversable Arc's cost and Manhattan displacement, separately by mode; it is
invalidated by graph identity/version. Authored lengths, diagonals and speed changes are included.
Zero-cost Arcs retain Dijkstra. Equal-cost predecessors and arrivals retain Dijkstra's ordering,
so directing the search does not choose different routes. `SettleAll` remains exhaustive.
`DirectedRoutingTests` compares exact costs, arrival endpoints and Arc sequences on 12,000 queries,
including one-way roads, diagonals, ties, zero-cost edges and speed changes.

The bounded cross-Tick FIFO probe is an opportunity count, **not an implemented cache**. Release
.NET 10.0.2, M4 Pro, one simulation thread, `stress-shopping.toml`, seed 0, resumed at Tick 12,288,
warmed to 14,336 and observed through 18,432; other work overlapped, so its timings are not used.
The cache starts empty at capture. A 65,536-entry window would avoid 3.20% of observed settled-node
work; a 1,048,576-entry window would avoid 10.45%, across 4,740,678 observed searches. The larger
window's second-Day-only share is 12.49%. These are weighted work opportunities, not wall-clock
savings; FIFO eviction, capacity, exact Addresses and graph version constrain the result. They do
not rule out a different cache policy or shared route structure. This capture observes Trip and
Shopping searches; the subsequently added civic observer broadens future captures. End hash
`0540B89003B50764` and invariants match the existing control. This does not justify prioritising
an exact route cache over reducing search work.

`ProfileDump` now supports `--profile-services` and `--profile-reuse`. `ProfileServices.Place`
distributes facilities before ageing; its provisional population-based counts are reported.
`CivicEngine.EventObserved` counts actual school attendance, treatment and other events independently
of bounded history retention. `profile-services.toml` adds life stages, school and care to the stress
geography. A capture at age 14,336 had no school attendance; ageing through Tick 65,536 reaches
children and the activity assertion now checks the following week. School attendance, treatment,
purchases and wages must all occur. Hospital placement alone does not prove inpatient work, and
this fixture's absence of admissions is not a failure of its outpatient-care assertion.

The fixture remains economic distress, not balanced production. A separate attempted retail-only
employment variant did not establish a functioning full economy and is preserved as a rejected
experiment in the evidence, not shipped as a successful control. The existing stress fixture's
non-selling employers cannot fund its broad workforce; `waged.toml`'s header already describes the
narrow paying loop. Functioning-economy controls, larger service workloads, the million-Citizen
second seed and shipping-hardware certification remain open. Neither successful activity nor a
faster stress fixture discharges those gates.

### Retained timing comparison

Frozen before/after Release builds, .NET 10.0.2, M4 Pro, desktop open, with captures sequential and
no overlapping tests/builds. `stress-shopping.toml`; resume Tick 12,288, warm to 14,336, measure
2,048 Ticks. These are local diagnostics, not the i5-10400-class one-core budget's ratification.
Worker limits include the caller; all World writes remain serial.

| Citizens | Seed | Workers | Build | Mean ms | p95 ms | p99 ms | Max ms |
|---:|---:|---:|---|---:|---:|---:|---:|
| 100,000 | 0 | 1 | before | 8.61 | 19.02 | 23.50 | 42.51 |
| 100,000 | 0 | 1 | after | 5.28 | 10.19 | 11.73 | 30.40 |
| 100,000 | 1 | 1 | before | 8.84 | 20.34 | 24.87 | 41.39 |
| 100,000 | 1 | 1 | after | 5.26 | 10.26 | 11.98 | 30.08 |
| 1,000,000 | 0 | 8 | before | 81.37 | 216.70 | 242.76 | 391.77 |
| 1,000,000 | 0 | 8 | after | 50.16 | 92.80 | 107.60 | 259.18 |

Every compared activity field and end State Hash matches. Step allocations are unchanged at
100,000 Citizens; the million-Citizen eight-worker pair allocates 64,027,008 → 65,858,720 bytes.
The additional heap priority array costs eight bytes per heap entry per scratch; GC/scheduling
also affect process-wide allocation readings. This is not a zero-memory-cost change.

The million-Citizen after reading supplies about 20 Ticks/s on this M4 Pro eight-worker stress
fixture. It clears the isolated 16-Tick/s design-rate throughput but not the 64-Tick/s accelerated
rate: the average wall-clock gap to 15.6 ms remains about 3.2-fold. This does not establish minimum
specification, full-game frame costs, service costs at that population or sustained older-city
performance. It strengthens the case for continuing; it does not ratify a shipping promise.

The broader untraced capture uses the same Release/M4 Pro conditions, **one simulation thread**,
seed 0, `profile-services.toml`, starting at 10,000 Citizens and measuring Tick 65,536–79,872.
It holds 11,087 Citizens during the measured week, with ten schools, twenty clinics and two
hospitals placed before ageing. It records 2,245 school attendances, 8,821 treatments, 1,128
purchases and wages on every measured Day; no admissions occur. Daily mean Ticks range
1.25–1.46 ms and daily p99 ranges 7.45–8.74 ms. These costs belong to this combined distress
fixture and horizon, not an isolated care price or a million-Citizen extrapolation.

The separate counted 100,000-Citizen seed-0 pair answers identical route requests and searches,
with matching activity/hash: settled nodes fall 225,812,513 → 40,868,059 (81.90% less work).
These instrumented timings are not substituted for the untraced table above.

The 1,000-Citizen resumed seven-Day control retains identical hashes/activity, with similar later-Day
costs. Its first measured Day is slower after the change in both capture orders: mean
0.17591 → 0.19022 ms, then 0.18101 → 0.19138 ms in the reverse-order repeat. A diagnostic
`DOTNET_TieredCompilation=0` pair removes that initial spike: first-Day means 0.03448 → 0.03428 ms,
with after means also lower on the remaining six Days. This supports a runtime-tiering explanation,
not a steady-state search regression. Default runtime settings are retained; the small initial
cost remains disclosed rather than counted as a win. All are Release M4 Pro one-thread captures,
desktop open, with the same checkpoint, warmup and measured week.

Final assertion lane: 2,968 passed (`/tmp/borough-test-20260908-002157.log`). The earlier 2,969-case
run included two generated `TreasuryFromAFileTests` cases for the rejected retail candidate; moving
that file out of shipped Rulesets removes those two cases, while the missing-target routing case
adds one. No existing assertion was removed. This is the assertion lane, not a full milestone run.
The timing builds precede the final invalid-target guard and reporting-only refinements; the
closeout capture rechecks the final binary, with fingerprints preserved separately.

The final-binary 100,000-Citizen seed-0 recheck records mean 4.98 ms and p99 11.04 ms under the
same one-thread Release/M4 Pro conditions, retaining hash/activity. The new civic work observer
also preserves hash/activity against an uninstrumented matching run while observing 16,578 civic
searches. Durable commands, builds, raw readings, rejected attempts, source and verified per-file
fingerprints are in `artifacts/aged-city-performance/20260908/viability-evidence.tar.gz`, with
`viability-manifest.json` and `viability-checks.json`. Checkpoint fingerprints and reproduction
commands are retained; the large reproducible save dumps are not archived again.


## Remaining Shopping discovery work — 2026-09-08

`ShoppingWork` now separates discovery draws, Businesses examined, missing sale Bins, already-known
providers, route refusals and additions. `DiscoveryCalls` counts nonempty candidate-box searches;
`DiscoveryWithoutAddition` counts those adding no provider. `NoSaleBin` means `StockBin` found no Bin
for the requested Good connected to a market, **not that its shelf was empty**. The counters are
opt-in through `--profile-work`; they do not change sampling, retry timing or provider choice.

Release .NET 10.0.2, M4 Pro desktop, one simulation thread, `stress-shopping.toml`; resume Tick
12,288, warm to 14,336 and capture through 16,384. Captures run sequentially without overlapping
builds/tests. These are local diagnostics, not quiet-machine budget ratification or a functioning
economy. Traced and counted timings are not untraced throughput readings.

| Citizens | Seed | Discovery draws | Searches adding no provider / searches | Businesses lacking a sale Bin / examined | Providers added | Discovery route refusals |
|---:|---:|---:|---:|---:|---:|---:|
| 100,000 | 0 | 10,289,995 | 311,745 / 325,977 | 9,992,528 / 10,010,723 | 14,362 | 124 |
| 100,000 | 1 | 10,209,522 | 309,230 / 323,292 | 9,934,942 / 9,953,002 | 14,176 | 99 |
| 1,000,000 | 0 | 143,544,987 | 4,467,880 / 4,486,175 | 139,792,675 / 139,814,982 | 18,382 | 2,722 |

The wasted discovery work is predominantly rejection before routing. It is not evidence that
Households cannot reach shops or that reachable shelves are empty. Geometry and occupied geography
still vary with population; this table does not isolate a scaling exponent. Reducing route-search
cost cannot eliminate these candidate draws. Any candidate-selection optimization must preserve the
Building ordinal distribution and provider order required by step 4.

At 100,000 Citizens on seed 0, fresh managed attribution assigns 6.10% of sampled Step weight to
`BuildingResidency.NthIn`, all called through Shopping. `WalkScratch.Search` holds 26.21% inclusive;
`WorkSchedule.Accrue` holds 14.32% inclusive and `CommuteEngine.Generate` 21.36% inclusive.
These inclusive rows overlap and must not be added. This is sampled stack weight, not CPU utilization.

The new observer preserves daily activity and hashes against frozen uninstrumented controls on
both 100,000-Citizen seeds. Every previous per-Tick Shopping, synchronous route and movement counter
also matches the prior directed-routing capture on seed 0. The million-Citizen end hash remains
`FAE56437750E05C7`; end invariants pass. Uncounted before/observer-disabled means are 4.79/4.95 ms
for seed 0 and 5.09/5.02 ms for seed 1 under the conditions above. These mixed differences are not
a speed improvement or a claim of zero observer overhead. No optimization is retained by this step.

At one million Citizens on seed 0 with the same one-thread conditions, `NthIn` holds 12.39% of
sampled Step weight, all through Shopping; `Discover` holds 14.59% inclusive. `WalkScratch.Search`
holds 27.21% inclusive, `WorkSchedule.Accrue` 14.17%, and `CommuteEngine.Generate` 35.97%.
These overlapping weights identify remaining work, not isolated wall-clock savings. The traced
million-Citizen control and counted run match every daily activity field and the end hash.

**Row-total reuse rejected:** candidates preserve checked hashes/activity but fail step 4.
Under the same Release/M4 Pro desktop conditions, one-worker 100,000-Citizen seed-1 adaptive
means worsen: 5.16895 → 5.18906 ms; reverse order 5.24723 → 5.33834 ms.
Million-Citizen seed-0 mean improves 54.59126 → 54.28640 ms; p95/p99 worsen.
Candidate code removed. Next: attribute payroll/commute scans. Broader controls remain open.
`artifacts/aged-city-performance/20260908/draws-checks.json` carries comparisons;
`draws-evidence.tar.gz` and `draws-manifest.json` preserve and verify candidates, tests and captures.

Evidence, frozen builds, commands, raw traces, counted logs and source are preserved in
`artifacts/aged-city-performance/20260908/shopping-evidence.tar.gz`; `shopping-checks.json` exposes
the comparisons and `shopping-manifest.json` verifies archive and per-file fingerprints. Large
reproducible saves are excluded, with checkpoint hashes and commands retained.


## Payroll experiments and boundary counts — 2026-09-08

Neither payroll candidate is retained. Release .NET 10.0.11, Ubuntu 24.04, Intel Core i5-10400,
one simulation thread, `stress-shopping.toml`, fresh worlds aged to Tick 14,336 and measured
through 16,384; Decide guard off. These are unpinned desktop diagnostics with no CPU-frequency
or external-workload control, not quiet-machine budget ratification. Builds and focused tests
ran during portions of ageing; capture programs ran sequentially.

| Candidate | Seed | Before mean Step ms | Candidate mean Step ms |
|---|---:|---:|---:|
| Iterate existing employer worker lists | 0 | 12.34281 | 13.93300 |
| Reuse return buckets while retaining Citizen order | 0 | 12.34281 | 12.52134 |
| Reuse return buckets while retaining Citizen order | 1 | 15.31761 | 12.79479 |

The first candidate calculated shift start and weekday eligibility once per employer; the second
reconstructed shift start from the return bucket and calculated shift length once per worker.
Both preserve the compared 100,000-Citizen activity, allocations and end hashes. The roster
candidate also preserves the small seven-Day control's activity and hashes. Neither establishes
consistent two-seed Step benefit; both were removed without further refinement. These screening
results do not isolate why either candidate costs what it does. Full tails and memory readings
remain in the evidence.

`Simulation.PayrollStarting` now exposes the accrual boundary to optional diagnostics.
`ProfilePayroll` supplies `payrollWork` under `--profile-work`: a duplicate population scan
immediately before accrual, **not counters inside the implementation**. Its timings and allocations
are intrusive. `PayrollAccrualTests` compares each worker's wages and remainder per Tick through a
week, dismissal, rehiring, loss/restoration of premises, rebuilding, save/reload and changed night
shifts, wage rates and shift lengths. Exact-count and profiling-equivalence assertions cover the
observer and its removal after capture.

On the same machine/thread/runtime conditions, seed 0, resume Tick 16,384, warm to 18,432 and
measure through 20,480: **204,800,000 Citizen-slot visits, 44,923,890 AtWork visits and 44,644,836
OnDuty visits**. All AtWork visits have a declared employer and a commute bucket; no illness
refusal occurs. Distinct employers summed per Tick total 7,055,762. These are **Tick-summed visits,
not distinct people over the Day**, and OnDuty means entitlement, not wages paid. They form a
hypothesis about inactive-Citizen scanning; they establish neither its cost nor the benefit of
ordered AtWork membership. The next investigation above must settle the target before implementation.

Original, observer-disabled and counted captures match activity and end hash `853C7070AA58EE03`.
Original/observer-disabled mean Step costs are 14.41541/14.18522 ms, with equal allocations and
mixed tail differences: no speed or zero-overhead claim. No isolated consumer cost is filed in
`0013`. Commands, frozen binaries, rejected source, raw readings, regression logs and verified file
fingerprints are retained in `artifacts/aged-city-performance/20260908/payroll-evidence.tar.gz`,
with `payroll-checks.json` and `payroll-manifest.json`. The reproducible save is excluded, with its
fingerprint and command retained. Item 11 remains open.

Validation: all 2,971 assertions pass through `scripts/test.sh`; the complete log is retained in
the payroll evidence archive. This is the assertion lane, not the unfiltered milestone suite.

## Pinned repeatability and i5 attribution — 2026-09-08

Release .NET 10.0.11, Ubuntu 24.04, i5-10400, one simulation thread pinned to logical CPU 2;
100,000 Citizens, seed 0, `stress-shopping.toml`. Four sequential runs use the frozen payroll
observer build and identical Tick-16,384 checkpoint, warm to 18,432 and measure through 20,480.
Decide guard off. No builds/tests overlap these captures. This is a later Day than the original
14,336–16,384 control, and remains a desktop diagnostic.

| Repeat | Mean ms | p95 ms | p99 ms | Max ms | Step allocation bytes |
|---:|---:|---:|---:|---:|---:|
| 1 | 15.34530 | 31.3993 | 39.2874 | 97.7839 | 3,836,696 |
| 2 | 15.33742 | 32.0475 | 39.5920 | 96.8045 | 3,836,696 |
| 3 | 15.55748 | 32.2848 | 37.4439 | 92.4390 | 3,861,320 |
| 4 | 15.74976 | 33.1182 | 39.8975 | 100.9434 | 3,836,696 |

Activity and end hash `853C7070AA58EE03` match throughout; end invariants pass and no collections
occur. The extra 24,624 allocated bytes in repeat 3 remain unexplained. Mean range is 0.41233 ms,
2.69% of the minimum, not a confidence interval. `intel_pstate` remains in `powersave`, turbo enabled;
frequency is observed rather than locked. CPU 2's sibling, CPU 8, is busy 33.92–43.34% across each
whole run. Host samples include setup, warmup and ending checks, so they do not isolate interference
inside the measurement window. Affinity has not established quiet-machine repeatability.

A separate pinned trace of that frozen build matches activity/hash. It runs outside the sandbox,
which blocks diagnostic sockets. `summarize-simulation-profile.py` now excludes Linux's synthetic
`CPU_TIME` leaf, previously misreported as almost all self weight; the regression includes that
marker and the new payroll/commute scopes. Corrected Step weights:

| Function | Managed leaf % | Inclusive % |
|---|---:|---:|
| `WorkSchedule.Accrue` | 20.34 | 20.34 |
| `CommuteEngine.Generate` | 15.60 | 23.25 |
| `WalkScratch.Search` | 14.98 | 18.30 |
| `TripEngine.AdvanceTravellers` | 6.63 | 10.30 |
| `BuildingResidency.NthIn` | 3.36 | 3.36 |

Inclusive rows overlap. Inlining leaves payroll's scanning, schedule calculations and writes in one
frame; its leaf weight is not a scan price. Eliminating *all* payroll would imply roughly 3.14 ms
at the baseline median if sample shares represented elapsed costs. That is only an upper-bound
model, not an achievable saving. Existing boundary counts cannot divide it further. Payroll keeps
its priority, but no implementation target or speed improvement is established. Next: isolate those
subcosts and host interference before the paired, two-seed candidate gate.

`scripts/repeat-profile.py` preserves commands, input/assembly hashes, raw captures and host samples;
it checks activity/hash separately from allocation variation. `ProfileDump` now reports runtime
processor count and GC mode; these metadata fields postdate the frozen timing build. Validation:
16 `ProfileDump` assertions, attribution regressions, and replay of captured logs through the repeat
checker, including a deliberate hash mismatch. Evidence and verified per-file fingerprints are in
`artifacts/aged-city-performance/20260908/repeatability-evidence.tar.gz` and
`repeatability-manifest.json`; `repeatability-checks.json` retains the comparisons. No `0013` cost changes.

## Payroll scan boundaries — 2026-09-08

The contiguous scan is no longer the leading payroll hypothesis. `WorkSchedule.Accrue` repeats
`JobRuleset.ShiftLengthOf` for every entitled worker: `OnDuty` already computed it before returning
true. Investigate that avoidable calculation before replacing the scan with indirect membership.
Its isolated cost and a candidate's retained benefit are not yet established.

`-p:PayrollAttribution=true` compiles timing boundaries into actual accrual. Normal builds omit
them. `PayrollClock` samples every 67th Tick under `--profile-work`, separating schedule checks,
wage calculation/writes, and the residual loop/filter work. Empty-body boundary calibration runs
outside Step. The output identifies diagnostic builds; these are not throughput measurements.
An earlier `NoInlining` attempt produced no `OnDuty` samples despite a disassembled call, so it
could not separate the costs and was removed. The evidence retains that unsuccessful trace.

Release .NET 10.0.11, Ubuntu 24.04, i5-10400, one simulation thread pinned to CPU 2; seed 0,
100,000 Citizens, `stress-shopping.toml`, resume Tick 16,384, warm to 18,432, measure 2,048 Ticks,
Decide guard off. Captures run sequentially without overlapping builds/tests. Two fresh normal
controls have mean/p95/p99/max of **15.20798/31.2082/38.0982/101.3936 ms** and
**15.49167/32.6334/41.0062/129.8400 ms**, each allocating 3,836,696 Step bytes. Host isolation
remains incomplete: CPU 8, the sibling, is busy 38.50%/42.41% across the respective whole runs.
`powersave` and turbo remain active; these are desktop diagnostics, not budget ratification.

Each diagnostic repeat samples the same 30 Ticks, 3,000,000 Citizen-slot visits, 670,726 schedule
calls and 666,683 wage updates. Values below are **sums over those sampled Ticks**, not per-Tick
prices or whole-Day totals.

| Component | Repeat 1 actual / empty ms | Repeat 2 actual / empty ms |
|---|---:|---:|
| Schedule eligibility | 81.007 / 20.207 | 68.409 / 13.949 |
| Wage calculation and writes | 59.032 / 14.551 | 47.620 / 13.418 |
| Scan/filter residual | 47.907 / 30.232 | 40.950 / 27.211 |

Subtracting the empty calibration gives a **diagnostic model**, not an exact overhead correction:
roughly 49–53% schedule, 33–36% wage work and 13–14% residual. Calibration has different loop/cache
conditions; some individual residual differences are negative. Do not clip those differences or
translate these shares into predicted Step savings. The boundary counter pass also warms data
before accrual. Twelve sampled Ticks per repeat have no schedule calls at all: their 100,000-slot
scan medians are 0.215572/0.228322 ms. Those idle-Tick readings do not price an active-Tick scan.
Together, the observations lower the priority of a scan replacement; they do not price an index.

Both controls, the unsuccessful trace and both direct diagnostic captures preserve activity and
end hash `853C7070AA58EE03`; end invariants pass. Per-Tick payroll counts match between diagnostic
repeats, and sampled schedule/wage counts match the independent boundary probe. Diagnostic Step
allocations are 3,855,696/3,904,896 bytes; no collections occur. Diagnostic mean Steps are
18.69649/18.94488 ms and are not used for a performance comparison.

Validation: 18 focused payroll/profiling assertions pass in each final build configuration,
including boundary counts, timing partition, observer cleanup and city equivalence. The normal
build is restored. No simulation optimization is retained and `0013` is unchanged. Commands,
frozen builds, traces, raw samples, calibration, checks and source are retained in
`artifacts/aged-city-performance/20260908/payroll-scan-evidence.tar.gz`, with
`payroll-scan-checks.json` and verified per-file hashes in `payroll-scan-manifest.json`.

```sh
scripts/test.sh --filter 'tier!=instrument&(FullyQualifiedName~PayrollAccrual|FullyQualifiedName~ProfileDump)' -- -p:PayrollAttribution=true
# Freeze this build before restoring a normal build; --profile-work emits payrollTiming/calibration.
scripts/test.sh --filter 'tier!=instrument&(FullyQualifiedName~PayrollAccrual|FullyQualifiedName~ProfileDump)'
```


## Sale-Bin indexing and shift-length reuse — 2026-09-08

Neither candidate is retained. A `DistrictMarkets` lookup replacing Shopping's owner-Bin walk
passed 30 Shopping assertions but its seed-0 screen reduced mean Step only from 12.65337 to
12.54435 ms, below the observed noise, while adding 272,064 allocated Step bytes. Source and
regressions are archived; the index is removed.

A separate `WorkSchedule.OnDuty` overload returned its already-computed shift length to `Accrue`,
avoiding a second deterministic draw per entitled worker without persistent state. It passed
83 focused payroll, Shopping, replay, save/reload, derived rebuild and Civic assertions, plus three
payroll/profiling assertions in the diagnostic build. Its paired timings did not establish a
repeatable two-seed benefit, so it too is removed.

Release .NET 10.0.11, Ubuntu 24.04, i5-10400, one simulation thread pinned to CPU 2;
100,000 Citizens, `stress-shopping.toml`, resume Tick 16,384, warm to 18,432, measure 2,048 Ticks,
Decide guard off. Each cell is **mean / p95 / p99 / max Step milliseconds**. These desktop
comparisons are not quiet-machine budget ratification.

| Seed / order | Baseline | Shift-length reuse |
|---|---|---|
| 0 / candidate first | 12.47097 / 24.0665 / 26.5118 / 84.3628 | 12.17665 / 23.6203 / 25.9532 / 100.7181 |
| 0 / baseline first | 12.49709 / 24.3048 / 26.4313 / 83.7872 | 12.07077 / 23.4636 / 25.6443 / 83.4170 |
| 1 / baseline first, browser isolated | 13.41032 / 25.9013 / 28.5621 / 88.7227 | 12.69595 / 24.1936 / 27.6277 / 88.8538 |
| 1 / candidate first, browser isolated | 14.92344 / 27.4261 / 32.4203 / 86.8054 | 16.57375 / 36.9802 / 40.5514 / 129.1462 |

Six earlier seed-1 captures encountered changing host load and are preserved separately from these
pairs. Temporarily excluding Chrome threads from CPUs 2/8 did not make the final comparisons agree;
all affected affinities were restored, with no restoration errors or remaining restricted threads.
The contradictory result cannot establish either a code speedup or a code regression. Stop repeating
small candidates under these conditions; stable unchanged-build controls are the next requirement.

Activity and hashes match within each seed throughout: seed 0 `853C7070AA58EE03`, seed 1
`ABB33231E5EB4570`; end invariants pass. The first seed-0 candidate allocates 24,624 bytes more,
a variation also seen in unchanged controls earlier; other paired allocations match. No collections
occur. Raw captures, commands, host samples, frozen binaries, both discarded implementations and
test logs are in `artifacts/aged-city-performance/20260908/reuse-evidence.tar.gz`; checks and verified
fingerprints are in `reuse-checks.json` and `reuse-manifest.json`. `0013` is unchanged.

Restored-build validation: 24 payroll, profiling and corpus-budget assertions pass; a normal
profile smoke run reports attribution disabled and passing end invariants.
