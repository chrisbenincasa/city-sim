# A milestone number is an identity and the roadmap's order is the sequence

**A milestone's integer is a **name**, allocated next-free and never re-used. It does **not** encode
position. What orders the work is the **row order of [`06`](../06-roadmap.md)'s milestone table**, which
is read top to bottom. **Inserting work appends a new number and moves a row; it renumbers nothing.**
The existing retired-numbering table stays for ever, and no renumber is ever performed again.**
`SOLVE THE ACTUAL PROBLEM` `LEGIBLE CAUSE`

**This supersedes the *insertion rule* in [`PROCESS.md`](../../PROCESS.md) → *Numbering*** — *"a
milestone that has not started holds a position rather than a name, so inserting one renumbers the
unshipped tail"* — and amends the row that reads *"the integer is the position in the sequence."*
**Nothing else in that scheme moves**: phases, slices, tasks, sessions, spikes and rounds are untouched,
and a shipped milestone's number was already frozen.

## Why

**The rule was affordable on a premise, the premise was measured, and it has expired.** `PROCESS.md`
states it in the same breath as the rule: renumbering the tail *"is affordable precisely because it only
ever touches rows nobody has built, **which are the least cited rows in the corpus**"* — with the
number that made the case, session K's **126** references against **282** for the shipped head.

🔴 **Counted 2026-08-22, before the third insertion was attempted: the unshipped tail carries 276
citations across 73 files.** That is not *the least cited rows in the corpus*; it is within 2% of what
the shipped head cost on the day the rule was written to avoid touching it. ***The tail became
expensive because the corpus kept describing work it had not started***, which is the same property
that makes this project's planning good. **The rule punishes exactly the behaviour the rest of the
corpus rewards.**

**And the cost is not the edit, it is the class of error the edit invites.** `06`'s own instruction is
that ***"the mapping is applied by reading each citation's **subject**, never by its digits"***, because
two renumbers have already happened and a pre-renumber citation means something different from a
post-renumber one. **The corpus has recorded that trap firing twice** — `06`'s roots table contradicting
its own **#** column, and a citation translated as though it were old when it was not. ***A rule whose
correct application requires 276 subject judgements is a rule that will be applied wrongly***, and the
failure is silent: a mistranslated citation still resolves, still reads plausibly, and points at the
wrong work.

**The property being bought is already broken, three times over.** *The integer is the position* is
false of **5a**, **5a-bis**, **5b**, **5b-bis** and **5c**, which `PROCESS.md` itself notes *"keep names
that no longer sort."* It is false across the retired-numbering generations, where `12` names two
different milestones depending on the fortnight. ***Paying 276 citations to preserve a property four
rows already violate and two renumbers have already invalidated is paying for something that is not
there.***

**What the number is actually used for is citation, not ordering.** A reader asking *what is next* is
sent to [`plans/0000`](../../plans/0000-board.md), which is a view over `06` and `plans/0003` and states
order in prose. A reader meeting *milestone 21* in an ADR wants to know **which work**, and an identity
answers that better than a position does — because an identity does not move under them.

⚠ **The honest cost, stated rather than buried: `06`'s milestone table stops sorting by its own first
column.** A reader who wants build order must read the table's rows in order rather than sort by number.
**That is a real loss** and it is why this record exists rather than the change being made quietly.

## Consequences

- **`06`'s milestone table is ordered by row and its `#` column is a name.** ⚠ **The table gains a
  standing note saying so**, because a numeric first column that does not sort is a trap without one.
- **The two milestones this record was written for are appended as 25 and 26**, and their rows sit
  **between 12 and the old 13** where the work belongs. ***That is the change working as intended and it
  will look wrong the first time.***
- **The retired-numbering table in `06` is frozen, not deleted.** Every citation written before
  2026-08-22 still resolves through it, and the two historical generations keep their date windows.
  **A third block will never be added.**
- **`PROCESS.md` → *Numbering* keeps its shipped-milestone clause**, which said the same thing this
  record now says of every milestone: *"the number has become history, and every citation pointing at it
  is a record of work that happened under that name."* ***This record generalises a rule the scheme
  already applied to half its rows.***
- **A number is still never re-used for different work**, and next-free allocation is what guarantees
  it. **The highest allocated number is found by reading the table**, never by counting rows.
- **In-flight branch names stop going stale.** `milestone-18-coarse-day-wheel` and
  `milestone-24-terrain-scoping` would both have been renamed by the insertion this record replaces.
- ⚠ **This does not repair the two renumbers already taken.** They happened, their mappings stand, and
  `06`'s subject-not-digits instruction still governs any citation written before today. **What ends is
  the production of new ones.**

## What would trigger revisiting

- **The roadmap table ceasing to be a single ordered list.** The order lives in the rows, so a table
  that splits into per-phase sections, or grows a second ordering axis, takes the sequence with it.
  ***Then the order needs a home of its own and this record's premise is gone.***
- **A tool that sorts milestones by number.** Nothing does today. One that did would silently reorder
  the plan, and the answer is to fix the tool — but if the tool cannot be fixed, reopen this.
- **The citation count collapsing.** If the unshipped tail ever again becomes the least-cited part of
  the corpus, the old rule becomes affordable again. ⚠ **Count before believing it** — this record
  exists because the premise was asserted for a year and measured once.
- **Numbers running out of legibility.** Next-free allocation with a moving table means the newest
  milestone is the highest number wherever it sits. At three digits, or with many insertions near the
  front, a reader may stop being able to hold the map. **That is a legibility failure, not a correctness
  one, and the fix is a column rather than a renumber.**
