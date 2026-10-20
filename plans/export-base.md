# Export base and Professional Services

Board rows: Growth drivers and the export base; Office and agglomeration.
Design: [economy §1](../docs/04-economy-and-goods.md#professional-services-and-the-firms-that-sell-them),
[simulation model §5.4–5.5](../docs/02-simulation-model.md#54-the-choice-itself),
[`CONTEXT.md`](../CONTEXT.md) → Professional Service. "Service" alone means a public Service; this plan says "service" for short only where the meaning is plain.

## Outcome

A city earns from outside by selling what it makes, Goods or services. Firms buy services from each
other, so offices cluster near their clients. Reachable jobs draw Households in, and a long jobless
spell sends them out. Growth continues after the first Unplaced Pool empties because exports pay
for jobs and jobs attract people.

## Current behaviour (checked 10/02/2026 at `8224b394`)

- Money enters through `World.Endow`/`EndowTreasury` (the founding treasury, founding Households and
  arrival purses) and, since slice 2, through `World.PayForExport`.
- `World.TryImportPrice`/`PayOutside` import a Good at the cheapest gated Hinterland edge.
  `World.TryExportTo` sells one to the highest-paying gated edge with budget left.
- `ZoneRuleEngine.MarketFor` returns the market row of a kind's first Business-owned Good Bin, or
  else of the first Professional Service its Rules make (slice 3b). A kind that sells neither can
  never be raised.
- Production is labour-bound (PR #105): a Rule with a labour input makes nothing when nobody is at work.
- `PlacementEngine.Compare` scores a dwelling on rent and centrality only. No jobs term exists.
- Only an unhoused Household departs (`PlacementEngine`, give-up bound). A housed Household with no
  work never leaves.
- A Hinterland is authored per Ruleset and pinned to an edge (`[[hinterland]] edge`). Seeds vary
  only the land.
- The only office is `officeblock` in `rulesets/schooling.toml`, a stand-in that sells `repairs`.

## Decisions (10/02/2026)

| # | Decision | Later |
|---|---|---|
| 1 | A city can export anything: Goods and services | — |
| 2 | Services trade inside the city between firms, as well as out | — |
| 3 | A firm buys a service from a provider within a travel-time reach set per service kind. A remotely delivered kind may have unlimited reach | Sparse meeting Trips once business Trips exist |
| 4 | Firms buy services as a required Rule input. A firm short of services stalls like any starved Rule | Households buy at life events; treasury buys |
| 5 | Hinterlands are price takers: any volume at an authored price per Good and per service | Price falls with volume for large cities |
| 6 | A daily purchase budget per Hinterland bounds exports | Removed once production needs workers |
| 7 | The player works on the internal economy. Outside prices stay mostly steady; firms sell locally first and export surplus without a trade screen | Occasional outside price events |
| 8 | Reachable jobs, as `log(1 + jobs)`, enter arrival placement on both the city and Hinterland sides | Wages, once posted wages (`adr/0026`) exist |
| 9 | A housed Household with a jobless adult reruns the same comparison against the Hinterland after each failed job search. No duration gates it (`adr/0102`, 10/19/2026). Life Stage sets reluctance through `[[life_stage]] leaving_costs_rent` | Departure when it cannot pay (Household financial resilience row) |
| 10 | Seeds differ through land. Hinterlands stay authored per Ruleset | Generator deals Hinterlands to edges (world-creation row) |
| 11 | A firm keeps its service provider under a saved contract and chooses again only when the provider cannot supply it (`adr/0179`, 10/15/2026). A District Pool was rejected because it cannot say "within reach of this firm" | Local providers and their capacity, offices slice |
| 12 | Surplus is output a firm cannot store. A Rule with its inputs and workers present, blocked only by a full output Bin of a Good it makes, sells that batch to the Hinterland. The Bin stays full, so local buyers lose nothing (10/16/2026). A waiting period before exporting was considered and not built | A waiting period, if a long run shows local buyers going without |
| 13 | Service exports wait for their own slice, 3c. Nothing in the city makes a service before 3a, and 3a sells only to city clients. 3b raises offices and adds no export, because export income still has no sink (10/17/2026) | Selling unused office capacity, slice 3c |
| 14 | A firm importing a service is unserved demand for an office, counted from the Tick it chose the Hinterland. It stops counting once any office in its District makes the service, whether or not that office reaches it. A stalled firm counts as before. An imported Good still counts for nothing (10/17/2026) | A capacity signal, so a sold-out office can raise a second; Goods imports as demand |

Consequences of these decisions:

- An Office earns by selling services. No per-worker export payment.
- Unserved service demand raises offices through the existing Zone Rule market trigger, read from
  the District market row a service buyer already waits on. Land bids and private capital stay
  undesigned and are not needed.
- A young city with no offices imports services from the Hinterland and replaces imports as local
  offices open.
- On-site services (cleaning, security, repair) stay ordinary jobs at the client's Building.
- Services are made from labour only. As an input to Food processing, the longest chain is two.

## Open

- **Profit build-up. Re-measured 10/18/2026: still unbounded.** Money in the city grows by the
  export income, about 77,000 a Day, with or without owner payouts and land sales. Owner payouts
  (`adr/0176`) move the build-up from tills to owner Households. Land sales spend it only when the
  city builds, and this fixture barely builds. Rent cannot bound it, because rent moves money to the
  treasury or a Landowner and none of it leaves the city.
  - Conditions: `ExportProfitTests` (instrument), `rulesets/exported.toml`, 400 Citizens, seed 0,
    120 Days, commit `b24a01de`, Release, machine `zeus` under load (load average 7 to 12).
  - Variants are edited in memory. Payouts add `pays_owner_above_pay_periods = 2` to both trades.
    Land sales add `[lots]` `demolition_price_per_tile = 1024`, `construction_charge_per_floor_tile =
    256` and `buyer_reserve_per_member = 1024`.

  | Run | Issued, Day 10 → 120 | Firms, Day 120 | Households, Day 120 | Treasury, Day 120 |
  |---|---|---|---|---|
  | Shipped file | 3.59M → 12.10M | 10.48M | 1.63M | 0 |
  | Owner payouts | 3.59M → 12.10M | 0.63M | 11.47M | 0 |
  | Payouts and land sales | 3.51M → 11.96M | 0.76M | 11.00M | 0.20M |

  - The shipped file reproduces the 10/16/2026 baseline. Firm balances rise from 3.56M on Day 10.
  - Payouts paid owners 11.3M over 120 Days. Firm balances fall after Day 10 and hover under 1.1M.
  - Land sales bought 4 Lots in 120 Days. Buildings went from 43 to 47 and Households stayed at 144,
    so construction charges removed about 140,000 against about 9.2M of exports.
  - `MoneyIsConserved` held in all three runs.
  - A sink has to scale with income rather than with growth. Candidates are Household spending that
    rises with wealth, imports of construction Materials, or departures that take savings out.
    Decide one before this slice ships to a base Ruleset.
- **Who staffs a service firm.** Keep the single `requires_tier` minimum. A tier mix waits on
  `adr/0026`.
- **Office capacity. Decided 10/17/2026, slice 3a.** The unit is a Day's units of one service, made
  by the office's own Rule and expiring at the Day boundary. A contract holds no share of it: the
  buyer re-matches on the purchase that finds the allowance empty, so capacity binds per purchase and
  a contract names only the provider. The alternative, a reserved share per client, would have to be
  released when a client stopped buying and nothing watches for that.
- **Reach test. Built 10/17/2026, slice 3a.** A straight-line box, then one Foot walk per candidate
  office inside it, premises door to premises door — `ServiceEngine`'s shape for a Household judging
  a school, both halves of it. The box is `EmploymentEngine.Radius` over the authored reach, so it
  over-supplies candidates and never under-supplies them, and an office across the map costs two Lot
  reads rather than a search. The `RuleEngine` owns the `WalkScratch`; Phase 2 is a sequential loop
  and a search writes scratch rather than world state. A remote service returns before the box and
  the search, so a Ruleset with no finite reach asks the Road Graph nothing.
  - **The `TravelTimeMatrix` was built first and does not fit.** Its order is the Road Graph's
    occupied partition count, and `serviced.toml`'s `[roads]` lattice occupies all 16,384 partitions
    on the map at 43 Buildings — 263,169 nodes, 535,794 Segments. One rebuild ran 16,384 one-to-all
    searches and settled 4,311,760,896 nodes against 1.07 GB of entries. The two counts are
    deterministic; the wall time was about 18 minutes in Release on this workstation at a load
    average between 4 and 29 from concurrent sessions, which makes it **provisional** and an upper
    bound rather than a quiet-machine figure. `RoutingPartition.Count`'s remark predicts ~1,444 partitions for a 1M city; the shipped
    generator does not keep to it, and that is a road-generation defect rather than a matrix one.
  - A walk is also exact where a matrix entry overstates a real walk by up to 9.2 minutes, so an
    authored reach is now a sharp threshold.
- **Reachable-jobs cost.** Counting jobs per sampled dwelling must read a refreshed accessibility
  field rather than routing (`02 §5.8`). Slice 3a's walk per candidate does not license it: the
  provider choice walks the offices inside one box, where a reachable-jobs count walks every job in
  reach of every sampled dwelling, and no box bounds that to a handful. Measure the arrival pass
  before and after.
- **Gate load.** Goods exports teleport, as imports do, until the Freight and Outside trade row
  builds Shipments.

## Slices

Ordered by dependency. Each slice is one PR with its own acceptance.

1. **Professional Service family, imported. Built 10/15/2026 on `export-base-1`.**
   - `family = "service"` with `reach_minutes` or `remote = true`. A service has no Bin and is bought
     only through a `pool` input.
   - Hinterland `prices` accept services. A firm keeps the Hinterland it chose while that edge has a
     gate, and stalls on its District market when no gated Hinterland prices the service.
   - `ServiceContractTable` saves the firm's provider. Save format version 24; goldens re-recorded
     for the new table only.
   - A `pool` term on a utility or labour Resource is refused at load (issue #177).
   - Fixture `rulesets/serviced.toml`; tests `ServiceImportTests`, `ServiceRulesetLoadTests`.
   - Reach is read and stored but binds nothing until local providers exist.
2. **Exports with the budget guard. Built 10/16/2026 on `export-base-2`.**
   - `[[hinterland]] export_budget`: Money per Day, reset at each Day boundary. Absent buys nothing.
   - A Hinterland buys at its `prices` figure. The buyer is the highest payer with a gate and budget
     for the whole batch, with ties to the one declared first.
   - Only a Good the firm makes is exported, and one output per firing. A Rule that buys the same Good
     through a pool term is restocking and does not export.
   - `World.PayForExport` pays the firm's balance, moves `Issued` and records revenue with no cost of
     goods. A new gate and each Day boundary wake firms asleep on a full Bin.
   - `HinterlandTable.ExportedToday`/`ExportedYesterday` are saved. Save format version 25; goldens
     re-recorded for the new columns only.
   - Goods only (Decision 13). Fixture `rulesets/exported.toml`; tests `ExportTests`.
3a. **Offices make and sell services. Built 10/17/2026 on `export-base-3`.**
   - A service is a `local` output on a Business Rule with a labour input, so an office with nobody at
     work makes nothing. A service output in any other scope, a Rule making one that no Business runs,
     one that spends no labour, and one that both buys and makes the same service are refused at load.
   - `ServiceAllowanceTable` saves what each office has made and sold today, per service. Units left
     are made less sold, and a row whose flow Day is behind today reads as empty — so the allowance
     expires at the Day boundary and no sweep has to clear it.
   - `ServiceContractTable.Provider` names the office and is severable; none means the `Hinterland`
     column applies. A demolished office leaves its clients a stale handle, which is what they
     re-match on, and its own allowance rows are freed with it.
   - The buyer takes the nearest office in reach on foot with units left for the whole batch: a
     straight-line box, then one walk per candidate inside it, ties drawn by counter hash
     (`PurposeTag.ProviderChoice`, keyed on the Rule Instance and the service). A local sale is purse
     to provider balance with no `PayOutside`, at the buyer's District market-row price. It books the
     office's revenue and the client's expense on the Day, because a service is consumed in the
     firing that bought it and no Bin can carry its cost forward. The units left cap the
     applications, as a full output Bin caps a Good's.
   - ⚠ An *imported* service is still expensed nowhere. The office sale was the asymmetry that
     mattered — revenue on one side only — and pricing imports into a firm's profit moves every world
     that buys one, so it is a change of its own rather than part of this slice. **Its consequence is
     a provider-dependent tax bill**: the same service at the same price is an expense bought from a
     city office and not bought from a Hinterland, so a firm switching from import to office sees its
     `ProfitOn` and its `BusinessTaxEngine` bill fall. Expect that before reading it as drift.
   - Every deposit to an allowance wakes that service's market rows, so a firm stalled with no gated
     Hinterland starts again without polling. Not only the first of the Day: a buyer needing four
     would sleep through an office climbing from one to sixty-four (`adr/0171`).
   - `02 §5.8` and `adr/0179` are amended in place. Testing reach by routing inside the choice loop
     is `adr/0047:126`'s pre-registered reopening condition met, and the walk is bounded by a box.
   - Save format version 26; goldens re-recorded for the new table, the new column and the version.
   - Fixture `rulesets/offices.toml`; tests `LocalServiceTests`, `ServiceRulesetLoadTests`.
3b. **Unserved service demand raises offices. Built 10/17/2026 on `export-base-3b`.**
   - `ZoneRuleEngine.MarketFor` reads a kind's first Business-owned *Good* Bin, so a labour Bin no
     longer hides the rest, and otherwise the first service the kind's Rules make. An office's market
     is the District row its clients' `pool` terms wait on.
   - Demand adds, per District row, the Ticks since each firm chose a Hinterland that still supplies
     the service (Decision 14). It reads the saved `ServiceContractTable.Since` and adds no state.
   - A District where an office already makes the service adds no import demand. Without that bound,
     a firm no office reaches kept its clock running: with `reach_minutes = 1` the city raised 22
     offices in 6 Days across 2 Districts. Units left are not read, because an allowance is empty
     every morning until staff arrive.
   - ⚠ Two known gaps. A District that outgrows its office's capacity raises no second office from
     imports, and its out-of-reach firms stay on imports. An office reads demand only for its first
     service (`accounting` in `offices.toml`), so demand for `insurance` alone raises nothing.
   - `World.WakeServiceMarkets` reaches one row per District through `DistrictMarkets.Row` instead of
     scanning every Pool row. Wake order is now District order, so a world that buys services moves
     its State Hash. No golden fixture buys one.
   - `RuleEngine.ProviderRouteWork` counts the reach walks; `--profile-work` reports them as
     `providerRoutes`. Measured in `plans/0013`: 286 walks and 420 settled nodes over two
     Days with one office, the largest walk 4 nodes. A candidate in the box with a long road route
     is still unmeasured, because the fixture raises one office near all its clients.
   - Without the tests' capital top-up, seed 0 wrote no service contract in 6 Days, and a
     `--profile` run on the fixture walked nothing. The cause is not established.
   - `offices.toml` gains the `offices` Zone Rule. `LocalServiceTests` removes it except in the test
     where the city raises its own office.
3c. **Service exports.** Sell unused office capacity to Hinterlands (Decision 13). Needs a decision
   first: what counts as unused (left at the Day boundary, or per firing), and whether a service
   shares a Hinterland's Goods `export_budget`. Still blocked on the profit build-up, because the
   10/18/2026 run found no sink that scales with export income.
4. **Jobs attract arrivals. Built 10/18/2026 on `export-base-4`.** Arrival choice reads qualified
   reachable vacancies on city and Hinterland alternatives. Plan 0073 keeps its no-wage boundary;
   the new count is not income. See the evidence and rollout limit below.
5. **Jobless departure. Built 10/19/2026 on `export-base-5`.**
   - A Citizen is jobless when of working age, without a Workplace, not studying, and last concluded
     `None` or `NoVacancy` (`World.IsJobless`). `BeyondReach` is the destitute channel's and
     `BelowCredential` names a missing school, so neither triggers the comparison.
   - Every failed search by a jobless Citizen rechecks its Household on the same Tick
     (`EmploymentEngine.FailedSearches`, then `PlacementEngine.ReconsiderJobless`). A Ruleset that
     states `[placement] reconsider_ticks` also rechecks in the reassessment sample.
   - The home scores rent, centrality, qualified reachable vacancies for the highest jobless tier,
     `moving_costs_rent` and the stage's `leaving_costs_rent`. Each Hinterland scores its rent,
     centrality and `reachable_jobs`. One draw (`PurposeTag.JoblessDeparture`) picks staying or the
     destination edge.
   - `World.Leave` shares the unhoused door's sale, money sink, ledger and Hinterland return. The
     tenancy ends through `RetireHousehold`. `PlacementActivity.Left` counts the channel.
   - Runs only with `[placement] jobs_rent` and `[immigration]` stated, so only `jobs-attract.toml`
     changes. No saved state, no save-format change and no golden re-record.
   - Evidence: `JoblessDepartureTests`, `jobs-attract.toml`, seed 7, employers destroyed after one
     Day, two Days observed. Of the Households housed at closure, 71% stayed against 86% with
     employers open. Young Households kept 56%. Families with `leaving_costs_rent = 1200` kept 100%,
     and 68% without it.
   - ⚠ No leaving Trip. The Citizens retire at once, because a Trip needs a Citizen to outlive its
     Household until it reaches the gate (issue #204).
   - ⚠ Churn. Over those two Days 510 Households left a city that housed about 450 at closure, so
     arrivals into a jobless city leave again after their first failed search. Arrival choice
     weighs jobs too, but the fixture's Outside job counts are small.

## Acceptance

- A fixture firm imports a service and stalls when its Hinterland supply is cut.
- A fixture city's offices open on unserved service demand and sit closer to their clients than a
  random placement of the same Lots would.
- Export income enters the city and `MoneyIsConserved` holds across save, reload and replay.
- A fixture with jobs in reach draws more arrivals than the same fixture without them.
- A fixture that loses its employers loses Households over time, families last.
- A driven demonstration shows an office district forming and the treasury's balance of payments
  turning positive.


## Slice 4 evidence and limits

- Arrival `Compare` adds `ln(1 + qualified open posts)` for standing housing, buildable housing
  Lots and the Hinterland. Highest adult Skill Tier determines eligibility; a post counts once,
  regardless of the number of qualifying adults. Residents and departures keep their existing score.
- `[placement] jobs_rent` is the shared daily-rent equivalent of one logarithmic unit. Zero disables
  the term. `[[hinterland]] reachable_jobs` defaults to zero and describes qualified vacant posts.
  Life Stage-specific jobs weights remain deferred. Existing Rulesets do not change behavior.
- City reach is exact Foot travel time within `[trips] commute_budget_minutes`, including the
  existing same-Segment crossing rule. This scope understates drivers' reach. Zero jobs remain a
  soft disadvantage. Wages are not added; plan 0073's wage boundary remains in force.
- `rulesets/jobs-attract.toml` copies the shipped `attracted.toml` demonstration with provisional
  `jobs_rent = 240` and `reachable_jobs = 8`. At seed 7 and 1,000 Citizens, one Day admits
  **256 Households with employers versus 238 without employers**. No arrivals are commanded.
  Removing employers also removes their output; this demonstrates behavior rather than estimating
  a city's response elasticity.

### Candidate preparation

- The reviewed slice is commit `d8abb8a8`. Its full-field refresh rebuilt every Lot after an input
  change. The candidate-only preparation follow-up keeps its decisions and numerical order.
- `Compare` collects the same feasible alternatives and their original sampled Lots first.
  A separate pass prepares those origins, then adds jobs utility before `Choice.Draw`.
  Preparation runs in Phase 6 or the requested-arrival command path, never in Phase 2.
  `VerifyDecideWritesNothing` remains green.
- The existing sample has two budgets, up to `placement.candidates` standing homes and the same
  number of buildable Lots. Each comparison therefore prepares at most twice that authored number
  of origins. Repeated Lots share one flood. Fresh occasions and queued reviews use the same bound.
- Input snapshots still scan Lots and Businesses on each preparation. Any changed vacancy,
  eligibility, door, road version or Ruleset invalidates all prepared flags and counts. Only sampled
  origins are then reconstructed; scoring reads exact current counts without routing or staleness.
  Aggregation scales with prepared origins times nearby employers, not all Lots times employers.
- `World.RebuildDerived` forces a full cold reconstruction after load. `Refresh` remains the cold
  rebuild and test-reference path. The audit corrupts counts while prepared flags still match;
  rebuild clears those flags and restores the counts. No cache state is saved.
- Candidate counts match a full-field oracle after vacancy, Ruleset and road changes. Candidate-only
  choices match full-refresh choices after employer changes. Save/reload and Input Log replay
  preserve State Hashes across route-worker settings 1 and 8 and pass end-of-run invariants.
- All 64 first-Day hash samples at 32-Tick intervals match the reviewed slice at seed 7 and
  1,000 Citizens. Both traces end at Tick 2048 with `0572E38A0DA3034B`. The admission counts remain
  256 versus 238; the larger city's admissions remain 166.

### Walking work

Release counts on `zeus`, Intel Core i5-10400, seed 7, route workers 1, first full Day,
Decide-write guard disabled, concurrent sessions active. The authored fixture runs at 1,000
Citizens and as a larger synthetic city at 10,000 Citizens. These are deterministic work counts,
not quiet-machine timing claims. Cold save/reload reconstruction is outside these captures.

| Citizens | Live Lots | Live Businesses | Full-field floods | Candidate floods | Full-field nodes | Candidate nodes |
|---|---|---|---|---|---|---|
| 1,000 | 136 | 100 | 8,704 | 875 | 16,864,384 | 686,806 |
| 10,000 | 1,032 | 864 | 66,048 | 330 | 144,919,488 | 408,368 |

Reproduce the work counts with the focused instrument.

```sh
scripts/test.sh --filter 'FullyQualifiedName~JobsAttractTests.Accessibility_work_per_Day' -- --logger 'console;verbosity=detailed'
```

- No saved field, save-format version or behavior changes arise from candidate preparation.
  Existing goldens need no re-recording. Base Rulesets retain their disabled jobs term; the new
  fixture's tuning remains provisional. Generated schema and key reference include the new keys.
- The reviewed slice passed `dotnet build`, the 4,875-test simulation lane, the final 60-test
  jobs-and-Corpus filter, TOML lint and `git diff --check`. Independent loop review approved.
- The candidate-only follow-up passes the original jobs tests, exact admission assertions,
  full-field oracle checks and the Decide guard. Its final full gate and loop review are pending.
