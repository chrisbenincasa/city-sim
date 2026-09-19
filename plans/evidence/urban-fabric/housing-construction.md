# Capacity-shortage housing construction

The executable specimen is `HousingConstructionTests`, using the five-Lot redevelopment
walkthrough. `rulesets/urban-housing.toml` exposes the same form envelopes and provisional search
budgets to the normal Ruleset loader. It is a Core mechanics fixture, not balanced city content.

The specimen observes:

- Four distinct unplaced Households and full occupied neighbours support either two two-tenancy
  terraces or one four-tenancy courtyard. Selection can choose either deterministically from its
  seed. A terrace commit changes only its own site; the hypothetical second terrace is not queued.
- Before placement, the Pool still contains four Households, but the two newly available tenancies
  reduce uncovered need to two. Further samples, a second housing Zone Rule and subsequent Ticks
  cannot answer the same capacity shortage again.
- Existing vacancies missed by placement suppress construction. Augmenting-path matching protects
  the only cheap vacancy from being consumed as evidence by a flexible, wealthier seeker.
- An unaffordable prospective home supplies no evidence. Each Household compares with its own
  Outside; a tie or loss supplies no positive construction evidence. Mixed-use Buildings subtract
  the actual fitted Business from the shared ceiling.
- Repainting, occupancy changes and already-covered demand invalidate a proposed commit without
  mutating saved or derived tables. Exceeding the complete-coverage budgets refuses before building
  matching scratch. No sample is extrapolated to city-wide need.
- Save/load after the first terrace reconstructs two remaining uncovered seekers. Repainting the
  remaining Lot prevents the old form from continuing. A separate branch resumes automatic Zone
  Rule construction with identical State Hashes at one and two route workers and end-of-run
  invariant checks.

Review of the integration exposed recycled sample slots: whole-Lot assembly can retire a Lot that
was also drawn later in the sweep. Housing sweeps now capture handles before acting and skip retired
identities. The full working-lane check also exposed two fixture/Formats obligations: form envelopes
need unique names for Ruleset package identity lowering, and shipped fixtures declare money.

## C# allocation observation

Measured 2026-09-18 on `zeus`, Intel Core i5-10400 @ 2.90 GHz, .NET 10, Release. The synchronous
selection call runs on one test thread. Other validation was running, so this is **not a timing or
quiet-machine throughput measurement**.

Command:

```sh
scripts/test.sh --filter 'FullyQualifiedName~HousingConstructionTests|FullyQualifiedName~RulesetSourceLegacyTests|FullyQualifiedName~RulesetSchemaTests|FullyQualifiedName~RulesetKeyNoteTests' -- -m:1 --no-restore --logger 'console;verbosity=detailed'
```

Log: `/tmp/borough-test-20260918-233739.log`; 84 tests passed. After one warm call, selection on
**5 Lots, 2 full Buildings and 4 seekers**, with source limit 3 and form/site attempt limit 32,
allocated **1,840 managed bytes**, measured by `GC.GetAllocatedBytesForCurrentThread`. This counts
one call's allocation, not retained heap, peak process memory or maximum-budget cost. Matching
buffers are reused across alternatives within that call. The geographic permission table's separate
[C# storage measurement](permission-storage-csharp.md) remains applicable.

## Boundaries

Only authored opt-in housing Rules use this path. It handles capacity shortage immediately,
including first homes. Persistent preference mismatch alongside otherwise usable vacancies needs
saved elapsed-Tick evidence in a later slice. The current search compares at most two Buildings
within a consecutive vacant window extending from the sampled Lot in increasing Street coordinate.
It is not exhaustive block redevelopment. Complete coverage is currently bounded by Lot and
Building slot high-water marks; above either authored limit it refuses rather than inferring absence.
Numeric intensity caps, broader layout search, District trade assembly and new shell controls remain
outside this slice.

## Final validation

The final Release working-lane gate passed **3,833 tests**, including golden/replay, save/reload,
derived rebuild, route-worker equivalence and the existing long-running assertion fixtures:

```sh
scripts/test.sh -- -m:1 --no-restore --logger 'console;verbosity=normal'
```

Log: `/tmp/borough-test-20260918-234115.log`. The unfiltered instrument suite was not rerun for this
incremental commit; the pre-existing parking-scarcity instrument issue remains on the backlog.
Focused validation above includes the new allocation instrument. Godot Debug build, the repository
formatting check and Taplo lint of all 53 shipped Rulesets also passed. The schema and reference
were regenerated through the headless runner, and their consistency checks passed in the gate.
