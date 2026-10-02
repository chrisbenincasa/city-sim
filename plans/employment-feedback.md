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
- `Citizens.NoVacancySince` records the Tick a Citizen entered `NoVacancy`. The wait is elapsed
  time from it, like `Shopping.UnservedSince`.
- A job search that draws no employer concludes nothing and leaves the earlier state in place.
  Writing `None` over `NoVacancy` restarted the wait at random under sampling.
- The census prints `no vacancy`, the seekers who found every post in reach full.

### Construction

- A tier-1 trade Zone Rule reads two triggers per District. Market demand passes
  `build_threshold_days` in household-Days. The jobless wait passes `jobless_threshold_days` in
  Citizen-Days. The second key is separate because a Building adds a fixed number of posts, not a
  fixed amount of relief for hunger. Absent means the Rule does not read joblessness, and the key
  requires `build_threshold_days`.
- On a sampled Building of the Rule's kind with a vacant Unit, either trigger opens the kind's trade
  in that Unit at zero balance (`World.OpenInVacantUnit`, shared with world creation's
  `FillUnits`). No construction permission is asked, because the Building stands.
- Re-opening skips the per-District cooldown and does not restart it. The cooldown gives a new
  Building time to stock before demand is read again, and a re-opened Unit hires on the next job
  pass. With the 8-Day cooldown applied, the acceptance run re-opened 8 Units in 30 Days and ended
  at 672 employed.
- Founded Businesses take vacant Units through placement on every pass, so they usually reach a
  Unit before demand passes a threshold. Nothing enforces that order.
- On a vacant Lot either trigger raises a Building as before, under the cooldown.
- No Zone Rule can raise a commercial form, because `World.ConstructionPermission` admits only the
  housing patterns. A demolished centre's Lot stays empty. The Trade forms in play board row owns
  that (PR #78).
- `pictured.toml` states `jobless_threshold_days = 4`, provisional. Thresholds 2, 4, 8 and 16 gave
  identical acceptance runs, because the wait after a mass bankruptcy passes all of them at once.

## Steps

1. Release staff on Unit loss. Test that a Business losing its Unit leaves its workers jobless and
   seeking. Re-record goldens.
2. The arrears clock. Replace the key in the loader, `RulesetKeyNotes`, `insolvent.toml` and
   `funded.toml`; add it to every Ruleset whose trades declare a wage, including `pictured.toml`.
   Regenerate the key reference and schema. Re-record goldens.
3. The jobless signal per District, with a census counter and a test. Done, with the
   `jobless_threshold_days` trigger.
4. Construction answers the signal by re-opening vacant Units. Done.
5. Acceptance run. Done as an instrument test; the driven shell run remains.

## Open questions

- Re-opened Businesses fail often. In the acceptance run 102 re-openings refilled 48 Units over 30
  Days, and employment settled about 15% below the unshocked control. A guess, unmeasured: a
  Business opens at zero balance while Household spending is depressed, so it falls into arrears.
- A Business with no wage bill cannot be in arrears, so a founder-only Business that holds a Unit
  and sells nothing never fails. Leave it until a measurement shows it; the fix would be a clock on
  a premised Business with no staff.
- Whether `--commute` prints the working-age denominator (#66) here or in its own fix.

## Acceptance

- `EmploymentRecoveryTests`, instrument tier: `pictured.toml`, 2,000 Citizens, seed 0, 81,920
  Ticks (40 Days). On Day 10 three in four premised Businesses go bankrupt, through the same steps
  as `WageEngine`. Employment must reach 75% of its pre-shock level by Day 15 and stay there to
  Day 40, with no Ruleset edit.
  - With the jobless signal: 1,138 before the shock, 363 after, 1,205 on Day 15, lowest 1,079
    after Day 15, 1,079 on Day 40. Passes.
  - Without it: 894 on Day 15, under the bar from Day 23, 504 on Day 40. Fails, as the second test
    asserts.
  - Unshocked control: 1,472 on Day 20, 1,452 on Day 40.
- Some Citizens stay jobless at steady state, and failing Businesses appear in the run.
- A driven shell run shows the trigger, the new employer and the returning commutes.
