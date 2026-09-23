---
name: next
description: Orient in city-sim, or establish whether work is actually finished. Use when the user asks what is next or wants to pick up unfinished work without naming it, and equally when they ask about the state of a named backlog row or plan — "are we finished with 0077?", "where are we on plan X", "is the urban fabric row done?", "what's left on the founding loop". Answer from Git, code and tests rather than from the board's prose, because a row accretes claims for months and goes stale.
---

# Finding work and finishing it in city-sim

Two questions arrive here. *What should I start* needs the backlog. *Is this finished* needs the
code. They share a first step and diverge after it.

## Both paths start with Git

Check `git status --short`, `git worktree list`, recent local commits, and remote commits after
`git fetch --quiet`. If fetching is unavailable, say the remote view may be stale. Never discard
uncommitted work or choose an item already being built in another worktree.

## Choosing work to start

Read [the backlog](../../../plans/0000-board.md), then only the selected item's plan and relevant
code. [PROCESS.md](../../../PROCESS.md) owns the workflow; the old amnesty and separate ledgers
are retired. Do not use their historical status to schedule work.

Check a prerequisite in the code or evidence that owns it. Historical reports marked for
verification are leads, not established current defects or gates. If Git contradicts the backlog,
report the specific discrepancy. Do not reconstruct a second status ledger.

Give a short report: current work, next available outcome, real blocker if any, first concrete
step, and one reasonable alternative. Ask which to pursue, then stop. If the user already asked to
implement a specific outcome, continue that authorised work instead of asking them to select it
again.

## Establishing whether a named item is finished

A board row is written once and appended to for months, so it holds every claim ever made about the
work and no account of where those claims leave it. One row runs past 500 words and calls its work
both unclaimed implementation and blocked on a person. Reading such a row back tells the user
nothing they could not read themselves, which is why this question keeps returning.

Treat the row and the plan as the list of claims, then settle each one elsewhere:

- A claimed mechanism is finished when its type or method exists and its test passes. Locate it with
  Serena, run the narrowest `scripts/test.sh <Area>` that covers it, and cite what you ran.
- A claimed Ruleset or document change is finished when the file says so. Read it.
- Work in flight sits on a branch or in a worktree rather than in the row.
  `git log origin/main..<branch>` says what is built.
- A step needing a person — a designer session, a playtest — is not unfinished work an agent can
  advance. Give it its own category, or the report implies work is available when none is.

Report in this shape, short enough to act on:

- **Done**, each with the evidence that settles it.
- **Outstanding**, each with the first concrete step.
- **Blocked**, separating a technical dependency from a step that needs a person.
- **Contradicted**, where a claim and the code disagree. Say which is right.

Do not edit the board while answering. The user asked a question, and rewriting their ledger is a
separate decision that needs its own approval.
