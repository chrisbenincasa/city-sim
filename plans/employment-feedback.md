# Employment feedback

Board row: Employment feedback. Evidence: [#67](https://github.com/chrisbenincasa/city-sim/issues/67),
[#66](https://github.com/chrisbenincasa/city-sim/issues/66), [#51](https://github.com/chrisbenincasa/city-sim/issues/51), PR #70.

## Outcome

A world that loses its posts recovers employment without a Ruleset edit. Citizens who find every
post in reach full raise employers on zoned trade Lots. Employers that cannot pay their staff fail,
so unemployment settles where customers can support the posts.

## Current behaviour (checked 09/30/2026 at `b3fc533e`)

- `World.Unpremise` keeps a Business's staff when it loses its Unit. They stop commuting and stop
  seeking. On `pictured.toml` before PR #70, about 960 Citizens were held this way at Day 20.
- `[[business]] goes_bankrupt_after_short_paydays` is optional, and absent means never. Only
  `insolvent.toml` and `funded.toml` state it.
- A tier-0 Zone Rule builds only while the Unplaced Pool is non-empty. A tier-1 Zone Rule
  (`build_threshold_days`) reads District market demand in `ZoneRuleEngine.RecomputeDemand`:
  unserved shopping outings and Rule Instances starving on a market. No term reads employment.
- `EmploymentEngine` records `EmploymentState.NoVacancy` when every post in reach was full. Nothing
  reads it.
- Founding (`PlacementEngine`, `adr/0146`) lets a jobless Citizen whose Household holds
  `founding_band` found a Business with no premises. Placement re-lets vacant Units to those
  Businesses.

## Decisions

### Staff on Unit loss

- A Business that loses its Unit dismisses all its staff, founder included. No column records the
  founder, and an owner whose shop has closed looks for work like anyone else.
- Dismissed Citizens return to the job pass and can register as jobless.

### Failure

- Replace `goes_bankrupt_after_short_paydays` with `[[business]] goes_bankrupt_after_days_in_arrears`.
- A Business falls into arrears on a payday it cannot meet in full. A payday that pays wages in
  full ends arrears. A payday with nobody owed leaves them as they were, because unpaid staff
  leave and an employer with no staff and no money must still fail.
- Unpaid wages are written off, not carried, so arrears is a state and not a debt.
- The key is required for a trade that declares a wage. Failure is always on; the Ruleset sets only
  the clock.
- Days rather than paydays, so the pay period does not change how fast a Business fails.
- A zero balance is not the test. A healthy shop that pays out what it earns, and a trade raised
  by a Zone Rule, both stand at zero.
- Bankruptcy dismisses the staff and frees the Unit, as winding up does now.
- Provisional values: `pictured.toml` 8 Days, to match its shopfront tenancy clock. `insolvent.toml`
  21 and the two school files 14, which reproduce their old payday counts on weekly pay. Every
  other waged file 28.
- A founded Business without premises owes its founder a wage and sells nothing, so it now
  fails. On `pictured.toml` at 2,000 Citizens, seed 0, 40,960 Ticks, 153 trades were wound up,
  almost all of them founded and never premised, and premised employment rose from 1,425 to
  1,472.

### Jobless signal

- Count, per District, Citizens at `EmploymentState.NoVacancy` and how long they have waited.
- `BeyondReach` and `BelowCredential` do not count. A new employer does not help them; roads and
  schools do.
- The count is a sum of individual Citizens' states per District, like the Unplaced Pool. It is not
  a city-wide meter.

### Construction

- Vacant Units are re-let first. A founded Business waiting for premises takes a new Unit before an
  instantiated trade.
- A tier-1 trade Zone Rule answers the jobless signal past its `build_threshold_days`, on a vacant
  Lot zoned for trade.
- A Building raised this way starts its declared trades at zero balance (`adr/0148`). It needs no
  founder's money and pays wages from sales.
- With no vacant zoned Lot, the signal goes unanswered. Core reports the unanswered count; the shell
  shows it so the player can zone more trade land. Redeveloping occupied Lots is out of scope.

## Steps

1. Release staff on Unit loss. Test that a Business losing its Unit leaves its workers jobless and
   seeking. Re-record goldens.
2. The arrears clock. Replace the key in the loader, `RulesetKeyNotes`, `insolvent.toml` and
   `funded.toml`; add it to every Ruleset whose trades declare a wage, including `pictured.toml`.
   Regenerate the key reference and schema. Re-record goldens.
3. The jobless signal per District, with a census counter and a test.
4. Construction answers the signal. Decide which form the trade Zone Rule raises (open question).
5. Acceptance run.

## Open questions

- Which commercial form a Zone Rule raises. The trade Zone Rule on `pictured.toml` names
  `shopfront`; world creation raises car-park centres and other forms from PR #69.
- Whether the jobless signal and the market signal share one threshold or each has its own.
- A Business with no wage bill cannot be in arrears, so a founder-only Business that holds a Unit
  and sells nothing never fails. Leave it until a measurement shows it; the fix would be a clock on
  a premised Business with no staff.
- Whether `--commute` prints the working-age denominator (#66) here or in its own fix.

## Acceptance

- On `pictured.toml` at 2,000 Citizens, a world that loses most of its posts (for example by
  demolishing trade Buildings mid-run) recovers employment within a stated number of Days, with no
  Ruleset edit. Name the Ruleset, population, seed and Tick count.
- Some Citizens stay jobless at steady state, and failing Businesses appear in the run.
- A driven shell run shows the trigger, the new employer and the returning commutes.
