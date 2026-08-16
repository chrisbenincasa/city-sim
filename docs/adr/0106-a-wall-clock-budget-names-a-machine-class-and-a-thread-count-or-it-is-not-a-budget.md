# A wall-clock budget names a machine class and a thread count, or it is not a budget

**Every threshold this corpus states in milliseconds must name the machine class and the thread count
it applies to, in the same sentence as the number.** A duration without both is not a budget, not a
tripwire and not a band — it is a figure that silently retunes itself whenever the reader's mental
picture of the hardware changes, and it can be neither passed nor failed.

**The reference class, until a shipping target exists, is the desktop every figure in the corpus was
actually taken on:** a 2020 six-core x86-64 desktop — Intel i5-10400 class, DDR4-2133 — running the
`powersave` governor, **one core, single-threaded**. Stated as a class rather than as a serial number,
and deliberately at the slow end.

Guiding concepts: `HONEST DEGRADATION`, `SOLVE THE ACTUAL PROBLEM`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
Which machine to *design against* is a product judgement; what a given machine costs is measured and is
`docs/spike-results.md`'s.

## Why

### The corpus owns two machines and they disagree by four times

`spike-results` → *The second machine* exists because a single host cannot tell you which of your
findings are about your code. Across the two machines this project has actually measured on:

| | Desktop (i5-10400) | MacBook Pro (M4 Pro) |
|---|---|---|
| Single-thread copy | 13.2 GB/s | **63.2 GB/s** |
| Single-thread read | 15.8 GB/s | **66.6 GB/s** |
| Read, best aggregate | 28.9 GB/s, 1.83× | 251.8 GB/s, **3.75×** |

**The same loop is bandwidth-bound on one and compute-bound on the other**, and `plans/0002` records
the consequence in the form that matters: K1's ratio-to-ideal *"degrades from 1.10× to 1.99× without
the code changing"*. A millisecond figure inherits every one of those factors and carries none of them.

### Three live thresholds already have this defect, and one of them is the Tick budget

`plans/0002` filed the pattern and named all three: **thresholds whose meaning depends on an unstated
machine.**

| Threshold | Where | What goes wrong |
|---|---|---|
| **The Tick budget**, 15.6 ms | `CLAUDE.md`, [`plans/0013`](../../plans/0013-tick-budget.md) | The whole ledger is `powersave`, one core, and says so in one line on page one; every share quoted out of it says neither |
| [`0037`](0037-the-world-is-single-buffered-and-hazards-are-per-table.md)'s **8–15 ms** async-copy band | `adr/0037` | Both M4 Pro figures fall **below its floor**, so a fast host does not pass the band — it leaves it |
| `plans/0004`'s **ratio tripwires** | `plans/0004` | Written as ratios against a hand-computed ideal that binds on one machine and not the other; the footgun variant reaches 4.53× on a fast host and would fire a wire it does not deserve to fire |

**The failure mode is specific and it is not vagueness.** A range meant as *acceptable* reads as
*expected*, so a machine that beats it looks like a machine that missed it, and a genuinely bad kernel
hides inside a bandwidth ceiling it cannot exceed. `adr/0037`'s band is the clean case: it was written
to describe a cost and a faster host makes it unsatisfiable in the *good* direction, which no reader
expects a budget to do.

### Naming the slow machine is the conservative choice and the other options are worse

Three were available.

**State it against the slow reference, as measured.** Real hardware then beats the budget, which is the
direction a budget should be beaten in, and no existing figure needs re-taking. The cost is that the
budget is pessimistic by an unknown amount — which is exactly the *upper bound* clause `plans/0013`
already attaches to every absolute it carries.

**State it against a fast machine, or against `performance` with turbo.** Closer to a player's box, and
it invalidates the upper-bound clause the whole ledger leans on: every existing absolute would have to
be re-taken before anything could be compared to anything. **A `performance` re-capture is owed
regardless** and is not this decision — when it lands, the class stays and the numbers under it tighten.

**Name no machine and treat milliseconds as absolute.** The status quo, and it is what produced the
three rows above.

### And a thread count is half the statement, not a footnote

`plans/0013`'s **lever 2** is that everything measured is single-threaded while Tick phase 2 is parallel
by construction. So *the same code on the same machine* has a per-Tick cost that differs by whatever
threading buys — S5 L6 measured **1.84–1.93× at two threads** on one kernel, bimodal at four.

**A budget of 15.6 ms is meaningless until somebody says 15.6 ms of what.** If `step()` shares the
render thread, the effective budget is a fraction of a frame; if it owns a core, it is 15.6 ms of that
core; if it fans out, it is 15.6 ms of wall clock across N. Those are three different targets wearing
one number, and **`05 §6` has never chosen between them** — which is why
[`0105`](0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md)
is explicitly conditional on session **R**.

⚠ **This is the clause most likely to be dropped when the number is quoted**, because a thread count
does not look like part of a duration. It is `plans/0012` **Cause 5** waiting to happen on a figure the
whole corpus is denominated in.

## Consequences

**The Tick budget is written as *15.6 ms on one core of the reference class*.** `CLAUDE.md`'s constants
table, `plans/0013` and `plans/0000-board.md` all carry the long form. Where a share is quoted, the
bill and the class travel with it — `plans/0013` already requires the first half of that and this adds
the second.

**`adr/0037`'s band and `plans/0004`'s tripwires are owed the same treatment**, and both are recorded
in `plans/0012` rather than edited here: this ADR states the rule, and applying it to a decision
somebody else argued is a correction with an owner.

**A `performance`, turbo re-capture of the reference desktop is owed and no verdict turns on it.** It
tightens every absolute in `plans/0013` in a known direction and moves no ratio. It was already owed by
S0a and by S5; this ADR does not add an obligation, it says which number the obligation is attached to.

**The second machine stays a control and never a target.** The M4 Pro exists in this corpus to
*disagree* with the desktop, and a finding that holds on both is a finding about the code. Quoting an
M4 Pro absolute as a budget would delete the only instrument the project has for telling those apart.

**Nothing in the build changes.** This governs how a threshold is written down.

## What would trigger revisiting

**A shipping hardware target.** The moment there is a minimum spec, the reference class is that spec
and this ADR's second paragraph is replaced rather than argued with. Nothing else in it moves.

**The reference desktop ceasing to be representative.** It is a 2020 part. If the class drifts far
enough below what anybody plays on, a budget stated against it stops being conservative and starts
being irrelevant — the tell is a ledger that fits comfortably on the reference and visibly does not on
the machine somebody is actually using.

**A threading policy that makes the thread count uninteresting.** If `05 §6` lands on a fixed
simulation thread count baked into the architecture, the count stops being a variable a quotation can
lose and this ADR's second half becomes a restatement of `05 §6` rather than a rule.
