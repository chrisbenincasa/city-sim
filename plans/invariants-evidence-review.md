# Invariants and evidence review

Sweep 03 of the [system review](system-review-brief.md).

## Scope

- **Revision:** `b673754915417832a0231839de26141abe10e9fd` on `main`.
- **Covered:**
  - `src/Borough.Core/Invariants/`, all 35 registered checks and the per-Tick `Require` write sites
    in `World.cs`, `TripEngine.cs` and `EventWheel.cs`.
  - `Evidence/`, `Instruments/` and `Rules/Readouts.cs`.
  - The shell text built from them in `Main.Information.cs` and `Main.Diagnosis.cs`.
  - `tests/Borough.Tests/Invariants/`, `Evidence/` and `Instruments/`, plus `InvariantCoverageTests`.
- **Flows traced:**
  - Invariant scheduling in headless and shell runs.
  - Street lay and removal under moving traffic, then save and load.
  - A Rule shortage, a failed commute and a refused command, each from cause to the shell's text.
  - Evidence columns across slot reuse.
- **Decisions consulted:** `adr/0031`, `0033`, `0041`, `0063`, `0084`; `docs/02-simulation-model.md`
  §10.
- **Existing work:** #132 shares the root of IE-B2. Sweep 01's RT-C06 (no test shows `Step` runs the
  staggered tier) and RT-C07 (crash artifacts lack the phase) are cited, not refiled. #196, #210 and
  #226 sit next to IE-I5 and the readout path but are different defects.
- **Open work touching swept paths:**
  - #212 (`utilities-5`) edits `Evidence.cs`, `BuildingEvidence.cs`, `Census.cs`, `Metric.cs`,
    `TreasuryFlows.cs` and `TreasuryFlowsExplainTheBalanceTests.cs`. It adds a `Tariff` income term
    to that test, so IE-I5's count becomes 10 of 14 after merge. Recheck IE-B3 line numbers.
  - #223 (`needs-panel`) adds `Instruments/NeedsReading.cs`, which this sweep did not review.
  - #230 (`day-number`) edits `Main.Panels.cs`.
  - Worktrees `catchment-fold` (`city-sim-terrain`) and `utilities-2` edit `Invariant.cs`,
    `WorldInvariants.cs`, `InvariantTierTests.cs` or `Evidence.cs`. Their owners should recheck
    IE-I1 and IE-I2.
- **Not covered:** see [Coverage limits](#coverage-limits).

## Findings

### Obvious bugs

#### IE-B1 — Splitting a Segment under a car leaves permanent traffic volume, and no invariant notices

- **Category:** bug. **Priority:** high. **Confidence:** confirmed by reproduction, twice.
- **Finding:** `SplitRouteHops` re-points a moving vehicle's current hop to the new half of a split
  Segment. It does not move the volume `TripEngine.Enter` added to the old Segment. `Leave` then
  decrements the new half. The old Segment keeps +1 and the new half reads −1 with no vehicle on
  either. `TrafficIsConserved` compares two city-wide sums, so the +1 and −1 cancel and it stays green.
- **Consequence:**
  - An ordinary Street lay across a road carrying traffic triggers it. Every backward through-hop is
    affected, and so are first and last hops whose Address moved to the new half. Only forward
    through-hops are safe.
  - The +1 Segment shows "1 Vehicle present" in the road inspector for ever. It also counts as moving
    traffic for line-source fields.
  - The −1 half prices its next real vehicle as free, because `LoadOf` treats volume ≤ 0 as no load.
  - Bulldozing either half later removes one side of the pair. From then on every end-of-run check
    throws `SegmentVolumeIsConserved`, so every later save of that city fails to load in the shell.
- **Evidence:**
  - `src/Borough.Core/Entities/World.Streets.cs:202,207,211` (re-point).
  - `src/Borough.Core/Movement/TripEngine.cs:752-756` (Enter) and `:782-786` (Leave).
  - `src/Borough.Core/Invariants/WorldInvariants.cs:138-177` (sum comparison).
  - `RoadSegmentTable.cs:87-88`: the volume columns are saved, so nothing rebuilds them.
  - `RoadSegmentTable.cs:331` (`LoadOf`); `Main.RoadInformation.cs:195`;
    `LineSourceQueries.cs:236,314`; `TrafficPresence.cs:47`.
  - Probe P2 (`declining.toml`, every Household owns a car, 4,000 Citizens): Segment 104 holds
    backward +1 and new Segment 267 holds −1 with 0 vehicular travellers 1,024 Ticks after the lay.
    0 violations were collected.
  - Probe V1 repeats the lay, then bulldozes Segment 267 at Tick 1117. 16 of 16 end-of-run checks
    over the next 4,096 Ticks threw.
- **Recommendation:**
  - Move the in-flight increment from the old Segment to the new one when a hop is re-pointed.
  - Strengthen `TrafficIsConserved` to tally each Traveller's current hop by Segment and direction.
    It should require each count to equal its tally and be ≥ 0. The cost stays
    O(Segments + Travellers).
  - Fixing this changes the State Hash on fixtures that lay Streets under traffic. One PR.
- **Acceptance:** P2 and V1 invert (no drift, no violation). The stronger check fires on a planted
  +1/−1 pair.
- **Routing:** proposed new issue, `needs-triage`.

#### IE-B2 — A save taken just after bulldozing a Street under a car refuses to load

- **Category:** bug. **Priority:** medium. **Confidence:** core path confirmed by reproduction;
  shell path confirmed by code trace.
- **Finding:** `World.RemoveStreet` frees a Segment and its volume while a car still holds a
  vehicular hop on it. `TripEngine.Enter` and `Leave` skip a dead Segment, so the car still counts as
  driving and no Segment counts it. `CitySave.Write` saves without a check. Loading runs
  `CheckEndOfRun`, which throws `SegmentVolumeIsConserved`. The menu then shows "Could not load
  city: invariant SegmentVolumeIsConserved violated…".
- **Consequence:** the window is narrow. Commands apply before Move in the same Step, so only cars
  whose priced arrival falls after the demolition Tick stay on the dead hop. That is about a Tick or
  two, longer on long or congested Segments. A manual save in that window cannot be loaded. The world
  recovers once the car moves on. The Godot shell has no autosave.
- **Evidence:**
  - `src/Borough.Core/Simulation.cs:1764` (demolish) and `:523,531` (input before Move).
  - `TripEngine.cs:740,777` (dead-Segment skips) and `:831-851` (zero-cost crossing of dead hops,
    the subject of #132).
  - `src/Borough.Godot/Main.Menu.cs:212` (save) and `:224,233-241` (load check and message);
    `src/Borough.Godot/CityPreparation.cs:53`.
  - Probe P1: Segment 202 carries one vehicle at Tick 92. The load check throws "invariant
    SegmentVolumeIsConserved violated at Tick 92, row 0, row 1". The world is clean again by
    Tick 2140. P1 saves without stepping, so it overstates the window.
- **Recommendation:** the invariant is right; the removal is wrong. Settle #132's open question
  (end the Trip with a Fate, or re-plan it). The removal must also take the volume off cleanly, which
  IE-B1's repair needs too. IE-D1 covers the separate question of whether a load should refuse.
- **Acceptance:** P1 loads cleanly after a demolition under traffic. A planted real leak still fires.
- **Routing:** attach this evidence to #132.

#### IE-B3 — A Rule waiting for Labour reads as a supply shortfall "since Tick 0"

- **Category:** bug. **Priority:** medium. **Confidence:** confirmed by reproduction.
- **Finding:** `RuleEngine.Stop` treats a Labour block as unstaffed rather than starved, so it
  clears `StarvedSince`. That covers closed shifts and understaffing during a shift. `Blocked` stays
  `Supply`. Evidence then counts the wait as a shortfall:
  - `SupplyOfBuilding` counts it as a shortfall.
  - `OfCity` ranks it as trouble, because only `Blocking.Space` is a routine wait.
  - The inspector prints "Current shortfall since Tick 0 · 0 missed firings" and never checks the
    sentinel.
- **Consequence:** every staffed trade outside its shift hours shows a false shortage. It distorts
  the City Evidence ranking that tells the player where supply is failing.
- **Evidence:**
  - `src/Borough.Core/Rules/RuleEngine.cs:2158-2178`.
  - `src/Borough.Core/Evidence/Evidence.cs:56-62,59,382-387,895`.
  - `src/Borough.Godot/Main.Information.cs:1189,1222-1223`.
  - Probe over 20,000 Ticks of the base founding city, sampled every 256 Ticks: all 7,415
    Labour-blocked samples had `StarvedSince == 0`. 7,343 of 29,158 City Evidence supply-trouble
    subjects (25.2%) were waiting for Labour. Building 91 at Tick 8704 reads "Current shortfall since
    Tick 0 · 0 missed firings". Most of those rows sit beside a real supply shortfall on the same
    Building. Five samples were blocked on Labour alone.
- **Recommendation:** classify a Labour wait in evidence the way the engine does, as routine or as
  a distinct unstaffed state. Omit the "since" line when `StarvedSince` is the sentinel. No hash
  change. One small PR.
- **Acceptance:** the probe reports 0 Labour subjects in trouble groups and no "since Tick 0" lines.
- **Routing:** proposed new issue, `needs-triage`.

#### IE-B4 — The failing-commute cause comes from the last Trip of any purpose

- **Category:** bug. **Priority:** medium. **Confidence:** confirmed by reproduction, with the
  failure start injected.
- **Finding:** the shell's commute reading maps `LastTrip?.Fate` to a cause. A worker whose commute
  failed then shops or visits a Service, and that Trip overwrites the fate. The shell then shows a
  generic "the commute failed" in place of the cause that is running the dismissal clock.
- **Consequence:** a worker at risk of dismissal shows the wrong reason, so the player cannot tell
  a budget problem from a missing road. A non-commute Trip that ends `NoRouteFound` would show "no
  route to work" for a budget failure. That case follows from the code but was not observed.
- **Evidence:**
  - `src/Borough.Godot/Main.Diagnosis.cs:99-125` (`CommuteReading`).
  - `World.cs:8861-8919` (`RecordTripFate`) and `:8930-8973` (commute timing).
  - `ShoppingEngine.cs:439` and `CivicEngine.cs:432` (overwrites).
  - `FailedCommuteClockTests.A_completed_trip_for_another_purpose_leaves_the_clock_running`
    (`:387-431`) reaches this state with a real cut bridge. A player reaches it by bulldozing a
    Street link.
  - Probe: the outbound-commute refusal is injected through `RecordTripFate` at Tick 8,000 for 184
    workers. Afterwards 84 of them had samples with the wrong cause (6,030 "Completed", 6 null). The
    injection writes the same Citizen columns a real refusal writes.
- **Recommendation:** store the failing run's own Fate beside `CommuteFailedSince` and show that.
  Saved state, so the hash changes. One PR, best done with IE-I3.
- **Acceptance:** the probe's mismatch count is 0.
- **Routing:** proposed new issue, `needs-triage`, together with IE-I3.

### Bad design decisions

#### IE-D1 — The shell refuses a whole city on any end-of-run violation and never checks before saving

- **Category:** design. **Priority:** medium. **Confidence:** confirmed by code trace.
- **Finding:** the shell runs the end-of-run tier only when it loads a save. Any violation refuses
  the load, and there is no collect mode or override. The tier never runs during play or before a
  save. So a transient state (IE-B2) or a defect in the check itself becomes a save the player cannot
  open, discovered only later.
- **Rationale:** throwing at the first violation suits headless runs, where the run can be replayed
  to the Tick before (`InvariantRegistry.Collect` remarks). `Collect` was meant for a long balance
  run, but no shipped host sets it.
- **Cost:** the player loses access to a city over a check that may be transient or wrong, and gets
  no evidence about when the state went bad.
- **Alternatives:**
  - Check before writing a save, and refuse the save with the violation. This costs about 17–22 ms
    per save at 4,000 Citizens and catches the problem while the player still has the live city.
  - Load in collect mode and warn instead of refusing. The city opens, but the player continues
    from a state known to be wrong.
  - Both. Check before saving and warn on load.
- **Decision needed:** whether a failed check refuses a load. The repair is small once that is
  decided.
- **Acceptance:** a save taken in IE-B2's window is either refused at write time with the violation
  named, or loads with a visible warning.
- **Routing:** proposed new issue, `needs-triage`, with the decision stated.

### Clear performance issues and poor performance implementation

No finding. Measured on zeus (Linux 6.8.0-147, 12 cores) while the machine was busy (load average
3.8–5.9). Release build, `GoldenFixtures.Seed`, 4,000 Citizens, 1,440 Households, 267 Segments,
3,590 Bins, one route worker, after 2,048 Ticks. These figures support behaviour claims only, not
quiet-machine timing.

| Ruleset | Mean Step | Staggered tier per Tick | `CheckEndOfRun`, mean of 10 |
|---|---|---|---|
| `declining.toml`, every Household owns a car | 141.0 µs | 0.56 µs (about 0.4%) | 22.0 ms |
| `minimal.toml` | 54.0 µs | 0.57 µs | 17.3 ms |

### Integrity and maintainability improvements

#### IE-I1 — Several end-of-run checks compare totals, so offsetting errors pass

- **Category:** integrity. **Priority:** medium. **Confidence:** confirmed by reproduction for
  traffic and parking; by code trace for the rest.
- **Finding:**
  - `TrafficIsConserved` and `ParkingOccupancyIsConserved` compare city-wide sums. IE-B1 is a
    reachable case of +1/−1 passing. Probe P3 shows occupancy counted on the wrong Car Park also
    passes (A: holder, occupancy 0; B: no holder, occupancy 1).
  - `TheQueueMatchesItsReservations` counts the reviews list and never checks which rows are on it.
  - `TheCompositionIndexNamesEveryGroup` checks list lengths against the live count only.
  - `DistrictPoolsAreOneLiveBinPerGood` checks that a Bin exists. It does not check that the Bin is
    unique or that its Resource is the Good.
  - `AgesMatchTheirBins` walks existing Expiry rows only, so a perishable Bin with no Expiry row goes
    unchecked.
- **Evidence:** `WorldInvariants.cs:138-177,204-245,2083-2092,2149-2157`. The one guarded creation
  path for District Pools is at `World.cs:6442`.
- **Recommendation:** replace each sum with a per-row tally at the same O(world) cost. Traffic is
  covered by IE-B1's repair. Parking is latent today, because both writers move the holder and the
  count together (`World.cs:8511-8512,8633-8645`).
- **Acceptance:** each strengthened check fires on a planted offsetting pair.
- **Routing:** proposed new issue, `needs-triage`, excluding the traffic check.

#### IE-I2 — Ten invariants have never been shown to fire, and a coverage test proves less than its name

- **Category:** integrity. **Priority:** medium. **Confidence:** confirmed by search and code trace.
- **Finding:** these ten `Invariant` members appear in no test that makes them fail:
  - `ThePoolNamesOnlyUnhousedHouseholds`, `ThePoolIsDense`, `BuildingHasRoomForTheHousehold`
  - `CitizenIsNotAlreadyEmployedHere`, `CatchmentIsUnchangedSinceItWasLaid`
  - `TheCompositionIndexNamesEveryGroup`, `TheCityAndItsOutsideBalance`, `AgesMatchTheirBins`
  - `VacantLotHasFrontage`, which only runs green in `LotLongRunTests.cs:158-171`
  - `CitizenIsInExactlyOneWorkplace`, which is only named in a comment at `GoldenFixtures.cs:672`
- **More gaps:**
  - No meta-test requires every live member to have a failing case.
  - `Every_row_is_covered_within_one_full_sweep` (`InvariantTierTests.cs:266`) clears every
    Household's member list and then asserts that some violation was found. One slice would satisfy
    it. `The_slices_partition_the_table` covers the arithmetic.
  - `A_household_balance_check_follows_the_money_declaration` (`:76`) has no assertion.
  - The three "violates nothing" tests run on worlds where the checks hold trivially. This is the
    same pattern as #239, where a test named as an audit never performs the step it audits.
- **Recommendation:** add a failing-case test for each of the ten. Add a reflection test requiring
  each live member to appear in an expected-violation assertion. Fix the two named tests. RT-C06 can
  ride in the same PR.
- **Acceptance:** deleting the body of any registered check turns at least one test red.
- **Routing:** proposed new issue, `needs-triage`, together with IE-I4.

#### IE-I3 — Departing on a shopping or Service Trip erases the Citizen's last Trip

- **Category:** integrity. **Priority:** low. **Confidence:** confirmed by reproduction.
- **Finding:** `ShoppingEngine` and `CivicEngine` reset `LastTripFate` to `InFlight` on departure
  and read it back to detect arrival. So `OfCitizen` returns no last Trip while the Citizen travels.
  Two contracts say otherwise:
  - `CitizenTable.cs:426` says the column is written in `RecordTripFate` "and nowhere else".
  - `Evidence.cs:609-611` says a stored `InFlight` means no journey has ever ended.
- **Evidence:** `ShoppingEngine.cs:299,439`; `CivicEngine.cs:176,432`. The base city with no reload
  gave 6,315 mid-trip samples over 66 Citizens with no last Trip.
  `LastTripFateTests.Somebody_mid_journey_who_has_travelled_before_has_both` (`:226`) covers only a
  commanded Trip in a fixture without shopping.
- **Recommendation:** give the engines their own arrival signal and stop writing the evidence
  column outside `RecordTripFate`. Extend that test to a shopping world.
- **Acceptance:** the extended test passes on a shopping world.
- **Routing:** with IE-B4.

#### IE-I4 — The invariant coverage test cannot see names that begin with a one-letter word

- **Category:** integrity. **Priority:** low. **Confidence:** confirmed by code trace and a regex
  replica.
- **Finding:** `InvariantCoverageTests` matches backticked names with
  `` `([A-Z][a-z]+(?:[A-Z][a-z]+)+)` ``, which cannot match a name starting `A…`. Both `ADistrict…`
  names in `02 §10`'s tier table are therefore invisible to it. One of them,
  `ADistrictCellNamesALiveDistrictAndBuiltGround`, names no member. The enum has
  `ADistrictCellNamesALiveDistrict`, and the built-ground half moved out of the end-of-run tier
  (`WorldInvariants.cs:1737-1742`). The test also counts a member as live if `Invariant.X` appears
  anywhere in `src/`, comments included (`:110`).
- **Evidence:** `tests/Borough.Tests/Corpus/InvariantCoverageTests.cs:42-43,110`;
  `docs/02-simulation-model.md:1073`; `src/Borough.Core/Invariants/Invariant.cs:932,961,1048`.
- **Recommendation:** allow one-letter words in the pattern and correct the table cell.
- **Acceptance:** the corrected test fails on the current cell and passes on the fixed one.
- **Routing:** with IE-I2.

#### IE-I5 — The treasury identity test omits four of the thirteen money-flow counters

- **Category:** integrity. **Priority:** low. **Confidence:** confirmed by code trace.
- **Finding:** `TreasuryFlowsExplainTheBalanceTests` sums 9 of the 13 `MoneyFlowCounter` columns.
  It omits `Compensation`, `Grant`, `Rent` and `UtilityImport`. Every fixture derives from
  `taxing.toml` and states none of them, so they are 0 and the "every interval" identity holds
  without exercising them. Despite its name, the test never reads the `TreasuryFlows` type.
- **Evidence:** `tests/Borough.Tests/Invariants/TreasuryFlowsExplainTheBalanceTests.cs:298-326`;
  `src/Borough.Core/Instruments/Metric.cs:602-900`. #212 adds `Tariff`, which makes it 10 of 14.
- **Recommendation:** add each counter that crosses the treasury's edge, and one fixture where each
  is non-zero. Settle which of the four cross that edge as part of the repair.
- **Acceptance:** the identity holds on a fixture with grants, compensation, rent and utility
  imports.
- **Routing:** proposed new issue, `needs-triage`. It sits next to #196 and #210 but is separate
  from them.

## Reviewed with no finding

- **Scheduling:** `Commit` runs the staggered tier every Tick in both hosts. Each slice is
  `Tick % Slices`, and `Range` partitions the slot count, so dead slots are covered. `Slices` is
  neither set by any host nor saved, so a reload does not change coverage. Headless runs call
  `CheckEndOfRun` at the end; the shell calls it only on load (IE-D1).
- **Boundedness:** no unbounded evidence or instrument collection. The table below names each sink.

  | Collection | Sink |
  |---|---|
  | Census ring | Overwrites at 1,024 readings |
  | Engine flows | Drained by the Census. In the shell only `TreasuryFlows` is drained, and the rest would overflow after about 10^18 Ticks |
  | Condemnation trail | 256-row ring |
  | Care history | Evicts the oldest at `HistoryKeeps` |
  | Care Days | Pruned past `ReportDays` |
  | Trip and commute evidence columns | Overwritten per Trip and zeroed when a row is freed |

  The `ushort` Day fields saturate at 65,535 Days, which is 134,215,680 Ticks and out of reach.
  A recycled slot cannot inherit evidence because freeing a row zeroes it and handles carry
  generations.
- **Refusal trace:** `Main.Send` asks `Simulation.Refuses`, which uses the same predicate as
  phase 0. The shell words `LastRefused`. When several commands are refused in one Step only the last
  is worded, which is minor and not filed.
- **Readouts:** every declared Readout reads what its name says, and `IsReadableAgainst` matches
  every caller and the loader's checks.

## Open questions

These were seen in code but not verified. None is a finding.

- When `SlotCount` grows mid-cycle, a row near a slice boundary may be skipped for one cycle.
- `Bins.Move` at `World.cs:6855-6856,9362` moves Bin levels without the per-Tick capacity guard
  that `Deposit` and `Withdraw` carry.
- A Rule that starves on Tick 0 stores `StarvedSince = 0`, which is also the "not starving"
  sentinel (`RuleEngine.cs:2168-2171`).
- `EveryHandleResolves` coverage depends on `world.Tables` listing every table that holds handles.
  That was not checked.
- `Series.Complete` reports false when the oldest surviving reading sits exactly on the window
  floor (`Census.cs:567`). This looks harmless.

## Coverage limits

- No Godot shell session was driven. Shell claims rest on code traces.
- No population above 4,000. No run longer than 20,000 Ticks. No evidence was checked after a reload.
- Not reviewed in depth: `HinterlandReading`, `PopulationReading`, `RentEvidence`, the construction
  refusal path of `LotEvidence` (`Evidence.cs:939-996`), `EvidenceTests`, `UtilityEvidenceTests`,
  `MoneyCensusTests`, `HinterlandReadingTests`, `Main.Panels.cs`, `Main.Readout.cs`, and #223's
  `NeedsReading`.
- Not every per-Tick `Require` site was checked for reachability.
- IE-B4's failure start is injected. A real failing commute was not produced inside 20,000 Ticks of
  the base city.

## Validation and reproductions

Harnesses live under [`evidence/invariants-evidence-review/`](evidence/invariants-evidence-review/).
Each `run.sh` takes a checkout as its first argument (default: the script's own repository). It
copies the probe into `tests/Borough.Tests/ReviewProbes/`, runs `scripts/test.sh` under
`timeout 600`, and removes the probe on exit. The probes reproduce defects; they are not regression
tests and should fail after a repair.

| Finding | Command |
|---|---|
| IE-B1 drift | `plans/evidence/invariants-evidence-review/a-invariant-probes/run.sh <checkout> 'FullyQualifiedName~Borough.Tests.ReviewProbes.InvariantsProbeA.P2'` |
| IE-B1 permanent failure | `plans/evidence/invariants-evidence-review/v-split-then-bulldoze/run.sh <checkout>` |
| IE-B2 | `plans/evidence/invariants-evidence-review/a-invariant-probes/run.sh <checkout> 'FullyQualifiedName~Borough.Tests.ReviewProbes.InvariantsProbeA.P1'` |
| IE-I1 parking | `plans/evidence/invariants-evidence-review/a-invariant-probes/run.sh <checkout> 'FullyQualifiedName~Borough.Tests.ReviewProbes.InvariantsProbeA.P3'` |
| Cost table | `plans/evidence/invariants-evidence-review/a-invariant-probes/run.sh <checkout> 'FullyQualifiedName~Borough.Tests.ReviewProbes.InvariantsProbeA.M1'` |
| IE-B3 | `plans/evidence/invariants-evidence-review/b-labour-shortfall/run.sh <checkout>` (recorded output in `run-output.txt`) |
| IE-B4, IE-I3 | `plans/evidence/invariants-evidence-review/b-commute-cause/run.sh <checkout>` (recorded output in `run-output.txt`) |

The shared baseline was
`scripts/test.sh --filter 'FullyQualifiedName~Borough.Tests.Invariants|FullyQualifiedName~Borough.Tests.Evidence|FullyQualifiedName~Borough.Tests.Instruments|FullyQualifiedName~DerivedRebuildAuditTests|FullyQualifiedName~ReadoutTests'`.
It passed 199 of 199 tests in 2 min 46 s. One verifier reran P1, P2 and V1 and checked every material
finding against the code. V1 was rerun once more from its retained copy and gave the same result.

## Decisions and next actions

- Decide IE-D1: whether a failed check refuses a load, warns, or is caught at save time.
- File IE-B1, IE-B3, IE-B4 with IE-I3, IE-D1, IE-I1, IE-I2 with IE-I4, and IE-I5 as `needs-triage`
  issues, and attach IE-B2's evidence to #132. Nothing has been filed yet.
- IE-B1 is the most consequential. It is reachable in ordinary play and ends with saves that cannot
  be loaded.
