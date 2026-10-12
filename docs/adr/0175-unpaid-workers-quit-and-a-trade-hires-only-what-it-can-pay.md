# Unpaid workers quit, and a trade hires only what it can pay

Status: accepted, 10/11/2026.

## Decision

- `[[business]] quits_after_days_unpaid` is an optional, per-trade key. Absent means workers never
  quit for want of pay.
- `CitizenTable.DaysUnpaid` is saved per Citizen. A payday that pays a worker less than they are
  owed adds the trade's pay period. A payday that pays them in full resets it. Hire resets it.
- When the run reaches the key's value, `WageEngine` dismisses the worker through `World.Dismiss`
  after paying what the till held. The worker forfeits the rest and rejoins the job search.
- A trade that sets the key hires only while its till holds one pay period's wages for its staff
  plus the new hire (`World.HasJob`).

## Why

- `WageEngine.Pay` pays workers in list order until the till is empty. Before this change the
  workers left unpaid stayed until the employer went bankrupt. In the base founding city 33–60% of
  each Day's payroll went unpaid while profitable grocers accumulated money.
- Quitting on its own did little. In that city N = 3 produced about 6,300 quits in 120 Days, and
  the unpaid share fell only from 23% to 21%. Each vacancy was filled by the next job seeker, who
  also went unpaid.
- The first hiring rule tried was a freeze while in arrears, and it trapped employers. A trade
  that lost all its staff had a payday with nobody owed, which leaves arrears unchanged. It could
  never hire again and went bankrupt.
- The affordability rule has no such trap. With N = 1 the unpaid share after Day 60 fell to 12%.

## Consequences

- The new saved column moves the State Hash. The save format moves to version 18, and the goldens
  are re-recorded.
- Rulesets without the key behave exactly as before. Measured on base with M2's content, the
  payroll on Day 120 matched to the unit.
- The affordability test uses the trade's flat wage. It ignores grading by Skill Tier and
  experience, so it is approximate where a Ruleset grades wages.
- Quitting changes which employers hold workers. It does not move money out of Business tills. Over
  400 Days the base city still ends with the treasury at zero and grocer tills holding most of the
  money, with or without quitting.
