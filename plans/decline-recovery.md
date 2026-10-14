# Row 30 — Diagnose decline, intervene and observe recovery

State: **ready**. Designed in a session on 10/13/2026 against `e27fdaf6`; decisions below and in
`adr/0177`. Owner: `decline-recovery` worktree. Coordinate Ruleset keys and saved state with 0077.

## Outcome and boundary

A player can inspect a struggling Building or tenant, identify a sustained cause, follow its
Evidence to an actionable place or person, intervene, and observe recovery or persistent failure.
Reuse the existing supply diagnosis interface. Add a real consequence from at least one additional
failure source; explanatory labels or counters alone do not complete this work.

Recommended first slice: a failed commute caused by a broken connection, followed by a player
restoring that connection. Show the affected Citizen and premises, the missed activity and its
consequence, and a subsequent successful commute. Keep sustained Trip failure and below-tolerance
conditions in the design scope; the latter needs the explicit disposition described below.

This slice does not own founding content, paid Outside supply, labour-dependent production,
Ruleset runtime factoring, trajectory notifications/Pins, clearance programmes or contagion.
It must not depend on private production becoming labour-dependent to demonstrate a consequence.
Recovery means the affected activity works again and its ongoing pressure stops; it need not
instantly restore lost occupants or revive an abandoned Building.

## Current mechanisms and gaps

- `Rules/RuleEngine.Stop` starts `RuleInstances.StarvedSince` for `Blocking.Supply` and clears it
  for other blocking reasons; successful execution also clears it. This measures elapsed failure.
- `Rules/ZoneRuleEngine.Worst`, `Shed` and `Condemn` consume supply-failure duration. Household,
  Business and premises failures are filtered separately. Preserve that separation: a struggling
  tenant must not automatically condemn its neighbours' premises.
- `World.RecordReachFailure` counts failed job-search occasions. It is not a Trip-failure clock.
  `World.ResolveTrip`/`RecordTripFate` retain the Citizen's last Fate and end Day; resolved Trip
  storage is released. A last failure cannot establish that failure remained continuous.
- `Main.Diagnosis.SupplySummary` explicitly reports supply status. Building/Business summaries
  reuse it; `Main.Schooling` separately reports staffing. Extend the overall service explanation
  so available supplies cannot imply usable staffed capacity. The viability school observation
  is a regression scenario, not proof that the current wording literally says “healthy”.
- Below-tolerance conditions cannot be implemented as a generic scalar: `CONTEXT.md` defines
  tolerance through individual Household comparison with a Hinterland. The old 0047 statement
  that residential choice is absent is not a verified current prerequisite.

## Decided

Decided in a design session on 10/13/2026.

- **The commuting Citizen holds a failed commute.** A run of failed commutes is timed on the
  Citizen, not on the Household, the Business or the premises. At a Ruleset deadline the Citizen
  loses the job through the existing dismissal, and the post opens to job search. The Business
  needs no new rule: it already loses that worker's labour each missed shift. Moving home in
  response can follow once housing choice reads commutes.
- **A failed outbound commute starts the Citizen's failure clock.** Both plan-time failures count:
  *no route found* and *exceeded commute budget*. Either way the worker misses the shift.
  - The clock measures elapsed Ticks since the first failure. Days off count. Missed shifts are
    never tallied.
  - No retry needs adding: the Citizen departs again each working day.
  - The next completed outbound commute clears the clock. Other Trip Purposes cannot clear it.
  - A failed homeward commute starts nothing, because that Citizen stays at work and keeps working.
  - Dismissal at the deadline, a job change, death and departure end the clock without recovery.
  - The deadline is a new Ruleset value in Days, independent of `condemn_after`.
- **An over-budget commute counts because it is a real mismatch.** One completed commute resets
  the clock, so intermittent congestion cannot reach the deadline. Only a commute that fails every
  working day for the whole deadline costs the job, and each dismissal relieves the road.
  The budget test prices free-flow only, so congestion alone never fails it (probe below).
  Distance and a full parking shed are its only causes. Counting it is provisional until a
  measurement shows how often a full parking shed dismisses workers in a growing city. The acceptance list below
  carries that measurement.

- **Commute failure adds no premises pressure.** The employer feels it only as missing labour,
  and a labour shortage does not start `StarvedSince`. So one failed commute is never counted
  twice, once on the worker and again through a downstream supply shortage.
- **Below-tolerance departure is split into its own board row.** A housed Household cannot leave
  the city, and `HousingUtility.Worth` reads only rent and centrality, so no local condition a
  player could fix exists to fall below tolerance. The new row waits on export-base slice 5,
  which builds the housed comparison against the Hinterland. Once both land, a worker dismissed
  for failed commutes enters a jobless spell, and that spell can take the Household out of the city.

- **A Building nobody can reach is condemned by its own cut-off clock.** This is a new, named
  premises mechanism, separate from Rule starvation, as `adr/0079` requires of any road-shaped
  pressure. A Building is cut off when its Access Point's Segment shares no foot or car component
  (`RoadNodeTable.FootComponent`, `CarComponent`) with any Outside Connection, or when it has no
  Access Point (`Address.None`). The clock runs in elapsed Ticks from the road edit that cut it off,
  clears when an edit reconnects it, and condemns at its own Ruleset deadline. It covers homes,
  shops and workplaces alike, including a workplace whose posts no one can fill. The clock never
  runs in a world with no Outside Connection, because nothing defines what such a city is cut off
  from. Recorded in `adr/0177`.
- **Money handles a reachable shop that loses trade.** A staffed shop with no sales already goes
  bankrupt through the arrears clock. The cut-off clock measures access and money measures sales,
  so neither counts the other's failure. The remaining money gaps go elsewhere:
  - Households keep one failed outing per lap on an unreachable known shop, because `Failed`
    resets on any purchase. Issue #147.
  - `ZoneRuleEngine.Reopen` reopens a vacant Unit nobody can reach. Issue #148.
  - A founder-only shop with no wage bill never fails. The "Posts that follow margin" row owns it.
  - District Pool sales ignore roads by design. The cut-off clock ends them when it condemns.

- **The player reads both clocks in existing panels.**

  | Panel | While failing | Afterwards |
  |---|---|---|
  | Household, per Citizen | Days unable to reach work, the Fate, days left before dismissal, a link to the workplace | Commute restored on a Day, or the dismissal with its duration |
  | Workplace (Business) | How many workers cannot reach it and since when, each linked to its Household. Derived from the workers' clocks, never saved. | Nothing extra |
  | Building | Cut off since a Day, the reason, days left before abandonment | The abandonment record names the cause |
  | Service summary | The supply line plus a staffing line, so full stocks cannot read as a working service | Unchanged |

  - Each Citizen saves its most recent ended episode: start, end and outcome (recovered or
    dismissed). One record per Citizen keeps it bounded.
  - There is no *recovery awaiting confirmation* state. One completed commute clears the worker's
    clock, and a reconnecting edit clears the cut-off clock.

- **Two optional Ruleset keys set the deadlines. Leaving one out turns its clock off.**

  | Key | Starting value | Scope |
  |---|---|---|
  | `[jobs] dismissed_after_failed_commute_days` | 10 Days | City-wide, because the worker holds the clock |
  | `[[building]] abandoned_after_cut_off_days` | 14 Days | Per kind, beside `condemn_after_days` |

  - Only the base package and the demonstration fixture set them. Other Rulesets keep their behaviour.
  - The new saved fields move the State Hash even with the keys absent, so the goldens are
    re-recorded once.
  - Both values are provisional. The congestion measurement tests the first and a playtest the second.
  - Each clock saves elapsed Ticks as Tick + 1, so zero means no clock. A freed slot is zeroed
    (`Rows.FreeSlot`), so a reused Citizen or Building starts clear.

- **One diagnostic fixture demonstrates both clocks from one cause.** Homes stand on the side of
  a gap that holds the Outside Connection; a workplace and a shop stand on the far side; one bridge
  Segment joins them. Removing the bridge starts every far-side worker's commute clock and the far
  side's cut-off clock.

  | Run | Player action | Expected result |
  |---|---|---|
  | Repaired in time | Rebuild the bridge on Day 5 | Next commutes complete, both clocks clear, nobody is dismissed |
  | Repaired late | Rebuild on Day 12 | Workers dismissed on Day 10; the far side regains access and job search refills the posts |
  | Not repaired | None | Workers dismissed on Day 10, far-side Buildings abandoned on Day 14 |

  - Remove the bridge outside commute hours. A Traveller on a removed Segment still arrives until
    #132 is fixed.

## Probe results

`BridgeCommuteProbe` (instrument tier) builds the fixture on `congested.toml` with a gate kind:
two 2×2-block districts, a gate on the near side, one bridge Segment. Measured on 10/13/2026.

- The budget test prices free-flow only. With 10,000 vehicles on the bridge, a planned car
  commute across it is unchanged (71,580 raw), while a driver on the bridge pays 39 times free-flow.
  Arc times are precomputed from free-flow; congestion is charged only in `TripEngine.Enter`.
- 271 of 1,000 Citizens commute across the bridge. Removing it at 23:00 found no Traveller in
  flight, so #132 does not touch the fixture.
- Each crosser fails once per working Day with *no route found* and stays home. No homeward
  failure follows. Nothing retries within the Day, and the next Day's departure tries again.
- No current mechanism ends the failure. After 13 Days unrepaired, all 271 crossers still hold
  the same job, and none moves or finds work on their own side.
- One completed commute follows the repair. Rebuilt on Day 5 or Day 12, all 271 complete the
  next Day's commute and city trips return to the baseline 1,070 per Day.
- The cut-off test works as designed: all 32 far-side Buildings lose the gate's car component
  on removal and regain it on repair. Nothing is abandoned today.
- No commute exceeded the budget in any run.

## Built

- **The commuting Citizen's failure clock.** `CitizenTable.CommuteFailedSince` saves the first
  failed outbound commute's Tick + 1. `World.RecordTripFate` starts it on a failed outbound commute,
  clears it on a completed one, and dismisses through `World.Dismiss` once a failure lands at or
  past `[jobs] dismissed_after_failed_commute_days`. `Dismiss` and `Employ` clear it. The base
  package states 10 Days. Tests: `FailedCommuteClockTests`.
  - The dismissal lands on the first failed departure at or past the deadline. A worker who is
    off on the deadline Day loses the job at the next working Day's failure.
- **Probe with the 10-Day deadline.** Unrepaired, all 271 crossers lose the job on Day 13, ten Days
  after their first failure on Day 3. 148 find a job on their own side that Day and 123 stay
  jobless. Repaired on Day 12, nobody is dismissed.
- **Re-hiring can fail the budget.** After the dismissals, 5 same-side commutes exceeded the
  Commute Budget, though the job search had accepted them. Two clocks started and cleared the next
  Day. Congestion cannot cause this, so a full parking shed is the likely cause. The parking
  measurement in the acceptance list should confirm it.

- **The readout.** Each Citizen saves its most recent ended episode in three columns: start Day,
  end Day and outcome (`CommuteEpisodeOutcome`: recovered or dismissed). Only a completed commute
  and the deadline dismissal write it. A run cut short by another dismissal or a job change leaves
  the previous record. `CitizenEvidence.FailingCommute` gives the running clock's start and the
  first Tick a failure dismisses; `CitizenEvidence.LastCommuteEpisode` gives the record.
  `Evidence.FailingCommutersOf` derives a workplace's failing workers from their clocks.
  - Household panel, per Citizen: days unable to reach work, the Fate, days left before
    dismissal; afterwards the restored commute or the dismissal with its duration.
  - Business panel: how many workers cannot reach it, since when, each linked to its Household.
  - Building summary: a staffing line under the supply line for any Building that serves a Need.
  - Driven on 10/13/2026 from city saves of the bridge fixture at a 3-Day deadline: failing on
    Day 4, repaired on Day 4 and read on Day 6, unrepaired and read on Day 8. The Household panel
    read "Unable to reach work since Day 3, 1 Day ago · no route to work" and "Dismissal in 2 Days
    if no commute completes", then "Commute restored on Day 5 after 2 Days of failed commutes" or
    "Dismissed on Day 6 after 3 Days of failed commutes". Business 45 listed its 4 failing workers,
    each linked to a Household. The unhired school from `ServiceStaffingTests` read "No staff · 0
    of 4 posts filled, so it serves nobody" beside "No supply shortfalls reported".
  - Observation exposed two defects, both fixed. The lines counted Days from 0 while the HUD clock
    counts from 1. A re-hired Citizen's dismissal read as if the new employer had dismissed them,
    so it now reads "Lost the previous job".
  - The shell still numbers Days two ways: the HUD clock from 1, the Health panel and the debug
    readout from 0. These panels follow the HUD clock.

- **The cut-off clock.** `BuildingTable.CutOffSince` saves the Tick + 1 of the road edit that cut a
  Building off from every Outside Connection; `BuildingTable.AbandonedCause` saves why the city
  abandoned it (`AbandonmentCause`). `World.TimeCutOffBuildings` runs at the end of each Tick. It
  re-judges every Building when the Tick changed the road graph, the gates or the Ruleset, and at
  each Day boundary abandons every Building past `[[building]] abandoned_after_cut_off_days`. A
  Building raised later judges itself in `CreateBuilding`. A load judges nothing, because the
  clocks are saved. The base package states 14 Days, with `collapses_after_days = 30`, on
  `dwelling`, `shopfront` and `school`. Tests: `CutOffClockTests`.
  - The deadline is checked daily rather than on a Zone Rule visit. The visit is a random sample
    and lagged the deadline by up to four Days on the bridge fixture.
  - The Building panel shows "Cut off since Day N", the reason (no Access Point, or no road to an
    Outside Connection) and Days left; afterwards "Abandoned on Day N" with the cause. Every
    abandonment path now records its cause.
- **Probe with both shipped deadlines.** Unrepaired, all 32 far-side Buildings are abandoned at the
  start of Day 17, fourteen Days after the removal. Because every far dwelling is also a shop's
  premises, 76 crossers re-hired on the far side lose those jobs that Day and jobless crossers rise
  from 121 to 197. Repaired on Day 5 or Day 12, nothing is abandoned.

- **Driven on the bridge fixture.** Saves at Day 6 and Day 19 on the HUD clock, from the probe's
  run with both deadlines, loaded into the shell with far dwelling 33 selected. Day 6 reads "Cut off
  since Day 3 · no road reaches an Outside Connection" and "Abandoned in 12 Days unless a road
  reconnects it". Day 19 reads "Abandoned on Day 18 · cut off from every Outside Connection", with
  no Households and no workers. The abandoned far district draws exactly as an occupied one, so
  the map alone does not show the loss; only the panel does.

## Next step

The remaining acceptance items: the no-intervention case, the parking measurement and the edge
cases in the list below.

## Evidence and acceptance

- Inspect the subject, cause, episode start/elapsed duration, actual consequence and latest
  recovery evidence. Link to the affected Citizen and relevant location through existing navigation.
  Distinguish an ongoing failure, a recovery and a dismissal.
- Demonstrate a stable baseline, broken connection, sustained failed commute and consequence,
  player reconnection, then successful activity and stopped pressure. Run a matching no-intervention
  case to show that time alone does not produce the claimed recovery.
- Measure over-budget dismissals in a growing city whose parking fills, with no broken
  connection. Congestion cannot cause one, so a full parking shed is the dynamic cause to test.
  Report dismissals per thousand workers per game month at the shipped deadline, and whether the
  city's employment settles or spirals. This decides whether over-budget keeps counting.
- Cover intermittent failures, unrelated successful Trips, no retry, multiple tenants, subject
  removal/slot reuse and simultaneous supply/Trip failure. A change in observation cadence must
  not change the elapsed failure being measured.
- Preserve existing supply recovery and tenancy/shedding behaviour; start from
  `LastTripFateTests`, `TripCommandTests`, `TenancyEndsTests`, `BusinessTenancyTests`,
  `OccupancySheddingTests` and `ThinnedRulesetTests` when choosing focused checks.
- Save/reload during failure and after repair; verify Input Log replay, State Hash and thread-count
  equivalence. Saved episode state must be bounded and hashed; derived indexes must rebuild.
- Check an unstaffed school with available supplies: the summary must explain unavailable service
  and point to staffing evidence. Do not change the staffing/production model as an interface fix.
- Before the shell demonstration, verify deterministic hover aiming with the drive skill. Capture
  trigger, response and consequence, and record what observation exposes here. Run the working
  test lane before committing implementation and full/invariant checks at the completed milestone.

## Coordination

0077 owns loader/authoring contracts and coordinates runtime factoring. Agree any new saved state
and Ruleset keys before changing those surfaces. The first playable owns its founding and shortage
world; use a separate diagnostic fixture and agree how its evidence will integrate later.
The design is complete. No blocker is identified for the probe.
