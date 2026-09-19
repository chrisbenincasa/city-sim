# Persistent housing preference mismatch

Observed 2026-09-19 in the `urban-permissions` worktree. This extends the
[capacity-shortage construction fixture](housing-construction.md) with saved search episodes.

## Executable acceptance

`HousingConstructionTests` uses two unplaced Households, each with a balance of 100, and four empty
existing tenancies at rent 50. Their own Outside rent is 25; a prospective dwelling costs zero.
Centrality is neutral. The gap is substantial at the fixture's 0.10-utility margin. Persistence is
8 Ticks, freshness is 4 Ticks. A high choice sensitivity makes the poor homes lose the actual choice;
the evidence classification itself is independent of that probabilistic draw.

The tests establish:

- Qualifying observations every 1, 2 or 4 Ticks all first permit construction at Tick 8. Repeated
  observations at one Tick and time in the Pool before the first observation add no evidence.
- An eight-Tick unsampled gap restarts the four-Tick-freshness episode; a mature but stale episode
  cannot justify construction. Affordability or capacity observations clear previous mismatch.
- First homes and affordability shortages still qualify immediately. Minor utility gaps, ties and
  suitable available homes never supply preference evidence. A newly suitable home defeats a
  previously justified commit without mutating state, even before another placement pass.
- Current proposal rent and Outside comparisons still apply. Real new capacity suppresses another
  equivalent proposal while the original seekers remain unplaced.
- Pool swap-removal carries the correct Household's history; reentry and Ruleset adoption clear it.
  Incomplete coverage clears evidence; the invariant catches a clock later than the world's Tick.
- Saving mid-episode preserves the State Hash and continues real placement searches and automatic
  Zone Rule construction identically with one and two route workers, including end-of-run invariants.

The normal placement sampler may draw the same Household twice. In the automatic fixture, the first
Household starts at Tick 0, the second at Tick 4. Both have fresh qualifying elapsed evidence at
Tick 14. The post-Tick-14 state has three Buildings, with the new two-tenancy home between the
existing neighbours. Placement houses one seeker at Tick 16 and the other at Tick 18. Through
Tick 32 there is still only one addition. This exposed why asserting construction at Tick 8 from
Pool entry was wrong: only each individual's actual observations can establish its episode.

## Driven observation

Built Godot Debug, exported the exact automatic test fixture to a temporary `.borough-city`, and
loaded it through the existing menu/file picker. Used the drive skill's socket channel with a reply
read after every command, `focus 32 8 300`, `tilt 65`, and speed rung 1 between paused captures.
No shell code or Building assets changed.

| Captured Tick | Buildings | Vacant Lots | Drawn Building ids |
|---|---|---|---|
| 0 | 2 | 3 | 1, 2 |
| 7 | 2 | 3 | 1, 2 |
| 16 | 3 | 2 | 1, 2, 3 |
| 32 | 3 | 2 | 1, 2, 3 |

The [initial frame](../../../artifacts/visual-study/urban-preference/initial.png) and
[post-construction frame](../../../artifacts/visual-study/urban-preference/built.png) show the new
Building between the standing neighbours. The [captured draw rows](../../../artifacts/visual-study/urban-preference/observation.json)
confirm that both neighbours retain exactly their transforms and colours, and that no second
addition appears after placement. This mechanics fixture creates Households without Citizens, so
its population headline is zero; it does not demonstrate a playable immigration or movement loop.
The dawn frames are dark but the massing and occupied ground are visible. Shell explanations of
these episodes remain part of the urban-fabric interface follow-up.

The temporary export, source TOML, complete Core sequence, drive scripts, screenshots and readouts
are under `/tmp/urban-evidence/`. The reproducible Core acceptance fixture is
`HousingConstructionTests.Mid_episode_save_replay_and_route_threads_continue_real_search_and_construction`.

## Validation

Focused working lane: **31 tests passed**, log `/tmp/borough-test-20260919-081703.log`.
Godot Debug built without warnings or errors. Taplo lint passed for all 53 Rulesets. The generated
Ruleset schema and key reference include the three required preference keys.

Core save format advances 8 → 9. Both golden traces were re-recorded using the documented procedure;
only the second half of `session-trace.txt` changed. The hand-built world golden check and driving
trace remain unchanged. The saved columns are zero in those legacy construction worlds; the
nonzero episode acceptance is in the focused fixture above.

The final Release working lane passed **3,848 tests**, including the new housing checks,
golden/replay, save/reload, derived rebuild, route-worker equivalence and long-run assertions:

```sh
scripts/test.sh -- -m:1 --no-restore --no-build --logger 'console;verbosity=normal'
```

Log: `/tmp/borough-test-20260919-083143.log`. Repository formatting and `git diff --check` passed.
The full instrument lane was not rerun for this incremental slice. The completed gate follows a
corrected source-scanner false positive on test parameter names; no allocation mechanism changed.
