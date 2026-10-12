# Money circulation: profit, rent and premises costs

## Outcome

Money that reaches a Business or a Household has somewhere to go next. A city running for hundreds
of Days keeps money moving between Households, Businesses and the treasury, and no one holder
accumulates it.

## Current behaviour (checked 10/11/2026 at `a5553cbb`)

| Business outflow | Built | Recipient |
|---|---|---|
| Wages | `WageEngine` | Worker's Household, less income tax to the treasury |
| Business tax | `BusinessTaxEngine` | Treasury |
| Rule inputs | `RuleEngine` trades | Selling Business, or outside the city |
| Levies | Content `[[policy]]` only | Treasury |
| Profit to an owner | `WageEngine.Draw` | Founding Household, or a housed Household drawn at random |
| Rent | No | — |
| Premises upkeep | No | — |
| Utilities | No | — |

- Households pay for shopping, tuition, income tax and content levies. Nothing else leaves a
  Household.
- `[[building]] rent` is a placement filter only. Placement skips a dwelling a Household cannot
  afford. Nothing charges it.
- `World.Found` saves the founding Household as the Business's owner. A Zone Rule's Business has
  no founder, and the treasury's opening grant funds it. It draws an owner on its first payday
  with a surplus.
- Street upkeep is the only maintenance cost, and the treasury pays it.

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
| Housed-only rebate | Built and in base (`housed` Readout) | Replace with rent once its recipient is decided |
| Rent collection | Design | Decide the recipient. Coordinate with the land ownership row |
| Premises upkeep | Scoping | Decide whether upkeep is Money only or consumes repairs |
| Utility billing | Scoping | Waits on the Power, Water and Sewage delivery row |

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

### Open

- **Rent recipient.** The treasury makes rent a tax and enlarges the treasury's surplus. An owning
  Household makes rent a second income channel. No landlord entity exists yet.
- **Rent and the rebate.** Rent may let base drop its levies and rebate. With owner draw and no
  levies, income tax still piles up in the treasury and employment collapses
  ([slice 4 record](first-playable-founding-loop.md#findings)).

## Acceptance

- Owner draw: a profitable founded Business pays its owner and keeps its reserve. A Business
  short of its reserve pays nothing. `MoneyIsConserved` holds.
- The base founding city meets slice 4's operation checks to Day 200, with no levy or rebate
  patch, or with a recorded reason for keeping one.
