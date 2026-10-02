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

- Money enters only through `World.Endow`/`EndowTreasury`: the founding treasury, founding
  Households and arrival purses. Nothing pays the city for anything it sells.
- `World.TryImportPrice`/`PayOutside` import a Good at the cheapest gated Hinterland edge. No
  export path exists.
- `ZoneRuleEngine.MarketFor` returns a market only for a kind holding a Business-owned, non-money
  Good Bin. A kind that sells no Good can never be raised.
- Rules produce whether or not anyone is at work. Labour-bound production is designed, not built.
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

Consequences of these decisions:

- An Office earns by selling services. No per-worker export payment.
- Unserved service demand raises offices through the existing Zone Rule market trigger, once a
  service is a Business-owned Bin. Land bids and private capital stay undesigned and are not needed.
- A young city with no offices imports services from the Hinterland and replaces imports as local
  offices open.
- On-site services (cleaning, security, repair) stay ordinary jobs at the client's Building.
- Services are made from labour only. As an input to Food processing, the longest chain is two.

## Open

- **Profit build-up.** Business tills receive export income with no investment sink until private
  capital is designed. The budget guard bounds inflow, not accumulation. Check boundedness in the
  first long run with exports.
- **Who staffs a service firm.** Keep the single `requires_tier` minimum. A tier mix waits on
  `adr/0026`.
- **Service purchase mechanism.** Either the pool purchase (`adr/0167`) with a reach filter, or a
  saved contract between buyer and provider. Decide in slice 1 by tracing `RuleEngine.Buy`.
- **Reachable-jobs cost.** Counting jobs per sampled dwelling must read a refreshed accessibility
  field, never route inside the choice (`02 §5.8`). Measure the arrival pass before and after.
- **Gate load.** Goods exports teleport, as imports do, until the Freight and Outside trade row
  builds Shipments.

## Slices

Ordered by dependency. Each slice is one PR with its own acceptance.

1. **Professional Service family, imported.** Add the Professional Service Resource family to the Ruleset reader and schema.
   Firms declare service inputs. A Hinterland sells each service at an authored price. Reach per
   service kind. No local producer yet. Needs no labour work.
2. **Exports with the budget guard.** A Hinterland buys surplus Goods and services at its price, up
   to a recovering daily budget. Firms sell locally first. Money enters at a new door that moves
   `Issued`.
3. **Offices make services.** Waits on the first slice of labour-bound production, so an unstaffed
   office makes nothing. Unserved service demand raises offices through `MarketFor`.
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
