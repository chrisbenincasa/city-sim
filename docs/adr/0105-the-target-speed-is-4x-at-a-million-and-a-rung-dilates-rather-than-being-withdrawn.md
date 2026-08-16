# The target speed is 4× at a million Citizens, and a rung dilates rather than being withdrawn

**The simulation is budgeted at 15.6 ms a Tick at 1,000,000 Citizens — 4×, the top of `01 §1`'s
ladder — and every rung of that ladder is offered at every city size for ever.** No rung is ever
greyed out, withdrawn, or made conditional on population. When a host cannot sustain the rung the
player selected, it **dilates wall-clock time and says so**; it does not take the rung away.

**The budget is stated against a named machine and a named thread count**, which is
[`0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md)'s
subject and not restated here.

**The target is set roughly 3× away from where the ledger stands today, and that is recorded rather
than hidden.** [`plans/0013`](../../plans/0013-tick-budget.md) re-summed at the shipped clock reads
**≥44–50 ms** on one core of the reference machine; the target is **283–318%** of it. Two named levers are
between the two and neither has been measured.

Guiding concepts: `PLAYER GOVERNS`, `HONEST DEGRADATION`.

**Arguable** under [`0043`](0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md).
No machine produces a target: a machine says what something costs and a target says what is wanted.
`adr/0052` does **not** apply and no ratifier is owed — see *Why this sat unargued*, below.

## Why

### Taking a rung away is a worse failure than missing one

The ladder is a control the player holds, and `01 §1`'s core loop —
**Observe → Diagnose → Intervene → Wait → Observe** — puts `Wait` in the middle of it. A large city is
precisely where a turn of that loop is slowest to come round: more Citizens, more Buildings, longer to
see whether an intervention worked. **A design that withdraws fast-forward as the city grows removes
the loop's `Wait` step exactly at the scale the loop is hardest to close.**

`01 §1` said the opposite in one clause — *4× is "the first thing a large city stops offering"* — and
that clause is struck by this decision. The rest of the row survives: 4× is still *getting somewhere,
not watching*, and that is a statement about what the rung is **for**, not about who may have it.

**The alternative was argued and refused with the user in the room, on 2026-08-16.** A two-point
specification — the whole ladder guaranteed at 10,000 Citizens, only 1× guaranteed at 1,000,000 — is
what the corpus was already drifting toward, it is what `adr/0096` had keyed itself on, and it is a
defensible engineering position. It was refused on a product ground that no measurement touches:
***options disappearing as a city progresses is a worse experience than a rung that runs slower than
it says.***

### Withdrawal and dilation are different things, and only one of them is refused

This is the distinction the decision turns on, and the corpus had no word separating them.

| | What the player sees | Status |
|---|---|---|
| **Withdrawal** | the 4× control is unavailable at this city size | **Refused here** |
| **Dilation** | the 4× control works; the host delivers fewer than 64 Ticks/s and reports *simulation running behind* | **Mandatory**, and already decided |

Dilation is [`0019`](0019-ticks-per-day-is-a-balance-constant-not-a-pacing-knob.md)'s surviving rider
— *"dilate wall-clock time, never skip Ticks"* — and it is
[`03 §3.9`](../03-agent-architecture.md)'s second row, where a **hardware** limit is answered with
fewer Ticks per second plus an indicator. Both predate this ADR and neither moves. What this decision
adds is that dilation is the **only** permitted response to a rung the host cannot sustain, so the
first row of that table — lowering simulation fidelity — may never be spent to protect a speed
setting.

**No rung is guaranteed in wall-clock terms and no game could guarantee one.** 64 Ticks/s at 1M on
unknown hardware is not a promise anybody can make. What is guaranteed is that the *control* is
present, that the simulation is identical whichever rung is chosen (`CONTEXT.md` → Speed: *"no speed
setting can change any outcome"*), and that falling short is announced rather than concealed.

### The ledger could not have chosen this and it is important to say why

`06` files this obligation on the ground that *"`plans/0013`'s whole ledger reads as a share of nothing
until it is settled"*, and `0013`'s own stop-and-fix condition says it *"cannot be evaluated until it
has been argued"*. Both are true and both invite the inverse reading — that the ledger, once summed,
would indicate a rung. **It cannot, for three separate reasons found while running this session.**

**The sum was stale.** `0013` prices routing at 9.4–10.5 ms and sums to ≥17.8 ms.
[`0094`](0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
quartered the Day, route searches fire per Trip and Trips are daily, so the routing row multiplies by
four — which `01 §1` and `CLAUDE.md` both state and which the ledger never applied. Re-summed it is
**≥44–50 ms**, and see *What this session found* in
[`plans/0027`](../../plans/0027-session-t-the-target-speed.md) for why that is worse than a stale
number.

**No rung fits, so no rung is indicated.** At ≥44–50 ms the ladder reads 35–40% / 71–79% / 141–159% / 212–238% / **283–318%**
from 0.5× to 4×. Picking 1× because it is the only column under 100% would have been picking the
column where the **row known to be wrong is small**: `0013` says routing's multiplicand *counts the
wrong event*, and the event it should count — a diverting Traveller re-searching — is priced at
**134.135 ms on its own**, 215% of a 62.5 ms Tick. A ledger cannot arbitrate between rungs when its
largest row is wrong in kind and the correction's direction is known to point up.

**And the option set was a ladder the product does not have.** `0013` prices **8×**, which `01 §1`
removed, and has no column for **0.5×** or **3×**, which it added. This document's own framing —
*"8× / 4× / 2× / 1×"* — is a ladder retired by session P and quoted by session K's inventory a month
later. ***A table of options is a fact stored in prose and drifts like any other.***

### Why this sat unargued for the whole life of the project

**Because it is the one product number that changes no state, so no ledger organised around state had a
row for it.** `CONTEXT.md` → Speed is categorical: the simulation cannot observe the tick rate, so no
speed setting can change any outcome. That makes the target speed **not hash-bearing and not
world-creation**, so [`0052`](0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
never applied and `plans/0002` §D — *numbers chosen and never ratified* — structurally could not hold
it. It is not a defect in a document either, so `plans/0012` could not. It surfaced only when `06`
built a table of **obligations no milestone can hold**, which is a table organised by *who owes what*
rather than by what kind of thing is owed.

***A number that changes no state has no home in a ledger organised by state***, and it accumulated
authority for months by sitting in `CLAUDE.md`'s constants table as *"15.6 ms at 4× speed"* — stated,
cited, and never chosen.

## Consequences

**`plans/0013` is denominated in 15.6 ms and the four-column table becomes the shipped ladder.** 8×
leaves the document; 0.5× and 3× join it. The bill is stated in milliseconds first and the shares
follow, which that file already requires of itself.

**`0013`'s stop-and-fix condition becomes evaluable, and it says keep building.** The old condition
could not fire because the chosen speed was one of its own levers. It now reads: *stop when the table
sums past 100% of 15.6 ms at 1M with **measured** multiplicands and a **measured** thread count.*
Today it sums to 283–318% with the largest multiplicand guessed and threading unmeasured, so the standing
answer is the one that file already gives — **keep building while every over-budget row has a guess in
it**.

**The gap is ~3× and the two levers that could cover it are both named and both unmeasured.**
*Threading* is `0013` lever 2: everything measured is single-threaded, Tick phase 2 is parallel by
construction, and S5 L6 measured the Lane kernel at **1.84–1.93× on two threads** with four bimodal on
a contended machine. ⚠ **That figure may not be carried to the Rule engine or to routing** — it is one
kernel, and `adr/0096` exists because a number travelled without its clause. *The routing multiplicand*
is the other, and it is expected to move the wrong way. **So threading must cover more than 3× if the
routing correction lands where R6.3 says it will**, and saying so is what stops this target reading as
comfortable.

**Session R stops gating nothing and becomes what the target rests on.** `06` records `05 §6`'s
threading policy as gating no milestone in 6–24, which was true of *milestones* and is now false of
*this decision*: a 15.6 ms budget means 15.6 ms of a core, and which thread runs `step()` is
unanswered. **T is conditional on R** — see [`0106`](0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md).

**The Microscopic Cap does not move, and `adr/0096`'s own revisit trigger fired and was honoured.**
That ADR is keyed on two claims: `03 §3.9`'s two-box table (leg 1) and *"it is the rung the binding
case will not be running"* (leg 2). **Leg 2 is now false and leg 1 is untouched.** A Cap is a world
constant that decides which Segments are exact, identical on every machine; a budget is a wall-clock
bill that dilation can absorb. Pricing the Cap at 15.6 ms would buy a smooth top rung by making traffic
permanently less exact for every player in every city, which is the trade `03 §3.9` separates into two
boxes precisely so it is not made by accident. **The Cap stays at 62.5 ms**, and `adr/0096` carries an
amendment saying which of its legs fell.

**`01 §1` loses one clause and keeps its ladder.** *"The first thing a large city stops offering"* is
struck. The rungs, their budgets, their purposes and the legibility argument that produced 3× all
stand.

**Nothing in the build changes and no State Hash moves.** Speed is a host concern the simulation cannot
observe, which is the same property that made this number homeless.

## What would trigger revisiting

**The routing multiplicand becoming real and landing where R6.3 says.** If a measured diversion rate
puts routing anywhere near 134 ms at 1M, 15.6 ms is not 3× away, it is 10× away, and a target nobody
can approach stops being a target. The response is an architecture pass, not a re-tune — the condition
above is written so it fires.

**Threading coming back flat.** If `step()` does not parallelise — if lint 4's equivalence turns out to
cost more than the parallelism buys, or if `05 §6` lands on a single simulation thread — then the only
lever of the right size is gone and this target is unreachable on the reference machine. That is
session **R**'s to report.

**A play session finding that 1× at 1M is enough.** This decision spends engineering effort to keep a
control available; if the first real play of a large city shows nobody reaches for 4× there, the effort
is being spent on a rung nobody wants and the two-point form refused here becomes right after all.
⚠ **The refuting observation is behavioural and not a benchmark**: a large city in which the top rungs
go untouched.

**The ladder changing again.** This ADR is keyed on 4× being the top of `01 §1`'s ladder. If a rung is
added above it, the target follows the top and this derivation is re-read rather than the number
re-tuned — which is `adr/0096`'s trigger, restated here because it is the one that actually fired.
