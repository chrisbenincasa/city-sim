---
description: Reuse-first implementation
priority: 90
---
- Trace the flow before changing anything. Read the symbol with Serena and decide whether the
  requested behaviour needs new code at all.
- Prefer an existing code path, then `Borough.Core.Arithmetic`, then an installed dependency. A
  bespoke component needs a written exception naming the property no library supplies (`adr/0018`).
- Fix the smallest shared root cause. Delete code when behaviour survives the deletion.
- Skip speculative guards, abstractions, configuration and cleanup outside the request. Three similar
  lines beat a premature abstraction.
- A tuning number belongs in the TOML Ruleset and is hot-reloadable (`adr/0015`). A `const` in
  simulation source where a Ruleset value belongs is a defect.
- While iterating, run the narrowest check that exercises the change: `scripts/test.sh <Area>`. The
  unfiltered suite is a milestone gate, not a working-lane habit (`adr/0121`).
