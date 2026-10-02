# Runtime and Tick review

## Scope and confidence

- **Revision:** `919290a52abfdd95841c61c0c21ad7883d62bebc` on `main`.
- **Scope:** `Simulation.Step`, phase metadata and ordering, Decide write guard, invariant cadence,
  `World.Advance`, save boundary, headless/Godot scheduling, Move scratch and Trip advancement,
  `RouteBatch` and `WalkScratch` parallel worker scratch, and the Godot manual-save and shell
  batching (both the default threaded loop and `--main-thread-sim`) paths.
- **Reviewed flows:** command admission and application through phase 7 and advance; observer
  callbacks; Ruleset batch identity across both shell batching loops and preparation's fast-forward;
  Shopping row growth; Traveller/Trip advancement; Core and Godot save delivery, including all four
  shipped `SaveFile`/`CitySave` writers and the readers that validate a foreign save's header.
- **Not reviewed:** command semantics beyond the demolition/service trigger traced below, Rule
  internals, persistence formats, rendering, and later gameplay sweeps. No whole-game coverage claim
  is made.
- **Evidence:** investigator reports and retained harnesses under
  `.orchestra/20260925-runtime-tick-sweep/evidence/{B01,B02,B04,B05}/`. **This evidence is local
  and Git-ignored** (`.git/info/exclude:18`; `git ls-files .orchestra` is empty). It will not
  accompany a fresh checkout or a transfer to another machine unless someone deliberately copies it
  out first. Every reproduction command below is stated in full so it can be rerun from source
  without that copy. B01's approved loop review is the attributed record embedded in
  `reports/B01.md`; two later loop reviewers (B04's, B05's) also returned their verdicts through
  hand-back rather than a written file, for the same reason, and are attributed in their owning
  reports.
- **Acceptance command lifetime:** `evidence/acceptance.sh` pins `HEAD` to this sweep's base
  revision and fails once `plans/runtime-tick-review.md` (or anything else) is committed on top of
  it. Combined with the Git-ignored evidence above, the acceptance command is machine-local and
  expires on commit; rerunning it after that point requires checking out the pinned revision
  again, and the retained local evidence, to reproduce these results.
- **Limits:** all performance evidence separates code-proven counts or allocations from elapsed
  time. The machines were shared. No quiet-machine bottleneck, speedup, or Godot frame-duration claim
  is made anywhere in this document. The confirmation harnesses (B01's `RuntimeTickEvidence`, B04's
  `QueuedCommandEvidence`) reproduce defects; they are not regression tests and should fail after a
  repair. B04's chain for `RT-C05` is a complete source trace, not a driven Godot session — no GUI
  crash was observed, and that distinction is repeated at every place the claim appears below.

## Findings by requested category

### Obvious bugs

This sweep confirmed and extended one already-filed bug (issue #25, `RT-C05`) and found one new
latent defect, fix-ready but not yet triggering today (`RT-C03`). No other bug was found.

#### RT-C05 — A command admitted against pre-batch state throws when an earlier command in the same batch invalidates it

- **Category** Bug. **Priority** high. **Confidence** confirmed and reproduced at the Core boundary;
  host reachability by a complete source trace, not a driven shell.
- **Routing** Existing GitHub issue **#25**, open and labelled `bug`, `ready-for-agent`
  (*"Two commands in one Tick can crash the shell"*). This is not a new discovery; #25 already
  names the same root cause and the same remedy. No issue was created.

**Root cause.** `Simulation.Refuses` answers about the world as it stands. The shell asks it once
per click and queues the command (`src/Borough.Godot/Main.Verbs.cs:368-381`), then applies the
whole queued batch in sequence (`ApplyInput`). Every command after the first in a batch is
therefore validated against a world that no longer exists by the time it applies. The two comments
that assert this is safe — `Main.Verbs.cs:362-365` and `Simulation.cs:717-721` — both reason about
single Steps; the property that breaks is a *batch* property, and both comments predate the
multi-command batch being the normal case.

**Player-reachable chain, verified link by link at the pinned baseline.**

1. One click produces one admission check and one queue append (`Main.Verbs.cs:368-381`); `_queued`
   is a plain `List<Command>` (`Main.cs:869`) with no de-duplication or conflict check.
2. Multi-event batches are the normal case on the default threaded path, not an edge case: clicks
   deferred while the step thread owns the world are replayed back-to-back against the same
   unstepped world before the single `Ordered()` call (`Main.cs:1373-1376,1416`;
   `Main.Threading.cs:45-59`). A zoning drag alone sends one command per block
   (`Main.Zoning.cs:97-105`).
3. `Clear` resolves a demolition click to the *nearest abandoned Building in the Cell*, not the
   clicked Tile (`Main.Verbs.cs:692-705`), so two clicks on different nearby Tiles can produce
   byte-identical commands.
4. Both are admitted — `RefuseDemolish` (`Simulation.cs:993-1000`) returns `None` while the
   abandoned Building still stands, which it does for both questions.
5. `Apply` throws `InvalidOperationException` on the second, non-`None` refusal
   (`ApplyDemolish`, `Simulation.cs:1795-1800`).
6. The default threaded Godot path catches at `Main.cs:1358-1363` (`TryComplete`) and calls
   `Stop(1)`; headless catches at `Session.cs:138` and exits 4.

**Three conditions, kept separate rather than folded into one claim:**

| # | Condition | Player-reachable? |
|---|---|---|
| 1 | Two commands both admitted against the pre-batch world | Yes |
| 2 | First applies, second throws; Tick left unadvanced, saved state moved | Yes — this is what the player hits |
| 3 | Re-entering `Step` reruns the Tick number | Partly undetermined — see below |
| 3b | That re-entry loses `_placementThisTick` accounting | Follows from 3; a **code trace**, not reproduced |

`--main-thread-sim` calls `StepAccounting` directly (`Main.cs:1426`), outside the one `try`/`catch`
in `_Process` (which wraps only `TryComplete` at `Main.cs:1358`). What Godot's .NET interop layer
does after an uncaught exception leaves `_Process` — terminate, or log and call `_Process` again
next frame — **cannot be established from this repository and was not observed.** If it continues,
condition 3 becomes reachable on a supported shell flag, and so does a third consequence: `Ordered()`
appends to the Input Log and clears the queue (`Main.Verbs.cs:67,70`) *before* `Step` throws, so a
continued `_Process` would record a second batch at the same, unadvanced Tick — issue #25's own
*"the log cannot replay either"* arriving by this path. This is stated as an open question in both
directions, not a claim.

**What the crash costs.** `Main.Stop(int)` writes no Input Log, no city save and no crash artifact
(`Main.cs:1721-1726,1729-1748`); the Input Log is written only inside `Quit()`
(`Main.Verbs.cs:97-135`, called from `Main.cs:1999`), which the crash path never reaches. Diagnosis
of the crashed session is lost, but no already-written save or log is corrupted. The shipped Godot
host also has no autosave — `SaveAtEndOfTick` has exactly one caller in `src/`, `Session.cs:97`, and
nothing in `src/Borough.Godot/` calls it — so the unsaved-work loss is bounded by the player's last
manual save, which can be the whole session, but not by a cadence.

**Priority: high, not critical.** The trigger is ordinary play on the default path and the session
ends, but no state or determinism damage survives the process on the two host paths whose outcome is
established (default threaded, headless); the failure is loud and immediate, not silent or
divergent; and the remedy is already specified in an open `ready-for-agent` issue. On
`--main-thread-sim` the process outcome is undetermined, so "survives the process" is not settled
there; if that path continues rather than terminating, the label would need revisiting.

**Smallest remedy, not implemented.** Issue #25 already names it: `ApplyInput` reports a refusal
instead of throwing. The command stays in the log and a replay reaches the same refusal at the same
Tick, preserving deterministic replay. The remaining open design question — not yet in the issue's
*Done when* — is how the refusal reaches the player. The alternative, validating against projected
queued state in the shell, was considered and rejected: it duplicates the applier's predicate in the
front end, the exact drift `Refuses`' own comment (`Simulation.cs:704-710`) exists to prevent.

**Supersedes** this document's earlier classification, which called the equivalent throw "public-API
hardening… not reachable through either shipped host" and filed it against issue #28. Both were
wrong. #28 covers a related but distinct mid-Tick Ruleset failure.

#### RT-C03 — Both shell batching loops, and the shell's own first Tick, drop the Ruleset hash (latent defect)

- **Category** Latent implementation defect, fix-ready. **Priority** low today; blocking for hot
  reload in the shell. **Confidence** confirmed.
- **Supersedes** this document's earlier version, which covered only `SimulationThread` and framed
  the per-Tick Ruleset contract as an open user decision. Both were wrong: the contract is settled in
  source (`Simulation.cs:492-503` — *"a Tick has exactly one Ruleset"*, *"`TickInput.RulesetHash` is
  populated every Tick"*), and the defect is larger than either loop alone.

**Both batching loops are the same line twice**, carrying the supplied hash on Tick 0 of a batch and
`default` (hash 0) on every later Tick:

| Path | Site |
|---|---|
| Threaded (default) | `src/Borough.Godot/SimulationThread.cs:76` |
| `--main-thread-sim` (supported, reachable — `Main.Threading.cs:61-72`) | `src/Borough.Godot/Main.cs:1426` |

**The shell never supplies the real hash at all, even on the first Tick.** `CityPreparation.cs:32`
boots with `new TickInput([boot], 0)`; `Ordered()` (`Main.Verbs.cs:72`) hardcodes `0` for every
player Tick; `CityPreparation.StepTo` (`:82-89`), the normal startup fast-forward, and the
loaded-city constructor (`:51`) share the same omission. `Reload`'s first-Tick branch
(`Simulation.cs:515-520`) therefore sets `_inForce = 0` and it stays 0 for the whole session, while
the session's own Input Log is built with the real content hash (`Main.Preparation.cs:177`) — the
log and the runtime's own idea of which Ruleset is in force disagree.

**No wrong simulation output today.** The Ruleset reaches the world through a different door
(`new World(citizens, rules, key)`, `CityPreparation.cs:27`); `_inForce` stays 0, every Tick passes
0, and `Simulation.cs:522-525`'s equality return fires every time. The cost today is a false
`RulesetInForce` readout and a log whose recorded identity the runtime never held.

**The failure mode is a throw, not a silent drop.** The moment the shell is given a catalogue and a
non-zero hash, a batch's later Ticks pass 0, `0 != _inForce`, `TryResolve(0)` fails, and
`Simulation.cs:528-534` throws — reaching the host the same way `RT-C05`'s throw does. Whoever wires
hot reload into the shell hits this on the first multi-Tick batch.

**The remedy needs four sites, not two, and the ordering is a trap.** Feed the real content hash in
at the first Tick and carry it on every subsequent Tick, keeping commands on the first Tick only:

1. `CityPreparation.cs:32`, the boot Tick — `CityPreparation` has no `_capture` member today, so the
   hash must be threaded through its constructor.
2. `CityPreparation.StepTo` (`:82-89`) and the loaded-city constructor (`:51`).
3. `Ordered()` (`Main.Verbs.cs:72`), every player Tick.
4. Both batch loops (`SimulationThread.cs:76`, `Main.cs:1426`).

Fixing any earlier site without the later ones turns a latent defect into an immediate crash: once
`_inForce` goes non-zero while some path still passes 0, that Tick hits `TryResolve(0)` against an
empty catalogue and throws — fixing only the boot input would throw at preparation Tick 2, before
either runtime batching loop ever runs.

**Acceptance:** a multi-Tick batch under a stable non-zero Ruleset hash, asserting no throw,
`RulesetInForce` equal to that hash from the first Tick onward, and **`Reloads == 0`** — not one
reload, because `Reload`'s opening branch adopts the hash and returns without touching the counter
(`Simulation.cs:515-519`); asserting one reload would fail a correct fix. Coverage must span
preparation's fast-forward as well as both runtime loops.

**The four shipped save writers and readers were checked for a related hazard.** All four writers
that use `SaveFile.Write`/`CitySave.Write` in `src/` write a correct hash today (Godot menu save,
headless `SaveCity`, headless `--save`, headless `--profile-save`); none currently writes `_inForce`
except headless `--save`, and Godot never arms `SaveAtEndOfTick`, so no Godot save is corrupted by
this defect today. Of the four raw `SaveFile.Read` sites, three validate the header's Ruleset hash
against an expectation (`Session.cs:311`, `Session.cs:348-354`, `ProfileDump.cs:52-53`); one,
`Session.cs:193`'s `RoundTrip`, does not, but it is a write-then-read self-check within one
invocation and not a door a foreign save enters through.

**This finding is not a prerequisite for repairing issue #25**, and an earlier version of this
document implied a firmer ordering than the source supports; that ordering claim was raised in gate
review and withdrawn. The fact that settles it: `SaveAtEndOfTick` has exactly one caller in `src/`
(`Session.cs:97`), so no shell save currently reads `_inForce`, and #25's actual subject — graceful
refusal of a conflicting queued command — needs no save-hash change at all. **The cross-link is
conditional on scope, not unconditional:** if a separate recovery design later adds end-of-Tick
autosave to the shell and builds it on `SaveAtEndOfTick`, that autosave would write `_inForce` (0),
and `RT-C03` becomes a prerequisite for it, packaged through `CitySave` or read by the readers above.
Absent that design decision, the two findings are independent.

- **Routing** New GitHub issue, `needs-triage`, single PR, covering all four sites and both loops. No
  issue was created.

---

### Bad design decisions

#### RT-P03 — Manual Godot saves do expensive work synchronously

- **Trigger and consequence:** `Main.Menu.cs:174-185` calls `CitySave.Write(path, _world, ...)`; the
  chain hashes, serializes, compresses, flushes to disk, and renames before the callback returns. The
  ownership guard (`RequireWorld`) prevents tearing, so this is a stall and ownership-design cost, not
  a data-race claim.
- **Priority** Medium. **Confidence** confirmed caller chain and a synchronous snapshot-path probe. A
  10,000-Citizen probe delivered 20,646,930 bytes on the Step caller's thread. Compression, disk
  latency, and frame duration were not measured.
- **Smallest remedy and trade-off:** capture one immutable `WorldSnapshot` at a guarded boundary and
  move hash, ZIP, flush, and rename to one save worker. This adds retained snapshot memory, worker
  lifecycle, error delivery, and a policy for concurrent saves.
- **Acceptance:** a driven large-city save keeps presenting frames after capture; reload matches
  captured Tick and hash; failure and interruption preserve the prior atomic save; shutdown handles
  the worker.
- **Routing:** existing `Snapshot, thread and table ownership` board row; share implementation with
  persistence and shell sweeps.

---

### Clear performance issues and poor performance implementation

**No Tick elapsed time was measured anywhere in this sweep.** The evidence in this category is
source reading plus allocation and scan counts, gathered on `rulesets/stress-shopping.toml` at
10,000 and 100,000 Citizens, single-threaded (`routeWorkers:1` in the harness output), on a shared
machine. No timing, frame-duration or speedup claim is made. The one retained performance finding
is a bounded startup allocation inefficiency with an existing in-codebase remedy; the category is
thin because that is the result, not because work was skipped.

#### RT-P01 — Shopping scratch arrays replace exact-sized buffers on every growth

- **Trigger and consequence:** `ShoppingEngine.Step` (`src/Borough.Core/Rules/ShoppingEngine.cs:49-82`)
  allocates `_active` and `_order` at the current row count before creating the next Household
  stripe. Every high-water increase allocates and discards both arrays.
- **Priority** Low. **Confidence** confirmed by code and by two assertion-bearing probe runs at
  10,000 and 100,000 Citizens.
- **Measured, both populations:** the growth interval is Ticks 1–64 inclusive and nothing after —
  the Shopping fixture's 64-Tick interval filling every Household stripe once. Payload over the whole
  2,048-Tick run was 1,408,608 B at 10,000 Citizens and 14,046,144 B at 100,000. In both cases the
  remaining 1,983 Ticks make zero scratch allocation. **Everything measured is a startup transient,
  not a steady-state cost** — no frame impact and no elapsed time has been measured at either
  population. At 100,000 Citizens, 83.2% of the payload (11,692,864 of 14,046,144 bytes) crosses
  .NET's 85,000-byte large-object threshold; at 10,000 Citizens none of it does.
- **Unmeasured:** an incrementally growing city, where every Tick that raises the Shopping high-water
  count would trigger a fresh full-size replacement on the following Tick, is the one workload where
  this pattern could recur indefinitely. No growing-city allocation trace was captured; no claim is
  made about it beyond the structural bound that doubling caps replacements at
  `ceil(log2(final rows / 64)) + 1` regardless of arrival pattern, while exact sizing bounds them only
  by Tick count.
- **Smallest remedy and trade-off:** reuse the doubling-from-64 policy already implemented at
  `ServiceEngine.GrowOccasions` (`src/Borough.Core/Rules/ServiceEngine.cs:1002-1023`) — three lines,
  one site. Over the measured row-count series this needs 7 replacements instead of 64, and
  9.0–14.4× less payload. This retains unused scratch capacity (at most twice the high-water row
  count) but changes no draw, order, or hash, because the sort already runs on the populated prefix
  only (`ShoppingEngine.cs:65`) and the scratch is unsaved.
- **Acceptance:** an incremental-growth probe shows logarithmically bounded replacements, no
  steady-state Tick allocation, unchanged readings, hashes, replay, and save/reload equivalence.
- **Not recommended:** sweeping the other seven exact-sizing scratch sites in the codebase
  (`HinterlandEngine.cs:151`, `CivicEngine.cs:454`, `RouteCache.cs:410,464`, `PlacementEngine.cs:1111`,
  `WalkScratch.cs:138`, `TravelTimeMatrix.cs:142`); exact sizing is the prevailing idiom here and only
  the Shopping site has counted evidence.
- **Routing:** `City-scale simulation performance` board row; proposed issue, `needs-triage`, not
  created.

---

### Integrity and maintainability improvements

Test runtime contracts at their execution boundaries, rather than only restating metadata or
calling helpers directly. Keep host input identity and command rejection aligned with Core across
each entry path. Update source comments and design descriptions together when timing or ownership
changes, so future work does not inherit obsolete assumptions.

#### RT-C09 — Durable documents and central class comments describe superseded runtime behaviour

- **Priority** Low, except entry **(c)**, which sits in the class documentation of the project's
  central type and is flatly wrong rather than partly stale. **Confidence** confirmed by direct
  quotation against live behaviour.

`Simulation.Commit` (`Simulation.cs:2330-2338`) sets `_phase` and calls
`_world.Invariants.RunStaggered(_world)`. That is all of it; the `tick` parameter is not read.

| # | Location | Claim | Verdict |
|---|---|---|---|
| (a) | `docs/05-technical-architecture.md:400` | Saves occur at phase-7 end | **Stale.** `adr/0087` and the code save after `World.Advance`, not at phase-7 end. |
| (b) | `plans/0013-tick-budget.md:103` | Decide verification is the default | **Stale.** Core defaults `VerifyDecideWritesNothing` off; headless enables it, not Core. |
| (c) | `src/Borough.Core/Simulation.cs:28-30` | The Tick is kept out of the World and out of the State Hash | **Flatly wrong.** The Tick is `Clock.Tick[0]` on the World (`World.cs:831`), a `Rows.Saved` field registered in `World._tables` (`:344`), folded by `World.HashState` (`:1731-1763`). This sits in the class documentation of the project's central type. |
| (d) | `src/Borough.Core/TickPhase.cs:43` | Commit schedules events, re-evaluates Stress, emits the hash | **Wrong.** None of the three happens; there is no "swap buffers" clause at this site either. |
| (e) | `src/Borough.Core/Simulation.cs:2322` | Same sentence | **Partly wrong.** `:2323-2329` already disclaims the swap and redirects hash emission to the caller. Only "schedule next events" and "re-evaluate Stress" stand uncorrected. |
| (f) | `docs/02-simulation-model.md:35` | Commit swaps Past and Future, re-evaluates Stress, applies promotions | **Partly wrong.** `:41` already corrects the Past/Future mechanism in a block quote six lines below; the swap row itself is still stale. |
| (g) | Three test comments | The Decide guard defaults on | **Wrong** — `Simulation.cs:410,416` documents the guard OFF by default since 2026-08-30. Exactly three: `tests/Borough.Tests/Space/SealingCostTests.cs:25`, `tests/Borough.Tests/Space/TerrainFoldCostTests.cs:27`, `tests/Borough.Tests/Parking/ParkingArrivalStreamTests.cs:412-413`. Two other candidates (`Headless/ArrivalDumpTests.cs:28`, true of a headless session; `Rules/RuleEvaluationTests.cs:66-71`, the post-mortem of this drift) are correctly excluded. |

**(a) and (b) are this document's original two locations; (c)-(g) are B04's verified extension of
the same finding, not a replacement of them.**

**Segment Stress does not exist anywhere in `Borough.Core`.** Every occurrence is doc prose; three
separate documents describe a phase re-evaluating a mechanism the project has never built.

- **Acceptance** A reading of each location against live behaviour: `Commit`'s body
  (`Simulation.cs:2330-2338`), the clock's fold through `World._tables` (`World.cs:344`),
  `Simulation.cs:416`'s uninitialised auto-property, and `adr/0087` against the save boundary.
- **Routing** Proposed `needs-triage` issue, single PR, covering all seven one-sentence
  documentation/comment corrections in the files that own them ((a), (b), (c) and (g) first). This
  sweep made no changes; filing is awaiting the user's approval, not created here.

#### RT-C01 — Phase concurrency guard is tautological

`Phases.Runs` is derived directly from `Phases.Permits` (`src/Borough.Core/TickPhase.cs:119-122`), so
`TickPhaseTests` can never catch a mismatch. The harness reproduces the projection for all phases.
Make the metadata independently authored, or replace it with execution-observing instrumentation;
the latter must first define whether nested route-worker fan-out counts. Acceptance is a deliberate
metadata mismatch, or serial-phase multi-thread execution, turning the relevant check red. **Board
decision required** — this needs a user choice between the two repair shapes before any test change.

#### RT-C02 — `PhaseCompleted` mutation contract is unenforced

The public observer at `Simulation.cs:73-74,436-452` can mutate live state after the Decide guard. A
derived-column write that changed state left an immediate write-only `HashState` comparison false.
The existing contract already forbids mutation. Document that this relies on the observer honoring
that requirement; the hook is not an enforced read-only boundary. A documentation change does not
prevent mutation. Enforcement or a read-only view would require a separate design decision and a
behavioural test. Distinct from `RT-C04` because this concerns the hook, not Decide's guard.

#### RT-C04 — Decide guard throw has no test that reaches it

Removing `Simulation.cs:2088-2093` remains green because existing tests exercise only the passing
path or reimplement the fold. Add an internal test-only probe between the folds, write a derived
column, and assert `Step` throws `InvalidOperationException` naming `adr/0037`. Acceptance is that
deleting the throw turns the test red.

#### RT-C06 — Step does not have a test proving staggered invariants run

`Simulation.Commit` calls `RunStaggered` (`Simulation.cs:2337`), but current tests invoke the
registry directly; no test does `Assert.Throws<InvariantViolationException>(() => simulation.Step(...))`.
Add a hand-built always-failing staggered check and assert `Step` throws at the Commit Tick.
Acceptance is that removing the call turns the test red.

#### RT-C07 — Crash artifacts cannot identify the phase

`Simulation.Phase` is described as crash-artifact context (`Simulation.cs:376-377`), but
`CrashArtifact.Of` and `Session.Panic` do not carry or read it (`Borough.Formats/CrashArtifact.cs:136`,
`Borough.Headless/Session.cs:417-447`). Add the phase and assert a phase-6 panic records `Growth`, or
remove the stale claim. Acceptance is the phase-bearing artifact test.

#### RT-C08 — Two Growth ordering comments are false

The block above `Simulation.Growth` (`src/Borough.Core/Simulation.cs:2201-2229`) says policy is ahead
of everything and disasters are first, while `SpoilExpired` and five other passes precede those
calls. Split notes above their calls and state the actual relative order. Acceptance is a source
reading showing no positional claim contradicted by the eleven calls. This is a comment-contract
correction, not a behaviour claim.

- **Routing (RT-C01, RT-C02, RT-C04, RT-C06, RT-C07)** Proposed single GitHub issue, `needs-triage`,
  bundling the five test/contract gaps (RT-C01 also needs a prior board decision on repair shape).
  No issue was created.

#### RouteBatch coverage — reviewed, no defect found

`RouteBatch.cs` (113 lines) and `WalkScratch.cs:138-200` were reviewed for the parallel Move-worker
scratch that `RT-P01`/`RT-P02` sit beside: scratch ownership is disjoint by construction (each
`Parallel.For` index owns one `WalkScratch`), route partitioning is disjoint and total, no shared
state accumulates inside the parallel region, and `WalkScratch`'s generation-stamp rollover makes a
search's result independent of prior searches on that scratch — thread count cannot change a route.
Demonstrated by `scripts/test.sh --filter '...RouteWorkerTests.'` (`Failed: 0, Passed: 7`), including
one test asserting real parallelism with order-independent results and one switching
`RouteWorkerCount` between 8, 2 and 1 mid-run while asserting identical `HashState()` at every
switch. **Coverage limits:** `WorkerStarting`/`WorkerCompleted` are `internal`, so no isolated
evidence project observes worker occupancy; the generation rollover (after 2³¹ searches on one
scratch) is a code argument, not a test; existing instrumentation attaches to `TripEngine._walk`
rather than the worker scratches. No destination — no defect.

## Refuted leads, dropped candidates and existing work

B01 refuted Tick-0 reload swallowing, host continuation after throws, a Godot tuner `World.Adopt`,
save hash `0`, incorrect post-Advance save timing, unreachable layout guards, repeated invariant
slices after reload, and manual-save tearing. B02 rejected staggered-invariant optimisation, Decide-
guard optimisation, phase-concurrency duplication, and unmeasured inner-loop suspicions. B04 refuted
the earlier claim that this document's phase-0 throw was not player-reachable (withdrawn — see
`RT-C05` above), the claim that `RT-C03` was a silent drop rather than a throw, and the claim that
`RT-C03`'s per-Tick contract was an open user decision. None of these are findings. Existing issue
#28 and the performance and snapshot board rows predate this sweep; retained findings are not
presented as newly filed work.

#### RT-P02 — no longer a finding; retained as an unpriced measurement

`TripEngine.Advance` scans `[0, SlotCount)` for Travellers and Trips every Tick
(`src/Borough.Core/Movement/TripEngine.cs:802-956`); `Rows.FreeSlot` leaves freed slots below the
high-water mark (`src/Borough.Core/Tables/Rows.cs:88-97,456-478`), so the scan revisits dead slots
indefinitely. Measured, per loop, per Tick, over 2,048 Ticks: 194.534 dead checks at 10,000 Citizens
and 986.981 at 100,000 (389.068 and 1,973.963 across both loops combined). The high-water mark equals
peak-ever concurrent Travellers/Trips *exactly*, with no slack above it, so the scan can never exceed
the busiest Tick the city has had; a per-Tick series over the same run shows 607 of 1,966 non-empty
Ticks scanning the full high-water range with zero live rows at scan time, and 86.8% of the scan dead
over the 796 Ticks following the run's peak.

**Why this is a measurement, not a performance finding.** The dead-slot probe is how this
representation currently discovers liveness; removing it means replacing the representation, and no
replacement was shown to cost less overall. The one candidate considered, a derived ascending
live-slot index or occupancy bitmap, is withdrawn: it addresses only 62.6–69.3% of `ReleaseEnded`'s
do-nothing visits (the rest are live in-flight Trips it must still walk), its maintenance is not
small (it would need `Rows.Derived` status, genuine rebuild for `DerivedRebuildAuditTests`, and
preserved slot/free order because changing row reuse changes the State Hash), and no elapsed-time
attribution in the codebase can separate the dead check from the live cursor advance to price it. A
finding whose only proposed repair is "no code change" is a measurement, not a defect.

- **Routing** Record the counts (both populations, both denominators, the fixture, and the statement
  that no elapsed time was measured) as an unpriced consumer in `plans/0013-tick-budget.md`, which
  already uses that convention for exactly this situation. `City-scale simulation performance` stays
  the owning board row if the question is ever reopened with a measurement. No code change, no issue.

## Validation and reproductions

The focused lane was run by every investigator with 117 passed, 0 failed, 0 skipped. The acceptance
script runs it once, then sequentially: B01's `RuntimeTickEvidence`, B02's `RuntimeProbe`, B04's
`QueuedCommandEvidence` and `RouteWorkerTests`, and B05's `ScanProfile` at 10,000 and 100,000
Citizens. See `.orchestra/20260925-runtime-tick-sweep/evidence/acceptance.sh` and its saved output
for exact commands; all logs are local-only. **The B01, B02 and B04 harnesses intentionally reproduce
confirmed defects and therefore count as evidence success, not regression proof** — they are expected
to fail once the defect they demonstrate is repaired. `ScanProfile`'s exit-0 result is a measurement
assertion (its per-Tick derived counts must be non-negative and, at 10,000 Citizens, must reproduce
B02's retained totals exactly); it does not assert or disprove a defect.

## Decisions and next actions

User decisions remain open on: `RT-C01`'s metadata-authoring versus execution-observing repair shape;
`RT-C05`'s rejection-reporting design (how a refused batch command should reach the player, which
issue #25's own *Done when* does not yet cover) and whether a stronger read-only `PhaseCompleted`
observer API is worth its scope; and whether a future #25 remedy adds end-of-Tick autosave recovery to
the Godot shell, which would make `RT-C03` a prerequisite rather than an independent fix. `RT-C03`
itself needs no user decision — its contract and remedy are settled in source. No production fix,
issue, Golden, Ruleset, or existing-test change was made by this sweep.
