# 0027 — Session T: the target speed

**Run 2026-08-16, one sitting, with the user in the room. Three decisions, two ADRs, six findings.**

The last of `06`'s five *obligations no milestone can hold* to be given an owner, and the first to be
discharged. It was scheduled *"now, and it is the cheapest session in the corpus"*. It was cheap. It
was also the session where a document that exists to add numbers up turned out not to have added them
up.

| | |
|---|---|
| **Question** | Which rung of `01 §1`'s ladder is the simulation's Tick budget set against at 1,000,000 Citizens? |
| **Type** | **Arguable** ([`adr/0043`](../docs/adr/0043-a-claim-a-measurement-could-settle-must-not-be-settled-by-argument.md)) — no machine produces a target |
| **Blocked** | [`0013`](0013-tick-budget.md) entire; its own stop-and-fix condition; [`adr/0096`](../docs/adr/0096-the-microscopic-cap-derives-from-the-design-speeds-budget-and-not-from-the-top-rungs.md), which had already re-based the Microscopic Cap on it once |
| **Produced** | [`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md), [`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md); amendments to `adr/0019` and `adr/0096`; `01 §1`, `0013`, `06`, `0000`, `0002`, `0012`, `CLAUDE.md` |

---

## The three decisions

**1. The target speed is 4× at 1,000,000 Citizens — 15.6 ms — and no rung is ever withdrawn.**
[`adr/0105`](../docs/adr/0105-the-target-speed-is-4x-at-a-million-and-a-rung-dilates-rather-than-being-withdrawn.md).
Every rung of `01 §1`'s ladder is offered at every city size for ever; a host that cannot sustain the
rung the player chose **dilates wall-clock time and says so**.

**The alternative was on the table with a recommendation behind it and was refused.** A two-point
specification — the whole ladder guaranteed at 10,000 Citizens, only 1× guaranteed at 1,000,000 — is
what the corpus was already drifting toward, is what `01 §1` said in one clause, is what `adr/0096` had
keyed itself on, and is what the ledger's arithmetic pointed at. It was refused on a product ground no
measurement touches: ***taking options away while a city progresses is weird.*** The counter that
carried it is that the core loop's `Wait` step is hardest to close in a large city, so withdrawing
fast-forward there removes the loop exactly where it is most needed.

**2. A wall-clock budget names a machine class and a thread count.**
[`adr/0106`](../docs/adr/0106-a-wall-clock-budget-names-a-machine-class-and-a-thread-count-or-it-is-not-a-budget.md).
The reference class is a 2020 six-core x86-64 desktop — i5-10400 class, DDR4-2133 — at `powersave`, **one
core**, deliberately the slow end. This discharges a pattern `plans/0002` had been carrying since the S4
rounds and which it had already named against this exact number.

**All three of the options offered were taken**, which is coherent because they are riders rather than
alternatives: state it against the slow reference *now*, owe the `performance` re-capture, and record
that the number means nothing until `05 §6` says whose core it is.

**3. The Microscopic Cap does not move.** `adr/0096`'s own revisit trigger fired — it is keyed on *"4×
being the rung a large city withdraws"* — and the decision survives on its other leg. **A Cap and a bill
are different objects**: a Cap is a world constant that decides which Segments are exact, identical on
every machine; a budget is a wall-clock bill dilation can absorb. Pricing the Cap at 15.6 ms would make
traffic permanently less exact for every player in every city to stop the top rung stuttering on one
machine, which is `03 §3.9`'s first row spent to avoid its second.

---

## What this session found

### 1. The ledger that owns the sum was stale by ×4, against a correction it was itself carrying

`plans/0013` priced routing at 9.4–10.5 ms and summed to **≥17.8 ms**.
[`adr/0094`](../docs/adr/0094-a-day-is-2048-ticks-because-ticks-per-day-is-a-sampling-rate-and-not-a-length-of-life.md)
quartered the Day; route searches fire per Trip and Trips are daily, so the row multiplies by four.
Re-summed: **≥44–50 ms**, which is **283–318%** of the target rung.

**Three copies, and the owner is the one that drifted.** `01 §1` states the ×4 and quotes ~47.6 ms;
`CLAUDE.md` carries it; `plans/0013` does not. That inverts the usual shape — `plans/0012` **Cause 1**
is *a fact with several copies, and the copy nobody owns is the one that goes wrong*, and here the
**source** is the wrong copy while two views are right.

⚠ **And it is worse than a cross-document miss, because both copies are inside `plans/0013`.** That
file's *Segment volume attribution* row was re-derived on **2026-08-14** by exactly this reasoning
(~80,000 → ~320,000 pairs a Tick), and its sidebar on `adr/0061`'s 861.87% has said since 2026-08-13
that `adr/0094` *"multiplies every routing count by 4"*. **The correction was in the document, twice,
attached to two other numbers, and never reached the row.**

> ***A correction attached to a number does not travel with it, exactly as a caveat does not.***
> `plans/0012` **Cause 5** with the polarity reversed once more: Cause 5 is a *caveat* left behind when
> digits are copied; this is a *correction* left behind when a premise expires. **The tell is that no
> mechanical check could have caught it** — every one of this corpus's checks is document-to-document,
> and here there was no second document to disagree with. Registered as **Cause 5's third form**.

It is also 5c task 6's own finding committed by the document that recorded it: ***a premise that expires
retires every site resting on it, and finding one of them is not finding them.*** Nine days.

### 2. The option set was a ladder the product had already retired

`06`, `plans/0000` and `plans/0013` all framed the question as **8× / 4× / 2× / 1×**. Session P's
`01 §1` ladder is **pause / 0.5× / 1× / 2× / 3× / 4×**. So the ledger priced a rung the game does not
offer and had no column for two it does — and **0.5× is not decorative**, being where `01 §7` requires
rendered traffic to be visually truthful.

***A table of options is a fact stored in prose and drifts like any other.*** The framing survived a
month, three documents and session K's inventory pass.

### 3. No rung fits, so the ledger could not have chosen one — and everybody expected it to

`06` files this obligation because *"`plans/0013`'s whole ledger reads as a share of nothing until it is
settled"*, and `0013`'s condition says the speed *"cannot be evaluated until it has been argued"*. Both
true; together they imply an arbitration the ledger cannot perform.

At ≥44–50 ms the ladder reads **35–40% / 71–79% / 141–159% / 212–238% / 283–318%**. Choosing 1× because
it is the last column under 100% would have been choosing **the column in which the row known to be
wrong is small**: `0013` says routing's multiplicand *counts the wrong event*, and the event it should
count — a diverting Traveller re-searching — is priced at **134.135 ms on its own**.

> ***A ledger says what a choice costs and never which choice to make***, and once its largest row is
> wrong **in kind** it cannot do the first reliably either.

### 4. "64 Ticks/s is Factorio's rate" is wrong twice, and the conclusion survives anyway

`adr/0019`'s first rider. **Factorio runs at 60 UPS.** 64 is `2⁶` off that ADR's own 16 Ticks/s
reference rate; it landed within 7% of a figure from another game and was read as corroboration.
***A number arriving with no clause saying what it measures is a coincidence of magnitude rather than
evidence*** — `plans/0012` **Cause 5**, and the **first sighting against a source outside the corpus**,
where no check we could ever build would reach.

**The mapping is also backwards, which is larger than the digit.** Factorio has no speed multiplier in
normal play: 60 UPS is its only rate and it is a **design choice**, that game's whole mechanical timing
being denominated in 1/60 s. The corresponding thing here is the **reference rate, 1×** — Factorio has
nothing corresponding to 2×/3×/4×. So the rider took another game's design rate, mapped it onto our
ladder top, and concluded the ladder top was not a design choice.

**Nothing built on it moves**: the top rung *is* a performance budget, because `01 §1` says 4× is
*getting somewhere, not watching*. A citation defect, not a design defect.

**The comparison nobody ran is the one that pays.**

| | in-world s per update | budget | ms of compute per in-world second |
|---|---:|---:|---:|
| Factorio, 60 UPS | 0.0167 (1:1) | 16.67 ms | **1,000** |
| Us, 1× | 42.1875 | 62.5 ms | **1.48** |
| Us, 4× | 42.1875 | 15.6 ms | **0.37** |

**~2,530× more world per update for ~1/2,700th the compute per in-world second.** Both directions are
real: a larger per-update budget here is arithmetic rather than indulgence, *and* Factorio's per-update
work is trivial per entity in hand-optimised C++ where ours runs Dijkstra searches. ***Two rates sharing
a unit are not thereby comparable.***

### 5. The Layer row's price is stale by ~16×, and the same note re-derived its other half

`plans/0013`'s Layer note was amended on 2026-08-13 to move the residency **knee** from 256 to 8,192
sources for the 512-Cell map, and left the **cost** at its 128-Cell value. A whole-map recompute is
`O(cells)`, so **1.01 ms at 16,384 Cells is ~16 ms at 262,144** — which at the target rung exceeds a
whole Tick on its own, at any population.

**No verdict turns on it** (the floor and the amortised figure are unaffected, and a real city's dirty
region is a fraction of the map), and the shape is the finding: ***one reader re-derived one consequence
of a premise and the other consequence sat two lines away.*** Same shape as finding 1, same file, found
the same afternoon. **Owed: re-measure rather than scale**, since a 16× extrapolation is what this
document refuses everywhere else.

### 6. This question had no home because it changes no state

`CONTEXT.md` → Speed is categorical: the simulation cannot observe the tick rate, so **no speed setting
can change any outcome**. That makes the target speed **not hash-bearing and not world-creation**, so
[`adr/0052`](../docs/adr/0052-a-hash-bearing-number-is-chosen-with-a-named-ratifier-or-not-at-all.md)
never applied and `0002` §D — *numbers chosen and never ratified* — structurally could not hold it. It
is not a wrong sentence either, so `0012` could not. Its only home in the whole corpus was one cell of
`0013`'s *How this file gets less embarrassing* table.

> ***A number that changes no state has no home in a ledger organised by state.*** It sat in
> `CLAUDE.md`'s constants table as *"15.6 ms at 4× speed"* for the life of the project — stated, cited,
> denominating everything, and never chosen. It surfaced only when `06` built a table organised by *who
> owes what* rather than by what kind of thing is owed, which is an argument for that table existing.

**The value did not change. Its standing did.** 15.6 ms was right; nobody had picked it.

---

## What this session leaves

**Session R is promoted from *gates nothing* to what the target rests on.** `06` records `05 §6`'s
threading policy as gating no milestone in 6–24 — true of milestones, false of this decision. A 15.6 ms
budget means 15.6 ms **of a core**, and which thread runs `step()` is unanswered. `0013` **lever 2** is
now lever 1 and is the only named lever the size of the ~3× gap; S5 L6's **1.84–1.93× at two threads**
is one kernel and ⚠ **may not be carried to the Rule engine or to routing**.

**`0013`'s stop-and-fix condition gained a third term and became evaluable.** *Measured multiplicands,
**measured thread count**, 100% of 15.6 ms at 1M on the reference class.* Today: 283–318%, largest
multiplicand guessed, threading unmeasured — **keep building**. Without the third term the condition
would fire on a single-threaded figure against an architecture that is parallel by construction and
unexercised.

**Owed, and none of it blocks anything.**

| Owed | To whom |
|---|---|
| Re-measure the whole-map Layer recompute at 512 Cells rather than scaling it | `0013`, whoever next touches Map Layers |
| Check whether the walk-search row's 464 routes are commute-derived, and so take the same ×4 | `0013` — **deliberately not applied blind**, which is how the volume premise expired unnoticed |
| Apply `adr/0106` to `adr/0037`'s 8–15 ms band and `plans/0004`'s ratio tripwires | `0012`, two rows |
| A `performance`, turbo re-capture of the reference desktop | S0a/S5's standing debt; **no verdict turns on it** |

**What would reopen the decision** is in `adr/0105`'s revisit triggers and the sharpest is behavioural:
**a play session in which nobody reaches for the top rungs in a large city.** This decision spends
engineering effort keeping a control available; if nobody uses it there, the two-point form refused here
was right after all.
