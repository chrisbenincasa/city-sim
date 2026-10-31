# Money circulation: profit, rent and premises costs

## Outcome

Money that reaches a Business or a Household has somewhere to go next. A city running for hundreds
of Days keeps money moving between Households, Businesses and the treasury, and no one holder
accumulates it.

## Current behaviour (checked 10/11/2026 at `a5553cbb`; the Utility rows 10/20/2026 on `utilities-4`)

| Business outflow | Built | Recipient |
|---|---|---|
| Wages | `WageEngine` | Worker's Household, less income tax to the treasury |
| Business tax | `BusinessTaxEngine` | Treasury |
| Rule inputs | `RuleEngine` trades | Selling Business, or outside the city |
| Levies | Content `[[policy]]` only | Treasury |
| Profit to an owner | `WageEngine.Draw` | Founding Household, or a housed Household drawn at random |
| Rent | No | — |
| Premises upkeep | No | — |
| Utilities | `RuleEngine` at firing, by the tariff Policy | Treasury |

- Households pay for shopping, tuition, income tax and content levies. Nothing else leaves a
  Household.
- `[[building]] rent` is a placement filter only. Placement skips a dwelling a Household cannot
  afford. Nothing charges it.
- `World.Found` saves the founding Household as the Business's owner. A Zone Rule's Business has
  no founder, and the treasury's opening grant funds it. It draws an owner on its first payday
  with a surplus.
- Street upkeep and Utility imports are the treasury's two standing costs.
- The treasury buys a short District's Utility shortfall at an Outside Connection at the daily
  settle, and pays for what that settle imported. The money leaves the city to the Outside, so the
  money supply falls by it (`adr/0024`), as a Good import's payment does. There is no debt, so an
  empty treasury imports nothing and the District stays short.

## Evidence

The founding loop's base city, run with `quits_after_days_unpaid` and the M2 content set from
[slice 4](first-playable-founding-loop.md#slice-4-record). Founding log, seed `0x135283C`.

| Day 400 | No quitting | N = 3 with hiring rule |
|---|---|---|
| Treasury | 0 | 0 |
| Business tills | 3.96M | 3.88M |
| Household money | 289K | 97K |
| Households | 820 | 781 |
| Employed | 862 | 798 |

- Grocer tills hold most of the city's money from about Day 220.
- The levies and the daily rebate are the only path from tills back to Households. Households
  waiting for housing collect the rebate and emigrate with it. They took 1.96M out of the city
  between Days 101 and 180.
- Two-earner Households save because their spending is fixed and they pay no rent.

## Items

| Item | State | Next step |
|---|---|---|
| Owner draw | Built and in base (`adr/0176`) | — |
| Housed-only rebate | Built and in base (`housed` Readout) | Decide whether base keeps it after the slice 7 run |
| Land ownership and rent | Decided (L1–L18) | Build [slices 1–7](#land-and-rent-slices) |
| Premises upkeep | Scoping | Decide whether upkeep is Money only or consumes repairs |
| Utility billing | Built on `utilities-5` in `powered.toml` ([utilities](utilities.md) slice 5); the tariff recovers what slice 4's imports cost the treasury | Base adopts it with the Utilities in slice 7 |

## Decisions

### Owner draw (decided 10/11/2026)

- A founded Business saves its founding Household as its owner.
- On each payday after wages, an owned Business pays its owner what its till holds above a
  reserve. A Ruleset key on `[[business]]` sets the reserve in pay periods of the trade's wage bill.
- Absent means no draw, so existing Rulesets do not change.

### Owner of a Business with none (decided 10/12/2026)

- A Business with no owner and a surplus on a payday draws a housed Household as owner and keeps
  it. Each housed Household is equally likely.
- This covers a Zone Rule's Business and a Business whose owner departed or dissolved.
- The pick happens at the payday, not at opening. Generated cities raise Buildings before they
  house anyone, so a pick at opening finds nobody.
- A draw to the treasury was rejected. The treasury already piles up money when levies run, and
  Households would get money back only through the rebate.

### Land ownership and rent (decided 10/16/2026)

| # | Question | Decided | Rejected |
|---|---|---|---|
| L1 | Who owns land? | A Household, the Landowner | The city owning all land, which makes rent a tax. A landlord Business, which would be a Business without premises (`adr/0142`) |
| L2 | What does a Landowner hold? | The Lot, vacant or built, and the Building on it | The Building only, which leaves vacant land unowned and land banking with no actor |
| L3 | Who holds a newly carved Lot? | The city, until a Household buys it to build. The price is the `World.DemolitionPrice` formula, so the city sells and buys back at one price | A free draw to a random Household. Nobody until built |
| L4 | Who decides to build? | The Zone Rule triggers, unchanged. The engine then finds a buyer. With no buyer the Lot stays vacant, with "No capital" as its reason | Households investing on a pro-forma. Deferred with `adr/0163` tier 3 |
| L5 | Which Household buys? | Equally likely among Households that can pay and keep a reserve. The reserve is set by L15 | The richest, which concentrates ownership. The future resident first |
| L6 | Who receives construction Money? | Outside the city, as an import, from a Ruleset charge per floor Tile. Buying a Materials Good replaces it later | The treasury. Local builder Businesses, which belong to the construction phase row. No charge, which leaves Lots nearly free |
| L7 | How much is rent? | Daily, per tenancy: the kind's rent per floor Tile × the tenancy's floor area × (1 + land value at the Lot), floored at zero. A resident Landowner pays none. Placement and `HousingUtility` read the same figure, so `[[building]] rent` becomes a per-floor-Tile rate in every Ruleset | One rent per kind, which charges small and large Units alike. Market rent |
| L8 | What if a tenant cannot pay? | It pays what it can and owes the rest (L16). An arrears clock runs from the first short Day and resets when nothing is owed. Past a Ruleset duration in Days, a Household is evicted to the Unplaced Pool (`adr/0054`) and a Business becomes unpremised and emigrates (`adr/0142`) | Eviction on the first short Day. Forgiving the shortfall, which costs Landowners most of a weekly-paid tenant's rent |
| L9 | What happens to a Lot when its Landowner goes? | On departure the Landowner sells at the L3 price and takes the money out of the city. Households housed on the Lot get first refusal, chosen as in L5; then any Household as in L5. With no buyer the Lot passes to the city unpaid. On dissolution the Lot passes to the city with the estate (`World.Dissolve`). The city collects a held Lot's rent and sells it, first refusal included, when a buyer appears | Always reverting to the city, which makes rent drift toward a tax. A free redraw, which undoes L3 |
| L10 | What if assembly or a Street needs owned Lots? | Assembly merges only Lots the city holds or one Landowner holds; another Household's Lot blocks it. A Street or the player's `Demolish` is compulsory purchase: the treasury pays the Landowner the L3 price, and tenants are evicted through the L8 paths with no payment | Assembly buying out every owner, which removes holdouts. Paying tenants, as `World.PayDisplaced` does today, which leaves the Landowner unpaid |
| L11 | What if a Zone Rule wants to build on a vacant Lot a Household owns? | The Landowner builds if it can pay the construction charge and keep its reserve. Otherwise it sells at the L3 price to a buyer chosen as in L5, who builds | Holding when it cannot pay, which idles land for poverty rather than choice. A Landowner pro-forma, deferred with L4 |
| L12 | How does the player break a holdout? | A player action buys a vacant owned Lot by compulsory purchase at the L3 price. The city then holds it, so assembly can proceed. It extends `Demolish`'s compulsory purchase (`adr/0091`) to vacant Lots | The city taking a Lot automatically when no buyer exists. The city never builds (L4), so the Lot would stay vacant |
| L13 | Does a Landowner pay a holding cost? | Not yet. Run the base city to Day 400 under L1–L12 and check that Landowner and treasury balances stay bounded, and whether base can drop its levies and housed rebate | A land tax Policy now; the treasury is already where taxes pile up. Spending that rises with wealth, which belongs to the Household financial resilience row |
| L14 | Who owns Lots that world creation builds? | The city. They sell through the L9 step, tenants first, as buyers appear | A free endowment to residents, which creates land wealth without a purchase. A sale at creation, which opening balances cannot fund |
| L15 | What reserve must a buyer keep? | A Money amount per Household member, set by a Ruleset key. Only the Household's own balance counts; a Business's till reaches its owner only through the owner draw | Days of spending, which the code does not measure. Days of the buyer's rent, which is zero for an unhoused Household |
| L16 | How does daily rent meet paydays? | Unpaid rent is owed and carried forward, and the tenant pays it off as money arrives. Eviction at the L8 deadline caps what can be owed. The loader refuses an eviction deadline shorter than the longest `pay_period_days` | Forgiving each Day's shortfall. Base's school pays every 7 Days, so its staff would be short 6 Days in 7 |
| L17 | What if a Landowner is evicted? | It keeps its Lots and their rent while in the Unplaced Pool. If the Pool's give-up bound expires, it departs and its Lots sell as in L9 | Selling on eviction, which adds a second sale trigger for a state the Pool already bounds |
| L18 | What is a built Lot's price? | The land price plus the construction charge for the Building's floor Tiles, which is what it would cost to build again. Every transfer of a built Lot uses it: compulsory purchase, sale on departure and the city's sales. A vacant Lot's price is the land price | The land price alone, which lets a Street take a builder's investment for nothing. A depreciated value, which needs a saved Building age |

- Land value rests at or below zero (`adr/0122`–`0127`), so L7 can only discount rent today.
- L18 makes Streets through built blocks expensive. It may also leave world-created Lots unsold
  at full price, so the city collects their rent as a tax. The slice 7 run checks this.

### Open

- **Rent and the rebate.** Rent may let base drop its levies and rebate. With owner draw and no
  levies, income tax still piles up in the treasury and employment collapses
  ([slice 4 record](first-playable-founding-loop.md#findings)).

## Land and rent slices

Each slice is one PR. Every new mechanism sits behind an optional Ruleset key, and an absent key
keeps today's behavior, as owner draw does. Base adopts the keys only in slice 7.

| # | Slice | Decisions | Needs |
|---|---|---|---|
| 1 | Landowner column | L1, L2, L9 dissolution, L14 | — |
| 2 | Land sale and construction charge | L3–L6, L11, L15, L18 | 1 |
| 3 | One rent figure | L7 price | — |
| 4 | Rent collection and arrears | L7 charge, L8, L16, L17 | 1, 3 |
| 5 | Sale on departure and city-held Lots | L9 departure, L14 sales | 2, 4 |
| 6 | Compulsory purchase | L10, L12 | 1, 2 |
| 7 | Base adoption and the Day-400 run | L13 | All |

Slices 1 and 3 can run in parallel. After slice 1, slices 2 and 6 can too.

### 1. Landowner column

- `LotTable` gains a saved, severable Household handle, following `BusinessTable.Owner`. An empty
  handle means the city holds the Lot.
- A newly carved Lot and every Lot world creation builds start held by the city.
- `World.Dissolve` and `World.Depart` sever the handle, so the Lot passes to the city. Slice 5
  replaces departure's unpaid reversion with a sale.
- Evidence names a Lot's Landowner, and the shell inspector shows it.
- Acceptance: a dissolved Landowner's Lots read as city-held. Save/reload and replay hold. Goldens
  are re-recorded once, because the saved column moves the State Hash.
- Built 10/17/2026: `LotTable.Landowner`, saved and severable, with `LandownerTests`, save format 22
  and the goldens re-recorded. `LotTable` takes `Rows<Household>` from `HouseholdTable.NewRows`,
  because a Lot naming its Landowner makes the three tables' relations a cycle.

### 2. Land sale and construction charge

- `World.LandPrice` shares the demolition footprint formula without its displacement guard.
  `World.DemolitionPrice` still returns zero when nobody is displaced; slice 6 changes who it pays.
- A Lot's price follows L18: the land price, plus the construction charge for any Building's floor
  Tiles.
- New optional `[lots]` keys are `construction_charge_per_floor_tile` and
  `buyer_reserve_per_member`. Either enables sales, including an explicit zero. Both absent
  preserve free, city-held construction. Base does not adopt them until slice 7.
- Before `ZoneRuleEngine` raises a Building on a city-held Lot, it draws a buyer with equal chance
  among Households that can pay both and keep the reserve. The draw is a counter hash with its own
  `PurposeTag`. The buyer pays the land price to the treasury and the construction charge out
  through `World.PayOutside`, and becomes the Landowner.
- On an owned vacant Lot, the Landowner builds if it can pay the charge and keep the reserve.
  Otherwise it sells to a buyer drawn the same way.
- With no buyer, the Lot stays vacant and its evidence gives "No capital" as the reason.
- Measure the buyer search on a large generated city before merging. It must not scan every
  Household for each attempt if that shows in the Tick budget (`plans/0013`).
- Acceptance: a fixture where Households with savings buy and build, a poorer Household is never
  drawn, and a city with no savings leaves zoned Lots vacant with "No capital". `MoneyIsConserved`
  holds, and `Issued` falls by the construction charge.

- Built 10/17/2026 with `LandSaleTests`. Local housing checks capital before retiring source Lots,
  preserves a common Landowner, and refuses mixed ownership. It prices the selected footprint
  and floor area; vacancy evidence checks that same plan. Compulsory buyouts remain in slice 6.
- Headless evidence demonstration used temporary copies of `declining.toml`, seed `17`, `1,000`
  Citizens and `8,192` Ticks. A charge of `2,147,483,647` left `53` vacant Lots with "No capital";
  an explicit zero left `42` vacant Lots and no capital refusals. The demonstration exposed a
  stale evidence footer claiming capital had no mechanism; the footer now describes the flags.
- Finance preflight reads the maximum available Household capital from one unsorted pass per
  Zone Rule sweep. Only a real buyer draw builds the sorted scratch array. Binary search finds
  the affordable suffix, and a counter hash draws uniformly within it. Payments update the sorted
  entries and invalidate the maximum; a missing entry invalidates the index for rebuilding.
- Scratch ends with the sweep and is neither saved nor hashed. Paid-construction tests match with
  one and eight route workers and across a midrun save/reload. Batch Lot evidence shares one
  maximum-capital pass without retaining a snapshot across queries.

#### Buyer-search measurement (10/17/2026)

- Machine `zeus`, Intel i5-10400, six cores/twelve logical CPUs, `powersave`, .NET 10.0.12,
  Release, one simulation thread. Load average was `6.80/13.58/16.40` before the paired capture;
  MSBuild workers were active. Host load was uncontrolled. These are busy-host observations,
  not quiet-machine timings or a whole-Tick budget claim.
- `SyntheticCity` generated two copies of `minimal.toml` with a demand-reading Zone Rule at
  interval `32`, seed `62002`, actual population `100,000`, `36,000` Households and `10,080` Lots.
  Every Household received `10,000` Money. Only the enabled copy declared the construction key.
- Command `scripts/test.sh --filter 'FullyQualifiedName~LandSaleTests' -- --logger 'console;verbosity=detailed'`.
  Each copy warmed up for 32 triggers. The next 32 trigger pairs were interleaved, alternating
  which copy ran first. Neither sweep bought anything or built a sorted index. The table keeps
  the one-time pass or sort rather than dividing it across 1,000 attempts.

| Capture | Observed time | Scope |
|---|---|---|
| Enabled no-purchase Zone Rule sweep, worst / mean | 2.793 / 1.507 ms | Zone Rule contribution to one trigger Tick, without sorting |
| Keys-absent sweep, worst / mean | 0.421 / 0.328 ms | Matched generated city and trigger Ticks |
| Mean paired enabled-minus-absent difference | 1.179 ms | Difference within the 32 interleaved pairs; host load is uncontrolled |
| First finance preflight in one sweep | 0.472 ms | One unsorted Household pass and one affordability query |
| Subsequent first draw, index build and payment | 1.164 ms | The full sort remains payable on a purchase Tick |
| Combined single-purchase finance work | 1.636 ms | Preflight plus draw/index/payment; Building creation is excluded |
| Poor-city batch evidence over all 10,080 Lots | 11.218 ms | One capital pass; Household balances were emptied before timing |

- PR review kept finance before `Demanded` because `Demanded` claims demand or joblessness and
  starts the cooldown; the preflight path and its recorded measurement remain unchanged.

- The isolated purchase cost `100` land plus `100` construction. A prior unpaired capture at
  load average `18.67/17.27/17.81`, while another worktree's full suite was active, observed a
  `22.047 ms` worst sweep. It cannot show the added cost by itself; the paired capture above
  supplies the keys-absent baseline. Neither capture establishes quiet-host performance.
- Logs are `test-B01-fix-paired.log`, `host-B01-fix-paired.log`,
  `test-B01-fix-instrument-final.log` and `host-B01-fix-instrument.log` under
  `.orchestra/20261017-land-slices-2-4/`.
- Neither capture measures the `1,000,000`-Citizen target or an entire `Simulation.Step`. Repeat
  at that population on a quiet host before slice 7 claims the Tick budget fits. The consumer
  is recorded in `plans/0013`.
- The unsorted preflight prevents no-purchase triggers from paying the sort. Successful purchases
  still move the affected sorted-array range. Local housing evidence still evaluates assembly
  geometry per Lot; sharing capital removes the Household-times-Lot term, not that geometry cost.

### 3. One rent figure

- A function returns a tenancy's daily Rent: the kind's rate per floor Tile × the tenancy's floor
  area × (1 + land value at the Lot), floored at zero. A Business's floor is `World.UnitFloorOf`.
  A Household's floor is its equal share of the Building's floor.
- `[[building]] rent` becomes `rent_per_floor_tile`. The old key is refused with a message naming
  the new one, so no Ruleset silently reads a per-tenancy price as a per-Tile rate. Every Ruleset
  that sets it is converted, along with the schema and the key reference.
- Placement's affordability check and `HousingUtility.Worth` read this figure. `Worth` no longer
  assumes rent cancels across candidates.
- Nothing is charged yet.
- Acceptance: two dwellings of one kind on different land value rent for different amounts, and
  placement prefers the cheaper one when centrality is equal. Goldens are re-recorded.

**Built 2026-10-17.** `World.TenancyRent` is the one function, `HouseholdRent` and `BusinessRent`
name the two tenancies, and every Ruleset states `rent_per_floor_tile`.

**Base's rate is 23 and not the 25 its capacity rate implies, and the reason is measured.** Rate
24 and rate 25 leave the school one teacher short of its five jobs for three Days around Day 66 of
`BaseFoundingPackageTests.Governing_the_rebate_down_restores_the_school_and_the_control_stays_short`,
which is the demonstration slice 7 adopts. Measured on the committed `rulesets/base/founding.borough`
at seed 20,260,924 and 2,000 Citizens, replaying 70 Days twice with the Day-45 `govern` command:
every rate from 16 to 23 and from 26 to 32 keeps 5 of 5 teachers from Day 61 on, and the control
stays short at all of them. A base tenancy holds about 17 Tiles, so 23 prices a home at about 390
a Day on ground worth nothing against the flat 400 the file used to state. That named test is what
rejected 24 and 25, so the shipped rate is guarded; what nothing guards is the sweep across rates,
which is why it is recorded here.

### 4. Rent collection and arrears

- Each Day, each tenancy pays the slice 3 figure to its Lot's Landowner, or to the treasury while
  the city holds the Lot. A resident Landowner pays none for its own tenancy.
- A tenant short of its rent pays what it holds, owes the rest and saves the Day the shortfall
  began. Later income pays the debt first. Clearing the debt clears the Day.
- Rent received is income, so a payment settles every Landowner it passes on the way down the chain.
- Where a ring of Landowners owes each other, the mutual debt cancels rather than circulating. The
  smallest debt in the ring comes off every member, which is the end state that carrying one coin
  round the ring repeatedly reaches, at a cost set by the ring and not by the debt. It takes a coin
  in the ring to stand in for, so a ring holding nothing cancels nothing. The Day's reading reports
  the cancelled figure beside the cash, so the rent recorded is what circulation would have moved.
- The loader refuses an eviction deadline shorter than the longest `pay_period_days`.
- Past a Ruleset duration in Days, a Household is evicted with `World.Unplace` and a Business with
  `World.Unpremise`.
- Evidence and the inspector show "Behind on rent since Day N".
- An evicted Landowner keeps its Lots and their rent in the Unplaced Pool.
- `examples/UrbanNeighbourhood` endows its two resident Households 100 against the 160 a Day its
  homes now charge, so charging rent puts the showcase city in arrears on Day 1. Raise that
  endowment with this slice, as slice 3 raised the arrival band to 256.
- Acceptance: a fixture where a tenant falls behind and is evicted at the deadline, a tenant
  whose wage rises in time is not, and a tenant paid every 7 Days is never in arrears at payday.
  `MoneyIsConserved` holds.
- Rent reaching the treasury is treasury income and gets a flow counter of its own, so the budget's
  balance stays explained by the columns beside it. Private rent and cancelled debt are not income.
- Built 10/17/2026: `RentEngine` sweeps each Day boundary behind payroll and the subsidies, one
  optional key `[rent] evicts_after_days` turns it on and bounds the debt, `RentDebt` and
  `RentDebtSinceDay` are saved on both tenant tables, eviction writes the debt off where the tenancy
  ends, and `rulesets/renting.toml` demonstrates the clock. `RentAccounts` settles arrears at each
  income site as money lands, follows the Landowner chain and cancels a ring's mutual debt. Save
  format 23 and the goldens re-recorded.
- The sweep charges and settles every tenancy on both tables before it tests any deadline, because a
  Landowner in an earlier slot can be cleared by a tenant in a later one. `Behind` is counted in
  that second pass, so it reports what the city owes when the Day finishes.
- `MoneyFlowCounter.Rent` carries the treasury's share through `TreasuryFlows`, the Census, the
  shell budget's `rent · in` row and `--income`'s `rent in` column. `--income` and `--evidence` both
  run on `rulesets/renting.toml` unmodified, which is what the file's header advertises.

### 5. Sale on departure and city-held Lots

- A departing Landowner sells each Lot at its slice 2 price. Households housed on the Lot are drawn
  first, then any Household, both as in slice 2. The seller takes the money out of the city. With
  no buyer, the Lot passes to the city unpaid.
- A pass offers city-held built Lots for sale on a Ruleset cadence, tenants first. This sells the
  Lots world creation built.
- Acceptance: a departing Landowner's Lot passes to its tenant when the tenant can pay, and
  `Issued` falls by the sale price. A generated city's Lots move from the city to Households over
  time.
- Built 10/18/2026 with `LandDepartureTests`. `World.SellLot` is the one sale step. It draws a
  tenant who can pay and keep the reserve (`PurposeTag.LotTenantBuyer`), then any Household through
  slice 2's sorted capital index with the seller excluded (`PurposeTag.LotResaleBuyer`). Slice 2's
  construction draw shares that index search. `SellLot` checks the richest Household first, so a
  Lot nobody can afford costs a price read and no tenant walk.
- Departure sale follows slice 2's switch: either `[lots]` sale key turns it on. `World.Depart`
  sells each Lot the leaver holds before the balance leaves, so `Issued` falls by the proceeds too.
  The Unplaced Pool's give-up is the only departure, so an evicted Landowner sells the same way (L17).
- `[lots] city_sale_interval_days` turns on the city's offer. It runs at the Day boundary behind
  rent, so the Day's rent reaches the treasury before the Lot sells. The loader refuses it without
  a sale key. No key moves the State Hash where it is absent, so goldens and base are unchanged.
- Rent debt follows the Lot. A tenant's standing debt is paid to whoever holds its Lot when the
  money arrives, which is slice 4's rule for a Lot that passes to the city. A tenant that buys its
  own Lot has its debt written off, as slice 4 does for a resident Landowner.
- Generated cities sell most built Lots on the first offer when balances exceed prices.
  `shopping.toml` at a land rate of 400 keeps 11 of 60; they stay unsold until Households are
  endowed, then sell at the next offer. `waged.toml` and `declining.toml` demolish the kept Lots
  before anyone can afford them, so neither shows a later sale.
- Land income to the treasury has no budget flow counter. Slice 2's construction sales already
  had this gap, and city sales widen it. `TreasuryFlowsExplainTheBalanceTests` cannot see it,
  because its Ruleset states no sale key. Tracked in #196.

#### Departure and city-sale measurement (10/18/2026)

- Machine `zeus`, as for slice 2, Release, one simulation thread. Load average was `8.5–16.7` on
  12 logical CPUs while other worktrees ran suites. These are busy-host observations.
- `LandDepartureTests.Departure_and_city_sale_scans_on_a_large_generated_city` generates two
  copies of `minimal.toml` at seed `62002`: `99,808` Citizens, `35,936` Households and `10,080`
  Lots. Only the enabled copy states `construction_charge_per_floor_tile = 1` and
  `city_sale_interval_days = 1`. Paired runs alternate which copy goes first.
- Command `scripts/test.sh --filter 'FullyQualifiedName~LandDepartureTests.Departure_and' -- --logger 'console;verbosity=detailed'`.

| Capture | Observed time | Scope |
|---|---|---|
| Day offer with nobody able to buy, worst / mean paired difference | 2.026 / 1.646 ms | 32 pairs. The richest-Household check runs before the tenant walk |
| The same offer before that check | 17.556 / 7.258 ms | Every tenant was tested on every Lot |
| Departure holding no Lot, mean paired difference | 0.216–0.378 ms | 32 pairs per run, two runs |
| Departure selling one built Lot, mean paired difference | 3.843–5.463 ms | 32 pairs per run, two runs. The worst pair reached 18.7 ms |
| The sale step alone, its parts | Lot scan 0.097 ms, capital pass 0.508 ms, one Lot price 0.035 ms | Single reads |
| First offer selling all 8,723 city-held built Lots | 8.926–12.774 ms | Every Household held 10,000 |

- A departure scans every Lot, because no column lists a Household's Lots. The scan is 0.1 ms
  here. A per-Landowner list would need a derived column with a rebuild audit, and the scan does
  not yet justify one.
- Repeat at `1,000,000` Citizens on a quiet host before slice 7 claims the Tick budget fits.

### 6. Compulsory purchase

- A Street through a Lot and the player's `Demolish` pay the Landowner the Lot's slice 2 price from
  the treasury, replacing `World.PayDisplaced`. Tenants are evicted with no payment. A city-held Lot
  costs nothing.
- Vacant-Lot assembly (`LocalLayout`) accepts only Lots that the city or one Landowner holds.
- A new player command buys a vacant owned Lot for the city at its land price. It is refused when
  the treasury cannot pay. The command is encoded in `Borough.Formats`, and the shell gets the
  tool.
- Acceptance: an assembly blocked by a second Landowner proceeds after the player buys that Lot,
  shown headless and in a driven shell capture.

### 7. Base adoption and the Day-400 run

- Set the slice 2, 4 and 5 keys in `rulesets/base/`.
- Run the founding log to Day 400 and record Landowner, Household, Business and treasury
  balances in [Evidence](#evidence). Then decide L13's holding cost and whether base drops its
  levies and housed rebate.
- Re-record the founding package hash and goldens.
- Acceptance: the plan's base acceptance below, with the result recorded.

## Acceptance

- Owner draw: a profitable founded Business pays its owner and keeps its reserve. A Business
  short of its reserve pays nothing. `MoneyIsConserved` holds.
- The base founding city meets slice 4's operation checks to Day 200, with no levy or rebate
  patch, or with a recorded reason for keeping one.
