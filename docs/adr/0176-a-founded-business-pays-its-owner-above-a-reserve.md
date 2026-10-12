# A founded Business pays its owner above a reserve

Status: accepted, 10/11/2026.

## Decision

- `BusinessTable.Owner` is a saved, severable handle to the Household that receives the draw.
  `World.Found` sets it to the founding Household.
- A Business with no owner and a surplus on a payday draws a housed Household as owner and keeps
  it. The draw is a counter hash of the Business's id and the payday (`PurposeTag.BusinessOwner`).
  Each housed Household is equally likely. Amended 10/12/2026.
- `[[business]] pays_owner_above_pay_periods` is an optional, per-trade key. Absent means no draw.
- On a payday that paid every worker in full, `WageEngine` pays the owner whatever the till holds
  above a reserve. The reserve is the key's value times the pay period times the Day's graded wage
  bill.
- A payday that paid nobody pays no draw. A staffless Business has a wage bill of zero, and the
  draw would otherwise empty its till.
- An owner that departs or dissolves severs the handle. The Business draws a new owner at its next
  payday with a surplus.

## Why

- Profit had no path back to Households. In the base founding city over 400 Days, grocer tills
  ended holding most of the city's money while the treasury reached zero (`plans/money-circulation.md`).
- Content levies and a daily rebate were the only return path. Households waiting for housing
  collected the rebate and emigrated with it.
- The draw needs no landlord decision, so it could be built before rent.

## Consequences

- The new saved column moves the State Hash. Save format version 18 carries it with
  `CitizenTable.DaysUnpaid`, and the goldens are re-recorded.
- Rulesets without the key behave as before.
- The draw moves money between two holders in the city. It creates and destroys none.
- A Zone Rule's Business pays its profit to a resident Household. Without this, about 240 grocers
  held 2.5M–3.25M of the founding city's money through Day 200 (`plans/money-circulation.md`).
- The pick happens at a payday rather than at opening, because generated cities raise Buildings
  before they house anyone.
- A pick walks every Household slot. It runs only for a trade with the key, on a payday with a
  surplus and no owner.
