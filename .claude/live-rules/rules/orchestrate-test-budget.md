---
description: Orchestrated runs spend the full test lane once
prompt: ["/orchestrat/i"]
globs: [".orchestra/**"]
priority: 5
---
- Split an orchestrated run into at most two briefs unless the user asks for more.
- Implementers and loop or gate reviewers run narrow filters only (`scripts/test.sh <Area>` or
  `--filter`). Briefs and reviewer prompts say so explicitly and never ask for the full lane.
- A brief commits after its narrow checks pass. Inside a run this replaces CLAUDE.md's per-commit
  full `scripts/test.sh`.
- The orchestrator runs the full `scripts/test.sh` exactly once, on the final commit, as the done gate.
- A gate finding the reviewer marks non-blocking goes to the plan's known gaps, not into a fix brief.
- Why: utilities slice 2 (10/20/2026) took about 9 hours. Eight or more full lanes on a loaded
  machine cost 2.5 to 3 hours, and four serial briefs each waited on the last.
