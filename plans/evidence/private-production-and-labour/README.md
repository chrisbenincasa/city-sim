# Labour deposit and shelf-life measurements

Measured on 2026-09-21 America/New_York (2026-09-22 02:08–02:21 UTC) for PR #16, against `0009e3d4e895f5bcb4f65378880267e3af061d01`.
Production code, shipped Rulesets and goldens were unchanged. The reproducible instrument is
[`spikes/LabourCost`](../../../spikes/LabourCost/README.md); raw samples accompany this report.

## Questions and limits

1. What does adding a fractional labour deposit to `WorkSchedule.Accrue` cost?
2. What do N expiry buckets cost at the Bin counts and allocation capacities this build actually has?

The CPU experiment is an isolated wage-pass comparison with a prototype deposit, not a complete
labour implementation or a whole-Tick benchmark. It includes integer productivity grading,
a per-Citizen remainder, a cached Business-to-Bin lookup, `World.Deposit`, and (in the expiry mode)
lazy bucket ageing, `World.Withdraw` and a discarded-unit counter. It excludes populated wait
lists, consequent Rule evaluations, production purchases, save/hash work and final Ruleset lookup.
The non-market proxy uses the existing Money Resource; it is not implementing the new family.
The instrument rate is 4097 units per Day before grading, deliberately high enough for every
on-duty worker to deposit whole units on every pass. Lower authored rates can skip many whole-unit
deposits while still updating remainders; those content choices were not swept. A cached handle
is a favourable lookup assumption. Actual final integration must be measured again.

No independent CPU or memory allocation has been assigned to labour in the design. Thus there is
no invented pass threshold. The existing target is **15.6 ms for the entire Tick at one million
Citizens, on one core of an i5-10400-class machine**. A marginal cost can be compared to that target;
a marginal cost below it does not establish that the complete Tick fits.

## Conditions and method

Intel Core i5-10400, Linux x86-64, .NET 10.0.12 / SDK 10.0.112, Release,
one thread pinned to logical CPU 2 (SMT sibling 8), workstation GC, JIT tiering disabled.
The `powersave` governor and turbo were left as found. The host was not reserved or frequency
locked. CPU frequency and per-CPU load are retained in the host captures; these are **desktop
diagnostics, not quiet-machine capacity certification**. No test suite or second benchmark ran
alongside the capture. Across complete capture processes, CPU 2’s SMT sibling averaged 4.4–7.0% busy and the other
CPUs averaged 5.2–7.1% busy. Sampled CPU 2 frequency ranged from 0.8 to 4.29 GHz. These figures
include setup and are not instantaneous measurements of the timed functions. Machine details,
source fingerprints and runtime details are in `machine.json`.

All worlds use seed 0 and the unchanged `rulesets/stress-shopping.toml`, SHA-256
`2DE63BECD3932F2BBBBA77163F32B2CA4CDE0DFFC688A95F0DED9676F52914B8`.
The 10,000-Citizen world was stepped to Tick 15,360 (7.5 Days), then its event queues were drained
and end invariants passed. Its saved State Hash is `47B065FBF0FBD7CA`. The 100,000 and one-million
worlds were generated but **not aged**. They isolate scaling in actual engine tables; they do not
represent balanced mature cities.

The aged world first measures its observed roster. Controlled variants then set 0%, 25%, 50% or
100% of Citizens to `AtWork`, scattering their workplace handles across live Businesses. They
retain the real illness and work-schedule predicates at Tick phase 1024 (17:00, not noon, because
the Day begins at 05:00). Their reported on-duty counts are the actual deposit multiplicands;
the control percentages are not employment rates. No whole-world simulation is stepped in these
constructed employment states.

Nine paired batches alternate measurement order. Each measures at least 64 passes (200 at 10,000),
with wages and prototype state restored outside timing. Expiry advances every pass while the
schedule remains frozen. The four-bucket prototype uses 32-Tick cycles and a 128-Tick lifetime;
seeded age buckets ensure expiration is exercised in every batch. The control copy of the wage
pass has a no-op deposit call, exposing copying/branch/code-generation overhead. Tables below use
the **median of within-pair differences**, with their full range; these are not confidence intervals.
Warmup, setup, counters and output are excluded. Each batch records GC collections and allocations.

Self-checks establish identical wage and wage-remainder outputs between original and prototype,
valid fractional remainders, bucket totals equal to Bin levels, and deposited = retained + expired
across 257 Ticks. These validate the instrument, not the plan's gameplay acceptance checks.

## CPU results

| World / roster | On-duty workers | Baseline ms | Deposit only: extra ms | Deposit + expiry: extra ms (range) | Extra / 15.6 ms target |
|---|---:|---:|---:|---:|---:|
| aged-10000 / observed | 1,974 | 0.292 | 0.072 | 0.084 (0.073–0.087) | 0.5% |
| generated-100000 / controlled-25 | 6,488 | 2.768 | 0.410 | 0.459 (0.232–0.592) | 2.9% |
| generated-100000 / controlled-50 | 13,015 | 5.101 | 0.872 | 1.135 (0.865–1.362) | 7.3% |
| generated-100000 / controlled-100 | 26,285 | 9.991 | 1.768 | 2.229 (2.027–3.000) | 14.3% |
| generated-1000000 / controlled-25 | 64,673 | 32.429 | 7.299 | 11.650 (9.667–12.398) | 74.7% |
| generated-1000000 / controlled-50 | 129,427 | 59.715 | 12.645 | 20.798 (18.681–23.202) | 133.3% |
| generated-1000000 / controlled-100 | 259,340 | 109.551 | 25.999 | 40.818 (38.151–45.004) | 261.7% |

All **468 timed batches** reported **zero allocated bytes and zero GC collections**. The
zero-worker controls are near zero marginal cost. The complete clone-control overheads,
paired samples and all thirteen workloads are retained in `summary.json`.

At 64,673 workers, the no-op copy adds -0.041 ms median (range -1.013–1.190).
At 129,427 workers, the no-op copy adds 0.223 ms median (range -2.268–1.126).
At 259,340 workers, the no-op copy adds 1.995 ms median (range 1.187–3.066).

The observed small-world expiry increment is about 43 ns per on-duty worker; the first
million-Citizen staffed case is about 180 ns. Working-set growth matters: multiplying the
small-world figure by a guessed million-city workforce would understate the measured cost.

## Storage results

`BinTable` currently has **83 bytes per row**, including allocator columns; 71 bytes are saved.
A fixed inline array of N 64-bit quantities plus one 64-bit expiry clock adds `8 × (N + 1)` bytes.
The engine uses separate column arrays: this is summed column width, not a padded array-of-structs
row size. Fifteen of sixteen allocation readings equal payload plus 48 bytes for the two array
headers, including every four-bucket reading. The million-Citizen eight-bucket reading contains an
additional 7,424 bytes in the thread allocation counter (0.012% of payload); this instrument does
not attribute those bytes. The raw result is retained. Column payload below is derived from the
measured element widths and allocated slot counts, not from that counter’s incidental overhead.

| Buckets | Extra bytes/Bin | Increase over 83-byte row | At 900,000 allocated Bins |
|---|---:|---:|---:|
| 2 | 24 | 28.9% | 20.60 MiB |
| 4 | 40 | 48.2% | 34.33 MiB |
| 8 | 72 | 86.7% | 61.80 MiB |
| 16 | 136 | 163.9% | 116.73 MiB |

Those numbers cover existing Bin capacity only. Labour needs its own Bins and a saved per-Citizen
remainder. The measured census is:

| World | Live Bins | Allocated Bin slots | Live Businesses | Existing total table payload |
|---|---:|---:|---:|---:|
| Aged 10,000, before saving | 8,985 | 9,000 | 869 | 63.73 MiB |
| Generated 100,000 | 89,204 | 90,000 | 8,481 | 93.68 MiB |
| Generated 1,000,000 | 891,597 | 900,000 | 84,670 | 416.79 MiB |

The restored aged checkpoint has capacity 8,985, rather than 9,000; its census is also retained.
This is why live counts, slot counts and allocated capacity must not be used interchangeably.

At one million, **one labour Bin per Business** raises the slot requirement to 976,267. With the
current doubling allocator, capacity becomes 1,800,000. For N = 4:

- Existing 83-byte columns' additional capacity: **71.24 MiB**.
- Buckets plus clocks across the enlarged capacity: **68.66 MiB**.
- One 64-bit remainder per allocated Citizen: **7.63 MiB**.
- Total additional column payload: **147.53 MiB**, **35.4%** of the starting table payload.

This is a content scenario, not a prediction that every Business will produce. The boundary is
crossed after **8,404 additional Bins**, about 9.9% of this world's Businesses. With exact initial
sizing rather than doubling, the same live-slot scenario would add about **50.91 MiB** including
remainders. Sizing and expiry representation are separate costs.

With the same all-Business scenario and current allocator, N = 2/4/8/16 adds respectively
**120.07 / 147.53 / 202.47 / 312.33 MiB** at one million. Additional Business handles, waste counters,
object headers, transient allocations during growth and other eventual implementation columns are
not included. Raw arithmetic and array-allocation measurements are in the census and memory rows.

For perspective, four buckets plus a clock only on the 84,670 new labour Bins would have **3.23 MiB**
of live payload, before indexing, allocator columns and headroom. If all existing Goods also perish,
there are 446,926 Goods Bins to include, making that **20.28 MiB** for Goods plus labour at live count.
Neither is a measured sparse implementation or a measured perishable-resource mix. They show that
paying for every allocated Bin is a material representation choice.

## Recommendation

Do not classify the proposed per-worker deposit path as affordable against the million-Citizen
Tick target on the strength of the existing scan. Even 64,673 depositing workers add 11.65 ms;
129,427 add 20.80 ms in this prototype. Background load and the simplified integration prevent
precise capacity certification, but every paired result in those cases is a substantial cost.
The whole wage pass already exceeds the target at these constructed workloads; these results
must not be presented as a new complete-Tick budget or added to old incomparable timings.

**Next CPU experiment:** keep individual fractional accrual and per-Tick attendance response,
but combine deposits per Business within the Tick and measure the flush, indexing and wake-up
cost. Preserve when waiting Rules become eligible; aggregation must not silently change their
ordering or throughput. Also exercise the intended authored deposit rate, since this run deliberately
makes every on-duty worker deposit whole units every Tick. Switching to one deposit per Day would
change gameplay and is not warranted by these measurements alone.

**Storage:** reject the premise that unconditional buckets are noise. Compare a compact store for
expiring Bins and sensible initial Bin sizing before choosing the layout. Four buckets are a
measured candidate, not a selected precision requirement. The memory result is exact for these
counts; the content mix and future layout remain choices.

The requested measurements are complete. The implementation strategy still needs those choices;
the report does not claim production, shelf life or their gameplay acceptance checks are built.

## Reproduction and artifacts

Use the commands in the [instrument README](../../../spikes/LabourCost/README.md).
`age-10000.jsonl` records ageing, pre-save census and the invariant/hash result. `aged-10000.jsonl`
and `generated-*.jsonl` retain every timing, allocation, census and instrument-check result.
`summary.json` retains paired deltas, ranges, per-worker arithmetic and budget comparisons.
Host samples preserve process-wide capture conditions, including setup; they are not synchronized
function-level traces. `machine.json` identifies the CPU, runtime and source hashes.

The checkpoint is included as `aged-10000.borough.gz` and can also be regenerated by the documented
command. Host captures are compressed as `*-host.jsonl.gz`; `host-summary.json` records aggregate
load/frequency conditions. `SHA256SUMS` covers the evidence files. The measurements introduce no runtime
feature, so no visual demonstration or golden re-record is needed.

## Validation

- `dotnet build spikes/LabourCost -c Release --no-restore --nologo -m:1 -nr:false`: passed, no warnings or errors.
- Instrument wage/remainder and expiry checks: passed in all thirteen workloads.
- Aged city end invariants: passed before checkpoint capture.
- `scripts/test.sh`: **3,969 passed**, zero failures/skips; complete runner output in `validation.txt`.
- Harness whitespace verification: passed (Roslyn emitted a workspace-loading warning).
- Captured source fingerprints, raw batch counts and archived checkpoint bytes verified.

The allocation-counter outlier described above remains visible in the raw evidence. No production
code, Ruleset content, save schema or golden fixture changed.
