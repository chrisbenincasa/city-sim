# Runtime composition evidence for plan 0075

## What was authored and exercised

[connected.toml](connected.toml) combines the shopping, wages,
income tax and shopfront rates of `taxing.toml` with the teaching trade, school, grant,
treasury and Life Stages of `funded.toml`. It omits taxing's four Policies, profit tax and
pollution emission Rule, retains its fixed shopfront rates Rule, and uses funded's floor
capacity settings. It adds education Need rates and the required Unplaced give-up duration.
There are three Resources, two trades, three Building kinds, five Bin Rules, two Zone Rules,
one Policy and five Life Stages in 24 top-level sections, in a 300-line file.

The connected circuit is Household Money → shopping purchase and carried sundries → grocer
Money → wages and taxes → Household and treasury → teaching grant → teachers' wages.
The school derives places from staffing. A single service placement costs 262,144 from an
opening treasury of 4,194,304. Its 96 Tiles of floor provide 32 places; its trade's occupied
floor share gives **five jobs**, not 32. Floor area divided by the global job density alone
would overestimate its payroll: occupancy and tenancy participate in the calculation.

The file is deliberately provisional. Shop stock still has an empty-input source, repairs
have no supplier, grocers have no bankruptcy threshold, and private construction is not
charged through a founding capital/Materials flow. The runner seeds land and residents.
None of these shortcuts is presented as a sustainable city or paid Outside supply.

[Program.cs](Program.cs) generates two variants from the single
source file. The first changes the teaching wage from 4,096 to 8,192; the second also changes
the daily per-job grant from 4,096 to 8,192. These are **one and two scalar edits**, respectively,
with no interpreter or production-source change. Variants are generated evidence, not three
independently maintained authoring files.

## Observations

All results below use seed 0, capacity for 1,000 Citizens, one routing worker, Debug,
.NET SDK 10.0.112, Ubuntu x86-64, Intel i5-10400. The instrument disables the Decide-write
hash guard. Runs overlapped development and shell activity; **no timing claim** is made.
Each run uses a `Populate` command at Tick 0 and a `Service` command at Tick 1 on Tile (67,64),
then empty input through Tick 114,688. Population sizing is fixture code, not a new Ruleset key.

| Reading at end of Day 56 | Baseline | Double wage only | Double wage and grant |
|---|---:|---:|---:|
| Live Households | 216 | 216 | 216 |
| Shopping purchases, cumulative | 7,926 | 7,909 | 7,926 |
| Sundries bought and delivered, cumulative | 536,383 | 534,702 | 536,383 |
| Payroll paid, cumulative | 5,386,937 | 4,667,681 | 6,427,321 |
| Payroll shortfall, cumulative | 7,496,691 | 7,896,657 | 7,517,171 |
| Treasury | 19,499,866 | 19,922,227 | 19,023,028 |
| Teaching grants, cumulative | 1,126,400 | 348,160 | 2,252,800 |
| School workers / jobs | 5 / 5 | 0 / 0 | 5 / 5 |
| School places | 32 | 0 | 32 |
| Bankruptcies, cumulative | 0 | 1 | 0 |
| Households with negative Sustenance | 73 | 76 | 73 |

The wage-only teaching business is gone in the **Day 19** reading; the school loses all
places. At the same point the city holds 7,772,418, so this is not treasury exhaustion.
Matching the grant keeps the school staffed for the observation window. Payroll shortfall
is summed across employers and payroll occasions, not outstanding debt: a continuing arrear
can appear in more than one shortfall reading. The grocers' missing bankruptcy threshold is
why their mounting shortfalls need not remove their Businesses.

**A solvent treasury does not establish viable Households or employers.** The baseline starts
with 360 Households, has no negative Sustenance at Day 21, then has 73 at Day 56 alongside its
large treasury. Life Stage dissolution also changes the population. Do not label the entire
360→216 fall a hunger Departure: this experiment did not attribute demographic losses by cause.

Treasury flow accounting leaves a residual: baseline 2,452,886 at Day 56, versus zero at
Day 28. `World.Dissolve` transfers estates to the treasury outside the eight reported flows;
`TreasuryFlows` documents this omission. The instrument preserves the residual rather than
quietly calling it tax income. Its exact per-source attribution was not measured here.

A separate existing `--school` run of the connected file for 28 Days found 85 attendance
occasions: 47 delivered and 38 with no school in the box; none were unreachable or full.
Children first appeared in its Day 23 reading. This dump populates directly before school
placement, so its trajectory is separate from the command-based three-way comparison.
It reports access and attendance but does not establish educational progression: the file
has no `[schooling]` pipeline.

Captured TOML comments retain the original unnumbered report filename so their recorded
content hashes remain valid. This numbered plan owns the report.

The raw daily trajectories, input variants and loader responses are in
[evidence/ruleset-viability/results](results/).

## Authoring friction and diagnostics

| Exercise | Current result | Implication |
|---|---|---|
| Add the funded Life Stages to the shopping economy | Loader refuses until `[placement] gives_up_after_days` is supplied | Useful cross-table, boundedness diagnosis; this was the first composition failure |
| Misspell a Building's trade | Refusal names the Building, trade and source line | Cross-reference errors are usable |
| Add a Good without a Hinterland price in a District world | Refusal names the Good and missing price | Structural price coverage is checked; supply is still a different question |
| Supply an unknown section | Loader refuses it with permitted sections | Runtime loader is authoritative; schema deliberately permits unknown keys |
| Double wages without increasing grants | Loads and runs; school loses staff and places | Legal underfunding needs a balance report, not a mandatory equality constraint |
| Read only school or only income dump | Each constructs its own world; neither provides the complete circuit | A connected report needed ordinary host code; headless dump composition is not a common-world report |
| Compare treasury against measured flows | Nonzero residual after dissolution begins | Include estate accounting in whole-circuit acceptance |
| Use the in-game tuner for a balance edit | `Main.Panels.cs` invokes `Regenerate` | It creates a new city; it is not the implemented Core hot-reload path |

The generated key descriptions cover individual units and omission behaviour, but do not
explain a whole content dependency graph. The school experiment required reading capacity,
Policy and wage code to distinguish declared posts, employed workers and places. Effective
Policy values must be read through `AmountOf`: a governed override survives reload, while an
ungoverned row falls through to the Ruleset. A raw `PolicyTable.Amount` read is not its grant.

A semantic census of the **49** shipped TOMLs (ignoring comments and key order) finds:

| Shared table | Files stating it | Distinct values | Largest identical group |
|---|---:|---:|---:|
| roads | 49 | 5 | 40 |
| layers | 49 | 2 | 47 |
| lots | 49 | 5 | 44 |
| capacity | 49 | 4 | 42 |
| placement | 49 | 9 | 30 |
| jobs | 49 | 2 | 48 |

This measures duplication, not wasted designer controls. Distinct demonstrations may need
identical settings. It supports avoiding copied whole fixtures, but does not test the complexity
inside one coherent Ruleset. It does **not** measure human editing time or decide the DSL question.

## Historical claims checked

- **0050:** its 169-key/31-file count is a historical snapshot. `RefuseRetired` and schema
  tests now distinguish accepted keys from retired-name refusals; the eight advertised
  tombstones are not an outstanding prerequisite. `provisioned.toml`'s header still says
  Pool purchasing throws, but current `RuleEngine.Buy` selects sellers and pays them, and
  the connected experiment exercises purchases. Read code and results, not that old header.
- **ADR 0164:** the designer-facing test still applies. No new fixture-size key is needed.
  Repeated settings alone do not make a key scaffolding. Its statement that zoning has no
  production caller is obsolete; the shell's zoning command is implemented.
- **Deferred DSL:** the interpreter boundary still permits another input format, but the
  observed problems are copied content, cross-mechanism understanding and absent mechanisms.
  A parser would not supply those mechanisms. The connected file was assembled with implementation knowledge; its
  length does not establish whether a human can author or maintain the game.

## Concrete prerequisites and ownership

| Required outcome | Evidence and acceptance | Owning board work |
|---|---|---|
| Paid founding supply with a Money counterparty | `Scope` has local/pool/global/map; `Buy` waits with no local seller, and `DistrictMarkets` indexes city Businesses. Hinterland prices do not create stock. Demonstrate a paid delivery from Outside through a gate and a recurring way to finance it; do not relabel empty-input shop stock an import. Full Office/agglomeration is not automatically required for the smallest founding scenario. | Freight and Outside trade; first playable founding loop selects its minimal supply/funding contract |
| Labour affects private production where content claims it | Current readouts are occupancy, balance, emission and declared jobs. A fixed empty-input stock Rule is not gated by employed/present workers. Define the labour contract and demonstrate output/earnings responding to staff loss and recovery. | Private production and labour |
| Ground-to-residents founding flow | Seeded `Populate` is not player founding. Opening treasury alone does not fund private capital, Materials or pay periods. Demonstrate first roads/homes, a usable gate, affordable housing, jobs and supplies with the same Ruleset. | First playable founding loop, coordinating with row 31 and construction/capital work |
| Coherent arrivals and demographic horizon | 0073 owns autonomous prospects, Outside stock/recovery, income/rent affordability, queue/quota and gate commands. Integrate its completed implementation; do not use old `welcomed.toml` arrivals as proof. Tune sufficient housing/job slack and opening purse against consumption/pay cadence. | Row 31, then first playable founding loop |
| Explain connected balance outcomes | Report payroll, employer solvency, purchases/deliveries, Needs, grant coverage, estate transfers and treasury flows in one world. Demonstrate shortage and player-led recovery, with replay/save checks. | Whole-money-circuit acceptance; first playable founding loop |

The author-facing recommendation and its evidence are in [plan 0075](../../0075-base-game-ruleset-viability.md).
The existing Ruleset-authoring and tuner entries own implementation and product integration.
No conclusion here requires building every deferred city system or the complete production tree.

## Reproduction and validation

From the repository root:

```sh
dotnet build plans/evidence/ruleset-viability/Viability.csproj -m:1 -nr:false -p:NuGetAudit=false
dotnet plans/evidence/ruleset-viability/bin/Debug/net10.0/Viability.dll
dotnet plans/evidence/ruleset-viability/bin/Debug/net10.0/Viability.dll plans/evidence/ruleset-viability --reload
```

`NuGetAudit=false` is an offline restore workaround for this capture, not a repository setting.
The experiment reads one TOML, generates the two variants and fixed-length CSVs, and exits
nonzero on a failed invariant or persistence check. Existing headless command used separately:

```sh
dotnet src/Borough.Headless/bin/Debug/net10.0/Borough.Headless.dll --school \
  --ruleset plans/evidence/ruleset-viability/connected.toml --citizens 1000 --ticks 57344 --schools 1
```

Each variant passes end-of-run invariants at 56 Days. Its Day-7 State Hash matches a fresh
replay of Populate/Service and a save/reload (17,447,385 bytes). The saved continuation and
fresh replay agree every Tick through Day 8. These checks do not implement the explicitly
unbuilt Goods-conservation or exclusive Citizen-location invariants.

The working lane passed **3,399 tests**, zero failures/skips, using
`scripts/test.sh -- --no-restore -m:1 -nr:false`; retained log:
`/tmp/borough-test-20260912-153719.log`. Headless, Godot Debug and the research instrument
builds passed. No existing gameplay or golden Ruleset was edited; no golden re-record is needed.
The full instrument suite was not run: this investigation is not a playable milestone.

Two further transitions were encoded with `InputLogCodec`: wage-only at Day 2 and matched
grant at Day 4. Both applied, the effective wage and grant were checked, and two replays
matched every 64 Ticks through Day 8 with end-of-run invariants. The retained
[Input Log](results/reload.borough) names all three content hashes;
[reload.txt](results/reload.txt) holds the final hash and result.
This exercises the session path, not the specialised dumps' known reload omission.

## Driven observation

Built Godot Debug, then ran on the local X11 display using Vulkan Forward+, GTX 1080,
2,560 × 1,371 viewport. Reproduce the retained short script from the repository root:

```sh
mkdir -p /tmp/viability-drive
dotnet build src/Borough.Godot --no-restore -m:1 -nr:false
godot --path src/Borough.Godot -- --ruleset plans/evidence/ruleset-viability/connected.toml \
  --citizens 1000 --start-at 1 --drive plans/evidence/ruleset-viability/school.drive
```

The script places the school at Tick 1 and selects its interior at Tile (69,69).
[At Tick 2](results/placed.txt), it has one teaching Business,
zero of five posts filled and zero places on a floor built for 32. At Tick 2,304 the
[readout](results/staffed.txt) and
[screenshot](results/staffed.png) show five of five posts filled
and 32 places. The budget strip reads 3,944,448; the readout also reports eight shopping
Households and 192 Goods being carried. The [compressed draw list](results/staffed.tsv.gz)
contains 52 Buildings, matching the readout; Building 49 is the school. No asset was authored.

What observation exposed: selecting the frontage Tile picks the Street; selecting inside
its drawn footprint reaches the school. More substantively, the new unstaffed school’s
headline says “No supply shortfalls reported” while its teaching section correctly reports
zero places and zero staff. The section explains the staffing consequence, but the headline
alone is not a whole-service health diagnosis. Row 30/information work should retain this
case when extending cause summaries. No children attend this early; attendance was checked
separately in the 28-Day school dump rather than inferred from staffed capacity.
