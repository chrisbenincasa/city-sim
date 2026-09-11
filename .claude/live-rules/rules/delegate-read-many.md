---
description: Delegate the read-many questions; keep symbol reads in the main loop
priority: 45
---
- Serena is how to read code whose location is already known. A subagent is how to avoid reading
  forty files yourself. Both apply here; the shape of the question decides which.
- Delegate when the answer is a sentence and the evidence is many files: corpus sweeps across
  `plans/`, `docs/adr/` and `CONTEXT.md`; the blast radius of a hash-bearing Ruleset edit across
  `rulesets/`, `tests/` and the Golden fixtures; consistency between two large documents.
- Keep in the main loop: a symbol body, a caller list, a known-path read, a single fact.
- Pick the agent by need. `Explore` for read-only fan-out. `fork` when it needs this conversation's
  history. `isolation: "worktree"` for independent slice work alongside the live worktrees.
- Relay the conclusion. A subagent's report is not shown to the user.
