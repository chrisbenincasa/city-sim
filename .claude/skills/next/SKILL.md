---
name: next
description: Cold-start orientation for city-sim. Reads the tree and the corpus in a fixed order, works out what the next row of work is on whichever of the three tracks owns it, and reports where the project stands and what starting that row would mean. Use at the beginning of a session when the user says "next", "/next", "what's next", "pick up where we left off", or otherwise wants to start the next thing without describing it.
---

# Getting oriented and finding the next row

This corpus is ~28,000 lines of prose against ~19,600 lines of simulation. A cold start that reads
broadly burns the session's context before any work happens, and a cold start that reads the board
alone reports work that shipped an hour ago. The order below is what avoids both.

**Read the stages in sequence. Do not skip ahead, and do not read anything not named here** until
stage 6 tells you which row you are on.

**The output of this skill is a report and a question, never a commit.** You stop at stage 7.

---

## Stage 0 — the tree, before any prose

```
git log --oneline -15
git status --short
```

The board is the document most likely to be read *instead of* the build. On 2026-08-13 a sitting read
a paragraph of it to answer *what is next* and reported work that had shipped an hour earlier in the
same tree. [`adr/0093`](../../../docs/adr/0093-a-description-of-the-build-is-where-to-look-and-never-what-you-found.md)
— *a description of the build is where to look, and never what you found* — applies to the board itself.

Hold two things from this stage: **what the last three or four commits actually did**, and **whether
the working tree is dirty**. Uncommitted changes to `plans/` or `docs/` usually mean a session was
interrupted mid-write; that is a candidate for the next row all by itself, and it outranks the board.

## Stage 1 — the board's three live sections, and only those

Read [`plans/0000-board.md`](../../../plans/0000-board.md), these sections:

| Section | What you are taking from it |
|---|---|
| `## What is next` | The narrative of the last few closures, and the ⚠ warnings attached to the upcoming row |
| `## Do these next` | **The ranked table.** Columns: rank, track, task, plan document, why this one |
| `## Blocked` | The per-milestone gate table. Look up the row you landed on — *the cleared ones are listed too* |

**Skip** `## State of play`, `## Done`, `## Open tracks` and `## Owed` on a normal cold start. They
are history and menu; none of them names the next row. Read them only if a later stage sends you.

The board is **a view, never a source**. Where it disagrees with `0002`, `0003`, `06` or an ADR, those
win, and stage 3 is where you check.

## Stage 2 — reconcile the tree against the board

Compare what stage 0 found with what stage 1 claims. Three outcomes:

- **They agree.** Proceed.
- **The tree is ahead** — a commit did something the board still lists as pending. The board is stale.
  Say so in the report; updating it is a legitimate next row and is usually a five-minute one.
- **The tree contradicts the board** — a row marked closed has no commit behind it, or a row marked
  blocked has code in the tree. Stop and report the contradiction rather than choosing. This corpus's
  recurring failure (`plans/0012` *Cause 1*) is two copies of a fact drifting, and you have just found
  one; resolving it silently is how the wrong copy wins.

## Stage 3 — route by track to the owning document

`Do these next` gives each row a **track**. Each track has a different owning document, and the board
is not it.

| Track | Owning document | Also read |
|---|---|---|
| **code**, Phase 2 milestone | [`docs/06-roadmap.md`](../../../docs/06-roadmap.md) — the milestone table and the milestone's own row | The milestone's `plans/00NN` brief if one exists |
| **code**, Phase 0/1 slice or the hash-moving queue | [`plans/0003-build-plan.md`](../../../plans/0003-build-plan.md) — the slice ledger and the gate board | — |
| **argument** (a session) | [`plans/0002-open-questions.md`](../../../plans/0002-open-questions.md) §A and §C, and the session's `plans/00NN` brief | `PROCESS.md` → *The three tracks* for the standing rule |
| **spike** | [`docs/spike-results.md`](../../../docs/spike-results.md) → the spike's section | The spike's `plans/00NN` plan |
| **tidy** | Whatever the board's Plan column names | — |

Read the row's own plan document if it has one, and **read the ⚠ cells in the board's row in full** —
they are usually a trap somebody already fell into.

## Stage 4 — check the gate before anything else

**A gated slice must not be started before its gate clears.** Look the row up in `## Blocked`, and if
it names a gate, verify the gate's state in its owning document rather than trusting the board's cell.

⚠ Two gate failure modes this corpus has actually hit, both worth a moment:

- **A row held twice for unrelated reasons.** Clearing one gate does not clear the other. The S2
  harness deletion is the standing example.
- **A gate cleared elsewhere and never written back.** Milestone 5c read as blocked for two days
  after all three of its gates had been discharged in other documents. If a gate looks stale, check
  the document that owns the gate, not the board.

If the row is gated, the next row is the one below it in the table. Say which and why.

## Stage 5 — the standing traps, only where they touch this row

Do not audit the corpus. Check only what the chosen row will make you touch:

- **Numbers.** If the row will quote a figure, quote *the sentence*, never the digits
  (`plans/0012` *Cause 5*). Percentages of a Tick budget are the special case — **carry the bill, not
  the percentage**. The current bill is **≥44–50 ms a Tick** against a **15.6 ms at 4×, one core,
  2020 six-core x86-64** target ([`adr/0105`](../../../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md),
  [`adr/0106`](../../../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)).
- **New hash-bearing numbers.** If the row will choose one, it needs a **named ratifier** — a machine,
  **a world, and a quantity** (`adr/0052` as amended). A category is not a name.
- **Absences.** If the row's reasoning rests on *the simulation does not do X*, classify X as
  *unbuilt*, *undesigned* or *refused* (`adr/0070`). Only **refused** is evidence; otherwise the
  answer is build X.
- **Claims.** Before settling anything, type it (`adr/0043`): name the number that would refute it and
  the machine that would produce it. If you can, it is measurable and belongs to a spike.

## Stage 6 — read the code only if the row is code

If and only if the chosen row is on the code track, and only for the files it names:

- `mcp__serena__get_symbols_overview` on the target file
- `mcp__serena__find_symbol` with `include_body=true` for the specific symbols

Never open a code file with `Read` when a Serena tool covers the task. And **open the mechanism rather
than trusting a sentence about it** — where a description of the build is wrong, it is wrong about the
*trigger* (`adr/0093`).

## Stage 7 — report, then stop

Give the user a debrief of **six lines or fewer**, plain English, no corpus register:

1. **Where the project stands** — one line, from the tree, not the board.
2. **The next row** — its rank, track, name, and the document that owns it.
3. **Its gate** — cleared, or what it is waiting on.
4. **What starting it would actually mean** — write a plan document, run a session, take a capture,
   fix a filed defect. Name the first concrete artefact.
5. **Anything stale you found** — a board row the tree contradicts, a gate cleared and not written
   back. One line.
6. **The question** — offer the row, and name the runner-up if there is a reasonable one.

Then **stop and wait**. Do not scope, do not write, do not edit. The user picks.

---

## What this skill does not do

- It does not update the board. If the board is stale, say so and offer it as a row.
- It does not read `CONTEXT.md` or `PROCESS.md` end to end. Pull single terms from them when a
  document uses a word you need, and no more.
- It does not read `plans/0012` or `plans/0013` unless the chosen row is about corpus debt or about
  what a Tick costs.
- It does not read the archive halves of `plans/0002` (everything below `# Archive`) or
  `plans/0000a-board-archive.md`. The archive is an index of one-line summaries; a one-line summary is
  a caveat-free compression of somebody else's sentence, which is `plans/0012` *Cause 5* by
  construction. Follow the link instead of quoting the line.
