# State storage and arithmetic review

Sweep 02 of the [system review](system-review-brief.md).

## Scope

- **Revision:** `b673754915417832a0231839de26141abe10e9fd` on `main`.
- **Covered:** `src/Borough.Core/Tables/`, `Arithmetic/`, `Quantities/`, `Determinism/`;
  `World.HashState`, `World.RebuildDerived`; `src/Borough.Analysers/` and
  `tests/Borough.Tests/Analysers/`; `DerivedRebuildAuditTests`, `TableRegistrationTests`; every
  `Randomness.Draw` site; the `[layers]` percent keys in `RulesetLoader`.
- **Flows traced:** row allocation, free and slot reuse; handle folding; hash order; derived
  rebuild on load; save, reload and run-on for 12 shipped Rulesets; placement and reassessment
  draws; Ruleset values through `RulesetLoader` into Q16.16.
- **Decisions consulted:** `adr/0003`, `0004`, `0038`, `0058`, `0065`, `0071`, `0110`, `0112`,
  `0115`.
- **Existing work:** no issue or board row covered this sweep. Sweep 01's unrouted findings
  (RT-C01, C02, C04, C06–C09) are excluded.
- **Open PRs touching swept paths:** #212 (`World.cs`, `DerivedRebuildAuditTests.cs`), #219
  (`World.cs`), #221 (`Quantities/Tiles.cs`). None changes a finding. #219 adds one more derived
  index held outside tables (`LotsInCells`), which widens SS-I1. #212 shifts `RulesetLoader.cs`
  line numbers by about +80. Owners should recheck SS-I1 and SS-B2 after merge.
- **Not covered:** `Transcendental.cs` table bounds; thread-count equivalence of the rebuild;
  performance (no candidate arose).

## Findings

### Obvious bugs

#### SS-B1 — With `pollution_decay_ticks = 0`, a reloaded world diverges

- **Category:** bug. **Priority:** medium. **Confidence:** confirmed by reproduction.
- **Finding:** `MapLayers._pollutionDirty` carries state from an emission to the next pollution
  cadence. Nothing saves, hashes or rebuilds it. When decay is on, `DecayPollution` re-marks every
  source each cadence and hides the loss. When decay is 0 it returns early, so a world saved
  between an emission and the next cadence never diffuses that emission after reload.
- **Consequence:** breaks save/reload equivalence. Pollution, land value and desirability diverge
  from the first cadence after the save. The loader accepts 0 as "the plume never fades". No
  shipped Ruleset uses 0 (all 132 occurrences are 2048). A hot reload from decay > 0 to 0 reaches
  the same state; that path was read, not run.
- **Evidence:** `src/Borough.Core/Space/MapLayers.cs:209` (field), `:408-421` (marked on
  emission), `:507-522` (cleared on diffusion), `:932-937` (early return at tau 0), `:532-536`
  (`RediffusePollution`, documented as "what a load would call" but called only by
  `Borough.Headless/LayerDump.cs:56`). `World.RebuildDerived` and `MapLayers.Adopt` never mark
  dirty. `PollutionDirtyReloadProbe`: decay 2048 passes; decay 0 fails with "diverged at tick 705
  (saved at 701)", dirty at save `True`, after load `False`.
- **Recommendation:** call `RediffusePollution` from `RebuildDerived`, or mark every non-zero
  source dirty there. Hash unchanged for decay > 0. Small, one PR.
- **Acceptance:** the decay 0 probe passes; a Factorio case runs `fouled.toml` with decay 0.
- **Routing:** proposed GitHub issue, `needs-triage`.

#### SS-B2 — Six `[layers]` percent keys load with the wrong sign above 16383

- **Category:** bug. **Priority:** low. **Confidence:** confirmed by reproduction.
- **Finding:** `IntegerMath.RoundDiv(int, int)` doubles its numerator unchecked, so it wraps once
  the numerator reaches 2^30. `RulesetLoader` reads `noise_intensity_percent`,
  `shoreline_intensity_percent`, `desirability_{pollution,noise,shoreline}_percent` and
  `fertility_pollution_percent` with a minimum and no maximum, then computes
  `RoundDiv(Fixed.FromInt(x), 100)`. Values 16384–32767 load with a negative weight. Values of
  32768 and above throw `OverflowException` from `Fixed.FromInt`'s checked multiply instead of a
  refusal.
- **Consequence:** a designer value in that range flips the sign of a desirability or fertility
  term without any message, or crashes the load. Shipped values are 4–600, so only an authored
  edit or hot reload reaches it.
- **Evidence:** `src/Borough.Core/Arithmetic/IntegerMath.cs:122-130`;
  `src/Borough.Core/Arithmetic/Fixed.cs:54`; `src/Borough.Formats/RulesetLoader.cs:7889-7938`
  (keys), `:7995-8010` (`Number()` checks only the minimum). No `CheckForOverflowUnderflow` in
  any project. `RoundDivOverflowProbe`: 16384 gives -10737417 against 10737418; 20000 loads
  `IntensityPerFlow` -8367635 against 13107200; 40000 throws.
- **Recommendation:** compute `RoundDiv(int)` through the long overload with a checked narrow,
  and give the six keys a maximum so the loader refuses with a message. No hash change where it
  did not overflow. Small, one PR.
- **Acceptance:** `RoundDivOverflowProbe` passes; a loader test refuses 20000 and 40000.
- **Routing:** proposed GitHub issue, `needs-triage`.

#### SS-B3 — Same-Tick reassessment reuses placement's random draws

- **Category:** bug. **Priority:** low. **Confidence:** confirmed by code trace; effect not
  measured.
- **Finding:** `PlacementEngine.Place` houses households and then runs `Reassess` on the same
  Tick. `Reassess` can sample a household housed moments earlier. Both paths draw candidates on
  `PlacementCandidate` and the choice on `ChoiceDraw` with the same entity, Tick and key. The
  reassessment therefore sees the same Lot sequence (minus its own Lot) and the same uniform,
  plus the staying bonus. The household almost certainly stays, by reuse rather than by an
  independent choice. The `ChoiceDraw` comment claims the entity id separates consumers, which is
  false for this pair.
- **Consequence:** breaks per-draw independence for a small share of each Tick's newly housed
  households. Reachable only in `rulesets/chosen.toml`, the one Ruleset that sets both
  `[placement] mu_percent` and `reconsider_ticks`.
- **Evidence:** `src/Borough.Core/Rules/PlacementEngine.cs:211` (order), `:429-431` vs
  `:1567-1569` (candidates), `:500-503` vs `:1600-1603` (choice), `:1441-1461` (sampling filters
  only liveness and a valid Dwelling); `src/Borough.Core/Determinism/PurposeTag.cs:871-874`.
- **Recommendation:** give reassessment its own candidate and choice tags, or skip households
  housed this Tick. Either moves `chosen.toml` goldens. Small.
- **Acceptance:** a test houses H at Tick T, forces H into the reassessment sample at T, and
  asserts different draws.
- **Routing:** proposed GitHub issue, `needs-triage`.

### Bad design decisions

None confirmed. The lazy caches are recorded under open questions.

### Clear performance issues and poor performance implementation

None. No performance candidate arose in this scope.

### Integrity and maintainability improvements

#### SS-I1 — The rebuild audit never reloads, so it cannot see derived state held outside tables

- **Category:** integrity (test coverage). **Priority:** medium, because it let SS-B1 through.
  **Confidence:** confirmed by code trace.
- **Finding:** `DerivedRebuildAuditTests` clears and compares only columns in `world.Tables`, and
  rebuilds on the same `World` object. State held outside `Rows` keeps its live value through
  the audit, where a real load starts from constructor defaults. `TreasuryTable`'s derived
  `bin_head` and `bin_tail` sit outside `Tables`; `RebuildDerived` does rebuild them, but no audit
  checks it. Neither the audit nor `FactorioTests` applies Commands, and no test combines a
  Ruleset swap with a save.
- **Unaudited off-table derived state:** `MapLayers._pollutionDirty` (SS-B1), `DistrictMarkets`,
  `StandingHousing`, `CommuteRoster` heads and tails, `CarParkResidency`, `BuildingResidency`,
  `Frontage`, and `LotsInCells` once #219 merges. `_flowOfPool` and `JobAccess` already have
  their own load tests.
- **Already covered elsewhere:** Command-driven save, reload and compare exist in
  `StreetRemovalTests.cs:167-170`, `ZoningPaintTests.cs:62-71` and
  `HinterlandPersistenceTests.cs:217-230`.
- **Evidence:** `tests/Borough.Tests/Tables/DerivedRebuildAuditTests.cs:733-741`, `:782`, `:799`,
  `:821`; `src/Borough.Core/Entities/World.cs:430-436`, `:4946-4947`;
  `src/Borough.Core/Entities/TreasuryTable.cs:49-50`.
- **Recommendation:** add an audit variant that writes a save, reads it into a new `World`, steps
  both K Ticks and compares hashes, over the audit's worlds plus one Ruleset swap. Add Treasury to
  the column walk. One PR.
- **Acceptance:** the variant fails on the SS-B1 world before its fix and passes after.
- **Routing:** proposed GitHub issue, `needs-triage`.

#### SS-I2 — 25 analyser gaps compile cleanly in `Borough.Core`

- **Category:** integrity. **Priority:** low. No production code uses any of them today.
  **Confidence:** confirmed; each gap has a probe, and the control probe fires `BOR0201`.

| Rule | Gaps (probe names drop the `ReviewProbe_` prefix) | Smallest fix |
|---|---|---|
| BOR0301 hash-order walk | `HashSetListCtor`, `HashSetAddRange`, `HashSetCopyTo`, `HashSetUpcastForeach`, `DictionaryExplicitCast` | Report any conversion of a hash-ordered type to `IEnumerable`/`ICollection`, implicit or explicit; add `CopyTo` |
| BOR0201/0202 floating point | `FloatViaConvert`, `DoubleSqrt`, `GenericFloat` | Ban `System.Double`/`Single`/`Half` static members; flag any binary operation on a floating-point operand |
| BOR0203/0204 division and shift | `GenericDivision`, `GenericShift`, `IntDivRem` | Treat generic-math operators as built-in; ban `int.DivRem`/`long.DivRem` |
| BOR0205/0206/0302 entropy and identity | `CryptoRandom`, `GuidVersion7`, `RuntimeHelpersHash`, `ComparerStringHash`, `StringHashOverload`, `TupleStringHash` | Ban `RandomNumberGenerator`, every `Guid` factory, `RuntimeHelpers.GetHashCode`, `IEqualityComparer.GetHashCode` and every `GetHashCode` overload |
| BOR0207 scaled ratio | `ScaledRatioViaLocal`, `ScaledRatioCompound` | Follow a scaled local or compound assignment into `FloorDiv` |
| BOR0701 unmanaged state | `ManagedClassState`, `ColdPathEmptyReason` | Reject an empty `[ColdPath]` reason; optionally flag collection-array fields on classes |
| BOR0901 table storage | `TableWithoutAttribute`, `TableBaseClassField`, `TableDirectColumn` | Make `Column` constructors reachable only from `Rows`; walk base types; report a `Rows` owner without `[Table]` |
| BOR0801 purpose tag | `PurposeTagCast` | Report any conversion to `PurposeTag` outside its declaration |

- **Evidence:** `plans/evidence/state-storage-review/analyser-probes/`; root causes in
  `HashMapOrderAnalyser.cs`, `FloatingPointAnalyser.cs`, `BannedArithmeticAnalyser.cs`,
  `NondeterministicApiAnalyser.cs`, `ScaledRatioAnalyser.cs`, `UnmanagedStateAnalyser.cs`,
  `TableDeclarationAnalyser.cs`, `PurposeTagAnalyser.cs`.
- **Recommendation:** close BOR0301 first. It is the cheapest change and the likeliest door for
  ordinary code, because passing a `HashSet` to a method that takes `IEnumerable<T>` looks
  harmless. The rest can follow as one PR per analyser.
- **Acceptance:** each probe becomes a `Fires` case in `ArithmeticLintTests` or `StateLintTests`;
  `run-probes.sh` prints `fired` for every probe.
- **Routing:** proposed GitHub issue, `needs-triage`, one per analyser or one with a checklist.

#### SS-I3 — `TableRegistrationTests` matches tables by type, so a new instance of a registered type can go unsaved

- **Category:** integrity. **Priority:** low. **Confidence:** confirmed by reproduction.
- **Finding:** the guard compares `World._tables` with `[Table]` types by `Rows<T>` type. Three
  tables share `Rows<WheelBucket>`. Removing an existing one is caught by four `GoldenHashTests`,
  because every walked table folds its allocator scalars. A fourth `WheelBucketTable` that is
  never registered would escape both the guard and the goldens, and so escape the save and the
  hash. That is the `UnpremisedPool` failure the `_tables` comment describes.
- **Evidence:** `tests/Borough.Tests/Tables/TableRegistrationTests.cs:20-25`;
  `src/Borough.Core/Rules/EventWheel.cs:18-48`; `TableRegistrationByTypeProbe` stays green with
  `LifeStages.Buckets` removed.
- **Recommendation:** match `Rows` instances reachable from `World` by reference. Small.
- **Acceptance:** the probe's removal makes the guard fail.
- **Routing:** proposed GitHub issue, `needs-triage`.

## Open questions

- `DistrictMarkets` and `StandingHousing` stay correct only through hand-placed `Invalidate`
  calls; a missed call would diverge on reload without moving the hash. `LazyCacheProbe` passed
  on 12 Rulesets over 2,100 Ticks, but some of those cities may be near-empty. SS-I1's variant
  would cover this.
- Can a Lot's pattern change under a standing Building? If so, `HousingBuildings` can go stale.
- Rebuild correctness of the off-table structures in SS-I1 was not traced individually.
- About 54 shipped Rulesets were not swept for save/reload; the full sweep exceeded 25 minutes.
- `TravelTime.Over` throws above 32767 Ticks. Whether walk speed and map span reach that in a
  shipped Ruleset was not checked. Unchecked `Money` and `TravelTime` multiplications were
  sampled (about 40 sites, all widened to `long`), not enumerated.

## Reviewed with no finding

- Declaration and hash order are explicit: columns by declaration call order, tables by the
  `_tables` array. No reflection ordering.
- Every saved column is hashed, dead rows included. `FreeSlot` zeroes every column. The free
  list and allocator scalars are saved and hashed, so a reload reuses the same slots.
- Handles fold the target's monotonic id; a stale handle folds to a constant. Nothing orders by
  slot outside `Tables`. Intrusive-list slot links hash deterministically because allocator state
  is saved.
- The 57 derived columns are classified correctly, and `RebuildDerived` runs in a sound order.
- All 82 `Randomness.Draw` sites pass a named tag. Every shared tag stream other than SS-B3 is
  deliberate and documented.
- `Fixed.Mul`, `Fixed.Div` and `FloorDiv` are checked and round correctly for negatives.
  `Apportionment` guards its products.
- `RouteBatch`'s `Parallel.For` writes disjoint slots and sums after the join.
- No `BOR` diagnostic is suppressed anywhere in `src/`.

## Reproduction

Run from a checkout of the revision above, sequentially, because both scripts build in place.

| Command | Expected result at this revision |
|---|---|
| `scripts/test.sh --filter 'tier!=instrument&(FullyQualifiedName~Borough.Tests.Analysers\|FullyQualifiedName~Borough.Tests.Tables\|FullyQualifiedName~Borough.Tests.Arithmetic\|FullyQualifiedName~Borough.Tests.Determinism\|FullyQualifiedName~Borough.Tests.Quantities\|FullyQualifiedName~Factorio\|FullyQualifiedName~Replay)'` | 468 passed, 0 failed (shared baseline) |
| `plans/evidence/state-storage-review/review-tests/run-review-tests.sh 'FullyQualifiedName~RoundDivOverflowProbe\|FullyQualifiedName~PollutionDirtyReloadProbe\|FullyQualifiedName~TableRegistrationByTypeProbe'` | 10 tests, 5 fail: RoundDiv at 16384 and 32767, noise intensity at 20000, the 40000 refusal, pollution decay 0 |
| `plans/evidence/state-storage-review/analyser-probes/run-probes.sh` | 25 probes print `BUILD OK, no BOR diagnostic`; the control prints `fired BOR0201` |
| `PROBE_FILES=fouled.toml,care.toml plans/evidence/state-storage-review/review-tests/run-review-tests.sh 'FullyQualifiedName~LazyCacheProbe\|FullyQualifiedName~ReloadSweepProbe'` | Pass. Without `PROBE_FILES` these run every Ruleset and take over 25 minutes |

Two investigators and one verifier produced the findings. The verifier reran every harness from
a fresh worktree. A failing review test means the defect is still present; after a repair
it should pass.

## Next actions

- Decide which proposed issues to file: SS-B1, SS-B2, SS-B3, SS-I1, SS-I2, SS-I3.
- SS-B1 and SS-I1 belong together; the audit variant is the regression check for the bug.
- Recheck SS-I1 after #219 merges and SS-B2's line numbers after #212 merges.
