---
name: next
description: Orient in city-sim using Git, worktrees and the single backlog when the user asks what is next or wants to pick up unfinished work without naming it.
---

# Find the next available work

Check `git status --short`, `git worktree list`, recent local commits, and remote commits after
`git fetch --quiet`. If fetching is unavailable, say the remote view may be stale. Never discard
uncommitted work or choose an item already being built in another worktree.

Read [the backlog](../../../plans/0000-board.md), then only the selected item's plan and relevant
code. [PROCESS.md](../../../PROCESS.md) owns the workflow; the old amnesty and separate ledgers
are retired. Do not use their historical status to schedule work.

Check a prerequisite in the code or evidence that owns it. Historical reports marked for
verification are leads, not established current defects or gates. If Git contradicts the backlog,
report the specific discrepancy. Do not reconstruct a second status ledger.

For an orientation request, give a short report: current work, next available outcome, real blocker
if any, first concrete step, and one reasonable alternative. Ask which to pursue, then stop.
If the user already asked to implement a specific outcome, continue that authorised work instead
of asking them to select it again.
