# Working on Borough

## Start and choose work

Check Git status, recent local and remote commits, and the worktrees before choosing work.
Read [the backlog](plans/0000-board.md), then the plan and code for the selected item.
An occupied worktree is taken. Preserve other people's uncommitted work.

The backlog is the only maintained list of all known unfinished work: gameplay, design, art,
audio, interface and accessibility, architecture, performance, tooling, tests, delivery and
documentation. Work needing discovery or a decision belongs there too. Recording a candidate
commits us to considering its scope, not to building it. Each active item names its outcome,
owner or worktree, plan, and any prerequisite that actually prevents starting it. Questions needed
to implement that item belong in its plan. Other actionable findings get one backlog entry.
Remove completed entries; commits and PRs record completion.

## Plans and design

Use the board's state to distinguish **scoping**, **design**, **ready**, **active** and **deferred**.
Scoping establishes current behaviour, the gap, boundaries and acceptance checks; design resolves
a named choice; ready means implementation can start; active names its owner; deferred names a
revisit trigger. A candidate may be declined after consideration; record the decision in the
relevant design text and remove its entry. Section order outside active/next is not a priority order.

Keep one entry per independently selectable outcome. Group related candidates while scoping;
split them when they need different owners, prerequisites or acceptance checks. A broad entry
must name its contents and a concrete next step. Put subordinate tasks in its plan; promote one
to the board when it becomes independently scheduled work. Do not require a separate plan or a
full design just to capture work. Roadmap and deferred-design documents supply context and
rationale; the board owns selection, state and ownership. Add newly discovered work as it appears,
without creating an exhaustive audit gate or treating historical absence claims as present facts.

Use one short plan per active outcome: intended outcome, decisions needed, implementation steps
where useful, and acceptance checks. Do not create a plan for a routine fix. Existing numbered
identities remain readable; new plans may use descriptive filenames to avoid allocation races.

Update durable design documentation when the design changes. Explain the current decision and
the reason a future maintainer needs. An ADR is optional for a substantial lasting tradeoff, not
a required output of every discussion. Code comments explain contracts, units, invariants and
non-obvious reasons. Do not put session narratives, repeated policy arguments or closure histories
in them. Measurements retain their machine, world, command, conditions and limitations beside
the result; measure again when a decision depends on present performance.

Provisional tuning is allowed. Mark it beside the authored value or in the active plan, and evaluate
it in a demonstration. There is no central ratification register or requirement to turn each
chosen number into another task. A claimed measurement still needs evidence.

`CONTEXT.md` owns domain terminology. The roadmap describes longer-term capabilities, not a second
schedule. Code and tests establish implemented behaviour; documentation explains intent.
Only an explicit design refusal is a reason to rule out a capability. Missing code is work to
consider, not a permanent constraint.

## Finish and validate

Run focused checks while iterating and `scripts/test.sh` before a commit. The full unfiltered suite
and long-run checks remain milestone validation; post-submit CI runs them separately. Preserve
determinism, save/reload equivalence, bounded state and the simulation/render boundary. Re-record
golden fixtures when behaviour deliberately changes. A visible capability needs a driven
demonstration and an account of what observation exposed, in its plan or PR.

Finish by updating the affected design if necessary and removing the completed backlog entry.
The commit or PR says what changed, why and how it was checked. Documentation-only improvements
are legitimate; do not add source code to qualify a commit.

## History and supersession

This workflow replaces the amnesty and the former board/ledger/audit/ratifier system by the user's
instruction on 2026-09-12. Amnesty ended by that decision, not by reaching its ratio. Its ratio,
word and ADR ceilings, freezes and source-change requirement are retired. The suspended procedural
requirements of ADRs 0043 and 0052 do not resume.

This file takes precedence over older process instructions in plans, skills and ADRs, including
mandatory reciprocal citations, coverage maps, separate correction ledgers, exhaustive number
ratification, argumentative prose, and retaining superseded text in the working tree. Technical
and gameplay decisions in those records continue to apply unless separately changed.

Delete obsolete prose instead of appending its correction history. Git retains old versions.
When a historical result remains useful, link to a pinned revision with its original qualifications.
Legacy entry points may retain a short redirect for existing links; they are not active ledgers.
Do not recreate the retired structure under new filenames or add tests for its shape.
