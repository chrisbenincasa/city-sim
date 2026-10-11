# Founding probe

Replays a founding log and prints one tab-separated row per Day. It serves the operation checks in
[the founding loop plan](../../plans/first-playable-founding-loop.md). It is not in the solution.

Headless `--money` and `--income` refuse `--log`, so this replays the log with `Replay.Trace` and
reads `MoneyLedger.Of` and `Simulation.DrainTreasuryFlows` at each Day boundary.

From the repository root:

```sh
dotnet build spikes/FoundingProbe -c Release
dotnet spikes/FoundingProbe/bin/Release/net10.0/FoundingProbe.dll \
  rulesets/base/ruleset.toml rulesets/base/founding.borough 120 30 > founding-120.tsv
```

The arguments are the Ruleset, the log, the number of Days and an optional save Day. With a save
Day, the probe also saves at that Day, reloads, continues to the last Day and compares every daily
State Hash with the uninterrupted run. It exits 1 when they differ. The end-of-run invariants run
at the last Day.

Set `PROBE_NOON=1` to sample each Day at noon instead of midnight. Shops are shut at midnight, so
midnight samples report most unserved outings as `Closed`.

## Columns

| Column | Meaning |
|---|---|
| `treasury`, `tills`, `households$` | Money held by the treasury, all Businesses and all Households |
| `supply` | Money issued so far |
| `citizens`, `households`, `unplaced`, `employed`, `businesses`, `segments` | Live counts |
| `mill#`, `mill$`, `mill_arrears`, `mill_staff`, and the same for `grocer` and `teaching` | Count, summed till, Businesses in arrears and staff of each trade |
| `flour_min` … `sundries_rate` | Lowest and highest District Pool price, seller stock and summed rate of each good |
| `starving` | Starving Rule Instances, as `rule:count` pairs |
| `shopping` | Unserved outings, failed attempts, unserved reasons and outing stages |
| `purses` | Households with a worker, without a purse, at 0 and under 100, and balance percentiles |
| `hash` | State Hash at the end of the Day |
| `income_tax` … `other_out` | Treasury flows during the Day, from `TreasuryFlows` |
