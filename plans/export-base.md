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
| 9 | A jobless spell past a Ruleset duration makes a housed Household rerun the same comparison against the Hinterland. Life Stage sets reluctance | Departure when it cannot pay (Household financial resilience row) |
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

- **Profit build-up.** A till pays its surplus above a reserve to its owner Household (`adr/0176`).
  That Household's savings then buy Lots and pay construction, which sends the land price to the
  treasury and the construction charge outside the city
  ([land ownership and rent](money-circulation.md#land-ownership-and-rent-decided-10162026)). The
  budget guard bounds inflow, not accumulation. Check boundedness in the first long run with exports.
  Measured 10/16/2026 on `rulesets/exported.toml`, seed 0, 120 Days, before owner payouts and land
  purchase reached this branch: both Hinterlands spent their whole budget every Day (about 77,000 a
  Day), and firm balances grew in a straight line from 3.6M on Day 10 to 10.5M on Day 120.
  `MoneyIsConserved` held. Re-measure with owner payouts and land purchase in force before this
  slice ships to a base Ruleset.
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
   - `ServiceContractTable` saves the firm's provider. Save format version 22; goldens re-recorded
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
   - `HinterlandTable.ExportedToday`/`ExportedYesterday` are saved. Save format version 23; goldens
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
   - Save format version 24; goldens re-recorded for the new table, the new column and the version.
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
   shares a Hinterland's Goods `export_budget`. Blocked in practice on the profit build-up, because
   it is a second inflow with no sink.
4. **Jobs attract arrivals.** Add the reachable-jobs term to `PlacementEngine.Compare` and to the
   Hinterland row. Record the change to plan 0073's "no Hinterland wage" line.
5. **Jobless departure.** Read `NoVacancySince`; past the Ruleset duration, rerun the comparison
   with Life Stage reluctance. Coordinate failure semantics with the decline-and-recovery row.

## Acceptance

- A fixture firm imports a service and stalls when its Hinterland supply is cut.
- A fixture city's offices open on unserved service demand and sit closer to their clients than a
  random placement of the same Lots would.
- Export income enters the city and `MoneyIsConserved` holds across save, reload and replay.
- A fixture with jobs in reach draws more arrivals than the same fixture without them.
- A fixture that loses its employers loses Households over the stated duration, families last.
- A driven demonstration shows an office district forming and the treasury's balance of payments
  turning positive.
