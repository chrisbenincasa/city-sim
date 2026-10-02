# System review brief

## Outcome

Review the current game one system at a time. Produce actionable findings about correctness,
design, performance, and integrity or maintainability. Establish what the code does before
judging whether its design serves the game.

This brief defines the investigation; individual sweep reports record findings and verification.
Implementation and remediation are separate work, selected after findings are reviewed.

## Baseline and boundaries

- Start from `main` at `919290a52abfdd95841c61c0c21ad7883d62bebc`.
- Record the exact revision for each sweep. Recheck affected findings if the code changes.
- Review implemented behaviour, shipped content, and the tools that build, test and present them.
- Treat documentation as intent and code plus tests as evidence of current behaviour.
- Exclude unmerged worktrees. Coordinate with their owners before recommending overlapping work.
- Distinguish a broken implemented contract from a planned or deliberately deferred capability.
- Challenge documented decisions when their assumptions or costs no longer hold. An ADR explains
  a choice; it does not exempt that choice from review.
- Evaluate alternatives within the project's requirements for deterministic arithmetic,
  individual Citizen decisions and the Core/Godot boundary. Flag any proposed change to those
  requirements as a separate decision for the user.
- Keep this work advisory. It creates no new release gate or standing audit process.

## Finding categories

| Output | Admission standard | Required substance |
|---|---|---|
| Obvious bugs | A reachable input or state violates an established contract. | Show the trigger, expected and actual behaviour, responsible path, and a reproduction or decisive code trace. Identify the missing regression check. |
| Bad design decisions | An existing choice causes a concrete gameplay, ownership, coupling, or change-cost problem. | Explain the original rationale, the observed cost, a feasible alternative, and its trade-offs. Separate a decision needed from a repair ready to implement. |
| Clear performance issues and poor performance implementation | Work is demonstrably unnecessary, scales poorly on a reachable workload, or causes a measured budget problem. | Name the operation, frequency, input scale and cost mechanism. Measure material impact before claiming a bottleneck or speedup. |
| Integrity and maintainability improvements | Evidence shows a weakness in state integrity, boundaries, validation, diagnosis, testing, or safe change. | Tie the recommendation to concrete examples. Give the smallest useful improvement, its acceptance check, and its likely scope. |

Assign one primary category per finding. Add secondary impacts without duplicating the same root
cause. Empty categories are valid. A preferred style or an unfamiliar implementation is not a
finding.

## System map and order

Use the following order by default. Start with the simulation runtime because every later system
relies on its Tick and mutation contracts. Each row is independently reviewable; split a row into
smaller passes if its evidence cannot be checked thoroughly in one sweep.

Paths below are relative to `src/` unless they explicitly name another root. The entry points locate
work; they are not claims that those files contain defects.

| Sweep | Boundary and review questions | Main entry points |
|---|---|---|
| 01 — Simulation runtime | Review Tick ordering, phase contracts, scheduling, read-only decisions and the save boundary. Does every engine run with the state and timing it expects? | `Borough.Core/Simulation.cs`, `TickPhase.cs`, `Entities/World.cs` (`Advance`). |
| 02 — State storage and deterministic arithmetic | Review table declarations, handles, slot reuse, hashing, derived rebuilds, random purpose tags and arithmetic. Do the analysers enforce the intended restrictions? | `Borough.Core/Tables/`, `Arithmetic/`, `Quantities/`, `Determinism/`; `World.HashState` and `RebuildDerived`; `Borough.Analysers/`. |
| 03 — Invariants and diagnostic evidence | Review what checks actually prove, their frequency and cost, bounded readings and evidence provenance. Can a plausible failure pass unnoticed or produce a misleading explanation? | `Borough.Core/Invariants/`, `Evidence/`, `Instruments/`, `Rules/Readouts.cs`. |
| 04 — Persistence and recovery | Review snapshot ownership, binary and content identity, corruption handling, interrupted saves, compatibility and crash reproduction. Does reload restore equivalent behaviour? | `Borough.Core/Persistence/`; `Borough.Formats/CitySave.cs`, `CrashArtifact.cs`, `HashTrace.cs`; `Simulation.TakeSaveIfDue`. |
| 05 — Commands and replay | Review command validation, refusal explanations, application order, rejected-command atomicity and log encoding. Do live actions and replay produce the same result? | `Borough.Core/Input/`; `Simulation.Apply`, `Refuses` and `Explain`; `Borough.Formats/InputLogCodec.cs`, `DriveScript.cs`. |
| 06 — Rulesets and content authoring | Review package capture, parsing, validation, lowering, defaults, hot reload and migration. Distinguish fixture intent from playable content and check generated references against the loader. | `Borough.Formats/RulesetLoader.cs`, `RulesetSource.cs`, `RulesetBundle.cs`, `RulesetImpact.cs`; `Borough.Core/Rules/Ruleset.cs`, `RulesetMigration.cs`; repository `rulesets/`. |
| 07 — Rule execution, Needs and Services | Review Bin Rule scheduling, wakeups, claims, expiry, Need satisfaction, care and Service capacity. Can a waiting or failed action recover, and does its explanation match its consequence? | `Borough.Core/Rules/RuleEngine.cs`, `EventWheel.cs`, `Choice.cs`, `ServiceEngine.cs`, `CivicEngine.cs`; Rule instances, expiry and care tables. |
| 08 — Citizens, Households and population | Review admission, individual choice, placement, Life Stages, schooling, departure and death. Check ownership and cleanup across every lifecycle transition. | `Borough.Core/Entities/World.cs`; `Rules/HinterlandEngine.cs`, `LifeStageEngine.cs`, `SchoolingEngine.cs`, `PlacementEngine.cs`; population and Unplaced tables. |
| 09 — Economy and public finance | Review Money transfers, Goods stocks, production, purchases, prices, employment, wages, taxation, subsidies and insolvency. Are counterparties, constraints and failure consequences coherent? | `Borough.Core/Rules/ShoppingEngine.cs`, `EmploymentEngine.cs`, `WageEngine.cs`, `BusinessTaxEngine.cs`, `PolicyEngine.cs`, `SubsidyEngine.cs`; `Space/DistrictMarkets.cs`; World balance, Bin and Business operations. |
| 10 — Land, development and built form | Review permissions, Lots, frontage, placement, capacity, construction, abandonment and demolition. Does spatial geometry agree with occupancy and access? | `Borough.Core/Rules/ZoneRuleEngine.cs`, `HousingConstruction.cs`; `Space/LocalLayout.cs`, `BuildingPlan.cs`, `LandPermissions.cs`; Building, Lot and Block tables and lifecycle operations. |
| 11 — Movement and transport | Review Trips, commute, walking, routes, parking, traffic costs and route invalidation. Check unreachable destinations, route-worker equivalence and individual location across transitions. | `Borough.Core/Movement/`, `Parking/`; `Space/RoadGraph.cs`, `RoadConnectivity.cs`, `RoutingPartition.cs`, `TrafficPresence.cs`. |
| 12 — Terrain, spatial fields and environment | Review generation, field updates, Districts, land value, pollution, woodland, water and disasters. Check units, spatial boundaries, refresh cadence and sources or sinks. | `Borough.Core/Space/MapLayers.cs`, `LayerSchedule.cs`, `LayerDiffusion.cs`, `TerrainGenerator.cs`, `WaterGenerator.cs`; `Space/DisasterEngine.cs`; World runoff and drainage operations. |
| 13 — Shell, controls and information | Review world ownership across threads, input, tools, menus, saving, settings, accessibility and inspection. Does the player see current, truthful information and understand what an action will do? | `Borough.Godot/Main.cs`, `SimulationThread.cs`, `Main.Threading.cs`, `Main.Channels.cs`, `Main.Verbs.cs`, `Main.Menu.cs`, `Main.Information.cs`, `Main.Readout.cs`, `Main.Panels.cs`. |
| 14 — Rendering and picking | Review retained geometry, chunks, visibility, detail changes, lighting, shadows, picking, invalidation, uploads and allocations. Does the picture remain correct during edits and camera movement, and what limits frame time? | `Borough.Godot/Main.Ground.cs`, `Main.Massing.cs`, `Main.FamilyBodies.cs`, `Main.ShellBand.cs`, `Main.Sky.cs`, `Main.Foliage.cs`, `InstanceLayer.cs`. |
| 15 — Appearance and asset pipeline | Review family selection, generated bodies, materials, variation, import conventions and asset provenance. Do visual forms match simulated facts and remain maintainable from Blender through Godot? | `Borough.Appearance/`; repository `appearance/`, `art/`, `scripts/art/`, `src/Borough.Godot/assets/`; `docs/07-the-drawing.md`. |
| 16 — Headless tools, tests and delivery | Review runner modes, synthetic worlds, test assertions and instruments, CI coverage, profiling, clean setup and packaging. Can developers reproduce failures and trust the checks, and can the current game be built and run reliably? | `Borough.Headless/`, `Borough.Core/Entities/SyntheticCity.cs`; repository `tests/`, `scripts/`, `.github/workflows/`, `Borough.slnx`, `global.json`. |

Rendering and appearance remain in scope even while related feature worktrees are active. Review
the pinned main revision and recheck affected conclusions after those branches merge; do not defer
the entire visual review or treat unmerged work as missing from its owner's implementation.

### Existing validation entry points

| Area | Reuse before creating a new harness |
|---|---|
| Tick, commands and determinism | Use `TickPhaseTests`, `SimulationTests`, `ReplayTests`, `FactorioTests`, `DerivedRebuildAuditTests` and the analyser tests. |
| Saves and content changes | Use `tests/Borough.Tests/Persistence/`, `Formats/`, and `Golden/`, including the golden re-record procedure when later implementing a deliberate change. |
| Game systems | Use the corresponding `Rules/`, `Entities/`, `Space/`, `Movement/` and `Parking/` tests; inspect fixtures before treating their outcomes as balance evidence. |
| Concurrency and visuals | Use `RouteWorkerTests`, `tests/Borough.Tests/Shell/`, `tests/Borough.Renderer.Tests/`, `scripts/test-renderer.sh` and `/drive`. |
| Performance | Reuse the headless profiling modes, benchmark tests, `scripts/profile-simulation.py`, shell measurement scripts and plans `0013`, `0066` and `0067`. |

### Cross-system pass

After the individual sweeps, trace complete scenarios through their boundaries. Assign a shared
root cause to one owning system and link the other affected systems.

- Found and grow a city through commands, permissions, construction, arrival, housing and jobs.
- Follow a purchase and a workday through stocks, Money, routes, attendance, wages and taxes.
- Trigger a shortage, failed Trip or Service failure; compare individual consequences with the
  displayed explanation and a supported player intervention.
- Edit or remove occupied spatial state; follow invalidation through routes, derived fields,
  selection, picking and rendered geometry. Include expected refusals.
- Save, reload and replay an aged city; compare behaviour and identity across these paths and
  supported Ruleset transitions.
- Move the camera and change simulation speed while interacting; check thread ownership, stale
  readings, frame cost and unchanged simulation results.

## Sweep method

### Operating budget for subsequent sweeps

The user approved this lighter process after the runtime review. It applies to review sweeps,
not to implementation work.

- Check open issues and existing board work before investigating candidates. Separate known
  defects, new defects and optional improvements from the start.
- Use at most two investigators and one verifier. Agents do not launch nested reviewers. The
  coordinator writes the synthesis; there is no separate synthesis agent or repeated final gate.
- Give the first pass a 90-minute budget. Return verified findings and explicit coverage gaps at
  that boundary. Extend only with a named consequential question and the user's agreement.
- Run one shared focused baseline. Reuse its results and repeat a check only when changed code,
  a changed reproduction, or a specific disputed claim warrants it.
- Verify reachability, consequences and evidence. Correct report wording and classification
  directly; those edits do not restart the investigation or justify another broad test run.
- Use one verification pass for material findings. Retain unresolved claims as open questions
  rather than creating successive reviewer rounds to force a verdict.
- On an infrastructure failure, make one recovery attempt and use the available fallback. Check
  saved completion status when an agent stops before telling the user that work is still running.

1. Record the revision, covered paths, relevant decisions, existing issues or board work, and
   explicit exclusions. Read current code before adopting historical findings.
2. Trace representative flows through input, state mutation, downstream use, and visible
   consequence where applicable. Include creation, steady operation, failure, removal and reload.
3. Review both design and implementation. Check invariants, ownership, numerical limits,
   lifecycle cleanup, invalidation, error handling and workload growth.
4. Challenge candidate findings. Look for callers, guards, intentional semantics, rebuild paths
   and tests that could disprove them. Reproduce bugs with the narrowest useful check.
5. Measure performance candidates with existing instruments before proposing new machinery.
   Keep unmeasured impact explicit. Use driven shell observations for visual or interaction claims.
6. Report the four categories, ordered by consequence. State what was verified, what remains
   uncertain, and what was not covered. Do not treat an unrun check as a pass.
7. Review findings with the user before selecting fixes or starting the next sweep.

## Evidence and validation

### Correctness and integrity

- Preserve replay, save/reload and thread-count equivalence. Check identity across recyclable
  slots, saved versus derived state, rebuild coverage and deterministic ordering.
- Check integer and Q16.16 arithmetic, overflow, rounding and unit boundaries. Verify that
  designer-facing tuning remains in the Ruleset.
- Trace Money and Goods through sources, transfers and sinks. Check bounded queues, histories,
  caches and per-entity collections across long runs and removal.
- Verify individual Citizen decisions and explainable consequences. Do not substitute aggregate
  behaviour for an individual contract.
- Test trust boundaries where files, commands and authored content enter the game. Include
  malformed input, partial failure and recovery where the current interface supports them.
- Review the tests themselves for false confidence, weak assertions and missing lifecycle paths.
  A passing suite does not settle design quality or untested behaviour.

### Performance

- Reuse `plans/0013-tick-budget.md`, existing headless modes, shell counters and test instruments.
  Historical results retain their original conditions and are leads for present measurements.
- Capture the revision, Release configuration, machine, world or Ruleset, seed, population,
  world age, thread count, command and instrumentation settings.
- Separate startup from steady state, and simulation from scene preparation, upload, drawing and
  UI work. For rendering, also record camera path, viewport, GPU and rendering settings.
- Exercise applicable small, representative city-scale and aged worlds. Include churn, reloads,
  edits and camera movement when those actions drive the workload. Label synthetic fixtures.
- Record repetitions and relevant variation. A busy-machine run can verify behaviour but cannot
  establish quiet-machine timing. State whether a budget is agreed or still needs a decision.
- State a code-proven cost separately from measured impact. A repeated full scan can be confirmed
  without claiming it dominates the frame. Unverified suspicions remain follow-up questions.

### Tools and boundaries

- Use focused `scripts/test.sh` checks during investigation. Read the runner's saved log rather
  than repeating a run to recover results.
- Use `/drive` when observing the Godot shell. Capture a trigger, response and consequence;
  screenshots alone do not establish an interaction or simulation claim.
- Run broader suites, invariants and relevant long-run checks when the reviewed scope warrants
  them. Do not re-record goldens to make a review pass.
- Keep investigation read-only by default. Any reproduction harness or retained regression test
  is an explicit, isolated change. Do not silently fix the system being evaluated.

## Per-sweep report

Keep the report short enough to support a decision. Link detailed reproductions and measurements
rather than copying logs into it.

| Field | Content |
|---|---|
| Scope | Record the system, revision, covered flows and exclusions. |
| Finding | State one defect or design cost in plain language and assign its primary category. |
| Consequence | Name who or what is affected, the triggering conditions and the scale of harm. |
| Evidence | Cite exact paths and lines or symbols, reproduction commands and retained results. |
| Confidence | Mark the finding confirmed or needing verification. Explain the remaining uncertainty. |
| Priority | Use critical for corruption or loss, high for broken core behaviour or severe cost, medium for bounded defects or material change friction, and low for limited improvements. Keep priority separate from confidence. |
| Recommendation | Give the smallest useful repair or design decision, trade-offs, and rough scope. |
| Acceptance | Name the check that would demonstrate resolution without breaking key equivalences. |
| Routing | Link an existing issue or board item, or identify the proposed destination. |

Keep unresolved hypotheses outside the confirmed finding lists. Include a brief account of reviewed
areas with no findings, coverage limits, and checks that could not run. Do not claim the system is
correct solely because a sweep found nothing.

## Routing and completion

- Check for existing work before creating a destination. Attach new evidence to the owning work
  rather than filing the same problem again.
- Route a single-PR defect or improvement to a GitHub issue with `needs-triage`. Ask before
  publishing issues or comments.
- Route work needing design, studies or several PRs to the backlog board and its owning plan.
  Propose durable documentation corrections in the document that owns the decision.
- Keep the four output categories in the sweep report. Issues and the board remain the work
  queues; do not create separate status, question or audit ledgers.
- Finish each sweep with evidence-backed findings, explicit limits and proposed next actions.
  Finishing a review does not require fixing everything it found.
- Finish the overall review with a short cross-system synthesis that removes duplicate root
  causes and ranks the few improvements with the greatest combined benefit.
