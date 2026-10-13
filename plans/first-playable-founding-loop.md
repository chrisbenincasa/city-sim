# First playable founding loop

Status: built. All six slices are merged or in review, and every acceptance check below is met.
Builds on: [0073 arrivals](0073-the-city-attracts-people.md), [0070 city spending](0070-the-city-spends.md),
[labour-bound production](private-production-and-labour.md) (PR #105), [0075 viability](0075-base-game-ruleset-viability.md).

## Outcome

One Ruleset, the base package in `rulesets/base/`, plays a whole loop from bare Ground:

1. The player lays Streets, zones housing and work, and places a gate and a school.
2. Families arrive, find homes and jobs, and buy what local Businesses make.
3. The city keeps running for months on its own Money circuit.
4. A shortage arises from growth. The player sees its cause, acts, and the city recovers.

Replay and save/reload hold throughout. Completion does not require every planned city system.

## Current behaviour (checked 10/05/2026 at `ef405f1c`)

| Stage | Exists | Missing |
|---|---|---|
| Start | Shell defaults to `rulesets/base/ruleset.toml`. "New empty city" steps `Ground` (`CityPreparation.cs`) | The shell never reads `founding.borough`, which only `BaseFoundingPackageTests` and Headless `--log` use |
| Arrival | A gate plus zoned vacant Lots is enough. `HinterlandEngine` admits families, and the housing Zone Rule builds a free `dwelling` while the Unplaced Pool is non-empty | — |
| Supply | `restock` has `inputs = []`, so every dwelling makes its own `sundries` for free | Production, shop sales and Household purchases. Base declares no `[shopping]`, `pool` term or Hinterland `prices` |
| Business Money | Wages, arrears and bankruptcy (`WageEngine`). `[[business]] opening_grant` pays a zone-raised Business out of the treasury (slice 3a) | Base declares no grant, so its zone-raised Businesses open at zero balance. The `shop` trade posts no wage and buys nothing |
| City Money | Opening treasury 4,194,304. School `placement_cost` 262,144. `school_funding` grant per job. Demolition paid to the displaced | Income. `ruleset.toml` says "the city has no income". `[income_tax]` and `[business_tax]` exist only in `taxed.toml`/`taxing.toml`. Road upkeep code exists (`SpendOnUpkeep`) but no shipped Ruleset sets it |
| Shortage | `StarvedSince` clock, `SupplyEvidence`, the inspector's "Waiting for X" line, the city evidence panel and the trouble layer | Staffing, school places and jobs have counters but no elapsed episode. No notification surface |
| Recovery | Zone, Street, Service, Gate, Govern, Fund, Tax and Demolish all reach the shell. `scripts/ui/check-diagnosis.py` shows a player-led recovery on a `taxed.toml` fixture | No shortage-then-recovery run on base |
| Verification | `BaseFoundingPackageTests` replays `founding.borough` twice to Day 12 and matches daily State Hashes. A Day 6 save reloads and matches the uninterrupted run to Day 12. `TreasuryFromAFileTests` sweeps base with the top-level files | No run longer than 12 Days |

PR #105 ships `milled.toml`. In it, mills turn labour into flour, and grocers bake flour into sundries
for Households. Its trade fades after about two weeks. Businesses open with no Money, and Money then
collects in mill balances with no way out. The founding loop inherits this problem the moment base adopts that chain.

## Boundaries

In scope:

- Base package content and the mechanisms the loop needs to close its Money circuit.
- Persistence and replay coverage for base.
- One shortage with a player-led recovery, demonstrated headless and driven in the shell.

Out of scope:

| Work | Owner |
|---|---|
| Exports and Money entering from outside | [export-base](export-base.md) slice 2 |
| Charging for Streets and zoning | Infrastructure construction, wear and renewal |
| Faster construction gestures and rezoning | Player construction tools |
| Freeform Streets and road-aligned Lots | Freeform local Streets |
| Notifications | Trajectory notifications and persistent Pins |
| Integrated execution replacing the Ruleset lowering | [0077](0077-ruleset-source-loading.md) item 3. It gates the first *release*, not this loop |
| Balance | Playtesting and content calibration. Numbers here are provisional |

## Decisions (10/05/2026)

| # | Question | Options | Decided |
|---|---|---|---|
| D1 | Where do sundries come from? | (a) The labour chain from `milled.toml`: mill → flour → shop bakes → Households buy. (b) Priced imports only | (a), with a priced import at the gate edge as the expensive fallback. Imports alone drain Money every purchase and make the city a pass-through |
| D2 | Where does a zone-raised Business get its opening Money? | (a) The treasury pays a Ruleset-declared opening grant, refused when the treasury is short. (b) A founder Household capitalises it, as `Found` does. (c) New Money is minted at opening | (a). It conserves Money and gives the treasury a growth cost the player feels. (b) needs zoning and founding to be reconciled. (c) adds a Money door outside export-base |
| D3 | How does Business surplus return to circulation? | (a) `[business_tax]` to the treasury. (b) A dividend to an owner Household. (c) Leave it and accept accumulation | (a). It exists, it is daily, and it closes the loop through school funding and wages. Check in the long run whether tills stay bounded. Revisit with private capital |
| D4 | Which shortage does the acceptance run show? | (a) Treasury squeeze. Growth raises school funding past tax income, teachers go unpaid, and the school closes places. The fix is Tax or Fund. (b) Supply break. A cut Street starves shops. The fix is relaying the Street. (c) School places. Growth fills the school. The fix is placing a second one | (a), restated 10/12/2026: growth raises the treasury's outflows, not school funding. School funding is fixed by floor area. The housed rebate grows with the city, and the school grant is paid after it, so a short treasury stops the grant first. The fix is `govern` on the rebate, because `fund` accepts only subsidy Policies. (b) is a player-caused break. (c) needs a school-place episode record that does not exist |
| D5 | Road upkeep in base? | On / off | On. The code exists. It gives the treasury a cost that scales with the network |

D1 to D3 fix the Money circuit that slices 2–4 build. D4 fixes the shortage that slice 5 demonstrates.

## Slices

Each slice is one PR. [Execution](#execution) gives the order and which slices run in parallel.

1. **Base in the verification lanes.** Built. Add `rulesets/base/` to the all-Ruleset sweeps. Replay
   `founding.borough` twice and compare State Hashes. Save at Day 6, reload and continue to
   Day 12, and match the uninterrupted run. Needs nothing else.
2. **Production chain in base (D1).** Built. Every dwelling hosts a `mill`.
   A trade zone (bit 1) raises `shopfront` Buildings, each hosting a `grocer` that buys flour through
   the District Pool and bakes sundries. `restock` draws from the Pool. Every gate prices sundries,
   flour and repairs. `founding.borough` zones blocks (1,1), (2,2) and (3,0) for trade.
   `BaseFoundingPackageTests` asserts that grocers open and that mills and grocers both earn revenue
   by Day 12. In a Day 30 replay, Households grow to 624 and Businesses to 147. Business balances
   peak at 1,533,350 and end at 1,074,631. Jobs run out from about Day 18, and 87 seekers find no
   vacancy by Day 30. Slices 3b and 4 own both effects.
3a. **Opening grant (D2). Built.** `[[business]] opening_grant` is the Money the treasury pays a
   Business that a Zone Rule opens, on a raised Building or in a vacant Unit. `World.CreateBusiness`
   moves it from the treasury Bin to the Business's balance. A treasury short of the grant opens no
   Business, and the Building stands without its trade. `Found` and placed Buildings receive none.
   The budget panel and the Census count it as the `opening grant` flow. Absent pays nothing, and no
   golden or shipped Ruleset hash moved. `rulesets/granted.toml` and `OpeningGrantTests` prove it.
3b. **City income in base (D3, D5). Built.** Base turns on 3a's grant and adds `[business_tax]`,
   `[income_tax]` and road upkeep. Every number is provisional, and slice 4 tunes them.
   [City income record](#city-income-record-slice-3b) has the 120-Day run.
4. **Continued operation.** Tune provisional numbers until the 120-Day run meets the operation
   checks below, and confirm the result holds to Day 200. Record the run's command, seed and
   measurements in this plan. Built; [slice 4 record](#slice-4-record).
5. **Shortage and recovery (D4).** Built; [slice 5 record](#slice-5-record). A headless test pairs
   an intervention run with a control. It samples the school each Day and defines no episode,
   because row 30 (decline and recovery) owns failure-duration semantics.
6. **Driven demonstration.** Built; [slice 6 record](#slice-6-record). `scripts/ui/founding.drive`
   founds the city from "New empty city" with the shell's tools, then shows the shortage, the
   player's action and the recovery. Its Input Log is `founding.borough`, which is re-recorded with
   `street` commands for it.

## City income record (slice 3b)

Provisional numbers, as shipped:

| Key | Value | Anchor |
|---|---|---|
| `mill` `opening_grant` | 2,048 | About one worker-week at 256 a Day |
| `grocer` `opening_grant` | 32,768 | About one worker-week at 4,096 a Day |
| `[income_tax]` | allowance 512, upper threshold 8,192, rates 10% / 20% | Mill wages untaxed; grocer and teacher wages in the middle band |
| `[business_tax]` | threshold 16,384, rates 10% / 20% | Threshold is four grocer day-wages; rates from `taxing.toml` |
| `[roads] upkeep_per_segment_per_day` | 128 | 40 Segments cost 5,120 a Day |

Run:

- Log `rulesets/base/founding.borough`, seed `0x135283C`, 120 Days (245,760 Ticks), at `founding-3b`.
- Command: `dotnet spikes/FoundingProbe/bin/Release/net10.0/FoundingProbe.dll rulesets/base/ruleset.toml rulesets/base/founding.borough 120 30`.
  [The probe's README](../spikes/FoundingProbe/README.md) explains the build and the columns.
- End-of-run invariants hold at Day 120. Save at Day 30, reload and continue matches all 120 daily
  State Hashes.

| Day | Treasury | Business tills | Household balances | Households | Employed | Businesses | Mills | Grocers |
|---|---|---|---|---|---|---|---|---|
| 10 | 3,084,020 | 1,646,367 | 202,838 | 321 | 374 | 114 | 93 | 20 |
| 20 | 2,681,097 | 2,281,070 | 535,956 | 511 | 589 | 129 | 100 | 28 |
| 30 | 2,058,010 | 2,070,611 | 1,682,003 | 624 | 711 | 147 | 102 | 44 |
| 40 | 1,533,427 | 1,445,965 | 3,097,746 | 717 | 737 | 148 | 89 | 58 |
| 50 | 1,072,402 | 1,045,478 | 4,121,547 | 785 | 619 | 118 | 50 | 67 |
| 60 | 135,662 | 1,468,392 | 4,813,113 | 844 | 521 | 101 | 26 | 74 |
| 70 | 0 | 1,257,757 | 5,234,095 | 870 | 458 | 88 | 25 | 62 |
| 80 | 11,690 | 1,349,544 | 5,315,523 | 896 | 343 | 66 | 25 | 40 |
| 90 | 7,687 | 1,734,257 | 5,188,614 | 931 | 287 | 55 | 25 | 29 |
| 100 | 0 | 1,228,012 | 5,885,920 | 970 | 175 | 34 | 25 | 8 |
| 110 | 0 | 1,299,991 | 5,910,235 | 1,009 | 181 | 35 | 25 | 9 |
| 120 | 2,745 | 1,962,337 | 5,015,434 | 1,027 | 169 | 33 | 25 | 7 |

Treasury flows over 120 Days:

| Flow | Total |
|---|---|
| Opening grants | 3,846,144 out |
| School funding (policy) | 1,781,760 out |
| Road upkeep | 538,653 out |
| School placement | 262,144 out |
| Income tax | 1,057,086 in |
| Business tax | 399,050 in |

Findings for slice 4:

- Opening grants empty the treasury on Day 63. Grocers close and reopen, and each opening draws a
  new grant. Afterwards the treasury lives on each Day's taxes. It pays school funding on some Days
  and almost no grants, and employment falls from 737 at Day 40 to 169 at Day 120.
- Mills fall from 102 at Day 30 to 25 by Day 70, as in slice 2. Base's flour trade fades as
  `milled.toml`'s does.
- Business tills do not settle. They fall to about 1.0M by Day 50 and climb back to 1.96M by Day 120,
  while the Businesses holding them drop from 148 to 33.
- Household balances rise from 0.2M to 5.0M and hold most of the city's Money after Day 50.

## Slice 4 record

Base runs 400 Days of the founding log with every stock bounded. Unpaid wages settle near 7%
rather than zero.

### Engine changes

Each change has a regression test that fails without it.

| Commit | Change |
|---|---|
| `1d52a634` | Unaffordable and Closed outings raised shops that could serve them no better |
| `7046cabb` | A buyer priced out of the import slept on its till and missed a city seller's restock |
| `33dea1ea` | A driver's own parked car counted as taking its space, so it could not leave a full area |
| `5916da9d` | Discovery walked a shared building's tenants in slot order, hiding later grocers from every shopper |
| `4b06838d` | Founding drew the treasury-funded `teaching` trade about a third of the time |
| `a5553cbb` | Unpaid workers never left. `quits_after_days_unpaid` and its hiring rule ([`adr/0175`](../docs/adr/0175-unpaid-workers-quit-and-a-trade-hires-only-what-it-can-pay.md)) |
| `6ddf8475` | A Business pays its owner what its till holds above a reserve ([`adr/0176`](../docs/adr/0176-a-founded-business-pays-its-owner-above-a-reserve.md)) |
| `e9003934` | A Business with no owner draws a housed Household on its first payday with a surplus |
| `05db4b20` | A `housed` Readout lets a Policy pay housed Households only |

`33dea1ea`, `a5553cbb` and `6ddf8475` re-baseline the golden traces.

### Base content

Changes from slice 3b's base:

- `mill` makes one flour per application, and `grind` is gone. `buy_flour` moves 4 flour.
- Parking 6 floor tiles a space. Shop flour bin 8, sundries bin 256, dwelling flour bin 128.
- Mill and grocer opening grants 8,192. Every wage 1,024, paid daily. School funding 1,024 per
  declared job, weekly as before.
- `labour_per_day` 16. Trade Zone Rule threshold 2 and cooldown 2 Days.
- Workers quit after 3 unpaid Days. Mill and grocer pay their owner above 2 Days' wages.
- `[founding]` band 8,192, reconsidered every 8,192 Ticks.
- A 5% daily levy on Business and Household balances, and a 384 daily rebate to housed Households.

### Measurements

Founding log, seed `0x135283C`, committed base, 400 Days. Command:
`spikes/FoundingProbe/tuning/tune.sh final 400 ""`, then `summary.sh final`.

| Measure | Result |
|---|---|
| Employed, Days 30–120 | 731–968 |
| Households, Days 30–120 | 645–808 |
| Businesses, Days 30–120 | 229–344 |
| Employed, Days 30–200 | 731–969 |
| Business tills, Days 60–400 | 2.1M–2.8M, no trend |
| Treasury, Days 60–400 | 0.8M–2.2M, no trend |
| Unpaid share of wages | 13.4% Days 1–100, 9.3% Days 101–200, 7.1% Days 301–400 |
| Owner draws | 84.5M over 400 Days |
| Bankruptcies | 95 |

### Findings

- Without levies and rebate, income tax piles up in the treasury and employment collapses. With
  a rebate to every Household, unhoused arrivals take it with them when they emigrate, and the
  money supply fell from 6M to 3.1M. The housed-only rebate holds it at 5.3M–6.0M.
- Slower trade openings (threshold 3, cooldown 7) leave 364 employed at Day 40 against 792.
- A founded grocer that takes a vacant unit late is known to no Household. A Household with a
  full list of working shops hears of a new one only after its own shops fail, so late entrants
  sell little. This likely feeds the remaining unpaid wages and is not addressed here.

## Slice 5 record

Growth empties the treasury by Day 35, and the school has no teachers by Day 45. Governing the
housed rebate from 640 to 384 on Day 45 restores all 5 teachers within 15 Days. Without the
command the school stays short through Day 400.

### Engine changes

| Change | Test |
|---|---|
| A service trade that goes bankrupt reopens at once in its Building, with an empty till. Before, nothing recreated it, and a founded mill or grocer could take the school's unit | `A_school_whose_trade_went_bankrupt_opens_it_again_empty` |
| The Policy engine reports which Policies the treasury ran short on at their last firing. This is engine memory, not saved state, so a reloaded world reads false until each Policy next fires | `A_rebate_the_treasury_cannot_cover_is_named_as_short` |
| The city evidence panel lists each Policy the treasury could not pay in full, and whom it pays | Driven on the Day 50 control save: both `housed_rebate` and `school_funding` are listed |

### Base content

Changes from slice 4's base:

- The housed rebate is one application per housed Household of 640, up from 384. `govern` sets
  that amount directly.
- `school_funding` is order 5, after the rebate. It pays 1,280 per declared job, a quarter above the
  wage, which covers the 5% levy on the school's till. At 1,024 the school was in arrears on 14 of
  120 Days with a full treasury.

### Measurements

Re-measured in slice 6 on the re-recorded log, which founds the same grid with `street` commands
from Tick 256. Founding log, seed `0x135283C`, committed base, 400 Days. Control:
`spikes/FoundingProbe/tuning/tune.sh control 400 ""`. Governed: the same log with
`92160 govern 384 0 2` appended (Day 45, Policy 2 is `housed_rebate`).

| Day | 5 | 15 | 25 | 30 | 35 | 40 | 45 |
|---|---|---|---|---|---|---|---|
| Treasury | 3,052,853 | 2,065,607 | 917,265 | 417,433 | 0 | 0 | 0 |
| Teachers (of 5) | 3 | 5 | 5 | 5 | 5 | 4 | 0 |

| Measure, governed city | Result |
|---|---|
| Teachers back to 5 | Day 54 |
| Employed, Days 30–120 | 715–969 |
| Households, Days 30–120 | 616–810 |
| Businesses, Days 30–120 | 242–361 |
| Business tills, Days 60–400 | 2.0M–3.0M, no trend |
| Treasury, Days 60–400 | 0.4M–2.0M, no trend |
| Money supply, Days 60–400 | 5.2M–6.5M |
| Unpaid share of wages after Day 60 | 9.1% |
| Bankruptcies | 124 |

In the control, the school is short of teachers on every Day from 46 to 400.

### Findings

- Household balances in the governed city drift down, from 2.9M on Day 100 to 2.0M on Day 400,
  while the tills and the treasury hold. The money supply stays inside slice 4's band.
- The governed school still drops below 5 teachers on 54 Days after Day 46, as workers quit and
  are replaced. The acceptance test checks Days 61–70 only.
- The evidence panel names the short Policy but not what outgrew tax income. The budget panel's
  flows carry that.
- The panel said `housed_rebate` "did not pay all households". Slice 6 makes it say "housed
  households" for a Policy whose apply count scales by the `housed` Readout.

## Slice 6 record

`scripts/ui/founding.drive` plays the whole loop from "New empty city" with the shell's own tools.
The session's Input Log is `founding.borough` with `92160 govern 384 0 2` appended, byte for byte,
so the driven city is the city `BaseFoundingPackageTests` measures headless.

```sh
godot --path src/Borough.Godot -- --empty --seed 0x135283C --citizens 2000 \
  --drive scripts/ui/founding.drive
```

The run takes about 65 minutes, because rung 8 plays a Day in 64 s.

### Changes

| Change | Check |
|---|---|
| `founding.borough` lays the grid as ten straight `street` runs at Tick 256, zones each block at its south-west corner at 257, and places the gate and school at 258. The shell can no longer send `connect`, and Tick 256 is 08:00, where "New empty city" opens | The driven session's log equals it. `The_committed_founding_log_is_the_one_the_package_produces` |
| The shell takes `--seed N`, decimal or `0x` hex | The driven log's header carries `0x135283C` |
| `ui govern POLICY AMOUNT` opens the Government panel, types the amount and presses Set | `06-governed`: "housed_rebate set to 384 on Tick 92,161." |
| A Policy scaled by the `housed` Readout names "housed households" in the Government panel and the city evidence panel | `05-school-short` |
| Save, reload and continue at Days 30 and 120 matches the uninterrupted run to Day 126 | `A_founded_city_saved_on_days_30_and_120_continues_as_the_uninterrupted_one`, an instrument (6 m 50 s in Debug) |

The slice 5 numbers above are re-measured on the re-recorded log. The story holds, and the
teachers return on Day 54 instead of Day 60.

### Stages

Photographs and captions are in [evidence/founding-loop](evidence/founding-loop/).

| Photograph | Tick | What it shows |
|---|---|---|
| `01-streets` | 257 | Ten Streets make a 4×4 grid of 128 m blocks |
| `02-zoned` | 259 | Sixteen zoned blocks, the gate and the school. Treasury 3,932,160 |
| `03-grown` | Day 12 | 591 Citizens in 104 Buildings. The school has 5 of 5 teachers |
| `04-treasury-dry` | Day 35 | Treasury 0. Policy payouts are 378,240 a Day |
| `05-school-short` | Day 44 | 0 of 5 teachers. The evidence panel names `housed_rebate` and `school_funding` as short |
| `06-governed` | 92,161 | The Government panel after the cut to 384 |
| `07-teachers-back` | Day 54 | 5 of 5 teachers |
| `08-recovered` | Day 60 | 5 of 5 teachers, 40 pupils, treasury 473,197, no short Policies |

Teachers by Day, read from the school inspector: 5 through Day 37, 4 from Day 38, 0 on Days 45–46,
then 2, 3, 3, 4 and 5 on Days 48, 50, 52, 53 and 54. They match the headless probe on every Day.

### Findings

- The day boundary is 05:00, before sunrise, so a photograph at a Day's first Tick is black. The
  script photographs at 12:30. The govern stays at Tick 92,160 to match the log, so its photograph
  is dark behind the panel.
- The tool palette drew over the city evidence panel when it first opened and under it later in
  the same run. Filed as #145.
- The HUD counts Days from 1. It reads "Day 13 · 12:30" where the readout and this plan say Day 12.
- The school inspector reads "No supply shortfalls reported" with 0 of 5 teachers, and nothing on it
  links the empty posts to the unpaid grant. Row 30's service cause summaries own this case.
- "Occupied places: 1 / 7" counts the school building's units, beside "Places: 40 a Day" for pupils.
- A founded grocer opened in a vacant unit of the school's building between Days 50 and 54. The
  school's own trade stayed open throughout.
- The budget panel shows Policy payouts as one row, so it cannot show that the rebate is the outflow
  that grew.
- The zone tool's `click` paints nothing in a driven run, and Zone Rules count from 0. The drive
  skill now says how to zone and govern from a script.

## Execution

```text
(1 ∥ 3a) → #105 merges → 2 → 3b → 4 → 5 → 6
                                          ↑ freeform slice 5
```

| Slice | Size | Waits on | Lane | Risk |
|---|---|---|---|---|
| 1 | Small | — | Parallel now | May expose base defects nobody has seen |
| 3a | Medium | — | Parallel now | Golden hashes must not move. If they do, the default is wrong |
| 2 | Medium | #105 | Serial chain | Base needs a work zone so Zone Rules raise mills |
| 3b | Small | 2, 3a | Serial chain | — |
| 4 | Large | 3b | Serial chain, fan-out for tuning | Business tax may not bound tills. That would need a new mechanism |
| 5 | Medium | 4 | Serial chain | Resolved: a bankrupt school did not reopen, and slice 5 makes it |
| 6 | Small–medium | 5, freeform slice 5 | Serial chain | Built. The log is re-recorded with `street` |

Rules for parallel sessions:

- **Claim before starting.** Name the slice and its worktree in the board row's owner column, and
  check `git worktree list` for a tree already holding it.
- **One worktree per slice.** Branch from `main`, and name the branch `founding-<slice>`, e.g. `founding-3a`.
- **Only 1 and 3a run concurrently.** Slice 1 touches tests only. Slice 3a touches the Ruleset reader,
  schema, key notes and `World`, plus a fixture. Neither edits `rulesets/base/`.
- **The chain from 2 onward is serial.** Every one of those slices edits `rulesets/base/` and
  re-records `founding.borough`, whose header carries the package hash. Concurrent edits there conflict.

How to run each slice:

| Slice | Approach |
|---|---|
| 1, 2, 3b, 5, 6 | One session works it directly, in its own worktree |
| 3a | One implementer agent in an isolated worktree, or a separate session. `/orchestrate` adds an independent reviewer |
| 4 | One session owns the slice. Optionally fan out 4–6 agents, each running the 120-Day headless run with a different set of provisional numbers and reporting the Money curves |
| Before 5 | A read-only agent can settle the school-reopening risk early, while 2–4 are in progress |

## Acceptance

- **Founding.** From Ground, the committed log houses at least 80% of admitted Households by Day 12.
  Every shop sells sundries made from local flour. No Rule outside labour produces from nothing.
- **Operation.** Across Days 30–120:
  - Population, employment and the Business count stay within a band the plan records.
  - The sum of Business tills and the treasury each stay bounded. Neither grows linearly after Day 60.
  - `MoneyIsConserved` and the end-of-run invariants hold.
- **Shortage.** The D4 shortage arises without a scripted fault. The city evidence panel names its cause.
- **Recovery.** One player command restores the shortage's metric within a stated number of Days.
  The control run without that command does not recover.
- **Persistence.** Double replay matches. Save, reload and continue matches the uninterrupted run at
  Day 30 and Day 120. Built in slice 6 as an instrument, because the 126-Day run exceeds the
  assertion budget.
- **Shell.** A driven run shows founding, the shortage, the action and the recovery.

## Coordination

- **#105** must merge before slice 2.
- **Freeform Streets** changes the Street command and the Lot layer. Slices 1–5 are content and
  economy, and survive with a re-recorded log. Slice 6 should follow freeform slice 5.
- **Export-base** later adds a Money source. Re-run the operation checks when its slice 2 lands.
- **Row 30** owns failure-duration semantics. Slice 5 reuses them and does not define its own.
