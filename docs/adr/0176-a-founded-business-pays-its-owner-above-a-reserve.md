# A founded Business pays its owner above a reserve

Status: accepted, 10/11/2026.

## Decision

- `BusinessTable.Owner` is a saved, severable handle to the Household that founded the Business.
  `World.Found` sets it. A Business a Zone Rule opened has no owner.
- `[[business]] pays_owner_above_pay_periods` is an optional, per-trade key. Absent means no draw.
- On a payday that paid every worker in full, `WageEngine` pays the owner whatever the till holds
  above a reserve. The reserve is the key's value times the pay period times the Day's graded wage
  bill.
- A payday that paid nobody pays no draw. A staffless Business has a wage bill of zero, and the
  draw would otherwise empty its till.
- An owner that departs or dissolves severs the handle. The Business keeps trading and pays no draw.

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
- Zone-raised Businesses still keep their profit. Whether they pay a draw, and to whom, is open in
  `plans/money-circulation.md`.
