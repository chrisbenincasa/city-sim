# Row 30 — Diagnose decline, intervene and observe recovery

State: **design**. Scoping checked against `f7cc9ae` on 2026-09-15. No implementation
owner claimed. Coordinate implementation with the agent working on 0077 and the first playable.

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

## Decisions required before implementation

1. **Attribution and consequence.** For each admitted Trip Purpose/Fate, identify the affected
   Citizen, Household, Business or premises and the resulting missed activity, tenancy loss or
   Building decline. Begin with commuting; do not assign a failure to both endpoints by default.
   Decide how several affected individuals establish premises pressure without erasing individual
   decisions or counting the same failure again through a downstream supply shortage.
2. **Duration and recovery.** Define what starts an episode, what confirms it remains unresolved,
   and what successful activity clears it. Specify retry/re-evaluation when no further Trip occurs,
   plus job/home changes, departure, deletion and slot reuse. An unrelated successful Trip must
   not clear a failed commute. Use elapsed Ticks, never accumulated failure counts. Define any
   persistence/observation duration independently of the Building condemnation window; authored
   tuning belongs in the Ruleset. Do not infer continuity from a stale last Fate.
3. **Below-tolerance conditions.** Trace the current housed-choice/departure path and select a
   concrete local condition and intervention. Decide whether existing individual movement already
   supplies the consequence, or a further premises mechanism is necessary. Record that decision
   here and in the owning design text. If it needs independently scheduled work, split a named
   board outcome with its actual prerequisite; do not silently drop this source from row 30.

The first implementation step is a small headless broken-commute/repaired-connection probe that
resolves decisions 1–2 against actual activity transitions. Do not mark this plan ready until
the affected subject, consequence and episode lifecycle are specified and decision 3 is disposed of.

## Evidence and acceptance

- Inspect the subject, cause, episode start/elapsed duration, actual consequence and latest
  recovery evidence. Link to the affected Citizen and relevant location through existing navigation.
  Distinguish ongoing failure, a historical failure and recovery awaiting confirmation.
- Demonstrate a stable baseline, broken connection, sustained failed commute and consequence,
  player reconnection, then successful activity and stopped pressure. Run a matching no-intervention
  case to show that time alone does not produce the claimed recovery.
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
There is no identified blocker to completing this design now.
